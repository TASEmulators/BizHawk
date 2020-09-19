# BizHawk

A multi-system emulator written in C#. As well as quality-of-life features for casual players, it also has recording/playback and debugging tools, making it the first choice for TASers (Tool-Assisted Speedrunners).

[![(latest) release | GitHub](https://img.shields.io/github/release/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)
[![latest dev build | AppVeyor](https://img.shields.io/badge/latest_dev_build-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/build/artifacts)
[![latest dev build | GitLab CI](https://img.shields.io/badge/latest_dev_build-GitLab_CI-orange.svg?logo=gitlab&style=popout)](https://gitlab.com/TASVideos/BizHawk/pipelines/master/latest)
[![GitHub open issues counter](https://img.shields.io/github/issues-raw/TASVideos/BizHawk.svg?logo=github&logoColor=333333&style=popout)](https://github.com/TASVideos/BizHawk/issues)

---

Jump to:
* Installing
	* [Windows](#windows)
	* [Unix](#unix)
	* [Development builds](#development-builds)
* Building
	* [Windows](#windows-1)
	* [Unix](#unix-1)
* [Usage](#usage)
	* [TASing](#tasing)
	* [Cores](#cores)
* [Support and troubleshooting](#support-and-troubleshooting)
* [Contributing](#contributing)
	* [EmuHawk development](#emuhawk-development)
	* [Core development](#core-development)
	* [Testing/QA](#testingqa)
	* [Localization](#localization)
	* [License](#license)
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
* GCE Vectrex
* Magnavox Odyssey² / Videopac G7000
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
	* Saturn
	* SG-1000
* Sinclair ZX Spectrum
* Sony Playstation (PSX)
* Texas Instruments TI-83
* Uzebox
* more coming soon..?

See [*Usage*](#usage) below for info on basic config needed to play games.

[to top](#bizhawk)

## Installing

### Windows

Released binaries can be found right here on GitHub (also linked at the top of this readme):

[![Windows | binaries](https://img.shields.io/badge/Windows-binaries-%230078D6.svg?logo=windows&logoColor=0078D6&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

Click `BizHawk-<version>.zip` to download it. Also note the changelog, the full version of which is [here at TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html). Extract it anywhere, but **don't mix different versions** of BizHawk, keep each version in its own folder. You may move or rename the folder containing `EmuHawk.exe`, even to another drive — as long as you keep all the files together, and the prerequisites are installed when you go to run it.

Run `EmuHawk.exe` to start. If startup is blocked by a Windows SmartScreen dialog, click "More Info" to reveal the override button. Third-party antivirus may also block startup.

EmuHawk does have some prerequisites which it can't work without (it will let you know if they're missing). The list is [here](https://github.com/TASVideos/BizHawk-Prereqs/blob/master/README), and we've made an all-in-one installer which you can get [here](https://github.com/TASVideos/BizHawk-Prereqs/releases/latest). You should only have to run this once per machine, unless the changelog says we need something extra.

We will be [following Microsoft](https://support.microsoft.com/en-us/help/13853/windows-lifecycle-fact-sheet) in dropping Windows support, that is, we reserve the right to ignore your problems unless you've updated to at least Win10 1909 or Win8.1 KB4550961.

A "backport" release, [1.13.2](https://github.com/TASVideos/BizHawk/releases/tag/1.13.2), is available for users of Windows XP, 7, or 8.1 32-bit. It has many bugs that will never be fixed and it doesn't have all the features of the later versions.

[to top](#bizhawk)

### Unix

**Note**: There's only one dev working on Linux (@YoshiRulz)! Please have patience, and try not to bother everyone else.

Install the listed package with your package manager (some buttons are links to the relevant package). The changelog can be found [on TASVideos](http://tasvideos.org/Bizhawk/ReleaseHistory.html).

[![Manjaro | bizhawk-monort (AUR)](https://img.shields.io/badge/Manjaro-bizhawk--monort_(AUR)-%2335BF5C.svg?logo=manjaro&style=popout)](https://aur.archlinux.org/packages/bizhawk-monort)

No package for your distro? Grab the latest release here on GitHub (it's the same as the Windows version):

[![Misc. Linux | bizhawk-monort](https://img.shields.io/badge/Misc._Linux-bizhawk--monort-%23FCC624.svg?logo=linux&logoColor=black&style=popout)](https://github.com/TASVideos/BizHawk/releases/latest)

If you download BizHawk this way, **don't mix different versions**, keep each version in its own folder. The runtime dependencies are glibc, Mono "complete", VB.NET, OpenAL, and `lsb_release`. .NET Core is **not** a runtime dependency, only Mono. WINE is also **not** a runtime dependency.

Run `EmuHawkMono.sh` to start EmuHawk—you can run it from anywhere, so creating a .desktop file to wrap the script is fine. The shell script should print an error if it fails, otherwise it's safe to ignore console output. There are some command-line options which aren't well-documented; you might be able to figure them out from [the code](https://github.com/TASVideos/BizHawk/blob/e128cb82f211dade27d04a21737e073374098f49/src/BizHawk.Client.EmuHawk/ArgParser.cs). They're the same on Windows, with one exception: passing `--mono-no-redirect` *as the first argument* prints stdout to the console. *Not* passing it will redirect stdout to a file.

Most features and cores work, notable omissions being Lua support, Mupen64Plus (N64), and Octoshock (PSX). See [#1430](https://github.com/TASVideos/BizHawk/issues/1430) for details.

[to top](#bizhawk)

#### macOS (legacy BizHawk)

EmuHawk depends on certain libraries for graphics, and these don't work on macOS. Users on macOS have three options:
* Use another machine with Windows or Linux, or install either in a VM (WINE is not a VM).
* Use an older 1.x release which was ported to macOS by @Sappharad (with replacements for the missing libraries). Links and more details are in [this TASVideos forum thread](http://tasvideos.org/forum/viewtopic.php?t=12659) (jump to last page for latest binaries).
* For the technically-minded, download the [source](https://github.com/Sappharad/BizHawk/tree/MacUnixMonoCompat) of an older 2.x release. @Sappharad put a lot of work into it but ultimately decided to stop.

[to top](#bizhawk)

### Development builds

Development builds are made automatically whenever someone contributes. Because of this, we recommend using a release for work that requires stability (such as TASing), and only switching to a dev build if there's a specific change or addition you need.

[![recent dev builds | AppVeyor](https://img.shields.io/badge/recent_dev_builds-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/history)
[![recent dev builds | GitLab CI](https://img.shields.io/badge/recent_dev_builds-GitLab_CI-orange.svg?logo=gitlab&style=popout)](https://gitlab.com/TASVideos/BizHawk/pipelines)
[![latest dev build | AppVeyor](https://img.shields.io/badge/latest_dev_build-AppVeyor-orange.svg?logo=appveyor&logoColor=333333&style=popout)](https://ci.appveyor.com/project/zeromus/bizhawk-udexo/build/artifacts)
[![latest dev build | GitLab CI](https://img.shields.io/badge/latest_dev_build-GitLab_CI-orange.svg?logo=gitlab&style=popout)](https://gitlab.com/TASVideos/BizHawk/pipelines/master/latest)

Click one of the buttons above to download a dev build (they're also at the top of this readme). AppVeyor uses Windows and GitLab CI uses Linux, but they work all the same.
* On the AppVeyor page for a Build, click "Artifacts", then `BizHawk_Developer-<datetime>-#<long hexadecimal>.zip`.
* On the GitLab CI page for a Pipeline, click "Jobs", then the download button on the right under the heading "Package". (On the Pipelines list page, there's also a download button on each Pipeline—choose `package:archive` there.)

To find the dev builds for a specific commit, you can click the green checkmark next to it (in the [commit history](https://github.com/TASVideos/BizHawk/commits/master), for example) for a dropdown, then click either "Details" link to go to AppVeyor/GitLab.

## Building

### Windows

If you don't have Git, download [an archive of `master`](https://github.com/TASVideos/BizHawk/archive/master.zip). If you have WSL, Git BASH, or similar, clone the repo with:
```
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
```

Once it's downloaded and extracted, go into the repo's `Dist` folder and run `QuickTestBuildAndPackage_Release.bat`. This is the same process used by AppVeyor.

For anything more complicated than just building, you'll need an IDE like [VS Community 2019](https://visualstudio.microsoft.com/vs/community), currently the best free C# IDE (you may prefer Rider, VS Code, or something else). To build with VS, open `BizHawk.sln` and use the toolbar to choose `Release | Any CPU | BizHawk.Client.EmuHawk` and click the Start button. See [*Compiling* at TASVideos](http://tasvideos.org/Bizhawk/Compiling.html) for more detailed instructions (warning: somewhat outdated).

[to top](#bizhawk)

### Unix

Before you can build, you'll need the .NET Core SDK 3.1 (package name is usually `dotnet-sdk-3.1`, see [full instructions](https://docs.microsoft.com/en-gb/dotnet/core/install/sdk?pivots=os-linux)). You may need to uninstall MSBuild first. Once it's installed, run:
```sh
git clone https://github.com/TASVideos/BizHawk.git BizHawk_master
# or ssh: git clone git@github.com:TASVideos/BizHawk.git BizHawk_master
BizHawk_master/Dist/BuildRelease.sh
```

The assemblies are put in `BizHawk_master/output`, so if you have the runtime dependencies (see [*Installing*](#unix)) you can call `BizHawk_master/output/EmuHawkMono.sh`. Reminder that stdout is redirected to `BizHawk_master/output/EmuHawkMono_laststdout.txt` unless `--mono-no-redirect` is the first command-line argument.

VS 2019 isn't available on Linux, but Rider and VS Code are. You can always code from the command line...

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

BizHawk can be configured to flush saves to disk automatically in `Config` > `Customize` > `Advanced AutoSaveRAM`. Upon closing the ROM (which includes any core reboot) BizHawk may try to flush save RAM automatically as well.

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

### External tools

Creating a GUI with Lua scripts is fiddly. If you know some C# (or another .NET language), you can replace your Lua script with an *external tool*. See the [ext. tools wiki](https://github.com/TASVideos/BizHawk-ExternalTools/wiki) for more details.

We're looking to create [a catalog](https://github.com/TASVideos/BizHawk-ExternalTools/wiki/Catalog) of tools made by the community, share yours on IRC/Discord (links [below](#support-and-troubleshooting)).

### Cores

A *core* is what we call the smaller bits of software that emulate just one system or family of systems, e.g. NES/Famicom. For the most part, there's a "best" core for each system, based on accuracy, but there are a few alternative cores which are *faster and less accurate*.

*Hawk* cores are part of the BizHawk project. All other cores are ported, mainly from the Mednafen project.

System | Core | Alt. Cores
--:|:--|:--
Apple II | Virtu |
Atari 2600 | Atari2600Hawk |
Atari 7800 | A7800Hawk |
Atari Lynx | Handy |
Commodore 64 | C64Hawk |
ColecoVision | ColecoHawk |
Game Boy / Color | GBHawk | Gambatte
Game Boy Advance | mGBA |
Intellivision | IntelliHawk |
N64 | Mupen64Plus |
Neo Geo Pocket | NeoPop |
NES | NesHawk | QuickNes |
Odyssey² | O2Hawk |
PC-FX | T.S.T. |
Playstation (PSX) | Octoshock |
Sega 32X | PicoDrive |
Sega Game Gear | SMSHawk |
Sega Genesis | Genplus-gx |
Sega Master System | SMSHawk |
Sega Saturn | Saturnus |
SNES | BSNES | Faust, Snes9x
Super Game Boy | BSNES | SameBoy
TI-83 | TI83Hawk |
TurboGrafx | TurboNyma | HyperNyma, PCEHawk
Uzebox | Uzem |
Vectrex | VectrexHawk |
Virtual Boy | Virtual Boyee |
WonderSwan / Color | Cygne |
ZX Spectrum | ZXHawk |

There are also works-in-progress for:
* Amstrad CPC (home-grown core)
* Fairchild Channel F (home-grown core)
* [MAME](https://mamedev.org)
* MSX (home-grown core)
* Nintendo DS via [melonDS](http://melonds.kuribo64.net)
* Playstation 2 via [Dobiestation](https://github.com/PSI-Rockin/DobieStation)
* others maybe ([candidates](https://gitlab.com/TASVideos/BizHawk/snippets/1890492))

Please don't bother core devs about these WIPs unless you're looking to contribute in some way.

[to top](#bizhawk)

## Support and troubleshooting

A short [FAQ](http://tasvideos.org/Bizhawk/FAQ.html) is provided on the [BizHawk wiki](http://tasvideos.org/Bizhawk.html). If your problem is one of the many not answered there, and you can't find it in the [issue tracker search](https://github.com/TASVideos/BizHawk/issues?q=is%3Aissue+PUT_ISSUE_KEYWORDS_HERE), you can try:
* `#bizhawk` on freenode IRC ([via web browser](https://webchat.freenode.net/#bizhawk); via HexChat/Irssi: `chat.freenode.net:6697`; [via Matrix](https://matrix.to/#/#freenode_#bizhawk:matrix.org))
* `#emulation` (TASers: `#tas-production`) on [the TASVideos Discord](https://discordapp.com/invite/GySG2b6)
* The TASVideos [forum for BizHawk](http://tasvideos.org/forum/viewforum.php?f=64)

You can [open a new issue](https://github.com/TASVideos/BizHawk/issues/new) at any time if you're logged in to GitHub.

[to top](#bizhawk)

## Contributing

### EmuHawk development

Do you want your name next to [these fine people](https://github.com/TASVideos/BizHawk/graphs/contributors)? Fork the repo and work on one of our [many open issues](https://github.com/TASVideos/BizHawk/issues). If you ask on IRC/Discord (see above), you might get more info about the problem—or you might find someone else is also working on it. It's especially important to ask about adding new features.

All the source code for EmuHawk is in `/src`. The project file, `/src/BizHawk.Client.EmuHawk/BizHawk.Client.EmuHawk.csproj`, includes the other projects [in a tree](https://gitlab.com/TASVideos/BizHawk/snippets/1886666).

When opening a PR:
* Consider making changes over multiple commits instead of one large commit. Bonus points if each commit is a working build.
* Rebase instead of merging when pulling changes.
* Don't use the `master` branch of your fork! Using another branch makes rebasing so much easier.
* Our test suite is small, but it's still worth running. Build the executable project `BizHawk.Tests`.
	* If you fork on GitLab, the tests will run in CI.
* For the time being, code style is checked manually. Please use CRLF, tabs, and [Allman braces](https://en.wikipedia.org/wiki/Indentation_style#Allman_style) in new files.
	* Static code analysis is configured but disabled—build with `-p:MachineRunAnalyzersDuringBuild=true`.
		* If you fork on GitLab, the Analyzers will run in CI if you use `git push -o ci.variable="BIZHAWKBUILD_USE_ANALYZERS=true"` (or otherwise set that env. var).

[to top](#bizhawk)

### Core development

We're not particularly interested in PRs adding cores out-of-the-blue, but if you have experience in emulator development please get in touch on IRC/Discord.

[to top](#bizhawk)

### Testing/QA

Not a programmer? You can still be helpful by grabbing a recent [dev build](#development-builds) and reproducing old bugs, i.e. checking if they've been fixed or not.

[to top](#bizhawk)

### Localization

ping YoshiRulz on IRC or Discord (`YoshiRulz#4472`)

[to top](#bizhawk)

### License

EmuHawk and DiscoHawk can be used by anyone for any purpose allowed by the permissive *MIT License* (Expat). The [full text](https://github.com/TASVideos/BizHawk/blob/master/LICENSE) is very short.

Any developers looking to re-use code from BizHawk in their own work should understand which files the license applies to. It's included in the text, but tl;dr: anything outside `/src` isn't ours and we can't give you permission to share, use, or sell it. That means not all the files included with BizHawk *releases or dev builds* are free to share, either.

Disclaimer time! Can't have emulation software without a disclaimer...
> Following the terms of our license does not make you immune from other contracts or laws.
> Some or all of the following may be illegal where you live: creating a copy of non-free software for backup purposes ("dumping" or "ripping"); distributing copies of non-free software; soliciting pirated copies of software; knowingly posessing pirated copies of software; importing software from the USA (GitHub and TASVideos are American entities); using a backup copy of non-free software without the original.
> For obvious reasons, **we cannot and will not distribute dumped games or firmware that is under copyright**.

[to top](#bizhawk)

## Related projects

* [Dolphin](https://dolphin-emu.org) for GameCube and Wii — cross-platform
* [FCEUX](http://www.fceux.com/web/home.html) for NES/Famicom — cross-platform; TASing is Windows-only
* [libTAS](https://github.com/clementgallet/libTAS) for ELF (Linux desktop apps) — requires GNU+Linux host; also emulates other emulators
* [lsnes](http://tasvideos.org/Lsnes.html) for GB and SNES — cross-platform

Emulators for other systems can be found on the [EmulatorResources page](http://tasvideos.org/EmulatorResources.html) at TASVideos. The [TASVideos GitHub page](https://github.com/TASVideos) also holds copies of other emulators and plugins where development happens sometimes, their upstreams may be of use.

[to top](#bizhawk)
