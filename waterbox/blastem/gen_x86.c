/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#include "gen_x86.h"
#include "mem.h"
#include "util.h"
#include <stddef.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>

#define REX_RM_FIELD 0x1
#define REX_SIB_FIELD 0x2
#define REX_REG_FIELD 0x4
#define REX_QUAD 0x8

#define OP_ADD 0x00
#define OP_OR  0x08
#define PRE_2BYTE 0x0F
#define OP_ADC 0x10
#define OP_SBB 0x18
#define OP_AND 0x20
#define OP_SUB 0x28
#define OP_XOR 0x30
#define OP_CMP 0x38
#define PRE_REX 0x40
#define OP_PUSH 0x50
#define OP_POP 0x58
#define OP_MOVSXD 0x63
#define PRE_SIZE 0x66
#define OP_IMUL 0x69
#define OP_JCC 0x70
#define OP_IMMED_ARITH 0x80
#define OP_TEST 0x84
#define OP_XCHG 0x86
#define OP_MOV 0x88
#define PRE_XOP 0x8F
#define OP_XCHG_AX 0x90
#define OP_CDQ 0x99
#define OP_PUSHF 0x9C
#define OP_POPF 0x9D
#define OP_MOV_I8R 0xB0
#define OP_MOV_IR 0xB8
#define OP_SHIFTROT_IR 0xC0
#define OP_RETN 0xC3
#define OP_MOV_IEA 0xC6
#define OP_SHIFTROT_1 0xD0
#define OP_SHIFTROT_CL 0xD2
#define OP_LOOP 0xE2
#define OP_CALL 0xE8
#define OP_JMP 0xE9
#define OP_JMP_BYTE 0xEB
#define OP_NOT_NEG 0xF6
#define OP_SINGLE_EA 0xFF

#define OP2_JCC 0x80
#define OP2_SETCC 0x90
#define OP2_BT 0xA3
#define OP2_BTS 0xAB
#define OP2_IMUL 0xAF
#define OP2_BTR 0xB3
#define OP2_BTX_I 0xBA
#define OP2_BTC 0xBB
#define OP2_MOVSX 0xBE
#define OP2_MOVZX 0xB6

#define OP_EX_ADDI 0x0
#define OP_EX_ORI  0x1
#define OP_EX_ADCI 0x2
#define OP_EX_SBBI 0x3
#define OP_EX_ANDI 0x4
#define OP_EX_SUBI 0x5
#define OP_EX_XORI 0x6
#define OP_EX_CMPI 0x7

#define OP_EX_ROL 0x0
#define OP_EX_ROR 0x1
#define OP_EX_RCL 0x2
#define OP_EX_RCR 0x3
#define OP_EX_SHL 0x4
#define OP_EX_SHR 0x5
#define OP_EX_SAL 0x6 //identical to SHL
#define OP_EX_SAR 0x7

#define OP_EX_BT  0x4
#define OP_EX_BTS 0x5
#define OP_EX_BTR 0x6
#define OP_EX_BTC 0x7

#define OP_EX_TEST_I 0x0
#define OP_EX_NOT    0x2
#define OP_EX_NEG    0x3
#define OP_EX_MUL    0x4
#define OP_EX_IMUL   0x5
#define OP_EX_DIV    0x6
#define OP_EX_IDIV   0x7

#define OP_EX_INC     0x0
#define OP_EX_DEC     0x1
#define OP_EX_CALL_EA 0x2
#define OP_EX_JMP_EA  0x4
#define OP_EX_PUSH_EA 0x6

#define BIT_IMMED_RAX 0x4
#define BIT_DIR 0x2
#define BIT_SIZE 0x1


enum {
	X86_RAX = 0,
	X86_RCX,
	X86_RDX,
	X86_RBX,
	X86_RSP,
	X86_RBP,
	X86_RSI,
	X86_RDI,
	X86_AH=4,
	X86_CH,
	X86_DH,
	X86_BH,
	X86_R8=0,
	X86_R9,
	X86_R10,
	X86_R11,
	X86_R12,
	X86_R13,
	X86_R14,
	X86_R15
};

char * x86_reg_names[] = {
#ifdef X86_64
	"rax",
	"rcx",
	"rdx",
	"rbx",
	"rsp",
	"rbp",
	"rsi",
	"rdi",
#else
	"eax",
	"ecx",
	"edx",
	"ebx",
	"esp",
	"ebp",
	"esi",
	"edi",
#endif
	"ah",
	"ch",
	"dh",
	"bh",
	"r8",
	"r9",
	"r10",
	"r11",
	"r12",
	"r13",
	"r14",
	"r15",
};

char * x86_sizes[] = {
	"b", "w", "d", "q"
};

#ifdef X86_64
#define CHECK_DISP(disp) (disp <= ((ptrdiff_t)INT32_MAX) && disp >= ((ptrdiff_t)INT32_MIN))
#else
#define CHECK_DISP(disp) 1
#endif

void jmp_nocheck(code_info *code, code_ptr dest)
{
	code_ptr out = code->cur;
	ptrdiff_t disp = dest-(out+2);
	if (disp <= 0x7F && disp >= -0x80) {
		*(out++) = OP_JMP_BYTE;
		*(out++) = disp;
	} else {
		disp = dest-(out+5);
		if (CHECK_DISP(disp)) {
			*(out++) = OP_JMP;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
		} else {
			fatal_error("jmp: %p - %p = %l which is out of range of a 32-bit displacementX\n", dest, out + 6, (long)disp);
		}
	}
	code->cur = out;
}

void check_alloc_code(code_info *code, uint32_t inst_size)
{
	if (code->cur + inst_size > code->last) {
		size_t size = CODE_ALLOC_SIZE;
		code_ptr next_code = alloc_code(&size);
		if (!next_code) {
			fatal_error("Failed to allocate memory for generated code\n");
		}
		if (next_code != code->last + RESERVE_WORDS) {
			//new chunk is not contiguous with the current one
			jmp_nocheck(code, next_code);
			code->cur = next_code;
		}
		code->last = next_code + size/sizeof(code_word) - RESERVE_WORDS;
	}
}

