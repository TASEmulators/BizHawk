#!/bin/sh
set -e

make -f turbo.mak $1 -j
make -f hyper.mak $1 -j
make -f ngp.mak $1 -j
make -f faust.mak $1 -j
make -f pcfx.mak $1 -j
make -f ss.mak $1 -j
# make -f lynx.mak $1 -j
# make -f vb.mak $1 -j
# make -f wswan.mak $1 -j
