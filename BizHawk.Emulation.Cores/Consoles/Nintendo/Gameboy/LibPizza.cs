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
		[StructLayout(LayoutKind.Sequential)]
		public class FrameInfo
		{
			public IntPtr VideoBuffer;
			public int Clocks;
		}

		[BizImport(CC)]
		public abstract bool Init(byte[] rom, int romlen);
		[BizImport(CC)]
		public abstract void FrameAdvance([In,Out] FrameInfo frame);
	}
}
