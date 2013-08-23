/*  src/psp/rtlopt.c: Optimization processing for RTL
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
 * This source file contains code for the various transformations applied
 * to RTL code to produce more optimized instruction streams.  The
 * currently implemented transformations are:
 *
 * Constant folding/propagation (rtlopt_fold_constants)
 * ----------------------------------------------------
 * Replaces instructions that operate on constant operands with LOAD_IMM
 * (load immediate) instructions that load the result of the operation
 * directly into the target register, eliminating the operation itself and
 * allowing the constant result to be propagated further.  Source operands
 * that are not used elsewhere are eliminated entirely, with the
 * instructions that loaded them converted to NOPs.
 *
 * While most constant expressions will be resolved by the source
 * translator at RTL generation time, this optimization step allows macros
 * in particular to take faster paths when they are called with constant
 * parameters.
 *
 * Deconditioning (rtlopt_decondition)
 * ----------------------------------
 * Converts conditional jumps (GOTO_IF_Z, GOTO_IF_NZ, GOTO_IF_E, GOTO_IF_NE)
 * whose test result is a constant to either unconditional jumps (GOTO) or
 * NOPs, depending on the instruction and the result of the test.
 *
 * Dead unit removal (rtlopt_drop_dead_units)
 * ------------------------------------------
 * Removes unreachable basic units from the RTL code stream.
 *
 * Useless branch removal (rtlopt_drop_dead_branches)
 * --------------------------------------------------
 * Replaces branch instructions that branch to the next instruction in the
 * code stream with NOPs.
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

static inline int fold_one_register(RTLBlock * const block,
                                    RTLRegister * const reg);
static void maybe_eliminate_folded_register(RTLBlock * const block,
                                            RTLRegister * const reg,
                                            const unsigned int reg_index);

static void drop_dead_unit(RTLBlock * const block,
                           const unsigned int unit_index);

/*************************************************************************/
/*********************** Library-internal routines ***********************/
/*************************************************************************/

/**
 * rtlopt_fold_constants:  Perform constant folding on the given RTL block,
 * converting instructions that operate on constant operands into load-
 * immediate instructions that load the result of the operation.  If such
 * an operand is not used by any other instruction, the instruction that
 * loaded it is changed to a NOP.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtlopt_fold_constants(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);

    unsigned int reg_index;
    for (reg_index = 1; reg_index < block->next_reg; reg_index++) {
        RTLRegister * const reg = &block->regs[reg_index];
        if (reg->live && reg->source == RTLREG_RESULT) {
            fold_one_register(block, reg);
        }
    }

    return 1;
}

/*************************************************************************/

