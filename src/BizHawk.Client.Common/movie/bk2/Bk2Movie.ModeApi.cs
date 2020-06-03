using System.Linq;

namespace BizHawk.Client.Common
{
	internal partial class Bk2Movie
	{
		public MovieMode Mode { get; protected set; } = MovieMode.Inactive;

		public virtual void StartNewRecording()
		{
			Mode = MovieMode.Record;
			if (Session.Settings.EnableBackupMovies && MakeBackup && Log.Any())
			{
				SaveBackup();
				MakeBackup = false;
			}

			Log.Clear();
		}

		public void StartNewPlayback() => Mode = MovieMode.Play;
		public void SwitchToRecord() => Mode = MovieMode.Record;
		public void SwitchToPlay() => Mode = MovieMode.Play;
		public void FinishedMode() => Mode = MovieMode.Finished;

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
	}
}
