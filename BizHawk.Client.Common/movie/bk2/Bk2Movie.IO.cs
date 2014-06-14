using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
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

		public bool Load()
		{
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}

			using (BinaryStateLoader bl = BinaryStateLoader.LoadAndDetect(Filename))
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
							var pair = line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
							Header.Add(pair[0], pair[1]);
						}
					}
				});

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

				bl.GetLump(BinaryStateLump.Input, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (line != null && line.StartsWith("|"))
						{
							_log.AppendFrame(line);
						}
					}
				});

				// Movies 2.0 TODO: be smart about text or binary state
				if (StartsFromSavestate)
				{
					bl.GetLump(BinaryStateLump.CorestateText, true, delegate(TextReader tr)
					{
						string line;
						while ((line = tr.ReadLine()) != null)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								SavestateBinaryBase64Blob = line;
							}
						}
					});
				}
			}

			return true;
		}

		public bool PreLoadText(HawkFile hawkFile)
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

		private void Write(string fn)
		{
			// Movies 2.0 TODO: Save and Load Movie version
			// there's a lot of common code here with SavestateManager.  refactor?
			using (FileStream fs = new FileStream(Filename, FileMode.Create, FileAccess.Write))
			using (BinaryStateSaver bs = new BinaryStateSaver(fs))
			{
				bs.PutLump(BinaryStateLump.Movieheader, (tw) => tw.WriteLine(Header.ToString()));
				bs.PutLump(BinaryStateLump.Comments, (tw) => tw.WriteLine(CommentsString()));
				bs.PutLump(BinaryStateLump.Subtitles, (tw) => tw.WriteLine(Subtitles.ToString()));
				bs.PutLump(BinaryStateLump.SyncSettings, (tw) => tw.WriteLine(_syncSettingsJson));

				bs.PutLump(BinaryStateLump.Input, (tw) => tw.WriteLine(RawInputLog()));

				if (StartsFromSavestate)
				{
					bs.PutLump(BinaryStateLump.CorestateText, (tw) => tw.WriteLine(SavestateBinaryBase64Blob));
				}
			}

			Changes = false;
		}

		private void ClearBeforeLoad()
		{
			Header.Clear();
			_log.Clear();
			Subtitles.Clear();
			Comments.Clear();
			_syncSettingsJson = string.Empty;
			_savestateBlob = string.Empty;
		}
	}
}
