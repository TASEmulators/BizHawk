start fart "..\src\BizHawk.Common\VersionInfo.cs" "DeveloperBuild = true" "DeveloperBuild = false"
call QuickTestBuildAndPackage.bat
start fart "..\src\BizHawk.Common\VersionInfo.cs" "DeveloperBuild = false" "DeveloperBuild = true"
