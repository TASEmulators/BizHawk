using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable UnusedMember.Global

namespace BizHawk.Common
{
	public static class RawInputImports
	{
		/// <summary>
		/// This enum generally assumes a QWERTY layout (and goes off PS/2 Set 1 scancodes, i.e. what RAWINPUT uses)
		/// Bit7 will indicate that the input has an E0 prefix
		/// (This also somewhat mimics DirectInput's DIK_ enum)
		/// </summary>
		public enum RawKey : byte
		{
			ESCAPE = 0x01,
			_1 = 0x02,
			_2 = 0x03,
			_3 = 0x04,
			_4 = 0x05,
			_5 = 0x06,
			_6 = 0x07,
			_7 = 0x08,
			_8 = 0x09,
			_9 = 0x0A,
			_0 = 0x0B,
			MINUS = 0x0C,
			EQUALS = 0x0D,
			BACKSPACE = 0x0E,
			TAB = 0x0F,
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
			LEFTBRACKET = 0x1A,
			RIGHTBRACKET = 0x1B,
			ENTER = 0x1C,
			LEFTCONTROL = 0x1D,
			A = 0x1E,
			S = 0x1F,
			D = 0x20,
			F = 0x21,
			G = 0x22,
			H = 0x23,
			J = 0x24,
			K = 0x25,
			L = 0x26,
			SEMICOLON = 0x27,
			APOSTROPHE = 0x28,
			GRAVE = 0x29,
			LEFTSHIFT = 0x2A,
			BACKSLASH = 0x2B, // also EUROPE1
			Z = 0x2C,
			X = 0x2D,
			C = 0x2E,
			V = 0x2F,
			B = 0x30,
			N = 0x31,
			M = 0x32,
			COMMA = 0x33,
			PERIOD = 0x34,
			SLASH = 0x35,
			RIGHTSHIFT = 0x36,
			MULTIPLY = 0x37,
			LEFTALT = 0x38,
			SPACEBAR = 0x39,
			CAPSLOCK = 0x3A,
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
			NUMLOCK = 0x45,
			SCROLLLOCK = 0x46,
			NUMPAD7 = 0x47,
			NUMPAD8 = 0x48,
			NUMPAD9 = 0x49,
			SUBSTRACT = 0x4A,
			NUMPAD4 = 0x4B,
			NUMPAD5 = 0x4C,
			NUMPAD6 = 0x4D,
			ADD = 0x4E,
			NUMPAD1 = 0x4F,
			NUMPAD2 = 0x50,
			NUMPAD3 = 0x51,
			NUMPAD0 = 0x52,
			DECIMAL = 0x53,
			EUROPE2 = 0x56,
			F11 = 0x57,
			F12 = 0x58,
			NUMPADEQUALS = 0x59,
			INTL6 = 0x5C,
			F13 = 0x64,
			F14 = 0x65,
			F15 = 0x66,
			F16 = 0x67,
			F17 = 0x68,
			F18 = 0x69,
			F19 = 0x6A,
			F20 = 0x6B,
			F21 = 0x6C,
			F22 = 0x6D,
			F23 = 0x6E,
			INTL2 = 0x70,
			INTL1 = 0x73,
			F24 = 0x76, // also LANG5
			LANG4 = 0x77,
			LANG3 = 0x78,
			INTL4 = 0x79,
			INTL5 = 0x7B,
			INTL3 = 0x7D,
			SEPARATOR = 0x7E,
			PREVTRACK = 0x90,
			NEXTTRACK = 0x97,
			NUMPADENTER = 0x9C,
			RIGHTCONTROL = 0x9D,
			MUTE = 0xA0,
			CALCULATOR = 0xA1,
			PLAYPAUSE = 0xA2,
			STOP = 0xA4,
			VOLUMEDOWN = 0xAE,
			VOLUMEUP = 0xB0,
			BROWSERHOME = 0xB2,
			DIVIDE = 0xB5,
			PRINTSCREEN = 0xB7,
			RIGHTALT = 0xB8,
			PAUSE = 0xC5,
			HOME = 0xC7,
			UP = 0xC8,
			PAGEUP = 0xC9,
			LEFT = 0xCB,
			RIGHT = 0xCD,
			END = 0xCF,
			DOWN = 0xD0,
			PAGEDOWN = 0xD1,
			INSERT = 0xD2,
			DELETE = 0xD3,
			LEFTGUI = 0xDB,
			RIGHTGUI = 0xDC,
			APPS = 0xDD,
			POWER = 0xDE,
			SLEEP = 0xDF,
			WAKE = 0xE3,
			BROWSERSEARCH = 0xE5,
			BROWSERFAVORITES = 0xE6,
			BROWSERREFRESH = 0xE7,
			BROWSERSTOP = 0xE8,
			BROWSERFORWARD = 0xE9,
			BROWSERBACK = 0xEA,
			MYCOMPUTER = 0xEB,
			MAIL = 0xEC,
			MEDIASELECT = 0xED,
		}

