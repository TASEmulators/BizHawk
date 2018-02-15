## ZXHawk

At this moment this is still *very* experimental and needs a lot more work.

### Implemented and sorta working
* IEmulator
* ZX Spectrum 48k, 128k & Plus2 models
* ULA video output (implementing IVideoProvider)
* ULA Mode 1 VBLANK interrupt generation
* IM2 Interrupts and DataBus implementation (thanks Aloysha)
* Beeper/Buzzer output (implementing ISoundProvider)
* AY-3-8912 sound chip implementation
* Keyboard input (implementing IInputPollable)
* Kempston joystick (mapped to J1 currently)
* Tape device that will load spectrum games in realtime (*.tzx and *.tap)
* IStatable
* ISettable core settings
* IMemoryDomains (I think)

### Work in progress
* Exact emulator timings
* Floating memory bus emulation
* Tape auto-loading routines (currently you have to manually start and stop the virtual tape device)

### Not working
* IDebuggable
* ZX Spectrum Plus3 emulation
* Default keyboard keymappings (you have to configure yourself in the core controller settings)

### Known bugs
* Audible 'popping' from the emulated buzzer after a load state operation

-Asnivor
