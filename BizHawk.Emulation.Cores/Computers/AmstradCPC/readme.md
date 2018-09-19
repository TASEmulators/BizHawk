## CPCHawk

This may or may not work out. But I figured I could at least start by building on the existing ZXHawk implementation (the machines do have CPU, tape format, PSG and disk drive/controller in common).

We'll see how that goes...

#### In Place (but probably requires more work)

* CPC464 model template
* Non-paged memory
* Standard lower and upper ROM
* Port IO decoding
* CRCT (Cathode Ray Tube Controller) chip emulation
* Amstrad Gate Array chip emulation
* Video rendering (mode 1)
* i8255 Programmable Peripheral Interface (PPI) chip emulation
* AY-3-8912 PSG Port IO
* Keyboard/Joystick
* .CDT tape image file support

#### Not Yet

* CPC664, CPC6128, CPC464plus, CPC6128plus, GX4000 models
* RAM banking
* Upper ROM banking
* Video rendering (modes 0, 2 & 3)
* AY-3-8912 PSG sound output
* Datacorder (tape) device
* FDC and FDD devices
* .DSK image parsing and identification (to auto differenciate from ZX Spectrum disk bootloader)
* Expansion IO

-Asnivor
