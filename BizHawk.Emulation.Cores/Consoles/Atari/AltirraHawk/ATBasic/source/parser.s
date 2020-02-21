; Altirra BASIC - Parser module
; Copyright (C) 2014-2016 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;===========================================================================
; Parser module
;===========================================================================
;
; The parser is responsible for accepting a line of input and converting it
; to a tokenized line, either deferred (line 0-32767) or immediate (line
; 32768).
;
; Execution phases and error handling
; -----------------------------------
; Oddly enough, the parser may be invoked by program execution by means of
; the ENTER statement being used in deferred mode. However, execution stops
; after this happens. In Atari BASIC, CONT can be used to resume execution.
;
; One corner case that must be dealt with is that it is possible for the
; parser to run out of memory trying to expand the variable or statement
; tables, raising Error 2. This can in turn invoke TRAP! This means that
; STMCUR must be on a valid line for the optimized line lookup to succeed,
; although any line will do.
;
; Entering a line number between 32768 and 65535 produces a syntax error.
; However, entering a line number that fails FPI (not in 0-65535) causes
; error 3. This can also invoke TRAP.
;
; If a syntax error occurs during parsing, any variables added during
; parsing are rolled back. If an error occurs during table expansion, added
; variables are NOT rolled back.
;
; Memory usage
; ------------
; The argument stack area (LOMEM to LOMEM+$ff) is used as the temporary
; tokenization buffer and the 6502 stack is used to handle backtracking.
; Unlike Atari BASIC, the region from $0480-$057F is not used by the parser.
;
; Parser stack
; ------------
; The 6502 stack is used for the parser stack. There are three frames that
; can be encountered:
;
; Bottom of stack      Backtrack       Call
;                      PARPTR          PARPTR
;                      PARPTR+1        PARPTR+1
;                      PAROUT          $FF
; $00                  CIX             $FF
;
;===========================================================================

?parser_start = *

;============================================================================
.nowarn .proc _paFinishLine

not_found:						;C = 0
		;check if we are trying to delete a line that doesn't exist (which is ok)
		lda		parout
		beq		done

do_expand:		
		;move statements and tables up in memory
		ldy		#0
		ldx		#starp
		jsr		expandTable

same_length:

		;copy line from temporary memory into statement table
		ldy		parout
		beq		done
copyloop:
		dey
		lda		(argstk),y
		sta		(stmcur),y
		tya
		bne		copyloop

done:
		;exit C=0 for delete/insert/replace, C=1 for immediate
		asl		stScratch3		;line number bit 15 -> C
done2:
		rts
		
.def :paCmdEOL
		;remove backtracking sentinel
		pla

		;mark statement length
		ldy		#0
		lda		parout
		cmp		#4
		bcs		not_empty
		
		;hmm, empty -- we're deleting, then.
		sty		parout

		;check if we are trying to delete the immediate line -- just
		;exit C=0 if so.
		lda		stScratch3
		bmi		done2
		
not_empty:
		;retrieve line number
		lda		(argstk),y
		tax
		lda		stScratch3

		;determine where this should fit in statement table
		jsr		exFindLineInt

		;set insertion/deletion address for table adjustment
		sta		fr0
		sty		fr0+1
		
		;save off address
		sta		stmcur
		sty		stmcur+1
		
		;store statement length
		ldy		#2
		lda		parout
		sta		(argstk),y

		;check whether we should expand or contract the statement table
		bcc		not_found

		;compute difference from existing length
		sbc		(fr0),y			;!! - carry is set from bcc above
		beq		same_length
		bcs		do_expand

do_contract:
		;move statements and tables down in memory
		;compute source position (a1 = a0-neg_len)
		sta		a2
		lda		#$ff
		sta		a2+1
		sec
		eor		a2
		adc		a0
		sta		a1
		lda		a0+1
		adc		#0
		sta		a1+1
		
		;compute bytes to copy (a3 = memtop2-a1)
		sec
		lda		memtop2
		sbc		a1
		sta		a3
		lda		memtop2+1
		sbc		a1+1
		sta		a3+1

		jsr		copyAscending

		ldx		#starp
		jsr		MemAdjustTablePtrs
		jmp		same_length
