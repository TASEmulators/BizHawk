using System.IO;
using System.Reflection;

namespace BizHawk.Common
{
	public static partial class VersionInfo
	{
		// keep this updated at every major release
		public const string MainVersion = "2.4.0"; // Use numbers only or the new version notification won't work
		public const string ReleaseDate = "January 18, 2020";
		public const string HomePage = "http://tasvideos.org/BizHawk.html";
		public static readonly bool DeveloperBuild = true;

		public static readonly string? CustomBuildString;

		public static string GetEmuVersion()
		{
			return DeveloperBuild
				? "GIT " + GIT_BRANCH + "#" + GIT_SHORTHASH
				: "Version " + MainVersion;
		}

		static VersionInfo()
		{
			string path = Path.Combine(GetExeDirectoryAbsolute(), "dll");
			path = Path.Combine(path, "custombuild.txt");
			if (File.Exists(path))
			{
				var lines = File.ReadAllLines(path);
				if (lines.Length > 0)
				{
					CustomBuildString = lines[0];
				}
			}
		}

		// code copied to avoid depending on code in other projects
		private static string GetExeDirectoryAbsolute()
		{
			var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";
			if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				path = path.Remove(path.Length - 1, 1);
			}

			return path;
		}
	}
}
