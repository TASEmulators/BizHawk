;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Serial Input/Output Routines
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

.proc SIOInit
	;turn off POKEY init mode so polynomial counters and audio run
	mva		#3 skctl
	sta		sskctl
	
	;enable noisy sound (yes, this is actually documented as being inited to
	;3)
	sta		soundr
	rts
.endp

;==============================================================================
.proc SIO
	;set retry counters
	mva		#$01 dretry
	
	;enter critical section
	sta		critic
	
.if _KERNEL_PBI_SUPPORT
	;attempt PBI transfer
	jsr		PBIAttemptSIO
	scc:jmp	xit_pbi
.endif

	;we must not save STACKP until after PBI devices are polled -- the
	;BlackBox PBI routines depend on being able to reuse this location
	tsx
	stx		stackp
	
	;Set timeout timer address -- MUST be done on each call to SIO, or
	;Cross-Town Crazy Eight hangs on load due to taking over this vector
	;previously. This is guaranteed by the OS Manual in Appendix L, H27.
	jsr		SIOSetTimeoutVector

	;check for cassette -- needs to match $60 or else Prisma: Alien Ambush
	;fails to load.
	ldx		#0
	lda		ddevic
	cmp		#$60
	sne:dex
	stx		casflg

	;init POKEY hardware
	jsr		SIOInitHardware

	;go do cassette now
	bit		casflg
	bpl		retry_command
	jmp		SIOCassette	
	
retry_command:
	;We try 13 times to get a command accepted by a device; after that it
	;counts as a device failure and we try one more round of 13 tries.
	mva		#$0d cretry
	
retry_command_2:
	;init command buffer
	lda		ddevic
	clc
	adc		dunit
	sec
	sbc		#1
	sta		cdevic

	mva		dcomnd		ccomnd
	mva		daux1		caux1
	mva		daux2		caux2
		
	;assert command line
	mva		#$34		pbctl

	;send command frame
	mva		#0			nocksm
	lda		#>cdevic
	sta		bufrhi
	sta		bfenhi
	mva		#<cdevic	bufrlo
	mva		#<caux2+1	bfenlo
	jsr		SIOSend
	bmi		xit
		
	;wait for the ACK
	jsr		SIOWaitForACK
	bpl		ackOK
	
command_error:
	jsr		SIOReceiveStop
	dec		cretry
	bpl		retry_command_2
	bmi		transfer_error

ackOK:

	;check if we should send a data frame
	bit		dstats
	bpl		no_send_frame
	
	;setup buffer pointers
	jsr		SIOSetupBufferPointers
	
	;send data frame	
	jsr		SIOSend
	bmi		xit
	
	;wait for ACK
	jsr		SIOWaitForACK
	bmi		command_error

no_send_frame:
	
	;setup for receiving complete
	ldx		#$ff
	stx		timflg
	stx		nocksm

	;setup frame delay for complete
	lda		dtimlo
	ror
	ror
	pha
	ror
	and		#$c0
	tay
	pla
	and		#$3f
	tax

	lda		#1
	jsr		setvbv

	ldx		#<temp
	stx		bufrlo
	inx
	stx		bfenlo
	ldx		#>temp
	stx		bufrhi
	stx		bfenhi
	jsr		SIOReceive
	bmi		transfer_error
	
	;Check if we received a C ($43) or E ($45) -- we must NOT abort immediately
	;on a device error, as the device still sends back data we need to read, and
	;Music Studio relies on the data coming back from a CRC error.
	lda		temp
	cmp		#$43
	beq		completeOK
	cmp		#$45
	beq		completeOK
	
	;we received crap... fail it now
device_error:
	ldy		#SIOErrorDeviceError
	
transfer_error:
	jsr		SIOReceiveStop

	dec		dretry
	bmi		device_retries_exhausted
	jmp		retry_command

device_retries_exhausted:
xit:
	ldx		casflg
	bne		leave_cassette_audio_on
	stx		audc1
	stx		audc2
	stx		audc3
	stx		audc4
leave_cassette_audio_on:
	ldx		stackp
	txs

xit_pbi:
	lda		#0
	sta		critic

	cpy		#0			;!! - A=0 must be preserved for LiteDOS
	sty		dstats
	sty		status
	rts

