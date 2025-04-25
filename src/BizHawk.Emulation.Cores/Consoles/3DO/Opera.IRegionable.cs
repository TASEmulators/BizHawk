using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles.Panasonic3DO
{
	public partial class Opera : IRegionable
	{
		public DisplayType Region => _syncSettings.VideoStandard is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
