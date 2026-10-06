#!/bin/sh
# Build libwaterboxhost for macOS as an x86_64 Mach-O dylib (runs under Rosetta 2 on
# Apple Silicon). Build on macOS with rustup, the x86_64-apple-darwin target,
# and Xcode Command Line Tools. See build-release.sh for the Linux/.so equivalent.
#
# The macOS interop trampoline supports guest entry under native x86_64 and Rosetta 2.
set -e
if [ -z "$BIZHAWKBUILD_HOME" ]; then export BIZHAWKBUILD_HOME="$(realpath "$(dirname "$0")/../..")"; fi
cd "$(dirname "$0")"

# This crate predates several nightly API changes (try_trait_v2, unsafe intrinsics), so pin
# a known-good nightly rather than the floating channel in rust-toolchain.toml.
TOOLCHAIN="${WBX_NIGHTLY:-nightly-2024-10-18}"
TARGET="x86_64-apple-darwin"
# Build for an older baseline so the dylib loads on older Macs, not just the build host.
# (The overall floor is set by the Homebrew deps, currently macOS 14.)
export MACOSX_DEPLOYMENT_TARGET="${MACOSX_DEPLOYMENT_TARGET:-11.0}"

rustup toolchain install "$TOOLCHAIN" --profile minimal
rustup target add --toolchain "$TOOLCHAIN" "$TARGET"

# cargo invokes a bare `rustc`; if another rustc (e.g. Homebrew's) precedes ~/.cargo/bin on
# PATH it gets picked and lacks the cross std. Pin RUSTC to the toolchain's rustc explicitly.
RUSTC_BIN="$(rustup which --toolchain "$TOOLCHAIN" rustc)"

# Regenerate the interop blobs (needs nasm: `brew install nasm`). The macOS variant
# (interop_macos.bin) handles %gs differently — see src/context/interop_macos.s.
if command -v nasm >/dev/null 2>&1; then
	make -C src/context
fi

RUSTC="$RUSTC_BIN" rustup run "$TOOLCHAIN" cargo build --release --target "$TARGET"

OUT="target/$TARGET/release/libwaterboxhost.dylib"
install_name_tool -id @rpath/libwaterboxhost.dylib "$OUT"
codesign --force --sign - "$OUT"
cp "$OUT" "$BIZHAWKBUILD_HOME/Assets/dll/libwaterboxhost.dylib"
if [ -e "$BIZHAWKBUILD_HOME/output" ]; then
	cp "$OUT" "$BIZHAWKBUILD_HOME/output/dll/libwaterboxhost.dylib"
fi
printf "copied libwaterboxhost.dylib (%s) into Assets/dll\n" "$TARGET"
