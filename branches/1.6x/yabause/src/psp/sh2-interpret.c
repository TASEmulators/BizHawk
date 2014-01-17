/*  src/psp/sh2-interpret.c: Instruction interpreter for SH-2 emulator
                             (mostly for debugging)
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
/*************************** Required headers ****************************/
/*************************************************************************/

#include "common.h"

#include "../sh2core.h"

#include "sh2.h"
#include "sh2-internal.h"

/*************************************************************************/
/***************** SH-2 interpreted execution interface ******************/
/*************************************************************************/

/* Declare a register identifier, but don't allocate a new register for it */
#define DECLARE_REG(name)  uintptr_t name

/* Allocate a register for a declared identifier */
#define ALLOC_REG(name)  /*nothing*/

/* Define a new register (equivalent to DECLARE_REG followed by ALLOC_REG) */
#define DEFINE_REG(name)  uintptr_t name

/* Register-register operations */
#define MOVE(dest,src)          ((dest) = (src))
#define SELECT(dest,src1,src2,cond)  ((dest) = (cond) ? (src1) : (src2))
#define ADD(dest,src1,src2)     ((dest) = (src1) + (intptr_t)(int32_t)(src2))
#define SUB(dest,src1,src2)     ((dest) = (src1) - (intptr_t)(int32_t)(src2))
#define MUL(dest,src1,src2)     ((dest) = (uint32_t)((src1) * (src2)))
#define MULU_64(dest,src1,src2,dest_hi)  do {                   \
    (dest)    = (uint32_t)((src1) * (src2));                    \
    (dest_hi) = (uint64_t)((uint64_t)(uint32_t)(src1)           \
                           * (uint64_t)(uint32_t)(src2)) >> 32; \
} while (0)
#define MULS_64(dest,src1,src2,dest_hi)  do {                   \
    (dest)    = (uint32_t)((src1) * (src2));                    \
    (dest_hi) = (int64_t)((int64_t)(int32_t)(src1)              \
                           * (int64_t)(int32_t)(src2)) >> 32;   \
} while (0)
#define MADDU_64(dest,src1,src2,dest_hi)  do {                  \
    uint64_t initial = (uint64_t)(uint32_t)(dest_hi) << 32      \
                     | (uint64_t)(uint32_t)(dest);              \
    uint64_t product = (uint64_t)(uint32_t)(src1)               \
                     * (uint64_t)(uint32_t)(src2);              \
    uint64_t result = initial + product;                        \
    (dest)    = (uint32_t)result;                               \
    (dest_hi) = (uint32_t)(result >> 32);                       \
} while (0)
#define MADDS_64(dest,src1,src2,dest_hi)  do {                  \
    uint64_t initial = (int64_t)(int32_t)(dest_hi) << 32        \
                     | (int64_t)(uint32_t)(dest);               \
    uint64_t product = (int64_t)(int32_t)(src1)                 \
                     * (int64_t)(int32_t)(src2);                \
    uint64_t result = initial + product;                        \
    (dest)    = (uint32_t)result;                               \
    (dest_hi) = (uint32_t)(result >> 32);                       \
} while (0)
#define DIVMODU(dest,src1,src2,rem)  do {                       \
    if ((src2) != 0) {                                          \
        (dest) = (uint32_t)(src1) / (uint32_t)(src2);           \
        (rem)  = (uint32_t)(src1) % (uint32_t)(src2);           \
    } else {                                                    \
        /* Have to set these to avoid a compiler warning */     \
        (dest) = 0;                                             \
        (rem) = 0;                                              \
    }                                                           \
} while (0)
#define DIVMODS(dest,src1,src2,rem)  do {                       \
    if ((src2) != 0) {                                          \
        (dest) = (int32_t)(src1) / (int32_t)(src2);             \
        (rem)  = (int32_t)(src1) % (int32_t)(src2);             \
    } else {                                                    \
        /* Have to set these to avoid a compiler warning */     \
        (dest) = 0;                                             \
        (rem) = 0;                                              \
    }                                                           \
} while (0)
#define AND(dest,src1,src2)     ((dest) = (src1) & (intptr_t)(int32_t)(src2))
#define OR(dest,src1,src2)      ((dest) = (src1) | (uintptr_t)(uint32_t)(src2))
#define XOR(dest,src1,src2)     ((dest) = (src1) ^ (uintptr_t)(uint32_t)(src2))
#define NOT(dest,src)           ((dest) = (uint32_t)(~(src)))
#define SLL(dest,src1,src2)     ((dest) = (uint32_t)((src1) << (src2)))
#define SRL(dest,src1,src2)     ((dest) = (uint32_t)(src1) >> (src2))
#define SRA(dest,src1,src2)     ((dest) = (int32_t)(src1) >> (src2))
#define ROR(dest,src1,src2) \
    ((dest) = (((src2) & 31)                              \
               ? (uint32_t)(src1) >> ((src2) & 31)        \
                 | (uint32_t)(src1) << (31-((src2) & 31)) \
               : (uint32_t)(src1)))
