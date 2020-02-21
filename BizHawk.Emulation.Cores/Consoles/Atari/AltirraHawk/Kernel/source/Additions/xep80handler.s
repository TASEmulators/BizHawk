;	Altirra - Atari 800/800XL/5200 emulator
;	Replacement XEP80 Handler Firmware - E:/S: Device Handler
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

.ifndef XEP_OPTION_TURBO
		XEP_OPTION_TURBO = 0
.endif

XEP_TURBO_4X = 0

;==========================================================================
keybdv	equ		$e420

;==========================================================================

.macro _loop opcode adrmode operand
		.if :adrmode!='#'
		.error "Immediate addressing mode must be used with lo-opcode"
		.endif
		
		:opcode #<:operand
.endm

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

.macro _ldalo adrmode operand " "
		_loop lda :adrmode :operand
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
XEPCMD_HORIZ_POS			= $00
XEPCMD_HORIZ_POS_HI			= $50
XEPCMD_SET_LMARGN_LO		= $60
XEPCMD_SET_LMARGN_HI		= $70
XEPCMD_VERT_POS				= $80
XEPCMD_SET_RMARGN_LO		= $A0
XEPCMD_SET_RMARGN_HI		= $B0
XEPCMD_GET_BYTE_AND_ADVANCE	= $C0
XEPCMD_MASTER_RESET			= $C2
XEPCMD_EXIT_BURST_MODE		= $D2
XEPCMD_ENTER_BURST_MODE		= $D3
XEPCMD_CHARSET_A			= $D4
XEPCMD_CHARSET_B			= $D5
XEPCMD_CURSOR_OFF			= $D8
XEPCMD_CURSOR_ON			= $D9
XEPCMD_MOVE_TO_LOGLINE_START= $DB
XEPCMD_SET_EXTRA_BYTE		= $E1
XEPCMD_SET_BAUD_RATE		= $FA
XEPCMD_SET_UMX				= $FC

;==========================================================================

		org		BASEADDR

base_addr:
		jmp		Init

data_begin:
data_byte	dta		0
mode_80		dta		0
saverow		dta		0
savecol		dta		0
savedsp		dta		0
savelmr		dta		0
savermr		dta		0
savechb		dta		0

;==========================================================================
opflag	dta		0
burst	dta		0
shdatal	dta		0
shdatah	dta		0
curlm	dta		0
currm	dta		0
curchb	dta		0
currow	dta		0
curcol	dta		0
curinh	dta		0
data_end:
portbit	dta		$10
portbitr	dta		$20

;==========================================================================
.proc XEPSDevice
		dta		a(XEPScreenOpen-1)
		dta		a(XEPScreenClose-1)
		dta		a(XEPScreenGetByte-1)
		dta		a(XEPScreenPutByte-1)
		dta		a(XEPScreenGetStatus-1)
		dta		a(XEPScreenSpecial-1)
.endp

;==========================================================================
.proc XEPEDevice
		dta		a(XEPEditorOpen-1)
		dta		a(XEPEditorClose-1)
		dta		a(XEPEditorGetByte-1)
		dta		a(XEPEditorPutByte-1)
		dta		a(XEPEditorGetStatus-1)
		dta		a(XEPEditorSpecial-1)
.endp

;==========================================================================
.proc XEPScreenOpen
		lda		dspflg
		pha
		lda		#0
		sta		dspflg
		lda		#$7D
		jsr		XEPEditorPutByte
		pla
		sta		dspflg
		tya
		rts
.endp

;==========================================================================
.proc XEPScreenClose
		ldy		#1
		rts
.endp

;==========================================================================
.proc XEPScreenPutByte
		sec
		ror		dspflg
		php
		jsr		XEPEditorPutByte
		plp
		rol		dspflg
		tya
		rts
.endp

;==========================================================================
.proc XEPScreenGetByte
		jsr		XEPEnterCriticalSection
		lda		#XEPCMD_GET_BYTE_AND_ADVANCE
		jsr		XEPTransmitCommand
		jsr		XEPReceiveByte
		bmi		fail
		pha
		jsr		XEPReceiveCursorUpdate
		pla
fail:
		jsr		XEPLeaveCriticalSection
		rts
.endp

;==========================================================================
.proc XEPScreenGetStatus
		ldy		#1
		rts
.endp

