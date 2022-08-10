/*
 *    Stack-less Just-In-Time compiler
 *
 *    Copyright Zoltan Herczeg (hzmester@freemail.hu). All rights reserved.
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

/* Must be the first one. Must not depend on any other include. */
#include "sljitLir.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4127) /* conditional expression is constant */
#endif

#if defined _WIN32 || defined _WIN64
#define COLOR_RED
#define COLOR_GREEN
#define COLOR_ARCH
#define COLOR_DEFAULT
#else
#define COLOR_RED "\33[31m"
#define COLOR_GREEN "\33[32m"
#define COLOR_ARCH "\33[33m"
#define COLOR_DEFAULT "\33[0m"
#endif

union executable_code {
	void* code;
	sljit_sw (SLJIT_FUNC *func0)(void);
	sljit_sw (SLJIT_FUNC *func1)(sljit_sw a);
	sljit_sw (SLJIT_FUNC *func2)(sljit_sw a, sljit_sw b);
	sljit_sw (SLJIT_FUNC *func3)(sljit_sw a, sljit_sw b, sljit_sw c);

	void (SLJIT_FUNC *test70_f1)(sljit_s32 a, sljit_uw b, sljit_u32 c, sljit_sw d);
	void (SLJIT_FUNC *test70_f2)(sljit_s32 a, sljit_u32 b, sljit_sw c, sljit_sw d);
	void (SLJIT_FUNC *test70_f3)(sljit_s32 a, sljit_f32 b, sljit_uw c, sljit_f64 d);
	void (SLJIT_FUNC *test70_f4)(sljit_f32 a, sljit_f64 b, sljit_f32 c, sljit_s32 d);
	void (SLJIT_FUNC *test70_f5)(sljit_f64 a, sljit_f32 b, sljit_u32 c, sljit_f32 d);
	void (SLJIT_FUNC *test70_f6)(sljit_f64 a, sljit_s32 b, sljit_f32 c, sljit_f64 d);
	void (SLJIT_FUNC *test70_f7)(sljit_f32 a, sljit_s32 b, sljit_uw c, sljit_u32 d);
	void (SLJIT_FUNC *test70_f8)(sljit_f64 a, sljit_f64 b, sljit_uw c, sljit_sw d);
	void (SLJIT_FUNC *test70_f9)(sljit_f64 a, sljit_f64 b, sljit_uw c, sljit_f64 d);
	void (SLJIT_FUNC *test70_f10)(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_s32 d);

	sljit_sw (SLJIT_FUNC *test71_f1)(sljit_f32 a, sljit_f64 b);
	sljit_sw (SLJIT_FUNC *test71_f2)(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_f64 d);
	sljit_sw (SLJIT_FUNC *test71_f3)(sljit_f64 a, sljit_f64 b, sljit_f64 c);

	void (SLJIT_FUNC *test73_f1)(sljit_s32 a, sljit_sw b, sljit_sw c, sljit_s32 d);
	void (SLJIT_FUNC *test73_f2)(sljit_sw a, sljit_sw b, sljit_s32 c, sljit_s32 d);
	void (SLJIT_FUNC *test73_f3)(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_sw d);
	void (SLJIT_FUNC *test73_f4)(sljit_f64 a, sljit_f64 b, sljit_sw c, sljit_sw d);
};
typedef union executable_code executable_code;

static sljit_s32 successful_tests = 0;
static sljit_s32 verbose = 0;
static sljit_s32 silent = 0;

#define FAILED(cond, text) \
	if (SLJIT_UNLIKELY(cond)) { \
		printf(text); \
		return; \
	}

#define CHECK(compiler) \
	if (sljit_get_compiler_error(compiler) != SLJIT_ERR_COMPILED) { \
		printf("Compiler error: %d\n", sljit_get_compiler_error(compiler)); \
		sljit_free_compiler(compiler); \
		return; \
	}

static void cond_set(struct sljit_compiler *compiler, sljit_s32 dst, sljit_sw dstw, sljit_s32 type)
{
	/* Testing both sljit_emit_op_flags and sljit_emit_jump. */
	struct sljit_jump* jump;
	struct sljit_label* label;

	sljit_emit_op_flags(compiler, SLJIT_MOV, dst, dstw, type);
	jump = sljit_emit_jump(compiler, type);
	sljit_emit_op2(compiler, SLJIT_ADD, dst, dstw, dst, dstw, SLJIT_IMM, 2);
	label = sljit_emit_label(compiler);
	sljit_set_label(jump, label);
}

#if !(defined SLJIT_CONFIG_UNSUPPORTED && SLJIT_CONFIG_UNSUPPORTED)

/* For interface testing and for test64. */
void *sljit_test_malloc_exec(sljit_uw size, void *exec_allocator_data)
{
	if (exec_allocator_data)
		return exec_allocator_data;

	return SLJIT_BUILTIN_MALLOC_EXEC(size, exec_allocator_data);
}

/* For interface testing. */
void sljit_test_free_code(void* code, void *exec_allocator_data)
{
	SLJIT_UNUSED_ARG(exec_allocator_data);
	SLJIT_BUILTIN_FREE_EXEC(code, exec_allocator_data);
}

#define MALLOC_EXEC(result, size) \
	result = SLJIT_MALLOC_EXEC(size, NULL); \
	if (!result) { \
		printf("Cannot allocate executable memory\n"); \
		return; \
	} \
	memset(result, 255, size);

#define FREE_EXEC(ptr) \
	SLJIT_FREE_EXEC(((sljit_u8*)(ptr)) + SLJIT_EXEC_OFFSET(ptr), NULL);

static void test_exec_allocator(void)
{
	/* This is not an sljit test. */
	void *ptr1;
	void *ptr2;
	void *ptr3;

	if (verbose)
		printf("Run executable allocator test\n");

	MALLOC_EXEC(ptr1, 32);
	MALLOC_EXEC(ptr2, 512);
	MALLOC_EXEC(ptr3, 512);
	FREE_EXEC(ptr2);
	FREE_EXEC(ptr3);
	FREE_EXEC(ptr1);
	MALLOC_EXEC(ptr1, 262104);
	MALLOC_EXEC(ptr2, 32000);
	FREE_EXEC(ptr1);
	MALLOC_EXEC(ptr1, 262104);
	FREE_EXEC(ptr1);
	FREE_EXEC(ptr2);
	MALLOC_EXEC(ptr1, 512);
	MALLOC_EXEC(ptr2, 512);
	MALLOC_EXEC(ptr3, 512);
	FREE_EXEC(ptr2);
	MALLOC_EXEC(ptr2, 512);
#if (defined SLJIT_EXECUTABLE_ALLOCATOR && SLJIT_EXECUTABLE_ALLOCATOR)
	sljit_free_unused_memory_exec();
#endif
	FREE_EXEC(ptr3);
	FREE_EXEC(ptr1);
	FREE_EXEC(ptr2);

#if (defined SLJIT_EXECUTABLE_ALLOCATOR && SLJIT_EXECUTABLE_ALLOCATOR)
	sljit_free_unused_memory_exec();
#endif
}

#undef MALLOC_EXEC

#endif /* !(defined SLJIT_CONFIG_UNSUPPORTED && SLJIT_CONFIG_UNSUPPORTED) */

static void test1(void)
{
	/* Enter and return from an sljit function. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);

	if (verbose)
		printf("Run test1\n");

	FAILED(!compiler, "cannot create compiler\n");

	/* 3 arguments passed, 3 arguments used. */
	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, W, W, W), 3, 3, 0, 0, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_S1, 0);

	SLJIT_ASSERT(sljit_get_generated_code_size(compiler) == 0);
	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	SLJIT_ASSERT(compiler->error == SLJIT_ERR_COMPILED);
	SLJIT_ASSERT(sljit_get_generated_code_size(compiler) > 0);
	sljit_free_compiler(compiler);

	FAILED(code.func3(3, -21, 86) != -21, "test1 case 1 failed\n");
	FAILED(code.func3(4789, 47890, 997) != 47890, "test1 case 2 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test2(void)
{
	/* Test mov. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[8];
	static sljit_sw data[2] = { 0, -9876 };

	if (verbose)
		printf("Run test2\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = 5678;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 9999);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_S1, SLJIT_S0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 3);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_S1, SLJIT_S0), SLJIT_WORD_SHIFT, SLJIT_MEM0(), (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R0), (sljit_sw)&data);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf - 0x12345678);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R0), 0x12345678);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 3456);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf - 0xff890 + 6 * (sljit_sw)sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0xff890, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf + 0xff890 + 7 * (sljit_sw)sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), -0xff890, SLJIT_R0, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R2, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 9999, "test2 case 1 failed\n");
	FAILED(buf[1] != 9999, "test2 case 2 failed\n");
	FAILED(buf[2] != 9999, "test2 case 3 failed\n");
	FAILED(buf[3] != 5678, "test2 case 4 failed\n");
	FAILED(buf[4] != -9876, "test2 case 5 failed\n");
	FAILED(buf[5] != 5678, "test2 case 6 failed\n");
	FAILED(buf[6] != 3456, "test2 case 6 failed\n");
	FAILED(buf[7] != 3456, "test2 case 6 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test3(void)
{
	/* Test not. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[5];

	if (verbose)
		printf("Run test3\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 1234;
	buf[1] = 0;
	buf[2] = 9876;
	buf[3] = 0;
	buf[4] = 0x12345678;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_NOT, SLJIT_MEM0(), (sljit_sw)&buf[1], SLJIT_MEM0(), (sljit_sw)&buf[1]);
	sljit_emit_op1(compiler, SLJIT_NOT, SLJIT_RETURN_REG, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_NOT, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf[4] - 0xff0000 - 0x20);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)&buf[4] - 0xff0000);
	sljit_emit_op1(compiler, SLJIT_NOT, SLJIT_MEM1(SLJIT_R1), 0xff0000 + 0x20, SLJIT_MEM1(SLJIT_R2), 0xff0000);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != ~1234, "test3 case 1 failed\n");
	FAILED(buf[1] != ~1234, "test3 case 2 failed\n");
	FAILED(buf[3] != ~9876, "test3 case 3 failed\n");
	FAILED(buf[4] != ~0x12345678, "test3 case 4 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test4(void)
{
	/* Test negate. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[4];

	if (verbose)
		printf("Run test4\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 1234;
	buf[2] = 0;
	buf[3] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, P, W), 3, 2, 0, 0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_IMM, 0, SLJIT_S1, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM0(), (sljit_sw)&buf[0], SLJIT_IMM, 0, SLJIT_MEM0(), (sljit_sw)&buf[1]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 299);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_IMM, 0, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0, SLJIT_S1, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func2((sljit_sw)&buf, 4567) != -4567, "test4 case 1 failed\n");
	FAILED(buf[0] != -1234, "test4 case 2 failed\n");
	FAILED(buf[2] != -4567, "test4 case 3 failed\n");
	FAILED(buf[3] != -299, "test4 case 4 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test5(void)
{
	/* Test add. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[9];

	if (verbose)
		printf("Run test5\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 100;
	buf[1] = 200;
	buf[2] = 300;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 313;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 50);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 1, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 1, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw) + 2);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, 50);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_IMM, 4, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_IMM, 50, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_IMM, 50, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x1e7d39f2);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R1, 0, SLJIT_IMM, 0x23de7c06);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_IMM, 0x3d72e452, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_IMM, -43, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_IMM, 1000, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1430);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_IMM, -99, SLJIT_R0, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 2437 + 2 * sizeof(sljit_sw), "test5 case 1 failed\n");
	FAILED(buf[0] != 202 + 2 * sizeof(sljit_sw), "test5 case 2 failed\n");
	FAILED(buf[2] != 500, "test5 case 3 failed\n");
	FAILED(buf[3] != 400, "test5 case 4 failed\n");
	FAILED(buf[4] != 200, "test5 case 5 failed\n");
	FAILED(buf[5] != 250, "test5 case 6 failed\n");
	FAILED(buf[6] != 0x425bb5f8, "test5 case 7 failed\n");
	FAILED(buf[7] != 0x5bf01e44, "test5 case 8 failed\n");
	FAILED(buf[8] != 270, "test5 case 9 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test6(void)
{
	/* Test addc, sub, subc. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[11];

	if (verbose)
		printf("Run test6\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 0;
	buf[9] = 0;
	buf[10] = 4000;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_ADDC, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADDC, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 4);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 100);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_R0, 0, SLJIT_IMM, 50);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 6000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_IMM, 10);
	sljit_emit_op2(compiler, SLJIT_SUBC, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 100);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5000);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5000);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_IMM, 6000, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 100);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 32768);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -32767);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 8, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x52cd3bf4);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 9, SLJIT_R0, 0, SLJIT_IMM, 0x3da297c6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 6000);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, 10);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_RETURN_REG, 0, SLJIT_RETURN_REG, 0, SLJIT_IMM, 5);
	sljit_emit_op2(compiler, SLJIT_SUBC, SLJIT_RETURN_REG, 0, SLJIT_RETURN_REG, 0, SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_RETURN_REG, 0, SLJIT_RETURN_REG, 0, SLJIT_IMM, -2220);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 2223, "test6 case 1 failed\n");
	FAILED(buf[0] != 1, "test6 case 2 failed\n");
	FAILED(buf[1] != 5, "test6 case 3 failed\n");
	FAILED(buf[2] != 50, "test6 case 4 failed\n");
	FAILED(buf[3] != 4, "test6 case 5 failed\n");
	FAILED(buf[4] != 50, "test6 case 6 failed\n");
	FAILED(buf[5] != 50, "test6 case 7 failed\n");
	FAILED(buf[6] != 1000, "test6 case 8 failed\n");
	FAILED(buf[7] != 100 - 32768, "test6 case 9 failed\n");
	FAILED(buf[8] != 100 + 32767, "test6 case 10 failed\n");
	FAILED(buf[9] != 0x152aa42e, "test6 case 11 failed\n");
	FAILED(buf[10] != -2000, "test6 case 12 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test7(void)
{
	/* Test logical operators. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[8];

	if (verbose)
		printf("Run test7\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0xff80;
	buf[1] = 0x0f808080;
	buf[2] = 0;
	buf[3] = 0xaaaaaa;
	buf[4] = 0;
	buf[5] = 0x4040;
	buf[6] = 0;
	buf[7] = (sljit_sw)0xc43a7f95;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xf0C000);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_R0, 0, SLJIT_IMM, 0x308f);
	sljit_emit_op2(compiler, SLJIT_XOR, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_AND, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_IMM, 0xf0f0f0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xC0F0);
	sljit_emit_op2(compiler, SLJIT_XOR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xff0000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0xC0F0);
	sljit_emit_op2(compiler, SLJIT_AND, SLJIT_R2, 0, SLJIT_R2, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_R2, 0, SLJIT_IMM, 0xff0000);
	sljit_emit_op2(compiler, SLJIT_XOR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 0xFFFFFF, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_IMM, (sljit_sw)0xa56c82c0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7);
	sljit_emit_op2(compiler, SLJIT_XOR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_IMM, (sljit_sw)0xff00ff00, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0xff00ff00);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 0x0f);
	sljit_emit_op2(compiler, SLJIT_AND, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0x888888, SLJIT_R1, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 0x8808, "test7 case 1 failed\n");
	FAILED(buf[0] != 0x0F807F00, "test7 case 2 failed\n");
	FAILED(buf[1] != 0x0F7F7F7F, "test7 case 3 failed\n");
	FAILED(buf[2] != 0x00F0F08F, "test7 case 4 failed\n");
	FAILED(buf[3] != 0x00A0A0A0, "test7 case 5 failed\n");
	FAILED(buf[4] != 0x00FF80B0, "test7 case 6 failed\n");
	FAILED(buf[5] != 0x00FF4040, "test7 case 7 failed\n");
	FAILED(buf[6] != (sljit_sw)0xa56c82c0, "test7 case 8 failed\n");
	FAILED(buf[7] != 0x3b3a8095, "test7 case 9 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test8(void)
{
	/* Test flags (neg, cmp, test). */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[22];
	sljit_s32 i;

	if (verbose)
		printf("Run test8\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 100;
	for (i = 1; i < 21; i++)
		buf[i] = 3;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 20);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 10);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_IMM, 6, SLJIT_IMM, 5);
	/* buf[1] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_NOT_EQUAL);
	/* buf[2] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_EQUAL);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_IMM, 3000);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 3000);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_S1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_LESS);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R2, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_IMM, -15);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_SIG_GREATER);
	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -(sljit_sw)(~(sljit_uw)0 >> 1) - 1);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	/* buf[6] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_NOT | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_R0, 0);
	/* buf[7] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_NOT | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R1, 0);
	/* buf[8] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 8, SLJIT_ZERO);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_IMM, 0xffff, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 0xffff);
	/* buf[9] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 9, SLJIT_NOT_ZERO);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_IMM, 0xffff, SLJIT_R1, 0);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_IMM, 0xffff);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 0x1);
	/* buf[10] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, SLJIT_NOT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -(sljit_sw)(~(sljit_uw)0 >> 1) - 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0);
	/* buf[11] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 11, SLJIT_OVERFLOW);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0);
	/* buf[12] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 12, SLJIT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 9);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	/* buf[13] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 13, SLJIT_LESS);
	/* buf[14] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 14, SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	/* buf[15] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 15, SLJIT_LESS);
	/* buf[16] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 16, SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -9);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	/* buf[17] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 17, SLJIT_SIG_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -9);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	/* buf[18] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 18, SLJIT_SIG_GREATER);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	/* buf[19] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 19, SLJIT_ZERO);
	/* buf[20] */
	sljit_emit_op2u(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 20, SLJIT_ZERO);
	sljit_emit_op2(compiler, SLJIT_XOR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	/* buf[21] */
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 21, SLJIT_ZERO);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[1] != 1, "test8 case 1 failed\n");
	FAILED(buf[2] != 0, "test8 case 2 failed\n");
	FAILED(buf[3] != 0, "test8 case 3 failed\n");
	FAILED(buf[4] != 1, "test8 case 4 failed\n");
	FAILED(buf[5] != 1, "test8 case 5 failed\n");
	FAILED(buf[6] != 1, "test8 case 6 failed\n");
	FAILED(buf[7] != 1, "test8 case 7 failed\n");
	FAILED(buf[8] != 0, "test8 case 8 failed\n");
	FAILED(buf[9] != 1, "test8 case 9 failed\n");
	FAILED(buf[10] != 0, "test8 case 10 failed\n");
	FAILED(buf[11] != 1, "test8 case 11 failed\n");
	FAILED(buf[12] != 0, "test8 case 12 failed\n");
	FAILED(buf[13] != 1, "test8 case 13 failed\n");
	FAILED(buf[14] != 0, "test8 case 14 failed\n");
	FAILED(buf[15] != 0, "test8 case 15 failed\n");
	FAILED(buf[16] != 1, "test8 case 16 failed\n");
	FAILED(buf[17] != 0, "test8 case 17 failed\n");
	FAILED(buf[18] != 1, "test8 case 18 failed\n");
	FAILED(buf[19] != 1, "test8 case 19 failed\n");
	FAILED(buf[20] != 0, "test8 case 20 failed\n");
	FAILED(buf[21] != 1, "test8 case 21 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test9(void)
{
	/* Test shift. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[13];
#ifdef SLJIT_PREF_SHIFT_REG
	sljit_s32 shift_reg = SLJIT_PREF_SHIFT_REG;
#else
	sljit_s32 shift_reg = SLJIT_R2;
#endif

	SLJIT_ASSERT(shift_reg >= SLJIT_R2 && shift_reg <= SLJIT_R3);

	if (verbose)
		printf("Run test9\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 1 << 10;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 0;
	buf[9] = 3;
	buf[10] = 0;
	buf[11] = 0;
	buf[12] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 4, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xf);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 3);
	sljit_emit_op2(compiler, SLJIT_LSHR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -64);
	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_ASHR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_R0, 0, shift_reg, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 0xff);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op2(compiler, SLJIT_SHL, shift_reg, 0, shift_reg, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, shift_reg, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 0xff);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_op2(compiler, SLJIT_LSHR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, shift_reg, 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 0xf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_S1, 0, SLJIT_S1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_S1, 0);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0xf00);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op2(compiler, SLJIT_LSHR, SLJIT_R1, 0, SLJIT_R2, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 8, SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 9);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), SLJIT_WORD_SHIFT);

	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 4);
	sljit_emit_op2(compiler, SLJIT_SHL, shift_reg, 0, SLJIT_IMM, 2, shift_reg, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, shift_reg, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xa9);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x7d00);
	sljit_emit_op2(compiler, SLJIT_LSHR32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 32);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
#endif
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xe30000);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op2(compiler, SLJIT_ASHR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xffc0);
#else
	sljit_emit_op2(compiler, SLJIT_ASHR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xffe0);
#endif
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x25000000);
	sljit_emit_op2(compiler, SLJIT_SHL32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xfffe1);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
#endif
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 11, SLJIT_R1, 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x5c);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R1, 0, SLJIT_R0, 0, shift_reg, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0xf600);
	sljit_emit_op2(compiler, SLJIT_LSHR32, SLJIT_R0, 0, SLJIT_R0, 0, shift_reg, 0);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	/* Alternative form of uint32 type cast. */
	sljit_emit_op2(compiler, SLJIT_AND, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xffffffff);
#endif
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x630000);
	sljit_emit_op2(compiler, SLJIT_ASHR, SLJIT_R0, 0, SLJIT_R0, 0, shift_reg, 0);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 12, SLJIT_R1, 0, SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 0x3c, "test9 case 1 failed\n");
	FAILED(buf[1] != 0xf0, "test9 case 2 failed\n");
	FAILED(buf[2] != -16, "test9 case 3 failed\n");
	FAILED(buf[3] != 0xff0, "test9 case 4 failed\n");
	FAILED(buf[4] != 4, "test9 case 5 failed\n");
	FAILED(buf[5] != 0xff00, "test9 case 6 failed\n");
	FAILED(buf[6] != 0x3c, "test9 case 7 failed\n");
	FAILED(buf[7] != 0xf0, "test9 case 8 failed\n");
	FAILED(buf[8] != 0xf0, "test9 case 9 failed\n");
	FAILED(buf[9] != 0x18, "test9 case 10 failed\n");
	FAILED(buf[10] != 32, "test9 case 11 failed\n");
	FAILED(buf[11] != 0x4ae37da9, "test9 case 12 failed\n");
	FAILED(buf[12] != 0x63f65c, "test9 case 13 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test10(void)
{
	/* Test multiplications. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[7];

	if (verbose)
		printf("Run test10\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 3;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 6;
	buf[4] = -10;
	buf[5] = 0;
	buf[6] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 7);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_R0, 0, SLJIT_R2, 0, SLJIT_IMM, 8);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_R0, 0, SLJIT_IMM, -3, SLJIT_IMM, -4);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -2);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw) / 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf[3]);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 1, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 1, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 9);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_R0, 0);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 3);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x123456789));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_R0, 0);
#endif
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_RETURN_REG, 0, SLJIT_IMM, 11, SLJIT_IMM, 10);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 110, "test10 case 1 failed\n");
	FAILED(buf[0] != 15, "test10 case 2 failed\n");
	FAILED(buf[1] != 56, "test10 case 3 failed\n");
	FAILED(buf[2] != 12, "test10 case 4 failed\n");
	FAILED(buf[3] != -12, "test10 case 5 failed\n");
	FAILED(buf[4] != 100, "test10 case 6 failed\n");
	FAILED(buf[5] != 81, "test10 case 7 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[6] != SLJIT_W(0x123456789) * 3, "test10 case 8 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test11(void)
{
	/* Test rewritable constants. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_const* const1;
	struct sljit_const* const2;
	struct sljit_const* const3;
	struct sljit_const* const4;
	void* value;
	sljit_sw executable_offset;
	sljit_uw const1_addr;
	sljit_uw const2_addr;
	sljit_uw const3_addr;
	sljit_uw const4_addr;
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw word_value1 = (sljit_sw)SLJIT_W(0xaaaaaaaaaaaaaaaa);
	sljit_sw word_value2 = (sljit_sw)SLJIT_W(0xfee1deadfbadf00d);
#else
	sljit_sw word_value1 = (sljit_sw)0xaaaaaaaal;
	sljit_sw word_value2 = (sljit_sw)0xfbadf00dl;
#endif
	sljit_sw buf[3];

	if (verbose)
		printf("Run test11\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);

	SLJIT_ASSERT(!sljit_alloc_memory(compiler, 0));
	SLJIT_ASSERT(!sljit_alloc_memory(compiler, 16 * sizeof(sljit_sw) + 1));

	const1 = sljit_emit_const(compiler, SLJIT_MEM0(), (sljit_sw)&buf[0], -0x81b9);

	value = sljit_alloc_memory(compiler, 16 * sizeof(sljit_sw));
	if (value != NULL)
	{
		SLJIT_ASSERT(!((sljit_sw)value & ((sljit_sw)sizeof(sljit_sw) - 1)));
		memset(value, 255, 16 * sizeof(sljit_sw));
	}

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	const2 = sljit_emit_const(compiler, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT - 1, -65535);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[0] + 2 * (sljit_sw)sizeof(sljit_sw) - 2);
	const3 = sljit_emit_const(compiler, SLJIT_MEM1(SLJIT_R0), 0, word_value1);

	value = sljit_alloc_memory(compiler, 17);
	if (value != NULL)
	{
		SLJIT_ASSERT(!((sljit_sw)value & ((sljit_sw)sizeof(sljit_sw) - 1)));
		memset(value, 255, 16);
	}

	const4 = sljit_emit_const(compiler, SLJIT_RETURN_REG, 0, (sljit_sw)0xf7afcdb7);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	executable_offset = sljit_get_executable_offset(compiler);
	const1_addr = sljit_get_const_addr(const1);
	const2_addr = sljit_get_const_addr(const2);
	const3_addr = sljit_get_const_addr(const3);
	const4_addr = sljit_get_const_addr(const4);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != (sljit_sw)0xf7afcdb7, "test11 case 1 failed\n");
	FAILED(buf[0] != -0x81b9, "test11 case 2 failed\n");
	FAILED(buf[1] != -65535, "test11 case 3 failed\n");
	FAILED(buf[2] != word_value1, "test11 case 4 failed\n");

	sljit_set_const(const1_addr, -1, executable_offset);
	sljit_set_const(const2_addr, word_value2, executable_offset);
	sljit_set_const(const3_addr, (sljit_sw)0xbab0fea1, executable_offset);
	sljit_set_const(const4_addr, -60089, executable_offset);

	FAILED(code.func1((sljit_sw)&buf) != -60089, "test11 case 5 failed\n");
	FAILED(buf[0] != -1, "test11 case 6 failed\n");
	FAILED(buf[1] != word_value2, "test11 case 7 failed\n");
	FAILED(buf[2] != (sljit_sw)0xbab0fea1, "test11 case 8 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test12(void)
{
	/* Test rewriteable jumps. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *label1;
	struct sljit_label *label2;
	struct sljit_label *label3;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;
	struct sljit_jump *jump3;
	sljit_sw executable_offset;
	void* value;
	sljit_uw jump1_addr;
	sljit_uw label1_addr;
	sljit_uw label2_addr;
	sljit_sw buf[1];

	if (verbose)
		printf("Run test12\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, W), 3, 2, 0, 0, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_S1, 0, SLJIT_IMM, 10);
	jump1 = sljit_emit_jump(compiler, SLJIT_REWRITABLE_JUMP | SLJIT_SIG_GREATER);
	/* Default handler. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 5);
	jump2 = sljit_emit_jump(compiler, SLJIT_JUMP);

	value = sljit_alloc_memory(compiler, 15);
	if (value != NULL)
	{
		SLJIT_ASSERT(!((sljit_sw)value & ((sljit_sw)sizeof(sljit_sw) - 1)));
		memset(value, 255, 15);
	}

	/* Handler 1. */
	label1 = sljit_emit_label(compiler);
	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 6);
	jump3 = sljit_emit_jump(compiler, SLJIT_JUMP);
	/* Handler 2. */
	label2 = sljit_emit_label(compiler);
	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 7);
	/* Exit. */
	label3 = sljit_emit_label(compiler);
	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_set_label(jump2, label3);
	sljit_set_label(jump3, label3);
	/* By default, set to handler 1. */
	sljit_set_label(jump1, label1);
	sljit_emit_return_void(compiler);

	value = sljit_alloc_memory(compiler, 8);
	if (value != NULL)
	{
		SLJIT_ASSERT(!((sljit_sw)value & ((sljit_sw)sizeof(sljit_sw) - 1)));
		memset(value, 255, 8);
	}

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	executable_offset = sljit_get_executable_offset(compiler);
	jump1_addr = sljit_get_jump_addr(jump1);
	label1_addr = sljit_get_label_addr(label1);
	label2_addr = sljit_get_label_addr(label2);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, 4);
	FAILED(buf[0] != 5, "test12 case 1 failed\n");

	code.func2((sljit_sw)&buf, 11);
	FAILED(buf[0] != 6, "test12 case 2 failed\n");

	sljit_set_jump_addr(jump1_addr, label2_addr, executable_offset);
	code.func2((sljit_sw)&buf, 12);
	FAILED(buf[0] != 7, "test12 case 3 failed\n");

	sljit_set_jump_addr(jump1_addr, label1_addr, executable_offset);
	code.func2((sljit_sw)&buf, 13);
	FAILED(buf[0] != 6, "test12 case 4 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test13(void)
{
	/* Test fpu monadic functions. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_f64 buf[7];
	sljit_sw buf2[6];

	if (verbose)
		printf("Run test13\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test13 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 7.75;
	buf[1] = -4.5;
	buf[2] = 0.0;
	buf[3] = 0.0;
	buf[4] = 0.0;
	buf[5] = 0.0;
	buf[6] = 0.0;

	buf2[0] = 10;
	buf2[1] = 10;
	buf2[2] = 10;
	buf2[3] = 10;
	buf2[4] = 10;
	buf2[5] = 10;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 3, 2, 6, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&buf[2], SLJIT_MEM0(), (sljit_sw)&buf[1]);
	sljit_emit_fop1(compiler, SLJIT_ABS_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM0(), (sljit_sw)&buf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_FR2, 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_FR2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&buf[4], SLJIT_FR3, 0);
	sljit_emit_fop1(compiler, SLJIT_ABS_F64, SLJIT_FR4, 0, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR4, 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_FR4, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_F_GREATER);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR5, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw), SLJIT_F_GREATER);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_FR1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_F_LESS);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_NOT_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_F_NOT_EQUAL);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&buf2);
	FAILED(buf[2] != -4.5, "test13 case 1 failed\n");
	FAILED(buf[3] != 4.5, "test13 case 2 failed\n");
	FAILED(buf[4] != -7.75, "test13 case 3 failed\n");
	FAILED(buf[5] != 4.5, "test13 case 4 failed\n");
	FAILED(buf[6] != -4.5, "test13 case 5 failed\n");

	FAILED(buf2[0] != 1, "test13 case 6 failed\n");
	FAILED(buf2[1] != 0, "test13 case 7 failed\n");
	FAILED(buf2[2] != 1, "test13 case 8 failed\n");
	FAILED(buf2[3] != 0, "test13 case 9 failed\n");
	FAILED(buf2[4] != 0, "test13 case 10 failed\n");
	FAILED(buf2[5] != 1, "test13 case 11 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test14(void)
{
	/* Test fpu diadic functions. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_f64 buf[15];

	if (verbose)
		printf("Run test14\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test14 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}
	buf[0] = 7.25;
	buf[1] = 3.5;
	buf[2] = 1.75;
	buf[3] = 0.0;
	buf[4] = 0.0;
	buf[5] = 0.0;
	buf[6] = 0.0;
	buf[7] = 0.0;
	buf[8] = 0.0;
	buf[9] = 0.0;
	buf[10] = 0.0;
	buf[11] = 0.0;
	buf[12] = 8.0;
	buf[13] = 4.0;
	buf[14] = 0.0;

	FAILED(!compiler, "cannot create compiler\n");
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 6, 0, 0);

	/* ADD */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 3, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR1, 0, SLJIT_FR0, 0, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 4, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 5, SLJIT_FR1, 0);

	/* SUB */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 6, SLJIT_FR3, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_F64_SHIFT);
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_FR2, 0, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_FR3, 0, SLJIT_FR2, 0, SLJIT_FR3, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 7, SLJIT_FR2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 8, SLJIT_FR3, 0);

	/* MUL */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 9, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_F64_SHIFT, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_FR1, 0, SLJIT_FR1, 0, SLJIT_FR2, 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2, SLJIT_FR2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 10, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 11, SLJIT_FR5, 0);

	/* DIV */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 13);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR4, 0, SLJIT_FR5, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_FR5, 0, SLJIT_FR5, 0, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_FR4, 0, SLJIT_FR1, 0, SLJIT_FR4, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 13, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 14, SLJIT_FR4, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[3] != 10.75, "test14 case 1 failed\n");
	FAILED(buf[4] != 5.25, "test14 case 2 failed\n");
	FAILED(buf[5] != 7.0, "test14 case 3 failed\n");
	FAILED(buf[6] != 0.0, "test14 case 4 failed\n");
	FAILED(buf[7] != 5.5, "test14 case 5 failed\n");
	FAILED(buf[8] != 3.75, "test14 case 6 failed\n");
	FAILED(buf[9] != 24.5, "test14 case 7 failed\n");
	FAILED(buf[10] != 38.5, "test14 case 8 failed\n");
	FAILED(buf[11] != 9.625, "test14 case 9 failed\n");
	FAILED(buf[12] != 2.0, "test14 case 10 failed\n");
	FAILED(buf[13] != 2.0, "test14 case 11 failed\n");
	FAILED(buf[14] != 0.5, "test14 case 12 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static sljit_sw func(sljit_sw a, sljit_sw b, sljit_sw c)
{
	return a + b + c + 5;
}

static void test15(void)
{
	/* Test function call. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_sw buf[7];

	if (verbose)
		printf("Run test15\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = SLJIT_FUNC_ADDR(func);

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 4, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 7);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_REWRITABLE_JUMP, SLJIT_ARGS3(W, W, W, W));
	sljit_set_target(jump, (sljit_uw)-1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 40);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -60);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -30);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 100);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 110);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 120);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 6);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_set_jump_addr(sljit_get_jump_addr(jump), SLJIT_FUNC_UADDR(func), sljit_get_executable_offset(compiler));
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != -15, "test15 case 1 failed\n");
	FAILED(buf[0] != 14, "test15 case 2 failed\n");
	FAILED(buf[1] != -8, "test15 case 3 failed\n");
	FAILED(buf[2] != SLJIT_FUNC_ADDR(func) + 42, "test15 case 4 failed\n");
	FAILED(buf[3] != SLJIT_FUNC_ADDR(func) - 85, "test15 case 5 failed\n");
	FAILED(buf[4] != SLJIT_FUNC_ADDR(func) + 31, "test15 case 6 failed\n");
	FAILED(buf[5] != 335, "test15 case 7 failed\n");
	FAILED(buf[6] != -15, "test15 case 8 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test16(void)
{
	/* Ackermann benchmark. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *entry;
	struct sljit_label *label;
	struct sljit_jump *jump;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;

	if (verbose)
		printf("Run test16\n");

	FAILED(!compiler, "cannot create compiler\n");

	entry = sljit_emit_label(compiler);
	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, W, W), 3, 2, 0, 0, 0);
	/* If x == 0. */
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_S0, 0, SLJIT_IMM, 0);
	jump1 = sljit_emit_jump(compiler, SLJIT_EQUAL);
	/* If y == 0. */
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_S1, 0, SLJIT_IMM, 0);
	jump2 = sljit_emit_jump(compiler, SLJIT_EQUAL);

	/* Ack(x,y-1). */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 1);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(W, W, W));
	sljit_set_label(jump, entry);

	/* Returns with Ack(x-1, Ack(x,y-1)). */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_RETURN_REG, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(W, W, W));
	sljit_set_label(jump, entry);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	/* Returns with y+1. */
	label = sljit_emit_label(compiler);
	sljit_set_label(jump1, label);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_RETURN_REG, 0, SLJIT_IMM, 1, SLJIT_S1, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	/* Returns with Ack(x-1,1) */
	label = sljit_emit_label(compiler);
	sljit_set_label(jump2, label);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(W, W, W));
	sljit_set_label(jump, entry);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func2(3, 3) != 61, "test16 case 1 failed\n");
	/* For benchmarking. */
	/* FAILED(code.func2(3, 11) != 16381, "test16 case 1 failed\n"); */

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test17(void)
{
	/* Test arm constant pool. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;
	sljit_sw buf[5];

	if (verbose)
		printf("Run test17\n");

	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 5; i++)
		buf[i] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 0, 0, 0);
	for (i = 0; i <= 0xfff; i++) {
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x81818000 | i);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x81818000 | i);
		if ((i & 0x3ff) == 0)
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), (i >> 10) * (sljit_sw)sizeof(sljit_sw), SLJIT_R0, 0);
	}
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED((sljit_uw)buf[0] != 0x81818000, "test17 case 1 failed\n");
	FAILED((sljit_uw)buf[1] != 0x81818400, "test17 case 2 failed\n");
	FAILED((sljit_uw)buf[2] != 0x81818800, "test17 case 3 failed\n");
	FAILED((sljit_uw)buf[3] != 0x81818c00, "test17 case 4 failed\n");
	FAILED((sljit_uw)buf[4] != 0x81818fff, "test17 case 5 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test18(void)
{
	/* Test 64 bit. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[11];

	if (verbose)
		printf("Run test18\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 100;
	buf[6] = 100;
	buf[7] = 100;
	buf[8] = 100;
	buf[9] = 0;
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE) && (defined SLJIT_BIG_ENDIAN && SLJIT_BIG_ENDIAN)
	buf[10] = SLJIT_W(1) << 32;
#else
	buf[10] = 1;
#endif

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 2, 0, 0, 0);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, SLJIT_W(0x1122334455667788));
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x1122334455667788));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(1000000000000));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(1000000000000));
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_IMM, SLJIT_W(5000000000000), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x1108080808));
	sljit_emit_op2(compiler, SLJIT_ADD32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x1120202020));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x1108080808));
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x1120202020));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_AND32 | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x1120202020));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_ZERO);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x1108080808));
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x2208080808));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_AND32 | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x1104040404));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 8, SLJIT_NOT_ZERO);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op2(compiler, SLJIT_SHL32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 9, SLJIT_IMM, SLJIT_W(0xffff0000), SLJIT_R0, 0);

	sljit_emit_op2(compiler, SLJIT_MUL32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 10, SLJIT_IMM, -1);
#else
	/* 32 bit operations. */

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 0x11223344);
	sljit_emit_op2(compiler, SLJIT_ADD32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 0x44332211);

