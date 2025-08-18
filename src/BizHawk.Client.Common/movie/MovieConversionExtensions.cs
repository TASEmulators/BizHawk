using System.IO;
using System.Linq;

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

			foreach (var (k, v) in old.HeaderEntries)
			{
				if (k is HeaderKeys.MovieVersion) continue;
				tas.HeaderEntries[k] = v;
			}

			tas.SyncSettingsJson = old.SyncSettingsJson;

			foreach (var comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

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

			foreach (var (k, v) in old.HeaderEntries)
			{
				if (k is HeaderKeys.MovieVersion) continue;
				bk2.HeaderEntries[k] = v;
			}

			bk2.SyncSettingsJson = old.SyncSettingsJson;

			foreach (var comment in old.Comments)
			{
				bk2.Comments.Add(comment);
			}

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

			foreach (var (k, v) in old.HeaderEntries) tas.HeaderEntries[k] = v;

			tas.StartsFromSavestate = true;
			tas.SyncSettingsJson = old.SyncSettingsJson;

			foreach (string comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

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

			var entries = old.GetLogEntries();

			tas.CopyVerificationLog(old.VerificationLog);
			tas.CopyVerificationLog(entries);

			foreach (var (k, v) in old.HeaderEntries) tas.HeaderEntries[k] = v;

			tas.StartsFromSaveRam = true;
			tas.SyncSettingsJson = old.SyncSettingsJson;

			foreach (string comment in old.Comments)
			{
				tas.Comments.Add(comment);
			}

			foreach (Subtitle sub in old.Subtitles)
			{
				tas.Subtitles.Add(sub);
			}

			tas.Save();
			return tas;
		}

#pragma warning disable RCS1224 // private but for unit test
		internal static string ConvertFileNameToTasMovie(string oldFileName)
#pragma warning restore RCS1224
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
