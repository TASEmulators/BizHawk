using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		public void Save()
		{
			Write(Filename);
		}

		public void SaveBackup()
		{
			if (string.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			var backupDir = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesBackupsPathFragment, null);
			var backupName = Filename;
			backupName = backupName.Insert(Filename.LastIndexOf("."), $".{DateTime.Now:yyyy-MM-dd HH.mm.ss}");
			backupName = Path.Combine(backupDir, Path.GetFileName(backupName));

			var directoryInfo = new FileInfo(backupName).Directory;
			if (directoryInfo != null)
			{
				Directory.CreateDirectory(directoryInfo.FullName);
			}

			Write(backupName, backup: true);
		}

		public virtual bool Load(bool preload)
		{
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}

			using (var bl = BinaryStateLoader.LoadAndDetect(Filename, true))
			{
				if (bl == null)
				{
					return false;
				}

				ClearBeforeLoad();

				bl.GetLump(BinaryStateLump.Movieheader, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							var pair = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

							if (pair.Length > 1)
							{
								if (!Header.ContainsKey(pair[0]))
								{
									Header.Add(pair[0], pair[1]);
								}
							}
						}
					}
				});

				bl.GetLump(BinaryStateLump.Comments, false, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							Comments.Add(line);
						}
					}
				});

				bl.GetLump(BinaryStateLump.Subtitles, false, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							Subtitles.AddFromString(line);
						}
					}

					Subtitles.Sort();
				});

				bl.GetLump(BinaryStateLump.SyncSettings, false, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							_syncSettingsJson = line;
						}
					}
				});

				bl.GetLump(BinaryStateLump.Input, true, delegate(TextReader tr)
				{
					string errorMessage;
					IsCountingRerecords = false;
					ExtractInputLog(tr, out errorMessage);
					IsCountingRerecords = true;
				});

				if (StartsFromSavestate)
				{
					bl.GetCoreState(
						delegate(BinaryReader br, long length)
						{
							BinarySavestate = br.ReadBytes((int)length);
						},
						delegate(TextReader tr)
						{
							TextSavestate = tr.ReadToEnd();
						});
					bl.GetLump(BinaryStateLump.Framebuffer, false,
						delegate(BinaryReader br, long length)
						{
							SavestateFramebuffer = new int[length / sizeof(int)];
							for (int i = 0; i < SavestateFramebuffer.Length; i++)
							{
								SavestateFramebuffer[i] = br.ReadInt32();
							}
						});
				}
				else if (StartsFromSaveRam)
				{
					bl.GetLump(BinaryStateLump.MovieSaveRam, false,
						delegate(BinaryReader br, long length)
						{
							SaveRam = br.ReadBytes((int)length);
						});
				}
			}

			Changes = false;
			return true;
		}

		public bool PreLoadHeaderAndLength(HawkFile hawkFile)
		{
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}

			Filename = file.FullName;
			return Load(true);
		}

		protected virtual void Write(string fn, bool backup = false)
		{
			if (Global.Emulator is Emulation.Cores.Nintendo.SubNESHawk.SubNESHawk)
			{
				var _subnes = (Emulation.Cores.Nintendo.SubNESHawk.SubNESHawk)Global.Emulator;
				Header["VBlankCount"] = _subnes.VBL_CNT.ToString();
			}
			else if (Global.Emulator is Emulation.Cores.Nintendo.Gameboy.Gameboy)
			{
				var _gameboy = (Emulation.Cores.Nintendo.Gameboy.Gameboy)Global.Emulator;
				Header["CycleCount"] = _gameboy.CycleCount.ToString();
			}

			var file = new FileInfo(fn);
			if (!file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}

			using (var bs = new BinaryStateSaver(fn, false))
			{
				bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
				bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
				bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
				bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(_syncSettingsJson));

				bs.PutLump(BinaryStateLump.Input, WriteInputLog);

				if (StartsFromSavestate)
				{
					if (TextSavestate != null)
					{
						bs.PutLump(BinaryStateLump.CorestateText, (TextWriter tw) => tw.Write(TextSavestate));
					}
					else
					{
						bs.PutLump(BinaryStateLump.Corestate, (BinaryWriter bw) => bw.Write(BinarySavestate));
					}

					if (SavestateFramebuffer != null)
					{
						bs.PutLump(BinaryStateLump.Framebuffer, (BinaryWriter bw) => bw.Write(SavestateFramebuffer));
					}
				}
				else if (StartsFromSaveRam)
				{
					bs.PutLump(BinaryStateLump.MovieSaveRam, (BinaryWriter bw) => bw.Write(SaveRam));
				}
			}

			if (!backup)
			{
				Changes = false;
			}
		}

		protected void ClearBeforeLoad()
		{
			Header.Clear();
			Log.Clear();
			Subtitles.Clear();
			Comments.Clear();
			_syncSettingsJson = "";
			TextSavestate = null;
			BinarySavestate = null;
		}
	}
}
