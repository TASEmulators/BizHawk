#!/bin/sh
set -e

cd emulibc && make -f Makefile $1 -j && cd -
cd libco && make -f Makefile $1 -j && cd -
cd gpgx && make -f Makefile $1 -j && cd -
cd libsnes && make -f Makefile $1 -j && cd -
cd picodrive && make -f Makefile $1 -j && cd -
cd sameboy && make -f Makefile $1 -j && cd -
cd snes9x && make -f Makefile $1 -j && cd -
cd uzem && make -f Makefile $1 -j && cd -
cd vb && make -f Makefile $1 -j && cd -
cd nyma && make -f faust.mak $1 -j && cd -
cd nyma && make -f hyper.mak $1 -j && cd -
#cd nyma && make -f lynx.mak $1 -j && cd -
cd nyma && make -f ngp.mak $1 -j && cd -
cd nyma && make -f pcfx.mak $1 -j && cd -
cd nyma && make -f ss.mak $1 -j && cd -
cd nyma && make -f turbo.mak $1 -j && cd -
#cd nyma && make -f vb.mak $1 -j && cd -
#cd nyma && make -f wswan.mak $1 -j && cd -
