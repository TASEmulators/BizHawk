/*  src/psp/me-test.c: Test program for PSP Media Engine access library
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

#include <stdio.h>
#include <string.h>

#include <pspuser.h>
/* Helpful hint for GCC */
extern void sceKernelExitGame(void) __attribute__((noreturn));

#include "me.h"
#include "me-utility.h"

/* Macro to get the length of an array */
#define lenof(a)  (sizeof((a)) / sizeof((a)[0]))

/* Size of the ME's data cache, in bytes */
#define ME_DCACHE_SIZE  (256*64)  // 256 cache lines of 64 bytes each

/*************************************************************************/
/************************ PSP module information *************************/
/*************************************************************************/

#define MODULE_FLAGS \
    (PSP_MODULE_USER | PSP_MODULE_SINGLE_LOAD | PSP_MODULE_SINGLE_START)
#define MODULE_VERSION   1
#define MODULE_REVISION  0

PSP_MODULE_INFO("me-test", MODULE_FLAGS, MODULE_VERSION, MODULE_REVISION);
const PSP_MAIN_THREAD_PRIORITY(32);
const PSP_MAIN_THREAD_STACK_SIZE_KB(64);
const PSP_MAIN_THREAD_ATTR(PSP_THREAD_ATTR_USER);
const PSP_HEAP_SIZE_KB(-64);

/*************************************************************************/
/******************* Forward declarations of routines ********************/
/*************************************************************************/

static int test_meStart(void);
static int test_meCall(void);
static int test_mePoll(void);
static int test_meWait(void);
static int test_meResult(void);
static int test_meException(void);
static int test_meStop(void);
static int test_restart(void);
static int test_meIsME_SC(void);
static int test_meIsME_ME(void);
static int test_meInterruptWait(void);
static int test_meInterruptPoll(void);
static int test_meInterruptClear(void);
static int test_icache(void);
static int test_icache_inval(void);
static int test_dcache_read(void);
static int test_dcache_write(void);
static int test_dcache_inval(void);
static int test_dcache_wbinv(void);

static int mefunc_return_123(void *param);
static int mefunc_store_456(void *param);
static int mefunc_delay_and_store_789(void *param);
static int mefunc_count_forever(void *param);
static int mefunc_address_error(void *param);
static int mefunc_return_IsME(void *param);
static int mefunc_send_interrupt(void *param);
static int mefunc_dcache_read(void *param);
static int mefunc_dcache_write(void *param);
static int mefunc_dcache_inval(void *param);
static int mefunc_dcache_wbinv(void *param);

static __attribute__((always_inline)) void delay(const unsigned int cycles);

/*************************************************************************/
/***************************** List of tests *****************************/
/*************************************************************************/

typedef struct TestInfo_ {
    /* Name of this test */
    const char * const name;

    /* Name of a previous test which must have passed in order to run this
     * test (or NULL if none) */
    const char * const precondition;

    /* Routine implementing test */
    int (* const routine)(void);

    /* Result of test (nonzero = passed, zero = failed or not executed) */
    int passed;
} TestInfo;

static TestInfo tests[] = {
    {"meStart",          NULL,              test_meStart},
    {"meCall",           "meStart",         test_meCall},
    {"mePoll",           "meStart",         test_mePoll},
    {"meWait",           "meStart",         test_meWait},
    {"meResult",         "meWait",          test_meResult},
    {"meException",      "meWait",          test_meException},
    {"meStop",           "meResult",        test_meStop},
    {"restart",          "meStop",          test_restart},

    {"meIsME-SC",        "restart",         test_meIsME_SC},
    {"meIsME-ME",        "meIsME-SC",       test_meIsME_ME},

    {"meInterruptWait",  "restart",         test_meInterruptWait},
    {"meInterruptPoll",  "meInterruptWait", test_meInterruptPoll},
    {"meInterruptClear", "meInterruptPoll", test_meInterruptClear},

    {"icache",           "restart",         test_icache},
    {"icache-inval",     "restart",         test_icache_inval},
    {"dcache-read",      "restart",         test_dcache_read},
    {"dcache-write",     "restart",         test_dcache_write},
    {"dcache-inval",     "restart",         test_dcache_inval},
    {"dcache-wbinv",     "restart",         test_dcache_wbinv},
};

