;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement 1030 Modem Firmware - T: Device Handler
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

;If CMCMD is nonzero, ESC sequences are processed in the PUT BYTE stream.
;This flag doesn't affect any command sequence that is already in progress.
;This is documented in the XM301 handler documentation.
cmcmd			equ		$07

;Input and output buffer levels, in bytes. These are documented in the
;XM301 handler documentation and must be kept up to date at all times.
incnt			equ		$0400
outcnt			equ		$0401

;private variables
output_buffer	equ		$0402
				;		$0441
error_flags		equ		$0442
handler_stat	equ		$0443
proceed_pending	equ		$0444
active_count	equ		$0445
last_command	equ		$0446
xmit_busy		equ		$0447
command_vec		equ		$0448
				;		$0449
command_state	equ		$044A
input_parity	equ		$044C
output_parity	equ		$044D
xlat_wnt_char	equ		$044E		;will-not-translate char for heavy mode
xlat_addlf		equ		$044F		;bit 7 = enable add LF
xlat_mode		equ		$0450		;bit 7 = translation disabled, bit 6 = heavy mode
irq_status		equ		$0451		;!! - updated from IRQ!

		org		BASEADDR		;nominally $1D00
		opt		h-f+

;The ERR_BUF_OVERRUN and STAT_CD flags are kept in irq_status so that
;they can be updated by the IRQ routine without requiring the mainline
;code to disable interrupts to get to the rest of the flags. The status
;routine merges them.

ERR_FRAME		= $80		;checked by status routine
ERR_SIO_OVERRUN	= $40		;checked by status routine
ERR_PARITY		= $20		;checked by get byte routine
ERR_BUF_OVERRUN	= $10		;checked by receive interrupt handler
ERR_INVALID_CMD	= $01		;checked by command processor

STAT_CD			= $80		;carrier detect (updated from interrupt)
STAT_LOOPBACK	= $20		;analog loopback test enabled
STAT_ANSWER		= $10		;answer mode (vs. originate mode)
STAT_AUTOANSWER	= $08		;auto-answer mode enabled
STAT_TONEDIAL	= $04		;tone dial mode (vs. pulse dial mode)
STAT_OFFHOOK	= $01		;phone off hook

;The 1030 handler loads at $1D00-282F.
;XM-301 handler loads at $1E00 and sets MEMLO to $2898.

;==========================================================================
; CIO handler table
;
; This must be first. $1D0C is the entry point.
;
.nowarn .proc HandlerTable
		dta		a(HandlerOpen-1)
		dta		a(HandlerClose-1)
		dta		a(HandlerGetByte-1)
		dta		a(HandlerPutByte-1)
		dta		a(HandlerStatus-1)
		dta		a(HandlerSpecial-1)
		jmp		Init
.endp

;==========================================================================
; Persistent variables
;

open_flag	dta		0

;==========================================================================
.proc HandlerOpen
		;check if we're already open on another IOCB
		lda		open_flag
		beq		not_open

		;indicate modem already open
		ldy		#$96
		rts

not_open:
		;mark modem open
		sec
		ror		open_flag

		ldy		#0
		sty		active_count
		sty		command_state
		sty		error_flags
		sty		irq_status
		sty		handler_stat
		sty		input_parity
		sty		output_parity
		sty		xlat_mode
		sty		xlat_addlf

		;jumpstart modem
		jsr		CmdY

		ldy		#1
		rts
.endp

;==========================================================================
.proc HandlerClose
		;check if we're actually open and clear flag if so
		asl		open_flag
		bcc		wasnt_open

		;reset the modem
		lda		#'Q'
		jsr		CmdSend

		;kill communications
		jsr		DisableSerial

wasnt_open:
		;all done
		ldy		#1
		rts
.endp

;==========================================================================
; CIO GET BYTE handler
;
; Notes:
;	- A parity check failure does NOT block the character from being
;	  returned.
;
.proc HandlerGetByte
		;check if we're suspended
		lda		active_count
		beq		is_suspended

		;wait for a byte to arrive
