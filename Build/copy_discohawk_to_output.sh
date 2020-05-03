#!/bin/sh
cd "$(dirname "$0")/.."
cd src/BizHawk.Client.DiscoHawk/bin && for d in *; do
	cp -f $d/* ../../../output && cd ../../../output && mv BizHawk.Client.DiscoHawk.exe DiscoHawk.exe && mv BizHawk.Client.DiscoHawk.exe.config DiscoHawk.exe.config && exit 0
done
