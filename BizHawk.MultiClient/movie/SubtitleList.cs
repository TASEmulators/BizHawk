using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace BizHawk.MultiClient
{
	public class SubtitleList
	{
		private readonly List<Subtitle> subs = new List<Subtitle>();

		public SubtitleList()
		{

		}

		public SubtitleList(Movie m)
		{
			if (m != null && m.Subtitles.Count == 0)
			{
				return;
			}

			for (int x = 0; x < m.Subtitles.Count; x++)
			{
				Subtitle s = new Subtitle(m.Subtitles.GetSubtitleByIndex(x));
				subs.Add(s);
			}
		}

		public Subtitle GetSubtitleByIndex(int index)
		{
			if (index >= subs.Count || index < 0) return new Subtitle();

			return subs[index];
		}

		public string GetSubtitleText(int index)
		{
			if (index >= subs.Count || index < 0)
			{
				return "";
			}

			StringBuilder sb = new StringBuilder("subtitle ");
			sb.Append(subs[index].Frame.ToString());
			sb.Append(" ");
			sb.Append(subs[index].X.ToString());
			sb.Append(" ");
			sb.Append(subs[index].Y.ToString());
			sb.Append(" ");
			sb.Append(subs[index].Duration.ToString());
			sb.Append(" ");
			sb.Append(string.Format("{0:X8}", subs[index].Color));
			sb.Append(" ");
			sb.Append(subs[index].Message);
			return sb.ToString();
		}

		/// <summary>
		/// Manages the logic of what subtitle should be displayed on any given frame based on frame & duration
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public string GetSubtitleMessage(int frame)
		{
			if (subs.Count == 0) return "";

			foreach (Subtitle t in subs)
			{
				if (frame >= t.Frame && frame <= t.Frame + t.Duration)
				{
					return t.Message;
				}
			}
			return "";
		}

		public Subtitle GetSubtitle(int frame)
		{
			if (subs.Count == 0) return new Subtitle();

			foreach (Subtitle t in subs)
			{
				if (frame >= t.Frame && frame <= t.Frame + t.Duration)
				{
					return t;
				}
			}
			return new Subtitle();
		}

		public List<Subtitle> GetSubtitles(int frame)
		{
			if (subs.Count == 0)
			{
				return null;
			}

			return subs.Where(t => frame >= t.Frame && frame <= t.Frame + t.Duration).ToList();
		}

		public int Count
		{
			get { return subs.Count; }
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
		public bool AddSubtitle(string subtitleStr)
		{
			if (subtitleStr.Length == 0) return false;

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
			subs.Add(s);

			return true;
		}

		public void AddSubtitle(Subtitle s)
		{
			subs.Add(s);
		}

		public void ClearSubtitles()
		{
			subs.Clear();
		}

		public void Remove(int index)
		{
			if (index >= subs.Count) return;

			subs.RemoveAt(index);
		}

		public void WriteText(StreamWriter sw)
		{
			int length = subs.Count;
			for (int x = 0; x < length; x++)
			{
				sw.WriteLine(GetSubtitleText(x));
			}
		}
	}
}