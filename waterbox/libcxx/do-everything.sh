#!/bin/sh
set -e
./configure-for-waterbox-phase-- && cd build- && make -j && make install && cd ..
printf "completed phase -1\n"
./configure-for-waterbox-phase-0 && cd build0 && make -j && make install && cd ..
printf "completed phase 0\n"
./configure-for-waterbox-phase-1 && cd build1 && make -j && make install && cd ..
printf "completed phase 1\n"
./configure-for-waterbox-phase-2 && cd build2 && make -j && make install && cd ..
printf "completed phase 2\n"
