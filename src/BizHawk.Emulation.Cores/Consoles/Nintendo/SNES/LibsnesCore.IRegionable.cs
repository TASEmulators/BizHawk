using BizHawk.API.ApiHawk;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IRegionable
	{
		public DisplayType Region => Api.Region == LibsnesApi.SNES_REGION.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
