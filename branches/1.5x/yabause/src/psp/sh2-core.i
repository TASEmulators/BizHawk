/*  src/psp/sh2-core.i: SH-2 instruction decoding core for PSP
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

/*
 * This file is intended to be #included as part of a source file that
 * processes SH-2 instructions.  It defines the function:
 *     static inline unsigned int decode_insn(...)
 * which decodes one instruction, performs actions to implement the
 * instruction, and returns the instruction's opcode.  The parameter list
 * is set by the caller using
 *     #define DECODE_INSN_PARAMS ...
 * and should be "void" (without quotes) if no parameters are used.  The
 * caller may also #define DECODE_INSN_INLINE to any inline-related
 * keyword, including ALWAYS_INLINE or NOINLINE; if not defined, no such
 * keyword will be used.
 *
 * The following identifiers must be either defined as macros or passed to
 * decode_insn() as parameters:
 *     uint32_t initial_PC;  // Address of the first instruction in the
 *                           //    current block, if OPTIMIZE_IDLE is defined
 *     uint32_t state_reg;   // RTL register holding the state block pointer
 *     uint32_t cur_PC;      // Address of the instruction to be decoded
 *     uint16_t *fetch;      // Pointer to instruction to decode (or NULL if
 *                           //    direct access is not possible)
 *
 * Each instruction is decoded into a series of "micro-ops", which should
 * be defined as macros before this file is included.  See sh2.c or
 * sh2-interpret.c for the micro-ops used.
 *
 * Note that while the decoder generally attempts to maintain SSA (static
 * single assignment) form for all registers, optimization of state block
 * fields into long-lived registers requires that every path through
 * conditional code assign the same register to a given state block field.
 * Since a proper SSA implementation could require many SELECT operations
 * and temporary registers, leading to inefficient code (the optimization
 * of which would be too time-consuming), we intentionally break SSA form
 * in such cases, conditionally reassigning new values to state block
 * mirror registers.  Such cases are noted throughout the code.
 */

/*************************************************************************/

/* Local convenience macros for defining and loading SH-2 registers */

#define GET_REG(name,reg,field,mask)                \
    DECLARE_REG(name);                              \
    if ((REG_GETKNOWN((reg)) & (mask)) == (mask)) { \
        ALLOC_REG(name);                            \
        MOVEI(name, REG_GETVALUE((reg)) & (mask));  \
    } else {                                        \
        LOAD_STATE_ALLOC(name, field);              \
    }
#define GET_R0    GET_REG(R0,   REG_R(0),  R[0],   0xFFFFFFFF)
#define GET_R0_W  GET_REG(R0,   REG_R(0),  R[0],   0xFFFF)
#define GET_R0_B  GET_REG(R0,   REG_R(0),  R[0],   0xFF)
#define GET_R15   GET_REG(R15,  REG_R(15), R[15],  0xFFFFFFFF)
#define GET_Rn    GET_REG(Rn,   REG_R(n),  R[n],   0xFFFFFFFF)
#define GET_Rm    GET_REG(Rm,   REG_R(m),  R[m],   0xFFFFFFFF)
#define GET_Rm_W  GET_REG(Rm,   REG_R(m),  R[m],   0xFFFF)
#define GET_Rm_B  GET_REG(Rm,   REG_R(m),  R[m],   0xFF)
#define GET_GBR   GET_REG(GBR,  REG_GBR,   GBR,    0xFFFFFFFF)
/* These are generally unknown, so don't waste time on checking them */
#define GET_SR    FLUSH_STATE_SR_T(); \
                  DECLARE_REG(SR);   LOAD_STATE_ALLOC(SR,   SR)
#define GET_SR_T  DECLARE_REG(T);    LOAD_STATE_SR_T (T)
#define GET_VBR   DECLARE_REG(VBR);  LOAD_STATE_ALLOC(VBR,  VBR)
#define GET_MACH  DECLARE_REG(MACH); LOAD_STATE_ALLOC(MACH, MACH)
#define GET_MACL  DECLARE_REG(MACL); LOAD_STATE_ALLOC(MACL, MACL)
#define GET_PR    DECLARE_REG(PR);   LOAD_STATE_ALLOC(PR,   PR)

/* MACH/MACL may be overwritten in the same register by the MAC.[WL] insns,
 * so STS MAC uses these macros to force creation of a new register */
#define GET_MACH_COPY  DEFINE_REG(MACH); LOAD_STATE_COPY(MACH, MACH)
#define GET_MACL_COPY  DEFINE_REG(MACL); LOAD_STATE_COPY(MACL, MACL)

/* Versions used by load/store macros to retain the cached offset */
#define GET_REG_KEEPOFS(name,field) \
    DECLARE_REG(name); LOAD_STATE_ALLOC_KEEPOFS(name, field)
#define GET_R0_KEEPOFS   GET_REG_KEEPOFS(R0,  R[0])
#define GET_R15_KEEPOFS  GET_REG_KEEPOFS(R15, R[15])
#define GET_Rn_KEEPOFS   GET_REG_KEEPOFS(Rn,  R[n])
#define GET_Rm_KEEPOFS   GET_REG_KEEPOFS(Rm,  R[m])
#define GET_GBR_KEEPOFS  GET_REG_KEEPOFS(GBR, GBR)

/* Local convenience macro for moving a value from one SH-2 register to
 * another.  If the source uses a fixed RTL register (e.g. due to loop
 * optimization) but the destination does not, the destination will end up
 * sharing the source register, causing the destination's value to
 * improperly change when the source is modified.  Pass the relevant SH-2
 * register name (R[m], GBR, etc.) as the macro parameter. */

#define COPY_FROM_Rn(dest)                      \
    DECLARE_REG(value);                         \
    if (!STATE_CACHE_FIXED_REG_WRITABLE(dest)) { \
        STATE_CACHE_CLEAR_FIXED_REG(dest);      \
    }                                           \
    if (STATE_CACHE_FIXED_REG(R[n]) && !STATE_CACHE_FIXED_REG(dest)) { \
        ALLOC_REG(value);                       \
        LOAD_STATE_COPY(value, R[n]);           \
    } else {                                    \
        LOAD_STATE_ALLOC(value, R[n]);          \
    }                                           \
    if (offsetof(SH2State,SR) == offsetof(SH2State,dest)) { \
        RESET_STATE_SR_T();                     \
    }                                           \
    STORE_STATE(dest, value)

#define COPY_TO_Rn(src)                         \
    DECLARE_REG(value);                         \
    if (offsetof(SH2State,SR) == offsetof(SH2State,src)) { \
        FLUSH_STATE_SR_T();                     \
    }                                           \
    if (!STATE_CACHE_FIXED_REG_WRITABLE(R[n])) { \
        STATE_CACHE_CLEAR_FIXED_REG(R[n]);      \
    }                                           \
    if (STATE_CACHE_FIXED_REG(src) && !STATE_CACHE_FIXED_REG(R[n])) { \
        ALLOC_REG(value);                       \
        LOAD_STATE_COPY(value, src);            \
    } else {                                    \
        LOAD_STATE_ALLOC(value, src);           \
    }                                           \
    STORE_STATE(R[n], value)

/* Local convenience macro for creating result registers to be stored in a
 * state block field */

#define DEFINE_RESULT_REG(name,field)           \
    DECLARE_REG(name);                          \
    do {                                        \
        name = STATE_CACHE_FIXED_REG(field);    \
        if (name && !STATE_CACHE_FIXED_REG_WRITABLE(field)) { \
            STATE_CACHE_CLEAR_FIXED_REG(field); \
            name = 0;                           \
        }                                       \
        if (!name) {                            \
            ALLOC_REG(name);                    \
        }                                       \
    } while (0)

/* Local convenience macros for storing values to SH-2 registers */

#define SET_R0(reg)    STORE_STATE(R[0], (reg))
#define SET_R15(reg)   STORE_STATE(R[15],(reg))
#define SET_Rn(reg)    STORE_STATE(R[n], (reg))
#define SET_Rm(reg)    STORE_STATE(R[m], (reg))
#define SET_SR(reg)    RESET_STATE_SR_T(); STORE_STATE(SR, (reg))
#define SET_SR_T(reg)  STORE_STATE_SR_T ((reg))
#define SET_GBR(reg)   STORE_STATE(GBR,  (reg))
#define SET_VBR(reg)   STORE_STATE(VBR,  (reg))
#define SET_MACH(reg)  STORE_STATE(MACH, (reg))
#define SET_MACL(reg)  STORE_STATE(MACL, (reg))
#define SET_PR(reg)    STORE_STATE(PR,   (reg))
#define SET_PC(reg)    STORE_STATE(PC,   (reg))

/* Set PC to a known value */
#define SET_PC_KNOWN(value)  STORE_STATE_PC((value))

/* Local convenience macros for adding constants to SH-2 registers */

#define ADDI_R0(imm)         ADDI_STATE(R[0],  (imm), R0)
#define ADDI_R15(imm)        ADDI_STATE(R[15], (imm), R15)
#define ADDI_Rn(imm)         ADDI_STATE(R[n],  (imm), Rn)
#define ADDI_Rm(imm)         ADDI_STATE(R[m],  (imm), Rm)
#define ADDI_R0_NOREG(imm)   ADDI_STATE_NOREG(R[0],  (imm))
#define ADDI_R15_NOREG(imm)  ADDI_STATE_NOREG(R[15], (imm))
#define ADDI_Rn_NOREG(imm)   ADDI_STATE_NOREG(R[n],  (imm))
#define ADDI_Rm_NOREG(imm)   ADDI_STATE_NOREG(R[m],  (imm))

/* Local convenience macro for updating the cycle count */

#define ADD_CYCLES()  do {                              \
    if (cur_cycles > 0) {                               \
        ADDI_STATE_NOREG(cycles, cur_cycles);           \
        cur_cycles = 0;                                 \
    }                                                   \
} while (0)

/*-----------------------------------------------------------------------*/

/* Local convenience macros for loading or storing values with various
 * addressing modes */

/* Increment/decrement amounts */
#define INCDEC_B  1
#define INCDEC_W  2
#define INCDEC_L  4

/*----------------------------------*/

#define LOAD_Rm(size,dest)  LOAD_disp_Rm(size, dest, 0)

#define LOAD_disp_Rm(size,dest,offset)          \
    if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF) { \
        SH2_LOAD_ABS_##size(dest, REG_GETVALUE(REG_R(m)) + (offset)); \
    } else {                                    \
        SH2_LOAD_REG_##size(dest, m, (offset), 0); \
    }

/* Only treat R0 as an offset if it's "small" -- otherwise we might end up
 * treating a small value in Rm as a pointer */
#define LOAD_R0_Rm(size,dest)                   \
    if (REG_GETKNOWN(REG_R(0)) == 0xFFFFFFFF    \
     && REG_GETVALUE(REG_R(0)) + 0x8000 < 0x10000 \
    ) {                                         \
        SH2_LOAD_REG_##size(dest, m, REG_GETVALUE(REG_R(0)), 0); \
    } else {                                    \
        GET_R0_KEEPOFS;                         \
        GET_Rm_KEEPOFS;                         \
        DEFINE_REG(address);                    \
        ADD(address, R0, Rm);                   \
        const int32_t offset = STATE_CACHE_OFFSET(R[0]) \
                             + STATE_CACHE_OFFSET(R[m]); \
        DECLARE_REG(offset_addr);               \
        if (offset) {                           \
            ALLOC_REG(offset_addr);             \
            ADDI(offset_addr, address, offset); \
        } else {                                \
            offset_addr = address;              \
        }                                       \
        SH2_LOAD_##size(dest, offset_addr);     \
    }

#define LOAD_Rm_inc(size,dest)                  \
    if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF) { \
        SH2_LOAD_ABS_##size(dest, REG_GETVALUE(REG_R(m))); \
        ADDI_Rm_NOREG(INCDEC_##size);           \
        REG_SETVALUE(REG_R(m), REG_GETVALUE(REG_R(m)) + INCDEC_##size); \
    } else {                                    \
        SH2_LOAD_REG_##size(dest, m, 0, 1);     \
        REG_SETKNOWN(REG_R(m), 0);              \
    }

#define LOAD_Rn(size,dest)                      \
    if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) { \
        SH2_LOAD_ABS_##size(dest, REG_GETVALUE(REG_R(n))); \
    } else {                                    \
        SH2_LOAD_REG_##size(dest, n, 0, 0);     \
    }

#define LOAD_Rn_inc(size,dest)                  \
    if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) { \
        SH2_LOAD_ABS_##size(dest, REG_GETVALUE(REG_R(n))); \
        ADDI_Rn_NOREG(INCDEC_##size);           \
        REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) + INCDEC_##size); \
    } else {                                    \
        SH2_LOAD_REG_##size(dest, n, 0, 1);     \
        REG_SETKNOWN(REG_R(n), 0);              \
    }

#define LOAD_disp_GBR(size,dest,offset)         \
    GET_GBR_KEEPOFS;                            \
    const int32_t final_offset = STATE_CACHE_OFFSET(GBR) + (offset); \
    DECLARE_REG(address);                       \
    if (final_offset) {                         \
        ALLOC_REG(address);                     \
        ADDI(address, GBR, final_offset);       \
    } else {                                    \
        address = GBR;                          \
    }                                           \
    SH2_LOAD_##size(dest, address)

#define LOAD_R0_GBR(size,dest)                  \
    DECLARE_REG(address);                       \
    GET_GBR_KEEPOFS;                            \
    if (REG_GETKNOWN(REG_R(0)) == 0xFFFFFFFF) { \
        const int32_t offset = STATE_CACHE_OFFSET(GBR) \
                             + REG_GETVALUE(REG_R(0)); \
        if (offset) {                           \
            ALLOC_REG(address);                 \
            ADDI(address, GBR, offset);         \
        } else {                                \
            address = GBR;                      \
        }                                       \
    } else {                                    \
        GET_R0_KEEPOFS;                         \
        DEFINE_REG(temp);                       \
        ADD(temp, R0, GBR);                     \
        const int32_t offset = STATE_CACHE_OFFSET(GBR) \
                             + STATE_CACHE_OFFSET(R[0]); \
        if (offset) {                           \
            ALLOC_REG(address);                 \
            ADDI(address, temp, offset);        \
        } else {                                \
            address = temp;                     \
        }                                       \
    }                                           \
    SH2_LOAD_##size(dest, address)

/*----------------------------------*/

#define STORE_Rn(size,value)  STORE_disp_Rn(size, value, 0)

#define STORE_disp_Rn(size,value,offset)        \
    if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) { \
        SH2_STORE_ABS_##size(REG_GETVALUE(REG_R(n)) + (offset), value, \
                             PTR_ISLOCAL(n));   \
    } else {                                    \
        SH2_STORE_REG_##size(n, value, offset, 0); \
    }

#define STORE_dec_Rn(size,value)                \
    if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) { \
        ADDI_Rn_NOREG(-INCDEC_##size);          \
        REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) - INCDEC_##size); \
        SH2_STORE_ABS_##size(REG_GETVALUE(REG_R(n)), value, \
                             PTR_ISLOCAL(n));   \
    } else {                                    \
        REG_SETKNOWN(REG_R(n), 0);              \
        SH2_STORE_REG_##size(n, value, 0, 1);   \
    }

#define STORE_R0_Rn(size,value)                 \
    if (REG_GETKNOWN(REG_R(0)) == 0xFFFFFFFF    \
     && REG_GETVALUE(REG_R(0)) + 0x8000 < 0x10000 \
    ) {                                         \
        SH2_STORE_REG_##size(n, value, REG_GETVALUE(REG_R(0)), 0); \
    } else {                                    \
        GET_R0_KEEPOFS;                         \
        GET_Rn_KEEPOFS;                         \
        DEFINE_REG(address);                    \
        ADD(address, R0, Rn);                   \
        const int32_t offset = STATE_CACHE_OFFSET(R[0]) \
                             + STATE_CACHE_OFFSET(R[n]); \
        DECLARE_REG(offset_addr);               \
        if (offset) {                           \
            ALLOC_REG(offset_addr);             \
            ADDI(offset_addr, address, offset); \
        } else {                                \
            offset_addr = address;              \
        }                                       \
        SH2_STORE_##size(offset_addr, value);   \
    }

#define STORE_disp_Rm(size,value,offset)        \
    if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF) { \
        SH2_STORE_ABS_##size(REG_GETVALUE(REG_R(m)) + (offset), value, \
                             PTR_ISLOCAL(n));   \
    } else {                                    \
        SH2_STORE_REG_##size(m, value, offset, 0); \
    }

#define STORE_disp_GBR(size,value,offset)       \
    GET_GBR_KEEPOFS;                            \
    const int32_t final_offset = STATE_CACHE_OFFSET(GBR) + (offset); \
    DECLARE_REG(address);                       \
    if (final_offset) {                         \
        ALLOC_REG(address);                     \
        ADDI(address, GBR, final_offset);       \
    } else {                                    \
        address = GBR;                          \
    }                                           \
    SH2_STORE_##size(address, value)

/* @(R0,GBR) is only used in RMW instructions, so use the saved address */
#define STORE_SAVED_R0_GBR(size,value)          \
    SH2_STORE_##size(address, value)

/*-----------------------------------------------------------------------*/

/* Local convenience macro to take a specified exception (immediate value),
 * using the specified register as the return PC. */

#define TAKE_EXCEPTION(imm_index,PC_reg)  do {  \
    /* Push SR and PC */                        \
    GET_R15_KEEPOFS;                            \
    GET_SR;                                     \
    SH2_STORE_REG_L(15, SR, 0, 1);              \
    SH2_STORE_REG_L(15, PC_reg, 0, 1);          \
    if (REG_GETKNOWN(REG_R(15)) == 0xFFFFFFFF) { \
        REG_SETVALUE(REG_R(15), REG_GETVALUE(REG_R(15)) - 8); \
    } else {                                    \
        REG_SETKNOWN(REG_R(15), 0);             \
    }                                           \
    /* Call the exception vector */             \
    DEFINE_REG(target);                         \
    SH2_LOAD_ABS_L(target, (imm_index) << 2);   \
    SET_PC(target);                             \
    FLUSH_STATE_CACHE();                        \
    JUMP();                                     \
} while (0)

/*-----------------------------------------------------------------------*/

#ifdef OPTIMIZE_SHIFT_SEQUENCES

/*
 * Local macro to retrieve the next opcode for use with shift optimization;
 * the opcode is stored in the variable "next_opcode".  If the next opcode
 * in the instruction stream is a delayed branch which does not depend on
 * the value of the register being shifted (Rn of the current instruction)
 * or the T bit set by the shift (if a single-bit shift or rotate), then
 * the instruction in the branch's delay slot is retrieved instead.  If the
 * next opcode cannot be retrieved, either because we've reached the end of
 * the block or because we're interpreting, or if the instruction is not in
 * directly-accessible memory, zero is stored in next_opcode.
 */

