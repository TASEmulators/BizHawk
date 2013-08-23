/*  Copyright 2004-2005 Theo Berkau
    Copyright 2005 Joost Peters

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

#ifndef CD_HH
#define CD_HH
#ifdef HAVE_NTDDCDRM
#include <ntddcdrm.h>
#include <ntddscsi.h>
#else
#include "fakeddk.h"
#endif
#ifdef HAVE_WNASPI32_H
#include <wnaspi32.h>
#endif
#include "../cdbase.h"

#define CDCORE_SPTI     2
#define CDCORE_ASPI     3

int SPTICDInit(const char *);
void SPTICDDeInit(void);
int SPTICDGetStatus(void);
s32 SPTICDReadTOC(u32 *);
int SPTICDReadSectorFAD(u32, void *);
void SPTICDReadAheadFAD(u32);

extern CDInterface ArchCD;

int ASPICDInit(const char *);
void ASPICDDeInit();
int ASPICDGetStatus();
s32 ASPICDReadTOC(u32 *);
int ASPICDReadSectorFAD(u32, void *);
void ASPICDReadAheadFAD(u32);

extern CDInterface ASPICD;

#endif
