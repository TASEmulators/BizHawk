; Altirra BASIC - READ/INPUT data module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

?read_start = *

;===========================================================================
; INPUT [#aexp{,|;}] var [,var...]
;
; Reads input from the console or an I/O channel.
;
; If the IOCB is #0, either implicitly or explicitly, a question mark is
; printed to the console first. Whether the IOCB number is followed by
; a comma or semicolon doesn't matter.
;
; Leading spaces are included for string input and skipped for numeric
; input. Numeric input is considered invalid if the immediately following
; character is not a comma or a string of spaces (spaces followed by
; a comma is _not_ accepted). Numeric inputs may not be empty -- either
; a blank line or no input after a comma will cause a numeric input to
; fail.
;
; When multiple variables are supplied, subsequent variables are read in
; from the same line if more values are available as a comma separated
; list. If not, a new line is read in. String reads eat the comma as part
; of the string and always force a new line.
;
; The default line buffer is always used (255 bytes), even if a larger
; string buffer is supplied. The string is silently truncated as necessary.
; EOLs are not placed into string arrays. If the string array is not
; dimensioned, error 9 is issued, but only when that string array is
; reached and after input has been read.
;
; End of file gives an EOF error.
;
;==========================================================================
; READ var [,var...]
;
; Reads input from DATA lines in a program.
;
; Quirks:
;	- Atari BASIC caches the pointer and offset within the DATA line and
;	  does not update them when tables are adjusted. This means that
;	  editing the current program, even just by adding new variables,
;	  will cause corrupted READs from immediate mode unless a RESTORE is
;	  done.
;
; Entry (stDataRead):
;	IOCBIDX = IOCB to use for INPUT, or -1 for READ
;
.nowarn .proc stDataRead
need_data_line:
		;check if we are at line 32768
		;#ASSERT dw(dataptr) >= dw(stmtab) && dw(dataptr) < dw(starp)
		ldy		#1
		lda		(dataptr),y
		bpl		data_line_valid
		jmp		errorOutOfData

.def :stInput = *
		;parse optional #iocb and set iocbidx
		jsr		evaluateHashIOCBOpt
		bcs		read_loop
		
		;eat following comma or semicolon
		inc		exLineOffset
		bne		read_loop
.def :stRead = *
		lda		#$ff
		sta		iocbidx
		sta		iocbidx2
read_loop:
		;check if IOCB #0 was specified and we're in INPUT mode, and if so,
		;print a question mark. note that this happens even if the IOCB was
		;specified, i.e. INPUT #0,A$, and that INPUT #16;A$ should not print
		;a prompt.
		ldx		iocbidx2
		bne		skip_prompt
		
		;print ? prompt
		lda		#'?'
		jsr		IoPutCharDirect
		
skip_prompt:
		;reset read pointer
		lda		#0
		sta		cix

		;check if we are doing READ or INPUT
		ldx		iocbidx
		bpl		read_line_input
		
		;we're doing READ -- check if we have a valid cached line
		lda		dataptr+1
		bne		have_data_line
		
		;call into exec to get next line
		ldx		dataln
		lda		dataln+1
		jsr		exFindLineInt
		sta		dataptr
		sty		dataptr+1
		
		;reset starting index
have_data_line_2:
		mva		#0 dataoff
		
		;cache off line length
		ldy		#2
		mva		(dataptr),y dataLnEnd

have_data_line:
		;check if we have a valid index into a DATA statement already
		ldy		dataoff
		beq		need_data_line
		cpy		dataLnEnd
		bcc		have_data
		
data_line_end:
		;jump to next line
		lda		dataLnEnd
		ldx		#dataptr
		jsr		VarAdvancePtrX
		
		;##TRACE "Data: Advancing to line %u ($%04X)" dw(dw(dataptr)) dw(dataptr)
		;stash off line number
		ldy		#1
		mva		(0,x) dataln				;!! X = dataptr
		mva		(dataptr),y dataln+1
		bpl		have_data_line_2			;!! - unconditional

data_line_valid:
		;##TRACE "Data: Scanning for DATA token on line %u ($%04X)" dw(dw(dataptr)) dw(dataptr)
		ldy		#3
data_line_scan:
		;scan the line to find the next DATA statement
		cpy		dataLnEnd
		beq		data_line_end
		
		;fetch next statement token
		iny
		lda		(dataptr),y
		
		;is it the DATA token?
		cmp		#TOK_DATA
		beq		have_data_stmt
		
		;no... jump to next statement
		dey
		lda		(dataptr),y
		tay
		bne		data_line_scan				;!! - unconditional
				
have_data_stmt:
		iny
have_data:
		sty		cix

		lda		dataptr
		ldy		dataptr+1
		jsr		IoSetInbuffYA

		;##TRACE "Beginning READ with data: $%04X+$%02X [%.*s]" dw(dataptr) db(cix) db(dataLnEnd)-db(cix) dw(dataptr)+db(cix)
		bne		parse_loop					;!! - unconditional

is_eol:
		;force end of DATA statement, if we are reading one
		ldy		dataLnEnd

is_comma:
		;check if we are processing a DATA statement -- if so, stash the current
		;offset.
		bit		iocbidx
		spl:sty	dataoff

		;check if we have more vars to read
		;read current token and check for comma
		jsr		ExecGetComma
		bne		xit
				
		;read new line if line is empty, else keep parsing
		txa
		bne		parse_loop
		beq		read_loop		;!! - unconditional

read_line_input:
		;read line to LBUFF
		jsr		IoSetupReadLineLDBUFA
		jsr		ioChecked
				
parse_loop:
		;get next variable
		jsr		evaluateVar
				
		;check type of variable
		lda		expType
		bpl		is_numeric
		
		;we have a string... compute the remaining length
		;
		;READ statements will stop string reads at a comma; INPUT
		;statements will consume the comma. Note that we must NOT actually
		;consume the comma here, as the end of read routine needs to see it
		;in case we have this case:
		;
		; DATA ABC,
		;
		;Eating the comma here would cause the end of read code to jump to
		;the next line, preventing the empty trailing string from being read.
		;
		ldx		#fr1
		jsr		zf1
		ldy		cix
		sty		fr1				;set src offset
len_loop:
		lda		(inbuff),y
		cmp		#$9b
		beq		len_loop_end
		bit		iocbidx
		bpl		no_comma_stop
		cmp		#','
		beq		len_loop_end
no_comma_stop:
		iny
		inc		fr1+2
		bne		len_loop
len_loop_end:

		;warp to end of the input
		sty		cix

		;jump through string assignment code to set string array
		ldx		#fr1
		stx		expAsnCtx		;set assignment mode to force length update
		ldy		#inbuff
		jsr		IntAdd
		jsr		funAssignStr._read_entry

advance:
		;advance to next input, checking for EOL or a comma -- we must
		;do this before we store the parsed value and even if there are no
		;other values to retrieve
		ldy		cix
		lda		(inbuff),y

		;check if we had an EOL (and stash EOL flag in X)
		eor		#$9b
		tax
		beq		is_eol

		;not an EOL -- better be comma or it's an error
		iny
		sty		cix
		cmp		#[','^$9b]
		beq		is_comma

parse_error:
		jmp		errorInputStmt

xit:
		rts

is_numeric:
		;attempt to parse out a number
		jsr		MathParseFP
		bcs		parse_error

		;store numeric value to variable
		;##TRACE "READ -> %g" fr0
		jsr		VarStoreFR0

		beq		advance			;!! - unconditional
.endp

;============================================================================

.echo "- READ/INPUT module length: ",*-?read_start
