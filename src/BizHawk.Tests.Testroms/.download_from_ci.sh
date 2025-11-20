#!/bin/sh
set -e
for j in "$@"; do
	if [ -e "${j}_artifact" ]; then
		printf "Using existing copy of %s\n" "$j"
	else
		curl -L -o "$j.zip" "https://gitlab.com/tasbot/libre-roms-ci/-/jobs/artifacts/master/download?job=$j"
		unzip "$j.zip" >/dev/null
		find "${j}_artifact" -type d -exec chmod 755 "{}" \;
		find "${j}_artifact" -type f -exec chmod 644 "{}" \;
		rm "$j.zip"
		printf "Downloaded and extracted %s CI artifact\n" "$j"
	fi
done
exit 0

# TODO finish this and put it in a separate script
nixVersion="$(nix --version 2>&1)"
if [ $? -eq 0 ]; then
	for a in blargg-gb-tests; do
		printf "(TODO: nix-build %s)\n" "$a"
	done
fi
