/*  src/psp/sh2.h: SH-2 emulator header
    Copyright 2009-2010 Andrew Church

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

#ifndef SH2_H
#define SH2_H

#ifndef SH2CORE_H
# include "../sh2core.h"
#endif
#ifndef RTL_H
# include "rtl.h"
#endif

/*************************************************************************/

/**** Optimization flags used with sh2_{set,get}_optimizations() ****/

/**
 * SH2_OPTIMIZE_ASSUME_SAFE_DIVISION:  When enabled, optimizes division
 * operations by assuming that quotients in division operations will not
 * overflow and that division by zero will not occur.
 *
 * This optimization will cause the translated code to behave incorrectly
 * if this assumption is violated.
 */
#define SH2_OPTIMIZE_ASSUME_SAFE_DIVISION  (1<<0)

/**
 * SH2_OPTIMIZE_BRANCH_TO_RTS:  When enabled, optimizes static branch
 * instructions (BRA, BT, BF, BT/S, BF/S) which target a "RTS; NOP" pair by
 * performing the RTS operation at the same time as the branch.
 *
 * This optimization may cause the translated code to behave incorrectly
 * or crash the host program if the memory word containing the targeted RTS
 * is modified independently of the branch which targets it.
 *
 * This optimization can cause the timing of cycle count checks to change,
 * and therefore alters trace output in all trace modes.
 */
#define SH2_OPTIMIZE_BRANCH_TO_RTS  (1<<1)

/**
 * SH2_OPTIMIZE_LOCAL_ACCESSES:  When enabled, checks for instructions
 * which modify memory via PC-relative addresses, and bypasses the normal
 * check for overwriting translated blocks on the assumption that such
 * stores will not overwrite program code.
 *
 * This optimization will cause the translated code to behave incorrectly
 * if the above assumption is violated.
 */
#define SH2_OPTIMIZE_LOCAL_ACCESSES  (1<<2)

/**
 * SH2_OPTIMIZE_LOCAL_POINTERS:  When enabled, assumes that pointers
 * loaded via PC-relative addresses will always point to the same region
 * of memory, and bypasses the normal translation from SH-2 addresses to
 * native addresses.
 *
 * This optimization will cause the translated code to behave incorrectly
 * or crash the host program if the above assumption is violated.
 *
 * This optimization depends on both SH2_OPTIMIZE_LOCAL_ACCESSES and
 * SH2_OPTIMIZE_POINTERS, and has no effect if either optimization is
 * disabled.
 */
#define SH2_OPTIMIZE_LOCAL_POINTERS  (1<<3)

/**
 * SH2_OPTIMIZE_MAC_NOSAT:  When enabled, checks the S flag of the SR
 * register when translating any block containing a MAC.W or MAC.L
 * instruction; if the S flag is clear at the beginning of the block and is
 * not modified within the block, the saturation checks are omitted
 * entirely from the generated code.  (In such blocks, a check is added to
 * the beginning of the block to ensure that S is still clear at runtime,
 * and the block is retranslated if S is set.)
 *
 * This optimization will cause the translated code to behave incorrectly
 * if a subroutine or exception called while the block is executing sets
 * the S flag and does not restore it on return.
 */
#define SH2_OPTIMIZE_MAC_NOSAT  (1<<4)

/**
 * SH2_OPTIMIZE_POINTERS:  When enabled, attempts to optimize accesses
 * through pointers to RAM by making the following assumptions:
 *
 * - Any general-purpose register (R0-R15) which points to internal RAM at
 *   the start of a block of code, and which is actually used to access
 *   memory, will continue pointing to a valid RAM address unless and until
 *   the register is the target of a subsequent instruction other than the
 *   immediate-operand forms of ADD, XOR, or OR (since XOR and OR cannot,
 *   and ADD normally will not, affect the high bits of the register's
 *   value).
 *
 * - Accesses through such a pointer register will not run over the end of
 *   the memory region to which the pointer points.  (For example, a store
 *   to @(4,R0) when R0 = 0x060FFFFC would violate this assumption.)
 *
 * This optimization will cause the translated code to behave incorrectly
 * or crash the host program if either of the assumptions above is violated.
 */
#define SH2_OPTIMIZE_POINTERS  (1<<5)

