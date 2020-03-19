using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Consoles.Belogic
{
	public abstract class LibUzem : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public int ButtonsP1;
			public int ButtonsP2;
			public int ButtonsConsole;
		}

		[BizImport(CC)]
		public abstract bool Init();

		[BizImport(CC)]
		public abstract bool MouseEnabled();
	}
}
