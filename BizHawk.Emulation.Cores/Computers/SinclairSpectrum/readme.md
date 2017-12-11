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
* IStatable (although this is not currently working/implemented properly during tape load operations)
* ISettable core settings
* IMemoryDomains (I think)

### Work in progress
* Exact emulator timings
* Floating memory bus emulation

### Not working
* IDebuggable
* ZX Spectrum Plus3 emulation
* Default keyboard keymappings (you have to configure yourself in the core controller settings)
* Manual tape device control (at the moment the tape device detects when the spectrum goes into 'loadbytes' mode and auto-plays the tape. This is not ideal and manual control should be implemented so the user can start/stop manually, return to zero etc..)
* Only standard spectrum tape blocks currently work. Any fancy SpeedLock encoded (and similar) blocks do not

### Known bugs
* Audible 'popping' from the emulated buzzer after a load state operation

-Asnivor
