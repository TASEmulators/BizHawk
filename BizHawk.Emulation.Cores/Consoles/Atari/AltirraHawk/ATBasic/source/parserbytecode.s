; Altirra BASIC - Parser bytecode program module
; Copyright (C) 2014 Avery Lee, All Rights Reserved.
;
; Copying and distribution of this file, with or without modification,
; are permitted in any medium without royalty provided the copyright
; notice and this notice are preserved.  This file is offered as-is,
; without any warranty.

?parser_program_start = *

.macro _PA_STATE_OFFSET
.def ?state_offset = :1-[pa_state_start&$ff00]-1
.if ?state_offset < 0 || ?state_offset > 255
.error "Offset out of bounds: ",?state_offset," (state address: ",:1,", base address: ", pa_state_start, ")"
.endif
		dta		?state_offset
.endm

parse_state_table:
		_PA_STATE_OFFSET		pa_state0
		_PA_STATE_OFFSET		pa_state1
		_PA_STATE_OFFSET		pa_expr
		_PA_STATE_OFFSET		pa_aexpr.entry
		_PA_STATE_OFFSET		pa_sexpr
		_PA_STATE_OFFSET		pa_avar
		_PA_STATE_OFFSET		pa_iocb
		_PA_STATE_OFFSET		pa_array
		_PA_STATE_OFFSET		pa_array2
		_PA_STATE_OFFSET		pa_comma
		_PA_STATE_OFFSET		pa_aexpr_comma
		_PA_STATE_OFFSET		pa_let
		_PA_STATE_OFFSET		pa_openfun
		_PA_STATE_OFFSET		pa_aexpr_next

.macro _PA_STATEMENT_OFFSET
.def ?statement_offset = :1-[pa_statements_begin&$ff00]-1
.if ?statement_offset < 0 || ?statement_offset > 255
.error "Offset out of bounds: ",?statement_offset," (statement address: ",:1,", base address: ", pa_statements_begin, ")"
.endif
		dta		?statement_offset
.endm

		;statements
parse_state_table_statements:
		_PA_STATEMENT_OFFSET		pa_state_rem
		_PA_STATEMENT_OFFSET		pa_state_data
		_PA_STATEMENT_OFFSET		pa_state_input
		_PA_STATEMENT_OFFSET		pa_state_color
		_PA_STATEMENT_OFFSET		pa_state_list
		_PA_STATEMENT_OFFSET		pa_state_enter
		_PA_STATEMENT_OFFSET		pa_state_let
		_PA_STATEMENT_OFFSET		pa_state_if
		_PA_STATEMENT_OFFSET		pa_state_for
		_PA_STATEMENT_OFFSET		pa_state_next
		_PA_STATEMENT_OFFSET		pa_state_goto
		_PA_STATEMENT_OFFSET		pa_state_goto2
		_PA_STATEMENT_OFFSET		pa_state_gosub
		_PA_STATEMENT_OFFSET		pa_state_trap
		_PA_STATEMENT_OFFSET		pa_state_bye
		_PA_STATEMENT_OFFSET		pa_state_cont
		_PA_STATEMENT_OFFSET		pa_state_com
		_PA_STATEMENT_OFFSET		pa_state_close
		_PA_STATEMENT_OFFSET		pa_state_clr
		_PA_STATEMENT_OFFSET		pa_state_deg
		_PA_STATEMENT_OFFSET		pa_state_dim
		_PA_STATEMENT_OFFSET		pa_state_end
		_PA_STATEMENT_OFFSET		pa_state_new
		_PA_STATEMENT_OFFSET		pa_state_open
		_PA_STATEMENT_OFFSET		pa_state_load
		_PA_STATEMENT_OFFSET		pa_state_save
		_PA_STATEMENT_OFFSET		pa_state_status
		_PA_STATEMENT_OFFSET		pa_state_note
		_PA_STATEMENT_OFFSET		pa_state_point
		_PA_STATEMENT_OFFSET		pa_state_xio
		_PA_STATEMENT_OFFSET		pa_state_on
		_PA_STATEMENT_OFFSET		pa_state_poke
		_PA_STATEMENT_OFFSET		pa_state_print
		_PA_STATEMENT_OFFSET		pa_state_rad
		_PA_STATEMENT_OFFSET		pa_state_read
		_PA_STATEMENT_OFFSET		pa_state_restore
		_PA_STATEMENT_OFFSET		pa_state_return
		_PA_STATEMENT_OFFSET		pa_state_run
		_PA_STATEMENT_OFFSET		pa_state_stop
		_PA_STATEMENT_OFFSET		pa_state_pop
		_PA_STATEMENT_OFFSET		pa_state_print
		_PA_STATEMENT_OFFSET		pa_state_get
		_PA_STATEMENT_OFFSET		pa_state_put
		_PA_STATEMENT_OFFSET		pa_state_graphics
		_PA_STATEMENT_OFFSET		pa_state_plot
		_PA_STATEMENT_OFFSET		pa_state_position
		_PA_STATEMENT_OFFSET		pa_state_dos
		_PA_STATEMENT_OFFSET		pa_state_drawto
		_PA_STATEMENT_OFFSET		pa_state_setcolor
		_PA_STATEMENT_OFFSET		pa_state_locate
		_PA_STATEMENT_OFFSET		pa_state_sound
		_PA_STATEMENT_OFFSET		pa_state_lprint
		_PA_STATEMENT_OFFSET		pa_state_csave
		_PA_STATEMENT_OFFSET		pa_state_cload
		dta		0					;implicit let
		dta		0					;syntax error
		dta		0					;WHILE
		dta		0					;ENDWHILE
		dta		0					;TRACEOFF
		dta		0					;TRACE
		_PA_STATEMENT_OFFSET		pa_state_else
		_PA_STATEMENT_OFFSET		pa_state_endif
		_PA_STATEMENT_OFFSET		pa_state_dpoke
		_PA_STATEMENT_OFFSET		pa_state_lomem

		;$40
		dta		0					;DEL
		dta		0					;RPUT
		dta		0					;RGET
		_PA_STATEMENT_OFFSET		pa_state_bput
		_PA_STATEMENT_OFFSET		pa_state_bget
		dta		0					;TAB
		_PA_STATEMENT_OFFSET		pa_state_cp
		_PA_STATEMENT_OFFSET		pa_state_erase
		_PA_STATEMENT_OFFSET		pa_state_protect
		_PA_STATEMENT_OFFSET		pa_state_unprotect
		_PA_STATEMENT_OFFSET		pa_state_dir
		_PA_STATEMENT_OFFSET		pa_state_rename
		_PA_STATEMENT_OFFSET		pa_state_move
		_PA_STATEMENT_OFFSET		pa_state_missile
		_PA_STATEMENT_OFFSET		pa_state_pmclr
		_PA_STATEMENT_OFFSET		pa_state_pmcolor

		;$50
		_PA_STATEMENT_OFFSET		pa_state_pmgraphics
		_PA_STATEMENT_OFFSET		pa_state_pmmove

