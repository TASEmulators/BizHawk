;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - 65C816 Character Input/Output Facility
;	Copyright (C) 2008-2018 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.proc CIOInit	
	sec
	ldx		#$70
iocb_loop:
	lda		#$ff
	sta		ichid,x
	jsr		CIOSetPutByteClosed
	txa
	sbc		#$10
	tax
	bpl		iocb_loop
	rts
.endp

;==============================================================================
;	Character I/O entry vector
;
;	On entry:
;		X = IOCB offset (# x 16)
;
;	Returns:
;		A = depends on operation
;		X = IOCB offset (# x 16)
;		Y = status (reflected in P)
;
;	Notes:
;		BUFADR must not be touched from CIO. DOS XE relies on this for
;		temporary storage and breaks if it is modified.
;
;	XL/XE mode notes:
;		HNDLOD is always set to $00 afterward, per Sweet 16 supplement 3.
;
;		CIO can optionally attempt a provisional open by doing a type 4 poll
;		over the SIO bus. This happens unconditionally if HNDLOD is non-zero
;		and only after the device is not found in HATABS if HNDLOD is zero.
;		If this succeeds, the IOCB is provisionally opened. Type 4 polling
;		ONLY happens for direct opens -- it does not happen for a soft open.
;
.proc CIO
	;stash IOCB offset (X) and acc (A)
	sta		ciochr
	stx		icidno
	jsr		process
xit:
	;copy status back to IOCB
	ldx		icidno
	tya
	sta		icsta,x
	php
	
	stz		hndlod

	lda		ciochr
	plp
	rts
	
process:
		;validate IOCB offset
		txa
		bit		#$8f
		beq		validIOCB

		;check if it's the special X=$FF used by 816os for first unused
		;IOCB
		inc
		bne		iocb_invalid

		;find first unused IOCB
		lda		#0
		clc
iocb_search_loop:
		tax
		ldy		ichid,x
		bmi		found_iocb
		adc		#$10
		bpl		iocb_search_loop

iocb_invalid:
		;return invalid IOCB error
		ldy		#CIOStatInvalidIOCB
		rts
	
cmdInvalid:
		;invalid command <$03
		ldy		#CIOStatInvalidCmd
		rts

found_iocb:
		stx		icidno
		ldy		#1
		rts

validIOCB:
	jsr		CIOLoadZIOCB
	
	;check if we're handling the OPEN command
	lda		iccomz
	cmp		#CIOCmdOpen
	beq		cmdOpen
	bcc		cmdInvalid
	
	;check if the IOCB is open
	ldy		ichidz
	
	bmi		not_open
	
	;check for a provisionally open IOCB
	iny
	bpl		isOpen
	
	;okay, it's provisionally open... check if it's a close
	cmp		#CIOCmdClose
	sne:jmp	cmdCloseProvisional
	
	;check if we're allowed to load a handler
	lda		hndlod
	beq		not_open
	
	;try to load the handler
	jsr		CIOLoadHandler
	bpl		isOpen
	rts
	
not_open:
	;IOCB isn't open - issue error
	;
	;Special cases;
	; - No error issued for close ($0C). This is needed so that extra CLOSE
	;   commands from BASIC don't trip errors.
	; - Get status ($0D) and special ($0E+) do soft open and close if needed.
	;   $0D case is required for Top Dos 1.5a to boot; $0E+ case is encountered
	;   with R: device XIO commands.
	;
	ldy		#1
	lda		iccomz
	cmp		#CIOCmdClose
	beq		ignoreOpen
	cmp		#CIOCmdGetStatus
	bcs		preOpen				;closed IOCB is OK for get status and special
not_open_handler:
	ldy		#CIOStatNotOpen
ignoreOpen:
	rts
	
preOpen:
	;If the device is not open when a SPECIAL command is issued, parse the path
	;and soft-open the device in the zero page IOCB.
	jsr		CIOParsePath

	;check for special command
	lda		iccomz
	cmp		#CIOCmdGetStatus
	beq		cmdGetStatusSoftOpen
	cmp		#CIOCmdSpecial
	bcs		cmdSpecialSoftOpen
	
isOpen:
	ldx		iccomz
	cpx		#CIOCmdSpecial
	scc:ldx	#$0e
	
	;do permissions check
	lda		perm_check_table-4,x
	bmi		skip_perm_check
	bit		icax1z
	beq		perm_check_fail
skip_perm_check:

	;load command table vector
	lda		command_table_hi-4,x
	pha
	lda		command_table_lo-4,x
	pha
	
	;preload dispatch vector and dispatch to command
	ldy		vector_preload_table-4,x
load_vector:
	ldx		ichidz
	mwa		hatabs+1,x icax3z
	lda		(icax3z),y
	tax
	dey
	lda		(icax3z),y
	sta		icax3z
	stx		icax3z+1
	
	;many commands want to check length=0 on entry
	lda		icbllz
	ora		icblhz
	rts

perm_check_fail:
	;at this point we have A=$04 if we failed a get perm check, and A=$08
	;if we failed a put perm check -- these need to be translated to Y=$83
	;and Y=$87.
	clc
	adc		#$7f
	tay
	rts

;--------------------------------------------------------------------------
cmdGetStatusSoftOpen:
	ldy		#9
	dta		{bit $0100}
cmdSpecialSoftOpen:
	ldy		#11
invoke_and_soft_close_xit:
	jmp		CIOInvoke
		
;--------------------------------------------------------------------------
; Open command ($03).
;
cmdOpen:
	;check if the IOCB is already open
	ldy		ichidz
	iny
	beq		notAlreadyOpen
	
	;IOCB is already open - error
	ldy		#CIOStatIOCBInUse
	rts
	
notAlreadyOpen:
	;attempt to parse and open -- note that this will fail out directly
	;on an unknown device or provisional open
	jsr		CIOParsePath

open_entry:
	;request open
	ldy		#1
	jsr		CIOInvoke

	;move handler ID and device number to IOCB
	ldx		icidno
	mva		ichidz ichid,x
	mva		icdnoz icdno,x

	tya
	bpl		openOK
	rts
	
openOK:

	;copy PUT BYTE vector for Atari Basic
	ldx		ichidz
	mwa		hatabs+1,x icax3z
	ldy		#6
	lda		(icax3z),y
	ldx		icidno
	sta		icptl,x
	iny
	lda		(icax3z),y
	sta		icpth,x
	ldy		#1
	rts

cmdGetStatus = CIOInvoke.invoke_vector
cmdSpecial:
	jsr		CIOInvoke.invoke_vector

	;need to copy AUX1/2 back for R:
	ldx		icidno
	mva		icax1z icax1,x
	mva		icax2z icax2,x
	rts
	
;--------------------------------------------------------------------------
cmdGetRecord:
	;check if buffer is full on entry
	beq		cmdGetRecordBufferFull
cmdGetRecordLoop:
cmdGetRecordGetByte:
	;fetch byte
	jsr		CIOInvoke.invoke_vector
	cpy		#0
	bmi		cmdGetRecordXit
	
	;store byte (even if EOL)
	ldx		#0
	sta		(icbalz,x)

	;check for EOL
	eor		#$9b
	cmp		#1

	;increment buffer pointer and decrement length
	jsr		advance_pointers

	;skip buffer full check if we had an EOL
	bcc		cmdGetRecordXit

	;loop back for more bytes if buffer not full
	bne		cmdGetRecordLoop
	
cmdGetRecordBufferFull:
	;read byte to discard
	jsr		CIOInvoke.invoke_vector
	cpy		#0
	bmi		cmdGetRecordXit

	;continue if not EOL
	cmp		#$9b
	bne		cmdGetRecordBufferFull

	;return truncated record
	ldy		#CIOStatTruncRecord

cmdGetRecordXit:
cmdGetPutDone:
	;update byte count in IOCB
	ldx		icidno
	sec
	lda		icbll,x
	sbc		icbllz
	sta		icbll,x
	lda		icblh,x
	sbc		icblhz
	sta		icblh,x

	;NOMAM 2013 BASIC Ten-Liners disk requires ICBALZ to be untouched :P
	mwa		icbal,x icbalz
	
	;Pacem in Terris requires Y=1 exit.
	;DOS 3.0 with 128K/XE mode requires Y=3 for EOF imminent.
	rts
	
;--------------------------------------------------------------------------
cmdGetChars:
	beq		cmdGetCharsSingle
cmdGetCharsLoop:
	jsr		CIOInvoke.invoke_vector
	cpy		#0
	bmi		cmdGetCharsError
	ldx		#0
	sta		ciochr					;required by HOTEL title screen
	sta		(icbalz,x)
	jsr		advance_pointers
	bne		cmdGetCharsLoop
cmdGetCharsError:
	jmp		cmdGetPutDone
	
cmdGetCharsSingle:
	jsr		CIOInvoke.invoke_vector
	sta		ciochr
	rts
	
;--------------------------------------------------------------------------
; PUT RECORD handler ($09)
;
; Exit:
;	ICBAL/ICBAH: Not changed
;	ICBLL/ICBLH: Number of bytes processed
;
; If the string does not contain an EOL character, one is printed at the
; end. Also, in this case CIOCHR must reflect the last character in the
; buffer and not the EOL. (Required by Atari DOS 2.5 RAMDISK banner)
;
; If length=0, the character in the A register is output without an EOL.
; This behavior is required by the graphics library for Mad Pascal.
;
cmdPutRecord:
	beq		cmdPutCharsSingle
cmdPutRecordLoop:
	ldy		#0
	lda		(icbalz),y
	jsr		CIOInvoke.invoke_vector
	tya
	bmi		cmdPutRecordError
	jsr		advance_pointers
	beq		cmdPutRecordEOL
	lda		#$9b
	cmp		ciochr
	beq		cmdPutRecordDone
	bne		cmdPutRecord
	
cmdPutRecordEOL:
	lda		#$9b
	jsr		CIOInvoke.invoke_vector
cmdPutRecordError:
cmdPutRecordDone:
	jmp		cmdGetPutDone
	
;--------------------------------------------------------------------------
cmdPutChars:
	beq		cmdPutCharsSingle
cmdPutCharsLoop:
	ldy		#0
	lda		(icbalz),y
	jsr		CIOInvoke.invoke_vector
	tya
	bmi		cmdPutRecordError
	jsr		advance_pointers
	bne		cmdPutCharsLoop
	jmp		cmdGetPutDone
cmdPutCharsSingle:
	jmp		CIOInvoke.invoke_vector_ciochr
	
;--------------------------------------------------------------------------

advance_pointers:
	inw		icbalz
	dew		icbllz
	sne:lda	icblhz
	rts

;--------------------------------------------------------------------------
cmdClose:
	jsr		CIOInvoke.invoke_vector
cmdCloseProvisional:	
	ldx		icidno
	jsr		CIOSetPutByteClosed
	mva		#$ff	ichid,x
	rts

perm_check_table:
	dta		$04					;$04 (get record)
	dta		$04					;$05 (get record)
	dta		$04					;$06 (get chars)
	dta		$04					;$07 (get chars)
	dta		$08					;$08 (put record)
	dta		$08					;$09 (put record)
	dta		$08					;$0A (put chars)
	dta		$08					;$0B (put chars)
	dta		$ff					;$0C (close)
	dta		$ff					;$0D (get status)
	dta		$ff					;$0E (special)

vector_preload_table:
	dta		$05					;$04 (get record)
	dta		$05					;$05 (get record)
	dta		$05					;$06 (get chars)
	dta		$05					;$07 (get chars)
	dta		$07					;$08 (put record)
	dta		$07					;$09 (put record)
	dta		$07					;$0A (put chars)
	dta		$07					;$0B (put chars)
	dta		$03					;$0C (close)
	dta		$09					;$0D (get status)
	dta		$0b					;$0E (special)

command_table_lo:
	dta		<(cmdGetRecord-1)	;$04
	dta		<(cmdGetRecord-1)	;$05
	dta		<(cmdGetChars-1)	;$06
	dta		<(cmdGetChars-1)	;$07
	dta		<(cmdPutRecord-1)	;$08
	dta		<(cmdPutRecord-1)	;$09
	dta		<(cmdPutChars-1)	;$0A
	dta		<(cmdPutChars-1)	;$0B
	dta		<(cmdClose-1)		;$0C
	dta		<(cmdGetStatus-1)	;$0D
	dta		<(cmdSpecial-1)		;$0E

command_table_hi:
	dta		>(cmdGetRecord-1)	;$04
	dta		>(cmdGetRecord-1)	;$05
	dta		>(cmdGetChars-1)	;$06
	dta		>(cmdGetChars-1)	;$07
	dta		>(cmdPutRecord-1)	;$08
	dta		>(cmdPutRecord-1)	;$09
	dta		>(cmdPutChars-1)	;$0A
	dta		>(cmdPutChars-1)	;$0B
	dta		>(cmdClose-1)		;$0C
	dta		>(cmdGetStatus-1)	;$0D
	dta		>(cmdSpecial-1)		;$0E
.endp

;==========================================================================
; Invoke device vector.
;
; Entry (standard):
;	A, X = ignored
;	Y = offset to high vector byte in device table
;
; Entry (invoke_vector):
;	A = byte to pass to PUT CHAR vector
;	X, Y = ignored
;
; Exit:
;	A = byte returned from GET CHAR vector
;	Y = status
;
.proc CIOInvoke
	jsr		CIO.load_vector		
invoke_vector:
	sta		ciochr
invoke_vector_ciochr:
	lda		icax3z+1
	pha
	lda		icax3z
	pha
	ldy		#CIOStatNotSupported
	ldx		icidno
	lda		ciochr
	rts
.endp

;==========================================================================
; Copy IOCB to ZIOCB.
;
; Entry:
;	X = IOCB
;
; [OSManual p236] "Although both the outer level IOCB and the Zero-page
; IOCB are defined to be 16 bytes in size, only the first 12 bytes are
; moved by CIO."	
;
.proc CIOLoadZIOCB
	;We used to do a trick here where we would count Y from $F4 to $00...
	;but we can't do that because the 65C816 doesn't wrap abs,Y within
	;bank 0 even in emulation mode. Argh!
	
	ldy		#0
copyToZIOCB:
	lda		ichid,x
	sta		ziocb,y
	inx
	iny
	cpy		#12	
	bne		copyToZIOCB
	rts
.endp

;==========================================================================
.proc CIOParsePath
	;default to device #1
	ldx		#1

	;pull first character of filename and stash it
	lda		(icbalz-1,x)
	sta		icax4z
		
	;Check for a device number.
	;
	; - D1:-D9: is supported. D0: also gives unit 1, and any digits beyond
	;   the first are ignored.
	;
	; We don't validate the colon anymore -- Atari OS allows opening just "C" to get
	; to the cassette.
	;
	ldy		#1
	lda		(icbalz),y
	sec
	sbc		#'0'
	beq		nodevnum
	cmp		#10
	bcs		nodevnum
	tax
	
	iny
	
nodevnum:
	stx		icdnoz
	
.if _KERNEL_XLXE
	;check if we are doing a true open and if we should do a type 4 poll
	lda		iccomz
	cmp		#CIOCmdOpen
	bne		skip_poll
	
	;clear DVSTAT+0/+1 to indicate no poll
	lda		#0
	sta		dvstat
	sta		dvstat+1
	
	;check if we should do an unconditional poll (HNDLOD nonzero).
	lda		hndlod
	bne		unconditional_poll
	
	;search handler table
	jsr		CIOFindHandler
	beq		found
	
unconditional_poll:
	;do type 4 poll
	jsr		CIOPollForDevice
	bmi		unknown_device
	
	;mark provisionally open
	ldx		icidno
	mva		#$7f ichid,x
	mva		icax4z icax3,x
	mva		dvstat+2 icax4,x
	mwa		#CIOPutByteLoadHandler-1 icptl,x
	mva		icdnoz icdno,x

	;do direct exit, bypassing regular open path
	pla
	pla
	ldy		#1
	rts

skip_poll:
.endif

	;search handler table
	jsr		CIOFindHandler
	beq		found
	
unknown_device:
	;return unknown device error
	ldy		#CIOStatUnkDevice
	pla
	pla
found:
	rts
.endp

;==========================================================================
.proc CIOSetPutByteClosed
	lda		#<[CIO.not_open_handler-1]
	sta		icptl,x
	lda		#>[CIO.not_open_handler-1]
	sta		icpth,x
	rts
.endp

;==========================================================================
; Attempt to find a handler entry in HATABS.
;
.proc CIOFindHandler
	;search for handler
	lda		icax4z
	ldx		#11*3
findHandler:
	cmp		hatabs,x
	beq		foundHandler
	dex
	dex
	dex
	bpl		findHandler
foundHandler:
	;store handler ID
	stx		ichidz
	rts
.endp

;==========================================================================
; Poll SIO bus for CIO device
;
; Issues a type 4 poll ($4F/$40/devname/devnumber).
;
.if _KERNEL_XLXE
.proc CIOPollForDevice
	lda		icax4z
	sta		daux1
	lda		icdnoz
	sta		daux2
	
	ldx		#9
	mva:rpl	cmd_tab,x ddevic,x-
	
	jmp		siov
	
cmd_tab:
	dta		$4f		;device
	dta		$01		;unit
	dta		$40		;command
	dta		$40		;status (transfer flags)
	dta		<dvstat	;dbuflo
	dta		>dvstat	;dbufhi
	dta		$40		;dtimlo
	dta		$00		;unused
	dta		$04		;dbytlo
	dta		$00		;dbythi
.endp
.endif

;==========================================================================
; Load handler for a provisionally open IOCB.
;
.if _KERNEL_XLXE
.proc CIOLoadHandler
	;load handler over SIO bus
	mwa		dvstat+2 loadad
	ldx		icidno
	mva		icax4,x ddevic
	jsr		PHLoadHandler
	bcs		fail
	
	;let's see if we can look up the handler now
	ldx		icidno
	mva		icax3,x icax4z
	jsr		CIOFindHandler
	bne		fail
	
	;follow through with open
	jsr		CIO.open_entry
	bpl		ok
fail:
	ldy		#CIOStatUnkDevice
ok:
	rts
.endp
.endif

;==========================================================================
; PUT BYTE handler for provisionally open IOCBs.
;
; This handler is used when an IOCB has been provisionally opened pending
; a handler load over the SIO bus. It is used when a direct call is made
; through ICPTL/ICPTH. If HNDLOD=0, the call fails as handler loading is
; not set up; if it is nonzero, the handler is loaded over the SIO bus and
; then the PUT BYTE call continues if everything is good.
;
.if _KERNEL_XLXE
.proc CIOPutByteLoadHandler
	;save off A/X
	sta		ciochr
	stx		icidno
		
	;check if we're allowed to load a handler and bail if not
	lda		hndlod
	beq		load_error

	;copy IOCB to ZIOCB
	jsr		CIOLoadZIOCB

	;try to load the handler
	jsr		CIOLoadHandler
	bmi		load_error
	
	;all good... let's invoke the standard handler
	ldy		#7
	jsr		CIOInvoke
	jmp		xit
	
load_error:
	ldy		#CIOStatUnkDevice
xit:
	php
	lda		ciochr
	ldx		icidno
	plp
	rts
.endp
.endif
