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

static sljit_sw func(sljit_sw a, sljit_sw b, sljit_sw c)
{
	return a + b + c + 5;
}

static sljit_sw func4(sljit_sw a, sljit_sw b, sljit_sw c, sljit_sw d)
{
	return func(a, b, c) + d;
}

static void test_call1(void)
{
	/* Test function call. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_sw buf[9];
	sljit_sw res;

	if (verbose)
		printf("Run test_call1\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;
	buf[3] = 0;
	buf[4] = 0;
	buf[5] = 0;
	buf[6] = 0;
	buf[7] = 0;
	buf[8] = SLJIT_FUNC_ADDR(func);

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(W, P), 4, 2, 0, 0, 0);

	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 7);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);

	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -5);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_REWRITABLE_JUMP, SLJIT_ARGS3(W, W, W, W));
	sljit_set_target(jump, (sljit_uw)-1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 40);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -60);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -30);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 100);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 110);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 120);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_R3, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 3);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[7] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 3);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -6);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(func4));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, W, W, W), SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	/* buf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -10);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 6);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(W, W, W, W), SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_RETURN_REG, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_set_jump_addr(sljit_get_jump_addr(jump), SLJIT_FUNC_UADDR(func), sljit_get_executable_offset(compiler));
	sljit_free_compiler(compiler);

	res = code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(res != -15, "test_call1 case 1 failed\n");
	FAILED(buf[0] != 14, "test_call1 case 2 failed\n");
	FAILED(buf[1] != -8, "test_call1 case 3 failed\n");
	FAILED(buf[2] != SLJIT_FUNC_ADDR(func) + 42, "test_call1 case 4 failed\n");
	FAILED(buf[3] != SLJIT_FUNC_ADDR(func) - 85, "test_call1 case 5 failed\n");
	FAILED(buf[4] != SLJIT_FUNC_ADDR(func) + 31, "test_call1 case 6 failed\n");
	FAILED(buf[5] != 335, "test_call1 case 7 failed\n");
	FAILED(buf[6] != 11, "test_call1 case 8 failed\n");
	FAILED(buf[7] != 5, "test_call1 case 9 failed\n");
	FAILED(buf[8] != -15, "test_call1 case 10 failed\n");

	successful_tests++;
}

static void test_call2(void)
{
	/* Ackermann benchmark. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *entry;
	struct sljit_label *label;
	struct sljit_jump *jump;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;
	sljit_sw res;

	if (verbose)
		printf("Run test_call2\n");

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

	/* For benchmarking. */
	/* FAILED(code.func2(3, 11) != 16381, "test_call2 case 1 failed\n"); */

	res = code.func2(3, 3);
	sljit_free_code(code.code, NULL);

	FAILED(res != 61, "test_call2 case 1 failed\n");

	successful_tests++;
}

static sljit_f64 test_call3_f1(sljit_f32 a, sljit_f32 b, sljit_f64 c)
{
	return (sljit_f64)a + (sljit_f64)b + c;
}

static sljit_f32 test_call3_f2(sljit_sw a, sljit_f64 b, sljit_f32 c)
{
	return (sljit_f32)((sljit_f64)a + b + (sljit_f64)c);
}

static sljit_f64 test_call3_f3(sljit_sw a, sljit_f32 b, sljit_sw c)
{
	return (sljit_f64)a + (sljit_f64)b + (sljit_f64)c;
}

static sljit_f64 test_call3_f4(sljit_f32 a, sljit_sw b)
{
	return (sljit_f64)a + (sljit_f64)b;
}

static sljit_f32 test_call3_f5(sljit_f32 a, sljit_f64 b, sljit_s32 c)
{
	return (sljit_f32)((sljit_f64)a + b + (sljit_f64)c);
}

static sljit_sw test_call3_f6(sljit_f64 a, sljit_sw b)
{
	return (sljit_sw)(a + (sljit_f64)b);
}

