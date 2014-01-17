/*  src/psp/sh2-internal.h: SH-2 emulator internal definitions/declarations
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

#ifndef SH2_INTERNAL_H
#define SH2_INTERNAL_H

#ifndef SH2_H
# include "sh2.h"
#endif

/*************************************************************************/
/************************* Configuration options *************************/
/*************************************************************************/

/*============ Compilation environment settings ============*/

/**
 * LOG2_SIZEOF_PTR:  The base-2 log of the size of a pointer value (i.e.
 * sizeof(void *) == 1 << LOG2_SIZEOF_PTR).
 */
#define LOG2_SIZEOF_PTR  (sizeof(void *) == 8 ? 3 : 2)

/*============ General options ============*/

/**
 * INTERRUPT_STACK_SIZE:  Sets the maximum number of interrupts that can
 * be stacked.  Any interrupts occurring when the stack is full will be
 * lost.
 */
#ifndef INTERRUPT_STACK_SIZE
# define INTERRUPT_STACK_SIZE 50
#endif

/**
 * ENABLE_JIT:  When defined, enables the use of dynamic recompilation.
 */
#define ENABLE_JIT

/**
 * JIT_ACCURATE_ACCESS_TIMING:  When defined, checks the current cycle
 * count against the cycle limit before each load or store operation to
 * ensure that external accesses occur at the proper times, as compared to
 * interpreted execution.  This does not apply to accesses which can be
 * proven to be to internal RAM or ROM ($[02]0[026]xxxxx).
 */
// #define JIT_ACCURATE_ACCESS_TIMING

/**
 * JIT_ACCURATE_LDC_SR_TIMING:  When defined, prevents interrupts from
 * being accepted during the instruction following an LDC ...,SR
 * instruction, just like a real SH-2 processor.  When not defined, accepts
 * interrupts immediately following an LDC ...,SR instruction, which may
 * provide better performance depending on the code being executed.
 */
// #define JIT_ACCURATE_LDC_SR_TIMING

/**
 * JIT_ALLOW_DISTANT_BRANCHES:  When defined, the translator will scan
 * forward past an unconditional branch for branch targets later in the
 * SH-2 code and attempt to include them in the same block.  Otherwise,
 * only a branch targeting the instruction immediately following the
 * branch's delay slot (or targeting the delay slot itself) will be
 * considered as part of the same block.
 */
#define JIT_ALLOW_DISTANT_BRANCHES

/**
 * JIT_TABLE_SIZE:  Specifies the size of the dynamic translation (JIT)
 * routine table.  The larger the table, the more translated code can be
 * retained in memory, but the greater the cost of stores that overwrite
 * previously-translated code.  This should always be a prime number.
 */
#ifndef JIT_TABLE_SIZE
# define JIT_TABLE_SIZE 4001
#endif

/**
 * JIT_DATA_LIMIT_DEFAULT:  Specifies the default for the maximum total
 * size of translated code, in bytes of native code.  When this limit is
 * reached, the least recently created translations will be purged from
 * memory to make room for new translations.  This limit can be changed
 * dynamically with sh2_set_jit_data_limit().
 */
#ifndef JIT_DATA_LIMIT_DEFAULT
# define JIT_DATA_LIMIT_DEFAULT 20000000
#endif

/**
 * JIT_BRANCH_PREDICTION_SLOTS:  Specifies the number of dynamic branch
 * prediction slots to use for each block of translated code.  A larger
 * value increases the number of different branch targets which can be
 * predicted without having to search for the code in the global
 * translation table, but slows down processing of code with varying
 * branch targets.  Each slot also uses up 16 * JIT_TABLE_SIZE bytes of
 * memory in the translation table (on a 32-bit system).
 *
 * A value of zero disables branch prediction entirely (including optimized
 * static branch prediction).
 */
#ifndef JIT_BRANCH_PREDICTION_SLOTS
# define JIT_BRANCH_PREDICTION_SLOTS 3
#endif

/**
 * JIT_BRANCH_PREDICTION_FLOAT:  When defined, causes successfully
 * predicted dynamic branches to "float" up the predicted branch table, so
 * they can be found more quickly the next time the block is executed.
 * Whether this helps or hurts performance depends on the particular code
 * being executed; blocks called from a single location many times followed
 * by a different single location many times will benefit, while blocks
 * which are alternately called by two or more locations will suffer.
 *
 * This option has no effect (obviously) if the value of
 * JIT_BRANCH_PREDICTION_SLOTS is 0 or 1.
 */
#define JIT_BRANCH_PREDICTION_FLOAT

/**
 * JIT_BRANCH_PREDICT_STATIC:  When defined, optimizes static branches and
 * block termination code to minimize the overhead of jumping from one
 * block to another.
 *
 * This option is ignored if the value of JIT_BRANCH_PREDICTION_SLOTS is 0.
 */
