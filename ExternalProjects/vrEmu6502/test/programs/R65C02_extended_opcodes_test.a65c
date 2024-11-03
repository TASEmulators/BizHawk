;
; 6 5 C 0 2   E X T E N D E D   O P C O D E S   T E S T
;
; Copyright (C) 2013-2017  Klaus Dormann
;
; This program is free software: you can redistribute it and/or modify
; it under the terms of the GNU General Public License as published by
; the Free Software Foundation, either version 3 of the License, or
; (at your option) any later version.
;
; This program is distributed in the hope that it will be useful,
; but WITHOUT ANY WARRANTY; without even the implied warranty of
; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
; GNU General Public License for more details.
;
; You should have received a copy of the GNU General Public License
; along with this program.  If not, see <http://www.gnu.org/licenses/>.


; This program is designed to test all additional 65C02 opcodes, addressing
; modes and functionality not available in the NMOS version of the 6502.
; The 6502_functional_test is a prerequisite to this test.
; NMI, IRQ, STP & WAI are covered in the 6502_interrupt_test.
; 
; version 04-dec-2017
; contact info at http://2m5.de or email K@2m5.de
;
; assembled with AS65 from http://www.kingswood-consulting.co.uk/assemblers/
; command line switches: -l -m -s2 -w -x -h0
;                         |  |  |   |  |  no page headers in listing
;                         |  |  |   |  65C02 extensions
;                         |  |  |   wide listing (133 char/col)
;                         |  |  write intel hex file instead of binary
;                         |  expand macros in listing
;                         generate pass2 listing
;
; No IO - should be run from a monitor with access to registers.
; To run load intel hex image with a load command, than alter PC to 400 hex
; (code_segment) and enter a go command.
; Loop on program counter determines error or successful completion of test.
; Check listing for relevant traps (jump/branch *).
; Please note that in early tests some instructions will have to be used before
; they are actually tested!
;
; RESET, NMI or IRQ should not occur and will be trapped if vectors are enabled.
; Tests documented behavior of the original 65C02 only!
; Decimal ops will only be tested with valid BCD operands and the V flag will
; be ignored as it is absolutely useless in decimal mode.
;
; Debugging hints:
;     Most of the code is written sequentially. if you hit a trap, check the
;   immediately preceeding code for the instruction to be tested. Results are
;   tested first, flags are checked second by pushing them onto the stack and
;   pulling them to the accumulator after the result was checked. The "real"
;   flags are no longer valid for the tested instruction at this time!
;     If the tested instruction was indexed, the relevant index (X or Y) must
;   also be checked. Opposed to the flags, X and Y registers are still valid.
;
; versions:
;   19-jul-2013  1st version distributed for testing
;   23-jul-2013  fixed BRA out of range due to larger trap macros
;                added RAM integrity check
;   16-aug-2013  added error report to standard output option
;   23-aug-2015  change revoked
;   24-aug-2015  all self modifying immediate opcodes now execute in data RAM
;   28-aug-2015  fixed decimal adc/sbc immediate only testing carry
;   09-feb-2017  fixed RMB/SMB tested when they shouldn't be tested
;   04-dec-2017  fixed BRK not tested for actually going through the IRQ vector
;                added option to skip the remainder of a failing test
;                in report.i65
;                added skip override to undefined opcode as NOP test


; C O N F I G U R A T I O N

;ROM_vectors writable (0=no, 1=yes)
;if ROM vectors can not be used interrupts will not be trapped
;as a consequence BRK can not be tested but will be emulated to test RTI
ROM_vectors = 1

;load_data_direct (0=move from code segment, 1=load directly)
;loading directly is preferred but may not be supported by your platform
;0 produces only consecutive object code, 1 is not suitable for a binary image
load_data_direct = 1

;I_flag behavior (0=force enabled, 1=force disabled, 2=prohibit change, 3=allow
;change) 2 requires extra code and is not recommended.
I_flag = 3

;configure memory - try to stay away from memory used by the system
;zero_page memory start address, $4e (78) consecutive Bytes required
;                                add 2 if I_flag = 2
zero_page = $a  

;data_segment memory start address, $63 (99) consecutive Bytes required
; + 12 Bytes at data_segment + $f9 (JMP indirect page cross test)
data_segment = $200
    if (data_segment & $ff) != 0
        ERROR ERROR ERROR low byte of data_segment MUST be $00 !!
    endif  

;code_segment memory start address, 10kB of consecutive space required
;                                   add 1 kB if I_flag = 2
code_segment = $400  

;added WDC only opcodes WAI & STP (0=test as NOPs, >0=no test)
wdc_op = 1

;added Rockwell & WDC opcodes BBR, BBS, RMB & SMB
;(0=test as NOPs, 1=full test, >1=no test) 
rkwl_wdc_op = 1

;skip testing all undefined opcodes override
;0=test as NOP, >0=skip
skip_nop = 0

;report errors through I/O channel (0=use standard self trap loops, 1=include
;report.i65 as I/O channel, add 3 kB)
report = 0

;RAM integrity test option. Checks for undesired RAM writes.
;set lowest non RAM or RAM mirror address page (-1=disable, 0=64k, $40=16k)
;leave disabled if a monitor, OS or background interrupt is allowed to alter RAM
ram_top = -1

        noopt       ;do not take shortcuts
        
;macros for error & success traps to allow user modification
;example:
;trap    macro
;        jsr my_error_handler
;        endm
;trap_eq macro
;        bne skip\?
;        trap           ;failed equal (zero)
;skip\?
;        endm
;
; my_error_handler should pop the calling address from the stack and report it.
; putting larger portions of code (more than 3 bytes) inside the trap macro
; may lead to branch range problems for some tests.
    if report = 0
trap    macro
        jmp *           ;failed anyway
        endm
trap_eq macro
        beq *           ;failed equal (zero)
        endm
trap_ne macro
        bne *           ;failed not equal (non zero)
        endm
trap_cs macro
        bcs *           ;failed carry set
        endm
trap_cc macro
        bcc *           ;failed carry clear
        endm
trap_mi macro
        bmi *           ;failed minus (bit 7 set)
        endm
trap_pl macro
        bpl *           ;failed plus (bit 7 clear)
        endm
trap_vs macro
        bvs *           ;failed overflow set
        endm
trap_vc macro
        bvc *           ;failed overflow clear
        endm
; please observe that during the test the stack gets invalidated
; therefore a RTS inside the success macro is not possible
success macro
        jmp *           ;test passed, no errors
        endm
    endif
    if report = 1
trap    macro
        jsr report_error
        endm
trap_eq macro
        bne skip\?
        trap           ;failed equal (zero)
skip\?
        endm
trap_ne macro
        beq skip\?
        trap            ;failed not equal (non zero)
skip\?
        endm
trap_cs macro
        bcc skip\?
        trap            ;failed carry set
skip\?
        endm
trap_cc macro
        bcs skip\?
        trap            ;failed carry clear
skip\?
        endm
trap_mi macro
        bpl skip\?
        trap            ;failed minus (bit 7 set)
skip\?
        endm
trap_pl macro
        bmi skip\?
        trap            ;failed plus (bit 7 clear)
skip\?
        endm
trap_vs macro
        bvc skip\?
        trap            ;failed overflow set
skip\?
        endm
trap_vc macro
        bvs skip\?
        trap            ;failed overflow clear
skip\?
        endm
; please observe that during the test the stack gets invalidated
; therefore a RTS inside the success macro is not possible
success macro
        jsr report_success
        endm
    endif


carry   equ %00000001   ;flag bits in status
zero    equ %00000010
intdis  equ %00000100
decmode equ %00001000
break   equ %00010000
reserv  equ %00100000
overfl  equ %01000000
minus   equ %10000000

fc      equ carry
fz      equ zero
fzc     equ carry+zero
fv      equ overfl
fvz     equ overfl+zero
fn      equ minus
fnc     equ minus+carry
fnz     equ minus+zero
fnzc    equ minus+zero+carry
fnv     equ minus+overfl

fao     equ break+reserv    ;bits always on after PHP, BRK
fai     equ fao+intdis      ;+ forced interrupt disable
m8      equ $ff             ;8 bit mask
m8i     equ $ff&~intdis     ;8 bit mask - interrupt disable

;macros to allow masking of status bits.
;masking of interrupt enable/disable on load and compare
;masking of always on bits after PHP or BRK (unused & break) on compare
        if I_flag = 0
load_flag   macro
            lda #\1&m8i         ;force enable interrupts (mask I)
            endm
cmp_flag    macro
            cmp #(\1|fao)&m8i   ;I_flag is always enabled + always on bits
            endm
eor_flag    macro
            eor #(\1&m8i|fao)   ;mask I, invert expected flags + always on bits
            endm
        endif
        if I_flag = 1
load_flag   macro
            lda #\1|intdis      ;force disable interrupts
            endm
cmp_flag    macro
            cmp #(\1|fai)&m8    ;I_flag is always disabled + always on bits
            endm
eor_flag    macro
            eor #(\1|fai)       ;invert expected flags + always on bits + I
            endm
        endif
        if I_flag = 2
load_flag   macro
            lda #\1
            ora flag_I_on       ;restore I-flag
            and flag_I_off
            endm
