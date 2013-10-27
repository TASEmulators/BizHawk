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

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static StickyXORAdapter StickyXORAdapter = new StickyXORAdapter();

		public static AutoFireStickyXORAdapter AutofireStickyXORAdapter = new AutoFireStickyXORAdapter();

		/// <summary>
		/// Forces any controller button to Off, useful for things like Joypad.Set
		/// </summary>
		public static ForceOffAdaptor ForceOffAdaptor = new ForceOffAdaptor();

		/// <summary>
		/// will OR together two IControllers
		/// </summary>
		public static ORAdapter OrControllerAdapter = new ORAdapter();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static SimpleController MovieOutputController = new SimpleController();

		public static Controller ClientControls;
	}
}
