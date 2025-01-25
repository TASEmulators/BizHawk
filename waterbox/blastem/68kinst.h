/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef M68KINST_H_
#define M68KINST_H_

#include <stdint.h>

#ifdef M68030
#define M68020
#endif
#ifdef M68020
#define M68010
#endif

typedef enum {
	BIT_MOVEP_IMMED = 0,
	MOVE_BYTE,
	MOVE_LONG,
	MOVE_WORD,
	MISC,
	QUICK_ARITH_LOOP,
	BRANCH,
	MOVEQ,
	OR_DIV_SBCD,
	SUB_SUBX,
	A_LINE,
	CMP_XOR,
	AND_MUL_ABCD_EXG,
	ADD_ADDX,
	SHIFT_ROTATE,
	F_LINE
} m68k_optypes;

typedef enum {
	M68K_ABCD,
	M68K_ADD,
	M68K_ADDX,
	M68K_AND,
	M68K_ANDI_CCR,
	M68K_ANDI_SR,
	M68K_ASL,
	M68K_ASR,
	M68K_BCC,
	M68K_BCHG,
	M68K_BCLR,
	M68K_BSET,
	M68K_BSR,
	M68K_BTST,
	M68K_CHK,
	M68K_CLR,
	M68K_CMP,
	M68K_DBCC,
	M68K_DIVS,
	M68K_DIVU,
	M68K_EOR,
	M68K_EORI_CCR,
	M68K_EORI_SR,
	M68K_EXG,
	M68K_EXT,
	M68K_ILLEGAL,
	M68K_JMP,
	M68K_JSR,
	M68K_LEA,
	M68K_LINK,
	M68K_LSL,
	M68K_LSR,
	M68K_MOVE,
	M68K_MOVE_CCR,
	M68K_MOVE_FROM_SR,
	M68K_MOVE_SR,
	M68K_MOVE_USP,
	M68K_MOVEM,
	M68K_MOVEP,
	M68K_MULS,
	M68K_MULU,
	M68K_NBCD,
	M68K_NEG,
	M68K_NEGX,
	M68K_NOP,
	M68K_NOT,
	M68K_OR,
	M68K_ORI_CCR,
	M68K_ORI_SR,
	M68K_PEA,
	M68K_RESET,
	M68K_ROL,
	M68K_ROR,
	M68K_ROXL,
	M68K_ROXR,
	M68K_RTE,
	M68K_RTR,
	M68K_RTS,
	M68K_SBCD,
	M68K_SCC,
	M68K_STOP,
	M68K_SUB,
	M68K_SUBX,
	M68K_SWAP,
	M68K_TAS,
	M68K_TRAP,
	M68K_TRAPV,
	M68K_TST,
	M68K_UNLK,
	M68K_INVALID,
	M68K_A_LINE_TRAP,
	M68K_F_LINE_TRAP,
#ifdef M68010
	M68K_BKPT,
	M68K_MOVE_FROM_CCR,
	M68K_MOVEC,
	M68K_MOVES,
	M68K_RTD,
#endif
#ifdef M68020
	M68K_BFCHG,
	M68K_BFCLR,
	M68K_BFEXTS,
	M68K_BFEXTU,
	M68K_BFFFO,
	M68K_BFINS,
	M68K_BFSET,
	M68K_BFTST,
	M68K_CALLM,
	M68K_CAS,
	M68K_CAS2,
	M68K_CHK2,
	M68K_CMP2,
	M68K_CP_BCC,
	M68K_CP_DBCC,
	M68K_CP_GEN,
	M68K_CP_RESTORE,
	M68K_CP_SAVE,
	M68K_CP_SCC,
	M68K_CP_TRAPCC,
	M68K_DIVSL,
	M68K_DIVUL,
	M68K_EXTB,
	M68K_PACK,
	M68K_RTM,
	M68K_TRAPCC,
	M68K_UNPK,
#endif
} m68K_op;

typedef enum {
	VAR_NORMAL,
	VAR_QUICK,
	VAR_IMMEDIATE,
	VAR_BYTE,
	VAR_WORD,
	VAR_LONG
} m68K_variant;

typedef enum {
	OPSIZE_BYTE=0,
	OPSIZE_WORD,
	OPSIZE_LONG,
	OPSIZE_INVALID,
	OPSIZE_UNSIZED
} m68K_opsizes;