#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[0] != SLJIT_W(0x1122334455667788), "test18 case 1 failed\n");
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	FAILED(buf[1] != 0x55667788, "test18 case 2 failed\n");
#else
	FAILED(buf[1] != SLJIT_W(0x5566778800000000), "test18 case 2 failed\n");
#endif
	FAILED(buf[2] != SLJIT_W(2000000000000), "test18 case 3 failed\n");
	FAILED(buf[3] != SLJIT_W(4000000000000), "test18 case 4 failed\n");
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	FAILED(buf[4] != 0x28282828, "test18 case 5 failed\n");
#else
	FAILED(buf[4] != SLJIT_W(0x2828282800000000), "test18 case 5 failed\n");
#endif
	FAILED(buf[5] != 0, "test18 case 6 failed\n");
	FAILED(buf[6] != 1, "test18 case 7 failed\n");
	FAILED(buf[7] != 1, "test18 case 8 failed\n");
	FAILED(buf[8] != 0, "test18 case 9 failed\n");
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	FAILED(buf[9] != (sljit_sw)0xfff00000, "test18 case 10 failed\n");
	FAILED(buf[10] != (sljit_sw)0xffffffff, "test18 case 11 failed\n");
#else
	FAILED(buf[9] != (sljit_sw)SLJIT_W(0xfff0000000000000), "test18 case 10 failed\n");
	FAILED(buf[10] != (sljit_sw)SLJIT_W(0xffffffff00000000), "test18 case 11 failed\n");
#endif
#else
	FAILED(buf[0] != 0x11223344, "test18 case 1 failed\n");
	FAILED(buf[1] != 0x44332211, "test18 case 2 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test19(void)
{
	/* Test arm partial instruction caching. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[10];

	if (verbose)
		printf("Run test19\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 6;
	buf[1] = 4;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 2;
	buf[7] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 0, 0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM0(), (sljit_sw)&buf[2], SLJIT_MEM0(), (sljit_sw)&buf[1], SLJIT_MEM0(), (sljit_sw)&buf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_MEM1(SLJIT_R0), (sljit_sw)&buf[0], SLJIT_MEM1(SLJIT_R1), (sljit_sw)&buf[0]);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_MEM0(), (sljit_sw)&buf[0], SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_R0), (sljit_sw)&buf[0] + 4 * (sljit_sw)sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_IMM, 10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 7);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_R1), (sljit_sw)&buf[5], SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM1(SLJIT_R1), (sljit_sw)&buf[5]);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 10, "test19 case 1 failed\n");
	FAILED(buf[1] != 4, "test19 case 2 failed\n");
	FAILED(buf[2] != 14, "test19 case 3 failed\n");
	FAILED(buf[3] != 14, "test19 case 4 failed\n");
	FAILED(buf[4] != 8, "test19 case 5 failed\n");
	FAILED(buf[5] != 6, "test19 case 6 failed\n");
	FAILED(buf[6] != 12, "test19 case 7 failed\n");
	FAILED(buf[7] != 10, "test19 case 8 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test20(void)
{
	/* Test stack. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump;
	struct sljit_label* label;
	sljit_sw buf[6];
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw offset_value = SLJIT_W(0x1234567812345678);
#else
	sljit_sw offset_value = SLJIT_W(0x12345678);
#endif

	if (verbose)
		printf("Run test20\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 5;
	buf[1] = 12;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 111;
	buf[5] = -12345;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 5, 5, 0, 0, 4 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw), SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_uw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R4, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S4, 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_uw), SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_uw), SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw), SLJIT_MEM1(SLJIT_SP), 0);
	sljit_get_local_base(compiler, SLJIT_R0, 0, -offset_value);
	sljit_get_local_base(compiler, SLJIT_MEM1(SLJIT_S0), 0, -0x1234);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_uw), SLJIT_MEM1(SLJIT_R0), offset_value, SLJIT_MEM1(SLJIT_R1), 0x1234 + sizeof(sljit_sw));
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_uw));
	/* Dummy last instructions. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 23);
	sljit_emit_label(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != -12345, "test20 case 1 failed\n")

	FAILED(buf[2] != 60, "test20 case 2 failed\n");
	FAILED(buf[3] != 17, "test20 case 3 failed\n");
	FAILED(buf[4] != 7, "test20 case 4 failed\n");

	sljit_free_code(code.code, NULL);

	compiler = sljit_create_compiler(NULL, NULL);
	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, W, W, W), 3, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_get_local_base(compiler, SLJIT_R0, 0, SLJIT_MAX_LOCAL_SIZE - sizeof(sljit_sw));
	sljit_get_local_base(compiler, SLJIT_R1, 0, -(sljit_sw)sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -1);
	label = sljit_emit_label(compiler);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R2, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_R0, 0);
	jump = sljit_emit_jump(compiler, SLJIT_NOT_EQUAL);
	sljit_set_label(jump, label);

	/* Saved registers should keep their value. */
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_S1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_S2, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func3(1234, 5678, 9012) != 15924, "test20 case 5 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test21(void)
{
	/* Test set context. The parts of the jit code can be separated in the memory. */
	executable_code code1;
	executable_code code2;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_uw addr;
	sljit_sw executable_offset;
	sljit_sw buf[4];

	if (verbose)
		printf("Run test21\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 9;
	buf[1] = -6;
	buf[2] = 0;
	buf[3] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 2, 0, 0, 2 * sizeof(sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, 10);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_SP), 0);

	jump = sljit_emit_jump(compiler, SLJIT_JUMP | SLJIT_REWRITABLE_JUMP);
	sljit_set_target(jump, 0);

	code1.code = sljit_generate_code(compiler);
	CHECK(compiler);

	executable_offset = sljit_get_executable_offset(compiler);
	addr = sljit_get_jump_addr(jump);

	sljit_free_compiler(compiler);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	/* Other part of the jit code. */
	sljit_set_context(compiler, 0, 1, 3, 2, 0, 0, 2 * sizeof(sljit_sw));

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_sw));

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code2.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	sljit_set_jump_addr(addr, SLJIT_FUNC_UADDR(code2.code), executable_offset);

	FAILED(code1.func1((sljit_sw)&buf) != 19, "test21 case 1 failed\n");
	FAILED(buf[2] != -16, "test21 case 2 failed\n");
	FAILED(buf[3] != 100, "test21 case 3 failed\n");

	sljit_free_code(code1.code, NULL);
	sljit_free_code(code2.code, NULL);
	successful_tests++;
}

static void test22(void)
{
	/* Test simple byte and half-int data transfers. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[4];
	sljit_s16 sbuf[9];
	sljit_s8 bbuf[5];

	if (verbose)
		printf("Run test22\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = -1;
	buf[3] = -1;

	sbuf[0] = 0;
	sbuf[1] = 0;
	sbuf[2] = -9;
	sbuf[3] = 0;
	sbuf[4] = 0;
	sbuf[5] = 0;
	sbuf[6] = 0;

	bbuf[0] = 0;
	bbuf[1] = 0;
	bbuf[2] = -56;
	bbuf[3] = 0;
	bbuf[4] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 3, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_IMM, -13);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s16), SLJIT_IMM, 0x1234);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S1, 0, SLJIT_S1, 0, SLJIT_IMM, 2 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s16), SLJIT_MEM1(SLJIT_S1), -(sljit_sw)sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xff0000 + 8000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 1, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_S1, 0, SLJIT_IMM, 0x1234 - 3 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_R2), 0x1234, SLJIT_IMM, -9317);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S1, 0, SLJIT_IMM, 0x1234 + 4 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_R2), -0x1234, SLJIT_IMM, -9317);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_S1, 0, SLJIT_IMM, 0x12348 - 5 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_R2), 0x12348, SLJIT_IMM, -8888);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S1, 0, SLJIT_IMM, 0x12348 + 6 * sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_R2), -0x12348, SLJIT_IMM, -8888);

	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_MEM1(SLJIT_S2), 0, SLJIT_IMM, -45);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_s8), SLJIT_IMM, 0x12);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 4 * sizeof(sljit_s8));
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_s8));
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_S1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_S1, 0, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R2, 0, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_s8), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM2(SLJIT_S2, SLJIT_R0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&buf, (sljit_sw)&sbuf, (sljit_sw)&bbuf);
	FAILED(buf[0] != -9, "test22 case 1 failed\n");
	FAILED(buf[1] != -56, "test22 case 2 failed\n");
	FAILED(buf[2] != 0, "test22 case 3 failed\n");
	FAILED(buf[3] != 0, "test22 case 4 failed\n");

	FAILED(sbuf[0] != -13, "test22 case 5 failed\n");
	FAILED(sbuf[1] != 0x1234, "test22 case 6 failed\n");
	FAILED(sbuf[3] != 0x1234, "test22 case 7 failed\n");
	FAILED(sbuf[4] != 8000, "test22 case 8 failed\n");
	FAILED(sbuf[5] != -9317, "test22 case 9 failed\n");
	FAILED(sbuf[6] != -9317, "test22 case 10 failed\n");
	FAILED(sbuf[7] != -8888, "test22 case 11 failed\n");
	FAILED(sbuf[8] != -8888, "test22 case 12 failed\n");

	FAILED(bbuf[0] != -45, "test22 case 13 failed\n");
	FAILED(bbuf[1] != 0x12, "test22 case 14 failed\n");
	FAILED(bbuf[3] != -56, "test22 case 15 failed\n");
	FAILED(bbuf[4] != 4, "test22 case 16 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test23(void)
{
	/* Test 32 bit / 64 bit signed / unsigned int transfer and conversion.
	   This test has do real things on 64 bit systems, but works on 32 bit systems as well. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[9];
	sljit_s32 ibuf[5];
	union {
		sljit_s32 asint;
		sljit_u8 asbytes[4];
	} u;
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw garbage = SLJIT_W(0x1234567812345678);
#else
	sljit_sw garbage = 0x12345678;
#endif

	if (verbose)
		printf("Run test23\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 0;

	ibuf[0] = 0;
	ibuf[1] = 0;
	ibuf[2] = -5791;
	ibuf[3] = 43579;
	ibuf[4] = 658923;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, P, P), 3, 3, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_IMM, 34567);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), 0, SLJIT_IMM, -7654);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, garbage);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_s32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, garbage);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_s32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, garbage);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_s32));
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x0f00f00);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 0x7777);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0x7777 + 3 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 0x7777);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), -0x7777 + 4 * (sljit_sw)sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 5 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_LSHR, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_R1, SLJIT_R1), 0, SLJIT_IMM, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), 1, SLJIT_IMM, 64, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&buf[7], SLJIT_IMM, 0x123456);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), (sljit_sw)&buf[6], SLJIT_MEM0(), (sljit_sw)&buf[7]);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 5 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 7 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_LSHR, SLJIT_R2, 0, SLJIT_R2, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_MEM2(SLJIT_R2, SLJIT_R2), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[8] - 0x12340);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0x12340, SLJIT_R2, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_R0), 0x12340, SLJIT_MEM1(SLJIT_R2), 3 * sizeof(sljit_sw), SLJIT_IMM, 6);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_s32), SLJIT_IMM, 0x12345678);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x2bd700 | 243);
	sljit_emit_return(compiler, SLJIT_MOV_S8, SLJIT_R1, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func2((sljit_sw)&buf, (sljit_sw)&ibuf) != -13, "test23 case 1 failed\n");
	FAILED(buf[0] != -5791, "test23 case 2 failed\n");
	FAILED(buf[1] != 43579, "test23 case 3 failed\n");
	FAILED(buf[2] != 658923, "test23 case 4 failed\n");
	FAILED(buf[3] != 0x0f00f00, "test23 case 5 failed\n");
	FAILED(buf[4] != 0x0f00f00, "test23 case 6 failed\n");
	FAILED(buf[5] != 80, "test23 case 7 failed\n");
	FAILED(buf[6] != 0x123456, "test23 case 8 failed\n");
	FAILED(buf[7] != (sljit_sw)&buf[5], "test23 case 9 failed\n");
	FAILED(buf[8] != (sljit_sw)&buf[5] + 6, "test23 case 10 failed\n");

	FAILED(ibuf[0] != 34567, "test23 case 11 failed\n");
	FAILED(ibuf[1] != -7654, "test23 case 12 failed\n");
	u.asint = ibuf[4];
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	FAILED(u.asbytes[0] != 0x78, "test23 case 13 failed\n");
	FAILED(u.asbytes[1] != 0x56, "test23 case 14 failed\n");
	FAILED(u.asbytes[2] != 0x34, "test23 case 15 failed\n");
	FAILED(u.asbytes[3] != 0x12, "test23 case 16 failed\n");
#else
	FAILED(u.asbytes[0] != 0x12, "test23 case 13 failed\n");
	FAILED(u.asbytes[1] != 0x34, "test23 case 14 failed\n");
	FAILED(u.asbytes[2] != 0x56, "test23 case 15 failed\n");
	FAILED(u.asbytes[3] != 0x78, "test23 case 16 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test24(void)
{
	/* Some complicated addressing modes. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[9];
	sljit_s16 sbuf[5];
	sljit_s8 bbuf[7];

	if (verbose)
		printf("Run test24\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = 100567;
	buf[1] = 75799;
	buf[2] = 0;
	buf[3] = -8;
	buf[4] = -50;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 0;

	sbuf[0] = 30000;
	sbuf[1] = 0;
	sbuf[2] = 0;
	sbuf[3] = -12345;
	sbuf[4] = 0;

	bbuf[0] = -128;
	bbuf[1] = 0;
	bbuf[2] = 0;
	bbuf[3] = 99;
	bbuf[4] = 0;
	bbuf[5] = 0;
	bbuf[6] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 3, 3, 0, 0, 0);

	/* Nothing should be updated. */
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM0(), (sljit_sw)&sbuf[1], SLJIT_MEM0(), (sljit_sw)&sbuf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_MEM0(), (sljit_sw)&bbuf[1], SLJIT_MEM0(), (sljit_sw)&bbuf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), 1, SLJIT_MEM0(), (sljit_sw)&sbuf[3]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM2(SLJIT_R0, SLJIT_R2), SLJIT_WORD_SHIFT, SLJIT_MEM0(), (sljit_sw)&buf[0], SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_s8));
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_R0), (sljit_sw)&bbuf[1], SLJIT_MEM1(SLJIT_R0), (sljit_sw)&bbuf[2]);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_s16));
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_MEM1(SLJIT_R1), (sljit_sw)&sbuf[3], SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 3);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_WORD_SHIFT);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 9 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 4 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -(4 << SLJIT_WORD_SHIFT));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_R0, SLJIT_R2), 0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf - 0x7fff8000 + 6 * (sljit_sw)sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 952467);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0x7fff8000, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0x7fff8000 + sizeof(sljit_sw), SLJIT_MEM1(SLJIT_R0), 0x7fff8000);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf + 0x7fff7fff + 6 * (sljit_sw)sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_R0), -0x7fff7fff + 2 * (sljit_sw)sizeof(sljit_sw), SLJIT_MEM1(SLJIT_R0), -0x7fff7fff + (sljit_sw)sizeof(sljit_sw), SLJIT_MEM1(SLJIT_R0), -0x7fff7fff);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&bbuf - 0x7fff7ffe + 3 * (sljit_sw)sizeof(sljit_s8));
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_MEM1(SLJIT_R0), 0x7fff7fff, SLJIT_MEM1(SLJIT_R0), 0x7fff7ffe);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&bbuf + 0x7fff7fff + 5 * (sljit_sw)sizeof(sljit_s8));
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_MEM1(SLJIT_R0), -0x7fff7fff, SLJIT_MEM1(SLJIT_R0), -0x7fff8000);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&bbuf - SLJIT_W(0x123456123456));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&bbuf - SLJIT_W(0x123456123456));
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_MEM1(SLJIT_R0), SLJIT_W(0x123456123456) + 6 * sizeof(sljit_s8), SLJIT_MEM1(SLJIT_R1), SLJIT_W(0x123456123456));
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&buf, (sljit_sw)&sbuf, (sljit_sw)&bbuf);
	FAILED(buf[2] != 176366, "test24 case 1 failed\n");
	FAILED(buf[3] != 64, "test24 case 2 failed\n");
	FAILED(buf[4] != -100, "test24 case 3 failed\n");
	FAILED(buf[5] != 100567, "test24 case 4 failed\n");
	FAILED(buf[6] != 952467, "test24 case 5 failed\n");
	FAILED(buf[7] != 952467, "test24 case 6 failed\n");
	FAILED(buf[8] != 952467 * 2, "test24 case 7 failed\n");

	FAILED(sbuf[1] != 30000, "test24 case 8 failed\n");
	FAILED(sbuf[2] != -12345, "test24 case 9 failed\n");
	FAILED(sbuf[4] != sizeof(sljit_s16), "test24 case 10 failed\n");

	FAILED(bbuf[1] != -128, "test24 case 11 failed\n");
	FAILED(bbuf[2] != 99, "test24 case 12 failed\n");
	FAILED(bbuf[4] != 99, "test24 case 13 failed\n");
	FAILED(bbuf[5] != 99, "test24 case 14 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(bbuf[6] != -128, "test24 case 15 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test25(void)
{
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	/* 64 bit loads. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[14];

	if (verbose)
		printf("Run test25\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 7;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = 0;
	buf[9] = 0;
	buf[10] = 0;
	buf[11] = 0;
	buf[12] = 0;
	buf[13] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 1 * sizeof(sljit_sw), SLJIT_IMM, 0x7fff);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_IMM, -0x8000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_IMM, 0x7fffffff);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(-0x80000000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x1234567887654321));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0xff80000000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x3ff0000000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_IMM, (sljit_sw)SLJIT_W(0xfffffff800100000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_IMM, (sljit_sw)SLJIT_W(0xfffffff80010f000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x07fff00000008001));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x07fff00080010000));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x07fff00080018001));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_IMM, SLJIT_W(0x07fff00ffff00000));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 0, "test25 case 1 failed\n");
	FAILED(buf[1] != 0x7fff, "test25 case 2 failed\n");
	FAILED(buf[2] != -0x8000, "test25 case 3 failed\n");
	FAILED(buf[3] != 0x7fffffff, "test25 case 4 failed\n");
	FAILED(buf[4] != SLJIT_W(-0x80000000), "test25 case 5 failed\n");
	FAILED(buf[5] != SLJIT_W(0x1234567887654321), "test25 case 6 failed\n");
	FAILED(buf[6] != SLJIT_W(0xff80000000), "test25 case 7 failed\n");
	FAILED(buf[7] != SLJIT_W(0x3ff0000000), "test25 case 8 failed\n");
	FAILED((sljit_uw)buf[8] != SLJIT_W(0xfffffff800100000), "test25 case 9 failed\n");
	FAILED((sljit_uw)buf[9] != SLJIT_W(0xfffffff80010f000), "test25 case 10 failed\n");
	FAILED(buf[10] != SLJIT_W(0x07fff00000008001), "test25 case 11 failed\n");
	FAILED(buf[11] != SLJIT_W(0x07fff00080010000), "test25 case 12 failed\n");
	FAILED(buf[12] != SLJIT_W(0x07fff00080018001), "test25 case 13 failed\n");
	FAILED(buf[13] != SLJIT_W(0x07fff00ffff00000), "test25 case 14 failed\n");

	sljit_free_code(code.code, NULL);
#endif
	successful_tests++;
}

static void test26(void)
{
	/* Aligned access without aligned offsets. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[4];
	sljit_s32 ibuf[4];
	sljit_f64 dbuf[4];

	if (verbose)
		printf("Run test26\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = -2789;
	buf[1] = 0;
	buf[2] = 4;
	buf[3] = -4;

	ibuf[0] = -689;
	ibuf[1] = 0;
	ibuf[2] = -6;
	ibuf[3] = 3;

	dbuf[0] = 5.75;
	dbuf[1] = 0.0;
	dbuf[2] = 0.0;
	dbuf[3] = -4.0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 3, 3, 0, 0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 3);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S1, 0, SLJIT_S1, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), -3);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s32) - 1, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) - 3, SLJIT_R0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 100);
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_MEM1(SLJIT_R0), (sljit_sw)sizeof(sljit_sw) * 2 - 103, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2 - 3, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 3 - 3);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 100);
	sljit_emit_op2(compiler, SLJIT_MUL32, SLJIT_MEM1(SLJIT_R0), (sljit_sw)sizeof(sljit_s32) * 2 - 101, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s32) * 2 - 1, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s32) * 3 - 1);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S2, 0, SLJIT_S2, 0, SLJIT_IMM, 3);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f64) - 3, SLJIT_MEM1(SLJIT_S2), -3);
		sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f64) * 2 - 3, SLJIT_MEM1(SLJIT_S2), -3, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f64) - 3);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S2, 0, SLJIT_IMM, 2);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sizeof(sljit_f64) * 3 - 4) >> 1);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, 1);
		sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64) * 3 - 5, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f64) * 2 - 3, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 1);
	}

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&buf, (sljit_sw)&ibuf, (sljit_sw)&dbuf);

	FAILED(buf[1] != -689, "test26 case 1 failed\n");
	FAILED(buf[2] != -16, "test26 case 2 failed\n");
	FAILED(ibuf[1] != -2789, "test26 case 3 failed\n");
	FAILED(ibuf[2] != -18, "test26 case 4 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		FAILED(dbuf[1] != 5.75, "test26 case 5 failed\n");
		FAILED(dbuf[2] != 11.5, "test26 case 6 failed\n");
		FAILED(dbuf[3] != -2.875, "test26 case 7 failed\n");
	}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test27(void)
{
#define SET_NEXT_BYTE(type) \
		cond_set(compiler, SLJIT_R2, 0, type); \
		sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_R2, 0); \
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
#define RESULT(i) i
#else
#define RESULT(i) (3 - i)
#endif

	/* Playing with conditional flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_u8 buf[41];
	sljit_u32 i;
#ifdef SLJIT_PREF_SHIFT_REG
	sljit_s32 shift_reg = SLJIT_PREF_SHIFT_REG;
#else
	sljit_s32 shift_reg = SLJIT_R2;
#endif

	SLJIT_ASSERT(shift_reg >= SLJIT_R2 && shift_reg <= SLJIT_R3);

	if (verbose)
		printf("Run test27\n");

	for (i = 0; i < sizeof(buf); ++i)
		buf[i] = 10;

	FAILED(!compiler, "cannot create compiler\n");

	/* 3 arguments passed, 3 arguments used. */
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 4, 3, 0, 0, 0);

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x1001);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 20);
	/* 0x100100000 on 64 bit machines, 0x100000 on 32 bit machines. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x800000);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op0(compiler, SLJIT_ENDBR); /* ENDBR should keep the flags. */
	sljit_emit_op0(compiler, SLJIT_NOP); /* Nop should keep the flags. */
	SET_NEXT_BYTE(SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op0(compiler, SLJIT_ENDBR); /* ENDBR should keep the flags. */
	sljit_emit_op0(compiler, SLJIT_NOP); /* Nop should keep the flags. */
	SET_NEXT_BYTE(SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_LESS);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x1000);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 20);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0x10);
	/* 0x100000010 on 64 bit machines, 0x10 on 32 bit machines. */
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_IMM, 0x80);
	SET_NEXT_BYTE(SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 0x80);
	SET_NEXT_BYTE(SLJIT_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_IMM, 0x80);
	SET_NEXT_BYTE(SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 0x80);
	SET_NEXT_BYTE(SLJIT_LESS);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	/* 0xff..ff on all machines. */
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_LESS_EQUAL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	SET_NEXT_BYTE(SLJIT_LESS_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_GREATER_EQUAL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	SET_NEXT_BYTE(SLJIT_GREATER_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R2, 0, SLJIT_R1, 0, SLJIT_IMM, -1);
	SET_NEXT_BYTE(SLJIT_SIG_GREATER);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, -1);
	SET_NEXT_BYTE(SLJIT_SIG_LESS);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R2, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	SET_NEXT_BYTE(SLJIT_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	SET_NEXT_BYTE(SLJIT_NOT_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, -2);
	SET_NEXT_BYTE(SLJIT_OVERFLOW);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, -2);
	SET_NEXT_BYTE(SLJIT_NOT_OVERFLOW);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, -2);
	SET_NEXT_BYTE(SLJIT_GREATER_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_LESS_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, -2);
	SET_NEXT_BYTE(SLJIT_LESS_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)((sljit_uw)1 << ((8 * sizeof(sljit_uw)) - 1)));
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, 1);
	SET_NEXT_BYTE(SLJIT_SIG_LESS);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -1);
	SET_NEXT_BYTE(SLJIT_SIG_GREATER);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -2);
	SET_NEXT_BYTE(SLJIT_SIG_GREATER_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2);
	SET_NEXT_BYTE(SLJIT_SIG_GREATER);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x80000000);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 16);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 16);
	/* 0x80..0 on 64 bit machines, 0 on 32 bit machines. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)0xffffffff);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_OVERFLOW);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_NOT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_OVERFLOW);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_NOT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2u(compiler, SLJIT_SUBC | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUBC, SLJIT_R0, 0, SLJIT_IMM, 6, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2u(compiler, SLJIT_ADDC | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_ADDC, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 9);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, (8 * sizeof(sljit_sw)) - 1);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 0);
	SET_NEXT_BYTE(SLJIT_EQUAL);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0);
	SET_NEXT_BYTE(SLJIT_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_ASHR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	SET_NEXT_BYTE(SLJIT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_LSHR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xffffc0);
	SET_NEXT_BYTE(SLJIT_NOT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_ASHR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, shift_reg, 0);
	SET_NEXT_BYTE(SLJIT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, shift_reg, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_LSHR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R0, 0, shift_reg, 0);
	SET_NEXT_BYTE(SLJIT_NOT_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SUBC | SLJIT_SET_CARRY, SLJIT_R2, 0, SLJIT_IMM, 1, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_R2, 0);
	sljit_emit_op2u(compiler, SLJIT_SUBC | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_SUBC, SLJIT_R2, 0, SLJIT_IMM, 1, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 2, SLJIT_R2, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 2);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -34);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 0x1234);
	SET_NEXT_BYTE(SLJIT_LESS);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_IMM, 0x1234);
	SET_NEXT_BYTE(SLJIT_SIG_LESS);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x12300000000) - 43);
#else
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, -43);
#endif
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -96);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_LESS);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	SET_NEXT_BYTE(SLJIT_SIG_GREATER);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != RESULT(1), "test27 case 1 failed\n");
	FAILED(buf[1] != RESULT(2), "test27 case 2 failed\n");
	FAILED(buf[2] != 2, "test27 case 3 failed\n");
	FAILED(buf[3] != 1, "test27 case 4 failed\n");
	FAILED(buf[4] != RESULT(1), "test27 case 5 failed\n");
	FAILED(buf[5] != RESULT(2), "test27 case 6 failed\n");
	FAILED(buf[6] != 2, "test27 case 7 failed\n");
	FAILED(buf[7] != 1, "test27 case 8 failed\n");

	FAILED(buf[8] != 2, "test27 case 9 failed\n");
	FAILED(buf[9] != 1, "test27 case 10 failed\n");
	FAILED(buf[10] != 2, "test27 case 11 failed\n");
	FAILED(buf[11] != 1, "test27 case 12 failed\n");
	FAILED(buf[12] != 1, "test27 case 13 failed\n");
	FAILED(buf[13] != 2, "test27 case 14 failed\n");
	FAILED(buf[14] != 2, "test27 case 15 failed\n");
	FAILED(buf[15] != 1, "test27 case 16 failed\n");
	FAILED(buf[16] != 1, "test27 case 17 failed\n");
	FAILED(buf[17] != 2, "test27 case 18 failed\n");
	FAILED(buf[18] != 1, "test27 case 19 failed\n");
	FAILED(buf[19] != 1, "test27 case 20 failed\n");
	FAILED(buf[20] != 1, "test27 case 21 failed\n");
	FAILED(buf[21] != 2, "test27 case 22 failed\n");

	FAILED(buf[22] != RESULT(1), "test27 case 23 failed\n");
	FAILED(buf[23] != RESULT(2), "test27 case 24 failed\n");
	FAILED(buf[24] != 2, "test27 case 25 failed\n");
	FAILED(buf[25] != 1, "test27 case 26 failed\n");

	FAILED(buf[26] != 5, "test27 case 27 failed\n");
	FAILED(buf[27] != 9, "test27 case 28 failed\n");

	FAILED(buf[28] != 2, "test27 case 29 failed\n");
	FAILED(buf[29] != 1, "test27 case 30 failed\n");

	FAILED(buf[30] != 1, "test27 case 31 failed\n");
	FAILED(buf[31] != 1, "test27 case 32 failed\n");
	FAILED(buf[32] != 1, "test27 case 33 failed\n");
	FAILED(buf[33] != 1, "test27 case 34 failed\n");

	FAILED(buf[34] != 1, "test27 case 35 failed\n");
	FAILED(buf[35] != 0, "test27 case 36 failed\n");

	FAILED(buf[36] != 2, "test27 case 37 failed\n");
	FAILED(buf[37] != 1, "test27 case 38 failed\n");
	FAILED(buf[38] != 2, "test27 case 39 failed\n");
	FAILED(buf[39] != 1, "test27 case 40 failed\n");
	FAILED(buf[40] != 10, "test27 case 41 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
#undef SET_NEXT_BYTE
#undef RESULT
}

static void test28(void)
{
	/* Test mov. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_const* const1 = NULL;
	struct sljit_label* label = NULL;
	sljit_uw label_addr = 0;
	sljit_sw buf[5];

	if (verbose)
		printf("Run test28\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = -36;
	buf[1] = 8;
	buf[2] = 0;
	buf[3] = 10;
	buf[4] = 0;

	FAILED(!compiler, "cannot create compiler\n");
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 5, 5, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -234);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R4, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_MUL, SLJIT_S3, 0, SLJIT_R3, 0, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_S3, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_S3, 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_NOT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_S3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S4, 0, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw));
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_S4, 0, SLJIT_S4, 0, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_S4, 0);

	const1 = sljit_emit_const(compiler, SLJIT_S3, 0, 0);
	sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_S3, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_S3, 0, SLJIT_S3, 0, SLJIT_IMM, 100);
	label = sljit_emit_label(compiler);
	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_S3, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R4, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	label_addr = sljit_get_label_addr(label);
	sljit_set_const(sljit_get_const_addr(const1), (sljit_sw)label_addr, sljit_get_executable_offset(compiler));

	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 8, "test28 case 1 failed\n");
	FAILED(buf[1] != -1872, "test28 case 2 failed\n");
	FAILED(buf[2] != 1, "test28 case 3 failed\n");
	FAILED(buf[3] != 2, "test28 case 4 failed\n");
	FAILED(buf[4] != (sljit_sw)label_addr, "test28 case 5 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test29(void)
{
	/* Test signed/unsigned bytes and halfs. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[25];
	sljit_s32 i;

	if (verbose)
		printf("Run test29\n");

	for (i = 0; i < 25; i++)
		buf[i] = 0;

	FAILED(!compiler, "cannot create compiler\n");
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 5, 5, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_IMM, -187);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_IMM, -605);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_IMM, -56);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R4, 0, SLJIT_IMM, 0xcde5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_uw), SLJIT_R4, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_IMM, -45896);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_IMM, -1472797);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R0, 0, SLJIT_IMM, -12890);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R4, 0, SLJIT_IMM, 0x9cb0a6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_uw), SLJIT_R4, 0);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-3580429715));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-100722768662));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-1457052677972));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R4, 0, SLJIT_IMM, SLJIT_W(0xcef97a70b5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_uw), SLJIT_R4, 0);
#endif

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -187);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -605);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -56);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0xcde5);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R4, 0, SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 15 * sizeof(sljit_uw), SLJIT_R4, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -45896);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 16 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1472797);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 17 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -12890);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R0, 0, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 18 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0x9cb0a6);
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R4, 0, SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 19 * sizeof(sljit_uw), SLJIT_R4, 0);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(-3580429715));
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 20 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, SLJIT_W(-100722768662));
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 21 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, SLJIT_W(-1457052677972));
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 22 * sizeof(sljit_uw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, SLJIT_W(0xcef97a70b5));
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R4, 0, SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 23 * sizeof(sljit_uw), SLJIT_R4, 0);
#endif

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 0x9faa5);
	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_S2, 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 24 * sizeof(sljit_uw), SLJIT_S2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 69, "test29 case 1 failed\n");
	FAILED(buf[1] != -93, "test29 case 2 failed\n");
	FAILED(buf[2] != 200, "test29 case 3 failed\n");
	FAILED(buf[3] != 0xe5, "test29 case 4 failed\n");
	FAILED(buf[4] != 19640, "test29 case 5 failed\n");
	FAILED(buf[5] != -31005, "test29 case 6 failed\n");
	FAILED(buf[6] != 52646, "test29 case 7 failed\n");
	FAILED(buf[7] != 0xb0a6, "test29 case 8 failed\n");

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[8] != SLJIT_W(714537581), "test29 case 9 failed\n");
	FAILED(buf[9] != SLJIT_W(-1938520854), "test29 case 10 failed\n");
	FAILED(buf[10] != SLJIT_W(3236202668), "test29 case 11 failed\n");
	FAILED(buf[11] != SLJIT_W(0xf97a70b5), "test29 case 12 failed\n");
#endif

	FAILED(buf[12] != 69, "test29 case 13 failed\n");
	FAILED(buf[13] != -93, "test29 case 14 failed\n");
	FAILED(buf[14] != 200, "test29 case 15 failed\n");
	FAILED(buf[15] != 0xe5, "test29 case 16 failed\n");
	FAILED(buf[16] != 19640, "test29 case 17 failed\n");
	FAILED(buf[17] != -31005, "test29 case 18 failed\n");
	FAILED(buf[18] != 52646, "test29 case 19 failed\n");
	FAILED(buf[19] != 0xb0a6, "test29 case 20 failed\n");

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[20] != SLJIT_W(714537581), "test29 case 21 failed\n");
	FAILED(buf[21] != SLJIT_W(-1938520854), "test29 case 22 failed\n");
	FAILED(buf[22] != SLJIT_W(3236202668), "test29 case 23 failed\n");
	FAILED(buf[23] != SLJIT_W(0xf97a70b5), "test29 case 24 failed\n");
#endif

	FAILED(buf[24] != -91, "test29 case 25 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test30(void)
{
	/* Test unused results. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[1];

	if (verbose)
		printf("Run test30\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 5, 5, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R4, 0, SLJIT_IMM, 1);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_S1, 0, SLJIT_IMM, SLJIT_W(-0x123ffffffff));
#else
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_S1, 0, SLJIT_IMM, 1);
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S4, 0, SLJIT_IMM, 1);

	/* Some calculations with unused results. */
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_GREATER_EQUAL, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2u(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_S0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2u(compiler, SLJIT_SHL | SLJIT_SET_Z, SLJIT_S3, 0, SLJIT_R2, 0);
	sljit_emit_op2u(compiler, SLJIT_LSHR | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 5);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, 0xff);

	/* Testing that any change happens. */
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R2, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R3, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_S1, 0, SLJIT_S1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S3, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0, SLJIT_S4, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 9, "test30 case 1 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test31(void)
{
	/* Integer mul and set flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[12];
	sljit_s32 i;
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw big_word = SLJIT_W(0x7fffffff00000000);
	sljit_sw big_word2 = SLJIT_W(0x7fffffff00000012);
#else
	sljit_sw big_word = 0x7fffffff;
	sljit_sw big_word2 = 0x00000012;
#endif

	if (verbose)
		printf("Run test31\n");

	for (i = 0; i < 12; i++)
		buf[i] = 3;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 5, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_IMM, -45);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_NOT_OVERFLOW);
	sljit_emit_op2u(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_IMM, -45);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, big_word);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, -2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 33); /* Should not change flags. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0); /* Should not change flags. */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_OVERFLOW);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, -2);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_NOT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_S3, 0, SLJIT_IMM, 0x3f6b0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_S4, 0, SLJIT_IMM, 0x2a783);
	sljit_emit_op2(compiler, SLJIT_MUL32 | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_S3, 0, SLJIT_S4, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, big_word2);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_MUL32 | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, 23);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op2u(compiler, SLJIT_MUL32 | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_IMM, -23);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_NOT_OVERFLOW);
	sljit_emit_op2u(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_IMM, -23);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_NOT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 67);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, -23);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 1, "test31 case 1 failed\n");
	FAILED(buf[1] != 2, "test31 case 2 failed\n");