void x86_rr_sizedir(code_info *code, uint16_t opcode, uint8_t src, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_B && dst >= RSP && dst <= RDI) {
		opcode |= BIT_DIR;
		tmp = dst;
		dst = src;
		src = tmp;
	}
	if (size == SZ_Q || src >= R8 || dst >= R8 || (size == SZ_B && src >= RSP && src <= RDI)) {
#ifdef X86_64
		*out = PRE_REX;
		if (src >= AH && src <= BH || dst >= AH && dst <= BH) {
			fatal_error("attempt to use *H reg in an instruction requiring REX prefix. opcode = %X\n", opcode);
		}
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_REG_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X, src: %s, dst: %s, size: %s\n", opcode, x86_reg_names[src], x86_reg_names[dst], x86_sizes[size]);
#endif
	}
	if (size == SZ_B) {
		if (src >= AH && src <= BH) {
			src -= (AH-X86_AH);
		}
		if (dst >= AH && dst <= BH) {
			dst -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	if (opcode >= 0x100) {
		*(out++) = opcode >> 8;
		*(out++) = opcode;
	} else {
		*(out++) = opcode;
	}
	*(out++) = MODE_REG_DIRECT | dst | (src << 3);
	code->cur = out;
}

void x86_rrdisp_sizedir(code_info *code, uint16_t opcode, uint8_t reg, uint8_t base, int32_t disp, uint8_t size, uint8_t dir)
{
	check_alloc_code(code, 10);
	code_ptr out = code->cur;
	//TODO: Deal with the fact that AH, BH, CH and DH can only be in the R/M param when there's a REX prefix
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || reg >= R8 || base >= R8 || (size == SZ_B && reg >= RSP && reg <= RDI)) {
#ifdef X86_64
		*out = PRE_REX;
		if (reg >= AH && reg <= BH) {
			fatal_error("attempt to use *H reg in an instruction requiring REX prefix. opcode = %X\n", opcode);
		}
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (reg >= R8) {
			*out |= REX_REG_FIELD;
			reg -= (R8 - X86_R8);
		}
		if (base >= R8) {
			*out |= REX_RM_FIELD;
			base -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X, reg: %s, base: %s, size: %s\n", opcode, x86_reg_names[reg], x86_reg_names[base], x86_sizes[size]);
#endif
	}
	if (size == SZ_B) {
		if (reg >= AH && reg <= BH) {
			reg -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	opcode |= dir;
	if (opcode >= 0x100) {
		*(out++) = opcode >> 8;
		*(out++) = opcode;
	} else {
		*(out++) = opcode;
	}
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | base | (reg << 3);
	} else {
		*(out++) = MODE_REG_DISPLACE32 | base | (reg << 3);
	}
	if (base == RSP) {
		//add SIB byte, with no index and RSP as base
		*(out++) = (RSP << 3) | RSP;
	}
	*(out++) = disp;
	if (disp >= 128 || disp < -128) {
	*(out++) = disp >> 8;
	*(out++) = disp >> 16;
	*(out++) = disp >> 24;
	}
	code->cur = out;
}

void x86_rrind_sizedir(code_info *code, uint8_t opcode, uint8_t reg, uint8_t base, uint8_t size, uint8_t dir)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	//TODO: Deal with the fact that AH, BH, CH and DH can only be in the R/M param when there's a REX prefix
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || reg >= R8 || base >= R8 || (size == SZ_B && reg >= RSP && reg <= RDI)) {
#ifdef X86_64
		*out = PRE_REX;
		if (reg >= AH && reg <= BH) {
			fatal_error("attempt to use *H reg in an instruction requiring REX prefix. opcode = %X\n", opcode);
		}
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (reg >= R8) {
			*out |= REX_REG_FIELD;
			reg -= (R8 - X86_R8);
		}
		if (base >= R8) {
			*out |= REX_RM_FIELD;
			base -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X, reg: %s, base: %s, size: %s\n", opcode, x86_reg_names[reg], x86_reg_names[base], x86_sizes[size]);
#endif
	}
	if (size == SZ_B) {
		if (reg >= AH && reg <= BH) {
			reg -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode | dir;
	if (base == RBP) {
		//add a dummy 8-bit displacement since MODE_REG_INDIRECT with
		//an R/M field of RBP selects RIP, relative addressing
		*(out++) = MODE_REG_DISPLACE8 | base | (reg << 3);
		*(out++) = 0;
	} else {
	*(out++) = MODE_REG_INDIRECT | base | (reg << 3);
	if (base == RSP) {
		//add SIB byte, with no index and RSP as base
		*(out++) = (RSP << 3) | RSP;
	}
	}
	code->cur = out;
}

void x86_rrindex_sizedir(code_info *code, uint8_t opcode, uint8_t reg, uint8_t base, uint8_t index, uint8_t scale, uint8_t size, uint8_t dir)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	//TODO: Deal with the fact that AH, BH, CH and DH can only be in the R/M param when there's a REX prefix
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || reg >= R8 || base >= R8 || (size == SZ_B && reg >= RSP && reg <= RDI)) {
#ifdef X86_64
		*out = PRE_REX;
		if (reg >= AH && reg <= BH) {
			fatal_error("attempt to use *H reg in an instruction requiring REX prefix. opcode = %X\n", opcode);
		}
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (reg >= R8) {
			*out |= REX_REG_FIELD;
			reg -= (R8 - X86_R8);
		}
		if (base >= R8) {
			*out |= REX_RM_FIELD;
			base -= (R8 - X86_R8);
		}
		if (index >= R8) {
			*out |= REX_SIB_FIELD;
			index -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X, reg: %s, base: %s, size: %s\n", opcode, x86_reg_names[reg], x86_reg_names[base], x86_sizes[size]);
#endif
	}
	if (size == SZ_B) {
		if (reg >= AH && reg <= BH) {
			reg -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode | dir;
	*(out++) = MODE_REG_INDIRECT | RSP | (reg << 3);
	if (scale == 4) {
		scale = 2;
	} else if(scale == 8) {
			scale = 3;
	} else {
		scale--;
	}
	*(out++) = scale << 6 | (index << 3) | base;
	code->cur = out;
}

void x86_r_size(code_info *code, uint8_t opcode, uint8_t opex, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 4);
	code_ptr out = code->cur;
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
#ifdef X86_64
		*out = PRE_REX;
		if (dst >= AH && dst <= BH) {
			fatal_error("attempt to use *H reg in an instruction requiring REX prefix. opcode = %X\n", opcode);
		}
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X:%X, reg: %s, size: %s\n", opcode, opex, x86_reg_names[dst], x86_sizes[size]);
#endif
	}
	if (size == SZ_B) {
		if (dst >= AH && dst <= BH) {
			dst -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode;
	*(out++) = MODE_REG_DIRECT | dst | (opex << 3);
	code->cur = out;
}

void x86_rdisp_size(code_info *code, uint8_t opcode, uint8_t opex, uint8_t dst, int32_t disp, uint8_t size)
{
	check_alloc_code(code, 7);
	code_ptr out = code->cur;
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
#ifdef X86_64
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X:%X, reg: %s, size: %s\n", opcode, opex, x86_reg_names[dst], x86_sizes[size]);
#endif
	}
	if (size != SZ_B) {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode;
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst | (opex << 3);
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst | (opex << 3);
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}
	code->cur = out;
}

void x86_ir(code_info *code, uint8_t opcode, uint8_t op_ex, uint8_t al_opcode, int32_t val, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 8);
	code_ptr out = code->cur;
	uint8_t sign_extend = 0;
	if (opcode != OP_NOT_NEG && (size == SZ_D || size == SZ_Q) && val <= 0x7F && val >= -0x80) {
		sign_extend = 1;
		opcode |= BIT_DIR;
	}
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (dst == RAX && !sign_extend && al_opcode) {
		if (size != SZ_B) {
			al_opcode |= BIT_SIZE;
			if (size == SZ_Q) {
#ifdef X86_64
				*out = PRE_REX | REX_QUAD;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X, reg: %s, size: %s\n", al_opcode, x86_reg_names[dst], x86_sizes[size]);
#endif
			}
		}
		*(out++) = al_opcode | BIT_IMMED_RAX;
	} else {
		if (size == SZ_Q || dst >= R8 || (size == SZ_B && dst >= RSP && dst <= RDI)) {
#ifdef X86_64
			*out = PRE_REX;
			if (size == SZ_Q) {
				*out |= REX_QUAD;
			}
			if (dst >= R8) {
				*out |= REX_RM_FIELD;
				dst -= (R8 - X86_R8);
			}
			out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X:%X, reg: %s, size: %s\n", opcode, op_ex, x86_reg_names[dst], x86_sizes[size]);
#endif
		}
		if (dst >= AH && dst <= BH) {
			dst -= (AH-X86_AH);
		}
		if (size != SZ_B) {
			opcode |= BIT_SIZE;
		}
		*(out++) = opcode;
		*(out++) = MODE_REG_DIRECT | dst | (op_ex << 3);
	}
	*(out++) = val;
	if (size != SZ_B && !sign_extend) {
		val >>= 8;
		*(out++) = val;
		if (size != SZ_W) {
			val >>= 8;
			*(out++) = val;
			val >>= 8;
			*(out++) = val;
		}
	}
	code->cur = out;
}

void x86_irdisp(code_info *code, uint8_t opcode, uint8_t op_ex, int32_t val, uint8_t dst, int32_t disp, uint8_t size)
{
	check_alloc_code(code, 12);
	code_ptr out = code->cur;
	uint8_t sign_extend = 0;
	if ((size == SZ_D || size == SZ_Q) && val <= 0x7F && val >= -0x80) {
		sign_extend = 1;
		opcode |= BIT_DIR;
	}
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}

	if (size == SZ_Q || dst >= R8) {
#ifdef X86_64
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
#else
		fatal_error("Instruction requires REX prefix but this is a 32-bit build | opcode: %X:%X, reg: %s, size: %s\n", opcode, op_ex, x86_reg_names[dst], x86_sizes[size]);
#endif
	}
	if (size != SZ_B) {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode;
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst | (op_ex << 3);
	*(out++) = disp;
	} else {
	*(out++) = MODE_REG_DISPLACE32 | dst | (op_ex << 3);
	*(out++) = disp;
	disp >>= 8;
	*(out++) = disp;
	disp >>= 8;
	*(out++) = disp;
	disp >>= 8;
	*(out++) = disp;
	}
	*(out++) = val;
	if (size != SZ_B && !sign_extend) {
		val >>= 8;
		*(out++) = val;
		if (size != SZ_W) {
			val >>= 8;
			*(out++) = val;
			val >>= 8;
			*(out++) = val;
		}
	}
	code->cur = out;
}

void x86_shiftrot_ir(code_info *code, uint8_t op_ex, uint8_t val, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || (size == SZ_B && dst >= RSP && dst <= RDI)) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}

	*(out++) = (val == 1 ? OP_SHIFTROT_1: OP_SHIFTROT_IR) | (size == SZ_B ? 0 : BIT_SIZE);
	*(out++) = MODE_REG_DIRECT | dst | (op_ex << 3);
	if (val != 1) {
		*(out++) = val;
	}
	code->cur = out;
}

