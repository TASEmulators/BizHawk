;	Altirra - Atari 800/800XL/5200 emulator
;	Additions - deferred program loader
;	Copyright (C) 2008-2018 Avery Lee
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

		org		$2200

;==========================================================================
.proc	main
memlo_ok:
		;issue handler read command
		ldx		#11
		mva:rpl	readhandler_cmd,x ddevic,x-
		jsr		siov
		bmi		fail

		;jump to CIO initialization function within absolute block
		jmp		load_buffer+14
.endp

load_buffer:
readhandler_cmd:
		dta		$FD,$01,$26,$40,a(load_buffer),a($0003),a($0080),a($0000)

fail:
		rts

		run		main
