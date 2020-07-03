#!/bin/sh
cargo b --release
cp target/release/libwaterboxhost.so ../../Assets
