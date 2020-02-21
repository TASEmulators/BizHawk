; Altirra DOS - DUP.SYS
; Copyright (C) 2014-2017 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;==========================================================================

DEBUG_ECHO_CMDLINE = 0

;==========================================================================
; Message base
;
msg_base:
msg_prompt:
		dta		$9B,'D1:',0

msg_no_cartridge:
		dta		'No cartridge',$9B,0

msg_error:
		dta		'Error ',0

msg_errors:
msg_err80	dta		'User break',0
msg_err81	dta		'IOCB in use',0
msg_err82	dta		'Unknown device',0
msg_err83	dta		'IOCB write only',0
msg_err84	dta		'Invalid command',0
msg_err85	dta		'Not open',0
msg_err86	dta		'Invalid IOCB',0
msg_err87	dta		'IOCB read only',0
msg_err88	dta		'End of file',0
msg_err89	dta		'Truncated record',0
msg_err8A	dta		'Timeout',0
msg_err8B	dta		'Device NAK',0
msg_err8C	dta		'Framing error',0
msg_err8D	dta		'Cursor out of range',0
msg_err8E	dta		'Overrun error',0
msg_err8F	dta		'Checksum error',0
msg_err90	dta		'Device error',0
msg_err91	dta		'Bad screen mode',0
msg_err92	dta		'Not supported',0
msg_err93	dta		'Out of memory',0
msg_err94	dta		0
msg_err95	dta		0
msg_err96	dta		0
msg_err97	dta		0
msg_err98	dta		0
msg_err99	dta		0
msg_err9A	dta		0
msg_err9B	dta		0
msg_err9C	dta		0
msg_err9D	dta		0
msg_err9E	dta		0
msg_err9F	dta		0
msg_errA0	dta		'Bad drive #',0
msg_errA1	dta		'Too many files',0
msg_errA2	dta		'Disk full',0
msg_errA3	dta		'Fatal disk error',0
msg_errA4	dta		'File number mismatch',0
msg_errA5	dta		'Bad file name',0
msg_errA6	dta		'Bad POINT offset',0
msg_errA7	dta		'File locked',0
msg_errA8	dta		'Invalid disk command',0
msg_errA9	dta		'Directory full',0
msg_errAA	dta		'File not found',0
msg_errAB	dta		'Invalid POINT',0

;==========================================================================
.proc DupMain
input_loop:
		;close IOCB #1 in case it was left open
		jsr		DOSCloseIOCB1

		;print prompt
		ldx		#msg_prompt-msg_base
		jsr		DupPrintMessage

		;read line
		ldx		#0
		jsr		DupSetupReadLine
		jsr		ciov
		bmi		input_loop

.if DEBUG_ECHO_CMDLINE
		ldx		#0
		jsr		DupSetupReadLine
		lda		#CIOCmdPutRecord
		sta		iccmd
		jsr		ciov
.endif

		;scan for an intrinsic command
		ldx		#0
		stx		zbufp
intrinsic_scan_loop:
		inc		zbufp
		ldy		#0
intrinsic_compare_loop:
		lda		dosvec_lnbuf,y
		and		#$df
		eor		intrinsic_commands,x
		inx
		asl
		bne		intrinsic_mismatch
		iny
		bcc		intrinsic_compare_loop

		;next char must be space or EOL
		lda		dosvec_lnbuf,y
		cmp		#$9b
		beq		intrinsic_hit
		cmp		#' '
		bne		intrinsic_mismatch

intrinsic_hit:
		sty		dosvec_lnoff
		ldx		zbufp
		jsr		DupDispatchIntrinsic
		jmp		input_loop

intrinsic_mismatch:
		lda		intrinsic_commands,x
		beq		intrinsic_fail
		asl
		inx
		bcc		intrinsic_mismatch
		bcs		intrinsic_scan_loop

intrinsic_fail:

		;parse out filename
		mva		#0 dosvec_lnoff
		jsr		DOSCPGetFilename
		beq		input_loop

		;yes, we did -- check if it has an extension
		ldy		#0
dotscan_loop:
		lda		dosvec_fnbuf,y
		iny
		cmp		#'.'
		beq		has_ext
		cmp		#$9b
		bne		dotscan_loop

		;add .COM at the end (if there is room)
		cpy		#13
		bcs		has_ext

		ldx		#3
		dey
comadd_loop:
		lda		com_ext,x
		sta		dosvec_fnbuf,y
		iny
		dex
		bpl		comadd_loop

has_ext:
		;attempt to open exe
		mva		#4 icax1+$10
		mva		#0 icax2+$10
		lda		#CIOCmdOpen
		jsr		DupDoCmdFnbufIOCB1

		;clear WARMST as we are about to stomp user memory and we don't
		;want BASIC to try to resume
		mva		#0 warmst

		;attempt to run it by XIO 40 and then exit (this usually does
		;not return on success)
		mva		#40 iccmd+$10
		mva		#0 icax1+$10
		jsr		DupDoIO
		jmp		input_loop

com_ext:
		dta		'MOC.'
.endp

;==========================================================================
.proc DupPrintError
		sty		zdrva
		ldx		#msg_error - msg_base
		jsr		DupPrintMessage
		lda		zdrva
		pha
		sec
		sbc		#100
		bcc		no_hundreds
		sta		zdrva
		lda		#'1'
		jsr		DupPutchar
