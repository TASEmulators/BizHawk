/*  src/psp/sh2.c: SH-2 emulator with dynamic translation support
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

/*************************************************************************/

/*
 * This SH-2 emulator is designed to execute SH-2 instructions by
 * recompiling them into equivalent native machine code instructions, then
 * executing that native code directly on the host CPU.
 *
 * The emulation loop (handled by sh2_run()) proceeds as follows:
 *
 * 1) If there is no current native block, sh2_run() calls jit_translate()
 *    to translate a block of SH-2 code beginning at the current PC.
 *    jit_translate() continues translating through the end of the block
 *    (determined heuristically) and returns the translated block to be
 *    executed.
 *
 * 3A) sh2_run() calls jit_exec(), which in turn calls the native code for
 *     the current, found, or created translated block.
 *
 * 3B) If translation failed for some reason, sh2_run() calls
 *     interpret_insn() to interpret and execute one instruction at the
 *     current PC, then starts over at step 1.  If the failure occurred
 *     during RTL code generation (i.e., after the SH-2 block size was
 *     determined), the entire block is blacklisted to avoid repeatedly
 *     trying to translate the block at each subsequent address.
 *
 * 4) The translated code returns either when it reaches the end of the
 *    block, or when the number of cycles executed exceeds the requested
 *    cycle count.  (For efficiency, cycle count checks are only performed
 *    before backward branches.)
 *
 * 5A) If the new PC at the end of the translated block is cached as the
 *     predicted branch target of the block, the current native block is
 *     set to that block.
 *
 * 5B) Otherwise, if a translated block exists for the new PC, it is cached
 *     as the predicted branch target and the current native block is set
 *     to that block.
 *
 * 5C) Otherwise, the current native block is cleared.
 *
 * During native code execution, if a write is made to a region of SH-2
 * memory containing translated blocks, all translated blocks containing
 * that address are deleted by calling jit_clear_write(), so the modified
 * code can be retranslated.
 *
 * If an external agent (such as DMA) modifies memory, sh2_write_notify()
 * should be called to clear any translations in the affected area.  In
 * this case, the modified range is expanded to a multiple of
 * 1<<JIT_PAGE_BITS bytes for efficiency, assuming that such writes will
 * tend to cover relatively large areas rather than single addresses.
 *
 * This code is written to be as portable and general as possible, but to
 * avoid excessive complexity, the code makes a number of references to
 * Saturn-specific features such as memory layout.
 *
 * ------------------------------------------------------------------------
 *
 * Note that a fair amount of voodoo is required to allow tracing of
 * instructions from translated RTL code.  Reading code located between
 * #ifdef TRACE{,_STEALTH} and the corresponding #else or #endif may be
 * dangerous to your health.
 */

/*************************************************************************/
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "../sh2core.h"

#include "rtl.h"
#include "sh2.h"
#include "sh2-internal.h"

#ifdef JIT_DEBUG_TRACE
# include "../sh2d.h"
#endif
#ifdef JIT_DEBUG_INTERPRET_RTL
# include "rtl-internal.h"  // For sizeof(RTL*)
#endif

/*************************************************************************/
/******************* Local data and other declarations *******************/
/*************************************************************************/

/* List of all allocated state blocks (iterated over when clearing a JIT
 * entry or taking other actions that may affect all active processors) */
SH2State *state_list;

/* Bitmask indicating which optional optimizations are enabled */
uint32_t optimization_flags;

/* Callback function for manual/special-case optimization */
SH2OptimizeCallback *manual_optimization_callback;

/* Callback function for native CPU cache flushing */
SH2CacheFlushCallback *cache_flush_callback;

/* Callback function for flushing native CPU caches */
SH2CacheFlushCallback *cache_flush_callback;

/* Callback function for invalid instructions */
SH2InvalidOpcodeCallback *invalid_opcode_callback;

/* Callback functions for tracing */
SH2TraceInsnCallback *trace_insn_callback;
SH2TraceAccessCallback *trace_storeb_callback;
SH2TraceAccessCallback *trace_storew_callback;
SH2TraceAccessCallback *trace_storel_callback;

/*************************************************************************/

#ifdef ENABLE_JIT

/*-----------------------------------------------------------------------*/

/* Array of directly-accessible page pointers, indexed by bits 19-31 of the
 * SH-2 address.  Each pointer is either NULL (meaning the page is not
 * directly accessible) or a value which, when added to the the SH-2
 * address, gives the corresponding native address (ignoring byte order
 * issues).  All pages are assumed to be organized as 16-bit units in
 * native byte order. */
uint8_t *direct_pages[0x2000];

/* Array of executable page pointers, indexed by bits 19-31 of the SH-2
 * address.  All pages are assumed to be organized as 16-bit units in
 * native byte order. */
uint8_t *fetch_pages[0x2000];

/* Array of directly-accessible byte-order page pointers, indexed by bits
 * 19-31 of the SH-2 address.  These pages are organized as 8-bit units, so
 * they cannot be used for opcode fetching. */
uint8_t *byte_direct_pages[0x2000];

/* Array of pointers to JIT page bitmaps, indexed by bits 19-31 of the SH-2
 * address.  Each bitmap (more accurately a "bytemap") contains one byte
 * per JIT page (1<<JIT_PAGE_BITS bytes), whose value is nonzero if the
 * page contains translated code, else zero. */
uint8_t *direct_jit_pages[0x2000];

/* Macros for manipulating JIT page bitmaps */
#define JIT_PAGE_SET(base,page)    ((base)[(page)] = 1)
#define JIT_PAGE_TEST(base,page)   ((base)[(page)])
#define JIT_PAGE_CLEAR(base,page)  ((base)[(page)] = 0)

/*-----------------------------------------------------------------------*/

typedef struct BranchTargetInfo_ BranchTargetInfo;
typedef struct JitEntry_ JitEntry;

/* Data for predicted branch targets (used in JitEntry below) */
struct BranchTargetInfo_ {
    BranchTargetInfo *next, *prev;  // Reference list pointers
    union {
        uint32_t target;            // Target SH-2 address (0 = none)
        void *native_target;        // Native jump target (only if entry!=NULL)
    };
    JitEntry *entry;                // JitEntry containing translated code
};

/* JIT translation data structure */
struct JitEntry_ {
    /* List pointers */
    JitEntry *next, *prev;   // Hash table collision chain / free list pointers
    JitEntry *lra_next, *lra_prev;
                             // Least Recently Added list pointers

    /* Head node for predicted branch target reference list (only the
     * next/prev fields are used here); the list is circularly(*) linked
     * for ease of insertion/deletion.
     * (*) We don't ever treat it as circular, so more accurately, it's a
     *     straight doubly-linked list with sentinels at both ends, where
     *     the same node serves as both head and tail sentinel.  But this
     *     is the same as a circularly linked list, so we call it that for
     *     simplicity.  The same is true of the least-recently-added list.
     */
    BranchTargetInfo pred_ref_head;

    /* Static data */
    uint32_t sh2_start;      // Code start address in SH-2 address space
                             //    (zero indicates a free entry)
    uint32_t sh2_end;        // Code end address in SH-2 address space
    RTLBlock *rtl;           // RTL generated for this block
    void *native_code;       // Pointer to native code
    uint32_t native_length;  // Length of native code (bytes)
    uint32_t timestamp;      // Time this entry was added

    /* Runtime flags */
    uint8_t running;         // Nonzero if entry is currently running
    uint8_t must_clear;      // Nonzero if entry must be cleared on completion

#if JIT_BRANCH_PREDICTION_SLOTS > 0
    /* Predicted dynamic branch target(s) */
    BranchTargetInfo predicted[JIT_BRANCH_PREDICTION_SLOTS];
#endif

#ifdef JIT_BRANCH_PREDICT_STATIC
    /* Predicted static branch target(s) */
    BranchTargetInfo *static_predict;   // Dynamically-allocated array
    uint32_t num_static_branches;       // Number of entries in array
#endif

#ifdef JIT_PROFILE
    uint32_t call_count;     // Number of calls made to this block
    uint32_t cycle_count;    // Number of SH-2 cycles spent in this block
    uint64_t exec_time;      // Time spent executing this block (when
                             //    interpreting RTL, # of insns executed)
#endif
};

/* Hash function */
#define JIT_HASH(addr)  ((uint32_t)(addr) % JIT_TABLE_SIZE)

/*-----------------------------------------------------------------------*/

/* Hash table of translated routines (shared between virtual processors) */
static JitEntry jit_table[JIT_TABLE_SIZE];       // Record buffer
static JitEntry *jit_hashchain[JIT_TABLE_SIZE];  // Hash collision chains

/* Head node for circular linked list of free entries */
static JitEntry jit_freelist;

/* Head node for circular linked list of active entries sorted by add
 * timestamp; used to quickly find the oldest (least recently added) entry
 * when we need to purge entries due to a full table or the data size limit */
static JitEntry jit_lralist;

/* Total size of translated data */
static int32_t jit_total_data; // Signed to protect against underflow from bugs

/* Size limit for translated data */
static int32_t jit_data_limit = JIT_DATA_LIMIT_DEFAULT;

/* Global timestamp for LRU expiration mechanism */
static uint32_t jit_timestamp;

/* Address from which data is being read */
static uint32_t jit_PC;

/* Flags indicating the status of each word in the current block
 * (0: unknown, else a combination of WORD_INFO_* flags) */
static uint8_t word_info[JIT_INSNS_PER_BLOCK];
#define WORD_INFO_CODE      (1<<0)  // 1: this is a code word
#define WORD_INFO_FALLTHRU  (1<<1)  // 1: branch at this address jumps to the
                                    //       next translated instruction
#define WORD_INFO_BRA_RTS   (1<<2)  // 1: branch at this address jumps to a
                                    //       RTS/NOP pair
#define WORD_INFO_THREADED  (1<<3)  // 1: branch at this address jumps to
                                    //       another branch
#define WORD_INFO_SELECT    (1<<4)  // 1: branch at this address acts like
                                    //       a SELECT
#define WORD_INFO_LOOP_JSR  (1<<5)  // 1: branch at this address targets a
                                    //       JSR/BSR/BSRF + delay slot
                                    //       immediately preceding the block
#define WORD_INFO_FOLDABLE  (1<<6)  // 1: BSR/JSR at this address jumps to
                                    //       a foldable subroutine

/* Branch target flag array (indicates which words in the current block
 * are targeted by branches within the same block) */
static uint32_t is_branch_target[(JIT_INSNS_PER_BLOCK+31)/32];

/* Subroutine call target array (used for subroutine folding) */
static uint32_t subroutine_target[JIT_INSNS_PER_BLOCK];

/* Native implementation function pointers for folded subroutines */
static SH2NativeFunctionPointer subroutine_native[JIT_INSNS_PER_BLOCK];

/* Threaded branch target array (indicates targets for branches marked with
 * WORD_INFO_THREADED) */
static uint32_t branch_thread_target[JIT_INSNS_PER_BLOCK];

/* Branch count for each threaded branch (for updating the cycle count) */
static uint8_t branch_thread_count[JIT_INSNS_PER_BLOCK];

/* Branch target lookup table (indicates where in the native code each
 * address is located) */
static struct {
    uint32_t sh2_address;   // Address of SH-2 instruction
    uint32_t rtl_label;     // RTL label number corresponding to instruction
} btcache[JIT_BTCACHE_SIZE];
static unsigned int btcache_index;  // Where to store the next branch target

/* Unresolved branch list (saves locations and targets of forward branches) */
static struct {
    uint32_t sh2_target;    // Branch target (SH-2 address, 0 = unused entry)
    uint32_t target_label;  // RTL label number to be defined at branch target
} unres_branches[JIT_UNRES_BRANCH_SIZE];

/* JIT translation blacklist; note that ranges are always 16-bit aligned */
static struct {
    uint32_t start, end;    // Zero in both fields indicates an empty entry
    uint32_t timestamp;     // jit_timestamp when region was last written to
    uint32_t pad;           // Pad entry size to 16 bytes
} blacklist[JIT_BLACKLIST_SIZE];

/* Table of addresses that may require blacklisting if they cause repeated
 * faults (see jit_clear_write()); we use the purge table constants for
 * this table because they are used for similar purposes */
static struct {
    uint32_t address;   // Access address address (0 = unused)
    uint32_t timestamp; // jit_timestamp when entry was added/updated
    uint32_t count;     // Number of jit_clear_write() faults on this block
} pending_blacklist[JIT_PURGE_TABLE_SIZE];

/* JIT purge table */
static struct {
    uint32_t address;   // Block starting address (0 = unused)
    uint32_t timestamp; // jit_timestamp when entry was added/updated
    uint32_t count;     // Number of purges on this block
} purge_table[JIT_PURGE_TABLE_SIZE];

/*----------------------------------*/

/* Should we ignore optimization hints?  (Used when checking for
 * optimizable subroutines to be folded.) */
static uint8_t ignore_optimization_hints;

/*----------------------------------*/

/* True if we can optimize out the MAC saturation check in this block
 * (i.e. S was clear on entry and has not been modified) */
static uint8_t can_optimize_mac_nosat;

/* True if the current block contains any MAC instructions */
static uint8_t block_contains_mac;

/*----------------------------------*/

/* Bitmask of registers hinted as having constant values on block entry */
static uint16_t is_constant_entry_reg;

/*----------------------------------*/

#ifdef OPTIMIZE_KNOWN_VALUES

/* Known state of registers (used for optimization when recompiling) */
static uint32_t reg_knownbits[17];// 1 in a bit means that bit's value is known
static uint32_t reg_value[17];    // Value of known bits in each register
#define REG_R(n)  (n)
#define REG_GBR   16
/* PC is always constant when translating, and the remaining registers are
 * generally unknown, so we don't waste time tracking them. */

#endif  // OPTIMIZE_KNOWN_VALUES

/*----------------------------------*/

/* Status of each register when viewed as a load/store address */

static struct {

    /*
     * Status of the register:
     *    known == 0: Value is not a translation-time constant.
     *    known != 0: Value is within a known memory region.
     *       known > 0: Value has not significantly changed since the start
     *                     of the block.
     *       known < 0: Value has been copied from another register
     *                     pointing to a known memory region, or has been
     *                     loaded from local data.
     */
    int8_t known;

    /*
     * Nonzero if this register is known to point to only data, and
     * therefore the JIT overwrite checks can be skipped.
     */
    uint8_t data_only;

    /*
     * Type of memory pointed to by the register:
     *    1: Arranged by 8-bit bytes.
     *    2: Arranged by native 16-bit words.
     */
    uint8_t type;

    /*
     * For known == 0 && rtl_basereg != 0 && !data_only, nonzero if the
     * direct_jit_pages[] entry for this pointer has already been checked,
     * else zero.
     */
    uint8_t checked_djp;

    /*
     * Nonzero if the "check_offset" field is valid.
     */
    uint8_t checked;

    /*
     * Last offset at which a JIT check was performed.
     */
    int16_t check_offset;

    /*
     * RTL register containing the base pointer for direct memory access,
     * to which the register's value is added to form a native address for
     * loads and stores.
     *
     * For known == 0:
     *    - rtl_basereg==0 means the register has not yet been used as a
     *      pointer, or its value has changed since the last such use.
     *    - rtl_basereg!=0 means the register has already been used as a
     *      pointer.  If the RTL register's value is nonzero, it is a
     *      native pointer through which accesses can be performed
     *      directly; if the value is zero, the access must go through
     *      the fallback functions.
     *
     * For known != 0:
     *    - rtl_basereg==0 means the access must go through the fallback
     *      functions.
     *    - rtl_basereg!=0 means the access can be performed directly; the
     *      RTL register holds the base address to be added to the register
     *      value.
     */
    uint16_t rtl_basereg;

    /*
     * RTL register containing a native pointer for direct memory access
     * (equal to rtl_basereg plus the register's value), or zero if no
     * such register has yet been created.  Only used if known != 0.
     */
    uint16_t rtl_ptrreg;

    /*
     * Base pointer for JIT page bitmap.  Only used for statically-known,
     * 16-bit, non-data pointers (known > 0 && type == 2 && !data_only).
     */
    uint8_t *djp_base;

    /*
     * Source address if this register was loaded from local data.
     */
    uint32_t source;

} pointer_status[16];

/* Register holding the native address corresponding to the stack pointer
 * (R15) when SH2_OPTIMIZE_STACK is defined */
static uint16_t stack_pointer;

/* Bitmask of general-purpose registers containing values which could be
 * pointers (used at scanning time to determine which registers need to be
 * checked against the constant memory region used in optimization) */
static uint16_t pointer_regs;
/* Bitmask of registers which are actually used to access memory */
static uint16_t pointer_used;

/* Bitmask of general-purpose registers containing local data addresses */
static uint16_t pointer_local;

/* Bitmask of general-purpose registers hinted to contain data pointers */
static uint16_t is_data_pointer_reg;

/* Array of flags indicating instructions which load data pointers
 * (optimization hint) */
static uint32_t is_data_pointer_load[(JIT_INSNS_PER_BLOCK+31)/32];

/*----------------------------------*/

#ifdef OPTIMIZE_STATE_BLOCK

/* Array indicating which SH-2 state block fields are cached in RTL
 * registers; if the rtlreg field is nonzero, the field should be accessed
 * through that RTL register rather than being loaded from the state block.
 * Indexed by the word offset into SH2State, i.e. offsetof(SH2State,REG) / 4
 * The index for a given structure offset can be found by calling
 * state_cache_index(offsetof(...)), which returns -1 if the given field
 * cannot be cached. */
static struct {
    uint32_t rtlreg;  // RTL register containing the SH-2 reg's current value
    int16_t offset;   // Accumulated offset from constant additions (always
                      //    zero if !OPTIMIZE_CONSTANT_ADDS)
    uint8_t fixed;    // Nonzero if we should always use this RTL register
                      //    for this state field (rather than allocating a
                      //    new register for each operation)
    uint8_t flush;    // Nonzero if we should always flush this cache
                      //    register to memory during cache writeback
                      //    (rather than only when it's marked dirty)
} state_cache[25],
  saved_state_cache[25];  // Saved cache state (used by SAVE_STATE_CACHE())

/* Flags indicating whether a state block field is dirty (needs to be
 * flushed); each bit corresponds to a state_cache[] index */
static uint32_t state_dirty, saved_state_dirty;

/* Register containing current value of SR.T, or 0 if none */
static uint32_t cached_SR_T, saved_cached_SR_T;

/* Nonzero if SR.T is dirty */
static uint8_t dirty_SR_T, saved_dirty_SR_T;

/* Cached known value of state->PC (overrides state_cache if nonzero) */
static uint32_t cached_PC, saved_cached_PC;

/* Last value actually stored to state->PC */
static uint32_t stored_PC, saved_stored_PC;

/* Function to convert a state block offset to a state_cache[] index */
#ifdef __GNUC__
__attribute__((const))
#endif
static inline int state_cache_index(const uint32_t offset) {
    if (offset < 25*4) {
        return offset / 4;
    } else {
        return -1;
    }
}

/* Function to convert a state_cache[] index to a state block offset */
#ifdef __GNUC__
__attribute__((const))
#endif
static inline uint32_t state_cache_field(const int index) {
    return index * 4;
}

#endif  // OPTIMIZE_STATE_BLOCK

/*----------------------------------*/

#ifdef OPTIMIZE_LOOP_REGISTERS

/* Is this block a loop that can be optimized? */
static uint8_t can_optimize_loop;

/* Bitmask of registers live in the loop (indexed by state cache index) */
static uint32_t loop_live_registers;

/* Bitmask of registers whose initial values are used in the loop, and which
 * therefore must be loaded ahead of time (indexed by state cache index) */
static uint32_t loop_load_registers;

/* Bitmask of registers changed in the loop (indexed by state cache index) */
static uint32_t loop_changed_registers;

/* Bitmask of registers which contain invariant pointers, i.e. pointers
 * which are unchanged (or only offset by predecrement/postincrement
 * addressing or ADD #imm) throughout the body of a loop */
static uint32_t loop_invariant_pointers;

#endif  // OPTIMIZE_LOOP_REGISTERS

/*-----------------------------------------------------------------------*/

#endif  // ENABLE_JIT

/*************************************************************************/

/* Local function declarations */

#ifdef ENABLE_JIT  // Through the function declarations

static JitEntry *jit_find(uint32_t address);
static NOINLINE JitEntry *jit_find_noinline(uint32_t address);

static NOINLINE JitEntry *jit_translate(SH2State *state, uint32_t address);

#if defined(PSP) && !defined(JIT_DEBUG_INTERPRET_RTL)
__attribute__((unused))  // We use custom assembly in this case
#endif
static inline void jit_exec(SH2State *state, JitEntry *entry);

static FASTCALL void jit_clear_write(SH2State *state, uint32_t address);
static void jit_clear_range(uint32_t address, uint32_t size);
static void jit_clear_all(void);
static void jit_blacklist_range(uint32_t start, uint32_t end);

static FASTCALL void jit_mark_purged(uint32_t address);
static int jit_check_purged(uint32_t address);

#if JIT_BRANCH_PREDICTION_SLOTS > 0
static NOINLINE JitEntry *jit_predict_branch(BranchTargetInfo *predicted,
                                             const uint32_t target);
#endif

#ifdef JIT_PROFILE
static NOINLINE void jit_print_profile(void);
#endif

static NOINLINE void clear_entry(JitEntry *entry);
static void clear_oldest_entry(void);

static inline void flush_native_cache(void *start, uint32_t length);

__attribute__((const)) static inline int timestamp_compare(
    uint32_t reference, uint32_t a, uint32_t b);

/*----------------------------------*/

static int translate_block(SH2State *state, JitEntry *entry);
static int setup_pointers(SH2State * const state, JitEntry * const entry,
                          const unsigned int state_reg,
                          const int skip_preconditions,
                          unsigned int * const invalidate_label_ptr);

static int scan_block(SH2State *state, JitEntry *entry, uint32_t address);
static inline void optimize_pointers(
    uint32_t start_address, uint32_t address, unsigned int opcode,
    uint32_t opcode_info, uint8_t pointer_map[16]);
static inline void optimize_pointers_mac(
    uint32_t address, unsigned int opcode, uint8_t pointer_map[16],
    uint32_t last_clrmac, int *stop_ret, int *rollback_ret);
#ifdef OPTIMIZE_LOOP_REGISTERS
static inline void check_loop_registers(
    unsigned int opcode, uint32_t opcode_info);
#endif
#ifdef OPTIMIZE_BRANCH_SELECT
static inline int optimize_branch_select(
    uint32_t start_address, uint32_t address, const uint16_t *fetch,
    unsigned int opcode, uint32_t target);
#endif
static int optimize_fold_subroutine(
    SH2State *state, const uint32_t start_address, uint32_t address,
    const uint32_t target, const uint16_t delay_slot, uint8_t pointer_map[16],
    uint32_t local_constant[16], SH2NativeFunctionPointer *native_ret);

static int translate_insn(SH2State *state, JitEntry *entry,
                          unsigned int state_reg, int recursing, int is_last);
#ifdef JIT_BRANCH_PREDICT_STATIC
static int add_static_branch_terminator(JitEntry *entry,
                                        unsigned int state_reg,
                                        uint32_t address, unsigned int index);
#endif
#if JIT_BRANCH_PREDICTION_SLOTS > 0
static FASTCALL JitEntry *update_branch_target(BranchTargetInfo *bti,
                                               uint32_t address);
#endif

/*----------------------------------*/

static uint32_t btcache_lookup(uint32_t address);
static int record_unresolved_branch(const JitEntry *entry, uint32_t sh2_target,
                                    unsigned int target_label);

static int writeback_state_cache(const JitEntry *entry, unsigned int state_reg,
                                 int flush_fixed);
static void clear_state_cache(int clear_fixed);

#endif  // ENABLE_JIT

/*-----------------------------------------------------------------------*/

/* Dummy, do-nothing callback function used as a default trace callback
 * so that the caller doesn't have to check for a NULL function pointer */
static void dummy_callback(void) {}

/*************************************************************************/
/********************** External interface routines **********************/
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
int sh2_init(void)
{
    /* Set up the opcode information tables */
    init_opcode_info();

    /* Default to no optimizations; let the caller decide what to enable */
    optimization_flags = 0;

    /* Set the dummy callback function in the trace callback pointers */
    trace_insn_callback = (SH2TraceInsnCallback *)dummy_callback;
    trace_storeb_callback = (SH2TraceAccessCallback *)dummy_callback;
    trace_storew_callback = (SH2TraceAccessCallback *)dummy_callback;
    trace_storel_callback = (SH2TraceAccessCallback *)dummy_callback;

    return 1;
}

/*-----------------------------------------------------------------------*/

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
void sh2_set_optimizations(uint32_t flags)
{
    if (flags != optimization_flags) {
#ifdef ENABLE_JIT
        jit_clear_all();
#endif
        optimization_flags = flags;
        if (!(optimization_flags & SH2_OPTIMIZE_LOCAL_ACCESSES)) {
            optimization_flags &= ~SH2_OPTIMIZE_LOCAL_POINTERS;
        }
        if (!(optimization_flags & SH2_OPTIMIZE_POINTERS)) {
            optimization_flags &= ~SH2_OPTIMIZE_LOCAL_POINTERS;
            optimization_flags &= ~SH2_OPTIMIZE_POINTERS_MAC;
        }
    }
}

uint32_t sh2_get_optimizations(void)
{
    return optimization_flags;
}

/*-----------------------------------------------------------------------*/

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
void sh2_set_jit_data_limit(uint32_t limit)
{
#ifdef ENABLE_JIT
    jit_data_limit = limit;
#endif
}

/*-----------------------------------------------------------------------*/

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
void sh2_set_manual_optimization_callback(SH2OptimizeCallback *funcptr)
{
    manual_optimization_callback = funcptr;
}

/*----------------------------------*/

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
void sh2_set_cache_flush_callback(SH2CacheFlushCallback *funcptr)
{
    cache_flush_callback = funcptr;
}

/*----------------------------------*/

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
void sh2_set_invalid_opcode_callback(SH2InvalidOpcodeCallback *funcptr)
{
    invalid_opcode_callback = funcptr;
}

/*----------------------------------*/

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
void sh2_set_trace_insn_callback(SH2TraceInsnCallback *funcptr)
{
    trace_insn_callback =
        funcptr ? funcptr : (SH2TraceInsnCallback *)dummy_callback;
}

/*----------------------------------*/

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
void sh2_set_trace_storeb_callback(SH2TraceAccessCallback *funcptr)
{
    trace_storeb_callback =
        funcptr ? funcptr : (SH2TraceAccessCallback *)dummy_callback;
}

void sh2_set_trace_storew_callback(SH2TraceAccessCallback *funcptr)
{
    trace_storew_callback =
        funcptr ? funcptr : (SH2TraceAccessCallback *)dummy_callback;
}

void sh2_set_trace_storel_callback(SH2TraceAccessCallback *funcptr)
{
    trace_storel_callback =
        funcptr ? funcptr : (SH2TraceAccessCallback *)dummy_callback;
}


/*-----------------------------------------------------------------------*/

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
void *sh2_alloc_direct_write_buffer(uint32_t size)
{
    const uint32_t bufsize =
        (size + ((1<<JIT_PAGE_BITS) - 1)) >> JIT_PAGE_BITS;
    void *buffer = calloc(bufsize, 1);
    if (UNLIKELY(!buffer)) {
        DMSG("Failed to get 0x%X-byte buffer for 0x%X-byte region",
             bufsize, size);
        return NULL;
    }
    return buffer;
}

/*-----------------------------------------------------------------------*/

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
void sh2_set_direct_access(uint32_t sh2_address, void *native_address,
                           uint32_t size, int readable, void *write_buffer)
{
#ifdef ENABLE_JIT
    if (UNLIKELY(((sh2_address | size) & ((1<<19)-1)) != 0)) {
        DMSG("sh2_address (0x%08X) and size (0x%X) must be aligned to a"
             " 19-bit page", sh2_address, size);
        return;
    }

    const unsigned int first_page = sh2_address >> 19;
    const unsigned int last_page = ((sh2_address + size) >> 19) - 1;
    if (native_address) {
        void * const target =
            (void *)((uintptr_t)native_address - (first_page << 19));
        void * const jit_target = write_buffer
            ? (void *)((uintptr_t)write_buffer
                       - (first_page << (19 - JIT_PAGE_BITS)))
            : NULL;
        unsigned int page;
        for (page = first_page; page <= last_page; page++) {
            fetch_pages[page] = target;
            direct_pages[page] = readable ? target : NULL;
            direct_jit_pages[page] = jit_target;
        }
    } else {
        unsigned int page;
        for (page = first_page; page <= last_page; page++) {
            fetch_pages[page] = NULL;
            direct_pages[page] = NULL;
            direct_jit_pages[page] = NULL;
        }
    }
#endif  // ENABLE_JIT
}

/*-----------------------------------------------------------------------*/

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
void sh2_set_byte_direct_access(uint32_t sh2_address, void *native_address,
                                uint32_t size)
{
#ifdef ENABLE_JIT
    if (UNLIKELY(((sh2_address | size) & ((1<<19)-1)) != 0)) {
        DMSG("sh2_address (0x%08X) and size (0x%X) must be aligned to a"
             " 19-bit page", sh2_address, size);
        return;
    }

    const unsigned int first_page = sh2_address >> 19;
    const unsigned int last_page = ((sh2_address + size) >> 19) - 1;
    if (native_address) {
        void * const target =
            (void *)((uintptr_t)native_address - (first_page << 19));
        unsigned int page;
        for (page = first_page; page <= last_page; page++) {
            byte_direct_pages[page] = target;
        }
    } else {
        unsigned int page;
        for (page = first_page; page <= last_page; page++) {
            byte_direct_pages[page] = NULL;
        }
    }
#endif  // ENABLE_JIT
}

/*-----------------------------------------------------------------------*/

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
 * the manual optimization callback, or of specifying an instruction other
 * than MOVA which does not set the register specified by the Rn field of
 * the opcode, is undefined.
 *
 * [Parameters]
 *       state: Processor state block pointer passed to callback function
 *     address: Address of load instruction to mark as loading a data pointer
 * [Return value]
 *     None
 */
void sh2_optimize_hint_data_pointer_load(SH2State *state, uint32_t address)
{
    PRECOND(state != NULL, return);
    PRECOND(state->translate_entry != NULL, return);

#ifdef ENABLE_JIT

    if (ignore_optimization_hints) {
        return;
    }
    if (!(optimization_flags & SH2_OPTIMIZE_POINTERS)) {
        return;
    }

    if (address >= state->translate_entry->sh2_start) {
        const unsigned int insn_index =
            (address - state->translate_entry->sh2_start) / 2;
        if (insn_index < lenof(is_data_pointer_load) * 32) {
            is_data_pointer_load[insn_index / 32] |= 1 << (insn_index % 32);
        }
    }

#endif  // ENABLE_JIT
}

/*----------------------------------*/

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
void sh2_optimize_hint_data_pointer_register(SH2State *state,
                                             unsigned int regnum)
{
    PRECOND(state != NULL, return);
    PRECOND(state->translate_entry != NULL, return);
    PRECOND(regnum < 16, return);

#ifdef ENABLE_JIT

    if (ignore_optimization_hints) {
        return;
    }
    if (!(optimization_flags & SH2_OPTIMIZE_POINTERS)) {
        return;
    }

    is_data_pointer_reg |= 1 << regnum;

#endif  // ENABLE_JIT
}

/*----------------------------------*/

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
void sh2_optimize_hint_constant_register(SH2State *state, unsigned int regnum)
{
    PRECOND(state != NULL, return);
    PRECOND(state->translate_entry != NULL, return);
    PRECOND(regnum < 16, return);

#ifdef ENABLE_JIT

    if (ignore_optimization_hints) {
        return;
    }

    is_constant_entry_reg |= 1 << regnum;

#endif // ENABLE_JIT
}

/*************************************************************************/

/**
 * sh2_create:  Create a new virtual SH-2 processor.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     SH-2 processor state block (NULL on error)
 */
SH2State *sh2_create(void)
{
    SH2State *state;

    state = malloc(sizeof(*state));
    if (!state) {
        DMSG("Out of memory allocating state block");
        goto error_return;
    }
    state->next = state_list;
    state_list = state;

    /* Allocate interrupt stack */
    state->interrupt_stack =
        malloc(sizeof(*state->interrupt_stack) * INTERRUPT_STACK_SIZE);
    if (!state->interrupt_stack) {
        DMSG("Out of memory allocating interrupt stack");
        goto error_free_state;
    }

    return state;

  error_free_state:
    free(state);
  error_return:
    return NULL;
}

/*-----------------------------------------------------------------------*/

/**
 * sh2_destroy:  Destroy a virtual SH-2 processor.
 *
 * [Parameters]
 *     state: SH-2 processor state block
 * [Return value]
 *     None
 */
void sh2_destroy(SH2State *state)
{
    if (!state) {
        return;
    }

    SH2State **prev_ptr;
    for (prev_ptr = &state_list; *prev_ptr; prev_ptr = &((*prev_ptr)->next)) {
        if ((*prev_ptr) == state) {
            break;
        }
    }
    if (!*prev_ptr) {
        DMSG("%p not found in state list", state);
        return;
    }
    (*prev_ptr) = state->next;

    free(state->interrupt_stack);
    free(state);
}

/*-----------------------------------------------------------------------*/

/**
 * sh2_reset:  Reset a virtual SH-2 processor.
 *
 * [Parameters]
 *     state: SH-2 processor state block
 * [Return value]
 *     None
 */
void sh2_reset(SH2State *state)
{
    state->delay = 0;
    state->asleep = 0;
    state->need_interrupt_check = 0;
    state->interrupt_stack_top = 0;
    state->current_entry = NULL;
    state->branch_type = SH2BRTYPE_NONE;
    state->folding_subroutine = 0;
    state->pending_select = 0;
    state->just_branched = 0;
    state->cached_shift_count = 0;
    state->varshift_target_PC = 0;
    state->division_target_PC = 0;

#ifdef ENABLE_JIT
    memset(blacklist, 0, sizeof(blacklist));
    memset(pending_blacklist, 0, sizeof(pending_blacklist));
    memset(purge_table, 0, sizeof(purge_table));
    jit_clear_all();
    jit_timestamp = 0;
#endif
}

/*************************************************************************/

/**
 * sh2_run:  Execute instructions for the given number of clock cycles.
 *
 * [Parameters]
 *      state: SH-2 processor state block
 *     cycles: Number of clock cycles to execute
 * [Return value]
 *     None
 */
