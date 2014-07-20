using System;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Client.Common
{
	public static class SavestateManager
	{
		public static void SaveStateFile(string filename, string name)
		{
			// the old method of text savestate save is now gone.
			// a text savestate is just like a binary savestate, but with a different core lump
			using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
			using (var bs = new BinaryStateSaver(fs))
			{
				if (Global.Config.SaveStateType == Config.SaveStateTypeE.Text ||
					(Global.Config.SaveStateType == Config.SaveStateTypeE.Default && !Global.Emulator.BinarySaveStatesPreferred))
				{
					// text savestate format
					bs.PutLump(BinaryStateLump.CorestateText, (tw) => Global.Emulator.SaveStateText(tw));
				}
				else
				{
					// binary core lump format
					bs.PutLump(BinaryStateLump.Corestate, bw => Global.Emulator.SaveStateBinary(bw));
				}

				if (Global.Config.SaveScreenshotWithStates)
				{
					var buff = Global.Emulator.VideoProvider.GetVideoBuffer();

					// If user wants large screenshots, or screenshot is small enough
					if (Global.Config.SaveLargeScreenshotWithStates || buff.Length < Global.Config.BigScreenshotSize)
					{
						bs.PutLump(BinaryStateLump.Framebuffer, (BinaryWriter bw) => bw.Write(buff));
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

		public static bool LoadStateFile(string path, string name)
		{
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
						bl.GetLump(BinaryStateLump.Input, true, tr => succeed = Global.MovieSession.HandleMovieLoadState_HackyStep2(tr));
						if (!succeed)
						{
							return false;
						}
					}

					bl.GetCoreState(br => Global.Emulator.LoadStateBinary(br), tr => Global.Emulator.LoadStateText(tr));

					bl.GetLump(BinaryStateLump.Framebuffer, false, 
						delegate(BinaryReader br)
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
						});
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
						Global.Emulator.LoadStateText(reader);

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
