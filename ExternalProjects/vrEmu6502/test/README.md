## Test runner

[vrEmu6502Test.c](vrEmu6502Test.c)
* The source file for the test runner.
* It can be built using the solution in the [msvc](../msvc) folder.
* The test runner binary (Windows) is included in the [bin](../bin) directory. 


### Options:

The test runner accepts Intel HEX files provided by the Klauss Dormann tests (located in the [programs](programs) folder)

```Usage:
Usage:
vrEmu6502Test [OPTION...] <testfile.hex>

Options:
  -c <cpumodel>     one of "6502", "65c02", "w65c02", "r65c02". defaults to 65c02.
  -i                output instruction count on each row
  -f <lines>        filter output to every #<lines> lines
  -h                output help and exit
  -m <from>[:<to>]  output given memory address or range
  -q                quiet mode - only print report
  -r <addr>         override run address
  -v [<count>]      verbose output from instruction #<count>
```

### Example output:

`..\bin\vrEmu6502Test --cpu w65c02 --quiet 21986970 -mem 0x08:0x0f programs\65C02_extended_opcodes_test.hex`

```
  -------------------------------------
          vrEmu6502 Test Runner
  -------------------------------------
    Copyright (c) 2022 Troy Schrapel
  https://github.com/visrealm/vrEmu6502
  -------------------------------------

Running test:                "programs\65C02_extended_opcodes_test.hex"

Options:
  Processor model:           Western Design Centre 65C02
  Output filtering:          Quiet until #21986970
  Output memory:             $0008 - $000f
  Start address:             $0400


Step #      | PC    | Instruction    | Acc | InX | InY | SP   Top |   Status    | $0008 - $000f
------------+-------+----------------+-----+-----+-----+----------+-------------+--------------
#21986970   | $2496 | lda #$99       | $99 | $0e | $ff | $ff: $00 | $f8: NVD... | $00 $00 $bd $ad $01 $00 $00 $00
#21986971   | $2498 | sta $0d        | $99 | $0e | $ff | $ff: $00 | $f8: NVD... | $00 $00 $bd $ad $01 $99 $00 $00
#21986972   | $249a | lda $0e        | $00 | $0e | $ff | $ff: $00 | $7a: .VD..Z | $00 $00 $bd $ad $01 $99 $00 $00
#21986973   | $249c | beq $24d7      | $00 | $0e | $ff | $ff: $00 | $7a: .VD..Z | $00 $00 $bd $ad $01 $99 $00 $00
#21986974   | $24d7 | cpx #$0e       | $00 | $0e | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986975   | $24d9 | bne $24d9      | $00 | $0e | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986976   | $24db | cpy #$ff       | $00 | $0e | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986977   | $24dd | bne $24dd      | $00 | $0e | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986978   | $24df | tsx            | $00 | $ff | $ff | $ff: $00 | $f9: NVD.C. | $00 $00 $bd $ad $01 $99 $00 $00
#21986979   | $24e0 | cpx #$ff       | $00 | $ff | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986980   | $24e2 | bne $24e2      | $00 | $ff | $ff | $ff: $00 | $7b: .VD.CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986981   | $24e4 | cld            | $00 | $ff | $ff | $ff: $00 | $73: .V..CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986982   | $24e5 | lda $0202      | $15 | $ff | $ff | $ff: $00 | $71: .V..C. | $00 $00 $bd $ad $01 $99 $00 $00
#21986983   | $24e8 | cmp #$15       | $15 | $ff | $ff | $ff: $00 | $73: .V..CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986984   | $24ea | bne $24ea      | $15 | $ff | $ff | $ff: $00 | $73: .V..CZ | $00 $00 $bd $ad $01 $99 $00 $00
#21986985   | $24ec | lda #$f0       | $f0 | $ff | $ff | $ff: $00 | $f1: NV..C. | $00 $00 $bd $ad $01 $99 $00 $00
#21986986   | $24ee | sta $0202      | $f0 | $ff | $ff | $ff: $00 | $f1: NV..C. | $00 $00 $bd $ad $01 $99 $00 $00

Final instruction:

Step #      | PC    | Instruction    | Acc | InX | InY | SP   Top |   Status    | $0008 - $000f
------------+-------+----------------+-----+-----+-----+----------+-------------+--------------
#21986987   | $24f1 | stp            | $f0 | $ff | $ff | $ff: $00 | $f1: NV..C. | $00 $00 $bd $ad $01 $99 $00 $00

Test results:                "programs\65C02_extended_opcodes_test.hex"

  Instructions executed:     21.986987 M
  Total clock cycles:        66.905005 M

  Elapsed time:              0.5550 sec
  Average clock rate:        120.5496 MHz
  Average instruction rate:  39.6162 MIPS
  Average clocks/instruction 3.0429

Test result:                 PASSED
```
  
