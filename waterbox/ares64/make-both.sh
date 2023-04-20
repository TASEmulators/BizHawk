#!/bin/sh
set -e

make -f interpreter.mak $1 -j
make -f recompiler.mak $1 -j
