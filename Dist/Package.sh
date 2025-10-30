#!/bin/sh
set -e
targetDir="packaged_output"
cd "$(dirname "$0")/.."
rm -fr "$targetDir" && mkdir -p "$targetDir"
find "output" -type f \( -name '.keep' -o -wholename "output/EmuHawk.exe" -o -wholename "output/DiscoHawk.exe" -o -wholename "output/*.config" -o -wholename "output/defctrl.json" -o -wholename "output/EmuHawkMono.sh" -o -wholename "output/dll/*" -o -wholename "output/Shaders/*" -o -wholename "output/gamedb/*" -o -wholename "output/NES/Palettes/*" -o -wholename "output/Lua/*" -o -wholename "output/Gameboy/Palettes/*" -o -wholename "output/overlay/*" \) \
	-not -name "*.pdb" -not -name "*.lib" -not -name "*.pgd" -not -name "*.ipdb" -not -name "*.iobj" -not -name "*.exp" -not -name "*.ilk" \
	-not -wholename "output/dll/*.xml" -not -wholename "output/dll/*.deps.json" \
	-exec install -D -m644 "{}" "packaged_{}" \;
cd "$targetDir"
rm -f */.keep
if [ "$1" = "windows-x64" ]; then
	rm -f "EmuHawkMono.sh"
	cd "dll"
	rm -f *.so
else
	find . -type f -name "*.sh" -exec chmod +x {} \; # installed with -m644 but needs to be 755
	cd "dll"
	rm -f "chd_capi.dll" "cimgui.dll" "e_sqlite3.dll" "lua54.dll" "SDL2.dll" \
		"mupen64plus-audio-bkm.dll" "mupen64plus-input-bkm.dll" "mupen64plus-rsp-cxd4-sse2.dll" "mupen64plus-rsp-hle.dll" "mupen64plus-video-angrylion-rdp.dll" "mupen64plus-video-glide64.dll" "mupen64plus-video-glide64mk2.dll" "mupen64plus-video-GLideN64.dll" "mupen64plus-video-rice.dll" "mupen64plus.dll" "octoshock.dll" \
		"bizlynx.dll" "bizswan.dll" "blip_buf.dll" "libbizhash.dll" "libdarm.dll" "libemu83.dll" "encore.dll" "libfwunpack.dll" "libgambatte.dll" "libLibretroBridge.dll" "libquicknes.dll" "librcheevos.dll" "libsameboy.dll" "mgba.dll" "MSXHawk.dll" "waterboxhost.dll"
	if [ "$1" = "linux-arm64" ]; then
		cp -ft . ../../Dist/arm64/*
	fi
fi
