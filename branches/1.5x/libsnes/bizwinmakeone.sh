#!/bin/sh -x

#this is for using the fast libco, hand-coded by byuu. the dll isnt used, and so the threads implementation wont be useful, so it cant be debugged easily
cd bsnes
mkdir obj
mkdir out

if [ "$1" == "64" ]; then
	export cflags64=-m64 ;
	export compiler=i686-w64-mingw32-c++.exe
fi

if [ "$1" == "32" ]; then
	export cflags32=-llibco_msvc_win32 ;
fi

#debug:
#export BIZWINCFLAGS="-I. -O0 -g -masm=intel -DLIBCO_IMPORT -DLIBCO_MSVC -static-libgcc -static-libstdc++"

#not debug
export BIZWINCFLAGS="-I. -O3 -masm=intel -static-libgcc -static-libstdc++ ${cflags64}"

export TARGET_LIBSNES_LIBDEPS="-L ../libco_msvc_win32/release/ -static  -static-libgcc -static-libstdc++ ${cflags64} ${cflags32} -mwindows"
export profile=$2
export bits=$1

platform=win target=libsnes make -e -j 4
cd ..

filename=libsneshawk-${bits}-${profile}.exe
targetdir=../BizHawk.MultiClient/output/dll
targetpath=${targetdir}/${filename}
cp bsnes/out/${filename} ${targetdir}
if [ "$3" == "compress" ]; then
	upx -9 ${targetpath} ;
fi