/**
 * rtlopt_decondition:  Perform "deconditioning" of conditional branches
 * with constant conditions.  For "GOTO_IF_Z (GOTO_IF_NZ) label, rN" where
 * rN is type RTLREG_CONSTANT, the instruction is changed to GOTO if the
 * value of rN is zero (nonzero) and changed to NOP otherwise; similarly
 * for "GOTO_IF_E (GOTO_IF_NE) label, rX, rY".  As with constant folding,
 * if a condition register is not used anywhere else, the register is
 * eliminated and the instruction that loaded it is changed to a NOP.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtlopt_decondition(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);

    /* A conditional branch always ends an basic unit and has two targets,
     * so we only need to check the last instruction of each unit with two
     * exit edges. */

    unsigned int unit_index;
    for (unit_index = 0; unit_index < block->num_units; unit_index++) {
        RTLUnit * const unit = &block->units[unit_index];
        if (unit->exits[1] != -1) {
            RTLInsn * const insn = &block->insns[unit->last_insn];
            const RTLOpcode opcode = insn->opcode;

            if (opcode == RTLOP_GOTO_IF_Z || opcode == RTLOP_GOTO_IF_NZ) {
                const unsigned int reg_index = insn->src1;
                RTLRegister * const condition_reg = &block->regs[reg_index];
                if (condition_reg->source == RTLREG_CONSTANT) {
                    const uintptr_t condition = condition_reg->value;
                    const int fallthrough_index =
                        (unit->exits[0] == unit_index+1) ? 0 : 1;
                    if ((opcode == RTLOP_GOTO_IF_Z && condition == 0)
                     || (opcode == RTLOP_GOTO_IF_NZ && condition != 0)
                    ) {
                        /* Branch always taken: convert to GOTO */
#ifdef RTL_TRACE_GENERATE
                        fprintf(stderr, "[RTL] %p/%u: Branch always taken,"
                                " convert to GOTO and drop edge %u->%u\n",
                                block, unit->last_insn, unit_index,
                                unit->exits[fallthrough_index]);
#endif
                        insn->opcode = RTLOP_GOTO;
                        rtlunit_remove_edge(block, unit_index,
                                            fallthrough_index);
                    } else {
                        /* Branch never taken: convert to NOP */
#ifdef RTL_TRACE_GENERATE
                        fprintf(stderr, "[RTL] %p/%u: Branch never taken,"
                                " convert to NOP and drop edge %u->%u\n",
                                block, unit->last_insn, unit_index,
                                unit->exits[fallthrough_index ^ 1]);
#endif
                        insn->opcode = RTLOP_NOP;
                        insn->src_imm = 0;
                        rtlunit_remove_edge(block, unit_index,
                                            fallthrough_index ^ 1);
                    }
                    if (condition_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, condition_reg,
                                                        reg_index);
                    }
                }

            } else if (opcode == RTLOP_GOTO_IF_E || opcode == RTLOP_GOTO_IF_NE){
                const unsigned int src1_index = insn->src1;
                const unsigned int src2_index = insn->src1;
                RTLRegister * const src1_reg = &block->regs[src1_index];
                RTLRegister * const src2_reg = &block->regs[src2_index];
                if (src1_reg->source == RTLREG_CONSTANT
                 && src2_reg->source == RTLREG_CONSTANT
                ) {
                    const uintptr_t condition =
                        src1_reg->value - src2_reg->value;
                    const int fallthrough_index =
                        (unit->exits[0] == unit_index+1) ? 0 : 1;
                    if ((opcode == RTLOP_GOTO_IF_Z && condition == 0)
                     || (opcode == RTLOP_GOTO_IF_NZ && condition != 0)
                    ) {
                        /* Branch always taken: convert to GOTO */
#ifdef RTL_TRACE_GENERATE
                        fprintf(stderr, "[RTL] %p/%u: Branch always taken,"
                                " convert to GOTO and drop edge %u->%u\n",
                                block, unit->last_insn, unit_index,
                                unit->exits[fallthrough_index]);
#endif
                        insn->opcode = RTLOP_GOTO;
                        rtlunit_remove_edge(block, unit_index,
                                            fallthrough_index);
                    } else {
                        /* Branch never taken: convert to NOP */
#ifdef RTL_TRACE_GENERATE
                        fprintf(stderr, "[RTL] %p/%u: Branch never taken,"
                                " convert to NOP and drop edge %u->%u\n",
                                block, unit->last_insn, unit_index,
                                unit->exits[fallthrough_index ^ 1]);
#endif
                        insn->opcode = RTLOP_NOP;
                        insn->src_imm = 0;
                        rtlunit_remove_edge(block, unit_index,
                                            fallthrough_index ^ 1);
                    }
                    if (src1_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, src1_reg,
                                                        src1_index);
                    }
                    if (src2_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, src2_reg,
                                                        src2_index);
                    }
                }

            }
        }  // if (unit->exits[1] != -1)
    }  // for (unit_index = 0; unit_index < block->num_units; unit_index++)

    return 1;
}

/*************************************************************************/

