#!/bin/sh
cd "$(dirname "$0")/.." && CscToolExe="$(which csc)" dotnet build BizHawk.sln -c Release -m -p:MachineNuGetPackageDir=$HOME/.nuget/packages "$@"