/**
 * SH2_OPTIMIZE_POINTERS_MAC:  When enabled, terminates a translation block
 * before a MAC instruction when either or both of the operands is not a
 * known pointer, or before a CLRMAC instruction followed by such a MAC
 * instruction.
 *
 * This optimization can cause the timing of cycle count checks to change,
 * and therefore alters trace output in all trace modes.
 *
 * This flag is ignored if SH2_OPTIMIZE_POINTERS is not enabled.
 */
#define SH2_OPTIMIZE_POINTERS_MAC  (1<<6)

/**
 * SH2_OPTIMIZE_STACK:  When enabled, optimizes accesses to the stack by
 * making the following assumptions:
 *
 * - The R15 register will always point to a directly-accessible region of
 *   memory.
 *
 * - The R15 register will always be 32-bit aligned.
 *
 * - Accesses through R15 will always access the same region of memory
 *   pointed to by R15 itself, regardless of any offset in the accessing
 *   instruction.
 *
 * - Writes through R15 will never overwrite SH-2 program code.
 *
 * This optimization will cause the translated code to behave incorrectly
 * or crash the host program if any of the assumptions above is violated.
 */
#define SH2_OPTIMIZE_STACK  (1<<7)

/**
 * SH2_OPTIMIZE_FOLD_SUBROUTINES:  When enabled, optimizes calls to short
 * subroutines which contain no branches (other than the terminating RTS)
 * by folding the instructions into the calling routine, thus omitting the
 * jump and return completely.  Whether a subroutine qualifies as "short"
 * is determined by the value of OPTIMIZE_FOLD_SUBROUTINES_MAX_LENGTH in
 * sh2-internal.h.
 *
 * This optimization assumes that any subroutines so called will not be
 * modified unless the calling routine is also modified, and that any JSR
 * target addresses loaded from PC-relative addresses are constants; the
 * translated code will behave incorrectly or crash if either of these
 * assumptions is violated.
 *
 * This optimization can cause the timing of cycle count checks to change,
 * and therefore alters trace output in all trace modes.
 */
#define SH2_OPTIMIZE_FOLD_SUBROUTINES  (1<<8)

/*-----------------------------------------------------------------------*/

/**
 * SH2State:  Structure containing SH-2 processor state information.  This
 * is the basic data type passed to all sh2_*() functions.
 */
typedef struct SH2State_ SH2State;
struct SH2State_ {

    /**** Public data ****/

    /* SH-2 register file */
    uint32_t R[16];
    uint32_t SR;
    uint32_t GBR;
    uint32_t VBR;
    uint32_t MACH;
    uint32_t MACL;
    uint32_t PR;
    uint32_t PC;

    /* Current clock cycle count (accumulates cycles during a call to
     * sh2_run()).  This field is _not_ reset when sh2_run() is called. */
    uint32_t cycles;

    /* Clock cycle limit for the current sh2_run() call */
    uint32_t cycle_limit;

    /*---- Fields below this line are not cached in translated code ----*/

    /* User data pointer (not used by SH-2 emulation) */
    void *userdata;

    /*================== Private data (do not touch!) ==================*/

    /* Flag indicating a delayed branch */
    uint8_t delay;

    /* Flag indicating that the processor is in sleep mode (after executing
     * a SLEEP instruction) */
    uint8_t asleep;

    /* Pending interrupt stack */
    struct {
        uint8_t vector;
        uint8_t level;
    } *interrupt_stack;
    /* Top-of-stack pointer (index of last valid entry + 1) */
    uint32_t interrupt_stack_top;

    /* Currently-executing block */
    struct JitEntry_ *current_entry;

