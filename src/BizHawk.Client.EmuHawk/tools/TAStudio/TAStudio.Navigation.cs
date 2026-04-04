using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		/// <summary>
		/// Seek to the given frame, past or future, and load a state first if doing so gets us there faster.
		/// Does nothing if we are already on the given frame.
		/// </summary>
		public void GoToFrame(int frame, bool OnLeftMouseDown = false, bool skipLoadState = false)
		{
			_lastRecordAction = -1;
			if (frame == Emulator.Frame)
			{
				StopSeeking();
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
			if (frame < Emulator.Frame || (closestState.Key > Emulator.Frame && !skipLoadState))
			{
				LoadState(closestState);
			}
			closestState.Value.Dispose();

			if (Emulator.Frame != frame)
			{
				_seekStartFrame = Emulator.Frame;
				_seekingByEdit = false;

				_seekingTo = frame;
				MainForm.PauseOnFrame = int.MaxValue; // This being set is how MainForm knows we are seeking, and controls TurboSeek.
				MainForm.UnpauseEmulator();

				if (_seekingTo - _seekStartFrame > 1)
				{
					MessageStatusLabel.Text = "Seeking...";
					ProgressBar.Visible = true;
				}
			}
			else
			{
				StopSeeking();
			}

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
				GoToFrame(RestorePositionFrame, skipLoadState: !Config.TurboSeek);
			}
		}

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleFrame(int? frame = null)
		{
			if (_leftButtonHeld)
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

		public int GetSeekFrame()
		{
			return _seekingTo == -1 ? Emulator.Frame : _seekingTo;
		}
	}
}
