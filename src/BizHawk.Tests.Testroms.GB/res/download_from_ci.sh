#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
exec ../../BizHawk.Tests.Testroms/.download_from_ci.sh \
	BullyGB \
	CasualPokePlayer-test-roms \
	cgb-acid-hell \
	cgb-acid2 \
	dmg-acid2 \
	Gambatte-testroms \
	mealybug-tearoom-tests \
	rtc3test \
	"$@"
