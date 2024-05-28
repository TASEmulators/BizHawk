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

static void simd_set(sljit_u8* buf, sljit_u8 start, sljit_s32 length)
{
	do {
		*buf++ = start;
		start = (sljit_u8)(start + 103);

		if (start == 0xaa)
			start = 0xab;
	} while (--length != 0);
}

static sljit_s32 check_simd_mov(sljit_u8* buf, sljit_u8 start, sljit_s32 length)
{
	if (buf[-1] != 0xaa || buf[length] != 0xaa)
		return 0;

	do {
		if (*buf++ != start)
			return 0;

		start = (sljit_u8)(start + 103);

		if (start == 0xaa)
			start = 0xab;
	} while (--length != 0);

	return 1;
}

static void test_simd1(void)
{
	/* Test simd data transfer. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 880];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd1\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 880; i++)
		buf[i] = 0xaa;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	simd_set(buf + 0, 81, 16);
	simd_set(buf + 65, 213, 16);
	simd_set(buf + 104, 33, 16);
	simd_set(buf + 160, 140, 16);
	simd_set(buf + 210, 7, 16);
	simd_set(buf + 256, 239, 16);
	simd_set(buf + 312, 176, 16);
	simd_set(buf + 368, 88, 8);
	simd_set(buf + 393, 197, 8);
	simd_set(buf + 416, 58, 16);
	simd_set(buf + 432, 203, 16);
	simd_set(buf + 496, 105, 16);
	simd_set(buf + 560, 19, 16);
	simd_set(buf + 616, 202, 8);
	simd_set(buf + 648, 123, 8);
	simd_set(buf + 704, 85, 32);
	simd_set(buf + 801, 215, 32);

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 2, 2, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 64);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_MEM_ALIGNED_128;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	/* buf[32] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 65);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 82 >> 1);
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_MEM_UNALIGNED;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	/* buf[82] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), 1);

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 70001);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 70001);
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_MEM_ALIGNED_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_R0), 70001 + 104);
	/* buf[136] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_R1), 136 - 70001);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_MEM_ALIGNED_128;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 160));
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 192));

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 1001);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 1001);
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_MEM_ALIGNED_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), 1001 + 210);
	/* buf[230] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_R1), 230 - 1001);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 256 >> 3);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 288 >> 3);
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_MEM_ALIGNED_128;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 3);
	/* buf[288] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), 3);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_MEM_ALIGNED_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 312);
	/* buf[344] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 344);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_MEM_ALIGNED_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 368);
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 384);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 393);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 402);
	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_MEM_UNALIGNED;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	/* buf[402] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM2(SLJIT_S0, SLJIT_R1), 0);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_MEM_ALIGNED_128;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 416);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 432);
	/* buf[464] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 464);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 496);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 480);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_FR3, 0);
	/* buf[528] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 528);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 560);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 544);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_FR0, 0);
	/* buf[592] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 592);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 616);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 608);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_FR5, 0);
	/* buf[632] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 632);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 648);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 640);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, fs0, 0);
	/* buf[664] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 664);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_MEM_ALIGNED_256;
	supported[0] = sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 704) != SLJIT_ERR_UNSUPPORTED;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32, SLJIT_FR2, fs0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 384);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM2(SLJIT_R1, SLJIT_S1), 1);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 801 - 32);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), 32);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_get_local_base(compiler, SLJIT_R1, 0, 128);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_R1), -128);
	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_MEM_ALIGNED_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM0(), (sljit_sw)(buf + 834));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_mov(buf + 32, 81, 16), "test_simd1 case 1 failed\n");
	FAILED(!check_simd_mov(buf + 82, 213, 16), "test_simd1 case 2 failed\n");
	FAILED(!check_simd_mov(buf + 136, 33, 16), "test_simd1 case 3 failed\n");
	FAILED(!check_simd_mov(buf + 192, 140, 16), "test_simd1 case 4 failed\n");
	FAILED(!check_simd_mov(buf + 230, 7, 16), "test_simd1 case 5 failed\n");
	FAILED(!check_simd_mov(buf + 288, 239, 16), "test_simd1 case 6 failed\n");
	FAILED(!check_simd_mov(buf + 344, 176, 16), "test_simd1 case 7 failed\n");
#if IS_ARM
	FAILED(!check_simd_mov(buf + 384, 88, 8), "test_simd1 case 8 failed\n");
	FAILED(!check_simd_mov(buf + 402, 197, 8), "test_simd1 case 9 failed\n");
#endif /* IS_ARM */
	FAILED(!check_simd_mov(buf + 464, sljit_has_cpu_feature(SLJIT_SIMD_REGS_ARE_PAIRS) ? 203 : 58, 16), "test_simd1 case 10 failed\n");
	FAILED(!check_simd_mov(buf + 528, 105, 16), "test_simd1 case 11 failed\n");
	FAILED(!check_simd_mov(buf + 592, 19, 16), "test_simd1 case 12 failed\n");
#if IS_ARM
	FAILED(!check_simd_mov(buf + 632, 202, 8), "test_simd1 case 13 failed\n");
	FAILED(!check_simd_mov(buf + 664, 123, 8), "test_simd1 case 14 failed\n");
#endif /* IS_ARM */

	if (supported[0]) {
		FAILED(!check_simd_mov(buf + 768, 85, 32), "test_simd1 case 15 failed\n");
		FAILED(!check_simd_mov(buf + 834, 215, 32), "test_simd1 case 16 failed\n");
	}

	successful_tests++;
}

static sljit_s32 check_simd_lane_mov(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_s32 is_odd)
{
	sljit_s32 count = (length / elem_size) >> 1;
	sljit_s32 value = 180 + length - elem_size;
	sljit_s32 i;

	if (!is_odd)
		value -= elem_size;

	do {
		if (is_odd) {
			for (i = 0; i < elem_size; i++)
				if (*buf++ != 0xaa)
					return 0;
		}

		for (i = 0; i < elem_size; i++)
			if (*buf++ != value++)
				return 0;

		if (!is_odd) {
			for (i = 0; i < elem_size; i++)
				if (*buf++ != 0xaa)
					return 0;
		}

		value -= 3 * elem_size;
	} while (--count != 0);

	return 1;
}

