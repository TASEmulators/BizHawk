#!/bin/sh
cd "$(dirname "$0")/.." && rm -r src/*/bin src/*/obj test_output output && git checkout -- output
