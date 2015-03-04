static class VersionInfo
{
	public const string MAINVERSION = "1.9.2"; // Use numbers only or the new version notification won't work
	public static readonly string RELEASEDATE = "March 5, 2015";
	public static readonly bool DeveloperBuild = true;
	public static readonly string HomePage = "http://tasvideos.org/BizHawk.html";

	public static string GetEmuVersion()
	{
		return DeveloperBuild ? "SVN " + SubWCRev.SVN_REV : ("Version " + MAINVERSION);
	}
}
