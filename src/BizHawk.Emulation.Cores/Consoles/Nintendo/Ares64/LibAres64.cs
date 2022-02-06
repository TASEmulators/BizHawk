using System;
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
		}

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			//public Buttons Keys;
		}

		[BizImport(CC)]
		public abstract bool Init(bool pal);
	}
}
