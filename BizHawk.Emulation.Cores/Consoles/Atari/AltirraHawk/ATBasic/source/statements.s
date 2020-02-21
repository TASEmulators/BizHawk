; Altirra BASIC - Statement module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

?statements_start = *

;===========================================================================
.proc stColor
		jsr		evaluateInt
		stx		grColor
		rts
.endp

;===========================================================================
; ENTER filespec
;
; Loads lines from a file and executes them. The lines are executed as-is;
; if there are any immediate mode commands without a line number, they are
; executed immediately.
;
; Note that ENTER uses the same IOCB (#7) that the RUN, LOAD, and SAVE
; commands use. RUN and LOAD terminate execution, so no problem there,
; but SAVE causes the ENTER process to crap out with an I/O error. The
; SAVE itself *does* succeed, which means that SAVE must close IOCB #7
; first.
;
; ENTER may be used in deferred mode, but execution stops after the
; statement is executed. This is equivalent to a STOP, so sounds and I/O
; channels are not affected.
;
; IOCB #7 is closed before the filename is evaluated, which matters
; if the evaluation fails with an error.
;
.proc stEnter
_vectmp = $0500

		;Use IOCB #7 for compatibility with Atari BASIC
		;get filename
		jsr		IoSetupIOCB7AndEval
				
		;issue open call for read
		jsr		IoDoOpenReadWithFilename

		;set exec IOCB to #7
		stx		iocbexec

		;restart execution loop, skipping the banner
		jmp		execLoop.loop2
.endp


;===========================================================================
stLet = evaluateAssignment

;===========================================================================
stData = stRem

;===========================================================================
stCom = stDim

;===========================================================================
; CLOSE #iocb
;
; Closes the given I/O channel. No error results if the IOCB is already
; closed.
;
; Errors:
;	Error 20 if IOCB #0 or #1-32767
;	Error 7 if IOCB #32768-65535
;	Error 3 if IOCB# not in [0,65535]
;
.proc stClose
		jsr		evaluateHashIOCB
close_iocb:
		jmp		IoClose
.endp

;===========================================================================
.proc stDir
		jsr		IoSetupIOCB7AndEval
		lda		#6
		ldy		argsp
		bne		open_fn
		ldy		#<devpath_d1all
		jsr		IoOpenStockDeviceIOCB7
		bpl		read_loop
open_fn:
		jsr		IoDoOpenWithFilename
read_loop:
		ldx		#$70
		jsr		IoReadLine
		beq		stClose.close_iocb
		ldx		#0
		jsr		IoSetupReadLine
		lda		#CIOCmdPutRecord
		jsr		IoDoCmdX
		bpl		read_loop			;!! N=0 - error would be trapped
.endp

;===========================================================================
; CLR
;
; Clears all numeric values to zero and un-dimensions any string and numeric
; arrays. The runtime stack is also cleared, so no FOR or GOSUB frames are
; left afterward.
;
; The current COLOR, degree/radian mode, and TRAP line are not affected.
;
.proc stClr
		jsr		zfr0			;!! - also sets A=0
		jsr		VarGetAddr0
		bne		loopstart
clearloop:
		;clear variable info and value
		jsr		VarStoreFR0
		
		;clear dimensioned bits for arrays/strings
		ldx		#varptr
		lda		(0,x)
		and		#$c0
		sta		(0,x)
		
		;next variable
		lda		#8
		jsr		VarAdvancePtrX
loopstart:
		lda		varptr
		cmp		stmtab
		lda		varptr+1
		sbc		stmtab+1
		bcc		clearloop
		
		;empty the string/array table region and runtime stack		
		;note: this loop is reused by NEW!
clear_arrays:
		ldx		#<-4
reset_loop:
		lda		starp+4,x
		sta		runstk+4,x
		inx
		bne		reset_loop

		;reset APPMHI
		jmp		MemAdjustAPPMHI
.endp


;===========================================================================
.nowarn .proc stDegRad
.def :stDeg = *
		lda		#6
		dta		{bit $0100}
.def :stRad = *
		lda		#0
		sta		degflg
done:
		rts
.endp

;===========================================================================
.proc stDim
loop:
		;DIM is the only statement that allows undimensioned strings to
		;be referenced, so we set a special flag.
		lda		#$40
		jsr		evaluate._assign_entry
		jsr		ExecGetComma
		beq		loop
xit:
		rts
.endp


;===========================================================================
; END
;
; Silences audio channels and closes IOCBs. Does not reset TRAP, PTABW, or
; clear variables.
;
stEnd = immediateModeReset

;==========================================================================
.proc IoCheckBusy
		;unterminate if we had one active from reset
		lda		ioTermFlag
		beq		stDim.xit

		;check if we need to do a full program reset
		bmi		_stNew.reset_entry

		jmp		IoUnterminateString
.endp

;===========================================================================
; NEW
;
; Erases all program text and variables, clears the TRAP line, silences
; sound, closes IOCBs, resets the tab width to 10, and resets angular mode
; to radians.
;
; Not affected by new: COLOR
;
.proc _stNew
.def :stLomem
		jsr		evaluateInt
		stx		memlo
		sta		memlo+1
.def :stNew
		jsr		reset_entry
		jsr		execRestore
		jmp		immediateModeReset

reset_entry:
		ldy		memlo+1
		lda		memlo
		sta		lomem

		;set up second argument stack pointer
		clc
		adc		#$6c
		sta		argstk2
		tya
		adc		#0
		sta		argstk2+1

		;initialize LOMEM from MEMLO
		iny
		sty		lomem+1

		;clear remaining tables
		;reset trap line
		;copy LOMEM to VNTP/VNTD/VVTP/STMTAB/STMCUR/STARP/RUNSTK/MEMTOP2
		ldx		#<-16
		stx		exTrapLine+1
		jsr		stClr.reset_loop

		;reset I/O termination flag
		stx		ioTermFlag		;!! - 0

		dec		lomem+1

		;reset tab width (not done by CLR, END, LOAD, or RUN!).
		lda		#10
		sta		ptabw

		;insert byte at VNTD
		stx		degflg			;!! - set degflg to $00 (radians)
		stx		a2+1
		inx
		stx		a2
		ldx		#vvtp
		jsr		MemAdjustTablePtrs

		;write sentinel into variable name table
		;insert three bytes at STARP
		ldy		#3
		sty		a2
		mva:rpl	empty_program,y (vntp),y-
		ldx		#starp
		jmp		MemAdjustTablePtrs
.endp

;===========================================================================
.proc stCload
		;open IOCB #7 to C: device for read
		lda		#$04
		jsr		IoOpenCassette
		
		;do load
		sec
		ror		stLoadRun._loadflg
		bmi		stLoadRun.with_open_iocb
.endp

;===========================================================================
; RUN [sexp]
;
; Optionally loads a file from disk and begins execution.
;
; All open IOCBs are closed and sound channels silenced prior to execution.
;
;===========================================================================
; LOAD filespec
;
; Loads a BASIC program in binary format.
;
; Execution is reset and the program is wiped prior to beginning the load.
; This means that the current program is lost even if the load fails.
; However, the program is not lost if the file open fails. If the file
; load fails, a NEW is performed. Yes, this means that PTABW is reset if
; and only if the open succeeds and load fails.
;
.proc stLoadRun
_vectmp = fr0
_loadflg = stScratch		;N=0 for run, N=1 for load

.def :stRun
		;set up for run
		lsr		_loadflg

		;check if we have a filename
		jsr		ExecTestEnd
		beq		do_imm_or_run
		bne		run_entry

.def :stLoad
		sec
		ror		_loadflg
		
run_entry:
		;Use IOCB #7 for compatibility with Atari BASIC
		;pop filename
		jsr		IoSetupIOCB7AndEval	

loader_entry:
		;do open
		jsr		IoDoOpenReadWithFilename

with_open_iocb:
		;load vector table to temporary area (14 bytes)
		lda		#CIOCmdGetChars
		jsr		setup_vector_io
		
		;check if first pointer is zero -- if not, assume bad file
		lda		_vectmp
		ora		_vectmp+1
		bne		bogus

		;taint program to force reset if we fail
		dec		ioTermFlag

		;relocate pointers
		ldx		#$80-12
relocloop:
		lda		_vectmp+14-$80,x
		add		lomem
		sta		lomem+14-$80,x
		tay
		lda		_vectmp+15-$80,x
		adc		lomem+1
		sta		lomem+15-$80,x
		jsr		MemCheckAddrAY
		inx
		inx
		bpl		relocloop

		;load remaining data at VNTP
		jsr		setup_main_io

do_imm_or_run:

		;mark program as OK
		inc		ioTermFlag

		;close IOCBs (including the one we just used) and reset sound
		jsr		ExecReset
		
		;clear runtime variables
		jsr		stClr
		
		;check if we should run
		asl		_loadflg
		scs:jmp	exec
		
		;jump to immediate mode loop
		jmp		immediateMode

bogus:
		jmp		errorLoadError
		
setup_vector_io:
		pha
		ldx		#$70
		
		ldy		#0
		lda		#<_vectmp
		jsr		IoSetupBufferAddress

		ldy		#14
		jsr		IoSetupBufferLengthY

		pla
		jmp		IoDoCmdX

setup_main_io:
		ldx		iocbidx
		lda		vntp
		ldy		vntp+1
		jsr		IoSetupBufferAddress
		sec
		lda		starp
		sbc		vntp
		tay
		lda		starp+1
		sbc		vntp+1
		jsr		IoSetupBufferLengthAY
		jmp		ioChecked
.endp

;===========================================================================
.proc stCsave
		;open IOCB #7 to C: device for write
		lda		#$08
		jsr		IoOpenCassette
		
		;do load
		bpl		stSave.with_open_iocb		;!! - unconditional
.endp


;===========================================================================
; SAVE filespec
;
; It is possible to issue a SAVE command during ENTER processing. For this
; reason, we must close IOCB #7 before reopening it.
;
.proc stSave
_vectmp = fr0

		;Use IOCB #7 for compatibility with Atari BASIC
		;close it in case ENTER is active
		;get filename
		jsr		IoSetupIOCB7AndEval
		
		;issue open call for write
		lda		#8
		jsr		IoDoOpenWithFilename
		
with_open_iocb:

		;Set up and relocate pointers. There are two gotchas here:
		;
		;1) We must actually subtract LOMEM from itself, producing an
		;   offset of 0. This is in fact required.
		;
		;2) Atari BASIC assumes that the load length is the STARP offset
		;   minus $0100, but rev. B has a bug where it extends the argument
		;   stack by 16 bytes each time it saves. This results in a
		;   corresponding amount of junk at the end of the file that must
		;   be there for the file to load. Because this is rather dumb,
		;   we fix up the offsets on save. We don't do this on load
		;   because there are a number of programs in the wild that have
		;   such offsets and we don't want to shift the memory layout.

		;clear LOMEM offset; note that VNTP offset will already be $0100
		;since we're subtracting (VNTP-$0100) from it
		jsr		zfr0

		ldx		#12
