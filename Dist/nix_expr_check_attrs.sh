#!/bin/sh
set -e
cd "$(dirname "$0")/.."

printf "Checking all documented attrs are actually exposed (expect \`missing = [];\`)...\n"
nix-instantiate --eval --strict -E 'import Dist/exposed-attr-checker.nix {}'
printf "\n"

attrNamesToCheck='discohawk-latest emuhawk-2_9_1 emuhawk-2_9 emuhawk-2_8 emuhawk-2_7 emuhawk-2_6_3 emuhawk-2_6_2 emuhawk-2_6_1 emuhawk-2_6'
attrNamesToCheckBinOnly='emuhawk-2_5_2 emuhawk-2_5_1 emuhawk-2_5 emuhawk-2_4_2 emuhawk-2_4_1 emuhawk-2_4 emuhawk-2_3_3 emuhawk-2_3_2'
# not checking `forNixOS = false` since it's just the same wrapped with nixGL
#commonBuildFlags='--pure --arg doCheck true' # not working :(
commonBuildFlags='--pure'
b() {
	printf "%s\n" "nix-build $commonBuildFlags $*"
	nix-build $commonBuildFlags "$@"
	printf "\n"
}
printf "Checking Nix expr is still functional (no guarantees EmuHawk will run). For best results, uninstall (nix-env -e <package>) and clear cached builds (nix-env --delete-generations +1; nix-store --gc).\nTHIS MAY TAKE A WHILE.\n"
if [ "$1" = "skipmsbuild" ]; then
	printf "Skipping building from source in CWD and building old releases from source.\n"
else
	if [ "$1" = "skipcwd" ]; then
		printf "Skipping building from source in CWD.\n"
	else
		printf "Starting with the slowest first, building from source in CWD (pass 'skipcwd' as first arg to skip).\n"
		b -A emuhawk --argstr buildConfig 'Debug'
#		b -A emuhawk --argstr buildConfig 'Debug' --check # definitely not
		b -A emuhawk
#		b -A emuhawk --check # definitely not
#		b -A discohawk # nah
#		b -A discohawk --check # definitely not
	fi
	printf "Building old releases from source (pass 'skipmsbuild' as first arg to skip).\n"
	for a in $attrNamesToCheck; do
		b -A "$a"
#		b -A "$a.assemblies" --check # not reproducible yet :(
	done
fi
printf "Packaging releases.\n"
for a in $attrNamesToCheck $attrNamesToCheckBinOnly; do
	b -A "$a-bin"
	b -A "$a-bin.assemblies" --check
done
