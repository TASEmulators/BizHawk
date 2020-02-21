;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 @: system device routines
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
.proc SystemDevHandlerTable
		dta		a(SysDevOpen-1)
		dta		a(SysDevClose-1)
		dta		a(SysDevGetByte-1)
		dta		a(SysDevPutByte-1)
		dta		a(SysDevGetStatus-1)
		dta		a(SysDevSpecial-1)
.endp

;==========================================================================
.proc SysDevOpen
		;For now, we reuse the cassette handler's FEOF flag for our index.
		stz		feof
		ldy		#1
		rts
.endp

;==========================================================================
.proc SysDevGetByte
		ldx		feof
		bmi		at_eof
		lda		SysDevData,x
		ldy		#1
		inx
		spl:ldy	#3
		rts

at_eof:
		lda		#CIOStatEndOfFile
.endp

;==========================================================================
.proc SysDevData
		dta		$12,$03,$18					;BCD date (D/M/Y)
		dta		$00							;Option
		dta		$42,$42,$00,$00,$02,$04		;OS version (BB 000002.04)
		dta		$02							;CPU code (65C816)
		dta		$00							;FPU code (none)
		dta		$00							;Number of additional 64K banks
		dta		$01							;Native interrupt services
		dta		$00							;Memory management services
		dta		$00							;SIO extensions
		dta		$00							;Fast serial I/O
		dta		$00							;CIO extensions
		dta		$70							;Max IOCB number
		dta		a($0000)					;E: XIO functions
		dta		a($0000)					;S: XIO functions
		dta		a($0000)					;K: XIO functions
		dta		a($0000)					;P: XIO functions
		dta		a($0000)					;N: XIO functions
		dta		a($0000)					;@: XIO functions
.endp

;==========================================================================
SysDevClose = CIOExitSuccess
SysDevPutByte = CIOExitNotSupported
SysDevGetStatus = CIOExitNotSupported
SysDevSpecial = CIOExitNotSupported
