using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	internal static class Bk2AxisMnemonicConstants
	{
		public static string Lookup(string button, string systemId)
		{
			var key = button
				.Replace("P1 ", "")
				.Replace("P2 ", "")
				.Replace("P3 ", "")
				.Replace("P4 ", "")
				.Replace("Key ", "");

			if (SystemOverrides.ContainsKey(systemId) && SystemOverrides[systemId].ContainsKey(key))
			{
				return SystemOverrides[systemId][key];
			}

			if (BaseMnemonicLookupTable.ContainsKey(key))
			{
				return BaseMnemonicLookupTable[key];
			}

			return button;
		}

		private static readonly Dictionary<string, string> BaseMnemonicLookupTable = new Dictionary<string, string>
		{
			["Zapper X"] = "zapX",
			["Zapper Y"] = "zapY",
			["Paddle"] = "Pad",
			["Pen"] = "Pen",
			["Mouse X"] = "mX",
			["Mouse Y"] = "mY",
			["Lightgun X"] = "lX",
			["Lightgun Y"] = "lY",
			["X Axis"] = "aX",
			["Y Axis"] = "aY",
			["LStick X"] = "lsX",
			["LStick Y"] = "lsY",
			["RStick X"] = "rsX",
			["RStick Y"] = "rsY",
			["Disc Select"] = "Disc"
		};

		private static readonly Dictionary<string, Dictionary<string, string>> SystemOverrides = new Dictionary<string, Dictionary<string, string>>
		{
			["A78"] = new Dictionary<string, string>
			{
				["VPos"] = "X",
				["HPos"] = "Y"
			}
		};
	}
}