;==========================================================================
.proc XEPScreenSpecial
		rts
.endp

;==========================================================================
.proc XEPEditorOpen
		lda		#$80
		jmp		XEPOpen
.endp

;==========================================================================
.proc XEPEditorClose
		ldy		#1
		rts
.endp

;==========================================================================
.proc XEPEditorGetByte
		php

		lda		#0
space_count = *-1
		beq		no_pending_spaces
		dec		space_count
		lda		#' '
return_char:
		ldy		#1
		plp
		rts
no_pending_spaces:

		lda		#' '
saved_char = *-1
		cmp		#' '
		beq		no_saved_char

		ldy		#' '
		sty		saved_char
		bne		return_char

no_saved_char:
		lda		bufcnt
		bne		in_line
		
		lda		colcrs
		sta		start_read_hpos

more_chars:
		jsr		XEPGetKey
		cpy		#0
		bmi		fail
		cmp		#$9b
		beq		got_eol
		pha
		php
		jsr		XEPEnterCriticalSection
		clc
		jsr		XEPTransmitByte
		jsr		XEPReceiveCursorUpdate
		jsr		XEPLeaveCriticalSection
		plp
		pla
		tya
		bmi		fail
		jmp		more_chars

got_eol:
		jsr		XEPEnterCriticalSection
		lda		#0
start_read_hpos = *-1
		jsr		XEPCheckSettings.do_col
		lda		#XEPCMD_MOVE_TO_LOGLINE_START
		jsr		XEPTransmitCommand
		inc		bufcnt
		bne		in_line_2
in_line:
		jsr		XEPEnterCriticalSection
in_line_2:
		lda		#XEPCMD_GET_BYTE_AND_ADVANCE
		jsr		XEPTransmitCommand
		jsr		XEPReceiveByte
		pha
		jsr		XEPReceiveCursorUpdate
		pla

		cmp		#' '
		bne		not_space
		lda		space_count
		bmi		not_space
		inc		space_count
		bne		in_line_2
		
not_space:
		cmp		#$9b
		bne		not_eol
		dec		bufcnt
		clc
		jsr		XEPTransmitByte
		jsr		XEPReceiveCursorUpdate
		lda		#$9b
		ldy		#0
		sty		space_count
		iny
not_eol:
		sty		_status
		ldy		space_count
		beq		no_spaces
		sta		saved_char
		dec		space_count
		lda		#' '
no_spaces:
		ldy		#1
_status = *-1
		jsr		XEPLeaveCriticalSection
fail:
		plp
		rts
.endp

;==========================================================================
.proc XEPGetKey
		lda		keybdv+5
		pha
		lda		keybdv+4
		pha
		rts
.endp

;==========================================================================
.proc XEPEditorPutByte
suspend_loop:
		;check for break
		ldy		brkkey
		beq		is_break

		;check for suspend
		ldy		ssflag
		bne		suspend_loop

		php
		jsr		XEPEnterCriticalSection
		pha
		jsr		XEPCheckSettings
		pla
		clc
		jsr		XEPTransmitByte
		bit		burst
		bpl		non_burst_mode
		
		jsr		XEPWaitBurstACK
		bcc		burst_done

		ldy		#CIOStatTimeout
		bne		xit
		
burst_done:
		ldy		#1
xit:
		jsr		XEPLeaveCriticalSection
		plp
		rts
		
non_burst_mode:
		jsr		XEPReceiveCursorUpdate
		jmp		burst_done

is_break:
		dec		brkkey
		ldy		#CIOStatBreak
		rts
.endp

.proc XEPWaitBurstACK
		ldy		#0
		lda		portbitr
burst_wait_loop:
		bit		porta
		bne		burst_done
		dex
		bne		burst_wait_loop
		dey
		bne		burst_wait_loop
		sec
		rts
burst_done:
		clc
		rts
.endp

;==========================================================================
XEPEditorGetStatus = XEPScreenGetStatus

;==========================================================================
.proc XEPEditorSpecial
		lda		iccomz
		cmp		#$14
		beq		cmd14
		cmp		#$15
		beq		cmd15
		cmp		#$16
		beq		cmd16
		cmp		#$18
		beq		cmd18
		cmp		#$19
		beq		cmd19
		rts

