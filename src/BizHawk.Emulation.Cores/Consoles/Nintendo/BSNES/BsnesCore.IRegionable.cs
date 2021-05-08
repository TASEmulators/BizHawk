using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore : IRegionable
	{
		public DisplayType Region => _region == BsnesApi.SNES_REGION.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
