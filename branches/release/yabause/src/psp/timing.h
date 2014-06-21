/*  src/psp/timing.h: Header for emulation timing logic
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

#ifndef PSP_TIMING_H
#define PSP_TIMING_H

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
extern void timing_init(void);

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
extern void timing_sync(void);

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
extern void timing_skip_next_sync(void);

/*************************************************************************/

#endif  // PSP_TIMING_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
