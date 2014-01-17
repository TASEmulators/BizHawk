/*  src/psp/sh2-optimize.c: Optimization routines for SH-2 emulator
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

#include "../core.h"

#include "rtl.h"
#include "sh2.h"
#include "sh2-internal.h"

#ifdef JIT_DEBUG_TRACE
# include "../sh2d.h"
#endif

/*************************************************************************/
/****************************** Common data ******************************/
/*************************************************************************/

#ifdef OPTIMIZE_IDLE

/*
 * The table below is used by OPTIMIZE_IDLE to find the effects of a given
 * SH-2 instruction on machine state.  Instruction opcodes are listed
 * hierarchically, starting with idle_table_main[], which is indexed by the
 * top 4 bits of the opcode (bits 12-15).  Each table entry is either a
 * pointer to a subtable with a shift count indicating which 4 bits of the
 * opcode are used to index the subtable, or an instruction definition
 * indicating the effects of the given set of opcodes.
 *
 * Instruction definitions consist of "used" and "changed" bitmasks,
 * indicating which registers are used (read) or changed (written) by the
 * instruction, respectively.  For example, the instruction ADD Rm,Rn uses
 * both registers Rm and Rn and changes register Rn, so it is defined as:
 *
 *     {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn}
 *
 * IDLE_Rm and IDLE_Rn in this example are pseudo-register flags, and are
 * interpreted to mean the registers specified by the "m" and "n" fields of
 * the opcode (bits 4-7 and 8-11 respectively).  There are also individual
 * flags for each of the individually-alterable fields in the SR register
 * (T, S, Q, and M); the I field can only be altered by writing SR as a
 * whole, and is not used directly by any instruction in any case.
 *
 * Instructions marked as IDLE_BAD in the "changed" field can never be part
 * of an idle loop, either because the opcode group itself is invalid, or
 * because the instruction modifies machine state in a fashion which is
 * either nonrepeatable (such as postincrement or predecrement memory
 * accesses) or not trivially repeatable (such as subroutine calls).
 *
 * The table also includes an "extra_cycles" field, used to indicate
 * instructions which require more than one clock cycle to complete.  This
 * information is not currently used, but is stored in case it becomes
 * useful in the future for computing the duration of a loop.
 */

/*-----------------------------------------------------------------------*/

/* Bit values for register bitmasks */
#define IDLE_R(n)  (1U << (n))
#define IDLE_SR_T  (1U << 16)
#define IDLE_SR_S  (1U << 17)
#define IDLE_SR_Q  (1U << 18)
#define IDLE_SR_M  (1U << 19)
#define IDLE_SR_MQT (IDLE_SR_T | IDLE_SR_Q | IDLE_SR_M)
#define IDLE_SR    (IDLE_SR_T | IDLE_SR_S | IDLE_SR_Q | IDLE_SR_M)
#define IDLE_GBR   (1U << 20)
#define IDLE_VBR   (1U << 21)
#define IDLE_PR    (1U << 22)
#define IDLE_MACL  (1U << 23)
#define IDLE_MACH  (1U << 24)
#define IDLE_MAC   (IDLE_MACL | IDLE_MACH)

/* Value used in IdleInfo.changed field to indicate an instruction which
 * can never be part of an idle loop */
#define IDLE_BAD   (1U << 31)

/* Virtual bits used for the Rn (bits 8-11) and Rm (bits 4-7) fields of
 * the instruction; e.g. IDLE_Rn translates to IDLE_R(opcode>>8 & 0xF) */
#define IDLE_Rn    (1U << 30)
#define IDLE_Rm    (1U << 29)

/* Table data structure; fields "used" and "changed" are ignored if
 * "subtable" is non-NULL */
typedef struct IdleInfo_ IdleInfo;
struct IdleInfo_ {
    uint32_t used;        // Bitmask of registers used by the instruction
    uint32_t changed;     // Bitmask of registers changed by the instruction
    uint8_t extra_cycles; // Clock cycles used by instruction minus 1
    uint8_t next_shift;   // Bit position of subtable index (0, 4, or 8)
    const IdleInfo *subtable; // NULL if no subtable for this opcode
};

/*-----------------------------------------------------------------------*/

