using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IRegionable
	{
		public DisplayType Region
		{
			get
			{
				if (api.Region == LibsnesApi.SNES_REGION.NTSC)
				{
					return DisplayType.NTSC;
				}
				
				return DisplayType.PAL;
			}
		}
	}
}