/* Qemu issues for 64 bit muls. */
#if !(defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[2] != 1, "test31 case 3 failed\n");
	FAILED(buf[3] != 2, "test31 case 4 failed\n");
#endif
	FAILED(buf[4] != 1, "test31 case 5 failed\n");
	FAILED((buf[5] & (sljit_sw)0xffffffff) != (sljit_sw)0x85540c10, "test31 case 6 failed\n");
	FAILED(buf[6] != 2, "test31 case 7 failed\n");
	FAILED(buf[7] != 1, "test31 case 8 failed\n");
#if !(defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[8] != 1, "test31 case 9 failed\n");
#endif
	FAILED(buf[9] != -1541, "test31 case 10 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test32(void)
{
	/* Floating point set flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;

	sljit_sw buf[16];
	union {
		sljit_f64 value;
		struct {
			sljit_s32 value1;
			sljit_s32 value2;
		} u;
	} dbuf[4];

	if (verbose)
		printf("Run test32\n");

	for (i = 0; i < 16; i++)
		buf[i] = 5;

	/* Two NaNs */
	dbuf[0].u.value1 = 0x7fffffff;
	dbuf[0].u.value2 = 0x7fffffff;
	dbuf[1].u.value1 = 0x7fffffff;
	dbuf[1].u.value2 = 0x7fffffff;
	dbuf[2].value = -13.0;
	dbuf[3].value = 27.0;

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test32 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");
	SLJIT_ASSERT(sizeof(sljit_f64) == 8 && sizeof(sljit_s32) == 4 && sizeof(dbuf[0]) == 8);

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 1, 2, 4, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_ORDERED);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_ORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_F_LESS);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_F_GREATER_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_F_GREATER);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_F_LESS_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_NOT_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_F_NOT_EQUAL);

	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR3, 0, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_F_EQUAL);

	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_ORDERED);

	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR3, 0, SLJIT_FR2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_UNORDERED);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&dbuf);

	FAILED(buf[0] != 1, "test32 case 1 failed\n");
	FAILED(buf[1] != 2, "test32 case 2 failed\n");
	FAILED(buf[2] != 2, "test32 case 3 failed\n");
	FAILED(buf[3] != 1, "test32 case 4 failed\n");
	FAILED(buf[4] != 1, "test32 case 5 failed\n");
	FAILED(buf[5] != 2, "test32 case 6 failed\n");
	FAILED(buf[6] != 2, "test32 case 7 failed\n");
	FAILED(buf[7] != 1, "test32 case 8 failed\n");
	FAILED(buf[8] != 2, "test32 case 9 failed\n");
	FAILED(buf[9] != 1, "test32 case 10 failed\n");
	FAILED(buf[10] != 2, "test32 case 11 failed\n");
	FAILED(buf[11] != 1, "test32 case 12 failed\n");
	FAILED(buf[12] != 2, "test32 case 13 failed\n");
	FAILED(buf[13] != 1, "test32 case 14 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test33(void)
{
	/* Test setting multiple flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump;
	sljit_sw buf[10];

	if (verbose)
		printf("Run test33\n");

	buf[0] = 3;
	buf[1] = 3;
	buf[2] = 3;
	buf[3] = 3;
	buf[4] = 3;
	buf[5] = 3;
	buf[6] = 3;
	buf[7] = 3;
	buf[8] = 3;
	buf[9] = 3;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 20);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 10);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_R2, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -10);
	jump = sljit_emit_jump(compiler, SLJIT_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 11);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_SIG_GREATER, SLJIT_R2, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_SIG_GREATER);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_IMM, 45);
	jump = sljit_emit_jump(compiler, SLJIT_NOT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_IMM, 55);
	sljit_set_label(jump, sljit_emit_label(compiler));

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)SLJIT_W(0x8000000000000000));
#else
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)SLJIT_W(0x80000000));
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_IMM, 33);
	jump = sljit_emit_jump(compiler, SLJIT_NOT_OVERFLOW);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_IMM, 13);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x80000000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB32 | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_NOT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_IMM, 78);
	jump = sljit_emit_jump(compiler, SLJIT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_IMM, 48);
	sljit_set_label(jump, sljit_emit_label(compiler));

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)SLJIT_W(0x8000000000000000));
#else
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)SLJIT_W(0x80000000));
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_Z | SLJIT_SET_OVERFLOW, SLJIT_R2, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_IMM, 30);
	jump = sljit_emit_jump(compiler, SLJIT_NOT_OVERFLOW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_IMM, 50);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_ZERO);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 0, "test33 case 1 failed\n");
	FAILED(buf[1] != 11, "test33 case 2 failed\n");
	FAILED(buf[2] != 1, "test33 case 3 failed\n");
	FAILED(buf[3] != 45, "test33 case 4 failed\n");
	FAILED(buf[4] != 13, "test33 case 5 failed\n");
	FAILED(buf[5] != 0, "test33 case 6 failed\n");
	FAILED(buf[6] != 0, "test33 case 7 failed\n");
	FAILED(buf[7] != 48, "test33 case 8 failed\n");
	FAILED(buf[8] != 50, "test33 case 9 failed\n");
	FAILED(buf[9] != 1, "test33 case 10 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test34(void)
{
	/* Test fast calls. */
	executable_code codeA;
	executable_code codeB;
	executable_code codeC;
	executable_code codeD;
	executable_code codeE;
	executable_code codeF;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump;
	struct sljit_label* label;
	sljit_uw addr;
	sljit_p buf[2];

	if (verbose)
		printf("Run test34\n");

	buf[0] = 0;
	buf[1] = 0;

	/* A */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 1, 5, 5, 0, 0, 2 * sizeof(sljit_p));

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_fast_enter(compiler, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 4);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_R1, 0);

	codeA.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* B */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 1, 5, 5, 0, 0, 2 * sizeof(sljit_p));

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_fast_enter(compiler, SLJIT_R4, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(codeA.code));
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_R1, 0);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_R4, 0);

	codeB.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* C */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 1, 5, 5, 0, 0, 2 * sizeof(sljit_p));

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_fast_enter(compiler, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_p));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 8);
	jump = sljit_emit_jump(compiler, SLJIT_FAST_CALL | SLJIT_REWRITABLE_JUMP);
	sljit_set_target(jump, SLJIT_FUNC_UADDR(codeB.code));
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_p));

	codeC.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* D */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 1, 5, 5, 0, 0, 2 * sizeof(sljit_p));

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_fast_enter(compiler, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_IMM, SLJIT_FUNC_ADDR(codeC.code));
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_MEM1(SLJIT_SP), 0);

	codeD.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* E */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 1, 5, 5, 0, 0, 2 * sizeof(sljit_p));

	sljit_emit_fast_enter(compiler, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 12);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_p), SLJIT_IMM, SLJIT_FUNC_ADDR(codeD.code));
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_p));
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_MEM1(SLJIT_S0), 0);

	codeE.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* F */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 5, 5, 0, 0, 2 * sizeof(sljit_p));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_IMM, SLJIT_FUNC_ADDR(codeE.code));
	label = sljit_emit_label(compiler);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	codeF.code = sljit_generate_code(compiler);
	CHECK(compiler);
	addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	FAILED(codeF.func1((sljit_sw)&buf) != 40, "test34 case 1 failed\n");
	FAILED(buf[0] != addr - SLJIT_RETURN_ADDRESS_OFFSET, "test34 case 2 failed\n");

	sljit_free_code(codeA.code, NULL);
	sljit_free_code(codeB.code, NULL);
	sljit_free_code(codeC.code, NULL);
	sljit_free_code(codeD.code, NULL);
	sljit_free_code(codeE.code, NULL);
	sljit_free_code(codeF.code, NULL);
	successful_tests++;
}

static void test35(void)
{
	/* More complicated tests for fast calls. */
	executable_code codeA;
	executable_code codeB;
	executable_code codeC;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump = NULL;
	struct sljit_label* label;
	sljit_sw executable_offset;
	sljit_uw return_addr;
	sljit_uw jump_addr = 0;
	sljit_p buf[1];

	if (verbose)
		printf("Run test35\n");

	buf[0] = 0;

	/* A */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 0, 2, 2, 0, 0, 0);

	sljit_emit_fast_enter(compiler, SLJIT_MEM0(), (sljit_sw)&buf[0]);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 5);

	jump = sljit_emit_jump(compiler, SLJIT_FAST_CALL | SLJIT_REWRITABLE_JUMP);
	sljit_set_target(jump, 0);

	label = sljit_emit_label(compiler);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_MEM0(), (sljit_sw)&buf[0]);

	codeA.code = sljit_generate_code(compiler);
	CHECK(compiler);
	executable_offset = sljit_get_executable_offset(compiler);
	jump_addr = sljit_get_jump_addr(jump);
	sljit_free_compiler(compiler);

	/* B */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, 0, 2, 2, 0, 0, 0);

	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_emit_fast_enter(compiler, SLJIT_R1, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 7);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_R1, 0);

	codeB.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	sljit_set_jump_addr(jump_addr, SLJIT_FUNC_UADDR(codeB.code), executable_offset);

	/* C */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 2, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_IMM, SLJIT_FUNC_ADDR(codeA.code));
	label = sljit_emit_label(compiler);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	codeC.code = sljit_generate_code(compiler);
	CHECK(compiler);
	return_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	FAILED(codeC.func0() != 12, "test35 case 1 failed\n");
	FAILED(buf[0] != return_addr - SLJIT_RETURN_ADDRESS_OFFSET, "test35 case 2 failed\n");

	sljit_free_code(codeA.code, NULL);
	sljit_free_code(codeB.code, NULL);
	sljit_free_code(codeC.code, NULL);
	successful_tests++;
}

static void cmp_test(struct sljit_compiler *compiler, sljit_s32 type, sljit_s32 src1, sljit_sw src1w, sljit_s32 src2, sljit_sw src2w)
{
	/* 2 = true, 1 = false */
	struct sljit_jump* jump;
	struct sljit_label* label;

	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_IMM, 2);
	jump = sljit_emit_cmp(compiler, type, src1, src1w, src2, src2w);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 1, SLJIT_IMM, 1);
	label = sljit_emit_label(compiler);
	sljit_emit_op0(compiler, SLJIT_ENDBR);
	sljit_set_label(jump, label);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
}

