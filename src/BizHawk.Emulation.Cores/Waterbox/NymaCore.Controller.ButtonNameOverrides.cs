using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BizHawk.Emulation.Cores.Waterbox
{
	partial class NymaCore
	{
		private static bool IsRomanNumeral(string str)
			=> new[] {"I", "II", "III", "IV", "V", "VI"}.Contains(str);

		private static readonly Dictionary<string, string> ButtonNameOverrides = new Dictionary<string, string>
		{
			["Left Shoulder"] = "L",
			["Right Shoulder"] = "R",
			["Left-Back"] = "L",
			["Right-Back"] = "R",

			// VB specific hack
			// needed like this as otherwise left and right dpads have the same
			["UP ↑ (Left D-Pad)"] = "L_Up",
			["DOWN ↓ (Left D-Pad)"] = "L_Down",
			["LEFT ← (Left D-Pad)"] = "L_Left",
			["RIGHT → (Left D-Pad)"] = "L_Right",
			["UP ↑ (Right D-Pad)"] = "R_Up",
			["DOWN ↓ (Right D-Pad)"] = "R_Down",
			["LEFT ← (Right D-Pad)"] = "R_Left",
			["RIGHT → (Right D-Pad)"] = "R_Right",
		};

		/// <summary>
		/// Override button names.  Technically this should be per core, but a lot of the names and overrides are the same,
		/// and an override that doesn't apply to a particular core will just be ignored
		/// </summary>
		private string OverrideButtonName(string original)
		{
			// VB hack
			if (ButtonNameOverrides.TryGetValue(original, out string vbOverrideName))
			{
				original = vbOverrideName;
			}

			original = Regex.Replace(original, @"\s*(↑|↓|←|→)\s*", "");
			original = Regex.Replace(original, @"\s*\([^\)]+\)\s*", "");
			if (!IsRomanNumeral(original))
			{
				original = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(original.ToLowerInvariant());
			}

			if (ButtonNameOverrides.TryGetValue(original, out string overrideName))
			{
				original = overrideName;
			}

			// TODO: Add dictionaries or whatever here as needed
			return original;
		}
	}
}
