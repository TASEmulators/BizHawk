/*  src/psp/rtl-internal.h: Internal-use declarations for RTL
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

#ifndef RTL_INTERNAL_H
#define RTL_INTERNAL_H

/*************************************************************************/
/************************* Configuration options *************************/
/*************************************************************************/

/*============ General options ============*/

/**
 * INSNS_EXPAND_SIZE:  Specifies the number of instructions by which to
 * expand a block's instruction array when the array is full.  This value
 * is also used for the initial size of the array.
 */
#define INSNS_EXPAND_SIZE  1000

/**
 * UNITS_EXPAND_SIZE:  Specifies the number of instructions by which to
 * expand a block's instruction array when the array is full.  This value
 * is also used for the initial size of the array.
 */
#define UNITS_EXPAND_SIZE  100

/**
 * REGS_EXPAND_SIZE:  Specifies the number of register entries by which to
 * expand a block's register array when the array is full.  This value is
 * also used for the initial size of the array.
 */
#define REGS_EXPAND_SIZE  1000

/**
 * REGS_LIMIT:  Specifies the maximum number of registers allowed for a
 * single block.  Must be no greater than 65535 (because this value must
 * fit into a uint16_t).  The actual number of available registers is one
 * less than this value, since register 0 is never used.
 */
#define REGS_LIMIT  65535

/**
 * LABELS_EXPAND_SIZE:  Specifies the number of entries by which to expand
 * a block's label-to-unit mapping array when the array is full.  This
 * value is also used for the initial size of the array.
 */
#define LABELS_EXPAND_SIZE  100

/**
 * LABELS_LIMIT:  Specifies the maximum number of labels allowed for a
 * single block.  Must be no greater than 65535 (because this value must
 * fit into a uint16_t).  The actual number of available labels is one less
 * than this value, since label 0 is never used.
 */
#define LABELS_LIMIT  65535

/**
 * NATIVE_EXPAND_SIZE:  Specifies the block size (in bytes) by which to
 * expand the native code buffer as necessary when translating.
 */
#define NATIVE_EXPAND_SIZE 8192

/*============ MIPS-specific options ============*/

/**
 * MIPS_FRAME_SIZE:  The stack frame size to use in generated native code,
 * in bytes.  This does not include space reserved for saving registers in
 * the function prologue.
 */
#define MIPS_FRAME_SIZE  64

/**
 * MIPS_OPTIMIZE_MERGE_CONSTANTS:  When defined, RTL registers with
 * identical constant values whose live ranges overlap will share the same
 * hardware register.  Hardware registers will also be reused if a value
 * loaded for a previously-used constant is still available after the
 * constant has died.
 */
#define MIPS_OPTIMIZE_MERGE_CONSTANTS

/**
 * MIPS_OPTIMIZE_IMMEDIATE:  When defined, the translator will optimize a
 * LOAD_IMM instruction followed by one of:
 *    ADD, SUB, AND, OR, XOR, SLL, SRL, SRA, SLTU, SLTS
 * which uses the LOAD_IMM target as its second operand into the equivalent
 * MIPS immediate instruction if the immediate operand is within range and
 * is not used elsewhere.  Similarly, LOAD_NATIVEADDR followed by a memory
 * load or store operation will be optimized if possible to insert the low
 * 16 bits of the address into the load/store instruction, allowing the
 * base register to be loaded with a single MIPS LUI instruction.
 */
#define MIPS_OPTIMIZE_IMMEDIATE

/**
 * MIPS_OPTIMIZE_ABSOLUTE_CALL:  When defined, the translator will optimize
 * a LOAD_NATIVEADDR instruction followed by a CALL_NATIVE instruction into
 * a MIPS JAL instruction to the address specified by the constant register.
 *
 * ==== PORTABILITY WARNING ====
 *
 * This optimization is NOT guaranteed to be portable across different
 * platforms!  The MIPS JAL instruction only allows the low 28 bits of the
 * target address to be specified, and takes the high bits from the current
 * PC.  We could theoretically check the target address against the address
 * of the JAL instruction when we add it, but since the address of the
 * native code block may change as it is expanded, we cannot guarantee at
 * the time we add the JAL instruction that the target address is reachable.
 *
 * That said, on (at least current iterations of) the PSP, the upper bits
 * of addresses for both code and data are always zero, so we can safely
 * optimize jumps to constant addresses.
 */
