using System;
using BizHawk.DiscSystem;
using System.Collections.Generic;
#if WINDOWS
using SlimDX.Direct3D9;
using SlimDX.DirectSound;

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
		public static CoreComm CoreComm;
		public static GameInfo Game;
		public static CheatList CheatList;

		public static Controller NullControls;
		public static AutofireController AutofireNullControls;

		public static Controller NESControls;
		public static AutofireController AutofireNESControls;

		public static Controller SNESControls;
		public static AutofireController AutofireSNESControls;

		public static Controller GBControls;
		public static AutofireController AutofireGBControls;

		public static Controller DualGBControls;
		public static AutofireController DualAutofireGBControls;

		public static Controller GBAControls;
		public static AutofireController AutofireGBAControls;

		public static Controller PCEControls;
		public static AutofireController AutofirePCEControls;

		public static Controller SMSControls;
		public static AutofireController AutofireSMSControls;

		public static Controller GenControls;
		public static AutofireController AutofireGenControls;

		public static Controller TI83Controls;

		public static Controller Atari2600Controls;
		public static AutofireController AutofireAtari2600Controls;

		public static Controller Atari7800Controls;
		public static AutofireController AutofireAtari7800Controls;

		public static Controller ColecoControls;
		public static AutofireController AutofireColecoControls;

		public static Controller SaturnControls;
		public static AutofireController AutofireSaturnControls;

		public static Controller IntellivisionControls;
		public static AutofireController AutofireIntellivisionControls;

		public static Controller Commodore64Controls;
		public static AutofireController AutofireCommodore64Controls;

		public static Controller N64Controls;
		public static AutofireController AutofireN64Controls;

		public static readonly Dictionary<string, Dictionary<string, string>> BUTTONS = new Dictionary<string, Dictionary<string, string>>
		{
			{
				"Gameboy Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}
				}
			},
			{
				"GBA Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}, {"L", "L"}, {"R", "R"}
				}
			},
			{
				"Genesis 3-Button Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Start", "S"}, {"A", "A"}, {"B", "B"},
					{"C", "C"}
				}
			},
			{
				"NES Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}
				}
			},
			{
				"SNES Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"},
					{"A", "A"}, {"X", "X"}, {"Y", "Y"}, {"L", "L"}, {"R", "R"}
				}
			},
			{
				"PC Engine Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Run", "r"}, {"B2", "2"},
					{"B1", "1"}
				}
			},
			{
				"SMS Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"B1", "1"}, {"B2", "2"}
				}
			},
			{
				"TI83 Controller", new Dictionary<string, string>
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
				"Atari 2600 Basic Controller", new Dictionary<string,string>
				{	
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Button", "B"}
				}
			},
			{
				"Atari 7800 ProLine Joystick Controller", new Dictionary<string,string>
				{	
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Trigger", "1"}, {"Trigger 2", "2"}
				}
			},
			{
				"Commodore 64 Controller", new Dictionary<string,string>
				{	
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Button", "B"}
				}
			},
			{
				"Commodore 64 Keyboard", new Dictionary<string,string>
				{	
					{"Key F1", "1"}, {"Key F3", "3"}, {"Key F5", "5"}, {"Key F7", "7"},
					{"Key Left Arrow", "l"}, {"Key 1", "1"}, {"Key 2", "2"}, {"Key 3", "3"}, {"Key 4", "4"}, {"Key 5", "5"}, {"Key 6", "6"}, {"Key 7", "7"}, {"Key 8", "8"}, {"Key 9", "9"}, {"Key 0", "0"}, {"Key Plus", "+"}, {"Key Minus", "-"}, {"Key Pound", "l"}, {"Key Clear/Home", "c"}, {"Key Insert/Delete", "i"}, 
					{"Key Control", "c"}, {"Key Q", "Q"}, {"Key W", "W"}, {"Key E", "E"}, {"Key R", "R"}, {"Key T", "T"}, {"Key Y", "Y"}, {"Key U", "U"}, {"Key I", "I"}, {"Key O", "O"}, {"Key P", "P"}, {"Key At", "@"}, {"Key Asterisk", "*"}, {"Key Up Arrow", "u"}, {"Key Restore", "r"},
					{"Key Run/Stop", "s"}, {"Key Lck", "k"}, {"Key A", "A"}, {"Key S", "S"}, {"Key D", "D"}, {"Key F", "F"}, {"Key G", "G"}, {"Key H", "H"}, {"Key J", "J"}, {"Key K", "K"}, {"Key L", "L"}, {"Key Colon", ":"}, {"Key Semicolon", ";"}, {"Key Equal", "="}, {"Key Return", "e"}, 
					{"Key Commodore", "o"}, {"Key Left Shift", "s"}, {"Key Z", "Z"}, {"Key X", "X"}, {"Key C", "C"}, {"Key V", "V"}, {"Key B", "B"}, {"Key N", "N"}, {"Key M", "M"}, {"Key Comma", ","}, {"Key Period", ">"}, {"Key Slash", "/"}, {"Key Right Shift", "s"}, {"Key Cursor Up/Down", "u"}, {"Key Cursor Left/Right", "l"}, 
					{"Key Space", "_"}
				}
			},
			{
				"ColecoVision Basic Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"L", "l"}, {"R", "r"},
					{"Key1", "1"}, {"Key2", "2"}, {"Key3", "3"}, {"Key4", "4"}, {"Key5", "5"}, {"Key6", "6"}, 
					{"Key7", "7"}, {"Key8", "8"}, {"Key9", "9"}, {"Star", "*"}, {"Key0", "0"}, {"Pound", "#"}
				}
			},
			{
				"Nintento 64 Controller", new Dictionary<string, string>()
				{
					{"DPad U", "U"}, {"DPad D", "D"}, {"DPad L", "L"}, {"DPad R", "R"},
					{"A", "A"}, {"B", "B"}, {"Z", "Z"}, {"Start", "S"},
					{"C Up", "u"}, {"C Down", "d"}, {"C Left", "l"}, {"C Right", "r"}
				}
			}
		};

		public static readonly Dictionary<string, Dictionary<string, string>> COMMANDS = new Dictionary<string, Dictionary<string, string>>
		{
			{"Atari 2600 Basic Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Select", "s"}}},
			{"Atari 7800 ProLine Joystick Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Select", "s"}}},
			{"Gameboy Controller", new Dictionary<string, string> {{"Power", "P"}}},
			{"GBA Controller", new Dictionary<string, string> {{"Power", "P"}}},
			{"Genesis 3-Button Controller", new Dictionary<string, string> {{"Reset", "r"}}},
			{"NES Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Power", "P"}, {"FDS Eject", "E"}, {"FDS Insert 0", "0"}, {"FDS Insert 1", "1"}, {"VS Coin 1", "c"}, {"VS Coin 2", "C"}}},
			{"SNES Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"PC Engine Controller", new Dictionary<string, string> {}},
			{"SMS Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
			{"TI83 Controller", new Dictionary<string, string> {}},
			{"Nintento 64 Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
		};

		public static readonly Dictionary<string, int> PLAYERS = new Dictionary<string, int>
		{
			{"Gameboy Controller", 1}, {"GBA Controller", 1}, {"Genesis 3-Button Controller", 2}, {"NES Controller", 4},
			{"SNES Controller", 4}, {"PC Engine Controller", 5}, {"SMS Controller", 2}, {"TI83 Controller", 1}, {"Atari 2600 Basic Controller", 2}, {"Atari 7800 ProLine Joystick Controller", 2},
			{"ColecoVision Basic Controller", 2}, {"Commodore 64 Controller", 2}, {"Nintento 64 Controller", 4}
		};

		// just experimenting with different possibly more painful ways to handle mnemonics
		// |P|UDLRsSBA|
		public static Tuple<string, char>[] DGBMnemonic = new Tuple<string, char>[]
		{
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P1 Power", 'P'),
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P1 Up", 'U'),
			new Tuple<string, char>("P1 Down", 'D'),
			new Tuple<string, char>("P1 Left", 'L'),
			new Tuple<string, char>("P1 Right", 'R'),
			new Tuple<string, char>("P1 Select", 's'),
			new Tuple<string, char>("P1 Start", 'S'),
			new Tuple<string, char>("P1 B", 'B'),
			new Tuple<string, char>("P1 A", 'A'),
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P2 Power", 'P'),
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P2 Up", 'U'),
			new Tuple<string, char>("P2 Down", 'D'),
			new Tuple<string, char>("P2 Left", 'L'),
			new Tuple<string, char>("P2 Right", 'R'),
			new Tuple<string, char>("P2 Select", 's'),
			new Tuple<string, char>("P2 Start", 'S'),
			new Tuple<string, char>("P2 B", 'B'),
			new Tuple<string, char>("P2 A", 'A'),
			new Tuple<string, char>(null, '|')
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

		public static string GetOutputControllersAsMnemonic()
		{
			MnemonicsGenerator mg = new MnemonicsGenerator();
			mg.SetSource(ControllerOutput);
			return mg.GetControllersAsMnemonic();
		}

		public static DiscHopper DiscHopper = new DiscHopper();
	}
}
