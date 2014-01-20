static class VersionInfo
{
	public const string MAINVERSION = "1.6.0";
	public const string RELEASEDATE = "Beta built on January 20, 2014";
	public static bool INTERIM = false;

	public static string GetEmuVersion()
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
