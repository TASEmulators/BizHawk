/*  src/psp/rtl-mips.c: RTL->MIPS translator used in dynamic translation
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
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "rtl.h"
#include "rtl-internal.h"
#include "rtl-mips.h"

#ifdef RTL_TRACE_STEALTH_FOR_SH2
# include "sh2.h"
# include "sh2-internal.h"
#endif

/*************************************************************************/
/************************** Local declarations ***************************/
/*************************************************************************/

/* Magic MIPS register index indicating that an RTL register is located in
 * the stack frame and has no hardware register assigned */
#define MIPS_noreg  32

/* List of caller-saved and callee-saved registers specified by the MIPS ABI.
 * Note that we omit $at and $v1 from the caller-saved register list because
 * we never assign them to RTL registers (they're only used to load values
 * from the stack frame for use as instruction operands) */
static const uint8_t caller_saved_regs[] = {
    MIPS_v0,
    MIPS_a0, MIPS_a1, MIPS_a2, MIPS_a3,
    MIPS_t0, MIPS_t1, MIPS_t2, MIPS_t3, MIPS_t4, MIPS_t5, MIPS_t6, MIPS_t7,
    MIPS_t8, MIPS_t9,
};
static const uint8_t callee_saved_regs[] = {
    MIPS_s0, MIPS_s1, MIPS_s2, MIPS_s3, MIPS_s4, MIPS_s5, MIPS_s6,
    MIPS_s7, MIPS_s8,
};

/* Array of flags indicating which registers are caller-saved or callee-saved*/
static const uint8_t reg_is_caller_saved[] = {
    [MIPS_v1] = 1,
    [MIPS_a0] = 1, [MIPS_a1] = 1, [MIPS_a2] = 1, [MIPS_a3] = 1,
    [MIPS_t0] = 1, [MIPS_t1] = 1, [MIPS_t2] = 1, [MIPS_t3] = 1,
    [MIPS_t4] = 1, [MIPS_t5] = 1, [MIPS_t6] = 1, [MIPS_t7] = 1,
    [MIPS_t8] = 1, [MIPS_t9] = 1,
};
static const uint8_t reg_is_callee_saved[] = {
    [MIPS_s0] = 1, [MIPS_s1] = 1, [MIPS_s2] = 1, [MIPS_s3] = 1,
    [MIPS_s4] = 1, [MIPS_s5] = 1, [MIPS_s6] = 1, [MIPS_s7] = 1,
    [MIPS_s8] = 1,
};

/* Fake opcodes for branching to labels (to be resolved later) */
#define __OP_BEQ_LABEL  074  // __OP_BEQ + 070
#define __OP_BNE_LABEL  075  // __OP_BNE + 070
#define MIPS_BEQ_LABEL(rs,rt,label) \
    __MIPS_INSN_IMM(BEQ_LABEL, (rs), (rt), (label))
#define MIPS_BNE_LABEL(rs,rt,label) \
    __MIPS_INSN_IMM(BNE_LABEL, (rs), (rt), (label))
#define MIPS_BEQZ_LABEL(reg,label)  MIPS_BEQ_LABEL((reg), MIPS_zero, (label))
#define MIPS_BNEZ_LABEL(reg,label)  MIPS_BNE_LABEL((reg), MIPS_zero, (label))
#define MIPS_B_LABEL(label)         MIPS_BEQZ_LABEL(MIPS_zero, (label))

/* Fake opcode for jumping to the epilogue, returning a constant in $v0 */
#define MIPS_B_EPILOGUE_RET_CONST   __MIPS_SPECIAL(0, 0, 0, 1, JALR)

/* Fake opcode for jumping to the epilogue, then chaining to the address
 * in $at */
#define MIPS_B_EPILOGUE_CHAIN_AT    __MIPS_SPECIAL(0, 0, 0, 2, JALR)

/*-----------------------------------------------------------------------*/

static int allocate_registers(RTLBlock * const block);
static inline int allocate_regs_unit(RTLBlock * const block,
                                     const unsigned int unit_index);
static inline int allocate_regs_insn(RTLBlock * const block,
                                     const unsigned int unit_index,
                                     const uint32_t insn_index);
static void add_to_active_list(RTLBlock * const block, const int is_frame,
                               const unsigned int index);
static void remove_from_active_list(RTLBlock * const block, const int is_frame,
                                    const unsigned int index);
static void clean_active_list(RTLBlock * const block,
                              const uint32_t clean_insn);
static int allocate_one_register(RTLBlock * const block,
                                 const unsigned int unit_index,
                                 const uint32_t insn_index,
                                 const unsigned int reg_index);
static int allocate_one_register_prefer(RTLBlock * const block,
                                        const unsigned int unit_index,
                                        const uint32_t insn_index,
                                        const unsigned int reg_index,
                                        const unsigned int prefer_mips);
static int merge_constant_register(RTLBlock * const block,
                                   const unsigned int unit_index,
                                   const uint32_t insn_index,
                                   const unsigned int reg_index);
static int allocate_frame_slot(RTLBlock * const block,
                               const uint32_t insn_index,
                               const unsigned int reg_index);
#ifdef MIPS_OPTIMIZE_IMMEDIATE
static int optimize_immediate(RTLBlock * const block,
                              const uint32_t insn_index);
#endif
#ifdef RTL_TRACE_GENERATE
static void print_regmap(RTLBlock * const block);
#endif

/*----------------------------------*/

static int translate_block(RTLBlock * const block);
static inline int translate_unit(RTLBlock * const block,
                                 const unsigned int unit_index);
static inline unsigned int translate_insn(RTLBlock * const block,
                                          const unsigned int unit_index,
                                          const uint32_t insn_index);
static int append_alu_1op(RTLBlock * const block, const RTLInsn * const insn,
                          const uint32_t opcode);
static int append_alu_reg(RTLBlock * const block, const RTLInsn * const insn,
                          const uint32_t opcode);
static int append_shift_reg(RTLBlock * const block, const RTLInsn * const insn,
                            const uint32_t opcode);
static int append_mult_div(RTLBlock * const block, const RTLInsn * const insn,
                           const uint32_t opcode, const int accumulate,
                           const uint32_t insn_index);

static int flush_for_call(RTLBlock * const block, const uint32_t insn_index);
static int reload_after_call(RTLBlock * const block,
                             const uint32_t insn_index);
static int flush_hilo(RTLBlock * const block, const uint32_t insn_index);

#ifdef MIPS_OPTIMIZE_MIN_MAX
static int optimize_min_max(RTLBlock * const block, const uint32_t insn_index);
#endif

#ifdef RTL_TRACE_STEALTH_FOR_SH2
static unsigned int sh2_stealth_trace_insn(RTLBlock * const block,
                                           const uint32_t insn_index);
static unsigned int sh2_stealth_cache_reg(RTLBlock * const block,
                                          const uint32_t insn_index);
static unsigned int sh2_stealth_trace_store(RTLBlock * const block,
                                            const uint32_t insn_index);
#endif

static int resolve_branches(RTLBlock * const block);

/*----------------------------------*/

static inline int append_insn(RTLBlock * const block, const uint32_t insn);
static int expand_block(RTLBlock * const block);
static uint32_t last_insn(const RTLBlock * const block);
static uint32_t pop_insn(RTLBlock * const block);

static int append_float(RTLBlock * const block, const uint32_t insn,
                        const int latency);
static int append_branch(RTLBlock * const block, const uint32_t insn);

static int append_prologue(RTLBlock * const block);
static int append_epilogue(RTLBlock * const block);

/*----------------------------------*/

#ifdef __GNUC__
# define CONST_FUNC  __attribute__((const))
#else
# define CONST_FUNC  /*nothing*/
#endif

static CONST_FUNC inline int insn_rs(const uint32_t opcode);
static CONST_FUNC inline int insn_rt(const uint32_t opcode);
static CONST_FUNC inline int insn_rd(const uint32_t opcode);
static CONST_FUNC inline int insn_imm(const uint32_t opcode);

static CONST_FUNC inline int insn_is_load(const uint32_t opcode);
static CONST_FUNC inline int insn_is_store(const uint32_t opcode);
static CONST_FUNC inline int insn_is_jump(const uint32_t opcode);
static CONST_FUNC inline int insn_is_branch(const uint32_t opcode);
static CONST_FUNC inline int insn_is_imm(const uint32_t opcode);
static CONST_FUNC inline int insn_is_imm_alu(const uint32_t opcode);
static CONST_FUNC inline int insn_is_special(const uint32_t opcode);
static CONST_FUNC inline int insn_is_regimm(const uint32_t opcode);
static CONST_FUNC inline int insn_is_allegrex(const uint32_t opcode);

static CONST_FUNC inline uint32_t insn_regs_used(const uint32_t opcode);
static CONST_FUNC inline uint32_t insn_regs_set(const uint32_t opcode);

/*************************************************************************/

#ifdef RTL_TRACE_STEALTH_FOR_SH2

/* Array for caching unflushed SH-2 register values (this assumes
 * singlethreaded execution, but since this is only for debugging anyway
 * let's not worry too hard about it) */

static uint32_t sh2_regcache[23];

/*----------------------------------*/

/* Set up common code blocks as constant arrays so we don't bloat the code
 * too badly */

/* Save all caller-saved MIPS registers and the current state block
 * register values on the stack; the state block pointer is assumed to be
 * in $at */
static const uint32_t code_save_regs_state[] = {
    /* Add stack space for saving registers */
    MIPS_ADDIU(MIPS_sp, MIPS_sp, -(4*(18+24))),
    /* First save all MIPS caller-saved registers */
    MIPS_SW(MIPS_v0,  0, MIPS_sp),
    MIPS_SW(MIPS_v1,  4, MIPS_sp),
    MIPS_SW(MIPS_a0,  8, MIPS_sp),
    MIPS_SW(MIPS_a1, 12, MIPS_sp),
    MIPS_SW(MIPS_a2, 16, MIPS_sp),
    MIPS_SW(MIPS_a3, 20, MIPS_sp),
    MIPS_SW(MIPS_t0, 24, MIPS_sp),
    MIPS_SW(MIPS_t1, 28, MIPS_sp),
    MIPS_SW(MIPS_t2, 32, MIPS_sp),
    MIPS_SW(MIPS_t3, 36, MIPS_sp),
    MIPS_SW(MIPS_t4, 40, MIPS_sp),
    MIPS_SW(MIPS_t5, 44, MIPS_sp),
    MIPS_SW(MIPS_t6, 48, MIPS_sp),
    MIPS_SW(MIPS_t7, 52, MIPS_sp),
    MIPS_SW(MIPS_t8, 56, MIPS_sp),
    MIPS_SW(MIPS_t9, 60, MIPS_sp),
    MIPS_MFLO(MIPS_v1),
    MIPS_SW(MIPS_v1, 64, MIPS_sp),
    MIPS_MFHI(MIPS_v1),
    MIPS_SW(MIPS_v1, 68, MIPS_sp),
    /* Copy the current register values in the SH-2 state block (which may
     * not be up to date) to the stack */
    MIPS_MOVE(MIPS_a0, MIPS_at),
    MIPS_ADDIU(MIPS_a1, MIPS_sp, 4*18),
    MIPS_ADDIU(MIPS_a2, MIPS_a0, 4*23),
    MIPS_LW(MIPS_v1, 0, MIPS_a0),
    MIPS_ADDIU(MIPS_a0, MIPS_a0, 4),
    MIPS_ADDIU(MIPS_a1, MIPS_a1, 4),
    MIPS_BNE(MIPS_a0, MIPS_a2, -4),
    MIPS_SW(MIPS_v1, -4, MIPS_a1),
    /* Copy the current cycle count to the stack (leaving a copy in $v1)
     * and return */
    MIPS_LW(MIPS_v1, offsetof(SH2State,cycles), MIPS_at),
    MIPS_JR(MIPS_ra),
    MIPS_SW(MIPS_v1, 0, MIPS_a1),
};

/* Restore all caller-saved MIPS registers and the current state block
 * register values from the stack; the state block pointer is assumed to be
 * in $at */
static const uint32_t code_restore_regs_state[] = {
    /* Restore values to the SH-2 state block */
    MIPS_MOVE(MIPS_a0, MIPS_at),
    MIPS_ADDIU(MIPS_a1, MIPS_sp, 4*18),
    MIPS_ADDIU(MIPS_a2, MIPS_a0, 4*23),
    MIPS_LW(MIPS_v1, 0, MIPS_a1),
    MIPS_ADDIU(MIPS_a0, MIPS_a0, 4),
    MIPS_ADDIU(MIPS_a1, MIPS_a1, 4),
    MIPS_BNE(MIPS_a0, MIPS_a2, -4),
    MIPS_SW(MIPS_v1, -4, MIPS_a0),
    MIPS_LW(MIPS_v1, 0, MIPS_a1),
    MIPS_SW(MIPS_v1, offsetof(SH2State,cycles), MIPS_at),
    /* Restore all MIPS caller-saved registers */
    MIPS_LW(MIPS_v1, 64, MIPS_sp),
    MIPS_MTLO(MIPS_v1),
    MIPS_LW(MIPS_v1, 68, MIPS_sp),
    MIPS_MTHI(MIPS_v1),
    MIPS_LW(MIPS_v0,  0, MIPS_sp),
    MIPS_LW(MIPS_v1,  4, MIPS_sp),
    MIPS_LW(MIPS_a0,  8, MIPS_sp),
    MIPS_LW(MIPS_a1, 12, MIPS_sp),
    MIPS_LW(MIPS_a2, 16, MIPS_sp),
    MIPS_LW(MIPS_a3, 20, MIPS_sp),
    MIPS_LW(MIPS_t0, 24, MIPS_sp),
    MIPS_LW(MIPS_t1, 28, MIPS_sp),
    MIPS_LW(MIPS_t2, 32, MIPS_sp),
    MIPS_LW(MIPS_t3, 36, MIPS_sp),
    MIPS_LW(MIPS_t4, 40, MIPS_sp),
    MIPS_LW(MIPS_t5, 44, MIPS_sp),
    MIPS_LW(MIPS_t6, 48, MIPS_sp),
    MIPS_LW(MIPS_t7, 52, MIPS_sp),
    MIPS_LW(MIPS_t8, 56, MIPS_sp),
    MIPS_LW(MIPS_t9, 60, MIPS_sp),
    /* Restore the stack pointer and return */
    MIPS_JR(MIPS_ra),
    MIPS_ADDIU(MIPS_sp, MIPS_sp, 4*(18+24)),
};

/* Save all caller-saved MIPS registers on the stack */
static const uint32_t code_save_regs[] = {
    /* Add stack space for saving registers */
    MIPS_ADDIU(MIPS_sp, MIPS_sp, -(4*16)),
    MIPS_SW(MIPS_v0,  0, MIPS_sp),
    MIPS_SW(MIPS_v1,  4, MIPS_sp),
    MIPS_SW(MIPS_a0,  8, MIPS_sp),
    MIPS_SW(MIPS_a1, 12, MIPS_sp),
    MIPS_SW(MIPS_a2, 16, MIPS_sp),
    MIPS_SW(MIPS_a3, 20, MIPS_sp),
    MIPS_SW(MIPS_t0, 24, MIPS_sp),
    MIPS_SW(MIPS_t1, 28, MIPS_sp),
    MIPS_SW(MIPS_t2, 32, MIPS_sp),
    MIPS_SW(MIPS_t3, 36, MIPS_sp),
    MIPS_SW(MIPS_t4, 40, MIPS_sp),
    MIPS_SW(MIPS_t5, 44, MIPS_sp),
    MIPS_SW(MIPS_t6, 48, MIPS_sp),
    MIPS_SW(MIPS_t7, 52, MIPS_sp),
    MIPS_SW(MIPS_t8, 56, MIPS_sp),
    MIPS_SW(MIPS_t9, 60, MIPS_sp),
    MIPS_MFLO(MIPS_v1),
    MIPS_SW(MIPS_v1, 64, MIPS_sp),
    MIPS_MFHI(MIPS_v1),
    MIPS_JR(MIPS_ra),
    MIPS_SW(MIPS_v1, 68, MIPS_sp),
};

/* Restore all caller-saved MIPS registers from the stack */
static const uint32_t code_restore_regs[] = {
    MIPS_LW(MIPS_v1, 64, MIPS_sp),
    MIPS_MTLO(MIPS_v1),
    MIPS_LW(MIPS_v1, 68, MIPS_sp),
    MIPS_MTHI(MIPS_v1),
    MIPS_LW(MIPS_v0,  0, MIPS_sp),
    MIPS_LW(MIPS_v1,  4, MIPS_sp),
    MIPS_LW(MIPS_a0,  8, MIPS_sp),
    MIPS_LW(MIPS_a1, 12, MIPS_sp),
    MIPS_LW(MIPS_a2, 16, MIPS_sp),
    MIPS_LW(MIPS_a3, 20, MIPS_sp),
    MIPS_LW(MIPS_t0, 24, MIPS_sp),
    MIPS_LW(MIPS_t1, 28, MIPS_sp),
    MIPS_LW(MIPS_t2, 32, MIPS_sp),
    MIPS_LW(MIPS_t3, 36, MIPS_sp),
    MIPS_LW(MIPS_t4, 40, MIPS_sp),
    MIPS_LW(MIPS_t5, 44, MIPS_sp),
    MIPS_LW(MIPS_t6, 48, MIPS_sp),
    MIPS_LW(MIPS_t7, 52, MIPS_sp),
    MIPS_LW(MIPS_t8, 56, MIPS_sp),
    MIPS_LW(MIPS_t9, 60, MIPS_sp),
    MIPS_JR(MIPS_ra),
    MIPS_ADDIU(MIPS_sp, MIPS_sp, 4*16),
};

#endif  // RTL_TRACE_STEALTH_FOR_SH2

/*************************************************************************/
/*********************** Main translation routine ************************/
/*************************************************************************/

/**
 * rtl_translate_block_mips:  Translate the given block into MIPS code.
 *
 * [Parameters]
 *        block: RTLBlock to translate
 *     code_ret: Pointer to variable to receive code buffer pointer
 *     size_ret: Pointer to variable to receive code buffer size (in bytes)
 * [Return value]
 *     Nonzero on success, zero on error
 * [Notes]
 *     On error, *code_ret and *size_ret are not modified.
 */
int rtl_translate_block_mips(RTLBlock *block, void **code_ret,
                             uint32_t *size_ret)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(code_ret != NULL, return 0);
    PRECOND(size_ret != NULL, return 0);

    /* Handle manually-optimized cases specially for increased efficiency */
    if (block->num_insns == 4
     && block->insns[0].opcode == RTLOP_LOAD_PARAM
     && block->insns[0].src_imm == 0
     && block->insns[1].opcode == RTLOP_LOAD_ADDR
     && block->insns[2].opcode == RTLOP_CALL
     && block->insns[2].src1 == block->insns[0].dest
     && block->insns[2].target == block->insns[1].dest
     && block->insns[3].opcode == RTLOP_RETURN
    ) {
        const unsigned int length = 16;
        uint32_t *code = malloc(length);
        if (!code) {
            DMSG("No memory for native buffer (%u bytes)", length);
            return 0;
        }
        if ((uintptr_t)code >> 28 == block->insns[1].src_addr >> 28) {
            code[0] = MIPS_J((block->insns[1].src_addr & 0x0FFFFFFF) >> 2);
            code[1] = MIPS_NOP();
        } else {
            code[0] = MIPS_LUI(MIPS_at, block->insns[1].src_addr >> 16);
            code[1] = MIPS_ORI(MIPS_at, MIPS_at,
                               block->insns[1].src_addr & 0xFFFF);
            code[2] = MIPS_JR(MIPS_at);
            code[3] = MIPS_NOP();
        }
        *code_ret = code;
        *size_ret = length;
        return 1;
    }

    /* Initialize translation-specific fields */
    block->native_buffer = NULL;
    block->native_bufsize = 0;
    block->native_length = 0;
    block->label_offsets = NULL;
    block->mips.need_save_ra = 0;
    block->mips.need_chain_at = 0;
    block->mips.frame_used = 0;
    block->mips.sreg_used = 0;

    /* Check that the number of labels won't cause problems for our fake
     * MIPS instructions */
    if (UNLIKELY(block->next_label > 1<<16)) {
        DMSG("%p: Too many labels (%u, max %u)", block, (1<<16) - 1,
             block->next_label - 1);
    }

    /* Allocate the label map */
    if (block->next_label > 0) {  // Should always be true, but just in case
        block->label_offsets =
            malloc(sizeof(*block->label_offsets) * block->next_label);
        if (UNLIKELY(!block->label_offsets)) {
            DMSG("No memory for block label offsets (%u bytes)",
                 sizeof(*block->label_offsets) * block->next_label);
            goto fail;
        }
        memset(block->label_offsets, -1,
               sizeof(*block->label_offsets) * block->next_label);
    }

    /* Allocate an initial native code buffer for the block */
    block->native_bufsize = NATIVE_EXPAND_SIZE;
    block->native_buffer = malloc(block->native_bufsize);
    if (UNLIKELY(!block->native_buffer)) {
        DMSG("No memory for native buffer (%u bytes)", block->native_bufsize);
        goto fail;
    }

    /* Allocate MIPS registers (and possibly stack frame locations) for all
     * RTL registers, and perform other pre-translation scanning and
     * optimization */
    if (UNLIKELY(!allocate_registers(block))) {
        goto fail;
    }