# define GET_NEXT_OPCODE_FOR_SHIFT_CACHE \
    uint16_t next_opcode;                                       \
    if (fetch && INSN_IS_AVAILABLE(1)) {                        \
        next_opcode = fetch[1];                                 \
        if (INSN_IS_AVAILABLE(2)) {                             \
            int use_delay = 0;                                  \
            if ((next_opcode & 0xF0DF) == 0x0003                \
             || (next_opcode & 0xF0DF) == 0x400B) {             \
                /* BSRF/BRAF Rn, JSR/JMP @Rn */                 \
                use_delay = ((next_opcode>>8 & 0xF) != n);      \
            } else if ((next_opcode & 0xFD00) == 0x8D00) {      \
                /* BT/S, BF/S (allow only SHL[LR]#) */          \
                use_delay = ((opcode & 0xF00F) == 0x4008);      \
            } else if ((next_opcode & 0xF0DF) == 0x000B         \
                    || (next_opcode & 0xE000) == 0xA000) {      \
                /* RTS/RTE, BRA/BSR */                          \
                use_delay = 1;                                  \
            }                                                   \
            if (use_delay) {                                    \
                next_opcode = fetch[2];                         \
            }                                                   \
        }                                                       \
    } else {                                                    \
        next_opcode = 0;                                        \
    }

#endif  // OPTIMIZE_SHIFT_SEQUENCES

/*-----------------------------------------------------------------------*/

/*
 * Debugging macro to print a line the first time an instruction is seen
 * (enabled when DEBUG_DECODER_INSN_COVERAGE is defined by the including
 * file).  A full list of these can be generated with:
 *
 * grep -F 'DEBUG_PRINT_ONCE("' sh2-core.i | sed 's/^[^"]*"\([^"]*\).*$/\1/'
 */

#ifdef DEBUG_DECODER_INSN_COVERAGE
# define DEBUG_PRINT_ONCE(insn)  do {   \
    static int seen = 0;                \
    if (!seen) {                        \
        fprintf(stderr, "%s\n", insn);  \
        seen = 1;                       \
    }                                   \
} while (0)
#else
# define DEBUG_PRINT_ONCE(insn)  /*nothing*/
#endif

/*************************************************************************/

#ifndef DECODE_INSN_INLINE
# define DECODE_INSN_INLINE  /*nothing*/
#endif

/**
 * decode_insn:  Decode a single SH-2 instruction.  Implements
 * translate_insn() using the shared decoder core.
 *
 * [Parameters]
 *     Defined by including file
 * [Return value]
 *     Decoded SH-2 opcode, or value returned by micro-ops
 */
