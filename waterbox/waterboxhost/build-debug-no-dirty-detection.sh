#!/bin/sh
if [ -z "$BIZHAWKBUILD_HOME" ]; then export BIZHAWKBUILD_HOME="$(realpath "$(dirname "$0")/../..")"; fi

cargo b --features "no-dirty-detection"

cp target/debug/libwaterboxhost.so "$BIZHAWKBUILD_HOME/Assets/dll"
if [ -e "$BIZHAWKBUILD_HOME/output" ]; then
	cp target/debug/libwaterboxhost.so "$BIZHAWKBUILD_HOME/output/dll"
fi