static void test_call3(void)
{
	/* Check function calls with floating point arguments. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump = NULL;
	sljit_f64 dbuf[7];
	sljit_f32 sbuf[7];
	sljit_sw wbuf[2];

	if (verbose)
		printf("Run test_call3\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test_call3 skipped\n");
		successful_tests++;
		return;
	}

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	dbuf[0] = 5.25;
	dbuf[1] = 0.0;
	dbuf[2] = 2.5;
	dbuf[3] = 0.0;
	dbuf[4] = 0.0;
	dbuf[5] = 0.0;
	dbuf[6] = -18.0;

	sbuf[0] = 6.75f;
	sbuf[1] = -3.5f;
	sbuf[2] = 1.5f;
	sbuf[3] = 0.0f;
	sbuf[4] = 0.0f;

	wbuf[0] = 0;
	wbuf[1] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 3, 3, 4, 0, sizeof(sljit_sw));

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(F64, F32, F32, F64), SLJIT_IMM, SLJIT_FUNC_ADDR(test_call3_f1));
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3(F64, F32, F32, F64));
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call3_f1));
	/* dbuf[3] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call3_f2));
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
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call3_f3));
	/* dbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -6);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(F64, F32, W));
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call3_f4));
	/* dbuf[5] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S0), 5 * sizeof(sljit_f64), SLJIT_FR0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call3_f5));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS3(F32, F32, F64, 32), SLJIT_MEM1(SLJIT_SP), 0);
	/* sbuf[4] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_f32), SLJIT_FR0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call3_f6));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(W, F64, W), SLJIT_R0, 0);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 0, SLJIT_R0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_f64));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 319);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call3_f6));
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS2(W, F64, W), SLJIT_R1, 0);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)&dbuf, (sljit_sw)&sbuf, (sljit_sw)&wbuf);
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[1] != 8.5, "test_call3 case 1 failed\n");
	FAILED(dbuf[3] != 0.5, "test_call3 case 2 failed\n");
	FAILED(sbuf[3] != 17.75, "test_call3 case 3 failed\n");
	FAILED(dbuf[4] != 11.75, "test_call3 case 4 failed\n");
	FAILED(dbuf[5] != -9.5, "test_call3 case 5 failed\n");
	FAILED(sbuf[4] != 12, "test_call3 case 6 failed\n");
	FAILED(wbuf[0] != SLJIT_FUNC_ADDR(test_call3_f6) - 18, "test_call3 case 7 failed\n");
	FAILED(wbuf[1] != 301, "test_call3 case 8 failed\n");

	successful_tests++;
}

static sljit_sw test_call4_f1(sljit_sw a, sljit_s32 b, sljit_sw c, sljit_sw d)
{
	return (sljit_sw)(a + b + c + d - SLJIT_FUNC_ADDR(test_call4_f1));
}

static sljit_s32 test_call4_f2(sljit_f64 a, sljit_f32 b, sljit_f64 c, sljit_sw d)
{
	return (sljit_s32)(a + b + c + (sljit_f64)d);
}

static sljit_f32 test_call4_f3(sljit_f32 a, sljit_s32 b, sljit_f64 c, sljit_sw d)
{
	return (sljit_f32)(a + (sljit_f64)b + c + (sljit_f64)d);
}

static sljit_f32 test_call4_f4(sljit_f32 a, sljit_f64 b, sljit_f32 c, sljit_f64 d)
{
	return (sljit_f32)(a + b + c + (sljit_f64)d);
}

static void test_call4(void)
{
	/* Check function calls with four arguments. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_jump* jump = NULL;
	sljit_sw wbuf[5];
	sljit_f64 dbuf[3];
	sljit_f32 sbuf[4];

	if (verbose)
		printf("Run test_call4\n");

	wbuf[0] = 0;
	wbuf[1] = 0;
	wbuf[2] = SLJIT_FUNC_ADDR(test_call4_f1);
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

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 4, 3, 4, 0, sizeof(sljit_sw));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 33);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -20);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call4_f1));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -40);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_R2, 0);
	/* wbuf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call4_f1));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, -25);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 100);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -10);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_R0, 0);
	/* wbuf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 231);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call4_f1) - 100);
	sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(W, W, 32, W, W), SLJIT_MEM2(SLJIT_R0, SLJIT_R2), SLJIT_WORD_SHIFT);
	/* wbuf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 3 * sizeof(sljit_sw), SLJIT_R0, 0);

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S1), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S2), 0);
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -100);
		sljit_emit_icall(compiler, SLJIT_CALL, SLJIT_ARGS4(32, F64, F32, F64, W), SLJIT_IMM, SLJIT_FUNC_ADDR(test_call4_f2));
		sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_R0, 0, SLJIT_R0, 0);
		/* wbuf[4] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 4 * sizeof(sljit_sw), SLJIT_R0, 0);

		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_f32));
		sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_f64));
		sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R0, 0, SLJIT_IMM, 36);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 41);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS4(F32, F32, 32, F64, W));
		sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call4_f3));
		/* sbuf[2] */
		sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_S2), 2 * sizeof(sljit_f32), SLJIT_FR0, 0);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call4_f4));
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
	sljit_free_code(code.code, NULL);

	FAILED(wbuf[0] != -27, "test_call4 case 1 failed\n");
	FAILED(wbuf[1] != 65, "test_call4 case 2 failed\n");
	FAILED(wbuf[3] != (sljit_sw)wbuf + 133, "test_call4 case 3 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		FAILED(wbuf[4] != -88, "test_call4 case 4 failed\n");
		FAILED(sbuf[2] != 79.75f, "test_call4 case 5 failed\n");
		FAILED(sbuf[3] != 8.625f, "test_call4 case 6 failed\n");
	}

	successful_tests++;
}