/*************************************************************************/
/****************************** Entry point ******************************/
/*************************************************************************/

/**
 * main:  Program entry point.  Calls each of the individual tests and
 * reports on their success or failure.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Does not return
 */
int main(void)
{
    /* Don't buffer stdout (since we print partial lines as we test). */
    setbuf(stdout, NULL);

    /* Wait a moment to let PSPlink print its "module started" line before
     * we start outputting anything, and output a blank line so the first
     * line doesn't follow the PSPlink prompt. */
    sceKernelDelayThread(100000);
    printf("\n");

    /* Load the ME library. */
    SceKernelLMOption lmopts;
    memset(&lmopts, 0, sizeof(lmopts));
    lmopts.size     = sizeof(lmopts);
    lmopts.mpidtext = PSP_MEMORY_PARTITION_KERNEL;
    lmopts.mpiddata = PSP_MEMORY_PARTITION_KERNEL;
    lmopts.position = 0;
    lmopts.access   = 1;
    SceUID modid = sceKernelLoadModule("me.prx", 0, &lmopts);
    if (modid < 0) {
        fprintf(stderr, "Failed to load me.prx: %08X\n", modid);
        sceKernelExitGame();
    }
    int dummy;
    int res = sceKernelStartModule(modid, strlen("me.prx")+1, "me.prx",
                                   &dummy, NULL);
    if (res < 0) {
        fprintf(stderr, "Failed to start me.prx: %08X\n", res);
        sceKernelUnloadModule(modid);
        sceKernelExitGame();
    }

    /* Run all tests in order. */
    unsigned int num_passed = 0, num_skipped = 0;
    unsigned int i;
    for (i = 0; i < lenof(tests); i++) {
        printf("%s...", tests[i].name);
        int can_run = 1;
        if (tests[i].precondition) {
            unsigned int precond_index;
            for (precond_index = 0; precond_index < i; precond_index++) {
                if (strcmp(tests[i].precondition,
                           tests[precond_index].name) == 0) {
                    break;
                }
            }
            if (precond_index >= i) {
                printf("skipped (precondition \"%s\" not found or not yet"
                       " run)\n", tests[i].precondition);
                can_run = 0;
            } else if (!tests[precond_index].passed) {
                printf("skipped (precondition \"%s\" failed or was skipped)",
                       tests[i].precondition);
                can_run = 0;
            }
        }
        if (can_run) {
            tests[i].passed = (*tests[i].routine)();
            if (tests[i].passed) {
                num_passed++;
                printf("passed\n");
            } else {
                printf("FAILED\n");
            }
        } else {
            num_skipped++;
        }
    }

    /* Print a summary of all results. */
    printf("\n");
    if (num_passed == lenof(tests)) {
        printf("All tests passed.\n");
    } else {
        printf("%u/%u tests passed (%u failed, %u skipped).\n",
               num_passed, lenof(tests),
               lenof(tests) - num_passed - num_skipped, num_skipped);
    }
    printf("\n");

    /* All done. */
    sceKernelExitGame();
}

/*************************************************************************/
/***************************** Test routines *****************************/
/*************************************************************************/

