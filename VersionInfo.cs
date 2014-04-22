using System;

static class VersionInfo
{
	public const string MAINVERSION = "1.7.0";
	public static string RELEASEDATE = "Beta built on " + DateTime.Now.ToShortDateString();
	public static bool INTERIM = true;

	public static string GetEmuVersion()
	{
		return INTERIM ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