.endp

.nowarn .proc _paCmdCopyLine
copy_loop:
		inc		cix
.def :paCmdCopyLine = *
		ldy		cix
		lda		(inbuff),y
		jsr		paCmdEmit.doEmitByte
		cmp		#$9b
		bne		copy_loop

.def :paCmdStEnd
		;backpatch the statement skip byte
		lda		parout
		ldy		parStBegin
		sta		(argstk),y
		bne		_parseLine.parse_loop			;!! - unconditional
.endp

;============================================================================
;
; Exit:
;	C = 0 if deferred edit
;	C = 1 if immediate line
;
.nowarn .proc _parseLine		
		;begin parsing loop
parse_loop_inc:
		jsr		paFetch
		dta		{bit $00}
push_then_restart_parse_loop:
		pha
parse_loop:
		jsr		paFetch
		;##TRACE "Parse: Executing opcode [$%04x]=$%02x [%y -> %y] @ offset %d (%c); stack(%x)=%x %x %x" dw(parptr) (a) dw(parptr) db(parse_dispatch_table_lo+db(dw(parptr)))+256*db(parse_dispatch_table_hi+db(dw(parptr)))+1 db(cix) db(dw(inbuff)+db(cix)) s db($101+s) db($102+s) db($103+s)
		bmi		is_state
		
		;check if it is a command
		cmp		#' '
		bcc		is_command
		
		;it's a literal char -- check it against the next char
		bne		not_space
		jsr		skpspc
		jmp		parse_loop
		
not_space:
		ldy		cix
		cmp		(inbuff),y
		bne		parseFail
		inc		cix
		bne		parse_loop			;!! - unconditional

is_command:
		tax
		lda		parse_dispatch_table_hi,x
		pha
		lda		parse_dispatch_table_lo,x
		pha
		rts

.def :parseLine
		;save VNTD/VVTP/STMTAB
		ldx		#5
		mva:rpl	vntd,x parPtrSav,x-

		;clear last statement marker and first variable added
		lda		#0
		sta		parStBegin

		;push backtrack sentinel onto stack
		pha

		;check if we've got a line number
		jsr		afp
		bcc		lineno_valid
		
		;use line 32768 for immediate statements (A:X)
		lda		#$80
		ldx		#$00
		
		;restart at beginning of line
		stx		cix
		beq		lineno_none
		
lineno_valid:
		;convert line to integer and throw error 3 if it fails
		;(yes, you can invoke TRAP this way!)
		jsr		ExprConvFR0Int
		
		;A:X = line number
		;check for a line >=32768, which is illegal
		bmi		paCmdFail

lineno_none:
		;stash the line number and a dummy byte as the line length
		ldy		#1
		sta		(argstk),y
		sta		stScratch3			;stash this later for C return
		dey
		txa
		sta		(argstk),y
		ldy		#3
		sty		parout
		
		;begin parsing at state 0
		lda		#$80

is_state:
		;extract the call bit and check if we have a call
		lsr
		tax
		bcc		is_state_jump
		
		;it's a call -- push frame on stack
		lda		parptr
		pha
		lda		parptr+1
		pha
		lda		#$ff
		pha
		pha
		
is_state_jump:
		;jump to new state
		lda		parse_state_table-$40,x
		ldy		#>pa_state_start
load_and_jmp:
		sta		parptr
		sty		parptr+1
		bcs		parse_loop

		;clear any backtracking entries from the stack
btc_loop:
		pla
		beq		push_then_restart_parse_loop
		pla
		pla
		pla
		bcc		btc_loop		;!! - C=0 from BCS above
.endp

;============================================================================
parseFail = paCmdFail.entry
.proc paCmdFail
entry:
		;see if we can backtrack
