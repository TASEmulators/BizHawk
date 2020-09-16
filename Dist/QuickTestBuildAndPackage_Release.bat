start fart "..\Version\VersionInfo.cs" "DeveloperBuild = true" "DeveloperBuild = false"
call BuildAndPackage.bat
start fart "..\Version\VersionInfo.cs" "DeveloperBuild = false" "DeveloperBuild = true"
