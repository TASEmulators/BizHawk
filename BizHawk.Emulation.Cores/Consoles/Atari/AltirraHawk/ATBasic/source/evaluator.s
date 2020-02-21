; Altirra BASIC - Expression evaluator module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

;===========================================================================
; The stack handling is conceptually similar to that of Atari BASIC, but
; has diverged quite a bit for efficiency. It now looks like this:
;
; +----------------------+  LOMEM + $100
; |    operator stack    |
; +----------------------+
; .          |           .
; .          v           .
; .                      .
; .          ^           .
; .          |           .
; +----------------------+
; |   argument stack 2   |
; +----------------------+  LOMEM + $6C
; .                      .
; .                      .
; .          ^           .
; .          |           .
; +----------------------+
; |   argument stack 1   |
; +----------------------+  LOMEM
;
; The argument stacks together contain the values pushed onto the stack
; and grow upward, while the operator stack grows downward from the top.
; opsp points to the last valid location. There is enough room for 36
; levels of nesting.
;
; The paired argument stack is very different from Atari BASIC. First,
; it only contains six bytes per entry instead of eight, omitting the
; type and variable bytes. This is because most of the time keeping
; these on the stack is unnecessary -- the argument types for each token
; type are already known and enforced by the parser. A couple of statements
; do take both types, including LIST, INPUT, and PRINT, and for those we
; maintain a type byte for the top of stack. Variable information is
; available from LVARPTR for the leftmost variable and VARPTR for the
; rightmost variable.
;
; The six bytes of the argument stack are split between the two argument
; stacks, with even bytes in stack 1 and odd bytes in stack 2. This serves
; two purposes, one being to reduce the amount of stack pointer futzing
; we have to do, and also to provide easy access to 16-bit quantities.
; argsp points to the next available location. It would be faster to
; store the stack as SoA like Turbo Basic XL does, but since we are running
; from ROM and have neither a suitable absolute addressed writable area
; nor enough zero page to burn on 6 pointers, we sacrifice a little
; speed here.
;
; There is one other trick that we do, which is to cache the top of stack
; in FR0. Each argument stack is actually shifted up one entry, with the
; first entry not used. This is a substantial performance and size
; optimization as it eliminates a lot of paired pushes and pops. For
; instance, instead of doing pop(fr1)/pop(fr0)/fadd/push(fr0) for an add
; token, we simply just do pop(fr1)/fadd. Unary operators are even
; simpler -- ABS() just has to clear FR0 bit 7!
;
; The bottom of the stack, argsp=0, has special significance when in an
; assignment context (expAsnCtx bit 7 = 1). Two differences in execution
; occur in this situation. First, a pointer to the variable's value is
; stashed in LVARPTR for later use. Second, the string indexing function
; allows selecting a range beyond the current length of a string.

;===========================================================================
ExprEvalIOCBCommaInt:
		jsr		evaluateHashIOCB
ExprSkipCommaAndEvalPopInt:
		inc		exLineOffset
.proc	evaluateInt
		jsr		evaluate
		jmp		ExprConvFR0Int
.endp

;===========================================================================
.proc evaluateHashIOCBOpt
		;default to IOCB #0
		ldx		#0
		stx		iocbidx
		
		;check if we have an IOCB
		jsr		ExecTestEnd
		cmp		#TOK_EXP_HASH
		sec							;set C=1 to indicate no #iocb found
		bne		valid_iocb

.def :evaluateHashIOCBNoCheckZero = *
		;fetch IOCB# -- note that we deliberately don't check the high
		;byte, for compatbility with Atari BASIC
		jsr		ExprSkipCommaAndEvalPopIntPos

		;IOCB #0 is allowed by some statements, so we don't check it here.
		;	OPEN - not allowed
		;	CLOSE - not allowed
		;	XIO - not allowed
		;	GET - not allowed
		;	PUT - not allowed
		;	NOTE - not allowed
		;	POINT - not allowed
		;	STATUS - not allowed
		;	PRINT - allowed
		;	INPUT - allowed
		;IOCB #8-15 aren't allowed, but #16 is (#$&*#)
		txa
		asl
		asl
		asl
		asl
		bmi		invalid_iocb
		sta		iocbidx

		clc							;set C=0 to indicate #iocb found
