#!/bin/sh
set -e

jobCount=`nproc`

make -f turbo.mak $1 -j${jobCount}
make -f hyper.mak $1 -j${jobCount}
make -f ngp.mak $1 -j${jobCount}
make -f faust.mak $1 -j${jobCount}
make -f pcfx.mak $1 -j${jobCount}
make -f ss.mak $1 -j${jobCount}
make -f shock.mak $1 -j${jobCount}
# make -f lynx.mak $1 -j${jobCount}
make -f vb.mak $1 -j${jobCount}
# make -f wswan.mak $1 -j${jobCount}
