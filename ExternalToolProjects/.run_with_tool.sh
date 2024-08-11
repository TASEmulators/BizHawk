#!/bin/sh
../../output/EmuHawkMono.sh --open-ext-tool-dll=$(printf *.csproj | head -c-7) "$@"
