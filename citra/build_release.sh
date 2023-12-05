#!/bin/sh
set -e
if [ -z "$CC" ]; then export CC="clang"; fi
if [ -z "$CXX" ]; then export CXX="clang++"; fi

# stdlibc++ has a bug in some versions (at least in Ubuntu 22.04, probably Debian 12 too) that will cause compilation to fail for Citra
# Debian 11's stdlibc++ is unaffected (and probably Ubuntu 20.04's too)
# Note that Debian 10's stdlibc++ is too old (nearly no c++20 support), so at least Debian 11 must be used to compile this core
# At least cmake 3.20 must be present too, so get cmake from bullseye-backports

rm -rf build
mkdir build
export GLSLANG_VALIDATOR_DIR=$(realpath ./glslangValidator)
cd build
cmake ../citra -DENABLE_SDL2=OFF -DUSE_SYSTEM_SDL2=OFF -DENABLE_QT=OFF -DENABLE_QT_TRANSLATION=OFF -DENABLE_QT_UPDATER=OFF \
 -DENABLE_TESTS=OFF -DENABLE_DEDICATED_ROOM=OFF -DENABLE_WEB_SERVICE=OFF -DENABLE_CUBEB=OFF -DENABLE_OPENAL=OFF \
 -DENABLE_LIBUSB=OFF -DUSE_DISCORD_PRESENCE=OFF -DUSE_SYSTEM_BOOST=OFF -DUSE_SYSTEM_OPENSSL=OFF -DUSE_SYSTEM_LIBUSB=OFF \
 -DENABLE_COMPATIBILITY_LIST_DOWNLOAD=OFF -DENABLE_HEADLESS=ON -DCMAKE_PROGRAM_PATH=$GLSLANG_VALIDATOR_DIR \
 -DCMAKE_BUILD_TYPE=Release -DENABLE_LTO=ON -DCMAKE_C_COMPILER=$CC -DCMAKE_CXX_COMPILER=$CXX \
 -DCMAKE_POSITION_INDEPENDENT_CODE=ON -DCMAKE_CXX_FLAGS="-Wno-deprecated -include limits.h" -G Ninja
ninja
