using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	public abstract class LibSameboy : LibWaterboxCore
	{
		[Flags]
		public enum Buttons : uint
		{
			A = 0x01,
			B = 0x02,
			SELECT = 0x04,
			START = 0x08,
			RIGHT = 0x10,
			LEFT = 0x20,
			UP = 0x40,
			DOWN = 0x80
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public long Time;
			public Buttons Keys;
		}

		[UnmanagedFunctionPointer(CC)]
		public delegate void PrinterCallback(IntPtr image,
										  byte height,
										  byte top_margin,
										  byte bottom_margin,
										  byte exposure);

		[BizImport(CC)]
		public abstract bool Init(bool cgb, byte[] spc, int spclen);

		[BizImport(CC)]
		public abstract void GetGpuMemory(IntPtr[] ptrs);

		[BizImport(CC)]
		public abstract void SetScanlineCallback(ScanlineCallback callback, int ly);

		[BizImport(CC)]
		public abstract byte GetIoReg(byte port);

		[BizImport(CC)]
		public abstract void PutSaveRam();

		[BizImport(CC)]
		public abstract void GetSaveRam();

		[BizImport(CC)]
		public abstract bool HasSaveRam();

		[BizImport(CC)]
		public abstract void SetPrinterCallback(PrinterCallback callback);
	}
}
