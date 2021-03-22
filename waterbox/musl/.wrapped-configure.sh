#!/bin/sh
set -e
if [ -e "hawk-src-overlay" ]; then
	printf "found dir hawk-src-overlay in cwd -- if running manually, you missed a step\n"
	exit 1
fi
mkdir -p "$SYSROOT"
export LDFLAGS=""
export CFLAGS="-Werror -fvisibility=hidden -mcmodel=large -mstack-protector-guard=global -no-pie -fno-pic -fno-pie" # TODO: Add -flto
export AR="gcc-ar"
export RANLIB="gcc-ranlib"
./configure --target=waterbox --build=waterbox --disable-shared --prefix="$SYSROOT" --syslibdir="$SYSROOT/syslib"
