#!/bin/sh
if ( uname -s | fgrep -i cygwin ); then
	flatc=./flatc
else
	flatc="$(command -v flatc)"
	if [ -z "$flatc" ]; then
		if ! ( command -v nix >/dev/null ); then
			printf "You do not have flatc (the FlatBuffers schema compiler) installed.\nIf it's not available from your package manager, you will have to build from source:\n%s\n" "https://google.github.io/flatbuffers/flatbuffers_guide_building.html"
			exit 1
		fi
		printf "Grabbing flatc via Nix...\n"
		nix-shell --run "$0"
		exit $?
	fi
fi
"$flatc" --cpp --gen-object-api NymaTypes.fbs
"$flatc" --csharp --gen-object-api -o ../../ExternalProjects/FlatBuffers.GenOutput NymaTypes.fbs
if ! ( uname -s | fgrep -i cygwin ); then
	unix2dos NymaTypes_generated.h
	unix2dos ../../ExternalProjects/FlatBuffers.GenOutput/NymaTypes/*
fi