// #define JIT_BRANCH_PREDICT_STATIC

/**
 * JIT_INSNS_PER_BLOCK:  Specifies the maximum number of SH-2 instructions
 * (or 16-bit words of local data) to translate in a single block.  A
 * larger value allows larger blocks of code to be translated as a single
 * unit, potentially increasing execution speed at the cost of longer
 * translation delays.
 */
#ifndef JIT_INSNS_PER_BLOCK
# define JIT_INSNS_PER_BLOCK 512
#endif

/**
 * JIT_MAX_INSN_GAP:  Specifies the maximum number of 16-bit words to skip
 * between SH-2 instructions or local data before terminating the block.
 * A larger value allows common paths through a block to be translated as a
 * single unit and helps detection of local data that does not immediately
 * follow the last translated instruction, but may result in some segments
 * of code being translated multiple times.
 *
 * Setting this option to 0 is not recommended when using the runtime-
 * selectable OPTIMIZE_LOCAL_ACCESSES optimization, since doing so prevents
 * recognition of all local data in blocks that end on a non-32-bit-aligned
 * address.
 */
#ifndef JIT_MAX_INSN_GAP
# define JIT_MAX_INSN_GAP 256
#endif

/**
 * JIT_BTCACHE_SIZE:  Specifies the size of the branch target cache.
 * A larger cache increases the amount of SH-2 code that can be translated
 * as a single block, but increases the time required to translate branch
 * instructions.
 */
#ifndef JIT_BTCACHE_SIZE
# define JIT_BTCACHE_SIZE 256
#endif

/**
 * JIT_UNRES_BRANCH_SIZE:  Specifies the size of the unresolved branch
 * table.  A larger table size increases the complexity of SH-2 code that
 * can be translated as a single block, but increases the time required to
 * translate all instructions.
 */
#ifndef JIT_UNRES_BRANCH_SIZE
# define JIT_UNRES_BRANCH_SIZE 32
#endif

/**
 * JIT_PAGE_BITS:  Specifies the page size used for checking whether a
 * store operation affects a previously-translated block, in powers of two
 * (e.g. a value of 8 means a page size of 256 bytes).  A larger page size
 * decreases the amount of memory needed for the page tables, but increases
 * the chance of an ordinary data write triggering an expensive check of
 * translated blocks.
 */
#ifndef JIT_PAGE_BITS
# define JIT_PAGE_BITS 8
#endif

/**
 * JIT_BLACKLIST_SIZE:  Specifies the size of the blacklist used for
 * tracking regions of memory that should not be translated due to runtime
 * modifications by nearby code.  A smaller blacklist size increases the
 * speed of handling writes to such regions as well as the speed of code
 * translation, but also increases the chance of thrashing on the table,
 * which can significantly degrade performance.
 */
#ifndef JIT_BLACKLIST_SIZE
# define JIT_BLACKLIST_SIZE 10
#endif

/**
 * JIT_BLACKLIST_EXPIRE:  Specifies the time after which a blacklist entry
 * will expire if it has not been written to.  The unit is calls to
 * jit_exec(), so the optimal value will need to be found through
 * experimentation.
 */
#ifndef JIT_BLACKLIST_EXPIRE
# define JIT_BLACKLIST_EXPIRE 1000000
#endif

/**
 * JIT_PURGE_TABLE_SIZE:  Specifies the size of the purge table, which
 * holds addresses of SH-2 code blocks whose translations have been purged
 * from memory due to failure of optimization preconditions.  A larger
 * table size slows down the translation of code blocks as well as the
 * handling of a purge operation.
 *
 * This value is also used for the size of the "pending blacklist" table,
 * used to track addresses which cause jit_clear_write() faults many times
 * in rapid succession so that they can be blacklisted to avoid repeated
 * retranslation of the affected blocks.  (This latter table is essentially
 * the equivalent of the purge table for data accesses.)
 */
#ifndef JIT_PURGE_TABLE_SIZE
# define JIT_PURGE_TABLE_SIZE 16
#endif

/**
 * JIT_PURGE_THRESHOLD:  Specifies the number of purges on a single block
 * after which all optimizations which require precondition checks are
 * disabled for that block.  This prevents blocks which take varying
 * parameters (pointers to different memory regions, for example) from
 * requiring a retranslation on potentially every call.  This value must
 * be greater than 1 (a value of 1 may cause incorrect operation).
 *
 * This value is also used for the blacklisting threshold of the pending
 * blacklist table.
 */
#ifndef JIT_PURGE_THRESHOLD
# define JIT_PURGE_THRESHOLD 3
#endif