relocloop:
		sec
		lda		lomem,x
		sbc		vntp
		sta		_vectmp,x
		lda		lomem+1,x
		sbc		vntp+1
		adc		#0
		sta		_vectmp+1,x
		dex
		dex
		bne		relocloop

		;write vector table (14 bytes)
		lda		#CIOCmdPutChars
		jsr		stLoadRun.setup_vector_io
		
		;write from VNTP from STARP
		jsr		stLoadRun.setup_main_io
		
		;close and exit
		jmp		IoClose
.endp


;===========================================================================
; STATUS #iocb, avar
;
; Retrieves the status code of an I/O channel and puts it into the given
; numeric variable.
;
; Bugs:
;	Atari BASIC allows a numeric array element to be passed as the second
;	parameter and stomps the array entry with a number. We do not currently
;	support this bug.
;
.proc stStatus
		jsr		evaluateHashIOCB
		jsr		ExprSkipCommaAndEvalVar
		
		lda		#CIOCmdGetStatus
		jsr		IoDoCmd

		lda		icsta,x
		jmp		stGet.store_byte_to_var
.endp

;===========================================================================
; NOTE #iocb, avar, avar
;
.proc stNote
		;consume #iocb,
		jsr		evaluateHashIOCB
		
		;issue XIO 38 to get current position
		lda		#38
		jsr		IoDoCmd
		
		;copy ICAX3/4 into first variable
		jsr		ExprSkipCommaAndEvalVar
		ldy		iocbidx
		lda		icax3,y
		ldx		icax4,y
		jsr		MathWordToFP
		jsr		VarStoreFR0
		
		;copy ICAX5 into second variable
		jsr		ExprSkipCommaAndEvalVar
		ldx		iocbidx
		lda		icax5,x
		jmp		stGet.store_byte_to_var
