static class VersionInfo
{
	public const string MAINVERSION = "1.6.1";
	public const string RELEASEDATE = "unknown";
	public static bool INTERIM = true;

	public static string GetEmuVersion()
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
