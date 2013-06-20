/*  src/psp/me-utility.c: PSP Media Engine utility routines
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

#include <stdint.h>
#include "me.h"
#include "me-utility.h"

/*************************************************************************/
/*************************************************************************/

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
void meUtilityIcacheInvalidateAll(void)
{
    unsigned int cachesize_bits;
    asm volatile("mfc0 %0, $16; ext %0, %0, 9, 3" : "=r" (cachesize_bits));
    const unsigned int cachesize = 4096 << cachesize_bits;

    asm volatile("mtc0 $zero, $28");  // TagLo
    asm volatile("mtc0 $zero, $29");  // TagHi
    unsigned int i;
    for (i = 0; i < cachesize; i += 64) {
        asm volatile("cache 0x1, 0(%0)" : : "r" (i));
        asm volatile("cache 0x3, 0(%0)" : : "r" (i));
    }
}

/*-----------------------------------------------------------------------*/

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
void meUtilityDcacheInvalidateAll(void)
{
    unsigned int cachesize_bits;
    asm volatile("mfc0 %0, $16; ext %0, %0, 6, 3" : "=r" (cachesize_bits));
    const unsigned int cachesize = 4096 << cachesize_bits;

    asm volatile("mtc0 $zero, $28");  // TagLo
    asm volatile("mtc0 $zero, $29");  // TagHi
    unsigned int i;
    for (i = 0; i < cachesize; i += 64) {
        asm volatile("cache 0x11, 0(%0)" : : "r" (i));
        asm volatile("cache 0x13, 0(%0)" : : "r" (i));
    }
}

/*-----------------------------------------------------------------------*/

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
void meUtilityDcacheWritebackInvalidateAll(void)
{
    unsigned int cachesize_bits;
    asm volatile("mfc0 %0, $16; ext %0, %0, 6, 3" : "=r" (cachesize_bits));
    const unsigned int cachesize = 4096 << cachesize_bits;

    unsigned int i;
    for (i = 0; i < cachesize; i += 64) {
        asm volatile("cache 0x14, 0(%0)" : : "r" (i));
    }
    asm volatile("sync");
}

/*************************************************************************/

/**
 * meUtilitySendInterrupt:  Send an interrupt to the main CPU.
 *
 * [Parameters]
 *     None
 * [Return value]
 *     None
 */
void meUtilitySendInterrupt(void)
{
    asm volatile("sync");
    asm volatile("sw %0, 0x44(%1)" : : "r" (1), "r" (0xBC100000));
    asm volatile("sync");
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
