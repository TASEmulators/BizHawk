;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement SX212 R: Device Handler
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
		beq		valid_unit
		
invalid_unit:
		ldy		#CIOStatUnkDevice
		rts
		
invalid_mode:
		ldy		#CIOStatInvalidCmd

valid_unit:
		;validate permissions -- must be at least read or write
		lda		icax1z
		and		#$0c
		beq		invalid_mode
		
		;stash permission flags for this channel
		lda		icax1z
		sta		serialPerms
		
		;reset handler-side state for the new channel
		ldy		#0
		sty		serialErrors
		iny
		rts
.endp

;==========================================================================
.proc RDevClose
		;check if we are in concurrent mode
		bit		serialConcurrent
		bpl		not_concurrent
		
		;yes -- wait for output to flush
		jsr		SerialFlushOutput

		;stop concurrent I/O
		jsr		SerialEndConcurrent
		
not_concurrent:
		ldy		#1
		rts
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
		bit		serialConcurrent
		bpl		not_concurrent
		
		sei
wait_loop:
		lda		brkkey
		cli
		beq		is_break
		sei
		lda		serialInSpace
		cmp		#$ff
		beq		wait_loop

		ldy		#0
input_idx = *-1
		lda		inputBuffer,y
		sta		ciochr
		
		inc		serialInSpace
		cli
		
		inc		input_idx

		;check for parity options
		lda		serialXlatMode
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
		lda		serialXlatMode
		clc
		adc		#$e0
		bcs		done
		tay
		
		lda		ciochr
		and		#$7f
		cmp		#$0d
		beq		receive_cr
		
		;check for heavy translation
		cpy		#0
		beq		done_2
		cmp		#$20
		bcc		reject_char
		cmp		#$7d
		bcc		done_2
reject_char:
		lda		serialXlatChar
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
		ora		serialErrors
		sta		serialErrors
		cli
		jmp		wait_loop
.endp

;==========================================================================
.proc RDevPutByte
		;check if concurrent mode
		bit		serialConcurrent
		bpl		not_concurrent

		;stash character
		sta		ciochr

		;check translation mode
		lda		serialXlatMode
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
		
not_concurrent:
		ldy		#CIOStatNotConcurrent
		rts

xlat_enabled:
		;check for an EOL
		lda		ciochr
		cmp		#$9b
		beq		is_eol

		;check if heavy translation is enabled
		lda		serialXlatMode
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
		lda		serialXlatMode
		and		#$40
		beq		put_byte_3
		
		;send CR then LF
		jsr		put_byte_3
		lda		#$0a
put_byte_2:
		sta		ciochr
put_byte_3:
		lda		serialXlatMode
		and		#$03
		tay
		dey
		bmi		put_loop
		jsr		SerialComputeParity
		sta		ciochr

put_loop:
		;check if we have break
		ldy		brkkey
		beq		is_break
		
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
		lda		#0
.def :serialOutTail = *-1
		and		#$3f
		tay
		lda		ciochr
		sta		outputBuffer,y
		inc		serialOutTail
		inc		SerialOutputIrqHandler.outLevel
		bne		concurrent_output_complete
		
output_idle:
		lda		ciochr
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
;				D7		Framing error
;				D6		Overrun error
;				D4		Input buffer overflow error
;
;	DVSTAT+1	Input level low byte (concurrent) or control state (non-c)
;				
;	DVSTAT+2	Input level high byte (0)
;	DVSTAT+3	Output level
;
.proc RDevGetStatus
		bit		serialConcurrent
		bpl		not_concurrent
		
		;retrieve buffer status
		php
		sei
		
		sec
		lda		serialInSpace
		eor		#$ff
		sta		dvstat+1
		lda		#0
		sta		dvstat+2
		
		lda		SerialOutputIrqHandler.outLevel
		sta		dvstat+3
		
		lda		skstat
		sta		skres
		
		plp
		
		;framing error is already in bit 7 where we need it, but overrun
		;needs to be moved from bit 5 to bit 6.
		and		#$a0
		adc		#$20
		and		#$c0
		eor		#$c0

		jmp		post_concurrent_check
				
not_concurrent:
		ldx		serialStatus
		lda		skstat
		and		#$10
		seq:inx
		stx		dvstat+1

		;reset carrier detect sticky bit (01->00, 10->11)
		txa
		clc
		adc		#4
		lsr
		and		#4
		eor		serialStatus
		sta		serialStatus

		lda		#0

