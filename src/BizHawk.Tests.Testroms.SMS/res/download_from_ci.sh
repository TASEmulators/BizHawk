#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
exec ../../BizHawk.Tests.Testroms/.download_from_ci.sh \
	SMSmemtest \
	"$@"
