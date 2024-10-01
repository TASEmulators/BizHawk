## CPCHawk

Work has started up again as of Sep2024.

#### In Place
* CPC464, CPC6128 (default)
* Port IO decoding
* i8255 Programmable Peripheral Interface (PPI) chip emulation
* AY-3-8912 PSG (and Port IO)
* Keyboard/Joystick
* FDC and FDD devices
* .DSK image parsing and identification (to auto differenciate from ZX Spectrum disk bootloader)

#### Right now
* Gate array implementation undergoing a full re-write
* CRTC implementations are being re-written (based on https://shaker.logonsystem.eu/ACCC1.8-EN.pdf)
* CRT screen emulation in progress
* Amstrad Gate Array chip emulation
* Z80 timing verification

#### Future..
* CPC664, CPC464plus, CPC6128plus, GX4000 models
* Expansion IO
* Working Datacorder (tape) emulation and handling of .CDT files
* Memory expansions
* External peripherals (Speech Synthesizers etc..)

#### Known Issues
* Lots of things

#### References in use
* https://shaker.logonsystem.eu/ACCC1.8-EN.pdf
* https://www.cpcwiki.eu/forum/amstrad-cpc-hardware/need-plustest-dsk-testbench-9-output-on-original-cpc-6128/

#### Test suites
* http://www.winape.net/download/plustest.zip
* https://shaker.logonsystem.eu/shaker26.dsk






-Asnivor
