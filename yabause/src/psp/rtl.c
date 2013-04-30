/*  src/psp/rtl.c: Implementation of a register transfer language (RTL) for
                   dynamic translation
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
 * This source file defines the basic functions used in translating machine
 * language instructions into register transfer language (RTL), a platform-
 * neutral assembly-like language which can then be retranslated into
 * machine language on a specific target platform, such as x86 or MIPS.
 * (This is the same idea used in general-purpose compilers like GCC, but
 * done at a simpler level.)
 *
 * In a typical RTL, each instruction performs exactly one operation on
 * one or two operands, such as loading a value from memory or adding two
 * values together (a so-called three address code: one destination and
 * two sources).  Furthermore, with a few exceptions, every operand to an
 * RTL instruction is a register--hence the name "register transfer
 * language"--though many of these "registers" are only used for temporary
 * storage of values and will be optimized out in later stages.  This
 * design simplifies the optimization and code generation stages by
 * reducing the number of distinct operations that must be handled.
 * Note that most SH-2 instructions break down into multiple RTL
 * instructions; for example, "AND #imm,R0" might translate to:
 *     LOAD_IMM reg1, #offsetof(SH2State,R[0])
 *     ADD      reg2, state_reg, reg1 // state_reg holds the SH2State pointer
 *     LOAD_W   reg3, (reg2)
 *     LOAD_IMM reg4, #imm
 *     AND      reg5, reg3, reg4
 *     STORE_W  reg5, (reg2)
 * plus various housekeeping instructions such as updating the current PC
 * and cycle count.
 *
 * The RTL used here is loosely based on the MIPS instruction set, partly
 * because this implementation is initially targeted at MIPS CPUs
 * (specifically the Allegrex CPU used in the PlayStation Portable), but
 * also because the MIPS instruction set fits well with the RTL ideal of
 * single-operation instructions.
 *
 * In this implementation, all RTL registers are assumed to be 32 bits
 * wide, _except_ that native pointer values (however wide those may be)
 * may be loaded into registers via the RTLOP_LOAD_ADDR or RTLOP_LOAD*_PTR
 * instructions for use as load/store addresses or native call targets.
 * Pointer values may be the first operand to an ADD, SUB, AND, OR, or XOR
 * instruction; the second operand is then sign-extended (for ADD/SUB/AND)
 * or zero-extended (for OR/XOR) from 32 bits to the width of the pointer.
 * Pointer values can also be stored back to memory with the
 * RTLOP_STORE*_PTR instructions.  All other operations on pointer values
 * are undefined.
 *
 * Dynamic translation via RTL is accomplished using the following steps:
 *
 * 1) Create a new, empty RTL block by calling rtl_create_block().
 *
 * 2) Generate RTL code implementing the source instructions being
 *    recompiled.  RTL instructions are added to the block with
 *    rtl_add_insn(); new registers and labels for use in RTL instructions
 *    can be obtained with rtl_alloc_register() and rtl_alloc_label()
 *    respectively.
 *
 * 3) When the RTL code is complete, call rtl_finalize_block() to perform
 *    post-generation housekeeping on the RTL block.
 *
 * 4) Optionally call rtl_optimize_block() to optimize the RTL code.
 *
 * 5A) Call rtl_translate_block() to translate the RTL block into native
 *     machine code; _or_
 *
 * 5B) Call rtl_execute_block() to execute the RTL code directly,
 *     interpreting RTL instructions one by one.  This interpreted
 *     execution is naturally slow, and is intended for debugging only.
 *     NOTE: rtl_execute_block() and rtl_translate_block() cannot both be
 *     called for the same block.
 *
 * 6) When the block is no longer needed, dispose of it with
 *    rtl_destroy_block().  Destroying a block will not destroy native
 *    machine code generated with rtl_translate_block().
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

static int add_unit_edges(RTLBlock * const block);
static int update_live_ranges(RTLBlock * const block);

#ifdef RTL_TRACE_GENERATE
static void dump_block(const RTLBlock * const block, const char * const tag);
#endif

/*************************************************************************/
/********************** External interface routines **********************/
/*************************************************************************/

/**
 * rtl_create_block:  Create a new RTLBlock structure for translation.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     New block, or NULL on error
 */