.endp


;===========================================================================
; POINT #iocb, avar, avar
;
; Note that there is only one byte in the IOCB for the sector offset (AUX5);
; Atari BASIC silently drops the high byte without error.
;
; For some reason, this command only takes avars instead of aexps, even
; though they're incoming parameters.
;
.proc stPoint
		;consume #iocb,
		jsr		evaluateHashIOCB
		
		;consume comma and then first var, which holds sector number
		lda		#icax3-icbal
		jsr		stGetIoWord
		
		;consume comma and then second var, which holds sector offset
		jsr		ExprSkipCommaAndEvalPopInt
		
		;move to ICAX5
		txa
		ldx		iocbidx
		sta		icax5,x
		
		;issue XIO 37 and exit
		lda		#37
		jmp		IoDoCmdX
.endp


;===========================================================================
; XIO cmdno, #aexp, aexp1, aexp2, filespec
;
; Performs extended I/O to an IOCB.
;
; This issues a CIO call as follows:
;	ICCMD = cmdno
;	ICAX1 = aexp1
;	ICAX2 = aexp2
;	ICBAL/H = filespec
;
; Quirks:
;	- Neither AUX bytes are modified until all arguments are successfully
;	  evaluated.
;	- Because ICAX1 is modified and not restored even in the event of
;	  success, subsequent attempts to read from the channel can fail due
;	  to AUX1 permission checks in SIO. Writes can work because BASIC
;	  bypasses CIO and jumps directly to ICPTL/H+1, but reads still go
;	  through CIO. One symptom that occurs is LOCATE commands failing with
;	  Error 131 after filling with XIO 18,#6,0,0,"S".
;
;	  This is avoided if the handler itself restores AUX1 or aexp1 is set
;	  to the permission byte instead.
;
;===========================================================================
; OPEN #iocb, aexp1, aexp2, filename
;
; Opens an I/O channel.
;
; Errors:
;	Error 7 if aexp1 or aexp2 in [32768, 65535]
;	Error 3 if aexp1 or aexp2 not in [0, 65535]
;	Error 20 if iocb #0
;
; Quirks:
;	AUX1/2 are overwritten and CIOV is called without checking whether the
;	IOCB is already open. This means that if the IOCB is already in use,
;	its permission byte will be stomped by the conflicting OPEN.
;
;	If an error occurs during evaluation of any of the parameters, none of
;	them are written to the IOCB.
;
.proc stXio
		jsr		evaluateInt
		inc		exLineOffset
		txa
		dta		{bit $0100}
.def :stOpen = *
		lda		#CIOCmdOpen
		pha
		jsr		ExprEvalIOCBCommaInt
		txa
		pha
		jsr		ExprSkipCommaAndEvalPopInt
		txa
		pha

		;get filename
		jsr		ExprSkipCommaAndEval
		
		ldx		iocbidx
		pla
		sta		icax2,x
		pla
		sta		icax1,x
				
		;issue command
		pla
		jmp		IoDoWithFilename
.endp

;===========================================================================
stPoke = stDpoke
.proc stDpoke
		stx		stScratch3

		;evaluate and save address
		jsr		evaluateInt

		;save address
		sta		stScratch+1
		stx		stScratch
		
		;skip comma and evaluate value
		jsr		ExprSkipCommaAndEvalPopInt
		
		;set up for DPOKE
		ldy		#1

		;check if we're doing POKE -- note that POKE's token is odd ($1F)
		;while DPOKE's is even ($3E)		
		lsr		stScratch3
		bcs		poke_only

		;do poke
		;;##TRACE "POKE %u,%u" dw(fr0+1) db(fr0)
		sta		(stScratch),y
poke_only:
		txa
		dey
		sta		(stScratch),y
		
xit:
		;done
		rts
.endp

;===========================================================================
;
; A comma causes movement to the next tab stop, where tab stops are 10
; columns apart. These are independent from the E: tab stops. Position
; relative to the tab stops is determined by the number of characters
; output since the beginning of the PRINT statement and is independent of
; the actual cursor position or any embedded EOLs in printed strings. A
; minimum of two spaces are always printed.
;
.proc stPrint
		;reset comma tab stop position
		mva		ptabw ioPrintCol

		;set IOCB, defaulting to #0 if there is none
		jsr		evaluateHashIOCBOpt

have_iocb_entry:
		;clear dangling flag
		lsr		printDngl
		
		;begin loop
		bpl		token_loop			;!! - unconditional

is_comma:
		;emit spaces until we are at the next tabstop, with a two-space
		;minimum
		jsr		IoPutSpace
comma_tab_loop:
		jsr		IoPutSpace
		lda		ioPrintCol
		cmp		ptabw
		bne		comma_tab_loop

token_semi:
		;set dangling flag
		sec
		ror		printDngl
		
