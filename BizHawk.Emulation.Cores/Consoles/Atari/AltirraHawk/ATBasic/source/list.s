; Altirra BASIC - LIST module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

?list_start = *

;==========================================================================
; LIST [filespec,] [lineno [,lineno]]
;
; If filespec is specified, IOCB #7 is used to send to that output.
;
; If one lineno is specified, only that line is listed. If two linenos
; are specified, lines within that range inclusive are listed. If the range
; is inverted, no lines are listed.
;
; Errors:
;	Error 3 if lineno not in [0,65535]
;	Error 7 if lineno in [32768, 65535]
;
; Unusual as it may be, it is perfectly OK to have a LIST statement inside
; of a running program. Therefore, we have to be careful not to disturb
; running execution state. We can, however, take over the argument stack
; area as well as the parser pointers.
;
; Another quirk in Atari BASIC is that if LIST is executed in deferred
; mode and Break is pressed, execution continues with the next statement
; instead of a stop occurring. We don't emulate this behavior right now.
;
.proc stList
_endline = stScratch2	;and stScratch3
_eos = stScratch4

		;init start and end lines
		lda		#$ff
		sta		_endline
		lsr
		sta		_endline+1
		lda		#0
		sta		parptr
		sta		parptr+1
		
		;assume IOCB #0
		sta		iocbidx
	
		;evaluate it
		jsr		evaluate

		;check if there is an argument
		ldy		argsp
		beq		no_lineno
		
		;test if it is a filespec
		lda		expType
		bpl		not_filespec
		
		;it's a filespec -- set and close IOCB #7
		jsr		IoSetupIOCB7
		
		;open IOCB for write
		lda		#$08
		jsr		IoDoOpenWithFilename
		
		;do listing
		jsr		do_list
		
		;close IOCB and exit
		jmp		IoClose
		
do_list:
		;check if we have more arguments
		jsr		ExecTestEnd
		beq		no_lineno
		
		;parse first line number after filename
		jsr		ExprSkipCommaAndEval
not_filespec:
		jsr		ExprConvFR0IntPos
		stx		parptr
		sta		parptr+1
		
		;check for a second line number
		jsr		ExecTestEnd
		beq		no_lineno2
		
		jsr		ExprSkipCommaAndEvalPopIntPos

no_lineno2:
		stx		_endline
		mva		fr0+1 _endline+1

no_lineno:
		;init first statement
		ldx		parptr
		lda		parptr+1
		jsr		exFindLineInt
		sta		parptr
		sty		parptr+1
		
		;turn on LIST mode display
		sec
		ror		dspflg
		
lineloop:
		;check that we haven't hit the end line; we'll always eventually
		;hit the immediate mode line
		ldy		#0
		clc
		lda		(parptr),y
		sta		fr0
		sbc		_endline
		iny
		lda		(parptr),y
		sta		fr0+1
		sbc		_endline+1
		bcc		not_done
		
		;turn off LIST mode display
		asl		dspflg
		
		;we're done
		rts
not_done:
		
		;convert line number; this will also set INBUFF = LBUFF
		jsr		IoPrintInt
		
		;add a space
		jsr		IoPutSpace

		;begin processing statements		
		ldy		#3
statement_loop:
		;read and cache the end of statement
		mva		(parptr),y+ _eos
		
		;read next token
		lda		(parptr),y+
		sty		parout
		
		;skip directly to function tokens if it's an implicit LET
		cmp		#TOK_ILET
		beq		do_function_tokens_loop

		;must special case syntax errors as the string isn't in the table
		;(otherwise it could be parsed)
		cmp		#TOK_SXERROR
		beq		syntax_error
		
		;lookup and print statement name
		pha
		ldy		#<statement_table
		ldx		#>statement_table
		jsr		ListPrintToken
		
		;print space
		jsr		IoPutSpace
		
		;check if we just printed REM, DATA or ERROR -- we must switch
		;to raw printing after this
		pla
		lsr					;check for TOK_REM ($00) or TOK_DATA ($01)
		beq		print_raw

		;process function tokens
do_function_tokens_loop:
		;fetch next function token
		jsr		ListGetByte
		cmp		#TOK_EOL
		beq		do_function_tokens_done
		jsr		ListPrintFunctionToken
		ldy		parout		

		;IF statements will abruptly stop after the THEN, so we must
		;catch that case
		cpy		_eos
		bne		do_function_tokens_loop
do_function_tokens_done:
		
statement_done:
		ldy		#2
		lda		(parptr),y
		cmp		_eos
		beq		line_done
		
		;next statement
		ldy		_eos
		bne		statement_loop
	
line_done:
		;advance to next line
		ldx		#parptr
		jsr		VarAdvancePtrX		

		;add a newline
		jsr		IoPutNewline

		;next line
		bpl		lineloop			;!! - unconditional

syntax_error:
		ldx		#msg_error2-msg_base
		jsr		IoPrintMessage
print_raw:
		jsr		ListGetByte
		cmp		#$9b
		beq		statement_done
		jsr		putchar
		bpl		print_raw			;!! - unconditional

print_const_number:
		pha
		ldx		#$fa
print_const_number_1:
		jsr		ListGetByte
		sta		fr0+6,x
		inx
		bne		print_const_number_1
		pla

		;check if we are doing hex or not
		cmp		#TOK_EXP_CHEX
		beq		print_const_hex_number
		jmp		IoPrintNumber

