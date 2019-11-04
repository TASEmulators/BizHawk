using System;
using System.Text;

namespace BizHawk.Client.Common
{
	public class TasSession
	{
		private readonly TasMovie _movie;
		public int CurrentFrame { get; private set; }
		public int CurrentBranch { get; private set; }

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
			UpdateValues();

			var sb = new StringBuilder();
			sb.AppendLine(CurrentFrame.ToString());
			sb.AppendLine(CurrentBranch.ToString());

			return sb.ToString();
		}

		public void PopulateFromString(string session)
		{
			if (!string.IsNullOrWhiteSpace(session))
			{
				string[] lines = session.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

				CurrentFrame = lines.Length > 0 ? int.Parse(lines[0]) : 0;

				if (lines.Length > 1)
				{
					CurrentBranch = int.Parse(lines[1]);
				}
				else
				{
					CurrentBranch = -1;
				}
			}
		}
	}
}
