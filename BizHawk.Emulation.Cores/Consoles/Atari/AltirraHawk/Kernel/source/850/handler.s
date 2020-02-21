;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement 850 Interface Firmware - R: Device Handler
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

;==========================================================================

CIOStatConcurrentNotEnabled = $97
CIOStatActiveConcurrent	= $99
CIOStatNotConcurrent	= $9A

siov	= $e459

;==========================================================================

.macro _hiop opcode adrmode operand
		.if :adrmode!='#'
		.error "Immediate addressing mode must be used with hi-opcode"
		.endif
		.if HIBUILD
		:opcode <:operand
		.else
		:opcode >:operand
		.endif
.endm

.macro _ldahi adrmode operand " "
		_hiop lda :adrmode :operand
.endm

.macro _ldxhi adrmode operand " "
		_hiop ldx :adrmode :operand
.endm

.macro _ldyhi adrmode operand " "
		_hiop ldy :adrmode :operand
.endm

.macro _adchi adrmode operand " "
		_hiop adc :adrmode :operand
.endm

.macro _orahi adrmode operand " "
		_hiop ora :adrmode :operand
.endm

.macro _eorhi adrmode operand " "
		_hiop eor :adrmode :operand
.endm

.macro _andhi adrmode operand " "
		_hiop and :adrmode :operand
.endm

.macro _sbchi adrmode operand " "
		_hiop sbc :adrmode :operand
.endm

.macro _cmphi adrmode operand " "
		_hiop cmp :adrmode :operand
.endm

.macro _cpxhi adrmode operand " "
		_hiop cpx :adrmode :operand
.endm

.macro _cpyhi adrmode operand " "
		_hiop cpy :adrmode :operand
.endm

;==========================================================================

		org		BASEADDR

;==========================================================================
		jmp		Init
		
.proc RDevTable
		dta		a(RDevOpen-1)
		dta		a(RDevClose-1)
		dta		a(RDevGetByte-1)
		dta		a(RDevPutByte-1)
		dta		a(RDevGetStatus-1)
		dta		a(RDevSpecial-1)
.endp

;==========================================================================
;
; Errors:
;	$84 (132) - if AUX1 mode is invalid (not read or write)
;
.proc RDevOpen
		;check if the unit is valid (1-4)
		ldx		icdnoz
		dex
		cpx		#4
		bcc		valid_unit
		
invalid_unit:
		ldy		#CIOStatUnkDevice
		rts
		
invalid_mode:
		ldy		#CIOStatInvalidCmd
		rts

valid_unit:
		;validate permissions -- must be at least read or write
		lda		icax1z
		and		#$0c
		beq		invalid_mode
		
		;stash permission flags for this channel
		lda		icax1z
		sta		serialPerms,x
		
		;reset handler-side state for the new channel
		lda		#0
		sta		serialOutTail,x
		sta		serialErrors,x
		ldy		#1
		rts
.endp

;==========================================================================
.proc RDevClose
		;check if the channel being closed is the current concurrent
		;I/O channel
		ldx		icdnoz
		cpx		serialConcurrentNum
		bne		not_concurrent
		
		;yes -- wait for output to flush
wait_loop:
		;check if concurrent I/O stops on its own (break key)
		lda		serialConcurrentNum
		beq		concurrent_stopped

		;wait for output buffer to drain
		lda		serialOutIdle
		bpl		wait_loop

		;wait for transmission to finish (only valid after SEROR)
		lda		irqst
		and		#$08
		bne		wait_loop

		;stop concurrent I/O
		jsr		SerialEndConcurrent
		
concurrent_stopped:
		ldy		#1
		rts

not_concurrent:
		;flush pending output, if any, and exit
		jmp		RDevXio32
.endp

;==========================================================================
RDevGetByte = _RDevGetByte._entry
.proc _RDevGetByte
not_concurrent:
		ldy		#CIOStatNotConcurrent
		rts

is_break:
		ldy		#CIOStatBreak
		rts

_entry:
		ldx		icdnoz
		cpx		serialConcurrentNum
		bne		not_concurrent
		
		sei
wait_loop:
		lda		brkkey
		cli
		beq		is_break
		sei
		lda		serialInSpaceLo
		cmp		serialInSize
		bne		wait_done
		lda		serialInSpaceHi
		cmp		serialInSize+1
		beq		wait_loop
wait_done:

		lda		$ffff
