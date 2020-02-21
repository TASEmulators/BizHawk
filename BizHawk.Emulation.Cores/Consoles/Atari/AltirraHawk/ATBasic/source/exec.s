; Altirra BASIC - Execution control module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;===========================================================================
.proc exec
		;if we're running in direct mode, stmcur is already set and we
		;should bypass changing that and checking for line 32768.		
		mwa		stmtab stmcur
		
		;reset DATA pointer
		jsr		execRestore

		;skip line number, but stash statement length
new_line:

		;##TRACE "Processing line: $%04X (line=%d)" dw(stmcur) dw(dw(stmcur))
		;##ASSERT dw(stmcur) != 32768
		;##ASSERT dw(stmcur)>=dw(stmtab) and dw(stmcur)<dw(starp)

		;check line number and make sure we're not executing the immediate
		;mode line
		ldy		#1
		lda		(stmcur),y
		bpl		direct_bypass

		;hitting the end of immediate mode does an implicit END
		jmp		stEnd

;Entry point for direct execution from immediate mode.
.def :execDirect
direct_bypass:
		;set EOL cache and start at beginning of line
		jsr		stNext.restart_line
loop:
		;check if break has been pressed
		lda		brkkey
		bpl		execStop

		;check if we are at EOL
		cpy		exLineEnd
		bcs		next_line

		;stash statement offset
		lda		(stmcur),y
		sta		exLineOffsetNxt
		iny

		;get statement token
		lda		(stmcur),y
		;##TRACE "Processing statement: $%04X+$%04x -> %y (line=%d)" dw(stmcur) y db(statementJumpTableLo+a)+db(statementJumpTableHi+a)*256+1 dw(dw(stmcur))
		iny
		sty		exLineOffset
		
		tax
		jsr		dispatch
		
		;skip continuation or EOL token
		ldy		exLineOffsetNxt
		bne		loop

.def :stRem = *
		;remove return address
		pla
		pla

next_line:
		;check if we're at the immediate mode line already
		ldy		#1
		lda		(stmcur),y
		bmi		hit_end

		;refetch EOL (saves a few bytes with CONT)
		iny
		lda		(stmcur),y

		;bump statement pointer
		ldx		#stmcur
		jsr		VarAdvancePtrX
		jmp		new_line

hit_end:
		jmp		execLoop
		
dispatch:
		;push statement address onto stack
		lda		statementJumpTableHi,x
		pha
		lda		statementJumpTableLo,x
		pha
		
next_statement:
		;execute the statement
		rts
.endp

;===========================================================================
.proc execStop
		mvy		#$80 brkkey

		dta		{bit $0100}
.def :ExecStopInvStructure = *
		ldy		#28
		jmp		IoThrowErrorY
.endp

;===========================================================================
; IF aexp THEN {lineno | statement [:statement...]}
;
; Evaluates aexp and checks whether it is non-zero. If so, the THEN clause
; is executed. Otherwise, execution proceeds at the next line (NOT the next
; statement). aexp is evaluated in float context so it may be negative or
; >65536.
;
; The token setup for the THEN clause is a bit wonky. If lineno is present,
; it shows up as an expression immediately after the THEN token, basically
; treating the THEN token as an operator. Otherwise, the statement abruptly
; ends at the THEN with no end of statement token and a new statement
; follows.
;
.proc stIf
		;evaluate condition
		jsr		evaluate

		;check for THEN token
		ldy		exLineOffset
		lda		(stmcur),y
		cmp		#TOK_EXP_THEN
		bne		block_if
		
		;check if condition is zero and skip the line if so
		lda		fr0
		;;##TRACE "If condition: %g" fr0
		beq		stRem

		;skip the THEN token, which is always present
		iny
		
		;check if this is the end of the statement
		cpy		exLineOffsetNxt
		beq		exec.next_statement		;statement follows, so execute it
		
		;no, it isn't... process the implicit GOTO.
		ldx		#$80-6
copy_loop:
		iny
		lda		(stmcur),y
		sta		fr0+6-$80,x
		inx
		bpl		copy_loop
		jsr		ExprConvFR0IntPos
		jmp		stGoto.gotoFR0Int