#define TEST_CASES	(7 + 10 + 12 + 11 + 4)
static void test36(void)
{
	/* Compare instruction. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);

	sljit_s8 buf[TEST_CASES];
	sljit_s8 compare_buf[TEST_CASES] = {
		1, 1, 2, 2, 1, 2, 2,
		1, 1, 2, 2, 2, 1, 2, 2, 1, 1,
		2, 2, 2, 1, 2, 2, 2, 2, 1, 1, 2, 2,
		2, 1, 2, 1, 1, 1, 2, 1, 2, 1, 2,
		2, 1, 1, 2
	};
	sljit_sw data[4];
	sljit_s32 i;

	if (verbose)
		printf("Run test36\n");

	FAILED(!compiler, "cannot create compiler\n");
	for (i = 0; i < TEST_CASES; ++i)
		buf[i] = 100;
	data[0] = 32;
	data[1] = -9;
	data[2] = 43;
	data[3] = -13;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 3, 2, 0, 0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 13);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 15);
	cmp_test(compiler, SLJIT_EQUAL, SLJIT_IMM, 9, SLJIT_R0, 0);
	cmp_test(compiler, SLJIT_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 3);
	cmp_test(compiler, SLJIT_EQUAL, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_IMM, -13);
	cmp_test(compiler, SLJIT_NOT_EQUAL, SLJIT_IMM, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_NOT_EQUAL | SLJIT_REWRITABLE_JUMP, SLJIT_IMM, 0, SLJIT_R0, 0);
	cmp_test(compiler, SLJIT_EQUAL, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT);
	cmp_test(compiler, SLJIT_EQUAL | SLJIT_REWRITABLE_JUMP, SLJIT_R0, 0, SLJIT_IMM, 0);

	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -8);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER, SLJIT_R0, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_SIG_LESS_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_SIG_LESS | SLJIT_REWRITABLE_JUMP, SLJIT_R0, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER_EQUAL, SLJIT_R1, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER, SLJIT_IMM, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw));
	cmp_test(compiler, SLJIT_SIG_LESS_EQUAL, SLJIT_IMM, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_IMM, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw));
	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_IMM, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw));
	cmp_test(compiler, SLJIT_SIG_LESS | SLJIT_REWRITABLE_JUMP, SLJIT_IMM, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	cmp_test(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw));
	cmp_test(compiler, SLJIT_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 8);
	cmp_test(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_IMM, -10);
	cmp_test(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_IMM, 8);
	cmp_test(compiler, SLJIT_GREATER_EQUAL, SLJIT_IMM, 8, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_GREATER_EQUAL | SLJIT_REWRITABLE_JUMP, SLJIT_IMM, 8, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_GREATER, SLJIT_IMM, 8, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_LESS_EQUAL, SLJIT_IMM, 7, SLJIT_R0, 0);
	cmp_test(compiler, SLJIT_GREATER, SLJIT_IMM, 1, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw));
	cmp_test(compiler, SLJIT_LESS_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_GREATER | SLJIT_REWRITABLE_JUMP, SLJIT_R0, 0, SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -3);
	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_R0, 0, SLJIT_IMM, -1);
	cmp_test(compiler, SLJIT_SIG_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 1);
	cmp_test(compiler, SLJIT_SIG_LESS, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_IMM, -1);
	cmp_test(compiler, SLJIT_SIG_LESS | SLJIT_REWRITABLE_JUMP, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_IMM, -1);
	cmp_test(compiler, SLJIT_SIG_LESS_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_LESS_EQUAL, SLJIT_IMM, -4, SLJIT_R0, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER, SLJIT_IMM, -1, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER | SLJIT_REWRITABLE_JUMP, SLJIT_R1, 0, SLJIT_IMM, -1);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0xf00000004));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_R1, 0);
	cmp_test(compiler, SLJIT_LESS | SLJIT_32, SLJIT_R1, 0, SLJIT_IMM, 5);
	cmp_test(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0xff0000004));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_R0, 0);
	cmp_test(compiler, SLJIT_SIG_GREATER | SLJIT_32, SLJIT_R1, 0, SLJIT_IMM, 5);
	cmp_test(compiler, SLJIT_SIG_GREATER, SLJIT_R0, 0, SLJIT_IMM, 5);
#else
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 4);
	cmp_test(compiler, SLJIT_LESS | SLJIT_32, SLJIT_R0, 0, SLJIT_IMM, 5);
	cmp_test(compiler, SLJIT_GREATER | SLJIT_32, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0xf0000004);
	cmp_test(compiler, SLJIT_SIG_GREATER | SLJIT_32, SLJIT_R0, 0, SLJIT_IMM, 5);
	cmp_test(compiler, SLJIT_SIG_LESS | SLJIT_32, SLJIT_R0, 0, SLJIT_IMM, 5);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&data);

	for (i = 0; i < TEST_CASES; ++i)
		if (SLJIT_UNLIKELY(buf[i] != compare_buf[i])) {
			printf("test36 case %d failed\n", i + 1);
			return;
		}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}
#undef TEST_CASES

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
#define BITN(n) (SLJIT_W(1) << (63 - (n)))
#define RESN(n) (n)
#else
#define BITN(n) (1 << (31 - ((n) & 0x1f)))
#define RESN(n) ((n) & 0x1f)
#endif

static void test37(void)
{
	/* Test count leading zeroes. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[9];
	sljit_s32 ibuf[2];
	sljit_s32 i;

	if (verbose)
		printf("Run test37\n");

	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 9; i++)
		buf[i] = -1;
	buf[2] = 0;
	buf[4] = BITN(13);
	ibuf[0] = -1;
	ibuf[1] = -1;
	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 1, 3, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, BITN(27));
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, BITN(47));
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_R0, 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_CLZ32, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, BITN(58));
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_CLZ, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R0, 0);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0xff08a00000));
#else
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x08a00000);
#endif
	sljit_emit_op1(compiler, SLJIT_CLZ32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s32), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_CLZ32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R0, 0);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0xffc8a00000));
#else
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0xc8a00000);
#endif
	sljit_emit_op1(compiler, SLJIT_CLZ32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&ibuf);
	FAILED(buf[0] != RESN(27), "test37 case 1 failed\n");
	FAILED(buf[1] != RESN(47), "test37 case 2 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[2] != 64, "test37 case 3 failed\n");
#else
	FAILED(buf[2] != 32, "test37 case 3 failed\n");
#endif
	FAILED(buf[3] != 0, "test37 case 4 failed\n");
	FAILED(ibuf[0] != 32, "test37 case 5 failed\n");
	FAILED(buf[4] != RESN(13), "test37 case 6 failed\n");
	FAILED(buf[5] != RESN(58), "test37 case 7 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[6] != 64, "test37 case 8 failed\n");
#else
	FAILED(buf[6] != 32, "test37 case 8 failed\n");
#endif
	FAILED(ibuf[1] != 4, "test37 case 9 failed\n");

	FAILED((buf[7] & (sljit_sw)0xffffffff) != 4, "test37 case 10 failed\n");
	FAILED((buf[8] & (sljit_sw)0xffffffff) != 0, "test37 case 11 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}
#undef BITN
#undef RESN

static void test38(void)
{
#if (defined SLJIT_UTIL_STACK && SLJIT_UTIL_STACK)
	/* Test stack utility. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* alloc1_fail;
	struct sljit_jump* alloc2_fail;
	struct sljit_jump* alloc3_fail;
	struct sljit_jump* sanity1_fail;
	struct sljit_jump* sanity2_fail;
	struct sljit_jump* sanity3_fail;
	struct sljit_jump* sanity4_fail;
	struct sljit_jump* jump;
	struct sljit_label* label;

	if (verbose)
		printf("Run test38\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8192);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 65536);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(P, W, W, P), SLJIT_IMM, SLJIT_FUNC_ADDR(sljit_allocate_stack));
	alloc1_fail = sljit_emit_cmp(compiler, SLJIT_EQUAL, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_RETURN_REG, 0);

	/* Write 8k data. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, start));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 8192);
	label = sljit_emit_label(compiler);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw));
	jump = sljit_emit_cmp(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_set_label(jump, label);

	/* Grow stack. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, end), SLJIT_IMM, 65536);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(P, P, P), SLJIT_IMM, SLJIT_FUNC_ADDR(sljit_stack_resize));
	alloc2_fail = sljit_emit_cmp(compiler, SLJIT_EQUAL, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0);
	sanity1_fail = sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_RETURN_REG, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, start));

	/* Write 64k data. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, start));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 65536);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, min_start));
	sanity2_fail = sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_R0, 0, SLJIT_R2, 0);
	label = sljit_emit_label(compiler);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw));
	jump = sljit_emit_cmp(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_set_label(jump, label);

	/* Shrink stack. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, end), SLJIT_IMM, 32768);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(P, P, P), SLJIT_IMM, SLJIT_FUNC_ADDR(sljit_stack_resize));
	alloc3_fail = sljit_emit_cmp(compiler, SLJIT_EQUAL, SLJIT_RETURN_REG, 0, SLJIT_IMM, 0);
	sanity3_fail = sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_RETURN_REG, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, start));

	/* Write 32k data. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, start));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), SLJIT_OFFSETOF(struct sljit_stack, end));
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_R1, 0, SLJIT_IMM, 32768);
	sanity4_fail = sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_R0, 0, SLJIT_R2, 0);
	label = sljit_emit_label(compiler);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_sw));
	jump = sljit_emit_cmp(compiler, SLJIT_LESS, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_set_label(jump, label);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(VOID, P, P), SLJIT_IMM, SLJIT_FUNC_ADDR(sljit_free_stack));

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 4567);

	label = sljit_emit_label(compiler);
	sljit_set_label(alloc1_fail, label);
	sljit_set_label(alloc2_fail, label);
	sljit_set_label(alloc3_fail, label);
	sljit_set_label(sanity1_fail, label);
	sljit_set_label(sanity2_fail, label);
	sljit_set_label(sanity3_fail, label);
	sljit_set_label(sanity4_fail, label);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* Just survive this. */
	FAILED(code.func0() != 4567, "test38 case 1 failed\n");

	sljit_free_code(code.code, NULL);
#endif
	successful_tests++;
}

static void test39(void)
{
	/* Test error handling. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump;

	if (verbose)
		printf("Run test39\n");

	FAILED(!compiler, "cannot create compiler\n");

	/* Such assignment should never happen in a regular program. */
	compiler->error = -3967;

	SLJIT_ASSERT(sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, W, W), 5, 5, 6, 0, 32) == -3967);
	SLJIT_ASSERT(sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R1, 0) == -3967);
	SLJIT_ASSERT(sljit_emit_op0(compiler, SLJIT_NOP) == -3967);
	SLJIT_ASSERT(sljit_emit_op0(compiler, SLJIT_ENDBR) == -3967);
	SLJIT_ASSERT(sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), 1) == -3967);
	SLJIT_ASSERT(sljit_emit_op2(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R0), 64, SLJIT_MEM1(SLJIT_S0), -64) == -3967);
	SLJIT_ASSERT(sljit_emit_fop1(compiler, SLJIT_ABS_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R1), 0) == -3967);
	SLJIT_ASSERT(sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_FR2, 0, SLJIT_MEM2(SLJIT_R0, SLJIT_S0), 0, SLJIT_FR2, 0) == -3967);
	SLJIT_ASSERT(!sljit_emit_label(compiler));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS4(W, 32, P, F32, F64));
	SLJIT_ASSERT(!jump);
	sljit_set_label(jump, (struct sljit_label*)0x123450);
	sljit_set_target(jump, 0x123450);
	jump = sljit_emit_cmp(compiler, SLJIT_SIG_LESS_EQUAL, SLJIT_R0, 0, SLJIT_R1, 0);
	SLJIT_ASSERT(!jump);
	SLJIT_ASSERT(sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_MEM1(SLJIT_R0), 8) == -3967);
	SLJIT_ASSERT(sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_OVERFLOW) == -3967);
	SLJIT_ASSERT(!sljit_emit_const(compiler, SLJIT_R0, 0, 99));

	SLJIT_ASSERT(!compiler->labels && !compiler->jumps && !compiler->consts);
	SLJIT_ASSERT(!compiler->last_label && !compiler->last_jump && !compiler->last_const);
	SLJIT_ASSERT(!compiler->buf->next && !compiler->buf->used_size);
	SLJIT_ASSERT(!compiler->abuf->next && !compiler->abuf->used_size);

	sljit_set_compiler_memory_error(compiler);
	FAILED(sljit_get_compiler_error(compiler) != -3967, "test39 case 1 failed\n");

	code.code = sljit_generate_code(compiler);
	FAILED(sljit_get_compiler_error(compiler) != -3967, "test39 case 2 failed\n");
	FAILED(!!code.code, "test39 case 3 failed\n");
	sljit_free_compiler(compiler);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	FAILED(sljit_get_compiler_error(compiler) != SLJIT_SUCCESS, "test39 case 4 failed\n");
	sljit_set_compiler_memory_error(compiler);
	FAILED(sljit_get_compiler_error(compiler) != SLJIT_ERR_ALLOC_FAILED, "test39 case 5 failed\n");
	sljit_free_compiler(compiler);

	successful_tests++;
}

static void test40(void)
{
	/* Test emit_op_flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[10];

	if (verbose)
		printf("Run test40\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = -100;
	buf[1] = -100;
	buf[2] = -100;
	buf[3] = -8;
	buf[4] = -100;
	buf[5] = -100;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = -100;
	buf[9] = -100;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 4, 0, 0, sizeof(sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -5);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_IMM, -6, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x123456);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_R1, 0, SLJIT_SIG_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -13);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_IMM, -13, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_NOT_EQUAL);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_IMM, -13, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 2, SLJIT_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -13);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 3);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_WORD_SHIFT, SLJIT_SIG_LESS);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -8);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 33);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_R2, 0, SLJIT_GREATER);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_S1, 0, SLJIT_EQUAL);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, 0x88);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_S3, 0, SLJIT_NOT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 4, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 5, SLJIT_S3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x84);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_IMM, 0x180, SLJIT_R0, 0);
	sljit_emit_op_flags(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 6, SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 7, SLJIT_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_flags(compiler, SLJIT_OR | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_NOT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 8, SLJIT_NOT_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x123456);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_R0, 0, SLJIT_GREATER);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw) * 9, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, 0xbaddead);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 0xbaddead, "test40 case 1 failed\n");
	FAILED(buf[0] != 0x123457, "test40 case 2 failed\n");
	FAILED(buf[1] != 1, "test40 case 3 failed\n");
	FAILED(buf[2] != 0, "test40 case 4 failed\n");
	FAILED(buf[3] != -7, "test40 case 5 failed\n");
	FAILED(buf[4] != 0, "test40 case 6 failed\n");
	FAILED(buf[5] != 0x89, "test40 case 7 failed\n");
	FAILED(buf[6] != 0, "test40 case 8 failed\n");
	FAILED(buf[7] != 1, "test40 case 9 failed\n");
	FAILED(buf[8] != 1, "test40 case 10 failed\n");
	FAILED(buf[9] != 0x123457, "test40 case 11 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test41(void)
{
	/* Test inline assembly. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;
	sljit_f64 buf[3];
#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	sljit_u8 inst[16];
#elif (defined SLJIT_CONFIG_X86_64 && SLJIT_CONFIG_X86_64)
	sljit_u8 inst[16];
	sljit_s32 reg;
#else
	sljit_u32 inst;
#endif

	if (verbose)
		printf("Run test41\n");

#if !(defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	SLJIT_ASSERT(sljit_has_cpu_feature(SLJIT_HAS_VIRTUAL_REGISTERS) == 0);
#endif

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++) {
#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
		if (SLJIT_R(i) >= SLJIT_R3 && SLJIT_R(i) <= SLJIT_R8) {
			SLJIT_ASSERT(sljit_get_register_index(SLJIT_R(i)) == -1);
			continue;
		}
#endif
		SLJIT_ASSERT(sljit_get_register_index(SLJIT_R(i)) >= 0 && sljit_get_register_index(SLJIT_R(i)) < 64);
	}

	FAILED(!compiler, "cannot create compiler\n");
	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, W, W), 3, 3, 0, 0, 0);

	/* Returns with the sum of SLJIT_S0 and SLJIT_S1. */
#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	/* lea SLJIT_RETURN_REG, [SLJIT_S0, SLJIT_S1] */
	inst[0] = 0x48;
	inst[1] = 0x8d;
	inst[2] = (sljit_u8)(0x04 | ((sljit_get_register_index(SLJIT_RETURN_REG) & 0x7) << 3));
	inst[3] = (sljit_u8)((sljit_get_register_index(SLJIT_S0) & 0x7)
		| ((sljit_get_register_index(SLJIT_S1) & 0x7) << 3));
	sljit_emit_op_custom(compiler, inst, 4);
#elif (defined SLJIT_CONFIG_X86_64 && SLJIT_CONFIG_X86_64)
	/* lea SLJIT_RETURN_REG, [SLJIT_S0, SLJIT_S1] */
	inst[0] = 0x48; /* REX_W */
	inst[1] = 0x8d;
	reg = sljit_get_register_index(SLJIT_RETURN_REG);
	inst[2] = (sljit_u8)(0x04 | ((reg & 0x7) << 3));
	if (reg > 7)
		inst[0] |= 0x04; /* REX_R */
	reg = sljit_get_register_index(SLJIT_S0);
	inst[3] = (sljit_u8)(reg & 0x7);
	if (reg > 7)
		inst[0] |= 0x01; /* REX_B */
	reg = sljit_get_register_index(SLJIT_S1);
	inst[3] = (sljit_u8)(inst[3] | ((reg & 0x7) << 3));
	if (reg > 7)
		inst[0] |= 0x02; /* REX_X */
	sljit_emit_op_custom(compiler, inst, 4);
#elif (defined SLJIT_CONFIG_ARM_V5 && SLJIT_CONFIG_ARM_V5) || (defined SLJIT_CONFIG_ARM_V7 && SLJIT_CONFIG_ARM_V7)
	/* add rd, rn, rm */
	inst = 0xe0800000 | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 12)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 16)
		| (sljit_u32)sljit_get_register_index(SLJIT_S1);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_ARM_THUMB2 && SLJIT_CONFIG_ARM_THUMB2)
	/* add rd, rn, rm */
	inst = 0xeb000000 | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 8)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 16)
		| (sljit_u32)sljit_get_register_index(SLJIT_S1);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	/* add rd, rn, rm */
	inst = 0x8b000000 | (sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 5)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S1) << 16);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_PPC && SLJIT_CONFIG_PPC)
	/* add rD, rA, rB */
	inst = (31 << 26) | (266 << 1) | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 21)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 16)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S1) << 11);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_MIPS_32 && SLJIT_CONFIG_MIPS_32)
	/* addu rd, rs, rt */
	inst = 33 | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 11)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 21)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S1) << 16);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_MIPS_64 && SLJIT_CONFIG_MIPS_64)
	/* daddu rd, rs, rt */
	inst = 45 | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 11)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 21)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S1) << 16);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_RISCV && SLJIT_CONFIG_RISCV)
	/* add rd, rs1, rs2 */
	inst = 0x33 | (0 << 12) | (0 << 25) | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 7)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 15)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S1) << 20);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_SPARC_32 && SLJIT_CONFIG_SPARC_32)
	/* add rd, rs1, rs2 */
	inst = (0x2u << 30) | ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 25)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 14)
		| (sljit_u32)sljit_get_register_index(SLJIT_S1);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_S390X && SLJIT_CONFIG_S390X)
	/* agrk rd, rs1, rs2 */
	inst = (0xb9e8u << 16)
		| ((sljit_u32)sljit_get_register_index(SLJIT_RETURN_REG) << 4)
		| ((sljit_u32)sljit_get_register_index(SLJIT_S0) << 12)
		| (sljit_u32)sljit_get_register_index(SLJIT_S1);
	sljit_emit_op_custom(compiler, &inst, sizeof(inst));
#else
	inst = 0;
	sljit_emit_op_custom(compiler, &inst, 0);
#endif

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func2(32, -11) != 21, "test41 case 1 failed\n");
	FAILED(code.func2(1000, 234) != 1234, "test41 case 2 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(code.func2(SLJIT_W(0x20f0a04090c06070), SLJIT_W(0x020f0a04090c0607)) != SLJIT_W(0x22ffaa4499cc6677), "test41 case 3 failed\n");
#endif

	sljit_free_code(code.code, NULL);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		buf[0] = 13.5;
		buf[1] = -2.25;
		buf[2] = 0.0;

		compiler = sljit_create_compiler(NULL, NULL);
		sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, 2, 0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
		/* addsd x, xm */
		inst[0] = 0xf2;
		inst[1] = 0x0f;
		inst[2] = 0x58;
		inst[3] = (sljit_u8)(0xc0 | (sljit_get_float_register_index(SLJIT_FR0) << 3)
			| sljit_get_float_register_index(SLJIT_FR1));
		sljit_emit_op_custom(compiler, inst, 4);
#elif (defined SLJIT_CONFIG_X86_64 && SLJIT_CONFIG_X86_64)
		/* addsd x, xm */
		if (sljit_get_float_register_index(SLJIT_FR0) > 7 || sljit_get_float_register_index(SLJIT_FR1) > 7) {
			inst[0] = 0;
			if (sljit_get_float_register_index(SLJIT_FR0) > 7)
				inst[0] |= 0x04; /* REX_R */
			if (sljit_get_float_register_index(SLJIT_FR1) > 7)
				inst[0] |= 0x01; /* REX_B */
			inst[1] = 0xf2;
			inst[2] = 0x0f;
			inst[3] = 0x58;
			inst[4] = (sljit_u8)(0xc0 | ((sljit_get_float_register_index(SLJIT_FR0) & 0x7) << 3)
				| (sljit_get_float_register_index(SLJIT_FR1) & 0x7));
			sljit_emit_op_custom(compiler, inst, 5);
		}
		else {
			inst[0] = 0xf2;
			inst[1] = 0x0f;
			inst[2] = 0x58;
			inst[3] = (sljit_u8)(0xc0 | (sljit_get_float_register_index(SLJIT_FR0) << 3)
				| sljit_get_float_register_index(SLJIT_FR1));
			sljit_emit_op_custom(compiler, inst, 4);
		}
#elif (defined SLJIT_CONFIG_ARM_32 && SLJIT_CONFIG_ARM_32)
		/* vadd.f64 dd, dn, dm */
		inst = 0xee300b00 | (((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) >> 1) << 12)
			| (((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) >> 1) << 16)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR1) >> 1);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
		/* fadd rd, rn, rm */
		inst = 0x1e602800 | (sljit_u32)sljit_get_float_register_index(SLJIT_FR0)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 5)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR1) << 16);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_PPC && SLJIT_CONFIG_PPC)
		/* fadd frD, frA, frB */
		inst = (63u << 26) | (21u << 1) | ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 21)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 16)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR1) << 11);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_MIPS && SLJIT_CONFIG_MIPS)
		/* add.d fd, fs, ft */
		inst = (17u << 26) | (17u << 21) | ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 6)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 11)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR1) << 16);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_RISCV && SLJIT_CONFIG_RISCV)
		/* fadd.d rd, rs1, rs2 */
		inst = (0x1u << 25) | (0x7u << 12) | (0x53u)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 7)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 15)
			| (sljit_u32)sljit_get_float_register_index(SLJIT_FR1) << 20;
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_SPARC_32 && SLJIT_CONFIG_SPARC_32)
		/* faddd rd, rs1, rs2 */
		inst = (0x2u << 30) | (0x34u << 19) | (0x42u << 5)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 25)
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 14)
			| (sljit_u32)sljit_get_float_register_index(SLJIT_FR1);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_S390X && SLJIT_CONFIG_S390X)
		/* adbr r1, r2 */
		inst = 0xb31a0000
			| ((sljit_u32)sljit_get_float_register_index(SLJIT_FR0) << 4)
			| (sljit_u32)sljit_get_float_register_index(SLJIT_FR1);
		sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#endif
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func1((sljit_sw)&buf);
		FAILED(buf[2] != 11.25, "test41 case 4 failed\n");

		sljit_free_code(code.code, NULL);
	}

	successful_tests++;
}

