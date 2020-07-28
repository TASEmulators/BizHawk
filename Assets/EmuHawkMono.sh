#!/bin/sh
cd "$(dirname "$(realpath "$0")")"
if [ "$(ps -C "mono" -o "cmd" --no-headers | grep "EmuHawk.exe")" ]; then
	echo "EmuHawk is already running, exiting..."
	exit 0
fi
libpath=""
winepath=""
if [ "$(command -v lsb_release)" ]; then
	case "$(lsb_release -i | cut -c17- | tr -d "\n")" in
		"Arch"|"ManjaroLinux") libpath="/usr/lib";;
		"Debian"|"LinuxMint"|"Ubuntu") libpath="/usr/lib/x86_64-linux-gnu"; export MONO_WINFORMS_XIM_STYLE=disabled;; # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9
	esac
else
	printf "Distro does not provide LSB release info API! (You've met with a terrible fate, haven't you?)\n"
fi
if [ -z "$libpath" ]; then
	printf "%s\n" "Unknown distro, assuming system-wide libraries are in /usr/lib..."
	libpath="/usr/lib"
fi
if [ -z "$winepath" ]; then winepath="$libpath/wine"; fi
export LD_LIBRARY_PATH="$PWD/dll:$PWD:$winepath:$libpath"
if [ "$1" = "--mono-no-redirect" ]; then
	shift
	printf "(received --mono-no-redirect, stdout was not captured)\n" >EmuHawkMono_laststdout.txt
	mono ./EmuHawk.exe "$@"
else
	mono ./EmuHawk.exe "$@" >EmuHawkMono_laststdout.txt
fi
