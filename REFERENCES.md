# Bibliographic References and Inspirations

This document provides comprehensive bibliographic references for the research, documentation, and inspiration behind BizHawkRafaelia and its emulation cores.

## Overview

BizHawkRafaelia is built on decades of emulation research, reverse engineering, and community collaboration. This document acknowledges the technical documentation, academic research, and inspirational projects that made this work possible.

## Primary Inspirations

### 1. BizHawk Emulator
**Project**: BizHawk - A Multi-System Emulator  
**Authors**: TASEmulators Team (adelikat, zeromus, natt, YoshiRulz, and contributors)  
**URL**: https://github.com/TASEmulators/BizHawk  
**License**: MIT License (core), Various (emulation cores)  
**Contribution**: Primary upstream project, architecture, frontend design, tool integration  
**Citation**: TASEmulators. (2024). BizHawk - Multi-system emulator. GitHub. https://github.com/TASEmulators/BizHawk

### 2. TASVideos Community
**Project**: Tool-Assisted Speedrun Community and Knowledge Base  
**URL**: https://tasvideos.org/  
**Contribution**: Methodology for tool-assisted speedrunning, testing protocols, accuracy standards  
**Citation**: TASVideos. (2024). TASVideos - Tool-Assisted Speedruns. https://tasvideos.org/

## Emulation Theory and Practice

### Accuracy-Focused Emulation

1. **byuu (Near). "Accuracy Takes Power: One Man's 3GHz Quest to Build a Perfect SNES Emulator"**
   - Source: Ars Technica, 2011
   - URL: https://arstechnica.com/gaming/2011/08/accuracy-takes-power-one-mans-3ghz-quest-to-build-a-perfect-snes-emulator/
   - Relevance: Philosophy of cycle-accurate emulation
   - Impact: Informed accuracy priorities in core selection

2. **byuu (Near). "SNES Preservation Project"**
   - Source: byuu.org (archived)
   - Relevance: Game preservation and emulation accuracy methodology
   - Impact: Accuracy-first approach to emulation

3. **Copetti, Rodrigo. "Console Architecture: A Practical Analysis"**
   - URL: https://www.copetti.org/writings/consoles/
   - Relevance: Detailed hardware architecture documentation
   - Systems Covered: PlayStation, Nintendo 64, Game Boy, SNES, and more
   - Impact: Reference for understanding emulated systems

### Dynamic Recompilation

1. **QEMU Project. "Dynamic Translation and Optimization"**
   - Source: QEMU Documentation
   - URL: https://www.qemu.org/docs/master/devel/tcg.html
   - Relevance: Dynamic recompilation techniques
   - Application: JIT compilation in various cores

2. **Bellard, Fabrice. "QEMU, a Fast and Portable Dynamic Translator"**
   - Conference: USENIX Annual Technical Conference, 2005
   - Relevance: Dynamic binary translation principles
   - Impact: Influenced recompiler implementations

## Console Hardware Documentation

### Nintendo Systems

#### Nintendo Entertainment System (NES)

1. **NESdev Wiki. "NES Architecture and Development"**
   - URL: https://www.nesdev.org/wiki/
   - Maintainers: NESdev Community
   - Coverage: CPU (6502), PPU, APU, mappers, timing
   - Usage: Reference for NESHawk and quickerNES cores

2. **6502.org. "6502 Microprocessor Documentation"**
   - URL: http://www.6502.org/
   - Relevance: CPU architecture and instruction set
   - Impact: Accurate CPU emulation

#### Super Nintendo Entertainment System (SNES)

1. **fullsnes. "Super Nintendo Entertainment System Documentation"**
   - Author: nocash
   - Coverage: Complete SNES hardware specification
   - Usage: Reference for BSNES integration

2. **anomie. "SNES Hardware Specifications"**
   - Source: anomie.retrogames.com (archived)
   - Relevance: Hardware registers and behavior
   - Impact: Accuracy verification

#### Nintendo 64

1. **N64brew. "Nintendo 64 Development Wiki"**
   - URL: https://n64brew.dev/
   - Coverage: Reality Coprocessor (RCP), CPU (VR4300), memory system
   - Usage: Reference for Mupen64Plus and Ares64

2. **Nintendo 64 Programming Manual**
   - Source: Official Nintendo documentation (leaked)
   - Relevance: Official hardware specifications
   - Note: Usage for emulation purposes only

#### Game Boy / Game Boy Color

