using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BizHawk.MultiClient
{
	public class SubtitleList
	{
		private List<Subtitle> subs = new List<Subtitle>();

		public SubtitleList()
		{

		}
		/// <summary>
		/// Manages the logic of what subtitle should be displayed on any given frame based on frame & duration
		/// </summary>
		/// <param name="frame"></param>
		/// <returns></returns>
		public string GetSubtitle(int frame) 
		{
			if (subs.Count == 0) return "";

			for (int x = 0; x < subs.Count; x++)
			{
				if (frame >= subs[x].Frame && frame <= subs[x].Frame + subs[x].Duration)
					return subs[x].Message;
			}
			return "";
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

			string str = subtitleStr.Substring(x + 1, subtitleStr.Length - x - 1);
			string frame = str.Substring(0, str.IndexOf(' '));
			try
			{
				s.Frame = Int16.Parse(frame);
			}
			catch
			{
				return false;
			}

			//TODO: actually attempt to parse these things and supply with default values if they don't exist
			s.X = 0;
			s.Y = 0;
			s.Duration = 120;
			s.Message = str;
			s.Color = 0xFFFFFFFF;
			subs.Add(s);

			return true;
		}
	}
}
