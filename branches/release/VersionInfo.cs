static class VersionInfo
{
	public const string MAINVERSION = "1.9.2";
	public static string RELEASEDATE = "March 5, 2015";
	public static bool DeveloperBuild = false;

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
