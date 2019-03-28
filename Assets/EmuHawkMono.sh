#!/bin/sh
cd "$(dirname "$0")"
if [ "$(ps -C "mono" -o "cmd" --no-headers | grep "EmuHawk.exe")" ]; then
	echo "EmuHawk is already running, exiting..."
	exit 0
fi
libpath=""
if [ "$(command -v lsb_release)" ]; then
	case "$(lsb_release -i | cut -c17- | tr -d "\n")" in
		"Arch"|"ManjaroLinux") libpath="/usr/lib/wine";;
		"Debian"|"Ubuntu"|"LinuxMint") libpath="/usr/lib/x86_64-linux-gnu/wine";;
	esac
fi
if [ -z "$libpath" ]; then
	printf "%s\n" "Unknown distro, assuming WINE library location is /usr/lib/wine..."
	libpath="/usr/lib/wine"
fi
LD_LIBRARY_PATH="$libpath" mono ./EmuHawk.exe
