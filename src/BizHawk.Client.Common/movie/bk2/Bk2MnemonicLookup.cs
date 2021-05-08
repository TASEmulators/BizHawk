using System.Collections.Generic;

// ReSharper disable StyleCop.SA1509
namespace BizHawk.Client.Common
{
	internal static class Bk2MnemonicLookup
	{
		public static char Lookup(string button, string systemId)
		{
			var key = button.Replace("Key ", "");
			if (key.StartsWith("P"))
			{
				if (key.Length > 2 && key[1] == '1' && key[2] >= '0' && key[2] <= '9') // Hack to support 10-20 controllers, TODO: regex this thing instead
				{
					key = key.Substring(4);
				}
				else if (key.Length > 1 && key[1] >= '0' && key[1] <= '9')
				{
					key = key.Substring(3);
				}
			}

			if (SystemOverrides.ContainsKey(systemId) && SystemOverrides[systemId].ContainsKey(key))
			{
				return SystemOverrides[systemId][key];
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

		public static string LookupAxis(string button, string systemId)
		{
			var key = button
				.Replace("P1 ", "")
				.Replace("P2 ", "")
				.Replace("P3 ", "")
				.Replace("P4 ", "")
				.Replace("Key ", "");

			if (AxisSystemOverrides.ContainsKey(systemId) && AxisSystemOverrides[systemId].ContainsKey(key))
			{
				return AxisSystemOverrides[systemId][key];
			}

			if (BaseAxisLookupTable.ContainsKey(key))
			{
				return BaseAxisLookupTable[key];
			}

			return button;
		}

		private static readonly Dictionary<string, char> BaseMnemonicLookupTable = new Dictionary<string, char>
		{
			["Power"] = 'P',
			["Reset"] = 'r',
			["Pause"] = 'p',
			["Rotate"] = 'R',

			["Up"] = 'U',
			["Down"] = 'D',
			["Left"] = 'L',
			["Right"] = 'R',

			["A"] = 'A',
			["B"] = 'B',
			["C"] = 'C',

			["X"] = 'X',
			["Y"] = 'Y',
			["Z"] = 'Z',

			["Select"] = 's',
			["SELECT"] = 's',
			["Start"] = 'S',
			["START"] = 'S',
			["Run"] = 'R',
			["RUN"] = 'R',

			["Left Shoulder"] = 'l',
			["Right Shoulder"] = 'r',
			["L"] = 'l',
			["R"] = 'r',

			["L1"] = 'l',
			["R1"] = 'r',

			["L2"] = 'L',
			["R2"] = 'R',

			["L3"] = '<',
			["R3"] = '>',

			["Button"] = 'B',
			["Button 1"] = '1',
			["Button 2"] = '2',
			["Button 3"] = '3',
			["Button 4"] = '4',
			["Button 5"] = '5',
			["Button 6"] = '6',
			["Button 7"] = '7',
			["Button 8"] = '8',
			["Button 9"] = '9',
			["B1"] = '1',
			["B2"] = '2',

			["Trigger"] = '1',
			["Trigger 1"] = '1',
			["Trigger 2"] = '2',

			["Mouse Left"] = 'l',
			["Mouse Right"] = 'r',
			["Mouse Center"] = 'c',
			["Mouse Start"] = 's',

			["Left Button"] = 'l',
			["Middle Button"] = 'm',
			["Right Button"] = 'r',

			["Mode"] = 'M',
			["MODE"] = 'M',
			["Mode 1"] = 'M',
			["Mode 2"] = 'm',

			["Fire"] = 'F',
			["Lightgun Trigger"] = 'T',
			["Lightgun Start"] = 'S',
			["Lightgun B"] = 'B',
			["Lightgun C"] = 'C',
			["Microphone"] = 'M',

			["Star"] = '*',
			["Pound"] = '#',

			["X1"] = '1',
			["X2"] = '2',
			["X3"] = '3',
			["X4"] = '4',
				
			["Y1"] = '1',
			["Y2"] = '2',
			["Y3"] = '3',
			["Y4"] = '4',

			["Triangle"] = 'T',
			["Circle"] = 'O',
			["Cross"] = 'X',
			["Square"] = 'Q',

			["Toggle Left Difficulty"] = 'l',
			["Toggle Right Difficulty"] = 'r',
			["BW"] = 'b',

			["Open"] = 'O',
			["Close"] = 'C',
			["Pedal"] = 'P',

			["Next Disk"] = '>',
			["Previous Disk"] = '<',

			["F1"] = '1',
			["F2"] = '2',
			["F3"] = '3',
			["F4"] = '4',
			["F5"] = '5',
			["F6"] = '6',
			["F7"] = '7',
			["F8"] = '8',
			["F9"] = '9',
			["F10"] = '0'
		};

		private static readonly Dictionary<string, Dictionary<string, char>> SystemOverrides = new Dictionary<string, Dictionary<string, char>>
		{
			["NES"] = new Dictionary<string, char>
			{
				["FDS Eject"] = 'E',
				["FDS Insert 0"] = '0',
				["FDS Insert 1"] = '1',
				["Insert Coin P1"] = 'c',
				["Insert Coin P2"] = 'C',
				["Service Switch"] = 'w',

				["PP1"] = '1',
				["PP2"] = '2',
				["PP3"] = '3',
				["PP4"] = '4',

				["PP5"] = '5',
				["PP6"] = '6',
				["PP7"] = '7',
				["PP8"] = '8',

				["PP9"] = '9',
				["PP10"] = 'A',
				["PP11"] = 'B',
				["PP12"] = 'C',
				["Click"] = 'C',
				["Touch"] = 'T',
			},
			["SNES"] = new Dictionary<string, char>
			{
				["Cursor"] = 'c',
				["Turbo"] = 't',
				["Toggle Multitap"] = 't',

				["B0"] = '0',
				["B1"] = '1',
				["B2"] = '2',
				["B3"] = '3',
				["B4"] = '4',
				["B5"] = '5',
				["B6"] = '6',
				["B7"] = '7',
				["B8"] = '8',
				["B9"] = '9',
				["B10"] = 'a',
				["B11"] = 'b',
				["B12"] = 'c',
				["B13"] = 'd',
				["B14"] = 'e',
				["B15"] = 'f',
				["B16"] = 'g',
				["B17"] = 'h',
				["B18"] = 'i',
				["B19"] = 'j',
				["B20"] = 'k',
				["B21"] = 'l',
				["B22"] = 'm',
				["B23"] = 'n',
				["B24"] = 'o',
				["B25"] = 'p',
				["B26"] = 'q',
				["B27"] = 'r',
				["B28"] = 's',
				["B29"] = 't',
				["B30"] = 'u',
				["B31"] = 'v'
			},
			["TI83"] = new Dictionary<string, char>
			{
				["UP"] = 'U',
				["DOWN"] = 'D',
				["LEFT"] = 'L',
				["RIGHT"] = 'R',
				["DOT"] = '`',
				["ON"] = 'O',
				["ENTER"] = '=',
				["PLUS"] = '+',
				["MINUS"] = '_',
				["MULTIPLY"] = '*',
				["DIVIDE"] = '/',
				["CLEAR"] = 'c',
				["EXP"] = '^',
				["DASH"] = '-',
				["PARAOPEN"] = '(',
				["PARACLOSE"] = ')',
				["TAN"] = 'T',
				["VARS"] = 'V',
				["COS"] = 'C',
				["PRGM"] = 'P',
				["STAT"] = 's',
				["MATRIX"] = 'm',
				["X"] = 'X',
				["STO"] = '>',
				["LN"] = 'n',
				["LOG"] = 'L',
				["SQUARED"] = '2',
				["NEG1"] = '1',
				["MATH"] = 'H',
				["ALPHA"] = 'A',
				["GRAPH"] = 'G',
				["TRACE"] = 't',
				["ZOOM"] = 'Z',
				["WINDOW"] = 'W',
				["Y"] = 'Y',
				["2ND"] = '&',
				["MODE"] = 'O',
				["DEL"] = 'D',
				["COMMA"] = ',',
				["SIN"] = 'S'
			},
			["C64"] = new Dictionary<string, char>
			{
				["L"] = 'L',
				["R"] = 'R',
				["Left Arrow"] = 'l',
				["Plus"] = '+',
				["Minus"] = '-',
				["Pound"] = 'l',
				["Clear/Home"] = 'c',
				["Insert/Delete"] = 'i',
				["Control"] = 'c',
				["At"] = '@',
				["Asterisk"] = '*',
				["Up Arrow"] = 'u',
				["Restore"] = 'r',
				["Run/Stop"] = 's',
				["Lck"] = 'k',
				["Colon"] = ':',
				["Semicolon"] = ';',
				["Equal"] = '=',
				["Return"] = 'e',
				["Commodore"] = 'o',
				["Left Shift"] = 's',
				["Comma"] = ',',
				["Period"] = '>',
				["Slash"] = '/',
				["Right Shift"] = 's',
				["Cursor Up/Down"] = 'u',
				["Cursor Left/Right"] = 'l',
				["Space"] = '_'
			},
			["ZXSpectrum"] = new Dictionary<string, char>
			{
				["Caps Shift"] = '^',
				["Caps Lock"] = 'L',
				["Period"] = '_',
				["Symbol Shift"] = 'v',
				["Semi-Colon"] = ';',
				["Quote"] = '"',
				["Comma"] = ',',
				["True Video"] = 'T',
				["Inv Video"] = 'I',
				["Break"] = 'B',
				["Delete"] = 'D',
				["Graph"] = 'G',
				["Extend Mode"] = 'M',
				["Edit"] = 'E',
				["Play Tape"] = 'P',
				["Stop Tape"] = 'S',
				["RTZ Tape"] = 'r',
				["Record Tape"] = 'R',
				["Insert Next Tape"] = '>',
				["Insert Previous Tape"] = '<',
				["Next Tape Block"] = ']',
				["Prev Tape Block"] = '[',
				["Get Tape Status"] = 'S',
				["Insert Next Disk"] = '}',
				["Insert Previous Disk"] = '{',
				["Get Disk Status"] = 's',
				["Return"] = 'e',
				["Space"] = '-',
				["Up Cursor"] = 'u',
				["Down Cursor"] = 'd',
				["Left Cursor"] = 'l',
				["Right Cursor"] = 'r'
			},
			["N64"] = new Dictionary<string, char>
			{
				["C Up"] = 'u',
				["C Down"] = 'd',
				["C Left"] = 'l',
				["C Right"] = 'r',

				["A Up"] = 'U',
				["A Down"] = 'D',
				["A Left"] = 'L',
				["A Right"] = 'R',

				["DPad U"] = 'U',
				["DPad D"] = 'D',
				["DPad L"] = 'L',
				["DPad R"] = 'R',
			},
			["DGB"] = new Dictionary<string, char>
			{
				["Toggle Cable"] = 'L'
			},
			["GB3x"] = new Dictionary<string, char>
			{
				["Toggle Cable LC"] = 'L',
				["Toggle Cable CR"] = 'C',
				["Toggle Cable RL"] = 'R'
			},
			["GB4x"] = new Dictionary<string, char>
			{
				["Toggle Cable UD"] = 'U',
				["Toggle Cable LR"] = 'L',
				["Toggle Cable X"] = 'X',
				["Toggle Cable 4x"] = '4'
			},
			["Lynx"] = new Dictionary<string, char>
			{
				["Option 1"] = '1',
				["Option 2"] = '2'
			},
			["NGP"] = new Dictionary<string, char>
			{
				["Option"] = 'O'
			},
			["AppleII"] = new Dictionary<string, char>
			{
				["Tab"] = 't' ,
				["Return"] = 'e' ,
				["Escape"] = 'x' ,
				["Delete"] = 'b' ,
				["Space"] = 's' ,
				["Control"] = 'c' ,
				["Shift"] = '^' ,
				["Caps Lock"] = 'C' ,
				["White Apple"] = 'w' ,
				["Black Apple"] = 'b' ,
				["L"] = 'L' ,
				["R"] = 'R'
			},
			["INTV"] = new Dictionary<string, char>
			{
				["Clear"] = 'C' ,
				["Enter"] = 'E' ,
				["Top"] = 'T' ,
				["NNE"] = 'n' ,
				["NE"] = '/' ,
				["ENE"] = 'e' ,
				["ESE"] = 'e' ,
				["SE"] = '\\' ,
				["SSE"] = 's' ,
				["SSW"] = 's' ,
				["SW"] = '/' ,
				["WSW"] = 'w' ,
				["WNW"] = 'w' ,
				["NW"] = '\\' ,
				["NNW"] = 'n'
			},
			["Coleco"] = new Dictionary<string, char>
			{
				["Yellow"] = 'Y',
				["Red"] = 'R',
				["Blue"] = 'B',
				["Purple"] = 'P'
			},
			["VB"] = new Dictionary<string, char>
			{
				["L_Up"] = 'U',
				["L_Down"] = 'D',
				["L_Left"] = 'L',
				["L_Right"] = 'R',
				["R_Up"] = 'u',
				["R_Down"] = 'd',
				["R_Left"] = 'l',
				["R_Right"] = 'r',
			},
			["PCFX"] = new Dictionary<string, char>
			{
				["I"] = '1',
				["II"] = '2',
				["III"] = '3',
				["IV"] = '4',
				["V"] = '5',
				["VI"] = '6',
				["Mode 1: Set A"] = 'a',
				["Mode 1: Set B"] = 'b',
				["Mode 2: Set A"] = 'A',
				["Mode 2: Set B"] = 'B'
			},
			["PCE"] = new Dictionary<string, char>
			{
				["I"] = '1',
				["II"] = '2',
				["III"] = '3',
				["IV"] = '4',
				["V"] = '5',
				["VI"] = '6',
				["Mode: Set 2-button"] = 'm',
				["Mode: Set 6-button"] = 'M'
			},
			["NDS"] = new Dictionary<string, char>
			{
				["LidOpen"] = 'o',
				["LidClose"] = 'c',
				["Touch"] = 'T'
			},
			["O2"] = new Dictionary<string, char>
			{
				["PERIOD"] = '.',
				["SPC"] = 's',
				["YES"] = 'y',
				["NO"] = 'n',
				["CLR"] = 'c',
				["ENT"] = 'e'
			},
			["MAME"] = new Dictionary<string, char>
			{
				["1 Player Start"] = '1',
				["2 Players Start"] = '2',
				["3 Players Start"] = '3',
				["4 Players Start"] = '4',
				["5 Players Start"] = '5',
				["6 Players Start"] = '6',
				["7 Players Start"] = '7',
				["8 Players Start"] = '8',
				["Board 0 (SW4)"] = '0',
				["Board 1 (SW5)"] = '1',
				["Board 2 (SW6)"] = '2',
				["Board 3 (SW7)"] = '3',
				["Coin 1"] = 'C',
				["Coin 2"] = 'C',
				["Coin 3"] = 'C',
				["Coin 4"] = 'C',
				["Coin 5"] = 'C',
				["Coin 6"] = 'C',
				["Coin 7"] = 'C',
				["Coin 8"] = 'C',
				["Coin 9"] = 'C',
				["Coinblock"] = 'C',
				["Cup Select 1"] = '1',
				["Cup Select 2"] = '2',
				["Fierce Punch"] = 'F',
				["Foot Pedal"] = 'P',
				["Forward Kick"] = 'f',
				["GEAR 1"] = '1',
				["GEAR 2"] = '2',
				["GEAR 3"] = '3',
				["GEAR 4"] = '4',
				["GEAR N"] = 'N',
				["Gun Trigger"] = 'G',
				["Handle A"] = 'A',
				["Handle B"] = 'B',
				["Jab Punch"] = 'J',
				["Left Stick/Up"] = '^',
				["Left Stick/Down"] = 'v',
				["Left Stick/Left"] = '<',
				["Left Stick/Right"] = '>',
				["Light"] = 'l',
				["Lightgun X"] = 'X',
				["Lightgun Y"] = 'Y',
				["Medium"] = 'm',
				["Paddle"] = 'P',
				["Pedal 1"] = '1',
				["Pedal 2"] = '2',
				["Relay"] = 'R',
				["Right Stick/Up"] = 'u',
				["Right Stick/Down"] = 'd',
				["Right Stick/Left"] = 'l',
				["Right Stick/Right"] = 'r',
				["Roundhouse Kick"] = 'r',
				["Sensor"] = 'S',
				["Service"] = 'S',
				["Service Mode"] = 'S',
				["Service Button"] = 'S',
				["Service 1"] = 's',
				["Service 2"] = 's',
				["Service 3"] = 's',
				["Service 4"] = 's',
				["Short Kick"] = 's',
				["Sold Out LED1"] = '1',
				["Sold Out LED2"] = '2',
				["Sold Out LED3"] = '3',
				["Sold Out SW1"] = '1',
				["Sold Out SW2"] = '2',
				["Sold Out SW3"] = '3',
				["Stand"] = 'S',
				["Strong"] = 's',
				["Strong Punch"] = 'S',
				["Tilt"] = 'T',
				["Tilt 1"] = 't',
				["Tilt 2"] = 't',
				["Tilt 3"] = 't',
				["Tilt 4"] = 't',
				["Volume Down"] = '-',
				["Volume Up"] = '+',
				["VR1 (Red)"] = 'R',
				["VR2 (Blue)"] = 'B',
				["VR3 (Yellow)"] = 'Y',
				["VR4 (Green)"] = 'G',
			},
			["SAT"] = new Dictionary<string, char>
			{
				["Smpc Reset"] = 's',
				["D-Pad Up"] = 'U',
				["D-Pad Down"] = 'D',
				["D-Pad Left"] = 'L',
				["D-Pad Right"] = 'R',
				["Mode: Set Digital(+)"] = '+',
				["Mode: Set Analog(○)"] = 'o',
				["L Gear Shift"] = 'L',
				["R Gear Shift"] = 'R',
				["Offscreen Shot"] = 'O'
			}
		};

		private static readonly Dictionary<string, string> BaseAxisLookupTable = new Dictionary<string, string>
		{
			["Zapper X"] = "zapX",
			["Zapper Y"] = "zapY",
			["Paddle"] = "Pad",
			["Pen"] = "Pen",
			["Mouse X"] = "mX",
			["Mouse Y"] = "mY",
			["Lightgun X"] = "lX",
			["Lightgun Y"] = "lY",
			["X Axis"] = "aX",
			["Y Axis"] = "aY",
			["LStick X"] = "lsX",
			["LStick Y"] = "lsY",
			["RStick X"] = "rsX",
			["RStick Y"] = "rsY",
			["Disc Select"] = "Disc"
		};

		private static readonly Dictionary<string, Dictionary<string, string>> AxisSystemOverrides = new Dictionary<string, Dictionary<string, string>>
		{
			["A78"] = new Dictionary<string, string>
			{
				["VPos"] = "X",
				["HPos"] = "Y"
			}
		};
	}
}