#ifdef __GNUC__
# define CLZ(dest,src)          ((dest) = __builtin_clz((src)))
#else
# define CLZ(dest,src)  do {    \
    uint32_t __temp = (src);    \
    (dest) = 32;                \
    while (__temp) {            \
        __temp >>= 1;           \
        (dest)--;               \
    }                           \
} while (0)
#endif  // __GNUC__
#define CLO(dest,src)  do {     \
    uint32_t __temp = (src);    \
    (dest) = 0;                 \
    while ((int32_t)__temp < 0) {\
        __temp <<= 1;           \
        (dest)++;               \
    }                           \
} while (0)
#define SLTU(dest,src1,src2)    ((dest) = (uint32_t)(src1) < (uint32_t)(src2))
#define SLTS(dest,src1,src2)    ((dest) = (int32_t)(src1) < (int32_t)(src2))
#define BSWAPH(dest,src) \
    ((dest) = ((uint32_t)(src) & 0xFF00FF00) >> 8 \
            | ((uint32_t)(src) & 0x00FF00FF) << 8)
#define BSWAPW(dest,src) \
    ((dest) = ((uint32_t)(src) & 0xFF000000) >> 24 \
            | ((uint32_t)(src) & 0x00FF0000) >>  8 \
            | ((uint32_t)(src) & 0x0000FF00) <<  8 \
            | ((uint32_t)(src) & 0x000000FF) << 24)
#define HSWAPW(dest,src) \
    ((dest) = ((uint32_t)(src) & 0xFFFF0000) >> 16 \
            | ((uint32_t)(src) & 0x0000FFFF) << 16)

/* Register-immediate operations */
#define MOVEI(dest,imm)         ((dest) = (imm))
#define MOVEA(dest,addr)        ((dest) = (uintptr_t)(addr))
#define ADDI(dest,src,imm)      ((dest) = (src) + (imm))
#define SUBI(dest,src,imm)      ((dest) = (src) - (imm))
#define ANDI(dest,src,imm)      ((dest) = (uint32_t)((src) & (imm)))
#define ORI(dest,src,imm)       ((dest) = (uint32_t)((src) | (imm)))
#define XORI(dest,src,imm)      ((dest) = (src) ^ (imm))
#define SLLI(dest,src,imm)      ((dest) = (uint32_t)((src) << (imm)))
#define SRLI(dest,src,imm)      ((dest) = (uint32_t)(src) >> (imm))
#define SRAI(dest,src,imm)      ((dest) = (int32_t)(src) >> (imm))
#define RORI(dest,src,imm) \
    ((dest) = (((imm) & 31)                             \
               ? (uint32_t)(src) >> ((imm) & 31)        \
                 | (uint32_t)(src) << (32-((imm) & 31)) \
               : (uint32_t)(src)))
