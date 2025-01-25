/*
 Copyright 2013 Michael Pavone
 This file is part of BlastEm.
 BlastEm is free software distributed under the terms of the GNU General Public License version 3 or greater. See COPYING for full license text.
*/
#ifndef GEN_X86_H_
#define GEN_X86_H_

#include <stdint.h>
#include "gen.h"

enum {
	RAX = 0,
	RCX,
	RDX,
	RBX,
	RSP,
	RBP,
	RSI,
	RDI,
	AH,
	CH,
	DH,
	BH,
	R8,
	R9,
	R10,
	R11,
	R12,
	R13,
	R14,
	R15
};

enum {
	CC_O = 0,
	CC_NO,
	CC_C,
	CC_B = CC_C,
	CC_NC,
	CC_NB = CC_NC,
	CC_Z,
	CC_NZ,
	CC_BE,
	CC_A,
	CC_S,
	CC_NS,
	CC_P,
	CC_NP,
	CC_L,
	CC_GE,
	CC_LE,
	CC_G
};

enum {
	SZ_B = 0,
	SZ_W,
	SZ_D,
	SZ_Q
};

#ifdef X86_64
#define SZ_PTR SZ_Q
#define MAX_INST_LEN 14
#ifdef _WIN32
#define FIRST_ARG_REG RCX
#define SECOND_ARG_REG RDX
#else
#define FIRST_ARG_REG RDI
#define SECOND_ARG_REG RSI
#endif
#else
#define SZ_PTR SZ_D
#define MAX_INST_LEN 11
#endif

enum {
	MODE_REG_INDIRECT = 0,
	MODE_REG_INDEXED = 4,
	MODE_REG_DISPLACE8 = 0x40,
	MODE_REG_INDEXED_DISPLACE8 = 0x44,
	MODE_REG_DISPLACE32 = 0x80,
	MODE_REG_INDEXED_DIPSLACE32 = 0x84,
	MODE_REG_DIRECT = 0xC0,
//"phony" mode
	MODE_IMMED = 0xFF
};

void rol_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void ror_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void rcl_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void rcr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void shl_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void shr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void sar_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void rol_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void ror_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void rcl_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void rcr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void shl_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void shr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void sar_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void rol_clr(code_info *code, uint8_t dst, uint8_t size);
void ror_clr(code_info *code, uint8_t dst, uint8_t size);
void rcl_clr(code_info *code, uint8_t dst, uint8_t size);
void rcr_clr(code_info *code, uint8_t dst, uint8_t size);
void shl_clr(code_info *code, uint8_t dst, uint8_t size);
void shr_clr(code_info *code, uint8_t dst, uint8_t size);
void sar_clr(code_info *code, uint8_t dst, uint8_t size);
void rol_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void ror_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void rcl_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void rcr_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void shl_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void shr_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void sar_clrdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void add_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void adc_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void or_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void xor_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void and_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void sub_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void sbb_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void cmp_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void add_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void adc_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void or_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void xor_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void and_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void sub_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void sbb_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void cmp_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void add_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void adc_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void or_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void xor_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void and_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void sub_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void sbb_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void cmp_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void add_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void adc_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void add_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void adc_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void or_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void or_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void xor_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void xor_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void and_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void and_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void sub_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void sub_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void sbb_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void sbb_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void cmp_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void cmp_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void imul_irr(code_info *code, int32_t val, uint8_t src, uint8_t dst, uint8_t size);
void imul_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void imul_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void imul_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void not_r(code_info *code, uint8_t dst, uint8_t size);
void neg_r(code_info *code, uint8_t dst, uint8_t size);
void not_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void neg_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void mul_r(code_info *code, uint8_t dst, uint8_t size);
void imul_r(code_info *code, uint8_t dst, uint8_t size);
void div_r(code_info *code, uint8_t dst, uint8_t size);
void idiv_r(code_info *code, uint8_t dst, uint8_t size);
void mul_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void imul_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void div_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void idiv_rdisp(code_info *code, uint8_t dst_base, int32_t disp, uint8_t size);
void test_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void test_ir(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void test_irdisp(code_info *code, int32_t val, uint8_t dst_base, int32_t disp, uint8_t size);
void test_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void test_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void mov_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void mov_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t disp, uint8_t size);
void mov_rdispr(code_info *code, uint8_t src_base, int32_t disp, uint8_t dst, uint8_t size);
void mov_rrindex(code_info *code, uint8_t src, uint8_t dst_base, uint8_t dst_index, uint8_t scale, uint8_t size);
void mov_rindexr(code_info *code, uint8_t src_base, uint8_t src_index, uint8_t scale, uint8_t dst, uint8_t size);
void mov_rrind(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void mov_rindr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void mov_ir(code_info *code, int64_t val, uint8_t dst, uint8_t size);
void mov_irdisp(code_info *code, int32_t val, uint8_t dst, int32_t disp, uint8_t size);
void mov_irind(code_info *code, int32_t val, uint8_t dst, uint8_t size);
void movsx_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t src_size, uint8_t size);
void movsx_rdispr(code_info *code, uint8_t src, int32_t disp, uint8_t dst, uint8_t src_size, uint8_t size);
void movzx_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t src_size, uint8_t size);
void movzx_rdispr(code_info *code, uint8_t src, int32_t disp, uint8_t dst, uint8_t src_size, uint8_t size);
void xchg_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void pushf(code_info *code);
void popf(code_info *code);
void push_r(code_info *code, uint8_t reg);
void push_rdisp(code_info *code, uint8_t base, int32_t disp);
void pop_r(code_info *code, uint8_t reg);
void pop_rind(code_info *code, uint8_t reg);
void setcc_r(code_info *code, uint8_t cc, uint8_t dst);
void setcc_rind(code_info *code, uint8_t cc, uint8_t dst);
void setcc_rdisp(code_info *code, uint8_t cc, uint8_t dst, int32_t disp);
void bt_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void bt_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void bt_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void bt_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void bts_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void bts_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void bts_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void bts_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void btr_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void btr_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void btr_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void btr_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void btc_rr(code_info *code, uint8_t src, uint8_t dst, uint8_t size);
void btc_rrdisp(code_info *code, uint8_t src, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void btc_ir(code_info *code, uint8_t val, uint8_t dst, uint8_t size);
void btc_irdisp(code_info *code, uint8_t val, uint8_t dst_base, int32_t dst_disp, uint8_t size);
void jcc(code_info *code, uint8_t cc, code_ptr dest);
void jmp_rind(code_info *code, uint8_t dst);
void call_noalign(code_info *code, code_ptr fun);
void call_r(code_info *code, uint8_t dst);
void retn(code_info *code);
void cdq(code_info *code);
void loop(code_info *code, code_ptr dst);
uint8_t is_mov_ir(code_ptr inst);

#endif //GEN_X86_H_