inputPtrHead = *-2
		sta		ciochr
		
		inc		serialInSpaceLo
		sne:inc	serialInSpaceHi
		cli
		
		inw		inputPtrHead
		lda		inputPtrHead
		cmp		SerialInputIrqHandler.inBufEndLo
		bne		no_wrap
		lda		inputPtrHead+1
		cmp		SerialInputIrqHandler.inBufEndHi
		bne		no_wrap
		mva		SerialInputIrqHandler.inBufLo inputPtrHead
		mva		SerialInputIrqHandler.inBufHi inputPtrHead+1

no_wrap:
		;check for parity options
		lda		serialXlatMode-1,x
		and		#$0c
		beq		no_parity
		lsr
		lsr
		tay
		dey
		cpy		#2
		beq		clear_parity
		
		jsr		SerialComputeParity
		eor		ciochr
		bmi		parity_error

clear_parity:		
		;mask off MSB
		asl		ciochr
		lsr		ciochr
no_parity:

		;check for translation
		lda		serialXlatMode-1,x
		and		#$30
		cmp		#$20
		bcs		done
		tay
		
		lda		ciochr
		and		#$7f
		cmp		#$0d
		beq		receive_cr
		
		;check for light translation
		cpy		#0
		beq		done_2

		;heavy translation - reject if not in $20-7C
		cmp		#$20
		bcc		reject_char
		cmp		#$7d
		bcc		done_2
reject_char:
		lda		serialXlatChar-1,x
		jmp		done_2
done:
		lda		ciochr
done_2:
		ldy		#1
		rts

receive_cr:
		lda		#$9b
		bne		done_2
		
parity_error:
		lda		#$20
		sei
		ora		serialErrors-1,x
		sta		serialErrors-1,x
		cli
		jmp		wait_loop
.endp

;==========================================================================
.proc RDevPutByte
		;stash unit number and character
		sta		ciochr
		lda		icdno,x
		sta		icdnoz
		tax

		;check translation mode
		lda		serialXlatMode-1,x
		and		#$20
		beq		xlat_enabled
		
		lda		ciochr
		jmp		put_byte
		
is_eol:
		;convert EOL to CR
		lda		#$0d
		bne		put_byte_0

heavy_xlat_reject:
		ldy		#1
		rts
		
xlat_enabled:
		;check for an EOL
		lda		ciochr
		cmp		#$9b
		beq		is_eol

		;check if heavy translation is enabled
		lda		serialXlatMode-1,x
		and		#$10
		beq		light_xlat
		
		;check if character is out of range
		lda		ciochr
		cmp		#$20
		bcc		heavy_xlat_reject
		cmp		#$7d
		bcs		heavy_xlat_reject
		
light_xlat:
		lda		ciochr
		and		#$7f
put_byte_0:
		sta		ciochr
put_byte:
		;check for CR
		cmp		#$0d
		bne		put_byte_2
		
		;check if append LF mode is on
		lda		serialXlatMode-1,x
		and		#$40
		beq		put_byte_3
		
		;send CR then LF
		jsr		put_byte_3
		lda		#$0a
put_byte_2:
		sta		ciochr
put_byte_3:
		lda		serialXlatMode-1,x
		and		#$03
		tay
		dey
		bmi		put_loop
		jsr		SerialComputeParity
		sta		ciochr

put_loop:
		lda		ciochr
		
		;check if we have break
		ldy		brkkey
		beq		is_break
		
		;check if we have a conflict on concurrent mode
		ldy		serialConcurrentNum
		beq		not_concurrent
		
		;check if this is the right channel
		cpy		icdnoz
		bne		concurrent_conflict
		
		;check if buffer is full
		ldy		#$20
		cpy		SerialOutputIrqHandler.outLevel
		beq		put_loop
		
		;enter critical section
		sei
		
		;check if output is idle
		bit		serialOutIdle
		bmi		output_idle

		;put byte		
		ldy		serialOutTail-1,x
		sta		$ffff,y
outBuf = *-2
		iny
		tya
		and		#$1f
		sta		serialOutTail-1,x
		inc		SerialOutputIrqHandler.outLevel
		bne		concurrent_output_complete
		
output_idle:
		sta		serout
		lsr		serialOutIdle
concurrent_output_complete:

		;leave critical section
		cli
		
		;all done
		ldy		#1
		rts

is_break:
		ldy		#CIOStatBreak
		rts
		
