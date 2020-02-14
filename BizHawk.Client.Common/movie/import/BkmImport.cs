using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Client.Common.movie.import
{
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("BizHawk", ".bkm")]
	internal class BkmImport : MovieImporter
	{
		protected override void RunImport()
		{
			var movie = new BkmMovie
			{
				Filename = SourceFile.FullName
			};

			movie.Load();
			Result.Movie = ToBk2(movie);
		}

		public static Bk2Movie ToBk2(BkmMovie old)
		{
			var bk2 = new Bk2Movie(old.Filename.Replace(old.PreferredExtension, Bk2Movie.Extension));

			for (var i = 0; i < old.InputLogLength; i++)
			{
				var input = old.GetInputState(i);
				bk2.AppendFrame(input);
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

			bk2.BinarySavestate = old.BinarySavestate;

			bk2.Save();

			return bk2;
		}
	}
}
