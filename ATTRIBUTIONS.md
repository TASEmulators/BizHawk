# Attributions and Third-Party Licenses

This document provides detailed attribution for all third-party software, libraries, and code used in BizHawkRafaelia.

## Primary License

BizHawkRafaelia's original work is licensed under the **MIT License** (see LICENSE file).

## License Heterogeneity Notice

**IMPORTANT**: This repository contains code under multiple different licenses, including but not limited to:
* MIT License
* GNU General Public License v2 (GPL-2.0)
* GNU General Public License v3 (GPL-3.0)
* BSD Licenses (2-Clause, 3-Clause)
* LGPL (Lesser GNU General Public License)
* Custom non-commercial licenses
* Public Domain

Some of these licenses may be incompatible with each other for distribution purposes. Users redistributing this software must carefully review each component's license.

## Component Licenses

### Emulation Cores

#### PicoDrive (Sega Genesis/CD/32X)
* **License**: Custom BSD-like with non-commercial restriction
* **Location**: `waterbox/picodrive/`
* **Copyright**: notaz, fDave, and contributors
* **License File**: `waterbox/picodrive/COPYING`
* **Notes**: May not be used in commercial products

#### Mupen64Plus (Nintendo 64)
* **License**: GNU General Public License v2 (GPL-2.0)
* **Location**: `libmupen64plus/`
* **Copyright**: Mupen64Plus Team
* **License Files**: 
  - `libmupen64plus/mupen64plus-core/LICENSES`
  - `libmupen64plus/mupen64plus-rsp-hle/LICENSES`
  - `libmupen64plus/mupen64plus-video-glide64mk2/LICENSES`
  - `libmupen64plus/mupen64plus-video-rice/LICENSES`
  - `libmupen64plus/mupen64plus-video-glide64/COPYING`

#### Ares64 (Nintendo 64)
* **License**: ISC License
* **Location**: `waterbox/ares64/`
* **Copyright**: ares team
* **License File**: `waterbox/ares64/ares/LICENSE`

#### TIC-80
* **License**: MIT License
* **Location**: `waterbox/tic80/`
* **Copyright**: Vadim Grigoruk (nesbox)
* **License File**: `waterbox/tic80/LICENSE`
* **Third-party components**:
  - wasm3 (MIT) - `waterbox/tic80/vendor/wasm3/LICENSE`
  - Wren (MIT) - `waterbox/tic80/vendor/wren/LICENSE`
  - MoonScript (MIT) - `waterbox/tic80/vendor/moonscript/LICENSE`
  - Squirrel - `waterbox/tic80/vendor/squirrel/COPYRIGHT`
  - sljit (BSD-2-Clause) - `waterbox/ares64/ares/thirdparty/sljit/LICENSE`

#### Mednafen Cores (Various Systems)
* **License**: GNU General Public License v2 (GPL-2.0)
* **Location**: Various waterbox cores
* **Copyright**: Mednafen Team
* **Notes**: Multiple systems including PSX, PC-FX, PCE, WonderSwan, etc.

#### Gambatte (Game Boy/Color)
* **License**: GNU General Public License v2 (GPL-2.0)
* **Copyright**: Sinamas
* **Integration**: BizHawk team

#### SameBoy (Game Boy/Color)
* **License**: MIT License
* **Copyright**: LIJI32 (Lior Halphon)
* **Integration**: BizHawk team

#### mGBA (Game Boy Advance)
* **License**: Mozilla Public License v2.0 (MPL-2.0)
* **Copyright**: endrift (Vicki Pfau)
* **Integration**: BizHawk team

#### Genesis Plus GX (Sega Genesis/CD/32X/Master System/Game Gear)
* **License**: Modified BSD License / GPL-compatible
* **Copyright**: Eke-Eke and contributors
* **Integration**: BizHawk team

