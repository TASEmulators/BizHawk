#!/bin/sh
set -e
config="$1"
shift
dotnet test -c "$config" \
	-l "junit;LogFilePath=TestResults/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" \
	-l "console;verbosity=detailed" \
	"$@"
