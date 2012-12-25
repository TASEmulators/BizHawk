#!/bin/sh -x

#this is for using the fast libco, hand-coded by byuu. the dll isnt used, and so the threads implementation wont be useful, so it cant be debugged easily
cd bsnes
mkdir obj
mkdir out

#debug:
#export BIZWINCFLAGS="-I. -O0 -g -masm=intel -DLIBCO_IMPORT -DLIBCO_MSVC -static-libgcc -static-libstdc++"

#not debug
export BIZWINCFLAGS="-I. -O3 -masm=intel -static-libgcc -static-libstdc++"

export TARGET_LIBSNES_LIBDEPS="-L ../libco_msvc_win32/release/ -llibco_msvc_win32  -static-libgcc -static-libstdc++ -Wl,--subsystem,windows"
export profile=$1

platform=win target=libsnes make -e -j 4
cd ..

filename=libsneshawk-${profile}.exe
targetdir=../BizHawk.MultiClient/output/dll
targetpath=${targetdir}/${filename}
cp bsnes/out/${filename} ${targetdir}
if [ "$2" == "compress" ]; then
	upx -9 ${targetpath} ;
fi