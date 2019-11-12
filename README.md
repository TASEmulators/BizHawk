# BizHawk

A multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).

[![unique systems emulated | 27](https://img.shields.io/badge/unique_systems_emulated-27-darkgreen.svg?logo=data:image/png;base64,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&style=popout)](#cores)
[![GitHub latest release](https://img.shields.io/github/release/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)
[![dev builds | AppVeyor](https://img.shields.io/badge/dev_builds-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history)
[![Windows prereqs | GitHub](https://img.shields.io/badge/Windows_prereqs-GitHub-darkred.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest)
[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/issues)

***

Click the "release" button above to grab the latest stable version ([changelog at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html)). 

New user on Windows? Install the prerequisites first, click the "prereqs" button to get that and see [*Installing*](#windows-78110) for info.

**Never mix different versions** of BizHawk — Keep each version in its own folder.

Jump to:
* Installing
	* [Windows 7/8.1/10](#windows-78110)
	* [Unix](#unix)
* Building
	* [Windows 7/8.1/10](#windows-78110-1)
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

### Windows 7/8.1/10

Released binaries can be found right here on GitHub:

[![Windows | binaries](https://img.shields.io/badge/Windows-binaries-%230078D6.svg?logo=windows&logoColor=0078D6&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

Click `BizHawk-<version>.zip` to download it. Also note the changelog, the full version of which is [here at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html). **Don't mix different versions** of BizHawk, keep each version in its own folder.

**Note**: Before you start (by running `EmuHawk.exe`), you'll need the Windows-only prerequisites installed. You can get them all at once with [this program](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest) (you don't need to do this every time BizHawk updates, check the date on its release page, but it can't hurt installing it again to be sure). The specific libraries it installs are:
* .NET Framework 4.6.1
* Visual C++ Redists
	* 2010 SP1
	* 2012
	* 2015
* Direct3D 9

BizHawk functions like a "portable" program, you may move or rename the folder containing `EmuHawk.exe`, even to another drive — as long as you keep all the files together, and the prerequisites are installed when you go to run it.

Following [Microsoft's support lifecycle](https://support.microsoft.com/en-us/help/13853/windows-lifecycle-fact-sheet), the supported versions of Windows are: Win10 from 1809 "Redstone 5", Win8.1, and until January, Win7 SP1.

A "backport" release, [1.13.2](https://github.com/TASVideos/BizHawk/releases/tag/1.13.2), is available for users of Windows XP and/or 32-bit Windows. Being in the 1.x series, many bugs remain and features are missing.

[to top](#bizhawk)

### Unix

**IMPORTANT**: Unix support is a work-in-progress! It is *not* complete, does *not* look very nice, and is *not* ready for anything that needs accuracy.

You'll need to either build BizHawk yourself (see [*Building*](#unix-1) below), or download a dev build (see [*Testing*](#testing) below; please note some features are broken in dev builds for unknown reasons).

The runtime dependencies are: Mono "complete", Mono VB.NET, WINE (just `libwine` if available), glibc, OpenAL, NVIDIA's `cgc` utility, and your distro's LSB implementation. Run `EmuHawkMono.sh` to start Mono with the right library and executable paths—you can run it from anywhere, so putting it in a .desktop file is fine.

The systems that currently work are: GB + GBC (GBHawk), NES (NesHawk), SMS, Atari 7800, and some classic home computers. Nothing other than EmuHawk has been ported. See [#1430](https://github.com/TASVideos/BizHawk/issues/1430) for progress.

[to top](#bizhawk)

## Building

### Windows 7/8.1/10

If you have WSL, Git BASH, or similar, clone the repo with:
```
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
```
...or use a [Git GUI](https://desktop.github.com). Without git, you'll have to download [an archive of master](https://github.com/TASVideos/BizHawk/archive/master.zip).

Once it's downloaded and extracted, go into the repo's `Dist` folder and run `BuildAndPackage_Release.bat`. BizHawk will be built as a .zip just like any other release.

For anything more complicated than just building, you'll need an IDE like [VS Community 2019](https://visualstudio.microsoft.com/vs/community), currently the best free C# IDE (you may prefer Rider, MonoDevelop, or something else). To start with VS, open `BizHawk.sln` and use the toolbar to choose `Release | Any CPU | BizHawk.Client.EmuHawk` and click the Start button. See [Compiling at TASVideos](http://tasvideos.org/Bizhawk/Compiling.html) for more detailed instructions (warning: somewhat outdated).

[to top](#bizhawk)

### Unix

Before you can build, you need `msbuild` (which should include `nuget`) You may need to [add a repo](https://www.mono-project.com/download/stable/), if so, if may conflict with the distro's Mono package. Once it's installed, run:
```sh
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
Dist/BuildRelease.sh
```

The assemblies are put in `output`, so if you have the runtime dependencies (see [*Installing*](#unix)) you can call `output/EmuHawkMono.sh`. You may need to add the WINE library path to the script—find `d3dx9_43.dll.so` and update the case statement accordingly (and then please post it to [#1430](https://github.com/TASVideos/BizHawk/issues/1430) or in IRC).

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

Amstrad CPC, Fairchild Channel F, Magnavox Odyssey², Sony PSP, and MB Vectrex emulation, and a MAME frontend, are works-in-progress and there is **no ETA**. Cores for other systems are only conceptual. If you want to help speed up development, ask on IRC (see below).

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