concurrent_conflict:
		ldy		#CIOStatActiveConcurrent
		rts

not_concurrent:
		;save byte to write
		pha

		;load and increment tail
		lda		serialOutTail-1,x
		inc		serialOutTail-1,x

		;check if we are filling buffer and set carry flag
		cmp		#$20

		;compute unified buffer offset
		ora		serialOutBufOffsets-1,x

		;write byte to buffer
		tay
		pla
		sta		outputBuffer0,y
		
		;flush if we filled the buffer
		bcs		force_flush

		;flush if we wrote a CR
		cmp		#$0d
		beq		force_flush

		;all done
		ldy		#1
		rts
		
force_flush:
		jmp		RDevXio32
.endp

;==========================================================================
.proc SerialComputeParity
		lda		ciochr
		cpy		#2			;check for mark parity
		beq		mark_parity
		asl
		eor		ciochr
		and		#%10101010		;pairwise sums in odd bits
		adc		#%01100110		;add bit 5 to 7 and bit 1 to 3
		and		#%10001000		;mask to bits 3 and 7
		adc		#%01111000		;parity in bit 7
		and		#$80			;mask to parity bit
		eor		ciochr			;set even parity
		dey
		bpl		even_parity
		eor		#$80
even_parity:
		rts
		
mark_parity:
		ora		#$80
		rts
.endp

;==========================================================================
; Get Status function
;
; Output:
;	DVSTAT+0	Error flags
;				D7		Framing error (C/I)
;				D6		Overrun error (C)
;				D5		Parity error (C)
;				D4		Input buffer overflow error (C)
;				D3		Illegal option error (C)
;				D2		External device not ready error (C)
;				D1		Interface error (I)
;				D0		SIO error (C)
;				(I) = from Interface Module
;				(C) = from computer, cleared by STATUS
;
;	DVSTAT+1	Input level low byte (concurrent) or control state (block)
;	DVSTAT+2	Input level high byte
;	DVSTAT+3	Output level
;
.proc RDevGetStatus
		ldy		serialConcurrentNum
		beq		not_concurrent
		
		;retrieve buffer status
		sei
		
		sec
		lda		serialInSize
		sbc		serialInSpaceLo
		sta		dvstat+1
		lda		serialInSize+1
		sbc		serialInSpaceHi
		sta		dvstat+2
		
		lda		SerialOutputIrqHandler.outLevel
		sta		dvstat+3
		
		lda		skstat
		sta		skres
		
		cli
		
		;framing error is already in bit 7 where we need it, but overrun
		;needs to be moved from bit 5 to bit 6.
		and		#$a0
		adc		#$20
		and		#$c0
		eor		#$c0

		jmp		post_concurrent_check
				
not_concurrent:
		mwa		#dvstat dbuflo
		lda		#'S'
		ldx		#$40
		ldy		#2
		jsr		SerialDoIo
		
		lda		dvstat

post_concurrent_check:
		;merge in handler-detected serial port errors
		ldx		icdnoz
		ora		serialErrors-1,x
		sta		dvstat
		lda		#0
		sta		serialErrors-1,x
		
		;all done
		ldy		#1
		rts
.endp

;==========================================================================
; XIO 32	Force short block
; XIO 34	Set control lines
; XIO 36	Set baud rate, word size, stop bits, and ready monitoring
; XIO 38	Set translation modes and parity
; XIO 40	Start concurrent mode I/O
;
.proc RDevSpecial
		lda		iccomz
		tax
		lsr
		bcs		not_supported
		cmp		#$10
		bcc		not_supported
		cmp		#$15
		bcs		not_supported
		jsr		dispatch
not_supported:
		;restore ICAX1 so GET ops still work after this call
		ldx		icdnoz
		mva		serialPerms-1,x icax1z
		rts

dispatch:
		lda		com_tab-31,x
		pha
		lda		com_tab-32,x
		pha
		ldx		icdnoz
		rts
		
com_tab:
		dta		a(RDevXio32-1)
		dta		a(RDevXio34-1)
		dta		a(RDevXio36-1)
		dta		a(RDevXio38-1)
		dta		a(RDevXio40-1)
.endp

;==========================================================================
.proc RDevXio32
		ldy		serialOutTail-1,x
		beq		buf_empty
		
		;flush buffer to device
		sty		daux1
		lda		serialOutBufOffsets-1,x
		clc
		adc		#<outputBuffer0
		sta		dbuflo
		lda		#0
		sta		daux2
		sta		serialOutTail-1,x
		_ldahi	#outputBuffer0
		sta		dbufhi
		
		lda		#'W'
		ldx		#$80
		ldy		#$40			;always send 64 bytes regardless of xmitlen
		jmp		SerialDoIo

