#!/bin/sh
cargo b --features "no-dirty-detection"
cp target/debug/libwaterboxhost.so ../../Assets/dll
cp target/debug/libwaterboxhost.so ../../output/dll
