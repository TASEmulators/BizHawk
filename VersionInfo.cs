static class VersionInfo
{
	public const string MAINVERSION = "1.8.3";
	public static string RELEASEDATE = "September 20, 2014";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
