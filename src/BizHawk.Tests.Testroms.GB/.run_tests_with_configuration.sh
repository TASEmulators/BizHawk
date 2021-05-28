#!/bin/sh
set -e
root="$(realpath "$PWD/../..")"
config="$1"
shift
res/download_from_ci.sh
export LD_LIBRARY_PATH="$root/output/dll:$LD_LIBRARY_PATH"
dotnet test -a "$root/test_output" -c "$config" -l "junit;LogFilePath=$root/test_output/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" -l "console;verbosity=detailed" "$@"