/**
 * rtlopt_drop_dead_units:  Search an RTL block for basic units which are
 * unreachable via any path from the initial unit and remove them from the
 * code stream.  All units dominated only by such dead units are
 * recursively removed as well.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtlopt_drop_dead_units(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);

    /* Allocate and clear a buffer for "seen" flags */
    block->unit_seen = calloc(block->num_units, sizeof(*block->unit_seen));
    if (UNLIKELY(!block->unit_seen)) {
        DMSG("Failed to allocate units_seen (%u bytes)", block->num_units);
        return 0;
    }

    /* Check each unit in sequence (except the initial unit, which is never
     * dead even if it has no entry edges) */
    unsigned int unit_index;
    for (unit_index = 1; unit_index < block->num_units; unit_index++) {
        if (block->units[unit_index].entries[0] < 0) {
            drop_dead_unit(block, unit_index);
        }
        block->unit_seen[unit_index] = 1;
    }

    /* Free the "seen" flag buffer before returning (since the core doesn't
     * touch this field) */
    free(block->unit_seen);
    block->unit_seen = NULL;  // Just for safety

    return 1;
}

/*************************************************************************/

/**
 * rtlopt_drop_dead_branches:  Search an RTL block for branch instructions
 * which branch to the next instruction in the code stream and replace them
 * with NOPs.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtlopt_drop_dead_branches(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    unsigned int unit_index;
    for (unit_index = 0; unit_index < block->num_units; unit_index++) {
        RTLUnit * const unit = &block->units[unit_index];
        if (unit->last_insn >= unit->first_insn) {
            RTLInsn * const insn = &block->insns[unit->last_insn];
            const RTLOpcode opcode = insn->opcode;
            if ((opcode == RTLOP_GOTO
              || opcode == RTLOP_GOTO_IF_Z
              || opcode == RTLOP_GOTO_IF_NZ
              || opcode == RTLOP_GOTO_IF_E
              || opcode == RTLOP_GOTO_IF_NE)
             && block->label_unitmap[insn->label] == unit->next_unit
            ) {
#ifdef RTL_TRACE_GENERATE
                fprintf(stderr, "[RTL] %p/%u: Dropping branch to next insn\n",
                        block, unit->last_insn);
#endif
                if (opcode == RTLOP_GOTO_IF_Z || opcode == RTLOP_GOTO_IF_NZ) {
                    RTLRegister *src1_reg = &block->regs[insn->src1];
                    if (src1_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, src1_reg,
                                                        insn->src1);
                    }
                } else if (opcode == RTLOP_GOTO_IF_E || opcode == RTLOP_GOTO_IF_NE) {
                    RTLRegister *src1_reg = &block->regs[insn->src1];
                    if (src1_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, src1_reg,
                                                        insn->src1);
                    }
                    RTLRegister *src2_reg = &block->regs[insn->src2];
                    if (src2_reg->death == unit->last_insn) {
                        maybe_eliminate_folded_register(block, src2_reg,
                                                        insn->src2);
                    }
                }
                insn->opcode = RTLOP_NOP;
                insn->src_imm = 0;
            }
        }
    }

    return 1;
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * fold_one_register:  Attempt to perform constant folding on a register.
 * The register must be of type RTLREG_RESULT.
 *
 * [Parameters]
 *     block: RTL block
 *       reg: Register on which to perform constant folding
 * [Return value]
 *     Nonzero if constant folding was performed, zero otherwise
 * [Notes]
 *     This routine calls itself recursively, but it is declared inline
 *     anyway to optimize the outer loop in rtlopt_fold_constants().
 */
