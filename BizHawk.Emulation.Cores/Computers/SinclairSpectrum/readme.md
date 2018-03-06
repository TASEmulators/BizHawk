## ZXHawk

At the moment this is very experimental and is still actively being worked on.

### Implemented and sorta working
* IEmulator
* ZX Spectrum 48k, 128k & Plus2 models
* ULA video output (implementing IVideoProvider)
* ULA Mode 1 VBLANK interrupt generation
* IM2 Interrupts and DataBus implementation (thanks Aloysha)
* Beeper/Buzzer output (implementing ISoundProvider)
* AY-3-8912 sound chip implementation
* Keyboard input (implementing IInputPollable)
* Default keyboard keymappings
* Kempston, Cursor and Sinclair (Left & Right) joysticks emulated
* Tape device that will load spectrum games in realtime (*.tzx and *.tap)
* Most tape protection/loading schemes that I've tested are currently working (see caveat below)
* IStatable
* ISettable core settings
* IDebuggable (for what it's worth)
* Tape auto-loading routines (as a setting)

### Work in progress
* Exact emulator timings
* Floating memory bus emulation
* TASStudio (need to verify that this works as it should)

### Not working
* ZX Spectrum Plus3 emulation

### Known bugs
* Audible 'popping' from the emulated buzzer after a load state operation (maybe this is a normal thing)
* Speedlock tape protection scheme doesn't appear to load correctly

-Asnivor
