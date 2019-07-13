using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
    public static class LibMAME
    {
        const string dll = "libpacmansh64d.dll";
        const CallingConvention cc = CallingConvention.Cdecl;

		public enum OutputChannel
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		};

		//-----------------------------------------------------------
		// main launcher
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern UInt32 mame_launch(int argc, string[] argv);


		//*********************************************************************
		// LUA API
		//*********************************************************************

		//-----------------------------------------------------------
		// execute
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_lua_execute(string code);

		//-----------------------------------------------------------
		// get int
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern int mame_lua_get_int(string code);

		//-----------------------------------------------------------
		// get double
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern double mame_lua_get_double(string code);

		//-----------------------------------------------------------
		// get bool
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_get_bool(string code);

		//-----------------------------------------------------------
		// get string
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern IntPtr mame_lua_get_string(string code, out int length);

		//-----------------------------------------------------------
		// free string
		//-----------------------------------------------------------
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool mame_lua_free_string(IntPtr pointer);
		

		//*********************************************************************
		// CALLBACKS
		//*********************************************************************

		//-----------------------------------------------------------
		// frame
		//-----------------------------------------------------------
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FrameCallback();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_frame_callback(BootCallback cb);

		//-----------------------------------------------------------
		// periodic
		//-----------------------------------------------------------
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void PeriodicCallback();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_periodic_callback(BootCallback cb);

		//-----------------------------------------------------------
		// boot
		//-----------------------------------------------------------
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BootCallback();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_boot_callback(BootCallback cb);

		//-----------------------------------------------------------
		// log
		//-----------------------------------------------------------
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LogCallback(OutputChannel channel, int size, string data);
		[DllImport(dll, CallingConvention = cc)]
		public static extern void mame_set_log_callback(LogCallback cb);
	}
}
