;	Altirra - Atari 800/800XL/5200 emulator
;	Rapidus emulator bootstrap firmware - 6502 PBI device
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; Rapidus 6502 PBI bootstrap firmware
;
; The Rapidus accelerator uses a PBI device to intercept the boot process
; on the 6502 and initialize the 65C816. It's job is to decompress the
; FPGA core stream from the on-board flash and upload it to the FPGA, then
; finally turn off the 6502 and boot the 65C816. The Shift key can be held
; on boot to suppress this and continue booting on the 6502.
;
; Since we don't have a real FPGA core in emulation, this firmware simply
; does a dummy core load to set the initialized bit and then turns on
; the '816. This means that this bootstrap must NOT be used on real
; hardware.
;

		icl		'kerneldb.inc'
		icl		'hardware.inc'

		org		$D800
		opt		h-f+

		dta		a(0)			;checksum (unused)
		dta		0				;revision (unused)
		dta		$80				;ID byte
		dta		0				;device type (unused)
		jmp		SIOHandler		;SIO vector
		jmp		IRQHandler		;IRQ vector
		dta		$91				;ID byte
		dta		0				;device name (unused)
		dta		a(CIOOpen)
		dta		a(CIOClose)
		dta		a(CIOGetByte)
		dta		a(CIOPutByte)
		dta		a(CIOGetStatus)
		dta		a(CIOSpecial)
		jmp		Init

;==========================================================================
.proc Init
		;kill NMIs (IRQs are already disabled)
		mva		#0 nmien

		;check if core has been loaded
		lda		$D191
		bmi		core_loaded
		jsr		LoadCore
core_loaded:

		;wait for vertical blank
		jsr		WaitVBL

		;set up display
		mwa		#dlist dlistl
		mva		#$22 dmactl
		mva		#$e0 chbase
		mva		#0 colbk
		sta		colpf2
		mva		#$0a colpf1

		;wait 120 vblanks while checking if the shift key is held to bypass '816 mode
		ldy		#90
		ldx		#0
wait_loop:
		jsr		WaitVBL

		lda		skstat
		and		#$08
		sne:ldx	#$ff

		dey
		bne		wait_loop

		;kill display
		sty		dmactl

		txa
		beq		go16

		;exit and continue 6502 boot
		rts

go16:
		;restart in 65C816 mode
		lda		#$00
		sta		$D191
		jmp		*
.endp

;==========================================================================
.proc WaitVBL
		lda		#124
		cmp:req	vcount
		cmp:rne	vcount
		rts
.endp

;==========================================================================
.proc LoadCore
		;fake the core load
		lda		#$41
		sta		$D191			;select FPGA
		sta		$D192			;fake load FPGA data
		lda		#$40
		sta		$D191			;deselect FPGA
		rts
.endp

;==========================================================================
SIOHandler:
IRQHandler:
CIOOpen:
CIOClose:
CIOGetByte:
CIOPutByte:
CIOGetStatus:
CIOSpecial:
		rts

;==========================================================================

		org		$DF00

dlist:
		:10 dta $70
		dta		$42,a(pf1)
		dta		$70
		dta		$02
		dta		$02
		dta		$41,a(dlist)

pf1:
		;		 0123456789012345678901234567890123456789
		dta		"      Altirra bootstrap for Rapidus     "
		dta		"    Hold Shift to bypass 65C816 boot    "
		dta		"       Hold Inverse to enter menu       "

		.if * > $E000
		.error "PBI ROM too long: ", *
		.endif

		.align	$800,$FF