		public enum VirtualKey : ushort
		{
			VK_BACK = 0x08,
			VK_TAB = 0x09,
			VK_CLEAR = 0x0C,
			VK_RETURN = 0x0D,
			VK_SHIFT = 0x10,
			VK_CONTROL = 0x11,
			VK_MENU = 0x12,
			VK_PAUSE = 0x13,
			VK_CAPITAL = 0x14,
			VK_KANA = 0x15,
			VK_IME_ON = 0x16,
			VK_JUNJA = 0x17,
			VK_FINAL = 0x18,
			VK_KANJI = 0x19,
			VK_IME_OFF = 0x1A,
			VK_ESCAPE = 0x1B,
			VK_CONVERT = 0x1C,
			VK_NONCONVERT = 0x1D,
			VK_ACCEPT = 0x1E,
			VK_MODECHANGE = 0x1F,
			VK_SPACE = 0x20,
			VK_PRIOR = 0x21,
			VK_NEXT = 0x22,
			VK_END = 0x23,
			VK_HOME = 0x24,
			VK_LEFT = 0x25,
			VK_UP = 0x26,
			VK_RIGHT = 0x27,
			VK_DOWN = 0x28,
			VK_SELECT = 0x29,
			VK_PRINT = 0x2A,
			VK_EXECUTE = 0x2B,
			VK_SNAPSHOT = 0x2C,
			VK_INSERT = 0x2D,
			VK_DELETE = 0x2E,
			VK_HELP = 0x2F,
			VK_0 = 0x30,
			VK_1 = 0x31,
			VK_2 = 0x32,
			VK_3 = 0x33,
			VK_4 = 0x34,
			VK_5 = 0x35,
			VK_6 = 0x36,
			VK_7 = 0x37,
			VK_8 = 0x38,
			VK_9 = 0x39,
			VK_A = 0x41,
			VK_B = 0x42,
			VK_C = 0x43,
			VK_D = 0x44,
			VK_E = 0x45,
			VK_F = 0x46,
			VK_G = 0x47,
			VK_H = 0x48,
			VK_I = 0x49,
			VK_J = 0x4A,
			VK_K = 0x4B,
			VK_L = 0x4C,
			VK_M = 0x4D,
			VK_N = 0x4E,
			VK_O = 0x4F,
			VK_P = 0x50,
			VK_Q = 0x51,
			VK_R = 0x52,
			VK_S = 0x53,
			VK_T = 0x54,
			VK_U = 0x55,
			VK_V = 0x56,
			VK_W = 0x57,
			VK_X = 0x58,
			VK_Y = 0x59,
			VK_Z = 0x5A,
			VK_LWIN = 0x5B,
			VK_RWIN = 0x5C,
			VK_APPS = 0x5D,
			VK_SLEEP = 0x5F,
			VK_NUMPAD0 = 0x60,
			VK_NUMPAD1 = 0x61,
			VK_NUMPAD2 = 0x62,
			VK_NUMPAD3 = 0x63,
			VK_NUMPAD4 = 0x64,
			VK_NUMPAD5 = 0x65,
			VK_NUMPAD6 = 0x66,
			VK_NUMPAD7 = 0x67,
			VK_NUMPAD8 = 0x68,
			VK_NUMPAD9 = 0x69,
			VK_MULTIPLY = 0x6A,
			VK_ADD = 0x6B,
			VK_SEPARATOR = 0x6C,
			VK_SUBTRACT = 0x6D,
			VK_DECIMAL = 0x6E,
			VK_DIVIDE = 0x6F,
			VK_F1 = 0x70,
			VK_F2 = 0x71,
			VK_F3 = 0x72,
			VK_F4 = 0x73,
			VK_F5 = 0x74,
			VK_F6 = 0x75,
			VK_F7 = 0x76,
			VK_F8 = 0x77,
			VK_F9 = 0x78,
			VK_F10 = 0x79,
			VK_F11 = 0x7A,
			VK_F12 = 0x7B,
			VK_F13 = 0x7C,
			VK_F14 = 0x7D,
			VK_F15 = 0x7E,
			VK_F16 = 0x7F,
			VK_F17 = 0x80,
			VK_F18 = 0x81,
			VK_F19 = 0x82,
			VK_F20 = 0x83,
			VK_F21 = 0x84,
			VK_F22 = 0x85,
			VK_F23 = 0x86,
			VK_F24 = 0x87,
			VK_NUMLOCK = 0x90,
			VK_SCROLL = 0x91,
			VK_OEM_FJ_JISHO = 0x92,
			VK_OEM_FJ_MASSHOU = 0x93,
			VK_OEM_FJ_TOUROKU = 0x94,
			VK_OEM_FJ_LOYA = 0x95,
			VK_OEM_FJ_ROYA = 0x96,
			VK_LSHIFT = 0xA0,
			VK_RSHIFT = 0xA1,
			VK_LCONTROL = 0xA2,
			VK_RCONTROL = 0xA3,
			VK_LMENU = 0xA4,
			VK_RMENU = 0xA5,
			VK_BROWSER_BACK = 0xA6,
			VK_BROWSER_FORWARD = 0xA7,
			VK_BROWSER_REFRESH = 0xA8,
			VK_BROWSER_STOP = 0xA9,
			VK_BROWSER_SEARCH = 0xAA,
			VK_BROWSER_FAVORITES = 0xAB,
			VK_BROWSER_HOME = 0xAC,
			VK_VOLUME_MUTE = 0xAD,
			VK_VOLUME_DOWN = 0xAE,
			VK_VOLUME_UP = 0xAF,
			VK_MEDIA_NEXT_TRACK = 0xB0,
			VK_MEDIA_PREV_TRACK = 0xB1,
			VK_MEDIA_STOP = 0xB2,
			VK_MEDIA_PLAY_PAUSE = 0xB3,
			VK_LAUNCH_MAIL = 0xB4,
			VK_LAUNCH_MEDIA_SELECT = 0xB5,
			VK_LAUNCH_APP1 = 0xB6,
			VK_LAUNCH_APP2 = 0xB7,
			VK_OEM_1 = 0xBA,
			VK_OEM_PLUS = 0xBB,
			VK_OEM_COMMA = 0xBC,
			VK_OEM_MINUS = 0xBD,
			VK_OEM_PERIOD = 0xBE,
			VK_OEM_2 = 0xBF,
			VK_OEM_3 = 0xC0,
			VK_ABNT_C1 = 0xC1,
			VK_ABNT_C2 = 0xC2,
			VK_OEM_4 = 0xDB,
			VK_OEM_5 = 0xDC,
			VK_OEM_6 = 0xDD,
			VK_OEM_7 = 0xDE,
			VK_OEM_8 = 0xDF,
			VK_OEM_AX = 0xE1,
			VK_OEM_102 = 0xE2,
			VK_ICO_HELP = 0xE3,
			VK_ICO_00 = 0xE4,
			VK_PROCESSKEY = 0xE5,
			VK_ICO_CLEAR = 0xE6,
			VK_PACKET = 0xE7,
			VK_OEM_RESET = 0xE9,
			VK_OEM_JUMP = 0xEA,
			VK_OEM_PA1 = 0xEB,
			VK_OEM_PA2 = 0xEC,
			VK_OEM_PA3 = 0xED,
			VK_OEM_WSCTRL = 0xEE,
			VK_OEM_CUSEL = 0xEF,
			VK_OEM_ATTN = 0xF0,
			VK_OEM_FINISH = 0xF1,
			VK_OEM_COPY = 0xF2,
			VK_OEM_AUTO = 0xF3,
			VK_OEM_ENLW = 0xF4,
			VK_OEM_BACKTAB = 0xF5,
			VK_ATTN = 0xF6,
			VK_CRSEL = 0xF7,
			VK_EXSEL = 0xF8,
			VK_EREOF = 0xF9,
			VK_PLAY = 0xFA,
			VK_ZOOM = 0xFB,
			VK_NONAME = 0xFC,
			VK_PA1 = 0xFD,
			VK_OEM_CLEAR = 0xFE,
			VK_NONE = 0xFF,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICE
		{
			public HidUsagePage usUsagePage;
			public HidUsageId usUsage;
			public RIDEV dwFlags;
			public IntPtr hwndTarget;

			public enum HidUsagePage : ushort
			{
				GENERIC = 1,
				GAME = 5,
				LED = 8,
				BUTTON = 9,
			}

			public enum HidUsageId : ushort
			{
				GENERIC_POINTER = 1,
				GENERIC_MOUSE = 2,
				GENERIC_JOYSTICK = 4,
				GENERIC_GAMEPAD = 5,
				GENERIC_KEYBOARD = 6,
				GENERIC_KEYPAD = 7,
				GENERIC_MULTI_AXIS_CONTROLLER = 8,
			}

			[Flags]
			public enum RIDEV : int
			{
				REMOVE = 0x00000001,
				EXCLUDE = 0x00000010,
				PAGEONLY = 0x00000020,
				NOLEGACY = PAGEONLY | EXCLUDE,
				INPUTSINK = 0x00000100,
				CAPTUREMOUSE = 0x00000200,
				NOHOTKEYS = CAPTUREMOUSE,
				APPKEYS = 0x00000400,
				EXINPUTSINK = 0x00001000,
				DEVNOTIFY = 0x00002000,
			}
		}

