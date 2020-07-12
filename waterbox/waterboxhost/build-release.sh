#!/bin/sh
cargo b --release
cp target/release/libwaterboxhost.so ../../Assets
cp target/release/libwaterboxhost.so ../../output
