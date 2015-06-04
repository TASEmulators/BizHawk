using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public static class LibmGBA
	{
		const string dll = "mgba.dll";
		const CallingConvention cc = CallingConvention.Cdecl;

		[DllImport(dll, CallingConvention=cc)]
		public static extern void BizDestroy(IntPtr ctx);

		[DllImport(dll, CallingConvention = cc)]
		public static extern IntPtr BizCreate();

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizReset(IntPtr ctx);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizLoad(IntPtr ctx, byte[] data, int length);

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizAdvance(IntPtr ctx, int keys, ref IntPtr vbuff);

	}
}