#define MIPS_OPTIMIZE_ABSOLUTE_CALL

/**
 * MIPS_OPTIMIZE_DELAY_SLOT:  When defined, the translator will move the
 * instruction preceding a branch or jump instruction into the branch's
 * delay slot, if possible.
 */
#define MIPS_OPTIMIZE_DELAY_SLOT

/**
 * MIPS_OPTIMIZE_BRANCHES:  When defined, the translator will perform the
 * following optimizations on branches:
 *
 * - A branch to another (unconditional) branch will be chained through to
 *   the final branch target, unless that target would be outside the range
 *   of a branch instruction.
 *
 * - When a branch has a NOP instruction in its delay slot, the instruction
 *   at the target address will be copied over the NOP, the branch will be
 *   changed to a Likely branch if it is conditional (e.g. BEQ becomes
 *   BEQL), and the branch target will be incremented by one instruction.
 *   (The optimization is not performed if the branch target is already at
 *   the positive branch offset limit.)
 */
#define MIPS_OPTIMIZE_BRANCHES

/**
 * MIPS_OPTIMIZE_SCHEDULING:  When defined, the translator will attempt to
 * reschedule load, multiply, and divide instructions to avoid stalls.
 */
#define MIPS_OPTIMIZE_SCHEDULING

/**
 * MIPS_OPTIMIZE_SEX:  When defined, the translator will optimize SLL/SRA
 * pairs to SEB or SEH when possible:
 *
 *    LOAD_IMM rD,24
 *    SLL rB,rA,rD
 *    LOAD_IMM rE,24
 *    SRA rC,rB,rE    --> seb $rC,$rA  [assuming rB is otherwise unused]
 *
 *    LOAD_IMM rD,16
 *    SLL rB,rA,rD
 *    LOAD_IMM rE,16
 *    SRA rC,rB,rE    --> seh $rC,$rA  [assuming rB is otherwise unused]
 *
 * SLLI/SRAI pairs are similarly optimized.
 */
#define MIPS_OPTIMIZE_SEX

/**
 * MIPS_OPTIMIZE_MIN_MAX:  When defined, the translator will optimize
 * SLTS/SELECT pairs to MIN or MAX when possible:
 *
 *     SLTS rC, rA, rB
 *     SELECT rD, rA, rB, rC --> min $rD, $rA, $rB
 *
 *     SLTS rC, rA, rB
 *     SELECT rD, rB, rA, rC --> max $rD, $rA, $rB
 *
 * (both assuming register rC is otherwise unused).
 */
#define MIPS_OPTIMIZE_MIN_MAX

/*============ Debugging options ============*/

/**
 * OPERAND_SANITY_CHECKS:  If defined, causes rtl_add_insn() to check that
 * register and label operands are within allowable ranges.
 *
 * This option is meaningless if CHECK_PRECONDITIONS is not defined.
 */
#define OPERAND_SANITY_CHECKS

/**
 * CHECK_PRECONDITIONS:  If defined, causes functions to check that their
 * preconditions are satisfied and return (with an error if appropriate)
 * if not.  This can add a significant amount of overhead.
 */
// #define CHECK_PRECONDITIONS

/**
 * RTL_TRACE_GENERATE:  Trace the generation of RTL blocks and instructions.
 */
// #define RTL_TRACE_GENERATE

/**
 * RTL_TRACE_EXECUTE:  Trace the execution of RTL instructions in
 * rtl_execute_block().
 */
// #define RTL_TRACE_EXECUTE

/**
 * RTL_TRACE_STEALTH_FOR_SH2:  Enable SH-2 stealth tracing (see the
 * documentation for TRACE_STEALTH in sh2.c).
 */
#define RTL_TRACE_STEALTH_FOR_SH2