pop_loop:
		pla
		beq		paEpicFail
		
		cmp		#$ff
		bne		not_jsr
		
		;pop off the jsr frame and keep going
		pla
		pla
		pla
		bcs		pop_loop		;!! - unconditional
		
not_jsr:
		;backtrack frame - restore state and parser IP and try again
		sta		cix
		pla
		;##TRACE "Parser: Backtracking to IP $%04x, pos $%02x" db($101+s)*256+db($102+s) db(cix)
		sta		parout

		dta		{bit $0100}		;bit $6868

.def :paCmdRts
		;remove backtracking indicator
		pla
		
		;remove dummy output val
		pla

pop_ip:
		pla
		sta		parptr+1
		pla
		sta		parptr
		jmp		_parseLine.parse_loop
.endp

;============================================================================
.proc paCmdHex
		;zero FR0 (although really only need 16-bit)
		jsr		zfr0

		;set empty flag
		ldx		#1
digit_loop:
		;try to parse a digit
		jsr		isdigt
		bcc		digit_ok
		and		#$df
		cmp		#$11
		bcc		parse_end
		cmp		#$17
		bcs		parse_end
		sbc		#7-1

digit_ok:
		inc		cix

		;shl4
		ldx		#4
shl4_loop:
		asl		fr0
		rol		fr0+1
		dex					;!! - also clears empty flag
		bne		shl4_loop

		;merge in new digit
		ora		fr0
		sta		fr0

		;loop back if we're not full
		lda		fr0+1
		cmp		#$10
		bcc		digit_loop

parse_end:
		;check if we actually got anything
		txa
		bne		paCmdFail

		;convert to FP
		jsr		ifp

		;emit and then branch
		lda		#TOK_EXP_CHEX
		bne		paCmdTryNumber.emit_number	;!! - unconditional
.endp

;============================================================================
.proc paCmdTryNumber
		;try to parse
		lda		cix
		pha
		jsr		MathParseFP
		pla
		bcc		succeeded
		sta		cix
		jmp		_parseLine.parse_loop_inc
succeeded:

		;emit a constant number token
		lda		#TOK_EXP_CNUM
emit_number:
		;emit the number
		ldx		#$80-7
copyloop:
		jsr		paCmdEmit.doEmitByte
		lda		fr0+7-$80,x
		inx
		bpl		copyloop
		
		;all done
		jmp		paCmdBranch
.endp

;============================================================================
.proc paEpicFail
		;##TRACE "Parser: No backtrack -- failing."
		lda		#0
		sta		parout
		ldx		#msg_error2-msg_base
		jsr		IoPrintMessageIOCB0		;!! - overwrites INBUFF
		
		inc		cix
print_loop:
		ldx		parout
		inc		parout
		lda		lbuff,x
		pha
		dec		cix
		bne		no_invert
		eor		#$80
		cmp		#$1b
		bne		not_eol
		lda		#$a0
		jsr		putchar
		lda		#$9b
no_invert:
not_eol:
		jsr		putchar
		pla
		cmp		#$9b
		bne		print_loop

		;undo changes to the VNT and VVT
		ldx		#vntd
		jsr		adjust_table
		ldx		#stmtab
		jsr		adjust_table

		;We use loop2 here because an syntax error does not interrupt
		;ENTER.
		jmp		execLoop.loop2

adjust_table:
		txa
		pha

		;copy length = MEMTOP2 - source
		lda		memtop2
		sec
		sbc		0,x
		sta		a3
		lda		memtop2+1
		sbc		1,x
		sta		a3+1

		;dest = prev VNTD/STMTAB
		;adjustment = dest - source (always negative)
		lda		parPtrSav-vntd,x
		sta		a0
		sbc		0,x
		sta		a2
		lda		parPtrSav+1-vntd,x
		sta		a0+1
		sbc		1,x
		sta		a2+1
		
		;source = current VNTD/STMTAB
		ldy		0,x
		lda		1,x

		;shift tables down and exit
		jsr		copyAscendingSrcAY

		pla
		tax
		jmp		MemAdjustTablePtrs
