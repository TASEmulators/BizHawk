#!/bin/sh
cd "$(dirname "$(realpath "$0")")"
case "$(uname -s)" in
Darwin)
	# macOS (x86_64; natively on Intel or via Rosetta 2 on Apple silicon). Mono's WinForms is
	# X11-based, so XQuartz must be running; see the macOS section in the readme. The display
	# method defaults to GdiPlus on macOS (in Config.cs) since XQuartz's OpenGL is unusable.
	#
	# BizHawk's P/Invokes and Mono load some libraries by Linux/soname-style names, and the
	# Homebrew/XQuartz deps have their own versioned names. We can't ship those symlinks (they
	# point at the user's Homebrew install), so (re)create them in ./dll at launch, similar to
	# the NixOS launch script. They all resolve to ONE Homebrew libX11 so that the X11 clients
	# (WinForms, cairo/libgdiplus, SDL) agree.
	dll="$PWD/dll"
	ln_dep() { [ -e "$1" ] && ln -sf "$1" "$dll/$2"; } # silently skip optional libs that aren't installed
	ln_dep /usr/local/opt/lua@5.4/lib/liblua5.4.dylib        liblua54.dylib
	ln_dep /usr/local/opt/openal-soft/lib/libopenal.dylib    libopenal.dylib
	ln_dep /usr/local/opt/openal-soft/lib/libopenal.1.dylib  libopenal.1.dylib
	ln_dep /usr/local/opt/zstd/lib/libzstd.1.dylib           libzstd.so.1
	ln_dep /usr/local/opt/sqlite/lib/libsqlite3.dylib        libe_sqlite3.dylib
	ln_dep /usr/local/opt/libx11/lib/libX11.6.dylib          libX11.6.dylib
	ln_dep /usr/local/opt/libxext/lib/libXext.6.dylib        libXext.6.dylib
	ln_dep /usr/local/opt/libxrender/lib/libXrender.1.dylib  libXrender.1.dylib
	ln_dep /usr/local/opt/libxcursor/lib/libXcursor.1.dylib  libXcursor.1.dylib
	ln_dep /usr/local/opt/libxinerama/lib/libXinerama.1.dylib libXinerama.1.dylib
	ln_dep /usr/local/opt/libxi/lib/libXi.6.dylib            libXi.6.dylib
	ln_dep /usr/local/opt/libxrandr/lib/libXrandr.2.dylib    libXrandr.2.dylib
	ln_dep /usr/local/opt/libxtst/lib/libXtst.6.dylib        libXtst.6.dylib
	ln_dep /usr/local/opt/libxfixes/lib/libXfixes.3.dylib    libXfixes.3.dylib
	ln_dep /usr/local/opt/libxscrnsaver/lib/libXss.1.dylib   libXss.1.dylib
	ln_dep /usr/local/opt/libxau/lib/libXau.6.dylib          libXau.6.dylib
	ln_dep /usr/local/opt/libxdmcp/lib/libXdmcp.6.dylib      libXdmcp.6.dylib
	ln_dep /usr/local/opt/libxcb/lib/libxcb.1.dylib          libxcb.1.dylib
	# soname/unversioned aliases hardcoded by BizHawk's P/Invokes and Mono (resolve within ./dll)
	[ -e "$dll/libX11.6.dylib" ] && { ln -sf libX11.6.dylib "$dll/libX11.dylib"; ln -sf libX11.6.dylib "$dll/libX11.so.6"; }
	[ -e "$dll/libXi.6.dylib" ] && ln -sf libXi.6.dylib "$dll/libXi.so.6"
	[ -e "$dll/libXfixes.3.dylib" ] && ln -sf libXfixes.3.dylib "$dll/libXfixes.so.3"
	[ -e "$dll/libgdiplus.0.dylib" ] && ln -sf libgdiplus.0.dylib "$dll/libgdiplus.dylib"
	# Let the loader find ./dll, the Homebrew deps under /usr/local, and XQuartz under /opt/X11.
	export DYLD_LIBRARY_PATH="$dll:$PWD:/usr/local/lib:/opt/X11/lib${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
	export DYLD_FALLBACK_LIBRARY_PATH="$dll:$PWD:/usr/local/lib:/opt/X11/lib:/usr/lib${DYLD_FALLBACK_LIBRARY_PATH:+:$DYLD_FALLBACK_LIBRARY_PATH}"
	# Prefer the x86_64 Homebrew Mono via PATH (so a Nix Mono etc. is still respected). Don't run
	# Mono through `arch` — it's SIP-protected and strips the DYLD_* vars.
	[ -d /usr/local/bin ] && export PATH="/usr/local/bin:$PATH"
	export MONO_MWF_MAC_FORCE_X11=1 # the default macOS WinForms driver (Carbon) is unported to 64-bit
	[ -z "$DISPLAY" ] && export DISPLAY=:0
	;;
*)
	# GNU+Linux (and other Unix)
	libpath=""
	if [ "$(command -v lsb_release)" ]; then
		case "$(lsb_release -i | head -n1 | cut -c17- | tr A-Z a-z)" in
			"arch"|"artix"|"manjarolinux") libpath="/usr/lib";;
			"fedora"|"gentoo"|"nobaralinux"|"opensuse") libpath="/usr/lib64";;
			"nixos") libpath="/usr/lib"; printf "Running on NixOS? Why aren't you using the Nix expr?\n" >&2;;
			"debian"|"linuxmint"|"pop"|"ubuntu") libpath="/usr/lib/x86_64-linux-gnu";;
		esac
	else
		printf "Distro does not provide LSB release info API! (You've met with a terrible fate, haven't you?)\n" >&2
	fi
	if [ -z "$libpath" ]; then
		printf "%s\n" "Unknown distro, assuming system-wide libraries are in /usr/lib..." >&2
		libpath="/usr/lib"
	fi
	export LD_LIBRARY_PATH="$PWD/dll:$PWD:$libpath"
	;;
esac
export MONO_CRASH_NOFILE=1
export MONO_WINFORMS_XIM_STYLE=disabled # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9
if (ps -A -o command= | grep -F "EmuHawk.exe" | grep -Fvq "grep"); then
	printf "(it seems EmuHawk is already running, NOT capturing output)\n" >&2
	exec mono EmuHawk.exe "$@"
fi
o="$(mktemp -u)"
e="$(mktemp -u)"
mkfifo "$o" "$e"
printf "(capturing output in %s/EmuHawkMono_last*.txt)\n" "$PWD" >&2
tee EmuHawkMono_laststdout.txt <"$o" &
tee EmuHawkMono_laststderr.txt <"$e" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >&2 &
exec mono EmuHawk.exe "$@" >"$o" 2>"$e"
