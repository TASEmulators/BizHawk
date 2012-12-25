#!/bin/sh

#makes all bsnes binaries for distribution
#TODO - 64bit

./bizwinclean.sh
./bizwinmakeone.sh performance compress
./bizwinclean.sh
./bizwinmakeone.sh compatibility compress

#leave compatibility built as objs because thats more useful to us while devving

