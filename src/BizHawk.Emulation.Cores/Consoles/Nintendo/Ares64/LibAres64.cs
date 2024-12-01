using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	public abstract class LibAres64 : LibWaterboxCore
	{
		[Flags]
		public enum Buttons : uint
		{
			UP      = 1 <<  0,
			DOWN    = 1 <<  1,
			LEFT    = 1 <<  2,
			RIGHT   = 1 <<  3,
			B       = 1 <<  4,
			A       = 1 <<  5,
			C_UP    = 1 <<  6,
			C_DOWN  = 1 <<  7,
			C_LEFT  = 1 <<  8,
			C_RIGHT = 1 <<  9,
			L       = 1 << 10,
			R       = 1 << 11,
			Z       = 1 << 12,
			START   = 1 << 13,
		}

		public enum ControllerType : uint
		{
			Unplugged,
			Standard,
			Mempak,
			Rumblepak,
			Transferpak,
			Mouse,
		}

		public enum DeinterlacerType : uint
		{
			Weave,
			Bob,
		}

		public enum CpuType : uint
		{
			Interpreter,
			Recompiler,
		}

		public enum IplVer : uint
		{
			Japan,
			Dev,
			USA,
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public long Time;

			public Buttons P1Buttons;
			public Buttons P2Buttons;
			public Buttons P3Buttons;
			public Buttons P4Buttons;

			public short P1XAxis;
			public short P1YAxis;

			public short P2XAxis;
			public short P2YAxis;

			public short P3XAxis;
			public short P3YAxis;

			public short P4XAxis;
			public short P4YAxis;

			public bool Reset;
			public bool Power;

			public bool BobDeinterlacer;
			public bool FastVI;
			public bool SkipDraw;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LoadData
		{
			public IntPtr PifData;
			public long PifLen;
			public IntPtr IplData;
			public long IplLen;
			public IntPtr RomData;
			public long RomLen;
			public IntPtr DiskData;
			public long DiskLen;
			public IntPtr DiskErrorData;
			public long DiskErrorLen;
			public IntPtr Gb1RomData;
			public long Gb1RomLen;
			public IntPtr Gb2RomData;
			public long Gb2RomLen;
			public IntPtr Gb3RomData;
			public long Gb3RomLen;
			public IntPtr Gb4RomData;
			public long Gb4RomLen;
		}

		[BizImport(CC)]
		public abstract bool Init(ref LoadData loadData, ControllerType[] controllerSettings, bool isPal, long initTime);

		[BizImport(CC)]
		public abstract bool GetRumbleStatus(int num);
		
		[BizImport(CC)]
		public abstract void PostLoadState();

		[BizImport(CC)]
		public abstract void GetDisassembly(uint address, uint instruction, byte[] buf);

		[BizImport(CC)]
		public abstract void GetRegisters(ulong[] buf);
	}
}
