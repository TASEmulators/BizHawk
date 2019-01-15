#!/bin/sh
cd "$(dirname "$0")/.." && msbuild /p:Configuration=Release BizHawk.sln