typedef enum {
//actual addressing mode field values
	MODE_REG = 0,
	MODE_AREG,
	MODE_AREG_INDIRECT,
	MODE_AREG_POSTINC,
	MODE_AREG_PREDEC,
	MODE_AREG_DISPLACE,
	MODE_AREG_INDEX_MEM, //bunch of relatively complicated modes
	MODE_PC_INDIRECT_ABS_IMMED, //Modes that use the program counter, an absolute address or immediate value
//expanded values
	MODE_AREG_INDEX_DISP8,
#ifdef M68020
	MODE_AREG_INDEX_BASE_DISP,
	MODE_AREG_PREINDEX,
	MODE_AREG_POSTINDEX,
	MODE_AREG_MEM_INDIRECT,
	MODE_AREG_BASE_DISP,
	MODE_INDEX_BASE_DISP,
	MODE_PREINDEX,
	MODE_POSTINDEX,
	MODE_MEM_INDIRECT,
	MODE_BASE_DISP,
#endif
	MODE_ABSOLUTE_SHORT,
	MODE_ABSOLUTE,
	MODE_PC_DISPLACE,
	MODE_PC_INDEX_DISP8,
#ifdef M68020
	MODE_PC_INDEX_BASE_DISP,
	MODE_PC_PREINDEX,
	MODE_PC_POSTINDEX,
	MODE_PC_MEM_INDIRECT,
	MODE_PC_BASE_DISP,
	MODE_ZPC_INDEX_BASE_DISP,
	MODE_ZPC_PREINDEX,
	MODE_ZPC_POSTINDEX,
	MODE_ZPC_MEM_INDIRECT,
	MODE_ZPC_BASE_DISP,
#endif
	MODE_IMMEDIATE,
	MODE_IMMEDIATE_WORD,//used to indicate an immediate operand that only uses a single extension word even for a long operation
	MODE_UNUSED
} m68k_addr_modes;
#ifdef M68020
#define M68K_FLAG_BITFIELD 0x80
#endif

typedef enum {
	COND_TRUE,
	COND_FALSE,
	COND_HIGH,
	COND_LOW_SAME,
	COND_CARRY_CLR,
	COND_CARRY_SET,
	COND_NOT_EQ,
	COND_EQ,
	COND_OVERF_CLR,
	COND_OVERF_SET,
	COND_PLUS,
	COND_MINUS,
	COND_GREATER_EQ,
	COND_LESS,
	COND_GREATER,
	COND_LESS_EQ
} m68K_condition;

#ifdef M68010
typedef enum {
	CR_SFC,
	CR_DFC,
#ifdef M68020
	CR_CACR,
#endif
	CR_USP,
	CR_VBR,
#ifdef M68020
	CR_CAAR,
	CR_MSP,
	CR_ISP
#endif
} m68k_control_reg;

#ifdef M68020
#define MAX_HIGH_CR 0x804
#define MAX_LOW_CR 0x002
#else
#define MAX_HIGH_CR 0x801
#define MAX_LOW_CR 0x001
#endif

#endif

typedef struct {
#ifdef M68020
	uint16_t bitfield;
#endif
	uint8_t  addr_mode;
	union {
		struct {
			uint8_t pri;
			uint8_t sec;
#ifdef M68020
			uint8_t scale;
			uint8_t disp_sizes;
#endif
			int32_t displacement;
#ifdef M68020
			int32_t outer_disp;
#endif
		} regs;
		uint32_t immed;
	} params;
} m68k_op_info;

typedef struct m68kinst {
	uint8_t op;
	uint8_t variant;
	union {
		uint8_t size;
		uint8_t cond;
	} extra;
	uint8_t bytes;
	uint32_t address;
	m68k_op_info src;
	m68k_op_info dst;
} m68kinst;

typedef enum {
	VECTOR_RESET_STACK,
	VECTOR_RESET_PC,
	VECTOR_ACCESS_FAULT,
	VECTOR_ADDRESS_ERROR,
	VECTOR_ILLEGAL_INST,
	VECTOR_INT_DIV_ZERO,
	VECTOR_CHK,
	VECTOR_TRAPV,
	VECTOR_PRIV_VIOLATION,
	VECTOR_TRACE,
	VECTOR_LINE_1010,
	VECTOR_LINE_1111,
	VECTOR_COPROC_VIOLATION=13,
	VECTOR_FORMAT_ERROR,
	VECTOR_UNINIT_INTERRUPT,
	VECTOR_SPURIOUS_INTERRUPT=24,
	VECTOR_INT_1,
	VECTOR_INT_2,
	VECTOR_INT_3,
	VECTOR_INT_4,
	VECTOR_INT_5,
	VECTOR_INT_6,
	VECTOR_INT_7,
	VECTOR_TRAP_0,
	VECTOR_TRAP_1,
	VECTOR_TRAP_2,
	VECTOR_TRAP_3,
	VECTOR_TRAP_4,
	VECTOR_TRAP_5,
	VECTOR_TRAP_6,
	VECTOR_TRAP_7,
	VECTOR_TRAP_8,
	VECTOR_TRAP_9,
	VECTOR_TRAP_10,
	VECTOR_TRAP_11,
	VECTOR_TRAP_12,
	VECTOR_TRAP_13,
	VECTOR_TRAP_14,
	VECTOR_TRAP_15,
	VECTOR_USER0 = 64
} m68k_vector;

typedef int (*format_label_fun)(char * dst, uint32_t address, void * data);

uint16_t * m68k_decode(uint16_t * istream, m68kinst * dst, uint32_t address);
uint32_t m68k_branch_target(m68kinst * inst, uint32_t *dregs, uint32_t *aregs);
uint8_t m68k_is_branch(m68kinst * inst);
uint8_t m68k_is_noncall_branch(m68kinst * inst);
int m68k_disasm(m68kinst * decoded, char * dst);
int m68k_disasm_labels(m68kinst * decoded, char * dst, format_label_fun label_fun, void * data);
int m68k_default_label_fun(char * dst, uint32_t address, void * data);

#endif

