using BizHawk.Client.Common;
using BizHawk.Bizware.BizwareGL;
using SlimDX.DirectSound;

namespace BizHawk.Client.MultiHawk
{
	public static class GlobalWin
	{
		public static Mainform MainForm;
		//public static ToolManager Tools;
		public static IGL GL;
		public static Bizware.BizwareGL.Drivers.OpenTK.IGL_TK IGL_GL;
		public static BizHawk.Client.EmuHawk.GLManager.ContextRef CR_GL;
		//public static Sound Sound;
		//public static PresentationPanel PresentationPanel;
		//public static OSDManager OSD = new OSDManager();
		//public static DisplayManager DisplayManager;
		public static BizHawk.Client.EmuHawk.GLManager GLManager;

		//input state which has been destined for game controller inputs are coalesced here
		//public static ControllerInputCoalescer ControllerInputCoalescer = new ControllerInputCoalescer();
		//input state which has been destined for client hotkey consumption are colesced here
		public static InputCoalescer HotkeyCoalescer = new InputCoalescer();
	}
}
