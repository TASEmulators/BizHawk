using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class Bk2MnemonicConstants
	{
		public char this[string button]
		{
			get
			{
				var key = button.Replace("Key ", "");
				if (key.StartsWith("P") && key.Length > 1 && key[1] >= '0' && key[1] <= '9')
					key = key.Substring(3);

				if (SystemOverrides.ContainsKey(Global.Emulator.SystemId) && SystemOverrides[Global.Emulator.SystemId].ContainsKey(key))
				{
					return SystemOverrides[Global.Emulator.SystemId][key];
				}

				if (BaseMnemonicLookupTable.ContainsKey(key))
				{
					return BaseMnemonicLookupTable[key];
				}

				if (key.Length == 1)
				{
					return key[0];
				}

				return '!';
			}
		}

		private readonly Dictionary<string, char> BaseMnemonicLookupTable = new Dictionary<string, char>
			{
				{ "Power", 'P' },
				{ "Reset", 'r' },
				{ "Pause", 'p' },
				{ "Rotate", 'R' },

				{ "Up", 'U' },
				{ "Down", 'D' },
				{ "Left", 'L' },
				{ "Right", 'R' },

				{ "A", 'A' },
				{ "B", 'B' },
				{ "C", 'C' },

				{ "X", 'X' },
				{ "Y", 'Y' },
				{ "Z", 'Z' },

				{ "Select", 's' },
				{ "Start", 'S' },
				{ "Run", 'R' },

				{ "L", 'l' },
				{ "R", 'r' },

				{ "L1", 'l' },
				{ "R1", 'r' },

				{ "L2", 'L' },
				{ "R2", 'R' },

				{ "L3", '<' },
				{ "R3", '>' },

				{ "Button", 'B' },
				{ "B1", '1' },
				{ "B2", '2' },

				{ "Trigger", '1' },
				{ "Trigger 1", '1' },
				{ "Trigger 2", '2' },

				{ "Mouse Left", 'l' },
				{ "Mouse Right", 'r' },
				{ "Mouse Center", 'c' },
				{ "Mouse Start", 's' },

				{"Mode", 'M'},
				{"MODE", 'M'},

				{ "Fire", 'F' },
				{ "Lightgun Trigger", 'T' },
				{ "Lightgun Start", 'S' },
				{ "Microphone", 'M' },

				{ "Star", '*' },
				{ "Pound", '#' },

				{ "X1", '1' },
				{ "X2", '2' },
				{ "X3", '3' },
				{ "X4", '4' },
				
				{ "Y1", '1' },
				{ "Y2", '2' },
				{ "Y3", '3' },
				{ "Y4", '4' },

				{ "Triangle", 'T' },
				{ "Circle", 'O' },
				{ "Cross", 'X' },
				{ "Square", 'Q' },

				{ "Toggle Left Difficulty", 'l' },
				{ "Toggle Right Difficulty", 'r' },

				{ "Open", 'O' },
				{ "Close", 'C' }
			};

		private readonly Dictionary<string, Dictionary<string, char>> SystemOverrides = new Dictionary<string, Dictionary<string, char>>
			{
				{
					"NES",
					new Dictionary<string, char>
					{
						{ "FDS Eject", 'E' },
						{ "FDS Insert 0", '0' },
						{ "FDS Insert 1", '1' },
						{ "Insert Coin P1", 'c' },
						{ "Insert Coin P2", 'C' },
						{ "Service Switch", 'w' },

						{ "PP1", '1' },
						{ "PP2", '2' },
						{ "PP3", '3' },
						{ "PP4", '4' },

						{ "PP5", '5' },
						{ "PP6", '6' },
						{ "PP7", '7' },
						{ "PP8", '8' },

						{ "PP9", '9' },
						{ "PP10", 'A' },
						{ "PP11", 'B' },
						{ "PP12", 'C' },
						{ "Click", 'C' },
						{ "Touch", 'T' },
					}
				},
				{
					"TI83",
					new Dictionary<string, char>
					{
						{ "UP", 'U'},
						{ "DOWN", 'D'},
						{ "LEFT", 'L'},
						{ "RIGHT", 'R'},
						{ "DOT", '`' },
						{ "ON", 'O' },
						{ "ENTER", '=' },
						{ "PLUS", '+' },
						{ "MINUS", '_' },
						{ "MULTIPLY", '*' },
						{ "DIVIDE", '/' },
						{ "CLEAR", 'c' },
						{ "EXP", '^' },
						{ "DASH", '-' },
						{ "PARAOPEN", '('},
						{ "PARACLOSE", ')'},
						{ "TAN", 'T' },
						{ "VARS", 'V' },
						{ "COS", 'C' },
						{ "PRGM", 'P' },
						{ "STAT", 's' },
						{ "MATRIX", 'm' },
						{ "X", 'X' },
						{ "STO", '>' },
						{ "LN", 'n' },
						{ "LOG", 'L' },
						{ "SQUARED", '2' },
						{ "NEG1", '1' },
						{ "MATH", 'H' },
						{ "ALPHA", 'A' },
						{ "GRAPH", 'G' },
						{ "TRACE", 't' },
						{ "ZOOM", 'Z' },
						{ "WINDOW", 'W' },
						{ "Y", 'Y' },
						{ "2ND", '&' },
						{ "MODE", 'O' },
						{ "DEL", 'D' },
						{ "COMMA", ',' },
						{ "SIN", 'S' }
					}
				},
				{
					"C64",
					new Dictionary<string, char>
					{
						{ "L", 'L' },
						{ "R", 'R' },
						{ "F1", '1' },
						{ "F3", '3' },
						{ "F5", '5' },
						{ "F7", '7' },
						{ "Left Arrow", 'l' },
						{ "Plus", '+' },
						{ "Minus", '-' },
						{ "Pound", 'l' },
						{ "Clear/Home", 'c' },
						{ "Insert/Delete", 'i' }, 
						{ "Control", 'c' },
						{ "At", '@' },
						{ "Asterisk", '*' },
						{ "Up Arrow", 'u' },
						{ "Restore", 'r' },
						{ "Run/Stop", 's' },
						{ "Lck", 'k' },
						{ "Colon", ':' },
						{ "Semicolon", ';' },
						{ "Equal", '=' },
						{ "Return", 'e'}, 
						{ "Commodore", 'o' },
						{ "Left Shift", 's' }, 
						{ "Comma", ',' },
						{ "Period", '>' },
						{ "Slash", '/' },
						{ "Right Shift", 's' },
						{ "Cursor Up/Down", 'u' },
						{ "Cursor Left/Right", 'l' }, 
						{ "Space", '_' }
					}
				},
				{
					"N64",
					new Dictionary<string, char>
					{
						{ "C Up", 'u' },
						{ "C Down", 'd' },
						{ "C Left", 'l' },
						{ "C Right", 'r' },
				  
						{ "A Up", 'U' },
						{ "A Down", 'D' },
						{ "A Left", 'L' },
						{ "A Right", 'R' },

						{ "DPad U", 'U' },
						{ "DPad D", 'D' },
						{ "DPad L", 'L' },
						{ "DPad R", 'R' },
					}
				},
				{
					"DGB",
					new Dictionary<string, char>
					{
						{ "Toggle Cable", 'L' },
					}
				},
				{
					"Lynx",
					new Dictionary<string, char>
					{
						{ "Option 1", '1' },
						{ "Option 2", '2' }
					}
				},
				{
					"AppleII",
					new Dictionary<string, char>
					{
						{ "Tab", 't' },
						{ "Return", 'e' },
						{ "Escape", 'x' },
						{ "Delete", 'b' },
						{ "Space", 's' },
						{ "Control", 'c' },
						{ "Shift", '^' },
						{ "Caps Lock", 'C' },
						{ "Next Disk", '>' },
						{ "Previous Disk", '<' },
						{ "White Apple", 'w' },
						{ "Black Apple", 'b' },
						{ "L", 'L' },
						{ "R", 'R' }
					}
				},
				{
					"INTV",
					new Dictionary<string, char>
					{
						{ "Clear", 'C' },
						{ "Enter", 'E' },
						{ "Top", 'T' },
						{ "NNE", 'n' },
						{ "NE", '/' },
						{ "ENE", 'e' },
						{ "ESE", 'e' },
						{ "SE", '\\' },
						{ "SSE", 's' },
						{ "SSW", 's' },
						{ "SW", '/' },
						{ "WSW", 'w' },
						{ "WNW", 'w' },
						{ "NW", '\\' },
						{ "NNW", 'n' }
					}
				}
			};
	}
}
