using System;
using System.IO;

using BizHawk.Common;

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

			var backupName = Filename;
			backupName = backupName.Insert(Filename.LastIndexOf("."), string.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			backupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(backupName));

			var directory_info = new FileInfo(backupName).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			Write(backupName);
		}

		public virtual bool Load()
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
								Header.Add(pair[0], pair[1]);
							}
						}
					}
				});

				if (bl.HasLump(BinaryStateLump.Comments))
				{
					bl.GetLump(BinaryStateLump.Comments, true, delegate(TextReader tr)
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
				}

				if (bl.HasLump(BinaryStateLump.Subtitles))
				{
					bl.GetLump(BinaryStateLump.Subtitles, true, delegate(TextReader tr)
					{
						string line;
						while ((line = tr.ReadLine()) != null)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								Subtitles.AddFromString(line);
							}
						}
					});
				}

				if (bl.HasLump(BinaryStateLump.SyncSettings))
				{
					bl.GetLump(BinaryStateLump.SyncSettings, true, delegate(TextReader tr)
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
				}

				bl.GetLump(BinaryStateLump.Input, true, delegate(TextReader tr)
				{
					var errorMessage = string.Empty;
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
								SavestateFramebuffer[i] = br.ReadInt32();
						});
				}
			}

			Changes = false;
			return true;
		}

		public bool PreLoadHeaderAndLength(HawkFile hawkFile)
		{
			// For now, preload simply loads everything
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}

			Filename = file.FullName;
			return Load();
		}

		protected virtual void Write(string fn)
		{
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

				bs.PutLump(BinaryStateLump.Input, tw => tw.WriteLine(RawInputLog()));

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
						bs.PutLump(BinaryStateLump.Framebuffer,
							(BinaryWriter bw) => BizHawk.Common.IOExtensions.IOExtensions.Write(bw, SavestateFramebuffer));
					}
				}
			}

			Changes = false;
		}

		protected void ClearBeforeLoad()
		{
			Header.Clear();
			_log.Clear();
			Subtitles.Clear();
			Comments.Clear();
			_syncSettingsJson = string.Empty;
			TextSavestate = null;
			BinarySavestate = null;
		}
	}
}
