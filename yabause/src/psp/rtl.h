/*  src/psp/rtl.h: Declarations for register transfer language used in
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

#ifndef RTL_H
#define RTL_H

/*************************************************************************/

/**
 * RTLBlock:  State information used in translating a block of code.
 * Opaque to callers.
 */
typedef struct RTLBlock_ RTLBlock;

/*-----------------------------------------------------------------------*/

/**
 * RTLOpcode:  Enumeration of operations, used as the value of the
 * RTLInsn.op field.
 */
typedef enum RTLOpcode_ {
    /* Zero is invalid */

    /* No operation */
    RTLOP_NOP = 1,      // [No operation -- note that a value can be given
                        //  in src1 for debugging purposes]

    /* Register-register operations */
    RTLOP_MOVE,         // dest = src1
    RTLOP_SELECT,       // dest = other ? src1 : src2
    RTLOP_ADD,          // dest = src1 + src2
    RTLOP_SUB,          // dest = src1 - src2
    RTLOP_MULU,         // (uint64_t){other,dest} =
                        //    (unsigned)src1 * (unsigned)src2
                        //    [upper 32 bits of result stored in "other",
                        //     lower 32 bits of result stored in "dest";
                        //     either dest or other may be zero = omitted]
    RTLOP_MULS,         // (int64_t){other,dest} = (signed)src1 * (signed)src2
                        //    [either dest or other may be zero = omitted]
    RTLOP_MADDU,        // (uint64_t){other,dest} +=
                        //    (unsigned)src1 * (unsigned)src2
                        //    [other/dest are both inputs and outputs; for
                        //     optimal performance, they should be the same
                        //     registers used as outputs of a MULU/MULS or
                        //     previous MADDU/MADDS insn]
    RTLOP_MADDS,        // (int64_t){other,dest} += (signed)src1 * (signed)src2
    RTLOP_DIVMODU,      // dest = (unsigned)src1 / (unsigned)src2;
                        //    other = (unsigned)src1 % (unsigned)src2
                        //    [both undefined if src2 == 0]
                        //    [either dest or other may be zero = omitted]
    RTLOP_DIVMODS,      // dest = (signed)src1 / (signed)src2;
                        //    other = (signed)src1 % (signed)src2
                        //    [both undefined if src2 == 0]
                        //    [either dest or other may be zero = omitted]
    RTLOP_AND,          // dest = src1 & src2
    RTLOP_OR,           // dest = src1 | src2
    RTLOP_XOR,          // dest = src1 ^ src2
    RTLOP_NOT,          // dest = ~src1
    RTLOP_SLL,          // dest = src1 << src2  [undefined when src2 >= 32]
    RTLOP_SRL,          // dest = (unsigned)src1 >> src2
                        //    [undefined when src2 >= 32]
    RTLOP_SRA,          // dest = (signed)src1 >> src2
                        //    [undefined when src2 >= 32]
    RTLOP_ROR,          // dest = src1 ROR (src2 % 32)  [in 32 bits]
    RTLOP_CLZ,          // dest = [number of leading zeros in src1]
    RTLOP_CLO,          // dest = [number of leading ones in src1]
    RTLOP_SLTU,         // dest = (unsigned)src1 < (unsigned)src2 ? 1 : 0
    RTLOP_SLTS,         // dest = (signed)src1 < (signed)src2 ? 1 : 0
    RTLOP_BSWAPH,       // dest = [swap adjacent pairs of bytes in src1]
    RTLOP_BSWAPW,       // dest = [reverse order of sets of 4 bytes in src1]
    RTLOP_HSWAPW,       // dest = [swap adjacent pairs of halfword in src1]

    /* Register-immediate operations */
    RTLOP_ADDI,         // dest = src1 + IMMEDIATE(src2)
    RTLOP_ANDI,         // dest = src1 & IMMEDIATE(src2)
    RTLOP_ORI,          // dest = src1 | IMMEDIATE(src2)
    RTLOP_XORI,         // dest = src1 ^ IMMEDIATE(src2)
    RTLOP_SLLI,         // dest = src1 << IMMEDIATE(src2)
    RTLOP_SRLI,         // dest = (unsigned)src1 >> IMMEDIATE(src2)
    RTLOP_SRAI,         // dest = (signed)src1 << IMMEDIATE(src2)
    RTLOP_RORI,         // dest = src1 ROR IMMEDIATE(src2)  [in 32 bits]
    RTLOP_SLTUI,        // dest = (unsigned)src1 < (unsigned)IMMEDIATE(src2)
                        //        ? 1 : 0
    RTLOP_SLTSI,        // dest = (signed)src1 < (signed)IMMEDIATE(src2)
                        //        ? 1 : 0

    /* Bitfield operations ("start" and "count" are encoded in the "other"
     * parameter as: other = start | count<<8) */
    RTLOP_BFEXT,        // dest = (src1 >> start) & ((1<<count) - 1)
    RTLOP_BFINS,        // dest = (src1 & ~(((1<<count) - 1) << start))
                        //      | ((src2 & ((1<<count) - 1)) << start)
                        //    [conceptually, "insert src2 into src1"]

    RTLOP_LOAD_IMM,     // dest = IMMEDIATE(src1)  [src1 is a 32-bit immediate]
    RTLOP_LOAD_ADDR,    // dest = IMMEDIATE(src1)  [src1 is a native pointer]
    RTLOP_LOAD_PARAM,   // dest = PARAM(src1)  [src1 is a function param index]

    /* Note: the load/store byte offset ("other") must be in [-32768,+32767] */
    RTLOP_LOAD_BU,      // dest = *(uint8_t *)(src1 + other)
    RTLOP_LOAD_BS,      // dest = *(int8_t *)(src1 + other)
    RTLOP_LOAD_HU,      // dest = *(uint16_t *)(src1 + other)
    RTLOP_LOAD_HS,      // dest = *(int16_t *)(src1 + other)
    RTLOP_LOAD_W,       // dest = *(uint32_t *)(src1 + other)
    RTLOP_LOAD_PTR,     // dest = *(uintptr_t *)(src1 + other)

    RTLOP_STORE_B,      // *(uint8_t *)(dest + other) = src1
    RTLOP_STORE_H,      // *(uint16_t *)(dest + other) = src1
    RTLOP_STORE_W,      // *(uint32_t *)(dest + other) = src1
    RTLOP_STORE_PTR,    // *(uintptr_t *)(dest + other) = src1

    RTLOP_LABEL,        // LABEL(other):  [src1 is a label number]
    RTLOP_GOTO,         // goto LABEL(other)
    RTLOP_GOTO_IF_Z,    // if (src1 == 0) goto LABEL(other)
    RTLOP_GOTO_IF_NZ,   // if (src1 != 0) goto LABEL(other)
    RTLOP_GOTO_IF_E,    // if (src1 == src2) goto LABEL(other)
    RTLOP_GOTO_IF_NE,   // if (src1 != src2) goto LABEL(other)

    RTLOP_CALL,         // result = (*other)(src1, src2)
                        //     [any of the registers result, src1, src2 may
                        //      be zero = omitted, except that src1 must be
                        //      nonzero if src2 is nonzero]
    RTLOP_RETURN,       // return
    RTLOP_RETURN_TO,    // goto other  [execute function epilogue and jump
                        //    to given address instead of returning; other
                        //    is a native address, or RTL block when
                        //    interpreting with rtl_execute_block()]
} RTLOpcode;
#define RTLOP__FIRST  RTLOP_NOP
#define RTLOP__LAST   RTLOP_RETURN_TO

