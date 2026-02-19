#!/bin/sh

case "$0" in
	*".sh");;
	*"/bin/"*"sh")
		# Very bad way to detect /path/to/shell
		echo "I don't know where I am! Could you run me as \"/path/to/EmuHawkMono.sh\"?" >& 2
		# stupid bash workaround
		# shellcheck disable=SC2317
		return 1 2> /dev/null || exit 1;;
	*"/"*);;
	*)
		echo "I don't know where I am! Could you run me as \"/path/to/EmuHawkMono.sh\"?" >& 2
		# shellcheck disable=SC2317
		return 1 2> /dev/null || exit 1
esac
if ! cd "$(dirname -- "$(realpath -- "$0")")"; then
	echo "Can't navigate to \$0's path?" >& 2
	exit 1
fi

libpath=""
if [ "$(command -v lsb_release)" ]; then
	# shellcheck disable=SC2018,SC2019
	case "$(lsb_release -i | head -n1 | cut -c17- | tr A-Z a-z)" in
		"arch"|"artix"|"manjarolinux") libpath="/usr/lib";;
		"fedora"|"gentoo"|"nobaralinux"|"opensuse") libpath="/usr/lib64";;
		"nixos") libpath="/usr/lib"; echo "Running on NixOS? Why aren't you using the Nix expr?" >& 2;;
		"debian"|"linuxmint"|"pop"|"ubuntu") libpath="/usr/lib/x86_64-linux-gnu";;
	esac
else
	echo "Distro does not provide LSB release info API! (You've met with a terrible fate, haven't you?)" >& 2
fi
if [ -z "$libpath" ]; then
	echo "Unknown distro, assuming system-wide libraries are in /usr/lib..." >& 2
	libpath="/usr/lib"
fi

export GTK_DATA_PREFIX=""
export LD_LIBRARY_PATH="$PWD/dll:$PWD:$libpath"
export MONO_CRASH_NOFILE=1
export MONO_WINFORMS_XIM_STYLE=disabled # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9

if [ "$1" = "--mono-no-redirect" ]; then
	echo "(passing --mono-no-redirect is no longer necessary)" >& 2
	shift
fi
# shellcheck disable=SC2009
if (ps -C "mono" -o "cmd" --no-headers | grep -Fq "EmuHawk.exe"); then
	echo "(it seems EmuHawk is already running, NOT capturing output)" >& 2
	mono EmuHawk.exe "$@"
	exit "$?"
fi

o="$(mktemp -u)"
e="$(mktemp -u)"
mkfifo "$o" "$e"
echo "(capturing output in $PWD/EmuHawkMono_last*.txt)" >& 2
tee EmuHawkMono_laststdout.txt < "$o" &
tee EmuHawkMono_laststderr.txt < "$e" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >& 2 &
mono EmuHawk.exe "$@" > "$o" 2> "$e"
exit "$?"
