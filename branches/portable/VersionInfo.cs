static class VersionInfo
{
	public const string MAINVERSION = "1.8.4";
	public static string RELEASEDATE = "Unoffical BETA of unknown origin!";
	public static bool DeveloperBuild = true;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
