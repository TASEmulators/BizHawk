using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class GenericDebugger : IControlMainform
	{
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