#ifdef RTL_TRACE_GENERATE
    print_regmap(block);
#endif

    /* Translate the RTL instructions into MIPS code */
#ifdef RTL_TRACE_STEALTH_FOR_SH2
    block->sh2_regcache_mask = 0;
#endif
    if (UNLIKELY(!translate_block(block))) {
        goto fail;
    }

    /* Free the branch label offset table */
    free(block->label_offsets);
    block->label_offsets = NULL;

    /* Shrink the buffer down to the actual length before returning it */
    *code_ret = realloc(block->native_buffer, block->native_length);
    if (UNLIKELY(!*code_ret)) {
        DMSG("realloc() to a smaller size failed?!");
        *code_ret = block->native_buffer;
    }
    *size_ret = block->native_length;

    /* Make sure we don't accidentally free the native code buffer while
     * the caller is using it */
    block->native_buffer = NULL;
    block->native_bufsize = 0;
    block->native_length = 0;

    /* Success */
    free(block->label_offsets);
    block->label_offsets = NULL;
    return 1;

  fail:
    free(block->native_buffer);
    free(block->label_offsets);
    block->native_buffer = NULL;
    block->native_bufsize = 0;
    block->native_length = 0;
    block->label_offsets = NULL;
    return 0;
}

/*************************************************************************/
/************************** Register allocation **************************/
/*************************************************************************/

/**
 * allocate_registers:  Allocate MIPS registers (and, if necessary, stack
 * frame locations) for all RTL registers in the block.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int allocate_registers(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    int unit_index;

    /*
     * The basic algorithm used for allocating registers is linear scan, as
     * described by Poletto and Sarkar.  However, since we do not have a
     * sorted list of live intervals, we instead iterate through the
     * instruction stream, allocating a hardware register (or frame slot)
     * for each RTL register the first time it is encountered.
     *
     * Since live intervals calculated by the core RTL code do not take
     * into account backward branches, the register allocation code checks
     * each basic unit for entering edges from later units in the code
     * stream, and if such a unit is found, updates the live intervals of
     * all live registers to extend through the end of that unit (the
     * latest unit in code stream order if there is more than one).
     *
     * The basic algorithm is tweaked as follows for the MIPS CPU:
     *
     *    - Registers which are live over multiple basic units including a
     *      unit with a subroutine call are preferentially assigned callee-
     *      saved registers $s0-$s8, while registers which are only live
     *      within a single basic unit or which do not cross a subroutine
     *      call are preferentially assigned caller-saved registers $v1,
     *      $a0-$a3, and $t0-$t9.  ($at and $v1 are reserved for loading
     *      values from the stack frame to be used as instruction
     *      operands.)  However, constants are always assigned caller-saved
     *      registers, since it is often faster (and never slower) to
     *      reload them with addiu/ori/lui instructions than to save and
     *      restore another register on the stack.
     *
     *    - Among caller-saved registers, preference is given to registers
     *      whose last use was longer ago.  This assists instruction
     *      rescheduling by providing a larger live window for each
     *      hardware register.  Callee-saved registers are allocated in
     *      numerical order regardless of last use to minimize the number
     *      of such registers which need to be saved and restored.
     *
     *    - When spilling registers, the register with the shortest usage
     *      interval is spilled, rather than the one with the longest.
     *      (Spilling the longest interval first would cause the SH-2 state
     *      block pointer to be spilled, significantly impacting
     *      performance.)
     *      [FIXME: Currently, we always spill the new register.]
     */

    /* Clear the register map, stack frame map, and active register list */
    memset(block->mips.reg_map, 0, sizeof(block->mips.reg_map));
    block->mips.reg_free = ~0;
    memset(block->mips.frame_map, 0, sizeof(block->mips.frame_map));
    memset(block->mips.frame_free, ~0, sizeof(block->mips.frame_free));
    block->mips.first_active = -1;

    /* Pick up the first unit with a CALL instruction, so we know where
     * we'll need to use callee-saved registers */
    block->mips.next_call_unit = block->first_call_unit;

    /* Allocate registers for each basic unit in code stream order */
    for (unit_index = 0; unit_index >= 0;
         unit_index = block->units[unit_index].next_unit
    ) {
        if (UNLIKELY(!allocate_regs_unit(block, unit_index))) {
            return 0;
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * allocate_regs_unit:  Allocate MIPS registers for an RTL basic unit.
 *
 * [Parameters]
 *     block: RTL block
 *      unit: Index of basic unit
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int allocate_regs_unit(RTLBlock * const block,
                              const unsigned int unit_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    RTLUnit * const unit = &block->units[unit_index];

    /* Copy the current register and stack frame map to the unit structure */

    memcpy(unit->mips.reg_map, block->mips.reg_map,
           sizeof(block->mips.reg_map));
    memcpy(unit->mips.frame_map, block->mips.frame_map,
           sizeof(block->mips.frame_map));

    /* Move to the next call unit if we reached the current one */

    while (block->mips.next_call_unit >= 0
        && block->mips.next_call_unit <= unit_index
    ) {
        block->mips.next_call_unit =
            block->units[block->mips.next_call_unit].next_call_unit;
    }

    /* Scan through RTL instructions and allocate MIPS registers */

    int32_t insn_index;  // Signed so we catch last_insn==-1 properly
    for (insn_index = unit->first_insn; insn_index <= unit->last_insn;
         insn_index++
    ) {
        if (UNLIKELY(!allocate_regs_insn(block, unit_index, insn_index))) {
            return 0;
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * allocate_regs_insn:  Perform register allocation for registers used in a
 * single RTL instruction.
 *
 * [Parameters]
 *          block: RTL block
 *     unit_index: Index of current RTL unit
 *     insn_index: Index of RTL instruction to allocate registers for
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int allocate_regs_insn(RTLBlock * const block,
                                     const unsigned int unit_index,
                                     const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(unit_index < block->num_units, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    RTLInsn * const insn = &block->insns[insn_index];

  retry:
    switch ((RTLOpcode)insn->opcode) {

      case RTLOP_NOP:
#ifdef RTL_TRACE_STEALTH_FOR_SH2
        if (insn->src_imm & 0x80000000) {
            /* If we have stealth trace NOPs, we'll be calling subroutines,
             * so ensure $ra is saved */
            block->mips.need_save_ra = 1;
        }
#endif
        return 1;

      case RTLOP_LABEL:
      case RTLOP_GOTO:
      case RTLOP_RETURN:
        return 1;

      case RTLOP_LOAD_IMM:
      case RTLOP_LOAD_ADDR:
        /* If this register is unused elsewhere, kill the instruction */
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
#ifdef MIPS_OPTIMIZE_IMMEDIATE
        /* Try to optimize this and the following instruction into a single
         * immediate instruction (but only if it's part of the same unit) */
        if (insn_index < block->units[unit_index].last_insn
         && optimize_immediate(block, insn_index)
        ) {
            if (insn->opcode != RTLOP_LOAD_IMM
             && insn->opcode != RTLOP_LOAD_ADDR
            ) {
                goto retry;  // Instruction was altered
            }
        }
#endif
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (!block->regs[insn->dest].native_allocated
         && merge_constant_register(block, unit_index, insn_index,
                                    insn->dest)
        ) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->dest);

      case RTLOP_LOAD_PARAM:
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        /* If it's a long-lived register, let allocate_one_register()
         * select a callee-saved register; otherwise, try to allocate the
         * appropriate parameter register to avoid an unnecessary MOVE */
        if (block->mips.next_call_unit >= 0
         && block->regs[insn->dest].death
                >= block->units[block->mips.next_call_unit].first_insn
        ) {
            return allocate_one_register(block, unit_index, insn_index,
                                         insn->dest);
        } else {
            return allocate_one_register_prefer(
                block, unit_index, insn_index, insn->dest,
                MIPS_a0 + block->regs[insn->dest].param_index
            );
        }

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
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src1))) {
            return 0;
        }
        /* Source registers can be reused as destination registers */
        clean_active_list(block, insn_index+1);
        if (insn->opcode == RTLOP_ANDI && insn->src_imm > 0xFFFF
         && block->regs[insn->src1].native_reg != MIPS_noreg
        ) {
            /* This might be handled as "ins dest,$zero,...", so try to
             * make the destination use the same register as the source */
            return allocate_one_register_prefer(
                block, unit_index, insn_index, insn->dest,
                block->regs[insn->src1].native_reg
            );
        } else {
            return allocate_one_register(block, unit_index, insn_index,
                                         insn->dest);
        }

      case RTLOP_STORE_B:
      case RTLOP_STORE_H:
      case RTLOP_STORE_W:
      case RTLOP_STORE_PTR:
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->src1)
            && allocate_one_register(block, unit_index, insn_index,
                                     insn->dest);

      case RTLOP_ADD:
      case RTLOP_SUB:
      case RTLOP_AND:
      case RTLOP_OR:
      case RTLOP_XOR:
      case RTLOP_SLL:
      case RTLOP_SRL:
      case RTLOP_SRA:
      case RTLOP_ROR:
      case RTLOP_SLTU:
      case RTLOP_SLTS:
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->src2].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src1))
         || UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src2))
        ) {
            return 0;
        }
        clean_active_list(block, insn_index+1);
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->dest);

      case RTLOP_SELECT: {
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->src2].last_used = insn_index;
        block->regs[insn->cond].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src1))
         || UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src2))
         || UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->cond))
        ) {
            return 0;
        }
        clean_active_list(block, insn_index+1);
        /* We implement SELECT with a MOVE/MOVZ pair in the general case.
         * If the target shares a hardware register with either of the
         * source operands, we can drop the MOVE and use a single MOVZ or
         * MOVN instead, so try to do that. */
        int desired_reg;
        if (((desired_reg = block->regs[insn->src1].native_reg) != MIPS_noreg
             && desired_reg != MIPS_zero
             && (block->mips.reg_free & (1 << desired_reg)))
         || ((desired_reg = block->regs[insn->src2].native_reg) != MIPS_noreg
             && desired_reg != MIPS_zero
             && (block->mips.reg_free & (1 << desired_reg)))
        ) {
            return allocate_one_register_prefer(block, unit_index, insn_index,
                                                insn->dest, desired_reg);
        } else {
            return allocate_one_register(block, unit_index, insn_index,
                                         insn->dest);
        }
      }

      case RTLOP_BFINS: {
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->src2].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src1))
         || UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src2))
        ) {
            return 0;
        }
        clean_active_list(block, insn_index+1);
        /* The INS instruction treats its destination (rt) as read-write,
         * so try to allocate the destination in the same register as src1.
         * If we fail, we'll have to MOVE(dest,src1) at translation time. */
        if (block->regs[insn->src1].native_reg == MIPS_noreg
         || block->regs[insn->src1].native_reg == MIPS_zero
        ) {
            return allocate_one_register(block, unit_index, insn_index,
                                         insn->dest);
        } else {
            const int desired_reg = block->regs[insn->src1].native_reg;
            return allocate_one_register_prefer(block, unit_index, insn_index,
                                                insn->dest, desired_reg);
        }
      }

      case RTLOP_MULU:
      case RTLOP_MULS:
      case RTLOP_MADDU:
      case RTLOP_MADDS:
      case RTLOP_DIVMODU:
      case RTLOP_DIVMODS:
        if (UNLIKELY((!insn->dest || block->regs[insn->dest].birth == block->regs[insn->dest].death)
                  && (!insn->dest2 || block->regs[insn->dest2].birth == block->regs[insn->dest2].death))
        ) {
            insn->opcode = RTLOP_NOP;
            insn->src_imm = 0;
            return 1;
        }
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->src2].last_used = insn_index;
        block->regs[insn->dest].last_used = insn_index;
        block->regs[insn->dest2].last_used = insn_index;
        clean_active_list(block, insn_index);
        if (UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src1))
         || UNLIKELY(!allocate_one_register(block, unit_index, insn_index,
                                            insn->src2))
        ) {
            return 0;
        }
        clean_active_list(block, insn_index+1);
        return (!insn->dest
                || allocate_one_register(block, unit_index, insn_index,
                                         insn->dest))
            && (!insn->dest2
                || allocate_one_register(block, unit_index, insn_index,
                                         insn->dest2));

      case RTLOP_GOTO_IF_Z:
      case RTLOP_GOTO_IF_NZ:
        block->regs[insn->src1].last_used = insn_index;
        clean_active_list(block, insn_index);
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->src1);

      case RTLOP_GOTO_IF_E:
      case RTLOP_GOTO_IF_NE:
        block->regs[insn->src1].last_used = insn_index;
        block->regs[insn->src2].last_used = insn_index;
        clean_active_list(block, insn_index);
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->src1)
            && allocate_one_register(block, unit_index, insn_index,
                                     insn->src2);

      case RTLOP_CALL: {
        if (UNLIKELY(block->regs[insn->dest].birth == block->regs[insn->dest].death)) {
            insn->dest = 0;
            /* Execute the call anyway, because it may have side effects */
        }
        if (insn->src1) {
            block->regs[insn->src1].last_used = insn_index;
        }
        if (insn->src2) {
            block->regs[insn->src2].last_used = insn_index;
        }
        block->regs[insn->target].last_used = insn_index;
        if (insn->dest) {
            block->regs[insn->dest].last_used = insn_index;
        }
        clean_active_list(block, insn_index);
        if ((insn->src1
             && UNLIKELY(!allocate_one_register_prefer(block, unit_index, insn_index, insn->src1, MIPS_a0)))
         || (insn->src2
             && UNLIKELY(!allocate_one_register_prefer(block, unit_index, insn_index, insn->src2, MIPS_a1)))
         || UNLIKELY(!allocate_one_register_prefer(block, unit_index, insn_index, insn->target, MIPS_t9))
        ) {
            return 0;
        }
        clean_active_list(block, insn_index+1);
        /* Null out any free caller-saved registers because they'll have
         * been clobbered by the call (otherwise we might try to reuse a
         * constant value that's no longer valid) */
        unsigned int i;
        for (i = 0; i < lenof(caller_saved_regs); i++) {
            const unsigned int mips_reg = caller_saved_regs[i];
            if (block->mips.reg_free & (1 << mips_reg)) {
                block->mips.reg_map[mips_reg] = NULL;
            }
        }
        if (insn->dest
            && UNLIKELY(!allocate_one_register_prefer(block, unit_index, insn_index, insn->dest, MIPS_v0))
        ) {
            return 0;
        }
        /* We'll need to save $ra now */
        block->mips.need_save_ra = 1;
        return 1;
      }  // case RTLOP_CALL

      case RTLOP_RETURN_TO:
        block->regs[insn->target].last_used = insn_index;
        clean_active_list(block, insn_index);
        return allocate_one_register(block, unit_index, insn_index,
                                     insn->target);

    }  // switch (insn->opcode)

    DMSG("%p/%u: Invalid RTL opcode %u", block, insn_index, insn->opcode);
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * add_to_active_list:  Add a register to the active list.
 *
 * [Parameters]
 *        block: RTL block
 *     is_frame: Nonzero if the register is in a frame slot, zero if in a
 *                  hardware register
 *        index: MIPS register index or frame slot index
 * [Return value]
 *     None
 */
static void add_to_active_list(RTLBlock * const block, const int is_frame,
                               const unsigned int index)
{
    PRECOND(block != NULL, return);

    const unsigned int list_index = is_frame ? 32+index : index;
    block->mips.active_list[list_index].is_frame = is_frame ? 1 : 0;
    block->mips.active_list[list_index].index = index;
    const uint32_t death = is_frame ? block->mips.frame_map[index]->death
                                    : block->mips.reg_map[index]->death;

    int insert_at = -1;
    int next = block->mips.first_active;
    while (next >= 0
        && (block->mips.active_list[next].is_frame
            ? block->mips.frame_map[block->mips.active_list[next].index]->death
            : block->mips.reg_map[block->mips.active_list[next].index]->death)
           < death
    ) {
        insert_at = next;
        next = block->mips.active_list[next].next;
    }
    if (insert_at < 0) {
        block->mips.first_active = list_index;
    } else {
        block->mips.active_list[insert_at].next = list_index;
    }
    block->mips.active_list[list_index].next = next;
}

/*----------------------------------*/

/**
 * remove_from_active_list:  Explicitly remove a register from the active
 * list.  Used when merging constants or updating the death time of an
 * active register.  This function does NOT set the "free" bit for the
 * freed register or frame slot.
 *
 * [Parameters]
 *        block: RTL block
 *     is_frame: Nonzero if the register is in a frame slot, zero if in a
 *                  hardware register
 *        index: MIPS register index or frame slot index
 * [Return value]
 *     None
 */
static void remove_from_active_list(RTLBlock * const block, const int is_frame,
                                    const unsigned int index)
{
    PRECOND(block != NULL, return);

    const unsigned int list_index = is_frame ? 32+index : index;

    int prev = -1;
    int next = block->mips.first_active;
    while (next >= 0 && next != list_index) {
        prev = next;
        next = block->mips.active_list[next].next;
    }
    if (next >= 0) {
        next = block->mips.active_list[next].next;
        if (prev < 0) {
            block->mips.first_active = next;
        } else {
            block->mips.active_list[prev].next = next;
        }
    }
}

/*----------------------------------*/

/**
 * clean_active_list:  Clean all dead registers from the active list.
 *
 * [Parameters]
 *          block: RTL block
 *     clean_insn: Instruction index for cleaning registers (any register
 *                    with death < clean_insn is cleaned)
 * [Return value]
 *     None
 */
