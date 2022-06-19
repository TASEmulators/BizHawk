#!/bin/sh
set -e

cd emulibc && make -f Makefile $1 -j && cd -
cd libco && make -f Makefile $1 -j && cd -
cd gpgx && make -f Makefile $1 -j && cd -
cd libsnes && make -f Makefile $1 -j && cd -
cd melon && make -f Makefile $1 -j && cd -
cd picodrive && make -f Makefile $1 -j && cd -
cd snes9x && make -f Makefile $1 -j && cd -
cd tic80 && make -f Makefile $1 -j && cd -
cd uzem && make -f Makefile $1 -j && cd -
cd nyma && ./make-all-released-cores.sh $1 && cd -
