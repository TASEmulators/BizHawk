#!/bin/sh
cd "$(dirname "$(realpath "$0")")"
case "$(uname -s)" in
Darwin)
	# macOS (x86_64, natively on Intel or via Rosetta 2 on Apple silicon). Mono's WinForms is
	# X11-based, so XQuartz must be running; see the macOS section in the readme.
	# Native libs are in ./dll; also let the loader find the Homebrew deps under /usr/local and
	# XQuartz's X11 libs under /opt/X11/lib.
	export DYLD_LIBRARY_PATH="$PWD/dll:$PWD:/usr/local/lib:/opt/X11/lib${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
	export DYLD_FALLBACK_LIBRARY_PATH="$PWD/dll:$PWD:/usr/local/lib:/opt/X11/lib:/usr/lib${DYLD_FALLBACK_LIBRARY_PATH:+:$DYLD_FALLBACK_LIBRARY_PATH}"
	# Prefer the x86_64 Homebrew Mono by putting it on PATH (PATH is then respected, so e.g. a Nix
	# Mono works too). Don't invoke Mono via `arch`: it's SIP-protected and strips the DYLD_* vars.
	[ -d /usr/local/bin ] && export PATH="/usr/local/bin:$PATH"
	export MONO_MWF_MAC_FORCE_X11=1 # the default macOS WinForms driver (Carbon) is unported to 64-bit
	[ -z "$DISPLAY" ] && export DISPLAY=:0
	# XQuartz's OpenGL is the legacy Apple GLX bridge (GL 2.1, and crashes under Rosetta), so use
	# the GdiPlus software display method.
	set -- --gdi "$@"
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