static void clean_active_list(RTLBlock * const block,
                              const uint32_t clean_insn)
{
    PRECOND(block != NULL, return);

    int first_active = block->mips.first_active;
    while (first_active >= 0
        && (block->mips.active_list[first_active].is_frame
            ? block->mips.frame_map[block->mips.active_list[first_active].index]->death
            : block->mips.reg_map[block->mips.active_list[first_active].index]->death)
           < clean_insn
    ) {
        const unsigned int index = block->mips.active_list[first_active].index;
        if (block->mips.active_list[first_active].is_frame) {
            block->mips.frame_free[index/32] |= 1 << (index%32);
        } else {
            block->mips.reg_free |= 1 << index;
        }
        first_active = block->mips.active_list[first_active].next;
        block->mips.first_active = first_active;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * allocate_one_register:  Allocate a MIPS register for the given RTL
 * register, if it does not have one allocated already, and update the
 * current register map.
 *
 * [Parameters]
 *          block: RTL block
 *     unit_index: Index of current basic unit
 *     insn_index: Index of current instruction
 *      reg_index: Index of RTL register
 * [Return value]
 *     Nonzero on success, zero on error
 * [Notes]
 *     This routine only maps into the available caller-saved registers,
 *     $3-$15 and $24-$25 ($v1, $a0-$a3, and $t0-$t9); the callee-saved
 *     registers ($s0-$s8) are only used when renaming.
 */
static int allocate_one_register(RTLBlock * const block,
                                 const unsigned int unit_index,
                                 const uint32_t insn_index,
                                 const unsigned int reg_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(reg_index < block->next_reg, return 0);

    RTLRegister * const reg = &block->regs[reg_index];

    /* If it's already allocated, just return */
    if (reg->native_allocated) {
        return 1;
    }

    /* If it's a constant zero value, just use $zero */
    if (reg->source == RTLREG_CONSTANT && reg->value == 0) {
        reg->native_allocated = 1;
        reg->native_reg = MIPS_zero;
        /* We don't mark it in the register map or add it to the active
         * list since $zero can be shared */
        return 1;
    }

    /* Macro to select a register if its last use is the oldest so far */
    #define SELECT_REG_BY_AGE(mips_reg)  {              \
        const int __mips_reg = (mips_reg);              \
        if (block->mips.reg_free & (1 << __mips_reg)) { \
            if (!block->mips.reg_map[__mips_reg]) {     \
                best_mips = __mips_reg;                 \
                break;                                  \
            } else if (block->mips.reg_map[__mips_reg]->death < oldest) { \
                best_mips = __mips_reg;                 \
                oldest = block->mips.reg_map[__mips_reg]->death; \
            }                                           \
        }                                               \
    }
    /* Macro to select a register unconditionally if it's free */
    #define SELECT_REG_BY_FREE(mips_reg)  {             \
        const int __mips_reg = (mips_reg);              \
        if (block->mips.reg_free & (1 << __mips_reg)) { \
            best_mips = __mips_reg;                     \
            break;                                      \
        }                                               \
    }

    int best_mips = -1;
    uint32_t oldest = insn_index;  // Last use of best_$mips
    unsigned int i;
    if (reg->source != RTLREG_CONSTANT
     && block->mips.next_call_unit >= 0
     && reg->death >= block->units[block->mips.next_call_unit].first_insn
    ) {
        /* Prefer a callee-saved register for non-constant, long-lived
         * registers */
        for (i = 0; i < lenof(callee_saved_regs); i++) {
            SELECT_REG_BY_FREE(callee_saved_regs[i]);
        }
        if (best_mips < 0) {
            for (i = 0; i < lenof(caller_saved_regs); i++) {
                SELECT_REG_BY_AGE(caller_saved_regs[i]);
            }
        }
    } else {
        /* Prefer a caller-saved register for short-lived registers */
        for (i = 0; i < lenof(caller_saved_regs); i++) {
            SELECT_REG_BY_AGE(caller_saved_regs[i]);
        }
        if (best_mips < 0) {
            for (i = 0; i < lenof(callee_saved_regs); i++) {
                SELECT_REG_BY_FREE(callee_saved_regs[i]);
            }
        }
    }

    #undef SELECT_REG_BY_AGE
    #undef SELECT_REG_BY_FREE

    if (best_mips < 0) {
        /* No free registers, so give it a frame slot instead */
        reg->native_allocated = 1;
        reg->native_reg = MIPS_noreg;
        return allocate_frame_slot(block, insn_index, reg_index);
    }

    reg->native_allocated = 1;
    reg->native_reg = best_mips;
    block->mips.reg_map[best_mips] = &block->regs[reg_index];
    block->mips.reg_free &= ~(1 << best_mips);
    if (reg_is_callee_saved[best_mips]) {
        block->mips.sreg_used |= 1 << best_mips;
    }
    add_to_active_list(block, 0, best_mips);
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * allocate_one_register_prefer:  Allocate a MIPS register for the given
 * RTL register, like allocate_one_register(), but use MIPS register
 * "prefer_mips" if possible.  (If a MIPS register has already been
 * allocated, the allocation is left unchanged.)
 *
 * [Parameters]
 *           block: RTL block
 *      unit_index: Index of current basic unit
 *      insn_index: Index of current instruction
 *       reg_index: Index of RTL register
 *     prefer_mips: MIPS register to prefer
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int allocate_one_register_prefer(RTLBlock * const block,
                                        const unsigned int unit_index,
                                        const uint32_t insn_index,
                                        const unsigned int reg_index,
                                        const unsigned int prefer_mips)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(reg_index < block->next_reg, return 0);
    PRECOND(prefer_mips < 32, return 0);

    RTLRegister * const reg = &block->regs[reg_index];

    if (!reg->native_allocated && (block->mips.reg_free & (1<<prefer_mips))) {
        reg->native_allocated = 1;
        reg->native_reg = prefer_mips;
        block->mips.reg_map[prefer_mips] = &block->regs[reg_index];
        block->mips.reg_free &= ~(1 << prefer_mips);
        if (reg_is_callee_saved[prefer_mips]) {
            block->mips.sreg_used |= 1 << prefer_mips;
        }
        add_to_active_list(block, 0, prefer_mips);
        return 1;
    }

    /* We couldn't get the preferred register, so just allocate as usual */
    return allocate_one_register(block, unit_index, insn_index, reg_index);
}

/*-----------------------------------------------------------------------*/

/**
 * merge_constant_register:  Look for a MIPS hardware register holding the
 * same constant value as the given RTL register; if found, assign that
 * MIPS register to the given RTL register as well.
 *
 * When MIPS_OPTIMIZE_MERGE_CONSTANTS is disabled, this function will not
 * search for generic constant registers, but will still assign $zero to
 * RTL registers with the constant value 0.
 *
 * [Parameters]
 *          block: RTL block
 *     unit_index: Index of current basic unit
 *     insn_index: Index of current instruction
 *      reg_index: Index of RTL register
 * [Return value]
 *     Nonzero if the register was successfully merged with another, else zero
 */
static int merge_constant_register(RTLBlock * const block,
                                   const unsigned int unit_index,
                                   const uint32_t insn_index,
                                   const unsigned int reg_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(unit_index < block->num_units, return 0);
    PRECOND(reg_index < block->next_reg, return 0);
    PRECOND(!block->regs[reg_index].native_allocated, return 0);

    RTLRegister * const reg = &block->regs[reg_index];

    /* If it's a constant zero value, just use $zero */
    if (reg->source == RTLREG_CONSTANT && reg->value == 0) {
        reg->native_allocated = 1;
        reg->native_reg = MIPS_zero;
        /* We don't mark it in the register map or add it to the active
         * list since $zero can be shared */
        return 1;
    }

    /* If it's a constant that has the same value as another constant
     * that's already been loaded (and if MIPS_OPTIMIZE_MERGE_CONSTANTS
     * is enabled), reuse the same hardware register.  But don't reuse a
     * register that died in a previous basic unit, since it may be on a
     * different code path */
#ifdef MIPS_OPTIMIZE_MERGE_CONSTANTS
    if (reg->source == RTLREG_CONSTANT) {
        unsigned int mips_reg;
        for (mips_reg = MIPS_v1; mips_reg <= MIPS_s8; mips_reg++) {
            if (block->mips.reg_map[mips_reg]
             && block->mips.reg_map[mips_reg]->death
                    >= block->units[unit_index].first_insn
             && block->mips.reg_map[mips_reg]->source == RTLREG_CONSTANT
             && block->mips.reg_map[mips_reg]->value == reg->value
            ) {
                /* Found a match--assign this MIPS register to the
                 * current RTL register, and ensure the MIPS register
                 * stays allocated until all merged registers are dead */
                reg->native_allocated = 1;
                reg->native_reg = mips_reg;
                if (reg->death < block->mips.reg_map[mips_reg]->death) {
                    reg->death = block->mips.reg_map[mips_reg]->death;
                }
                reg->next_merged = block->mips.reg_map[mips_reg];
                block->mips.reg_map[mips_reg] = &block->regs[reg_index];
                RTLRegister *merged_reg = reg;
                while ((merged_reg = merged_reg->next_merged) != NULL) {
                    merged_reg->death = reg->death;
                }
                if (!(block->mips.reg_free & (1 << mips_reg))) {
                    /* The register's live interval has changed, so remove
                     * it from the active list (we'll re-add it below) */
                    remove_from_active_list(block, 0, mips_reg);
                }
                block->mips.reg_free &= ~(1 << mips_reg);
                add_to_active_list(block, 0, mips_reg);
                return 1;
            }
        }
    }
#endif

    /* Couldn't merge this register into another */
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * allocate_frame_slot:  Allocate a frame slot for the given RTL register,
 * if it does not have one allocated already.
 *
 * [Parameters]
 *          block: RTL block
 *     insn_index: Index of current instruction
 *      reg_index: Index of RTL register
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int allocate_frame_slot(RTLBlock * const block,
                               const uint32_t insn_index,
                               const unsigned int reg_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(reg_index < block->next_reg, return 0);

    RTLRegister * const reg = &block->regs[reg_index];

    if (reg->frame_allocated) {
        return 1;
    }

    const unsigned int num_slots = MIPS_FRAME_SIZE/4;
    unsigned int slot;
    for (slot = 0; slot < num_slots; slot++) {
        if (block->mips.frame_free[slot/32] & (1 << (slot%32))) {
            break;
        }
    }
    if (slot >= num_slots) {
        DMSG("%p/%u: No free frame slots for RTL register %u",
             block, insn_index, reg_index);
        return 0;
    }

    reg->frame_allocated = 1;
    reg->frame_slot = slot;
    reg->stack_offset = slot * 4;
    block->mips.frame_map[slot] = &block->regs[reg_index];
    block->mips.frame_free[slot/32] &= ~(1 << (slot%32));
    block->mips.frame_used = 1;
    add_to_active_list(block, 1, slot);
    return 1;
}

/*-----------------------------------------------------------------------*/

#ifdef MIPS_OPTIMIZE_IMMEDIATE

/**
 * optimize_immediate:  Attempt to optimize a sequence of LOAD_IMM or
 * LOAD_ADDR followed by an instruction using the loaded constant into a
 * sequence which uses fewer MIPS instructions.  On success, the RTL
 * instruction (and possibly following instructions) will be altered
 * appropriately.
 *
 * The following optimizations are performed when r1 is not used beyond
 * the second instruction and (for ALU instructions) the constant value is
 * within range of the particular instruction:
 *
 *    LOAD_IMM r1, constant; ALUOP r2, r3, r1
 *       --> NOP; ALUOPI r2, r3, constant
 *
 *    LOAD_ADDR r1, address; LOAD_{BU,BS,HU,HS,W} r2, offset(r1)
 *       --> LOAD_ADDR r1, %hi(address+offset);
 *           l{bu,b,hu,h,w} r2, %lo(address+offset)(r1)
 *
 *    LOAD_ADDR r1, address; STORE_{B,H,W} offset(r1), r2
 *       --> LOAD_ADDR r1, %hi(address+offset);
 *           s{b,h,w} r2, %lo(address+offset)(r1)
 *
 * where ALUOP is one of ADD, SUB, AND, OR, XOR, SLL, SRL, SRA, ROR, SLTU,
 * or SLTS.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     unit_index: Index of current basic unit
 *     insn_index: Index of RTL LOAD_IMM or LOAD_ADDR instruction
 * [Return value]
 *     Nonzero if the given RTL instruction was successfully optimized,
 *     else zero
 */
static int optimize_immediate(RTLBlock * const block,
                              const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);
    PRECOND(block->insns[insn_index].opcode == RTLOP_LOAD_IMM
            || block->insns[insn_index].opcode == RTLOP_LOAD_ADDR,
            return 0);
    PRECOND(block->insns[insn_index].dest != 0
            && block->insns[insn_index].dest < block->next_reg, return 0);

    RTLInsn *insn = &block->insns[insn_index];
    const uint32_t imm_index = insn[0].dest;
    const uintptr_t value = block->regs[imm_index].value;

    if (block->regs[imm_index].death > insn_index+1) {
        /* Register is used later, so we can't optimize the load out */
        return 0;
    }

    switch ((RTLOpcode)insn[1].opcode) {

      case RTLOP_ADD:
        if (value + 0x8000 <= 0xFFFF) {
            if (insn[1].src2 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ADDI;
                insn[1].src_imm = value;
                return 1;
            } else if (insn[1].src1 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ADDI;
                insn[1].src1 = insn[1].src2;
                insn[1].src_imm = value;
                return 1;
            }
        }
        return 0;

      case RTLOP_SUB:
        if (value + 0x7FFF <= 0xFFFF && insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_ADDI;
            insn[1].src_imm = -value;
            return 1;
        }
        return 0;

      case RTLOP_AND: {
        int is_insable = 0;  // Can we turn it into "ins reg,$zero,..."?
        if (value & 1) {
            /* Must be 0b000...111 */
            is_insable = ((value & (value+1)) == 0);
        } else {
            /* Must be 0b111...000 */
            is_insable = ((~value & (~value+1)) == 0);
        }
        if (value <= 0xFFFF || is_insable) {
            if (insn[1].src2 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ANDI;
                insn[1].src_imm = value;
                return 1;
            } else if (insn[1].src1 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ANDI;
                insn[1].src1 = insn[1].src2;
                insn[1].src_imm = value;
                return 1;
            }
        }
        return 0;
      }  // case RTLOP_AND

      case RTLOP_OR:
        if (value <= 0xFFFF) {
            if (insn[1].src2 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ORI;
                insn[1].src_imm = value;
                return 1;
            } else if (insn[1].src1 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_ORI;
                insn[1].src1 = insn[1].src2;
                insn[1].src_imm = value;
                return 1;
            }
        }
        return 0;

      case RTLOP_XOR:
        if (value <= 0xFFFF) {
            if (insn[1].src2 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_XORI;
                insn[1].src_imm = value;
                return 1;
            } else if (insn[1].src1 == imm_index) {
                insn[0].opcode = RTLOP_NOP;
                insn[0].src_imm = 0;
                insn[1].opcode = RTLOP_XORI;
                insn[1].src1 = insn[1].src2;
                insn[1].src_imm = value;
                return 1;
            }
        }
        return 0;

      case RTLOP_SLL:
        if (insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_SLLI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      case RTLOP_SRL:
        if (insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_SRLI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      case RTLOP_SRA:
        if (insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_SRAI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      case RTLOP_ROR:
        if (insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_RORI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      case RTLOP_SLTU:
        if (value + 0x8000 <= 0xFFFF && insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_SLTUI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      case RTLOP_SLTS:
        if (value + 0x8000 <= 0xFFFF && insn[1].src2 == imm_index) {
            insn[0].opcode = RTLOP_NOP;
            insn[0].src_imm = 0;
            insn[1].opcode = RTLOP_SLTSI;
            insn[1].src_imm = value;
            return 1;
        }
        return 0;

      /*----------------------------*/

      case RTLOP_LOAD_BU:
      case RTLOP_LOAD_BS:
      case RTLOP_LOAD_HU:
      case RTLOP_LOAD_HS:
      case RTLOP_LOAD_W:
      case RTLOP_LOAD_PTR:
        if (insn[1].src1 == imm_index) {
            const uint32_t address = value + insn[1].offset;
            insn[0].src_imm = (address + 0x8000) & 0xFFFF0000;
            block->regs[imm_index].value = insn[0].src_imm;
            insn[1].offset = (int16_t)(address & 0xFFFF);
            return 1;
        }
        return 0;

      case RTLOP_STORE_B:
      case RTLOP_STORE_H:
      case RTLOP_STORE_W:
      case RTLOP_STORE_PTR:
        if (insn[1].dest == imm_index) {
            const uint32_t address = value + insn[1].offset;
            insn[0].src_imm = (address + 0x8000) & 0xFFFF0000;
            block->regs[imm_index].value = insn[0].src_imm;
            insn[1].offset = (int16_t)(address & 0xFFFF);
            return 1;
        }
        return 0;

      /*----------------------------*/

      case RTLOP_NOP:
      case RTLOP_MOVE:
      case RTLOP_SELECT:
      case RTLOP_MULU:
      case RTLOP_MULS:
      case RTLOP_MADDU:
      case RTLOP_MADDS:
      case RTLOP_DIVMODU:
      case RTLOP_DIVMODS:
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
      case RTLOP_BFINS:
      case RTLOP_LOAD_IMM:
      case RTLOP_LOAD_ADDR:
      case RTLOP_LOAD_PARAM:
      case RTLOP_LABEL:
      case RTLOP_GOTO:
      case RTLOP_GOTO_IF_Z:
      case RTLOP_GOTO_IF_NZ:
      case RTLOP_GOTO_IF_E:
      case RTLOP_GOTO_IF_NE:
      case RTLOP_CALL:
      case RTLOP_RETURN:
      case RTLOP_RETURN_TO:
        return 0;

    }  // switch (insn[1].opcode)

    DMSG("%p/%u: Invalid RTL opcode %u", block, insn_index, insn[1].opcode);
    return 0;
}

#endif  // MIPS_OPTIMIZE_IMMEDIATE

/*-----------------------------------------------------------------------*/

#ifdef RTL_TRACE_GENERATE

/* Helper macro for print_regmap() to check the status of an instruction
 * operand and update the line buffer and register map accordingly */

# define CHECK_REG(which)  do {                                         \
    const uint32_t __rtl_reg = insn->which;                             \
    if (__rtl_reg > 0 && __rtl_reg < block->next_reg                    \
     && block->regs[__rtl_reg].native_allocated                         \
     && block->regs[__rtl_reg].native_reg != MIPS_noreg                 \
    ) {                                                                 \
        const uint32_t __mips_reg = block->regs[__rtl_reg].native_reg;  \
        const unsigned int __column = columns[__mips_reg];              \
        if (!__column) {                                                \
            break;                                                      \
        }                                                               \
        if (block->regs[__rtl_reg].birth == insn_index) {               \
            block->mips.reg_map[__mips_reg] = &block->regs[__rtl_reg];  \
            unsigned int __value = __rtl_reg;                           \
            int __pos = 3;                                              \
            do {                                                        \
                linebuf[__column+__pos] = '0' + (__value % 10);         \
                __value /= 10;                                          \
                __pos--;                                                \
            } while (__value > 0 && __pos >= 0);                        \
            if (__pos >= 0) {                                           \
                linebuf[__column+__pos] = 'r';                          \
            }                                                           \
            print_line = 1;                                             \
        }                                                               \
    }                                                                   \
} while (0)

/*----------------------------------*/

/**
 * print_regmap:  Print the state of the RTL-to-MIPS register map over the
 * course of the block.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     None
 */
static void print_regmap(RTLBlock * const block)
{
    PRECOND(block != NULL, return);

    fprintf(stderr, "%p: MIPS register allocation:\n", block);
    fprintf(stderr, "     |  v0  v1:  a0  a1  a2  a3:"
                    "  t0  t1  t2  t3:  t4  t5  t6  t7:  t8  t9\n");
    fprintf(stderr, "-----+--------+----------------+"
                    "----------------+----------------+--------\n");
    static const char template[75] =
                    "     |        :                :"
                    "                :                :        \n";
    static const unsigned int columns[32] = {
         0, 0, 6,10, 15,19,23,27, 32,36,40,44, 49,53,57,61,
         0, 0, 0, 0,  0, 0, 0, 0, 66,70, 0, 0,  0, 0, 0, 0,
    };

    char linebuf[75];
    memcpy(linebuf, template, sizeof(template));
    memset(block->mips.reg_map, 0, sizeof(block->mips.reg_map));

    uint32_t insn_index;
    for (insn_index = 0; insn_index < block->num_insns; insn_index++) {
        const RTLInsn * const insn = &block->insns[insn_index];

        /* Only print lines where registers are born or die */
        int print_line = 0;

        /* Fill in active registers first */
        unsigned int mips_reg;
        for (mips_reg = MIPS_v0; mips_reg <= MIPS_t9;
             mips_reg = (mips_reg==MIPS_t7 ? MIPS_t8 : mips_reg+1)
        ) {
            if (block->mips.reg_map[mips_reg]
             && block->mips.reg_map[mips_reg]->death >= insn_index
            ) {
                unsigned int column = columns[mips_reg];
                if (column) {
                    if (block->mips.reg_map[mips_reg]->death == insn_index) {
                        block->mips.reg_map[mips_reg] = NULL;
                        linebuf[column+1] = '-';
                        linebuf[column+2] = '-';
                        linebuf[column+3] = '-';
                        print_line = 1;
                    } else {
                        linebuf[column+2] = '|';
                    }
                }
            }
        }

        /* For simplicity, don't bother checking opcodes; just look at any
         * register index that's in range and report it if it's become live */
        CHECK_REG(dest);
        CHECK_REG(src1);
        CHECK_REG(src2);
        CHECK_REG(dest2);

        /* Print the line (with instruction index) if appropriate */
        if (print_line) {
            unsigned int value = insn_index;
            int pos = 4;
            do {
                linebuf[pos] = '0' + (value % 10);
                value /= 10;
                pos--;
            } while (value > 0 && pos >= 0);
            fwrite(linebuf, sizeof(linebuf), 1, stderr);
        }

        /* Reset for the next line */
        memcpy(linebuf, template, sizeof(template));

    }  // for (insn_index)
}

#undef CHECK_REG

#endif  // RTL_TRACE_GENERATE

/*************************************************************************/
/************************ Instruction translation ************************/
/*************************************************************************/

/**
 * APPEND:  Append a MIPS instruction word to the native code buffer.
 */
#define APPEND(insn)  do {                       \
    if (UNLIKELY(!append_insn(block, (insn)))) { \
        return 0;                                \
    }                                            \
} while (0)

/**
 * APPEND_FLOAT:  Append a high-latency instruction (load, multiply or
 * divide) to the native code buffer.  If possible (and if rescheduling is
 * enabled), the instruction is floated up by at most "latency"
 * instructions to avoid a stall.
 */
#define APPEND_FLOAT(insn,latency)  do {                     \
    if (UNLIKELY(!append_float(block, (insn), (latency)))) { \
        return 0;                                            \
    }                                                        \
} while (0)

/**
 * APPEND_BRANCH:  Append a MIPS branch instruction to the native code buffer.
 */
#define APPEND_BRANCH(insn)  do {                  \
    if (UNLIKELY(!append_branch(block, (insn)))) { \
        return 0;                                  \
    }                                              \
} while (0)

/**
 * IS_SPILLED:  Return nonzero if the given register (by index) is spilled,
 * else zero.  Assumes the variable "insn_index" is available and valid,
 * and assumes that the register has been allocated a MIPS register.
 *
 * Under the current allocation scheme, simply returns nonzero iff the
 * given register is live and does not have a hardware register allocated.
 */
#define IS_SPILLED(reg_index) \
    (block->regs[(reg_index)].birth < insn_index \
     && block->regs[(reg_index)].native_reg == MIPS_noreg)

/**
 * MAP_REGISTER:  Look up the MIPS register mapped to the given RTL
 * register and store it in a local variable.  Pass one of the identifiers
 * "dest", "src1", "src2", "dest2", or "target" as the "which" parameter;
 * the MIPS register will be stored in the corresponding variable ("dest2"
 * and "target" must be declared in any blocks that use them).  If the RTL
 * register does not have an assigned MIPS register, it is loaded into a
 * temporary register.
 *
 * If the MIPS register must not collide with another register used by the
 * same instruction, pass the RTL register index of that register as the
 * "avoid" parameter (e.g. insn->src1 or insn->src2).  If the register to
 * be mapped is currently spilled and loading it would cause the "avoid"
 * register to be spilled, it is loaded into $at and left spilled.  If
 * there is no register to avoid, pass 0 as the "avoid" parameter.
 *
 * The "reload" parameter indicates whether the register should be reloaded
 * if it is not currently active.  At the moment, this only applies to
 * registers located on the stack.
 */
#define MAP_REGISTER(which,reload,avoid)  do {                          \
    which = __MAP_REGISTER(block, insn->which, (reload), (avoid));      \
    if (UNLIKELY(which < 0)) {                                          \
        return 0;                                                       \
    }                                                                   \
} while (0)
static int __MAP_REGISTER(RTLBlock * const block, const unsigned int reg_index,
                          const int reload, const uint32_t avoid_index)
{
    RTLRegister * const reg = &block->regs[reg_index];
    PRECOND(reg->native_allocated, return -1);
    int mips_reg = reg->native_reg;
    if (mips_reg == MIPS_noreg) {
        mips_reg = MIPS_at;
        if (block->mips.reg_map[mips_reg] == &block->regs[(avoid_index)]) {
            mips_reg = MIPS_v1;
        }
        PRECOND(reg->frame_allocated, return -1);
        if (reload
         && UNLIKELY(!append_insn(block, MIPS_LW(mips_reg, reg->stack_offset,
                                                 MIPS_sp)))
        ) {
            return -1;
        }
    }
    /* If it's in HI or LO, pull it out */
    if (reg->mips.is_in_hilo) {
        APPEND(reg->mips.is_in_hilo | mips_reg<<11);
        reg->mips.is_in_hilo = 0;
        /* We don't clear the register from block->mips.{hi,lo}_reg, in
         * case a subsequent madd/maddu instruction can make use of it */
    }
    /* Update the register map unless we're using $zero */
    if (mips_reg != MIPS_zero) {
        block->mips.reg_map[mips_reg] = reg;
    }
    /* Update the stack frame map if this register has a frame slot */
    if (reg->frame_allocated) {
        block->mips.frame_map[reg->frame_slot] = reg;
    }
    return mips_reg;
}

