using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// Constant class for the names of cores, that should be used in every <see cref="CoreAttribute"/><br/>
	/// For now we are only including ones that can be picked as a core preference, but all cores should be included here
	/// </summary>
	public static class CoreNames
	{
		public const string NesHawk = "NesHawk";
		public const string SubNesHawk = "SubNESHawk";
		public const string QuickNes = "QuickNes";
		public const string Snes9X = "Snes9x";
		public const string Bsnes = "BSNES";
		public const string Mgba = "mGBA";
		public const string GbHawk = "GBHawk";
		public const string Gambatte = "Gambatte";
		public const string SubGbHawk = "SubGBHawk";
		public const string SameBoy = "SameBoy";
		public const string PicoDrive = "PicoDrive";
		public const string Gpgx = "Genplus-gx";
		public const string PceHawk = "PCEHawk";
		public const string TurboNyma = "TurboNyma";
		public const string TurboTurboNyma = "TurboTurboNyma";
		public const string Faust = "Faust";
	}
}
