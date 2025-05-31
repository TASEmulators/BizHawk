namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		public MovieMode Mode { get; protected set; } = MovieMode.Inactive;

		public virtual void StartNewRecording()
		{
			Mode = MovieMode.Record;
			if (MakeBackup && Session.Settings.EnableBackupMovies && Log.Count is not 0)
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
				// How would a movie ever have changes while inactive?
				if (this.IsActive() && Changes)
				{
					Save();
					saved = true;
				}
			}

			Mode = MovieMode.Inactive;

			return saved;
		}
	}
}
