using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class SavestateManager
	{
		public static void SaveStateFile(string filename, string name)
		{
			var core = Global.Emulator as IStatable;
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

				if (Global.Config.SaveScreenshotWithStates)
				{
					var buff = Global.Emulator.VideoProvider.GetVideoBuffer();

					// If user wants large screenshots, or screenshot is small enough
					if (Global.Config.SaveLargeScreenshotWithStates || buff.Length < Global.Config.BigScreenshotSize)
					{
						using (new SimpleTime("Save Framebuffer"))
							bs.PutLump(BinaryStateLump.Framebuffer, DumpFramebuffer);
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
			}
		}

		public static void PopulateFramebuffer(BinaryReader br)
		{
			var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
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

		public static void DumpFramebuffer(BinaryWriter bw)
		{
			bw.Write(Global.Emulator.VideoProvider.GetVideoBuffer());
		}

		public static bool LoadStateFile(string path, string name)
		{
			var core = Global.Emulator as IStatable;

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
							if (args[0] == "Framebuffer")
							{
								Global.Emulator.VideoProvider.GetVideoBuffer().ReadFromHex(args[1]);
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
