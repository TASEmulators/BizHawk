using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public abstract class LibMAME
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		// enums
		public enum OutputChannel : int
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		}

		// constants
		public const int ROMENTRYTYPE_SYSTEM_BIOS = 8;
		public const int ROMENTRYTYPE_DEFAULT_BIOS = 9;
		public const int ROMENTRY_TYPEMASK = 15;
		public const int BIOS_INDEX = 24;
		public const int BIOS_FIRST = 1;
		public const string BIOS_LUA_CODE = "bios";
		public const string VIEW_LUA_CODE = "manager.machine.video.snapshot_target.view_names[]";

		// main launcher
		[BizImport(cc, Compatibility = true)]
		public abstract uint mame_launch(int argc, string[] argv);

		[BizImport(cc)]
		public abstract bool mame_coswitch();

		[BizImport(cc)]
		public abstract byte mame_read_byte(uint address);

		[BizImport(cc)]
		public abstract IntPtr mame_input_get_field_ptr(string tag, string field);

		[BizImport(cc)]
		public abstract void mame_input_set_fields(IntPtr[] fields, int[] inputs, int length);

		[BizImport(cc)]
		public abstract int mame_sound_get_samples(short[] buffer);

		[BizImport(cc)]
		public abstract void mame_video_get_dimensions(out int width, out int height);

		[BizImport(cc)]
		public abstract void mame_video_get_pixels(int[] buffer);

		[UnmanagedFunctionPointer(cc)]
		public delegate void FilenameCallbackDelegate(string name);

		[BizImport(cc)]
		public abstract void mame_nvram_get_filenames(FilenameCallbackDelegate cb);

		[BizImport(cc)]
		public abstract void mame_nvram_save();

		[BizImport(cc)]
		public abstract void mame_nvram_load();

		// info
		[UnmanagedFunctionPointer(cc)]
		public delegate void InfoCallbackDelegate(string info);

		[BizImport(cc)]
		public abstract void mame_info_get_warnings_string(InfoCallbackDelegate cb);

		// log
		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCallbackDelegate(OutputChannel channel, int size, string data);

		[BizImport(cc)]
		public abstract void mame_set_log_callback(LogCallbackDelegate cb);

		// base time
		[UnmanagedFunctionPointer(cc)]
		public delegate long BaseTimeCallbackDelegate();

		[BizImport(cc)]
		public abstract void mame_set_base_time_callback(BaseTimeCallbackDelegate cb);

		// input poll
		[UnmanagedFunctionPointer(cc)]
		public delegate void InputPollCallbackDelegate();

		[BizImport(cc)]
		public abstract void mame_set_input_poll_callback(InputPollCallbackDelegate cb);

		// execute
		[BizImport(cc)]
		public abstract void mame_lua_execute(string code);

		// get bool
		[BizImport(cc)]
		public abstract bool mame_lua_get_bool(string code);

		// get int
		[BizImport(cc)]
		public abstract int mame_lua_get_int(string code);

		// get long
		[BizImport(cc)]
		public abstract long mame_lua_get_long(string code);

		// get double
		[BizImport(cc)]
		public abstract double mame_lua_get_double(string code);

		// get string
		[BizImport(cc)]
		public abstract IntPtr mame_lua_get_string(string code, out int length);

		// free string
		[BizImport(cc)]
		public abstract void mame_lua_free_string(IntPtr pointer);
	}
}
