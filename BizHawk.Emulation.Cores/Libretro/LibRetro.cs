using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

#pragma warning disable 649, 169

using BizHawk.Common;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// libretro related shims
	/// </summary>
	public class LibRetro : IDisposable
	{
		public const int RETRO_API_VERSION = 1;

		public enum RETRO_ROTATION
		{
			ROTATION_0_CCW = 0,
			ROTATION_90_CCW = 1,
			ROTATION_180_CCW = 2,
			ROTATION_270_CCW = 3,
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
		};

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
		};

		public enum RETRO_LOG_LEVEL : int //exact type size is unclear
		{
			 DEBUG = 0,
			 INFO,
			 WARN,
			 ERROR,

			 DUMMY = Int32.MaxValue
		};

		public enum RETRO_DEVICE_ID_ANALOG
		{
			// LEFT / RIGHT?
			X = 0,
			Y = 1
		};

		public enum RETRO_DEVICE_ID_MOUSE
		{
			X = 0,
			Y = 1,
			LEFT = 2,
			RIGHT = 3
		};

		public enum RETRO_DEVICE_ID_LIGHTGUN
		{
			X = 0,
			Y = 1,
			TRIGGER = 2,
			CURSOR = 3,
			TURBO = 4,
			PAUSE = 5,
			START = 6
		};

		public enum RETRO_DEVICE_ID_POINTER
		{
			X = 0,
			Y = 1,
			PRESSED = 2
		};

		public enum RETRO_DEVICE_ID_SENSOR_ACCELEROMETER
		{
			X = 0,
			Y = 1,
			Z = 2
		};

		public enum RETRO_REGION
		{
			NTSC = 0,
			PAL = 1
		};

		public enum RETRO_MEMORY
		{
			SAVE_RAM = 0,
			RTC = 1,
			SYSTEM_RAM = 2,
			VIDEO_RAM = 3,
		};

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
		};

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
		};

		[Flags]
		public enum RETRO_SIMD
		{
			SSE = (1 << 0),
			SSE2 = (1 << 1),
			VMX = (1 << 2),
			VMX128 = (1 << 3),
			AVX = (1 << 4),
			NEON = (1 << 5),
			SSE3 = (1 << 6),
			SSSE3 = (1 << 7),
			MMX = (1 << 8),
			MMXEXT = (1 << 9),
			SSE4 = (1 << 10),
			SSE42 = (1 << 11),
			AVX2 = (1 << 12),
			VFPU = (1 << 13),
			PS = (1 << 14),
			AES = (1 << 15),
			VFPV3 = (1 << 16),
			VFPV4 = (1 << 17),
		}

		public enum RETRO_ENVIRONMENT
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
			//25,26 are experimental
			GET_LOG_INTERFACE = 27,
			GET_PERF_INTERFACE = 28,
			GET_LOCATION_INTERFACE = 29,
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

			EXPERIMENTAL = 0x10000
		};

		public enum retro_hw_context_type
		{
			 RETRO_HW_CONTEXT_NONE             = 0,
			 RETRO_HW_CONTEXT_OPENGL           = 1, 
			 RETRO_HW_CONTEXT_OPENGLES2        = 2,
			 RETRO_HW_CONTEXT_OPENGL_CORE      = 3,
			 RETRO_HW_CONTEXT_OPENGLES3        = 4,
			 RETRO_HW_CONTEXT_OPENGLES_VERSION = 5,

			 RETRO_HW_CONTEXT_DUMMY = Int32.MaxValue
		};

		public struct retro_hw_render_callback
		{
			public uint context_type; //retro_hw_context_type
			public IntPtr context_reset; //retro_hw_context_reset_t
			public IntPtr get_current_framebuffer; //retro_hw_get_current_framebuffer_t
			public IntPtr get_proc_address; //retro_hw_get_proc_address_t
			[MarshalAs(UnmanagedType.U1)] public byte depth;
			[MarshalAs(UnmanagedType.U1)] public bool stencil;
			[MarshalAs(UnmanagedType.U1)] public bool bottom_left_origin;
			public uint version_major;
			public uint version_minor;
			[MarshalAs(UnmanagedType.U1)] public bool cache_context;
			public IntPtr context_destroy; //retro_hw_context_reset_t
			[MarshalAs(UnmanagedType.U1)] public bool debug_context;
		};

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long retro_hw_context_reset_t();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr retro_hw_get_current_framebuffer_t();

		//not used
		//[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		//public delegate void retro_proc_address_t();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr retro_hw_get_proc_address_t(string sym);

		struct retro_memory_map
		{
			public IntPtr descriptors; //retro_memory_descriptor *
			public uint num_descriptors;
		};

		struct retro_memory_descriptor
		{
			ulong flags;
			IntPtr ptr;
			IntPtr offset; //size_t
			IntPtr start; //size_t
			IntPtr select; //size_t
			IntPtr disconnect; //size_t
			IntPtr len; //size_t
			string addrspace;
		};

		public enum RETRO_PIXEL_FORMAT
		{
			XRGB1555 = 0,
			XRGB8888 = 1,
			RGB565 = 2
		};

		public struct retro_message
		{
			public string msg;
			public uint frames;
		}

		public struct retro_input_descriptor
		{
			public uint port;
			public uint device;
			public uint index;
			public uint id;
		}

		public struct retro_system_info
		{
			public string library_name;
			public string library_version;
			public string valid_extensions;
			[MarshalAs(UnmanagedType.U1)]
			public bool need_fullpath;
			[MarshalAs(UnmanagedType.U1)]
			public bool block_extract;
		}

		public struct retro_game_geometry
		{
			public uint base_width;
			public uint base_height;
			public uint max_width;
			public uint max_height;
			public float aspect_ratio;
		}

		public struct retro_system_timing
		{
			public double fps;
			public double sample_rate;
		}

		public struct retro_system_av_info
		{
			public retro_game_geometry geometry;
			public retro_system_timing timing;
		}

		public struct retro_variable
		{
			public string key;
			public string value;
		}

		public struct retro_game_info
		{
			public string path;
			public IntPtr data;
			public uint size;
			public string meta;
		}

		//untested
		public struct retro_perf_counter
		{
			public string ident;
			public ulong start;
			public ulong total;
			public ulong call_cnt;

			[MarshalAs(UnmanagedType.U1)]
			public bool registered;
		};

		//perf callbacks
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long retro_perf_get_time_usec_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate long retro_perf_get_counter_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate ulong retro_get_cpu_features_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_log_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_register_t(ref retro_perf_counter counter);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_start_t(ref retro_perf_counter counter);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_perf_stop_t(ref retro_perf_counter counter);

		//for GET_PERF_INTERFACE
		public struct retro_perf_callback
		{
			public retro_perf_get_time_usec_t get_time_usec;
			public retro_get_cpu_features_t get_cpu_features;
			public retro_perf_get_counter_t get_perf_counter;
			public retro_perf_register_t perf_register;
			public retro_perf_start_t perf_start;
			public retro_perf_stop_t perf_stop;
			public retro_perf_log_t perf_log;
		}

		#region callback prototypes
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public unsafe delegate void retro_log_printf_t(RETRO_LOG_LEVEL level, string fmt, IntPtr a0, IntPtr a1, IntPtr a2, IntPtr a3, IntPtr a4, IntPtr a5, IntPtr a6, IntPtr a7, IntPtr a8, IntPtr a9, IntPtr a10, IntPtr a11, IntPtr a12, IntPtr a13, IntPtr a14, IntPtr a15);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return:MarshalAs(UnmanagedType.U1)]
		public delegate bool retro_environment_t(RETRO_ENVIRONMENT cmd, IntPtr data);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_video_refresh_t(IntPtr data, uint width, uint height, uint pitch);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_audio_sample_t(short left, short right);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint retro_audio_sample_batch_t(IntPtr data, uint frames);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void retro_input_poll_t();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate short retro_input_state_t(uint port, uint device, uint index, uint id);
		#endregion

		#region entry point prototypes
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_environment(retro_environment_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_video_refresh(retro_video_refresh_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_audio_sample(retro_audio_sample_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_audio_sample_batch(retro_audio_sample_batch_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_input_poll(retro_input_poll_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_input_state(retro_input_state_t cb);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_init();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_deinit();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint epretro_api_version();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_get_system_info(ref retro_system_info info);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_get_system_av_info(ref retro_system_av_info info);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_set_controller_port_device(uint port, uint device);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_reset();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_run();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint epretro_serialize_size();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool epretro_serialize(IntPtr data, uint size);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool epretro_unserialize(IntPtr data, uint size);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_cheat_reset();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_cheat_set(uint index, [MarshalAs(UnmanagedType.U1)]bool enabled, string code);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool epretro_load_game(ref retro_game_info game);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.U1)]
		public delegate bool epretro_load_game_special(uint game_type, ref retro_game_info info, uint num_info);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void epretro_unload_game();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint epretro_get_region();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr epretro_get_memory_data(RETRO_MEMORY id);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate uint epretro_get_memory_size(RETRO_MEMORY id);
		#endregion

		#region entry points
		// these are all hooked up by reflection on dll load
		public epretro_set_environment retro_set_environment;
		public epretro_set_video_refresh retro_set_video_refresh;
		public epretro_set_audio_sample retro_set_audio_sample;
		public epretro_set_audio_sample_batch retro_set_audio_sample_batch;
		public epretro_set_input_poll retro_set_input_poll;
		public epretro_set_input_state retro_set_input_state;
		public epretro_init retro_init;
		/// <summary>
		/// Dispose() calls this, so you shouldn't
		/// </summary>
		public epretro_deinit retro_deinit;
		public epretro_api_version retro_api_version;
		public epretro_get_system_info retro_get_system_info;
		public epretro_get_system_av_info retro_get_system_av_info;
		public epretro_set_controller_port_device retro_set_controller_port_device;
		public epretro_reset retro_reset;
		public epretro_run retro_run;
		public epretro_serialize_size retro_serialize_size;
		public epretro_serialize retro_serialize;
		public epretro_unserialize retro_unserialize;
		public epretro_cheat_reset retro_cheat_reset;
		public epretro_cheat_set retro_cheat_set;
		public epretro_load_game retro_load_game;
		public epretro_load_game_special retro_load_game_special;
		public epretro_unload_game retro_unload_game;
		public epretro_get_region retro_get_region;
		public epretro_get_memory_data retro_get_memory_data;
		public epretro_get_memory_size retro_get_memory_size;
		#endregion

		public void Dispose()
		{
			dll.Dispose();
		}

		InstanceDll dll;
		public LibRetro(string modulename)
		{
			dll = new InstanceDll(modulename);
			if (!ConnectAllEntryPoints())
			{
				dll.Dispose();
				throw new Exception("ConnectAllEntryPoints() failed.  The console may contain more details.");
			}
		}

		private static IEnumerable<FieldInfo> GetAllEntryPoints()
		{
			return typeof(LibRetro).GetFields().Where((field) => field.FieldType.Name.StartsWith("epretro"));
		}

		private void ClearAllEntryPoints()
		{
			foreach (var field in GetAllEntryPoints())
			{
				field.SetValue(this, null);
			}
		}

		private bool ConnectAllEntryPoints()
		{
			bool succeed = true;
			foreach (var field in GetAllEntryPoints())
			{
				string fieldname = field.Name;
				IntPtr entry = dll.GetProcAddress(fieldname);
				if (entry != IntPtr.Zero)
				{
					field.SetValue(this, Marshal.GetDelegateForFunctionPointer(entry, field.FieldType));
				}
				else
				{
					Console.WriteLine("Couldn't bind libretro entry point {0}", fieldname);
					succeed = false;
				}
			}
			return succeed;
		}
	}
}
