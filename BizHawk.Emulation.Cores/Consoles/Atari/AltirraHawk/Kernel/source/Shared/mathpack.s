;	Altirra - Atari 800/800XL/5200 emulator
;	Modular Kernel ROM - Decimal Floating-Point Math Pack
;	Copyright (C) 2008-2016 Avery Lee
;
;	Copying and distribution of this file, with or without modification,
;	are permitted in any medium without royalty provided the copyright
;	notice and this notice are preserved.  This file is offered as-is,
;	without any warranty.

;==========================================================================
;
; Known problems:
;	Currently incompatible with BASIC XE due to it relying on $DE-DF not
;	being modified by FADD/FSUB.
;
;==========================================================================
;
;                                       Notes
;
;                   AFP  FMUL
;                   |FASC.FDIV
;                   |.IPF.|PLYEVL
;                   |.|FPI|.EXP
;                   |.|.FADD|REDRNG
;                   |.|.|.|.|.LOG
;                   vvvvvvvvvvv
; $D4 FR0             M MMM.
; $D5  |              M MMM.
; $D6  |              M MMM.
; $D7  |              M MMM.
; $D8  |              M MMM.
; $D9  v              M MMM.
; $DA FR2 {FRE}     TTTTTTT.
; $DB  |            TT TTTT.
; $DC  |            TT TTTT.
; $DD  |            TT TTTT.
; $DE  |            T   TTT.            [1,2]
; $DF  v                TTT.            [1,2]
; $E0 FR1               MM .            [3]
; $E1  |                MM .            [3]
; $E2  |                MM .            [3]
; $E3  |                MM .            [3]
; $E4  |                MM .            [3]
; $E5  v                MM .            [3]
; $E6 FR3 {FR2}            . .
; $E7                      . .
; $E8                      . .
; $E9                      . .
; $EA                      . .
; $EB                      . .
; $EC {FRX}                . .
; $ED {EEXP}               . .
; $EE {NSIGN}              . .
; $EF {ESIGN}              . .
; $F0 {FCHRFLG}            . .
; $F1 {DIGRT}              . .
; $F2 CIX                  . .
; $F3 INBUFF               . .
; $F4  v                   . .
; $F5 {ZTEMP1}             . .
; $F6  v                   . .
; $F7 {ZTEMP4}             . .
; $F8  v                   . .
; $F9 {ZTEMP3}             . .
; $FA  v                   . .
; $FB DEGFLG/RADFLG        . .
; $FC FLPTR                . .
; $FD  v                   . .
; $FE FPTR2                . .
; $FF  v                   . .
; $05E0  PLYARG            T T
; $05E1   |                T T
; $05E2   |                T T
; $05E3   |                T T
; $05E4   |                T T
; $05E5   v                T T
; $05E6  FPSCR               TT
; $05E7   |                  TT
; $05E8   |                  TT
; $05E9   |                  TT
; $05EA   |                  TT
; $05EB   v                  TT
; $05EC  FPSCR1
; $05ED   |
; $05EE   |
; $05EF   |
; $05F0   |
; $05F1   v
;
; Notes:
; [1] BASIC XE relies on $DE/DF not being touched by FADD, or FOR/NEXT
;     breaks.
; [2] MAC/65 relies on $DE/DF not being touched by IPF.
; [3] DARG relies on FPI not touching FR1.
; [4] ACTris 1.2 relies on FASC not touching lower parts of FR2.
;

.macro	ckaddr
.if * <> %%1
		.error	'Incorrect address: ',*,' != ',%%1
.endif
.endm

.macro	fixadr
.if * < %%1
		.print (%%1-*),' bytes free before ',%%1
		org		%%1
.elif * > %%1
		.error 'Out of space: ',*,' > ',%%1,' (',*-%%1,' bytes over)'
		.endif
.endm

;==========================================================================
; AFP [D800]	Convert ASCII string at INBUFF[CIX] to FR0
;
	org		$d800
_afp = afp
.proc afp
dotflag = fr2
xinvert = fr2+1
cix0 = fr2+2
sign = fr2+3
digit2 = fr2+4

	;skip initial spaces
	jsr		skpspc

	;init FR0 and one extra mantissa byte
	lda		#$7f
	sta		fr0
	sta		digit2
	
	ldx		#fr0+1
	jsr		zf1

	;clear decimal flag
	sta		dotflag
	sta		sign
	
	;check for sign
	ldy		cix
	lda		(inbuff),y
	cmp		#'+'
	beq		isplus
	cmp		#'-'
	bne		postsign
	ror		sign
isplus:
	iny
postsign:	
	sty		cix0

	;skip leading zeroes
	lda		#'0'
	jsr		fp_skipchar
	
	;check if next char is a dot, indicating mantissa <1
	lda		(inbuff),y
	cmp		#'.'
	bne		not_tiny
	iny
	
	;set dot flag
	ror		dotflag

	;increment anchor so we don't count the dot as a digit for purposes
	;of seeing if we got any digits
	inc		cix0
	
	;skip zeroes and adjust exponent
	lda		#'0'
tiny_denorm_loop:
	cmp		(inbuff),y
	bne		tiny_denorm_loop_exit
	dec		fr0
	iny
	bne		tiny_denorm_loop
tiny_denorm_loop_exit:
	
not_tiny:

	;grab digits left of decimal point
	ldx		#1
nextdigit:
	lda		(inbuff),y
	cmp		#'E'
	beq		isexp
	iny
	cmp		#'.'
	beq		isdot
	eor		#'0'
	cmp		#10
	bcs		termcheck
	
	;write digit if we haven't exceeded digit count
	cpx		#6
	bcs		afterwrite
	
	bit		digit2
	bpl		writehi

	;clear second digit flag
	dec		digit2
	
	;merge in low digit
	ora		fr0,x
	sta		fr0,x
	
	;advance to next byte
	inx
	bne		afterwrite
	
