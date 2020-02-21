;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Miscellaneous data
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
;Used by PBI and display/editor.
;
.proc ReversedBitMasks
		dta		$80,$40,$20,$10,$08,$04,$02,$01
.endp

;==========================================================================
;Used by CIO devices
.proc CIOExitSuccess
		ldy		#1
exit_not_supported:
		rts
.endp

CIOExitNotSupported = CIOExitSuccess.exit_not_supported

;==========================================================================
; Sound a bell using the console speaker.
;
; Entry:
;	Y = duration
;
; Modified:
;	X
;
; Preserved:
;	A
;
.proc Bell
	pha
	lda		#$08
soundloop:
	ldx		#4
	pha
delay:
	lda		vcount
	cmp:req	vcount
	dex
	bne		delay
	pla
	eor		#$08
	sta		consol
	bne		soundloop
	dey
	bne		soundloop
	pla
	rts
.endp