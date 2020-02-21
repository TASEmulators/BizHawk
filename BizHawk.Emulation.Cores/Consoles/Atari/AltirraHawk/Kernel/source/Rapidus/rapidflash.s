;	Altirra - Atari 800/800XL/5200 emulator
;	Rapidus emulator bootstrap firmware - flash image
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		opt		h-o+

.macro _PAD16K
		opt		f-
		org		0
		opt		f+
		dta		$ff
		.align	$4000,$ff
		opt		f-
.endm

.macro _PAD32K
		opt		f-
		org		0
		opt		f+
		dta		$ff
		.align	$8000,$ff
		opt		f-
.endm
	
		org		0
		opt		f+
		ins		'rapidfirmware.bin'
.if * != $C000
	.error "Invalid firmware image: ",*
.endif
		opt		f-

		org		0
		opt		f+
		ins		'rapidos.bin'
		opt		f-

		_PAD32K				;$F1:0000-F1:7FFF

		opt		f-
		org		$8000
		opt		f+
		dta		$ff
		.align	$4000,$ff	;$C000
		dta		$ff
		.align	$1000,$ff	;$D000
		dta		$ff
		.align	$0800,$ff	;$D800

		;$1D800-1DFFF: 6502 PBI firmware
		ins		'rapidpbi8.bin'
		.align	$4000,$ff	;$10000

		_PAD32K				;$F2:0000-F2:7FFF
		_PAD32K				;$F2:8000-F2:FFFF
		_PAD32K				;$F3:0000-F3:7FFF
		_PAD32K				;$F3:8000-F3:FFFF
		_PAD32K				;$F4:0000-F4:7FFF
		_PAD32K				;$F4:8000-F4:FFFF
		_PAD32K				;$F5:0000-F5:7FFF
		_PAD32K				;$F5:8000-F5:FFFF
		_PAD32K				;$F6:0000-F6:7FFF
		_PAD32K				;$F6:8000-F6:FFFF
		_PAD32K				;$F7:0000-F7:7FFF
		_PAD32K				;$F7:8000-F7:FFFF

		end
