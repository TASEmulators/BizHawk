using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Drawing;
using System.IO;

namespace BizHawk.Client.Common
{
	public class SubtitleList : IEnumerable<Subtitle>
	{
		private readonly List<Subtitle> _subtitles = new List<Subtitle>();

		public SubtitleList() { }

		public SubtitleList(IEnumerable<Subtitle> subtitles)
		{
			foreach (var subtitle in subtitles)
			{
				_subtitles.Add(new Subtitle(subtitle)); //TODO: Multiclient.EditSubtitlesForm needs a deep copy here, refactor it so that it doesn't
			}
		}

		public IEnumerator<Subtitle> GetEnumerator()
		{
			return _subtitles.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Subtitle this[int index]
		{
			get
			{
				return _subtitles[index];
			}
		}

		public IEnumerable<Subtitle> GetSubtitles(int frame)
		{
			return _subtitles.Where(t => frame >= t.Frame && frame <= t.Frame + t.Duration);
		}

		public int Count
		{
			get { return _subtitles.Count; }
		}

		/// <summary>
		/// Attempts to parse string for necessary subtitle information, required is a frame and a message, space delminated, the word subtitle assumed to be first
		/// </summary>
		/// <param name="subtitleStr"></param>
		/// <returns></returns>
		public bool AddSubtitle(string subtitleStr)
		{
			if (!String.IsNullOrWhiteSpace(subtitleStr))
			{
				try
				{
					var subparts = subtitleStr.Split(' ');

					//Unfortunately I made the file format space delminated so this hack is necessary to get the message
					string message = String.Empty;
					for (int i = 6; i < subparts.Length; i++)
					{
						message += subparts[i] + ' ';
					}

					_subtitles.Add(new Subtitle()
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

		public void AddSubtitle(Subtitle subtitle)
		{
			_subtitles.Add(subtitle);
		}

		public void Clear()
		{
			_subtitles.Clear();
		}

		public void RemoveAt(int index)
		{
			if (index >= _subtitles.Count) return;

			_subtitles.RemoveAt(index);
		}

		public void WriteText(StreamWriter sw)
		{
			foreach(var subtitle in _subtitles)
			{
				sw.WriteLine(subtitle.ToString());
			}
		}
	}
}