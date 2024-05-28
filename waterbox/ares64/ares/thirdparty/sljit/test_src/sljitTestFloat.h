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

static void test_float1(void)
{
	/* Test fpu monadic functions. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 buf[7];
	sljit_sw buf2[6];

	if (verbose)
		printf("Run test_float1\n");

	compiler = sljit_create_compiler(NULL, NULL);
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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 3, 2, 6, 0, 0);
	/* buf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&buf[2], SLJIT_MEM0(), (sljit_sw)&buf[1]);
	/* buf[3] */
	sljit_emit_fop1(compiler, SLJIT_ABS_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	/* buf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM0(), (sljit_sw)&buf[0]);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_FR2, 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_FR2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&buf[4], SLJIT_FR3, 0);
	/* buf[5] */
	sljit_emit_fop1(compiler, SLJIT_ABS_F64, SLJIT_FR4, 0, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR4, 0);
	/* buf[6] */
	sljit_emit_fop1(compiler, SLJIT_NEG_F64, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_FR4, 0);

	/* buf2[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_F_GREATER);
	/* buf2[1] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR5, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw), SLJIT_F_GREATER);
	/* buf2[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	/* buf2[3] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_FR1, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_F_LESS);
	/* buf2[4] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	/* buf2[5] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_NOT_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_F_NOT_EQUAL);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&buf2);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != -4.5, "test_float1 case 1 failed\n");
	FAILED(buf[3] != 4.5, "test_float1 case 2 failed\n");
	FAILED(buf[4] != -7.75, "test_float1 case 3 failed\n");
	FAILED(buf[5] != 4.5, "test_float1 case 4 failed\n");
	FAILED(buf[6] != -4.5, "test_float1 case 5 failed\n");

	FAILED(buf2[0] != 1, "test_float1 case 6 failed\n");
	FAILED(buf2[1] != 0, "test_float1 case 7 failed\n");
	FAILED(buf2[2] != 1, "test_float1 case 8 failed\n");
	FAILED(buf2[3] != 0, "test_float1 case 9 failed\n");
	FAILED(buf2[4] != 0, "test_float1 case 10 failed\n");
	FAILED(buf2[5] != 1, "test_float1 case 11 failed\n");

	successful_tests++;
}

static void test_float2(void)
{
	/* Test fpu diadic functions. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 buf[15];

	if (verbose)
		printf("Run test_float2\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 3, 1, 6, 0, 0);

	/* ADD */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	/* buf[3] */
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 3, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR1, 0, SLJIT_FR0, 0, SLJIT_FR1, 0);
	/* buf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 4, SLJIT_FR0, 0);
	/* buf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 5, SLJIT_FR1, 0);

	/* SUB */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	/* buf[6] */
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 6, SLJIT_FR3, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_F64_SHIFT);
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_FR2, 0, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2);
	sljit_emit_fop2(compiler, SLJIT_SUB_F64, SLJIT_FR3, 0, SLJIT_FR2, 0, SLJIT_FR3, 0);
	/* buf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 7, SLJIT_FR2, 0);
	/* buf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 8, SLJIT_FR3, 0);

	/* MUL */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 1);
	/* buf[9] */
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 9, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), SLJIT_F64_SHIFT, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_FR1, 0, SLJIT_FR1, 0, SLJIT_FR2, 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 2, SLJIT_FR2, 0);
	/* buf[10] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 10, SLJIT_FR1, 0);
	/* buf[11] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 11, SLJIT_FR5, 0);

	/* DIV */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 13);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR4, 0, SLJIT_FR5, 0);
	/* buf[12] */
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 12, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_FR5, 0, SLJIT_FR5, 0, SLJIT_FR1, 0);
	sljit_emit_fop2(compiler, SLJIT_DIV_F64, SLJIT_FR4, 0, SLJIT_FR1, 0, SLJIT_FR4, 0);
	/* buf[13] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 13, SLJIT_FR5, 0);
	/* buf[14] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64) * 14, SLJIT_FR4, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[3] != 10.75, "test_float2 case 1 failed\n");
	FAILED(buf[4] != 5.25, "test_float2 case 2 failed\n");
	FAILED(buf[5] != 7.0, "test_float2 case 3 failed\n");
	FAILED(buf[6] != 0.0, "test_float2 case 4 failed\n");
	FAILED(buf[7] != 5.5, "test_float2 case 5 failed\n");
	FAILED(buf[8] != 3.75, "test_float2 case 6 failed\n");
	FAILED(buf[9] != 24.5, "test_float2 case 7 failed\n");
	FAILED(buf[10] != 38.5, "test_float2 case 8 failed\n");
	FAILED(buf[11] != 9.625, "test_float2 case 9 failed\n");
	FAILED(buf[12] != 2.0, "test_float2 case 10 failed\n");
	FAILED(buf[13] != 2.0, "test_float2 case 11 failed\n");
	FAILED(buf[14] != 0.5, "test_float2 case 12 failed\n");

	successful_tests++;
}

static void test_float3(void)
{
	/* Floating point set flags. */
	executable_code code;
	struct sljit_compiler* compiler;
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
		printf("Run test_float3\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 16; i++)
		buf[i] = 5;

	/* Two NaNs */
	dbuf[0].u.value1 = 0x7fffffff;
	dbuf[0].u.value2 = 0x7fffffff;
	dbuf[1].u.value1 = 0x7fffffff;
	dbuf[1].u.value2 = 0x7fffffff;
	dbuf[2].value = -13.0;
	dbuf[3].value = 27.0;

	SLJIT_ASSERT(sizeof(sljit_f64) == 8 && sizeof(sljit_s32) == 4 && sizeof(dbuf[0]) == 8);

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 1, 2, 4, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));
	/* buf[0] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
	/* buf[1] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_ORDERED);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[2] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[3] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_ORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[4] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_F_LESS);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[5] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_F_GREATER_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[6] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_F_GREATER);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_LESS_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[7] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_F_LESS_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[8] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_NOT_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	/* buf[9] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_sw), SLJIT_F_NOT_EQUAL);

	sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR3, 0, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	/* buf[10] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_sw), SLJIT_UNORDERED);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_EQUAL, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64));
	/* buf[11] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_sw), SLJIT_F_EQUAL);

	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_ORDERED, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
	/* buf[12] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 12 * sizeof(sljit_sw), SLJIT_ORDERED);

	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR3, 0, SLJIT_FR2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	/* buf[13] */
	cond_set(compiler, SLJIT_MEM1(SLJIT_S0), 13 * sizeof(sljit_sw), SLJIT_UNORDERED);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&dbuf);

	FAILED(buf[0] != 1, "test_float3 case 1 failed\n");
	FAILED(buf[1] != 2, "test_float3 case 2 failed\n");
	FAILED(buf[2] != 2, "test_float3 case 3 failed\n");
	FAILED(buf[3] != 1, "test_float3 case 4 failed\n");
	FAILED(buf[4] != 1, "test_float3 case 5 failed\n");
	FAILED(buf[5] != 2, "test_float3 case 6 failed\n");
	FAILED(buf[6] != 2, "test_float3 case 7 failed\n");
	FAILED(buf[7] != 1, "test_float3 case 8 failed\n");
	FAILED(buf[8] != 2, "test_float3 case 9 failed\n");
	FAILED(buf[9] != 1, "test_float3 case 10 failed\n");
	FAILED(buf[10] != 2, "test_float3 case 11 failed\n");
	FAILED(buf[11] != 1, "test_float3 case 12 failed\n");
	FAILED(buf[12] != 2, "test_float3 case 13 failed\n");
	FAILED(buf[13] != 1, "test_float3 case 14 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test_float4(void)
{
	/* Test inline assembly. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_f64 buf[3];
#if (defined SLJIT_CONFIG_X86 && SLJIT_CONFIG_X86)
	sljit_u8 inst[16];
#else
	sljit_u32 inst;
#endif

	if (verbose)
		printf("Run test_float4\n");

	buf[0] = 13.5;
	buf[1] = -2.25;
	buf[2] = 0.0;

	compiler = sljit_create_compiler(NULL, NULL);
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 0, 1, 2, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	/* addsd x, xm */
	inst[0] = 0xf2;
	inst[1] = 0x0f;
	inst[2] = 0x58;
	inst[3] = (sljit_u8)(0xc0 | (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 3)
		| sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1));
	sljit_emit_op_custom(compiler, inst, 4);
#elif (defined SLJIT_CONFIG_X86_64 && SLJIT_CONFIG_X86_64)
	/* addsd x, xm */
	if (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) > 7 || sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) > 7) {
		inst[0] = 0;
		if (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) > 7)
			inst[0] |= 0x04; /* REX_R */
		if (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) > 7)
			inst[0] |= 0x01; /* REX_B */
		inst[1] = 0xf2;
		inst[2] = 0x0f;
		inst[3] = 0x58;
		inst[4] = (sljit_u8)(0xc0 | ((sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) & 0x7) << 3)
			| (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) & 0x7));
		sljit_emit_op_custom(compiler, inst, 5);
	} else {
		inst[0] = 0xf2;
		inst[1] = 0x0f;
		inst[2] = 0x58;
		inst[3] = (sljit_u8)(0xc0 | (sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 3)
			| sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1));
		sljit_emit_op_custom(compiler, inst, 4);
	}
#elif (defined SLJIT_CONFIG_ARM_32 && SLJIT_CONFIG_ARM_32)
	/* vadd.f64 dd, dn, dm */
	inst = 0xee300b00 | ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 12)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 16)
		| (sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	/* fadd rd, rn, rm */
	inst = 0x1e602800 | (sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 5)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) << 16);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_PPC && SLJIT_CONFIG_PPC)
	/* fadd frD, frA, frB */
	inst = (63u << 26) | (21u << 1) | ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 21)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 16)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) << 11);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_MIPS && SLJIT_CONFIG_MIPS)
	/* add.d fd, fs, ft */
	inst = (17u << 26) | (17u << 21) | ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 6)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 11)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) << 16);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_RISCV && SLJIT_CONFIG_RISCV)
	/* fadd.d rd, rs1, rs2 */
	inst = (0x1u << 25) | (0x7u << 12) | (0x53u)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 7)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 15)
		| (sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) << 20;
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_S390X && SLJIT_CONFIG_S390X)
	/* adbr r1, r2 */
	inst = 0xb31a0000
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 4)
		| (sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1);
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#elif (defined SLJIT_CONFIG_LOONGARCH && SLJIT_CONFIG_LOONGARCH)
	/* fadd.d rd, rs1, rs2 */
	inst = (0x202u << 15)
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0))
		| ((sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR0) << 5)
		| (sljit_u32)sljit_get_register_index(SLJIT_FLOAT_REGISTER, SLJIT_FR1) << 10;
	sljit_emit_op_custom(compiler, &inst, sizeof(sljit_u32));
