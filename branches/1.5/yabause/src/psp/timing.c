/*  src/psp/timing.c: Emulation timing logic for PSP
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

#include "timing.h"

/*************************************************************************/
/****************************** Local data *******************************/
/*************************************************************************/

/* Length of a single frame in microseconds (32.32 fixed point). */
#define FRAME_TIME  (((1001000LL << 32) + 30) / 60)

/* Minimum elapsed time since the beginning of the previous frame before
 * we consider this the start of a new frame.  This is slightly less than
 * the actual frame length to account for slight discrepancies in hardware
 * timing. */
#define MIN_WAIT  (FRAME_TIME * 9/10)

/* Microsecond timestamp at the beginning of the previous frame
 * (32.32 fixed point). */
static int64_t last_frame_time;

/* Flag indicating that the next frame should be synched to real time.
 * This is set on the call to timing_init() and also each call to
 * timing_sync(), and can be cleared during a frame by a call to
 * timing_skip_next_sync().  If the flag is clear on entry to timing_sync(),
 * the sync is skipped and the last frame time is incremented by FRAME_TIME
 * regardless of the current time.  This flag is typically cleared by
 * psp-video.c during skipped output frames to let several emulated frames
 * execute at once. */
static int sync_next_frame;

/*************************************************************************/
/************************** Interface functions **************************/
/*************************************************************************/

/**
 * timing_init:  Initialize the timing functionality at program start.
 * Should be called as late as possible in the initialization sequence
 * (as close to the start of emulation as possible).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void timing_init(void)
{
    sync_next_frame = 1;
    sceDisplayWaitVblankStart();
    last_frame_time = (int64_t)sceKernelGetSystemTimeLow() << 32;
}

/*-----------------------------------------------------------------------*/

/**
 * timing_sync:  Wait until at least one frame has passed since the
 * beginning of the previous frame.  However, if timing_skip_next_sync()
 * was called after the previous call to timing_sync() or timing_init(),
 * this function returns immediately, and the next timing_sync() call will
 * wait one extra frame past the last synchronization point.  (For example,
 * calling timing_skip_next_sync() every second frame will cause the
 * immediately subsequent timing_sync() to return immediately, and the
 * timing_sync() after that to wait until two frames' time has passed since
 * the call to timing_sync() preceding the timing_skip_next_sync() call.)
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void timing_sync(void)
{
    if (sync_next_frame) {
        int waited = 0;
        int64_t now;
        while (now = (int64_t)sceKernelGetSystemTimeLow() << 32,
               now - last_frame_time < MIN_WAIT)
        {
            sceDisplayWaitVblankStart();
            waited = 1;
        }
        /* Keep the timer aligned as closely as possible to the actual
         * vertical blank for smoother timing. */
        if (waited) {
            last_frame_time = now;
        } else {
            do {
                last_frame_time += FRAME_TIME;
            } while (now - last_frame_time > FRAME_TIME/2);
        }
    } else {
        last_frame_time += FRAME_TIME;
        sync_next_frame = 1;
    }
}

/*************************************************************************/

/**
 * timing_skip_next_sync:  Called during a frame to indicate that the
 * next call to timing_sync() should not perform a sync.  See the
 * timing_sync() documentation for details.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void timing_skip_next_sync(void)
{
    sync_next_frame = 0;
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
