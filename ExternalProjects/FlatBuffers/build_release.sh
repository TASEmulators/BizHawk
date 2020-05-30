#!/bin/sh
cd "$(dirname "$0")" && dotnet build FlatBuffers.Core.csproj -c Release && cp bin/Release/*/FlatBuffers.Core.dll ../../References