cmp_flag    macro
            eor flag_I_on       ;I_flag is never changed
            cmp #(\1|fao)&m8i   ;expected flags + always on bits, mask I
            endm
eor_flag    macro
            eor flag_I_on       ;I_flag is never changed
            eor #(\1&m8i|fao)   ;mask I, invert expected flags + always on bits
            endm
        endif
        if I_flag = 3
load_flag   macro
            lda #\1             ;allow test to change I-flag (no mask)
            endm
cmp_flag    macro
            cmp #(\1|fao)&m8    ;expected flags + always on bits
            endm
eor_flag    macro
            eor #\1|fao         ;invert expected flags + always on bits
            endm
        endif

;macros to set (register|memory|zeropage) & status
set_stat    macro       ;setting flags in the processor status register
            load_flag \1
            pha         ;use stack to load status
            plp
            endm

set_a       macro       ;precharging accu & status
            load_flag \2
            pha         ;use stack to load status
            lda #\1     ;precharge accu
            plp
            endm

set_x       macro       ;precharging index & status
            load_flag \2
            pha         ;use stack to load status
            ldx #\1     ;precharge index x
            plp
            endm

set_y       macro       ;precharging index & status
            load_flag \2
            pha         ;use stack to load status
            ldy #\1     ;precharge index y
            plp
            endm

set_ax      macro       ;precharging indexed accu & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,x    ;precharge accu
            plp
            endm

set_ay      macro       ;precharging indexed accu & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,y    ;precharge accu
            plp
            endm

set_z       macro       ;precharging indexed zp & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,x    ;load to zeropage
            sta zpt
            plp
            endm

set_zx      macro       ;precharging zp,x & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,x    ;load to indexed zeropage
            sta zpt,x
            plp
            endm

set_abs     macro       ;precharging indexed memory & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,x    ;load to memory
            sta abst
            plp
            endm

set_absx    macro       ;precharging abs,x & immediate status
            load_flag \2
            pha         ;use stack to load status
            lda \1,x    ;load to indexed memory
            sta abst,x
            plp
            endm

;macros to test (register|memory|zeropage) & status & (mask)
tst_stat    macro       ;testing flags in the processor status register
            php         ;save status
            pla         ;use stack to retrieve status
            pha
            cmp_flag \1
            trap_ne
            plp         ;restore status
            endm
            
tst_a       macro       ;testing result in accu & flags
            php         ;save flags
            cmp #\1     ;test result
            trap_ne
            pla         ;load status
            pha
            cmp_flag \2
            trap_ne
            plp         ;restore status
            endm

tst_as      macro       ;testing result in accu & flags, save accu
            pha
            php         ;save flags
            cmp #\1     ;test result
            trap_ne
            pla         ;load status
            pha
            cmp_flag \2
            trap_ne
            plp         ;restore status
            pla
            endm

tst_x       macro       ;testing result in x index & flags
            php         ;save flags
            cpx #\1     ;test result
            trap_ne
            pla         ;load status
            pha
            cmp_flag \2
            trap_ne
            plp         ;restore status
            endm

tst_y       macro       ;testing result in y index & flags
            php         ;save flags
            cpy #\1     ;test result
            trap_ne
            pla         ;load status
            pha
            cmp_flag \2
            trap_ne
            plp         ;restore status
            endm

tst_ax      macro       ;indexed testing result in accu & flags
            php         ;save flags
            cmp \1,x    ;test result
            trap_ne
            pla         ;load status
            eor_flag \3
            cmp \2,x    ;test flags
            trap_ne     ;
            endm

tst_ay      macro       ;indexed testing result in accu & flags
            php         ;save flags
            cmp \1,y    ;test result
            trap_ne     ;
            pla         ;load status
            eor_flag \3
            cmp \2,y    ;test flags
            trap_ne
            endm
        
tst_z       macro       ;indexed testing result in zp & flags
            php         ;save flags
            lda zpt
            cmp \1,x    ;test result
            trap_ne
            pla         ;load status
            eor_flag \3
            cmp \2,x    ;test flags
            trap_ne
            endm

tst_zx      macro       ;testing result in zp,x & flags
            php         ;save flags
            lda zpt,x
            cmp \1,x    ;test result
            trap_ne
            pla         ;load status
            eor_flag \3
            cmp \2,x    ;test flags
            trap_ne
            endm

tst_abs     macro       ;indexed testing result in memory & flags
            php         ;save flags
            lda abst
            cmp \1,x    ;test result
            trap_ne
            pla         ;load status
            eor_flag \3
            cmp \2,x    ;test flags
            trap_ne
            endm

tst_absx    macro       ;testing result in abs,x & flags
            php         ;save flags
            lda abst,x
            cmp \1,x    ;test result
            trap_ne
            pla         ;load status
            eor_flag \3
            cmp \2,x    ;test flags
            trap_ne
            endm
            
; RAM integrity test
;   verifies that none of the previous tests has altered RAM outside of the
;   designated write areas.
;   uses zpt word as indirect pointer, zpt+2 word as checksum
        if ram_top > -1
check_ram   macro 
            cld
            lda #0
            sta zpt         ;set low byte of indirect pointer
            sta zpt+3       ;checksum high byte
            ldx #11         ;reset modifiable RAM
ccs1\?      sta jxi_tab,x   ;JMP indirect page cross area
            dex
            bpl ccs1\?
            clc
            ldx #zp_bss-zero_page ;zeropage - write test area
ccs3\?      adc zero_page,x
            bcc ccs2\?
            inc zpt+3       ;carry to high byte
            clc
ccs2\?      inx
            bne ccs3\?
            ldx #hi(abs1)   ;set high byte of indirect pointer
            stx zpt+1
            ldy #lo(abs1)   ;data after write & execute test area
ccs5\?      adc (zpt),y
            bcc ccs4\?
            inc zpt+3       ;carry to high byte
            clc
ccs4\?      iny
            bne ccs5\?
            inx             ;advance RAM high address
            stx zpt+1
            cpx #ram_top
            bne ccs5\?
            sta zpt+2       ;checksum low is
            cmp ram_chksm   ;checksum low expected
            trap_ne         ;checksum mismatch
            lda zpt+3       ;checksum high is
            cmp ram_chksm+1 ;checksum high expected
            trap_ne         ;checksum mismatch
            endm            
        else
check_ram   macro
            ;RAM check disabled - RAM size not set
            endm
        endif
            
next_test   macro           ;make sure, tests don't jump the fence
            lda test_case   ;previous test
            cmp #test_num
            trap_ne         ;test is out of sequence
test_num = test_num + 1
            lda #test_num   ;*** next tests' number
            sta test_case
            ;check_ram       ;uncomment to find altered RAM after each test
            endm

    if load_data_direct = 1
        data
    else
        bss                 ;uninitialized segment, copy of data at end of code!
    endif
        org zero_page
;break test interrupt save
irq_a   ds  1               ;a register
irq_x   ds  1               ;x register
    if I_flag = 2
;masking for I bit in status
flag_I_on   ds  1           ;or mask to load flags   
flag_I_off  ds  1           ;and mask to load flags
    endif
zpt                         ;5 bytes store/modify test area
;add/subtract operand generation and result/flag prediction
adfc    ds  1               ;carry flag before op
ad1     ds  1               ;operand 1 - accumulator
ad2     ds  1               ;operand 2 - memory / immediate
adrl    ds  1               ;expected result bits 0-7
adrh    ds  1               ;expected result bit 8 (carry)
adrf    ds  1               ;expected flags NV0000ZC (-V in decimal mode)
sb2     ds  1               ;operand 2 complemented for subtract
zp_bss
zp1     db  $c3,$82,$41,0   ;test patterns for LDx BIT ROL ROR ASL LSR
zp7f    db  $7f             ;test pattern for compare  
;logical zeropage operands
zpOR    db  0,$1f,$71,$80   ;test pattern for OR
zpAN    db  $0f,$ff,$7f,$80 ;test pattern for AND
zpEO    db  $ff,$0f,$8f,$8f ;test pattern for EOR
;indirect addressing pointers
ind1    dw  abs1            ;indirect pointer to pattern in absolute memory
        dw  abs1+1
        dw  abs1+2
        dw  abs1+3
        dw  abs7f
inw1    dw  abs1-$f8        ;indirect pointer for wrap-test pattern
indt    dw  abst            ;indirect pointer to store area in absolute memory
        dw  abst+1
        dw  abst+2
        dw  abst+3
inwt    dw  abst-$f8        ;indirect pointer for wrap-test store
indAN   dw  absAN           ;indirect pointer to AND pattern in absolute memory
        dw  absAN+1
        dw  absAN+2
        dw  absAN+3
indEO   dw  absEO           ;indirect pointer to EOR pattern in absolute memory
        dw  absEO+1
        dw  absEO+2
        dw  absEO+3
indOR   dw  absOR           ;indirect pointer to OR pattern in absolute memory
        dw  absOR+1
        dw  absOR+2
        dw  absOR+3
;add/subtract indirect pointers
adi2    dw  ada2            ;indirect pointer to operand 2 in absolute memory
sbi2    dw  sba2            ;indirect pointer to complemented operand 2 (SBC)
adiy2   dw  ada2-$ff        ;with offset for indirect indexed
sbiy2   dw  sba2-$ff
zp_bss_end
    
        org data_segment