print_const_hex_number:
		lda		#'$'
		jsr		putchar
		sec
		jsr		IoConvNumToHex
		ora		#$80
		dey
		sta		(inbuff),y
		jmp		printStringINBUFF

print_const_string:
		;print starting quote
		jsr		print_const_string_2
		
		;get length
		jsr		ListGetByte
		sta		fr0
		beq		print_const_string_2
print_const_string_1:
		jsr		ListGetByte
		jsr		putchar
		dec		fr0
		bne		print_const_string_1
print_const_string_2:
		lda		#'"'
		jmp		putchar
		
print_var:
		and		#$7f
		ldy		vntp
		ldx		vntp+1
		jsr		ListPrintToken
		
		;check if we got an array var -- if so, we need to skip the open
		;parens token that's coming
		cmp		#'('+$80
		sne:inc	parout
		rts
.endp

;==========================================================================
; ListPrintFunctionToken
;
;--------------------------------------------------------------------------
; ListPrintToken
;
; Entry:
;	A = token index
;	X:Y = table start
;
; Exit:
;	A = last character in token
;
; Modified:
;	iterPtr
;
.proc ListPrintFunctionToken
		tax
		bmi		stList.print_var
		cmp		#TOK_EXP_CSTR
		bcc		stList.print_const_number
		beq		stList.print_const_string
		sbc		#$12			;!! - carry is set

		ldy		#<funtok_name_table_base
		ldx		#>funtok_name_table_base
.def :ListPrintToken
		sta		stScratch
		sty		iterPtr
		stx		iterPtr+1
		tya
		jmp		print_var_entry
print_var_loop:
		jsr		VarAdvanceName
print_var_entry:
		dec		stScratch
		bpl		print_var_loop
print_var_done:
		ldy		iterPtr+1
		jsr		IoSetInbuffYA
		jmp		printStringINBUFF
.endp

;==========================================================================
.proc ListGetByte
		ldy		parout
		inc		parout
		lda		(parptr),y
		rts
.endp

;==========================================================================
funtok_name_table_base:
		;$12
		dta		c','+$80
		dta		c'$'+$80
		dta		c':'+$80
		dta		c';'+$80
		dta		c'?'+$80
		dta		c' GOTO',c' '+$80
		dta		c' GOSUB',c' '+$80
		dta		c' TO',c' '+$80
		dta		c' STEP',c' '+$80
		dta		c' THEN',c' '+$80
		dta		c'#'+$80
		dta		c'<',c'='+$80
		dta		c'<',c'>'+$80
		dta		c'>',c'='+$80

		;$20
		dta		c'<'+$80
		dta		c'>'+$80
		dta		c'='+$80
		dta		c'^'+$80
		dta		c'*'+$80
		dta		c'+'+$80
		dta		c'-'+$80
		dta		c'/'+$80
		dta		c' NOT',c' '+$80
		dta		c' OR',c' '+$80
		dta		c' AND',c' '+$80
		dta		c'('+$80
		dta		c')'+$80
		dta		c'='+$80
		dta		c'='+$80
		dta		c'<',c'='+$80

		;$30
		dta		c'<',c'>'+$80
		dta		c'>',c'='+$80
		dta		c'<'+$80
		dta		c'>'+$80
		dta		c'='+$80
		dta		c'+'+$80
		dta		c'-'+$80
		dta		c'('+$80
		dta		c'('+$80
		dta		c'('+$80
		dta		c'('+$80
		dta		c'('+$80
		dta		c','+$80

funtok_name_table:
		;$3D
		dta		c'STR',c'$'+$80
		dta		c'CHR',c'$'+$80
		dta		c'US',c'R'+$80

		;$40
		dta		c'AS',c'C'+$80
		dta		c'VA',c'L'+$80
		dta		c'LE',c'N'+$80
		dta		c'AD',c'R'+$80
		dta		c'AT',c'N'+$80
		dta		c'CO',c'S'+$80
		dta		c'PEE',c'K'+$80
		dta		c'SI',c'N'+$80
		dta		c'RN',c'D'+$80
		dta		c'FR',c'E'+$80
		dta		c'EX',c'P'+$80
		dta		c'LO',c'G'+$80
		dta		c'CLO',c'G'+$80
		dta		c'SQ',c'R'+$80
		dta		c'SG',c'N'+$80
		dta		c'AB',c'S'+$80

		;$50
		dta		c'IN',c'T'+$80
		dta		c'PADDL',c'E'+$80
		dta		c'STIC',c'K'+$80
		dta		c'PTRI',c'G'+$80
		dta		c'STRI',c'G'+$80
		dta		$81
		dta		'%'+$80
		dta		'!'+$80
		dta		'&'+$80
		dta		$81
		dta		'BUMP','('*
		dta		$81
		dta		c'HEX',c'$'+$80
		dta		$81
		dta		c'DPEE',c'K'+$80
		dta		$81

		;$60
		dta		'VSTIC','K'+$80
		dta		'HSTIC','K'+$80
		dta		'PMAD','R'*
		dta		'ER','R'+$80
		dta		0

		_STATIC_ASSERT *-funtok_name_table<254, "Function token name table is too long."
.echo "-- Function token table length: ", *-funtok_name_table

.echo "- List module length: ",*-?list_start
