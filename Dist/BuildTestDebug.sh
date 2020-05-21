#!/bin/sh
if [ -z "$NUGET_PACKAGES" ]; then export NUGET_PACKAGES="$HOME/.nuget/packages"; fi
cd "$(dirname "$0")/.." && dotnet test BizHawk.sln -a . -c Debug -l "junit;LogFilePath=$PWD/test_output/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" -m "$@"
