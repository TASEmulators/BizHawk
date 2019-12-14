namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IControlMainform
	{
		private bool _suppressAskSave;

		public bool NamedStatePending { get; set; }

		public bool WantsToControlSavestates => !NamedStatePending;

		public void SaveState()
		{
			BookMarkControl.UpdateBranchExternal();
		}

		public void LoadState()
		{
			BookMarkControl.LoadBranchExternal();
		}

		public void SaveStateAs()
		{
			// dummy
		}

		public void LoadStateAs()
		{
			// dummy
		}

		public void SaveQuickSave(int slot)
		{
			BookMarkControl.UpdateBranchExternal(slot);
		}

		public void LoadQuickSave(int slot)
		{
			BookMarkControl.LoadBranchExternal(slot);
		}

		public bool SelectSlot(int slot)
		{
			BookMarkControl.SelectBranchExternal(slot);
			return false;
		}

		public bool PreviousSlot()
		{
			BookMarkControl.SelectBranchExternal(false);
			return false;
		}

		public bool NextSlot()
		{
			BookMarkControl.SelectBranchExternal(true);
			return false;
		}

		public bool WantsToControlReadOnly => true;

		public void ToggleReadOnly()
		{
			if (CurrentTasMovie.IsPlaying)
			{
				TastudioRecordMode();
			}
			else if (CurrentTasMovie.IsRecording)
			{
				TastudioPlayMode();
			}
		}

		public bool WantsToControlStopMovie { get; private set; }

		public void StopMovie(bool suppressSave)
		{
			Focus();
			_suppressAskSave = suppressSave;
			NewTasMenuItem_Click(null, null);
			_suppressAskSave = false;
		}

		public bool WantsToControlRewind => true;

		public void CaptureRewind()
		{
			// Do nothing, Tastudio handles this just fine
		}

		public bool Rewind()
		{
			// copy pasted from TasView_MouseWheel(), just without notch logic
			if (Mainform.IsSeeking && !Mainform.EmulatorPaused)
			{
				Mainform.PauseOnFrame--;

				// that's a weird condition here, but for whatever reason it works best
				if (Emulator.Frame >= Mainform.PauseOnFrame)
				{
					Mainform.PauseEmulator();
					Mainform.PauseOnFrame = null;
					StopSeeking();
					GoToPreviousFrame();
				}

				RefreshDialog();
			}
			else
			{
				StopSeeking(); // late breaking memo: don't know whether this is needed
				GoToPreviousFrame();
			}

			return true;
		}

		public bool WantsToControlRestartMovie { get; }

		public void RestartMovie()
		{
			if (AskSaveChanges())
			{
				WantsToControlStopMovie = false;
				StartNewMovieWrapper(false);
				WantsToControlStopMovie = true;
				RefreshDialog();
			}
		}
	}
}