1. **Pan Docs. "Game Boy Complete Technical Reference"**
   - URL: https://gbdev.io/pandocs/
   - Maintainers: gbdev Community
   - Coverage: CPU (SM83), PPU, sound, cartridge hardware
   - Usage: Primary reference for GBHawk, Gambatte, SameBoy

2. **Gekkio. "Game Boy: Complete Technical Reference"**
   - Author: Joonas Javanainen (Gekkio)
   - URL: https://gekkio.fi/files/gb-docs/gbctr.pdf
   - Relevance: Extremely detailed hardware analysis
   - Impact: Accuracy verification and edge case handling

3. **The Cycle-Accurate Game Boy Docs**
   - Author: AntonioND
   - Relevance: Timing accuracy research
   - Usage: Cycle-accurate emulation implementation

#### Game Boy Advance

1. **GBATEK. "GBA/NDS Technical Information"**
   - Author: Martin Korth (nocash)
   - URL: https://problemkaputt.de/gbatek.htm
   - Coverage: ARM7TDMI CPU, PPU, DMA, interrupts
   - Usage: Reference for mGBA integration

2. **Tonc. "GBA Development Tutorial"**
   - Author: Jasper Vijn (Cearn)
   - Relevance: Practical GBA programming examples
   - Impact: Understanding real-world hardware usage

#### Nintendo DS

1. **GBATEK. "NDS Technical Information"**
   - Author: Martin Korth (nocash)
   - Coverage: ARM9/ARM7 CPUs, 3D engine, Wi-Fi
   - Usage: Reference for melonDS integration

### Sega Systems

#### Sega Genesis / Mega Drive

1. **MacDonald, Charles. "Sega Genesis / Mega Drive Documentation"**
   - URL: https://www.plutiedev.com/genesis-hardware (community maintained)
   - Original: http://cgfm2.emuviews.com/ (archived)
   - Coverage: Motorola 68000, VDP, Z80, YM2612
   - Usage: Primary reference for Genesis Plus GX and PicoDrive
   - Impact: Foundation of Genesis emulation

2. **Sega Genesis Software Manual**
   - Source: Official Sega documentation
   - Relevance: Official hardware specifications
   - Usage: Verification of emulation accuracy

#### Sega Master System / Game Gear

1. **SMS Power! "Master System / Game Gear Technical Documentation"**
   - URL: https://www.smspower.org/Development/Index
   - Coverage: Z80 CPU, VDP, PSG
   - Usage: Reference for SMSHawk and Genesis Plus GX

### Sony PlayStation

1. **No$PSX Specifications**
   - Author: Martin Korth (nocash)
   - URL: https://problemkaputt.de/psx-spx.htm
   - Coverage: MIPS R3000A CPU, GTE, GPU, SPU, CD-ROM
   - Usage: Reference for PSX cores (Octoshock, Nymashock)

2. **PSX Software Development Kit Documentation**
   - Source: Official Sony documentation (leaked)
   - Relevance: Hardware functionality and behavior
   - Note: Reference only for emulation

### Atari Systems

1. **Atari 2600 Hardware Manual**
   - Coverage: 6507 CPU, TIA chip
   - Usage: Reference for Atari2600Hawk and Stella

2. **Atari 7800 Development Documentation**
   - Coverage: 6502C CPU, MARIA graphics chip
   - Usage: Reference for A7800Hawk

### NEC PC Engine / TurboGrafx-16

1. **PC Engine Software Bible**
   - Source: NEC/Hudson documentation
   - Relevance: HuC6280 CPU and video hardware
   - Usage: Reference for PCEHawk and TurboNyma

### MSX Computers

1. **MSX Technical Handbook**
   - Author: ASCII Corporation
   - Coverage: Z80 CPU, TMS9918 VDP, PSG
   - Usage: Reference for MSXHawk

2. **MSX.org Resource Center**
   - URL: https://www.msx.org/
   - Relevance: Hardware variations and compatibility
   - Impact: Multi-model support

## Emulation Projects and Inspiration

### Standalone Emulators

1. **MAME (Multiple Arcade Machine Emulator)**
   - Authors: MAME Team
   - URL: https://www.mamedev.org/
   - License: GPL-2.0+
   - Contribution: Arcade system emulation, accuracy standards, preservation philosophy
   - Citation: MAME Development Team. (2024). MAME - Multiple Arcade Machine Emulator. https://www.mamedev.org/

2. **Mednafen Multi-System Emulator**
   - Author: Mednafen Team
   - URL: https://mednafen.github.io/
   - License: GPL-2.0
   - Contribution: Multiple emulation cores with high accuracy
   - Systems: PSX, PC-FX, PCE, NES, SNES, and more
   - Citation: Mednafen Team. (2024). Mednafen. https://mednafen.github.io/

