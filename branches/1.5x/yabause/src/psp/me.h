/*  src/psp/me.h: PSP Media Engine access library header
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

#ifndef ME_H
#define ME_H

/*************************************************************************/

/* Error codes specific to Media Engine routines */

enum {
    /* Media Engine has not been successfully started with meStart() */
    ME_ERROR_NOT_STARTED = 0x90000001,
    /* Media Engine is currently executing a function */
    ME_ERROR_BUSY = 0x90000002,
    /* No Media Engine interrupt is pending */
    ME_ERROR_NO_INTERRUPT = 0x90000003,
};

/*----------------------------------*/

/* Magic value stored in $k0 to indicate that code is running on the ME */

#define ME_K0_MAGIC  0x3E3E3E3E

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
extern int meStart(void);

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
extern void meStop(void);

/*----------------------------------*/

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
extern int meCall(int (*function)(void *), void *argument);

/**
 * mePoll:  Check whether the Media Engine is idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if the Media Engine is idle, otherwise an error code (negative)
 */
extern int mePoll(void);

/**
 * meWait:  Wait for the Media Engine to become idle.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Zero if the Media Engine has become (or was already) idle,
 *     otherwise an error code (negative)
 */
extern int meWait(void);

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
extern int meResult(void);

/*----------------------------------*/

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
extern int meException(void);

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
extern void meExceptionGetData(uint32_t *BadVAddr_ret, uint32_t *Status_ret,
                               uint32_t *Cause_ret, uint32_t *EPC_ret,
                               uint32_t *ErrorEPC_ret);

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
extern void meExceptionSetFatal(int fatal);

/*----------------------------------*/

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
extern int meInterruptPoll(void);

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
extern int meInterruptWait(void);

/**
 * meInterruptClear:  Clear any pending Media Engine interrupt.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void meInterruptClear(void);

/*************************************************************************/

#endif  // ME_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
