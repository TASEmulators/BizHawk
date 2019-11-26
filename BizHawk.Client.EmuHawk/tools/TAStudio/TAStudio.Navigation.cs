namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		/// <summary>
		/// Only goes to go to the frame if it is an event before current emulation, otherwise it is just a future event that can freely be edited
		/// </summary>
		private void GoToLastEmulatedFrameIfNecessary(int frame)
		{
			if (frame != Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				if (frame <= Emulator.Frame)
				{
					if ((Mainform.EmulatorPaused || !Mainform.IsSeeking)
						&& !CurrentTasMovie.LastPositionStable)
					{
						LastPositionFrame = Emulator.Frame;
						CurrentTasMovie.LastPositionStable = true; // until new frame is emulated
					}

					GoToFrame(frame);
				}
			}
		}

		public void GoToFrame(int frame, bool fromLua = false, bool fromRewinding = false)
		{
			// If seeking to a frame before or at the end of the movie, use StartAtNearestFrameAndEmulate
			// Otherwise, load the latest state (if not already there) and seek while recording.
			WasRecording = CurrentTasMovie.IsRecording || WasRecording;

			if (frame <= CurrentTasMovie.InputLogLength)
			{
				// Get as close as we can then emulate there
				StartAtNearestFrameAndEmulate(frame, fromLua, fromRewinding);

				MaybeFollowCursor();
			}
			else // Emulate to a future frame
			{
				if (frame == Emulator.Frame + 1) // We are at the end of the movie and advancing one frame, therefore we are recording, simply emulate a frame
				{
					bool wasPaused = Mainform.EmulatorPaused;
					Mainform.FrameAdvance();
					if (!wasPaused)
					{
						Mainform.UnpauseEmulator();
					}
				}
				else
				{
					TastudioPlayMode();

					// Simply getting the last state doesn't work if that state is the frame.
					// display isn't saved in the state, need to emulate to frame
					var lastState = CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame);
					if (lastState.Key > Emulator.Frame)
					{
						LoadState(lastState);
					}

					StartSeeking(frame);
				}
			}

			RefreshDialog();
			UpdateOtherTools();
		}

		public void GoToPreviousFrame()
		{
			if (Emulator.Frame > 0)
			{
				GoToFrame(Emulator.Frame - 1);
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

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleIndex(int? indexThatMustBeVisible = null)
		{
			if (TasView.AlwaysScroll && _leftButtonHeld)
				return;

			if (!indexThatMustBeVisible.HasValue)
			{
				indexThatMustBeVisible = Emulator.Frame;
			}

			TasView.ScrollToIndex(indexThatMustBeVisible.Value);
		}

		private void MaybeFollowCursor()
		{
			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleIndex();
			}
		}
	}
}
