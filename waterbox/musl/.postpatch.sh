#!/bin/sh
set -e
if [ -e "hawk-src-overlay" ]; then
	printf "found dir hawk-src-overlay in cwd -- if running manually, you missed a step\n"
	exit 1
fi
# To uselibclang-rt (and libunwind), we need to evict the gcc libs, but musl automatically puts that in the specs file and has no setting to remove it. Fix that now by clearing the 13th line.
cd "$SYSROOT/lib"
cp "musl-gcc.specs" "musl-gcc.broken.specs"
awk 'NR==13{print""};NR!=13' "musl-gcc.broken.specs" >"musl-gcc.specs" # clears the line after "*libgcc:"
rm "musl-gcc.broken.specs"
