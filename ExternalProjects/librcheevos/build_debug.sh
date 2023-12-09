#!/bin/sh
set -e
if [ -z "$CC" ]; then export CC="clang"; fi

rm -rf build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Debug -DCMAKE_C_COMPILER=$CC -G Ninja
ninja
