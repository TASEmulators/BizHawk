;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 Vertical Blank Interrupt Services
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; VBIExit - Vertical Blank Interrupt Exit Routine
;
; This is a drop-in replacement for XITVBV.
;
VBIExit = VBIProcess.xit

;==========================================================================
; VBIProcess - Vertical Blank Processor
;
VBIStage1 = VBIProcess.stage_1
VBIStage2 = VBIProcess.stage_2
.proc VBIProcess
stage_1:
	;increment real-time clock and do attract processing
	inc		rtclok+2
	bne		clock_done
	inc		atract
	inc		rtclok+1
	bne		clock_done
	inc		rtclok
clock_done:

	;Pole Position depends on DRKMSK and COLRSH being reset from VBI as it
	;clears kernel vars after startup.
	ldx		#$fe				;default to no mask
	lda		#$00				;default to no color alteration
	ldy		atract				;check attract counter
	bpl		attract_off			;skip if attract is off
	stx		atract				;lock the attract counter
	ldx		#$f6				;set mask to dim colors
	lda		rtclok+1			;use clock to randomize colors
attract_off:	
	stx		drkmsk				;set color mask
	sta		colrsh				;set color modifier
	
	;atract color 1 only
	eor		color1
	and		drkmsk
	sta		colpf1

	;decrement timer 1 and check for underflow
	lda		cdtmv1				;check low byte
	bne		timer1_lobytezero	;if non-zero, decrement and check for fire
	lda		cdtmv1+1			;check high byte
	beq		timer1_done			;if clear, timer's not running
	dec		cdtmv1+1			;decrement high byte
	dec		cdtmv1				;decrement low byte
	bne		timer1_done			;we're done
timer1_lobytezero:
	dec		cdtmv1				;decrement low byte
	bne		timer1_done
	lda		cdtmv1+1			;check if high byte is zero
	bne		timer1_done			;if it's not, we're not done yet ($xx00 > 0)
	jsr		timer1_dispatch		;jump through timer vector
timer1_done:

	;check for critical operation
	lda		critic
	beq		no_critic
xit:
	ply
	plx
exit_a:
	pla
exit_none:
	rti

timer1_dispatch:
	jmp		(cdtma1)

no_critic:
	lda		#$04			;I flag
	and		4,s				;I flag set on pushed stack?
	bne		xit				;exit if so
	
	;======== stage 2 processing
	
stage_2:	
	;re-enable interrupts
	cli

	;update shadow registers
	mva		sdlsth	dlisth
	mva		sdlstl	dlistl
	mva		sdmctl	dmactl
	mva		chbas	chbase
	mva		chact	chactl
	mva		gprior	prior
	
	ldx		#8
	stx		consol				;sneak in speaker reset while we have an 8
ColorLoop
	lda		pcolr0,x
	eor		colrsh
	and		drkmsk
	sta		colpm0,x
	dex
	bpl		ColorLoop

	;decrement timer 2 and check for underflow
	ldx		#3
	jsr		VBIDecrementTimer
	sne:jsr	(cdtma2-3,x)

	;Decrement timers 3-5 and set flags
	;
	;[OS Manual] Appendix L, page 254 says that the OS never modifies CDTMF3-5
	;except to set them to zero on timeout at init. This is a LIE. It also sets
	;the flags to $FF when they are running. It does not write the flags when
	;the timer is idle. Spider Quake depends on this.
	;
	ldx		#9
timer_n_loop:
	clc
	jsr		VBIDecrementTimer
	bcs		timer_n_not_running
	seq:lda	#$ff
timer_n_not_expired:
	sta		cdtmf3-5,x
timer_n_not_running:
	dex
	dex
	cpx		#5
	bcs		timer_n_loop
	
	;Read POKEY keyboard register and handle auto-repeat
	lda		skstat				;get key status
	and		#$04				;check if key is down
	bne		no_repeat_key		;skip if not
	dec		srtimr				;decrement repeat timer
	bne		no_repeat			;skip if not time to repeat yet
	mva		kbcode ch			;repeat last key

	mva		keyrep srtimr		;reset repeat timer

	bne		no_keydel			;skip debounce counter decrement

no_repeat_key:
	stz		srtimr
no_repeat:
	;decrement keyboard debounce counter
	lda		keydel
	seq:dec	keydel
