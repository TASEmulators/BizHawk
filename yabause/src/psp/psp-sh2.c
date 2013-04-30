/*  src/psp/psp-sh2.c: Yabause interface for PSP SH-2 emulator
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

#include "common.h"

#include "../bios.h"
#include "../error.h"
#include "../memory.h"
#include "../sh2core.h"
#include "../sh2trace.h"
#include "../vdp1.h"
#include "../vdp2.h"

#include "config.h"
#include "psp-sh2.h"
#include "rtl.h"
#include "sh2.h"
#include "sh2-internal.h"  // For TRACE, etc. (so we don't call sh2_trace_add_cycles() if not needed)

#include "satopt-sh2.h"

/*************************************************************************/
/************************* Interface definition **************************/
/*************************************************************************/

/* Interface function declarations (must come before interface definition) */

static int psp_sh2_init(void);
static void psp_sh2_deinit(void);
static void psp_sh2_reset(void);
static FASTCALL void psp_sh2_exec(SH2_struct *state, u32 cycles);

static void psp_sh2_get_registers(SH2_struct *yabause_state,
                                  sh2regs_struct *regs);
static u32 psp_sh2_get_GPR(SH2_struct *yabause_state, int num);
static u32 psp_sh2_get_SR(SH2_struct *yabause_state);
static u32 psp_sh2_get_GBR(SH2_struct *yabause_state);
static u32 psp_sh2_get_VBR(SH2_struct *yabause_state);
static u32 psp_sh2_get_MACH(SH2_struct *yabause_state);
static u32 psp_sh2_get_MACL(SH2_struct *yabause_state);
static u32 psp_sh2_get_PR(SH2_struct *yabause_state);
static u32 psp_sh2_get_PC(SH2_struct *yabause_state);

static void psp_sh2_set_registers(SH2_struct *yabause_state,
                                  const sh2regs_struct *regs);