#ifdef PSP
__attribute__((aligned(64)))
#endif
void sh2_run(SH2State *state, uint32_t cycles)
{
    /* Update the JIT timestamp counter once per call */
    jit_timestamp++;

    /* Save this in the state block for use by translated code */
    state->cycle_limit = cycles;

    /* Check for interrupts before we start executing (this will wake up
     * the processor if necessary) */
    if (UNLIKELY(check_interrupts(state))) {
#ifdef ENABLE_JIT
        state->current_entry = jit_find(state->PC);
#endif
    }

    /* If the processor is sleeping, consume all remaining cycles */
    if (UNLIKELY(state->asleep)) {
        state->cycles = state->cycle_limit;
        return;
    }

    /* In TRACE_LITE (but not TRACE_LITE_VERBOSE) mode, trace a single
     * instruction at the current PC before we start executing */
#if defined(TRACE_LITE) && !defined(TRACE_LITE_VERBOSE)
    (*trace_insn_callback)(state, state->PC);
#endif

    /* Load this for faster access during the loop */
    JitEntry *current_entry = state->current_entry;

    /* Loop until we've consumed the requested number of execution cycles.
     * There's no need to check state->delay here, because it's handled
     * either at translation time or by interpret_insn() call. */

    const uint32_t cycle_limit = state->cycle_limit;

#if !defined(ENABLE_JIT)

    while (state->cycles < cycle_limit) {
        interpret_insn(state);
    }

#else  // ENABLE_JIT

# if defined(PSP) && !defined(JIT_DEBUG_INTERPRET_RTL)

    /* GCC's optimizer fails horribly on this loop, so we have no choice
     * but to write it ourselves. */

#  ifdef JIT_PROFILE
    uint32_t start_cycles, start;  // As in jit_exec()
#  endif

    asm(".set push; .set noreorder\n"

        "lw $t0, %[cycles](%[state])\n"
        "beqz %[current_entry], 5f\n"
        "sltu $v0, $t0, %[cycle_limit]\n"
        /* In the non-trace, non-profile case, this NOP aligns the top of
         * the loop to a 64-byte (cache-line) boundary, so that the quick-
         * predict critical path fits within a single cache line.  This
         * provides a noticeable (sometimes >5%) boost to performance. */
        "nop\n"

        // Loop top, quick restart point (jit_translate() call is at the end)
        "1:\n"
        "beqz $v0, 9f\n"

        // Resume point after block translation
        "2:\n"
#  ifdef TRACE_LITE_VERBOSE
        "move $a0, %[state]\n"
        "jalr %[trace_insn_callback]\n"
        "lw $a1, %[PC](%[state])\n"
#  endif

        // jit_exec(state, current_entry);
#  ifdef JIT_PROFILE
#   ifndef TRACE_LITE_VERBOSE
        "nop\n"  // Fill branch delay slot from above
#   endif
        "jal sceKernelGetSystemTimeLow\n"
        "lw %[start_cycles], %[cycles](%[state])\n"
        "move %[start], $v0\n"
#  endif

        "lw $v0, %[native_code](%[current_entry])\n"
        "li $v1, 1\n"
        "sb $v1, %[running](%[current_entry])\n"
        "jalr $v0\n"
        "move $a0, %[state]\n"

#  ifdef JIT_PROFILE
        "jal sceKernelGetSystemTimeLow\n"
        "nop\n"
        "lw $v1, %[cycles](%[state])\n"
        "lw $a0, %[call_count](%[current_entry])\n"
        "lw $a1, %[cycle_count](%[current_entry])\n"
        "lw $a2, %[exec_time](%[current_entry])\n"
        "lw $a3, %[exec_time]+4(%[current_entry])\n"
        "addiu $a0, $a0, 1\n"
        "subu $v1, $v1, %[start_cycles]\n"
        "addu $a1, $a1, $v1\n"
        "subu $v0, $v0, %[start]\n"
        "addu $v0, $a2, $v0\n"
        "sltu $v1, $v0, $a2\n"
        "addu $a3, $a3, $v1\n"
        "sw $a0, %[call_count](%[current_entry])\n"
        "sw $a1, %[cycle_count](%[current_entry])\n"
        "sw $v0, %[exec_time](%[current_entry])\n"
        "sw $a3, %[exec_time]+4(%[current_entry])\n"
#  endif

        // Preload data used below
        "lbu $v0, %[must_clear](%[current_entry])\n"
        "lw $t0, %[cycles](%[state])\n"
#  if JIT_BRANCH_PREDICTION_SLOTS > 0
        "lw $a1, %[PC](%[state])\n"
        "lw $t1, %[predicted_target](%[current_entry])\n"
#  endif

        // Clear current_entry->running and check must_clear
        "bnez $v0, 8f\n"
        "sb $zero, %[running](%[current_entry])\n"

#  if JIT_BRANCH_PREDICTION_SLOTS > 0
        // Check for a quickly-predicted branch
        "sltu $v0, $t0, %[cycle_limit]\n"
        "beql $a1, $t1, 1b\n"
        "lw %[current_entry], %[predicted_entry](%[current_entry])\n"
        // Otherwise call jit_predict_branch() to get the next block
        "jal %[jit_predict_branch]\n"
        "addiu $a0, %[current_entry], %[predicted]\n"
#  else
        // Branch prediction is disabled, so just look up a block manually
        "jal %[jit_find_noinline]\n"
        "lw $a0, %[PC](%[state])\n"
#  endif
        // Loop back if we found a block, else translate it
        "3:\n"
        "lw $t0, %[cycles](%[state])\n"
        "4:\n"
        "move %[current_entry], $v0\n"
        "bnez %[current_entry], 1b\n"
        "sltu $v0, $t0, %[cycle_limit]\n"

        // Cycle check and jit_translate() call for not-found blocks
        "5:\n"
        "beqz $v0, 9f\n"
        "move $a0, %[state]\n"
        "6:\n"
        "jal %[jit_translate]\n"
        "lw $a1, %[PC](%[state])\n"
        "bnez $v0, 2b\n"
        "move %[current_entry], $v0\n"

        // interpret_insn() call for translation failure
        "jal interpret_insn\n"
        "move $a0, %[state]\n"
        "jal %[jit_find_noinline]\n"
        "lw $a0, %[PC](%[state])\n"
        "b 4b\n"
        "lw $t0, %[cycles](%[state])\n"

        // clear_entry() and jit_find_noinline() for purged blocks
        "8:\n"
        "jal %[clear_entry]\n"
        "move $a0, %[current_entry]\n"
        "jal %[jit_find_noinline]\n"
        "lw $a0, %[PC](%[state])\n"
        "b 4b\n"
        "lw $t0, %[cycles](%[state])\n"

        // End of loop
        "9:\n"
        ".set pop"
        : [current_entry] "=r" (current_entry),
#  ifdef JIT_PROFILE
          [start_cycles] "=&r" (start_cycles),
          [start] "=&r" (start),
#  endif
          "=m" (*state)
        : [state] "r" (state), [cycle_limit] "r" (cycle_limit),
          "0" (current_entry),
#  ifdef TRACE_LITE_VERBOSE
          [trace_insn_callback] "r" (trace_insn_callback),
#  endif
          [PC] "i" (offsetof(SH2State,PC)),
          [cycles] "i" (offsetof(SH2State,cycles)),
          [native_code] "i" (offsetof(JitEntry,native_code)),
          [running] "i" (offsetof(JitEntry,running)),
          [must_clear] "i" (offsetof(JitEntry,must_clear)),
#  if JIT_BRANCH_PREDICTION_SLOTS > 0
          [predicted] "i" (offsetof(JitEntry,predicted)),
          [predicted_target] "i" (offsetof(JitEntry,predicted[0].target)),
          [predicted_entry] "i" (offsetof(JitEntry,predicted[0].entry)),
#  endif
#  ifdef JIT_PROFILE
          [call_count] "i" (offsetof(JitEntry,call_count)),
          [cycle_count] "i" (offsetof(JitEntry,cycle_count)),
          [exec_time] "i" (offsetof(JitEntry,exec_time)),
#  endif
#  if JIT_BRANCH_PREDICTION_SLOTS > 0
          [jit_predict_branch] "S" (jit_predict_branch),
#  endif
          [jit_translate] "S" (jit_translate),
          [clear_entry] "S" (clear_entry),
          [jit_find_noinline] "S" (jit_find_noinline)
        : "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1",
          "t2", "t3", "t4", "t5", "t6", "t7", "t8", "t9", "ra"
    );

# else  // !(PSP && !JIT_DEBUG_INTERPRET_RTL)

    while (state->cycles < cycle_limit) {

        /* If we don't have a current native code block, translate from
         * the current address */
        if (UNLIKELY(!current_entry)) {
            current_entry = jit_translate(state, state->PC);
        }

        /* If we (now) have a native code block, execute from it */
        if (LIKELY(current_entry)) {
          quick_restart:;

# ifdef TRACE_LITE_VERBOSE
            (*trace_insn_callback)(state, state->PC);
# endif
            jit_exec(state, current_entry);

# if JIT_BRANCH_PREDICTION_SLOTS > 0
            const uint32_t new_PC = state->PC;
            BranchTargetInfo * const predicted = &current_entry->predicted[0];

            if (UNLIKELY(current_entry->must_clear)) {
                /* A purge was requested, so clear the just-executed entry */
                clear_entry(current_entry);
                current_entry = jit_find_noinline(new_PC);
                continue;
            }

            /* Even if we're using multiple prediction slots, handle the
             * first one specially for increased speed if it's a correct
             * prediction; we make the safe (for all practical purposes)
             * assumption that the target PC is not zero */
            if (new_PC == predicted->target) {
                current_entry = predicted->entry;
                if (LIKELY(state->cycles < cycle_limit)) {
                    goto quick_restart;
                }
            } else {
                current_entry = jit_predict_branch(predicted, new_PC);
            }
# else  // branch prediction disabled
            if (UNLIKELY(current_entry->must_clear)) {
                clear_entry(current_entry);
            }
            current_entry = jit_find_noinline(state->PC);
# endif

        } else {  // !current_entry

            /* We couldn't translate the code at this address, so interpret
             * it instead */
            interpret_insn(state);

        }  // if (current_entry)

    }  // while (state->cycles < cycle_limit)

# endif  // PSP && !JIT_DEBUG_INTERPRET_RTL
#endif  // ENABLE_JIT

    state->current_entry = current_entry;

#if defined(ENABLE_JIT) && defined(JIT_PROFILE)
    static int cycles_for_profile;
    cycles_for_profile += state->cycle_limit;
    if (cycles_for_profile >= JIT_PROFILE_INTERVAL) {
        cycles_for_profile = 0;
        jit_print_profile();
    }  // if (cycles_for_profile >= JIT_PROFILE_INTERVAL)
#endif  // ENABLE_JIT && JIT_PROFILE
}

/*************************************************************************/

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
void sh2_signal_interrupt(SH2State *state, unsigned int vector,
                          unsigned int level)
{
    int i;

    if (UNLIKELY(state->interrupt_stack_top >= INTERRUPT_STACK_SIZE)) {
        DMSG("WARNING: dropped interrupt <%u,%u> due to full stack",
             vector, level);
        DMSG("Interrupt stack:");
        for (i = state->interrupt_stack_top - 1; i >= 0; i--) {
            DMSG("   <%3u,%2u>", state->interrupt_stack[i].vector,
                 state->interrupt_stack[i].level);
        }
        return;
    }

    for (i = 0; i < state->interrupt_stack_top; i++) {
        if (state->interrupt_stack[i].vector == vector) {
            return;  // This interrupt is already raised
        }
    }

    for (i = 0; i < state->interrupt_stack_top; i++) {
        if (state->interrupt_stack[i].level > level) {
            memmove(&state->interrupt_stack[i+1], &state->interrupt_stack[i],
                    sizeof(*state->interrupt_stack)
                        * (state->interrupt_stack_top - i));
            break;
        }
    }
    state->interrupt_stack[i].level = level;
    state->interrupt_stack[i].vector = vector;
    state->interrupt_stack_top++;
}

/*************************************************************************/

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
void sh2_write_notify(uint32_t address, uint32_t size)
{
#ifdef ENABLE_JIT
    jit_clear_range(address, size);
#endif
}

/*************************************************************************/
/******************** General-purpose local routines *********************/
/*************************************************************************/

/**
 * check_interrupts:  Check whether there are any pending interrupts, and
 * service the highest-priority one if so.
 *
 * [Parameters]
 *     state: Processor state block
 * [Return value]
 *     Nonzero if an interrupt was serviced, else zero
 * [Notes]
 *     This routine is exported for use by sh2-interpret.c.
 */
FASTCALL int check_interrupts(SH2State *state)
{
    if (state->interrupt_stack_top > 0) {
        const int level =
            state->interrupt_stack[state->interrupt_stack_top-1].level;
        if (level > ((state->SR & SR_I) >> SR_I_SHIFT)) {
            const int vector =
                state->interrupt_stack[state->interrupt_stack_top-1].vector;
            state->R[15] -= 4;
#if defined(TRACE) || defined(TRACE_STEALTH)
            (*trace_storel_callback)(state->R[15], state->SR);
#endif
            MappedMemoryWriteLong(state->R[15], state->SR);
            state->R[15] -= 4;
#if defined(TRACE) || defined(TRACE_STEALTH)
            (*trace_storel_callback)(state->R[15], state->PC);
#endif
            MappedMemoryWriteLong(state->R[15], state->PC);
            state->SR &= ~SR_I;
            state->SR |= level << SR_I_SHIFT;
            state->PC = MappedMemoryReadLong(state->VBR + (vector << 2));
            state->asleep = 0;
            state->interrupt_stack_top--;
            return 1;
        }
    }
    return 0;
}

/*************************************************************************/
/**************** Dynamic translation management routines ****************/
/*************************************************************************/

#ifdef ENABLE_JIT  // Through the end of the file

/*************************************************************************/

/**
 * jit_find, jit_find_noinline:  Find the translated block for a given
 * address, if any.  jit_find_noinline() is explicitly marked NOINLINE to
 * minimize register pressure when called from sh2_run().
 *
 * [Parameters]
 *     address: Start address in SH-2 address space
 * [Return value]
 *     Translated block, or NULL if no such block exists
 */
static JitEntry *jit_find(uint32_t address)
{
    const int hashval = JIT_HASH(address);
    JitEntry *entry = jit_hashchain[hashval];
    while (entry) {
        if (entry->sh2_start == address) {
            return entry;
        }
        entry = entry->next;
    }
    return NULL;
}

static NOINLINE JitEntry *jit_find_noinline(uint32_t address)
{
    return jit_find(address);
}

/*************************************************************************/

/**
 * jit_translate:  Dynamically translate a block of instructions starting
 * at the given address.  If a translation already exists for the given
 * address, it is cleared.
 *
 * [Parameters]
 *       state: SH-2 processor state block
 *     address: Start address in SH-2 address space
 * [Return value]
 *     Translated block, or NULL on error
 */
static NOINLINE JitEntry *jit_translate(SH2State *state, uint32_t address)
{
    JitEntry *entry;
    int index;

#ifdef PSP_TIME_TRANSLATION
    static uint32_t total, count;
    const uint32_t a = sceKernelGetSystemTimeLow();
#endif

    /* Update the timestamp on every call */
    jit_timestamp++;

    /* First check for untranslatable addresses */
    if (UNLIKELY(address == 0)) {
        /* We use address 0 to indicate an unused entry, so we can't
         * translate from address 0.  But this should never happen except
         * in pathological cases (or unhandled exceptions), so just punt
         * and let the interpreter handle it. */
        return NULL;
    }
    if (UNLIKELY(address & 1)) {
        /* Odd addresses are invalid, so we can't translate them in the
         * first place. */
         return NULL;
    }
    if (UNLIKELY(!fetch_pages[address >> 19])) {
        /* Don't try to translate anything from non-directly-accessible
         * memory, since we can't track changes to such memory. */
        return NULL;
    }

    /* Check whether the starting address is blacklisted */
    for (index = 0; index < lenof(blacklist); index++) {
        uint32_t age = jit_timestamp - blacklist[index].timestamp;
        if (age >= JIT_BLACKLIST_EXPIRE) {
            /* Entry expired, so clear it */
            blacklist[index].start = blacklist[index].end = 0;
            continue;
        }
        if (blacklist[index].start <= address
                                   && address <= blacklist[index].end) {
            return NULL;
        }
    }

    /* Clear out any existing translation; if we've reached the data size
     * limit, also evict old entries until we're back under the limit */
    if ((entry = jit_find(address)) != NULL) {
        clear_entry(entry);
    }
    if (UNLIKELY(jit_total_data >= jit_data_limit)) {
#ifdef JIT_DEBUG
        DMSG("JIT data size over limit (%u >= %u), clearing entries",
             jit_total_data, jit_data_limit);
#endif
        while (jit_total_data >= jit_data_limit) {
            clear_oldest_entry();
        }
    }

    /* Obtain a free entry for this block */
    if (jit_freelist.next == &jit_freelist) {
        /* No free entries, so clear the oldest one and use it */
#ifdef JIT_DEBUG
        DMSG("No free slots for code at 0x%08X, clearing oldest", address);
#endif
        clear_oldest_entry();
        if (UNLIKELY(jit_freelist.next == &jit_freelist)) {  // paranoia
            DMSG("BUG: failed to update free list");
            return 0;
        }
    }
    entry = jit_freelist.next;
    jit_freelist.next = entry->next;
    jit_freelist.next->prev = &jit_freelist;

    /* Store the entry pointer in the state block, for reference by other
     * functions */
    state->translate_entry = entry;

    /* Initialize the new entry */
    entry->rtl = rtl_create_block();
    if (!entry->rtl) {
        DMSG("No memory for code at 0x%08X", address);
        goto fail;
    }
    const int hashval = JIT_HASH(address);
    entry->next = jit_hashchain[hashval];
    if (entry->next) {
        entry->next->prev = entry;
    }
    jit_hashchain[hashval] = entry;
    entry->prev = NULL;
    entry->lra_next = &jit_lralist;
    entry->lra_prev = jit_lralist.lra_prev;
    entry->lra_next->lra_prev = entry;
    entry->lra_prev->lra_next = entry;
    entry->pred_ref_head.next = &entry->pred_ref_head;
    entry->pred_ref_head.prev = &entry->pred_ref_head;
    entry->sh2_start = address;
    entry->sh2_end = 0;
    entry->native_code = NULL;
    entry->native_length = 0;
    entry->timestamp = jit_timestamp;
    entry->running = 0;
    entry->must_clear = 0;
#if JIT_BRANCH_PREDICTION_SLOTS > 0
    for (index = 0; index < JIT_BRANCH_PREDICTION_SLOTS; index++) {
        entry->predicted[index].target = 0;
        entry->predicted[index].entry = NULL;
        entry->predicted[index].next = NULL;
        entry->predicted[index].prev = NULL;
    }
#endif
#ifdef JIT_BRANCH_PREDICT_STATIC
    entry->static_predict = NULL;
    entry->num_static_branches = 0;
#endif
#ifdef JIT_PROFILE
    entry->call_count = 0;
    entry->cycle_count = 0;
    entry->exec_time = 0;
#endif

    /* Perform the SH-2 -> RTL translation */
    if (UNLIKELY(!translate_block(state, entry))) {
        DMSG("Failed to translate block at 0x%08X", address);
        /* If we found a valid end address for the block, blacklist the
         * entire block so we don't waste time trying to translate from
         * every successive instruction.  Otherwise, just blacklist the
         * first address in case we come back here again. */
        if (entry->sh2_end) {
            jit_blacklist_range(address, entry->sh2_end);
        } else {
            jit_blacklist_range(address, address+1);
        }
        goto clear_and_fail;
    }

    /* Translate from RTL to native code */
#ifndef JIT_DEBUG_INTERPRET_RTL
    if (UNLIKELY(!rtl_translate_block(entry->rtl, &entry->native_code,
                                      &entry->native_length))) {
        DMSG("Failed to translate block at 0x%08X", entry->sh2_start);
        goto clear_and_fail;
    }
    /* Make sure the new code isn't masked by cached data */
    flush_native_cache(entry->native_code, entry->native_length);
    rtl_destroy_block(entry->rtl);
    entry->rtl = NULL;
#endif

    /* Update JIT management data */
    uint8_t *jit_base = direct_jit_pages[entry->sh2_start >> 19];
    if (jit_base) {
        for (index =  entry->sh2_start >> JIT_PAGE_BITS;
             index <= entry->sh2_end   >> JIT_PAGE_BITS;
             index++
        ) {
            JIT_PAGE_SET(jit_base, index);
        }
    }
#ifdef JIT_DEBUG_INTERPRET_RTL
    /* Guesstimate how much memory was used (including the insn-to-unit LUT
     * later allocated by rtl_execute_block()); we assume average ratios of
     * 5:8 insns:regs, and 1:8 insns:units (we don't bother with labels
     * since they only require 2 bytes each and are generally less than 10%
     * of insns). */
    entry->native_length = ((sizeof(RTLInsn)+2)
                            + sizeof(RTLRegister)*5/8
                            + sizeof(RTLUnit)/8) * entry->rtl->insns_size
                         + sizeof(RTLBlock);
#endif
    jit_total_data += entry->native_length;

#ifdef PSP_TIME_TRANSLATION
    const uint32_t b = sceKernelGetSystemTimeLow();
    total += b-a;
    count += ((current_entry->sh2_end + 1) - current_entry->sh2_start) / 2;
    DMSG("%u/%u = %.3f us/insn", total, count, (float)total/count);
#endif

    /* All done */
    state->translate_entry = NULL;
    return entry;

    /* Error handling */
  clear_and_fail:
    clear_entry(entry);
  fail:
    state->translate_entry = NULL;
    return NULL;
}

/*************************************************************************/

/**
 * jit_clear_write:  Clear any translation which includes the given
 * address.  Intended to be called from within translated code.
 *
 * [Parameters]
 *       state: Processor state block
 *     address: Address to which data was written
 * [Return value]
 *     None
 */
static FASTCALL void jit_clear_write(SH2State *state, uint32_t address)
{
    int index;

    /* If it's a blacklisted address, we don't need to do anything (since
     * the address couldn't have been translated); just update the entry's
     * write timestamp and return */
    for (index = 0; index < lenof(blacklist); index++) {
        if (blacklist[index].start <= address
                                   && address <= blacklist[index].end) {
            blacklist[index].timestamp = jit_timestamp;
            return;
        }
    }

#ifdef JIT_DEBUG
    DMSG("WARNING: jit_clear_write(0x%08X) from PC %08X", address, state->PC);
#endif

    /* Clear any translations on the affected page */
    uint8_t * const jit_base  = direct_jit_pages[address >> 19];
    const uint32_t page       = address >> JIT_PAGE_BITS;
    const uint32_t page_start = address & -(1 << JIT_PAGE_BITS);
    const uint32_t page_end   = page_start + ((1 << JIT_PAGE_BITS) - 1);
    JIT_PAGE_CLEAR(jit_base, page);
    int found_entry = 0;   // Flag: Did we find a block on this page?
    int do_blacklist = 0;  // Flag: Blacklist this address?
    for (index = 0; index < JIT_TABLE_SIZE; index++) {
        if (jit_table[index].sh2_start == 0) {
            continue;
        }
        if (jit_table[index].sh2_start <= page_end
         && jit_table[index].sh2_end >= page_start
        ) {
            found_entry = 1;
            if (UNLIKELY(jit_table[index].running)) {
                jit_table[index].must_clear = 1;
                do_blacklist = 1;
            } else {
                clear_entry(&jit_table[index]);
            }
        }
    }

    /* If we fault on the same address multiple times in rapid succession,
     * blacklist the address even if it wasn't in a running block.  (We
     * don't blacklist this case on the first fault because it could just
     * be overwriting a block of memory with new code.) */
    if (found_entry && !do_blacklist) {
        int empty = -1;
        unsigned int oldest = 0;
        for (index = 0; index < lenof(pending_blacklist); index++) {
            if (pending_blacklist[index].address == address) {
                if (jit_timestamp - pending_blacklist[index].timestamp 
                    > JIT_PURGE_EXPIRE
                ) {
                    pending_blacklist[index].count = 0;
                }
                break;
            } else if (pending_blacklist[index].address != 0) {
                if (pending_blacklist[index].timestamp
                    < pending_blacklist[oldest].timestamp
                ) {
                    oldest = index;
                }
            } else if (empty < 0) {
                empty = index;
            }
        }
        if (index >= lenof(pending_blacklist)) {
            if (empty >= 0) {
                index = empty;
            } else {
                index = oldest;
            }
            pending_blacklist[index].address = address;
            pending_blacklist[index].count = 0;
        }
        pending_blacklist[index].timestamp = jit_timestamp;
        pending_blacklist[index].count++;
        if (pending_blacklist[index].count >= JIT_PURGE_THRESHOLD) {
#ifdef JIT_DEBUG
            DMSG("Blacklisting 0x%08X due to repeated faults",address);
#endif
            do_blacklist = 1;
        }
    }

    /* Blacklist this address if appropriate.  We always keep the blacklist
     * entries 16-bit-aligned; since we don't pass the write size in, we
     * assume the write is 32-bit-sized if on a 32-bit-aligned address, and
     * 16-bit-sized otherwise. */
    if (do_blacklist) {
        const uint32_t start = address & ~1;
        const uint32_t end = (address & ~3) + 3;
        jit_blacklist_range(start, end);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * jit_clear_range:  Clear any translation on any page touched by the given
 * range of addresses.  Intended to be called when an external agent
 * modifies memory.
 *
 * [Parameters]
 *     address: Beginning of address range to which data was written
 *        size: Size of address range to which data was written (in bytes)
 * [Return value]
 *     None
 */
static void jit_clear_range(uint32_t address, uint32_t size)
{
    const uint32_t first_page = address >> JIT_PAGE_BITS;
    const uint32_t last_page  = (address + size - 1) >> JIT_PAGE_BITS;
    const uint32_t page_start = first_page << JIT_PAGE_BITS;
    const uint32_t page_end   = ((last_page+1) << JIT_PAGE_BITS) - 1;

    int page;
    for (page = first_page; page <= last_page; page++) {
        if (direct_jit_pages[page >> (19-JIT_PAGE_BITS)]) {
            JIT_PAGE_CLEAR(direct_jit_pages[page >> (19-JIT_PAGE_BITS)], page);
        }
    }

    int index;
    for (index = 0; index < JIT_TABLE_SIZE; index++) {
        if (jit_table[index].sh2_start == 0) {
            continue;
        }
        if (jit_table[index].sh2_start <= page_end
         && jit_table[index].sh2_end >= page_start
        ) {
            clear_entry(&jit_table[index]);
        }
    }
}

/*-----------------------------------------------------------------------*/

/**
 * jit_clear_all:  Clear all translations.  Intended to be used when
 * initializing/resetting the processor or when a change in optimization
 * options invalidates existing translations.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void jit_clear_all(void)
{
    int index;

    /* Clear all JIT bitmaps */
    for (index = 0; index < lenof(direct_jit_pages); index++) {
        if (direct_jit_pages[index]) {
            uintptr_t address = (uintptr_t)direct_jit_pages[index];
            address += index << (19 - JIT_PAGE_BITS);
            memset((void *)address, 0, (1<<19) >> JIT_PAGE_BITS);
        }
    }

    /* Clear all entry data (don't call clear_entry() to avoid wasting time
     * updating management data for every single entry) */
    for (index = 0; index < JIT_TABLE_SIZE; index++) {
        if (jit_table[index].sh2_start) {
            free(jit_table[index].native_code);
            jit_table[index].native_code = NULL;
            rtl_destroy_block(jit_table[index].rtl);
            jit_table[index].rtl = NULL;
#ifdef JIT_BRANCH_PREDICT_STATIC
            free(jit_table[index].static_predict);
            jit_table[index].static_predict = NULL;
#endif
            jit_table[index].sh2_start = 0;
        }
        jit_table[index].timestamp = 0;
    }
    for (index = 0; index < JIT_TABLE_SIZE; index++) {
        jit_hashchain[index] = NULL;
    }

    /* Put every entry in the free list */
    jit_freelist.next = &jit_table[0];
    jit_table[0].prev = &jit_freelist;
    for (index = 1; index < JIT_TABLE_SIZE; index++) {
        jit_table[index-1].next = &jit_table[index];
        jit_table[index].prev = &jit_table[index-1];
    }
    jit_table[JIT_TABLE_SIZE-1].next = &jit_freelist;
    jit_freelist.prev = &jit_table[JIT_TABLE_SIZE-1];

    /* Clear other management data */
    jit_lralist.lra_next = &jit_lralist;
    jit_lralist.lra_prev = &jit_lralist;
    jit_total_data = 0;
    SH2State *state;
    for (state = state_list; state; state = state->next) {
        state->current_entry = NULL;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * jit_blacklist_range:  Add the specified range to the JIT blacklist.
 *
 * [Parameters]
 *     start: First byte in range to blacklist
 *       end: Last byte in range to blacklist
 * [Return value]
 *     None
 */
static void jit_blacklist_range(uint32_t start, uint32_t end)
{
    unsigned int index;

    /* Merge this to the beginning or end of another entry if possible */
    for (index = 0; index < lenof(blacklist); index++) {
        if (blacklist[index].start == end+1) {
            blacklist[index].start = start;
            /* See if there's another one we can join to this one */
            unsigned int index2;
            for (index2 = 0; index2 < lenof(blacklist); index2++) {
                if (start == blacklist[index2].end+1) {
                    blacklist[index].start = blacklist[index2].start;
                    blacklist[index2].start = blacklist[index2].end = 0;
                    break;
                }
            }
            goto entry_added;
        } else if (blacklist[index].end+1 == start) {
            blacklist[index].end = end;
            unsigned int index2;
            for (index2 = 0; index2 < lenof(blacklist); index2++) {
                if (end+1 == blacklist[index2].start) {
                    blacklist[index].end = blacklist[index2].end;
                    blacklist[index2].start = blacklist[index2].end = 0;
                    break;
                }
            }
            goto entry_added;
        }
    }

    /* We didn't find an entry to merge this into, so allocate a new one */
    for (index = 0; index < lenof(blacklist); index++) {
        if (!blacklist[index].start && !blacklist[index].end) {
            break;
        }
    }

    /* If the table is full, purge the oldest entry */
    if (index >= lenof(blacklist)) {
        unsigned int oldest = 0;
        for (index = 1; index < lenof(blacklist); index++) {
            if (timestamp_compare(jit_timestamp,
                                  blacklist[index].timestamp,
                                  blacklist[oldest].timestamp) < 0) {
                oldest = index;
            }
        }
#ifdef JIT_DEBUG
        DMSG("Blacklist full, purging entry %d (0x%X-0x%X)",
             oldest, blacklist[oldest].start, blacklist[oldest].end);
#endif
        index = oldest;
    }

    /* Fill in the new entry */
    blacklist[index].start = start;
    blacklist[index].end = end;
  entry_added:
    blacklist[index].timestamp = jit_timestamp;
}

/*************************************************************************/

/**
 * jit_mark_purged:  Mark the given block as purged due to precondition
 * failure.
 *
 * [Parameters]
 *     address: Block starting address
 * [Return value]
 *     None
 */
static FASTCALL void jit_mark_purged(uint32_t address)
{
    unsigned int i;
    unsigned int oldest = 0;      // Oldest entry (which will be overwritten)
    unsigned int oldest_age = 0;  // Age of purge_table[oldest]

#ifdef JIT_DEBUG
    DMSG("WARNING: Purging block 0x%08X", address);
#endif

    for (i = 0; i < lenof(purge_table); i++) {
        const uint32_t age = jit_timestamp - purge_table[i].timestamp;
        if (!purge_table[i].address) {
            /* Empty entry, so treat it as the oldest one for overwriting */
            oldest = i;
            oldest_age = JIT_PURGE_EXPIRE;
        } else if (age >= JIT_PURGE_EXPIRE) {
            /* Entry has expired, so clear it out */
            purge_table[i].address = 0;
            oldest = i;
            oldest_age = JIT_PURGE_EXPIRE;
        } else if (purge_table[i].address == address) {
            /* If this block is already in the list (and not expired),
             * increment its purge counter and stop immediately */
            purge_table[i].timestamp = jit_timestamp;
            purge_table[i].count++;
            return;
        } else if (age > oldest_age) {
            /* If this is the oldest entry in the table, we'll overwrite it */
            oldest = i;
            oldest_age = age;
        }
    }

    /* We didn't find the entry in the table, so overwrite the oldest slot
     * (which may be an empty slot) with this block's information */
    purge_table[oldest].address = address;
    purge_table[oldest].timestamp = jit_timestamp;
    purge_table[oldest].count = 1;
}

/*-----------------------------------------------------------------------*/

/**
 * jit_check_purged:  Check whether the given block has been purged often
 * enough to require disabling optimizations with preconditions.
 *
 * [Parameters]
 *     address: Block starting address
 * [Return value]
 *     Nonzero if relevant optimizations should be disabled, else zero
 */
static int jit_check_purged(uint32_t address)
{
    unsigned int i;

    for (i = 0; i < lenof(purge_table); i++) {
        if (purge_table[i].address == address) {
            const uint32_t age = jit_timestamp - purge_table[i].timestamp;
            if (age >= JIT_PURGE_EXPIRE) {
                purge_table[i].address = 0;
                return 0;
            } else {
                return purge_table[i].count >= JIT_PURGE_THRESHOLD;
            }
        }
    }

    return 0;
}

/*************************************************************************/

#if JIT_BRANCH_PREDICTION_SLOTS > 0

/**
 * jit_predict_branch:  Attempt to predict a branch to "target" using the
 * prediction array "predicted", and update the prediction table as
 * appropriate.  Helper function for sh2_run(), called after the first
 * prediction table entry has been checked.
 *
 * [Parameters]
 *        target: Target SH-2 address
 *     predicted: JitEntry.predicted array from current translated block
 * [Return value]
 *     Translated block for target address, or NULL if none was found
 */
static NOINLINE JitEntry *jit_predict_branch(BranchTargetInfo *predicted,
                                             const uint32_t target)
{
# if JIT_BRANCH_PREDICTION_SLOTS > 1
    /* First see if it's in any slot other than the first */
    unsigned int i;
    for (i = 1; i < JIT_BRANCH_PREDICTION_SLOTS; i++) {
        if (predicted[i].target != 0 && target == predicted[i].target) {
            JitEntry *result = predicted[i].entry;
#  ifdef JIT_BRANCH_PREDICTION_FLOAT
            /* Shift it up to the previous slot so we can find it faster next
             * time.  If we have any code that alternates between two branch
             * targets, we're going to take a major performance hit... */
            BranchTargetInfo *this_next = predicted[i].next;
            BranchTargetInfo *this_prev = predicted[i].prev;
            uint32_t this_target = predicted[i].target;
            JitEntry *this_entry = predicted[i].entry;
            predicted[i].target = predicted[i-1].target;
            predicted[i].entry  = predicted[i-1].entry;
            if (predicted[i].entry) {
                predicted[i].next = predicted[i-1].next;
                predicted[i].prev = predicted[i-1].prev;
                predicted[i].next->prev = &predicted[i];
                predicted[i].prev->next = &predicted[i];
            }
            predicted[i-1].next   = this_next;
            predicted[i-1].prev   = this_prev;
            predicted[i-1].target = this_target;
            predicted[i-1].entry  = this_entry;
            this_next->prev = &predicted[i-1];
            this_prev->next = &predicted[i-1];
#  endif  // JIT_BRANCH_PREDICTION_FLOAT
            return result;
        }
    }
    /* Update the first empty slot, or the last slot if none are empty */
    for (i = 0; i < JIT_BRANCH_PREDICTION_SLOTS-1; i++) {
        if (predicted[i].target == 0) {
            break;
        }
    }
    return update_branch_target(&predicted[i], target);
# else  // JIT_BRANCH_PREDICTION_SLOTS == 1
    return update_branch_target(predicted, target);
# endif
}

#endif  // JIT_BRANCH_PREDICTION_SLOTS > 0

/*************************************************************************/

#ifdef JIT_PROFILE

/**
 * jit_print_profile:  Print profiling statistics for translated code.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static NOINLINE void jit_print_profile(void)
{
    int top[JIT_PROFILE_TOP];
    unsigned int i;

    printf("\n");

    printf("================= Top callees by time: =================\n");
    memset(top, -1, sizeof(top));
    for (i = 0; i < lenof(jit_table); i++) {
        if (!jit_table[i].sh2_start) {
            continue;
        }
        unsigned int j;
        for (j = 0; j < lenof(top); j++) {
            if (top[j] < 0
                || jit_table[i].exec_time > jit_table[top[j]].exec_time
                ) {
                unsigned int k;
                for (k = lenof(top)-1; k > j; k--) {
                    top[k] = top[k-1];
                }
                top[j] = i;
                break;
            }
        }
    }
    printf(
# ifdef JIT_DEBUG_INTERPRET_RTL
           "RTL insns"
# else
           "Exec time"
# endif
                    "   SH2 cyc.   Native/SH2   # calls   Block addr\n");
    printf("---------   --------   ----------   -------   ----------\n");
    for (i = 0; i < lenof(top) && top[i] >= 0; i++) {
        printf("%9lld %10d %12.3f %9d   0x%08X\n",
                (unsigned long long)jit_table[top[i]].exec_time,
                jit_table[top[i]].cycle_count,
                (double)jit_table[top[i]].exec_time
                / (double)jit_table[top[i]].cycle_count,
                jit_table[top[i]].call_count,
                jit_table[top[i]].sh2_start);
    }
    printf("========================================================\n");


    printf("============== Top callees by call count: ==============\n");
    memset(top, -1, sizeof(top));
    for (i = 0; i < lenof(jit_table); i++) {
        if (!jit_table[i].sh2_start) {
            continue;
        }
        unsigned int j;
        for (j = 0; j < lenof(top); j++) {
            if (top[j] < 0
                || jit_table[i].call_count > jit_table[top[j]].call_count
                ) {
                unsigned int k;
                for (k = lenof(top)-1; k > j; k--) {
                    top[k] = top[k-1];
                }
                top[j] = i;
                break;
            }
        }
    }
    printf(
# ifdef JIT_DEBUG_INTERPRET_RTL
           "RTL insns"
# else
           "Exec time"
# endif
                    "   SH2 cyc.   Native/SH2   # calls   Block addr\n");
    printf("---------   --------   ----------   -------   ----------\n");
    for (i = 0; i < lenof(top) && top[i] >= 0; i++) {
        printf("%9lld %10d %12.3f %9d   0x%08X\n",
                (unsigned long long)jit_table[top[i]].exec_time,
                jit_table[top[i]].cycle_count,
                (double)jit_table[top[i]].exec_time
                / (double)jit_table[top[i]].cycle_count,
                jit_table[top[i]].call_count,
                jit_table[top[i]].sh2_start);
    }
    printf("========================================================\n");

    for (i = 0; i < lenof(jit_table); i++) {
        jit_table[i].call_count = 0;
        jit_table[i].cycle_count = 0;
        jit_table[i].exec_time = 0;
    }

}

#endif  // JIT_PROFILE

/*************************************************************************/
/***************** Dynamic translation utility routines ******************/
/*************************************************************************/

/**
 * clear_entry:  Clear a specific entry from the JIT table, freeing the
 * native code buffer and unlinking the entry from its references.
 *
 * [Parameters]
 *     entry: JitEntry structure pointer
 * [Return value]
 *     None
 */
static NOINLINE void clear_entry(JitEntry *entry)
{
    PRECOND(entry != NULL, return);

    /* Make sure the non-active processor isn't trying to use this entry
     * (if so, clear it out) */
    SH2State *state;
    for (state = state_list; state; state = state->next) {
        if (state->current_entry == entry) {
            state->current_entry = NULL;
        }
    }

    /* Free the native code, if any */
    jit_total_data -= entry->native_length;
    free(entry->native_code);
    entry->native_code = NULL;

    /* Free the RTL block, if any */
    rtl_destroy_block(entry->rtl);
    entry->rtl = NULL;

    /* Clear the entry from the predicted branch target of any entries
     * referencing it */
    BranchTargetInfo *referrer, *next;
    for (referrer = entry->pred_ref_head.next;
         referrer != &entry->pred_ref_head;
         referrer = next
    ) {
        next = referrer->next;
        referrer->target = 0;
        referrer->entry = NULL;
        referrer->next = NULL;
        referrer->prev = NULL;
    }
    entry->pred_ref_head.next = &entry->pred_ref_head;
    entry->pred_ref_head.prev = &entry->pred_ref_head;

    /* Remove this entry from the reference list of any branch it predicts */
#if JIT_BRANCH_PREDICTION_SLOTS > 0
    unsigned int i;
    for (i = 0; i < JIT_BRANCH_PREDICTION_SLOTS; i++) {
        if (entry->predicted[i].entry) {
            entry->predicted[i].next->prev = entry->predicted[i].prev;
            entry->predicted[i].prev->next = entry->predicted[i].next;
        }
    }
#endif
#ifdef JIT_BRANCH_PREDICT_STATIC
    for (i = 0; i < entry->num_static_branches; i++) {
        if (entry->static_predict[i].entry) {
            entry->static_predict[i].next->prev = entry->static_predict[i].prev;
            entry->static_predict[i].prev->next = entry->static_predict[i].next;
        }
    }
    free(entry->static_predict);
    entry->static_predict = NULL;
    jit_total_data -=
        entry->num_static_branches * sizeof(*entry->static_predict);
#endif

    /* Clear the entry from the hash chain and LRA list */
    if (entry->next) {
        entry->next->prev = entry->prev;
    }
    if (entry->prev) {
        entry->prev->next = entry->next;
    } else {
        jit_hashchain[JIT_HASH(entry->sh2_start)] = entry->next;
    }
    entry->lra_next->lra_prev = entry->lra_prev;
    entry->lra_prev->lra_next = entry->lra_next;

    /* Mark the entry as free */
    entry->sh2_start = 0;

    /* Insert the entry into the free list */
    entry->next = jit_freelist.next;
    entry->prev = &jit_freelist;
    entry->next->prev = entry;
    jit_freelist.next = entry;
}

/*-----------------------------------------------------------------------*/

/**
 * clear_oldest_entry:  Clear the oldest entry from the JIT table.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void clear_oldest_entry(void)
{
    JitEntry *oldest = jit_lralist.lra_next;
    if (LIKELY(oldest != &jit_lralist)) {
#ifdef JIT_DEBUG
        DMSG("Clearing oldest entry 0x%08X (age %u)",
             oldest->sh2_start, jit_timestamp - oldest->timestamp);
#endif
        clear_entry(oldest);
    } else {
#ifdef JIT_DEBUG
        DMSG("Tried to clear oldest entry from an empty table!");
#endif
        /* We don't call this function unless there's something we need to
         * clear out, so if we come here, our internal tables may be
         * corrupt.  Reset everything to be safe. */
        jit_total_data = 0;
    }
}

/*************************************************************************/

/**
 * flush_native_cache:  Flush a range of addresses from the native CPU's
 * caches.
 *
 * [Parameters]
 *      start: Pointer to start of range
 *     length: Length of range in bytes
 * [Return value]
 *     None
 */
static inline void flush_native_cache(void *start, uint32_t length)
{
    if (cache_flush_callback) {
        (*cache_flush_callback)(start, length);
    }
}

/*************************************************************************/

/**
 * timestamp_compare:  Compare two timestamps.
 *
 * [Parameters]
 *          a, b: Timestamps to compare
 *     reference: Reference timestamp by which the comparison is made
 * [Return value]
 *     -1 if a < b (i.e. "a is older than b")
 *      0 if a == b
 *      1 if a > b
 */
__attribute__((const)) static inline int timestamp_compare(
    uint32_t reference, uint32_t a, uint32_t b)
{
    const uint32_t age_a = reference - a;
    const uint32_t age_b = reference - b;
    return age_a > age_b ? -1 :
           age_a < age_b ?  1 : 0;
}

/*************************************************************************/
/****************** Macros for SH-2 -> RTL translation *******************/
/*************************************************************************/

/* Constants used in SH-2 memory access operations */
#define SH2_ACCESS_TYPE_B   0  // 1-byte access (signed for loads)
#define SH2_ACCESS_TYPE_W   1  // 2-byte access (signed for loads)
#define SH2_ACCESS_TYPE_L   2  // 4-byte access

/* Return the "high" (pointer register) and "low" (load/store offset) parts
 * of an address for generating optimal native load/store code */
#ifdef __mips__
# define ADDR_HI(address)  (((uintptr_t)(address) + 0x8000) & 0xFFFF0000)
# define ADDR_LO(address)  ((int16_t)((uintptr_t)(address) & 0xFFFF))
#else
# define ADDR_HI(address)  ((uintptr_t)address)
# define ADDR_LO(address)  0
#endif

/*-----------------------------------------------------------------------*/

/* Basic macro to pass a failure (zero) return value up the call chain */
#define ASSERT(expr)  do {      \
    if (UNLIKELY(!(expr))) {    \
        return 0;               \
    }                           \
} while (0)