static void test42(void)
{
	/* Test long multiply and division. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;
	sljit_sw buf[7 + 4 + 8 + 8];

	if (verbose)
		printf("Run test42\n");

	FAILED(!compiler, "cannot create compiler\n");
	for (i = 0; i < 7 + 4 + 8 + 8; i++)
		buf[i] = -1;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 5, 5, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -0x1fb308a);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0xf50c873);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R4, 0, SLJIT_IMM, 0x8a0475b);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 0x9dc849b);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -0x7c69a35);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, 0x5a4d0c4);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S4, 0, SLJIT_IMM, 0x9a3b06d);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-0x5dc4f897b8cd67f5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3f8b5c026cb088df));
	sljit_emit_op0(compiler, SLJIT_LMUL_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-0x5dc4f897b8cd67f5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3f8b5c026cb088df));
	sljit_emit_op0(compiler, SLJIT_LMUL_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-0x5dc4f897b8cd67f5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3f8b5c026cb088df));
	sljit_emit_op0(compiler, SLJIT_DIVMOD_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-0x5dc4f897b8cd67f5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3f8b5c026cb088df));
	sljit_emit_op0(compiler, SLJIT_DIVMOD_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x5cf783d3cf0a74b0));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3d5df42d03a28fc7));
	sljit_emit_op0(compiler, SLJIT_DIVMOD_U32);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 15 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 16 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x371df5197ba26a28));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x46c78a5cfd6a420c));
	sljit_emit_op0(compiler, SLJIT_DIVMOD_S32);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 17 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 18 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)SLJIT_W(0xc456f048c28a611b));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x3d4af2c543));
	sljit_emit_op0(compiler, SLJIT_DIV_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 19 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 20 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(-0x720fa4b74c329b14));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0xa64ae42b7d6));
	sljit_emit_op0(compiler, SLJIT_DIV_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 21 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 22 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x4af51c027b34));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(0x9ba4ff2906b14));
	sljit_emit_op0(compiler, SLJIT_DIV_U32);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 23 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 24 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0xc40b58a3f20d));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, SLJIT_W(-0xa63c923));
	sljit_emit_op0(compiler, SLJIT_DIV_S32);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 25 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 26 * sizeof(sljit_sw), SLJIT_R1, 0);

#else
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -0x58cd67f5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x3cb088df);
	sljit_emit_op0(compiler, SLJIT_LMUL_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -0x58cd67f5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x3cb088df);
	sljit_emit_op0(compiler, SLJIT_LMUL_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -0x58cd67f5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x3cb088df);
	sljit_emit_op0(compiler, SLJIT_DIVMOD_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -0x58cd67f5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x3cb088df);
	sljit_emit_op0(compiler, SLJIT_DIVMOD_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0xcf0a74b0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0x03a28fc7);
	sljit_emit_op0(compiler, SLJIT_DIVMOD_U32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 15 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 16 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x7ba26a28);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)0xfd6a420c);
	sljit_emit_op0(compiler, SLJIT_DIVMOD_S32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 17 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 18 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x9d4b7036);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0xb86d0);
	sljit_emit_op0(compiler, SLJIT_DIV_UW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 19 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 20 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -0x58b0692c);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0xd357);
	sljit_emit_op0(compiler, SLJIT_DIV_SW);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 21 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 22 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x1c027b34);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)0xf2906b14);
	sljit_emit_op0(compiler, SLJIT_DIV_U32);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 23 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 24 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0x58a3f20d);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -0xa63c923);
	sljit_emit_op0(compiler, SLJIT_DIV_S32);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 25 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 26 * sizeof(sljit_sw), SLJIT_R1, 0);
#endif

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_S3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_S4, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != -0x1fb308a, "test42 case 1 failed\n");
	FAILED(buf[1] != 0xf50c873, "test42 case 2 failed\n");
	FAILED(buf[2] != 0x8a0475b, "test42 case 3 failed\n");
	FAILED(buf[3] != 0x9dc849b, "test42 case 4 failed\n");
	FAILED(buf[4] != -0x7c69a35, "test42 case 5 failed\n");
	FAILED(buf[5] != 0x5a4d0c4, "test42 case 6 failed\n");
	FAILED(buf[6] != 0x9a3b06d, "test42 case 7 failed\n");

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[7] != SLJIT_W(-4388959407985636971), "test42 case 8 failed\n");
	FAILED(buf[8] != SLJIT_W(2901680654366567099), "test42 case 9 failed\n");
	FAILED(buf[9] != SLJIT_W(-4388959407985636971), "test42 case 10 failed\n");
	FAILED(buf[10] != SLJIT_W(-1677173957268872740), "test42 case 11 failed\n");
	FAILED(buf[11] != SLJIT_W(2), "test42 case 12 failed\n");
	FAILED(buf[12] != SLJIT_W(2532236178951865933), "test42 case 13 failed\n");
	FAILED(buf[13] != SLJIT_W(-1), "test42 case 14 failed\n");
	FAILED(buf[14] != SLJIT_W(-2177944059851366166), "test42 case 15 failed\n");
#else
	FAILED(buf[7] != -1587000939, "test42 case 8 failed\n");
	FAILED(buf[8] != 665003983, "test42 case 9 failed\n");
	FAILED(buf[9] != -1587000939, "test42 case 10 failed\n");
	FAILED(buf[10] != -353198352, "test42 case 11 failed\n");
	FAILED(buf[11] != 2, "test42 case 12 failed\n");
	FAILED(buf[12] != 768706125, "test42 case 13 failed\n");
	FAILED(buf[13] != -1, "test42 case 14 failed\n");
	FAILED(buf[14] != -471654166, "test42 case 15 failed\n");
#endif

	FAILED(buf[15] != 56, "test42 case 16 failed\n");
	FAILED(buf[16] != 58392872, "test42 case 17 failed\n");
	FAILED(buf[17] != -47, "test42 case 18 failed\n");
	FAILED(buf[18] != 35949148, "test42 case 19 failed\n");

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(buf[19] != SLJIT_W(0x3340bfc), "test42 case 20 failed\n");
	FAILED(buf[20] != SLJIT_W(0x3d4af2c543), "test42 case 21 failed\n");
	FAILED(buf[21] != SLJIT_W(-0xaf978), "test42 case 22 failed\n");
	FAILED(buf[22] != SLJIT_W(0xa64ae42b7d6), "test42 case 23 failed\n");
#else
	FAILED(buf[19] != SLJIT_W(0xda5), "test42 case 20 failed\n");
	FAILED(buf[20] != SLJIT_W(0xb86d0), "test42 case 21 failed\n");
	FAILED(buf[21] != SLJIT_W(-0x6b6e), "test42 case 22 failed\n");
	FAILED(buf[22] != SLJIT_W(0xd357), "test42 case 23 failed\n");
#endif

	FAILED(buf[23] != 0x0, "test42 case 24 failed\n");
	FAILED(buf[24] != (sljit_sw)0xf2906b14, "test42 case 25 failed\n");
	FAILED(buf[25] != -0x8, "test42 case 26 failed\n");
	FAILED(buf[26] != -0xa63c923, "test42 case 27 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test43(void)
{
	/* Test floating point compare. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump;

	union {
		sljit_f64 value;
		struct {
			sljit_u32 value1;
			sljit_u32 value2;
		} u;
	} dbuf[4];

	if (verbose)
		printf("Run test43\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test43 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");

	dbuf[0].value = 12.125;
	/* a NaN */
	dbuf[1].u.value1 = 0x7fffffff;
	dbuf[1].u.value2 = 0x7fffffff;
	dbuf[2].value = -13.5;
	dbuf[3].value = 12.125;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 1, 1, 3, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	/* dbuf[0] < dbuf[2] -> -2 */
	jump = sljit_emit_fcmp(compiler, SLJIT_F_GREATER_EQUAL, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_F64_SHIFT);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, -2);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	/* dbuf[0] and dbuf[1] is not NaN -> 5 */
	jump = sljit_emit_fcmp(compiler, SLJIT_UNORDERED, SLJIT_MEM0(), (sljit_sw)&dbuf[1], SLJIT_FR1, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 5);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, 11);
	/* dbuf[0] == dbuf[3] -> 11 */
	jump = sljit_emit_fcmp(compiler, SLJIT_F_EQUAL, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_FR2, 0);

	/* else -> -17 */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, -17);
	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&dbuf) != 11, "test43 case 1 failed\n");
	dbuf[3].value = 12;
	FAILED(code.func1((sljit_sw)&dbuf) != -17, "test43 case 2 failed\n");
	dbuf[1].value = 0;
	FAILED(code.func1((sljit_sw)&dbuf) != 5, "test43 case 3 failed\n");
	dbuf[2].value = 20;
	FAILED(code.func1((sljit_sw)&dbuf) != -2, "test43 case 4 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test44(void)
{
	/* Test mov. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	void *buf[5];

	if (verbose)
		printf("Run test44\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = buf + 2;
	buf[1] = NULL;
	buf[2] = NULL;
	buf[3] = NULL;
	buf[4] = NULL;
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(P, P), 3, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_P, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV_P, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_p), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_p));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV_P, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_POINTER_SHIFT, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_p));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 3 << SLJIT_POINTER_SHIFT);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_P, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_p));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1 << SLJIT_POINTER_SHIFT);
	sljit_emit_op1(compiler, SLJIT_MOV_P, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 2, SLJIT_R0, 0);

	sljit_emit_return(compiler, SLJIT_MOV_P, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != (sljit_sw)(buf + 2), "test44 case 1 failed\n");
	FAILED(buf[1] != buf + 2, "test44 case 2 failed\n");
	FAILED(buf[2] != buf + 3, "test44 case 3 failed\n");
	FAILED(buf[3] != buf + 4, "test44 case 4 failed\n");
	FAILED(buf[4] != buf + 2, "test44 case 5 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test45(void)
{
	/* Test single precision floating point. */

	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_f32 buf[12];
	sljit_sw buf2[6];
	struct sljit_jump* jump;

	if (verbose)
		printf("Run test45\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test45 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = 5.5;
	buf[1] = -7.25;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 8.75;
	buf[8] = 0;
	buf[9] = 16.5;
	buf[10] = 0;
	buf[11] = 0;

	buf2[0] = -1;
	buf2[1] = -1;
	buf2[2] = -1;
	buf2[3] = -1;
	buf2[4] = -1;
	buf2[5] = -1;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 3, 2, 6, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f32), SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_FR1, 0, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f32), SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f32), SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f32), SLJIT_FR4, 0);

	sljit_emit_fop2(compiler, SLJIT_ADD_F32, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f32), SLJIT_FR0, 0);
	sljit_emit_fop2(compiler, SLJIT_SUB_F32, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f32), SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F32, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f32), SLJIT_FR0, 0, SLJIT_FR0, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f32), SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f32), SLJIT_FR2, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 0x3d0ac);
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R0), 0x3d0ac);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 0x3d0ac + sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R0), -0x3d0ac);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw), SLJIT_F_LESS);
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_GREATER_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_F_GREATER_EQUAL);

	jump = sljit_emit_fcmp(compiler, SLJIT_F_LESS_EQUAL | SLJIT_32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_IMM, 7);
	sljit_set_label(jump, sljit_emit_label(compiler));

	jump = sljit_emit_fcmp(compiler, SLJIT_F_GREATER | SLJIT_32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_FR2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_IMM, 6);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&buf2);
	FAILED(buf[2] != -5.5, "test45 case 1 failed\n");
	FAILED(buf[3] != 7.25, "test45 case 2 failed\n");
	FAILED(buf[4] != 7.25, "test45 case 3 failed\n");
	FAILED(buf[5] != -5.5, "test45 case 4 failed\n");
	FAILED(buf[6] != -1.75, "test45 case 5 failed\n");
	FAILED(buf[7] != 16.0, "test45 case 6 failed\n");
	FAILED(buf[8] != 30.25, "test45 case 7 failed\n");
	FAILED(buf[9] != 3, "test45 case 8 failed\n");
	FAILED(buf[10] != -5.5, "test45 case 9 failed\n");
	FAILED(buf[11] != 7.25, "test45 case 10 failed\n");
	FAILED(buf2[0] != 1, "test45 case 11 failed\n");
	FAILED(buf2[1] != 2, "test45 case 12 failed\n");
	FAILED(buf2[2] != 2, "test45 case 13 failed\n");
	FAILED(buf2[3] != 1, "test45 case 14 failed\n");
	FAILED(buf2[4] != 7, "test45 case 15 failed\n");
	FAILED(buf2[5] != -1, "test45 case 16 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test46(void)
{
	/* Test sljit_emit_op_flags with 32 bit operations. */

	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 buf[24];
	sljit_sw buf2[6];
	sljit_s32 i;

	if (verbose)
		printf("Run test46\n");

	for (i = 0; i < 24; ++i)
		buf[i] = -17;
	buf[16] = 0;
	for (i = 0; i < 6; ++i)
		buf2[i] = -13;
	buf2[4] = -124;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 3, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -7);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_R2, 0, SLJIT_IMM, 13);
	sljit_emit_op_flags(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&buf, SLJIT_LESS);
	sljit_emit_op_flags(compiler, SLJIT_AND32, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_s32), SLJIT_NOT_ZERO);

	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R2, 0, SLJIT_IMM, -7);
	sljit_emit_op_flags(compiler, SLJIT_AND32 | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_s32), SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_AND32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_s32), SLJIT_NOT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x1235);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 0x1235);
	sljit_emit_op_flags(compiler, SLJIT_AND32 | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_ZERO);
	sljit_emit_op_flags(compiler, SLJIT_AND32, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_s32), SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_s32), SLJIT_R0, 0);

	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R2, 0, SLJIT_IMM, -7);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 12);
	sljit_emit_op_flags(compiler, SLJIT_MOV32, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 2, SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_EQUAL);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_s32), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 16);
	sljit_emit_op_flags(compiler, SLJIT_AND32 | SLJIT_SET_Z, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 2, SLJIT_EQUAL);
	sljit_emit_op_flags(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), 18 * sizeof(sljit_s32), SLJIT_NOT_EQUAL);

	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R2, 0, SLJIT_IMM, -7);
	sljit_emit_op_flags(compiler, SLJIT_XOR32 | SLJIT_SET_Z, SLJIT_MEM1(SLJIT_S0), 20 * sizeof(sljit_s32), SLJIT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 39);
	sljit_emit_op_flags(compiler, SLJIT_XOR32, SLJIT_R0, 0, SLJIT_NOT_ZERO);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), 22 * sizeof(sljit_s32), SLJIT_R0, 0);

	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R2, 0, SLJIT_IMM, -7);
	sljit_emit_op_flags(compiler, SLJIT_AND, SLJIT_MEM0(), (sljit_sw)&buf2, SLJIT_GREATER);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R2, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_flags(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_SIG_LESS);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_R2, 0, SLJIT_IMM, 5);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_LESS);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_NOT_EQUAL);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R2, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_op_flags(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_WORD_SHIFT, SLJIT_SIG_LESS);
	sljit_emit_op_flags(compiler, SLJIT_OR, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_ZERO);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_op_flags(compiler, SLJIT_XOR, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_GREATER);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&buf2);
	FAILED(buf[0] != 0, "test46 case 1 failed\n");
	FAILED(buf[1] != -17, "test46 case 2 failed\n");
	FAILED(buf[2] != 1, "test46 case 3 failed\n");
	FAILED(buf[3] != -17, "test46 case 4 failed\n");
	FAILED(buf[4] != 1, "test46 case 5 failed\n");
	FAILED(buf[5] != -17, "test46 case 6 failed\n");
	FAILED(buf[6] != 1, "test46 case 7 failed\n");
	FAILED(buf[7] != -17, "test46 case 8 failed\n");
	FAILED(buf[8] != 0, "test46 case 9 failed\n");
	FAILED(buf[9] != -17, "test46 case 10 failed\n");
	FAILED(buf[10] != 1, "test46 case 11 failed\n");
	FAILED(buf[11] != -17, "test46 case 12 failed\n");
	FAILED(buf[12] != 1, "test46 case 13 failed\n");
	FAILED(buf[13] != -17, "test46 case 14 failed\n");
	FAILED(buf[14] != 1, "test46 case 15 failed\n");
	FAILED(buf[15] != -17, "test46 case 16 failed\n");
	FAILED(buf[16] != 0, "test46 case 17 failed\n");
	FAILED(buf[17] != -17, "test46 case 18 failed\n");
	FAILED(buf[18] != 0, "test46 case 19 failed\n");
	FAILED(buf[19] != -17, "test46 case 20 failed\n");
	FAILED(buf[20] != -18, "test46 case 21 failed\n");
	FAILED(buf[21] != -17, "test46 case 22 failed\n");
	FAILED(buf[22] != 38, "test46 case 23 failed\n");
	FAILED(buf[23] != -17, "test46 case 24 failed\n");

	FAILED(buf2[0] != 0, "test46 case 25 failed\n");
	FAILED(buf2[1] != 1, "test46 case 26 failed\n");
	FAILED(buf2[2] != 0, "test46 case 27 failed\n");
	FAILED(buf2[3] != 1, "test46 case 28 failed\n");
	FAILED(buf2[4] != -123, "test46 case 29 failed\n");
	FAILED(buf2[5] != -14, "test46 case 30 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test47(void)
{
	/* Test jump optimizations. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[3];

	if (verbose)
		printf("Run test47\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x3a5c6f);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 3);
	sljit_set_target(sljit_emit_jump(compiler, SLJIT_LESS), 0x11223344);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0xd37c10);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_set_target(sljit_emit_jump(compiler, SLJIT_LESS), SLJIT_W(0x112233445566));
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x59b48e);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_set_target(sljit_emit_jump(compiler, SLJIT_LESS), SLJIT_W(0x1122334455667788));
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != 0x59b48e, "test47 case 1 failed\n");
	FAILED(buf[0] != 0x3a5c6f, "test47 case 2 failed\n");
	FAILED(buf[1] != 0xd37c10, "test47 case 3 failed\n");
	FAILED(buf[2] != 0x59b48e, "test47 case 4 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test48(void)
{
	/* Test floating point conversions. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	int i;
	sljit_f64 dbuf[10];
	sljit_f32 sbuf[10];
	sljit_sw wbuf[10];
	sljit_s32 ibuf[10];

	if (verbose)
		printf("Run test48\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test48 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");
	for (i = 0; i < 10; i++) {
		dbuf[i] = 0.0;
		sbuf[i] = 0.0;
		wbuf[i] = 0;
		ibuf[i] = 0;
	}

	dbuf[0] = 123.5;
	dbuf[1] = -367;
	dbuf[2] = 917.75;

	sbuf[0] = 476.25;
	sbuf[1] = -1689.75;

	wbuf[0] = 2345;

	ibuf[0] = 312;
	ibuf[1] = -9324;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 3, 3, 6, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&sbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)&ibuf);

	/* sbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_F64, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 3);
	/* sbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_F64, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_F32_SHIFT, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S1), 0);
	/* dbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR4, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_F32, SLJIT_FR2, 0, SLJIT_FR3, 0);
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR2, 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR3, 0);

	/* wbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F64, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F64, SLJIT_R0, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), SLJIT_F64_SHIFT);
	/* wbuf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S1), 0);
	/* wbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F32, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_sw), SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_FR0, 0, SLJIT_FR5, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 4);
	/* wbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F32, SLJIT_MEM2(SLJIT_S2, SLJIT_R1), SLJIT_WORD_SHIFT, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	/* ibuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F64, SLJIT_MEM1(SLJIT_R2), 2 * sizeof(sljit_s32), SLJIT_FR4, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F32, SLJIT_R0, 0, SLJIT_FR1, 0);
	/* ibuf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_MEM1(SLJIT_R2), 3 * sizeof(sljit_s32), SLJIT_R0, 0);

	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_SW, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S2), 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_SW, SLJIT_FR2, 0, SLJIT_IMM, -6213);
	/* dbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_FR2, 0);
	/* dbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_S32, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f64), SLJIT_MEM0(), (sljit_sw)&ibuf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_s32));
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_S32, SLJIT_FR1, 0, SLJIT_R0, 0);
	/* dbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f64), SLJIT_FR1, 0);
	/* dbuf[9] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_SW, SLJIT_MEM0(), (sljit_sw)(dbuf + 9), SLJIT_IMM, -77);
	/* sbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_SW, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_f32), SLJIT_IMM, -123);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 7190);
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_SW, SLJIT_FR3, 0, SLJIT_R0, 0);
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_f32), SLJIT_FR3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 123);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_R2, 0, SLJIT_IMM, 123 * sizeof(sljit_s32));
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_S32, SLJIT_FR1, 0, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 2);
	/* sbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_f32), SLJIT_FR1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 3812);
	/* sbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_S32, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), SLJIT_F32_SHIFT, SLJIT_R1, 0);
	/* sbuf[9] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_SW, SLJIT_MEM0(), (sljit_sw)(sbuf + 9), SLJIT_IMM, -79);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();
	FAILED(dbuf[3] != 476.25, "test48 case 1 failed\n");
	FAILED(dbuf[4] != 476.25, "test48 case 2 failed\n");
	FAILED(dbuf[5] != 2345.0, "test48 case 3 failed\n");
	FAILED(dbuf[6] != -6213.0, "test48 case 4 failed\n");
	FAILED(dbuf[7] != 312.0, "test48 case 5 failed\n");
	FAILED(dbuf[8] != -9324.0, "test48 case 6 failed\n");
	FAILED(dbuf[9] != -77.0, "test48 case 7 failed\n");

	FAILED(sbuf[2] != 123.5, "test48 case 8 failed\n");
	FAILED(sbuf[3] != 123.5, "test48 case 9 failed\n");
	FAILED(sbuf[4] != 476.25, "test48 case 10 failed\n");
	FAILED(sbuf[5] != -123, "test48 case 11 failed\n");
	FAILED(sbuf[6] != 7190, "test48 case 12 failed\n");
	FAILED(sbuf[7] != 312, "test48 case 13 failed\n");
	FAILED(sbuf[8] != 3812, "test48 case 14 failed\n");
	FAILED(sbuf[9] != -79.0, "test48 case 15 failed\n");

	FAILED(wbuf[1] != -367, "test48 case 16 failed\n");
	FAILED(wbuf[2] != 917, "test48 case 17 failed\n");
	FAILED(wbuf[3] != 476, "test48 case 18 failed\n");
	FAILED(wbuf[4] != -476, "test48 case 19 failed\n");

	FAILED(ibuf[2] != -917, "test48 case 20 failed\n");
	FAILED(ibuf[3] != -1689, "test48 case 21 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test49(void)
{
	/* Test floating point conversions. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	int i;
	sljit_f64 dbuf[10];
	sljit_f32 sbuf[9];
	sljit_sw wbuf[9];
	sljit_s32 ibuf[9];
	sljit_s32* dbuf_ptr = (sljit_s32*)dbuf;
	sljit_s32* sbuf_ptr = (sljit_s32*)sbuf;

	if (verbose)
		printf("Run test49\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test49 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 9; i++) {
		dbuf_ptr[i << 1] = -1;
		dbuf_ptr[(i << 1) + 1] = -1;
		sbuf_ptr[i] = -1;
		wbuf[i] = -1;
		ibuf[i] = -1;
	}

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	dbuf[9] = (sljit_f64)SLJIT_W(0x1122334455);
#endif
	dbuf[0] = 673.75;
	sbuf[0] = -879.75;
	wbuf[0] = 345;
	ibuf[0] = -249;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 3, 3, 3, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&sbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)&ibuf);

	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_F32, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S1), 0);
	/* sbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_F64, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S0), 0);
	/* wbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F64, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 0);
	/* wbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F32, SLJIT_MEM1(SLJIT_S2), 4 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S1), 0);
	/* ibuf[2] */
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F64, SLJIT_MEM1(SLJIT_R2), 2 * sizeof(sljit_s32), SLJIT_MEM1(SLJIT_S0), 0);
	/* ibuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F32, SLJIT_MEM1(SLJIT_R2), 4 * sizeof(sljit_s32), SLJIT_MEM1(SLJIT_S1), 0);
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_SW, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S2), 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_SW, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S2), 0);
	/* dbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_S32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_R2), 0);
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_S32, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R2), 0);

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F64, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f64));
	/* wbuf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 8 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F64, SLJIT_R0, 0, SLJIT_FR2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_AND32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0xffff);
	/* ibuf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), 8 * sizeof(sljit_s32), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_W(0x4455667788));
	/* dbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_SW, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f64), SLJIT_R0, 0);
	/* dbuf[9] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_S32, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f64), SLJIT_IMM, SLJIT_W(0x7766554433));
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();

	FAILED(dbuf_ptr[(1 * 2) + 0] != -1, "test49 case 1 failed\n");
	FAILED(dbuf_ptr[(1 * 2) + 1] != -1, "test49 case 2 failed\n");
	FAILED(dbuf[2] != -879.75, "test49 case 3 failed\n");
	FAILED(dbuf_ptr[(3 * 2) + 0] != -1, "test49 case 4 failed\n");
	FAILED(dbuf_ptr[(3 * 2) + 1] != -1, "test49 case 5 failed\n");
	FAILED(dbuf[4] != 345, "test49 case 6 failed\n");
	FAILED(dbuf_ptr[(5 * 2) + 0] != -1, "test49 case 7 failed\n");
	FAILED(dbuf_ptr[(5 * 2) + 1] != -1, "test49 case 8 failed\n");
	FAILED(dbuf[6] != -249, "test49 case 9 failed\n");
	FAILED(dbuf_ptr[(7 * 2) + 0] != -1, "test49 case 10 failed\n");
	FAILED(dbuf_ptr[(7 * 2) + 1] != -1, "test49 case 11 failed\n");

	FAILED(sbuf_ptr[1] != -1, "test49 case 12 failed\n");
	FAILED(sbuf[2] != 673.75, "test49 case 13 failed\n");
	FAILED(sbuf_ptr[3] != -1, "test49 case 14 failed\n");
	FAILED(sbuf[4] != 345, "test49 case 15 failed\n");
	FAILED(sbuf_ptr[5] != -1, "test49 case 16 failed\n");
	FAILED(sbuf[6] != -249, "test49 case 17 failed\n");
	FAILED(sbuf_ptr[7] != -1, "test49 case 18 failed\n");

	FAILED(wbuf[1] != -1, "test49 case 19 failed\n");
	FAILED(wbuf[2] != 673, "test49 case 20 failed\n");
	FAILED(wbuf[3] != -1, "test49 case 21 failed\n");
	FAILED(wbuf[4] != -879, "test49 case 22 failed\n");
	FAILED(wbuf[5] != -1, "test49 case 23 failed\n");

	FAILED(ibuf[1] != -1, "test49 case 24 failed\n");
	FAILED(ibuf[2] != 673, "test49 case 25 failed\n");
	FAILED(ibuf[3] != -1, "test49 case 26 failed\n");
	FAILED(ibuf[4] != -879, "test49 case 27 failed\n");
	FAILED(ibuf[5] != -1, "test49 case 28 failed\n");

#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(dbuf[8] != (sljit_f64)SLJIT_W(0x4455667788), "test49 case 29 failed\n");
	FAILED(dbuf[9] != (sljit_f64)SLJIT_W(0x66554433), "test49 case 30 failed\n");
	FAILED(wbuf[8] != SLJIT_W(0x1122334455), "test48 case 31 failed\n");
	FAILED(ibuf[8] == 0x4455, "test48 case 32 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test50(void)
{
	/* Test stack and floating point operations. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	sljit_uw size1, size2, size3;
	int result;
#endif
	sljit_f32 sbuf[7];

	if (verbose)
		printf("Run test50\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test50 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	FAILED(!compiler, "cannot create compiler\n");

	sbuf[0] = 245.5;
	sbuf[1] = -100.25;
	sbuf[2] = 713.75;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 3, 6, 0, 8 * sizeof(sljit_f32));

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), 0);
	/* sbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop2(compiler, SLJIT_ADD_F32, SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f32));
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_f32), SLJIT_IMM, 5934);
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_S32, SLJIT_MEM1(SLJIT_SP), 3 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_f32));
	/* sbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_SP), 3 * sizeof(sljit_f32));

#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	size1 = compiler->size;
#endif
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f32));
#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	size2 = compiler->size;
#endif
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_FR2, 0);
#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	size3 = compiler->size;
#endif
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f32), SLJIT_FR5, 0);
#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	result = (compiler->size - size3) == (size3 - size2) && (size3 - size2) == (size2 - size1);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&sbuf);

	FAILED(sbuf[3] != 245.5, "test50 case 1 failed\n");
	FAILED(sbuf[4] != 145.25, "test50 case 2 failed\n");
	FAILED(sbuf[5] != 5934, "test50 case 3 failed\n");
	FAILED(sbuf[6] != 713.75, "test50 case 4 failed\n");
#if !(defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	FAILED(!result, "test50 case 5 failed\n");
#endif

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test51(void)
{
	/* Test all registers provided by the CPU. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump;
	sljit_sw buf[2];
	sljit_s32 i;

	if (verbose)
		printf("Run test51\n");

	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = 39;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), SLJIT_NUMBER_OF_REGISTERS, 0, 0, 0, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 32);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);

	for (i = 2; i < SLJIT_NUMBER_OF_REGISTERS; i++) {
		if (sljit_get_register_index(SLJIT_R(i)) >= 0) {
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_R0, 0);
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_R(i)), 0);
		} else
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, buf[0]);
	}

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 32);
	for (i = 2; i < SLJIT_NUMBER_OF_REGISTERS; i++) {
		if (sljit_get_register_index(SLJIT_R(i)) >= 0) {
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_R0, 0);
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_R(i)), 32);
		} else
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, buf[0]);
	}

	for (i = 2; i < SLJIT_NUMBER_OF_REGISTERS; i++) {
		if (sljit_get_register_index(SLJIT_R(i)) >= 0) {
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 32);
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_MEM2(SLJIT_R(i), SLJIT_R0), 0);
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_MEM2(SLJIT_R0, SLJIT_R(i)), 0);
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 8);
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_MEM2(SLJIT_R0, SLJIT_R(i)), 2);
		} else
			sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, 3 * buf[0]);
	}

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 32 + sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();

	FAILED(buf[1] != (39 * 5 * (SLJIT_NUMBER_OF_REGISTERS - 2)), "test51 case 1 failed\n");

	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), SLJIT_NUMBER_OF_SCRATCH_REGISTERS, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 17);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_REGISTERS; i++)
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S(i), 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), SLJIT_NUMBER_OF_REGISTERS, 0, 0, 0, 0);
	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 35);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != (SLJIT_NUMBER_OF_SAVED_REGISTERS * 17), "test51 case 2 failed\n");

	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), SLJIT_NUMBER_OF_SCRATCH_REGISTERS, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S(i), 0, SLJIT_IMM, 68);
	for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 0);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_REGISTERS; i++)
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S(i), 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), SLJIT_NUMBER_OF_REGISTERS, 0, 0, 0, 0);
	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 43);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != (SLJIT_NUMBER_OF_SAVED_REGISTERS * 68), "test51 case 3 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test52(void)
{
	/* Test all registers provided by the CPU. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_f64 buf[3];
	sljit_s32 i;

	if (verbose)
		printf("Run test52\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test52 skipped\n");
		successful_tests++;
		return;
	}

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 6.25;
	buf[1] = 17.75;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_S0), 0);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(VOID));
	/* SLJIT_FR0 contains the first value. */
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR(i), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 1, 0, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[1]);
	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_R0), 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[2] != (SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS * 17.75 + SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS * 6.25), "test52 case 1 failed\n");

	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = -32.5;
	buf[1] = -11.25;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_S0), 0);
	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FS(i), 0, SLJIT_MEM1(SLJIT_S0), 0);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(VOID));
	/* SLJIT_FR0 contains the first value. */
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR(i), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 1, 0, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[1]);
	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_R0), 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[2] != (SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS * -11.25 + SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS * -32.5), "test52 case 2 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test53(void)
{
	/* Test addressing modes. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_u8 buf[3 + SLJIT_NUMBER_OF_REGISTERS * 3];
	sljit_u8 *buf_start;
	sljit_uw addr = (sljit_uw)&buf;
	sljit_s32 i;

	addr = addr + 3 - (addr % 3);
	buf_start = (sljit_u8*)addr;

	SLJIT_ASSERT((addr % 3) == 0);

	for (i = 0; i < (sljit_s32)sizeof(buf); i++)
		buf[i] = 0;

	if (verbose)
		printf("Run test78\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), SLJIT_NUMBER_OF_REGISTERS, 0, 0, 0, 0);

	addr /= 3;
	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++, addr++) {
		if (sljit_get_register_index(SLJIT_R(i)) == -1)
			continue;

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, (sljit_sw)addr);
		sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM2(SLJIT_R(i), SLJIT_R(i)), 1, SLJIT_IMM, 88 + i);

		if (i != 0) {
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)(addr * 2 + 1));
			sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM2(SLJIT_R(i), SLJIT_R0), 0, SLJIT_IMM, 147 + i);
		}

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, (sljit_sw)(addr * 3 + 2));
		sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_R(i)), 0, SLJIT_IMM, 191 + i);
	}

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R2, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++) {
		if (sljit_get_register_index(SLJIT_R(i)) == -1)
			continue;

		FAILED(buf_start[i * 3] != 88 + i, "test78 case 1 failed\n");
		if (i != 0) {
			FAILED(buf_start[i * 3 + 1] != 147 + i, "test78 case 2 failed\n");
		}
		FAILED(buf_start[i * 3 + 2] != 191 + i, "test78 case 3 failed\n");
	}

	sljit_free_code(code.code, NULL);

	successful_tests++;
}

static void test54(void)
{
	/* Check cmov. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw large_num = SLJIT_W(0x1234567812345678);
#else
	sljit_sw large_num = SLJIT_W(0x12345678);
#endif
	int i;
	sljit_sw buf[19];
	sljit_s32 ibuf[4];

	union {
		sljit_f32 value;
		sljit_s32 s32_value;
	} sbuf[3];

	sbuf[0].s32_value = 0x7fffffff;
	sbuf[1].value = 7.5;
	sbuf[2].value = -14.75;

	if (verbose)
		printf("Run test54\n");

	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 19; i++)
		buf[i] = 0;
	for (i = 0; i < 4; i++)
		ibuf[i] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 5, 3, 3, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 17);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 34);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_IMM, -10);
	sljit_emit_cmov(compiler, SLJIT_SIG_LESS, SLJIT_R0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_IMM, -10);
	sljit_emit_cmov(compiler, SLJIT_SIG_GREATER, SLJIT_R0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 24);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 24);
	sljit_emit_cmov(compiler, SLJIT_NOT_EQUAL, SLJIT_R0, SLJIT_IMM, 66);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_cmov(compiler, SLJIT_EQUAL, SLJIT_R0, SLJIT_IMM, 78);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_cmov(compiler, SLJIT_EQUAL, SLJIT_R0, SLJIT_IMM, large_num);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);

#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	SLJIT_ASSERT(sljit_get_register_index(SLJIT_R3) == -1 && sljit_get_register_index(SLJIT_R4) == -1);
#endif
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 7);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -45);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_cmov(compiler, SLJIT_OVERFLOW, SLJIT_R3, SLJIT_IMM, 35);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, large_num);
	sljit_emit_op2u(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_IMM, large_num);
	sljit_emit_cmov(compiler, SLJIT_OVERFLOW, SLJIT_R3, SLJIT_IMM, 35);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 71);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 13);
	sljit_emit_op2(compiler, SLJIT_LSHR | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_cmov(compiler, SLJIT_EQUAL, SLJIT_R3, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 12);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -29);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_cmov(compiler, SLJIT_NOT_OVERFLOW, SLJIT_R0, SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -12);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R4, 0, SLJIT_IMM, 21);
	sljit_emit_op2u(compiler, SLJIT_AND | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_cmov(compiler, SLJIT_NOT_EQUAL, SLJIT_R3, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R3, 0);
	sljit_emit_cmov(compiler, SLJIT_EQUAL, SLJIT_R3, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R3, 0);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 16);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
		sljit_emit_cmov(compiler, SLJIT_F_EQUAL, SLJIT_R0, SLJIT_IMM, -45);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_GREATER, SLJIT_FR1, 0, SLJIT_FR2, 0);
		sljit_emit_cmov(compiler, SLJIT_F_GREATER, SLJIT_R0, SLJIT_IMM, -45);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_GREATER_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
		sljit_emit_cmov(compiler, SLJIT_F_GREATER_EQUAL, SLJIT_R0, SLJIT_IMM, 33);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_R0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_FR2, 0);
		sljit_emit_cmov(compiler, SLJIT_F_LESS, SLJIT_R0, SLJIT_IMM, -70);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_LESS_EQUAL, SLJIT_FR2, 0, SLJIT_FR1, 0);
		sljit_emit_cmov(compiler, SLJIT_F_LESS_EQUAL, SLJIT_R0, SLJIT_IMM, -60);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 15 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_NOT_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
		sljit_emit_cmov(compiler, SLJIT_F_NOT_EQUAL, SLJIT_R0, SLJIT_IMM, 31);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 16 * sizeof(sljit_sw), SLJIT_R0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 53);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_ORDERED, SLJIT_FR1, 0, SLJIT_FR0, 0);
		sljit_emit_cmov(compiler, SLJIT_ORDERED, SLJIT_R0, SLJIT_IMM, 17);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 17 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_UNORDERED, SLJIT_FR1, 0, SLJIT_FR0, 0);
		sljit_emit_cmov(compiler, SLJIT_UNORDERED, SLJIT_R0, SLJIT_IMM, 59);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 18 * sizeof(sljit_sw), SLJIT_R0, 0);
	}

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 177);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_LESS, SLJIT_R0, 0, SLJIT_IMM, 178);
	sljit_emit_cmov(compiler, SLJIT_LESS, SLJIT_R0 | SLJIT_32, SLJIT_IMM, 200);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 95);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R3, 0, SLJIT_IMM, 177);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_LESS_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 95);
	sljit_emit_cmov(compiler, SLJIT_LESS_EQUAL, SLJIT_R3 | SLJIT_32, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s32), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R3, 0, SLJIT_IMM, 56);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R4, 0, SLJIT_IMM, -63);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_SIG_LESS, SLJIT_R3, 0, SLJIT_R4, 0);
	sljit_emit_cmov(compiler, SLJIT_SIG_LESS, SLJIT_R3 | SLJIT_32, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_s32), SLJIT_R3, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_SIG_GREATER, SLJIT_R3, 0, SLJIT_R4, 0);
	sljit_emit_cmov(compiler, SLJIT_SIG_GREATER, SLJIT_R3 | SLJIT_32, SLJIT_R4, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_s32), SLJIT_R3, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&buf, (sljit_sw)&ibuf, (sljit_sw)&sbuf);

	FAILED(buf[0] != 17, "test54 case 1 failed\n");
	FAILED(buf[1] != 34, "test54 case 2 failed\n");
	FAILED(buf[2] != 24, "test54 case 3 failed\n");
	FAILED(buf[3] != 78, "test54 case 4 failed\n");
	FAILED(buf[4] != large_num, "test54 case 5 failed\n");
	FAILED(buf[5] != -45, "test54 case 6 failed\n");
	FAILED(buf[6] != 35, "test54 case 7 failed\n");
	FAILED(buf[7] != 71, "test54 case 8 failed\n");
	FAILED(buf[8] != -29, "test54 case 9 failed\n");
	FAILED(buf[9] != -12, "test54 case 10 failed\n");
	FAILED(buf[10] != 21, "test54 case 11 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		FAILED(buf[11] != 16, "test54 case 12 failed\n");
		FAILED(buf[12] != -45, "test54 case 13 failed\n");
		FAILED(buf[13] != 33, "test54 case 14 failed\n");
		FAILED(buf[14] != 8, "test54 case 15 failed\n");
		FAILED(buf[15] != -60, "test54 case 16 failed\n");
		FAILED(buf[16] != 31, "test54 case 17 failed\n");
		FAILED(buf[17] != 53, "test54 case 18 failed\n");
		FAILED(buf[18] != 59, "test54 case 19 failed\n");
	}

	FAILED(ibuf[0] != 200, "test54 case 12 failed\n");
	FAILED(ibuf[1] != 95, "test54 case 13 failed\n");
	FAILED(ibuf[2] != 56, "test54 case 14 failed\n");
	FAILED(ibuf[3] != -63, "test54 case 15 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test55(void)
{
	/* Check value preservation. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[2];
	sljit_s32 i;

	if (verbose)
		printf("Run test55\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), SLJIT_NUMBER_OF_REGISTERS, 0, 0, 0, sizeof (sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, 217);

	/* Check 1 */
	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 118);

	sljit_emit_op0(compiler, SLJIT_DIVMOD_SW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);

	for (i = 2; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R(i), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_SP), 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 0), SLJIT_R0, 0);

	/* Check 2 */
	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 146);

	sljit_emit_op0(compiler, SLJIT_DIV_SW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);

	for (i = 1; i < SLJIT_NUMBER_OF_REGISTERS; i++)
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R(i), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_SP), 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 1), SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();

	FAILED(buf[0] != (SLJIT_NUMBER_OF_REGISTERS - 2) * 118 + 217, "test55 case 1 failed\n");
	FAILED(buf[1] != (SLJIT_NUMBER_OF_REGISTERS - 1) * 146 + 217, "test55 case 2 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test56(void)
{
	/* Check integer substraction with negative immediate. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[13];
	sljit_s32 i;

	if (verbose)
		printf("Run test56\n");

	for (i = 0; i < 13; i++)
		buf[i] = 77;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 90 << 12);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_SIG_GREATER);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_LESS, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_LESS);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER_EQUAL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_SIG_GREATER_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_LESS_EQUAL, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_LESS_EQUAL);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_GREATER);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_SIG_LESS);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 90);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_SIG_GREATER, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -91);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_SIG_GREATER);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 90);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_LESS_EQUAL, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -91);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_LESS_EQUAL);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, -0x7fffffff);
	sljit_emit_op2(compiler, SLJIT_ADD32 | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -(91 << 12));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, -0x7fffffff-1);
	sljit_emit_op2(compiler, SLJIT_SUB32 | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_IMM, 0, SLJIT_R0, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != (181 << 12), "test56 case 1 failed\n");
	FAILED(buf[1] != 1, "test56 case 2 failed\n");
	FAILED(buf[2] != (181 << 12), "test56 case 3 failed\n");
	FAILED(buf[3] != 1, "test56 case 4 failed\n");
	FAILED(buf[4] != 1, "test56 case 5 failed\n");
	FAILED(buf[5] != 1, "test56 case 6 failed\n");
	FAILED(buf[6] != 0, "test56 case 7 failed\n");
	FAILED(buf[7] != 0, "test56 case 8 failed\n");
	FAILED(buf[8] != 181, "test56 case 9 failed\n");
	FAILED(buf[9] != 1, "test56 case 10 failed\n");
	FAILED(buf[10] != 1, "test56 case 11 failed\n");
	FAILED(buf[11] != 1, "test56 case 12 failed\n");
	FAILED(buf[12] != 1, "test56 case 13 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test57(void)
{
	/* Check prefetch instructions. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label* labels[5];
	sljit_p addr[5];
	int i;

	if (verbose)
		printf("Run test57\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	labels[0] = sljit_emit_label(compiler);
	/* Should never crash. */
	sljit_emit_op_src(compiler, SLJIT_PREFETCH_L1, SLJIT_MEM2(SLJIT_R0, SLJIT_R0), 2);
	labels[1] = sljit_emit_label(compiler);
	sljit_emit_op_src(compiler, SLJIT_PREFETCH_L2, SLJIT_MEM0(), 0);
	labels[2] = sljit_emit_label(compiler);
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_emit_op_src(compiler, SLJIT_PREFETCH_L3, SLJIT_MEM1(SLJIT_R0), SLJIT_W(0x1122334455667788));
#else
	sljit_emit_op_src(compiler, SLJIT_PREFETCH_L3, SLJIT_MEM1(SLJIT_R0), 0x11223344);
#endif
	labels[3] = sljit_emit_label(compiler);
	sljit_emit_op_src(compiler, SLJIT_PREFETCH_ONCE, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw));
	labels[4] = sljit_emit_label(compiler);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	for (i = 0; i < 5; i++)
		addr[i] = sljit_get_label_addr(labels[i]);

	sljit_free_compiler(compiler);

	code.func0();

	if (sljit_has_cpu_feature(SLJIT_HAS_PREFETCH)) {
		FAILED(addr[0] == addr[1], "test57 case 1 failed\n");
		FAILED(addr[1] == addr[2], "test57 case 2 failed\n");
		FAILED(addr[2] == addr[3], "test57 case 3 failed\n");
		FAILED(addr[3] == addr[4], "test57 case 4 failed\n");
	}
	else {
		FAILED(addr[0] != addr[1], "test57 case 1 failed\n");
		FAILED(addr[1] != addr[2], "test57 case 2 failed\n");
		FAILED(addr[2] != addr[3], "test57 case 3 failed\n");
		FAILED(addr[3] != addr[4], "test57 case 4 failed\n");
	}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static sljit_f64 test58_f1(sljit_f32 a, sljit_f32 b, sljit_f64 c)
{
	return (sljit_f64)a + (sljit_f64)b + c;
}

static sljit_f32 test58_f2(sljit_sw a, sljit_f64 b, sljit_f32 c)
{
	return (sljit_f32)((sljit_f64)a + b + (sljit_f64)c);
}

static sljit_f64 test58_f3(sljit_sw a, sljit_f32 b, sljit_sw c)
{
	return (sljit_f64)a + (sljit_f64)b + (sljit_f64)c;
}

static sljit_f64 test58_f4(sljit_f32 a, sljit_sw b)
{
	return (sljit_f64)a + (sljit_f64)b;
}

static sljit_f32 test58_f5(sljit_f32 a, sljit_f64 b, sljit_s32 c)
{
	return (sljit_f32)((sljit_f64)a + b + (sljit_f64)c);
}

static sljit_sw test58_f6(sljit_f64 a, sljit_sw b)
{
	return (sljit_sw)(a + (sljit_f64)b);
}

static void test58(void)
{
	/* Check function calls with floating point arguments. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_f64 dbuf[7];
	sljit_f32 sbuf[7];
	sljit_sw wbuf[2];

	if (verbose)
		printf("Run test58\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test58 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	dbuf[0] = 5.25;
	dbuf[1] = 0.0;
	dbuf[2] = 2.5;
	dbuf[3] = 0.0;
	dbuf[4] = 0.0;
	dbuf[5] = 0.0;
	dbuf[6] = -18.0;

	sbuf[0] = 6.75;
	sbuf[1] = -3.5;
	sbuf[2] = 1.5;
	sbuf[3] = 0.0;
	sbuf[4] = 0.0;

	wbuf[0] = 0;
	wbuf[1] = 0;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 3, 3, 4, 0, sizeof(sljit_sw));

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(F64, F32, F32, F64), SLJIT_IMM, SLJIT_FUNC_ADDR(test58_f1));
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3(F64, F32, F32, F64));
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test58_f1));
	/* dbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test58_f2));
	sljit_get_local_base(compiler, SLJIT_R1, 0, -16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 16);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(F32, W, F64, F32), SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
	/* sbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f32), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -4);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 9);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3(F64, W, F32, W));
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test58_f3));
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -6);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(F64, F32, W));
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test58_f4));
	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test58_f5));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(F32, F32, F64, 32), SLJIT_MEM1(SLJIT_SP), 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test58_f6));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(W, F64, W), SLJIT_R0, 0);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 0, SLJIT_R0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 319);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test58_f6));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(W, F64, W), SLJIT_R1, 0);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&dbuf, (sljit_sw)&sbuf, (sljit_sw)&wbuf);

	FAILED(dbuf[1] != 8.5, "test58 case 1 failed\n");
	FAILED(dbuf[3] != 0.5, "test58 case 2 failed\n");
	FAILED(sbuf[3] != 17.75, "test58 case 3 failed\n");
	FAILED(dbuf[4] != 11.75, "test58 case 4 failed\n");
	FAILED(dbuf[5] != -9.5, "test58 case 5 failed\n");
	FAILED(sbuf[4] != 12, "test58 case 6 failed\n");
	FAILED(wbuf[0] != SLJIT_FUNC_ADDR(test58_f6) - 18, "test58 case 7 failed\n");
	FAILED(wbuf[1] != 301, "test58 case 8 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static sljit_sw test59_f1(sljit_sw a, sljit_s32 b, sljit_sw c, sljit_sw d)
{
	return (sljit_sw)(a + b + c + d - SLJIT_FUNC_ADDR(test59_f1));
}

static sljit_s32 test59_f2(sljit_f64 a, sljit_f32 b, sljit_f64 c, sljit_sw d)
{
	return (sljit_s32)(a + b + c + (sljit_f64)d);
}

static sljit_f32 test59_f3(sljit_f32 a, sljit_s32 b, sljit_f64 c, sljit_sw d)
{
	return (sljit_f32)(a + (sljit_f64)b + c + (sljit_f64)d);
}

static sljit_f32 test59_f4(sljit_f32 a, sljit_f64 b, sljit_f32 c, sljit_f64 d)
{
	return (sljit_f32)(a + b + c + (sljit_f64)d);
}

static void test59(void)
{
	/* Check function calls with four arguments. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_sw wbuf[5];
	sljit_f64 dbuf[3];
	sljit_f32 sbuf[4];

	if (verbose)
		printf("Run test59\n");

	wbuf[0] = 0;
	wbuf[1] = 0;
	wbuf[2] = SLJIT_FUNC_ADDR(test59_f1);
	wbuf[3] = 0;
	wbuf[4] = 0;

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		dbuf[0] = 5.125;
		dbuf[1] = 6.125;
		dbuf[2] = 4.25;

		sbuf[0] = 0.75;
		sbuf[1] = -1.5;
		sbuf[2] = 0.0;
		sbuf[3] = 0.0;
	}

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 4, 3, 4, 0, sizeof(sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 33);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -20);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test59_f1));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -40);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_R2, 0);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test59_f1));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -25);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 100);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -10);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_R0, 0);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 231);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test59_f1) - 100);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_MEM2(SLJIT_R0, SLJIT_R2), SLJIT_WORD_SHIFT);
	/* wbuf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -100);
		sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(32, F64, F32, F64, W), SLJIT_IMM, SLJIT_FUNC_ADDR(test59_f2));
		sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
		/* wbuf[4] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 36);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 41);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS4(F32, F32, 32, F64, W));
		sljit_set_target(jump, SLJIT_FUNC_UADDR(test59_f3));
		/* sbuf[2] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32), SLJIT_FR0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test59_f4));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));
		sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(F32, F32, F64, F32, F64), SLJIT_R0, 0);
		/* sbuf[2] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_f32), SLJIT_FR0, 0);
	}

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&wbuf, (sljit_sw)&dbuf, (sljit_sw)&sbuf);

	FAILED(wbuf[0] != -27, "test59 case 1 failed\n");
	FAILED(wbuf[1] != 65, "test59 case 2 failed\n");
	FAILED(wbuf[3] != (sljit_sw)wbuf + 133, "test59 case 3 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		FAILED(wbuf[4] != -88, "test59 case 4 failed\n");
		FAILED(sbuf[2] != 79.75, "test59 case 5 failed\n");
		FAILED(sbuf[3] != 8.625, "test59 case 6 failed\n");
	}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test60(void)
{
	/* Test memory accesses with pre/post updates. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_u32 i;
	sljit_s32 supported[10];
	sljit_sw wbuf[18];
	sljit_s8 bbuf[4];
	sljit_s32 ibuf[4];

#if (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64) || (defined SLJIT_CONFIG_ARM_THUMB2 && SLJIT_CONFIG_ARM_THUMB2)
	static sljit_u8 expected[10] = { 1, 1, 1, 1, 1, 1, 0, 0, 0, 0 };
#elif (defined SLJIT_CONFIG_ARM_32 && SLJIT_CONFIG_ARM_32)
	static sljit_u8 expected[10] = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
#elif (defined SLJIT_CONFIG_PPC_32 && SLJIT_CONFIG_PPC_32)
	static sljit_u8 expected[10] = { 1, 0, 1, 1, 0, 1, 1, 1, 0, 0 };
#elif (defined SLJIT_CONFIG_PPC_64 && SLJIT_CONFIG_PPC_64)
	static sljit_u8 expected[10] = { 1, 0, 0, 1, 0, 0, 1, 1, 0, 0 };
#else
	static sljit_u8 expected[10] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
#endif

	if (verbose)
		printf("Run test60\n");

	for (i = 0; i < 18; i++)
		wbuf[i] = 0;
	wbuf[2] = -887766;

	bbuf[0] = 0;
	bbuf[1] = 0;
	bbuf[2] = -13;

	ibuf[0] = -5678;
	ibuf[1] = 0;
	ibuf[2] = 0;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 4, 3, 4, 0, sizeof(sljit_sw));

	supported[0] = sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_sw));
	if (supported[0] == SLJIT_SUCCESS) {
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
		sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_sw));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	}

	supported[1] = sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM1(SLJIT_R2), -2 * (sljit_sw)sizeof(sljit_s8));
	if (supported[1] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S1, 0, SLJIT_IMM, 2 * sizeof(sljit_s8));
		sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM1(SLJIT_R2), -2 * (sljit_sw)sizeof(sljit_s8));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	supported[2] = sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_R2, SLJIT_MEM1(SLJIT_R1), -2 * (sljit_sw)sizeof(sljit_s32));
	if (supported[2] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S2, 0, SLJIT_IMM, 2 * sizeof(sljit_s32));
		sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_PRE, SLJIT_R2, SLJIT_MEM1(SLJIT_R1), -2 * (sljit_sw)sizeof(sljit_s32));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R2, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[3] = sljit_emit_mem(compiler, SLJIT_MOV32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R2), 2 * sizeof(sljit_s32));
	if (supported[3] == SLJIT_SUCCESS) {
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -8765);
		sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, sizeof(sljit_s32));
		sljit_emit_mem(compiler, SLJIT_MOV32 | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R2), 2 * sizeof(sljit_s32));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	supported[4] = sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_R1, SLJIT_MEM1(SLJIT_R2), -128 * (sljit_sw)sizeof(sljit_s8));
	if (supported[4] == SLJIT_SUCCESS) {
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -121);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S1, 0);
		sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_R1, SLJIT_MEM1(SLJIT_R2), -128 * (sljit_sw)sizeof(sljit_s8));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	supported[5] = sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), 1);
	if (supported[5] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 9 * sizeof(sljit_sw) - 1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -881199);
		sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), 1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R0, 0);
	}

	supported[6] = sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
	if (supported[6] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S2, 0, SLJIT_IMM, 213);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -213);
		sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_PRE, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[7] = sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
	if (supported[7] == SLJIT_SUCCESS) {
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S2, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2 * sizeof(sljit_s32));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -7890);
		sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[8] = sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 2);
	if (supported[8] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 2 * sizeof(sljit_sw));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2 * sizeof(sljit_sw));
		sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 2);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 15 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[9] = sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
	if (supported[9] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 2 * sizeof(sljit_s8));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -2 * (sljit_sw)sizeof(sljit_s8));
		sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 16 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 17 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	SLJIT_ASSERT(sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 1) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_R0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 1) == SLJIT_ERR_UNSUPPORTED);

#if (defined SLJIT_CONFIG_ARM_THUMB2 && SLJIT_CONFIG_ARM_THUMB2) || (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	/* TODO: at least for ARM (both V5 and V7) the range below needs further fixing */
	SLJIT_ASSERT(sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), 256) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_R1, SLJIT_MEM1(SLJIT_R0), -257) == SLJIT_ERR_UNSUPPORTED);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&wbuf, (sljit_sw)&bbuf, (sljit_sw)&ibuf);

	FAILED(sizeof(expected) != sizeof(supported) / sizeof(sljit_s32), "test60 case 1 failed\n");

	for (i = 0; i < sizeof(expected); i++) {
		if (expected[i]) {
			if (supported[i] != SLJIT_SUCCESS) {
				printf("tast60 case %d should be supported\n", i + 1);
				return;
			}
		} else {
			if (supported[i] == SLJIT_SUCCESS) {
				printf("test60 case %d should not be supported\n", i + 1);
				return;
			}
		}
	}

	FAILED(supported[0] == SLJIT_SUCCESS && wbuf[0] != -887766, "test60 case 2 failed\n");
	FAILED(supported[0] == SLJIT_SUCCESS && wbuf[1] != (sljit_sw)(wbuf + 2), "test60 case 3 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && wbuf[3] != -13, "test60 case 4 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && wbuf[4] != (sljit_sw)(bbuf), "test60 case 5 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && wbuf[5] != -5678, "test60 case 6 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && wbuf[6] != (sljit_sw)(ibuf), "test60 case 7 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && ibuf[1] != -8765, "test60 case 8 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && wbuf[7] != (sljit_sw)(ibuf + 1), "test60 case 9 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && bbuf[0] != -121, "test60 case 10 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && wbuf[8] != (sljit_sw)(bbuf) - 128 * (sljit_sw)sizeof(sljit_s8), "test60 case 11 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && wbuf[9] != -881199, "test60 case 12 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && wbuf[10] != (sljit_sw)(wbuf + 9), "test60 case 13 failed\n");
	FAILED(supported[6] == SLJIT_SUCCESS && wbuf[11] != -5678, "test60 case 14 failed\n");
	FAILED(supported[6] == SLJIT_SUCCESS && wbuf[12] != (sljit_sw)(ibuf), "test60 case 15 failed\n");
	FAILED(supported[7] == SLJIT_SUCCESS && ibuf[2] != -7890, "test60 case 16 failed\n");
	FAILED(supported[7] == SLJIT_SUCCESS && wbuf[13] != (sljit_sw)(ibuf + 2), "test60 case 17 failed\n");
	FAILED(supported[8] == SLJIT_SUCCESS && wbuf[14] != -887766, "test60 case 18 failed\n");
	FAILED(supported[8] == SLJIT_SUCCESS && wbuf[15] != (sljit_sw)(wbuf + 10), "test60 case 19 failed\n");
	FAILED(supported[9] == SLJIT_SUCCESS && wbuf[16] != -13, "test60 case 20 failed\n");
	FAILED(supported[9] == SLJIT_SUCCESS && wbuf[17] != (sljit_sw)(bbuf), "test60 case 21 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test61(void)
{
	/* Test float memory accesses with pre/post updates. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_u32 i;
	sljit_s32 supported[6];
	sljit_sw wbuf[6];
	sljit_f64 dbuf[4];
	sljit_f32 sbuf[4];
#if (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	static sljit_u8 expected[6] = { 1, 1, 1, 1, 0, 0 };
#elif (defined SLJIT_CONFIG_PPC && SLJIT_CONFIG_PPC)
	static sljit_u8 expected[6] = { 1, 0, 1, 0, 1, 1 };
#else
	static sljit_u8 expected[6] = { 0, 0, 0, 0, 0, 0 };
#endif

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test61 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	if (verbose)
		printf("Run test61\n");

	for (i = 0; i < 6; i++)
		wbuf[i] = 0;

	dbuf[0] = 66.725;
	dbuf[1] = 0.0;
	dbuf[2] = 0.0;
	dbuf[3] = 0.0;

	sbuf[0] = 0.0;
	sbuf[1] = -22.125;
	sbuf[2] = 0.0;
	sbuf[3] = 0.0;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 4, 3, 4, 0, sizeof(sljit_sw));

	supported[0] = sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_f64));
	if (supported[0] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 4 * sizeof(sljit_f64));
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_PRE, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	}

	supported[1] = sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), -(sljit_sw)sizeof(sljit_f64));
	if (supported[1] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 0);
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), -(sljit_sw)sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	}

	supported[2] = sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_FR1, SLJIT_MEM1(SLJIT_R2), -4 * (sljit_sw)sizeof(sljit_f32));
	if (supported[2] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, 4 * sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_FR1, SLJIT_MEM1(SLJIT_R2), -4 * (sljit_sw)sizeof(sljit_f32));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	supported[3] = sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR1, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f32));
	if (supported[3] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S2, 0, SLJIT_IMM, sizeof(sljit_f32));
		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_POST, SLJIT_FR1, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32), SLJIT_FR1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[4] = sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
	if (supported[4] == SLJIT_SUCCESS) {
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 8 * sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -8 * (sljit_sw)sizeof(sljit_f64));
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_PRE, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[5] = sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_FR2, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 0);
	if (supported[5] == SLJIT_SUCCESS) {
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S2, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 3 * sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE | SLJIT_MEM_PRE, SLJIT_FR2, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	SLJIT_ASSERT(sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0) == SLJIT_ERR_UNSUPPORTED);

#if (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	/* TODO: at least for ARM (both V5 and V7) the range below needs further fixing */
	SLJIT_ASSERT(sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_PRE, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 256) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), -257) == SLJIT_ERR_UNSUPPORTED);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&wbuf, (sljit_sw)&dbuf, (sljit_sw)&sbuf);

	FAILED(sizeof(expected) != sizeof(supported) / sizeof(sljit_s32), "test61 case 1 failed\n");

	for (i = 0; i < sizeof(expected); i++) {
		if (expected[i]) {
			if (supported[i] != SLJIT_SUCCESS) {
				printf("tast61 case %d should be supported\n", i + 1);
				return;
			}
		} else {
			if (supported[i] == SLJIT_SUCCESS) {
				printf("test61 case %d should not be supported\n", i + 1);
				return;
			}
		}
	}

	FAILED(supported[0] == SLJIT_SUCCESS && dbuf[1] != 66.725, "test61 case 2 failed\n");
	FAILED(supported[0] == SLJIT_SUCCESS && wbuf[0] != (sljit_sw)(dbuf), "test61 case 3 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && dbuf[2] != 66.725, "test61 case 4 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && wbuf[1] != (sljit_sw)(dbuf + 1), "test61 case 5 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && sbuf[0] != -22.125, "test61 case 6 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && wbuf[2] != (sljit_sw)(sbuf), "test61 case 7 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && sbuf[2] != -22.125, "test61 case 8 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && wbuf[3] != (sljit_sw)(sbuf + 2), "test61 case 9 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && dbuf[3] != 66.725, "test61 case 10 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && wbuf[4] != (sljit_sw)(dbuf), "test61 case 11 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && sbuf[3] != -22.125, "test61 case 12 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && wbuf[5] != (sljit_sw)(sbuf + 3), "test61 case 13 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test62(void)
{
	/* Test fast calls flag preservation. */
	executable_code code1;
	executable_code code2;
	struct sljit_compiler* compiler;

	if (verbose)
		printf("Run test62\n");

	/* A */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	sljit_set_context(compiler, 0, SLJIT_ARGS1(W, W), 1, 1, 0, 0, 0);

	sljit_emit_fast_enter(compiler, SLJIT_R0, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z | SLJIT_SET_LESS, SLJIT_S0, 0, SLJIT_IMM, 42);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_R0, 0);

	code1.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	/* B */
	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, W), 1, 1, 0, 0, 0);
	sljit_emit_ijump(compiler, SLJIT_FAST_CALL, SLJIT_IMM, SLJIT_FUNC_ADDR(code1.code));
	sljit_set_current_flags(compiler, SLJIT_CURRENT_FLAGS_SUB | SLJIT_CURRENT_FLAGS_COMPARE | SLJIT_SET_Z | SLJIT_SET_LESS);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_ZERO);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_LESS);
	sljit_emit_op2(compiler, SLJIT_SHL, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code2.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code2.func1(88) != 0, "test62 case 1 failed\n");
	FAILED(code2.func1(42) != 1, "test62 case 2 failed\n");
	FAILED(code2.func1(0) != 2, "test62 case 3 failed\n");

	sljit_free_code(code1.code, NULL);
	sljit_free_code(code2.code, NULL);
	successful_tests++;
}

