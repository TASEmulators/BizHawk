#!/bin/sh
set -e
if [ ! -e "BizHawk.sln" ]; then
	printf "wrong cwd (ran manually)? exiting\n"
	exit 1
fi
cmd="$1"
shift
config="$1"
shift
if [ -z "$NUGET_PACKAGES" ]; then export NUGET_PACKAGES="$HOME/.nuget/packages"; fi
printf "running 'dotnet %s' in %s configuration, extra args: %s\n" "$cmd" "$config" "$*"
dotnet "$cmd" BizHawk.sln -c "$config" -m -clp:NoSummary "$@"
