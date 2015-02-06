using System;
using System.IO;
using System.Linq;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

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

			return tas;
		}

		public static Bk2Movie ToBk2(this IMovie old, bool copy = false)
		{
			var newFilename = old.Filename + "." + Bk2Movie.Extension;
			var bk2 = new Bk2Movie(newFilename);

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
			foreach(var kvp in old.HeaderEntries)
			{
				bk2.HeaderEntries[kvp.Key] = kvp.Value;
			}

			bk2.SyncSettingsJson = old.SyncSettingsJson;

			bk2.Comments.Clear();
			foreach(var comment in old.Comments)
			{
				bk2.Comments.Add(comment);
			}

			bk2.Subtitles.Clear();
			foreach(var sub in old.Subtitles)
			{
				bk2.Subtitles.Add(sub);
			}

			bk2.TextSavestate = old.TextSavestate;
			bk2.BinarySavestate = old.BinarySavestate;

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

			var tas = new TasMovie(newFilename, true);
			tas.BinarySavestate = savestate;
			tas.TasStateManager.Clear();
			tas.ClearLagLog();

			var entries = old.GetLogEntries();

			tas.CopyLog(entries.Skip(frame));
			tas.CopyVerificationLog(entries.Take(frame));

			tas.HeaderEntries.Clear();
			foreach (var kvp in old.HeaderEntries)
			{
				tas.HeaderEntries[kvp.Key] = kvp.Value;
			}

			tas.StartsFromSavestate = true;
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

			foreach(var marker in old.Markers)
			{
				if (marker.Frame > 0)
				{
					tas.Markers.Add(marker);
				}
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

			if (Global.Emulator.HasPublicProperty("DisplayType"))
			{
				var region = Global.Emulator.GetPropertyValue("DisplayType");
				if ((DisplayType)region == DisplayType.PAL)
				{
					movie.HeaderEntries.Add(HeaderKeys.PAL, "1");
				}
			}

			if (Global.FirmwareManager.RecentlyServed.Any())
			{
				foreach (var firmware in Global.FirmwareManager.RecentlyServed)
				{
					movie.HeaderEntries.Add(firmware.SystemId + " Firmware " + firmware.FirmwareId, firmware.Hash);
				}
				
			}

			if (Global.Emulator is Gameboy && (Global.Emulator as Gameboy).IsCGBMode())
			{
				movie.HeaderEntries.Add("IsCGBMode", "1");
			}

			movie.Core = ((CoreAttributes)Attribute
				.GetCustomAttribute(Global.Emulator.GetType(), typeof(CoreAttributes)))
				.CoreName;
		}
	}
}
