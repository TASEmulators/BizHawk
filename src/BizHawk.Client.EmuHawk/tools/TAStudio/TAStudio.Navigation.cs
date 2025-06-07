using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		/// <summary> 
		/// Seek to the given frame, past or future, and load a state first if doing so gets us there faster.
		/// Does nothing if we are already on the given frame.
		/// </summary>
		public void GoToFrame(int frame, bool fromLua = false, bool fromRewinding = false, bool OnLeftMouseDown = false)
		{
			if (frame == Emulator.Frame)
			{
				return;
			}

			// Unpausing after a seek may seem like we aren't really seeking at all:
			// what is the significance of a seek to frame if we don't pause?
			// Answer: We use this in order to temporarily disable recording mode when the user navigates to a frame. (to avoid recording between whatever is the most recent state and the user-specified frame)
			// as well as ... to enable turbo-seek when navigating while unpaused? not sure (because this doesn't work)
			//   ... actually, fromRewinding is never set to true in any call AND NEVER WAS. TODO: Fix that.
			WasRecording = CurrentTasMovie.IsRecording() || WasRecording;
			_unpauseAfterSeeking = (fromRewinding || WasRecording) && !MainForm.EmulatorPaused;
			TastudioPlayMode();

			var closestState = GetPriorStateForFramebuffer(frame);
			if (frame < Emulator.Frame || closestState.Key > Emulator.Frame)
			{
				LoadState(closestState, true);
			}
			closestState.Value.Dispose();

			if (fromLua)
			{
				bool wasPaused = MainForm.EmulatorPaused;

				// why not use this? because I'm not letting the form freely run. it all has to be under this loop.
				// i could use this and then poll StepRunLoop_Core() repeatedly, but.. that's basically what I'm doing
				// PauseOnFrame = frame;

				while (Emulator.Frame != frame)
				{
					MainForm.SeekFrameAdvance();
				}

				if (!wasPaused)
				{
					MainForm.UnpauseEmulator();
				}

				// lua botting users will want to re-activate record mode automatically -- it should be like nothing ever happened
				if (WasRecording)
				{
					TastudioRecordMode();
				}

				// now the next section won't happen since we're at the right spot
			}

			// Seek needs to happen if any of:
			// 1) We should pause once reaching the target frame (that's what a seek is): currently paused OR currently seeking
			// 2) We are using _unpauseAfterSeeking (e.g. to manage recording state)
			// Otherwise, just don't seek and emulation will happily continue.
			if (MainForm.EmulatorPaused || _seekingTo != -1 || _unpauseAfterSeeking)
			{
				StartSeeking(frame);
			}
			else
			{
				// GUI users may want to be protected from clobbering their video when skipping around...
				// well, users who are rewinding aren't. (that gets done through the seeking system in the call above)
				// users who are clicking around.. I don't know.
			}

			if (!OnLeftMouseDown)
			{
				MaybeFollowCursor();
			}
		}

		/// <summary>
		/// Only goes to go to the frame if it is an event before current emulation, otherwise it is just a future event that can freely be edited
		/// </summary>
		private void GoToLastEmulatedFrameIfNecessary(int frame, bool OnLeftMouseDown = false)
		{
			if (frame != Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				if (frame <= Emulator.Frame)
				{
					if ((MainForm.EmulatorPaused || _seekingTo == -1)
						&& !CurrentTasMovie.LastPositionStable)
					{
						RestorePositionFrame = Emulator.Frame;
						CurrentTasMovie.LastPositionStable = true; // until new frame is emulated
					}

					GoToFrame(frame, false, false, OnLeftMouseDown);
				}
				else
				{
					_triggerAutoRestore = false;
				}
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