#endif
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != 11.25, "test_float4 case 1 failed\n");

	successful_tests++;
}

static void test_float5(void)
{
	/* Test floating point compare. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_sw res[4];

	union {
		sljit_f64 value;
		struct {
			sljit_u32 value1;
			sljit_u32 value2;
		} u;
	} dbuf[4];

	if (verbose)
		printf("Run test_float5\n");

	compiler = sljit_create_compiler(NULL, NULL);
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

	res[0] = code.func1((sljit_sw)&dbuf);
	dbuf[3].value = 12;
	res[1] = code.func1((sljit_sw)&dbuf);
	dbuf[1].value = 0;
	res[2] = code.func1((sljit_sw)&dbuf);
	dbuf[2].value = 20;
	res[3] = code.func1((sljit_sw)&dbuf);

	sljit_free_code(code.code, NULL);

	FAILED(res[0] != 11, "test_float5 case 1 failed\n");
	FAILED(res[1] != -17, "test_float5 case 2 failed\n");
	FAILED(res[2] != 5, "test_float5 case 3 failed\n");
	FAILED(res[3] != -2, "test_float5 case 4 failed\n");

	successful_tests++;
}

static void test_float6(void)
{
	/* Test single precision floating point. */

	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f32 buf[12];
	sljit_sw buf2[6];
	struct sljit_jump* jump;

	if (verbose)
		printf("Run test_float6\n");

	compiler = sljit_create_compiler(NULL, NULL);
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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 3, 2, 6, 0, 0);

	/* buf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f32), SLJIT_FR0, 0);
	/* buf[3] */
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_FR1, 0, SLJIT_FR5, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f32), SLJIT_FR1, 0);
	/* buf[4] */
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f32), SLJIT_FR5, 0);
	/* buf[5] */
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f32), SLJIT_FR4, 0);

	/* buf[6] */
	sljit_emit_fop2(compiler, SLJIT_ADD_F32, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f32), SLJIT_FR0, 0);
	/* buf[7] */
	sljit_emit_fop2(compiler, SLJIT_SUB_F32, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f32), SLJIT_FR5, 0);
	/* buf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop2(compiler, SLJIT_MUL_F32, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f32), SLJIT_FR0, 0, SLJIT_FR0, 0);
	/* buf[9] */
	sljit_emit_fop2(compiler, SLJIT_DIV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f32), SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f32), SLJIT_FR2, 0);
	/* buf[10] */
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 0x3d0ac);
	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_MEM1(SLJIT_S0), 10 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R0), 0x3d0ac);
	/* buf[11] */
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 0x3d0ac + sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_MEM1(SLJIT_S0), 11 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R0), -0x3d0ac);

	/* buf2[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_F_EQUAL);
	/* buf2[1] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_LESS, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_sw), SLJIT_F_LESS);
	/* buf2[2] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_sw), SLJIT_F_EQUAL);
	/* buf2[3] */
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_F_GREATER_EQUAL, SLJIT_FR1, 0, SLJIT_FR2, 0);
	cond_set(compiler, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_sw), SLJIT_F_GREATER_EQUAL);

	/* buf2[4] */
	jump = sljit_emit_fcmp(compiler, SLJIT_F_LESS_EQUAL | SLJIT_32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_IMM, 7);
	sljit_set_label(jump, sljit_emit_label(compiler));

	/* buf2[5] */
	jump = sljit_emit_fcmp(compiler, SLJIT_F_GREATER | SLJIT_32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_FR2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_IMM, 6);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, (sljit_sw)&buf2);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != -5.5, "test_float6 case 1 failed\n");
	FAILED(buf[3] != 7.25, "test_float6 case 2 failed\n");
	FAILED(buf[4] != 7.25, "test_float6 case 3 failed\n");
	FAILED(buf[5] != -5.5, "test_float6 case 4 failed\n");
	FAILED(buf[6] != -1.75, "test_float6 case 5 failed\n");
	FAILED(buf[7] != 16.0, "test_float6 case 6 failed\n");
	FAILED(buf[8] != 30.25, "test_float6 case 7 failed\n");
	FAILED(buf[9] != 3, "test_float6 case 8 failed\n");
	FAILED(buf[10] != -5.5, "test_float6 case 9 failed\n");
	FAILED(buf[11] != 7.25, "test_float6 case 10 failed\n");
	FAILED(buf2[0] != 1, "test_float6 case 11 failed\n");
	FAILED(buf2[1] != 2, "test_float6 case 12 failed\n");
	FAILED(buf2[2] != 2, "test_float6 case 13 failed\n");
	FAILED(buf2[3] != 1, "test_float6 case 14 failed\n");
	FAILED(buf2[4] != 7, "test_float6 case 15 failed\n");
	FAILED(buf2[5] != -1, "test_float6 case 16 failed\n");

	successful_tests++;
}

static void test_float7(void)
{
	/* Test floating point conversions. */
	executable_code code;
	struct sljit_compiler* compiler;
	int i;
	sljit_f64 dbuf[10];
	sljit_f32 sbuf[10];
	sljit_sw wbuf[10];
	sljit_s32 ibuf[10];

	if (verbose)
		printf("Run test_float7\n");

	compiler = sljit_create_compiler(NULL, NULL);
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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 3, 3, 6, 0, 0);
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
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[3] != 476.25, "test_float7 case 1 failed\n");
	FAILED(dbuf[4] != 476.25, "test_float7 case 2 failed\n");
	FAILED(dbuf[5] != 2345.0, "test_float7 case 3 failed\n");
	FAILED(dbuf[6] != -6213.0, "test_float7 case 4 failed\n");
	FAILED(dbuf[7] != 312.0, "test_float7 case 5 failed\n");
	FAILED(dbuf[8] != -9324.0, "test_float7 case 6 failed\n");
	FAILED(dbuf[9] != -77.0, "test_float7 case 7 failed\n");

	FAILED(sbuf[2] != 123.5, "test_float7 case 8 failed\n");
	FAILED(sbuf[3] != 123.5, "test_float7 case 9 failed\n");
	FAILED(sbuf[4] != 476.25, "test_float7 case 10 failed\n");
	FAILED(sbuf[5] != -123, "test_float7 case 11 failed\n");
	FAILED(sbuf[6] != 7190, "test_float7 case 12 failed\n");
	FAILED(sbuf[7] != 312, "test_float7 case 13 failed\n");
	FAILED(sbuf[8] != 3812, "test_float7 case 14 failed\n");
	FAILED(sbuf[9] != -79.0, "test_float7 case 15 failed\n");

	FAILED(wbuf[1] != -367, "test_float7 case 16 failed\n");
	FAILED(wbuf[2] != 917, "test_float7 case 17 failed\n");
	FAILED(wbuf[3] != 476, "test_float7 case 18 failed\n");
	FAILED(wbuf[4] != -476, "test_float7 case 19 failed\n");

	FAILED(ibuf[2] != -917, "test_float7 case 20 failed\n");
	FAILED(ibuf[3] != -1689, "test_float7 case 21 failed\n");

	successful_tests++;
}

static void test_float8(void)
{
	/* Test floating point conversions. */
	executable_code code;
	struct sljit_compiler* compiler;
	int i;
	sljit_f64 dbuf[10];
	sljit_f32 sbuf[9];
	sljit_sw wbuf[9];
	sljit_s32 ibuf[9];
	sljit_s32* dbuf_ptr = (sljit_s32*)dbuf;
	sljit_s32* sbuf_ptr = (sljit_s32*)sbuf;

	if (verbose)
		printf("Run test_float8\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 9; i++) {
		dbuf_ptr[i << 1] = -1;
		dbuf_ptr[(i << 1) + 1] = -1;
		sbuf_ptr[i] = -1;
		wbuf[i] = -1;
		ibuf[i] = -1;
	}

#if IS_64BIT
	dbuf[9] = (sljit_f64)SLJIT_W(0x1122334455);
#endif
	dbuf[0] = 673.75;
	sbuf[0] = -879.75;
	wbuf[0] = 345;
	ibuf[0] = -249;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 3, 3, 3, 0, 0);
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

#if IS_64BIT
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
#endif /* IS_64BIT */

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(dbuf_ptr[(1 * 2) + 0] != -1, "test_float8 case 1 failed\n");
	FAILED(dbuf_ptr[(1 * 2) + 1] != -1, "test_float8 case 2 failed\n");
	FAILED(dbuf[2] != -879.75, "test_float8 case 3 failed\n");
	FAILED(dbuf_ptr[(3 * 2) + 0] != -1, "test_float8 case 4 failed\n");
	FAILED(dbuf_ptr[(3 * 2) + 1] != -1, "test_float8 case 5 failed\n");
	FAILED(dbuf[4] != 345, "test_float8 case 6 failed\n");
	FAILED(dbuf_ptr[(5 * 2) + 0] != -1, "test_float8 case 7 failed\n");
	FAILED(dbuf_ptr[(5 * 2) + 1] != -1, "test_float8 case 8 failed\n");
	FAILED(dbuf[6] != -249, "test_float8 case 9 failed\n");
	FAILED(dbuf_ptr[(7 * 2) + 0] != -1, "test_float8 case 10 failed\n");
	FAILED(dbuf_ptr[(7 * 2) + 1] != -1, "test_float8 case 11 failed\n");

	FAILED(sbuf_ptr[1] != -1, "test_float8 case 12 failed\n");
	FAILED(sbuf[2] != 673.75, "test_float8 case 13 failed\n");
	FAILED(sbuf_ptr[3] != -1, "test_float8 case 14 failed\n");
	FAILED(sbuf[4] != 345, "test_float8 case 15 failed\n");
	FAILED(sbuf_ptr[5] != -1, "test_float8 case 16 failed\n");
	FAILED(sbuf[6] != -249, "test_float8 case 17 failed\n");
	FAILED(sbuf_ptr[7] != -1, "test_float8 case 18 failed\n");

	FAILED(wbuf[1] != -1, "test_float8 case 19 failed\n");
	FAILED(wbuf[2] != 673, "test_float8 case 20 failed\n");
	FAILED(wbuf[3] != -1, "test_float8 case 21 failed\n");
	FAILED(wbuf[4] != -879, "test_float8 case 22 failed\n");
	FAILED(wbuf[5] != -1, "test_float8 case 23 failed\n");

	FAILED(ibuf[1] != -1, "test_float8 case 24 failed\n");
	FAILED(ibuf[2] != 673, "test_float8 case 25 failed\n");
	FAILED(ibuf[3] != -1, "test_float8 case 26 failed\n");
	FAILED(ibuf[4] != -879, "test_float8 case 27 failed\n");
	FAILED(ibuf[5] != -1, "test_float8 case 28 failed\n");

