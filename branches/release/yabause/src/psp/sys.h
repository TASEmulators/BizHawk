/*  src/psp/sys.h: PSP system-related functions header
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

#ifndef PSP_SYS_H
#define PSP_SYS_H

/**************************************************************************/

/**
 * psp_strerror:  Convert a PSP error code into a string, like strerror().
 *
 * [Parameters]
 *     code: Error code
 * [Return value]
 *     Corresponding error string
 * [Notes]
 *     The returned string is stored in a static buffer, so it will be
 *     destroyed on the next call to psp_strerror().
 */
extern const char *psp_strerror(const int32_t code);

/*----------------------------------*/

/**
 * sys_load_module:  Load (and start) a PSP module.
 *
 * [Parameters]
 *        module: Module pathname
 *     partition: Memory partition (PSP_MEMORY_PARTITON_*)
 * [Return value]
 *     Module ID (nonnegative) on success, error code (negative) on failure
 */
extern SceUID sys_load_module(const char *module, int partition);

/*----------------------------------*/

/**
 * sys_start_thread:  Start a new thread, returning the created thread
 * handle.
 *
 * [Parameters]
 *          name: Thread name
 *         entry: Thread entry address (function pointer)
 *      priority: Thread priority
 *     stacksize: Thread stack size
 *          args: Size of data to pass as thread argument
 *          argp: Pointer to thread argument data
 * [Return value]
 *     Nonnegative = thread handle, negative = error code
 */
extern int32_t sys_start_thread(const char *name, void *entry, int priority,
                                int stacksize, int args, void *argp);

/**
 * sys_delete_thread_if_stopped:  Check whether the given thread has
 * exited, and if so, delete the thread and return its exit status.
 *
 * [Parameters]
 *         thread: Thread handle
 *     status_ret: Pointer to variable to receive exit status (may be NULL)
 * [Return value]
 *     Nonzero if the thread was deleted, else zero
 */
extern int sys_delete_thread_if_stopped(SceUID thread, int *status_ret);

/*----------------------------------*/

/**
 * sys_setup_callbacks:  Set up the system callbacks (HOME button and
 * power status change).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero on success, zero on error
 */
extern int sys_setup_callbacks(void);

/**************************************************************************/

#endif  // PSP_SYS_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
