#!/bin/sh
set -e
if [ -z "$SYSROOT" ]; then
	export SYSROOT="$(realpath "$(dirname "$0")/../sysroot")"
fi

if [ "$(find "upstream-src" -maxdepth 0 -empty)" ]; then
	git submodule update --init "upstream-src"
fi
cp -a "upstream-src" "src-combined"
cd "src-combined"
../.copy-dirs-for-wbox-arch.sh

if (uname -r | grep -q microsoft); then # on WSL, comment-out chmod invocations as they will error
	for f in "Makefile" "tools/install.sh"; do
		sed -i "s/chmod/#chmod/g" "$f"
	done
fi
patch -p1 -u <"../waterbox.patch"

../.wrapped-configure.sh
make
make install

../.postpatch.sh