#if IS_64BIT
	FAILED(dbuf[8] != (sljit_f64)SLJIT_W(0x4455667788), "test_float8 case 29 failed\n");
	FAILED(dbuf[9] != (sljit_f64)SLJIT_W(0x66554433), "test_float8 case 30 failed\n");
	FAILED(wbuf[8] != SLJIT_W(0x1122334455), "test_float8 case 31 failed\n");
	FAILED(ibuf[8] == 0x4455, "test_float8 case 32 failed\n");
#endif /* IS_64BIT */

	successful_tests++;
}

static void test_float9(void)
{
	/* Test stack and floating point operations. */
	executable_code code;
	struct sljit_compiler* compiler;
#if !IS_X86
	sljit_uw size1, size2, size3;
	int result;
#endif
	sljit_f32 sbuf[7];

	if (verbose)
		printf("Run test_float9\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sbuf[0] = 245.5;
	sbuf[1] = -100.25;
	sbuf[2] = 713.75;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 3, 3, 6, 0, 8 * sizeof(sljit_f32));

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

#if !IS_X86
	size1 = compiler->size;
#endif
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f32));
#if !IS_X86
	size2 = compiler->size;
#endif
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_FR2, 0);
#if !IS_X86
	size3 = compiler->size;
#endif
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f32), SLJIT_FR5, 0);
#if (defined SLJIT_CONFIG_S390X && SLJIT_CONFIG_S390X)
	result = (compiler->size - size3) == 2 && (size3 - size2) == 1 && (size2 - size1) == 2;
#elif !IS_X86
	result = (compiler->size - size3) == (size3 - size2) && (size3 - size2) == (size2 - size1);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(sbuf[3] != 245.5, "test_float9 case 1 failed\n");
	FAILED(sbuf[4] != 145.25, "test_float9 case 2 failed\n");
	FAILED(sbuf[5] != 5934, "test_float9 case 3 failed\n");
	FAILED(sbuf[6] != 713.75, "test_float9 case 4 failed\n");
#if !IS_X86
	FAILED(!result, "test_float9 case 5 failed\n");
#endif

	successful_tests++;
}

static void test_float10(void)
{
	/* Test all registers provided by the CPU. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_f64 buf[3];
	sljit_s32 i;

	if (verbose)
		printf("Run test_float10\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 6.25;
	buf[1] = 17.75;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_S0), 0);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0V());
	/* SLJIT_FR0 contains the first value. */
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR(i), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 1, 0, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[1]);
	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_R0), 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != (SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS * 17.75 + SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS * 6.25), "test_float10 case 1 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = -32.5;
	buf[1] = -11.25;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_S0), 0);
	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FS(i), 0, SLJIT_MEM1(SLJIT_S0), 0);

	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0V());
	/* SLJIT_FR0 contains the first value. */
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop2(compiler, SLJIT_ADD_F64, SLJIT_FR0, 0, SLJIT_FR0, 0, SLJIT_FR(i), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 1, 0, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf[1]);
	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_MEM1(SLJIT_R0), 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != (SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS * -11.25 + SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS * -32.5), "test_float10 case 2 failed\n");

	successful_tests++;
}

static void test_float11(void)
{
	/* Test float memory accesses with pre/post updates. */
	executable_code code;
	struct sljit_compiler* compiler;
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

	if (verbose)
		printf("Run test_float11\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 4, 3, 4, 0, sizeof(sljit_sw));

	supported[0] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_f64));
	if (supported[0] == SLJIT_SUCCESS) {
		/* dbuf[1] */
		sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 4 * sizeof(sljit_f64));
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F64, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
		/* wbuf[0] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	}

	supported[1] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), -(sljit_sw)sizeof(sljit_f64));
	if (supported[1] == SLJIT_SUCCESS) {
		/* dbuf[2] */
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 0);
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), -(sljit_sw)sizeof(sljit_f64));
		/* wbuf[1] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);
	}

	supported[2] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE, SLJIT_FR1, SLJIT_MEM1(SLJIT_R2), -4 * (sljit_sw)sizeof(sljit_f32));
	if (supported[2] == SLJIT_SUCCESS) {
		/* sbuf[0] */
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S2, 0, SLJIT_IMM, 4 * sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE, SLJIT_FR1, SLJIT_MEM1(SLJIT_R2), -4 * (sljit_sw)sizeof(sljit_f32));
		/* wbuf[2] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	supported[3] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR1, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f32));
	if (supported[3] == SLJIT_SUCCESS) {
		/* sbuf[2] */
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S2, 0, SLJIT_IMM, sizeof(sljit_f32));
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_POST, SLJIT_FR1, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32), SLJIT_FR1, 0);
		/* wbuf[3] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[4] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
	if (supported[4] == SLJIT_SUCCESS) {
		/* dbuf[3] */
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 8 * sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -8 * (sljit_sw)sizeof(sljit_f64));
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F64, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R0), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);
		/* wbuf[4] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R1, 0);
	}

	supported[5] = sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE, SLJIT_FR2, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 0);
	if (supported[5] == SLJIT_SUCCESS) {
		/* sbuf[3] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S2, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 3 * sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_STORE, SLJIT_FR2, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 0);
		/* wbuf[5] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_R2, 0);
	}

	SLJIT_ASSERT(sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_fmem_update(compiler, SLJIT_MOV_F32 | SLJIT_MEM_SUPP | SLJIT_MEM_STORE | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 0) == SLJIT_ERR_UNSUPPORTED);

#if (defined SLJIT_CONFIG_ARM_64 && SLJIT_CONFIG_ARM_64)
	/* TODO: at least for ARM (both V5 and V7) the range below needs further fixing */
	SLJIT_ASSERT(sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 256) == SLJIT_ERR_UNSUPPORTED);
	SLJIT_ASSERT(sljit_emit_fmem_update(compiler, SLJIT_MOV_F64 | SLJIT_MEM_SUPP | SLJIT_MEM_POST, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), -257) == SLJIT_ERR_UNSUPPORTED);
