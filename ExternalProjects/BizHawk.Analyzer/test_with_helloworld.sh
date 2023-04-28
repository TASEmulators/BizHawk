#!/bin/sh
set -e
cd "$(dirname "$0")"
./build_debug.sh
cd ../../ExternalToolProjects/HelloWorld
rm -fr bin obj
./build_debug.sh