static void test63(void)
{
	/* Test put label. */
	executable_code code;
	struct sljit_label *label[2];
	struct sljit_put_label *put_label[5];
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_uw addr[2];
	sljit_uw buf[4];
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	sljit_sw offs = SLJIT_W(0x123456789012);
#else
	sljit_sw offs = 0x12345678;
#endif

	if (verbose)
		printf("Run test63\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 2 * sizeof(sljit_sw));

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);

	put_label[1] = sljit_emit_put_label(compiler, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_uw), SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw));

	label[0] = sljit_emit_label(compiler);
	sljit_set_put_label(put_label[0], label[0]);
	sljit_set_put_label(put_label[1], label[0]);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)(buf + 2) - offs);
	put_label[2] = sljit_emit_put_label(compiler, SLJIT_MEM1(SLJIT_R0), offs);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (offs + (sljit_sw)sizeof(sljit_uw)) >> 1);
	put_label[3] = sljit_emit_put_label(compiler, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), 1);

	label[1] = sljit_emit_label(compiler);
	sljit_set_put_label(put_label[2], label[1]);
	sljit_set_put_label(put_label[3], label[1]);

	put_label[4] = sljit_emit_put_label(compiler, SLJIT_RETURN_REG, 0);
	sljit_set_put_label(put_label[4], label[0]);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	addr[0] = sljit_get_label_addr(label[0]);
	addr[1] = sljit_get_label_addr(label[1]);

	sljit_free_compiler(compiler);

	FAILED(code.func1((sljit_sw)&buf) != (sljit_sw)addr[0], "test63 case 1 failed\n");
	FAILED(buf[0] != addr[0], "test63 case 2 failed\n");
	FAILED(buf[1] != addr[0], "test63 case 3 failed\n");
	FAILED(buf[2] != addr[1], "test63 case 4 failed\n");
	FAILED(buf[3] != addr[1], "test63 case 5 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test64(void)
{
	/* Test put label with absolute label addresses */
	executable_code code;
	sljit_uw malloc_addr;
	struct sljit_label label[6];
	struct sljit_put_label *put_label[2];
	struct sljit_compiler* compiler;
	sljit_uw buf[7];
	sljit_s32 i;
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	/* Must be even because it is also used for addressing. */
	sljit_sw offs1 = SLJIT_W(0x1234567812);
	sljit_sw offs2 = SLJIT_W(0x123456781122);
	sljit_sw offs3 = SLJIT_W(0x7fffffff7ff);
	sljit_sw offs4 = SLJIT_W(0x1234567811223344);
#else /* !SLJIT_64BIT_ARCHITECTURE */
	sljit_sw offs1 = (sljit_sw)0x80000000;
	sljit_sw offs2 = (sljit_sw)0xe0000000;
	sljit_sw offs3 = (sljit_sw)0x87654321;
	sljit_sw offs4 = (sljit_sw)0xffffffff;
#endif /* SLJIT_64BIT_ARCHITECTURE */

	if (verbose)
		printf("Run test64\n");

	/* lock next allocation; see sljit_test_malloc_exec() */
#if !(defined SLJIT_CONFIG_UNSUPPORTED && SLJIT_CONFIG_UNSUPPORTED)
	malloc_addr = (sljit_uw)SLJIT_MALLOC_EXEC(1024, NULL);

	if (!malloc_addr) {
		printf("Cannot allocate executable memory\n");
		return;
	}

	compiler = sljit_create_compiler(NULL, (void*)malloc_addr);
	malloc_addr += (sljit_uw)SLJIT_EXEC_OFFSET((void*)malloc_addr);
#else /* SLJIT_CONFIG_UNSUPPORTED */
	malloc_addr = 0;
	compiler = sljit_create_compiler(NULL, (void*)malloc_addr);
#endif /* !SLJIT_CONFIG_UNSUPPORTED */

	label[0].addr = 0x1234;
	label[0].size = (sljit_uw)0x1234 - malloc_addr;

	label[1].addr = 0x12345678;
	label[1].size = (sljit_uw)0x12345678 - malloc_addr;

	label[2].addr = (sljit_uw)offs1;
	label[2].size = (sljit_uw)offs1 - malloc_addr;

	label[3].addr = (sljit_uw)offs2;
	label[3].size = (sljit_uw)offs2 - malloc_addr;

	label[4].addr = (sljit_uw)offs3;
	label[4].size = (sljit_uw)offs3 - malloc_addr;

	label[5].addr = (sljit_uw)offs4;
	label[5].size = (sljit_uw)offs4 - malloc_addr;

	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 6; i++)
		buf[i] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 3, 1, 0, 0, 2 * sizeof(sljit_sw));

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);

	put_label[1] = sljit_emit_put_label(compiler, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_uw), SLJIT_MEM1(SLJIT_SP), sizeof(sljit_uw));

	sljit_set_put_label(put_label[0], &label[0]);
	sljit_set_put_label(put_label[1], &label[0]);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)(buf + 2) - offs1);
	put_label[0] = sljit_emit_put_label(compiler, SLJIT_MEM1(SLJIT_R0), offs1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (offs1 + (sljit_sw)sizeof(sljit_uw)) >> 1);
	put_label[1] = sljit_emit_put_label(compiler, SLJIT_MEM2(SLJIT_R0, SLJIT_R1), 1);

	sljit_set_put_label(put_label[0], &label[1]);
	sljit_set_put_label(put_label[1], &label[1]);

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_R1, 0);
	sljit_set_put_label(put_label[0], &label[2]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_uw), SLJIT_R1, 0);

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_R2, 0);
	sljit_set_put_label(put_label[0], &label[3]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_uw), SLJIT_R2, 0);

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_R1, 0);
	sljit_set_put_label(put_label[0], &label[4]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_uw), SLJIT_R1, 0);

	put_label[0] = sljit_emit_put_label(compiler, SLJIT_RETURN_REG, 0);
	sljit_set_put_label(put_label[0], &label[5]);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);
	SLJIT_ASSERT(SLJIT_FUNC_UADDR(code.code) >= malloc_addr && SLJIT_FUNC_UADDR(code.code) <= malloc_addr + 8);

	FAILED(code.func1((sljit_sw)&buf) != (sljit_sw)label[5].addr, "test64 case 1 failed\n");
	FAILED(buf[0] != label[0].addr, "test64 case 2 failed\n");
	FAILED(buf[1] != label[0].addr, "test64 case 3 failed\n");
	FAILED(buf[2] != label[1].addr, "test64 case 4 failed\n");
	FAILED(buf[3] != label[1].addr, "test64 case 5 failed\n");
	FAILED(buf[4] != label[2].addr, "test64 case 6 failed\n");
	FAILED(buf[5] != label[3].addr, "test64 case 7 failed\n");
	FAILED(buf[6] != label[4].addr, "test64 case 8 failed\n");

	sljit_free_code(code.code, NULL);

	successful_tests++;
}

