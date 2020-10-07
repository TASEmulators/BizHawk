#!/bin/sh
echo "yo we updating VersionInfo"
cd "$(dirname "$0")/../src/BizHawk.Common"
sed -i "s/ReleaseDate = \"[^\"]*\"/ReleaseDate = \"$(date "+%B %-d, %Y")\"/" "VersionInfo.cs"
sed -i "s/DeveloperBuild = true/DeveloperBuild = false/" "VersionInfo.cs"
