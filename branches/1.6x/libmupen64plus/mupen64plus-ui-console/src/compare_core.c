/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *   Mupen64plus - compare_core.h                                          *
 *   Mupen64Plus homepage: http://code.google.com/p/mupen64plus/           *
 *   Copyright (C) 2009 Richard Goedeken                                   *
 *   Copyright (C) 2002 Hacktarux                                          *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License as published by  *
 *   the Free Software Foundation; either version 2 of the License, or     *
 *   (at your option) any later version.                                   *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License for more details.                          *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   along with this program; if not, write to the                         *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.          *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

#include <stdio.h>
#include <string.h>
#include <sys/stat.h>

#include "m64p_types.h"
#include "main.h"
#include "compare_core.h"
#include "core_interface.h"

/* local variables */

static FILE *fPipe = NULL;
static int comp_reg_32[32];
static long long comp_reg_64[32];
static unsigned int old_op = 0;
static int l_CoreCompareMode = CORE_COMPARE_DISABLE;

static long long *ptr_reg = NULL;  /* pointer to the 64-bit general purpose registers in the core */
static int       *ptr_cop0 = NULL; /* pointer to the 32-bit Co-processor 0 registers in the core */
static long long *ptr_fgr = NULL;  /* pointer to the 64-bit floating-point registers in the core */ 
static int       *ptr_PC = NULL;   /* pointer to 32-bit R4300 Program Counter */

/* local functions */
static void stop_it(void)
{
    static int errors = 0;

    (*CoreDoCommand)(M64CMD_STOP, 0, NULL);

    errors++;
#if !defined(WIN32)
    #if defined(__i386__) || defined(__x86_64__)
        if (errors > 7)
            asm("int $3;");
    #endif
#endif
}

static void display_error(char *txt)
{
    int i;

    printf("err: %6s  addr:%x\t ", txt, *ptr_PC);

    if (!strcmp(txt, "PC"))
    {
        printf("My PC: %x  Ref PC: %x\t ", *ptr_PC, *comp_reg_32);
    }
    else if (!strcmp(txt, "gpr"))
    {
        for (i=0; i<32; i++)
        {
            if (ptr_reg[i] != comp_reg_64[i])
                printf("My: reg[%d]=%llx\t Ref: reg[%d]=%llx\t ", i, ptr_reg[i], i, comp_reg_64[i]);
        }
    }
    else if (!strcmp(txt, "cop0"))
    {
        for (i=0; i<32; i++)
        {
            if (ptr_cop0[i] != comp_reg_32[i])
                printf("My: reg_cop0[%d]=%x\t Ref: reg_cop0[%d]=%x\t ", i, (unsigned int)ptr_cop0[i], i, (unsigned int)comp_reg_32[i]);
        }
    }
    else if (!strcmp(txt, "cop1"))
    {
        for (i=0; i<32; i++)
        {
            if (ptr_fgr[i] != comp_reg_64[i])
                printf("My: reg[%d]=%llx\t Ref: reg[%d]=%llx\t ", i, ptr_fgr[i], i, comp_reg_64[i]);
        }
    }
    printf("\n");
    /*for (i=0; i<32; i++)
      {
     if (reg_cop0[i] != comp_reg[i])
       printf("reg_cop0[%d]=%llx != reg[%d]=%llx\n",
          i, reg_cop0[i], i, comp_reg[i]);
      }*/

    stop_it();
}

static void compare_core_sync_data(int length, void *value)
{
    if (l_CoreCompareMode == CORE_COMPARE_RECV)
    {
        if (fread(value, 1, length, fPipe) != length)
            stop_it();
    }
    else if (l_CoreCompareMode == CORE_COMPARE_SEND)
    {
        if (fwrite(value, 1, length, fPipe) != length)
            stop_it();
    }
}

