#!/bin/sh
set -e
cd "$(dirname "$0")"

make -f interpreter.mak $1 -j
make -f recompiler.mak $1 -j