/**
 * JIT_PURGE_EXPIRE:  Specifies the time after which a purge table entry
 * will expire.  The unit is calls to jit_exec(), so the optimal value will
 * need to be found through experimentation.
 *
 * This value is also used for the expiration time of entries in the
 * pending blacklist table.
 */
#ifndef JIT_PURGE_EXPIRE
# define JIT_PURGE_EXPIRE 100000
#endif

/*============ Code generation options ============*/

/**
 * JIT_USE_RTL_REGIMM:  When defined, causes the JIT core to use the RTL
 * register-immediate instructions (RTLOP_ADDI, etc.); when not defined,
 * the JIT core will instead load immediate operands into registers which
 * are then used with the register-register form of the instruction.
 *
 * For MIPS, the register-immediate instructions are a significant win.
 */
#define JIT_USE_RTL_REGIMM

/**
 * JIT_USE_RTL_BITFIELDS:  When defined, causes the JIT core to use the RTL
 * bitfield manipulation instructions (RTLOP_BFINS and RTLOP_BFEXT) where
 * convenient; when not defined, appropriate combinations of AND, OR,
 * and shifts will be used instead.
 *
 * This is generally a win on any architecture supporting bitfield
 * manipulation instructions, including the MIPS Allegrex (PSP)
 * architecture.
 */
#define JIT_USE_RTL_BITFIELDS

/*============ Optimization options ============*/

/**
 * OPTIMIZE_IDLE:  When defined, attempts to find "idle loops", i.e. loops
 * which continue indefinitely until some external event occurs, and modify
 * their behavior to increase processing speed.  Specifically, when an idle
 * loop finishes an iteration and branches back to the beginning of the
 * loop, the virtual processor will consume all pending execution cycles
 * immediately rather than continue executing the loop until the requested
 * number of cycles have passed.
 *
 * This optimization will slightly change execution timing as compared to
 * real hardware, since the number of cycles per loop is ignored when
 * consuming pending cycles.
 */
#define OPTIMIZE_IDLE

/**
 * OPTIMIZE_IDLE_MAX_INSNS:  When OPTIMIZE_IDLE is defined, specifies the
 * maximum number of instructions to consider when looking at a single
 * potential idle loop.
 */
#ifndef OPTIMIZE_IDLE_MAX_INSNS
# define OPTIMIZE_IDLE_MAX_INSNS 8
#endif

/**
 * OPTIMIZE_DELAY:  When defined, modifies the behavior of delay loops of
 * the form
 *    label: DT Rn
 *           BF label
 * to increase their execution speed.
 *
 * This optimization alters trace output in TRACE and TRACE_STEALTH modes,
 * but does not change TRACE_LITE trace output.
 */
#define OPTIMIZE_DELAY

/**
 * OPTIMIZE_DELAY_OMIT_MAX:  Specifies the maximum number of iterations for
 * omitting delay loops entirely from the translated code.
 *
 * When OPTIMIZE_DELAY is defined and a delay loop with a known number of
 * iterations is found, if that known number of iterations is no greater
 * than this value, the loop will be optimized out completely and replaced
 * with code to consume the appropriate number of cycles (4 cycles per
 * iteration) and clear the counter register to zero.
 */
#ifndef OPTIMIZE_DELAY_OMIT_MAX
# define OPTIMIZE_DELAY_OMIT_MAX 100
#endif

/**
 * OPTIMIZE_DIVISION:  When defined, attempts to find instruction sequences
 * that perform division operations and replace them with native division
 * instructions.  This can achieve a speed increase of an order of
 * magnitude or more with respect to the division operation.
 *
 * This optimization alters trace output in TRACE and TRACE_STEALTH modes,
 * but does not change TRACE_LITE trace output.
 */
#define OPTIMIZE_DIVISION

/**
 * OPTIMIZE_SHIFT_SEQUENCES:  When defined, replaces sequences of similar
 * shift instructions with a single native shift instruction of the total
 * count.  The following replacements are performed:
 *
 *    - Zero or more SHLL{2,8,16} Rn followed by one or more SH[AL]L Rn
 *         ==> SLLI(Rn,Rn,count) and set T
 *    - One or more SHLL{2,8,16} Rn _not_ followed by SH[AL]L Rn
 *         ==> SLLI(Rn,Rn,count)
 *
 *    - Zero or more SHLR{2,8,16} Rn followed by one or more SHLR Rn
 *         ==> SRLI(Rn,Rn,count) and set T
 *    - One or more SHLR{2,8,16} Rn _not_ followed by SHLR Rn
 *         ==> SRLI(Rn,Rn,count)
 *
 *    - One or more SHAR Rn ==> SRAI(Rn,Rn,count)
 *
 *    - One or more ROTL Rn ==> RORI(Rn,Rn,(32-count))
 *
 *    - One or more ROTR Rn ==> RORI(Rn,Rn,count)
 *
 * This optimization alters trace output in TRACE and TRACE_STEALTH modes,
 * but does not change TRACE_LITE trace output.
 */
