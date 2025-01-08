#!/bin/sh
set -e
LLVM_TAG=llvmorg-18.1.8
LLVM_DIRS="cmake compiler-rt libunwind libcxx libcxxabi runtimes"
LLVM_PATH=../llvm-project

if [ ! -e "$LLVM_PATH/.git" ] || ! git -C "$LLVM_PATH" rev-parse $LLVM_TAG > /dev/null 2>&1; then
	rm -rf "$LLVM_PATH"
fi

if [ ! -e "$LLVM_PATH/.git" ]; then
	git clone --filter=tree:0 --sparse https://github.com/llvm/llvm-project.git "$LLVM_PATH"
fi

cd "$LLVM_PATH"
if [ `git describe` != $LLVM_TAG ] || ! ls $LLVM_DIRS > /dev/null 2>&1; then
	git checkout $LLVM_TAG && git sparse-checkout set $LLVM_DIRS
fi
