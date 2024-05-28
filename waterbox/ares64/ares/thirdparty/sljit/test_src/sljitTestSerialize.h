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

static void test_serialize1(void)
{
	/* Test serializing large code. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *label;
	struct sljit_jump *jump1;
	struct sljit_jump *jump2;
	struct sljit_jump *mov_addr;
	sljit_sw executable_offset;
	sljit_uw const_addr;
	sljit_uw jump_addr;
	sljit_uw label_addr;
	sljit_sw buf[3];
	sljit_uw* serialized_buffer;
	sljit_uw serialized_size;
	sljit_s32 i;

	if (verbose)
		printf("Run test_serialize1\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 3, 2, 0, 0, 0);

	jump1 = sljit_emit_jump(compiler, SLJIT_JUMP);
	label = sljit_emit_label(compiler);
	jump2 = sljit_emit_jump(compiler, SLJIT_JUMP);
	sljit_set_label(jump2, label);
	label = sljit_emit_label(compiler);
	sljit_set_label(jump1, label);

	mov_addr = sljit_emit_mov_addr(compiler, SLJIT_R2, 0);
	/* buf[0] */
	sljit_emit_const(compiler, SLJIT_MEM1(SLJIT_S0), 0, -1234);

	sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_R2, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, -1234);

	label = sljit_emit_label(compiler);
	sljit_set_label(mov_addr, label);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R1, 0, SLJIT_IMM, 7);
	for (i = 0; i < 4096; i++)
		sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R1, 0, SLJIT_R1, 0, SLJIT_IMM, 3);

	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_R1, 0);

	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_IMM, -56789);
	jump1 = sljit_emit_jump(compiler, SLJIT_JUMP | SLJIT_REWRITABLE_JUMP);
	label = sljit_emit_label(compiler);
	sljit_set_label(jump1, label);

	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_IMM, 0);
	label = sljit_emit_label(compiler);

	serialized_buffer = sljit_serialize_compiler(compiler, 0, &serialized_size);
	FAILED(!serialized_buffer, "cannot serialize compiler\n");
	sljit_free_compiler(compiler);

	/* Continue code generation. */
	compiler = sljit_deserialize_compiler(serialized_buffer, serialized_size, 0, NULL, NULL);
	SLJIT_FREE(serialized_buffer, NULL);
	FAILED(!compiler, "cannot deserialize compiler\n");

	jump1 = sljit_emit_jump(compiler, SLJIT_JUMP);
	label = sljit_emit_label(compiler);
	jump2 = sljit_emit_jump(compiler, SLJIT_JUMP);
	sljit_set_label(jump2, label);
	label = sljit_emit_label(compiler);
	sljit_set_label(jump1, label);

	sljit_emit_return_void(compiler);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	executable_offset = sljit_get_executable_offset(compiler);
	const_addr = sljit_get_const_addr(sljit_get_first_const(compiler));
	jump1 = sljit_get_next_jump(sljit_get_next_jump(sljit_get_next_jump(sljit_get_first_jump(compiler))));
	SLJIT_ASSERT(!sljit_jump_is_mov_addr(jump1));
	jump_addr = sljit_get_jump_addr(jump1);
	label = sljit_get_next_label(sljit_get_next_label(sljit_get_next_label(sljit_get_next_label(sljit_get_first_label(compiler)))));
	label_addr = sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	sljit_set_const(const_addr, 87654, executable_offset);
	sljit_set_jump_addr(jump_addr, label_addr, executable_offset);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != 87654, "test_serialize1 case 1 failed\n");
	FAILED(buf[1] != 7 + 4096 * 3, "test_serialize1 case 2 failed\n");
	FAILED(buf[2] != -56789, "test_serialize1 case 3 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test_serialize2(void)
{
	/* Test serializing jumps/labels. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *label;
	struct sljit_jump *jump;
	sljit_uw* serialized_buffer;
	sljit_uw serialized_size;
	sljit_sw buf[3];

	if (verbose)
		printf("Run test_serialize2\n");

	FAILED(!compiler, "cannot create compiler\n");
	buf[0] = 0;
	buf[1] = 0;
	buf[2] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS2V(P, W), 3, 3, 0, 0, 32);
	sljit_emit_cmp(compiler, SLJIT_EQUAL, SLJIT_S1, 0, SLJIT_IMM, 37);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R0), 0);

	sljit_emit_label(compiler);
	/* buf[0] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 0, SLJIT_IMM, -5678);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_SP), 16, SLJIT_IMM, -8765);

	sljit_emit_mov_addr(compiler, SLJIT_S2, 0);
	sljit_emit_cmp(compiler, SLJIT_NOT_EQUAL, SLJIT_S2, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_IMM, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R0, 0, SLJIT_MEM1(SLJIT_R0), 0);

	serialized_buffer = sljit_serialize_compiler(compiler, 0, &serialized_size);
	FAILED(!serialized_buffer, "cannot serialize compiler\n");
	sljit_free_compiler(compiler);

	/* Continue code generation. */
	compiler = sljit_deserialize_compiler(serialized_buffer, serialized_size, 0, NULL, NULL);
	SLJIT_FREE(serialized_buffer, NULL);
	FAILED(!compiler, "cannot deserialize compiler\n");

	label = sljit_emit_label(compiler);
	SLJIT_ASSERT(sljit_get_label_index(label) == 1);
	jump = sljit_get_first_jump(compiler);
	SLJIT_ASSERT(!sljit_jump_is_mov_addr(jump));
	SLJIT_ASSERT(!sljit_jump_has_label(jump) && !sljit_jump_has_target(jump));
	sljit_set_label(jump, label);

	/* buf[1] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), sizeof(sljit_sw), SLJIT_IMM, 3456);

	label = sljit_get_first_label(compiler);
	SLJIT_ASSERT(sljit_get_label_index(label) == 0);
	jump = sljit_emit_jump(compiler, SLJIT_JUMP);
	sljit_set_label(jump, label);

	sljit_emit_label(compiler);

	serialized_buffer = sljit_serialize_compiler(compiler, 0, &serialized_size);
	FAILED(!serialized_buffer, "cannot serialize compiler\n");
	sljit_free_compiler(compiler);

	/* Continue code generation. */
	compiler = sljit_deserialize_compiler(serialized_buffer, serialized_size, 0, NULL, NULL);
	SLJIT_FREE(serialized_buffer, NULL);
	FAILED(!compiler, "cannot deserialize compiler\n");

	sljit_emit_return_void(compiler);

	jump = sljit_get_first_jump(compiler);
	SLJIT_ASSERT(sljit_jump_has_label(jump) && !sljit_jump_has_target(jump));
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(sljit_jump_is_mov_addr(jump));
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(!sljit_jump_is_mov_addr(jump));
	SLJIT_ASSERT(!sljit_jump_has_label(jump) && !sljit_jump_has_target(jump));

	label = sljit_emit_label(compiler);
	sljit_set_label(jump, label);

	/* buf[2] */
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_MEM1(SLJIT_S0), 2 * sizeof(sljit_sw), SLJIT_MEM1(SLJIT_SP), 16);
	sljit_emit_ijump(compiler, SLJIT_JUMP, SLJIT_S2, 0);

	label = sljit_get_first_label(compiler);
	SLJIT_ASSERT(sljit_get_label_index(label) == 0);
	label = sljit_get_next_label(label);
	SLJIT_ASSERT(sljit_get_label_index(label) == 1);
	label = sljit_get_next_label(label);
	SLJIT_ASSERT(sljit_get_label_index(label) == 2);
	jump = sljit_get_next_jump(sljit_get_first_jump(compiler));
	SLJIT_ASSERT(sljit_jump_is_mov_addr(jump));
	sljit_set_label(jump, label);
	label = sljit_get_next_label(label);
	SLJIT_ASSERT(sljit_get_label_index(label) == 3);
	SLJIT_ASSERT(sljit_get_next_label(label) == NULL);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	sljit_free_compiler(compiler);

	code.func2((sljit_sw)&buf, 37);
	FAILED(buf[0] != -5678, "test_serialize2 case 1 failed\n");
	FAILED(buf[1] != 3456, "test_serialize2 case 2 failed\n");
	FAILED(buf[2] != -8765, "test_serialize2 case 3 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}

static void test_serialize3_f1(sljit_sw a, sljit_sw b, sljit_sw c)
{
	sljit_sw* ptr = (sljit_sw*)c;
	ptr[0] = a;
	ptr[1] = b;
}

static void test_serialize3(void)
{
	/* Test serializing consts/calls. */
	executable_code code;
	struct sljit_compiler* compiler = sljit_create_compiler(NULL, NULL);
	struct sljit_label *label;
	struct sljit_jump *jump;
	struct sljit_const *const_;
	sljit_sw executable_offset;
	sljit_uw* serialized_buffer;
	sljit_uw serialized_size;
	sljit_sw buf[6];
	sljit_sw label_addr;
	sljit_s32 i;

	if (verbose)
		printf("Run test_serialize3\n");

	FAILED(!compiler, "cannot create compiler\n");
	for (i = 0; i < 6 ; i++)
		buf[i] = 0;

	sljit_emit_enter(compiler, 0, SLJIT_ARGS1V(P), 3, 3, 0, 0, 32);

	sljit_emit_mov_addr(compiler, SLJIT_R0, 0);
	sljit_emit_const(compiler, SLJIT_R1, 0, 0);
	sljit_emit_op1(compiler, SLJIT_MOV, SLJIT_R2, 0, SLJIT_S0, 0);
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3V(W, W, W));
	/* buf[0], buf[1] */
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_serialize3_f1));

	serialized_buffer = sljit_serialize_compiler(compiler, 0, &serialized_size);
	FAILED(!serialized_buffer, "cannot serialize compiler\n");
	sljit_free_compiler(compiler);

	/* Continue code generation. */
	compiler = sljit_deserialize_compiler(serialized_buffer, serialized_size, 0, NULL, NULL);
	SLJIT_FREE(serialized_buffer, NULL);
	FAILED(!compiler, "cannot deserialize compiler\n");

	sljit_emit_mov_addr(compiler, SLJIT_R0, 0);
	sljit_emit_const(compiler, SLJIT_R1, 0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 2 * sizeof(sljit_sw));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3V(W, W, W));
	/* buf[2], buf[3] */
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_serialize3_f1));

	serialized_buffer = sljit_serialize_compiler(compiler, 0, &serialized_size);
	FAILED(!serialized_buffer, "cannot serialize compiler\n");
	sljit_free_compiler(compiler);

	/* Continue code generation. */
	compiler = sljit_deserialize_compiler(serialized_buffer, serialized_size, 0, NULL, NULL);
	SLJIT_FREE(serialized_buffer, NULL);
	FAILED(!compiler, "cannot deserialize compiler\n");

	sljit_emit_mov_addr(compiler, SLJIT_R0, 0);
	sljit_emit_const(compiler, SLJIT_R1, 0, 0);
	sljit_emit_op2(compiler, SLJIT_ADD, SLJIT_R2, 0, SLJIT_S0, 0, SLJIT_IMM, 4 * sizeof(sljit_sw));
	jump = sljit_emit_call(compiler, SLJIT_CALL, SLJIT_ARGS3V(W, W, W));
	/* buf[4], buf[5] */
	sljit_set_target(jump, SLJIT_FUNC_UADDR(test_serialize3_f1));

	sljit_emit_return_void(compiler);
	SLJIT_ASSERT(sljit_get_first_label(compiler) == NULL);
	label = sljit_emit_label(compiler);
	SLJIT_ASSERT(sljit_get_label_index(label) == 0);

	jump = sljit_get_first_jump(compiler);
	SLJIT_ASSERT(sljit_jump_is_mov_addr(jump));
	sljit_set_label(jump, label);
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(!sljit_jump_is_mov_addr(jump));
	SLJIT_ASSERT(sljit_jump_has_target(jump) && sljit_jump_get_target(jump) == SLJIT_FUNC_UADDR(test_serialize3_f1));
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(sljit_jump_is_mov_addr(jump));
	sljit_set_label(jump, label);
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(!sljit_jump_is_mov_addr(jump));
	SLJIT_ASSERT(sljit_jump_has_target(jump) && sljit_jump_get_target(jump) == SLJIT_FUNC_UADDR(test_serialize3_f1));
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(sljit_jump_is_mov_addr(jump));
	sljit_set_label(jump, label);
	jump = sljit_get_next_jump(jump);
	SLJIT_ASSERT(sljit_jump_has_target(jump) && sljit_jump_get_target(jump) == SLJIT_FUNC_UADDR(test_serialize3_f1));
	SLJIT_ASSERT(sljit_get_next_jump(jump) == NULL);

	code.code = sljit_generate_code(compiler);
	CHECK(compiler);
	executable_offset = sljit_get_executable_offset(compiler);

	const_ = sljit_get_first_const(compiler);
	sljit_set_const(sljit_get_const_addr(const_), 0x5678, executable_offset);
	const_ = sljit_get_next_const(const_);
	sljit_set_const(sljit_get_const_addr(const_), -0x9876, executable_offset);
	const_ = sljit_get_next_const(const_);
	sljit_set_const(sljit_get_const_addr(const_), 0x2345, executable_offset);
	SLJIT_ASSERT(sljit_get_next_const(const_) == NULL);

	label_addr = (sljit_sw)sljit_get_label_addr(label);
	sljit_free_compiler(compiler);

	code.func1((sljit_sw)&buf);
	FAILED(buf[0] != label_addr, "test_serialize3 case 1 failed\n");
	FAILED(buf[1] != 0x5678, "test_serialize3 case 2 failed\n");
	FAILED(buf[2] != label_addr, "test_serialize3 case 3 failed\n");
	FAILED(buf[3] != -0x9876, "test_serialize3 case 4 failed\n");
	FAILED(buf[4] != label_addr, "test_serialize3 case 5 failed\n");
	FAILED(buf[5] != 0x2345, "test_serialize3 case 6 failed\n");

	sljit_free_code(code.code, NULL);
	successful_tests++;
}
