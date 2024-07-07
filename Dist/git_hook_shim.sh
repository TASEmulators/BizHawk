#!/bin/sh
kind="$(basename "$0")"
pwsh="$(command -v pwsh)"
if [ -z "$pwsh" ]; then pwsh="$(command -v dotnet) pwsh"; fi
if ! ("$pwsh" -v >/dev/null 2>/dev/null); then
	printf "pwsh not found in PATH; skipping %s hook\n" "$kind"
	exit 0
fi
if [ -e "./Dist/git_hooks/$kind.ps1" ]; then
	"$pwsh" "./Dist/git_hooks/$kind.ps1" "$@" || exit $?
	if [ -e "./Dist/git_hooks/$kind.local.ps1" ]; then "$pwsh" "./Dist/git_hooks/$kind.local.ps1" "$@" || exit $?; fi
fi