valid_iocb:
		stx		iocbidx2
		rts

invalid_iocb:
		jmp		errorBadDeviceNo
.endp

;===========================================================================
.proc	evaluateHashIOCB
		jsr		evaluateHashIOCBNoCheckZero
		txa
		beq		evaluateHashIOCBOpt.invalid_iocb
		rts
.endp

;===========================================================================
ExprSkipCommaAndEvalVar = ExprSkipCommaAndEval
evaluateVar = evaluate

;===========================================================================
.proc ExprPushLiteralConst
		sta		expType
		jsr		ExprPushExtFR0
		ldy		exLineOffset
		:6 mva (stmcur),y+ fr0+#
		sty		exLineOffset
		;##TRACE "Pushing literal constant: %g" fr0
		bne		evaluate.loop
.endp

;===========================================================================
.proc ExprPushLiteralStr
		;build argument stack entry
		jsr		ExprPushExtFR0
		lda		#$83
		sta		expType
		
		;length
		;dimensioned length	
		;load and stash string literal length (so we don't have to thrash Y)
		ldy		exLineOffset
		lda		(stmcur),y
		sta		fr0+2
		
		;skip past length and string in statement text
		sec
		adc		exLineOffset
		sta		exLineOffset		

		;address
		tya
		sec								;+1 to skip length
		adc		stmcur
		sta		fr0
		lda		#0
		sta		fr0+3
		adc		stmcur+1
		sta		fr0+1
		
		;all done
		bne		evaluate.loop
.endp


;===========================================================================
; Main expression evaluator.
;
; _assign_entry:
;	Special entry point that takes custom evaluation flags in the A
;	register:
;
;	bit 7 = assignment context - allow string bounds beyond current
;	        length for first lvalue
;
;	bit 6 = DIM context - allow references to undimensioned array/string
;	        variables
;
ExprSkipCommaAndEval:
		inc		exLineOffset
.proc	evaluate
_tmpadr = fr0+1

		;set up rvalue context
		lda		#0
		dta		{bit $0100}				;bit $80xx
.def :evaluateAssignment
		lda		#$80
_assign_entry:
		sta		expAsnCtx
		
		;;##TRACE "Beginning evaluation at $%04x+$%02x = $%04x" dw(stmcur) db(exLineOffset) dw(stmcur)+db(exLineOffset)

		;reset stack pointers
		ldy		#0
		sty		opsp
		sty		argsp
loop_open_parens:
		sty		expCommas
loop:
		;get next token
		ldy		exLineOffset
		inc		exLineOffset
		lda		(stmcur),y
		;;##TRACE "Processing token: $%02x ($%04x+$%02x=$%04x)" (a) dw(stmcur) y dw(stmcur)+y
		
		;check if this token needs to be reduced immediately
		bmi		is_variable
		cmp		#$0f
		bcc		ExprPushLiteralConst
		beq		ExprPushLiteralStr
			
		;==== reduce loop ====
				
		;reduce while precedence of new operator is equal or lower than
		;precedence of last operator on stack
		
		;get push-on / shift precedence
		;
		;if bit 7 is set, we immedately shift
		pha
		tax
		lda		prec_table-$12,x
		bmi		shift
		sta		expCurPrec
		;;##TRACE "Current operator get-on precedence = $%02x" a

reduce_loop:		
		ldy		opsp
		beq		reduce_done
		lda		(argstk),y
		
		;get pull-off/reduce precendence
		tax
		lda		prec_table-$12,x		;!! - $10 (COM) and $11 (CLOSE) can never follow an expression
		and		#$7e
		
		;stop reducing if the current operator has higher precedence
		;;##TRACE "Checking precedence: tos $%02x vs. cur $%02x" a db(expCurPrec)
		cmp		expCurPrec
		bcc		reduce_done

		inc		opsp
		jsr		dispatch
		;##ASSERT (db(argsp)%3)=0
		jmp		reduce_loop

reduce_done:
		;exit if this is not an expression token
		lda		expCurPrec
		beq		done

		;push current operator on stack
shift:
		dec		opsp	
		ldy		opsp
		pla
		;##TRACE "Shift: $%02x" (a)
		sta		(argstk),y
		bne		loop					;!! - unconditional (we would never shift a $00 token)

done:
		pla
		;;##TRACE "Exiting evaluator"
		dec		exLineOffset
		rts
				
is_variable:
		;##TRACE "Push variable $%02X" (a)
		;get value address of variable
		jsr		VarGetAddr0
		
		;check if this is the first var at the base -- if so, set the
		;lvalue ptr for possible assignment
		ldy		argsp
		bne		not_lvalue
		
		lda		varptr
		adc		#2
		sta		lvarptr
		lda		varptr+1
		adc		#0
		sta		lvarptr+1
		
		;since we know the stack is empty, we know we don't need to push, either
		ldy		#3
		sty		argsp
		bne		skip_push_fr0

not_lvalue:

		;push variable entry from VNTP onto argument stack
		jsr		ExprPushFR0NonEmpty

skip_push_fr0:
		;load variable
		jsr		VarLoadFR0

		;fetch type and set expression type
		ldy		#0
		lda		(varptr),y
		sta		expType
		
		;check if we had an array or string		
		;;##TRACE "arg %02x %02x %02x %02x" db(dw(argstk)+0) db(dw(argstk)+1) db(dw(argstk)+2) db(dw(argstk)+3)
		cmp		#$40
		bcc		loop

		;check if it is dimensioned
		lsr
		bcc		not_dimmed
		
undim_ok:
		;check if we have a relative pointer
		lsr
		bcs		loop
		
		;it's relative -- convert relative pointer to absolute
		;;##TRACE "Converting to absolute"
		ldy		#starp
		jsr		IntAddToFR0
		bne		loop		;!! - unconditional

not_dimmed:
		;check if we allow unDIM'd vars (i.e. we're in DIM)
		bit		expAsnCtx
		bvs		undim_ok
		jmp		errorDimError

