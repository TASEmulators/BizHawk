#!/bin/sh
cd "$(dirname "$(realpath "$0")")"
libpath=""
if [ "$(command -v lsb_release)" ]; then
	case "$(lsb_release -i | head -n1 | cut -c17- | tr A-Z a-z)" in
		"arch"|"artix"|"manjarolinux") libpath="/usr/lib";;
		"fedora"|"gentoo"|"nobaralinux"|"opensuse") libpath="/usr/lib64";;
		"nixos") libpath="/usr/lib"; printf "Running on NixOS? Why aren't you using the Nix expr?\n";;
		"debian"|"linuxmint"|"pop"|"ubuntu") libpath="/usr/lib/x86_64-linux-gnu";;
	esac
else
	printf "Distro does not provide LSB release info API! (You've met with a terrible fate, haven't you?)\n"
fi
if [ -z "$libpath" ]; then
	printf "%s\n" "Unknown distro, assuming system-wide libraries are in /usr/lib..."
	libpath="/usr/lib"
fi
export LD_LIBRARY_PATH="$PWD/dll:$PWD:$libpath"
export MONO_CRASH_NOFILE=1
export MONO_WINFORMS_XIM_STYLE=disabled # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9
if [ "$1" = "--mono-no-redirect" ]; then
#	printf "(passing --mono-no-redirect is no longer necessary)\n" #TODO uncomment later
	shift
fi
if (ps -C "mono" -o "cmd" --no-headers | grep -Fq "EmuHawk.exe"); then
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
