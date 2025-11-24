using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		public MovieMode Mode { get; protected set; } = MovieMode.Inactive;

		public virtual void StartNewRecording(IEmulator emulator)
		{
			Mode = MovieMode.Record;
			if (MakeBackup && Session.Settings.EnableBackupMovies && Log.Count is not 0)
			{
				SaveBackup(emulator);
				MakeBackup = false;
			}

			Log.Clear();
		}

		public void StartNewPlayback() => Mode = MovieMode.Play;
		public void SwitchToRecord() => Mode = MovieMode.Record;
		public void SwitchToPlay() => Mode = MovieMode.Play;
		public void FinishedMode() => Mode = MovieMode.Finished;
		public void Stop() =>  Mode = MovieMode.Inactive;
	}
}
