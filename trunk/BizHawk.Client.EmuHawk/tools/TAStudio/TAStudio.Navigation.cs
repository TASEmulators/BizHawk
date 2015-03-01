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
				var restoreFrame = Emulator.Frame;

				if (frame <= Emulator.Frame)
				{
					GoToFrame(frame);
				}

				_autoRestoreFrame = restoreFrame;
			}
		}

		// SuuperW: I changed this to public so that it could be used by MarkerControl.cs
		public void GoToFrame(int frame)
		{
			// If past greenzone, emulate and capture states
			// If past greenzone AND movie, record input and capture states
			// If in greenzone, loadstate
			// If near a greenzone item, load and emulate
			// Do capturing and recording as needed

			if (frame < CurrentTasMovie.InputLogLength)
			{
				if (frame < Emulator.Frame) // We are rewinding
				{
					var goToFrame = frame == 0 ? 0 : frame - 1;

					if (CurrentTasMovie[goToFrame].HasState) // Go back 1 frame and emulate to get the display (we don't store that)
					{
						CurrentTasMovie.SwitchToPlay();
						LoadState(CurrentTasMovie[goToFrame].State);

						if (frame > 0) // We can't emulate up to frame 0!
						{
							GlobalWin.MainForm.FrameAdvance();
						}

						GlobalWin.DisplayManager.NeedsToPaint = true;
						SetVisibleIndex(frame);
					}
					else // Get as close as we can then emulate there
					{
						StartAtNearestFrameAndEmulate(frame);
						return;
					}
				}
				else // We are going foward
				{
					if (frame == Emulator.Frame + 1) // Just emulate a frame we only have 1 to go!
					{
						GlobalWin.MainForm.FrameAdvance();
					}
					else
					{
						var goToFrame = frame == 0 ? 0 : frame - 1;
						if (CurrentTasMovie[goToFrame].HasState) // Can we go directly there?
						{
							CurrentTasMovie.SwitchToPlay();
							LoadState(CurrentTasMovie[goToFrame].State);
							Emulator.FrameAdvance(true);
							GlobalWin.DisplayManager.NeedsToPaint = true;

							SetVisibleIndex(frame);
						}
						else
						{
							StartAtNearestFrameAndEmulate(frame);
							return;
						}
					}
				}
			}
			else // Emulate to a future frame
			{
				if (frame == Emulator.Frame + 1) // We are at the end of the movie and advancing one frame, therefore we are recording, simply emulate a frame
				{
					GlobalWin.MainForm.FrameAdvance();
				}
				else
				{
					// TODO: get the last greenzone frame and go there
					CurrentTasMovie.SwitchToPlay();

					// no reason to loadstate when we can emulate a frame instead
					if (frame - Emulator.Frame != 1)
					{
						LoadState(CurrentTasMovie[CurrentTasMovie.TasStateManager.LastEmulatedFrame].State);
					}

					if (frame != Emulator.Frame) // If we aren't already at our destination, seek
					{
						GlobalWin.MainForm.UnpauseEmulator();
						if (Settings.AutoPause && frame < CurrentTasMovie.InputLogLength)
						{
							GlobalWin.MainForm.PauseOnFrame = CurrentTasMovie.InputLogLength;
						}
						else
						{
							GlobalWin.MainForm.PauseOnFrame = frame;
						}
					}
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
	}
}