token_next:
		inc		exLineOffset
token_loop:
		jsr		ExecTestEnd
		bne		not_eos

		;skip EOL if print ended in semi or comma
		bit		printDngl
		bmi		stDpoke.xit
		
		jmp		IoPutNewline

not_eos:
		;check if we have a semicolon; we just ignore these.
		cmp		#TOK_EXP_SEMI
		beq		token_semi
		
		;check if we have a comma
		cmp		#TOK_EXP_COMMA
		beq		is_comma
		
		;must be an expression -- clear the dangling flag
		lsr		printDngl
		
		;evaluate expr
		jsr		evaluate
		
		;check if we have a number on the argstack
		lda		expType
		bmi		is_string
		
		;print the number
		jsr		IoPrintNumber
		bmi		token_loop			;!! - unconditional
		
is_string:
		;print chars
		jsr		IoSetInbuffFR0
strprint_loop:
		lda		fr0+2
		bne		strprint_loop1
		dec		fr0+3
		bmi		token_loop
strprint_loop1:
		dec		fr0+2
		
		ldy		#0
		lda		(inbuff),y
		jsr		IoPutCharAndInc
		bpl		strprint_loop		;!! - unconditional
.endp

;===========================================================================
; RESTORE [aexp]
;
; Resets the line number to be used next for READing data.
;
; Errors:
;	Error 3 if line not in [0,65535]
;	Error 7 if line in [32768,65535]
;
; The specified line does not have to exist at the time that RESTORE is
; issued. The next READ will start searching at the next line >= the
; specified line.
;
.proc stRestore
		jsr		execRestore
		
		;check if we have a line number
		jsr		ExecTestEnd
		beq		no_lineno
		
		;we have a line number -- pop it and copy to dataln
		jsr		ExprEvalPopIntPos
write_dataln:
		stx		dataln
		sta		dataln+1
no_lineno:
		rts
.endp

;===========================================================================
.proc execRestore
		lda		#0
		sta		dataptr+1
		tax
		beq		stRestore.write_dataln		;!! - unconditional
.endp

;===========================================================================
stStop = execStop

;===========================================================================
; POP
;
; This statement removes a GOSUB or FOR frame from the runtime stack. No
; error is issued if the runtime stack is empty.
;
; Test case: GOMOKO.BAS
;
.proc stPop
		;##ASSERT dw(runstk)<=dw(memtop2) and !((dw(memtop2)-dw(runstk))&3) and (dw(runstk)=dw(memtop2) or ((db(dw(memtop2)-4)=0 or db(dw(memtop2)-4)>=$80) and dw(dw(memtop2)-3) >= dw(stmtab)))

		;check if runtime stack is empty
		jsr		stReturn.check_rtstack_empty
		bcs		done
		
		;pop back one frame
		lda		#$fc
		jsr		dec_ptr			;(!) carry is clear

pop_frame_remainder:
		;check if we popped off a GOSUB frame
		ldy		#0
		lda		(memtop2),y
		bpl		done
		lda		#$f4
dec_ptr_2:
		clc
dec_ptr:
		adc		memtop2
		sta		memtop2
		scs:dec	memtop2+1
done:
		rts		
.endp


;===========================================================================
stQuestionMark = stPrint


;===========================================================================
; LOCATE aexp1, aexp2, var
;
; Positions the cursor at an (X, Y) location and reads a pixel.
;
; Errors:
;	Error 3 - X<0, Y<0, or Y>=256
;	Error 131 - S: not open on IOCB #6
;
; This statement only works with S: and will fail if IOCB #6 is closed. This
; leads to an oddity where LOCATE doesn't work immediately after BASIC boots,
; but will work if GR.0 is issued, because in that case S: is opened.
; CLUES.BAS depends on this behavior.
;
.proc stLocate
		jsr		stSetupCommandXY
		
		;select IOCB #6
		stx		iocbidx
				
		;do get char and store
		bne		stGet.get_and_store
.endp


;===========================================================================
.proc stGet
		jsr		evaluateHashIOCB

get_and_store:
		jsr		ExprSkipCommaAndEvalVar
		
		ldx		iocbidx
		ldy		#0
		jsr		IoSetupBufferLengthY
		lda		#CIOCmdGetChars
		jsr		IoDoCmd
		
store_byte_to_var:
		;convert retrieved byte to float
		jsr		MathByteToFP
		
		;store into variable and exit
.def :VarStoreFR0
		ldy		#2
.def :VarStoreExtFR0_Y
loop:
		mva		fr0-2,y (varptr),y
		iny
		cpy		#8
		bne		loop
		rts						;!! - READ/INPUT relies on Z set
.endp


;===========================================================================
; PUT #iocb, aexp
;
; Writes the given character by number to the specified I/O channel.
;
; Errors:
;	Error 20 if IOCB #0 or 8-32767
;	Error 7 if IOCB #32768-65535
;	Error 3 if IOCB# not in [0,65535]
;
.proc stPut
		jsr		ExprEvalIOCBCommaInt
		txa
		jmp		IoPutCharDirect
.endp


;===========================================================================
; GRAPHICS aexp
;
; Open a graphics mode.
;
; Errors:
;	3 - if mode <0 or >65535
;
; Quirks:
;	- Atari BASIC closes IOCB #6 before evaluating the expression. This
;	  breaks plot commands on the graphics screen if the eval fails, i.e.
;	  GR. 1/0.
;
; An oddity of this command is that it opens S: on IOCB #6 even in mode 0.
; This means that the I/O environment is different post-boot and after a
; GR.0 -- before then, graphics commands like PLOT and LOCATE will fail on
; the text mode screen, but after issuing GR.0 they will work. CLUES.BAS
; depends on this.
;
; This command must not reopen the E: device. Doing so will break Space
; Station Multiplication due to overwriting graphics data already placed
; at $BFxx.
;
.proc stGraphics
		jsr		evaluateInt

		jsr		pmTryDisable

		;close and reopen IOCB 6 with S:
		lda		fr0
		sta		icax2+$60
		and		#$30
		eor		#$1c 
		ldy		#<devname_s
		ldx		#$60
		jmp		IoOpenStockDeviceX