writehi:
	;set second digit flag
	inc		digit2
	
	;shift digit to high nibble and write
	asl
	asl
	asl
	asl
	sta		fr0,x

afterwrite:
	;adjust digit exponent if we haven't seen a dot yet
	bit		dotflag
	smi:inc	fr0
	
	;go back for more
	jmp		nextdigit
	
isdot:
	lda		dotflag
	bne		termcheck
	
	;set the dot flag and loop back for more
	ror		dotflag
	bne		nextdigit

termcheck:
	dey
	cpy		cix0
	beq		err_carryset
term:
	;stash offset
	sty		cix

term_rollback_exp:
	;divide digit exponent by two and merge in sign
	rol		sign
	ror		fr0
	
	;check if we need a one digit shift
	bcs		nodigitshift

	;shift right one digit
	ldx		#4
digitshift:
	lsr		fr0+1
	ror		fr0+2
	ror		fr0+3
	ror		fr0+4
	ror		fr0+5
	dex
	bne		digitshift

nodigitshift:
	jmp		fp_normalize

err_carryset:
	rts

isexp:
	cpy		cix0
	beq		err_carryset
	
	;save off this point as a fallback in case we don't actually have
	;exponential notation
	sty		cix

	;check for sign
	ldx		#0
	iny
	lda		(inbuff),y
	cmp		#'+'
	beq		isexpplus
	cmp		#'-'
	bne		postexpsign
	dex						;x=$ff
isexpplus:
	iny
postexpsign:
	stx		xinvert

	;pull up to two exponent digits -- check first digit
	jsr		fp_isdigit_y
	iny
	bcs		term_rollback_exp
	
	;stash first digit
	tax
	
	;check for another digit
	jsr		fp_isdigit_y
	bcs		notexpzero2
	iny

	adc		fp_mul10,x
	tax
notexpzero2:
	txa
	
	;zero is not a valid exponent
	beq		term_rollback_exp
	
	;check if mantissa is zero -- if so, don't bias
;	ldx		fr0+1
;	beq		term
	
	;apply sign to exponent
	eor		xinvert
	rol		xinvert

	;bias digit exponent
	adc		fr0
	sta		fr0
expterm:
	jmp		term

.endp

;==========================================================================
.proc fp_fmul_carryup
round_loop:
	adc		fr0,x
	sta		fr0,x
dec_entry:
	dex
	lda		#0
	bcs		round_loop
	rts
.endp