RTLBlock *rtl_create_block(void)
{
    RTLBlock *block = malloc(sizeof(*block));
    if (!block) {
        DMSG("No memory for RTLBlock");
        return NULL;
    }

    block->insns = NULL;
    block->insn_unitmap = NULL;
    block->insns_size = INSNS_EXPAND_SIZE;
    block->num_insns = 0;

    block->units = NULL;
    block->units_size = UNITS_EXPAND_SIZE;
    block->num_units = 0;
    block->have_unit = 0;
    block->cur_unit = 0;

    block->label_unitmap = NULL;
    block->labels_size = LABELS_EXPAND_SIZE;
    block->next_label = 1;

    block->regs = NULL;
    block->regs_size = REGS_EXPAND_SIZE;
    block->next_reg = 1;
    block->first_live_reg = 0;
    block->last_live_reg = 0;
    block->unique_pointer_index = 1;

    block->finalized = 0;

    block->first_call_unit = -1;
    block->last_call_unit = -1;

    block->insns = malloc(sizeof(*block->insns) * block->insns_size);
    if (!block->insns) {
        DMSG("No memory for %d RTLInsns", block->insns_size);
        goto fail;
    }

    block->units = malloc(sizeof(*block->units) * block->units_size);
    if (!block->units) {
        DMSG("No memory for %d RTLUnits", block->units_size);
        goto fail;
    }

    block->regs = malloc(sizeof(*block->regs) * block->regs_size);
    if (!block->regs) {
        DMSG("No memory for %d RTLRegisters", block->regs_size);
        goto fail;
    }
    memset(&block->regs[0], 0, sizeof(*block->regs));

    block->label_unitmap =
        malloc(sizeof(*block->label_unitmap) * block->labels_size);
    if (!block->label_unitmap) {
        DMSG("No memory for %d labels", block->labels_size);
        goto fail;
    }
    block->label_unitmap[0] = -1;

#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] Created new block at %p\n", block);
#endif
    return block;

  fail:
    free(block->insns);
    free(block->units);
    free(block->regs);
    free(block->label_unitmap);
    free(block);
    return NULL;
}

/*************************************************************************/

/* rtl_add_insn() helpers -- these move function calls like realloc() out
 * of rtl_add_insn() to help the compiler optimize the fast path, by
 * avoiding register spillage when it's not necessary.  Note that GCC (at
 * least version 4.3) will inline these by default, but that actually slows
 * down the fast path on at least MIPS due to spillage of the function
 * parameter registers, so we force these to be generated as separate
 * functions. */
#ifdef __GNUC__
__attribute__((noinline))
#endif
static int rtl_add_insn_with_extend(RTLBlock *block, RTLOpcode opcode,
                                    uint32_t dest, uintptr_t src1,
                                    uint32_t src2, uint32_t other);
#ifdef __GNUC__
__attribute__((noinline))
#endif
static int rtl_add_insn_with_new_unit(RTLBlock *block, RTLOpcode opcode,
                                      uint32_t dest, uintptr_t src1,
                                      uint32_t src2, uint32_t other);

/*----------------------------------*/

/**
 * rtl_add_insn:  Append an instruction to the given block.  The meaning of
 * each operand depends on the instruction.
 *
 * [Parameters]
 *      block: RTLBlock to append to
 *     opcode: Instruction opcode (RTLOP_*)
 *       dest: Destination register for instruction
 *       src1: First source register or immediate value for instruction
 *       src2: Second source register or immediate value for instruction
 *      other: Extra register for instruction
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtl_add_insn(RTLBlock *block, RTLOpcode opcode, uint32_t dest,
                 uintptr_t src1, uint32_t src2, uint32_t other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(!block->finalized, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(opcode >= RTLOP__FIRST && opcode <= RTLOP__LAST, return 0);

    /* Extend the instruction array if necessary */
    if (UNLIKELY(block->num_insns >= block->insns_size)) {
        return rtl_add_insn_with_extend(block, opcode,
                                        dest, src1, src2, other);
    }

    /* Create a new basic unit if there's no active one */
    if (UNLIKELY(!block->have_unit)) {
        return rtl_add_insn_with_new_unit(block, opcode,
                                          dest, src1, src2, other);
    }

    /* Fill in the instruction data */
    RTLInsn * const insn = &block->insns[block->num_insns];
    insn->opcode = opcode;
    if (UNLIKELY(!rtlinsn_make(block, insn, dest, src1, src2, other))) {
        return 0;
    }

#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] %p/%5u: %s\n", block, block->num_insns,
            rtl_decode_insn(block, block->num_insns, 0));
#endif
    block->num_insns++;
    return 1;
}

/*----------------------------------*/

/**
 * rtl_add_insn_with_extend:  Extend the instruction array and then add a
 * new instruction.  Called by rtl_add_insn() when the instruction array is
 * full upon entry.
 */
static int rtl_add_insn_with_extend(RTLBlock *block, RTLOpcode opcode,
                                    uint32_t dest, uintptr_t src1,
                                    uint32_t src2, uint32_t other)
{
    uint32_t new_insns_size = block->num_insns + INSNS_EXPAND_SIZE;
    RTLInsn *new_insns = realloc(block->insns,
                                 sizeof(*block->insns) * new_insns_size);
    if (UNLIKELY(!new_insns)) {
        DMSG("No memory to expand block %p to %d insns", block,
             new_insns_size);
        return 0;
    }
    block->insns = new_insns;
    block->insns_size = new_insns_size;

    /* Run back through rtl_add_insn() to handle the rest */
    return rtl_add_insn(block, opcode, dest, src1, src2, other);
}

/*----------------------------------*/

/**
 * rtl_add_insn_with_new_unit:  Start a new basic unit and then add a new
 * instruction.  Called by rtl_add_insn() when there is no current basic
 * unit upon entry.
 */
