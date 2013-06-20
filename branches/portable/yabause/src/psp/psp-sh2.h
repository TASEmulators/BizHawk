/*  src/psp/psp-sh2.h: Header for SH-2 emulator for PSP
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

#ifndef PSP_SH2_H
#define PSP_SH2_H

#include "../sh2core.h"  // for SH2Interface_struct

/*************************************************************************/

/* Module interface definition */
extern SH2Interface_struct SH2PSP;

/* Unique module ID (must be different from any in ../sh2{core,int}.h) */
#define SH2CORE_PSP  0x5CE  // "SCE"

/*************************************************************************/

#endif  // PSP_SH2_H

/*
 * Local variables:
 *   c-file-style: "stroustrup"
 *   c-file-offsets: ((case-label . *) (statement-case-intro . *))
 *   indent-tabs-mode: nil
 * End:
 *
 * vim: expandtab shiftwidth=4:
 */
