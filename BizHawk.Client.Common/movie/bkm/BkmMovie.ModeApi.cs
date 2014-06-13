namespace BizHawk.Client.Common
{
	public partial class BkmMovie : IMovie
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
			// adelikat: ClearSaveRam shouldn't be here at all most likely, especially considering this is an implementation detail
			// If Starting a new recording requires clearing sram it shoudl be done at a higher layer and not rely on all IMovies doing this
			// Haven't removed it yet because I coudln't guarantee that power-on movies coudl live without it
			// And the immediate fire is that Savestate movies are breaking
			if (!StartsFromSavestate)
			{
				Global.Emulator.ClearSaveRam();
			}

			_mode = Moviemode.Record;
			if (Global.Config.EnableBackupMovies && _makeBackup && _log.Length > 0)
			{
				SaveBackup();
				_makeBackup = false;
			}

			_log.Clear();
		}

		public void StartNewPlayback()
		{
			// See StartNewRecording for details as to why this savestate check is here
			if (!StartsFromSavestate)
			{
				Global.Emulator.ClearSaveRam();
			}

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

		public void Stop(bool saveChanges = true)
		{
			if (saveChanges)
			{
				if (_mode == Moviemode.Record || _changes)
				{
					Save();
				}
			}

			_changes = false;
			_mode = Moviemode.Inactive;
		}

		/// <summary>
		/// If a movie is in playback mode, this will set it to movie finished
		/// </summary>
		private void Finish()
		{
			if (_mode == Moviemode.Play)
			{
				_mode = Moviemode.Finished;
			}
		}
	}
}
