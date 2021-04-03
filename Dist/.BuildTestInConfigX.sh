#!/bin/sh
if [ ! -e "BizHawk.sln" ]; then
	printf "wrong cwd (ran manually)? exiting\n"
	exit 1
fi
config="$1"
shift
Dist/.InvokeCLIOnMainSln.sh "test" "$config" -a . -l "junit;LogFilePath=$PWD/test_output/{assembly}.coverage.xml;MethodFormat=Class;FailureBodyFormat=Verbose" "$@"