no_hundreds:
		;do tens
		ldx		#'0'
		lda		zdrva
		cmp		#10
		bcc		no_tens
tens_loop:
		cmp		#10
		bcc		tens_done
		inx
		sbc		#10
		bcs		tens_loop

tens_done:
		pha
		txa
		jsr		DupPutchar
		pla
no_tens:
		ora		#$30
		jsr		DupPutchar

		pla

		cmp		#$ac
		bcs		xit

		sta		zdrva
		lda		#' '
		jsr		DupPutchar

		mwa		#msg_errors-1 zbufp
msg_loop:
		dec		zdrva
		bpl		print_loop
skip_loop:
		jsr		getchar
		bne		skip_loop
		beq		msg_loop

print_loop_2:
		jsr		DupPutchar
print_loop:
		jsr		getchar
		bne		print_loop_2

xit:
		lda		#$9b
		jmp		DupPutchar
		
getchar:
		inw		zbufp
		ldy		#0
		lda		(zbufp),y
		rts
.endp

;==========================================================================
; Entry:
;	X = start of message
;
.proc DupPrintMessage
		stx		zbufp

put_loop:
		ldx		zbufp
		inc		zbufp
		lda		msg_base,x
		beq		xit
		jsr		DupPutchar
		jmp		put_loop

xit:
		rts
.endp

.proc DupPutchar
		sta		ciochr
		lda		icpth
		pha
		lda		icptl
		pha
		lda		ciochr
		ldx		#0
		rts
.endp

;==========================================================================
.proc DupSetupReadLine
		mwa		#dosvec_lnbuf icbal,x
		lda		#0
		sta		icblh,x
		lda		#64
		sta		icbll,x
		lda		#CIOCmdGetRecord
		sta		iccmd,x
		rts
.endp

;==========================================================================
.proc DupDoCmdFnbufIOCB1
		mwx		#dosvec_fnbuf icbal+$10
.def :DupDoCmdIOCB1 = *
		sta		iccmd+$10
		ldx		#$10
.def :DupDoIO = *
		jsr		ciov
		bmi		error
		rts
error:
		ldx		#$ff
		txs
		jsr		DupPrintError
		jmp		DupMain.input_loop
.endp

;==========================================================================
.proc DupDispatchIntrinsic
		;dispatch to intrinsic
		lda		intrinsic_dispatch_hi-1,x
		pha
		lda		intrinsic_dispatch_lo-1,x
		pha
		rts
.endp

;==========================================================================
intrinsic_commands:
		dta		'CAR','T'+$80
		dta		'DI','R'+$80
		dta		'ERAS','E'+$80
		dta		'PROTEC','T'+$80
		dta		'UNPROTEC','T'+$80
		dta		0


.macro _INTRINSIC_TABLE
		dta		:1[DupCmdCart - 1]
		dta		:1[DupCmdDir - 1]
		dta		:1[DupCmdMultiXio - 1]
		dta		:1[DupCmdMultiXio - 1]
		dta		:1[DupCmdMultiXio - 1]
.endm

intrinsic_dispatch_lo:
		_INTRINSIC_TABLE	<

intrinsic_dispatch_hi:
		_INTRINSIC_TABLE	>

;==========================================================================
.proc DupCmdCart
		;check if we have a cartridge
		lda		ramtop
		cmp		#$a1
		bcs		no_cartridge

		;invoke the cart
		ldx		#$ff
		txs
		jmp		($bffa)

no_cartridge:
		ldx		#<msg_no_cartridge
		jmp		DupPrintMessage

.endp

;==========================================================================
.proc DupCmdDir
restart:
		;parse out filename
		jsr		DOSCPGetFilename
		bne		have_filename

		;we have no filename... shove D: into the command line and retry
		mva		#0 dosvec_lnoff

		ldx		#2
		mva:rpl	d_path,x dosvec_lnbuf,x-
		bmi		restart

have_filename:
		;check if the filename ends in just a drive prefix
		lda		dosvec_fnbuf+3
		cmp		#$9b
		bne		have_pattern

		;no pattern... add *.*
		ldx		#3
		mva:rpl	all_spec,x dosvec_fnbuf+3,x-

have_pattern:
		;open IOCB #1 for directory read mode
		mva		#$06 icax1+$10
		mva		#$00 icax2+$10
		lda		#CIOCmdOpen
		jsr		DupDoCmdFnbufIOCB1

read_loop:
		;read a line at a time
		ldx		#$10
		jsr		DupSetupReadLine
		jsr		ciov
		bpl		read_ok
		cpy		#CIOStatEndOfFile
		beq		read_done
read_ok:
		ldx		#0
		jsr		DupSetupReadLine
		mva		#CIOCmdPutRecord iccmd
		jsr		ciov
		bmi		read_done
		jmp		read_loop

read_done:
		;we let the command interpreter close IOCB #1
		rts

d_path:
		dta		'D:',$9B
all_spec:
		dta		'*.*',$9B
.endp

;==========================================================================
.proc DupCmdMultiXio
		lda		cmd_table-3,x
		pha

		jsr		DOSCPGetFilename
		bne		have_filename
		rts

have_filename:
		pla
		jmp		DupDoCmdFnbufIOCB1

cmd_table:
		dta		$21,$23,$24
.endp
