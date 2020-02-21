; Altirra BASIC - Error handling module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

errorBadDeviceNo	inc		errno
errorLoadError		inc		errno
errorInvalidString	inc		errno
errorWTF			inc		errno		;17
errorBadRETURN		inc		errno
errorGOSUBFORGone	inc		errno
errorLineTooLong	inc		errno
errorNoMatchingFOR	inc		errno
errorLineNotFound	inc		errno		;12
errorFPError		inc		errno		;11
errorArgStkOverflow	inc		errno
errorDimError		inc		errno		;9
errorInputStmt		inc		errno
errorValue32K		inc		errno
errorOutOfData		inc		errno
errorStringLength	inc		errno		;5
errorTooManyVars	inc		errno
errorValueErr		inc		errno
errorNoMemory		inc		errno		;2
.nowarn .proc errorDispatch
		;##TRACE "Error %u at line %u" db(errno) dw(dw(stmcur))
		;restore stack
		ldx		#$ff
		txs
		
		;clear BREAK flag in case that's what caused us to stop
		stx		brkkey

		;re-terminate or force new if required
		jsr		IoCheckBusy
				
		;set stop line
		ldy		#0
		sty		dspflg			;force off list flag while we have a zero
		sty		fr0+1
		lda		(stmcur),y
		sta		stopln
		iny
		lda		(stmcur),y
		sta		stopln+1
				
		;save off error
		lda		errno
		sta		fr0
		sty		errno			;reset errno to 1

		;check if we are in break -- if so, we should not execute TRAP or
		;set ERRSAVE (but STOPLN should be set!).
		cmp		#$80
		bne		not_break

		;print STOPPED and then jump for opt lineno
		ldx		#msg_stopped-msg_base
		jsr		IoPrintMessageIOCB0
		jmp		print_lineno

not_break:
		;save off error
		sta		errsave

		;check if we have a trap line
		lda		exTrapLine+1
		bmi		no_trap
		
		ldx		exTrapLine
		
		;reset trap line
		sec
		ror		exTrapLine+1
		
		;goto trap line
		;!! - needs to use JSR because stGoto pops off the retaddr!
		jsr		stGoto.gotoFR0Int
		
no_trap:
		;ERROR-   11
		ldx		#msg_error-msg_base
		jsr		IoPrintMessageIOCB0
		jsr		IoPrintInt

print_lineno:
		lda		stopln+1
		sta		fr0+1
		bmi		imm_mode

		ldx		#msg_atline-msg_base
		jsr		IoPrintMessage

		lda		stopln
		sta		fr0
		jsr		IoPrintInt

imm_mode:
		jsr		IoPutNewline
		jmp		execLoop.loop2
.endp