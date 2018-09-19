## CPCHawk

Still very much work in progress....

#### In Place

* CPC464, CPC6128
* Port IO decoding
* i8255 Programmable Peripheral Interface (PPI) chip emulation
* AY-3-8912 PSG (and Port IO)
* Keyboard/Joystick
* FDC and FDD devices
* .DSK image parsing and identification (to auto differenciate from ZX Spectrum disk bootloader)

#### There, but needs more work
* CRCT (Cathode Ray Tube Controller) chip emulation
* Amstrad Gate Array chip emulation
* Video rendering (modes 0, 1, 2 & 3)
* Datacorder (tape) device
* .CDT tape image file support

#### Not Yet
* CPC664, CPC464plus, CPC6128plus, GX4000 models
* Expansion IO

#### Known Issues
* The CRCT and Gatearray chips are undergoing a re-write at the moment, so video emulation is nowhere near accurate (yet)
* .CDT tape image files will nearly always fail to load - timing conversion is still needed (so the tape device 'plays back' at the wrong speed)

-Asnivor