.endp

;============================================================================
parse_dispatch_table_lo:
		dta		<[paCmdFail-1]			;$00
		dta		<[paCmdAccept-1]		;$01
		dta		<[paCmdTryStatement-1]	;$02
		dta		<[paCmdOr-1]			;$03
		dta		<[paCmdEOL-1]			;$04
		dta		<[paCmdBranch-1]		;$05
		dta		<[paCmdBranchChar-1]	;$06
		dta		<[paCmdEmit-1]			;$07
		dta		<[paCmdCopyLine-1]		;$08
		dta		<[paCmdRts-1]			;$09
		dta		<[paCmdTryNumber-1]		;$0A
		dta		<[paCmdTryVariable-1]	;$0B
		dta		<[paCmdTryFunction-1]	;$0C
		dta		<[paCmdHex-1]			;$0D
		dta		<[paCmdStEnd-1]			;$0E
		dta		<[paCmdString-1]		;$0F
		dta		<[paCmdBranchStr-1]		;$10
		dta		<[paCmdNum-1]			;$11
		dta		<[paCmdStr-1]			;$12
		dta		<[paCmdEmitBranch-1]	;$13
		dta		<[paCmdTryArrayVar-1]	;$14
		dta		<[paCmdBranchEOS-1]		;$15
		dta		<[paCmdEndif-1]			;$16

parse_dispatch_table_hi:
		dta		>[paCmdFail-1]			;$00
		dta		>[paCmdAccept-1]		;$01
		dta		>[paCmdTryStatement-1]	;$02
		dta		>[paCmdOr-1]			;$03
		dta		>[paCmdEOL-1]			;$04
		dta		>[paCmdBranch-1]		;$05
		dta		>[paCmdBranchChar-1]	;$06
		dta		>[paCmdEmit-1]			;$07
		dta		>[paCmdCopyLine-1]		;$08
		dta		>[paCmdRts-1]			;$09
		dta		>[paCmdTryNumber-1]		;$0A
		dta		>[paCmdTryVariable-1]	;$0B
		dta		>[paCmdTryFunction-1]	;$0C
		dta		>[paCmdHex-1]			;$0D
		dta		>[paCmdStEnd-1]			;$0E
		dta		>[paCmdString-1]		;$0F
		dta		>[paCmdBranchStr-1]		;$10
		dta		>[paCmdNum-1]			;$11
		dta		>[paCmdStr-1]			;$12
		dta		>[paCmdEmitBranch-1]	;$13
		dta		>[paCmdTryArrayVar-1]	;$14
		dta		>[paCmdBranchEOS-1]		;$15
		dta		>[paCmdEndif-1]			;$16
		
;============================================================================
.proc paFetch
		inw		parptr
		ldy		#0
		lda		(parptr),y
		rts
.endp

;============================================================================
.proc paApplyBranch
		;get branch offset
		jsr		paFetch

		;decrement by a page if the original displacement was negative
		spl:dec	parptr+1
		
		;apply unsigned offset
		ldx		#parptr
		jmp		VarAdvancePtrX
.endp

;============================================================================
paCmdEndif:
		ldy		parout
		dey
		mva		#TOK_ENDIF (argstk),y
paCmdAccept:
		;remove backtracking entry
		;##TRACE "Parser: Accepting (removing backtracking entry)."
		pla
		pla
		pla
		pla
		jmp		_parseLine.parse_loop

;============================================================================
.proc paCmdTryFunction
		;scan the function table
		ldx		#<funtok_name_table
		lda		#>funtok_name_table
		ldy		#$3d
		bne		paCmdTryStatement.search_table
.endp

;============================================================================
.proc paCmdTryStatement
		;reserve byte for statement length and record its location
		jsr		paCmdEmit.doEmitByte
		sty		parStBegin
		
		;scan the statement table
		ldx		#<statement_table
		lda		#>statement_table
		ldy		#0

