#!/bin/sh
cd "$(dirname "$0")/.." && (cp -r Assets/* output; cp src/BizHawk.Client.EmuHawk/bin/*/* output)
