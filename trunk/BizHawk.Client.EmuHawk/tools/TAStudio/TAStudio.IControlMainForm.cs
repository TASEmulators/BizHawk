namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IControlMainform
	{
		
		public bool WantsToControlReadOnly { get { return false; } }
		public void ToggleReadOnly()
		{
			GlobalWin.OSD.AddMessage("TAStudio does not allow manual readonly toggle");
		}

		public bool WantsToControlStopMovie { get; private set; }

		public void StopMovie()
		{
			this.Focus();
			NewTasMenuItem_Click(null, null);
		}

		public bool WantsToControlRewind { get { return true; } }

		public void CaptureRewind()
		{
			// Do nothing, Tastudio handles this just fine
		}

		public bool Rewind()
		{
			GoToPreviousFrame();
			return true;
		}

		public bool WantsToControlRestartMovie { get; private set; }

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