.macro _PA_FUNCTION_OFFSET
.def ?function_offset = :1-[pa_functions_begin&$ff00]-1
.if ?function_offset < 0 || ?function_offset > 255
.error "Offset out of bounds: ",?function_offset," (function address: ",:1,", base address: ", pa_statements_begin, ")"
.endif
		dta		?function_offset
.endm

		;functions
parse_state_table_functions:
		_PA_FUNCTION_OFFSET		pa_state_str
		_PA_FUNCTION_OFFSET		pa_state_chr
		_PA_FUNCTION_OFFSET		pa_state_usr
		_PA_FUNCTION_OFFSET		pa_state_asc
		_PA_FUNCTION_OFFSET		pa_state_val
		_PA_FUNCTION_OFFSET		pa_state_len
		_PA_FUNCTION_OFFSET		pa_state_adr
		_PA_FUNCTION_OFFSET		pa_state_atn
		_PA_FUNCTION_OFFSET		pa_state_cos
		_PA_FUNCTION_OFFSET		pa_state_peek
		_PA_FUNCTION_OFFSET		pa_state_sin
		_PA_FUNCTION_OFFSET		pa_state_rnd
		_PA_FUNCTION_OFFSET		pa_state_fre
		_PA_FUNCTION_OFFSET		pa_state_exp
		_PA_FUNCTION_OFFSET		pa_state_log
		_PA_FUNCTION_OFFSET		pa_state_clog
		_PA_FUNCTION_OFFSET		pa_state_sqr
		_PA_FUNCTION_OFFSET		pa_state_sgn
		_PA_FUNCTION_OFFSET		pa_state_abs
		_PA_FUNCTION_OFFSET		pa_state_int
		_PA_FUNCTION_OFFSET		pa_state_paddle
		_PA_FUNCTION_OFFSET		pa_state_stick
		_PA_FUNCTION_OFFSET		pa_state_ptrig
		_PA_FUNCTION_OFFSET		pa_state_strig		
		dta			0			;USING (BASIC XL/XE)
		dta			0			;%
		dta			0			;!
		dta			0			;&
		dta			0			;; (BASIC XL/XE)
		_PA_FUNCTION_OFFSET		pa_state_bump
		dta			0			;FIND (BASIC XL/XE)
		_PA_FUNCTION_OFFSET		pa_state_hex
		dta			0			;RANDOM (BASIC XL/XE)
		_PA_FUNCTION_OFFSET		pa_state_dpeek
		dta			0			;SYS (BASIC XL/XE)
		_PA_FUNCTION_OFFSET		pa_state_vstick
		_PA_FUNCTION_OFFSET		pa_state_hstick
		_PA_FUNCTION_OFFSET		pa_state_pmadr
		_PA_FUNCTION_OFFSET		pa_state_err
		
