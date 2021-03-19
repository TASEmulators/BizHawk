# Waterbox toolchain - musl

Upstream is [musl libc](https://musl.libc.org).

## Linux (incl. WSL)

Assuming your distro isn't weird and has a normal GCC build environment, run `./do-local-build.sh` to build.
The output will be placed in `$SYSROOT`, which defaults to `../sysroot`.
If you don't have the submodule checked out, the script will try to do that.

## Windows

no
