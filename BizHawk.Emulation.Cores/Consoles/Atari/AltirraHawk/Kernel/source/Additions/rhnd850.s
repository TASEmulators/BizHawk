;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - R: Device Executable Loader
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

ciov	equ		$e456

;==========================================================================
.macro _ASSERT condition, message
		.if (:condition)=0
		.error	message
		.endif
.endm

;==========================================================================

		org		$00ea
		opt		o-
relocad	dta		a(0)		;this has to be at $EA (nop)
basead	dta		a(0)
srcptr	dta		a(0)
dstptr	dta		a(0)

		org		$3000
		opt		o+

;==========================================================================
.proc HandlerData
		ins		'850handler.bin'
.endp

;==========================================================================
.proc _main
		lda		#<msg_banner
		ldy		#>msg_banner
		jsr		PutMessage
		
		;check if HATABS has R: already
		ldx		#0
		lda		#'R'
hatabs_check:
		cmp		hatabs,x
		bne		not_r
		lda		#<msg_already_loaded
		ldy		#>msg_already_loaded
		jmp		PutMessage
not_r:
		inx
		inx
		inx
		cpx		#36
		bne		hatabs_check

		;set base address to MEMLO (and move it to more convenient zp)
		lda		memlo
		sta		basead
		sta		dstptr
		lda		memlo+1
		sta		basead+1
		sta		dstptr+1
		
		;relocate handler data down to MEMLO (ascending copy)
		mwa		#HandlerData srcptr
		ldx		#>[.len HandlerData]
		ldy		#0
page_loop:
		mva:rne	(srcptr),y (dstptr),y+
		inc		srcptr+1
		inc		dstptr+1
		dex
		bne		page_loop
		
		.if		<[.len HandlerData]
xtra_loop:
		lda		(srcptr),y
		sta		(dstptr),y
		iny
		cpy		#<[.len HandlerData]
		bne		xtra_loop
		.endif
						
		;relocate low bytes
		ldx		#0
		ldy		#1
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
		jsr		do_init

		;fake cold start around cold init so we don't reinit DOS
		lda		warmst
		pha
		mva		#0 warmst
		jsr		do_init2
		pla
		sta		warmst
		
		lda		#<msg_load_succeeded
		ldy		#>msg_load_succeeded
		jmp		PutMessage
		
do_init2
		jmp		(dosini)
		
do_init:
		jmp		(basead)

msg_banner:
		dta		'Altirra 850 R: Handler V0.2',$9B
		
msg_already_loaded:
		dta		'R: handler already loaded.',$9B
		
msg_load_succeeded:
		dta		'R: handler load succeeded.',$9B
.endp

;==========================================================================
; Input:
;	Y:A = message
;
.proc PutMessage
		sta		icbal
		sty		icbah
		mva		#CIOCmdPutRecord iccmd
		ldx		#1
		sta		icblh
		dex
		sta		icbll
		jmp		ciov
.endp

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

;==========================================================================
		run		_main
