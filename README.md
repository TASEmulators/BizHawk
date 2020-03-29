# BizHawk

A multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).

[![GitHub latest release](https://img.shields.io/github/release/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)
[![dev builds | AppVeyor](https://img.shields.io/badge/dev_builds-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history)
[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/issues)

---

Jump to:
* Installing
	* [Windows](#windows)
	* [Unix](#unix)
* Building
	* [Windows](#windows-1)
	* [Unix](#unix-1)
* [Usage](#usage)
	* [TASing](#tasing)
	* [Testing](#testing)
	* [Cores](#cores)
* [Support and troubleshooting](#support-and-troubleshooting)
* [Contributing](#contributing)
* [Related projects](#related-projects)
* [License](#license)

## Features and systems

The BizHawk common features (across all cores) are:
* format, region, and integrity detection for game images
* 10 save slots with hotkeys and infinite named savestates
* speed control, including frame stepping and rewinding
* memory view/search/edit in all emulated hardware components
* input recording (making TAS movies)
* screenshotting and recording audio + video to file
* firmware management
* input, framerate, and more in a HUD over the game
* emulated controllers via a comprehensive input mapper
* Lua control over core and frontend (Windows only)
* hotkey bindings to control the UI

Supported consoles and computers:

* Apple II
* Atari
	* Video Computer System / 2600
	* 7800
	* Lynx
* Bandai WonderSwan + Color
* CBM Commodore 64
* Coleco Industries ColecoVision
* Mattel Intellivision
* NEC
	* PC Engine / TurboGrafx-16 + SuperGrafx + CD
	* PC-FX
* Neo Geo Pocket + Color
* Nintendo
	* Famicom / Nintendo Entertainment System + FDS
	* Game Boy + Color
	* Game Boy Advance
	* Nintendo 64
	* Super Famicom / Super Nintendo Entertainment System
	* Virtual Boy
* Sega
	* Game Gear
	* Genesis + 32X + CD
	* Master System
	* Pico
	* Saturn
	* SG-1000
* Sinclair ZX Spectrum
* Sony Playstation (PSX)
* Texas Instruments TI-83
* Uzebox
* [More](http://tasvideos.org/Bizhawk/CoreRoadMap.html) coming soon..?

See [*Usage*](#usage) below for info on basic config needed to play games.

[to top](#bizhawk)

## Installing

### Windows

Released binaries can be found right here on GitHub (also linked at the top of this readme):

[![Windows | binaries](https://img.shields.io/badge/Windows-binaries-%230078D6.svg?logo=windows&logoColor=0078D6&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

Click `BizHawk-<version>.zip` to download it. Also note the changelog, the full version of which is [here at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html). Extract it anywhere, but **don't mix different versions** of BizHawk, keep each version in its own folder. Run `EmuHawk.exe` to start. You may move or rename the folder containing `EmuHawk.exe`, even to another drive — as long as you keep all the files together, and the prerequisites are installed when you go to run it.

EmuHawk does have some prerequisites which it can't work without (it will let you know if they're missing). The list is [here](https://github.com/TASVideos/BizHawk-Prereqs/blob/e364066a0f79ad560d6725e6088d680293e09e30/README), and we've made an all-in-one installer which you can get [here](https://github.com/TASVideos/BizHawk-Prereqs/releases/tag/2.1). You should only have to run this once per machine, unless the changelog says we need something extra.

We will be [following Microsoft](https://support.microsoft.com/en-us/help/13853/windows-lifecycle-fact-sheet) in dropping Windows support, that is, we reserve the right to ignore your problems unless you've updated to at least Win10 1809 "Redstone 5" or Win8.1 KB4530702 (latest at time of writing).

A "backport" release, [1.13.2](https://github.com/TASVideos/BizHawk/releases/tag/1.13.2), is available for users of Windows XP, 7, or 8.1 32-bit. It has many bugs that will never be fixed and it doesn't have all the features of the later versions.

[to top](#bizhawk)

### Unix

**IMPORTANT**: Unix support is a work-in-progress! It is *not* complete, does *not* look very nice, and is *not* ready for anything that needs accuracy.

You'll need to either build BizHawk yourself (see [*Building*](#unix-1) below), or download a dev build (see [*Testing*](#testing) below; please note some features are broken in dev builds for unknown reasons).

The runtime dependencies are: Mono "complete", Mono VB.NET, ~~`libwine`~~, glibc, OpenAL, NVIDIA's `cgc` utility, and your distro's LSB implementation (You do *not* need .NET at runtime). Run `EmuHawkMono.sh` to start Mono with the right library and executable paths—you can run it from anywhere, so putting it in a .desktop file is fine.

The systems that currently work are: GB + GBC (GBHawk), NES (NesHawk), SMS, Atari 7800, and some classic home computers. Nothing other than EmuHawk has been ported. See [#1430](https://github.com/TASVideos/BizHawk/issues/1430) for progress.

[to top](#bizhawk)

## Building

### Windows

If you don't have Git, download [an archive of `master`](https://github.com/TASVideos/BizHawk/archive/master.zip). If you have WSL, Git BASH, or similar, clone the repo with:
```
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
```

Once it's downloaded and extracted, go into the repo's `Dist` folder and run `BuildAndPackage_Release.bat`. This is the same process used by AppVeyor.

For anything more complicated than just building, you'll need an IDE like [VS Community 2019](https://visualstudio.microsoft.com/vs/community), currently the best free C# IDE (you may prefer Rider, MonoDevelop, or something else). To build with VS, open `BizHawk.sln` and use the toolbar to choose `Release | Any CPU | BizHawk.Client.EmuHawk` and click the Start button. See [*Compiling* at TASVideos](http://tasvideos.org/Bizhawk/Compiling.html) for more detailed instructions (warning: somewhat outdated).

[to top](#bizhawk)

### Unix

Before you can build, you'll need the .NET Core SDK 3.1 (package name is usually `dotnet-sdk-3.1`, see [full instructions](https://docs.microsoft.com/en-gb/dotnet/core/install/sdk?pivots=os-linux)). You may need to uninstall MSBuild first. Once it's installed, run:
```sh
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
BizHawk_master/Dist/BuildRelease.sh
```

The assemblies are put in `BizHawk_master/output`, so if you have the runtime dependencies (see [*Installing*](#unix)) you can call `BizHawk_master/output/EmuHawkMono.sh`. The shell script may yell at you, it should be safe to ignore. stdout is redirected to `BizHawk_master/output/EmuHawkMono_laststdout.txt` by default, pass `--mono-no-redirect` **as the first flag** to disable this.

[to top](#bizhawk)

## Usage

#### Loading firmware

Put all your dumped firmware files in the `Firmware` folder and everything will be automatically detected and loaded when you try to load a game (filenames and subfolders aren't enforced, you can just throw them in there). If you're missing required or optional firmware, you will see a "You are missing the needed firmware files [...]" dialog.

Keep in mind some firmware is optional, and some have multiple versions, only one of which needs to be set.

If you want to customise firmware (when there are alternative firmwares, for example) go to `Config` > `Firmwares...`, right-click the line of the firmware you want to change, click "Set Customization", and open the file.

You can change where BizHawk looks for firmware by going to `Config` > `Paths...` and changing "Firmware" in the "Global" tab to the new location. This allows multiple BizHawk releases to use the same folder.

#### Identifying a good rom

With a core and game loaded, look in the very left of the status bar (`View` > `Display Status Bar`):
* a green checkmark means you've loaded a "known good" rom;
* a "!" in a red circle means you've loaded a "known bad" rom, created by incorrect dumping methods; and
* something else, usually a ?-block, means you've loaded something that's not in the database.

#### Rebinding keys and controllers

There are two keybind windows, `Config` > `Controllers...` and `Config` > `Hotkeys...`. These let you bind your keyboard and controllers to virtual controllers and to frontend functions, respectively.

Using them is simple, click in a box next to an action and press the button (or bump the axis) you want bound to that action. If the "Auto Tab" checkbox at the bottom of the window is checked, the next box will be selected automatically and whatever button you press will be bound to *that* action, and so on down the list. If "Auto Tab" is unchecked, clicking a filled box will let you bind another button to the same action. Keep in mind there are multiple tabs of actions.

#### Changing cores

To change which core is used for NES, SNES, GB, or GBA, go to `Config` > `Cores`. There, you'll also find the `GB in SGB` item, which is a checkbox that makes GB games run with the *Super Game Boy* on an SNES.

Most cores have their own settings window too, look in the menubar for the system name after `Tools`. Some have multiple windows, like Mupen64Plus which has virtual controller settings and graphics settings.

#### Running Lua scripts

(Reminder that this feature is Windows-only for now.)

Go to `Tools` > `Lua Console`. The opened window has two parts, the loaded script list and the console output. The buttons below the menubar are shortcuts for items in the menus, hover over them to see what they do. Any script you load is added to the list, and will start running immediately. Instead of using "Open script", you can drag-and-drop .lua files onto the console or game windows.

Running scripts have a "▶️" beside their name, and stopped scripts (manually or due to an error) have a "⏹️" beside them. Using "Pause or Resume", you can temporarily pause scripts, those have a "⏸️".

"Toggle script" does just that (paused scripts are stopped). "Reload script" stops it and loads changes to the file, running scripts are then started again. "Remove script" stops it and removes it from the list.

#### In-game Saves

Games may internally [save your progress](https://en.wikipedia.org/wiki/Saved_game) into memory (SRAM, memory cards) or file. When this happens, BizHawk stores this in-game save in the Operating System memory and makes the `File` > `Save RAM` menu bold.

BizHawk can write in-game saves to disk - this is called flushing. Every time you save in the game (not to be confusing with *emulator savestates*), you should backup your saves! Go to `File` > `Save RAM` and hit `Flush Save Ram`. Note that some systems use SRAM for irrelevant tasks and store temporary data there, and the menu may become bold without in-game saves involved. Be aware when the game is *supposed to save* and flush accordingly.

BizHawk can be configured to flush saves to disk automatically in `Config` > `Customize` > `Advanced AutoSaveRAM`. Opon closing the ROM (which includes any core reboot) BizHawk may try to flush save RAM automatically as well.

```

DISCLAIMER

Automatic flushing is extremely unreliable and not being maintained.
It may corrupt your previous saves!
It will be completely removed in future.
Develop a habit to always flush saves manually every time you save in the game.
Make backups of the flushed save files!
If you don't flush saves manually and backup them, and something breaks, you're on your own.
If your save has been corrupted, there's nothing we can do about it.

```

[to top](#bizhawk)

### TASing

~~This section refers to BizHawk specifically. For resources on TASing in general, see [Welcome to TASVideos](http://tasvideos.org/WelcomeToTASVideos.html).~~ This section hasn't been written yet.

For now, the best way to learn how to TAS is to browse pages like [BasicTools](http://tasvideos.org/TasingGuide/BasicTools.html) on TASVideos and watch tutorials like [Sand_Knight and dwangoAC's](https://youtu.be/6tJniMaR2Ps).

[to top](#bizhawk)

### Testing

* [Latest development build](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/build/artifacts)

Testing bugfixes or new features can be just as helpful as making them! If code's more your thing, see [*Contributing*](#contributing) below.

Dev builds are automated with AppVeyor, every green checkmark in the [commit history](https://github.com/TASVideos/BizHawk/commits/master) is a successful build. Clicking a checkmark and then "Details" in the box that appears takes you straight to the build page. The full list is [here](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history), in future use the "dev builds" button at the top of this readme.

Once you're on the build page, click "Artifacts" and download `BizHawk_Developer-<datetime>-#<long hexadecimal>.zip`.

[to top](#bizhawk)

### Cores

A *core* is what we call the smaller bits of software that emulate just one system or family of systems, e.g. NES/Famicom. For the most part, there's a "best" core for each system, based on accuracy, but there are a few alternative cores which are *faster and less accurate*.

System | Core | Alt. Core
--:|:--|:--
Apple II | Virtu |
Atari 2600 | Atari2600Hawk |
Atari 7800 | A7800Hawk |
Atari Lynx | Handy |
Commodore 64 | C64Hawk |
ColecoVision | ColecoHawk |
Game Boy / Color | GBHawk | Gambatte
Game Boy Advance | mGBA | VBA-Next
Intellivision | IntelliHawk |
N64 | Mupen64Plus |
Neo Geo Pocket / Color | NeoPop |
NES | NesHawk | QuickNes |
PC-FX | T.S.T. |
Playstation (PSX) | Octoshock |
Sega Game Gear | SMSHawk |
Sega Genesis | Genplus-gx |
Sega Master System | SMSHawk |
Sega Saturn | Saturnus |
Sega Pico | PicoDrive |
SNES | BSNES | Snes9x
Super Game Boy | BSNES | SameBoy
TI-83 | TI83Hawk |
TurboGrafx / SuperGrafx | PCEHawk |
Uzebox | Uzem |
Virtual Boy | Virtual Boyee |
WonderSwan / Color | Cygne |
ZX Spectrum | ZXHawk |

Amstrad CPC, Fairchild Channel F, Magnavox Odyssey², Nintendo DS via [melonDS](https://github.com/Arisotura/melonDS), and MB Vectrex emulation are works-in-progress, as well as a front-end for MAME, and there is **no ETA** for any of them so don't ask. Cores for other systems are only conceptual. If you're willing and able to work on one of these, ask on IRC (see below).

[to top](#bizhawk)

## Support and troubleshooting

A short [FAQ](http://tasvideos.org/Bizhawk/FAQ.html) is provided on the [BizHawk wiki](http://tasvideos.org/Bizhawk.html).

If your problem is one of the many not answered there, and you can't find it in the [issue tracker search](https://github.com/TASVideos/BizHawk/issues?q=is%3Aissue+ISSUE_KEYWORDS), check the [BizHawk forum](http://tasvideos.org/forum/viewforum.php?f=64) at TASVideos, or ask on IRC:
* with an IRC client, join channel `#bizhawk` on `chat.freenode.net:6697`
* with a Matrix client, connect to [#freenode_#bizhawk:matrix.org](https://matrix.to/#/#freenode_#bizhawk:matrix.org) (via matrix.org's IRC bridge)
* use freenode's [web-based IRC client](http://webchat.freenode.net/?channels=bizhawk)

If there's no easy solution, what you've got is a bug. Or maybe a feature request. Either way, [open a new issue](https://github.com/TASVideos/BizHawk/issues/new) (you'll need a GitHub account, signup is very fast).

[to top](#bizhawk)

## Contributing

BizHawk is Open Source Software, so you're free to modify it however you please, and if you do, we invite you to share! Under the permissive *MIT License*, this is optional, just be careful with reusing cores as some have copyleft licenses.

Not a programmer? Something as simple as reproducing bugs with different software versions is still very helpful! See [*Testing*](#testing) above to learn about dev builds if you'd rather help us get the next release out.

If you'd like to fix bugs, check the [issue tracker](https://github.com/TASVideos/BizHawk/issues) here on GitHub.

It's a good idea to check if anyone is already working on an issue by asking on IRC (see [*Support*](#support-and-troubleshooting) above).

If you'd like to add a feature, first search the issue tracker for it. If it's a new idea, make your own feature request issue before you start coding.

For the time being, style is not enforced in PRs, only build success is. Please use CRLF, tabs, and [Allman braces](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) in new files.

Past contrbutors to the frontend and custom-built cores are listed [here](https://github.com/TASVideos/BizHawk/graphs/contributors). See the wiki for core authors.

[to top](#bizhawk)

## Related projects

* [DeSmuME](https://desmume.org) for DS/Lite — cross-platform
* [Dolphin](https://dolphin-emu.org) for GameCube and (original) Wii — cross-platform
* [FCEUX](http://www.fceux.com/web/home.html) for NES/Famicom — TASing is Windows-only, but it should run cross-platform
* [libTAS](https://github.com/clementgallet/libTAS) for Linux ELF — GNU+Linux-only, also emulates other emulators
* [lsnes](http://tasvideos.org/Lsnes.html) for GB and SNES — cross-platform
* [openMSX](https://openmsx.org) for MSX — cross-platform

Emulators for other systems can be found on the [EmulatorResources page](http://tasvideos.org/EmulatorResources.html) at TASVideos. The [TASVideos GitHub page](https://github.com/TASVideos) also holds copies of other emulators and plugins where development happens sometimes, their upstreams may be of use.

## License

From the [full text](https://github.com/TASVideos/BizHawk/blob/master/LICENSE):
> This repository contains original work chiefly in c# by the BizHawk team (which is all provided under the MIT License), embedded submodules from other authors with their own licenses clearly provided, other embedded submodules from other authors WITHOUT their own licenses clearly provided, customizations by the BizHawk team to many of those submodules (which is provided under the MIT license), and compiled binary executable modules from other authors without their licenses OR their origins clearly indicated.

In short, the frontend is MIT (Expat), beyond that you're on your own.
