#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

typedef long (SLJIT_FUNC *func3_t)(long a, long b, long c);

/*
  This example, we generate a function like this:

long func(long a, long b, long c)
{
	if ((a & 1) == 0) 
		return c;
	return b;
}

 */
static int branch(long a, long b, long c)
{
	void *code;
	unsigned long len;
	func3_t func;

	struct sljit_jump *ret_c;
	struct sljit_jump *out;

	/* Create a SLJIT compiler */
	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);

	/* 3 arg, 1 temp reg, 3 save reg */
	sljit_emit_enter(C, 0, SLJIT_ARGS3(W, W, W, W), 1, 3, 0, 0, 0);

	/* R0 = a & 1, S0 is argument a */
	sljit_emit_op2(C, SLJIT_AND, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);

	/* if R0 == 0 then jump to ret_c, where is ret_c? we assign it later */
	ret_c = sljit_emit_cmp(C, SLJIT_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 0);

	/* R0 = b, S1 is argument b */
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_S1, 0);

	/* jump to out */
	out = sljit_emit_jump(C, SLJIT_JUMP);

	/* here is the 'ret_c' should jump, we emit a label and set it to ret_c */
	sljit_set_label(ret_c, sljit_emit_label(C));

	/* R0 = c, S2 is argument c */
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_S2, 0);

	/* here is the 'out' should jump */
	sljit_set_label(out, sljit_emit_label(C));

	/* end of function */
	sljit_emit_return(C, SLJIT_MOV, SLJIT_RETURN_REG, 0);

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
	return branch(4, 5, 6);
}
