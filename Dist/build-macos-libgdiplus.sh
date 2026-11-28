#!/bin/sh
# Mono WinForms needs libgdiplus with X11; the stock Homebrew build lacks it.
# Upstream build instructions: https://github.com/mono/libgdiplus#build-instructions
set -eu
cd "$(dirname "$0")/.."
repo="$PWD"
out="$repo/Assets/dll/libgdiplus.0.dylib"
if [ "${BIZHAWK_REBUILD_GDIPLUS:-0}" != 1 ] && [ -f "$out" ] && lipo -verify_arch x86_64 "$out" && otool -D "$out" | grep -Fxq '@rpath/libgdiplus.0.dylib' && nm -gU "$out" | grep -q ' _GdipCreateFromXDrawable_linux$'; then
	printf '%s\n' 'Using existing x86_64 libgdiplus with X11 support.'
	exit 0
fi
export PATH="/usr/local/bin:$PATH"
if [ ! -x /usr/local/bin/pkg-config ]; then
	printf '%s\n' 'Install pkg-config and the graphics build dependencies with x86_64 Homebrew; see README.md.' >&2
	exit 1
fi
# Keep Cairo and X11 on the same Homebrew installation as the launcher.
export PKG_CONFIG_PATH=/usr/local/lib/pkgconfig
/usr/local/bin/pkg-config --exists cairo-xlib glib-2.0 libexif pangocairo pangoft2
build_dir=$(mktemp -d "${TMPDIR:-/tmp}/bizhawk-libgdiplus.XXXXXX")
cd "$build_dir"
curl -fL https://download.mono-project.com/sources/libgdiplus/libgdiplus-6.1.tar.gz -o libgdiplus-6.1.tar.gz
printf '%s\n' '97d5a83d6d6d8f96c27fb7626f4ae11d3b38bc88a1726b4466aeb91451f3255b  libgdiplus-6.1.tar.gz' | shasum -a 256 -c -
tar -xzf libgdiplus-6.1.tar.gz
cd libgdiplus-6.1
MACOSX_DEPLOYMENT_TARGET=14.0 CC=clang CXX=clang++ CFLAGS='-arch x86_64 -mmacosx-version-min=14.0 -O2' CXXFLAGS='-arch x86_64 -mmacosx-version-min=14.0 -O2' \
	CPPFLAGS='-I/usr/local/include' LDFLAGS='-arch x86_64 -mmacosx-version-min=14.0 -L/usr/local/lib' NM=/usr/bin/nm \
	LIBS="-framework CoreGraphics $(/usr/local/bin/pkg-config --libs pangoft2)" \
	./configure --host=x86_64-apple-darwin --disable-static --with-pango
make -C src -j4
lipo -verify_arch x86_64 src/.libs/libgdiplus.0.dylib
nm -gU src/.libs/libgdiplus.0.dylib | grep -q ' _GdipCreateFromXDrawable_linux$'
install_name_tool -id @rpath/libgdiplus.0.dylib src/.libs/libgdiplus.0.dylib
codesign --force --sign - src/.libs/libgdiplus.0.dylib
cp src/.libs/libgdiplus.0.dylib "$out"
mkdir -p "$repo/Assets/native-licenses"
cp LICENSE "$repo/Assets/native-licenses/libgdiplus-LICENSE"