static void test_simd2(void)
{
	/* Test simd lane data transfer. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 576];
	sljit_f64 tmp[1];
	sljit_u32 f32_result = 0;
	sljit_sw result[6];
	sljit_s32 result32[5];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd2\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 64; i++)
		buf[i] = (sljit_u8)(180 + i);

	for (i = 64; i < 576; i++)
		buf[i] = 0xaa;

	for (i = 0; i < 6; i++)
		result[i] = 0;

	for (i = 0; i < 5; i++)
		result32[i] = 0;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)tmp - 100000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)tmp + 1000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 100000 / 2);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 14, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 0, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 12, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_get_local_base(compiler, SLJIT_R2, 0, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 2, SLJIT_MEM1(SLJIT_R2), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 10, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 4, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 8, SLJIT_R3, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 6, SLJIT_R3, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 6, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 8, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 4, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 10, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 2, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 12, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 0, SLJIT_S2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 14, SLJIT_S2, 0);
	/* buf[128] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 128);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 1, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, 15, SLJIT_IMM, 181 + 0xffff00);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 183 + 0xffff00);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, 13, SLJIT_R2, 0);
	for (i = 5; i < 16; i += 2) {
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | SLJIT_32 | type, SLJIT_FR0, i, SLJIT_R2, 0);
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | SLJIT_32 | type, SLJIT_FR5, 16 - i, SLJIT_R2, 0);
	}
	/* buf[144] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 144);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 6, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 0, SLJIT_R2, 0);
	sljit_get_local_base(compiler, SLJIT_R2, 0, 4);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 4, SLJIT_MEM1(SLJIT_R2), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 2, SLJIT_MEM1(SLJIT_SP), 4);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 2, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 4, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 0, SLJIT_S3, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 6, SLJIT_S3, 0);
	/* buf[160] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 160);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 7, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 1, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 5, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 3, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 3, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 5, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 1, SLJIT_S2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 7, SLJIT_S2, 0);
	/* buf[176] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 176);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 2, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 0, SLJIT_R2, 0);
	sljit_get_local_base(compiler, SLJIT_R2, 0, 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_R2), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 2, SLJIT_MEM1(SLJIT_SP), 8);
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 192);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 3, SLJIT_S3, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, 1, SLJIT_S3, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 1, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, 3, SLJIT_MEM1(SLJIT_R1), -1000);
	/* buf[208] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 208);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 0, SLJIT_R2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, 0, SLJIT_R2, 0);
	/* buf[224] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 224);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 1, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 1, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	/* buf[240] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 240);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 2, SLJIT_FR1, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, 0, SLJIT_FR1, 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM0(), (sljit_sw)&f32_result, SLJIT_FR1, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 0, SLJIT_FR0, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, 2, SLJIT_FR0, 0);
	/* buf[256] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 256);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 3, SLJIT_FR2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 1, SLJIT_FR2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 1, SLJIT_FR4, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 3, SLJIT_FR4, 0);
	/* buf[272] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 272);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 2, SLJIT_MEM1(SLJIT_SP), 4);
	sljit_get_local_base(compiler, SLJIT_R2, 0, 4);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_R2), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 2, SLJIT_MEM1(SLJIT_R1), -1000);
	/* buf[288] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 288);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 3, SLJIT_MEM1(SLJIT_R1), -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 1, SLJIT_MEM2(SLJIT_R0, SLJIT_S1), 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 1, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 3, SLJIT_MEM0(), (sljit_sw)tmp);
	/* buf[304] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 304);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 0, SLJIT_FR4, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 0, SLJIT_FR4, 0);
	/* buf[320] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 320);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 1, SLJIT_FR2, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, 1, SLJIT_FR2, 0);
	/* buf[336] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 336);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_get_local_base(compiler, SLJIT_R2, 0, 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 0, SLJIT_MEM1(SLJIT_R2), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_SP), 8);
	/* buf[352] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 352);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 1, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 1, SLJIT_MEM1(SLJIT_R1), -1000);
	/* buf[368] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 368);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)result);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | SLJIT_SIMD_REG_128, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);
	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_lane_mov(compiler, type, SLJIT_FR1, 6, SLJIT_R0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S3, 0, SLJIT_IMM, -1);
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 13, SLJIT_S3, 0);
	/* result[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), 0, SLJIT_R0, 0);
	/* result[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_sw), SLJIT_S3, 0);

	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R3, 0, SLJIT_IMM, -1);
	sljit_emit_simd_lane_mov(compiler, type, SLJIT_FR1, 5, SLJIT_R3, 0);
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 7, SLJIT_R1, 0);
	/* result[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_sw) * 2, SLJIT_R3, 0);
	/* result[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_sw) * 3, SLJIT_R1, 0);

	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_lane_mov(compiler, type, SLJIT_FR1, 2, SLJIT_S3, 0);
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 3, SLJIT_R0, 0);
	/* result[4] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_sw) * 4, SLJIT_S3, 0);
	/* result[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_sw) * 5, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, (sljit_sw)result32);
	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_32;
	sljit_emit_simd_lane_mov(compiler, type, SLJIT_FR1, 0, SLJIT_R3, 0);
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 3, SLJIT_S2, 0);
	/* result32[0] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), 0, SLJIT_R3, 0);
	/* result32[1] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_s32), SLJIT_S2, 0);

	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16 | SLJIT_32;
	sljit_emit_simd_lane_mov(compiler, type, SLJIT_FR1, 0, SLJIT_R1, 0);
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 3, SLJIT_S3, 0);
	/* result32[2] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_s32) * 2, SLJIT_R1, 0);
	/* result32[3] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_s32) * 3, SLJIT_S3, 0);

	type = SLJIT_SIMD_STORE | SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_32;
	sljit_emit_simd_lane_mov(compiler, type | SLJIT_SIMD_LANE_SIGNED, SLJIT_FR1, 0, SLJIT_R0, 0);
	/* result32[4] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_R2), sizeof(sljit_s32) * 4, SLJIT_R0, 0);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, (sljit_sw)tmp - 100000);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, (sljit_sw)tmp + 1000);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 64);
	supported[0] = sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 30, SLJIT_MEM1(SLJIT_R1), -1000) != SLJIT_ERR_UNSUPPORTED;
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_R1), -1000);

	for (i = 2; i < 32; i += 2) {
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 30 - i, SLJIT_R2, 0);
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, i, SLJIT_R2, 0);
	}
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 384);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, 1, SLJIT_MEM1(SLJIT_SP), 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 15, SLJIT_MEM1(SLJIT_SP), 8);

	for (i = 3; i < 16; i += 2) {
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, i, SLJIT_R2, 0);
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, 16 - i, SLJIT_R2, 0);
	}
	/* buf[416] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 416);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 6, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, 0, SLJIT_MEM0(), (sljit_sw)tmp);

	for (i = 2; i < 8; i += 2) {
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, 6 - i, SLJIT_S1, 0);
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, i, SLJIT_S1, 0);
	}
	/* buf[448] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 448);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, -1000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 1, SLJIT_MEM1(SLJIT_R0), 100000);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, 3, SLJIT_MEM2(SLJIT_R1, SLJIT_S1), 0);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, 3, SLJIT_S1, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, 1, SLJIT_S1, 0);
	/* buf[480] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 480);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, 1, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, 7, SLJIT_MEM1(SLJIT_SP), 0);

	for (i = 3; i < 8; i += 2) {
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, i, SLJIT_FR2, 0);
		sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, 8 - i, SLJIT_FR2, 0);
	}
	/* buf[512] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 512);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 2, SLJIT_MEM0(), (sljit_sw)tmp);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_R0), 100000);

	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, 0, SLJIT_FR0, 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, 2, SLJIT_FR0, 0);
	/* buf[544] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 544);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_lane_mov(buf + 128, 16, 1, 0), "test_simd2 case 1 failed\n");
	FAILED(!check_simd_lane_mov(buf + 144, 16, 1, 1), "test_simd2 case 2 failed\n");
	FAILED(!check_simd_lane_mov(buf + 160, 16, 2, 0), "test_simd2 case 3 failed\n");
	FAILED(!check_simd_lane_mov(buf + 176, 16, 2, 1), "test_simd2 case 4 failed\n");
	FAILED(!check_simd_lane_mov(buf + 192, 16, 4, 0), "test_simd2 case 5 failed\n");
	FAILED(!check_simd_lane_mov(buf + 208, 16, 4, 1), "test_simd2 case 6 failed\n");
#if IS_64BIT
	FAILED(!check_simd_lane_mov(buf + 224, 16, 8, 0), "test_simd2 case 7 failed\n");
	FAILED(!check_simd_lane_mov(buf + 240, 16, 8, 1), "test_simd2 case 8 failed\n");
#endif /* IS_64BIT */
	FAILED(!check_simd_lane_mov(buf + 256, 16, 4, 0), "test_simd2 case 9 failed\n");
	FAILED(!check_simd_lane_mov(buf + 272, 16, 4, 1), "test_simd2 case 10 failed\n");
	FAILED(!check_simd_lane_mov(buf + 288, 16, 4, 0), "test_simd2 case 11 failed\n");
	FAILED(!check_simd_lane_mov(buf + 304, 16, 4, 1), "test_simd2 case 12 failed\n");
	FAILED(f32_result != LITTLE_BIG(0xbfbebdbc, 0xbcbdbebf), "test_simd2 case 13 failed\n");
	FAILED(!check_simd_lane_mov(buf + 320, 16, 8, 0), "test_simd2 case 14 failed\n");
	FAILED(!check_simd_lane_mov(buf + 336, 16, 8, 1), "test_simd2 case 15 failed\n");
	FAILED(!check_simd_lane_mov(buf + 352, 16, 8, 0), "test_simd2 case 16 failed\n");
	FAILED(!check_simd_lane_mov(buf + 368, 16, 8, 1), "test_simd2 case 17 failed\n");
	FAILED(result[0] != 186, "test_simd2 case 18 failed\n");
	FAILED(result[1] != -63, "test_simd2 case 19 failed\n");
	FAILED(result[2] != LITTLE_BIG(49086, 48831), "test_simd2 case 20 failed\n");
	FAILED(result[3] != LITTLE_BIG(-15422, -15677), "test_simd2 case 21 failed\n");
	FAILED(result[4] != LITTLE_BIG(WCONST(3216948668, -1078018628), WCONST(3166551743, -1128415553)), "test_simd2 case 22 failed\n");
	FAILED(result[5] != LITTLE_BIG(-1010646592, -1061043517), "test_simd2 case 23 failed\n");
	FAILED(result32[0] != 180, "test_simd2 case 24 failed\n");
	FAILED(result32[1] != -73, "test_simd2 case 25 failed\n");
	FAILED(result32[2] != LITTLE_BIG(46516, 46261), "test_simd2 case 26 failed\n");
	FAILED(result32[3] != LITTLE_BIG(-17478, -17733), "test_simd2 case 27 failed\n");
	FAILED(result32[4] != LITTLE_BIG(-1212762700, -1263159625), "test_simd2 case 28 failed\n");

	if (supported[0]) {
		FAILED(!check_simd_lane_mov(buf + 384, 32, 1, 0), "test_simd2 case 29 failed\n");
		FAILED(!check_simd_lane_mov(buf + 416, 32, 2, 1), "test_simd2 case 30 failed\n");
		FAILED(!check_simd_lane_mov(buf + 448, 32, 4, 0), "test_simd2 case 31 failed\n");
#if IS_64BIT
		FAILED(!check_simd_lane_mov(buf + 480, 32, 8, 1), "test_simd2 case 32 failed\n");
#endif /* IS_64BIT */
		FAILED(!check_simd_lane_mov(buf + 512, 32, 4, 1), "test_simd2 case 33 failed\n");
		FAILED(!check_simd_lane_mov(buf + 544, 32, 8, 0), "test_simd2 case 34 failed\n");
	}

	successful_tests++;
}

