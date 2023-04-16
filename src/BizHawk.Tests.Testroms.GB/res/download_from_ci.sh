#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
../../BizHawk.Tests.Testroms.GB/.download_from_ci.sh BullyGB cgb-acid-hell cgb-acid2 dmg-acid2 mealybug-tearoom-tests rtc3test
