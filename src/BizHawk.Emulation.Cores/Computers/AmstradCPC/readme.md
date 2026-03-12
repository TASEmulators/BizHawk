# CPCHawk

Work has started up again as of Sep2024.

## In Place
* CPC464, CPC6128 (default)
* Port IO decoding
* i8255 Programmable Peripheral Interface (PPI) chip emulation
* AY-3-8912 PSG (and Port IO)
* Keyboard/Joystick
* FDC and FDD devices
* .DSK image parsing and identification (to auto differenciate from ZX Spectrum disk bootloader)

## Right now
* Gate array implementation undergoing a full re-write
* CRTC implementations are being re-written (based on https://shaker.logonsystem.eu/ACCC1.8-EN.pdf)
* CRT screen emulation in progress
* Amstrad Gate Array chip emulation
* Z80 timing verification

## Future..
* CPC664, CPC464plus, CPC6128plus, GX4000 models
* Expansion IO
* Working Datacorder (tape) emulation and handling of .CDT files
* Memory expansions
* External peripherals (Speech Synthesizers etc..)
* Optimisation of EVERYTHING

## Known Issues
* Lots of things

## References in use
* https://shaker.logonsystem.eu/ACCC1.8-EN.pdf
* https://www.cpcwiki.eu/forum/amstrad-cpc-hardware/need-plustest-dsk-testbench-9-output-on-original-cpc-6128/

## Test suites


### WinAPU PlusTest (http://www.winape.net/download/plustest.zip)

#### Test 1: ASIC Sprite test
Plus models and GX4000 only - awaiting implementation

#### Test 2: Monitor HSYNC test
Failing

#### Test 3: Horizontal Split and Soft-Scroll test
Failing

#### Test 4: Interrupt, VSync and R52 timing test
Unknown

#### Test 5: Instruction timing test

All tests passing

#### Test 6: Register 0 test
Unknown

#### Test 7: Register 4 test
Unknown

#### Test 8: Register 9 test
Unknown

#### Test 9: Interrupt Wait state timing test

|PFX| OPC | Description | Comments |
|:-:|:-:|:-:|:-:|
|None| C0:2/4 | RET NZ | |
|None| C8:4/2 | RET Z | |
|None| D0:2/4 | RET NC | |
|None| D8:4/2| RET C | |
|None| E0:2/4 | RET Po | |
|None| E8:4/2 | RET Pe | |
|ED| 46:C | IM $0 | |
|ED| 4E:C | IM $0 | |
|ED| 66:C | IM $0 | |
|ED| 6E:C | IM $0 | |



### Amstrad Diagnostics (https://github.com/llopis/amstrad-diagnostics)

Program stalls. Likely because the firmware vertical flyback checks are not happening at the right time.
Might be fixed when port timing is correct.


### Shaker Tests (https://shaker.logonsystem.eu/shaker26.dsk)

Need to correct CRTC emulation and port access timing before even attempting these.

-Asnivor
