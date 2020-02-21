;	Altirra - Atari 800/800XL/5200 emulator
;	Executable loader - high speed version
;	Copyright (C) 2008-2015 Avery Lee
;
;	This program is free software; you can redistribute it and/or modify
;	it under the terms of the GNU General Public License as published by
;	the Free Software Foundation; either version 2 of the License, or
;	(at your option) any later version.
;
;	This program is distributed in the hope that it will be useful,
;	but WITHOUT ANY WARRANTY; without even the implied warranty of
;	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;	GNU General Public License for more details.
;
;	You should have received a copy of the GNU General Public License
;	along with this program; if not, write to the Free Software
;	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'sio.inc'

runad	equ		$02e0
initad	equ		$02e2
setvbv	equ		$e45c

		org		BASEADDR
		opt		h-f+

EXELOADER_DEVICE_ID = $FE
EXELOADER_COMMAND = $52

;==========================================================================

base:
		dta		0
		dta		4
		dta		a(BASEADDR)
		dta		a($e4c0)
read_loop:
		ldx		#7
		jsr		do_read

		lda		param_block2+2
		beq		doRun

		mwa		#dummy initad
		ldx		#15
		jsr		do_read

		jsr		doinit

		jmp		read_loop

do_read:
		ldy		#7
copy_loop:
		lda		param_block,x
		sta		dbuflo,y
		dex
		dey
		bpl		copy_loop

		jsr		SioHighSpeed

		inc		param_block + 6
dummy:
		rts

doInit:
		jmp		(initad)

doRun:
		jmp		(runad)

param_block:
		dta		a(param_block2), 8, 0, 8, 0, 0, 0
param_block2:
		.ds		8

;==========================================================================

.proc SioHighSpeed
		cld
		lda		#0
		sta		bufrlo
	
		;Enter critical section.
		;
		;We need to mask CPU interrupts so we can poll IRQST, but we also need to
		;set CRITIC. The reason is that the VBI routine is faster at aborting
		;stage 2 processing by CRITIC, since it checks that first and it takes
		;more time to check the I flag on the stack.
		sei
		sta		critic

		;set up timer 1 timeout
		mwa		#SioTimeoutCallback cdtma1
	
retry_command:
		;assert command line
		mva		#$34 pbctl

		;wait 750us-1.6ms (~12-25 scanlines)
		lda		dbytlo
		eor		#$ff
		tay
		iny
		sty		bfenlo
		lda		dbuflo
		sec
		sbc		bfenlo
		sta		bufrlo
		lda		dbufhi
		sbc		#0
		sta		bufrhi
		lda		bfenhi
		ldx		dbythi
		tya
		seq:inx
		stx		bfenhi

		;set up POKEY
		mva		#$28 audctl
		mva		#$a0 audc3
		ldx		soundr
		seq:lda	#$a8
		sta		audc4

		;shift to 57600 baud
		mva		hispeed_index audf3
		mva		#$00 audf4

		jsr		SioSendSetup
	
		;send command
		lda		#EXELOADER_DEVICE_ID
		sta		chksum
		sta		serout
		lda		#EXELOADER_COMMAND
		jsr		SioSendByteUpChk
		lda		daux1
		jsr		SioSendByteUpChk
		lda		daux2
		jsr		SioSendByteUpChk
		lda		chksum
		jsr		SioSendByte

		;wait 650us-950us (~11-15 scanlines)
		;wait for transmit to begin
		ldx		#$ff
		cpx:req	irqst

		;wait for transmit to complete
		mva		#$08 irqen
		cpx:req	irqst

		jsr		SioReceiveSetup
	
		;set timeout for two vblanks
		lda		#$01
		ldx		#$00
		stx		stackp
		ldy		#$04
		jsr		setvbv
		lda		#$ff
		sta		timflg		;MUST be after SETVBV
	
		;deassert command line -- the drive will send the ACK right after this,
		;so we need to be ready
		mva		#$3c pbctl
	
		;wait for command ACK
		jsr		SioReceiveByte
		cmp		#$41
		beq		command_ok
	
command_error:
		jmp		retry_command
	
command_ok:	

		;set timeout for 224 vblanks
		lda		#$01
		ldx		#$00
		stx		chksum			;sneak this in while we have time and we have a zero
		ldy		#$e0
		sty		stackp
		jsr		setvbv
		lda		#$ff
		sta		timflg			;MUST be after SETVBV
		
		;Wait for command result.
		;
		;This can take a while, but we have to be ready to receive data bytes ASAP
		;once this arrives. There is no timing requirement for complete-to-data in
		;the SIO specification and some drives send data immediately.
		jsr		SioReceiveByte
	
		;stash byte and check if it was Complete or Error; we can't handle it now
		;because we have to receive the data frame on an error
		sta		status
		cmp		#$43
		beq		opstatus_valid
		cmp		#$45
		beq		opstatus_valid
	
		;bogus returned status... see if we have retries
