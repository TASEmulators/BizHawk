#!/bin/sh
cd "$(dirname "$0")/.." && nuget restore BizHawk.sln && msbuild /p:Configuration=Debug BizHawk.sln
