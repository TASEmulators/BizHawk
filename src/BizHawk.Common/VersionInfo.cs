using System.IO;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Common
{
	public static partial class VersionInfo
	{
		/// <remarks>
		/// Bump this immediately after release.
		/// Only use '0'..'9' and '.' or it will fail to parse and the new version notification won't work.
		/// </remarks>
		public static readonly string MainVersion = "2.9.2";

		public static readonly string ReleaseDate = "May 3, 2023";

		public static readonly string HomePage = "https://tasvideos.org/BizHawk";

		public static readonly bool DeveloperBuild = true;

		public static readonly string? CustomBuildString;

		public static readonly string BizHawkContributorsListURI = "https://github.com/TASEmulators/BizHawk/graphs/contributors";

		public static readonly string UserAgentEscaped;

		static VersionInfo()
		{
			var path = Path.Combine(
				AppContext.BaseDirectory.RemoveSuffix(Path.DirectorySeparatorChar),
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
			UserAgentEscaped = $"{
				(string.IsNullOrWhiteSpace(CustomBuildString) ? "EmuHawk" : CustomBuildString!.OnlyAlphanumeric())
			}/{MainVersion}{(DeveloperBuild ? "-dev" : string.Empty)}";
		}

		public static (string Label, string TargetURI) GetGitCommitLink()
			=> ($"Commit :{GIT_BRANCH}@{GIT_SHORTHASH}", $"https://github.com/TASEmulators/BizHawk/commit/{GIT_HASH}");

		public static string GetFullVersionDetails()
		{
			//TODO prepare for AArch64/RISC-V
			var targetArch = UIntPtr.Size is 8 ? "x64" : "x86";
#if DEBUG
			const string buildConfig = "Debug";
#else
			const string buildConfig = "Release";
#endif
			return DeveloperBuild
				? $"Version {MainVersion} — dev build ({buildConfig}, {targetArch})"
				: $"Version {MainVersion} ({targetArch})";
		}

		public static string GetEmuVersion()
			=> DeveloperBuild ? $"GIT {GIT_BRANCH}#{GIT_SHORTHASH}" : $"Version {MainVersion}"; // intentionally leaving '#' here to differentiate it from the "proper" one in `Help` > `About...` --yoshi

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