pg_x    ds  2               ;high JMP indirect address for page cross bug
test_case   ds  1           ;current test number
ram_chksm   ds  2           ;checksum for RAM integrity test
;add/subtract operand copy - abs tests write area
abst                        ;5 bytes store/modify test area
ada2    ds  1               ;operand 2
sba2    ds  1               ;operand 2 complemented for subtract
        ds  3               ;fill remaining bytes
data_bss
    if load_data_direct = 1
ex_adci adc #0              ;execute immediate opcodes
        rts
ex_sbci sbc #0              ;execute immediate opcodes
        rts
    else
ex_adci ds  3
ex_sbci ds  3
    endif
abs1    db  $c3,$82,$41,0   ;test patterns for LDx BIT ROL ROR ASL LSR
abs7f   db  $7f             ;test pattern for compare
;loads
fLDx    db  fn,fn,0,fz      ;expected flags for load
;shifts
rASL                        ;expected result ASL & ROL -carry  
rROL    db  $86,$04,$82,0   ; "
rROLc   db  $87,$05,$83,1   ;expected result ROL +carry
rLSR                        ;expected result LSR & ROR -carry
rROR    db  $61,$41,$20,0   ; "
rRORc   db  $e1,$c1,$a0,$80 ;expected result ROR +carry
fASL                        ;expected flags for shifts
fROL    db  fnc,fc,fn,fz    ;no carry in
fROLc   db  fnc,fc,fn,0     ;carry in
fLSR
fROR    db  fc,0,fc,fz      ;no carry in
fRORc   db  fnc,fn,fnc,fn   ;carry in
;increments (decrements)
rINC    db  $7f,$80,$ff,0,1 ;expected result for INC/DEC
fINC    db  0,fn,fn,fz,0    ;expected flags for INC/DEC
;logical memory operand
absOR   db  0,$1f,$71,$80   ;test pattern for OR
absAN   db  $0f,$ff,$7f,$80 ;test pattern for AND
absEO   db  $ff,$0f,$8f,$8f ;test pattern for EOR
;logical accu operand
absORa  db  0,$f1,$1f,0     ;test pattern for OR
absANa  db  $f0,$ff,$ff,$ff ;test pattern for AND
absEOa  db  $ff,$f0,$f0,$0f ;test pattern for EOR
;logical results
absrlo  db  0,$ff,$7f,$80
absflo  db  fz,fn,0,fn
data_bss_end
;define area for page crossing JMP (abs) & JMP (abs,x) test
jxi_tab equ data_segment + $100 - 7     ;JMP (jxi_tab,x) x=6
ji_tab  equ data_segment + $100 - 3     ;JMP (ji_tab+2)
jxp_tab equ data_segment + $100         ;JMP (jxp_tab-255) x=255


        code
        org code_segment
start   cld
        ldx #$ff
        txs
        lda #0          ;*** test 0 = initialize
        sta test_case
test_num = 0

;stop interrupts before initializing BSS
    if I_flag = 1
        sei
    endif
    
;initialize I/O for report channel
    if report = 1
        jsr report_init
    endif
    
;initialize BSS segment
    if load_data_direct != 1
        ldx #zp_end-zp_init-1
ld_zp   lda zp_init,x
        sta zp_bss,x
        dex
        bpl ld_zp
        ldx #data_end-data_init-1
ld_data lda data_init,x
        sta data_bss,x
        dex
        bpl ld_data
      if ROM_vectors = 1
        ldx #5
ld_vect lda vec_init,x
        sta vec_bss,x
        dex
        bpl ld_vect
      endif
    endif

;retain status of interrupt flag
    if I_flag = 2
        php
        pla
        and #4          ;isolate flag
        sta flag_I_on   ;or mask
        eor #lo(~4)     ;reverse
        sta flag_I_off  ;and mask
    endif
        
;generate checksum for RAM integrity test
    if ram_top > -1
        lda #0 
        sta zpt         ;set low byte of indirect pointer
        sta ram_chksm+1 ;checksum high byte
        ldx #11         ;reset modifiable RAM
gcs1    sta jxi_tab,x   ;JMP indirect page cross area
        dex
        bpl gcs1
        clc
        ldx #zp_bss-zero_page ;zeropage - write test area
gcs3    adc zero_page,x
        bcc gcs2
        inc ram_chksm+1 ;carry to high byte
        clc
gcs2    inx
        bne gcs3
        ldx #hi(abs1)   ;set high byte of indirect pointer
        stx zpt+1
        ldy #lo(abs1)   ;data after write & execute test area
gcs5    adc (zpt),y
        bcc gcs4
        inc ram_chksm+1 ;carry to high byte
        clc
gcs4    iny
        bne gcs5
        inx             ;advance RAM high address
        stx zpt+1
        cpx #ram_top
        bne gcs5
        sta ram_chksm   ;checksum complete
    endif
        next_test            

;testing stack operations PHX PHY PLX PLY
        lda #$99        ;protect a
        ldx #$ff        ;initialize stack
        txs
        ldx #$55
        phx
        ldx #$aa
        phx
        cpx $1fe        ;on stack ?
        trap_ne
        tsx
        cpx #$fd        ;sp decremented?
        trap_ne
        ply
        cpy #$aa        ;successful retreived from stack?
        trap_ne
        ply
        cpy #$55
        trap_ne
        cpy $1ff        ;remains on stack?
        trap_ne
        tsx
        cpx #$ff        ;sp incremented?
        trap_ne
        
        ldy #$a5
        phy
        ldy #$5a
        phy
        cpy $1fe        ;on stack ?
        trap_ne
        tsx
        cpx #$fd        ;sp decremented?
        trap_ne
        plx
        cpx #$5a        ;successful retreived from stack?
        trap_ne
        plx
        cpx #$a5
        trap_ne
        cpx $1ff        ;remains on stack?
        trap_ne
        tsx
        cpx #$ff        ;sp incremented?
        trap_ne
        cmp #$99        ;unchanged?
        trap_ne
        next_test            
        
; test PHX does not alter flags or X but PLX does
        ldy #$aa        ;protect y
        set_x 1,$ff     ;push
        phx
        tst_x 1,$ff
        set_x 0,0
        phx
        tst_x 0,0
        set_x $ff,$ff
        phx
        tst_x $ff,$ff
        set_x 1,0
        phx
        tst_x 1,0
        set_x 0,$ff
        phx
        tst_x 0,$ff
        set_x $ff,0
        phx
        tst_x $ff,0
        set_x 0,$ff     ;pull
        plx
        tst_x $ff,$ff-zero
        set_x $ff,0
        plx
        tst_x 0,zero
        set_x $fe,$ff
        plx
        tst_x 1,$ff-zero-minus
        set_x 0,0
        plx
        tst_x $ff,minus
        set_x $ff,$ff
        plx
        tst_x 0,$ff-minus
        set_x $fe,0
        plx
        tst_x 1,0
        cpy #$aa        ;Y unchanged
        trap_ne
        next_test            
 
; test PHY does not alter flags or Y but PLY does
        ldx #$55        ;x & a protected
        set_y 1,$ff     ;push
        phy
        tst_y 1,$ff
        set_y 0,0
        phy
        tst_y 0,0
        set_y $ff,$ff
        phy
        tst_y $ff,$ff
        set_y 1,0
        phy
        tst_y 1,0
        set_y 0,$ff
        phy
        tst_y 0,$ff
        set_y $ff,0
        phy
        tst_y $ff,0
        set_y 0,$ff     ;pull
        ply
        tst_y $ff,$ff-zero
        set_y $ff,0
        ply
        tst_y 0,zero
        set_y $fe,$ff
        ply
        tst_y 1,$ff-zero-minus
        set_y 0,0
        ply
        tst_y $ff,minus
        set_y $ff,$ff
        ply
        tst_y 0,$ff-minus
        set_y $fe,0
        ply
        tst_y 1,0
        cpx #$55        ;x unchanged?
        trap_ne
        next_test            
 
; PC modifying instructions (BRA, BBR, BBS, 1, 2, 3 byte NOPs, JMP(abs,x))
; testing unconditional branch BRA

        ldx #$81        ;protect unused registers
        ldy #$7e
        set_a 0,$ff
        bra br1         ;branch should always be taken
        trap 
br1
        tst_a 0,$ff
        set_a $ff,0
        bra br2         ;branch should always be taken
        trap 
br2
        tst_a $ff,0
        cpx #$81
        trap_ne
        cpy #$7e
        trap_ne
        next_test            
        
        ldy #0          ;branch range test  
        bra bra0
        
bra1    cpy #1
        trap_ne         ;long range backward
        iny        
        bra bra2
                
bra3    cpy #3
        trap_ne         ;long range backward
        iny        
        bra bra4
                
bra5    cpy #5
        trap_ne         ;long range backward
        iny        
        ldy #0
        bra brf0
        
        iny
        iny
        iny
        iny        
brf0    bra brf1

        iny
        iny
        iny
brf1    iny        
        bra brf2
        
        iny
        iny
brf2    iny
        iny        
        bra brf3
        
        iny
brf3    iny
        iny
        iny        
        bra brf4
        
