#!/bin/sh
# Run after a managed build; no dependencies on an existing output directory.
set -eu
cd "$(dirname "$0")/.."
mkdir -p output/dll
for lib in libbizhash.dylib libwaterboxhost.dylib libgdiplus.0.dylib libSDL2.dylib libcimgui.dylib; do
	if [ ! -f "Assets/dll/$lib" ]; then
		printf 'Missing Assets/dll/%s; run sh Dist/BuildMacOS.sh first.\n' "$lib" >&2
		exit 1
	fi
	# Replace old dependency symlinks without writing into Homebrew's installation.
	if [ -L "output/dll/$lib" ]; then unlink "output/dll/$lib"; fi
	cp "Assets/dll/$lib" "output/dll/$lib"
done
