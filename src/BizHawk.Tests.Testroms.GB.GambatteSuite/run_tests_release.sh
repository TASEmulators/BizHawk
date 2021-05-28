#!/bin/sh
cd "$(dirname "$(realpath "$0")")" && ../BizHawk.Tests.Testroms.GB/.run_tests_with_configuration.sh "Release" "$@"