static void compare_core_check(unsigned int cur_opcode)
{
    static int comparecnt = 0;
    int iFirst = 1;
    char errHead[128];
    sprintf(errHead, "Compare #%i  old_op: %x op: %x\n", comparecnt++, old_op, cur_opcode);

    /* get pointer to current R4300 Program Counter address */
    ptr_PC = (int *) DebugGetCPUDataPtr(M64P_CPU_PC); /* this changes for every instruction */

    if (l_CoreCompareMode == CORE_COMPARE_RECV)
    {
        if (fread(comp_reg_32, sizeof(int), 4, fPipe) != 4)
            printf("compare_core_check: fread() failed");
        if (*ptr_PC != *comp_reg_32)
        {
            if (iFirst)
            {
                printf("%s", errHead);
                iFirst = 0;
            }
            display_error("PC");
        }
        if (fread (comp_reg_64, sizeof(long long int), 32, fPipe) != 32)
            printf("compare_core_check: fread() failed");
        if (memcmp(ptr_reg, comp_reg_64, 32*sizeof(long long int)) != 0)
        {
            if (iFirst)
            {
                printf("%s", errHead);
                iFirst = 0;
            }
            display_error("gpr");
        }
        if (fread(comp_reg_32, sizeof(int), 32, fPipe) != 32)
            printf("compare_core_check: fread() failed");
        if (memcmp(ptr_cop0, comp_reg_32, 32*sizeof(int)) != 0)
        {
            if (iFirst)
            {
                printf("%s", errHead);
                iFirst = 0;
            }
            display_error("cop0");
        }
        if (fread(comp_reg_64, sizeof(long long int), 32, fPipe) != 32)
            printf("compare_core_check: fread() failed");
        if (memcmp(ptr_fgr, comp_reg_64, 32*sizeof(long long int)))
        {
            if (iFirst)
            {
                printf("%s", errHead);
                iFirst = 0;
            }
            display_error("cop1");
        }
        /*fread(comp_reg, 1, sizeof(int), f);
        if (memcmp(&rdram[0x31280/4], comp_reg, sizeof(int)))
          display_error("mem");*/
        /*fread (comp_reg, 4, 1, f);
        if (memcmp(&FCR31, comp_reg, 4))
          display_error();*/
        old_op = cur_opcode;
    }
    else if (l_CoreCompareMode == CORE_COMPARE_SEND)
    {
        if (fwrite(ptr_PC, sizeof(int), 4, fPipe) != 4 ||
            fwrite(ptr_reg, sizeof(long long int), 32, fPipe) != 32 ||
            fwrite(ptr_cop0, sizeof(int), 32, fPipe) != 32 ||
            fwrite(ptr_fgr, sizeof(long long int), 32, fPipe) != 32)
            printf("compare_core_check: fwrite() failed");
        /*fwrite(&rdram[0x31280/4], 1, sizeof(int), f);
        fwrite(&FCR31, 4, 1, f);*/
    }
}

/* global functions */
void compare_core_init(int mode)
{
#if defined(WIN32)
    DebugMessage(M64MSG_VERBOSE, "core comparison feature not supported on Windows platform.");
    return;
#else
    /* set mode */
    l_CoreCompareMode = mode;
    /* set callback functions in core */
    if (DebugSetCoreCompare(compare_core_check, compare_core_sync_data) != M64ERR_SUCCESS)
    {
        l_CoreCompareMode = CORE_COMPARE_DISABLE;
        DebugMessage(M64MSG_WARNING, "DebugSetCoreCompare() failed, core comparison disabled.");
        return;
    }
    /* get pointers to emulated R4300 CPU registers */
    ptr_reg = (long long *) DebugGetCPUDataPtr(M64P_CPU_REG_REG);
    ptr_cop0 = (int *) DebugGetCPUDataPtr(M64P_CPU_REG_COP0);
    ptr_fgr = (long long *) DebugGetCPUDataPtr(M64P_CPU_REG_COP1_FGR_64);
    /* open file handle to FIFO pipe */
    if (l_CoreCompareMode == CORE_COMPARE_RECV)
    {
        mkfifo("compare_pipe", 0600);
        DebugMessage(M64MSG_INFO, "Core Comparison Waiting to read pipe.");
        fPipe = fopen("compare_pipe", "r");
    }
    else if (l_CoreCompareMode == CORE_COMPARE_SEND)
    {
        DebugMessage(M64MSG_INFO, "Core Comparison Waiting to write pipe.");
        fPipe = fopen("compare_pipe", "w");
    }
#endif
}

