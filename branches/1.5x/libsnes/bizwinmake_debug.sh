#this is for using the dll libco, and so the threads implementation can be used, for easier debugging
cd bsnes
mkdir obj
mkdir out
export BIZWINCFLAGS="-I. -O3 -masm=intel -DLIBCO_IMPORT -DLIBCO_MSVC -static-libgcc -static-libstdc++"
#for gdb debugging
#export BIZWINCFLAGS="-I. -O0 -g -masm=intel -DLIBCO_IMPORT -DLIBCO_MSVC -static-libgcc -static-libstdc++"
export TARGET_LIBSNES_LIBDEPS="-L ../libco_msvc_win32/release/ -llibco_msvc_win32  -static-libgcc -static-libstdc++"
profile=compatibility platform=win target=libsnes make -e -j 4
cd ..
cp bsnes/out/snes.dll ../BizHawk.MultiClient/output/dll/libsneshawk.dll