/**
 * MAYBE_SAVE:  Save the given register ("dest" or "dest2") to its frame
 * slot if one is assigned.
 */
#define MAYBE_SAVE(reg)  do {                                   \
    RTLRegister * const regptr = &block->regs[insn->reg];       \
    if (regptr->frame_allocated) {                              \
        APPEND(MIPS_SW(reg, regptr->stack_offset, MIPS_sp));    \
    }                                                           \
} while (0)

/*************************************************************************/

/**
 * translate_block:  Perform the actual translation of an RTL block.
 *
 * [Parameters]
 *     block: RTL block to translate
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int translate_block(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);

    int32_t i;

    /* Clear translation state */
    memset(block->mips.reg_map, 0, sizeof(block->mips.reg_map));
    block->mips.hi_reg = 0;
    block->mips.lo_reg = 0;
    memset(block->mips.frame_map, 0, sizeof(block->mips.frame_map));

    /* Determine the total frame size, including space for saving and
     * restoring callee-saved registers on entry/exit as well as for
     * saving caller-saved registers around a CALL instruction */
    if (block->mips.need_save_ra || block->mips.frame_used) {
        block->mips.total_frame_size =
            MIPS_FRAME_SIZE + 4*16 // 16 caller-saved regs (not $at/$v1)
                            + 4;   // $ra
    } else {
        block->mips.total_frame_size = 0;  // No space needed for CALLs
    }
    for (i = 0; i < lenof(callee_saved_regs); i++) {
        const unsigned int mips_reg = callee_saved_regs[i];
        if (block->mips.sreg_used & (1 << mips_reg)) {
            block->mips.total_frame_size += 4;
        }
    }
    block->mips.total_frame_size =
        (block->mips.total_frame_size + 4) & -8;  // Must be a multiple of 8

    /* Start with the function prologue */
    if (UNLIKELY(!append_prologue(block))) {
        return 0;
    }

    /* Translate code a unit at a time */
    for (i = 0; i >= 0; i = block->units[i].next_unit) {
        if (UNLIKELY(!translate_unit(block, i))) {
            return 0;
        }
    }

    /* Append the function epilogue */
    if (UNLIKELY(!append_epilogue(block))) {
        return 0;
    }

    /* Resolve branches to labels */
    if (UNLIKELY(!resolve_branches(block))) {
        return 0;
    }

    /* Finished */
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * translate_unit:  Translate an RTL basic unit into MIPS code.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     unit_index: Index of basic unit to translate
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int translate_unit(RTLBlock * const block,
                                 const unsigned int unit_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);
    PRECOND(unit_index < block->num_units, return 0);

    RTLUnit * const unit = &block->units[unit_index];
    uint32_t i;

    /* Reset the current register map to that at the beginning of the unit
     * as determined by flow analysis during register allocation */
    memcpy(block->mips.reg_map, unit->mips.reg_map, sizeof(unit->mips.reg_map));

    /* Translate each RTL instruction in the unit */
    block->mips.unit_start = block->native_length;
    i = unit->first_insn;
    while ((int32_t)i <= unit->last_insn) {
        const unsigned int num_processed = translate_insn(block, unit_index, i);
        if (UNLIKELY(!num_processed)) {
            return 0;
        }
        i += num_processed;
    }

    /* Flush out any values cached in HI/LO for safety */
    if (UNLIKELY(!flush_hilo(block, unit->last_insn+1))) {
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * translate_insn:  Translate a single RTL instruction into MIPS code.
 * Optimization may result in multiple RTL instructions being parsed.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     unit_index: Index of basic unit being translated
 *     insn_index: Index of RTL instruction to translate
 * [Return value]
 *     Number of RTL instructions processed (nonzero) on success, zero on error
 */
static inline unsigned int translate_insn(RTLBlock * const block,
                                          const unsigned int unit_index,
                                          const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    const RTLInsn *insn = &block->insns[insn_index];
    int dest, src1, src2;  // MIPS register indices (mapped only when needed)

    block->mips.reg_map[MIPS_at] = NULL;  // For MAP_REGISTER()

    /*------------------------------*/

    switch ((RTLOpcode)insn->opcode) {

      case RTLOP_NOP:
#ifdef RTL_TRACE_STEALTH_FOR_SH2
        if (insn->src_imm >> 28 == 0x8 || insn->src_imm >> 28 == 0xA) {
            return sh2_stealth_trace_insn(block, insn_index);
        } else if (insn->src_imm >> 28 == 0xB) {
            return sh2_stealth_cache_reg(block, insn_index);
        } else if (insn->src_imm >> 28 == 0xC) {
            return sh2_stealth_trace_store(block, insn_index);
        }
#endif  // RTL_TRACE_STEALTH_FOR_SH2
        if (insn->src_imm != 0) {
            /* Debugging stuff from JIT_DEBUG_INSERT_PC (sh2.c) */
            const uint32_t addr = insn->src_imm;
            APPEND(MIPS_LUI(MIPS_zero, addr>>16));
            APPEND(MIPS_ORI(MIPS_zero, MIPS_zero, addr & 0xFFFF));
        }
        return 1;

      /*----------------------------*/

      case RTLOP_MOVE:
        if (block->regs[insn->src1].mips.is_in_hilo
         && block->regs[insn->src1].death == insn_index
        ) {
            /* Handle this case specially--if we did it the usual way,
             * we'd waste a cycle with MFHI/MFLO to src1 followed by a
             * MOVE from src1 to dest. */
            MAP_REGISTER(dest, 0, 0);
            APPEND(block->regs[insn->src1].mips.is_in_hilo | dest<<11);
            block->regs[insn->src1].mips.is_in_hilo = 0;
        } else {
            MAP_REGISTER(src1, 1, 0);
            /* Note: we have to map the output operand _after_ all the input
             * operands, or else the output operand will get spilled if it
             * shares a hardware register with an input operand.  (This isn't
             * actually true with the current allocation scheme, since we never
             * share hardware registers between multiple live RTL registers,
             * but it's applicable in general, and it doesn't hurt anything to
             * do it this way.) */
            MAP_REGISTER(dest, 0, 0);
            APPEND(MIPS_MOVE(dest, src1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SELECT: {
        int cond;
        int spilled_v1 = 0;
        if (block->regs[insn->src1].native_reg != MIPS_noreg) {
            MAP_REGISTER(src1, 1, 0);
            MAP_REGISTER(src2, 1, insn->cond);
            MAP_REGISTER(cond, 1, insn->src2);
        } else if (block->regs[insn->src2].native_reg != MIPS_noreg) {
            MAP_REGISTER(src1, 1, insn->cond);
            MAP_REGISTER(src2, 1, 0);
            MAP_REGISTER(cond, 1, insn->src1);
        } else if (block->regs[insn->cond].native_reg != MIPS_noreg) {
            MAP_REGISTER(src1, 1, insn->src2);
            MAP_REGISTER(src2, 1, insn->src1);
            MAP_REGISTER(cond, 1, 0);
        } else {
            DMSG("%p/%u: WARNING: spilling $v1 to reload all 3 SELECT"
                 " operands", block, insn_index);
            MAP_REGISTER(src1, 1, insn->src2);
            MAP_REGISTER(src2, 1, insn->src1);
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, -8));
            APPEND(MIPS_SW(MIPS_v1, 0, MIPS_sp));
            APPEND(MIPS_LW(MIPS_v1,
                           8 + block->regs[insn->cond].stack_offset, MIPS_sp));
            cond = MIPS_v1;
            spilled_v1 = 1;
        }
        MAP_REGISTER(dest, 0, 0);
        if (dest == src1) {
            APPEND(MIPS_MOVZ(dest, src2, cond));
        } else if (dest == src2) {
            APPEND(MIPS_MOVN(dest, src1, cond));
        } else if (dest == cond) {
            DMSG("%p/%u: WARNING: dest==cond ($%u) for SELECT, using slow"
                 " register swap", block, insn_index, dest);
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, -8));
            APPEND(MIPS_SW(cond, 0, MIPS_sp));
            APPEND(MIPS_SW(src1, 4, MIPS_sp));
            APPEND(MIPS_MOVE(dest, src1));
            APPEND(MIPS_LW(src1, 0, MIPS_sp));
            APPEND(MIPS_MOVZ(dest, src2, src1));
            APPEND(MIPS_LW(src1, 4, MIPS_sp));
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
        } else {
            APPEND(MIPS_MOVE(dest, src1));
            APPEND(MIPS_MOVZ(dest, src2, cond));
        }
        if (spilled_v1) {
            APPEND(MIPS_LW(MIPS_v1, 0, MIPS_sp));
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
        }
        MAYBE_SAVE(dest);
        return 1;
      }

      case RTLOP_ADD:
        return append_alu_reg(block, insn, MIPS_ADDU(0, 0, 0));

      case RTLOP_SUB:
        return append_alu_reg(block, insn, MIPS_SUBU(0, 0, 0));

      case RTLOP_MULU:
        return append_mult_div(block, insn, MIPS_MULTU(0, 0), 0, insn_index);

      case RTLOP_MULS:
        return append_mult_div(block, insn, MIPS_MULT(0, 0), 0, insn_index);

      case RTLOP_MADDU:
        return append_mult_div(block, insn, MIPS_MADDU(0, 0), 1, insn_index);

      case RTLOP_MADDS:
        return append_mult_div(block, insn, MIPS_MADD(0, 0), 1, insn_index);

      case RTLOP_DIVMODU:
        return append_mult_div(block, insn, MIPS_DIVU(0, 0), 0, insn_index);

      case RTLOP_DIVMODS:
        return append_mult_div(block, insn, MIPS_DIV(0, 0), 0, insn_index);

      case RTLOP_AND:
        return append_alu_reg(block, insn, MIPS_AND(0, 0, 0));

      case RTLOP_OR:
        return append_alu_reg(block, insn, MIPS_OR(0, 0, 0));

      case RTLOP_XOR:
        return append_alu_reg(block, insn, MIPS_XOR(0, 0, 0));

      case RTLOP_NOT:
        return append_alu_1op(block, insn, MIPS_NOR(0, 0, MIPS_zero));

      case RTLOP_SLL:
        return append_shift_reg(block, insn, MIPS_SLLV(0, 0, 0));

      case RTLOP_SRL:
        return append_shift_reg(block, insn, MIPS_SRLV(0, 0, 0));

      case RTLOP_SRA:
        return append_shift_reg(block, insn, MIPS_SRAV(0, 0, 0));

      case RTLOP_ROR:
        return append_shift_reg(block, insn, MIPS_RORV(0, 0, 0));

      case RTLOP_CLZ:
        return append_alu_1op(block, insn, MIPS_CLZ(0, 0));

      case RTLOP_CLO:
        return append_alu_1op(block, insn, MIPS_CLZ(0, 0));

      case RTLOP_SLTU:
        return append_alu_reg(block, insn, MIPS_SLTU(0, 0, 0));

      case RTLOP_SLTS:
#ifdef MIPS_OPTIMIZE_MIN_MAX
        {
            const int num_insns = optimize_min_max(block, insn_index);
            if (num_insns) {
                return num_insns;
            }
        }
#endif
        return append_alu_reg(block, insn, MIPS_SLT(0, 0, 0));

      case RTLOP_BSWAPH:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_WSBH(dest, src1));
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_BSWAPW:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_WSBW(dest, src1));
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_HSWAPW:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_ROR(dest, src1, 16));
        MAYBE_SAVE(dest);
        return 1;

      /*----------------------------*/

      case RTLOP_ADDI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm + 0x8000 < 0x10000) {
            APPEND(MIPS_ADDIU(dest, src1, insn->src_imm & 0xFFFF));
        } else {
            if (insn->src_imm < 0x10000) {
                APPEND(MIPS_ORI(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_ADDU(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_ANDI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 0x10000) {
            APPEND(MIPS_ANDI(dest, src1, insn->src_imm & 0xFFFF));
        } else if (insn->src_imm == 0xFFFFFFFF) {
            if (dest != src1) {
                APPEND(MIPS_MOVE(dest, src1));
            }
        } else if (((insn->src_imm & 1)
                    && !(insn->src_imm & (insn->src_imm+1)))
                || (!(insn->src_imm & 1)
                    && !(~insn->src_imm & (~insn->src_imm+1)))
        ) {
            unsigned int start, count;
            if (insn->src_imm & 1) {
                count = __builtin_clz(insn->src_imm);
                start = 32 - count;
            } else {
                count = 32 - __builtin_allegrex_clo(insn->src_imm);
                start = 0;
            }
            if (dest != src1) {
                APPEND(MIPS_MOVE(dest, src1));
            }
            APPEND(MIPS_INS(dest, MIPS_zero, start, count));
        } else {
            if (insn->src_imm >= 0xFFFF8000) {
                APPEND(MIPS_ADDIU(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_AND(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_ORI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 0x10000) {
            APPEND(MIPS_ORI(dest, src1, insn->src_imm & 0xFFFF));
        } else {
            if (insn->src_imm >= 0xFFFF8000) {
                APPEND(MIPS_ADDIU(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_OR(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_XORI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 0x10000) {
            APPEND(MIPS_XORI(dest, src1, insn->src_imm & 0xFFFF));
        } else {
            if (insn->src_imm >= 0xFFFF8000) {
                APPEND(MIPS_ADDIU(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_XOR(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SLLI:
        MAP_REGISTER(src1, 1, 0);
#ifdef MIPS_OPTIMIZE_SEX
        if ((insn->src_imm == 16 || insn->src_imm == 24)
         && block->regs[insn->dest].death == insn_index+1
         && insn[1].opcode == RTLOP_SRAI
         && insn[1].src1 == insn->dest
         && insn[1].src_imm == insn->src_imm
        ) {
            insn++;
            MAP_REGISTER(dest, 0, 0);
            if (insn->src_imm == 16) {
                APPEND(MIPS_SEH(dest, src1));
            } else {
                APPEND(MIPS_SEB(dest, src1));
            }
            MAYBE_SAVE(dest);
            return 2;
        }
#endif
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 32) {
            APPEND(MIPS_SLL(dest, src1, insn->src_imm));
        } else {
            APPEND(MIPS_MOVE(dest, MIPS_zero));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SRLI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 32) {
            APPEND(MIPS_SRL(dest, src1, insn->src_imm));
        } else {
            APPEND(MIPS_MOVE(dest, MIPS_zero));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SRAI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm < 32) {
            APPEND(MIPS_SRA(dest, src1, insn->src_imm));
        } else {
            APPEND(MIPS_SRA(dest, src1, 31));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_RORI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_ROR(dest, src1, insn->src_imm & 31));
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SLTUI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm + 0x8000 < 0x10000) {
            APPEND(MIPS_SLTIU(dest, src1, insn->src_imm & 0xFFFF));
        } else {
            if (insn->src_imm < 0x10000) {
                APPEND(MIPS_ORI(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_SLTU(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_SLTSI:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        if (insn->src_imm + 0x8000 < 0x10000) {
            APPEND(MIPS_SLTI(dest, src1, insn->src_imm & 0xFFFF));
        } else {
            if (insn->src_imm < 0x10000) {
                APPEND(MIPS_ORI(MIPS_v1, MIPS_zero, insn->src_imm & 0xFFFF));
            } else {
                APPEND(MIPS_LUI(MIPS_v1, insn->src_imm>>16 & 0xFFFF));
                APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, insn->src_imm & 0xFFFF));
            } 
            APPEND(MIPS_SLT(dest, src1, MIPS_v1));
        }
        MAYBE_SAVE(dest);
        return 1;

      /*----------------------------*/

      case RTLOP_BFEXT:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_EXT(dest, src1,
                        insn->bitfield.start, insn->bitfield.count));
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_BFINS:
        MAP_REGISTER(src1, 1, insn->src2);
        MAP_REGISTER(dest, 0, 0);
        if (dest != src1) {
            APPEND(MIPS_MOVE(dest, src1));
        }
        MAP_REGISTER(src2, 1, insn->dest);
        APPEND(MIPS_INS(dest, src2,
                        insn->bitfield.start, insn->bitfield.count));
        MAYBE_SAVE(dest);
        return 1;

      /*----------------------------*/

      case RTLOP_LOAD_IMM: {
        const uint32_t value = insn->src_imm;
        MAP_REGISTER(dest, 0, 0);
        if (value + 0x8000 < 0x10000) {
            APPEND(MIPS_ADDIU(dest, MIPS_zero, value));
        } else if (value < 0x10000) {
            APPEND(MIPS_ORI(dest, MIPS_zero, value));
        } else {
            APPEND(MIPS_LUI(dest, value>>16));
            if (value & 0xFFFF) {
                APPEND(MIPS_ORI(dest, dest, value & 0xFFFF));
            }
        }
        MAYBE_SAVE(dest);
        return 1;
      }

      case RTLOP_LOAD_ADDR: {
        const uint32_t value = insn->src_addr;
#ifdef MIPS_OPTIMIZE_ABSOLUTE_CALL
        if (block->regs[insn->dest].source == RTLREG_CONSTANT
         && block->regs[insn->dest].birth == insn_index
         && block->regs[insn->dest].death == insn_index+1
         && insn[1].opcode == RTLOP_CALL
         && insn[1].target == insn->dest
        ) {
            /* Just skip the instruction; CALL will detect the constant and
             * handle it properly */
            return 1;
        }
#endif
        MAP_REGISTER(dest, 0, 0);
        if (value + 0x8000 < 0x10000) {
            APPEND(MIPS_ADDIU(dest, MIPS_zero, value));
        } else if (value < 0x10000) {
            APPEND(MIPS_ORI(dest, MIPS_zero, value));
        } else {
            APPEND(MIPS_LUI(dest, value>>16));
            if (value & 0xFFFF) {
                APPEND(MIPS_ORI(dest, dest, value & 0xFFFF));
            }
        }
        MAYBE_SAVE(dest);
        return 1;
      }

      case RTLOP_LOAD_PARAM: {
        const uint32_t param_index = insn->src_imm;
        if (UNLIKELY(param_index > 8)) {
            DMSG("Only 8 function parameters supported, index %u is invalid",
                 param_index);
            return 0;
        }
        const unsigned int param_reg = MIPS_a0 + param_index;
        MAP_REGISTER(dest, 0, 0);
        if (dest != param_reg) {
            APPEND(MIPS_MOVE(dest, param_reg));
        }
        MAYBE_SAVE(dest);
        return 1;
      }

      case RTLOP_LOAD_BU:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND_FLOAT(MIPS_LBU(dest, insn->offset, src1), 3);
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_LOAD_BS:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND_FLOAT(MIPS_LB(dest, insn->offset, src1), 3);
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_LOAD_HU:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND_FLOAT(MIPS_LHU(dest, insn->offset, src1), 3);
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_LOAD_HS:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND_FLOAT(MIPS_LH(dest, insn->offset, src1), 3);
        MAYBE_SAVE(dest);
        return 1;

      case RTLOP_LOAD_W:
      case RTLOP_LOAD_PTR:
        MAP_REGISTER(src1, 1, 0);
        MAP_REGISTER(dest, 0, 0);
        APPEND_FLOAT(MIPS_LW(dest, insn->offset, src1), 3);
        MAYBE_SAVE(dest);
        return 1;

      /*----------------------------*/

      case RTLOP_STORE_B:
        /* "dest" is actually a source operand here */
        MAP_REGISTER(src1, 1, insn->dest);
        MAP_REGISTER(dest, 1, insn->src1);
        APPEND(MIPS_SB(src1, insn->offset, dest));
        return 1;

      case RTLOP_STORE_H:
        MAP_REGISTER(src1, 1, insn->dest);
        MAP_REGISTER(dest, 1, insn->src1);
        APPEND(MIPS_SH(src1, insn->offset, dest));
        return 1;

      case RTLOP_STORE_W:
      case RTLOP_STORE_PTR:
        MAP_REGISTER(src1, 1, insn->dest);
        MAP_REGISTER(dest, 1, insn->src1);
        APPEND(MIPS_SW(src1, insn->offset, dest));
        return 1;

      /*----------------------------*/

      case RTLOP_LABEL:
        block->label_offsets[insn->label] = block->native_length;
        return 1;

      case RTLOP_GOTO:
        /* A branch to the following unit is in effect a no-op, so just
         * skip it entirely */
        if (block->label_unitmap[insn->label] == block->units[unit_index].next_unit) {
            return 1;
        }
        /* Flush any cached values out of HI or LO */
        if (UNLIKELY(!flush_hilo(block, insn_index))) {
            return 0;
        }
        /* Fill in a fake opcode for now; this will be replaced later with
         * a real branch instruction once the offset is known */
        APPEND_BRANCH(MIPS_B_LABEL(insn->label));
        return 1;

      case RTLOP_GOTO_IF_Z:
      case RTLOP_GOTO_IF_NZ: {
        if (block->label_unitmap[insn->label] == block->units[unit_index].next_unit) {
            return 1;
        }
        MAP_REGISTER(src1, 1, 0);
        /* Make sure it doesn't collide with anything we're about to
         * reload, and move to $at if so */
        unsigned int target_unit = block->label_unitmap[insn->label];
        if (block->units[target_unit].first_insn < insn_index) {
            RTLRegister * const target_reg =
                block->units[target_unit].mips.reg_map[src1];
            if (target_reg && target_reg != &block->regs[insn->src1]
             && target_reg->death >= insn_index
            ) {
                APPEND(MIPS_MOVE(MIPS_at, src1));
                src1 = MIPS_at;
            }
        }
        if (UNLIKELY(!flush_hilo(block, insn_index))) {
            return 0;
        }
        if (insn->opcode == RTLOP_GOTO_IF_Z) {
            APPEND_BRANCH(MIPS_BEQZ_LABEL(src1, insn->label));
        } else {
            APPEND_BRANCH(MIPS_BNEZ_LABEL(src1, insn->label));
        }
        return 1;
      }  // case RTLOP_GOTO_IF_{Z,NZ}

      case RTLOP_GOTO_IF_E:
      case RTLOP_GOTO_IF_NE: {
        if (block->label_unitmap[insn->label] == block->units[unit_index].next_unit) {
            return 1;
        }
        MAP_REGISTER(src1, 1, insn->src2);
        MAP_REGISTER(src2, 1, insn->src1);
        unsigned int target_unit = block->label_unitmap[insn->label];
        if (block->units[target_unit].first_insn < insn_index) {
            RTLRegister * const target_reg1 =
                block->units[target_unit].mips.reg_map[src1];
            if (target_reg1 && target_reg1 != &block->regs[insn->src1]
             && target_reg1->death >= insn_index
            ) {
                if (src2 == MIPS_at) {
                    APPEND(MIPS_MOVE(MIPS_v1, src1));
                    src1 = MIPS_v1;
                } else {
                    APPEND(MIPS_MOVE(MIPS_at, src1));
                    src1 = MIPS_at;
                }
            }
            RTLRegister * const target_reg2 =
                block->units[target_unit].mips.reg_map[src2];
            if (target_reg2 && target_reg2 != &block->regs[insn->src2]
             && target_reg2->death >= insn_index
            ) {
                if (src1 == MIPS_at) {
                    APPEND(MIPS_MOVE(MIPS_v1, src2));
                    src2 = MIPS_v1;
                } else {
                    APPEND(MIPS_MOVE(MIPS_at, src2));
                    src2 = MIPS_at;
                }
            }
        }
        if (UNLIKELY(!flush_hilo(block, insn_index))) {
            return 0;
        }
        if (insn->opcode == RTLOP_GOTO_IF_E) {
            APPEND_BRANCH(MIPS_BEQ_LABEL(src1, src2, insn->label));
        } else {
            APPEND_BRANCH(MIPS_BNE_LABEL(src1, src2, insn->label));
        }
        return 1;
      }  // case RTLOP_GOTO_IF_{E,NE}

      /*----------------------------*/

      case RTLOP_CALL: {

        /* Map the argument registers, if any */
        src1 = src2 = 0;  // Avoid a compiler warning (not actually needed)
        if (insn->src1) {
            MAP_REGISTER(src1, 1, insn->src2);
            if (insn->src2) {
                MAP_REGISTER(src2, 1, insn->src1);
            }
        }

        /* Flush any cached values out of HI or LO */
        if (UNLIKELY(!flush_hilo(block, insn_index))) {
            return 0;
        }

        /* Flush all live caller-saved registers to the stack */
        if (UNLIKELY(!flush_for_call(block, insn_index))) {
            return 0;
        }

        /* Map the function address, if necessary */
        int target;
#ifdef MIPS_OPTIMIZE_ABSOLUTE_CALL
        if (block->regs[insn->target].source == RTLREG_CONSTANT) {
            target = -1;
        } else {
#endif
            /* Need to avoid collision with both src1 and src2; the
             * MAP_REGISTER macro can't handle avoiding two registers at
             * once, so we do it manually here, taking advantage of the
             * registers reed up by flush_for_call().  This won't actually
             * occur in practice as long as MIPS_OPTIMIZE_ABSOLUTE_CALL is
             * enabled, since all the calls we make are to known addresses. */
            RTLRegister * const target_reg = &block->regs[insn->target];
            if (target_reg->native_reg == MIPS_noreg
             && (src1 == MIPS_v1 || src2 == MIPS_v1)
            ) {
                PRECOND(target_reg->frame_allocated, return 0);
                if (src1 == MIPS_v1) { // Impossible, but just for completeness
                    if (src2 == MIPS_a0) {
                        APPEND(MIPS_MOVE(MIPS_a2, src1));
                        src1 = MIPS_a2;
                    } else {
                        APPEND(MIPS_MOVE(MIPS_a2, src1));
                        src1 = MIPS_a2;
                    }
                } else if (src2 == MIPS_v1) {
                    if (src1 == MIPS_a1) {
                        APPEND(MIPS_MOVE(MIPS_a2, src2));
                        src2 = MIPS_a2;
                    } else {
                        APPEND(MIPS_MOVE(MIPS_a1, src2));
                        src2 = MIPS_a1;
                    }
                }
                APPEND(MIPS_LW(MIPS_v1, target_reg->stack_offset, MIPS_sp));
                target = MIPS_v1;
            } else {  // No collision
                MAP_REGISTER(target, 1, 0);
            }
#ifdef MIPS_OPTIMIZE_ABSOLUTE_CALL
        }
#endif

        /* Move any parameters to $a0-$a1 */
        if (insn->src1) {
            if (insn->src2) {
                if (src1 == MIPS_a1) {
                    if (src2 == MIPS_a0) {
                        /* Arguments are reversed, so use $at as a
                         * temporary to exchange them */
                        APPEND(MIPS_MOVE(MIPS_at, MIPS_a1));
                        src1 = MIPS_at;
                    } else {
                        APPEND(MIPS_MOVE(MIPS_a0, MIPS_a1));
                        src1 = MIPS_a0;
                    }
                }
                if (src2 != MIPS_a1) {
                    APPEND(MIPS_MOVE(MIPS_a1, src2));
                }
            }
            if (src1 != MIPS_a0) {
                APPEND(MIPS_MOVE(MIPS_a0, src1));
            }
        }

        /* Actually call the routine */
#ifdef MIPS_OPTIMIZE_ABSOLUTE_CALL
        if (block->regs[insn->target].source == RTLREG_CONSTANT) {
            const uint32_t address = block->regs[insn->target].value;
            APPEND_BRANCH(MIPS_JAL((address & 0x0FFFFFFC) >> 2));
        } else {
#endif
            APPEND_BRANCH(MIPS_JALR(MIPS_t9));
#ifdef MIPS_OPTIMIZE_ABSOLUTE_CALL
        }
#endif

        /* Copy the return value to the target register, if there is one */
        if (insn->dest) {
            MAP_REGISTER(dest, 0, 0);
            if (dest != MIPS_v0) {
                APPEND(MIPS_MOVE(dest, MIPS_v0));
            }
            MAYBE_SAVE(dest);
        }

        /* Reload all flushed registers */
        if (UNLIKELY(!reload_after_call(block, insn_index))) {
            return 0;
        }

        return 1;
      }  // case RTLOP_CALL

      case RTLOP_RETURN:
        if (insn_index == block->units[unit_index].last_insn
         && block->units[unit_index].next_unit == -1
        ) {
            /* This is the last instruction, so we can just fall through */
        } else {
            APPEND_BRANCH(MIPS_B_EPILOGUE_RET_CONST);
        }
        return 1;

      case RTLOP_RETURN_TO: {
        int target;
        MAP_REGISTER(target, 1, 0);
        if (target != MIPS_at) {
            APPEND(MIPS_MOVE(MIPS_at, target));
        }
        /* Don't forget to reload parameters.  (Since we only use this for
         * predicted static branches to other native code, we know that we
         * can just pass the first parameter, i.e. the state block pointer,
         * along; for a more general implementation, we'd treat this like
         * CALL and take parameters.) */
        if (UNLIKELY(block->regs[1].source != RTLREG_PARAMETER)
         || UNLIKELY(block->regs[1].param_index != 0)
        ) {
            DMSG("r1 is not state block, can't handle");
            return 0;
        }
        if (block->regs[1].native_reg != MIPS_a0) {
            APPEND(MIPS_MOVE(MIPS_a0, block->regs[1].native_reg));
        }
        APPEND_BRANCH(MIPS_B_EPILOGUE_CHAIN_AT);
        block->mips.need_chain_at = 1;
        return 1;
      }  // case RTLOP_RETURN_TO

    }  // switch (insn->opcode)

    /*------------------------------*/

    DMSG("%p/%u: Invalid RTL opcode %u", block, insn_index, insn->opcode);
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * append_alu_1op:  Append a 1-register-operand ALU instruction to the
 * instruction stream.
 *
 * [Parameters]
 *      block: RTL block being translated
 *       insn: RTL instruction being translated
 *     opcode: Instruction word to add (with rs/rd set to zero)
 * [Return value]
 *     1 on success, 0 on error
 */
static int append_alu_1op(RTLBlock * const block, const RTLInsn * const insn,
                          const uint32_t opcode)
{
    PRECOND(block != NULL, return 0);

    int src1, dest;
    MAP_REGISTER(src1, 1, 0);
    MAP_REGISTER(dest, 0, 0);
    APPEND(opcode | src1<<21 | dest<<11);
    MAYBE_SAVE(dest);
    return 1;
}

/*----------------------------------*/

/**
 * append_alu_reg:  Append a 2-register-operand ALU instruction (other than
 * a shift insturction) to the instruction stream.
 *
 * [Parameters]
 *      block: RTL block being translated
 *       insn: RTL instruction being translated
 *     opcode: Instruction word to add (with rs/rt/rd set to zero)
 * [Return value]
 *     1 on success, 0 on error
 */
static int append_alu_reg(RTLBlock * const block, const RTLInsn * const insn,
                          const uint32_t opcode)
{
    PRECOND(block != NULL, return 0);

    int src1, src2, dest;
    MAP_REGISTER(src1, 1, insn->src2);
    MAP_REGISTER(src2, 1, insn->src1);
    MAP_REGISTER(dest, 0, 0);
    APPEND(opcode | src1<<21 | src2<<16 | dest<<11);
    MAYBE_SAVE(dest);
    return 1;
}

/*----------------------------------*/

/**
 * append_shift_reg:  Append a variable-count shift instruction to the
 * instruction stream.
 *
 * [Parameters]
 *      block: RTL block being translated
 *       insn: RTL instruction being translated
 *     opcode: Instruction word to add (with rs/rt/rd set to zero)
 * [Return value]
 *     1 on success, 0 on error
 */
static int append_shift_reg(RTLBlock * const block, const RTLInsn * const insn,
                            const uint32_t opcode)
{
    PRECOND(block != NULL, return 0);

    int src1, src2, dest;
    MAP_REGISTER(src1, 1, insn->src2);
    MAP_REGISTER(src2, 1, insn->src1);
    MAP_REGISTER(dest, 0, 0);
    /* Register order is reversed from regular ALU insns: SLLV rd,rt,rs
     * (as opposed to ADDU rd,rs,rt) */
    APPEND(opcode | src2<<21 | src1<<16 | dest<<11);
    MAYBE_SAVE(dest);
    return 1;
}

/*----------------------------------*/

/**
 * append_mult_div:  Append a multiply or divide instruction to the
 * instruction stream.
 *
 * [Parameters]
 *          block: RTL block being translated
 *           insn: RTL instruction being translated
 *         opcode: Instruction word to add (with rs/rt set to zero)
 *     accumulate: Nonzero if this is an accumulating instruction (madd/maddu)
 *     insn_index: Index of instruction being translated
 * [Return value]
 *     1 on success, 0 on error
 */
static int append_mult_div(RTLBlock * const block, const RTLInsn * const insn,
                           const uint32_t opcode, const int accumulate,
                           const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);

    if (accumulate) {
        /* If the proper registers are already in HI and LO, we don't need
         * to do anything here */
        if (block->mips.hi_reg != insn->dest2
         || block->mips.lo_reg != insn->dest
        ) {
            /* Need to reload HI and/or LO; don't bother with the case
             * where one of the two is correctly loaded, since normally
             * both will be preloaded properly */
            flush_hilo(block, insn_index);
            int dest2;
            MAP_REGISTER(dest2, 1, 0);
            APPEND(MIPS_MTHI(dest2));
            int dest;
            MAP_REGISTER(dest, 1, 0);
            APPEND(MIPS_MTLO(dest));
        }
    } else {
        /* Make sure anything currently in HI/LO is flushed out before we
         * overwrite them */
        flush_hilo(block, insn_index);
    }

    int src1, src2;
    MAP_REGISTER(src1, 1, insn->src2);
    MAP_REGISTER(src2, 1, insn->src1);
    const int float_distance = ((opcode & 0x3E) == __SP_DIV) ? 35 : 6;
    APPEND_FLOAT(opcode | src1<<21 | src2<<16, float_distance);
    if (insn->dest) {
        if (block->regs[insn->dest].frame_allocated) {
            /* We have to save it to the stack, so it can't stay in LO */
            int dest;
            MAP_REGISTER(dest, 0, 0);
            APPEND(MIPS_MFLO(dest));
            MAYBE_SAVE(dest);
        } else {
            /* Leave it in LO for now; we'll extract it when we need it */
            block->regs[insn->dest].mips.is_in_hilo = MIPS_MFLO(0);
            block->mips.lo_reg = insn->dest;
        }
    }
    if (insn->dest2) {
        if (block->regs[insn->dest2].frame_allocated) {
            int dest2;
            MAP_REGISTER(dest2, 0, 0);
            APPEND(MIPS_MFHI(dest2));
            MAYBE_SAVE(dest2);
        } else {
            block->regs[insn->dest2].mips.is_in_hilo = MIPS_MFHI(0);
            block->mips.hi_reg = insn->dest2;
        }
    }
    return 1;
}

/*************************************************************************/

/**
 * flush_for_call:  Flush all caller-saved registers to the stack frame in
 * preparation for a subroutine call.  Registers which are born on or die
 * with the current instruction are not saved; the destination register is
 * also not saved (normally the destination register will be born with this
 * instruction, but if the code is not SSA-form, it may already be live).
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of instruction being translated
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int flush_for_call(RTLBlock * const block, const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    const uint32_t dest = block->insns[insn_index].dest;

    unsigned int push_offset = MIPS_FRAME_SIZE;
    unsigned int i;
    for (i = 0; i < lenof(caller_saved_regs); i++) {
        const unsigned int mips_reg = caller_saved_regs[i];
        RTLRegister * const reg = block->mips.reg_map[mips_reg];
        if (reg
         && reg->birth < insn_index
         && reg->death > insn_index
         && !(dest && reg == &block->regs[dest])
         && reg->source != RTLREG_CONSTANT
        ) {
            APPEND(MIPS_SW(mips_reg, push_offset, MIPS_sp));
            push_offset += 4;
        }
    }

    return 1;
}

/*----------------------------------*/

/**
 * reload_after_call:  Reload necessary caller-saved registers after a
 * subroutine call.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of instruction being translated
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int reload_after_call(RTLBlock * const block,
                             const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    const uint32_t dest = block->insns[insn_index].dest;

    unsigned int pop_offset = MIPS_FRAME_SIZE;
    unsigned int i;
    for (i = 0; i < lenof(caller_saved_regs); i++) {
        const unsigned int mips_reg = caller_saved_regs[i];
        RTLRegister * const reg = block->mips.reg_map[mips_reg];
        if (reg
         && reg->birth < insn_index
         && reg->death > insn_index
         && !(dest && reg == &block->regs[dest])
        ) {
            if (reg->source == RTLREG_CONSTANT) {
                const uint32_t value = reg->value;
                if (value + 0x8000 < 0x10000) {
                    APPEND(MIPS_ADDIU(mips_reg, MIPS_zero, value));
                } else if (value < 0x10000) {
                    APPEND(MIPS_ORI(mips_reg, MIPS_zero, value));
                } else {
                    APPEND(MIPS_LUI(mips_reg, value>>16));
                    if (value & 0xFFFF) {
                        APPEND(MIPS_ORI(mips_reg, mips_reg, value & 0xFFFF));
                    }
                }
            } else {
                APPEND(MIPS_LW(mips_reg, pop_offset, MIPS_sp));
                pop_offset += 4;
            }
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * flush_hilo:  Flush any values cached in the MIPS HI or LO registers to
 * the appropriate general-purpose register.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of RTL instruction being translated (if between
 *                    instructions, next instruction to be translated)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int flush_hilo(RTLBlock * const block, const uint32_t insn_index)
{
    if (block->mips.hi_reg) {
        RTLRegister * const reg = &block->regs[block->mips.hi_reg];
        /* If the register was already updated, its is_in_hilo field will
         * be zero, so we don't need to update the register ourselves in
         * that case; we just clear the register reference from hi_reg.
         * Also make sure that we don't try to update a register that's
         * already dead. */
        if (reg->mips.is_in_hilo && reg->death >= insn_index) {
            PRECOND(reg->mips.is_in_hilo == MIPS_MFHI(0), return 0);
            PRECOND(reg->native_allocated, return 0);
            PRECOND(reg->native_reg != MIPS_noreg, return 0);
            PRECOND(reg->native_reg != MIPS_zero, return 0);
            APPEND(reg->mips.is_in_hilo | reg->native_reg<<11);
            reg->mips.is_in_hilo = 0;
            block->mips.reg_map[reg->native_reg] = reg;
        }
        block->mips.hi_reg = 0;
    }
    if (block->mips.lo_reg) {
        RTLRegister * const reg = &block->regs[block->mips.lo_reg];
        if (reg->mips.is_in_hilo && reg->death >= insn_index) {
            PRECOND(reg->mips.is_in_hilo == MIPS_MFLO(0), return 0);
            PRECOND(reg->native_allocated, return 0);
            PRECOND(reg->native_reg != MIPS_noreg, return 0);
            PRECOND(reg->native_reg != MIPS_zero, return 0);
            APPEND(reg->mips.is_in_hilo | reg->native_reg<<11);
            reg->mips.is_in_hilo = 0;
            block->mips.reg_map[reg->native_reg] = reg;
        }
        block->mips.lo_reg = 0;
    }
    return 1;
}

/*************************************************************************/

#ifdef MIPS_OPTIMIZE_MIN_MAX

/**
 * optimize_min_max:  Attempt to optimize an SLTS instruction followed by a
 * SELECT instruction into a MIPS MIN or MAX instruction.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of RTL instruction to optimize (must be SLTS)
 * [Return value]
 *     Number of RTL instructions processed (nonzero) if the given RTL
 *     instruction was successfully optimized into a MIPS MIN or MAX
 *     instruction, else zero
 */
static int optimize_min_max(RTLBlock * const block, const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);
    PRECOND(block->insns[insn_index].opcode == RTLOP_SLTS, return 0);

    const RTLInsn *insn = &block->insns[insn_index];

    if (block->regs[insn->dest].death != insn_index+1
     || insn[1].opcode != RTLOP_SELECT
     || insn[1].cond != insn->dest
    ) {
        return 0;
    } else if (insn[1].src1 == insn->src1 && insn[1].src2 == insn->src2) {
        insn++;
        int src1, src2, dest;
        MAP_REGISTER(src1, 1, insn->src2);
        MAP_REGISTER(src2, 1, insn->src1);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_MIN(dest, src1, src2));
        return 2;
    } else if (insn[1].src1 == insn->src2 && insn[1].src2 == insn->src1) {
        insn++;
        int src1, src2, dest;
        MAP_REGISTER(src1, 1, insn->src2);
        MAP_REGISTER(src2, 1, insn->src1);
        MAP_REGISTER(dest, 0, 0);
        APPEND(MIPS_MAX(dest, src1, src2));
        return 2;
    } else {
        return 0;
    }
}

#endif  // MIPS_OPTIMIZE_MIN_MAX

/*************************************************************************/

#ifdef RTL_TRACE_STEALTH_FOR_SH2

/*----------------------------------*/

/**
 * sh2_stealth_trace_insn:  Add MIPS code to trace an instruction for SH-2
 * TRACE_STEALTH mode.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of RTL instruction to translate
 * [Return value]
 *     Number of RTL instructions processed (nonzero) on success, zero on error
 */
static unsigned int sh2_stealth_trace_insn(RTLBlock * const block,
                                           const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    if (block->regs[1].source != RTLREG_PARAMETER
     || block->regs[1].param_index != 0
    ) {
        DMSG("r1 is not state block, can't handle");
        return 0;
    }

    const RTLInsn *insn = &block->insns[insn_index];
    unsigned int num_insns = 1;  // Number of RTL instructions consumed
    unsigned int cond_reg = 0;
    unsigned int test_bit = 0;
    uint32_t op = insn[num_insns++].src_imm;
    if (op>>24 == 0x9F) {  // Conditional execution
        cond_reg = op & 0xFFFF;
        test_bit = op>>16 & 1;
        op = insn[num_insns++].src_imm;
    }
    uint32_t cached_cycles = op;
    PRECOND(cached_cycles>>24 == 0x98, return 1);
    cached_cycles &= 0xFFFF;
    uint32_t cached_cycle_reg = insn[num_insns++].src_imm;
    PRECOND(cached_cycles>>24 == 0x90, return 1);
    cached_cycle_reg &= 0xFFFF;

    /* First save all registers; load the state block pointer into $at in
     * the delay slot */
    APPEND(MIPS_JAL(((uintptr_t)code_save_regs_state & 0x0FFFFFFC) >> 2));
    if (IS_SPILLED(1)) {
        APPEND(MIPS_LW(MIPS_at, block->regs[1].stack_offset, MIPS_sp));
    } else {
        APPEND(MIPS_MOVE(MIPS_at, block->regs[1].native_reg));
    }

    /* Update the state block with cached register values */
    uint32_t mask = block->sh2_regcache_mask;
    if (mask) {
        const uintptr_t cache_ptr = (uintptr_t)sh2_regcache;
        APPEND(MIPS_LUI(MIPS_a0, cache_ptr >> 16));
        APPEND(MIPS_ORI(MIPS_a0, MIPS_a0, cache_ptr & 0xFFFF));
    }
    unsigned int sh2_reg;
    for (sh2_reg = 0; mask != 0; mask >>= 1, sh2_reg++) {
        if (mask & 1) {
            APPEND(MIPS_LW(MIPS_v1, 4*sh2_reg, MIPS_a0));
            APPEND(MIPS_SW(MIPS_v1, 4*sh2_reg, MIPS_at));
        }
    }

    /* Update the state block's cached cycle count */
    if (cached_cycle_reg) {
        RTLRegister * const reg = &block->regs[cached_cycle_reg];
        if (IS_SPILLED(cached_cycle_reg)) {
            APPEND(MIPS_LW(MIPS_v1, 4*(18+24) + reg->stack_offset, MIPS_sp));
        } else if (reg->native_reg >= MIPS_v0 && reg->native_reg <= MIPS_t7) {
            APPEND(MIPS_LW(MIPS_v1, 4*(reg->native_reg - MIPS_v0), MIPS_sp));
        } else if (reg->native_reg >= MIPS_t8 && reg->native_reg <= MIPS_t9) {
            APPEND(MIPS_LW(MIPS_v1,
                           4*(reg->native_reg - MIPS_t8 + 14), MIPS_sp));
        } else {
            APPEND(MIPS_MOVE(MIPS_v1, reg->native_reg));
        }
    }
    APPEND(MIPS_ADDI(MIPS_v1, MIPS_v1, cached_cycles));
    APPEND(MIPS_SW(MIPS_v1, offsetof(SH2State,cycles), MIPS_at));

    /* Call the trace routine (if the instruction isn't nulled out) */
    if (cond_reg) {
        if (IS_SPILLED(cond_reg)) {
            APPEND(MIPS_LW(MIPS_v1, 4*(18+24) + block->regs[cond_reg].stack_offset, MIPS_sp));
        } else if (block->regs[cond_reg].native_reg >= MIPS_v0
                && block->regs[cond_reg].native_reg <= MIPS_t7) {
            APPEND(MIPS_LW(MIPS_v1, 4*(block->regs[cond_reg].native_reg - MIPS_v0), MIPS_sp));
        } else if (block->regs[cond_reg].native_reg >= MIPS_t8
                && block->regs[cond_reg].native_reg <= MIPS_t9) {
            APPEND(MIPS_LW(MIPS_v1, 4*(block->regs[cond_reg].native_reg - MIPS_t8 + 14), MIPS_sp));
        } else {
            APPEND(MIPS_MOVE(MIPS_v1, block->regs[cond_reg].native_reg));
        }
        APPEND(MIPS_XORI(MIPS_v1, MIPS_v1, test_bit));
        APPEND(MIPS_BNEZ(MIPS_v1, 4));
    }
    APPEND(MIPS_LUI(MIPS_a1, (insn->src_imm & 0x7FFF0000) >> 16));
    APPEND(MIPS_ORI(MIPS_a1, MIPS_a1, insn->src_imm & 0xFFFF));
    APPEND(MIPS_JAL(((uintptr_t)trace_insn_callback & 0x0FFFFFFC) >> 2));
    APPEND(MIPS_MOVE(MIPS_a0, MIPS_at));

    /* Restore everything back the way it was */
    APPEND(MIPS_JAL(((uintptr_t)code_restore_regs_state & 0x0FFFFFFC) >> 2));
    if (IS_SPILLED(1)) {
        APPEND(MIPS_LW(MIPS_at,
                       4*(18+24) + block->regs[1].stack_offset, MIPS_sp));
    } else if (block->regs[1].native_reg >= MIPS_v0
            && block->regs[1].native_reg <= MIPS_t7) {
        APPEND(MIPS_LW(MIPS_at,
                       4*(block->regs[1].native_reg - MIPS_v0), MIPS_sp));
    } else if (block->regs[1].native_reg >= MIPS_t8
            && block->regs[1].native_reg <= MIPS_t9) {
        APPEND(MIPS_LW(MIPS_at,
                       4*(block->regs[1].native_reg - MIPS_t8 + 14), MIPS_sp));
    } else {
        APPEND(MIPS_MOVE(MIPS_at, block->regs[1].native_reg));
    }

    return num_insns;
}

/*----------------------------------*/

/**
 * sh2_stealth_cache_reg:  Add MIPS code to cache a register value for
 * SH-2 TRACE_STEALTH mode.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of RTL instruction to translate
 * [Return value]
 *     Number of RTL instructions processed (nonzero) on success, zero on error
 */
static unsigned int sh2_stealth_cache_reg(RTLBlock * const block,
                                          const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    const RTLInsn *insn = &block->insns[insn_index];
    unsigned int num_insns = 1;
    uint32_t op = insn->src_imm;
    uint32_t offset = 0;

    /* Get the offset if we have one */
    if (op & 0x08000000) {
        PRECOND(op>>24 == 0xB8, return 1);
        offset = (op & 0xFFFF) << 16;
        op = insn[num_insns++].src_imm;
        PRECOND(op>>24 == 0xBC, return 1);
        offset |= op & 0xFFFF;
        op = insn[num_insns++].src_imm;
        PRECOND(op>>24 == 0xB0, return 1);
    }

    unsigned int sh2_reg = (op >> 16) & 0xFF;
    unsigned int rtl_reg = op & 0xFFFF;

    if (rtl_reg) {
        /* Store a new value into the cache */

        const RTLRegister * const reg = &block->regs[rtl_reg];
        unsigned int mips_reg;
        if (reg->native_allocated
         && reg == block->mips.reg_map[reg->native_reg]
        ) {
            if (IS_SPILLED(rtl_reg)) {
                APPEND(MIPS_LW(MIPS_at, reg->stack_offset, MIPS_sp));
                mips_reg = MIPS_at;
            } else if (reg->mips.is_in_hilo) {
                APPEND(reg->mips.is_in_hilo | MIPS_at<<11);
                mips_reg = MIPS_at;
            } else if (reg->native_reg == MIPS_a0) {
                APPEND(MIPS_MOVE(MIPS_at, MIPS_a0));
                mips_reg = MIPS_at;
            } else {
                mips_reg = reg->native_reg;
            }
        } else {
            /* Either the register is already dead and got clobbered, or it
             * got eliminated perhaps because the corresponding SH-2
             * register was later overwritten. This is a pain if the
             * dead/eliminated register is a result register, but what can
             * we do... */
            switch (reg->source) {

              case RTLREG_CONSTANT:
                APPEND(MIPS_LUI(MIPS_at, reg->value >> 16));
                APPEND(MIPS_ORI(MIPS_at, MIPS_at, reg->value & 0xFFFF));
                break;

              case RTLREG_MEMORY: {
                const RTLRegister * const addr_reg =
                    &block->regs[reg->memory.addr_reg];
                unsigned int base_reg;
                if (addr_reg->native_allocated
                 && addr_reg==block->mips.reg_map[addr_reg->native_reg]
                ) {
                    base_reg = addr_reg->native_reg;
                } else if (addr_reg->source == RTLREG_CONSTANT) {
                    APPEND(MIPS_LUI(MIPS_at, addr_reg->value>>16 & 0xFFFF));
                    APPEND(MIPS_ORI(MIPS_at, MIPS_at, addr_reg->value&0xFFFF));
                    base_reg = MIPS_at;
                } else {
                    DMSG("%p/%u: Address register r%u not available for r%u",
                         block, insn_index, reg->memory.addr_reg, rtl_reg);
                    break;
                }
                if (reg->memory.size == 1) {  // 8 bits
                    if (reg->memory.is_signed) {
                        APPEND(MIPS_LB(MIPS_at, reg->memory.offset, base_reg));
                    } else {
                        APPEND(MIPS_LBU(MIPS_at, reg->memory.offset, base_reg));
                    }
                } else if (reg->memory.size == 2) {  // 16 bits
                    if (reg->memory.is_signed) {
                        APPEND(MIPS_LH(MIPS_at, reg->memory.offset, base_reg));
                    } else {
                        APPEND(MIPS_LHU(MIPS_at, reg->memory.offset, base_reg));
                    }
                } else {  // 32 bits
                    APPEND(MIPS_LW(MIPS_at, reg->memory.offset, base_reg));
                }
                break;
              }  // case RTLREG_MEMORY

              case RTLREG_RESULT:
              case RTLREG_RESULT_NOFOLD: {
                static const uint32_t mips_opcodes[] = {
                    [RTLOP_ADD    ] = MIPS_ADDU (MIPS_at, 0, 0),
                    [RTLOP_ADDI   ] = MIPS_ADDU (MIPS_at, 0, 0),
                    [RTLOP_SUB    ] = MIPS_SUBU (MIPS_at, 0, 0),
                    [RTLOP_MULU   ] = MIPS_MULTU(0, 0),
                    [RTLOP_MULS   ] = MIPS_MULT (0, 0),
                    [RTLOP_DIVMODU] = MIPS_DIVU (0, 0),
                    [RTLOP_DIVMODS] = MIPS_DIV  (0, 0),
                    [RTLOP_AND    ] = MIPS_AND  (MIPS_at, 0, 0),
                    [RTLOP_ANDI   ] = MIPS_AND  (MIPS_at, 0, 0),
                    [RTLOP_OR     ] = MIPS_OR   (MIPS_at, 0, 0),
                    [RTLOP_ORI    ] = MIPS_OR   (MIPS_at, 0, 0),
                    [RTLOP_XOR    ] = MIPS_XOR  (MIPS_at, 0, 0),
                    [RTLOP_XORI   ] = MIPS_XOR  (MIPS_at, 0, 0),
                    [RTLOP_NOT    ] = MIPS_NOR  (MIPS_at, 0, MIPS_zero),
                    [RTLOP_SLL    ] = MIPS_SLLV (MIPS_at, 0, 0),
                    [RTLOP_SLLI   ] = MIPS_SLLV (MIPS_at, 0, 0),
                    [RTLOP_SRL    ] = MIPS_SRLV (MIPS_at, 0, 0),
                    [RTLOP_SRLI   ] = MIPS_SRLV (MIPS_at, 0, 0),
                    [RTLOP_SRA    ] = MIPS_SRAV (MIPS_at, 0, 0),
                    [RTLOP_SRAI   ] = MIPS_SRAV (MIPS_at, 0, 0),
                    [RTLOP_ROR    ] = MIPS_RORV (MIPS_at, 0, 0),
                    [RTLOP_RORI   ] = MIPS_RORV (MIPS_at, 0, 0),
                    [RTLOP_CLZ    ] = MIPS_CLZ  (MIPS_at, 0),
                    [RTLOP_CLO    ] = MIPS_CLO  (MIPS_at, 0),
                    [RTLOP_SLTU   ] = MIPS_SLTU (MIPS_at, 0, 0),
                    [RTLOP_SLTUI  ] = MIPS_SLTU (MIPS_at, 0, 0),
                    [RTLOP_SLTS   ] = MIPS_SLT  (MIPS_at, 0, 0),
                    [RTLOP_SLTSI  ] = MIPS_SLT  (MIPS_at, 0, 0),
                    [RTLOP_BSWAPH ] = MIPS_WSBH (MIPS_at, 0),
                    [RTLOP_BSWAPW ] = MIPS_WSBW (MIPS_at, 0),
                    [RTLOP_HSWAPW ] = MIPS_ROR  (MIPS_at, 0, 16),
                };
                const RTLRegister *src1_reg, *src2_reg;
                src1_reg = &block->regs[reg->result.src1];
                src2_reg = &block->regs[reg->result.src2];
                switch (reg->result.opcode) {

                  case RTLOP_MOVE:
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                    } else {
                        APPEND(MIPS_MOVE(MIPS_at, src1_reg->native_reg));
                    }
                    break;

                  case RTLOP_NOT:
                  case RTLOP_CLZ:
                  case RTLOP_CLO: {
                    int rs;
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                        rs = MIPS_at;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                        rs = MIPS_at;
                    } else {
                        rs = src1_reg->native_reg;
                    }
                    APPEND(mips_opcodes[reg->result.opcode] | rs<<21);
                    break;
                  }

                  case RTLOP_BSWAPH:
                  case RTLOP_BSWAPW:
                  case RTLOP_HSWAPW: {
                    int rt;
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                        rt = MIPS_at;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                        rt = MIPS_at;
                    } else {
                        rt = src1_reg->native_reg;
                    }
                    APPEND(mips_opcodes[reg->result.opcode] | rt<<16);
                    break;
                  }

                  case RTLOP_ADD:
                  case RTLOP_SUB:
                  case RTLOP_AND:
                  case RTLOP_OR:
                  case RTLOP_XOR:
                  case RTLOP_SLL:
                  case RTLOP_SRL:
                  case RTLOP_SRA:
                  case RTLOP_ROR:
                  case RTLOP_SLTU:
                  case RTLOP_SLTS: {
                    unsigned int rs, rt;  // src1/src2 MIPS registers
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at, src1_reg->stack_offset, MIPS_sp));
                        rs = MIPS_at;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                        rs = MIPS_at;
                    } else {
                        rs = src1_reg->native_reg;
                    }
                    if (IS_SPILLED(reg->result.src2)) {
                        APPEND(MIPS_LW(MIPS_v1, src2_reg->stack_offset, MIPS_sp));
                        rt = MIPS_v1;
                    } else if (src2_reg->mips.is_in_hilo) {
                        APPEND(src2_reg->mips.is_in_hilo | MIPS_v1<<11);
                        rt = MIPS_v1;
                    } else {
                        rt = src2_reg->native_reg;
                    }
                    if ((mips_opcodes[reg->result.opcode] & 0xFC00003F) < 010){
                        /* For SLLV/SRLV/SRAV/RORV, rt is the source and
                         * rs is the shift amount */
                        APPEND(mips_opcodes[reg->result.opcode] | rt<<21 | rs<<16);
                    } else {
                        APPEND(mips_opcodes[reg->result.opcode] | rs<<21 | rt<<16);
                    }
                    break;
                  }

                  case RTLOP_ADDI:
                  case RTLOP_ANDI:
                  case RTLOP_ORI:
                  case RTLOP_XORI:
                  case RTLOP_SLLI:
                  case RTLOP_SRLI:
                  case RTLOP_SRAI:
                  case RTLOP_RORI:
                  case RTLOP_SLTUI:
                  case RTLOP_SLTSI: {
                    unsigned int rs, rt;  // src1/src2 MIPS registers
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                        rs = MIPS_at;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                        rs = MIPS_at;
                    } else {
                        rs = src1_reg->native_reg;
                    }
                    rt = MIPS_v1;
                    APPEND(MIPS_LUI(rt, reg->result.imm>>16 & 0xFFFF));
                    APPEND(MIPS_ORI(rt, rt, reg->result.imm & 0xFFFF));
                    if ((mips_opcodes[reg->result.opcode] & 0xFC00003F) < 010){
                        APPEND(mips_opcodes[reg->result.opcode] | rt<<21 | rs<<16);
                    } else {
                        APPEND(mips_opcodes[reg->result.opcode] | rs<<21 | rt<<16);
                    }
                    break;
                  }

                  case RTLOP_MULU:
                  case RTLOP_MULS:
                  case RTLOP_DIVMODU:
                  case RTLOP_DIVMODS: {
                    unsigned int rs, rt;
                    APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, -8));
                    APPEND(MIPS_MFHI(MIPS_v1));
                    APPEND(MIPS_SW(MIPS_v1, 0, MIPS_sp));
                    APPEND(MIPS_MFLO(MIPS_v1));
                    APPEND(MIPS_SW(MIPS_v1, 4, MIPS_sp));
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at, src1_reg->stack_offset, MIPS_sp));
                        rs = MIPS_at;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                        rs = MIPS_at;
                    } else {
                        rs = src1_reg->native_reg;
                    }
                    if (IS_SPILLED(reg->result.src2)) {
                        APPEND(MIPS_LW(MIPS_v1, src2_reg->stack_offset, MIPS_sp));
                        rt = MIPS_v1;
                    } else if (src2_reg->mips.is_in_hilo) {
                        APPEND(src2_reg->mips.is_in_hilo | MIPS_v1<<11);
                        rt = MIPS_v1;
                    } else {
                        rt = src2_reg->native_reg;
                    }
                    APPEND(mips_opcodes[reg->result.opcode] | rs<<21 | rt<<16);
                    if (reg->result.second_res) {
                        APPEND(MIPS_MFHI(MIPS_at));
                    } else {
                        APPEND(MIPS_MFLO(MIPS_at));
                    }
                    APPEND(MIPS_LW(MIPS_v1, 0, MIPS_sp));
                    APPEND(MIPS_MTHI(MIPS_v1));
                    APPEND(MIPS_LW(MIPS_v1, 4, MIPS_sp));
                    APPEND(MIPS_MTLO(MIPS_v1));
                    APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
                    break;
                  }

                  case RTLOP_SELECT: {
                    APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, -8));
                    APPEND(MIPS_SW(MIPS_v0, 0, MIPS_sp));
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                    } else {
                        APPEND(MIPS_MOVE(MIPS_at, src1_reg->native_reg));
                    }
                    int reg_src2, reg_cond;
                    if (IS_SPILLED(reg->result.src2)) {
                        APPEND(MIPS_LW(MIPS_v0,
                                       src2_reg->stack_offset, MIPS_sp));
                        reg_src2 = MIPS_v0;
                    } else if (src2_reg->mips.is_in_hilo) {
                        APPEND(src2_reg->mips.is_in_hilo | MIPS_v0<<11);
                        reg_src2 = MIPS_v0;
                    } else {
                        reg_src2 = src2_reg->native_reg;
                    }
                    if (IS_SPILLED(reg->result.cond)) {
                        APPEND(MIPS_LW(MIPS_v1, block->regs[reg->result.cond].stack_offset, MIPS_sp));
                        reg_cond = MIPS_v1;
                    } else if (block->regs[reg->result.cond].mips.is_in_hilo) {
                        APPEND(block->regs[reg->result.cond].mips.is_in_hilo
                               | MIPS_at<<11);
                    } else {
                        reg_cond = block->regs[reg->result.cond].native_reg;
                    }
                    APPEND(MIPS_MOVZ(MIPS_at, reg_src2, reg_cond));
                    APPEND(MIPS_LW(MIPS_v0, 0, MIPS_sp));
                    APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
                    break;
                  }

                  case RTLOP_BFEXT:
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src1_reg->stack_offset, MIPS_sp));
                        APPEND(MIPS_EXT(MIPS_at, MIPS_at,
                                        insn->bitfield.start,
                                        insn->bitfield.count));
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_at<<11);
                    } else {
                        APPEND(MIPS_EXT(MIPS_at, src1_reg->native_reg,
                                        insn->bitfield.start,
                                        insn->bitfield.count));
                    }
                    break;

                  case RTLOP_BFINS: {
                    unsigned int rs;
                    if (IS_SPILLED(reg->result.src2)) {
                        APPEND(MIPS_LW(MIPS_at,
                                       src2_reg->stack_offset, MIPS_sp));
                    } else if (src2_reg->mips.is_in_hilo) {
                        APPEND(src2_reg->mips.is_in_hilo | MIPS_at<<11);
                    } else {
                        APPEND(MIPS_MOVE(MIPS_at, src2_reg->native_reg));
                    }
                    if (IS_SPILLED(reg->result.src1)) {
                        APPEND(MIPS_LW(MIPS_v1, src1_reg->stack_offset, MIPS_sp));
                        rs = MIPS_v1;
                    } else if (src1_reg->mips.is_in_hilo) {
                        APPEND(src1_reg->mips.is_in_hilo | MIPS_v1<<11);
                        rs = MIPS_v1;
                    } else {
                        rs = src1_reg->native_reg;
                    }
                    APPEND(MIPS_INS(MIPS_at, rs, insn->bitfield.start,
                                    insn->bitfield.count));
                    break;
                  }

                  default:
                    DMSG("%p/%u: Don't know how to emulate result opcode %u"
                         " for r%u", block, insn_index, reg->result.opcode,
                         rtl_reg);