#define OPTIMIZE_SHIFT_SEQUENCES

/**
 * OPTIMIZE_VARIABLE_SHIFTS:  When defined, replaces instruction sequences
 * that perform variable-count shifts with shorter equivalent sequences of
 * RTL instructions, potentially allowing multiple branch instructions to
 * be eliminated.
 *
 * This optimization alters trace output in TRACE and TRACE_STEALTH modes,
 * and since it may eliminate branches, it may alter trace output in
 * TRACE_LITE mode as well.
 */
#define OPTIMIZE_VARIABLE_SHIFTS

/**
 * OPTIMIZE_KNOWN_VALUES:  When defined, tracks which bits of which
 * registers have known values and, when possible, performs calculations
 * using those values at translation time rather than runtime.
 *
 * This optimization can cause the timing of cycle count checks to change,
 * and therefore alters trace output in all trace modes.
 */
#define OPTIMIZE_KNOWN_VALUES

/**
 * OPTIMIZE_BRANCH_FALLTHRU:  When defined, checks for branches which
 * branch to the next instruction to be translated and converts them to
 * native no-ops.
 *
 * This optimization alters trace output in TRACE and TRACE_STEALTH modes,
 * but does not change TRACE_LITE trace output.
 */
#define OPTIMIZE_BRANCH_FALLTHRU

/**
 * OPTIMIZE_BRANCH_THREAD:  When defined, checks for conditional branches
 * which branch to another conditional branch of the same sense and
 * "threads" the branch through to the final target.  This sort of branch
 * chain can arise in long blocks of code due to the limited range of the
 * conditional branch instructions (-128...+127 instructions).
 *
 * This optimization alters trace output in all trace modes.
 */
#define OPTIMIZE_BRANCH_THREAD

/**
 * OPTIMIZE_BRANCH_SELECT:  When defined, checks for conditional branches
 * whose only use is to choose between one of two values for a register,
 * and converts such branches into native SELECT operations.
 *
 * This optimization alters trace output in TRACE mode.
 */
#define OPTIMIZE_BRANCH_SELECT

/**
 * OPTIMIZE_LOOP_TO_JSR:  When defined, checks for backward branches that
 * target a subroutine call (JSR, BSR, or BSRF) immediately preceding the
 * beginning of the current block, and encodes the subroutine call along
 * with its delay slot as part of the backward branch, avoiding the need
 * to jump to a separate block just for the subroutine call.
 *
 * This optimization alters trace output in all trace modes.
 */
#define OPTIMIZE_LOOP_TO_JSR

/**
 * OPTIMIZE_STATE_BLOCK:  When defined, attempts to minimize the number of
 * state block accesses (loads and stores) by keeping live as long as
 * possible each RTL register that holds a state block value.  Values are
 * flushed to memory when branching, and all cached values are cleared at
 * branch targets.
 */
#define OPTIMIZE_STATE_BLOCK

/**
 * OPTIMIZE_CONSTANT_ADDS:  When defined, accumulates constants added to or
 * subtracted from a register, either immediate values in ADD #imm or
 * offsets resulting from postincrement/predecrement memory accesses, and
 * attempts to minimize the number of actual ADD instructions used to
 * update the register in RTL code.
 *
 * This optimization relies on the following assumptions:
 *
 * - Offsets will not cause the final address to cross a page (2^19 byte)
 *   boundary.
 *
 * - Offsetted stores will not overwrite any code that a store to the
 *   non-offsetted address would not overwrite.
 *
 * If either of these assumptions are violated, the translated code will
 * behave incorrectly and may crash the host program.
 *
 * Depends on OPTIMIZE_STATE_BLOCK; if OPTIMIZE_STATE_BLOCK is not defined,
 * this optimization will not take place.
 */
#define OPTIMIZE_CONSTANT_ADDS

/**
 * OPTIMIZE_LOOP_REGISTERS:  When defined, attempts to keep SH-2 registers
 * and other state block fields used in a loop live in RTL registers for
 * the duration of the loop, rather than reloading and flushing on each
 * iteration.  Registers which are only set within the loop (i.e., whose
 * final value does not depend on the value of the register at the
 * beginning of the loop) are not treated specially.
 *
 * Loops which include internal forward branches and other sufficiently
 * complex loops will not be optimized.
 *
 * Depends on OPTIMIZE_STATE_BLOCK; if OPTIMIZE_STATE_BLOCK is not defined,
 * this optimization will not take place.
 */
#define OPTIMIZE_LOOP_REGISTERS

