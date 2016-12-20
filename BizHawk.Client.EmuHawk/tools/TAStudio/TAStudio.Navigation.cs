using System.Linq;
using System.IO;

using BizHawk.Client.Common;

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
				int restoreFrame = Emulator.Frame;

				if (frame <= Emulator.Frame)
				{
					if ((Mainform.EmulatorPaused || !Mainform.IsSeeking) &&
						!CurrentTasMovie.LastPositionStable)
					{
						LastPositionFrame = Emulator.Frame;
						CurrentTasMovie.LastPositionStable = true; // until new frame is emulated
					}
					GoToFrame(frame);
				}
			}
		}

		// SuuperW: I changed this to public so that it could be used by MarkerControl.cs
		public void GoToFrame(int frame)
		{
			// If seeking to a frame before or at the end of the movie, use StartAtNearestFrameAndEmulate
			// Otherwise, load the latest state (if not already there) and seek while recording.

			if (frame <= CurrentTasMovie.InputLogLength)
			{
				// Get as close as we can then emulate there
				StartAtNearestFrameAndEmulate(frame);

				MaybeFollowCursor();
			}
			else // Emulate to a future frame
			{
				if (frame == Emulator.Frame + 1) // We are at the end of the movie and advancing one frame, therefore we are recording, simply emulate a frame
				{
					bool wasPaused = Mainform.EmulatorPaused;
					Mainform.FrameAdvance();
					if (!wasPaused)
						Mainform.UnpauseEmulator();
				}
				else
				{
					TastudioPlayMode();

					int lastState = CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame).Key; // Simply getting the last state doesn't work if that state is the frame. [dispaly isn't saved in the state, need to emulate to frame]
					if (lastState > Emulator.Frame)
						LoadState(CurrentTasMovie.TasStateManager[lastState]); // STATE ACCESS

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

		public void GoToNextFrame()
		{
			GoToFrame(Emulator.Frame + 1);
		}

		public void GoToPreviousMarker()
		{
			if (Emulator.Frame > 0)
			{
				var prevMarker = CurrentTasMovie.Markers.Previous(Emulator.Frame);
				var prev = prevMarker != null ? prevMarker.Frame : 0;
				GoToFrame(prev);
			}
		}

		public void GoToNextMarker()
		{
			var nextMarker = CurrentTasMovie.Markers.Next(Emulator.Frame);
			var next = nextMarker != null ? nextMarker.Frame : CurrentTasMovie.InputLogLength - 1;
			GoToFrame(next);
		}

		public void GoToMarker(TasMovieMarker marker)
		{
			GoToFrame(marker.Frame);
		}

		/// <summary>
		/// Makes the given frame visible. If no frame is given, makes the current frame visible.
		/// </summary>
		public void SetVisibleIndex(int? indexThatMustBeVisible = null)
		{
			if (!indexThatMustBeVisible.HasValue)
				indexThatMustBeVisible = Emulator.Frame;

			TasView.ScrollToIndex(indexThatMustBeVisible.Value);
		}

		private void MaybeFollowCursor()
		{
			if (TasPlaybackBox.FollowCursor)
				SetVisibleIndex();
		}
	}
}
