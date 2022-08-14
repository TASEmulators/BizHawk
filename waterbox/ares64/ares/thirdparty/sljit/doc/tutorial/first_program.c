#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

typedef long (SLJIT_FUNC *func3_t)(long a, long b, long c);

static int add3(long a, long b, long c)
{
	void *code;
	unsigned long len;
	func3_t func;

	/* Create a SLJIT compiler */
	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);

	/* Start a context(function entry), have 3 arguments, discuss later */
	sljit_emit_enter(C, 0, SLJIT_ARGS3(W, W, W, W), 1, 3, 0, 0, 0);

	/* The first arguments of function is register SLJIT_S0, 2nd, SLJIT_S1, etc.  */
	/* R0 = first */
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);

	/* R0 = R0 + second */
	sljit_emit_op2(C, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S1, 0);

	/* R0 = R0 + third */
	sljit_emit_op2(C, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S2, 0);

	/* This statement mov R0 to RETURN REG and return */
	/* in fact, R0 is RETURN REG itself */
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
	return add3(4, 5, 6);
}
