using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class SubtitleList : List<Subtitle>
	{
		public IEnumerable<Subtitle> GetSubtitles(int frame)
		{
			return this.Where(t => frame >= t.Frame && frame <= t.Frame + t.Duration);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			ForEach(subtitle => sb.AppendLine(subtitle.ToString()));
			return sb.ToString();
		}

		public bool AddFromString(string subtitleStr)
		{
			if (!String.IsNullOrWhiteSpace(subtitleStr))
			{
				try
				{
					var subparts = subtitleStr.Split(' ');

					// Unfortunately I made the file format space delminated so this hack is necessary to get the message
					var message = String.Empty;
					for (var i = 6; i < subparts.Length; i++)
					{
						message += subparts[i] + ' ';
					}

					Add(new Subtitle 
					{
						Frame = int.Parse(subparts[1]),
						X = int.Parse(subparts[2]),
						Y = int.Parse(subparts[3]),
						Duration = int.Parse(subparts[4]),
						Color = uint.Parse(subparts[5], NumberStyles.HexNumber),
						Message = message
					});

					return true;
				}
				catch
				{
					return false;
				}
			}
			else
			{
				return false;
			}
		}
	}
}