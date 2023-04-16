using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : IRegionable
	{
		private LibretroApi.RETRO_REGION _region = LibretroApi.RETRO_REGION.NTSC;

		public DisplayType Region
		{
			get
			{
				return _region switch
				{
					LibretroApi.RETRO_REGION.NTSC => DisplayType.NTSC,
					LibretroApi.RETRO_REGION.PAL => DisplayType.PAL,
					_ => DisplayType.NTSC,
				};
			}
		}
	}
}
