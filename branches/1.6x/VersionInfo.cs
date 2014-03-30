static class VersionInfo
{
	public const string MAINVERSION = "1.6.2";
	public const string RELEASEDATE = "Interim";
	public static bool INTERIM = true;

	public static string GetEmuVersion()
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