/**
 * OPTIMIZE_LOOP_REGISTERS_MAX_REGS:  Specifies the maximum number of RTL
 * registers to keep live over a loop.  Higher values minimize reload
 * operations at the RTL generation level, but may increase reloads at the
 * native code level due to register pressure.
 */
#ifndef OPTIMIZE_LOOP_REGISTERS_MAX_REGS
# define OPTIMIZE_LOOP_REGISTERS_MAX_REGS 12
#endif

/**
 * OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD:  Specifies the number of
 * references to an unoptimizable pointer which will cause the block to be
 * terminated immediately before the first access (thus providing another
 * chance to optimize the pointer).  A value of 1 will be treated as 2; a
 * value of 0 disables this check entirely.
 */
#ifndef OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD
# define OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD 3
#endif

/**
 * OPTIMIZE_FOLD_SUBROUTINES_MAX_LENGTH:  Specifies the maximum number of
 * instructions in a subroutine (excluding the terminating RTS and its
 * delay slot) for the subroutine to qualify as foldable for the
 * SH2_OPTIMIZE_FOLD_SUBROUTINES optimization.
 */
#ifndef OPTIMIZE_FOLD_SUBROUTINES_MAX_LENGTH
# define OPTIMIZE_FOLD_SUBROUTINES_MAX_LENGTH 16
#endif

/**
 * JIT_OPTIMIZE_FLAGS:  Specifies the optimizations that should be
 * performed on the generated RTL code.  See RTLOPT_* in rtl.h for details
 * on the available flags.
 */
#ifndef JIT_OPTIMIZE_FLAGS
# define JIT_OPTIMIZE_FLAGS 0  // Optimization doesn't currently win us much
#endif

/*============ Debugging options ============*/

/**
 * TRACE:  When defined, all instructions and all store operations are
 * traced using the functions passed to sh2_trace_insn_callback() and
 * sh2_trace_store[bwl]_callback().
 */
// #define TRACE

/**
 * TRACE_STEALTH:  When defined, all instructions and all store operations
 * are traced in a way that does not affect the behavior of the generated
 * code.  Where TRACE inserts RTL instructions to flush cached values and
 * call the relevant trace functions (thus updating memory more often than
 * usual and potentially hiding bugs), TRACE_STEALTH inserts specially-
 * coded NOP instructions that inform the RTL interpreter and native code
 * translators about cached values and direct it to call the tracing
 * functions itself, thus not affecting the behavior of the RTL code.
 * (This requires significantly more overhead than regular tracing with the
 * TRACE option.)  Note that it is also necessary to define
 * RTL_TRACE_STEALTH_FOR_SH2 in rtl-internal.h to enable support for this
 * option.
 *
 * Due to optimization (such as clobbering of dead registers),
 * TRACE_STEALTH is likely to not work correctly with native code.
 *
 * TRACE takes precedent over TRACE_STEALTH; if TRACE is defined, then
 * TRACE_STEALTH is ignored and tracing code is added directly to the RTL
 * code stream.
 */
// #define TRACE_STEALTH

/**
 * TRACE_LITE:  When defined, traces instructions at the rate of one per
 * call to sh2_run().  This allows the progress of execution to be
 * monitored without the significant overhead imposed by inserting trace
 * calls for every instruction in the code stream.
 *
 * TRACE and TRACE_STEALTH take precedence over TRACE_LITE; if either of
 * the former two are defined, TRACE_LITE is ignored rather than causing a
 * duplicate trace to be output at the beginning of an sh2_run() call.
 */
// #define TRACE_LITE

/**
 * TRACE_LITE_VERBOSE:  When defined and when TRACE_LITE is also enabled,
 * additionally traces once per call to jit_exec() (except the first in
 * each sh2_run() call, to avoid a double trace).
 */
// #define TRACE_LITE_VERBOSE

/**
 * DEBUG_DECODER_INSN_COVERAGE:  When defined, a debug line is printed the
 * first time the SH-2 decoder encounters each instruction (specifically,
 * each opcode pattern handled by a distinct code block).  This can be used
 * to check the coverage of test runs.
 */
// #define DEBUG_DECODER_INSN_COVERAGE

/**
 * JIT_DEBUG:  When defined, debug messages are output in cases that may
 * indicate a problem in the translation or optimization of SH-2 code.
 */
// #define JIT_DEBUG

/**
 * JIT_DEBUG_VERBOSE:  When defined, additional debug messages are output
 * in certain cases considered useful in fine-tuning the translation and
 * optimization.
 */
// #define JIT_DEBUG_VERBOSE

/**
 * JIT_DEBUG_TRACE:  When defined, a trace line is printed for each SH-2
 * instruction translated.  This option is independent of the other trace
 * options.
 */
// #define JIT_DEBUG_TRACE