/* Basic macro to append an RTL instruction, aborting on error */
#define APPEND(opcode,dest,src1,src2,other)  \
    ASSERT(rtl_add_insn(entry->rtl, RTLOP_##opcode, \
                        (dest), (src1), (src2), (other)));

/*-----------------------------------------------------------------------*/

/* Declare a register identifier, but don't allocate a new register for it */
#define DECLARE_REG(name)  uint32_t name

/* Allocate a register for a declared identifier */
#define ALLOC_REG(name)    ASSERT(name = rtl_alloc_register(entry->rtl))

/* Define a new register (equivalent to DECLARE_REG followed by ALLOC_REG) */
#define DEFINE_REG(name)   \
    const uint32_t name = rtl_alloc_register(entry->rtl); \
    if (UNLIKELY(!name)) { \
        return 0;          \
    }

/*-----------------------------------------------------------------------*/

/* Register-register operations */

#define MOVE(dest,src)          APPEND(MOVE, (dest), (src), 0, 0)
#define SELECT(dest,src1,src2,cond) \
                                APPEND(SELECT, (dest), (src1), (src2), (cond))
#define ADD(dest,src1,src2)     APPEND(ADD, (dest), (src1), (src2), 0)
#define SUB(dest,src1,src2)     APPEND(SUB, (dest), (src1), (src2), 0)
#define MUL(dest,src1,src2)     APPEND(MULU, (dest), (src1), (src2), 0)
#define MULU_64(dest,src1,src2,dest_hi) \
    APPEND(MULU, (dest), (src1), (src2), (dest_hi))
#define MULS_64(dest,src1,src2,dest_hi) \
    APPEND(MULS, (dest), (src1), (src2), (dest_hi))
#define MADDU_64(dest,src1,src2,dest_hi) \
    APPEND(MADDU, (dest), (src1), (src2), (dest_hi))
#define MADDS_64(dest,src1,src2,dest_hi) \
    APPEND(MADDS, (dest), (src1), (src2), (dest_hi))
#define DIVMODU(dest,src1,src2,rem) \
    APPEND(DIVMODU, (dest), (src1), (src2), (rem))
#define DIVMODS(dest,src1,src2,rem) \
    APPEND(DIVMODS, (dest), (src1), (src2), (rem))
#define AND(dest,src1,src2)     APPEND(AND, (dest), (src1), (src2), 0)
#define OR(dest,src1,src2)      APPEND(OR, (dest), (src1), (src2), 0)
#define XOR(dest,src1,src2)     APPEND(XOR, (dest), (src1), (src2), 0)
#define NOT(dest,src)           APPEND(NOT, (dest), (src), 0, 0)
#define SLL(dest,src1,src2)     APPEND(SLL, (dest), (src1), (src2), 0)
#define SRL(dest,src1,src2)     APPEND(SRL, (dest), (src1), (src2), 0)
#define SRA(dest,src1,src2)     APPEND(SRA, (dest), (src1), (src2), 0)
#define ROR(dest,src1,src2)     APPEND(SRL, (dest), (src1), (src2), 0)
#define CLZ(dest,src)           APPEND(CLZ, (dest), (src), 0, 0)
#define CLO(dest,src)           APPEND(CLO, (dest), (src), 0, 0)
#define SLTU(dest,src1,src2)    APPEND(SLTU, (dest), (src1), (src2), 0)
#define SLTS(dest,src1,src2)    APPEND(SLTS, (dest), (src1), (src2), 0)
#define BSWAPH(dest,src)        APPEND(BSWAPH, (dest), (src), 0, 0)
#define BSWAPW(dest,src)        APPEND(BSWAPW, (dest), (src), 0, 0)
#define HSWAPW(dest,src)        APPEND(HSWAPW, (dest), (src), 0, 0)

/*----------------------------------*/

/* Register-immediate operations */

#define MOVEI(dest,imm)         APPEND(LOAD_IMM, (dest), (imm), 0, 0)
#define MOVEA(dest,addr)        APPEND(LOAD_ADDR, (dest), \
                                       (uintptr_t)(addr), 0, 0)
#ifdef JIT_USE_RTL_REGIMM
# define IMMOP(op,dest,src,imm) APPEND(op##I, (dest), (src), (imm), 0)
#else
# define IMMOP(op,dest,src,imm) do { DEFINE_REG(__immreg); \
                                     APPEND(LOAD_IMM, __immreg, (imm), 0, 0); \
                                     APPEND(op, (dest), (src), __immreg, 0); \
                                } while (0)
#endif
#define ADDI(dest,src,imm)      IMMOP(ADD, (dest), (src), (imm))
#define SUBI(dest,src,imm)      IMMOP(ADD, (dest), (src), -(imm))
#define ANDI(dest,src,imm)      IMMOP(AND, (dest), (src), (imm))
#define ORI(dest,src,imm)       IMMOP(OR, (dest), (src), (imm))
#define XORI(dest,src,imm)      IMMOP(XOR, (dest), (src), (imm))
#define SLLI(dest,src,imm)      IMMOP(SLL, (dest), (src), (imm))
#define SRLI(dest,src,imm)      IMMOP(SRL, (dest), (src), (imm))
#define SRAI(dest,src,imm)      IMMOP(SRA, (dest), (src), (imm))
#define RORI(dest,src,imm)      IMMOP(ROR, (dest), (src), (imm))
#define SLTUI(dest,src,imm)     IMMOP(SLTU, (dest), (src), (imm))
#define SLTSI(dest,src,imm)     IMMOP(SLTS, (dest), (src), (imm))

/*----------------------------------*/

/* Bitfield instructions */

#ifdef JIT_USE_RTL_BITFIELDS
# define BFOP(op,dest,src1,src2,start,count) \
    APPEND(BF##op, (dest), (src1), (src2), (start) | (count)<<8)
# define BFEXT(dest,src,start,count) \
    BFOP(EXT, (dest), (src), 0, (start), (count))
# define BFINS(dest,src1,src2,start,count) \
    BFOP(INS, (dest), (src1), (src2), (start), (count))
#else
# define BFEXT(dest,src,start,count)  do {      \
    const uint32_t __dest = (dest);             \
    const uint32_t __src = (src);               \
    const int __start = (start);                \
    const int __count = (count);                \
    if (__start == 0) {                         \
        if (__count == 32) {                    \
            MOVE(__dest, __src);                \
        } else {                                \
            ANDI(__dest, __src, (1U<<__count) - 1); \
        }                                       \
    } else if (__start + __count == 32) {       \
        SRLI(__dest, __src, __start);           \
    } else {                                    \
        DEFINE_REG(__temp);                     \
        SRLI(__temp, __src, (start));           \
        ANDI(__dest, __temp, (1U<<__count) - 1); \
    }                                           \
} while (0)
# define BFINS(dest,src1,src2,start,count)  do { \
    const uint32_t __dest = (dest);             \
    const uint32_t __src1 = (src1);             \
    const uint32_t __src2 = (src2);             \
    const int __start = (start);                \
    const int __count = (count);                \
    if (__start == 0 && __count == 32) {        \
        MOVE(__dest, __src2);                   \
    } else {                                    \
        const uint32_t __mask = (1U<<__count) - 1; \
        DEFINE_REG(__out2);                     \
        if (__start == 0) {                     \
            ANDI(__out2, __src2, __mask);       \
        } else {                                \
            DEFINE_REG(__temp);                 \
            ANDI(__temp, __src2, __mask);       \
            SLLI(__out2, __temp, __start);      \
        }                                       \
        DEFINE_REG(__out1);                     \
        ANDI(__out1, __src1, ~(__mask << __start)); \
        OR(__dest, __out1, __out2);             \
    }                                           \
} while (0)
#endif

/*----------------------------------*/

/* Variants of SLT */

#define SEQZ(dest,src)          SLTUI((dest), (src), 1)
#define SLTZ(dest,src)          SLTSI((dest), (src), 0)

/*----------------------------------*/

/* Load from or store to memory */

#define LOAD_BU(dest,address,offset) \
    APPEND(LOAD_BU,   (dest), (address), 0, (offset))
#define LOAD_BS(dest,address,offset) \
    APPEND(LOAD_BS,   (dest), (address), 0, (offset))
#define LOAD_HU(dest,address,offset) \
    APPEND(LOAD_HU,   (dest), (address), 0, (offset))
#define LOAD_HS(dest,address,offset) \
    APPEND(LOAD_HS,   (dest), (address), 0, (offset))
#define LOAD_W(dest,address,offset) \
    APPEND(LOAD_W,    (dest), (address), 0, (offset))
#define LOAD_PTR(dest,address,offset) \
    APPEND(LOAD_PTR,  (dest), (address), 0, (offset))
#define STORE_B(address,src,offset) \
    APPEND(STORE_B,   (address), (src),  0, (offset))
#define STORE_H(address,src,offset) \
    APPEND(STORE_H,   (address), (src),  0, (offset))
#define STORE_W(address,src,offset) \
    APPEND(STORE_W,   (address), (src),  0, (offset))
#define STORE_PTR(address,src,offset) \
    APPEND(STORE_PTR, (address), (src),  0, (offset))

/*----------------------------------*/

/* Load from, store to, or add constants to state block fields */

/* Loads */

#define LOAD_STATE(reg,field) \
    ASSERT(__LOAD_STATE(entry, state_reg, (reg), offsetof(SH2State,field)))
static inline int __LOAD_STATE(const JitEntry * const entry,
                               const uint32_t state_reg,
                               const uint32_t reg, const uint32_t offset)
{
#ifdef OPTIMIZE_STATE_BLOCK
    const int index = state_cache_index(offset);
    if (index >= 0) {
        if (state_cache[index].rtlreg) {
            if (reg == state_cache[index].rtlreg) {
                if (state_cache[index].offset) {
                    ADDI(state_cache[index].rtlreg, state_cache[index].rtlreg,
                         state_cache[index].offset);
                    if (index == 15 && stack_pointer) {
                        ADDI(stack_pointer, stack_pointer,
                             state_cache[index].offset);
                    }
                    state_cache[index].offset = 0;
                }
            } else {
                if (state_cache[index].offset) {
                    ADDI(reg, state_cache[index].rtlreg,
                         state_cache[index].offset);
                } else {
                    MOVE(reg, state_cache[index].rtlreg);
                }
            }
        } else {
            LOAD_W(reg, state_reg, offset);
        }
        if (!state_cache[index].fixed
         && !(index == 15 && (optimization_flags & SH2_OPTIMIZE_STACK)
              && state_cache[index].rtlreg != 0)
        ) {
            state_cache[index].rtlreg = reg;
            state_cache[index].offset = 0;
        }
        if (index < 16) {
            /* We changed the stored value, so we have to generate a new
             * direct pointer on the next access. */
            pointer_status[index].rtl_ptrreg = 0;
        }
        return 1;
    }
#endif
    LOAD_W(reg, state_reg, offset);
    return 1;
}

/* Load from a state block field, but don't change the state block cache */
#define LOAD_STATE_COPY(reg,field) \
    ASSERT(__LOAD_STATE_COPY(entry, state_reg, (reg), offsetof(SH2State,field)))
static inline int __LOAD_STATE_COPY(const JitEntry * const entry,
                                    const uint32_t state_reg,
                                    const uint32_t reg, const uint32_t offset)
{
#ifdef OPTIMIZE_STATE_BLOCK
    const int index = state_cache_index(offset);
    if (index >= 0 && state_cache[index].rtlreg) {
        PRECOND(reg != state_cache[index].rtlreg, return 0);
        if (state_cache[index].offset) {
            ADDI(reg, state_cache[index].rtlreg, state_cache[index].offset);
        } else {
            MOVE(reg, state_cache[index].rtlreg);
        }
        return 1;
    }
#endif
    LOAD_W(reg, state_reg, offset);
    return 1;
}

/* Allocate a new register and load it from the state block, or reuse an
 * old register if appropriate and if the register is not offsetted.  Note
 * that a register obtained from this macro MUST NOT be reassigned, since
 * it may be shared; if reassignment is necessary, use DEFINE_REG() and
 * LOAD_STATE() instead.  (The MAC.* instructions are exceptions to this
 * rule, since all other users of MACH/MACL use LOAD_STATE_COPY() to avoid
 * aliasing the register.) */
#define LOAD_STATE_ALLOC(reg,field) \
    ASSERT(reg = __LOAD_STATE_ALLOC(entry, state_reg, \
                                    offsetof(SH2State,field)))
static inline uint32_t __LOAD_STATE_ALLOC(const JitEntry * const entry,
                                          const uint32_t state_reg,
                                          const uint32_t offset)
{
#ifdef OPTIMIZE_STATE_BLOCK
    const int index = state_cache_index(offset);
    if (index >= 0 && state_cache[index].rtlreg != 0) {
        if (state_cache[index].offset == 0) {
            return state_cache[index].rtlreg;
        } else if (state_cache[index].fixed) {
            ADDI(state_cache[index].rtlreg, state_cache[index].rtlreg,
                 state_cache[index].offset);
            if (index == 15 && stack_pointer) {
                ADDI(stack_pointer, stack_pointer, state_cache[index].offset);
            }
            state_cache[index].offset = 0;
            return state_cache[index].rtlreg;
        }
    }
#endif
    DEFINE_REG(reg);
    ASSERT(__LOAD_STATE(entry, state_reg, reg, offset));
    return reg;
}

/* Allocate a new register and load it from the state block, or reuse an
 * old register (leaving any offset in the cache) if appropriate.  As with
 * LOAD_STATE_ALLOC(), a register obtained from this macro MUST NOT be
 * reassigned. */
#define LOAD_STATE_ALLOC_KEEPOFS(reg,field) \
    ASSERT(reg = __LOAD_STATE_ALLOC_KEEPOFS(entry, state_reg, \
                                            offsetof(SH2State,field)))
static inline uint32_t __LOAD_STATE_ALLOC_KEEPOFS(const JitEntry * const entry,
                                                  const uint32_t state_reg,
                                                  const uint32_t offset)
{
#ifdef OPTIMIZE_STATE_BLOCK
    const int index = state_cache_index(offset);
    if (index >= 0 && state_cache[index].rtlreg != 0) {
        return state_cache[index].rtlreg;
    }
#endif
    DEFINE_REG(reg);
    ASSERT(__LOAD_STATE(entry, state_reg, reg, offset));
    return reg;
}

#define LOAD_STATE_PTR(reg,field) \
    LOAD_PTR((reg), state_reg, offsetof(SH2State,field))

#define LOAD_STATE_SR_T(reg) \
    ASSERT(reg = __LOAD_STATE_SR_T(entry, state_reg))
static inline uint32_t __LOAD_STATE_SR_T(const JitEntry * const entry,
                                         const uint32_t state_reg)
{
#ifdef OPTIMIZE_STATE_BLOCK
    if (cached_SR_T) {
        return cached_SR_T;
    }
#endif
    DECLARE_REG(temp);
    LOAD_STATE_ALLOC(temp, SR);
    DEFINE_REG(reg);
    ANDI(reg, temp, SR_T);
#ifdef OPTIMIZE_STATE_BLOCK
    cached_SR_T = reg;
#endif
    return reg;
}

/* Stores */

#define STORE_STATE(field,reg) \
    ASSERT(__STORE_STATE(entry, state_reg, offsetof(SH2State,field), (reg)))
static inline int __STORE_STATE(const JitEntry * const entry,
                                const uint32_t state_reg,
                                const uint16_t offset, const uint32_t reg)
{
    if (offset == offsetof(SH2State,R[15])
     && (optimization_flags & SH2_OPTIMIZE_STACK)
    ) {
#ifdef JIT_DEBUG_VERBOSE
        if (pointer_status[15].known) {
            DMSG("WARNING: Reassigning stack pointer in OPTIMIZE_STACK mode");
        }
#endif
        DEFINE_REG(page);
        SRLI(page, reg, 19);
        DEFINE_REG(pageofs);
        SLLI(pageofs, page, LOG2_SIZEOF_PTR);
        DEFINE_REG(dp_base);
        MOVEA(dp_base, ADDR_HI(direct_pages));
        DEFINE_REG(dp_ptr);
        ADD(dp_ptr, dp_base, pageofs);
        if (!pointer_status[15].known) {
            DEFINE_REG(base);
            pointer_status[15].known = 1;
            pointer_status[15].type = 2;
            pointer_status[15].data_only = 1;
            pointer_status[15].rtl_basereg = base;
            pointer_status[15].rtl_ptrreg = 0; // stack_pointer is used instead
            DEFINE_REG(new_sp);
            stack_pointer = new_sp;
        }
        PRECOND(pointer_status[15].rtl_basereg != 0, return 0);
        LOAD_PTR(pointer_status[15].rtl_basereg, dp_ptr,
                 ADDR_LO(direct_pages));
        PRECOND(stack_pointer != 0, return 0);
        ADD(stack_pointer, pointer_status[15].rtl_basereg, reg);
    }
#ifdef OPTIMIZE_STATE_BLOCK
    const int index = state_cache_index(offset);
    if (index >= 0) {
        if (state_cache[index].fixed) {
            if (reg != state_cache[index].rtlreg) {
                MOVE(state_cache[index].rtlreg, reg);
            }
        } else {
            state_cache[index].rtlreg = reg;
        }
        if (index < 16) {
            pointer_status[index].rtl_ptrreg = 0;
        }
        state_cache[index].offset = 0;
        if (index == state_cache_index(offsetof(SH2State,PC))) {
            cached_PC = 0;
            stored_PC = 0;
        }
        state_dirty |= 1 << index;
# ifdef TRACE_STEALTH
        if (index < 23) {
            APPEND(NOP, 0, 0xB0000000 | index<<16 | state_cache[index].rtlreg,
                   0, 0);
        }
# endif
        return 1;
    }
#endif
    STORE_W(state_reg, reg, offset);
    return 1;
}

#define STORE_STATE_PC(value) \
    ASSERT(__STORE_STATE_PC(entry, state_reg, (value)))
static inline int __STORE_STATE_PC(const JitEntry * const entry,
                                   const uint32_t state_reg,
                                   const uint32_t value)
{
#ifdef OPTIMIZE_STATE_BLOCK
    const int index_PC = state_cache_index(offsetof(SH2State,PC));
    if (!state_cache[index_PC].fixed && value != 0) {
        if (stored_PC == value) {
            /* state->PC is already correct, so just clear the cache */
            if (state_cache[index_PC].rtlreg) {
# ifdef TRACE_STEALTH
                APPEND(NOP, 0, 0xB0000000 | index_PC<<16, 0, 0);
# endif
            }
            cached_PC = 0;
            state_cache[index_PC].rtlreg = 0;
            state_dirty &= ~(1<<index_PC);
            return 1;  // No need to update
        } else {
# ifdef TRACE_STEALTH
            APPEND(NOP, 0, 0xB8000000 | index_PC<<16 | (value >> 16), 0, 0);
            APPEND(NOP, 0, 0xBC000000 | index_PC<<16 | (value & 0xFFFF), 0, 0);
            APPEND(NOP, 0, 0xB0000000 | index_PC<<16, 0, 0);
# endif
            cached_PC = value;
            state_dirty |= 1<<index_PC;
            return 1;  // Don't bother creating a register
        }
    }
    stored_PC = value;
#endif
    DEFINE_REG(reg);
    MOVEI(reg, value);
    return __STORE_STATE(entry, state_reg, offsetof(SH2State,PC), reg);
}

#define STORE_STATE_B(field,reg) \
    STORE_B(state_reg, (reg), offsetof(SH2State,field))

#define STORE_STATE_PTR(field,reg) \
    STORE_PTR(state_reg, (reg), offsetof(SH2State,field))

#define STORE_STATE_SR_T(reg) \
    ASSERT(__STORE_STATE_SR_T(entry, state_reg, (reg)))
static inline int __STORE_STATE_SR_T(const JitEntry * const entry,
                                     const uint32_t state_reg,
                                     const uint32_t reg)
{
#ifdef OPTIMIZE_STATE_BLOCK
    cached_SR_T = (reg);
    dirty_SR_T = 1;
# ifdef TRACE_STEALTH
    APPEND(NOP, 0, 0xB0900000 | (reg), 0, 0);
# endif
#else
    DEFINE_REG(old_SR);
    LOAD_W(old_SR, state_reg, offsetof(SH2State,SR));
    DEFINE_REG(new_SR);
    BFINS(new_SR, old_SR, (reg), SR_T_SHIFT, 1);
    STORE_W(state_reg, new_SR, offsetof(SH2State,SR));
#endif
    return 1;
}

#define FLUSH_STATE_SR_T(reg)  ASSERT(__FLUSH_STATE_SR_T(entry, state_reg))
static inline int __FLUSH_STATE_SR_T(const JitEntry * const entry,
                                     const uint32_t state_reg)
{
#ifdef OPTIMIZE_STATE_BLOCK
    if (dirty_SR_T) {
        const int index_SR = state_cache_index(offsetof(SH2State,SR));
        DECLARE_REG(old_SR);
        LOAD_STATE_ALLOC(old_SR, SR);
        DECLARE_REG(new_SR);
        if (state_cache[index_SR].fixed) {
            new_SR = old_SR;
        } else {
            ALLOC_REG(new_SR);
        }
        BFINS(new_SR, old_SR, cached_SR_T, SR_T_SHIFT, 1);
        STORE_STATE(SR, new_SR);
        cached_SR_T = 0;
        dirty_SR_T = 0;
    }
#endif
    return 1;
}

#ifdef OPTIMIZE_STATE_BLOCK
# define RESET_STATE_SR_T()  (cached_SR_T = 0, dirty_SR_T = 0)
#else
# define RESET_STATE_SR_T()  /*nothing*/
#endif

/* Adds */

#define ADDI_STATE(field,imm,reg) \
    ASSERT(__ADDI_STATE(entry, state_reg, (reg), offsetof(SH2State,field), \
           (imm)))
#define ADDI_STATE_NOREG(field,imm) \
    ASSERT(__ADDI_STATE(entry, state_reg, 0, offsetof(SH2State,field), \
           (imm)))
static inline int __ADDI_STATE(const JitEntry * const entry,
                               const uint32_t state_reg, uint32_t cur_reg,
                               const uint32_t offset, const int32_t imm)
{
#ifdef OPTIMIZE_CONSTANT_ADDS
    const int index = state_cache_index(offset);
    if (index >= 0
     && (state_cache[index].rtlreg || !state_cache[index].fixed)
    ) {
        if (!state_cache[index].rtlreg) {
            DEFINE_REG(rtlreg);
            LOAD_W(rtlreg, state_reg, offset);
            state_cache[index].rtlreg = rtlreg;
            state_cache[index].offset = 0;
        }
        const int32_t new_offset = (int32_t)state_cache[index].offset + imm;
        if ((uint32_t)(new_offset + 0x8000) < 0x10000) {
            state_cache[index].offset = new_offset;
        } else {
            if (state_cache[index].fixed) {
                ADDI(state_cache[index].rtlreg, state_cache[index].rtlreg,
                     new_offset);
            } else {
                DEFINE_REG(newreg);
                ADDI(newreg, state_cache[index].rtlreg, new_offset);
                state_cache[index].rtlreg = newreg;
                if (index < 16) {
                    pointer_status[index].rtl_ptrreg = 0;
                }
            }
            state_cache[index].offset = 0;
        }
        state_dirty |= 1 << index;
# ifdef TRACE_STEALTH
        if (index < 23) {
            uint32_t temp = state_cache[index].offset;
            APPEND(NOP, 0, 0xB8000000 | index<<16 | (temp>>16 & 0xFFFF), 0, 0);
            APPEND(NOP, 0, 0xBC000000 | index<<16 | (temp>> 0 & 0xFFFF), 0, 0);
            APPEND(NOP, 0, 0xB0000000 | index<<16 | state_cache[index].rtlreg,
                   0, 0);
        }
# endif
        return 1;
    }
#endif
    if (!cur_reg) {
        ASSERT(cur_reg = __LOAD_STATE_ALLOC(entry, state_reg, offset));
    }
    DEFINE_REG(result);
    ADDI(result, cur_reg, imm);
    STORE_W(state_reg, result, offset);
    if (offset == offsetof(SH2State,R[15])
     && (optimization_flags & SH2_OPTIMIZE_STACK)
     && stack_pointer
    ) {
        ADDI(stack_pointer, stack_pointer, imm);
    }
    return 1;
}

