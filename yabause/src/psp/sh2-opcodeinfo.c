/*  src/psp/sh2-opcodeinfo.c: Information table for SH-2 opcodes
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

#include "sh2.h"
#include "sh2-internal.h"

/*************************************************************************/
/************************ Opcode table definition ************************/
/*************************************************************************/

/**
 * opcode_info_low:  Table of information bits for opcodes with the high
 * bit clear; in all such instructions, the "n" field (bits 8-11) is an
 * operand or part of an operand.  Indexed by bits 0-7 and 12-14 of the
 * opcode, i.e. ((opcode & 0x7000) >> 4 | (opcode & 0x00FF)).
 */
static int32_t opcode_info_low[0x800];

/**
 * opcode_info_high:  Table of information bits for opcodes in which the
 * "n" field (bits 8-11) is part of the instruction code and the lower 8
 * bits are one or more operands.  Indexed by bits 8-14 of the opcode.
 */
static int32_t opcode_info_high[0x80];

/*************************************************************************/
/***************** Opcode table initialization routines ******************/
/*************************************************************************/

/* Forward declarations */

static void init_0xxx(void);
static void init_1xxx(void);
static void init_2xxx(void);
static void init_3xxx(void);
static void init_4xxx(void);
static void init_5xxx(void);
static void init_6xxx(void);
static void init_7xxx(void);
static void init_8xxx(void);
static void init_9xxx(void);
static void init_Axxx(void);
static void init_Bxxx(void);
static void init_Cxxx(void);
static void init_Dxxx(void);
static void init_Exxx(void);

/*************************************************************************/

/**
 * init_opcode_info:  Initialize the opcode_info[] table.  Must be called
 * before accessing the table.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void init_opcode_info(void)
{
    /* First clear the tables (rendering all opcodes invalid)... */
    memset(opcode_info_low,  0, sizeof(opcode_info_low));
    memset(opcode_info_high, 0, sizeof(opcode_info_high));

    /* ... then fill in the tables by calling subroutines for each opcode
     * group. */
    init_0xxx();
    init_1xxx();
    init_2xxx();
    init_3xxx();
    init_4xxx();
    init_5xxx();
    init_6xxx();
    init_7xxx();
    init_8xxx();
    init_9xxx();
    init_Axxx();
    init_Bxxx();
    init_Cxxx();
    init_Dxxx();
    init_Exxx();
    /* 0xFxxx is invalid */
}

/*************************************************************************/

/**
 * init_xxxx:  Initialize individual groups of opcodes.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */

/*-----------------------------------------------------------------------*/