.endp


;===========================================================================
; PLOT aexp1, aexp2
;
; Plot a point on the graphics screen with the current color.
;
; Errors:
;	Error 3 if X or Y outside of [0,65535]
;	Error 3 if Y in [256, 32767]
;	Error 7 if Y in [32768, 65535]
;
.proc stPlot
		jsr		stSetupCommandXY
		jmp		IoPutCharDirectX
.endp


;===========================================================================
stCp = stDos
.proc stDos
		jsr		ExecReset

		;We may end up returning if DOS fails to load (MEM.SAV error, user
		;backs out!).
		.if CART==0
		jmp		ReturnToDOS
		.else
		jmp		(dosvec)
		.endif
.endp


;===========================================================================
.proc stDrawto
		jsr		stSetupCommandXY
		sta		atachr

		lda		#$11
		jmp		IoDoCmdX
.endp

;===========================================================================
.proc stTrap
		jsr		evaluateInt
		stx		exTrapLine
		sta		exTrapLine+1
		rts
.endp

;===========================================================================
; FOR avar=aexp TO aexp [STEP aexp]
;
; The runtime stack is scanned for conflicting FOR statements, stopping if
; a GOSUB frame is reached. If a FOR statement with the same variable is
; reached, it and any FOR statements in between are removed before the new
; one is added.
;
.proc stFor
		;get and save variable
		lda		(stmcur),y
		sta		stScratch
		
		;clean out stale frames
		lda		memtop2
		pha
		lda		memtop2+1
		pha
		bne		loop_start
loop:
		lda		#$fc
		jsr		stPop.dec_ptr_2

		;fetch the variable
		ldy		#0
		lda		(memtop2),y

		;if this isn't a FOR...NEXT loop, stop here -- Escape From Epsilon
		;requires this
		bpl		done

		;check if the variable matches
		cmp		stScratch
		php

		;advance pointer
		jsr		stPop.pop_frame_remainder

		plp
		beq		found_it

loop_start:
		;check if we're at the bottom of the stack
		jsr		stReturn.check_rtstack_empty
		bcc		loop

done:
		pla
		sta		memtop2+1
		pla
		sta		memtop2

		dta		{bit $0100}		;BIT $6868
found_it:
		pla
		pla

		;check that we have enough room
		lda		#16
		jsr		ExecCheckStack

		;execute assignment to set variable initial value
		jsr		evaluateAssignment
				
		;skip TO keyword, evaluate stop value and push
		jsr		ExprSkipCommaAndEval		;actually skipping TO, not a comma
		ldy		#0
		jsr		push_number
		
		;assume STEP 1
		jsr		fld1

		;check for a STEP keyword
		jsr		ExecTestEnd
		beq		no_step
		
		;skip STEP keyword, then evaluate and store step
		jsr		ExprSkipCommaAndEval
no_step:
		ldy		#6
		jsr		push_number

		;push frame and exit
		lda		stScratch
push_frame:
		ldx		stmcur
		jsr		push_ax
		lda		stmcur+1
		ldx		exLineOffsetNxt
		jsr		push_ax

advance_memtop2_y:
		tya
		jmp		stNext.advance_memtop2

push_ax_1:
		ldy		#1
push_ax:
		sta		(memtop2),y+
		txa
		sta		(memtop2),y+
		rts
		
push_number:
		ldx		#$80-6
_loop1:
		lda		fr0+6-$80,x
		sta		(memtop2),y
		iny
		inx
		bpl		_loop1
chkstk_ok:
		rts
.endp

;===========================================================================
; RETURN
;
; Returns to the execution point after the most recent GOSUB. Any
; intervening FOR frames are discarded (GOMOKO.BAS relies on this).
;
.proc stReturn
		;pop entries off runtime stack until we find the right frame
loop:
		;##ASSERT dw(runstk)<=dw(memtop2) and !((dw(memtop2)-dw(runstk))&3) and (dw(runstk)=dw(memtop2) or ((db(dw(memtop2)-4)=0 or db(dw(memtop2)-4)>=$80) and dw(dw(memtop2)-3) >= dw(stmtab)))
		jsr		fix_and_check_rtstack_empty
		bcs		stack_empty
stack_not_empty:

		;check if we have a GOSUB frame (varbyte=0) or a FOR frame (varbyte>=$80)
		dec		memtop2+1
		ldy		#<-4
		lda		(memtop2),y
		spl:ldy	#<-16

		;pop back one frame, regardless of its type
		jsr		stFor.advance_memtop2_y
		
		;keep going if it was a FOR
		cpy		#<-16
		beq		loop
		
		;switch context and exit
		ldy		#1
		bne		stNext.pop_frame		;!! - unconditional

stack_empty:
		jmp		errorBadRETURN

fix_and_check_rtstack_empty:
		;check if the runtime stack is floating and fix it if needed
		jsr		ExecFixStack
check_rtstack_empty:
		lda		runstk
		cmp		memtop2
		lda		runstk+1
		sbc		memtop2+1
		rts
.endp

;===========================================================================
; NEXT avar
;
; Closes a FOR loop, checks the loop condition, and loops back to the FOR
; statement if the loop is still active. The runtime stack is search for
; the appropriate matching FOR; any other FOR frames found in between are
; discarded. If a GOSUB frame is found first, error 13 is issued.
;
; The step factor is added to the loop variable before a check occurs. This
; means that a FOR I=1 TO 10:NEXT I will run ten times for I=[0..10] and
; exit with I=11. The check is > for a positive STEP and < for a negative
; STEP; the loop will terminate if the loop variable is manually modified
; to be beyond the end value. A FOR I=0 TO 0 STEP 0 loop will not normally
; terminate, but is considered positive step and will stop if I is modified
; to 1 or greater.
;
.proc stNext
		;pop entries off runtime stack until we find the right frame