void x86_shiftrot_irdisp(code_info *code, uint8_t op_ex, uint8_t val, uint8_t dst, int32_t disp, uint8_t size)
{
	check_alloc_code(code, 9);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}

	*(out++) = (val == 1 ? OP_SHIFTROT_1: OP_SHIFTROT_IR) | (size == SZ_B ? 0 : BIT_SIZE);
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst | (op_ex << 3);
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst | (op_ex << 3);
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}
	if (val != 1) {
		*(out++) = val;
	}
	code->cur = out;
}

void x86_shiftrot_clr(code_info *code, uint8_t op_ex, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 4);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || (size == SZ_B && dst >= RSP && dst <= RDI)) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}

	*(out++) = OP_SHIFTROT_CL | (size == SZ_B ? 0 : BIT_SIZE);
	*(out++) = MODE_REG_DIRECT | dst | (op_ex << 3);
	code->cur = out;
}

void x86_shiftrot_clrdisp(code_info *code, uint8_t op_ex, uint8_t dst, int32_t disp, uint8_t size)
{
	check_alloc_code(code, 8);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}

	*(out++) = OP_SHIFTROT_CL | (size == SZ_B ? 0 : BIT_SIZE);
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst | (op_ex << 3);
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst | (op_ex << 3);
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
}
	code->cur = out;
}

void rol_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_ROL, val, dst, size);
}

void ror_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_ROR, val, dst, size);
}

void rcl_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_RCL, val, dst, size);
}

void rcr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_RCR, val, dst, size);
}

void shl_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_SHL, val, dst, size);
}

void shr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_SHR, val, dst, size);
}

