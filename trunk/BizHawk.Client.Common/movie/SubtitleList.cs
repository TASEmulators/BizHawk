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

		/// <summary>
		/// Manages the logic of what subtitle should be displayed on any given frame based on frame & duration
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public string GetSubtitleMessage(int frame)
		{
			if (_subtitles.Count == 0) return "";

			foreach (Subtitle t in _subtitles)
			{
				if (frame >= t.Frame && frame <= t.Frame + t.Duration)
				{
					return t.Message;
				}
			}
			return String.Empty;
		}

		public Subtitle GetSubtitle(int frame)
		{
			if (_subtitles.Any())
			{
				foreach (Subtitle t in _subtitles)
				{
					if (frame >= t.Frame && frame <= t.Frame + t.Duration)
					{
						return t;
					}
				}
			}
			
			return new Subtitle();
		}

		public List<Subtitle> GetSubtitles(int frame)
		{
			return _subtitles.Where(t => frame >= t.Frame && frame <= t.Frame + t.Duration).ToList();
		}

		public int Count
		{
			get { return _subtitles.Count; }
		}

		//TODO
		public Point GetSubtitlePoint(int frame)
		{
			Point p = new Point(0, 0);
			return p;
		}

		/// <summary>
		/// Attempts to parse string for necessary subtitle information, required is a frame and a message, space delminated, the word subtitle assumed to be first
		/// </summary>
		/// <param name="subtitleStr"></param>
		/// <returns></returns>
		public bool AddSubtitle(string subtitleStr) //TODO: refactor with String.Split
		{
			if (!String.IsNullOrWhiteSpace(subtitleStr))
			{
				return false;
			}

			Subtitle s = new Subtitle();

			int x = subtitleStr.IndexOf(' ');
			if (x <= 0) return false;

			//remove "subtitle"
			string str = subtitleStr.Substring(x + 1, subtitleStr.Length - x - 1);

			x = str.IndexOf(' ');
			if (x <= 0) return false;

			string frame = str.Substring(0, x);
			str = str.Substring(x + 1, str.Length - x - 1);

			try
			{
				s.Frame = int.Parse(frame);
			}
			catch
			{
				return false;
			}

			x = str.IndexOf(' ');
			if (x <= 0) return false;
			string X = str.Substring(0, x);
			str = str.Substring(x + 1, str.Length - x - 1);
			try
			{
				s.X = int.Parse(X);
			}
			catch
			{
				return false;
			}

			x = str.IndexOf(' ');
			if (x <= 0) return false;
			string Y = str.Substring(0, x);
			str = str.Substring(x + 1, str.Length - x - 1);
			try
			{
				s.Y = int.Parse(Y);
			}
			catch
			{
				return false;
			}

			x = str.IndexOf(' ');
			if (x <= 0) return false;
			string Duration = str.Substring(0, x);
			str = str.Substring(x + 1, str.Length - x - 1);
			try
			{
				s.Duration = int.Parse(Duration);
			}
			catch
			{
				return false;
			}

			x = str.IndexOf(' ');
			if (x <= 0) return false;
			string Color = str.Substring(0, x);
			str = str.Substring(x + 1, str.Length - x - 1);
			try
			{
				s.Color = uint.Parse(Color, NumberStyles.HexNumber);
			}
			catch
			{
				return false;
			}

			s.Message = str;
			_subtitles.Add(s);

			return true;
		}

		public void AddSubtitle(Subtitle s)
		{
			_subtitles.Add(s);
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