;============================================================================
; Parser instructions
;
;	$00-1F	Parser command
;	$20-7F	Literal character match
;	$80-FF	Jump/Jsr to state (even for jump, odd for jsr)
;
.macro PA_BRANCH_TARGET
		dta		:1-(*+1)
		.if :1<*-$80||:1>*+$7f
		.error "Branch from ",*," to ",:1," out of range."
		.endif
.endm

.macro PAI_EXPECT			;Expect a character; fail if not there.
		dta		c:1
.endm

.macro PAI_SPACES			;Eat zero or more spaces.
		dta		c' '
.endm

.macro PAI_FAIL				;Fail the current line; backtrack if possible.
		dta		$00
.endm

.macro PAI_ACCEPT			;Accept the current OR clause (pop backtracking state).
		dta		$01
.endm

.macro PAI_TRYSTATEMENT		;Try to parse a statement; jump to statement state if so.
		dta		$02
.endm

.macro PAI_OR				;Push a backtracking state.
		dta		$03
		.if :1<*
		.error "PAI_OR only allows forward branches"
		.endif
		dta		:1-(*+1)
.endm

.macro PAI_EOL				;Check for end of line; fail if missing.
		dta		$04
.endm

.macro PAI_B				;Unconditional branch.
		dta		$05
		PA_BRANCH_TARGET :1
.endm

.macro PAI_BEQ				;Branch and eat character if match.
		dta		$06,:1,$00
		PA_BRANCH_TARGET :2
.endm

.macro PAI_BEQEMIT			;Branch, emit, and eat character if match.
		dta		$06,:1,:2
		PA_BRANCH_TARGET :3
.endm

.macro PAI_EMIT				;Emit a token.
		dta		$07,:1
.endm

.macro PAI_COPYLINE			;Copy remainder of line
		dta		$08
.endm

.macro PAI_RTS				;Return from subroutine.
		dta		$09
.endm

.macro PAI_TRYNUMBER		;Try to parse and emit a number; jump to target if so.
		dta		$0a
		PA_BRANCH_TARGET :1
.endm

.macro PAI_TRYVARIABLE		;Try to parse and emit a variable; jump to target if so. Implicit space skip.
		dta		$0b
		PA_BRANCH_TARGET :1
.endm

.macro PAI_TRYFUNCTION		;Try to parse and emit a variable; jump to target if so.
		dta		$0c
		PA_BRANCH_TARGET :1
.endm

.macro PAI_HEX_B			;Parse hex and then branch
		dta		$0d
		PA_BRANCH_TARGET :1
.endm

.macro PAI_STEND			;End a statement.
		dta		$0e
.endm

.macro PAI_STRING			;Parse a string literal.
		dta		$0f
.endm

.macro PAI_BSTR				;Branch if last variable was string.
		dta		$10
		PA_BRANCH_TARGET :1
.endm

.macro PAI_NUM				;Set expression type to number.
		dta		$11
.endm

.macro PAI_STR				;Set expression type to string.
		dta		$12
.endm

.macro PAI_EMIT_B			;PAI_EMIT + PAI_B
		dta		$13
		dta		:1
		PA_BRANCH_TARGET :2
.endm

.macro PAI_TRYARRAYVAR		;Try to parse and emit a array or string array variable; jump to target if so.
		dta		$14			;Must be greater than token for TRYVARIABLE! (See code)
		PA_BRANCH_TARGET :1
.endm

.macro PAI_BEOS				;Branch if end of statement
		dta		$15
		PA_BRANCH_TARGET :1
.endm

.macro PAI_ENDIF
		dta		$16
.endm

.macro PAI_JUMP				;Jump to the given state.
		dta		$80+[:1]*2
.endm

.macro PAI_JSR				;Jump to subroutine.
		dta		$81+[:1]*2
.endm

.macro PAM_EXPR
		PAI_JSR PST_EXPR
.endm

.macro PAM_AEXPR
		PAI_JSR PST_AEXPR
.endm

.macro PAM_SEXPR
		PAI_JSR PST_SEXPR
.endm

.macro PAM_COMMA
		PAI_JSR	PST_COMMA
.endm

.macro PAM_AEXPR_COMMA
		PAI_JSR	PST_AEXPR_COMMA
.endm

.macro PAM_NEXT
		PAI_JUMP PST_NEXT
.endm

