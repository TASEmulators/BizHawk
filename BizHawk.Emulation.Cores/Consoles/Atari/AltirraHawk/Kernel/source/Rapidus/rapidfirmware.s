;	Altirra - Atari 800/800XL/5200 emulator
;	Rapidus emulator bootstrap firmware - 65C816 firmware
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Rapidus 65C816 bootstrap firmware
;
; This firmware appears at $F0:0000-$F0:BFFF and contains 16-bit routines
; for use by the PBI boot as well as subsequently loaded software.
;
; IMPORTANT NOTE: This firmware is not intended to run on the real Rapidus!
; It takes many shortcuts for expediency and due to insufficient information.
;

		icl		'kerneldb.inc'
		icl		'hardware.inc'

		org		0
		opt		c+f+h-
		lmb		#$F0

i2c_dr		= $FF008C
i2c_cr		= $FF008D

;==========================================================================
; Signature
;
; Required for firmware to be recognized (the RA, at least)
;

		dta		'RAPIDUS '
		:8 dta 0

;==========================================================================
; Entry points
;
;--------------------------------------------------------------------------
;$F00010
;
		jmp		MenuCheck_L
		nop

;--------------------------------------------------------------------------
;$F00014
;
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00018
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F0001C
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00020
		jmp		MenuVBIHandler
		nop

;--------------------------------------------------------------------------
;$F00024 - initialize EEPROM access (not EEPROM data)
;
		jmp		EEPROMInit
		nop

;--------------------------------------------------------------------------
;$F00028 - write default configuration to EEPROM
;
		jmp		EEPROMReset
		nop

;--------------------------------------------------------------------------
;$F0002C - apply hardware configuration from EEPROM
;
		jmp		ApplyConfig
		nop

;--------------------------------------------------------------------------
;$F00030 - read configuration EEPROM data
;
		jmp		EEPROMRead
		nop

;--------------------------------------------------------------------------
;$F00034
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00038 - compute EEPROM checksum
;
		jmp		EEPROMCheck
		nop

;--------------------------------------------------------------------------
;$F0003C
;
		jmp		Syscall
		nop

;--------------------------------------------------------------------------
;$F00040 - 65C816 aware OS initialization
;
		jmp		Init816OS
		nop

;--------------------------------------------------------------------------
;$F00044
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00048
		jmp		Init
		nop

;--------------------------------------------------------------------------
;$F0004C
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00050
		jmp		Cleanup
		nop

;--------------------------------------------------------------------------
;$F00054
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00058
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F0005C
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00060
		jsl		$F00000+BugcheckInvalidEntryPoint

;--------------------------------------------------------------------------
;$F00064
		jsl		$F00000+BugcheckInvalidEntryPoint

.if * != 0x0068
.error "Entry points screwed up: ",*
.endif

;==========================================================================
.proc Bugcheck
_a = $10
_x = $12
_y = $14
_s = $16
_dp = $18
_b = $1a
_p = $1b
_pc = $1c
_k = $1e
_plot_x = $20
_plot_y = $22
_plot_line = $24
_va_args = $26

		;save P and disable IRQs
		php
		sei

		;save A
		rep		#$20
		sta.l	_a

		;disable NMIs
		sep		#$30
		lda		#0
		sta		$FF0000+nmien

		;disable hardware protect unit
		sta		$FF0090

		;reset MCR and CMCR
		rep		#$30
		lda.w	#$00FF
		sta		$FF0080

		;save DP and reset DP=0
		tdc
		sta.l	_dp
		lda.w	#0
		tcd

		;save P/B and reset B
		phb
		pla
		sta		_b

		;save PC and reset stack
		pla
		sta		_pc
		pla
		sta		_k
		lda.w	#$01FF
		tcs

		;save X and Y
		stx		_x
		sty		_y

		;copy display list to $07C0
		ldx.w	#dl_begin
		ldy.w	#$07C0
		lda.w	#[dl_end-dl_begin-1]
		mvn		$F0,0

		;reset GTIA/VBXE
		ldx		#0
		txa
		sta:rne	$D000,x+

		;wait for vertical blank
		sep		#$30
		lda		#124
		cmp:rne	vcount

		;bank in kernel ROM
		stz		pbctl
		stz		portb

		;set up ANTIC
		mwa		#$07C0 dlistl
		mva		#$22 dmactl
		mva		#$e0 chbase
		stz		chactl
		stz		colbk
		mva		#$44 colpf2
		mva		#$0a colpf1

		;set up plot code
		stz		_plot_x+1
		stz		_plot_y
		stz		_plot_y+1
		lda		#2
		sta		_plot_x
		jsr		plot_recompute_line

		;print message
		ldy		#1
		lda		_pc
		sta		_va_args
		lda		_pc+1
		sta		_va_args+1
		lda		_pc+2
		sta		_va_args+2
		jsr		vprintf

		jsr		printf
		dta		$0a,$0a,'PC=$#p',0

print_done:
		;wait
		jmp		*

printf:
		lda		1,s
		sta		_va_args
		lda		2,s
		sta		_va_args+1
		lda		#$F0
		sta		_va_args+2
		ldy		#1
		jsr		vprintf
		rep		#$20
		tya
		clc
		adc		1,s
		sta		1,s
		sep		#$20
		rts

vprintf_hex:
		pha
		lsr
		lsr
		lsr
		lsr
		tax
		lda		$F00000+vprintf_hexdig,x
		jsr		plot_putchar
		pla
		and		#$0f
		tax
		lda		$F00000+vprintf_hexdig,x
		jmp		plot_putchar

vprintf_hexdig:
		dta		'0123456789ABCDEF'

vprintf_special:
		iny
		lda		[_va_args],y
		cmp		#'p'
		bne		vprintf_not_pc
		lda		_pc+2
		jsr		vprintf_hex
		lda		_pc+1
		jsr		vprintf_hex
		lda		_pc+0
vprintf_hex_next:
		jsr		vprintf_hex
		bra		vprintf_next
vprintf_not_pc:
		cmp		#'a'
		bne		vprintf_not_a
		lda		_a
		bra		vprintf_hex_next
vprintf_not_a:
		cmp		#'x'
		bne		vprintf_not_x
		lda		_x
		bra		vprintf_hex_next
vprintf_not_x:
		cmp		#'y'
		bne		vprintf_not_y
		lda		_y
		bra		vprintf_hex_next
vprintf_not_y:
		bra		vprintf_next

vprintf_loop:
		cmp		#'#'
		beq		vprintf_special
		jsr		plot_putchar
vprintf_next:
		iny
vprintf:
		lda		[_va_args],y
		bne		vprintf_loop
		rts
		

plot_recompute_line:
		ldx		_plot_y
		rep		#$30
		txa
		asl
		asl
		adc		_plot_y
		asl
		asl
		asl
		adc.w	#$0400
		sta		_plot_line
		sep		#$30
		rts

plot_putchar:
		phy
		phx

		cmp		#$0a
		bne		plot_not_newline

		jsr		plot_newline
		bra		plot_putchar_done

plot_not_newline:
		pha
		rol
		rol
		rol
		rol
		and		#$03
		tax
		pla
		eor		$F00000+conv_tab,x

		ldy		_plot_x
		sta		(_plot_line),y
		iny
		sty		_plot_x
		cpy		#40
		bne		plot_putchar_done
		jsr		plot_newline
plot_putchar_done:
		plx
		ply
		rts

plot_newline:
		lda		_plot_y
		inc
		cmp		#24
		scc:lda	#0
		sta		_plot_y
		lda		#2
		sta		_plot_x
		jmp		plot_recompute_line

conv_tab:
		dta		$40
		dta		$20
		dta		$60
		dta		$00

dl_begin:
		:3 dta $70
		dta		$42,a($0400)
		:23 dta $02
		dta		$41,a($07C0)
dl_end:

.endp

;==========================================================================
.proc BugcheckInvalidEntryPoint
		rep		#$20
		lda		1,s
		dec
		dec
		dec
		sep		#$30
		tay
		xba
		tax
		lda		3,s
		jsl		$F00000+Bugcheck
		dta		'INVALID_ENTRY_POINT: $#a#x#y',0