completeOK:		
	;check if we should read a data frame
	bit		dstats
	bvc		no_receive_frame
	
	;setup buffer pointers
	jsr		SIOSetupBufferPointers

	;receive the rest of the frame
	jsr		SIOReceive
	bmi		transfer_error

no_receive_frame:
	;now we can finally shut off the receive IRQ
	jsr		SIOReceiveStop

	;Now check whether we got a device error earlier. If we did, return
	;that instead of success.
	lda		temp
	cmp		#'C'
	bne		device_error
	
	;nope, we're good... exit OK.
	ldy		#SIOSuccess
	bne		xit
.endp

;==============================================================================
.proc SIOSetTimeoutVector
	mwa		#SIOCountdown1Handler	cdtma1
	rts
.endp

;==============================================================================
.proc SIOWaitForACK
	;setup 2 frame delay for ack
	ldx		#$ff
	stx		timflg
	stx		nocksm
	inx					;X=0
	lda		#1
	ldy		#2
	sty		bufrhi		;>temp = 2
	sty		bfenhi		;>temp+1 = 2
	jsr		setvbv

	;setup for receiving ACK
	ldx		#<temp
	stx		bufrlo
	inx
	stx		bfenlo		;#<[temp+1]

	jsr		SIOReceive
	
	;check if we had a receive error
	bmi		xit
	
	;check if we got an ACK
	lda		temp
	cmp		#'A'
	beq		xit
	
	;doh
	ldy		#SIOErrorNAK
xit:
	rts
.endp

;==============================================================================
;SIO send enable routine
;
; This is one of those routines that Atari inadvisably exposed in the OS jump
; table even though they shouldn't. Responsibilities of this routine are:
;
;	- Hit SKCTL to reset serial hardware and init for sending
;	- Hit SKRES to clear status
;	- Enable send interrupts
;	- Configure AUDF3/AUDF4 frequency (19200 baud or 600 baud)
;	- Set AUDC3/AUDC4 for noisy or non-noisy audio
;	- Set AUDCTL
;
; It does not init any of the SIO variables, only hardware/shadow state.
;
SIOInitHardware = SIOSendEnable.init_hardware
.proc SIOSendEnable
	;enable serial output ready IRQ and suppress serial output complete IRQ
	lda		pokmsk
	ora		#$10
	and		#$f7
	sta		pokmsk
	sta		irqen

no_irq_setup:
	;clear forced break mode and reset serial clocking mode to timer 4
	;synchronous; also enable two-tone mode if in cassette mode
	lda		sskctl
	and		#$0f
	ora		#$20
	ldx		casflg
	seq:ora	#$08
	sta		sskctl
	sta		skctl

init_hardware:
	;clock channel 3 and 4 together at 1.79MHz
	;configure pokey timers 3 and 4 for 19200 baud (1789773/(2*40+7) = 19040)
	ldx		#8
	
	;check if we are doing a cassette transfer; if so, use the cassette
	;register table instead
	lda		casflg
	beq		not_cassette
	
	ldx		#17
	
not_cassette:

	;load POKEY audio registers
	ldy		#8
	mva:rpl	regdata_normal,x- audf1,y-

	;go noisy audio if requested
	lda		soundr
	beq		no_noise
	
	lda		#$a8
	sta		audc4
	ldx		casflg
	beq		no_noise
	lda		#$10
	bit		sskctl
	bne		no_noise
	sta		audc1
	sta		audc2

no_noise:

	;reset serial status
	sta		skres
	rts

regdata_normal:
	dta		$00		;audf1
	dta		$a0		;audc1
	dta		$00		;audf2
	dta		$a0		;audc2
	dta		$28		;audf3
	dta		$a0		;audc3
	dta		$00		;audf4
	dta		$a0		;audc4
	dta		$28		;audctl
	
regdata_cassette:
	dta		$05		;audf1
	dta		$a0		;audc1
	dta		$07		;audf2
	dta		$a0		;audc2
	dta		$cc		;audf3
	dta		$a0		;audc3
	dta		$05		;audf4
	dta		$a0		;audc4
	dta		$28		;audctl

.endp

;==============================================================================
.proc SIOSetupBufferPointers
	clc
	lda		dbuflo
	sta		bufrlo
	adc		dbytlo
	sta		bfenlo
	lda		dbufhi
	sta		bufrhi
	adc		dbythi
	sta		bfenhi
	rts
