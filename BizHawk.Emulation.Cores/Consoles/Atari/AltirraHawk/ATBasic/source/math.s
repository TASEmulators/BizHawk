; Altirra BASIC - Misc math module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;===========================================================================
;FCOMP		Floating point compare routine.
;
; Inputs:
;	FR0
;	FR1
;
; Outputs:
;	Z, C set for comparison result like SBC
;
.proc fcomp
		;check for sign difference
		lda		fr1
		eor		fr0
		bpl		signs_same

		;Signs are different. If FR0 is positive, then we need to
		;exit Z=0, C=1; if FR0 is negative, then Z=0, C=0.
		;
		;We're using a dirty trick here by skipping the ROR below.
		;A=FR0^FR1, so after the EOR FR0, A=FR1. This causes us to
		;set C=0 for -FR0,+FR1 and C=1 for +FR0,-FR1, which is what
		;we want.
		;
		dta		{bit $00}

		;okay, we've confirmed that the numbers are different, but the
		;carry flag may be going the wrong way if the numbers are
		;negative... so let's fix that.
diff:
		ror					;!! - skipped for differing sign path
		eor		fr0
		sec
		rol
xit:
		rts
		
signs_same:
		;Check for both values being zero, as only signexp and first
		;mantissa byte are guaranteed to be $00 in that case.
		;
		;We are using another trick here by testing:
		;
		;	(x ^ y) | x == 0
		;
		;in lieu of x|y. This works out at the boolean level.
		;
		ora		fr0
		beq		xit
		
		;compare signexp and mantissa bytes in order
		ldx		#<-6
loop:
		lda		fr0+6,x
		cmp		fr1+6,x
		bne		diff
		inx
		bne		loop
		rts					;!! - Z=1, C=1
		
.endp

;===========================================================================
.proc	MathFloor
		;These are the digits we need to check+zero by exponent:
		;
		;	$3F 00 00 00 00 00 -> always fraction
		;	$40 xx 00 00 00 00
		;	$41	xx xx 00 00 00
		;	$42 xx xx xx 00 00
		;	$43 xx xx xx xx 00
		;	$44 xx xx xx xx xx -> always integer, take no action
		;
		lda		fr0
		asl
		
		;if exponent is < $40 then we have zero or -1
		bmi		not_tiny
		php
		jsr		zfr0
		plp
		bcs		round_down
done:
		rts
		
not_tiny:
		;ok... using the exponent, compute the first digit offset we should
		;check
		lsr
		adc		#$bc		;!! - C=0
		bcs		done		;exit if exp too large and we can't have decimals
		tax
		
		;check digit pairs until we find a non-zero fractional digit pair,
		;zeroing as we go
		lda		#0
		tay
zero_loop:
		ora		fr0+6,x
		sty		fr0+6,x
		inx
		bne		zero_loop
		
		;skip rounding if it was already integral
		tay
		beq		done

		;check if we have a negative number; if so, we need to subtract one
		lda		fr0
		bpl		done
		
round_down:
		;subtract one to round down
		jsr		MathLoadOneFR1
		jmp		fsub
		
.endp

;===========================================================================
; Extract sign from FR0 into funScratch1 and take abs(FR0).
;
.proc MathSplitSign
		lda		fr0
		sta		funScratch1
		and		#$7f
		sta		fr0
xit:
		rts
.endp

;===========================================================================
.proc MathByteToFP
		ldx		#0
.def :MathWordToFP = *
		stx		fr0+1
.def :MathWordToFP_FR0Hi_A = *
		sta		fr0
		jmp		ifp
.endp

;===========================================================================
.proc MathLoadOneFR1
		ldx		#<const_one
.def :MathLoadConstFR1 = *
		ldy		#>const_one
		bne		MathLoadFR1_FPSCR.fld1r_trampoline
.endp

;===========================================================================
.proc MathStoreFR0_FPSCR
		ldx		#<fpscr
.def :MathStoreFR0_Page5 = *
		ldy		#>fpscr
		jmp		fst0r
.endp

;===========================================================================
.proc MathLoadFR1_FPSCR
		ldx		#<fpscr
.def :MathLoadFR1_Page5 = *
		ldy		#>fpscr
fld1r_trampoline:
		jmp		fld1r
.endp
