/*  src/psp/rtlinsn.c: Instruction encoding routines for RTL
    Copyright 2009 Andrew Church

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

/*************************************************************************/

/*
 * This source file defines functions for encoding RTL instructions into
 * the RTLInsn structure based on the type of instruction (2-operand ALU,
 * memory load, and so on).  These functions are not exported directly, but
 * are instead called through a lookup table by rtlinsn_make(), an inline
 * function defined in rtl-internal.h.
 *
 * Both the inlining of rtlinsn_make() and the requirement of preloading
 * the opcode into insn->opcode (and then taking the RTLInsn pointer in
 * place of the opcode parameter to rtl_add_insn()) are to help
 * optimization of the fast path; for example, on MIPS architectures,
 * rtl_add_insn() can directly call the encoding function in this file
 * without having to pass through a call to rtlinsn_make() or reload the
 * parameter registers ($a0-$t1).
 */

/*************************************************************************/
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "rtl.h"
#include "rtl-internal.h"

/*************************************************************************/
/************************** Local declarations ***************************/
/*************************************************************************/

/* Encoding functions for specific opcode groups */

static int make_nop(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                    uintptr_t src1, uint32_t src2, unsigned int other);
static int make_alu_1op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                        uintptr_t src1, uint32_t src2, unsigned int other);
static int make_alu_2op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                        uintptr_t src1, uint32_t src2, unsigned int other);
static int make_alui_2op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other);
static int make_alu_2op_2dest(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                              uintptr_t src1, uint32_t src2, unsigned int other);
static int make_select(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                       uintptr_t src1, uint32_t src2, unsigned int other);
static int make_madd(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other);
static int make_bitfield(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other);
static int make_load_imm(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other);
static int make_load_addr(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other);
static int make_load_param(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                           uintptr_t src1, uint32_t src2, unsigned int other);
static int make_load(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other);
static int make_store(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                      uintptr_t src1, uint32_t src2, unsigned int other);
static int make_label(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                      uintptr_t src1, uint32_t src2, unsigned int other);
static int make_goto(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other);
static int make_goto_cond(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other);
static int make_goto_cond2(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                           uintptr_t src1, uint32_t src2, unsigned int other);
static int make_call(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other);
static int make_return(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                       uintptr_t src1, uint32_t src2, unsigned int other);
static int make_return_to(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other);


/* Encoding function table */

int (* const makefunc_table[])(RTLBlock *, RTLInsn *, unsigned int,
                               uintptr_t, uint32_t, unsigned int) = {
    [RTLOP_NOP        ] = make_nop,
    [RTLOP_MOVE       ] = make_alu_1op,
    [RTLOP_SELECT     ] = make_select,
    [RTLOP_ADD        ] = make_alu_2op,
    [RTLOP_SUB        ] = make_alu_2op,
    [RTLOP_MULU       ] = make_alu_2op_2dest,
    [RTLOP_MULS       ] = make_alu_2op_2dest,
    [RTLOP_MADDU      ] = make_madd,
    [RTLOP_MADDS      ] = make_madd,
    [RTLOP_DIVMODU    ] = make_alu_2op_2dest,
    [RTLOP_DIVMODS    ] = make_alu_2op_2dest,
    [RTLOP_AND        ] = make_alu_2op,
    [RTLOP_OR         ] = make_alu_2op,
    [RTLOP_XOR        ] = make_alu_2op,
    [RTLOP_NOT        ] = make_alu_1op,
    [RTLOP_SLL        ] = make_alu_2op,
    [RTLOP_SRL        ] = make_alu_2op,
    [RTLOP_SRA        ] = make_alu_2op,
    [RTLOP_ROR        ] = make_alu_2op,
    [RTLOP_CLZ        ] = make_alu_1op,
    [RTLOP_CLO        ] = make_alu_1op,
    [RTLOP_SLTU       ] = make_alu_2op,
    [RTLOP_SLTS       ] = make_alu_2op,
    [RTLOP_BSWAPH     ] = make_alu_1op,
    [RTLOP_BSWAPW     ] = make_alu_1op,
    [RTLOP_HSWAPW     ] = make_alu_1op,
    [RTLOP_ADDI       ] = make_alui_2op,
    [RTLOP_ANDI       ] = make_alui_2op,
    [RTLOP_ORI        ] = make_alui_2op,
    [RTLOP_XORI       ] = make_alui_2op,
    [RTLOP_SLLI       ] = make_alui_2op,
    [RTLOP_SRLI       ] = make_alui_2op,
    [RTLOP_SRAI       ] = make_alui_2op,
    [RTLOP_RORI       ] = make_alui_2op,
    [RTLOP_SLTUI      ] = make_alui_2op,
    [RTLOP_SLTSI      ] = make_alui_2op,
    [RTLOP_BFEXT      ] = make_bitfield,
    [RTLOP_BFINS      ] = make_bitfield,
    [RTLOP_LOAD_IMM   ] = make_load_imm,
    [RTLOP_LOAD_ADDR  ] = make_load_addr,
    [RTLOP_LOAD_PARAM ] = make_load_param,
    [RTLOP_LOAD_BS    ] = make_load,
    [RTLOP_LOAD_BU    ] = make_load,
    [RTLOP_LOAD_HS    ] = make_load,
    [RTLOP_LOAD_HU    ] = make_load,
    [RTLOP_LOAD_W     ] = make_load,
    [RTLOP_LOAD_PTR   ] = make_load,
    [RTLOP_STORE_B    ] = make_store,
    [RTLOP_STORE_H    ] = make_store,
    [RTLOP_STORE_W    ] = make_store,
    [RTLOP_STORE_PTR  ] = make_store,
    [RTLOP_LABEL      ] = make_label,
    [RTLOP_GOTO       ] = make_goto,
    [RTLOP_GOTO_IF_Z  ] = make_goto_cond,
    [RTLOP_GOTO_IF_NZ ] = make_goto_cond,
    [RTLOP_GOTO_IF_E  ] = make_goto_cond2,
    [RTLOP_GOTO_IF_NE ] = make_goto_cond2,
    [RTLOP_CALL       ] = make_call,
    [RTLOP_RETURN     ] = make_return,
    [RTLOP_RETURN_TO  ] = make_return_to,
};

