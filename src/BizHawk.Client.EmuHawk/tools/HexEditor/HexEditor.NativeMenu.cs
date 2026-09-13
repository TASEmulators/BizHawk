using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class HexEditor
	{
		// Menu accessibility is handled centrally by FormBase.InstallNativeMenuShim.
		// This file just sets non-menu accessibility properties on the form's controls.
		private void InitializeNativeMenu()
		{
			AccessibleName = "Hex Editor";
			AccessibleDescription = "Hexadecimal memory editor for viewing and editing memory";
			AccessibleRole = AccessibleRole.Window;

			MemoryViewerBox.AccessibleName = "Memory Viewer";
			MemoryViewerBox.AccessibleDescription = "Displays memory contents in hexadecimal format";
			MemoryViewerBox.AccessibleRole = AccessibleRole.Pane;

			HexScrollBar.AccessibleName = "Memory Scroll";
			HexScrollBar.AccessibleRole = AccessibleRole.ScrollBar;

			AddressLabel.AccessibleName = "Address Column";
			AddressesLabel.AccessibleName = "Memory Values";
		}
	}
}
