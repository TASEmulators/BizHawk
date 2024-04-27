#!/bin/sh
set -e

jobCount=`nproc`

make -f interpreter.mak $1 -j${jobCount}
make -f recompiler.mak $1 -j${jobCount}
