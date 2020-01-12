using System.IO;

internal static class VersionInfo
{
	// keep this updated at every major release
	public const string Mainversion = "2.3.2"; // Use numbers only or the new version notification won't work
	public static readonly string RELEASEDATE = "June 18, 2019";
	public static readonly bool DeveloperBuild = true;
	public static readonly string HomePage = "http://tasvideos.org/BizHawk.html";

	public static readonly string CustomBuildString;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? ("GIT " + SubWCRev.GIT_BRANCH + "#" + SubWCRev.GIT_SHORTHASH) : ("Version " + Mainversion);
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
		var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
		if (path.EndsWith(Path.DirectorySeparatorChar.ToString()))
		{
			path = path.Remove(path.Length - 1, 1);
		}

		return path;
	}
}
