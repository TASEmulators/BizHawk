## ZXHawk

At the moment this is experimental and is still being worked on.

### Implemented and working (as far as I can tell)
* IEmulator
* ZX Spectrum 48k, 128k, +2 & +2A models
* ULA video output (implementing IVideoProvider)
* ULA Mode 1 VBLANK interrupt generation
* IM2 Interrupts and DataBus implementation (thanks Aloysha)
* Beeper/Buzzer output (implementing ISoundProvider)
* AY-3-8912 sound chip implementation (multiple stereo or mono  panning options available as a setting)
* Keyboard input (implementing IInputPollable)
* Default keyboard keymappings
* Kempston, Cursor and Sinclair (Left & Right) joysticks emulated
* Tape device that will load spectrum games in realtime (*.tzx and *.tap)
* Most tape protection/loading schemes that I've tested are currently working
* IStatable
* ISettable core settings
* IDebuggable (for what it's worth)
* DeterministicEmulation as a SyncSetting, LagFrame detection and FrameAdvance render & renderSound bools respected (when DeterministicEmulation == false)
* Tape auto-loading routines (as a setting - default ON)
* Basic tape block navigation (NextBlock, PrevBlock)
* Tape-related OSD messages (verbosity level configured in settings)

### Work in progress
* ZX Spectrum +3 emulation (partially working, see below)
* Exact emulator timings
* Floating memory bus emulation
* TASStudio (need to verify that this works as it should)

### Not working
* +3 disk drive - no implementation yet
* Hard & Soft Reset menu options in the client (they are greyed out for some reason)

### Help needed
* I'm not a TASer, i've never TASed before. It would be really useful if someone (anyone) can build this branch and test this core from a TAS-workflow / TAStudio perpective. There may still be some work to do an exact timings and memory contention, but otherwise this core is able to play the majority of speccy games out there.

-Asnivor
