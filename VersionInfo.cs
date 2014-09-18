static class VersionInfo
{
	public const string MAINVERSION = "1.8.2";
	public static string RELEASEDATE = "August 31, 2014";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
