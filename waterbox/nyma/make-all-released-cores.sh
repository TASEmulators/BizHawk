#!/bin/sh
set -e

make -f turbo.mak $1 -j install
make -f hyper.mak $1 -j install
make -f ngp.mak $1 -j install
make -f faust.mak $1 -j install
make -f pcfx.mak $1 -j install
make -f ss.mak $1 -j install
make -f shock.mak $1 -j install
# make -f lynx.mak $1 -j install
make -f vb.mak $1 -j install
# make -f wswan.mak $1 -j install
