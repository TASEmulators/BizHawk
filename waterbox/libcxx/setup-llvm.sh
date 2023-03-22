#!/bin/sh
set -e
LLVM_TAG=llvmorg-16.0.0
LLVM_PATH=../llvm-project

if [ -d "$LLVM_PATH" ]; then
	if ! git -C "$LLVM_PATH" rev-parse $LLVM_TAG > /dev/null 2>&1; then
		git submodule deinit -f "$LLVM_PATH"
		rm -rf "$LLVM_PATH"
	fi
fi

if [ ! -d "$LLVM_PATH" ]; then
	git submodule update --init --depth=1 --single-branch --progress "$LLVM_PATH"
fi
