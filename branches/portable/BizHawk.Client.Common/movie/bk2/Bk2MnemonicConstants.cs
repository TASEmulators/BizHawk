using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class Bk2MnemonicConstants
	{
		public char this[string button]
		{
			get
			{
				var key = button
					.Replace("P1 ", "")
					.Replace("P2 ", "")
					.Replace("P3 ", "")
					.Replace("P4 ", "")
					.Replace("Key", "")
					.Replace("Key ", "");

				if (SystemOverrides.ContainsKey(Global.Emulator.SystemId) && SystemOverrides[Global.Emulator.SystemId].ContainsKey(key))
				{
					return SystemOverrides[Global.Emulator.SystemId][key];
				}
				else if (BaseMnemonicLookupTable.ContainsKey(key))
				{
					return BaseMnemonicLookupTable[key];
				}
				else if (key.Length == 1)
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

				{ "L", 'l' },
				{ "R", 'r' },

				{ "L1", 'l' },
				{ "R1", 'r' },

				{ "Button", 'B' },
				{ "B1", '1' },
				{ "B2", '2' },

				{ "Trigger", '1' },
				{ "Trigger 1", '1' },
				{ "Trigger 2", '2' },

				{"Mode", 'M'},

				{ "Fire", 'F' },
				{ "Microphone", 'M' },

				{ "Star", '*' },
				{ "Pound", '#' },

				{ "P2 X1", '1' },
				{ "P2 X2", '2' },
				{ "P2 X3", '3' },
				{ "P2 X4", '4' },
				
				{ "P2 Y1", '1' },
				{ "P2 Y2", '2' },
				{ "P2 Y3", '3' },
				{ "P2 Y4", '4' },
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
						{ "VS Coin 1", 'c' },
						{ "VS Coin 2", 'C' },
					}
				},
				{
					"TI-83",
					new Dictionary<string, char>
					{
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
						{"Key F1", '1' },
						{"Key F3", '3' },
						{"Key F5", '5' },
						{"Key F7", '7' },
						{"Key Left Arrow", 'l' },
						{"Key Plus", '+' },
						{"Key Minus", '-' },
						{"Key Pound", 'l' },
						{"Key Clear/Home", 'c' },
						{"Key Insert/Delete", 'i' }, 
						{"Key Control", 'c' },
						{"Key At", '@' },
						{"Key Asterisk", '*' },
						{"Key Up Arrow", 'u' },
						{"Key Restore", 'r' },
						{"Key Run/Stop", 's' },
						{"Key Lck", 'k' },
						{"Key Colon", ':' },
						{"Key Semicolon", ';' },
						{"Key Equal", '=' },
						{"Key Return", 'e'}, 
						{"Key Commodore", 'o' },
						{"Key Left Shift", 's' }, 
						{"Key Comma", ',' },
						{"Key Period", '>' },
						{"Key Slash", '/' },
						{"Key Right Shift", 's' },
						{"Key Cursor Up/Down", 'u' },
						{"Key Cursor Left/Right", 'l' }, 
						{"Key Space", '_' }
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

						{ "DPad Up", 'U' },
						{ "DPad Down", 'D' },
						{ "DPad Left", 'L' },
						{ "DPad Right", 'R' },
					}
				}
			};
	}
}