buf_empty:
		ldy		#1
		rts
.endp

;==========================================================================
; XIO 34	Set control lines
;
.proc RDevXio34
		lda		#'A'
		jmp		SerialDoIoSimple
.endp

;==========================================================================
; XIO 36	Set baud rate, word size, stop bits, and ready monitoring
;
; Input:
;	ICAX1Z
;		D7=1	Enable two stop bits
;		D4:D5	Word size
;		D0:D3	Baud rate
;
;	ICAX2Z
;		D2=1	Monitor DSR
;		D1=1	Monitor CTS
;		D0=1	Monitor CRX
;
.proc RDevXio36
		;stash stop bit mode
		lda		icax1z
		sta		serial2SBMode-1,x

		lda		#'B'
		jmp		SerialDoIoSimple
.endp

;==========================================================================
; XIO 38	Set translation modes and parity
;
.proc RDevXio38
		;stash mode
		lda		icax1z
		sta		serialXlatMode-1,x
		
		;stash won't translate char
		mva		icax2z serialXlatChar-1,x
		
		;all done
		ldy		#1
		rts
.endp

;==========================================================================
; XIO 40	Start concurrent mode I/O
;
; Errors:
;	$85 (133) - if attempted to soft-opened unit
;	$97 (151) - if attempted on port without concurrent option
;	$99 (153) - if concurrent I/O is already active, even to the same unit
;
.proc RDevXio40
		lda		serialConcurrentNum
		beq		not_concurrent
		
		ldy		#CIOStatActiveConcurrent
		rts
		
not_enabled:
		ldy		#CIOStatConcurrentNotEnabled
		rts

not_concurrent:
		;check if concurrent I/O was enabled on this port
		lda		serialPerms-1,x
		ror
		bcc		not_enabled

		;all good, let's enter concurrent mode!
		lda		#$ff
		sta		serialOutIdle
		
		lda		#0
		sta		SerialOutputIrqHandler.outLevel
		sta		SerialOutputIrqHandler.outIndex
		sta		serialOutTail-1,x

		;check if we are using the internal buffer
		lda		icax1z
		bne		external_input_buffer
		
internal_input_buffer:
		;set input length
		lda		#$20
		sta		serialInSize
		sta		serialInSpaceLo
		lda		#0
		sta		serialInSize+1
		sta		serialInSpaceHi
		
		lda		#<inputBuffer
		_ldyhi	#inputBuffer		
		bne		post_input_buffer

external_input_buffer:
		;check for buffer length 0, which also selects internal buffer
		;(undocumented, but relied on by BobTerm)
		lda		icbllz
		bne		external_buflen_nonzero
		ldy		icblhz
		beq		internal_input_buffer

external_buflen_nonzero:
		;copy input length
		sta		serialInSize
		sta		serialInSpaceLo
		ldy		icblhz
		sty		serialInSize+1
		sty		serialInSpaceHi

		;copy input pointer
		lda		icbalz
		ldy		icbahz
		
post_input_buffer:
		;(A,Y) -> inBufLo/inBufHi and inputPtrHead
		sta		_RDevGetByte.inputPtrHead
		sta		SerialInputIrqHandler.inBufLo
		sta		SerialInputIrqHandler.inPtr
		sty		_RDevGetByte.inputPtrHead+1
		sty		SerialInputIrqHandler.inBufHi
		sty		SerialInputIrqHandler.inPtr+1

		clc
		adc		serialInSize
		sta		SerialInputIrqHandler.inBufEndLo
		tya
		adc		serialInSize+1
		sta		SerialInputIrqHandler.inBufEndHi

		;setup hardcoded addresses and DCB				
		lda		#<outputBuffer0
		sta		dbuflo
		clc
		adc		serialOutBufOffsets-1,x
		sta		SerialOutputIrqHandler.outBuf
		sta		RDevPutByte.outBuf
		_ldahi	#outputBuffer0
		sta		dbufhi
		adc		#0
		sta		SerialOutputIrqHandler.outBuf+1
		sta		RDevPutByte.outBuf+1

		;attempt to kick port into concurrent mode
		lda		icax1z
		sta		daux1
		lda		#'X'
		ldx		#$40
		ldy		#9
		jsr		SerialDoIo
		bmi		fail
		
		;init POKEY
		ldx		#8
		mva:rpl	outputBuffer0,x $d200,x-
		
		;mark concurrent mode active		
		sei
		ldx		icdnoz
		stx		serialConcurrentNum

		;select one/two stop bit serial routines
		ldy		#5
		lda		serial2SBMode-1,x
		spl:ldy	#11

		;swap in interrupt handlers
		ldx		#5
