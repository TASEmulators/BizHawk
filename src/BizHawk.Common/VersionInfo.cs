using System.IO;
using System.Reflection;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Common
{
	public static partial class VersionInfo
	{
		/// <remarks>
		/// Bump this immediately after release.
		/// Only use '0'..'9' and '.' or it will fail to parse and the new version notification won't work.
		/// </remarks>
		public static readonly string MainVersion = "2.6.0";

		public static readonly string ReleaseDate = "January 17, 2020";

		public static readonly string HomePage = "http://tasvideos.org/BizHawk.html";

		public static readonly bool DeveloperBuild = true;

		public static readonly string? CustomBuildString;

		static VersionInfo()
		{
			var path = Path.Combine(
				Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)?.RemoveSuffix(Path.DirectorySeparatorChar) ?? string.Empty,
				"dll",
				"custombuild.txt"
			);
			if (File.Exists(path))
			{
				var lines = File.ReadAllLines(path);
				if (lines.Length > 0)
				{
					CustomBuildString = lines[0];
				}
			}
		}

		public static string GetEmuVersion()
			=> DeveloperBuild ? $"GIT {GIT_BRANCH}#{GIT_SHORTHASH}" : $"Version {MainVersion}";

		/// <summary>"2.5.1" => 0x02050100</summary>
		public static uint VersionStrToInt(string s)
		{
			var a = s.Split('.');
			var v = 0U;
			var i = 0;
			while (i < 4)
			{
				v <<= 8;
				v += i < a.Length && byte.TryParse(a[i], out var b) ? b : 0U;
				i++;
			}
			return v;
		}
	}
}