    /* Pending branch type (to be executed after "delay" instructions) */
    enum {SH2BRTYPE_NONE = 0,   // No branch pending
          SH2BRTYPE_STATIC,     // Unconditional static (relative) branch
          SH2BRTYPE_DYNAMIC,    // Unconditional dynamic branch
          SH2BRTYPE_BT,         // Conditional static branch
          SH2BRTYPE_BF,
          SH2BRTYPE_BT_S,
          SH2BRTYPE_BF_S,
          SH2BRTYPE_RTE,        // Return from exception (dynamic branch)
          SH2BRTYPE_FOLDED,     // Subroutine call that will be folded
    } branch_type;
    uint32_t branch_target;     // Target address (used by decoder)
    uint32_t branch_target_reg; // RTL register containing target address
                                //    (must be 32-bit for interpreter's sake)
    void *branch_fold_native;   // For subroutine folding, address of native
                                //    implementation or NULL if none
    uint16_t branch_cond_reg;   // For SH2BRTYPE_BT etc or pending_select,
                                //    RTL register containing value of T bit
    uint16_t branch_cycles;     // For conditional branches, number of cycles
                                //    to add when branching
    uint8_t branch_targets_rts; // Nonzero if this branch targets an RTS/NOP
    uint8_t loop_to_jsr;        // Nonzero if this branch is a loop branch
                                //    targeting a JSR/BSR/BSRF which
                                //    immediately precedes the current block
    uint8_t folding_subroutine; // Nonzero if we're folding a subroutine
    uint8_t pending_select;     // Nonzero if we're processing a branch which
                                //    acts like a SELECT (?:) operation
    uint8_t select_sense;       // For pending_select, nonzero if it's a BT
                                //    or BT/S, zero if a BF or BF/S
    uint8_t just_branched;      // Nonzero if we just executed a branch
                                //    (used to determine whether a block of
                                //    code can fall off the end)

    /* Flag indicating that an interrupt check is required after this
     * instruction */
    uint8_t need_interrupt_check;

    /* Flag indicating whether MACH/MACL are known to be zero (used to
     * optimize MAC instructions) */
    uint8_t mac_is_zero;

    /* Cached shift count (used to merge successive shift operations) */
    uint8_t cached_shift_count;

    /* Data for optimization of variable shifts */
    uint32_t varshift_target_PC;// First instruction following a variable
                                //    shift sequence implemented with BRAF
                                //    (0 if not in such a sequence)
    uint8_t varshift_type;      // Type of variable shift
    uint8_t varshift_Rcount;    // Count register for variable shifts
    uint8_t varshift_max;       // Maximum count for variable shifts
    uint8_t varshift_Rshift;    // Target register for variable shifts

    /* Data for optimization of division operations */
    uint32_t division_target_PC;// PC of final DIV1 instruction (0 if not
                                //    in the middle of an optimized division)
    struct {
        uint16_t Rquo, Rrem;    // Register numbers (quotient and remainder)
        uint32_t quo, rem, SR;  // Result values for registers
    } div_data;

    /*--- Data used only during translation (invalid at other times) ---*/

    /* Block currently being translated */
    struct JitEntry_ *translate_entry;

    /* Flag indicating whether Rn of the current instruction should be
     * treated as a data pointer (for data pointer load hints) */
    uint8_t make_Rn_data_pointer;

    /*-------------- Miscellaneous data management fields --------------*/

    /* Pointer for linked list of all allocated state blocks */
    SH2State *next;
};

/* SR register bits */

#define SR_T    0x001
#define SR_S    0x002
#define SR_I    0x0F0
#define SR_Q    0x100
#define SR_M    0x200

#define SR_T_SHIFT  0
#define SR_S_SHIFT  1
#define SR_I_SHIFT  4
#define SR_Q_SHIFT  8
#define SR_M_SHIFT  9

/*----------------------------------*/

/**
 * SH2NativeFunction:  Type of a native function implementing an SH-2
 * routine.  Used for returning native implementstions from the manual
 * optimization callback.
 *
 * [Parameters]
 *     state: Processor state block pointer
 * [Return value]
 *     None
 */
typedef FASTCALL void (*SH2NativeFunctionPointer)(SH2State *state);

/*----------------------------------*/

/**
 * SH2OptimizeCallback:  Type of an optimization callback function, as
 * passed to sh2_set_manual_optimization_callback().
 *
 * [Parameters]
 *        state: Processor state block pointer
 *      address: Address from which to translate
 *        fetch: Pointer corresponding to "address" from which opcodes can
 *                  be fetched
 *     func_ret: Pointer to variable to receive address of native function
 *                  implementing this routine if return value is nonzero
 *     for_fold: Nonzero if the callback is being called to look up a
 *                  subroutine for folding, zero if being called for a
 *                  full block translation
 * [Return value]
 *     Length of translated block in instructions (nonzero) if a native
 *     function is returned, else zero
 * [Notes]
 *     (1) If the callback function returns nonzero, indicating that a
 *         native implementation is available, it must store an appropriate
 *         function pointer in *func_ret.  However, if for_fold is nonzero,
 *         the function may return NULL in *func_ret to indicate that the
 *         routine at the given address is not to be folded even if folding
 *         would be possible.
 *     (2) If for_fold is nonzero, any function returned must leave all
 *         registers except R0-R7, MACH/MACL, and PC unmodified (as
 *         specified by the SH-2 ABI).
 */