static sljit_s32 check_simd_replicate(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_s32 value)
{
	sljit_s32 count = length / elem_size;
	sljit_s32 i;

	do {
		for (i = 0; i < elem_size; i++)
			if (*buf++ != value++)
				return 0;

		value -= elem_size;
	} while (--count != 0);

	return 1;
}

static sljit_s32 check_simd_replicate_u32(sljit_u8* buf, sljit_s32 length, sljit_u32 value)
{
	sljit_s32 count = length / 4;
	sljit_u32 start_value = value;
	sljit_s32 i;

	do {
		for (i = 0; i < 4; i++) {
			if (*buf++ != (value & 0xff))
				return 0;
			value >>= 8;
		}

		value = start_value;
	} while (--count != 0);

	return 1;
}

static void test_simd3(void)
{
	/* Test simd replicate scalar to all lanes. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 768];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd3\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 32; i++)
		buf[i] = (sljit_u8)(200 + i);

	for (i = 32; i < 768; i++)
		buf[i] = 0xaa;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0xffff00 + 78);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_R2, 0);
	/* buf[48] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 48);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR3, SLJIT_IMM, 0xffff00 + 253);
	/* buf[64] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_SP), 3, SLJIT_IMM, 42);
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_MEM1(SLJIT_SP), 3);
	/* buf[80] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 80);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 15);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 0);
	/* buf[96] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 96);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV_S16, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 24);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR3, SLJIT_R1, 0);
	/* buf[112] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 112);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 10));
	/* buf[128] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 128);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 10000 + 20);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_MEM1(SLJIT_R0), -10000);
	/* buf[144] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 144);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV_S32, SLJIT_S3, 0, SLJIT_MEM1(SLJIT_S0), 28);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_S3, 0);
	/* buf[160] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 160);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV_U32, SLJIT_MEM1(SLJIT_SP), 4, SLJIT_MEM1(SLJIT_S0), 12);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_MEM1(SLJIT_SP), 4);
	/* buf[176] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 176);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 100000 - 24);
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_MEM1(SLJIT_R2), 100000);
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 192);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_MEM1(SLJIT_S0), 8);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR2, SLJIT_S1, 0);
	/* buf[208] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 208);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 3);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_MEM2(SLJIT_S0, SLJIT_R0), 3);
	/* buf[224] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 224);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_MEM0(), (sljit_sw)buf);
	/* buf[240] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 240);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 4);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_FR2, 0);
	/* buf[256] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 256);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 20);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR4, SLJIT_FR4, 0);
	/* buf[272] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 272);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_MEM1(SLJIT_SP), 4, SLJIT_MEM1(SLJIT_S0), 12);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_MEM1(SLJIT_SP), 4);
	/* buf[288] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 288);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_FR0, 0);
	/* buf[304] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 304);

	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_FR5, 0);
	/* buf[320] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 320);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 10000 + 8);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_MEM1(SLJIT_R2), -10000);
	/* buf[336] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 336);

	/* Test constant values. */
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_IMM, WCONST(0xff00123456, 0x123456));
	/* buf[352] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 352);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_IMM, 0xff0000);
	/* buf[368] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 368);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR2, SLJIT_IMM, 0x1ffff);
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 384);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR3, SLJIT_IMM, 0);
	/* buf[400] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 400);

	/* Test ARM constant values. */
	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_IMM, 0xff0034);
	/* buf[416] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 416);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_IMM, 0xff45ff);
	/* buf[432] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 432);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_IMM, 0xb3);
	/* buf[448] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 448);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_IMM, (sljit_sw)0xffff46ff);
	/* buf[464] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 464);

	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_IMM, 0x4c0000);
	/* buf[480] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 480);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR3, SLJIT_IMM, 0x71ffffff);
	/* buf[496] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 496);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR4, SLJIT_IMM, 0x9eff);
	/* buf[512] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 512);

	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_IMM, (sljit_sw)0xff070000);
	/* buf[528] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 528);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	supported[0] = sljit_emit_simd_replicate(compiler, type, SLJIT_FR2, SLJIT_IMM, 0xffff00 + 181) != SLJIT_ERR_UNSUPPORTED;
	/* buf[544] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 544);

	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_IMM, 0xffff00);
	/* buf[576] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 576);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_R1, 0, SLJIT_MEM1(SLJIT_S0), 30);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_R1, 0);
	/* buf[608] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 608);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 4);
	/* buf[640] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 640);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64;
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 4);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_S1), 2);
	/* buf[672] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 672);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 20);
	sljit_emit_simd_replicate(compiler, type, SLJIT_FR1, SLJIT_FR2, 0);
	/* buf[704] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 704);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_replicate(compiler, type, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 8));
	/* buf[736] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 736);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_replicate(buf + 48, 16, 1, 78), "test_simd3 case 1 failed\n");
	FAILED(!check_simd_replicate(buf + 64, 16, 1, 253), "test_simd3 case 2 failed\n");
	FAILED(!check_simd_replicate(buf + 80, 16, 1, 42), "test_simd3 case 3 failed\n");
	FAILED(!check_simd_replicate(buf + 96, 16, 1, 215), "test_simd3 case 4 failed\n");
	FAILED(!check_simd_replicate(buf + 112, 16, 2, 224), "test_simd3 case 5 failed\n");
	FAILED(!check_simd_replicate(buf + 128, 16, 2, 210), "test_simd3 case 6 failed\n");
	FAILED(!check_simd_replicate(buf + 144, 16, 2, 220), "test_simd3 case 7 failed\n");
	FAILED(!check_simd_replicate(buf + 160, 16, 4, 228), "test_simd3 case 8 failed\n");
	FAILED(!check_simd_replicate(buf + 176, 16, 4, 212), "test_simd3 case 9 failed\n");
	FAILED(!check_simd_replicate(buf + 192, 16, 4, 224), "test_simd3 case 10 failed\n");
#if IS_64BIT
	FAILED(!check_simd_replicate(buf + 208, 16, 8, 208), "test_simd3 case 11 failed\n");
	FAILED(!check_simd_replicate(buf + 224, 16, 8, 224), "test_simd3 case 12 failed\n");
	FAILED(!check_simd_replicate(buf + 240, 16, 8, 200), "test_simd3 case 13 failed\n");
#endif /* IS_64BIT */
	FAILED(!check_simd_replicate(buf + 256, 16, 4, 204), "test_simd3 case 14 failed\n");
	FAILED(!check_simd_replicate(buf + 272, 16, 4, 220), "test_simd3 case 15 failed\n");
	FAILED(!check_simd_replicate(buf + 288, 16, 4, 212), "test_simd3 case 16 failed\n");
	FAILED(!check_simd_replicate(buf + 304, 16, 8, 216), "test_simd3 case 17 failed\n");
	FAILED(!check_simd_replicate(buf + 320, 16, 8, 200), "test_simd3 case 18 failed\n");
	FAILED(!check_simd_replicate(buf + 336, 16, 8, 208), "test_simd3 case 19 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 352, 16, LITTLE_BIG(0x123456, 0x56341200)), "test_simd3 case 20 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 368, 16, 0), "test_simd3 case 21 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 384, 16, 0xffffffff), "test_simd3 case 22 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 400, 16, 0), "test_simd3 case 23 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 416, 16, LITTLE_BIG(0x340034, 0x34003400)), "test_simd3 case 24 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 432, 16, LITTLE_BIG(0x45ff45ff, 0xff45ff45)), "test_simd3 case 25 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 448, 16, LITTLE_BIG(0xb3, 0xb3000000)), "test_simd3 case 26 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 464, 16, LITTLE_BIG(0xffff46ff, 0xff46ffff)), "test_simd3 case 27 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 480, 16, LITTLE_BIG(0x4c0000, 0x4c00)), "test_simd3 case 28 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 496, 16, LITTLE_BIG(0x71ffffff, 0xffffff71)), "test_simd3 case 29 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 512, 16, LITTLE_BIG(0x9eff, 0xff9e0000)), "test_simd3 case 30 failed\n");
	FAILED(!check_simd_replicate_u32(buf + 528, 16, LITTLE_BIG(0xff070000, 0x07ff)), "test_simd3 case 31 failed\n");

	if (supported[0]) {
		FAILED(!check_simd_replicate(buf + 544, 32, 1, 181), "test_simd3 case 32 failed\n");
		FAILED(!check_simd_replicate(buf + 576, 32, 1, 0), "test_simd3 case 33 failed\n");
		FAILED(!check_simd_replicate(buf + 608, 32, 2, 230), "test_simd3 case 34 failed\n");
		FAILED(!check_simd_replicate(buf + 640, 32, 4, 204), "test_simd3 case 35 failed\n");
#if IS_64BIT
		FAILED(!check_simd_replicate(buf + 672, 32, 8, 216), "test_simd3 case 36 failed\n");
#endif /* IS_64BIT */
		FAILED(!check_simd_replicate(buf + 704, 32, 4, 220), "test_simd3 case 37 failed\n");
		FAILED(!check_simd_replicate(buf + 736, 32, 8, 208), "test_simd3 case 38 failed\n");
	}

	successful_tests++;
}