block_if:
		;For a block IF, a successful condition merely resumes execution.
		lda		fr0
		bne		exec.next_statement

		;Now we need to skip until the next ELSE or ENDIF. Tricky part is
		;that we also need to handle nested block AND non-block IFs, and
		;also reuse this code for ELSE. The logic is as follows:
		;
		;	if statement is IF:
		;		skip expression (must not evaluate)
		;		if token() is THEN:
		;			skip remainder of line
		;		else:
		;			++nesting_count
		;	else if statement is ELSE:
		;		if nesting_count == 0:
		;			if original statement is ELSE:
		;				error
		;			else
		;				break
		;	else if statement is ENDIF:
		;		if nesting_count == 0:
		;			break
		;		--nesting_count
		;	else if end of program:
		;		error

		sec
		dta		{bit $00}
.def :stElse = *
		clc
		ror		stScratch

.def :ExecScanBlockIf = *
		mva		#$ff stScratch2			;nesting count = -1
next_statement_incnest:
		inc		stScratch2
next_statement:
		ldy		exLineOffsetNxt
scan_loop:
		;check if we're at EOL
		cpy		exLineEnd
		bne		not_eol

		;check for immediate mode on the *current* line
		tya
		pha
		ldy		#1
		lda		(stmcur),y
		bmi		ExecStopInvStructure
		pla

		;bump statement pointer to next line
		ldx		#stmcur
		jsr		VarAdvancePtrX

		;check for immediate mode on the *next* line
		lda		(stmcur),y
		bmi		ExecStopInvStructure

		;fetch end
		iny
		lda		(stmcur),y
		sta		exLineEnd

		;begin processing statements
		iny
		bne		scan_loop

not_eol:
		lda		(stmcur),y
		sta		exLineOffsetNxt
		iny

		;get statement token
		lda		(stmcur),y

		;check for ENDIF
		cmp		#TOK_ENDIF
		bne		not_endif

		;decrement nesting count and continue execution on underflow
		dec		stScratch2
		bpl		next_statement
		rts

not_endif:
		;check for ELSE
		cmp		#TOK_ELSE
		bne		not_else

		;check if nesting count is non-zero, in which case ELSE is
		;ignored
		lda		stScratch2
		bne		next_statement

		;continue execution if doing IF; fail with error if ELSE
		bit		stScratch
		bpl		ExecStopInvStructure
		rts

not_else:
		;check for IF
		cmp		#TOK_IF
		bne		next_statement

		;Okay, we have an IF statement... this is the annoying part. We
		;need to check for the THEN token to see if this is a block IF or
		;not since only block IFs increase the nesting count. This means
		;that we need to parse past the expression.
		;
		;Note that there is an ambiguity in Basic XE regarding non-block
		;IF constructs nested within block IFs:
		;
		; 10 IF X=0
		; 20 IF Z=1 THEN PRINT "FOO" : ELSE
		; 30 PRINT "BAR"
		; 40 ENDIF
		;
		;This prints BAR because the block IF prevents the execution
		;engine from seeing the ELSE. However, if the condition is changed
		;to X=1, it still prints BAR, because the scanner sees the ELSE.
		;This means that the scanner should continue with the next
		;statement after an IF regardless of whether it's a block IF or not.
		;
		;The logic for the expression scanning loop:
		;
		;	token = stmcur[pos++]
		;	if token is THEN:
		;		break
		;	else if token is EOS or EOL:
		;		++nesting_count
		;		break
		;	else if token is CSTR:
		;		pos += 1 + stmcur[pos]
		;	else if token is CNUM or CHEX:
		;		pos += 7

exp_loop:
		;bump pointer and check for end of statement
		iny
		cpy		exLineOffsetNxt
		beq		next_statement_incnest

		;get expression token
		lda		(stmcur),y

		;check for THEN
		cmp		#TOK_EXP_THEN
		beq		next_statement

		;check for constant string or number
		cmp		#TOK_EXP_CSTR
		bcc		is_cnum
		bne		exp_loop			;nope, single byte token (note: may be EOL/EOS)

		;literal string
		iny
		lda		(stmcur),y			;get length
		clc
		dta		{bit $0100}			;skip LDA #6 below
