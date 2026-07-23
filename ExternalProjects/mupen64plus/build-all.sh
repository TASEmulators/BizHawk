#!/bin/sh
set -e
cd "$(dirname "$0")"

install_to_bizhawk () {
	if [ "$OS" = "Windows_NT" ]; then
		path=$1$2.dll
		target=$2.dll
	else
		path=$1$2.so
		target=lib$2.so
	fi
	strip -p "$path"
	cp "$path" ../../Assets/dll/$target
	if [ -d ../../output/dll ]; then
		cp "$path" ../../output/dll/$target
	fi
}

mkdir -p build

rm -rf mupen64plus-core/projects/unix/_obj
# do we want ACCURATE_FPU=1 here?
make -C mupen64plus-core/projects/unix VULKAN=0 NEW_DYNAREC=1 OSD=0 ACCURATE_FPU=0 KEYBINDINGS=0 DEBUGGER=1 all -j4
if [ "$OS" != "Windows_NT" ]; then
	mv mupen64plus-core/projects/unix/libmupen64plus.so.2.0.0 build/mupen64plus.so
	patchelf --replace-needed libSDL2-2.0.so.0 libSDL2.so build/mupen64plus.so
else
	mv mupen64plus-core/projects/unix/mupen64plus.dll build/
fi
install_to_bizhawk build/ mupen64plus
cp mupen64plus-core/data/mupen64plus.ini ../../Assets/gamedb/

make -C mupen64plus-audio-bkm install

make -C mupen64plus-input-bkm install

rm -rf mupen64plus-rsp-cxd4/projects/unix/_obj-sse2
make -C mupen64plus-rsp-cxd4/projects/unix all -j4
install_to_bizhawk mupen64plus-rsp-cxd4/projects/unix/ mupen64plus-rsp-cxd4-sse2

rm -rf mupen64plus-rsp-hle/projects/unix/_obj
make -C mupen64plus-rsp-hle/projects/unix all -j4
install_to_bizhawk mupen64plus-rsp-hle/projects/unix/ mupen64plus-rsp-hle

rm -rf build/rsp-parallel && mkdir -p build/rsp-parallel
cmake -S mupen64plus-rsp-parallel -B build/rsp-parallel -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/rsp-parallel
install_to_bizhawk build/rsp-parallel/ mupen64plus-rsp-parallel

rm -rf build/video-angrylion-plus && mkdir -p build/video-angrylion-plus
cmake -S mupen64plus-video-angrylion-plus -B build/video-angrylion-plus -G Ninja -DCMAKE_BUILD_TYPE=MinSizeRel
cmake --build build/video-angrylion-plus
install_to_bizhawk build/video-angrylion-plus/ mupen64plus-video-angrylion-plus

rm -rf build/video-GLideN64 && mkdir -p build/video-GLideN64
cmake -S mupen64plus-video-GLideN64/src -B build/video-GLideN64 -G Ninja -DCMAKE_CXX_FLAGS="-fno-gnu-unique" -DCMAKE_BUILD_TYPE=Release -DVEC4_OPT=On -DCRC_OPT=On -DMUPENPLUSAPI=On -DNO_OSD=On
cmake --build build/video-GLideN64
install_to_bizhawk build/video-GLideN64/plugin/Release/ mupen64plus-video-GLideN64

rm -rf build/video-parallel && mkdir -p build/video-parallel
cmake -S mupen64plus-video-parallel -B build/video-parallel -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build/video-parallel
install_to_bizhawk build/video-parallel/ mupen64plus-video-parallel