/*-----------------------------------------------------------------------*/

/**
 * RTLOPT_*:  Flags indicating which optimizations should be performed on
 * an RTL block.  Bitwise or'd together and passed as the second parameter
 * to rtl_optimize_block().
 */

/* Perform constant folding, precomputing the results of operations on
 * constant operands and changing the corresponding RTL instructions to
 * LOAD_IMM instructions */
#define RTLOPT_FOLD_CONSTANTS   (1<<0)

/* Convert conditional branches with constant conditions to unconditional
 * branches or NOPs */
#define RTLOPT_DECONDITION      (1<<1)

/* Eliminate unreachable basic units from the code stream */
#define RTLOPT_DROP_DEAD_UNITS  (1<<2)

/* Eliminate branches to the following instruction */
#define RTLOPT_DROP_DEAD_BRANCHES  (1<<3)

/*************************************************************************/

/**
 * rtl_create_block:  Create a new RTLBlock structure for translation.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     New block, or NULL on error
 */
extern RTLBlock *rtl_create_block(void);

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
extern int rtl_add_insn(RTLBlock *block, RTLOpcode opcode, uint32_t dest,
                        uintptr_t src1, uint32_t src2, uint32_t other);

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
extern uint32_t rtl_alloc_register(RTLBlock *block);

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
extern int rtl_register_set_unique_pointer(RTLBlock *block, uint32_t regnum);

/**
 * rtl_alloc_label:  Allocate a new label for use in the given block.
 *
 * [Parameters]
 *     block: RTLBlock to allocate a label for
 * [Return value]
 *     Label number (nonzero) on success, zero on error
 */
extern uint32_t rtl_alloc_label(RTLBlock *block);

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
extern int rtl_finalize_block(RTLBlock *block);

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
extern int rtl_optimize_block(RTLBlock *block, uint32_t flags);

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
extern int rtl_translate_block(RTLBlock *block, void **code_ret, uint32_t *size_ret);

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
extern uint32_t rtl_execute_block(RTLBlock *block, void *state);

/**
 * rtl_destroy_block:  Destroy the given block, freeing any resources it
 * used.
 *
 * [Parameters]
 *     block: RTLBlock to destroy (if NULL, this function does nothing)
 * [Return value]
 *     None
 */
extern void rtl_destroy_block(RTLBlock *block);

/*************************************************************************/

#endif  // RTL_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