#define SLTUI(dest,src,imm)     ((dest) = (uint32_t)(src) < (uint32_t)(imm))
#define SLTSI(dest,src,imm)     ((dest) = (int32_t)(src) < (int32_t)(imm))

/* Bitfield operations */
#define BFEXT(dest,src,start,count) \
    ((dest) = ((uint32_t)(src) >> (start)) & ((1 << (count)) - 1))
#define BFINS(dest,src1,src2,start,count) \
    ((dest) = ((uint32_t)(src1) & ~(((1 << (count)) - 1) << (start))) \
            | (((uint32_t)(src2) & ((1 << (count)) - 1)) << (start)))

/* Variants of SLT */
#define SEQZ(dest,src)  SLTUI((dest), (src), 1)
#define SLTZ(dest,src)  SLTSI((dest), (src), 0)

/* Load from or store to memory */
#define LOAD_BU(dest,address,offset) \
    ((dest) = *(uint8_t *)((address)+(offset)))
#define LOAD_BS(dest,address,offset) \
    ((dest) = *(int8_t *)((address)+(offset)))
#define LOAD_HU(dest,address,offset) \
    ((dest) = *(uint16_t *)((address)+(offset)))
#define LOAD_HS(dest,address,offset) \
    ((dest) = *(int16_t *)((address)+(offset)))
#define LOAD_W(dest,address,offset) \
    ((dest) = *(uint32_t *)((address)+(offset)))
#define LOAD_PTR(dest,address,offset) \
    ((dest) = *(uintptr_t *)((address)+(offset)))
#define STORE_B(address,src,offset) \
    (*(uint8_t *)((address)+(offset)) = (src))
#define STORE_H(address,src,offset) \
    (*(uint16_t *)((address)+(offset)) = (src))
#define STORE_W(address,src,offset) \
    (*(uint32_t *)((address)+(offset)) = (src))
#define STORE_PTR(address,src,offset) \
    (*(uintptr_t *)((address)+(offset)) = (src))

/* Load from, store to, or add constants to state block fields */
#define LOAD_STATE(reg,field)        ((reg) = state->field)
#define LOAD_STATE_PTR(reg,field)    ((reg) = (uintptr_t)state->field)
#define LOAD_STATE_SR_T(reg)         ((reg) = (state->SR & SR_T) >> SR_T_SHIFT)
#define STORE_STATE(field,reg)       (state->field = (reg))
#define STORE_STATE_PC(value)        (state->PC = (value))
#define STORE_STATE_B(field,reg)     (state->field = (reg))
#define STORE_STATE_PTR(field,reg)   (state->field = (void *)(reg))
#define STORE_STATE_SR_T(reg)        (state->SR &= ~SR_T, \
                                      state->SR |= ((reg) & 1) << SR_T_SHIFT)
#define FLUSH_STATE_SR_T()           /*nothing*/
#define RESET_STATE_SR_T()           /*nothing*/
#define ADDI_STATE(field,imm,reg)    (state->field = (reg) + (imm))
#define ADDI_STATE_NOREG(field,imm)  (state->field += (imm))

/* Load from a state block field, but don't change the state block cache */
#define LOAD_STATE_COPY(name,field)  LOAD_STATE(name,field)

/* Allocate a new register and load it from the state block, or reuse an
 * old register if appropriate */
#define LOAD_STATE_ALLOC(name,field)  ALLOC_REG(name); LOAD_STATE(name,field)

/* Allocate a new register and load it from the state block, or reuse an
 * old register (leaving any offset in the cache) if appropriate */
#define LOAD_STATE_ALLOC_KEEPOFS(name,field)  LOAD_STATE_ALLOC(name,field)

/* Execute an SH-2 load or store operation (note that size desginations are
 * SH-2 style B[yte]/W[ord]/L[ong] rather than RTL B[yte]/H[alfword]/W[ord],
 * and all 8- and 16-bit loads are signed) */

