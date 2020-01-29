#!/bin/sh
name="$(basename "$PWD").dll"
CscToolExe="$(which csc)" dotnet build -c Debug -m && cp -f "bin/Debug/net48/$name" "../../output/ExternalTools/$name"
