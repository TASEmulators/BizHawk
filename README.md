# BizHawk

A multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).

[![unique systems emulated | 27](https://img.shields.io/badge/unique_systems_emulated-27-darkgreen.svg?logo=buffer&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/blob/master/README.md#cores)
[![GitHub latest release](https://img.shields.io/github/release/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)
[![dev builds | AppVeyor](https://img.shields.io/badge/dev_builds-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history)
[![Windows prereqs | GitHub](https://img.shields.io/badge/Windows_prereqs-GitHub-darkred.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest)

***

Click the "release" button above to grab the latest stable version ([changelog at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html)). 

New user on Windows? Install the prerequisites first, click the "prereqs" button to get that and see [*Installing*](https://github.com/TASVideos/BizHawk/blob/master/README.md#windows-78110) for info.

**Never mix different versions** of BizHawk — Keep each version in its own folder.

Jump to:
* Installing
	* [Windows 7/8.1/10](https://github.com/TASVideos/BizHawk/blob/master/README.md#windows-78110)
	* [GNU+Linux and macOS](https://github.com/TASVideos/BizHawk/blob/master/README.md#gnulinux-and-macos)
* Building
	* [Windows 7/8.1/10](https://github.com/TASVideos/BizHawk/blob/master/README.md#windows-78110-1)
	* [GNU+Linux and macOS](https://github.com/TASVideos/BizHawk/blob/master/README.md#gnulinux-and-macos-1)
* [Usage](https://github.com/TASVideos/BizHawk/blob/master/README.md#usage)
	* [TASing](https://github.com/TASVideos/BizHawk/blob/master/README.md#tasing)
	* [Testing](https://github.com/TASVideos/BizHawk/blob/master/README.md#testing)
	* [Cores](https://github.com/TASVideos/BizHawk/blob/master/README.md#cores)
* [Support and troubleshooting](https://github.com/TASVideos/BizHawk/blob/master/README.md#support-and-troubleshooting)
* [Contributing](https://github.com/TASVideos/BizHawk/blob/master/README.md#contributing)
* [Related projects](https://github.com/TASVideos/BizHawk/blob/master/README.md#related-projects)
* [License](https://github.com/TASVideos/BizHawk/blob/master/README.md#license)

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
* Mattel IntelliVision
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
* Sony Playstation / PSX
* Texas Instruments TI-83
* Uzebox
* [More](http://tasvideos.org/Bizhawk/CoreRoadMap.html) coming soon..?

See [*Usage*](https://github.com/TASVideos/BizHawk/blob/master/README.md#usage) below for details about specific tools and config menus.

## Installing

### Windows 7/8.1/10

Released binaries can be found right here on GitHub:

[![Windows | binaries](https://img.shields.io/badge/Windows-binaries-%230078D6.svg?logo=windows&logoColor=0078D6&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

Click `BizHawk-<version>.zip` to download it. Also note the changelog, the full version of which is [here at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html). **Don't mix different versions** of BizHawk, keep each version in its own folder.

Before you start (by running `EmuHawk.exe`), you'll need the following Windows-only prerequisites installed. You can get them all at once with [this program](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest).
* .NET Framework 4.6.1
* Visual C++ Redists
	* 2010 SP1
	* 2012
	* 2015
* Direct3D 9

BizHawk functions like a "portable" program, you may move or rename the folder containing `EmuHawk.exe`, even to another drive — as long as you keep all the files together, and the prerequisites are installed when you go to run it.

Win7 is supported from SP1, Win8 is supported from 8.1, and Win10 is supported from 1709 "Redstone 3", following [Microsoft's support lifecycle](https://support.microsoft.com/en-us/help/13853/windows-lifecycle-fact-sheet).

A "backport" release, [1.13.2](https://github.com/TASVideos/BizHawk/releases/tag/1.13.2), is available for Windows XP and 32-bit users. Being in the 1.x series, many bugs remain and features are missing.

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

### GNU+Linux and macOS

**IMPORTANT**: The Mono "port" is a work-in-progress! It is *not* complete, does *not* look very nice and is *not* ready for anything that needs accuracy.

You'll need to either build BizHawk yourself (see [*Building*](https://github.com/TASVideos/BizHawk/blob/master/README.md#gnulinux-and-macos-1) below), or download a dev build (see [*Testing*](https://github.com/TASVideos/BizHawk/blob/master/README.md#testing) below).

Run `EmuHawkMono.sh` to give Mono the library and executable paths — you can run it from anywhere, so putting it in a .desktop file is fine. If running the script doesn't start EmuHawk, you may need to edit it (if you use a terminal, it will say so in the output).

If you run `EmuHawkMono.sh` from a terminal, note that `File > Exit (Alt+F4)` doesn't terminate the process correctly, you'll need to send SIGINT (`^C`). The systems that currently work are: Game Boy + GBC (GBHawk), NES (NesHawk), Sega Master System, Atari 7800, Commodore 64, ColecoVision, IntelliVision, TurboGrafx, and ZX Spectrum. See #1430 for progress.

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

## Building

### Windows 7/8.1/10

If you have WSL, Git BASH, or similar, clone the repo with:
```
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
```
...or use a [Git GUI](https://desktop.github.com). Otherwise, you'll have to download an archive from GitHub.

Once it's downloaded and extracted, go into the repo's `Dist` folder and run `BuildAndPackage_Release.bat`. BizHawk will be built as a .zip just like any other release.

For anything more complicated than building, you'll need an IDE like [VS Community 2017](https://visualstudio.microsoft.com/vs/community), currently the best free C# IDE. Open `BizHawk.sln` with VS to start and use the toolbar to choose EmuHawk and build. See [Compiling at TASVideos](http://tasvideos.org/Bizhawk/Compiling.html) (somewhat outdated) for more detailed instructions.

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

### GNU+Linux and macOS

*Compiling* requires MSBuild and *running* requires Mono and WINE, but **BizHawk does not run under WINE** — only the bundled libraries are required.

If you use GNU+Linux, there might be a `bizhawk-git` package or similar in the same repo as the main package. If it's available, installing it will automate the build process.

Building is as easy as:
```sh
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
msbuild /p:Configuration=Release BizHawk.sln
```

Remove the `/p:...` flag from MSBuild if you want debugging symbols.

If your distro isn't listed under *Installing* above, `libblip_buf` probably isn't in your package repos. You can easily [build it yourself](https://gitlab.com/TASVideos/libblip_buf/blob/unified/README.md).

Once built, see [*Installing*](https://github.com/TASVideos/BizHawk/blob/master/README.md#gnulinux-and-macos) above, substituting the repo's `output` folder for the download.

Again, if your distro isn't listed there, you might get an "Unknown distro" warning in the terminal, and BizHawk may not open or may show the missing dependencies dialog. You may need to add your distro to the case statement in the script, setting `libpath` to the location of `d3dx9_43.dll.so` (please do share if you get it working).

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

## Usage

#### Loading firmware

You may have seen a dialog saying "You are missing the needed firmware files [...]" when trying to open a rom. Pressing "Yes" opens the Firmware Manager, or you can go to `Config` > `Firmwares...`.

To load firmwares, the easiest way is to click "Import" in the menubar, navigate to the dumped firmware(s), select them all, and click "Open". It's a good idea to have them copied into the `Firmware` folder, which is nicely organised, when prompted. If you were trying to open a rom, click "Close and reload ROM" to do that. Keep in mind some firmware is optional and some have multiple versions, only one of which needs to be set.

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

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

### TASing

~~This section refers to BizHawk specifically. For resources on TASing in general, see [Welcome to TASVideos](http://tasvideos.org/WelcomeToTASVideos.html).~~ This section hasn't been written yet.

For now, the best way to learn how to TAS is to browse pages like [BasicTools](http://tasvideos.org/TasingGuide/BasicTools.html) on TASVideos and watch tutorials like [Sand_Knight and dwangoAC's](https://youtu.be/6tJniMaR2Ps).

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

### Testing

Testing bugfixes or new features can be just as helpful as making them! If code's more your thing, see [*Contributing*](https://github.com/TASVideos/BizHawk/blob/master/README.md#contributing) below.

Dev builds are automated with AppVeyor, every green checkmark in the [commit history](https://github.com/TASVideos/BizHawk/commits/master) is a successful build. Clicking a checkmark and then "Details" in the box that appears takes you straight to the build page. The full list is [here](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history), in future use the "dev builds" button at the top of this readme.

Once you're on the build page, click "Artifacts" and download `BizHawk_Developer-<datetime>-#<long hexadecimal>.zip`.

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

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
IntelliVision | IntelliHawk |
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

Amstrad CPC, Magnavox Odyssey², and PSP emulation are works-in-progress and there is **no ETA**. Cores for other systems are only conceptual. If you want to help speed up development, ask on IRC (see below).

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

## Support and troubleshooting

A short [FAQ](http://tasvideos.org/Bizhawk/FAQ.html) is provided on the [BizHawk wiki](http://tasvideos.org/Bizhawk.html). If your problem is one of the many not answered there, and you can't find it in the [issue tracker search](https://github.com/TASVideos/BizHawk/issues?q=is%3Aissue+ISSUE_KEYWORDS), check the [BizHawk forum](http://tasvideos.org/forum/viewforum.php?f=64) at TASVideos, or ask on IRC:

[![IRC | chat.freenode.net #bizhawk](https://img.shields.io/badge/IRC-chat.freenode.net_%23bizhawk-steelblue.svg?style=flat)](ircs://chat.freenode.net:6697/bizhawk)
[![Matrix (IRC bridge) | #freenode_#bizhawk:matrix.org](https://img.shields.io/badge/Matrix_(IRC_bridge)-%23freenode__%23bizhawk:matrix.org-mediumpurple.svg?logo=matrix&style=flat)](https://matrix.to/#/#freenode_#bizhawk:matrix.org)
[![freenode webchat | #bizhawk](https://img.shields.io/badge/freenode_webchat-%23bizhawk-lightcoral.svg?style=flat)](http://webchat.freenode.net/?channels=bizhawk)

If there's no easy solution, what you've got is a bug. Or maybe a feature request. Either way, [open a new issue](https://github.com/TASVideos/BizHawk/issues/new) (you'll need a GitHub account, signup is very fast).

[to top](https://github.com/TASVideos/BizHawk/blob/master/README.md#bizhawk)

## Contributing

BizHawk is Open Source Software, so you're free to modify it however you please, and if you do, we invite you to share! Under the permissive *MIT License*, this is optional, just be careful with reusing cores as some have copyleft licenses.

Not a programmer? Something as simple as reproducing bugs with different software versions is still very helpful! See [*Testing*](https://github.com/TASVideos/BizHawk/blob/master/README.md#testing) above to learn about dev builds if you'd rather help us get the next release out.

If you'd like to fix bugs, check the issue tracker here on GitHub:

[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/issues)

It's a good idea to check if anyone is already working on an issue by asking on IRC (see [*Support*](https://github.com/TASVideos/BizHawk/blob/master/README.md#support-and-troubleshooting) above).

If you'd like to add a feature, first search the issue tracker for it. If it's a new idea, make your own feature request issue before you start coding.

For the time being, style is not enforced in PRs, only build success is. Please use CRLF, tabs, and [Allman braces](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) in new files.

Past contrbutors to the frontend and custom-built cores are listed [here](https://github.com/TASVideos/BizHawk/graphs/contributors). See a core's docs for its authors.

## Related projects

* [DeSmuME](https://desmume.org) for DS/Lite — cross-platform
* [Dolphin](https://dolphin-emu.org) for GameCube and (original) Wii — cross-platform
* [FCEUX](http://www.fceux.com/web/home.html) for NES/Famicom — TASing is Windows-only, but it should run on Unix
* [libTAS](https://github.com/clementgallet/libTAS) for Linux ELF — GNU+Linux-only, also emulates other emulators
* [lsnes](http://tasvideos.org/Lsnes.html) for GB and SNES — cross-platform
* [openMSX](https://openmsx.org) for MSX — cross-platform

Emulators for other systems can be found on the [EmulatorResources page](http://tasvideos.org/EmulatorResources.html) at TASVideos. The [TASVideos GitHub page](https://github.com/TASVideos) also holds copies of other emulators and plugins where development happens sometimes, their upstreams may be of use.

## License

From the [full text](https://github.com/TASVideos/BizHawk/blob/master/LICENSE):
> This repository contains original work chiefly in c# by the BizHawk team (which is all provided under the MIT License), embedded submodules from other authors with their own licenses clearly provided, other embedded submodules from other authors WITHOUT their own licenses clearly provided, customizations by the BizHawk team to many of those submodules (which is provided under the MIT license), and compiled binary executable modules from other authors without their licenses OR their origins clearly indicated.

In short, the frontend is MIT (Expat), beyond that you're on your own.