wait:
		bit		irq_status
		bpl		no_carrier
		lda		brkkey
		beq		on_break
		lda		incnt
		beq		wait

		lda		input_buffer
inptr = *-2
		inc		inptr
		dec		incnt

		;do parity check
		ldy		input_parity
		beq		skip_parity

		;check if we're just clearing the parity bit
		cpy		#3
		beq		parity_ok

		;recompute parity
		sta		ciochr
		jsr		ComputeParity

		;compute character to see if parity is correct
		eor		ciochr
		beq		parity_ok

		;set parity error flag
		lda		#ERR_PARITY
		ora		error_flags
		sta		error_flags

parity_ok:
		lda		ciochr
		and		#$7f
skip_parity:

		;check if translation is on
		bit		xlat_mode
		bmi		skip_translation

		;check for CR
		cmp		#$0d
		beq		is_eol

		;check if heavy translation is on
		bit		xlat_mode
		bvc		skip_heavy_xlat

		;check for char outside of 20-7C
		cmp		#$20
		bcc		wont_translate
		cmp		#$7d
		bcc		skip_translation
wont_translate:
		;load the won't-translate char
		lda		xlat_wnt_char

		;skip bit 7 masking
		ldy		#1
		rts

skip_heavy_xlat:
		and		#$7f

skip_translation:
		ldy		#1
		rts

no_carrier:
		ldy		#CIOStatEndOfFile
		rts

is_eol:
		;convert CR to EOL and skip remaining translation
		lda		#$9b
		bne		skip_translation

is_suspended:
		ldy		#CIOStatInvalidCmd
		rts

on_break:
		ldy		#CIOStatBreak
		rts
.endp

;==========================================================================
.proc ProcessCommand
		ldy		#1
		sty		status

		;check if we are in a sub-state
		inx
		bpl		sub_state

		;check for escape - if so, ignore it and keep waiting for cmd byte
		cmp		#$1b
		beq		exit

		;clear command mode (unless command handler later sets it)
		ldy		#0
		sty		command_state

		;check if suspended
		ldy		active_count
		bne		not_suspended

		;must be Y (resume) if suspended
		cmp		#'Y'
		beq		not_suspended

		;force an invalid command
		lda		#'B'

not_suspended:
		sta		last_command

		;check for valid uppercase letter
		sbc		#'A'
		cmp		#26
		jcs		HandlerPutByte.invalid_command

		;dispatch to command
		jsr		dispatch

exit:
		ldy		status
		rts

sub_state:
		sec
		jsr		call_last_command
		ldy		status
		rts

dispatch:
		tax
		lda		command_table_hi,x
		sta		command_vec+1
		lda		command_table_lo,x
		sta		command_vec

		clc
		lda		last_command

call_last_command:
		jmp		(command_vec)
.endp

;==========================================================================
.proc OnBreak
		ldy		#CIOStatBreak
		rts
.endp

;==========================================================================
; CIO PUT BYTE handler
;
; Command processing is a bit complicated here:
;	- Initial state
;		- If CMCMD is set:
;			- Check if we have ESC
;			- Return invalid command if not
;			- Enter command byte state and return
;		- If CMCMD is not set:
;			- Translate byte
;			- Compute parity
;			- Transmit byte(s)
;	- Command byte state
;		- If byte is ESC, return silently
;		- Check if byte is valid command byte, return inv.cmd. if not
;		- Dispatch to command
;	- Additional command byte states:
;		- Send byte to command
;		- Return to initial state unless command needs additional bytes
;
; Notes:
;	- No bytes can be transmitted while CMCMD is set.
;	- ESC ESC <cmd> executes that command.
;	- ESC sequences are not processed while CMCMD is cleared.
;	- CMCMD is ignored after ESC has been seen while CMCMD is set, until
;	  that command completes.
;
.proc HandlerPutByte
		;wait for proceed
