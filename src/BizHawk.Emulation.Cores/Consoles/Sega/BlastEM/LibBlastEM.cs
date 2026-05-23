using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Sega.BlastEm
{
	public abstract class LibBlastEm : LibWaterboxCore
	{
		public enum Region : int
		{
			Auto = 0,
			JapanNTSC = 1,
			JapanPAL = 2,
			US = 4,
			Europe = 8,
		}

		[BizImport(CC)]
		public abstract bool Init(bool cd, bool _32xPreinit, Region regionAutoOrder, Region regionOverride);

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public int Buttons;
		}
	}
}