.endp

;==============================================================================
;SIO send routine
;
.proc SIOSend
	dew		bufrlo				;must be -1 for (vseror)
	
	;configure serial port for synchronous transmission
	;enable transmission IRQs
	sei
	jsr		SIOSendEnable
	cli
	
	lda		#0
	sta		xmtdon
	sta		status
	sta		chksum
	sta		chksnt
	
	;send first byte
	lda		#>wait
	pha
	lda		#<wait
	pha
	php
	sei
	pha
	jmp		(vseror)
	
	;wait for transmit to complete
wait:
	ldy		status
	bmi		error
	lda		xmtdon
	beq		wait
	ldy		status
	
	;shut off transmission IRQs
error:
	sei
	lda		pokmsk
	and		#$e7
	sta		pokmsk
	sta		irqen
	cli

	;we're done
	tya
	rts
.endp

;==============================================================================
; SIO receive routine
;
; The exit and entry paths of this routine are time critical when receiving
; the Complete/Error byte and data frame since there may be no delay at all
; in between. The exit path needs to exit with the receive IRQ hot but IRQs
; masked; the entry path in turn needs to keep the IRQ enabled but not unmask
; IRQs until ready to receive.
;
.proc SIOReceive
	lda		#0
use_checksum:
	sta		chksum
	ldx		#0
	stx		recvdn			;receive done flag = false
	stx		bufrfl			;buffer full flag = false
	inx
	stx		status			;set status to success (1)

	;configure serial port for asynchronous receive
	;enable receive IRQ
	sei
	lda		sskctl
	and		#$8f
	ora		#$10
	sta		sskctl
	sta		skctl
	lda		pokmsk
	ora		#$20
	sta		pokmsk
	sta		irqen
	cli
	
	;Negate command line (if it isn't already negated).
	;
	;Note that we DON'T do this until we are entirely ready to receive,
	;because as soon as we do this we can get data.
	mva		#$3c pbctl

	;wait for receive to complete
wait:
	lda		timflg			;check for timeout
	beq		timeout			;bail if so
	ldy		status			;check for another error code
	bmi		error			;bail if so
	lda		recvdn			;check for receive complete
	beq		wait			;keep waiting if not
	tya						;set flags from status
	
error:
	;Mask interrupts, but exit with them masked. We do this in order to
	;handle the transition from Complete to data frame during receive. There
	;is no guaranteed device delay in between these and some disk drives
	;send back-to-back bytes. In addition, there are demos that have DLIs
	;active during SIO loads. Therefore, we avoid turning off the receive
	;interrupt and instead hold it off to give us the best chance of snagging
	;the first and second bytes successfully, even if delayed.
	sei
	rts
	
timeout:
	ldy		#SIOErrorTimeout
	sei
	rts
.endp

;==============================================================================
.proc SIOReceiveStop
	;shut off receive IRQs
	lda		pokmsk
	and		#$d7
	sta		pokmsk
	sta		irqen
	cli
	rts
.endp

;==============================================================================
; SIO serial input routine
;
; DOS 2.0S replaces (VSERIN), so it's critical that this routine follow the
; rules compatible with DOS. The rules are as follows:
;
;	BUFRLO/BUFRHI:	Points to next byte to read. Note that this is different
;					from (VSEROR)!
;	BFENLO/BFENHI:	Points one after last byte in buffer.
;	BUFRFL:			Set when all data bytes have been read.
;	NOCKSM:			Set if no checksum byte is expected. Cleared after checked.
;	RECVDN:			Set when receive is complete, including any checksum.	
;
.proc SIOInputReadyHandler
	lda		bufrfl
	bne		receiveChecksum

	;receive data byte
	tya
	pha
	lda		serin
	ldy		#$00
	sta		(bufrlo),y
	clc
	adc		chksum
	adc		#$00
	sta		chksum

	;restore Y now
	pla
	tay
	
	;bump buffer pointer
	inw		bufrlo
	
	;check for EOB
	lda		bufrlo
	cmp		bfenlo
	lda		bufrhi
	sbc		bfenhi
	bcc		xit

	dec		bufrfl					;!! - this was $00 coming in
	
	;should there be a checksum?
	lda		nocksm
	bne		skipChecksum
xit:
	pla
	rti
	
receiveChecksum:
	;read and compare checksum
	lda		serin
	cmp		chksum
	bne		checksum_fail
	
signal_end:
	;set receive done flag
	mva		#$ff	recvdn

	;exit
	pla
	rti

checksum_fail:
	lda		#SIOErrorChecksum
	sta		status
	bne		signal_end				;!! - unconditional
	
skipChecksum:
	;set receive done flag
	sta		recvdn
	
	;clear no checksum flag
	lda		#0
	sta		nocksm
	pla
	rti
.endp

;==============================================================================
; SIO serial output ready routine
;
; DOS 2.0S replaces (VSEROR), so it's critical that this routine follow the
; rules compatible with DOS. The rules are as follows:
;
;	BUFRLO/BUFRHI:	On entry, points to one LESS than the next byte to write.
;	BFENLO/BFENHI:	Points to byte immediately after buffer.
;	CHKSUM:			Holds running checksum as bytes are output.
;	CHKSNT:			$00 if checksum not yet sent, $FF if checksum sent.
;	POKMSK:			Used to enable the serial output complete IRQ after sending
;					checksum.
;
.proc SIOOutputReadyHandler
	;increment buffer pointer
	inc		bufrlo
	bne		addrcc
	inc		bufrhi
addrcc:

	;compare against buffer end
	lda		bufrlo
	cmp		bfenlo
	lda		bufrhi
	sbc		bfenhi			;set flags according to (dst - end)
	bcs		doChecksum

	;save Y
	tya
	pha

	;send out next byte
	ldy		#0
	lda		(bufrlo),y
	sta		serout
	
	;update checksum
	adc		chksum
	adc		#0
	sta		chksum

	;restore registers and exit
	pla
	tay
	pla
	rti
	
doChecksum:
	;send checksum
	lda		chksum
	sta		serout
	
	;set checksum sent flag
	mva		#$ff	chksnt
	
	;enable output complete IRQ and disable serial output IRQ
	lda		pokmsk
	ora		#$08
	and		#$ef
	sta		pokmsk
	sta		irqen
	
	pla
	rti
.endp

;==============================================================================
.proc SIOOutputCompleteHandler
	;check that we've sent the checksum
	lda		chksnt
	beq		xit
	
	;we're done sending the checksum
	sta		xmtdon
	
	;need to shut off this interrupt as it is not latched
	lda		pokmsk
	and		#$f7
	sta		pokmsk
	sta		irqen

xit:
	pla
	rti
.endp

;==============================================================================
.proc SIOCountdown1Handler
	;signal operation timeout
	mva		#0	timflg
	rts
.endp

;==============================================================================
.proc SIOCassette
	;check if it's read sector
	lda		dcomnd
	cmp		#$52
	beq		isread
	
	;check if it's put sector
	cmp		#$50
	beq		iswrite

	;nope, bail	
	ldy		#SIOErrorNAK
	jmp		SIO.xit
	
iswrite:
	jsr		SIOCassetteWriteFrame
	jmp		SIO.xit

isread:
	jsr		SIOCassetteReadFrame
	jmp		SIO.xit
.endp

;==============================================================================
.proc SIOCassetteWriteFrame
	;wait for pre-record write tone or IRG read delay
	ldx		#2
	jsr		CassetteWaitLongShortCheck

	;set up to transmit
	jsr		SIOSendEnable
	
	;setup buffer pointers
	jsr		SIOSetupBufferPointers
	
	;send data frame
	jsr		SIOSend
	
	;all done
	jmp		SIO.xit
.endp

;==============================================================================
.proc SIOCassetteReadFrame
	;wait for pre-record write tone or IRG read delay
	ldx		#4
	jsr		CassetteWaitLongShortCheck

	;set to 600 baud, turn on async read to shut off annoying tone
	mva		#$cc audf3
	mva		#$05 audf4
	lda		#0
	sta		audc4
	
	lda		sskctl
	and		#$8f
	ora		#$10
	sta		sskctl

	;set timeout (approx; no NTSC/PAL switching yet)
	mva		#$ff timflg
	lda		#1
	ldx		#>3600
	ldy		#<3600
	jsr		VBISetVector

	;wait for beginning of frame
	lda		#$10		;test bit 4 of SKSTAT
waitzerostart:
	bit		timflg
	bpl		timeout
	bit		skstat
	bne		waitzerostart
	
	;take first time measurement
	jsr		readtimer
	sty		timer1+1
	sta		timer1

	;wait for 19 bit transitions
	lda		#$10		;test bit 4 of SKSTAT
	ldx		#10			;test 10 pairs of bits
waitone:
	bit		timflg
	bpl		timeout
	bit		skstat
	beq		waitone
	dex
	beq		waitdone
waitzero:
	bit		timflg
	bpl		timeout
	bit		skstat
	bne		waitzero
	beq		waitone
	
timeout:
	ldy		#SIOErrorTimeout
	jmp		SIO.xit	

waitdone:
	
	;take second time measurement
	jsr		readtimer
	sta		timer2
	sty		timer2+1
	
	;compute baud rate and adjust pokey divisor
	;
	; counts = (pal ? 156 : 131)*rtdelta + vdelta;
	; lines = counts * 2
	; lines_per_bit = lines / 19
	; cycles_per_bit = lines_per_bit * 114
	; pokey_divisor = cycles_per_bit / 2 - 7
	;
	; -or-
	;
	; pokey_divisor = counts * 2 * 114 / 19 / 2 - 7
	;               = counts * 114 / 19 - 7
	;
	;16 bits at 600 baud is nominally 209 scanline pairs. This means that we
	;don't have to worry about more than two frames, which is at least 262
	;scanline pairs or less than 480 baud.
	
	;set frame height - 262 scanlines for NTSC, 312 for PAL
	ldx		#131
	lda		pal
	lsr
	sne:ldx	#156
	stx		temp3
	
	;compute line difference
	lda		timer1
	jsr		correct_time
	sta		bfenlo
	
	lda		timer2
	jsr		correct_time
	clc							;!! this decrements one line from the line delta
	sbc		bfenlo
	sta		bfenlo
	ldy		#0
	scs:dey
	
	;compute frame difference
	lda		timer2+1
	sub		timer1+1
	tax
	
	;accumulate frame difference
	beq		no_frames
	lda		bfenlo
add_frame_loop:
	clc
	adc		temp3
	scc:iny
	dex
	bne		add_frame_loop
no_frames:
	sty		bfenhi

	;compute lines*6 - 7 = (lines-1)*6 - 1
	asl							;(lines-1)*2 (lo)
	rol		bfenhi				;(lines-1)*2 (hi)
	sta		bfenlo
	ldy		bfenhi				;
	asl		bfenlo				;(lines-1)*4 (lo)
	rol		bfenhi				;(lines-1)*4 (hi)
	adc		bfenlo				;(lines-1)*6 (lo)
	tax							;
	tya							;
	adc		bfenhi				;(lines-1)*6 (hi) (and c=0)
	dex							;-1 line, bringing us to -7
	stx		audf3
	stx		cbaudl
	inx
	sne:sbc	#0
	sta		audf4
	sta		cbaudh
		
	;kick pokey into init mode to reset serial input shift hw
	ldx		sskctl
	txa
	and		#$fc
	sta		skctl
	
	;reset serial port status
	sta		skres
	
	;re-enable serial input hw
	stx		skctl
	
	jsr		SIOSetupBufferPointers
	
	;stuff two $55 bytes into the buffer, which we "read" above
	lda		#$55
	ldy		#0
	ldx		#2
aaloop:
	sta		(bufrlo),y
	inw		bufrlo
	dex:bne	aaloop
	
	;reset checksum for two $55 bytes and receive frame
	lda		#$aa
	sta		chksum
	
	jsr		SIOReceive.use_checksum
	jmp		SIOReceiveStop

;-------------------------------------------------------------------------
; We have to be VERY careful when reading (RTCLOK+2, VCOUNT), because
; the VBI can strike in between. First, we double-check RTCLOK+2 to see
; if it has changed. If so, we retry the read. Second, we check if
; VCOUNT=124, which corresponds to lines 248/249. This can correspond to
; either before or after the VBI -- with CRITIC off the VBI ends around
; (249, 20-50) -- so we don't know which side of the frame boundary we're
; on.
;
readtimer:
	ldy		rtclok+2
	lda		vcount
	cpy		rtclok+2
	bne		readtimer
	cmp		#124
	beq		readtimer
	rts
	
correct_time:
	sec
	sbc		#124
	bcs		time_ok
	adc		temp3
time_ok:
	rts
		
.endp