brf4    iny
        iny
        iny
        iny
        cpy #10
        trap_ne     ;short range forward
        bra brb0

brb4    dey
        dey
        dey
        dey
        bra brb5        

brb3    dey
        dey
        dey
        bra brb4        

brb2    dey
        dey
        bra brb3        

brb1    dey
        bra brb2        

brb0    bra brb1        

brb5    cpy #0
        trap_ne     ;short range backward
        bra bra6

bra4    cpy #4
        trap_ne     ;long range forward
        iny        
        bra bra5
                
bra2    cpy #2
        trap_ne     ;long range forward
        iny        
        bra bra3
                
bra0    cpy #0
        trap_ne     ;long range forward
        iny        
        bra bra1
                
bra6
        next_test
        
    if rkwl_wdc_op = 1
; testing BBR & BBS

bbt     macro           ;\1 = bitnum
        lda #(1<<\1)    ;testing 1 bit on
        sta zpt
        set_a $33,0     ;with flags off
        bbr \1,zpt,fail1\?
        bbs \1,zpt,ok1\?
        trap            ;bbs branch not taken
fail1\?
        trap            ;bbr branch taken
ok1\?   
        tst_a $33,0
        set_a $cc,$ff   ;with flags on
        bbr \1,zpt,fail2\?
        bbs \1,zpt,ok2\?
        trap            ;bbs branch not taken
fail2\? 
        trap            ;bbr branch taken
ok2\?   
        tst_a $cc,$ff
        lda zpt
        cmp #(1<<\1)
        trap_ne         ;zp altered
        lda #$ff-(1<<\1) ;testing 1 bit off
        sta zpt
        set_a $33,0     ;with flags off
        bbs \1,zpt,fail3\?
        bbr \1,zpt,ok3\?
        trap            ;bbr branch not taken
fail3\? 
        trap            ;bbs branch taken
ok3\?   
        tst_a $33,0
        set_a $cc,$ff   ;with flags on
        bbs \1,zpt,fail4\?
        bbr \1,zpt,ok4\?
        trap            ;bbr branch not taken
fail4\? 
        trap            ;bbs branch taken
ok4\?   
        tst_a $cc,$ff
        lda zpt
        cmp #$ff-(1<<\1)
        trap_ne         ;zp altered
        endm

        ldx #$11        ;test bbr/bbs integrity
        ldy #$22
        bbt 0
        bbt 1
        bbt 2
        bbt 3
        bbt 4
        bbt 5
        bbt 6
        bbt 7
        cpx #$11
        trap_ne         ;x overwritten
        cpy #$22
        trap_ne         ;y overwritten
        next_test 

bbrc    macro           ;\1 = bitnum
        bbr \1,zpt,skip\?
        eor #(1<<\1)       
skip\?
        endm
bbsc    macro           ;\1 = bitnum
        bbs \1,zpt,skip\?
        eor #(1<<\1)       
skip\?
        endm

        lda #0          ;combined bit test
        sta zpt
bbcl    lda #0
        bbrc 0
        bbrc 1
        bbrc 2
        bbrc 3
        bbrc 4
        bbrc 5
        bbrc 6
        bbrc 7
        eor zpt
        trap_ne         ;failed bbr bitnum in accu       
        lda #$ff
        bbsc 0
        bbsc 1
        bbsc 2
        bbsc 3
        bbsc 4
        bbsc 5
        bbsc 6
        bbsc 7
        eor zpt
        trap_ne         ;failed bbs bitnum in accu       
        inc zpt
        bne bbcl
        next_test            
    endif
    
; testing NOP

nop_test    macro       ;\1 = opcode, \2 = # of bytes
            ldy #$42
            ldx #4-\2
            db  \1          ;test nop length
        if \2 = 1
            dex
            dex
        endif
        if \2 = 2
            iny
            dex
        endif
        if \2 = 3
            iny
            iny
        endif
            dex
            trap_ne         ;wrong number of bytes
            set_a $ff-\1,0
            db  \1          ;test nop integrity - flags off
            nop
            nop
            tst_a $ff-\1,0
            set_a $aa-\1,$ff
            db  \1          ;test nop integrity - flags on
            nop
            nop
            tst_a $aa-\1,$ff
            cpy #$42
            trap_ne         ;y changed
            cpx #0
            trap_ne         ;x changed
            endm

    if skip_nop = 0
        nop_test $02,2
        nop_test $22,2
        nop_test $42,2
        nop_test $62,2
        nop_test $82,2
        nop_test $c2,2
        nop_test $e2,2
        nop_test $44,2
        nop_test $54,2
        nop_test $d4,2
        nop_test $f4,2
        nop_test $5c,3
        nop_test $dc,3
        nop_test $fc,3
        nop_test $03,1
        nop_test $13,1
        nop_test $23,1
        nop_test $33,1
        nop_test $43,1
        nop_test $53,1
        nop_test $63,1
        nop_test $73,1
        nop_test $83,1
        nop_test $93,1
        nop_test $a3,1
        nop_test $b3,1
        nop_test $c3,1
        nop_test $d3,1
        nop_test $e3,1
        nop_test $f3,1
        nop_test $0b,1
        nop_test $1b,1
        nop_test $2b,1
        nop_test $3b,1
        nop_test $4b,1
        nop_test $5b,1
        nop_test $6b,1
        nop_test $7b,1
        nop_test $8b,1
        nop_test $9b,1
        nop_test $ab,1
        nop_test $bb,1
        nop_test $eb,1
        nop_test $fb,1
    if rkwl_wdc_op = 0      ;NOPs not available on Rockwell & WDC 65C02
        nop_test $07,1
        nop_test $17,1
        nop_test $27,1
        nop_test $37,1
        nop_test $47,1
        nop_test $57,1
        nop_test $67,1
        nop_test $77,1
        nop_test $87,1
        nop_test $97,1
        nop_test $a7,1
        nop_test $b7,1
        nop_test $c7,1
        nop_test $d7,1
        nop_test $e7,1
        nop_test $f7,1
        nop_test $0f,1
        nop_test $1f,1
        nop_test $2f,1
        nop_test $3f,1
        nop_test $4f,1
        nop_test $5f,1
        nop_test $6f,1
        nop_test $7f,1
        nop_test $8f,1
        nop_test $9f,1
        nop_test $af,1
        nop_test $bf,1
        nop_test $cf,1
        nop_test $df,1
        nop_test $ef,1
        nop_test $ff,1
    endif
    if  wdc_op = 0          ;NOPs not available on WDC 65C02 (WAI, STP)
        nop_test $cb,1
        nop_test $db,1
    endif
        next_test
    endif
            
; jump indirect (test page cross bug is fixed)
        ldx #3          ;prepare table
ji1     lda ji_adr,x
        sta ji_tab,x
        dex
        bpl ji1
        lda #hi(ji_px) ;high address if page cross bug
        sta pg_x
        set_stat 0
        lda #'I'
        ldx #'N'
        ldy #'D'        ;N=0, V=0, Z=0, C=0
        jmp (ji_tab)
        nop
        trap_ne         ;runover protection

        dey
        dey
ji_ret  php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        plp
        trap_eq         ;returned flags OK?
        trap_pl
        trap_cc
        trap_vc
        cmp #('I'^$aa)  ;returned registers OK?
        trap_ne
        cpx #('N'+1)
        trap_ne
        cpy #('D'-6)
        trap_ne
        tsx             ;SP check
        cpx #$ff
        trap_ne
        next_test

; jump indexed indirect
        ldx #11         ;prepare table
jxi1    lda jxi_adr,x
        sta jxi_tab,x
        dex
        bpl jxi1
        lda #hi(jxi_px) ;high address if page cross bug
        sta pg_x
        set_stat 0
        lda #'X'
        ldx #4
        ldy #'I'        ;N=0, V=0, Z=0, C=0
        jmp (jxi_tab,x)
        nop
        trap_ne         ;runover protection

        dey
        dey
jxi_ret php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        plp
        trap_eq         ;returned flags OK?
        trap_pl
        trap_cc
        trap_vc
        cmp #('X'^$aa)  ;returned registers OK?
        trap_ne
        cpx #6
        trap_ne
        cpy #('I'-6)
        trap_ne
        tsx             ;SP check
        cpx #$ff
        trap_ne

        lda #lo(jxp_ok) ;test with index causing a page cross
        sta jxp_tab
        lda #hi(jxp_ok)
        sta jxp_tab+1
        lda #lo(jxp_px)
        sta pg_x
        lda #hi(jxp_px)
        sta pg_x+1
        ldx #$ff
        jmp (jxp_tab-$ff,x)
        
jxp_px  
        trap            ;page cross by index to wrong page

jxp_ok
        next_test

    if ROM_vectors = 1
; test BRK clears decimal mode
        load_flag 0     ;with interrupts enabled if allowed!
        pha
        lda #'B'
        ldx #'R'
        ldy #'K'
        plp             ;N=0, V=0, Z=0, C=0
        brk
        dey             ;should not be executed
