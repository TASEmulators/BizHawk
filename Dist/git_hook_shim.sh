#!/bin/sh
set -e
pwsh="$(command -v pwsh)"
if [ -z "$pwsh" ]; then pwsh="$(command -v dotnet) pwsh"; fi
if ! ("$pwsh" -v >/dev/null 2>/dev/null); then exit 0; fi
kind="$(basename "$0")"
"$pwsh" "./Dist/git_hooks/$kind.ps1" "$@"
if [ -e "./Dist/git_hooks/$kind.local.ps1" ]; then "$pwsh" "./Dist/git_hooks/$kind.local.ps1" "$@"; fi
