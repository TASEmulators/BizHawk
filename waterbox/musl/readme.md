# Waterbox toolchain - musl

Upstream is [musl libc](https://musl.libc.org).

## Nix

With [Nix](https://nixos.org) 2.3.x installed, run `nix-build --pure` to build the derivation. (It's pinned on the `release-20.09` branch.)
This isn't really that useful yet. If you `cp -a "$(realpath "$PWD/result")" "../sysroot"` afterwards then the other tools *should* work.

## Linux w/o Nix (incl. WSL)

Assuming your distro isn't weird and has a normal GCC build environment, run `./do-local-build.sh` to build.
The output will be placed in `$SYSROOT`, which defaults to `../sysroot`.
If you don't have the submodule checked out, the script will try to do that.

## Windows

no
