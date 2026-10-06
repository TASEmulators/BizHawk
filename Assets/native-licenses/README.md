# macOS native libraries

This directory contains license notices for the macOS libraries in `Assets/dll`.
For setup instructions, see the [macOS section of the main readme](../../README.md#macos-experimental-x86_64-port).

The libraries come from:

- `libbizhash.dylib`: `ExternalProjects/LibBizHash`. See its readme for the CRC32 and SHA1 licenses.
- `libwaterboxhost.dylib`: `waterbox/waterboxhost`, built with Rust nightly `2024-10-18`. See the repository `LICENSE` and the Cargo dependencies' licenses.
- `libSDL2.dylib`: the SDL2 submodule at `3eba0b6f8a21392f47b1b53a476e7633048de9b1`. License: `SDL2-LICENSE.txt`.
- `libgdiplus.0.dylib`: [libgdiplus 6.1](https://download.mono-project.com/sources/libgdiplus/libgdiplus-6.1.tar.gz), built with X11 and Pango support. License: `libgdiplus-LICENSE`.
- `libcimgui.dylib`: copied from ImGui.NET NuGet package `1.90.6.1` (source commit `0b2d0f85f5c41777785d348f123fa9f2516c0df4`). Licenses: `cimgui-LICENSE`, `imgui-LICENSE.txt`, and `ImGui.NET-LICENSE`.

To rebuild them, run this from the repository root:

```sh
sh Dist/BuildMacOS.sh
```

The helper reuses libgdiplus if it already has X11 support. Set `BIZHAWK_REBUILD_GDIPLUS=1` to force a rebuild.
It checks the source archive's SHA-256 before building. If you update ImGui, update the version and license notices here too.

The hash library, Waterbox and SDL2 target macOS 11. Libgdiplus targets macOS 14 and needs the x86_64 Homebrew dependencies listed in the main readme.
ImGui's library targets macOS 10.13 for x86_64 and 11 for arm64; EmuHawk itself still runs as x86_64.
Older macOS versions have not been tested.

Dependency symlinks are created by `EmuHawkMono.sh` when it runs. Don't check those or the launch logs into Git.
Windows and Linux packages leave out the `.dylib` files.
