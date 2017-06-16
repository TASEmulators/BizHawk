using BizHawk.Common.BizInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	public abstract class LibPizza
	{
		private const CallingConvention CC = CallingConvention.Cdecl;
		[Flags]
		public enum Buttons : ushort
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
		public class FrameInfo
		{
			public IntPtr VideoBuffer;
			public IntPtr SoundBuffer;
			public int Clocks;
			public int Samples;
			public Buttons Keys;
		}

		[BizImport(CC)]
		public abstract bool Init(byte[] rom, int romlen);
		[BizImport(CC)]
		public abstract void FrameAdvance([In,Out] FrameInfo frame);
		[BizImport(CC)]
		public abstract bool IsCGB();
	}
}