void sar_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	x86_shiftrot_ir(code, OP_EX_SAR, val, dst, size);
}

void rol_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_ROL, val, dst_base, disp, size);
}

void ror_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_ROR, val, dst_base, disp, size);
}

void rcl_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_RCL, val, dst_base, disp, size);
}

void rcr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_RCR, val, dst_base, disp, size);
}

void shl_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_SHL, val, dst_base, disp, size);
}

void shr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_SHR, val, dst_base, disp, size);
}

void sar_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_irdisp(code, OP_EX_SAR, val, dst_base, disp, size);
}

void rol_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_ROL, dst, size);
}

void ror_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_ROR, dst, size);
}

void rcl_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_RCL, dst, size);
}

void rcr_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_RCR, dst, size);
}

void shl_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_SHL, dst, size);
}

void shr_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_SHR, dst, size);
}

void sar_clr(code_info *code, uint8_t dst, uint8_t size)
{
	x86_shiftrot_clr(code, OP_EX_SAR, dst, size);
}

void rol_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_ROL, dst_base, disp, size);
}

void ror_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_ROR, dst_base, disp, size);
}

void rcl_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_RCL, dst_base, disp, size);
}

void rcr_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_RCR, dst_base, disp, size);
}

void shl_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_SHL, dst_base, disp, size);
}

void shr_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_SHR, dst_base, disp, size);
}

void sar_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_shiftrot_clrdisp(code, OP_EX_SAR, dst_base, disp, size);
}

void add_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_ADD, src, dst, size);
}

void add_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_ADDI, OP_ADD, val, dst, size);
}

void add_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_ADDI, val, dst_base, disp, size);
}

void add_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_ADD, src, dst_base, disp, size, 0);
}

void add_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_ADD, dst, src_base, disp, size, BIT_DIR);
}

void adc_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_ADC, src, dst, size);
}

void adc_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_ADCI, OP_ADC, val, dst, size);
}

void adc_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_ADCI, val, dst_base, disp, size);
}

void adc_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_ADC, src, dst_base, disp, size, 0);
}

void adc_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_ADC, dst, src_base, disp, size, BIT_DIR);
}

void or_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_OR, src, dst, size);
}
void or_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_ORI, OP_OR, val, dst, size);
}

void or_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_ORI, val, dst_base, disp, size);
}

void or_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_OR, src, dst_base, disp, size, 0);
}

void or_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_OR, dst, src_base, disp, size, BIT_DIR);
}

void and_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_AND, src, dst, size);
}

void and_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_ANDI, OP_AND, val, dst, size);
}

void and_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_ANDI, val, dst_base, disp, size);
}

void and_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_AND, src, dst_base, disp, size, 0);
}

void and_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_AND, dst, src_base, disp, size, BIT_DIR);
}

void xor_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_XOR, src, dst, size);
}

void xor_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_XORI, OP_XOR, val, dst, size);
}

void xor_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_XORI, val, dst_base, disp, size);
}

void xor_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_XOR, src, dst_base, disp, size, 0);
}

void xor_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_XOR, dst, src_base, disp, size, BIT_DIR);
}

void sub_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_SUB, src, dst, size);
}

void sub_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_SUBI, OP_SUB, val, dst, size);
}

void sub_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_SUBI, val, dst_base, disp, size);
}

void sub_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_SUB, src, dst_base, disp, size, 0);
}

void sub_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_SUB, dst, src_base, disp, size, BIT_DIR);
}

void sbb_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_SBB, src, dst, size);
}

void sbb_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_SBBI, OP_SBB, val, dst, size);
}

void sbb_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_SBBI, val, dst_base, disp, size);
}

void sbb_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_SBB, src, dst_base, disp, size, 0);
}

void sbb_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_SBB, dst, src_base, disp, size, BIT_DIR);
}

void cmp_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_CMP, src, dst, size);
}

void cmp_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_IMMED_ARITH, OP_EX_CMPI, OP_CMP, val, dst, size);
}

void cmp_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_IMMED_ARITH, OP_EX_CMPI, val, dst_base, disp, size);
}

void cmp_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_CMP, src, dst_base, disp, size, 0);
}

void cmp_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_CMP, dst, src_base, disp, size, BIT_DIR);
}

void test_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_TEST, src, dst, size);
}

void test_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	x86_ir(code, OP_NOT_NEG, OP_EX_TEST_I, OP_TEST, val, dst, size);
}

void test_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_irdisp(code, OP_NOT_NEG, OP_EX_TEST_I, val, dst_base, disp, size);
}

void test_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_TEST, src, dst_base, disp, size, 0);
}

void test_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_TEST, dst, src_base, disp, size, BIT_DIR);
}

void imul_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP2_IMUL | (PRE_2BYTE << 8), dst, src, size);
}

void imul_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP2_IMUL | (PRE_2BYTE << 8), dst, src_base, disp, size, 0);
}

void imul_irr(code_info *code, int32_t val, uint8_t src, uint8_t dst, uint8_t size)
{
	if (size == SZ_B) {
		fatal_error("imul immediate only supports 16-bit sizes and up");
	}
	
	x86_ir(code, OP_IMUL, dst, 0, val, src, size);
}

void not_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_NOT, dst, size);
}

void neg_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_NEG, dst, size);
}

void not_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_NOT, dst_base, disp, size);
}

void neg_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_NEG, dst_base, disp, size);
}

void mul_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_MUL, dst, size);
}

void imul_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_IMUL, dst, size);
}

void div_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_DIV, dst, size);
}

void idiv_r(code_info *code, uint8_t dst, uint8_t size)
{
	x86_r_size(code, OP_NOT_NEG, OP_EX_IDIV, dst, size);
}

void mul_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_MUL, dst_base, disp, size);
}

void imul_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_IMUL, dst_base, disp, size);
}

void div_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_DIV, dst_base, disp, size);
}

void idiv_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rdisp_size(code, OP_NOT_NEG, OP_EX_IDIV, dst_base, disp, size);
}

void mov_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rr_sizedir(code, OP_MOV, src, dst, size);
}

void mov_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_MOV, src, dst_base, disp, size, 0);
}

void mov_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size)
{
	x86_rrdisp_sizedir(code, OP_MOV, dst, src_base, disp, size, BIT_DIR);
}

void mov_rrind(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rrind_sizedir(code, OP_MOV, src, dst, size, 0);
}

