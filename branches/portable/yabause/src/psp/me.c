/*  src/psp/me.c: PSP Media Engine access library
    Copyright 2010 Andrew Church

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
 * This library provides simple, low-level access to the Media Engine CPU.
 * Typical usage is as follows (error checks are omitted):
 *
 * init() {
 *     meStart();
 * }
 *
 * main() {
 *     meCall(function, parameter);
 *     meWait();  // or use mePoll() in a loop
 *     if (meException()) {
 *         // An exception (address error, etc.) occurred
 *     } else {
 *         result = meResult();
 *     }
 * }
 *
 * Code running on the Media Engine cannot call any external library
 * functions which are gated through the syscall interface (which naturally
 * includes all firmware functions).  However, a few utility functions are
 * available in me-utility.[ch] for operations such as cache management.
 *
 * This library trusts the caller completely, and runs code on the ME in
 * kernel mode.  Callers must therefore be careful about errors in the
 * functions they execute, since improper writes to hardware registers
 * could destroy flash or Memory Stick data or have other catastrophic
 * results.
 *
 * Naturally, this library cannot be used alongside any firmware functions
 * which make use of the ME, such as audio or video decoding.  It is also
 * currently impossible to suspend the PSP with the power switch after
 * calling meStart(), even if meStop() is later called to halt ME
 * processing.  (Further investigation is needed to determine why this
 * occurs.)
 */

#include <pspkernel.h>
extern int sceSysregAvcResetEnable(void);  // Missing from pspsysreg.h

#include "me.h"

/*************************************************************************/
/************************ PSP module information *************************/
/*************************************************************************/

#define MODULE_FLAGS \
    (PSP_MODULE_KERNEL | PSP_MODULE_SINGLE_LOAD | PSP_MODULE_SINGLE_START)
#define MODULE_VERSION   1
#define MODULE_REVISION  1

PSP_MODULE_INFO("melib", MODULE_FLAGS, MODULE_VERSION, MODULE_REVISION);

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Local function declarations */

static void maybe_raise_exception(void);

static int interrupt_handler(void);

extern void me_init, me_init_end;
__attribute__((section(".text.me"), noreturn)) void me_loop(void);
__attribute__((section(".text.me"), noreturn)) void me_exception_finish(void);

/*************************************************************************/

/* Has the ME been started? */

static int me_started;

/* Should ME exceptions be fatal to the caller? */

static int exceptions_are_fatal;

/*----------------------------------*/

/* Message block for signaling between the main CPU and Media Engine */

typedef struct MEMessageBlock_ {
    /* ME idle flag; nonzero indicates that the Media Engine is idle and
     * ready to accept a new execute request.  Set only by the ME; cleared
     * only by the main CPU. */
    int idle;

    /* Execute request flag; nonzero indicates that the execute request
     * fields below have been filled in and the ME should start executing
     * the given function.  Set only by the main CPU; cleared only by the
     * ME (except when cleared by the main CPU before starting the ME). */
    int request;

    /* Function to be executed.  Written only by the main CPU. */
    int (*function)(void *);

    /* Argument to be passed to function.  Written only by the main CPU. */
    void *argument;

    /* Return value of last function executed.  Written only by the ME. */
    int result;

    /* Exception flag; nonzero indicates that the last function executed on
     * the ME triggered an exception.  Set only by the ME; cleared only by
     * the main CPU. */
    int exception;
} MEMessageBlock;

static volatile __attribute__((aligned(64)))
    MEMessageBlock message_block_buffer;

/*----------------------------------*/

/* Buffer for storing registers on an ME exception (buffer size must be a
 * multiple of the cache line size) */

typedef struct MEExceptionRegs_ {
    uint32_t r[32];
    uint32_t hi, lo;
    uint32_t BadVAddr;
    uint32_t Status;
    uint32_t Cause;
    uint32_t EPC;
    uint32_t ErrorEPC;
    uint32_t pad[9];
} MEExceptionRegs;

