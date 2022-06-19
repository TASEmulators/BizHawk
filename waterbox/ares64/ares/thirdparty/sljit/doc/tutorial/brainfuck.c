/*
 *    Brainfuck interpreter with SLJIT
 *
 *    Copyright 2015 Wen Xichang (wenxichang@163.com). All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 *
 *   1. Redistributions of source code must retain the above copyright notice, this list of
 *      conditions and the following disclaimer.
 *
 *   2. Redistributions in binary form must reproduce the above copyright notice, this list
 *      of conditions and the following disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER(S) AND CONTRIBUTORS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
 * SHALL THE COPYRIGHT HOLDER(S) OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
 * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
 * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>

#define BF_CELL_SIZE		3000
#define BF_LOOP_LEVEL		256

static int readvalid(FILE *src)
{
	int chr;

	while ((chr = fgetc(src)) != EOF) {
		switch (chr) {
		case '+':
		case '-':
		case '>':
		case '<':
		case '.':
		case ',':
		case '[':
		case ']':
			return chr;
		}
	}

	return chr;
}

/* reading same instruction, and count, for optimization */
/* ++++  -> '+', 4 */
static int gettoken(FILE *src, int *ntok)
{
	int chr = readvalid(src);
	int chr2;
	int cnt = 1;

	if (chr == EOF)
		return EOF;

	if (chr == '.' || chr == ',' || chr == '[' || chr == ']') {
		*ntok = 1;
		return chr;
	}
	
	while ((chr2 = readvalid(src)) == chr)
		cnt++;

	if (chr2 != EOF)
		ungetc(chr2, src);

	*ntok = cnt;
	return chr;
}

/* maintaining loop matched [] */
struct loop_node_st {
	struct sljit_label *loop_start;
	struct sljit_jump *loop_end;
};

/* stack of loops */
static struct loop_node_st loop_stack[BF_LOOP_LEVEL];
static int loop_sp;

static int loop_push(struct sljit_label *loop_start, struct sljit_jump *loop_end)
{
	if (loop_sp >= BF_LOOP_LEVEL)
		return -1;

	loop_stack[loop_sp].loop_start = loop_start;
	loop_stack[loop_sp].loop_end = loop_end;
	loop_sp++;
	return 0;
}

static int loop_pop(struct sljit_label **loop_start, struct sljit_jump **loop_end)
{
	if (loop_sp <= 0)
		return -1;

	loop_sp--;
	*loop_start = loop_stack[loop_sp].loop_start;
	*loop_end = loop_stack[loop_sp].loop_end;
	return 0;
}

static void *SLJIT_FUNC my_alloc(long size, long n)
{
	return calloc(size, n);
}

static void SLJIT_FUNC my_putchar(long c)
{
	putchar(c);
}

static long SLJIT_FUNC my_getchar(void)
{
	return getchar();
}

static void SLJIT_FUNC my_free(void *mem)
{
	free(mem);
}

#define loop_empty()		(loop_sp == 0)

