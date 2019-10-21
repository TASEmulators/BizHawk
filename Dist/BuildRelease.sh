#!/bin/sh
cd "$(dirname "$0")/.." && dotnet build BizHawk.sln -c Release