static void test_simd4(void)
{
	/* Test simd replicate lane to all lanes. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 992];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd4\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 32; i++)
		buf[i] = (sljit_u8)(100 + i);

	for (i = 32; i < 992; i++)
		buf[i] = 0xaa;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR0, 0);
	/* buf[48] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 48);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR2, SLJIT_FR1, 12);
	/* buf[64] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR3, SLJIT_FR5, 6);
	/* buf[80] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 80);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR3, SLJIT_FR3, 9);
	/* buf[96] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 96);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, fs0, SLJIT_FR0, 10);
	/* buf[112] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 112);
	/* buf[128] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 128);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR0, 0);
	/* buf[144] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 144);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 3);
	/* buf[160] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 160);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR4, 5);
	/* buf[176] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 176);
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 192);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR3, SLJIT_FR3, 0);
	/* buf[208] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 208);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 2);
	/* buf[224] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 224);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR5, SLJIT_FR2, 3);
	/* buf[240] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 240);
	/* buf[256] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 256);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 0);
	/* buf[272] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 272);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR0, 1);
	/* buf[288] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 288);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, fs0, SLJIT_FR3, 1);
	/* buf[304] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 304);
	/* buf[320] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 320);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR2, SLJIT_FR2, 0);
	/* buf[336] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 336);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR0, 3);
	/* buf[352] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 352);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR5, SLJIT_FR0, 1);
	/* buf[368] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 368);
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 384);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR4, SLJIT_FR4, 0);
	/* buf[400] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 400);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 1);
	/* buf[416] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 416);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 16);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR2, 1);
	/* buf[432] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 432);
	/* buf[448] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 448);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	supported[0] = sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR2, SLJIT_FR2, 0) != SLJIT_ERR_UNSUPPORTED;
	/* buf[480] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 480);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR4, 13);
	/* buf[512] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 512);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR4, 6);
	/* buf[544] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 544);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR4, SLJIT_FR4, 28);
	/* buf[576] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 576);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR2, SLJIT_FR1, 0);
	/* buf[608] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 608);

	sljit_emit_simd_lane_replicate(compiler, type, fs0, SLJIT_FR1, 2);
	/* buf[640] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 640);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 13);
	/* buf[672] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 672);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR5, 0);
	/* buf[704] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 704);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR5, SLJIT_FR5, 5);
	/* buf[736] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 736);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, fs0, SLJIT_FR0, 0);
	/* buf[768] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 768);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR0, SLJIT_FR0, 1);
	/* buf[800] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 800);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR2, SLJIT_FR1, 0);
	/* buf[832] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 832);

	sljit_emit_simd_lane_replicate(compiler, type, fs0, SLJIT_FR1, 1);
	/* buf[864] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 864);

	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, SLJIT_FR1, 4);
	/* buf[896] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 896);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_replicate(compiler, type, SLJIT_FR1, fs0, 0);
	/* buf[928] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 928);

	sljit_emit_simd_lane_replicate(compiler, type, fs0, fs0, 2);
	/* buf[960] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 960);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_replicate(buf + 48, 16, 1, 100), "test_simd4 case 1 failed\n");
	FAILED(!check_simd_replicate(buf + 64, 16, 1, 128), "test_simd4 case 2 failed\n");
	FAILED(!check_simd_replicate(buf + 80, 16, 1, 106), "test_simd4 case 3 failed\n");
	FAILED(!check_simd_replicate(buf + 96, 16, 1, 125), "test_simd4 case 4 failed\n");
	FAILED(!check_simd_replicate(buf + 112, 16, 1, 110), "test_simd4 case 5 failed\n");
	FAILED(!check_simd_replicate(buf + 128, 16, 16, 100), "test_simd4 case 6 failed\n");
	FAILED(!check_simd_replicate(buf + 144, 16, 2, 100), "test_simd4 case 7 failed\n");
	FAILED(!check_simd_replicate(buf + 160, 16, 2, 122), "test_simd4 case 8 failed\n");
	FAILED(!check_simd_replicate(buf + 176, 16, 2, 126), "test_simd4 case 9 failed\n");
	FAILED(!check_simd_replicate(buf + 192, 16, 16, 116), "test_simd4 case 10 failed\n");
	FAILED(!check_simd_replicate(buf + 208, 16, 4, 100), "test_simd4 case 11 failed\n");
	FAILED(!check_simd_replicate(buf + 224, 16, 4, 124), "test_simd4 case 12 failed\n");
	FAILED(!check_simd_replicate(buf + 240, 16, 4, 112), "test_simd4 case 13 failed\n");
	FAILED(!check_simd_replicate(buf + 256, 16, 16, 100), "test_simd4 case 14 failed\n");
	FAILED(!check_simd_replicate(buf + 272, 16, 8, 116), "test_simd4 case 15 failed\n");
	FAILED(!check_simd_replicate(buf + 288, 16, 8, 108), "test_simd4 case 16 failed\n");
	FAILED(!check_simd_replicate(buf + 304, 16, 8, 124), "test_simd4 case 17 failed\n");
	FAILED(!check_simd_replicate(buf + 320, 16, 16, 116), "test_simd4 case 18 failed\n");
	FAILED(!check_simd_replicate(buf + 336, 16, 4, 100), "test_simd4 case 19 failed\n");
	FAILED(!check_simd_replicate(buf + 352, 16, 4, 128), "test_simd4 case 20 failed\n");
	FAILED(!check_simd_replicate(buf + 368, 16, 4, 104), "test_simd4 case 21 failed\n");
	FAILED(!check_simd_replicate(buf + 384, 16, 16, 100), "test_simd4 case 22 failed\n");
	FAILED(!check_simd_replicate(buf + 400, 16, 8, 116), "test_simd4 case 23 failed\n");
	FAILED(!check_simd_replicate(buf + 416, 16, 8, 108), "test_simd4 case 24 failed\n");
	FAILED(!check_simd_replicate(buf + 432, 16, 8, 124), "test_simd4 case 25 failed\n");
	FAILED(!check_simd_replicate(buf + 448, 16, 16, 116), "test_simd4 case 26 failed\n");

	if (supported[0]) {
		FAILED(!check_simd_replicate(buf + 480, 32, 1, 100), "test_simd4 case 27 failed\n");
		FAILED(!check_simd_replicate(buf + 512, 32, 1, 113), "test_simd4 case 28 failed\n");
		FAILED(!check_simd_replicate(buf + 544, 32, 1, 106), "test_simd4 case 29 failed\n");
		FAILED(!check_simd_replicate(buf + 576, 32, 1, 128), "test_simd4 case 30 failed\n");
		FAILED(!check_simd_replicate(buf + 608, 32, 2, 100), "test_simd4 case 31 failed\n");
		FAILED(!check_simd_replicate(buf + 640, 32, 2, 104), "test_simd4 case 32 failed\n");
		FAILED(!check_simd_replicate(buf + 672, 32, 2, 126), "test_simd4 case 33 failed\n");
		FAILED(!check_simd_replicate(buf + 704, 32, 4, 100), "test_simd4 case 34 failed\n");
		FAILED(!check_simd_replicate(buf + 736, 32, 4, 120), "test_simd4 case 35 failed\n");
		FAILED(!check_simd_replicate(buf + 768, 32, 8, 100), "test_simd4 case 36 failed\n");
		FAILED(!check_simd_replicate(buf + 800, 32, 8, 108), "test_simd4 case 37 failed\n");
		FAILED(!check_simd_replicate(buf + 832, 32, 4, 100), "test_simd4 case 38 failed\n");
		FAILED(!check_simd_replicate(buf + 864, 32, 4, 104), "test_simd4 case 39 failed\n");
		FAILED(!check_simd_replicate(buf + 896, 32, 4, 116), "test_simd4 case 40 failed\n");
		FAILED(!check_simd_replicate(buf + 928, 32, 8, 100), "test_simd4 case 41 failed\n");
		FAILED(!check_simd_replicate(buf + 960, 32, 8, 116), "test_simd4 case 42 failed\n");
	}

	successful_tests++;
}

static sljit_s32 check_simd_lane_mov_zero(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_s32 start, sljit_s32 value)
{
	sljit_s32 i;

	for (i = 0; i < start; i++)
		if (*buf++ != 0)
			return 0;

	for (i = 0; i < elem_size; i++)
		if (*buf++ != value++)
			return 0;

	for (i = start + elem_size; i < length; i++)
		if (*buf++ != 0)
			return 0;

	return 1;
}

static void test_simd5(void)
{
	/* Test simd zero register before move to lane. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 672];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd5\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 64; i++)
		buf[i] = (sljit_u8)(100 + i);

	for (i = 64; i < 672; i++)
		buf[i] = 0xaa;

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);
	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 100000);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 10000);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 0xffff00 + 85);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR0, 0, SLJIT_R2, 0);
	/* buf[64] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, fs0, 0, SLJIT_IMM, 0xffff00 + 18);
	/* buf[80] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 80);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV_U8, SLJIT_MEM1(SLJIT_SP), 10, SLJIT_IMM, 170);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | SLJIT_32 | type, SLJIT_FR5, 5, SLJIT_MEM1(SLJIT_SP), 10);
	/* buf[96] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 96);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, LITTLE_BIG(0x789a6d6c, 0x789a6c6d));
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR1, 0, SLJIT_S2, 0);
	/* buf[112] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 112);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | SLJIT_32 | type, SLJIT_FR4, 0, SLJIT_IMM, LITTLE_BIG(0xff8382, 0xff8283));
	/* buf[128] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 128);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, fs0, 3, SLJIT_MEM1(SLJIT_R0), 100004);
	/* buf[144] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 144);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_R2, 0, SLJIT_MEM1(SLJIT_S0), 4);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | SLJIT_32 | type, SLJIT_FR2, 0, SLJIT_R2, 0);
	/* buf[160] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 160);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR5, 0, SLJIT_IMM, LITTLE_BIG(0x29282726, 0x26272829));
	/* buf[176] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 176);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 3);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | SLJIT_32 | type, SLJIT_FR1, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R2), 2);
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 192);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR3, 3, SLJIT_MEM1(SLJIT_R1), -10000 + 8);
	/* buf[208] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 208);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, fs0, 0, SLJIT_S2, 0);
	/* buf[224] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 224);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR4, 0,
		SLJIT_IMM, LITTLE_BIG(WCONST(0xe3e2e1e0dfdedddc, 0), WCONST(0xdcdddedfe0e1e2e3, 0)));
	/* buf[240] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 240);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR3, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R2), 0);
	/* buf[256] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 256);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR1, 1, SLJIT_MEM1(SLJIT_R0), 100000);
	/* buf[272] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 272);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR0, 0, SLJIT_MEM1(SLJIT_S0), 12);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR0, 0, SLJIT_FR0, 0);
	/* buf[288] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 288);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR5, 0, SLJIT_MEM1(SLJIT_S0), 4);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR2, 0, SLJIT_FR5, 0);
	/* buf[304] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 304);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 1);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR1, 0, SLJIT_MEM2(SLJIT_S0, SLJIT_R2), 3);
	/* buf[320] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 320);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR4, 1, SLJIT_FR4, 0);
	/* buf[336] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 336);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR2, 0, SLJIT_FR2, 0);
	/* buf[352] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 352);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR4, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR3, 0, SLJIT_FR4, 0);
	/* buf[368] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 368);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR1, 0, SLJIT_MEM0(), (sljit_sw)(buf + 8));
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 384);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR2, 1, SLJIT_FR2, 0);
	/* buf[400] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 400);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 215);
	supported[0] = sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR0, 0, SLJIT_R2, 0) != SLJIT_ERR_UNSUPPORTED;
	/* buf[416] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 416);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, fs0, 17, SLJIT_IMM, 78);
	/* buf[448] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 448);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 0xff3433);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR1, 4, SLJIT_S1, 0);
	/* buf[480] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 480);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR5, 5, SLJIT_MEM1(SLJIT_S0), 60);
	/* buf[512] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 512);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR4, 3, SLJIT_MEM0(), (sljit_sw)buf + 32);
	/* buf[544] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 544);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR2, 0, SLJIT_MEM1(SLJIT_S0), 48);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR2, 3, SLJIT_FR2, 0);
	/* buf[576] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 576);

	sljit_emit_fop1(compiler, SLJIT_MOV_F32, SLJIT_FR3, 0, SLJIT_MEM1(SLJIT_S0), 8);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR3, 6, SLJIT_FR3, 0);
	/* buf[608] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 608);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_fop1(compiler, SLJIT_MOV_F64, SLJIT_MEM1(SLJIT_SP), 8, SLJIT_MEM1(SLJIT_S0), 40);
	sljit_emit_simd_lane_mov(compiler, SLJIT_SIMD_LANE_ZERO | type, SLJIT_FR0, 3, SLJIT_MEM1(SLJIT_SP), 8);
	/* buf[640] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 640);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_lane_mov_zero(buf + 64, 16, 1, 0, 85), "test_simd5 case 1 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 80, 16, 1, 0, 18), "test_simd5 case 2 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 96, 16, 1, 5, 170), "test_simd5 case 3 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 112, 16, 2, 0, 108), "test_simd5 case 4 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 128, 16, 2, 0, 130), "test_simd5 case 5 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 144, 16, 2, 6, 104), "test_simd5 case 6 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 160, 16, 4, 0, 104), "test_simd5 case 7 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 176, 16, 4, 0, 38), "test_simd5 case 8 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 192, 16, 4, 0, 112), "test_simd5 case 9 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 208, 16, 4, 12, 108), "test_simd5 case 10 failed\n");
#if IS_64BIT
	FAILED(!check_simd_lane_mov_zero(buf + 224, 16, 8, 0, 100), "test_simd5 case 11 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 240, 16, 8, 0, 220), "test_simd5 case 12 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 256, 16, 8, 0, 108), "test_simd5 case 13 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 272, 16, 8, 8, 100), "test_simd5 case 14 failed\n");
#endif /* IS_64BIT */
	FAILED(!check_simd_lane_mov_zero(buf + 288, 16, 4, 0, 112), "test_simd5 case 15 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 304, 16, 4, 0, 104), "test_simd5 case 16 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 320, 16, 4, 0, 108), "test_simd5 case 17 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 336, 16, 4, 4, 100), "test_simd5 case 18 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 352, 16, 8, 0, 108), "test_simd5 case 19 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 368, 16, 8, 0, 100), "test_simd5 case 20 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 384, 16, 8, 0, 108), "test_simd5 case 21 failed\n");
	FAILED(!check_simd_lane_mov_zero(buf + 400, 16, 8, 8, 100), "test_simd5 case 22 failed\n");

	if (supported[0]) {
		FAILED(!check_simd_lane_mov_zero(buf + 416, 32, 1, 0, 215), "test_simd5 case 23 failed\n");
		FAILED(!check_simd_lane_mov_zero(buf + 448, 32, 1, 17, 78), "test_simd5 case 24 failed\n");
		FAILED(!check_simd_lane_mov_zero(buf + 480, 32, 2, 8, 51), "test_simd5 case 25 failed\n");
		FAILED(!check_simd_lane_mov_zero(buf + 512, 32, 4, 20, 160), "test_simd5 case 26 failed\n");
#if IS_64BIT
		FAILED(!check_simd_lane_mov_zero(buf + 544, 32, 8, 24, 132), "test_simd5 case 27 failed\n");
#endif /* IS_64BIT */
		FAILED(!check_simd_lane_mov_zero(buf + 576, 32, 4, 12, 148), "test_simd5 case 28 failed\n");
		FAILED(!check_simd_lane_mov_zero(buf + 608, 32, 4, 24, 108), "test_simd5 case 29 failed\n");
		FAILED(!check_simd_lane_mov_zero(buf + 640, 32, 8, 24, 140), "test_simd5 case 30 failed\n");
	}

	successful_tests++;
}

