using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Amiga
{
	public partial class UAE : IRegionable
	{
		public DisplayType Region => _syncSettings.Region is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}
