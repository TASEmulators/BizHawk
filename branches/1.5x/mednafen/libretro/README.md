# Mednafen PSX libretro

This is port of Mednafen PSX core to the libretro API.
It currently runs on Linux, OSX and possibly Windows.

## Running

To run this core, the "system directory" must be defined if running in RetroArch.
Here, the PSX BIOSes must be placed, $sysdir/SCPH550{0,1,2} for Japanese, NA and EU regions respectively.
Memory cards will also be saved to this system directory.

## Loading ISOs

Mednafen differs from other PS1 emulators in that it reads a .cue sheet that points to an .iso/.bin whatever.
If you have e.g. <tt>foo.iso</tt>, you should create a foo.cue, and fill this in:

    FILE "foo.iso" BINARY
       TRACK 01 MODE1/2352
          INDEX 01 00:00:00

After that, you can load the <tt>foo.cue</tt> file as a ROM.
Note that this is a dirty hack and will not work on all games.
Ideally, make sure to use rips that have cue-sheets.

