#!/bin/sh
set -e
if [ -e "hawk-src-overlay" ]; then
	printf "found dir hawk-src-overlay in cwd -- if running manually, you missed a step\n"
	exit 1
fi
cp -a "../hawk-src-overlay" -T "."