/* Opcode table for $0xx2 opcodes */
static const IdleInfo idle_table_0xx2[16] = {
    {.used = IDLE_SR, .changed = IDLE_Rn},  // STC SR,Rn
    {.used = IDLE_GBR, .changed = IDLE_Rn}, // STC GBR,Rn
    {.used = IDLE_VBR, .changed = IDLE_Rn}, // STC VBR,Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $0xx3 opcodes */
static const IdleInfo idle_table_0xx3[16] = {
    {.changed = IDLE_BAD},              // BSRF Rn
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_Rn, .changed = 0, .extra_cycles = 1}, // BRAF Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $0xx8 opcodes */
static const IdleInfo idle_table_0xx8[16] = {
    {.used = 0, .changed = IDLE_SR_T},  // CLRT
    {.used = 0, .changed = IDLE_SR_T},  // SETT
    {.used = 0, .changed = IDLE_MAC},   // CLRMAC
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $0xx9 opcodes */
static const IdleInfo idle_table_0xx9[16] = {
    {.used = 0, .changed = 0},          // NOP
    {.used = 0, .changed = IDLE_SR_MQT}, // DIV0U
    {.used = IDLE_SR_T, .changed = IDLE_Rn}, // MOVT Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

static const IdleInfo idle_table_0xxA[16] = {
    {.used = IDLE_MACH, .changed = IDLE_Rn}, // STS MACH,Rn
    {.used = IDLE_MACL, .changed = IDLE_Rn}, // STS MACL,Rn
    {.used = IDLE_PR, .changed = IDLE_Rn},   // STS PR,Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $0xxx opcodes */
static const IdleInfo idle_table_0xxx[16] = {
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.next_shift = 4, .subtable = idle_table_0xx2},
    {.next_shift = 4, .subtable = idle_table_0xx3},

    {.changed = IDLE_BAD},              // MOV.B Rm,@(R0,Rn)
    {.changed = IDLE_BAD},              // MOV.W Rm,@(R0,Rn)
    {.changed = IDLE_BAD},              // MOV.L Rm,@(R0,Rn)
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_MACL, .extra_cycles = 1}, // MUL.L Rm,Rn

    {.next_shift = 4, .subtable = idle_table_0xx8},
    {.next_shift = 4, .subtable = idle_table_0xx9},
    {.next_shift = 4, .subtable = idle_table_0xxA},
    {.changed = IDLE_BAD},              // RTS, SLEEP, RTE

    {.used = IDLE_R(0)|IDLE_Rm, .changed = IDLE_Rn}, // MOV.B @(R0,Rm),Rn
    {.used = IDLE_R(0)|IDLE_Rm, .changed = IDLE_Rn}, // MOV.W @(R0,Rm),Rn
    {.used = IDLE_R(0)|IDLE_Rm, .changed = IDLE_Rn}, // MOV.L @(R0,Rm),Rn
    {.changed = IDLE_BAD},              // MAC.L @Rm+,@Rn+
};

/*----------------------------------*/

/* Opcode table for $2xxx opcodes */
static const IdleInfo idle_table_2xxx[16] = {
    {.changed = IDLE_BAD},              // MOV.B Rm,@Rn
    {.changed = IDLE_BAD},              // MOV.W Rm,@Rn
    {.changed = IDLE_BAD},              // MOV.L Rm,@Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // MOV.B Rm,@-Rn
    {.changed = IDLE_BAD},              // MOV.W Rm,@-Rn
    {.changed = IDLE_BAD},              // MOV.L Rm,@-Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_MQT}, // DIV0S Rm,Rn

    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // TST Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn},   // AND Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn},   // XOR Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn},   // OR Rm,Rn

    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/ST Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn},   // XTRCT Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_MACL}, // MULU.W Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_MACL}, // MULS.W Rm,Rn
};

/*----------------------------------*/

/* Opcode table for $3xxx opcodes */
static const IdleInfo idle_table_3xxx[16] = {
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/EQ Rm,Rn
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/HS Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/GE Rm,Rn

    {.changed = IDLE_BAD},              // DIV1 Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_MAC, .extra_cycles = 1}, // DMULU.L Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/HI Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_SR_T}, // CMP/GT Rm,Rn

    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn}, // SUB Rm,Rn
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_Rm|IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // SUBC Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // SUBV Rm,Rn

    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_Rn}, // ADD Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn, .changed = IDLE_MAC, .extra_cycles = 1}, // DMULS.L Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // ADDC Rm,Rn
    {.used = IDLE_Rm|IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // ADDV Rm,Rn
};

/*----------------------------------*/

