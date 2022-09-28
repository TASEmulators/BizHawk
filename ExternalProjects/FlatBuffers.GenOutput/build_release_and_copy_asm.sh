#!/bin/sh
outDir="../../References"
cd "$(dirname "$0")" && rm -f "$outDir/FlatBuffers.GenOutput.dll" && dotnet build -c Release "$@" && cp bin/Release/*/FlatBuffers.GenOutput.dll $outDir
