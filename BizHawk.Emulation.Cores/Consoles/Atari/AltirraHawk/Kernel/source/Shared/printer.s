;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Printer Handler
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
.proc PrinterInit
	;set printer timeout to default
	mva		#30 ptimot
	rts
.endp

;==========================================================================
.proc PrinterOpen
	;check for sideways mode and compute line size
	ldx		#40
	lda		icax2z
	cmp		#$53
	sne:ldx	#29
	stx		pbufsz

	lda		#0
	sta		pbpnt

xit:
	ldy		#1
	rts
.endp

;==========================================================================
PrinterClose = _PrinterPutByte.close_entry
PrinterPutByte = _PrinterPutByte.put_entry

.proc _PrinterPutByte
close_entry:
	;check if we have anything in the buffer
	lda		pbpnt
	
	;exit if buffer is empty
	beq		PrinterOpen.xit
	
	;fall through to put with EOL
	lda		#$9b

put_entry:
	;preload buffer index (useful later)
	ldx		pbpnt
	
	;check for EOL
	cmp		#$9b
	beq		do_eol
	
	;mask off MSB
	and		#$7f
	
	;put char and advance
	sta		prnbuf,x
	inx
	stx		pbpnt
	
	;check for end of line and exit if not
	cpx		pbufsz
	bcc		PrinterOpen.xit
	
	;fall through to EOL

do_eol:
	;fill remainder of buffer with spaces
	lda		#$20
fill_loop:
	cpx		pbufsz
	bcs		fill_done
	sta		prnbuf,x
	inx
	bcc		fill_loop
fill_done:
	
	;send line to printer
	ldy		#10
	mva:rne	iocbdat-1,y ddevic-1,y-

	;empty buffer
	sty		pbpnt

	;set line length
	stx		dbytlo

	;Compute AUX1 byte from length.
	;
	;Note that the OS manual is wrong -- this byte needs to go into AUX1 and
	;not AUX2 as the manual says.
	;
	;	normal   (40): 00101000 -> 01001110 ($4E 'N')
	;	sideways (29): 00011101 -> 01010011 ($53 'S')
	;	               010_1I1_
	txa
	and		#%00010101
	eor		#%01001110
	sta		daux1			;set AUX1 to indicate width to device
	
	;send to printer and exit
do_io:
	mva		ptimot dtimlo
	jmp		siov

iocbdat:
	dta		$40			;device
	dta		$01			;unit
	dta		$57			;command 'W'
	dta		$80			;input/output mode (write)
	dta		a(prnbuf)	;buffer address
	dta		a(0)		;timeout
	dta		a(0)		;buffer length
.endp

;==============================================================================
.proc PrinterGetStatus
	;setup parameter block
	ldx		#9
	mva:rpl	iocbdat,x ddevic,x-

	;issue status call
	jsr		_PrinterPutByte.do_io
	bmi		error
	
	;update timeout
	mva		dvstat+2 ptimot
	
error:
	rts

iocbdat:
	dta		$40			;device
	dta		$01			;unit
	dta		$53			;command 'S'
	dta		$40			;input/output mode (read)
	dta		a(dvstat)	;buffer address
	dta		a(0)		;timeout
	dta		a(4)		;buffer length
.endp

;==============================================================================
PrinterGetByte = CIOExitNotSupported
PrinterSpecial = CIOExitNotSupported
