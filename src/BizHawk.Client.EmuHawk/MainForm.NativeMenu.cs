namespace BizHawk.Client.EmuHawk
{
	public partial class MainForm
	{
		// Menu accessibility is handled centrally by FormBase.InstallNativeMenuShim,
		// which mirrors MainMenuStrip into a native Win32 MainMenu so MSAA focus events
		// fire correctly for screen readers.
		private void InitializeNativeMenu()
		{
		}
	}
}
