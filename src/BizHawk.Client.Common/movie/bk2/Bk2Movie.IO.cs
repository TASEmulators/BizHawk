using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;

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
			backupName = backupName.Insert(Filename.LastIndexOf('.'), $".{DateTime.Now:yyyy-MM-dd HH.mm.ss}");
			backupName = Path.Combine(Session.BackupDirectory, Path.GetFileName(backupName));

			Write(backupName, isBackup: true);
		}

		protected virtual void Write(string fn, bool isBackup = false)
		{
			SetCycleValues();
			// EmulatorVersion used to store the unchanging original emulator version.
			if (!Header.ContainsKey(HeaderKeys.OriginalEmulatorVersion))
			{
				Header[HeaderKeys.OriginalEmulatorVersion] = Header[HeaderKeys.EmulatorVersion];
			}
			Header[HeaderKeys.EmulatorVersion] = VersionInfo.GetEmuVersion();
			CreateDirectoryIfNotExists(fn);

			using var bs = new ZipStateSaver(fn, Session.Settings.MovieCompressionLevel);
			AddLumps(bs, isBackup);

			if (!isBackup)
			{
				Changes = false;
			}
		}

		public void SetCycleValues() //TODO IEmulator should not be an instance prop of movies, it should be passed in to every call (i.e. from MovieService) --yoshi
		{
			// The saved cycle value will only be valid if the end of the movie has been emulated.
			if (this.IsAtEnd() && Emulator.AsCycleTiming() is { } cycleCore)
			{
				// legacy movies may incorrectly have no ClockRate header value set
				Header[HeaderKeys.ClockRate] = cycleCore.ClockRate.ToString(NumberFormatInfo.InvariantInfo);
				Header[HeaderKeys.CycleCount] = cycleCore.CycleCount.ToString();
			}
			else
			{
				Header.Remove(HeaderKeys.CycleCount); // don't allow invalid cycle count fields to stay set
			}
		}

		private static void CreateDirectoryIfNotExists(string fn)
		{
			var file = new FileInfo(fn);
			if (file.Directory != null && !file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}
		}

		protected virtual void AddLumps(ZipStateSaver bs, bool isBackup = false)
		{
			AddBk2Lumps(bs);
		}

		protected void AddBk2Lumps(ZipStateSaver bs)
		{
			bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
			bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
			bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
			bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(SyncSettingsJson));
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

		protected override void ClearBeforeLoad()
		{
			base.ClearBeforeLoad();
			ClearBk2Fields();
		}

		private void ClearBk2Fields()
		{
			Log.Clear();
			_syncSettingsJson = "";
			TextSavestate = null;
			BinarySavestate = null;
		}

		protected override void LoadFields(ZipStateLoader bl)
		{
			base.LoadFields(bl);
			LoadBk2Fields(bl);
		}

		private void LoadBk2Fields(ZipStateLoader bl)
		{
			bl.GetLump(BinaryStateLump.Input, abort: true, tr =>
			{
				IsCountingRerecords = false;
				ExtractInputLog(tr, out _);
				IsCountingRerecords = true;
			});

			bl.GetLump(BinaryStateLump.SyncSettings, abort: false, tr =>
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

			if (StartsFromSavestate)
			{
				bl.GetCoreState(
					br => BinarySavestate = br.ReadAllBytes(),
					tr => TextSavestate = tr.ReadToEnd());
				bl.GetLump(BinaryStateLump.Framebuffer, false,
					br =>
					{
						var fb = br.ReadAllBytes();
						SavestateFramebuffer = new int[fb.Length / sizeof(int)];
						Buffer.BlockCopy(fb, 0, SavestateFramebuffer, 0, fb.Length);
					});
			}
			else if (StartsFromSaveRam)
			{
				bl.GetLump(BinaryStateLump.MovieSaveRam, false,
					br => SaveRam = br.ReadAllBytes());
			}
		}
	}
}