#### BSNES (Super Nintendo)
* **License**: GNU General Public License v3 (GPL-3.0)
* **Copyright**: byuu (Near) - RIP
* **Integration**: BizHawk team
* **Notes**: In memory of byuu/Near, a legendary emulator developer

#### melonDS (Nintendo DS/DSi)
* **License**: GNU General Public License v3 (GPL-3.0)
* **Copyright**: StapleButter (Arisotura)
* **Integration**: BizHawk team

#### Encore/Panda3DS (Nintendo 3DS)
* **License**: GNU General Public License v3 (GPL-3.0)
* **Copyright**: Fluto and Panda3DS team
* **Integration**: BizHawk team

#### MSXHawk (MSX)
* **License**: MIT License
* **Copyright**: zeromus, BizHawk team
* **Location**: `libHawk/MSXHawk/`

#### Hawk Cores (NESHawk, GBHawk, etc.)
* **License**: MIT License
* **Copyright**: Alyosha, BizHawk team
* **Cores**: NESHawk, GBHawk, A7800Hawk, IntelliHawk, O2Hawk, SMSHawk, VectrexHawk, ZXHawk, TI83Hawk, PCEHawk, ChannelFHawk, ColecoHawk

#### Virtual Jaguar (Atari Jaguar)
* **License**: GNU General Public License v3 (GPL-3.0)
* **Copyright**: Niels Wagenaar, Carwin Jones, James Hammons
* **Location**: `waterbox/virtualjaguar/`

#### Virtu (Apple II)
* **License**: MIT License
* **Copyright**: Sean Fausett
* **Location**: `ExternalCoreProjects/Virtu/`

#### MAME Cores (Arcade)
* **License**: GNU General Public License v2 or later (GPL-2.0+)
* **Copyright**: MAME Team
* **Website**: https://www.mamedev.org/
* **Notes**: Used for arcade system emulation

### Libraries and Dependencies

#### NLua (Lua Scripting)
* **License**: MIT License
* **Location**: `ExternalProjects/NLua/`
* **Copyright**: Vinicius Jarina (viniciusjarina@gmail.com)
* **License File**: `ExternalProjects/NLua/LICENSE`

#### Lua Programming Language
* **License**: MIT License
* **Copyright**: Lua.org, PUC-Rio
* **Website**: https://www.lua.org/

#### iso-parser
* **License**: MIT License
* **Location**: `ExternalProjects/iso-parser/`
* **License File**: `ExternalProjects/iso-parser/LICENSE.md`

#### PcxFileTypePlugin.HawkQuantizer
* **License**: MIT License
* **Location**: `ExternalProjects/PcxFileTypePlugin.HawkQuantizer/`
* **License File**: `ExternalProjects/PcxFileTypePlugin.HawkQuantizer/License.md`

#### LibBizHash (Checksum Library)
* **License**: zlib License and others
* **Location**: `ExternalProjects/LibBizHash/`
* **Copyright**: Various (Intel, ARM, etc.)
* **Notes**: Hardware-accelerated CRC32 and SHA1

#### zlib (Compression Library)
* **License**: zlib License
* **Copyright**: Mark Adler, Jean-loup Gailly
* **Website**: https://www.zlib.net/

#### SDL2 (Simple DirectMedia Layer)
* **License**: zlib License
* **Copyright**: Sam Lantinga and contributors
* **Website**: https://www.libsdl.org/

#### OpenAL (Audio Library)
* **License**: LGPL (Lesser GNU General Public License)
* **Copyright**: Creative Labs and contributors
* **Website**: https://www.openal.org/

#### FlatBuffers
* **License**: Apache License 2.0
* **Location**: `ExternalProjects/FlatBuffers.GenOutput/`
* **Copyright**: Google Inc.
* **Website**: https://google.github.io/flatbuffers/

#### .NET Runtime and Libraries
* **License**: MIT License
* **Copyright**: .NET Foundation and contributors
* **Website**: https://dotnet.microsoft.com/