static inline int fold_one_register(RTLBlock * const block,
                                    RTLRegister * const reg)
{
    PRECOND(block != NULL, return 0);
    PRECOND(reg != NULL, return 0);
    PRECOND(reg->live, return 0);
    PRECOND(reg->source == RTLREG_RESULT, return 0);

    /* Flag the register as not foldable for the moment (to avoid infinite
     * recursion in case invalid code creates register dependency loops */

    reg->source = RTLREG_RESULT_NOFOLD;

    /* See if the operands are constant, folding them if necessary */

    RTLRegister * const src1 = &block->regs[reg->result.src1];
    RTLRegister * const src2 = &block->regs[reg->result.src2];
    if (src1->source != RTLREG_CONSTANT
     && !(src1->source == RTLREG_RESULT
          && fold_one_register(block, src1))
    ) {
        return 0;  // Operand 1 wasn't constant
    }
    if (!reg->result.is_imm
     && reg->result.src2 != 0  // In case it's a 1-operand instruction
     && src2->source != RTLREG_CONSTANT
     && !(src2->source == RTLREG_RESULT
          && fold_one_register(block, src2))
    ) {
        return 0;  // Operand 2 wasn't constant
    }
    if (reg->result.opcode == RTLOP_SELECT) {
        RTLRegister * const cond = &block->regs[reg->result.cond];
        if (cond->source != RTLREG_CONSTANT
         && !(cond->source == RTLREG_RESULT
              && fold_one_register(block, cond))
        ) {
            return 0;  // Condition operand wasn't constant
        }
    }

    /* All operands are constants, so perform the operation now and convert
     * the register to a constant */

    uintptr_t result = 0;
    switch (reg->result.opcode) {

      case RTLOP_MOVE:
        result = src1->value;
        break;

      case RTLOP_SELECT:
        result =
            block->regs[reg->result.cond].value ? src1->value : src2->value;
        break;

      case RTLOP_ADD:
        result = src1->value + (intptr_t)(int32_t)src2->value;
        break;

      case RTLOP_SUB:
        result = src1->value - (intptr_t)(int32_t)src2->value;
        break;

      case RTLOP_MULU:
        if (reg->result.second_res) {
            result = ((uint64_t)src1->value * (uint64_t)src2->value) >> 32;
        } else {
            result = src1->value * src2->value;
        }
        break;

      case RTLOP_MULS:
        if (reg->result.second_res) {
            result = ((int64_t)(int32_t)src1->value
                      * (int64_t)(int32_t)src2->value) >> 32;
        } else {
            result = src1->value * src2->value;
        }
        break;

      case RTLOP_DIVMODU:
        if (reg->result.second_res) {
            result = src2->value ? src1->value % src2->value : 0;
        } else {
            result = src2->value ? src1->value / src2->value : 0;
        }
        break;

      case RTLOP_DIVMODS:
        if (reg->result.second_res) {
            result = src2->value ? (int32_t)src1->value % (int32_t)src2->value
                                 : 0;
        } else {
            result = src2->value ? (int32_t)src1->value / (int32_t)src2->value
                                 : 0;
        }
        break;

      case RTLOP_AND:
        result = src1->value & (intptr_t)(int32_t)src2->value;
        break;

      case RTLOP_OR:
        result = src1->value | (uintptr_t)(uint32_t)src2->value;
        break;

      case RTLOP_XOR:
        result = src1->value ^ (uintptr_t)(uint32_t)src2->value;
        break;

      case RTLOP_NOT:
        result = ~src1->value;
        break;

      case RTLOP_SLL:
        result = src1->value << src2->value;
        break;

      case RTLOP_SRL:
        result = (uint32_t)src1->value >> src2->value;
        break;

      case RTLOP_SRA:
        result = (int32_t)src1->value >> src2->value;
        break;

      case RTLOP_ROR:
        if ((src2->value & 31) != 0) {
            result = (uint32_t)src1->value >> (src2->value & 31)
                   | (uint32_t)src1->value << (32 - (src2->value & 31));
        } else {
            result = (uint32_t)src1->value;
        }
        break;

      case RTLOP_CLZ: {
#ifdef __GNUC__
        result = __builtin_clz(src1->value);
#else
        uint32_t temp = src1->value;
        result = 32;
        while (temp) {
            temp >>= 1;
            result--;
        }
#endif  // __GNUC__
        break;
      }

      case RTLOP_CLO: {
        uint32_t temp = src1->value;
        result = 0;
        while ((int32_t)temp < 0) {
            temp <<= 1;
            result++;
        }
        break;
      }

      case RTLOP_SLTU:
        result = ((uint32_t)src1->value < (uint32_t)src2->value) ? 1 : 0;
        break;

      case RTLOP_SLTS:
        result = ((int32_t)src1->value < (int32_t)src2->value) ? 1 : 0;
        break;

      case RTLOP_BSWAPH:
        result = ((uint32_t)src1->value & 0xFF00FF00) >> 8
               | ((uint32_t)src1->value & 0x00FF00FF) << 8;
        break;

      case RTLOP_BSWAPW:
        result = ((uint32_t)src1->value & 0xFF000000) >> 24
               | ((uint32_t)src1->value & 0x00FF0000) >>  8
               | ((uint32_t)src1->value & 0x0000FF00) <<  8
               | ((uint32_t)src1->value & 0x000000FF) << 24;
        break;

      case RTLOP_HSWAPW:
        result = ((uint32_t)src1->value & 0xFFFF0000) >> 16
               | ((uint32_t)src1->value & 0x0000FFFF) << 16;
        break;

      case RTLOP_ADDI:
        result = src1->value + reg->result.imm;
        break;

      case RTLOP_ANDI:
        result = src1->value & reg->result.imm;
        break;

      case RTLOP_ORI:
        result = src1->value | reg->result.imm;
        break;

      case RTLOP_XORI:
        result = src1->value ^ reg->result.imm;
        break;

      case RTLOP_SLLI:
        result = src1->value << reg->result.imm;
        break;

      case RTLOP_SRLI:
        result = (uint32_t)src1->value >> reg->result.imm;
        break;

      case RTLOP_SRAI:
        result = (int32_t)src1->value >> reg->result.imm;
        break;

      case RTLOP_RORI:
        if ((reg->result.imm & 31) != 0) {
            result = (uint32_t)src1->value >> (reg->result.imm & 31)
                   | (uint32_t)src1->value << (32 - (reg->result.imm & 31));
        } else {
            result = (uint32_t)src1->value;
        }
        break;

      case RTLOP_SLTUI:
        result = ((uint32_t)src1->value < (uint32_t)reg->result.imm) ? 1 : 0;
        break;

      case RTLOP_SLTSI:
        result = ((int32_t)src1->value < (int32_t)reg->result.imm) ? 1 : 0;
        break;

      case RTLOP_BFEXT:
        result = ((uint32_t)src1->value >> reg->result.start)
               & ((1 << reg->result.count) - 1);
        return 1;

      case RTLOP_BFINS:
        result = ((uint32_t)src1->value
                  & ~(((1 << reg->result.count) - 1) << reg->result.start))
               | (((uint32_t)src2->value & ((1 << reg->result.count) - 1))
                  << reg->result.start);
        return 1;

      /* The remainder will never appear, but list them individually
       * rather than using a default case so the compiler will warn us if
       * we add a new opcode but don't include it here */
      case RTLOP_NOP:
      case RTLOP_MADDU:
      case RTLOP_MADDS:
      case RTLOP_LOAD_IMM:
      case RTLOP_LOAD_ADDR:
      case RTLOP_LOAD_PARAM:
      case RTLOP_LOAD_BU:
      case RTLOP_LOAD_BS:
      case RTLOP_LOAD_HU:
      case RTLOP_LOAD_HS:
      case RTLOP_LOAD_W:
      case RTLOP_LOAD_PTR:
      case RTLOP_STORE_B:
      case RTLOP_STORE_H:
      case RTLOP_STORE_W:
      case RTLOP_STORE_PTR:
      case RTLOP_LABEL:
      case RTLOP_GOTO:
      case RTLOP_GOTO_IF_Z:
      case RTLOP_GOTO_IF_NZ:
      case RTLOP_GOTO_IF_E:
      case RTLOP_GOTO_IF_NE:
      case RTLOP_CALL:
      case RTLOP_RETURN:
      case RTLOP_RETURN_TO:
        DMSG("impossible: opcode %u on RESULT register %u",
             reg->result.opcode, (unsigned int)(reg - block->regs));
        return 0;

    }  // switch (insn->opcode)

#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] Folded r%u to constant value 0x%lX at insn %u\n",
            (unsigned int)(reg - block->regs), (unsigned long)result,
            reg->birth);
