;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement 1030 Modem Firmware - ModemLink software
;	Copyright (C) 2008-2017 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

		icl		'hardware.inc'
		icl		'kerneldb.inc'
		icl		'cio.inc'

ciov	= $E456
siov	= $E459

;==========================================================================
; BOOT SECTOR
;
		org		$0600
		opt		h-f+

.nowarn .proc BootSector
		dta		$00						;flags (unused)
		dta		1						;sector count
		dta		a($0600)				;boot address
		dta		a(init_addr)			;init address

boot_entry:
		;display loading message
		mva		#CIOCmdPutChars iccmd
		mwa		#boot_message icbal
		mva		#[.len boot_message] icbll
		ldx		#0
		stx		icblh
		jsr		ciov

		;issue SIO call to load main firmware
		ldx		#11
		mva:rpl	sio_request,x ddevic,x-
		jsr		siov
		bpl		boot_ok
		sec
init_addr:
		rts
boot_ok:
		jsr		Init
		ldx		#0
		stx		icbll
		lda		#$7D
		jsr		ciov
		clc
		rts

.proc boot_message
		dta		'Loading ModemLink...'
.endp

sio_request:
		dta		$58			;device
		dta		$01			;unit
		dta		$3B			;command
		dta		$40			;status/mode
		dta		a($0C00)	;buffer address
		dta		a($00E0)	;timeout
		dta		a($2800)	;buffer length
		dta		$00			;AUX1
		dta		$00			;AUX2
.endp

.if * < $067f
		org		$067F
		dta		0
.endif
		org		$0680

;==========================================================================
; MODEMLINK SOFTWARE (PART 1)
;
		opt		f-
		org		$0C00
		opt		f+

.proc Init
		mwa		#Main dosvec

		;We load up to $3400, but the real ModemLink software only raises
		;MEMLO to $3200.
		lda		#<$3200
		sta		memlo
		sta		appmhi
		lda		#>$3200
		sta		memlo+1
		sta		appmhi+1
		clc
		rts
.endp

.proc Main
		mva		#CIOCmdPutChars iccmd
		mwa		#error_message icbal
		mwa		#[.len error_message] icbll
		ldx		#0
		jsr		ciov
		jmp		*
		
.proc error_message
		dta		$7D
		dta		'ModemLink firmware missing           '*,$9B
		dta		'This is a placeholder for the',$9B
		dta		'ModemLink software that is normally',$9B
		dta		'built into the 1030 Modem and boots',$9B
		dta		'when no disk drive is connected.',$9B
		dta		'A ModemLink firmware image can be',$9B
		dta		'set up in the Firmware Images dialog.',$9B
.endp
.endp


;==========================================================================
; EMBEDDED T: HANDLER
;
		org		$1D00
		ins		'1030handler.bin'

;==========================================================================
; MODEMLINK SOFTWARE (PART 2)
;
		org		$2830


.if * < $3400
		org		$33FF
		dta		0
.endif
		org		$3400