#ifdef PSP_DEBUG
                    return 0;  // This is a bug, so abort
#else
                    break;
#endif
                }
                break;
              }  // case RTLREG_RESULT{,_NOFOLD}

              default:
                DMSG("%p/%u: Don't know how to emulate register source %u"
                     " for r%u", block, insn_index, reg->source, rtl_reg);
                /* Let it slide, because there's nothing we can do about it */
                break;

            }  // switch (reg->source)
            mips_reg = MIPS_at;
        }  // if live

        if (offset) {
            if (offset+0x8000 < 0x10000) {
                APPEND(MIPS_ADDIU(MIPS_at, mips_reg, offset));
            } else {
                if (offset < 0x10000) {
                    APPEND(MIPS_ORI(MIPS_v1, MIPS_zero, offset));
                } else {
                    APPEND(MIPS_LUI(MIPS_v1, offset >> 16));
                    APPEND(MIPS_ORI(MIPS_v1, MIPS_v1, offset & 0xFFFF));
                }
                APPEND(MIPS_ADDU(MIPS_at, mips_reg, MIPS_v1));
            }
            mips_reg = MIPS_at;
        }

        APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, -8));
        APPEND(MIPS_SW(MIPS_a0, 0, MIPS_sp));
        if (sh2_reg & 0x80) {  // Used for SR.T
            const uintptr_t cache_ptr = (uintptr_t)&sh2_regcache[16];
            APPEND(MIPS_LUI(MIPS_a0, (cache_ptr + 0x8000) >> 16));
            if (!(block->sh2_regcache_mask & 1<<16)) {
                if (block->regs[1].source != RTLREG_PARAMETER
                 || block->regs[1].param_index != 0
                ) {
                    DMSG("r1 is not state block, can't handle");
                    return 0;
                }
                if (IS_SPILLED(1)) {
                    APPEND(MIPS_LW(MIPS_v1,
                                   block->regs[1].stack_offset, MIPS_sp));
                    APPEND(MIPS_LW(MIPS_v1, offsetof(SH2State,SR), MIPS_v1));
                } else {
                    APPEND(MIPS_LW(MIPS_v1, offsetof(SH2State,SR),
                                   block->regs[1].native_reg));
                }
                block->sh2_regcache_mask |= 1<<16;
            } else {
                APPEND(MIPS_LW(MIPS_v1, cache_ptr & 0xFFFF, MIPS_a0));
            }
            APPEND(MIPS_INS(MIPS_v1, mips_reg, SR_T_SHIFT, 1));
            APPEND(MIPS_SW(MIPS_v1, cache_ptr & 0xFFFF, MIPS_a0));
            APPEND(MIPS_LW(MIPS_a0, 0, MIPS_sp));
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
        } else {
            const uintptr_t cache_ptr = (uintptr_t)&sh2_regcache[sh2_reg];
            APPEND(MIPS_LUI(MIPS_a0, (cache_ptr + 0x8000) >> 16));
            APPEND(MIPS_SW(mips_reg, cache_ptr & 0xFFFF, MIPS_a0));
            APPEND(MIPS_LW(MIPS_a0, 0, MIPS_sp));
            APPEND(MIPS_ADDIU(MIPS_sp, MIPS_sp, 8));
            block->sh2_regcache_mask |= 1<<sh2_reg;
        }

    } else {  // !rtl_reg
        /* Clear the cache for this SH-2 register (or set PC directly) */

        if (offset) {  // Used to record PC directly without a register
            APPEND(MIPS_LUI(MIPS_at, offset>>16 & 0xFFFF));
            APPEND(MIPS_ORI(MIPS_at, MIPS_at, offset & 0xFFFF));
            const uintptr_t cache_ptr = (uintptr_t)&sh2_regcache[sh2_reg];
            APPEND(MIPS_LUI(MIPS_v1, (cache_ptr + 0x8000) >> 16));
            APPEND(MIPS_SW(MIPS_at, cache_ptr & 0xFFFF, MIPS_v1));
            block->sh2_regcache_mask |= 1<<sh2_reg;
        } else if (!(sh2_reg & 0x80)) {
            block->sh2_regcache_mask &= ~(1<<sh2_reg);
        }

    }

    return num_insns;
}

