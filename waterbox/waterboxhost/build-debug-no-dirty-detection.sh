#!/bin/sh
cargo b --features "no-dirty-detection"
cp target/debug/libwaterboxhost.so ../../Assets
cp target/debug/libwaterboxhost.so ../../output
