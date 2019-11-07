#!/bin/sh
cd "$(dirname "$0")/.." && nuget restore BizHawk.sln && msbuild /p:Configuration=Release BizHawk.sln
