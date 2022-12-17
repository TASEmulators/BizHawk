# BizHawk changelog

## changes from 2.8 to 2.9

- Misc. changes to EmuHawk:
	- fixed keybinds not working after waking from lock screen on Windows (#3161)
	- added warning when current firmware customization does not match the one specified in the movie header (#2498)
	- refactored memory poking, allowing negative values for fixed-point watches and fixing other bugs (#3175)
	- fixed `.gmv` importer not setting the core name `.bk2` header
	- fixed regression where screenshots from some cores were transparent (#3166)
	- fixed Lua referencing a core after it's unloaded, making scripts crash (#3226)
	- made DirectX optional on Windows (it's still in the prereq installer because it's usually faster)
	- fixed BSNES rom loading edge case where the filename doesn't exactly match the one in the gamedb
	- fixed opening roms from "jump list" in Windows shell / file manager (#3224)
	- improved UX of loading a savestate from an older (or newer) version
	- fixed shaders' height being used for width
	- finished UX for merging/unmerging LShift+RShift and other modifier keys (#3184, #3257)
	- fixed `.dsm` importer which relied on non-deterministic `Dictionary` (hashmap) ordering for axis names
	- stopped offering to remove missing file from recent roms when it's not actually missing, it just failed to load (#3006)
	- fixed hotkeys triggering accidentally in Virtual Pad on Windows (#3087)
	- increased precision when tweaking axis sensitivity/deadzone (#3038)
	- fixed category radio buttons being cut off in Messages config
	- restored "priority" option for U+D/L+R policy, and made it the default again (#2752)
	- refactored firmware config so the acceptability status icons make sense (#3157)
	- fixed "Screenshot (raw) -> Clipboard" not showing keybind hint
	- added UI for editing any* core's settings/syncsettings without it being loaded
	- fixed strange behaviour when trying to extract archive which contains folders
	- bumped FFmpeg to 4.4.1, added auto-download to Linux port (#3259)
	- reordered `Config` > `Preferred Cores` submenu
	- removed some unnecessary prompts to reboot core
	- moved "Save Window Position" for main window to `Config` > `Display...` > `Window` and added a "Stay on Top"
	- fixed Windows version detection and enabled warning for unsupported versions (#2972, #3194)
	- fixed some systems not having a name to display in the window title
	- fixed MSX rom loading
	- made file extension check for disc images case-insensitive
	- fixed Debugger breakpoint crash with mGBA (#3287)
- Linux port:
	- fixed various file pickers using case-sensitive file extensions
	- changed default Lua engine to "NLua+KopiLua" which doesn't seem to crash on normal Mono builds like the other one does
	- added short-circuit to Mupen64Plus loading to avoid error messages and any strange failure state
	- adjusted alignment and size of UI elements in Record Movie dialog so they don't overlap... again
- TAStudio:
	- fixed "Select between Markers" hotkey not working
	- fixed `.tasproj` headers being written differently based on locale (i.e. ',' instead of '.')
- Lua/ApiHawk:
	- (Lua) added arguments to memory callback functions (cb will be called with addr, val, flags)—check `event.can_use_callback_params("memory")` when writing polyfills
	- (ApiHawk) merged `IGameInfoApi` into `IEmulationApi`, and some other minor API method signature changes
	- (ApiHawk/Lua) fixed `event.onmemoryread` behaviour under mGBA (#3230)
	- (ApiHawk/Lua) improved how removing a memory callback from within a callback is processed (#1823)
	- (Lua) fixed setting size of Lua Forms (#3034)
	- (ApiHawk) changed injector to include non-public properties when looking for `ApiContainer`
	- (ApiHawk) deprecated `CoreSystem` enum in favour of `VSystemID` const strings
	- (Lua) fixed encoding bug which caused e.g. Japanese text to become mojibake/garbled (#190, #2041)
- Other cores:
	- Virtu:
		- changed RTC to use deterministic time when recording instead of (host) system time
	- ZXHawk:
		- removed redundant `ZXSpectrumPokeMemory` tool

[948049bb2 CPP] (Genplus-gx) stopped byteswapping Z80 domains (#3290)

[4df256cd6 CPP] (Virtu) fixed some internal state not being overwritten by savestates

[5afb6ca45 Yoshi] fixed `InvalidOperationException` when using SMS peripherals (#3282)

[1d4e7dd3f Morilli] (BSNESv115+) rework Payload peripheral and fix Virtual Pads

[363afcd55 Morilli] (BSNESv115+) improve peripheral selection for P1

[2c3b6b3cd CPP] (TIC-80) new core (nesbox' own reference implementation)

[0174abde6 CPP] (a26) fix crash when pushing Select on Karate title screen

[f5d8c0fb1 Yoshi] (Genplus-gx) changed default peripheral to 3-button Genesis gamepad (#2775, #3262)

[1c27c73c8 CPP] fixed disc switching for Nymashock and Saturnus

[0c95088e0 CPP] (VirtualBoyee) updated to Mednafen 1.29.0

[e6d74c316 CPP] (Faust) updated to Mednafen 1.29.0

[e6d74c316 CPP] (HyperNyma) updated to Mednafen 1.29.0

[e6d74c316 CPP] (TurboNyma) updated to Mednafen 1.29.0

[e6d74c316 CPP] (NeoPop) updated to Mednafen 1.29.0

[e6d74c316 CPP] (T. S. T.) updated to Mednafen 1.29.0

[e6d74c316 CPP] (Nymashock) updated to Mednafen 1.29.0

[e6d74c316 CPP] (Saturnus) updated to Mednafen 1.29.0

[cd9327a10 CPP] (mGBA) updated to interim version after 0.9.3

[0d42459be Yoshi] (CPCHawk) removed redundant `AmstradCpcPokeMemory` tool

[3fe168ad0 CPP] (melonDS) updated to interim version after 0.9.4

[9a73be0e2 CPP] (SameBoy) updated to interim version after 0.14.7, fixing some bugs and adding GB palette customiser (#3185, #3239)

[c93ceae46 Yoshi] fixed typo in Snes9x sound settings bitfield (#1208)

[25fb81698 CPP] (Libretro) rewrote Libretro host implementation, fixing some crashes, adding memory domains, and slightly improving performance (#3211, #3216)

[c496c97c8 CPP] remove some render off logic (this might not be sync safe), move threaded rendering to a sync setting (this probably doesn't affect sync, but best be safe here) WRITEME

[97a11ec08 CPP] fix NESHawk mistakenly having cycle count complained about WRITEME

[db7d72be9 Morilli] update nonfunctional bsnes links WRITEME

[672ad1579 Morilli] fixed #3195 WRITEME

[688adf27e CPP] resolve #3192
seems to have been a null reference on init. saving seems to still function fine after this change WRITEME

[3d039934a Morilli] BSNESv115+: expose fast dsp and fast coprocessor options WRITEME

[339d34413 Morilli] Implement msu1 handling for bsnes115+ (#3190) WRITEME

[da2a20e55 Morilli] BSNESv115+: Some general cleanup, remove nonfunctional msu1 code WRITEME

[a68c835a4 CPP] (Gambatte) update gambatte WRITEME (m161, MBC1, HuC1, HuC3, MMM01)

[ccac4d100 CPP] Ares64 WRITEME

[3726cc629 Morilli] fixed #3173 by only calling bus.map() on initial power, not subsequent calls (#3176) WRITEME

[6e4a5a96a CPP] (melonDS) reset caches after load state WRITEME

[8b6f1c96d CPP] (melonDS) don't state sound buffers too WRITEME

[4bfb3ba48 CPP] (melonDS) don't state these big caches WRITEME

[46c2d6faf CPP] (melonDS) Cleanups & Updates & Prep For Multithread Rendering Support & Prep For DSi NAND bs (#3174) WRITEME

[9411e659b zeromus] (NesHawk) WritePrg really needs to be masking the address... there's no way it's correct, otherwise. WRITEME

[fcce7b64d zeromus] (NesHawk) relaxed restriction on vrc1 PRG registers, so they can be larger than the original vrc1 chip allows (for rom-expanding hacks)

[114124c82 CPP] BSNES Region Override (#3169) WRITEME

[4bac1bbf1 CPP] (melonDS) fixed SaveRAM not getting written to disk when unloading/reloading core (#3165)

[0ff69c560 CPP] fixed SXROM Detection (#3170) WRITEME

[7b857e7ac alyosha] SMS: only update tone on second byte write, filter out highest frequency, fixes #3160 WRITEME

## changes from 2.7 to 2.8

- Misc. changes to EmuHawk:
	- fixed various bugs with the Paths config dialog
	- fixed "Close and reload ROM" in Firmware Manager crashing if it was opened from missing FW dialog (#3054)
	- fixed typos which broke INTV, NES, and PSX gamedb lookups
	- fixed known good Saturn BIOS choices marked as unknown (#3095)
	- updated list of Sega CD / Mega CD BIOSes in firmware database
	- many gamedb additions and updates
	- fixed RAM Search difference fields can't fit all possible values (#3117)
	- fixed `File` > `Load State` > `Auto-load Last Slot` breaking movie recording/playback (#2384)
	- fixed .dsm importer
	- fixed .smv importer nagging about core choice thousands of times (#3022)
	- fixed .vbm importer
	- made OSD message duration (time to fade) configurable in UI, and allow API calls to override it
	- added option to split RAM Watch entries (#1024)
	- fixed drag+drop bug (#1483)
	- made `Config` > `Customize...` > `Pause when menu activated` behave consistently
	- fixed overlapping UI elements in Debugger (#3026)
	- tidied up numbering of save slots and TAStudio branches (#3112)
	- fixed `ObjectDisposedException` when triggering single-instance passthrough
	- fixed throttle edge cases
	- fixed edge cases where post-frame tool updates would run twice after a frame advance
	- added more logging and warning dialogs for edge cases
	- many smaller fixes and even some frontend speed optimisations (not sarcasm, this is a first)
- Linux port:
	- added support for PSX (see Nymashock below) and technically N64 and TI-83 (see Ares64 and Emu83 below)
	- fixed various problems w/ alignment and size under Mono
	- fixed hotkeys triggering accidentally when typing in Virtual Pad fields (#3087)
	- added Nix expression for reproducible EmuHawk (and DiscoHawk) builds without dependency issues
- TAStudio:
	- re-enabled editing of movie comments (#3063)
	- added an edit dialog for TAStudio palette (#2119)
	- fixed branch screenshots (#1513)
	- fixed weird edge case putting TAStudio in an unaccounted-for state (#3066)
	- fixed error when autoloading a rom and TAStudio AND a .tasproj for a different rom
	- fixed incorrect behaviour when jumping to frame 0
- Lua/ApiHawk:
	- added support for `"#RRGGBB"` format when parsing colours ("luacolor" in docs) in Lua API
	- fixed `joypad.setfrommnemonicstr` not working without reinitialising MovieSession (#2525)
- DiscoHawk:
	- fixed deadlocks (#3128)
- New and graduating cores:
	- Nymashock:
		- new PSX core ported from Mednafen (like Octoshock, though this is newer and more easily updated, which also means it has more peripherals)
	- SameBoy:
		- new ported GB/C core with comparable accuracy to Gambatte and GBHawk (not to be confused with the SameBoy SGB core in older releases)
	- Emu83:
		- new TI-83 core
	- BSNESv115+:
		- BSNESv115+ (the "new BSNES port") is no longer experimental
		- fixed Hex Editor and others using read instead of peek (#3060)
		- implemented memory callbacks
		- fixed ram and rom memory domains
		- increased MmapHeapSize to prevent crashes, affected Star Ocean
		- refactored latching to improve dumping/verification
		- reduced savestate size (increased frequency for TAStudio/rewind)
		- improved speed of fast ppu check
	- MSXHawk:
		- MSXHawk is no longer experimental
		- implemented more hardware/mappers
		- added Linux port
	- Ares64:
		- 2 new experimental cores ported from Ares: one for casual play which is slow and desync-prone ("Performance"), and one for TASing which is even slower but will sync ("Accuracy")
- Other cores:
	- A7800Hawk:
		- fixed nondeterminism caused by incorrect savestate code
		- fixed Basketbrawl and summer games
		- fixed off pixel detection in write mode, affected baby pac man
	- Atari2600Hawk:
		- fixed audio issues, affected ms pac man
	- Gambatte:
		- updated core
		- fixed error when attempting to write to some registers e.g. with the Debugger (#3028)
		- fixed crash on Linux
		- refactored GambatteLink to be more modular and add support for 3x/4x
		- added proper memory callback scopes for link play
		- enabled disassembly in link play
		- allowed linking w/ GBC cart IR
		- stopped using fine-grained timing for link play when the link is disconnected
		- added more scopes for memory callbacks
	- GBHawk:
		- updated GBA startup state, affected Konami collection vol. 4
		- fixed link play, affected perfect dark
	- Genplus-gx:
		- added support for SRAM larger than 8MB (#3073)
		- prevented crash when loading Debugger (#2769, #2958)
	- Handy:
		- fixed crash on Linux (#2425)
	- IntelliHawk:
		- adjusted sme timings and fixed accesses to registers, affected motocross
		- cleaned up CPU, fix trace logger and throw less exceptions
		- fixed intellicart, affected various homebrew roms
		- fixed MOBs with x-coord 0 being visible, affected bowling
		- fixed overflow flag calculation, affected checkers and reversi
		- more compatibility work
	- MAME:
		- stopped treating warnings as errors
		- set System Bus correctly
	- melonDS:
		- updated core, adding experimental DSiWare support
		- enabled memory callbacks
		- fixed empty strings in firmware settings preventing roms from loading (#3030)
	- Mupen64Plus:
		- fixed haptic feedback causing movie playback to crash
		- enabled GLideN64's texture wildcard support (#3104)
	- NesHawk:
		- added mapper for Pokemon bootlegs
		- fixed not parsing iNES v2 headers (#3082)
		- other small accuracy improvements and bugfixes
	- O2Hawk:
		- fixed blobbers and Popeye
	- PCEHawk:
		- fixed crash related to framebuffer resizing (#3018)
	- SMSHawk:
		- fixed sprite collision inaccuracy (#1611), affected Ecco the Dolphin
		- fixed Fray (vert lock update)
		- fixed SMS backdrop colors
		- fixed ys (JPN) by emulating VRAM masking bit
		- improved SG-1000 8kb ram adapter emulation
	- VectrexHawk:
		- implemented 64K bank switching