brk_ret0                ;address of break return
        php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        cmp #'B'^$aa    ;returned registers OK?
        ;the IRQ vector was never executed if A & X stay unmodified
        trap_ne
        cpx #'R'+1
        trap_ne
        cpy #'K'-6
        trap_ne
        pla             ;returned flags OK (unchanged)?
        cmp_flag 0
        trap_ne
        tsx             ;sp?
        cpx #$ff
        trap_ne
;pass 2
        load_flag $ff   ;with interrupts disabled if allowed!
        pha
        lda #$ff-'B'
        ldx #$ff-'R'
        ldy #$ff-'K'
        plp             ;N=1, V=1, Z=1, C=1
        brk
        dey             ;should not be executed
brk_ret1                ;address of break return
        php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        cmp #($ff-'B')^$aa  ;returned registers OK?
        ;the IRQ vector was never executed if A & X stay unmodified
        trap_ne
        cpx #$ff-'R'+1
        trap_ne
        cpy #$ff-'K'-6
        trap_ne
        pla             ;returned flags OK (unchanged)?
        cmp_flag $ff
        trap_ne
        tsx             ;sp?
        cpx #$ff
        trap_ne
        next_test
    endif
 
; testing accumulator increment/decrement INC A & DEC A
        ldx #$ac    ;protect x & y
        ldy #$dc
        set_a $fe,$ff
        inc a           ;ff
        tst_as $ff,$ff-zero
        inc a           ;00
        tst_as 0,$ff-minus
        inc a           ;01
        tst_as 1,$ff-minus-zero
        dec a           ;00
        tst_as 0,$ff-minus
        dec a           ;ff
        tst_as $ff,$ff-zero
        dec a           ;fe
        set_a $fe,0
        inc a           ;ff
        tst_as $ff,minus
        inc a           ;00
        tst_as 0,zero
        inc a           ;01
        tst_as 1,0
        dec a           ;00
        tst_as 0,zero
        dec a           ;ff
        tst_as $ff,minus
        cpx #$ac
        trap_ne     ;x altered during test
        cpy #$dc
        trap_ne     ;y altered during test
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test

; testing load / store accumulator LDA / STA (zp)
        ldx #$99    ;protect x & y
        ldy #$66
        set_stat 0  
        lda (ind1)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$c3    ;test result
        trap_ne
        pla         ;load status
        eor_flag 0
        cmp fLDx    ;test flags
        trap_ne
        set_stat 0
        lda (ind1+2)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+2)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$82    ;test result
        trap_ne
        pla         ;load status
        eor_flag 0
        cmp fLDx+1  ;test flags
        trap_ne
        set_stat 0
        lda (ind1+4)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+4)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$41    ;test result
        trap_ne
        pla         ;load status
        eor_flag 0
        cmp fLDx+2  ;test flags
        trap_ne
        set_stat 0
        lda (ind1+6)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+6)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #0      ;test result
        trap_ne
        pla         ;load status
        eor_flag 0
        cmp fLDx+3  ;test flags
        trap_ne
        cpx #$99
        trap_ne     ;x altered during test
        cpy #$66
        trap_ne     ;y altered during test

        ldy #3      ;testing store result
        ldx #0
tstai1  lda abst,y
        eor #$c3
        cmp abs1,y
        trap_ne     ;store to indirect data
        txa
        sta abst,y  ;clear                
        dey
        bpl tstai1

        ldx #$99    ;protect x & y
        ldy #$66
        set_stat $ff  
        lda (ind1)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$c3    ;test result
        trap_ne
        pla         ;load status
        eor_flag lo~fnz ;mask bits not altered
        cmp fLDx    ;test flags
        trap_ne
        set_stat $ff
        lda (ind1+2)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+2)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$82    ;test result
        trap_ne
        pla         ;load status
        eor_flag lo~fnz ;mask bits not altered
        cmp fLDx+1  ;test flags
        trap_ne
        set_stat $ff
        lda (ind1+4)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+4)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #$41    ;test result
        trap_ne
        pla         ;load status
        eor_flag lo~fnz ;mask bits not altered
        cmp fLDx+2  ;test flags
        trap_ne
        set_stat $ff
        lda (ind1+6)
        php         ;test stores do not alter flags
        eor #$c3
        plp
        sta (indt+6)
        php         ;flags after load/store sequence
        eor #$c3
        cmp #0      ;test result
        trap_ne
        pla         ;load status
        eor_flag lo~fnz ;mask bits not altered
        cmp fLDx+3  ;test flags
        trap_ne
        cpx #$99
        trap_ne     ;x altered during test
        cpy #$66
        trap_ne     ;y altered during test

        ldy #3      ;testing store result
        ldx #0
tstai2  lda abst,y
        eor #$c3
        cmp abs1,y
        trap_ne     ;store to indirect data
        txa
        sta abst,y  ;clear                
        dey
        bpl tstai2
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test

; testing STZ - zp / abs / zp,x / abs,x
        ldy #123    ;protect y
        ldx #4      ;precharge test area
        lda #7
tstz1   sta zpt,x
        asl a
        dex
        bpl tstz1
        ldx #4
        set_a $55,$ff
        stz zpt     
        stz zpt+1
        stz zpt+2
        stz zpt+3
        stz zpt+4
        tst_a $55,$ff
tstz2   lda zpt,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz2
        ldx #4      ;precharge test area
        lda #7
tstz3   sta zpt,x
        asl a
        dex
        bpl tstz3
        ldx #4
        set_a $aa,0
        stz zpt     
        stz zpt+1
        stz zpt+2
        stz zpt+3
        stz zpt+4
        tst_a $aa,0
tstz4   lda zpt,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz4
        
        ldx #4      ;precharge test area
        lda #7
tstz5   sta abst,x
        asl a
        dex
        bpl tstz5
        ldx #4
        set_a $55,$ff
        stz abst     
        stz abst+1
        stz abst+2
        stz abst+3
        stz abst+4
        tst_a $55,$ff
tstz6   lda abst,x   ;verify zeros stored
        trap_ne     ;non zero after STZ abs
        dex
        bpl tstz6
        ldx #4      ;precharge test area
        lda #7
tstz7   sta abst,x
        asl a
        dex
        bpl tstz7
        ldx #4
        set_a $aa,0
        stz abst     
        stz abst+1
        stz abst+2
        stz abst+3
        stz abst+4
        tst_a $aa,0
tstz8   lda abst,x   ;verify zeros stored
        trap_ne     ;non zero after STZ abs
        dex
        bpl tstz8
        
        ldx #4      ;precharge test area
        lda #7
tstz11  sta zpt,x
        asl a
        dex
        bpl tstz11
        ldx #4
tstz15
        set_a $55,$ff
        stz zpt,x     
        tst_a $55,$ff
        dex
        bpl tstz15
        ldx #4
tstz12  lda zpt,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz12
        ldx #4      ;precharge test area
        lda #7
tstz13  sta zpt,x
        asl a
        dex
        bpl tstz13
        ldx #4
tstz16
        set_a $aa,0
        stz zpt,x
        tst_a $aa,0
        dex
        bpl tstz16
        ldx #4
tstz14  lda zpt,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz14
        
        ldx #4      ;precharge test area
        lda #7
tstz21  sta abst,x
        asl a
        dex
        bpl tstz21
        ldx #4
tstz25
        set_a $55,$ff
        stz abst,x     
        tst_a $55,$ff
        dex
        bpl tstz25
        ldx #4
tstz22  lda abst,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz22
        ldx #4      ;precharge test area
        lda #7
tstz23  sta abst,x
        asl a
        dex
        bpl tstz23
        ldx #4
tstz26
        set_a $aa,0
        stz abst,x
        tst_a $aa,0
        dex
        bpl tstz26
        ldx #4
tstz24  lda abst,x   ;verify zeros stored
        trap_ne     ;non zero after STZ zp
        dex
        bpl tstz24
        
        cpy #123
        trap_ne     ;y altered during test 
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test

; testing BIT - zp,x / abs,x / #
        ldy #$42
        ldx #3
        set_a $ff,0
        bit zp1,x   ;00 - should set Z / clear  NV
        tst_a $ff,fz 
        dex
        set_a 1,0
        bit zp1,x   ;41 - should set V (M6) / clear NZ
        tst_a 1,fv
        dex
        set_a 1,0
        bit zp1,x   ;82 - should set N (M7) & Z / clear V
        tst_a 1,fnz
        dex
        set_a 1,0
        bit zp1,x   ;c3 - should set N (M7) & V (M6) / clear Z
        tst_a 1,fnv
        
        set_a 1,$ff
        bit zp1,x   ;c3 - should set N (M7) & V (M6) / clear Z
        tst_a 1,~fz
        inx
        set_a 1,$ff
        bit zp1,x   ;82 - should set N (M7) & Z / clear V
        tst_a 1,~fv
        inx
        set_a 1,$ff
        bit zp1,x   ;41 - should set V (M6) / clear NZ
        tst_a 1,~fnz
        inx
        set_a $ff,$ff
        bit zp1,x   ;00 - should set Z / clear  NV
        tst_a $ff,~fnv 
        
        set_a $ff,0
        bit abs1,x  ;00 - should set Z / clear  NV
        tst_a $ff,fz 
        dex
        set_a 1,0
        bit abs1,x  ;41 - should set V (M6) / clear NZ
        tst_a 1,fv
        dex
        set_a 1,0
        bit abs1,x  ;82 - should set N (M7) & Z / clear V
        tst_a 1,fnz
        dex
        set_a 1,0
        bit abs1,x  ;c3 - should set N (M7) & V (M6) / clear Z
        tst_a 1,fnv
        
        set_a 1,$ff
        bit abs1,x  ;c3 - should set N (M7) & V (M6) / clear Z
        tst_a 1,~fz
        inx
        set_a 1,$ff
        bit abs1,x  ;82 - should set N (M7) & Z / clear V
        tst_a 1,~fv
        inx
        set_a 1,$ff
        bit abs1,x  ;41 - should set V (M6) / clear NZ
        tst_a 1,~fnz
        inx
        set_a $ff,$ff
        bit abs1,x  ;00 - should set Z / clear  NV
        tst_a $ff,~fnv 
        
        set_a $ff,0
        bit #$00    ;00 - should set Z
        tst_a $ff,fz 
        dex
        set_a 1,0
        bit #$41    ;41 - should clear Z
        tst_a 1,0