/*************************************************************************/
/*************************** Type declarations ***************************/
/*************************************************************************/

#undef mips  // Avoid namespace pollution from the compiler on MIPS machines

/*-----------------------------------------------------------------------*/

/**
 * RTLInsn:  A single platform-neutral (more or less) operation.  SH-2
 * instructions are translated into sequences of RTLInsns, which are then
 * optimized and retranslated into MIPS instructions.
 */
typedef struct RTLInsn_ {
    uint8_t opcode;           // Operation code (RTLOpcode)
    uint16_t dest;            // Destination register
    uint16_t src1, src2;      // Source registers
    union {
        uint16_t dest2;       // Second output register (for MULU_64, etc.)
        uint16_t cond;        // Condition register for SELECT
        struct {
            uint8_t start;    // First (lowest) bit number for a bitfield
            uint8_t count;    // Number of bits for a bitfield
        } bitfield;
        int16_t offset;       // Byte offset for load/store instructions
        uint16_t label;       // GOTO target label
        uint16_t target;      // CALL_NATIVE branch target register
        uint32_t src_imm;     // Source immediate value
        uintptr_t src_addr;   // Source native address value
    };
} RTLInsn;

/*----------------------------------*/

/**
 * RTLRegType:  The type (source information) of a register used in an RTL
 * block.
 */
typedef enum RTLRegType_ {
    RTLREG_UNDEFINED = 0,   // Not yet defined to anything
    RTLREG_CONSTANT,        // Constant value (RTLRegister.value)
    RTLREG_PARAMETER,       // Function parameter (.param_index)
    RTLREG_MEMORY,          // Memory reference
    RTLREG_RESULT,          // Result of an operation on other registers
    RTLREG_RESULT_NOFOLD,   // Result of an operation (not constant foldable)
    RTLREG_UNKNOWN,         // Source unknown (e.g. due to reassignment)
} RTLRegType;

/**
 * RTLRegister:  Data about registers used in an RTL block.  All registers
 * are 32 bits wide.
 */
typedef struct RTLRegister_ RTLRegister;
struct RTLRegister_ {
    /* Basic register information */
    uint8_t source;             // Register source (RTLRegType)
    uint8_t live;               // Nonzero if this register has been referenced
                                //    (this field is never cleared once set)
    uint16_t live_link;         // Next register in live list (sorted by birth)
    uint32_t birth;             // First RTL insn index when register is live
                                //    (if SSA, insn index where it's assigned)
    uint32_t death;             // Last RTL insn index when register is live

    /* Unique pointer information.  The "unique_pointer" field has the
     * property that all registers with the same nonzero value for
     * "unique_pointer" are native addresses which point to the same region
     * of memory, and that region of memory will only be accessed through
     * a register with the same "unique_pointer" value. */
    uint16_t unique_pointer;

    /* Register value information */
    union {
        uintptr_t value;        // Value of register for RTLREG_CONSTANT;
                                //    also used during interpreted execution
        unsigned int param_index;// Function parameter idx for RTLREG_PARAMETER
        struct {
            uint16_t addr_reg;  // Register holding address for RTLREG_MEMORY
            int16_t offset;     // Access offset
            uint8_t size;       // Access size in bytes (1, 2, 4; or 8 if a
                                //    pointer, regardless of actual size)
            uint8_t is_signed;  // Nonzero if a signed load, zero if unsigned
        } memory;
        struct {
            uint8_t opcode;     // Operation code for RTLREG_RESULT
            uint8_t second_res:1; // "Second result" flag (high word of
                                  //    MUL[US]_64, remainder of DIVMOD[US])
            uint8_t is_imm:1;   // Nonzero if a register-immediate operation
            uint16_t src1;      // Operand 1
            union {
                struct {
                    uint16_t src2;  // Op 2 for register-register operations
                    union {
                        uint16_t cond;      // Condition register for SELECT
                        struct {
                            uint8_t start;  // Start bit for bitfields
                            uint8_t count;  // Bit count for bitfields
                        };
                    };
                };
                uint32_t imm;   // Operand 2 for register-immediate operations
            };
        } result;
    };