device_error:
operation_error:
		jmp		retry_command
	
opstatus_valid:	
		;This receive loop is the most critical part of the routine. We're
		;assuming that we may have to deal with both a critical mode VBI
		;routine and a mode 2 normal width screen. Here are our deadlines:
		;
		;	Mode					Divisor		Cycles per byte (ideal)
		;	Fastest possible		$00			140
		;	Indus GT Synchromesh	$06			260
		;	Speedy 1050				$09			320
		;	Happy / US Doubler		$0A			340
		;	XF551					$10			460
		;	Standard SIO			$28			940
		;
		;The VBI routine executes in about 161 cycles with CRITIC enabled, which
		;makes Synchromesh a bit dicey. The good news is that if we're hitting it,
		;we're in the blank region of the screen which means we only have some
		;refresh cycles besides the VBI. The bad news is that there's no possible
		;way we can hit divisor 0 with the VBI routine enabled.
		;
		;The first scanline of a mode 2 line takes 80 solid cycles + 1-3 DL cycles,
		;and is surrounded by other scanlines that take 40 cycles. This is more
		;relaxed than the VBI case and so we're OK here.
		;
		;Timing:
		; 37 cycles (<=256 bytes, no wait)
		; 49 cycles (>256 bytes, no wait)

		ldx		#$df
		ldy		bfenlo
		clc
		bcc		receive_loop			;!! - unconditional

receive_wait:
		bit		timflg					;4
		bmi		receive_wait_loop		;2+1
		jmp		SioReceiveByte.timeout
	
receive_loop:
		;wait for serial input ready IRQ to active
		lda		#$20					;2
receive_wait_loop:
		bit		irqst					;4
		bne		receive_wait			;2+1
	
		;reset SERIN IRQ and read byte
		stx		irqen					;4
		sta		irqen					;4
		lda		serin					;4
		sta		(bufrlo),y				;6
		adc		chksum					;3
		sta		chksum					;3
		iny								;2
		bne		receive_loop			;2+1
		inc		bufrhi					;5
		dec		bfenhi					;5
		bne		receive_loop			;2+1
	
		adc		#0
		sta		chksum

		;receive and compare checksum
		jsr		SioReceiveByte
		cmp		chksum
		bne		device_error

receive_done:
		;check if we had a device error
		lda		status
		cmp		#$43
		bne		device_error

		;all done!
		lda		#0
		sta		cdtmv1
		sta		audc4
		sta		irqen
		sta		critic
		lda		pokmsk
		sta		irqen

		cli
		rts
.endp

;==========================================================================
.proc SioReceiveByte
		;wait for serial input ready IRQ to active
		ldx		#$df				;2
wait_loop:
		cpx		irqst				;4
		bcc		waiting				;2+1
	
		;read the new data byte
		lda		serin				;4
	
		;reset SERIN IRQ
		stx		irqen				;4
		ldx		#$20				;2
		stx		irqen				;4
		rts							;6
	
waiting:
		lda		timflg
		bmi		wait_loop
		pla
		pla
timeout:	
		jmp		SioHighSpeed.retry_command
.endp

;==========================================================================
.proc SioReceiveSetup
		;reset serial hardware
		lda		#$00
		sta		skctl
		sta		skres
	
		;clear interrupts
		sta		irqen
	
		;set asynchronous receive mode, channel 4
		mva		#$13 skctl
	
		;enable serial input ready IRQ
		mva		#$20 irqen
		rts
.endp

;==========================================================================
.proc SioSendSetup
		;reset serial hardware
		lda		#$00
		sta		skctl
		sta		skres
	
		;clear interrupts
		sta		irqen
	
		;set transmit mode, channel 4
		mva		#$23 skctl
	
		;enable serial output ready IRQ
		mva		#$10 irqen
		rts
.endp

;==========================================================================
SioSendByte = SioSendByteUpChk.no_chk_entry
.proc SioSendByteUpChk
		tax
		add		chksum
		adc		#0
		sta		chksum
		txa
no_chk_entry:

		;wait for serial output ready IRQ to activate
		ldx		#$ef
		cpx:rcc	irqst
	
		;reset SEROR IRQ
		stx		irqen
		ldx		#$10
		stx		irqen
	
		;write the new data byte
		sta		serout
		rts
.endp

;==========================================================================
.proc SioTimeoutCallback
		lsr		timflg
		rts
.endp

;==========================================================================

		.echo	"End of high-speed EXE loader: ",*

		.if * > BASEADDR+$200
		.error	"Loader too long: ",*
		.endif

		org		BASEADDR+$1FF
hispeed_index:
		dta		0
