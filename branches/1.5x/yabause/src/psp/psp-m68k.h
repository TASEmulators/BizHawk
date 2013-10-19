/*  src/psp/psp-m68k.h: PSP M68k emulator interface module header
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

#ifndef PSP_M68K_H
#define PSP_M68K_H

#include "../m68kcore.h"  // for M68K_struct

/*************************************************************************/

/* Module interface definition */
extern M68K_struct M68KPSP;

/* Unique module ID (must be different from any in ../m68kcore.h) */
#define M68KCORE_PSP  0x5CE  // "SCE"

/*************************************************************************/

#endif  // PSP_M68K_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
