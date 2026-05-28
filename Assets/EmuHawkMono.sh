#!/bin/sh
set -e
case "$0" in
	*"/"*);;
	*)
		printf %s\\n "I don't know where I am! Could you run me as \"/path/to/EmuHawkMono.sh\"?" >&2
		# shellcheck disable=SC2317
		return 1 2>/dev/null || exit 1
esac
if ! cd "$(dirname -- "$(realpath -- "$0")")"; then
	printf "Can't navigate to \$0's path?\\n" >&2
	exit 1
fi
if [ "$1" = '--install' ]; then
	cat >"${XDG_DATA_HOME:-"$HOME/.local/share"}/applications/bizhawk.s"h <<-EOF
		[Desktop Entry]
		Categories=Game
		Comment=A multi-system emulator.
		Exec=./${0##*/}
		$(
		# "application/octet-stream" is provided since many ROMs don't actually have a
		# dedicated mimetype.
		)MimeType=application/octet-stream;vnd.nintendo.snes.rom;x-atari-2600-rom;x-atari-7800-rom;x-atari-lynx-rom;x-gameboy-color-rom;x-gameboy-rom;x-gamegear-rom;x-gba-rom;x-genesis-32x-rom;x-genesis-rom;x-msx-rom;x-n64-rom;x-neo-geo-pocket-color-rom;x-neo-geo-pocket-rom;x-nes-rom;x-nintendo-3ds-rom;x-nintendo-ds-rom;x-pc-engine-rom;x-saturn-rom;x-sega-cd-rom;x-sg1000-rom;x-sms-rom;x-virtual-boy-rom;
		Name=BizHawk
		Path=$PWD
		Type=Application
	EOF
	exit
fi
libpath=""
if [ "$(command -v lsb_release)" ]; then
	# shellcheck disable=SC2018,SC2019
	case "$(lsb_release -i | head -n1 | cut -c17- | tr A-Z a-z)" in
		"arch"|"artix"|"manjarolinux") libpath="/usr/lib";;
		"fedora"|"gentoo"|"nobaralinux"|"opensuse") libpath="/usr/lib64";;
		"nixos") libpath="/usr/lib"; printf %s\\n "Running on NixOS? Why aren't you using the Nix expr?" >&2;;
		"debian"|"linuxmint"|"pop"|"ubuntu") libpath="/usr/lib/x86_64-linux-gnu";;
	esac
else
	printf "Distro does not provide LSB release info API! (You've met with a terrible fate, haven't you?)\\n" >&2
fi
if [ -z "$libpath" ]; then
	printf "%s\\n" "Unknown distro, assuming system-wide libraries are in \"/usr/lib\"..." >&2
	libpath="/usr/lib"
fi
export GTK_DATA_PREFIX=""
export LD_LIBRARY_PATH="$PWD/dll:$PWD:$libpath"
export MONO_CRASH_NOFILE=1
export MONO_WINFORMS_XIM_STYLE=disabled # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9
if [ "$1" = "--mono-no-redirect" ]; then
	printf "(passing --mono-no-redirect is no longer necessary)\\n" >&2
	shift
fi
# shellcheck disable=SC2009
if (ps -C "mono" -o "cmd" --no-headers | grep -Fq "EmuHawk.exe"); then
	printf "(it seems EmuHawk is already running, NOT capturing output)\\n" >&2
	exec mono EmuHawk.exe "$@"
fi
outfile="$(mktemp -u)"
errfile="$(mktemp -u)"
mkfifo "$outfile" "$errfile"
printf "(capturing output in %s/EmuHawkMono_last*.txt)\\n" "$PWD" >&2
tee EmuHawkMono_laststdout.txt <"$outfile" &
tee EmuHawkMono_laststderr.txt <"$errfile" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >&2 &
exec mono EmuHawk.exe "$@" >"$outfile" 2>"$errfile"