static __attribute__((aligned(64),used)) MEExceptionRegs exception_registers;

/*----------------------------------*/

/* Event flag used for catching ME interrupts */

static SceUID interrupt_flag;

/*************************************************************************/
/************************** Module entry points **************************/
/*************************************************************************/

/**
 * module_start:  Entry point called when the module is started.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, otherwise an error code (negative)
 */
extern int module_start(void);
int module_start(void)
{
    /* Create an event flag for receiving interrupts from the ME. */
    interrupt_flag = sceKernelCreateEventFlag("meInterruptFlag",
                                              PSP_EVENT_WAITMULTIPLE, 0, 0);
    if (interrupt_flag < 0) {
        return interrupt_flag;
    }

    /* Install our interrupt handler (overwriting any handler installed
     * by the firmware). */
    sceKernelReleaseIntrHandler(31);
    int res =
        sceKernelRegisterIntrHandler(31, 2, interrupt_handler, NULL, NULL);
    if (res < 0) {
        sceKernelDeleteEventFlag(interrupt_flag);
        interrupt_flag = 0;
        return res;
    }
    sceKernelEnableIntr(31);

    return 0;
}

/*************************************************************************/

/**
 * module_stop:  Entry point called when the module is stopped.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, otherwise an error code (negative)
 */
extern int module_stop(void);
int module_stop(void)
{
    if (me_started) {
        meStop();
    }

    sceKernelDisableIntr(31);
    sceKernelReleaseIntrHandler(31);

    sceKernelDeleteEventFlag(interrupt_flag);
    interrupt_flag = 0;

    return 0;
}

/*************************************************************************/
/************************** Interface routines ***************************/
/*************************************************************************/

/**
 * meStart:  Start up the Media Engine.  This function must be called
 * before any other Media Engine operations are performed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero on success, otherwise an error code (negative)
 */
