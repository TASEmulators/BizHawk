; Altirra - Additions AUTORUN.SYS module
; Copyright (C) 2014-2017 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'
		icl		'sio.inc'

runad	equ		$02e0
initad	equ		$02e2
dskinv	equ		$e453
ciov	equ		$e456

		org		$2400

;==========================================================================
.proc main
		mva		#CIOCmdPutChars iccmd
		mwa		#message icbal
		mwa		#[.len message] icbll
		ldx		#0
		jmp		ciov

cio_command:
		dta		CIOCmdPutChars,0,a(message),a(.len message)
.endp

;==========================================================================
.proc message
		;		 01234567890123456789012345678901234567
		dta		$7D
		dta		'Altirra Additions disk'+$80,$9B
		dta		'This disk contains helper software to',$9B
		dta		'use with emulated peripherals, such',$9B
		dta		'as R: and T: handlers. See Help for',$9B
		dta		'details on the contents of the',$9B
		dta		'Additions disk.',$9B
		dta		$9B
		dta		'Note that while this disk contains a',$9B
		dta		'mostly compatible DOS 2 replacement,',$9B
		dta		'it',$27,'s still very buggy, so use with',$9B
		dta		'at your own risk.',$9B
		dta		$9B
		dta		'If you have built-in BASIC enabled,',$9B
		dta		'use DOS to enter the CP and CART to',$9B
		dta		'restart BASIC.',$9B
		dta		$9B
.endp

		run		main