typedef unsigned int SH2OptimizeCallback(SH2State *state, uint32_t address,
                                         const uint16_t *fetch,
                                         SH2NativeFunctionPointer *func_ret,
                                         int for_fold);

/*----------------------------------*/

/**
 * SH2CacheFlushCallback:  Type of a cache flush callback function, as
 * passed to sh2_set_cache_flush_callback().
 *
 * [Parameters]
 *      start: Pointer to start of range
 *     length: Length of range in bytes
 * [Return value]
 *     None
 */
typedef void SH2CacheFlushCallback(void *start, uint32_t length);

/*----------------------------------*/

/**
 * SH2InvalidOpcodeCallback:  Type of an invalid opcode callback function,
 * as passed to sh2_set_invalid_opcode_callback().
 *
 * [Parameters]
 *      state: Processor state block pointer
 *         PC: PC at which the invalid instruction was found
 *     opcode: The invalid opcode itself
 * [Return value]
 *     None
 */
typedef void SH2InvalidOpcodeCallback(SH2State *state, uint32_t PC,
                                      uint16_t opcode);

/*----------------------------------*/

/**
 * SH2TraceInsnCallback:  Type of an instruction tracing callback function,
 * as passed to sh2_set_trace_insn_func().
 *
 * [Parameters]
 *       state: Processor state block pointer
 *     address: Address of instruction to trace
 * [Return value]
 *     None
 */
typedef FASTCALL void SH2TraceInsnCallback(SH2State *state, uint32_t address);

/**
 * SH2TraceAccessCallback:  Type of a memory access tracing callback
 * function, as passed to sh2_set_trace_store[bwl]_func().
 *
 * [Parameters]
 *     address: Access address
 *       value: Value written
 * [Return value]
 *     None
 */
typedef FASTCALL void SH2TraceAccessCallback(uint32_t address, uint32_t value);

/*************************************************************************/

/**
 * sh2_init:  Initialize the SH-2 core.  Must be called before creating any
 * virtual processors.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int sh2_init(void);

/**
 * sh2_set_optimizations, sh2_get_optimizations:  Set or retrieve the
 * bitmask of currently-enabled optional optimizations, as defined by the
 * SH2_OPTIMIZE_* constants.
 *
 * [Parameters]
 *     flags: Bitmask of optimizations to enable (sh2_set_optimizations() only)
 * [Return value]
 *     Bitmask of enabled optimizations (sh2_get_optimizations() only)
 */
extern void sh2_set_optimizations(uint32_t flags);
extern uint32_t sh2_get_optimizations(void);

/**
 * sh2_set_jit_data_limit:  Set the limit on the total size of translated
 * code, in bytes of native code (or bytes of RTL data when using the RTL
 * interpreter).  Does nothing if dynamic translation is disabled.
 *
 * [Parameters]
 *     limit: Total JIT data size limit
 * [Return value]
 *     None
 */
extern void sh2_set_jit_data_limit(uint32_t limit);

/**
 * sh2_set_manual_optimization_callback:  Set a callback function to be
 * called when beginning to analyze a new block of SH-2 code.  If the
 * function returns nonzero, it is assumed to have translated a block
 * starting at the given address into an optimized RTL instruction stream,
 * and the normal block analysis and translation is skipped.
 *
 * [Parameters]
 *     funcptr: Callback function pointer (NULL to unset a previously-set
 *                 function)
 * [Return value]
 *     None
 */
extern void sh2_set_manual_optimization_callback(SH2OptimizeCallback *funcptr);

/**
 * sh2_set_cache_flush_callback:  Set a callback function to be called when
 * a range of addresses should be flushed from the native CPU's caches.
 * This is called in preparation for executing a newly-translated block of
 * code, so the given range must be flushed from both data and instruction
 * caches.
 *
 * [Parameters]
 *     funcptr: Callback function pointer (NULL to unset a previously-set
 *                 function)
 * [Return value]
 *     None
 */