.endp

;==========================================================================
.proc Init
		rtl
.endp

;==========================================================================
.proc Cleanup
		rtl
.endp

;==========================================================================
.proc ApplyConfig
		php
		sep		#$30
		jsr		ApplyConfig_I
		plp
		rtl
.endp

.proc ApplyConfig_I
		;copy EEPROM locations $03-$06 to MCR/CMCR/SCR/ACR
		ldx		#3
copy_loop:
		jsr		EEPROMRead_I
		sta		$FF0080-3,x
		inx
		cpx		#7
		bne		copy_loop
		rts
.endp

;==========================================================================
; System call facility
;
; Inputs:
;	A = service
;		7 = restart in 6502C mode
;
.proc Syscall
		cmp		#7
		bne		invalid

		;start the 6502 and halt the 65C816
		mva		#2 $FF0084
		stp

invalid:
		jsl		$F00000+Bugcheck
		dta		'INVALID_SYSTEM_CALL: $#a',0
.endp

;==========================================================================
.proc EEPROMInit
		rtl
.endp

;==========================================================================
; Read from EEPROM.
;
; Input:
;	X = EEPROM address
;
; Output:
;	A = EEPROM data
;
; Preserved:
;	X
;
.proc EEPROMRead
		php
		sep		#$30
		jsr		EEPROMRead_I
		plp
		rtl
.endp

.proc EEPROMRead_I
		;start read sequence
		lda		#$A1
		sta		i2c_dr
		lda		#$14
		sta		i2c_cr

		;set address
		txa
		sta		i2c_dr
		lda		#$10
		sta		i2c_cr

		;issue read command
		lda		#$00
		sta		i2c_cr

		lda		i2c_dr
		rts
.endp

;==========================================================================
; Write to EEPROM.
;
; Input:
;	X = EEPROM address
;
; Preserved:
;	X, Y
;
.proc EEPROMWrite
		php
		sep		#$30
		jsr		EEPROMWrite_I
		plp
		rtl
.endp

.proc EEPROMWrite_I
		;start write sequence
		pha
		lda		#$A0
		sta		i2c_dr
		lda		#$14
		sta		i2c_cr

		;write address
		txa
		sta		i2c_dr
		lda		#$10
		sta		i2c_cr

		;write data
		pla
		sta		i2c_dr
		lda		#$10
		sta		i2c_cr
		rts
.endp

;==========================================================================
; Checksum a region of the EEPROM.
;
; Input:
;	X = Last EEPROM address.
;	A = First EEPROM address.
;
; Output:
;	A = Checksum of addresses [A, X].
;
; The checksum algorithm used by Rapidus is pretty weird -- it involves a
; conditional XOR before each carry wraparound. The final checksum is then
; inverted.
;
.proc EEPROMCheck
		php
		sep		#$30
		jsr		EEPROMCheck_I
		plp
		rtl
.endp

.proc EEPROMCheck_I		
		pha
		lda		#0
		pha

read_loop:
		jsr		EEPROMRead_I
		clc
		adc		1,s
		spl:eor	#$aa
		adc		#0
		sta		1,s
		dex
		txa
		cmp		2,s
		bcs		read_loop

		pla
		eor		#$ff
		plx
		rts
.endp

;==========================================================================
; Reset EEPROM contents.
;
.proc EEPROMReset
		php
		sep		#$30
		jsr		EEPROMReset_I
		plp
		rtl
.endp

.proc EEPROMReset_I
		ldx		#[.len eeprom_default]-1
write_loop:
		lda.l	$F00000+eeprom_default,x
		jsr		EEPROMWrite_I
		dex
		bpl		write_loop
		rts
.endp

.proc eeprom_default
		dta		$A5		;checksum
		dta		$00
		dta		$00
		dta		$EF		;MCR: Base OS, I/O enabled, write-through enabled, SRAM disabled
		dta		$81		;CMCR: High fast RAM enabled, PORTB memory present
		dta		$80		;SCR: Cache enabled, bank disabled
		dta		$00		;ACR: Nothing
		dta		$00		;6502C mode disabled
.endp

