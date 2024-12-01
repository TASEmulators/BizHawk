﻿using System.Collections.Generic;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	public static class Bk2MnemonicLookup
	{
		/// <remarks>duplicated with <c>LibretroControllerDef</c></remarks>
		private const string PFX_RETROPAD = "RetroPad ";

		public static char Lookup(string button, string systemId)
		{
			var key = button.Replace("Key ", "");
			if (key.StartsWith('P'))
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
			key = key.RemovePrefix(PFX_RETROPAD);

			if (SystemOverrides.TryGetValue(systemId, out var overridesForSys) && overridesForSys.TryGetValue(key, out var c))
			{
				return c;
			}

			if (BaseMnemonicLookupTable.TryGetValue(key, out var c1))
			{
				return c1;
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

			if (AxisSystemOverrides.TryGetValue(systemId, out var overridesForSystem) && overridesForSystem.TryGetValue(key, out var s))
			{
				return s;
			}

			if (BaseAxisLookupTable.TryGetValue(key, out var s1))
			{
				return s1;
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
			["Lightgun Offscreen Shot"] = 'O',
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
			[VSystemID.Raw.NES] = new()
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
			[VSystemID.Raw.SNES] = new()
			{
				["Cursor"] = 'c',
				["Turbo"] = 't',
				["Toggle Multitap"] = 't',
				["Offscreen"] = 'o',

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
				["B31"] = 'v',

				["Extra1"] = '1',
				["Extra2"] = '2',
				["Extra3"] = '3',
				["Extra4"] = '4',

				["Subframe"] = 'F'
			},
			[VSystemID.Raw.TI83] = new()
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
				["SIN"] = 'S',
				["SEND"] = 'N',
			},
			[VSystemID.Raw.C64] = new()
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
			[VSystemID.Raw.ZXSpectrum] = new()
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
			[VSystemID.Raw.N64] = new()
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
			[VSystemID.Raw.GBL] = new()
			{
				// gbhawk
				["Toggle Cable"] = 'L',
				["Toggle Cable LC"] = 'L',
				["Toggle Cable CR"] = 'C',
				["Toggle Cable RL"] = 'R',
				["Toggle Cable UD"] = 'U',
				["Toggle Cable LR"] = 'L',
				["Toggle Cable X"] = 'X',
				["Toggle Cable 4x"] = '4',
				// gambatte
				["Toggle Link Connection"] = 'L',
				["Toggle Link Shift"] = 'F',
				["Toggle Link Spacing"] = 'C',
			},
			[VSystemID.Raw.Jaguar] = new()
			{
				["Option"] = 'O',
				["Asterisk"] = '*',
			},
			[VSystemID.Raw.Lynx] = new()
			{
				["Option 1"] = '1',
				["Option 2"] = '2'
			},
			[VSystemID.Raw.NGP] = new()
			{
				["Option"] = 'O'
			},
			[VSystemID.Raw.AppleII] = new()
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
			[VSystemID.Raw.INTV] = new()
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
			[VSystemID.Raw.Coleco] = new()
			{
				["Yellow"] = 'Y',
				["Red"] = 'R',
				["Blue"] = 'B',
				["Purple"] = 'P'
			},
			[VSystemID.Raw.VB] = new()
			{
				["L_Up"] = 'U',
				["L_Down"] = 'D',
				["L_Left"] = 'L',
				["L_Right"] = 'R',
				["R_Up"] = 'u',
				["R_Down"] = 'd',
				["R_Left"] = 'l',
				["R_Right"] = 'r',
				["Battery Voltage: Set Normal"] = '+',
				["Battery Voltage: Set Low"] = '-',
			},
			[VSystemID.Raw.PCFX] = new()
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
			[VSystemID.Raw.PCE] = new()
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
			[VSystemID.Raw.PCECD] = new()
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
			[VSystemID.Raw.N3DS] = new()
			{
				["Debug"] = 'd',
				["GPIO14"] = 'g',
				["ZL"] = 'z',
				["ZR"] = 'z',
				["Touch"] = 'T',
				["Tilt"] = 't'
			},
			[VSystemID.Raw.NDS] = new()
			{
				["LidOpen"] = 'o',
				["LidClose"] = 'c',
				["Microphone"] = 'M',
				["Touch"] = 'T'
			},
			[VSystemID.Raw.O2] = new()
			{
				["PERIOD"] = '.',
				["SPC"] = 's',
				["YES"] = 'y',
				["NO"] = 'n',
				["CLR"] = 'c',
				["ENT"] = 'e'
			},
			[VSystemID.Raw.Arcade] = new()
			{
				["1 Player Start"] = '1',
				["2 Players Start"] = '2',
				["3 Players Start"] = '3',
				["4 Players Start"] = '4',
				["5 Players Start"] = '5',
				["6 Players Start"] = '6',
				["7 Players Start"] = '7',
				["8 Players Start"] = '8',
				["Advance"] = 'A',
				["Attack"] = 'a',
				["Auto Up / Manual Down"] = 'a',
				["Banknote 1"] = 'b',
				["Block"] = 'B',
				["Block 2"] = 'b',
				["Blue"] = 'B',
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
				["Door Interlock"] = 'I',
				["Effect"] = 'E',
				["Emergency"] = 'E',
				["Fierce Punch"] = 'F',
				["Fire Down"] = 'd',
				["Fire Left"] = 'l',
				["Fire Right"] = 'r',
				["Fire Up"] = 'u',
				["Foot Pedal"] = 'P',
				["Forward Kick"] = 'f',
				["GEAR 1"] = '1',
				["GEAR 2"] = '2',
				["GEAR 3"] = '3',
				["GEAR 4"] = '4',
				["GEAR N"] = 'N',
				["Green"] = 'G',
				["Gun Trigger"] = 'G',
				["Handle A"] = 'A',
				["Handle B"] = 'B',
				["High Kick"] = 'K',
				["High Punch"] = 'P',
				["High Score Reset"] = 'r',
				["Hyperspace"] = 'H',
				["Jab Punch"] = 'J',
				["Jump"] = 'j',
				["Left Stick/Up"] = '^',
				["Left Stick/Down"] = 'v',
				["Left Stick/Left"] = '<',
				["Left Stick/Right"] = '>',
				["Light"] = 'l',
				["Low Kick"] = 'k',
				["Low Punch"] = 'p',
				["Medium"] = 'm',
				["Move Down"] = 'D',
				["Move Left"] = 'L',
				["Move Right"] = 'R',
				["Move Up"] = 'U',
				["Paddle"] = 'P',
				["Pedal 1"] = '1',
				["Pedal 2"] = '2',
				["Push SW1 (Service)"] = 's',
				["Push SW2 (Test)"] = 'T',
				["Push SW3 (Service)"] = 's',
				["Push SW4 (Test)"] = 'T',
				["Red"] = 'R',
				["Relay"] = 'R',
				["Reverse"] = 'R',
				["Right Stick/Up"] = 'u',
				["Right Stick/Down"] = 'd',
				["Right Stick/Left"] = 'l',
				["Right Stick/Right"] = 'r',
				["Roundhouse Kick"] = 'r',
				["Sensor"] = 'S',
				["Service"] = 'S',
				["Service Mode"] = 'S',
				["Service Mode 2"] = 'S',
				["Service Button"] = 'S',
				["Service 1"] = 's',
				["Service 2"] = 's',
				["Service 3"] = 's',
				["Service 4"] = 's',
				["Short Kick"] = 's',
				["Smart Bomb"] = 'B',
				["Sold Out LED1"] = '1',
				["Sold Out LED2"] = '2',
				["Sold Out LED3"] = '3',
				["Sold Out SW1"] = '1',
				["Sold Out SW2"] = '2',
				["Sold Out SW3"] = '3',
				["Stand"] = 'S',
				["Strong"] = 's',
				["Strong Punch"] = 'S',
				["Test"] = 'T',
				["Thrust"] = 'T',
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
			[VSystemID.Raw.SAT] = new()
			{
				["Smpc Reset"] = 's',
				["St-V Test"] = 'T',
				["St-V Service"] = 'V',
				["St-V Pause"] = 'P',
				["Insert Coin"] = 'c',
				["D-Pad Up"] = 'U',
				["D-Pad Down"] = 'D',
				["D-Pad Left"] = 'L',
				["D-Pad Right"] = 'R',
				["Mode: Set Digital(+)"] = '+',
				["Mode: Set Analog(○)"] = 'o',
				["L Gear Shift"] = 'L',
				["R Gear Shift"] = 'R',
				["Offscreen Shot"] = 'O'
			},
			[VSystemID.Raw.PSX] = new()
			{
				["D-Pad Up"] = 'U',
				["D-Pad Down"] = 'D',
				["D-Pad Left"] = 'L',
				["D-Pad Right"] = 'R',
				["Thumbstick Up"] = 'U',
				["Thumbstick Down"] = 'D',
				["Thumbstick Left"] = 'L',
				["Thumbstick Right"] = 'R',
				["□"] = 'Q',
				["△"] = 'T',
				["○"] = 'O',
				["Left Stick, Button"] = '<',
				["Right Stick, Button"] = '>',
				["Left Stick, L-Thumb"] = 'L',
				["Right Stick, L-Thumb"] = 'l',
				["Left Stick, R-Thumb"] = 'R',
				["Right Stick, R-Thumb"] = 'r',
				["Left Stick, Trigger"] = 'T',
				["Right Stick, Trigger"] = 't',
				["Left Stick, Pinky"] = 'P',
				["Right Stick, Pinky"] = 'p',
				["Analog"] = 'M',
				["Offscreen Shot"] = 'o',
				["Open Tray"] = 'o',
				["Close Tray"] = 'c',
			},
			[VSystemID.Raw.TIC80] = new()
			{
				["Mouse Left Click"] = 'l',
				["Mouse Middle Click"] = 'm',
				["Mouse Right Click"] = 'r',
				["Mouse Relative Toggle"] = 't',

				["Minus"] = '-',
				["Equals"] = '=',
				["Left Bracket"] = '[',
				["Right Bracket"] = ']',
				["Backslash"] = '\\',
				["Semicolon"] = ';',
				["Apostrophe"] = '\'',
				["Grave"] = '`',
				["Comma"] = ',',
				["Period"] = '.',
				["Slash"] = '/',
				["Space"] = 's',
				["Tab"] = 't',
				["Return"] = 'r',
				["Backspace"] = 'b',
				["Delete"] = 'd',
				["Insert"] = 'i',
				["Page Up"] = 'u',
				["Page Down"] = 'v',
				["Home"] = 'h',
				["End"] = 'e',
				["Caps Lock"] = 'l',
				["Control"] = 'c',
				["Shift"] = '^',
				["Alt"] = 'a',
				["Escape"] = 'e',
				["F11"] = '1',
				["F12"] = '2',
			},
			[VSystemID.Raw.Amiga] = new()
			{
				["L"] = 'L',
				["R"] = 'R',
				["NP 0"] = '0',
				["NP 1"] = '1',
				["NP 2"] = '2',
				["NP 3"] = '3',
				["NP 4"] = '4',
				["NP 5"] = '5',
				["NP 6"] = '6',
				["NP 7"] = '7',
				["NP 8"] = '8',
				["NP 9"] = '9',
				["NP Add"] = '+',
				["NP Delete"] = 'd',
				["NP Div"] = '/',
				["NP Enter"] = 'e',
				["NP Left Paren"] = '(',
				["NP Mul"] = '*',
				["NP Right Paren"] = ')',
				["NP Sub"] = '-',
				["Backquote"] = '`',
				["Backslash"] = '\\',
				["Backspace"] = 'b',
				["Caps Lock"] = 'c',
				["Comma"] = ',',
				["Ctrl"] = 'c',
				["Delete"] = 'd',
				["Down"] = 'D',
				["Equal"] = '=',
				["Escape"] = 'e',
				["Help"] = 'h',
				["Left"] = 'L',
				["Left Alt"] = 'a',
				["Left Amiga"] = 'A',
				["Left Bracket"] = '[',
				["Left Shift"] = 's',
				["Less"] = '<',
				["Minus"] = '-',
				["Number Sign"] = '#',
				["Period"] = 'p',
				["Quote"] = '\"',
				["Return"] = 'r',
				["Right"] = 'R',
				["Right Alt"] = 'a',
				["Right Amiga"] = 'A',
				["Right Bracket"] = ']',
				["Right Shift"] = 's',
				["Semicolon"] = ';',
				["Slash"] = '/',
				["Space"] = '_',
				["Tab"] = 't',
				["Up"] = 'U',
				["Joystick Button 1"] = '1',
				["Joystick Button 2"] = '2',
				["Joystick Button 3"] = '3',
				["Joystick Up"] = 'U',
				["Joystick Down"] = 'D',
				["Joystick Left"] = 'L',
				["Joystick Right"] = 'R',
				["CD32 pad Up"] = 'U',
				["CD32 pad Down"] = 'D',
				["CD32 pad Left"] = 'L',
				["CD32 pad Right"] = 'R',
				["CD32 pad Play"] = '>',
				["CD32 pad Rewind"] = '{',
				["CD32 pad Forward"] = '}',
				["CD32 pad Green"] = 'g',
				["CD32 pad Yellow"] = 'y',
				["CD32 pad Red"] = 'r',
				["CD32 pad Blue"] = 'b',
				["Mouse Left Button"] = 'l',
				["Mouse Middle Button"] = 'm',
				["Mouse Right Button"] = 'r',
				["Eject Disk"] = '^',
				["Insert Disk"] = 'v',
				["Next Drive"] = '}',
				["Next Slot"] = '>',
			},
			[VSystemID.Raw.ChannelF] = new()
			{
				["Right"] = 'R',
				["Left"] = 'L',
				["Back"] = 'D',
				["Forward"] = 'U',
				["CCW"] = 'c',
				["CW"] = 'C',
				["Pull"] = 'O',
				["Push"] = 'P',
				["TIME"] = 'T',
				["MODE"] = 'M',
				["HOLD"] = 'H',
				["START"] = 'S',
				["RESET"] = 'r',
			},
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
			["Disc Select"] = "Disc",
			["Disk Index"] = "Disk",
		};

		private static readonly Dictionary<string, Dictionary<string, string>> AxisSystemOverrides = new Dictionary<string, Dictionary<string, string>>
		{
			[VSystemID.Raw.A78] = new()
			{
				["VPos"] = "X",
				["HPos"] = "Y"
			},
			[VSystemID.Raw.PSX] = new()
			{
				["Left Stick Left / Right"] = "lsX",
				["Left Stick Up / Down"] = "lsY",
				["Right Stick Left / Right"] = "rsX",
				["Right Stick Up / Down"] = "rsY",
				["Left Stick, Left / Right"] = "lsX",
				["Left Stick, Fore / Back"] = "lsZ",
				["Right Stick, Left / Right"] = "rsX",
				["Right Stick, Fore / Back"] = "rsZ",
				["Motion Left / Right"] = "mX",
				["Motion Up / Down"] = "mY",
				["Twist | / |"] = "Twist",
			},
			[VSystemID.Raw.TIC80] = new()
			{
				["Mouse Position X"] = "mpX",
				["Mouse Position Y"] = "mpY",
				["Mouse Scroll X"] = "msX",
				["Mouse Scroll Y"] = "msY",
			},
		};
	}
}
