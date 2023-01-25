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
	- added warning dialog when running as Superuser/Administrator
	- fixed overlapping UI elements in `Config` > `Controllers...` when Windows UI scale is not 100% (#2605)
	- fixed overlapping UI elements in Record Movie dialog when Windows UI scale is not 100% and on Linux (#2605)
	- fixed the autoselection of a movie when opening the Play Movie dialog
	- fixed `%recent%` being expanded to the wrong path
	- added warning when loading a `.bot` into the Basic Bot which was made on a different game or system, or on an older version
	- fixed window title being blank when chromeless mode is enabled
	- improved handling of host sound init failure
	- added prompt for if you start movie playback while cheats are enabled (#3389)
	- improved cheat editing UX
	- removed default bindings for virtual keyboards
	- update virtualpads immediately when the movie status changes between read-only and record mode
	- fix key releases not getting handled correctly when modifier keys are pressed (#3327)
	- fix KeyLog not being respected in TAStudio and custom LogKey getting discarded (#2843)
	- fix hang with "Go to Address" dialog in RamSearch (#3384)
	- fix a crash when selecting user shaders (#3495)
	- fix gamedb loading not blocking when loading a rom, potentially causing a miss with a slow hdd (#3489)
	- support marshalling arrays of length 0 through BizInvoker, fixing a crash with lua
	- fix crashes when setting absurdly large padding values (#3321)
	- make `DisplayConfig` defaults button set padding back to 0
	- add auto save state on close (#1861)
	- zstd compression is used instead of deflate compression where possible (rewind, .wbx cores, internal resource files, binary blobs in movies, savestates, etc)
	- hardware accelerated CRC32 and SHA1 algorithms are used if the user's hardware allows it
	- block edge cases where global `GameInfo` is uninitialised
	- improve error message for `IToolFormAutoConfig` ext. tool missing menu
	- add hash for newly discovered GBC-GBA bootrom
	- ensure there can be no edge cases involving SGXCD sysID
	- add more system ids internally (for better errors for currently unsupported systems)
	- cache `ToolStripRenderer` used by `FormBase.FixBackColorOnControls`
	- refactor `IGameInfo.FilesystemSafeName` extension
	- add and use 2 extension methods for splitting path into dir+filename
	- hardcoded certain edge cases in `MovieConversionExtensions`
	- removed gongshell, added "simple" code for opening win32 context menu (gongshell's only actual use) (#2261)
	- fixed firmware fields using a dummy checksum using the wrong firmware info (#3159)
	- added way to obtain error message in ILinkedLibManager (used to display an error code for initial library checks)
	- added quick failure if EmuHawk is somehow running as a 32 bit process (likely due to bad .NET config, see #3375)
	- fixed "Toggle All Cheats" hotkey behaviour
	- cleaned up `MainForm.CheckHotkey`
	- deduplicated some code in `MainForm`
	- did minor refactors to byteswapping (N64 rom loading and Lua bit library)
- Linux port:
	- fixed various file pickers using case-sensitive file extensions
	- added short-circuit to Mupen64Plus loading to avoid error messages and any strange failure state
	- enabled menu mnemonics (Alt+X) for MainForm
	- fixed inconsistent application of colours from GTK theme when Mono is able to use it
- Basic Bot:
	- increased max frames from 999 to 9999
	- added NOT operator
	- fixed anchor points and a misaligned label
	- fixed code logic error for 3 way tie breakers
	- refactor `BasicBot.IsBetter`
	- fixed issue where the Copy button was not toggled on/off properly
	- change addresses to `ulong?` (fixes empty address fields being saved as `0x0`)
- RAM Watch:
	- fix CTRL+A not working properly
- RAM Search:
	- added Select All/None
	- switch `_watchList` from a List to an Array (faster)
- Debugger:
	- fixed the "To PC" button not updating the disassembler view
- TAStudio:
	- fixed "Select between Markers" hotkey not working
	- fixed `.tasproj` headers being written differently based on locale (i.e. ',' instead of '.')
	- don't autorestore if current frame remained valid
	- refactors for selection in `InputRoll`, standardising behaviour of Select All and Insert Separator buttons
	- resolve some inputs showing ! in Nymashock and hide some columns by default
	- when starting new `.tasproj` from SaveRAM, don't clone SaveRAM twice
	- did minor refactor to prevent mutation local in `TAStudio.TasView_MouseDown`
	- fixed modifier key check in `TAStudio.TasView_MouseDown`
	- removed unused "TAStudio states" path
	- added 'Edit marker frame' feature
	- fixed `ArgumentOutOfRangeException` when loading TAStudio with cheats
- Lua/ApiHawk:
	- (Lua) replace the two lua engines with an updated version of NLua, backed internally by native lua 5.4
	- (Lua) rely on a system provided lua 5.4 .so (or lua 5.3 if needed) when on Linux, resolving issues due to providing our own lua
	- (Lua) add in a migration helper for lua bitwise ops (put `bit = (require "migration_helpers").EmuHawk_pre_2_9_bit();` at top of file)
	- (Lua) added arguments to memory callback functions (cb will be called with addr, val, flags)—check `event.can_use_callback_params("memory")` when writing polyfills
	- (ApiHawk) merged `IGameInfoApi` into `IEmulationApi`, and some other minor API method signature changes
	- (ApiHawk/Lua) fixed `event.onmemoryread` behaviour under mGBA (#3230)
	- (ApiHawk/Lua) improved how removing callbacks from within a callback is processed (#1823)
	- (Lua) fixed setting size of Lua Forms (#3034)
	- (ApiHawk) changed injector to include non-public properties when looking for `ApiContainer`
	- (ApiHawk) deprecated some `ExternalToolApplicability.*` attributes
	- (Lua) fixed encoding bug which caused e.g. Japanese text to become mojibake/garbled (#190, #2041)
	- (ApiHawk/Lua) added `IMovieApi.PlayFromStart`/`movie.play_from_start` (#384)
	- (ApiHawk/Lua) added `saveChanges` parameter to `IMovieApi.Stop`/`movie.stop`
	- (ApiHawk/Lua) fixed edge cases for `MemoryApi.{Read,Write}ByteRange`/`{memory,mainmemory}.read_bytes_as_{array,dict}`/`{memory,mainmemory}.write_bytes_as_array`
	- (ApiHawk/Lua) added `IUserDataApi.Keys`/`userdata.get_keys`
	- (ApiHawk) fixed trying to load tool Forms when services aren't satisfied, causing NREs (#3329)
	- (ApiHawk) fixed HTTP and sockets not being initialised in time for tools autoloaded on startup
	- (Lua) fixed the `forms.*` functions for `LuaPictureBox`es erroneously affecting every form instead of the one specified (#3395)
	- (ApiHawk) added "memory" of which ext. tools the user has vetted so the prompt does not appear when restarting EmuHawk
	- (ApiHawk/Lua) added length prefix to `ICommApi.Sockets.SendScreenshot`/`comm.socketServerScreenShot` to match `SendString`/`socketServerSend`
	- (Lua) renamed (deprecated) `event.onmemory{read,write,exec,execany}` to `event.on_bus_{read,write,exec,exec_any}` (#759)
	- (Lua) added `bizstring.pad_{start,end}` convenience functions
	- (Lua) changed Lua Console to trigger a clear of drawing surfaces and the OSD when removing a Lua script, and to reset padding when removing the last script
	- (ApiHawk) changed some parameter and return types from `List` to more suitable read-only collection types
	- (Lua) fixed `require` not looking in Lua dir on Linux
	- (Lua) fixed and updated some bundled lua scripts: `Gargoyles.lua`, `Earthworm Jim 2.lua`, `Super Mario World.lua`
	- (Lua) added "Clear Output" button to Lua Console
	- (Lua) fix documentation error in `client.gettool`
	- (ApiHawk/Lua) have `MemoryDomain` inherit `IMonitor`, which can be used to avoid waterbox overhead for many nonsequential memory accesses (already used internally to speedup RAM Search and some lua functions; no-op for non-waterbox cores) (#3296)
	- (Lua) set `Form.Owner` to MainForm for Lua-made forms
	- (Lua) fixed wiki export, add more notes to fill in some of the holes
	- (Lua) documented frameadvance loop
	- (Lua) documented socket response format
- Meta:
	- adjust wording in Issue templates
	- add core port request Issue template
	- add contributor's guide
	- add more testroms to GB testroms project
	- updated `PcxFileTypePlugin.HawkQuantizer` project file to match others
- New and graduating cores:
	- Ares64:
		- removed the Ares64 (Performance) core and renamed Ares64 (Accuracy) to Ares64, now no longer experimental
		- updated to interim version after v130.1
		- integrated Angrylion-rdp for RDP and VI emulation, avoiding many issues the MAME RDP had
		- fixed A/V Sync when interlaced
		- added Transfer Pak support and N64 Mouse support
		- added more debugging features (tracer, disassembler, get registers, System Bus domain)
		- fixed tracer regression from upstream update
		- enabled SIMD RSP implementation
	- VirtualJaguar
		- new core for Jaguar and Jaguar CD emulation!
		- core has had a fair bit of modifications from upstream for better accuracy and Jaguar CD support
		- due to the lack of multisession support, a CD has to be split into multiple CDs for each session currently
	- TIC-80:
		- added a new core for the TIC-80 fantasy computer, using nesbox's own reference implementation
		- added settings for enabling/disabling controllers
	- SubBSNESv115+:
		- subframe capable variant of the BSNESv115+ core (#3281)
		- allows subframe inputs and delayed resets
	- MAME:
		- technically not "new" as c# side code was always present, the actual MAME library is now included in the main package (although still experimental)
		- MAME has been waterboxed, hopefully fixing all sync issues
		- added in various missing mnemonics (more likely remain, please report!)
		- resolved erroneous LibMAME errors due to mame_lua_get_string returning NULL with an empty string
		- use actual doubles for figuring out aspect ratio (fixes potential divide by 0 exception)
		
	- DobieStation:
		- This PS2 core has been removed due to being unusuably slow and not very accurate
- Other cores:
	- A2600Hawk:
		- fixed crash when pushing Select on Karate title screen
	- (old) BSNES:
		- fixed graphics debugger exception when freezing a tile (#3195)
		- removed libspeex dependency
		- fixed a possible `IndexOutOfRangeException` in the graphics debugger (#3399)
		- also fixed a potential `DivideByZeroException` (#3398)
	- BSNESv115+:
		- made this core default in places where the old BSNES core was
		- reworked Payload peripheral and fixed Virtual Pads
		- improved peripheral selection for P1
		- reimplemented MSU1 properly
		- added fast DSP and fast coprocessor settings
		- fixed crash when loading a savestate after a reset (#3173)
		- added region override setting
		- added overscan and aspect ratio correction settings
		- implemented an `ExtendedGamepad` controller which acts like a normal gamepad with 4 extra buttons
		- pulled upstream, fixed justifier controller and applied misc. core fixes
		- added option to disable ppu sprite limit (#3440)
		- implemented SNES graphics debugger
		- updated internal sameboy version for SGB by linking it to the standalone sameboy core, fix SGB saveRAM
		- fixed CARTROM and CARTRAM memory domain names (#3405), provide SGB memory domains, set MainMemory and SystemBus domains properly
		- provide a more proper `IBoardInfo`, provide `SGB` SystemId when in SGB mode
	- SubBSNESv115+:
		- fix LsmvImport in numerous ways and import as SubBSNESv115 movies to allow handling subframe inputs and delayed resets
	- CPCHawk:
		- removed redundant `AmstradCpcPokeMemory` tool
	- Cygne:
		- allowed .pc2 (Pocket Challenge v2) files to be loaded
	- Faust:
		- updated to Mednafen 1.29.0
	- Gambatte:
		- improved MBC1/MBC1M emulation
		- improved HuC1 emulation and implement HuC1 IR support
		- improved HuC3 emulation and implement support for mapper sound (HuC3 is currently the only use case)
		- implemented MMM01 emulation
		- implemented M161 emulation
		- improved heuristics for various multicart mappers and remove the multicart detection setting (now effectively always true)
		- implemented remote control controls, expanded remote control emulation for HuC1 IR and CGB IR (previously only HuC3 IR had this implemented, using a hardcoded value)
		- cleaned up the mapper internals, IR, and RTC code
		- made various optimizations to the CPU loop and read/write code (around 10-15% performance increase)
		- trimmed down initial time settings to a single setting, using total number of seconds
		- implemented quirk with bit 4 of rLCDC, fixes cgb-acid-hell testrom compliance
		- fixed sprite priority in CGB-DMG mode
		- prevent crashes due to "negative" numbers being added to the sound buffer pointer (#3425)
		- fixed audio output being too quiet (#3338)
		- added a CGB color correction option using the same formula as SameBoy, and made that the default
	- GBHawk
		- fixed Code-Data Logger crashing due to typo'd mem domain name (#3497)
	- Genplus-gx:
		- stopped byteswapping Z80 domains (#3290)
		- changed default peripheral to 3-button Genesis gamepad (#2775, #3262)
		- added option to disable ppu sprite limit (#3440)
		- prevent svp dereferences when not using an svp cart (#3297)
		- give NHL 96 (Genesis) SRAM (#3300)
		- fixed disc swapping, re-enabled the disc buttons
		- fixed disabled layers being wrongly re-enabled on a load state (#3388)
		- fixed pattern cache invalidation (#3363)
	- HyperNyma:
		- updated to Mednafen 1.29.0
	- Libretro:
		- rewrote Libretro host implementation, fixing some crashes, adding memory domains, and slightly improving performance (#3211, #3216)
		- fixed input display (#3360)
		- implemented needed environment functions for resolution changes
		- added reset support (#3482)
	- melonDS:
		- updated to interim version after 0.9.5
		- fixed SaveRAM not getting written to disk when unloading/reloading core (#3165)
		- implemented threaded renderer support
		- replaced darm with a new DS centric disassembler, fixing various issues with tracing/disassembly
		- r13/r14/r15 reported as sp/lr/pc for tracelogs
		- split ARM7/touch screen polls to an "alt lag" variable and added a setting for whether to consider this "alt lag" (#3278)
		- reduced state size a bit
		- improve audio resampling; get rid of libspeex dependency
		- added missing TMD for Zombie Skape, improved error message when TMD cannot be found
		- ensured firmware settings match up with sync settings if real firmware is not used (#3377)
		- did various internal cleanups
	- mGBA:
		- updated to interim version after 0.10.0, fixing a softlock in Hamtaro: Ham Ham Heartbreak (#2541)
		- implemented save override support with EEPROM512 and SRAM512
	- Mupen64Plus
		- always savestate expansion pak regardless of settings, resolves some desyncs/crashes due to shoddy no expansion pak implementation (#3092, #3328)
		- fixed changing expansion pack setting
		- added angrylion as yet another graphics plugin
		- fixed mistake in angrylion implementation (#3372)
	- NeoPop:
		- updated to Mednafen 1.29.0
	- NesHawk:
		- relaxed restriction on VRC1 PRG registers, so they can be larger than the original VRC1 chip allows (for rom-expanding hacks)
		- fixed PGR writes for VRC1 not using mask for address
		- fixed SXROM detection (#3168)
		- fixed crash for Namco 163 mapper (#3192)
		- "un-implemented" `ICycleTiming` so that NesHawk no longer tries to use the cycle timing intended for SubNesHawk
	- Nymashock:
		- updated to Mednafen 1.29.0
		- fixed disc switching
		- fixed light guns (#3359)
		- wired up rumble support
	- SameBoy:
		- updated to interim version after 0.15.7, fixing some bugs (#3185)
		- added GB palette customiser (#3239)
		- wired up rumble support
	- Saturnus:
		- updated to Mednafen 1.29.0
		- fixed disc switching
		- fixed light guns (#3359)
	- SMSHawk:
		- fixed `InvalidOperationException` when using SMS peripherals (#3282)
		- fixed screechy/static audio during Sega logo in Ys (Japan) (#3160)
	- Snes9x:
		- fixed typo in sound settings bitfield (#1208)
	- T. S. T.:
		- updated to Mednafen 1.29.0
	- TurboNyma:
		- updated to Mednafen 1.29.0
	- Virtu:
		- fixed some internal state not being overwritten by savestates
		- changed RTC to use deterministic time when recording instead of (host) system time
	- VirtualBoyee:
		- refactored core to use the same Nyma system as the other Mednafen ports
		- updated to Mednafen 1.29.0
	- ZXHawk:
		- removed redundant `ZXSpectrumPokeMemory` tool
[HEAD]

[7703ee5f37 Yoshi] Refactor `IGameboyCommon.IsCGBMode`

[767e30eee5 Yoshi] Also rename bundled CPC firmware files (see #3494)
fixes 5be8b0aab

[a680739c6e Yoshi] Rename bundled ZX Spectrum firmware file (resolves #3494)
fixes 5be8b0aab

[2989a73430 CPP] workaround ares state size being blown up, fix compilation issue in some gcc versions

[b3c7f0fa48 CPP] IPlatformLuaLibEnv -> ILuaLibraries / Win32LuaLibraries -> LuaLibraries, cleanup usage of it, fix doc error in client.gettool

[f101cb5a54 Yoshi] Additional corrections to newly-added Lua documentation
fixes 49cd836e1, c7781d1c1

[29443dae49 CPP] fix #3484

[c4f4c793da Yoshi] Remove unused `IPlatformLuaLibEnv` implementation

[5197c36a5d Yoshi] Remove `[Lua*StringParam]` as they're no longer relevant
fixes 45fbdb484

[5c0143d6f6 Yoshi] Minor corrections to newly-added Lua documentation
fixes 1452f831a, 82c3b471a, b687dea1b, 49cd836e1

[49cd836e18 CPP] log warning when using the deprecated lua bit functions

[1fc08e3d95 CPP] Use NLua's MethodCache if possible for MethodBase based lua functions (see https://github.com/TASEmulators/NLua/commit/0ed3085ec301fe4da6751ca545407f9d264b0e83)

[01ab9416b5 kalimag] Make script paths in .luases relative to .luases path
Restore behavior before 99dc0e03df4c4cd20420507a06dd9987cbdf7140

[7fdc3f992d Yoshi] Propagate success through to caller for movie load/restart

[e0a7a39b0d Yoshi] Have `IMovieApi.Stop` implementation use `MainForm.StopMovie`

[596e8d9198 kalimag] Call `onexit` and cleanup when removing lua script

[50fc7e28da kalimag] Make "Stop all scripts" behave the same as toggling them off

[817b258a79 kalimag] Remove relative path manipulation in `LuaConsole`

[f625771cd0 kalimag] Don't create FileSystemWatcher for missing directories

[cf2b83b102 kalimag] Disable lua script if loading fails

[bd53807b0f kalimag] Store `LuaFile` `FileSystemWatcher` in dictionary
Avoid path string comparisons, `FileSystemWatcher` events may format relative paths differently

[6aa7c48402 kalimag] Update Lua registered functions window after restart
Make registered functions window show functions for new LuaImp after core restart/reopening the Lua console

[ee66faba0b kalimag] Clean up old LuaImp before creating new one
Prevents memory/resource leak
Causes open forms to be closed on core restart

[3a70fb65f8 kalimag] Refactor disabling Lua script into separate method

[7c7ac64ae6 kalimag] Stop discarding Lua session save directory
Previously any path would into ".\foo.luases" and be saved in exe dir

[9ee788195a kalimag] Improve Lua `FileSystemWatcher` thread safety
Make FSW invoke the entire event handler on main thread.
Avoids theoretical race condition and thread safety issues with the linq query.

[733a8bee88 kalimag] Dispose FileSystemWatchers in LuaConsole

[28d6415190 kalimag] Remove running scripts before loading session
Clean up scripts instead of just clearing script list

[cc10de4033 kalimag] Refactor removing Lua scripts into separate method

[564a1e4a67 kalimag] Remove obsolete `LuaConsole.RunLuaScripts`
Method is mostly a duplicate of `EnableLuaFile`, only called when loading a session or an already loaded script.
In either case it didn't actually start the scripts due to an inverted condition, and would stop running scripts without doing the required cleanup.

[5d143ca879 kalimag] Properly start scripts after loading Lua session
Previously, scripts would display as enabled but not actually run until toggled off and on.

[0effd435f6 kalimag] Fix issues when opening same Lua script multiple times

[14e713837b kalimag] Change remaining `Config.DisableLuaScriptsOnLoad` refs to Settings
Resolve inconsistent use of duplicate property on `Config` and `LuaConsoleSettings`, finishes partial refactoring from 324a50a
This will effectively reset this setting to default in existing configs.

[102874e480 Yoshi] Fix N64 header detection being swapped (fixes #3477)
fixes abeaa2a10
how ironic

[62f6f3b471 Yoshi] Fix Win32LuaLibraries init'ing incorrectly on `DeveloperBuild = false`

[b04260bee7 CPP] fix unwrapped lua exceptions not being correctly thrown

[b687dea1b0 CPP] change every IntPtr<->int cast to IntPtr<->long. we got 64 bit integers with lua now, and a pointer is 64 bits, so might avoid some dumb bug due to truncations and some ungodly amount of ram being used TODO: see if we can skip this cast nonsense. the lua tests indicate IntPtr should pass through fine, being considered "userdata", probably better so the user can't just pass raw numbers for the handle.

[bc79664461 CPP] fix implicit lua number to .net conversion fix .net exceptions not halting the running script fix inconsistency with stdout and lua console printing errors, both should end in a newline now print the inner exception of a lua exception (i.e. the .net extension) if possible

[eb00019c86 CPP] fix passing numbers for string args in .net lua functions (old engine had this behavior, granted "bad user" if they relied on this), add appropriate test fix passing sbyte/char as args, add appropriate tests cleanup the lua auto unlock hack, using a nice ref struct + dispose to handle it

[920682688b CPP] deprecate lua bit functions which have direct operator counterparts in new lua

[dcd570bf87 CPP] fix mainmemory.write_bytes_as_dict

[abeaa2a106 Yoshi] Be less lazy about N64 header detection in byteswapper
fixes 82c3b471a, 9660c16a0

[9660c16a0a CPP] fix N64 roms coming through multidisk bundler in ares

[84d2866f53 Yoshi] Clean up usage of `LuaFile.Enabled`/`Paused`

[f798021bba CPP] CloseRom acts like rebooting the core, so make it just reset Lua libs (more properly fixes #3226 without any yield nonsense) Slight revert of 2efae13af4f9dd5ca233a31b9085195829f6a513 (still want to set running scripts as it's used later) Fix detaching registered functions (old logic was broken, Stop would null out the LuaRef used for creating the new dummy thread for the detached function. best solution i've come up with is to simply pass a callback over for creating the thread, nicely encapsulating that functionality) Various cleanups, don't need VS complaining about old pattern matching code here anymore...

[51f01efdc4 CPP] Properly handle errors when running a lua script, using Resume/Yield methods added to the LuaThread class (see https://github.com/TASEmulators/NLua/commit/f904fa0d53b06c67dd8e9b409dcbb9fa8aa721f2)

[2efae13af4 CPP] prevent some NREs occurring with the new Lua stuff

[42455ac4a3 Yoshi] Fix syntax in `defctrl.json` and remove empty objects

[6381448472 Yoshi] decimal is not floating-point
fixes fdbb34dff

[5603e5ac01 Yoshi] Reorder items in Tools menu

[3dcc3ff89f Yoshi] Improve handling of exceptions thrown in `Form.Load` handlers
obviously only benefits forms inheriting `FormBase`

[9393e1b764 CPP] Fix #3417 and improve handling of `default.tasproj` (squashed PR #3462)
* Fix #3417 and improve handling of default.tasproj
* expose SetMovieController in the MovieSession interface (please don't rely on it anywhere else)
* don't use this explicit public in the interface
(is this mentioned anywhere? i assume this is proper style)
* use this helper function

[10a38270e5 feos] forgot a char

[bace52c4f8 feos] fix #2119

[45fbdb4844 CPP] Move to NLua/KeraLua/Lua5.4 (#3361)

[fdbb34dff6 CPP] Lua tests (#3373)

[92c1cdff22 CPP] RetroAchievements Support (#3407)

[eb1cef1ffc CPP] update mame to 0.250

[8818f79bb0 CPP] actually make N64DD support work

[6baee38717 CPP] add n64 to multidisk bundler list

[9420c8b21c CPP] merge latest ares, hook up its new N64DD support, make ares use AxisContraint (see #3453), some other cleanups here

[c23b063733 CPP] basic virtualpad + default controls for TIC80, mark it as released

[bae71326bf CPP] Fix hex editor for MAME when Open Advanced is not used

[9a0403617b Yoshi] Clean up SHA1

[f9ac3c4b32 Yoshi] Clean up `MainForm.ExternalToolMenuItem_DropDownOpening`

[e269bfd49f adelikat] Log window - when copying pasting "MD5:2345" and Sha1, strip the md5 and sha1 out.  I just want the number if I'm copying pasting the single line.  If someone finds this objectionable, feel free to revert, but this savesme a lot of time

[683aa263a0 Yoshi] Include `ControllerDefinition._orderedControls` in clone ctor
I don't think this is used, but as the caching was new in 2.8, going to include
this just in case

[0711c2b1d6 Yoshi] Also downcase Odyssey² gamedb filename in import line
fixes 5a4dc9fd8

[248e87b6d1 CPP] try to load a different core if an autodetected mame rom ends up failing to load

[5a4dc9fd88 Yoshi] Downcase Odyssey² gamedb filename to match others

[937872eaf6 Yoshi] Fix malformed PC Engine gamedb entry
broken since addition in 8295e6d65 ("Sounds" was interpreted as the sysID)

[a5ab31643f Yoshi] Remove malformed SMS gamedb entry
reverts d6d2e4c6f
(it's missing a tab, plus this is a duplicate of the entry above)

[51826c4c17 CPP] Fix wrong MBC5 mapper being given a battery
0x1A is MBC5+RAM, 0x1B is MBC5+RAM+BATTERY

[0bd182e6cc CPP] properly handle "NO GOOD DUMP KNOWN" mame rom hashes (note, these roms are not actually in the romset, so the singular hash in movies doesn't have to be affected here)

[2804ad3041 CPP] fix crashes in mame due to bad single thread handling

[44944e1d70 CPP] more simple string and double handling, allow SaveRAM usage with different bios files

[d0266816a5 CPP] Fix #3448. Support MAME 7z romsets

[5ae4470466 CPP] Correct floating point arg support with msabi<->sysv adapter
While msabi and sysv do agree what to do with floating point args for 4 floating point args (pass in xmm0-4), they dont agree what to do with mixing
msabi will choose the register corresponding with argument position. so if you have (int foo, float bar), bar will use xmm1
sysv instead will choose the first register available in the group. so with the previous example, you instead have bar using xmm0
the simple solution is to simply prohibit mixed args for now. maybe someday we could support mixing, but that's probably overkill (best use a struct at that point)

[62c3b4b8e3 CPP] Use a small dll for handling the msabi<->sysv adapter (#3451)
make a small dll for handling the msabi<->sysv adapter, using only assembly (taken from generated optimized rustc output) and handcrafted unwind information (c# exceptions in a callback seem to work fine in testing)
additionally, allow floating point arguments. this really only needs to occur on the c# side. msabi and sysv agree on the first 4 floating point args and for returns, so no work actually has to be done adapting these
with assembly being used, we can guarantee rax will not be stomped by compiler whims (and avoid potential floating point args from being trashed)

[fd2772707b Yoshi] Update `forms.drawImageRegion` documentation with a diagram
only embeds on TASVideos Wiki, which I held off on updating because there are a
lot of changes and we can do them all at once

[c8d4e606af CPP] suppress updates while rebooting core, fixes #3424

[b81728b2dc CPP] Correctly account for multiboot GBA ROMs, fixes #3421

[ad85be7bed Prcuvu] Register TCM areas for melonDS core (#3420)
* Register TCM areas for melonDS core
* reorder mem domains a bit, add TCM to ARM9 System Bus, build
Co-authored-by: CPP

[9528a2030f CPP] GBS support using SameBoy

[0c6f0523a0 CPP] Update sameboy, expose audio channel enable/disabling, cleanup settings to go through a single call/struct

[7f8b4b8c87 CPP] fix whitespace in default controls (fixes 8732e561a1d70974ad60ca145b1aeed03ca8cc45)

[a59d66dfdd CPP] proper fix for mmult opcode, properly fixes Baldies music

[7efafc18da Yoshi] Extract helper code for Analyzers and Source Generators

[1fbb95a353 Yoshi] Make MSBuild ignore shell scripts for external .NET projects

[04fcf59afe Yoshi] Update C++ FlatBuffers lib, check in new codegen, and rebuild cores

[158c897702 Yoshi] Use `Google.FlatBuffers` NuGet package and check in new Nyma codegen

[cf0053fd3c Yoshi] Update FlatBuffers codegen script for Nyma cores
uses latest (they switched from SemVer to dates, so 22.9.24 follows 2.0.8)
works on real Linux, using Nix if installed

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