;==========================================================================
; 65C816 aware OS initialization
;
; In the standard Rapidus flash, this allocates some memory from an 816
; aware OS for firmware use by means of kmalloc (COP #1 / $0001). We don't
; currently have a use for this and silently ignore the call.
;
.proc Init816OS
		rtl
.endp

;==========================================================================
.proc MenuCheck_L
		php
		sep		#$30
		jsr		MenuCheck
		plp
		rtl
.endp

.proc MenuCheck
		;check if Inverse key is held
		lda		kbcode
		cmp		#$27
		beq		enter
		rts

enter:
		;okay, Inverse key held -- enter the menu!

		;disable OS ROM
		lda		portb
		pha
		lda		#$FE
		sta		portb

		;reset MCR/CMCR
		lda		$FF0080
		pha
		lda		$FF0081
		pha
		lda		#$E7
		sta		$FF0080
		lda		#$81
		sta		$FF0081

		;run the menu
		jsr		MenuInit
		jsr		Menu
		jsr		MenuCleanup

		;restore MCR/CMCR
		pla
		sta		$FF0081
		pla
		sta		$FF0080

		;restore PORTB
		pla
		sta		portb
		rts
.endp

;==========================================================================
menu_cx		= $00
menu_cy		= $01
menu_lptr	= $02
menu_selidx	= $04
menu_stksave = $06
menu_optvals = $40
menu_enavals = $60
menu_colpf2 = $200
menu_colpm23 = $218

menu_missiles = $3D80
menu_player0 = $3E00
menu_player1 = $3E80
menu_player2 = $3F00
menu_player3 = $3F80

.proc Menu
		rep		#$20
		tsc
		sta.w	menu_stksave
		sep		#$20

menu_loop:
		;wait for key up
		lda		#$04
		bit:req	skstat

keydown_loop:
		;wait for key down
		lda		irqst
		bit		#$40
		bne		keydown_loop

		stz		irqen
		lda		#$40
		sta		irqen

		lda		kbcode
		ldx		#[.len menu_keys]-3
menu_key_loop:
		cmp.l	$F00000+menu_keys,x
		bne		not_key

		jsr		(menu_keys+1,x)
		jmp		menu_loop

not_key:
		dex
		dex
		dex
		bpl		menu_key_loop
		jmp		menu_loop
.endp

.proc menu_keys
		dta		$06,a(MenuKeyLeft)
		dta		$07,a(MenuKeyRight)
		dta		$86,a(MenuKeyLeft)
		dta		$87,a(MenuKeyRight)
		dta		$0E,a(MenuKeyUp)
		dta		$0F,a(MenuKeyDown)
		dta		$8E,a(MenuKeyUp)
		dta		$8F,a(MenuKeyDown)

		dta		$80,a(MenuKeyLoad)
		dta		$BE,a(MenuKeySave)
		dta		$95,a(MenuKeySaveAndBoot)
		dta		$1C,a(MenuKeyExit)
.endp

;==========================================================================
.proc MenuKeyLoad
		lda		#0
		jsr		MenuSelectOption
		jmp		MenuLoadConfiguration
.endp

;==========================================================================
.proc MenuKeySave
		jmp		MenuSaveConfiguration
.endp

;==========================================================================
.proc MenuKeySaveAndBoot
		jsr		MenuSaveConfiguration
		jmp		MenuKeyExit
.endp

;==========================================================================
.proc MenuKeyExit
		rep		#$20
		lda.w	menu_stksave
		tcs
		sep		#$20
		rts
.endp

;==========================================================================
.proc MenuKeyLeft
		ldx.w	menu_selidx
		lda.w	menu_optvals,x
		bne		not_first
		rts

not_first:
		dec.w	menu_optvals,x
		jsr		MenuRefreshOption
		jmp		MenuRefreshEnables
.endp

;==========================================================================
.proc MenuKeyRight
		;check if we are already on the last option value
		ldx.w	menu_selidx
		lda.w	menu_optvals,x
		asl
		pha
		txa
		asl
		tax
		pla
		rep		#$30
		and.w	#$00FF
		clc
		adc.l	$F00000+menuopt_defs,x
		tax
		lda.l	$F00002,x
		sep		#$30
		bne		not_first

		;already on last, just exit
		rts

not_first:
		ldx.w	menu_selidx
		inc.w	menu_optvals,x
		jsr		MenuRefreshOption
		jmp		MenuRefreshEnables
.endp

;==========================================================================
.proc MenuKeyUp
		lda.w	menu_selidx
up_loop:
		tax
		bne		not_first
		lda		#[.len menuopt_xys]/2
not_first:
		dec

		;check if enabled
		tax
		ldy.w	menu_enavals,x
		beq		up_loop

		jmp		MenuSelectOption
.endp

;==========================================================================
.proc MenuKeyDown
		lda.w	menu_selidx
down_loop:
		inc
		cmp		#[.len menuopt_xys]/2
		bcc		not_last
		lda		#0
not_last:

		;check if enabled
		tax
		ldy.w	menu_enavals,x
		beq		down_loop

		jmp		MenuSelectOption
.endp

;==========================================================================
.macro SCR_LITERAL
		.local
		.if data_start==data_end || (data_end-data_start)>127
		.error "Invalid literal length"
		.endif
		dta		data_end-data_start
data_start:
		dta		:1
		.if :0>1
		dta		:2
		.endif
		.if :0>2
		dta		:3
		.endif
		.if :0>3
		dta		:4
		.endif
		.if :0>4
		dta		:5
		.endif
		.if :0>5
		dta		:6
		.endif
		.if :0>6
		dta		:7
		.endif
		.if :0>7
		dta		:8
		.endif
data_end:
		.endl
.endm

.macro SCR_LITERALN count
		.if :count<1 || :count>127
		.error "Invalid literal length"
		.endif
		dta		:count
.endm

.macro SCR_POSITION x, y
		dta		$80,:x,:y
.endm

.macro SCR_HLINE count, value
		dta		$81,:count,:value
.endm

.macro SCR_VLINE count, value
		.if :count<2 || :count>129
		.error "Invalid repeat length"
		.endif

		dta		$82,:count,:value
.endm

.macro SCR_RECTCLEAR w, h
		dta		$83,:w,:h
.endm

.macro SCR_END
		dta		0
.endm

.proc MenuInit
		;save lower 32K
		rep		#$30
		ldx.w	#0
		ldy.w	#0
		lda.w	#$7FFF
		mvn		0,$ef

		;save upper 32b
		ldx.w	#$ffe0
		ldy.w	#$8000
		lda.w	#31
		mvn		0,$ef

		;upload new vectors
		ldx.w	#MenuHiCode
		ldy.w	#$10000-[.len MenuHiCode]
		lda.w	#[.len MenuHiCode]-1
		mvn		$f0,0

		;clear zero page
		stz		0
		ldx.w	#0
		ldy.w	#1
		lda.w	#$fe
		mvn		0,0

		;copy down font to $3C00
		rep		#$30
		ldx.w	#$e000
		ldy.w	#$7c00
		lda.w	#$03ff
		mvn		$f0,0

		;clear P/M graphics
		stz		menu_missiles
		ldx.w	#menu_missiles
		ldy.w	#menu_missiles+2
		lda.w	#$27D
		mvn		0,0

		;set players 0-2
		lda.w	#$FFFF
		sta		menu_player2+$20
		sta		menu_player3+$20

		ldx.w	#menu_player2+$20
		ldy.w	#menu_player2+$22
		lda.w	#4*15-1
		mvn		0,0
		ldx.w	#menu_player3+$20
		ldy.w	#menu_player3+$22
		lda.w	#4*15-1
		mvn		0,0

		;preset framebuffers
		sta		$4000
		ldx.w	#$4000
		ldy.w	#$4001
		lda.w	#$3FFE
		mvn		0,0

		;unpack display lists
		ldx.w	#dlist_data_lz
		ldy.w	#$7F00
		jsr		LZUnpack

		;copy display list 2 to display list 1
		ldx.w	#$7F00
		ldy.w	#$5F00
		lda.w	#$CD-1
		mvn		0,0

		;patch display list 1 to use its own framebuffer and jump back to 2
		lda.w	#$4010
		sta.w	$5F04
		lda.w	#$5000
		sta.w	$5F6C
		lda.w	#$7F00
		sta.w	$5FC8

		;unpack colors
		ldx.w	#colortables_lz
		ldy.w	#$0200
		jsr		LZUnpack

		;initialize display
		lda.w	#$7f00
		sta		dlistl
		lda.w	#$9ac2
		sta		colpf1
		sep		#$30
		mva		#$7c chbase
		stz		chactl
		stz		colbk
		lda		#$3C
		sta		pmbase
		lda		#$03
		sta		sizep0
		sta		sizep1
		sta		sizep2
		sta		sizep3
		sta		sizem
		mva		#$6A hposp0
		mva		#$76 hposp1
		mva		#$6A hposp2
		mva		#$76 hposp3
		mva		#$4F colpm0
		mva		#$4F colpm1
		mva		#$9F colpm2
		mva		#$9F colpm3
		mva		#$21 prior

		;draw screen
		ldx		#2
		ldy		#0
		jsr		Position80
		rep		#$10
		ldx.w	#menu_screen
		jsr		DrawScreen80
		sep		#$10

		;load configuration
		jsr		MenuLoadConfiguration

		;select first option
		lda		#$ff
		sta.w	menu_selidx
		inc
		jsr		MenuSelectOption

		;enable keyboard interrupts
		stz		irqen
		lda		#$40
		sta		irqen

		;wait for vertical blank
		lda		#124
		cmp:rne	vcount

		;enable display
		lda		#$2E
		sta		dmactl
		lda		#$c0
		sta		nmien
		lda		#$03
		sta		gractl

		rts

.macro LZ_LITERAL count
		.if :count<1 || :count>127
		.error "Invalid literal length"
		.endif
		dta		:count
.endm

.macro LZ_REPEAT count
		.if :count<2 || :count>65
		.error "Invalid repeat length"
		.endif

		dta		$BE+:count
.endm

.macro LZ_COPY count,distance
		.if :distance<1 || :distance>64
		.error "Invalid copy distance"
		.endif

		.if :count<2 || :count>257
		.error "Invalid copy length"
		.endif

		dta		$7F+:distance,:count-2
.endm

.macro LZ_END
		dta		0
.endm

dlist_data_lz:
		LZ_LITERAL 7
		dta		$70,$70,$F0,$4F,a($6010),$0F

		LZ_REPEAT 5
		LZ_LITERAL 2
		dta		$8F,$0F

		LZ_COPY 93,8
		LZ_LITERAL 3
		dta		$4F,a($7000)
		LZ_COPY 8,18
		LZ_COPY 81,8
		LZ_LITERAL 3
		dta		$41,a($5F00)
		LZ_END

colortables_lz:
		LZ_LITERAL 48

		dta		$BA
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$BA
		dta		$BA

		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9F
		dta		$9F
		dta		$9F
		dta		$9F
		dta		$9F
		dta		$94
		dta		$94
		dta		$94
		dta		$94
		dta		$94
		dta		$94
		dta		$94
		dta		$94
		dta		$9F
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A
		dta		$9A

		LZ_END

menu_screen:
		SCR_POSITION	29,0
		SCR_LITERAL		'Rapidus Configuration'

		;framing
		SCR_POSITION	2,1
		SCR_LITERAL		$11
		SCR_HLINE		50,$12
		SCR_LITERAL		$17
		SCR_HLINE		25,$12
		SCR_LITERAL		$05
		SCR_POSITION	2,2

		;vertical bars
		SCR_VLINE		19,$7C
		SCR_POSITION	53,2
		SCR_VLINE		19,$7C
		SCR_POSITION	79,2
		SCR_VLINE		19,$7C

		;help panel cutout
		SCR_POSITION	64,2
		SCR_LITERAL		'Help'
		SCR_POSITION	53,3
		SCR_LITERAL		$01
		SCR_HLINE		25,$12
		SCR_LITERAL		$04

		;bottom frame
		SCR_POSITION	2,21
		SCR_LITERAL		$1A
		SCR_HLINE		50,$12
		SCR_LITERAL		$18
		SCR_HLINE		25,$12
		SCR_LITERAL		$03

		;static options
		SCR_POSITION	5,4
		SCR_LITERAL		'CPU'
		SCR_POSITION	5,6
		SCR_LITERAL		'Fast RAM mode'
		SCR_POSITION	8,7
		SCR_LITERAL		'$0000-3FFF'
		SCR_POSITION	8,8
		SCR_LITERAL		'$4000-7FFF'
		SCR_POSITION	8,9
		SCR_LITERAL		'$8000-BFFF'
		SCR_POSITION	8,10
		SCR_LITERAL		'$C000-FFFF'
		SCR_POSITION	5,12
		SCR_LITERAL		'Operating System'
		SCR_POSITION	5,13
		SCR_LITERAL		'64K Wrapping'
		SCR_POSITION	5,14
		SCR_LITERAL		'SDRAM 4K cache'

		;help text at bottom
		SCR_POSITION	2,22
		SCR_LITERAL		$1C,$1D,'  Select option'
		SCR_POSITION	2,23
		SCR_LITERAL		$1E,$1F,'  Change option'
		SCR_POSITION	25,22
		SCR_LITERAL		'Ctrl+S  Save'
		SCR_POSITION	25,23
		SCR_LITERAL		'Ctrl+B  Save and boot'
		SCR_POSITION	50,22
		SCR_LITERAL		'Ctrl+L  Load'
		SCR_POSITION	50,23
		SCR_LITERAL		'Esc     Exit without saving'
		SCR_END
.endp

;==========================================================================
.proc MenuCleanup
		;disable display and VBI/DLIs
		stz		nmien
		stz		dmactl

		;reset PMBASE, in case someone depends on $00....
		stz		pmbase

		;kill P/M graphics
		stz		gractl
		stz		grafp0
		stz		grafp1
		stz		grafp2
		stz		grafp3
		stz		grafm

		;restore upper 32b
		rep		#$30
		ldx.w	#$8000
		ldy.w	#$ffe0
		lda.w	#31
		mvn		$ef,0

		;save return address
		pla
		sta.l	$EF8000

		;restore lower 32K
		ldx.w	#0
		ldy.w	#0
		lda.w	#$7FFF
		mvn		$ef,0

		;restore return address on new stack
		lda.l	$EF8000
		pha

		;restore IRQ enable mask
		lda.w	pokmsk
		sta		irqen

		sep		#$30
		rts
.endp

;==========================================================================
.proc LZUnpack
copy_loop:
		lda		$F00000,x
		inx
		and.w	#$00FF
		cmp.w	#$0080
		bcc		is_literal
		cmp.w	#$C0
		bcs		is_repeat

		;$80-BF nn = repeat nn+2 bytes from $01-40 back
		phx
		phy
		eor.w	#$0080^$FFFF
		clc
		adc		1,s
		ply
		pha
		lda		$F00000,x
		and.w	#$00FF
		inc
		plx
		mvn		0,0
		plx
		inx
		bra		copy_loop

is_repeat:		;$C0-FF = repeat last byte 2-64 times
		and.w	#$3F
		inc
		phx
		tyx
		dex
		mvn		0,0
		plx
		bra		copy_loop

is_literal:		;$01-7F = that many literals
		cmp.w	#0
		beq		is_done
		dec
		mvn		$F0,0
		bra		copy_loop

is_done:
		rts
.endp

;==========================================================================
; Menu VBI handler
;
; Entry:
;	DBK saved, DBK=0, M=1
;
; Exit:
;	Pop DBK and RTI
;
.proc MenuVBIHandler
		plb
		rti
.endp

;==========================================================================
.proc MenuHiCode
natcop	dta		a(0)			;FFE4
natbrk	dta		a(0)			;FFE6
natabrt	dta		a(0)			;FFE8
natnmi	dta		a($FFF0)		;FFEA
		dta		a(0)			;FFEC
natirq	dta		a(0)			;FFEE
nmi		jml		$F00000+MenuNMIHandler	;FFF0
emucop	dta		a(0)			;FFF4
		dta		a(0)			;FFF6
emuabrt	dta		a(0)			;FFF8
emunmi	dta		a(0)			;FFFA
emures	dta		a(0)			;FFFC
emuirq	dta		a(0)			;FFFE
.endp

;==========================================================================
.proc MenuNMIHandler
		phx
		phy
		php
		sep		#$30
		pha
		lda.l	nmist
		bpl		is_vbi

		lda		vcount
		lsr
		lsr
		tax

		lda.w	menu_colpf2-3,x
		ldy.w	menu_colpm23-3,x

		sta.l	wsync
		sta.l	colpf2
		tya
		sta.l	colpm2
		sta.l	colpm3

		pla
		plp
		ply
		plx
		rti

is_vbi:
		pla
		plp
		ply
		plx
		rti
.endp

;==========================================================================

.macro _MENU_OPTION mode,x,y,optdef,enaptr,helpptr
		:mode	:x,:y,:optdef,:enaptr,:helpptr
.endm

.macro _MENU_OPTIONS
		_MENU_OPTION	:1,30,4,menuopt_cpumode,MenuEnableTestAlways,menuopt_help_cpumode
		_MENU_OPTION	:1,30,7,menuopt_sram0mode,MenuEnableTest816,menuopt_help_fastsram0
		_MENU_OPTION	:1,30,8,menuopt_sram123mode,MenuEnableTest816,menuopt_help_fastsram123
		_MENU_OPTION	:1,30,9,menuopt_sram123mode,MenuEnableTest816,menuopt_help_fastsram123
		_MENU_OPTION	:1,30,10,menuopt_sram123mode,MenuEnableTest816,menuopt_help_fastsram123
		_MENU_OPTION	:1,30,12,menuopt_fwmode,MenuEnableTest816,menuopt_help_fwmode
		_MENU_OPTION	:1,30,13,menuopt_binary,MenuEnableTest816,menuopt_help_64kwrap
		_MENU_OPTION	:1,30,14,menuopt_binary,MenuEnableTest816,menuopt_help_sdramcache
.endm

.macro _MENU_OPTION_XY x,y,optdef,enaptr,helpptr
		dta :x,:y
.endm

.macro _MENU_OPTION_DEF x,y,optdef,enaptr,helpptr
		dta a(:optdef)
.endm

.macro _MENU_OPTION_ENATEST x,y,optdef,enaptr,helpptr
		dta a(:enaptr-1)
.endm

.macro _MENU_OPTION_HELPPTR x,y,optdef,enaptr,helpptr
		dta a(:helpptr)
.endm

.proc menuopt_xys
		_MENU_OPTIONS _MENU_OPTION_XY
.endp

.proc menuopt_defs
		_MENU_OPTIONS _MENU_OPTION_DEF
.endp

.proc menuopt_enatests
		_MENU_OPTIONS _MENU_OPTION_ENATEST
.endp

.proc menuopt_helpptrs
		_MENU_OPTIONS _MENU_OPTION_HELPPTR
.endp

menuopt_cpumode:
		dta		a(menuopt_cpumode_0)
		dta		a(menuopt_cpumode_1)
		dta		a(0)

menuopt_cpumode_0		dta		'6502  ',0
menuopt_cpumode_1		dta		'65C816',0

menuopt_sram0mode:
		dta		a(menuopt_srammode_0)
		dta		a(menuopt_srammode_1)
		dta		a(menuopt_srammode_2)
		dta		a(0)

menuopt_sram123mode:
		dta		a(menuopt_srammode_0)
		dta		a(menuopt_srammode_1)
		dta		a(0)

menuopt_srammode_0		dta		'Normal         ',0
menuopt_srammode_1		dta		'Fast read      ',0
menuopt_srammode_2		dta		'Fast read/write',0

menuopt_fwmode:
		dta		a(menuopt_fwmode_0)
		dta		a(menuopt_fwmode_1)
		dta		a(0)

menuopt_fwmode_0		dta		'Default',0
menuopt_fwmode_1		dta		'Rapidus',0

menuopt_binary:
		dta		a(menuopt_binary_0)
		dta		a(menuopt_binary_1)
		dta		a(0)

menuopt_binary_0		dta		'Disabled',0
menuopt_binary_1		dta		'Enabled ',0

;                        0123456789012345678901234
menuopt_help_cpumode:
		SCR_POSITION	54,4
		SCR_LITERAL		'Selects the 6502 or the'
		SCR_POSITION	54,5
		SCR_LITERAL		'65C816 as the CPU. When'
		SCR_POSITION	54,6
		SCR_LITERAL		'the 6502 is used, all'
		SCR_POSITION	54,7
		SCR_LITERAL		'Rapidus functions are'
		SCR_POSITION	54,8
		SCR_LITERAL		'disabled.'
		SCR_END

menuopt_help_fastsram0:
menuopt_help_fastsram123:
		SCR_POSITION	54,4
		SCR_LITERAL		'Enables fast SRAM in 16K'
		SCR_POSITION	54,5
		SCR_LITERAL		'windows, allowing the'
		SCR_POSITION	54,6
		SCR_LITERAL		'65C816 to run faster when'
		SCR_POSITION	54,7
		SCR_LITERAL		'accessing that memory.'
		SCR_POSITION	54,8
		SCR_LITERAL		'Fast read mode may cause'
		SCR_POSITION	54,9
		SCR_LITERAL		'problems if hardware'
		SCR_POSITION	54,10
		SCR_LITERAL		'exists in those regions.'
		SCR_POSITION	54,11
		SCR_LITERAL		'For $0000-3FFF, fast'
		SCR_POSITION	54,12
		SCR_LITERAL		'writes can also be'
		SCR_POSITION	54,13
		SCR_LITERAL		'enabled if no graphics'
		SCR_POSITION	54,14
		SCR_LITERAL		'are stored there.'
		SCR_END

menuopt_help_fwmode:
		SCR_POSITION	54,4
		SCR_LITERAL		'Selects either the OS ROM'
		SCR_POSITION	54,5
		SCR_LITERAL		'on the motherboard or the'
		SCR_POSITION	54,6
		SCR_LITERAL		'OS in on-board Rapidus'
		SCR_POSITION	54,7
		SCR_LITERAL		'flash. The on-board OS'
		SCR_POSITION	54,8
		SCR_LITERAL		'allows the 65C816 to run'
		SCR_POSITION	54,9
		SCR_LITERAL		'the OS faster.'
		SCR_END

menuopt_help_64kwrap:
		SCR_POSITION	54,4
		SCR_LITERAL		'Enables page zero mirror'
		SCR_POSITION	54,5
		SCR_LITERAL		'in bank $01 to prevent'
		SCR_POSITION	54,6
		SCR_LITERAL		'compatibility issues'
		SCR_POSITION	54,7
		SCR_LITERAL		'with software using'
		SCR_POSITION	54,8
		SCR_LITERAL		'wrapped indexing, such'
		SCR_POSITION	54,9
		SCR_LITERAL		'as MyDOS.'
		SCR_END

menuopt_help_sdramcache:
		SCR_POSITION	54,4
		SCR_LITERAL		'Enables faster access'
		SCR_POSITION	54,5
		SCR_LITERAL		'to SDRAM. (Note: This'
		SCR_POSITION	54,6
		SCR_LITERAL		'is not currently'
		SCR_POSITION	54,7
		SCR_LITERAL		'implemented in Altirra.)'
		SCR_END

;==========================================================================
.proc MenuRefreshEnables
		ldx		#0
loop:
		jsr		MenuRefreshOptionEnable
		inx
		cpx		#[.len menuopt_xys]/2
		bne		loop
		rts
.endp

;==========================================================================
.proc MenuRefreshOptionEnable
		phx
		txa
		asl
		tax
		rep		#$30
		lda.l	$F00000+menuopt_enatests,x
		pea		#done-1
		pha
		sep		#$31
		rts

done:
		plx
		phx

		php
		lda		#0
		rol
		sta		menu_enavals,x

		txa
		asl
		tax
		lda		$F00000+menuopt_xys+1,x
		tay
		plp

		lda		#$96
		scc:lda	#$9F

		sta.w	menu_colpm23,y

		plx
		rts
.endp

;==========================================================================
.proc MenuEnableTestAlways
		rts
.endp

;==========================================================================
.proc MenuEnableTest816
		lda.w	menu_optvals
		lsr
		rts
.endp

;==========================================================================
.proc MenuRefreshAllOptions
		ldx		#0
loop:
		jsr		MenuRefreshOption
		inx
		cpx		#[.len menuopt_xys]/2
		bne		loop
		rts
.endp

;==========================================================================
; Input:
;	X = option to refresh
;
; Preserved:
;	X
;
.proc MenuRefreshOption
		phx
		lda.w	menu_optvals,x
		pha
		txa
		asl
		tax
		phx
		lda.l	$F00000+menuopt_xys+1,x
		tay
		lda.l	$F00000+menuopt_xys,x
		tax
		jsr		Position80
		plx
		phx
		lda		2,s
		rep		#$30
		and.w	#$00FF
		asl
		clc
		adc.l	$F00000+menuopt_defs,x
		tax
		lda.l	$F00000,x
		sep		#$30
		jsr		PutString80
		plx
		pla
		plx
		rts
.endp

;==========================================================================
; Input:
;	A = option to select
;
.proc MenuSelectOption
		cmp.w	menu_selidx
		beq		done

		;save desired option
		pha

		;get previous option
		lda.w	menu_selidx

		;clear old highlight
		ldy		#$00
		jsr		write_highlight

		;set new highlight
		pla
		sta.w	menu_selidx
		ldy		#$FF
		jsr		write_highlight

		;clear help pane
		rep		#$10
		ldx.w	#help_clear
		jsr		DrawScreen80
		sep		#$10

		;draw help, if there is any for the option
		lda.w	menu_selidx
		asl
		tax
		rep		#$30
		lda.l	$F00000+menuopt_helpptrs,x
		beq		no_help
		tax
		sep		#$20
		jsr		DrawScreen80
no_help:
		sep		#$30

done:
		rts

help_clear:
		SCR_POSITION	54,4
		SCR_RECTCLEAR	25,12
		SCR_END

write_highlight:
		asl
		tax
		lda.l	$F00000+menuopt_xys+1,x
		asl
		asl
		tax
		tya
		xba
		tya
		rep		#$20
		sta.w	menu_player0+$10,x
		sta.w	menu_player0+$12,x
		sta.w	menu_player1+$10,x
		sta.w	menu_player1+$12,x
		sep		#$20
		rts
.endp

;==========================================================================
.proc MenuLoadConfiguration
		;compute EEPROM checksum of $07 down to $01, but not $00
		ldx		#7
		lda		#1
		jsr		EEPROMCheck_I
		pha

		;read EEPROM checksum byte
		ldx		#0
		jsr		EEPROMRead_I

		;check if checksum is valid
		eor		1,s
		plx
		tax
		beq		eeprom_valid

		;use defaults
		ldx		#7
reset_loop:
		lda.l	$F00000+defaults,x
		sta		menu_optvals,x
		dex
		bpl		reset_loop
		jmp		exit

defaults:
		dta		1,0,0,0,0,0,1,1

eeprom_valid:

		;wipe configuration
		ldx		#31
		stz:rpl	menu_optvals,x-

		;read EEPROM 7 and see if 6502 mode is enabled
		ldx		#7
		jsr		EEPROMRead_I

		eor		#1
		and		#1
		sta.w	menu_optvals

		;read control register values from EEPROM 3-6 into stack
		ldx		#6
read_loop:
		phx
		jsr		EEPROMRead_i
		plx
		pha
		dex
		cpx		#3
		bcs		read_loop

		;SRAM #0 option: MCR bit 0, CMCR bit 6
		ldx		#2
		lda		2,s					;get CMCR
		bit		#$40				;check if fast writes are enabled for $0000-3FFF
		bne		fastwrite0
		lda		1,s					;get MCR
		and		#$01
		eor		#$01
		tax
fastwrite0:
		stx.w	menu_optvals+1

		;SRAM #1-3 options come from MCR bits 0-2, inverted
		lda		1,s
		eor		#$FF
		lsr
		lsr
		rol.w	menu_optvals+2
		lsr
		rol.w	menu_optvals+3
		lsr
		rol.w	menu_optvals+4

		;firmware mode is MCR bit 7
		lda		1,s
		eor		#$80
		asl
		rol.w	menu_optvals+5

		;64K wrapping is CMCR bit 5
		lda		2,s
		and		#$20
		seq:inc.w	menu_optvals+6

		;SDRAM caching is SCR bit 7
		lda		3,s
		asl
		rol.w	menu_optvals+7

		;clean up stack
		pla
		pla
		pla
		pla

		;redraw and exit
exit:
		jsr		MenuRefreshAllOptions
		jmp		MenuRefreshEnables
.endp

;==========================================================================
.proc MenuSaveConfiguration
		;copy defaults to stack, except for checksum
		ldx		#1
copy_loop:
		lda		$F00000+eeprom_default,x
		pha
		inx
		cpx		#8
		bne		copy_loop

		;compute MCR
		lda		menu_optvals+5		;firmware mode
		asl
		asl
		asl
		asl
		ora		menu_optvals+4		;fast read #3
		asl
		ora		menu_optvals+3		;fast read #2
		asl
		ora		menu_optvals+2		;fast read #1
		ldx		menu_optvals+1		;fast read/write #0
		cpx		#1
		rol
		eor		#$EF
		sta		5,s

		;compute CMCR
		ldx		#$81
		lda		menu_optvals+1		;fast read/write #0
		and		#$02
		seq:ldx	#$C1
		txa
		ldy		menu_optvals+6		;64K wrapping
		seq:eor	#$20
		sta		4,s

		;compute SCR
		lda		menu_optvals+7
		lsr
		lda		#0
		ror
		sta		3,s

		;capture 6502 mode
		lda		menu_optvals
		eor		#1
		sta		1,s

		;write to EEPROM and compute checksum
		ldx		#7
		ldy		#0
write_loop:
		lda		1,s
		jsr		EEPROMWrite_I
		tya
		clc
		adc		1,s
		spl:eor	#$aa
		adc		#0
		tay
		pla
		dex
		bne		write_loop

		;write checksum
		tya
		eor		#$ff
		ldx		#0
		jsr		EEPROMWrite_I

		;all done
		rts
.endp

;==========================================================================
.proc DrawScreen80
		php
		sep		#$20

decode_loop:
		;fetch next opcode
		inx
		lda		$EFFFFF,x
		bmi		is_special
		beq		done

		;literal code
		tay
literal_loop:
		inx
		lda		$EFFFFF,x
		phx
		sep		#$10
		jsr		PutChar80
		rep		#$10
		plx
		dey
		bne		literal_loop
		bra		decode_loop

done:
		plp
		rts

is_special:
		pea		#decode_loop-1
		asl
		rep		#$20
		phx
		and.w	#$00FF
		tax
		lda.l	$F00000+special_tab,x
		plx
		pha
		lda.w	#0
		sep		#$20
		rts

special_tab:
		dta		a(special_position-1)
		dta		a(special_hline-1)
		dta		a(special_vline-1)
		dta		a(special_rectclear-1)

special_position:
		inx
		lda.l	$EFFFFF,x
		sta.w	menu_cx
		inx
		lda.l	$EFFFFF,x
		sta.w	menu_cy
		jmp		RecomputeLPtr80

special_hline:
		inx
		lda.l	$EFFFFF,x
		tay
		inx
		lda.l	$EFFFFF,x
		phx
		sep		#$30
special_hline_loop:
		pha
		jsr		PutChar80
		pla
		dey
		bne		special_hline_loop
		rep		#$10
		plx
		rts

special_vline:
		inx
		lda.l	$EFFFFF,x
		tay
		inx
		lda.l	$EFFFFF,x
		phx
		sep		#$30
special_vline_loop:
		pha
		phy
		jsr		RawPutChar80
		ply
		inc.w	menu_cy
		lda.w	menu_cy
		cmp		#24
		scc
		stz.w	menu_cy
		jsr		RecomputeLPtr80
		pla
		dey
		bne		special_vline_loop
		rep		#$10
		plx
		rts

special_rectclear:
		phx

		;save position
		lda.w	menu_cy
		pha
		lda.w	menu_cx
		pha

		;fetch width
		lda.l	$F00000,x
		pha

		;fetch height
		lda.l	$F00001,x
		pha

		sep		#$10
special_rectclear_loop:
		lda		3,s
		tax
		lda		4,s
		tay
		jsr		Position80
		lda		2,s
		tax
		lda		#' '
		jsr		RepeatChar80

		lda		4,s
		inc
		sta		4,s
		pla
		dec
		pha
		bne		special_rectclear_loop

		rep		#$10
		plx
		plx
		plx
		inx
		inx
		rts
.endp

;==========================================================================
; Print zero-terminated string.
;
; Input:
;	B:A = string address (255 chars max)
;
.proc PutString80
		phx
		rep		#$10
		tax
char_loop:
		lda		$F00000,x
		beq		done
		phx
		sep		#$10
		jsr		PutChar80
		rep		#$10
		plx
		inx
		bra		char_loop
done:
		sep		#$10
		plx
		rts
.endp

;==========================================================================
.proc RepeatChar80
		tay
loop:
		tya
		jsr		PutChar80
		dex
		bne		loop
		rts
.endp

;==========================================================================
; Print a character on the 80screen, with special char processing.
;
; Inputs:
;	A = character
;
; Preserved:
;	X, Y
;
.proc PutChar80
		cmp		#$0a
		bne		not_lf

lf:
		lda		#$02
		sta.w	menu_cx
		lda.w	menu_cy
		inc
		cmp		#24
		bne		no_scroll
		lda		#0
no_scroll:
		sta.w	menu_cy
		jmp		RecomputeLPtr80

not_lf:
		phx
		phy
		jsr		RawPutChar80
		ply
		plx
		inc.w	menu_cx
		lda.w	menu_cx
		cmp		#80
		bcs		lf
		rts
.endp

;==========================================================================
.proc Position80
		stx.w	menu_cx
		sty.w	menu_cy
		bra		RecomputeLPtr80
.endp

;==========================================================================
.proc RecomputeLPtr80
		lda.w	menu_cy
		asl					;x2
		asl					;x4
		adc.w	menu_cy		;x5
		xba					;x1280
		lda		#0
		rep		#$20
		lsr					;x640
		lsr					;x320
		sta.w	menu_lptr
		sep		#$20
		rts
.endp

;==========================================================================
.proc RawPutChar80
		tax
		ldy.w	menu_cx

		;compute destination offset
		rep		#$30
		tya
		lsr
		php
		clc
		adc.w	menu_lptr
		tay

		;blast character to screen
		plp
		sep		#$20

		bcc		even
		jmp		odd

even:
		.rept 8,#
			lda		$F00000+font80_even:1,x
			eor		$4010+40*#,y
			and		#$F0
			eor		$4010+40*#,y
			sta		$4010+40*#,y
			lda		$F00000+font80_odd:1,x
			eor		$6010+40*#,y
			and		#$F0
			eor		$6010+40*#,y
			sta		$6010+40*#,y
		.endr

		jmp		done

odd:
		.rept 8,#
			lda		$F00000+font80_even:1,x
			eor		$4010+40*#,y
			and		#$0F
			eor		$4010+40*#,y
			sta		$4010+40*#,y
			lda		$F00000+font80_odd:1,x
			eor		$6010+40*#,y
			and		#$0F
			eor		$6010+40*#,y
			sta		$6010+40*#,y
		.endr

done:
		sep		#$30
		rts
.endp

;==========================================================================
; Created by font80.cpp.


font80_even0:
		dta		$FF,$BB,$CC,$BB,$BB,$FF,$DD,$FF,$FF,$FF,$FF,$99,$77,$00,$FF,$FF
		dta		$FF,$FF,$FF,$BB,$FF,$FF,$FF,$FF,$BB,$77,$BB,$33,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$BB,$FF,$BB,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$DD,$FF,$77,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$FF,$BB,$FF

font80_odd0:
		dta		$FF,$BB,$EE,$BB,$BB,$FF,$EE,$77,$EE,$FF,$FF,$CC,$33,$00,$FF,$FF
		dta		$FF,$FF,$FF,$BB,$FF,$FF,$77,$FF,$BB,$33,$BB,$33,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$BB,$FF,$99,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$DD,$FF,$77,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$FF,$FF,$BB

font80_even1:
		dta		$99,$BB,$EE,$BB,$BB,$FF,$CC,$77,$EE,$FF,$77,$CC,$33,$00,$FF,$FF
		dta		$99,$FF,$FF,$BB,$FF,$FF,$77,$FF,$BB,$33,$BB,$77,$BB,$BB,$BB,$BB
		dta		$FF,$BB,$55,$55,$99,$55,$99,$BB,$DD,$33,$55,$BB,$FF,$FF,$FF,$DD
		dta		$99,$BB,$99,$11,$DD,$11,$99,$11,$99,$99,$FF,$FF,$DD,$FF,$BB,$99
		dta		$99,$BB,$11,$99,$33,$11,$11,$99,$55,$11,$DD,$55,$77,$66,$55,$99
		dta		$11,$99,$11,$99,$11,$55,$55,$66,$55,$55,$11,$99,$77,$33,$FF,$FF
		dta		$BB,$FF,$77,$FF,$DD,$FF,$DD,$FF,$77,$BB,$DD,$77,$BB,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$BB,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$BB,$11,$BB,$BB

font80_odd1:
		dta		$55,$BB,$CC,$BB,$BB,$FF,$DD,$77,$DD,$FF,$FF,$99,$77,$00,$FF,$FF
		dta		$BB,$FF,$FF,$BB,$FF,$FF,$FF,$FF,$BB,$77,$BB,$77,$BB,$BB,$BB,$BB
		dta		$FF,$BB,$55,$55,$11,$55,$55,$BB,$99,$77,$55,$BB,$FF,$FF,$FF,$DD
		dta		$33,$BB,$33,$11,$BB,$11,$33,$11,$33,$33,$FF,$FF,$BB,$FF,$77,$33
		dta		$33,$BB,$33,$33,$33,$11,$11,$11,$55,$11,$DD,$55,$77,$55,$55,$33
		dta		$33,$33,$33,$33,$11,$55,$55,$55,$55,$55,$11,$99,$FF,$33,$BB,$FF
		dta		$BB,$FF,$77,$FF,$DD,$FF,$99,$FF,$77,$BB,$DD,$77,$33,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$BB,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$BB,$11,$BB,$BB

font80_even2:
		dta		$11,$BB,$CC,$BB,$BB,$FF,$99,$77,$DD,$FF,$77,$99,$77,$FF,$FF,$FF
		dta		$BB,$FF,$FF,$BB,$33,$FF,$FF,$FF,$BB,$77,$BB,$33,$33,$BB,$77,$BB
		dta		$FF,$BB,$55,$11,$77,$33,$BB,$BB,$BB,$33,$33,$BB,$FF,$FF,$FF,$BB
		dta		$55,$33,$55,$BB,$BB,$77,$77,$DD,$55,$55,$BB,$BB,$BB,$11,$BB,$55
		dta		$55,$33,$55,$55,$33,$77,$77,$77,$55,$BB,$DD,$33,$77,$55,$55,$55
		dta		$55,$55,$55,$77,$BB,$55,$55,$55,$55,$55,$BB,$BB,$77,$BB,$BB,$FF
		dta		$33,$33,$77,$33,$DD,$33,$BB,$11,$77,$FF,$FF,$77,$BB,$55,$33,$33
		dta		$33,$11,$33,$11,$11,$55,$55,$55,$55,$55,$11,$33,$BB,$33,$33,$BB

font80_odd2:
		dta		$00,$BB,$EE,$BB,$BB,$FF,$DD,$33,$CC,$FF,$77,$CC,$33,$FF,$FF,$FF
		dta		$99,$FF,$FF,$BB,$99,$FF,$77,$FF,$BB,$33,$BB,$33,$99,$BB,$BB,$DD
		dta		$FF,$BB,$55,$00,$77,$55,$99,$BB,$99,$BB,$99,$BB,$FF,$FF,$FF,$DD
		dta		$55,$BB,$55,$DD,$99,$77,$77,$DD,$55,$55,$BB,$BB,$BB,$11,$BB,$55
		dta		$55,$99,$55,$55,$55,$77,$77,$77,$55,$BB,$DD,$55,$77,$00,$11,$55
		dta		$55,$55,$55,$77,$BB,$55,$55,$66,$55,$55,$DD,$BB,$77,$BB,$99,$FF
		dta		$99,$99,$77,$99,$DD,$99,$BB,$99,$77,$FF,$FF,$77,$BB,$55,$11,$99
		dta		$11,$99,$11,$99,$11,$55,$55,$66,$55,$55,$11,$99,$BB,$33,$BB,$99

font80_even3:
		dta		$00,$88,$EE,$33,$33,$33,$99,$BB,$CC,$FF,$33,$CC,$33,$FF,$FF,$FF
		dta		$00,$88,$00,$00,$11,$FF,$77,$00,$00,$33,$88,$77,$11,$BB,$11,$11
		dta		$FF,$BB,$55,$55,$99,$BB,$BB,$BB,$BB,$BB,$00,$11,$FF,$11,$FF,$BB
		dta		$55,$BB,$DD,$BB,$99,$11,$11,$DD,$99,$99,$BB,$BB,$BB,$FF,$DD,$DD
		dta		$55,$55,$11,$77,$55,$11,$11,$77,$11,$BB,$DD,$33,$77,$00,$11,$55
		dta		$55,$55,$55,$99,$BB,$55,$55,$66,$99,$99,$BB,$BB,$BB,$BB,$99,$FF
		dta		$11,$DD,$11,$77,$99,$55,$99,$55,$11,$BB,$DD,$55,$BB,$00,$55,$55
		dta		$55,$55,$55,$77,$BB,$55,$55,$66,$99,$55,$DD,$11,$BB,$11,$33,$99

font80_odd3:
		dta		$11,$88,$CC,$33,$33,$33,$BB,$33,$99,$FF,$77,$99,$77,$FF,$FF,$FF
		dta		$55,$88,$00,$00,$11,$FF,$FF,$00,$00,$77,$88,$77,$11,$BB,$11,$11
		dta		$FF,$BB,$55,$55,$33,$BB,$33,$BB,$BB,$BB,$11,$11,$FF,$11,$FF,$BB
		dta		$11,$BB,$BB,$BB,$33,$33,$33,$BB,$33,$11,$BB,$BB,$77,$FF,$BB,$BB
		dta		$11,$55,$33,$77,$55,$33,$33,$77,$11,$BB,$DD,$33,$77,$11,$11,$55
		dta		$55,$55,$55,$33,$BB,$55,$55,$11,$33,$33,$BB,$BB,$77,$BB,$55,$FF
		dta		$11,$DD,$33,$77,$11,$55,$11,$55,$33,$33,$DD,$33,$BB,$11,$55,$55
		dta		$55,$55,$55,$77,$BB,$55,$55,$11,$33,$55,$BB,$11,$BB,$33,$33,$99

font80_even4:
		dta		$11,$88,$CC,$33,$33,$33,$33,$BB,$99,$99,$33,$FF,$FF,$FF,$FF,$77
		dta		$55,$88,$00,$00,$11,$00,$FF,$00,$00,$77,$88,$11,$BB,$11,$77,$BB
		dta		$FF,$BB,$FF,$55,$DD,$77,$11,$FF,$BB,$BB,$33,$BB,$FF,$FF,$FF,$77
		dta		$55,$BB,$BB,$BB,$33,$DD,$55,$BB,$55,$DD,$FF,$FF,$BB,$FF,$BB,$BB
		dta		$11,$55,$55,$77,$55,$77,$77,$11,$55,$BB,$DD,$33,$77,$11,$11,$55
		dta		$33,$55,$33,$DD,$BB,$55,$55,$11,$33,$BB,$77,$BB,$BB,$BB,$55,$FF
		dta		$11,$11,$55,$77,$55,$11,$BB,$55,$55,$BB,$DD,$33,$BB,$11,$55,$55
		dta		$55,$55,$77,$33,$BB,$55,$55,$11,$BB,$55,$BB,$11,$BB,$11,$33,$BB

font80_odd4:
		dta		$99,$88,$EE,$33,$33,$33,$BB,$99,$88,$CC,$33,$FF,$FF,$FF,$FF,$33
		dta		$00,$88,$00,$00,$11,$00,$77,$00,$00,$33,$88,$11,$BB,$11,$BB,$DD
		dta		$FF,$BB,$FF,$55,$DD,$BB,$44,$FF,$BB,$BB,$99,$BB,$FF,$FF,$FF,$BB
		dta		$11,$BB,$BB,$DD,$55,$DD,$55,$BB,$55,$DD,$FF,$FF,$BB,$FF,$BB,$BB
		dta		$55,$55,$55,$77,$55,$77,$77,$55,$55,$BB,$DD,$33,$77,$66,$11,$55
		dta		$11,$55,$11,$DD,$BB,$55,$55,$00,$99,$BB,$BB,$BB,$BB,$BB,$66,$FF
		dta		$11,$99,$55,$77,$55,$11,$BB,$55,$55,$BB,$DD,$33,$BB,$00,$55,$55
		dta		$55,$55,$77,$99,$BB,$55,$55,$00,$BB,$55,$BB,$11,$BB,$55,$BB,$99

font80_even5:
		dta		$99,$BB,$EE,$FF,$BB,$BB,$33,$DD,$88,$CC,$11,$FF,$FF,$FF,$FF,$33
		dta		$FF,$BB,$FF,$BB,$11,$00,$77,$BB,$FF,$33,$FF,$BB,$BB,$99,$BB,$BB
		dta		$FF,$FF,$FF,$00,$11,$55,$55,$FF,$99,$BB,$55,$BB,$BB,$FF,$BB,$77
		dta		$55,$BB,$BB,$55,$11,$55,$55,$BB,$55,$DD,$BB,$BB,$DD,$11,$BB,$FF
		dta		$77,$11,$55,$55,$55,$77,$77,$55,$55,$BB,$55,$55,$77,$66,$55,$55
		dta		$77,$55,$55,$DD,$BB,$55,$99,$00,$55,$BB,$77,$BB,$DD,$BB,$FF,$FF
		dta		$99,$55,$55,$77,$55,$77,$BB,$99,$55,$BB,$DD,$55,$BB,$66,$55,$55
		dta		$11,$99,$77,$DD,$BB,$55,$99,$99,$99,$99,$BB,$BB,$BB,$55,$BB,$BB

font80_odd5:
		dta		$BB,$BB,$CC,$FF,$BB,$BB,$77,$99,$11,$99,$33,$FF,$FF,$FF,$FF,$77
		dta		$BB,$BB,$FF,$BB,$11,$00,$FF,$BB,$FF,$77,$FF,$BB,$BB,$33,$BB,$BB
		dta		$FF,$FF,$FF,$11,$33,$55,$55,$FF,$BB,$33,$55,$BB,$BB,$FF,$BB,$77
		dta		$55,$BB,$77,$55,$11,$55,$55,$77,$55,$BB,$BB,$BB,$BB,$11,$77,$FF
		dta		$77,$11,$55,$55,$33,$77,$77,$55,$55,$BB,$55,$33,$77,$55,$11,$55
		dta		$77,$33,$33,$DD,$BB,$55,$33,$55,$55,$BB,$77,$BB,$BB,$BB,$FF,$FF
		dta		$33,$55,$55,$77,$55,$77,$BB,$11,$55,$BB,$DD,$33,$BB,$11,$55,$55
		dta		$33,$11,$77,$DD,$BB,$55,$33,$11,$33,$11,$77,$BB,$BB,$55,$BB,$BB

font80_even6:
		dta		$BB,$BB,$CC,$FF,$BB,$BB,$77,$DD,$11,$99,$11,$FF,$FF,$FF,$00,$77
		dta		$BB,$BB,$FF,$BB,$33,$00,$FF,$BB,$FF,$77,$FF,$99,$BB,$BB,$FF,$FF
		dta		$FF,$BB,$FF,$55,$BB,$DD,$11,$FF,$99,$77,$FF,$FF,$BB,$FF,$BB,$FF
		dta		$33,$11,$11,$33,$BB,$33,$33,$77,$33,$33,$BB,$BB,$DD,$FF,$77,$BB
		dta		$11,$55,$33,$33,$33,$11,$77,$11,$55,$11,$33,$55,$11,$55,$55,$33
		dta		$77,$55,$55,$33,$BB,$11,$BB,$55,$55,$BB,$11,$99,$DD,$33,$FF,$11
		dta		$BB,$11,$33,$33,$11,$33,$BB,$DD,$55,$33,$DD,$55,$33,$55,$55,$33
		dta		$77,$DD,$77,$33,$99,$11,$BB,$55,$55,$BB,$11,$33,$BB,$DD,$BB,$FF

font80_odd6:
		dta		$FF,$BB,$EE,$FF,$BB,$BB,$77,$CC,$00,$CC,$11,$FF,$FF,$FF,$00,$33
		dta		$99,$BB,$FF,$BB,$99,$00,$77,$BB,$FF,$33,$FF,$99,$BB,$BB,$FF,$FF
		dta		$FF,$BB,$FF,$55,$BB,$55,$AA,$FF,$DD,$33,$FF,$FF,$BB,$FF,$BB,$77
		dta		$99,$11,$11,$99,$DD,$99,$99,$BB,$99,$BB,$BB,$BB,$DD,$FF,$77,$BB
		dta		$99,$55,$11,$99,$33,$11,$77,$99,$55,$11,$99,$55,$11,$66,$55,$99
		dta		$77,$99,$55,$99,$BB,$11,$BB,$66,$55,$BB,$11,$99,$DD,$33,$FF,$00
		dta		$BB,$99,$11,$99,$99,$99,$BB,$DD,$55,$99,$DD,$55,$99,$66,$55,$99
		dta		$77,$DD,$77,$11,$DD,$99,$BB,$99,$55,$DD,$11,$99,$BB,$DD,$FF,$BB

font80_even7:
		dta		$FF,$BB,$EE,$FF,$BB,$BB,$77,$EE,$00,$CC,$00,$FF,$FF,$FF,$00,$33
		dta		$FF,$BB,$FF,$BB,$FF,$00,$77,$BB,$FF,$33,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$BB,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$11,$FF,$FF,$99,$FF,$FF,$FF,$FF,$FF
		dta		$77,$DD,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$33,$FF,$FF,$BB,$FF,$FF,$FF

font80_odd7:
		dta		$FF,$BB,$CC,$FF,$BB,$BB,$FF,$DD,$11,$99,$11,$FF,$FF,$FF,$00,$77
		dta		$FF,$BB,$FF,$BB,$FF,$00,$FF,$BB,$FF,$77,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$77,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$77,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$FF
		dta		$FF,$FF,$FF,$FF,$FF,$FF,$FF,$33,$FF,$FF,$33,$FF,$FF,$FF,$FF,$FF
		dta		$77,$DD,$FF,$FF,$FF,$FF,$FF,$FF,$FF,$33,$FF,$FF,$BB,$FF,$FF,$FF

;==========================================================================

		.align	$4000,$00

.if * < $C000
		dta		$FF
		.align	$4000,$00
.endif

.if * < $C000
		dta		$FF
		.align	$4000,$00
.endif
