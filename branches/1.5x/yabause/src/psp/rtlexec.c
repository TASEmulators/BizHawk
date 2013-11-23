/*  src/psp/rtlexec.c: Interpreted execution of RTL instructions
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
 * This source file contains code to interpret and execute RTL instructions
 * directly, i.e. without translating them into native machine code.  Such
 * interpreted execution is naturally very slow, generally even slower than
 * interpreting the original machine code; it is intended primarily for
 * testing conversions from source machine code to RTL or debugging the RTL
 * library itself.
 */

/*************************************************************************/
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "rtl.h"
#include "rtl-internal.h"

#ifdef RTL_TRACE_STEALTH_FOR_SH2
# include "sh2.h"
# include "sh2-internal.h"
#endif

/*************************************************************************/
/************************** Local declarations ***************************/
/*************************************************************************/

static int create_insn_unitmap(RTLBlock *block);

static inline int rtl_execute_insn(RTLBlock *block, uint32_t *index_ptr,
                                   void *state);

/*************************************************************************/
/********************** External interface routines **********************/
/*************************************************************************/

/**
 * rtl_execute_block:  Execute the given block by interpreting RTL
 * instructions one at a time.
 *
 * [Parameters]
 *     block: RTLBlock to execute
 *     state: SH-2 processor state block
 * [Return value]
 *     Number of RTL instructions executed
 */
uint32_t rtl_execute_block(RTLBlock *block, void *state)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->finalized, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(state != NULL, return 0);

    /* Create the instruction-to-unit lookup table if it hasn't been
     * created yet */
    if (UNLIKELY(!block->insn_unitmap)) {
        if (UNLIKELY(!create_insn_unitmap(block))) {
            DMSG("%p: Failed to create insn-to-unit lookup table", block);
            return 0;
        }
    }

#ifdef RTL_TRACE_STEALTH_FOR_SH2
    /* Clear the SH-2 register cache bitmask */
    block->sh2_regcache_mask = 0;
#endif

    /* Actually execute instructions */
    uint32_t insn_count = 0;
    uint32_t index = 0;
    while (index < block->num_insns) {
        insn_count++;
        if (!rtl_execute_insn(block, &index, state)) {
            break;
        }
    }
    return insn_count;
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * create_insn_unitmap:  Create a lookup table mapping instruction indices to
 * unit indices, used by rtl_execute_block() to find the unit containing
 * the first instruction to execute.
 *
 * [Parameters]
 *     block: RTLBlock to create table for
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int create_insn_unitmap(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->units != NULL, return 0);

    block->insn_unitmap =
        malloc(sizeof(*block->insn_unitmap) * block->num_insns);
    if (UNLIKELY(!block->insn_unitmap)) {
        DMSG("No memory for insn-to-unit lookup table (%lu bytes)",
             (unsigned long)(sizeof(*block->insn_unitmap) * block->num_insns));
        return 0;
    }
    memset(block->insn_unitmap, -1,
           sizeof(*block->insn_unitmap) * block->num_insns);

    unsigned int unit_index;
    for (unit_index = 0; unit_index < block->num_units; unit_index++) {
        uint32_t insn_index;
        for (insn_index = block->units[unit_index].first_insn;
             (int32_t)insn_index <= block->units[unit_index].last_insn;
             insn_index++
        ) {
            block->insn_unitmap[insn_index] = unit_index;
        }
    }

    return 1;
}

/*************************************************************************/

/**
 * rtl_execute_insn:  Execute a single RTL instruction from the given
 * block.
 *
 * [Parameters]
 *         block: RTLBlock to execute
 *     index_ptr: Pointer to variable holding index of instruction to
 *                   execute; incremented on return to indicate the next
 *                   instruction to execute
 *          state: SH-2 state block
 * [Return value]
 *     Nonzero to continue execution, zero to terminate execution
 */
