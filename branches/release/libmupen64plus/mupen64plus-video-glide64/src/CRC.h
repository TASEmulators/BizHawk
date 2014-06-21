/*
*   Glide64 - Glide video plugin for Nintendo 64 emulators.
*   Copyright (c) 2002  Dave2001
*   Copyright (c) 2008  Günther <guenther.emu@freenet.de>
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public
*   Licence along with this program; if not, write to the Free
*   Software Foundation, Inc., 51 Franklin Street, Fifth Floor, 
*   Boston, MA  02110-1301, USA
*/

//****************************************************************
//
// Glide64 - Glide Plugin for Nintendo 64 emulators (tested mostly with Project64)
// Project started on December 29th, 2001
//
// To modify Glide64:
// * Write your name and (optional)email, commented by your work, so I know who did it, and so that you can find which parts you modified when it comes time to send it to me.
// * Do NOT send me the whole project or file that you modified.  Take out your modified code sections, and tell me where to put them.  If people sent the whole thing, I would have many different versions, but no idea how to combine them all.
//
// Official Glide64 development channel: #Glide64 on EFnet
//
// Original author: Dave2001 (Dave2999@hotmail.com)
// Other authors: Gonetz, Gugaman
//
//****************************************************************
//
// CRC32 calculation functions 
//
// Created by Gonetz, 2004
//
//****************************************************************

#if !defined(WIN32) && defined(GCC)
#define Crc32 _Crc32
#define CRCTable _CRCTable
#endif

extern unsigned int CRCTable[ 256 ];

void CRC_BuildTable();

inline unsigned int CRC_Calculate( unsigned int crc, const void *buffer, unsigned int count )
{
#if !defined(__GNUC__) && !defined(NO_ASM)
  unsigned int Crc32=crc;
  __asm {
            mov esi, buffer
            mov edx, count
            add edx, esi
            mov ecx, crc

loop1:
            mov bl, byte ptr [esi]
            movzx   eax, cl
            inc esi
            xor al, bl
            shr ecx, 8
            mov ebx, [CRCTable+eax*4]
            xor ecx, ebx

            cmp edx, esi
            jne loop1

            xor Crc32, ecx
   }
   return Crc32;
#else
    unsigned int result = crc;
    for (const char * p = (const char*)buffer; p != (const char*)buffer + count; ++p)
    {
    unsigned char al = result;
    al ^= *p;
    result >>= 8;
    result ^= CRCTable[al];
    }
    result ^= crc;
    return result;
#endif
}

