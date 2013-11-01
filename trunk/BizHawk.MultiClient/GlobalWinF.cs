using SlimDX.Direct3D9;
using SlimDX.DirectSound;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public static class GlobalWinF
	{
		public static MainForm MainForm;
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

		public static UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		public static AutoFireStickyXORAdapter AutofireStickyXORAdapter = new AutoFireStickyXORAdapter();

		/// <summary>
		/// will OR together two IControllers
		/// </summary>
		public static ORAdapter OrControllerAdapter = new ORAdapter();

		public static SimpleController MovieOutputController = new SimpleController();

		public static Controller ClientControls;
	}
}
