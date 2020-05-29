#!/bin/sh
./flatc --cpp --gen-object-api NymaTypes.fbs
./flatc --csharp --gen-object-api -o ../../src/Bizhawk.Emulation.Cores/Waterbox NymaTypes.fbs