#endif

    /* Update the instruction that set this register (use LOAD_ADDR in case
     * the original value was an address) */

    block->insns[reg->birth].opcode = RTLOP_LOAD_ADDR;
    block->insns[reg->birth].src_addr = result;

    /* See whether the source register(s) are used anywhere else, and
     * eliminate them if not */

    if (src1->death == reg->birth) {
        maybe_eliminate_folded_register(block, src1, reg->result.src1);
    }
    if ((!reg->result.is_imm && reg->result.src2 != 0)
     && src2->death == reg->birth
    ) {
        maybe_eliminate_folded_register(block, src2, reg->result.src2);
    }

    /* Constant folding was successful */

    reg->source = RTLREG_CONSTANT;
    reg->value = result;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * maybe_eliminate_folded_register:  See whether the given register is no
 * longer used after being eliminated from the instruction indexed by
 * reg->death.  If so, eliminate the register and change the instruction
 * indexed by reg->birth to a NOP; otherwise, update reg->death to point to
 * the last instruction that still uses the register.
 *
 * It is assumed that the register is only assigned once, at the
 * instruction indexed by reg->birth.
 *
 * [Parameters]
 *         block: RTL block
 *           reg: Register to attempt to eliminate
 *     reg_index: Index of register in block->regs[]
 * [Return value]
 *     None
 */
