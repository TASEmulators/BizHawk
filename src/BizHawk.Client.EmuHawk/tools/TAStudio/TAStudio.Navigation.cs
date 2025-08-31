using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		/// <summary>
		/// Seek to the given frame, past or future, and load a state first if doing so gets us there faster.
		/// Does nothing if we are already on the given frame.
		/// </summary>
		public void GoToFrame(int frame, bool OnLeftMouseDown = false)
		{
			_lastRecordAction = -1;
			if (frame == Emulator.Frame)
			{
				CancelSeek();
				return;
			}

			// Unpausing after a seek may seem like we aren't really seeking at all:
			// what is the significance of a seek to frame if we don't pause?
			// Answer: We use this in order to temporarily disable recording mode when the user navigates to a frame. (to avoid recording between whatever is the most recent state and the user-specified frame)
			// Other answer: turbo seek, navigating while unpaused
			_pauseAfterSeeking = MainForm.EmulatorPaused || (_seekingTo != -1 && _pauseAfterSeeking);
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;
			TastudioPlayMode();

			var closestState = GetPriorStateForFramebuffer(frame);
			if (frame < Emulator.Frame || closestState.Key > Emulator.Frame)
			{
				LoadState(closestState, true);
			}
			closestState.Value.Dispose();

			StartSeeking(frame);

			if (!OnLeftMouseDown)
			{
				MaybeFollowCursor();
			}
		}

		public void GoToPreviousMarker()
		{
			if (Emulator.Frame > 0)
			{
				var prevMarker = CurrentTasMovie.Markers.Previous(Emulator.Frame);
				var prev = prevMarker?.Frame ?? 0;
				GoToFrame(prev);
			}
		}

		public void GoToNextMarker()
		{
			var nextMarker = CurrentTasMovie.Markers.Next(Emulator.Frame);
			var next = nextMarker?.Frame ?? CurrentTasMovie.InputLogLength - 1;
			GoToFrame(next);
		}

		public void RestorePosition()
		{
			if (RestorePositionFrame != -1)
			{
				// restore makes no sense without pausing
				// Pausing here ensures any seek done by GoToFrame pauses after completing.
				MainForm.PauseEmulator();
				GoToFrame(RestorePositionFrame);
			}
		}

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleFrame(int? frame = null)
		{
			if (TasView.AlwaysScroll && _leftButtonHeld)
			{
				return;
			}

			TasView.ScrollToIndex(frame ?? Emulator.Frame);
		}

		private void MaybeFollowCursor()
		{
			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleFrame();
			}
		}
	}
}
