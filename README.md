# BizHawk

[![unique systems emulated | 24](https://img.shields.io/badge/unique_systems_emulated-24-darkgreen.svg?logo=buffer&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/blob/master/README.md#cores)
[![GitHub latest release](https://img.shields.io/github/release/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)
[![dev builds | AppVeyor](https://img.shields.io/badge/dev_builds-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history)
[![Windows prereqs | GitHub](https://img.shields.io/badge/Windows_prereqs-GitHub-darkred.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest)

A multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).

Click the "release" button above to grab the latest stable version ([changelog at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html)). 

New users on Windows should click the "prereqs" button too, see *Installing* for info.

**Never mix different versions** of BizHawk — Keep each version in its own folder.

Jump to:
* [Installing](https://github.com/TASVideos/BizHawk/blob/master/README.md#installing)
	* [Windows 7/8.1/10](https://github.com/TASVideos/BizHawk/blob/master/README.md#windows-78110)
	* [GNU+Linux and macOS](https://github.com/TASVideos/BizHawk/blob/master/README.md#gnulinux-and-macos)
* [Building](https://github.com/TASVideos/BizHawk/blob/master/README.md#building)
* [Usage](https://github.com/TASVideos/BizHawk/blob/master/README.md#usage)
	* [TASing](https://github.com/TASVideos/BizHawk/blob/master/README.md#tasing)
	* [Cores](https://github.com/TASVideos/BizHawk/blob/master/README.md#cores)
* [Support and troubleshooting](https://github.com/TASVideos/BizHawk/blob/master/README.md#support-and-troubleshooting)
* [Contributing](https://github.com/TASVideos/BizHawk/blob/master/README.md#contributing)
	* [Testing](https://github.com/TASVideos/BizHawk/blob/master/README.md#testing)
* [Related projects](https://github.com/TASVideos/BizHawk/blob/master/README.md#related-projects)
* [License](https://github.com/TASVideos/BizHawk/blob/master/README.md#license)

## Features and systems

The BizHawk common features (across all cores) are:
* format and region detection for game images
* image corruption warning checked against database
* 10 savestate slots and save/load to file
* speed control, including frame stepping and rewinding
* memory view/search/edit in all parts of the emulated HW
* input recording (making TAS movies)
* screenshotting and recording video
* organised firmware
* input, framerate, and other overlays
* emulated controllers via a comprehensive input mapper
* Lua control over core and frontend (Windows only)

Supported consoles and PCs:

* N64 and [all](http://tasvideos.org/Bizhawk/N64.html) peripherals; Playstation (PSX); Saturn; Virtual Boy
* Game Boy Advance; Game Boy Color; Neo Geo Pocket (Color); WonderSwan (Color)
* Genesis and [all](https://bitbucket.org/eke/genesis-plus-gx/src/b573cd25853f9f8b5b941fc36506835e228144c6/wiki/Features.md?at=master&fileviewer=file-view-default) peripherals; SNES; TurboGrafx / SuperGrafx
* Atari Lynx; Game Boy; Game Gear; TI-83
* Atari 7800; Commodore 64 and [some](http://tasvideos.org/Bizhawk/C64.html) peripherals; Master System; NES; ZX Spectrum and [some](http://tasvideos.org/Bizhawk/ZXSpectrum.html) peripherals
* Apple II; Atari 2600; ColecoVision; IntelliVision
* [More](http://tasvideos.org/Bizhawk/CoreRoadMap.html) coming soon..?

See the *Usage* sections below for details about specific tools and config menus.

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

### GNU+Linux and macOS

Install BizHawk with your distro's package manager. The package name is given on each button below, and some buttons are links. For the changelog, [see TASVideos here](http://tasvideos.org/Bizhawk/ReleaseHistory.html).

[![Arch Linux (AUR) | bizhawk](https://img.shields.io/badge/Arch_Linux_(AUR)-bizhawk-%231793D1.svg?logo=arch-linux&style=popout)](https://aur.archlinux.org/packages/bizhawk)
[![Debian (Launchpad) | bizhawk](https://img.shields.io/badge/Debian_(Launchpad)-bizhawk-%23A81D33.svg?logo=debian&style=popout)](https://example.com/bizhawk)
[![Linux Mint (Launchpad) | bizhawk](https://img.shields.io/badge/Linux_Mint_(Launchpad)-bizhawk-%2387CF3E.svg?logo=data:image/png;base64,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&style=popout)](https://example.com/bizhawk)
[![macOS (Homebrew) | bizhawk](https://img.shields.io/badge/macOS_(Homebrew)-bizhawk-%23999999.svg?logo=apple&style=popout)](https://example.com/bizhawk)
[![Manjaro (AUR) | bizhawk](https://img.shields.io/badge/Manjaro_(AUR)-bizhawk-%2335BF5C.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAABRUlEQVR4nO3bQUrDQBTG8Qdu3dS91r2CAzYE3Cm9QBURmjq7Yk9QIsELJYJ2XYqh7UJbL9NcYFyoGAQZiFMygf8Hb/vNm192IREhhJBS+ou4Fa3vM71Kitv3B1P3lHer2qFXSTFYJ2l/EbesANev46y7HJkw1yZ4GdQ+5d2qdoS5Nt3lyNy8xakV4Hw+LOq+tGuA77mY322sAL48+W0AhLk2f93b2SE+A/zuAwAAAAAAAAAAAAAAAAAAcH3IqcNpJMBWAkATAVy+E2wkgE9dAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAADAZzrTaOPT0i67OtPI/suMyq6efVraZdfJ4+WTFWD3bP9Ypb1JMIsq/zzlHcAsKlTam+wFB0dWABHZEZFDEVHi4NO2f3S47FIi0v66GyHkJx/0rGTAdYdsEwAAAABJRU5ErkJggg==&style=popout)](https://aur.archlinux.org/packages/bizhawk)
[![Ubuntu (Launchpad) | bizhawk](https://img.shields.io/badge/Ubuntu_(Launchpad)-bizhawk-%23E95420.svg?colorA=772953&logo=ubuntu&style=popout)](https://example.com/bizhawk)

If you run `EmuHawkMono.sh` from a terminal, note that `File > Exit (Alt+F4)` doesn't terminate the process correctly, you'll need to send SIGINT (`^C`). The systems that currently work are: Game Boy + GBC (GBHawk), NES (NesHawk), Master System, Atari 7800, Commodore 64, ColecoVision, IntelliVision, TurboGrafx, and ZX Spectrum. See #1430 for progress.

Is your distro not there? Released binaries can be found right here on GitHub (same download as for Windows):

[![Misc. Linux | binaries](https://img.shields.io/badge/Misc._Linux-binaries-%23FCC624.svg?logo=linux&logoColor=black&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

If you download BizHawk this way, **don't mix different versions**, keep each version in its own folder. Run `EmuHawkMono.sh` to give Mono the library and executable paths — you can run it from anywhere, so putting it in a .desktop file is fine. If running the script doesn't start EmuHawk, you may need to edit it (if you use a terminal, it will say so in the output).

Linux distros are supported if the distributor is still supporting your version, you're using Linux 4.4/4.9/4.14/4.19 LTS or 4.20, and there are no updates available in your package manager. *Please* update and reboot.

macOS is supported from 10.11 "El Capitan" (Darwin 15.6). Apple doesn't seem to care about lifecycles, so we'll go with 6 months from the last security update.

## Building

If you want to test the latest changes without building BizHawk yourself, grab the developer build from [AppVeyor](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history). Pick the topmost one that doesn't say "Pull request", then click "Artifacts" and download `BizHawk_Developer-<datetime>-#<long hexadecimal>.zip`.

If you use GNU+Linux, there might be a `bizhawk-git` package or similar in the same repo as the main package. If it's available, installing it will automate the build process.

### Windows 7/8.1/10

TODO

powershell *should* be `C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe /p:Configuration=Release BizHawk.sln`, but getting errors w/ master

something something IDE is [VS Community 2017](https://visualstudio.microsoft.com/vs/community)

[Compiling](http://tasvideos.org/Bizhawk/Compiling.html)

### GNU+Linux and macOS

*Compiling* requires MSBuild and *running* requires Mono and WINE, but **BizHawk does not run under WINE** — only the bundled libraries are required.

Building is as easy as:
```sh
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master && cd BizHawk_master
msbuild /p:Configuration=Release BizHawk.sln
```

Remove the `/p:...` flag from MSBuild if you want debugging symbols.

If your distro isn't listed under *Installing* above, `libblip_buf` probably isn't in your package repos. You can easily [build it yourself](https://gitlab.com/TASVideos/libblip_buf/blob/unified/README.md).

Once built, see the *Installing* section, substituting the repo's `output` folder for the download.

Again, if your distro isn't listed there, you might get an "Unknown distro" warning in the terminal, and BizHawk may not open or may show the missing dependencies dialog. You may need to add your distro to the case statement in the script, setting `libpath` to the location of `d3dx9_43.dll.so` (please do share if you get it working).

## Usage

TODO

### TASing

This section refers to BizHawk specifically. For resources on TASing in general, see [Welcome to TASVideos](http://tasvideos.org/WelcomeToTASVideos.html).

TODO

[Commandline](http://tasvideos.org/Bizhawk/CommandLine.html)

[CompactDiscInfoDump](http://tasvideos.org/Bizhawk/CompactDiscInfoDump.html)

[Rerecording](http://tasvideos.org/Bizhawk/Rerecording.html)

[TAS movie file format](http://tasvideos.org/Bizhawk/TASFormat.html)

### Cores

A *core* is what we call the smaller bits of software that emulate just one system or family of systems, e.g. NES/Famicom. For the most part, there's a "best" core for each system, based on accuracy, but there's currently a bit of overlap in the cores BizHawk uses as noted below.

TABLE GOES HERE

## Support and troubleshooting

A short [FAQ](http://tasvideos.org/Bizhawk/FAQ.html) is provided on the [BizHawk wiki](http://tasvideos.org/Bizhawk.html). If your problem is one of the many not answered there, and you can't find it in the [issue tracker search](https://github.com/TASVideos/BizHawk/issues?q=is%3Aissue+ISSUE_KEYWORDS), check the [BizHawk forum](http://tasvideos.org/forum/viewforum.php?f=64) at TASVideos, or ask on IRC:

[![IRC | chat.freenode.net #bizhawk](https://img.shields.io/badge/IRC-chat.freenode.net_%23bizhawk-steelblue.svg?style=flat)](ircs://chat.freenode.net:6697/bizhawk)
[![Matrix (IRC bridge) | #freenode_#bizhawk:matrix.org](https://img.shields.io/badge/Matrix_(IRC_bridge)-%23freenode__%23bizhawk:matrix.org-mediumpurple.svg?logo=matrix&style=flat)](https://matrix.to/#/#freenode_#bizhawk:matrix.org)
[![freenode webchat | #bizhawk](https://img.shields.io/badge/freenode_webchat-%23bizhawk-lightcoral.svg?style=flat)](http://webchat.freenode.net/?channels=bizhawk)

If there's no easy solution, what you've got is a bug. Or maybe a feature request. Either way, [open a new issue](https://github.com/TASVideos/BizHawk/issues/new) (you'll need a GitHub account, signup is very fast).

## Contributing

BizHawk is Open Source Software, so you're free to modify it however you please, and if you do, we invite you to share! Under the MIT license, this is *optional*, just be careful with reusing cores as some have copyleft licenses.

If you'd like to fix bugs, check the issue tracker here on GitHub:

[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/issues)

It's a good idea to check if anyone is already working on an issue by asking on IRC (see *Support* above).

If you'd like to add a feature, first search the issue tracker for it. If it's a new idea, make your own feature request issue before you start coding.

### Testing

Dev builds are automated with AppVeyor, every green checkmark in the [commit history](https://github.com/TASVideos/BizHawk/commits/master) is a successful build and clicking the check takes you straight there. The full list is [here](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history), in future use the "dev builds" button at the top of this readme.

## Related projects

* [DeSmuME](https://desmume.org) for DS/Lite — cross-platform
* [Dolphin](https://dolphin-emu.org) for GameCube and (original) Wii — cross-platform
* [FCEUX](http://www.fceux.com/web/home.html) for NES/Famicom — TASing is Windows-only, but it should run on Unix
* [libTAS](https://github.com/clementgallet/libTAS) for Linux ELF — GNU+Linux-only, also emulates other emulators
* [lsnes](http://tasvideos.org/Lsnes.html) for SNES — Windows-only
* [openMSX](https://openmsx.org) for MSX — cross-platform

## License

From the [full text](https://github.com/TASVideos/BizHawk/blob/master/LICENSE):
> This repository contains original work chiefly in c# by the BizHawk team (which is all provided under the MIT License), embedded submodules from other authors with their own licenses clearly provided, other embedded submodules from other authors WITHOUT their own licenses clearly provided, customizations by the BizHawk team to many of those submodules (which is provided under the MIT license), and compiled binary executable modules from other authors without their licenses OR their origins clearly indicated.

In short, the frontend is MIT (Expat), beyond that you're on your own.
