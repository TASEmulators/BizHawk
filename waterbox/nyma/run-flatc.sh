#!/bin/sh
./flatc --cpp --gen-object-api NymaTypes.fbs
./flatc --csharp --gen-object-api -o ../../ExternalProjects/FlatBuffers.GenOutput NymaTypes.fbs