static void init_simd_extend(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_s32 is_float, sljit_s32 data)
{
	sljit_u8* end = buf + length;

	do {
		if (elem_size == 1)
			*buf = (sljit_u8)data;
		else if (elem_size == 2)
			*(sljit_u16*)buf = (sljit_u16)data;
		else if (!is_float)
			*(sljit_u32*)buf = (sljit_u32)data;
		else
			*(sljit_f32*)buf = (sljit_f32)data;

		buf += elem_size;
		data++;
	} while (buf < end);
}

static sljit_s32 check_simd_extend_unsigned(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_u32 mask)
{
	sljit_s32 data;
	sljit_u8* end = buf + length;

	if (elem_size == 2)
		data = -(length >> 2);
	else if (elem_size == 4)
		data = -(length >> 3);
	else
		data = -(length >> 4);

	do {
		if (elem_size == 2) {
			if (*(sljit_u16*)buf != ((sljit_u16)data & mask))
				return 0;
		} else if (elem_size == 4) {
			if (*(sljit_u32*)buf != ((sljit_u32)data & mask))
				return 0;
		} else {
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
			if (*(sljit_u32*)buf != ((sljit_u32)data & mask) || *(sljit_u32*)(buf + 4) != 0)
				return 0;
#else /* !SLJIT_LITTLE_ENDIAN */
			if (*(sljit_u32*)(buf + 4) != ((sljit_u32)data & mask) || *(sljit_u32*)buf != 0)
				return 0;
#endif /* SLJIT_LITTLE_ENDIAN */
		}

		buf += elem_size;
		data++;
	} while (buf < end);

	return 1;
}