static int rtl_add_insn_with_new_unit(RTLBlock *block, RTLOpcode opcode,
                                      uint32_t dest, uintptr_t src1,
                                      uint32_t src2, uint32_t other)
{
    if (UNLIKELY(!rtlunit_add(block))) {
        return 0;
    }
    block->have_unit = 1;
    block->cur_unit = block->num_units - 1;
    block->units[block->cur_unit].first_insn = block->num_insns;

    /* Run back through rtl_add_insn() to handle the rest */
    return rtl_add_insn(block, opcode, dest, src1, src2, other);
}

/*-----------------------------------------------------------------------*/

/**
 * rtl_alloc_register:  Allocate a new register for use in the given block.
 * The register's value is undefined until it has been used as the
 * destination of an instruction.
 *
 * [Parameters]
 *     block: RTLBlock to allocate a register for
 * [Return value]
 *     Register number (nonzero) on success, zero on error
 */
unsigned int rtl_alloc_register(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(!block->finalized, return 0);
    PRECOND(block->regs != NULL, return 0);

    if (UNLIKELY(block->next_reg >= block->regs_size)) {
        if (block->regs_size >= REGS_LIMIT) {
            DMSG("Too many registers in block %p (limit %u)",
                 block, REGS_LIMIT);
            return 0;
        }
        unsigned int new_regs_size;
        /* Avoid 16-bit overflow (not that there are any modern machines
         * where int is 16 bits, but let's follow the rules anyway) */
        if (block->regs_size > REGS_LIMIT - REGS_EXPAND_SIZE) {
            new_regs_size = REGS_LIMIT;
        } else {
            new_regs_size = block->next_reg + REGS_EXPAND_SIZE;
        }
        RTLRegister * const new_regs =
            realloc(block->regs, sizeof(*block->regs) * new_regs_size);
        if (UNLIKELY(!new_regs)) {
            DMSG("No memory to expand block %p to %d registers",
                 block, new_regs_size);
            return 0;
        }
        block->regs = new_regs;
        block->regs_size = new_regs_size;
    }

    const unsigned int reg_index = block->next_reg++;
    memset(&block->regs[reg_index], 0, sizeof(block->regs[reg_index]));
    return reg_index;
}

/*-----------------------------------------------------------------------*/

/**
 * rtl_register_set_unique_pointer:  Mark the given register as being a
 * "unique pointer", which points to a region of memory which will never
 * be accessed except through this register (or another register copied
 * from it).  This function must be called after adding the instruction
 * which sets the register, and if the register's value is subsequently
 * modified, its "unique pointer" status will be cancelled.
 *
 * [Parameters]
 *      block: RTLBlock containing register to mark
 *     regnum: Register number to mark
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtl_register_set_unique_pointer(RTLBlock *block, uint32_t regnum)
{
    PRECOND(block != NULL, return 0);
    PRECOND(!block->finalized, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(regnum != 0 && regnum < block->next_reg, return 0);

    if (block->unique_pointer_index == 0) {  // i.e. it wrapped around
        DMSG("Unique pointer index overflow at register r%u", regnum);
        return 0;
    }
    block->regs[regnum].unique_pointer = block->unique_pointer_index++;
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * rtl_alloc_label:  Allocate a new label for use in the given block.
 *
 * [Parameters]
 *     block: RTLBlock to allocate a label for
 * [Return value]
 *     Label number (nonzero) on success, zero on error
 */
unsigned int rtl_alloc_label(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(!block->finalized, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    if (UNLIKELY(block->next_label >= block->labels_size)) {
        if (block->labels_size >= LABELS_LIMIT) {
            DMSG("Too many labels in block %p (limit %u)",
                 block, LABELS_LIMIT);
            return 0;
        }
        unsigned int new_labels_size;
        if (block->labels_size > LABELS_LIMIT - LABELS_EXPAND_SIZE) {
            new_labels_size = LABELS_LIMIT;
        } else {
            new_labels_size = block->next_label + LABELS_EXPAND_SIZE;
        }
        int16_t * const new_label_unitmap =
            realloc(block->label_unitmap,
                    sizeof(*block->label_unitmap) * new_labels_size);
        if (UNLIKELY(!new_label_unitmap)) {
            DMSG("No memory to expand block %p to %d labels", block,
                 new_labels_size);
            return 0;
        }
        block->label_unitmap = new_label_unitmap;
        block->labels_size = new_labels_size;
    }

    const unsigned int label = block->next_label++;
    block->label_unitmap[label] = -1;
    return label;
}

/*************************************************************************/

/**
 * rtl_finalize_block:  Perform housekeeping at the end of the given
 * block's translation.  rtl_add_insn(), rtl_alloc_register(), and
 * rtl_alloc_label() may not be called for a block after calling this
 * function on the block.
 *
 * [Parameters]
 *     block: RTLBlock to finalize
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtl_finalize_block(RTLBlock *block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(!block->finalized, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    /* Terminate the last unit (if there is one) */
    if (block->have_unit) {
        block->units[block->cur_unit].last_insn = block->num_insns - 1;
        block->have_unit = 0;
    }

    /* Add execution graph edges for GOTO instructions */
    if (UNLIKELY(!add_unit_edges(block))) {
        return 0;
    }

    /* Update live ranges for registers used in loops */
    if (UNLIKELY(!update_live_ranges(block))) {
        return 0;
    }

    block->finalized = 1;
#ifdef RTL_TRACE_GENERATE
    rtlunit_dump_all(block, NULL);
    fprintf(stderr, "[RTL] Finalized block at %p\n", block);
#endif
    return 1;
}