is_cnum:
		lda		#6					;!! - skipped by cstr path
		sty		exLineOffset
		adc		exLineOffset
		tay
		bne		exp_loop			;!! - unconditional
.endp

;===========================================================================
; ENDIF
;
; The ENDIF statement is a no-op when encountered in regular execution.
;
stEndIf = exec.next_statement

;===========================================================================
.macro STATEMENT_JUMP_TABLE
		;$00
		dta		:1[stRem-1]
		dta		:1[stData-1]
		dta		:1[stInput-1]
		dta		:1[stColor-1]
		dta		:1[stList-1]
		dta		:1[stEnter-1]
		dta		:1[stLet-1]
		dta		:1[stIf-1]
		dta		:1[stFor-1]
		dta		:1[stNext-1]
		dta		:1[stGoto-1]
		dta		:1[stGoto2-1]
		dta		:1[stGosub-1]
		dta		:1[stTrap-1]
		dta		:1[stBye-1]
		dta		:1[stCont-1]
		
		;$10
		dta		:1[stCom-1]
		dta		:1[stClose-1]
		dta		:1[stClr-1]
		dta		:1[stDeg-1]
		dta		:1[stDim-1]
		dta		:1[stEnd-1]
		dta		:1[stNew-1]
		dta		:1[stOpen-1]
		dta		:1[stLoad-1]
		dta		:1[stSave-1]
		dta		:1[stStatus-1]
		dta		:1[stNote-1]
		dta		:1[stPoint-1]
		dta		:1[stXio-1]
		dta		:1[stOn-1]
		dta		:1[stPoke-1]
		dta		:1[stPrint-1]
		dta		:1[stRad-1]
		dta		:1[stRead-1]
		dta		:1[stRestore-1]
		dta		:1[stReturn-1]
		dta		:1[stRun-1]
		dta		:1[stStop-1]
		dta		:1[stPop-1]
		dta		:1[stQuestionMark-1]
		dta		:1[stGet-1]
		dta		:1[stPut-1]
		dta		:1[stGraphics-1]
		dta		:1[stPlot-1]
		dta		:1[stPosition-1]
		dta		:1[stDos-1]
		dta		:1[stDrawto-1]
		dta		:1[stSetcolor-1]
		dta		:1[stLocate-1]
		dta		:1[stSound-1]
		dta		:1[stLprint-1]
		dta		:1[stCsave-1]
		dta		:1[stCload-1]
		dta		:1[stImpliedLet-1]
		dta		:1[stSyntaxError-1]
		dta		:1[errorWTF-1]			;BASIC XL/XE: WHILE
		dta		:1[errorWTF-1]			;BASIC XL/XE: ENDWHILE
		dta		:1[errorWTF-1]			;BASIC XL/XE: TRACEOFF
		dta		:1[errorWTF-1]			;BASIC XL/XE: TRACE
		dta		:1[stElse-1]
		dta		:1[stEndIf-1]
		dta		:1[stDpoke-1]
		dta		:1[stLomem-1]			;BASIC XL/XE: LOMEM

		;$40
		dta		:1[errorWTF-1]			;BASIC XL/XE: DEL
		dta		:1[errorWTF-1]			;BASIC XL/XE: RPUT
		dta		:1[errorWTF-1]			;BASIC XL/XE: RGET
		dta		:1[stBput-1]
		dta		:1[stBget-1]
		dta		:1[errorWTF-1]			;BASIC XL/XE: TAB
		dta		:1[stCp-1]
		dta		:1[stFileOp-1]
		dta		:1[stFileOp-1]
		dta		:1[stFileOp-1]
		dta		:1[stDir-1]
		dta		:1[stFileOp-1]
		dta		:1[stMove-1]
		dta		:1[stMissile-1]
		dta		:1[stPmclr-1]
		dta		:1[stPmcolor-1]

		;$50
		dta		:1[stPmgraphics-1]
		dta		:1[stPmmove-1]
.endm

statementJumpTableLo:
		STATEMENT_JUMP_TABLE	<

statementJumpTableHi:
		STATEMENT_JUMP_TABLE	>