post_concurrent_check:
		;merge in handler-detected serial port errors
		ora		serialErrors
		sta		dvstat
		lda		#0
		sta		serialErrors
		
		;all done
		ldy		#1
		rts
.endp

;==========================================================================
; XIO 34	Force hangup
; XIO 36	Set baud rate
; XIO 38	Set translation modes and parity
; XIO 40	Start concurrent mode I/O
;
.proc RDevSpecial
		lda		iccomz
		tax
		lsr
		bcs		not_supported
		cmp		#$11
		bcc		not_supported
		cmp		#$15
		bcs		not_supported
		jsr		dispatch
not_supported:
		;restore ICAX1 so GET ops still work after this call
		mva		serialPerms icax1z
		rts

dispatch:
		lda		com_tab-33,x
		pha
		lda		com_tab-34,x
		pha
		rts
		
com_tab:
		dta		a(RDevXio34-1)
		dta		a(RDevXio36-1)
		dta		a(RDevXio38-1)
		dta		a(RDevXio40-1)
.endp

;==========================================================================
; XIO 34	Hang up
;
.proc RDevXio34
		;must not be in concurrent mode
		bit		serialConcurrent
		bpl		not_concurrent

		ldy		#CIOStatActiveConcurrent
		rts

not_concurrent:
		;AUX1 must be 128 per SX212 docs
		lda		icax1z
		cmp		#$80
		bne		done

		;check if carrier is active
		lda		serialStatus
		and		#8
		bne		carrier_active

done:
		ldy		#1
error:
		rts

carrier_active:
		;enter concurrent mode temporarily
		jsr		SerialBeginConcurrent

		;escape to command mode
		jsr		SerialEnterCommandMode

		;send ATH
		ldx		#modem_commands.ath-modem_commands
		jsr		SerialSendModemCommand

		jsr		SerialFlushOutput

		;exit concurrent mode
		jsr		SerialEndConcurrent

		;all done
		jmp		done
.endp