3. **RetroArch / Libretro**
   - Authors: Libretro Team
   - URL: https://www.retroarch.com/
   - License: GPL-3.0
   - Contribution: Core interface architecture, multi-platform support
   - Impact: Influenced core integration approach

4. **higan / bsnes**
   - Author: byuu (Near) - RIP
   - License: GPL-3.0
   - Contribution: Cycle-accurate SNES emulation
   - Philosophy: "Accuracy over performance"
   - Note: In loving memory of byuu, whose work revolutionized emulation

5. **Dolphin Emulator (GameCube/Wii)**
   - Authors: Dolphin Team
   - URL: https://dolphin-emu.org/
   - License: GPL-2.0+
   - Contribution: Modern emulation architecture, debugging tools
   - Inspiration: High-quality open-source emulator design

6. **PPSSPP (PlayStation Portable)**
   - Author: Henrik Rydgård
   - URL: https://www.ppsspp.org/
   - License: GPL-2.0+
   - Contribution: Cross-platform emulation, JIT compilation
   - Inspiration: Portable architecture design

## Sound and Audio Research

1. **Green, Shay (Blargg). "Sound Emulation Overview"**
   - Source: blargg.8bitalley.com (archived)
   - Contribution: blip_buf library, sound synthesis techniques
   - Usage: Audio resampling in multiple cores

2. **Yamaha YM2612 Technical Documentation**
   - Source: Yamaha semiconductor documentation
   - Research: Nemesis (SMForums)
   - Usage: Genesis/Mega Drive sound emulation

3. **Texas Instruments SN76489 Data Sheet**
   - Source: TI official documentation
   - Usage: Master System, Game Gear, Genesis PSG emulation

## Software Engineering and Architecture

1. **Fowler, Martin. "Patterns of Enterprise Application Architecture"**
   - Publisher: Addison-Wesley, 2002
   - ISBN: 0-321-12742-0
   - Relevance: Software architecture patterns
   - Application: Frontend design, plugin system

2. **Gamma, Erich, et al. "Design Patterns: Elements of Reusable Object-Oriented Software"**
   - Publisher: Addison-Wesley, 1994
   - ISBN: 0-201-63361-2
   - Relevance: Object-oriented design patterns
   - Application: Core architecture, tool interfaces

3. **Martin, Robert C. "Clean Code: A Handbook of Agile Software Craftsmanship"**
   - Publisher: Prentice Hall, 2008
   - ISBN: 0-132-35088-2
   - Relevance: Code quality and maintainability
   - Application: Development standards

## Testing and Quality Assurance

1. **Test ROM Suites**
   - Blargg's Test ROMs (NES, Game Boy, SNES)
   - Gekkio's Mooneye Test Suite (Game Boy)
   - Mealybug Tearoom Tests (Game Boy)
   - Various homebrew test programs
   - Usage: Automated accuracy testing

2. **TASVideos Testing Methodology**
   - Source: TASVideos.org
   - Contribution: Real-world testing scenarios
   - Application: Regression testing with TAS movies

## Research on Tool-Assisted Speedrunning

1. **"The Theory and Practice of Tool-Assisted Speedrunning"**
   - Source: TASVideos Community Documentation
   - URL: https://tasvideos.org/
   - Topics: Frame-perfect input, RNG manipulation, glitch exploitation
   - Application: TAStudio design and features

2. **"Deterministic Emulation Requirements"**
   - Source: TASVideos Forum Discussions
   - Relevance: Ensuring reproducible behavior
   - Impact: Emulation core requirements

## Cross-Platform Development

1. **.NET and Mono Documentation**
   - Source: Microsoft and Mono Project
   - URLs: https://docs.microsoft.com/dotnet/, https://www.mono-project.com/
   - Relevance: Cross-platform C# development
   - Application: Windows and Linux support

2. **SDL2 Documentation**
   - Source: Simple DirectMedia Layer
   - URL: https://wiki.libsdl.org/
   - Relevance: Cross-platform input and audio
   - Application: Input handling, audio output

## Legal and Licensing Resources

1. **Free Software Foundation. "Various Licenses and Comments about Them"**
   - URL: https://www.gnu.org/licenses/license-list.html
   - Relevance: License compatibility and compliance
   - Application: Multi-license project management

2. **Open Source Initiative. "Licenses & Standards"**
   - URL: https://opensource.org/licenses
   - Relevance: Open source license definitions
   - Application: License selection and compliance