void mov_rindr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	x86_rrind_sizedir(code, OP_MOV, dst, src, size, BIT_DIR);
}

void mov_rrindex(code_info *code, uint8_t src, uint8_t dst_base, uint8_t dst_index, uint8_t scale, uint8_t size)
{
	x86_rrindex_sizedir(code, OP_MOV, src, dst_base, dst_index, scale, size, 0);
}

void mov_rindexr(code_info *code, uint8_t src_base, uint8_t src_index, uint8_t scale, uint8_t dst, uint8_t size)
{
	x86_rrindex_sizedir(code, OP_MOV, dst, src_base, src_index, scale, size, BIT_DIR);
}

void mov_ir(code_info *code, int64_t val, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 14);
	code_ptr out = code->cur;
	uint8_t sign_extend = 0;
	if (size == SZ_Q && val <= ((int64_t)INT32_MAX) && val >= ((int64_t)INT32_MIN)) {
		sign_extend = 1;
	}
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || (size == SZ_B && dst >= RSP && dst <= RDI)) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}
	if (size == SZ_B) {
		*(out++) = OP_MOV_I8R | dst;
	} else if (size == SZ_Q && sign_extend) {
		*(out++) = OP_MOV_IEA | BIT_SIZE;
		*(out++) = MODE_REG_DIRECT | dst;
	} else {
		*(out++) = OP_MOV_IR | dst;
	}
	*(out++) = val;
	if (size != SZ_B) {
		val >>= 8;
		*(out++) = val;
		if (size != SZ_W) {
			val >>= 8;
			*(out++) = val;
			val >>= 8;
			*(out++) = val;
			if (size == SZ_Q && !sign_extend) {
				val >>= 8;
				*(out++) = val;
				val >>= 8;
				*(out++) = val;
				val >>= 8;
				*(out++) = val;
				val >>= 8;
				*(out++) = val;
			}
		}
	}
	code->cur = out;
}

uint8_t is_mov_ir(code_ptr inst)
{
	while (*inst == PRE_SIZE || *inst == PRE_REX)
	{
		inst++;
	}
	return (*inst & 0xF8) == OP_MOV_I8R || (*inst & 0xF8) == OP_MOV_IR || (*inst & 0xFE) == OP_MOV_IEA;
}

void mov_irdisp(code_info *code, int32_t val, uint8_t dst, int32_t disp, uint8_t size)
{
	check_alloc_code(code, 12);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}
	*(out++) = OP_MOV_IEA | (size == SZ_B ? 0 : BIT_SIZE);
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst;
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst;
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}

	*(out++) = val;
	if (size != SZ_B) {
		val >>= 8;
		*(out++) = val;
		if (size != SZ_W) {
			val >>= 8;
			*(out++) = val;
			val >>= 8;
			*(out++) = val;
		}
	}
	code->cur = out;
}

void mov_irind(code_info *code, int32_t val, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 8);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || (size == SZ_B && dst >= RSP && dst <= RDI)) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (dst >= AH && dst <= BH) {
		dst -= (AH-X86_AH);
	}
	*(out++) = OP_MOV_IEA | (size == SZ_B ? 0 : BIT_SIZE);
	*(out++) = MODE_REG_INDIRECT | dst;

	*(out++) = val;
	if (size != SZ_B) {
		val >>= 8;
		*(out++) = val;
		if (size != SZ_W) {
			val >>= 8;
			*(out++) = val;
			val >>= 8;
			*(out++) = val;
		}
	}
	code->cur = out;
}

void movsx_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t src_size, uint8_t size)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || src >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_RM_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_REG_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (src_size == SZ_D) {
		*(out++) = OP_MOVSXD;
	} else {
		*(out++) = PRE_2BYTE;
		*(out++) = OP2_MOVSX | (src_size == SZ_B ? 0 : BIT_SIZE);
	}
	*(out++) = MODE_REG_DIRECT | src | (dst << 3);
	code->cur = out;
}

void movsx_rdispr(code_info *code, uint8_t src, int32_t disp, uint8_t dst, uint8_t src_size, uint8_t size)
{
	check_alloc_code(code, 12);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || src >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_RM_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_REG_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	if (src_size == SZ_D) {
		*(out++) = OP_MOVSXD;
	} else {
		*(out++) = PRE_2BYTE;
		*(out++) = OP2_MOVSX | (src_size == SZ_B ? 0 : BIT_SIZE);
	}
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | src | (dst << 3);
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | src | (dst << 3);
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}
	code->cur = out;
}

void movzx_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t src_size, uint8_t size)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || src >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_RM_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_REG_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_MOVZX | (src_size == SZ_B ? 0 : BIT_SIZE);
	*(out++) = MODE_REG_DIRECT | src | (dst << 3);
	code->cur = out;
}

void movzx_rdispr(code_info *code, uint8_t src, int32_t disp, uint8_t dst, uint8_t src_size, uint8_t size)
{
	check_alloc_code(code, 9);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8 || src >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_RM_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_REG_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_MOVZX | (src_size == SZ_B ? 0 : BIT_SIZE);
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | src | (dst << 3);
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | src | (dst << 3);
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}
	code->cur = out;
}

void xchg_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 4);
	code_ptr out = code->cur;
	//TODO: Use OP_XCHG_AX when one of the registers is AX, EAX or RAX
	uint8_t tmp;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_B && dst >= RSP && dst <= RDI) {
		tmp = dst;
		dst = src;
		src = tmp;
	}
	if (size == SZ_Q || src >= R8 || dst >= R8 || (size == SZ_B && src >= RSP && src <= RDI)) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_REG_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	uint8_t opcode = OP_XCHG;
	if (size == SZ_B) {
		if (src >= AH && src <= BH) {
			src -= (AH-X86_AH);
		}
		if (dst >= AH && dst <= BH) {
			dst -= (AH-X86_AH);
		}
	} else {
		opcode |= BIT_SIZE;
	}
	*(out++) = opcode;
	*(out++) = MODE_REG_DIRECT | dst | (src << 3);
	code->cur = out;
}

void pushf(code_info *code)
{
	check_alloc_code(code, 1);
	code_ptr out = code->cur;
	*(out++) = OP_PUSHF;
	code->cur = out;
}

void popf(code_info *code)
{
	check_alloc_code(code, 1);
	code_ptr out = code->cur;
	*(out++) = OP_POPF;
	code->cur = out;
}

