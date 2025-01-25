/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm. 
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef Z80INST_H_
#define Z80INST_H_

#include <stdint.h>

enum {
	Z80_LD,
	Z80_PUSH,
	Z80_POP,
	Z80_EX,
	Z80_EXX,
	Z80_LDI,
	Z80_LDIR,
	Z80_LDD,
	Z80_LDDR,
	Z80_CPI,
	Z80_CPIR,
	Z80_CPD,
	Z80_CPDR,
	Z80_ADD,
	Z80_ADC,
	Z80_SUB,
	Z80_SBC,
	Z80_AND,
	Z80_OR,
	Z80_XOR,
	Z80_CP,
	Z80_INC,
	Z80_DEC,
	Z80_DAA,
	Z80_CPL,
	Z80_NEG,
	Z80_CCF,
	Z80_SCF,
	Z80_NOP,
	Z80_HALT,
	Z80_DI,
	Z80_EI,
	Z80_IM,
	Z80_RLC,
	Z80_RL,
	Z80_RRC,
	Z80_RR,
	Z80_SLA,
	Z80_SRA,
	Z80_SLL,
	Z80_SRL,
	Z80_RLD,
	Z80_RRD,
	Z80_BIT,
	Z80_SET,
	Z80_RES,
	Z80_JP,
	Z80_JPCC,
	Z80_JR,
	Z80_JRCC,
	Z80_DJNZ,
	Z80_CALL,
	Z80_CALLCC,
	Z80_RET,
	Z80_RETCC,
	Z80_RETI,
	Z80_RETN,
	Z80_RST,
	Z80_IN,
	Z80_INI,
	Z80_INIR,
	Z80_IND,
	Z80_INDR,
	Z80_OUT,
	Z80_OUTI,
	Z80_OTIR,
	Z80_OUTD,
	Z80_OTDR,
	Z80_USE_MAIN
};

enum {
	Z80_C=0,
	Z80_B,
	Z80_E,
	Z80_D,
	Z80_L,
	Z80_H,
	Z80_IXL,
	Z80_IXH,
	Z80_IYL,
	Z80_IYH,
	Z80_I,
	Z80_R,
	Z80_A,
	Z80_BC,
	Z80_DE,
	Z80_HL,
	Z80_SP,
	Z80_AF,
	Z80_IX,
	Z80_IY,
	Z80_UNUSED
};

#define Z80_IMMED_FLAG 0x80
#define Z80_USE_IMMED (Z80_IMMED_FLAG|Z80_UNUSED)

enum {
	Z80_CC_NZ,
	Z80_CC_Z,
	Z80_CC_NC,
	Z80_CC_C,
	Z80_CC_PO,
	Z80_CC_PE,
	Z80_CC_P,
	Z80_CC_M
};

enum {
	Z80_REG,
	Z80_REG_INDIRECT,
	Z80_IMMED,
	Z80_IMMED_INDIRECT,
	Z80_IX_DISPLACE,
	Z80_IY_DISPLACE
};
#define Z80_DIR 0x80

typedef struct {
	uint8_t  op;
	uint8_t  reg;
	uint8_t  addr_mode;
	uint8_t  ea_reg;
	uint16_t immed;
	uint16_t  opcode_bytes;
} z80inst;

uint8_t * z80_decode(uint8_t * istream, z80inst * decoded);
int z80_disasm(z80inst * decoded, char * dst, uint16_t address);
uint8_t z80_high_reg(uint8_t reg);
uint8_t z80_low_reg(uint8_t reg);
uint8_t z80_word_reg(uint8_t reg);
uint8_t z80_is_terminal(z80inst * inst);

#endif //Z80INST_H_