static inline int rtl_execute_insn(RTLBlock *block, uint32_t *index_ptr,
                                   void *state)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(index_ptr != NULL, return 0);
    PRECOND(*index_ptr < block->num_insns, return 0);
    PRECOND(state != NULL, return 0);

#ifdef RTL_TRACE_EXECUTE
    fprintf(stderr, "%p/%u: %s\n", block, *index_ptr,
            rtl_decode_insn(block, *index_ptr, 1));
#endif

    const RTLInsn * const insn = &block->insns[*index_ptr];
    const RTLUnit * const unit = &block->units[block->insn_unitmap[*index_ptr]];
    if (*index_ptr < unit->last_insn) {
        (*index_ptr)++;
    } else {
        int next_unit = unit->next_unit;
        while (next_unit >= 0 && block->units[next_unit].first_insn
                                     > block->units[next_unit].last_insn) {
            next_unit = block->units[next_unit].next_unit;
        }
        if (next_unit >= 0) {
            *index_ptr = block->units[next_unit].first_insn;
        } else {
            *index_ptr = block->num_insns;  // Terminate execution
        }
    }

    RTLRegister * const dest = &block->regs[insn->dest];
    const RTLRegister * const src1 = &block->regs[insn->src1];
    const RTLRegister * const src2 = &block->regs[insn->src2];

    /*------------------------------*/

    switch ((RTLOpcode)insn->opcode) {

      case RTLOP_NOP:
#ifdef RTL_TRACE_STEALTH_FOR_SH2
        if (insn->src_imm >> 28 == 0x8 || insn->src_imm >> 28 == 0xA) {
            /* Trace an instruction */

            SH2State *sh2 = (SH2State *)state;
            uint32_t *sh2_regs = (uint32_t *)&sh2;
            uint32_t saveregs[23];
            memcpy(saveregs, sh2_regs, sizeof(saveregs));
            uint32_t mask = block->sh2_regcache_mask;
            unsigned int sh2_reg;
            for (sh2_reg = 0; mask != 0; mask >>= 1, sh2_reg++) {
                if (mask & 1) {
                    sh2_regs[sh2_reg] = block->sh2_regcache[sh2_reg];
                }
            }
            uint32_t op = block->insns[(*index_ptr)++].src_imm;
            unsigned int do_trace = 1;
            if (op>>24 == 0x9F) {  // Conditional execution
                const unsigned int cond_reg = op & 0xFFFF;
                const unsigned int test_bit = op>>16 & 1;
                do_trace = (block->regs[cond_reg].value == test_bit);
                op = block->insns[(*index_ptr)++].src_imm;
            }
            uint32_t cached_cycles = op;
            PRECOND(cached_cycles>>24 == 0x98, return 1);
            cached_cycles &= 0xFFFF;
            uint32_t cached_cycle_reg = block->insns[(*index_ptr)++].src_imm;
            PRECOND(cached_cycles>>24 == 0x90, return 1);
            cached_cycle_reg &= 0xFFFF;
            if (cached_cycle_reg) {
                cached_cycles += block->regs[cached_cycle_reg].value;
            } else {
                cached_cycles += sh2->cycles;
            }
            const uint32_t old_cycles = sh2->cycles;
            sh2->cycles = cached_cycles;
            if (do_trace) {
                (*trace_insn_callback)(sh2, insn->src_imm & 0x7FFFFFFF);
            }
            sh2->cycles = old_cycles;
            memcpy(sh2_regs, saveregs, sizeof(saveregs));

        } else if (insn->src_imm >> 28 == 0xB) {
            /* Record a cached SH-2 register value */

            uint32_t op = insn->src_imm;
            uint32_t offset = 0;
            if (op & 0x08000000) {
                PRECOND(op>>24 == 0xB8, return 1);
                offset = (op & 0xFFFF) << 16;
                op = block->insns[(*index_ptr)++].src_imm;
                PRECOND(op>>24 == 0xBC, return 1);
                offset |= op & 0xFFFF;
                op = block->insns[(*index_ptr)++].src_imm;
                PRECOND(op>>24 == 0xB0, return 1);
            }
            unsigned int sh2_reg = (op >> 16) & 0xFF;
            unsigned int rtl_reg = op & 0xFFFF;
            if (rtl_reg) {
                if (sh2_reg & 0x80) {  // Used for SR.T
                    if (!(block->sh2_regcache_mask & 1<<16)) {
                        SH2State *sh2 = (SH2State *)state;
                        block->sh2_regcache[16] = sh2->SR & ~SR_T;
                        block->sh2_regcache_mask |= 1<<16;
                    } else {
                        block->sh2_regcache[16] &= ~SR_T;
                    }
                    block->sh2_regcache[16] |= block->regs[rtl_reg].value;
                } else {
                    block->sh2_regcache[sh2_reg] =
                        block->regs[rtl_reg].value + offset;
                    block->sh2_regcache_mask |= 1<<sh2_reg;
                }
            } else {
                if (offset) {  // Used to record PC directly without a register
                    block->sh2_regcache[sh2_reg] = offset;
                    block->sh2_regcache_mask |= 1<<sh2_reg;
                } else if (!(sh2_reg & 0x80)) {
                    block->sh2_regcache_mask &= ~(1<<sh2_reg);
                }
            }

        } else if (insn->src_imm >> 28 == 0xC) {
            /* Trace a store operation */

            uint32_t op;
            op = block->insns[(*index_ptr)++].src_imm;
            PRECOND(op>>28 == 0xD, return 1);
            uint32_t address;
            if (op & 0x08000000) {
                PRECOND(op>>24 == 0xD8, return 1);
                address = (op & 0xFFFF) << 16;
                op = block->insns[(*index_ptr)++].src_imm;
                PRECOND(op>>24 == 0xDC, return 1);
                address |= op & 0xFFFF;
            } else {
                address = block->regs[op & 0xFFFF].value;
                op = block->insns[(*index_ptr)++].src_imm;
                PRECOND(op>>24 == 0xD4, return 1);
                address += (op & 0xFFFF) << 16;
                op = block->insns[(*index_ptr)++].src_imm;
                PRECOND(op>>24 == 0xD6, return 1);
                address += op & 0xFFFF;
            }
            op = block->insns[(*index_ptr)++].src_imm;
            PRECOND(op>>28 == 0xE, return 1);
            const uint32_t src = block->regs[op & 0xFFFF].value;
            switch (insn->src_imm & 0xFFFF) {
              case 1: (*trace_storeb_callback)(address, src); break;
              case 2: (*trace_storew_callback)(address, src); break;
              case 4: (*trace_storel_callback)(address, src); break;
              default:
                DMSG("Invalid store trace type %u", insn->src_imm & 0xFF);
                break;
            }

        }
#endif  // RTL_TRACE_STEALTH_FOR_SH2
        return 1;

      /*----------------------------*/

      case RTLOP_MOVE:
        dest->value = src1->value;
        return 1;

      case RTLOP_SELECT:
        dest->value =
            block->regs[insn->cond].value ? src1->value : src2->value;
        return 1;

      case RTLOP_ADD:
        dest->value = src1->value + (intptr_t)(int32_t)src2->value;
        return 1;

      case RTLOP_SUB:
        dest->value = src1->value - (intptr_t)(int32_t)src2->value;
        return 1;

      case RTLOP_MULU: {
        RTLRegister * const dest2 = &block->regs[insn->dest2];
        dest->value = (uint32_t)(src1->value * src2->value);
        dest2->value = (uint64_t)((uint64_t)(uint32_t)src1->value
                                  * (uint64_t)(uint32_t)src2->value) >> 32;
        return 1;
      }

      case RTLOP_MULS: {
        RTLRegister * const dest2 = &block->regs[insn->dest2];
        dest->value = (uint32_t)(src1->value * src2->value);
        dest2->value = (int64_t)((int64_t)(int32_t)src1->value
                                 * (int64_t)(int32_t)src2->value) >> 32;
        return 1;
      }

      case RTLOP_MADDU: {
        RTLRegister * const dest2 = &block->regs[insn->dest2];
        uint64_t initial = (uint64_t)(uint32_t)dest2->value << 32
                         | (uint64_t)(uint32_t)dest->value;
        uint64_t product = (uint64_t)(uint32_t)src1->value
                         * (uint64_t)(uint32_t)src2->value;
        uint64_t result = initial + product;
        dest->value = (uint32_t)result;
        dest2->value = (uint32_t)(result >> 32);
        return 1;
      }

      case RTLOP_MADDS: {
        RTLRegister * const dest2 = &block->regs[insn->dest2];
        int64_t initial = (int64_t)(int32_t)dest2->value << 32
                        | (int64_t)(uint32_t)dest->value;  // unsigned!
        int64_t product = (int64_t)(int32_t)src1->value
                        * (int64_t)(int32_t)src2->value;
        int64_t result = initial + product;
        dest->value = (uint32_t)result;
        dest2->value = (uint32_t)(result >> 32);
        return 1;
      }

      case RTLOP_DIVMODU:
        if (src2->value) {
            RTLRegister * const dest2 = &block->regs[insn->dest2];
            dest->value  = (uint32_t)src1->value / (uint32_t)src2->value;
            dest2->value = (uint32_t)src1->value % (uint32_t)src2->value;
        }
        return 1;

      case RTLOP_DIVMODS:
        if (src2->value) {
            RTLRegister * const dest2 = &block->regs[insn->dest2];
            dest->value  = (int32_t)src1->value / (int32_t)src2->value;
            dest2->value = (int32_t)src1->value % (int32_t)src2->value;
        }
        return 1;

      case RTLOP_AND:
        dest->value = src1->value & (intptr_t)(int32_t)src2->value;
        return 1;

      case RTLOP_OR:
        dest->value = src1->value | (uintptr_t)(uint32_t)src2->value;
        return 1;

      case RTLOP_XOR:
        dest->value = src1->value ^ (uintptr_t)(uint32_t)src2->value;
        return 1;

      case RTLOP_NOT:
        dest->value = (uint32_t)(~src1->value);
        return 1;

      case RTLOP_SLL:
        if (src2->value < 32) {
            dest->value = (uint32_t)(src1->value << src2->value);
        } else {
            dest->value = 0;
        }
        return 1;

      case RTLOP_SRL:
        if (src2->value < 32) {
            dest->value = (uint32_t)src1->value >> src2->value;
        } else {
            dest->value = 0;
        }
        return 1;

      case RTLOP_SRA:
        if (src2->value < 32) {
            dest->value = (int32_t)src1->value >> src2->value;
        } else {
            dest->value = (int32_t)src1->value >> 31;
        }
        return 1;

      case RTLOP_ROR:
        if ((src2->value & 31) != 0) {
            dest->value = (uint32_t)src1->value >> (src2->value & 31)
                        | (uint32_t)src1->value << (32 - (src2->value & 31));
        } else {
            dest->value = (uint32_t)src1->value;
        }
        return 1;

      case RTLOP_CLZ: {
#ifdef __GNUC__
        dest->value = __builtin_clz(src1->value);
#else
        uint32_t temp = src1->value;
        dest->value = 32;
        while (temp) {
            temp >>= 1;
            dest->value--;
        }
#endif
        return 1;
      }

      case RTLOP_CLO: {
        uint32_t temp = src1->value;
        dest->value = 0;
        while ((int32_t)temp < 0) {
            temp <<= 1;
            dest->value++;
        }
        return 1;
      }

      case RTLOP_SLTU:
        dest->value = ((uint32_t)src1->value < (uint32_t)src2->value) ? 1 : 0;
        return 1;

      case RTLOP_SLTS:
        dest->value = ((int32_t)src1->value < (int32_t)src2->value) ? 1 : 0;
        return 1;

      case RTLOP_BSWAPH:
        dest->value = ((uint32_t)src1->value & 0xFF00FF00) >> 8
                    | ((uint32_t)src1->value & 0x00FF00FF) << 8;
        return 1;

      case RTLOP_BSWAPW:
        dest->value = ((uint32_t)src1->value & 0xFF000000) >> 24
                    | ((uint32_t)src1->value & 0x00FF0000) >>  8
                    | ((uint32_t)src1->value & 0x0000FF00) <<  8
                    | ((uint32_t)src1->value & 0x000000FF) << 24;
        return 1;

      case RTLOP_HSWAPW:
        dest->value = ((uint32_t)src1->value & 0xFFFF0000) >> 16
                    | ((uint32_t)src1->value & 0x0000FFFF) << 16;
        return 1;

      /*----------------------------*/

      case RTLOP_ADDI:
        dest->value = src1->value + (intptr_t)(int32_t)insn->src_imm;
        return 1;

      case RTLOP_ANDI:
        dest->value = src1->value & (intptr_t)(int32_t)insn->src_imm;
        return 1;

      case RTLOP_ORI:
        dest->value = src1->value | (uintptr_t)(uint32_t)insn->src_imm;
        return 1;

      case RTLOP_XORI:
        dest->value = src1->value ^ (uintptr_t)(uint32_t)insn->src_imm;
        return 1;

      case RTLOP_SLLI:
        if (insn->src_imm < 32) {
            dest->value = (uint32_t)(src1->value << insn->src_imm);
        } else {
            dest->value = 0;
        }
        return 1;

      case RTLOP_SRLI:
        if (insn->src_imm < 32) {
            dest->value = (uint32_t)src1->value >> insn->src_imm;
        } else {
            dest->value = 0;
        }
        return 1;

      case RTLOP_SRAI:
        if (insn->src_imm < 32) {
            dest->value = (int32_t)src1->value >> insn->src_imm;
        } else {
            dest->value = (int32_t)src1->value >> 31;
        }
        return 1;

      case RTLOP_RORI:
        if ((insn->src_imm & 31) != 0) {
            dest->value = (uint32_t)src1->value >> (insn->src_imm & 31)
                        | (uint32_t)src1->value << (32 - (insn->src_imm & 31));
        } else {
            dest->value = (uint32_t)src1->value;
        }
        return 1;

      case RTLOP_SLTUI:
        dest->value = ((uint32_t)src1->value < (uint32_t)insn->src_imm) ? 1 : 0;
        return 1;

      case RTLOP_SLTSI:
        dest->value = ((int32_t)src1->value < (int32_t)insn->src_imm) ? 1 : 0;
        return 1;

      /*----------------------------*/

      case RTLOP_BFEXT:
        dest->value = ((uint32_t)src1->value >> insn->bitfield.start)
                    & ((1 << insn->bitfield.count) - 1);
        return 1;

      case RTLOP_BFINS:
        dest->value = ((uint32_t)src1->value
                       & ~(((1 << insn->bitfield.count) - 1)
                           << insn->bitfield.start))
                    | (((uint32_t)src2->value
                        & ((1 << insn->bitfield.count) - 1))
                       << insn->bitfield.start);
        return 1;

      /*----------------------------*/

      case RTLOP_LOAD_IMM:
        dest->value = insn->src_imm;
        return 1;

      case RTLOP_LOAD_ADDR:
        dest->value = insn->src_addr;
        return 1;

      case RTLOP_LOAD_PARAM:
        if (insn->src_imm == 0) {
            dest->value = (uintptr_t)state;
        } else {
            DMSG("LOAD_PARAM for undefined parameter %u",
                 (unsigned int)insn->src_imm);
            dest->value = 0xDEADF00D;
        }
        return 1;

      case RTLOP_LOAD_BU:
        dest->value = *(uint8_t *)(src1->value + insn->offset);
        return 1;

      case RTLOP_LOAD_BS:
        dest->value = *(int8_t *)(src1->value + insn->offset);
        return 1;

      case RTLOP_LOAD_HU:
        dest->value = *(uint16_t *)(src1->value + insn->offset);
        return 1;

      case RTLOP_LOAD_HS:
        dest->value = *(int16_t *)(src1->value + insn->offset);
        return 1;

      case RTLOP_LOAD_W:
        dest->value = *(uint32_t *)(src1->value + insn->offset);
        return 1;

      case RTLOP_LOAD_PTR:
        dest->value = *(uintptr_t *)(src1->value + insn->offset);
        return 1;

      /*----------------------------*/

      case RTLOP_STORE_B:
        *(uint8_t *)(dest->value + insn->offset) = src1->value;
        return 1;

      case RTLOP_STORE_H:
        *(uint16_t *)(dest->value + insn->offset) = src1->value;
        return 1;

      case RTLOP_STORE_W:
        *(uint32_t *)(dest->value + insn->offset) = src1->value;
        return 1;

      case RTLOP_STORE_PTR:
        *(uintptr_t *)(dest->value + insn->offset) = src1->value;
        return 1;

      /*----------------------------*/

      case RTLOP_LABEL:
        return 1;

      case RTLOP_GOTO:
      do_goto:
        if (insn->label < 1 || insn->label > block->next_label) {
            DMSG("%p/%u: label %u out of range",
                 block, (int)(insn - block->insns), insn->label);
        } else if (block->label_unitmap[insn->label] < 0) {
            DMSG("%p/%u: label %u not defined",
                 block, (int)(insn - block->insns), insn->label);
        } else {
            *index_ptr =
                block->units[block->label_unitmap[insn->label]].first_insn;
        }
        return 1;

      case RTLOP_GOTO_IF_Z:
        if (src1->value == 0) {
            goto do_goto;
        }
        return 1;

      case RTLOP_GOTO_IF_NZ:
        if (src1->value != 0) {
            goto do_goto;
        }
        return 1;

      case RTLOP_GOTO_IF_E:
        if (src1->value == src2->value) {
            goto do_goto;
        }
        return 1;

      case RTLOP_GOTO_IF_NE:
        if (src1->value != src2->value) {
            goto do_goto;
        }
        return 1;

      /*----------------------------*/

      case RTLOP_CALL: {
        /* The called function must take pointer-sized parameters and
         * return a pointer-sized value. */
        const void *funcptr = (const void *)block->regs[insn->target].value;
        if (insn->dest) {
            if (insn->src1) {
                if (insn->src2) {
                    FASTCALL uintptr_t (*func)(uintptr_t, uintptr_t) = funcptr;
                    dest->value = (*func)(src1->value, src2->value);
                } else {
                    FASTCALL uintptr_t (*func)(uintptr_t) = funcptr;
                    dest->value = (*func)(src1->value);
                }
            } else {
                FASTCALL uintptr_t (*func)(void) = funcptr;
                dest->value = (*func)();
            }
        } else {
            if (insn->src1) {
                if (insn->src2) {
                    FASTCALL void (*func)(uintptr_t, uintptr_t) = funcptr;
                    (*func)(src1->value, src2->value);
                } else {
                    FASTCALL void (*func)(uintptr_t) = funcptr;
                    (*func)(src1->value);
                }
            } else {
                FASTCALL void (*func)(void) = funcptr;
                (*func)();
            }
        }
        return 1;
      }

      /*----------------------------*/

      case RTLOP_RETURN:
        return 0;

      case RTLOP_RETURN_TO: {
        RTLBlock *newblock = (RTLBlock *)block->regs[insn->target].value;
        /* Assume we won't fill up the stack doing this... */
        rtl_execute_block(newblock, state);
        return 0;
      }

    }  // switch (insn->opcode)

    /*------------------------------*/

    DMSG("Block %p index %u: invalid opcode %u", block, *index_ptr,
         insn->opcode);
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
