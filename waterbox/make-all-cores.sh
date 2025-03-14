#!/bin/sh
set -e
cd "$(dirname "$0")"

make -C emulibc
make -C libco
make -C ares64
make -C bsnescore
make -C dsda
make -C gpgx
make -C libsnes
make -C melon
make -C picodrive
make -C stella
make -C snes9x
make -C tic80
make -C uae
make -C uzem
make -C virtualjaguar
./nyma/make-all-released-cores.sh $1

# this won't include MAME by default, due to the large amount of time it takes to build it
# to include MAME just do INCLUDE_MAME=1 ./make-all-cores.sh
if test "$INCLUDE_MAME" ; then
	make -C mame-arcade
fi
