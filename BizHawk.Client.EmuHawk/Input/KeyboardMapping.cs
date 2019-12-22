using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using SlimDX.DirectInput;

namespace BizHawk.Client.EmuHawk
{
	internal static class KeyboardMapping
	{
		private const uint MAPVK_VSC_TO_VK_EX = 0x03;

		public static Key Handle(Key key)
		{
			if (!Global.Config.HandleAlternateKeyboardLayouts) return key;
			ScanCode inputScanCode = SlimDXScanCodeMap[(int)key];
			Keys virtualKey = (Keys)BizHawk.Common.Win32Imports.MapVirtualKey((uint)inputScanCode, MAPVK_VSC_TO_VK_EX);
			ScanCode standardScanCode = GetStandardScanCode(virtualKey);
			if (standardScanCode == 0)
				standardScanCode = inputScanCode;
			return ScanCodeToSlimDXKey[standardScanCode];
		}

		private static ScanCode GetStandardScanCode(Keys virtualKey)
		{
			switch (virtualKey)
			{
				case Keys.Escape: return ScanCode.Escape;
				case Keys.D1: return ScanCode.D1;
				case Keys.D2: return ScanCode.D2;
				case Keys.D3: return ScanCode.D3;
				case Keys.D4: return ScanCode.D4;
				case Keys.D5: return ScanCode.D5;
				case Keys.D6: return ScanCode.D6;
				case Keys.D7: return ScanCode.D7;
				case Keys.D8: return ScanCode.D8;
				case Keys.D9: return ScanCode.D9;
				case Keys.D0: return ScanCode.D0;
				case Keys.OemMinus: return ScanCode.Minus;
				case Keys.Oemplus: return ScanCode.Equals;
				case Keys.Back: return ScanCode.Back;
				case Keys.Tab: return ScanCode.Tab;
				case Keys.Q: return ScanCode.Q;
				case Keys.W: return ScanCode.W;
				case Keys.E: return ScanCode.E;
				case Keys.R: return ScanCode.R;
				case Keys.T: return ScanCode.T;
				case Keys.Y: return ScanCode.Y;
				case Keys.U: return ScanCode.U;
				case Keys.I: return ScanCode.I;
				case Keys.O: return ScanCode.O;
				case Keys.P: return ScanCode.P;
				case Keys.OemOpenBrackets: return ScanCode.LBracket;
				case Keys.OemCloseBrackets: return ScanCode.RBracket;
				case Keys.Return: return ScanCode.Return;
				case Keys.LControlKey: return ScanCode.LControl;
				case Keys.A: return ScanCode.A;
				case Keys.S: return ScanCode.S;
				case Keys.D: return ScanCode.D;
				case Keys.F: return ScanCode.F;
				case Keys.G: return ScanCode.G;
				case Keys.H: return ScanCode.H;
				case Keys.J: return ScanCode.J;
				case Keys.K: return ScanCode.K;
				case Keys.L: return ScanCode.L;
				case Keys.OemSemicolon: return ScanCode.Semicolon;
				case Keys.OemQuotes: return ScanCode.Apostrophe;
				case Keys.Oemtilde: return ScanCode.Grave;
				case Keys.LShiftKey: return ScanCode.LShift;
				case Keys.OemPipe: return ScanCode.Backslash;
				case Keys.Z: return ScanCode.Z;
				case Keys.X: return ScanCode.X;
				case Keys.C: return ScanCode.C;
				case Keys.V: return ScanCode.V;
				case Keys.B: return ScanCode.B;
				case Keys.N: return ScanCode.N;
				case Keys.M: return ScanCode.M;
				case Keys.Oemcomma: return ScanCode.Comma;
				case Keys.OemPeriod: return ScanCode.Period;
				case Keys.OemQuestion: return ScanCode.Slash;
				case Keys.RShiftKey: return ScanCode.RShift;
				case Keys.Multiply: return ScanCode.Multiply;
				case Keys.LMenu: return ScanCode.LMenu;
				case Keys.Space: return ScanCode.Space;
				case Keys.Capital: return ScanCode.Capital;
				case Keys.F1: return ScanCode.F1;
				case Keys.F2: return ScanCode.F2;
				case Keys.F3: return ScanCode.F3;
				case Keys.F4: return ScanCode.F4;
				case Keys.F5: return ScanCode.F5;
				case Keys.F6: return ScanCode.F6;
				case Keys.F7: return ScanCode.F7;
				case Keys.F8: return ScanCode.F8;
				case Keys.F9: return ScanCode.F9;
				case Keys.F10: return ScanCode.F10;
				case Keys.NumLock: return ScanCode.NumLock;
				case Keys.Scroll: return ScanCode.Scroll;
				case Keys.Subtract: return ScanCode.Subtract;
				case Keys.Add: return ScanCode.Add;
				case Keys.OemBackslash: return ScanCode.Oem_102;
				case Keys.F11: return ScanCode.F11;
				case Keys.F12: return ScanCode.F12;
				case Keys.F13: return ScanCode.F13;
				case Keys.F14: return ScanCode.F14;
				case Keys.F15: return ScanCode.F15;
			}
			return 0;
		}

