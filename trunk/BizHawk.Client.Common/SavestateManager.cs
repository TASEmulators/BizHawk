using System;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class SavestateManager
	{
		public static void SaveStateFile(string filename, string name)
		{
			if (Global.Config.SaveStateType == Config.SaveStateTypeE.Text ||
				(Global.Config.SaveStateType == Config.SaveStateTypeE.Default && !Global.Emulator.BinarySaveStatesPreferred))
			{
				// text mode savestates
				var writer = new StreamWriter(filename);
				Global.Emulator.SaveStateText(writer);
				Global.MovieSession.HandleMovieSaveState(writer);
				if (Global.Config.SaveScreenshotWithStates)
				{
					writer.Write("Framebuffer ");
					Global.Emulator.VideoProvider.GetVideoBuffer().SaveAsHex(writer);
				}
				writer.Close();
			}
			else
			{
				// binary savestates
				using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
				using (BinaryStateSaver bs = new BinaryStateSaver(fs))
				{
#if true
					bs.PutCoreStateBinary(
						delegate(Stream s)
						{
							BinaryWriter bw = new BinaryWriter(s);
							Global.Emulator.SaveStateBinary(bw);
							bw.Flush();
						});
#else
					// this would put text states inside the zipfile
					bs.PutCoreStateText(
						delegate(Stream s)
						{
							StreamWriter sw = new StreamWriter(s);
							Global.Emulator.SaveStateText(sw);
							sw.Flush();
						});
#endif
					if (Global.Config.SaveScreenshotWithStates)
					{
						bs.PutFrameBuffer(
							delegate(Stream s)
							{
								var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
								BinaryWriter bw = new BinaryWriter(s);
								bw.Write(buff);
								bw.Flush();
							});
					}
					if (Global.MovieSession.Movie.IsActive)
					{
						bs.PutInputLog(
							delegate(Stream s)
							{
								StreamWriter sw = new StreamWriter(s);
								// this never should have been a core's responsibility
								sw.WriteLine("Frame {0}", Global.Emulator.Frame);
								Global.MovieSession.HandleMovieSaveState(sw);
								sw.Flush();
							});
					}
				}
			}
		}

		public static bool LoadStateFile(string path, string name)
		{
			// try to detect binary first
			BinaryStateLoader bw = BinaryStateLoader.LoadAndDetect(path);
			if (bw != null)
			{
				try
				{
					bool succeed = false;

					if (Global.MovieSession.Movie.IsActive)
					{
						bw.GetInputLogRequired(
							delegate(Stream s)
							{
								StreamReader sr = new StreamReader(s);
								succeed = Global.MovieSession.HandleMovieLoadState(sr);
							});
						if (!succeed)
						{
							return false;
						}
					}

					bw.GetCoreState(
						delegate(Stream s)
						{
							BinaryReader br = new BinaryReader(s);
							Global.Emulator.LoadStateBinary(br);
						},
						delegate(Stream s)
						{
							StreamReader sr = new StreamReader(s);
							Global.Emulator.LoadStateText(sr);
						});

					bw.GetFrameBuffer(
						delegate(Stream s)
						{
							BinaryReader br = new BinaryReader(s);
							int i;
							var buff = Global.Emulator.VideoProvider.GetVideoBuffer();
							try
							{
								for (i = 0; i < buff.Length; i++)
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
					bw.Dispose();
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
							string str = reader.ReadLine();
							if (str == null) break;
							if (str.Trim() == "") continue;

							string[] args = str.Split(' ');
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
