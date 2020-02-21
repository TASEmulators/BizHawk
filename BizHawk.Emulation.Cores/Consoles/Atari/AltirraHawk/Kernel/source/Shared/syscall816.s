;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 system call routines
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
; System COP #0 handler (816os API)
;
; Calls from native mode into emulation mode.
;
; Input:
;	(1,S).w = routine to call in emulation mode
;	A/X/Y = inputs to routine
;
; Output:
;	A/X/Y = output from routine
;	P.NVDIZC = flags from routine
;
; Preserved:
;	(1,s).w
;
.proc IntNativeCop0Handler
		;The stack frame on entry looks like this:
		;
		; 12,S	emulation routine hi
		; 11,S	emulation routine lo
		; 10,S	return address bank
		; 9,S	return address hi
		; 8,S	return address lo
		; 7,S	return flags	
		; 6,S	DH
		; 5,S	DL
		; 4,S	DBR
		; 3,S	internal return address bank
		; 2,S	internal return address hi
		; 1,S	internal return address lo

		;switch to emulation mode
		sec
		xce

		;call the routine
		pea		#xit-1
		pea		#0
		pha
		lda		16,s
		sta		2,s
		lda		17,s
		sta		3,s

		lda		12,s
		pha
		plp
		pla

		php
		rti

xit:
		;merge flags
		pha
		php
		pla
		eor		8,s
		and		#$cf
		eor		8,s
		sta		8,s
		pla

		;switch back to native mode
		clc
		xce

		;all done
		rtl
.endp

;==========================================================================
.proc IntNativeCopUHandler
		;switch to 16-bit registers
		rep		#$30

		;load function code
		lda		1,s
		cmp		#3

kpsize:
		lda.w	#$0100
		ldy.w	#1
		rtl

kmalloc:
		lda.w	#0
		ldy.w	#1
		rtl

kfree:
		rtl
.endp

;==========================================================================
.proc IntNativeCopCHandler
		rtl
.endp
