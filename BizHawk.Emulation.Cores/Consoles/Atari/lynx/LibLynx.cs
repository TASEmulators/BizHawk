using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Atari.Lynx
{
	public static class LibLynx
	{
		const string dllname = "bizlynx.dll";
		const CallingConvention cc = CallingConvention.Cdecl;

		[DllImport(dllname, CallingConvention = cc)]
		public static extern IntPtr Create(byte[] game, int gamesize, byte[] bios, int biossize, int pagesize0, int pagesize1, bool lowpass);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Destroy(IntPtr s);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Reset(IntPtr s);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Advance(IntPtr s, int buttons, int[] vbuff, short[] sbuff, ref int sbuffsize);
	}
}
