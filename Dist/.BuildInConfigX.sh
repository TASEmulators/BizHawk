#!/bin/sh
set -e
if [ ! -e "BizHawk.sln" ]; then
	printf "wrong cwd (ran manually)? exiting\n"
	exit 1
fi
config="$1"
shift
Dist/.InvokeCLIOnMainSln.sh "build" "$config" "$@"
# macOS: the build overwrites output/dll and doesn't create the native-library symlinks
# (Homebrew deps + Linux-soname aliases), so (re)stage them to keep the output runnable.
if [ "$(uname -s)" = "Darwin" ] && [ -d output/dll ]; then
	Dist/stage-macos-dylibs.sh output/dll
fi
