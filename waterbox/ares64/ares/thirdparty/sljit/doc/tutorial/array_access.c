#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

typedef long (SLJIT_FUNC *func_arr_t)(long *arr, long narr);

static long SLJIT_FUNC print_num(long a)
{
	printf("num = %ld\n", a);
	return a + 1;
}

/*
  This example, we generate a function like this:

long func(long *array, long narray)
{
	long i;
	for (i = 0; i < narray; ++i)
		print_num(array[i]);
	return narray;
}

*/

static int array_access(long *arr, long narr)
{
	void *code;
	unsigned long len;
	func_arr_t func;

	/* Create a SLJIT compiler */
	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);

	sljit_emit_enter(C, 0, SLJIT_ARGS2(W, P, W),  1, 3, 0, 0, 0);
	/*                  opt arg R  S  FR FS local_size */
	sljit_emit_op2(C, SLJIT_XOR, SLJIT_S2, 0, SLJIT_S2, 0, SLJIT_S2, 0);				// S2 = 0
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, narr);                         // S1 = narr
	struct sljit_label *loopstart = sljit_emit_label(C);								// loopstart:
	struct sljit_jump *out = sljit_emit_cmp(C, SLJIT_GREATER_EQUAL, SLJIT_S2, 0, SLJIT_S1, 0);	// S2 >= a --> jump out

	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_S2), SLJIT_WORD_SHIFT);// R0 = (long *)S0[S2];
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));					// print_num(R0);

	sljit_emit_op2(C, SLJIT_ADD, SLJIT_S2, 0, SLJIT_S2, 0, SLJIT_IMM, 1);						// S2 += 1
	sljit_set_label(sljit_emit_jump(C, SLJIT_JUMP), loopstart);									// jump loopstart
	sljit_set_label(out, sljit_emit_label(C));											// out:
	sljit_emit_return(C, SLJIT_MOV, SLJIT_S1, 0);										// return RET

	/* Generate machine code */
	code = sljit_generate_code(C);
	len = sljit_get_generated_code_size(C);

	/* Execute code */
	func = (func_arr_t)code;
	printf("func return %ld\n", func(arr, narr));

	/* dump_code(code, len); */

	/* Clean up */
	sljit_free_compiler(C);
	sljit_free_code(code, NULL);
	return 0;
}

int main()
{
	long arr[8] = { 3, -10, 4, 6, 8, 12, 2000, 0 };
	return array_access(arr, 8);
}
