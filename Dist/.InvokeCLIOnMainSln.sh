#!/bin/sh
set -e
if [ ! -e "BizHawk.slnx" ]; then
	printf "wrong cwd (ran manually)? exiting\n"
	exit 1
fi
cmd="$1"
shift
config="$1"
shift
if [ -z "$NUGET_PACKAGES" ]; then export NUGET_PACKAGES="$HOME/.nuget/packages"; fi
printf "running 'dotnet %s' in %s configuration, extra args: %s\n" "$cmd" "$config" "$*"
version=$(grep -Po "MainVersion = \"\K(.*)(?=\")" src/BizHawk.Common/VersionInfo.cs)
git_hash="$(git rev-parse --verify HEAD 2>/dev/null || printf "0000000000000000000000000000000000000000")"
dotnet "$cmd" BizHawk.slnx -c "$config" -m -clp:NoSummary -p:Version="$version" -p:SourceRevisionId="$git_hash" "$@"
