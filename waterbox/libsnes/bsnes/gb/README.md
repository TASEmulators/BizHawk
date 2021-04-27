# SameBoy

SameBoy is an open source Game Boy (DMG) and Game Boy Color (CGB) emulator, written in portable C. It has a native Cocoa frontend for macOS, an SDL frontend for other operating systems, and a libretro core. It also includes a text-based debugger with an expression evaluator. Visit [the website](https://sameboy.github.io/).

## bsnes integration

This directory should be a clean copy of the upstream SameBoy source, with the exception of this section of the README, and the `GNUmakefile` file that integrates it with bsnes' build system. Only files in the `Core/` directory are used, none of the UI or other portions.

## Features
Features common to both Cocoa and SDL versions:
 * Supports Game Boy (DMG) and Game Boy Color (CGB) emulation
 * Lets you choose the model you want to emulate regardless of ROM
 * High quality 96KHz audio
 * Battery save support
 * Save states
 * Includes open source DMG and CGB boot ROMs:
   * Complete support for (and documentation of) *all* game-specific palettes in the CGB boot ROM, for accurate emulation of Game Boy games on a Game Boy Color
   * Supports manual palette selection with key combinations, with 4 additional new palettes (A + B + direction)
   * Supports palette selection in a CGB game, forcing it to run in 'paletted' DMG mode, if ROM allows doing so.
   * Support for games with a non-Nintendo logo in the header
   * No long animation in the DMG boot
 * Advanced text-based debugger with an expression evaluator, disassembler, conditional breakpoints, conditional watchpoints, backtracing and other features
 * Extremely high accuracy
 * Emulates [PCM_12 and PCM_34 registers](https://github.com/LIJI32/GBVisualizer)
 * T-cycle accurate emulation of LCD timing effects, supporting the Demotronic trick, Prehistorik Man, [GBVideoPlayer](https://github.com/LIJI32/GBVideoPlayer) and other tech demos
 * Real time clock emulation
 * Retina/High DPI display support, allowing a wider range of scaling factors without artifacts
 * Optional frame blending (Requires OpenGL 3.2 or later)
 * Several [scaling algorithms](https://sameboy.github.io/scaling/) (Including exclusive algorithms like OmniScale and Anti-aliased Scale2x; Requires OpenGL 3.2 or later or Metal)

Features currently supported only with the Cocoa version:
 * Native Cocoa interface, with support for all system-wide features, such as drag-and-drop and smart titlebars
 * Game Boy Camera support
 
[Read more](https://sameboy.github.io/features/).

## Compatibility
SameBoy passes all of [blargg's test ROMs](http://gbdev.gg8.se/wiki/articles/Test_ROMs#Blargg.27s_tests), all of [mooneye-gb's](https://github.com/Gekkio/mooneye-gb) tests (Some tests require the original boot ROMs), and all of [Wilbert Pol's tests](https://github.com/wilbertpol/mooneye-gb/tree/master/tests/acceptance). SameBoy should work with most games and demos, please [report](https://github.com/LIJI32/SameBoy/issues/new) any broken ROM. The latest results for SameBoy's automatic tester are available [here](https://sameboy.github.io/automation/).

## Contributing
SameBoy is an open-source project licensed under the MIT license, and you're welcome to contribute by creating issues, implementing new features, improving emulation accuracy and fixing existing open issues. You can read the [contribution guidelines](CONTRIBUTING.md) to make sure your contributions are as effective as possible.

## Compilation
SameBoy requires the following tools and libraries to build:
 * clang (Recommended; required for macOS) or GCC
 * make
 * macOS Cocoa port: macOS SDK and Xcode (For command line tools and ibtool)
 * SDL port: libsdl2
 * [rgbds](https://github.com/bentley/rgbds/releases/), for boot ROM compilation

On Windows, SameBoy also requires:
 * Visual Studio (For headers, etc.)
 * [GnuWin](http://gnuwin32.sourceforge.net/)
 * Running vcvars32 before running make. Make sure all required tools and libraries are in %PATH% and %lib%, respectively. (see [Build FAQ](https://github.com/LIJI32/SameBoy/blob/master/build-faq.md) for more details on Windows compilation)

To compile, simply run `make`. The targets are `cocoa` (Default for macOS), `sdl` (Default for everything else), `libretro`, `bootroms` and `tester`. You may also specify `CONF=debug` (default), `CONF=release`, `CONF=native_release` or `CONF=fat_release`  to control optimization, symbols and multi-architectures. `native_release` is faster than `release`, but is optimized to the host's CPU and therefore is not portable. `fat_release` is exclusive to macOS and builds x86-64 and ARM64 fat binaries; this requires using a recent enough `clang` and macOS SDK using `xcode-select`, or setting them explicitly with `CC=` and `SYSROOT=`, respectively. All other configurations will build to your host architecture. You may set `BOOTROMS_DIR=...` to a directory containing precompiled boot ROM files, otherwise the build system will compile and use SameBoy's own boot ROMs.

By default, the SDL port will look for resource files with a path relative to executable. If you are packaging SameBoy, you may wish to override this by setting the `DATA_DIR` variable during compilation to the target path of the directory containing all files (apart from the executable, that's not necessary) from the `build/bin/SDL` directory in the source tree. Make sure the variable ends with a `/` character.

SameBoy was compiled and tested on macOS, Ubuntu and 64-bit Windows 7.
