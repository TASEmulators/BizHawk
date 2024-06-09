#!/bin/sh
set -e
kind="$(basename "$0")"
dotnet pwsh "./Dist/git_hooks/$kind.ps1" "$@"
if [ -e "./Dist/git_hooks/$kind.local.ps1" ]; then dotnet pwsh "./Dist/git_hooks/$kind.local.ps1" "$@"; fi
