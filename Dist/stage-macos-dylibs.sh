#!/bin/sh
# Stage the macOS (x86_64 / Rosetta) native-library glue into the EmuHawk output dir.
#
# Run this once after building (Dist/BuildRelease.sh) and before launching with
# Assets/EmuHawkMono.sh. It symlinks the Homebrew-provided dependencies (SDL deps,
# OpenAL, Lua, zstd, SQLite, the X11 client stack) and creates the Linux-soname aliases
# (libX11.so.6, etc.) that BizHawk's P/Invokes and Mono expect, all pointing at the SAME
# Homebrew libX11 so cairo/libgdiplus/WinForms/SDL agree (see notes in EmuHawkMono.sh).
#
# The dylibs BizHawk itself builds/bundles (libSDL2, libgdiplus, libcimgui, libwaterboxhost)
# ship in Assets/dll and are copied to output/dll by the build, so they are not handled here.
#
# Prereqs (x86_64 Homebrew under Rosetta):
#   brew install mono sdl2 openal-soft lua@5.4 zstd sqlite mono-libgdiplus \
#                libx11 libxext libxrender libxcursor libxinerama libxi libxrandr \
#                libxtst libxfixes libxscrnsaver libxau libxdmcp libxcb
#   brew install --cask xquartz
set -e

# Target dll dir: arg 1, else ./dll relative to cwd, else output/dll under the repo.
DLL="${1:-}"
if [ -z "$DLL" ]; then
	if [ -d "dll" ]; then DLL="dll"
	else DLL="$(cd "$(dirname "$0")/.." && pwd)/output/dll"; fi
fi
if [ ! -d "$DLL" ]; then printf "output dll dir not found: %s\n" "$DLL" >&2; exit 1; fi
cd "$DLL"

opt() { echo "/usr/local/opt/$1"; }
link() { # link <target-abs-path> <linkname>
	[ -e "$1" ] && ln -sf "$1" "$2" || printf "WARN missing %s (for %s)\n" "$1" "$2" >&2
}

# Homebrew runtime deps under their canonical names
link "$(opt lua@5.4)/lib/liblua5.4.dylib"        liblua54.dylib
link "$(opt openal-soft)/lib/libopenal.dylib"    libopenal.dylib
link "$(opt openal-soft)/lib/libopenal.1.dylib"  libopenal.1.dylib
link "$(opt zstd)/lib/libzstd.1.dylib"           libzstd.1.dylib
link "$(opt sqlite)/lib/libsqlite3.dylib"        libe_sqlite3.dylib
link "$(opt sqlite)/lib/libsqlite3.dylib"        e_sqlite3.dylib

# X11 client stack (all from Homebrew, so they share one libX11)
link "$(opt libx11)/lib/libX11.6.dylib"          libX11.6.dylib
link "$(opt libxext)/lib/libXext.6.dylib"        libXext.6.dylib
link "$(opt libxrender)/lib/libXrender.1.dylib"  libXrender.1.dylib
link "$(opt libxcursor)/lib/libXcursor.1.dylib"  libXcursor.1.dylib
link "$(opt libxinerama)/lib/libXinerama.1.dylib" libXinerama.1.dylib
link "$(opt libxi)/lib/libXi.6.dylib"            libXi.6.dylib
link "$(opt libxrandr)/lib/libXrandr.2.dylib"    libXrandr.2.dylib
link "$(opt libxtst)/lib/libXtst.6.dylib"        libXtst.6.dylib
link "$(opt libxfixes)/lib/libXfixes.3.dylib"    libXfixes.3.dylib
link "$(opt libxscrnsaver)/lib/libXss.1.dylib"   libXss.1.dylib
link "$(opt libxau)/lib/libXau.6.dylib"          libXau.6.dylib
link "$(opt libxdmcp)/lib/libXdmcp.6.dylib"      libXdmcp.6.dylib
link "$(opt libxcb)/lib/libxcb.1.dylib"          libxcb.1.dylib

# Linux-soname aliases that BizHawk's P/Invokes and Mono hardcode (XlibImports etc.)
ln -sf libX11.6.dylib     libX11.dylib
ln -sf libX11.6.dylib     libX11.so.6
ln -sf libXfixes.3.dylib  libXfixes.so.3
ln -sf libXi.6.dylib      libXi.so.6
ln -sf libzstd.1.dylib    libzstd.so.1
ln -sf libgdiplus.0.dylib libgdiplus.dylib

printf "staged macOS dylib symlinks into %s\n" "$DLL"