static void maybe_eliminate_folded_register(RTLBlock * const block,
                                            RTLRegister * const reg,
                                            const unsigned int reg_index)
{
    PRECOND(block != NULL, return);
    PRECOND(reg != NULL, return);
    PRECOND(reg->live, return);

    uint32_t insn_index;
    for (insn_index = reg->death-1; insn_index > reg->birth; insn_index--) {
        const RTLInsn * const insn = &block->insns[insn_index];
        switch (insn->opcode) {

          case RTLOP_NOP:
          case RTLOP_LOAD_IMM:
          case RTLOP_LOAD_ADDR:
          case RTLOP_LOAD_PARAM:
          case RTLOP_LABEL:
          case RTLOP_GOTO:
          case RTLOP_RETURN:
            break;

          case RTLOP_MOVE:
          case RTLOP_NOT:
          case RTLOP_CLZ:
          case RTLOP_CLO:
          case RTLOP_BSWAPH:
          case RTLOP_BSWAPW:
          case RTLOP_HSWAPW:
          case RTLOP_ADDI:
          case RTLOP_ANDI:
          case RTLOP_ORI:
          case RTLOP_XORI:
          case RTLOP_SLLI:
          case RTLOP_SRLI:
          case RTLOP_SRAI:
          case RTLOP_RORI:
          case RTLOP_SLTUI:
          case RTLOP_SLTSI:
          case RTLOP_BFEXT:
          case RTLOP_LOAD_BU:
          case RTLOP_LOAD_BS:
          case RTLOP_LOAD_HU:
          case RTLOP_LOAD_HS:
          case RTLOP_LOAD_W:
          case RTLOP_LOAD_PTR:
          case RTLOP_STORE_B:
          case RTLOP_STORE_H:
          case RTLOP_STORE_W:
          case RTLOP_STORE_PTR:
          case RTLOP_GOTO_IF_Z:
          case RTLOP_GOTO_IF_NZ:
            if (insn->src1 == reg_index) {
              still_used:
#ifdef RTL_TRACE_GENERATE
                fprintf(stderr, "[RTL] maybe_eliminate_folded_register: r%u"
                        " still used at insn %u\n", reg_index, insn_index);
#endif
                reg->death = insn_index;
                return;
            }
            break;

          case RTLOP_ADD:
          case RTLOP_SUB:
          case RTLOP_MULU:
          case RTLOP_MULS:
          case RTLOP_DIVMODU:
          case RTLOP_DIVMODS:
          case RTLOP_AND:
          case RTLOP_OR:
          case RTLOP_XOR:
          case RTLOP_SLL:
          case RTLOP_SRL:
          case RTLOP_SRA:
          case RTLOP_ROR:
          case RTLOP_SLTU:
          case RTLOP_SLTS:
          case RTLOP_BFINS:
          case RTLOP_GOTO_IF_E:
          case RTLOP_GOTO_IF_NE:
            if (insn->src1 == reg_index || insn->src2 == reg_index) {
                goto still_used;
            }
            break;

          case RTLOP_MADDU:
          case RTLOP_MADDS:
            if (insn->src1 == reg_index || insn->src2 == reg_index
             || insn->dest == reg_index || insn->dest2 == reg_index
            ) {
                goto still_used;
            }
            break;

          case RTLOP_SELECT:
            if (insn->src1 == reg_index || insn->src2 == reg_index
             || insn->cond == reg_index
            ) {
                goto still_used;
            }
            break;

          case RTLOP_CALL:
            if (insn->src1 == reg_index || insn->src2 == reg_index
             || insn->target == reg_index
            ) {
                goto still_used;
            }
            break;

          case RTLOP_RETURN_TO:
            if (insn->target == reg_index) {
                goto still_used;
            }
            break;

        }  // switch (opcode)
    }  // for (insn_index)

    /* If we got this far, nothing else uses the register, so nuke it */
#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] maybe_eliminate_folded_register: r%u no longer"
            " used, eliminating\n", reg_index);
