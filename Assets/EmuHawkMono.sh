#!/bin/sh
cd "$(dirname "$0")" && if [ -z "$(ps -C "mono" -o "cmd" --no-headers | grep "EmuHawk.exe")" ]; then LD_LIBRARY_PATH="/usr/lib/wine" mono ./EmuHawk.exe; fi
