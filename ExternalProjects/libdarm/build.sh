#!/bin/sh
set -e
cd "$(dirname "$0")/../../submodules/libdarm"
make
cp "libdarm.so" "../../Assets/dll"
