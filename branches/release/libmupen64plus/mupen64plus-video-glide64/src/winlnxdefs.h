/**
 * Glide64 Video Plugin - winlnxdefs.h
 * Copyright (C) 2002 Dave2001
 *
 * Mupen64Plus homepage: http://code.google.com/p/mupen64plus/
 * 
 * This program is free software; you can redistribute it and/
 * or modify it under the terms of the GNU General Public Li-
 * cence as published by the Free Software Foundation; either
 * version 2 of the License, or any later version.
 *
 * This program is distributed in the hope that it will be use-
 * ful, but WITHOUT ANY WARRANTY; without even the implied war-
 * ranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 * See the GNU General Public Licence for more details.
 *
 * You should have received a copy of the GNU General Public
 * Licence along with this program; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin Street, Fifth Floor, 
 * Boston, MA  02110-1301, USA
 *
**/

#ifndef WINLNXDEFS_H
#define WINLNXDEFS_H
#ifndef WIN32
typedef int BOOL;
typedef unsigned char BYTE;
typedef unsigned short WORD;
typedef unsigned int DWORD;
typedef int INT;
typedef long long LONGLONG;

typedef int __int32;

typedef void* HINSTANCE;
typedef int PROPSHEETHEADER;
typedef int PROPSHEETPAGE;
typedef int HWND;

#define FALSE false
#define TRUE true
#define __stdcall
#define __declspec(dllexport)
#define _cdecl
#define WINAPI

typedef union _LARGE_INTEGER
{
   struct
     {
    DWORD LowPart;
    INT HighPart;
     } s;
   struct
     {
    DWORD LowPart;
    INT HighPart;
     } u;
   LONGLONG QuadPart;
} LARGE_INTEGER, *PLARGE_INTEGER;

#define HIWORD(a) ((unsigned int)(a) >> 16)
#define LOWORD(a) ((a) & 0xFFFF)
#endif
#endif // WINLNXDEFS_H