#endif
    block->insns[reg->birth].opcode = RTLOP_NOP;
    block->insns[reg->birth].src_imm = 0;
    reg->live = 0;
}

/*************************************************************************/

/**
 * drop_dead_unit:  Drop a dead basic unit from an RTL block.  Recursive
 * helper function for rtlopt_drop_dead_units().
 *
 * [Parameters]
 *          block: RTL block
 *     unit_index: Index of unit in block->units[]
 * [Return value]
 *     None
 */
static void drop_dead_unit(RTLBlock * const block,
                           const unsigned int unit_index)
{
    PRECOND(block != NULL, return);
    PRECOND(block->units != NULL, return);
    PRECOND(block->unit_seen != NULL, return);
    PRECOND(unit_index < block->num_units, return);
    PRECOND(block->units[unit_index].entries[0] < 0, return);
    PRECOND(block->units[unit_index].prev_unit >= 0, return);

    RTLUnit * const unit = &block->units[unit_index];
#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] %p: Dropping dead unit %u", block, unit_index);
    if (unit->exits[0] < 0) {
        fprintf(stderr, " (no exits)\n");
    } else if (unit->exits[1] < 0) {
        fprintf(stderr, " (exits: %d)\n", unit->exits[0]);
    } else {
        fprintf(stderr, " (exits: %d, %d)\n", unit->exits[0], unit->exits[1]);
    }
#endif

    block->units[unit->prev_unit].next_unit = unit->next_unit;
    if (unit->next_unit >= 0) {
        block->units[unit->next_unit].prev_unit = unit->prev_unit;
    }
    if (unit->prev_call_unit >= 0) {
        block->units[unit->prev_call_unit].next_call_unit =
            unit->next_call_unit;
    }
    if (unit->next_call_unit >= 0) {
        block->units[unit->next_call_unit].prev_call_unit =
            unit->prev_call_unit;
    }

    while (unit->exits[0] >= 0) {
        const unsigned int to_index = unit->exits[0];
        rtlunit_remove_edge(block, unit_index, 0);
        if (block->unit_seen[to_index]) {
            /* We already saw this unit and (presumably) skipped it because
             * it wasn't dead.  Check again now that we've removed this
             * edge, and if it's now dead, recursively drop it.  There's no
             * danger of infinite recursion since any dead block has no
             * edges entering into it by definition. */
            if (block->units[to_index].entries[0] < 0) {
                drop_dead_unit(block, to_index);
            }
        }
    }
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
