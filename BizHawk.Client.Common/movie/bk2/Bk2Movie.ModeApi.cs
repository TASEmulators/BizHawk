using System.Linq;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		public MovieMode Mode { get; protected set; } = MovieMode.Inactive;

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
				if (Mode == MovieMode.Record || (this.IsActive() && Changes))
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