/**
 * JIT_DEBUG_INSERT_PC:  When defined, causes the native code generator to
 * insert dummy instructions at the beginning of the code for each SH-2
 * instruction, indicating the SH-2 PC for that instruction.  The dummy
 * instructions are of the form:
 *    (RTL)
 *       nop 0x12345678
 *    (MIPS)
 *       lui $zero, 0x1234
 *       ori $zero, $zero, 0x5678
 * for SH-2 PC 0x12345678.
 */
// #define JIT_DEBUG_INSERT_PC

/**
 * JIT_DEBUG_INTERPRET_RTL:  When defined, causes the JIT core to execute
 * RTL instruction sequences directly rather than translating them into
 * MIPS machine code.
 *
 * This is currently forced on when not compiling on PSP since the only
 * available RTL->native translator at the moment is the MIPS translator.
 */
// #define JIT_DEBUG_INTERPRET_RTL

/**
 * JIT_PROFILE:  When defined, counts the number of times each code block
 * is executed and the time spent in execution.  Every JIT_PROFILE_INTERVAL
 * SH-2 clock cycles, the first JIT_PROFILE_TOP callees in terms of
 * execution time and number of calls are printed.
 */
// #define JIT_PROFILE
#ifndef JIT_PROFILE_INTERVAL
# define JIT_PROFILE_INTERVAL 50000000
#endif
#ifndef JIT_PROFILE_TOP
# define JIT_PROFILE_TOP 10
#endif

/**
 * PSP_TIME_TRANSLATION:  When defined, calculates the average amount of
 * time required to translate a single SH-2 instruction.  Only works on the
 * PSP.
 */
// #define PSP_TIME_TRANSLATION

/*************************************************************************/

/* Perform sanity checks on configuration options */

#if JIT_BRANCH_PREDICTION_SLOTS <= 0
# undef JIT_BRANCH_PREDICT_STATIC
#endif

#if JIT_PURGE_THRESHOLD < 2
# undef JIT_PURGE_THRESHOLD
# define JIT_PURGE_THRESHOLD 2
#endif

#ifndef OPTIMIZE_STATE_BLOCK
# undef OPTIMIZE_CONSTANT_ADDS
# undef OPTIMIZE_LOOP_REGISTERS
#endif

#if !defined(TRACE) || !defined(OPTIMIZE_DIVISION)
# undef TRACE_OPTIMIZED_DIVISION
#endif

#ifdef TRACE
# undef TRACE_STEALTH
# undef TRACE_LITE
#endif

#ifdef TRACE_STEALTH
# undef TRACE_LITE
#endif

#ifndef TRACE_LITE
# undef TRACE_LITE_VERBOSE
#endif

#ifndef PSP
# define JIT_DEBUG_INTERPRET_RTL
# undef PSP_TIME_TRANSLATION
#endif

/*************************************************************************/
/************** Internal-use data and function declarations **************/
/*************************************************************************/

/******** sh2.c ********/

/* Bitmask indicating which optional optimizations are enabled */
extern uint32_t optimization_flags;

/* Callback function for manual/special-case optimization */
extern SH2OptimizeCallback *manual_optimization_callback;

/* Callback function for native CPU cache flushing */
extern SH2CacheFlushCallback *cache_flush_callback;

/* Callback function for invalid instructions */
extern SH2InvalidOpcodeCallback *invalid_opcode_callback;

/* Callback functions for tracing */
extern SH2TraceInsnCallback *trace_insn_callback;
extern SH2TraceAccessCallback *trace_storeb_callback;
extern SH2TraceAccessCallback *trace_storew_callback;
extern SH2TraceAccessCallback *trace_storel_callback;

#ifdef ENABLE_JIT
/* Page tables (exported for use in sh2-optimize.c) */
extern uint8_t *direct_pages[0x2000];
extern uint8_t *fetch_pages[0x2000];
extern uint8_t *byte_direct_pages[0x2000];
extern uint8_t *direct_jit_pages[0x2000];
#endif

/**
 * check_interrupts:  Check whether there are any pending interrupts, and
 * service the highest-priority one if so.
 *
 * [Parameters]
 *     state: Processor state block
 * [Return value]
 *     Nonzero if an interrupt was serviced, else zero
 */
extern FASTCALL int check_interrupts(SH2State *state);

/******** sh2-interpret.c ********/

/**
 * interpret_insn:  Interpret and execute a single SH-2 instruction at the
 * current PC.
 *
 * [Parameters]
 *     state: SH-2 processor state
 * [Return value]
 *     None
 */
extern void interpret_insn(SH2State *state);


/******** sh2-opcodeinfo.c ********/

/**
 * SH2_OPCODE_INFO_*:  Flags used in the get_opcode_info() return value.
 * "Rn" always refers to the register specified by bits 8-11 of the opcode,
 * and "Rm" always refers to bits 4-7 of the opcode, regardless of the
 * labeling used in the official Hitachi specs.
 */