static DECODE_INSN_INLINE unsigned int decode_insn(DECODE_INSN_PARAMS)
{
    uint16_t opcode;

    /* Load the opcode to decode */

    if (fetch) {
        opcode = *fetch;
    } else {
        opcode = MappedMemoryReadWord(cur_PC);
    }

    /* Perform any early processing required */

    OPCODE_INIT(opcode);

    /* If tracing, trace the instruction (do this after OPCODE_INIT() so
     * the JIT branch target label gets added before the trace call) */

#ifdef TRACE
    DEFINE_REG(trace_PC);
    MOVEI(trace_PC, cur_PC);
    DEFINE_REG(trace_funcptr);
    MOVEA(trace_funcptr, trace_insn_callback);
    CALL_NORET(state_reg, trace_PC, trace_funcptr);
#endif

    /* Save and clear the delay flag (this needs to be done here, not at
     * the end of the previous iteration, so (1) the translator knows not
     * to interrupt the pair of instructions and (2) the delay flag can be
     * accessed by OPCODE_INIT() if needed) */

    const int in_delay = state->delay;
    state->delay = 0;

    /* Clear the just-branched flag on every instruction, so
     * fall-off-the-end is detected properly; we'll set it later if we
     * branch after this instruction */

    state->just_branched = 0;

    /* Record the number of cycles used by this instruction (any additional
     * cycles necessary are added by the individual opcode handlers) */

    int cur_cycles = 1;

    /**** The Big Honkin' Opcode Switch Begins Here ****/

    /* Extract these early for convenience */
    const unsigned int n = opcode>>8 & 0xF;
    const unsigned int m = opcode>>4 & 0xF;

    /* Note: Mnemonics listed here for some instructions differ slightly
     * from the official Hitachi specs.  Here, register Rn _always_ refers
     * to the register specified by bits 8-11 of the opcode, and register
     * Rm _always_ refers to the register specified by bits 4-7 of the
     * opcode, regardless of the register's role (source or destination). */

    switch (opcode>>12 & 0xF) {

      case 0x0: {
        switch (opcode & 0xFF) {

          // $0xx2
          case 0x02: {  // STC SR,Rn
            DEBUG_PRINT_ONCE("STC SR,Rn");
            COPY_TO_Rn(SR);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }
          case 0x12: {  // STC GBR,Rn
            DEBUG_PRINT_ONCE("STC GBR,Rn");
            COPY_TO_Rn(GBR);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_GBR));
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_GBR));
            PTR_CLEAR(n);
            break;
          }
          case 0x22: {  // STC VBR,Rn
            DEBUG_PRINT_ONCE("STC VBR,Rn");
            COPY_TO_Rn(VBR);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xx3
          case 0x03: {  // BSRF Rn
            DEBUG_PRINT_ONCE("BSRF Rn");
            DEFINE_RESULT_REG(ret_addr, PR);
            MOVEI(ret_addr, cur_PC + 4);
            SET_PR(ret_addr);
            if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                state->branch_type = SH2BRTYPE_STATIC;
                state->branch_target = (cur_PC + 4) + REG_GETVALUE(REG_R(n));
            } else {
                state->branch_type = SH2BRTYPE_DYNAMIC;
                GET_Rn;
                DEFINE_REG(target);
                ADD(target, ret_addr, Rn);
                state->branch_target_reg = target;
            }
            state->delay = 1;
            cur_cycles += 1;
            break;
          }
          case 0x23: {  // BRAF Rn
            DEBUG_PRINT_ONCE("BRAF Rn");
#ifdef OPTIMIZE_VARIABLE_SHIFTS
            if (fetch) {
                unsigned int Rshift, count_max, Rcount, type;
                const unsigned int num_insns = can_optimize_variable_shift(
                    fetch, cur_PC, &Rcount, &count_max, &Rshift, &type, NULL
                );
                if (num_insns) {
                    state->varshift_target_PC = cur_PC + 2*num_insns;
                    state->varshift_type      = type;
                    state->varshift_Rcount    = Rcount;
                    state->varshift_max       = count_max;
                    state->varshift_Rshift    = Rshift;
                }
            }
#endif
            if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                state->branch_type = SH2BRTYPE_STATIC;
                state->branch_target = (cur_PC + 4) + REG_GETVALUE(REG_R(n));
            } else {
                state->branch_type = SH2BRTYPE_DYNAMIC;
                GET_Rn;
                DEFINE_REG(target);
                ADDI(target, Rn, cur_PC + 4);
                state->branch_target_reg = target;
            }
            state->delay = 1;
            cur_cycles += 1;
            break;
          }

          // $0xx4
          case 0x04: case 0x14: case 0x24: case 0x34:
          case 0x44: case 0x54: case 0x64: case 0x74:
          case 0x84: case 0x94: case 0xA4: case 0xB4:
          case 0xC4: case 0xD4: case 0xE4: case 0xF4: {  // MOV.B Rm,@(R0,Rn)
            DEBUG_PRINT_ONCE("MOV.B Rm,@(R0,Rn)");
            GET_Rm_B;
            STORE_R0_Rn(B, Rm);
            break;
          }

          // $0xx5
          case 0x05: case 0x15: case 0x25: case 0x35:
          case 0x45: case 0x55: case 0x65: case 0x75:
          case 0x85: case 0x95: case 0xA5: case 0xB5:
          case 0xC5: case 0xD5: case 0xE5: case 0xF5: {  // MOV.W Rm,@(R0,Rn)
            DEBUG_PRINT_ONCE("MOV.W Rm,@(R0,Rn)");
            GET_Rm_W;
            STORE_R0_Rn(W, Rm);
            break;
          }

          // $0xx6
          case 0x06: case 0x16: case 0x26: case 0x36:
          case 0x46: case 0x56: case 0x66: case 0x76:
          case 0x86: case 0x96: case 0xA6: case 0xB6:
          case 0xC6: case 0xD6: case 0xE6: case 0xF6: {  // MOV.L Rm,@(R0,Rn)
            DEBUG_PRINT_ONCE("MOV.L Rm,@(R0,Rn)");
            GET_Rm;
            STORE_R0_Rn(L, Rm);
            break;
          }

          // $0xx7
          case 0x07: case 0x17: case 0x27: case 0x37:
          case 0x47: case 0x57: case 0x67: case 0x77:
          case 0x87: case 0x97: case 0xA7: case 0xB7:
          case 0xC7: case 0xD7: case 0xE7: case 0xF7: {  // MUL.L Rm,Rn
            DEBUG_PRINT_ONCE("MUL.L Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(result, MACL);
            MUL(result, Rn, Rm);
            SET_MACL(result);
            CLEAR_MAC_IS_ZERO();
            cur_cycles += 1;  // Minimum number of cycles
            break;
          }

          // $0xx8
          case 0x08: {  // CLRT
            DEBUG_PRINT_ONCE("CLRT");
            DEFINE_REG(zero);
            MOVEI(zero, 0);
            SET_SR_T(zero);
            break;
          }
          case 0x18: {  // SETT
            DEBUG_PRINT_ONCE("SETT");
            DEFINE_REG(one);
            MOVEI(one, 1);
            SET_SR_T(one);
            break;
          }
          case 0x28: {  // CLRMAC
            DEBUG_PRINT_ONCE("CLRMAC");
            DEFINE_RESULT_REG(MACH, MACH);
            MOVEI(MACH, 0);
            SET_MACH(MACH);
            DEFINE_RESULT_REG(MACL, MACL);
            MOVEI(MACL, 0);
            SET_MACL(MACL);
            SET_MAC_IS_ZERO();
            break;
          }

          // $0xx9
          case 0x09: {  // NOP
            DEBUG_PRINT_ONCE("NOP");
            /* No operation */
            break;
          }
          case 0x19: {  // DIV0U
            DEBUG_PRINT_ONCE("DIV0U");
            GET_SR;
            DEFINE_RESULT_REG(new_SR, SR);
            ANDI(new_SR, SR, ~(SR_T | SR_Q | SR_M));
            SET_SR(new_SR);
#ifdef OPTIMIZE_DIVISION
            int Rlo = -1, Rhi = -1, Rdiv = -1;  // Register numbers
            int div_bits = 0;  // Number of bits of division to perform
            int skip_first_rotcl = 0;  // Is the 1st ROTCL skipped? (see below)
            /* Don't try to optimize if the DIV0U is sitting in a
             * branch delay slot; also, only optimize when we can do
             * direct fetching (otherwise our read-aheads might have side
             * effects). */
            if (LIKELY(!in_delay) && fetch) {
                if ((fetch[1] & 0xF00F) == 0x3004
                 && (fetch[2] & 0xF0FF) == 0x4024
                ) {
                    /* If the value of Rlo is zero, the first ROTCL may be
                     * omitted (since neither Rlo or SR.T would change). */
#ifdef __GNUC__  // Avoid a warning if known value optimization is disabled
                    __attribute__((unused))
#endif
                    const int rotcl_Rn = fetch[2]>>8 & 0xF;
                    if (REG_GETKNOWN(REG_R(rotcl_Rn)) == 0xFFFFFFFF
                     && REG_GETVALUE(REG_R(rotcl_Rn)) == 0
                    ) {
                        skip_first_rotcl = 1;
                    }
                }
                div_bits = can_optimize_div0u(fetch, cur_PC, skip_first_rotcl,
                                              &Rhi, &Rlo, &Rdiv);
            }
            if (div_bits > 0) {
# ifdef JIT_DEBUG_VERBOSE
                DMSG("Optimizing unsigned division at 0x%08X", (int)cur_PC);
# endif
                /* Set up optimization flags/data */
                const int is_32bit = (REG_GETKNOWN(Rhi) == 0xFFFFFFFF
                                      && REG_GETVALUE(Rhi) == 0);
                const int safe_division =
                    (optimization_flags & SH2_OPTIMIZE_ASSUME_SAFE_DIVISION);
                state->division_target_PC = cur_PC + div_bits*4;
                if (!safe_division) {
                    /* Flush cached data since we jump out from inside the
                     * runtime conditional. */
                    ADD_CYCLES();
                    FLUSH_STATE_CACHE();
                } else {
                    /* No need to flush data because we omit the
                     * unoptimized code entirely. */
                }
                /* Load operands */
                DECLARE_REG(lo);
                LOAD_STATE_ALLOC(lo, R[Rlo]);
                DECLARE_REG(div);
                LOAD_STATE_ALLOC(div, R[Rdiv]);
                DECLARE_REG(hi);
                LOAD_STATE_ALLOC(hi, R[Rhi]);
                /* Define output registers (shared across all cases) */
                DEFINE_REG(quotient);
                DEFINE_REG(remainder);
                /* If the divide is less than 32 bits, save the low bits of
                 * the dividend.  We don't shift the dividend right yet
                 * because we have to check Rhi first; the divide will
                 * still misbehave if Rhi >= div. */
                DECLARE_REG(lo_lowbits);
                if (div_bits < 32) {
                    ALLOC_REG(lo_lowbits);
                    SLLI(lo_lowbits, lo, 32 - div_bits);
                } else {  // Not needed, but avoid a compiler warning
                    lo_lowbits = 0;
                }
                /* Allocate labels for use in non-optimized cases */
                CREATE_LABEL(div0u_not_safe);
                CREATE_LABEL(div0u_not_32bit);
                CREATE_LABEL(div0u_done);
                if (!safe_division) {
                    GOTO_IF_Z(div0u_not_safe, div);
                }
                if (!is_32bit) {
                    GOTO_IF_NZ(div0u_not_32bit, hi);
                }
                /* Divide 32 bits by 32 bits, unsigned */
                if (div_bits < 32) {
                    /* Note: SSA violation */
                    SRLI(lo, lo, 32 - div_bits);
                }
                DIVMODU(quotient, lo, div, remainder);
                if (!is_32bit) {
                    GOTO_LABEL(div0u_done);
                    DEFINE_LABEL(div0u_not_32bit);
                    if (!safe_division) {
                        DEFINE_REG(test_safe);
                        SLTU(test_safe, hi, div);
                        GOTO_IF_Z(div0u_not_safe, test_safe);
                    }
                    if (div_bits < 32) {
                        /* Note: SSA violations */
                        SRLI(lo, lo, 32 - div_bits);
                        BFINS(lo, lo, hi, div_bits, 32 - div_bits);
                        SRLI(hi, hi, 32 - div_bits);
                    }
                    /*
                     * Divide 64 bits by 32 bits, unsigned, when the
                     * quotient is known to fit in 32 bits.
                     *
                     * Here, we use the algorithm followed by GCC (among
                     * others), which is essentially to perform long
                     * division in base 2^16 with a normalized divisor,
                     * i.e. a divisor with the highest bit set.  We divide
                     * the two-"digit" divisor into the upper three
                     * "digits" of the dividend, then append the final
                     * "digit" to the remainder and divide again.  Since
                     * operands to a division instruction can only be 32
                     * bits, i.e. 2 "digits", we process the divisor one
                     * "digit" at a time.
                     *
                     * To demonstrate, this is how it would work if a word
                     * consisted of two decimal digits (i.e. 0-99):
                     *   _____
                     * 59)5023
                     *   -50    Step 1: 50/5 = 10 (q1), 10*5 = 50
                     *   ----
                     *      2   Step 2: Carry down the next digit
                     *          Step 3: Is the remainder less than 10*9?
                     *    +59   Step 3A: Yes, so add 59 and decrement q1
                     *   ---- (q1=9)
                     *     61   Step 3B: Is the remainder still less than 10*9?
                     *    +59   Step 3C: Yes, so add 59 and decrement q1 again
                     *   ---- (q1=8)        (since the divisor is normalized,
                     *    120                we will never need to add it more
                     *                       than twice to avoid borrowing)
                     *    -90   Step 4: Subtract 10*9
                     *   ----
                     *     30   (Repeat steps 1-4)
                     *    -30   Step 5: 30/5 = 6 (q0), 6*5 = 30
                     *    ----
                     *       3  Step 6: Carry down the next (last) digit
                     *          Step 7: Is the remainder less than 6*9?
                     *     +59  Step 7A: Yes, so add 59 and decrement q0
                     *    ---- (q0=5)
                     *      62  Step 7B: Is the remainder still less than 6*9?
                     *          Step 7C: No, so skip the add and decrement
                     *     -54  Step 8: Subtract 6*9
                     *    ----
                     *       8  Result: quotient 85, remainder 8
                     */
                    /* Normalize the divisor and dividend */
                    DEFINE_REG(norm_shift);
                    CLZ(norm_shift, div);
                    DEFINE_REG(hi_norm_temp1);
                    SLL(hi_norm_temp1, hi, norm_shift);
                    DEFINE_REG(imm_32);
                    MOVEI(imm_32, 32);
                    DEFINE_REG(norm_invshift);
                    SUB(norm_invshift, imm_32, norm_shift);
                    DEFINE_REG(hi_norm_temp2);
                    SRL(hi_norm_temp2, lo, norm_invshift);
                    DEFINE_REG(hi_norm_temp3);
                    OR(hi_norm_temp3, hi_norm_temp1, hi_norm_temp2);
                    /* Need to SELECT here in case norm_shift==0, because
                     * shifting by 32 bits (norm_invshift) is undefined */
                    DEFINE_REG(hi_norm);
                    SELECT(hi_norm, hi_norm_temp3, hi, norm_shift);
                    DEFINE_REG(lo_norm);
                    SLL(lo_norm, lo, norm_shift);
                    DEFINE_REG(div_norm);
                    SLL(div_norm, div, norm_shift);
                    /* Extract the upper and lower halfwords of the divisor */
                    DEFINE_REG(div1);
                    SRLI(div1, div_norm, 16);
                    DEFINE_REG(div0);
                    ANDI(div0, div_norm, 0xFFFF);
                    /* Divide the upper 48 bits (steps 1-4) */
                    DEFINE_REG(q1);
                    DEFINE_REG(mid_norm);
                    DIVMODU(q1, hi_norm, div1, mid_norm);       // Step 1
                    SLLI(mid_norm, mid_norm, 16);
                    DEFINE_REG(mid_norm_temp);
                    SRLI(mid_norm_temp, lo_norm, 16);
                    OR(mid_norm, mid_norm, mid_norm_temp);      // Step 2
                    DEFINE_REG(q1_div0);
                    MUL(q1_div0, q1, div0);
                    CREATE_LABEL(div0u_mid_done);
                    DEFINE_REG(mid_test1);
                    SLTU(mid_test1, mid_norm, q1_div0);         // Step 3
                    GOTO_IF_Z(div0u_mid_done, mid_test1); {
                        ADD(mid_norm, mid_norm, div_norm);      // Step 3A
                        SUBI(q1, q1, 1);
                        /* If mid_norm < div_norm, then we overflowed and
                         * have a virtual 1 in bit position 32, so mid_norm
                         * must be greater than q1_div0 */
                        DEFINE_REG(mid_test2a);
                        SLTU(mid_test2a, mid_norm, div_norm);   // Step 3B
                        DEFINE_REG(mid_test2b);
                        SLTU(mid_test2b, mid_norm, q1_div0);
                        GOTO_IF_NZ(div0u_mid_done, mid_test2a);
                        GOTO_IF_Z(div0u_mid_done, mid_test2b); {
                            ADD(mid_norm, mid_norm, div_norm);  // Step 3C
                            SUBI(q1, q1, 1);
                        }
                    } DEFINE_LABEL(div0u_mid_done);
                    SUB(mid_norm, mid_norm, q1_div0);           // Step 4
                    /* Divide the last 16 bits (steps 5-8) */
                    DEFINE_REG(q0);
                    DEFINE_REG(rem_norm);
                    DIVMODU(q0, mid_norm, div1, rem_norm);      // Step 5
                    SLLI(rem_norm, rem_norm, 16);
                    DEFINE_REG(rem_norm_temp);
                    ANDI(rem_norm_temp, lo_norm, 0xFFFF);
                    OR(rem_norm, rem_norm, rem_norm_temp);      // Step 6
                    DEFINE_REG(q0_div0);
                    MUL(q0_div0, q0, div0);
                    CREATE_LABEL(div0u_lo_done);
                    DEFINE_REG(lo_test1);
                    SLTU(lo_test1, rem_norm, q0_div0);          // Step 7
                    GOTO_IF_Z(div0u_lo_done, lo_test1); {
                        ADD(rem_norm, rem_norm, div_norm);      // Step 7A
                        SUBI(q0, q0, 1);
                        DEFINE_REG(lo_test2a);
                        SLTU(lo_test2a, rem_norm, div_norm);    // Step 7B
                        DEFINE_REG(lo_test2b);
                        SLTU(lo_test2b, rem_norm, q0_div0);
                        GOTO_IF_NZ(div0u_lo_done, lo_test2a);
                        GOTO_IF_Z(div0u_lo_done, lo_test2b); {
                            ADD(rem_norm, rem_norm, div_norm);  // Step 7C
                            SUBI(q0, q0, 1);
                        }
                    } DEFINE_LABEL(div0u_lo_done);
                    SUB(rem_norm, rem_norm, q0_div0);           // Step 8
                    /* Merge the two quotient halves */
                    BFINS(quotient, q0, q1, 16, 16);
                    /* Un-normalize the remainder */
                    SRL(remainder, rem_norm, norm_shift);
                    DEFINE_LABEL(div0u_done);
                }  // if (!is_32bit)
                /* Process the division result */
                DEFINE_REG(new_T);
                ANDI(new_T, quotient, 1);  // Low bit to T flag
                DEFINE_RESULT_REG(quo_out, R[Rlo]);
                if (div_bits < 32) {
                    DEFINE_REG(quo_temp);
                    SRLI(quo_temp, quotient, 1);
                    OR(quo_out, quo_temp, lo_lowbits);
                } else {
                    SRLI(quo_out, quotient, 1);
                }
                STORE_STATE(R[Rlo], quo_out);
                REG_SETKNOWN(REG_R(Rlo), 0);
                DEFINE_REG(temp0);
                SUB(temp0, remainder, div);
                DEFINE_REG(rem_out);
                SELECT(rem_out, remainder, temp0, new_T);
                STORE_STATE(R[Rhi], rem_out);
                REG_SETKNOWN(REG_R(Rhi), 0);
                DEFINE_REG(SR_temp);
                OR(SR_temp, new_SR, new_T);
                DEFINE_REG(new_Q);
                XORI(new_Q, new_T, 1);
                DEFINE_RESULT_REG(SR_out, SR);
                BFINS(SR_out, SR_temp, new_Q, SR_Q_SHIFT, 1);
                SET_SR(SR_out);
                /* Skip over the step-by-step emulation */
                const unsigned int skipped_insns =
                    div_bits*2 - (skip_first_rotcl ? 1 : 0);
                cur_cycles += skipped_insns;
                if (!safe_division) {
                    ADD_CYCLES();
                    SET_PC_KNOWN(cur_PC + (2 + skipped_insns*2));
                    state->branch_target = cur_PC + (2 + skipped_insns*2);
                    FLUSH_STATE_CACHE();
                    JUMP_STATIC();
                    DEFINE_LABEL(div0u_not_safe);
                } else {
                    INC_PC_BY(skipped_insns*2);
                }
            }  // if (div_bits > 0)
#endif  // OPTIMIZE_DIVISION
            break;
          }  // DIV0U
          case 0x29: {  // MOVT Rn
            DEBUG_PRINT_ONCE("MOVT Rn");
            GET_SR_T;
            SET_Rn(T);
            REG_SETKNOWN(REG_R(n), 0xFFFFFFFE);
            REG_SETVALUE(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xxA
          case 0x0A: {  // STS MACH,Rn
            DEBUG_PRINT_ONCE("STS MACH,Rn");
            if (STATE_CACHE_FIXED_REG_WRITABLE(R[n])) {
                COPY_TO_Rn(MACH);
            } else {
                /* Make sure Rn gets a copy of the register so it's not
                 * altered by subsequent MAC instructions */
                GET_MACH_COPY;
                SET_Rn(MACH);
            }
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }
          case 0x1A: {  // STS MACL,Rn
            DEBUG_PRINT_ONCE("STS MACL,Rn");
            if (STATE_CACHE_FIXED_REG_WRITABLE(R[n])) {
                COPY_TO_Rn(MACL);
            } else {
                GET_MACL_COPY;
                SET_Rn(MACL);
            }
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }
          case 0x2A: {  // STS PR,Rn
            DEBUG_PRINT_ONCE("STS PR,Rn");
            COPY_TO_Rn(PR);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xxB
          case 0x0B: {  // RTS
            DEBUG_PRINT_ONCE("RTS");
            /* We don't do anything if this is an RTS from a folded
             * subroutine, since that's handled by our caller. */
            if (!state->folding_subroutine) {
                state->branch_type = SH2BRTYPE_DYNAMIC;
                GET_PR;
                state->branch_target_reg = PR;
                state->delay = 1;
            }
            cur_cycles += 1;
            break;
          }
          case 0x1B: {  // SLEEP
            DEBUG_PRINT_ONCE("SLEEP");
            cur_cycles += 2;
            ADD_CYCLES();
            FLUSH_STATE_CACHE();
            DEFINE_REG(check_interrupts_funcptr);
            MOVEA(check_interrupts_funcptr, check_interrupts);
            DEFINE_REG(result);
            CALL(result, state_reg, 0, check_interrupts_funcptr);
            CREATE_LABEL(sleep_intr);
            GOTO_IF_NZ(sleep_intr, result); {
                DEFINE_REG(imm_1);
                MOVEI(imm_1, 1);
                STORE_STATE_B(asleep, imm_1);
                DEFINE_REG(cycle_limit);
                LOAD_STATE(cycle_limit, cycle_limit);
                STORE_STATE(cycles, cycle_limit);
            } DEFINE_LABEL(sleep_intr);
            RETURN();
            break;
          }
          case 0x2B: {  // RTE
            DEBUG_PRINT_ONCE("RTE");
            GET_R15_KEEPOFS;
            DEFINE_REG(new_PC);
            SH2_LOAD_REG_L(new_PC, 15, 0, 1);
            DEFINE_REG(new_SR);
            SH2_LOAD_REG_L(new_SR, 15, 0, 1);
            if (REG_GETKNOWN(REG_R(15)) == 0xFFFFFFFF) {
                REG_SETVALUE(REG_R(15), REG_GETVALUE(REG_R(15)) + 8);
            } else {
                REG_SETKNOWN(REG_R(15), 0);
            }
            REG_SETKNOWN(REG_R(15), REG_GETKNOWN(REG_R(15)) & 7);
            state->branch_type = SH2BRTYPE_RTE;
            state->branch_target_reg = new_PC;
            DEFINE_RESULT_REG(new_SR_3F3, SR);
            ANDI(new_SR_3F3, new_SR, 0x3F3);
            SET_SR(new_SR_3F3);
            DEFINE_REG(imm_1);
            MOVEI(imm_1, 1);
            state->delay = 1;
            cur_cycles += 3;
            break;
          }

          // $0xxC
          case 0x0C: case 0x1C: case 0x2C: case 0x3C:
          case 0x4C: case 0x5C: case 0x6C: case 0x7C:
          case 0x8C: case 0x9C: case 0xAC: case 0xBC:
          case 0xCC: case 0xDC: case 0xEC: case 0xFC: {  // MOV.B @(R0,Rm),Rn
            DEBUG_PRINT_ONCE("MOV.B @(R0,Rm),Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_R0_Rm(B, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xxD
          case 0x0D: case 0x1D: case 0x2D: case 0x3D:
          case 0x4D: case 0x5D: case 0x6D: case 0x7D:
          case 0x8D: case 0x9D: case 0xAD: case 0xBD:
          case 0xCD: case 0xDD: case 0xED: case 0xFD: {  // MOV.W @(R0,Rm),Rn
            DEBUG_PRINT_ONCE("MOV.W @(R0,Rm),Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_R0_Rm(W, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xxE
          case 0x0E: case 0x1E: case 0x2E: case 0x3E:
          case 0x4E: case 0x5E: case 0x6E: case 0x7E:
          case 0x8E: case 0x9E: case 0xAE: case 0xBE:
          case 0xCE: case 0xDE: case 0xEE: case 0xFE: {  // MOV.L @(R0,Rm),Rn
            DEBUG_PRINT_ONCE("MOV.L @(R0,Rm),Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_R0_Rm(L, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          // $0xxF
          case 0x0F: case 0x1F: case 0x2F: case 0x3F:
          case 0x4F: case 0x5F: case 0x6F: case 0x7F:
          case 0x8F: case 0x9F: case 0xAF: case 0xBF:
          case 0xCF: case 0xDF: case 0xEF: case 0xFF: {  // MAC.L @Rm+,@Rn+
            DEBUG_PRINT_ONCE("MAC.L @Rm+,@Rn+");

            /* Load the values */
            DEFINE_REG(value_m);
            LOAD_Rm_inc(L, value_m);
            DEFINE_REG(value_n);
            LOAD_Rn_inc(L, value_n);

            /* Do the actual multiplication and addition */
            if (MAC_IS_ZERO()) {
                DEFINE_RESULT_REG(new_MACL, MACL);
                DEFINE_RESULT_REG(new_MACH, MACH);
                MULS_64(new_MACL, value_m, value_n, new_MACH);
                SET_MACL(new_MACL);
                SET_MACH(new_MACH);
            } else {
                GET_MACL;
                GET_MACH;
                MADDS_64(MACL, value_m, value_n, MACH);
                SET_MACL(MACL);
                SET_MACH(MACH);
            }

            /* Perform saturation if the S flag of SR is set */
            if (!CAN_OMIT_MAC_S_CHECK) {
                GET_SR;
                DEFINE_REG(saturate);
                ANDI(saturate, SR, SR_S);
                CREATE_LABEL(macl_nosat);
                GOTO_IF_Z(macl_nosat, saturate); {
                    GET_MACL;
                    GET_MACH;
                    DEFINE_REG(saturate_minus);
                    SLTSI(saturate_minus, MACH, -0x7FFF);
                    CREATE_LABEL(macl_sat_nominus);
                        GOTO_IF_Z(macl_sat_nominus, saturate_minus); {
                        DEFINE_REG(temp);
                        MOVEI(temp, -0x8000);
                        /* Note: SSA violations for state block optimization */
                        MOVE(MACH, temp);
                        SET_MACH(MACH);
                        MOVEI(MACL, 0);
                        SET_MACL(MACL);
                        GOTO_LABEL(macl_nosat);
                    } DEFINE_LABEL(macl_sat_nominus);
                    DEFINE_REG(satplus_test);
                    SUBI(satplus_test, MACH, 0x8000);
                    DEFINE_REG(inv_saturate_plus);  // Inverse value
                    SLTSI(inv_saturate_plus, satplus_test, 0);
                    GOTO_IF_NZ(macl_nosat, inv_saturate_plus); {
                        DEFINE_REG(temp1);
                        MOVEI(temp1, 0x7FFF);
                        /* Note: SSA violations for state block optimization */
                        MOVE(MACH, temp1);
                        SET_MACH(MACH);
                        DEFINE_REG(temp2);
                        MOVEI(temp2, 0xFFFFFFFF);
                        MOVE(MACL, temp2);
                        SET_MACL(MACL);
                    }
                } DEFINE_LABEL(macl_nosat);
            }  // if (!CAN_OMIT_MAC_S_CHECK)

            CLEAR_MAC_IS_ZERO();
            cur_cycles += 2;
            break;
          }  // MAC.L

          default:
            goto invalid;
        }
        break;
      }  // $0xxx

      case 0x1: {  // MOV.L Rm,@(disp,Rn)
        DEBUG_PRINT_ONCE("MOV.L Rm,@(disp,Rn)");
        const int disp = (opcode & 0xF) * 4;
        GET_Rm;
        STORE_disp_Rn(L, Rm, disp);
        break;
      }  // $1xxx

      case 0x2: {
        switch (opcode & 0xF) {

          case 0x0: {  // MOV.B Rm,@Rn
            DEBUG_PRINT_ONCE("MOV.B Rm,@Rn");
            GET_Rm_B;
            STORE_Rn(B, Rm);
            break;
          }

          case 0x1: {  // MOV.W Rm,@Rn
            DEBUG_PRINT_ONCE("MOV.W Rm,@Rn");
            GET_Rm_W;
            STORE_Rn(W, Rm);
            break;
          }

          case 0x2: {  // MOV.L Rm,@Rn
            DEBUG_PRINT_ONCE("MOV.L Rm,@Rn");
            GET_Rm;
            STORE_Rn(L, Rm);
            break;
          }

          case 0x4: {  // MOV.B Rm,@-Rn
            DEBUG_PRINT_ONCE("MOV.B Rm,@-Rn");
            GET_Rm_B;
            STORE_dec_Rn(B, Rm);
            break;
          }

          case 0x5: {  // MOV.W Rm,@-Rn
            DEBUG_PRINT_ONCE("MOV.W Rm,@-Rn");
            GET_Rm_W;
            STORE_dec_Rn(W, Rm);
            break;
          }

          case 0x6: {  // MOV.L Rm,@-Rn
            DEBUG_PRINT_ONCE("MOV.L Rm,@-Rn");
            GET_Rm;
            STORE_dec_Rn(L, Rm);
            break;
          }

          case 0x7: {  // DIV0S Rm,Rn
            DEBUG_PRINT_ONCE("DIV0S Rm,Rn");
            GET_Rm;
            DEFINE_REG(new_M);
            SRLI(new_M, Rm, 31);
            GET_SR;
            DEFINE_REG(SR_withM);
            BFINS(SR_withM, SR, new_M, SR_M_SHIFT, 1);
            GET_Rn;
            DEFINE_REG(new_Q);
            SRLI(new_Q, Rn, 31);
            DEFINE_REG(SR_withQ);
            BFINS(SR_withQ, SR_withM, new_Q, SR_Q_SHIFT, 1);
            DEFINE_REG(new_T);
            XOR(new_T, new_M, new_Q);
            DEFINE_RESULT_REG(new_SR, SR);
            BFINS(new_SR, SR_withQ, new_T, SR_T_SHIFT, 1);
            SET_SR(new_SR);
#ifdef OPTIMIZE_DIVISION
            int Rlo, Rhi = n, Rdiv = m;   // Register numbers
            const int can_optimize = !in_delay && fetch
                && can_optimize_div0s(fetch+1, cur_PC+2, Rhi, &Rlo, Rdiv);
            if (can_optimize) {
# ifdef JIT_DEBUG_VERBOSE
                DMSG("Optimizing signed division at 0x%08X", (int)cur_PC);
# endif
                const int safe_division =
                    (optimization_flags & SH2_OPTIMIZE_ASSUME_SAFE_DIVISION);
                state->division_target_PC = cur_PC + 128;
                if (!safe_division) {
                    /* Flush cached data since we jump out from inside the
                     * runtime conditional */
                    ADD_CYCLES();
                    FLUSH_STATE_CACHE();
                } else {
                    /* No need to flush data because we omit the
                     * unoptimzied code entirely */
                }
                /* Load operands */
                DECLARE_REG(lo);
                LOAD_STATE_ALLOC(lo, R[Rlo]);
                DECLARE_REG(div);
                LOAD_STATE_ALLOC(div, R[Rdiv]);
                DECLARE_REG(hi);
                LOAD_STATE_ALLOC(hi, R[Rhi]);
                /* Define output registers (shared across all cases) */
                DEFINE_REG(quotient);
                DEFINE_REG(remainder);
                /* Allocate a label for use in non-optimized cases */
                CREATE_LABEL(div0s_not_safe);
                if (!safe_division) {
                    /* Make sure the divisor is nonzero (we don't handle
                     * division by zero here because we'd have to carry lo
                     * down too far) */
                    GOTO_IF_Z(div0s_not_safe, div);
                    /* Check that the 64-bit value is within the range of
                     * a signed 32-bit integer.  The Hitachi docs suggest
                     * that 64/32 (or 32/16) signed division isn't
                     * supported, so don't bother trying to optimize that
                     * case.  Besides, 64/32 division is a pain when you
                     * only have 32-bit registers (see DIV0U). */
                    DEFINE_REG(test_safe);
                    SRAI(test_safe, lo, 31);  // Either 0 or -1
                    GOTO_IF_NE(div0s_not_safe, hi, test_safe);
                }
                /* Divide 32 bits by 32 bits, unsigned */
                DIVMODS(quotient, lo, div, remainder);
                /* Process the division result depending on the dividend sign*/
                DEFINE_REG(M_bit);
                SRLI(M_bit, div, 31);   // Get divisor sign bit (M)
                DEFINE_REG(lo_sign);
                SRLI(lo_sign, lo, 31);  // Get dividend sign bit
                /* Note: SSA violations for state block optimization */
                DEFINE_RESULT_REG(quo_out, R[Rlo]);
                DEFINE_RESULT_REG(rem_out, R[Rhi]);
                DEFINE_RESULT_REG(final_SR, SR);
                CREATE_LABEL(div0s_final);
                CREATE_LABEL(div0s_neg_dividend);
                GOTO_IF_NZ(div0s_neg_dividend, lo_sign); {
                    /* Positive dividend */
                    DEFINE_REG(quo_adj);
                    SUB(quo_adj, quotient, M_bit); // Adjust quotient
                    DEFINE_REG(new_T2);
                    ANDI(new_T2, quo_adj, 1);      // Get value of T
                    DEFINE_REG(new_SR_T);
                    BFINS(new_SR_T, new_SR, new_T2, SR_T_SHIFT, 1);
                    SRAI(quo_out, quo_adj, 1);     // Set quotient>>1
                    DEFINE_REG(temp_Q2);
                    XOR(temp_Q2, new_T2, M_bit);   // Get value of Q
                    DEFINE_REG(new_Q2);
                    XORI(new_Q2, temp_Q2, 1);
                    BFINS(final_SR, new_SR_T, new_Q2, SR_Q_SHIFT, 1);
                    SET_SR(final_SR);
                    DEFINE_REG(zero);
                    MOVEI(zero, 0);
                    DEFINE_REG(neg_div);
                    SUB(neg_div, zero, div);       // Get abs(divisor)
                    DEFINE_REG(abs_div);           // (remember that M bit is
                    SELECT(abs_div, neg_div, div, M_bit);  // divisor sign)
                    DEFINE_REG(rem_adj);
                    SUB(rem_adj, remainder, abs_div); // Adjust remainder
                    SELECT(rem_out, rem_adj, remainder, new_Q2);
                    GOTO_LABEL(div0s_final);
                } DEFINE_LABEL(div0s_neg_dividend); {
                    /* Negative dividend */
                    DEFINE_REG(M_adjtemp);
                    SLLI(M_adjtemp, M_bit, 1);     // Adjust quotient
                    DEFINE_REG(M_adjtemp2);
                    SUBI(M_adjtemp2, M_adjtemp, 1);
                    DEFINE_REG(zero);
                    MOVEI(zero, 0);
                    DEFINE_REG(M_adj);
                    SELECT(M_adj, M_adjtemp2, zero, remainder);
                    DEFINE_REG(qtemp);
                    ADD(qtemp, quotient, M_adj);
                    DEFINE_REG(quo_adj);
                    SUB(quo_adj, qtemp, M_bit);
                    DEFINE_REG(new_T2);
                    ANDI(new_T2, quo_adj, 1);      // Get value of T
                    DEFINE_REG(new_SR_T);
                    BFINS(new_SR_T, new_SR, new_T2, SR_T_SHIFT, 1);
                    SRAI(quo_out, quo_adj, 1);     // Set quotient>>1
                    DEFINE_REG(temp_Q2);
                    XOR(temp_Q2, new_T2, M_bit);   // Get value of Q
                    DEFINE_REG(new_Q2);
                    XORI(new_Q2, temp_Q2, 1);
                    BFINS(final_SR, new_SR_T, new_Q2, SR_Q_SHIFT, 1);
                    SET_SR(final_SR);
                    DEFINE_REG(neg_div);
                    SUB(neg_div, zero, div);       // Get abs(divisor)
                    DEFINE_REG(abs_div);
                    SELECT(abs_div, neg_div, div, M_bit);
                    DEFINE_REG(rem_Q_temp);
                    SUB(rem_Q_temp, remainder, abs_div);
                    DEFINE_REG(rem_Q);             // Remainder if Q
                    SELECT(rem_Q, remainder, rem_Q_temp, remainder);
                    DEFINE_REG(rem_nQ_temp);
                    ADD(rem_nQ_temp, remainder, abs_div);
                    DEFINE_REG(rem_nQ);            // Remainder if !Q
                    SELECT(rem_nQ, rem_nQ_temp, remainder, remainder);
                    SELECT(rem_out, rem_Q, rem_nQ, new_Q2);
                } DEFINE_LABEL(div0s_final);
                STORE_STATE(R[Rlo], quo_out);
                REG_SETKNOWN(REG_R(Rlo), 0);
                STORE_STATE(R[Rhi], rem_out);
                REG_SETKNOWN(REG_R(Rhi), 0);
                /* Skip over the step-by-step emulation */
                cur_cycles += 64;  // 1 cycle was already added
                if (!safe_division) {
                    ADD_CYCLES();
                    SET_PC_KNOWN(cur_PC + 130);
                    state->branch_target = cur_PC + 130;
                    FLUSH_STATE_CACHE();
                    JUMP_STATIC();
                    DEFINE_LABEL(div0s_not_safe);
                } else {
                    INC_PC_BY(128);  // Incremented by another 2 below
                }
            }  // if (can_optimize)
#endif  // OPTIMIZE_DIVISION
            break;
          }  // DIV0S

          case 0x8: {  // TST Rm,Rn
            DEBUG_PRINT_ONCE("TST Rm,Rn");
            GET_Rn;
            /* TST Rn,Rn is a common idiom for checking the zeroness of Rn */
            DEFINE_REG(new_T);
            if (m == n) {
                SEQZ(new_T, Rn);
            } else {
                GET_Rm;
                DEFINE_REG(result);
                AND(result, Rn, Rm);
                SEQZ(new_T, result);
            }
            SET_SR_T(new_T);
            break;
          }

          case 0x9: {  // AND Rm,Rn
            DEBUG_PRINT_ONCE("AND Rm,Rn");
            if (m == n) {
                break;  // AND Rn,Rn is a no-op
            }
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            AND(result, Rn, Rm);
            SET_Rn(result);
            /* A bit in the result of an AND operation is known iff (1) the
             * bit is known in both operands or (2) the bit is known to be
             * 0 in one operand. */
            REG_SETKNOWN(REG_R(n),
                         (REG_GETKNOWN(REG_R(m)) & REG_GETKNOWN(REG_R(n)))
                       | (REG_GETKNOWN(REG_R(m)) & ~REG_GETVALUE(REG_R(m)))
                       | (REG_GETKNOWN(REG_R(n)) & ~REG_GETVALUE(REG_R(n))));
            REG_SETVALUE(REG_R(n),
                         REG_GETVALUE(REG_R(m)) & REG_GETVALUE(REG_R(n)));
            PTR_CLEAR(n);
            break;
          }

          case 0xA: {  // XOR Rm,Rn
            DEBUG_PRINT_ONCE("XOR Rm,Rn");
            if (m == n) {  // XOR Rn,Rn is a common idiom for clearing Rn
                DEFINE_RESULT_REG(result, R[n]);
                MOVEI(result, 0);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
            } else {
                GET_Rn;
                GET_Rm;
                DEFINE_RESULT_REG(result, R[n]);
                XOR(result, Rn, Rm);
                SET_Rn(result);
                /* A bit in the result of an XOR operation is known iff the
                 * bit is known in both operands. */
                REG_SETKNOWN(REG_R(n),
                             REG_GETKNOWN(REG_R(m)) & REG_GETKNOWN(REG_R(n)));
                REG_SETVALUE(REG_R(n),
                             REG_GETVALUE(REG_R(m)) ^ REG_GETVALUE(REG_R(n)));
            }
            PTR_CLEAR(n);
            break;
          }

          case 0xB: {  // OR Rm,Rn
            DEBUG_PRINT_ONCE("OR Rm,Rn");
            if (m == n) {
                break;  // OR Rn,Rn is a no-op
            }
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            OR(result, Rn, Rm);
            SET_Rn(result);
            /* A bit in the result of an OR operation is known iff (1) the
             * bit is known in both operands or (2) the bit is known to be
             * 1 in one operand. */
            REG_SETKNOWN(REG_R(n),
                         (REG_GETKNOWN(REG_R(m)) & REG_GETKNOWN(REG_R(n)))
                       | (REG_GETKNOWN(REG_R(m)) & REG_GETVALUE(REG_R(m)))
                       | (REG_GETKNOWN(REG_R(n)) & REG_GETVALUE(REG_R(n))));
            REG_SETVALUE(REG_R(n),
                         REG_GETVALUE(REG_R(m)) | REG_GETVALUE(REG_R(n)));
            PTR_CLEAR(n);
            break;
          }

          case 0xC: {  // CMP/ST Rm,Rn
            DEBUG_PRINT_ONCE("CMP/ST Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(temp);
            XOR(temp, Rn, Rm);
            DEFINE_REG(byte3);
            BFEXT(byte3, temp, 24, 8);
            DEFINE_REG(cmp3);
            SEQZ(cmp3, byte3);
            DEFINE_REG(byte2);
            BFEXT(byte2, temp, 16, 8);
            DEFINE_REG(cmp2);
            SEQZ(cmp2, byte2);
            DEFINE_REG(cmp23);
            OR(cmp23, cmp2, cmp3);
            DEFINE_REG(byte1);
            BFEXT(byte1, temp, 8, 8);
            DEFINE_REG(cmp1);
            SEQZ(cmp1, byte1);
            DEFINE_REG(byte0);
            ANDI(byte0, temp, 0xFF);
            DEFINE_REG(cmp0);
            SEQZ(cmp0, byte0);
            DEFINE_REG(cmp01);
            OR(cmp01, cmp0, cmp1);
            DEFINE_REG(new_T);
            OR(new_T, cmp01, cmp23);
            SET_SR_T(new_T);
            break;
          }

          case 0xD: {  // XTRCT Rm,Rn
            DEBUG_PRINT_ONCE("XTRCT Rm,Rn");
            GET_Rn;
            DEFINE_REG(shift_Rn);
            SRLI(shift_Rn, Rn, 16);
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            BFINS(result, shift_Rn, Rm, 16, 16);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(m)) << 16
                                 | REG_GETKNOWN(REG_R(n)) >> 16);
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(m)) << 16
                                 | REG_GETVALUE(REG_R(n)) >> 16);
            PTR_CLEAR(n);
            break;
          }

          case 0xE: {  // MULU.W Rm,Rn
            DEBUG_PRINT_ONCE("MULU.W Rm,Rn");
            GET_Rn;
            DEFINE_REG(ext_Rn);
            ANDI(ext_Rn, Rn, 0xFFFF);
            GET_Rm;
            DEFINE_REG(ext_Rm);
            ANDI(ext_Rm, Rm, 0xFFFF);
            DEFINE_RESULT_REG(result, MACL);
            MUL(result, ext_Rn, ext_Rm);
            SET_MACL(result);
            CLEAR_MAC_IS_ZERO();
            break;
          }

          case 0xF: {  // MULS.W Rm,Rn
            DEBUG_PRINT_ONCE("MULS.W Rm,Rn");
            GET_Rn;
            DEFINE_REG(temp_Rn);
            SLLI(temp_Rn, Rn, 16);
            DEFINE_REG(ext_Rn);
            SRAI(ext_Rn, temp_Rn, 16);
            GET_Rm;
            DEFINE_REG(temp_Rm);
            SLLI(temp_Rm, Rm, 16);
            DEFINE_REG(ext_Rm);
            SRAI(ext_Rm, temp_Rm, 16);
            DEFINE_RESULT_REG(result, MACL);
            MUL(result, ext_Rn, ext_Rm);
            SET_MACL(result);
            CLEAR_MAC_IS_ZERO();
            break;
          }

          default:
            goto invalid;
        }
        break;
      }  // $2xxx

      case 0x3: {
        switch (opcode & 0xF) {

          case 0x0: {  // CMP/EQ Rm,Rn
            DEBUG_PRINT_ONCE("CMP/EQ Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(test);
            XOR(test, Rn, Rm);
            DEFINE_REG(new_T);
            SEQZ(new_T, test);
            SET_SR_T(new_T);
            break;
          }

          case 0x2: {  // CMP/HS Rm,Rn
            DEBUG_PRINT_ONCE("CMP/HS Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(test);
            SLTU(test, Rn, Rm);
            DEFINE_REG(new_T);
            XORI(new_T, test, 1);
            SET_SR_T(new_T);
            break;
          }

          case 0x3: {  // CMP/GE Rm,Rn
            DEBUG_PRINT_ONCE("CMP/GE Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(test);
            SLTS(test, Rn, Rm);
            DEFINE_REG(new_T);
            XORI(new_T, test, 1);
            SET_SR_T(new_T);
            break;
          }

          case 0x4: {  // DIV1 Rm,Rn
            DEBUG_PRINT_ONCE("DIV1 Rm,Rn");
            GET_Rn;
            DEFINE_REG(new_Q);
            SRLI(new_Q, Rn, 31);
            GET_SR;
            DEFINE_REG(old_Q);
            BFEXT(old_Q, SR, SR_Q_SHIFT, 1);
            DEFINE_REG(old_T);
            BFEXT(old_T, SR, SR_T_SHIFT, 1);
            DEFINE_REG(temp_Rn);
            SLLI(temp_Rn, Rn, 1);
            DEFINE_REG(new_Rn);
            OR(new_Rn, temp_Rn, old_T);
            DEFINE_REG(M_bit);
            BFEXT(M_bit, SR, SR_M_SHIFT, 1);
            DEFINE_REG(M_xor_Q);
            XOR(M_xor_Q, M_bit, old_Q);
            GET_Rm;
            DEFINE_REG(temp1);
            /* Note: SSA violation for state block optimization */
            DEFINE_RESULT_REG(final_Rn, R[n]);
            CREATE_LABEL(div1_MeqQ);
            CREATE_LABEL(div1_continue);
            GOTO_IF_Z(div1_MeqQ, M_xor_Q); {
                ADD(final_Rn, new_Rn, Rm);
                SLTU(temp1, final_Rn, new_Rn);
                GOTO_LABEL(div1_continue);
            } DEFINE_LABEL(div1_MeqQ); {
                SUB(final_Rn, new_Rn, Rm);
                SLTU(temp1, new_Rn, final_Rn);
            } DEFINE_LABEL(div1_continue);
            SET_Rn(final_Rn);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            DEFINE_REG(temp2);
            XOR(temp2, temp1, M_bit);
            DEFINE_REG(final_Q);
            XOR(final_Q, new_Q, temp2);
            DEFINE_REG(SR_withQ);
            BFINS(SR_withQ, SR, final_Q, SR_Q_SHIFT, 1);
            DEFINE_REG(new_T);
            XOR(new_T, final_Q, M_bit);
            DEFINE_REG(final_T);
            XORI(final_T, new_T, 1);
            DEFINE_RESULT_REG(final_SR, SR);
            BFINS(final_SR, SR_withQ, final_T, SR_T_SHIFT, 1);
            SET_SR(final_SR);
#ifdef OPTIMIZE_DIVISION
            if (cur_PC == state->division_target_PC) {
                /* Flush cached state block fields here so they don't get
                 * picked up on the optimized path */
                ADD_CYCLES();
                FLUSH_STATE_CACHE();
                state->division_target_PC = 0;
            }
#endif
            break;
          }  // DIV1 Rm,Rn

          case 0x5: {  // DMULU.L Rm,Rn
            DEBUG_PRINT_ONCE("DMULU.L Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(lo, MACL);
            DEFINE_RESULT_REG(hi, MACH);
            MULU_64(lo, Rn, Rm, hi);
            SET_MACL(lo);
            SET_MACH(hi);
            CLEAR_MAC_IS_ZERO();
            cur_cycles += 1;  // Minimum number of cycles
            break;
          }

          case 0x6: {  // CMP/HI Rm,Rn
            DEBUG_PRINT_ONCE("CMP/HI Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(new_T);
            SLTU(new_T, Rm, Rn);
            SET_SR_T(new_T);
            break;
          }

          case 0x7: {  // CMP/GT Rm,Rn
            DEBUG_PRINT_ONCE("CMP/GT Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_REG(new_T);
            SLTS(new_T, Rm, Rn);
            SET_SR_T(new_T);
            break;
          }

          case 0x8: {  // SUB Rm,Rn
            DEBUG_PRINT_ONCE("SUB Rm,Rn");
            if (m == n) {  // SUB Rn,Rn is an alternate way to clear Rn
                DEFINE_RESULT_REG(result, R[n]);
                MOVEI(result, 0);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
                PTR_CLEAR(n);
            } else if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF) {
                /* Optimize subtraction a constants */
                const int32_t Rm_value = REG_GETVALUE(REG_R(m));
                ADDI_Rn_NOREG(-Rm_value);
                if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                    REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) - Rm_value);
                } else {
                    REG_SETKNOWN(REG_R(n), 0);
                }
                if (PTR_CHECK(m)) {
                    /* Subtracting a pointer from anything does not leave
                     * a valid pointer. */
                    PTR_CLEAR(n);
                }
            } else {
                GET_Rn;
                GET_Rm;
                DEFINE_RESULT_REG(result, R[n]);
                SUB(result, Rn, Rm);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0);
                PTR_CLEAR(n);
            }
            break;
          }

          case 0xA: {  // SUBC Rm,Rn
            DEBUG_PRINT_ONCE("SUBC Rm,Rn");
            GET_SR_T;
            if (m == n) {  // SUBC Rn,Rn sets Rn to -(SR.T), and is
                           // commonly used in signed division
                DEFINE_REG(zero);
                MOVEI(zero, 0);
                DEFINE_RESULT_REG(result, R[n]);
                SUB(result, zero, T);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0);
                PTR_CLEAR(n);
                /* T is unchanged */
            } else {
                GET_Rn;
                GET_Rm;
                DEFINE_REG(temp);
                SUB(temp, Rn, Rm);
                DEFINE_REG(borrow1);
                SLTU(borrow1, Rn, temp);
                DEFINE_RESULT_REG(result, R[n]);
                SUB(result, temp, T);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0);
                PTR_CLEAR(n);
                DEFINE_REG(borrow2);
                SLTU(borrow2, temp, result);
                DEFINE_REG(new_T);
                OR(new_T, borrow1, borrow2);
                SET_SR_T(new_T);
            }
            break;
          }

          case 0xB: {  // SUBV Rm,Rn
            DEBUG_PRINT_ONCE("SUBV Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            SUB(result, Rn, Rm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            DEFINE_REG(temp0);
            XOR(temp0, Rn, Rm);
            DEFINE_REG(temp1);
            XOR(temp1, Rn, result);
            DEFINE_REG(temp2);
            AND(temp2, temp0, temp1);
            DEFINE_REG(new_T);
            SRLI(new_T, temp2, 31);
            SET_SR_T(new_T);
            break;
          }

          case 0xC: {  // ADD Rm,Rn
            DEBUG_PRINT_ONCE("ADD Rm,Rn");
            if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF) {
                /* Optimize addition of a constant */
                const int32_t Rm_value = REG_GETVALUE(REG_R(m));
                ADDI_Rn_NOREG(Rm_value);
                if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                    REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) + Rm_value);
                } else {
                    REG_SETKNOWN(REG_R(n), 0);
                }
                if (PTR_CHECK(m)) {
                    if (PTR_CHECK(n)) {
                        PTR_CLEAR(n);
                    } else {
                        PTR_COPY(m, n, 1);
                    }
                }
            } else if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                /* Treat this like MOV Rm,Rn; ADD old_Rn,Rn and optimize
                 * as above */
                const int32_t old_Rn_value = REG_GETVALUE(REG_R(n));
                const int old_PTR_CHECK = PTR_CHECK(n);
                COPY_TO_Rn(R[m]);
                PTR_COPY(m, n, 0);
                ADDI_Rn_NOREG(old_Rn_value);
                REG_SETKNOWN(REG_R(n), 0);
                if (old_PTR_CHECK) {
                    /* We lost the old pointer data in the PTR_COPY() above,
                     * so just give up and mark it as not a pointer.
                     * Hopefully this case won't be too common. */
                    PTR_CLEAR(n);
                }
            } else {  // Neither operand is a known constant
                GET_Rn;
                GET_Rm;
                DEFINE_RESULT_REG(result, R[n]);
                ADD(result, Rn, Rm);
                SET_Rn(result);
                REG_SETKNOWN(REG_R(n), 0);
                if (PTR_CHECK(m)) {
                    if (PTR_CHECK(n)) {
                        /* Adding a pointer to a pointer is invalid, so
                         * clear pointer info for the result register. */
                        PTR_CLEAR(n);
                    } else {
                        /* ADD Rptr,Rn is equivalent to "MOV Rptr,Rn"
                         * followed by "ADD old_value_of_Rn,Rn", which we
                         * treat as a pointer result. */
                        PTR_COPY(m, n, 1);
                    }
                }
            }
            break;
          }

          case 0xD: {  // DMULS.L Rm,Rn
            DEBUG_PRINT_ONCE("DMULS.L Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(lo, MACL);
            DEFINE_RESULT_REG(hi, MACH);
            MULS_64(lo, Rn, Rm, hi);
            SET_MACL(lo);
            SET_MACH(hi);
            CLEAR_MAC_IS_ZERO();
            cur_cycles += 1;  // Minimum number of cycles
            break;
          }

          case 0xE: {  // ADDC Rm,Rn
            DEBUG_PRINT_ONCE("ADDC Rm,Rn");
            GET_Rn;
            GET_Rm;
            GET_SR_T;
            DEFINE_REG(temp);
            ADD(temp, Rn, Rm);
            DEFINE_REG(carry1);
            SLTU(carry1, temp, Rn);
            DEFINE_RESULT_REG(result, R[n]);
            ADD(result, temp, T);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            DEFINE_REG(carry2);
            SLTU(carry2, result, temp);
            DEFINE_REG(new_T);
            OR(new_T, carry1, carry2);
            SET_SR_T(new_T);
            break;
          }

          case 0xF: {  // ADDV Rm,Rn
            DEBUG_PRINT_ONCE("ADDV Rm,Rn");
            GET_Rn;
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            ADD(result, Rn, Rm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            DEFINE_REG(temp0);
            XOR(temp0, Rn, Rm);
            DEFINE_REG(temp1);
            XORI(temp1, temp0, 1);
            DEFINE_REG(temp2);
            XOR(temp2, Rn, result);
            DEFINE_REG(temp3);
            AND(temp3, temp1, temp2);
            DEFINE_REG(new_T);
            SRLI(new_T, temp3, 31);
            SET_SR_T(new_T);
            break;
          }

          default:
            goto invalid;
        }
        break;
      }  // $3xxx

      case 0x4: {
        switch (opcode & 0xFF) {

          // $4xx0
          case 0x00:    // SHLL Rn
          case 0x20: {  // SHAL Rn
            DEBUG_PRINT_ONCE("SH[AL]L Rn");
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                /*
                 * If the next instruction is either SHLL or SHAL on the
                 * same register, we can cache the 1-bit shift from this
                 * instruction and combine it with the next instruction.
                 * (We can't do so with multi-bit shifts after a single-bit
                 * shift, because the multi-bit shifts don't set the T bit
                 * in SR.)  If the next instruction is a delayed branch and
                 * does not reference the shifted register, we check the
                 * delay slot as well.
                 *
                 * If _this_ instruction is in a delay slot, we don't try
                 * to optimize at all because the next instruction executed
                 * may not be the next one in the code stream.  Note that
                 * we check state->branch_type rather than state->delay
                 * because the latter is cleared at the top of this routine.
                 */
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if ((next_opcode & 0xFFDF) == (opcode & 0xFFDF)) {
                    if (CACHED_SHIFT_COUNT() < 32) {
                        ADD_TO_SHIFT_CACHE(1);
                    }
                    break;
                }
            }
            const unsigned int shift_count = CACHED_SHIFT_COUNT() + 1;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int shift_count = 1;
#endif
            DEFINE_REG(new_T);
            DEFINE_RESULT_REG(result, R[n]);
            if (shift_count > 32) {
                /* Completely shifted out (including T bit) */
                MOVEI(new_T, 0);
                MOVEI(result, 0);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
            } else {
                GET_Rn;
                BFEXT(new_T, Rn, 32 - shift_count, 1);
                if (shift_count == 32) {
                    MOVEI(result, 0);
                    REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                    REG_SETVALUE(REG_R(n), 0);
                } else {
                    SLLI(result, Rn, shift_count);
                    REG_SETKNOWN(REG_R(n),
                                 REG_GETKNOWN(REG_R(n)) << shift_count
                                 | 0xFFFFFFFFU >> (32-shift_count));
                    REG_SETVALUE(REG_R(n),
                                 REG_GETVALUE(REG_R(n)) << shift_count);
                }
            }
            SET_SR_T(new_T);
            SET_Rn(result);
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }  // SHLL/SHAL
          case 0x10: {  // DT Rn
            DEBUG_PRINT_ONCE("DT Rn");
            GET_Rn;
            DEFINE_RESULT_REG(result, R[n]);
#ifdef OPTIMIZE_DELAY
            if (fetch && INSN_IS_AVAILABLE(1) && fetch[1]==0x8BFD){
                /* It's a DT/BF loop, so eat as many iterations as we can.
                 * At most, we consume enough iterations to push us over
                 * the cycle limit. */
# ifdef JIT_DEBUG_VERBOSE
                DMSG("Found DT/BF delay loop at 0x%08X", (int)cur_PC);
# endif
                if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF
                 && REG_GETVALUE(REG_R(n)) > 0  // Just in case
                 && REG_GETVALUE(REG_R(n)) <= OPTIMIZE_DELAY_OMIT_MAX
                ) {
# ifdef JIT_DEBUG_VERBOSE
                    DMSG("-- Known to be %u cycles, omitting loop",
                         REG_GETVALUE(REG_R(n)));
# endif
                    /* The iteration count is known and within the limit
                     * for omitting the loop code, so just clear the
                     * register and update the cycle count. */
                    cur_cycles += REG_GETVALUE(REG_R(n)) * 4;
                    MOVEI(result, 0);
                    SET_Rn(result);
                    REG_SETVALUE(REG_R(n), 0);
                    PTR_CLEAR(n);
                    DEFINE_REG(new_T);
                    MOVEI(new_T, 1);
                    SET_SR_T(new_T);
                    INC_PC_BY(2);  // Skip over the BF
                    break;
                }
                /* Calculate the maximum number of loop iterations we can
                 * consume with the remaining cycles, but always execute
                 * at least one iteration. */
                DEFINE_REG(imm_1);
                MOVEI(imm_1, 1);
                /* cycle_limit generally won't be preloaded here, and we
                 * don't want to carry a previous use too far (so as not to
                 * create an unnecessarily long-lived register for what's
                 * essentially a constant value), so we don't bother trying
                 * to reuse any previous register with LOAD_STATE_ALLOC().
                 * However, we _should_ make use of a fixed register if one
                 * is available. */
                DECLARE_REG(cycle_limit);
                cycle_limit = STATE_CACHE_FIXED_REG(cycle_limit);
                if (!cycle_limit) {
                    ALLOC_REG(cycle_limit);
                    LOAD_STATE(cycle_limit, cycle_limit);
                }
                DEFINE_REG(temp);
                ADDI(temp, cycle_limit, 4-1);  // Round the result up
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_REG(cycles_left);
                SUB(cycles_left, temp, cycles);
                DEFINE_REG(max_loops_temp);
                SRAI(max_loops_temp, cycles_left, 2);  // 4 cycles per loop
                DEFINE_REG(max_loops_test);
                SLTS(max_loops_test, max_loops_temp, imm_1);
                DEFINE_REG(max_loops);
                SELECT(max_loops, imm_1, max_loops_temp, max_loops_test);
                /* Don't consume more iterations than are actually
                 * available. */
                DEFINE_REG(test);
                SLTU(test, Rn, max_loops);
                DEFINE_REG(num_loops_temp);
                SELECT(num_loops_temp, Rn, max_loops, test);
                /* Protect against the case of Rn == 0.  That should never
                 * happen in practice (it would take over 10 minutes to
                 * complete the loop), but since we're optimizing out
                 * hundreds of cycles here, we can afford a single extra
                 * instruction to be safe. */
                DEFINE_REG(num_loops);
                SELECT(num_loops, num_loops_temp, max_loops, Rn);
                SUB(result, Rn, num_loops);
                /* Update the cycle counter; since the final loop will have
                 * its cycles added separately, we subtract one loop's
                 * worth of cycles before adding. */
                DEFINE_REG(used_cycles_temp);
                SLLI(used_cycles_temp, num_loops, 2);
                DEFINE_REG(used_cycles);
                SUBI(used_cycles, used_cycles_temp, 4);
                DEFINE_REG(final_cycles);
                ADD(final_cycles, cycles, used_cycles);
                STORE_STATE(cycles, final_cycles);
            } else  // Not optimizable
#endif  // OPTIMIZE_DELAY
            SUBI(result, Rn, 1);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            DEFINE_REG(new_T);
            SEQZ(new_T, result);
            SET_SR_T(new_T);
            break;
          }  // DT

          // $4xx1
          case 0x01: {  // SHLR Rn
            DEBUG_PRINT_ONCE("SHLR Rn");
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (next_opcode == opcode) {
                    if (CACHED_SHIFT_COUNT() < 32) {
                        ADD_TO_SHIFT_CACHE(1);
                    }
                    break;
                }
            }
            const unsigned int shift_count = CACHED_SHIFT_COUNT() + 1;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int shift_count = 1;
#endif
            DEFINE_REG(new_T);
            DEFINE_RESULT_REG(result, R[n]);
            if (shift_count > 32) {
                MOVEI(new_T, 0);
                MOVE(result, 0);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
            } else {
                GET_Rn;
                BFEXT(new_T, Rn, shift_count - 1, 1);
                if (shift_count == 32) {
                    MOVEI(result, 0);
                    REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                    REG_SETVALUE(REG_R(n), 0);
                } else {
                    SRLI(result, Rn, shift_count);
                    REG_SETKNOWN(REG_R(n),
                                 REG_GETKNOWN(REG_R(n)) >> shift_count
                                 | 0xFFFFFFFF << (32-shift_count));
                    REG_SETVALUE(REG_R(n),
                                 REG_GETVALUE(REG_R(n)) >> shift_count);
                }
            }
            SET_SR_T(new_T);
            SET_Rn(result);
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }  // SHLR Rn
          case 0x11: {  // CMP/PZ Rn
            DEBUG_PRINT_ONCE("CMP/PZ Rn");
            GET_Rn;
            DEFINE_REG(temp);
            SLTZ(temp, Rn);
            DEFINE_REG(new_T);
            XORI(new_T, temp, 1);
            SET_SR_T(new_T);
            break;
          }
          case 0x21: {  // SHAR Rn
            DEBUG_PRINT_ONCE("SHAR Rn");
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (next_opcode == opcode) {
                    if (CACHED_SHIFT_COUNT() < 32) {
                        ADD_TO_SHIFT_CACHE(1);
                    }
                    break;
                }
            }
            const unsigned int shift_count = CACHED_SHIFT_COUNT() + 1;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int shift_count = 1;
#endif
            GET_Rn;
            DEFINE_REG(new_T);
            DEFINE_RESULT_REG(result, R[n]);
            if (shift_count >= 32) {
                SRAI(result, Rn, 31);
                ANDI(new_T, result, 1);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n),
                             (int32_t)REG_GETVALUE(REG_R(n)) >> 31);
            } else {
                BFEXT(new_T, Rn, shift_count - 1, 1);
                SRAI(result, Rn, shift_count);
                REG_SETKNOWN(REG_R(n),
                             REG_GETKNOWN(REG_R(n)) >> shift_count
                             | 0xFFFFFFFF << (32-shift_count));
                REG_SETVALUE(REG_R(n),
                             (int32_t)REG_GETVALUE(REG_R(n))>>shift_count);
            }
            SET_SR_T(new_T);
            SET_Rn(result);
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }  // SHAR Rn

          // $4xx2
          case 0x02: {  // STS.L MACH,@-Rn
            DEBUG_PRINT_ONCE("STS.L MACH,@-Rn");
            GET_MACH_COPY;
            STORE_dec_Rn(L, MACH);
            break;
          }
          case 0x12: {  // STS.L MACL,@-Rn
            DEBUG_PRINT_ONCE("STS.L MACL,@-Rn");
            GET_MACL_COPY;
            STORE_dec_Rn(L, MACL);
            break;
          }
          case 0x22: {  // STS.L PR,@-Rn
            DEBUG_PRINT_ONCE("STS.L PR,@-Rn");
            GET_PR;
            STORE_dec_Rn(L, PR);
            break;
          }

          // $4xx3
          case 0x03: {  // STC.L SR,@-Rn
            DEBUG_PRINT_ONCE("STC.L SR,@-Rn");
            GET_SR;
            STORE_dec_Rn(L, SR);
            cur_cycles += 1;
            break;
          }
          case 0x13: {  // STC.L GBR,@-Rn
            DEBUG_PRINT_ONCE("STC.L GBR,@-Rn");
            GET_GBR;
            STORE_dec_Rn(L, GBR);
            cur_cycles += 1;
            break;
          }
          case 0x23: {  // STC.L VBR,@-Rn
            DEBUG_PRINT_ONCE("STC.L VBR,@-Rn");
            GET_VBR;
            STORE_dec_Rn(L, VBR);
            cur_cycles += 1;
            break;
          }

          // $4xx4
          case 0x04: {  // ROTL Rn
            DEBUG_PRINT_ONCE("ROTL Rn");
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (next_opcode == opcode) {
                    /* Don't worry about overflow; we only look at the
                     * lower 5 bits of the value */
                    ADD_TO_SHIFT_CACHE(1);
                    break;
                }
            }
            const unsigned int rotate_count = (CACHED_SHIFT_COUNT() + 1) & 31;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int rotate_count = 1;
