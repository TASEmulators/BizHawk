#!/bin/sh
set -e
if [ -z "$CC" ]; then export CC="clang"; fi

rm -rf build
mkdir build
cd build
cmake ../libchdr -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER=$CC -G Ninja \
	-DBUILD_LTO=ON -DBUILD_SHARED_LIBS=ON -DINSTALL_STATIC_LIBS=OFF -DWITH_SYSTEM_ZLIB=OFF
ninja
cp -t ../../../Assets/dll/ ./libchdr.so
cp -t ../../../output/dll/ ./libchdr.so
