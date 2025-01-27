using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IRegionable
	{
		public DisplayType Region => _syncSettings.Region is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