static void test65(void)
{
	/* Test jump tables. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;
	/* Normally this table is allocated on the heap. */
	sljit_uw addr[64];
	struct sljit_label *labels[64];
	struct sljit_jump *jump;

	if (verbose)
		printf("Run test65\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, W, W), 1, 2, 0, 0, 0);

	jump = sljit_emit_cmp(compiler, SLJIT_GREATER_EQUAL, SLJIT_S0, 0, SLJIT_IMM, 64);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)addr);
	sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_MEM2(SLJIT_R0, SLJIT_S0), SLJIT_WORD_SHIFT);

	for (i = 0; i < 64; i++) {
		labels[i] = sljit_emit_label(compiler);
		sljit_emit_op0(compiler, SLJIT_ENDBR);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, i * 2);
		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);
	}

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, -1);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	for (i = 0; i < 64; i++) {
		addr[i] = sljit_get_label_addr(labels[i]);
	}

	sljit_free_compiler(compiler);

	FAILED(code.func2(64, 0) != -1, "test65 case 1 failed\n");

	for (i = 0; i < 64; i++) {
		FAILED(code.func2(i, i * 2) != i * 4, "test65 case 2 failed\n");
	}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test66(void)
{
	/* Test direct jumps (computed goto). */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_s32 i;
	sljit_uw addr[64];
	struct sljit_label *labels[64];

	if (verbose)
		printf("Run test66\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, W, W), 1, 2, 0, 0, 0);
	sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_S0, 0);

	for (i = 0; i < 64; i++) {
		labels[i] = sljit_emit_label(compiler);
		sljit_emit_op0(compiler, SLJIT_ENDBR);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, i * 2);
		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);
	}

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	for (i = 0; i < 64; i++) {
		addr[i] = sljit_get_label_addr(labels[i]);
	}

	sljit_free_compiler(compiler);

	for (i = 0; i < 64; i++) {
		FAILED(code.func2((sljit_sw)addr[i], i) != i * 3, "test66 case 1 failed\n");
	}

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test67(void)
{
	/* Test skipping returns from fast calls (return type is fast). */
	executable_code code;
	struct sljit_compiler *compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump *call, *jump;
	struct sljit_label *label;

	if (verbose)
		printf("Run test67\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	call = sljit_emit_jump(compiler, SLJIT_FAST_CALL);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	/* First function, never returns. */
	label = sljit_emit_label(compiler);
	sljit_set_label(call, label);
	sljit_emit_fast_enter(compiler, SLJIT_R1, 0);

	call = sljit_emit_jump(compiler, SLJIT_FAST_CALL);

	/* Should never return here, marked by a segmentation fault if it does. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	/* Second function, skips the first function. */
	sljit_set_label(call, sljit_emit_label(compiler));
	sljit_emit_fast_enter(compiler, SLJIT_R2, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);

	jump = sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_R1, 0);
	sljit_set_label(sljit_emit_jump(compiler, SLJIT_FAST_CALL), label);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_src(compiler, SLJIT_SKIP_FRAMES_BEFORE_FAST_RETURN, SLJIT_S0, 0);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_S0, 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_op_src(compiler, SLJIT_SKIP_FRAMES_BEFORE_FAST_RETURN, SLJIT_R1, 0);
	sljit_emit_op_src(compiler, SLJIT_FAST_RETURN, SLJIT_R1, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	sljit_free_compiler(compiler);

	FAILED(code.func0() != 3, "test67 case 1 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test68(void)
{
	/* Test skipping returns from fast calls (return type is normal). */
	executable_code code;
	struct sljit_compiler *compiler;
	struct sljit_jump *call, *jump;
	struct sljit_label *label;
	int i;

	if (verbose)
		printf("Run test68\n");

	for (i = 0; i < 6; i++) {
		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 2 + (i % 6), (i % 6), 0, 0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
		call = sljit_emit_jump(compiler, SLJIT_FAST_CALL);

		/* Should never return here, marked by a segmentation fault if it does. */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		/* Recursive fast call. */
		label = sljit_emit_label(compiler);
		sljit_set_label(call, label);
		sljit_emit_fast_enter(compiler, SLJIT_R1, 0);

		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);

		jump = sljit_emit_cmp(compiler, SLJIT_GREATER_EQUAL, SLJIT_R0, 0, SLJIT_IMM, 4);

		sljit_set_label(sljit_emit_jump(compiler, SLJIT_FAST_CALL), label);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);

		sljit_free_compiler(compiler);

		if (SLJIT_UNLIKELY(code.func0() != 4)) {
			printf("test68 case %d failed\n", i + 1);
			return;
		}
		sljit_free_code(code.code, NULL);
	}

	successful_tests++;
}

static void test69(void)
{
	/* Test sljit_set_current_flags. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[8];
	sljit_s32 i;

	if (verbose)
		printf("Run test69\n");

	for (i = 0; i < 8; i++)
		buf[i] = 4;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 3, 1, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)1 << ((sizeof (sljit_sw) * 8) - 2));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_OVERFLOW | SLJIT_CURRENT_FLAGS_ADD);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 5);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_OVERFLOW | SLJIT_CURRENT_FLAGS_ADD);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_R1, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_OVERFLOW);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 5);
	sljit_emit_op2(compiler, SLJIT_MUL | SLJIT_SET_OVERFLOW, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_OVERFLOW);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_OVERFLOW);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 5);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_GREATER, SLJIT_R1, 0, SLJIT_R2, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_GREATER | SLJIT_CURRENT_FLAGS_SUB | SLJIT_CURRENT_FLAGS_COMPARE);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_GREATER);

	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_R2, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_Z | SLJIT_CURRENT_FLAGS_SUB | SLJIT_CURRENT_FLAGS_COMPARE);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_ZERO);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)(1u << 31));
	sljit_emit_op2u(compiler, SLJIT_ADD32 | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_R1, 0);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_Z | SLJIT_CURRENT_FLAGS_32 | SLJIT_CURRENT_FLAGS_ADD);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_ZERO);

	sljit_emit_op2u(compiler, SLJIT_SHL32 | SLJIT_SET_Z, SLJIT_R1, 0, SLJIT_IMM, 1);
	sljit_emit_label(compiler);
	sljit_set_current_flags(compiler, SLJIT_SET_Z | SLJIT_CURRENT_FLAGS_32);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_NOT_ZERO);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 1, "test69 case 1 failed\n");
	FAILED(buf[1] != 2, "test69 case 2 failed\n");
	FAILED(buf[2] != 1, "test69 case 3 failed\n");
	FAILED(buf[3] != 2, "test69 case 4 failed\n");
	FAILED(buf[4] != 1, "test69 case 5 failed\n");
	FAILED(buf[5] != 2, "test69 case 6 failed\n");
	FAILED(buf[6] != 1, "test69 case 7 failed\n");
	FAILED(buf[7] != 2, "test69 case 8 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test70(void)
{
	/* Test argument passing to sljit_emit_enter. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw wbuf[2];
	sljit_s32 ibuf[2];
	sljit_f64 dbuf[3];
	sljit_f32 fbuf[2];

	if (verbose)
		printf("Run test70\n");

	wbuf[0] = 0;
	wbuf[1] = 0;
	ibuf[0] = 0;
	ibuf[1] = 0;

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32, W, 32, W), 1, 4, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_s32), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test70_f1(-1478, 9476, 4928, -6832);

	FAILED(wbuf[0] != 9476, "test70 case 1 failed\n");
	FAILED(wbuf[1] != -6832, "test70 case 2 failed\n");
	FAILED(ibuf[0] != -1478, "test70 case 3 failed\n");
	FAILED(ibuf[1] != 4928, "test70 case 4 failed\n");

	sljit_free_code(code.code, NULL);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32, 32, W, W), 1, 4, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_s32), SLJIT_S3, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test70_f2(4721, 7892, -3579, -4830);

	FAILED(wbuf[0] != 4721, "test70 case 5 failed\n");
	FAILED(wbuf[1] != 7892, "test70 case 6 failed\n");
	FAILED(ibuf[0] != -3579, "test70 case 7 failed\n");
	FAILED(ibuf[1] != -4830, "test70 case 8 failed\n");

	sljit_free_code(code.code, NULL);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		wbuf[0] = 0;
		ibuf[0] = 0;
		dbuf[0] = 0;
		fbuf[0] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32, F32, W, F64), 2, 2, 2, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR1, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR0, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f3(-6834, 674.5, 2789, -895.25);

		FAILED(wbuf[0] != 2789, "test70 case 9 failed\n");
		FAILED(ibuf[0] != -6834, "test70 case 10 failed\n");
		FAILED(dbuf[0] != -895.25, "test70 case 11 failed\n");
		FAILED(fbuf[0] != 674.5, "test70 case 12 failed\n");

		ibuf[0] = 0;
		dbuf[0] = 0;
		fbuf[0] = 0;
		fbuf[1] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F32, F64, F32, 32), 1, 1, 3, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&fbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f32), SLJIT_FR2, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f4(-4712.5, 5342.25, 2904.25, -4607);

		FAILED(ibuf[0] != -4607, "test70 case 13 failed\n");
		FAILED(dbuf[0] != 5342.25, "test70 case 14 failed\n");
		FAILED(fbuf[0] != -4712.5, "test70 case 15 failed\n");
		FAILED(fbuf[1] != 2904.25, "test70 case 16 failed\n");

		ibuf[0] = 0;
		dbuf[0] = 0;
		fbuf[0] = 0;
		fbuf[1] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F32, 32, F32), 1, 1, 3, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&fbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR1, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f32), SLJIT_FR2, 0);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f5(3578.5, 4619.25, 6859, -1807.75);

		FAILED(ibuf[0] != 6859, "test70 case 17 failed\n");
		FAILED(dbuf[0] != 3578.5, "test70 case 18 failed\n");
		FAILED(fbuf[0] != 4619.25, "test70 case 19 failed\n");
		FAILED(fbuf[1] != -1807.75, "test70 case 20 failed\n");

		ibuf[0] = 0;
		dbuf[0] = 0;
		dbuf[1] = 0;
		fbuf[0] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, 32, F32, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 2, 1, 3, 0, 33);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR2, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR1, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f6(2740.75, -2651, -7909.25, 3671.5);

		FAILED(ibuf[0] != -2651, "test70 case 21 failed\n");
		FAILED(dbuf[0] != 2740.75, "test70 case 22 failed\n");
		FAILED(dbuf[1] != 3671.5, "test70 case 23 failed\n");
		FAILED(fbuf[0] != -7909.25, "test70 case 24 failed\n");

		wbuf[0] = 0;
		ibuf[0] = 0;
		ibuf[1] = 0;
		fbuf[0] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F32, 32, W, 32), 1, 3, 1, 0, 1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_s32), SLJIT_S2, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR0, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f7(-5219.25, -4530, 7214, 6741);

		FAILED(wbuf[0] != 7214, "test70 case 25 failed\n");
		FAILED(ibuf[0] != -4530, "test70 case 26 failed\n");
		FAILED(ibuf[1] != 6741, "test70 case 27 failed\n");
		FAILED(fbuf[0] != -5219.25, "test70 case 28 failed\n");

		wbuf[0] = 0;
		wbuf[1] = 0;
		dbuf[0] = 0;
		dbuf[1] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F64, W, W), 1, 5, 2, 0, SLJIT_MAX_LOCAL_SIZE - 1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_S0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_SP), SLJIT_MAX_LOCAL_SIZE - 2 * sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f8(-3749.75, 5280.5, 9134, -6506);

		FAILED(wbuf[0] != 9134, "test70 case 29 failed\n");
		FAILED(wbuf[1] != -6506, "test70 case 30 failed\n");
		FAILED(dbuf[0] != -3749.75, "test70 case 31 failed\n");
		FAILED(dbuf[1] != 5280.5, "test70 case 32 failed\n");

		wbuf[0] = 0;
		dbuf[0] = 0;
		dbuf[1] = 0;
		dbuf[2] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F64, W, F64), 1, 1, 3, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f9(-6049.25, 7301.5, 4610, -4312.75);

		FAILED(wbuf[0] != 4610, "test70 case 33 failed\n");
		FAILED(dbuf[0] != -6049.25, "test70 case 34 failed\n");
		FAILED(dbuf[1] != 7301.5, "test70 case 35 failed\n");
		FAILED(dbuf[2] != -4312.75, "test70 case 36 failed\n");

		ibuf[0] = 0;
		dbuf[0] = 0;
		dbuf[1] = 0;
		dbuf[2] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F64, F64, 32), 1, 1, 3, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test70_f10(4810.5, -9148.75, 8601.25, 6703);

		FAILED(ibuf[0] != 6703, "test70 case 37 failed\n");
		FAILED(dbuf[0] != 4810.5, "test70 case 38 failed\n");
		FAILED(dbuf[1] != -9148.75, "test70 case 39 failed\n");
		FAILED(dbuf[2] != 8601.25, "test70 case 40 failed\n");
	}

	successful_tests++;
}

static sljit_sw test71_f1(sljit_sw a)
{
	return a + 10000;
}

static sljit_sw test71_f2(sljit_sw a, sljit_s32 b, sljit_s32 c, sljit_sw d)
{
	return a | b | c | d;
}

static sljit_sw test71_f3(sljit_sw a, sljit_s32 b, sljit_s32 c, sljit_sw d)
{
	SLJIT_UNUSED_ARG(a);
	return b | c | d;
}

static sljit_sw test71_f4(void)
{
	return 7461932;
}

static sljit_sw test71_f5(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_f64 d)
{
	if (a == 1345.5 && b == -8724.25 && c == 9034.75 && d == 6307.5)
		return 8920567;
	return 0;
}

static sljit_sw test71_f6(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_sw d)
{
	if (a == 4061.25 && b == -3291.75 && c == 8703.5 && d == 1706)
		return 5074526;
	return 0;
}

static void test71(void)
{
	/* Test tail calls. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump;
	sljit_uw jump_addr;
	sljit_sw executable_offset;
	sljit_sw wbuf[1];
	sljit_f64 dbuf[4];

	if (verbose)
		printf("Run test71\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, W), 4, 4, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, -1);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f1));
	/* Should crash. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func1(7987) != 17987, "test71 case 1 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, W), 1, 4, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, -1);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_REWRITABLE_JUMP | SLJIT_CALL_RETURN, SLJIT_ARGS1(W, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_target(jump, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	executable_offset = sljit_get_executable_offset(compiler);
	jump_addr = sljit_get_jump_addr(jump);
	sljit_free_compiler(compiler);

	sljit_set_jump_addr(jump_addr, SLJIT_FUNC_UADDR(test71_f1), executable_offset);

	FAILED(code.func1(3903) != 13903, "test71 case 2 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f2));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0x28000000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0x00140000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_IMM, 0x00002800);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0x00000041);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, W, 32, 32, W), SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != 0x28142841, "test71 case 3 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 4, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f2));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0x81000000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0x00480000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_IMM, 0x00002100);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0x00000014);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, W, 32, 32, W), SLJIT_S3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != (sljit_sw)0x81482114, "test71 case 4 failed\n");

	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 0, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f3));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0x342);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_IMM, 0x451000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0x21000000);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, W, 32, 32, W), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != 0x21451342, "test71 case 5 failed\n");

	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 1, 0, 0, 0, 9);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS0(W), SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f4));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	FAILED(code.func0() != 7461932, "test71 case 6 failed\n");

	sljit_free_code(code.code, NULL);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		/* Next test. */

		dbuf[0] = 9034.75;
		dbuf[1] = 6307.5;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS2(W, F32, F64), 1, 1, 4, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_F32, SLJIT_FR0, 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R0), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64));
		sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, F64), SLJIT_IMM, SLJIT_FUNC_ADDR(test71_f5));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		FAILED(code.test71_f1(1345.5, -8724.25) != 8920567, "test71 case 7 failed\n");

		sljit_free_code(code.code, NULL);

		/* Next test. */

		wbuf[0] = SLJIT_FUNC_ADDR(test71_f5);

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(W, F64, F64, F64, F64), 1, 0, 4, 0, 0);
		sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, F64), SLJIT_MEM0(), (sljit_sw)wbuf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		FAILED(code.test71_f2(1345.5, -8724.25, 9034.75, 6307.5) != 8920567, "test71 case 8 failed\n");

		sljit_free_code(code.code, NULL);

		/* Next test. */

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), 1, 0, 4, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
		jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		sljit_set_target(jump, SLJIT_FUNC_UADDR(test71_f6));

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		FAILED(code.test71_f3(4061.25, -3291.75, 8703.5) != 5074526, "test71 case 9 failed\n");

		sljit_free_code(code.code, NULL);

		/* Next test. */

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 1, 0, 4, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
		jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		sljit_set_target(jump, SLJIT_FUNC_UADDR(test71_f6));

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		FAILED(code.test71_f3(4061.25, -3291.75, 8703.5) != 5074526, "test71 case 10 failed\n");

		sljit_free_code(code.code, NULL);

		/* Next test. */

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 1, 1, 3, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
		jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		sljit_set_target(jump, SLJIT_FUNC_UADDR(test71_f6));

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		FAILED(code.test71_f3(4061.25, -3291.75, 8703.5) != 5074526, "test71 case 11 failed\n");

		sljit_free_code(code.code, NULL);
	}

	successful_tests++;
}

static void test72(void)
{
	/* Test using all fpu registers. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_f64 buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS];
	sljit_f64 buf2[2];
	struct sljit_jump *jump;
	sljit_s32 i;

	if (verbose)
		printf("Run test72\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test72 skipped\n");
		successful_tests++;
		if (compiler)
			sljit_free_compiler(compiler);
		return;
	}

	/* Next test. */
	FAILED(!compiler, "cannot create compiler\n");

	buf2[0] = 7.75;
	buf2[1] = -8.25;

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		buf[i] = 0.0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 1, 2, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S1, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS1(VOID, W));

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), i * (sljit_sw)sizeof(sljit_f64), SLJIT_FR(i), 0);
	sljit_emit_return_void(compiler);

	/* Called function. */
	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

	sljit_set_context(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)buf, (sljit_sw)buf2);

	for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++) {
		FAILED(buf[i] != -8.25, "test72 case 1 failed\n");
	}

	for (i = SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++) {
		FAILED(buf[i] != 7.75, "test72 case 2 failed\n");
	}

	sljit_free_code(code.code, NULL);

	/* Next test. */
	if (SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS >= 3) {
		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		buf2[0] = -6.25;
		buf2[1] = 3.75;

		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
			buf[i] = 0.0;

		sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 1, 2, SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2, 1, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FS0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FS0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S1, 0);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS1(VOID, W));

		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), i * (sljit_sw)sizeof(sljit_f64), SLJIT_FR(i), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), (SLJIT_NUMBER_OF_FLOAT_REGISTERS - 1) * (sljit_sw)sizeof(sljit_f64), SLJIT_FS0, 0);
		sljit_emit_return_void(compiler);

		/* Called function. */
		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, SLJIT_MAX_LOCAL_SIZE);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
		for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

		sljit_set_context(compiler, 0, SLJIT_ARGS1(VOID, P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func2((sljit_sw)buf, (sljit_sw)buf2);

		for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++) {
			FAILED(buf[i] != 3.75, "test72 case 3 failed\n");
		}

		for (i = SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++) {
			FAILED(buf[i] != -6.25, "test72 case 4 failed\n");
		}

		FAILED(buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2] != 0, "test72 case 5 failed\n");
		FAILED(buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS - 1] != -6.25, "test72 case 6 failed\n");

		sljit_free_code(code.code, NULL);
	}
	successful_tests++;
}

static void test73(void)
{
	/* Test pasing arguments in registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_sw wbuf[2];
	sljit_s32 ibuf[2];
	sljit_f64 dbuf[3];

	if (verbose)
		printf("Run test73\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32_R, W, W_R, 32), 3, 2, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_s32), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test73_f1(3467, -6781, 5038, 6310);

	FAILED(wbuf[0] != -6781, "test73 case 1 failed\n");
	FAILED(wbuf[1] != 5038, "test73 case 2 failed\n");
	FAILED(ibuf[0] != 3467, "test73 case 3 failed\n");
	FAILED(ibuf[1] != 6310, "test73 case 4 failed\n");
	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32, W_R, W, 32_R), 4, 2, 0, 0, 8192);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_s32), SLJIT_R3, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test73_f1(-9723, 5208, 4761, 5084);

	FAILED(wbuf[0] != 5208, "test73 case 5 failed\n");
	FAILED(wbuf[1] != 4761, "test73 case 6 failed\n");
	FAILED(ibuf[0] != -9723, "test73 case 7 failed\n");
	FAILED(ibuf[1] != 5084, "test73 case 8 failed\n");
	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, 32_R, W_R, W_R, 32_R), 4, 1, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_s32), SLJIT_R3, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test73_f1(5934, 6043, -8572, -3861);

	FAILED(wbuf[0] != 6043, "test73 case 9 failed\n");
	FAILED(wbuf[1] != -8572, "test73 case 10 failed\n");
	FAILED(ibuf[0] != 5934, "test73 case 11 failed\n");
	FAILED(ibuf[1] != -3861, "test73 case 12 failed\n");
	sljit_free_code(code.code, NULL);

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, W_R, W_R, 32_R, 32_R), 4, 1, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_set_context(compiler, 0, SLJIT_ARGS4(VOID, W_R, W_R, 32_R, 32_R), 4, 1, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_s32), SLJIT_R3, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test73_f2(6732, -5916, 2740, -3621);

	FAILED(wbuf[0] != 6732, "test73 case 13 failed\n");
	FAILED(wbuf[1] != -5916, "test73 case 14 failed\n");
	FAILED(ibuf[0] != 2740, "test73 case 15 failed\n");
	FAILED(ibuf[1] != -3621, "test73 case 16 failed\n");
	sljit_free_code(code.code, NULL);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		/* Next test. */

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F64, F64, W_R), 1, 0, 3, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test73_f3(7390.25, -8045.5, 1390.75, 8201);

		FAILED(wbuf[0] != 8201, "test73 case 17 failed\n");
		FAILED(dbuf[0] != 7390.25, "test73 case 18 failed\n");
		FAILED(dbuf[1] != -8045.5, "test73 case 19 failed\n");
		FAILED(dbuf[2] != 1390.75, "test73 case 20 failed\n");

		/* Next test. */

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS4(VOID, F64, F64, W, W_R), 2, 1, 2, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_R1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.test73_f4(4892.75, -3702.5, 4731, 8530);

		FAILED(wbuf[0] != 4731, "test73 case 21 failed\n");
		FAILED(wbuf[1] != 8530, "test73 case 22 failed\n");
		FAILED(dbuf[0] != 4892.75, "test73 case 23 failed\n");
		FAILED(dbuf[1] != -3702.5, "test73 case 24 failed\n");
	}

	successful_tests++;
}

static void test74(void)
{
	/* Test carry flag. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_sw wbuf[15];
	sljit_s32 i;

	if (verbose)
		printf("Run test74\n");

	for (i = 0; i < 15; i++)
		wbuf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(VOID, W), 3, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_set_current_flags(compiler, SLJIT_SET_CARRY | SLJIT_CURRENT_FLAGS_ADD);
	/* wbuf[2] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_NOT_CARRY);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SHL32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 31);
	sljit_emit_op2u(compiler, SLJIT_ADD32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op2(compiler, SLJIT_ADD32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 5);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_NOT_CARRY);
	/* wbuf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	/* wbuf[7] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_CARRY);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2u(compiler, SLJIT_SUB32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_set_current_flags(compiler, SLJIT_SET_CARRY | SLJIT_CURRENT_FLAGS_32 | SLJIT_CURRENT_FLAGS_SUB);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_NOT_CARRY);
	/* wbuf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SHL32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 31);
	sljit_emit_op2(compiler, SLJIT_SUB32, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SUB32 | SLJIT_SET_CARRY, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_R0, 0);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_NOT_CARRY);
	/* wbuf[9] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, -2);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_NOT_CARRY);
	/* wbuf[10] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_ADDC | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R0, 0);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[11] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SHL32, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 31);
	sljit_emit_op2(compiler, SLJIT_ADD32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_ADDC32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R0, 0);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[12] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUB | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUBC | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_CARRY);
	/* wbuf[13] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op2(compiler, SLJIT_SUB32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op2(compiler, SLJIT_SUBC32 | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 1);
	cond_set(compiler, SLJIT_R1, 0, SLJIT_NOT_CARRY);
	/* wbuf[14] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 14 * sizeof(sljit_sw), SLJIT_R1, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&wbuf);

	FAILED(wbuf[0] != 1, "test74 case 1 failed\n");
	FAILED(wbuf[1] != 1, "test74 case 2 failed\n");
	FAILED(wbuf[2] != 2, "test74 case 3 failed\n");
	FAILED(wbuf[3] != 1, "test74 case 4 failed\n");
	FAILED(wbuf[4] != 2, "test74 case 5 failed\n");
	FAILED(wbuf[5] != 1, "test74 case 6 failed\n");
	FAILED(wbuf[6] != 1, "test74 case 7 failed\n");
	FAILED(wbuf[7] != 1, "test74 case 8 failed\n");
	FAILED(wbuf[8] != 2, "test74 case 9 failed\n");
	FAILED(wbuf[9] != 2, "test74 case 10 failed\n");
	FAILED(wbuf[10] != 1, "test74 case 11 failed\n");
	FAILED(wbuf[11] != 2, "test74 case 12 failed\n");
	FAILED(wbuf[12] != 1, "test74 case 13 failed\n");
	FAILED(wbuf[13] != 1, "test74 case 14 failed\n");
	FAILED(wbuf[14] != 1, "test74 case 15 failed\n");

	successful_tests++;
}

static void test75_set(struct sljit_compiler *compiler, sljit_s32 compare, sljit_s32 type, sljit_s32 left_fr, sljit_s32 right_fr)
{
	/* Testing both sljit_emit_op_flags and sljit_emit_jump. */
	struct sljit_jump* jump1;
	struct sljit_jump* jump2;
	sljit_s32 is_ordered;

	if (sljit_cmp_info(type)) {
		sljit_emit_fop1(compiler, compare | SLJIT_SET(type), left_fr, 0, right_fr, 0);
		sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R0, 0, type);
		jump1 = sljit_emit_jump(compiler, type);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2);
		sljit_set_label(jump1, sljit_emit_label(compiler));
	} else {
		if (type < SLJIT_UNORDERED_OR_EQUAL) {
			is_ordered = (type & 0x1) ^ 0x1;
			type += SLJIT_UNORDERED_OR_EQUAL - SLJIT_ORDERED_EQUAL;
		} else {
			is_ordered = type & 0x1;
			type -= SLJIT_UNORDERED_OR_EQUAL - SLJIT_ORDERED_EQUAL;
		}
		SLJIT_ASSERT(sljit_cmp_info(type) && sljit_cmp_info(SLJIT_UNORDERED) && sljit_cmp_info(SLJIT_ORDERED));

		sljit_emit_fop1(compiler, compare | SLJIT_SET(type), left_fr, 0, right_fr, 0);
		sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R0, 0, type);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);

		if (is_ordered) {
			sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_ORDERED);

			jump1 = sljit_emit_jump(compiler, SLJIT_UNORDERED);
			jump2 = sljit_emit_jump(compiler, type);
			sljit_set_label(jump1, sljit_emit_label(compiler));
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
			sljit_set_label(jump2, sljit_emit_label(compiler));

			sljit_emit_op2(compiler, SLJIT_AND, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R1, 0);
		} else {
			sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_UNORDERED);

			jump1 = sljit_emit_jump(compiler, SLJIT_UNORDERED);
			jump2 = sljit_emit_jump(compiler, type);
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
			sljit_set_label(jump1, sljit_emit_label(compiler));
			sljit_set_label(jump2, sljit_emit_label(compiler));

			sljit_emit_op2(compiler, SLJIT_OR, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R1, 0);
		}

		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_R2, 0);
	}

	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, sizeof(sljit_s8));
}