void push_r(code_info *code, uint8_t reg)
{
	check_alloc_code(code, 2);
	code_ptr out = code->cur;
	if (reg >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		reg -= R8 - X86_R8;
	}
	*(out++) = OP_PUSH | reg;
	code->cur = out;
	code->stack_off += sizeof(void *);
}

void push_rdisp(code_info *code, uint8_t base, int32_t disp)
{
	//This instruction has no explicit size, so we pass SZ_B
	//to avoid any prefixes or bits being set
	x86_rdisp_size(code, OP_SINGLE_EA, OP_EX_PUSH_EA, base, disp, SZ_B);
	code->stack_off += sizeof(void *);
}

void pop_r(code_info *code, uint8_t reg)
{
	check_alloc_code(code, 2);
	code_ptr out = code->cur;
	if (reg >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		reg -= R8 - X86_R8;
	}
	*(out++) = OP_POP | reg;
	code->cur = out;
	code->stack_off -= sizeof(void *);
}

void pop_rind(code_info *code, uint8_t reg)
{
	check_alloc_code(code, 3);
	code_ptr out = code->cur;
	if (reg >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		reg -= R8 - X86_R8;
	}
	*(out++) = PRE_XOP;
	*(out++) = MODE_REG_INDIRECT | reg;
	code->cur = out;
	code->stack_off -=  sizeof(void *);
}

void setcc_r(code_info *code, uint8_t cc, uint8_t dst)
{
	check_alloc_code(code, 4);
	code_ptr out = code->cur;
	if (dst >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		dst -= R8 - X86_R8;
	} else if (dst >= RSP && dst <= RDI) {
		*(out++) = PRE_REX;
	} else if (dst >= AH && dst <= BH) {
		dst -= AH - X86_AH;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_SETCC | cc;
	*(out++) = MODE_REG_DIRECT | dst;
	code->cur = out;
}

void setcc_rind(code_info *code, uint8_t cc, uint8_t dst)
{
	check_alloc_code(code, 4);
	code_ptr out = code->cur;
	if (dst >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		dst -= R8 - X86_R8;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_SETCC | cc;
	*(out++) = MODE_REG_INDIRECT | dst;
	code->cur = out;
}

void setcc_rdisp(code_info *code, uint8_t cc, uint8_t dst, int32_t disp)
{
	check_alloc_code(code, 8);
	code_ptr out = code->cur;
	if (dst >= R8) {
		*(out++) = PRE_REX | REX_RM_FIELD;
		dst -= R8 - X86_R8;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_SETCC | cc;
	if (disp < 128 && disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst;
	*(out++) = disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst;
		*(out++) = disp;
		*(out++) = disp >> 8;
		*(out++) = disp >> 16;
		*(out++) = disp >> 24;
	}
	code->cur = out;
}

void bit_rr(code_info *code, uint8_t op2, uint8_t src, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || src >= R8 || dst >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_REG_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = op2;
	*(out++) = MODE_REG_DIRECT | dst | (src << 3);
	code->cur = out;
}

void bit_rrdisp(code_info *code, uint8_t op2, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	check_alloc_code(code, 9);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || src >= R8 || dst_base >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (src >= R8) {
			*out |= REX_REG_FIELD;
			src -= (R8 - X86_R8);
		}
		if (dst_base >= R8) {
			*out |= REX_RM_FIELD;
			dst_base -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = op2;
	if (dst_disp < 128 && dst_disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst_base | (src << 3);
	*(out++) = dst_disp;
	} else {
	*(out++) = MODE_REG_DISPLACE32 | dst_base | (src << 3);
	*(out++) = dst_disp;
	*(out++) = dst_disp >> 8;
	*(out++) = dst_disp >> 16;
	*(out++) = dst_disp >> 24;
	}
	code->cur = out;
}

void bit_ir(code_info *code, uint8_t op_ex, uint8_t val, uint8_t dst, uint8_t size)
{
	check_alloc_code(code, 6);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst >= R8) {
			*out |= REX_RM_FIELD;
			dst -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_BTX_I;
	*(out++) = MODE_REG_DIRECT | dst | (op_ex << 3);
	*(out++) = val;
	code->cur = out;
}

void bit_irdisp(code_info *code, uint8_t op_ex, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	check_alloc_code(code, 10);
	code_ptr out = code->cur;
	if (size == SZ_W) {
		*(out++) = PRE_SIZE;
	}
	if (size == SZ_Q || dst_base >= R8) {
		*out = PRE_REX;
		if (size == SZ_Q) {
			*out |= REX_QUAD;
		}
		if (dst_base >= R8) {
			*out |= REX_RM_FIELD;
			dst_base -= (R8 - X86_R8);
		}
		out++;
	}
	*(out++) = PRE_2BYTE;
	*(out++) = OP2_BTX_I;
	if (dst_disp < 128 && dst_disp >= -128) {
	*(out++) = MODE_REG_DISPLACE8 | dst_base | (op_ex << 3);
	*(out++) = dst_disp;
	} else {
		*(out++) = MODE_REG_DISPLACE32 | dst_base | (op_ex << 3);
		*(out++) = dst_disp;
		*(out++) = dst_disp >> 8;
		*(out++) = dst_disp >> 16;
		*(out++) = dst_disp >> 24;
	}
	*(out++) = val;
	code->cur = out;
}

void bt_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	return bit_rr(code, OP2_BT, src, dst, size);
}

void bt_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_rrdisp(code, OP2_BT, src, dst_base, dst_disp, size);
}

void bt_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	return bit_ir(code, OP_EX_BT, val, dst, size);
}

void bt_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_irdisp(code, OP_EX_BT, val, dst_base, dst_disp, size);
}

void bts_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	return bit_rr(code, OP2_BTS, src, dst, size);
}

void bts_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_rrdisp(code, OP2_BTS, src, dst_base, dst_disp, size);
}

void bts_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	return bit_ir(code, OP_EX_BTS, val, dst, size);
}

void bts_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_irdisp(code, OP_EX_BTS, val, dst_base, dst_disp, size);
}

void btr_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	return bit_rr(code, OP2_BTR, src, dst, size);
}

void btr_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_rrdisp(code, OP2_BTR, src, dst_base, dst_disp, size);
}

void btr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	return bit_ir(code, OP_EX_BTR, val, dst, size);
}