;==========================================================================
; XIO 36	Set baud rate
;
; Input:
;	ICAX1Z
;		D0:D3	Baud rate (>=10 sets 1200 baud0
;
.proc RDevXio36
		;set hi speed if rate >= 10 (850's 1200 baud speed)
		lda		icax1z
		and		#$0f
		cmp		#10
		ror		serialHiSpeed

		;update POKEY if we are in concurrent mode
		bit		serialConcurrent
		bpl		not_concurrent

not_concurrent:
		ldy		#1
		rts
.endp

;==========================================================================
; XIO 38	Set translation modes and parity
;
.proc RDevXio38
		;stash mode
		mva		icax1z serialXlatMode
		
		;stash won't translate char
		mva		icax2z serialXlatChar
		
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
		lda		serialConcurrent
		bpl		not_concurrent
		
		ldy		#CIOStatActiveConcurrent
		rts
		
not_enabled:
		ldy		#CIOStatConcurrentNotEnabled
		rts

not_concurrent:
		;check if concurrent I/O was enabled on this port
		lda		serialPerms
		ror
		bcc		not_enabled

		;all good, let's enter concurrent mode!
		jsr		SerialBeginConcurrent

		;all done
		ldy		#1
fail:
		rts
.endp


;==========================================================================
; SerialBeginConcurrent
;
; Starts concurrent I/O.
;
.proc SerialBeginConcurrent
		lda		#$ff
		sta		serialOutIdle
		
		lda		#0
		sta		SerialOutputIrqHandler.outLevel
		sta		SerialOutputIrqHandler.outIndex
		sta		SerialInputIrqHandler.in_offset
		sta		_RDevGetByte.input_idx
		sta		serialOutTail
		
		;mark entire input buffer free
		lda		#$ff
		sta		serialInSpace
		
		;init POKEY
		ldy		#8
		bit		serialHiSpeed
		spl:ldy	#17
		ldx		#8
		mva:rpl	pokey_regs,y- $d200,x-

		;enter critical section
		php
		sei

		ldx		#3
copy_loop:
		mva		vserin,x serialVecSave,x
		mva		serialVecs,x vserin,x
		dex
		bpl		copy_loop
				
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

		;assert motor line to enable comm from SX212
		lda		pactl
		and		#$c7
		ora		#$30
		sta		pactl

		;set concurrent flag
		sec
		ror		serialConcurrent

		;exit critical section
		plp

		;all done
		rts

pokey_regs:
		dta		<$0ba0,$a0
		dta		>$0ba0,$a0
		dta		<$0ba0,$a0
		dta		>$0ba0,$a0
		dta		$78
		dta		<$02e3,$a0
		dta		>$02e3,$a0
		dta		<$02e3,$a0
		dta		>$02e3,$a0
		dta		$78
.endp

;==========================================================================
; SerialEndConcurrent
;
; Terminates concurrent I/O.
;
; Used: A, X only; Y not touched
;
.proc SerialEndConcurrent
		;enter critical section
		php
		sei
		
		;check if concurrent I/O is active
		bit		serialConcurrent
		bpl		not_active
		
		;disable serial interrupts
		lda		pokmsk
		and		#$c7
		sta		pokmsk
		sta		irqen

		;restore interrupt vectors
		ldx		#3
restore_vecs:
		mva:rpl	serialVecSave,x vserin,x-
		
		cli
		
		;clear concurrent flag
		lsr		serialConcurrent

		;deassert the motor line		
		lda		pactl
		ora		#$38
		sta		pactl
		
not_active:
		
		;leave critical section
		plp
		
		;all done
		rts
.endp

;==========================================================================
; Wait for the output buffer to drain and for all bytes to be transmitted.
;
.proc SerialFlushOutput
		lda		#8
wait_loop:
		ldy		brkkey
		beq		is_break
		bit:rpl	serialOutIdle
		bit		irqst
		bne		wait_loop
		ldy		#1
		rts

is_break:
		ldy		#CIOStatBreak
		rts
.endp

;==========================================================================
.proc SerialInputIrqHandler
		;check if we have space in the buffer
		lda		#0
.def :serialInSpace = *-1
		beq		is_full
		
not_full:
		;read char and store it in the buffer
		txa
		pha
		ldx		#0
in_offset = *-1
		lda		serin
		sta		inputBuffer,x
		pla
		tax

		;bump write (tail) pointer
		inc		in_offset

		;decrement space level in buffer
		dec		serialInSpace
		
xit:
		pla
		rti
		
is_full:
		;set overflow error status (bit 4)
		lda		#$10
		ora		serialErrors
		sta		serialErrors
		pla
		rti
.endp

;==========================================================================
.proc SerialOutputIrqHandler
		lda		#0
outLevel = *-1
		beq		is_empty
		dec		outLevel
		txa
		pha
		ldx		#0
outIndex = *-1
		lda		outputBuffer,x
		sta		serout
		inx
		txa
		and		#$1f
		sta		outIndex
		pla
		tax
xit:
		lda		#$ef
		sta		irqen
		lda		pokmsk
		sta		irqen
		pla
		rti
is_empty:
		sec
		ror		serialOutIdle
		bne		xit
.endp

;==========================================================================
; SIO proceed line handler
;
; Activated by the SX212 according to carrier detect status.
;
.proc SerialProceedHandler
		;invert edge detection direction on PIA
		lda		pactl
		eor		#$02
		sta		pactl

		;update carrier detect bits
		;     0   1
		; 00  00  10
		; 01  01  10
		; 10  01  10
		; 11  01  11
		and		#2
		beq		carrier_off

		lda		serialStatus
		cmp		#$0c
		bcs		done
		and		#$03
		ora		#$08
		bne		update

carrier_off:
		lda		serialStatus
		cmp		#$04
		bcc		done
		and		#$03
		ora		#$04
update:
		sta		serialStatus

done:
		;all done
		pla
		rti
.endp

;==========================================================================
; SIO interrupt line handler
;
; Activated by the SX212 according to modem speed status.
;
.proc SerialInterruptHandler
		;invert edge detection direction on PIA
		lda		pbctl
		eor		#$02
		sta		pbctl

		;update speed bit in status
		eor		serialStatus
		and		#2
		eor		serialStatus
		sta		serialStatus

		;all done
		pla
		rti
.endp

;==========================================================================
.proc SerialEnterCommandMode
		;wait for at least one second
		jsr		wait_second

		;flush input
		php
		sei
		lda		SerialInputIrqHandler.in_offset
		sta		_RDevGetByte.input_idx
		lda		#$ff
		sta		serialInSpace
		plp

		;issue escape to command mode
		jsr		send_plusplusplus
		jsr		SerialFlushOutput

		;wait for at least one second again and then exit
		jmp		wait_second

wait_second:
		lda		rtclok+2
		clc
		adc		#70				;a little bit more than 1s just to be sure
		cmp:rne	rtclok+2
		rts

send_plusplusplus:
		jsr		send_plus
send_plusplus:
		jsr		send_plus
send_plus:
		lda		#'+'
		jmp		RDevPutByte
.endp

;==========================================================================
.proc SerialSendModemCommand
		stx		index
next_byte:
		ldx		#0
index = *-1
		inc		index
		lda		modem_commands,x
		pha
		and		#$7f
		jsr		RDevPutByte
		pla
		bpl		next_byte
		rts
.endp

;==========================================================================
.proc modem_commands
ath:
		dta		'ATH',$8D

ats15:
		dta		'ATS15?',$8D

ato:
		dta		'ATO',$8D
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

		;clear BSS
		lda		#0
		ldx		#serialClearEnd-serialClearBegin-1
		sta:rpl	serialClearBegin,x-

		;hook proceed/interrupt vectors
		ldx		#3
copy_loop:
		mva		vprced,x serialVecSave2,x
		mva		serialVecs+4,x vprced,x
		dex
		bpl		copy_loop

		;enable both proceed and interrupt IRQs on negative edges
		php
		sei

		lda		pactl
		and		#$fd
		ora		#$01
		sta		pactl
		
		lda		pbctl
		and		#$fd
		ora		#$01
		sta		pbctl

		plp

		;set up timeout for the whole modem sync operation; note that we
		;have two seconds of wait involved
		ldx		rtclok+2
		dex
		stx		timeout_val

		;enable transmission
		jsr		SerialBeginConcurrent

modem_sync_retry:
		;jump to command mode
		jsr		SerialEnterCommandMode

		;send query command
		ldx		#modem_commands.ats15-modem_commands
		jsr		SerialSendModemCommand

		;try to get three decimal digits
digit_loop_wtf:
		lda		#0
		sta		bufrhi
		ldx		#3
		stx		bufrlo
digit_loop:
		jsr		try_get_byte
		eor		#$30
		cmp		#10
		bcs		digit_loop_wtf
		sta		bfenlo
		lda		bufrhi
		asl
		asl
		clc
		adc		bufrhi
		asl
		adc		bfenlo
		sta		bufrhi
		dec		bufrlo
		bne		digit_loop

		;sync speed bit from the speed we used
		lda		serialHiSpeed
		asl
		rol
		rol
		and		#2
		sta		serialStatus
		lda		pbctl
		and		#$fd
		ora		serialStatus
		sta		pbctl

		;sync carrier detect from status bit 6
		lda		bufrhi
		and		#$40
		beq		no_cd

		lda		pactl
		ora		#$02
		sta		pactl

		lda		serialStatus
		ora		#$0c
		sta		serialStatus

		;modem was in online state, so return
		ldx		#modem_commands.ato-modem_commands
		jsr		SerialSendModemCommand
no_cd:

		jsr		SerialFlushOutput

		clc
		dta		{bit $00}
modem_sync_give_up:
		sec

		php
		;disable transmission
		jsr		SerialEndConcurrent

		;check if this is a warmstart, and don't call into DOS if not
		lda		warmst
		beq		skip_chain
		
		plp
		jmp		$ffff
dosini_chain = *-2

skip_chain:
		plp
		rts

modem_sync_failed:
		pla
		pla

		;check if we were in low speed -- this might fail if the modem is
		;online. flip to high speed and try again
		bit		serialHiSpeed
		bmi		modem_sync_give_up

		sec
		ror		serialHiSpeed
		jmp		modem_sync_retry

try_get_byte:
		lda		rtclok+2
		cmp		#0
timeout_val = *-1
		beq		modem_sync_failed
		lda		serialInSpace
		cmp		#$ff
		beq		try_get_byte
		jmp		RDevGetByte

dev_entry:
		dta		'R'
		dta		a(RDevTable)
.endp

;==========================================================================
serialVecs:
		dta		a(SerialInputIrqHandler)
		dta		a(SerialOutputIrqHandler)
		dta		a(SerialProceedHandler)
		dta		a(SerialInterruptHandler)

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
serialPerms		.ds		1
serialVecSave	.ds		4
serialVecSave2	.ds		4
serialErrors	.ds		1
serialStatus	.ds		1

;these are cleared together
serialClearBegin = *
serialHiSpeed	.ds		1
serialXlatMode	.ds		1
serialXlatChar	.ds		1
serialConcurrent	.ds	1
serialClearEnd = *

inputBuffer		.ds		256
outputBuffer	.ds		64

bss_end = outputBuffer + $40

;==========================================================================

		run		Init
