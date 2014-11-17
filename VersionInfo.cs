static class VersionInfo
{
	public const string MAINVERSION = "1.9.0";
	public static string RELEASEDATE = "November 23, 2014";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