#### Mono Runtime
* **License**: MIT License / LGPL (runtime)
* **Copyright**: Mono Project, Microsoft
* **Website**: https://www.mono-project.com/
* **Notes**: Used for Linux builds

### Build Tools and Development

#### Roslyn Analyzers
* **License**: MIT License
* **Copyright**: .NET Foundation
* **Location**: `ExternalProjects/BizHawk.Analyzer/` and related

#### Waterbox Toolchain
* **License**: Various (musl libc - MIT, gcc - GPL)
* **Location**: `waterbox/`
* **Notes**: Custom sandboxing and cross-compilation environment

### Sound and Audio Components

#### blip_buf (Audio Resampling)
* **License**: GNU Lesser General Public License (LGPL)
* **Location**: `blip_buf/`
* **Copyright**: Shay Green

#### Yamaha FM Sound Generator (from MAME)
* **License**: GPL-2.0+ (from MAME)
* **Copyright**: Tatsuyuki Satoh, Jarek Burczynski, MAME Team
* **Notes**: Used in various emulation cores

#### Texas Instruments SN76489/SN76496
* **License**: GPL-2.0+ (from MAME)
* **Copyright**: MAME Team
* **Notes**: Programmable tone/noise generator

### Additional Components

#### quickerNES (Nintendo Entertainment System)
* **License**: LGPL and/or GPL (derived from Blargg's Quick NES)
* **Copyright**: Shay Green (Blargg), Sergio Martin (quickerNES)
* **Notes**: Optimized fork of Quick NES

#### Handy (Atari Lynx)
* **License**: zlib/libpng License
* **Copyright**: Keith Wilkins
* **Notes**: From Mednafen

#### Cygne (WonderSwan)
* **License**: GPL-2.0
* **Copyright**: Mednafen Team
* **Notes**: From Mednafen

## Third-Party Research and Documentation

This project benefits from extensive research and documentation:

* **Charles MacDonald** - Genesis hardware documentation
* **Steve Snake** - Genesis emulation development
* **Stephane Dallongeville** - Gens emulator (open source)
* **Tasco Deluxe** - SVP reverse engineering
* **Bart Trzynadlowski** - SSFII and 68000 documentation
* **Haze** - Research and MAME contributions
* **Nemesis** - YM2612 and VDP research
* **TASVideos Community** - Testing and documentation
* **No$GBA author** - GameBoy documentation
* **Gekkio** - GameBoy research and documentation
* **Pan Docs** - GameBoy technical reference
* **NESdev Wiki** - NES technical documentation

## Bibliographic References

### Academic and Technical References

1. **Emulation Architecture**
   - "Accuracy Takes Power: One Man's 3GHz Quest to Build a Perfect SNES Emulator" - byuu/Near
   - "The Design and Implementation of a Verification System for Emulated Processors" - Various TAS community members

2. **Console Architecture Documentation**
   - "Genesis Technical Documentation" - Charles MacDonald
   - "SNES Development Manual" - Nintendo
   - "GameBoy Programming Manual" - Nintendo
   - "PlayStation Architecture: A Practical Analysis" - Rodrigo Copetti

3. **Tool-Assisted Speedrun Methodology**
   - TASVideos.org - Community knowledge base
   - "The Art of Tool-Assisted Speedrunning" - Multiple authors

### Software Engineering References

1. **Code Quality and Patterns**
   - "Design Patterns: Elements of Reusable Object-Oriented Software" - Gang of Four
   - "Clean Code" - Robert C. Martin

2. **Emulation Techniques**
   - "Emulation Accuracy vs Performance" - Various MAME documentation
   - "Dynamic Recompilation in Emulators" - Various technical articles

## GPL-Licensed Components Notice

### GNU General Public License v2 Components

The following components are licensed under GPL-2.0:

1. Mupen64Plus (N64 emulation)
2. Mednafen cores (multiple systems)
3. Gambatte (GB/GBC)
4. MAME arcade cores
5. Various sound chip implementations

### GNU General Public License v3 Components

The following components are licensed under GPL-3.0:

1. BSNES (SNES emulation)
2. melonDS (DS/DSi)
3. Encore/Panda3DS (3DS)
4. Virtual Jaguar (Jaguar)

### GPL Distribution Requirements

If you distribute BizHawkRafaelia, you MUST:

1. Include complete source code for all GPL-licensed components
2. Provide GPL license texts
3. Allow recipients to modify and redistribute under GPL terms
4. Not impose additional restrictions on GPL-licensed code
5. Maintain copyright notices and authorship information

### GPL Compliance Resources

For more information about GPL compliance:
* GNU.org: https://www.gnu.org/licenses/gpl-howto.html
* Free Software Foundation: https://www.fsf.org/licensing/
* Software Freedom Conservancy: https://sfconservancy.org/

## License Compatibility Matrix

| Component Type | Primary License | Compatible With | Notes |
|---------------|----------------|-----------------|-------|
| BizHawk Core | MIT | Most licenses | Very permissive |
| GPL-2.0 Cores | GPL-2.0 | GPL-2.0, GPL-3.0 | Copyleft |
| GPL-3.0 Cores | GPL-3.0 | GPL-3.0 | Copyleft, not compatible with GPL-2.0 distribution |
| BSD Licensed | BSD | Most licenses | Permissive |
| MIT Licensed | MIT | Most licenses | Very permissive |
| LGPL | LGPL | Most (as library) | Can link with proprietary |
| Non-Commercial | Custom | Limited | Cannot use commercially |

## Inspiration and Acknowledgments

BizHawkRafaelia is inspired by and builds upon:

1. **BizHawk Project** - The upstream emulator providing the core foundation
2. **RetroArch/Libretro** - Multi-system emulation architecture concepts
3. **MAME Project** - Preservation philosophy and accuracy standards
4. **TASVideos Community** - Tool-assisted speedrunning methodology
5. **Individual Emulator Projects** - Each core's original standalone emulator
6. **Open Source Community** - Collaborative development model
7. **Academic Research** - Computer architecture and emulation theory

## Usage Restrictions and Compliance

### Commercial Use Restrictions

The following components PROHIBIT commercial use:
* PicoDrive core and derivatives

### Patent Considerations

Some emulated systems may involve patent-protected technology. Users are responsible for compliance with applicable patent laws.

### Firmware and BIOS Files

This software requires firmware/BIOS files for some systems. Users must:
1. Dump firmware from devices they own
2. Not distribute copyrighted firmware files
3. Comply with applicable copyright laws

## How to Attribute This Software

When using BizHawkRafaelia in your work:

1. **For Academic/Research**:
   ```
   BizHawkRafaelia Emulator. (2025). 
   Derived from BizHawk by TASEmulators.
   https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
   ```

2. **For Software**:
   - Include this ATTRIBUTIONS.md file
   - Include CONTRIBUTORS.md
   - Include LICENSE and all component licenses
   - Clearly indicate which components you're using

3. **For Publications**:
   - Cite the specific emulation cores used
   - Reference original emulator authors
   - Acknowledge the BizHawk and BizHawkRafaelia projects

## Updates and Maintenance

This attribution file should be updated when:
1. New third-party components are added
2. Component licenses change
3. New contributors make significant contributions
4. License compliance issues are discovered

Last Updated: 2025-11-16

## Contact for Licensing Questions

For questions about licenses or attributions:
* Open an issue on GitHub: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
* Review original project licenses in their respective directories
* Consult with legal counsel for commercial use or redistribution

## Disclaimer

This attribution file is provided for informational purposes. It does not constitute legal advice. Users are responsible for ensuring their own compliance with all applicable licenses and laws. When in doubt, consult the original license files and/or legal counsel.