void btr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_irdisp(code, OP_EX_BTR, val, dst_base, dst_disp, size);
}

void btc_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size)
{
	return bit_rr(code, OP2_BTC, src, dst, size);
}

void btc_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_rrdisp(code, OP2_BTC, src, dst_base, dst_disp, size);
}

void btc_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size)
{
	return bit_ir(code, OP_EX_BTC, val, dst, size);
}

void btc_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size)
{
	return bit_irdisp(code, OP_EX_BTC, val, dst_base, dst_disp, size);
}

void jcc(code_info *code, uint8_t cc, code_ptr dest)
{
	check_alloc_code(code, 6);
	code_ptr out = code->cur;
	ptrdiff_t disp = dest-(out+2);
	if (disp <= 0x7F && disp >= -0x80) {
		*(out++) = OP_JCC | cc;
		*(out++) = disp;
	} else {
		disp = dest-(out+6);
		if (CHECK_DISP(disp)) {
			*(out++) = PRE_2BYTE;
			*(out++) = OP2_JCC | cc;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
		} else {
			fatal_error("jcc: %p - %p = %lX which is out of range for a 32-bit displacement\n", dest, out + 6, (long)disp);
		}
	}
	code->cur = out;
}

void jmp(code_info *code, code_ptr dest)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	ptrdiff_t disp = dest-(out+2);
	if (disp <= 0x7F && disp >= -0x80) {
		*(out++) = OP_JMP_BYTE;
		*(out++) = disp;
	} else {
		disp = dest-(out+5);
		if (CHECK_DISP(disp)) {
			*(out++) = OP_JMP;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
			disp >>= 8;
			*(out++) = disp;
		} else {
			fatal_error("jmp: %p - %p = %lX which is out of range for a 32-bit displacement\n", dest, out + 6, (long)disp);
		}
	}
	code->cur = out;
}

void jmp_r(code_info *code, uint8_t dst)
{
	check_alloc_code(code, 3);
	code_ptr out = code->cur;
	if (dst >= R8) {
		dst -= R8 - X86_R8;
		*(out++) = PRE_REX | REX_RM_FIELD;
	}
	*(out++) = OP_SINGLE_EA;
	*(out++) = MODE_REG_DIRECT | dst | (OP_EX_JMP_EA << 3);
	code->cur = out;
}

void jmp_rind(code_info *code, uint8_t dst)
{
	check_alloc_code(code, 3);
	code_ptr out = code->cur;
	if (dst >= R8) {
		dst -= R8 - X86_R8;
		*(out++) = PRE_REX | REX_RM_FIELD;
	}
	*(out++) = OP_SINGLE_EA;
	*(out++) = MODE_REG_INDIRECT | dst | (OP_EX_JMP_EA << 3);
	code->cur = out;
}

void call_noalign(code_info *code, code_ptr fun)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	ptrdiff_t disp = fun-(out+5);
	if (CHECK_DISP(disp)) {
		*(out++) = OP_CALL;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
	} else {
		//TODO: Implement far call???
		fatal_error("call: %p - %p = %lX which is out of range for a 32-bit displacement\n", fun, out + 5, (long)disp);
	}
	code->cur = out;
}

volatile int foo;
void call(code_info *code, code_ptr fun)
{
	foo = *fun;
	code->stack_off += sizeof(void *);
	int32_t adjust = 0;
	if (code->stack_off & 0xF) {
		adjust = 16 - (code->stack_off & 0xF);
		code->stack_off += adjust;
		sub_ir(code, adjust, RSP, SZ_PTR);
	}
	call_noalign(code, fun);
	if (adjust) {
		add_ir(code, adjust, RSP, SZ_PTR);
	}
	code->stack_off -= sizeof(void *) + adjust;
}
void call_raxfallback(code_info *code, code_ptr fun)
{
	check_alloc_code(code, 5);
	code_ptr out = code->cur;
	ptrdiff_t disp = fun-(out+5);
	if (CHECK_DISP(disp)) {
		*(out++) = OP_CALL;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
		disp >>= 8;
		*(out++) = disp;
		code->cur = out;
	} else {
		mov_ir(code, (int64_t)fun, RAX, SZ_PTR);
		call_r(code, RAX);
	}
}

void call_r(code_info *code, uint8_t dst)
{
	code->stack_off += sizeof(void *);
	int32_t adjust = 0;
	if (code->stack_off & 0xF) {
		adjust = 16 - (code->stack_off & 0xF);
		code->stack_off += adjust;
		sub_ir(code, adjust, RSP, SZ_PTR);
	}
	check_alloc_code(code, 2);
	code_ptr out = code->cur;
	*(out++) = OP_SINGLE_EA;
	*(out++) = MODE_REG_DIRECT | dst | (OP_EX_CALL_EA << 3);
	code->cur = out;
	if (adjust) {
		add_ir(code, adjust, RSP, SZ_PTR);
	}
	code->stack_off -= sizeof(void *) + adjust;
}

void retn(code_info *code)
{
	check_alloc_code(code, 1);
	code_ptr out = code->cur;
	*(out++) = OP_RETN;
	code->cur = out;
}

void rts(code_info *code)
{
	retn(code);
}

void cdq(code_info *code)
{
	check_alloc_code(code, 1);
	code_ptr out = code->cur;
	*(out++) = OP_CDQ;
	code->cur = out;
}

void loop(code_info *code, code_ptr dst)
{
	check_alloc_code(code, 2);
	code_ptr out = code->cur;
	ptrdiff_t disp = dst-(out+2);
	*(out++) = OP_LOOP;
	*(out++) = disp;
	code->cur = out;
}

