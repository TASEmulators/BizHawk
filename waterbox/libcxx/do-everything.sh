#!/bin/sh
set -e
./setup-llvm.sh

configure_and_install () {
	build_dir=build$1
	./configure-for-waterbox-phase-$1
	cmake --build $build_dir
	cmake --install $build_dir
	printf "completed phase $1\n"
}

configure_and_install -
configure_and_install 0
configure_and_install 1
configure_and_install 2
