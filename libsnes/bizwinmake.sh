cd bsnes
export BIZWINCFLAGS="-I. -O3 -masm=intel -DLIBCO_IMPORT -DLIBCO_MSVC"
export TARGET_LIBSNES_LIBDEPS="-L ../libco_msvc_win32/release/ -llibco_msvc_win32"
profile=compatibility platform=win target=libsnes make -e -j
cd ..
cp bsnes/out/snes.dll ../BizHawk.MultiClient/output