# SameBoy

SameBoy is an open source Gameboy (DMG) and Gameboy Color (CGB) emulator, written in portable C. It has a native Cocoa frontend for OS X, and an incomplete experimental SDL frontend for other operating systems. It also includes a text-based debugger with an expression evaluator. Visit [the website](https://sameboy.github.io/).

## Features
Features common to both Cocoa and SDL versions:
 * Supports Gameboy (DMG) and Gameboy Color (CGB) emulation
 * Lets you choose the model you want to emulate regardless of ROM
 * High quality 96KHz audio
 * Battery save support
 * Save states
 * Includes open source DMG and CGB boot ROMs:
   * Complete support for (and documentation of) *all* game-specific palettes in the CGB boot ROM, for accurate emulation of Gameboy games on a Gameboy Color
   * Supports manual palette selection with key combinations, with 4 additional new palettes (A + B + direction)
   * Supports palette selection in a CGB game, forcing it to run in 'paletted' DMG mode, if ROM allows doing so.
   * Support for games with a non-Nintendo logo in the header
   * No long animation in the DMG boot
 * Advanced text-based debugger with an expression evaluator, disassembler, conditional breakpoints, conditional watchpoints, backtracing and other features
 * Emulates [PCM_12 and PCM_34 registers](https://github.com/LIJI32/GBVisualizer)
 * Emulates LCD timing effects, supporting the Demotronic trick, [GBVideoPlayer](https://github.com/LIJI32/GBVideoPlayer) and other tech demos
 * Extermely high accuracy
 * Real time clock emulation

Features currently supported only with the Cocoa version:
 * Native Cocoa interface, with support for all system-wide features, such as drag-and-drop and smart titlebars
 * Retina display support, allowing a wider range of scaling factors without artifacts
 * Optional frame blending
 * Several [scaling algorithms](https://sameboy.github.io/scaling/) (Including exclusive algorithms like OmniScale and Anti-aliased Scale2x)
 * GameBoy Camera support
 
[Read more](https://sameboy.github.io/features/).

## Compatibility
SameBoy passes many of [blargg's test ROMs](http://gbdev.gg8.se/wiki/articles/Test_ROMs#Blargg.27s_tests), as well as 77 out of 78 of [mooneye-gb's](https://github.com/Gekkio/mooneye-gb) tests (Some tests require the original boot ROMs). SameBoy should work with most games and demos, please [report](https://github.com/LIJI32/SameBoy/issues/new) any broken ROM. The latest results for SameBoy's automatic tester are available [here](https://sameboy.github.io/automation/).

## Compilation
SameBoy requires the following tools and libraries to build:
 * clang
 * make
 * Cocoa port: OS X SDK and Xcode command line tools
 * SDL port: SDL2.framework (OS X) or libsdl2 (Other platforms)
 * [rgbds](https://github.com/bentley/rgbds/releases/), for boot ROM compilation

On Windows, SameBoy also requires:
 * Visual Studio (For headers, etc.)
 * [GnuWin](http://gnuwin32.sourceforge.net/)
 * Running vcvars32 before running make. Make sure all required tools and libraries are in %PATH% and %lib%, repsectively.

To compile, simply run `make`. The targets are cocoa (Default for OS X), sdl (Default for everything else), bootroms and tester. You may also specify CONF=debug (default), CONF=release or CONF=native_release to control optimization and symbols. native_release is faster than release, but is optimized to the host's CPU and therefore is not portable. You may set BOOTROMS_DIR=... to a directory containing precompiled dmg_boot.bin and cgb_boot.bin files, otherwise the build system will compile and use SameBoy's own boot ROMs.

SameBoy was compiled and tested on macOS, Ubuntu and 32-bit Windows 7.
