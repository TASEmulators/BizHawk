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
		public static extern IntPtr BizCreate(byte[] bios, byte[] data, int length, [In]OverrideInfo dbinfo, bool skipBios);

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizReset(IntPtr ctx);

		public enum SaveType : int
		{
			Autodetect = -1,
			ForceNone = 0,
			Sram = 1,
			Flash512 = 2,
			Flash1m = 3,
			Eeprom = 4
		}

		[Flags]
		public enum Hardware : int
		{
			None = 0,
			Rtc = 1,
			Rumble = 2,
			LightSensor = 4,
			Gyro = 8,
			Tilt = 16,
			GbPlayer = 32,
			GbPlayerDetect = 64,
			NoOverride = 0x8000 // can probably ignore this
		}

		[StructLayout(LayoutKind.Sequential)]
		public class OverrideInfo
		{
			public SaveType Savetype;
			public Hardware Hardware;
			public uint IdleLoop = IDLE_LOOP_NONE;
			public const uint IDLE_LOOP_NONE = unchecked((uint)0xffffffff);
		}

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
			public IntPtr sram;
			public int sram_size;
		}

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizGetMemoryAreas(IntPtr ctx, [Out]MemoryAreas dst);

		[DllImport(dll, CallingConvention = cc)]
		public static extern int BizGetSaveRam(IntPtr ctx, byte[] dest, int maxsize);
		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizPutSaveRam(IntPtr ctx, byte[] src, int size);

		/// <summary>
		/// start a savestate operation
		/// </summary>
		/// <param name="ctx"></param>
		/// <param name="p">private parameter to be passed to BizFinishGetState</param>
		/// <param name="size">size of buffer to be allocated for BizFinishGetState</param>
		/// <returns>if false, operation failed and BizFinishGetState should not be called</returns>
		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizStartGetState(IntPtr ctx, ref IntPtr p, ref int size);

		/// <summary>
		/// finish a savestate operation.  if StartGetState returned true, this must be called else memory leaks
		/// </summary>
		/// <param name="p">returned by BizStartGetState</param>
		/// <param name="dest">buffer of length size</param>
		/// <param name="size">returned by BizStartGetState</param>
		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizFinishGetState(IntPtr p, byte[] dest, int size);

		[DllImport(dll, CallingConvention = cc)]
		public static extern bool BizPutState(IntPtr ctx, byte[] src, int size);

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

		[Flags]
		public enum Sounds : int
		{
			CH0 = 1,
			CH1 = 2,
			CH2 = 4,
			CH3 = 8,
			CHA = 16,
			CHB = 32
		}

		[DllImport(dll, CallingConvention = cc)]
		public static extern void BizSetSoundMask(IntPtr ctx, Sounds mask);
	}
}
