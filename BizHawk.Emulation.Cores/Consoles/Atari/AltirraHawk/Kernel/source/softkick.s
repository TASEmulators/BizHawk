;	Altirra - Atari 800/800XL/5200 emulator
;	LLE kernel soft loader
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

	icl		'hardware.inc'
	icl		'kerneldb.inc'

;==========================================================================

		opt		h-o-
		org		$80
copysrc	dta		a(0)
copydst	dta		a(0)

		opt		h+o+
	
		org		$3000

;==========================================================================
.proc	main
	;kill NMIs and DMA
	sei
	ldx		#0
	stx		nmien
	stx		dmactl
	
	;turn off kernel ROM
	lda		#$38
	sta		pbctl			;switch to DDRB
	dex
	stx		portb			;switch port B to all outputs
	lda		#$3c
	sta		pbctl			;switch to IORB
	dex
	stx		portb			;turn off self-test, BASIC, and kernel ROMs

	;copy $4000-4FFF -> $C000-CFFF
	lda		#$c0
	ldy		#$40
	ldx		#$10
	jsr		CopyMemory
	
	;copy $5800-7FFF -> $D800-FFFF
	lda		#$d8
	ldy		#$58
	ldx		#$28
	jsr		CopyMemory
	
	;stomp PUPBT1 to force cold reset
	lda		#0
	sta		pupbt1
	
	;jump to reset vector
	jmp		($fffc)
.endp

;==========================================================================
; Entry:
;	A = dest page
;	Y = src page
;	X = pages to copy
;
.proc	CopyMemory
	sty		copysrc+1
	sta		copydst+1
	ldy		#0
	sty		copysrc
	sty		copydst
copyloop:
	mva:rne	(copysrc),y (copydst),y+
	inc		copysrc+1
	inc		copydst+1
	dex
	bne		copyloop
	rts
.endp

;==========================================================================
	org		$4000
	ins		'kernelxlsoft.bin'

	run		main
