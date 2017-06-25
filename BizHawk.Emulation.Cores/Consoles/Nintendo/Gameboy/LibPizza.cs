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
	public abstract class LibPizza : LibWaterboxCore, ICustomSaveram
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
			public Buttons Keys;
		}
		[BizImport(CC)]
		public abstract bool Init(byte[] rom, int romlen, bool sgb, byte[] spc, int spclen);
		[BizImport(CC)]
		public abstract bool IsCGB();
		[BizImport(CC)]
		public abstract int GetSaveramSize();
		[BizImport(CC)]
		public abstract void PutSaveram(byte[] data, int size);
		[BizImport(CC)]
		public abstract void GetSaveram(byte[] data, int size);
	}
}
