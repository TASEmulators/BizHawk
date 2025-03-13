using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Consoles._3DO
{
	public partial class Opera : IRegionable
	{
		public DisplayType Region => _syncSettings.VideoStandard is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
