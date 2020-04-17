using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// Constant class for the names of cores, that should be used  in every <see cref="CoreAttribute"/>
	/// For now we are only including ones that can be picked as a core preference,
	/// but all cores should be included ere
	/// </summary>
	public class CoreNames
	{
		public const string NesHawk = "NesHawk";
		public const string SubNesHawk = "SubNESHawk";
		public const string QuickNes = "QuickNes";
		public const string Snes9X = "Snes9x";
		public const string Bsnes = "BSNES";
		public const string Mgba = "mGBA";
		public const string VbaNext = "VBA-Next";
		public const string GbHawk = "GBHawk";
		public const string Gambatte = "Gambatte";
		public const string SubGbHawk = "SubGBHawk";
	}
}
