static class VersionInfo
{
	public const string MAINVERSION = "1.5.2";
	public const string RELEASEDATE = "August 22, 2013";
	public static bool INTERIM = true;

	public static string GetEmuVersion() //This doesn't need to be on mainform
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