#define SH2_OPCODE_INFO_USES_R0         (1<< 0) // Uses the value of R0
#define SH2_OPCODE_INFO_USES_Rm         (1<< 1) // Uses the value of Rm
#define SH2_OPCODE_INFO_USES_Rn         (1<< 2) // Uses the value of Rn
#define SH2_OPCODE_INFO_USES_R15        (1<< 3) // Uses the value of R15
#define SH2_OPCODE_INFO_SETS_R0         (1<< 4) // Sets the value of R0
                                      /* 1<< 5 is unused */
#define SH2_OPCODE_INFO_SETS_Rn         (1<< 6) // Sets the value of Rn
#define SH2_OPCODE_INFO_SETS_SR_T       (1<< 7) // Sets the value of SR.T
#define SH2_OPCODE_INFO_ACCESSES_GBR    (1<< 8) // Accesses memory through GBR
#define SH2_OPCODE_INFO_ACCESSES_Rm     (1<< 9) // Accesses memory through Rm
#define SH2_OPCODE_INFO_ACCESSES_Rn     (1<<10) // Accesses memory through Rn
#define SH2_OPCODE_INFO_ACCESSES_R15    (1<<11) // Accesses memory through R15
#define SH2_OPCODE_INFO_ACCESSES_R0_GBR (1<<12) // Accesses memory thru R0+GBR
#define SH2_OPCODE_INFO_ACCESSES_R0_Rm  (1<<13) // Accesses memory thru R0+Rm
#define SH2_OPCODE_INFO_ACCESSES_R0_Rn  (1<<14) // Accesses memory thru R0+Rn
#define SH2_OPCODE_INFO_ACCESSES_PC     (1<<15) // Accesses memory through PC
#define SH2_OPCODE_INFO_ACCESS_IS_STORE (1<<16) // Memory access is a store
#define SH2_OPCODE_INFO_ACCESS_IS_RMW   (1<<17) // Read/modify/write operation
#define SH2_OPCODE_INFO_ACCESS_POSTINC  (1<<18) // Postincrement access mode
#define SH2_OPCODE_INFO_ACCESS_PREDEC   (1<<19) // Predecrement access mode
#define SH2_OPCODE_INFO_ACCESS_SIZE_B   (1<<20) // Memory access is size 1
#define SH2_OPCODE_INFO_ACCESS_SIZE_W   (1<<21) // Memory access is size 2
#define SH2_OPCODE_INFO_ACCESS_SIZE_L   (1<<22) // Memory access is size 4
#define SH2_OPCODE_INFO_ACCESS_SIZE_LL  (1<<23) // Memory access is size 8
#define SH2_OPCODE_INFO_ACCESS_DISP_4   (1<<24) // 4-bit displacement
#define SH2_OPCODE_INFO_ACCESS_DISP_8   (1<<25) // 8-bit displacement
#define SH2_OPCODE_INFO_BRANCH_UNCOND   (1<<26) // Branches unconditionally
#define SH2_OPCODE_INFO_BRANCH_COND     (1<<27) // Branches conditionally
#define SH2_OPCODE_INFO_BRANCH_DELAYED  (1<<28) // Branches after a delay slot
#define SH2_OPCODE_INFO_VALID           (1<<31) // Opcode is valid

/**
 * SH2_OPCODE_INFO_ACCESS_SIZE:  Returns the size in bytes of an access
 * performed by an instruction, given that instruction's get_opcode_info()
 * value.
 */
#define SH2_OPCODE_INFO_ACCESS_SIZE(opcode_info)  (((opcode_info) >> 20) & 0xF)

/**
 * init_opcode_info:  Initialize the opcode_info[] table.  Must be called
 * before calling get_opcode_info().
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void init_opcode_info(void);

/**
 * get_opcode_info:  Return information about the given opcode.
 *
 * [Parameters]
 *     opcode: SH-2 opcode to obtain information about
 * [Return value]
 *     
 */
#ifdef __GNUC__
__attribute__((const))
#endif
extern int32_t get_opcode_info(uint16_t opcode);

/******** sh2-optimize.c ********/

#ifdef OPTIMIZE_IDLE

/**
 * can_optimize_idle:  Return whether the given sequence of instructions
 * forms an "idle loop", in which the processor remains in a constant state
 * (or repeating sequence of states) indefinitely until a certain external
 * event occurs, such as an interrupt or a change in the value of a memory-
 * mapped register.  If an idle loop is detected, also return information
 * allowing the loop to be translated into a faster sequence of native
 * instructions.
 *
 * The sequence of instructions is assumed to end with a branch instruction
 * to the beginning of the sequence (possibly including a delay slot).
 *
 * [Parameters]
 *     insn_ptr: Pointer to first instruction
 *           PC: PC of first instruction
 *        count: Number of instructions to check
 * [Return value]
 *     Nonzero if the given sequence of SH-2 instructions form an idle
 *     loop, else zero
 */
