#!/bin/sh
cargo b --release
cp target/release/libwaterboxhost.so ../../Assets/dll
cp target/release/libwaterboxhost.so ../../output/dll
