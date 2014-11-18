using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
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
		public static extern bool Advance(IntPtr s, Buttons buttons, int[] vbuff, short[] sbuff, ref int sbuffsize);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetRotation(IntPtr s, int value);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool GetSaveRamPtr(IntPtr s, out int size, out IntPtr data);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void GetReadOnlyCartPtrs(IntPtr s, out int s0, out IntPtr p0, out int s1, out IntPtr p1);


		[DllImport(dllname, CallingConvention = cc)]
		public static extern int BinStateSize(IntPtr s);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool BinStateSave(IntPtr s, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool BinStateLoad(IntPtr s, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void TxtStateSave(IntPtr s, [In]ref TextStateFPtrs ff);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void TxtStateLoad(IntPtr s, [In]ref TextStateFPtrs ff);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern IntPtr GetRamPointer(IntPtr s);

		[Flags]
		public enum Buttons : ushort
		{
			Up = 0x0040,
			Down = 0x0080,
			Left = 0x0010,
			Right = 0x0020,
			Option_1 = 0x008,
			Option_2 = 0x004,
			B = 0x002,
			A = 0x001,
			Pause = 0x100,
		}
	}
}
