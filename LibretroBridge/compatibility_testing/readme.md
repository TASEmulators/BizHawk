This is a fairly simple ~~script~~ wizard which automates the process of launching EmuHawk, setting a Libretro core, and loading a rom.

### Setup

You will need the .NET SDK and Runtime, 10.x or later. If you've built BizHawk then you likely already have them.
(Also you have to **build the main solution** since the wizard launches `/output/EmuHawk.exe`.
If you wanted to test an old release or someone else's build, set the `BIZHAWKBUILD_WRAPPER_SCRIPT` env. var.)

You'll need to provide Libretro cores. There's [a CI server](https://buildbot.libretro.com/stable/) hosting prebuilt binaries.
Drop them in `/output/Libretro/Cores` (doesn't respect config, but you can set env. var. `BIZHAWKBUILD_LIBRETRO_CORES`).
For cores which need firmware, it might work if you've set up `/output/Libretro/System` already.

Lastly, you need to provide roms.
Create folders `/compatibility_testing/roms/<sysID>`, then drop *one or more roms*&dagger; in each of them. They may be symlinks&dagger;.
The sysIDs match BizHawk's where possible&Dagger;. Watch for `No <sysID> roms available` in the wizard's output.

&dagger; Each file will be launched in EmuHawk, one at a time, then after the last one you're prompted to record success/failure.
`.cue` might work if you edit it to use absolute paths. Symlink'd `.cue` doesn't work.

&Dagger; The wizard only knows what sysID a core is for because it's hardcoded.
And that data is incomplete; watch for `Relevant sysID for <core name> is not known` in the wizard's output.

#### Nix

If you have Nix, you can use it to get everything but the roms. Run `nix-shell` in this dir.

### Running

`dotnet run tester_wizard.app.cs -- update-dev` or `dotnet run tester_wizard.app.cs -- update-baseline`,
then follow the prompts.

Once you've confirmed a rom is working/broken, close EmuHawk to continue.

Press Ctrl+C to take a break at any time. The wizard saves to a temporary file after each core,
then writes `{baseline,dev}.json` once you've gone through all available cores. (You downloaded them all, right?)
It's all just text in case you need to go in and make a manual edit.