/*----------------------------------*/

/* Execute an SH-2 load or store operation (note that size desginations are
 * SH-2 style B[yte]/W[ord]/L[ong] rather than RTL B[yte]/H[alfword]/W[ord],
 * and all 8- and 16-bit loads are signed) */

#define SH2_LOAD_B(dest,address) \
    do_load(entry, (dest), (address), SH2_ACCESS_TYPE_B)
#define SH2_LOAD_W(dest,address) \
    do_load(entry, (dest), (address), SH2_ACCESS_TYPE_W)
#define SH2_LOAD_L(dest,address) \
    do_load(entry, (dest), (address), SH2_ACCESS_TYPE_L)
#define SH2_STORE_B(address,src) \
    do_store(entry, (address), (src), SH2_ACCESS_TYPE_B, state_reg)
#define SH2_STORE_W(address,src) \
    do_store(entry, (address), (src), SH2_ACCESS_TYPE_W, state_reg)
#define SH2_STORE_L(address,src) \
    do_store(entry, (address), (src), SH2_ACCESS_TYPE_L, state_reg)

/*----------------------------------*/

/* Execute an SH-2 load or store to a known address */

#define SH2_LOAD_ABS_B(dest,address) \
    do_load_abs(entry, (dest), (address), SH2_ACCESS_TYPE_B)
#define SH2_LOAD_ABS_W(dest,address) \
    do_load_abs(entry, (dest), (address), SH2_ACCESS_TYPE_W)
#define SH2_LOAD_ABS_L(dest,address) \
    do_load_abs(entry, (dest), (address), SH2_ACCESS_TYPE_L)
#define SH2_STORE_ABS_B(address,src,islocal) \
    do_store_abs(entry, (address), (src), SH2_ACCESS_TYPE_B, state_reg, \
                 (islocal))
#define SH2_STORE_ABS_W(address,src,islocal) \
    do_store_abs(entry, (address), (src), SH2_ACCESS_TYPE_W, state_reg, \
                 (islocal))
#define SH2_STORE_ABS_L(address,src,islocal) \
    do_store_abs(entry, (address), (src), SH2_ACCESS_TYPE_L, state_reg, \
                 (islocal))

/*----------------------------------*/

/* Execute an SH-2 load or store through an SH-2 register */

#define SH2_LOAD_REG_B(dest,sh2reg,offset,postinc) \
    do_load_reg(entry, (dest), (sh2reg), (offset), SH2_ACCESS_TYPE_B, \
                (postinc), state, state_reg)
#define SH2_LOAD_REG_W(dest,sh2reg,offset,postinc) \
    do_load_reg(entry, (dest), (sh2reg), (offset), SH2_ACCESS_TYPE_W, \
                (postinc), state, state_reg)
#define SH2_LOAD_REG_L(dest,sh2reg,offset,postinc) \
    do_load_reg(entry, (dest), (sh2reg), (offset), SH2_ACCESS_TYPE_L, \
                (postinc), state, state_reg)
#define SH2_STORE_REG_B(sh2reg,src,offset,predec) \
    do_store_reg(entry, (sh2reg), (src), (offset), SH2_ACCESS_TYPE_B, \
                 (predec), state, state_reg)
#define SH2_STORE_REG_W(sh2reg,src,offset,predec) \
    do_store_reg(entry, (sh2reg), (src), (offset), SH2_ACCESS_TYPE_W, \
                 (predec), state, state_reg)
#define SH2_STORE_REG_L(sh2reg,src,offset,predec) \
    do_store_reg(entry, (sh2reg), (src), (offset), SH2_ACCESS_TYPE_L, \
                 (predec), state, state_reg)

/*----------------------------------*/

/* Branches (within an SH-2 instruction's RTL code) */

#define CREATE_LABEL(label)                             \
    const uint32_t label = rtl_alloc_label(entry->rtl); \
    if (UNLIKELY(!label)) {                             \
        return 0;                                       \
    }
#define DEFINE_LABEL(label)     APPEND(LABEL, 0, 0, 0, (label))
#define GOTO_LABEL(label)       APPEND(GOTO, 0, 0, 0, (label))
#define GOTO_IF_Z(label,reg)    APPEND(GOTO_IF_Z, 0, (reg), 0, (label))
#define GOTO_IF_NZ(label,reg)   APPEND(GOTO_IF_NZ, 0, (reg), 0, (label))
#define GOTO_IF_E(label,reg1,reg2) \
    APPEND(GOTO_IF_E, 0, (reg1), (reg2), (label))
#define GOTO_IF_NE(label,reg1,reg2) \
    APPEND(GOTO_IF_NE, 0, (reg1), (reg2), (label))

/*----------------------------------*/

/* Jumps (to other SH-2 instructions) */

#define JUMP_STATIC() \
    ASSERT(branch_static(state, entry, state_reg))
#define JUMP()  RETURN()

/* Call to a native subroutine */
#define CALL(result,arg1,arg2,func) \
    APPEND(CALL, (result), (arg1), (arg2), (func))
#define CALL_NORET(arg1,arg2,func) \
    APPEND(CALL, 0, (arg1), (arg2), (func))

/* Return from the current block */
#define RETURN()  APPEND(RETURN, 0, 0, 0, 0)

/* Chain to a different native routine */
#define RETURN_TO(addr)  APPEND(RETURN_TO, 0, 0, 0, addr)

/*-----------------------------------------------------------------------*/

/* Use global variables for the PC and cycle count; the initial PC can be
 * taken from the JitEntry structure */
#define initial_PC  (entry->sh2_start)
#define cur_PC      jit_PC

/* Pre- and post-decode processing is implemented by separate functions
 * defined below */
#define OPCODE_INIT(opcode) \
    ASSERT(opcode_init(state, entry, state_reg, (opcode), recursing))
#define OPCODE_DONE(opcode) \
    ASSERT(opcode_done(state, entry, state_reg, (opcode), recursing))


/* Need to update both jit_PC and state->PC */
#define INC_PC()  do {                  \
    jit_PC += 2;                        \
    STORE_STATE_PC(jit_PC);             \
} while (0)
#ifdef JIT_DEBUG_TRACE
/* Make sure we trace instructions even if eliminated by optimization */
# define INC_PC_BY(amount)    do {      \
    const unsigned int __amount = (amount); \
    unsigned int __i;                   \
    for (__i = 0; __i < __amount; __i += 2) { \
        jit_PC += 2;                    \
        char tracebuf[100];             \
        const unsigned int __opcode = MappedMemoryReadWord(jit_PC); \
        SH2Disasm(jit_PC, __opcode, 0, tracebuf); \
        fprintf(stderr, "%08X: %04X  %s\n", jit_PC, __opcode, tracebuf+12); \
    }                                   \
    STORE_STATE_PC(jit_PC);             \
} while (0)
#else  // !JIT_DEBUG_TRACE
# define INC_PC_BY(amount)    do {      \
    jit_PC += (amount);                 \
    STORE_STATE_PC(jit_PC);             \
} while (0)
#endif

/* Return whether the word at "offset" words from the current instruction
 * is available for peephole optimization */
#define INSN_IS_AVAILABLE(offset) \
    (recursing ? !is_last :                                                   \
     jit_PC + (offset)*2 <= entry->sh2_end                                    \
     && (word_info[(jit_PC - entry->sh2_start)/2 + (offset)] & WORD_INFO_CODE)\
     && !(is_branch_target[((jit_PC - entry->sh2_start) / 2 + (offset)) / 32] \
          & (1 << (((jit_PC - entry->sh2_start) / 2 + (offset)) % 32))))

/*----------------------------------*/

/* Return whether the saturation check for MAC can be omitted */
#define CAN_OMIT_MAC_S_CHECK    (can_optimize_mac_nosat)

/* Get or set whether the MACL/MACH pair is known to be zero */
#define MAC_IS_ZERO()           (state->mac_is_zero)
#define SET_MAC_IS_ZERO()       (state->mac_is_zero = 1)
#define CLEAR_MAC_IS_ZERO()     (state->mac_is_zero = 0)

/*----------------------------------*/

/* Get, add to, or clear the cached shift count */
#define CAN_CACHE_SHIFTS()      1
#define CACHED_SHIFT_COUNT()    (state->cached_shift_count)
#define ADD_TO_SHIFT_CACHE(n)   (state->cached_shift_count += (n))
#define CLEAR_SHIFT_CACHE()     (state->cached_shift_count = 0)

/*----------------------------------*/

/* Get or set register known bits and values */
#ifdef OPTIMIZE_KNOWN_VALUES
# define REG_GETKNOWN(reg)        reg_knownbits[(reg)]
# define REG_GETVALUE(reg)        reg_value[(reg)]
# define REG_SETKNOWN(reg,known)  (reg_knownbits[(reg)] = (known))
# define REG_SETVALUE(reg,value)  (reg_value[(reg)] = (value))
# define REG_RESETKNOWN()  do {                         \
    unsigned int __i;                                   \
    for (__i = 0; __i < lenof(reg_knownbits); __i++) {  \
        reg_knownbits[__i] = 0;                         \
    }                                                   \
} while (0)
#else
# define REG_GETKNOWN(reg)  0
# define REG_GETVALUE(reg)  0
# define REG_SETKNOWN(reg,value)  /*nothing*/
# define REG_SETVALUE(reg,value)  /*nothing*/
# define REG_RESETKNOWN()         /*nothing*/
#endif

/*----------------------------------*/

/* Track pointer registers */

/* Check or set the local-pointer flag for a GPR (used to skip JIT overwrite
 * checks in do_store_abs()); flag is cleared by PTR_CLEAR() */
#define PTR_ISLOCAL(reg)   (pointer_local & 1<<(reg))
#define PTR_SETLOCAL(reg)  do {                         \
    if (optimization_flags & SH2_OPTIMIZE_LOCAL_POINTERS) { \
        pointer_local |= 1<<(reg);                      \
    }                                                   \
} while (0)

/* Mark a GPR as having taken its value from the given local address */
#define PTR_SET_SOURCE(reg,address)  do {               \
    if (optimization_flags & SH2_OPTIMIZE_LOCAL_POINTERS) { \
        pointer_status[(reg)].source = (address);       \
    }                                                   \
} while (0)

/* Return whether the given register is a known pointer */
#define PTR_CHECK(reg)  (pointer_status[(reg)].known != 0 \
                         || pointer_status[(reg)].rtl_basereg != 0)

/* Copy pointer status for a MOVE Rm,Rn or ADD Rm,Rn instruction */
#define PTR_COPY(reg,new,for_add)  do {                 \
    const unsigned int __reg = (reg);                   \
    const unsigned int __new = (new);                   \
    pointer_status[__new] = pointer_status[__reg];      \
    if (pointer_status[__reg].known) {                  \
        pointer_status[__new].known = -1;               \
    }                                                   \
    if (for_add) {                                      \
        pointer_status[__new].rtl_ptrreg = 0;           \
    }                                                   \
    if (PTR_ISLOCAL(__reg)) {                           \
        pointer_local |= 1<<__new;                      \
    } else {                                            \
        pointer_local &= ~(1<<__new);                   \
    }                                                   \
} while (0)

/* Clear pointer status on a register modification */
#define PTR_CLEAR(reg)  do {                            \
    const unsigned int __reg = (reg);                   \
    if (__reg != 15 || !(optimization_flags & SH2_OPTIMIZE_STACK)) { \
        pointer_status[__reg].known = 0;                \
        pointer_status[__reg].rtl_basereg = 0;          \
        pointer_status[__reg].rtl_ptrreg = 0;           \
        pointer_status[__reg].source = 0;               \
        pointer_local &= ~(1<<__reg);                   \
    }                                                   \
} while (0)

/*----------------------------------*/

/* Processor state block caching */

/* Save the current cache state */
#define SAVE_STATE_CACHE()  do {                                \
    memcpy(saved_state_cache, state_cache, sizeof(state_cache));\
    saved_state_dirty = state_dirty;                            \
    saved_cached_SR_T = cached_SR_T;                            \
    saved_dirty_SR_T = dirty_SR_T;                              \
    saved_cached_PC = cached_PC;                                \
    saved_stored_PC = stored_PC;                                \
} while (0)

/* Restore the saved cache state */
#define RESTORE_STATE_CACHE()  do {                             \
    RESTORE_STATE_CACHE_APPEND_STEALTH_NOPS();                  \
    memcpy(state_cache, saved_state_cache, sizeof(state_cache));\
    state_dirty = saved_state_dirty;                            \
    cached_SR_T = saved_cached_SR_T;                            \
    dirty_SR_T = saved_dirty_SR_T;                              \
    cached_PC = saved_cached_PC;                                \
    stored_PC = saved_stored_PC;                                \
} while (0)
#ifdef TRACE_STEALTH
# define RESTORE_STATE_CACHE_APPEND_STEALTH_NOPS()  do {        \
    unsigned int __i;                                           \
    for (__i = 0; __i < 23; __i++) {                            \
        if (__i == 22 && saved_cached_PC != 0) {                \
            APPEND(NOP, 0, 0xB8160000 | (saved_cached_PC>>16 & 0xFFFF), 0, 0);\
            APPEND(NOP, 0, 0xBC160000 | (saved_cached_PC>> 0 & 0xFFFF), 0, 0);\
            APPEND(NOP, 0, 0xB0160000, 0, 0);                   \
        } else if ((saved_state_dirty & (1<<__i))               \
                   && (!(state_dirty & (1<<__i))                \
                       || saved_state_cache[__i].rtlreg != state_cache[__i].rtlreg \
                       || saved_state_cache[__i].offset != state_cache[__i].offset) \
        ) {                                                     \
            uint32_t __temp = saved_state_cache[__i].offset;    \
            if (__temp) {                                       \
                APPEND(NOP, 0, 0xB8000000 | __i<<16 | (__temp>>16 & 0xFFFF), 0, 0); \
                APPEND(NOP, 0, 0xBC000000 | __i<<16 | (__temp>> 0 & 0xFFFF), 0, 0); \
            }                                                   \
            APPEND(NOP, 0, 0xB0000000 | __i<<16 | saved_state_cache[__i].rtlreg, 0, 0); \
        } else if (!(saved_state_dirty & (1<<__i)) && (state_dirty & (1<<__i))) { \
            APPEND(NOP, 0, 0xB0000000 | __i<<16, 0, 0);         \
        }                                                       \
    }                                                           \
    if (saved_dirty_SR_T && (!dirty_SR_T || saved_cached_SR_T != cached_SR_T)) { \
        APPEND(NOP, 0, 0xB0900000 | saved_cached_SR_T, 0, 0);   \
    } else if (!saved_dirty_SR_T && dirty_SR_T) {               \
        APPEND(NOP, 0, 0xB0900000, 0, 0);                       \
    }                                                           \
} while (0)
#else
# define RESTORE_STATE_CACHE_APPEND_STEALTH_NOPS()  /*nothing*/
#endif

#ifndef OPTIMIZE_STATE_BLOCK
# undef SAVE_STATE_CACHE
# undef RESTORE_STATE_CACHE
# define SAVE_STATE_CACHE()  /*nothing*/
# define RESTORE_STATE_CACHE()  /*nothing*/
#endif

/* Write back to the state block any cached, dirty state block values
 * (but leave them dirty) */
#define WRITEBACK_STATE_CACHE() \
    ASSERT(writeback_state_cache(entry, state_reg, 0));

/* Flush any cached state block values */
#define FLUSH_STATE_CACHE()  do {                       \
    ASSERT(writeback_state_cache(entry, state_reg, 1)); \
    clear_state_cache(0);                               \
} while (0)

/* Return the cached offset for the given state block field, or 0 if none */
#ifdef OPTIMIZE_CONSTANT_ADDS
# define STATE_CACHE_OFFSET(field) \
    (state_cache_index(offsetof(SH2State,field)) >= 0 \
     ? state_cache[state_cache_index(offsetof(SH2State,field))].offset \
     : 0)
#else
# define STATE_CACHE_OFFSET(field)  0
#endif

/* Return the fixed RTL register to use for the given state block field,
 * or 0 if none */
#ifdef OPTIMIZE_STATE_BLOCK
# define STATE_CACHE_FIXED_REG(field) \
    (state_cache_index(offsetof(SH2State,field)) >= 0 \
     && state_cache[state_cache_index(offsetof(SH2State,field))].fixed \
     ? state_cache[state_cache_index(offsetof(SH2State,field))].rtlreg \
     : 0)
#else
# define STATE_CACHE_FIXED_REG(field)  0
#endif

/* Return whether the given state block field has a fixed RTL register that
 * can be modified */
#ifdef OPTIMIZE_STATE_BLOCK
# define STATE_CACHE_FIXED_REG_WRITABLE(field) \
    (state_cache_index(offsetof(SH2State,field)) >= 0 \
     ? state_cache[state_cache_index(offsetof(SH2State,field))].fixed \
       && state_cache[state_cache_index(offsetof(SH2State,field))].flush \
     : 0)
#else
# define STATE_CACHE_FIXED_REG_WRITABLE(field)  0
#endif

/* Clear any fixed RTL register for the given state block field */
#ifdef OPTIMIZE_STATE_BLOCK
# define STATE_CACHE_CLEAR_FIXED_REG(field)  do { \
    if (state_cache_index(offsetof(SH2State,field)) >= 0) { \
        state_cache[state_cache_index(offsetof(SH2State,field))].fixed = 0; \
        state_cache[state_cache_index(offsetof(SH2State,field))].flush = 0; \
    } \
} while (0)
#else
# define STATE_CACHE_CLEAR_FIXED_REG(field)  /*nothing*/
#endif

/*----------------------------------*/

/* Check the status of a branch instruction */

#define BRANCH_FALLS_THROUGH(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_FALLTHRU \
     : 0)
#define BRANCH_TARGETS_RTS(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_BRA_RTS \
     : 0)

#define BRANCH_IS_THREADED(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_THREADED \
     : 0)

#define BRANCH_THREAD_TARGET(addr) \
    (branch_thread_target[((addr) - entry->sh2_start) / 2])

#define BRANCH_THREAD_COUNT(addr) \
    (branch_thread_count[((addr) - entry->sh2_start) / 2])

#define BRANCH_IS_SELECT(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_SELECT \
     : 0)

#define BRANCH_IS_LOOP_TO_JSR(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_LOOP_JSR \
     : 0)

#define BRANCH_IS_FOLDABLE_SUBROUTINE(addr) \
    ((addr) >= entry->sh2_start && (addr) <= entry->sh2_end \
     ? word_info[((addr) - entry->sh2_start) / 2] & WORD_INFO_FOLDABLE \
     : 0)

#define BRANCH_FOLD_TARGET(addr) \
    (subroutine_target[((addr) - entry->sh2_start) / 2])

#define BRANCH_FOLD_TARGET_FETCH(addr) \
    ((const uint16_t *)((uintptr_t)fetch_pages[(addr) >> 19] + (addr)))

#define BRANCH_FOLD_NATIVE_FUNC(addr) \
    (subroutine_native[((addr) - entry->sh2_start) / 2])

/*************************************************************************/
/************* Helper functions for SH-2 -> RTL translation **************/
/*************************************************************************/

/**
 * do_load_common:  Generate code for an SH-2 memory load operation common
 * to both generic loads and register loads from variable addresses.
 *
 * [Parameters]
 *        entry: Block being translated
 *         dest: RTL register into which value is to be loaded
 *      address: RTL register holding load address
 *       offset: Offset to be added to address
 *         type: Access type (SH2_ACCESS_TYPE_*)
 *     page_ptr: RTL register holding base pointer for memory page or NULL
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_load_common(const JitEntry *entry, uint32_t dest,
                          uint32_t address, int32_t offset,
                          unsigned int type, uint32_t page_ptr)
{
    CREATE_LABEL(label_fallback);
    CREATE_LABEL(label_done);

    DEFINE_REG(final_ptr); // Move this up to help MIPS delay slot optimization
    if (offset < -0x8000 || offset > 0x7FFF) {
        /* Memory offsets must be within the range [-0x8000,0x7FFF], so if
         * we fall outside that range, add the offset to the address
         * separately */
        DEFINE_REG(offset_addr);
        ADDI(offset_addr, address, offset);
        offset = 0;
        ADD(final_ptr, page_ptr, offset_addr);
    } else {
        ADD(final_ptr, page_ptr, address);
    }

    GOTO_IF_Z(label_fallback, page_ptr); {
        /* We can access the data directly */
        // FIXME: all the code here assumes little-endian; this won't work
        // if we port to a big-endian machine
        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            DEFINE_REG(real_ptr);
            int32_t real_offset;
            if (offset & 1) {
                /* Can't use an immediate offset because we don't know
                 * whether the source address is odd, so we can't predict
                 * the effect of the XOR */
                DEFINE_REG(offset_ptr);
                ADDI(offset_ptr, final_ptr, offset);
                XORI(real_ptr, offset_ptr, 1);
                real_offset = 0;
            } else {
                XORI(real_ptr, final_ptr, 1);
                real_offset = offset;
            }
            LOAD_BS(dest, real_ptr, real_offset);
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            LOAD_HS(dest, final_ptr, offset);
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            DEFINE_REG(swapped);
            LOAD_W(swapped, final_ptr, offset);
            HSWAPW(dest, swapped);
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }
        GOTO_LABEL(label_done);

    } DEFINE_LABEL(label_fallback); {
        /* Not direct access, so call the fallback routine */
        DECLARE_REG(real_address);
        if (offset) {
            ALLOC_REG(real_address);
            ADDI(real_address, address, offset);
        } else {
            real_address = address;
        }
        DEFINE_REG(fallback);
        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            MOVEA(fallback, MappedMemoryReadByte);
            DEFINE_REG(retval);
            CALL(retval, real_address, 0, fallback);
            DEFINE_REG(tempdest);
            SLLI(tempdest, retval, 24);
            SRAI(dest, tempdest, 24);
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            MOVEA(fallback, MappedMemoryReadWord);
            DEFINE_REG(retval);
            CALL(retval, real_address, 0, fallback);
            DEFINE_REG(tempdest);
            SLLI(tempdest, retval, 16);
            SRAI(dest, tempdest, 16);
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            MOVEA(fallback, MappedMemoryReadLong);
            CALL(dest, real_address, 0, fallback);
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }

    } DEFINE_LABEL(label_done);

    return 1;
}

/*----------------------------------*/

/**
 * do_load:  Generate code for an SH-2 memory load operation.
 *
 * [Parameters]
 *       entry: Block being translated
 *        dest: RTL register into which value is to be loaded
 *     address: RTL register holding load address
 *        type: Access type (SH2_ACCESS_TYPE_*)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_load(const JitEntry *entry, uint32_t dest, uint32_t address,
                   unsigned int type)
{
    DEFINE_REG(page);
    SRLI(page, address, 19);
    DEFINE_REG(dp_base);
    MOVEA(dp_base, ADDR_HI(direct_pages));
    DEFINE_REG(temp);
    SLLI(temp, page, LOG2_SIZEOF_PTR);
    DEFINE_REG(dp_ptr);
    ADD(dp_ptr, dp_base, temp);
    DEFINE_REG(page_ptr);
    LOAD_PTR(page_ptr, dp_ptr, ADDR_LO(direct_pages));

    return do_load_common(entry, dest, address, 0, type, page_ptr);
}

/*----------------------------------*/

/**
 * do_load_abs:  Generate optimized code for an SH-2 memory load operation
 * from a known address.
 *
 * [Parameters]
 *       entry: Block being translated
 *        dest: RTL register into which value is to be loaded
 *     address: Load address (in SH-2 address space)
 *        type: Access type (SH2_ACCESS_TYPE_*)
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_load_abs(const JitEntry *entry, uint32_t dest, uint32_t address,
                       unsigned int type)
{
    const uint32_t page = address >> 19;

    if (direct_pages[page]) {

        const uintptr_t real_address =
            (uintptr_t)direct_pages[page] + address;
        DEFINE_REG(addr_reg);
        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            MOVEA(addr_reg, ADDR_HI(real_address ^ 1));
            LOAD_BS(dest, addr_reg, ADDR_LO(real_address ^ 1));
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            MOVEA(addr_reg, ADDR_HI(real_address));
            LOAD_HS(dest, addr_reg, ADDR_LO(real_address));
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            MOVEA(addr_reg, ADDR_HI(real_address));
            DEFINE_REG(swapped);
            LOAD_W(swapped, addr_reg, ADDR_LO(real_address));
            HSWAPW(dest, swapped);
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }

    } else {

        DEFINE_REG(addr_reg);
        MOVEA(addr_reg, address);
        DEFINE_REG(fallback);
        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            MOVEA(fallback, MappedMemoryReadByte);
            DEFINE_REG(retval);
            CALL(retval, addr_reg, 0, fallback);
            DEFINE_REG(temp);
            SLLI(temp, retval, 24);
            SRAI(dest, temp, 24);
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            MOVEA(fallback, MappedMemoryReadWord);
            DEFINE_REG(retval);
            CALL(retval, addr_reg, 0, fallback);
            DEFINE_REG(temp);
            SLLI(temp, retval, 16);
            SRAI(dest, temp, 16);
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            MOVEA(fallback, MappedMemoryReadLong);
            CALL(dest, addr_reg, 0, fallback);
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }

    }

    return 1;
}

/*----------------------------------*/

/**
 * do_load_reg:  Generate optimized code for an SH-2 memory load operation
 * through an SH-2 register.
 *
 * [Parameters]
 *         entry: Block being translated
 *          dest: RTL register into which value is to be loaded
 *        sh2reg: SH-2 register holding load address (0-15)
 *        offset: Offset to be added to address
 *          type: Access type (SH2_ACCESS_TYPE_*)
 *       postinc: Nonzero if a postincrement access, else zero
 *         state: SH-2 state block pointer
 *     state_reg: RTL register holding state block pointer
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_load_reg(const JitEntry *entry, uint32_t dest,
                       unsigned int sh2reg, int32_t offset, unsigned int type,
                       int postinc, const SH2State *state,
                       uint32_t state_reg)
{
    const int postinc_size =
        !postinc ? 0 :
        type==SH2_ACCESS_TYPE_L ? 4 : type==SH2_ACCESS_TYPE_W ? 2 : 1;

#ifdef OPTIMIZE_STATE_BLOCK
    offset += state_cache[state_cache_index(offsetof(SH2State,R[sh2reg]))].offset;
#endif

    if (!pointer_status[sh2reg].known && pointer_status[sh2reg].source != 0) {
        const uint32_t address =
            MappedMemoryReadLong(pointer_status[sh2reg].source);
        pointer_status[sh2reg].known = -1;  // Only tentatively known
        pointer_status[sh2reg].data_only = 0;
        if ((address & 0xDFF00000) == 0x00200000
         || (address & 0xDE000000) == 0x06000000
        ) {
            pointer_status[sh2reg].type = 2;
            DEFINE_REG(basereg);
            MOVEA(basereg, direct_pages[address>>19]);
            pointer_status[sh2reg].rtl_basereg = basereg;
        } else if ((address & 0xDFF80000) == 0x05C00000
                || (address & 0xDFF00000) == 0x05E00000
        ) {
            pointer_status[sh2reg].type = 1;
            DEFINE_REG(basereg);
            MOVEA(basereg, byte_direct_pages[address>>19]);
            pointer_status[sh2reg].rtl_basereg = basereg;
        } else {
            pointer_status[sh2reg].rtl_basereg = 0;
        }
        pointer_status[sh2reg].rtl_ptrreg = 0;
    }

    if (pointer_status[sh2reg].known) {

        DECLARE_REG(address);
        LOAD_STATE_ALLOC_KEEPOFS(address, R[sh2reg]);

        if (pointer_status[sh2reg].rtl_basereg) {

            DECLARE_REG(ptr);
            if (sh2reg == 15 && stack_pointer) {
                if (offset < -0x8000 || offset > 0x7FFF) {
                    ALLOC_REG(ptr);
                    ADDI(ptr, stack_pointer, offset);
                    offset = 0;
                } else {
                    ptr = stack_pointer;
                }
            } else {
                if (offset < -0x8000 || offset > 0x7FFF) {
                    DEFINE_REG(offset_addr);
                    ADDI(offset_addr, address, offset);
                    offset = 0;
                    ALLOC_REG(ptr);
                    ADD(ptr, pointer_status[sh2reg].rtl_basereg, offset_addr);
                } else if (pointer_status[sh2reg].rtl_ptrreg) {
                    ptr = pointer_status[sh2reg].rtl_ptrreg;
                } else {
                    ALLOC_REG(ptr);
                    ADD(ptr, pointer_status[sh2reg].rtl_basereg, address);
                    pointer_status[sh2reg].rtl_ptrreg = ptr;
                }
            }
            switch (type) {
              case SH2_ACCESS_TYPE_B: {
                DECLARE_REG(real_ptr);
                int32_t real_offset;
                if (sh2reg == 15 && (optimization_flags & SH2_OPTIMIZE_STACK)){
                    /* Assume the stack is 32-bit aligned, so we can apply
                     * the XOR directly to the offset */
                    real_ptr = ptr;
                    real_offset = offset ^ 1;
                } else if (pointer_status[sh2reg].type == 1) {
                    /* No need to modify address for byte-ordered memory */
                    real_ptr = ptr;
                    real_offset = offset;
                } else if (offset & 1) {
                    DEFINE_REG(offset_ptr);
                    ADDI(offset_ptr, ptr, offset);
                    ALLOC_REG(real_ptr);
                    XORI(real_ptr, offset_ptr, 1);
                    real_offset = 0;
                } else {
                    ALLOC_REG(real_ptr);
                    XORI(real_ptr, ptr, 1);
                    real_offset = offset;
                }
                LOAD_BS(dest, real_ptr, real_offset);
                break;
              }
              case SH2_ACCESS_TYPE_W: {
                if (pointer_status[sh2reg].type == 1) {
                    DEFINE_REG(swapped);
                    LOAD_HU(swapped, ptr, offset);
                    DEFINE_REG(temp);
                    BSWAPH(temp, swapped);
                    DEFINE_REG(temp2);
                    SLLI(temp2, temp, 16);
                    SRAI(dest, temp2, 16);
                } else {
                    LOAD_HS(dest, ptr, offset);
                }
                break;
              }
              case SH2_ACCESS_TYPE_L: {
                DEFINE_REG(swapped);
                LOAD_W(swapped, ptr, offset);
                if (pointer_status[sh2reg].type == 1) {
                    BSWAPW(dest, swapped);
                } else {
                    HSWAPW(dest, swapped);
                }
                break;
              }
              default:
                DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
                return 0;
            }

        } else {  // !pointer_status[sh2reg].rtl_basereg

            DECLARE_REG(offset_addr);
            if (offset) {
                ALLOC_REG(offset_addr);
                ADDI(offset_addr, address, offset);
            } else {
                offset_addr = address;
            }
            DEFINE_REG(fallback);
            switch (type) {
              case SH2_ACCESS_TYPE_B: {
                MOVEA(fallback, MappedMemoryReadByte);
                DEFINE_REG(retval);
                CALL(retval, offset_addr, 0, fallback);
                DEFINE_REG(temp);
                SLLI(temp, retval, 24);
                SRAI(dest, temp, 24);
                break;
              }
              case SH2_ACCESS_TYPE_W: {
                MOVEA(fallback, MappedMemoryReadWord);
                DEFINE_REG(retval);
                CALL(retval, offset_addr, 0, fallback);
                DEFINE_REG(temp);
                SLLI(temp, retval, 16);
                SRAI(dest, temp, 16);
                break;
              }
              case SH2_ACCESS_TYPE_L: {
                MOVEA(fallback, MappedMemoryReadLong);
                CALL(dest, offset_addr, 0, fallback);
                break;
              }
              default:
                DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
                return 0;
            }

        }  // if (pointer_status[sh2reg].rtl_basereg)

    } else {  // !pointer_status[sh2reg].known

        DECLARE_REG(address);
        LOAD_STATE_ALLOC_KEEPOFS(address, R[sh2reg]);
        if (optimization_flags & SH2_OPTIMIZE_POINTERS) {
            if (!pointer_status[sh2reg].rtl_basereg) {
                DEFINE_REG(page);
                SRLI(page, address, 19);
                DEFINE_REG(dp_base);
                MOVEA(dp_base, ADDR_HI(direct_pages));
                DEFINE_REG(table_offset);
                SLLI(table_offset, page, LOG2_SIZEOF_PTR);
                DEFINE_REG(dp_ptr);
                ADD(dp_ptr, dp_base, table_offset);
                DEFINE_REG(basereg);
                LOAD_PTR(basereg, dp_ptr, ADDR_LO(direct_pages));
                pointer_status[sh2reg].rtl_basereg = basereg;
                pointer_status[sh2reg].rtl_ptrreg = 0;
                pointer_status[sh2reg].checked_djp = 0;
            }
            ASSERT(do_load_common(entry, dest, address, offset, type,
                                  pointer_status[sh2reg].rtl_basereg));
        } else {
            DECLARE_REG(offset_addr);
            if (offset) {
                ALLOC_REG(offset_addr);
                ADDI(offset_addr, address, offset);
            } else {
                offset_addr = address;
            }
            ASSERT(do_load(entry, dest, offset_addr, type));
        }

    }

    if (postinc_size) {
        ADDI_STATE_NOREG(R[sh2reg], postinc_size);
    }
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * log_store:  Helper function for do_store() and friends to log a store
 * operation.  Does nothing if no relevant tracing option is enabled.
 *
 * [Parameters]
 *       entry: Block being translated
 *     address: Register holding SH-2 store address, or store address itself
 *         src: Register holding value to store
 *      offset: Offset to be added to address
 *        type: Access type (SH2_ACCESS_TYPE_[BWL] only)
 *      is_abs: Nonzero if "address" is the actual store address
 * [Return value]
 *     Nonzero on success, zero on error
 */