; *** DEBUG INFO ***
; if it fails the previous test and your BIT # has set the V flag
; see http://forum.6502.org/viewtopic.php?f=2&t=2241&p=27243#p27239
; why it shouldn't alter N or V flags on a BIT #
        dex
        set_a 1,0
        bit #$82    ;82 - should set Z
        tst_a 1,fz
        dex
        set_a 1,0
        bit #$c3    ;c3 - should clear Z
        tst_a 1,0
        
        set_a 1,$ff
        bit #$c3    ;c3 - clear Z
        tst_a 1,~fz
        inx
        set_a 1,$ff
        bit #$82    ;82 - should set Z
        tst_a 1,$ff
        inx
        set_a 1,$ff
        bit #$41    ;41 - should clear Z
        tst_a 1,~fz
        inx
        set_a $ff,$ff
        bit #$00   ;00 - should set Z
        tst_a $ff,$ff
        
        cpx #3
        trap_ne     ;x altered during test
        cpy #$42
        trap_ne     ;y altered during test 
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test

; testing TRB, TSB - zp / abs

trbt    macro       ;\1 = memory, \2 = flags   
        sty \1
        load_flag \2
        pha
        lda zpt+1
        plp
        trb \1
        php
        cmp zpt+1
        trap_ne     ;accu was changed
        pla
        pha
        ora #fz     ;mask Z
        cmp_flag \2|fz
        trap_ne     ;flags changed except Z
        pla
        and #fz
        cmp zpt+2
        trap_ne     ;Z flag invalid
        lda zpt+3
        cmp zpt
        trap_ne     ;altered bits in memory wrong       
        endm

tsbt    macro       ;\1 = memory, \2 = flags   
        sty \1
        load_flag \2
        pha
        lda zpt+1
        plp
        tsb \1
        php
        cmp zpt+1
        trap_ne     ;accu was changed
        pla
        pha
        ora #fz     ;mask Z
        cmp_flag \2|fz
        trap_ne     ;flags changed except Z
        pla
        and #fz
        cmp zpt+2
        trap_ne     ;Z flag invalid
        lda zpt+4
        cmp zpt
        trap_ne     ;altered bits in memory wrong        
        endm

        ldx #$c0
        ldy #0      ;op1 - memory save
        ;   zpt     ;op1 - memory modifiable
        stz zpt+1   ;op2 - accu
        ;   zpt+2   ;and flags
        ;   zpt+3   ;memory after reset
        ;   zpt+4   ;memory after set
        
tbt1    tya
        and zpt+1   ;set Z by anding the 2 operands
        php
        pla
        and #fz     ;mask Z
        sta zpt+2
        tya         ;reset op1 bits by op2
        eor #$ff
        ora zpt+1
        eor #$ff
        sta zpt+3
        tya         ;set op1 bits by op2
        ora zpt+1
        sta zpt+4

        trbt zpt,$ff
        trbt abst,$ff
        trbt zpt,0
        trbt abst,0
        tsbt zpt,$ff
        tsbt abst,$ff
        tsbt zpt,0
        tsbt abst,0
        
        iny         ;iterate op1
        bne tbt3
        inc zpt+1   ;iterate op2
        beq tbt2
tbt3    jmp tbt1        
tbt2
        cpx #$c0
        trap_ne     ;x altered during test
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test    

    if rkwl_wdc_op = 1
; testing RMB, SMB - zp
rmbt    macro       ;\1 = bitnum
        lda #$ff
        sta zpt
        set_a $a5,0
        rmb \1,zpt
        tst_a $a5,0
        lda zpt
        cmp #$ff-(1<<\1)
        trap_ne     ;wrong bits set or cleared
        lda #1<<\1
        sta zpt
        set_a $5a,$ff
        rmb \1,zpt
        tst_a $5a,$ff
        lda zpt
        trap_ne     ;wrong bits set or cleared
        endm
smbt    macro       ;\1 = bitnum
        lda #$ff-(1<<\1)
        sta zpt
        set_a $a5,0
        smb \1,zpt
        tst_a $a5,0
        lda zpt
        cmp #$ff
        trap_ne     ;wrong bits set or cleared
        lda #0
        sta zpt
        set_a $5a,$ff
        smb \1,zpt
        tst_a $5a,$ff
        lda zpt
        cmp #1<<\1
        trap_ne     ;wrong bits set or cleared
        endm

        ldx #$ba    ;protect x & y
        ldy #$d0
        rmbt 0
        rmbt 1
        rmbt 2
        rmbt 3
        rmbt 4
        rmbt 5
        rmbt 6
        rmbt 7
        smbt 0
        smbt 1
        smbt 2
        smbt 3
        smbt 4
        smbt 5
        smbt 6
        smbt 7
        cpx #$ba
        trap_ne     ;x altered during test
        cpy #$d0
        trap_ne     ;y altered during test
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test
    endif        
         
; testing CMP - (zp)         
        ldx #$de    ;protect x & y
        ldy #$ad
        set_a $80,0
        cmp (ind1+8)
        tst_a $80,fc
        set_a $7f,0
        cmp (ind1+8)
        tst_a $7f,fzc
        set_a $7e,0
        cmp (ind1+8)
        tst_a $7e,fn
        set_a $80,$ff
        cmp (ind1+8)
        tst_a $80,~fnz
        set_a $7f,$ff
        cmp (ind1+8)
        tst_a $7f,~fn
        set_a $7e,$ff
        cmp (ind1+8)
        tst_a $7e,~fzc
        cpx #$de
        trap_ne     ;x altered during test
        cpy #$ad
        trap_ne     ;y altered during test 
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test

; testing logical instructions - AND EOR ORA (zp)
        ldx #$42    ;protect x & y

        ldy #0      ;AND
        lda indAN   ;set indirect address
        sta zpt
        lda indAN+1
        sta zpt+1
tand1
        set_ay  absANa,0
        and (zpt)
        tst_ay  absrlo,absflo,0
        inc zpt
        iny
        cpy #4
        bne tand1
        dey
        dec zpt
tand2
        set_ay  absANa,$ff
        and (zpt)
        tst_ay  absrlo,absflo,$ff-fnz
        dec zpt
        dey
        bpl tand2

        ldy #0      ;EOR
        lda indEO   ;set indirect address
        sta zpt
        lda indEO+1
        sta zpt+1
teor1
        set_ay  absEOa,0
        eor (zpt)
        tst_ay  absrlo,absflo,0
        inc zpt
        iny
        cpy #4
        bne teor1
        dey
        dec zpt
teor2
        set_ay  absEOa,$ff
        eor (zpt)
        tst_ay  absrlo,absflo,$ff-fnz
        dec zpt
        dey
        bpl teor2

        ldy #0      ;ORA
        lda indOR   ;set indirect address
        sta zpt
        lda indOR+1
        sta zpt+1
tora1
        set_ay  absORa,0
        ora (zpt)
        tst_ay  absrlo,absflo,0
        inc zpt
        iny
        cpy #4
        bne tora1
        dey
        dec zpt
tora2
        set_ay  absORa,$ff
        ora (zpt)
        tst_ay  absrlo,absflo,$ff-fnz
        dec zpt
        dey
        bpl tora2

        cpx #$42
        trap_ne     ;x altered during test
        tsx
        cpx #$ff
        trap_ne     ;sp push/pop mismatch
        next_test
        
    if I_flag = 3
        cli
    endif                

; full binary add/subtract test - (zp) only
; iterates through all combinations of operands and carry input
; uses increments/decrements to predict result & result flags
        cld
        ldx #ad2        ;for indexed test
        ldy #$ff        ;max range
        lda #0          ;start with adding zeroes & no carry
        sta adfc        ;carry in - for diag
        sta ad1         ;operand 1 - accumulator
        sta ad2         ;operand 2 - memory or immediate
        sta ada2        ;non zp
        sta adrl        ;expected result bits 0-7
        sta adrh        ;expected result bit 8 (carry out)
        lda #$ff        ;complemented operand 2 for subtract
        sta sb2
        sta sba2        ;non zp
        lda #2          ;expected Z-flag
        sta adrf