loop:
		jsr		stReturn.fix_and_check_rtstack_empty
		bcc		stack_not_empty
		
error:
		jmp		errorNoMatchingFOR
		
stack_not_empty:

		;pop back one frame
		lda		#$f0
		jsr		stPop.dec_ptr_2
		
		;check that it's a FOR
		ldy		#$0c
		lda		(memtop2),y
		beq		error
		
		;check that it's the right one
		ldy		exLineOffset
		cmp		(stmcur),y
		bne		loop
		
		;compute variable address
		jsr		VarGetAddr0
		
		;load loop variable
		jsr		VarLoadFR0
		
		;load step		
		ldy		#11
		ldx		#5
pop_loop:
		lda		(memtop2),y
		dey
		sta		fr1,x
		dex
		bpl		pop_loop

		;save off step sign
		sta		stScratch

		;add step to variable		
		jsr		fadd
		jsr		VarStoreFR0
		
		;compare to end value
		ldx		memtop2
		ldy		memtop2+1
		jsr		fld1r
		
		;;##TRACE "NEXT: Checking %g <= %g" fr0 fr1
		jsr		fcomp
		
		;exit if current value is > termination value for positive step,
		;< termination value for negative step
		beq		not_done
		
		ror
		eor		stScratch
		bmi		loop_done

not_done:
		;warp to FOR end
		;;##TRACE "Continuing FOR loop"
		ldy		#$0d
		jsr		pop_frame
		
		;restore frame on stack and continue execution after for
		lda		#$10
advance_memtop2:
		ldx		#memtop2
		jmp		VarAdvancePtrX
		
pop_frame:
		mva		(memtop2),y+ stmcur
		mva		(memtop2),y+ stmcur+1
		mva		(memtop2),y exLineOffsetNxt
		
		;fixup line info cache
restart_line:
		ldy		#2
		mva		(stmcur),y+ exLineEnd		;!! - set Y=3 for exec.direct_bypass

		;##ASSERT dw(stmcur) >= dw(stmtab) and dw(stmcur) < dw(starp)
		;##ASSERT dw(memtop2) >= dw(runstk) and ((dw(memtop2)-dw(runstk))&3)=0
		;##ASSERT dw(memtop2) = dw(runstk) or db(dw(memtop2)-4)=0 or db(dw(memtop2)-4)>=$80
loop_done:
		rts
.endp


;===========================================================================
; ON aexp {GOTO | GOSUB} lineno [,lineno...]
;
; aexp is converted to integer with rounding, using standard FPI rules. The
; resulting integer is then used to select following line numbers, where
; 1 selects the first lineno, etc. Zero or greater than the number provided
; results in execution continuing with the next statement.
;
; The selection value and all line numbers up to that value must pass
; FPI conversion and be below 32768, or else errors 3 and 7 result,
; respectively. In addition, the selection value must be below 256 or
; error 3 results. If the selection value converts to 0, none of the line
; numbers are evaluated or checked, and if it is greater than the number
; provided, all are evaluated.
;
; Examples:
;	ON 1 GOTO 10, 20 (Jumps to line 10)
;	ON 2 GOTO 10, 20 (Jumps to line 20)
;	ON 3 GOTO 10, 20 (Continues execution)
;	ON -0.01 GOTO 10 (Error 3)
;	ON 255.5 GOTO 10 (Error 3)
;	ON 32768 GOTO 10 (Error 7)
;	ON 65536 GOTO 10 (Error 3)
;	ON 0 GOTO 1/0 (Continues execution)
;	ON 2 GOTO 1/0 (Error 11)
;	ON 1 GOTO 10,1/0 (Jumps to line 10)
;
.proc stOn
_index = stScratch
		;fetch and convert the selection value
		jsr		ExprEvalPopIntPos

		;issue error 3 if value is greater than 255		
		bne		stErrorValueError
				
		;exit immediately if index is zero		
		txa
		beq		xit
		sta		_index

		;next token should be GOTO or GOSUB
		jsr		ExecGetComma

		;save GOTO/GOSUB token
		pha

count_loop:
		;check if it's time to branch
		dec		_index
		beq		do_branch

		;evaluate a line number
		jsr		ExprEvalPopIntPos
		
		;read next token and check if it is a comma
		jsr		ExecGetComma
		beq		count_loop
		
		;out of line numbers -- continue with next statement
		pla
xit:
		rts
				
do_branch:
		;check if we should do GOTO or GOSUB
		pla
		lsr
		bcs		stGoto

		;!! fall through to stGosub!
.endp

stGosub:
		;push gosub frame
		;##TRACE "Pushing GOSUB frame: $%04x+$%02x" dw(stmcur) db(exLineOffset)
		lda		#4
		jsr		ExecCheckStack
		lda		#0
		tay
		jsr		stFor.push_frame

		;fall through

.proc stGoto
		;get line number
		jsr		ExprEvalPopIntPos
gotoFR0Int:
		jsr		exFindLineInt
		bcc		not_found
		sta		stmcur
		sty		stmcur+1

		;jump to it
		pla
		pla
		jmp		exec.new_line

not_found:
		jmp		errorLineNotFound
.endp

;===========================================================================
stGoto2 = stGoto

;===========================================================================
stBye = blkbdv

;===========================================================================
; CONT
;
; Resumes execution from the last stop or error point.
;
; Quirks:
;	- The documentation says that CONT resumes execution at the next lineno,
;	  but this is incorrect. Instead, Atari BASIC appears to do an insertion
;	  search for the stop line itself, then skip to the end of that line.
;	  This means that if the stop line is deleted, execution will resume at
;	  the line AFTER the next line.
;
.proc stCont
		;check if we are executing the immediate mode line
		ldy		#1
		lda		(stmcur),y

		;if we aren't (deferred mode), it's a no-op
		bpl		stPosition.xit

		;bail if stop line is >=32K
		ldx		stopln
		lda		stopln+1
		bmi		stGoto.not_found

		;search for the stop line -- okay if this fails
		jsr		exFindLineInt

		;jump to that line
		sta		stmcur
		sty		stmcur+1

		;warp to end of line and continue execution
		jmp		exec.next_line