static sljit_sw test_call5_f1(sljit_sw a)
{
	return a + 10000;
}

static sljit_sw test_call5_f2(sljit_sw a, sljit_s32 b, sljit_s32 c, sljit_sw d)
{
	return a | b | c | d;
}

static sljit_sw test_call5_f3(sljit_sw a, sljit_s32 b, sljit_s32 c, sljit_sw d)
{
	SLJIT_UNUSED_ARG(a);
	return b | c | d;
}

static sljit_sw test_call5_f4(void)
{
	return 7461932;
}

static void test_call5(void)
{
	/* Test tail calls. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump;
	sljit_uw jump_addr;
	sljit_sw executable_offset;
	sljit_sw res;

	if (verbose)
		printf("Run test_call5\n");

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
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS1(W, W), SLJIT_IMM, SLJIT_FUNC_ADDR(test_call5_f1));
	/* Should crash. */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.func1(7987);
	sljit_free_code(code.code, NULL);

	FAILED(res != 17987, "test_call5 case 1 failed\n");

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

	sljit_set_jump_addr(jump_addr, SLJIT_FUNC_UADDR(test_call5_f1), executable_offset);

	res = code.func1(3903);
	sljit_free_code(code.code, NULL);

	FAILED(res != 13903, "test_call5 case 2 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call5_f2));
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

	res = code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(res != 0x28142841, "test_call5 case 3 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 4, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call5_f2));
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

	res = code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(res != (sljit_sw)0x81482114, "test_call5 case 4 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 4, 0, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, SLJIT_FUNC_ADDR(test_call5_f3));
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R1, 0, SLJIT_IMM, 0x342);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_IMM, 0x451000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 0x21000000);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, W, 32, 32, W), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(res != 0x21451342, "test_call5 case 5 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 1, 0, 0, 0, 9);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS0(W), SLJIT_IMM, SLJIT_FUNC_ADDR(test_call5_f4));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(res != 7461932, "test_call5 case 6 failed\n");

	successful_tests++;
}

static sljit_sw test_call6_f5(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_f64 d)
{
	if (a == 1345.5 && b == -8724.25 && c == 9034.75 && d == 6307.5)
		return 8920567;
	return 0;
}

static sljit_sw test_call6_f6(sljit_f64 a, sljit_f64 b, sljit_f64 c, sljit_sw d)
{
	if (a == 4061.25 && b == -3291.75 && c == 8703.5 && d == 1706)
		return 5074526;
	return 0;
}

static void test_call6(void)
{
	/* Test tail calls. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump;
	sljit_sw res;
	sljit_sw wbuf[1];
	sljit_f64 dbuf[4];

	if (verbose)
		printf("Run test_call6\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test_call6 skipped\n");
		successful_tests++;
		return;
	}

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
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, F64), SLJIT_IMM, SLJIT_FUNC_ADDR(test_call6_f5));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.test_call6_f1(1345.5f, -8724.25);
	sljit_free_code(code.code, NULL);

	FAILED(res != 8920567, "test_call6 case 1 failed\n");

	/* Next test. */

	wbuf[0] = SLJIT_FUNC_ADDR(test_call6_f5);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS4(W, F64, F64, F64, F64), 1, 0, 4, 0, 0);
	sljit_emit_icall(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, F64), SLJIT_MEM0(), (sljit_sw)wbuf);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.test_call6_f2(1345.5, -8724.25, 9034.75, 6307.5);
	sljit_free_code(code.code, NULL);

	FAILED(res != 8920567, "test_call6 case 2 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), 1, 0, 4, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call6_f6));

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.test_call6_f3(4061.25, -3291.75, 8703.5);
	sljit_free_code(code.code, NULL);

	FAILED(res != 5074526, "test_call6 case 3 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 1, 0, 4, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call6_f6));

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.test_call6_f3(4061.25, -3291.75, 8703.5);
	sljit_free_code(code.code, NULL);

	FAILED(res != 5074526, "test_call6 case 4 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3(W, F64, F64, F64), SLJIT_NUMBER_OF_SCRATCH_REGISTERS + 1, 1, 3, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
	jump = sljit_emit_call(compiler, SLJIT_CALL | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_call6_f6));

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	res = code.test_call6_f3(4061.25, -3291.75, 8703.5);
	sljit_free_code(code.code, NULL);

	FAILED(res != 5074526, "test_call6 case 5 failed\n");

	successful_tests++;
}

static void test_call7(void)
{
	/* Test register argument and keep saved registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_sw buf[9];
	sljit_s32 i;

	if (verbose)
		printf("Run test_call7\n");

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 7945);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -9267);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 4309);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -8321);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6803);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -5497);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 4, 2, 0, 0, 32);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[3-6] */
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
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 6028, "test_call7 case 1 failed\n");
	FAILED(buf[1] != 6803, "test_call7 case 2 failed\n");
	FAILED(buf[2] != -5497, "test_call7 case 3 failed\n");
	FAILED(buf[3] != 7945, "test_call7 case 4 failed\n");
	FAILED(buf[4] != -9267, "test_call7 case 5 failed\n");
	FAILED(buf[5] != 4309, "test_call7 case 6 failed\n");
	FAILED(buf[6] != -8321, "test_call7 case 7 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -2608);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 4751);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 5740);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -9704);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -8749);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 9213);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 6, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_set_context(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 6, 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[3-7] */
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
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != -7351, "test_call7 case 8 failed\n");
	FAILED(buf[1] != 3628, "test_call7 case 9 failed\n");
	FAILED(buf[2] != 9213, "test_call7 case 10 failed\n");
	FAILED(buf[3] != -8749, "test_call7 case 11 failed\n");
	FAILED(buf[4] != -2608, "test_call7 case 12 failed\n");
	FAILED(buf[5] != 4751, "test_call7 case 13 failed\n");
	FAILED(buf[6] != 5740, "test_call7 case 14 failed\n");
	FAILED(buf[7] != -9704, "test_call7 case 15 failed\n");
	FAILED(buf[8] != -1, "test_call7 case 16 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 4, 2, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8653);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 7245);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, -3610);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, 4591);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -2865);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 2510);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4V(W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS4(W, W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[2-7] */
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
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 5789, "test_call7 case 17 failed\n");
	FAILED(buf[1] != -9214, "test_call7 case 18 failed\n");
	FAILED(buf[2] != -2865, "test_call7 case 19 failed\n");
	FAILED(buf[3] != 2510, "test_call7 case 20 failed\n");
	FAILED(buf[4] != 8653, "test_call7 case 21 failed\n");
	FAILED(buf[5] != 7245, "test_call7 case 22 failed\n");
	FAILED(buf[6] != -3610, "test_call7 case 23 failed\n");
	FAILED(buf[7] != 4591, "test_call7 case 24 failed\n");
	FAILED(buf[8] != -1, "test_call7 case 25 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 2, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6071);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -3817);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 9250);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS0(W), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[5] */
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
	/* buf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 6 * sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_R2, 0, SLJIT_R1, 0);
	/* buf[7] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 7 * sizeof(sljit_sw), SLJIT_R2, 0);
	/* buf[8] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 8 * sizeof(sljit_sw), SLJIT_R3, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 6923);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 6923, "test_call7 case 26 failed\n");
	FAILED(buf[1] != 4892, "test_call7 case 27 failed\n");
	FAILED(buf[2] != -7513, "test_call7 case 28 failed\n");
	FAILED(buf[3] != 9250, "test_call7 case 29 failed\n");
	FAILED(buf[4] != 6071, "test_call7 case 30 failed\n");
	FAILED(buf[5] != -3817, "test_call7 case 31 failed\n");
	FAILED(buf[6] != -6278, "test_call7 case 32 failed\n");
	FAILED(buf[7] != 7150, "test_call7 case 33 failed\n");
	FAILED(buf[8] != 8413, "test_call7 case 34 failed\n");

	successful_tests++;
}

static void test_call8(void)
{
	/* Test register argument and keep saved registers. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_sw buf[9];
	sljit_f64 dbuf[3];
	sljit_s32 i;

	if (verbose)
		printf("Run test_call8\n");

	for (i = 0; i < 8; i++)
		buf[i] = -1;

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test_call8 skipped\n");
		successful_tests++;
		return;
	}

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	dbuf[0] = 4061.25;
	dbuf[1] = -3291.75;
	dbuf[2] = 8703.5;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 2, 3, 3, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	/* dbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R1), 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_f64));
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_f64));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -8956);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 4381);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -5314);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4(W, F64, F64, F64, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS4(W, F64, F64, F64, W_R), 1, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_sw), SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	/* dbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_FR2, 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_f64), SLJIT_FR0, 0);
	/* dbuf[2] */
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
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 2784, "test_call8 case 1 failed\n");
	FAILED(buf[1] != 1503, "test_call8 case 2 failed\n");
	FAILED(buf[2] != 4381, "test_call8 case 3 failed\n");
	FAILED(buf[3] != -5314, "test_call8 case 4 failed\n");
	FAILED(buf[4] != -8956, "test_call8 case 5 failed\n");
	FAILED(buf[5] != 1706, "test_call8 case 6 failed\n");
	FAILED(buf[6] != -1, "test_call8 case 7 failed\n");
	FAILED(dbuf[0] != 8703.5, "test_call8 case 8 failed\n");
	FAILED(dbuf[1] != 4061.25, "test_call8 case 9 failed\n");
	FAILED(dbuf[2] != -3291.75, "test_call8 case 10 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	dbuf[0] = 4061.25;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 3, 3, 1, 0, 0);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM0(), (sljit_sw)&dbuf);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8793);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -4027);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 2910);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4619);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1502);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 5316);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4V(F64, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_S0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS4V(F64, W_R, W_R, W_R), 3, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 3 * sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 4 * sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 5 * sizeof(sljit_sw), SLJIT_R0, 0);
	/* buf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), 6 * sizeof(sljit_sw), SLJIT_R1, 0);
	/* buf[7] */
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
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 7839, "test_call8 case 11 failed\n");
	FAILED(buf[1] != -9215, "test_call8 case 12 failed\n");
	FAILED(buf[2] != 5316, "test_call8 case 13 failed\n");
	FAILED(buf[3] != 4619, "test_call8 case 14 failed\n");
	FAILED(buf[4] != -1502, "test_call8 case 15 failed\n");
	FAILED(buf[5] != 8793, "test_call8 case 16 failed\n");
	FAILED(buf[6] != -4027, "test_call8 case 17 failed\n");
	FAILED(buf[7] != 2910, "test_call8 case 18 failed\n");
	FAILED(buf[8] != -1, "test_call8 case 19 failed\n");
	FAILED(dbuf[0] != -4061.25, "test_call8 case 20 failed\n");

	/* Next test. */

	for (i = 0; i < 9; i++)
		buf[i] = -1;

	dbuf[0] = 4061.25;
	dbuf[1] = -3291.75;
	dbuf[2] = 8703.5;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 2, 3, 0, 0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 7869);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -5406);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 4951);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_R0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 3 * sizeof(sljit_sw), SLJIT_S2, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS0(W), 1, 3, 3, 0, SLJIT_MAX_LOCAL_SIZE);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 5 * sizeof(sljit_sw), SLJIT_S1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	/* dbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R0), 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64));
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 1706);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 4713);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -2078);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, -1);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG | SLJIT_CALL_RETURN, SLJIT_ARGS4(W, F64, F64, F64, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS4(W, F64, F64, F64, W_R), 1, 0, 3, 0, 256);

	/* buf[6] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)&buf[6], SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&dbuf);
	/* dbuf[0] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_FR2, 0);
	/* dbuf[1] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_FR0, 0);
	/* dbuf[2] */
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 2 * sizeof(sljit_f64), SLJIT_FR1, 0);

	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_IMM, 5074);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 5074, "test_call8 case 20 failed\n");
	FAILED(buf[1] != 4713, "test_call8 case 22 failed\n");
	FAILED(buf[2] != -2078, "test_call8 case 23 failed\n");
	FAILED(buf[3] != 4951, "test_call8 case 24 failed\n");
	FAILED(buf[4] != 7869, "test_call8 case 25 failed\n");
	FAILED(buf[5] != -5406, "test_call8 case 26 failed\n");
	FAILED(buf[6] != 1706, "test_call8 case 27 failed\n");
	FAILED(buf[7] != -1, "test_call8 case 28 failed\n");
	FAILED(dbuf[0] != 8703.5, "test_call8 case 29 failed\n");
	FAILED(dbuf[1] != 4061.25, "test_call8 case 30 failed\n");
	FAILED(dbuf[2] != -3291.75, "test_call8 case 31 failed\n");

	successful_tests++;
}