.macro PAM_AVAR
		PAI_JSR PST_AVAR
.endm

.macro PAM_IOCB
		PAI_JSR PST_IOCB
.endm

.macro PAM_AEXPR_NEXT
		PAI_JUMP PST_AEXPR_NEXT
.endm

;============================================================================
PST_NEXT			= $01
PST_EXPR			= $02
PST_AEXPR			= $03
PST_SEXPR			= $04
PST_AVAR			= $05
PST_IOCB			= $06
PST_ARRAY			= $07
PST_ARRAY2			= $08
PST_COMMA			= $09
PST_AEXPR_COMMA		= $0A
PST_LET				= $0B
PST_OPENFUN			= $0C
PST_AEXPR_NEXT		= $0D

;----------------------------
.nowarn .proc pa_array_sexpr_
sarrayvar:
		PAI_SPACES
		PAI_BEQEMIT	'(', TOK_EXP_OPEN_STR, substring
sarrayvar_exit:
		PAI_STR
		PAI_RTS
		
substring:
		PAI_JSR		PST_ARRAY2
		PAI_B		sarrayvar_exit

multi:
		PAM_AEXPR
		PAI_B		term

const_string:
		PAI_STRING
done:
		PAI_RTS

is_str:
		PAI_SPACES
		PAI_BEQEMIT	'(', TOK_EXP_OPEN_STR, substring
		PAI_RTS

func:
		PAI_BSTR	done
var:
		PAI_BSTR	is_str
		PAI_FAIL

.def :pa_state_start
.def :pa_array
		PAI_BSTR	sarrayvar		
		PAI_EMIT	TOK_EXP_OPEN_ARY
.def :pa_array2
		PAM_AEXPR
		PAI_BEQEMIT	',', TOK_EXP_ARRAY_COMMA, multi
term:
		PAI_EXPECT	')'
		PAI_EMIT	TOK_EXP_CLOSEPAREN
		PAI_RTS

.def :pa_sexpr
		PAI_SPACES
		PAI_BEQEMIT		'"',TOK_EXP_CSTR, const_string
		PAI_TRYFUNCTION	func
		PAI_TRYVARIABLE var
		PAI_FAIL
.endp

;----------------------------
pa_aexpr_comma:
		PAM_AEXPR
.proc pa_comma
		PAI_SPACES
		PAI_EXPECT	','
		PAI_EMIT	TOK_EXP_COMMA
		PAI_RTS
.endp

;----------------------------
.proc pa_expr
		PAI_OR		sexpr
		PAM_AEXPR
		PAI_ACCEPT
		PAI_RTS
sexpr:
		PAM_SEXPR
		PAI_RTS
.endp

;----------------------------
.proc pa_avar
		PAI_TRYARRAYVAR fail
		PAI_TRYVARIABLE var_ok
fail:
		PAI_FAIL
var_ok:
		PAI_BSTR	fail
		PAI_RTS
.endp

;----------------------------
.proc pa_iocb
		PAI_SPACES
		PAI_EXPECT	'#'
		PAI_EMIT	TOK_EXP_HASH
		PAM_AEXPR
		PAI_RTS
.endp

;----------------------------
.proc pa_openfun
		PAI_EXPECT	'('
		PAI_EMIT	TOK_EXP_OPEN_FUN
		PAI_RTS		
.endp

;============================================================================
.proc pa_state0		;initial statement
		PAI_SPACES
		PAI_BEQ		$9B, pa_state1.eol
		PAI_B		first_statement

.def :pa_cont_statement
		PAI_STEND
first_statement:
		PAI_SPACES
		PAI_TRYSTATEMENT
		
		;assume it's an implicit let
		PAI_EMIT	TOK_ILET
.def :pa_let
		PAI_TRYARRAYVAR is_array
		PAI_TRYVARIABLE ilet_ok
		PAI_FAIL
is_array:
		PAI_JSR		PST_ARRAY
ilet_ok:
		PAI_SPACES
		PAI_EXPECT	'='
		PAI_BSTR	string_assign
		PAI_EMIT	TOK_EXP_ASSIGNNUM
.def :pa_aexpr_next
		PAM_AEXPR
		PAM_NEXT
		
string_assign:
		PAI_EMIT	TOK_EXP_ASSIGNSTR
		PAM_SEXPR
		PAM_NEXT
.endp

.proc pa_state1

		;skip spaces
		PAI_SPACES
		
		;check for continuation
		PAI_BEQEMIT	':', TOK_EOS, pa_cont_statement
		PAI_BEQEMIT $9B, TOK_EOL, eos_eol
		PAI_FAIL
eos_eol:
		PAI_STEND
eol:
		PAI_EOL
.endp

;----------------------------
.proc pa_aexpr
		;This is pretty complicated.

