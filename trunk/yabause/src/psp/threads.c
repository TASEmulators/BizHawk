/*  src/psp/threads.c: Yabause thread management routines
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

#include "common.h"
#include "config.h"
#include "me.h"
#include "me-utility.h"
#include "sys.h"

#include "../threads.h"

/*************************************************************************/

/* Thread handle and setup data for each Yabause subthread */
static struct {
    int32_t handle;
    const char * const name;
    int priority;
    uint32_t stack_size;
} thread_data[YAB_NUM_THREADS] = {
    [YAB_THREAD_SCSP] = {.name = "YabauseScspThread",
                         .priority = THREADPRI_MAIN,
                         .stack_size = 16384},  // FIXME: could be smaller?
};

/*************************************************************************/
/************************** Interface routines ***************************/
/*************************************************************************/

/**
 * YabThreadStart:  Start a new thread for the given function.  Only one
 * thread will be started for each thread ID (YAB_THREAD_*).
 *
 * [Parameters]
 *       id: Yabause subthread ID (YAB_THREAD_*)
 *     func: Function to execute
 * [Return value]
 *     Zero on success, negative on error
 */
int YabThreadStart(unsigned int id, void (*func)(void))
{
    if (thread_data[id].handle) {
        fprintf(stderr, "YabThreadStart: thread %u is already started!\n", id);
        return -1;
    }

    if (id == YAB_THREAD_SCSP) {  // We run the SCSP on the ME
        if (!me_available || !config_get_use_me()) {
            DMSG("ME not available or disabled by user");
            return -1;
        }
        sceKernelDcacheWritebackAll();
#ifdef PSP_DEBUG
        meExceptionSetFatal(1);
#endif
        int32_t res;
        if ((res = meStart()) < 0) {
            DMSG("meStart(): %s", psp_strerror(res));
            return -1;
        } else if ((res = meWait()) < 0) {
            DMSG("meWait(): %s", psp_strerror(res));
            return -1;
        } else if ((res = meCall((void *)func, NULL)) < 0) {
            DMSG("Failed to start thread %d (%s) on ME: %s",
                 id, thread_data[id].name, psp_strerror(res));
            return -1;
        }
        /* Attempting to suspend the PSP will currently cause a crash,
         * so prevent suspending entirely.  (This is documented in the
         * README.PSP file.) */
        scePowerLock(0);
        thread_data[id].handle = 1;  // Anything nonzero will do
    } else {
        int32_t res = sys_start_thread(thread_data[id].name, func,
                                       thread_data[id].priority,
                                       thread_data[id].stack_size, 0, NULL);
        if (res < 0) {
            DMSG("Failed to start thread %d (%s): %s",
                 id, thread_data[id].name, psp_strerror(res));
            return -1;
        }
        thread_data[id].handle = res;
    }

    return 0;
}

/*************************************************************************/

/**
 * YabThreadWait:  Wait for the given ID's thread to terminate.  Returns
 * immediately if no thread has been started on the given ID.
 *
 * [Parameters]
 *     id: Yabause subthread ID (YAB_THREAD_*)
 * [Return value]
 *     None
 */
void YabThreadWait(unsigned int id)
{
    if (!thread_data[id].handle) {
        return;  // Thread wasn't running in the first place
    }

    if (id == YAB_THREAD_SCSP) {
        sceKernelDcacheWritebackInvalidateAll();
        int32_t res;
        if ((res = meWait()) < 0) {
            DMSG("meWait(): %s", psp_strerror(res));
        }
    } else {
        int32_t res;
        if ((res = sceKernelWaitThreadEnd(thread_data[id].handle, NULL)) < 0) {
            DMSG("WaitThreadEnd(%d): %s", id, psp_strerror(res));
        }
        if ((res = sceKernelDeleteThread(thread_data[id].handle)) < 0) {
            DMSG("DeleteThread(%d): %s", id, psp_strerror(res));
        }
    }

    thread_data[id].handle = 0;
}

/*************************************************************************/

/**
 * YabThreadYield:  Yield CPU execution to another thread.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void YabThreadYield(void)
{
    if (meUtilityIsME()) {
        /* On the ME, there's no other thread to yield to, but we do flush
         * the cache to ensure the SC can see our updates. */
        meUtilityDcacheWritebackInvalidateAll();
    } else {
#ifdef PSP_DEBUG
        if (thread_data[YAB_THREAD_SCSP].handle) {
            /* ME is running, so check for ME exceptions */
            (void) mePoll();
        }
#endif
        sceKernelDelayThread(0);
    }
}

/*************************************************************************/

/**
 * YabThreadSleep:  Put the current thread to sleep.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void YabThreadSleep(void)
{
    if (meUtilityIsME()) {
        /* Used in place of YabThreadYield() when the SCSP thread has
         * nothing to do; there's no real point in sleeping on the ME, but
         * flush the data cache as in Yield(). */
        meUtilityDcacheWritebackInvalidateAll();
    } else {
        sceKernelSleepThread();
    }
}

/*************************************************************************/

/**
 * YabThreadWake:  Wake up the given thread if it is asleep.
 *
 * [Parameters]
 *     id: Yabause subthread ID (YAB_THREAD_*)
 * [Return value]
 *     None
 */
void YabThreadWake(unsigned int id)
{
    if (!thread_data[id].handle) {
        return;  // Thread is not running
    }

    if (id == YAB_THREAD_SCSP) {
#ifdef PSP_DEBUG
        /* Check for ME exceptions */
        (void) mePoll();
#endif
    } else {
        sceKernelWakeupThread(thread_data[id].handle);
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