#endif

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&wbuf, (sljit_sw)&dbuf, (sljit_sw)&sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(sizeof(expected) != sizeof(supported) / sizeof(sljit_s32), "test_float11 case 1 failed\n");

	for (i = 0; i < sizeof(expected); i++) {
		if (expected[i]) {
			if (supported[i] != SLJIT_SUCCESS) {
				printf("test_float11 case %d should be supported\n", i + 1);
				return;
			}
		} else {
			if (supported[i] == SLJIT_SUCCESS) {
				printf("test_float11 case %d should not be supported\n", i + 1);
				return;
			}
		}
	}

	FAILED(supported[0] == SLJIT_SUCCESS && dbuf[1] != 66.725, "test_float11 case 2 failed\n");
	FAILED(supported[0] == SLJIT_SUCCESS && wbuf[0] != (sljit_sw)(dbuf), "test_float11 case 3 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && dbuf[2] != 66.725, "test_float11 case 4 failed\n");
	FAILED(supported[1] == SLJIT_SUCCESS && wbuf[1] != (sljit_sw)(dbuf + 1), "test_float11 case 5 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && sbuf[0] != -22.125, "test_float11 case 6 failed\n");
	FAILED(supported[2] == SLJIT_SUCCESS && wbuf[2] != (sljit_sw)(sbuf), "test_float11 case 7 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && sbuf[2] != -22.125, "test_float11 case 8 failed\n");
	FAILED(supported[3] == SLJIT_SUCCESS && wbuf[3] != (sljit_sw)(sbuf + 2), "test_float11 case 9 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && dbuf[3] != 66.725, "test_float11 case 10 failed\n");
	FAILED(supported[4] == SLJIT_SUCCESS && wbuf[4] != (sljit_sw)(dbuf), "test_float11 case 11 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && sbuf[3] != -22.125, "test_float11 case 12 failed\n");
	FAILED(supported[5] == SLJIT_SUCCESS && wbuf[5] != (sljit_sw)(sbuf + 3), "test_float11 case 13 failed\n");

	successful_tests++;
}

static void test_float12(void)
{
	/* Test floating point argument passing to sljit_emit_enter. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw wbuf[2];
	sljit_s32 ibuf[2];
	sljit_f64 dbuf[3];
	sljit_f32 fbuf[2];

	if (verbose)
		printf("Run test_float12\n");

	wbuf[0] = 0;
	ibuf[0] = 0;
	dbuf[0] = 0;
	fbuf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(32, F32, W, F64), 2, 2, 2, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f1(-6834, 674.5f, 2789, -895.25);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 2789, "test_float12 case 1 failed\n");
	FAILED(ibuf[0] != -6834, "test_float12 case 2 failed\n");
	FAILED(dbuf[0] != -895.25, "test_float12 case 3 failed\n");
	FAILED(fbuf[0] != 674.5f, "test_float12 case 4 failed\n");

	ibuf[0] = 0;
	dbuf[0] = 0;
	fbuf[0] = 0;
	fbuf[1] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F32, F64, F32, 32), 1, 1, 3, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&fbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f32), SLJIT_FR2, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f2(-4712.5f, 5342.25, 2904.25f, -4607);
	sljit_free_code(code.code, NULL);

	FAILED(ibuf[0] != -4607, "test_float12 case 5 failed\n");
	FAILED(dbuf[0] != 5342.25, "test_float12 case 6 failed\n");
	FAILED(fbuf[0] != -4712.5f, "test_float12 case 7 failed\n");
	FAILED(fbuf[1] != 2904.25f, "test_float12 case 8 failed\n");

	ibuf[0] = 0;
	dbuf[0] = 0;
	fbuf[0] = 0;
	fbuf[1] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F32, 32, F32), 1, 1, 3, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM0(), (sljit_sw)&dbuf, SLJIT_FR0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&fbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f32), SLJIT_FR2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f3(3578.5, 4619.25f, 6859, -1807.75f);
	sljit_free_code(code.code, NULL);

	FAILED(ibuf[0] != 6859, "test_float12 case 9 failed\n");
	FAILED(dbuf[0] != 3578.5, "test_float12 case 10 failed\n");
	FAILED(fbuf[0] != 4619.25f, "test_float12 case 11 failed\n");
	FAILED(fbuf[1] != -1807.75f, "test_float12 case 12 failed\n");

	ibuf[0] = 0;
	dbuf[0] = 0;
	dbuf[1] = 0;
	fbuf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, 32, F32, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 2, 1, 3, 0, 33);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR1, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f4(2740.75, -2651, -7909.25, 3671.5);
	sljit_free_code(code.code, NULL);

	FAILED(ibuf[0] != -2651, "test_float12 case 13 failed\n");
	FAILED(dbuf[0] != 2740.75, "test_float12 case 14 failed\n");
	FAILED(dbuf[1] != 3671.5, "test_float12 case 15 failed\n");
	FAILED(fbuf[0] != -7909.25, "test_float12 case 16 failed\n");

	wbuf[0] = 0;
	ibuf[0] = 0;
	ibuf[1] = 0;
	fbuf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F32, 32, W, 32), 1, 3, 1, 0, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&ibuf);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_s32), SLJIT_S2, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&fbuf, SLJIT_FR0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f5(-5219.25f, -4530, 7214, 6741);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 7214, "test_float12 case 17 failed\n");
	FAILED(ibuf[0] != -4530, "test_float12 case 18 failed\n");
	FAILED(ibuf[1] != 6741, "test_float12 case 19 failed\n");
	FAILED(fbuf[0] != -5219.25f, "test_float12 case 20 failed\n");

	wbuf[0] = 0;
	wbuf[1] = 0;
	dbuf[0] = 0;
	dbuf[1] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F64, W, W), 1, 5, 2, 0, SLJIT_MAX_LOCAL_SIZE - 1);
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

	code.test_float12_f6(-3749.75, 5280.5, 9134, -6506);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 9134, "test_float12 case 21 failed\n");
	FAILED(wbuf[1] != -6506, "test_float12 case 22 failed\n");
	FAILED(dbuf[0] != -3749.75, "test_float12 case 23 failed\n");
	FAILED(dbuf[1] != 5280.5, "test_float12 case 24 failed\n");

	wbuf[0] = 0;
	dbuf[0] = 0;
	dbuf[1] = 0;
	dbuf[2] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F64, W, F64), 1, 1, 3, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f7(-6049.25, 7301.5, 4610, -4312.75);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 4610, "test_float12 case 25 failed\n");
	FAILED(dbuf[0] != -6049.25, "test_float12 case 26 failed\n");
	FAILED(dbuf[1] != 7301.5, "test_float12 case 27 failed\n");
	FAILED(dbuf[2] != -4312.75, "test_float12 case 28 failed\n");

	ibuf[0] = 0;
	dbuf[0] = 0;
	dbuf[1] = 0;
	dbuf[2] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F64, F64, 32), 1, 1, 3, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM0(), (sljit_sw)&ibuf, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float12_f8(4810.5, -9148.75, 8601.25, 6703);
	sljit_free_code(code.code, NULL);

	FAILED(ibuf[0] != 6703, "test_float12 case 29 failed\n");
	FAILED(dbuf[0] != 4810.5, "test_float12 case 30 failed\n");
	FAILED(dbuf[1] != -9148.75, "test_float12 case 31 failed\n");
	FAILED(dbuf[2] != 8601.25, "test_float12 case 32 failed\n");

	successful_tests++;
}

static void test_float13(void)
{
	/* Test using all fpu registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS];
	sljit_f64 buf2[2];
	struct sljit_jump *jump;
	sljit_s32 i;

	if (verbose)
		printf("Run test_float13\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	buf2[0] = 7.75;
	buf2[1] = -8.25;

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		buf[i] = 0.0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 1, 2, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S1, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS1V(W));

	for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), i * (sljit_sw)sizeof(sljit_f64), SLJIT_FR(i), 0);
	sljit_emit_return_void(compiler);

	/* Called function. */
	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

	sljit_set_context(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)buf, (sljit_sw)buf2);
	sljit_free_code(code.code, NULL);

	for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++) {
		FAILED(buf[i] != -8.25, "test_float13 case 1 failed\n");
	}

	for (i = SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++) {
		FAILED(buf[i] != 7.75, "test_float13 case 2 failed\n");
	}

	/* Next test. */

	if (SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS >= 3) {
		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		buf2[0] = -6.25;
		buf2[1] = 3.75;

		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
			buf[i] = 0.0;

		sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 1, 2, SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2, 1, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FS0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FS0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S1, 0);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS1V(W));

		for (i = 0; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), i * (sljit_sw)sizeof(sljit_f64), SLJIT_FR(i), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), (SLJIT_NUMBER_OF_FLOAT_REGISTERS - 1) * (sljit_sw)sizeof(sljit_f64), SLJIT_FS0, 0);
		sljit_emit_return_void(compiler);

		/* Called function. */
		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, SLJIT_MAX_LOCAL_SIZE);

		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
		for (i = 1; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS; i++)
			sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR(i), 0, SLJIT_FR0, 0);

		sljit_set_context(compiler, 0, SLJIT_ARGS1V(P), 0, 1, SLJIT_NUMBER_OF_FLOAT_REGISTERS, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_return_void(compiler);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		sljit_free_compiler(compiler);

		code.func2((sljit_sw)buf, (sljit_sw)buf2);
		sljit_free_code(code.code, NULL);

		for (i = 0; i < SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i++) {
			FAILED(buf[i] != 3.75, "test_float13 case 3 failed\n");
		}

		for (i = SLJIT_NUMBER_OF_SCRATCH_FLOAT_REGISTERS; i < SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2; i++) {
			FAILED(buf[i] != -6.25, "test_float13 case 4 failed\n");
		}

		FAILED(buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS - 2] != 0, "test_float13 case 5 failed\n");
		FAILED(buf[SLJIT_NUMBER_OF_FLOAT_REGISTERS - 1] != -6.25, "test_float13 case 6 failed\n");
	}

	successful_tests++;
}

static void test_float14(void)
{
	/* Test passing arguments in registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_sw wbuf[2];
	sljit_f64 dbuf[3];

	if (verbose)
		printf("Run test_float14\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F64, F64, W_R), 1, 0, 3, 0, SLJIT_MAX_LOCAL_SIZE);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&wbuf, SLJIT_R0, 0);
	/* dbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float14_f1(7390.25, -8045.5, 1390.75, 8201);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 8201, "test_float14 case 1 failed\n");
	FAILED(dbuf[0] != 7390.25, "test_float14 case 2 failed\n");
	FAILED(dbuf[1] != -8045.5, "test_float14 case 3 failed\n");
	FAILED(dbuf[2] != 1390.75, "test_float14 case 4 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4V(F64, F64, W, W_R), 2, 1, 2, 0, SLJIT_MAX_LOCAL_SIZE);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_R1, 0);
	/* dbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR0, 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR1, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.test_float14_f2(4892.75, -3702.5, 4731, 8530);
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != 4731, "test_float14 case 5 failed\n");
	FAILED(wbuf[1] != 8530, "test_float14 case 6 failed\n");
	FAILED(dbuf[0] != 4892.75, "test_float14 case 7 failed\n");
	FAILED(dbuf[1] != -3702.5, "test_float14 case 8 failed\n");

	successful_tests++;
}

static void test_float15_set(struct sljit_compiler *compiler, sljit_s32 compare, sljit_s32 type, sljit_s32 left_fr, sljit_s32 right_fr)
{
	/* Testing both sljit_emit_op_flags and sljit_emit_jump. */
	struct sljit_jump* jump;

	sljit_emit_fop1(compiler, compare | SLJIT_SET(type & 0xfe), left_fr, 0, right_fr, 0);
	sljit_emit_op_flags(compiler, SLJIT_MOV, SLJIT_R0, 0, type);
	jump = sljit_emit_jump(compiler, type);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2);
	sljit_set_label(jump, sljit_emit_label(compiler));

	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, sizeof(sljit_s8));
}

