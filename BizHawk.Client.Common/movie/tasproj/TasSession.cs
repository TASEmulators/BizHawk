using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class TasSession
	{
		private TasMovie _movie;
		public int CurrentFrame { get; set; }
		public int CurrentBranch { get; set; }

		public TasSession(TasMovie movie)
		{
			_movie = movie;
			CurrentFrame = 0;
			CurrentBranch = -1;
		}

		public void UpdateValues()
		{
			CurrentFrame = Global.Emulator.Frame;
			CurrentBranch = _movie.CurrentBranch;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			UpdateValues();
			sb.AppendLine(CurrentFrame.ToString());
			sb.AppendLine(CurrentBranch.ToString());

			return sb.ToString();
		}

		public void PopulateFromString(string session)
		{
			if (!string.IsNullOrWhiteSpace(session))
			{
				string[] lines = session.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

				if (lines.Length > 0)
					CurrentFrame = int.Parse(lines[0]);
				else
					CurrentFrame = 0;

				if (lines.Length > 1)
					CurrentBranch = int.Parse(lines[1]);
				else
					CurrentBranch = -1;
			}
		}
	}
}
