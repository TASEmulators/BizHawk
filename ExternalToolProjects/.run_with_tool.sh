#!/bin/sh
../../output/EmuHawkMono.sh --mono-no-redirect --open-ext-tool-dll=$(printf *.csproj | head -c-7) "$@"
