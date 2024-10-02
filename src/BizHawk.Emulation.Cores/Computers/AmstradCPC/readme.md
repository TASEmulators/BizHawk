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

| Prefix | OPC | Inst. | Comments |
|:----:|:----:|:-----:|:------------:|
|NONE| D3:4 | OUT A |				|
|NONE| DB:4 | IN A  |				|
|NONE| D3:5 | OUT A |				|	
|NONE| DB:5 | IN A  |				|
|ED| 41:3 | OUT (C), B     |              |
|ED| 49:3 | OUT (C), C      |              |
|ED| 51:3 | OUT (C), D       |              |
|ED| 59:3 | OUT (C), E      |              |
|ED| 61:3 | OUT (C), H      |              |
|ED| 69:3 | OUT (C), L      |              |
|ED| 71:3 | OUT (C), 0      |              |
|ED| 79:3 | OUT (C), A      |              |
|ED| A2:6 | INI      |              |
|ED| A3:6 | OUTI      |              |
|ED| AA:6 | IND      |              |
|ED| B2:7/6| INIR     |              |
|ED| B3:7/6| OTIR     |              |
|ED| BA:7/6| INDR    |              |
|ED| BB:7/6| OTDR    |              |
|DD CB| D3:5| SET 2, (ix+d), e    |              |
|DD CB| DB:5| SET 3, (ix+d), e    |              |

Everything else passes. Almost certainly the problems observed relate to IO timing.

#### Test 6: Register 0 test
Unknown

#### Test 7: Register 4 test
Unknown

#### Test 8: Register 9 test
Unknown

#### Test 9: Interrupt Wait test



### Amstrad Diagnostics (https://github.com/llopis/amstrad-diagnostics)

Program stalls. Likely because the firmware vertical flyback checks are not happening at the right time.
Might be fixed when port timing is correct.


### Shaker Tests (https://shaker.logonsystem.eu/shaker26.dsk)

Need to correct CRTC emulation and port access timing before even attempting these.

-Asnivor
