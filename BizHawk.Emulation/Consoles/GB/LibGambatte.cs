using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.GB
{
	/// <summary>
	/// static bindings into libgambatte.dll
	/// </summary>
	public static class LibGambatte
	{
		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr gambatte_create();

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_destroy(IntPtr core);

		[Flags]
		public enum LoadFlags : uint
		{
			/// <summary>Treat the ROM as not having CGB support regardless of what its header advertises</summary>
			FORCE_DMG = 1,
			/// <summary>Use GBA intial CPU register values when in CGB mode.</summary>
			GBA_CGB = 2,
			/// <summary>Use heuristics to detect and support some multicart MBCs disguised as MBC1.</summary>
			MULTICART_COMPAT = 4 
		}

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_load(IntPtr core, string filename, LoadFlags flags);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern long gambatte_runfor(IntPtr core, uint[] videobuf, int pitch, short[] soundbuf, ref uint samples);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_reset(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setdmgpalettecolor(IntPtr core, uint palnum, uint colornum, uint rgb32);

		[Flags]
		public enum Buttons
		{ 
			A = 0x01,
			B = 0x02,
			SELECT = 0x04,
			START = 0x08,
			RIGHT = 0x10,
			LEFT = 0x20,
			UP = 0x40,
			DOWN = 0x80
		}

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setinputgetter(IntPtr core, Func<Buttons> getinput);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setsavedir(IntPtr core, string sdir);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_iscgb(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_isloaded(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_savesavedate(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_savestate(IntPtr core, uint[] videobuf, int pitch);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_loadstate(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_savestate_file(IntPtr core, uint[] videobuf, int pitch, string filepath);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_loadstate_file(IntPtr core, string filepath);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_selectstate(IntPtr core, int n);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int gambatte_currentstate(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern string gambatte_romtitle(IntPtr core);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setgamegenie(IntPtr core, string codes);

		[DllImport("libgambatte.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern void gambatte_setgameshark(IntPtr core, string codes);

	}
}