dispatch:
		;##TRACE "Reduce: $%02x (%y) by %02x - %u values on stack (%02X%02X%02X%02X%02X%02X %g)" (x) db(functionDispatchTableLo-$1D+x)+256*db(functionDispatchTableHi-$1D+x)+1 db($100+((s+1)&$ff)) db(argsp)/3 db(dw(argstk)+db(argsp)-3) db(dw(argstk2)+db(argsp)-3) db(dw(argstk)+db(argsp)-2) db(dw(argstk2)+db(argsp)-2) db(dw(argstk)+db(argsp)-1) db(dw(argstk2)+db(argsp)-1) fr0
		lda		functionDispatchTableHi-$1D,x
		pha
		lda		functionDispatchTableLo-$1D,x
		pha

		;On entry to all functions, X is guaranteed to hold the operator
		;token. This is used by some multi-dispatch points.
		rts
.endp

;===========================================================================
; Precedence tables
;
; There are two precedences for each operator, a go-on and come-off
; precedence. A reduce happens if prec_on(cur) <= prec_off(tos); a
; shift happens otherwise. A prec_on of zero also terminates evaluation
; after the entire stack is reduced. prec_on is the value from the table,
; while prec_off = prec_on & $7F.
;
; If bit 7 is set, the operator always shifts when it is encountered.
; Unary operators and open parens need this.
;
; For arithmetic operators, prec_on <= prec_off for left associativity and
; prec_on > prec_off for right associativity. Bit 0 therefore indicates
; right associativity.
;
; Parentheses use a bit of a hack: open parens are force-shifted onto the
; stack but have a low precedence to allow arguments to accumulate. The
; close parens also has a low precedence and causes reduction of everything
; in between, including comma operators and the open paren. The open paren's
; reduction routine then terminates the reduction loop to prevent the close
; paren from reducing more than one nesting level or itself being
; shifted/reduced.
;
; Commas are shifted onto the stack along with the parameters they separate,
; and when finally reduced due to the close parenthesis, increment the comma
; count as they reduce. They have to be shifted onto the stack instead of
; reducing immediately so that expressions like USR(X,USR(A,B,C)) work --
; the outer call's commas need to be stacked so they don't collide with those
; of the inner call.
;
; Unary operators have to be right-associative. We don't care about order of
; unary +/-, but we do need to preserve ordering of NOT versus +/- so that
; -NOT 0 works. Atari BASIC does not allow this sequence, but Basic XE does.
;
PREC_PCLOSE		= 2
PREC_POPEN		= 4+$80
PREC_COMMA		= 6+$01			;Commas must be right associative so that nesting works, i.e. USR(0,1,2*(X-Y)
PREC_ASSIGN		= 8
PREC_OR			= 10
PREC_AND		= 12
PREC_NOT		= 14+$80
PREC_REL		= 16
PREC_ADD		= 18
PREC_MUL		= 20
PREC_BITWISE	= 22
PREC_EXP		= 24
PREC_UNARY		= 26+$80
PREC_RELSTR		= 28

;Normally, making functions right associative wouldn't make sense because
;there are always parentheses in between -- the closing parens will force
;reduction to the open parens, which will in turn force reduction of the
;function operator. However, SYSGEN from the Corvus disk has a uniquely
;broken statement:
;
; $494B: 22001 
; $494F:    $12 $36   
; $4950:      $80        DAT$
; $4951:      $2E        =
; $4952:      $3E        CHR$
; $4953:      $3A        (
; $4954:      $0E        16
; $495B:      $2C        )
; $495C:      $14        : 
; $495E:    $2C $36   
; $495F:      $80        DAT$
; $4960:      $37        (
; $4961:      $0E        2
; $4968:      $3C        ,
; $4969:      $0E        2
; $4970:      $2C        )
; $4971:      $2E        =
; $4972:      $3E        CHR$
; $4973:      $47        SIN
; $4974:      $9B        DRVNUM
; $4975:      $2C        )
; $4976:      $14        : 
; $4978:    $38 $36   
; $4979:      $9C        THELEN
; $497A:      $2D        =
; $497B:      $0E        2
; $4982:      $14        : 
; $4984:    $3C $0C   GOSUB 
; $4985:      $8A        BCI
; $4986:      $14        : 
; $4988:    $3F $24   RETURN  {end $498A}
;
;There is a SIN token where there should be a function open token. This
;happens to work because Atari BASIC computes SIN(DRVNUM) first, which
;rounds off to be 1 again. To make this work, we have to ensure that the
;SIN reduces first.
;
PREC_FUNC		= 30+$80

