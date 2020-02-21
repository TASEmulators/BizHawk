;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - 850 R: Handler Boot Utility
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'

ciov	equ		$e456
siov	equ		$e459

		org		$80

		org		$3c00

;==========================================================================
.proc	main
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

		;check if MEMLO is too low
		lda		#0
		cmp		memlo
		lda		#$1d
		sbc		memlo+1
		bcs		memlo_ok

		;fail due to low memlo
		lda		#<msg_memlo_1
		ldy		#>msg_memlo_1
		jsr		PutMessage
		lda		#<msg_memlo_2
		ldy		#>msg_memlo_2
		jmp		PutMessage

memlo_ok:
		;issue handler read command
		ldx		#11
		mva:rpl	readhandler_cmd,x ddevic,x-
		jsr		siov
		tya
		bpl		load_succeeded
		
fail_exit:
		lda		#<msg_load_failed
		ldy		#>msg_load_failed
		jmp		PutMessage
		
load_succeeded:
		clc
		jsr		$1D0C
		
		lda		#<msg_load_succeeded
		ldy		#>msg_load_succeeded
		jmp		PutMessage

readhandler_cmd:
		dta		$58,$01,$3C,$40,a($1D00),a($0080),a($0B30),a($0000)
		
msg_banner:
		dta		'Altirra 1030 T: Handler Loader V0.1',$9B
		
msg_already_loaded:
		dta		'T: handler already loaded.',$9B

msg_memlo_1:
		dta		'Cannot load T: handler as MEMLO is',$9B
msg_memlo_2:
		dta		'above $1D00.',$9B

msg_load_failed:
		dta		'T: handler load failed.',$9B
		
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
		run		main
