using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public class LibVBANext
	{
		const string dllname = "libvbanext.dll";
		const CallingConvention cc = CallingConvention.Cdecl;

		[Flags]
		public enum Buttons : int
		{
			A = 1,
			B = 2,
			Select = 4,
			Start = 8,
			Right = 16,
			Left = 32,
			Up = 64,
			Down = 128,
			R = 256,
			L = 512
		}

		[StructLayout(LayoutKind.Sequential)]
		public class FrontEndSettings
		{
			public enum SaveType : int
			{
				auto = 0,
				eeprom = 1,
				sram = 2,
				flash = 3,
				eeprom_sensor = 4,
				none = 5
			}
			public enum FlashSize : int
			{
				small = 0x10000,
				big = 0x20000
			}
			public SaveType saveType;
			public FlashSize flashSize = FlashSize.big;
			public bool enableRtc;
			public bool mirroringEnable;
			public bool skipBios;

			public bool RTCUseRealTime = true;
			public int RTCyear; // 00..99
			public int RTCmonth; // 00..11
			public int RTCmday; // 01..31
			public int RTCwday; // 00..06
			public int RTChour; // 00..23
			public int RTCmin; // 00..59
			public int RTCsec; // 00..59
		}

		/// <summary>
		/// create a new context
		/// </summary>
		/// <returns></returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern IntPtr Create();

		/// <summary>
		/// destroy a context
		/// </summary>
		/// <param name="g"></param>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Destroy(IntPtr g);

		/// <summary>
		/// load a rom
		/// </summary>
		/// <param name="g"></param>
		/// <param name="romfile"></param>
		/// <param name="romfilelen"></param>
		/// <param name="biosfile"></param>
		/// <param name="biosfilelen"></param>
		/// <returns>success</returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool LoadRom(IntPtr g, byte[] romfile, uint romfilelen, byte[] biosfile, uint biosfilelen, [In]FrontEndSettings settings);

		/// <summary>
		/// hard reset
		/// </summary>
		/// <param name="g"></param>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void Reset(IntPtr g);

		/// <summary>
		/// frame advance
		/// </summary>
		/// <param name="g"></param>
		/// <param name="input"></param>
		/// <param name="videobuffer">240x160 packed argb32</param>
		/// <param name="audiobuffer">buffer to recieve stereo audio</param>
		/// <param name="numsamp">number of samples created</param>
		/// <param name="videopalette"></param>
		/// <returns>true if lagged</returns>
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool FrameAdvance(IntPtr g, Buttons input, int[] videobuffer, short[] audiobuffer, out int numsamp, int[] videopalette);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern int BinStateSize(IntPtr g);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool BinStateSave(IntPtr g, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool BinStateLoad(IntPtr g, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void TxtStateSave(IntPtr g, [In]ref TextStateFPtrs ff);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void TxtStateLoad(IntPtr g, [In]ref TextStateFPtrs ff);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern int SaveRamSize(IntPtr g);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool SaveRamSave(IntPtr g, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern bool SaveRamLoad(IntPtr g, byte[] data, int length);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void GetMemoryAreas(IntPtr g, [Out]MemoryAreas mem);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SystemBusWrite(IntPtr g, int addr, byte val);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern byte SystemBusRead(IntPtr g, int addr);

		[UnmanagedFunctionPointer(cc)]
		public delegate void StandardCallback();

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetScanlineCallback(IntPtr g, StandardCallback cb, int scanline);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern IntPtr GetRegisters(IntPtr g);

		[UnmanagedFunctionPointer(cc)]
		public delegate void AddressCallback(uint addr);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="addr">if bit 0 is set, thumb mode</param>
		/// <param name="opcode"></param>
		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(uint addr, uint opcode);

		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetPadCallback(IntPtr g, StandardCallback cb);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetFetchCallback(IntPtr g, AddressCallback cb);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetReadCallback(IntPtr g, AddressCallback cb);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetWriteCallback(IntPtr g, AddressCallback cb);
		[DllImport(dllname, CallingConvention = cc)]
		public static extern void SetTraceCallback(IntPtr g, TraceCallback cb);


		[StructLayout(LayoutKind.Sequential)]
		public class MemoryAreas
		{
			public IntPtr bios;
			public IntPtr iwram;
			public IntPtr ewram;
			public IntPtr palram;
			public IntPtr vram;
			public IntPtr oam;
			public IntPtr rom;
			public IntPtr mmio;
			public IntPtr sram;
			public int sram_size;
		}

		// this isn't used directly at the moment.  but it could be used for something eventually...
		[StructLayout(LayoutKind.Sequential)]
		public class Registers
		{
			public int R0;
			public int R1;
			public int R2;
			public int R3;
			public int R4;
			public int R5;
			public int R6;
			public int R7;
			public int R8;
			public int R9;
			public int R10;
			public int R11;
			public int R12;
			public int R13;
			public int R14;
			public int R15;
			public int CPSR;
			public int SPSR;
			public int R13_IRQ;
			public int R14_IRQ;
			public int SPSR_IRQ;
			public int _unk0; // what are these???
			public int _unk1;
			public int _unk2;
			public int _unk3;
			public int _unk4;
			public int R13_USR;
			public int R14_USR;
			public int R13_SVC;
			public int R14_SVC;
			public int SPSR_SVC;
			public int R13_ABT;
			public int R14_ABT;
			public int SPSR_ABT;
			public int R13_UND;
			public int R14_UND;
			public int SPSR_UND;
			public int R8_FIQ;
			public int R9_FIQ;
			public int R10_FIQ;
			public int R11_FIQ;
			public int R12_FIQ;
			public int R13_FIQ;
			public int R14_FIQ;
			public int SPSR_FIQ;
		}
	}
}
