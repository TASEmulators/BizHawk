#nullable enable

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Bizware.Graphics;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		public FileWriteResult Save()
		{
			return Write(Filename);
		}

		public FileWriteResult SaveBackup()
		{
			if (string.IsNullOrWhiteSpace(Filename))
			{
				return new();
			}

			string backupName = Filename.InsertBeforeLast('.', insert: $".{DateTime.Now:yyyy-MM-dd HH.mm.ss}", out _);
			backupName = Path.Combine(Session.BackupDirectory, Path.GetFileName(backupName));

			return Write(backupName, isBackup: true);
		}

		protected virtual FileWriteResult Write(string fn, bool isBackup = false)
		{
			SetCycleValues();
			// EmulatorVersion used to store the unchanging original emulator version.
			if (!Header.ContainsKey(HeaderKeys.OriginalEmulatorVersion))
			{
				Header[HeaderKeys.OriginalEmulatorVersion] = Header[HeaderKeys.EmulatorVersion];
			}
			Header[HeaderKeys.EmulatorVersion] = VersionInfo.GetEmuVersion();
			Directory.CreateDirectory(Path.GetDirectoryName(fn)!);

			var createResult = ZipStateSaver.Create(fn, Session.Settings.MovieCompressionLevel);
			if (createResult.IsError) return createResult;

			ZipStateSaver saver = createResult.Value!;
			try
			{
				AddLumps(saver, isBackup);
			}
			catch (Exception ex)
			{
				saver.Abort();
				return new(FileWriteEnum.FailedDuringWrite, createResult.Paths, ex);
			}

			FileWriteResult result = saver.CloseAndDispose();
			if (!isBackup && !result.IsError)
			{
				Changes = false;
			}

			return result;
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
					bs.PutLump(
						BinaryStateLump.Framebuffer,
						s => QuickBmpFile.Save(new BitmapBufferVideoProvider(SavestateFramebuffer), s, SavestateFramebuffer.Width, SavestateFramebuffer.Height),
						zstdCompress: false);
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
				string? line;
				while ((line = tr.ReadLine()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						_syncSettingsJson = line;
						break;
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
						if (bl.Version < 3)
						{
							var fb = MemoryMarshal.Cast<byte, int>(br.ReadAllBytes());
							// width and height are unknown, so just use dummy values
							SavestateFramebuffer = new BitmapBuffer(fb.Length / 4, 1, fb.ToArray());
						}
						else
						{
							QuickBmpFile.LoadAuto(br.BaseStream, out var bmp);
							SavestateFramebuffer = new BitmapBuffer(bmp.BufferWidth, bmp.BufferHeight, bmp.GetVideoBuffer());
						}
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
