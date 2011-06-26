using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BizHawk.MultiClient
{
	class SubtitleList
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
				if (subs[x].Frame + subs[x].Duration >= frame)
					return subs[x].Message;
			}
			return "";
		}

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

			string str = subtitleStr.Substring(x, subtitleStr.Length - x);
			try
			{
				s.Frame = Int16.Parse(str);
			}
			catch
			{
				return false;
			}

			x = str.IndexOf(' ');


			return true;
		}
	}
}
