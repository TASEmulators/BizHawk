#if WINDOWS
using SlimDX.Direct3D9;
using SlimDX.DirectSound;
#endif

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class GlobalWin
	{
		public static MainForm MainForm;
		public static ToolManager Tools;
#if WINDOWS
		public static DirectSound DSound;
		public static Direct3D Direct3D;
#endif
		public static Sound Sound;
		public static IRenderer RenderPanel;
		public static OSDManager OSD = new OSDManager();
		public static DisplayManager DisplayManager = new DisplayManager();

		//input state which has been destined for game controller inputs are coalesced here
		public static ControllerInputCoalescer ControllerInputCoalescer = new ControllerInputCoalescer();
		//input state which has been destined for client hotkey consumption are colesced here
		public static InputCoalescer HotkeyCoalescer = new InputCoalescer();
	}
}
