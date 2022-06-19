#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

struct point_st {
	long x;
	int y;
	short z;
	char d;
};

typedef long (SLJIT_FUNC *point_func_t)(struct point_st *point);;

static long SLJIT_FUNC print_num(long a)
{
	printf("a = %ld\n", a);
	return a + 1;
}

/*
  This example, we generate a function like this:

long func(struct point_st *point)
{
	print_num(point->x);
	print_num(point->y);
	print_num(point->z);
	print_num(point->d);
	return point->x;
}
*/

static int struct_access()
{
	void *code;
	unsigned long len;
	point_func_t func;

	struct point_st point = {
		-5, -20, 5, 'a'
	};

	/* Create a SLJIT compiler */
	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);

	sljit_emit_enter(C, 0, SLJIT_ARGS1(W, W), 1, 1, 0, 0, 0);
	/*                  opt arg R  S  FR FS local_size */

	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct point_st, x));	// S0->x --> R0
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, P), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));								// print_num(R0);

	sljit_emit_op1(C, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct point_st, y));	// S0->y --> R0
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, P), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));								// print_num(R0);

	sljit_emit_op1(C, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct point_st, z));	// S0->z --> R0
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, P), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));								// print_num(R0);

	sljit_emit_op1(C, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct point_st, d));	// S0->d --> R0
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, P), SLJIT_IMM, SLJIT_FUNC_ADDR(print_num));								// print_num(R0);

	sljit_emit_return(C, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct point_st, x));				// return S0->x

	/* Generate machine code */
	code = sljit_generate_code(C);
	len = sljit_get_generated_code_size(C);

	/* Execute code */
	func = (point_func_t)code;
	printf("func return %ld\n", func(&point));

	/* dump_code(code, len); */

	/* Clean up */
	sljit_free_compiler(C);
	sljit_free_code(code, NULL);
	return 0;
}

int main()
{
	return struct_access();
}
