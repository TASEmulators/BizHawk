; Altirra BASIC - Variables module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;==========================================================================
; Input:
;	A = index of variable to look up ($00-7F or $80-FF)
;	X = ZP variable to store result in
;	Y = offset to variable pointer (0-7)
;
; Output:
;	varptr = address of variable
.proc VarGetAddr0
		;;##TRACE "Looking up variable: $%02x" a|$80
		asl					;!! ignore bit 7 of variable index
		asl
		rol		varptr+1
		asl
		rol		varptr+1
		clc
		adc		vvtp
		sta		varptr
		lda		varptr+1
		and		#$03
		adc		vvtp+1
		sta		varptr+1
		;##ASSERT ((dw(varptr)-dw(vvtp))&7)=0
		;##ASSERT db(dw(varptr)+1)=(dw(varptr)-dw(vvtp))/8
		;;##TRACE "varptr=$%04x" dw(varptr)
		rts
.endp

;==========================================================================
.proc VarLoadFR0
		ldy		#2
.def :VarLoadFR0_OffsetY = *
		:5 mva (varptr),y+ fr0+#
		mva (varptr),y fr0+5
		rts
.endp

;==========================================================================
.proc VarAdvanceName
		ldy		#0
skip_loop:
		lda		(iterPtr),y
		iny
		tax
		bpl		skip_loop
		tya
		ldx		#iterPtr
.def :VarAdvancePtrX = *
		clc
		adc		0,x
		sta		0,x
		scc:inc	1,x
		rts
.endp