/* Opcode table for $4xx0 opcodes */
static const IdleInfo idle_table_4xx0[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // SHLL Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // DT Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // SHAL Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xx1 opcodes */
static const IdleInfo idle_table_4xx1[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // SHLR Rn
    {.used = IDLE_Rn, .changed = IDLE_SR_T}, // CMP/PZ Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // SHAR Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xx4 opcodes */
static const IdleInfo idle_table_4xx4[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // ROTL Rn
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // ROTCL Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xx5 opcodes */
static const IdleInfo idle_table_4xx5[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn|IDLE_SR_T}, // ROTR Rn
    {.used = IDLE_Rn, .changed = IDLE_SR_T}, // CMP/PL Rn
    {.used = IDLE_Rn|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // ROTCR Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xx8 opcodes */
static const IdleInfo idle_table_4xx8[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLL2 Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLL8 Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLL16 Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xx9 opcodes */
static const IdleInfo idle_table_4xx9[16] = {
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLR2 Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLR8 Rn
    {.used = IDLE_Rn, .changed = IDLE_Rn}, // SHLR16 Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xxA opcodes */
static const IdleInfo idle_table_4xxA[16] = {
    {.used = IDLE_Rn, .changed = IDLE_MACH}, // LDS Rn,MACH
    {.used = IDLE_Rn, .changed = IDLE_MACL}, // LDS Rn,MACL
    {.used = IDLE_Rn, .changed = IDLE_PR},   // LDS Rn,PR
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xxB opcodes */
static const IdleInfo idle_table_4xxB[16] = {
    {.changed = IDLE_BAD},              // JSR @Rn
    {.changed = IDLE_BAD},              // TAS @Rn
    {.used = 0, .changed = 0, .extra_cycles = 1}, // JMP @Rn
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xxE opcodes */
static const IdleInfo idle_table_4xxE[16] = {
    {.used = IDLE_Rn, .changed = IDLE_SR},  // LDC Rn,SR
    {.used = IDLE_Rn, .changed = IDLE_GBR}, // LDC Rn,GBR
    {.used = IDLE_Rn, .changed = IDLE_VBR}, // LDC Rn,VBR
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
};

/* Opcode table for $4xxx opcodes */
static const IdleInfo idle_table_4xxx[16] = {
    {.next_shift = 4, .subtable = idle_table_4xx0},
    {.next_shift = 4, .subtable = idle_table_4xx1},
    {.changed = IDLE_BAD},              // STSL ...,@-Rn
    {.changed = IDLE_BAD},              // STCL ...,@-Rn

    {.next_shift = 4, .subtable = idle_table_4xx4},
    {.next_shift = 4, .subtable = idle_table_4xx5},
    {.changed = IDLE_BAD},              // LDSL @Rm+,...
    {.changed = IDLE_BAD},              // LDCL @Rm+,...

    {.next_shift = 4, .subtable = idle_table_4xx8},
    {.next_shift = 4, .subtable = idle_table_4xx9},
    {.next_shift = 4, .subtable = idle_table_4xxA},
    {.next_shift = 4, .subtable = idle_table_4xxB},

    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid
    {.next_shift = 4, .subtable = idle_table_4xxE},
    {.changed = IDLE_BAD},              // MAC.W @Rm+,@Rn+
};

/*----------------------------------*/

/* Opcode table for $6xxx opcodes */
static const IdleInfo idle_table_6xxx[16] = {
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // MOV.B @Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // MOV.W @Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // MOV.L @Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // MOV Rm,Rn

    {.used = IDLE_Rm, .changed = IDLE_Rm|IDLE_Rn}, // MOV.B @Rm+,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rm|IDLE_Rn}, // MOV.W @Rm+,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rm|IDLE_Rn}, // MOV.L @Rm+,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // NOT Rm,Rn

    {.used = IDLE_Rm, .changed = IDLE_Rn}, // SWAP.B Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // SWAP.W Rm,Rn
    {.used = IDLE_Rm|IDLE_SR_T, .changed = IDLE_Rn|IDLE_SR_T}, // NEGC Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // NEG Rm,Rn

    {.used = IDLE_Rm, .changed = IDLE_Rn}, // EXTU.B Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // EXTU.W Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // EXTS.B Rm,Rn
    {.used = IDLE_Rm, .changed = IDLE_Rn}, // EXTS.W Rm,Rn
};

/*----------------------------------*/

/* Opcode table for $8xxx opcodes */
static const IdleInfo idle_table_8xxx[16] = {
    {.changed = IDLE_BAD},              // MOV.B R0,@(disp,Rm)
    {.changed = IDLE_BAD},              // MOV.W R0,@(disp,Rm)
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.used = IDLE_Rm, .changed = IDLE_R(0)}, // MOV.B @(disp,Rm),R0
    {.used = IDLE_Rm, .changed = IDLE_R(0)}, // MOV.W @(disp,Rm),R0
    {.changed = IDLE_BAD},              // invalid
    {.changed = IDLE_BAD},              // invalid

    {.used = IDLE_R(0), .changed = IDLE_SR_T}, // CMP/EQ #imm,R0
    {.used = IDLE_SR_T, .changed = 0, .extra_cycles = 2}, // BT label
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_SR_T, .changed = 0, .extra_cycles = 2}, // BF label

    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_SR_T, .changed = 0, .extra_cycles = 1}, // BT/S label
    {.changed = IDLE_BAD},              // invalid
    {.used = IDLE_SR_T, .changed = 0, .extra_cycles = 1}, // BF/S label
};

/*----------------------------------*/

/* Opcode table for $Cxxx opcodes */
static const IdleInfo idle_table_Cxxx[16] = {
    {.changed = IDLE_BAD},              // MOV.B R0,@(disp,GBR)
    {.changed = IDLE_BAD},              // MOV.W R0,@(disp,GBR)
    {.changed = IDLE_BAD},              // MOV.L R0,@(disp,GBR)
    {.changed = IDLE_BAD},              // TRAPA #imm

    {.used = IDLE_GBR, .changed = IDLE_R(0)}, // MOV.B @(disp,GBR),R0
    {.used = IDLE_GBR, .changed = IDLE_R(0)}, // MOV.W @(disp,GBR),R0
    {.used = IDLE_GBR, .changed = IDLE_R(0)}, // MOV.L @(disp,GBR),R0
    {.used = 0, .changed = IDLE_R(0)},    // MOVA @(disp,PC),R0

    {.used = IDLE_R(0), .changed = IDLE_SR_T}, // TST #imm,R0
    {.used = IDLE_R(0), .changed = IDLE_R(0)}, // AND #imm,R0
    {.used = IDLE_R(0), .changed = IDLE_R(0)}, // XOR #imm,R0
    {.used = IDLE_R(0), .changed = IDLE_R(0)}, // OR #imm,R0

    {.used = IDLE_R(0)|IDLE_GBR, .changed = IDLE_SR_T}, // TST #imm,@(R0,GBR)
    {.changed = IDLE_BAD},              // AND #imm,@(R0,GBR)
    {.changed = IDLE_BAD},              // XOR #imm,@(R0,GBR)
    {.changed = IDLE_BAD},              // OR #imm,@(R0,GBR)
};

/*----------------------------------*/

/* Main opcode table (for bits 12-15) */
static const IdleInfo idle_table_main[16] = {
    {.next_shift = 0, .subtable = idle_table_0xxx},
    {.changed = IDLE_BAD},  // MOV.L Rm,@(disp,Rn)
    {.next_shift = 0, .subtable = idle_table_2xxx},
    {.next_shift = 0, .subtable = idle_table_3xxx},

    {.next_shift = 0, .subtable = idle_table_4xxx},
    {.used = IDLE_Rm, .changed = IDLE_Rn},  // MOV.L @(disp,Rn),Rm
    {.next_shift = 0, .subtable = idle_table_6xxx},
    {.changed = IDLE_BAD},              // ADD #imm,Rn

    {.next_shift = 8, .subtable = idle_table_8xxx},
    {.used = 0, .changed = IDLE_Rn},    // MOV.W @(disp,PC),Rn
    {.used = 0, .changed = 0, .extra_cycles = 1}, // BRA label
    {.changed = IDLE_BAD},              // BSR label

    {.next_shift = 8, .subtable = idle_table_Cxxx},
    {.used = 0, .changed = IDLE_Rn},    // MOV.L @(disp,PC),Rn
    {.used = 0, .changed = IDLE_Rn},    // MOV #imm,Rn
    {.changed = IDLE_BAD},              // invalid
};

/*-----------------------------------------------------------------------*/

#endif  // OPTIMIZE_IDLE

/*************************************************************************/
/************************ Idle loop optimization *************************/
/*************************************************************************/

#ifdef OPTIMIZE_IDLE

/*
 * A loop is "idle" if a single execution of the entire loop, starting from
 * the first instruction of the loop and ending immediately before the next
 * time the first instruction is executed, has no net effect on the state
 * of the system after the first iteration of the loop.  Each individual
 * instruction may alter system state (and in fact, the program counter
 * will change with each instruction executed), but provided that the
 * external system state remains constant, the final result of executing N
 * loops must be identical to the result of executing N-1 loops for N
 * greater than 1.
 *
 * For example, a loop consisting solely of a branch to the same
 * instruction:
 *     0x1000: bra 0x1000
 *     0x1002: nop
 * would naturally qualify, as would a loop reading from the same memory
 * location (here, waiting for a memory address to read as zero):
 *     0x1000: mov.l @r0, r1
 *     0x1002: tst r1, r1
 *     0x1004: bf 0x1000
 *
 * On the flip side, a loop with a counter:
 *     0x1000: dt r0
 *     0x1002: bf 0x1000
 * would _not_ qualify, because the state of the system changes with each
 * iteration of the loop (though this particular loop can be optimized
 * separately through OPTIMIZE_DELAY).  Likewise, a loop containing a store
 * to memory:
 *     0x1000: mov.l r2, @r3
 *     0x1002: mov.l @r0, r1
 *     0x1004: tst r1, r1
 *     0x1006: bt 0x1000
 * would not qualify, because a store operation is assumed to change system
 * state even if the value stored is the same.  (Even for ordinary memory,
 * in a multiprocessor system like the Saturn memory can be accessed by
 * agents other than the processor, and an identical store operation may
 * have differing effects from one iteration to the next.)
 *
 * To determine whether a loop is idle, we check whether all operations
 * performed in the loop depend on constant values; in other words, no
 * instruction in the loop may modify a register which is used as a source
 * for that or an earlier instruction.  (The contents of memory are assumed
 * to be constant for this purpose, and any side effects of load
 * instructions are ignored.)  One result of this is that any loop with an
 * ALU-type operation other than CMP cannot be considered idle, except as
 * discussed below, even in cases where the loop does not in fact have a
 * net change in state:
 *     0x1000: mov.l @r0+, r1
 *     0x1002: cmp/pl @r1
 *     0x1004: bt 0x100A
 *     0x1006: bra 0x1000
 *     0x1008: add #-4, r0
 *     0x100A: ...
 * While admittedly somewhat contrived, this example has no net effect on
 * system state because R0 is decremented at the end of each loop; but
 * because R0 is used both in a postincrement memory access and as the
 * target of an ADD, its value cannot be considered a constant, so the
 * loop is not treated as idle.  (A more sophisticated algorithm could
 * track such changes in value and determine their cumulative effect, but
 * in most cases the simplistic algorithm we use is sufficient.)
 *
 * As an exception to the above, if a register is modified before it is
 * used by another instruction, the register becomes a "don't care" for
 * the remainder of the loop.  Thus a loop like:
 *     0x1000: mov.b @r0, r1
 *     0x1002: add r2, r1
 *     0x1004: tst r1, r1
 *     0x1006: bt 0x1000
 * qualifies as an idle loop despite the "add" instruction writing to
 * the same register it reads from.
 */

/*-----------------------------------------------------------------------*/

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
int can_optimize_idle(const uint16_t *insn_ptr, uint32_t PC,
                      unsigned int count)
{
    uint32_t used_regs;  // Which registers have been used so far?
    uint32_t dont_care;  // Which registers are "don't cares"?
    int can_optimize = 1;
    unsigned int i;

    /* We detect failure by matching the changed-registers bitmask against
     * used_regs; set the initial value so that non-idleable instructions
     * are automatically rejected */
    used_regs = IDLE_BAD;
    /* Initially, no registers are don't cares */
    dont_care = 0;

    for (i = 0; can_optimize && i < count; i++) {
        const uint16_t opcode = *insn_ptr++;

        /* Find the entry for this opcode */
        int shift = 12;
        const IdleInfo *table = idle_table_main;
        while (table[opcode>>shift & 0xF].subtable) {
            int newshift = table[opcode>>shift & 0xF].next_shift;
            table = table[opcode>>shift & 0xF].subtable;
            shift = newshift;
        }

        /* Replace virtual Rn/Rm bits with real registers from opcode */
        uint32_t used = table[opcode>>shift & 0xF].used;
        uint32_t changed = table[opcode>>shift & 0xF].changed;
        if (used & IDLE_Rn) {
            used &= ~IDLE_Rn;
            used |= IDLE_R(opcode>>8 & 0xF);
        }
        if (used & IDLE_Rm) {
            used &= ~IDLE_Rm;
            used |= IDLE_R(opcode>>4 & 0xF);
        }
        if (changed & IDLE_Rn) {
            changed &= ~IDLE_Rn;
            changed |= IDLE_R(opcode>>8 & 0xF);
        }
        if (changed & IDLE_Rm) {
            changed &= ~IDLE_Rm;
            changed |= IDLE_R(opcode>>4 & 0xF);
        }

        /* Add any registers used by this instruction to used_regs */
        used_regs |= used;

        /* Add any not-yet-used registers modified by this instruction to
         * dont_care */
        dont_care |= changed & ~used_regs;

        /* See whether we can still treat this as an idle loop */
        if (changed & ~dont_care & used_regs) {
            can_optimize = 0;
        }
    }

    return can_optimize;
}

/*-----------------------------------------------------------------------*/

#endif  // OPTIMIZE_IDLE

/*************************************************************************/
/************************* Division optimization *************************/
/*************************************************************************/

#ifdef OPTIMIZE_DIVISION

/*
 * On the SH-2, unsigned 64bit/32bit division follows the sequence:
 *     DIV0U
 *     .arepeat 32  ;Repeat 32 times
 *         ROTCL Rlo
 *         DIV1 Rdiv,Rhi
 *     .aendr
 * and finishes with the following state changes:
 *     M   = 0
 *     T   = low bit of quotient
 *     Q   = !T
 *     Rlo = high 31 bits of quotient in bits 0-30, with 0 in bit 31
 *     Rhi = !Q ? remainder : (remainder - Rdiv)
 * If the number of repetitions of ROTCL Rlo / DIV1 Rdiv,Rhi is less than
 * 32, the result is equivalent to shifting the dividend right (32-N) bits
 * where N is the repetition count, except that the low (32-N) bits of the
 * dividend remain in the most-significant bits of Rlo.
 *
 * Signed 64bit/32bit division (where the dividend is a 32-bit signed value
 * sign-extended to 64 bits) follows a sequence identical to unsigned
 * division except for the first instruction:
 *     DIV0S Rdiv,Rhi
 *     .arepeat 32  ;Repeat 32 times
 *         ROTCL Rlo
 *         DIV1 Rdiv,Rhi
 *     .aendr
 * and finishes with the following state changes:
 *     M   = sign bit of divisor
 * (if dividend >= 0)
 *     T   = low bit of (quotient - M)
 *     Q   = !(T ^ M)
 *     Rlo = high 31 bits of (quotient - M) in bits 0-30,
 *              sign-extended to 32 bits
 *     Rhi = !Q ? remainder : (remainder - abs(Rdiv))
 * (if dividend < 0)
 *     qtemp = quotient + (remainder ? (M ? 1 : -1) : 0)
 *     T   = low bit of (qtemp - M)
 *     Q   = !(T ^ M)
 *     Rlo = high 31 bits of (qtemp - M) in bits 0-30,
 *              sign-extended to 32 bits
 *     Rhi = !Q ? (remainder == 0 ? remainder : remainder + abs(Rdiv))
 *              : (remainder != 0 ? remainder : remainder - abs(Rdiv))
 *
 * In all cases, division by zero ends with:
 *     M = 0
 *     T = 1
 *     Q = 0
 *     Rhi:Rlo = Rlo<<32 | x<<31 | (unsigned)(~Rhi)>>1
 * where x is 0 for unsigned division and the high bit of Rlo for signed
 * division.
 *
 * Both of these sequences can thus be optimized significantly using native
 * division instructions (at one point, a factor-of-18 reduction in
 * execution speed was measured).
 */

/*-----------------------------------------------------------------------*/

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
int can_optimize_div0u(const uint16_t *insn_ptr, uint32_t PC,
                       int skip_first_rotcl,
                       int *Rhi_ret, int *Rlo_ret, int *Rdiv_ret)
{
    uint16_t opcode;  // Opcode read from instruction stream
    uint16_t rotcl_op = 0, div1_op = 0;  // Expected opcodes
    int nbits;  // Number of bits divided so far

    if (!skip_first_rotcl) {
        opcode = *++insn_ptr;
        if ((opcode & 0xF0FF) != 0x4024) {  // ROTCL Rlo
#ifdef JIT_DEBUG
            DMSG("DIV0U optimization failed: PC+2 (0x%08X) is 0x%04X, not"
                 " ROTCL", PC+2, opcode);
#endif
            return 0;
        }
        *Rlo_ret = opcode>>8 & 0xF;
        rotcl_op = opcode;
    }

    opcode = *++insn_ptr;
    if ((opcode & 0xF00F) != 0x3004) {  // DIV1 Rdiv,Rhi
#ifdef JIT_DEBUG
        DMSG("DIV0U optimization failed: PC+%d (0x%08X) is 0x%04X, not DIV1",
             skip_first_rotcl ? 2 : 4, PC + (skip_first_rotcl ? 2 : 4),
             opcode);
#endif
        return 0;
    }
    *Rhi_ret  = opcode>>8 & 0xF;
    *Rdiv_ret = opcode>>4 & 0xF;
    div1_op = opcode;

    if (skip_first_rotcl) {
        opcode = insn_ptr[1];  // Don't advance yet--we're just peeking
        if ((opcode & 0xF0FF) != 0x4024) {
#ifdef JIT_DEBUG
            DMSG("DIV0U optimization failed (with skip_first_rotcl):"
                 " PC+4 (0x%08X) is 0x%04X, not ROTCL", PC+4, opcode);
#endif
            return 0;
        }
        *Rlo_ret = opcode>>8 & 0xF;
        rotcl_op = opcode;
    }

    for (nbits = 1; nbits < 32; nbits++) {
        opcode = *++insn_ptr;
        if (opcode != rotcl_op) {
#ifdef JIT_DEBUG_VERBOSE
            DMSG("DIV0U optimization stopped at %d bits: PC+%d (0x%08X) is"
                 " 0x%04X, not ROTCL R%d",
                 nbits, (skip_first_rotcl ? 0 : 2) + 4*nbits,
                 PC + ((skip_first_rotcl ? 0 : 2) + 4*nbits), opcode,
                 *Rlo_ret);
#endif
            break;
        }

        opcode = *++insn_ptr;
        if (opcode != div1_op) {
#ifdef JIT_DEBUG_VERBOSE
            DMSG("DIV0U optimization stopped at %d bits: PC+%d (0x%08X) is"
                 " 0x%04X, not DIV1 R%d,R%d",
                 nbits, (skip_first_rotcl ? 2 : 4) + 4*nbits,
                 PC + ((skip_first_rotcl ? 2 : 4) + 4*nbits), opcode,
                 *Rdiv_ret, *Rhi_ret);
#endif
            break;
        }
    }  // for 32 bits

    return nbits;
}

/*-----------------------------------------------------------------------*/

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
int can_optimize_div0s(const uint16_t *insn_ptr, uint32_t PC,
                       int Rhi, int *Rlo_ret, int Rdiv)
{
    uint16_t rotcl_op = 0, div1_op = 0x3004 | Rhi<<8 | Rdiv<<4;
    int can_optimize = 1;
    unsigned int i;

    for (i = 0; can_optimize && i < 64; i++) {
        uint16_t nextop = *insn_ptr++;

        if (i%2 == 0) {  // ROTCL Rlo

            if (i == 0) {
                if ((nextop & 0xF0FF) == 0x4024) {
                    *Rlo_ret = nextop>>8 & 0xF;
                    rotcl_op = nextop;
                } else {
                    /* Don't complain for the first instruction, since
                     * DIV0S seems to be put to uses other than division
                     * as well */
                    can_optimize = 0;
                }
            } else {
                if (UNLIKELY(nextop != rotcl_op)) {
                    DMSG("DIV0S optimization failed: PC+%d (0x%08X) is 0x%04X,"
                         " not ROTCL R%d", 2 + 2*i, PC + 2*i, nextop,
                         *Rlo_ret);
                    can_optimize = 0;
                }
            }

        } else {  // DIV1 Rdiv,Rhi

            if (UNLIKELY(nextop != div1_op)) {
                DMSG("DIV0S optimization failed: PC+%d (0x%08X) is 0x%04X,"
                     " not DIV1 R%d,R%d", 2 + 2*i, PC + 2*i, nextop, Rdiv, Rhi);
                can_optimize = 0;
            }

        }
    }  // for 64 instructions

    return can_optimize;
}

#endif  // OPTIMIZE_DIVISION

/*************************************************************************/
/********************** Variable shift optimization **********************/
/*************************************************************************/

#ifdef OPTIMIZE_VARIABLE_SHIFTS

/*
 * The SH-2's repertoire of shift instructions is fairly limited compared
 * to modern processors.  While the SH-2 has a few multi-bit shift
 * instructions (SHLL2, SHLR8, and the like), it cannot shift a register
 * by an arbitrary constant, instead requiring a sequence of fixed-sized
 * shifts to accomplish the task.  More importantly, the SH-2 also cannot
 * shift by a variable count specified in another register, meaning that
 * programs must use conditional tests or loops instead.  Since modern
 * processors do have variable-count shift instructions, such tests or
 * loops can be significantly shortened, eliminating branches that can
 * hurt optimizability of the code.
 * 
 * Currently, the following variable shift/rotate sequences is recognized:
 *
 *    - TST #1, R0
 *      BT .+4
 *      SHL[LR] Rshift
 *      TST #2, R0
 *      BT .+4
 *      SHL[LR]2 Rshift
 *      TST #4, R0
 *      BT .+6
 *      SHL[LR]2 Rshift
 *      SHL[LR]2 Rshift
 *      [TST #8, R0
 *       BT .+4
 *       SHL[LR]8 Rshift
 *       [TST #16, R0
 *        BT .+4
 *        SHL[LR]16 Rshift]]
 *
 *    - BRAF Rcount_adjusted  [Rcount_adjusted = (maximum shift - Rcount) * 2]
 *      (delay slot)
 *      SHLL Rshift  [or SHAL, SHLR, SHAR, ROTL, ROTR]
 *      SHLL Rshift
 *      ...
 */

/*-----------------------------------------------------------------------*/

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
unsigned int can_optimize_variable_shift(
    const uint16_t *insn_ptr, uint32_t PC, unsigned int *Rcount_ret,
    unsigned int *max_ret, unsigned int *Rshift_ret, unsigned int *type_ret,
    const uint8_t **cycles_ret)
{
    if (insn_ptr[0] == 0xC801                  // TST #1, R0
     && insn_ptr[1] == 0x8900                  // BT .+4
     && (insn_ptr[2] & 0xF0FE) == 0x4000       // SHL[LR] Rshift
     && insn_ptr[3] == 0xC802                  // TST #2, R0
     && insn_ptr[4] == 0x8900                  // BT .+4
     && insn_ptr[5] == (insn_ptr[2] | 0x0008)  // SHL[LR]2 Rshift
     && insn_ptr[6] == 0xC804                  // TST #4, R0
     && insn_ptr[7] == 0x8901                  // BT .+6
     && insn_ptr[8] == (insn_ptr[2] | 0x0008)  // SHL[LR]2 Rshift
     && insn_ptr[9] == (insn_ptr[2] | 0x0008)  // SHL[LR]2 Rshift
    ) {
        *Rcount_ret = 0;
        *Rshift_ret = insn_ptr[2]>>8 & 0xF;
        *type_ret = (insn_ptr[2] & 1) ? 1 : 0;
        if (insn_ptr[10] == 0xC808                  // TST #8, R0
         && insn_ptr[11] == 0x8900                  // BT .+4
         && insn_ptr[12] == (insn_ptr[2] | 0x0018)  // SHL[LR]8 Rshift
        ) {
            if (insn_ptr[13] == 0xC810                  // TST #16, R0
             && insn_ptr[14] == 0x8900                  // BT .+4
             && insn_ptr[15] == (insn_ptr[2] | 0x0028)  // SHL[LR]16 Rshift
            ) {
                *max_ret = 31;
                static const uint8_t cycles_array[32] =
                    {20,19,19,18,20,19,19,18,19,18,18,17,19,18,18,17,
                     19,18,18,17,19,18,18,17,18,17,17,16,18,17,17,16};
                *cycles_ret = cycles_array;
                return 16;
            } else {
                *max_ret = 15;
                static const uint8_t cycles_array[16] =
                    {16,15,15,14,16,15,15,14,15,14,14,13,15,14,14,13};
                *cycles_ret = cycles_array;
                return 13;
            }
        } else {
            *max_ret = 7;
            static const uint8_t cycles_array[8] = {12,11,11,10,12,11,11,10};
            *cycles_ret = cycles_array;
            return 10;
        }
    }

    if ((insn_ptr[0] & 0xF0FF) == 0x0023       // BRAF Rcount_adjusted
     && ((insn_ptr[2] & 0xF0DE) == 0x4000      // SH[LA][LR] Rshift
      || (insn_ptr[2] & 0xF0FE) == 0x4004)     // ROT[LR] Rshift
    ) {
        unsigned int num_insns = 3;
        while (num_insns < 2+32 && insn_ptr[num_insns] == insn_ptr[2]) {
            num_insns++;
        }
        *Rcount_ret = insn_ptr[0]>>8 & 0xF;
        *Rshift_ret = insn_ptr[2]>>8 & 0xF;
        *max_ret = num_insns - 2;
        switch (insn_ptr[2] & 0xFF) {
            case 0x00: *type_ret = 0; break;
            case 0x20: *type_ret = 0; break;
            case 0x01: *type_ret = 1; break;
            case 0x21: *type_ret = 2; break;
            case 0x04: *type_ret = 3; break;
            case 0x05: *type_ret = 4; break;
        }
        return num_insns;
    }

    return 0;
}

/*-----------------------------------------------------------------------*/

#endif  // OPTIMIZE_VARIABLE_SHIFTS

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