/*************************************************************************/

/**
 * rtl_optimize_block:  Perform target-independent optimization on the
 * given block.  Before calling this function, rtl_finalize_block() must be
 * called for the block.
 *
 * [Parameters]
 *     block: RTLBlock to optimize
 *     flags: RTLOPT_* flags indicating which optimizations to perform
 * [Return value]
 *     Nonzero on success, zero on error
 */
int rtl_optimize_block(RTLBlock *block, uint32_t flags)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->finalized, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    if (flags & RTLOPT_FOLD_CONSTANTS) {
        if (UNLIKELY(!rtlopt_fold_constants(block))) {
            DMSG("Constant folding failed");
            return 0;
        }
    }

    if (flags & RTLOPT_DECONDITION) {
        if (UNLIKELY(!rtlopt_decondition(block))) {
            DMSG("Deconditioning failed");
            return 0;
        }
    }

    if (flags & RTLOPT_DECONDITION) {
        if (UNLIKELY(!rtlopt_drop_dead_units(block))) {
            DMSG("Dead unit dropping failed");
            return 0;
        }
    }

#ifdef RTL_TRACE_GENERATE
    fprintf(stderr, "[RTL] Optimized block at %p\n", block);
    dump_block(block, "optimize");
#endif
    return 1;
}

/*************************************************************************/

/**
 * rtl_translate_block:  Translate the given block into native machine code.
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
int rtl_translate_block(RTLBlock *block, void **code_ret, uint32_t *size_ret)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->finalized, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(code_ret != NULL, return 0);
    PRECOND(size_ret != NULL, return 0);

    int retval;

#if defined(PSP)
    retval = rtl_translate_block_mips(block, code_ret, size_ret);
#else
    DMSG("Native code generation is not implemented for this platform");
    retval = 0;
#endif

#ifdef RTL_TRACE_GENERATE
    if (retval) {
        fprintf(stderr, "[RTL] Translated block at %p to native code at %p"
                " (size %u)\n", block, *code_ret, *size_ret);
    } else {
        fprintf(stderr, "[RTL] FAILED to translate block at %p\n", block);
    }
#endif
    return retval;
}

/*************************************************************************/

/**
 * rtl_destroy_block:  Destroy the given block, freeing any resources it
 * used.
 *
 * [Parameters]
 *     block: RTLBlock to destroy (if NULL, this function does nothing)
 * [Return value]
 *     None
 */
void rtl_destroy_block(RTLBlock *block)
{
    if (block) {
#ifdef RTL_TRACE_GENERATE
        fprintf(stderr, "[RTL] Destroying block at %p\n", block);
#endif
        free(block->insns);
        free(block->insn_unitmap);
        free(block->units);
        free(block->regs);
        free(block->label_unitmap);
        free(block);
    }
}

/*************************************************************************/
/*********************** Library-internal routines ***********************/
/*************************************************************************/

#if defined(RTL_TRACE_GENERATE) || defined(RTL_TRACE_EXECUTE)

/**
 * rtl_decode_insn:  Decode an RTL instruction into a human-readable
 * string.
 *
 * [Parameters]
 *       block: RTLBlock containing instruction to decode
 *       index: Index of instruction to decode
 *     is_exec: Nonzero if being called from interpreted execution, else zero
 * [Return value]
 *     Human-readable string describing the instruction
 * [Notes]
 *     The returned string is stored in a static buffer which is
 *     overwritten on each call.
 */
