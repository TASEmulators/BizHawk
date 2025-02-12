quickerNES
-----------

[![Build & Tests](https://github.com/SergioMartin86/quickerNES/actions/workflows/make.yml/badge.svg)](https://github.com/SergioMartin86/quickerNES/actions/workflows/make.yml)

quickerNES is an attempt to modernizing [quickNES](https://github.com/kode54/QuickNES). The goals for this project are, in order of importance:

- Improve overall emulation performance for modern (x86) CPUs (portability to other systems not guaranteed)
- Modernize the code base with best programming practices, including CI tests, benchmarks, and coverage analysis
- Add support for more mappers, controllers, and features supported by other emulators
- Improve accuracy, if possible

The main aim is to improve the performance of headless re-recording for TASing and botting (See: [JaffarPlus](https://github.com/SergioMartin86/jaffarPlus)) purposes. However, if this work can help regular play emulation, then much better.

Improvements
-------------

- Optimizations made in the CPU emulation core, including:
  + Forced alignment at the start of a page to prevent crossing cache line boundaries
  + Simplifying the 6502 CPU instruction fetching and decoding
  + Multiple branch optimizations
  + Assuming little endiannes to reduce unnecessary conversion operations (not portable to big endian systems)
  + Minimize compiled code size to reduce pressure on L1i cache
- Added support for FourScore controller
- General code reorganization (make it header only to help compiler optimizations)

Credits
---------

- quickNES was originally by Shay Green (a.k.a. [Blaarg](http://www.slack.net/~ant/)) under the GNU GPLv2 license. The source code is still located [here](https://github.com/kode54/QuickNES) 
- The code was later improved and maintained by Christopher Snowhill (a.k.a. [kode54](https://kode54.net/))
- I could trace further contributions (e.g., new mappers) by retrowertz, CaH4e3, some adaptations from the [FCEUX emulator](https://github.com/TASEmulators/fceux) (see mapper021)
- The latest version of the code is maintained by Libretro's community [here](https://github.com/libretro/QuickNES_Core)
- For the interactive player, this project drew some code from [HeadlessQuickNES (HQN)](https://github.com/Bindernews/HeadlessQuickNes) by Drew (Binder News)
- We use some of the [NES test rom set](https://github.com/christopherpow/nes-test-roms) made by multiple authors and gathered by Christopher Pow et al.
- We also use some movies from the [TASVideos](tasvideos.org) website for testing. These movies are copied into this repository with authorization under the Creative Commons Attribution 2.0 license.

All base code for this project was found under open source licenses, which I preserved in their corresponding files/folders. Any non-credited work is unintentional and shall be immediately rectfied.

