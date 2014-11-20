using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
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
		public static Rewinder Rewinder;

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
		public static MultitrackRewiringControllerAdapter MultitrackRewiringAdapter = new MultitrackRewiringControllerAdapter();

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

		public static DiscHopper DiscHopper = new DiscHopper();

		public static UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		public static AutoFireStickyXorAdapter AutofireStickyXORAdapter = new AutoFireStickyXorAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static StickyXorAdapter StickyXORAdapter = new StickyXorAdapter();

		/// <summary>
		/// Used to AND to another controller, used for Joypad.Set()
		/// </summary>
		public static OverrideAdaptor LuaAndAdaptor = new OverrideAdaptor();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static SimpleController MovieOutputController = new SimpleController();

		public static Controller ClientControls;

		// Input state which has been estine for game controller inputs are coalesce here
		// This relies on a client specific implementation!
		public static SimpleController ControllerInputCoalescer;

		public static SystemInfo SystemInfo
		{
			get
			{
				switch(Global.Emulator.SystemId)
				{ 
					default:
					case "NULL":
						return SystemInfo.Null;
					case "NES":
						return SystemInfo.Nes;
					case "INTV":
						return SystemInfo.Intellivision;
					case "SG":
						return SystemInfo.SG;
					case "SMS":
						if ((Global.Emulator as SMS).IsGameGear)
						{
							return SystemInfo.GG;
						}
						else if ((Global.Emulator as SMS).IsSG1000)
						{
							return SystemInfo.SG;
						}

						return SystemInfo.SMS;
					case "PCECD":
						return SystemInfo.PCECD;
					case "PCE":
						return SystemInfo.PCE;
					case "SGX":
						return SystemInfo.SGX;
					case "GEN":
						return SystemInfo.Genesis;
					case "TI83":
						return SystemInfo.TI83;
					case "SNES":
						return SystemInfo.SNES;
					case "GB":
						if ((Global.Emulator as Gameboy).IsCGBMode())
						{
							return SystemInfo.GBC;
						}

						return SystemInfo.GB;
					case "A26":
						return SystemInfo.Atari2600;
					case "A78":
						return SystemInfo.Atari7800;
					case "C64":
						return SystemInfo.C64;
					case "Coleco":
						return SystemInfo.Coleco;
					case "GBA":
						return SystemInfo.GBA;
					case "N64":
						return SystemInfo.N64;
					case "SAT":
						return SystemInfo.Saturn;
					case "DGB":
						return SystemInfo.DualGB;
					case "WSWAN":
						return SystemInfo.WonderSwan;
					case "LYNX":
						return SystemInfo.Lynx;
				}
			}
		}
	}
}