tadd    clc             ;test with carry clear
        jsr chkadd
        inc adfc        ;now with carry
        inc adrl        ;result +1
        php             ;save N & Z from low result
        php
        pla             ;accu holds expected flags
        and #$82        ;mask N & Z
        plp
        bne tadd1
        inc adrh        ;result bit 8 - carry
tadd1   ora adrh        ;merge C to expected flags
        sta adrf        ;save expected flags except overflow
        sec             ;test with carry set
        jsr chkadd
        dec adfc        ;same for operand +1 but no carry
        inc ad1
        bne tadd        ;iterate op1
        lda #0          ;preset result to op2 when op1 = 0
        sta adrh
        inc ada2
        inc ad2
        php             ;save NZ as operand 2 becomes the new result
        pla
        and #$82        ;mask N00000Z0
        sta adrf        ;no need to check carry as we are adding to 0
        dec sb2         ;complement subtract operand 2
        dec sba2
        lda ad2         
        sta adrl
        bne tadd        ;iterate op2

        cpx #ad2
        trap_ne         ;x altered during test
        cpy #$ff
        trap_ne         ;y altered during test 
        tsx
        cpx #$ff
        trap_ne         ;sp push/pop mismatch
        next_test

; decimal add/subtract test
; *** WARNING - tests documented behavior only! ***
;   only valid BCD operands are tested, the V flag is ignored
;   although V is declared as beeing valid on the 65C02 it has absolutely
;   no use in BCD math. No sign = no overflow!
; iterates through all valid combinations of operands and carry input
; uses increments/decrements to predict result & carry flag
        sed 
        ldx #ad2        ;for indexed test
        ldy #$ff        ;max range
        lda #$99        ;start with adding 99 to 99 with carry
        sta ad1         ;operand 1 - accumulator
        sta ad2         ;operand 2 - memory or immediate
        sta ada2        ;non zp
        sta adrl        ;expected result bits 0-7
        lda #1          ;set carry in & out
        sta adfc        ;carry in - for diag
        sta adrh        ;expected result bit 8 (carry out)
        lda #$81        ;set N & C (99 + 99 + C = 99 + C)
        sta adrf
        lda #0          ;complemented operand 2 for subtract
        sta sb2
        sta sba2        ;non zp
tdad    sec             ;test with carry set
        jsr chkdad
        dec adfc        ;now with carry clear
        lda adrl        ;decimal adjust result
        bne tdad1       ;skip clear carry & preset result 99 (9A-1)
        dec adrh
        lda #$99
        sta adrl
        bne tdad3
tdad1   and #$f         ;lower nibble mask
        bne tdad2       ;no decimal adjust needed
        dec adrl        ;decimal adjust (?0-6)
        dec adrl
        dec adrl
        dec adrl
        dec adrl
        dec adrl
tdad2   dec adrl        ;result -1
tdad3   php             ;save valid flags
        pla
        and #$82        ;N-----Z-
        ora adrh        ;N-----ZC
        sta adrf
        clc             ;test with carry clear
        jsr chkdad
        inc adfc        ;same for operand -1 but with carry
        lda ad1         ;decimal adjust operand 1
        beq tdad5       ;iterate operand 2
        and #$f         ;lower nibble mask
        bne tdad4       ;skip decimal adjust
        dec ad1         ;decimal adjust (?0-6)
        dec ad1
        dec ad1
        dec ad1
        dec ad1
        dec ad1
tdad4   dec ad1         ;operand 1 -1
        jmp tdad        ;iterate op1

tdad5   lda #$99        ;precharge op1 max
        sta ad1
        lda ad2         ;decimal adjust operand 2
        beq tdad7       ;end of iteration
        and #$f         ;lower nibble mask
        bne tdad6       ;skip decimal adjust
        dec ad2         ;decimal adjust (?0-6)
        dec ad2
        dec ad2
        dec ad2
        dec ad2
        dec ad2
        inc sb2         ;complemented decimal adjust for subtract (?9+6)
        inc sb2
        inc sb2
        inc sb2
        inc sb2
        inc sb2
tdad6   dec ad2         ;operand 2 -1
        inc sb2         ;complemented operand for subtract
        lda sb2
        sta sba2        ;copy as non zp operand
        lda ad2
        sta ada2        ;copy as non zp operand
        sta adrl        ;new result since op1+carry=00+carry +op2=op2
        php             ;save flags
        pla
        and #$82        ;N-----Z-
        ora #1          ;N-----ZC
        sta adrf
        inc adrh        ;result carry
        jmp tdad        ;iterate op2

tdad7   cpx #ad2
        trap_ne         ;x altered during test
        cpy #$ff
        trap_ne         ;y altered during test 
        tsx
        cpx #$ff
        trap_ne         ;sp push/pop mismatch
        cld

        lda test_case
        cmp #test_num
        trap_ne         ;previous test is out of sequence
        lda #$f0        ;mark opcode testing complete
        sta test_case

; final RAM integrity test
;   verifies that none of the previous tests has altered RAM outside of the
;   designated write areas.
        check_ram
; *** DEBUG INFO ***
; to debug checksum errors uncomment check_ram in the next_test macro to 
; narrow down the responsible opcode.
; may give false errors when monitor, OS or other background activity is
; allowed during previous tests.


; S U C C E S S ************************************************       
; -------------       
        success         ;if you get here everything went well
; -------------       
; S U C C E S S ************************************************       
        jmp start       ;run again      

; core subroutine of the decimal add/subtract test
; *** WARNING - tests documented behavior only! ***
;   only valid BCD operands are tested, V flag is ignored
; iterates through all valid combinations of operands and carry input
; uses increments/decrements to predict result & carry flag
chkdad
; decimal ADC / SBC zp
        php             ;save carry for subtract
        lda ad1
        adc ad2         ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc sb2         ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC abs
        php             ;save carry for subtract
        lda ad1
        adc ada2        ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc sba2        ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC #
        php             ;save carry for subtract
        lda ad2
        sta ex_adci+1   ;set ADC # operand
        lda ad1
        jsr ex_adci     ;execute ADC # in RAM
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda sb2
        sta ex_sbci+1   ;set SBC # operand
        lda ad1
        jsr ex_sbci     ;execute SBC # in RAM
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC zp,x
        php             ;save carry for subtract
        lda ad1
        adc 0,x         ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc sb2-ad2,x   ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC abs,x
        php             ;save carry for subtract
        lda ad1
        adc ada2-ad2,x  ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc sba2-ad2,x  ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC abs,y
        php             ;save carry for subtract
        lda ad1
        adc ada2-$ff,y  ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc sba2-$ff,y  ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC (zp,x)
        php             ;save carry for subtract
        lda ad1
        adc (lo adi2-ad2,x) ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc (lo sbi2-ad2,x) ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC (abs),y
        php             ;save carry for subtract
        lda ad1
        adc (adiy2),y   ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc (sbiy2),y   ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
; decimal ADC / SBC (zp)
        php             ;save carry for subtract
        lda ad1
        adc (adi2)      ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc (sbi2)      ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$83        ;mask N-----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        rts

; core subroutine of the full binary add/subtract test
; iterates through all combinations of operands and carry input
; uses increments/decrements to predict result & result flags
chkadd  lda adrf        ;add V-flag if overflow
        and #$83        ;keep N-----ZC / clear V
        pha
        lda ad1         ;test sign unequal between operands
        eor ad2
        bmi ckad1       ;no overflow possible - operands have different sign
        lda ad1         ;test sign equal between operands and result
        eor adrl
        bpl ckad1       ;no overflow occured - operand and result have same sign
        pla
        ora #$40        ;set V
        pha
ckad1   pla
        sta adrf        ;save expected flags
; binary ADC / SBC (zp)
        php             ;save carry for subtract
        lda ad1
        adc (adi2)      ;perform add
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$c3        ;mask NV----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        php             ;save carry for next add
        lda ad1
        sbc (sbi2)      ;perform subtract
        php          
        cmp adrl        ;check result
        trap_ne         ;bad result
        pla             ;check flags
        and #$c3        ;mask NV----ZC
        cmp adrf
        trap_ne         ;bad flags
        plp
        rts
        
; target for the jump indirect test
ji_adr  dw test_ji
        dw ji_ret

        dey
        dey
test_ji
        php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        plp
        trap_cs         ;flags loaded?
        trap_vs
        trap_mi
        trap_eq 
        cmp #'I'        ;registers loaded?
        trap_ne
        cpx #'N'
        trap_ne        
        cpy #('D'-3)
        trap_ne
        pha             ;save a,x
        txa
        pha
        tsx
        cpx #$fd        ;check SP
        trap_ne
        pla             ;restore x
        tax
        set_stat $ff
        pla             ;restore a
        inx             ;return registers with modifications
        eor #$aa        ;N=1, V=1, Z=0, C=1
        jmp (ji_tab+2)
        nop
        nop
        trap            ;runover protection
        jmp start       ;catastrophic error - cannot continue

; target for the jump indirect test
jxi_adr dw  trap_ind
        dw  trap_ind
        dw  test_jxi    ;+4
        dw  jxi_ret     ;+6
        dw  trap_ind
        dw  trap_ind

        dey
        dey