static inline int log_store(const JitEntry *entry, uint32_t address, 
                            uint32_t src, int32_t offset, int type, int abs)
{
#if defined(TRACE)

    static SH2TraceAccessCallback ** const logfunc_ptrs[] = {
        [SH2_ACCESS_TYPE_B] = &trace_storeb_callback,
        [SH2_ACCESS_TYPE_W] = &trace_storew_callback,
        [SH2_ACCESS_TYPE_L] = &trace_storel_callback,
    };
    DEFINE_REG(funcptr);
    DECLARE_REG(addr_param);
    if (abs) {
        ALLOC_REG(addr_param);
        MOVEI(addr_param, address + offset);
    } else if (offset) {
        ALLOC_REG(addr_param);
        ADDI(addr_param, address, offset);
    } else {
        addr_param = address;
    }
    MOVEA(funcptr, *logfunc_ptrs[type]);
    CALL_NORET(addr_param, src, funcptr);

#elif defined(TRACE_STEALTH)

    static const unsigned int storecode[] = {
        [SH2_ACCESS_TYPE_B] = 1,
        [SH2_ACCESS_TYPE_W] = 2,
        [SH2_ACCESS_TYPE_L] = 4,
    };
    APPEND(NOP, 0, 0xC0000000 | storecode[type], 0, 0);
    if (abs) {
        address += offset;
        APPEND(NOP, 0, 0xD8000000 | ((address >> 16) & 0xFFFF), 0, 0);
        APPEND(NOP, 0, 0xDC000000 | (address & 0xFFFF), 0, 0);
    } else {
        APPEND(NOP, 0, 0xD0000000 | address, 0, 0);
        APPEND(NOP, 0, 0xD4000000 | ((offset >> 16) & 0xFFFF), 0, 0);
        APPEND(NOP, 0, 0xD6000000 | (offset & 0xFFFF), 0, 0);
    }
    APPEND(NOP, 0, 0xE0000000 | src, 0, 0);

#endif

    return 1;
}

/*----------------------------------*/

/**
 * do_store_common:  Generate code for an SH-2 memory store operation common
 * to both generic loads and register loads from variable addresses.
 *
 * [Parameters]
 *           entry: Block being translated
 *         address: RTL register holding store address
 *             src: RTL register holding value to be stored
 *          offset: Offset to be added to address
 *            type: Access type (SH2_ACCESS_TYPE_[BWL] only)
 *       state_reg: RTL register holding state block pointer
  *       page_ptr: RTL register holding base pointer for memory page or NULL
  *    djppage_ptr: RTL register holding base pointer for JIT flag table, or
 *                     zero to skip JIT write check
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_store_common(const JitEntry *entry, uint32_t address,
                           uint32_t src, int32_t offset, unsigned int type,
                           uint32_t state_reg, uint32_t page_ptr,
                           uint32_t djppage_ptr)
{
    CREATE_LABEL(label_fallback);
    CREATE_LABEL(label_done);

    DEFINE_REG(final_ptr); // Move this up to help MIPS delay slot optimization
    int32_t final_offset;
    if (offset < -0x8000 || offset > 0x7FFF) {
        DEFINE_REG(offset_addr);
        ADDI(offset_addr, address, offset);
        ADD(final_ptr, page_ptr, offset_addr);
        final_offset = 0;
    } else {
        ADD(final_ptr, page_ptr, address);
        final_offset = offset;
    }

    GOTO_IF_Z(label_fallback, page_ptr); {
        /* We can write data directly */

        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            DEFINE_REG(real_ptr);
            int32_t real_offset;
            if (final_offset & 1) {
                DEFINE_REG(offset_ptr);
                ADDI(offset_ptr, final_ptr, final_offset);
                XORI(real_ptr, offset_ptr, 1);
                real_offset = 0;
            } else {
                XORI(real_ptr, final_ptr, 1);
                real_offset = final_offset;
            }
            STORE_B(real_ptr, src, real_offset);
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            STORE_H(final_ptr, src, final_offset);
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            DEFINE_REG(swapped);
            HSWAPW(swapped, src);
            STORE_W(final_ptr, swapped, final_offset);
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }

        /* Check for modified translations and clear if needed */
        if (djppage_ptr) {
            /* Look up the appropriate byte in the table */
            DEFINE_REG(byteofs);
            SRLI(byteofs, address, JIT_PAGE_BITS);
            DEFINE_REG(byteaddr);
            ADD(byteaddr, djppage_ptr, byteofs);
            DEFINE_REG(test);
            LOAD_BU(test, byteaddr, 0);
            /* Does the JIT page contain translations? */
            GOTO_IF_Z(label_done, test); {
                /* Clear translations from the JIT page */
                DEFINE_REG(jcw_ptr);
                MOVEA(jcw_ptr, jit_clear_write);
                CALL_NORET(state_reg, address, jcw_ptr);
            } DEFINE_LABEL(label_done);
        }

    } DEFINE_LABEL(label_fallback); {
        /* Not direct access, so call the fallback routine */

        DECLARE_REG(real_address);
        if (final_offset) {
            ALLOC_REG(real_address);
            ADDI(real_address, address, final_offset);
        } else {
            real_address = address;
        }
        void *funcptr;
        switch (type) {
          case SH2_ACCESS_TYPE_B: funcptr = MappedMemoryWriteByte; break;
          case SH2_ACCESS_TYPE_W: funcptr = MappedMemoryWriteWord; break;
          case SH2_ACCESS_TYPE_L: funcptr = MappedMemoryWriteLong; break;
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }
        DEFINE_REG(fallback);
        MOVEA(fallback, funcptr);
        CALL_NORET(real_address, src, fallback);
        /* No need to check translations if not direct-access */

    } DEFINE_LABEL(label_done);

    return 1;
}

/*----------------------------------*/

/**
 * do_store:  Generate code for an SH-2 memory store operation.
 *
 * [Parameters]
 *         entry: Block being translated
 *       address: RTL register holding store address
 *           src: RTL register holding value to be stored
 *          type: Access type (SH2_ACCESS_TYPE_[BWL] only)
 *     state_reg: RTL register holding state block pointer
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_store(const JitEntry *entry, uint32_t address, uint32_t src,
                    unsigned int type, uint32_t state_reg)
{
    CREATE_LABEL(label_fallback);
    CREATE_LABEL(label_done);

    /* Log the store if appropriate */
    log_store(entry, address, src, 0, type, 0);

    /* Look up the direct access pointer */
    DEFINE_REG(page);
    SRLI(page, address, 19);
    DEFINE_REG(table_offset);
    SLLI(table_offset, page, LOG2_SIZEOF_PTR);
    DEFINE_REG(dp_base);
    MOVEA(dp_base, ADDR_HI(direct_pages));
    DEFINE_REG(dp_ptr);
    ADD(dp_ptr, dp_base, table_offset);
    DEFINE_REG(page_ptr_temp);
    LOAD_PTR(page_ptr_temp, dp_ptr, ADDR_LO(direct_pages));
    /* Also check direct_jit_pages[] to make sure it's writable */
    DEFINE_REG(djp_base);
    MOVEA(djp_base, ADDR_HI(direct_jit_pages));
    DEFINE_REG(djp_ptr);
    ADD(djp_ptr, djp_base, table_offset);
    DEFINE_REG(djppage_ptr);
    LOAD_PTR(djppage_ptr, djp_ptr, ADDR_LO(direct_jit_pages));
    /* Select to zero if the direct_jit_pages[] entry is zero */
    DEFINE_REG(page_ptr);
    SELECT(page_ptr, page_ptr_temp, djppage_ptr, djppage_ptr);

    /* Actually perform the store */
    return do_store_common(entry, address, src, 0, type, state_reg,
                           page_ptr, djppage_ptr);
}

/*----------------------------------*/

/**
 * do_store_abs:  Generate optimized code for an SH-2 memory store
 * operation to a known address.
 *
 * [Parameters]
 *         entry: Block being translated
 *       address: Store address
 *           src: RTL register holding value to be stored
 *          type: Access type (SH2_ACCESS_TYPE_[BWL] only)
 *     state_reg: RTL register holding state block pointer
 *       islocal: Nonzero if the address points to local data, else zero
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_store_abs(const JitEntry *entry, uint32_t address, uint32_t src,
                        unsigned int type, uint32_t state_reg, int islocal)
{
    if (!(optimization_flags & SH2_OPTIMIZE_LOCAL_ACCESSES)) {
        islocal = 0;  // Prevent optimization
    }

    log_store(entry, address, src, 0, type, 1);

    const uint32_t page = address >> 19;

    if ((address & 0x1FF00000) != 0 && direct_pages[page]) {

        const uintptr_t real_address =
            (uintptr_t)direct_pages[page] + address;
        DEFINE_REG(addr_reg);
        switch (type) {
          case SH2_ACCESS_TYPE_B: {
            MOVEA(addr_reg, ADDR_HI(real_address ^ 1));
            STORE_B(addr_reg, src, ADDR_LO(real_address ^ 1));
            break;
          }
          case SH2_ACCESS_TYPE_W: {
            MOVEA(addr_reg, ADDR_HI(real_address));
            STORE_H(addr_reg, src, ADDR_LO(real_address));
            break;
          }
          case SH2_ACCESS_TYPE_L: {
            MOVEA(addr_reg, ADDR_HI(real_address));
            DEFINE_REG(swapped);
            HSWAPW(swapped, src);
            STORE_W(addr_reg, swapped, ADDR_LO(real_address));
            break;
          }
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }
#ifdef JIT_DEBUG_VERBOSE
        if (islocal) {
            fprintf(stderr, "[OLA] %08X: store to local address 0x%08X\n",
                    jit_PC, address);
        }
#endif
        if (!islocal && direct_jit_pages[page]) {
            CREATE_LABEL(label_done);
            /* Look up the appropriate bit in the direct_jit_pages table */
            const uintptr_t byteaddr_val = (uintptr_t)direct_jit_pages[page]
                                           + (address >> JIT_PAGE_BITS);
            DEFINE_REG(byteaddr);
            MOVEA(byteaddr, ADDR_HI(byteaddr_val));
            DEFINE_REG(test);
            LOAD_BU(test, byteaddr, ADDR_LO(byteaddr_val));
            /* Does the JIT page contain translations? */
            GOTO_IF_Z(label_done, test); {
                /* Clear translations from the JIT page */
                DEFINE_REG(sh2_addr_reg);
                MOVEI(sh2_addr_reg, address);
                DEFINE_REG(jcw_ptr);
                MOVEA(jcw_ptr, jit_clear_write);
                CALL_NORET(state_reg, sh2_addr_reg, jcw_ptr);
            } DEFINE_LABEL(label_done);
        }

    } else {

        void *funcptr;
        switch (type) {
          case SH2_ACCESS_TYPE_B: funcptr = MappedMemoryWriteByte; break;
          case SH2_ACCESS_TYPE_W: funcptr = MappedMemoryWriteWord; break;
          case SH2_ACCESS_TYPE_L: funcptr = MappedMemoryWriteLong; break;
          default:
            DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
            return 0;
        }
        DEFINE_REG(addr_reg);
        MOVEI(addr_reg, address);
        DEFINE_REG(fallback);
        MOVEA(fallback, funcptr);
        CALL_NORET(addr_reg, src, fallback);

    }

    return 1;
}

/*----------------------------------*/

/**
 * do_store_reg:  Generate optimized code for an SH-2 memory store
 * operation through an SH-2 register.
 *
 * [Parameters]
 *         entry: Block being translated
 *        sh2reg: SH-2 register holding store address
 *           src: RTL register holding value to be stored
 *        offset: Offset to be added to address
 *          type: Access type (SH2_ACCESS_TYPE_[BWL] only)
 *        predec: Nonzero if a predecrement access, else zero
 *         state: SH-2 state block pointer
 *     state_reg: RTL register holding state block pointer
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int do_store_reg(const JitEntry *entry, unsigned int sh2reg,
                        uint32_t src, int32_t offset, unsigned int type,
                        int predec, const SH2State *state, uint32_t state_reg)
{
    /* Half of a JIT page, in bytes (used when deciding whether to perform
     * a check for overwrites of translated code) */
    const int32_t half_jit_page = (1U << JIT_PAGE_BITS) / 2;

    const int predec_size =
        !predec ? 0 :
        type==SH2_ACCESS_TYPE_L ? 4 :
        type==SH2_ACCESS_TYPE_W ? 2 : 1;
    if (predec_size) {
        ADDI_STATE_NOREG(R[sh2reg], -predec_size);
    }

#ifdef OPTIMIZE_STATE_BLOCK
    offset += state_cache[state_cache_index(offsetof(SH2State,R[sh2reg]))].offset;
#endif

    if (!pointer_status[sh2reg].known && pointer_status[sh2reg].source != 0) {
        const uint32_t address =
            MappedMemoryReadLong(pointer_status[sh2reg].source);
        pointer_status[sh2reg].known = -1;  // Only tentatively known
        pointer_status[sh2reg].data_only = 0;
        pointer_status[sh2reg].checked = 0;
        if ((address & 0xDFF00000) == 0x00200000
         || (address & 0xDE000000) == 0x06000000
        ) {
            pointer_status[sh2reg].type = 2;
            DEFINE_REG(basereg);
            MOVEA(basereg, direct_pages[address>>19]);
            pointer_status[sh2reg].rtl_basereg = basereg;
        } else if ((address & 0xDFF80000) == 0x05C00000
                || (address & 0xDFF00000) == 0x05E00000
        ) {
            pointer_status[sh2reg].type = 1;
            DEFINE_REG(basereg);
            MOVEA(basereg, byte_direct_pages[address>>19]);
            pointer_status[sh2reg].rtl_basereg = basereg;
        } else {
            pointer_status[sh2reg].rtl_basereg = 0;
        }
        pointer_status[sh2reg].rtl_ptrreg = 0;
    }

    DECLARE_REG(address);
    LOAD_STATE_ALLOC_KEEPOFS(address, R[sh2reg]);

    if (pointer_status[sh2reg].known) {

        log_store(entry, address, src, offset, type, 0);

        if (pointer_status[sh2reg].rtl_basereg) {

            DECLARE_REG(ptr);
            int32_t ptr_offset;
            if (sh2reg == 15 && stack_pointer) {
                if (offset < -0x8000 || offset > 0x7FFF) {
                    ALLOC_REG(ptr);
                    ADDI(ptr, stack_pointer, offset);
                    ptr_offset = 0;
                } else {
                    ptr_offset = offset;
                    ptr = stack_pointer;
                }
            } else {
                if (offset < -0x8000 || offset > 0x7FFF) {
                    DEFINE_REG(offset_addr);
                    ADDI(offset_addr, address, offset);
                    ptr_offset = 0;
                    ALLOC_REG(ptr);
                    ADD(ptr, pointer_status[sh2reg].rtl_basereg, offset_addr);
                } else {
                    ptr_offset = offset;
                    if (pointer_status[sh2reg].rtl_ptrreg) {
                        ptr = pointer_status[sh2reg].rtl_ptrreg;
                    } else {
                        ALLOC_REG(ptr);
                        ADD(ptr, pointer_status[sh2reg].rtl_basereg, address);
                        pointer_status[sh2reg].rtl_ptrreg = ptr;
                    }
                }
            }
            switch (type) {
              case SH2_ACCESS_TYPE_B: {
                DECLARE_REG(real_ptr);
                int32_t real_offset;
                if (sh2reg == 15 && (optimization_flags & SH2_OPTIMIZE_STACK)){
                    real_ptr = ptr;
                    real_offset = ptr_offset ^ 1;
                } else if (pointer_status[sh2reg].type == 1) {
                    real_ptr = ptr;
                    real_offset = ptr_offset;
                } else if (offset & 1) {
                    DEFINE_REG(offset_ptr);
                    ADDI(offset_ptr, ptr, ptr_offset);
                    ALLOC_REG(real_ptr);
                    XORI(real_ptr, offset_ptr, 1);
                    real_offset = 0;
                } else {
                    ALLOC_REG(real_ptr);
                    XORI(real_ptr, ptr, 1);
                    real_offset = ptr_offset;
                }
                STORE_B(real_ptr, src, real_offset);
                break;
              }
              case SH2_ACCESS_TYPE_W: {
                if (pointer_status[sh2reg].type == 1) {
                    DEFINE_REG(swapped);
                    BSWAPH(swapped, src);
                    STORE_H(ptr, swapped, ptr_offset);
                } else {
                    STORE_H(ptr, src, ptr_offset);
                }
                break;
              }
              case SH2_ACCESS_TYPE_L: {
                DEFINE_REG(swapped);
                if (pointer_status[sh2reg].type == 1) {
                    BSWAPW(swapped, src);
                } else {
                    HSWAPW(swapped, src);
                }
                STORE_W(ptr, swapped, ptr_offset);
                break;
              }
              default:
                DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
                return 0;
            }

            /* See if we need to check for overwrites of translated code */
            int need_jit_check = 1;
            if (sh2reg == 15 && (optimization_flags & SH2_OPTIMIZE_STACK)) {
                need_jit_check = 0;
            } else if (pointer_status[sh2reg].data_only) {
                need_jit_check = 0;
            } else if (pointer_status[sh2reg].type != 2) {
                need_jit_check = 0;
            } else if (pointer_status[sh2reg].checked
                    && offset - pointer_status[sh2reg].check_offset > -half_jit_page
                    && offset - pointer_status[sh2reg].check_offset < half_jit_page
            ) {
                need_jit_check = 0;
            }

            if (need_jit_check) {
                /* Obtain the address for checking.  If the store offset is
                 * small, use an offset of zero for checking to save an
                 * instruction */
                DECLARE_REG(jit_address);
                int32_t jit_offset;
                if (offset > -half_jit_page && offset < half_jit_page) {
                    jit_address = address;
                    jit_offset = 0;
                } else {
                    ALLOC_REG(jit_address);
                    ADDI(jit_address, address, offset);
                    jit_offset = offset;
                }
                /* Load the JIT bitmap address */
                CREATE_LABEL(label_done);
                DEFINE_REG(djppage_ptr);
                int djppage_ofs;
                if (pointer_status[sh2reg].known > 0) {
                    MOVEA(djppage_ptr,
                          ADDR_HI(pointer_status[sh2reg].djp_base));
                    djppage_ofs = ADDR_LO(pointer_status[sh2reg].djp_base);
                } else {
                    DEFINE_REG(temp1);
                    SRLI(temp1, jit_address, 19);
                    DEFINE_REG(djp_offset);
                    SLLI(djp_offset, temp1, LOG2_SIZEOF_PTR);
                    DEFINE_REG(djp_base);
                    MOVEA(djp_base, ADDR_HI(direct_jit_pages));
                    DEFINE_REG(djp_ptr);
                    ADD(djp_ptr, djp_base, djp_offset);
                    LOAD_PTR(djppage_ptr, djp_ptr, ADDR_LO(direct_jit_pages));
                    GOTO_IF_Z(label_done, djppage_ptr);
                    djppage_ofs = 0;
                }
                /* Look up the appropriate bit in the table */
                DEFINE_REG(byteofs);
                SRLI(byteofs, address, JIT_PAGE_BITS);
                DEFINE_REG(byteaddr);
                ADD(byteaddr, djppage_ptr, byteofs);
                DEFINE_REG(test);
                LOAD_BU(test, byteaddr, djppage_ofs);
                /* Does the JIT page contain translations? */
                GOTO_IF_Z(label_done, test); {
                    /* Clear translations from the JIT page */
                    DEFINE_REG(jcw_ptr);
                    MOVEA(jcw_ptr, jit_clear_write);
                    CALL_NORET(state_reg, address, jcw_ptr);
                } DEFINE_LABEL(label_done);
                /* Mark this register checked for translation overwrites */
                pointer_status[sh2reg].checked = 1;
                pointer_status[sh2reg].check_offset = jit_offset;
            }  // if not an optimized stack access

        } else {  // !pointer_status[sh2reg].rtl_basereg

            DECLARE_REG(offset_addr);
            if (offset) {
                ALLOC_REG(offset_addr);
                ADDI(offset_addr, address, offset);
            } else {
                offset_addr = address;
            }
            void *funcptr;
            switch (type) {
              case SH2_ACCESS_TYPE_B: funcptr = MappedMemoryWriteByte; break;
              case SH2_ACCESS_TYPE_W: funcptr = MappedMemoryWriteWord; break;
              case SH2_ACCESS_TYPE_L: funcptr = MappedMemoryWriteLong; break;
              default:
                DMSG("0x%08X: BUG: invalid access type %u", jit_PC, type);
                return 0;
            }
            DEFINE_REG(fallback);
            MOVEA(fallback, funcptr);
            CALL_NORET(offset_addr, src, fallback);

        }  // if (pointer_status[sh2reg].rtl_basereg)

    } else {  // !pointer_status[sh2reg].known

        if (optimization_flags & SH2_OPTIMIZE_POINTERS) {
            log_store(entry, address, src, offset, type, 0);
            DECLARE_REG(djppage_ptr);
            if (!pointer_status[sh2reg].rtl_basereg) {
                DEFINE_REG(page);
                SRLI(page, address, 19);
                DEFINE_REG(dp_base);
                MOVEA(dp_base, ADDR_HI(direct_pages));
                DEFINE_REG(table_offset);
                SLLI(table_offset, page, LOG2_SIZEOF_PTR);
                DEFINE_REG(dp_ptr);
                ADD(dp_ptr, dp_base, table_offset);
                DEFINE_REG(page_ptr);
                LOAD_PTR(page_ptr, dp_ptr, ADDR_LO(direct_pages));
                /* Also check direct_jit_pages[] to make sure it's writable */
                DEFINE_REG(djp_base);
                MOVEA(djp_base, ADDR_HI(direct_jit_pages));
                DEFINE_REG(djp_ptr);
                if (offset > -half_jit_page && offset < half_jit_page) {
                    /* Check at an offset of zero for efficiency */
                    ADD(djp_ptr, djp_base, table_offset);
                    pointer_status[sh2reg].check_offset = 0;
                } else {
                    DEFINE_REG(djp_address);
                    ADDI(djp_address, address, offset);
                    DEFINE_REG(djp_page);
                    SRLI(djp_page, djp_address, 19);
                    DEFINE_REG(djp_table_offset);
                    SLLI(djp_table_offset, djp_page, LOG2_SIZEOF_PTR);
                    ADD(djp_ptr, djp_base, djp_table_offset);
                    pointer_status[sh2reg].check_offset = offset;
                }
                ALLOC_REG(djppage_ptr);
                LOAD_PTR(djppage_ptr, djp_ptr, ADDR_LO(direct_jit_pages));
                pointer_status[sh2reg].checked = 1;
                /* Select to zero if the direct_jit_pages[] entry is zero */
                DEFINE_REG(basereg);
                SELECT(basereg, page_ptr, djppage_ptr, djppage_ptr);
                pointer_status[sh2reg].rtl_basereg = basereg;
                pointer_status[sh2reg].rtl_ptrreg = 0;
                pointer_status[sh2reg].checked_djp = 1;
            } else if (!pointer_status[sh2reg].checked_djp) {
                /* This register was first used in a load, so we don't yet
                 * know whether it's writable; check the direct_jit_pages[]
                 * entry as above */
                DEFINE_REG(page);
                SRLI(page, address, 19);
                DEFINE_REG(dp_base);
                MOVEA(dp_base, ADDR_HI(direct_pages));
                DEFINE_REG(table_offset);
                SLLI(table_offset, page, LOG2_SIZEOF_PTR);
                DECLARE_REG(basereg);
                basereg = pointer_status[sh2reg].rtl_basereg;
                DEFINE_REG(djp_base);
                MOVEA(djp_base, ADDR_HI(direct_jit_pages));
                DEFINE_REG(djp_ptr);
                if (offset > -half_jit_page && offset < half_jit_page) {
                    ADD(djp_ptr, djp_base, table_offset);
                    pointer_status[sh2reg].check_offset = 0;
                } else {
                    DEFINE_REG(djp_address);
                    ADDI(djp_address, address, offset);
                    DEFINE_REG(djp_page);
                    SRLI(djp_page, djp_address, 19);
                    DEFINE_REG(djp_table_offset);
                    SLLI(djp_table_offset, djp_page, LOG2_SIZEOF_PTR);
                    ADD(djp_ptr, djp_base, djp_table_offset);
                    pointer_status[sh2reg].check_offset = offset;
                }
                ALLOC_REG(djppage_ptr);
                LOAD_PTR(djppage_ptr, djp_ptr, ADDR_LO(direct_jit_pages));
                pointer_status[sh2reg].checked = 1;
                SELECT(basereg, basereg, djppage_ptr, djppage_ptr);
                pointer_status[sh2reg].checked_djp = 1;
            } else if (!pointer_status[sh2reg].checked
                    || offset - pointer_status[sh2reg].check_offset <= -half_jit_page
                    || offset - pointer_status[sh2reg].check_offset >= half_jit_page
            ) {
                /* Pointer has not been checked or the last check was at
                 * least half a JIT page away, so check for JIT overwrites */
                DEFINE_REG(djp_base);
                MOVEA(djp_base, ADDR_HI(direct_jit_pages));
                DEFINE_REG(djp_address);
                ADDI(djp_address, address, offset);
                DEFINE_REG(djp_page);
                SRLI(djp_page, djp_address, 19);
                DEFINE_REG(djp_table_offset);
                SLLI(djp_table_offset, djp_page, LOG2_SIZEOF_PTR);
                DEFINE_REG(djp_ptr);
                ADD(djp_ptr, djp_base, djp_table_offset);
                ALLOC_REG(djppage_ptr);
                LOAD_PTR(djppage_ptr, djp_ptr, ADDR_LO(direct_jit_pages));
                pointer_status[sh2reg].checked = 1;
                pointer_status[sh2reg].check_offset = offset;
            } else {
                /* We recently checked for JIT overwrites here, so no need
                 * to do so again */
                djppage_ptr = 0;
            }
            ASSERT(do_store_common(entry, address, src, offset, type, state_reg,
                                   pointer_status[sh2reg].rtl_basereg,
                                   djppage_ptr));
        } else {
            DECLARE_REG(offset_addr);
            if (offset) {
                ALLOC_REG(offset_addr);
                ADDI(offset_addr, address, offset);
            } else {
                offset_addr = address;
            }
            ASSERT(do_store(entry, offset_addr, src, type, state_reg));
        }

    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * check_cycles:  Add RTL code to commit cycles cached in "cur_cycles",
 * check whether we've hit the cycle limit, and terminate execution if so.
 *
 * [Parameters]
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 * [Return value]
 *    Nonzero on success, zero on error
 */
#if defined(__GNUC__) && !defined(JIT_ACCURATE_ACCESS_TIMING)
__attribute__((unused))
#endif
static int check_cycles(const JitEntry *entry, uint32_t state_reg)
{
    DECLARE_REG(cycles);
    LOAD_STATE_ALLOC(cycles, cycles);
    DECLARE_REG(cycle_limit);
    LOAD_STATE_ALLOC(cycle_limit, cycle_limit);
    DEFINE_REG(test);
    SLTS(test, cycles, cycle_limit);
    CREATE_LABEL(no_interrupt);
    GOTO_IF_NZ(no_interrupt, test); {
        SAVE_STATE_CACHE();
        FLUSH_STATE_CACHE();
        RETURN();
        RESTORE_STATE_CACHE();
    } DEFINE_LABEL(no_interrupt);

    return 1;
}

/*----------------------------------*/

/**
 * check_cycles_and_goto:  Add RTL code to commit cycles and check the
 * cycle count, jumping to the specified label if the cycle count has not
 * reached the limit and terminating execution otherwise.
 *
 * [Parameters]
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *         label: Target label for jump
 * [Return value]
 *    Nonzero on success, zero on error
 */
static int check_cycles_and_goto(const JitEntry *entry, uint32_t state_reg,
                                 uint32_t label)
{
    DECLARE_REG(cycles);
    LOAD_STATE_ALLOC(cycles, cycles);
    DECLARE_REG(cycle_limit);
    LOAD_STATE_ALLOC(cycle_limit, cycle_limit);
    DEFINE_REG(test);
    SLTS(test, cycles, cycle_limit);
    GOTO_IF_NZ(label, test);
    FLUSH_STATE_CACHE();
    RETURN();

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * branch_static:  Generate code to branch to a static address.  The
 * address is assumed to be stored in state->branch_target.
 * Implements the JUMP_STATIC() macro used by the decoding core.
 *
 * [Parameters]
 *         state: Processor state block
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 * [Return value]
 *     Nonzero on success, zero on failure
 */
static int branch_static(SH2State *state, JitEntry *entry, uint32_t state_reg)
{
    if ((state->branch_target < jit_PC
         && state->branch_target != entry->sh2_start)
     || state->branch_target > entry->sh2_end
    ) {
        FLUSH_STATE_CACHE();
        RETURN();
        return 1;
    }

    uint32_t target_label = btcache_lookup(state->branch_target);
    if (!target_label) {
#ifdef JIT_DEBUG_VERBOSE
        DMSG("Unresolved branch from %08X to %08X", jit_PC - 2,
             (int)state->branch_target);
#endif
        target_label = rtl_alloc_label(entry->rtl);
        if (UNLIKELY(!target_label)) {
#ifdef JIT_DEBUG
            DMSG("Failed to allocate label for unresolved branch at 0x%08X",
                 jit_PC);
#endif
            FLUSH_STATE_CACHE();
            RETURN();
            return 1;
        }
        if (!record_unresolved_branch(entry, state->branch_target,
                                      target_label)) {
            return 0;
        }
        unsigned int index = (state->branch_target - entry->sh2_start) / 2;
        is_branch_target[index/32] |= 1 << (index % 32);
    }

    if (state->branch_target < jit_PC) {
        check_cycles_and_goto(entry, state_reg, target_label);
    } else {
        APPEND(GOTO, 0, 0, 0, target_label);
    }
    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * opcode_init:  Perform pre-decode processing for an instruction.
 * Implements the OPCODE_INIT() macro used by the decoding core.
 *
 * [Parameters]
 *         state: Processor state block
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *        opcode: SH-2 opcode of instruction being decoded
 *     recursing: Nonzero if this is a recursive decode, else zero
 * [Return value]
 *     Nonzero on success, zero on failure
 */
static int opcode_init(SH2State *state, JitEntry *entry, uint32_t state_reg,
                       unsigned int opcode, int recursing)
{
    unsigned int index;

    /*
     * (1) If translation tracing is enabled, print a trace line for the
     *     instruction.
     */
#ifdef JIT_DEBUG_TRACE
    char tracebuf[100];
    SH2Disasm(jit_PC, opcode, 0, tracebuf);
    fprintf(stderr, "%08X: %04X  %s\n", jit_PC, opcode, tracebuf+12);
#endif

    /*
     * (2) In JIT_ACCURATE_ACCESS_TIMING mode, determine whether the
     *     instruction performs a load or store that might not be to
     *     ROM/RAM, and check the cycle count if so.  (This is done before
     *     the branch target for optimization reasons, since a cycle check
     *     or unconditional interrupt/termination is always performed on a
     *     branch.)
     */
#if defined(JIT_ACCURATE_ACCESS_TIMING)
    const unsigned int n = opcode>>8 & 0xF;
    const unsigned int m = opcode>>4 & 0xF;
    int is_pointer;
    uint32_t knownbits = 0, value = 0;
    if ((opcode & 0xF00E) == 0x0004 || (opcode & 0xF00F) == 0x0006) {
        is_pointer = 0;
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(0)] & reg_knownbits[REG_R(n)];
        value      = reg_value    [REG_R(0)] & reg_value    [REG_R(n)];
# endif
    } else if (opcode == 0x002B || (opcode & 0xFF00) == 0xC300) {
        is_pointer = PTR_CHECK(15);
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(15)];
        value      = reg_value    [REG_R(15)];
# endif
    } else if ((opcode & 0xF00E) == 0x000C || (opcode & 0xF00F) == 0x000E) {
        is_pointer = 0;
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(0)] & reg_knownbits[REG_R(m)];
        value      = reg_value    [REG_R(0)] & reg_value    [REG_R(m)];
# endif
    } else if ((opcode & 0xB00F) == 0x000F) {
        is_pointer = PTR_CHECK(n) && PTR_CHECK(m);
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(n)] & reg_knownbits[REG_R(m)];
        value      = reg_value    [REG_R(n)] & reg_value    [REG_R(m)];
# endif
    } else if ((opcode & 0xF000) == 0x1000
            || (opcode & 0xF00A) == 0x2000 || (opcode & 0xF00B) == 0x2002
            || (opcode & 0xF00A) == 0x4002 || (opcode & 0xF0FF) == 0x401B) {
        is_pointer = PTR_CHECK(n);
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(n)];
        value      = reg_value    [REG_R(n)];
# endif
    } else if ((opcode & 0xF000) == 0x5000
            || (opcode & 0xF00A) == 0x6000 || (opcode & 0xF00B) == 0x6002
            || (opcode & 0xFA00) == 0x8000) {
        is_pointer = PTR_CHECK(m);
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_R(m)];
        value      = reg_value    [REG_R(m)];
# endif
    } else if ((opcode & 0xB000) == 0x9000) {
        is_pointer = 0;
        knownbits  = 0xFFFFFFFF;
        value      = jit_PC;
    } else if ((opcode & 0xFA00) == 0xC000 || (opcode & 0xFB00) == 0xC200) {
        is_pointer = 0;
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_GBR];
        value      = reg_value    [REG_GBR];
# endif
    } else if ((opcode & 0xFC00) == 0xCC00) {
        is_pointer = 0;
# ifdef OPTIMIZE_KNOWN_VALUES
        knownbits  = reg_knownbits[REG_GBR] & reg_knownbits[REG_R(0)];
        value      = reg_value    [REG_GBR] & reg_value    [REG_R(0)];
# endif
    } else {
        is_pointer = 1;  // i.e. no need to check
    }
    if (!is_pointer
     && ((knownbits & 0xFFF00000) != 0xFFF00000
      || ((value & 0xDFF00000) != 0x00200000  // Don't bother with $Axxxxxxx
       && (value & 0xDFF00000) != 0x06000000))
    ) {
        /* Exceptions are not accepted when processing a delay slot (SH7604
         * manual page 75, section 4.6.1).  For BT/S and BF/S, we have to
         * check whether we would actually branch or not to know whether
         * the following instruction is considered to be in a delay slot. */
        if (!state->delay) {
            check_cycles(entry, state_reg);
        } else {  // It's a delayed-branch instruction
            if (state->branch_type == SH2BRTYPE_BT_S
             || state->branch_type == SH2BRTYPE_BF_S
            ) {
                CREATE_LABEL(no_branch);
                if (state->branch_type == SH2BRTYPE_BT_S) {
                    GOTO_IF_NZ(no_branch, state->branch_cond_reg);
                } else {
                    GOTO_IF_Z(no_branch, state->branch_cond_reg);
                }
                check_cycles(entry, state_reg);
                DEFINE_LABEL(no_branch);
            }
        }
    }
