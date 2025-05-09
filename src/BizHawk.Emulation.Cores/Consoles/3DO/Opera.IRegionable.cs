using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Panasonic3DO
{
	public partial class Opera : IRegionable
	{
		public DisplayType Region => _syncSettings.VideoStandard is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