str_var:
		PAI_FAIL
arrayvar:
		PAI_JSR		PST_ARRAY
variable:
need_either_operator:
		PAI_BSTR	str_var
		PAI_ACCEPT
		PAI_B		need_operator

const_hex:
		PAI_HEX_B	need_operator

open_paren:
		PAM_AEXPR
		PAI_EXPECT	')'
		PAI_EMIT_B	TOK_EXP_CLOSEPAREN, need_operator

accept_need_value:
		PAI_ACCEPT
entry:
need_value:
		PAI_SPACES
		PAI_BEQEMIT	'(', TOK_EXP_OPENPAREN, open_paren
		PAI_BEQ		'$',const_hex

		;This needs to be before unary +/- since Atari BASIC only allows one unary operator,
		;but can fold +/- into a constant.
		PAI_TRYNUMBER	need_operator
		PAI_BEQEMIT	'+', TOK_EXP_UNPLUS, need_value
		PAI_BEQEMIT	'-', TOK_EXP_UNMINUS, need_value
		PAI_OR		not_not
		PAI_EXPECT	'N'
		PAI_EXPECT	'O'
		PAI_EXPECT	'T'
		PAI_ACCEPT
		PAI_EMIT_B	TOK_EXP_NOT, need_value

not_not:
		PAI_OR		svalue
		PAI_TRYFUNCTION	variable
		PAI_TRYARRAYVAR	arrayvar
		PAI_TRYVARIABLE	variable
		PAI_FAIL

op_less:
		PAI_BEQEMIT	'=', TOK_EXP_LE, need_value
		PAI_BEQEMIT	'>', TOK_EXP_NE, need_value
		PAI_EMIT_B	TOK_EXP_LT, need_value

need_svalue:
		PAM_SEXPR
need_operator:
		PAI_SPACES
		PAI_BEQEMIT	'+', TOK_EXP_ADD, need_value
		PAI_BEQEMIT	'-', TOK_EXP_SUBTRACT, need_value
		PAI_BEQEMIT	'*', TOK_EXP_MULTIPLY, need_value
		PAI_BEQEMIT	'/', TOK_EXP_DIVIDE, need_value
		PAI_BEQEMIT	'^', TOK_EXP_POWER, need_value
		PAI_BEQ		'<',op_less
		PAI_BEQ		'>',op_greater
		PAI_BEQEMIT	'=', TOK_EXP_EQUAL, need_value
		PAI_BEQEMIT	'%', TOK_EXP_BITWISE_XOR, need_value
		PAI_BEQEMIT	'!', TOK_EXP_BITWISE_OR, need_value
		PAI_BEQEMIT	'&', TOK_EXP_BITWISE_AND, need_value
		PAI_OR		not_and
		PAI_EXPECT	'A'
		PAI_EXPECT	'N'
		PAI_EXPECT	'D'
		PAI_EMIT_B	TOK_EXP_AND, accept_need_value

not_and:
		PAI_OR		not_or
		PAI_EXPECT	'O'
		PAI_EXPECT	'R'
		PAI_EMIT_B	TOK_EXP_OR, accept_need_value
not_or:
		PAI_RTS

op_greater:
		PAI_BEQEMIT	'=', TOK_EXP_GE, need_value
		PAI_EMIT_B	TOK_EXP_GT, need_value

svalue:
		PAM_SEXPR
need_soperator:
		PAI_SPACES
		PAI_BEQ		'<',op_str_l
		PAI_BEQ		'>',op_str_g
		PAI_BEQEMIT	'=', TOK_EXP_STR_EQ, need_svalue
		PAI_FAIL

op_str_l:
		PAI_BEQEMIT	'>', TOK_EXP_STR_NE, need_svalue
		PAI_BEQEMIT	'=', TOK_EXP_STR_LE, need_svalue
		PAI_EMIT_B	TOK_EXP_STR_LT, need_svalue

op_str_g:
		PAI_BEQEMIT	'=', TOK_EXP_STR_GE, need_svalue
		PAI_EMIT_B	TOK_EXP_STR_GT, need_svalue
.endp

;==========================================================================
pa_functions_begin:

;aexpr fun(aexpr)
pa_state_abs:
pa_state_atn:
pa_state_clog:
pa_state_cos:
pa_state_exp:
pa_state_fre:
pa_state_int:
pa_state_log:
pa_state_paddle:
pa_state_peek:
pa_state_ptrig:
pa_state_rnd:
pa_state_sgn:
pa_state_sin:
pa_state_sqr:
pa_state_stick:
pa_state_strig:
pa_state_dpeek:
pa_state_vstick:
pa_state_hstick:
pa_state_pmadr:
pa_state_err:
		PAI_JSR		PST_OPENFUN
