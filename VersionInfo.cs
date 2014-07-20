static class VersionInfo
{
	public const string MAINVERSION = "1.7.2";
	public static string RELEASEDATE = "July 20, 2014";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
