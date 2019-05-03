using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
    public static class LibMAME
    {
        const string dll = "mame.dll";
        const CallingConvention cc = CallingConvention.Cdecl;

		[DllImport(dll, CallingConvention = cc)]
		public static extern UInt32 mame_number();
	}
}