copy_loop:
		mva		vserin,x serialVecSave,x
		mva		serialVecs,y vserin,x
		dey
		dex
		bpl		copy_loop
		
		mwa		brkky brkVecSave
		mwa		brk_vec brkky
		
		;switch serial port to channel 4 async recv, channel 2 xmit
		lda		sskctl
		and		#$07
		ora		#$70
		sta		sskctl
		sta		skctl
		
		;enable serial input and output ready IRQs
		lda		pokmsk
		ora		#$30
		sta		pokmsk
		sta		irqen
		cli

		;all done
		ldy		#1
fail:
		rts
		
brk_vec:
		dta		a(BreakHandler)
.endp

;==========================================================================
;
; Input:
;	A = SIO command
;	X = SIO input/output flag
;	Y = Byte count low
;	ICDNOZ = device unit (1-4)
;	DBUFLO/DBUFHI = dest buffer (if Y>0)
;
.proc SerialDoIoSimple
		ldx		icax1z
		ldy		icax2z
.def :SerialDoIo_AUX_XY = *
		stx		daux1
		sty		daux2
		ldx		#0
		ldy		#0
.def :SerialDoIo = *
		sta		dcomnd
		stx		dstats
		sty		dbytlo
		mva		#$50 ddevic
		mva		icdnoz dunit
		mva		#$08 dtimlo
		mva		#0 dtimlo+1
		sta		dbythi
		jsr		siov
		bpl		done
		
		;check for device error
		cpy		#$90
		bne		done2
		
		;set device error flag
		ldx		icdnoz
		lda		#1
		ora		serialErrors-1,x
		sta		serialErrors-1,x
done2:
		tya
done:
		rts
.endp

;==========================================================================
; SerialEndConcurrent
;
; Terminates concurrent I/O.
;
; Note that this can be called from interrupt via the BREAK key, and so
; it must be thread-safe.
;
; Used: A, X only; Y not touched
;
.proc SerialEndConcurrent
		;enter critical section
		php
		sei
		
		;check if concurrent I/O is active (needed due to possible race
		;with BREAK key)
		lda		serialConcurrentNum
		beq		not_active
		
		;disable serial interrupts
		lda		pokmsk
		and		#$c7
		sta		pokmsk
		sta		irqen

		;restore interrupt vectors
		ldx		#5
		mva:rpl	serialVecSave,x vserin,x-
		
		mwa		brkVecSave brkky
		
		cli
		
		;clear concurrent index
		inx
		stx		serialConcurrentNum
		
		;issue a dummy write to the 850 to terminate concurrent mode
		;(anything that activates the command line will do).
		lda		#'W'		;write block command
		ldx		#0			;no data frame
		ldy		#0			;zero bytes
		cli
		jsr		SerialDoIo_AUX_XY
		sei
		
not_active:
		
		;leave critical section
		plp
		
		;all done
		rts
.endp

;==========================================================================
.proc SerialInputIrqHandler
		;check if we have space in the buffer
		lda		#0
.def :serialInSpaceLo = *-1
		bne		not_full
		lda		#0
.def :serialInSpaceHi = *-1
		beq		is_full
		
not_full:
		;read char and store it in the buffer
		lda		serin
		sta		$ffff
inPtr = *-2

		;bump write (tail) pointer
		inw		inPtr
		lda		inPtr
		cmp		#0
inBufEndLo = *-1
		bne		no_wrap
		lda		inPtr+1
		cmp		#0
inBufEndHi = *-1
		bne		no_wrap
		lda		#0
inBufLo = *-1
		sta		inptr
		lda		#0
inBufHi = *-1
		sta		inPtr+1
no_wrap:
		;decrement space level in buffer
		lda		serialInSpaceLo
		sne:dec	serialInSpaceHi
		dec		serialInSpaceLo
		
xit:
		pla
		rti
		
