## ZXHawk

### Whats in the box?
* Emulates the Sinclair ZX Spectrum 16k, 48k, 128k, +2, +2A & +3
* Accurate Z80A implementation
* Precise screen timing, floating bus, memory contention and port contention for all models
* Full keyboard emulation
* Kempston, Cursor and Sinclair joysticks emulated
* Full beeper and AY-3-3912 sound emulation
* Tape device (datacorder) emulation
* Internal 3" disk drive emulation (found in the +3 model)
* Currently supports the following tape image formats: *.tzx, *.tap, *.pzx, *.csw, *.wav
* Currently supports the following disk image formats (+3 only): *.dsk
* Fully integrated into the Bizhawk ecosystem
* See the ZXSpectrum menu for all available configuration options

### Firmware
ZXHawk ships with the official ZX Spectrum ROMs embedded (licensed by Amstrad).

"Amstrad have kindly given their permission for the redistribution of their copyrighted material but retain that copyright"
http://www.worldofspectrum.org/permits/amstrad-roms.txt

### Issues
* Tape images are read-only. This may change in the future, but with bizhawk's savestate system this is not strictly a necessity 
* Disk images are currently read-only as well. There is certain write functionality implemented within the emulated UPD756A disk controller (in order to make games work that require this), but this is not persistent
* Disk drive emulation timing is currently not accurate, meaning that disk games will load faster than they would on a real +3. Due to how the Spectrum interfaces with the disk controller though, this should not cause any compatibility issues

Any questions, issues or bug reports, either use the GitHub issue tracker, or post in the forum thread:

http://tasvideos.org/forum/viewtopic.php?t=20004

-Asnivor
