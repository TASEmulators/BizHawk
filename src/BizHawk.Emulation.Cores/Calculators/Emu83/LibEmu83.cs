using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public static class LibEmu83
	{
		private const string lib = "libemu83";
		private const CallingConvention cc = CallingConvention.Cdecl;

		[DllImport(lib, CallingConvention = cc)]
		public static extern IntPtr TI83_CreateContext(byte[] rom, int len);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_DestroyContext(IntPtr context);

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_LoadLinkFile(IntPtr context, byte[] linkfile, int len);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_SetLinkFilesAreLoaded(IntPtr context);

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_GetLinkActive(IntPtr context);

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_Advance(IntPtr context, bool onpress, bool sendnextlinkfile, int[] videobuf, uint bgcol, uint forecol);

		[DllImport(lib, CallingConvention = cc)]
		public static extern long TI83_GetStateSize();

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_SaveState(IntPtr context, byte[] buf);

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_LoadState(IntPtr context, byte[] buf);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_GetRegs(IntPtr context, int[] buf);

		public enum MemoryArea_t : int
		{
			MEM_ROM,
			MEM_RAM,
			MEM_VRAM,
		}

		[DllImport(lib, CallingConvention = cc)]
		public static extern bool TI83_GetMemoryArea(IntPtr context, MemoryArea_t which, ref IntPtr ptr, ref int len);

		[DllImport(lib, CallingConvention = cc)]
		public static extern byte TI83_ReadMemory(IntPtr context, ushort addr);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_WriteMemory(IntPtr context, ushort addr, byte val);

		[DllImport(lib, CallingConvention = cc)]
		public static extern long TI83_GetCycleCount(IntPtr context);

		public enum MemoryCallbackId_t : int
		{
			MEM_CB_READ,
			MEM_CB_WRITE,
			MEM_CB_EXECUTE,
		}

		[UnmanagedFunctionPointer(cc)]
		public delegate void MemoryCallback(ushort addr, long _cycleCount);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_SetMemoryCallback(IntPtr context, MemoryCallbackId_t id, MemoryCallback callback);

		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(long _cycleCount);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_SetTraceCallback(IntPtr context, TraceCallback callback);

		[UnmanagedFunctionPointer(cc)]
		public delegate byte InputCallback(byte _keyboardMask);

		[DllImport(lib, CallingConvention = cc)]
		public static extern void TI83_SetInputCallback(IntPtr context, InputCallback callback);
	}
}
