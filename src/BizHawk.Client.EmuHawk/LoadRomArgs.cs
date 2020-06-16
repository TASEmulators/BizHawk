using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class LoadRomArgs
	{
		public bool? Deterministic { get; set; }
		public IOpenAdvanced OpenAdvanced { get; set; }
	}
}
