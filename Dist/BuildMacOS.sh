#!/bin/sh
# Build the experimental x86_64 Mono + XQuartz port from source.
set -eu
cd "$(dirname "$0")/.."
if [ "$(uname -s)" != Darwin ]; then
	printf '%s\n' 'This helper requires macOS.' >&2
	exit 1
fi
for tool in dotnet rustup make clang cmake; do
	if ! command -v "$tool" >/dev/null 2>&1; then
		printf 'Missing %s; see the macOS setup in README.md.\n' "$tool" >&2
		exit 1
	fi
done
if [ ! -x /usr/local/bin/mono ]; then
	printf '%s\n' 'Install the x86_64 Homebrew runtime dependencies under /usr/local first; see README.md.' >&2
	exit 1
fi
lipo -verify_arch x86_64 /usr/local/bin/mono
make -C ExternalProjects/LibBizHash
sh Dist/build-macos-libgdiplus.sh
if [ ! -f ExternalProjects/SDL2/SDL/CMakeLists.txt ]; then
	printf '%s\n' 'Run git submodule update --init ExternalProjects/SDL2/SDL first.' >&2
	exit 1
fi
# SDL2 must match our native API; Homebrew may supply an SDL3 compatibility shim.
mkdir -p output/dll
if [ -L output/dll/libSDL2.dylib ]; then unlink output/dll/libSDL2.dylib; fi
cmake -S ExternalProjects/SDL2 -B waterbox/waterboxhost/target/macos-sdl2 \
	-DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES=x86_64 \
	-DCMAKE_OSX_DEPLOYMENT_TARGET=11.0 -DCMAKE_PREFIX_PATH=/usr/local \
	-DPKG_CONFIG_EXECUTABLE=/usr/local/bin/pkg-config
cmake --build waterbox/waterboxhost/target/macos-sdl2 --parallel 4
sh waterbox/waterboxhost/build-release-macos.sh
# Remove legacy launch aliases before MSBuild copies assets over them.
for lib in libgdiplus.0.dylib libSDL2.so; do
	if [ -L "output/dll/$lib" ]; then unlink "output/dll/$lib"; fi
done
sh Dist/BuildRelease.sh "$@"
# Refresh the checked-in ImGui native asset when the NuGet version changes.
imgui_package=$(dotnet msbuild src/BizHawk.Bizware.Graphics/BizHawk.Bizware.Graphics.csproj -nologo -getProperty:PkgImGui_NET)
cp "$imgui_package/runtimes/osx/native/libcimgui.dylib" Assets/dll/libcimgui.dylib
sh Dist/stage-macos-dylibs.sh
printf '%s\n' 'Build complete. Start XQuartz, then run: sh output/EmuHawkMono.sh'
