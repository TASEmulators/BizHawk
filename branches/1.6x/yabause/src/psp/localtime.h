/*  src/psp/localtime.h: PSP localtime() header
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

#ifndef PSP_LOCALTIME_H
#define PSP_LOCALTIME_H

#include <stdint.h>
#include <time.h>  // For struct tm declaration

/*************************************************************************/

/**
 * localtime_init:  Perform initialization required for localtime().
 * Called by PSP initialization code at program startup.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void localtime_init(void);

/**
 * localtime_utc_offset:  Return the UTC offset of the current time zone in
 * seconds (for example, GMT-1 has a UTC offset of -3600 seconds).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     UTC offset in seconds
 */
extern int32_t localtime_utc_offset(void);

/**
 * internal_localtime_r:  Local, reentrant version of localtime() used by
 * ../smpc.c.  Breaks down a timestamp integer into Y/M/D h:m:s
 * representation.
 *
 * [Parameters]
 *      timep: Timestamp to break down
 *     result: Broken-down time
 * [Return value]
 *     result, or NULL on error
 */
extern struct tm *internal_localtime_r(const time_t *timep, struct tm *result);

/*************************************************************************/

#endif  // PSP_LOCALTIME_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
