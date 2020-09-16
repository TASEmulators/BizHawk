start fart "..\Version\VersionInfo.cs" "DeveloperBuild = true" "DeveloperBuild = false"
call QuickTestBuildAndPackage.bat
start fart "..\Version\VersionInfo.cs" "DeveloperBuild = false" "DeveloperBuild = true"