.endp

;===========================================================================
stSetupCommandXY = stPosition
.proc stPosition
		;evaluate X
		jsr		evaluateInt
		pha					;push X high
		txa
		pha					;push X low

		;skip comma and evaluate Y
		jsr		ExprSkipCommaAndEvalPopIntPos
		bne		stErrorValueError
		
		;position at (X,Y)
		stx		rowcrs
		pla
		sta		colcrs
		pla
		sta		colcrs+1

		;preload IOCB #6 and current color for PLOT/DRAWTO
		lda		grColor
		ldx		#$60			;!! - exit NZ for unconditional branch in LOCATE
xit:
		rts
.endp

;===========================================================================
stErrorValueError:
		jmp		errorValueErr

;===========================================================================
; SOUND voice, pitch, distortion, volume
;
; Modifies a POKEY sound channel.
;
; Errors:
;	Error 3 if voice not in [0,65535] or in [4,32767]
;	Error 7 if voice in [32768,65535]
;	Error 3 if pitch not in [0,65535]
;	Error 3 if distortion not in [0,65535]
;	Error 3 if volume not in [0,65535]
;
; The values are mixed together as follows:
;	AUDFn <- pitch
;	AUDCn <- distortion*16+value
;
; All audio channels are set to unlinked, 64KHz clock, and 17-bit noise by
; this command.
;
; Asynchronous receive mode is also turned off in SKCTL so that channels 3+4
; function. However, a quirk in Atari BASIC is that the shadow SSKCTL is
; *not* updated.
;
.proc stSound
_channel = stScratch

		;get voice
		jsr		ExprEvalPopIntPos
		bne		stErrorValueError
		txa
		cmp		#4
		bcs		stErrorValueError
		asl
		sta		_channel
		
		;get pitch
		jsr		ExprSkipCommaAndEvalPopInt
		txa
		pha

		;get distortion and volume
		jsr		stSetcolor.decode_dual

		ldx		_channel
		sta		audc1,x
		pla
		sta		audf1,x
		
		;force all audio channels to 64K clock and unlinked
		mva		#0 audctl

		;force off asynchronous mode so that channels 3 and 4 work
		mva		#3 skctl
		rts
.endp

;===========================================================================
; SETCOLOR aexpr1, aexpr2, aexpr3
;
; Set OS color register aexpr1 to aexpr2*16+aexpr3.
;
; Errors:
;	Error 7 if aexpr1 in [32768, 65535].
;	Error 3 if aexpr1, aexpr2, or aexpr3 not in [0, 65535].
;
.proc stPmcolor
		;get color index
		jsr		ExprEvalPopIntPos
		cpx		#4
		bcs		stErrorValueError
		txa
		bpl		stSetcolor.pmcolor_entry
.endp

.proc stSetcolor
_channel = stScratch

		;get color index
		jsr		ExprEvalPopIntPos
		cpx		#5
		bcs		stErrorValueError
		txa
		adc		#4
pmcolor_entry:
		sta		_channel

		;get hue and luma
		jsr		decode_dual
		
		;store new color
		ldx		_channel
		sta		pcolr0,x
		rts
		
decode_dual:
		;get hue/distortion
		jsr		decode_single
decode_single:
		asl
		asl
		asl
		asl
		pha
		;get luma/volume
		jsr		ExprSkipCommaAndEvalPopInt
		pla
		adc		fr0				;X*16+Y		
		rts
.endp

;===========================================================================
.proc stLprint
		;open IOCB #7 to P: device for write
		ldy		#<devname_p
		lda		#8
		jsr		IoOpenStockDeviceIOCB7
		
		;do PRINT
		jsr		stPrint.have_iocb_entry
		
		;close IOCB #7
		jmp		IoClose
.endp


;===========================================================================
stImpliedLet = evaluateAssignment

;===========================================================================
stSyntaxError = errorWTF

;===========================================================================
.proc stFileOp
		lda		op_table-TOK_ERASE,x
		pha
		jsr		IoSetupIOCB7AndEval
		pla
		jmp		IoDoWithFilename

op_table:
		dta		$21,$23,$24,0,$20
.endp

;===========================================================================
.proc stMove
		;get source address and save
		jsr		evaluateInt
		pha
		txa
		pha

		;get destination address and save
		jsr		ExprSkipCommaAndEvalPopInt
		pha
		txa
		pha

		;get length
		jsr		ExprSkipCommaAndEval

		;split off the sign and take abs
		jsr		MathSplitSign

		;convert to int and save
		jsr		ExprConvFR0Int
		stx		a3
		sta		a3+1

		;unpack destination and source addresses
		ldx		#$7c
unpack_loop:
		pla
		sta		a0-$7c,x+
		bpl		unpack_loop

		;check if we are doing a descending copy
		bit		funScratch1
		bmi		copy_down

		;copy up
		jmp		copyAscending

copy_down:
		;adjust pointers
		ldy		#a3
		jsr		IntAddToFR0
		ldx		#a1
		jsr		IntAdd

		;copy down
		jmp		copyDescending
.endp

;===========================================================================
.proc stBput
		lda		#CIOCmdPutChars
		dta		{bit $0100}
.def :stBget = *
		lda		#CIOCmdGetChars
		pha

		;consume #iocb,
		jsr		evaluateHashIOCB
		
		;consume comma and then first val (address)
		lda		#icbal-icbal
		jsr		stGetIoWord
		
		;consume comma and then second val (length)
		lda		#icbll-icbal
		jsr		stGetIoWord

		;issue read call and exit
		pla
		jmp		IoDoCmd
