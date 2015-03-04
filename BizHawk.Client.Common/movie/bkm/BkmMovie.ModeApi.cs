using System.Linq;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie
	{
		private enum Moviemode { Inactive, Play, Record, Finished }

		private Moviemode _mode = Moviemode.Inactive;

		public bool IsPlaying
		{
			get { return _mode == Moviemode.Play || _mode == Moviemode.Finished; }
		}

		public bool IsRecording
		{
			get { return _mode == Moviemode.Record; }
		}

		public bool IsActive
		{
			get { return _mode != Moviemode.Inactive; }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public void StartNewRecording()
		{
			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && _makeBackup && _log.Any())
			{
				SaveBackup();
				_makeBackup = false;
			}

			_log.Clear();
		}

		public void StartNewPlayback()
		{
			_mode = Moviemode.Play;
		}

		public void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		public void SwitchToPlay()
		{
			_mode = Moviemode.Play;
			Save();
		}

		public bool Stop(bool saveChanges = true)
		{
			bool saved = false;
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || _changes)
				{
					Save();
					saved = true;
				}
			}

			_changes = false;
			_mode = Moviemode.Inactive;

			return saved;
		}

		public void FinishedMode()
		{
			_mode = Moviemode.Finished;
		}
	}
}
