#!/bin/sh
cd "$(dirname "$0")/.."
cp -f -r Assets/* output || printf ""
cd src/BizHawk.Client.EmuHawk/bin && for d in *; do
	cp -f $d/* ../../../output && cd ../../../output && mv BizHawk.Client.EmuHawk.exe EmuHawk.exe && mv BizHawk.Client.EmuHawk.exe.config EmuHawk.exe.config && exit 0
done
