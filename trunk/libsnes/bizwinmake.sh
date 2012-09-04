cd bsnes
profile=compatibility platform=win target=libsnes make -e -j
cd ..
cp bsnes/out/snes.dll ../BizHawk.MultiClient/output