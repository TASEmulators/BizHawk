#!/bin/sh

./configure-for-waterbox-phase--
cd build-
make
make install
cd ..

./configure-for-waterbox-phase-0
cd build0
make
make install
cd ..

./configure-for-waterbox-phase-1
cd build1
make
make install
cd ..

./configure-for-waterbox-phase-2
cd build2
make
make install
cd ..
