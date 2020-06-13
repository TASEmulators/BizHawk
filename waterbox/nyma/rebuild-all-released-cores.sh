#!/bin/sh

make -f pce.mak install -j6
make -f pce-fast.mak install -j6
make -f ngp.mak install -j6
make -f faust.mak install -j6
make -f pcfx.mak install -j6
make -f ss.mak install -j6
