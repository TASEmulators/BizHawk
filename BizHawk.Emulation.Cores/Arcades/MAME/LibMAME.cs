using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
    public static class LibMAME
    {
        const string dll = "libpacmansh64d.dll";
        const CallingConvention cc = CallingConvention.Cdecl;


		public enum osd_output_channel
		{
			OSD_OUTPUT_CHANNEL_ERROR,
			OSD_OUTPUT_CHANNEL_WARNING,
			OSD_OUTPUT_CHANNEL_INFO,
			OSD_OUTPUT_CHANNEL_DEBUG,
			OSD_OUTPUT_CHANNEL_VERBOSE,
			OSD_OUTPUT_CHANNEL_LOG,
			OSD_OUTPUT_CHANNEL_COUNT
		};


		[DllImport(dll, CallingConvention = cc)]
		public static extern UInt32 mame_launch(int argc, string[] argv);


		// LUA API

		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_lua_execute(string code);

		[DllImport(dll, CallingConvention = cc)]
		public static extern int mame_lua_get_int(string code);

		[DllImport(dll, CallingConvention = cc)]
		public static extern double mame_lua_get_double(string code);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_get_bool(string code);

		[DllImport(dll, CallingConvention = cc)]
		public static extern IntPtr mame_lua_get_string(string code, out int length);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_free_string(IntPtr pointer);


		// CALLBACKS

		// boot
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BootCallback();

		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_boot_callback(BootCallback cb);

		// log
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LogCallback(osd_output_channel channel, int size, string data);

		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_log_callback(LogCallback cb);
	}
}