const char *rtl_decode_insn(const RTLBlock *block, uint32_t index, int is_exec)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    /* We don't check the index because it'll be out of range when tracing
     * code generation */

    static const char * const opcode_names[] = {
        [RTLOP_NOP        ] = "NOP",
        [RTLOP_MOVE       ] = "MOVE",
        [RTLOP_SELECT     ] = "SELECT",
        [RTLOP_ADD        ] = "ADD",
        [RTLOP_SUB        ] = "SUB",
        [RTLOP_MULU       ] = "MULU",
        [RTLOP_MULS       ] = "MULS",
        [RTLOP_MADDU      ] = "MADDU",
        [RTLOP_MADDS      ] = "MADDS",
        [RTLOP_DIVMODU    ] = "DIVMODU",
        [RTLOP_DIVMODS    ] = "DIVMODS",
        [RTLOP_AND        ] = "AND",
        [RTLOP_OR         ] = "OR",
        [RTLOP_XOR        ] = "XOR",
        [RTLOP_NOT        ] = "NOT",
        [RTLOP_SLL        ] = "SLL",
        [RTLOP_SRL        ] = "SRL",
        [RTLOP_SRA        ] = "SRA",
        [RTLOP_ROR        ] = "ROR",
        [RTLOP_CLZ        ] = "CLZ",
        [RTLOP_CLO        ] = "CLO",
        [RTLOP_SLTU       ] = "SLTU",
        [RTLOP_SLTS       ] = "SLTS",
        [RTLOP_BSWAPH     ] = "BSWAPH",
        [RTLOP_BSWAPW     ] = "BSWAPW",
        [RTLOP_HSWAPW     ] = "HSWAPW",
        [RTLOP_ADDI       ] = "ADDI",
        [RTLOP_ANDI       ] = "ANDI",
        [RTLOP_ORI        ] = "ORI",
        [RTLOP_XORI       ] = "XORI",
        [RTLOP_SLLI       ] = "SLLI",
        [RTLOP_SRLI       ] = "SRLI",
        [RTLOP_SRAI       ] = "SRAI",
        [RTLOP_RORI       ] = "RORI",
        [RTLOP_SLTUI      ] = "SLTUI",
        [RTLOP_SLTSI      ] = "SLTSI",
        [RTLOP_BFEXT      ] = "BFEXT",
        [RTLOP_BFINS      ] = "BFINS",
        [RTLOP_LOAD_IMM   ] = "LOAD_IMM",
        [RTLOP_LOAD_ADDR  ] = "LOAD_ADDR",
        [RTLOP_LOAD_PARAM ] = "LOAD_PARAM",
        [RTLOP_LOAD_BS    ] = "LOAD_BS",
        [RTLOP_LOAD_BU    ] = "LOAD_BU",
        [RTLOP_LOAD_HS    ] = "LOAD_HS",
        [RTLOP_LOAD_HU    ] = "LOAD_HU",
        [RTLOP_LOAD_W     ] = "LOAD_W",
        [RTLOP_LOAD_PTR   ] = "LOAD_PTR",
        [RTLOP_STORE_B    ] = "STORE_B",
        [RTLOP_STORE_H    ] = "STORE_H",
        [RTLOP_STORE_W    ] = "STORE_W",
        [RTLOP_STORE_PTR  ] = "STORE_PTR",
        [RTLOP_LABEL      ] = "LABEL",
        [RTLOP_GOTO       ] = "GOTO",
        [RTLOP_GOTO_IF_Z  ] = "GOTO_IF_Z",
        [RTLOP_GOTO_IF_NZ ] = "GOTO_IF_NZ",
        [RTLOP_GOTO_IF_E  ] = "GOTO_IF_E",
        [RTLOP_GOTO_IF_NE ] = "GOTO_IF_NE",
        [RTLOP_CALL       ] = "CALL",
        [RTLOP_RETURN     ] = "RETURN",
        [RTLOP_RETURN_TO  ] = "RETURN_TO",
    };

    static char buf[500];

    const RTLInsn * const insn = &block->insns[index];
    const char * const name = opcode_names[insn->opcode];
    const unsigned int dest = insn->dest;
    const unsigned int src1 = insn->src1;
    const unsigned int src2 = insn->src2;

