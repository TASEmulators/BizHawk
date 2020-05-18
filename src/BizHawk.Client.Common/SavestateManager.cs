using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class SavestateManager
	{
		public static void SaveStateFile(IEmulator emulator, string filename)
		{
			var core = emulator.AsStatable();

			// the old method of text savestate save is now gone.
			// a text savestate is just like a binary savestate, but with a different core lump
			using var bs = new BinaryStateSaver(filename);
			if (Global.Config.SaveStateType == SaveStateTypeE.Text)
			{
				// text savestate format
				using (new SimpleTime("Save Core"))
				{
					bs.PutLump(BinaryStateLump.CorestateText, tw => core.SaveStateText(tw));
				}
			}
			else
			{
				// binary core lump format
				using (new SimpleTime("Save Core"))
				{
					bs.PutLump(BinaryStateLump.Corestate, bw => core.SaveStateBinary(bw));
				}
			}

			if (Global.Config.SaveScreenshotWithStates && emulator.HasVideoProvider())
			{
				var vp = emulator.AsVideoProvider();
				var buff = vp.GetVideoBuffer();
				if (buff.Length == 1)
				{
					// is a hacky opengl texture ID. can't handle this now!
					// need to discuss options
					// 1. cores must be able to provide a pixels VideoProvider in addition to a texture ID, on command (not very hard overall but interface changing and work per core)
					// 2. SavestateManager must be setup with a mechanism for resolving texture IDs (even less work, but sloppy)
					// There are additional problems with AVWriting. They depend on VideoProvider providing pixels.
				}
				else
				{
					int outWidth = vp.BufferWidth;
					int outHeight = vp.BufferHeight;

					// if buffer is too big, scale down screenshot
					if (!Global.Config.NoLowResLargeScreenshotWithStates && buff.Length >= Global.Config.BigScreenshotSize)
					{
						outWidth /= 2;
						outHeight /= 2;
					}

					using (new SimpleTime("Save Framebuffer"))
					{
						bs.PutLump(BinaryStateLump.Framebuffer, s => QuickBmpFile.Save(emulator.AsVideoProvider(), s, outWidth, outHeight));
					}
				}
			}

			if (Global.MovieSession.Movie.IsActive())
			{
				bs.PutLump(BinaryStateLump.Input,
					delegate(TextWriter tw)
					{
						// this never should have been a core's responsibility
						tw.WriteLine("Frame {0}", emulator.Frame);
						Global.MovieSession.HandleSaveState(tw);
					});
			}

			if (Global.UserBag.Any())
			{
				bs.PutLump(BinaryStateLump.UserData,
					delegate(TextWriter tw)
					{
						var data = ConfigService.SaveWithType(Global.UserBag);
						tw.WriteLine(data);
					});
			}

			if (Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie is TasMovie)
			{
				bs.PutLump(BinaryStateLump.LagLog,
					delegate(TextWriter tw)
					{
						((TasMovie)Global.MovieSession.Movie).LagLog.Save(tw);
					});
			}
		}

		public static bool LoadStateFile(IEmulator emulator, string path)
		{
			var core = emulator.AsStatable();

			// try to detect binary first
			var bl = BinaryStateLoader.LoadAndDetect(path);
			if (bl != null)
			{
				try
				{
					var succeed = false;

					// Movie timeline check must happen before the core state is loaded
					if (Global.MovieSession.Movie.IsActive())
					{
						bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.CheckSavestateTimeline(tr));
						if (!succeed)
						{
							return false;
						}
					}

					using (new SimpleTime("Load Core"))
					{
						bl.GetCoreState(br => core.LoadStateBinary(br), tr => core.LoadStateText(tr));
					}

					// We must handle movie input AFTER the core is loaded to properly handle mode changes, and input latching
					if (Global.MovieSession.Movie.IsActive())
					{
						bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleLoadState(tr));
						if (!succeed)
						{
							return false;
						}
					}

					bl.GetLump(BinaryStateLump.Framebuffer, false, PopulateFramebuffer);

					string userData = "";
					bl.GetLump(BinaryStateLump.UserData, false, delegate(TextReader tr)
					{
						string line;
						while ((line = tr.ReadLine()) != null)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								userData = line;
							}
						}
					});

					if (!string.IsNullOrWhiteSpace(userData))
					{
						Global.UserBag = (Dictionary<string, object>)ConfigService.LoadWithType(userData);
					}

					if (Global.MovieSession.Movie.IsActive() && Global.MovieSession.Movie is TasMovie)
					{
						bl.GetLump(BinaryStateLump.LagLog, false, delegate(TextReader tr)
						{
							((TasMovie)Global.MovieSession.Movie).LagLog.Load(tr);
						});
					}
				}
				finally
				{
					bl.Dispose();
				}

				return true;
			}

			return false;
		}

		private static void PopulateFramebuffer(BinaryReader br)
		{
			if (!Global.Emulator.HasVideoProvider())
			{
				return;
			}

			try
			{
				using (new SimpleTime("Load Framebuffer"))
				{
					QuickBmpFile.Load(Global.Emulator.AsVideoProvider(), br.BaseStream);
				}
			}
			catch
			{
				var buff = Global.Emulator.AsVideoProvider().GetVideoBuffer();
				try
				{
					for (int i = 0; i < buff.Length; i++)
					{
						int j = br.ReadInt32();
						buff[i] = j;
					}
				}
				catch (EndOfStreamException)
				{
				}
			}
		}
	}
}