#define SH2_LOAD_B(dest,address) \
    ((dest) = (int8_t)MappedMemoryReadByte((address)))
#define SH2_LOAD_W(dest,address) \
    ((dest) = (int16_t)MappedMemoryReadWord((address)))
#define SH2_LOAD_L(dest,address) \
    ((dest) = MappedMemoryReadLong((address)))

#ifdef TRACE
# define LOG_STORE(address,src,type)  ((*trace_store##type##_callback)((address), (src)))
#else
# define LOG_STORE(address,src,type)  /*nothing*/
#endif
#define SH2_STORE_B(address,src)  do {          \
    LOG_STORE((address), (src), b);             \
    MappedMemoryWriteByte((address), (src));    \
} while (0)
#define SH2_STORE_W(address,src)  do {          \
    LOG_STORE((address), (src), w);             \
    MappedMemoryWriteWord((address), (src));    \
} while (0)
#define SH2_STORE_L(address,src)  do {          \
    LOG_STORE((address), (src), l);             \
    MappedMemoryWriteLong((address), (src));    \
} while (0)

/* Execute an SH-2 load or store to a known address */
#define SH2_LOAD_ABS_B(dest,address)  SH2_LOAD_B(dest,address)
#define SH2_LOAD_ABS_W(dest,address)  SH2_LOAD_W(dest,address)
#define SH2_LOAD_ABS_L(dest,address)  SH2_LOAD_L(dest,address)
#define SH2_STORE_ABS_B(address,src,islocal)  SH2_STORE_B(address,src)
#define SH2_STORE_ABS_W(address,src,islocal)  SH2_STORE_W(address,src)
#define SH2_STORE_ABS_L(address,src,islocal)  SH2_STORE_L(address,src)

/* Execute an SH-2 load or store through an SH-2 register */
#define SH2_LOAD_REG_B(dest,sh2reg,offset,postinc)  do {        \
    SH2_LOAD_B(dest, state->R[sh2reg] + (offset));              \
    if (postinc) {                                              \
        state->R[sh2reg] += 1;                                  \
    }                                                           \
} while (0)
#define SH2_LOAD_REG_W(dest,sh2reg,offset,postinc)  do {        \
    SH2_LOAD_W(dest, state->R[sh2reg] + (offset));              \
    if (postinc) {                                              \
        state->R[sh2reg] += 2;                                  \
    }                                                           \
} while (0)
#define SH2_LOAD_REG_L(dest,sh2reg,offset,postinc)  do {        \
    SH2_LOAD_L(dest, state->R[sh2reg] + (offset));              \
    if (postinc) {                                              \
        state->R[sh2reg] += 4;                                  \
    }                                                           \
} while (0)
#define SH2_STORE_REG_B(sh2reg,src,offset,predec)  do {         \
    if (predec) {                                               \
        state->R[sh2reg] -= 1;                                  \
    }                                                           \
    SH2_STORE_B(state->R[sh2reg] + (offset), src);              \
} while (0)
#define SH2_STORE_REG_W(sh2reg,src,offset,predec)  do {         \
    if (predec) {                                               \
        state->R[sh2reg] -= 2;                                  \
    }                                                           \
    SH2_STORE_W(state->R[sh2reg] + (offset), src);              \
} while (0)
#define SH2_STORE_REG_L(sh2reg,src,offset,predec)  do {         \
    if (predec) {                                               \
        state->R[sh2reg] -= 4;                                  \
    }                                                           \
    SH2_STORE_L(state->R[sh2reg] + (offset), src);              \
} while (0)

