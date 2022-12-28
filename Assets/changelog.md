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
- Linux port:
	- fixed various file pickers using case-sensitive file extensions
	- changed default Lua engine to "NLua+KopiLua" which doesn't seem to crash on normal Mono builds like the other one does
	- added short-circuit to Mupen64Plus loading to avoid error messages and any strange failure state
	- enabled menu mnemonics (Alt+X) for MainForm
	- fixed inconsistent application of colours from GTK theme when Mono is able to use it
- TAStudio:
	- fixed "Select between Markers" hotkey not working
	- fixed `.tasproj` headers being written differently based on locale (i.e. ',' instead of '.')
- Lua/ApiHawk:
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
- New and graduating cores:
	- Ares64:
		- removed the Ares64 (Performance) core and renamed Ares64 (Accuracy) to Ares64, now no longer experimental
		- updated to interim version after v130.1
		- integrated Angrylion-rdp for RDP and VI emulation, avoiding many issues the MAME RDP had
		- fixed A/V Sync when interlaced
		- added Transfer Pak support and N64 Mouse support
		- added more debugging features (tracer, disassembler, get registers, System Bus domain)
		- fixed tracer regression from upstream update
	- TIC-80:
		- added a new experimental core for the TIC-80 fantasy computer, using nesbox's own reference implementation
	- SubBSNESv115+:
		- subframe capable variant of the BSNESv115+ core (#3281)
		- allows subframe inputs and delayed resets
	- DobieStation:
		- This PS2 core has been removed due to being unusuably slow and not very accurate
- Other cores:
	- A2600Hawk:
		- fixed crash when pushing Select on Karate title screen
	- (old) BSNES:
		- fixed graphics debugger exception when freezing a tile (#3195)
		- remove libspeex dependency
		- fix a possible `IndexOutOfRangeException` in the graphics debugger (#3399)
		- also fix a potential `DivideByZeroException` (#3398)
	- BSNESv115+:
		- make this core default in places where the old BSNES core was
		- reworked Payload peripheral and fixed Virtual Pads
		- improved peripheral selection for P1
		- reimplemented MSU1 properly
		- added fast DSP and fast coprocessor settings
		- fixed crash when loading a savestate after a reset (#3173)
		- added region override setting
		- added overscan and aspect ratio correction settings
		- implement an `ExtendedGamepad` controller which acts like a normal gamepad with 4 extra buttons
		- pull upstream, fix justifier controller and apply misc. core fixes
		- added option to disable ppu sprite limit (#3440)
		- implement SNES graphics debugger
		- update internal sameboy version for SGB by linking it to the standalone sameboy core, fix SGB saveRAM
		- fix CARTROM and CARTRAM memory domain names (#3405), provide SGB memory domains, set MainMemory and SystemBus domains properly
		- provide a more proper `IBoardInfo`, provide `SGB` SystemId when in SGB mode
	- SubBSNESv115+:
		- fix LsmvImport in numerous ways and import as SubBSNESv115 movies to allow handling subframe inputs and delayed resets
	- CPCHawk:
		- removed redundant `AmstradCpcPokeMemory` tool
	- Faust:
		- updated to Mednafen 1.29.0
	- Gambatte:
		- improved MBC1/MBC1M emulation
		- improved HuC1 emulation and implement HuC1 IR support
		- improved HuC3 emulation and implement support for mapper sound (HuC3 is currently the only use case)
		- implemented MMM01 emulation
		- implemented M161 emulation
		- improved heuristics for various multicart mappers and remove the multicart detection setting (now effectively always true)
		- cleaned up the mapper internals, IR, and RTC code
		- made various optimizations to the CPU loop and read/write code (around 10-15% performance increase)
		- trimmed down initial time settings to a single setting, using total number of seconds
		- implemented quirk with bit 4 of rLCDC, fixes cgb-acid-hell testrom compliance
		- fixed sprite priority in CGB-DMG mode
		- Prevent crashes due to "negative" numbers being added to the sound buffer pointer (#3425)
		- fixed audio output being too quiet (#3338)
	- GBHawk
		- fixed Code-Data Logger crashing due to typo'd mem domain name (#3497)
	- Genplus-gx:
		- stopped byteswapping Z80 domains (#3290)
		- changed default peripheral to 3-button Genesis gamepad (#2775, #3262)
		- added option to disable ppu sprite limit (#3440)
	- HyperNyma:
		- updated to Mednafen 1.29.0
	- Libretro:
		- rewrote Libretro host implementation, fixing some crashes, adding memory domains, and slightly improving performance (#3211, #3216)
		- fixed input display (#3360)
	- melonDS:
		- updated to interim version after 0.9.5
		- fixed SaveRAM not getting written to disk when unloading/reloading core (#3165)
		- implemented threaded renderer support
		- replaced darm with a new DS centric disassembler, fixing various issues with tracing/disassembly
		- split ARM7/touch screen polls to an "alt lag" variable and added a setting for whether to consider this "alt lag" (#3278)
		- reduced state size a bit
		- improve audio resampling; get rid of libspeex dependency
		- did various internal cleanups
	- mGBA:
		- updated to interim version after 0.10.0, fixing a softlock in Hamtaro: Ham Ham Heartbreak (#2541)
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
	- SameBoy:
		- updated to interim version after 0.14.7, fixing some bugs and adding GB palette customiser (#3185, #3239)
	- Saturnus:
		- updated to Mednafen 1.29.0
		- fixed disc switching
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

[c2a5b37799 CPP] Update contributing guide for the new Lua setup

[bc12fcca87 Yoshi] Minor revision to EmuHawk contribution guide

[c7c5ed229d CPP] add pc2 extension for wonderswan
these are pocket challenge v2 roms, which is some handheld system which is actually just a wonderswan inside so these roms work anyways with cygne

[91ce98ef12 CPP] better handle lua on linux, be compatible with lua 5.3 (we don't actually use any API exclusive to 5.4 so no real change in this case)

[66c19cfcb2 natt] Support marshalling arrays of length 0 through BizInvoker
Such arrays will be marshalled with valid and unique pointers that can be compared but not read from or written to.

[7703ee5f37 Yoshi] Refactor `IGameboyCommon.IsCGBMode`

[339915c013 CPP] check-in NLua to main repo combine NLua with KeraLua (KeraLua is "gone" now I guess) make it use the BizInvoker (so now it can properly handle the liblua5.4.so and lua54.dll names differing), also delete the liblua54.so. minor speedup when creating a new empty table make lua default to UTF8 internally, so we don't need to manually change the state's encoding

[a1da5753ee CPP] check-in NLua to main repo combine NLua with KeraLua (KeraLua is "gone" now I guess) make it use the BizInvoker (so now it can properly handle the liblua5.4.so and lua54.dll names differing), also delete the liblua54.so. minor speedup when creating a new empty table make lua default to UTF8 internally, so we don't need to manually change the state's encoding

[767e30eee5 Yoshi] Also rename bundled CPC firmware files (see #3494)
fixes 5be8b0aab

[8d5f7b5478 CPP] make selecting user shaders not crash with the default empty string path Path.GetDirectoryName throws if it is handed an empty string apparently

[a680739c6e Yoshi] Rename bundled ZX Spectrum firmware file (resolves #3494)
fixes 5be8b0aab

[2989a73430 CPP] workaround ares state size being blown up, fix compilation issue in some gcc versions

[2187602dc1 CPP] fix Package.sh too fixes 91e400bdd9959fff3c6f7cc9f2e9a4be255603c8

[91e400bdd9 CPP] fix QuickTestBuildAndPackage to include the "overlay" folder (meant for RetroAchievements stuff)

[767cc9059d CPP] Improve handling of RA http requests, add some handling in case RA sound files are missing. Normally this shouldn't be needed as docs specify if the wav file fails to load it plays the default beep sound, except actually it just throws in practice??? The 2.9 rcs apparently have the "overlay" folder missing, so the sound files aren't present. I'm assuming there's some issue with build scripts there for releases...

[27f6800d45 CPP] fix #3489 (InitializeWork is called by itself for each gamedb file #include'd, so the event would have been set once the first gamedb file is loaded, oops), do some other cleanup here

[b3c7f0fa48 CPP] IPlatformLuaLibEnv -> ILuaLibraries / Win32LuaLibraries -> LuaLibraries, cleanup usage of it, fix doc error in client.gettool

[f101cb5a54 Yoshi] Additional corrections to newly-added Lua documentation
fixes 49cd836e1, c7781d1c1

[c7781d1c17 Yoshi] Add Lua migration helper library for bitwise ops
see 49cd836e1, #3485
put `bit = (require "migration_helpers").EmuHawk_pre_2_9_bit();` at top of file
can now easily add helpers for migrating from other emulators

[9e4836d300 CPP] libretro handling cleanup, reorg some of this, fix some input cases, better domain names funsie found in this cleanup: can't use `in` params with the BizInvoker as it doesn't like the read only semantics (results in some exception in CreateType)

[64d693e63f CPP] call retro_unload_game before retro_deinit (libretro api specifies retro_unload_game be called before retro_deinit, in practice cores don't really care but best fix this)

[29443dae49 CPP] fix #3484

[c4f4c793da Yoshi] Remove unused `IPlatformLuaLibEnv` implementation

[5197c36a5d Yoshi] Remove `[Lua*StringParam]` as they're no longer relevant
fixes 45fbdb484

[5c0143d6f6 Yoshi] Minor corrections to newly-added Lua documentation
fixes 1452f831a, 82c3b471a, b687dea1b, 49cd836e1

[49cd836e18 CPP] log warning when using the deprecated lua bit functions

[56d66ca555 CPP] add reset support to libretro, resolves #3482

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

[eec86ad81a CPP] Use actual doubles for figuring out aspect ratio Fixes issues when mame sends over < 1 bounds which round down to 0 with a long cast (resulting in div by 0 exceptions) Also fix some oopsies with incorrect function signatures. Remove MAME string docs as they aren't really relevant anymore, as only MameGetString handles lua string handling now

[64044845a6 CPP] resolve erroneous LibMAME errors due to mame_lua_get_string returning nullptr with an empty string (now will only do so on an error) add back in mame_lua_get_double, to be used to resolve other issues (c# code pending...)

[715f4f497c CPP] add some missing mame mnemonics

[066297d5e7 CPP] MAME Waterbox (#3437)

[fd2772707b Yoshi] Update `forms.drawImageRegion` documentation with a diagram
only embeds on TASVideos Wiki, which I held off on updating because there are a
lot of changes and we can do them all at once

[8ee75879e6 CPP] Rework MAME integration a bit
The periodic callback is now used as a way to service "commands" sent from the main thread
Upon servicing a command, the mame thread will set the current command to NO_CMD then wait for the main thread allow the mame thread to continue
During this wait, the main thread may optionally set the next command (done here for STEP -> VIDEO), ensuring the next callback will service that command
A dummy "WAIT" command can be sent to trigger this waiting behavior, allowing the main thread to safely touch mame while the mame thread is frozen (important for mem accesses and probably savestates?)
A/V sync is also reworked. We can assume that 1/50 of a second worth of samples will be sent each sound callback. We can also assume 1/FPS of a second worth of time will be advanced each frame advance
So, we can just give hawk 1/FPS worth of samples every GetSamplesSync, if they are available. If we have less (probable on first few frames), we'll just give all the samples, and hope it balances out later.

[c8d4e606af CPP] suppress updates while rebooting core, fixes #3424

[b81728b2dc CPP] Correctly account for multiboot GBA ROMs, fixes #3421

[ad85be7bed Prcuvu] Register TCM areas for melonDS core (#3420)
* Register TCM areas for melonDS core
* reorder mem domains a bit, add TCM to ARM9 System Bus, build
Co-authored-by: CPP

[f29113287e CPP] add Jaguar to MultiDiskBundler menu

[82c3b471a5 Yoshi] Minor refactors to byteswapping (N64 rom loading and Lua bit library)

[2fa46efda6 CPP] add jaguar db, change db parser to prefer the strongest hash available, fix potential edge case if a crc32: prefix is present (and simplify the code)

[c547a20f8e CPP] Use `LoadOther` for GBS files, cleanup the code a little, update SameBoy version info

[9528a2030f CPP] GBS support using SameBoy

[0c6f0523a0 CPP] Update sameboy, expose audio channel enable/disabling, cleanup settings to go through a single call/struct

[7f8b4b8c87 CPP] fix whitespace in default controls (fixes 8732e561a1d70974ad60ca145b1aeed03ca8cc45)

[8732e561a1 CPP] Better Jaguar Virtual Width/Height Jaguar VPad Proper Jaguar default controls Remove a lot of unneeded `ReSharper disable once UnusedMember.Global` in vpads (Global has been gone for a while now...) Set VirtualJaguar to released

[71f2676ad8 CPP] more virtualjaguar cleanup, fix various bugs, have the DSP run more in sync with the CPU/GPU (makes Zoop boot, Doom is much less laggy, various missing sound issues are no longer present)

[2e5c62f632 CPP] default Jaguar bios to not be skipped, some games rely on Jaguar bios running on boot

[be771c134c CPP] default to KSeries bios instead of MSeries bios, as it seems to be more compatible

[5f509525bc CPP] (virtualjaguar) less screen changing spam

[98be50057a CPP] (virtualjaguar) proper mulitwidth support, fixes Doom

[a59d66dfdd CPP] proper fix for mmult opcode, properly fixes Baldies music

[7efafc18da Yoshi] Extract helper code for Analyzers and Source Generators

[1fbb95a353 Yoshi] Make MSBuild ignore shell scripts for external .NET projects

[929b034321 CPP] fix bad fix to risc jr, & 0x10 seems to be correct according to battle morph and blue lightning...

[95c06e0b6e CPP] better fix for d7810f6ea90e3674a56c728848109c3f54a54906, remove incorrect comment

[f1a3e02e89 CPP] revert some of baa3bdf948f79bcb768e360a80e3cbadca4ac598, as it caused regressions elsewhere, make GPU interrupts not stupid (makes Myst stop crashing with NTSC)

[baa3bdf948 CPP] (virtualjaguar) rework dsp and gpu opcodes to come from the same source, fix some wrong opcodes (partially fixes Club Drive and Baldies music)

[04fcf59afe Yoshi] Update C++ FlatBuffers lib, check in new codegen, and rebuild cores

[158c897702 Yoshi] Use `Google.FlatBuffers` NuGet package and check in new Nyma codegen

[cf0053fd3c Yoshi] Update FlatBuffers codegen script for Nyma cores
uses latest (they switched from SemVer to dates, so 22.9.24 follows 2.0.8)
works on real Linux, using Nix if installed

[ddac50eb30 Yoshi] Pull musl submodule

[9e9e041bba Yoshi] Fix AOoRE when loading TAStudio w/ cheats(?)
partially reverts dd4f9aaf6
InputRoll code is too inscrutable for me to determine the actual cause so I just
left a `Debug.Assert`

[d7810f6ea9 CPP] fix buggy TOM writes, fixes Baldies

[f8a4524df7 CPP] add in missing platform framerates, add in Jaguar CD iding for movie file, fix aspect ratio for jaguar

[f0529fde28 CPP] (virtualjaguar) stop a CD transfer when address is greater than the end, rather than greater than or equal to, fixes battle morph

[80cf3a0c48 CPP] (virtualjaguar) memtrack support, fix bug with event system, various cleanups

[94bb881d00 CPP] better completely wrong cd timings, fix some bad risc opcodes, fixes FMVs in jag cd games

[b84ef509ec CPP] fix circular buffer, fix 24bpp mode

[bc83c9c917 CPP] adjust jagcd timing
this is still very wrong (needs something smarter for timing) but seems to make games intros not puke garbage sound now

[c2ae5bfa0e CPP] jagcd cd_initm support, fix some bugs

[1f9337d225 CPP] fix loading in byteswapped jag cds

[ceff5f3e90 CPP] jagcd progress, fix jaguar lag when dsp is polling inputs

[3cbdd36fe0 Yoshi] Deduplicate some code in `MainForm`

[728f393eb1 Yoshi] Clean up `MainForm.CheckHotkey`

[d7c79a5f03 Yoshi] Fix "Toggle All Cheats" hotkey behaviour re: separators

[3122078dfd CPP] tweak jaguar cpu tracelogger

[ff5c8d4e52 CPP] more jag disasm tweaks

[c92c2bf661 CPP] fix jag risc disasm

[38b4b98fc0 CPP] minor touchups to jag risc disassembler

[c9618b3f92 CPP] c# side to jag debugging improvements

[740cd1f8d4 CPP] more reg get/setting and tracing support for gpu/dsp

[ef18a76064 CPP] improve jaguar system bus, add more jaguar memory domains

[d8825deb8d CPP] fix non-word alignment hack

[8194e5ff4b CPP] add hacks to support byteswapped and/or non-word aligned jagcds

[801a783c69 CPP] fix gpu/dsp ram domains

[6113f3c17b CPP] partial jagcd support (doesn't seem to completely work here) fix some issues with vjaguar cleanup add mem/trace callbacks and get/set reg support

[d50454b37a CPP] cleanup vjaguar code

[71e3dfed74 CPP] fix #3388

[38d3d36199 CPP] fix opcode address in exec callbacks + tracing (thanks prefetch) sp/lr/pc for r13/r14/r15 for tracing fix a bad for threaded renderer's thread start callback

[e242d35a22 CPP] pull latest sameboy, rework build system into a makefile

[5e34dc6166 CPP] Always savestate expansion pak regardless of settings.
All the disable expansion pak setting actually does is tell the game the expansion pak is not available.
However, not all games actually abide by this, some will use the expansion pak area anyways.
Video plugins also end up just using a "segfault test" to determine if the expansion pak is present or not
So video plugins may use the expansion pak area too
This ends up causing savestates sometimes just crashing the game if the expansion pak ends up used
Resolves #3092, other state issues might be solved with this (I suspect #3328 is caused by this)

[de38781081 CPP] Implement Rumble for Nyma

[de1e7eef69 Yoshi] Document socket response format

[1bf2bb758c Yoshi] Change serialisation of Jaguar VSystemID
also fixed line ending

[483258a04d CPP] virtualjaguar port, resolves #1907

[34c504d7b9 CPP] update ds disassembler

[f024986ffc brunovalads] Added 'Edit marker frame' feature (squashed PR #3351)
* Added 'Edit marker frame' feature
* Changed Edit Marker Frame icon to clock
* Hotkey tooltip + Prevent changing to a frame that already has marker
* Forgot to delete this icon, was replaced by Clock.png
* De-rookie here and there
* Clean up diff

[09e8c7a9b6 CPP] ensure ds firmware settings represent sync settings if real firmware isn't used
resolves possible cause for #3377

[70906c9004 CPP] quick fail is the user is somehow running EmuHawk as a 32 bit process

[e198122691 CPP] output a more helpful error message on windows for GetErrorMessage

[06226e78cf CPP] add way to obtain error message in ILinkedLibManager, use it to display an error code for init lib checks

[463780a875 CPP] more cleanly deal with dummy hashes
fixes a515672

[a515672d4d CPP] fix #3159

[13069d08f4 CPP] fix gpgx pattern cache invalidation, resolves #3363

[faf4a8b24f Yoshi] Remove unused "TAStudio states" path
TAStudio prop unused since 5bf21e391, path was still in use until b1296dd9b

[f1ef8d0887 CPP] fix oopsie in angrylion, resolves #3372

[c761d6d807 CPP] fix changing mupen expansion pack setting

[a344ee2288 Yoshi] Fix modifier key check in `TAStudio.TasView_MouseDown`

[61c34eca74 Yoshi] Minor refactor to not mutate local in `TAStudio.TasView_MouseDown`

[352977c7ea CPP] speedup HashRegion/ReadByteRange/WriteByteRange for waterbox cores (doesn't do anything for non-wbx cores)

[afdfa065bd CPP] missed apostrophe somehow

[9174d17bd8 CPP] tic80 settings for enabling/disabling controllers, proper mnemonics

[98a8cdf693 CPP] remove gongshell, add "simple" code for opening win32 context menu (gongshell's only actual use), re: #2261

[d58a4a07f5 Yoshi] Update `PcxFileTypePlugin.HawkQuantizer` project file to match others

[3a3494aedb Yoshi] Add missing attribute to `events.can_use_callback_params` param

[8d484ac196 Yoshi] Hardcode edge cases in `MovieConversionExtensions` to pass test
the argument in every real call is from `IMovie.Filename`, which is never
assigned null, and I don't think it's assigned anything but an absolute path

[2ecb572892 CPP] fix nyma light guns, resolves #3359

[7cde8bb466 Yoshi] Add and use 2 extension methods for splitting path into dir+filename

[dce961357a Yoshi] Refactor `IGameInfo.FilesystemSafeName` extension
it doesn't make any sense to split this string into dir+filename, it shouldn't
contain a slash

[d5bf542a3c Yoshi] Cache `ToolStripRenderer` used by `FormBase.FixBackColorOnControls`

[3958348e94 peteyus] Add auto save state on close (squashed PR #3218)
resolves #1861
* Add configuration for auto-saving state on exit
* Update MainForm to auto save on close game if configured
* Fix config serialization test.
* Revert unnecessary changes to Designer file
* Move autosave configuration into Save States menu off of File
* Undo previous test changes
* Remove explicit size on menu item.
* Fix logic

[f1fc05fe60 CPP] quick fix some graphical bugs
this isn't right but should suffice in practice most of the time

[31c7f59e86 CPP] fix some edge cases with new zip compression

[0ff4aca182 CPP] (Gambatte) Remote control controls and remote control emulation expanded to HuC1 IR and CGB IR (previously only done in HuC3)

[aba3359dde Yoshi] Add CPP's testroms to GB testroms project

[5be8b0aab9 CPP] Zstd Compression (#3345)
Deflate compression in rewinder is now zstd compression
Binary blobs in zip files are zstd compressed (text is uncompresed for user ease).
All wbx cores and resources are re-compressed with zstd, wbx build scripts are changed to account for this. Shaves off a bit with download size and it's faster to decompress to.

[32e8afcedc CPP] Implement hardware accelerated CRC32 and SHA1, using them if possible (#3348)
* Implement hardware accelerated CRC32 and SHA1, use them if possible.
CRC32's generic function is also replaced with zlib's as it is much more performant than our implementation
Full hash of a ~731MB disc took only ~369 ms with this, and the generic CRC32 isn't so far behind at ~659 ms
SHA1 should perform 4x faster if the user's CPU supports the SHA instructions.
Co-authored-by: Yoshi
Co-authored-by: Morilli

[8f153fd503 Yoshi] Restore PS2 sysID and add some others from RomLoader

[1452f831af Yoshi] Fix Lua Wiki export, add more notes to fill in some of the holes
frameadvance loop is documented now so for the first time you can write a script
without reading someone else's :O imagine that

[5c48cb96fd Yoshi] When starting new `.tasproj` from SaveRAM, don't clone array twice

[787b413913 Yoshi] Change Basic Bot's addresses to `ulong?`
fixes empty address fields being saved as `0x0`, see #3326

[bd58bde07c Yoshi] Hopefully block edge cases where global `GameInfo` is uninitialised
`Game == null` conditions in `MainForm` ctor looked unreachable, so I changed
them to `Game.IsNullInstance()` which is what I assume was intended, and added
an assert to `RomLoader` in case a bug is introduced later

[d84da4ec4b CPP] wire up sameboy's rumble

[070e7035b3 Yoshi] Ensure there can be no edge cases involving SGXCD sysID
breaks config, in case you care about setting a custom save dir for PCE

[cb468ba806 CPP] pull in latest sameboy master, add stub camera pixel callback to prevent nondeterminism, wire disabling joypad bounce as a sync setting, various cleanup

[e2a36c7242 zeromus] DisplayConfig defaults button should whack the padding back to 0

[3b181ba6e4 zeromus] DisplayManager - fix crashes when setting absurdly large padding values (fixes #3321)

[6eafdf7156 tom_mai78101] Fixed issue where the Copy button in Basic Bot is not toggled on/off properly.
If the Copy button is enabled, but there is no best attempt recorded, it will crash BasicBot / EmuHawk if it attempts to copy a null Log of the best attempt.

[d8fc32f1a8 CPP] (mGBA) Add in missing save types

[4956bae3a2 Yoshi] Improve error message for `IToolFormAutoConfig` ext. tool missing menu

[9bb96fbadc Yoshi] Update doc comments to reflect thrown exception change
fixes 4f98733c2

[6b325ff56c Yoshi] Refactor `BasicBot.IsBetter`

[008a5953f6 tom_mai78101] Fixed code logic error in BasicBot
It was comparing with itself when it's comparing the Tie Breaker 3 value.

[c02975c757 CPP] merge aresv129, enable SIMD RSP

[8289c1051b CPP] add hash for newly discovered GBC-GBA bootrom

[efbef0bbda tom_mai78101] Fixed Basic Bot anchor points.
Also fixed a misaligned label.

[e2fb6017b7 tom_mai78101] Added missing NOT operators from the dropdown menu.

[7e54322901 Yoshi] Add contributor's guide (squashed PR #3292)
Includes primitive contributor license agreement; when creating a PR, contributors will need to check a box confirming they're not infringing on any copyrights.

[94e85f1079 Yoshi] Set `Form.Owner` to MainForm for Lua-made forms

[ece2d8d68c tom_mai78101] Added NOT operator to Basic Bot.

[f8c847af40 CPP] add missing TMD for Zombie Skape, improve error message when TMD cannot be found

[a6823e3afa Yoshi] Add core port request Issue template

[730905b6c3 Yoshi] Adjust wording in Issue templates

[1948721991 tom_mai78101] Increased Basic Bot max frames from 999 to 9999.

[2308ba1ecc tom_mai78101] Added "Clear Output" button to Lua Console (squashed PR #3307)
* Added "Clear Output" button to Lua Console tool.
* Swapped out indentation from tabs to space from Line 248 through 249 for consistency.
* Swapped out indentation from tabs to spaces for consistency.
* Added a custom "Clear Console" icon to Bizhawk.

[0d3c7b7e0c CPP] (Libretro) Implement SET_SYSTEM_AV_INFO and SET_GEOMETRY

[8642513572 CPP] sameboy color correction option, make default for gambatte

[e41d1a996e CPP] fix gpgx_swap_disc, re-enable disk buttons (seems to work?)

[011f4bfe03 Morilli] Further imrpove RamSearch performance
- switch _watchList from a List to an array
- more Domain.EnterExit usage

[9e90290b87 CPP] make MemoryDomain implement IMonitor (default is no-op Enter/Exit), cleanup, remove wrapper use (has a lot of churn itself), probably better performance with bulk functions

[fe22d61b3a Morilli] Save Monitor for all Monitor MemoryDomains
appends 596bd03ebe2a7721c42a8293dbf98e37e2413638 for speedup for those domains

[596bd03ebe CPP] expose a possible IMonitor for memory domains, use it to speed up RAM Search for waterbox cores (25-30% speedup?)
see #3296

[806830c314 feos] nymashock: resolve !s and hide some columns in tastudio

[318c1a7fea feos] tastudio: don't autorestore if current frame remained valid
1b8b4b492623f25b0aa322901c2009de9071ebb4 removed an important bit of logic that set `_triggerAutoRestore` to false in certain cases. but simply putting the same line back there doesn't fix the problem, probably due to major refactorings over the years. so I'm adding it right into `GoToLastEmulatedFrameIfNecessary()` which is still called properly when it's needed. `JumpToGreenzone()` is kinda redundant now since it contains the same check, but it's used from the outside, and I didn't feel like refactoring this part.

[369bdbe9a6 tom_mai78101] Fixed the "To PC" button not updating the disassembler view (squashed PR #3299)
* Fixed the "To PC" button from not updating the disassembler view in the Debugger window.
* Fixed the calls being reversed, per feedback.

[b8f8b41f2c CPP] Give NHL 96 (Genesis) SRAM (fixes #3300)

[206dcaf49b Yoshi] Refactors for selection in `InputRoll`
also standardises behaviour of Select All and Insert Separator buttons
see e88fa8135

[5875df4b76 CPP] prevent svp dereferences when not using an svp cart (fixes #3297)

[e88fa81358 tom_mai78101] Added Select All/None to RAM Search (squashed PR #3295)
* Added the ability to select all addresses / deselect all addresses in the RAM Search window.
* Fixed logic error. This now makes more intuitive sense, in that, if a portion of the rows were selected, and you do Select All, it should select the unselected rows along with the selected rows.
* Simplify condition
Co-authored-by: Yoshi

[a86591c595 tom_mai78101] Fixed RAM Watch not having CTRL+A working properly.

[d9c828ef57 CPP] deterministic emulation means not real time

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
