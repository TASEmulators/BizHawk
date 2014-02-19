static class VersionInfo
{
	public const string MAINVERSION = "1.6.0";
	public const string RELEASEDATE = "February 18, 2014";
	public static bool INTERIM = true;

	public static string GetEmuVersion()
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
