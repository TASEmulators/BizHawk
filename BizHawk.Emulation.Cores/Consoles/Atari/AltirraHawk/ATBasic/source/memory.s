; Altirra BASIC - Memory handling module
; Copyright (C) 2014-2016 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;==========================================================================
; Input:
;	Y:A		Total bytes required
;	X		ZP offset of first pointer to offset
;	A0		Insertion point
;
; Errors:
;	Error 2 if out of memory (yes, this may invoke TRAP!)
;
.proc expandTable
		;##TRACE "Expanding table: $%04x bytes required, table offset $%02x (%y:%y) [$%04x:$%04x], insert pt=$%04x" y*256+a x x-2 x dw(x-2) dw(x) dw(a0)
		sta		a2
		sty		a2+1

		txa
		pha

		;compute number of bytes to copy
		;##ASSERT dw(a0) <= dw(memtop2)
		sec
		sbw		memtop2 a0 a3
		
		;top of src = memtop2		
		;top of dst = memtop2 + N
		clc
		lda		memtop2
		sta		a1
		adc		a2
		tay
		lda		memtop2+1
		sta		a1+1
		adc		a2+1

		;check if we're going to go above MEMTOP with the copy and throw
		;error 2 if so; note that we are deliberately off by one here to
		;match FRE(0)
		jsr		MemCheckAddrAY
		jsr		copyDescendingDstAY
		
		pla
		tax
.def :MemAdjustTablePtrs = *

		ldy		#a2
offset_loop:
		jsr		IntAdd
		inx
		inx
		cpx		#memtop2+2
		bne		offset_loop

.def :MemAdjustAPPMHI = *			;NOTE: Must not modify X or CLR/NEW will break.
		;update OS APPMHI from our memory top
		sta		appmhi+1
		lda		memtop2
		sta		appmhi

nothing_to_do:
xit:
		rts
.endp

;==========================================================================
.proc MemCheckAddrAY
		bcs		out_of_memory		;dst+N > $FFFF => obviously out of memory

		;out of memory if (dst+N) > memtop
		cmp		memtop+1
		bne		test_byte
		cpy		memtop
		beq		expandTable.xit
test_byte:
		bcc		expandTable.xit
out_of_memory:
		jmp		errorNoMemory
.endp

;==========================================================================
; Input:
;	A1	end of source range
;	A0	end of destination range
;	A3	bytes to copy
;
; Modified:
;	A0, A1
;
; Preserved:
;	A2
.proc copyDescendingDstAY
		sty		a0
		sta		a0+1
.def :copyDescending
		;##TRACE "Copy descending src=$%04x-$%04x, dst=$%04x-$%04x (len=$%04x)" dw(a0)-dw(a3) dw(a0) dw(a1)-dw(a3) dw(a1) dw(a3)
		;##ASSERT dw(a3) <= dw(a0) and dw(a3) <= dw(a1)
		ldy		#0

		;check if we have any whole pages to copy
		ldx		a3+1
		inx
		bne		loop_entry		;!! - unconditional

loop:
		dey
		lda		(a1),y
		sta		(a0),y
		tya
		bne		loop
loop_entry:
		dec		a0+1
		dec		a1+1
		dex
		bne		loop

		ldx		a3
		beq		leftovers_done
leftover_loop:
		dey
		lda		(a1),y
		sta		(a0),y
		dex
		bne		leftover_loop
leftovers_done:
		rts
.endp

;==========================================================================
.proc IntAddToFR0
		ldx		#fr0
.def :IntAdd = *
		lda		0,x
		add		0,y
		sta		0,x
		lda		1,x
		adc		1,y
		sta		1,x
		rts
.endp
