using System.Globalization;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Client.Common
{
	public static class MovieConversionExtensions
	{
		public static ITasMovie ToTasMovie(this IMovie old)
		{
			string newFilename = ConvertFileNameToTasMovie(old.Filename);
			var tas = (ITasMovie)old.Session.Get(newFilename);
			tas.CopyLog(old.GetLogEntries());
			tas.LogKey = old.LogKey;

			old.Truncate(0); // Trying to minimize ram usage

			tas.HeaderEntries.Clear();
			foreach (var (k, v) in old.HeaderEntries) tas.HeaderEntries[k] = v;

			// TODO: we have this version number string generated in multiple places
			tas.HeaderEntries[HeaderKeys.MovieVersion] = $"BizHawk v2.0 Tasproj v{TasMovie.CurrentVersion.ToString(NumberFormatInfo.InvariantInfo)}";

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

			tas.StartsFromSavestate = old.StartsFromSavestate;
			tas.TextSavestate = old.TextSavestate;
			tas.BinarySavestate = old.BinarySavestate;
			tas.SaveRam = old.SaveRam;

			return tas;
		}

		public static IMovie ToBk2(this IMovie old)
		{
			var bk2 = old.Session.Get(old.Filename.Replace(old.PreferredExtension, Bk2Movie.Extension));
			bk2.CopyLog(old.GetLogEntries());
			bk2.LogKey = old.LogKey;

			bk2.HeaderEntries.Clear();
			foreach (var (k, v) in old.HeaderEntries) bk2.HeaderEntries[k] = v;

			// TODO: we have this version number string generated in multiple places
			bk2.HeaderEntries[HeaderKeys.MovieVersion] = "BizHawk v2.0";

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

			return bk2;
		}

		public static ITasMovie ConvertToSavestateAnchoredMovie(this ITasMovie old, int frame, byte[] savestate)
		{
			string newFilename = ConvertFileNameToTasMovie(old.Filename);

			var tas = (ITasMovie)old.Session.Get(newFilename);
			tas.BinarySavestate = savestate;
			tas.LagLog.Clear();

			var entries = old.GetLogEntries();

			tas.CopyLog(entries.Skip(frame));
			tas.LogKey = old.LogKey;
			tas.CopyVerificationLog(old.VerificationLog);
			tas.CopyVerificationLog(entries.Take(frame));

			// States can't be easily moved over, because they contain the frame number.
			// TODO? I'm not sure how this would be done.

			// Lag Log
			tas.LagLog.FromLagLog(old.LagLog);
			tas.LagLog.StartFromFrame(frame);

			tas.HeaderEntries.Clear();
			foreach (var (k, v) in old.HeaderEntries) tas.HeaderEntries[k] = v;

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
				{
					tas.Markers.Add(new TasMovieMarker(marker.Frame - frame, marker.Message));
				}
			}

			tas.Save();
			return tas;
		}

		public static ITasMovie ConvertToSaveRamAnchoredMovie(this ITasMovie old, byte[] saveRam)
		{
			string newFilename = ConvertFileNameToTasMovie(old.Filename);

			var tas = (ITasMovie)old.Session.Get(newFilename);
			tas.SaveRam = saveRam;
			tas.LagLog.Clear();

			var entries = old.GetLogEntries();

			tas.CopyVerificationLog(old.VerificationLog);
			tas.CopyVerificationLog(entries);

			tas.HeaderEntries.Clear();
			foreach (var (k, v) in old.HeaderEntries) tas.HeaderEntries[k] = v;

			tas.StartsFromSaveRam = true;
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

			tas.Save();
			return tas;
		}

		internal static string ConvertFileNameToTasMovie(string oldFileName)
		{
			if (oldFileName is null) return null;
			var (dir, fileNoExt, _) = oldFileName.SplitPathToDirFileAndExt();
			if (dir is null) return string.Empty;
			var newFileName = Path.Combine(dir, $"{fileNoExt}.{TasMovie.Extension}");
			int fileSuffix = 0;
			while (File.Exists(newFileName))
			{
				// Using this should hopefully be system agnostic
				newFileName = Path.Combine(dir, $"{fileNoExt} {++fileSuffix}.{TasMovie.Extension}");
			}

			return newFileName;
		}
	}
}
