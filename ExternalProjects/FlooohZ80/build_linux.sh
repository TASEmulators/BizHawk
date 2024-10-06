#!/bin/sh
if [ -z "$BIZHAWKBUILD_HOME" ]; then export BIZHAWKBUILD_HOME="$(realpath "$(dirname "$0")/../..")"; fi
if [ -z "$CXX" ]; then export CXX="clang++"; fi

mkdir -p build
$CXX -std=c++11 -O3 -flto -fvisibility=hidden -fPIC -shared -s FlooohZ80.cpp -o build/libFlooohZ80.so

cp build/libFlooohZ80.so "$BIZHAWKBUILD_HOME/Assets/dll"
if [ -e "$BIZHAWKBUILD_HOME/output" ]; then
	cp build/libFlooohZ80.so "$BIZHAWKBUILD_HOME/output/dll"
fi
