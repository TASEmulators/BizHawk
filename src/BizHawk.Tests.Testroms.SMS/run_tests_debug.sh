#!/bin/sh
set -e
cd "$(dirname "$(realpath "$0")")"
exec ../BizHawk.Tests.Testroms/.run_tests_with_configuration.sh "Debug" "$@"
