namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IControlMainform
	{

		public bool WantsToControlReboot => false;
		public void RebootCore()
		{
		}

		public bool WantsToControlSavestates => false;

		public void SaveState() { }

		public bool LoadState()
			=> false;

		public void SaveStateAs() { }

		public bool LoadStateAs()
			=> false;

		public void SaveQuickSave(int slot) { }

		public bool LoadQuickSave(int slot)
			=> false;

		public bool SelectSlot(int slot) => false;
		public bool PreviousSlot() => false;
		public bool NextSlot() => false;

		public bool WantsToControlReadOnly => false;
		public void ToggleReadOnly() { }

		public bool WantsToControlStopMovie => false;
		public void StopMovie(bool suppressSave) { }

		// TODO: We probably want to do this
		public bool WantsToControlRewind => false;
		public void CaptureRewind() { }
		public bool Rewind() => false;

		public bool WantsToControlRestartMovie => false;

		public bool RestartMovie()
			=> false;

		public bool WantsToBypassMovieEndAction => false;

		// TODO: We want to prevent movies and probably other things
	}
}
