using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	public abstract class LibSaturnus : LibNymaCore
	{
		[BizImport(CC)]
		public abstract int GetSaveRamLength();
		[BizImport(CC)]
		public abstract void GetSaveRam(byte[] data);
		[BizImport(CC)]
		public abstract void PutSaveRam(byte[] data, int length);
	}
}