#endif
            GET_Rn;
            DEFINE_REG(new_T);
            BFEXT(new_T, Rn, (32 - rotate_count) & 31, 1);
            SET_SR_T(new_T);
            DEFINE_RESULT_REG(result, R[n]);
            RORI(result, Rn, (32 - rotate_count) & 31);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n),
                         REG_GETKNOWN(REG_R(n)) << rotate_count
                         | REG_GETKNOWN(REG_R(n)) >> (32 - rotate_count));
            REG_SETVALUE(REG_R(n),
                         REG_GETVALUE(REG_R(n)) << rotate_count
                         | REG_GETVALUE(REG_R(n)) >> (32 - rotate_count));
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }
          case 0x24: {  // ROTCL Rn
            DEBUG_PRINT_ONCE("ROTCL Rn");
            GET_SR_T;
            GET_Rn;
            DEFINE_REG(new_T);
            SRLI(new_T, Rn, 31);
            SET_SR_T(new_T);
            DEFINE_REG(temp);
            SLLI(temp, Rn, 1);
            DEFINE_RESULT_REG(result, R[n]);
            OR(result, temp, T);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(n)) << 1);
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) << 1);
            PTR_CLEAR(n);
            break;
          }

          // $4xx5
          case 0x05: {  // ROTR Rn
            DEBUG_PRINT_ONCE("ROTR Rn");
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (next_opcode == opcode) {
                    ADD_TO_SHIFT_CACHE(1);
                    break;
                }
            }
            const unsigned int rotate_count = (CACHED_SHIFT_COUNT() + 1) & 31;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int rotate_count = 1;