cmd14:
		php
		jsr		XEPEnterCriticalSection
		lda		icax2z
		jsr		XEPTransmitCommand
		jsr		XEPLeaveCriticalSection
ok_2:
		plp
ok:
		ldy		#1
		rts

cmd15:	;Set burst mode: ICAX2 = 0 for normal, 1 for burst
		ldx		#0
		lda		icax2z
		seq:dex
		cpx		burst
		beq		ok
		stx		burst
		php
		jsr		XEPEnterCriticalSection
		lda		burst
		and		#1
		ora		#XEPCMD_EXIT_BURST_MODE
		jsr		XEPTransmitCommand
		jsr		XEPLeaveCriticalSection
		jmp		ok_2			

cmd16:
		php
		jsr		XEPEnterCriticalSection
		lda		icax2z
		jsr		XEPTransmitCommand
		jsr		XEPReceiveByte
		sta		dvstat+1
		jsr		XEPLeaveCriticalSection
		plp
		rts

cmd18:
		rts

cmd19:
		rts

.endp

;==========================================================================
.proc XEPCheckSettings
		;check if someone turned on ANTIC DMA -- Basic XE does this,
		;and if screws up the send/receive timing
		lda		sdmctl
		bne		dma_wtf

check_inh:
		lda		crsinh
		cmp		curinh
		bne		do_inh
check_lmargn:
		lda		lmargn
		cmp		curlm
		bne		do_lmargn
check_rmargn:
		lda		rmargn
		cmp		currm
		bne		do_rmargn
check_chbase:
		lda		chbase
		cmp		curchb
		bne		do_chbase
check_row:
		lda		rowcrs
		cmp		currow
		bne		do_row
check_col:
		lda		colcrs
		cmp		curcol
		bne		do_col
		rts

dma_wtf:
		lda		#0
		sta		sdmctl
		sta		dmactl
		beq		check_inh

do_inh:
		sta		curinh
		cmp		#1
		lda		#XEPCMD_CURSOR_ON
		scc:lda	#XEPCMD_CURSOR_OFF
		jsr		XEPTransmitCommand
		jmp		check_lmargn

do_lmargn:
		sta		curlm
		pha
		and		#$0f
		ora		#XEPCMD_SET_LMARGN_LO
		jsr		XEPTransmitCommand
		pla
		and		#$f0
		beq		check_rmargn
		lsr
		lsr
		lsr
		lsr
		ora		#XEPCMD_SET_LMARGN_HI
		jsr		XEPTransmitCommand
		jmp		check_rmargn
		
do_rmargn:
		sta		currm
		pha
		and		#$0f
		ora		#XEPCMD_SET_RMARGN_LO
		jsr		XEPTransmitCommand
		pla
		and		#$f0
		cmp		#$40
		beq		check_chbase
		lsr
		lsr
		lsr
		lsr
		ora		#XEPCMD_SET_RMARGN_HI
		jsr		XEPTransmitCommand
		jmp		check_rmargn
		
do_chbase:
		sta		curchb
		cmp		#$cc
		lda		#XEPCMD_CHARSET_A
		sne:lda	#XEPCMD_CHARSET_B
		jsr		XEPTransmitCommand
		jmp		check_chbase
		
do_row:
		sta		currow
		ora		#$80
		jsr		XEPTransmitCommand
		jmp		check_col
		
do_col:
		sta		curcol
		cmp		#80
		bcs		do_wide_col
		jmp		XEPTransmitCommand

do_wide_col:
		pha
		and		#$0f
		jsr		XEPTransmitCommand
		pla
		lsr
		lsr
		lsr
		lsr
		ora		#XEPCMD_HORIZ_POS_HI
		jmp		XEPTransmitCommand
.endp

;==========================================================================
.proc XEPOpen
		bit		opflag
		beq		not_open
		ldy		#1
		rts
		
not_open:
		eor		opflag
		sta		opflag
		
		lda		#0
		sta		sdmctl
		sta		dmactl
		
		;switch to ORA
		lda		pactl
		ora		#$04
		sta		pactl

		;raise input line
		ldx		portbit
		stx		porta

		;switch to DDRA
		and		#$fb
		sta		pactl

		;switch to port bit to output
		stx		porta

		;switch back to ORA
		ora		#$04
		sta		pactl
		
		php
		jsr		XEPEnterCriticalSection
		
