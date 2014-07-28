using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	public class LibSnes9x
	{
		const string DllName = "libbizsnes.dll";
		const CallingConvention CC = CallingConvention.Cdecl;

		[DllImport(DllName, CallingConvention = CC)]
		public static extern bool debug_init(byte[] data, int length);

		[DllImport(DllName, CallingConvention = CC)]
		public static extern void debug_advance(int[] data);
	}
}
