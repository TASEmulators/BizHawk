#!/bin/sh
./flatc --cpp --gen-object-api NymaTypes.fbs
./flatc --csharp --gen-object-api -o ../../ExternalProjects/FlatBuffers.GenOutput NymaTypes.fbs
if ! ( uname -s | fgrep -i cygwin ); then
	unix2dos NymaTypes_generated.h
	unix2dos ../../ExternalProjects/FlatBuffers.GenOutput/NymaTypes/*
fi