.endp

;===========================================================================
.proc stGetIoWord
		ora		iocbidx
		sta		stScratch
		jsr		ExprSkipCommaAndEvalPopInt
		tay
		txa
		ldx		stScratch
.def :IoSetupBufferAddress
		sta		icbal,x
		tya
		sta		icbah,x
		rts
.endp

;===========================================================================
; PMGRAPHICS aexp [BASIC XL/XE]
;
; Enables or disables player/missile graphics mode. 0 disables, 1 enables
; single-line mode, and 2 enables double-line mode.
;
; All players and missiles are reset to position 0 and 1x size. P/M graphics
; memory is not cleared.
;
.proc stPmgraphics
		jsr		ExprEvalPopIntPos
		dex
		bmi		pmDisable
		beq		single_line
		ldx		#1
single_line:

		ldy		pmgmode_tab,x
		lda		mask_tab,x
		and		memtop+1
		clc
		adc		mask_tab,x
		cmp		memtop2+1
		bcs		mem_ok
		
		jmp		errorNoMemory

.def :ExecReset
		jsr		IoCheckBusy


		;close IOCBs 1-7
		ldx		#$70
close_loop:
		jsr		IoCloseX
		txa
		sec
		sbc		#$10
		tax
		bne		close_loop

		;silence all sound channels
		ldx		#7
		sta:rpl	$d200,x-		;!! - A=0 from above
		
.def :pmTryDisable
		lda		pmgbase
		beq		no_pmg

.def :pmDisable
		ldx		#2
		lda		#0
		tay
		sta		gractl
		clc
mem_ok:
		sta		pmgbase
		sta		pmbase
		sty		pmgmode

		bcc		skip_pmenable
		lda		#3
		sta		gractl
		ora		gprior
		and		#$c1
		sta		gprior
skip_pmenable:

		lda		sdmctl
		and		#$e3
		ora		pmg_dmactl_tab,x
		sta		sdmctl

		;reset sprite positions, sizes, and graphics latches
		ldy		#17
		lda		#0
		sta:rpl	hposp0,y-

no_pmg:
		rts

mask_tab:
		dta		$f8,$fc
.endp

;===========================================================================
.proc stPmclr
		jsr		ExprEvalPopIntPos
.def :pmClear
		jsr		pmGetAddrX
clear2:
		ldy		pmgmode
		lda		#0
clloop:
		dey
		sta		(parptr),y
		bne		clloop
done:
		rts
.endp

;==========================================================================
; PMMOVE aexp[,hpos][;vdel]
;
; Moves a sprite horizontally or vertically.
;
; Only bits 0-6 or bits 0-7 of |vdel| are used.
;
.proc stPmmove
		jsr		ExprEvalPopIntPos
		cpx		#8
		bcs		stPmclr.done
		stx		stScratch2
		lda		pmg_move_mask_tab,x
		sta		stScratch4
		jsr		pmGetAddrX
		sta		iterPtr+1
op_loop:
		jsr		ExecGetComma
		bne		not_hmove

		;read absolute horizontal pos
		jsr		ExprEvalPopIntPos
		txa
		ldx		stScratch2
		sta		hposp0,x
		bpl		op_loop				;!! - unconditional

not_hmove:
		cmp		#TOK_EXP_SEMI
		bne		stPmclr.done

		;read relative vertical pos
		jsr		evaluate
		jsr		MathSplitSign
		jsr		ExprConvFR0Int
		txa
		ora		pmgmode
		eor		pmgmode
		beq		stPmclr.done
		sta		stScratch3
		ora		parptr
		sta		iterPtr

		;test vsign -- + is up (ascending copy), - is down (descending copy)
		lda		pmgmode
		sec
		sbc		stScratch3

		bit		funScratch1
		bpl		move_up

move_down:
		tay
down_loop:
		dey
		lda		(parptr),y
		eor		(iterPtr),y
		and		stScratch4
		eor		(parptr),y
		sta		(iterPtr),y
		tya
		bne		down_loop
down_loop_exit:
clear:
		ldy		stScratch3
down_clear_loop:
		dey
		lda		(parptr),y
		and		stScratch4
		sta		(parptr),y
		tya
		bne		down_clear_loop
		rts

move_up:
		ldy		#0
		tax
up_loop:
		lda		(iterPtr),y
		eor		(parptr),y
		and		stScratch4
		eor		(iterPtr),y
		sta		(parptr),y
		iny
		dex
		bne		up_loop
up_loop_exit:
up_clear_loop:
		lda		(parptr),y
		and		stScratch4
		sta		(parptr),y
		iny
		cpy		pmgmode
		bne		up_clear_loop
		rts
.endp

;==========================================================================
.proc stMissile
		jsr		ExprEvalPopIntPos
		txa
		and		#3
		tax
		lda		pmg_move_mask_tab+4,x
		eor		#$ff
		sta		stScratch
		jsr		ExprSkipCommaAndEvalPopIntPos
		stx		stScratch2

		ldx		#4
		jsr		pmGetAddrX

		jsr		ExprSkipCommaAndEvalPopIntPos
		txa
		beq		done

		ldy		stScratch2
xor_loop:
		lda		(parptr),y
		eor		stScratch
		sta		(parptr),y
		iny
		dex
		bne		xor_loop
done:
		rts
.endp

;==========================================================================
.proc pmGetAddrX
		ldy		#0
		sty		parptr
		lda		#3
		cpx		#4
		bcs		is_missile
		txa
		ora		#4
is_missile:
		bit		pmgmode
		bpl		full_res
		lsr
		ror		parptr
full_res:
		ora		pmgbase
		cmp		#8
		bcc		err_pm
		sta		parptr+1
		rts
err_pm:
		ldy		#30
		jmp		IoThrowErrorY
.endp

;===========================================================================
.echo "- Statement module length: ",*-?statements_start
