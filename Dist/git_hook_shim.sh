#!/bin/sh
set -e
dotnet pwsh "./Dist/git_hooks/$(basename "$0").ps1" "$@"