/* compile bf source to a void func() */
static void *compile(FILE *src, unsigned long *lcode)
{
	void *code = NULL;
	int chr;
	int nchr;

	struct sljit_compiler *C = sljit_create_compiler(NULL, NULL);
	struct sljit_jump *end;
	struct sljit_label *loop_start;
	struct sljit_jump *loop_end;

	int SP = SLJIT_S0;			/* bf SP */
	int CELLS = SLJIT_S1;		/* bf array */

	sljit_emit_enter(C, 0, SLJIT_ARGS2(VOID, W, W), 2, 2, 0, 0, 0);								/* opt arg R  S  FR FS local_size */

	sljit_emit_op2(C, SLJIT_XOR, SP, 0, SP, 0, SP, 0);						/* SP = 0 */

	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, BF_CELL_SIZE);
	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS2(P, W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(my_alloc));/* calloc(BF_CELL_SIZE, 1) => R0 */

	end = sljit_emit_cmp(C, SLJIT_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 0);		/* R0 == 0 --> jump end */

	sljit_emit_op1(C, SLJIT_MOV, CELLS, 0, SLJIT_R0, 0);					/* CELLS = R0 */

	while ((chr = gettoken(src, &nchr)) != EOF) {
		switch (chr) {
		case '+':
		case '-':
			sljit_emit_op1(C, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_MEM2(CELLS, SP), 0);		/* R0 = CELLS[SP] */
			sljit_emit_op2(C, chr == '+' ? SLJIT_ADD : SLJIT_SUB, 
						   SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, nchr);					/* R0 ?= nchr */
			sljit_emit_op1(C, SLJIT_MOV_U8, SLJIT_MEM2(CELLS, SP), 0, SLJIT_R0, 0);		/* CELLS[SP] = R0 */
			break;
		case '>':
		case '<':
			sljit_emit_op2(C, chr == '>' ? SLJIT_ADD : SLJIT_SUB, 
						   SP, 0, SP, 0, SLJIT_IMM, nchr);								/* SP ?= nchr */
			break;
		case '.':
			sljit_emit_op1(C, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_MEM2(CELLS, SP), 0);		/* R0 = CELLS[SP] */
			sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(my_putchar));	/* putchar(R0) */
			break;
		case ',':
			sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS0(W), SLJIT_IMM, SLJIT_FUNC_ADDR(my_getchar));	/* R0 = getchar() */
			sljit_emit_op1(C, SLJIT_MOV_U8, SLJIT_MEM2(CELLS, SP), 0, SLJIT_R0, 0);		/* CELLS[SP] = R0 */
			break;
		case '[':
			loop_start = sljit_emit_label(C);											/* loop_start: */
			sljit_emit_op1(C, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_MEM2(CELLS, SP), 0);		/* R0 = CELLS[SP] */
			loop_end = sljit_emit_cmp(C, SLJIT_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 0);		/* IF R0 == 0 goto loop_end */
			
			if (loop_push(loop_start, loop_end)) {
				fprintf(stderr, "Too many loop level\n");
				goto compile_failed;
			}
			break;
		case ']':
			if (loop_pop(&loop_start, &loop_end)) {
				fprintf(stderr, "Unmatch loop ]\n");
				goto compile_failed;
			}
			
			sljit_set_label(sljit_emit_jump(C, SLJIT_JUMP), loop_start);				/* goto loop_start */
			sljit_set_label(loop_end, sljit_emit_label(C));								/* loop_end: */
			break;
		}
	}

	if (!loop_empty()) {
		fprintf(stderr, "Unmatch loop [\n");
		goto compile_failed;
	}

	sljit_emit_op1(C, SLJIT_MOV, SLJIT_R0, 0, CELLS, 0);
	sljit_emit_icall(C, SLJIT_CALL, SLJIT_ARGS1(P, P), SLJIT_IMM, SLJIT_FUNC_ADDR(my_free));	/* free(CELLS) */

	sljit_set_label(end, sljit_emit_label(C));
	sljit_emit_return_void(C);

	code = sljit_generate_code(C);
	if (lcode)
		*lcode = sljit_get_generated_code_size(C);

compile_failed:
	sljit_free_compiler(C);
	return code;
}

/* function prototype of bf compiled code */
typedef void (*bf_entry_t)(void);

int main(int argc, char **argv)
{
	void *code;
	bf_entry_t entry;
	FILE *fp;

	if (argc < 2) {
		fprintf(stderr, "Usage: %s <brainfuck program>\n", argv[0]);
		return -1;
	}

	fp = fopen(argv[1], "rb");
	if (!fp) {
		perror("open");
		return -1;
	}

	code = compile(fp, NULL);
	fclose(fp);

	if (!code) {
		fprintf(stderr, "[Fatal]: Compile failed\n");
		return -1;
	}

	entry = (bf_entry_t)code;
	entry();

	sljit_free_code(code, NULL);
	return 0;
}