static void test_call9(void)
{
	/* Test register register preservation in keep saveds mode. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	sljit_sw buf[6 + SLJIT_NUMBER_OF_REGISTERS];
	struct sljit_jump* jump;
	sljit_s32 i;

	if (verbose)
		printf("Run test_call9\n");

	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), SLJIT_NUMBER_OF_REGISTERS - 3, 3, 0, 0, 0);

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R(i), 0, SLJIT_IMM, 8469 + 1805 * i);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 3671);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 2418);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 1597);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS4V(W, W, W, W));

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 6), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)&buf);
	/* buf[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 3 * sizeof(sljit_sw), SLJIT_S0, 0);
	/* buf[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 4 * sizeof(sljit_sw), SLJIT_S1, 0);
	/* buf[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 5 * sizeof(sljit_sw), SLJIT_S2, 0);

	for (i = 1; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), (6 + i) * (sljit_sw)sizeof(sljit_sw), SLJIT_R(i), 0);

	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS4V(W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_set_context(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS4V(W_R, W_R, W_R, W_R), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE);

	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 0), SLJIT_S0, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 1), SLJIT_S1, 0);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM0(), (sljit_sw)(buf + 2), SLJIT_S2, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 6501);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 7149);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 5732);

	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG | SLJIT_CALL_RETURN, SLJIT_ARGS0V());
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_MEM0(), 0);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS0V(), 4, 3, 0, 0, SLJIT_MAX_LOCAL_SIZE / 2);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 3671, "test_call9 case 1 failed\n");
	FAILED(buf[1] != 2418, "test_call9 case 2 failed\n");
	FAILED(buf[2] != 1597, "test_call9 case 3 failed\n");
	FAILED(buf[3] != 6501, "test_call9 case 4 failed\n");
	FAILED(buf[4] != 7149, "test_call9 case 5 failed\n");
	FAILED(buf[5] != 5732, "test_call9 case 6 failed\n");

	for (i = 0; i < SLJIT_NUMBER_OF_REGISTERS - 3; i++) {
		FAILED(buf[6 + i] != 8469 + 1805 * i, "test_call9 case 7 failed\n");
	}

	successful_tests++;
}

static void test_call10(void)
{
	/* Test return with floating point value. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	sljit_f64 dbuf[2];
	sljit_f32 sbuf[2];

	if (verbose)
		printf("Run test_call10\n");

	if (!sljit_has_cpu_feature(SLJIT_HAS_FPU)) {
		if (verbose)
			printf("no fpu available, test_call10 skipped\n");
		successful_tests++;
		return;
	}

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(F64, W), 0, 1, 3, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_return(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	dbuf[0] = 35.125;
	dbuf[0] = code.test_call10_f2((sljit_sw)dbuf);
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[0] != 35.125, "test_call10 case 1 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(F32, W), 0, 1, 1, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_RETURN_FREG, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_return(compiler, SLJIT_MOV_F32, SLJIT_RETURN_FREG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	sbuf[0] = -9027.5;
	sbuf[0] = code.test_call10_f1((sljit_sw)sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(sbuf[0] != -9027.5, "test_call10 case 2 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(F32, W), 0, 1, 1, 0, sizeof(sljit_f32));
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_return(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_SP), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	sbuf[0] = -6.75;
	sbuf[0] = code.test_call10_f1((sljit_sw)sbuf);
	sljit_free_code(code.code, NULL);

	FAILED(sbuf[0] != -6.75, "test_call10 case 3 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1(F64, W), 0, 1, 1, 0, 2 * sizeof(sljit_f64));
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f64), SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_return(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_f64));

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	dbuf[0] = 45.125;
	dbuf[0] = code.test_call10_f2((sljit_sw)dbuf);
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[0] != 45.125, "test_call10 case 4 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 1, 0, 1, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)dbuf - 33);
	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS1(F64, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)dbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f64), SLJIT_RETURN_FREG, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS1(F64, W_R), 1, 0, 1, 0, 0);
	sljit_emit_return(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_R0), 33);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	dbuf[0] = 2571.75;
	dbuf[1] = 0;
	code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(dbuf[1] != 2571.75, "test_call10 case 5 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 1, 0, 1, 0, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(F32));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)sbuf);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_f32), SLJIT_RETURN_FREG, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS0(F32), 0, 0, 1, 0, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_RETURN_FREG, 0, SLJIT_MEM0(), (sljit_sw)sbuf);
	sljit_emit_return(compiler, SLJIT_MOV_F32, SLJIT_RETURN_FREG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	sbuf[0] = 6310.25;
	sbuf[1] = 0;
	code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(sbuf[1] != 6310.25, "test_call10 case 6 failed\n");

	successful_tests++;
}

static void test_call11(void)
{
	/* Test return_to operation. */
	executable_code code, code2;
	struct sljit_compiler* compiler;
	struct sljit_jump* jump;
	struct sljit_label* label;
	sljit_s32 i;
	sljit_sw buf[3];

	if (verbose)
		printf("Run test_call11\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 2, 1, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -7602);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S0, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2(W, W, W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);
	label = sljit_emit_label(compiler);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);
	sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(W_R, W_R), 2, 0, 0, 0, 256);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 256 - sizeof(sljit_sw), SLJIT_IMM, -1);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), sizeof(sljit_sw), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, 8945);
	sljit_emit_return_to(compiler, SLJIT_MEM1(SLJIT_R1), 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	buf[0] = (sljit_sw)sljit_get_label_addr(label);
	buf[1] = 0;

	sljit_free_compiler(compiler);
	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != 8945, "test_call11 case 1 failed\n");
	FAILED(buf[1] != -7602, "test_call11 case 2 failed\n");

	/* Next test. */

	for (i = 0; i < 3; i++) {
		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 2, 1, 0, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 6032);
		jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS1(W, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);
		label = sljit_emit_label(compiler);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)buf);
		/* buf[0] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 0, SLJIT_RETURN_REG, 0);
		/* buf[2] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R1), 2 * sizeof(sljit_sw), SLJIT_S0, 0);
		sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(1), SLJIT_ARGS1V(W_R), 2, i == 1 ? 2 : 1, 0, 0, SLJIT_MAX_LOCAL_SIZE);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_IMM, -1);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), sizeof(sljit_sw), SLJIT_R0, 0);
		/* buf[1] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_MEM1(SLJIT_SP), sizeof(sljit_sw));
		if (i == 2)
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_S0), 0);
		else
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S(i), 0, SLJIT_MEM1(SLJIT_S0), 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), SLJIT_MAX_LOCAL_SIZE - sizeof(sljit_sw), SLJIT_IMM, -1);
		if (i != 0)
			sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -3890);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, 7145);
		if (i == 2)
			sljit_emit_return_to(compiler, SLJIT_MEM1(SLJIT_SP), 2 * sizeof(sljit_sw));
		else
			sljit_emit_return_to(compiler, SLJIT_S(i), 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);

		buf[0] = (sljit_sw)sljit_get_label_addr(label);
		buf[1] = 0;
		buf[2] = 0;

		sljit_free_compiler(compiler);

		code.func1((sljit_sw)buf);
		sljit_free_code(code.code, NULL);

		FAILED(buf[0] != 7145, "test_call11 case 3 failed\n");
		FAILED(buf[1] != 6032, "test_call11 case 4 failed\n");
		if (i != 0)
			FAILED(buf[2] != -3890, "test_call11 case 5 failed\n");
	}

	/* Next test. */

	for (i = 0; i < 3; i++) {
		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P_R), 2, 1, 0, 0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_R0, 0);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS1(W, W));
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);
		label = sljit_emit_label(compiler);

		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)buf);
		/* buf[0] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);
		sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(W_R), 2, 1, 0, 0, (i == 0) ? 0 : (i == 1) ? 512 : 32768);
		/* buf[1] */
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), sizeof(sljit_sw), SLJIT_R0, 0);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, -1);
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R0, 0, SLJIT_IMM, 0x1000);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, -4502);
		sljit_emit_return_to(compiler, SLJIT_MEM1(SLJIT_R1), -0x1000);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);

		buf[0] = (sljit_sw)sljit_get_label_addr(label);
		buf[1] = 0;

		sljit_free_compiler(compiler);

		code.func1((sljit_sw)buf);
		sljit_free_code(code.code, NULL);

		FAILED(buf[0] != -4502, "test_call11 case 6 failed\n");
		FAILED(buf[1] != (sljit_sw)buf, "test_call11 case 7 failed\n");
	}

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

