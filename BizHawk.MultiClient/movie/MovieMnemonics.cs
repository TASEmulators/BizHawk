using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
	public static class MnemonicConstants
	{
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
					{"B", "B"}, {"A", "A"}, {"Z", "Z"}, {"Start", "S"}, {"L", "L"}, {"R", "R"},
					{"C Up", "u"}, {"C Down", "d"}, {"C Left", "l"}, {"C Right", "r"}
				}
			},
			{
				"Saturn Controller", new Dictionary<string, string>()
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
			{"NES Controller", new Dictionary<string, string> {{"Reset", "r"}, {"Power", "P"}, {"FDS Eject", "E"}, {"FDS Insert 0", "0"}, {"FDS Insert 1", "1"}, {"VS Coin 1", "c"}, {"VS Coin 2", "C"}}},
			{"SNES Controller", new Dictionary<string, string> {{"Power", "P"}, {"Reset", "r"}}},
			{"PC Engine Controller", new Dictionary<string, string> {}},
			{"SMS Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
			{"TI83 Controller", new Dictionary<string, string> {}},
			{"Nintento 64 Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
			{"Saturn Controller", new Dictionary<string, string> {{"Pause", "p"}, {"Reset", "r"}}},
		};

		public static readonly Dictionary<string, int> PLAYERS = new Dictionary<string, int>
		{
			{"Gameboy Controller", 1}, {"GBA Controller", 1}, {"Genesis 3-Button Controller", 2}, {"NES Controller", 4},
			{"SNES Controller", 4}, {"PC Engine Controller", 5}, {"SMS Controller", 2}, {"TI83 Controller", 1}, {"Atari 2600 Basic Controller", 2}, {"Atari 7800 ProLine Joystick Controller", 2},
			{"ColecoVision Basic Controller", 2}, {"Commodore 64 Controller", 2}, {"Nintento 64 Controller", 4}, {"Saturn Controller", 2}
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
	}

	public class MnemonicsGenerator
	{
		IController Source;
		public void SetSource(IController source)
		{
			Source = source;
			ControlType = source.Type.Name;
		}
		string ControlType;

		bool IsBasePressed(string name)
		{
			bool ret = Source.IsPressed(name);
			return ret;
		}

		float GetBaseFloat(string name)
		{
			return Source.GetFloat(name);
		}

		public bool IsEmpty
		{
			get
			{
				string empty = GetEmptyMnemonic;
				string input = GetControllersAsMnemonic();
				if (empty == input)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public string GetEmptyMnemonic
		{
			get
			{
				switch (Global.Emulator.SystemId)
				{
					default:
					case "NULL":
						return "|.|";
					case "A26":
						return "|..|.....|.....|";
					case "A78":
						return "|....|......|......|";
					case "TI83":
						return "|..................................................|.|";
					case "NES":
						return "|.|........|........|........|........|";
					case "SNES":
						return "|.|............|............|............|............|";
					case "SMS":
					case "GG":
					case "SG":
						return "|......|......|..|";
					case "GEN":
						return "|.|........|........|";
					case "GB":
						return "|.|........|";
					case "DGB":
						return "|.|........|.|........|";
					case "PCE":
					case "PCECD":
					case "SGX":
						return "|.|........|........|........|........|........|";
					case "Coleco":
						return "|..................|..................|";
					case "C64":
						return "|.....|.....|..................................................................|";
					case "GBA":
						return "|.|..........|";
					case "N64":
						return "|.|............|............|............|............|";
					case "SAT":
						return "|.|.............|.............|";
				}
			}
		}

		private string GetGBAControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
			}
			else
			{
				input.Append(".");
			}
			input.Append("|");
			foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
			{
				input.Append(IsBasePressed(button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
			}
			input.Append("|");
			return input.ToString();
		}

		//adelikat: I"m going to do all controllers like this, so what if it is redundant! It is better than reducing lines of code with convoluted logic that is difficult to expand to support new platforms
		private string GetSNESControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
			}
			else
			{
				input.Append('.');
			}

			input.Append("|");
			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}

			return input.ToString();
		}

		private string GetC64ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			foreach (string button in MnemonicConstants.BUTTONS["Commodore 64 Keyboard"].Keys)
			{
				input.Append(IsBasePressed(button) ? MnemonicConstants.BUTTONS["Commodore 64 Keyboard"][button] : ".");
			}
			input.Append('|');

			input.Append('|');
			return input.ToString();
		}

		private string GetDualGameBoyControllerAsMnemonic()
		{
			// |.|........|.|........|
			StringBuilder input = new StringBuilder();

			foreach (var t in MnemonicConstants.DGBMnemonic)
			{
				if (t.Item1 != null)
					input.Append(IsBasePressed(t.Item1) ? t.Item2 : '.');
				else
					input.Append(t.Item2); // seperator
			}
			return input.ToString();
		}


		private string GetA78ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			input.Append(IsBasePressed("Power") ? 'P' : '.');
			input.Append(IsBasePressed("Reset") ? 'r' : '.');
			input.Append(IsBasePressed("Select") ? 's' : '.');
			input.Append(IsBasePressed("Pause") ? 'p' : '.');
			input.Append('|');
			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			return input.ToString();
		}

		private string GetN64ControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append('P');
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append('r');
			}
			else
			{
				input.Append('.');
			}
			input.Append('|');

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}

				if (MnemonicConstants.ANALOGS[ControlType].Keys.Count > 0)
				{
					foreach (string name in MnemonicConstants.ANALOGS[ControlType].Keys)
					{
						int val = 0;
						
						//Nasty hackery
						if (name == "X Axis")
						{
							if (IsBasePressed("P" + player + " A Up"))
							{
								val = 127;
							}
							else if (IsBasePressed("P" + player + " A Down"))
							{
								val = -127;
							}
							else
							{
								val = (int)GetBaseFloat("P" + player + " " + name);
							}
						}
						else if (name == "Y Axis")
						{
							if (IsBasePressed("P" + player + " A Left"))
							{
								val = -127;
							}
							else if (IsBasePressed("P" + player + " A Right"))
							{
								val = 127;
							}
							else
							{
								val = (int)GetBaseFloat("P" + player + " " + name);
							}
						}
						else
						{
							val = (int)GetBaseFloat("P" + player + " " + name);
						}

						if (val != 0)
						{
							input.Append(String.Format("{0:000}", val)).Append(',');
						}
					}
					input.Remove(input.Length - 1, 1);
				}

				input.Append('|');
			}

			return input.ToString();
		}

		private string GetSaturnControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");
			if (IsBasePressed("Power"))
			{
				input.Append('P');
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append('r');
			}
			else
			{
				input.Append('.');
			}
			input.Append('|');

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed("P" + player + " " + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append('|');
			}

			return input.ToString();
		}

		public string GetControllersAsMnemonic()
		{
			if (ControlType == "Null Controller")
			{
				return "|.|";
			}
			else if (ControlType == "Atari 7800 ProLine Joystick Controller")
			{
				return GetA78ControllersAsMnemonic();
			}
			else if (ControlType == "SNES Controller")
			{
				return GetSNESControllersAsMnemonic();
			}
			else if (ControlType == "Commodore 64 Controller")
			{
				return GetC64ControllersAsMnemonic();
			}
			else if (ControlType == "GBA Controller")
			{
				return GetGBAControllersAsMnemonic();
			}
			else if (ControlType == "Dual Gameboy Controller")
			{
				return GetDualGameBoyControllerAsMnemonic();
			}
			else if (ControlType == "Nintento 64 Controller")
			{
				return GetN64ControllersAsMnemonic();
			}
			else if (ControlType == "Saturn Controller")
			{
				return GetSaturnControllersAsMnemonic();
			}
			else if (ControlType == "PSP Controller")
			{
				return "|.|"; // TODO
			}

			StringBuilder input = new StringBuilder("|");

			if (ControlType == "PC Engine Controller")
			{
				input.Append(".");
			}
			else if (ControlType == "Atari 2600 Basic Controller")
			{
				input.Append(IsBasePressed("Reset") ? "r" : ".");
				input.Append(IsBasePressed("Select") ? "s" : ".");
			}
			else if (ControlType == "NES Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
				}
				else if (IsBasePressed("FDS Eject"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["FDS Eject"]);
				}
				else if (IsBasePressed("FDS Insert 0"))
				{
					input.Append("0");
				}
				else if (IsBasePressed("FDS Insert 1"))
				{
					input.Append("1");
				}
				else if (IsBasePressed("FDS Insert 2"))
				{
					input.Append("2");
				}
				else if (IsBasePressed("FDS Insert 3"))
				{
					input.Append("3");
				}
				else if (IsBasePressed("VS Coin 1"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["VS Coin 1"]);
				}
				else if (IsBasePressed("VS Coin 2"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["VS Coin 2"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (ControlType == "Genesis 3-Button Controller")
			{
				if (IsBasePressed("Power"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Power"]);
				}
				else if (IsBasePressed("Reset"))
				{
					input.Append(MnemonicConstants.COMMANDS[ControlType]["Reset"]);
				}
				else
				{
					input.Append('.');
				}
			}
			else if (ControlType == "Gameboy Controller")
			{
				input.Append(IsBasePressed("Power") ? MnemonicConstants.COMMANDS[ControlType]["Power"] : ".");
			}

			if (ControlType != "SMS Controller" && ControlType != "TI83 Controller" && ControlType != "ColecoVision Basic Controller")
			{
				input.Append("|");
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player.ToString() + " ";
				}
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed(prefix + button) ? MnemonicConstants.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "SMS Controller")
			{
				foreach (string command in MnemonicConstants.COMMANDS[ControlType].Keys)
				{
					input.Append(IsBasePressed(command) ? MnemonicConstants.COMMANDS[ControlType][command] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "TI83 Controller")
			{
				input.Append(".|"); //TODO: perhaps ON should go here?
			}
			return input.ToString();
		}
	}


	public class MovieControllerAdapter : IController
	{
		//IController implementation:
		public ControllerDefinition Type { get; set; }
		public bool this[string button] { get { return MyBoolButtons[button]; } }
		public bool IsPressed(string button) { return MyBoolButtons[button]; }
		public float GetFloat(string name) { return MyFloatControls[name]; }
		public void UpdateControls(int frame) { }
		//--------

		private readonly WorkingDictionary<string, bool> MyBoolButtons = new WorkingDictionary<string, bool>();
		private readonly WorkingDictionary<string, float> MyFloatControls = new WorkingDictionary<string, float>();

		void Force(string button, bool state)
		{
			MyBoolButtons[button] = state;
		}

		void Force(string name, float state)
		{
			MyFloatControls[name] = state;
		}

		string ControlType { get { return Type.Name; } }

		class MnemonicChecker
		{
			private readonly string m;

			public MnemonicChecker(string _m)
			{
				m = _m;
			}

			public bool this[int c]
			{
				get { return m[c] != '.'; }
			}
		}

		/// <summary>
		/// latches one player from the source
		/// </summary>
		public void LatchPlayerFromSource(IController playerSource, int playerNum)
		{
			foreach (string button in playerSource.Type.BoolButtons)
			{
				ButtonNameParser bnp = ButtonNameParser.Parse(button);
				if (bnp == null) continue;
				if (bnp.PlayerNum != playerNum) continue;
				bool val = playerSource[button];
				MyBoolButtons[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the provided source
		/// </summary>
		public void LatchFromSource(IController source)
		{
			foreach (string button in Type.BoolButtons)
			{
				MyBoolButtons[button] = source[button];
			}

			foreach (string name in Type.FloatControls)
			{
				MyFloatControls[name] = source.GetFloat(name);
			}
		}

		//Redundancy beats crazy if logic that makes new consoles annoying to add

		private void SetGBAControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();
			if (mnemonic.Length < 2)
			{
				return;
			}
			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			int start = 3;
			foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
			{
				Force(button, c[start++]);
			}
		}

		private void SetSNESControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + MnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetN64ControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + MnemonicConstants.ANALOGS[ControlType].Count * 3 + 1);

				if (mnemonic.Length < srcindex + 3 + MnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}

				foreach (string name in MnemonicConstants.ANALOGS[ControlType].Keys)
				{
					if (InputValidate.IsValidUnsignedNumber(mnemonic.Substring(srcindex + start, 3)))
					{
						Force("P" + player + " " + name, Int32.Parse(mnemonic.Substring(srcindex + start, 3)) - 128);
					}

					start += 3;
				}
			}
		}

		private void SetSaturnControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 3 + MnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetAtari7800AsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 5)
			{
				return;
			}
			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			if (mnemonic[2] == 'r')
			{
				Force("Reset", true);
			}
			if (mnemonic[3] == 's')
			{
				Force("Select", true);
			}
			if (mnemonic[4] == 'p')
			{
				Force("Pause", true);
			}

			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + 1);
				int start = 6;
				if (mnemonic.Length < srcindex + start + MnemonicConstants.BUTTONS[ControlType].Count)
				{
					return;
				}

				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}
		}

		private void SetDualGameBoyControllerAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();
			for (int i = 0; i < MnemonicConstants.DGBMnemonic.Length; i++)
			{
				var t = MnemonicConstants.DGBMnemonic[i];
				if (t.Item1 != null)
					Force(t.Item1, c[i]);
			}
		}

		private void SetC64ControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();


			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + 1);

				if (mnemonic.Length < srcindex + 1 + MnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 1;
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}

			int startk = 13;
			foreach (string button in MnemonicConstants.BUTTONS["Commodore 64 Keyboard"].Keys)
			{
				Force(button, c[startk++]);
			}
		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			if (ControlType == "Null Controller")
			{
				return;
			}
			else if (ControlType == "SNES Controller")
			{
				SetSNESControllersAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "Commodore 64 Controller")
			{
				SetC64ControllersAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "GBA Controller")
			{
				SetGBAControllersAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "Atari 7800 ProLine Joystick Controller")
			{
				SetAtari7800AsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "Dual Gameboy Controller")
			{
				SetDualGameBoyControllerAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "Nintento 64 Controller")
			{
				SetN64ControllersAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "Saturn Controller")
			{
				SetSaturnControllersAsMnemonic(mnemonic);
				return;
			}
			else if (ControlType == "PSP Controller")
			{
				// TODO
				return;
			}

			MnemonicChecker c = new MnemonicChecker(mnemonic);

			MyBoolButtons.Clear();

			int start = 3;
			if (ControlType == "NES Controller")
			{
				if (mnemonic.Length < 2)
				{
					return;
				}
				else if (mnemonic[1] == 'P')
				{
					Force("Power", true);
				}
				else if (mnemonic[1] == 'E')
				{
					Force("FDS Eject", true);
				}
				else if (mnemonic[1] == '0')
				{
					Force("FDS Insert 0", true);
				}
				else if (mnemonic[1] == '1')
				{
					Force("FDS Insert 1", true);
				}
				else if (mnemonic[1] == '2')
				{
					Force("FDS Insert 2", true);
				}
				else if (mnemonic[1] == '3')
				{
					Force("FDS Insert 3", true);
				}
				else if (mnemonic[1] == 'c')
				{
					Force("VS Coin 1", true);
				}
				else if (mnemonic[1] == 'C')
				{
					Force("VS Coin 2", true);
				}
				else if (mnemonic[1] != '.')
				{
					Force("Reset", true);
				}
			}
			if (ControlType == "Gameboy Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Power", mnemonic[1] != '.');
			}
			if (ControlType == "Genesis 3-Button Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.');
			}
			if (ControlType == "SMS Controller" || ControlType == "TI83 Controller" || ControlType == "ColecoVision Basic Controller")
			{
				start = 1;
			}
			if (ControlType == "Atari 2600 Basic Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0');
				Force("Select", mnemonic[2] != '.' && mnemonic[2] != '0');
				start = 4;
			}
			for (int player = 1; player <= MnemonicConstants.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (MnemonicConstants.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				if (mnemonic.Length < srcindex + ctr + MnemonicConstants.BUTTONS[ControlType].Count - 1)
				{
					return;
				}
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player + " ";
				}
				foreach (string button in MnemonicConstants.BUTTONS[ControlType].Keys)
				{
					Force(prefix + button, c[srcindex + ctr++]);
				}
			}
			if (ControlType == "SMS Controller")
			{
				int srcindex = MnemonicConstants.PLAYERS[ControlType] * (MnemonicConstants.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				foreach (string command in MnemonicConstants.COMMANDS[ControlType].Keys)
				{
					Force(command, c[srcindex + ctr++]);
				}
			}
		}
	}

}