    /* The following fields are for use by RTL-to-native translators: */
    uint32_t last_used;         // Last insn index where this register was used
    uint8_t native_allocated;   // Nonzero if a native reg has been allocated
    uint8_t native_reg;         // Native register allocated for this register
    uint8_t frame_allocated;    // Nonzero if a frame slot has been allocated
    uint8_t frame_slot;         // Frame slot allocated for this register
    int16_t stack_offset;       // Stack offset of this register's frame slot
    RTLRegister *next_merged;   // Next register in merge chain, or NULL
    union {
        struct {
            /* If nonzero, this field contains the opcode to retrieve the
             * register's value from the MIPS HI or LO register (either
             * MIPS_MFHI(0) or MIPS_MFLO(0)) */
            uint32_t is_in_hilo;
        } mips;
    };
};

/*----------------------------------*/

/**
 * RTLUnit:  Information about an basic unit of code (a sequence of
 * instructions with one entry point and one exit point).  Note that a unit
 * can be empty, denoted by last_insn < first_insn, and that last_insn can
 * be negative, if first_insn is 0 and the unit is empty.
 */
typedef struct RTLUnit_ {
    int32_t first_insn;         // block->insns[] index of first insn in unit
    int32_t last_insn;          // block->insns[] index of last insn in unit
    int16_t next_unit;          // block->units[] index of next unit in code
                                //    stream (may not be the sequentially next
                                //    unit in the array due to optimization);
                                //    -1 indicates the end of the code stream
    int16_t prev_unit;          // block->units[] index of previous unit in
                                //    code stream
    int16_t entries[8];         // block->units[] indices of dominating units;
                                //    -1 indicates an unused slot.  Holes in
                                //    the list are not permitted; for more
                                //    than 8 slots, add a dummy unit on top
                                //    (rtlunit_*() functions handle all this)
    int16_t exits[2];           // block->units[] indices of postdominating
                                //    units.  A terminating insn can go at
                                //    most two places (conditional GOTO).

    /* These fields are provided as hints to RTL-to-native translators: */
    int16_t next_call_unit;     // Next unit with a CALL_NATIVE insn (-1=none)
    int16_t prev_call_unit;     // Prev. unit with a CALL_NATIVE insn (-1=none)

    /* The following fields are used only by RTL-to-native translators: */
    union {
        struct {
            /* Register and stack frame state at the beginning of the unit */
            RTLRegister *reg_map[32];   // MIPS-to-RTL register map
            RTLRegister *frame_map[MIPS_FRAME_SIZE/4];  // Stack frame reg map
        } mips;
    };
} RTLUnit;

/*----------------------------------*/

/**
 * RTLBlock:  State information used in translating a block of code.  The
 * RTLBlock type itself is defined in rtl.h.
 */
struct RTLBlock_ {
    RTLInsn *insns;             // Instruction array
    int16_t *insn_unitmap;      // Insn-to-unit mapping (used by interpreter)
    uint32_t insns_size;        // Size of instruction array (entries)
    uint32_t num_insns;         // Number of instructions actually in array

    RTLUnit *units;             // Basic unit array
    uint16_t units_size;        // Size of unit array (entries)
    uint16_t num_units;         // Number of units actually in array
    uint8_t have_unit;          // Nonzero if there is a currently active unit
    uint16_t cur_unit;          // Current unit index if have_unit != 0

    int16_t *label_unitmap;     // Label-to-unit-index mapping (-1 = unset)
    uint16_t labels_size;       // Size of label-to-unit map array (entries)
    uint16_t next_label;        // Next label number to allocate

    RTLRegister *regs;          // Register array
    uint16_t regs_size;         // Size of register array (entries)
    uint16_t next_reg;          // Next register number to allocate
    uint16_t first_live_reg;    // First register in live range list
    uint16_t last_live_reg;     // Last register in live range list
    uint16_t unique_pointer_index; // Next value for RTLRegister.unique_pointer