static void test_float15(void)
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

	if (verbose)
		printf("Run test_float15\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	dbuf[0].u.value1 = 0x7fffffff;
	dbuf[0].u.value2 = 0x7fffffff;
	dbuf[1].value = -13.0;
	dbuf[2].value = 27.0;

	sbuf[0].value1 = 0x7fffffff;
	sbuf[1].value = -13.0;
	sbuf[2].value = 27.0;

	for (i = 0; i < 96; i++)
		bbuf[i] = -3;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 3, 3, 6, 0, 0);

	i = SLJIT_CMP_F64;
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));

	while (1) {
		/* bbuf[0] and bbuf[48] */
		test_float15_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[1] and bbuf[49] */
		test_float15_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[2] and bbuf[50] */
		test_float15_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[3] and bbuf[51] */
		test_float15_set(compiler, i, SLJIT_ORDERED_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[4] and bbuf[52] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[5] and bbuf[53] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[6] and bbuf[54] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[7] and bbuf[55] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_NOT_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[8] and bbuf[56] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[9] and bbuf[57] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[10] and bbuf[58] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[11] and bbuf[59] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[12] and bbuf[60] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[13] and bbuf[61] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[14] and bbuf[62] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[15] and bbuf[63] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[16] and bbuf[64] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[17] and bbuf[65] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[18] and bbuf[66] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[19] and bbuf[67] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[20] and bbuf[68] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[21] and bbuf[69] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[22] and bbuf[70] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[23] and bbuf[71] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[24] and bbuf[72] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[25] and bbuf[73] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[26] and bbuf[74] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[27] and bbuf[75] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[28] and bbuf[76] */
		test_float15_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[29] and bbuf[77] */
		test_float15_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[30] and bbuf[78] */
		test_float15_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[31] and bbuf[79] */
		test_float15_set(compiler, i, SLJIT_ORDERED_NOT_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[32] and bbuf[80] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[33] and bbuf[81] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[34] and bbuf[82] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[35] and bbuf[83] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_LESS, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[36] and bbuf[84] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[37] and bbuf[85] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[38] and bbuf[86] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[39] and bbuf[87] */
		test_float15_set(compiler, i, SLJIT_ORDERED_GREATER_EQUAL, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[40] and bbuf[88] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR2, SLJIT_FR4);
		/* bbuf[41] and bbuf[89] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[42] and bbuf[90] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[43] and bbuf[91] */
		test_float15_set(compiler, i, SLJIT_UNORDERED_OR_GREATER, SLJIT_FR0, SLJIT_FR2);

		/* bbuf[44] and bbuf[92] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR2, SLJIT_FR3);
		/* bbuf[45] and bbuf[93] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR4, SLJIT_FR2);
		/* bbuf[46] and bbuf[94] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR0, SLJIT_FR1);
		/* bbuf[47] and bbuf[95] */
		test_float15_set(compiler, i, SLJIT_ORDERED_LESS_EQUAL, SLJIT_FR0, SLJIT_FR2);

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
	sljit_free_code(code.code, NULL);

	/* SLJIT_CMP_F64 */
	FAILED(bbuf[0] != 1, "test_float15 case 1 failed\n");
	FAILED(bbuf[1] != 2, "test_float15 case 2 failed\n");
	FAILED(bbuf[2] != 2, "test_float15 case 3 failed\n");
	FAILED(bbuf[3] != 2, "test_float15 case 4 failed\n");

	FAILED(bbuf[4] != 2, "test_float15 case 5 failed\n");
	FAILED(bbuf[5] != 1, "test_float15 case 6 failed\n");
	FAILED(bbuf[6] != 1, "test_float15 case 7 failed\n");
	FAILED(bbuf[7] != 1, "test_float15 case 8 failed\n");

	FAILED(bbuf[8] != 2, "test_float15 case 9 failed\n");
	FAILED(bbuf[9] != 1, "test_float15 case 10 failed\n");
	FAILED(bbuf[10] != 2, "test_float15 case 11 failed\n");
	FAILED(bbuf[11] != 2, "test_float15 case 12 failed\n");

	FAILED(bbuf[12] != 2, "test_float15 case 13 failed\n");
	FAILED(bbuf[13] != 1, "test_float15 case 14 failed\n");
	FAILED(bbuf[14] != 1, "test_float15 case 15 failed\n");
	FAILED(bbuf[15] != 1, "test_float15 case 16 failed\n");

	FAILED(bbuf[16] != 2, "test_float15 case 17 failed\n");
	FAILED(bbuf[17] != 1, "test_float15 case 18 failed\n");
	FAILED(bbuf[18] != 2, "test_float15 case 19 failed\n");
	FAILED(bbuf[19] != 2, "test_float15 case 20 failed\n");

	FAILED(bbuf[20] != 1, "test_float15 case 21 failed\n");
	FAILED(bbuf[21] != 2, "test_float15 case 22 failed\n");
	FAILED(bbuf[22] != 1, "test_float15 case 23 failed\n");
	FAILED(bbuf[23] != 1, "test_float15 case 24 failed\n");

	FAILED(bbuf[24] != 2, "test_float15 case 25 failed\n");
	FAILED(bbuf[25] != 1, "test_float15 case 26 failed\n");
	FAILED(bbuf[26] != 1, "test_float15 case 27 failed\n");
	FAILED(bbuf[27] != 1, "test_float15 case 28 failed\n");

	FAILED(bbuf[28] != 2, "test_float15 case 29 failed\n");
	FAILED(bbuf[29] != 1, "test_float15 case 30 failed\n");
	FAILED(bbuf[30] != 2, "test_float15 case 31 failed\n");
	FAILED(bbuf[31] != 2, "test_float15 case 32 failed\n");

	FAILED(bbuf[32] != 1, "test_float15 case 33 failed\n");
	FAILED(bbuf[33] != 2, "test_float15 case 34 failed\n");
	FAILED(bbuf[34] != 1, "test_float15 case 35 failed\n");
	FAILED(bbuf[35] != 1, "test_float15 case 36 failed\n");

	FAILED(bbuf[36] != 2, "test_float15 case 37 failed\n");
	FAILED(bbuf[37] != 1, "test_float15 case 38 failed\n");
	FAILED(bbuf[38] != 2, "test_float15 case 39 failed\n");
	FAILED(bbuf[39] != 2, "test_float15 case 40 failed\n");

	FAILED(bbuf[40] != 2, "test_float15 case 41 failed\n");
	FAILED(bbuf[41] != 1, "test_float15 case 42 failed\n");
	FAILED(bbuf[42] != 1, "test_float15 case 43 failed\n");
	FAILED(bbuf[43] != 1, "test_float15 case 44 failed\n");

	FAILED(bbuf[44] != 1, "test_float15 case 45 failed\n");
	FAILED(bbuf[45] != 2, "test_float15 case 46 failed\n");
	FAILED(bbuf[46] != 2, "test_float15 case 47 failed\n");
	FAILED(bbuf[47] != 2, "test_float15 case 48 failed\n");

	/* SLJIT_CMP_F32 */
	FAILED(bbuf[48] != 1, "test_float15 case 49 failed\n");
	FAILED(bbuf[49] != 2, "test_float15 case 50 failed\n");
	FAILED(bbuf[50] != 2, "test_float15 case 51 failed\n");
	FAILED(bbuf[51] != 2, "test_float15 case 52 failed\n");

	FAILED(bbuf[52] != 2, "test_float15 case 53 failed\n");
	FAILED(bbuf[53] != 1, "test_float15 case 54 failed\n");
	FAILED(bbuf[54] != 1, "test_float15 case 55 failed\n");
	FAILED(bbuf[55] != 1, "test_float15 case 56 failed\n");

	FAILED(bbuf[56] != 2, "test_float15 case 57 failed\n");
	FAILED(bbuf[57] != 1, "test_float15 case 58 failed\n");
	FAILED(bbuf[58] != 2, "test_float15 case 59 failed\n");
	FAILED(bbuf[59] != 2, "test_float15 case 60 failed\n");

	FAILED(bbuf[60] != 2, "test_float15 case 61 failed\n");
	FAILED(bbuf[61] != 1, "test_float15 case 62 failed\n");
	FAILED(bbuf[62] != 1, "test_float15 case 63 failed\n");
	FAILED(bbuf[63] != 1, "test_float15 case 64 failed\n");

	FAILED(bbuf[64] != 2, "test_float15 case 65 failed\n");
	FAILED(bbuf[65] != 1, "test_float15 case 66 failed\n");
	FAILED(bbuf[66] != 2, "test_float15 case 67 failed\n");
	FAILED(bbuf[67] != 2, "test_float15 case 68 failed\n");

	FAILED(bbuf[68] != 1, "test_float15 case 69 failed\n");
	FAILED(bbuf[69] != 2, "test_float15 case 70 failed\n");
	FAILED(bbuf[70] != 1, "test_float15 case 71 failed\n");
	FAILED(bbuf[71] != 1, "test_float15 case 72 failed\n");

	FAILED(bbuf[72] != 2, "test_float15 case 73 failed\n");
	FAILED(bbuf[73] != 1, "test_float15 case 74 failed\n");
	FAILED(bbuf[74] != 1, "test_float15 case 75 failed\n");
	FAILED(bbuf[75] != 1, "test_float15 case 76 failed\n");

	FAILED(bbuf[76] != 2, "test_float15 case 77 failed\n");
	FAILED(bbuf[77] != 1, "test_float15 case 78 failed\n");
	FAILED(bbuf[78] != 2, "test_float15 case 79 failed\n");
	FAILED(bbuf[79] != 2, "test_float15 case 80 failed\n");

	FAILED(bbuf[80] != 1, "test_float15 case 81 failed\n");
	FAILED(bbuf[81] != 2, "test_float15 case 82 failed\n");
	FAILED(bbuf[82] != 1, "test_float15 case 83 failed\n");
	FAILED(bbuf[83] != 1, "test_float15 case 84 failed\n");

	FAILED(bbuf[84] != 2, "test_float15 case 85 failed\n");
	FAILED(bbuf[85] != 1, "test_float15 case 86 failed\n");
	FAILED(bbuf[86] != 2, "test_float15 case 87 failed\n");
	FAILED(bbuf[87] != 2, "test_float15 case 88 failed\n");

	FAILED(bbuf[88] != 2, "test_float15 case 89 failed\n");
	FAILED(bbuf[89] != 1, "test_float15 case 90 failed\n");
	FAILED(bbuf[90] != 1, "test_float15 case 91 failed\n");
	FAILED(bbuf[91] != 1, "test_float15 case 92 failed\n");

	FAILED(bbuf[92] != 1, "test_float15 case 93 failed\n");
	FAILED(bbuf[93] != 2, "test_float15 case 94 failed\n");
	FAILED(bbuf[94] != 2, "test_float15 case 95 failed\n");
	FAILED(bbuf[95] != 2, "test_float15 case 96 failed\n");

	successful_tests++;
}

static void test_float16(void)
{
	/* Test sljit_emit_fcopy. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 dbuf[4];
	sljit_f32 sbuf[2];
#if IS_64BIT
	sljit_sw wbuf[2];
	sljit_s32 ibuf[2];
#else /* !IS_64BIT */
	sljit_s32 ibuf[7];
#endif /* IS_64BIT */

	if (verbose)
		printf("Run test_float16\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sbuf[0] = 12345.0;
	sbuf[1] = -1.0;
	ibuf[0] = -1;
	ibuf[1] = (sljit_s32)0xc7543100;
	dbuf[0] = 123456789012345.0;
	dbuf[1] = -1.0;
#if IS_64BIT
	wbuf[0] = -1;
	wbuf[1] = (sljit_sw)0xc2fee0c29f50cb10;
#else /* !IS_64BIT */
	ibuf[2] = -1;
	ibuf[3] = -1;
	ibuf[4] = -1;
	ibuf[5] = (sljit_sw)0x9f50cb10;
	ibuf[6] = (sljit_sw)0xc2fee0c2;
#endif /* IS_64BIT */

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(W, W), 5, 5, 5, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)ibuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fcopy(compiler, SLJIT_COPY32_FROM_F32, SLJIT_FR2, SLJIT_R0);
	/* ibuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R3, 0, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_s32));
	sljit_emit_fcopy(compiler, SLJIT_COPY32_TO_F32, SLJIT_FR4, SLJIT_R3);
	/* sbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32), SLJIT_FR4, 0);

#if IS_64BIT
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)wbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fcopy(compiler, SLJIT_COPY_FROM_F64, SLJIT_FR1, SLJIT_S2);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_S2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw));
	sljit_emit_fcopy(compiler, SLJIT_COPY_TO_F64, SLJIT_FR0, SLJIT_R3);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_fcopy(compiler, SLJIT_COPY_TO_F64, SLJIT_FR3, SLJIT_R2);
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR3, 0);
#else /* !IS_64BIT */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fcopy(compiler, SLJIT_COPY_FROM_F64, SLJIT_FR1, SLJIT_REG_PAIR(SLJIT_S3, SLJIT_S2));
	/* ibuf[2-3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S3, 0);

	sljit_emit_fcopy(compiler, SLJIT_COPY_FROM_F64, SLJIT_FR1, SLJIT_R2);
	/* ibuf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 4 * sizeof(sljit_sw), SLJIT_R2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_MEM1(SLJIT_R1), 5 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R1), 6 * sizeof(sljit_sw));
	sljit_emit_fcopy(compiler, SLJIT_COPY_TO_F64, SLJIT_FR0, SLJIT_REG_PAIR(SLJIT_R0, SLJIT_R3));
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0);
	sljit_emit_fcopy(compiler, SLJIT_COPY_TO_F64, SLJIT_FR3, SLJIT_REG_PAIR(SLJIT_R2, SLJIT_R2));
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)0xc00c0000);
	sljit_emit_fcopy(compiler, SLJIT_COPY_TO_F64, SLJIT_FR3, SLJIT_R2);
	/* dbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR3, 0);
#endif /* IS_64BIT */

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)dbuf, (sljit_sw)sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(ibuf[0] != (sljit_s32)0x4640e400, "test_float16 case 1 failed\n");
	FAILED(sbuf[1] != -54321.0, "test_float16 case 2 failed\n");
#if IS_64BIT
	FAILED(wbuf[0] != (sljit_sw)0x42dc12218377de40, "test_float16 case 3 failed\n");
	FAILED(dbuf[1] != -543210987654321.0, "test_float16 case 4 failed\n");
	FAILED(dbuf[2] != 0.0, "test_float16 case 5 failed\n");
#else /* !IS_64BIT */
	FAILED(ibuf[2] != (sljit_sw)0x8377de40, "test_float16 case 3 failed\n");
	FAILED(ibuf[3] != (sljit_sw)0x42dc1221, "test_float16 case 4 failed\n");
	FAILED(ibuf[4] != (sljit_sw)0x42dc1221, "test_float16 case 5 failed\n");
	FAILED(dbuf[1] != -543210987654321.0, "test_float16 case 6 failed\n");
	FAILED(dbuf[2] != 0.0, "test_float16 case 7 failed\n");
	FAILED(dbuf[3] != -3.5, "test_float16 case 8 failed\n");
#endif /* IS_64BIT */

	successful_tests++;
}

static void test_float17(void)
{
	/* Test fselect operation. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 dbuf[10];
	sljit_f32 sbuf[10];
	sljit_s32 i;

	if (verbose)
		printf("Run test_float17\n");

	for (i = 4; i < 10; i++)
		dbuf[i] = -1.0;
	for (i = 4; i < 10; i++)
		sbuf[i] = -1.0;

	dbuf[0] = 759.25;
	dbuf[1] = -316.25;
	dbuf[2] = 591.5;
	dbuf[3] = -801.75;

	sbuf[0] = 630.5;
	sbuf[1] = -912.75;
	sbuf[2] = 264.25;
	sbuf[3] = -407.5;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(W, W), 3, 3, 4, 0, 2 * sizeof(sljit_f64));

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op2u(compiler, SLJIT_ADD | SLJIT_SET_CARRY, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_fselect(compiler, SLJIT_CARRY, SLJIT_FR2, SLJIT_FR3, 0, SLJIT_FR2);
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, (sljit_s32)0x80000000);
	sljit_emit_op2u(compiler, SLJIT_ADD32 | SLJIT_SET_OVERFLOW, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_fselect(compiler, SLJIT_OVERFLOW, SLJIT_FR2, SLJIT_FR2, 0, SLJIT_FR3);
	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_UNORDERED, SLJIT_FR2, 0, SLJIT_FR3, 0);
	sljit_emit_fselect(compiler, SLJIT_UNORDERED, SLJIT_FR3, SLJIT_MEM0(), (sljit_sw)(dbuf + 2), SLJIT_FR2);
	/* dbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_FR3, 0);
	sljit_emit_fselect(compiler, SLJIT_ORDERED, SLJIT_FR2, SLJIT_MEM0(), (sljit_sw)(dbuf + 2), SLJIT_FR2);
	/* dbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_CMP_F64 | SLJIT_SET_F_GREATER, SLJIT_FR2, 0, SLJIT_FR2, 0);
	sljit_emit_fselect(compiler, SLJIT_F_LESS_EQUAL, SLJIT_FR0, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f64), SLJIT_FR0);
	/* dbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_fselect(compiler, SLJIT_F_LESS_EQUAL, SLJIT_FR1, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 3, SLJIT_FR1);
	/* dbuf[9] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f64), SLJIT_FR1, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_Z, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_fselect(compiler, SLJIT_EQUAL | SLJIT_32, SLJIT_FR0, SLJIT_FR1, 0, SLJIT_FR2);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR0, 0);
	sljit_emit_fselect(compiler, SLJIT_NOT_EQUAL | SLJIT_32, SLJIT_FR0, SLJIT_FR1, 0, SLJIT_FR2);
	/* sbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_f32), SLJIT_FR0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, WCONST(0x1234000000, 0x123400) + 3 * sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_ORDERED_GREATER, SLJIT_FR1, 0, SLJIT_FR2, 0);
	sljit_emit_fselect(compiler, SLJIT_ORDERED_GREATER | SLJIT_32, SLJIT_FR1, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_FR2);
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_f32), SLJIT_FR1, 0);
	sljit_emit_fselect(compiler, SLJIT_ORDERED_GREATER | SLJIT_32, SLJIT_FR2, SLJIT_MEM1(SLJIT_R1), WCONST(-0x1234000000, -0x123400), SLJIT_FR2);
	/* sbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_f32), SLJIT_FR2, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -100);
	sljit_emit_op2u(compiler, SLJIT_SUB | SLJIT_SET_SIG_LESS, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_fselect(compiler, SLJIT_SIG_LESS | SLJIT_32, SLJIT_FR2, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_FR3);
	/* sbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 8 * sizeof(sljit_f32), SLJIT_FR2, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S2, 0, SLJIT_S1, 0, SLJIT_IMM, -0x5678 + 2 * (sljit_s32)sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_CMP_F32 | SLJIT_SET_ORDERED_EQUAL, SLJIT_FR3, 0, SLJIT_FR3, 0);
	sljit_emit_fselect(compiler, SLJIT_ORDERED_EQUAL | SLJIT_32, SLJIT_FR3, SLJIT_MEM1(SLJIT_S2), 0x5678, SLJIT_FR3);
	/* sbuf[9] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 9 * sizeof(sljit_f32), SLJIT_FR3, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)dbuf, (sljit_sw)sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[4] != -316.25, "test_float17 case 1 failed\n");
	FAILED(dbuf[5] != 759.25, "test_float17 case 2 failed\n");
	FAILED(dbuf[6] != 759.25, "test_float17 case 3 failed\n");
	FAILED(dbuf[7] != 591.5, "test_float17 case 4 failed\n");
	FAILED(dbuf[8] != -801.75, "test_float17 case 5 failed\n");
	FAILED(dbuf[9] != -316.25, "test_float17 case 6 failed\n");
	FAILED(sbuf[4] != 630.5, "test_float17 case 7 failed\n");
	FAILED(sbuf[5] != -912.75, "test_float17 case 8 failed\n");
	FAILED(sbuf[6] != 264.25, "test_float17 case 9 failed\n");
	FAILED(sbuf[7] != -407.5, "test_float17 case 10 failed\n");
	FAILED(sbuf[8] != -912.75, "test_float17 case 11 failed\n");
	FAILED(sbuf[9] != 264.25, "test_float17 case 12 failed\n");

	successful_tests++;
}

static void test_float18(void)
{
	/* Floating point set immediate. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 dbuf[6];
	sljit_f32 sbuf[5];
	sljit_s32 check_buf[2];
	sljit_s32 i;

	if (verbose)
		printf("Run test_float18\n");

	for (i = 0; i < 6; i++)
		dbuf[i] = -1.0;

	for (i = 0; i < 5; i++)
		sbuf[i] = -1.0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 2, 2, 4, 0, 0);

	sljit_emit_fset64(compiler, SLJIT_FR0, 0.0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_FR0, 0);
	sljit_emit_fset64(compiler, SLJIT_FR1, -0.0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR1, 0);
	sljit_emit_fset64(compiler, SLJIT_FR2, 1.0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR2, 0);
	sljit_emit_fset64(compiler, SLJIT_FR2, -31.0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR2, 0);
	sljit_emit_fset64(compiler, SLJIT_FR2, 545357837627392.0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR2, 0);
	sljit_emit_fset64(compiler, SLJIT_FR0, 983752153845214.5);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fset32(compiler, SLJIT_FR0, 0.0f);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_FR0, 0);
	sljit_emit_fset32(compiler, SLJIT_FR1, -0.0f);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32), SLJIT_FR1, 0);
	sljit_emit_fset32(compiler, SLJIT_FR2, 1.0f);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_FR2, 0);
	sljit_emit_fset32(compiler, SLJIT_FR2, 31.0f);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f32), SLJIT_FR2, 0);
	sljit_emit_fset32(compiler, SLJIT_FR2, -811.5f);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&dbuf, (sljit_sw)&sbuf);
	sljit_free_code(code.code, NULL);

	copy_u8(check_buf, 0, dbuf + 0, sizeof(sljit_f64));
	FAILED(check_buf[0] != 0, "test_float18 case 1 failed\n");
	FAILED(check_buf[1] != 0, "test_float18 case 2 failed\n");
	copy_u8(check_buf, 0, dbuf + 1, sizeof(sljit_f64));
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	FAILED(check_buf[0] != 0, "test_float18 case 3 failed\n");
	FAILED(check_buf[1] != (sljit_s32)0x80000000, "test_float18 case 4 failed\n");
#else /* !SLJIT_LITTLE_ENDIAN */
	FAILED(check_buf[1] != 0, "test_float18 case 3 failed\n");
	FAILED(check_buf[0] != (sljit_s32)0x80000000, "test_float18 case 4 failed\n");
#endif /* SLJIT_LITTLE_ENDIAN */
	FAILED(dbuf[2] != 1.0, "test_float18 case 5 failed\n");
	FAILED(dbuf[3] != -31.0, "test_float18 case 6 failed\n");
	FAILED(dbuf[4] != 545357837627392.0, "test_float18 case 7 failed\n");
	FAILED(dbuf[5] != 983752153845214.5, "test_float18 case 8 failed\n");

	copy_u8(check_buf, 0, sbuf + 0, sizeof(sljit_f32));
	FAILED(check_buf[0] != 0, "test_float18 case 9 failed\n");
	copy_u8(check_buf, 0, sbuf + 1, sizeof(sljit_f32));
	FAILED(check_buf[0] != (sljit_s32)0x80000000, "test_float18 case 10 failed\n");
	FAILED(sbuf[2] != 1.0, "test_float18 case 11 failed\n");
	FAILED(sbuf[3] != 31.0, "test_float18 case 12 failed\n");
	FAILED(sbuf[4] != -811.5, "test_float18 case 13 failed\n");

	successful_tests++;
}

static void test_float19(void)
{
	/* Floating point convert from unsigned. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f64 dbuf[9];
	sljit_f32 sbuf[9];
	sljit_s32 i;
	sljit_sw value1 = WCONST(0xfffffffffffff800, 0xffffff00);
	sljit_sw value2 = WCONST(0x8000000000000801, 0x80000101);

	union {
		sljit_f64 value;
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
		struct {
			sljit_u32 low;
			sljit_u32 high;
		} bin;
#else /* !SLJIT_LITTLE_ENDIAN */
		struct {
			sljit_u32 high;
			sljit_u32 low;
		} bin;
#endif /* SLJIT_LITTLE_ENDIAN */
	} f64_check;

	union {
		sljit_f32 value;
		sljit_u32 bin;
	} f32_check;

	if (verbose)
		printf("Run test_float19\n");

	for (i = 0; i < 9; i++)
		dbuf[i] = -1.0;

	for (i = 0; i < 9; i++)
		sbuf[i] = -1.0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 4, 4, 4, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 0x7fffffff);
	/* dbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_U32, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R1, 0);
	/* sbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_U32, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_R1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_U32, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 3, SLJIT_IMM, (sljit_sw)0xfff00000);
	/* sbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_U32, SLJIT_MEM2(SLJIT_S1, SLJIT_R0), 2, SLJIT_IMM, (sljit_sw)0xfff00000);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)0xffffff80);
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_U32, SLJIT_FR1, 0, SLJIT_R0, 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_U32, SLJIT_FR3, 0, SLJIT_R0, 0);
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64), SLJIT_FR1, 0);
	/* sbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32), SLJIT_FR3, 0);

	/* dbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_IMM, (sljit_sw)0xffffff00);
	/* sbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f32), SLJIT_IMM, (sljit_sw)0xffffff00);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, WCONST(0x7fff000000000000, 0x7fff0000));
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_R3, 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_R3, 0);

	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_FR2, 0, SLJIT_MEM0(), (sljit_sw)&value1);
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_FR1, 0, SLJIT_MEM0(), (sljit_sw)&value1);
	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR2, 0);
	/* sbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_f32), SLJIT_FR1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, WCONST(0xaaaaaaaaaaaaaaaa, 0xaaaaaaaa));
	/* dbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_S3, 0);
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_f32), SLJIT_S3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)&value2 + 64);
	/* dbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f64), SLJIT_MEM1(SLJIT_R2), -64);
	/* sbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_f32), SLJIT_MEM1(SLJIT_R2), -64);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, WCONST(0x8000000000000401, 0x80000001));
	/* dbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F64_FROM_UW, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f64), SLJIT_R2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, WCONST(0x8000008000000001, 0x80000081));
	/* sbuf[8] */
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_UW, SLJIT_MEM1(SLJIT_S1), 8 * sizeof(sljit_f32), SLJIT_R2, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&dbuf, (sljit_sw)&sbuf);

	f64_check.value = dbuf[0]; /* 0x7fffffff */
	FAILED(f64_check.bin.low != 0xffc00000 || f64_check.bin.high != 0x41dfffff, "test_float19 case 1 failed\n");
	f32_check.value = sbuf[0]; /* 0x7fffffff */
	FAILED(f32_check.bin != 0x4f000000, "test_float19 case 2 failed\n");
	f64_check.value = dbuf[1]; /* 0xfff00000 */
	FAILED(f64_check.bin.low != 0 || f64_check.bin.high != 0x41effe00, "test_float19 case 3 failed\n");
	f32_check.value = sbuf[1]; /* 0xfff00000 */
	FAILED(f32_check.bin != 0x4f7ff000, "test_float19 case 4 failed\n");
	f64_check.value = dbuf[2]; /* 0xffffff80 */
	FAILED(f64_check.bin.low != 0xf0000000 || f64_check.bin.high != 0x41efffff, "test_float19 case 5 failed\n");
	f32_check.value = sbuf[2]; /* 0xffffff80 */
	FAILED(f32_check.bin != 0x4f800000, "test_float19 case 6 failed\n");
	f64_check.value = dbuf[3]; /* 0xffffff00 */
	FAILED(f64_check.bin.low != 0xe0000000 || f64_check.bin.high != 0x41efffff, "test_float19 case 7 failed\n");
	f32_check.value = sbuf[3]; /* 0xffffff00 */
	FAILED(f32_check.bin != 0x4f7fffff, "test_float19 case 8 failed\n");