/* Branches (within an SH-2 instruction's RTL code) */
#define CREATE_LABEL(label)      /*nothing*/
#define DEFINE_LABEL(label)      label:
#define GOTO_LABEL(label)        goto label;
#define GOTO_IF_Z(label,reg)     if ((reg) == 0) goto label;
#define GOTO_IF_NZ(label,reg)    if ((reg) != 0) goto label;
#define GOTO_IF_E(label,reg1,reg2)   if ((reg1) == (reg2)) goto label;
#define GOTO_IF_NE(label,reg1,reg2)  if ((reg1) != (reg2)) goto label;

/* Jumps (to other SH-2 instructions) */
#define JUMP_STATIC()            jumped = 1
#define JUMP()                   jumped = 1

/* Call to a native subroutine */
#define CALL(result,arg1,arg2,func)  do { \
    FASTCALL uintptr_t (*__func)(uintptr_t,uintptr_t) = (void *)(uintptr_t)(func); \
    (result) = (*__func)((arg1), (arg2)); \
} while (0)
#define CALL_NORET(arg1,arg2,func)  do { \
    FASTCALL void (*__func)(uintptr_t,uintptr_t) = (void *)(uintptr_t)(func); \
    (*__func)((arg1), (arg2)); \
} while (0)

/* Return from the current block */
#define RETURN()                 return 0

/*-----------------------------------------------------------------------*/

/* We don't have "registers", so alias state_reg directly to the pointer */
#define state_reg   ((uintptr_t)state)

/* Access the state block directly for the PC */
#define cur_PC      (state->PC)

/* No direct fetching */
#define fetch       ((uint16_t *)NULL)  // uint16_t * to avoid compiler errors

/* No pre- or post-decode processing needed */
#define OPCODE_INIT(opcode)  /*nothing*/
#define OPCODE_DONE(opcode)  /*nothing*/

/* cur_PC and REG_PC are the same thing, so only need to update one of them
 * (but only do so for the default case if we didn't already set the PC via
 * a jump) */
#define INC_PC()  do {  \
    if (!jumped) {      \
        cur_PC += 2;    \
    }                   \
} while (0)
#define INC_PC_BY(amount)  (cur_PC += (amount))

/* Return whether the word at "offset" words from the current instruction
 * is available for peephole optimization */
#define INSN_IS_AVAILABLE(offset)  0

/* Return the "high" (pointer register) and "low" (load/store offset) parts
 * of an address for generating optimal native load/store code */
#define ADDR_HI(address)        ((uintptr_t)address)
#define ADDR_LO(address)        0

/* Return whether the saturation check for MAC can be omitted */
#define CAN_OMIT_MAC_S_CHECK    0

/* Get or set whether the MACL/MACH pair is known to be zero */
#define MAC_IS_ZERO()           0
#define SET_MAC_IS_ZERO()       /*nothing*/
#define CLEAR_MAC_IS_ZERO()     /*nothing*/

/* Get, add to, or clear the cached shift count */
#define CAN_CACHE_SHIFTS()      0
#define CACHED_SHIFT_COUNT()    0
#define ADD_TO_SHIFT_CACHE(n)   /*nothing*/
#define CLEAR_SHIFT_CACHE()     /*nothing*/

/* Get or set register known bits and values */
#define REG_GETKNOWN(reg)       0
#define REG_GETVALUE(reg)       0
#define REG_SETKNOWN(reg,value) /*nothing*/
#define REG_SETVALUE(reg,value) /*nothing*/

/* Track pointer registers */
#define PTR_ISLOCAL(reg)        0
#define PTR_SETLOCAL(reg)       /*nothing*/
#define PTR_SET_SOURCE(reg,address)  /*nothing*/
#define PTR_CHECK(reg)          0
#define PTR_COPY(reg,new,for_add)    /*nothing*/
#define PTR_CLEAR(reg)          /*nothing*/

/* Save the current cache state */
#define SAVE_STATE_CACHE()      /*nothing*/
/* Restore the saved cache state */
#define RESTORE_STATE_CACHE()   /*nothing*/
/* Write back to the state block any cached, dirty state block values
 * (but leave them dirty) */