static sljit_s32 check_simd_extend_signed(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_s32 is_float)
{
	sljit_s32 data;
	sljit_u8* end = buf + length;

	if (elem_size == 2)
		data = -(length >> 2);
	else if (elem_size == 4)
		data = -(length >> 3);
	else if (!is_float)
		data = -(length >> 4);
	else
		data = 1000;

	do {
		if (elem_size == 2) {
			if (*(sljit_s16*)buf != data)
				return 0;
		} else if (elem_size == 4) {
			if (*(sljit_s32*)buf != data)
				return 0;
		} else if (!is_float) {
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
			if (*(sljit_s32*)buf != data)
				return 0;
			if (*(sljit_s32*)(buf + 4) != (data >> 31))
				return 0;
#else /* !SLJIT_LITTLE_ENDIAN */
			if (*(sljit_s32*)(buf + 4) != data)
				return 0;
			if (*(sljit_s32*)buf != (data >> 31))
				return 0;
#endif /* SLJIT_LITTLE_ENDIAN */
		} else {
			if (*(sljit_f64*)buf != (sljit_f64)data)
				return 0;
		}

		buf += elem_size;
		data++;
	} while (buf < end);

	return 1;
}

static void test_simd6(void)
{
	/* Test simd extension operation. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 1088];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd6\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 1088; i++)
		buf[i] = 0xaa;

	init_simd_extend(buf + 0, 16, 1, 0, -8);
	init_simd_extend(buf + 32, 16, 2, 0, -4);
	init_simd_extend(buf + 64, 16, 4, 0, -2);
	init_simd_extend(buf + 96, 16, 4, 1, 1000);
	init_simd_extend(buf + 128, 8, 1, 0, -4);
	init_simd_extend(buf + 160, 8, 2, 0, -2);
	init_simd_extend(buf + 192, 8, 4, 0, -1);
	init_simd_extend(buf + 224, 8, 4, 1, 1000);
	init_simd_extend(buf + 256, 4, 1, 0, -2);
	init_simd_extend(buf + 288, 4, 2, 0, -1);
	init_simd_extend(buf + 320, 2, 1, 0, -1);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 32);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 128);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR2, SLJIT_FR0, 0);
	/* buf[352] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 352);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR1, SLJIT_FR0, 0);
	/* buf[368] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 368);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_S0, 0, SLJIT_IMM, 128);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR0, SLJIT_MEM1(SLJIT_R1), 0);
	/* buf[384] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 384);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 128);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, fs0, SLJIT_MEM1(SLJIT_SP), 0);
	/* buf[400] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 400);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 160);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR4, SLJIT_FR4, 0);
	/* buf[416] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 416);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 160);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR0, SLJIT_FR4, 0);
	/* buf[432] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 432);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 40);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR1, SLJIT_MEM2(SLJIT_S0, SLJIT_R2), 2);
	/* buf[448] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 448);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 160));
	/* buf[464] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 464);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 192);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR0, SLJIT_FR2, 0);
	/* buf[480] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 480);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR3, SLJIT_FR2, 0);
	/* buf[496] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 496);

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 10000 - 192);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR2, SLJIT_MEM1(SLJIT_R0), 10000);
	/* buf[512] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 512);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R0, 0, SLJIT_S0, 0, SLJIT_IMM, 100000 + 192);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, fs0, SLJIT_MEM1(SLJIT_R0), -100000);
	/* buf[528] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 528);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 224);
	sljit_emit_simd_extend(compiler, type, fs0, fs0, 0);
	/* buf[544] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 544);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 224);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_SP), 0);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR3, SLJIT_MEM1(SLJIT_SP), 0);
	/* buf[560] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 560);

	sljit_emit_simd_extend(compiler, type, SLJIT_FR5, SLJIT_FR1, 0);
	/* buf[576] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR5, SLJIT_MEM1(SLJIT_S0), 576);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 256);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR0, SLJIT_FR2, 0);
	/* buf[592] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 592);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR2, SLJIT_FR2, 0);
	/* buf[608] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 608);

	sljit_emit_simd_extend(compiler, type, fs0, SLJIT_MEM1(SLJIT_S0), 256);
	/* buf[624] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 624);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR4, SLJIT_MEM0(), (sljit_sw)(buf + 256));
	/* buf[640] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 640);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 320);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR0, fs0, 0);
	/* buf[656] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 656);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR0, fs0, 0);
	/* buf[672] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 672);

	sljit_emit_op1(compiler, SLJIT_MOV_U16, SLJIT_MEM1(SLJIT_SP), 0, SLJIT_MEM1(SLJIT_S0), 320);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR3, SLJIT_MEM1(SLJIT_SP), 0);
	/* buf[688] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 688);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S2, 0, SLJIT_IMM, 320);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR3, SLJIT_MEM2(SLJIT_S0, SLJIT_S2), 0);
	/* buf[704] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 704);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 288);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR2, SLJIT_FR0, 0);
	/* buf[720] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 720);

	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR0, SLJIT_FR0, 0);
	/* buf[736] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 736);

	sljit_emit_op2(compiler, SLJIT_SUB, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 100000 - 288);
	sljit_emit_simd_extend(compiler, type, fs0, SLJIT_MEM1(SLJIT_R2), 100000);
	/* buf[752] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 752);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S2, 0, SLJIT_S0, 0, SLJIT_IMM, 10000 + 288);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR1, SLJIT_MEM1(SLJIT_S2), -10000);
	/* buf[768] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 768);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 256);
	sljit_emit_simd_extend(compiler, type, fs0, SLJIT_FR1, 0);
	/* buf[784] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 784);

	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 256);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR2, SLJIT_MEM1(SLJIT_R2), 0);
	/* buf[792] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 792);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_simd_extend(compiler, type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 320);
	/* buf[800] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 800);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 320);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR2, fs0, 0);
	/* buf[808] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 808);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 288);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR2, SLJIT_FR1, 0);
	/* buf[816] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 816);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_S1, 0, SLJIT_IMM, 288);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, fs0, SLJIT_MEM2(SLJIT_S1, SLJIT_S0), 0);
	/* buf[824] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 824);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	supported[0] = sljit_emit_simd_extend(compiler, type, SLJIT_FR4, SLJIT_FR1, 0) != SLJIT_ERR_UNSUPPORTED;
	/* buf[832] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 832);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 128);
	/* buf[864] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 864);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_extend(compiler, type, fs0, SLJIT_MEM0(), (sljit_sw)(buf + 256));
	/* buf[896] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 896);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_EXTEND_32;
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_S0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_IMM, 16);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR0, SLJIT_MEM2(SLJIT_R1, SLJIT_R2), 1);
	/* buf[928] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 928);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_S1, 0, SLJIT_S0, 0, SLJIT_IMM, 100000 + 160);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S1), -100000);
	/* buf[960] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 960);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_EXTEND_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_extend(compiler, type | SLJIT_SIMD_EXTEND_SIGNED, SLJIT_FR0, fs0, 0);
	/* buf[992] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 992);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_EXTEND_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 96);
	sljit_emit_simd_extend(compiler, type, SLJIT_FR2, SLJIT_FR2, 0);
	/* buf[1024] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 1024);

	sljit_emit_simd_extend(compiler, type, SLJIT_FR4, SLJIT_MEM0(), (sljit_sw)(buf + 96));
	/* buf[1056] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 1056);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_extend_unsigned(buf + 352, 16, 2, 0xff), "test_simd6 case 1 failed\n");
	FAILED(!check_simd_extend_signed(buf + 368, 16, 2, 0), "test_simd6 case 2 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 384, 16, 2, 0xff), "test_simd6 case 3 failed\n");
	FAILED(!check_simd_extend_signed(buf + 400, 16, 2, 0), "test_simd6 case 4 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 416, 16, 4, 0xffff), "test_simd6 case 5 failed\n");
	FAILED(!check_simd_extend_signed(buf + 432, 16, 4, 0), "test_simd6 case 6 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 448, 16, 4, 0xffff), "test_simd6 case 7 failed\n");
	FAILED(!check_simd_extend_signed(buf + 464, 16, 4, 0), "test_simd6 case 8 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 480, 16, 8, 0xffffffff), "test_simd6 case 9 failed\n");
	FAILED(!check_simd_extend_signed(buf + 496, 16, 8, 0), "test_simd6 case 10 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 512, 16, 8, 0xffffffff), "test_simd6 case 11 failed\n");
	FAILED(!check_simd_extend_signed(buf + 528, 16, 8, 0), "test_simd6 case 12 failed\n");
	FAILED(!check_simd_extend_signed(buf + 544, 16, 8, 1), "test_simd6 case 13 failed\n");
	FAILED(!check_simd_extend_signed(buf + 560, 16, 8, 1), "test_simd6 case 14 failed\n");
	FAILED(!check_simd_extend_signed(buf + 576, 16, 8, 1), "test_simd6 case 15 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 592, 16, 4, 0xff), "test_simd6 case 16 failed\n");
	FAILED(!check_simd_extend_signed(buf + 608, 16, 4, 0), "test_simd6 case 17 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 624, 16, 4, 0xff), "test_simd6 case 18 failed\n");
	FAILED(!check_simd_extend_signed(buf + 640, 16, 4, 0), "test_simd6 case 19 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 656, 16, 8, 0xff), "test_simd6 case 20 failed\n");
	FAILED(!check_simd_extend_signed(buf + 672, 16, 8, 0), "test_simd6 case 21 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 688, 16, 8, 0xff), "test_simd6 case 22 failed\n");
	FAILED(!check_simd_extend_signed(buf + 704, 16, 8, 0), "test_simd6 case 23 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 720, 16, 8, 0xffff), "test_simd6 case 24 failed\n");
	FAILED(!check_simd_extend_signed(buf + 736, 16, 8, 0), "test_simd6 case 25 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 752, 16, 8, 0xffff), "test_simd6 case 26 failed\n");
	FAILED(!check_simd_extend_signed(buf + 768, 16, 8, 0), "test_simd6 case 27 failed\n");

#if IS_ARM
	FAILED(!check_simd_extend_unsigned(buf + 784, 8, 2, 0xff), "test_simd6 case 28 failed\n");
	FAILED(!check_simd_extend_signed(buf + 792, 8, 2, 0), "test_simd6 case 29 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 800, 8, 4, 0xff), "test_simd6 case 30 failed\n");
	FAILED(!check_simd_extend_signed(buf + 808, 8, 4, 0), "test_simd6 case 31 failed\n");
	FAILED(!check_simd_extend_unsigned(buf + 816, 8, 4, 0xffff), "test_simd6 case 32 failed\n");
	FAILED(!check_simd_extend_signed(buf + 824, 8, 4, 0), "test_simd6 case 33 failed\n");
#endif /* IS_ARM */

	if (supported[0]) {
		FAILED(!check_simd_extend_unsigned(buf + 832, 32, 2, 0xff), "test_simd6 case 34 failed\n");
		FAILED(!check_simd_extend_signed(buf + 864, 32, 4, 0), "test_simd6 case 35 failed\n");
		FAILED(!check_simd_extend_unsigned(buf + 896, 32, 8, 0xff), "test_simd6 case 36 failed\n");
		FAILED(!check_simd_extend_signed(buf + 928, 32, 4, 0), "test_simd6 case 37 failed\n");
		FAILED(!check_simd_extend_unsigned(buf + 960, 32, 8, 0xffff), "test_simd6 case 38 failed\n");
		FAILED(!check_simd_extend_signed(buf + 992, 32, 8, 0), "test_simd6 case 39 failed\n");
		FAILED(!check_simd_extend_signed(buf + 1024, 32, 8, 1), "test_simd6 case 40 failed\n");
		FAILED(!check_simd_extend_signed(buf + 1056, 32, 8, 1), "test_simd6 case 41 failed\n");
	}

	successful_tests++;
}

