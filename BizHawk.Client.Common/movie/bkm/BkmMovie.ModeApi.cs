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
			// adelikat: ClearSaveRam shouldn't be here at all most likely, especially considering this is an implementation detail
			// If Starting a new recording requires clearing sram it shoudl be done at a higher layer and not rely on all IMovies doing this
			// Haven't removed it yet because I coudln't guarantee that power-on movies coudl live without it
			// And the immediate fire is that Savestate movies are breaking

			/*
			 * natt: in light of more recent changes, the front end is no longer errantly loading saveram for new movies.  so, as best as i
			 * can tell through snaking through the debugger, this is no longer needed.
			 *
			if (!StartsFromSavestate)
			{
				Global.Emulator.ClearSaveRam();
			}
			*/
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
			// See StartNewRecording for details as to why this code is gone
			/*
			if (!StartsFromSavestate)
			{
				Global.Emulator.ClearSaveRam();
			}*/

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