#if IS_64BIT
	f64_check.value = dbuf[4]; /* 0x7fff000000000000 */
	FAILED(f64_check.bin.low != 0 || f64_check.bin.high != 0x43dfffc0, "test_float19 case 9 failed\n");
	f32_check.value = sbuf[4]; /* 0x7fff000000000000 */
	FAILED(f32_check.bin != 0x5efffe00, "test_float19 case 10 failed\n");
	f64_check.value = dbuf[5]; /* 0xfffffffffffff800 */
	FAILED(f64_check.bin.low != 0xffffffff || f64_check.bin.high != 0x43efffff, "test_float19 case 11 failed\n");
	f32_check.value = sbuf[5]; /* 0xfffffffffffff800 */
	FAILED(f32_check.bin != 0x5f800000, "test_float19 case 12 failed\n");
	f64_check.value = dbuf[6]; /* 0xffff000000000000 */
	FAILED(f64_check.bin.low != 0x55555555 || f64_check.bin.high != 0x43e55555, "test_float19 case 13 failed\n");
	f32_check.value = sbuf[6]; /* 0xffff000000000000 */
	FAILED(f32_check.bin != 0x5f2aaaab, "test_float19 case 14 failed\n");
	f64_check.value = dbuf[7]; /* 0x8000000000000801 */
	FAILED(f64_check.bin.low != 1 || f64_check.bin.high != 0x43e00000, "test_float19 case 15 failed\n");
	f32_check.value = sbuf[7]; /* 0x8000000000000801 */
	FAILED(f32_check.bin != 0x5f000000, "test_float19 case 16 failed\n");
	f64_check.value = dbuf[8]; /* 0x8000000000000401 */
	FAILED(f64_check.bin.low != 1 || f64_check.bin.high != 0x43e00000, "test_float19 case 17 failed\n");
	f32_check.value = sbuf[8]; /* 0x8000008000000001 */
	FAILED(f32_check.bin != 0x5f000001, "test_float19 case 18 failed\n");