#endif  // JIT_ACCURATE_ACCESS_TIMING

    /*
     * (4) If this is not a recursive decode and the current address is the
     *     target of a static branch, reset all known register values and
     *     pointer base registers, flush the state block register cache,
     *     and commit pending cycles; then add a label to serve as the
     *     branch target.
     */
    int cached_label = 0;
    index = (jit_PC - initial_PC) / 2;
    if (!recursing && (is_branch_target[index/32] & (1 << (index % 32)))) {
        CLEAR_MAC_IS_ZERO();
#ifdef JIT_DEBUG
        if (UNLIKELY(CACHED_SHIFT_COUNT() != 0)) {
            DMSG("0x%08X: WARNING: Shift count (%u) cached over a branch"
                 " target!", jit_PC, CACHED_SHIFT_COUNT());
        }
#endif
        REG_RESETKNOWN();
        unsigned int reg;
        for (reg = 0; reg < lenof(pointer_status); reg++) {
            /* Known registers were checked at the start of the block, so
             * there's no risk of them being undefined due to jumping over
             * the code that loads them.  However, if a register has been
             * copied, the copy may have occurred on a different code
             * branch, so mark it as unknown in that case. */
            if (pointer_status[reg].known <= 0) {
                pointer_status[reg].known = 0;
                pointer_status[reg].rtl_basereg = 0;
                pointer_status[reg].source = 0;
            }
            /* Always reset this to avoid having too many long-lived
             * registers (see setup_pointers()); we'll regenerate it as
             * needed. */
            pointer_status[reg].rtl_ptrreg = 0;
        }
        pointer_local = 0;  // Known registers never appear here
        ASSERT(writeback_state_cache(entry, state_reg, 0));
        clear_state_cache(0);
        btcache[btcache_index].sh2_address = jit_PC;
        btcache[btcache_index].rtl_label = rtl_alloc_label(entry->rtl);
        if (UNLIKELY(!btcache[btcache_index].rtl_label)) {
            DMSG("Failed to generate label for branch target at 0x%08X",
                 jit_PC);
            return 0;
        } else {
            DEFINE_LABEL(btcache[btcache_index].rtl_label);
            btcache_index++;
            if (UNLIKELY(btcache_index >= lenof(btcache))) {
                btcache_index = 0;
            }
            cached_label = 1;
        }
    }

    /*
     * (5) If this is not a recursive decode and there are any pending
     *     static branches to the current address, resolve them by defining
     *     the RTL label they branch to.  (However, do _not_ define a label
     *     if this instruction is in a delay slot, because the RTL code
     *     implementing the branch will be appended after this instruction,
     *     which would cause incorrect behavior for other code branching to
     *     this instruction.)
     */
    if (!recursing && !state->delay) {
        for (index = 0; index < lenof(unres_branches); index++) {
            if (unres_branches[index].sh2_target == jit_PC) {
                const uint32_t label = unres_branches[index].target_label;
                APPEND(LABEL, 0, 0, 0, label);
                unres_branches[index].sh2_target = 0;
                if (!cached_label) {
                    /* Probably won't need it, but cache anyway just in case */
                    btcache[btcache_index].sh2_address = jit_PC;
                    btcache[btcache_index].rtl_label = label;
                    btcache_index++;
                    if (UNLIKELY(btcache_index >= lenof(btcache))) {
                        btcache_index = 0;
                    }
                    cached_label = 1;
                }
            }
        }
    }

    /*
     * (6) If this is not a recursive decode and this instruction is hinted
     *     as loading a data pointer, set a flag so that we will generate
     *     RTL to load the base pointer after the instruction completes;
     *     otherwise, clear the flag.  (Note that the hint flag will never
     *     be set if pointer optimization is disabled, so we don't need to
     *     check again here.)
     */
    index = (jit_PC - initial_PC) / 2;
    if (!recursing && (is_data_pointer_load[index/32] & (1 << (index % 32)))) {
        state->make_Rn_data_pointer = 1;
    } else {
        state->make_Rn_data_pointer = 0;
    }

    /*
     * (7) If enabled, insert a dummy instruction indicating the current
     *     SH-2 PC.  Do this after the label so the instruction isn't
     *     optimized away as part of a dead code block following a branch.
     */
#ifdef JIT_DEBUG_INSERT_PC
    APPEND(NOP, 0, jit_PC, 0, 0);
#endif

    /*
     * (8) If TRACE_STEALTH is enabled, insert coded NOPs to inform the
     *     RTL interpreter of cached values and direct it to trace the
     *     instruction.
     */
#ifdef TRACE_STEALTH
    APPEND(NOP, 0, 0x80000000 | jit_PC, 0, 0);
    if (state->pending_select && !state->delay) {
        APPEND(NOP, 0, 0x9F000000 | (!state->select_sense) << 16
                                  | state->branch_cond_reg, 0, 0);
    }
# ifdef OPTIMIZE_STATE_BLOCK
    APPEND(NOP, 0, 0x98000000 | STATE_CACHE_OFFSET(cycles), 0, 0);
    APPEND(NOP, 0, 0x90000000 | state_cache[state_cache_index(offsetof(SH2State,cycles))].rtlreg, 0, 0);
# else
    APPEND(NOP, 0, 0x98000000, 0, 0);
    APPEND(NOP, 0, 0x90000000, 0, 0);
# endif
#endif

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * opcode_done:  Perform post-decode processing for an instruction.
 * Implements the OPCODE_DONE() macro used by the decoding core.
 *
 * [Parameters]
 *         state: Processor state block
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *        opcode: SH-2 opcode of instruction being decoded
 *     recursing: Nonzero if this is a recursive decode, else zero
 * [Return value]
 *     Nonzero on success, zero on failure
 */
static int opcode_done(SH2State *state, JitEntry *entry, uint32_t state_reg,
                       unsigned int opcode, int recursing)
{
    /*
     * (1) If this instruction is hinted as loading a data pointer,
     *     generate RTL to set up a base pointer register for the SH-2
     *     register identified by the Rn field of this instruction (or R0,
     *     if the instruction is MOVA @(disp,PC),R0).
     */
    if (state->make_Rn_data_pointer) {
        const unsigned int n =
            ((opcode & 0xFF00) == 0xC700) ? 0 : opcode>>8 & 0xF;

        DECLARE_REG(Rn);
        LOAD_STATE_ALLOC_KEEPOFS(Rn, R[n]);
        DEFINE_REG(page);
        SRLI(page, Rn, 19);
        DEFINE_REG(dp_base);
        MOVEA(dp_base, ADDR_HI(direct_pages));
        DEFINE_REG(table_offset);
        SLLI(table_offset, page, LOG2_SIZEOF_PTR);
        DEFINE_REG(dp_ptr);
        ADD(dp_ptr, dp_base, table_offset);
        DEFINE_REG(page_ptr);
        LOAD_PTR(page_ptr, dp_ptr, ADDR_LO(direct_pages));
        pointer_status[n].known = -1;
        pointer_status[n].type = 2;
        pointer_status[n].data_only = 1;
        pointer_status[n].rtl_basereg = page_ptr;
    }

    /*
     * (2) If tracing, write all cached SH-2 registers back to the state
     *     block so traces show the correct values at each instruction.
     */
#ifdef TRACE
    ASSERT(writeback_state_cache(entry, state_reg, 1));
#endif

    return 1;
}

/*************************************************************************/
/***************** SH-2 -> RTL translation main routines *****************/
/*************************************************************************/

/**
 * translate_block:  Translate a block of SH-2 instructions beginning at
 * the address in entry->sh2_start into RTL code.  On success, the RTL
 * block in entry->rtl is finalized.
 *
 * [Parameters]
 *     state: SH-2 processor state
 *     entry: Block being translated
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int translate_block(SH2State *state, JitEntry *entry)
{
    const uint16_t *fetch_base =
        (const uint16_t *)fetch_pages[entry->sh2_start >> 19];
    PRECOND(fetch_base != NULL, return 0);

    unsigned int index;

    /* Clear out translation state. */

    memset(is_branch_target, 0, sizeof(is_branch_target));
    for (index = 0; index < lenof(btcache); index++) {
        btcache[index].sh2_address = 0;
    }
    btcache_index = 0;
    for (index = 0; index < lenof(unres_branches); index++) {
        unres_branches[index].sh2_target = 0;
    }
#ifdef OPTIMIZE_KNOWN_VALUES
    for (index = 0; index < lenof(reg_knownbits); index++) {
        reg_knownbits[index] = 0;
        reg_value[index] = 0;
    }
#endif
    ignore_optimization_hints = 0;
    is_constant_entry_reg = 0;
    memset(pointer_status, 0, sizeof(pointer_status));
    stack_pointer = 0;
    pointer_local = 0;
    is_data_pointer_reg = 0;
    memset(is_data_pointer_load, 0, sizeof(is_data_pointer_load));
#ifdef OPTIMIZE_STATE_BLOCK
    clear_state_cache(1);  // Should always be clear here, but just in case
    stored_PC = entry->sh2_start;  // So loops can skip the store if optimized
#endif
    state->branch_type = SH2BRTYPE_NONE;
    state->branch_target = 0;
    state->branch_target_reg = 0;
    state->branch_fold_native = 0;
    state->branch_cond_reg = 0;
    state->branch_cycles = 0;
    state->branch_targets_rts = 0;
    state->loop_to_jsr = 0;
    state->folding_subroutine = 0;
    state->pending_select = 0;
    state->select_sense = 0;
    state->just_branched = 0;
    state->need_interrupt_check = 0;
    state->mac_is_zero = 0;
    state->cached_shift_count = 0;
    state->varshift_target_PC = 0;
    state->varshift_type = 0;
    state->varshift_Rcount = 0;
    state->varshift_max = 0;
    state->varshift_Rshift = 0;
    state->division_target_PC = 0;
    state->div_data.Rquo = 0;
    state->div_data.Rrem = 0;
    state->div_data.quo = 0;
    state->div_data.rem = 0;
    state->div_data.SR = 0;

    /* If a manual optimization callback has been provided, check for a
     * match at this address, and use the optimized translation if found.
     * (This must be done after clearing state, because the callback may
     * set optimization hints and return zero.) */

    if (manual_optimization_callback) {
        const uint16_t *fetch =
            (const uint16_t *)((uintptr_t)fetch_base + entry->sh2_start);
        SH2NativeFunctionPointer hand_tuned_function;
        const unsigned int hand_tuned_len =
            (*manual_optimization_callback)(state, entry->sh2_start,
                                            fetch, &hand_tuned_function, 0);
        if (hand_tuned_len > 0) {
#ifdef JIT_DEBUG_VERBOSE
            DMSG("Using hand-tuned code for 0x%08X + %u insns",
                 entry->sh2_start, hand_tuned_len);
#endif
            DEFINE_REG(state_reg);
            APPEND(LOAD_PARAM, state_reg, 0, 0, 0);
            DEFINE_REG(funcptr_reg);
            MOVEA(funcptr_reg, hand_tuned_function);
            CALL_NORET(state_reg, 0, funcptr_reg);
            RETURN();
            ASSERT(rtl_finalize_block(entry->rtl));
            entry->sh2_end = entry->sh2_start + hand_tuned_len*2 - 1;
            return 1;
        }
    }

    /* If any registers were marked as constant on entry, load their
     * values into the known-constant array. */

#ifdef OPTIMIZE_KNOWN_VALUES
    if (is_constant_entry_reg) {
        for (index = 0; index < 16; index++) {
            if (is_constant_entry_reg & (1<<index)) {
                reg_knownbits[index] = 0xFFFFFFFF;
                reg_value[index] = state->R[index];
            }
        }
    }
#endif

    /* Check whether saturation code can potentially be optimized out of
     * MAC instructions. */

    can_optimize_mac_nosat = (optimization_flags & SH2_OPTIMIZE_MAC_NOSAT)
                          && !(state->SR & SR_S);

    /* Mark which registers are currently valid pointers.  Note that we
     * currently can't detect ROM pointers, because the pointer analysis
     * logic would treat "offset + unknown pointer" as "ROM pointer +
     * unknown offset". */

    pointer_regs = 0;
    if (optimization_flags & SH2_OPTIMIZE_POINTERS) {
        for (index = 0; index < 16; index++) {
            if ((state->R[index] & 0xDFF00000) == 0x00200000
             || (state->R[index] & 0xDE000000) == 0x06000000
             || (state->R[index] & 0xDFF80000) == 0x05C00000
             || (state->R[index] & 0xDFF00000) == 0x05E00000
            ) {
                pointer_regs |= 1 << index;
            }
        }
    }

    /* Scan through the SH-2 code to find the end of the block and
     * determine what parts of it are translatable code, and to perform
     * pre-translation checks for various optimizations. */

    unsigned int block_len = scan_block(state, entry, entry->sh2_start);
    if (!block_len) {
        DMSG("Failed to find any translatable instructions at 0x%08X",
             entry->sh2_start);
        return 0;
    }
    entry->sh2_end = entry->sh2_start + block_len*2 - 1;

    /* Preload the state block pointer (first function argument) into an
     * RTL register, and mark it as a unique pointer. */

    DEFINE_REG(state_reg);
    APPEND(LOAD_PARAM, state_reg, 0, 0, 0);
    ASSERT(rtl_register_set_unique_pointer(entry->rtl, state_reg));

    /* If we have an optimizable loop, prepare RTL registers for all SH-2
     * registers live in the loop as well as the cycle count and limit, up
     * to the maximum configured. */

#ifdef OPTIMIZE_LOOP_REGISTERS
    if (can_optimize_loop) {
        loop_live_registers |=
              1<<state_cache_index(offsetof(SH2State,cycles))
            | 1<<state_cache_index(offsetof(SH2State,cycle_limit));
        loop_load_registers |=
              1<<state_cache_index(offsetof(SH2State,cycles))
            | 1<<state_cache_index(offsetof(SH2State,cycle_limit));
        loop_changed_registers |=
              1<<state_cache_index(offsetof(SH2State,cycles));
# ifdef JIT_DEBUG_VERBOSE
        DMSG("Optimizing loop at %08X: live=%08X load=%08X changed=%08X"
             " invptr=%08X", entry->sh2_start, loop_live_registers,
             loop_load_registers, loop_changed_registers,
             loop_invariant_pointers);
# endif
        unsigned int regs_fixed = 0;
        for (index = 0; index < lenof(state_cache); index++) {
            if (loop_live_registers & 1<<index) {
                if (regs_fixed >= OPTIMIZE_LOOP_REGISTERS_MAX_REGS) {
# ifdef JIT_DEBUG_VERBOSE
                    DMSG("Out of fixed registers (%u), skipping some",
                         regs_fixed);
# endif
                    break;
                }
                ALLOC_REG(state_cache[index].rtlreg);
                state_cache[index].offset = 0;
                state_cache[index].fixed = 1;
                state_cache[index].flush =
                    (loop_changed_registers & 1<<index) ? 1 : 0;
#ifndef TRACE  // Need to preload all regs since we flush them at every insn
                if (loop_load_registers & 1<<index) {
#endif
                    LOAD_W(state_cache[index].rtlreg, state_reg,
                           state_cache_field(index));
#ifndef TRACE
                }
#endif
                regs_fixed++;
            }
        }
    }
#endif

    /* Prepare a potential label for use by optimizations that need to
     * invalidate and retranslate the block if preconditions fail. */

    unsigned int invalidate_label = 0;  // 0 = not used

    /* Check whether (and how often) this block has been recently purged
     * due to precondition failure; if the purge frequency exceeds a
     * certain threshold, skip all optimizations that depend on
     * preconditions so we don't retranslate the block over and over. */

    const int skip_preconditions = jit_check_purged(entry->sh2_start);

    /* If the block contains MACs and the S flag is known not to change
     * from block entry to the last MAC, add a check to ensure S is still
     * clear at runtime and retranslate the block if not. */
    // FIXME:  We assume that in a loop, if S is left clear through the
    // last MAC in the loop, it won't be set before the end of the loop
    // body.  If anybody actually does that, they should be dragged out
    // into the street and shot.

    if (!skip_preconditions && can_optimize_mac_nosat && block_contains_mac) {
        if (!invalidate_label) {
            ASSERT(invalidate_label = rtl_alloc_label(entry->rtl));
        }
        DECLARE_REG(SR);
        LOAD_STATE_ALLOC(SR, SR);
        DEFINE_REG(S);
        ANDI(S, SR, SR_S);
        GOTO_IF_NZ(invalidate_label, S);
    }

    /* If we're optimizing a loop, initialize pointer status and preload
     * pointer base addresses before the block-top label.  In this case, we
     * ignore any pointers which are not loop invariants and look them up
     * at runtime instead. */
    // FIXME: I wonder if we have to load the stack pointer dynamically?
    // (e.g. if the same code is executed by both MSH2 and SSH2, and SSH2
    // uses $002xxxxx instead of $060xxxxx)

#ifdef OPTIMIZE_LOOP_REGISTERS
    if (can_optimize_loop) {
        pointer_used &= loop_invariant_pointers;
        ASSERT(setup_pointers(state, entry, state_reg, skip_preconditions,
                              &invalidate_label));
    }
#endif

    /* Add a label for the special case of branching back to the beginning
     * of the block (backward branches normally terminate execution of the
     * block).  When we're not optimizing loops, this has to be added
     * _before_ precondition checks on pointer values so changes in those
     * values are properly detected when the code loops back. */

    CREATE_LABEL(block_start_label);
    DEFINE_LABEL(block_start_label);
    btcache[btcache_index].sh2_address = entry->sh2_start;
    btcache[btcache_index].rtl_label = block_start_label;
    btcache_index++;

    /* If we're not optimizing a loop, initialize pointer status and
     * preload pointer base addresses after the block-top label. */

#ifdef OPTIMIZE_LOOP_REGISTERS
    if (!can_optimize_loop)
#endif
    ASSERT(setup_pointers(state, entry, state_reg, skip_preconditions,
                          &invalidate_label));

    /* Perform the actual translation of SH-2 instructions to RTL code. */

#ifdef JIT_DEBUG_VERBOSE
    DMSG("Starting translation at %08X", entry->sh2_start);
#endif
    jit_PC = entry->sh2_start;
    uint32_t next_code_address = jit_PC;  // For the fall-off-the-end case
    while (jit_PC <= entry->sh2_end) {
        index = (jit_PC - entry->sh2_start) / 2;
        if (word_info[index] & WORD_INFO_CODE) {
            if (UNLIKELY(!translate_insn(state, entry, state_reg, 0, 0))) {
                DMSG("Failed to translate instruction at 0x%08X",
                     entry->sh2_start + index*2);
                return 0;
            }
            next_code_address = jit_PC;
        } else {
            /* It's not code, so just skip the word */
            jit_PC += 2;
        }
    }
#ifdef JIT_DEBUG_VERBOSE
    DMSG("Translation ended at %08X", jit_PC-2);
#endif

    /* If static branch prediction is enabled, count the number of static
     * branches to be predicted and allocate memory for the prediction
     * data.  The memory used by this data will be included in the total
     * data size of the translated block.  Otherwise, just define a flag
     * indicating whether we need to append a RETURN to terminate the
     * block. */

#ifdef JIT_BRANCH_PREDICT_STATIC
    entry->num_static_branches = state->just_branched ? 0 : 1;
    for (index = 0; index < lenof(unres_branches); index++) {
        if (unres_branches[index].sh2_target != 0) {
            entry->num_static_branches++;
        }
    }
    if (entry->num_static_branches > 0) {
        const uint32_t static_predict_size =
            entry->num_static_branches * sizeof(BranchTargetInfo);
        entry->static_predict = calloc(static_predict_size, 1);
        if (UNLIKELY(!entry->static_predict)) {
            DMSG("No memory for %u static branch predictions",
                 entry->num_static_branches);
            return 0;
        }
        jit_total_data += static_predict_size;
    }
    int branch_num = 0;
#else  // !JIT_BRANCH_PREDICT_STATIC
    int need_return = 0;
#endif

    /* Flush the state block cache and terminate the code if it can fall
     * off the end of the block. */

    if (!state->just_branched) {
        ASSERT(writeback_state_cache(entry, state_reg, 1));
        clear_state_cache(0);
#ifdef JIT_BRANCH_PREDICT_STATIC
        ASSERT(add_static_branch_terminator(
                   entry, state_reg, next_code_address, branch_num++));
#else
        need_return = 1;
#endif
    }

    /* Add fallback termination code for unresolved static branches. */

    for (index = 0; index < lenof(unres_branches); index++) {
        if (unres_branches[index].sh2_target != 0) {
#ifdef JIT_DEBUG_VERBOSE
            DMSG("FAILED to resolve branch to %08X",
                 unres_branches[index].sh2_target);
#endif
            DEFINE_LABEL(unres_branches[index].target_label);
#ifdef JIT_BRANCH_PREDICT_STATIC
            ASSERT(add_static_branch_terminator(
                       entry, state_reg, unres_branches[index].sh2_target,
                       branch_num++));
#else
            need_return = 1;
#endif
        }
    }

    /* If static branch prediction is disabled, add a RETURN if needed for
     * falling off the end of the block or terminating unresolved static
     * branches. */

#ifndef JIT_BRANCH_PREDICT_STATIC
    if (need_return) {
        RETURN();
    }
#endif

    /* If necessary, add fallback code to invalidate the block if a
     * precondition check fails. */

    if (invalidate_label) {
        DEFINE_LABEL(invalidate_label);
        DEFINE_REG(imm_sh2_start);
        MOVEI(imm_sh2_start, entry->sh2_start);
        DEFINE_REG(addr_mark_purged);
        MOVEA(addr_mark_purged, jit_mark_purged);
        CALL_NORET(imm_sh2_start, 0, addr_mark_purged);
        DEFINE_REG(imm_1);
        MOVEI(imm_1, 1);
        DEFINE_REG(addr_must_clear);
        MOVEA(addr_must_clear, &entry->must_clear);
        STORE_B(addr_must_clear, imm_1, 0);
        RETURN();
    }

    /* Close out the translated block. */

    if (UNLIKELY(!rtl_finalize_block(entry->rtl))) {
        DMSG("Failed to finalize block at 0x%08X", entry->sh2_start);
        return 0;
    }
    if (JIT_OPTIMIZE_FLAGS) {
        if (UNLIKELY(!rtl_optimize_block(entry->rtl, JIT_OPTIMIZE_FLAGS))) {
#ifdef JIT_DEBUG
            DMSG("Failed to optimize block at 0x%08X", entry->sh2_start);
#endif
            /* Block is still usable, so keep going */
        }
    }

    /* Clear everything from the cache, including any fixed registers. */

    clear_state_cache(1);

    /* All done, return success. */

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * setup_pointers:  Initialize the pointer_status[] array and load any
 * necessary pointer base registers.  Helper function for translate_block().
 *
 * [Parameters]
 *                    state: SH-2 processor state
 *                    entry: Block being translated
 *       skip_preconditions: Nonzero if preconditions should be skipped
 *     invalidate_label_ptr: Pointer to variable holding label index to
 *                              jump to when the block should be invalidated
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int setup_pointers(SH2State * const state, JitEntry * const entry,
                          const unsigned int state_reg,
                          const int skip_preconditions,
                          unsigned int * const invalidate_label_ptr)
{
    unsigned int index;

    for (index = 0; index < lenof(pointer_status); index++) {
        if ((pointer_used & 1<<index)
            && (((optimization_flags & SH2_OPTIMIZE_STACK) && index == 15)
                || ((optimization_flags & SH2_OPTIMIZE_POINTERS)
                    && (!skip_preconditions
                        || (is_data_pointer_reg & 1<<index))))
        ) {
            pointer_status[index].known = 1;
            pointer_status[index].data_only =
                (is_data_pointer_reg & 1<<index) != 0;
            pointer_status[index].checked = 0;
            if ((pointer_regs | is_data_pointer_reg) & 1<<index) {
                /* It's a directly accessible address; check at runtime to
                 * make sure the pointer is still valid (unless it's the
                 * stack pointer and we're optimizing, or the register has
                 * been hinted as safe to optimize), then assign a base
                 * register. */
                const uint32_t reg_address = state->R[index];
                const unsigned int addr_shift =
                    ((reg_address & 0x0F000000) == 0x05000000) ? 19 : 20;
                if (!(index == 15 && (optimization_flags & SH2_OPTIMIZE_STACK))
                 && !(is_data_pointer_reg & 1<<index)
                ) {
                    if (!*invalidate_label_ptr) {
                        ASSERT(*invalidate_label_ptr =
                                   rtl_alloc_label(entry->rtl));
                    }
                    DECLARE_REG(regval);
                    LOAD_STATE_ALLOC(regval, R[index]);
                    DEFINE_REG(highbits);
                    SRLI(highbits, regval, addr_shift);
                    DEFINE_REG(test);
                    XORI(test, highbits, reg_address >> addr_shift);
                    GOTO_IF_NZ(*invalidate_label_ptr, test);
                }
                pointer_status[index].type =
                    ((reg_address & 0x0F000000) == 0x05000000) ? 1 : 2;
                DEFINE_REG(basereg);
                if (pointer_status[index].type == 1) {
                    MOVEA(basereg, byte_direct_pages[reg_address>>19]);
                } else {
                    MOVEA(basereg, direct_pages[reg_address>>19]);
                }
                pointer_status[index].rtl_basereg = basereg;
                /* We don't immediately load a direct pointer in order to
                 * minimize the number of long-lived registers (one base
                 * register instead of lots of direct pointer registers);
                 * the direct pointer will be created as needed and
                 * discarded at branch points. */
                pointer_status[index].rtl_ptrreg = 0;
                pointer_status[index].djp_base =
                    direct_jit_pages[reg_address>>19];
                if (index == 15 && (optimization_flags & SH2_OPTIMIZE_STACK)) {
                    DECLARE_REG(regval);
                    LOAD_STATE_ALLOC(regval, R[index]);
                    DEFINE_REG(ptrreg);
                    ADD(ptrreg, basereg, regval);
                    stack_pointer = ptrreg;
                }
            } else {
                /* Used as a pointer, but not pointing to a directly-
                 * accessible memory region, so we'll call the fallback
                 * functions for accesses through this register.  But first
                 * make sure it really isn't pointing to a memory region
                 * that can contain code, or else we could overwrite an
                 * already-translated routine and not notice. */
                if (!*invalidate_label_ptr) {
                    ASSERT(*invalidate_label_ptr =
                                   rtl_alloc_label(entry->rtl));
                }
                DECLARE_REG(regval);
                LOAD_STATE_ALLOC(regval, R[index]);
                DEFINE_REG(page);
                SRLI(page, regval, 19);
                DEFINE_REG(page_ofs);
                SLLI(page_ofs, page, LOG2_SIZEOF_PTR);
                DEFINE_REG(djp_base);
                MOVEA(djp_base, ADDR_HI(direct_jit_pages));
                DEFINE_REG(djp_ptr);
                ADD(djp_ptr, djp_base, page_ofs);
                DEFINE_REG(test);
                LOAD_PTR(test, djp_ptr, ADDR_LO(direct_jit_pages));
                GOTO_IF_NZ(*invalidate_label_ptr, test);
                pointer_status[index].rtl_basereg = 0;
                pointer_status[index].rtl_ptrreg = 0;
            }
        } else {
            pointer_status[index].known = 0;
            pointer_status[index].rtl_basereg = 0;
            pointer_status[index].rtl_ptrreg = 0;
        }
    }

    return 1;
}

/*************************************************************************/

/**
 * scan_block:  Scan through a block of SH-2 code, finding the length of
 * the block, recording whether each word in the block is code or data, and
 * recording all addresses targeted by branches in the is_branch_target[]
 * array.
 *
 * [Parameters]
 *       state: Processor state block pointer
 *       entry: Translated block data structure
 *     address: Start of block in SH-2 address space
 * [Return value]
 *     Length of block, in instructions
 * [Side effects]
 *     - Fills in word_info[], is_branch_target[], branch_thread_target[],
 *          and branch_thread_count[]
 *     - Clears can_optimize_mac_nosat if an unoptimizable MAC is found
 *     - Sets or clears block_contains_mac appropriately
 *     - Sets pointer_used to the set of registers used as pointers
 *     - If OPTIMIZE_LOOP_REGISTERS is defined, sets can_optimize_loop,
 *          loop_live_registers, loop_load_registers, loop_changed_registers,
 *          and loop_invariant_pointers appropriately
 */
