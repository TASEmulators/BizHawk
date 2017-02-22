using System;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public static class SavestateManager
	{
		public static void SaveStateFile(string filename, string name)
		{
			var core = Global.Emulator.AsStatable();
			// the old method of text savestate save is now gone.
			// a text savestate is just like a binary savestate, but with a different core lump
			using (var bs = new BinaryStateSaver(filename))
			{
				if (Global.Config.SaveStateType == Config.SaveStateTypeE.Text ||
					(Global.Config.SaveStateType == Config.SaveStateTypeE.Default && !core.BinarySaveStatesPreferred))
				{
					// text savestate format
					using (new SimpleTime("Save Core"))
						bs.PutLump(BinaryStateLump.CorestateText, (tw) => core.SaveStateText(tw));
				}
				else
				{
					// binary core lump format
					using (new SimpleTime("Save Core"))
						bs.PutLump(BinaryStateLump.Corestate, bw => core.SaveStateBinary(bw));
				}

				if (Global.Config.SaveScreenshotWithStates && Global.Emulator.HasVideoProvider())
				{
					var vp = Global.Emulator.AsVideoProvider();
					var buff = vp.GetVideoBuffer();
					if (buff.Length == 1)
					{
						//is a hacky opengl texture ID. can't handle this now!
						//need to discuss options
						//1. cores must be able to provide a pixels videoprovider in addition to a texture ID, on command (not very hard overall but interface changing and work per core)
						//2. SavestateManager must be setup with a mechanism for resolving texture IDs (even less work, but sloppy)
						//There are additional problems with AVWriting. They depend on VideoProvider providing pixels.
					}
					else
					{
						int out_w = vp.BufferWidth;
						int out_h = vp.BufferHeight;

						// if buffer is too big, scale down screenshot
						if (!Global.Config.NoLowResLargeScreenshotWithStates && buff.Length >= Global.Config.BigScreenshotSize)
						{
							out_w /= 2;
							out_h /= 2;
						}
						using (new SimpleTime("Save Framebuffer"))
							bs.PutLump(BinaryStateLump.Framebuffer, (s) => QuickBmpFile.Save(Global.Emulator.AsVideoProvider(), s, out_w, out_h));
					}
				}

				if (Global.MovieSession.Movie.IsActive)
				{
					bs.PutLump(BinaryStateLump.Input,
						delegate(TextWriter tw)
						{
							// this never should have been a core's responsibility
							tw.WriteLine("Frame {0}", Global.Emulator.Frame);
							Global.MovieSession.HandleMovieSaveState(tw);
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

				if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie is TasMovie)
				{
					bs.PutLump(BinaryStateLump.LagLog,
						delegate(BinaryWriter bw)
						{
							(Global.MovieSession.Movie as TasMovie).TasLagLog.Save(bw);
						});
				}
			}
		}

		public static void PopulateFramebuffer(BinaryReader br)
		{
			if (!Global.Emulator.HasVideoProvider())
			{
				return;
			}

			try
			{
				using (new SimpleTime("Load Framebuffer"))
					QuickBmpFile.Load(Global.Emulator.AsVideoProvider(), br.BaseStream);
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
				catch (EndOfStreamException) { }
			}
		}

		public static void PopulateFramebuffer(byte[] bytes)
		{
			using (var ms = new MemoryStream(bytes))
			{
				using (var br = new BinaryReader(ms))
				{
					PopulateFramebuffer(br);
				}
			}
		}

		public static bool LoadStateFile(string path, string name)
		{
			var core = Global.Emulator.AsStatable();

			// try to detect binary first
			var bl = BinaryStateLoader.LoadAndDetect(path);
			if (bl != null)
			{
				try
				{
					var succeed = false;

					if (Global.MovieSession.Movie.IsActive)
					{
						bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleMovieLoadState_HackyStep1(tr));
						if (!succeed)
						{
							return false;
						}

						bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleMovieLoadState_HackyStep2(tr));
						if (!succeed)
						{
							return false;
						}
					}

					using (new SimpleTime("Load Core"))
						bl.GetCoreState(br => core.LoadStateBinary(br), tr => core.LoadStateText(tr));

					bl.GetLump(BinaryStateLump.Framebuffer, false, PopulateFramebuffer);

					if (bl.HasLump(BinaryStateLump.UserData))
					{
						string userData = string.Empty;
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

						Global.UserBag = (Dictionary<string, object>)ConfigService.LoadWithType(userData);
					}

					if (bl.HasLump(BinaryStateLump.LagLog)
						&& Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie is TasMovie)
					{
						bl.GetLump(BinaryStateLump.LagLog, false, delegate(BinaryReader br, long length)
						{
							(Global.MovieSession.Movie as TasMovie).TasLagLog.Load(br);
						});
					}
				}
				catch
				{
					return false;
				}
				finally
				{
					bl.Dispose();
				}

				return true;
			}
			else // text mode
			{
				if (Global.MovieSession.HandleMovieLoadState(path))
				{
					using (var reader = new StreamReader(path))
					{
						core.LoadStateText(reader);

						while (true)
						{
							var str = reader.ReadLine();
							if (str == null)
							{
								break;
							}
							
							if (str.Trim() == string.Empty)
							{
								continue;
							}

							var args = str.Split(' ');
							if (args[0] == "Framebuffer" && Global.Emulator.HasVideoProvider())
							{
								Global.Emulator.AsVideoProvider().GetVideoBuffer().ReadFromHex(args[1]);
							}
						}
					}

					return true;
				}
				else
				{
					return false;
				}
			}
		}
	}
}
