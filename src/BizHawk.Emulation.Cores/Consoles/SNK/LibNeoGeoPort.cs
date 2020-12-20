using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	public abstract class LibNeoGeoPort : LibNymaCore
	{
		[BizImport(CC)]
		public abstract bool GetSaveRam();
		[BizImport(CC)]
		public abstract bool PutSaveRam();
	}
}
