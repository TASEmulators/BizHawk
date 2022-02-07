#!/bin/sh
set -e
cd "$(dirname "$0")"
dotnet build "$@"
cd bin
mono Test.exe
