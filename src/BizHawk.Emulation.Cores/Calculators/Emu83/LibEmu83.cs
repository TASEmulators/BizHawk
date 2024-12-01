using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Calculators.Emu83
{
	public abstract class LibEmu83
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[BizImport(cc)]
		public abstract IntPtr TI83_CreateContext(byte[] rom, int len);

		[BizImport(cc)]
		public abstract void TI83_DestroyContext(IntPtr context);

		[BizImport(cc)]
		public abstract bool TI83_LoadLinkFile(IntPtr context, byte[] linkfile, int len);

		[BizImport(cc)]
		public abstract void TI83_SetLinkFilesAreLoaded(IntPtr context);

		[BizImport(cc)]
		public abstract bool TI83_GetLinkActive(IntPtr context);

		[BizImport(cc)]
		public abstract bool TI83_Advance(IntPtr context, bool onpress, bool sendnextlinkfile, int[] videobuf, uint bgcol, uint forecol);

		[BizImport(cc)]
		public abstract long TI83_GetStateSize();

		[BizImport(cc)]
		public abstract bool TI83_SaveState(IntPtr context, byte[] buf);

		[BizImport(cc)]
		public abstract bool TI83_LoadState(IntPtr context, byte[] buf);

		[BizImport(cc)]
		public abstract void TI83_GetRegs(IntPtr context, int[] buf);

		public enum MemoryArea_t : int
		{
			MEM_ROM,
			MEM_RAM,
			MEM_VRAM,
		}

		[BizImport(cc)]
		public abstract bool TI83_GetMemoryArea(IntPtr context, MemoryArea_t which, ref IntPtr ptr, ref int len);

		[BizImport(cc)]
		public abstract byte TI83_ReadMemory(IntPtr context, ushort addr);

		[BizImport(cc)]
		public abstract void TI83_WriteMemory(IntPtr context, ushort addr, byte val);

		[BizImport(cc)]
		public abstract long TI83_GetCycleCount(IntPtr context);

		public enum MemoryCallbackId_t : int
		{
			MEM_CB_READ,
			MEM_CB_WRITE,
			MEM_CB_EXECUTE,
		}

		[UnmanagedFunctionPointer(cc)]
		public delegate void MemoryCallback(ushort addr, long _cycleCount);

		[BizImport(cc)]
		public abstract void TI83_SetMemoryCallback(IntPtr context, MemoryCallbackId_t id, MemoryCallback callback);

		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(long _cycleCount);

		[BizImport(cc)]
		public abstract void TI83_SetTraceCallback(IntPtr context, TraceCallback callback);

		[UnmanagedFunctionPointer(cc)]
		public delegate byte InputCallback(byte _keyboardMask);

		[BizImport(cc)]
		public abstract void TI83_SetInputCallback(IntPtr context, InputCallback callback);
	}
}
