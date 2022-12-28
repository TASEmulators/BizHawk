#!/bin/sh
cd "$(dirname "$0")/.." || exit $?

p=Dist/packages #TODO tempfile?
dotnet restore --packages="$p" BizHawk.sln "$@" >/dev/null
ec=$?
if [ $ec -ne 0 ]; then
	rm -fr "$p"
	echo "NuGet restore failed"
	exit $ec
fi

f=Dist/deps-new.nix # needs to be valid Nix path, so no spaces
nuget-to-nix "$p" >"$f"
ec=$?
rm -fr "$p"
if [ $ec -ne 0 ]; then
	rm "$f"
	echo "Nix codegen failed"
	exit $ec
fi

sed -i -e 's/{ fetchNuGet }: //' -e 's/fetchNuGet //g' -e 's/pname =/name =/g' "$f"
nix-instantiate -E "let f = (import <nixpkgs> {}).lib.subtractLists; curr = (import Dist/deps-base.nix).hawk_2_8_1; new = import $f; in builtins.foldl' (a: b: builtins.trace b a) [] [ \"removed:\" (f new curr) \"added:\" (f curr new) ]"
rm "$f"
