#this is for using the fast libco, hand-coded by byuu. the dll isnt used, and so the threads implementation wont be useful, so it cant be debugged easily
cd bsnes
mkdir obj
mkdir out
export BIZWINCFLAGS="-I. -O3 -masm=intel -static-libgcc -static-libstdc++"
export TARGET_LIBSNES_LIBDEPS="-L ../libco_msvc_win32/release/ -llibco_msvc_win32  -static-libgcc -static-libstdc++"
profile=compatibility platform=win target=libsnes make -e -j 4
cd ..
cp bsnes/out/snes.dll ../BizHawk.MultiClient/output/dll/libsneshawk.dll