    uint8_t finalized;          // Nonzero if block has been finalized

    /* These fields are provided as hints to RTL-to-native translators: */
    int16_t first_call_unit;    // First unit with a CALL_NATIVE insn (-1=none)
    int16_t last_call_unit;     // Last unit with a CALL_NATIVE insn (-1=none)

    /* The following fields are used only by optimization routines: */
    uint8_t *unit_seen;         // Array of "seen" flags for all units
                                //    (used by rtlopt_drop_dead_units())

    /* The following fields are used only by RTL-to-native translators: */
    void *native_buffer;        // Native code buffer
    uint32_t native_bufsize;    // Allocated size of native code buffer
    uint32_t native_length;     // Length of native code
    uint32_t *label_offsets;    // Array of native offsets for labels
    union {
        struct {
            uint8_t need_save_ra;       // Nonzero = need to save/restore $ra
            uint8_t need_chain_at;      // Nonzero = need chain-to-$at epilogue
            uint8_t frame_used;         // Nonzero = 1 or more frame slots used
            uint32_t sreg_used;         // Bitmask of $sN registers used
            uint32_t total_frame_size;  // Frame size incl. space for $sN/$ra
            RTLRegister *reg_map[32];   // MIPS-to-RTL register map
            uint32_t reg_free;          // Bitmask of free MIPS registers
            uint32_t hi_reg, lo_reg;    // RTL registers cached in HI and LO
            RTLRegister *frame_map[MIPS_FRAME_SIZE/4];  // Stack frame reg map
            uint32_t frame_free[((MIPS_FRAME_SIZE/4)+31)/32]; // Free slot mask
            uint32_t unit_start;        // Offset of first insn in current unit
            struct {
                uint8_t is_frame;           // 0 = MIPS reg, 1 = frame slot
                uint8_t index;              // Register or frame slot index
                int16_t next;               // Next entry index or -1 for EOL
            } active_list[32 + MIPS_FRAME_SIZE/4];
            int first_active;           // First active entry, or -1 if none
            int16_t next_call_unit;     // Current position in call_unit chain
            uint32_t epilogue_offset;   // Start offset of epilogue code
            uint32_t chain_offset;      // Start offset of chain epilogue code
                                        //    (only valid if need_chain_at!=0)
        } mips;
    };

#ifdef RTL_TRACE_STEALTH_FOR_SH2
    uint32_t sh2_regcache[23];  // Cached values of SH-2 registers
    uint32_t sh2_regcache_mask; // Bitmask of cached registers
#endif
};

/*************************************************************************/
/***************************** Miscellaneous *****************************/
/*************************************************************************/

/* We use the PRECOND() macro from common.h for precondition checking; if
 * CHECK_PRECONDITIONS is _not_ defined, then redefine PRECOND() here to do
 * nothing. */

#ifndef CHECK_PRECONDITIONS
# undef PRECOND
# define PRECOND(condition,fail_action)  /*nothing*/
#endif

/*************************************************************************/
/**************** Library-internal function declarations *****************/
/*************************************************************************/

/**** Instruction encoding function declarations ****/

/* Internal table used by rtlinsn_make() */
extern int (* const makefunc_table[])(RTLBlock *, RTLInsn *, unsigned int,
                                      uintptr_t, uint32_t, uint32_t);

