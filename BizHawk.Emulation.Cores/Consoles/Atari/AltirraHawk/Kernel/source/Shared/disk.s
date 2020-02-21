;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Disk Routines
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.proc DiskInit
	.if _KERNEL_XLXE
	;set disk sector size to 128 bytes
	mwa		#$80	dsctln
	.endif
	rts
.endp

;==========================================================================
; Disk handler routine (pointed to by DSKINV)
;
; Exit:
;	A = command byte (undocumented; required by Pooyan)
;	Y = status
;	N = 1 if error, 0 if success (high bit of Y)
;	C = 1 if command is >=$21 (undocumented; required by Arcade Machine)
;
.proc DiskHandler
	mva		#$31	ddevic
	mva		#$0f	dtimlo
	
	;check for status command
	lda		dcomnd
	sta		ccomnd
	cmp		#$53
	bne		notStatus

	lda		#<dvstat
	sta		dbuflo
	lda		#>dvstat
	sta		dbufhi
	asl							;hack to save a byte to get $04 since >dvstat is $02
	sta		dbytlo
	lda		#0
	sta		dbythi
	
	jsr		do_read
	bmi		xit
	
	;update format timeout
	mvx		dvstat+2 dsktim
	tax
xit:
	rts
	
notStatus:

	;set disk sector length
	.if _KERNEL_XLXE
	mwy		dsctln	dbytlo
	.else
	mwy		#$80 dbytlo
	.endif
	
	;check for put/write
	.if _KERNEL_XLXE
	cmp		#$50
	beq		do_write
	.endif
	cmp		#$57
	beq		do_write
	
	;check for format, or else assume it's a read command ($52) or similar
	cmp		#$21
	bne		do_read

	;it's format... use the format timeout
	mva		dsktim dtimlo

do_read:
	lda		#$40
do_io:
	sta		dstats
	jsr		siov
	
	;load disk command back into A (required by Pooyan)
	;emulate compare against format command (required by Arcade Machine)
	;sort-of emulate compare against status (required by Micropainter)
	lda		dcomnd
	cpy		#0					;!! Atari800WinPlus's SIO patch doesn't set STATUS
	sec
	rts

do_write:
	lda		#$80
	bne		do_io
.endp