#endif
            GET_Rn;
            DEFINE_REG(new_T);
            BFEXT(new_T, Rn, (rotate_count + 31) & 31, 1);
            SET_SR_T(new_T);
            DEFINE_RESULT_REG(result, R[n]);
            RORI(result, Rn, rotate_count);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n),
                         REG_GETKNOWN(REG_R(n)) >> rotate_count
                         | REG_GETKNOWN(REG_R(n)) << (32 - rotate_count));
            REG_SETVALUE(REG_R(n),
                         REG_GETVALUE(REG_R(n)) >> rotate_count
                         | REG_GETVALUE(REG_R(n)) << (32 - rotate_count));
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }
          case 0x15: {  // CMP/PL Rn
            DEBUG_PRINT_ONCE("CMP/PL Rn");
            GET_Rn;
            DEFINE_REG(zero);
            MOVEI(zero, 0);
            DEFINE_REG(new_T);
            SLTS(new_T, zero, Rn);
            SET_SR_T(new_T);
            break;
          }
          case 0x25: {  // ROTCR Rn
            DEBUG_PRINT_ONCE("ROTCR Rn");
            GET_SR_T;
            GET_Rn;
            DEFINE_REG(new_T);
            ANDI(new_T, Rn, 1);
            SET_SR_T(new_T);
            DEFINE_REG(temp);
            SRLI(temp, Rn, 1);
            DEFINE_RESULT_REG(result, R[n]);
            BFINS(result, temp, T, 31, 1);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(n)) >> 1);
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) >> 1);
            PTR_CLEAR(n);
            break;
          }

          // $4xx6
          case 0x06: {  // LDS.L @Rn+,MACH
            DEBUG_PRINT_ONCE("LDS.L @Rn+,MACH");
            DEFINE_RESULT_REG(value, MACH);
            LOAD_Rn_inc(L, value);
            SET_MACH(value);
            CLEAR_MAC_IS_ZERO();
            break;
          }
          case 0x16: {  // LDS.L @Rn+,MACL
            DEBUG_PRINT_ONCE("LDS.L @Rn+,MACL");
            DEFINE_RESULT_REG(value, MACL);
            LOAD_Rn_inc(L, value);
            SET_MACL(value);
            CLEAR_MAC_IS_ZERO();
            break;
          }
          case 0x26: {  // LDS.L @Rn+,PR
            DEBUG_PRINT_ONCE("LDS.L @Rn+,PR");
            DEFINE_RESULT_REG(value, PR);
            LOAD_Rn_inc(L, value);
            SET_PR(value);
            break;
          }

          // $4xx7
          case 0x07: {  // LDC.L @Rn+,SR
            DEBUG_PRINT_ONCE("LDC.L @Rn+,SR");
            DEFINE_REG(value);
            LOAD_Rn_inc(L, value);
            DEFINE_RESULT_REG(new_SR, SR);
            ANDI(new_SR, value, 0x3F3);
            SET_SR(new_SR);
            cur_cycles += 2;
            state->need_interrupt_check = 1;
            break;
          }
          case 0x17: {  // LDC.L @Rn+,GBR
            DEBUG_PRINT_ONCE("LDC.L @Rn+,GBR");
            DEFINE_RESULT_REG(value, GBR);
            LOAD_Rn_inc(L, value);
            SET_GBR(value);
            REG_SETKNOWN(REG_GBR, 0);
            cur_cycles += 2;
            break;
          }
          case 0x27: {  // LDC.L @Rn+,VBR
            DEBUG_PRINT_ONCE("LDC.L @Rn+,VBR");
            DEFINE_RESULT_REG(value, VBR);
            LOAD_Rn_inc(L, value);
            SET_VBR(value);
            cur_cycles += 2;
            break;
          }

          // $4xx8
          case 0x08: case 0x18: case 0x28: {
            unsigned int this_count;
            switch (opcode>>4 & 0xF) {
              case 0x0:  // SHLL2 Rn
                DEBUG_PRINT_ONCE("SHLL2 Rn");
                this_count = 2;
                break;
              case 0x1:  // SHLL8 Rn
                DEBUG_PRINT_ONCE("SHLL8 Rn");
                this_count = 8;
                break;
              case 0x2:  // SHLL16 Rn
                DEBUG_PRINT_ONCE("SHLL16 Rn");
                this_count = 16;
                break;
              default:  // Not needed, but avoid a compiler warning
                this_count = 0;
                break;
            }
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                /* For multi-bit shifts, we can accept any size shift in
                 * the same direction on the next instruction. */
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (((next_opcode & 0xFFCF) == (opcode & 0xFFCF)
                     && ((next_opcode>>4 & 0xF) != 3)) //Watch out for invalids
                 || (next_opcode & 0xFFDF) == (opcode & 0xFF07)
                ) {
                    if (CACHED_SHIFT_COUNT() < 32) {
                        ADD_TO_SHIFT_CACHE(this_count);
                    }
                    break;
                }
            }
            const unsigned int shift_count = CACHED_SHIFT_COUNT() + this_count;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int shift_count = this_count;
