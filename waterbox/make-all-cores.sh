#!/bin/sh
set -e
cd "$(dirname "$0")"

make -C emulibc $1 -j
make -C libco $1 -j
./ares64/make-both.sh $1
make -C bsnescore $1 -j
make -C dosbox $1 -j8
make -C dsda $1 -j
make -C gpgx $1 -j
make -C libsnes $1 -j
make -C melon $1 -j
make -C opera $1 -j
make -C picodrive $1 -j
make -C stella $1 -j
make -C snes9x $1 -j
make -C tic80 $1 -j
make -C uae $1 -j
make -C uzem $1 -j
make -C virtualjaguar $1 -j
./nyma/make-all-released-cores.sh $1

# this won't include MAME by default, due to the large amount of time it takes to build it
# to include MAME just do INCLUDE_MAME=1 ./make-all-cores.sh
if test "$INCLUDE_MAME" ; then
	make -C mame-arcade $1 -j
fi