static int scan_block(SH2State *state, JitEntry *entry, uint32_t address)
{
    const uint32_t start_address = address;  // Save the block's start address

    const uint16_t *fetch_base = (const uint16_t *)fetch_pages[address >> 19];
    PRECOND(fetch_base != NULL, return 0);
    const uint16_t *fetch =
        (const uint16_t *)((uintptr_t)fetch_base + address);

    unsigned int index;

    memset(word_info, 0, sizeof(word_info));

    /* If we just encountered a branch instruction that branched to an
     * address we haven't seen before, remember the branch and target
     * addresses so we can mark the branch as fall-through if we don't
     * translate anything in between. */
    unsigned int fallthru_target = 0; // Target word_info[] index (0 = none)
    unsigned int fallthru_source = 0; // Location of branch (word_info[] index)

    /* Remember whether we've seen something write to SR (we don't clear
     * the can-optimize flag unless we see a MAC instruction _after_ a
     * write to SR, since the SR write could just be restoring it at the
     * end of the subroutine) */
    int sr_was_changed = 0;
    /* We haven't seen a MAC instruction yet */
    block_contains_mac = 0;

    /* Remember the last CLRMAC we saw, as well as relevant state
     * information at the point of that CLRMAC, so we can roll back if we
     * later find an unoptimizable MAC instruction. */
    int last_clrmac = -1;  // Word index (negative = none seen)
    uint32_t last_clrmac_pointer_used = 0;
    uint8_t last_clrmac_comn = 0;  // can_optimize_mac_nosat
    uint8_t last_clrmac_bcm = 0;   // block_contains_mac

    /* Map current register contents to registers at the block entry point,
     * so we can track pointer values through MOV instructions.  A value of
     * 31 is inserted for registers which have been overwritten with other
     * values (so we don't attempt to mark the original register indices as
     * "used"); a value of 30 indicates that the register is known to be a
     * pointer (either by being loaded with a MOVA instruction or from an
     * optimization hint) but does not derive from a specific register. */
    uint8_t pointer_map[16] = {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15};
    /* Clear the current set of registers used as pointers. */
    pointer_used = 0;

    /* Track which registers have values loaded via MOV @(disp,PC) for
     * subroutine folding.  A value of 1 (an impossible target address)
     * indicates that the register has not been so loaded.  We assume that
     * any PC-relative load subsequently used in a JSR is a constant. */
    uint32_t local_constant[16] = {1,1,1,1, 1,1,1,1, 1,1,1,1, 1,1,1,1};
    for (index = 0; index < 16; index++) {
        if (is_constant_entry_reg & (1<<index)) {
            /* We don't bother checking here whether the value is
             * actually a valid address; we assume that if a JSR
             * through this register is executed, it must be valid. */
            local_constant[index] = state->R[index];
        }
    }
    /* We also need to remember the state of each register at every branch
     * target; if a particular instruction can be reached from multiple
     * sources or does not fall through from the preceding instruction, the
     * contents of registers will need to be updated appropriately. */
    static uint32_t local_constant_per_insn[JIT_INSNS_PER_BLOCK][16];
    int is_local_branch = 0;
    unsigned int local_branch_target_index = 0;
    int local_branch_is_new = 0;

#ifdef OPTIMIZE_LOOP_REGISTERS
    /* Initialize loop optimization tracking state; also keep track of
     * whether we've seen a backward branch to the beginning of the block
     * and the loop register state at the time of that branch. */
    can_optimize_loop = 1;
    loop_live_registers = 0;
    loop_load_registers = 0;
    loop_changed_registers = 0;
    loop_invariant_pointers = 0xFFFF;
    int is_loop = 0;
    int saw_branch_target = 0;
    int at_loop_branch = 0;
    uint32_t final_live_registers = 0;
    uint32_t final_load_registers = 0;
    uint32_t final_changed_registers = 0;
    uint32_t final_invariant_pointers = 0;

    /* Remember which registers had been set before the first forward
     * branch encountered in the loop.  Any registers set by subsequent
     * instructions will be added to the preload set even if their initial
     * values are not used as source operands within the loop, so the
     * corresponding RTL registers have the proper values even if the code
     * jumps out from the middle of the loop. */
    uint32_t loop_noload_registers = 0;
    int loop_noload_registers_set = 0;
    int at_branch = 0;
#endif


    /* Find the maximum length of this block: either JIT_INSNS_PER_BLOCK or
     * the number of instruction words before the next blacklisted region,
     * whichever is smaller. */

    unsigned int max_len = JIT_INSNS_PER_BLOCK;
    for (index = 0; index < lenof(blacklist); index++) {
        if (blacklist[index].start >= start_address
         && blacklist[index].start < (start_address + max_len*2)
        ) {
            max_len = (blacklist[index].start - start_address) / 2;
        }
    }
    if (UNLIKELY(max_len < 2)) {
        DMSG("No translatable block at 0x%X (max_len=%u)",
             start_address, max_len);
        return 0;
    }

    int block_len = 0;
    int end_block = 0;
    int end_block_next = 0;  // For use with LDC ...,SR
    int delay = 0;
    int at_uncond_branch = 0;
    for (; !end_block || delay; address += 2, fetch++, block_len++) {

        /* Save whether or not this is a delay slot, then clear "delay". */

        const int now_in_delay = delay;
        delay = 0;

        /* Move the delayed block-end flag (set when an LDC ...,SR
         * instruction is detected) to end_block; if it's set, we'll end
         * the block after this instruction. */

        end_block = end_block_next;
        end_block_next = 0;

        /* Get the opcode. */

        const unsigned int opcode = *fetch;

        /* Retrieve information about the opcode. */

        const uint32_t opcode_info = get_opcode_info(opcode);

        /* Check whether the opcode is valid, and abort if not.  Don't
         * include the invalid instruction in the block; if we get there
         * and it's still invalid, we'll interpret it (but who knows, maybe
         * it'll be modified in the meantime). */

        if (!(opcode_info & SH2_OPCODE_INFO_VALID)) {
            goto abort_scan;
        }

        /* Mark this word as code. */

        word_info[block_len] = WORD_INFO_CODE;

        /* If we're not in a delay slot, check whether we just fell through
         * from a branch, and clear the fall-through variables. */

        int at_fallthru = 0;
        const uint32_t saved_fallthru_source = fallthru_source;
        const uint32_t saved_fallthru_target = fallthru_target;
        if (!now_in_delay && fallthru_target) {
            if (block_len == fallthru_target) {
#ifdef JIT_DEBUG_VERBOSE
                DMSG("Continuing at %08X from fall-through branch at %08X",
                     address, start_address + fallthru_source*2);
#endif
                /* We don't actually update the word_info[] table until we
                 * have successfully scanned this instruction.  If we
                 * marked the branch as fall-through here but later decided
                 * to terminate the block for optimization reasons (e.g.
                 * unoptimizable pointers) before this instruction, the
                 * generated RTL would incorrectly set the PC to the
                 * instruction following the branch instead of the branch
                 * target. */
                at_fallthru = 1;
            }
            fallthru_target = fallthru_source = 0;
        }

        /* If this is a branch target, update the local constant list used
         * by subroutine folding. */

        if (is_branch_target[block_len/32] & (1 << (block_len%32))) {
            unsigned int reg;
            for (reg = 0; reg < 16; reg++) {
                local_constant[reg] = local_constant_per_insn[block_len][reg];
            }
        }

        /* Check for MAC.[WL] optimizations. */

        if (optimization_flags & SH2_OPTIMIZE_MAC_NOSAT) {
            if ((opcode & 0xF0FF) == 0x4007 || (opcode & 0xF0FF) == 0x400E) {
                sr_was_changed = 1;
            } else if ((opcode & 0xB00F) == 0x000F) {
                block_contains_mac = 1;
                if (sr_was_changed) {
                    can_optimize_mac_nosat = 0;  // Oh well, tough luck
                }
            }
        }

        /* Record pointer accesses and register modifications for pointer
         * optimization. */

        if (optimization_flags & SH2_OPTIMIZE_POINTERS) {

            optimize_pointers(start_address, address, opcode, opcode_info,
                              pointer_map);

            /* If we're looking at a MAC instruction beyond the beginning
             * of the block, and at least one of the source registers to
             * the MAC is an unknown pointer, terminate the block before
             * this instruction (or before a preceding CLRMAC if there was
             * a recent one) so we can translate the MAC with maximum
             * optimization.  But don't check this if we're in a delay
             * slot, since the delay slot isn't necessarily part of the
             * following code stream (and interrupting a block between a
             * branch and its delay slot will wreak havoc with processor
             * state). */
            if ((optimization_flags & SH2_OPTIMIZE_POINTERS_MAC)
             && block_len > 0
             && !now_in_delay
            ) {
                if ((opcode & 0xB00F) == 0x000F) {
                    int stop = 0, rollback = 0;
                    optimize_pointers_mac(
                        address, opcode, pointer_map,
                        last_clrmac<0 ? 0 : start_address + last_clrmac*2,
                        &stop, &rollback
                    );
                    if (stop) {
                        if (rollback) {
                            block_len = last_clrmac;
                            pointer_used = last_clrmac_pointer_used;
                            can_optimize_mac_nosat = last_clrmac_comn;
                            block_contains_mac = last_clrmac_bcm;
                        }
                        goto abort_scan;
                    }
                } else if (opcode == 0x0028) {
                    /* Remember this CLRMAC so we can terminate before it
                     * if we find a MAC with unoptimizable memory accesses */
                    last_clrmac = block_len;
                    last_clrmac_pointer_used = pointer_used;
                    last_clrmac_comn = can_optimize_mac_nosat;
                    last_clrmac_bcm = block_contains_mac;
                }
            }  // if ((opt_flags & SH2_OPTIMIZE_POINTERS_MAC) && etc.)

#if OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD > 0
            /* If this instruction accesses through a non-optimizable
             * pointer, and there are more accesses to the same pointer in
             * subsequent instructions, terminate the block before this
             * instruction.  (Again, don't do this if we're in a delay
             * slot.) */
            int pointer_reg = -1;
            if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rn) {
                pointer_reg = opcode>>8 & 0xF;
            } else if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rm) {
                pointer_reg = opcode>>4 & 0xF;
            }
            /* First make sure this isn't an instruction that overwrites
             * the same register it uses as a pointer. */
            if (((opcode_info & SH2_OPCODE_INFO_SETS_R0) && pointer_reg == 0)
             || ((opcode_info & SH2_OPCODE_INFO_SETS_Rn)
                 && pointer_reg == (opcode>>8 & 0xF))
            ) {
                pointer_reg = -1;
            }
            if (pointer_reg >= 0
             && pointer_map[pointer_reg] == 31
             && !now_in_delay
            ) {
                unsigned int accesses = 1;
                int i;
                for (i = 1; i < max_len - block_len; i++) {
                    const unsigned int opcode2 = fetch[i];
                    const unsigned int n = opcode2>>8 & 0xF;
                    const unsigned int m = opcode2>>8 & 0xF;
                    const uint32_t info2 = get_opcode_info(opcode2);
                    int access_reg = -1;
                    if (info2 & SH2_OPCODE_INFO_ACCESSES_Rn) {
                        access_reg = n;
                    } else if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rm) {
                        access_reg = m;
                    }
                    if (access_reg == pointer_reg) {
                        accesses++;
                        if (accesses >= OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD) {
# ifdef JIT_DEBUG_VERBOSE
                            DMSG("Breaking block from %08X at %08X due to"
                                 " unoptimizable pointer in r%d\n",
                                 start_address, address, pointer_reg);
# endif
                            goto abort_scan;
                        }
                    }
                    if (((info2 & SH2_OPCODE_INFO_SETS_R0) && pointer_reg == 0)
                     || ((info2 & SH2_OPCODE_INFO_SETS_Rn) && pointer_reg == n)
                     || (info2 & (SH2_OPCODE_INFO_BRANCH_UNCOND
                                  | SH2_OPCODE_INFO_BRANCH_COND))
                    ) {
                        /* The pointer register was overwritten, or a
                         * branch interrupted the block, so stop checking.
                         * (We stop even for conditional branches, so we
                         * don't break a block for the first access when
                         * that access is immediately followed by a check. */
                        break;
                    }
                }
            }  // if (pointer_reg >= 0 && etc.)
#endif  // OPTIMIZE_POINTERS_BLOCK_BREAK_THRESHOLD > 0

        }  // if (optimization_flags & SH2_OPTIMIZE_POINTERS)

#ifdef OPTIMIZE_LOOP_REGISTERS

        /* If this address is a branch target and we're within a loop, the
         * loop is too complex to optimize.  Of course, we won't know
         * whether we're in a loop until we see a branch back to the top,
         * so just track that we saw a branch target for now. */

        if (is_branch_target[block_len/32] & (1 << (block_len%32))) {
            saw_branch_target = 1;
        }

        /* If this is a potential loop, check for changes to register state. */

        if (can_optimize_loop) {
            check_loop_registers(opcode, opcode_info);
        }

#endif  // OPTIMIZE_LOOP_REGISTERS

        /* If the instruction is a static branch within the potential range
         * of this block, record the target; also check for optimizations. */

        if ((opcode & 0xF900) == 0x8900  // B[TF]{,_S}
         || (opcode & 0xF000) == 0xA000  // BRA
        ) {
            int32_t disp;
            if ((opcode & 0xF000) == 0xA000) {
                disp = opcode & 0xFFF;
                disp <<= 20;  // Sign-extend
                disp >>= 19;  // And double
            } else {
                disp = opcode & 0xFF;
                disp <<= 24;
                disp >>= 23;
            }
            uint32_t target = (address + 4) + disp;

#ifdef OPTIMIZE_BRANCH_THREAD
            /* If this is a conditional branch, check whether the target is
             * another conditional branch of the same sense, either without
             * a delay slot or with a NOP in the delay slot; if so, then
             * thread the branch through to the final target.  But don't
             * thread if this branch has a delay slot whose instruction
             * modifies SR.T. */
            if ((opcode & 0xF900) == 0x8900
             && target >= start_address
             && target <= start_address + max_len*2
             && (!(opcode & 0x0400)
                 || !(get_opcode_info(fetch[1]) & SH2_OPCODE_INFO_SETS_SR_T))
            ) {
                branch_thread_count[block_len] = 0;
                unsigned int target_insn0 = fetch[2+(disp/2)+0];
                unsigned int target_insn1 = fetch[2+(disp/2)+1];
                while ((target_insn0 & 0xFF00) == (opcode & 0xFB00)  // B[TF]
                    || ((target_insn0 & 0xFF00)
                        == ((opcode & 0xFB00) | 0x0400)  // B[TF]/S
                     && target_insn1 == 0x0009)  // NOP
                ) {
                    disp = ((int32_t)target_insn0 << 24) >> 23;
                    target = target + 4 + disp;
#ifdef JIT_DEBUG_VERBOSE
                    DMSG("Threading branch from 0x%08X through to 0x%08X",
                         address, target);
#endif
                    word_info[block_len] |= WORD_INFO_THREADED;
                    branch_thread_target[block_len] = target;
                    branch_thread_count[block_len]++;  // Assume no overflow
                    disp = target - (address + 4);
                    if (target < start_address
                     || target >= start_address + max_len*2
                    ) {
                        break;
                    }
                    target_insn0 = fetch[2+(disp/2)+0];
                    target_insn1 = fetch[2+(disp/2)+1];
                }
            }
#endif  // OPTIMIZE_BRANCH_THREAD

#ifdef OPTIMIZE_BRANCH_SELECT
            /* If this is a conditional branch, check whether the branch
             * acts like an RTL SELECT instruction.  To be treated like a
             * SELECT, the branch must skip exactly one instruction which
             * is not targeted by any other branch and which supports being
             * used as a SELECT source in the SH-2 decoder (sh2-core.i). */
            if (target < start_address + max_len*2) {
                if (optimize_branch_select(start_address, address, fetch,
                                           opcode, target)) {
                    goto skip_branch_target_check;
                }
            }
#endif  // OPTIMIZE_BRANCH_SELECT

            /* If enabled, check whether this branch targets a RTS/NOP pair.
             * We allow this check even if the target is out of range of
             * the block, on the assumption that they are part of the same
             * routine. */
            if (optimization_flags & SH2_OPTIMIZE_BRANCH_TO_RTS) {
                unsigned int target_insn0 = fetch[2+(disp/2)+0];
                unsigned int target_insn1 = fetch[2+(disp/2)+1];
                if (target_insn0 == 0x000B  // RTS
                 && target_insn1 == 0x0009  // NOP
                ) {
                    word_info[block_len] |= WORD_INFO_BRA_RTS;
                    goto skip_branch_target_check;
                }
            }

#ifdef OPTIMIZE_LOOP_REGISTERS
            /* If this is a backward branch to the beginning of the block,
             * mark the block as a loop and save loop state after the
             * branch's delay slot (if any).  But if we've seen a branch
             * target before (or at) this instruction, then the loop is too
             * complex to optimize, so clear the relevant flag. */
            if (target == start_address) {
                if (saw_branch_target) {
                    can_optimize_loop = 0;
                } else {
                    is_loop = 1;
                    at_loop_branch = 1;
                }
            }
#endif

#ifdef OPTIMIZE_LOOP_TO_JSR
            /* If this is a backward branch to two instructions before
             * the beginning of the block, record whether the targeted
             * instruction is a subroutine call (JSR, BSR, or BSRF).
             * In this case, also mark the following instruction as a
             * branch target, because we're effectively flipping the
             * sense of the branch and skipping the call and delay slot
             * on the opposite condition.  (If we don't do this, side
             * effects of the delay slot such as generating a native
             * pointer register for an SH-2 pointer won't be forgotten in
             * the fall-through case, causing the fall-through code to use
             * uninitialized registers and potentially crash.) */
            if (target == start_address - 4) {
                unsigned int target_insn = fetch[2+(disp/2)];
                if ((target_insn & 0xF0FF) == 0x400B  // JSR @Rn
                 || (target_insn & 0xF000) == 0xB000  // BSR label
                 || (target_insn & 0xF0FF) == 0x0003  // BSRF Rn
                ) {
                    word_info[block_len] |= WORD_INFO_LOOP_JSR;
                    uint32_t false_target;
                    if (opcode_info & SH2_OPCODE_INFO_BRANCH_DELAYED) {
                        false_target = address+2;
                    } else {
                        false_target = address+4;
                    }
                    const uint32_t target_index =
                        (false_target - start_address) / 2;
                    is_local_branch = 1;
                    local_branch_target_index = target_index;
                    const uint32_t flag = 1 << (target_index % 32);
                    local_branch_is_new = !(is_branch_target[index/32] & flag);
                    is_branch_target[target_index/32] |= flag;
                    /* We don't set the OPTIMIZE_BRANCH_FALLTHRU variables
                     * because the branch itself does _not_ fall through;
                     * this branch target handling is only a hack to avoid
                     * overoptimization. */
                }
            }
#endif

            /* Only record the branch in the lookup table if it's a forward
             * branch.  We allow backward branches to the beginning of the
             * block (index == 0), but we don't add them to the table
             * because they're handled specially--we insert a label before
             * any precondition checks instead of adding one when we
             * translate the instruction itself. */
            if (target > address) {
                const uint32_t target_index = (target - start_address) / 2;
                if (target_index < lenof(is_branch_target)*32) {
                    is_local_branch = 1;
                    local_branch_target_index = target_index;
                    const uint32_t flag = 1 << (target_index % 32);
                    if (is_branch_target[target_index/32] & flag) {
                        /* There's already another branch targeting the
                         * same instruction.  After processing any delay
                         * slot, we'll need to clear any constants from
                         * the target's local constant table which differ
                         * from the current known register values. */
                        local_branch_is_new = 0;
                    } else {
                        /* This is the first time we've seen this target;
                         * remember it in case we can fall through. */
#ifdef OPTIMIZE_BRANCH_FALLTHRU
                        fallthru_target = target_index;
                        fallthru_source = block_len;
#endif
                        local_branch_is_new = 1;
                    }
                    is_branch_target[target_index/32] |= flag;
                }
            }
          skip_branch_target_check:;  // From branch optimization above
        }

        /* If this is a BSR or a JSR to a known target, see whether it can
         * be folded into the current block. */

        if (optimization_flags & SH2_OPTIMIZE_FOLD_SUBROUTINES) {

            /* First check for PC-relative loads so we can track JSR
             * target addresses. */
            if ((opcode & 0xB000) == 0x9000) {
                const unsigned int n = opcode>>8 & 0xF;
                const unsigned int disp = opcode & 0xFF;
                if (opcode & 0x4000) {  // MOV.L
                    const unsigned int offset = (address&2 ? 1 : 2) + 2*disp;
                    local_constant[n] = fetch[offset]<<16 | fetch[offset+1];
                } else {  // MOV.W
                    const unsigned int offset = 2 + disp;
                    local_constant[n] = fetch[offset];
                }
            } else {
                if (opcode_info & SH2_OPCODE_INFO_SETS_R0) {
                    local_constant[0] = 1;
                }
                if (opcode_info & SH2_OPCODE_INFO_SETS_Rn) {
                    local_constant[opcode>>8 & 0xF] = 1;
                }
                if (opcode_info & (SH2_OPCODE_INFO_ACCESS_POSTINC
                                   | SH2_OPCODE_INFO_ACCESS_PREDEC)) {
                    if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rm) {
                        local_constant[opcode>>4 & 0xF] = 1;
                    }
                    if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rn) {
                        local_constant[opcode>>8 & 0xF] = 1;
                    }
                    if (opcode_info & SH2_OPCODE_INFO_ACCESSES_R15) {
                        local_constant[15] = 1;
                    }
                }
            }

            /* Now see if this is an optimizable call. */
            uint32_t target = 1; // 1 = not a subroutine call or unknown target
            if ((opcode & 0xF000) == 0xB000) {  // BSR label
                int32_t disp = opcode & 0xFFF;
                disp <<= 20;  // Sign-extend
                disp >>= 19;  // And double
                target = (address + 4) + disp;
            } else if ((opcode & 0xF0FF) == 0x400B) {  // JSR @Rn
                const unsigned int n = opcode>>8 & 0xF;
                if (local_constant[n]) {
                    target = local_constant[n];
                }
            }
            if (target != 1
             && optimize_fold_subroutine(state, start_address, address,
                                         target, fetch[1],
                                         pointer_map, local_constant,
                                         &subroutine_native[block_len])
            ) {
                word_info[block_len] |= WORD_INFO_FOLDABLE;
                subroutine_target[block_len] = target;
#ifdef OPTIMIZE_LOOP_REGISTERS
                /* Subroutine folding and loop optimization don't currently
                 * play together well, so disable loop optimization if we
                 * fold a subroutine into the current block.  (FIXME: this
                 * is a loss if the loop has already ended; better to just
                 * not fold and break the block instead in that case?) */
                is_loop = 0;
                can_optimize_loop = 0;
#endif
            }

        }  // if (optimization_flags & SH2_OPTIMIZE_FOLD_SUBROUTINES)

        /* Check whether this is an instruction with a delay slot. */

        delay = ((opcode_info & SH2_OPCODE_INFO_BRANCH_DELAYED) != 0);

        /* Check whether this is an unconditional branch or similar
         * instruction; when we get past its delay slot (if applicable),
         * we'll check whether to end the block. */

        at_uncond_branch |=
            (((opcode_info & SH2_OPCODE_INFO_BRANCH_UNCOND)
              && !(word_info[block_len] & WORD_INFO_FOLDABLE))
             || opcode == 0x001B);  // SLEEP

        /* If we have a pending local branch, update the local constant
         * table as appropriate. */

        if (is_local_branch && !delay) {
            is_local_branch = 0;
            unsigned int reg;
            if (local_branch_is_new) {
                for (reg = 0; reg < 16; reg++) {
                    local_constant_per_insn[local_branch_target_index][reg]
                        = local_constant[reg];
                }
            } else {
                for (reg = 0; reg < 16; reg++) {
                    if (local_constant_per_insn[local_branch_target_index][reg] != local_constant[reg]) {
                        local_constant_per_insn[local_branch_target_index][reg] = 1;
                    }
                }
            }
        }

        /* If we have a pending branch and we're past its delay slot
         * (if applicable), update loop optimization state. */

#ifdef OPTIMIZE_LOOP_REGISTERS
        at_branch |=
            at_uncond_branch || (opcode_info & SH2_OPCODE_INFO_BRANCH_COND);
        if (at_branch && !delay) {
            at_branch = 0;
            if (at_loop_branch) {
                final_live_registers = loop_live_registers;
                final_load_registers = loop_load_registers;
                final_changed_registers = loop_changed_registers;
                unsigned int reg;
                for (reg = 0; reg < 16; reg++) {
                    if (pointer_map[reg] != reg) {
                        loop_invariant_pointers &= ~(1 << reg);
                    }
                }
                final_invariant_pointers = loop_invariant_pointers;
                at_loop_branch = 0;
            }
# if !defined(TRACE) && !defined(TRACE_STEALTH) && !defined(TRACE_LITE)
            /* If this is a branch back to the beginning of the loop from
             * the middle, followed by more loop body code, then any
             * registers first set below this branch will still have the
             * proper value on exit from the loop, assuming an exception
             * handler interrupting the loop doesn't try to alter or rely
             * on the loop's state.  But this will cause traces (including
             * TRACE_LITE) to report invalid register contents if the
             * early branch is taken and stops due to the cycle limit
             * before the register has been set at least once, so if we're
             * tracing, we make an exception to our "don't change behavior
             * with TRACE_STEALTH/TRACE_LITE" rule and set the noload
             * register set here.  If we're _not_ tracing, we omit this
             * "else" and let the noload registers continue to accumulate. */
            else
# endif
            if (!loop_noload_registers_set) {
                loop_noload_registers = loop_changed_registers;
                loop_noload_registers_set = 1;
            }
        }  // if (at_branch && !delay)
#endif  // OPTIMIZE_LOOP_REGISTERS

        /* If this is an instruction that loads SR (LDC ...,SR), terminate
         * the block after the next instruction (since the SH-2 ignores
         * interrupts between LDC and the following instruction--SH7604
         * manual page 76, section 4.6.2) to allow any potentially unmasked
         * interrupts to be recognized.  While the other LD[CS]/ST[CS]
         * instructions also ignore interrupts for the following
         * instruction, we don't terminate the block for them anyway, so
         * we don't worry about them here. */

        if ((opcode & 0xF0FF) == 0x4007 || (opcode & 0xF0FF) == 0x400E) {
#ifdef JIT_ACCURATE_LDC_SR_TIMING
            if (block_len+1 >= max_len) {
                /* We'll hit the block size limit after this instruction,
                 * so terminate the block _before_ the LDC and include it
                 * in the next block.  If the following instruction is in a
                 * blacklisted address, we'll end up interpreting the LDC,
                 * which is fine too. */
                goto abort_scan;
            } else {
                end_block_next = 1;
            }
#else
            end_block = 1;
#endif
        }

        /* If this instruction is the target of a preceding fall-through
         * branch, update the word_info[] and is_branch_target[] tables
         * to reflect that.
         *
         * IMPORTANT:  Once this code is executed, it is CRITICAL that this
         * instruction be included in the scanned block, or incorrect code
         * will result!  See the earlier code that sets at_fallthru for an
         * explanation. */

        if (at_fallthru) {
#ifdef JIT_DEBUG_VERBOSE
            DMSG("Marking branch at 0x%08X as fall-through",
                 start_address + saved_fallthru_source*2);
#endif
            word_info[saved_fallthru_source] |= WORD_INFO_FALLTHRU;
            /* Also clear the branch target flag so we don't set a label
             * (which would break the basic unit and cause an optimization
             * barrier). */
            const uint32_t flag = 1 << (saved_fallthru_target % 32);
            is_branch_target[saved_fallthru_target/32] &= ~flag;
        }

        /* If we have a pending unconditional branch and we're past its
         * delay slot (if applicable), check whether there are any branch
         * targets beyond this address which could be part of the same
         * block (i.e. are within the instruction limit as well as
         * JIT_MAX_INSN_GAP).  If there are, we'll continue translating at
         * the first such address; otherwise we'll end the block here.
         * (In !JIT_ALLOW_DISTANT_BRANCHES mode, we always stop unless the
         * next instruction is a branch target.) */

        if (at_uncond_branch && !delay && !end_block) {
            at_uncond_branch = 0;
            index = block_len + 1;

            /* If variable shift optimization is enabled, allow a BRAF
             * followed by one or more shift/rotate instructions to fall
             * through to the instruction following the shift sequence. */
#ifdef OPTIMIZE_VARIABLE_SHIFTS
            if ((fetch[-1] & 0xF0FF) == 0x0023) {  // BRAF
                unsigned int next_opcode = fetch[1];
                if ((next_opcode & 0xF0DE) == 0x4000  // SH[LA][LR]
                 || (next_opcode & 0xF0FE) == 0x4004  // ROT[LR]
                ) {
                    const unsigned int shift_opcode = next_opcode;
                    do {
                        address += 2;
                        fetch++;
                        block_len++;
                        next_opcode = fetch[1];
                    } while (next_opcode == shift_opcode);
                }
            } else
#endif

            if (!(is_branch_target[index/32] & (1 << (index % 32)))) {
                end_block = 1;
#ifdef JIT_ALLOW_DISTANT_BRANCHES
                /* See below for why the "-1" on max_len */
                const uint32_t gap_limit = block_len + JIT_MAX_INSN_GAP;
                const uint32_t index_limit = (max_len-1 < gap_limit
                                              ? max_len-1 : gap_limit);
                const uint32_t address_limit =
                    start_address + (index_limit * 2);
                const uint32_t first_valid =
                    (now_in_delay ? address : address+2);
                index = (first_valid - start_address) / 2;
                /* Search for a non-empty word, then find the first bit set
                 * in that word */
                uint32_t mask = 0xFFFFFFFF << (index % 32);
                index /= 32;
                for (; index < lenof(is_branch_target); index++) {
                    if (is_branch_target[index] & mask) {
                        break;
                    }
                    mask = 0xFFFFFFFF;
                }
                if (index < lenof(is_branch_target)) {
                    mask &= is_branch_target[index];
                    index *= 32;
                    while (!(mask & 1)) {
                        mask >>= 1;
                        index++;
                    }
                    const uint32_t first_target = start_address + index*2;
                    if (first_target < address_limit) {
                        int skip_bytes = first_target - (address+2);
                        if (skip_bytes > 0) {
                            address += skip_bytes;
                            fetch += skip_bytes / 2;
                            block_len += skip_bytes / 2;
                        }
                        end_block = 0;
                    }
                }
#endif
            }  // if next insn is not a branch target
        }  // if (at_uncond_branch && !delay && !end_block)

        /* Check whether we've hit the instruction limit.  To avoid the
         * potential for problems arising from an instruction with a delay
         * slot crossing the limit, we stop one instruction early; if this
         * instruction had a delay slot, the delay slot will still fall
         * within the limit.  (We could also explicitly check against
         * (max_len+delay), but this way is both simpler and marginally
         * faster.)  However, if we're stopping after the following
         * instruction due to an LDC SR, don't break early here. */

        if (!end_block && !end_block_next && block_len+2 >= max_len) {
#ifdef JIT_DEBUG
            DMSG("WARNING: Terminating block 0x%08X after 0x%08X due to"
                 " instruction limit (%d insns) or blacklist",
                 start_address, delay ? address+2 : address, max_len);
#endif
            end_block = 1;
        }

#ifdef OPTIMIZE_VARIABLE_SHIFTS

        /* If this opcode begins a variable shift instruction sequence,
         * skip the remaining instructions in the sequence.  (Normally we
         * don't alter scanning behavior for optimizations, but since
         * variable shift sequences involve branches, we want to avoid
         * marking the following instruction as a branch target--thus
         * creating an optimization barrier--when the branches will be
         * eliminated anyway.)  However, we skip sequences starting with
         * BRAF, since we handle them separately. */

        if (!now_in_delay && (opcode & 0xF0FF) != 0x0023) {
            unsigned int count_reg, count_max, shift_reg, type;
            const uint8_t *cycles;
            const unsigned int num_insns =
                can_optimize_variable_shift(fetch, address, &count_reg,
                                            &count_max, &shift_reg, &type,
                                            &cycles);
            if (num_insns) {
                block_len += num_insns - 1;
                fetch += num_insns - 1;
                address += (num_insns - 1) * 2;
            }
        }

#endif

    }  // for (; !end_block || delay; address += 2, fetch++, block_len++)

  abort_scan:

    /* Record the final set of entry registers used as pointers. */

    pointer_used &= 0xFFFF;

#ifdef OPTIMIZE_LOOP_REGISTERS
    /* If this block is a loop, update loop_*_registers with the values
     * found at the end of the loop body, so we don't unnecessarily fix
     * SH-2 registers that aren't used in the body of the loop; also add
     * registers changed after any non-loop branch to the set of registers
     * that must be preloaded.  Otherwise, there's no point in attempting
     * loop optimization, so clear the flag. */

    if (is_loop) {
        loop_live_registers = final_live_registers;
        loop_load_registers = final_load_registers;
        loop_changed_registers = final_changed_registers;
        loop_invariant_pointers = final_invariant_pointers;
        if (loop_noload_registers_set) {
            loop_load_registers |=
                (loop_changed_registers & ~loop_noload_registers);
        }
    } else {
        can_optimize_loop = 0;
    }
#endif

    /* All done! */
    return block_len;
}

/*-----------------------------------------------------------------------*/

/**
 * optimize_pointers:  Check the current instruction to determine whether
 * it accesses memory through a pointer register or modifies a register
 * assumed to be a pointer.  Helper function for scan_block().
 *
 * [Parameters]
 *     start_address: Start address of block, or 1 for subroutine fold checks
 *           address: Address of current instruction
 *            opcode: Opcode of current instruction
 *       opcode_info: Information about current instruction
 *       pointer_map: pointer_map[] array pointer from scan_block()
 * [Return value]
 *     None
 */
static inline void optimize_pointers(
    uint32_t start_address, uint32_t address, unsigned int opcode,
    uint32_t opcode_info, uint8_t pointer_map[16])
{
    const unsigned int index = (address - start_address) / 2;
    if (start_address != 1
     && (is_data_pointer_load[index/32] & (1 << (index % 32)))
    ) {

        if ((opcode & 0xFF00) == 0xC700) {  // MOVA
            pointer_map[0] = 30;
#ifdef JIT_DEBUG_VERBOSE
            DMSG("%08X marked as data pointer load for R0", address);
#endif
        } else {
            const unsigned int n = opcode>>8 & 0xF;
            pointer_map[n] = 30;
#ifdef JIT_DEBUG
            if (!(opcode_info & SH2_OPCODE_INFO_SETS_Rn)) {
                DMSG("WARNING: %08X marked as data pointer load but does not"
                     " set Rn!", address);
# ifdef JIT_DEBUG_VERBOSE
            } else {
                DMSG("%08X marked as data pointer load for R%u", address, n);
# endif
            }
#endif
        }

    } else if ((opcode & 0xF00F) == 0x6003) {  // MOV Rm,Rn

        const unsigned int n = opcode>>8 & 0xF;
        const unsigned int m = opcode>>4 & 0xF;
        pointer_map[n] = pointer_map[m];

    } else if ((opcode & 0xFF00) == 0xC700) {  // MOVA @(disp,PC),R0

        pointer_map[0] = 30;

    } else if ((opcode & 0xF000) == 0x7000) {  // ADD #imm,Rn

        /* This only modifies Rn by a small, fixed amount, so assume the
         * pointer is still valid and don't clear pointer_map[n]. */

    } else if ((opcode & 0xF00F) == 0x3008) {  // SUB Rm,Rn

        const unsigned int m = opcode>>4 & 0xF;
        const unsigned int n = opcode>>8 & 0xF;
        if (pointer_map[m] == 31) {
            /* If we're subtracting a non-pointer from a pointer, assume
             * it's a pointer adjustment that leaves the pointer in Rn
             * valid.  (If Rn is already not a pointer, we don't need to
             * do anything in the first place.) */
        } else {
            pointer_map[n] = 31;
        }

    } else if ((opcode & 0xF00F) == 0x300C) {  // ADD Rm,Rn

        const unsigned int m = opcode>>4 & 0xF;
        const unsigned int n = opcode>>8 & 0xF;
        if (pointer_map[m] == 31) {
            /* Like SUB Rm,Rn, we ignore ADD Rm,Rn where Rm is not a
             * pointer on the assumption that it's a pointer adjustment. */
        } else if (pointer_map[n] == 31) {
            /* Addition is commutative, unlike subtraction, so we could
             * also have the case where Rm is a pointer and Rn is an
             * adjustment, leaving a pointer value in Rn.  Treat this case
             * like MOV Rm,Rn where Rm is a pointer. */
            pointer_map[n] = pointer_map[m];
        } else {
            pointer_map[n] = 31;
        }

    } else {

        const unsigned int m = opcode>>4 & 0xF;
        const unsigned int n = opcode>>8 & 0xF;

        if (opcode_info & SH2_OPCODE_INFO_SETS_R0) {
            pointer_map[0] = 31;
        }
        if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rm) {
            pointer_used |= 1 << pointer_map[m];
        } else if (opcode_info & SH2_OPCODE_INFO_ACCESSES_R0_Rm) {
            if (pointer_map[0] == 30
             || (pointer_map[0] != 31 && (pointer_regs & 1<<pointer_map[0]))
            ) {
                pointer_used |= 1 << pointer_map[0];
            } else {
                pointer_used |= 1 << pointer_map[m];
            }
        }
        if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rn) {
            pointer_used |= 1 << pointer_map[n];
        } else if (opcode_info & SH2_OPCODE_INFO_ACCESSES_R0_Rn) {
            if (pointer_map[0] == 30
             || (pointer_map[0] != 31 && (pointer_regs & 1<<pointer_map[0]))
            ) {
                pointer_used |= 1 << pointer_map[0];
            } else {
                pointer_used |= 1 << pointer_map[n];
            }
        }
        if (opcode_info & SH2_OPCODE_INFO_SETS_Rn) {
            pointer_map[n] = 31;
        }
        if (opcode_info & SH2_OPCODE_INFO_ACCESSES_R15) {
            pointer_used |= 1 << pointer_map[15];
        }

    }
}

/*-----------------------------------------------------------------------*/

/**
 * optimize_pointers_mac:  Check the current MAC instruction to determine
 * whether the block should be terminated to improve pointer optimization.
 * Helper function for scan_block().
 *
 * [Parameters]
 *           opcode: Opcode of current instruction
 *      pointer_map: pointer_map[] array pointer from scan_block()
 *          address: Address of current instruction
 *      last_clrmac: Address of last CLRMAC instruction seen, or zero if none
 *         stop_ret: Pointer to variable to receive nonzero if the block
 *                      should be terminated
 *     rollback_ret: Pointer to variable to receive nonzero if, when
 *                      terminating the block, the termination point should
 *                      roll back to before the last CLRMAC instruction
 * [Return value]
 *     None
 */
static inline void optimize_pointers_mac(
    uint32_t address, unsigned int opcode, uint8_t pointer_map[16],
    uint32_t last_clrmac, int *stop_ret, int *rollback_ret)
{
    const int n = opcode>>8 & 0xF;
    const int m = opcode>>4 & 0xF;
    if (pointer_map[n] == 31
     || (pointer_map[n] != 30 && !(pointer_regs & (1<<pointer_map[n])))
     || pointer_map[m] == 31
     || (pointer_map[m] != 30 && !(pointer_regs & (1<<pointer_map[m])))
    ) {
        *stop_ret = 1;
        const unsigned int CLRMAC_SCAN_INSNS = 8;
        if (last_clrmac && address - last_clrmac <= CLRMAC_SCAN_INSNS*2) {
            /* There's a recent CLRMAC, so roll back to that position and
             * terminate */
#ifdef JIT_DEBUG_VERBOSE
            DMSG("Terminating block before CLRMAC at %08X due to"
                 " unoptimizable MAC at %08X (pointer status of R%u is"
                 " unknown)", last_clrmac, address,
                 (pointer_map[n] == 31
                  || (pointer_map[n] != 30
                      && !(pointer_regs & (1<<pointer_map[n]))))
                 ? n : m);
#endif
            *rollback_ret = 1;
        } else {
            /* Terminate before the MAC */
#ifdef JIT_DEBUG_VERBOSE
            DMSG("Terminating block before unoptimizable MAC at %08X"
                 " (pointer status of R%u is unknown)", address,
                 (pointer_map[n] == 31
                  || (pointer_map[n] != 30
                      && !(pointer_regs & (1<<pointer_map[n]))))
                 ? n : m);
#endif
        }
    }
}

