#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

typedef long (SLJIT_FUNC *func3_t)(long a, long b, long c);

static long SLJIT_FUNC print_num(long a)
{
	printf("a = %ld\n", a);
	return a + 1;
}

/*
  This example, we generate a function like this:

long func(long a, long b, long c)
{
	if ((a & 1) == 0) 
		return print_num(c);
	return print_num(b);
}
*/

static int func_call(long a, long b, long c)
{
	void *code;
	unsigned long len;
	func3_t func;

	struct sljit_jump *out;
	struct sljit_jump *print_c;

	/* Create a SLJIT compiler */
	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);

	sljit_emit_enter(C, 0, SLJIT_ARGS3(W, W, W, W), 3, 3, 0, 0, 0);

	/*  a & 1 --> R0 */
	sljit_emit_op2(C, SLJIT_AND, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
	/* R0 == 0 --> jump print_c */
	print_c = sljit_emit_cmp(C, SLJIT_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 0);

	/* R0 = S1; print_num(R0) */
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S1, 0);
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));

	/* jump out */
	out = sljit_emit_jump(C, SLJIT_JUMP);
	/* print_c: */
	sljit_set_label(print_c, sljit_emit_label(C));

	/* R0 = c; print_num(R0); */
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));

	/* out: */
	sljit_set_label(out, sljit_emit_label(C));
	sljit_emit_return(C, SLJIT_MOV, SLJIT_R0, 0);

	/* Generate machine code */
	code = sljit_generate_code(C);
	len = sljit_get_generated_code_size(C);

	/* Execute code */
	func = (func3_t)code;
	printf("func return %ld\n", func(a, b, c));

	/* dump_code(code, len); */

	/* Clean up */
	sljit_free_compiler(C);
	sljit_free_code(code, NULL);
	return 0;
}

int main()
{
	return func_call(4, 5, 6);
}
