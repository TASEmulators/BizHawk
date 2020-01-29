#!/bin/sh
name="$(basename "$PWD").dll"
CscToolExe="$(which csc)" dotnet build -c Release -m && cp -f "bin/Release/net48/$name" "../../output/ExternalTools/$name"
