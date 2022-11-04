using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public abstract class LibMAME
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		// enums
		public enum OutputChannel
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		}

		public enum SaveError
		{
			NONE, NOT_FOUND, ILLEGAL_REGISTRATIONS, INVALID_HEADER, READ_ERROR, WRITE_ERROR, DISABLED
		}

		// constants
		public const int ROMENTRYTYPE_SYSTEM_BIOS = 9;
		public const int ROMENTRYTYPE_DEFAULT_BIOS = 10;
		public const int ROMENTRY_TYPEMASK = 15;
		public const int BIOS_INDEX = 24;
		public const int BIOS_FIRST = 1;
		public const string BIOS_LUA_CODE = "bios";

		// main launcher
		[BizImport(cc, Compatibility = true)]
		public abstract uint mame_launch(int argc, string[] argv);

		[BizImport(cc)]
		public abstract void mame_coswitch();

		[BizImport(cc)]
		public abstract char mame_read_byte(uint address);

		// execute
		[BizImport(cc)]
		public abstract void mame_lua_execute(string code);

		// get int
		[BizImport(cc)]
		public abstract int mame_lua_get_int(string code);

		// get double (internally cast to long)
		[BizImport(cc)]
		public abstract long mame_lua_get_double(string code);

		// get bool
		[BizImport(cc)]
		public abstract bool mame_lua_get_bool(string code);

		// get string
		[BizImport(cc)]
		public abstract IntPtr mame_lua_get_string(string code, out int length);

		// free string
		[BizImport(cc)]
		public abstract bool mame_lua_free_string(IntPtr pointer);

		// sound
		[UnmanagedFunctionPointer(cc)]
		public delegate void SoundCallbackDelegate();

		[BizImport(cc)]
		public abstract void mame_set_sound_callback(SoundCallbackDelegate cb);

		// log
		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCallbackDelegate(OutputChannel channel, int size, string data);

		[BizImport(cc)]
		public abstract void mame_set_log_callback(LogCallbackDelegate cb);
	}
}
