using System.Reflection;

using BizHawk.Common;

[assembly: AssemblyVersion(VersionInfo.MainVersion)]
[assembly: AssemblyFileVersion(VersionInfo.MainVersion + "." + VersionInfo.SVN_REV)]
[assembly: AssemblyInformationalVersion(VersionInfo.MainVersion + "." + VersionInfo.SVN_REV + "#" + VersionInfo.GIT_SHORTHASH)]