static void test75(void)
{
	/* Test floating point comparison. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s8 bbuf[96];
	sljit_s32 i;

	union {
		sljit_f64 value;
		struct {
			sljit_s32 value1;
			sljit_s32 value2;
		} u;
	} dbuf[3];

	union {
		sljit_f32 value;
		sljit_s32 value1;
	} sbuf[3];

	dbuf[0].u.value1 = 0x7fffffff;
	dbuf[0].u.value2 = 0x7fffffff;
	dbuf[1].value = -13.0;
	dbuf[2].value = 27.0;

	sbuf[0].value1 = 0x7fffffff;
	sbuf[1].value = -13.0;
	sbuf[2].value = 27.0;

	if (verbose)
		printf("Run test75\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test75 skipped\n");
		successful_tests++;
		return;
	}

	for (i = 0; i < 96; i++)
		bbuf[i] = -3;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 3, 3, 6, 0, 0);

	i = SLJIT_CMP_F64;
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));

	while (1) {
		/* bbuf[0] and bbuf[48] */
		test75_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[1] and bbuf[49] */
		test75_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[2] and bbuf[50] */
		test75_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[3] and bbuf[51] */
		test75_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[4] and bbuf[52] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[5] and bbuf[53] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[6] and bbuf[54] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[7] and bbuf[55] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[8] and bbuf[56] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[9] and bbuf[57] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[10] and bbuf[58] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[11] and bbuf[59] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[12] and bbuf[60] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[13] and bbuf[61] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[14] and bbuf[62] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[15] and bbuf[63] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[16] and bbuf[64] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[17] and bbuf[65] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[18] and bbuf[66] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[19] and bbuf[67] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[20] and bbuf[68] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[21] and bbuf[69] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[22] and bbuf[70] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[23] and bbuf[71] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[24] and bbuf[72] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[25] and bbuf[73] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[26] and bbuf[74] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[27] and bbuf[75] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[28] and bbuf[76] */
		test75_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[29] and bbuf[77] */
		test75_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[30] and bbuf[78] */
		test75_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[31] and bbuf[79] */
		test75_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[32] and bbuf[80] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[33] and bbuf[81] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[34] and bbuf[82] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[35] and bbuf[83] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[36] and bbuf[84] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[37] and bbuf[85] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[38] and bbuf[86] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[39] and bbuf[87] */
		test75_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[40] and bbuf[88] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[41] and bbuf[89] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[42] and bbuf[90] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[43] and bbuf[91] */
		test75_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[44] and bbuf[92] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[45] and bbuf[93] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[46] and bbuf[94] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[47] and bbuf[95] */
		test75_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR0, SLJIT_FR2);

		if (i == SLJIT_CMP_F32)
			break;

		i = SLJIT_CMP_F32;
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32));
	}

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&bbuf, (sljit_sw)&dbuf, (sljit_sw)&sbuf);

	/* SLJIT_CMP_F64 */
	FAILED(bbuf[0] != 1, "test75 case 1 failed\n");
	FAILED(bbuf[1] != 2, "test75 case 2 failed\n");
	FAILED(bbuf[2] != 2, "test75 case 3 failed\n");
	FAILED(bbuf[3] != 2, "test75 case 4 failed\n");

	FAILED(bbuf[4] != 2, "test75 case 5 failed\n");
	FAILED(bbuf[5] != 1, "test75 case 6 failed\n");
	FAILED(bbuf[6] != 1, "test75 case 7 failed\n");
	FAILED(bbuf[7] != 1, "test75 case 8 failed\n");

	FAILED(bbuf[8] != 2, "test75 case 9 failed\n");
	FAILED(bbuf[9] != 1, "test75 case 10 failed\n");
	FAILED(bbuf[10] != 2, "test75 case 11 failed\n");
	FAILED(bbuf[11] != 2, "test75 case 12 failed\n");

	FAILED(bbuf[12] != 2, "test75 case 13 failed\n");
	FAILED(bbuf[13] != 1, "test75 case 14 failed\n");
	FAILED(bbuf[14] != 1, "test75 case 15 failed\n");
	FAILED(bbuf[15] != 1, "test75 case 16 failed\n");

	FAILED(bbuf[16] != 2, "test75 case 17 failed\n");
	FAILED(bbuf[17] != 1, "test75 case 18 failed\n");
	FAILED(bbuf[18] != 2, "test75 case 19 failed\n");
	FAILED(bbuf[19] != 2, "test75 case 20 failed\n");

	FAILED(bbuf[20] != 1, "test75 case 21 failed\n");
	FAILED(bbuf[21] != 2, "test75 case 22 failed\n");
	FAILED(bbuf[22] != 1, "test75 case 23 failed\n");
	FAILED(bbuf[23] != 1, "test75 case 24 failed\n");

	FAILED(bbuf[24] != 2, "test75 case 25 failed\n");
	FAILED(bbuf[25] != 1, "test75 case 26 failed\n");
	FAILED(bbuf[26] != 1, "test75 case 27 failed\n");
	FAILED(bbuf[27] != 1, "test75 case 28 failed\n");

	FAILED(bbuf[28] != 2, "test75 case 29 failed\n");
	FAILED(bbuf[29] != 1, "test75 case 30 failed\n");
	FAILED(bbuf[30] != 2, "test75 case 31 failed\n");
	FAILED(bbuf[31] != 2, "test75 case 32 failed\n");

	FAILED(bbuf[32] != 1, "test75 case 33 failed\n");
	FAILED(bbuf[33] != 2, "test75 case 34 failed\n");
	FAILED(bbuf[34] != 1, "test75 case 35 failed\n");
	FAILED(bbuf[35] != 1, "test75 case 36 failed\n");

	FAILED(bbuf[36] != 2, "test75 case 37 failed\n");
	FAILED(bbuf[37] != 1, "test75 case 38 failed\n");
	FAILED(bbuf[38] != 2, "test75 case 39 failed\n");
	FAILED(bbuf[39] != 2, "test75 case 40 failed\n");

	FAILED(bbuf[40] != 2, "test75 case 41 failed\n");
	FAILED(bbuf[41] != 1, "test75 case 42 failed\n");
	FAILED(bbuf[42] != 1, "test75 case 43 failed\n");
	FAILED(bbuf[43] != 1, "test75 case 44 failed\n");

	FAILED(bbuf[44] != 1, "test75 case 45 failed\n");
	FAILED(bbuf[45] != 2, "test75 case 46 failed\n");
	FAILED(bbuf[46] != 2, "test75 case 47 failed\n");
	FAILED(bbuf[47] != 2, "test75 case 48 failed\n");

	/* SLJIT_CMP_F32 */
	FAILED(bbuf[48] != 1, "test75 case 49 failed\n");
	FAILED(bbuf[49] != 2, "test75 case 50 failed\n");
	FAILED(bbuf[50] != 2, "test75 case 51 failed\n");
	FAILED(bbuf[51] != 2, "test75 case 52 failed\n");

	FAILED(bbuf[52] != 2, "test75 case 53 failed\n");
	FAILED(bbuf[53] != 1, "test75 case 54 failed\n");
	FAILED(bbuf[54] != 1, "test75 case 55 failed\n");
	FAILED(bbuf[55] != 1, "test75 case 56 failed\n");

	FAILED(bbuf[56] != 2, "test75 case 57 failed\n");
	FAILED(bbuf[57] != 1, "test75 case 58 failed\n");
	FAILED(bbuf[58] != 2, "test75 case 59 failed\n");
	FAILED(bbuf[59] != 2, "test75 case 60 failed\n");

	FAILED(bbuf[60] != 2, "test75 case 61 failed\n");
	FAILED(bbuf[61] != 1, "test75 case 62 failed\n");
	FAILED(bbuf[62] != 1, "test75 case 63 failed\n");
	FAILED(bbuf[63] != 1, "test75 case 64 failed\n");

	FAILED(bbuf[64] != 2, "test75 case 65 failed\n");
	FAILED(bbuf[65] != 1, "test75 case 66 failed\n");
	FAILED(bbuf[66] != 2, "test75 case 67 failed\n");
	FAILED(bbuf[67] != 2, "test75 case 68 failed\n");

	FAILED(bbuf[68] != 1, "test75 case 69 failed\n");
	FAILED(bbuf[69] != 2, "test75 case 70 failed\n");
	FAILED(bbuf[70] != 1, "test75 case 71 failed\n");
	FAILED(bbuf[71] != 1, "test75 case 72 failed\n");

	FAILED(bbuf[72] != 2, "test75 case 73 failed\n");
	FAILED(bbuf[73] != 1, "test75 case 74 failed\n");
	FAILED(bbuf[74] != 1, "test75 case 75 failed\n");
	FAILED(bbuf[75] != 1, "test75 case 76 failed\n");

	FAILED(bbuf[76] != 2, "test75 case 77 failed\n");
	FAILED(bbuf[77] != 1, "test75 case 78 failed\n");
	FAILED(bbuf[78] != 2, "test75 case 79 failed\n");
	FAILED(bbuf[79] != 2, "test75 case 80 failed\n");

	FAILED(bbuf[80] != 1, "test75 case 81 failed\n");
	FAILED(bbuf[81] != 2, "test75 case 82 failed\n");
	FAILED(bbuf[82] != 1, "test75 case 83 failed\n");
	FAILED(bbuf[83] != 1, "test75 case 84 failed\n");

	FAILED(bbuf[84] != 2, "test75 case 85 failed\n");
	FAILED(bbuf[85] != 1, "test75 case 86 failed\n");
	FAILED(bbuf[86] != 2, "test75 case 87 failed\n");
	FAILED(bbuf[87] != 2, "test75 case 88 failed\n");

	FAILED(bbuf[88] != 2, "test75 case 89 failed\n");
	FAILED(bbuf[89] != 1, "test75 case 90 failed\n");
	FAILED(bbuf[90] != 1, "test75 case 91 failed\n");
	FAILED(bbuf[91] != 1, "test75 case 92 failed\n");

	FAILED(bbuf[92] != 1, "test75 case 93 failed\n");
	FAILED(bbuf[93] != 2, "test75 case 94 failed\n");
	FAILED(bbuf[94] != 2, "test75 case 95 failed\n");
	FAILED(bbuf[95] != 2, "test75 case 96 failed\n");

	successful_tests++;
}

static void test76(void)
{
	/* Test register argument and keep saved registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_sw buf[9];
	sljit_f64 dbuf[3];
	sljit_s32 i;

	if (verbose)
		printf("Run test76\n");

	/* Next test. */
	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 7945);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -9267);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 4309);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -8321);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6803);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -5497);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 4, 2, 0, 0, 32);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 6028);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4982);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1289);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 6028, "test76 case 1 failed\n");
	FAILED(buf[1] != 6803, "test76 case 2 failed\n");
	FAILED(buf[2] != -5497, "test76 case 3 failed\n");
	FAILED(buf[3] != 7945, "test76 case 4 failed\n");
	FAILED(buf[4] != -9267, "test76 case 5 failed\n");
	FAILED(buf[5] != 4309, "test76 case 6 failed\n");
	FAILED(buf[6] != -8321, "test76 case 7 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -2608);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 4751);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 5740);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -9704);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -8749);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 9213);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 6, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_set_context(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 6, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -7351);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 3628);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != -7351, "test76 case 8 failed\n");
	FAILED(buf[1] != 3628, "test76 case 9 failed\n");
	FAILED(buf[2] != 9213, "test76 case 10 failed\n");
	FAILED(buf[3] != -8749, "test76 case 11 failed\n");
	FAILED(buf[4] != -2608, "test76 case 12 failed\n");
	FAILED(buf[5] != 4751, "test76 case 13 failed\n");
	FAILED(buf[6] != 5740, "test76 case 14 failed\n");
	FAILED(buf[7] != -9704, "test76 case 15 failed\n");
	FAILED(buf[8] != -1, "test76 case 16 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8653);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 7245);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3610);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 4591);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -2865);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 2510);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(VOID, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 4 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 5 * sizeof(sljit_sw), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 6 * sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 7 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 5789);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -9214);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 5789, "test76 case 17 failed\n");
	FAILED(buf[1] != -9214, "test76 case 18 failed\n");
	FAILED(buf[2] != -2865, "test76 case 19 failed\n");
	FAILED(buf[3] != 2510, "test76 case 20 failed\n");
	FAILED(buf[4] != 8653, "test76 case 21 failed\n");
	FAILED(buf[5] != 7245, "test76 case 22 failed\n");
	FAILED(buf[6] != -3610, "test76 case 23 failed\n");
	FAILED(buf[7] != 4591, "test76 case 24 failed\n");
	FAILED(buf[8] != -1, "test76 case 25 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 2, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6071);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -3817);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 9250);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS0(W), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 5 * sizeof(sljit_sw), SLJIT_S1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -6278);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1467);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 7150 - 1467);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 8413);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4892);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -7513);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, W, W, W, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 4, 2, 0, 0, 256);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_R2, 0, SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 6923);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);

	FAILED(buf[0] != 6923, "test76 case 26 failed\n");
	FAILED(buf[1] != 4892, "test76 case 27 failed\n");
	FAILED(buf[2] != -7513, "test76 case 28 failed\n");
	FAILED(buf[3] != 9250, "test76 case 29 failed\n");
	FAILED(buf[4] != 6071, "test76 case 30 failed\n");
	FAILED(buf[5] != -3817, "test76 case 31 failed\n");
	FAILED(buf[6] != -6278, "test76 case 32 failed\n");
	FAILED(buf[7] != 7150, "test76 case 33 failed\n");
	FAILED(buf[8] != 8413, "test76 case 34 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		/* Next test. */

		for (i = 0; i < 9; i++)
			buf[i] = -1;

		dbuf[0] = 4061.25;
		dbuf[1] = -3291.75;
		dbuf[2] = 8703.5;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 2, 3, 3, 0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R1), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_f64));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -8956);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 4381);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -5314);

		jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, F64, F64, F64, W));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, F64, F64, F64, W_R), 1, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_R0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_FR2, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64), SLJIT_FR1, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2784);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 1503);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);

		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_R0, 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func1((sljit_sw)&buf);

		FAILED(buf[0] != 2784, "test76 case 35 failed\n");
		FAILED(buf[1] != 1503, "test76 case 36 failed\n");
		FAILED(buf[2] != 4381, "test76 case 37 failed\n");
		FAILED(buf[3] != -5314, "test76 case 38 failed\n");
		FAILED(buf[4] != -8956, "test76 case 39 failed\n");
		FAILED(buf[5] != 1706, "test76 case 40 failed\n");
		FAILED(buf[6] != -1, "test76 case 41 failed\n");
		FAILED(dbuf[0] != 8703.5, "test76 case 42 failed\n");
		FAILED(dbuf[1] != 4061.25, "test76 case 43 failed\n");
		FAILED(dbuf[2] != -3291.75, "test76 case 44 failed\n");

		/* Next test. */

		for (i = 0; i < 9; i++)
			buf[i] = -1;

		dbuf[0] = 4061.25;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 3, 3, 1, 0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM0(), (sljit_sw)&dbuf);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8793);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -4027);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2910);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4619);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1502);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 5316);

		jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(VOID, F64, W, W, W));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_sw), SLJIT_S2, 0);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS4(VOID, F64, W_R, W_R, W_R), 3, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 4 * sizeof(sljit_sw), SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 5 * sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 6 * sizeof(sljit_sw), SLJIT_R1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 7 * sizeof(sljit_sw), SLJIT_R2, 0);

		sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 7839);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -9215);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func1((sljit_sw)&buf);

		FAILED(buf[0] != 7839, "test76 case 45 failed\n");
		FAILED(buf[1] != -9215, "test76 case 46 failed\n");
		FAILED(buf[2] != 5316, "test76 case 47 failed\n");
		FAILED(buf[3] != 4619, "test76 case 48 failed\n");
		FAILED(buf[4] != -1502, "test76 case 49 failed\n");
		FAILED(buf[5] != 8793, "test76 case 50 failed\n");
		FAILED(buf[6] != -4027, "test76 case 51 failed\n");
		FAILED(buf[7] != 2910, "test76 case 52 failed\n");
		FAILED(buf[8] != -1, "test76 case 53 failed\n");
		FAILED(dbuf[0] != -4061.25, "test76 case 54 failed\n");

		/* Next test. */

		for (i = 0; i < 9; i++)
			buf[i] = -1;

		dbuf[0] = 4061.25;
		dbuf[1] = -3291.75;
		dbuf[2] = 8703.5;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), 2, 3, 0, 0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 7869);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -5406);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 4951);

		jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(W));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS0(W), 1, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 5 * sizeof(sljit_sw), SLJIT_S1, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R0), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64));

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4713);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -2078);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);

		jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS4(W, F64, F64, F64, W_R), 1, 0, 3, 0, 256);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&buf[6], SLJIT_R0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR2, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR0, 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR1, 0);

		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 5074);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func1((sljit_sw)&buf);

		FAILED(buf[0] != 5074, "test76 case 55 failed\n");
		FAILED(buf[1] != 4713, "test76 case 56 failed\n");
		FAILED(buf[2] != -2078, "test76 case 57 failed\n");
		FAILED(buf[3] != 4951, "test76 case 58 failed\n");
		FAILED(buf[4] != 7869, "test76 case 59 failed\n");
		FAILED(buf[5] != -5406, "test76 case 60 failed\n");
		FAILED(buf[6] != 1706, "test76 case 61 failed\n");
		FAILED(buf[7] != -1, "test76 case 62 failed\n");
		FAILED(dbuf[0] != 8703.5, "test76 case 63 failed\n");
		FAILED(dbuf[1] != 4061.25, "test76 case 64 failed\n");
		FAILED(dbuf[2] != -3291.75, "test76 case 65 failed\n");
	}

	successful_tests++;
}

static void copy_u8(void *dst, sljit_sw offset, const void *src, sljit_uw length)
{
	sljit_u8 *dst_p = (sljit_u8 *)dst + offset;
	const sljit_u8 *src_p = (sljit_u8 *)src;

	while (length-- != 0)
		*dst_p++ = *src_p++;
}

static int cmp_u8(const void *src1, sljit_sw offset, const void *src2, sljit_uw length)
{
	const sljit_u8 *src1_p = (sljit_u8 *)src1 + offset;
	const sljit_u8 *src2_p = (sljit_u8 *)src2;

	while (--length != 0) {
		if (*src1_p != *src2_p)
			return 0;
		src1_p++;
		src2_p++;
	}
	return 1;
}

static void test77(void)
{
	/* Test unaligned accesses. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_sw i;
	sljit_sw wbuf[13];
	sljit_s32 ibuf[1];
	sljit_s16 hbuf[1];
	sljit_f64 dbuf[5];
	sljit_f32 sbuf[3];
	sljit_s8 bbuf_start[40 + 8 /* for alignment */];
	sljit_s8 *bbuf = (sljit_s8 *)(((sljit_uw)bbuf_start + 7) & ~(sljit_uw)7);

	SLJIT_ASSERT(((sljit_uw)bbuf & 0x7) == 0);

	if (verbose)
		printf("Run test77\n");

	for (i = 0; i < 13; i++)
		wbuf[i] = -3;

	for (i = 0; i < 40; i++)
		bbuf[i] = -3;

	wbuf[0] = -46870;
	ibuf[0] = -38512;
	hbuf[0] = -28531;
	copy_u8(bbuf, 3, hbuf, sizeof(sljit_s16));
	copy_u8(bbuf, 5, ibuf, sizeof(sljit_s32));
	copy_u8(bbuf, 9, wbuf, sizeof(sljit_sw));
	copy_u8(bbuf, 18, ibuf, sizeof(sljit_s32));
	copy_u8(bbuf, 22, wbuf, sizeof(sljit_sw));
	copy_u8(bbuf, 32, wbuf, sizeof(sljit_sw));

	wbuf[0] = -62945;
	ibuf[0] = -90678;
	hbuf[0] = -17249;
	bbuf[0] = -73;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2(VOID, P, P), 2, 2, 0, 0, 0);

	sljit_emit_mem(compiler, SLJIT_MOV_S8 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM0(), (sljit_sw)bbuf);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_mem(compiler, SLJIT_MOV_U8 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), 0);
	/* wbuf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S8, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	/* bbuf[1] */
	sljit_emit_mem(compiler, SLJIT_MOV_U8 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_s8));

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 100000);
	sljit_emit_mem(compiler, SLJIT_MOV_S16 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), 100000 + 3);
	/* wbuf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 1000);
	sljit_emit_mem(compiler, SLJIT_MOV_U16 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), -1000 + 3);
	/* wbuf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R0, 0, SLJIT_MEM0(), (sljit_sw)&hbuf);
	/* bbuf[3] */
	sljit_emit_mem(compiler, SLJIT_MOV_S16 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_S1), 3);

	sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_S1), 5);
	/* wbuf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_mem(compiler, SLJIT_MOV_U32 | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 0);
	/* wbuf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 100000);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_MEM0(), (sljit_sw)&ibuf);
	/* bbuf[5] */
	sljit_emit_mem(compiler, SLJIT_MOV32 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), 100000 + 5);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 100000);
#if (defined SLJIT_UNALIGNED && SLJIT_UNALIGNED)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R1), -100000 + 9);
#else /* !SLJIT_UNALIGNED */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), -100000 + 9);
#endif /* SLJIT_UNALIGNED */
	/* wbuf[7] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	/* bbuf[9] */
#if (defined SLJIT_UNALIGNED && SLJIT_UNALIGNED)
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), -100000 + 9, SLJIT_R0, 0);
#else /* !SLJIT_UNALIGNED */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), -100000 + 9);
#endif /* SLJIT_UNALIGNED */

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 18 >> 1);
	sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_R0, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 1);
	/* wbuf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_MEM0(), (sljit_sw)&ibuf);
	/* bbuf[18] */
	sljit_emit_mem(compiler, SLJIT_MOV_S32 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_R0, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 1);

	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_R0, SLJIT_MEM0(), (sljit_sw)bbuf + 22);
	/* wbuf[9] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	/* bbuf[22] */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_R0, SLJIT_MEM0(), (sljit_sw)bbuf + 22);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 128);
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_R0, SLJIT_MEM1(SLJIT_R0), -128 + 32);
	/* wbuf[10] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 128);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	/* bbuf[32] */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_R0, SLJIT_MEM1(SLJIT_R1), -128 + 32);

	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_S0), 0);
	/* wbuf[11] */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_R0, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw));
	/* wbuf[12] */
	sljit_emit_mem(compiler, SLJIT_MOV | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_S0, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&wbuf, (sljit_sw)bbuf);

	FAILED(wbuf[1] != -73, "test77 case 1 failed\n");
	FAILED(wbuf[2] != (sljit_u8)-73, "test77 case 2 failed\n");
	FAILED(bbuf[1] != -73, "test77 case 3 failed\n");
	FAILED(wbuf[3] != -28531, "test77 case 4 failed\n");
	FAILED(wbuf[4] != (sljit_u16)-28531, "test77 case 5 failed\n");
	FAILED(cmp_u8(bbuf, 3, hbuf, sizeof(sljit_s16)) != 1, "test77 case 6 failed\n");
	FAILED(wbuf[5] != -38512, "test77 case 7 failed\n");
#if (defined SLJIT_64BIT_ARCHITECTURE && SLJIT_64BIT_ARCHITECTURE)
	FAILED(wbuf[6] != (sljit_u32)-38512, "test77 case 8 failed\n");
#else
	FAILED(wbuf[6] != -38512, "test77 case 8 failed\n");
#endif
	FAILED(cmp_u8(bbuf, 5, ibuf, sizeof(sljit_s32)) != 1, "test77 case 9 failed\n");
	FAILED(wbuf[7] != -46870, "test77 case 10 failed\n");
	FAILED(cmp_u8(bbuf, 9, wbuf, sizeof(sljit_sw)) != 1, "test77 case 11 failed\n");
	FAILED(wbuf[8] != -38512, "test77 case 12 failed\n");
	FAILED(cmp_u8(bbuf, 18, ibuf, sizeof(sljit_s32)) != 1, "test77 case 13 failed\n");
	FAILED(wbuf[9] != -46870, "test77 case 14 failed\n");
	FAILED(cmp_u8(bbuf, 22, wbuf, sizeof(sljit_sw)) != 1, "test77 case 15 failed\n");
	FAILED(wbuf[10] != -46870, "test77 case 16 failed\n");
	FAILED(cmp_u8(bbuf, 32, wbuf, sizeof(sljit_sw)) != 1, "test77 case 17 failed\n");
	FAILED(wbuf[11] != -62945, "test77 case 18 failed\n");
	FAILED(wbuf[12] != (sljit_sw)&wbuf, "test77 case 19 failed\n");

	sljit_free_code(code.code, NULL);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		for (i = 0; i < 40; i++)
			bbuf[i] = -3;

		for (i = 0; i < 5; i++)
			dbuf[i] = 0;

		dbuf[0] = 6897.75;
		sbuf[0] = -8812.25;
		copy_u8(bbuf, 1, sbuf, sizeof(sljit_f32));
		copy_u8(bbuf, 5, dbuf, sizeof(sljit_f64));
		copy_u8(bbuf, 14, sbuf, sizeof(sljit_f32));
		copy_u8(bbuf, 18, dbuf, sizeof(sljit_f64));
		copy_u8(bbuf, 28, dbuf, sizeof(sljit_f64));

		dbuf[0] = -18046.5;
		sbuf[0] = 3751.75;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS3(VOID, P, P, P), 1, 3, 1, 0, 0);

		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_UNALIGNED, SLJIT_FR0, SLJIT_MEM0(), (sljit_sw)bbuf + 1);
		/* sbuf[1] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32), SLJIT_FR0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		/* bbuf[1] */
		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_FR0, SLJIT_MEM0(), (sljit_sw)bbuf + 1);

		sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S2, 0, SLJIT_IMM, 100000);
#if (defined SLJIT_FPU_UNALIGNED && SLJIT_FPU_UNALIGNED)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R0), 100000 + 5);
#else /* !SLJIT_FPU_UNALIGNED */
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_UNALIGNED, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 100000 + 5);
#endif /* SLJIT_FPU_UNALIGNED */
		/* dbuf[1] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
		/* bbuf[5] */
#if (defined SLJIT_FPU_UNALIGNED && SLJIT_FPU_UNALIGNED)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 100000 + 5, SLJIT_FR0, 0);
#else /* !SLJIT_FPU_UNALIGNED */
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 100000 + 5);
#endif /* SLJIT_FPU_UNALIGNED */

		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_FR0, SLJIT_MEM1(SLJIT_S2), 14);
		/* sbuf[2] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_FR0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		/* bbuf[14] */
		sljit_emit_fmem(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_FR0, SLJIT_MEM1(SLJIT_S2), 14);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 18 >> 1);
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_FR0, SLJIT_MEM2(SLJIT_S2, SLJIT_R0), 1);
		/* dbuf[2] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
		/* bbuf[18] */
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_16, SLJIT_FR0, SLJIT_MEM2(SLJIT_S2, SLJIT_R0), 1);

		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S2, 0, SLJIT_IMM, 128);
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), -128 + 28);
		/* dbuf[3] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
		/* bbuf[28] */
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), -128 + 28);

		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
		/* dbuf[4] */
		sljit_emit_fmem(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_UNALIGNED | SLJIT_MEM_ALIGNED_32, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64));

		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func3((sljit_sw)&dbuf, (sljit_sw)&sbuf, (sljit_sw)bbuf);

		FAILED(sbuf[1] != -8812.25, "test77 case 20 failed\n");
		FAILED(cmp_u8(bbuf, 1, sbuf, sizeof(sljit_f32)) != 1, "test77 case 21 failed\n");
		FAILED(dbuf[1] != 6897.75, "test77 case 22 failed\n");
		FAILED(cmp_u8(bbuf, 5, dbuf, sizeof(sljit_f64)) != 1, "test77 case 23 failed\n");
		FAILED(sbuf[2] != -8812.25, "test77 case 24 failed\n");
		FAILED(cmp_u8(bbuf, 14, sbuf, sizeof(sljit_f32)) != 1, "test77 case 25 failed\n");
		FAILED(dbuf[2] != 6897.75, "test77 case 26 failed\n");
		FAILED(cmp_u8(bbuf, 18, dbuf, sizeof(sljit_f64)) != 1, "test77 case 27 failed\n");
		FAILED(dbuf[3] != 6897.75, "test77 case 28 failed\n");
		FAILED(cmp_u8(bbuf, 28, dbuf, sizeof(sljit_f64)) != 1, "test77 case 29 failed\n");
		FAILED(dbuf[4] != -18046.5, "test77 case 30 failed\n");

		sljit_free_code(code.code, NULL);
	}

	successful_tests++;
}

static void test78(void)
{
	/* Test register register preservation in keep saveds mode. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[6 + SLJIT_NUMBER_OF_REGISTERS];
	struct sljit_jump* jump;
	sljit_s32 i;

	if (verbose)
		printf("Run test78\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(VOID), SLJIT_NUMBER_OF_REGISTERS - 3, 3, 0, 0, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 8469 + 1805 * i);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 3671);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 2418);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 1597);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(VOID, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 6), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 3 * sizeof(sljit_sw), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 5 * sizeof(sljit_sw), SLJIT_S2, 0);

	for (i = 1; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), (6 + i) * (sljit_sw)sizeof(sljit_sw), SLJIT_R(i), 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS4(VOID, W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_set_context(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS4(VOID, W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 0), SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 1), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 2), SLJIT_S2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6501);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 7149);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 5732);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG | SLJIT_CALL_RETURN, SLJIT_ARGS0(VOID));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_MEM0(), 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS0(VOID), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE / 2);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();

	FAILED(buf[0] != 3671, "test78 case 1 failed\n");
	FAILED(buf[1] != 2418, "test78 case 2 failed\n");
	FAILED(buf[2] != 1597, "test78 case 3 failed\n");
	FAILED(buf[3] != 6501, "test78 case 4 failed\n");
	FAILED(buf[4] != 7149, "test78 case 5 failed\n");
	FAILED(buf[5] != 5732, "test78 case 6 failed\n");

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++) {
		FAILED(buf[6 + i] != 8469 + 1805 * i, "test78 case 7 failed\n");
	}

	successful_tests++;
}

int sljit_test(int argc, char* argv[])
{
	sljit_s32 has_arg = (argc >= 2 && argv[1][0] == '-' && argv[1][2] == '\0');
	verbose = has_arg && argv[1][1] == 'v';
	silent = has_arg && argv[1][1] == 's';

	if (!verbose && !silent)
		printf("Pass -v to enable verbose, -s to disable this hint.\n\n");

#if !(defined SLJIT_CONFIG_UNSUPPORTED && SLJIT_CONFIG_UNSUPPORTED)
	test_exec_allocator();
#endif
	test1();
	test2();
	test3();
	test4();
	test5();
	test6();
	test7();
	test8();
	test9();
	test10();
	test11();
	test12();
	test13();
	test14();
	test15();
	test16();
	test17();
	test18();
	test19();
	test20();
	test21();
	test22();
	test23();
	test24();
	test25();
	test26();
	test27();
	test28();
	test29();
	test30();
	test31();
	test32();
	test33();
	test34();
	test35();
	test36();
	test37();
	test38();
	test39();
	test40();
	test41();
	test42();
	test43();
	test44();
	test45();
	test46();
	test47();
	test48();
	test49();
	test50();
	test51();
	test52();
	test53();
	test54();
	test55();
	test56();
	test57();
	test58();
	test59();
	test60();
	test61();
	test62();
	test63();
	test64();
	test65();
	test66();
	test67();
	test68();
	test69();
	test70();
	test71();
	test72();
	test73();
	test74();
	test75();
	test76();
	test77();
	test78();

#if (defined SLJIT_EXECUTABLE_ALLOCATOR && SLJIT_EXECUTABLE_ALLOCATOR)
	sljit_free_unused_memory_exec();
#endif

#	define TEST_COUNT 78

	printf("SLJIT tests: ");
	if (successful_tests == TEST_COUNT)
		printf("all tests are " COLOR_GREEN "PASSED" COLOR_DEFAULT " ");
	else
		printf(COLOR_RED "%d" COLOR_DEFAULT " (" COLOR_RED "%d%%" COLOR_DEFAULT ") tests are " COLOR_RED "FAILED" COLOR_DEFAULT " ", TEST_COUNT - successful_tests, (TEST_COUNT - successful_tests) * 100 / TEST_COUNT);
	printf("on " COLOR_ARCH "%s" COLOR_DEFAULT "%s\n", sljit_get_platform_name(), sljit_has_cpu_feature(SLJIT_HAS_FPU) ? " (with fpu)" : " (without fpu)");

	return TEST_COUNT - successful_tests;

#	undef TEST_COUNT
}

#ifdef _MSC_VER
#pragma warning(pop)
#endif
