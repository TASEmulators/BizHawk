#!/bin/sh
cd "$(dirname "$0")/.." && (rm -r src/*/bin src/*/obj src/BizHawk.Common/VersionInfo.gen.cs test_output output; mkdir output)
