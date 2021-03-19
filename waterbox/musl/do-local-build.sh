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
cp -a "../hawk-src-overlay" -T "."

if (uname -r | grep -q microsoft); then # on WSL, comment-out chmod invocations as they will error
	for f in "Makefile" "tools/install.sh"; do
		sed -i "s/chmod/#chmod/g" "$f"
	done
fi
patch -p1 -u <"../waterbox.patch"

mkdir -p "$SYSROOT"
export LDFLAGS=""
export CFLAGS="-Werror -fvisibility=hidden -mcmodel=large -mstack-protector-guard=global -no-pie -fno-pic -fno-pie" # TODO: Add -flto
export AR="gcc-ar"
export RANLIB="gcc-ranlib"
./configure --target=waterbox --build=waterbox --disable-shared --prefix="$SYSROOT" --syslibdir="$SYSROOT/syslib"
make
make install

# To uselibclang-rt (and libunwind), we need to evict the gcc libs, but musl automatically puts that in the specs file and has no setting to remove it. Fix that now by clearing the 13th line.
cd "$SYSROOT/lib"
cp "musl-gcc.specs" "musl-gcc.broken.specs"
awk 'NR==13{print""};NR!=13' "musl-gcc.broken.specs" >"musl-gcc.specs" # clears the line after "*libgcc:"
rm "musl-gcc.broken.specs"