proceed_wait:
		ldy		brkkey
		beq		is_break
		ldy		proceed_pending
		bmi		proceed_wait

		;check if we are already in command (cmcmd doesn't matter here)
		ldx		command_state
		bne		ProcessCommand

		;check if we should be scanning for command escapes
		ldy		cmcmd
		bne		check_escape

not_escape:
		;can't send regular bytes while suspended
		ldy		active_count
		beq		invalid_command

put_raw:
		;check if translation is enabled
		bit		xlat_mode
		bmi		buffered_put_byte

		;check for EOL
		cmp		#$9b
		bne		not_eol

		;convert EOL to CR
		lda		#$0d
		
		;check if add LF mode is on
		bit		xlat_addlf
		bpl		buffered_put_byte

		;put the CR character
		jsr		buffered_put_byte

		;exit immediately if we already have an error
		bmi		error_exit

		;then queue the LF
		lda		#$0a
		bne		buffered_put_byte
not_eol:

		;check for heavy translation
		bit		xlat_mode
		bvc		skip_heavy_xlat

		;check if code is translatable and reject it if so
		cmp		#$20
		bcc		xlat_heavy_reject
		cmp		#$7d
		bcs		xlat_heavy_reject
skip_heavy_xlat:
		;strip bit 7 (only actually does anything in light mode)
		and		#$7f

buffered_put_byte:
		;compute output parity
		ldy		output_parity
		beq		skip_parity
		sta		ciochr
		jsr		ComputeParity
skip_parity:

		;wait for space to put the byte
		ldx		#64
wait:
		bit		irq_status
		bpl		no_carrier
		ldy		brkkey
		beq		OnBreak
		cpx		outcnt
		beq		wait

.def :RawPutByte = *

		;suspend interrupts
		php
		sei

		;check if transmit shift register is idle
		bit		xmit_busy
		bpl		restart_xmit

		;add byte to output buffer
		sta		output_buffer
outptr = *-2
		inc		outcnt
		plp

		inc		outptr
		lda		outptr
		cmp		#<[output_buffer+64]
		bne		no_wrap
		lda		#<output_buffer
		sta		outptr
no_wrap:
send_done:

xlat_heavy_reject:
		ldy		#1
error_exit:
		rts

is_break:
		ldy		#$80
		rts

restart_xmit:
		sta		serout
		sec
		ror		xmit_busy
		plp
		jmp		send_done

check_escape:
		;check for ESC
		cmp		#$1b
		bne		not_escape

		;switch to command state and exit
		lda		#$80
		sta		command_state
		ldy		#1
		rts

invalid_command:
		ldy		#CIOStatInvalidCmd
		rts

no_carrier:
		ldy		#CIOStatEndOfFile
		rts
.endp

;==========================================================================
; Entry:
;	CIOCHR = input character
;	Y = parity mode
;		0 = no parity
;		1 = odd parity
;		2 = even parity
;		3 = space parity (clear bit 7)
;
; Exit:
;	A = output character with desired parity
;
.proc ComputeParity
		lda		ciochr
		dey
		bmi		no_parity
		cpy		#2
		beq		mark_parity
		asl
		eor		ciochr
		and		#%10101010		;pairwise sums in odd bits
		adc		#%01100110		;add bit 5 to 7 and bit 1 to 3
		and		#%10001000		;mask to bits 3 and 7
		adc		#%01111000		;parity in bit 7
		and		#$80			;mask to parity bit
		eor		ciochr			;set even parity
		dey						;check if we should do odd parity
		spl:eor	#$80			;complement if odd parity
even_parity:
no_parity:
		rts
		
mark_parity:
		ora		#$80
		rts
.endp


;==========================================================================
; Command dispatch
;
; Entry:
;	C=0 if initial call, C=1 if additional data byte
;
; Exit:
;	STATUS = return code to CIO
;
.macro _COMMAND_TABLE
		dta		:1[CmdA]			;A - Set translation
		dta		:1[CmdInvalid]
		dta		:1[CmdC]			;C - Set parity
		dta		:1[CmdInvalid]
		dta		:1[CmdE]			;E - End commands
		dta		:1[HandlerStatus]	;F - Status
		dta		:1[CmdG]			;G - Enable auto-answer (XM301)
		dta		:1[CmdH]			;H - Break
		dta		:1[CmdI]			;I - Set originate
		dta		:1[CmdJ]			;J - Set answer
		dta		:1[CmdK]			;K - Dial
		dta		:1[CmdSend]			;L - Go off hook
		dta		:1[CmdSend]			;M - Go on hook
		dta		:1[CmdN]			;N - Set pulse dialing
		dta		:1[CmdO]			;O - Set tone dialing
		dta		:1[CmdP]			;P - Start 30sec timeout
		dta		:1[CmdSend]			;Q - Reset modem
		dta		:1[CmdSend]			;R - Enable sound
		dta		:1[CmdSend]			;S - Disable sound
		dta		:1[CmdT]			;T - Disable auto-answer (XM301)
		dta		:1[CmdInvalid]
		dta		:1[CmdInvalid]
		dta		:1[CmdW]			;W - Set analog loop
		dta		:1[CmdX]			;X - Clear analog loop
		dta		:1[CmdY]			;Y - Resume
		dta		:1[CmdZ]			;Z - Suspend
.endm

command_table_lo:
		_COMMAND_TABLE	<

command_table_hi:
		_COMMAND_TABLE	>

;==========================================================================
.proc CmdInvalid
		lda		#ERR_INVALID_CMD
		ora		error_flags
		sta		error_flags

		ldy		#CIOStatInvalidCmd
		sty		status
		rts
.endp

;==========================================================================
.proc CmdSend
		sta		last_command
		jsr		DrainOutputBuffer
		bmi		break_exit

		;enable serial routines if not already set up (this nests)
		jsr		EnableSerial

		;assert command line
		lda		pbctl
		and		#$c7
		ora		#$30
		sta		pbctl

		;send command byte
		lda		last_command
		jsr		RawPutByte

		;wait for byte to be sent
		lda:rmi	xmit_busy

		;wait for byte to complete
		lda		#8
		bit:rne	irqst

		;deassert command line
		lda		pbctl
		ora		#$08
		sta		pbctl

		;disable serial routines if they were off coming in
		jsr		DisableSerial

		;all done
		rts

break_exit:
		sty		status
		rts
.endp

;==========================================================================
; A - Set translation
;
; First byte:
;	Bit 7: Ignored
;	Bit 6: Add LF after CR (only if translation is enabled)
;	Bits 4-5:
;		00 = light translation (CR <-> EOL, strip bit 7)
;		01 = heavy translation (CR + 20-7C + EOL)
;		1x = translation disabled
;	Bits 0-3: Ignored
;
.proc CmdA
		bcs		process_argument
		lda		#$02
		sta		command_state
		rts

process_argument:
		dec		command_state
		ldx		command_state
		cpx		#1
		bne		process_arg2

		asl
		asl
		ror		xlat_addlf
		sta		xlat_mode
		rts

process_arg2:
		sta		xlat_wnt_char
		rts
.endp

;--------------------------------------------------------------------------
; C - Set parity
;
; Argument:
;	Bits 4-7: Ignored
;	Bits 2-3: Input parity checking
;		00 = disabled
;		01 = odd parity
;		10 = even parity
;		11 = strip parity
;	Bits 0-1: Output parity
;		00 = disabled
;		01 = odd parity
;		10 = even parity
;		11 = mark parity
;
.proc CmdC
		bcs		process_argument
		inc		command_state
		rts

process_argument:
		tay
		and		#$03
		sta		output_parity
		tya
		lsr
		lsr
		and		#$03
		sta		input_parity
		dec		command_state
		rts
.endp

;--------------------------------------------------------------------------
; E - End commands
;
.proc CmdE
		;exit command state
		lda		#0
		sta		cmcmd
		rts
.endp

;--------------------------------------------------------------------------
; G - Enable auto-answer
;
.proc CmdG
		lda		#STAT_AUTOANSWER

		bit		handler_stat
		beq		set_autoanswer
		rts

set_autoanswer:
		eor		handler_stat
		sta		handler_stat

		lda		#'G'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; H - Break
;
.proc CmdH
		rts
.endp

;--------------------------------------------------------------------------
; I - Set originate mode
;
.proc CmdI
		lda		#STAT_ANSWER

		bit		handler_stat
		bne		set_originate
		rts

set_originate:
		eor		handler_stat
		sta		handler_stat

		lda		#'I'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; J - Set answer mode
;
.proc CmdJ
		lda		#STAT_ANSWER

		bit		handler_stat
		beq		set_answer
		rts

set_answer:
		eor		handler_stat
		sta		handler_stat

		lda		#'J'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; K - Dial digits
;
; In touch tone mode, we must generate DTMF codes via the speaker. This is
; done by telling the modem to connect POKEY to the phone line (cmd O) and
; then generating the DTMF tones by volume only mode.
;
; Only the four low bits of each character are checked:
;	- 0-9 dials digits/pulses
;	- B ends dialing
;	- C does a 3 second delay 
;	- A,D-F ignored
;
; Touch dial sequences are not interruptable by BREAK.
;
.proc CmdK
		bcc		initial_command

		and		#$0f
		cmp		#10
		bcs		not_digit
		jmp		PlayDTMFTone

not_digit:
		cmp		#$0b
		bne		not_end

		;do 30 second wait
		lda		#'P'
		jsr		CmdSend

		;end this command
		dec		command_state
		rts

not_end:
		cmp		#$0c
		bne		not_delay

		;wait 3 seconds
		lda		rtclok+2
		adc		#150-1
		tay
		clc
		adc		#180-1
delay_loop:
		;check if the current scanline is beyond what we could get on NTSC
		lda		vcount
		cmp		#140
		bcc		not_pal

		;yes, it is -- switch to the PAL deadline
		tya
not_pal:
		cmp		rtclok+2
		bne		delay_loop

not_delay:
		rts

initial_command:
		;jump to persistent command state
		inc		command_state

		;wait for output buffer to drain
		jsr		DrainOutputBuffer
		sty		status

		;set up modem for tone dialing
		lda		#'O'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; N - Set pulse dialing
;
; Selects pulse dialing for future dial (K) commands. This is a handler
; command; no command is sent to the modem.
;
.proc CmdN
		lda		#STAT_TONEDIAL^$FF
		and		handler_stat
		sta		handler_stat
		rts
.endp

;--------------------------------------------------------------------------
; O - set tone dial mode
;
; Selects tone dialing for future dial (K) commands. This is a handler
; command; no command is sent to the modem.
;
.proc CmdO
		lda		#STAT_TONEDIAL
		ora		handler_stat
		sta		handler_stat
		rts
.endp

;--------------------------------------------------------------------------
; P - Start 30 second wait
.proc CmdP
		rts
.endp

;--------------------------------------------------------------------------
; T - Disable auto-answer
;
.proc CmdT
		lda		#STAT_AUTOANSWER

		bit		handler_stat
		bne		clear_autoanswer
		rts

clear_autoanswer:
		eor		handler_stat
		sta		handler_stat

		lda		#'T'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; W - Set analog loop
;
.proc CmdW
		lda		#STAT_LOOPBACK

		bit		handler_stat
		beq		set_loopback
		rts

set_loopback:
		ora		handler_stat
		sta		handler_stat

		lda		#'W'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; X - Clear analog loop
;
.proc CmdX
		lda		#STAT_LOOPBACK

		bit		handler_stat
		bne		clear_loopback
		rts

clear_loopback:
		eor		handler_stat
		sta		handler_stat

		lda		#'X'
		jmp		CmdSend
.endp

;--------------------------------------------------------------------------
; Y - Resume
;
; The XM301 handler documentation says that this command clears CMCMD and
; fills DVSTAT with status info. Too bad the handler on that same disk
; doesn't actually do that....
;
.proc CmdY
		;check if modem is already resumed
		lda		active_count
		bne		already_active

		;clear buffers
		sta		incnt
		sta		outcnt
		sta		IrqSerialInputReady.inptr
		sta		HandlerGetByte.inptr
		sta		proceed_pending
		sta		xmit_busy

		lda		#<output_buffer
		sta		IrqSerialOutputReady.outptr
		sta		HandlerPutByte.outptr

		;enable serial reception
		jsr		EnableSerial

		;send modem the resume command
		lda		#'Y'
		jsr		CmdSend

already_active:
		rts
.endp

;--------------------------------------------------------------------------
; Z - Suspend
;
.proc CmdZ
		lda		active_count
		beq		already_suspended

		jsr		DisableSerial

already_suspended:
		rts
.endp

;==========================================================================
; CIO GET STATUS handler
;
; All errors are cleared after this command.
;
; A get status command does not send any commands to the device.
; This is true both of the handler command (cmd $0C) and ESC F.
;
.proc HandlerStatus
		;check for framing and receive errors
		lda		skstat
		sta		skres

		;move bit 5 to bit 6
		eor		#$a0
		and		#$a0
		adc		#$20

		;merge in with software error flags
		and		#$c0
		ora		error_flags
		sta		dvstat

		;enter critical section and merge in flags from interrupt
		php
		sei

		lda		irq_status
		tax
		and		#ERR_BUF_OVERRUN^$FF
		sta		irq_status
		plp

		txa
		and		#ERR_BUF_OVERRUN
		ora		dvstat
		sta		dvstat

		txa
		and		#STAT_CD
		ora		handler_stat
		sta		dvstat+1

		;these are undocumented, but AMODEM 7.5 needs them
		mva		incnt dvstat+2
		mva		outcnt dvstat+3

		;clear error flags
		ldy		#0
		sty		error_flags

		;return success
		iny
		rts
.endp

;==========================================================================
.proc HandlerSpecial
		rts
.endp

;==========================================================================
; DTMF Tone generation
;
; The DTMF tones are as follows:
;
; 697   1    2    3
; 770   4    5    6
; 852   7    8    9
; 941   *    0    #
;     1209 1336 1477 (Hz)
;
; The frequencies in question are spaced by a ratio of (21/19), except for
; the gap between the tone groups which is (21/19)^2.5.
;
; The easiest way for us to synthesize these tones is with a 7.8KHz bit-bang
; routine, due to WSYNC.
;
; According to ANSI T1.401-1988, we need at least 45ms gap between tones,
; at least 50ms of tone, and at least 100ms tone-to-tone spacing.
; Complicating this is at in GR.0, we have as much as 84 cycles of DMA on
; a scanline with just 30 cycles from WSYNC-to-WSYNC in the worst case.
; That is not enough to run dual tone generators in real-time.
;
; 50ms of tone at 7.8KHz takes 392 samples. Packing two samples together
; gives 175 bytes, which we can cram into the input buffer. To make things
; simpler we go ahead and generate 512 samples for a tone duration of 58ms.
;
.proc PlayDTMFTone
		tax
		mva		dtmf_tone_1_table_lo,x inc1lo
		mva		dtmf_tone_1_table_hi,x inc1hi
		mva		dtmf_tone_2_table_lo,x inc2lo
		mva		dtmf_tone_2_table_hi,x inc2hi

		;wait three VBLANKs to ensure enough gap time
		lda		rtclok+2
		clc
		adc		#3
		cmp:rne	rtclok+2

		;clear the DTMF buffer
		lda		#0
		tax
		sta:rne	input_buffer,x+

		;accumulate tones
		tay
		stx		acc1lo
		stx		acc2lo
accum_loop:
		clc
		lda		dtmf_sin_table,x
		adc		dtmf_sin_table,y
		lsr
		lsr
		lsr
		lsr
		sta		low_nibble

		jsr		accum_update

		clc
		lda		dtmf_sin_table,x
		adc		dtmf_sin_table,y
		and		#$f0

		ora		#0
low_nibble = *-1
		sta		input_buffer
outptr = *-2

		jsr		accum_update

		inc		outptr
		bne		accum_loop

		;interrupts off
		php
		sei

		inc		critic

		;play the tone
		ldx		#0
play_loop:
		sta		wsync
		lda		input_buffer,x		;4
		ora		#$10				;2
		tay							;2
		
		sta		wsync
		sty		audc4				;4
		sec							;2
		ror							;2
		
		sta		wsync
		lsr							;2
		lsr							;2
		lsr							;2
		inx							;2

		sta		wsync
		sta		audc4				;4
		bne		play_loop			;3

		;clear tone
		lda		#$b0
		sta		audc4

		;interrupts back on
		dec		critic
		plp

		;all done
		rts

accum_update:
		clc
		lda		#0
acc1lo = *-1
		adc		#0
inc1lo = *-1
		sta		acc1lo
		txa
		adc		#0
inc1hi = *-1
		and		#$3f
		tax

		clc
		lda		#0
acc2lo = *-1
		adc		#0
inc2lo = *-1
		sta		acc2lo
		tya
		adc		#0
inc2hi = *-1
		and		#$3f
		tay
		rts
.endp

;==========================================================================
dtmf_tone_1_table_lo:
		dta		<1964
		dta		<1455
		dta		<1455
		dta		<1455
		dta		<1608
		dta		<1608
		dta		<1608
		dta		<1777
		dta		<1777
		dta		<1777

dtmf_tone_1_table_hi:
		dta		>1964
		dta		>1455
		dta		>1455
		dta		>1455
		dta		>1608
		dta		>1608
		dta		>1608
		dta		>1777
		dta		>1777
		dta		>1777

dtmf_tone_2_table_lo:
		dta		<2788
		dta		<2523
		dta		<2788
		dta		<3082
		dta		<2523
		dta		<2788
		dta		<3082
		dta		<2523
		dta		<2788
		dta		<3082

dtmf_tone_2_table_hi:
		dta		>2788
		dta		>2523
		dta		>2788
		dta		>3082
		dta		>2523
		dta		>2788
		dta		>3082
		dta		>2523
		dta		>2788
		dta		>3082

;==========================================================================
dtmf_sin_table:
		dta		128/2, 140/2, 152/2, 165/2, 176/2, 188/2, 198/2, 208/2, 218/2, 226/2, 234/2, 240/2
		dta		245/2, 250/2, 253/2, 254/2, 255/2, 254/2, 253/2, 250/2, 245/2, 240/2, 234/2, 226/2 
		dta		218/2, 208/2, 198/2, 188/2, 176/2, 165/2, 152/2, 140/2, 128/2, 115/2, 103/2, 90 /2
		dta		79/2, 67/2, 57/2, 47/2, 37/2, 29/2, 21/2, 15/2, 10/2, 5/2, 2/2, 1/2, 0/2, 1/2, 2/2
		dta		5/2, 10/2, 15/2, 21/2, 29/2, 37/2, 47/2, 57/2, 67/2, 79/2, 90/2, 103/2, 115/2

;==========================================================================
.proc DrainOutputBuffer
wait:
		lda		brkkey
		beq		is_break
		lda		outcnt
		bne		wait
		rts

is_break:
		ldy		#$80
		rts
.endp

;==========================================================================
.proc EnableSerial
		lda		active_count
		bne		already_on

		;silence audio channels
		lda		#$a0
		sta		audc1
		sta		audc2
		sta		audc3
		sta		audc4

		;disable interrupts
		php
		sei

		;swap in our interrupt vectors
		jsr		SwapVectors

		;set channels 2 and 4 to 600Hz (300 baud)
		lda		#<[2983-7]
		sta		audf1
		sta		audf3
		lda		#>[2983-7]
		sta		audf2
		sta		audf4

		;link 1+2 and 3+4 @ 1.79MHz
		lda		#$78
		sta		audctl

		;reset serial hardware, then set channel 2 transmit clock, channel
		;4 async receive clock
		lda		sskctl
		and		#$8f
		sta		skctl
		ora		#$70
		sta		sskctl
		sta		skctl

		;enable serial input/output ready interrupts and disable all timer
		;interrupts and serial output complete interrupt
		lda		pokmsk
		ora		#$30
		and		#$f0
		sta		pokmsk
		sta		irqen

		;enable PIA interrupts on negative transition of SIO proceed and
		;interrupt lines
		lda		pactl
		and		#$fd
		ora		#$01
		sta		pactl
		lda		pbctl
		and		#$fd
		ora		#$01
		sta		pbctl

		;clear any stray PIA interrupts
		lda		porta
		lda		portb

		;clear any old errors
		sta		skres

		;re-enable interrupts
		plp

already_on:
		inc		active_count
		rts
.endp

;==========================================================================
.proc DisableSerial
		lda		active_count
		beq		xit
		dec		active_count
		bne		xit

		;disable interrupts
		php
		sei

		;shut off serial input/output ready interrupts
		lda		pokmsk
		and		#$cf
		sta		pokmsk
		sta		irqen

		;turn off PIA interrupts
		lda		pactl
		and		#$fe
		sta		pactl
		lda		pbctl
		and		#$fe
		sta		pbctl

		;clear PIA interrupts
		lda		porta
		lda		portb

		;restore interrupt vectors
		jsr		SwapVectors

		;re-enable interrupts
		plp

xit:
		rts
.endp

;==========================================================================
.proc SwapVectors
		ldx		#13
loop:
		lda		vprced,x
		ldy		vector_table,x
		sta		vector_table,x
		tya
		sta		vprced,x
		cpx		#8
		sne:ldx	#4
		dex
		bpl		loop
		rts
.endp

;==========================================================================
.proc IrqSerialInputReady
		inc		incnt
		beq		buffer_full

		lda		serin
		sta		input_buffer
inptr = *-2
		inc		inptr

done:
		lda		#$df
		sta		irqen
		lda		pokmsk
		sta		irqen
		pla
		rti

buffer_full:
		dec		incnt
		lda		#ERR_BUF_OVERRUN
		ora		irq_status
		sta		irq_status
		bne		done
.endp

;==========================================================================
.proc IrqSerialOutputReady
		lda		outcnt
		beq		no_data
		dec		outcnt
		lda		output_buffer
outptr = *-2
		sta		serout
		inc		outptr
		lda		outptr
		cmp		#<[output_buffer+64]
		bne		no_wrap
		lda		#<output_buffer
		sta		outptr
no_wrap:
		pla
		rti

no_data:
		lsr		xmit_busy
		pla
		rti
.endp

;==========================================================================
; SIO proceed handler
;
.proc IrqPiaProceed
		;clear proceed pending flag
		lsr		proceed_pending

		;acknowledge interrupt
		lda		porta

		pla
		rti
.endp

;==========================================================================
; SIO interrupt handler
;
.proc IrqPiaInterrupt
		;acknowledge interrupt
		lda		portb

		;toggle the carrier detect bit
		lda		irq_status
		eor		#$80
		sta		irq_status

		;all done
		pla
		rti
.endp

;==========================================================================
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
		ldx		#<bss_end
		lda		#>bss_end
		cpx		memlo
		sbc		memlo+1
		bcc		memlo_already_higher
		stx		memlo
		lda		#>bss_end
		sta		memlo+1
memlo_already_higher:
		rts

dev_entry:
		dta		'T'
		dta		a(HandlerTable)
.endp

;==========================================================================
.proc ReinitHook
		jsr		Reinit
		jmp		$ffff
dosini_chain = *-2
.endp

;==========================================================================
vector_table:
		dta		a(IrqPiaProceed)
		dta		a(IrqPiaInterrupt)
		dta		a(0)
		dta		a(0)
		dta		a(IrqSerialInputReady)
		dta		a(IrqSerialOutputReady)

;==========================================================================
; BSS start
;
; Everything after here is lost after init!
;
bss_start:

;==========================================================================
.proc Init
		;hook DOSINI
		mwa		dosini ReinitHook.dosini_chain
		
		lda		#<ReinitHook
		sta		dosini
		lda		#>ReinitHook
		sta		dosini+1

		jsr		Reinit
		
		;all done
		clc
		rts
.endp

;==========================================================================
; End of initialized code/data
;
		.echo "Init end: ", *

;==========================================================================
; BSS
;
input_buffer	= [bss_start+$FF]&$FF00
bss_end			= input_buffer + 256

		.echo "BSS end: ", bss_end

		.if bss_end > $2830-$1D00+BASEADDR
		.error "BSS too long: ", *
		.endif