no_keydel:

	;Update controller shadows.
	;
	;The PORTA/PORTB decoding is a bit complex:
	;
	;	bits 0-3 -> STICK0/4 (and no, we cannot leave junk in the high bits)
	;	bits 4-7 -> STICK1/5
	;	bit 2    -> PTRIG0/4
	;	bit 3    -> PTRIG1/5
	;	bit 6    -> PTRIG2/6
	;	bit 7    -> PTRIG3/7
	;
	;XL/XE machines only have two joystick ports, so the results of ports 0-1
	;are mapped onto ports 2-3.
	;
	
	lda		porta
	tax
	and		#$0f
	sta		stick0
	sta		stick2	
	txa
	lsr						;shr1
	lsr						;shr2
	tax
	lsr						;shr3
	lsr						;shr4
	sta		stick1
	sta		stick3
	lsr						;shr5
	lsr						;shr6
	tay
	and		#$01
	sta		ptrig2
	tya
	lsr
	sta		ptrig3
	txa
	and		#$01
	sta		ptrig0
	txa
	lsr
	and		#$01
	sta		ptrig1

	ldx		#3
pot_loop:
	lda		pot0,x
	sta		paddl0,x
	sta		paddl4,x
	lda		ptrig0,x
	sta		ptrig4,x
	dex
	bpl		pot_loop

	ldx		#1
port_loop:
	lda		trig0,x
	sta		strig0,x
	sta		strig2,x
	dex
	bpl		port_loop
	
	;restart pots (required for SysInfo)
	sta		potgo
	
	;update light pen
	mva		penh lpenh
	mva		penv lpenv
	
	jmp		(vvblkd)	;jump through vblank deferred vector
.endp

;==========================================================================
; VBIDecrementTimer
;
; Entry:
;	X = timer index *2+1 (1-9)
;
; Exit:
;	C=1,      Z=0, A!0		timer not running
;	C=0/same, Z=1, A=0		timer just expired
;	C=0/same, Z=0, A=?		timer still running
;
.proc VBIDecrementTimer
	;check low byte
	lda		cdtmv1-1,x
	bne		lononzero
	
	;check high byte; set C=1/Z=1 if zero, C=0/Z=0 otherwise
	cmp		cdtmv1,x
	bne		lozero_hinonzero
	
	;both bytes are zero, so timer's not running
	txa
	rts
	
lozero_hinonzero:
	;decrement high byte
	dec		cdtmv1,x

lononzero:
	;decrement low byte
	dec		cdtmv1-1,x
	bne		still_running
	
	;return high byte to set Z appropriately
	lda		cdtmv1,x
still_running:
	rts
.endp

;==========================================================================
; VBISetVector - set vertical blank vector or counter
;
; This is a drop-in replacement for SETVBV.
;
; A = item to update
;	1-5	timer 1-5 counter value
;	6	VVBLKI
;	7	VVBLKD
; X = MSB
; Y = LSB
;
.proc VBISetVector
	;A = item to update
	;	1-5	timer 1-5 counter value
	;	6	VVBLKI
	;	7	VVBLKD
	;X = MSB
	;Y = LSB
	;
	;NOTE:
	;The Atari OS Manual says that DLIs will be disabled after SETVBV is called.
	;This is a lie -- neither the OS-B nor XL kernels do this, and the Bewesoft
	;8-players demo depends on it being left enabled.
	;
	;IRQ mask state must be saved across this proc. DOSDISKA.ATR breaks if IRQs
	;are unmasked.
	
	asl
	sta		intemp
	php
	sei
	tya
	ldy		intemp
	
	;We're relying on a rather tight window here. We can't touch NMIEN, so we have
	;to wing it with DLIs enabled. Problem is, in certain conditions we can be under
	;very tight timing constraints. In order to do this safely we have to finish
	;before a DLI can execute. The worst case is a wide mode 2 line at the end of
	;a vertically scrolled region with P/M graphics enabled and an LMS on the next
	;mode line. In that case we only have 7 cycles before we hit the P/M graphics
	;and another two cycles after that until the DLI fires. The exact cycle timing
	;looks like this:
	;
	;*		inc wsync
	;ANTIC halts CPU until cycle 105
	;105	playfield DMA
	;106	refresh DMA
	;107	sta abs,y (1/5)
	;108	sta abs,y (2/5)
	;109	sta abs,y (3/5)
	;110	sta abs,y (4/5)
	;111	sta abs,y (5/5)
	;112	txa (1/2)
	;113	txa (2/2)
	;0		missiles
	;1		display list
	;2		player 0
	;3		player 1
	;4		player 2
	;5		player 3
	;6		display list address low
	;7		display list address high
	;8		sta abs,y (1/5)
	;9		sta abs,y (2/5)
	;10		sta abs,y (3/5)
	;11		sta abs,y (4/5)
	;12		sta abs,y (5/5)
	;
	;We rely on the 6502 not being able to service interrupts until the end of an
	;instruction for this to work. The INC WSYNC is necessary to combat the case
	;where the NMI is triggered across the WSYNC wait; without it, the VBI could
	;fire immediately after the first STA.
	
	inc		wsync
	sta		cdtmv1-2,y
	txa
	sta		cdtmv1-1,y
	plp
	rts
.endp
