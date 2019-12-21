using System.Linq;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected enum MovieMode
		{
			Inactive, Play, Record, Finished
		}

		protected MovieMode Mode { get; set; } = MovieMode.Inactive;

		public bool IsActive => Mode != MovieMode.Inactive;

		public bool IsPlaying => Mode == MovieMode.Play || Mode == MovieMode.Finished;

		public bool IsRecording => Mode == MovieMode.Record;

		public bool IsFinished => Mode == MovieMode.Finished;

		public virtual void StartNewRecording()
		{
			Mode = MovieMode.Record;
			if (Global.Config.EnableBackupMovies && MakeBackup && Log.Any())
			{
				SaveBackup();
				MakeBackup = false;
			}

			Log.Clear();
		}

		public virtual void StartNewPlayback()
		{
			Mode = MovieMode.Play;
		}

		public virtual void SwitchToRecord()
		{
			Mode = MovieMode.Record;
		}

		public virtual void SwitchToPlay()
		{
			Mode = MovieMode.Play;
		}

		public virtual bool Stop(bool saveChanges = true)
		{
			bool saved = false;
			if (saveChanges)
			{
				if (Mode == MovieMode.Record || (IsActive && Changes))
				{
					Save();
					saved = true;
				}
			}

			Changes = false;
			Mode = MovieMode.Inactive;

			return saved;
		}

		public void FinishedMode()
		{
			Mode = MovieMode.Finished;
		}
	}
}
