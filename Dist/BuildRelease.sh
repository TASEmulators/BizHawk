#!/bin/sh
cd "$(dirname "$0")/.." && nuget restore BizHawk.sln && CscToolExe="$(which csc)" dotnet build BizHawk.sln -c Release "$@"