#define APPEND_REG_DESC(regnum) \
    snprintf(buf + strlen(buf), sizeof(buf) - strlen(buf), "\n    r%u: %s", \
             (regnum), rtl_describe_register(&block->regs[(regnum)], is_exec));

    switch ((RTLOpcode)insn->opcode) {

      case RTLOP_NOP:
        if (insn->src_imm) {
            snprintf(buf, sizeof(buf), "%-11s 0x%X", name, insn->src_imm);
        } else {
            snprintf(buf, sizeof(buf), "%s", name);
        }
        return buf;

      case RTLOP_MOVE:
      case RTLOP_NOT:
      case RTLOP_CLZ:
      case RTLOP_CLO:
      case RTLOP_BSWAPH:
      case RTLOP_BSWAPW:
      case RTLOP_HSWAPW:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u", name, dest, src1);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_SELECT:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u, r%u", name,
                 dest, src1, src2, insn->cond);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        APPEND_REG_DESC(insn->cond);
        return buf;

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
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u", name, dest, src1,
                 src2);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        return buf;

      case RTLOP_ADDI:
      case RTLOP_SLTSI:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, %d", name, dest, src1,
                 insn->src_imm);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_SLLI:
      case RTLOP_SRLI:
      case RTLOP_SRAI:
      case RTLOP_RORI:
      case RTLOP_SLTUI:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, %u", name, dest, src1,
                 insn->src_imm);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_ANDI:
      case RTLOP_ORI:
      case RTLOP_XORI:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, 0x%X", name, dest, src1,
                 insn->src_imm);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_MULU:
      case RTLOP_MULS:
      case RTLOP_DIVMODU:
      case RTLOP_DIVMODS:
        if (dest == 0) {
            if (insn->dest2 == 0) {
                snprintf(buf, sizeof(buf), "%-11s ---, r%u, r%u, ---", name,
                         src1, src2);
            } else {
                snprintf(buf, sizeof(buf), "%-11s ---, r%u, r%u, r%u", name,
                         src1, src2, insn->dest2);
            }
        } else {
            if (insn->dest2 == 0) {
                snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u, ---", name,
                         dest, src1, src2);
            } else {
                snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u, r%u", name,
                         dest, src1, src2, insn->dest2);
            }
        }
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        return buf;

      case RTLOP_MADDU:
      case RTLOP_MADDS:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u, r%u", name,
                 dest, src1, src2, insn->dest2);
        APPEND_REG_DESC(dest);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        APPEND_REG_DESC(insn->dest2);
        return buf;

      case RTLOP_BFEXT:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, %u, %u", name, dest,
                 src1, insn->bitfield.start, insn->bitfield.count);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_BFINS:
        snprintf(buf, sizeof(buf), "%-11s r%u, r%u, r%u, %u, %u", name, dest,
                 src1, src2, insn->bitfield.start, insn->bitfield.count);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        return buf;

      case RTLOP_LOAD_IMM:
        if (insn->src_imm >= 0x10000 && insn->src_imm < 0xFFFF0000) {
            snprintf(buf, sizeof(buf), "%-11s r%u, 0x%X", name, dest,
                     insn->src_imm);
        } else {
            snprintf(buf, sizeof(buf), "%-11s r%u, %d", name, dest,
                     (int32_t)insn->src_imm);
        }
        return buf;

      case RTLOP_LOAD_ADDR:
        snprintf(buf, sizeof(buf), "%-11s r%u, 0x%lX", name, dest,
                 (unsigned long)insn->src_addr);
        return buf;

      case RTLOP_LOAD_PARAM:
        snprintf(buf, sizeof(buf), "%-11s r%u, params[%u]", name, dest,
                 insn->src_imm);
        return buf;

      case RTLOP_LOAD_BU:
      case RTLOP_LOAD_BS:
      case RTLOP_LOAD_HU:
      case RTLOP_LOAD_HS:
      case RTLOP_LOAD_W:
      case RTLOP_LOAD_PTR:
        snprintf(buf, sizeof(buf), "%-11s r%u, %d(r%u)", name, dest,
                 insn->offset, src1);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_STORE_B:
      case RTLOP_STORE_H:
      case RTLOP_STORE_W:
      case RTLOP_STORE_PTR:
        snprintf(buf, sizeof(buf), "%-11s %d(r%u), r%u", name,
                 insn->offset, dest, src1);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(dest);
        return buf;

      case RTLOP_LABEL:
      case RTLOP_GOTO:
        snprintf(buf, sizeof(buf), "%-11s L%u", name, insn->label);
        return buf;

      case RTLOP_GOTO_IF_Z:
      case RTLOP_GOTO_IF_NZ:
        snprintf(buf, sizeof(buf), "%-11s L%u, r%u", name, insn->label, src1);
        APPEND_REG_DESC(src1);
        return buf;

      case RTLOP_GOTO_IF_E:
      case RTLOP_GOTO_IF_NE:
        snprintf(buf, sizeof(buf), "%-11s L%u, r%u, r%u", name, insn->label,
                 src1, src2);
        APPEND_REG_DESC(src1);
        APPEND_REG_DESC(src2);
        return buf;

      case RTLOP_CALL:
        if (insn->dest) {
            int len = snprintf(buf, sizeof(buf), "%-11s r%u = r%u(",
                               name, dest, insn->target);
            if (src1) {
                len += snprintf(buf+len, sizeof(buf)-len, "r%u", src1);
                if (src2) {
                    len += snprintf(buf+len, sizeof(buf)-len, ", r%u", src2);
                }
            }
            snprintf(buf+len, sizeof(buf)-len, ")");
        } else {
            int len = snprintf(buf, sizeof(buf), "%-11s r%u(",
                               name, insn->target);
            if (src1) {
                len += snprintf(buf+len, sizeof(buf)-len, "r%u", src1);
                if (src2) {
                    len += snprintf(buf+len, sizeof(buf)-len, ", r%u", src2);
                }
            }
            snprintf(buf+len, sizeof(buf)-len, ")");
        }
        if (src1) {
            APPEND_REG_DESC(src1);
            if (src2) {
                APPEND_REG_DESC(src2);
            }
        }
        APPEND_REG_DESC(insn->target);
        return buf;

      case RTLOP_RETURN:
        snprintf(buf, sizeof(buf), "%-11s", name);
        return buf;

      case RTLOP_RETURN_TO:
        snprintf(buf, sizeof(buf), "%-11s r%u", name, insn->target);
        APPEND_REG_DESC(insn->target);
        return buf;

    }  // switch (insn->opcode)

    snprintf(buf, sizeof(buf), "???");
    return buf;

#undef APPEND_REG_DESC
}

/*-----------------------------------------------------------------------*/

/**
 * rtl_describe_register:  Generate a string describing the contents of the
 * given RTL register.
 *
 * [Parameters]
 *         reg: Register to describe
 *     is_exec: Nonzero if being called from interpreted execution, else zero
 * [Return value]
 *     Human-readable string describing the register
 * [Notes]
 *     The returned string is stored in a static buffer which is
 *     overwritten on each call.
 */