uint32_t prep_args(code_info *code, uint32_t num_args, va_list args)
{
	uint8_t *arg_arr = malloc(num_args);
	for (int i = 0; i < num_args; i ++)
	{
		arg_arr[i] = va_arg(args, int);
	}
#ifdef X86_64
	uint32_t stack_args = 0;
#ifdef _WIN32
	//Microsoft is too good for the ABI that everyone else uses on x86-64 apparently
	uint8_t abi_regs[] = {RCX, RDX, R8, R9};
#else
	uint8_t abi_regs[] = {RDI, RSI, RDX, RCX, R8, R9};
#endif
	int8_t reg_swap[R15+1];
	uint32_t usage = 0;
	memset(reg_swap, -1, sizeof(reg_swap));
	for (int i = 0; i < num_args; i ++)
	{
		usage |= 1 << arg_arr[i];
	}
	for (int i = 0; i < num_args; i ++)
	{
		uint8_t reg_arg = arg_arr[i];
		if (i < sizeof(abi_regs)) {
			if (reg_swap[reg_arg] >= 0) {
				reg_arg = reg_swap[reg_arg];
			}
			if (reg_arg != abi_regs[i]) {
				if (usage & (1 << abi_regs[i])) {
					xchg_rr(code, reg_arg, abi_regs[i], SZ_PTR);
					reg_swap[abi_regs[i]] = reg_arg;
				} else {
					mov_rr(code, reg_arg, abi_regs[i], SZ_PTR);
				}
			}
		} else {
			arg_arr[stack_args++] = reg_arg;
		}
	}
#else
#define stack_args num_args
#endif
	uint32_t stack_off_call = code->stack_off + sizeof(void *) * (stack_args + 1);
	uint32_t adjust = 0;
	if (stack_off_call & 0xF) {
		adjust = 16 - (stack_off_call & 0xF);
		sub_ir(code, adjust, RSP, SZ_PTR);
		code->stack_off += adjust;
	}
	for (int i = stack_args -1; i >= 0; i--)
	{
		push_r(code, arg_arr[i]);
	}
	free(arg_arr);
#if defined(X86_64) && defined(_WIN32)
	sub_ir(code, 32, RSP, SZ_PTR);
	code->stack_off += 32;
	adjust += 32;
#endif
	
	return stack_args * sizeof(void *) + adjust;
}

void call_args(code_info *code, code_ptr fun, uint32_t num_args, ...)
{
	va_list args;
	va_start(args, num_args);
	uint32_t adjust = prep_args(code, num_args, args);
	va_end(args);
	call_raxfallback(code, fun);
	if (adjust) {
		add_ir(code, adjust, RSP, SZ_PTR);
		code->stack_off -= adjust;
	}
}

void call_args_r(code_info *code, uint8_t fun_reg, uint32_t num_args, ...)
{
	va_list args;
	va_start(args, num_args);
	uint32_t adjust = prep_args(code, num_args, args);
	va_end(args);
	call_r(code, fun_reg);
	if (adjust) {
		add_ir(code, adjust, RSP, SZ_PTR);
		code->stack_off -= adjust;
	}
}
/*
void call_args_abi(code_info *code, code_ptr fun, uint32_t num_args, ...)
{
	va_list args;
	va_start(args, num_args);
	uint32_t adjust = prep_args(code, num_args, args);
	va_end(args);
#ifdef X86_64
	test_ir(code, 8, RSP, SZ_PTR); //check stack alignment
	code_ptr do_adjust_rsp = code->cur + 1;
	jcc(code, CC_NZ, code->cur + 2);
#endif
	call_raxfallback(code, fun);
	if (adjust) {
		add_ir(code, adjust, RSP, SZ_PTR);
	}
#ifdef X86_64
	code_ptr no_adjust_rsp = code->cur + 1;
	jmp(code, code->cur + 2);
	*do_adjust_rsp = code->cur - (do_adjust_rsp+1);
	sub_ir(code, 8, RSP, SZ_PTR);
	call_raxfallback(code, fun);
	add_ir(code, adjust + 8 , RSP, SZ_PTR);
	*no_adjust_rsp = code->cur - (no_adjust_rsp+1);
#endif
}
*/
void save_callee_save_regs(code_info *code)
{
	push_r(code, RBX);
	push_r(code, RBP);
#ifdef X86_64
	push_r(code, R12);
	push_r(code, R13);
	push_r(code, R14);
	push_r(code, R15);
#endif
#if !defined(X86_64) || defined(_WIN32)
	push_r(code, RDI);
	push_r(code, RSI);
#endif
}

void restore_callee_save_regs(code_info *code)
{
#if !defined(X86_64) || defined(_WIN32)
	pop_r(code, RSI);
	pop_r(code, RDI);
#endif
#ifdef X86_64
	pop_r(code, R15);
	pop_r(code, R14);
	pop_r(code, R13);
	pop_r(code, R12);
#endif
	pop_r(code, RBP);
	pop_r(code, RBX);
}

uint8_t has_modrm(uint8_t prefix, uint8_t opcode)
{
	if (!prefix) {
		switch (opcode)
		{
		case OP_JMP:
		case OP_JMP_BYTE:
		case OP_JCC:
		case OP_CALL:
		case OP_RETN:
		case OP_LOOP:
		case OP_MOV_I8R:
		case OP_MOV_IR:
		case OP_PUSHF:
		case OP_POPF:
		case OP_PUSH:
		case OP_POP:
		case OP_CDQ:
			return 0;
		}
	} else if (prefix == PRE_2BYTE) {
		switch (opcode)
		{
		case OP2_JCC:
			return 0;
		}
	}
	return 1;
}

uint8_t has_sib(uint8_t mod_rm)
{
	uint8_t mode = mod_rm & 0xC0;
	uint8_t rm = mod_rm & 3;

	return mode != MODE_REG_DIRECT && rm == RSP;
}

uint32_t x86_inst_size(code_ptr start)
{
	code_ptr code = start;
	uint8_t cont = 1;
	uint8_t prefix = 0;
	uint8_t op_size = SZ_B;
	uint8_t main_op;

	while (cont)
	{
		if (*code == PRE_SIZE) {
			op_size = SZ_W;
		} else if (*code == PRE_REX) {
			if (*code & REX_QUAD) {
				op_size = SZ_Q;
			}
		} else if(*code == PRE_2BYTE || *code == PRE_XOP) {
			prefix = *code;
		} else {
			main_op = *code;
			cont = 0;
		}
		code++;
	}
	if (has_modrm(prefix, main_op)) {
		uint8_t mod_rm = *(code++);
		if (has_sib(mod_rm)) {
			//sib takes up a byte, but can't add any additional ones beyond that
			code++;
		}
		uint8_t mode = mod_rm & 0xC0;
		uint8_t rm = mod_rm & 3;
		if (mode == MODE_REG_DISPLACE8) {
			code++;
		} else if (mode == MODE_REG_DISPLACE32 || (mode == MODE_REG_INDIRECT && rm == RBP)) {
			code += 4;
		}
	} else {
	}

	return code-start;
}
