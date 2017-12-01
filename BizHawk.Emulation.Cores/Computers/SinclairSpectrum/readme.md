## ZXHawk

At this moment this is still *very* experimental and needs a lot more work.

### Implemented and sorta working
* IEmulator
* ZX Spectrum 48k model
* ULA video output (implementing IVideoProvider)
* ULA Mode 1 VBLANK interrupt generation
* IM2 Interrupts and DataBus implementation (thanks Aloysha)
* Beeper/Buzzer output (implementing ISoundProvider)
* Keyboard input (implementing IInputPollable)
* Tape device that will load spectrum games in realtime (*.tzx and *.tap)
* IStatable (although this is not currently working/implemented properly during tape load operations)

### Some progress
* ISettable - There are some Settings and SyncSettings instantiated, although they are not really used and I haven't yet figured out how to wire these up to the front-end yet

### Not working
* IMemoryDomains - I started looking at this but didn't really know what I was doing yet
* IDebuggable
* Default keyboard keymappings (you have to configure yourself in the core controller settings)
* Joystick support (I still need to implement a Kemptston joystick and interface)
* Manual tape device control (at the moment the tape device detects when the spectrum goes into 'loadbytes' mode and auto-plays the tape. This is not ideal and manual control should be implemented so the user can start/stop manually, return to zero etc..)
* Only standard spectrum tape blocks currently work. Any fancy SpeedLock encoded (and similar) blocks do not

### Known bugs
* The 'return' keyboard key is acting the same as Space/Break when doing a BASIC RUN or LOAD "" command. The upshot of this is that upon boot, when you go to load the attached spectrum cassette (you have to type: "J", then "SYMSHIFT + P", then "SYMSHIFT + P", then RETURN) it more often than not interrupts the load routine. You then have to try again but hitting the RETURN key at the end of the sequence for as small a time as possible. Rinse and repeat until the load process starts. Clearly NOT ideal.
* Audible 'popping' from the emulated buzzer after a load state operation