/*----------------------------------*/

/**
 * sh2_stealth_trace_store:  Add MIPS code to trace a store operation for
 * SH-2 TRACE_STEALTH mode.
 *
 * [Parameters]
 *          block: RTL block being translated
 *     insn_index: Index of RTL instruction to translate
 * [Return value]
 *     Number of RTL instructions processed (nonzero) on success, zero on error
 */
static unsigned int sh2_stealth_trace_store(RTLBlock * const block,
                                            const uint32_t insn_index)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);
    PRECOND(insn_index < block->num_insns, return 0);

    const RTLInsn *insn = &block->insns[insn_index];
    unsigned int num_insns = 1;

    APPEND(MIPS_JAL(((uintptr_t)code_save_regs & 0x0FFFFFFC) >> 2));
    APPEND(MIPS_NOP());

    uintptr_t funcptr;
    switch (insn->src_imm & 0xFFFF) {
      case 1: funcptr = (uintptr_t)trace_storeb_callback; break;
      case 2: funcptr = (uintptr_t)trace_storew_callback; break;
      case 4: funcptr = (uintptr_t)trace_storel_callback; break;
      default:
        DMSG("Invalid store trace type %u", insn->src_imm & 0xFF);
        return 0;
    }

    uint32_t op = insn[num_insns++].src_imm;
    PRECOND(op>>28 == 0xD, return 1);
    if (op & 0x08000000) {
        PRECOND(op>>24 == 0xD8, return 0);
        APPEND(MIPS_LUI(MIPS_a0, op & 0xFFFF));
        op = insn[num_insns++].src_imm;
        PRECOND(op>>24 == 0xDC, return 0);
        APPEND(MIPS_ORI(MIPS_a0, MIPS_a0, op & 0xFFFF));
    } else {
        const uint32_t address_reg = op & 0xFFFF;
        const RTLRegister *reg = &block->regs[address_reg];
        if (IS_SPILLED(address_reg)) {
            APPEND(MIPS_LW(MIPS_a0, 4*18 + reg->stack_offset, MIPS_sp));
        } else if (reg->mips.is_in_hilo) {
            APPEND(reg->mips.is_in_hilo | MIPS_a0<<11);
        } else if (reg->native_reg != MIPS_a0) {
            APPEND(MIPS_MOVE(MIPS_a0, reg->native_reg));
        }
        op = insn[num_insns++].src_imm;
        PRECOND(op>>24 == 0xD4, return 1);
        APPEND(MIPS_LUI(MIPS_at, op & 0xFFFF));
        op = insn[num_insns++].src_imm;
        PRECOND(op>>24 == 0xD6, return 1);
        APPEND(MIPS_ORI(MIPS_at, MIPS_at, op & 0xFFFF));
        APPEND(MIPS_ADD(MIPS_a0, MIPS_a0, MIPS_at));
    }

    op = insn[num_insns++].src_imm;
    PRECOND(op>>28 == 0xE, return 1);
    const uint32_t src_reg = op & 0xFFFF;
    const RTLRegister *reg = &block->regs[src_reg];
    APPEND(MIPS_JAL((funcptr & 0x0FFFFFFC) >> 2));
    if (IS_SPILLED(src_reg)) {
        APPEND(MIPS_LW(MIPS_a1, 4*18 + reg->stack_offset, MIPS_sp));
    } else if (reg->mips.is_in_hilo) {
        APPEND(reg->mips.is_in_hilo | MIPS_a1<<11);
    } else if (reg->native_reg == MIPS_a0) {
        APPEND(MIPS_LW(MIPS_a1, 8, MIPS_sp));
    } else if (reg->native_reg != MIPS_a1) {
        APPEND(MIPS_MOVE(MIPS_a1, reg->native_reg));
    } else {  // Don't forget to fill the delay slot!
        APPEND(MIPS_NOP());
    }

    APPEND(MIPS_JAL(((uintptr_t)code_restore_regs & 0x0FFFFFFC) >> 2));
    APPEND(MIPS_NOP());

    return num_insns;
}