#else /* !IS_64BIT */
	f64_check.value = dbuf[4]; /* 0x7fff0000 */
	FAILED(f64_check.bin.low != 0 || f64_check.bin.high != 0x41dfffc0, "test_float19 case 9 failed\n");
	f32_check.value = sbuf[4]; /* 0x7fff0000 */
	FAILED(f32_check.bin != 0x4efffe00, "test_float19 case 10 failed\n");
	f64_check.value = dbuf[5]; /* 0xffffff00 */
	FAILED(f64_check.bin.low != 0xe0000000 || f64_check.bin.high != 0x41efffff, "test_float19 case 11 failed\n");
	f32_check.value = sbuf[5]; /* 0xffffff00 */
	FAILED(f32_check.bin != 0x4f7fffff, "test_float19 case 12 failed\n");
	f64_check.value = dbuf[6]; /* 0xaaaaaaaa */
	FAILED(f64_check.bin.low != 0x55400000 || f64_check.bin.high != 0x41e55555, "test_float19 case 13 failed\n");
	f32_check.value = sbuf[6]; /* 0xaaaaaaaa */
	FAILED(f32_check.bin != 0x4f2aaaab, "test_float19 case 14 failed\n");
	f64_check.value = dbuf[7]; /* 0x80000101 */
	FAILED(f64_check.bin.low != 0x20200000 || f64_check.bin.high != 0x41e00000, "test_float19 case 15 failed\n");
	f32_check.value = sbuf[7]; /* 0x80000101 */
	FAILED(f32_check.bin != 0x4f000001, "test_float19 case 16 failed\n");
	f64_check.value = dbuf[8]; /* 0x80000001 */
	FAILED(f64_check.bin.low != 0x00200000 || f64_check.bin.high != 0x41e00000, "test_float19 case 17 failed\n");
	f32_check.value = sbuf[8]; /* 0x80000081 */
	FAILED(f32_check.bin != 0x4f000001, "test_float19 case 18 failed\n");
#endif /* IS_64BIT */

	successful_tests++;
}