extern int can_optimize_idle(const uint16_t *insn_ptr, uint32_t PC,
                             unsigned int count);

#endif  // OPTIMIZE_IDLE

#ifdef OPTIMIZE_DELAY

/**
 * can_optimize_delay:  Return whether the given sequence of instructions
 * forms a "delay loop", in which a counter register is repeatedly
 * decremented with the DT instruction until it reaches zero.
 *
 * The sequence of instructions is assumed to end with a branch instruction
 * to the beginning of the sequence (possibly including a delay slot).
 *
 * [Parameters]
 *        insn_ptr: Pointer to first instruction
 *              PC: PC of first instruction
 *           count: Number of instructions to check
 *     counter_ret: Pointer to variable to receive counter register index
 *                     if a delay loop (unmodified if not a delay loop)
 * [Return value]
 *     Number of clock cycles taken by the loop (nonzero) if the given
 *     sequence of SH-2 instructions form a delay loop, else zero
 */
extern int can_optimize_delay(const uint16_t *insn_ptr, uint32_t PC,
                              unsigned int count, unsigned int *counter_ret);

#endif  // OPTIMIZE_DELAY

#ifdef OPTIMIZE_DIVISION

/**
 * can_optimize_div0u:  Return whether a sequence of instructions starting
 * from a DIV0U instruction can be optimized to a native divide operation.
 *
 * [Parameters]
 *     insn_ptr: Pointer to DIV0U instruction
 *           PC: PC of DIV0U instruction
 *     skip_first_rotcl: Nonzero if the first ROTCL instruction is known to
 *                          be omitted (as may happen if the low word of
 *                          the dividend is known to be zero)
 *      Rhi_ret: Pointer to variable to receive index of dividend high register
 *      Rlo_ret: Pointer to variable to receive index of dividend low register
 *     Rdiv_ret: Pointer to variable to receive index of divisor register
 * [Return value]
 *     Number of bits of division performed by the instructions following
 *     the DIV0U instruction (1-32), or zero if the following instructions
 *     do not perform a division operation
 */
extern int can_optimize_div0u(const uint16_t *insn_ptr, uint32_t PC,
                              int skip_first_rotcl,
                              int *Rhi_ret, int *Rlo_ret, int *Rdiv_ret);

/**
 * can_optimize_div0s:  Return whether a sequence of instructions starting
 * from a DIV0S instruction can be optimized to a native divide operation.
 *
 * [Parameters]
 *     insn_ptr: Pointer to instruction following DIV0S instruction
 *           PC: PC of instruction following DIV0S instruction
 *          Rhi: Index of dividend high register
 *      Rlo_ret: Pointer to variable to receive index of dividend low register
 *         Rdiv: Index of divisor register
 * [Return value]
 *     Nonzero if the next 64 SH-2 instructions form a 32-bit division
 *     operation, else zero
 */
extern int can_optimize_div0s(const uint16_t *insn_ptr, uint32_t PC,
                              int Rhi, int *Rlo_ret, int Rdiv);

#endif  // OPTIMIZE_DIVISION

#ifdef OPTIMIZE_VARIABLE_SHIFTS

/**
 * can_optimize_variable_shift:  Return whether a sequence of instructions
 * can be optimized to a native variable-count shift operation.
 *
 * [Parameters]
 *       insn_ptr: Pointer to first instruction
 *             PC: PC of first instruction
 *     Rcount_ret: Pointer to variable to receive index of shift count register
 *        max_ret: Pointer to variable to receive maximum shift count
 *     Rshift_ret: Pointer to variable to receive index of target register
 *       type_ret: Pointer to variable to receive:
 *                    0 if a SHLL/SHAL sequence
 *                    1 if a SHLR sequence
 *                    2 if a SHAR sequence
 *                    3 if a ROTL sequence
 *                    4 if a ROTR sequence
 *     cycles_ret: Pointer to variable to receive pointer to an array of
 *                    cycle counts indexed by shift count (unused for some
 *                    types of sequences)
 * [Return value]
 *     Number of instructions consumed (nonzero) if an optimizable sequence
 *     is found, else zero
 */
extern unsigned int can_optimize_variable_shift(
    const uint16_t *insn_ptr, uint32_t PC, unsigned int *Rcount_ret,
    unsigned int *max_ret, unsigned int *Rshift_ret, unsigned int *type_ret,
    const uint8_t **cycles_ret);

#endif  // OPTIMIZE_VARIABLE_SHIFTS

/*************************************************************************/
/*************************************************************************/

#endif  // SH2_INTERNAL_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