/*----------------------------------*/

#endif  // RTL_TRACE_STEALTH_FOR_SH2

/*************************************************************************/

/**
 * resolve_branches:  Resolve branches to RTL labels and jumps to the
 * epilogue within MIPS code.  If MIPS_OPTIMIZE_BRANCHES is defined, also
 * perform optimizations on branches.
 *
 * [Parameters]
 *     block: RTL block to translate
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int resolve_branches(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->label_offsets != NULL, return 0);

    uint32_t *native_ptr = (uint32_t *)block->native_buffer;
    uint32_t * const native_top =
        (uint32_t *)((uintptr_t)native_ptr + block->native_length);
    const uint32_t * const epilogue_ptr =
        (uint32_t *)((uintptr_t)native_ptr + block->mips.epilogue_offset);
    const uint32_t * const chain_ptr =
        (uint32_t *)((uintptr_t)native_ptr + block->mips.chain_offset);

    for (; native_ptr < native_top; native_ptr++) {
        uint32_t opcode = *native_ptr;

        if (opcode>>27 == __OP_BEQ_LABEL>>1 /*covers both opcodes*/) {
            const unsigned int label = opcode & ((1<<16) - 1);
            const uint32_t delay_offset =
                (uintptr_t)native_ptr - (uintptr_t)block->native_buffer + 4;
            if (UNLIKELY((int32_t)block->label_offsets[label] < 0)) {
                DMSG("%p: Label %u not defined at native offset %u",
                     block, label, delay_offset-4);
                return 0;
            }
            int32_t disp = (block->label_offsets[label] - delay_offset);
            if (disp < -0x8000<<2 || disp > 0x7FFF<<2) {
                /* FIXME: Technically, we could resolve this by flipping
                 * the sense of the branch and inserting a jump
                 * instruction, but that would require another pass after
                 * the final buffer pointer was known to fill in absolute
                 * addresses; we could also do a BAL and calculate the
                 * target address manually.  But in any case, the chance
                 * of hitting this is so slim that we don't worry about it
                 * for now. */
                DMSG("%p: Displacement to label %u (+0x%X) from +0x%X too"
                     " large, can't resolve", block, label,
                     block->label_offsets[label], delay_offset);
                return 0;
            }
            *native_ptr = ((__OP_BEQ>>1) << 27)
                        | (opcode & 0x07FF0000)  // Include the EQ/NE bit
                        | ((disp>>2) & 0xFFFF);

        } else if (opcode == MIPS_B_EPILOGUE_RET_CONST) {
            const int32_t offset = epilogue_ptr - (native_ptr+1);
            if (LIKELY(offset <= 0x7FFF)) {  // Always positive
                *native_ptr = MIPS_B(offset);
            } else {
                DMSG("WARNING: block too large, using nonportable J");
                const uint32_t j_addr = (uintptr_t)epilogue_ptr & 0x0FFFFFFC;
                *native_ptr = MIPS_J(j_addr >> 2);
            }

        } else if (opcode == MIPS_B_EPILOGUE_CHAIN_AT) {
            const int32_t offset = chain_ptr - (native_ptr+1);
            if (LIKELY(offset <= 0x7FFF)) {  // Always positive
                *native_ptr = MIPS_B(offset);
            } else {
                DMSG("WARNING: block too large, using nonportable J");
                const uint32_t j_addr = (uintptr_t)chain_ptr & 0x0FFFFFFC;
                *native_ptr = MIPS_J(j_addr >> 2);
            }

        }

#ifdef MIPS_OPTIMIZE_BRANCHES
        opcode = *native_ptr;  // It may have been updated above
        if (opcode>>28 == __OP_BEQ>>2
         || (opcode>>26 == __OP_REGIMM
             && (insn_rt(opcode) & ~021) == __RI_BLTZ)
        ) {
            uint32_t likely_opcode;
            if (opcode>>16 == MIPS_B(0)>>16) {
                /* Unconditional branch, so no need for Likely bit */
                likely_opcode = opcode;
            } else if (opcode>>28 == __OP_BEQ>>2) {
                likely_opcode = opcode | (uint32_t)020<<26;
            } else {  // REGIMM insns
                likely_opcode = opcode | (uint32_t)002<<16;
            }
            int32_t offset = (int16_t)(opcode & 0xFFFF);
            const uint32_t *target = native_ptr + (1+offset);
            unsigned int limit = 16;  // Watch out for infinite loops
            while ((*target>>16 == MIPS_B(0)>>16
                    || *target>>16 == MIPS_B_LABEL(0)>>16
                    || *target == MIPS_B_EPILOGUE_RET_CONST)
                && (native_ptr[1] == MIPS_NOP() || target[1] == MIPS_NOP())
                && limit > 0
            ) {
                limit--;
                if (target[1] != MIPS_NOP()) {
                    native_ptr[1] = target[1];  // Must have been a NOP
                    opcode = likely_opcode;
                }
                int32_t new_offset;
                if (*target == MIPS_B_EPILOGUE_RET_CONST) {
                    const uint32_t delay_pos =
                        (native_ptr+1) - (uint32_t *)block->native_buffer;
                    new_offset = block->mips.epilogue_offset/4 - delay_pos;
                } else if (*target>>16 == MIPS_B_LABEL(0)>>16) {
                    const unsigned int label = *target & 0xFFFF;
                    if (UNLIKELY((int32_t)block->label_offsets[label] < 0)) {
                        /* Just skip out here; the Bxx_LABEL processing
                         * code will report the error */
                        break;
                    }
                    const uint32_t delay_pos =
                        (native_ptr+1) - (uint32_t *)block->native_buffer;
                    new_offset = block->label_offsets[label]/4 - delay_pos;
                } else {
                    new_offset = offset + (1 + (int16_t)(*target & 0xFFFF));
                }
                if (new_offset < -0x8000 || new_offset > 0x7FFF) {
                    break;
                }
                offset = new_offset;
                *native_ptr = (opcode & 0xFFFF0000) | (offset & 0xFFFF);
                target = native_ptr + (1+offset);
            }
            if (*native_ptr>>16 == MIPS_B(0)>>16
             && (*target>>26 == MIPS_J(0)
                 || (*target & 0xFC00003F) == MIPS_JR(0))
             && (native_ptr[1] == MIPS_NOP() || target[1] == MIPS_NOP())
            ) {
                /* Chain an unconditional branch through to a jump (but not
                 * JAL/JALR, since we'd get the wrong return address) */
                if (target[1] != MIPS_NOP()) {
                    native_ptr[1] = target[1];
                }
                *native_ptr = *target;
            } else if (offset != 0x7FFF
                    && native_ptr[1] == MIPS_NOP()
                    && !insn_is_jump(*target)
                    && !insn_is_branch(*target)
            ) {
                *native_ptr =
                    (likely_opcode & 0xFFFF0000) | ((offset+1) & 0xFFFF);
                native_ptr[1] = *target;
            }
        }
#endif

    }  // for (; native_ptr < native_top; native_ptr++)

    return 1;
}

/*************************************************************************/

#undef MAP_REGISTER
#undef APPEND

/*************************************************************************/
/*********************** Low-level helper routines ***********************/
/*************************************************************************/

