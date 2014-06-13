using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private enum Moviemode { Inactive, Play, Record, Finished }
		private Moviemode _mode = Moviemode.Inactive;

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play || _mode == Moviemode.Finished; }
		}

		public bool IsRecording
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public void StartNewRecording()
		{
			throw new NotImplementedException();
		}

		public void StartNewPlayback()
		{
			throw new NotImplementedException();
		}

		public void SwitchToRecord()
		{
			throw new NotImplementedException();
		}

		public void SwitchToPlay()
		{
			throw new NotImplementedException();
		}

		public void Stop(bool saveChanges = true)
		{
			throw new NotImplementedException();
		}
	}
}
