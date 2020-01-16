#!/bin/sh
name="$(basename "$PWD").dll"
CscToolExe="$(which csc)" dotnet build -c Release -m && cp -f "bin/Release/$name" "../../output/ExternalTools/$name"
