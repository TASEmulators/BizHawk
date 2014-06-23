using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common.MovieConversionExtensions
{
	public static class MovieConversionExtensions
	{
		public static Bk2Movie ToBk2(this BkmMovie bkm)
		{
			var newFilename = bkm.Filename + "." + Bk2Movie.Extension;
			var bk2 = new Bk2Movie(newFilename);
			bk2.HeaderEntries.Clear();
			foreach(var kvp in bkm.HeaderEntries)
			{
				bk2.HeaderEntries[kvp.Key] = kvp.Value;
			}

			bk2.SyncSettingsJson = bkm.SyncSettingsJson;

			bk2.Comments.Clear();
			foreach(var comment in bkm.Comments)
			{
				bk2.Comments.Add(comment);
			}

			bk2.Subtitles.Clear();
			foreach(var sub in bkm.Subtitles)
			{
				bk2.Subtitles.Add(sub);
			}

			bk2.TextSavestate = bkm.TextSavestate;
			bk2.BinarySavestate = bkm.BinarySavestate;

			for (int i = 0; i < bkm.InputLogLength; i++)
			{
				var input = bkm.GetInputState(i);
				bk2.AppendFrame(input);
			}

			return bk2;
		}
	}
}
