#!/bin/sh
cargo b
cp target/debug/libwaterboxhost.so ../../Assets/dll
cp target/debug/libwaterboxhost.so ../../output/dll