pa_start_numeric_function:
		PAM_AEXPR
pa_close_numeric_function:
		PAI_NUM
		PAI_B		pa_close_function

pa_state_bump:
		PAM_AEXPR
		PAI_EXPECT	','
		PAI_EMIT_B	TOK_EXP_ARRAY_COMMA, pa_start_numeric_function

;aexpr fun(sexpr)
pa_state_adr:
pa_state_asc:
pa_state_len:
pa_state_val:
		PAI_JSR		PST_OPENFUN
		PAM_SEXPR
		PAI_B		pa_close_numeric_function

;aexpr usr(aexpr[, aexpr])
pa_state_usr:
		PAI_JSR		PST_OPENFUN
pa_state_usr_loop:
		PAM_AEXPR
		PAI_BEQEMIT	',', TOK_EXP_ARRAY_COMMA, pa_state_usr_loop
		PAI_B		pa_close_numeric_function

;sexpr fun(aexpr)
pa_state_chr:
pa_state_str:
pa_state_hex:
		PAI_JSR		PST_OPENFUN
		
		_PAGE_CHECK pa_functions_begin

		PAM_AEXPR
		PAI_STR
pa_close_function:
		PAI_EXPECT	')'
		PAI_EMIT	TOK_EXP_CLOSEPAREN
		PAI_RTS

pa_functions_end:

;==========================================================================

pa_statements_begin:

pa_state_sound:			;STATEMENT aexpr,aexpr,aexpr,aexpr
		PAM_AEXPR_COMMA
pa_state_setcolor:		;STATEMENT aexpr,aexpr,aexpr
pa_state_move:
pa_state_missile:
pa_state_pmcolor:
		PAM_AEXPR_COMMA
pa_state_drawto:		;STATEMENT aexpr,aexpr
pa_state_plot:
pa_state_poke:
pa_state_position:
pa_state_dpoke:
		PAM_AEXPR_COMMA
pa_state_color:			;STATEMENT aexpr
pa_state_goto:
pa_state_goto2:
pa_state_gosub:
pa_state_graphics:
pa_state_trap:
pa_state_lomem:
pa_state_pmclr:
pa_state_pmgraphics:
		PAM_AEXPR_NEXT

pa_state_bput:			;BPUT #iocb,aexpr,aexpr
pa_state_bget:			;BGET #iocb,aexpr,aexpr
		PAM_IOCB
		PAM_COMMA
		PAI_B			pa_state_dpoke

pa_state_close:
		PAM_IOCB
pa_state_bye:			;STATEMENT
pa_state_cload:
pa_state_clr:
pa_state_cont:
pa_state_csave:
pa_state_deg:
pa_state_dos:
pa_state_new:
pa_state_pop:
pa_state_rad:
pa_state_return:
pa_state_stop:
pa_state_cp:
pa_state_else:
pa_state_endif:			;never actually hit due to END
		PAM_NEXT

pa_state_data:
pa_state_rem:
		PAI_SPACES
		PAI_COPYLINE
		PAI_EOL

;---------------------------------------------------------------------------		
pa_state_end:
		;special code required for ENDIF
		PAI_OR		pa_state_end_normal
		PAI_EXPECT	'I'
		PAI_EXPECT	'F'
		PAI_ENDIF		;also does ACCEPT
pa_state_end_normal:
		PAM_NEXT

;---------------------------------------------------------------------------		
.proc pa_state_restore
		PAI_BEOS	pa_state_end_normal
		PAM_AEXPR_NEXT
.endp

;---------------------------------------------------------------------------		
pa_state_com = pa_state_dim
.proc pa_state_dim
next_var_2:
		PAI_SPACES
		PAI_TRYFUNCTION is_func
		PAI_TRYARRAYVAR	is_var
is_func:
		PAI_FAIL

is_var:
		PAI_SPACES
		PAI_BSTR	is_string
		PAI_EMIT	TOK_EXP_OPEN_DIMARY
		PAI_JSR		PST_ARRAY2
		PAI_B		next

is_string:
		PAI_EMIT	TOK_EXP_OPEN_DIMSTR
		PAI_EXPECT	'('
		PAM_AEXPR
		PAI_EXPECT	')'
		PAI_EMIT	TOK_EXP_CLOSEPAREN
next:
		PAI_SPACES
		PAI_BEQEMIT	',', TOK_EXP_COMMA, next_var_2
		PAM_NEXT
.endp

;--------------------------------------------------------------------------
.proc pa_state_for
		PAM_AVAR
		PAI_SPACES
		PAI_EXPECT	'='
		PAI_EMIT	TOK_EXP_ASSIGNNUM
		PAM_AEXPR
		PAI_EMIT	TOK_EXP_TO
		PAI_EXPECT	'T'
		PAI_EXPECT	'O'
		PAM_AEXPR
		PAI_BEQEMIT	'S', TOK_EXP_STEP, have_step
		PAM_NEXT