static void test_float20(void)
{
	/* Test fpu copysign. */
	executable_code code;
	struct sljit_compiler* compiler;
	int i;

	union {
		sljit_f64 value;
		struct {
#if defined(SLJIT_LITTLE_ENDIAN) && SLJIT_LITTLE_ENDIAN
			sljit_u32 lo;
			sljit_u32 hi;
#else /* !SLJIT_LITTLE_ENDIAN */
			sljit_u32 hi;
			sljit_u32 lo;
#endif /* SLJIT_LITTLE_ENDIAN */
		} bits;
	} dbuf[8];
	union {
		sljit_f32 value;
		sljit_u32 bits;
	} sbuf[8];

	if (verbose)
		printf("Run test_float20\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 8; i++)
		dbuf[i].value = 123.0;

	for (i = 0; i < 8; i++)
		sbuf[i].value = 123.0f;

	dbuf[0].value = 1786.5;
	dbuf[1].value = -8403.25;
	dbuf[2].bits.lo = 0;
	dbuf[2].bits.hi = 0x7fff0000;
	dbuf[3].value = 9054;

	sbuf[0].value = 6371.75f;
	sbuf[1].value = -2713.5f;
	sbuf[2].bits = 0xfff00000;
	sbuf[3].value = -5791.25f;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 2, 2, 6, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64));
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F64, SLJIT_FR0, SLJIT_FR0, 0, SLJIT_FR1, 0);
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F32, SLJIT_FR3, SLJIT_FR4, 0, SLJIT_FR3, 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 8 * sizeof(sljit_f64));
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F64, SLJIT_FR2, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0, SLJIT_MEM1(SLJIT_R1), -7 * (sljit_sw)sizeof(sljit_f64));
	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F32, SLJIT_FR5, SLJIT_MEM2(SLJIT_S1, SLJIT_R1), 2, SLJIT_FR4, 0);
	/* sbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_f32), SLJIT_FR5, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64));
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F64, SLJIT_FR0, SLJIT_FR3, 0, SLJIT_FR2, 0);
	/* dbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_f32));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S1, 0, SLJIT_IMM, 0x12345);
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F32, SLJIT_FR2, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R0), -0x12345 + (sljit_sw)sizeof(sljit_f32));
	/* sbuf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_f32), SLJIT_FR2, 0);

	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F64, SLJIT_FR5, SLJIT_MEM0(), (sljit_sw)(dbuf + 1), SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	/* dbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f64), SLJIT_FR5, 0);

	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F32, SLJIT_FR4, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_MEM0(), (sljit_sw)(sbuf + 2));
	/* sbuf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_f32), SLJIT_FR4, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&dbuf, (sljit_sw)&sbuf);
	FAILED(dbuf[4].value != -1786.5, "test_float20 case 1 failed\n");
	FAILED(sbuf[4].value != 2713.5, "test_float20 case 2 failed\n");
	FAILED(dbuf[5].bits.lo != 0, "test_float20 case 3 failed\n");
	FAILED(dbuf[5].bits.hi != 0xffff0000, "test_float20 case 4 failed\n");
	FAILED(sbuf[5].bits != 0x7ff00000, "test_float20 case 5 failed\n");
	FAILED(dbuf[6].value != 9054, "test_float20 case 6 failed\n");
	FAILED(sbuf[6].value != -5791.25, "test_float20 case 7 failed\n");
	FAILED(dbuf[7].value != 8403.25, "test_float20 case 8 failed\n");
	FAILED(sbuf[7].value != -sbuf[0].value, "test_float20 case 9 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test_float21(void)
{
	/* Test f64 as f32 register pair access. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_f32 buf[10];
	sljit_sw num;
	sljit_s32 i;

	if (verbose)
		printf("Run test_float21\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_F64_AS_F32_PAIR)) {
		if (verbose)
			printf("f32 register pairs are not available, test_float21 skipped\n");
		successful_tests++;
		return;
	}

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	buf[0] = -45.25;
	buf[1] = 33.5;
	buf[2] = -104.75;

	for (i = 3; i < 10; i++)
		buf[i] = -1.0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 3, 2, 4, 2, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FR0), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));

	sljit_emit_fop1(compiler, SLJIT_NEG_F32, SLJIT_F64_SECOND(SLJIT_FR0), 0, SLJIT_F64_SECOND(SLJIT_FR0), 0);
	/* buf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FR0), 0);

	sljit_emit_fop1(compiler, SLJIT_ABS_F32, SLJIT_F64_SECOND(SLJIT_FR0), 0, SLJIT_FR0, 0);
	/* buf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FR0), 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 89);
	sljit_emit_fop1(compiler, SLJIT_CONV_F32_FROM_SW, SLJIT_F64_SECOND(SLJIT_FR1), 0, SLJIT_R0, 0);
	/* buf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FR1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FR1), 0, SLJIT_FR1, 0);
	/* num */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F32, SLJIT_MEM0(), (sljit_sw)&num, SLJIT_F64_SECOND(SLJIT_FR1), 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FS1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FS1), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop2(compiler, SLJIT_ADD_F32, SLJIT_F64_SECOND(SLJIT_FS1), 0, SLJIT_FS1, 0, SLJIT_F64_SECOND(SLJIT_FS1), 0);
	/* buf[6] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FS1), 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FR1), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop2r(compiler, SLJIT_COPYSIGN_F32, SLJIT_F64_SECOND(SLJIT_FR1), SLJIT_FR1, 0, SLJIT_F64_SECOND(SLJIT_FR1), 0);
	/* buf[7] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FR1), 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FS0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fset32(compiler, SLJIT_F64_SECOND(SLJIT_FS0), -78.75f);
	/* buf[8] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FS0), 0);

	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_S1, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_F64_SECOND(SLJIT_FR3), 0, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f32));
	sljit_emit_fcopy(compiler, SLJIT_COPY32_TO_F32, SLJIT_F64_SECOND(SLJIT_FR3), SLJIT_S1);
	/* buf[9] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S0), 9 * sizeof(sljit_f32), SLJIT_F64_SECOND(SLJIT_FR3), 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[3] != -33.5, "test_float21 case 1 failed\n");
	FAILED(buf[4] != 45.25, "test_float21 case 2 failed\n");
	FAILED(buf[5] != 89.0, "test_float21 case 3 failed\n");
	FAILED(num != -104, "test_float21 case 4 failed\n");
	FAILED(buf[6] != -11.75, "test_float21 case 5 failed\n");
	FAILED(buf[7] != -33.5, "test_float21 case 6 failed\n");
	FAILED(buf[8] != -78.75, "test_float21 case 7 failed\n");
	FAILED(buf[9] != -45.25, "test_float21 case 8 failed\n");

	successful_tests++;
}

static void test_float22(void)
{
	/* Test float to int conversion corner cases. */
	executable_code code;
	struct sljit_compiler *compiler;
	struct sljit_label *label;
	int i;

	union {
		sljit_f64 value_f64;
		sljit_uw value_uw;
		sljit_u32 value_u32;

		struct {
#if defined(SLJIT_LITTLE_ENDIAN) && SLJIT_LITTLE_ENDIAN
			sljit_u32 lo;
			sljit_u32 hi;
#else /* !SLJIT_LITTLE_ENDIAN */
			sljit_u32 hi;
			sljit_u32 lo;
#endif /* SLJIT_LITTLE_ENDIAN */
		} bits;
	} dbuf[32];
	union {
		sljit_f32 value_f32;
		sljit_u32 bits;
	} sbuf[6];

	const sljit_uw min_uw = (sljit_uw)1 << ((sizeof(sljit_uw) * 8) - 1);
	const sljit_u32 min_u32 = (sljit_u32)1 << 31;

#if SLJIT_CONV_MAX_FLOAT == SLJIT_CONV_RESULT_MIN_INT
	const sljit_uw large_pos_uw = min_uw;
	const sljit_u32 large_pos_u32 = min_u32;
#else
	const sljit_uw large_pos_uw = min_uw - 1;
	const sljit_u32 large_pos_u32 = min_u32 - 1;
#endif

#if SLJIT_CONV_MIN_FLOAT == SLJIT_CONV_RESULT_MIN_INT
	const sljit_uw large_neg_uw = min_uw;
	const sljit_u32 large_neg_u32 = min_u32;
#else
	const sljit_uw large_neg_uw = min_uw - 1;
	const sljit_u32 large_neg_u32 = min_u32 - 1;
#endif

#if SLJIT_CONV_NAN_FLOAT == SLJIT_CONV_RESULT_MIN_INT
	const sljit_uw nan_uw = min_uw;
	const sljit_u32 nan_u32 = min_u32;
#elif SLJIT_CONV_NAN_FLOAT == SLJIT_CONV_RESULT_MAX_INT
	const sljit_uw nan_uw = min_uw - 1;
	const sljit_u32 nan_u32 = min_u32 - 1;
#else
	const sljit_uw nan_uw = 0;
	const sljit_u32 nan_u32 = 0;
#endif

	if (verbose)
		printf("Run test_float22\n");

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	for (i = 0; i < 31; i++)
		dbuf[i].value_f64 = 123.0;

	/* Large positive integer */
	dbuf[0].bits.hi = (sljit_u32)0x7fe << 20;
	dbuf[0].bits.lo = 0;
	/* Large negative integer */
	dbuf[1].bits.hi = (sljit_u32)0xffe << 20;
	dbuf[1].bits.lo = 0;
	/* Positive infinity */
	dbuf[2].bits.hi = (sljit_u32)0x7ff << 20;
	dbuf[2].bits.lo = 0;
	/* Negative infinity */
	dbuf[3].bits.hi = (sljit_u32)0xfff << 20;
	dbuf[3].bits.lo = 0;
	/* Canonical NaN */
	dbuf[4].bits.hi = (sljit_u32)0xfff << 19;
	dbuf[4].bits.lo = 0;
	/* NaN */
	dbuf[5].bits.hi = (sljit_u32)0xfff << 20;
	dbuf[5].bits.lo = 1;

	/* Large positive integer */
	sbuf[0].bits = (sljit_u32)0x7f000000;
	/* Large negative integer */
	sbuf[1].bits = (sljit_u32)0xff000000;
	/* Positive infinity */
	sbuf[2].bits = (sljit_u32)0x7f800000;
	/* Negative infinity */
	sbuf[3].bits = (sljit_u32)0xff800000;
	/* Canonical NaN */
	sbuf[4].bits = (sljit_u32)0x7fc00000;
	/* NaN */
	sbuf[5].bits = (sljit_u32)0x7f800001;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, P), 2, 2, 2, 0, 0);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 6 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_R0, 0);

	label = sljit_emit_label(compiler);
	/* dbuf[6 - 17] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S0, 0, SLJIT_S0, 0, SLJIT_IMM, sizeof(sljit_f64));
	sljit_set_label(sljit_emit_cmp(compiler, SLJIT_LESS, SLJIT_S0, 0, SLJIT_R1, 0), label);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S1, 0, SLJIT_IMM, 6 * sizeof(sljit_f32));

	label = sljit_emit_label(compiler);
	/* dbuf[18 - 29] */
	sljit_emit_fop1(compiler, SLJIT_CONV_SW_FROM_F32, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_CONV_S32_FROM_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_R0, 0, SLJIT_IMM, 2 * sizeof(sljit_f64));
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S1, 0, SLJIT_S1, 0, SLJIT_IMM, sizeof(sljit_f32));
	sljit_set_label(sljit_emit_cmp(compiler, SLJIT_LESS, SLJIT_S1, 0, SLJIT_R1, 0), label);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&dbuf, (sljit_sw)&sbuf);
	sljit_free_code(code.code, NULL);

	/* Large integer */
	FAILED(dbuf[6].value_uw != large_pos_uw, "test_float22 case 1 failed\n");
	FAILED(dbuf[7].value_u32 != large_pos_u32, "test_float22 case 2 failed\n");
	FAILED(dbuf[8].value_uw != large_neg_uw, "test_float22 case 3 failed\n");
	FAILED(dbuf[9].value_u32 != large_neg_u32, "test_float22 case 4 failed\n");
	/* Infinity */
	FAILED(dbuf[10].value_uw != large_pos_uw, "test_float22 case 5 failed\n");
	FAILED(dbuf[11].value_u32 != large_pos_u32, "test_float22 case 6 failed\n");
	FAILED(dbuf[12].value_uw != large_neg_uw, "test_float22 case 7 failed\n");
	FAILED(dbuf[13].value_u32 != large_neg_u32, "test_float22 case 8 failed\n");
	/* NaN */
	FAILED(dbuf[14].value_uw != nan_uw, "test_float22 case 9 failed\n");
	FAILED(dbuf[15].value_u32 != nan_u32, "test_float22 case 10 failed\n");
	FAILED(dbuf[16].value_uw != nan_uw, "test_float22 case 11 failed\n");
	FAILED(dbuf[17].value_u32 != nan_u32, "test_float22 case 12 failed\n");

	/* Large integer */
	FAILED(dbuf[18].value_uw != large_pos_uw, "test_float22 case 13 failed\n");
	FAILED(dbuf[19].value_u32 != large_pos_u32, "test_float22 case 14 failed\n");
	FAILED(dbuf[20].value_uw != large_neg_uw, "test_float22 case 15 failed\n");
	FAILED(dbuf[21].value_u32 != large_neg_u32, "test_float22 case 16 failed\n");
	/* Infinity */
	FAILED(dbuf[22].value_uw != large_pos_uw, "test_float22 case 17 failed\n");
	FAILED(dbuf[23].value_u32 != large_pos_u32, "test_float22 case 18 failed\n");
	FAILED(dbuf[24].value_uw != large_neg_uw, "test_float22 case 19 failed\n");
	FAILED(dbuf[25].value_u32 != large_neg_u32, "test_float22 case 20 failed\n");
	/* NaN */
	FAILED(dbuf[26].value_uw != nan_uw, "test_float22 case 21 failed\n");
	FAILED(dbuf[27].value_u32 != nan_u32, "test_float22 case 22 failed\n");
	FAILED(dbuf[28].value_uw != nan_uw, "test_float22 case 23 failed\n");
	FAILED(dbuf[29].value_u32 != nan_u32, "test_float22 case 24 failed\n");

	FAILED(dbuf[30].value_f64 != 123.0, "test_float22 case 25 failed\n");

	successful_tests++;
}
