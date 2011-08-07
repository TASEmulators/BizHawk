using BizHawk.DiscSystem;
using SlimDX.Direct3D9;
using SlimDX.DirectSound;

namespace BizHawk.MultiClient
{
	public static class Global
	{
		public static MainForm MainForm;
		public static DirectSound DSound;
		public static Direct3D Direct3D;
		public static Sound Sound;
		public static IRenderer RenderPanel;
		public static Config Config;
		public static IEmulator Emulator;
		public static CoreInputComm CoreInputComm;
		public static GameInfo Game;
		public static Controller SMSControls;
		public static Controller PCEControls;
		public static Controller GenControls;
		public static Controller TI83Controls;
		public static Controller NESControls;
		public static Controller GBControls;
		public static Controller NullControls;
		public static CheatList CheatList;

		//the movie will be spliced inbetween these if it is present
		public static CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();
		public static CopyControllerAdapter MovieOutputAdapter = new CopyControllerAdapter();

		/// <summary>
		/// the global MovieSession can use this to deal with multitrack player remapping (should this be here? maybe it should be in MovieSession)
		/// </summary>
		public static MultitrackRewiringControllerAdapter MultitrackRewiringControllerAdapter = new MultitrackRewiringControllerAdapter();


		public static MovieSession MovieSession = new MovieSession();


		//dont take my word for it, since the final word is actually in RewireInputChain, but here is a guide...
		//user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
		//.. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
		//(1)->Input Display
		
		//the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController;
		
		//the "output" port for the controller chain. 
		public static CopyControllerAdapter ControllerOutput = new CopyControllerAdapter();

		//input state which has been destined for game controller inputs are coalesced here
		public static InputCoalescer ControllerInputCoalescer = new InputCoalescer();
		//input state which has been destined for client hotkey consumption are colesced here
		public static InputCoalescer HotkeyCoalescer = new InputCoalescer();

		public static UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static StickyXORAdapter StickyXORAdapter = new StickyXORAdapter();

		/// <summary>
		/// Auto-fire (Rapid fire) of controller buttons
		/// </summary>
		public static AutoFireAdapter AutoFireAdapter = new AutoFireAdapter();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static SimpleController MovieOutputController = new SimpleController();

		public static Controller ClientControls;

		public static string GetOutputControllersAsMnemonic()
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(Global.ControllerOutput);
			return mg.GetControllersAsMnemonic();
		}

		public static DiscHopper DiscHopper = new DiscHopper();


		public static CoreAccessor PsxCoreLibrary = new CoreAccessor(new Win32LibAccessor("PsxHawk.Core.dll"));

	}
}