/*-----------------------------------------------------------------------*/

/* Macro to mark a given register live, updating birth/death fields too
 * (requires block and insn_index to be separately defined) */
#define MARK_LIVE(reg,index)  do {              \
    RTLRegister * const __reg = (reg);          \
    const unsigned int __index = (index);       \
    if (!__reg->live) {                         \
        __reg->live = 1;                        \
        __reg->birth = insn_index;              \
        if (!block->last_live_reg) {            \
            block->first_live_reg = __index;    \
        } else {                                \
            block->regs[block->last_live_reg].live_link = __index; \
        }                                       \
        block->last_live_reg = __index;         \
        reg->live_link = 0;                     \
    }                                           \
    __reg->death = insn_index;                  \
} while (0)

/*************************************************************************/
/************************** Routine definitions **************************/
/*************************************************************************/

/**
 * make_nop:  Encode a NOP instruction.
 */
static int make_nop(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                    uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(insn != NULL, return 0);

    insn->src_imm = src1;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_alu_1op:  Encode a 1-operand ALU instruction.
 */
static int make_alu_1op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                        uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
#endif

    insn->dest = dest;
    // FIXME: why does GCC waste time with an ANDI temp,arg,0xFFFF for
    // stores like this src1 (but not with dest)?
    insn->src1 = src1;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
    if (insn->opcode == RTLOP_MOVE) {
        destreg->unique_pointer = src1reg->unique_pointer;
    } else {
        destreg->unique_pointer = 0;
    }
    destreg->result.opcode = insn->opcode;
    destreg->result.second_res = 0;
    destreg->result.is_imm = 0;
    destreg->result.src1 = src1;
    destreg->result.src2 = 0;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_alu_2op:  Encode a 2-operand ALU instruction.
 */
static int make_alu_2op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                        uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(src2 != 0 && src2 < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    RTLRegister * const src2reg = &block->regs[src2];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
    destreg->unique_pointer = 0;
    destreg->result.opcode = insn->opcode;
    destreg->result.second_res = 0;
    destreg->result.is_imm = 0;
    destreg->result.src1 = src1;
    destreg->result.src2 = src2;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);
    MARK_LIVE(src2reg, src2);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_alui_2op:  Encode a 2-operand register-immediate ALU instruction.
 */
static int make_alui_2op(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src_imm = src2;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
    destreg->unique_pointer = 0;
    destreg->result.opcode = insn->opcode;
    destreg->result.second_res = 0;
    destreg->result.is_imm = 1;
    destreg->result.src1 = src1;
    destreg->result.imm = src2;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_alu_2op_2dest:  Encode a 2-operand, 2-destination ALU instruction.
 */
static int make_alu_2op_2dest(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                              uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(src2 != 0 && src2 < block->next_reg, return 0);
    PRECOND(other < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;
    insn->dest2 = other;

    RTLRegister * const destreg  = &block->regs[dest];
    RTLRegister * const src1reg  = &block->regs[src1];
    RTLRegister * const src2reg  = &block->regs[src2];
    RTLRegister * const dest2reg = &block->regs[other];
    const uint32_t insn_index = block->num_insns;
    if (dest != 0) {
        destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
        destreg->unique_pointer = 0;
        destreg->result.opcode = insn->opcode;
        destreg->result.second_res = 0;
        destreg->result.is_imm = 0;
        destreg->result.src1 = src1;
        destreg->result.src2 = src2;
        MARK_LIVE(destreg, dest);
    }
    if (other != 0) {
        dest2reg->source = dest2reg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
        dest2reg->unique_pointer = 0;
        dest2reg->result.opcode = insn->opcode;
        dest2reg->result.second_res = 1;
        dest2reg->result.is_imm = 0;
        dest2reg->result.src1 = src1;
        dest2reg->result.src2 = src2;
        MARK_LIVE(dest2reg, other);
    }
    MARK_LIVE(src1reg, src1);
    MARK_LIVE(src2reg, src2);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_select:  Encode a SELECT instruction.
 */
static int make_select(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                       uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(src2 != 0 && src2 < block->next_reg, return 0);
    PRECOND(other != 0 && other < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;
    insn->cond = other;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    RTLRegister * const src2reg = &block->regs[src2];
    RTLRegister * const condreg = &block->regs[other];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
    destreg->unique_pointer = 0;
    destreg->result.opcode = insn->opcode;
    destreg->result.second_res = 0;
    destreg->result.is_imm = 0;
    destreg->result.src1 = src1;
    destreg->result.src2 = src2;
    destreg->result.cond = other;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);
    MARK_LIVE(src2reg, src2);
    MARK_LIVE(condreg, other);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_madd:  Encode an MADDU or MADDS instruction.
 */
static int make_madd(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(src2 != 0 && src2 < block->next_reg, return 0);
    PRECOND(other != 0 && other < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;
    insn->dest2 = other;

    RTLRegister * const destreg  = &block->regs[dest];
    RTLRegister * const src1reg  = &block->regs[src1];
    RTLRegister * const src2reg  = &block->regs[src2];
    RTLRegister * const dest2reg = &block->regs[other];
    const uint32_t insn_index = block->num_insns;
    destreg->source = RTLREG_UNKNOWN;  // Always UNKNOWN, since we modify it
    destreg->unique_pointer = 0;
    MARK_LIVE(destreg, dest);
    dest2reg->source = RTLREG_UNKNOWN;
    dest2reg->unique_pointer = 0;
    MARK_LIVE(dest2reg, other);
    MARK_LIVE(src1reg, src1);
    MARK_LIVE(src2reg, src2);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_bitfield:  Encode a bitfield instruction.
 */
static int make_bitfield(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
    const int start = other & 0xFF;
    const int count = (other >> 8) & 0xFF;
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND((insn->opcode != RTLOP_BFINS || src2 != 0) && src2 < block->next_reg, return 0);
    PRECOND(start < 32, return 0);
    PRECOND(count <= 32 - start, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;
    insn->bitfield.start = start;
    insn->bitfield.count = count;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    RTLRegister * const src2reg = &block->regs[src2];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_RESULT;
    destreg->unique_pointer = 0;
    destreg->result.opcode = insn->opcode;
    destreg->result.second_res = 0;
    destreg->result.is_imm = 0;
    destreg->result.src1 = src1;
    destreg->result.src2 = src2;
    destreg->result.start = start;
    destreg->result.count = count;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);
    if (src2) {
        MARK_LIVE(src2reg, src2);
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_load_imm:  Encode a LOAD_IMM instruction.
 */
static int make_load_imm(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                         uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src_imm = src1;

    RTLRegister * const destreg = &block->regs[dest];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_CONSTANT;
    destreg->unique_pointer = 0;
    destreg->value = src1;
    MARK_LIVE(destreg, dest);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_load_addr:  Encode a LOAD_ADDR instruction.
 */
static int make_load_addr(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src_addr = src1;

    RTLRegister * const destreg = &block->regs[dest];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_CONSTANT;
    destreg->unique_pointer = 0;
    destreg->value = src1;
    MARK_LIVE(destreg, dest);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_load_param:  Encode a LOAD_PARAM instruction.
 */
static int make_load_param(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                           uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
#endif

    insn->dest = dest;
    insn->src_imm = src1;

    RTLRegister * const destreg = &block->regs[dest];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_PARAMETER;
    destreg->unique_pointer = 0;
    destreg->param_index = src1;
    MARK_LIVE(destreg, dest);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_load:  Encode a memory load instruction.
 */
static int make_load(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND((int)other >= -0x8000 && (int)other <= 0x7FFF, return 0);
#endif

    /* Lookup tables for destreg->memory.{size,is_signed} */
    static const uint8_t size_lookup[] = {
        [RTLOP_LOAD_BU ] = 1, [RTLOP_LOAD_BS] = 1,
        [RTLOP_LOAD_HU ] = 2, [RTLOP_LOAD_HS] = 2,
        [RTLOP_LOAD_W  ] = 4,
        [RTLOP_LOAD_PTR] = sizeof(void *),
    };
    static const uint8_t is_signed_lookup[] = {
        [RTLOP_LOAD_BS] = 1,
        [RTLOP_LOAD_HS] = 1,
    };

    insn->dest = dest;
    insn->src1 = src1;
    insn->offset = other;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    const uint32_t insn_index = block->num_insns;
    destreg->source = destreg->source ? RTLREG_UNKNOWN : RTLREG_MEMORY;
    destreg->unique_pointer = 0;
    destreg->memory.addr_reg = src1;
    destreg->memory.offset = other;
    const unsigned int opcode = insn->opcode;
    destreg->memory.size = size_lookup[opcode];
    destreg->memory.is_signed = is_signed_lookup[opcode];
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_store:  Encode a memory store instruction.
 */
static int make_store(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                      uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest != 0 && dest < block->next_reg, return 0);
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND((int)other >= -0x8000 && (int)other <= 0x7FFF, return 0);
#endif

    insn->dest = dest;
    insn->src1 = src1;
    insn->offset = other;

    RTLRegister * const destreg = &block->regs[dest];
    RTLRegister * const src1reg = &block->regs[src1];
    const uint32_t insn_index = block->num_insns;
    MARK_LIVE(destreg, dest);
    MARK_LIVE(src1reg, src1);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_label:  Encode a LABEL instruction.
 */
static int make_label(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                      uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->cur_unit >= 0 && block->cur_unit < block->num_units,
            return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(other != 0 && other < block->next_label, return 0);
#endif

    insn->label = other;

    /* If this is _not_ the first instruction in the current basic
     * unit, end it and create a new basic unit starting here, since
     * there could potentially be a branch to this location. */
    if (block->units[block->cur_unit].first_insn != block->num_insns) {
        block->units[block->cur_unit].last_insn = block->num_insns - 1;
        if (UNLIKELY(!rtlunit_add(block))) {
            DMSG("%p/%u: Failed to start a new basic unit",
                 block, block->num_insns);
            return 0;
        }
        const uint32_t new_unit = block->num_units - 1;
        if (UNLIKELY(!rtlunit_add_edge(block, block->cur_unit, new_unit))){
            DMSG("%p/%u: Failed to add edge %u->%u", block,
                 block->num_insns, block->cur_unit, new_unit);
            return 0;
        }
        block->cur_unit = new_unit;
        block->units[block->cur_unit].first_insn = block->num_insns;
    }

    /* Save the label's unit number in the label-to-unit map */
    block->label_unitmap[insn->label] = block->cur_unit;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_goto:  Encode a GOTO instruction.
 */
static int make_goto(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(other != 0 && other < block->next_label, return 0);
#endif

    insn->label = other;

    /* Terminate the current basic unit after this instruction */
    block->units[block->cur_unit].last_insn = block->num_insns;
    block->have_unit = 0;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_goto_cond:  Encode a GOTO_IF_Z or GOTO_IF_NZ instruction.
 */
static int make_goto_cond(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(other != 0 && other < block->next_label, return 0);
#endif

    insn->src1 = src1;
    insn->label = other;

    RTLRegister * const src1reg = &block->regs[src1];
    const uint32_t insn_index = block->num_insns;
    MARK_LIVE(src1reg, src1);

    /* Terminate the current basic unit after this instruction, and
     * start a new basic unit with an edge connecting from this one */
    block->units[block->cur_unit].last_insn = block->num_insns;
    if (UNLIKELY(!rtlunit_add(block))) {
        DMSG("%p/%u: Failed to start a new basic unit",
             block, block->num_insns);
        return 0;
    }
    const unsigned int new_unit = block->num_units - 1;
    if (UNLIKELY(!rtlunit_add_edge(block, block->cur_unit, new_unit))) {
        DMSG("%p/%u: Failed to add edge %u->%u", block, block->num_insns,
             block->cur_unit, new_unit);
        return 0;
    }
    block->cur_unit = new_unit;
    block->units[block->cur_unit].first_insn = block->num_insns + 1;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_goto_cond2:  Encode a GOTO_IF_E or GOTO_IF_NE instruction.
 */
static int make_goto_cond2(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                           uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(src1 != 0 && src1 < block->next_reg, return 0);
    PRECOND(src2 != 0 && src2 < block->next_reg, return 0);
    PRECOND(other != 0 && other < block->next_label, return 0);
#endif

    insn->src1 = src1;
    insn->src2 = src2;
    insn->label = other;

    RTLRegister * const src1reg = &block->regs[src1];
    RTLRegister * const src2reg = &block->regs[src2];
    const uint32_t insn_index = block->num_insns;
    MARK_LIVE(src1reg, src1);
    MARK_LIVE(src2reg, src2);

    block->units[block->cur_unit].last_insn = block->num_insns;
    if (UNLIKELY(!rtlunit_add(block))) {
        DMSG("%p/%u: Failed to start a new basic unit",
             block, block->num_insns);
        return 0;
    }
    const unsigned int new_unit = block->num_units - 1;
    if (UNLIKELY(!rtlunit_add_edge(block, block->cur_unit, new_unit))) {
        DMSG("%p/%u: Failed to add edge %u->%u", block, block->num_insns,
             block->cur_unit, new_unit);
        return 0;
    }
    block->cur_unit = new_unit;
    block->units[block->cur_unit].first_insn = block->num_insns + 1;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_call:  Encode a CALL instruction.
 */
static int make_call(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                     uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);
#ifdef OPERAND_SANITY_CHECKS
    PRECOND(dest < block->next_reg, return 0);
    PRECOND(!(src1 == 0 && src2 != 0) && src1 < block->next_reg, return 0);
    PRECOND(src2 < block->next_reg, return 0);
    PRECOND(other != 0 && other < block->next_reg, return 0);
#endif

    RTLRegister * const destreg   = &block->regs[dest];
    RTLRegister * const src1reg   = &block->regs[src1];
    RTLRegister * const src2reg   = &block->regs[src2];
    RTLRegister * const targetreg = &block->regs[other];
    const uint32_t insn_index = block->num_insns;
    insn->dest = dest;
    insn->src1 = src1;
    insn->src2 = src2;
    insn->target = other;
    if (dest) {
        destreg->source = RTLREG_UNKNOWN;
        destreg->unique_pointer = 0;
        MARK_LIVE(destreg, dest);
    }
    if (src1) {
        MARK_LIVE(src1reg, src1);
    }
    if (src2) {
        MARK_LIVE(src2reg, src2);
    }
    MARK_LIVE(targetreg, other);

    const int cur_unit = block->cur_unit;
    if (block->first_call_unit < 0) {
        block->first_call_unit = cur_unit;
    }
    if (block->last_call_unit < cur_unit) {
        if (block->last_call_unit >= 0) {
            block->units[block->last_call_unit].next_call_unit = cur_unit;
            block->units[cur_unit].prev_call_unit = block->last_call_unit;
        } else {
            block->units[cur_unit].prev_call_unit = -1;
        }
        block->last_call_unit = cur_unit;
    }
    block->units[cur_unit].next_call_unit = -1;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_return:  Encode a RETURN instruction.
 */
static int make_return(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                       uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);

    /* Terminate the current basic unit, like GOTO */
    block->units[block->cur_unit].last_insn = block->num_insns;
    block->have_unit = 0;

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * make_return_to:  Encode a RETURN_TO instruction.
 */
static int make_return_to(RTLBlock *block, RTLInsn *insn, unsigned int dest,
                          uintptr_t src1, uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn != NULL, return 0);

    RTLRegister * const targetreg = &block->regs[other];
    const uint32_t insn_index = block->num_insns;
    insn->target = other;
    MARK_LIVE(targetreg, other);

    /* Terminate the current basic unit, like GOTO */
    block->units[block->cur_unit].last_insn = block->num_insns;
    block->have_unit = 0;

    return 1;
}

/*************************************************************************/
/*************************************************************************/

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
