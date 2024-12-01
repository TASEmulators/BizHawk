using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Libretro
{
	public abstract class LibretroApi
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		public enum RETRO_DEVICE : int
		{
			NONE = 0,
			JOYPAD = 1,
			MOUSE = 2,
			KEYBOARD = 3,
			LIGHTGUN = 4,
			ANALOG = 5,
			POINTER = 6,

			LAST,
		}

		public enum RETRO_DEVICE_ID_ANALOG
		{
			// LEFT / RIGHT?
			X = 0,
			Y = 1,
			BUTTON = 2,

			LAST,
		}

		public enum RETRO_DEVICE_ID_MOUSE
		{
			X = 0,
			Y = 1,
			LEFT = 2,
			RIGHT = 3,
			WHEELUP = 4,
			WHEELDOWN = 5,
			MIDDLE = 6,
			HORIZ_WHEELUP = 7,
			HORIZ_WHEELDOWN = 8,
			BUTTON_4 = 9,
			BUTTON_5 = 10,

			LAST,
		}

		public enum RETRO_DEVICE_ID_LIGHTGUN
		{
			X = 0,
			Y = 1,
			TRIGGER = 2,
			CURSOR = 3,
			TURBO = 4,
			PAUSE = 5,
			START = 6,
			SELECT = 7,
			AUX_C = 8,
			DPAD_UP = 9,
			DPAD_DOWN = 10,
			DPAD_LEFT = 11,
			DPAD_RIGHT = 12,
			SCREEN_X = 13,
			SCREEN_Y = 14,
			IS_OFFSCREEN = 15,
			RELOAD = 16,

			LAST,
		}

		public enum RETRO_DEVICE_ID_POINTER
		{
			X = 0,
			Y = 1,
			PRESSED = 2,
			COUNT = 3,

			LAST,
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

			LAST,
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
			SCROLLLOCK = 64,
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
			R3 = 15,

			LAST,
		}

		public enum RETRO_SENSOR
		{
			ACCELEROMETER_X = 0,
			ACCELEROMETER_Y = 1,
			ACCELEROMETER_Z = 2,
			GYROSCOPE_X = 3,
			GYROSCOPE_Y = 4,
			GYROSCOPE_Z = 5,
			ILLUMINANCE = 6,

			LAST,
		}

		public enum RETRO_REGION : uint
		{
			NTSC = 0,
			PAL = 1,
		}

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
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_message
		{
			public IntPtr msg;
			public uint frames;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_system_info
		{
			public IntPtr library_name;
			public IntPtr library_version;
			public IntPtr valid_extensions;
			public bool need_fullpath;
			public bool block_extract;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_game_info
		{
			public IntPtr path;
			public IntPtr data;
			public long size;
			public IntPtr meta;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_game_geometry
		{
			public uint base_width;
			public uint base_height;
			public uint max_width;
			public uint max_height;
			public float aspect_ratio;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_system_timing
		{
			public double fps;
			public double sample_rate;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct retro_system_av_info
		{
			public retro_game_geometry geometry;
			public retro_system_timing timing;
		}

		[BizImport(cc)]
		public abstract void retro_init();

		[BizImport(cc)]
		public abstract void retro_deinit();

		[BizImport(cc)]
		public abstract uint retro_api_version();

		[BizImport(cc)]
		public abstract void retro_get_system_info(out retro_system_info retro_system_info);

		// this is allowed to not initialize every variable, so ref is used instead of out

		[BizImport(cc)]
		public abstract void retro_get_system_av_info(ref retro_system_av_info retro_system_av_info);

		[BizImport(cc)]
		public abstract void retro_set_environment(IntPtr retro_environment);

		[BizImport(cc)]
		public abstract void retro_set_video_refresh(IntPtr retro_video_refresh);

		[BizImport(cc)]
		public abstract void retro_set_audio_sample(IntPtr retro_audio_sample);

		[BizImport(cc)]
		public abstract long retro_set_audio_sample_batch(IntPtr retro_audio_sample_batch);

		[BizImport(cc)]
		public abstract void retro_set_input_poll(IntPtr retro_input_poll);

		[BizImport(cc)]
		public abstract void retro_set_input_state(IntPtr retro_input_state);

		[BizImport(cc)]
		public abstract void retro_set_controller_port_device(uint port, uint device);

		[BizImport(cc)]
		public abstract void retro_reset();

		[BizImport(cc)]
		public abstract void retro_run();

		[BizImport(cc)]
		public abstract long retro_serialize_size();

		[BizImport(cc)]
		public abstract bool retro_serialize(byte[] data, long size);

		[BizImport(cc)]
		public abstract bool retro_unserialize(byte[] data, long size);

		[BizImport(cc)]
		public abstract void retro_cheat_reset();

		[BizImport(cc)]
		public abstract void retro_cheat_set(uint index, bool enabled, string code);

		// maybe it would be better if retro_game_info was just a class instead of a struct?

		[BizImport(cc, EntryPoint = "retro_load_game")]
		public abstract bool retro_load_no_game(IntPtr no_game_info = default); // don't send anything here

		[BizImport(cc)]
		public abstract bool retro_load_game(ref retro_game_info retro_game_info);

		[BizImport(cc)]
		public abstract bool retro_load_game_special(uint game_type, ref retro_game_info retro_game_info, long num_info);

		[BizImport(cc)]
		public abstract void retro_unload_game();

		[BizImport(cc)]
		public abstract RETRO_REGION retro_get_region();

		[BizImport(cc)]
		public abstract IntPtr retro_get_memory_data(RETRO_MEMORY id);

		[BizImport(cc)]
		public abstract long retro_get_memory_size(RETRO_MEMORY id);
	}

	public abstract class LibretroBridge
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract IntPtr LibretroBridge_CreateCallbackHandler();

		[BizImport(cc)]
		public abstract void LibretroBridge_DestroyCallbackHandler(IntPtr cbHandler);

		[BizImport(cc)]
		public abstract void LibretroBridge_SetGlobalCallbackHandler(IntPtr cbHandler);

		[BizImport(cc)]
		public abstract bool LibretroBridge_GetSupportsNoGame(IntPtr cbHandler);

		[BizImport(cc)]
		public abstract bool LibretroBridge_GetRetroGeometryInfo(IntPtr cbHandler, ref LibretroApi.retro_game_geometry g);

		[BizImport(cc)]
		public abstract bool LibretroBridge_GetRetroTimingInfo(IntPtr cbHandler, ref LibretroApi.retro_system_timing t);

		[BizImport(cc)]
		public abstract void LibretroBridge_GetRetroMessage(IntPtr cbHandler, out LibretroApi.retro_message m);

		[BizImport(cc)]
		public abstract void LibretroBridge_SetDirectories(IntPtr cbHandler, string systemDirectory, string saveDirectory, string coreDirectory, string coreAssetsDirectory);

		[BizImport(cc)]
		public abstract void LibretroBridge_SetVideoSize(IntPtr cbHandler, int sz);

		[BizImport(cc)]
		public abstract void LibretroBridge_GetVideo(IntPtr cbHandler, out int width, out int height, int[] videoBuf);

		[BizImport(cc)]
		public abstract uint LibretroBridge_GetAudioSize(IntPtr cbHandler);

		[BizImport(cc)]
		public abstract void LibretroBridge_GetAudio(IntPtr cbHandler, out int numSamples, short[] sampleBuf);

		[BizImport(cc)]
		public abstract void LibretroBridge_SetInput(IntPtr cbHandler, LibretroApi.RETRO_DEVICE device, int port, short[] input);

		public struct retro_procs
		{
			public IntPtr retro_environment_proc;
			public IntPtr retro_video_refresh_proc;
			public IntPtr retro_audio_sample_proc;
			public IntPtr retro_audio_sample_batch_proc;
			public IntPtr retro_input_poll_proc;
			public IntPtr retro_input_state_proc;
		}

		[BizImport(cc)]
		public abstract void LibretroBridge_GetRetroProcs(out retro_procs cb_procs);
	}
}