3. **Heather Meeker. "Open Source for Business"**
   - Publisher: CreateSpace, 2015
   - ISBN: 1-511-61617-7
   - Relevance: Open source licensing in practice
   - Application: License compliance strategy

## Preservation and Archival

1. **Software Preservation Society**
   - URL: https://www.worldofspectrum.org/
   - Relevance: Game preservation importance
   - Philosophy: Preserving computing history

2. **Internet Archive Software Collection**
   - URL: https://archive.org/details/software
   - Relevance: Historical software preservation
   - Inspiration: Long-term preservation goals

## Community Resources and Forums

1. **Reddit Communities**
   - r/emulation - General emulation discussion
   - r/EmuDev - Emulation development
   - r/TAS - Tool-assisted speedrunning
   - Relevance: Community feedback and knowledge sharing

2. **Discord Communities**
   - TASVideos Discord
   - Emulation Development Discord
   - Individual emulator communities
   - Relevance: Real-time collaboration and support

3. **GitHub and Open Source Community**
   - Issue tracking
   - Pull request collaboration
   - Code review processes
   - Impact: Collaborative development model

## Academic Publications

1. **Bellard, Fabrice. "QEMU, a Fast and Portable Dynamic Translator"**
   - Conference: USENIX Annual Technical Conference, 2005
   - Relevance: Dynamic binary translation
   - Application: Recompiler design principles

2. **Papers on Binary Translation and Emulation**
   - Various IEEE and ACM conference publications
   - Topics: Performance optimization, accuracy verification
   - Application: Theoretical foundations

## Development Tools and Methodologies

1. **Git Version Control**
   - Chacon, Scott and Straub, Ben. "Pro Git"
   - URL: https://git-scm.com/book/
   - Publisher: Apress, 2014
   - Application: Source control management

2. **Continuous Integration and Testing**
   - GitHub Actions Documentation
   - Relevance: Automated testing and builds
   - Application: CI/CD pipeline

## Special Acknowledgments

### In Memory

* **byuu (Near)** - Pioneering emulator developer, creator of BSNES/higan
  - Contribution: Revolutionized accuracy-focused emulation
  - Legacy: Inspired accuracy-first approach across emulation community
  - Years Active: 2004-2021
  - Note: The emulation community mourns the loss of this talented developer

### Research Contributors

* **Martin Korth (nocash)** - Comprehensive technical documentation (GBA, DS, PSX)
* **Charles MacDonald** - Genesis/Mega Drive hardware documentation
* **Gekkio (Joonas Javanainen)** - Game Boy accuracy research
* **Shay Green (Blargg)** - Sound emulation and test ROMs
* **Nemesis** - YM2612 and VDP research
* **anomie** - SNES hardware research

### Organizational Support

* **TASVideos** - Community platform and knowledge base
* **GitHub** - Code hosting and collaboration
* **Various console homebrew communities** - Testing and development tools

## Updates and Contributions

This references document is a living document. Contributions to expand and improve it are welcome.

To add a reference:
1. Ensure it's relevant to emulation, BizHawk, or related fields
2. Provide complete citation information
3. Explain the reference's relevance and impact
4. Include URLs when available (with archive links if needed)

## Citation Format

When citing BizHawkRafaelia in academic or professional work:

**APA Style:**
```
Reis, R. M. (2025). BizHawkRafaelia [Computer software]. 
Derived from BizHawk by TASEmulators. 
https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
```

**IEEE Style:**
```
R. M. Reis, "BizHawkRafaelia," 2025. [Online]. 
Available: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia
```

**BibTeX:**
```bibtex
@software{bizhawkrafaelia2025,
  author = {Reis, Rafael Melo},
  title = {BizHawkRafaelia},
  year = {2025},
  note = {Derived from BizHawk by TASEmulators},
  url = {https://github.com/rafaelmeloreisnovo/BizHawkRafaelia}
}
```

## Conclusion

BizHawkRafaelia stands on the shoulders of giants. This document recognizes the immense collective effort of the emulation community, hardware researchers, software engineers, and preservationists who made this work possible.

Every contributor, from those who wrote technical documentation to those who developed emulation cores, from those who created test ROMs to those who reported bugs, has played a crucial role in advancing the field of emulation.

This project is dedicated to preserving computing history and enabling tool-assisted speedrunning as an art form and technical pursuit.

---

**Last Updated**: 2025-11-16  
**Maintained By**: Rafael Melo Reis  
**Project**: BizHawkRafaelia  
**License**: See LICENSE, CONTRIBUTORS.md, and ATTRIBUTIONS.md

For corrections or additions to these references, please open an issue or submit a pull request on GitHub.
