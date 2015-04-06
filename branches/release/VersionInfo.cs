static class VersionInfo
{
	public const string MAINVERSION = "1.9.4";
	public static string RELEASEDATE = "April 7, 2015";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