int meStart(void)
{
    uint32_t old_k1;
    asm volatile("move %0, $k1; move $k1, $zero" : "=r" (old_k1));

    if (me_started) {
        asm volatile("move $k1, %0" : : "r" (old_k1));
        return 0;
    }

    /* We generate this pointer on the fly rather than storing it in a
     * static variable because attempting to access that static variable
     * would pull in the cache line, which is exactly what we're trying
     * to avoid. */
    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    message_block->idle = 0;
    message_block->request = 0;
    meInterruptClear();

    /* Roll our own memcpy() to avoid an unneeded libc reference. */
    const uint32_t *src = (const uint32_t *)&me_init;
    const uint32_t *src_top = (const uint32_t *)&me_init_end;
    uint32_t *dest = (uint32_t *)0xBFC00040;
    for (; src < src_top; src++, dest++) {
        *dest = *src;
    }

    /* Flush the data cache, just to be safe. */
    sceKernelDcacheWritebackInvalidateAll();

    int res;
    if ((res = sceSysregMeResetEnable()) < 0
     || (res = sceSysregMeBusClockEnable()) < 0
     || (res = sceSysregMeResetDisable()) < 0
    ) {
        asm volatile("move $k1, %0" : : "r" (old_k1));
        return res;
    }

    me_started = 1;

    asm volatile("move $k1, %0" : : "r" (old_k1));
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * meStop:  Stop the Media Engine.  No other Media Engine functions except
 * meStart() may be called after this function returns.
 *
 * If code is currently executing on the Media Engine when this function is
 * called, the effect on system state is undefined.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void meStop(void)
{
    uint32_t old_k1;
    asm volatile("move %0, $k1; move $k1, $zero" : "=r" (old_k1));

    sceSysregVmeResetEnable();
    sceSysregAvcResetEnable();
    sceSysregMeResetEnable();
    sceSysregMeBusClockDisable();

    me_started = 0;

    asm volatile("move $k1, %0" : : "r" (old_k1));
}

/*************************************************************************/

/**
 * meCall:  Begin executing the given function on the Media Engine.  The
 * function must not call any firmware functions, whether directly or
 * indirectly.  This routine may only be called when the Media Engine is
 * idle (i.e., when mePoll() returns zero).
 *
 * [Parameters]
 *     function: Function pointer
 *     argument: Optional argument to function
 * [Return value]
 *     Zero on success, otherwise an error code (negative)
 */
int meCall(int (*function)(void *), void *argument)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    if (!message_block->idle || message_block->request) {
        return ME_ERROR_BUSY;
    }

    message_block->exception = 0;
    message_block->function = function;
    message_block->argument = argument;
    message_block->request = 1;
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * mePoll:  Check whether the Media Engine is idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if the Media Engine is idle, otherwise an error code (negative)
 */
int mePoll(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    if (!message_block->idle || message_block->request) {
        return ME_ERROR_BUSY;
    }
    maybe_raise_exception();
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * meWait:  Wait for the Media Engine to become idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if the Media Engine has become (or was already) idle,
 *     otherwise an error code (negative)
 */
int meWait(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    while (!message_block->idle || message_block->request) { /*spin*/ }
    maybe_raise_exception();
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * meResult:  Return the result (return value) of the most recently
 * executed function.  The result is undefined if the Media Engine has not
 * been started or is not idle, if the most recently executed function did
 * not return a result (i.e. had a return type of void), or if the most
 * recently executed function triggered an exception.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Result of the most recently executed function
 */
int meResult(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    return message_block->result;
}

/*************************************************************************/

/**
 * meException:  Return whether the most recently executed function
 * triggered an exception.  The result is undefined if the Media Engine has
 * not been started or is not idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the most recently executed function triggered an
 *     exception, else zero
 */
int meException(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    return message_block->exception;
}

/*-----------------------------------------------------------------------*/

/**
 * meExceptionGetData:  Retrieve CPU status register values related to the
 * most recent exception.  The values retrieved are undefined in any case
 * where meException() returns zero or has an undefined return value.
 *
 * NULL can be passed for any unneeded register values.
 *
 * [Parameters]
 *     BadVAddr_ret: Pointer to variable to receive BadVAddr register value
 *       Status_ret: Pointer to variable to receive Status register value
 *        Cause_ret: Pointer to variable to receive Cause register value
 *          EPC_ret: Pointer to variable to receive EPC register value
 *     ErrorEPC_ret: Pointer to variable to receive ErrorEPC register value
 * [Return value]
 *     None
 */
void meExceptionGetData(uint32_t *BadVAddr_ret, uint32_t *Status_ret,
                        uint32_t *Cause_ret, uint32_t *EPC_ret,
                        uint32_t *ErrorEPC_ret)
{
    if (!me_started) {
        return;
    }

    if (BadVAddr_ret) {
        *BadVAddr_ret = exception_registers.BadVAddr;
    }
    if (Status_ret) {
        *Status_ret = exception_registers.Status;
    }
    if (Cause_ret) {
        *Cause_ret = exception_registers.Cause;
    }
    if (EPC_ret) {
        *EPC_ret = exception_registers.EPC;
    }
    if (ErrorEPC_ret) {
        *ErrorEPC_ret = exception_registers.ErrorEPC;
    }
}

/*-----------------------------------------------------------------------*/

/**
 * meExceptionSetFatal:  Set whether an exception on the Media Engine
 * should automatically trigger an exception on the main CPU.  If enabled,
 * any call to mePoll() or meWait() when an exception is pending will cause
 * an address error exception to be generated on the main CPU, with
 * exception status information stored in a buffer pointed to by $gp:
 *     0($gp) = Status
 *     4($gp) = Cause
 *     8($gp)= EPC or ErrorEPC (depending on the exception type)
 *    12($gp) = BadVAddr
 *    16($gp) = Media Engine's $sp
 *    20($gp) = Media Engine's $gp
 * All general-purpose registers other than $sp and $gp, as well as $hi and
 * $lo, are copied from the Media Engine.
 *
 * Unlike other functions in this library, this function can be called even
 * when the ME is not running, and it will always succeed.
 *
 * By default, exceptions are not fatal.
 *
 * [Parameters]
 *     fatal: Nonzero to make exceptions fatal, zero to make them nonfatal
 * [Return value]
 *     None
 */
void meExceptionSetFatal(int fatal)
{
    exceptions_are_fatal = (fatal != 0);
}

/*************************************************************************/

/**
 * meInterruptPoll:  Return whether an interrupt from the Media Engine is
 * pending.  Any pending interrupt is _not_ cleared.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if an interrupt from the Media Engine is pending, otherwise an
 *     error code (negative)
 */
int meInterruptPoll(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    uint32_t old_k1;
    asm volatile("move %0, $k1; move $k1, $zero" : "=r" (old_k1));
    int res = sceKernelPollEventFlag(interrupt_flag, 1, 0, NULL);
    asm volatile("move $k1, %0" : : "r" (old_k1));

    if (res == SCE_KERNEL_ERROR_EVF_COND) {
        return ME_ERROR_NO_INTERRUPT;
    } else if (res < 0) {
        return res;
    }

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * meInterruptWait:  Wait for an interrupt from the Media Engine if none is
 * already pending, then clear the interrupt and return.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if an interrupt was received from the Media Engine, otherwise
 *     an error code (negative)
 */
int meInterruptWait(void)
{
    if (!me_started) {
        return ME_ERROR_NOT_STARTED;
    }

    uint32_t old_k1;
    asm volatile("move %0, $k1; move $k1, $zero" : "=r" (old_k1));
    int res = sceKernelWaitEventFlag(interrupt_flag, 1,
                                     PSP_EVENT_WAITCLEAR, NULL, NULL);
    asm volatile("move $k1, %0" : : "r" (old_k1));

    if (res < 0) {
        return res;
    }

    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * meInterruptClear:  Clear any pending Media Engine interrupt.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void meInterruptClear(void)
{
    uint32_t old_k1;
    asm volatile("move %0, $k1; move $k1, $zero" : "=r" (old_k1));
    /* This function is slightly misnamed--it doesn't clear the bits
     * specified by the second parameter, but simply performs a bitwise
     * AND between the current flag value and the second parameter. */
    sceKernelClearEventFlag(interrupt_flag, 0);
    asm volatile("move $k1, %0" : : "r" (old_k1));
}

/*************************************************************************/
/**************************** Local routines *****************************/
/*************************************************************************/

/**
 * maybe_raise_exception:  If fatal exceptions have been requested via
 * meExceptionSetFatal() and an ME exception is pending, load registers as
 * described in the meExceptionSetFatal() documentation and generate an
 * address error exception.
 *
 * Assumes that the ME is currently idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
static void maybe_raise_exception(void)
{
    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);
    static uint32_t exception_info[6];

    if (exceptions_are_fatal && message_block->exception) {
        asm volatile(".set push; .set noreorder; .set noat\n"
                     "move $at, %[exception_registers]\n"
                     "move $gp, %[exception_info]\n"
                     "lw $t8, %[Status]($at)\n"
                     "lw $t9, %[Cause]($at)\n"
                     "lw $k0, %[EPC]($at)\n"
                     "andi $v0, $t8, 4\n"
                     "bnezl $v0, 1f\n"
                     "lw $k0, %[ErrorEPC]($at)\n"
                     "1:\n"
                     "lw $k1, %[BadVAddr]($at)\n"
                     "lw $v0, 120($at)\n"
                     "lw $v1, 116($at)\n"
                     "sw $t8, 0($gp)\n"
                     "sw $t9, 4($gp)\n"
                     "sw $k0, 8($gp)\n"
                     "sw $k1, 12($gp)\n"
                     "sw $v0, 16($gp)\n"
                     "sw $v1, 20($gp)\n"
                     "lw $v0, 128($at)\n"
                     "lw $v1, 132($at)\n"
                     "mthi $v0\n"
                     "mtlo $v1\n"
                     "lw $v0, 8($at)\n"
                     "lw $v1, 12($at)\n"
                     "lw $a0, 16($at)\n"
                     "lw $a1, 20($at)\n"
                     "lw $a2, 24($at)\n"
                     "lw $a3, 28($at)\n"
                     "lw $t0, 32($at)\n"
                     "lw $t1, 36($at)\n"
                     "lw $t2, 40($at)\n"
                     "lw $t3, 44($at)\n"
                     "lw $t4, 48($at)\n"
                     "lw $t5, 52($at)\n"
                     "lw $t6, 56($at)\n"
                     "lw $t7, 60($at)\n"
                     "lw $s0, 64($at)\n"
                     "lw $s1, 68($at)\n"
                     "lw $s2, 72($at)\n"
                     "lw $s3, 76($at)\n"
                     "lw $s4, 80($at)\n"
                     "lw $s5, 84($at)\n"
                     "lw $s6, 88($at)\n"
                     "lw $s7, 92($at)\n"
                     "lw $t8, 96($at)\n"
                     "lw $t9, 100($at)\n"
                     "lw $k0, 104($at)\n"
                     "lw $k1, 108($at)\n"
                     "lw $fp, 120($at)\n"
                     "lw $ra, 124($at)\n"
                     "lw $at, 4($at)\n"
                     "sw $zero, -16162($zero)\n"
                     ".set pop"
                     : "=m" (exception_info)
                     : [exception_info] "r" (&exception_info),
                       [exception_registers] "r" (&exception_registers),
                       "m" (exception_registers),
                       [Status] "i" (offsetof(MEExceptionRegs,Status)),
                       [Cause] "i" (offsetof(MEExceptionRegs,Cause)),
                       [BadVAddr] "i" (offsetof(MEExceptionRegs,BadVAddr)),
                       [EPC] "i" (offsetof(MEExceptionRegs,EPC)),
                       [ErrorEPC] "i" (offsetof(MEExceptionRegs,ErrorEPC))
        );
    }
}

/*************************************************************************/

/**
 * interrupt_handler:  Handler for Media Engine interrupts.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Always -1
 */
static int interrupt_handler(void)
{
    sceKernelSetEventFlag(interrupt_flag, 1);
    return -1;
}

/*************************************************************************/

/**
 * me_init:  Initialization code for the Media Engine.  Copied to
 * 0xBFC00040 in shared memory space.
 */
asm("\
        .set push; .set noreorder                                       \n\
                                                                        \n\
        .globl me_init                                                  \n\
        .type me_init, @function                                        \n\
me_init:                                                                \n\
                                                                        \n\
        # Set up hardware registers.                                    \n\
        li $k0, 0xBC100000                                              \n\
        li $v0, 7                                                       \n\
        sw $v0, 0x50($k0)  # Enable bus clock                           \n\
        li $v0, -1                                                      \n\
        sw $v0, 0x04($k0)  # Clear pending interrupts                   \n\
        li $v0, 2          # Assume 64MB for now (watch your pointers!) \n\
        sw $v0, 0x40($k0)  # Set memory size                            \n\
                                                                        \n\
        # Clear the caches                                              \n\
        mtc0 $zero, $28    # TagLo                                      \n\
        mtc0 $zero, $29    # TagHi                                      \n\
        mfc0 $v1, $16      # Config                                     \n\
        ext $v0, $v1, 9, 3 # Instruction cache size                     \n\
        li $a0, 2048                                                    \n\
        sllv $a0, $a0, $v0                                              \n\
        ext $v0, $v1, 6, 3 # Data cache size                            \n\
        li $a1, 2048                                                    \n\
        sllv $a1, $a1, $v0                                              \n\
1:      addiu $a0, $a0, -64                                             \n\
        bnez $a0, 1b                                                    \n\
        cache 0x01, 0($a0) # Shouldn't this be 0x00?                    \n\
1:      addiu $a1, $a1, -64                                             \n\
        bnez $a1, 1b                                                    \n\
        cache 0x11, 0($a1)                                              \n\
                                                                        \n\
        # Enable the FPU (COP1) and clear the interrupt-pending flag.   \n\
        li $v0, 0x20000000                                              \n\
        mtc0 $v0, $12   # Status                                        \n\
        mtc0 $zero, $13 # Cause                                         \n\
                                                                        \n\
        # Wait for the hardware to become ready.                        \n\
        li $k0, 0xBCC00000                                              \n\
        li $v0, 1                                                       \n\
        sw $v0, 0x10($k0)                                               \n\
1:      lw $v0, 0x10($k0)                                               \n\
        andi $v0, $v0, 1                                                \n\
        bnez $v0, 1b                                                    \n\
        nop                                                             \n\
                                                                        \n\
        # Set up more hardware registers.                               \n\
        li $v0, 1                                                       \n\
        sw $v0, 0x70($k0)                                               \n\
        li $v0, 8                                                       \n\
        sw $v0, 0x30($k0)                                               \n\
        li $v0, 2  # Assume 64MB for now (watch your pointers!)         \n\
        sw $v0, 0x40($k0)                                               \n\
        sync                                                            \n\
                                                                        \n\
        # Start the internal clock counter running, in case someone     \n\
        # wants to access it.  (No Compare interrupts will be generated \n\
        # by default, since Status.IM[7] is cleared above.)             \n\
        mtc0 $zero, $9  # Count                                         \n\
                                                                        \n\
        # Set the exception handler address.                            \n\
        lui $v0, %hi(me_exception)                                      \n\
        addiu $v0, %lo(me_exception)                                    \n\
        mtc0 $v0, $25                                                   \n\
                                                                        \n\
        # Initialize the stack pointer and call the main loop.          \n\
        li $sp, 0x80200000                                              \n\
        lui $ra, %hi(me_loop)                                           \n\
        addiu $ra, %lo(me_loop)                                         \n\
        jr $ra                                                          \n\
        nop                                                             \n\
                                                                        \n\
        .globl me_init_end                                              \n\
me_init_end:                                                            \n\
                                                                        \n\
        .set pop                                                        \n\
");

/*-----------------------------------------------------------------------*/

/**
 * me_loop:  Media Engine main loop.  Infinitely repeats the cycle of
 * waiting for an execute request from the main CPU, then executing the
 * requested function.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Does not return
 * [Notes]
 *     This is not declared "static" because the address must be referenced
 *     by the assembly code in me_init().
 */
__attribute__((section(".text.me"), noreturn)) void me_loop(void)
{
    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    asm("move $k0, %0" : : "r" (ME_K0_MAGIC));

    for (;;) {
        message_block->idle = 1;
        while (!message_block->request) { /*spin*/ }
        message_block->idle = 0;
        int (*function)(void *) = message_block->function;
        void *argument = message_block->argument;
        message_block->request = 0;
        message_block->result = (*function)(argument);
    }
}

/*-----------------------------------------------------------------------*/

/**
 * me_exception:  Media Engine exception handler.  Stores exception data in
 * the local buffer, then clears the ME stack and restarts the main loop.
 */
asm("\
        .set push; .set noreorder; .set noat                            \n\
        .pushsection .text.me,\"ax\",@progbits                          \n\
        .balign 64                                                      \n\
                                                                        \n\
        .globl me_exception                                             \n\
        .type me_exception, @function                                   \n\
me_exception:                                                           \n\
                                                                        \n\
        # Save $v0 and $v1 so we have some work registers available.    \n\
        ctc0 $v0, $4                                                    \n\
        ctc0 $v1, $5                                                    \n\
                                                                        \n\
        # Save all registers to the exception register buffer.          \n\
        lui $v1, %hi(exception_registers)                               \n\
        addiu $v1, %lo(exception_registers)                             \n\
        cache 0x18, 0($v1)                                              \n\
        cache 0x18, 64($v1)                                             \n\
        cache 0x18, 128($v1)                                            \n\
        sw $zero, 0($v1)                                                \n\
        sw $at, 4($v1)                                                  \n\
        sw $v0, 8($v1)                                                  \n\
        cfc0 $v0, $5                                                    \n\
        sw $v0, 12($v1)                                                 \n\
        sw $a0, 16($v1)                                                 \n\
        sw $a1, 20($v1)                                                 \n\
        sw $a2, 24($v1)                                                 \n\
        sw $a3, 28($v1)                                                 \n\
        sw $t0, 32($v1)                                                 \n\
        sw $t1, 36($v1)                                                 \n\
        sw $t2, 40($v1)                                                 \n\
        sw $t3, 44($v1)                                                 \n\
        sw $t4, 48($v1)                                                 \n\
        sw $t5, 52($v1)                                                 \n\
        sw $t6, 56($v1)                                                 \n\
        sw $t7, 60($v1)                                                 \n\
        sw $s0, 64($v1)                                                 \n\
        sw $s1, 68($v1)                                                 \n\
        sw $s2, 72($v1)                                                 \n\
        sw $s3, 76($v1)                                                 \n\
        sw $s4, 80($v1)                                                 \n\
        sw $s5, 84($v1)                                                 \n\
        sw $s6, 88($v1)                                                 \n\
        sw $s7, 92($v1)                                                 \n\
        sw $t8, 96($v1)                                                 \n\
        sw $t9, 100($v1)                                                \n\
        sw $k0, 104($v1)                                                \n\
        sw $k1, 108($v1)                                                \n\
        sw $gp, 112($v1)                                                \n\
        sw $sp, 116($v1)                                                \n\
        sw $fp, 120($v1)                                                \n\
        sw $ra, 124($v1)                                                \n\
        mfhi $v0                                                        \n\
        sw $v0, 128($v1)                                                \n\
        mflo $v0                                                        \n\
        sw $v0, 132($v1)                                                \n\
        mfc0 $v0, $8                                                    \n\
        sw $v0, 136($v1)                                                \n\
        mfc0 $v0, $12                                                   \n\
        sw $v0, 140($v1)                                                \n\
        mfc0 $v0, $13                                                   \n\
        sw $v0, 144($v1)                                                \n\
        mfc0 $v0, $14                                                   \n\
        sw $v0, 148($v1)                                                \n\
        mfc0 $v0, $30                                                   \n\
        sw $v0, 152($v1)                                                \n\
        cache 0x1A, 0($v1)                                              \n\
        cache 0x1A, 64($v1)                                             \n\
        cache 0x1A, 128($v1)                                            \n\
                                                                        \n\
        # Run the remainder of the handler (compiled C code).           \n\
        j me_exception_finish                                           \n\
        nop                                                             \n\
                                                                        \n\
        .popsection                                                     \n\
        .set pop                                                        \n\
");

__attribute__((section(".text.me"), noreturn)) void me_exception_finish(void)
{
    volatile MEMessageBlock * const message_block = (volatile MEMessageBlock *)
        ((uintptr_t)&message_block_buffer | 0xA0000000);

    message_block->exception = 1;
    asm volatile("li $sp, 0x80200000");
    asm volatile("mtc0 %0, $14; "
                 "mtc0 %0, $30" : : "r" (me_loop));
    asm volatile("eret");
    for (;;) {}  // Unreachable, but tell the compiler we don't return
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
