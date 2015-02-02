start fart "..\Version\VersionInfo.cs" "DeveloperBuild = true" "DeveloperBuild = false"
start BuildAndPackage.bat
start fart "..\Version\VersionInfo.cs" "DeveloperBuild = false" "DeveloperBuild = true"
exit