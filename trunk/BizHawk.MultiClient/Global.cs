using BizHawk.DiscSystem;
using System.Collections.Generic;
#if WINDOWS
using SlimDX.Direct3D9;
using SlimDX.DirectSound;
using System.Drawing;
#endif

namespace BizHawk.MultiClient
{
	public static class Global
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
		public static Config Config;
		public static IEmulator Emulator;
		public static CoreInputComm CoreInputComm;
		public static GameInfo Game;
		public static Controller SMSControls;
		public static Controller PCEControls;
		public static Controller GenControls;
		public static Controller TI83Controls;
		public static Controller NESControls;
		public static Controller SNESControls;
		public static Controller GBControls;
		public static Controller Atari2600Controls;
		public static Controller NullControls;
		public static Controller ColecoControls;
		public static Controller Commodore64Controls;
		public static CheatList CheatList;

		public static AutofireController AutofireNullControls;
		public static AutofireController AutofireNESControls;
		public static AutofireController AutofireSNESControls;
		public static AutofireController AutofireSMSControls;
		public static AutofireController AutofirePCEControls;
		public static AutofireController AutofireGBControls;
		public static AutofireController AutofireGenControls;
		public static AutofireController AutofireAtari2600Controls;
		public static AutofireController AutofireCommodore64Controls;

		public static readonly Dictionary<string, Dictionary<string, string>> BUTTONS = new Dictionary<string, Dictionary<string, string>>()
		{
			{
				"Gameboy Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}
				}
			},
			{
				"Genesis 3-Button Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Start", "S"}, {"A", "A"}, {"B", "B"},
					{"C", "C"}
				}
			},
			{
				"NES Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}
				}
			},
			{
				"SNES Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}, {"X", "X"}, {"Y", "Y"}, {"L", "L"}, {"R", "R"}, 
				}
			},
			{
				"PC Engine Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Run", "r"}, {"B2", "2"},
					{"B1", "1"}
				}
			},
			{
				"SMS Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"B1", "1"}, {"B2", "2"}
				}
			},
			{
				"TI83 Controller", new Dictionary<string, string>()
				{
					{"0", "0"}, {"1", "1"}, {"2", "2"}, {"3", "3"}, {"4", "4"}, {"5", "5"}, {"6", "6"}, {"7", "7"},
					{"8", "8"}, {"9", "9"}, {"DOT", "`"}, {"ON", "O"}, {"ENTER", "="}, {"UP", "U"}, {"DOWN", "D"},
					{"LEFT", "L"}, {"RIGHT", "R"}, {"PLUS", "+"}, {"MINUS", "_"}, {"MULTIPLY", "*"}, {"DIVIDE", "/"},
 					{"CLEAR", "c"}, {"EXP", "^"}, {"DASH", "-"}, {"PARAOPEN", "("}, {"PARACLOSE", ")"}, {"TAN", "T"},
					{"VARS", "V"}, {"COS", "C"}, {"PRGM", "P"}, {"STAT", "s"}, {"MATRIX", "m"}, {"X", "X"}, {"STO", ">"},
					{"LN", "n"}, {"LOG", "L"}, {"SQUARED", "2"}, {"NEG1", "1"}, {"MATH", "H"}, {"ALPHA", "A"},
					{"GRAPH", "G"}, {"TRACE", "t"}, {"ZOOM", "Z"}, {"WINDOW", "W"}, {"Y", "Y"}, {"2ND", "&"}, {"MODE", "O"},
					{"DEL", "D"}, {"COMMA", ","}, {"SIN", "S"}
				}
			},
			{
				"Atari 2600 Basic Controller", new Dictionary<string,string>()
				{	
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Button", "B"}
				}
			},
			{
				"ColecoVision Basic Controller", new Dictionary<string, string>()
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"L1", "l"}, {"L2", "L"}, {"R1", "r"}, {"R2", "R"}, //adelikat: These mnemonics are terrible but I can't think of anything better
					{"Key1", "1"}, {"Key2", "2"}, {"Key3", "3"}, {"Key4", "4"}, {"Key5", "5"}, {"Key6", "6"}, {"Key7", "7"}, 
					{"Key8", "8"}, {"Key9", "9"}, {"Star", "*"}, {"Pound", "#"}
				}
			}
		};

		public static readonly Dictionary<string, Dictionary<string, string>> COMMANDS = new Dictionary<string, Dictionary<string, string>>()
		{
			{"Atari 2600 Basic Controller", new Dictionary<string, string>() {{"Reset", "r"}, {"Select", "s"}}},
			{"Gameboy Controller", new Dictionary<string, string>() {{"Power", "P"}}},
			{"Genesis 3-Button Controller", new Dictionary<string, string>() {{"Reset", "r"}}},
			{"NES Controller", new Dictionary<string, string>() {{"Reset", "r"}, {"Power", "P"}, {"FDS Eject", "E"}, {"FDS Insert 0", "0"}, {"FDS Insert 1", "1"}, {"VS Coin 1", "c"}, {"VS Coin 2", "C"}}},
			{"SNES Controller", new Dictionary<string, string>() {{"Power", "P"}, {"Reset", "r"}}},
			{"PC Engine Controller", new Dictionary<string, string>() {}},
			{"SMS Controller", new Dictionary<string, string>() {{"Pause", "p"}, {"Reset", "r"}}},
			{"TI83 Controller", new Dictionary<string, string>() {}}
		};

		public static readonly Dictionary<string, int> PLAYERS = new Dictionary<string, int>()
		{
			{"Gameboy Controller", 1}, {"Genesis 3-Button Controller", 2}, {"NES Controller", 4},
			{"SNES Controller", 4},
			{"PC Engine Controller", 5}, {"SMS Controller", 2}, {"TI83 Controller", 1}, {"Atari 2600 Basic Controller", 2},
			{"ColecoVision Basic Controller", 1}
		};

		/// <summary>
		/// whether throttling is force-disabled by use of fast forward
		/// </summary>
		public static bool ForceNoThrottle;

		//the movie will be spliced inbetween these if it is present
		public static CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();
		public static CopyControllerAdapter MovieOutputHardpoint = new CopyControllerAdapter();

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

		//rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public static AutofireController AutoFireController;

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

		public static AutoFireStickyXORAdapter AutofireStickyXORAdapter = new AutoFireStickyXORAdapter();

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

		public static string GetOutputControllersAsMnemonic()
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(Global.ControllerOutput);
			return mg.GetControllersAsMnemonic();
		}

		public static DiscHopper DiscHopper = new DiscHopper();
	}
}
