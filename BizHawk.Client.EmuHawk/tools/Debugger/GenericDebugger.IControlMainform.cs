using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IControlMainform
	{
		public bool WantsToControlSavestates { get { return false; } }

		public void SaveState() { }
		public void LoadState() { }
		public void SaveStateAs() { }
		public void LoadStateAs() { }
		public void SaveQuickSave(int slot) { }
		public void LoadQuickSave(int slot) { }
		public void SelectSlot(int slot) { }
		public void PreviousSlot() { }
		public void NextSlot() { }

		public bool WantsToControlReadOnly { get { return false; } }
		public void ToggleReadOnly() { }

		public bool WantsToControlStopMovie { get { return false; } }
		public void StopMovie(bool supressSave) { }

		// TODO: We probably want to do this
		public bool WantsToControlRewind { get { return false; } }
		public void CaptureRewind() { }
		public bool Rewind() { return false; }

		public bool WantsToControlRestartMovie { get { return false; } }
		public void RestartMovie() { }

		// TODO: We want to prevent movies and probably other things
	}
}