#endif
            DEFINE_RESULT_REG(result, R[n]);
            if (shift_count >= 32) {
                MOVEI(result, 0);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
            } else {
                GET_Rn;
                SLLI(result, Rn, shift_count);
                REG_SETKNOWN(REG_R(n),
                             REG_GETKNOWN(REG_R(n)) << shift_count
                             | 0xFFFFFFFFU >> (32-shift_count));
                REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) << shift_count);
            }
            SET_Rn(result);
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }  // SHLLx Rn

          // $4xx9
          case 0x09: case 0x19: case 0x29: {
            unsigned int this_count;
            switch (opcode>>4 & 0xF) {
              case 0x0:  // SHLR2 Rn
                DEBUG_PRINT_ONCE("SHLR2 Rn");
                this_count = 2;
                break;
              case 0x1:  // SHLR8 Rn
                DEBUG_PRINT_ONCE("SHLR8 Rn");
                this_count = 8;
                break;
              case 0x2:  // SHLR16 Rn
                DEBUG_PRINT_ONCE("SHLR16 Rn");
                this_count = 16;
                break;
              default:  // Not needed, but avoid a compiler warning
                this_count = 0;
                break;
            }
#ifdef OPTIMIZE_SHIFT_SEQUENCES
            if (!in_delay && CAN_CACHE_SHIFTS()) {
                /* We could theoretically cached SHLRn + SHAR by treating
                 * the SHAR as an SHLR instead (since the top bit will be
                 * zero following SHLRn), but since this would require
                 * extra logic for a case assumed to be extremely rare,
                 * we let it slide. */
                GET_NEXT_OPCODE_FOR_SHIFT_CACHE;
                if (((next_opcode & 0xFFCF) == (opcode & 0xFFCF)
                     && ((next_opcode>>4 & 0xF) != 3)) //Watch out for invalids
                 || (next_opcode & 0xFFFF) == (opcode & 0xFF07)
                ) {
                    if (CACHED_SHIFT_COUNT() < 32) {
                        ADD_TO_SHIFT_CACHE(this_count);
                    }
                    break;
                }
            }
            const unsigned int shift_count = CACHED_SHIFT_COUNT() + this_count;
#else  // !OPTIMIZE_SHIFT_SEQUENCES
            const unsigned int shift_count = this_count;
#endif
            DEFINE_RESULT_REG(result, R[n]);
            if (shift_count >= 32) {
                MOVEI(result, 0);
                REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
                REG_SETVALUE(REG_R(n), 0);
            } else {
                GET_Rn;
                SRLI(result, Rn, shift_count);
                REG_SETKNOWN(REG_R(n),
                             REG_GETKNOWN(REG_R(n)) >> shift_count
                             | 0xFFFFFFFF << (32-shift_count));
                REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) >> shift_count);
            }
            SET_Rn(result);
            PTR_CLEAR(n);
            CLEAR_SHIFT_CACHE();
            break;
          }  // SHLRx Rn

          // $4xxA
          case 0x0A: {  // LDS Rn,MACH
            DEBUG_PRINT_ONCE("LDS Rn,MACH");
            /* Make sure we load a _copy_ of Rn, so a subsequent overwrite
             * of the register by a MADD instruction doesn't modify the
             * value of Rn as well. */
            DEFINE_RESULT_REG(Rn, MACH);
            LOAD_STATE_COPY(Rn, R[n]);
            SET_MACH(Rn);
            CLEAR_MAC_IS_ZERO();
            break;
          }
          case 0x1A: {  // LDS Rn,MACL
            DEBUG_PRINT_ONCE("LDS Rn,MACL");
            DEFINE_RESULT_REG(Rn, MACL);
            LOAD_STATE_COPY(Rn, R[n]);
            SET_MACL(Rn);
            CLEAR_MAC_IS_ZERO();
            break;
          }
          case 0x2A: {  // LDS Rn,PR
            DEBUG_PRINT_ONCE("LDS Rn,PR");
            COPY_FROM_Rn(PR);
            break;
          }

          // $4xxB
          case 0x0B: {  // JSR @Rn
            DEBUG_PRINT_ONCE("JSR @Rn");
            DEFINE_RESULT_REG(ret_addr, PR);
            MOVEI(ret_addr, cur_PC + 4);
            SET_PR(ret_addr);
            if (BRANCH_IS_FOLDABLE_SUBROUTINE(cur_PC)) {
                state->branch_type = SH2BRTYPE_FOLDED;
                state->branch_target = BRANCH_FOLD_TARGET(cur_PC);
                state->branch_fold_native = BRANCH_FOLD_NATIVE_FUNC(cur_PC);
            } else if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                state->branch_type = SH2BRTYPE_STATIC;
                state->branch_target = REG_GETVALUE(REG_R(n));
            } else {
                state->branch_type = SH2BRTYPE_DYNAMIC;
                GET_Rn;
                state->branch_target_reg = Rn;
            }
            state->delay = 1;
            cur_cycles += 1;
            break;
          }
          case 0x1B: {  // TAS.B @Rn
            DEBUG_PRINT_ONCE("TAS.B @Rn");
            DEFINE_REG(value);
            LOAD_Rn(B, value);
            DEFINE_REG(new_T);
            SEQZ(new_T, value);
            SET_SR_T(new_T);
            DEFINE_REG(new_value);
            ORI(new_value, value, 0x80);
            STORE_Rn(B, new_value);
            cur_cycles += 3;
            break;
          }
          case 0x2B: {  // JMP @Rn
            DEBUG_PRINT_ONCE("JMP @Rn");
            if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                state->branch_type = SH2BRTYPE_STATIC;
                state->branch_target = REG_GETVALUE(REG_R(n));
            } else {
                state->branch_type = SH2BRTYPE_DYNAMIC;
                GET_Rn;
                state->branch_target_reg = Rn;
            }
            state->delay = 1;
            cur_cycles += 1;
            break;
          }

          // $4xxE
          case 0x0E: {  // LDC Rn,SR
            DEBUG_PRINT_ONCE("LDC Rn,SR");
            GET_Rn;
            DEFINE_RESULT_REG(new_SR, SR);
            ANDI(new_SR, Rn, 0x3F3);
            SET_SR(new_SR);
            state->need_interrupt_check = 1;
            break;
          }
          case 0x1E: {  // LDC Rn,GBR
            DEBUG_PRINT_ONCE("LDC Rn,GBR");
            COPY_FROM_Rn(GBR);
            REG_SETKNOWN(REG_GBR, REG_GETKNOWN(REG_R(n)));
            REG_SETVALUE(REG_GBR, REG_GETVALUE(REG_R(n)));
            break;
          }
          case 0x2E: {  // LDC Rn,VBR
            DEBUG_PRINT_ONCE("LDC Rn,VBR");
            COPY_FROM_Rn(VBR);
            break;
          }

          // $4xxF
          case 0x0F: case 0x1F: case 0x2F: case 0x3F:
          case 0x4F: case 0x5F: case 0x6F: case 0x7F:
          case 0x8F: case 0x9F: case 0xAF: case 0xBF:
          case 0xCF: case 0xDF: case 0xEF: case 0xFF: {  // MAC.W @Rm+,@Rn+
            DEBUG_PRINT_ONCE("MAC.W @Rm+,@Rn+");

            /* Load the values */
            DEFINE_REG(value_m);
            LOAD_Rm_inc(W, value_m);
            DEFINE_REG(value_n);
            LOAD_Rn_inc(W, value_n);

            /* If we're saturating, we may need the old value of MACH/MACL */
            DECLARE_REG(old_MACH);
            DECLARE_REG(old_MACL);
            if (!CAN_OMIT_MAC_S_CHECK) {
                ALLOC_REG(old_MACH);
                LOAD_STATE_COPY(old_MACH, MACH);
                ALLOC_REG(old_MACL);
                LOAD_STATE_COPY(old_MACL, MACL);
            } else {  // Not needed, but avoid a compiler warning
                old_MACH = 0;
                old_MACL = 0;
            }

            /* Do the actual multiplication and addition */
            if (MAC_IS_ZERO()) {
                DEFINE_RESULT_REG(new_MACL, MACL);
                DEFINE_RESULT_REG(new_MACH, MACH);
                MULS_64(new_MACL, value_m, value_n, new_MACH);
                SET_MACL(new_MACL);
                SET_MACH(new_MACH);
            } else {
                GET_MACL;
                GET_MACH;
                MADDS_64(MACL, value_m, value_n, MACH);
                SET_MACL(MACL);
                SET_MACH(MACH);
            }

            /* Perform saturation if the S flag of SR is set */
            if (!CAN_OMIT_MAC_S_CHECK) {
                GET_SR;
                DEFINE_REG(saturate);
                ANDI(saturate, SR, SR_S);
                CREATE_LABEL(macw_nosat);
                GOTO_IF_Z(macw_nosat, saturate); {
                    GET_MACL;
                    GET_MACH;
                    DEFINE_REG(value_m_sign);
                    SLTZ(value_m_sign, value_m);
                    DEFINE_REG(value_n_sign);
                    SLTZ(value_n_sign, value_n);
                    DEFINE_REG(sum_sign);
                    SLTZ(sum_sign, MACL);                       // sum < 0
                    DEFINE_REG(product_sign);
                    XOR(product_sign, value_m_sign, value_n_sign);  // product < 0
                    DEFINE_REG(MACL_sign);
                    SLTZ(MACL_sign, old_MACL);                  // MACL < 0
                    DEFINE_REG(temp0);
                    XOR(temp0, product_sign, MACL_sign);
                    DEFINE_REG(temp1);
                    XORI(temp1, temp0, 1);           // (product<0) == (MACL<0)
                    DEFINE_REG(temp2);
                    XOR(temp2, sum_sign, product_sign);  // (sum<0) != (product<0)
                    DEFINE_REG(overflow);
                    AND(overflow, temp2, temp1);     // Nonzero if overflow
                    DEFINE_REG(overflow_bit);
                    MOVEI(overflow_bit, 0);
                    CREATE_LABEL(macw_no_overflow);
                    GOTO_IF_Z(macw_no_overflow, overflow); {
                        DEFINE_REG(sat_neg);
                        MOVEI(sat_neg, 0x80000000);
                        DEFINE_REG(sat_pos);
                        NOT(sat_pos, sat_neg);
                        DEFINE_REG(saturated);
                        SELECT(saturated, sat_neg, sat_pos, product_sign);
                        /* Note: SSA violations for state block optimization */
                        MOVE(MACL, saturated);
                        SET_MACL(MACL);
                        MOVEI(overflow_bit, 1);
                    } DEFINE_LABEL(macw_no_overflow);
                    OR(MACH, old_MACH, overflow_bit);
                    SET_MACH(MACH);
                } DEFINE_LABEL(macw_nosat);
            }  // if (!CAN_OMIT_MAC_S_CHECK)

            CLEAR_MAC_IS_ZERO();
            cur_cycles += 2;
            break;
          }  // MAC.W

          default:
            goto invalid;
        }
        break;
      }  // $4xxx

      case 0x5: {  // MOV.L @(disp,Rm),Rn
        DEBUG_PRINT_ONCE("MOV.L @(disp,Rm),Rn");
        const int disp = (opcode & 0xF) * 4;
        DEFINE_RESULT_REG(value, R[n]);
        LOAD_disp_Rm(L, value, disp);
        SET_Rn(value);
        REG_SETKNOWN(REG_R(n), 0);
        PTR_CLEAR(n);
        if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF && PTR_ISLOCAL(m)) {
            PTR_SET_SOURCE(n, REG_GETVALUE(REG_R(m)) + disp);
        }
        break;
      }  // $5xxx

      case 0x6: {
        switch (opcode & 0xF) {

          case 0x0: {  // MOV.B @Rm,Rn
            DEBUG_PRINT_ONCE("MOV.B @Rm,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm(B, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0x1: {  // MOV.W @Rm,Rn
            DEBUG_PRINT_ONCE("MOV.W @Rm,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm(W, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0x2: {  // MOV.L @Rm,Rn
            DEBUG_PRINT_ONCE("MOV.L @Rm,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm(L, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF && PTR_ISLOCAL(m)) {
                PTR_SET_SOURCE(n, REG_GETVALUE(REG_R(m)));
            }
            break;
          }

          case 0x3: {  // MOV Rm,Rn
            DEBUG_PRINT_ONCE("MOV Rm,Rn");
            if (state->pending_select && !in_delay) {
                state->pending_select = 0;
                GET_Rm;
                GET_Rn;
                DEFINE_RESULT_REG(selected, R[n]);
                if (state->select_sense) {
                    /* If select_sense is nonzero, the branch was a BT or BT/S,
                     * meaning we _skip_ the new value if SR.T is set. */
                    SELECT(selected, Rn, Rm, state->branch_cond_reg);
                } else {
                    SELECT(selected, Rm, Rn, state->branch_cond_reg);
                }
                SET_Rn(selected);
                REG_SETKNOWN(REG_R(n), 0);
                PTR_CLEAR(n);
            } else {
                COPY_TO_Rn(R[m]);
                REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(m)));
                REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(m)));
                PTR_COPY(m, n, 0);
            }
            break;
          }

          case 0x4: {  // MOV.B @Rm+,Rn
            DEBUG_PRINT_ONCE("MOV.B @Rm+,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm_inc(B, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0x5: {  // MOV.W @Rm+,Rn
            DEBUG_PRINT_ONCE("MOV.W @Rm+,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm_inc(W, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0x6: {  // MOV.L @Rm+,Rn
            DEBUG_PRINT_ONCE("MOV.L @Rm+,Rn");
            DEFINE_RESULT_REG(value, R[n]);
            LOAD_Rm_inc(L, value);
            SET_Rn(value);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            if (REG_GETKNOWN(REG_R(m)) == 0xFFFFFFFF && PTR_ISLOCAL(m)) {
                PTR_SET_SOURCE(n, REG_GETVALUE(REG_R(m)) - 4);
            }
            break;
          }

          case 0x7: {  // NOT Rm,Rn
            DEBUG_PRINT_ONCE("NOT Rm,Rn");
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            NOT(result, Rm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(m)));
            REG_SETVALUE(REG_R(n), ~REG_GETVALUE(REG_R(m)));
            PTR_CLEAR(n);
            break;
          }

          case 0x8: {  // SWAP.B Rm,Rn
            DEBUG_PRINT_ONCE("SWAP.B Rm,Rn");
            GET_Rm;
            /* BSWAPH swaps both low and high halfwords, but we want to
             * keep the high halfword unchanged */
            DEFINE_REG(temp);
            BSWAPH(temp, Rm);
            DEFINE_RESULT_REG(result, R[n]);
            BFINS(result, Rm, temp, 0, 16);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), (REG_GETKNOWN(REG_R(m)) & 0xFFFF0000)
                                 | (REG_GETKNOWN(REG_R(m)) & 0x0000FF00) >> 8
                                 | (REG_GETKNOWN(REG_R(m)) & 0x000000FF) << 8);
            REG_SETVALUE(REG_R(n), (REG_GETVALUE(REG_R(m)) & 0xFFFF0000)
                                 | (REG_GETVALUE(REG_R(m)) & 0x0000FF00) >> 8
                                 | (REG_GETVALUE(REG_R(m)) & 0x000000FF) << 8);
            PTR_CLEAR(n);
            break;
          }

          case 0x9: {  // SWAP.W Rm,Rn
            DEBUG_PRINT_ONCE("SWAP.W Rm,Rn");
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            HSWAPW(result, Rm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), (REG_GETKNOWN(REG_R(m)) & 0xFFFF0000) >> 16
                                 | (REG_GETKNOWN(REG_R(m)) & 0x0000FFFF) << 16);
            REG_SETVALUE(REG_R(n), (REG_GETVALUE(REG_R(m)) & 0xFFFF0000) >> 16
                                 | (REG_GETVALUE(REG_R(m)) & 0x0000FFFF) << 16);
            PTR_CLEAR(n);
            break;
          }

          case 0xA: {  // NEGC Rm,Rn
            DEBUG_PRINT_ONCE("NEGC Rm,Rn");
            GET_SR_T;
            GET_Rm;
            DEFINE_REG(zero);
            MOVEI(zero, 0);
            DEFINE_REG(borrow);
            SLTU(borrow, zero, Rm);
            DEFINE_REG(new_T);
            OR(new_T, borrow, T);
            SET_SR_T(new_T);
            DEFINE_REG(temp);
            SUB(temp, zero, Rm);
            DEFINE_RESULT_REG(result, R[n]);
            SUB(result, temp, T);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0xB: {  // NEG Rm,Rn
            DEBUG_PRINT_ONCE("NEG Rm,Rn");
            GET_Rm;
            DEFINE_REG(zero);
            MOVEI(zero, 0);
            DEFINE_RESULT_REG(result, R[n]);
            SUB(result, zero, Rm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0);
            PTR_CLEAR(n);
            break;
          }

          case 0xC: {  // EXTU.B Rm,Rn
            DEBUG_PRINT_ONCE("EXTU.B Rm,Rn");
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            ANDI(result, Rm, 0xFF);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(m)) | 0xFFFFFF00);
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(m)) & 0x000000FF);
            PTR_CLEAR(n);
            break;
          }

          case 0xD: {  // EXTU.W Rm,Rn
            DEBUG_PRINT_ONCE("EXTU.W Rm,Rn");
            GET_Rm;
            DEFINE_RESULT_REG(result, R[n]);
            ANDI(result, Rm, 0xFFFF);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), REG_GETKNOWN(REG_R(m)) | 0xFFFF0000);
            REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(m)) & 0x0000FFFF);
            PTR_CLEAR(n);
            break;
          }

          case 0xE: {  // EXTS.B Rm,Rn
            DEBUG_PRINT_ONCE("EXTS.B Rm,Rn");
            GET_Rm;
            DEFINE_REG(temp);
            SLLI(temp, Rm, 24);
            DEFINE_RESULT_REG(result, R[n]);
            SRAI(result, temp, 24);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n),
                         (int32_t)(REG_GETKNOWN(REG_R(m)) << 24) >> 24);
            REG_SETVALUE(REG_R(n),
                         (int32_t)(REG_GETVALUE(REG_R(m)) << 24) >> 24);
            PTR_CLEAR(n);
            break;
          }

          case 0xF: {  // EXTS.W Rm,Rn
            DEBUG_PRINT_ONCE("EXTS.W Rm,Rn");
            GET_Rm;
            DEFINE_REG(temp);
            SLLI(temp, Rm, 16);
            DEFINE_RESULT_REG(result, R[n]);
            SRAI(result, temp, 16);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n),
                         (int32_t)(REG_GETKNOWN(REG_R(m)) << 16) >> 16);
            REG_SETVALUE(REG_R(n),
                         (int32_t)(REG_GETVALUE(REG_R(m)) << 16) >> 16);
            PTR_CLEAR(n);
            break;
          }

          default:   // impossible, but included for consistency
            goto invalid;
        }
        break;
      }  // $6xxx

      case 0x7: {  // ADD #imm,Rn
        DEBUG_PRINT_ONCE("ADD #imm,Rn");
        const int8_t imm = opcode & 0xFF;
        if (state->pending_select && !in_delay) {
            state->pending_select = 0;
            GET_Rn;
            DEFINE_REG(result);
            ADDI(result, Rn, (int32_t)imm);
            DEFINE_RESULT_REG(selected, R[n]);
            if (state->select_sense) {
                /* If select_sense is nonzero, the branch was a BT or BT/S,
                 * meaning we _skip_ the new value if SR.T is set. */
                SELECT(selected, Rn, result, state->branch_cond_reg);
            } else {
                SELECT(selected, result, Rn, state->branch_cond_reg);
            }
            SET_Rn(selected);
            REG_SETKNOWN(REG_R(n), 0);
        } else {
            ADDI_Rn_NOREG((int32_t)imm);
            if (REG_GETKNOWN(REG_R(n)) == 0xFFFFFFFF) {
                REG_SETVALUE(REG_R(n), REG_GETVALUE(REG_R(n)) + (int32_t)imm);
            } else {
                REG_SETKNOWN(REG_R(n), 0);
            }
        }
        break;
      }  // $7xxx

      case 0x8: {
        switch (opcode>>8 & 0xF) {

          case 0x0: {  // MOV.B R0,@(disp,Rm)
            DEBUG_PRINT_ONCE("MOV.B R0,@(disp,Rm)");
            const int disp = opcode & 0xF;
            GET_R0;
            STORE_disp_Rm(B, R0, disp);
            break;
          }

          case 0x1: {  // MOV.W R0,@(disp,Rm)
            DEBUG_PRINT_ONCE("MOV.W R0,@(disp,Rm)");
            const int disp = (opcode & 0xF) * 2;
            GET_R0;
            STORE_disp_Rm(W, R0, disp);
            break;
          }

          case 0x4: {  // MOV.B @(disp,Rm),R0
            DEBUG_PRINT_ONCE("MOV.B @(disp,Rm),R0");
            const int disp = opcode & 0xF;
            DEFINE_RESULT_REG(value, R[0]);
            LOAD_disp_Rm(B, value, disp);
            SET_R0(value);
            REG_SETKNOWN(REG_R(0), 0);
            PTR_CLEAR(0);
            break;
          }

          case 0x5: {  // MOV.W @(disp,Rm),R0
            DEBUG_PRINT_ONCE("MOV.W @(disp,Rm),R0");
            const int disp = (opcode & 0xF) * 2;
            DEFINE_RESULT_REG(value, R[0]);
            LOAD_disp_Rm(W, value, disp);
            SET_R0(value);
            REG_SETKNOWN(REG_R(0), 0);
            PTR_CLEAR(0);
            break;
          }

          case 0x8: {  // CMP/EQ #imm,R0
            DEBUG_PRINT_ONCE("CMP/EQ #imm,R0");
            const int8_t imm = opcode & 0xFF;
            GET_R0;
            DEFINE_REG(test);
            SUBI(test, R0, (int32_t)imm);
            DEFINE_REG(new_T);
            SEQZ(new_T, test);
            SET_SR_T(new_T);
            break;
          }

          case 0x9: {  // BT label
            DEBUG_PRINT_ONCE("BT label");
            GET_SR_T;
            if (BRANCH_FALLS_THROUGH(cur_PC) || BRANCH_IS_SELECT(cur_PC)) {
                /* We don't need to branch in the native code, and we don't
                 * need to update PC (because a later instruction will do
                 * so), but we do need to update the cycle counter
                 * appropriately. */
                ADD_CYCLES();
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_REG(inc_cycles);
                ADDI(inc_cycles, cycles, BRANCH_IS_SELECT(cur_PC) ? 1 : 2);
                DEFINE_RESULT_REG(new_cycles, cycles);
                SELECT(new_cycles, inc_cycles, cycles, T);
                STORE_STATE(cycles, new_cycles);
                if (BRANCH_IS_SELECT(cur_PC)) {
                    state->pending_select = 1;
                    state->select_sense = 1;
                    state->branch_cond_reg = T;
                }
                break;
            }
            const int disp = ((int32_t)(opcode & 0xFF) << 24) >> 23;
            state->branch_type = SH2BRTYPE_BT;
            state->branch_targets_rts = BRANCH_TARGETS_RTS(cur_PC);
            state->loop_to_jsr = BRANCH_IS_LOOP_TO_JSR(cur_PC);
            state->branch_cycles = 2;
            if (state->branch_targets_rts) {
                GET_PR;
                state->branch_target_reg = PR;
                state->branch_cycles += 3;
            } else if (BRANCH_IS_THREADED(cur_PC)) {
                state->branch_target = BRANCH_THREAD_TARGET(cur_PC);
                state->branch_cycles += BRANCH_THREAD_COUNT(cur_PC) * 3;
            } else {
                state->branch_target = (cur_PC + 4) + disp;
            }
            state->branch_cond_reg = T;
            break;
          }

          case 0xB: {  // BF label
            DEBUG_PRINT_ONCE("BF label");
            GET_SR_T;
            if (BRANCH_FALLS_THROUGH(cur_PC) || BRANCH_IS_SELECT(cur_PC)) {
                ADD_CYCLES();
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_REG(inc_cycles);
                ADDI(inc_cycles, cycles, BRANCH_IS_SELECT(cur_PC) ? 1 : 2);
                DEFINE_RESULT_REG(new_cycles, cycles);
                SELECT(new_cycles, cycles, inc_cycles, T);
                STORE_STATE(cycles, new_cycles);
                if (BRANCH_IS_SELECT(cur_PC)) {
                    state->pending_select = 1;
                    state->select_sense = 0;
                    state->branch_cond_reg = T;
                }
                break;
            }
            const int disp = ((int32_t)(opcode & 0xFF) << 24) >> 23;
            state->branch_type = SH2BRTYPE_BF;
            state->branch_targets_rts = BRANCH_TARGETS_RTS(cur_PC);
            state->loop_to_jsr = BRANCH_IS_LOOP_TO_JSR(cur_PC);
            state->branch_cycles = 2;
            if (state->branch_targets_rts) {
                GET_PR;
                state->branch_target_reg = PR;
                state->branch_cycles += 3;
            } else if (BRANCH_IS_THREADED(cur_PC)) {
                state->branch_target = BRANCH_THREAD_TARGET(cur_PC);
                state->branch_cycles += BRANCH_THREAD_COUNT(cur_PC) * 3;
            } else {
                state->branch_target = (cur_PC + 4) + disp;
            }
            state->branch_cond_reg = T;
            break;
          }

          case 0xD: {  // BT/S label
            DEBUG_PRINT_ONCE("BT/S label");
            GET_SR_T;
            /* For delayed SELECT branches, the cycle count ends up the
             * same whether the branch is taken or not, so we can skip all
             * the cycle stuff. */
            if (BRANCH_FALLS_THROUGH(cur_PC)) {
                ADD_CYCLES();
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_REG(inc_cycles);
                ADDI(inc_cycles, cycles, 1);
                DEFINE_RESULT_REG(new_cycles, cycles);
                SELECT(new_cycles, inc_cycles, cycles, T);
                STORE_STATE(cycles, new_cycles);
                break;
            } else if (BRANCH_IS_SELECT(cur_PC)) {
                state->pending_select = 1;
                state->select_sense = 1;
                state->branch_cond_reg = T;
                state->delay = 1;
                break;
            }
            const int disp = ((int32_t)(opcode & 0xFF) << 24) >> 23;
            state->branch_type = SH2BRTYPE_BT_S;
            state->branch_targets_rts = BRANCH_TARGETS_RTS(cur_PC);
            state->loop_to_jsr = BRANCH_IS_LOOP_TO_JSR(cur_PC);
            state->branch_cycles = 1;
            if (state->branch_targets_rts) {
                GET_PR;
                state->branch_target_reg = PR;
                state->branch_cycles += 3;
            } else if (BRANCH_IS_THREADED(cur_PC)) {
                state->branch_target = BRANCH_THREAD_TARGET(cur_PC);
                state->branch_cycles += BRANCH_THREAD_COUNT(cur_PC) * 3;
            } else {
                state->branch_target = (cur_PC + 4) + disp;
            }
            state->branch_cond_reg = T;
            state->delay = 1;
            /* Unlike the other delayed branch instructions, we don't add
             * the extra cycle for conditional branches until we actually
             * branch; this avoids having to add a variable number of
             * cycles to the cycle counter and thus breaking compile-time
             * accumulation of cycles with OPTIMIZE_CONSTANT_ADDS. */
            break;
          }

          case 0xF: {  // BF/S label
            DEBUG_PRINT_ONCE("BF/S label");
            GET_SR_T;
            if (BRANCH_FALLS_THROUGH(cur_PC)) {
                ADD_CYCLES();
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_REG(inc_cycles);
                ADDI(inc_cycles, cycles, 1);
                DEFINE_RESULT_REG(new_cycles, cycles);
                SELECT(new_cycles, inc_cycles, cycles, T);
                STORE_STATE(cycles, new_cycles);
                break;
            } else if (BRANCH_IS_SELECT(cur_PC)) {
                state->pending_select = 1;
                state->select_sense = 0;
                state->branch_cond_reg = T;
                state->delay = 1;
                break;
            }
            const int disp = ((int32_t)(opcode & 0xFF) << 24) >> 23;
            state->branch_type = SH2BRTYPE_BF_S;
            state->branch_targets_rts = BRANCH_TARGETS_RTS(cur_PC);
            state->loop_to_jsr = BRANCH_IS_LOOP_TO_JSR(cur_PC);
            state->branch_cycles = 1;
            if (state->branch_targets_rts) {
                GET_PR;
                state->branch_target_reg = PR;
                state->branch_cycles += 3;
            } else if (BRANCH_IS_THREADED(cur_PC)) {
                state->branch_target = BRANCH_THREAD_TARGET(cur_PC);
                state->branch_cycles += BRANCH_THREAD_COUNT(cur_PC) * 3;
            } else {
                state->branch_target = (cur_PC + 4) + disp;
            }
            state->branch_cond_reg = T;
            state->delay = 1;
            break;
          }

          default:
            goto invalid;
        }
        break;
      }  // $8xxx

      case 0x9: {  // MOV.W @(disp,PC),Rn
        DEBUG_PRINT_ONCE("MOV.W @(disp,PC),Rn");
        const int disp = (opcode & 0xFF) * 2;
        const uint32_t address = cur_PC + 4 + disp;
        DEFINE_RESULT_REG(value, R[n]);
        SH2_LOAD_ABS_W(value, address);
        SET_Rn(value);
        REG_SETKNOWN(REG_R(n), 0);
        PTR_CLEAR(n);
        break;
      }  // $9xxx

      case 0xA: {  // BRA label
        DEBUG_PRINT_ONCE("BRA label");
        if (BRANCH_FALLS_THROUGH(cur_PC)) {
            cur_cycles += 1;
            break;
        }
        if (BRANCH_TARGETS_RTS(cur_PC)) {
            state->branch_type = SH2BRTYPE_DYNAMIC;
            GET_PR;
            state->branch_target_reg = PR;
            cur_cycles += 3;
        } else {
            state->loop_to_jsr = BRANCH_IS_LOOP_TO_JSR(cur_PC);
            const int disp = ((int32_t)(opcode & 0xFFF) << 20) >> 19;
            state->branch_type = SH2BRTYPE_STATIC;
            state->branch_target = (cur_PC + 4) + disp;
        }
        state->delay = 1;
        cur_cycles += 1;
        break;
      }  // $Axxx

      case 0xB: {  // BSR label
        DEBUG_PRINT_ONCE("BSR label");
        const int disp = ((int32_t)(opcode & 0xFFF) << 20) >> 19;
        DEFINE_RESULT_REG(ret_addr, PR);
        MOVEI(ret_addr, cur_PC + 4);
        SET_PR(ret_addr);
        if (BRANCH_IS_FOLDABLE_SUBROUTINE(cur_PC)) {
            state->branch_type = SH2BRTYPE_FOLDED;
            state->branch_target = BRANCH_FOLD_TARGET(cur_PC);
            state->branch_fold_native = BRANCH_FOLD_NATIVE_FUNC(cur_PC);
        } else {
            state->branch_type = SH2BRTYPE_STATIC;
            state->branch_target = (cur_PC + 4) + disp;
        }
        state->delay = 1;
        cur_cycles += 1;
        break;
      }  // $Bxxx

      case 0xC: {
        const unsigned int imm = opcode & 0xFF;
        switch (opcode>>8 & 0xF) {

          case 0x0: {  // MOV.B R0,@(disp,GBR)
            DEBUG_PRINT_ONCE("MOV.B R0,@(disp,GBR)");
            GET_R0;
            STORE_disp_GBR(B, R0, imm);
            break;
          }

          case 0x1: {  // MOV.W R0,@(disp,GBR)
            DEBUG_PRINT_ONCE("MOV.W R0,@(disp,GBR)");
            GET_R0;
            STORE_disp_GBR(W, R0, imm*2);
            break;
          }

          case 0x2: {  // MOV.L R0,@(disp,GBR)
            DEBUG_PRINT_ONCE("MOV.L R0,@(disp,GBR)");
            GET_R0;
            STORE_disp_GBR(L, R0, imm*4);
            break;
          }

          case 0x3: {  // TRAPA #imm
            DEBUG_PRINT_ONCE("TRAPA #imm");
            cur_cycles += 7;
            DEFINE_REG(PC);
            MOVEI(PC, cur_PC + 2);
            TAKE_EXCEPTION(imm, PC);
            break;
          }

          case 0x4: {  // MOV.B @(disp,GBR),R0
            DEBUG_PRINT_ONCE("MOV.B @(disp,GBR),R0");
            DEFINE_RESULT_REG(value, R[0]);
            LOAD_disp_GBR(B, value, imm);
            SET_R0(value);
            REG_SETKNOWN(REG_R(0), 0);
            PTR_CLEAR(0);
            break;
          }

          case 0x5: {  // MOV.W @(disp,GBR),R0
            DEBUG_PRINT_ONCE("MOV.W @(disp,GBR),R0");
            DEFINE_RESULT_REG(value, R[0]);
            LOAD_disp_GBR(W, value, imm*2);
            SET_R0(value);
            REG_SETKNOWN(REG_R(0), 0);
            PTR_CLEAR(0);
            break;
          }

          case 0x6: {  // MOV.L @(disp,GBR),R0
            DEBUG_PRINT_ONCE("MOV.L @(disp,GBR),R0");
            DEFINE_RESULT_REG(value, R[0]);
            LOAD_disp_GBR(L, value, imm*4);
            SET_R0(value);
            REG_SETKNOWN(REG_R(0), 0);
            PTR_CLEAR(0);
            break;
          }

          case 0x7: {  // MOVA @(disp,PC),R0
            DEBUG_PRINT_ONCE("MOVA @(disp,PC),R0");
            DEFINE_RESULT_REG(address, R[0]);
            MOVEI(address, (cur_PC & ~3) + 4 + imm*4);
            SET_R0(address);
            REG_SETKNOWN(REG_R(0), 0xFFFFFFFF);
            REG_SETVALUE(REG_R(0), (cur_PC & ~3) + 4 + imm*4);
            /* Technically it's still a pointer, but the former value is
             * gone (and we'll be using absolute loads/stores for this
             * register anyway), so clear pointer status */
            PTR_CLEAR(0);
            PTR_SETLOCAL(0);  // Flag to pass to the load/store routines
            break;
          }

          case 0x8: {  // TST #imm,R0
            DEBUG_PRINT_ONCE("TST #imm,R0");
            GET_R0;
#ifdef OPTIMIZE_VARIABLE_SHIFTS
            if (LIKELY(!in_delay) && fetch) {
                unsigned int count_reg, count_mask, shift_reg, shift_type;
                const uint8_t *cycles_array;
                const unsigned int num_insns = can_optimize_variable_shift(
                    fetch, cur_PC, &count_reg, &count_mask, &shift_reg,
                    &shift_type, &cycles_array
                );
                if (num_insns) {
# ifdef JIT_DEBUG_VERBOSE
                    DMSG("Optimizing variable shift at 0x%08X", (int)cur_PC);
# endif
                    DECLARE_REG(Rshift);
                    LOAD_STATE_ALLOC(Rshift, R[shift_reg]);
                    DEFINE_REG(count);
                    ANDI(count, R0, count_mask);  // count_reg is always 0
                    DEFINE_REG(new_T);
                    DEFINE_RESULT_REG(result, R[shift_reg]);
                    if (shift_type == 0) {
                        SRLI(new_T, Rshift, 31);
                        SLL(result, Rshift, count);
                    } else {
                        ANDI(new_T, Rshift, 1);
                        SRL(result, Rshift, count);
                    }
                    STORE_STATE(R[shift_reg], result);
                    /* SR.T is only set if the shift count is odd */
                    GET_SR_T;
                    DEFINE_REG(test);
                    ANDI(test, count, 1);
                    DEFINE_REG(final_T);
                    SELECT(final_T, new_T, T, test);
                    SET_SR_T(final_T);
                    DEFINE_REG(cycles_array_base);
                    MOVEA(cycles_array_base, ADDR_HI(cycles_array));
                    DEFINE_REG(cycles_array_ptr);
                    ADD(cycles_array_ptr, cycles_array_base, count);
                    cur_cycles--;  // Avoid double-counting this instruction
                    ADD_CYCLES();
                    DECLARE_REG(cycles);
                    LOAD_STATE_ALLOC(cycles, cycles);
                    DEFINE_REG(cycles_to_add);
                    LOAD_BU(cycles_to_add, cycles_array_ptr,
                            ADDR_LO(cycles_array));
                    DEFINE_RESULT_REG(new_cycles, cycles);
                    ADD(new_cycles, cycles, cycles_to_add);
                    STORE_STATE(cycles, new_cycles);
                    INC_PC_BY((num_insns - 1) * 2);
                    break;
                }
            }
#endif
            DEFINE_REG(test);
            ANDI(test, R0, imm);
            DEFINE_REG(new_T);
            SEQZ(new_T, test);
            SET_SR_T(new_T);
            break;
          }

          case 0x9: {  // AND #imm,R0
            DEBUG_PRINT_ONCE("AND #imm,R0");
            GET_R0;
            DEFINE_RESULT_REG(result, R[0]);
            ANDI(result, R0, imm);
            SET_R0(result);
            REG_SETKNOWN(REG_R(0), REG_GETKNOWN(REG_R(0)) | ~imm);
            REG_SETVALUE(REG_R(0), REG_GETVALUE(REG_R(0)) & imm);
            break;
          }

          case 0xA: {  // XOR #imm,R0
            DEBUG_PRINT_ONCE("XOR #imm,R0");
            GET_R0;
            DEFINE_RESULT_REG(result, R[0]);
            XORI(result, R0, imm);
            SET_R0(result);
            REG_SETVALUE(REG_R(0), REG_GETVALUE(REG_R(0)) ^ imm);
            break;
          }

          case 0xB: {  // OR #imm,R0
            DEBUG_PRINT_ONCE("OR #imm,R0");
            GET_R0;
            DEFINE_RESULT_REG(result, R[0]);
            ORI(result, R0, imm);
            SET_R0(result);
            REG_SETKNOWN(REG_R(0), REG_GETKNOWN(REG_R(0)) | imm);
            REG_SETVALUE(REG_R(0), REG_GETVALUE(REG_R(0)) | imm);
            break;
          }

          case 0xC: {  // TST.B #imm,@(R0,GBR)
            DEBUG_PRINT_ONCE("TST.B #imm,@(R0,GBR)");
            DEFINE_REG(value);
            LOAD_R0_GBR(B, value);
            DEFINE_REG(test);
            ANDI(test, value, imm);
            DEFINE_REG(new_T);
            SEQZ(new_T, test);
            SET_SR_T(new_T);
            break;
          }

          case 0xD: {  // AND.B #imm,@(R0,GBR)
            DEBUG_PRINT_ONCE("AND.B #imm,@(R0,GBR)");
            DEFINE_REG(value);
            LOAD_R0_GBR(B, value);
            DEFINE_REG(result);
            ANDI(result, value, imm);
            STORE_SAVED_R0_GBR(B, result);
            cur_cycles += 2;
            break;
          }

          case 0xE: {  // XOR.B #imm,@(R0,GBR)
            DEBUG_PRINT_ONCE("XOR.B #imm,@(R0,GBR)");
            DEFINE_REG(value);
            LOAD_R0_GBR(B, value);
            DEFINE_REG(result);
            XORI(result, value, imm);
            STORE_SAVED_R0_GBR(B, result);
            cur_cycles += 2;
            break;
          }

          case 0xF: {  // OR.B #imm,@(R0,GBR)
            DEBUG_PRINT_ONCE("OR.B #imm,@(R0,GBR)");
            DEFINE_REG(value);
            LOAD_R0_GBR(B, value);
            DEFINE_REG(result);
            ORI(result, value, imm);
            STORE_SAVED_R0_GBR(B, result);
            cur_cycles += 2;
            break;
          }

          default:  // impossible, but included for consistency
            goto invalid;
        }
        break;

      }  // $Cxxx

      case 0xD: {  // MOV.L @(disp,PC),Rn
        DEBUG_PRINT_ONCE("MOV.L @(disp,PC),Rn");
        const int disp = (opcode & 0xFF) * 4;
        const uint32_t address = (cur_PC & ~3) + 4 + disp;
        DEFINE_RESULT_REG(value, R[n]);
        SH2_LOAD_ABS_L(value, address);
        REG_SETKNOWN(REG_R(n), 0);
        SET_Rn(value);
        PTR_CLEAR(n);
        PTR_SET_SOURCE(n, address);
        break;
      }  // $Dxxx

      case 0xE: {  // MOV #imm,Rn
        DEBUG_PRINT_ONCE("MOV #imm,Rn");
        const int8_t imm = opcode & 0xFF;
        if (state->pending_select && !in_delay) {
            state->pending_select = 0;
            GET_Rn;
            DEFINE_REG(result);
            MOVEI(result, (int32_t)imm);
            DEFINE_RESULT_REG(selected, R[n]);
            if (state->select_sense) {
                /* If select_sense is nonzero, the branch was a BT or BT/S,
                 * meaning we _skip_ the new value if SR.T is set. */
                SELECT(selected, Rn, result, state->branch_cond_reg);
            } else {
                SELECT(selected, result, Rn, state->branch_cond_reg);
            }
            SET_Rn(selected);
            REG_SETKNOWN(REG_R(n), 0);
        } else {
            DEFINE_RESULT_REG(result, R[n]);
            MOVEI(result, (int32_t)imm);
            SET_Rn(result);
            REG_SETKNOWN(REG_R(n), 0xFFFFFFFF);
            REG_SETVALUE(REG_R(n), (int32_t)imm);
        }
        PTR_CLEAR(n);
        break;
      }  // $Exxx

      case 0xF: {
        goto invalid;
      }  // $Fxxx

    }

    /**** The Big Honkin' Opcode Switch Ends Here ****/

    /* Update the PC and cycle count */

    INC_PC();
    ADD_CYCLES();

    /* Handle any pending branches */

    if (UNLIKELY(state->branch_type != SH2BRTYPE_NONE && !state->delay)) {

        int is_idle = 0;

#ifdef OPTIMIZE_IDLE
        if ((state->branch_type == SH2BRTYPE_STATIC
             || ((state->branch_type == SH2BRTYPE_BT
                  || state->branch_type == SH2BRTYPE_BF
                  || state->branch_type == SH2BRTYPE_BT_S
                  || state->branch_type == SH2BRTYPE_BF_S)
                 && !state->branch_targets_rts))
         && state->branch_target >= initial_PC
         && state->branch_target < cur_PC
         && (cur_PC - state->branch_target) / 2 <= OPTIMIZE_IDLE_MAX_INSNS
         && fetch
        ) {
            const int num_insns = (cur_PC - state->branch_target) / 2;
            is_idle = can_optimize_idle((fetch+1) - num_insns,
                                        state->branch_target, num_insns);
# ifdef JIT_DEBUG_VERBOSE
            if (is_idle) {
                DMSG("Found idle loop at 0x%08X (%d instructions)",
                     (int)state->branch_target, num_insns);
            }
# endif
        }
#endif

        switch (state->branch_type) {

          case SH2BRTYPE_NONE:  // Avoid a compiler warning
            break;

          case SH2BRTYPE_STATIC: {
            if (is_idle) {
                /* See delay loop handling in DT for why we don't use
                 * LOAD_STATE_ALLOC here. */
                DECLARE_REG(cycle_limit);
                cycle_limit = STATE_CACHE_FIXED_REG(cycle_limit);
                if (!cycle_limit) {
                    ALLOC_REG(cycle_limit);
                    LOAD_STATE(cycle_limit, cycle_limit);
                }
                STORE_STATE(cycles, cycle_limit);
            }
            if (state->loop_to_jsr) {
                /* Clear the flag now, or else it'll get picked up if the
                 * subroutine call is a BSR or the target register is known */
                state->loop_to_jsr = 0;
                const uint32_t target = state->branch_target;
                RECURSIVE_DECODE(target, 0);
                RECURSIVE_DECODE(target+2, 1);
                /* The flush/jump will be performed by the subroutine call */
            } else {
                SET_PC_KNOWN(state->branch_target);
                FLUSH_STATE_CACHE();
                JUMP_STATIC();
            }
            state->just_branched = 1;
            break;
          }

          case SH2BRTYPE_DYNAMIC: {
#ifdef OPTIMIZE_VARIABLE_SHIFTS
            /* We still have to jump normally if the branch register is
             * outside the shift sequence, so create a label for that */
            CREATE_LABEL(label_do_varshift);
            int doing_varshift = 0;
            DECLARE_REG(Rcount);
            if (state->varshift_target_PC) {
                doing_varshift = 1;
                DEFINE_REG(test);
                LOAD_STATE_ALLOC(Rcount, R[state->varshift_Rcount]);
                SLTUI(test, Rcount,
                      (state->varshift_target_PC + 1) - cur_PC);
                GOTO_IF_NZ(label_do_varshift, test);
                SAVE_STATE_CACHE();
            } else {  // Not needed, but avoid a compiler warning
                Rcount = 0;
            }
#endif
            if (is_idle) {
                DECLARE_REG(cycle_limit);
                cycle_limit = STATE_CACHE_FIXED_REG(cycle_limit);
                if (!cycle_limit) {
                    ALLOC_REG(cycle_limit);
                    LOAD_STATE(cycle_limit, cycle_limit);
                }
                STORE_STATE(cycles, cycle_limit);
            }
            SET_PC(state->branch_target_reg);
            FLUSH_STATE_CACHE();
            JUMP();
#ifdef OPTIMIZE_VARIABLE_SHIFTS
            if (doing_varshift) {
                RESTORE_STATE_CACHE();
                DEFINE_LABEL(label_do_varshift);
                /* A branch distance of zero means the maximum count */
                DEFINE_REG(count_2_max);
                MOVEI(count_2_max, state->varshift_max * 2);
                DEFINE_REG(count_2);
                SUB(count_2, count_2_max, Rcount);
                DEFINE_REG(count);
                SRLI(count, count_2, 1);
                DECLARE_REG(Rshift);
                LOAD_STATE_ALLOC(Rshift, R[state->varshift_Rshift]);
                DEFINE_RESULT_REG(new_Rshift,
                                  R[state->varshift_Rshift]);
                DEFINE_REG(new_T);
                switch (state->varshift_type) {
                  case 0: {
                    DEFINE_REG(temp);
                    SUBI(temp, count, 1);
                    DEFINE_REG(temp2);
                    SLL(temp2, Rshift, temp);
                    SRLI(new_T, temp2, 31);
                    SLL(new_Rshift, Rshift, count);
                    break;
                  }
                  case 1: {
                    DEFINE_REG(temp);
                    SUBI(temp, count, 1);
                    DEFINE_REG(temp2);
                    SRL(temp2, Rshift, temp);
                    ANDI(new_T, temp2, 1);
                    SRL(new_Rshift, Rshift, count);
                    break;
                  }
                  case 2: {
                    DEFINE_REG(temp);
                    SUBI(temp, count, 1);
                    DEFINE_REG(temp2);
                    SRA(temp2, Rshift, temp);
                    ANDI(new_T, temp2, 1);
                    SRA(new_Rshift, Rshift, count);
                    break;
                  }
                  case 3: {
                    DEFINE_REG(imm_32);
                    MOVEI(imm_32, 32);
                    DEFINE_REG(right_count);
                    SUB(right_count, imm_32, count);
                    ROR(new_Rshift, Rshift, right_count);
                    ANDI(new_T, new_Rshift, 1);
                    break;
                  }
                  case 4:
                    ROR(new_Rshift, Rshift, count);
                    SRLI(new_T, new_Rshift, 31);
                    break;
                  default:
                    DMSG("Invalid shift type %u at 0x%X",
                         state->varshift_type, (unsigned int)cur_PC - 4);
                    MOVEI(new_T, 0);
                    break;
                }
                STORE_STATE(R[state->varshift_Rshift], new_Rshift);
                GET_SR_T;
                DEFINE_REG(final_T);
                SELECT(final_T, new_T, T, count);
                SET_SR_T(final_T);
                DECLARE_REG(cycles);
                LOAD_STATE_ALLOC(cycles, cycles);
                DEFINE_RESULT_REG(new_cycles, cycles);
                ADD(new_cycles, cycles, count);
                STORE_STATE(cycles, new_cycles);
                INC_PC_BY(state->varshift_target_PC - cur_PC);
                /* We fall through in this case, so don't set the
                 * just_branched flag */
                break;
            }
#endif  // OPTIMIZE_VARIABLE_SHIFTS
            state->just_branched = 1;
            break;
          }  // case SH2BRTYPE_DYNAMIC

          case SH2BRTYPE_RTE: {
            SET_PC(state->branch_target_reg);
            FLUSH_STATE_CACHE();
            DEFINE_REG(check_interrupts_funcptr);
            MOVEA(check_interrupts_funcptr, check_interrupts);
            CALL_NORET(state_reg, 0, check_interrupts_funcptr);
            JUMP();
            break;
          }  // case SH2BRTYPE_RTE

          case SH2BRTYPE_BT:
          case SH2BRTYPE_BF:
          case SH2BRTYPE_BT_S:
          case SH2BRTYPE_BF_S: {
            DECLARE_REG(cycle_limit);
            cycle_limit = STATE_CACHE_FIXED_REG(cycle_limit);
            if (!cycle_limit) {
                ALLOC_REG(cycle_limit);
                LOAD_STATE(cycle_limit, cycle_limit);
            }
            DECLARE_REG(cycles);
            LOAD_STATE_ALLOC_KEEPOFS(cycles, cycles);
            if (STATE_CACHE_FIXED_REG(SR)) {
                FLUSH_STATE_SR_T();  // Avoid needing to flush it twice
            }
            SAVE_STATE_CACHE();
            CREATE_LABEL(bt_bf_nobranch);
            if (state->branch_type == SH2BRTYPE_BT
             || state->branch_type == SH2BRTYPE_BT_S
            ) {
                GOTO_IF_Z(bt_bf_nobranch, state->branch_cond_reg);
            } else {
                GOTO_IF_NZ(bt_bf_nobranch, state->branch_cond_reg);
            }
            if (is_idle) {
                STORE_STATE(cycles, cycle_limit);
            } else {
                DEFINE_RESULT_REG(new_cycles, cycles);
                ADDI(new_cycles, cycles,
                     state->branch_cycles + STATE_CACHE_OFFSET(cycles));
                STORE_STATE(cycles, new_cycles);
            }
            if (state->branch_targets_rts) {
                SET_PC(state->branch_target_reg);
                FLUSH_STATE_CACHE();
                JUMP();
            } else if (state->loop_to_jsr) {
                state->loop_to_jsr = 0;
                const uint32_t target = state->branch_target;
                RECURSIVE_DECODE(target, 0);
                RECURSIVE_DECODE(target+2, 1);
            } else {
                SET_PC_KNOWN(state->branch_target);
                WRITEBACK_STATE_CACHE(); // Avoid stores of fixed registers
                JUMP_STATIC();
            }
            DEFINE_LABEL(bt_bf_nobranch);
            RESTORE_STATE_CACHE();
            /* We don't set state->just_branched here, because the code
             * will fall through if the condition isn't met */
            break;
          }  // case SH2BRTYPE_B{T,F}{,_S}

          case SH2BRTYPE_FOLDED: {
            /* Handle cleanup for this instruction first. */
            OPCODE_DONE(opcode);
            /* Clear branch_type now (rather than at the end) so the
             * recursive decode doesn't see it. */
            state->branch_type = SH2BRTYPE_NONE;
            /* If it's a native implementation, just call it (but remember
             * to update PC and flush the state block cache first). */
            if (state->branch_fold_native) {
                SET_PC_KNOWN(state->branch_target);
                FLUSH_STATE_CACHE();
                unsigned int i;
                for (i = 0; i < 8; i++) {
                    PTR_CLEAR(i);
                    REG_SETKNOWN(REG_R(i), 0);
                }
                DEFINE_REG(funcptr_reg);
                MOVEA(funcptr_reg, state->branch_fold_native);
                CALL_NORET(state_reg, 0, funcptr_reg);
            } else {
                /* Fold in the contents of the called subroutine.  We don't
                 * accept subroutines with branches in the first place, so
                 * we just recursively decode one instruction at a time
                 * until we reach RTS, then process the RTS manually,
                 * decode the delay slot, and start back up with the
                 * instruction after the BSR/JSR's delay slot. */
                state->folding_subroutine = 1;
                uint32_t sub_PC = state->branch_target;
                const uint16_t *sub_fetch =
                    BRANCH_FOLD_TARGET_FETCH(state->branch_target);
                for (;;) {
                    uint16_t sub_opcode;
                    if (sub_fetch) {
                        sub_opcode = *sub_fetch++;
                    } else {
                        sub_opcode = MappedMemoryReadWord(sub_PC);
                    }
                    if (sub_opcode == 0x000B) {  // RTS
                        break;
                    }
                    RECURSIVE_DECODE(sub_PC, 0);
                    sub_PC += 2;
                }
                RECURSIVE_DECODE(sub_PC, 0);
                RECURSIVE_DECODE(sub_PC+2, 1);
                state->folding_subroutine = 0;
                DEFINE_REG(post_fold_PC);
                MOVEI(post_fold_PC, cur_PC);
                SET_PC(post_fold_PC);
            }  // if (state->branch_fold_native)
            break;
          }  // case SH2BRTYPE_FOLDED

        }  // switch (state->branch_type)

        state->branch_type = SH2BRTYPE_NONE;
#ifdef OPTIMIZE_VARIABLE_SHIFTS
        state->varshift_target_PC = 0;
#endif
        state->branch_targets_rts = 0;
        state->loop_to_jsr = 0;

    }  // if we need to branch

    /* Check for interrupts if necessary */

    if (state->need_interrupt_check) {
        state->need_interrupt_check = 0;
        FLUSH_STATE_CACHE();
        DEFINE_REG(check_interrupts_funcptr);
        MOVEA(check_interrupts_funcptr, check_interrupts);
        DEFINE_REG(result);
        CALL(result, state_reg, 0, check_interrupts_funcptr);
        CREATE_LABEL(nointr);
        GOTO_IF_Z(nointr, result); {
            RETURN();
        } DEFINE_LABEL(nointr);
    }

    /* Invalid opcode handler (jumped to on detection of an invalid opcode) */

    if (0) {
      invalid:
        if (invalid_opcode_callback) {
            (*invalid_opcode_callback)(state, cur_PC, opcode);
        }
        cur_cycles++;
        DEFINE_REG(PC);
        MOVEI(PC, cur_PC);
        INC_PC();  // So we don't get stuck when translating
        TAKE_EXCEPTION(state->delay ? 6 : 4, PC);
    }

    /* All done; perform any cleanup requested and return the instruction's
     * opcode to the caller. */

    OPCODE_DONE(opcode);
    return opcode;

}  // End of decode_insn()