/**
 * append_insn:  Append a MIPS instruction word to the given block.
 *
 * [Parameters]
 *     block: RTL block to append to
 *      insn: MIPS instruction word to append
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int append_insn(RTLBlock * const block, const uint32_t insn)
{
    PRECOND(block != NULL, return 0);

    if (UNLIKELY(block->native_length >= block->native_bufsize)) {
        if (UNLIKELY(!expand_block(block))) {
            return 0;
        }
    }

    *(uint32_t *)((uint8_t *)block->native_buffer + block->native_length)
        = insn;
    block->native_length += 4;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * expand_block:  Expand the current native code buffer.
 *
 * [Parameters]
 *     block: RTL block to expand
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int expand_block(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);

    block->native_bufsize = block->native_length + NATIVE_EXPAND_SIZE;
    void *new_buffer = realloc(block->native_buffer, block->native_bufsize);
    if (UNLIKELY(!new_buffer)) {
        DMSG("No memory to expand native buffer to %u bytes",
             block->native_bufsize);
        return 0;
    }
    block->native_buffer = new_buffer;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * last_insn:  Return the last 32-bit instruction word added to the given
 * block.  If the block is empty, returns MIPS_NOP().
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Last instruction word added
 */
static uint32_t last_insn(const RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->native_buffer != NULL, return 0);

    if (UNLIKELY(block->native_length == 0)) {
        return MIPS_NOP();
    }
    return *(uint32_t *)((uint8_t *)block->native_buffer
                         + (block->native_length - 4));
}

/*-----------------------------------------------------------------------*/

/**
 * pop_insn:  Return the last 32-bit instruction word added to the given
 * block, and remove that word from the end of the block.  If the block is
 * empty, returns MIPS_NOP() without modifying the block.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Last instruction word added
 */
static uint32_t pop_insn(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->native_buffer != NULL, return 0);

    if (UNLIKELY(block->native_length == 0)) {
        return MIPS_NOP();
    }
    block->native_length -= 4;
    return *(uint32_t *)((uint8_t *)block->native_buffer
                         + block->native_length);
}

/*************************************************************************/

/**
 * append_float:  Append a high-latency instruction (a load, multiply, or
 * divide instruction) to the given block.  If possible (and if
 * MIPS_OPTIMIZE_SCHEDULING is enabled), float the instruction upwards to
 * obtain a distance of at least "latency" instructions before the next
 * instruction.  Preceding instructions in the same dependency chain are
 * also floated upwards to preserve correctness.
 *
 * [Parameters]
 *       block: RTL block to append to
 *        insn: Instruction word to append
 *     latency: Desired distance to next instruction
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int append_float(RTLBlock * const block, const uint32_t insn,
                        const int latency)
{
#ifndef MIPS_OPTIMIZE_SCHEDULING
    return append_insn(block, insn);
#else

    PRECOND(block != NULL, return 0);
    PRECOND(insn_is_load(insn)
            || (insn & 0xFC00003C) == __SP_MULT
            || (insn & 0xFC00003E) == __SP_MADD
            || (insn & 0xFC00003E) == __SP_MSUB, return 0);

    /* Does the dependency chain ending with the new instruction have a
     * load or store instruction?  If so, do we know where it points? */

    int has_mem;
    unsigned int unique_pointer;
    uint32_t unique_pointer_birth;
    if (insn_is_load(insn)) {  // We're never called for stores
        has_mem = 1;
        PRECOND(block->mips.reg_map[insn_rs(insn)] != NULL, return 0);
        unique_pointer = block->mips.reg_map[insn_rs(insn)]->unique_pointer;
        unique_pointer_birth = block->mips.reg_map[insn_rs(insn)]->birth;
    } else {
        has_mem = 0;
        unique_pointer = 0;
    }

    /* First add the instruction to the block. */

    if (UNLIKELY(!append_insn(block, insn))) {
        return 0;
    }

    /* Extract registers used and set by this instruction; we'll add to
     * these sets all registers used by preceding instructions in the
     * dependency chain. */

    uint32_t regs_used = insn_regs_used(insn);
    uint32_t regs_set  = insn_regs_set(insn);

    /* Scan backwards, up to the beginning of the unit or preceding branch
     * delay slot, to find instructions that can be placed below this one,
     * and move them below the current instruction. */

    uint32_t * const unit_start = (uint32_t *)
        ((uint8_t *)block->native_buffer + block->mips.unit_start);
    uint32_t *insn_ptr = (uint32_t *)
        ((uint8_t *)block->native_buffer + (block->native_length - 4));
    uint32_t * const target = insn_ptr - (latency-1);
    uint32_t *search;

    for (search = insn_ptr - 1; search >= unit_start; search--) {

        /* If the preceding instruction is a jump or branch, stop here.
         * If this is the first instruction in the unit, the preceding
         * instruction can't be a jump or branch (if it was, a delay slot
         * instruction would have been added after it at the end of the
         * unit). */

        if (search > unit_start
         && (insn_is_jump(search[-1]) || insn_is_branch(search[-1]))
        ) {
            break;
        }

        /* An instruction is independent of a following sequence of
         * instructions if that instruction:
         *    1) Does not set a register used in the following sequence
         *       (i.e., the sequence does not depend on the result of the
         *       instruction).
         *    2) Does not set a register set in the following sequence
         *       (i.e., the sequence does not reuse the instruction's
         *       result register).
         *    3) Does not use a register set in the following sequence
         *       (i.e., the sequence does not clobber an operand of the
         *       instruction).
         * We explicitly do not move load or store instructions across
         * other load or store instructions, so as to maintain ordering of
         * memory accesses (otherwise, we might try to load from an address
         * before the store that updates it) and to avoid introducing a
         * stall by shifting a previous load farther down the code stream.
         * However, we _do_ float a load above a preceding store if we know
         * that the store and load point to different addresses; i.e., if
         * at least one of the registers is a unique pointer, and they do
         * not reference the same unique address. */

        const uint32_t this_used = insn_regs_used(*search);
        const uint32_t this_set  = insn_regs_set(*search);
        int this_is_mem;
        if (insn_is_load(*search)) {
            this_is_mem = 1;
        } else if (insn_is_store(*search)) {
            // FIXME: We don't currently save unique pointer data with the
            // generated MIPS instructions, so for now we assume that the
            // current register is the only one pointing to this unique
            // region.  This works at the moment because the state block
            // pointer is the only register we mark as a unique pointer,
            // but could break in other cases.
            if (unique_pointer == 0 || insn_rs(insn) != insn_rs(*search)) {
                this_is_mem = 1;
            } else {
                PRECOND(block->mips.reg_map[insn_rs(*search)] != NULL,
                        return 0);
                this_is_mem = (insn_imm(insn) == insn_imm(*search));
            }
        } else {
            this_is_mem = 0;
        }
        if ((this_set & (regs_used | regs_set)) != 0
         || (this_used & regs_set) != 0
         || (has_mem && this_is_mem)
        ) {
            /* Add this register's used and set registers to the dependency
             * chain's cumulative set. */
            regs_used |= this_used;
            regs_set  |= this_set;
            /* If it was a load or store instruction, record that fact. */
            if (insn_is_load(*search) || insn_is_store(*search)) {
                has_mem = 1;
                /* Register assignments may have changed, so we don't know
                 * for certain what the register pointed to at the time, so
                 * play it safe. */
                unique_pointer = 0;
            }
        } else {
            /* Move this instruction immediately below the one we added. */
            const uint32_t move_insn = *search;
            uint32_t *move_ptr;
            for (move_ptr = search; move_ptr < insn_ptr; move_ptr++) {
                *move_ptr = *(move_ptr + 1);
            }
            *move_ptr = move_insn;
            /* The instruction we added has now moved one word up. */
            insn_ptr--;
            /* If we've achieved the requested distance, stop. */
            if (insn_ptr <= target) {
                break;
            }
        }

    }  // for (search = insn_ptr - 1; search >= unit_start; search--)

    return 1;

#endif  // MIPS_OPTIMIZE_SCHEDULING
}

/*-----------------------------------------------------------------------*/

/**
 * append_branch:  Append a branch instruction to the given block.  If
 * possible (and if MIPS_OPTIMIZE_DELAY_SLOT is enabled), place the current
 * last instruction of the block into the branch's delay slot.
 *
 * [Parameters]
 *     block: RTL block to append to
 *      insn: Branch instruction word to append
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int append_branch(RTLBlock * const block, const uint32_t insn)
{
    PRECOND(block != NULL, return 0);

    uint32_t delay_slot = MIPS_NOP();

#ifdef MIPS_OPTIMIZE_DELAY_SLOT
    if (block->native_length - 4 >= block->mips.unit_start) {
        int can_swap_insns = 0;
        const uint32_t prev1_insn = pop_insn(block);
        uint32_t prev2_insn;
        if (block->native_length - 4 >= block->mips.unit_start) {
            prev2_insn = last_insn(block);
        } else {
            prev2_insn = MIPS_NOP();
        }
        if (!insn_is_jump(prev2_insn) && !insn_is_branch(prev2_insn)) {
            if (insn == MIPS_B_EPILOGUE_RET_CONST
             || insn == MIPS_B_EPILOGUE_CHAIN_AT
             || !(insn_regs_set(prev1_insn) & insn_regs_used(insn))
            ) {
                can_swap_insns = 1;
            }
        }
        if (can_swap_insns) {
            delay_slot = prev1_insn;
        } else {
            if (UNLIKELY(!append_insn(block, prev1_insn))) {
                // Just to be safe...
                DMSG("Failed to re-append insn");
                return 0;
            }
        }
    }
#endif

    return append_insn(block, insn) && append_insn(block, delay_slot);
}

/*************************************************************************/

/**
 * append_prologue:  Append a function prologue to the given block.
 *
 * [Parameters]
 *     block: RTL block to append to
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int append_prologue(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);

    /* Set up the stack frame (if we need one) */
    if (block->mips.total_frame_size) {
        if (UNLIKELY(!append_insn(block, MIPS_ADDIU(MIPS_sp, MIPS_sp, -(block->mips.total_frame_size))))) {
            DMSG("Failed to append prologue (stack frame)");
            return 0;
        }
    }

    /* Save $ra if necessary */
    if (block->mips.need_save_ra
     && UNLIKELY(!append_insn(block, MIPS_SW(MIPS_ra, block->mips.total_frame_size - 4, MIPS_sp)))
    ) {
        DMSG("Failed to append prologue ($ra)");
        return 0;
    }

    /* Save all callee-saved registers that we use */
    uint32_t offset = block->mips.total_frame_size - 8;
    unsigned int i;
    for (i = 0; i < lenof(callee_saved_regs); i++) {
        const unsigned int mips_reg = callee_saved_regs[i];
        if (block->mips.sreg_used & (1 << mips_reg)) {
            if (UNLIKELY(!append_insn(block,
                                      MIPS_SW(mips_reg, offset, MIPS_sp)))) {
                DMSG("Failed to append prologue ($%u)", mips_reg);
                return 0;
            }
            offset -= 4;
        }
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * append_epilogue:  Append a function epilogue to the given block.
 *
 * [Parameters]
 *     block: RTL block to append to
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int append_epilogue(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);

    /* Save the offset to the epilogue code for branch resolution */
    block->mips.epilogue_offset = block->native_length;

    /* Restore the original values of saved registers ($ra and $sN) */
    if (block->mips.need_save_ra
     && UNLIKELY(!append_insn(block, MIPS_LW(MIPS_ra, block->mips.total_frame_size - 4, MIPS_sp)))
    ) {
        DMSG("Failed to append epilogue ($ra)");
        return 0;
    }
    uint32_t offset = block->mips.total_frame_size - 8;
    unsigned int i;
    for (i = 0; i < lenof(callee_saved_regs); i++) {
        const unsigned int mips_reg = callee_saved_regs[i];
        if (block->mips.sreg_used & (1 << mips_reg)) {
            if (UNLIKELY(!append_insn(block,
                                      MIPS_LW(mips_reg, offset, MIPS_sp)))) {
                DMSG("Failed to append epilogue ($%u)", mips_reg);
                return 0;
            }
            offset -= 4;
        }
    }

    /* Free the stack frame (if we allocated one) and return */
    uint32_t final_insn;
    if (block->mips.total_frame_size) {
        final_insn =
            MIPS_ADDIU(MIPS_sp, MIPS_sp, block->mips.total_frame_size);
    } else {
        final_insn = MIPS_NOP();
    }
    if (UNLIKELY(!append_insn(block, MIPS_JR(MIPS_ra)))
     || UNLIKELY(!append_insn(block, final_insn))
    ) {
        DMSG("Failed to append epilogue (return)");
        return 0;
    }

    /* If we need a chain-to-$at epilogue as well, generate that */
    if (block->mips.need_chain_at) {
        block->mips.chain_offset = block->native_length;
        if (block->mips.need_save_ra
         && UNLIKELY(!append_insn(block, MIPS_LW(MIPS_ra, block->mips.total_frame_size - 4, MIPS_sp)))
        ) {
            DMSG("Failed to append chain epilogue ($ra)");
            return 0;
        }
        offset = block->mips.total_frame_size - 8;
        for (i = 0; i < lenof(callee_saved_regs); i++) {
            const unsigned int mips_reg = callee_saved_regs[i];
            if (block->mips.sreg_used & (1 << mips_reg)) {
                if (UNLIKELY(!append_insn(block,
                                          MIPS_LW(mips_reg, offset, MIPS_sp)))) {
                    DMSG("Failed to append chain epilogue ($%u)", mips_reg);
                    return 0;
                }
                offset -= 4;
            }
        }
        if (block->mips.total_frame_size) {
            final_insn =
                MIPS_ADDIU(MIPS_sp, MIPS_sp, block->mips.total_frame_size);
        } else {
            final_insn = MIPS_NOP();
        }
        if (UNLIKELY(!append_insn(block, MIPS_JR(MIPS_at)))
         || UNLIKELY(!append_insn(block, final_insn))
        ) {
            DMSG("Failed to append chain epilogue (return)");
            return 0;
        }
    }  // if (block->mips.need_chain_at)

    return 1;
}

/*************************************************************************/
/****************** MIPS opcode informational functions ******************/
/*************************************************************************/

/**
 * insn_rs, insn_rt, insn_rd:  Return the register number specified in the
 * rs, rt, or rd field of the given instruction, respectively.
 *
 * [Parameters]
 *     opcode: Instruction opcode
 * [Return value]
 *     rs, rt, or rd register number (0-31)
 */
static inline int insn_rs(const uint32_t opcode)
{
    return (opcode >> 21) & 0x1F;
}

static inline int insn_rt(const uint32_t opcode)
{
    return (opcode >> 16) & 0x1F;
}

static inline int insn_rd(const uint32_t opcode)
{
    return (opcode >> 11) & 0x1F;
}

/*----------------------------------*/

/**
 * insn_imm:  Return the 16-bit immediate value specified in the given
 * instruction.
 *
 * [Parameters]
 *     opcode: Instruction opcode
 * [Return value]
 *     16-bit immediate value as a signed integer
 */
static inline int insn_imm(const uint32_t opcode)
{
    return (int)(int16_t)opcode;
}

/*************************************************************************/

/**
 * insn_is_*:  Return whether the given instruction is of the type
 * specified by the function name:
 *     insn_is_load     -- a load instruction
 *     insn_is_store    -- a store instruction
 *     insn_is_jump     -- a JUMP class instruction (J or JAL)
 *     insn_is_branch   -- an IMM/REGIMM class branch instruction (BEQ, etc.)
 *     insn_is_imm      -- an IMM class instruction
 *     insn_is_imm_alu  -- an IMM class ALU instruction (ADDI, etc.)
 *     insn_is_special  -- a SPECIAL class instruction
 *     insn_is_regimm   -- a REGIMM class instruction (BLTZ, etc.)
 *     insn_is_allegrex -- an Allegrex-specific instruction (INS, SEB, etc.)
 *
 * [Parameters]
 *     opcode: Instruction opcode
 * [Return value]
 *     Nonzero if the instruction is of the named type, else zero
 */
static inline int insn_is_load(const uint32_t opcode)
{
    return opcode>>29 == 4;  // We don't use LL, so we don't check for it
}

static inline int insn_is_store(const uint32_t opcode)
{
    return opcode>>29 == 5;  // We don't use SC, so we don't check for it
}

static inline int insn_is_jump(const uint32_t opcode)
{
    return opcode>>27 == __OP_J>>1
        || (insn_is_special(opcode) && (opcode & 0x3E) == __SP_JR);  // JR/JALR
}

static inline int insn_is_branch(const uint32_t opcode)
{
    return opcode>>28 == __OP_BEQ>>2
        || opcode>>28 == __OP_BEQL>>2
        || opcode>>27 == __OP_BEQ_LABEL>>1
        || insn_is_regimm(opcode);
}

static inline int insn_is_imm(const uint32_t opcode)
{
    return (opcode>>26 >= __OP_BEQ && opcode>>26 <= __OP_BGTZL)
        || (opcode>>27 == __OP_BEQ_LABEL>>1);
}

static inline int insn_is_imm_alu(const uint32_t opcode)
{
    return opcode>>29 == 1;
}

static inline int insn_is_special(const uint32_t opcode)
{
    return opcode>>26 == __OP_SPECIAL;
}

static inline int insn_is_regimm(const uint32_t opcode)
{
    return opcode>>26 == __OP_REGIMM;
}

static inline int insn_is_allegrex(const uint32_t opcode)
{
    return opcode>>26 == __OP_ALLEGREX;
}

/*************************************************************************/

/**
 * insn_regs_used:  Return a bitmask of MIPS registers used by the given
 * instruction (i.e. the instruction's source registers).  $zero is never
 * included in the set; bit 0 instead reflects the HI and LO registers.
 *
 * [Parameters]
 *     opcode: Instruction opcode
 * [Return value]
 *     Bitmask of registers used by the instruction
 */
static inline uint32_t insn_regs_used(const uint32_t opcode)
{
    uint32_t regs;
    int hilo = 0;

    if (insn_is_load(opcode) || insn_is_regimm(opcode) 
     || insn_is_imm_alu(opcode)
    ) {
        regs = 1<<insn_rs(opcode);
    } else if (insn_is_store(opcode) || insn_is_imm(opcode)) {
        regs = 1<<insn_rs(opcode) | 1<<insn_rt(opcode);
    } else if (insn_is_special(opcode)) {
        regs = 1<<insn_rs(opcode) | 1<<insn_rt(opcode);
        if ((opcode & 0x3D) == __SP_MFHI) {  // MFHI or MFLO
            hilo = 1;
        }
    } else if (insn_is_allegrex(opcode)) {
        if ((opcode & 0x3F) == __AL_EXT) {
            regs = 1<<insn_rs(opcode);
        } else if ((opcode & 0x3F) == __AL_INS) {
            regs = 1<<insn_rs(opcode) | 1<<insn_rt(opcode);
        } else if ((opcode & 0x3F) == __AL_MISC) {
            regs = 1<<insn_rt(opcode);
        } else {
            regs = 0;
        }
    } else {
        regs = 0;
    }

    return (regs & ~(1<<MIPS_zero)) | hilo;
}

/*-----------------------------------------------------------------------*/

/**
 * insn_regs_set:  Return a bitmask of MIPS registers set by the given
 * instruction (i.e. the instruction's destination register, if any).
 * $zero is never included in the set; bit 0 instead reflects the HI and LO
 * registers.
 *
 * [Parameters]
 *     opcode: Instruction opcode
 * [Return value]
 *     Bitmask of registers set by the instruction
 */
static inline uint32_t insn_regs_set(const uint32_t opcode)
{
    uint32_t regs;
    int hilo = 0;

    if (insn_is_load(opcode) || insn_is_imm_alu(opcode)) {
        regs = 1<<insn_rt(opcode);
    } else if (insn_is_special(opcode)) {
        regs = 1<<insn_rd(opcode);
        if ((opcode & 0x3D) == __SP_MTHI  // MTHI or MTLO
         || (opcode & 0x38) == __SP_MULT  // MULT[U], DIV[U], MADD[U]
         || (opcode & 0x3E) == __SP_MSUB  // MSUB[U]
        ) {
            hilo = 1;
        }
    } else if (insn_is_allegrex(opcode)) {
        if ((opcode & 0x3B) == __AL_EXT) {  // EXT or INS
            regs = 1<<insn_rt(opcode);
        } else if ((opcode & 0x3F) == __AL_MISC) {
            regs = 1<<insn_rd(opcode);
        } else {
            regs = 0;
        }
    } else if (opcode>>26 == __OP_JAL
               || (insn_is_regimm(opcode)
                   && insn_rt(opcode)>>2 == __RI_BLTZAL>>2)
    ) {
        regs = 1<<MIPS_ra;
    } else {
        regs = 0;
    }

    return (regs & ~(1<<MIPS_zero)) | hilo;
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