		public enum RID : uint
		{
			HEADER = 0x10000005,
			INPUT = 0x10000003,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTHEADER
		{
			public RIM_TYPE dwType;
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam;

			public enum RIM_TYPE : uint
			{
				MOUSE = 0,
				KEYBOARD = 1,
				HID = 2,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWMOUSE
		{
			public ushort usFlags;
			public uint ulButtons;
			public uint ulRawButtons;
			public int lLastX;
			public int lLastY;
			public uint ulExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWKEYBOARD
		{
			public ushort MakeCode;
			public RI_KEY Flags;
			public ushort Reserved;
			public VirtualKey VKey;
			public uint Message;
			public uint ExtraInformation;

			[Flags]
			public enum RI_KEY : ushort
			{
				MAKE = 0,
				BREAK = 1,
				E0 = 2,
				E1 = 4,
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWHID
		{
			public uint dwSizeHid;
			public uint dwCount;
			public byte bRawData;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct RAWINPUTDATA
		{
			[FieldOffset(0)]
			public RAWMOUSE mouse;
			[FieldOffset(0)]
			public RAWKEYBOARD keyboard;
			[FieldOffset(0)]
			public RAWHID hid;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUT
		{
			public RAWINPUTHEADER header;
			public RAWINPUTDATA data;
		}

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern int GetRawInputData(IntPtr hRawInput, RID uiCommand, IntPtr pData, out int bSize, int cbSizeHeader);

		[DllImport("user32.dll", ExactSpelling = true)]
		public static extern unsafe int GetRawInputData(IntPtr hRawInput, RID uiCommand, RAWINPUT* pData, ref int bSize, int cbSizeHeader);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		public static extern unsafe int GetRawInputBuffer(RAWINPUT* pData, ref int bSize, int cbSizeHeader);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterRawInputDevices(ref RAWINPUTDEVICE pRawInputDevice, uint uiNumDevices, int cbSize);

		[DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, int cbSize);

		[DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern uint MapVirtualKeyW(uint uCode, uint uMapType);
	}
}
