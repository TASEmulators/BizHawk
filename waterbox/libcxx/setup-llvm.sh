#!/bin/sh
set -e
LLVM_TAG=llvmorg-15.0.7
LLVM_DIRS="clang/lib/Headers cmake compiler-rt libunwind libcxx libcxxabi"
LLVM_PATH=../llvm-project

if [ -d "$LLVM_PATH" ]; then
	if ! git -C "$LLVM_PATH" rev-parse $LLVM_TAG > /dev/null 2>&1; then
		rm -rf "$LLVM_PATH"
	fi
fi

if [ ! -d "$LLVM_PATH" ]; then
	git clone --filter=tree:0 --sparse https://github.com/llvm/llvm-project.git "$LLVM_PATH"
fi

cd "$LLVM_PATH"
if [ `git describe` != $LLVM_TAG ] || ! ls $LLVM_DIRS > /dev/null 2>&1; then
	git checkout $LLVM_TAG && git sparse-checkout set $LLVM_DIRS
fi