.proc	prec_table
		dta		0				;$12	,
		dta		0				;$13	$
		dta		0				;$14	: (statement end)
		dta		0				;$15	;
		dta		0				;$16	EOL
		dta		0				;$17	goto
		dta		0				;$18	gosub
		dta		0				;$19	to
		dta		0				;$1A	step
		dta		0				;$1B	then
		dta		0				;$1C	#
		dta		PREC_REL		;$1D	<=
		dta		PREC_REL		;$1E	<>
		dta		PREC_REL		;$1F	>=
		dta		PREC_REL		;$20	<
		dta		PREC_REL		;$21	>
		dta		PREC_REL		;$22	=
		dta		PREC_EXP		;$23	^
		dta		PREC_MUL		;$24	*
		dta		PREC_ADD		;$25	+
		dta		PREC_ADD		;$26	-
		dta		PREC_MUL		;$27	/
		dta		PREC_NOT		;$28	not
		dta		PREC_OR			;$29	or
		dta		PREC_AND		;$2A	and
		dta		PREC_POPEN		;$2B	(
		dta		PREC_PCLOSE		;$2C	)
		dta		PREC_ASSIGN		;$2D	= (numeric assignment)
		dta		PREC_ASSIGN		;$2E	= (string assignment)
		dta		PREC_RELSTR		;$2F	<= (strings)
		dta		PREC_RELSTR		;$30	<>
		dta		PREC_RELSTR		;$31	>=
		dta		PREC_RELSTR		;$32	<
		dta		PREC_RELSTR		;$33	>
		dta		PREC_RELSTR		;$34	=
		dta		PREC_UNARY		;$35	+ (unary)
		dta		PREC_UNARY		;$36	-
		dta		PREC_POPEN		;$37	( (string left paren)
		dta		PREC_POPEN		;$38	( (array left paren)
		dta		PREC_POPEN		;$39	( (dim array left paren)
		dta		PREC_POPEN		;$3A	( (fun left paren)
		dta		PREC_POPEN		;$3B	( (dim str left paren)
		dta		PREC_COMMA		;$3C	, (array/argument comma)
		
		;$3D and on are functions
		dta		PREC_FUNC		;$3D
		dta		PREC_FUNC		;$3E
		dta		PREC_FUNC		;$3F
		dta		PREC_FUNC		;$40
		dta		PREC_FUNC		;$41
		dta		PREC_FUNC		;$42
		dta		PREC_FUNC		;$43
		dta		PREC_FUNC		;$44
		dta		PREC_FUNC		;$45
		dta		PREC_FUNC		;$46
		dta		PREC_FUNC		;$47
		dta		PREC_FUNC		;$48
		dta		PREC_FUNC		;$49
		dta		PREC_FUNC		;$4A
		dta		PREC_FUNC		;$4B
		dta		PREC_FUNC		;$4C
		dta		PREC_FUNC		;$4D
		dta		PREC_FUNC		;$4E
		dta		PREC_FUNC		;$4F
		dta		PREC_FUNC		;$50
		dta		PREC_FUNC		;$51
		dta		PREC_FUNC		;$52
		dta		PREC_FUNC		;$53
		dta		PREC_FUNC		;$54
		dta		PREC_FUNC		;$55
		dta		PREC_BITWISE	;$56	% (xor)
		dta		PREC_BITWISE	;$57	! (or)
		dta		PREC_BITWISE	;$58	& (and)
		dta		PREC_FUNC		;$59
		dta		PREC_POPEN		;$5A	BUMP(
		dta		PREC_FUNC		;$5B
		dta		PREC_FUNC		;$5C
		dta		PREC_FUNC		;$5D
		dta		PREC_FUNC		;$5E
		dta		PREC_FUNC		;$5F
		dta		PREC_FUNC		;$60
		dta		PREC_FUNC		;$61
.endp

;===========================================================================
ExprPopExtFR0 = expPopFR0

;===========================================================================
ExprFmoveAndPopFR0:
		jsr		fmove
.proc	expPopFR0
		ldy		argsp
		;##ASSERT (y%3)=0 and y
		dey
		mva		(argstk2),y fr0+5
		mva		(argstk),y fr0+4
		dey
		mva		(argstk2),y fr0+3
		mva		(argstk),y fr0+2
		dey
		mva		(argstk2),y fr0+1
		mva		(argstk),y fr0
		sty		argsp
		rts
.endp

;===========================================================================
; Output:
;	A:X = integer value
;	P.N,Z = set from A
;	P.C = 0 (since we fire an error if C=1 from FPI)
;
.proc	expPopFR0Int
		jsr		expPopFR0
.def :ExprConvFR0Int = *
		jsr		fpi
		bcs		fail
		ldx		fr0
		lda		fr0+1
exit:
		rts
fail:
		jmp		errorValueErr
.endp

;===========================================================================
; Output:
;	A:X = integer value
;	P.N,Z = set from A
;	P.C = 0
;
ExprSkipCommaAndEvalPopIntPos:
		inc		exLineOffset
ExprEvalPopIntPos:
		jsr		evaluate
.proc	ExprConvFR0IntPos
		jsr		ExprConvFR0Int
		bpl		expPopFR0Int.exit
		jmp		errorValue32K
.endp

;===========================================================================
; Exit:
;	A = exponent/sign of popped value
;	P.NZ = set from A
;
.proc	expPopFR1
		ldy		argsp
		;##ASSERT (y%3)=0 and y
		dey
		mva (argstk2),y fr1+5
		mva (argstk),y fr1+4
		dey
		mva (argstk2),y fr1+3
		mva (argstk),y fr1+2
		dey
		mva (argstk2),y fr1+1
		mva (argstk),y fr1
		sty		argsp
		rts
.endp

;===========================================================================
.proc	ExprPushExtFR0
		ldy		argsp
		beq		stack_empty
.def :ExprPushFR0NonEmpty = *
		mva		fr0 (argstk),y
		mva		fr0+1 (argstk2),y+
		mva		fr0+2 (argstk),y
		mva		fr0+3 (argstk2),y+
		mva		fr0+4 (argstk),y
		mva		fr0+5 (argstk2),y+
		dta		{bit $0100}
stack_empty:
		ldy		#3
		sty		argsp
.def :funNoOp
		rts
.endp

;===========================================================================
.macro FUNCTION_DISPATCH_TABLE
		;$1D
		dta		:1[funCompare-1]
		dta		:1[funCompare-1]
		dta		:1[funCompare-1]

		;$20
		dta		:1[funCompare-1]
		dta		:1[funCompare-1]
		dta		:1[funCompare-1]
		dta		:1[funPower-1]
		dta		:1[funMultiply-1]
		dta		:1[funAdd-1]
		dta		:1[funSubtract-1]
		dta		:1[funDivide-1]
		dta		:1[funNot-1]
		dta		:1[funOr-1]
		dta		:1[funAnd-1]
		dta		:1[funOpenParens-1]
		dta		:1[funNoOp-1]				;Necessary for SYSGEN (see above).
		dta		:1[funAssignNum-1]
		dta		:1[funAssignStr-1]
		dta		:1[funStringCompare-1]

		;$30
		dta		:1[funStringCompare-1]
		dta		:1[funStringCompare-1]
		dta		:1[funStringCompare-1]
		dta		:1[funStringCompare-1]
		dta		:1[funStringCompare-1]
		dta		:1[funUnaryPlus-1]
		dta		:1[funUnaryMinus-1]
		dta		:1[funArrayStr-1]
		dta		:1[funArrayNum-1]
		dta		:1[funDimArray-1]
		dta		:1[funOpenParens-1]
		dta		:1[funDimStr-1]
		dta		:1[funArrayComma-1]
		
		;$3D
		dta		:1[funStr-1]
		dta		:1[funChr-1]
		dta		:1[funUsr-1]

		;$40
		dta		:1[funAsc-1]
		dta		:1[funVal-1]
		dta		:1[funLen-1]
		dta		:1[funAdr-1]
		dta		:1[funAtn-1]
		dta		:1[funCos-1]
		dta		:1[funPeek-1]
		dta		:1[funSin-1]
		dta		:1[funRnd-1]
		dta		:1[funFre-1]
		dta		:1[funExp-1]
		dta		:1[funLog-1]
		dta		:1[funClog-1]
		dta		:1[funSqr-1]
		dta		:1[funSgn-1]
		dta		:1[funAbs-1]
		
		;$50
		dta		:1[funInt-1]
		dta		:1[funPaddleStick-1]		;PADDLE
		dta		:1[funPaddleStick-1]		;STICK
		dta		:1[funPaddleStick-1]		;PTRIG
		dta		:1[funPaddleStick-1]		;STRIG
		dta		:1[funInvalid-1]			;USING
		dta		:1[funBitwiseXor-1]
		dta		:1[funBitwiseOr-1]
		dta		:1[funBitwiseAnd-1]
		dta		:1[funInvalid-1]			;semicolon
		dta		:1[funBump-1]				;BUMP
		dta		:1[funInvalid-1]			;FIND
		dta		:1[funHex-1]
		dta		:1[funInvalid-1]			;RANDOM
		dta		:1[funDpeek-1]
		dta		:1[funInvalid-1]			;SYS

		;$60
		dta		:1[funVstick-1]
		dta		:1[funHstick-1]
		dta		:1[funPmadr-1]				;PMADR
		dta		:1[funErr-1]
.endm

funInvalid = errorWTF

;===========================================================================
.proc functionDispatchTableLo
		FUNCTION_DISPATCH_TABLE <
.endp

.proc functionDispatchTableHi
		FUNCTION_DISPATCH_TABLE >
.endp