;-------------------
; Entry:
;	A:X = search table
;	Y = token base (0 for statements, $3D for functions)
;
search_table:
_stateIdx = fr0
_functionMode = fr0+1

		sty		_functionMode
		sty		_stateIdx
		stx		iterPtr
		sta		iterPtr+1

		;check if first character is a letter, a qmark, or a period
		ldy		cix
		lda		(inbuff),y
		cmp		#'.'
		bne		not_period
		lda		_functionMode
		beq		first_ok
not_period:

		;Check if we have a question mark or a letter. This will let @ through
		;too, but that's fine as it won't match anything, and it's not valid
		;in any other context either.
		sub		#'?'
		cmp		#26+2
		bcs		fail_try

first_ok:
		;okay, it's a letter... let's go down the statement table.
table_loop:
		;begin scan
		ldy		#0
		ldx		cix
statement_loop:
		lda		(iterPtr),y
		beq		fail_try				;exit if we hit the end of the table
		and		#$7f
		inx
		cmp		lbuff-1,x
		bne		fail_stcheck

		;check if this was the last char
		lda		(iterPtr),y
		asl
		
		;progress to next char
		iny
		bcc		statement_loop
		
check_term:

		;Term check
		;
		;For statements, a partial match is accepted:
		;
		;	PRINTI -> PRINT I
		;
		;However, this is not true for functions. A function name will not match
		;if there is an alphanumeric character after it. Examples:
		;
		;	PRINT SIN(0) -> OK, parsed as function call
		;	PRINT SIN0(0) -> OK, parsed as array reference
		;	PRINT SINE(0) -> OK, parsed as array reference
		;	PRINT SIN$(0) -> Parse error at $
		;	PRINT STR(0) -> OK, parsed as array reference

		ldy		_functionMode
		beq		accept
		cmp		#'('*2
		beq		accept

		lda		lbuff,x
		jsr		paIsalnum
		bcc		fail

accept:
		;looks like we've got a hit -- update input pointer, emit token, and change the state.
		stx		cix
		
		lda		_stateIdx
		jsr		paCmdEmit.doEmitByte
		tax

		;init for statements
		lda		parse_state_table_statements,x
		ldy		#>pa_statements_begin

		;check if we're doing functions
		lsr		_functionMode
		bcc		do_branch

		;init for functions

		stx		stScratch
		jsr		paApplyBranch
				
		;push frame on stack
		lda		parptr
		pha
		lda		parptr+1
		pha
		lda		parout
		pha
		lda		#0
		pha
		
		ldx		stScratch
		lda		parse_state_table_functions-$3d,x
		ldy		#>pa_functions_begin

do_branch:
		clc
		jmp		_parseLine.load_and_jmp

fail_stcheck:
		;check for a ., which is a trivial accept -- this is only allowed for
		;statements and not functions
		ldy		_functionMode
		bne		fail

		lda		lbuff-1,x
		cmp		#'.'
		beq		accept

fail:
		;skip chars until we're at the end of the entry
		jsr		VarAdvanceName
		
		;loop back for more
		inc		_stateIdx
		bne		table_loop
		
		;whoops
fail_try:
		lda		_functionMode
		beq		paCmdBranch.next
		bne		paCmdBranchStr.next_inc			;!! - unconditional
.endp

;============================================================================
.proc paCmdOr
		;push backtracking entry with offset onto stack
		;##TRACE "Parser: Pushing backtracking state IP=$%04x, pos=$%02x, out=$%02x" dw(parptr)+dsb(dw(parptr)+1)+1 db(cix) db(parout)
		jsr		paFetch
		clc
		adc		parptr
		pha
		lda		parptr+1
		adc		#0
		pha
		lda		parout
		pha
		lda		cix

		jmp		_parseLine.push_then_restart_parse_loop
.endp

;============================================================================
.proc paCmdBranchStr
		lda		parStrType
		bmi		paCmdBranch
next_inc:
		jmp		_parseLine.parse_loop_inc
.endp

