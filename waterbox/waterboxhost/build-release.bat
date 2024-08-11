:: To maintain Win7/8/8.1 compat for release builds, we must compile using the *-win7-windows-msvc target
:: This is a "Tier 3" target, which does not have prebuilt binaries (e.g. for standard library) available in rustup
:: The simplest way to use this target is to use the build-std argument, which builds the standard library
:: This requires the user to install the nightly toolchain (build-std is not in stable), then add the rust-src component
:: rustup toolchain install nightly-x86_64-pc-windows-msvc
:: rustup component add rust-src --toolchain nightly-x86_64-pc-windows-msvc
:: These don't need to be done for developer only builds (e.g. debug and/or no-dirty-detection), only release builds need this
@cargo +nightly-x86_64-pc-windows-msvc b --release -Z build-std --target x86_64-win7-windows-msvc
@copy target\x86_64-win7-windows-msvc\release\waterboxhost.dll ..\..\Assets\dll
@copy target\x86_64-win7-windows-msvc\release\waterboxhost.dll ..\..\output\dll
