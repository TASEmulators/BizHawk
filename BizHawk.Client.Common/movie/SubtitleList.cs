using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class SubtitleList : List<Subtitle>
	{
		public bool ConcatMultilines { get; set; }
		public bool AddColorTag { get; set; }

		public SubtitleList()
		{
			ConcatMultilines = false;
			AddColorTag = false;
		}

		public IEnumerable<Subtitle> GetSubtitles(int frame) => this.Where(t => t.Frame.RangeTo(t.Frame + t.Duration).Contains(frame));

		public override string ToString()
		{
			var sb = new StringBuilder();
			Sort();
			ForEach(subtitle => sb.AppendLine(subtitle.ToString()));
			return sb.ToString();
		}

		public bool AddFromString(string subtitleStr)
		{
			if (!string.IsNullOrWhiteSpace(subtitleStr))
			{
				try
				{
					var subParts = subtitleStr.Split(' ');

					// Unfortunately I made the file format space delaminated so this hack is necessary to get the message
					var message = "";
					for (var i = 6; i < subParts.Length; i++)
					{
						message += subParts[i] + ' ';
					}

					Add(new Subtitle 
					{
						Frame = int.Parse(subParts[1]),
						X = int.Parse(subParts[2]),
						Y = int.Parse(subParts[3]),
						Duration = int.Parse(subParts[4]),
						Color = uint.Parse(subParts[5], NumberStyles.HexNumber),
						Message = message.Trim()
					});

					return true;
				}
				catch
				{
					return false;
				}
			}
			
			return false;
		}

		public new void Sort()
		{
			Sort((x, y) =>
			{
				int result = x.Frame.CompareTo(y.Frame);
				return result != 0 ? result : x.Y.CompareTo(y.Y);
			});
		}

		public string ToSubRip(double fps)
		{
			int index = 1;
			var sb = new StringBuilder();
			List<Subtitle> subs = new List<Subtitle>();
			foreach (var subtitle in this)
			{
				subs.Add(new Subtitle(subtitle));
			}

			// absence of line wrap forces multiline subtitle macros
			// so we sort them just in case and optionally concat back to a single unit
			// todo: instead of making this pretty, add the line wrap feature to subtitles
			if (ConcatMultilines)
			{
				int lastFrame = 0;
				subs = subs.OrderBy(s => s.Frame).ThenBy(s => s.Y).ToList();

				for (int i = 0;; i++)
				{
					if (i == subs.Count) // we're modifying it
					{
						break;
					}

					subs[i].Message = subs[i].Message.Trim();

					if (i > 0 && lastFrame == subs[i].Frame)
					{
						subs[i].Message = $"{subs[i - 1].Message} {subs[i].Message}";
						subs.Remove(subs[i - 1]);
						i--;
					}

					lastFrame = subs[i].Frame;
				}
			}
			else
			{
				// srt stacks multilines upwards
				subs = subs.OrderBy(s => s.Frame).ThenByDescending(s => s.Y).ToList();
			}

			foreach (var subtitle in subs)
			{
				sb.Append(subtitle.ToSubRip(index++, fps, AddColorTag));
			}

			return sb.ToString();
		}
	}
}