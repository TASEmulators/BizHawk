using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Sony.PS2
{
	public abstract class LibDobieStation : LibWaterboxCore
	{
		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
		}

		public unsafe delegate void CdCallback(ulong sector, byte* dest);

		[BizImport(CC)]
		public abstract bool Initialize(byte[] bios, ulong cdLength, CdCallback cdCallback);
	}
}
