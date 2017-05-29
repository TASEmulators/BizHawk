using System.Linq;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected enum Moviemode
		{
			Inactive, Play, Record, Finished
		}

		protected Moviemode Mode { get; set; } = Moviemode.Inactive;

		public bool IsActive => Mode != Moviemode.Inactive;

		public bool IsPlaying => Mode == Moviemode.Play || Mode == Moviemode.Finished;

		public bool IsRecording => Mode == Moviemode.Record;

		public bool IsFinished => Mode == Moviemode.Finished;

		public virtual void StartNewRecording()
		{
			Mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && MakeBackup && Log.Any())
			{
				SaveBackup();
				MakeBackup = false;
			}

			Log.Clear();
		}

		public virtual void StartNewPlayback()
		{
			Mode = Moviemode.Play;
		}

		public virtual void SwitchToRecord()
		{
			Mode = Moviemode.Record;
		}

		public virtual void SwitchToPlay()
		{
			Mode = Moviemode.Play;
		}

		public virtual bool Stop(bool saveChanges = true)
		{
			bool saved = false;
			if (saveChanges)
			{
				if (Mode == Moviemode.Record || (IsActive && Changes))
				{
					Save();
					saved = true;
				}
			}

			Changes = false;
			Mode = Moviemode.Inactive;

			return saved;
		}

		public void FinishedMode()
		{
			Mode = Moviemode.Finished;
		}
	}
}