extern void sh2_set_cache_flush_callback(SH2CacheFlushCallback *funcptr);

/**
 * sh2_set_invalid_opcode_callback:  Set a callback function to be called
 * when the SH-2 decoder encounters an invalid instruction.
 *
 * [Parameters]
 *     funcptr: Callback function pointer (NULL to unset a previously-set
 *                 function)
 * [Return value]
 *     None
 */
extern void sh2_set_invalid_opcode_callback(SH2InvalidOpcodeCallback *funcptr);

/**
 * sh2_set_trace_insn_callback:  Set a callback function to be used for
 * tracing instructions as they are executed.  The function is only called
 * if tracing is enabled in the SH-2 core.
 *
 * [Parameters]
 *     funcptr: Callback function pointer (NULL to unset a previously-set
 *                 function)
 * [Return value]
 *     None
 */
extern void sh2_set_trace_insn_callback(SH2TraceInsnCallback *funcptr);

/**
 * sh2_set_trace_store[bwl]_callback:  Set a callback function to be used
 * for tracing 1-, 2-, or 4-byte write accesses, respectively.  The
 * function is only called if tracing is enabled in the SH-2 core.
 *
 * [Parameters]
 *     funcptr: Callback function pointer (NULL to unset a previously-set
 *                 function)
 * [Return value]
 *     None
 */
extern void sh2_set_trace_storeb_callback(SH2TraceAccessCallback *funcptr);
extern void sh2_set_trace_storew_callback(SH2TraceAccessCallback *funcptr);
extern void sh2_set_trace_storel_callback(SH2TraceAccessCallback *funcptr);

/**
 * sh2_alloc_direct_write_buffer:  Allocate a buffer which can be passed
 * as the write_buffer parameter to sh2_set_direct_access().  The buffer
 * should be freed with free() when no longer needed, but no earlier than
 * calling sh2_destroy() for all active virtual processors.
 *
 * [Parameters]
 *     size: Size of SH-2 address region to be marked as directly accessible
 * [Return value]
 *     Buffer pointer, or NULL on error
 */
extern void *sh2_alloc_direct_write_buffer(uint32_t size);

/**
 * sh2_set_direct_access:  Mark a region of memory as being directly
 * accessible.  The data format of the memory region is assumed to be
 * sequential 16-bit words in native order.
 *
 * As memory is internally divided into 512k (2^19) byte pages, both
 * sh2_address and size must be aligned to a multiple of 2^19.
 *
 * Passing a NULL parameter for native_address causes the region to be
 * marked as not directly accessible.
 *
 * [Parameters]
 *        sh2_address: Base address of region in SH-2 address space
 *     native_address: Pointer to memory block in native address space
 *               size: Size of memory block, in bytes
 *           readable: Nonzero if region is readable, zero if execute-only
 *       write_buffer: Buffer for checking writes to this region (allocated
 *                        with sh2_alloc_direct_write_buffer()), or NULL to
 *                        indicate that the region is read- or execute-only
 * [Return value]
 *     None
 */
extern void sh2_set_direct_access(uint32_t sh2_address, void *native_address,
                                  uint32_t size, int readable, void *write_buffer);

/**
 * sh2_set_byte_direct_access:  Mark a region of memory as being directly
 * accessible in 8-bit units.
 *
 * As memory is internally divided into 512k (2^19) byte pages, both
 * sh2_address and size must be aligned to a multiple of 2^19.
 *
 * Passing a NULL parameter for native_address causes the region to be
 * marked as not directly accessible.
 *
 * [Parameters]
 *        sh2_address: Base address of region in SH-2 address space
 *     native_address: Pointer to memory block in native address space
 *               size: Size of memory block, in bytes
 * [Return value]
 *     None
 */
extern void sh2_set_byte_direct_access(uint32_t sh2_address, void *native_address,
                                       uint32_t size);

/**
 * sh2_optimize_hint_data_pointer_load:  Provide a hint to the optimizer
 * that the load or ALU instruction at the given address loads a pointer to
 * a 16-bit direct-access data region.  If pointer optimization (the
 * SH2_OPTIMIZE_POINTERS flag) is enabled, accesses through the register so
 * loaded will automatically use direct access to SH-2 memory, and will
 * bypass the code overwrite checks normally executed for store operations.
 * If pointer optimization is disabled, this hint has no effect.
 *
 * The effect of calling this function from any context other than within
 * the manual optimization callback, or of specifying an instruction which
 * does not set the register specified by the Rn field of the opcode, is
 * undefined.
 *
 * [Parameters]
 *       state: Processor state block pointer passed to callback function
 *     address: Address of load instruction to mark as loading a data pointer
 * [Return value]
 *     None
 */
