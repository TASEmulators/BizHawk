#!/bin/sh
cd "$(dirname "$0")/.."
cp -f -r Assets/* output || printf ""
