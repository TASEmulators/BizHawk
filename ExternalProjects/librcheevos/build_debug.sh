#!/bin/sh
rm -rf build
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Debug -DCMAKE_C_COMPILER=clang -G Ninja
ninja
