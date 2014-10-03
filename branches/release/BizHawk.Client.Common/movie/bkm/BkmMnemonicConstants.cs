using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public static class BkmMnemonicConstants
	{
		public static readonly Dictionary<string, Dictionary<string, string>> BUTTONS = new Dictionary<string, Dictionary<string, string>>
		{
			{
				"Gameboy Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"}, {"A", "A"}
				}
			},
			{
				"Lynx Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"Select", "s"}, {"Start", "S"}, {"B", "B"}, {"A", "A"}
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
				"GPGX Genesis Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"A", "A"}, {"B", "B"}, {"C", "C"},
					{"Start", "S"}, {"X", "X"}, {"Y", "Y"}, {"Z", "Z"}, {"Mode", "M"}
				}
			},
			{
				"GPGX 3-Button Controller", new Dictionary<string, string>
				{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"}, {"A", "A"}, {"B", "B"},
					{"C", "C"}, {"Start", "S"},
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
				"Nintento 64 Controller", new Dictionary<string, string>
					{
					{"DPad U", "U"}, {"DPad D", "D"}, {"DPad L", "L"}, {"DPad R", "R"},
					{"B", "B"}, {"A", "A"}, {"Z", "Z"}, {"Start", "S"}, {"L", "L"}, {"R", "R"},
					{"C Up", "u"}, {"C Down", "d"}, {"C Left", "l"}, {"C Right", "r"}
				}
			},
			{
				"Saturn Controller", new Dictionary<string, string>
					{
					{"Up", "U"}, {"Down", "D"}, {"Left", "L"}, {"Right", "R"},
					{"Start", "S"}, {"X", "X"}, {"Y", "Y"}, {"Z", "Z"}, {"A", "A"}, {"B", "B"}, {"C", "C"},
					{"L", "l"}, {"R", "r"},
				}
			}
		};

		public static readonly Dictionary<string, Dictionary<string, string>> ANALOGS = new Dictionary<string, Dictionary<string, string>>
		{
			{"Nintento 64 Controller", new Dictionary<string, string> {{"X Axis", "X"}, {"Y Axis", "Y"}}}
		};

		public static readonly Dictionary<string, Dictionary<string, string>> COMMANDS = new Dictionary<string, Dictionary<string, string>>
		{
			{"Atari 2600 Basic Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Select", "s"}}},
			{"Atari 7800 ProLine Joystick Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Select", "s"}}},
			{"Gameboy Controller", new Dictionary<string, string> {{"Power", "P"}}},
			{"GBA Controller", new Dictionary<string, string> {{"Power", "P"}}},
			{"Genesis 3-Button Controller", new Dictionary<string, string> {{"Reset", "r"}}},
			{"GPGX Genesis Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"NES Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Power", "P"}, {"FDS Eject", "E"}, {"FDS Insert 0", "0"}, {"FDS Insert 1", "1"}, {"VS Coin 1", "c"}, {"VS Coin 2", "C"}}},
			{"SNES Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"PC Engine Controller", new Dictionary<string, string>()},
			{"SMS Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
			{"TI83 Controller", new Dictionary<string, string>()},
			{"Nintento 64 Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"Saturn Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"GPGX 3-Button Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
		};

		public static readonly Dictionary<string, int> PLAYERS = new Dictionary<string, int>
		{
			{"Gameboy Controller", 1}, {"GBA Controller", 1}, {"Genesis 3-Button Controller", 2}, {"GPGX Genesis Controller", 2}, {"NES Controller", 4},
			{"SNES Controller", 4}, {"PC Engine Controller", 5}, {"SMS Controller", 2}, {"TI83 Controller", 1}, {"Atari 2600 Basic Controller", 2}, {"Atari 7800 ProLine Joystick Controller", 2},
			{"ColecoVision Basic Controller", 2}, {"Commodore 64 Controller", 2}, {"Nintento 64 Controller", 4}, {"Saturn Controller", 2},
			{"GPGX 3-Button Controller", 2}, { "Lynx Controller", 1 }
		};

		// just experimenting with different possibly more painful ways to handle mnemonics
		// |P|UDLRsSBA|
		public static Tuple<string, char>[] DGBMnemonic =
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

		public static Tuple<string, char>[] WSMnemonic =
		{
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P1 X1", '1'),
			new Tuple<string, char>("P1 X3", '3'),
			new Tuple<string, char>("P1 X4", '4'),
			new Tuple<string, char>("P1 X2", '2'),
			new Tuple<string, char>("P1 Y1", '1'),
			new Tuple<string, char>("P1 Y3", '3'),
			new Tuple<string, char>("P1 Y4", '4'),
			new Tuple<string, char>("P1 Y2", '2'),
			new Tuple<string, char>("P1 Start", 'S'),
			new Tuple<string, char>("P1 B", 'B'),
			new Tuple<string, char>("P1 A", 'A'),
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("P2 X1", '1'),
			new Tuple<string, char>("P2 X3", '3'),
			new Tuple<string, char>("P2 X4", '4'),
			new Tuple<string, char>("P2 X2", '2'),
			new Tuple<string, char>("P2 Y1", '1'),
			new Tuple<string, char>("P2 Y3", '3'),
			new Tuple<string, char>("P2 Y4", '4'),
			new Tuple<string, char>("P2 Y2", '2'),
			new Tuple<string, char>("P2 Start", 'S'),
			new Tuple<string, char>("P2 B", 'B'),
			new Tuple<string, char>("P2 A", 'A'),
			new Tuple<string, char>(null, '|'),
			new Tuple<string, char>("Power", 'P'),
			new Tuple<string, char>("Rotate", 'R'),
			new Tuple<string, char>(null, '|')
		};
	}
}