#if (defined SLJIT_CONFIG_X86_32 && SLJIT_CONFIG_X86_32)
	i = SLJIT_S2;
#else
	i = SLJIT_S(SLJIT_NUMBER_OF_SAVED_REGISTERS - 1);
#endif

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 2, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, i, 0, SLJIT_IMM, 2 * sizeof(sljit_sw));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(W));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);
	label = sljit_emit_label(compiler);
	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM2(SLJIT_S0, i), 0, SLJIT_RETURN_REG, 0);
	sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 2, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 16);
	for (i = 0; i < SLJIT_NUMBER_OF_SAVED_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S(i), 0, SLJIT_IMM, -1);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, (sljit_sw)(buf + 3));
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -3);
	sljit_emit_return_to(compiler, SLJIT_MEM2(SLJIT_RETURN_REG, SLJIT_R1), SLJIT_WORD_SHIFT);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	buf[0] = (sljit_sw)sljit_get_label_addr(label);
	buf[1] = 0;
	buf[2] = 0;

	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[2] != (sljit_sw)(buf + 3), "test_call11 case 8 failed\n");

	/* Next test. */

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P_R, P), 2, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 586000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 392);
	sljit_emit_icall(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS0(W), SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM0(), 0);
	label = sljit_emit_label(compiler);
	/* buf[0] */
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM1(SLJIT_S2), 0, SLJIT_S0, 0, SLJIT_S1, 0);
	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_sw), SLJIT_RETURN_REG, 0);
	sljit_emit_op0(compiler, SLJIT_SKIP_FRAMES_BEFORE_RETURN);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);

	buf[0] = (sljit_sw)sljit_get_label_addr(label);

	sljit_free_compiler(compiler);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(2), SLJIT_ARGS0V(), 2, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, 16);
	for (i = 2; i < SLJIT_NUMBER_OF_SAVED_REGISTERS; i++)
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S(i), 0, SLJIT_IMM, -1);
	/* buf[2] */
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_MEM0(), (sljit_sw)(buf + 2), SLJIT_S0, 0, SLJIT_S1, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S0, 0, SLJIT_IMM, 416000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 931);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0, SLJIT_IMM, 2906);
	sljit_emit_return_to(compiler, SLJIT_IMM, buf[0]);

	code2.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;

	code.func2(SLJIT_FUNC_ADDR(code2.func0), (sljit_sw)buf);
	sljit_free_code(code.code, NULL);
	sljit_free_code(code2.code, NULL);

	FAILED(buf[0] != 416931, "test_call11 case 9 failed\n");
	FAILED(buf[1] != 2906, "test_call11 case 10 failed\n");
	FAILED(buf[2] != 586392, "test_call11 case 11 failed\n");

	successful_tests++;
}

