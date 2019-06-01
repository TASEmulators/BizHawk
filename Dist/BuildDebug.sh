#!/bin/sh
cd "$(dirname "$0")/.." && msbuild /p:Configuration=Debug BizHawk.sln