/**
 * test_meStart:  Test that the meStart() function succeeds on both the
 * first call and a subsequent call.
 *
 * If the test passes, the ME has been started and can be used in
 * subsequent tests.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meStart(void)
{
    int res;

    if ((res = meStart()) != 0) {
        fprintf(stderr, "meStart() #1 failed: %08X\n", res);
        return 0;
    }

    /* Wait for the ME to become ready (done with a manual delay loop since
     * we don't assume that any other library functions work yet). */
    delay(1000000);

    if ((res = meStart()) != 0) {
        fprintf(stderr, "meStart() #2 failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meCall:  Test that the meCall() function can be used to execute a
 * routine on the ME.
 *
 * Assumes that test_meStart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meCall(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_store_456, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    /* Since we don't yet assume meWait() to work properly, delay long
     * enough for the function to finish executing before we check whether
     * it successfully ran. */
    delay(1000000);
    if ((res = *bufptr) != 456) {
        fprintf(stderr, "Bad value in buffer (expected 456, got %d)", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_mePoll:  Test that the mePoll() function can be used to check
 * whether the ME is idle.
 *
 * Assumes that test_meStart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_mePoll(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_delay_and_store_789, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = *bufptr) != 0) {
        fprintf(stderr, "Bad value in buffer (expected 0, got %d)", res);
        return 0;
    }

    if ((res = mePoll()) != ME_ERROR_BUSY) {
        if (res == 0) {
            fprintf(stderr, "mePoll() #1 returned idle\n");
        } else {
            fprintf(stderr, "mePoll() #1 failed: %08X\n", res);
        }
        return 0;
    }

    delay(2000000);
    if ((res = *bufptr) != 789) {
        fprintf(stderr, "Bad value in buffer (expected 789, got %d)", res);
        return 0;
    }

    if ((res = mePoll()) != 0) {
        fprintf(stderr, "mePoll() #2 failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meWait:  Test that the meWait() function can be used to wait for
 * the ME to become idle.
 *
 * Assumes that test_meStart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meWait(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_delay_and_store_789, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = *bufptr) != 0) {
        fprintf(stderr, "Bad value in buffer (expected 0, got %d)", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1 failed: %08X\n", res);
        return 0;
    }

    if ((res = *bufptr) != 789) {
        fprintf(stderr, "Bad value in buffer (expected 789, got %d)", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2 failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meResult:  Test that the meResult() function can be used to
 * retrieve the result of a routine executed on the ME.
 *
 * Assumes that test_meWait() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meResult(void)
{
    int res;

    if ((res = meCall(mefunc_return_123, NULL)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    if ((res = meResult()) != 123) {
        fprintf(stderr, "meResult() gave wrong result (expected 123, got"
                " %d)\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meException:  Test that the meException() function can be used to
 * retrieve the exception status of a routine executed on the ME.
 *
 * Assumes that test_meWait() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meException(void)
{
    int res;

    if ((res = meCall(mefunc_return_123, NULL)) != 0) {
        fprintf(stderr, "meCall() #1 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1 failed: %08X\n", res);
        return 0;
    }

    if ((res = meException()) != 0) {
        fprintf(stderr, "meException() #1 gave wrong result (expected 0, got"
                " %d)\n", res);
        return 0;
    }

    if ((res = meCall(mefunc_address_error, NULL)) != 0) {
        fprintf(stderr, "meCall() #2 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2 failed: %08X\n", res);
        return 0;
    }

    if ((res = meException()) == 0) {
        fprintf(stderr, "meException() #2 gave wrong result (expected"
                " nonzero, got zero)\n");
        return 0;
    }

    uint32_t BadVAddr = 0, Status = 0, Cause = 0, EPC = 0, ErrorEPC = 0;
    meExceptionGetData(&BadVAddr, &Status, &Cause, &EPC, &ErrorEPC);
    fprintf(stderr, "Exception data:\n");
    fprintf(stderr, "    BadVAddr = %08X\n", BadVAddr);
    fprintf(stderr, "    Status   = %08X\n", Status);
    fprintf(stderr, "    Cause    = %08X\n", Cause);
    fprintf(stderr, "    EPC      = %08X\n", EPC);
    fprintf(stderr, "    ErrorEPC = %08X\n", ErrorEPC);

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meStop:  Test that the meStop() function can be used to halt the ME.
 *
 * Assumes that test_meResult() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meStop(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_count_forever, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    delay(1000000);  // Let the counter run for a bit

    /* Stopping the ME while a function is executing is technically an
     * undefined operation, but since all the function does is increment a
     * counter, we assume it's safe. */
    meStop();

    delay(1000000);  // The ME should be stopped already, but wait to be sure

    int last_count = *bufptr;
    delay(1000000);
    if ((res = *bufptr) != last_count) {
        fprintf(stderr, "meStop() failed to stop ME (counter changed from"
                " %d to %d)", last_count, res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_restart:  Test that the ME can be safely stopped and restarted
 * using the meStop() and meStart() functions.
 *
 * Assumes that test_meStop() has passed and the ME is currently stopped.
 * If the test passes, the ME has been restarted and can be used in
 * subsequent tests.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_restart(void)
{
    int res;

    if ((res = meStart()) != 0) {
        fprintf(stderr, "meStart() #1 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1A failed: %08X\n", res);
        return 0;
    }

    if ((res = meCall(mefunc_return_123, NULL)) != 0) {
        fprintf(stderr, "meCall() #1 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1B failed: %08X\n", res);
        return 0;
    }

    if ((res = meResult()) != 123) {
        fprintf(stderr, "meResult() #1 gave wrong result (expected 123, got"
                " %d)\n", res);
        return 0;
    }

    meStop();
    delay(1000000);  // Wait for the system to settle (just in case)

    if ((res = meStart()) != 0) {
        fprintf(stderr, "meStart() #2 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2A failed: %08X\n", res);
        return 0;
    }

    if ((res = meCall(mefunc_return_123, NULL)) != 0) {
        fprintf(stderr, "meCall() #2 failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2B failed: %08X\n", res);
        return 0;
    }

    if ((res = meResult()) != 123) {
        fprintf(stderr, "meResult() #2 gave wrong result (expected 123, got"
                " %d)\n", res);
        return 0;
    }

    return 1;
}

/*************************************************************************/

/**
 * test_meIsME_SC:  Test that meUtilityIsME() properly returns zero when
 * executing on the SC.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meIsME_SC(void)
{
    int res = meUtilityIsME();

    if (res != 0) {
        fprintf(stderr, "meUtilityIsME() returned nonzero (%d) on SC\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meIsME_ME:  Test that meUtilityIsME() properly returns nonzero when
 * executing on the ME.
 *
 * Assumes that test_meIsME() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meIsME_ME(void)
{
    int res;

    if ((res = meCall(mefunc_return_IsME, NULL)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    if ((res = meResult()) == 0) {
        fprintf(stderr, "meResult() gave wrong result (expected nonzero,"
                " got 0)\n");
        return 0;
    }

    return 1;
}

/*************************************************************************/

/**
 * test_meInterruptWait:  Test that meUtilitySendInterrupt() can be used by
 * ME code to send an interrupt to the main CPU, and that meInterruptWait()
 * can be used to wait until the interrupt is received.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meInterruptWait(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_send_interrupt, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = *bufptr) != 0) {
        fprintf(stderr, "Bad value in buffer (expected 0, got %d)", res);
        return 0;
    }

    if ((res = meInterruptWait()) != 0) {
        fprintf(stderr, "meInterruptWait() failed: %08X\n", res);
        return 0;
    }

    if ((res = *bufptr) != 654) {
        fprintf(stderr, "Bad value in buffer (expected 654, got %d)", res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meInterruptPoll:  Test that meInterruptPoll() can be used to check
 * whether an ME interrupt is pending without clearing the interrupt.  Also
 * check that meInterruptWait() clears the interrupt after waiting for it.
 *
 * Assumes that test_meInterruptWait() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meInterruptPoll(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_send_interrupt, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = meInterruptPoll()) != ME_ERROR_NO_INTERRUPT) {
        fprintf(stderr, "meInterruptPoll() #1 returned bad value (expected"
                " %08X, got %08X)\n", ME_ERROR_NO_INTERRUPT, res);
        return 0;
    }

    delay(2000000);

    if ((res = meInterruptPoll()) != 0) {
        fprintf(stderr, "meInterruptPoll() #2 failed: %08X\n", res);
        return 0;
    }

    if ((res = meInterruptWait()) != 0) {
        fprintf(stderr, "meInterruptWait() failed: %08X\n", res);
        return 0;
    }

    if ((res = meInterruptPoll()) != ME_ERROR_NO_INTERRUPT) {
        fprintf(stderr, "meInterruptPoll() #3 returned bad value (expected"
                " %08X, got %08X)\n", ME_ERROR_NO_INTERRUPT, res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_meInterruptClear:  Test that meInterruptClear() can be used to
 * clear a pending ME interrupt.
 *
 * Assumes that test_meInterruptPoll() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_meInterruptClear(void)
{
    static __attribute__((aligned(64))) int buffer;
    volatile int *bufptr = (volatile int *)((uintptr_t)&buffer | 0x40000000);
    int res;

    *bufptr = 0;
    if ((res = meCall(mefunc_send_interrupt, (void *)bufptr)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    if ((res = meInterruptPoll()) != ME_ERROR_NO_INTERRUPT) {
        fprintf(stderr, "meInterruptPoll() #1 returned bad value (expected"
                " %08X, got %08X)\n", ME_ERROR_NO_INTERRUPT, res);
        return 0;
    }

    delay(2000000);

    if ((res = meInterruptPoll()) != 0) {
        fprintf(stderr, "meInterruptPoll() #2 failed: %08X\n", res);
        return 0;
    }

    meInterruptClear();

    if ((res = meInterruptPoll()) != ME_ERROR_NO_INTERRUPT) {
        fprintf(stderr, "meInterruptPoll() #3 returned bad value (expected"
                " %08X, got %08X)\n", ME_ERROR_NO_INTERRUPT, res);
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*************************************************************************/

/**
 * test_icache:  Test that the ME's instruction cache operates as expected.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_icache(void)
{
    static __attribute__((aligned(64))) uint32_t insn_buf[2];
    volatile uint32_t *insn_ptr =
        (volatile uint32_t *)((uintptr_t)&insn_buf[0] | 0x40000000);
    int res;

    insn_ptr[0] = 0x03E00008;  // jr $ra
    insn_ptr[1] = 0x34021234;  // li $v0, 0x1234

    /* Note that we use insn_buf (the cacheable version) rather than
     * insn_ptr (the uncacheable one), because we want the ME to cache the
     * instructions and therefore _not_ pick up our subsequent change. */
    if ((res = meCall((void *)insn_buf, NULL)) != 0) {
        fprintf(stderr, "meCall() #1 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x1234) {
        fprintf(stderr, "meResult() #1 gave wrong result (expected 0x1234,"
                " got 0x%X)\n", res);
        return 0;
    }

    insn_ptr[1] = 0x34025678;  // li $v0, 0x5678 (not seen by ME)

    if ((res = meCall((void *)insn_buf, NULL)) != 0) {
        fprintf(stderr, "meCall() #2 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x1234) {
        fprintf(stderr, "meResult() #2 gave wrong result (expected 0x1234,"
                " got 0x%X)\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_icache_inval:  Test that meUtilityIcacheInvalidateAll() can be used
 * to invalidate the ME's instruction cache.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_icache_inval(void)
{
    static __attribute__((aligned(64))) uint32_t insn_buf[8];
    volatile uint32_t *insn_ptr =
        (volatile uint32_t *)((uintptr_t)insn_buf | 0x40000000);
    int res;

    insn_ptr[0] = 0x27BDFFF8;  // addiu $sp, $sp, -8
    insn_ptr[1] = 0xAFBF0004;  // sw $ra, 4($sp)
    insn_ptr[2] = 0x0080F809;  // jalr $a0
    insn_ptr[3] = 0x00000000;  // nop
    insn_ptr[4] = 0x8FBF0004;  // lw $ra, 4($sp)
    insn_ptr[5] = 0x34024321;  // li $v0, 0x4321
    insn_ptr[6] = 0x03E00008;  // jr $ra
    insn_ptr[7] = 0x27BD0008;  // addiu $sp, $sp, 8

    /* Use mefunc_return_123() as a do-nothing function to take the place of
     * meUtilityIcacheInvalidateAll(). */
    if ((res = meCall((void *)insn_buf, mefunc_return_123)) != 0) {
        fprintf(stderr, "meCall() #1 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #1 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x4321) {
        fprintf(stderr, "meResult() #1 gave wrong result (expected 0x4321,"
                " got 0x%X)\n", res);
        return 0;
    }

    /* Change the load instruction from the main CPU, but don't flush the
     * ME's cache. */

    insn_ptr[5] = 0x34025432;  // li $v0, 0x5432 (not seen by ME)

    if ((res = meCall((void *)insn_buf, mefunc_return_123)) != 0) {
        fprintf(stderr, "meCall() #2 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #2 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x4321) {
        fprintf(stderr, "meResult() #2 gave wrong result (expected 0x4321,"
                " got 0x%X)\n", res);
        return 0;
    }

    /* Change the load instruction and flush the ME's cache. */

    insn_ptr[5] = 0x34026543;  // li $v0, 0x6543

    if ((res = meCall((void *)insn_buf, meUtilityIcacheInvalidateAll)) != 0) {
        fprintf(stderr, "meCall() #3 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #3 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x6543) {
        fprintf(stderr, "meResult() #3 gave wrong result (expected 0x6543,"
                " got 0x%X)\n", res);
        return 0;
    }

    /* Test once more to make sure the new instruction is properly cached. */

    insn_ptr[5] = 0x34027654;  // li $v0, 0x7654 (not seen by ME)

    if ((res = meCall((void *)insn_buf, mefunc_return_123)) != 0) {
        fprintf(stderr, "meCall() #4 failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() #4 failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0x6543) {
        fprintf(stderr, "meResult() #4 gave wrong result (expected 0x6543,"
                " got 0x%X)\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_dcache_read:  Test that the ME's data cache operates as expected
 * when reading from the cache.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_dcache_read(void)
{
    static __attribute__((aligned(64))) int buffer[ME_DCACHE_SIZE/4];
    volatile int *bufptr = (volatile int *)((uintptr_t)buffer | 0x40000000);
    int res, i;

    for (i = 0; i < lenof(buffer); i++) {
        bufptr[i] = 0;
    }

    if ((res = meCall(mefunc_dcache_read, buffer)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    /* Wait long enough for the ME to finish reading from the buffer before
     * we update it. */
    delay(ME_DCACHE_SIZE*4);  // Experimentally determined to be sufficient
    for (i = 0; i < lenof(buffer); i++) {
        bufptr[i] = 0xFFFFFFFF;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }
    if ((res = meResult()) != 0) {
        fprintf(stderr, "meResult() gave wrong result (expected 0, got %d)\n",
                res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_dcache_write:  Test that the ME's data cache operates as expected
 * when writing to the cache.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_dcache_write(void)
{
    static __attribute__((aligned(64))) int buffer[ME_DCACHE_SIZE/4];
    volatile int *bufptr = (volatile int *)((uintptr_t)buffer | 0x40000000);
    int res, i;

    for (i = 0; i < lenof(buffer); i++) {
        bufptr[i] = -1;
    }

    if ((res = meCall(mefunc_dcache_write, buffer)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }

    /* Wait long enough for the ME to finish writing to the buffer before
     * we read from it.  We don't use meWait() because allowing the
     * function to return will flush some lines from the cache when the
     * library updates its internal state. */
    delay(ME_DCACHE_SIZE*4);

    res = -1;
    for (i = 0; i < lenof(buffer); i++) {
        res &= bufptr[i];
    }
    if (res != -1) {
        fprintf(stderr, "Bad buffer contents (expected -1, got %d)\n", res);
        meWait();
        return 0;
    }

    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_dcache_inval:  Test that meUtilityDcacheInvalidateAll() can be used
 * to invalidate the ME's data cache.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_dcache_inval(void)
{
    static __attribute__((aligned(64))) int buffer[ME_DCACHE_SIZE/4];
    volatile int *bufptr = (volatile int *)((uintptr_t)buffer | 0x40000000);
    int res, i;

    for (i = 0; i < lenof(buffer); i++) {
        bufptr[i] = 0xFFFFFFFF;
    }

    if ((res = meCall(mefunc_dcache_inval, buffer)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    res = -1;
    for (i = 0; i < lenof(buffer); i++) {
        res &= bufptr[i];
    }
    if (res != -1) {
        fprintf(stderr, "Bad buffer contents (expected -1, got %d)\n", res);
        return 0;
    }

    return 1;
}

/*-----------------------------------------------------------------------*/

/**
 * test_dcache_wbinv:  Test that meUtilityDcacheWritebackInvalidateAll()
 * can be used to flush (write back and invalidate) the ME's data cache.
 *
 * Assumes that test_restart() has passed.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if the test passes, zero if it fails
 */
static int test_dcache_wbinv(void)
{
    static __attribute__((aligned(64))) int buffer[ME_DCACHE_SIZE/4];
    volatile int *bufptr = (volatile int *)((uintptr_t)buffer | 0x40000000);
    int res, i;

    for (i = 0; i < lenof(buffer); i++) {
        bufptr[i] = 0xFFFFFFFF;
    }

    if ((res = meCall(mefunc_dcache_wbinv, buffer)) != 0) {
        fprintf(stderr, "meCall() failed: %08X\n", res);
        return 0;
    }
    if ((res = meWait()) != 0) {
        fprintf(stderr, "meWait() failed: %08X\n", res);
        return 0;
    }

    res = 0;
    for (i = 0; i < lenof(buffer); i++) {
        res |= bufptr[i];
    }
    if (res != 0) {
        fprintf(stderr, "Bad buffer contents (expected 0, got %d)\n", res);
        return 0;
    }


    return 1;
}

/*************************************************************************/
/********************** Routines executed on the ME **********************/
/*************************************************************************/

/**
 * mefunc_return_123:  Return 123 to the caller.
 *
 * [Parameters]
 *     param: Unused
 * [Return value]
 *     123
 */
static int mefunc_return_123(void *param)
{
    return 123;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_store_456:  Store 456 in the location pointed to by the parameter.
 *
 * [Parameters]
 *     param: Pointer to an int variable to receive 456.
 * [Return value]
 *     0
 */
static int mefunc_store_456(void *param)
{
    *(int *)param = 456;
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_delay_and_store_789:  Delay for 1M cycles, then Store 789 in the
 * location pointed to by the parameter.
 *
 * [Parameters]
 *     param: Pointer to an int variable to receive 789
 * [Return value]
 *     0
 */
static int mefunc_delay_and_store_789(void *param)
{
    delay(1000000);
    *(int *)param = 789;
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_count_forever:  Loop indefinitely, continuously incrementing the
 * location pointed to by the parameter.
 *
 * [Parameters]
 *     param: Pointer to an int variable to be continuously incremented
 * [Return value]
 *     Does not return
 */
static int mefunc_count_forever(void *param)
{
    volatile int *ptr = (volatile int *)param;
    for (;;) {
        (*ptr)++;
    }
    return 0;  // Not reached, but avoid a compiler warning
}

/*************************************************************************/

/**
 * mefunc_address_error:  Trigger an address error by accessing an
 * unaligned value.
 *
 * [Parameters]
 *     param: Unused
 * [Return value]
 *     Does not return
 */
static int mefunc_address_error(void *param)
{
    int dummy = 321;
    return *(int *)((uintptr_t)&dummy | 1);
}

/*************************************************************************/

/**
 * mefunc_return_IsME():  Return the value returned by meUtilityIsME().
 *
 * [Parameters]
 *     param: Unused
 * [Return value]
 *     Nonzero
 */
static int mefunc_return_IsME(void *param)
{
    return meUtilityIsME();
}

/*************************************************************************/

/**
 * mefunc_send_interrupt:  Wait 1M cycles, then store 654 in the location
 * pointed to by the parameter and send an interrupt to the main CPU.
 *
 * [Parameters]
 *     param: Pointer to an int variable to receive 654
 * [Return value]
 *     0
 */
static int mefunc_send_interrupt(void *param)
{
    delay(1000000);
    *(int *)param = 654;
    meUtilitySendInterrupt();
    return 0;
}

/*************************************************************************/

/**
 * mefunc_dcache_read:  Read ME_DCACHE_SIZE/4 words of memory starting at
 * the location pointed to by the parameter, wait 1M cycles, then read the
 * same words and return their combined bitwise OR.
 *
 * [Parameters]
 *     param: Pointer to a buffer of ME_DCACHE_SIZE/4 32-bit words
 * [Return value]
 *     Bitwise OR of all words in buffer
 */
static int mefunc_dcache_read(void *param)
{
    volatile uint32_t *ptr = (volatile uint32_t *)param;
    uint32_t res;
    int i;

    for (i = 0; i < ME_DCACHE_SIZE/4; i++) {
        (void) ptr[i];
    }

    delay(1000000);

    res = 0;
    for (i = 0; i < ME_DCACHE_SIZE/4; i++) {
        res |= ptr[i];
    }
    return res;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_dcache_write:  Write all zeroes to ME_DCACHE_SIZE/4 words of
 * memory starting at the location pointed to by the parameter, then wait
 * 1M cycles before returning.
 *
 * [Parameters]
 *     param: Pointer to a buffer of ME_DCACHE_SIZE/4 32-bit words
 * [Return value]
 *     0
 */
static int mefunc_dcache_write(void *param)
{
    volatile uint32_t *ptr = (volatile uint32_t *)param;
    int i;

    for (i = 0; i < ME_DCACHE_SIZE/4; i++) {
        ptr[i] = 0;
    }

    delay(1000000);
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_dcache_inval:  Write all zeroes to ME_DCACHE_SIZE/4 words of
 * memory starting at the location pointed to by the parameter, then
 * invalidate the data cache (thus nullifying the writes) and return.
 *
 * [Parameters]
 *     param: Pointer to a buffer of ME_DCACHE_SIZE/4 32-bit words
 * [Return value]
 *     0
 */
static int mefunc_dcache_inval(void *param)
{
    volatile uint32_t *ptr = (volatile uint32_t *)param;
    int i;

    /* Normally, we would have to flush the data cache once to ensure that
     * values pushed to the stack (like $ra) were properly stored to memory
     * before the invalidate call.  In this case, however, we completely
     * replace the cache set, so any cached stack values will be implicitly
     * flushed by the time we're done. */

    for (i = 0; i < ME_DCACHE_SIZE/4; i++) {
        ptr[i] = 0;
    }

    meUtilityDcacheInvalidateAll();
    return 0;
}

/*-----------------------------------------------------------------------*/

/**
 * mefunc_dcache_wbinv:  Write all zeroes to ME_DCACHE_SIZE/4 words of
 * memory starting at the location pointed to by the parameter, then flush
 * the data cache and return.
 *
 * [Parameters]
 *     param: Pointer to a buffer of ME_DCACHE_SIZE/4 32-bit words
 * [Return value]
 *     0
 */
static int mefunc_dcache_wbinv(void *param)
{
    volatile uint32_t *ptr = (volatile uint32_t *)param;
    int i;

    for (i = 0; i < ME_DCACHE_SIZE/4; i++) {
        ptr[i] = 0;
    }

    meUtilityDcacheWritebackInvalidateAll();
    return 0;
}

/*************************************************************************/
/************************ Other utility routines *************************/
/*************************************************************************/

/**
 * delay:  Delay the calling function for approximately the given number of
 * CPU clock cycles.  The actual length of the delay may differ from the
 * requested length by up to four cycles.
 *
 * [Parameters]
 *     cycles: Cycles to delay
 * [Return value]
 *     None
 */
static __attribute__((always_inline)) void delay(const unsigned int cycles)
{
    unsigned int iterations = cycles/4;
    asm volatile("   .set push; .set noreorder\n"
                 "1: bnez %[iterations], 1b\n"
                 "   addiu %[iterations], %[iterations], -1\n"
                 "   .set pop"
                 : [iterations] "=r" (iterations)
                 : "0" (iterations)
    );
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
