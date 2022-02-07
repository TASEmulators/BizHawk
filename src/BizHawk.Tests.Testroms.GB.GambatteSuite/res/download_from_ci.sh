#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
../../BizHawk.Tests.Testroms.GB/.download_from_ci.sh Gambatte-testroms
