using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BizHawk.Emulation.Cores.Waterbox
{
	partial class NymaCore
	{
		/// <summary>
		/// Override button names.  Technically this should be per core, but a lot of the names and overrides are the same,
		/// and an override that doesn't apply to a particular core will just be ignored
		/// </summary>
		private string OverrideButtonName(string original)
		{
			original = Regex.Replace(original, @"\s*(↑|↓|←|→)\s*", "");
			original = Regex.Replace(original, @"\s*\([^\)]+\)\s*", "");
			original = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(original.ToLowerInvariant());
			// TODO: Add dictionaries or whatever here as needed
			return original;
		}
	}
}