.if XEP_OPTION_TURBO
		jsr		XEPSetTransmitStd
		jsr		XEPReset
		bpl		open_successful

std_failed:
		;delay a bit in case the XEP-80 is farked up
		ldx		#$40
delay_loop:
		lda		vcount
		cmp:req	vcount
		dex
		bne		delay_loop

		jsr		XEPSetTransmitFast
		jsr		XEPReset
		bpl		open_successful

.else
		jsr		XEPReset
		bpl		open_successful
.endif

		tya
		pha
		jsr		XEPClose.force_close
		pla
		tay
xit:
		jsr		XEPLeaveCriticalSection
		plp
		tya
		rts
		
open_successful = xit
.endp

;==========================================================================
.proc XEPResetScreenVars
		lda		#2
		sta		lmargn
		sta		colcrs
		mva		#79 rmargn
		lda		#0
		sta		rowcrs
		sta		colcrs+1
		rts
.endp

;==========================================================================
.proc XEPReset
		lda		#XEPCMD_MASTER_RESET
		jsr		XEPTransmitCommand
		jsr		XEPReceiveByte
		bmi		init_timeout
		cmp		#$01
		beq		init_ok
		ldy		#CIOStatNAK
init_timeout:
		rts

init_ok:
		jsr		XEPResetScreenVars

.if XEP_OPTION_TURBO
		;enter burst mode
		lda		#XEPCMD_ENTER_BURST_MODE
		jsr		XEPTransmitCommand

		;set UART multiplex register to transmit at half rate
.if XEP_TURBO_4X
		lda		#$04			;/4
.else
		lda		#$02			;/2
.endif
		clc
		jsr		XEPTransmitByte
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		lda		#XEPCMD_SET_UMX
		jsr		XEPTransmitCommand
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		;switch UART prescaler from /8 to /4
.if XEP_TURBO_4X
		lda		#$02
.else
		lda		#$05			;/6 baud divisor
.endif
		clc
		jsr		XEPTransmitByte
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		lda		#XEPCMD_SET_EXTRA_BYTE
		jsr		XEPTransmitCommand
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		lda		#$10			;/4 prescaler
		clc
		jsr		XEPTransmitByte
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		lda		#XEPCMD_SET_BAUD_RATE
		jsr		XEPTransmitCommand
		sta		wsync
		sta		wsync
		jsr		XEPWaitBurstACK

		;switch to fast transmit
		jsr		XEPSetTransmitFast

		;exit burst mode
		lda		#XEPCMD_EXIT_BURST_MODE
		jsr		XEPTransmitCommand
.endif

		ldy		#1
		rts
.endp

;==========================================================================
.proc XEPClose
		bit		opflag
		beq		already_closed
		eor		opflag
		sta		opflag
		bne		already_closed
		
force_close:
		lda		pactl
		ora		#$04
		sta		pactl
		ldx		#$ff
		stx		porta
		and		#$fb
		sta		pactl
		stx		porta
		ora		#$04
		sta		pactl
		
		mva		#$22 sdmctl
		sta		dmactl
		
already_closed:
		ldy		#1
		rts
.endp

;==========================================================================
.proc XEPEnterCriticalSection
		mvy		#0 nmien
		sei
		rts
.endp

;==========================================================================
.proc XEPLeaveCriticalSection
		mvx		#$40 nmien
		cpy		#0
		rts
.endp

;==========================================================================
.proc XEPReceiveCursorUpdate
		jsr		XEPReceiveByte
		bmi		err
		tax
		bmi		horiz_or_vert_update
		sta		colcrs
		sta		curcol
		rts
horiz_or_vert_update:
		cmp		#$c0
		bcs		vert_update
		and		#$7f
		sta		colcrs
		sta		curcol
		jsr		XEPReceiveByte
		bmi		err
vert_update:
		and		#$1f
		sta		rowcrs
		sta		currow
		tya
err:
		rts
.endp

;==========================================================================
.if XEP_OPTION_TURBO
.proc XEPTransmitCommand
		sec
.def :XEPTransmitByte
		jmp		XEPTransmitByteStd
.endp

.proc XEPSetTransmitFast
		_ldalo	#XEPTransmitByteFast
		sta		XEPTransmitByte+1
		_ldahi	#XEPTransmitByteFast
		sta		XEPTransmitByte+2
		rts
.endp