/**
 * rtlinsn_make:  Fill in an RTLInsn structure based on the opcode stored
 * in the structure and the parameters passed to the function.
 *
 * [Parameters]
 *     block: RTLBlock containing instruction
 *      insn: RTLInsn structure to fill in (insn->opcode must be set by caller)
 *      dest: Destination register for instruction
 *      src1: First source register or immediate value for instruction
 *      src2: Second source register or immediate value for instruction
 *     other: Extra register for instruction
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int rtlinsn_make(RTLBlock *block, RTLInsn *insn,
                               unsigned int dest, uintptr_t src1,
                               uint32_t src2, unsigned int other)
{
    PRECOND(block != NULL, return 0);
    PRECOND(block->insns != NULL, return 0);
    PRECOND(block->units != NULL, return 0);
    PRECOND(block->regs != NULL, return 0);
    PRECOND(block->label_unitmap != NULL, return 0);
    PRECOND(insn->opcode >= RTLOP__FIRST && insn->opcode <= RTLOP__LAST,
            return 0);

    /* Keep this check out of PRECOND() to try and avoid crashes even when
     * CHECK_PRECONDITIONS is disabled; also, invert the sense and call the
     * table function first so the parameter registers or stack frame
     * aren't spilled by the DMSG() call in debug mode. */
    if (LIKELY(makefunc_table[insn->opcode])) {
        return (*makefunc_table[insn->opcode])(block, insn,
                                               dest, src1, src2, other);
    }
    DMSG("BUG: missing function for opcode %u", insn->opcode);
    return 0;
}


/*-----------------------------------------------------------------------*/

/**** Optimization function declarations ****/

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
extern int rtlopt_fold_constants(RTLBlock *block);

/**
 * rtlopt_decondition:  Perform "deconditioning" of conditional branches
 * with constant conditions.  For "GOTO_IF_Z (GOTO_IF_NZ) label, rN" where
 * rN is type RTLREG_CONSTANT, the instruction is changed to GOTO if the
 * value of rN is zero (nonzero) and changed to NOP otherwise.  As with
 * constant folding, if the condition register is not used anywhere else,
 * the register is eliminated and  the instruction that loaded it is
 * changed to a NOP.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int rtlopt_decondition(RTLBlock *block);

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
extern int rtlopt_drop_dead_units(RTLBlock *block);

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
extern int rtlopt_drop_dead_branches(RTLBlock *block);

/*-----------------------------------------------------------------------*/

/**** Basic unit processing function declarations ****/

/**
 * rtlunit_add:  Add a new, empty basic unit to the given block
 * at the end of the block->units[] array.
 *
 * [Parameters]
 *     block: RTL block
 * [Return value]
 *     Nonzero on success, zero on failure
 */
extern int rtlunit_add(RTLBlock *block);

/**
 * rtlunit_add_edge:  Add a new edge between two basic units.
 *
 * [Parameters]
 *          block: RTL block
 *     from_index: Index of dominating basic unit (in block->units[])
 *       to_index: Index of postdominating basic unit (in block->units[])
 * [Return value]
 *     Nonzero on success, zero on failure
 */
extern int rtlunit_add_edge(RTLBlock *block, unsigned int from_index,
                            unsigned int to_index);

/**
 * rtlunit_remove_edge:  Remove an edge between two basic units.
 *
 * [Parameters]
 *          block: RTL block
 *     from_index: Index of dominating basic unit (in block->units[])
 *     exit_index: Index of exit edge to remove (in units[from_index].exits[])
 * [Return value]
 *     None
 */
extern void rtlunit_remove_edge(RTLBlock *block, const unsigned int from_index,
                                unsigned int exit_index);

/**
 * rtlunit_dump_all:  Dump a list of all basic units in the block to
 * stderr.  Intended for debugging.
 *
 * [Parameters]
 *     block: RTL block
 *       tag: Tag to prepend to all lines, or NULL for none
 * [Return value]
 *     None
 */
extern void rtlunit_dump_all(const RTLBlock * const block, const char * const tag);

/*-----------------------------------------------------------------------*/

/**** Architecture-specific translation function declarations ****/

/**
 * rtl_translate_block_XXX:  Translate the given block into native code for
 * a particular architecture.
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
extern int rtl_translate_block_mips(RTLBlock *block, void **code_ret,
                                    uint32_t *size_ret);

/*-----------------------------------------------------------------------*/

/**** Debugging-related functions ****/

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
extern const char *rtl_decode_insn(const RTLBlock *block, uint32_t index, int is_exec);

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
extern const char *rtl_describe_register(const RTLRegister *reg, int is_exec);

#endif  // RTL_TRACE_GENERATE || RTL_TRACE_EXECUTE

/*************************************************************************/
/*************************************************************************/

#endif  // RTL_INTERNAL_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
