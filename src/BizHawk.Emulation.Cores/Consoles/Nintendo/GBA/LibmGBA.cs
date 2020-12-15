using System;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.GBA
{
	public abstract class LibmGBA
	{
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

		public static Buttons GetButtons(IController c)
		{
			Buttons ret = 0;
			foreach (string s in Enum.GetNames(typeof(Buttons)))
			{
				if (c.IsPressed(s))
				{
					ret |= (Buttons)Enum.Parse(typeof(Buttons), s);
				}
			}
			return ret;
		}

		private const CallingConvention cc = CallingConvention.Cdecl;

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

		[Flags]
		public enum Layers : int
		{
			BG0 = 1,
			BG1 = 2,
			BG2 = 4,
			BG3 = 8,
			OBJ = 16
		}

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

		public enum mWatchpointType
		{
			WATCHPOINT_WRITE = 1,
			WATCHPOINT_READ = 2,
			WATCHPOINT_RW = 3,
			WATCHPOINT_WRITE_CHANGE = 4,
		}

		[StructLayout(LayoutKind.Sequential)]
		public class OverrideInfo
		{
			public SaveType Savetype;
			public Hardware Hardware;
			public uint IdleLoop = IDLE_LOOP_NONE;
			public const uint IDLE_LOOP_NONE = unchecked((uint)0xffffffff);
		}

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
		}

		[BizImport(cc, Compatibility = true)]
		public abstract void BizDestroy(IntPtr ctx);

		[BizImport(cc, Compatibility = true)]
		public abstract IntPtr BizCreate(byte[] bios, byte[] data, int length, [In]OverrideInfo dbinfo, bool skipBios);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizReset(IntPtr ctx);

		[BizImport(cc, Compatibility = true)]
		public abstract bool BizAdvance(IntPtr ctx, Buttons keys, int[] vbuff, ref int nsamp, short[] sbuff,
			long time, short gyrox, short gyroy, short gyroz, byte luma);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetPalette(IntPtr ctx, int[] palette);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizGetMemoryAreas(IntPtr ctx, [Out]MemoryAreas dst);

		[BizImport(cc, Compatibility = true)]
		public abstract int BizGetSaveRam(IntPtr ctx, byte[] dest, int maxsize);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizPutSaveRam(IntPtr ctx, byte[] src, int size);

		/// <summary>
		/// start a savestate operation
		/// </summary>
		/// <param name="p">private parameter to be passed to BizFinishGetState</param>
		/// <param name="size">size of buffer to be allocated for BizFinishGetState</param>
		/// <returns>if false, operation failed and BizFinishGetState should not be called</returns>
		[BizImport(cc, Compatibility = true)]
		public abstract bool BizStartGetState(IntPtr ctx, ref IntPtr p, ref int size);

		/// <summary>
		/// finish a savestate operation.  if StartGetState returned true, this must be called else memory leaks
		/// </summary>
		/// <param name="p">returned by BizStartGetState</param>
		/// <param name="dest">buffer of length size</param>
		/// <param name="size">returned by BizStartGetState</param>
		[BizImport(cc, Compatibility = true)]
		public abstract void BizFinishGetState(IntPtr p, byte[] dest, int size);

		[BizImport(cc, Compatibility = true)]
		public abstract bool BizPutState(IntPtr ctx, byte[] src, int size);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetLayerMask(IntPtr ctx, Layers mask);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetSoundMask(IntPtr ctx, Sounds mask);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizGetRegisters(IntPtr ctx, int[] dest);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetRegister(IntPtr ctx, int index, int value);

		[BizImport(cc, Compatibility = true)]
		public abstract ulong BizGetGlobalTime(IntPtr ctx);

		[BizImport(cc, Compatibility = true)]
		public abstract void BizWriteBus(IntPtr ctx, uint addr, byte val);

		[BizImport(cc, Compatibility = true)]
		public abstract byte BizReadBus(IntPtr ctx, uint addr);

		[UnmanagedFunctionPointer(cc)]
		public delegate void TraceCallback(string msg);
		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetTraceCallback(IntPtr ctx, TraceCallback cb);

		[UnmanagedFunctionPointer(cc)]
		public delegate void MemCallback(uint addr, mWatchpointType type, uint oldValue, uint newValue);
		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetMemCallback(IntPtr ctx, MemCallback cb);

		[UnmanagedFunctionPointer(cc)]
		public delegate void ExecCallback(uint pc);
		[BizImport(cc, Compatibility = true)]
		public abstract void BizSetExecCallback(IntPtr ctx, ExecCallback cb);

		[BizImport(cc, Compatibility = true)]
		public abstract int BizSetWatchpoint(IntPtr ctx, uint addr, mWatchpointType type);

		[BizImport(cc, Compatibility = true)]
		public abstract bool BizClearWatchpoint(IntPtr ctx, int id);
	}
}