.proc XEPSetTransmitStd
		_ldalo	#XEPTransmitByteStd
		sta		XEPTransmitByte+1
		_ldahi	#XEPTransmitByteStd
		sta		XEPTransmitByte+2
		rts
.endp

.else
XEPTransmitByte = XEPTransmitByteStd
XEPTransmitCommand = XEPTransmitCommandStd
.endif

;==========================================================================
; Input:
;	A = byte
;	C = command flag (1 = command, 0 = data)
;
; Modified:
;	A, X
;
; Preserved:
;	Y
;
.proc XEPTransmitCommandStd
		sec
.def :XEPTransmitByteStd
		;##ASSERT (p&4)
		;##TRACE "Transmitting byte %02X" (a)
		sta		shdatal
		rol		shdatah
		
		;send start bit
		lda		portbit
		eor		#$ff
		sta		wsync
		sta		porta
		
		;send data bits
		ldx		#9
transmit_loop:
		lsr		shdatah
		ror		shdatal
		lda		#0
		scs:lda	portbit
		eor		#$ff
		sta		wsync
		sta		porta
		dex
		bne		transmit_loop
		
		;send stop bit
		lda		#$ff
		sta		wsync
		sta		porta
		rts
.endp

;==========================================================================
.if XEP_OPTION_TURBO
.proc XEPTransmitCommandFast
		sec
.def :XEPTransmitByteFast
		sta		shdatal
		lda		#$ff
		rol
		sta		shdatah
		
		;send start bit
		lda		portbit
		eor		#$ff
		ldy		#$ff
		sta		wsync
		sta		porta			;4		*,105-107
		
.if XEP_TURBO_4X
		;Ideal 4X timing: 108, 22.5, 51, 79.5

		;send 9 data bits and stop bit
		ldx		#5				;2		108-109
		bit		$80				;3		110-112
transmit_loop:
		lsr		shdatah			;6		113-5
		ror		shdatal			;6		6-11
		bcs		bit1_1			;2/3	12-13 / 12-14
		bit		$00				;3		14-16
		nop						;2		17-18
		sta		porta			;4		19-22[!]
		bcc		bit1_0			;3		23-24, 25*, 26
bit1_1:
		bit		portbit			;4		15-18
		sty		porta			;4		19-22[!]
		bcs		bit1_0			;3		23-24, 25*, 26
bit1_0:

		lsr		shdatah			;6		27-28, 29*, 30-32, 33*, 34
		ror		shdatal			;6		35-36, 37*, 38-40, 41*, 42
		bcs		bit2_1			;2/3	43-44 / 43-44, 45*, 46
		sta		porta-$ff,y		;5		45*, 46-48, 49*, 50-51 [!]
		bcc		bit2_0			;3		52, 53*, 54, 55
bit2_1:
		sty		porta			;4		47-48, 49*, 50-51[!]
		bcs		bit2_0			;3		52, 53*, 54, 55
bit2_0:

		dex						;2		56, 57*, 58
		beq		done			;2/3	59-60 / 59-61

		lsr		shdatah			;6		61-66
		ror		shdatal			;6		67-72
		bcs		bit3_1			;2/3	73-74 / 73-75
		sta		porta-$ff,y		;5		75-79 [!]
		bcc		bit3_0			;3		80-82
bit3_1:
		sty		porta			;4		76-79 [!]
		bcs		bit3_0			;3		80-82
bit3_0:

		lsr		shdatah			;6		83-88
		ror		shdatal			;6		89-94
		bcs		bit4_1			;2/3	95-96 / 95-97
		sta		wsync			;4		97-100
		sta		porta			;4		101, 102-104*, 105-107 [!]
		dex						;2		108-109
		bne		transmit_loop	;3		110-112
bit4_1:
		sta		wsync			;4		98-101
		sty		porta			;4		102, 103-104*, 105-107 [!]
		dex						;2		108-109
		bne		transmit_loop	;3		110-112

done:
		rts

.else
		;send 9 data bits
		ldx		#9				;2		108-109
		bit		$80				;3		110-112