extern void sh2_optimize_hint_data_pointer_load(SH2State *state, uint32_t address);

/**
 * sh2_optimize_hint_data_pointer_register:  Provide a hint to the
 * optimizer that the given general-purpose register (R0 through R15)
 * contains a pointer to a direct-access data region at the start of the
 * block, and the memory region pointed to will not change over the life
 * of the block.  If pointer optimization (the SH2_OPTIMIZE_POINTERS flag)
 * is enabled, accesses through that register (as long as the register's
 * value is unchanged) will automatically use direct access to SH-2 memory,
 * and will bypass the code overwrite checks normally executed for store
 * operations; furthermore, the register's value will not be verified for
 * address validity at block start time, unlike dynamically-detected
 * pointers which are verified each time the block is called.  If pointer
 * optimization is disabled, this hint has no effect.
 *
 * The effect of calling this function from any context other than within
 * the manual optimization callback is undefined.
 *
 * [Parameters]
 *      state: Processor state block pointer passed to callback function
 *     regnum: General-purpose register to mark as containing a data pointer
 * [Return value]
 *     None
 */
extern void sh2_optimize_hint_data_pointer_register(SH2State *state,
                                                    unsigned int regnum);

/**
 * sh2_optimize_hint_constant_register:  Provide a hint to the optimizer
 * that the given general-purpose register (R0 through R15) contains a
 * value which will be constant at block entry for the life of the block.
 * Instructions which use the register as a source operand may be optimized
 * to use the constant value rather than loading the register, and if
 * subroutine folding (the SH2_OPTIMIZE_FOLD_SUBROUTINES flag) is enabled,
 * JSRs through that register will be considered for subroutine folding
 * using the address contained in that register at translation time.
 *
 * The effect of calling this function from any context other than within
 * the manual optimization callback is undefined.
 *
 * [Parameters]
 *      state: Processor state block pointer passed to callback function
 *     regnum: General-purpose register to mark as containing a data pointer
 * [Return value]
 *     None
 */
extern void sh2_optimize_hint_constant_register(SH2State *state, unsigned int regnum);

/*----------------------------------*/

/**
 * sh2_create:  Create a new virtual SH-2 processor.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     SH-2 processor state block (NULL on error)
 */
extern SH2State *sh2_create(void);

/**
 * sh2_destroy:  Destroy a virtual SH-2 processor.
 *
 * [Parameters]
 *     state: SH-2 processor state block
 * [Return value]
 *     None
 */
extern void sh2_destroy(SH2State *state);

/**
 * sh2_reset:  Reset a virtual SH-2 processor.
 *
 * [Parameters]
 *     state: SH-2 processor state block
 * [Return value]
 *     None
 */
extern void sh2_reset(SH2State *state);

/**
 * sh2_run:  Execute instructions for the given number of clock cycles.
 *
 * [Parameters]
 *      state: SH-2 processor state block
 *     cycles: Number of clock cycles to execute
 * [Return value]
 *     None
 */
extern void sh2_run(SH2State *state, uint32_t cycles);

/**
 * sh2_signal_interrupt:  Signal an interrupt to a virtual SH-2 processor.
 * The interrupt is ignored if another interrupt on the same vector has
 * already been signalled.
 *
 * [Parameters]
 *      state: SH-2 processor state block
 *     vector: Interrupt vector (0-127)
 *      level: Interrupt level (0-15, or 16 for NMI)
 * [Return value]
 *     None
 */
extern void sh2_signal_interrupt(SH2State *state, unsigned int vector,
                                 unsigned int level);

/**
 * sh2_write_notify:  Called when an external agent modifies memory.
 * Used here to clear any JIT translations in the modified range.
 *
 * [Parameters]
 *     address: Beginning of address range to which data was written
 *        size: Size of address range to which data was written (in bytes)
 * [Return value]
 *     None
 */
extern void sh2_write_notify(uint32_t address, uint32_t size);

/*************************************************************************/

#endif  // SH2_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