have_step:
		PAI_EXPECT	'T'
		PAI_EXPECT	'E'
		PAI_EXPECT	'P'
		PAM_AEXPR_NEXT
.endp

;--------------------------------------------------------------------------
pa_state_get:
pa_state_status:
		PAM_IOCB
		PAM_COMMA
		PAM_AVAR
pa_state_get_next:
		PAM_NEXT

;--------------------------------------------------------------------------
pa_state_put:
		PAM_IOCB
		PAM_COMMA
		PAM_AEXPR_NEXT

;--------------------------------------------------------------------------
.proc pa_state_input		
		PAI_OR		var_loop
		PAM_IOCB
		PAI_ACCEPT
		PAI_BEQEMIT	';', TOK_EXP_SEMI, var_loop
		PAM_COMMA
.def :pa_state_read = *
var_loop:
		PAI_TRYVARIABLE var_ok
		PAI_FAIL
var_ok:
		PAI_BEOS	pa_state_get_next
		PAM_COMMA
		PAI_B		var_loop
.endp

;--------------------------------------------------------------------------
.proc pa_state_if
		PAM_AEXPR
		PAI_BEOS	pa_state_get_next
		PAI_EMIT	TOK_EXP_THEN
		PAI_EXPECT	'T'
		PAI_EXPECT	'H'
		PAI_EXPECT	'E'
		PAI_EXPECT	'N'
		PAI_TRYNUMBER	pa_state_get_next
		PAI_STEND			;must end statement without EOS
		PAI_JUMP	0
.endp

;--------------------------------------------------------------------------
; LIST filename[,lineno[,lineno]]
; LIST [lineno[,lineno]]
.proc pa_state_list
		PAI_BEOS	pa_state_get_next
		PAI_OR		no_filespec
		PAM_SEXPR
		PAI_ACCEPT
		PAI_BEOS	pa_state_get_next
		PAM_COMMA
no_filespec:
		PAM_AEXPR
		PAI_BEOS	pa_state_get_next
		PAM_COMMA
		PAM_AEXPR_NEXT
.endp

;--------------------------------------------------------------------------
; We need this trampoline since states and statements are in different
; pages. It costs a byte.
pa_state_let:
		PAI_JUMP	PST_LET

;--------------------------------------------------------------------------
pa_state_locate:
		PAM_AEXPR_COMMA
		PAM_AEXPR_COMMA
pa_state_next:
		PAM_AVAR
		PAM_NEXT

pa_state_note:
pa_state_point:
		PAM_IOCB
		PAM_COMMA
		PAM_AVAR
		PAM_COMMA
		PAM_AVAR
		PAM_NEXT

.proc pa_state_on
		;parse conditional expression
		PAM_AEXPR
		PAI_EXPECT	'G'
		PAI_EXPECT	'O'
		PAI_BEQEMIT	'T', TOK_EXP_GOTO, is_goto
		PAI_EXPECT	'S'
		PAI_EXPECT	'U'
		PAI_EXPECT	'B'
		PAI_EMIT_B	TOK_EXP_GOSUB, linenos
		
is_goto:
		PAI_EXPECT	'O'
lineno_comma:
linenos:
		PAM_AEXPR
		PAI_BEQEMIT	',', TOK_EXP_COMMA, lineno_comma
		PAM_NEXT
.endp

pa_state_xio:
		PAM_AEXPR_COMMA
pa_state_open:
		PAM_IOCB
		PAM_COMMA
		PAM_AEXPR_COMMA
		PAM_AEXPR_COMMA
pa_state_load:
pa_state_save:
pa_state_enter:
pa_state_erase:
pa_state_protect:
pa_state_unprotect:
pa_state_rename:
		PAM_SEXPR
pa_state_print_simple:
		PAM_NEXT

;============================================================================		
.proc pa_state_pmmove
		PAM_AEXPR
		PAI_BEQEMIT	',', TOK_EXP_COMMA, horiz
try_vert:
		PAI_BEQEMIT ';', TOK_EXP_SEMI, vert
done:
		PAM_NEXT
horiz:
		PAM_AEXPR
		PAI_B		try_vert
vert:
		PAM_AEXPR_NEXT
.endp

;============================================================================		
pa_state_run:
pa_state_dir:
		PAI_BEOS	pa_state_run_2
		PAM_SEXPR
pa_state_run_2:
		PAM_NEXT

pa_state_print:
		PAI_OR		pa_state_print_item
		PAM_IOCB
		PAI_ACCEPT
		PAI_B		pa_state_print_sep
