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
	}
}