;============================================================================
paCmdEmitBranch:
		jsr		paFetch
paCmdEmitByteBranch:
		jsr		paCmdEmit.doEmitByte
.proc paCmdBranch
		jsr		paApplyBranch
next:
		jmp		_parseLine.parse_loop
.endp

;============================================================================
.proc paCmdBranchChar
		;get character and check
		jsr		paFetch
		ldy		cix
		cmp		(inbuff),y
		beq		char_match

		;skip past branch offset and emit char and continue execution
		jsr		paFetch
		jmp		_parseLine.parse_loop_inc

char_match:
		;eat the char
		;##TRACE "Parser: Branching on char: %c" (a)
		inc		cix
		
		;check if we need to emit
		jsr		paFetch
		beq		paCmdBranch
		bne		paCmdEmitByteBranch			;!! - unconditional
.endp

;============================================================================
.proc paCmdBranchEOS
		;skip spaces
		jsr		skpspc

		;get character and check
		lda		(inbuff),y
		cmp		#':'
		beq		paCmdBranch
		cmp		#$9b
		beq		paCmdBranch

		;skip past branch offset and continue execution
		jmp		_parseLine.parse_loop_inc
.endp

;============================================================================
.proc paCmdEmit
		jsr		doEmit
		jmp		_parseLine.parse_loop

doEmit:
		;get token to emit
		jsr		paFetch
		
doEmitByte:
		;emit the token
		ldy		parout
		inc		parout
		beq		overflow
		sta		(argstk),y
		
		;all done
		rts

overflow:
		jmp		errorLineTooLong
.endp

;============================================================================
; Exit:
;	C = 0 if alphanumeric
;	C = 1 if not alphanumeric
;
; Preserved:
;	A, X, Y
;
.proc paIsalnum
		pha
		sec
		sbc		#'0'
		cmp		#10
		bcc		success
		sbc		#'A'-'0'
		cmp		#26
success:
		pla
		rts
.endp

;============================================================================
.proc paCmdTryArrayVar
_index = prefr0+1
_reqarray = a5+1
_nameLen = a4
_nameEnd = a3

.def :paCmdTryVariable

		cpx		#$14			;token for TryArrayVar
		ror		_reqarray

		;first non-space character must be a letter
		jsr		skpspc
		lda		(inbuff),y
		sub		#'A'
		cmp		#26
		bcs		reject

		;compute length of the name
		iny
		
namelen_loop:
		lda		(inbuff),y
		jsr		paIsalnum
		bcs		namelen_loop_end
		iny
		bne		namelen_loop
		
namelen_loop_end:
		;check if we have an array or string specifier
		tax
		lda		#0
		sta		_index
		cpx		#'$'
		beq		is_string_var
		cpx		#'('
		beq		is_array_var
		
		;not an array... reject if we needed one
		bit		_reqarray
		bpl		not_array
		
reject:
		jmp		_parseLine.parse_loop_inc

is_array_var:
		ror
is_string_var:
		ror

		;capture the extra char
		iny
		
not_array:
		;set expr type flag
		sta		parStrType

		;record the ending position
		sty		_nameEnd

		;search the variable name table
		mwa		vntp iterPtr
search_loop:
		;check characters one at a time for this entry
		ldy		#0
		ldx		cix
match_loop:
		inx
		lda		(iterPtr),y
		beq		create_new				;exit if we hit the sentinel (should only happen at beginning)
		bmi		match_last
		cmp		lbuff-1,x
		bne		no_match
		iny
		bne		match_loop
		
match_last:
		;reject if we're not on the last character of the varname in the input
		cpx		_nameEnd
		bne		no_match
		
		;reject if the last char doesn't match
		eor		#$80
		cmp		lbuff-1,x
		bne		no_match

		stx		cix
match_ok_2:
		;emit variable and branch
		;##TRACE "Taking variable branch"
		lda		_index
		ora		#$80
		jmp		paCmdEmitByteBranch
				
