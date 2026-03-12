#!/bin/sh
set -e
if [ -z "$CC" ]; then export CC="clang"; fi
if [ -z "$CXX" ]; then export CXX="clang++"; fi

rm -rf build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=ON -DCMAKE_C_COMPILER=$CC -DCMAKE_CXX_COMPILER=$CXX -G Ninja
ninja
