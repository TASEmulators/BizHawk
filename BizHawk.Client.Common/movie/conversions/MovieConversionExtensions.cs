using System;
using System.IO;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

namespace BizHawk.Client.Common.MovieConversionExtensions
{
	public static class MovieConversionExtensions
	{
		public static TasMovie ToTasMovie(this IMovie old, bool copy = false)
		{
			string newFilename = old.Filename + "." + TasMovie.Extension;

			if (File.Exists(newFilename))
			{
				int fileNum = 1;
				bool fileConflict = true;
				while (fileConflict)
				{
					if (File.Exists(newFilename))
					{
						newFilename = old.Filename + " (" + fileNum + ")" + "." + TasMovie.Extension;
						fileNum++;
					}
					else
					{
						fileConflict = false;
					}
				}
			}

			var tas = new TasMovie(newFilename, old.StartsFromSavestate);
			tas.TasStateManager.MountWriteAccess();

			for (var i = 0; i < old.InputLogLength; i++)
			{
				var input = old.GetInputState(i);
				tas.AppendFrame(input);
			}

			if (!copy)
			{
				old.Truncate(0); // Trying to minimize ram usage
			}

			tas.HeaderEntries.Clear();
			foreach (var kvp in old.HeaderEntries)
			{
				tas.HeaderEntries[kvp.Key] = kvp.Value;
			}

			tas.SyncSettingsJson = old.SyncSettingsJson;

			tas.Comments.Clear();
			foreach (var comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

			tas.Subtitles.Clear();
			foreach (var sub in old.Subtitles)
			{
				tas.Subtitles.Add(sub);
			}

			tas.TextSavestate = old.TextSavestate;
			tas.BinarySavestate = old.BinarySavestate;
			tas.SaveRam = old.SaveRam;

			return tas;
		}

		public static Bk2Movie ToBk2(this IMovie old, bool copy = false, bool backup = false)
		{
			var bk2 = new Bk2Movie(old.Filename.Replace(old.PreferredExtension, Bk2Movie.Extension));

			for (var i = 0; i < old.InputLogLength; i++)
			{
				var input = old.GetInputState(i);
				bk2.AppendFrame(input);
			}

			if (!copy)
			{
				old.Truncate(0); // Trying to minimize ram usage
			}

			bk2.HeaderEntries.Clear();
			foreach (var kvp in old.HeaderEntries)
			{
				bk2.HeaderEntries[kvp.Key] = kvp.Value;
			}

			bk2.SyncSettingsJson = old.SyncSettingsJson;

			bk2.Comments.Clear();
			foreach (var comment in old.Comments)
			{
				bk2.Comments.Add(comment);
			}

			bk2.Subtitles.Clear();
			foreach (var sub in old.Subtitles)
			{
				bk2.Subtitles.Add(sub);
			}

			bk2.TextSavestate = old.TextSavestate;
			bk2.BinarySavestate = old.BinarySavestate;
			bk2.SaveRam = old.SaveRam;

			if (!backup)
				bk2.Save();

			return bk2;
		}

		public static TasMovie ConvertToSavestateAnchoredMovie(this TasMovie old, int frame, byte[] savestate)
		{
			string newFilename = old.Filename + "." + TasMovie.Extension;

			if (File.Exists(newFilename))
			{
				int fileNum = 1;
				bool fileConflict = true;
				while (fileConflict)
				{
					if (File.Exists(newFilename))
					{
						newFilename = old.Filename + " (" + fileNum + ")" + "." + TasMovie.Extension;
						fileNum++;
					}
					else
					{
						fileConflict = false;
					}
				}
			}

			TasMovie tas = new TasMovie(newFilename, true);
			tas.BinarySavestate = savestate;
			tas.ClearLagLog();

			var entries = old.GetLogEntries();

			tas.CopyLog(entries.Skip(frame));
			tas.CopyVerificationLog(old.VerificationLog);
			tas.CopyVerificationLog(entries.Take(frame));

			// States can't be easily moved over, because they contain the frame number.
			// TODO? I'm not sure how this would be done.
			tas.TasStateManager.MountWriteAccess();
			old.TasStateManager.Clear();

			// Lag Log
			tas.TasLagLog.FromLagLog(old.TasLagLog);
			tas.TasLagLog.StartFromFrame(frame);

			tas.HeaderEntries.Clear();
			foreach (var kvp in old.HeaderEntries)
			{
				tas.HeaderEntries[kvp.Key] = kvp.Value;
			}

			tas.StartsFromSavestate = true;
			tas.SyncSettingsJson = old.SyncSettingsJson;

			tas.Comments.Clear();
			foreach (string comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

			tas.Subtitles.Clear();
			foreach (Subtitle sub in old.Subtitles)
			{
				tas.Subtitles.Add(sub);
			}

			foreach (TasMovieMarker marker in old.Markers)
			{
				if (marker.Frame > frame)
					tas.Markers.Add(new TasMovieMarker(marker.Frame - frame, marker.Message));
			}

			tas.TasStateManager.Settings = old.TasStateManager.Settings;

			tas.Save();
			return tas;
		}

		public static TasMovie ConvertToSaveRamAnchoredMovie(this TasMovie old, byte[] saveRam)
		{
			string newFilename = old.Filename + "." + TasMovie.Extension;

			if (File.Exists(newFilename))
			{
				int fileNum = 1;
				bool fileConflict = true;
				while (fileConflict)
				{
					if (File.Exists(newFilename))
					{
						newFilename = old.Filename + " (" + fileNum + ")" + "." + TasMovie.Extension;
						fileNum++;
					}
					else
					{
						fileConflict = false;
					}
				}
			}

			TasMovie tas = new TasMovie(newFilename, true);
			tas.SaveRam = saveRam;
			tas.TasStateManager.Clear();
			tas.ClearLagLog();

			var entries = old.GetLogEntries();

			tas.CopyVerificationLog(old.VerificationLog);
			tas.CopyVerificationLog(entries);

			tas.HeaderEntries.Clear();
			foreach (var kvp in old.HeaderEntries)
			{
				tas.HeaderEntries[kvp.Key] = kvp.Value;
			}

			tas.StartsFromSaveRam = true;
			tas.StartsFromSavestate = false;
			tas.SyncSettingsJson = old.SyncSettingsJson;

			tas.Comments.Clear();
			foreach (string comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

			tas.Subtitles.Clear();
			foreach (Subtitle sub in old.Subtitles)
			{
				tas.Subtitles.Add(sub);
			}

			tas.TasStateManager.Settings = old.TasStateManager.Settings;

			tas.Save();
			return tas;
		}

		// TODO: This doesn't really belong here, but not sure where to put it
		public static void PopulateWithDefaultHeaderValues(this IMovie movie, string author = null)
		{
			movie.Author = author ?? Global.Config.DefaultAuthor;
			movie.EmulatorVersion = VersionInfo.GetEmuVersion();
			movie.SystemID = Global.Emulator.SystemId;

			var settable = new SettingsAdapter(Global.Emulator);
			if (settable.HasSyncSettings)
			{
				movie.SyncSettingsJson = ConfigService.SaveWithType(settable.GetSyncSettings());
			}

			if (Global.Game != null)
			{
				movie.GameName = PathManager.FilesystemSafeName(Global.Game);
				movie.Hash = Global.Game.Hash;
				if (Global.Game.FirmwareHash != null)
				{
					movie.FirmwareHash = Global.Game.FirmwareHash;
				}
			}
			else
			{
				movie.GameName = "NULL";
			}

			if (Global.Emulator.BoardName != null)
			{
				movie.BoardName = Global.Emulator.BoardName;
			}

			if (Global.Emulator.HasRegions())
			{
				var region = Global.Emulator.AsRegionable().Region;
				if (region == Emulation.Common.DisplayType.PAL)
				{
					movie.HeaderEntries.Add(HeaderKeys.PAL, "1");
				}
			}

			if (Global.FirmwareManager.RecentlyServed.Any())
			{
				foreach (var firmware in Global.FirmwareManager.RecentlyServed)
				{
					var key = firmware.SystemId + "_Firmware_" + firmware.FirmwareId;

					if (!movie.HeaderEntries.ContainsKey(key))
					{
						movie.HeaderEntries.Add(key, firmware.Hash);
					}
				}

			}

			if (Global.Emulator is Gameboy && (Global.Emulator as Gameboy).IsCGBMode())
			{
				movie.HeaderEntries.Add("IsCGBMode", "1");
			}

			if (Global.Emulator is SMS && (Global.Emulator as SMS).IsSG1000)
			{
				movie.HeaderEntries.Add("IsSGMode", "1");
			}

			if (Global.Emulator is GPGX && (Global.Emulator as GPGX).IsSegaCD)
			{
				movie.HeaderEntries.Add("IsSegaCDMode", "1");
			}

			movie.Core = ((CoreAttributes)Attribute
				.GetCustomAttribute(Global.Emulator.GetType(), typeof(CoreAttributes)))
				.CoreName;
		}
	}
}
