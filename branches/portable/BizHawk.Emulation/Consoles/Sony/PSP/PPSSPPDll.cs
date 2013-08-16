using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Sony.PSP
{
	public static class PPSSPPDll
	{
		const CallingConvention cc = CallingConvention.StdCall;
		const string dd = "PPSSPPBizhawk.dll";

		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCB(char type, string message);

		[DllImport(dd, CallingConvention = cc)]
		public static extern bool init(string fn, LogCB logcallback);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void setvidbuff(IntPtr buff);

		[DllImport(dd, CallingConvention = cc)]
		public static extern void die();

		[DllImport(dd, CallingConvention = cc)]
		public static extern void advance();

		[DllImport(dd, CallingConvention = cc)]
		public static extern int mixsound(short[] buff, int nsamp);
	}
}