/*************************************************************************/

#undef GET_REG
#undef GET_R0
#undef GET_R0_W
#undef GET_R0_B
#undef GET_R15
#undef GET_Rn
#undef GET_Rm
#undef GET_Rm_W
#undef GET_Rm_B
#undef GET_SR
#undef GET_SR_T
#undef GET_GBR
#undef GET_VBR
#undef GET_MACH
#undef GET_MACL
#undef GET_PR
#undef GET_MACH_COPY
#undef GET_MACL_COPY
#undef GET_REG_KEEPOFS
#undef GET_R0_KEEPOFS
#undef GET_R15_KEEPOFS
#undef GET_Rn_KEEPOFS
#undef GET_Rm_KEEPOFS
#undef GET_GBR_KEEPOFS

#undef COPY_FROM_Rn
#undef COPY_TO_Rn
#undef DEFINE_RESULT_REG

#undef SET_R0
#undef SET_R15
#undef SET_Rn
#undef SET_Rm
#undef SET_SR
#undef SET_SR_T
#undef SET_GBR
#undef SET_VBR
#undef SET_MACH
#undef SET_MACL
#undef SET_PR
#undef SET_PC
#undef SET_PC_KNOWN

#undef ADDI_R0
#undef ADDI_R15
#undef ADDI_Rn
#undef ADDI_Rm
#undef ADDI_R0_NOREG
#undef ADDI_R15_NOREG
#undef ADDI_Rn_NOREG
#undef ADDI_Rm_NOREG
#undef ADD_CYCLES

#undef INCDEC_B
#undef INCDEC_W
#undef INCDEC_L
#undef LOAD_Rm
#undef LOAD_disp_Rm
#undef LOAD_R0_Rm
#undef LOAD_Rm_inc
#undef LOAD_Rn
#undef LOAD_Rn_inc
#undef LOAD_disp_GBR
#undef LOAD_R0_GBR
#undef STORE_Rn
#undef STORE_disp_Rn
#undef STORE_dec_Rn
#undef STORE_R0_Rn
#undef STORE_disp_Rm
#undef STORE_disp_GBR
#undef STORE_SAVED_R0_GBR

#undef TAKE_EXCEPTION
#undef GET_NEXT_OPCODE_FOR_SHIFT_CACHE
#undef DEBUG_PRINT_ONCE

/*************************************************************************/

/*
 * Local variables:
 *   mode: c
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
