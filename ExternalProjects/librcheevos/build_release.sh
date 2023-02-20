#!/bin/sh
rm -rf build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INTERPROCEDURAL_OPTIMIZATION=TRUE -DCMAKE_C_COMPILER=clang -G Ninja
ninja