static void init_simd_sign(sljit_u8* buf, sljit_s32 length, sljit_s32 elem_size, sljit_u32 data)
{
#if (defined SLJIT_LITTLE_ENDIAN && SLJIT_LITTLE_ENDIAN)
	sljit_u8* end = buf + length;

	do {
		if (elem_size == 1)
			*buf = (sljit_u8)(((data & 0x1) << 7) + 0x7f);
		else if (elem_size == 2)
			*(sljit_u16*)buf = (sljit_u16)(((data & 0x1) << 15) + 0x7fff);
		else if (elem_size == 4)
			*(sljit_u32*)buf = (sljit_u32)(((data & 0x1) << 31) + 0x7fffffff);
		else {
			*(sljit_u32*)buf = 0xffffffff;
			*(sljit_u32*)(buf + 4) = (sljit_u32)(((data & 0x1) << 31) + 0x7fffffff);
		}

		data >>= 1;
		buf += elem_size;
	} while (buf < end);
#else /* !SLJIT_LITTLE_ENDIAN */
	sljit_u8* current = buf + length - elem_size;

	do {
		if (elem_size == 1)
			*current = (sljit_u8)(((data & 0x1) << 7) + 0x7f);
		else if (elem_size == 2)
			*(sljit_u16*)current = (sljit_u16)(((data & 0x1) << 15) + 0x7fff);
		else if (elem_size == 4)
			*(sljit_u32*)current = (sljit_u32)(((data & 0x1) << 31) + 0x7fffffff);
		else {
			*(sljit_u32*)(current + 4) = 0xffffffff;
			*(sljit_u32*)current = (sljit_u32)(((data & 0x1) << 31) + 0x7fffffff);
		}

		data >>= 1;
		current -= elem_size;
	} while (current >= buf);
#endif /* SLJIT_LITTLE_ENDIAN */
}

static void test_simd7(void)
{
	/* Test simd sign extraction operation. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 288];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS0 : SLJIT_FR5;
	sljit_uw resw[9];
	sljit_u32 res32[7];

	if (verbose)
		printf("Run test_simd7\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 9; i++)
		resw[i] = (sljit_uw)-1;
	for (i = 0; i < 7; i++)
		res32[i] = (sljit_u32)-1;

	init_simd_sign(buf + 0, 16, 1, 0x8fa3);
	init_simd_sign(buf + 16, 16, 1, 0x34d5);
	init_simd_sign(buf + 32, 16, 2, 0xa6);
	init_simd_sign(buf + 48, 16, 2, 0x5e);
	init_simd_sign(buf + 64, 16, 4, 0xd);
	init_simd_sign(buf + 80, 16, 4, 0x5);
	init_simd_sign(buf + 96, 16, 8, 0x2);
	init_simd_sign(buf + 112, 16, 8, 0x1);

	init_simd_sign(buf + 128, 8, 1, 0x45);
	init_simd_sign(buf + 136, 8, 2, 0x9);
	init_simd_sign(buf + 144, 8, 4, 0x1);

	init_simd_sign(buf + 160, 32, 1, 0x51e83b71);
	init_simd_sign(buf + 192, 32, 2, 0xc90d);
	init_simd_sign(buf + 224, 32, 4, 0xa5);
	init_simd_sign(buf + 256, 32, 8, 0x9);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_R0, 0);
	/* resw[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 0, SLJIT_R0, 0);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 16);
	/* resw[1] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S1), sizeof(sljit_uw));

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_R2, 0);
	/* resw[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 2 * sizeof(sljit_uw), SLJIT_R2, 0);

	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 48);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, SLJIT_FR4, SLJIT_MEM1(SLJIT_SP), 4);
	/* res32[0] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S2), 0, SLJIT_MEM1(SLJIT_SP), 4);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 64);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, fs0, SLJIT_R1, 0);
	/* res32[1] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S2), sizeof(sljit_u32), SLJIT_R1, 0);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 80);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 4);
	/* res32[2] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, SLJIT_FR1, SLJIT_MEM2(SLJIT_S2, SLJIT_R1), 1);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 96);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_S3, 0);
	/* resw[3] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 3 * sizeof(sljit_uw), SLJIT_S3, 0);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 112);
	/* resw[4] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM0(), (sljit_sw)(resw + 4));

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 128);
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_R0, 0);
	/* resw[5] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S1), 5 * sizeof(sljit_uw), SLJIT_R0, 0);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 136);
	/* res32[4] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, SLJIT_FR0, SLJIT_MEM1(SLJIT_S2), 4 * sizeof(sljit_u32));

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 144);
	/* resw[6] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S1), 6 * sizeof(sljit_uw));

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	supported[0] = sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 160) != SLJIT_ERR_UNSUPPORTED;
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, SLJIT_FR2, SLJIT_R2, 0);
	/* res32[5] */
	sljit_emit_op1(compiler, SLJIT_MOV32, SLJIT_MEM1(SLJIT_S2), 5 * sizeof(sljit_u32), SLJIT_R2, 0);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 192);
	/* resw[7] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S1), 7 * sizeof(sljit_uw));

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32;
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S1, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, sizeof(sljit_uw));
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 224);
	/* resw[8] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM2(SLJIT_R2, SLJIT_R1), 3);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 256);
	/* res32[6] */
	sljit_emit_simd_sign(compiler, SLJIT_SIMD_STORE | type | SLJIT_32, SLJIT_FR0, SLJIT_MEM0(), (sljit_sw)(res32 + 6));

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func3((sljit_sw)buf, (sljit_sw)resw, (sljit_sw)res32);
	sljit_free_code(code.code, NULL);

	FAILED(resw[0] != 0x8fa3, "test_simd7 case 1 failed\n");
	FAILED(resw[1] != 0x34d5, "test_simd7 case 2 failed\n");
	FAILED(resw[2] != 0xa6, "test_simd7 case 3 failed\n");
	FAILED(res32[0] != 0x5e, "test_simd7 case 4 failed\n");
	FAILED(res32[1] != 0xd, "test_simd7 case 5 failed\n");
	FAILED(res32[2] != 0x5, "test_simd7 case 6 failed\n");
	FAILED(res32[3] != (sljit_u32)-1, "test_simd7 case 7 failed\n");
	FAILED(resw[3] != 0x2, "test_simd7 case 8 failed\n");
	FAILED(resw[4] != 0x1, "test_simd7 case 9 failed\n");
