#!/bin/sh
set -e
if [ ! -e "BizHawk.sln" ]; then
	printf "wrong cwd (ran manually)? exiting\n"
	exit 1
fi
config="$1"
shift
Dist/.InvokeCLIOnMainSln.sh "build" "$config" "$@"
Dist/.InvokeCLIOnMainSln.sh publish "$config" --no-build "$@"
