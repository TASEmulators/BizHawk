﻿namespace BizHawk.Client.EmuHawk
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

		public bool LoadState()
			=> BookMarkControl.LoadBranchExternal();

		public void SaveStateAs()
		{
			// dummy
		}

		public bool LoadStateAs()
			=> false;

		public void SaveQuickSave(int slot)
			=> BookMarkControl.UpdateBranchExternal(slot - 1);

		public bool LoadQuickSave(int slot)
			=> BookMarkControl.LoadBranchExternal(slot - 1);

		public bool SelectSlot(int slot)
		{
			BookMarkControl.SelectBranchExternal(slot - 1);
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
			TastudioToggleReadOnly();
		}

		public bool WantsToControlStopMovie { get; private set; }

		public void StopMovie(bool suppressSave)
		{
			if (!MainForm.GameIsClosing)
			{
				Activate();
				_suppressAskSave = suppressSave;
				StartNewTasMovie();
				_suppressAskSave = false;
			}
		}

		public bool WantsToControlRewind { get; private set; } = true;

		public void CaptureRewind()
		{
			// Do nothing, Tastudio handles this just fine
		}

		public bool Rewind()
		{
			int rewindStep = MainForm.IsFastForwarding ? Settings.RewindStepFast : Settings.RewindStep;
			WheelSeek(rewindStep);
			return true;
		}

		public bool WantsToControlRestartMovie { get; }

		public bool RestartMovie()
		{
			if (!AskSaveChanges()) return false;
			var success = StartNewMovieWrapper(CurrentTasMovie, isNew: false);
			RefreshDialog();
			return success;
		}

		public bool WantsToControlReboot => false;
		public void RebootCore() => throw new NotSupportedException("This should never be called");

		public bool WantsToBypassMovieEndAction => true;
	}
}