#if IS_ARM
	FAILED(resw[5] != 0x45, "test_simd7 case 10 failed\n");
	FAILED(res32[4] != 0x9, "test_simd7 case 11 failed\n");
	FAILED(resw[6] != 0x1, "test_simd7 case 12 failed\n");
#endif /* IS_ARM */

	if (supported[0]) {
		FAILED(res32[5] != 0x51e83b71, "test_simd7 case 13 failed\n");
		FAILED(resw[7] != 0xc90d, "test_simd7 case 14 failed\n");
		FAILED(resw[8] != 0xa5, "test_simd7 case 15 failed\n");
		FAILED(res32[6] != 0x9, "test_simd7 case 16 failed\n");
	}

	successful_tests++;
}

static void init_simd_u32(sljit_u8* buf, sljit_s32 length, sljit_u32 data)
{
	sljit_u32* current = (sljit_u32*)buf;
	sljit_u32* end = (sljit_u32*)(buf + length);

	while (current < end)
		*current++ = data;
}

static sljit_s32 check_simd_u32(sljit_u8* buf, sljit_s32 length, sljit_u32 data)
{
	sljit_u32* current = (sljit_u32*)buf;
	sljit_u32* end = (sljit_u32*)(buf + length);

	while (current < end) {
		if (*current++ != data)
			return 0;
	}

	return 1;
}

static void test_simd8(void)
{
	/* Test simd binary logical operation. */
	executable_code code;
	struct sljit_compiler* compiler;
	sljit_s32 i, type;
	sljit_u8 supported[1];
	sljit_u8* buf;
	sljit_u8 data[63 + 1024];
	sljit_s32 fs0 = SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? SLJIT_FS1 : SLJIT_FR5;

	if (verbose)
		printf("Run test_simd8\n");

	/* Buffer is 64 byte aligned. */
	buf = (sljit_u8*)(((sljit_sw)data + (sljit_sw)63) & ~(sljit_sw)63);

	for (i = 0; i < 1024; i++)
		buf[i] = 0xaa;

	init_simd_u32(buf, 32, 0x00ff00ff);
	init_simd_u32(buf + 32, 32, 0x0000ffff);

	compiler = sljit_create_compiler(NULL, NULL);
	FAILED(!compiler, "cannot create compiler\n");

	sljit_emit_enter(compiler, 0, SLJIT_ARGS3V(P, P, P), 4, 4, 6, SLJIT_NUMBER_OF_SAVED_FLOAT_REGISTERS > 0 ? 2 : 0, 16);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_8;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_AND | type, SLJIT_FR0, SLJIT_FR0, SLJIT_FR2);
	/* buf[64] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 64);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_OR | type, SLJIT_FR2, SLJIT_FR0, SLJIT_FR2);
	/* buf[80] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 80);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_16;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_XOR | type, SLJIT_FR4, fs0, SLJIT_FR2);
	/* buf[96] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 96);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_AND | type, SLJIT_FR1, SLJIT_FR2, SLJIT_FR0);
	/* buf[112] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 112);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_128;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_OR | type, fs0, SLJIT_FR0, fs0);
	/* buf[128] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 128);

	type = SLJIT_SIMD_REG_128 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_XOR | type, SLJIT_FR2, SLJIT_FR4, SLJIT_FR0);
	/* buf[144] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 144);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_32;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_AND | type, SLJIT_FR4, SLJIT_FR0, SLJIT_FR4);
	/* buf[160] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR4, SLJIT_MEM1(SLJIT_S0), 160);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_64 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, fs0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_OR | type, SLJIT_FR0, SLJIT_FR2, fs0);
	/* buf[168] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 168);

	type = SLJIT_SIMD_REG_64 | SLJIT_SIMD_ELEM_64;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_XOR | type, fs0, SLJIT_FR0, SLJIT_FR2);
	/* buf[176] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 176);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_8;
	supported[0] = sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_AND | type | SLJIT_SIMD_TEST, SLJIT_FR0, SLJIT_FR0, SLJIT_FR2) != SLJIT_ERR_UNSUPPORTED;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_AND | type, SLJIT_FR0, SLJIT_FR0, SLJIT_FR2);
	/* buf[192] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 192);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_256;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR0, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR2, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_OR | type, fs0, SLJIT_FR0, SLJIT_FR2);
	/* buf[224] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, fs0, SLJIT_MEM1(SLJIT_S0), 224);

	type = SLJIT_SIMD_REG_256 | SLJIT_SIMD_ELEM_32 | SLJIT_SIMD_FLOAT;
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR1, SLJIT_MEM1(SLJIT_S0), 0);
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_LOAD | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 32);
	sljit_emit_simd_op2(compiler, SLJIT_SIMD_OP2_XOR | type, SLJIT_FR3, SLJIT_FR1, SLJIT_FR3);
	/* buf[256] */
	sljit_emit_simd_mov(compiler, SLJIT_SIMD_STORE | type, SLJIT_FR3, SLJIT_MEM1(SLJIT_S0), 256);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)buf);
	sljit_free_code(code.code, NULL);

	FAILED(!check_simd_u32(buf + 64, 16, 0x000000ff), "test_simd8 case 1 failed\n");
	FAILED(!check_simd_u32(buf + 80, 16, 0x00ffffff), "test_simd8 case 2 failed\n");
	FAILED(!check_simd_u32(buf + 96, 16, 0x00ffff00), "test_simd8 case 3 failed\n");
	FAILED(!check_simd_u32(buf + 112, 16, 0x000000ff), "test_simd8 case 4 failed\n");
	FAILED(!check_simd_u32(buf + 128, 16, 0x00ffffff), "test_simd8 case 5 failed\n");
	FAILED(!check_simd_u32(buf + 144, 16, 0x00ffff00), "test_simd8 case 6 failed\n");

#if IS_ARM
	FAILED(!check_simd_u32(buf + 160, 8, 0x000000ff), "test_simd8 case 7 failed\n");
	FAILED(!check_simd_u32(buf + 168, 8, 0x00ffffff), "test_simd8 case 8 failed\n");
	FAILED(!check_simd_u32(buf + 176, 8, 0x00ffff00), "test_simd8 case 9 failed\n");
#endif /* IS_ARM */

	if (supported[0]) {
		FAILED(!check_simd_u32(buf + 192, 32, 0x000000ff), "test_simd8 case 10 failed\n");
		FAILED(!check_simd_u32(buf + 224, 32, 0x00ffffff), "test_simd8 case 11 failed\n");
		FAILED(!check_simd_u32(buf + 256, 32, 0x00ffff00), "test_simd8 case 12 failed\n");
	}

	successful_tests++;
}