no_match:
		;skip remaining chars in this entry
		jsr		VarAdvanceName

		inc		_index
		bpl		search_loop

		;oops... too many variables!
		jmp		errorTooManyVars
		
create_new:
		;!! Y = 0 here -- need to maintain until expandTable!
		
		;set insertion point for expandTable below (and cache original STMTAB)
		lda		stmtab+1
		sta		a0+1
		pha
		lda		stmtab
		sta		a0
		pha

		;##TRACE "Creating new variable $%02x [%.*s]" db(paCmdTryVariable._index) db(paCmdTryVariable._nameLen) lbuff+db(cix)
		;bump input pointer to end of name and compute name length
		lda		_nameEnd
		tax
		sec
		sbc		cix					;!! - C=1 for ADC below
		stx		cix
		sta		_nameLen
		
		;OK, now we need to make room at the top of the VNT for the new name,
		;plus add another 8 chars at the top of the VVT. To save some time,
		;we only make room at the top of the VVT first, then we move just
		;the VVT. This optimizes the insert to only one move, and more
		;importantly, avoids the possibility of running out of memory midway
		;between two inserts (BAD).
		adc		#8-1				;!! - carry is set from above!
		ldx		#stmtab
		jsr		expandTable
		
		;now we need to move the VNT sentinel and VVT -- move [VNTD, old STMTAB)
		;to [-, new STMTAB - 8]
		;##TRACE "vntp=$%04x, vvtp=$%04x, stmtab=$%04x" dw(vntp) dw(vvtp) dw(stmtab)
		sec
		pla							;compute count = VVT size + 1
		sta		a1
		sbc		vntd
		sta		a3
		pla
		sta		a1+1				;A1 = source_end = original stmtab
		sbc		vntd+1				;!! - C=1 for below
		sta		a3+1

		;set dest_end = new stmtab - 8
		lda		stmtab
		sbc		#8
		tay
		lda		stmtab+1
		sbc		#0

		;relocate VNT sentinel and VVT
		jsr		copyDescendingDstAY
		
		;copy the name to the previous VNTD location, setting bit 7 on the last byte
		ldx		cix
		ldy		_nameLen
		lda		#$80		;invert last name byte (we're going backwards)
		dta		{bit $0100}
name_copy:
		lda		#0			;leave remaining bytes alone
		dex
		eor		lbuff,x
		dey
		sta		(vntd),y
		bne		name_copy

		;relocate VNTD and VVTP by +_nameLen
		ldx		#vvtp
reloc_loop:
		lda		_nameLen
		jsr		VarAdvancePtrX
		dex
		dex
		cpx		#vntd-2
		bne		reloc_loop
		
		;zero out remaining bytes and write to VVTP
		lda		_index
;		bit		parNewVar
;		bmi		not_first_new_var
;		sta		parNewVar
;not_first_new_var:
		jsr		VarGetAddr0
		jsr		zfr0
		tay
		jsr		VarStoreExtFR0_Y

		jmp		match_ok_2

.endp

;============================================================================
; String literal.
;
; Note that an unterminated (dangling) string literal is permitted. Floyd
; of the Jungle (1982) relies on this.
;
.proc paCmdString
		;stash offset, then write dummy length
		ldy		parout
		sty		a0

		;advance and copy until we find the terminating quote
loop:
		jsr		paCmdEmit.doEmitByte	;!! - also sets Y to offset written
		ldx		cix
		lda		lbuff,x
		cmp		#$9b
		beq		unterminated
		inc		cix
		cmp		#'"'
		bne		loop
unterminated:
		
		;compute length -- note that A0,Y are -1 from start and end of string
		tya
		sbc		a0				;!! - carry is set
		
		;store length
		ldy		a0
		sta		(argstk),y
		
		;resume
		ldx		#$12
.def :paCmdStr
.def :paCmdNum
		cpx		#$12
		ror		parStrType
		jmp		_parseLine.parse_loop
.endp

;============================================================================

.echo "- Parser length: ",*-?parser_start
