; Altirra BASIC - Utility module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;==========================================================================
; Unsigned 16x16 -> 16 multiply.
;
; Inputs:
;	Multiplicand: FR0+3, FR0+2
;	Multiplier:   FR0+5, FR0+4
;
; Output:
;	16-bit result in [FR0+1, FR0+0]
;	High byte in A
;	C = 0 if OK
;	C = 1 if overflow
;
; Altered:
;	A, X, FR0+2, FR0+3
;
.proc	umul16x16
		;##TRACE "umul16x16(%u,%u)" dw(fr0) dw(fr1)
		lda		#0
		sta		fr0+0
		ldx		#16
bitloop:
		;shift result left
		asl		fr0+0
		rol
		bcs		overflow
		
		;stash result hi
		tay
		
		;shift multiplicand left
		asl		fr0+2
		rol		fr0+3
		bcc		no_add
		
		;add multiplier to result
		clc
		lda		fr0
		adc		fr0+4
		sta		fr0
		tya
		adc		fr0+5
		bcs		overflow
no_add:
		dex
		bne		bitloop
		sta		fr0+1
overflow:
		;##TRACE "umul16x16 result %u" dw(fr0)
		rts
		
.endp

;==========================================================================
; Unsigned 16-bit x #6 multiply.
;
; This routine relies on the result not overflowing. We can assume this
; due to checks in the array allocate routines.
;
; Inputs:
;	Multiplicand: FR0+1, FR0
;
; Output:
;	16-bit result in [FR0+1, FR0]
;	High byte in A
;	C = 0
;
.proc umul16_6
		lda		fr0
		ldy		fr0+1
		asl
		rol		fr0+1
		adc		fr0
		sta		fr0
		tya
		adc		fr0+1
		asl		fr0
		rol
		sta		fr0+1
		rts
.endp