transmit_loop:
		lsr		shdatah			;6		113-5
		ror		shdatal			;6		6-11
		lda		#$ff			;2		12-13
		scs:eor	portbit			;3/5	14-21
		scc:cmp	portbit			;5/3	"
		pha:pla					;7		22-24,25*,26-28,29*,30
		pha:pla					;7		31-32,33*,34-36,37*,38-39
		cmp		portbit			;2		40,41*,42,43,44
		sta		porta			;4		45*,46,47,48,49*,50

		lda		#$ff			;2
		dex
		beq		transmit_done

		lsr		shdatah			;6
		ror		shdatal			;6
		scs:eor	portbit			;3/5
		sta		wsync			;4
		sta		porta			;4		*,105-107

		dex						;2		108-109
		bne		transmit_loop	;3		110-112		!! - unconditional

transmit_done:
		;send stop bit -- we do this separately to save a few cycles
		sta		wsync
		sta		porta
.endif
		rts
.endp
.endif

;==========================================================================
.proc XEPReceiveByte
		;set timeout (we are being sloppy on X to save time)
		ldy		#$40
		
		;wait for PORTA bit to go low
		lda		portbitr
wait_loop:
		bit		porta
		beq		found_start
		dex
		bne		wait_loop
		dey
		bne		wait_loop
		
		;timeout
		;##TRACE "Timeout"
		ldy		#CIOStatTimeout
		rts
		
found_start:
		;wait until approx middle of start bit
		ldx		#10				;2
		dex:rne					;49
		
		;sample the center of the start bit, make sure it is one
		bit		porta
		bne		wait_loop		;3
		pha						;3
		pla						;4
		
		;now shift in 10 bits at 105 CPU cycles apart (114 machine cycles)
		ldx		#10				;2
receive_loop:
		ldy		#14				;2
		dey:rne					;69
		bit		$00				;3
		ror		shdatah			;6
		ror		shdatal			;6
		lda		porta			;4
		lsr						;2
		and		portbit			;4
		clc						;2
		adc		#$ff			;2
		dex						;2
		bne		receive_loop	;3
		
		;check that we got a proper stop bit
		bcc		stop_bit_bad
		
		;shift out the command bit into the carry and return
		lda		shdatah
		rol		shdatal
		rol
		;##TRACE "Received byte %02X" (a)
		ldy		#1
		rts

stop_bit_bad:
		ldy		#CIOStatSerFrameErr
		rts
.endp

;==========================================================================
; Note that DOS 2.0's AUTORUN.SYS does some pretty funky things here -- it
; jumps through (DOSINI) after loading the handler, but that must NOT
; actually invoke DOS's init, or the EXE loader hangs. Therefore, we have
; to check whether we're handling a warmstart, and if we're not, we have
; to return without chaining.
;
.proc Reinit
		;reset work vars
		ldx		#data_end-data_begin-1
		lda		#0
		sta:rpl	data_begin,x-
		
		;open 80-column display
		lda		#$40
		jsr		XEPOpen
		bpl		open_success

		sec
		rts

open_success:
		;install CIO handlers for E: and S:
		ldx		#30
check_loop:
		lda		hatabs,x
		cmp		#'E'
		bne		not_e
		_ldalo	#XEPEDevice
		sta		hatabs+1,x
		_ldahi	#XEPEDevice
		sta		hatabs+2,x
		bne		not_s
not_e:
		cmp		#'S'
		bne		not_s
		_ldalo	#XEPSDevice
		sta		hatabs+1,x
		_ldahi	#XEPSDevice
		sta		hatabs+2,x
not_s:
		dex
		dex
		dex
		bpl		check_loop

		;reset put char vector for E:
		_ldalo	#[XEPEditorPutByte-1]
		sta		icptl

		_ldahi	#[XEPEditorPutByte-1]
		sta		icpth

		;adjust MEMTOP
		_ldalo	#base_addr
		sta		memtop
		_ldahi	#base_addr
		sta		memtop+1

		;mark success for Init
		clc

		;check if this is a warmstart, and don't call into DOS if not
		lda		warmst
		beq		skip_chain
		
		jmp		skip_chain
dosini_chain = *-2

skip_chain:
		rts
.endp

;==========================================================================
.proc Init
		;attempt to initialize XEP-80
		jsr		Reinit
		bcs		fail

		;hook DOSINI
		mwa		dosini Reinit.dosini_chain
		
		_ldalo	#Reinit
		sta		dosini
		_ldahi	#Reinit
		sta		dosini+1
		
		;all done
		clc
fail:
		rts
.endp

		run		Init
