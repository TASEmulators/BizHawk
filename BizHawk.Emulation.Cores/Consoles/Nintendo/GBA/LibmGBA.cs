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

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizDestroy(IntPtr ctx);

		[DllImport(dll, CallingConvention = cc)]
		public static extern IntPtr BizCreate(byte[] bios);

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizReset(IntPtr ctx);

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizSkipBios(IntPtr ctx);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizLoad(IntPtr ctx, byte[] data, int length);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizAdvance(IntPtr ctx, LibVBANext.Buttons keys, int[] vbuff, ref int nsamp, short[] sbuff,
			long time, short gyrox, short gyroy, short gyroz, byte luma);

		[StructLayout(LayoutKind.Sequential)]
		public class MemoryAreas
		{
			public IntPtr bios;
			public IntPtr wram;
			public IntPtr iwram;
			public IntPtr mmio;
			public IntPtr palram;
			public IntPtr vram;
			public IntPtr oam;
			public IntPtr rom;
		}

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizGetMemoryAreas(IntPtr ctx, [Out]MemoryAreas dst);

		[DllImport(dll, CallingConvention = cc)]
		public static extern int BizGetSaveRamSize(IntPtr ctx);
		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizGetSaveRam(IntPtr ctx, byte[] dest);
		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizPutSaveRam(IntPtr ctx, byte[] src);

		[DllImport(dll, CallingConvention = cc)]
		public static extern int BizGetStateSize();
		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizGetState(IntPtr ctx, byte[] dest);
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizPutState(IntPtr ctx, byte[] src);

		[Flags]
		public enum Layers : int
		{
			BG0 = 1,
			BG1 = 2,
			BG2 = 4,
			BG3 = 8,
			OBJ = 16
		}

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizSetLayerMask(IntPtr ctx, Layers mask);
	}
}