#define WRITEBACK_STATE_CACHE() /*nothing*/
/* Flush all cached state block values */
#define FLUSH_STATE_CACHE()     /*nothing*/
/* Return the cached offset for the given state block field, or 0 if none */
#define STATE_CACHE_OFFSET(field)  0
/* Return the fixed RTL register to use for the given state block field,
 * or 0 if none */
#define STATE_CACHE_FIXED_REG(field)  0
/* Return whether the given state block field has a fixed RTL register that
 * can be modified */
#define STATE_CACHE_FIXED_REG_WRITABLE(field)  0
/* Clear any fixed RTL register for the given state block field */
#define STATE_CACHE_CLEAR_FIXED_REG(field)  /*nothing*/

/* Check the status of a branch instruction */
#define BRANCH_FALLS_THROUGH(addr)          0
#define BRANCH_TARGETS_RTS(addr)            0
#define BRANCH_IS_THREADED(addr)            0
#define BRANCH_THREAD_TARGET(addr)          0
#define BRANCH_THREAD_COUNT(addr)           0
#define BRANCH_IS_SELECT(addr)              0
#define BRANCH_IS_LOOP_TO_JSR(addr)         0
#define BRANCH_IS_FOLDABLE_SUBROUTINE(addr) 0
#define BRANCH_FOLD_TARGET(addr)            0
#define BRANCH_FOLD_TARGET_FETCH(addr)      NULL
#define BRANCH_FOLD_NATIVE_FUNC(addr)       NULL

/*************************************************************************/

/**
 * decode_insn:  Decode a single SH-2 instruction.  Implements
 * interpret_insn() using the shared decoder core.
 *
 * [Parameters]
 *          state: SH-2 processor state
 *     initial_PC: Equal to state->PC (used by OPTIMIZE_IDLE)
 *         jumped: Local register tracking whether a jump was performed
 * [Return value]
 *     Decoded SH-2 opcode (not used)
 */
#define DECODE_INSN_INLINE  NOINLINE
#define DECODE_INSN_PARAMS \
    SH2State *state, uint32_t initial_PC, int jumped
#define RECURSIVE_DECODE(address,is_last)  do { \
    const uint32_t saved_PC = state->PC;        \
    state->PC = (address);                      \
    interpret_insn(state);                      \
    state->PC = saved_PC;                       \
} while (0)
#include "sh2-core.i"

/*-----------------------------------------------------------------------*/

/**
 * interpret_insn:  Interpret and execute a single SH-2 instruction at the
 * current PC.
 *
 * [Parameters]
 *     state: SH-2 processor state
 * [Return value]
 *     None
 */
void interpret_insn(SH2State *state)
{
    /* Make sure we're not trying to execute from an odd address */
    if (UNLIKELY(state->PC & 1)) {
        /* Push SR and PC */
        state->R[15] -= 4;
        MappedMemoryWriteLong(state->R[15], state->SR);
        state->R[15] -= 4;
        MappedMemoryWriteLong(state->R[15], state->PC);
        /* Jump to the instruction address error exception vector (9) */
        state->PC = MappedMemoryReadLong(9<<2);
    }

    decode_insn(state, state->PC, 0);

    if (UNLIKELY(state->delay)) {
        /* Don't treat the instruction after a not-taken conditional branch
         * as a delay slot.  (Note that when interpreting, the
         * branch_cond_reg field holds the actual value of the condition.) */
        if (!(state->branch_type == SH2BRTYPE_BT_S && !state->branch_cond_reg)
         && !(state->branch_type == SH2BRTYPE_BF_S && state->branch_cond_reg)
        ) {
            /* Make sure we interpret the delay slot immediately, so (1) we
             * don't try to translate it as the beginning of a block and
             * (2) we don't let any exceptions get in the way (SH7604
             * manual page 75, section 4.6.1: exceptions are not accepted
             * when processing a delay slot). */
            decode_insn(state, state->PC, 0);
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
