using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;

namespace BizHawk.Client.Common.movie.import
{
	[ImporterFor("BizHawk", ".bkm")]
	internal class BkmImport : MovieImporter
	{
		protected override void RunImport()
		{
			var bkm = new BkmMovie { Filename = SourceFile.FullName };
			bkm.Load();

			for (var i = 0; i < bkm.InputLogLength; i++)
			{
				var input = bkm.GetInputState(i, bkm.Header[HeaderKeys.Platform]);
				Result.Movie.AppendFrame(input);
			}

			Result.Movie.LogKey = bkm.GenerateLogKey;

			Result.Movie.HeaderEntries.Clear();
			foreach (var (k, v) in bkm.Header) Result.Movie.HeaderEntries[k] = v;

			// migrate some stuff, probably incomplete
			if (Result.Movie.HeaderEntries[HeaderKeys.Core] is "QuickNes") Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.QuickNes;
			if (Result.Movie.HeaderEntries[HeaderKeys.Core] is "EMU7800") Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.A7800Hawk;
			if (Result.Movie.HeaderEntries[HeaderKeys.Platform] is "DGB") Result.Movie.HeaderEntries[HeaderKeys.Platform] = VSystemID.Raw.GBL;

			Result.Movie.SyncSettingsJson = bkm.SyncSettingsJson;

			Result.Movie.Comments.Clear();
			foreach (var comment in bkm.Comments)
			{
				Result.Movie.Comments.Add(comment);
			}

			Result.Movie.Subtitles.Clear();
			foreach (var sub in bkm.Subtitles)
			{
				Result.Movie.Subtitles.Add(sub);
			}

			Result.Movie.BinarySavestate = bkm.BinarySavestate;
		}
	}
}