		private enum ScanCode
		{
			Escape = 0x01,
			D1 = 0x02,
			D2 = 0x03,
			D3 = 0x04,
			D4 = 0x05,
			D5 = 0x06,
			D6 = 0x07,
			D7 = 0x08,
			D8 = 0x09,
			D9 = 0x0A,
			D0 = 0x0B,
			Minus = 0x0C,
			Equals = 0x0D,
			Back = 0x0E,
			Tab = 0x0F,
			Q = 0x10,
			W = 0x11,
			E = 0x12,
			R = 0x13,
			T = 0x14,
			Y = 0x15,
			U = 0x16,
			I = 0x17,
			O = 0x18,
			P = 0x19,
			LBracket = 0x1A,
			RBracket = 0x1B,
			Return = 0x1C,
			LControl = 0x1D,
			A = 0x1E,
			S = 0x1F,
			D = 0x20,
			F = 0x21,
			G = 0x22,
			H = 0x23,
			J = 0x24,
			K = 0x25,
			L = 0x26,
			Semicolon = 0x27,
			Apostrophe = 0x28,
			Grave = 0x29,
			LShift = 0x2A,
			Backslash = 0x2B,
			Z = 0x2C,
			X = 0x2D,
			C = 0x2E,
			V = 0x2F,
			B = 0x30,
			N = 0x31,
			M = 0x32,
			Comma = 0x33,
			Period = 0x34,
			Slash = 0x35,
			RShift = 0x36,
			Multiply = 0x37,
			LMenu = 0x38,
			Space = 0x39,
			Capital = 0x3A,
			F1 = 0x3B,
			F2 = 0x3C,
			F3 = 0x3D,
			F4 = 0x3E,
			F5 = 0x3F,
			F6 = 0x40,
			F7 = 0x41,
			F8 = 0x42,
			F9 = 0x43,
			F10 = 0x44,
			NumLock = 0x45,
			Scroll = 0x46,
			NumPad7 = 0x47,
			NumPad8 = 0x48,
			NumPad9 = 0x49,
			Subtract = 0x4A,
			NumPad4 = 0x4B,
			NumPad5 = 0x4C,
			NumPad6 = 0x4D,
			Add = 0x4E,
			NumPad1 = 0x4F,
			NumPad2 = 0x50,
			NumPad3 = 0x51,
			NumPad0 = 0x52,
			Decimal = 0x53,
			Oem_102 = 0x56,
			F11 = 0x57,
			F12 = 0x58,
			F13 = 0x64,
			F14 = 0x65,
			F15 = 0x66,
			Kana = 0x70,
			Abnt_C1 = 0x73,
			Convert = 0x79,
			NoConvert = 0x7B,
			Yen = 0x7D,
			Abnt_C2 = 0x7E,
			NumPadEquals = 0x8D,
			PrevTrack = 0x90,
			AT = 0x91,
			Colon = 0x92,
			Underline = 0x93,
			Kanji = 0x94,
			Stop = 0x95,
			AX = 0x96,
			Unlabeled = 0x97,
			NextTrack = 0x99,
			NumPadEnter = 0x9C,
			RControl = 0x9D,
			Mute = 0xA0,
			Calculator = 0xA1,
			PlayPause = 0xA2,
			MediaStop = 0xA4,
			VolumeDown = 0xAE,
			VolumeUp = 0xB0,
			WebHome = 0xB2,
			NumPadComma = 0xB3,
			Divide = 0xB5,
			SysRq = 0xB7,
			RMenu = 0xB8,
			Pause = 0xC5,
			Home = 0xC7,
			Up = 0xC8,
			Prior = 0xC9,
			Left = 0xCB,
			Right = 0xCD,
			End = 0xCF,
			Down = 0xD0,
			Next = 0xD1,
			Insert = 0xD2,
			Delete = 0xD3,
			LWin = 0xDB,
			RWin = 0xDC,
			Apps = 0xDD,
			Power = 0xDE,
			Sleep = 0xDF,
			Wake = 0xE3,
			WebSearch = 0xE5,
			WebFavorites = 0xE6,
			WebRefresh = 0xE7,
			WebStop = 0xE8,
			WebForward = 0xE9,
			WebBack = 0xEA,
			MyComputer = 0xEB,
			Mail = 0xEC,
			MediaSelect = 0xED
		}