static void test_call12(void)
{
	/* Test get return address. */
	executable_code code;
	struct sljit_compiler* compiler;
	struct sljit_jump *jump;
	struct sljit_label *label;
	sljit_uw return_addr = 0;
	sljit_uw buf[1];

	if (verbose)
		printf("Run test_call12\n");

	/* Next test. */

	buf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(W), 1, 1, 0, 0, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(W));
	label = sljit_emit_label(compiler);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 1, 0, 0, 0, 0);
	sljit_emit_op_dst(compiler, SLJIT_GET_RETURN_ADDRESS, SLJIT_RETURN_REG, 0);
	sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	return_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != return_addr, "test_call12 case 1 failed\n");

	/* Next test. */

	buf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS0V(), 2, 0, 0, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, -1);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, -1);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS2V(W, W));
	label = sljit_emit_label(compiler);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(W, W), 1, SLJIT_NUMBER_OF_SAVED_REGISTERS - 2, 0, 0, SLJIT_MAX_LOCAL_SIZE);
	sljit_emit_op_dst(compiler, SLJIT_GET_RETURN_ADDRESS, SLJIT_MEM0(), (sljit_sw)buf);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	return_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	code.func0();
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != return_addr, "test_call12 case 2 failed\n");

	/* Next test. */

	buf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(W), 1, 3, 0, 0, 0);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_S2, 0, SLJIT_S0, 0, SLJIT_IMM, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 8);
	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS1V(W));
	label = sljit_emit_label(compiler);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG | SLJIT_ENTER_KEEP(3), SLJIT_ARGS1V(W_R), 1, SLJIT_NUMBER_OF_SAVED_REGISTERS, 0, 0, SLJIT_MAX_LOCAL_SIZE >> 1);
	sljit_emit_op_dst(compiler, SLJIT_GET_RETURN_ADDRESS, SLJIT_MEM2(SLJIT_S2, SLJIT_R0), 1);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	return_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != return_addr, "test_call12 case 3 failed\n");

	/* Next test. */

	buf[0] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(W_R), 1, 0, 0, 0, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL_REG_ARG, SLJIT_ARGS1V(W));
	label = sljit_emit_label(compiler);
	sljit_emit_return_void(compiler);

	sljit_set_label(jump, sljit_emit_label(compiler));
	sljit_emit_enter(compiler, SLJIT_ENTER_REG_ARG, SLJIT_ARGS1V(W_R), 1, SLJIT_NUMBER_OF_SAVED_REGISTERS >> 1, 0, 0, 64);
	sljit_emit_op_dst(compiler, SLJIT_GET_RETURN_ADDRESS, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R0), 0, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	return_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(buf[0] != return_addr, "test_call12 case 4 failed\n");

	if (sljit_has_cpu_feature(SLJIT_HAS_FPU) && SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0) {
		/* Next test. */

		buf[0] = 0;

		compiler = sljit_create_compiler(NULL, NULL);
		FAILED(!compiler, "cannot create compiler\n");

		sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(W), 1, 1, 0, 0, 0);
		jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS0(W));
		label = sljit_emit_label(compiler);
		sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_RETURN_REG, 0);
		sljit_emit_return_void(compiler);

		sljit_set_label(jump, sljit_emit_label(compiler));
		sljit_emit_enter(compiler, 0, SLJIT_ARGS0(W), 1, 3, 0, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS, 64);
		sljit_emit_op_dst(compiler, SLJIT_GET_RETURN_ADDRESS, SLJIT_RETURN_REG, 0);
		sljit_emit_return(compiler, SLJIT_MOV, SLJIT_RETURN_REG, 0);

		code.code = sljit_generate_code(compiler);
		CHECK(compiler);
		return_addr = sljit_get_label_addr(label);
		sljit_free_compiler(compiler);

		code.func1((sljit_sw)buf);
		sljit_free_code(code.code, NULL);

		FAILED(buf[0] != return_addr, "test_call12 case 5 failed\n");
	}

	successful_tests++;
}
