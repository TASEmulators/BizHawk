using System.IO;
using System.Reflection;

namespace BizHawk.Common
{
	public static partial class VersionInfo
	{
		// keep this updated at every major release
		public static readonly string MainVersion = "2.5.3"; // Use numbers only or the new version notification won't work
		public static readonly string ReleaseDate = "September 12, 2020";
		public static readonly string HomePage = "http://tasvideos.org/BizHawk.html";
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

		/// <summary>"2.5.1" => 0x02050100</summary>
		public static int VersionStrToInt(string s)
		{
			var a = s.Split('.');
			var v = 0;
			var i = 0;
			while (i < 4)
			{
				v <<= 8;
				v += (i < a.Length && byte.TryParse(a[i], out var b)) ? b : 0;
				i++;
			}
			return v;
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