pa_state_lprint:
pa_state_print_item:
		PAI_BEOS	pa_state_print_simple

		_PAGE_CHECK pa_statements_begin

		PAI_BEQEMIT	',', TOK_EXP_COMMA, pa_state_print_item
		PAI_BEQEMIT	';', TOK_EXP_SEMI, pa_state_print_item
		PAM_EXPR
pa_state_print_sep:
		PAI_BEOS	pa_state_print_simple
		PAI_BEQEMIT	',', TOK_EXP_COMMA, pa_state_print_item
		PAI_BEQEMIT	';', TOK_EXP_SEMI, pa_state_print_item
		PAI_FAIL

pa_statements_end:

;============================================================================		
statement_table:
		dta		c'RE',c'M'+$80		;R.
		dta		c'DAT',c'A'+$80		;D.
		dta		c'INPU',c'T'+$80	;I.
		dta		c'COLO',c'R'+$80	;C.		exp
		dta		c'LIS',c'T'+$80		;L.
		dta		c'ENTE',c'R'+$80	;E.
		dta		c'LE',c'T'+$80		;LE.
		dta		c'I',c'F'+$80		;IF
		dta		c'FO',c'R'+$80		;F.
		dta		c'NEX',c'T'+$80		;N.
		dta		c'GOT',c'O'+$80		;G.
		dta		c'GO T',c'O'+$80
		dta		c'GOSU',c'B'+$80	;GOS.
		dta		c'TRA',c'P'+$80		;T.
		dta		c'BY',c'E'+$80		;B.
		dta		c'CON',c'T'+$80		;CON.
		dta		c'CO',c'M'+$80
		dta		c'CLOS',c'E'+$80	;CL.	#exp
		dta		c'CL',c'R'+$80
		dta		c'DE',c'G'+$80		;DE.
		dta		c'DI',c'M'+$80		;DI.
		dta		c'EN',c'D'+$80		;
		dta		c'NE',c'W'+$80		;
		dta		c'OPE',c'N'+$80		;O.
		dta		c'LOA',c'D'+$80		;LO.
		dta		c'SAV',c'E'+$80		;S.
		dta		c'STATU',c'S'+$80	;ST.
		dta		c'NOT',c'E'+$80		;NO.
		dta		c'POIN',c'T'+$80	;P.
		dta		c'XI',c'O'+$80		;X.
		dta		c'O',c'N'+$80		;
		dta		c'POK',c'E'+$80		;POK.
		dta		c'PRIN',c'T'+$80	;PR.
		dta		c'RA',c'D'+$80		;
		dta		c'REA',c'D'+$80		;REA.
		dta		c'RESTOR',c'E'+$80	;RES.
		dta		c'RETUR',c'N'+$80	;RET.
		dta		c'RU',c'N'+$80		;RU.
		dta		c'STO',c'P'+$80		;STO.
		dta		c'PO',c'P'+$80		;
		dta		c'?'+$80
		dta		c'GE',c'T'+$80		;GE.
		dta		c'PU',c'T'+$80		;PU.
		dta		c'GRAPHIC',c'S'+$80	;GR.
		dta		c'PLO',c'T'+$80		;PL.
		dta		c'POSITIO',c'N'+$80	;POS.
		dta		c'DO',c'S'+$80		;DO.
		dta		c'DRAWT',c'O'+$80	;DR.
		dta		c'SETCOLO',c'R'+$80	;SE.
		dta		c'LOCAT',c'E'+$80	;LOC.
		dta		c'SOUN',c'D'+$80	;SO.
		dta		c'LPRIN',c'T'+$80	;LP.
		dta		c'CSAV',c'E'+$80
		dta		c'CLOA',c'D'+$80	;CLOA.
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'ELS',c'E'+$80
		dta		c'ENDI',c'F'+$80
		dta		c'DPOK',c'E'+$80
		dta		c'LOME',c'M'+$80

		;$40
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'?'+$80
		dta		c'BPU',c'T'+$80
		dta		c'BGE',c'T'+$80
		dta		c'?'+$80
		dta		c'C',c'P'+$80
		dta		c'ERAS',c'E'+$80
		dta		c'PROTEC',c'T'+$80
		dta		c'UNPROTEC',c'T'+$80
		dta		c'DI',c'R'+$80		;
		dta		c'RENAM',c'E'+$80
		dta		c'MOV',c'E'+$80
		dta		'MISSIL','E'*
		dta		'PMCL','R'*
		dta		'PMCOLO','R'*

		;$50
		dta		'PMGRAPHIC','S'*
		dta		'PMMOV','E'*
		dta		0					;end for searching
	
.echo "-- Statement token table length: ", *-statement_table

.echo "- Parser program length: ",*-?parser_program_start
