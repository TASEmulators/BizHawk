;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement 850 Interface Firmware - R: Device Relocator Loader
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'cio.inc'
		icl		'sio.inc'
		icl		'kerneldb.inc'
		icl		'hardware.inc'

;==========================================================================
.macro _ASSERT condition, message
		.if (:condition)=0
		.error	message
		.endif
.endm

;==========================================================================

siov = $e459
KnownRTS = $e4c0

;==========================================================================

		org		$00ea
		opt		o-
relocad	dta		a(0)		;this has to be at $EA (nop)
basead	dta		a(0)

		org		$0500
		opt		o+h-f+

		dta		0			;DFLAGS
		dta		3			;DBSECT
		dta		a($0500)	;BOOTAD
		dta		a(KnownRTS)	;init vector D(DOSINI)
_main:
		ldx		#9
		mva:rpl	dcb,x ddevic,x-

		;set base address to MEMLO (and move it to more convenient zp)
		lda		memlo
		sta		dbuflo
		sta		basead
		lda		memlo+1
		sta		dbufhi
		sta		basead+1
				
		;issue request to download
		jsr		siov
		bpl		dl_ok
		sec
		rts
		
dcb:
		dta		$50					;device
		dta		$01					;unit
		dta		$26					;command
		dta		$40					;mode (read)
		dta		a(0)				;address
		dta		a($0008)			;timeout
		dta		a(relocbin_len)		;length		
	
dl_ok:
		ldx		#0
		ldy		#1
		
		;relocate low bytes
		jsr		Relocate
		
		;relocate words
		lda		#$91
		sta		Relocate.high_write_op
		jsr		Relocate
		
		;relocate high bytes
		_ASSERT	((FetchDest ^ GetByte)&$ff00)==0, "fetch routines must be in the same page"
		mva		#<GetByte Relocate.fetch_routine
		lda		#{nop}
		sta		Relocate.lo_write_op
		jsr		Relocate
		
		;execute init
		jmp		(basead)

;==========================================================================
; Apply relocations.
;
; Inputs:
;	X = 0 for low/word relocations, 2 for high byte relocations
;	Y = 1
;
; Outputs:
;	X, Y preserved
;
.proc Relocate
		;reset relocation pointer
		mwa		basead relocad
		
reloc_loop:
		;fetch first delta byte
		jsr		GetByte
		
		;if first delta byte is zero, we're done
		beq		done

ext_addr:
		;add delta byte to relocation pointer
		pha
		clc
		adc		relocad
		sta		relocad
		scc:inc	relocad+1
		pla
		
		;loop back for more bytes if we have a $FF, but note that $00 is
		;not a terminator after the first byte
		cmp		#$ff
		bne		next_addr_done
		jsr		GetByte
		bcs		ext_addr
		
next_addr_done:
		jsr		FetchDest			;get dest lo (lo/word), get stream byte (hi)
fetch_routine = *-2
		adc		basead
		sta		(relocad,x)			;turned into nop/nop for (hi)
lo_write_op = *-2
		lda		(relocad),y
		adc		basead+1
		eor		#relocad			;opcode changed from $49 (EOR #imm) (lo) to $91 (STA (zp),Y) (word/hi)
high_write_op = *-2
		jmp		reloc_loop

done:
		rts
.endp
		
;==========================================================================
.proc FetchDest
		lda		(relocad,x)
		rts
.endp
	
;==========================================================================
.proc GetByte
		inw		get_ptr
		lda		relocdata_begin-1
get_ptr = *-2
		rts
.endp

;==========================================================================
		opt		l-
		icl		'850handler-relocs.inc'
		opt		l+
