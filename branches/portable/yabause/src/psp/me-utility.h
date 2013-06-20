/*  src/psp/me-utility.h: PSP Media Engine utility routine header
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

#ifndef ME_UTILITY_H
#define ME_UTILITY_H

/*************************************************************************/

/**
 * meUtilityIsME:  Return whether the current CPU is the ME (nonzero) or
 * the SC (zero).
 *
 * [Parameters]
 *     None
 * [Return value]
 *     Nonzero if executing on the ME, zero if executing on the SC
 */
static inline int meUtilityIsME(void)
{
    int test;
    asm("xor %0, $k0, %1" : "=r" (test) : "r" (ME_K0_MAGIC));
    return test == 0;
}

/*----------------------------------*/

/**
 * meUtilityIcacheInvalidateAll:  Invalidate all entries in the Media
 * Engine's instruction cache.
 *
 * This routine may only be called from code executing on the Media Engine.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void meUtilityIcacheInvalidateAll(void);

/**
 * meUtilityDcacheInvalidateAll:  Invalidate all entries in the Media
 * Engine's data cache.
 *
 * This routine may only be called from code executing on the Media Engine.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void meUtilityDcacheInvalidateAll(void);

/**
 * meUtilityDcacheWritebackInvalidateAll:  Write back and then invalidate
 * all entries in the Media Engine's data cache.
 *
 * This routine may only be called from code executing on the Media Engine.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void meUtilityDcacheWritebackInvalidateAll(void);

/**
 * meUtilitySendInterrupt:  Send an interrupt to the main CPU.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
extern void meUtilitySendInterrupt(void);

/*************************************************************************/

#endif  // ME_UTILITY_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