const char *rtl_describe_register(const RTLRegister *reg, int is_exec)
{
    PRECOND(reg != NULL, return "");

    static char buf[100];
    if (is_exec || reg->source == RTLREG_CONSTANT) {
        if ((intptr_t)reg->value >= 0x10000
         || (intptr_t)reg->value < -0x10000
        ) {
            snprintf(buf, sizeof(buf), "0x%lX", (unsigned long)reg->value);
        } else {
            snprintf(buf, sizeof(buf), "%d", (int32_t)reg->value);
        }
    } else if (reg->source == RTLREG_PARAMETER) {
        snprintf(buf, sizeof(buf), "param[%u]", reg->param_index);
    } else if (reg->source == RTLREG_MEMORY) {
        snprintf(buf, sizeof(buf), "(%ssigned) @(%d,r%u).%s",
                 reg->memory.is_signed ? "" : "un", reg->memory.offset,
                 reg->memory.addr_reg,
                 reg->memory.size==1 ? "b" :
                 reg->memory.size==2 ? "w" :
                 reg->memory.size==4 ? "l" : "ptr");
    } else if (reg->source == RTLREG_RESULT
               || reg->source == RTLREG_RESULT_NOFOLD) {
        static const char * const operators[] = {
            [RTLOP_ADD   ] = "+",
            [RTLOP_SUB   ] = "-",
            [RTLOP_AND   ] = "&",
            [RTLOP_OR    ] = "|",
            [RTLOP_XOR   ] = "^",
            [RTLOP_SLL   ] = "<<",
            [RTLOP_SRL   ] = ">>",
            [RTLOP_SRA   ] = ">>",
            [RTLOP_ROR   ] = "ROR",
            [RTLOP_CLZ   ] = "CLZ",
            [RTLOP_CLO   ] = "CLO",
            [RTLOP_SLTU  ] = "<",
            [RTLOP_SLTS  ] = "<",
            [RTLOP_BSWAPH] = "BSWAPH",
            [RTLOP_BSWAPW] = "BSWAPW",
            [RTLOP_HSWAPW] = "HSWAPW",
            [RTLOP_ADDI  ] = "+",
            [RTLOP_ANDI  ] = "&",
            [RTLOP_ORI   ] = "|",
            [RTLOP_XORI  ] = "^",
            [RTLOP_SLLI  ] = "<<",
            [RTLOP_SRLI  ] = ">>",
            [RTLOP_SRAI  ] = ">>",
            [RTLOP_RORI  ] = "ROR",
            [RTLOP_SLTUI ] = "<",
            [RTLOP_SLTSI ] = "<",
        };
        // 0x01: immediate operand is signed, 0x02: display "(signed)"
        static const uint8_t is_signed[] = {
            [RTLOP_SRA  ] = 2,
            [RTLOP_SLTS ] = 2,
            [RTLOP_ADDI ] = 1,
            [RTLOP_SRAI ] = 2,
            [RTLOP_SLTSI] = 3,
            [RTLOP_SLTUI] = 1,
        };
        switch (reg->result.opcode) {
          case RTLOP_MOVE:
            snprintf(buf, sizeof(buf), "r%u", reg->result.src1);
            break;
          case RTLOP_SELECT:
            snprintf(buf, sizeof(buf), "r%u ? r%u : r%u", reg->result.cond,
                     reg->result.src1, reg->result.src2);
            break;
          case RTLOP_NOT:
            snprintf(buf, sizeof(buf), "~r%u", reg->result.src1);
            break;
          case RTLOP_CLZ:
          case RTLOP_CLO:
          case RTLOP_BSWAPH:
          case RTLOP_BSWAPW:
          case RTLOP_HSWAPW:
            snprintf(buf, sizeof(buf), "%s(r%u)",
                     operators[reg->result.opcode], reg->result.src1);
            break;
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
            snprintf(buf, sizeof(buf), "%sr%u %s r%u",
                     is_signed[reg->result.opcode] & 2 ? "(signed) " : "",
                     reg->result.src1, operators[reg->result.opcode],
                     reg->result.src2);
            break;
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
            snprintf(buf, sizeof(buf), "%sr%u %s ",
                     is_signed[reg->result.opcode] & 2 ? "(signed) " : "",
                     reg->result.src1, operators[reg->result.opcode]);
            if ((is_signed[reg->result.opcode] & 1)
             && (int32_t)reg->result.imm >= -0x8000
             && (int32_t)reg->result.imm <= 0xFFFF
            ) {
                snprintf(buf + strlen(buf), sizeof(buf) - strlen(buf), "%d",
                         (int)reg->result.imm);
            } else {
                snprintf(buf + strlen(buf), sizeof(buf) - strlen(buf), "0x%X",
                         (int)reg->result.imm);
            }
            break;
          case RTLOP_MULU:
          case RTLOP_MULS:
            snprintf(buf, sizeof(buf), "%s%sr%u * r%u%s",
                     reg->result.opcode == RTLOP_MULS ? "(signed) " : "",
                     reg->result.second_res ? "(" : "",
                     reg->result.src1, reg->result.src2,
                     reg->result.second_res ? ") >> 32" : "");
            break;
          case RTLOP_DIVMODU:
          case RTLOP_DIVMODS:
            snprintf(buf, sizeof(buf), "%sr%u %c r%u",
                     reg->result.opcode == RTLOP_DIVMODS ? "(signed) " : "",
                     reg->result.src1, reg->result.second_res ? '%' : '/',
                     reg->result.src2);
            break;
          case RTLOP_BFEXT:
            snprintf(buf, sizeof(buf), "BFEXT(r%u, %u, %u)",
                     reg->result.src1, reg->result.start, reg->result.count);
            break;
          case RTLOP_BFINS:
            snprintf(buf, sizeof(buf), "BFINS(r%u, r%u, %u, %u)",
                     reg->result.src1, reg->result.src2,
                     reg->result.start, reg->result.count);
            break;
          default:
            snprintf(buf, sizeof(buf), "???");
            break;
        }  // switch (reg->result.opcode)
    } else {
        snprintf(buf, sizeof(buf), "???");
    }
    return buf;
}

