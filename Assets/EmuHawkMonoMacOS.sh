#!/bin/sh
# Launcher for EmuHawk on macOS via Mono (x86_64, under Rosetta 2 on Apple Silicon).
# Mono's System.Windows.Forms is X11-based, so an X server (XQuartz) must be running.
# See also EmuHawkMono.sh (the Linux equivalent this is modelled on).
cd "$(dirname "$(realpath "$0")")"

# Native libs ship in ./dll; also let the loader find Homebrew-provided deps
# (SDL2, OpenAL, Lua, zstd, libgdiplus, ...) under /usr/local, and XQuartz's X11
# libs under /opt/X11/lib (Mono's WinForms dlopens libX11 etc. from there).
export DYLD_LIBRARY_PATH="$PWD/dll:$PWD:/usr/local/lib:/opt/X11/lib${DYLD_LIBRARY_PATH:+:$DYLD_LIBRARY_PATH}"
export DYLD_FALLBACK_LIBRARY_PATH="$PWD/dll:$PWD:/usr/local/lib:/opt/X11/lib:/usr/lib${DYLD_FALLBACK_LIBRARY_PATH:+:$DYLD_FALLBACK_LIBRARY_PATH}"

export MONO_CRASH_NOFILE=1
export MONO_WINFORMS_XIM_STYLE=disabled # see https://bugzilla.xamarin.com/show_bug.cgi?id=28047#c9
# Mono's default macOS WinForms backend is the Carbon driver, which is unported to 64-bit
# ("very few parts of Windows.Forms will work properly, or at all"). Force the X11 driver,
# which renders through XQuartz.
export MONO_MWF_MAC_FORCE_X11=1

# XQuartz: ensure DISPLAY points at a running X server.
if [ -z "$DISPLAY" ]; then
	export DISPLAY=:0
fi
if ! command -v Xquartz >/dev/null 2>&1 && [ ! -d "/Applications/Utilities/XQuartz.app" ] && [ ! -d "/opt/X11" ]; then
	printf "%s\n" "XQuartz does not appear to be installed; Mono WinForms needs an X11 server. Install with: brew install --cask xquartz" >&2
fi

# Prefer the x86_64 Mono from Homebrew at /usr/local. Invoke it DIRECTLY (not via `arch`):
# /usr/bin/arch is SIP-protected and strips DYLD_* from the environment, which breaks our
# library paths. An x86_64-only binary auto-runs under Rosetta 2 when exec'd directly.
mono_bin="mono"
if [ -x "/usr/local/bin/mono" ]; then
	mono_bin="/usr/local/bin/mono"
fi

# Force the GdiPlus (software) display method: XQuartz's OpenGL is Apple's legacy GLX bridge,
# which is only GL 2.1 and crashes (CGLSetCurrentContext) under Rosetta. Without this, EmuHawk
# aborts trying to init OpenGL. GdiPlus is full-speed here since the host only blits frames.
set -- --gdi "$@"

if (ps -A -o "command" | grep -F "EmuHawk.exe" | grep -Fvq "grep"); then
	printf "(it seems EmuHawk is already running, NOT capturing output)\n" >&2
	exec $mono_bin EmuHawk.exe "$@"
fi
o="$(mktemp -u)"
e="$(mktemp -u)"
mkfifo "$o" "$e"
printf "(capturing output in %s/EmuHawkMono_last*.txt)\n" "$PWD" >&2
tee EmuHawkMono_laststdout.txt <"$o" &
tee EmuHawkMono_laststderr.txt <"$e" | sed "s/.*/$(tput setaf 1)&$(tput sgr0)/" >&2 &
exec $mono_bin EmuHawk.exe "$@" >"$o" 2>"$e"
