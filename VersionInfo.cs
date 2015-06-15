static class VersionInfo
{
	public const string MAINVERSION = "1.10.0";
	public static string RELEASEDATE = "June 15, 2015";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