		private static readonly ScanCode[] SlimDXScanCodeMap = new ScanCode[]
		{
			ScanCode.D0, // 0
			ScanCode.D1, // 1
			ScanCode.D2, // 2
			ScanCode.D3, // 3
			ScanCode.D4, // 4
			ScanCode.D5, // 5
			ScanCode.D6, // 6
			ScanCode.D7, // 7
			ScanCode.D8, // 8
			ScanCode.D9, // 9
			ScanCode.A, // 10
			ScanCode.B, // 11
			ScanCode.C, // 12
			ScanCode.D, // 13
			ScanCode.E, // 14
			ScanCode.F, // 15
			ScanCode.G, // 16
			ScanCode.H, // 17
			ScanCode.I, // 18
			ScanCode.J, // 19
			ScanCode.K, // 20
			ScanCode.L, // 21
			ScanCode.M, // 22
			ScanCode.N, // 23
			ScanCode.O, // 24
			ScanCode.P, // 25
			ScanCode.Q, // 26
			ScanCode.R, // 27
			ScanCode.S, // 28
			ScanCode.T, // 29
			ScanCode.U, // 30
			ScanCode.V, // 31
			ScanCode.W, // 32
			ScanCode.X, // 33
			ScanCode.Y, // 34
			ScanCode.Z, // 35
			ScanCode.Abnt_C1, // 36
			ScanCode.Abnt_C2, // 37
			ScanCode.Apostrophe, // 38
			ScanCode.Apps, // 39
			ScanCode.AT, // 40
			ScanCode.AX, // 41
			ScanCode.Back, // 42
			ScanCode.Backslash, // 43
			ScanCode.Calculator, // 44
			ScanCode.Capital, // 45
			ScanCode.Colon, // 46
			ScanCode.Comma, // 47
			ScanCode.Convert, // 48
			ScanCode.Delete, // 49
			ScanCode.Down, // 50
			ScanCode.End, // 51
			ScanCode.Equals, // 52
			ScanCode.Escape, // 53
			ScanCode.F1, // 54
			ScanCode.F2, // 55
			ScanCode.F3, // 56
			ScanCode.F4, // 57
			ScanCode.F5, // 58
			ScanCode.F6, // 59
			ScanCode.F7, // 60
			ScanCode.F8, // 61
			ScanCode.F9, // 62
			ScanCode.F10, // 63
			ScanCode.F11, // 64
			ScanCode.F12, // 65
			ScanCode.F13, // 66
			ScanCode.F14, // 67
			ScanCode.F15, // 68
			ScanCode.Grave, // 69
			ScanCode.Home, // 70
			ScanCode.Insert, // 71
			ScanCode.Kana, // 72
			ScanCode.Kanji, // 73
			ScanCode.LBracket, // 74
			ScanCode.LControl, // 75
			ScanCode.Left, // 76
			ScanCode.LMenu, // 77
			ScanCode.LShift, // 78
			ScanCode.LWin, // 79
			ScanCode.Mail, // 80
			ScanCode.MediaSelect, // 81
			ScanCode.MediaStop, // 82
			ScanCode.Minus, // 83
			ScanCode.Mute, // 84
			ScanCode.MyComputer, // 85
			ScanCode.NextTrack, // 86
			ScanCode.NoConvert, // 87
			ScanCode.NumLock, // 88
			ScanCode.NumPad0, // 89
			ScanCode.NumPad1, // 90
			ScanCode.NumPad2, // 91
			ScanCode.NumPad3, // 92
			ScanCode.NumPad4, // 93
			ScanCode.NumPad5, // 94
			ScanCode.NumPad6, // 95
			ScanCode.NumPad7, // 96
			ScanCode.NumPad8, // 97
			ScanCode.NumPad9, // 98
			ScanCode.NumPadComma, // 99
			ScanCode.NumPadEnter, // 100
			ScanCode.NumPadEquals, // 101
			ScanCode.Subtract, // 102
			ScanCode.Decimal, // 103
			ScanCode.Add, // 104
			ScanCode.Divide, // 105
			ScanCode.Multiply, // 106
			ScanCode.Oem_102, // 107
			ScanCode.Next, // 108
			ScanCode.Prior, // 109
			ScanCode.Pause, // 110
			ScanCode.Period, // 111
			ScanCode.PlayPause, // 112
			ScanCode.Power, // 113
			ScanCode.PrevTrack, // 114
			ScanCode.RBracket, // 115
			ScanCode.RControl, // 116
			ScanCode.Return, // 117
			ScanCode.Right, // 118
			ScanCode.RMenu, // 119
			ScanCode.RShift, // 120
			ScanCode.RWin, // 121
			ScanCode.Scroll, // 122
			ScanCode.Semicolon, // 123
			ScanCode.Slash, // 124
			ScanCode.Sleep, // 125
			ScanCode.Space, // 126
			ScanCode.Stop, // 127
			ScanCode.SysRq, // 128
			ScanCode.Tab, // 129
			ScanCode.Underline, // 130
			ScanCode.Unlabeled, // 131
			ScanCode.Up, // 132
			ScanCode.VolumeDown, // 133
			ScanCode.VolumeUp, // 134
			ScanCode.Wake, // 135
			ScanCode.WebBack, // 136
			ScanCode.WebFavorites, // 137
			ScanCode.WebForward, // 138
			ScanCode.WebHome, // 139
			ScanCode.WebRefresh, // 140
			ScanCode.WebSearch, // 141
			ScanCode.WebStop, // 142
			ScanCode.Yen, // 143
			0 // 144
		};

		private static readonly Dictionary<ScanCode, Key> ScanCodeToSlimDXKey =
			SlimDXScanCodeMap
				.Select((n, i) => new { Value = n, Index = i })
				.ToDictionary(n => n.Value, n => (Key)n.Index);
	}
}
