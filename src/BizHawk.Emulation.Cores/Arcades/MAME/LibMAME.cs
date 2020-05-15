using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public static class LibMAME
	{
		const string dll = "libmamearcade64.dll"; // libmamearcade64.dll libpacmansh64d.dll
		const CallingConvention cc = CallingConvention.Cdecl;

		public enum OutputChannel
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		}

		public enum SaveError
		{
			NONE, NOT_FOUND, ILLEGAL_REGISTRATIONS, INVALID_HEADER, READ_ERROR, WRITE_ERROR, DISABLED
		};

		#region C API

		// main launcher
		[DllImport(dll, CallingConvention = cc)]
		public static extern uint mame_launch(int argc, string[] argv);

		[DllImport(dll, CallingConvention = cc)]
		public static extern SaveError mame_save_buffer(byte[] buf, out int length);

		[DllImport(dll, CallingConvention = cc)]
		public static extern SaveError mame_load_buffer(byte[] buf, int length);

		#endregion

		#region Lua API

		// execute
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_lua_execute(string code);

		// get int
		[DllImport(dll, CallingConvention = cc)]
		public static extern int mame_lua_get_int(string code);

		// get double
		[DllImport(dll, CallingConvention = cc)]
		public static extern double mame_lua_get_double(string code);

		// get bool
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_get_bool(string code);

		// get string
		[DllImport(dll, CallingConvention = cc)]
		public static extern IntPtr mame_lua_get_string(string code, out int length);

		// free string
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_free_string(IntPtr pointer);

		#endregion

		#region Callbacks

		// periodic
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void PeriodicCallbackDelegate();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_periodic_callback(PeriodicCallbackDelegate cb);

		// sound
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SoundCallbackDelegate();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_sound_callback(SoundCallbackDelegate cb);

		// boot
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BootCallbackDelegate();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_boot_callback(BootCallbackDelegate cb);

		// log
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LogCallbackDelegate(OutputChannel channel, int size, string data);
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_log_callback(LogCallbackDelegate cb);

		#endregion
	}
}
