;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - T: Device Executable Loader
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

		org		$0080
		opt		o-
relocad	dta		a(0)
basead	dta		a(0)
srcptr	dta		a(0)
dstptr	dta		a(0)

		org		$3000
		opt		o+

;==========================================================================
.proc HandlerData
		ins		'1030handler-reloc.bin'
.endp

;==========================================================================
.proc _main
		lda		#<msg_banner
		ldy		#>msg_banner
		jsr		PutMessage
		
		;check if HATABS has T: already
		ldx		#0
		lda		#'T'
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

		;set base address to MEMLO with page alignment (and move it to more convenient zp)
		lda		#0
		sta		basead
		sta		relocad
		sta		dstptr
		lda		memlo
		cmp		#1
		lda		memlo+1
		adc		#0
		sta		basead+1
		sta		relocad+1
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
						
		;relocate high bytes
		jsr		Relocate
		
		;execute init
		lda		#$0c
		sta		basead
		jsr		do_init
		
		lda		#<msg_load_succeeded
		ldy		#>msg_load_succeeded
		jmp		PutMessage
				
do_init:
		jmp		(basead)

msg_banner:
		dta		'Altirra 1030 T: Handler V0.1',$9B
		
msg_already_loaded:
		dta		'T: handler already loaded.',$9B
		
msg_load_succeeded:
		dta		'T: handler load succeeded.',$9B
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
.proc Relocate
		ldx		#0
reloc_loop:
		;fetch first delta byte
		jsr		GetByte
		
		;if first delta byte is zero, we're done
		beq		done

ext_addr:
		;add delta byte to relocation pointer
		tay
		clc
		adc		relocad
		sta		relocad
		scc:inc	relocad+1
		tya
		
		;loop back for more bytes if we have a $FF, but note that $00 is
		;not a terminator after the first byte
		cmp		#$ff
		bne		next_addr_done
		jsr		GetByte
		bcs		ext_addr
		
next_addr_done:
		lda		(relocad,x)
		adc		basead+1
		sta		(relocad,x)
		jmp		reloc_loop

done:
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
		icl		'1030handler-reloc.inc'
		opt		l+

;==========================================================================
		run		_main