#endif  // RTL_TRACE_GENERATE || RTL_TRACE_EXECUTE

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * add_unit_edges:  Add edges between basic units for GOTO instructions.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 * [Notes]
 *     Execution time is O(n) in the number of basic units.
 */
static int add_unit_edges(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);

    unsigned int unit_index;
    for (unit_index = 0; unit_index < block->num_units; unit_index++) {
        RTLUnit * const unit = &block->units[unit_index];
        if (unit->first_insn <= unit->last_insn) {
            const RTLInsn * const insn = &block->insns[unit->last_insn];
            if (insn->opcode == RTLOP_GOTO
             || insn->opcode == RTLOP_GOTO_IF_Z
             || insn->opcode == RTLOP_GOTO_IF_NZ
             || insn->opcode == RTLOP_GOTO_IF_E
             || insn->opcode == RTLOP_GOTO_IF_NE
            ) {
                const unsigned int label = insn->label;
                if (UNLIKELY(block->label_unitmap[label] < 0)) {
                    DMSG("%p/%u: GOTO to unknown label %u", block,
                         unit->last_insn, label);
                    return 0;
                } else if (UNLIKELY(!rtlunit_add_edge(block, unit_index, block->label_unitmap[label]))) {
                    DMSG("%p: Failed to add edge %u->%u for %s L%u", block,
                         unit_index, block->label_unitmap[label],
                         insn->opcode == RTLOP_GOTO ? "GOTO" :
                         insn->opcode == RTLOP_GOTO_IF_Z ? "GOTO_IF_Z" :
                         insn->opcode == RTLOP_GOTO_IF_NZ ? "GOTO_IF_NZ" :
                         insn->opcode == RTLOP_GOTO_IF_E ? "GOTO_IF_E" :
                             "GOTO_IF_NE",
                         label);
                    return 0;
                }
            }
        }
    }

    return 1;
}

/*************************************************************************/

/**
 * update_live_ranges:  Update the live range of any register live at the
 * beginning unit targeted by a backward branch so that the register is
 * live through all branches that target the unit.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 * [Notes]
 *     Worst-case execution time is O(n*m) in the number of units (n) and
 *     the number of registers (m).  However, the register scan is only
 *     required for units targeted by backward branches, and terminates at
 *     the first register born within or after the targeted unit.
 */
static int update_live_ranges(RTLBlock * const block)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);

    int unit_index;
    for (unit_index = 0; unit_index < block->num_units; unit_index++) {
        const RTLUnit * const unit = &block->units[unit_index];
        int latest_entry_unit = -1;
        unsigned int i;
        for (i = 0; i < lenof(unit->entries) && unit->entries[i] >= 0; i++) {
            if (unit->entries[i] > latest_entry_unit) {
                latest_entry_unit = unit->entries[i];
            }
        }
        if (latest_entry_unit >= unit_index
         && block->units[latest_entry_unit].last_insn >= 0  // Just in case
        ) {
            const uint32_t birth_limit = unit->first_insn;
            const uint32_t min_death =
                block->units[latest_entry_unit].last_insn;
            unsigned int reg;
            for (reg = block->first_live_reg;
                 reg != 0 && block->regs[reg].birth < birth_limit;
                 reg = block->regs[reg].live_link
            ) {
                if (block->regs[reg].death >= birth_limit
                 && block->regs[reg].death < min_death
                ) {
                    block->regs[reg].death = min_death;
                }
            }
        }
    }

    return 1;
}

/*************************************************************************/
/*************************************************************************/

#ifdef RTL_TRACE_GENERATE

/**
 * dump_block:  Dump the contents of an RTL block to stderr.
 *
 * [Parameters]
 *     block: RTL block
 *       tag: Tag to prepend to all lines, or NULL for none
 * [Return value]
 *     None
 */
static void dump_block(const RTLBlock * const block, const char * const tag)
{
    PRECOND(block != NULL, return);

    uint32_t insn_index;
    for (insn_index = 0; insn_index < block->num_insns; insn_index++) {
        fprintf(stderr, "[RTL] %s%s%s%p/%5u: %s\n",
                tag ? "[" : "", tag ? tag : "", tag ? "] " : "",
                block, insn_index, rtl_decode_insn(block, insn_index, 0));
    }

    rtlunit_dump_all(block, tag);
}

#endif  // RTL_TRACE_GENERATE

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
