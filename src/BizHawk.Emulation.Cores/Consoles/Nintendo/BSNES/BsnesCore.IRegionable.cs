using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : IRegionable
	{
		private readonly BsnesApi.SNES_REGION _region;

		public DisplayType Region => _region == BsnesApi.SNES_REGION.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