static void init_0xxx(void)
{
    unsigned int i;

    /* STC SR,Rn */
    opcode_info_low[0x002] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* STC GBR,Rn */
    opcode_info_low[0x012] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* STC VBR,Rn */
    opcode_info_low[0x022] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;

    /* BSRF Rn */
    opcode_info_low[0x003] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;
    /* BRAF Rn */
    opcode_info_low[0x023] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;

    /* MOV.* Rm,@(R0,Rn) */
    for (i = 0x000; i <= 0x0F0; i += 0x10) {
        opcode_info_low[i|0x4] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B;
        opcode_info_low[i|0x5] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W;
        opcode_info_low[i|0x6] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L;
    }

    /* MUL.L Rm,Rn */
    for (i = 0x000; i <= 0x0F0; i += 0x10) {
        opcode_info_low[i|0x7] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn;
    }

    /* CLRT */
    opcode_info_low[0x008] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* SETT */
    opcode_info_low[0x018] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* CLRMAC */
    opcode_info_low[0x028] = SH2_OPCODE_INFO_VALID;

    /* NOP */
    opcode_info_low[0x009] = SH2_OPCODE_INFO_VALID;
    /* DIV0U */
    opcode_info_low[0x019] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* MOVT Rn */
    opcode_info_low[0x029] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;

    /* STS MACH,Rn */
    opcode_info_low[0x00A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* STS MACL,Rn */
    opcode_info_low[0x01A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* STS PR,Rn */
    opcode_info_low[0x02A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_Rn;

    /* RTS */
    opcode_info_low[0x00B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;
    /* SLEEP */
    opcode_info_low[0x01B] = SH2_OPCODE_INFO_VALID;
    /* RTE */
    opcode_info_low[0x02B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R15
                           | SH2_OPCODE_INFO_SETS_SR_T
                           | SH2_OPCODE_INFO_ACCESSES_R15
                           | SH2_OPCODE_INFO_ACCESS_SIZE_LL
                           | SH2_OPCODE_INFO_ACCESS_POSTINC
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;

    /* MOV.* @(R0,Rm),Rn */
    for (i = 0x000; i <= 0x0F0; i += 0x10) {
        opcode_info_low[i|0xC] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B;
        opcode_info_low[i|0xD] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W;
        opcode_info_low[i|0xE] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_R0
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_R0_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L;
    }

    /* MAC.L @Rm+,@Rn+ */
    for (i = 0x000; i <= 0x0F0; i += 0x10) {
        opcode_info_low[i|0xF] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L
                               | SH2_OPCODE_INFO_ACCESS_POSTINC;
    }
}

/*-----------------------------------------------------------------------*/

static void init_1xxx(void)
{
    /* MOV.L Rm,@(disp,Rn) */
    unsigned int i;
    for (i = 0x100; i <= 0x1FF; i++) {
        opcode_info_low[i] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;
    }
}

/*-----------------------------------------------------------------------*/

static void init_2xxx(void)
{
    unsigned int i;
    for (i = 0x200; i <= 0x2F0; i += 0x10) {

        /* MOV.* Rm,@Rn */
        opcode_info_low[i|0x0] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B;
        opcode_info_low[i|0x1] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W;
        opcode_info_low[i|0x2] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L;

        /* MOV.* Rm,@-Rn */
        opcode_info_low[i|0x4] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B
                               | SH2_OPCODE_INFO_ACCESS_PREDEC;
        opcode_info_low[i|0x5] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W
                               | SH2_OPCODE_INFO_ACCESS_PREDEC;
        opcode_info_low[i|0x6] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_IS_STORE
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L
                               | SH2_OPCODE_INFO_ACCESS_PREDEC;

        /* DIV0S Rm,Rn */
        opcode_info_low[i|0x7] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* TST Rm,Rn */
        opcode_info_low[i|0x8] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* AND Rm,Rn */
        opcode_info_low[i|0x9] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* XOR Rm,Rn */
        opcode_info_low[i|0xA] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* OR Rm,Rn */
        opcode_info_low[i|0xB] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* CMP/ST Rm,Rn */
        opcode_info_low[i|0xC] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* XTRCT Rm,Rn */
        opcode_info_low[i|0xD] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* MULU.W Rm,Rn */
        opcode_info_low[i|0xE] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn;

        /* MULS.W Rm,Rn */
        opcode_info_low[i|0xF] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn;
    }
}

/*-----------------------------------------------------------------------*/

static void init_3xxx(void)
{
    unsigned int i;
    for (i = 0x300; i <= 0x3F0; i += 0x10) {

        /* CMP/EQ Rm,Rn */
        opcode_info_low[i|0x0] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* CMP/HS Rm,Rn */
        opcode_info_low[i|0x2] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* CMP/GE Rm,Rn */
        opcode_info_low[i|0x3] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* DIV1 Rm,Rn */
        opcode_info_low[i|0x4] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* DMULU.L Rm,Rn */
        opcode_info_low[i|0x5] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn;

        /* CMP/HI Rm,Rn */
        opcode_info_low[i|0x6] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* CMP/GT Rm,Rn */
        opcode_info_low[i|0x7] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* SUB Rm,Rn */
        opcode_info_low[i|0x8] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* SUBC Rm,Rn */
        opcode_info_low[i|0xA] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* SUBV Rm,Rn */
        opcode_info_low[i|0xB] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* ADD Rm,Rn */
        opcode_info_low[i|0xC] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* DMULS.L Rm,Rn */
        opcode_info_low[i|0xD] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn;

        /* ADDC Rm,Rn */
        opcode_info_low[i|0xE] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* ADDV Rm,Rn */
        opcode_info_low[i|0xF] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;
    }
}

/*-----------------------------------------------------------------------*/

static void init_4xxx(void)
{
    /* SHLL Rn */
    opcode_info_low[0x400] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* DT Rn */
    opcode_info_low[0x410] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* SHAL Rn */
    opcode_info_low[0x420] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* SHLR Rn */
    opcode_info_low[0x401] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* CMP/PZ Rn */
    opcode_info_low[0x411] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* SHAR Rn */
    opcode_info_low[0x421] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* STS.L MACH,@-Rn */
    opcode_info_low[0x402] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;
    /* STS.L MACL,@-Rn */
    opcode_info_low[0x412] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;
    /* STS.L PR,@-Rn */
    opcode_info_low[0x422] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;

    /* STC.L SR,@-Rn */
    opcode_info_low[0x403] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;
    /* STC.L GBR,@-Rn */
    opcode_info_low[0x413] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;
    /* STC.L VBR,@-Rn */
    opcode_info_low[0x423] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_PREDEC;

    /* ROTL Rn */
    opcode_info_low[0x404] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* ROTCL Rn */
    opcode_info_low[0x424] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* ROTR Rn */
    opcode_info_low[0x405] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* CMP/PL Rn */
    opcode_info_low[0x415] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* ROTCR Rn */
    opcode_info_low[0x425] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* LDS.L @Rn+,MACH */
    opcode_info_low[0x406] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;
    /* LDS.L @Rn+,MACL */
    opcode_info_low[0x416] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;
    /* LDS.L @Rn+,PR */
    opcode_info_low[0x426] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;

    /* LDC.L @Rn+,SR */
    opcode_info_low[0x407] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;
    /* LDC.L @Rn+,GBR */
    opcode_info_low[0x417] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;
    /* LDC.L @Rn+,VBR */
    opcode_info_low[0x427] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_POSTINC;

    /* SHLL2 Rn */
    opcode_info_low[0x408] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* SHLL8 Rn */
    opcode_info_low[0x418] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* SHLL16 Rn */
    opcode_info_low[0x428] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;

    /* SHLR2 Rn */
    opcode_info_low[0x409] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* SHLR8 Rn */
    opcode_info_low[0x419] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;
    /* SHLR16 Rn */
    opcode_info_low[0x429] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;

    /* LDS Rn,MACH */
    opcode_info_low[0x40A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn;
    /* LDS Rn,MACL */
    opcode_info_low[0x41A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn;
    /* LDS Rn,PR */
    opcode_info_low[0x42A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn;

    /* JSR @Rn */
    opcode_info_low[0x40B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;
    /* TAS.B @Rn */
    opcode_info_low[0x41B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T
                           | SH2_OPCODE_INFO_ACCESSES_Rn
                           | SH2_OPCODE_INFO_ACCESS_IS_RMW
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B;
    /* JMP @Rn */
    opcode_info_low[0x42B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_BRANCH_UNCOND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;

    /* LDC Rn,SR */
    opcode_info_low[0x40E] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_SR_T;
    /* LDC Rn,GBR */
    opcode_info_low[0x41E] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn;
    /* LDC Rn,VBR */
    opcode_info_low[0x42E] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn;

    /* MAC.W @Rm+,@Rn+ */
    unsigned int i;
    for (i = 0x400; i <= 0x4F0; i += 0x10) {
        opcode_info_low[i|0xF] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_USES_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESSES_Rn
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W
                               | SH2_OPCODE_INFO_ACCESS_POSTINC;
    }
}

/*-----------------------------------------------------------------------*/

static void init_5xxx(void)
{
    /* MOV.L @(disp,Rm),Rn */
    unsigned int i;
    for (i = 0x500; i <= 0x5FF; i++) {
        opcode_info_low[i] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_SETS_Rn
                           | SH2_OPCODE_INFO_ACCESSES_Rm
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;
    }
}

/*-----------------------------------------------------------------------*/

static void init_6xxx(void)
{
    unsigned int i;
    for (i = 0x600; i <= 0x6F0; i += 0x10) {

        /* MOV.* @Rm,Rn */
        opcode_info_low[i|0x0] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B;
        opcode_info_low[i|0x1] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W;
        opcode_info_low[i|0x2] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L;

        /* MOV Rm,Rn */
        opcode_info_low[i|0x3] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* MOV.* @Rm+,Rn */
        opcode_info_low[i|0x4] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_B
                               | SH2_OPCODE_INFO_ACCESS_POSTINC;
        opcode_info_low[i|0x5] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_W
                               | SH2_OPCODE_INFO_ACCESS_POSTINC;
        opcode_info_low[i|0x6] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_ACCESSES_Rm
                               | SH2_OPCODE_INFO_ACCESS_SIZE_L
                               | SH2_OPCODE_INFO_ACCESS_POSTINC;

        /* NOT Rm,Rn */
        opcode_info_low[i|0x7] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* SWAP.* Rm,Rn */
        opcode_info_low[i|0x8] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;
        opcode_info_low[i|0x9] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* NEGC Rm,Rn */
        opcode_info_low[i|0xA] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn
                               | SH2_OPCODE_INFO_SETS_SR_T;

        /* NEG Rm,Rn */
        opcode_info_low[i|0xB] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* EXTU.* Rm,Rn */
        opcode_info_low[i|0xC] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;
        opcode_info_low[i|0xD] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;

        /* EXTS.* Rm,Rn */
        opcode_info_low[i|0xE] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;
        opcode_info_low[i|0xF] = SH2_OPCODE_INFO_VALID
                               | SH2_OPCODE_INFO_USES_Rm
                               | SH2_OPCODE_INFO_SETS_Rn;
    }
}

/*-----------------------------------------------------------------------*/

static void init_7xxx(void)
{
    /* ADD #imm,Rn */
    unsigned int i;
    for (i = 0x700; i <= 0x7FF; i++) {
        opcode_info_low[i] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rn
                           | SH2_OPCODE_INFO_SETS_Rn;
    }
}

/*-----------------------------------------------------------------------*/

static void init_8xxx(void)
{
    /* MOV.B R0,@(disp,Rm) */
    opcode_info_high[0x00] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_ACCESSES_Rm
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;

    /* MOV.W R0,@(disp,Rm) */
    opcode_info_high[0x01] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_ACCESSES_Rm
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_W
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;

    /* MOV.B @(disp,Rm),R0 */
    opcode_info_high[0x04] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_SETS_R0
                           | SH2_OPCODE_INFO_ACCESSES_Rm
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;

    /* MOV.W @(disp,Rm),R0 */
    opcode_info_high[0x05] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_Rm
                           | SH2_OPCODE_INFO_SETS_R0
                           | SH2_OPCODE_INFO_ACCESSES_Rm
                           | SH2_OPCODE_INFO_ACCESS_SIZE_W
                           | SH2_OPCODE_INFO_ACCESS_DISP_4;

    /* CMP/EQ #imm,R0 */
    opcode_info_high[0x08] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* BT label */
    opcode_info_high[0x09] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_BRANCH_COND;

    /* BF label */
    opcode_info_high[0x0B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_BRANCH_COND;

    /* BT/S label */
    opcode_info_high[0x0D] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_BRANCH_COND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;

    /* BF/S label */
    opcode_info_high[0x0F] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_BRANCH_COND
                           | SH2_OPCODE_INFO_BRANCH_DELAYED;
}

/*-----------------------------------------------------------------------*/

static void init_9xxx(void)
{
    /* MOV.W @(disp,PC),Rn */
    unsigned int i;
    for (i = 0x10; i <= 0x1F; i++) {
        opcode_info_high[i] = SH2_OPCODE_INFO_VALID
                            | SH2_OPCODE_INFO_SETS_Rn
                            | SH2_OPCODE_INFO_ACCESSES_PC
                            | SH2_OPCODE_INFO_ACCESS_SIZE_W
                            | SH2_OPCODE_INFO_ACCESS_DISP_8;
    }
}

/*-----------------------------------------------------------------------*/

static void init_Axxx(void)
{
    /* BRA label */
    unsigned int i;
    for (i = 0x20; i <= 0x2F; i++) {
        opcode_info_high[i] = SH2_OPCODE_INFO_VALID
                            | SH2_OPCODE_INFO_BRANCH_UNCOND
                            | SH2_OPCODE_INFO_BRANCH_DELAYED;
    }
}

/*-----------------------------------------------------------------------*/

static void init_Bxxx(void)
{
    /* BRA label */
    unsigned int i;
    for (i = 0x30; i <= 0x3F; i++) {
        opcode_info_high[i] = SH2_OPCODE_INFO_VALID
                            | SH2_OPCODE_INFO_BRANCH_UNCOND
                            | SH2_OPCODE_INFO_BRANCH_DELAYED;
    }
}

/*-----------------------------------------------------------------------*/

static void init_Cxxx(void)
{
    /* MOV.B R0,@(disp,GBR) */
    opcode_info_high[0x40] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* MOV.W R0,@(disp,GBR) */
    opcode_info_high[0x41] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_W
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* MOV.L R0,@(disp,GBR) */
    opcode_info_high[0x42] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* TRAPA #imm */
    opcode_info_high[0x43] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R15
                           | SH2_OPCODE_INFO_ACCESSES_R15
                           | SH2_OPCODE_INFO_ACCESS_IS_STORE
                           | SH2_OPCODE_INFO_ACCESS_SIZE_LL
                           | SH2_OPCODE_INFO_ACCESS_PREDEC
                           | SH2_OPCODE_INFO_BRANCH_UNCOND;

    /* MOV.B @(disp,GBR),R0 */
    opcode_info_high[0x44] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* MOV.W @(disp,GBR),R0 */
    opcode_info_high[0x45] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_SIZE_W
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* MOV.L @(disp,GBR),R0 */
    opcode_info_high[0x46] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_R0
                           | SH2_OPCODE_INFO_ACCESSES_GBR
                           | SH2_OPCODE_INFO_ACCESS_SIZE_L
                           | SH2_OPCODE_INFO_ACCESS_DISP_8;

    /* MOVA @(disp,PC),R0 */
    opcode_info_high[0x47] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_SETS_R0;

    /* TST #imm,R0 */
    opcode_info_high[0x48] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_SR_T;

    /* AND #imm,R0 */
    opcode_info_high[0x49] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_R0;

    /* XOR #imm,R0 */
    opcode_info_high[0x4A] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_R0;

    /* OR #imm,R0 */
    opcode_info_high[0x4B] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_R0;

    /* TST.B #imm,@(R0,GBR) */
    opcode_info_high[0x4C] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_SETS_SR_T
                           | SH2_OPCODE_INFO_ACCESSES_R0_GBR
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B;

    /* AND.B #imm,@(R0,GBR) */
    opcode_info_high[0x4D] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_R0_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_RMW
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B;

    /* XOR.B #imm,@(R0,GBR) */
    opcode_info_high[0x4E] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_R0_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_RMW
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B;

    /* OR.B #imm,@(R0,GBR) */
    opcode_info_high[0x4F] = SH2_OPCODE_INFO_VALID
                           | SH2_OPCODE_INFO_USES_R0
                           | SH2_OPCODE_INFO_ACCESSES_R0_GBR
                           | SH2_OPCODE_INFO_ACCESS_IS_RMW
                           | SH2_OPCODE_INFO_ACCESS_SIZE_B;
}

/*-----------------------------------------------------------------------*/

static void init_Dxxx(void)
{
    /* MOV.L @(disp,PC),Rn */
    unsigned int i;
    for (i = 0x50; i <= 0x5F; i++) {
        opcode_info_high[i] = SH2_OPCODE_INFO_VALID
                            | SH2_OPCODE_INFO_SETS_Rn
                            | SH2_OPCODE_INFO_ACCESSES_PC
                            | SH2_OPCODE_INFO_ACCESS_SIZE_L
                            | SH2_OPCODE_INFO_ACCESS_DISP_8;
    }
}

/*-----------------------------------------------------------------------*/

static void init_Exxx(void)
{
    /* MOV #imm,Rn */
    unsigned int i;
    for (i = 0x60; i <= 0x6F; i++) {
        opcode_info_high[i] = SH2_OPCODE_INFO_VALID
                            | SH2_OPCODE_INFO_SETS_Rn;
    }
}

/*************************************************************************/
/********************** Opcode table lookup routine **********************/
/*************************************************************************/

/**
 * get_opcode_info:  Return information about the given opcode.
 *
 * [Parameters]
 *     opcode: SH-2 opcode to obtain information about
 * [Return value]
 *     
 */
int32_t get_opcode_info(uint16_t opcode)
{
    if (opcode & 0x8000) {
        return opcode_info_high[(opcode & 0x7F00) >> 8];
    } else {
        return opcode_info_low[(opcode & 0x7000) >> 4 | (opcode & 0xFF)];
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