/*-----------------------------------------------------------------------*/

#ifdef OPTIMIZE_LOOP_REGISTERS

/**
 * check_loop_registers:  Check the effects of the given instruction on
 * register state, and update loop_live_registers, loop_load_registers,
 * and loop_changed_registers as appropriate.  Helper function for
 * scan_block().
 *
 * [Parameters]
 *          opcode: Opcode of instruction to check
 *     opcode_info: Information about current instruction
 * [Return value]
 *     None
 * [Preconditions]
 *     The opcode is assumed to be a valid SH-2 opcode.
 */
static inline void check_loop_registers(
    unsigned int opcode, uint32_t opcode_info)
{
    uint32_t this_used;  // Registers used by this instruction
    uint32_t this_set;   // Registers set by this instruction

    /* Indices for operands */
    const unsigned int Rn  = opcode>>8 & 0xF;
    const unsigned int Rm  = opcode>>4 & 0xF;
    /* And other register indices */
    const unsigned int R0  = 0;
    const unsigned int R15 = 15;
    const unsigned int SR  = state_cache_index(offsetof(SH2State,SR));
    const unsigned int GBR = state_cache_index(offsetof(SH2State,GBR));
    const unsigned int VBR = state_cache_index(offsetof(SH2State,VBR));
    const unsigned int MACH= state_cache_index(offsetof(SH2State,MACH));
    const unsigned int MACL= state_cache_index(offsetof(SH2State,MACL));
    const unsigned int PR  = state_cache_index(offsetof(SH2State,PR));
    #define SR_GBR_VBR(x)    ((x)==0 ? SR : (x)==1 ? GBR : VBR)
    #define MACH_MACL_PR(x)  ((x)==0 ? MACH : (x)==1 ? MACL : PR)

    if ((opcode & 0xF00F) == 0x0002) {          // STC ...,Rn
        this_used = 1<<SR_GBR_VBR(Rm);
        this_set  = 1<<Rn;
    } else if ((opcode & 0xF0FF) == 0x0003      // BSRF
            || (opcode & 0xF0FF) == 0x400B      // JSR
            || (opcode & 0xF000) == 0xB000) {   // BSR
        /* MUST come before 0xF00F/0x400B */
        this_used = 1<<Rn;
        this_set  = 1<<PR;
    } else if ((opcode & 0xF00F) == 0x0007      // MUL.L
            || (opcode & 0xF00E) == 0x200E) {   // MUL*.W
        this_used = 1<<Rm | 1<<Rn;
        this_set  = 1<<MACL;
    } else if ((opcode & 0xF0EF) == 0x0008      // CLRT / SETT
            || (opcode & 0xF0FF) == 0x0019) {   // DIV0U
        this_used = 1<<SR;
        this_set  = 1<<SR;
    } else if ((opcode & 0xF0FF) == 0x0028) {   // CLRMAC
        this_used = 0;
        this_set  = 1<<MACH | 1<<MACL;
    } else if ((opcode & 0xF0FF) == 0x0029) {   // MOVT
        this_used = 1<<SR;
        this_set  = 1<<Rn;
    } else if ((opcode & 0xF00F) == 0x000A) {   // STS ...,Rn
        this_used = 1<<MACH_MACL_PR(Rm);
        this_set  = 1<<Rn;
    } else if ((opcode & 0xF0FF) == 0x000B) {   // RTS
        this_used = 1<<PR;
        this_set  = 0;
    } else if ((opcode & 0xF0FF) == 0x002B) {   // RTE
        this_used = 1<<R15;
        this_set  = 1<<R15 | 1<<SR;
    } else if ((opcode & 0xB00F) == 0x000F) {   // MAC.*
        this_used = 1<<Rm | 1<<Rn;
        this_set  = 1<<MACL | 1<<MACH;
    } else if ((opcode & 0xF00F) == 0x3004      // DIV1
            || (opcode & 0xF00A) == 0x300A) {   // ADDC / ADDV / SUBC / SUBV
        /* MUST come before 0xF008/0x3000 */
        this_used = 1<<Rm | 1<<Rn | 1<<SR;
        this_set  = 1<<Rn | 1<<SR;
    } else if ((opcode & 0xF007) == 0x3005) {   // DMUL*.L
        /* MUST come before 0xF008/0x3000 */
        this_used = 1<<Rm | 1<<Rn;
        this_set  = 1<<MACH | 1<<MACL;
    } else if ((opcode & 0xF00F) == 0x2007      // DIV0S
            || (opcode & 0xF00B) == 0x2008      // TST / CMP/ST
            || (opcode & 0xF008) == 0x3000) {   // CMP/*
        this_used = 1<<Rm | 1<<Rn | 1<<SR;
        this_set  = 1<<SR;
    } else if ((opcode & 0xF0FB) == 0x4011) {   // CMP/PZ / CMP/PL
        this_used = 1<<Rn | 1<<SR;
        this_set  = 1<<SR;
    } else if ((opcode & 0xF00F) == 0x200A && Rm == Rn) {  // XOR Rn,Rn
        /* This doesn't actually rely on the value of Rn, and seems to be
         * common enough that it's worth a special case to optimize it */
        this_used = 0;
        this_set  = 1<<Rn;
    } else if ((opcode & 0xF00F) == 0x3008 && Rm == Rn) {  // SUBC Rn,Rn
        /* This likewise doesn't rely on the value of Rn, and is commonly
         * used in signed division routines */
        this_used = 1<<SR;
        this_set  = 1<<Rn;
    } else if ((opcode & 0xF00A) == 0x4000) {   // shift / rotate / DT
        this_used = 1<<Rn | 1<<SR;
        this_set  = 1<<Rn | 1<<SR;
    } else if ((opcode & 0xF00F) == 0x4002) {   // STS ...,@-Rn
        this_used = 1<<MACH_MACL_PR(Rm) | 1<<Rn;
        this_set  = 0;
    } else if ((opcode & 0xF00F) == 0x4003) {   // STC ...,@-Rn
        this_used = 1<<SR_GBR_VBR(Rm) | 1<<Rn;
        this_set  = 0;
    } else if ((opcode & 0xF00F) == 0x4006) {   // LDS @Rn+,...
        this_used = 1<<Rn;
        this_set  = 1<<MACH_MACL_PR(Rm) | 1<<Rn;
    } else if ((opcode & 0xF00F) == 0x4007) {   // LDC @Rn+,...
        this_used = 1<<Rn;
        this_set  = 1<<SR_GBR_VBR(Rm) | 1<<Rn;
    } else if ((opcode & 0xF00F) == 0x400A) {   // LDS Rn,...
        this_used = 1<<Rn;
        this_set  = 1<<MACH_MACL_PR(Rm);
    } else if ((opcode & 0xF00F) == 0x400E) {   // LDC Rn,...
        this_used = 1<<Rn;
        this_set  = 1<<SR_GBR_VBR(Rm);
    } else if ((opcode & 0xF00F) == 0x600A) {   // NEGC
        this_used = 1<<Rm | 1<<SR;
        this_set  = 1<<Rn | 1<<SR;
    } else if ((opcode & 0xFF00) == 0x8800) {   // CMP/EQ #imm,R0
        this_used = 1<<R0 | 1<<SR;
        this_set  = 1<<SR;
    } else if ((opcode & 0xF900) == 0x8900) {   // BT / BF / BT/S / BF/S
        this_used = 1<<SR;
        this_set  = 0;
    } else if ((opcode & 0xFF00) == 0xC300) {   // TRAPA #imm
        /* MUST come before 0xFC00/0xC000 */
        this_used = 1<<R15 | 1<<SR;
        this_set  = 1<<R15;
    } else if ((opcode & 0xFC00) == 0xC000      // MOV.* R0,@(disp,GBR)
            || (opcode & 0xFC00) == 0xCC00) {   // logic_op.B #imm,@(R0,GBR)
        this_used = 1<<R0 | 1<<GBR;
        this_set  = 0;
    } else if ((opcode & 0xFC00) == 0xC400) {   // MOV.* @(disp,GBR),R0
        this_used = 1<<GBR;
        this_set  = 1<<R0;
    } else if ((opcode & 0xFF00) == 0xC800) {   // TST #imm,R0
        /* MUST come before 0xFC00/0xC800 */
        this_used = 1<<R0 | 1<<SR;
        this_set  = 1<<SR;
    } else {  // Generic instruction
        this_used = this_set = 0;
        if (opcode_info & SH2_OPCODE_INFO_USES_R0) {
            this_used |= 1<<R0;
        }
        if (opcode_info & SH2_OPCODE_INFO_USES_Rm) {
            this_used |= 1<<Rm;
        }
        if (opcode_info & SH2_OPCODE_INFO_USES_Rn) {
            this_used |= 1<<Rn;
        }
        if (opcode_info & SH2_OPCODE_INFO_USES_R15) {
            this_used |= 1<<R15;
        }
        if (opcode_info & SH2_OPCODE_INFO_SETS_R0) {
            this_set |= 1<<R0;
        }
        if (opcode_info & SH2_OPCODE_INFO_SETS_Rn) {
            this_set |= 1<<Rn;
        }
        if (opcode_info & (SH2_OPCODE_INFO_ACCESS_POSTINC
                         | SH2_OPCODE_INFO_ACCESS_PREDEC)
        ) {
            if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rn) {
                this_set |= 1<<Rn;
            } else if (opcode_info & SH2_OPCODE_INFO_ACCESSES_Rm) {
                this_set |= 1<<Rm;
            } else {  // Must be an R15 access
                this_set |= 1<<Rn;
            }
        }
    }

    /* Any registers not yet live which are used by this instruction must
     * be loaded at the top of the loop */
    this_used &= ~loop_live_registers;
    loop_load_registers |= this_used;
    loop_live_registers |= this_used;

    /* Any registers not yet live which are _set_ by this instruction will
     * _not_ need to be loaded */
    loop_live_registers |= this_set;

    /* All registers set by this instruction go into the changed set */
    loop_changed_registers |= this_set;
}

#endif  // OPTIMIZE_LOOP_REGISTERS

/*-----------------------------------------------------------------------*/

#ifdef OPTIMIZE_BRANCH_SELECT

/**
 * optimize_branch_select:  Determine whether a branch instruction can be
 * converted to an RTL SELECT instruction, and update word_info[]
 * accordingly.
 *
 * [Parameters]
 *     start_address: Start address of block
 *           address: Address of current instruction
 *             fetch: Fetch pointer for current instruction
 *            opcode: Opcode of current (branch) instruction
 *            target: Branch target of current instruction
 * [Return value]
 *     Nonzero if the branch can be converted to a SELECT, else zero
 * [Preconditions]
 *     The opcode is assumed to be a conditional branch instruction.
 */
static inline int optimize_branch_select(
    uint32_t start_address, uint32_t address, const uint16_t *fetch,
    unsigned int opcode, uint32_t target)
{
    const int disp = target - address;
    if ((opcode & 0xF900) == 0x8900 && disp == ((opcode & 0x400) ? 6 : 4)) {
# ifdef JIT_DEBUG_VERBOSE
        static const char * const mnemonics[] = {"BT", "BF", "BT/S", "BF/S"};
        const char * const mnemonic = mnemonics[(opcode >> 9) & 3];
# endif
        const uint32_t skip_index = ((target-2) - start_address) / 2;
        if (!(is_branch_target[skip_index/32] & (1<<(skip_index%32)))) {
            const unsigned int skipped_insn = fetch[(opcode & 0x400) ? 2 : 1];
            if ((skipped_insn & 0xF00F) == 0x6003  // MOV Rm,Rn
             || (skipped_insn & 0xF000) == 0x7000  // ADD #imm,Rn
             || (skipped_insn & 0xF000) == 0xE000  // MOV #imm,Rn
            ) {
                word_info[(address - start_address) / 2] |= WORD_INFO_SELECT;
# ifdef JIT_DEBUG_VERBOSE
                DMSG("Converted %s to SELECT at %08X", mnemonic, address);
# endif
                return 1;
# ifdef JIT_DEBUG_VERBOSE
            } else {
                DMSG("Failed to convert %s to SELECT at %08X (skipped opcode"
                     " is %04X)", mnemonic, address, skipped_insn);
# endif
            }
# ifdef JIT_DEBUG_VERBOSE
        } else {
            DMSG("Failed to convert %s to SELECT at %08X (skipped instruction"
                 " at %08X is a branch target)", mnemonic, address, target-2);
# endif
        }
    }
    return 0;
}

#endif  // OPTIMIZE_BRANCH_SELECT

/*-----------------------------------------------------------------------*/

/**
 * optimize_fold_subroutine:  Determine whether the code sequence starting
 * at the SH-2 address "target" qualifies as a foldable subroutine.  If so,
 * update the pointer_map[] array with any changes as a result of the
 * subroutine.
 *
 * [Parameters]
 *              state: Processor state block pointer
 *      start_address: Start address of block
 *            address: Address of subroutine call
 *             target: Target address of subroutine call
 *         delay_slot: Instruction word in delay slot of subroutine call
 *        pointer_map: pointer_map[] array pointer from scan_block()
 *     local_constant: local_constant[] array pointer from scan_block()
 *         native_ret: Pointer to variable to receive native implementation
 *                        function address if the routine is to be folded
 *                        using a native implementation, NULL if the routine
 *                        is to be folded by translating instructions
 * [Return value]
 *     Nonzero if subroutine is foldable, else zero
 */
static int optimize_fold_subroutine(
    SH2State *state, const uint32_t start_address, uint32_t address,
    const uint32_t target, const uint16_t delay_slot, uint8_t pointer_map[16],
    uint32_t local_constant[16], SH2NativeFunctionPointer *native_ret)
{
    const uint16_t *fetch_base = (const uint16_t *)fetch_pages[target >> 19];
    if (fetch_base == NULL) {
        return 0;  // Unfetchable, therefore unoptimizable
    }
    const uint16_t *fetch =
        (const uint16_t *)((uintptr_t)fetch_base + target);
    const uint16_t * const start = fetch;
    const uint16_t * const limit =
        fetch + OPTIMIZE_FOLD_SUBROUTINES_MAX_LENGTH + 2;

    /* First see if there's a native implementation which we can call. */

    if (manual_optimization_callback) {
        ignore_optimization_hints = 1;
        const unsigned int hand_tuned_len =
            (*manual_optimization_callback)(state, target, fetch,
                                            native_ret, 1);
        ignore_optimization_hints = 0;
        if (hand_tuned_len > 0) {
            if (*native_ret == NULL) {
                return 0;  // Folding was refused by the callback.
            }
#ifdef JIT_DEBUG_VERBOSE
            DMSG("Using hand-tuned code to fold subroutine at 0x%08X", target);
#endif
            /* We assume all of R0-R7 have been destroyed. */
            unsigned int reg;
            for (reg = 0; reg < 8; reg++) {
                pointer_map[reg] = 31;
                local_constant[reg] = 1;
            }
            return 1;
        }
    }

    /* Make a local copy of optimization state data, so we can revert if
     * we discover that the subroutine can't be optimized. */

    uint8_t saved_pointer_map[16];
    memcpy(saved_pointer_map, pointer_map, 16);
    const uint16_t saved_pointer_used = pointer_used;

    /* Scan the delay slot, then each instruction in the subroutine.
     * Branches (or similar instructions) are not permitted in delay
     * slots, so we assume the instruction is a simple one. */

    if (optimization_flags & SH2_OPTIMIZE_POINTERS) {
        optimize_pointers(start_address, address+2, delay_slot,
                          get_opcode_info(delay_slot), pointer_map);
    }

    for (address = target;
         !(fetch >= start+2 && fetch[-2] == 0x000B);
         address += 2, fetch++
    ) {
        if (fetch > limit) {
            goto fail;  // Subroutine is too long
        }
        const uint16_t opcode = *fetch;
        const uint32_t opcode_info = get_opcode_info(opcode);
        if (opcode == 0x000B) {  // RTS
            /* Ignore; we'll catch it after the delay slot */
        } else if (opcode_info & (SH2_OPCODE_INFO_BRANCH_UNCOND
                                  | SH2_OPCODE_INFO_BRANCH_COND)) {
            goto fail;  // Branches break optimization
        } else if ((optimization_flags & SH2_OPTIMIZE_POINTERS_MAC)
                   && (opcode & 0xB00F) == 0x000F) {  // MAC.[WL]
            const int n = opcode>>8 & 0xF;
            const int m = opcode>>4 & 0xF;
            if (pointer_map[n] == 31
             || (pointer_map[n] != 30 && !(pointer_regs & (1<<pointer_map[n])))
             || pointer_map[m] == 31
             || (pointer_map[m] != 30 && !(pointer_regs & (1<<pointer_map[m])))
            ) {
                /* This MAC can't be optimized in the current context, so
                 * let the subroutine call go through as normal--we may be
                 * able to optimize the MAC pointers that way. */
                goto fail;
            }
        } else if ((opcode & 0xF0FF) == 0x4007) {  // LDC Rn,SR
            goto fail;  // LDC ...,SR can cause an interrupt, so don't optimize
        } else if ((opcode & 0xF0FF) == 0x001B) {  // SLEEP
            goto fail;  // SLEEP will always break the block, so don't optimize
        }
        if (optimization_flags & SH2_OPTIMIZE_POINTERS) {
            optimize_pointers(target, address, opcode, opcode_info,
                              pointer_map);
        }
        // FIXME: update local_constant[] too
    }

    /* The subroutine is small and simple enough to optimize by translation. */
    *native_ret = NULL;
    return 1;

    /* Roll back the optimization state if the subroutine can't be
     * optimized. */
  fail:
    memcpy(pointer_map, saved_pointer_map, 16);
    pointer_used = saved_pointer_used;
    return 0;
}

/*************************************************************************/

/**
 * jit_exec:  Execute translated code from the given block.
 *
 * [Parameters]
 *       state: Processor state block
 *       entry: Translated code block
 *     address: Address to execute from, or NULL to execute from the
 *                 beginning of the block
 * [Return value]
 *     None
 */
static inline void jit_exec(SH2State *state, JitEntry *entry)
{
    entry->running = 1;

#ifdef JIT_PROFILE
    const uint32_t start_cycles = state->cycles;
#endif

#ifdef JIT_DEBUG_INTERPRET_RTL

# ifdef JIT_PROFILE
    entry->exec_time +=
# endif
    rtl_execute_block(entry->rtl, state);

#else  // !JIT_DEBUG_INTERPRET_RTL

    const void (*native_code)(SH2State *state) = entry->native_code;

# ifdef JIT_PROFILE
    /* For systems that have an efficient way to measure execution time
     * (such as a performance counter register), we could use that to
     * update entry->exec_time.  On the PSP, the MIPS Count register is
     * only accessible in kernel mode, so we're stuck with using a syscall
     * to a kernel routine that costs hundreds of cycles (where a short
     * block of code may finish in as few as 10 cycles), and we can't even
     * get a cycle-accurate count.  Oh well... it's better than nothing. */
#  if defined(PSP)
    const uint32_t start = sceKernelGetSystemTimeLow();
#  endif
# endif
    (*native_code)(state);
# ifdef JIT_PROFILE
#  if defined(PSP)
    entry->exec_time += sceKernelGetSystemTimeLow() - start;
#  else
    entry->exec_time++;  // Default is to just count each call as 1 time unit
#  endif
# endif

#endif  // JIT_DEBUG_INTERPRET_RTL

#ifdef JIT_PROFILE
    entry->call_count++;
    entry->cycle_count += state->cycles - start_cycles;
#endif

    entry->running = 0;
}

/*************************************************************************/

/**
 * decode_insn:  Decode a single SH-2 instruction.  Implements
 * translate_insn() using the shared decoder core.
 *
 * [Parameters]
 *         fetch: Pointer to instruction to decode
 *         state: SH-2 processor state
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *     recursing: Nonzero if this is a recursive decode, else zero
 *       is_last: Nonzero if this is the last instruction of a recursive
 *                   decode, else zero
 * [Return value]
 *     Decoded SH-2 opcode (nonzero) on success, zero on error
 * [Notes]
 *     Since the opcode 0x0000 is invalid and scan_block() prevents invalid
 *     opcodes from being included in a block to be translated, the return
 *     value of this routine will always be nonzero on success.
 */

#define DECODE_INSN_INLINE  inline
#define DECODE_INSN_PARAMS \
    const uint16_t *fetch, SH2State *state, JitEntry *entry, \
    const unsigned int state_reg, const int recursing, const int is_last

#ifdef TRACE
# define TRACE_WRITEBACK_FOR_RECURSIVE_DECODE \
    ASSERT(writeback_state_cache(entry, state_reg, 1))
#else
# define TRACE_WRITEBACK_FOR_RECURSIVE_DECODE  /*nothing*/
#endif
#define RECURSIVE_DECODE(address,is_last)  do { \
    const uint32_t saved_PC = jit_PC;           \
    jit_PC = (address);                         \
    if (UNLIKELY(!translate_insn(state, entry, state_reg, 1, (is_last)))) { \
        return 0;                               \
    }                                           \
    TRACE_WRITEBACK_FOR_RECURSIVE_DECODE;       \
    jit_PC = saved_PC;                          \
} while (0)

#include "sh2-core.i"

/*-----------------------------------------------------------------------*/

/**
 * translate_insn:  Translate a single SH-2 instruction at the current
 * translation address into one or more equivalent RTL instructions.
 *
 * [Parameters]
 *         state: SH-2 processor state
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *     recursing: Nonzero if this is a recursive decode (for inserting
 *                   copies of instructions from other locations), else zero
 *       is_last: Nonzero if this is the last instruction in a recursively
 *                   decoded sequence, else zero
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int translate_insn(SH2State *state, JitEntry *entry,
                          unsigned int state_reg, int recursing, int is_last)
{
    /* Get a fetch pointer for the current PC */
    const uint16_t *fetch_base = (const uint16_t *)fetch_pages[cur_PC >> 19];
    PRECOND(fetch_base != NULL, return 0);
    const uint16_t *fetch = (const uint16_t *)((uintptr_t)fetch_base + cur_PC);

    /* Process the instruction */
    const unsigned int opcode =
        decode_insn(fetch, state, entry, state_reg, recursing, is_last);
    if (UNLIKELY(!opcode)) {
        return 0;
    }

    return 1;
}

/*************************************************************************/

#ifdef JIT_BRANCH_PREDICT_STATIC

/**
 * add_static_branch_terminator:  Add a terminator for a static branch when
 * using static branch prediction.  Code will be added to retrieve or look
 * up the target address and branch to it if possible.
 *
 * [Parameters]
 *         entry: Block being translated
 *     state_reg: RTL register holding state block pointer
 *       address: Branch target address (in SH-2 address space)
 *         index: entry->static_predict[] index to use for static prediction
 * [Return value]
 *     Nonzero on success, zero on failure
 */
static int add_static_branch_terminator(JitEntry *entry,
                                        unsigned int state_reg,
                                        uint32_t address, unsigned int index)
{
    CREATE_LABEL(label_not_predicted);
    CREATE_LABEL(label_return);

    /* Load the address of the BranchTargetInfo we're using */
    DEFINE_REG(bti);
    MOVEA(bti, &entry->static_predict[index]);

    /* Load relevant values early to mitigate load stalls (very MIPSy) */
    DEFINE_REG(predicted_entry);
    LOAD_PTR(predicted_entry, bti, offsetof(BranchTargetInfo,entry));
    DECLARE_REG(cycles);
    LOAD_STATE_ALLOC(cycles, cycles);
    DECLARE_REG(cycle_limit);
    LOAD_STATE_ALLOC(cycle_limit, cycle_limit);
    DEFINE_REG(native_target);
    LOAD_PTR(native_target, bti, offsetof(BranchTargetInfo,native_target));
    DEFINE_REG(can_continue);
    SLTS(can_continue, cycles, cycle_limit);

    /* If we already have a prediction, jump to it (or return if we're out
     * of cycles) */
    GOTO_IF_Z(label_not_predicted, predicted_entry);
    STORE_STATE_PTR(current_entry, predicted_entry);
    GOTO_IF_Z(label_return, can_continue);
    RETURN_TO(native_target);

    /* Look up the translated block for this address, then return.  For
     * simplicity (and to keep code size down), we don't try too hard to
     * optimize this case; we'll make up the time in future runs */
    DEFINE_LABEL(label_not_predicted);
    DEFINE_REG(addr_reg);
    MOVEI(addr_reg, address);
    DEFINE_REG(ptr_update_branch_target);
    MOVEA(ptr_update_branch_target, update_branch_target);
    DEFINE_REG(new_entry);
    CALL(new_entry, bti, addr_reg, ptr_update_branch_target);
    STORE_STATE_PTR(current_entry, new_entry);
    GOTO_IF_Z(label_return, new_entry);
    DEFINE_REG(new_target);
#ifdef JIT_DEBUG_INTERPRET_RTL
    LOAD_PTR(new_target, new_entry, offsetof(JitEntry,rtl));
#else
    LOAD_PTR(new_target, new_entry, offsetof(JitEntry,native_code));
#endif
    STORE_PTR(bti, new_target, offsetof(BranchTargetInfo,native_target));

    DEFINE_LABEL(label_return);
    RETURN();

    clear_state_cache(0);
    return 1;
}

#endif  // JIT_BRANCH_PREDICT_STATIC

/*************************************************************************/

#if JIT_BRANCH_PREDICTION_SLOTS > 0

/**
 * update_branch_target:  Find the translated block, if any, corresponding
 * to the given address and update the given branch target structure with
 * the found block.
 *
 * [Parameters]
 *         bti: BranchTargetInfo structure to update
 *     address: Branch target address
 * [Return value]
 *     Translated block found, or NULL if none was found
 */
static FASTCALL JitEntry *update_branch_target(BranchTargetInfo *bti,
                                               uint32_t address)
{
    JitEntry *new_entry = jit_find(address);
    if (new_entry) {
        if (bti->entry) {
            bti->next->prev = bti->prev;
            bti->prev->next = bti->next;
        }
        bti->target = address;
        bti->entry  = new_entry;
        bti->next   = new_entry->pred_ref_head.next;
        bti->prev   = &new_entry->pred_ref_head;
        bti->next->prev = bti; 
        bti->prev->next = bti; 
    }
    return new_entry;
}

#endif  // JIT_BRANCH_PREDICTION_SLOTS > 0

/*************************************************************************/
/************************ Other utility routines *************************/
/*************************************************************************/

/**
 * btcache_lookup:  Search the branch target cache for the given SH-2
 * address.
 *
 * [Parameters]
 *     address: SH-2 address to search for
 * [Return value]
 *     Corresponding RTL label, or zero if the address could not be found
 */
static uint32_t btcache_lookup(uint32_t address)
{
    /* Search backwards from the current instruction so we can handle short
     * loops quickly; note that btcache_index is now pointing to where the
     * _next_ instruction will go */
    const int current = (btcache_index + (lenof(btcache)-1)) % lenof(btcache);
    int index = current;
    do {
        if (btcache[index].sh2_address == address) {
            return btcache[index].rtl_label;
        }
        index--;
        if (UNLIKELY(index < 0)) {
            index = lenof(btcache) - 1;
        }
    } while (index != current);
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * record_unresolved_branch:  Record the given branch target and native
 * offset in an empty slot in the unresolved branch table.  If there are
 * no empty slots, purge the oldest (lowest native offset) entry.
 *
 * [Parameters]
 *            entry: Block being translated
 *       sh2_target: Branch target address in SH2 address space
 *     target_label: RTL label number to be defined at branch target
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int record_unresolved_branch(const JitEntry *entry, uint32_t sh2_target,
                                    unsigned int target_label)
{
    PRECOND(entry != NULL, return 0);

    int oldest = 0;
    int i;
    for (i = 0; i < lenof(unres_branches); i++) {
        if (unres_branches[i].sh2_target == 0) {
            oldest = i;
            break;
        } else if (unres_branches[i].target_label
                   < unres_branches[oldest].target_label) {
            oldest = i;
        }
    }
    if (UNLIKELY(unres_branches[oldest].sh2_target != 0)) {
#ifdef JIT_DEBUG
        DMSG("WARNING: Unresolved branch table full, dropping branch to PC"
             " 0x%08X (RTL label %u)", unres_branches[oldest].sh2_target,
             unres_branches[oldest].target_label);
#endif
        /* Add a JUMP fallback for this entry in the middle of the code
         * stream, since we have nowhere else to put it; use a temporary
         * label to branch past it in the normal code flow */
        DEFINE_REG(temp_reg);
        CREATE_LABEL(temp_label);
        GOTO_LABEL(temp_label);
        DEFINE_LABEL(unres_branches[oldest].target_label);
        RETURN();
        DEFINE_LABEL(temp_label);
    }
    unres_branches[oldest].sh2_target   = sh2_target;
    unres_branches[oldest].target_label = target_label;
    return 1;
}

/*************************************************************************/

/**
 * writeback_state_cache:  Write all cached state block values to memory.
 * Does nothing (and returns success) if OPTIMIZE_STATE_BLOCK is not
 * defined.
 *
 * [Parameters]
 *           entry: Block being translated
 *       state_reg: RTL register holding state block pointer
 *     flush_fixed: Nonzero to flush dirty fixed-register fields to memory
 * [Return value]
 *     Nonzero on success, zero on error
 */
static int writeback_state_cache(const JitEntry *entry, unsigned int state_reg,
                                 int flush_fixed)
{
    PRECOND(entry != NULL, return 0);

#ifdef OPTIMIZE_STATE_BLOCK

    if (dirty_SR_T) {
        const int index_SR = state_cache_index(offsetof(SH2State,SR));
        DECLARE_REG(old_SR);
        if (state_cache[index_SR].rtlreg) {
            old_SR = state_cache[index_SR].rtlreg;
        } else {
            ALLOC_REG(old_SR);
            LOAD_W(old_SR, state_reg, offsetof(SH2State,SR));
        }
        DECLARE_REG(new_SR);
        if (state_cache[index_SR].fixed) {
            new_SR = old_SR;
        } else {
            ALLOC_REG(new_SR);
        }
        BFINS(new_SR, old_SR, cached_SR_T, SR_T_SHIFT, 1);
        state_cache[index_SR].rtlreg = new_SR;
        state_dirty |= 1 << index_SR;
        dirty_SR_T = 0;
    }

    unsigned int index;
    for (index = 0; index < lenof(state_cache); index++) {
        if ((state_dirty & (1 << index))
         || (flush_fixed && state_cache[index].flush)
        ) {
            DECLARE_REG(rtlreg);
            if (index == state_cache_index(offsetof(SH2State,PC))
             && cached_PC != 0
            ) {
                ALLOC_REG(rtlreg);
                MOVEI(rtlreg, cached_PC);
                cached_PC = 0;
                state_cache[index].rtlreg = rtlreg;
                state_cache[index].offset = 0;
            } else if (state_cache[index].offset != 0) {
                if (state_cache[index].fixed) {
                    rtlreg = state_cache[index].rtlreg;
                } else {
                    ALLOC_REG(rtlreg);
                }
                ADDI(rtlreg, state_cache[index].rtlreg,
                     state_cache[index].offset);
                state_cache[index].rtlreg = rtlreg;
                if (index < 16) {
                    pointer_status[index].rtl_ptrreg = 0;
                }
                if (index == 15 && stack_pointer) {
                    ADDI(stack_pointer, stack_pointer,
                         state_cache[index].offset);
                }
                state_cache[index].offset = 0;
            } else {
                rtlreg = state_cache[index].rtlreg;
            }
            if (!state_cache[index].fixed || flush_fixed) {
                STORE_W(state_reg, rtlreg, state_cache_field(index));
                state_dirty &= ~(1 << index);
                if (index == state_cache_index(offsetof(SH2State,PC))) {
                    stored_PC = 0;
                }
# ifdef TRACE_STEALTH
                if (index < 23) {
                    APPEND(NOP, 0, 0xB0000000 | index<<16, 0, 0);
                }
# endif
            }
        }
    }

#endif  // OPTIMIZE_STATE_BLOCK

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * clear_state_cache:  Clear all cached state block registers.  Does
 * nothing if OPTIMIZE_STATE_BLOCK is not defined.
 *
 * [Parameters]
 *     clear_fixed: Nonzero to clear fixed registers from the cache as well,
 *                     zero to leave them in the cache
 * [Return value]
 *     None
 */
static void clear_state_cache(int clear_fixed)
{
#ifdef OPTIMIZE_STATE_BLOCK
    if (clear_fixed) {
# ifdef JIT_DEBUG
        if (UNLIKELY(state_dirty) || UNLIKELY(dirty_SR_T)) {
            DMSG("WARNING: Clearing dirty state fields! (dirty=%08X SR_T=%d)",
                 state_dirty, dirty_SR_T);
        }
# endif
        memset(state_cache, 0, sizeof(state_cache));
        state_dirty = 0;
    } else {
        unsigned int index;
# ifdef JIT_DEBUG
        uint32_t nonfixed = 0;
        for (index = 0; index < lenof(state_cache); index++) {
            if (!state_cache[index].fixed) {
                nonfixed |= 1<<index;
            }
        }
        if (UNLIKELY(state_dirty & nonfixed) || UNLIKELY(dirty_SR_T)) {
            DMSG("WARNING: Clearing dirty state fields! (dirty&nonfixed=%08X"
                 " SR_T=%d)", state_dirty & nonfixed, dirty_SR_T);
        }
# endif
        for (index = 0; index < lenof(state_cache); index++) {
            if (!state_cache[index].fixed) {
                memset(&state_cache[index], 0, sizeof(state_cache[index]));
                state_dirty &= ~(1<<index);
            }
        }
    }
    cached_SR_T = 0;
    dirty_SR_T = 0;
    cached_PC = 0;
    stored_PC = 0;
#endif  // OPTIMIZE_STATE_BLOCK
}

/*************************************************************************/

#endif  // ENABLE_JIT

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
