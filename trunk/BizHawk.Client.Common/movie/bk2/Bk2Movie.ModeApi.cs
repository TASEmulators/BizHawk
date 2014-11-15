using System.Linq;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected enum Moviemode { Inactive, Play, Record, Finished }
		protected Moviemode _mode = Moviemode.Inactive;

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
			get { return _mode == Moviemode.Record; }
		}

		public bool IsFinished
		{
			get { return _mode == Moviemode.Finished; }
		}

		public virtual void StartNewRecording()
		{
			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && MakeBackup && _log.Any())
			{
				SaveBackup();
				MakeBackup = false;
			}

			_log.Clear();
		}

		public virtual void StartNewPlayback()
		{
			_mode = Moviemode.Play;
		}

		public virtual void SwitchToRecord()
		{
			_mode = Moviemode.Record;
		}

		public virtual void SwitchToPlay()
		{
			_mode = Moviemode.Play;
		}

		public virtual bool Stop(bool saveChanges = true)
		{
			bool saved = false;
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || (IsActive && Changes))
				{
					Save();
					saved = true;
				}
			}

			Changes = false;
			_mode = Moviemode.Inactive;

			return saved;
		}

		public void FinishedMode()
		{
			_mode = Moviemode.Finished;
		}
	}
}