;===========================================================================
; Input:
;	A:X		Line number (integer)
;
; Output:
;	Y:A		Line address
;	P.C		Set if found, unset otherwise
;
; If the line is not found, the insertion point for the line is returned
; instead.
;
.proc exFindLineInt
_lptr = iterPtr
		;search for line
		stx		fr0
		sta		fr0+1
		
		;check if line is >= current line -- if so start there
		txa
		ldx		#0
		cmp		(stmcur,x)
		ldy		#1
		lda		fr0+1
		sbc		(stmcur),y
		bcs		use_current
		ldx		#<[stmtab-stmcur]
use_current
		
		;load pointer
		lda.b	stmcur+1,x
		sta		_lptr+1
		lda.b	stmcur,x

		ldx		#0
search_loop:
		sta		_lptr
		lda		(_lptr),y		;load current lineno hi
		cmp		fr0+1			;subtract desired lineno hi
		bcc		next			;cur_hi < desired_hi => next line
		bne		not_found		;cur_hi > desired_hi => not found
		lda		(_lptr,x)
		cmp		fr0
		bcs		at_or_past
next:
		iny
		lda		_lptr
		adc		(_lptr),y
		dey
		bcc		search_loop
		inc		_lptr+1
		bne		search_loop		;!! - unconditional jump
		
at_or_past:
		beq		found
not_found:
		clc
found:
		lda		_lptr
		ldy		_lptr+1
.def :ExNop
		rts
.endp

;===========================================================================
; Check if the current token is end of statement/line.
;
; Output:
;	Y = current line offset
;	A = current token
;	P.Z = set if end of statement/line
;
; Preserved:
;	X
;
.proc ExecTestEnd
		ldy		exLineOffset
		lda		(stmcur),y
		cmp		#TOK_EOS
		beq		is_end
		cmp		#TOK_EOL
is_end:
		rts
.endp

;===========================================================================
.proc ExecGetComma
		ldy		exLineOffset
		inc		exLineOffset
		lda		(stmcur),y
		cmp		#TOK_EXP_COMMA
		rts
.endp

;===========================================================================
.proc _ExecFixStack
.def :ExecCheckStack
		clc
		adc		memtop2
		tax
		lda		#0
		adc		memtop2+1
		cpx		memtop
		sbc		memtop+1
		bcc		chkstk_ok
		jmp		errorNoMemory
chkstk_ok:
		;Now we have to fix the stack in case it is floating, since
		;we can't mix fixed and floated data on the stack.
.def :ExecFixStack
		bit		exFloatStk
		bpl		xit
		lsr		exFloatStk

		lda		memtop2
		pha
		lda		memtop2+1
		pha
		bne		loop_start
loop:
		lda		#$fc
		jsr		stPop.dec_ptr_2

		;fetch the line number
		ldy		#1
		lda		(memtop2),y
		tax
		iny
		lda		(memtop2),y

		;do line lookup
		jsr		exFindLineInt
		bcc		not_found
		ldx		iterPtr+1

		jsr		stFor.push_ax_1

		;advance pointer
		jsr		stPop.pop_frame_remainder

loop_start:
		;check if we're at the bottom of the stack
		jsr		stReturn.check_rtstack_empty
		bcc		loop

done:
		pla
		sta		memtop2+1
		pla
		sta		memtop2
xit:
		rts

not_found:
		jmp		errorGOSUBFORGone
.endp

;===========================================================================
.proc ExecFloatStack
		bit		exFloatStk
		bmi		_ExecFixStack.xit
		sec
		ror		exFloatStk

		lda		memtop2
		pha
		lda		memtop2+1
		pha
loop:
		;check if we're at the bottom of the stack
		jsr		stReturn.check_rtstack_empty
		bcs		_ExecFixStack.done

		lda		#$fc
		jsr		stPop.dec_ptr_2

		;fetch the line address
		ldy		#2
		lda		(memtop2),y
		sta		fr0+1
		dey
		lda		(memtop2),y
		sta		fr0

		;fetch the line number
		lda		(fr0),y
		tax
		dey
		lda		(fr0),y

		jsr		stFor.push_ax_1

		;advance pointer
		jsr		stPop.pop_frame_remainder
		jmp		loop
.endp