static void psp_sh2_set_GPR(SH2_struct *yabause_state, int num, u32 value);
static void psp_sh2_set_SR(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_GBR(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_VBR(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_MACH(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_MACL(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_PR(SH2_struct *yabause_state, u32 value);
static void psp_sh2_set_PC(SH2_struct *yabause_state, u32 value);

static void psp_sh2_send_interrupt(SH2_struct *yabause_state,
                                   u8 vector, u8 level);
static int psp_sh2_get_interrupts(SH2_struct *yabause_state,
                                  interrupt_struct interrupts[MAX_INTERRUPTS]);
static void psp_sh2_set_interrupts(SH2_struct *yabause_state,
                                   int num_interrupts,
                                   const interrupt_struct interrupts[MAX_INTERRUPTS]);

static void psp_sh2_write_notify(u32 start, u32 length);


/* Module interface definition */

SH2Interface_struct SH2PSP = {
    .id            = SH2CORE_PSP,
    .Name          = "PSP SH-2 Core",

    .Init          = psp_sh2_init,
    .DeInit        = psp_sh2_deinit,
    .Reset         = psp_sh2_reset,
    .Exec          = psp_sh2_exec,

    .GetRegisters  = psp_sh2_get_registers,
    .GetGPR        = psp_sh2_get_GPR,
    .GetSR         = psp_sh2_get_SR,
    .GetGBR        = psp_sh2_get_GBR,
    .GetVBR        = psp_sh2_get_VBR,
    .GetMACH       = psp_sh2_get_MACH,
    .GetMACL       = psp_sh2_get_MACL,
    .GetPR         = psp_sh2_get_PR,
    .GetPC         = psp_sh2_get_PC,

    .SetRegisters  = psp_sh2_set_registers,
    .SetGPR        = psp_sh2_set_GPR,
    .SetSR         = psp_sh2_set_SR,
    .SetGBR        = psp_sh2_set_GBR,
    .SetVBR        = psp_sh2_set_VBR,
    .SetMACH       = psp_sh2_set_MACH,
    .SetMACL       = psp_sh2_set_MACL,
    .SetPR         = psp_sh2_set_PR,
    .SetPC         = psp_sh2_set_PC,

    .SendInterrupt = psp_sh2_send_interrupt,
    .GetInterrupts = psp_sh2_get_interrupts,
    .SetInterrupts = psp_sh2_set_interrupts,

    .WriteNotify   = psp_sh2_write_notify,
};

/*************************************************************************/
/************************ Local data definitions *************************/
/*************************************************************************/

/* Master and slave SH-2 state blocks */
static SH2State *master_SH2, *slave_SH2;

/* Write buffers for low and high RAM */
static void *write_buffer_lram, *write_buffer_hram;

/*-----------------------------------------------------------------------*/

/* Local function declarations */
static void flush_caches(void *start, uint32_t length);
static void invalid_opcode_handler(SH2State *state, uint32_t PC,
                                   uint16_t opcode);
static FASTCALL void trace_insn_handler(SH2State *state, uint32_t address);

/*************************************************************************/
/********************** External interface routines **********************/
/*************************************************************************/

/**
 * psp_sh2_init:  Initialize the SH-2 core.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, negative on error
 */
static int psp_sh2_init(void)
{
    master_SH2 = sh2_create();
    slave_SH2 = sh2_create();
    if (!master_SH2 || !slave_SH2) {
        return -1;
    }
    master_SH2->userdata = MSH2;
    slave_SH2->userdata = SSH2;

    write_buffer_lram = sh2_alloc_direct_write_buffer(0x100000);
    write_buffer_hram = sh2_alloc_direct_write_buffer(0x100000);
    if (UNLIKELY(!write_buffer_lram) || UNLIKELY(!write_buffer_hram)) {
        DMSG("WARNING: Failed to allocate RAM write buffers, performance"
             " will suffer");
    }

    if (UNLIKELY(!sh2_init())) {
        return -1;
    }
#ifdef PSP
    sh2_set_optimizations(config_get_sh2_optimizations());
    /* If we can allocate >24MB of memory, we must be on a PSP-2000 (Slim)
     * or newer model; otherwise, assume we're on a PSP-1000 (Phat) and
     * reduce the JIT data size limit to avoid crowding out other data. */
    void *memsize_test = malloc(24*1024*1024 + 1);
    if (memsize_test) {
        free(memsize_test);
        sh2_set_jit_data_limit(20*1000000);
    } else {
        sh2_set_jit_data_limit(8*1000000);
    }
#else  // !PSP
    sh2_set_optimizations(SH2_OPTIMIZE_ASSUME_SAFE_DIVISION
                        | SH2_OPTIMIZE_BRANCH_TO_RTS
                        | SH2_OPTIMIZE_FOLD_SUBROUTINES
                        | SH2_OPTIMIZE_LOCAL_ACCESSES
                        | SH2_OPTIMIZE_LOCAL_POINTERS
                        | SH2_OPTIMIZE_MAC_NOSAT
                        | SH2_OPTIMIZE_POINTERS
                        | SH2_OPTIMIZE_POINTERS_MAC
                        | SH2_OPTIMIZE_STACK);
    /* Give the SH-2 core some breathing room for saving RTL blocks */
    sh2_set_jit_data_limit(200*1000000);
#endif
    sh2_set_manual_optimization_callback(saturn_optimize_sh2);
    sh2_set_cache_flush_callback(flush_caches);
    sh2_set_invalid_opcode_callback(invalid_opcode_handler);
    sh2_set_trace_insn_callback(trace_insn_handler);
    sh2_set_trace_storeb_callback(sh2_trace_writeb);
    sh2_set_trace_storew_callback(sh2_trace_writew);
    sh2_set_trace_storel_callback(sh2_trace_writel);

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sh2_deinit:  Perform cleanup for the SH-2 core.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_sh2_deinit(void)
{
    sh2_destroy(master_SH2);
    sh2_destroy(slave_SH2);

    free(write_buffer_lram);
    free(write_buffer_hram);
    write_buffer_lram = write_buffer_hram = NULL;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sh2_reset:  Reset the SH-2 core.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void psp_sh2_reset(void)
{
    /* Sanity checks on pointers -- none of these are actually possible
     * on the PSP, but it couldn't hurt to have them for testing this code
     * on other platforms. */
    if ((uintptr_t)BiosRom == 0x00000000UL
     || (uintptr_t)BiosRom == 0x00080000UL
     || (uintptr_t)BiosRom == 0x20000000UL
     || (uintptr_t)BiosRom == 0x20080000UL
     || (uintptr_t)BiosRom == 0xA0000000UL
     || (uintptr_t)BiosRom == 0xA0080000UL
     || (uintptr_t)LowWram == 0x00200000UL
     || (uintptr_t)LowWram == 0x20200000UL
     || (uintptr_t)LowWram == 0xA0200000UL
     || ((uintptr_t)HighWram & 0xFE0FFFFF) == 0x06000000UL
     || ((uintptr_t)HighWram & 0xFE0FFFFF) == 0x26000000UL
     || ((uintptr_t)HighWram & 0xFE0FFFFF) == 0xA6000000UL
    ) {
        DMSG("WARNING: ROM/RAM located at an inconvenient place;"
             " performance will suffer!\nROM=%p LRAM=%p HRAM=%p",
             BiosRom, LowWram, HighWram);
    }

#define SET_PAGE(sh2_addr,psp_addr,size,writebuf)  do {                 \
    if (!(psp_addr)) {                                                  \
        DMSG("WARNING: %s == NULL", #psp_addr);                         \
    } else {                                                            \
        sh2_set_direct_access((sh2_addr) | 0x00000000, (psp_addr),      \
                              (size), 1, (writebuf));                   \
        sh2_set_direct_access((sh2_addr) | 0x20000000, (psp_addr),      \
                              (size), 1, (writebuf));                   \
        sh2_set_direct_access((sh2_addr) | 0xA0000000, (psp_addr),      \
                              (size), 1, (writebuf));                   \
    }                                                                   \
} while (0)
#define SET_EXEC_PAGE(sh2_addr,psp_addr,size)  do {                     \
    if (!(psp_addr)) {                                                  \
        DMSG("WARNING: %s == NULL", #psp_addr);                         \
    } else {                                                            \
        sh2_set_direct_access((sh2_addr) | 0x00000000, (psp_addr),      \
                              (size), 0, 0);                            \
        sh2_set_direct_access((sh2_addr) | 0x20000000, (psp_addr),      \
                              (size), 0, 0);                            \
        sh2_set_direct_access((sh2_addr) | 0xA0000000, (psp_addr),      \
                              (size), 0, 0);                            \
    }                                                                   \
} while (0)
#define SET_BYTE_PAGE(sh2_addr,psp_addr,size)  do {                     \
    if (!(psp_addr)) {                                                  \
        DMSG("WARNING: %s == NULL", #psp_addr);                         \
    } else {                                                            \
        sh2_set_byte_direct_access((sh2_addr) | 0x00000000, (psp_addr), \
                                   (size));                             \
        sh2_set_byte_direct_access((sh2_addr) | 0x20000000, (psp_addr), \
                                   (size));                             \
        sh2_set_byte_direct_access((sh2_addr) | 0xA0000000, (psp_addr), \
                                   (size));                             \
    }                                                                   \
} while (0)

    SET_EXEC_PAGE(0x00000000, BiosRom, 0x80000);
    SET_EXEC_PAGE(0x00080000, BiosRom, 0x80000);
    if (write_buffer_lram) {
        SET_PAGE(0x00200000, LowWram, 0x100000, write_buffer_lram);
    }
    if (write_buffer_hram) {
        uint32_t base;
        for (base = 0x06000000; base < 0x08000000; base += 0x00100000) {
            SET_PAGE(base, HighWram, 0x100000, write_buffer_hram);
        }
    }
    SET_BYTE_PAGE(0x05C00000, Vdp1Ram, 0x80000);
    SET_BYTE_PAGE(0x05E00000, Vdp2Ram, 0x80000);
    SET_BYTE_PAGE(0x05E80000, Vdp2Ram, 0x80000);

#undef SET_PAGE
#undef SET_EXEC_PAGE
#undef SET_BYTE_PAGE

    sh2_reset(master_SH2);
    sh2_reset(slave_SH2);
}

/*************************************************************************/

/**
 * psp_sh2_exec:  Execute instructions for the given number of clock cycles.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *            cycles: Number of clock cycles to execute
 * [Return value]
 *     None
 */
static FASTCALL void psp_sh2_exec(SH2_struct *yabause_state, u32 cycles)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;

    state->cycles = yabause_state->cycles;

#if defined(TRACE) || defined(TRACE_STEALTH) || defined(TRACE_LITE)
    /* Avoid accumulating leftover cycles multiple times, since the trace
     * code automatically adds state->cycles to the cycle accumulator when
     * printing a trace line */
    sh2_trace_add_cycles(-(state->cycles));
#endif
    sh2_run(state, cycles);
#if defined(TRACE) || defined(TRACE_STEALTH) || defined(TRACE_LITE)
    sh2_trace_add_cycles(state->cycles);
#endif

    yabause_state->cycles = state->cycles;
}

/*************************************************************************/

/**
 * psp_sh2_get_registers:  Retrieve the values of all SH-2 registers.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *              regs: Structure to receive register values
 * [Return value]
 *     None
 */
static void psp_sh2_get_registers(SH2_struct *yabause_state,
                                  sh2regs_struct *regs)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    memcpy(regs, state, sizeof(*regs));
}

/*----------------------------------*/

/**
 * psp_sh2_get_{GPR,SR,GBR,VBR,MACH,MACL,PR,PC}:  Return the value of the
 * named register.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *               num: General purpose register number to get (get_GPR() only)
 * [Return value]
 *     Register's value
 */
static u32 psp_sh2_get_GPR(SH2_struct *yabause_state, int num)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->R[num];
}

static u32 psp_sh2_get_SR(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->SR;
}

static u32 psp_sh2_get_GBR(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->GBR;
}

static u32 psp_sh2_get_VBR(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->VBR;
}

static u32 psp_sh2_get_MACH(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->MACH;
}

static u32 psp_sh2_get_MACL(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->MACL;
}

static u32 psp_sh2_get_PR(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->PR;
}

static u32 psp_sh2_get_PC(SH2_struct *yabause_state)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    return state->PC;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sh2_set_registers:  Set the values of all SH-2 registers.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *              regs: Structure containing new values for registers
 * [Return value]
 *     None
 */
static void psp_sh2_set_registers(SH2_struct *yabause_state,
                                  const sh2regs_struct *regs)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    memcpy(state, regs, sizeof(*regs));
}

/*----------------------------------*/

/**
 * psp_sh2_set_{GPR,SR,GBR,VBR,MACH,MACL,PR,PC}:  Set the value of the
 * named register.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *               num: General purpose register number to get (get_GPR() only)
 *             value: New value for register
 * [Return value]
 *     None
 */
static void psp_sh2_set_GPR(SH2_struct *yabause_state, int num, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->R[num] = value;
}

static void psp_sh2_set_SR(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->SR = value;
}

static void psp_sh2_set_GBR(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->GBR = value;
}

static void psp_sh2_set_VBR(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->VBR = value;
}

static void psp_sh2_set_MACH(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->MACH = value;
}

static void psp_sh2_set_MACL(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->MACL = value;
}

static void psp_sh2_set_PR(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->PR = value;
}

static void psp_sh2_set_PC(SH2_struct *yabause_state, u32 value)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->PC = value;
}

/*************************************************************************/

/**
 * psp_sh2_send_interrupt:  Send an interrupt to the given SH-2 processor.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 * [Return value]
 *     None
 */
static void psp_sh2_send_interrupt(SH2_struct *yabause_state,
                                   u8 vector, u8 level)
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    if (UNLIKELY(vector > 127)) {
        return;
    }
    sh2_signal_interrupt(state, vector, level);
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sh2_set_interrupts:  Set the state of the interrupt stack.
 *
 * [Parameters]
 *     yabause_state: Yabause SH-2 context structure
 *        interrupts: Array to receive interrupt data
 * [Return value]
 *     Number of pending interrupts
 */
static int psp_sh2_get_interrupts(SH2_struct *yabause_state,
                                  interrupt_struct interrupts[MAX_INTERRUPTS])
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    int i;
    for (i = 0; i < state->interrupt_stack_top && i < MAX_INTERRUPTS; i++) {
        interrupts[i].level = state->interrupt_stack[i].level;
        interrupts[i].vector = state->interrupt_stack[i].vector;
    }
    return state->interrupt_stack_top;
}

/*-----------------------------------------------------------------------*/

/**
 * psp_sh2_set_interrupts:  Set the state of the interrupt stack.
 *
 * [Parameters]
 *      yabause_state: Yabause SH-2 context structure
 *     num_interrupts: Number of pending interrupts
 *         interrupts: Array of pending interrupts
 * [Return value]
 *     None
 */
static void psp_sh2_set_interrupts(SH2_struct *yabause_state,
                                   int num_interrupts,
                                   const interrupt_struct interrupts[MAX_INTERRUPTS])
{
    SH2State *state = (yabause_state == MSH2) ? master_SH2 : slave_SH2;
    state->interrupt_stack_top = 0;
    int i;
    for (i = 0; i < num_interrupts; i++) {
        if (UNLIKELY(interrupts[i].vector > 127)) {
            return;
        }
        sh2_signal_interrupt(state, interrupts[i].vector, interrupts[i].level);
    }
}

/*************************************************************************/

/**
 * psp_sh2_write_notify:  Called when an external agent modifies memory.
 *
 * [Parameters]
 *     address: Beginning of address range to which data was written
 *        size: Size of address range to which data was written (in bytes)
 * [Return value]
 *     None
 */
static void psp_sh2_write_notify(u32 address, u32 size)
{
    sh2_write_notify(address, size);
}

/*************************************************************************/
/**************************** Local functions ****************************/
/*************************************************************************/

/**
 * flush_caches:  Callback function to flush the native CPU's caches.
 *
 * [Parameters]
 *      start: Pointer to start of range
 *     length: Length of range in bytes
 * [Return value]
 *     None
 */
static void flush_caches(void *start, uint32_t length)
{
#ifdef PSP  // Protect so we can test this SH-2 core on other platforms
    sceKernelDcacheWritebackInvalidateRange(start, length);
    sceKernelIcacheInvalidateRange(start, length);
#endif
}

/*-----------------------------------------------------------------------*/

/**
 * invalid_opcode_handler:  Callback function for invalid opcodes detected
 * in the instruction stream.
 *
 * [Parameters]
 *      state: Processor state block pointer
 *         PC: PC at which the invalid instruction was found
 *     opcode: The invalid opcode itself
 * [Return value]
 *     None
 */
static void invalid_opcode_handler(SH2State *state, uint32_t PC,
                                   uint16_t opcode)
{
    SH2_struct *yabause_state = (SH2_struct *)(state->userdata);
    uint32_t saved_PC = state->PC;
    state->PC = PC;  // Show the proper PC in the error message
    yabause_state->instruction = opcode;
    YabSetError(YAB_ERR_SH2INVALIDOPCODE, yabause_state);
    state->PC = saved_PC;
}

/*-----------------------------------------------------------------------*/

/**
 * trace_insn_handler:  Callback function for tracing instructions.
 * Updates the appropriate Yabause SH2_struct's registers and cycle count,
 * then calls out to the common SH-2 tracing functionality.
 *
 * [Parameters]
 *       state: Processor state block pointer
 *     address: Address of instruction to trace
 * [Return value]
 *     None
 */
static FASTCALL void trace_insn_handler(SH2State *state, uint32_t address)
{
    SH2_struct *yabause_state = (SH2_struct *)(state->userdata);
    yabause_state->cycles = state->cycles;
    sh2_trace(yabause_state, address);
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