is_full:
		;set overflow error status (bit 4)
		txa
		pha
		ldx		serialConcurrentNum
		lda		#$10
		ora		serialErrors-1,x
		sta		serialErrors-1,x
		pla
		tax
		jmp		xit
.endp

;==========================================================================
; Serial output ready IRQ handler for two stop bits.
;
.proc SerialOutputIrqHandler2SB
		;turn on complete IRQ
		lda		pokmsk
		ora		#$08
		sta		pokmsk
		sta		irqen
		pla
		rti
.endp

;==========================================================================
; Serial output complete IRQ handler for two stop bits.
;
.proc SerialCompleteIrqHandler2SB
		;turn off complete IRQ
		lda		pokmsk
		and		#$f7
		sta		pokmsk
		sta		irqen

		;fall through!
.endp

;==========================================================================
; Serial output ready IRQ handler for one stop bit.
;
.proc SerialOutputIrqHandler
		lda		#0
outLevel = *-1
		beq		is_empty
		dec		outLevel
		txa
		pha
		ldx		#0
outIndex = *-1
		lda		$ffff,x
outBuf = *-2
		sta		serout
		inx
		txa
		and		#$1f
		sta		outIndex
		pla
		tax
xit:
.def :SerialCompleteIrqHandler = *
		pla
		rti
is_empty:
		sec
		ror		serialOutIdle
		bne		xit
.endp

;==========================================================================
.proc BreakHandler
		;terminate concurrent I/O
		txa
		pha
		jsr		SerialEndConcurrent
		pla
		tax
		
		;jump through old BREAK vector (already restored)
		jmp		(brkky)
.endp

;==========================================================================
;
; Note that DOS 2.0's AUTORUN.SYS does some pretty funky things here -- it
; jumps through (DOSINI) after loading the handler, but that must NOT
; actually invoke DOS's init, or the EXE loader hangs. Therefore, we have
; to check whether we're handling a warmstart, and if we're not, we have
; to return without chaining.
;
.proc Reinit
		;install CIO handler
		ldx		#0
hatabs_loop:
		lda		hatabs,x
		beq		found_hatabs_slot
		inx
		inx
		inx
		cpx		#35
		bne		hatabs_loop
		
		;oops, no slots
		sec
		rts
		
found_hatabs_slot:
		ldy		#$fd
copy_entry:
		mva		dev_entry-$fd,y hatabs,x+
		iny
		bne		copy_entry
		
		;adjust MEMLO
		lda		#<bss_end
		sta		memlo
		_ldahi	#bss_end
		sta		memlo+1

		lda		#0
		ldx		#serialClearEnd-serialClearBegin-1
		sta:rpl	serialClearBegin,x-

		;check if this is a warmstart, and don't call into DOS if not
		lda		warmst
		beq		skip_chain
		
		jmp		$ffff
dosini_chain = *-2

skip_chain:
		rts

dev_entry:
		dta		'R'
		dta		a(RDevTable)
.endp

;==========================================================================
serialVecs:
		dta		a(SerialInputIrqHandler)
		dta		a(SerialOutputIrqHandler)
		dta		a(SerialCompleteIrqHandler)

serialVecs2SB:
		dta		a(SerialInputIrqHandler)
		dta		a(SerialOutputIrqHandler2SB)
		dta		a(SerialCompleteIrqHandler2SB)

serialOutBufOffsets:
		dta		$00,$20,$40,$60

;==========================================================================
bss_start = *

.proc Init		
		;hook DOSINI
		mwa		dosini Reinit.dosini_chain
		
		lda		#<Reinit
		sta		dosini
		_ldahi	#Reinit
		sta		dosini+1
		
		;all done
		clc
		rts
.endp

;==========================================================================

		org		bss_start
serialOutIdle	.ds		1
serialInSize	.ds		2
serialPerms		.ds		4
serialVecSave	.ds		6
serialOutTail	.ds		4
serialErrors	.ds		4

;these are cleared together
serialClearBegin = *
serial2SBMode	.ds		4		;two stop bits flag (bit 7)
serialXlatMode	.ds		4		;translation/parity
serialXlatChar	.ds		4
serialConcurrentNum	.ds	1
serialClearEnd = *

brkVecSave		.ds		2
inputBuffer		.ds		32
outputBuffer0	.ds		32
outputBuffer1	.ds		32
outputBuffer2	.ds		32
outputBuffer3	.ds		32

bss_end = outputBuffer3 + $20

;==========================================================================

		run		Init
