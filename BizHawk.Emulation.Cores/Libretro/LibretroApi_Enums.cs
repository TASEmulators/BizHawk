using System;

namespace BizHawk.Emulation.Cores.Libretro
{
	unsafe partial class LibretroApi
	{
		public enum eMessage : int
		{
			NotSet,

			Resume,

			QUERY_FIRST,
			QUERY_GetMemory,
			QUERY_LAST,

			CMD_FIRST,
			CMD_SetEnvironment,
			CMD_LoadNoGame,
			CMD_LoadData,
			CMD_LoadPath,
			CMD_Deinit,
			CMD_Reset,
			CMD_Run,
			CMD_UpdateSerializeSize,
			CMD_Serialize,
			CMD_Unserialize,
			CMD_LAST,

			SIG_InputState,
			SIG_VideoUpdate,
			SIG_Sample,
			SIG_SampleBatch,
		}


		public enum RETRO_MEMORY
		{
			SAVE_RAM = 0,
			RTC = 1,
			SYSTEM_RAM = 2,
			VIDEO_RAM = 3,
		}


		public enum RETRO_DEVICE
		{
			NONE = 0,
			JOYPAD = 1,
			MOUSE = 2,
			KEYBOARD = 3,
			LIGHTGUN = 4,
			ANALOG = 5,
			POINTER = 6,
			SENSOR_ACCELEROMETER = 7
		}

	public enum RETRO_DEVICE_ID_ANALOG
		{
			// LEFT / RIGHT?
			X = 0,
			Y = 1
		}

		public enum RETRO_DEVICE_ID_MOUSE
		{
			X = 0,
			Y = 1,
			LEFT = 2,
			RIGHT = 3
		}

		public enum RETRO_DEVICE_ID_LIGHTGUN
		{
			X = 0,
			Y = 1,
			TRIGGER = 2,
			CURSOR = 3,
			TURBO = 4,
			PAUSE = 5,
			START = 6
		}

		public enum RETRO_DEVICE_ID_POINTER
		{
			X = 0,
			Y = 1,
			PRESSED = 2
		}

				public enum RETRO_KEY
		{
			UNKNOWN = 0,
			FIRST = 0,
			BACKSPACE = 8,
			TAB = 9,
			CLEAR = 12,
			RETURN = 13,
			PAUSE = 19,
			ESCAPE = 27,
			SPACE = 32,
			EXCLAIM = 33,
			QUOTEDBL = 34,
			HASH = 35,
			DOLLAR = 36,
			AMPERSAND = 38,
			QUOTE = 39,
			LEFTPAREN = 40,
			RIGHTPAREN = 41,
			ASTERISK = 42,
			PLUS = 43,
			COMMA = 44,
			MINUS = 45,
			PERIOD = 46,
			SLASH = 47,
			_0 = 48,
			_1 = 49,
			_2 = 50,
			_3 = 51,
			_4 = 52,
			_5 = 53,
			_6 = 54,
			_7 = 55,
			_8 = 56,
			_9 = 57,
			COLON = 58,
			SEMICOLON = 59,
			LESS = 60,
			EQUALS = 61,
			GREATER = 62,
			QUESTION = 63,
			AT = 64,
			LEFTBRACKET = 91,
			BACKSLASH = 92,
			RIGHTBRACKET = 93,
			CARET = 94,
			UNDERSCORE = 95,
			BACKQUOTE = 96,
			a = 97,
			b = 98,
			c = 99,
			d = 100,
			e = 101,
			f = 102,
			g = 103,
			h = 104,
			i = 105,
			j = 106,
			k = 107,
			l = 108,
			m = 109,
			n = 110,
			o = 111,
			p = 112,
			q = 113,
			r = 114,
			s = 115,
			t = 116,
			u = 117,
			v = 118,
			w = 119,
			x = 120,
			y = 121,
			z = 122,
			DELETE = 127,

			KP0 = 256,
			KP1 = 257,
			KP2 = 258,
			KP3 = 259,
			KP4 = 260,
			KP5 = 261,
			KP6 = 262,
			KP7 = 263,
			KP8 = 264,
			KP9 = 265,
			KP_PERIOD = 266,
			KP_DIVIDE = 267,
			KP_MULTIPLY = 268,
			KP_MINUS = 269,
			KP_PLUS = 270,
			KP_ENTER = 271,
			KP_EQUALS = 272,

			UP = 273,
			DOWN = 274,
			RIGHT = 275,
			LEFT = 276,
			INSERT = 277,
			HOME = 278,
			END = 279,
			PAGEUP = 280,
			PAGEDOWN = 281,

			F1 = 282,
			F2 = 283,
			F3 = 284,
			F4 = 285,
			F5 = 286,
			F6 = 287,
			F7 = 288,
			F8 = 289,
			F9 = 290,
			F10 = 291,
			F11 = 292,
			F12 = 293,
			F13 = 294,
			F14 = 295,
			F15 = 296,

			NUMLOCK = 300,
			CAPSLOCK = 301,
			SCROLLOCK = 302,
			RSHIFT = 303,
			LSHIFT = 304,
			RCTRL = 305,
			LCTRL = 306,
			RALT = 307,
			LALT = 308,
			RMETA = 309,
			LMETA = 310,
			LSUPER = 311,
			RSUPER = 312,
			MODE = 313,
			COMPOSE = 314,

			HELP = 315,
			PRINT = 316,
			SYSREQ = 317,
			BREAK = 318,
			MENU = 319,
			POWER = 320,
			EURO = 321,
			UNDO = 322,

			LAST
		}

		[Flags]
		public enum RETRO_MOD
		{
			NONE = 0,
			SHIFT = 1,
			CTRL = 2,
			ALT = 4,
			META = 8,
			NUMLOCK = 16,
			CAPSLOCK = 32,
			SCROLLLOCK = 64
		}

		public enum RETRO_DEVICE_ID_SENSOR_ACCELEROMETER
		{
			X = 0,
			Y = 1,
			Z = 2
		}

		public enum RETRO_DEVICE_ID_JOYPAD
		{
			B = 0,
			Y = 1,
			SELECT = 2,
			START = 3,
			UP = 4,
			DOWN = 5,
			LEFT = 6,
			RIGHT = 7,
			A = 8,
			X = 9,
			L = 10,
			R = 11,
			L2 = 12,
			R2 = 13,
			L3 = 14,
			R3 = 15
		}

		public enum eStatus : int
		{
			eStatus_Idle,
			eStatus_CMD,
			eStatus_BRK
		}

		public enum BufId : int
		{
			Param0 = 0,
			Param1 = 1,
			SystemDirectory = 2,
			SaveDirectory = 3,
			CoreDirectory = 4,
			CoreAssetsDirectory = 5,
			BufId_Num
		}

		//libretro enums:

		public enum retro_pixel_format : uint
		{
			XRGB1555 = 0,
			XRGB8888 = 1,
			RGB565 = 2
		}

		public enum retro_region : uint
		{
			NTSC = 0,
			PAL = 1
		}

	}
}