;==========================================================================
.proc fp_tab_lo_100
	:10 dta <[100*#]
.endp

;==========================================================================
		fixadr	$d8e6
_fasc = fasc
.proc fasc
dotcntr = ztemp4
expval = ztemp4+1
trimbase = ztemp4+2
	jsr		ldbufa
	ldy		#0

	;read exponent and check if number is zero
	lda		fr0
	bne		notzero
	
	lda		#$b0
	sta		(inbuff),y
	rts
	
notzero:
	sty		expval
	sty		trimbase

	;insert sixth mantissa byte
	sty		fr0

	;check if number is negative
	bpl		ispos
	ldx		#'-'
	dec		inbuff
	stx		lbuff-1
	inc		trimbase
	iny
ispos:

	;set up for 5 mantissa bytes
	ldx		#-5

	;compute digit offset to place dot
	;  0.001 (10.0E-04) = 3E 10 00 00 00 00 -> -1
	;   0.01 ( 1.0E-02) = 3F 01 00 00 00 00 -> 1
	;    0.1 (10.0E-02) = 3F 10 00 00 00 00 -> 1
	;    1.0 ( 1.0E+00) = 40 01 00 00 00 00 -> 3
	;   10.0 (10.0E+00) = 40 10 00 00 00 00 -> 3
	;  100.0 ( 1.0E+02) = 40 01 00 00 00 00 -> 5
	; 1000.0 (10.0E+02) = 40 10 00 00 00 00 -> 5

	asl
	sec
	sbc		#125

	;check if we should go to exponential form (exp >= 10 or <=-3)
	cmp		#12
	bcc		noexp

	;yes - compute and stash explicit exponent
	sbc		#2				;!! - carry set from BCC fail
	sta		expval			;$0A <= expval < $FE

	;reset dot counter
	lda		#2

	;exclude first two digits from zero trim
	inc		trimbase
	inc		trimbase

noexp:		
	;check if number is less than 1.0 and init dot counter
	cmp		#2
	bcs		not_tiny
	
	;use sixth mantissa byte
	adc		#2
	dex
not_tiny:
	sta		dotcntr			;$02 <= dotcntr < $0C
	
	;check if number begins with a leading zero
	lda		fr0+6,x
	cmp		#$10
	bcs		digitloop

	dec		trimbase

	;yes - skip the high digit
	lsr		expval
	asl		expval
	bne		writelow
	dec		dotcntr
	bcc		writelow

	;write out mantissa digits
digitloop:
	dec		dotcntr
	bne		no_hidot
	lda		#'.'
	sta		(inbuff),y
	iny
no_hidot:

	;write out high digit
	lda		fr0+6,x
	lsr
	lsr
	lsr
	lsr
	ora		#$30
	sta		(inbuff),y
	iny
	
writelow:
	;write out low digit
	dec		dotcntr
	bne		no_lodot
	lda		#'.'
	sta		(inbuff),y
	iny
no_lodot:
	
	lda		fr0+6,x
	and		#$0f
	ora		#$30
	sta		(inbuff),y
	iny

	;next digit
	inx
	bne		digitloop

	;skip trim if dot hasn't been written
	lda		dotcntr
	bpl		skip_zero_trim	
	
	;trim off leading zeroes
	lda		#'0'
lzloop:
	cpy		trimbase
	beq		stop_zero_trim
	dey
	cmp		(inbuff),y
	beq		lzloop

	;trim off dot
stop_zero_trim:
	lda		(inbuff),y
	cmp		#'.'
	bne		no_trailing_dot

skip_zero_trim:
	dey
	lda		(inbuff),y
no_trailing_dot:

	;check if we have an exponent to deal with
	ldx		expval
	beq		noexp2
	
	;print an 'E'
	lda		#'E'
	iny
	sta		(inbuff),y
	
	;check for a negative exponent
	txa
	bpl		exppos
	eor		#$ff
	tax
	inx
	lda		#'-'
	dta		{bit $0100}
exppos:
	lda		#'+'
expneg:
	iny
	sta		(inbuff),y
	
	;print tens digit, if any
	txa
	sec
	ldx		#$2f
tensloop:
	inx
	sbc		#10
	bcs		tensloop
	pha
	txa
	iny
	sta		(inbuff),y
	pla
	adc		#$3a
	iny
noexp2:
	;set high bit on last char
	ora		#$80
	sta		(inbuff),y
	rts
.endp

;==========================================================================
; IPF [D9AA]	Convert 16-bit integer at FR0 to FP
;
; !NOTE! Cannot use FR2/FR3 -- MAC/65 requires that $DE-DF be preserved.
;
	fixadr	$d9aa
.proc ipf
	sed

	ldx		#fr0+2
	ldy		#5
	jsr		zfl
	
	ldy		#16
byteloop:
	;shift out binary bit
	asl		fr0
	rol		fr0+1
	
	;shift in BCD bit
	lda		fr0+4
	adc		fr0+4
	sta		fr0+4
	lda		fr0+3
	adc		fr0+3
	sta		fr0+3
	rol		fr0+2
	
	dey
	bne		byteloop
	
	lda		#$43
	sta		fr0

	jmp		fp_normalize_cld
.endp

;==========================================================================
; FPI [D9D2]	Convert FR0 to 16-bit integer at FR0 with rounding
;
; This cannot overwrite FR1. Darg relies on being able to stash a value
; there across a call to FPI in its startup.
;
	fixadr	$d9d2
.nowarn .proc fpi
_acc0 = fr2
_acc1 = fr2+1
	
	;error out if it's guaranteed to be too big or negative (>999999)
	lda		fr0
	cmp		#$43
	bcs		err

	;zero number if it's guaranteed to be too small (<0.01)
	sbc		#$3f-1			;!!- carry is clear
	bcc		zfr0

	tax
	
	;clear temp accum and set up rounding
	lda		#0
	ldy		fr0+1,x
	cpy		#$50
	rol						;!! - clears carry too
	sta		fr0
	lda		#0

	;check for [0.01, 1)
	dex
	bmi		done

	;convert ones/tens digit pair to binary (one result byte: 0-100)
	lda		fr0+1,x
	jsr		fp_dectobin
	adc		fr0
	adc		fp_dectobin_tab,y
	clc
	sta		fr0
	lda		#0

	;check if we're done
	dex
	bmi		done

	;convert hundreds/thousands digit pair to binary (two result bytes: 0-10000)
	lda		fr0+1,x
	jsr		fp_dectobin
	lda		fr0
	adc		fp_tab_lo_1000,y
	sta		fr0
	lda		fp_tab_hi_1000,y
	adc		#0
	pha
	lda		fr0+1,x
	and		#$0f
	tay
	lda		fr0
	adc		fp_tab_lo_100,y
	sta		fr0
	pla
	adc		fp_tab_hi_100,y

	;check if we're done
	dex
	bmi		done

	;convert ten thousands digit pair to binary (two result bytes: 0-100000, overflow possible)
	ldy		fr0+1,x
	cpy		#$07
	bcs		err
	tax
	tya
	asl
	asl
	asl
	asl
	adc		fr0
	sta		fr0
	txa
	adc		fp_tab_hi_10000-1,y

done:
	;move result back to FR0, with rounding
	sta		fr0+1
err:
	rts
.endp

;==========================================================================
fp_mul10:
	dta		0,10,20,30,40,50,60,70,80,90

;==========================================================================
; ZFR0 [DA44]	Zero FR0
; ZF1 [DA46]	Zero float at (X)
; ZFL [DA48]	Zero float at (X) with length Y (UNDOCUMENTED)
;
	fixadr	$da44
zfr0:
	ldx		#fr0
	ckaddr	$da46
zf1:
	ldy		#6
	ckaddr	$da48
zfl:
	lda		#0
zflloop:
	sta		0,x
	inx
	dey
	bne		zflloop
	rts

;==========================================================================
; LDBUFA [DA51]	Set LBUFF to #INBUFF (UNDOCUMENTED)
;
		fixadr	$da51
ldbufa:
	mwa		#lbuff inbuff
	rts

;==========================================================================
; FPILL_SHL16 [DA5A] Shift left 16-bit word at $F7:F8 (UNDOCUMENTED)
;
; Illegal entry point used by MAC/65 when doing hex conversion.
;
; Yes, even the byte ordering is wrong.
;
		fixadr	$da5a
	
.nowarn .proc fpill_shl16
		asl		$f8
		rol		$f7
		rts
.endp

;** 1 byte free**

;==========================================================================
; FSUB [DA60]	Subtract FR1 from FR0; FR1 is altered
; FADD [DA66]	Add FR1 to FR0; FR1 is altered
		fixadr	$da60
fadd = fsub._fadd
.proc fsub

_diffmode = fr1

	;toggle sign on FR1
	lda		fr1
	eor		#$80
	sta		fr1
	
	;fall through to FADD
	
	ckaddr	$da66
_fadd:
	;if fr1 is zero, we're done
	lda		fr1
	beq		sum_xit
	
	;if fr0 is zero, swap
	lda		fr0
	beq		swap

	;compute difference in exponents, ignoring sign
	lda		fr1			;load fr1 sign
	eor		fr0			;compute fr0 ^ fr1 signs
	and		#$80		;mask to just sign
	tax
	eor		fr1			;flip fr1 sign to match fr0
	clc
	sbc		fr0			;compute difference in exponents - 1
	bcc		noswap
	
	;swap FR0 and FR1
swap:
	jsr		fp_swap
	
	;loop back and retry
	bmi		_fadd
	
noswap:	
	;A = FR1 - FR0 - 1
	;X = add/sub flag

	;compute positions for add/subtract	
	adc		#6			;A = (FR1) - (FR0) + 6   !! carry is clear coming in
	tay
	
	;check if FR1 is too small in magnitude to matter
	bmi		sum_xit
	
	;jump to decimal mode and prepare for add/sub loops
	sed

	;check if we are doing a sum or a difference
	cpx		#$80
	ldx		#5
	bcs		do_subtract
	
	;set up rounding
	lda		#0
	cpy		#5
	bcs		add_no_round
	lda		fr1+1,y
add_no_round:
	cmp		#$50
		
	;add mantissas
	tya
	beq		post_add_loop
add_loop:
	lda		fr1,y
	adc		fr0,x
	sta		fr0,x
	dex
	dey
	bne		add_loop
post_add_loop:
		
	;check if we had a carry out
	bcc		sum_xit
	
	;carry it up
	bcs		sum_carryloop_start
sum_carryloop:
	lda		fr0+1,x
	adc		#0
	sta		fr0+1,x
	bcc		sum_xit
sum_carryloop_start:
	dex
	bpl		sum_carryloop

	jsr		fp_carry_expup
	
sum_xit:
	;exit decimal mode
	;normalize if necessary and exit (needed for borrow, as well to check over/underflow)
	jmp		fp_normalize_cld

do_subtract:
	;subtract FR0 and FR1 mantissas (!! carry is set coming in)
	sty		fr1
	bcs		sub_loop_entry
sub_loop:
	lda		fr0,x
	sbc		fr1+1,y
	sta		fr0,x
	dex
sub_loop_entry:
	dey
	bpl		sub_loop
	jmp		fp_fsub_cont
.endp

;==========================================================================
; Entry:
;	A = BCD value
;	P.D = clear
;
; Exit:
;	A = binary value
;	Y = modified
;
.proc fp_dectobin
	pha
	lsr
	lsr
	lsr
	lsr
	tay
	pla
.def :fp_exit_success
	clc
	rts
.endp

;==========================================================================
; FMUL [DADB]:	Multiply FR0 * FR1 -> FR0
;
	fixadr	$dad6
fp_fld1r_const_fmul:
	ldy		#>fpconst_ten
fp_fld1r_fmul:
	jsr		fld1r
	ckaddr	$dadb
.proc fmul

	;We use FR0:FR3 as a double-precision accumulator, and copy the
	;original multiplicand value in FR0 to FR1. The multiplier in
	;FR1 is converted to binary digit pairs into FR2.
	
_offset = _fr3+5
_offset2 = fr2

	;if FR0 is zero, we're done
	lda		fr0
	beq		fp_exit_success
	
	;if FR1 is zero, zero FR0 and exit
	lda		fr1
	clc
	beq		fp_exit_zero
	
	;move fr0 to fr2
	jsr		fp_fmul_fr0_to_binfr2
	
	;compute new exponent and stash
	lda		fr1
	clc
	jsr		fp_adjust_exponent.fmul_entry
	
	sta		fr0
	inc		fr0

	;clear accumulator through to exponent byte of fr1
	ldx		#fr0+1
	ldy		#12
	sed

	jmp		fp_fmul_innerloop
.endp

underflow_overflow:
	pla
	pla
fp_exit_zero:
	jmp		zfr0

.proc fp_adjust_exponent
fdiv_entry:
	lda		fr1
	eor		#$7f
	sec
fmul_entry:
	;stash modified exp1
	tax
	
	;compute new sign
	eor		fr0
	and		#$80
	sta		fr1
	
	;merge exponents
	txa
	adc		fr0
	tax
	eor		fr1
	
	;check for underflow/overflow
	cmp		#128-49
	bcc		underflow_overflow
	
	cmp		#128+49
	bcs		underflow_overflow
	
	;rebias exponent
	txa
	sbc		#$40-1		;!! - C=0 from bcs fail
	rts
.endp

;==========================================================================
	.pages 1	;optimized by fp_fld1r_const_fmul
	
fpconst_ten:
	.fl		10

fpconst_ln10:
	.fl		2.3025850929940456840179914546844

	.endpg
;==========================================================================
; FDIV [DB28]	Divide FR0 / FR1 -> FR0
;
; Compatibility:
;	- It is important that FDIV rounds if FADD/FMUL do. Otherwise, some
;	  forms of square root computation can have a slight error on integers,
;	  which breaks TICKTOCK.BAS.
;
		fixadr		$db28
.proc fdiv
_digit = _fr3+1
_index = _fr3+2
	;check if divisor is zero
	lda		fr1
	beq		err

	;check if dividend is zero
	lda		fr0
	beq		ok
	
	;compute new exponent
	jsr		fp_adjust_exponent.fdiv_entry

	jsr		fp_fdiv_init	

digitloop:
	;just keep going if we're accurate
	lda		fr0
	ora		fr0+1
	beq		nextdigit
	
	;check if we should either divide or add based on current sign (stored in carry)
	bcc		incloop

	jsr		fp_fdiv_decloop
	bcc		nextdigit
	
incloop:
	;decrement quotient mantissa byte
	lda		#0
	sbc		_digit
	ldx		_index
downloop:
	adc		fr2+7,x
	sta		fr2+7,x
	lda		#$99
	dex
	bcc		downloop
	
	;add mantissas
	clc
	.rept 6
		lda		fr0+(5-#)
		adc		fr1+(5-#)
		sta		fr0+(5-#)
	.endr
	
	;keep going until we overflow
	bcc		incloop	
	
nextdigit:
	;shift dividend (make sure to save carry state)
	php
	ldx		#4
bitloop:
	asl		fr0+5
	rol		fr0+4
	rol		fr0+3
	rol		fr0+2
	rol		fr0+1
	rol		fr0
	dex
	bne		bitloop
	plp
	
	;next digit
	lda		_digit
	eor		#$09
	sta		_digit
	beq		digitloop
	
	;next quo byte
	inc		_index
	bne		digitloop
	
	;move back to fr0
	jsr		fp_fdiv_complete
	cld
ok:
	clc
	rts
err:
	sec
	rts
.endp

;==========================================================================
; SKPSPC [DBA1]	Increment CIX while INBUFF[CIX] is a space
		fixadr	$dba1
skpspc:
	lda		#' '
	ldy		cix
fp_skipchar:
skpspc_loop:
	cmp		(inbuff),y
	bne		skpspc_xit
	iny
	bne		skpspc_loop
skpspc_xit:
	sty		cix
	rts

;==========================================================================
; ISDIGT [DBAF]	Check if INBUFF[CIX] is a digit (UNDOCUMENTED)
		fixadr	$dbaf
isdigt = _isdigt
.proc _isdigt
	ldy		cix
.def :fp_isdigit_y = *
	lda		(inbuff),y
	sec
	sbc		#'0'
	cmp		#10
	rts
.endp

;==========================================================================
.proc fp_fdiv_decloop
decloop:
	;increment quotient mantissa byte
	lda		fdiv._digit
	ldx		fdiv._index
uploop:
	adc		fr2+7,x
	sta		fr2+7,x
	lda		#0
	dex
	bcs		uploop

	;subtract mantissas
	sec
	lda		fr0+5
	sbc		fr1+5
	sta		fr0+5
	lda		fr0+4
	sbc		fr1+4
	sta		fr0+4
	lda		fr0+3
	sbc		fr1+3
	sta		fr0+3
	lda		fr0+2
	sbc		fr1+2
	sta		fr0+2
	lda		fr0+1
	sbc		fr1+1
	sta		fr0+1
	lda		fr0
	sbc		#0
	sta		fr0

	;keep going until we underflow
	bcs		decloop
	rts
.endp

.proc fp_fdiv_complete
	ldx		#fr2-1
	ldy		_fr3
	lda		fr2
	bne		no_normstep
	inx
	dey
no_normstep:
	sty		0,x
	jmp		fld0r_zp
.endp

;==========================================================================
; NORMALIZE [DC00]	Normalize FR0 (UNDOCUMENTED)
		fixadr	$dc00-1
fp_normalize_cld:
	cld
	ckaddr	$dc00
fp_normalize:
.nowarn .proc normalize
	ldy		#5
normloop:
	lda		fr0
	and		#$7f
	beq		underflow2
	
	ldx		fr0+1
	beq		need_norm

	;Okay, we're done normalizing... check if the exponent is in bounds.
	;It needs to be within +/-48 to be valid. If the exponent is <-49,
	;we set it to zero; otherwise, we mark overflow.
	
	cmp		#64-49
	bcc		underflow
	cmp		#64+49
	rts
	
need_norm:
	dec		fr0
	ldx		#-5
normloop2:
	mva		fr0+7,x fr0+6,x
	inx
	bne		normloop2
	stx		fr0+6
	dey
	bne		normloop
	
	;Hmm, we shifted out everything... must be zero; reset exponent. This
	;is critical since Atari Basic depends on the exponent being zero for
	;a zero result.
	sty		fr0
	sty		fr0+1
xit:
	clc
	rts
	
underflow2:
	clc
underflow:
	jmp		zfr0
	
.endp

;==========================================================================
; HELPER ROUTINES
;==========================================================================

.proc fp_fdiv_init
	sta		_fr3

	ldx		#fr2
	jsr		zf1
	lda		#$50
	sta		fr2+6
	
	ldx		#0
	stx		fr0
	stx		fr1
	
	;check if dividend begins with a leading zero digit -- if so, shift it left 4
	;and begin with the tens digit
	lda		fr1+1
	cmp		#$10
	bcs		start_with_ones

	ldy		#4
bitloop:
	asl		fr1+5
	rol		fr1+4
	rol		fr1+3
	rol		fr1+2
	rol		fr1+1
	dey
	bne		bitloop

	ldx		#$09
	
start_with_ones:

	stx		fdiv._digit
	sed

	ldx		#0-7
	stx		fdiv._index
	sec
	rts
.endp

;--------------------------------------------------------------------------
.proc fp_fsub_cont
	;check if we had a borrow
	bcs		sub_xit
	bcc		borrow_loop_start

	;propagate borrow up
borrow_loop:
	lda		fr0+1,x
	sbc		#0
	sta		fr0+1,x
	bcs		sub_xit
borrow_loop_start:
	dex
	bpl		borrow_loop

	ldx		#5
	sec
diff_borrow:
	lda		#0
	sbc		fr0,x
	sta		fr0,x
	dex
	bne		diff_borrow
	lda		#$80
	eor		fr0
	sta		fr0
sub_xit:

norm_loop:
	;Check if the exponent is in bounds.
	;It needs to be within +/-48 to be valid. If the exponent is <-49,
	;we set it to zero. Overflow isn't possible as this is the mantissa
	;subtraction path.
	lda		fr0
	and		#$7f
	cmp		#64-49
	bcc		underflow
	
	ldx		fr0+1
	beq		need_norm

	;check if we need to round, i.e.:
	; 2.00000000
	;-0.000000005
	;load rounding byte offset
	ldx		fr1
	cpx		#4
	bcs		no_round
	lda		fr1+2,x
	cmp		#$50
	bcs		round_up
no_round:

	clc
	cld
	rts
	
need_norm:
	ldx		#-4
scan_loop:
	dec		fr0
	ldy		fr0+6,x
	bne		found_pos
	inx
	bne		scan_loop
	
	;hmm... mantissa is all zero.
underflow2:
	clc
underflow:
	cld
	jmp		zfr0
	
found_pos:
	;shift up mantissa
	ldy		#0
shift_loop:
	mva		fr0+6,x fr0+1,y
	iny
	inx
	bne		shift_loop
	
	;clear remaining mantissa bytes
clear_loop:
	stx		fr0+1,y+
	cpy		#6
	bne		clear_loop
	
	;check if we need to round
	
	
	;if not, loop back to check the exponent and exit
;	bcc		norm_loop
	beq		norm_loop
	
round_up:
	;jump back into fadd code to carry up and exit
	ldx		#5
	jmp		fsub.sum_carryloop
.endp

;--------------------------------------------------------------------------
.proc fp_fmul_innerloop
_offset = _fr3+5
_offset2 = fr2

	jsr		zfl

	;set up for 7 bits per digit pair (0-99 in 0-127)
	ldy		#7

	;set rounding byte, assuming renormalize needed (fr0+2 through fr0+6)
	lda		#$50
	sta		fr0+7

	;begin outer loop -- this is where we process one _bit_ out of each
	;multiplier byte in FR2's mantissa (note that this is inverted in that
	;it is bytes-in-bits instead of bits-in-bytes)
offloop:

	;begin inner loop -- here we process the same bit in each multiplier
	;byte, going from byte 5 down to byte 1
	ldx		#5
offloop2:
	;shift an inverted bit out of fr1 mantissa
	lsr		fr2,x
	bcs		noadd
			
	;add fr1 to fr0 at offset	
	.rept 6
		lda		fr0+(5-#),x
		adc		fr1+(5-#)
		sta		fr0+(5-#),x
	.endr
	
	;check if we have a carry out to the upper bytes
	bcc		no_carry
	stx		_offset2
	jsr		fp_fmul_carryup.dec_entry
	ldx		_offset2
no_carry:
	
noadd:
	;go back for next byte
	dex
	bne		offloop2

	;double fr1
	clc
	lda		fr1+5
	adc		fr1+5
	sta		fr1+5
	lda		fr1+4
	adc		fr1+4
	sta		fr1+4
	lda		fr1+3
	adc		fr1+3
	sta		fr1+3
	lda		fr1+2
	adc		fr1+2
	sta		fr1+2
	lda		fr1+1
	adc		fr1+1
	sta		fr1+1
	lda		fr1+0
	adc		fr1+0
	sta		fr1+0

	;loop back until all mantissa bytes finished
	dey
	bne		offloop
	
	;check if no renormalize is needed, and if so, re-add new rounding
	lda		fr0+1
	beq		renorm_needed

	lda		#$50
	ldx		#6
	jsr		fp_fmul_carryup

renorm_needed:
	;all done
	jmp		fp_normalize_cld
.endp

;==========================================================================
; PLYEVL [DD40]	Eval polynomial at (X:Y) with A coefficients using FR0
;
		fixadr	$dd3e
fp_plyevl_10:
	lda		#10
.nowarn .proc plyevl
	;stash arguments
	stx		fptr2
	sty		fptr2+1
	sta		_fpcocnt
	
	;copy FR0 -> PLYARG
	ldx		#<plyarg
	ldy		#>plyarg
	jsr		fst0r
	
	jsr		zfr0
	
loop:
	;load next coefficient and increment coptr
	lda		fptr2
	tax
	clc
	adc		#6
	sta		fptr2
	ldy		fptr2+1
	scc:inc	fptr2+1
	jsr		fld1r

	;add coefficient to acc
	jsr		fadd
	bcs		xit

	dec		_fpcocnt
	beq		xit
	
	;copy PLYARG -> FR1
	;multiply accumulator by Z and continue
	ldx		#<plyarg
	ldy		#>plyarg	
	jsr		fp_fld1r_fmul
	bcc		loop
xit:
	rts
.endp

;==========================================================================
.proc fp_swap
	ldx		#5
swaploop:
	lda		fr0,x
	ldy		fr1,x
	sta		fr1,x
	sty		fr0,x
	dex
	bpl		swaploop
	rts
.endp

;==========================================================================
; FLD0R [DD89]	Load FR0 from (X:Y)
; FLD0P [DD8D]	Load FR0 from (FLPTR)
;
	fixadr	$dd87
fld0r_zp:
	ldy		#0
	ckaddr	$dd89
fld0r:
	stx		flptr
	sty		flptr+1
	ckaddr	$dd8d
fld0p:
	ldy		#5
fld0ploop:
	lda		(flptr),y
	sta		fr0,y
	dey
	bpl		fld0ploop
	rts

;==========================================================================
; FLD1R [DD98]	Load FR1 from (X:Y)
; FLD1P [DD9C]	Load FR1 from (FLPTR)
;
	fixadr	$dd98
fld1r:
	stx		flptr
	sty		flptr+1
	ckaddr	$dd9c
fld1p:
	ldy		#5
fld1ploop:
	lda		(flptr),y
	sta		fr1,y
	dey
	bpl		fld1ploop
	rts

;==========================================================================
; FST0R [DDA7]	Store FR0 to (X:Y)
; FST0P [DDAB]	Store FR0 to (FLPTR)
;
	fixadr	$dda7
fst0r:
	stx		flptr
	sty		flptr+1
	ckaddr	$ddab
fst0p:
	ldy		#5
fst0ploop:
	lda		fr0,y
	sta		(flptr),y
	dey
	bpl		fst0ploop
	rts

;==========================================================================
; FMOVE [DDB6]	Move FR0 to FR1
;
	fixadr	$ddb6
fmove:
	ldx		#5
fmoveloop:
	lda		fr0,x
	sta		fr1,x
	dex
	bpl		fmoveloop
	rts

;==========================================================================
; EXP [DDC0]	Compute e^x
; EXP10 [DDCC]	Compute 10^x
;
	fixadr	$ddc0
exp10 = exp._exp10
.proc exp
	ldx		#<fpconst_log10_e
	ldy		#>fpconst_log10_e
	jsr		fld1r		;we could use fp_fld1r_fmul, but then we have a hole :(
	jsr		fmul
	bcs		err2

	ckaddr	$ddcc
_exp10:
	;stash sign and compute abs
	lda		fr0
	sta		_fptemp1
	and		#$7f
	sta		fr0

	ldy		#0
	
	;check for |exp| >= 100 which would guarantee over/underflow
	cmp		#$40
	bcc		abs_ok
	beq		abs_large

abs_too_big:
	;okay, the |x| is too big... check if the original was negative.
	;if so, zero and exit, otherwise error.
	lda		_fptemp1
	bpl		err2
	clc
	jmp		zfr0

abs_large:	
	;|exp|>=1, so split it into integer/fraction
	lda		fr0+1
	jsr		fp_dectobin
	adc		fp_dectobin_tab,y
	pha
	lda		#0
	sta		fr0+1
	sta		fr0+6
	jsr		fp_normalize
	pla
	tay
			
abs_ok:
	;stash integer portion of exponent
	sty		_fptemp0
		
	;compute approximation z = 10^y
	ldx		#<coeff
	ldy		#>coeff
	jsr		fp_plyevl_10
	
	;tweak exponent
	lsr		_fptemp0
	
	;scale by 10 if necessary
	bcc		even
	ldx		#<fpconst_ten
	jsr		fp_fld1r_const_fmul
	bcs		abs_too_big
even:

	;bias exponent
	lda		_fptemp0
	adc		fr0
	cmp		#64+49
	bcs		abs_too_big
	sta		fr0
	
	;check if we should invert
	rol		_fptemp1
	bcc		xit2
	
	jsr		fmove
	ldx		#<fp_one
	ldy		#>fp_one
	jsr		fld0r
	jmp		fdiv

err2:
xit2:
	rts
	
coeff:		;Minimax polynomial for 10^x over 0 <= x < 1
	.fl		 0.0146908308
	.fl		-0.002005331171
	.fl		 0.0919452045
	.fl		 0.1921383884
	.fl		 0.5447325197
	.fl		 1.17018250
	.fl		 2.03478581
	.fl		 2.65094494
	.fl		 2.30258512
	.fl		 1
.endp	

;==========================================================================
fpconst_log10_e:
	.fl		0.43429448190325182765112891891661

.proc fp_carry_expup
	;adjust exponent
	inc		fr0

	;shift down FR0
	ldx		#4
sum_shiftloop:
	lda		fr0,x
	sta		fr0+1,x
	dex
	bne		sum_shiftloop
	
	;add a $01 at the top
	inx
	stx		fr0+1
	rts
.endp

;==========================================================================
.proc fp_fmul_fr0_to_binfr2		;$15 bytes
	ldx		#4
loop:
	lda		fr0+1,x
	lsr
	lsr
	lsr
	lsr
	tay
	clc
	lda		fr0+1,x
	adc		fp_dectobin_tab,y
	eor		#$ff
	sta		fr2+1,x
	dex
	bpl		loop
.def :fp_rts1
	rts
.endp

;==========================================================================
; REDRNG [DE95]	Reduce range via y = (x-C)/(x+C) (undocumented)
;
; X:Y = pointer to C argument
;
	fixadr	$de95
redrng = _redrng
.proc _redrng
	stx		fptr2
	sty		fptr2+1
	jsr		fld1r
	ldx		#<fpscr
	ldy		#>fpscr
	jsr		fst0r
	jsr		fadd
	bcs		fail
	ldx		#<plyarg
	ldy		#>plyarg
	jsr		fst0r
	ldx		#<fpscr
	ldy		#>fpscr
	jsr		fld0r
	ldx		fptr2
	ldy		fptr2+1
	jsr		fld1r
	jsr		fsub
	bcs		fail
	ldx		#<plyarg
	ldy		#>plyarg
	jsr		fld1r
	jmp		fdiv
	
fail = fp_rts1
.endp

;==========================================================================
; LOG [DECD]	Compute ln x
; LOG10 [DED1]	Compute log10 x
;
	fixadr	$decd
log10 = log._log10
.proc log
	lsr		_fptemp1
	bpl		entry
	ckaddr	$ded1
_log10:
	sec
	ror		_fptemp1
entry:
	;throw error on negative number
	lda		fr0
	bmi		err
	
	;stash exponentx2 - 128
	asl
	eor		#$80
	sta		_fptemp0
	
	;raise error if argument is zero
	lda		fr0+1
	beq		err
	
	;reset exponent so we are in 1 <= z < 100
	ldx		#$40
	stx		fr0
	
	;split into three ranges based on mantissa:
	;  1/sqrt(10) <= x < 1:            [31, 99] divide by 100
	;  sqrt(10)/100 <= x < 1/sqrt(10): [ 3, 30] divide by 10
	;  0 < x < sqrt(10)/100:           [ 1,  2] leave as-is
	
	cmp		#$03
	bcc		post_range_adjust
	cmp		#$31
	bcc		mid_range

	;increase result by 1 (equivalent to *10 input)
	inc		_fptemp0
	bne		adjust_exponent
	
mid_range:
	;multiply by 10
	ldx		#<fpconst_ten
	jsr		fp_fld1r_const_fmul
	bcs		err2

adjust_exponent:
	;increase result by 1 (equivalent to *10 input)
	inc		_fptemp0
	
	;divide fraction by 100
	dec		fr0
	
post_range_adjust:
	;at this point, we have 0.30 <= z <= 3; apply y = (z-1)/(z+1) transform
	;so we can use a faster converging series... this reduces y to
	;0 <= y < 0.81
	ldx		#<fp_one
	ldy		#>fp_one
	jsr		redrng
	
	;stash y so we can later multiply it back in
	ldx		#<fpscr
	ldy		#>fpscr
	jsr		fst0r
	
	;square the value so we compute a series on y^2n
	jsr		fmove
	jsr		fmul
	
	;do polynomial expansion
	ldx		#<fpconst_log10coeff
	ldy		#>fpconst_log10coeff
	jsr		fp_plyevl_10
	bcs		err2
	
	;multiply back in so we have series on y^(2n+1)
	ldx		#<fpscr
	ldy		#>fpscr
	jsr		fp_fld1r_fmul
	
	;stash
	jsr		fmove
	
	;convert exponent adjustment back to float (signed)
	lda		#0
	sta		fr0+1
	ldx		_fptemp0
	bpl		expadj_positive
	sec
	sbc		_fptemp0
	tax
expadj_positive:
	stx		fr0
	jsr		ipf
	
	;merge (cannot fail)
	asl		fr0
	asl		_fptemp0
	ror		fr0
	jsr		fadd
	
	;scale if doing log
	bit		_fptemp1
	bmi		xit2
	
	ldx		#<fpconst_ln10
	jmp		fp_fld1r_const_fmul

err:
	sec
xit2:
err2:
	rts
.endp

;==========================================================================
.proc fp_tab_lo_1000
	:10 dta <[1000*#]
.endp

.proc fp_tab_hi_1000
	:10 dta >[1000*#]
.endp

.proc fp_tab_hi_100
	:10 dta >[100*#]
.endp

.proc fp_tab_hi_10000
	:6 dta >[10000*[#+1]]
.endp

;==========================================================================
; HALF (used by Atari BASIC)
;
	fixadr	$df6c
fpconst_half:
	.fl		0.5
	
;==========================================================================
; log10(x) coefficients
;
; LOG10 computes:
;							-0.30 <= z <= 3.0
;	y = (z-1)/(z+1)			-0.54 <= y <= 0.5
;	x = y^2					0 <= x <= 0.29
;	log10(z) = f(x)*y
;
; Therefore:
;	f(x) = log10((1+y)/(1-y))/y
;
fpconst_log10coeff:		;Maclaurin series expansion for log10((z-1)/(z+1))
	.fl		 0.2026227154
	.fl		-0.0732044921
	.fl		 0.1060983564
	.fl		 0.0560417329
	.fl		 0.0804188407
	.fl		 0.0963916015
	.fl		 0.1240896135
	.fl		 0.1737176646
	.fl		 0.2895296558
	.fl		 0.8685889638

;==========================================================================
; Arctangent coefficients
;
; The 11 coefficients here form a power series approximation
; f(x^2) ~= atn(x)/x. This is not an official feature of the math pack but
; is relied upon by BASIC.
;
; We used to use the coefficients from Abramowitz & Stegun 4.4.49 here, but
; there seems to be an error there such that the result falls far short
; of the specified 2x10^-8 accuracy over 0<=x<=1 at x=1. Instead, we now
; use a custom minimax polynomial for f(y)=atn(sqrt(y))/sqrt(y) where y=x^2.
;
	fixadr	$dfae
atncoef:	;coefficients for atn(x)/x ~= f(x^2)
			;see Abramowitz & Stegun 4.4.49
		
	.fl		 0.001112075881		;x**10*1.11207588057982e-3
	.fl		-0.007304087520		;x**9*-7.30408751951452e-3
	.fl		 0.0224965573		;x**8*2.24965572957342e-2
	.fl		-0.0446185172		;x**7*-4.46185172165888e-2
	.fl		 0.0673463245		;x**6*6.73463245104305e-2
	.fl		-0.0880690664		;x**5*-8.80690663570546e-2
	.fl		 0.1105667499		;x**4*1.10566749879313e-1
	.fl		-0.1427949312		;x**3*-1.42794931245212e-1
	.fl		 0.1999963060		;x**2*1.99996306023439e-1
	.fl		-0.3333332472		;x**1*-3.33333247188074e-1
									;x**0*9.99999999667198e-1
fp_one:
	.fl		1.0				;also an arctan coeff
	fixadr	$dff0
fp_pi4:	;pi/4 - needed by Atari Basic ATN()
	.fl		0.78539816339744830961566084581988
	
fp_dectobin_tab:
	:10 dta	<[-6*#]
	
	ckaddr	$e000
