#!/bin/sh

#makes all bsnes binaries for distribution
#use bizwinmakedistro64.sh for 64bit (dont run it at the same time!)

./bizwinclean.sh
./bizwinmakeone.sh 64 performance compress
./bizwinclean.sh
./bizwinmakeone.sh 64 compatibility compress

#leave compatibility built as objs because thats more useful to us while devving

