#!/bin/sh
if [ -z "$NUGET_PACKAGES" ]; then export NUGET_PACKAGES="$HOME/.nuget/packages"; fi
cd "$(dirname "$0")/.." && dotnet test BHTest.sln -a . -c Release -l "junit;LogFilePath=$PWD/test_output/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" -m "$@"
