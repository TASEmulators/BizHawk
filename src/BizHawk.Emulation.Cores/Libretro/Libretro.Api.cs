using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Libretro
{
	public abstract class LibretroApi
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		public enum RETRO_ENVIRONMENT : uint
		{
			SET_ROTATION = 1,
			GET_OVERSCAN = 2,
			GET_CAN_DUPE = 3,
			SET_MESSAGE = 6,
			SHUTDOWN = 7,
			SET_PERFORMANCE_LEVEL = 8,
			GET_SYSTEM_DIRECTORY = 9,
			SET_PIXEL_FORMAT = 10,
			SET_INPUT_DESCRIPTORS = 11,
			SET_KEYBOARD_CALLBACK = 12,
			SET_DISK_CONTROL_INTERFACE = 13,
			SET_HW_RENDER = 14,
			GET_VARIABLE = 15,
			SET_VARIABLES = 16,
			GET_VARIABLE_UPDATE = 17,
			SET_SUPPORT_NO_GAME = 18,
			GET_LIBRETRO_PATH = 19,
			SET_AUDIO_CALLBACK = 22,
			SET_FRAME_TIME_CALLBACK = 21,
			GET_RUMBLE_INTERFACE = 23,
			GET_INPUT_DEVICE_CAPABILITIES = 24,
			GET_SENSOR_INTERFACE = 25 | EXPERIMENTAL,
			GET_CAMERA_INTERFACE = 26 | EXPERIMENTAL,
			GET_LOG_INTERFACE = 27,
			GET_PERF_INTERFACE = 28,
			GET_LOCATION_INTERFACE = 29,
			GET_CONTENT_DIRECTORY = 30,
			GET_CORE_ASSETS_DIRECTORY = 30,
			GET_SAVE_DIRECTORY = 31,
			SET_SYSTEM_AV_INFO = 32,
			SET_PROC_ADDRESS_CALLBACK = 33,
			SET_SUBSYSTEM_INFO = 34,
			SET_CONTROLLER_INFO = 35,
			SET_MEMORY_MAPS = 36 | EXPERIMENTAL,
			SET_GEOMETRY = 37,
			GET_USERNAME = 38,
			GET_LANGUAGE = 39,
			GET_CURRENT_SOFTWARE_FRAMEBUFFER = 40 | EXPERIMENTAL,
			GET_HW_RENDER_INTERFACE = 41 | EXPERIMENTAL,
			SET_SUPPORT_ACHIEVEMENTS = 42 | EXPERIMENTAL,
			SET_HW_RENDER_CONTEXT_NEGOTIATION_INTERFACE = 43 | EXPERIMENTAL,
			SET_SERIALIZATION_QUIRKS = 44,
			EXPERIMENTAL = 0x10000,
		}

		public enum RETRO_DEVICE : int
		{
			NONE = 0,
			JOYPAD = 1,
			MOUSE = 2,
			KEYBOARD = 3,
			LIGHTGUN = 4,
			ANALOG = 5,
			POINTER = 6,
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

		public enum RETRO_PIXEL_FORMAT : int
		{
			ZRGB1555 = 0,
			XRGB8888 = 1,
			RGB565 = 2,
			UNKNOWN = int.MaxValue,
		}

		public enum RETRO_LANGUAGE : int
		{
			ENGLISH = 0,
			JAPANESE = 1,
			FRENCH = 2,
			SPANISH = 3,
			GERMAN = 4,
			ITALIAN = 5,
			DUTCH = 6,
			PORTUGUESE = 7,
			RUSSIAN = 8,
			KOREAN = 9,
			CHINESE_TRADITIONAL = 10,
			CHINESE_SIMPLIFIED = 11,
			ESPERANTO = 12,
			POLISH = 13,
			VIETNAMESE = 14,
			LAST,

			DUMMY = int.MaxValue
		}

		public enum RETRO_LOG : int
		{
			DEBUG = 0,
			INFO,
			WARN,
			ERROR,
			DUMMY = int.MaxValue,
		};

		public enum RETRO_REGION : uint
		{
			NTSC = 0,
			PAL = 1,
		};

		public enum RETRO_MEMORY : uint
		{
			SAVE_RAM = 0,
			RTC = 1,
			SYSTEM_RAM = 2,
			VIDEO_RAM = 3,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_variable
		{
			public IntPtr key;
			public IntPtr value;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_message
		{
			public IntPtr msg;
			public uint frames;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_log_callback
		{
			public IntPtr log;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_system_info
		{
			public IntPtr library_name;
			public IntPtr library_version;
			public IntPtr valid_extensions;
			public bool need_fullpath;
			public bool block_extract;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_game_info
		{
			public IntPtr path;
			public IntPtr data;
			public long size;
			public IntPtr meta;
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_system_av_info
		{
			// struct retro_game_geometry
			public uint base_width;
			public uint base_height;
			public uint max_width;
			public uint max_height;
			public float aspect_ratio;

			// struct retro_system_timing
			public double fps;
			public double sample_rate;
		};

		[UnmanagedFunctionPointer(cc)]
		public delegate void retro_log_printf_t(RETRO_LOG level, IntPtr fmt, IntPtr args);

		[UnmanagedFunctionPointer(cc)]
		public delegate bool retro_environment_t(RETRO_ENVIRONMENT cmd, IntPtr data);

		[UnmanagedFunctionPointer(cc)]
		public delegate void retro_video_refresh_t(IntPtr data, uint width, uint height, long pitch);

		[UnmanagedFunctionPointer(cc)]
		public delegate void retro_audio_sample_t(short left, short right);

		[UnmanagedFunctionPointer(cc)]
		public delegate void retro_audio_sample_batch_t(IntPtr data, long frames);

		[UnmanagedFunctionPointer(cc)]
		public delegate void retro_input_poll_t();

		[UnmanagedFunctionPointer(cc)]
		public delegate short retro_input_state_t(uint port, uint device, uint index, uint id);

		[BizImport(cc)]
		public abstract void retro_init();

		[BizImport(cc)]
		public abstract void retro_deinit();

		[BizImport(cc)]
		public abstract uint retro_api_version();

		[BizImport(cc)]
		public abstract void retro_get_system_info(IntPtr retro_system_info);

		[BizImport(cc)]
		public abstract void retro_get_system_av_info(IntPtr retro_system_av_info);

		[BizImport(cc)]
		public abstract void retro_set_environment(retro_environment_t cb);

		[BizImport(cc)]
		public abstract void retro_set_video_refresh(retro_video_refresh_t cb);

		[BizImport(cc)]
		public abstract void retro_set_audio_sample(retro_audio_sample_t cb);

		[BizImport(cc)]
		public abstract long retro_set_audio_sample_batch(retro_audio_sample_batch_t cb);

		[BizImport(cc)]
		public abstract void retro_set_input_poll(retro_input_poll_t cb);

		[BizImport(cc)]
		public abstract void retro_set_input_state(retro_input_state_t cb);

		[BizImport(cc)]
		public abstract void retro_set_controller_port_device(uint port, uint device);

		[BizImport(cc)]
		public abstract void retro_reset();

		[BizImport(cc)]
		public abstract void retro_run();

		[BizImport(cc)]
		public abstract long retro_serialize_size();

		[BizImport(cc)]
		public abstract bool retro_serialize(IntPtr data, long size);

		[BizImport(cc)]
		public abstract bool retro_unserialize(IntPtr data, long size);

		[BizImport(cc)]
		public abstract void retro_cheat_reset();

		[BizImport(cc)]
		public abstract void retro_cheat_set(uint index, bool enabled, IntPtr code);

		[BizImport(cc)]
		public abstract bool retro_load_game(IntPtr retro_game_info);

		[BizImport(cc)]
		public abstract bool retro_load_game_special(uint game_type, IntPtr retro_game_info, long num_info);

		[BizImport(cc)]
		public abstract void retro_unload_game();

		[BizImport(cc)]
		public abstract RETRO_REGION retro_get_region();

		[BizImport(cc)]
		public abstract IntPtr retro_get_memory_data(RETRO_MEMORY id);

		[BizImport(cc)]
		public abstract long retro_get_memory_size(RETRO_MEMORY id);
	}
}
