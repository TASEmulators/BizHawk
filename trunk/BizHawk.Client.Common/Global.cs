using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator;
		public static CoreComm CoreComm;

		public static Config Config;
		public static GameInfo Game;
		public static CheatCollection CheatList;
		public static FirmwareManager FirmwareManager;

		//Movie

		/// <summary>
		/// the global MovieSession can use this to deal with multitrack player remapping (should this be here? maybe it should be in MovieSession)
		/// </summary>
		public static MultitrackRewiringControllerAdapter MultitrackRewiringControllerAdapter = new MultitrackRewiringControllerAdapter();
		public static MovieSession MovieSession = new MovieSession();

		/// <summary>
		/// whether throttling is force-disabled by use of fast forward
		/// </summary>
		public static bool ForceNoThrottle;

		public static Controller NullControls;
		public static AutofireController AutofireNullControls;

		//the movie will be spliced inbetween these if it is present
		public static CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();
		public static CopyControllerAdapter MovieOutputHardpoint = new CopyControllerAdapter();

		//dont take my word for it, since the final word is actually in RewireInputChain, but here is a guide...
		//user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
		//.. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
		//(1)->Input Display

		//the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController;

		//rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public static AutofireController AutoFireController;

		//the "output" port for the controller chain.
		public static CopyControllerAdapter ControllerOutput = new CopyControllerAdapter();


		public static string GetOutputControllersAsMnemonic()
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(ControllerOutput);
			return mg.GetControllersAsMnemonic();
		}

		public static DiscHopper DiscHopper = new DiscHopper();

		public static UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		public static AutoFireStickyXorAdapter AutofireStickyXORAdapter = new AutoFireStickyXorAdapter();

		/// <summary>
		/// will OR together two IControllers
		/// </summary>
		public static ORAdapter OrControllerAdapter = new ORAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static StickyXorAdapter StickyXORAdapter = new StickyXorAdapter();

		/// <summary>
		/// Forces any controller button to Off, useful for things like Joypad.Set
		/// </summary>
		public static ForceOffAdaptor ForceOffAdaptor = new ForceOffAdaptor();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static SimpleController MovieOutputController = new SimpleController();

		public static Controller ClientControls;
	}
}