test_jxi
        php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        plp
        trap_cs         ;flags loaded?
        trap_vs
        trap_mi
        trap_eq 
        cmp #'X'        ;registers loaded?
        trap_ne
        cpx #4
        trap_ne        
        cpy #('I'-3)
        trap_ne
        pha             ;save a,x
        txa
        pha
        tsx
        cpx #$fd        ;check SP
        trap_ne
        pla             ;restore x
        tax
        set_stat $ff
        pla             ;restore a
        inx             ;return registers with modifications
        inx
        eor #$aa        ;N=1, V=1, Z=0, C=1
        jmp (jxi_tab,x)
        nop
        nop
        trap            ;runover protection
        jmp start       ;catastrophic error - cannot continue

; JMP (abs,x) with bad x
        nop
        nop
trap_ind
        nop
        nop
        trap            ;near miss indexed indirect jump
        jmp start       ;catastrophic error - cannot continue

;trap in case of unexpected IRQ, NMI, BRK, RESET
nmi_trap
        trap            ;check stack for conditions at NMI
        jmp start       ;catastrophic error - cannot continue
res_trap
        trap            ;unexpected RESET
        jmp start       ;catastrophic error - cannot continue

        dey
        dey
irq_trap                ;BRK test or unextpected BRK or IRQ
        php             ;either SP or Y count will fail, if we do not hit
        dey
        dey
        dey
        ;next traps could be caused by unexpected BRK or IRQ
        ;check stack for BREAK and originating location
        ;possible jump/branch into weeds (uninitialized space)
        cmp #$ff-'B'    ;BRK pass 2 registers loaded?
        beq break2
        cmp #'B'        ;BRK pass 1 registers loaded?
        trap_ne
        cpx #'R'
        trap_ne        
        cpy #'K'-3
        trap_ne
        sta irq_a       ;save registers during break test
        stx irq_x
        tsx             ;test break on stack
        lda $102,x
        cmp_flag 0      ;break test should have B=1 & unused=1 on stack
        trap_ne         ;possible no break flag on stack
        pla
        cmp_flag intdis ;should have added interrupt disable
        trap_ne
        tsx
        cpx #$fc        ;sp -3? (return addr, flags)
        trap_ne
        lda $1ff        ;propper return on stack
        cmp #hi(brk_ret0)
        trap_ne
        lda $1fe
        cmp #lo(brk_ret0)
        trap_ne
        load_flag $ff
        pha
        ldx irq_x
        inx             ;return registers with modifications
        lda irq_a
        eor #$aa
        plp             ;N=1, V=1, Z=1, C=1 but original flags should be restored
        rti
        trap            ;runover protection
        jmp start       ;catastrophic error - cannot continue
        
break2                  ;BRK pass 2        
        cpx #$ff-'R'
        trap_ne        
        cpy #$ff-'K'-3
        trap_ne
        sta irq_a       ;save registers during break test
        stx irq_x
        tsx             ;test break on stack
        lda $102,x
        cmp_flag $ff    ;break test should have B=1
        trap_ne         ;possibly no break flag on stack
        pla
        cmp_flag $ff-decmode ;actual passed flags should have decmode cleared
        trap_ne
        tsx
        cpx #$fc        ;sp -3? (return addr, flags)
        trap_ne
        lda $1ff        ;propper return on stack
        cmp #hi(brk_ret1)
        trap_ne
        lda $1fe
        cmp #lo(brk_ret1)
        trap_ne
        load_flag intdis
        pha      
        ldx irq_x
        inx             ;return registers with modifications
        lda irq_a
        eor #$aa
        plp             ;N=0, V=0, Z=0, C=0 but original flags should be restored
        rti
        trap            ;runover protection
        jmp start       ;catastrophic error - cannot continue
        
    if report = 1
        include "report.i65"
    endif
            
;copy of data to initialize BSS segment
    if load_data_direct != 1
zp_init
zp1_    db  $c3,$82,$41,0   ;test patterns for LDx BIT ROL ROR ASL LSR
zp7f_   db  $7f             ;test pattern for compare  
;logical zeropage operands
zpOR_   db  0,$1f,$71,$80   ;test pattern for OR
zpAN_   db  $0f,$ff,$7f,$80 ;test pattern for AND
zpEO_   db  $ff,$0f,$8f,$8f ;test pattern for EOR
;indirect addressing pointers
ind1_   dw  abs1            ;indirect pointer to pattern in absolute memory
        dw  abs1+1
        dw  abs1+2
        dw  abs1+3
        dw  abs7f
inw1_   dw  abs1-$f8        ;indirect pointer for wrap-test pattern
indt_   dw  abst            ;indirect pointer to store area in absolute memory
        dw  abst+1
        dw  abst+2
        dw  abst+3
inwt_   dw  abst-$f8        ;indirect pointer for wrap-test store
indAN_  dw  absAN           ;indirect pointer to AND pattern in absolute memory
        dw  absAN+1
        dw  absAN+2
        dw  absAN+3
indEO_  dw  absEO           ;indirect pointer to EOR pattern in absolute memory
        dw  absEO+1
        dw  absEO+2
        dw  absEO+3
indOR_  dw  absOR           ;indirect pointer to OR pattern in absolute memory
        dw  absOR+1
        dw  absOR+2
        dw  absOR+3
;add/subtract indirect pointers
adi2_   dw  ada2            ;indirect pointer to operand 2 in absolute memory
sbi2_   dw  sba2            ;indirect pointer to complemented operand 2 (SBC)
adiy2_  dw  ada2-$ff        ;with offset for indirect indexed
sbiy2_  dw  sba2-$ff
zp_end
    if (zp_end - zp_init) != (zp_bss_end - zp_bss)   
        ;force assembler error if size is different   
        ERROR ERROR ERROR   ;mismatch between bss and zeropage data
    endif 
data_init
ex_adc_ adc #0              ;execute immediate opcodes
        rts
ex_sbc_ sbc #0              ;execute immediate opcodes
        rts
abs1_   db  $c3,$82,$41,0   ;test patterns for LDx BIT ROL ROR ASL LSR
abs7f_  db  $7f             ;test pattern for compare
;loads
fLDx_   db  fn,fn,0,fz      ;expected flags for load
;shifts
rASL_                       ;expected result ASL & ROL -carry  
rROL_   db  $86,$04,$82,0   ; "
rROLc_  db  $87,$05,$83,1   ;expected result ROL +carry
rLSR_                       ;expected result LSR & ROR -carry
rROR_   db  $61,$41,$20,0   ; "
rRORc_  db  $e1,$c1,$a0,$80 ;expected result ROR +carry
fASL_                       ;expected flags for shifts
fROL_   db  fnc,fc,fn,fz    ;no carry in
fROLc_  db  fnc,fc,fn,0     ;carry in
fLSR_
fROR_   db  fc,0,fc,fz      ;no carry in
fRORc_  db  fnc,fn,fnc,fn   ;carry in
;increments (decrements)
rINC_   db  $7f,$80,$ff,0,1 ;expected result for INC/DEC
fINC_   db  0,fn,fn,fz,0    ;expected flags for INC/DEC
;logical memory operand
absOR_  db  0,$1f,$71,$80   ;test pattern for OR
absAN_  db  $0f,$ff,$7f,$80 ;test pattern for AND
absEO_  db  $ff,$0f,$8f,$8f ;test pattern for EOR
;logical accu operand
absORa_ db  0,$f1,$1f,0     ;test pattern for OR
absANa_ db  $f0,$ff,$ff,$ff ;test pattern for AND
absEOa_ db  $ff,$f0,$f0,$0f ;test pattern for EOR
;logical results
absrlo_ db  0,$ff,$7f,$80
absflo_ db  fz,fn,0,fn
data_end
    if (data_end - data_init) != (data_bss_end - data_bss)
        ;force assembler error if size is different   
        ERROR ERROR ERROR   ;mismatch between bss and data
    endif 

vec_init
        dw  nmi_trap
        dw  res_trap
        dw  irq_trap
vec_bss equ $fffa
    endif                   ;end of RAM init data
    
; code at end of image due to the need to add blank space as required
    if ($ff & (ji_ret - * - 2)) < ($ff & (jxi_ret - * - 2))
; JMP (abs) when $xxff and $xx00 are from same page
        ds  lo(ji_ret - * - 2)
        nop
        nop
ji_px   nop             ;low address byte matched with ji_ret 
        nop
        trap            ;jmp indirect page cross bug

; JMP (abs,x) when $xxff and $xx00 are from same page
        ds  lo(jxi_ret - * - 2)
        nop
        nop
jxi_px  nop             ;low address byte matched with jxi_ret 
        nop
        trap            ;jmp indexed indirect page cross bug
    else
; JMP (abs,x) when $xxff and $xx00 are from same page
        ds  lo(jxi_ret - * - 2)
        nop
        nop
jxi_px  nop             ;low address byte matched with jxi_ret 
        nop
        trap            ;jmp indexed indirect page cross bug

; JMP (abs) when $xxff and $xx00 are from same page
        ds  lo(ji_ret - * - 2)
        nop
        nop
ji_px   nop             ;low address byte matched with ji_ret 
        nop
        trap            ;jmp indirect page cross bug
    endif
    
    if (load_data_direct = 1) & (ROM_vectors = 1)  
        org $fffa       ;vectors
        dw  nmi_trap
        dw  res_trap
        dw  irq_trap
    endif

        end start
