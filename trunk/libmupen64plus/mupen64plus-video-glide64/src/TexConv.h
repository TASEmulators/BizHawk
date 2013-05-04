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

void TexConv_ARGB1555_ARGB4444 (unsigned char * _src, unsigned char * _dst, int width, int height)
{
    int _size = (width * height) << 1;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [_src]
        mov edi,dword ptr [_dst]
        mov ecx,dword ptr [_size]

tc1_loop:
        mov eax,dword ptr [esi]
        add esi,4

        // arrr rrgg gggb bbbb
        // aaaa rrrr gggg bbbb
        mov edx,eax
        and eax,0x80008000
        mov ebx,eax             // ebx = 0xa000000000000000
        shr eax,1
        or ebx,eax              // ebx = 0xaa00000000000000
        shr eax,1
        or ebx,eax              // ebx = 0xaaa0000000000000
        shr eax,1
        or ebx,eax              // ebx = 0xaaaa000000000000

        mov eax,edx
        and eax,0x78007800      // eax = 0x0rrrr00000000000
        shr eax,3               // eax = 0x0000rrrr00000000
        or ebx,eax              // ebx = 0xaaaarrrr00000000

        mov eax,edx
        and eax,0x03c003c0      // eax = 0x000000gggg000000
        shr eax,2               // eax = 0x00000000gggg0000
        or ebx,eax              // ebx = 0xaaaarrrrgggg0000

        and edx,0x001e001e      // edx = 0x00000000000bbbb0
        shr edx,1               // edx = 0x000000000000bbbb
        or ebx,edx              // ebx = 0xaaaarrrrggggbbbb

        mov dword ptr [edi],ebx
        add edi,4

        dec ecx
        jnz tc1_loop
    }
#elif !defined(NO_ASM)
   //printf("TexConv_ARGB1555_ARGB4444\n");
   asm volatile (
         //"tc1_loop2:             \n"
         "0: \n"
         "mov (%[_src]), %%eax     \n"
         "add $4, %[_src]          \n"

        // arrr rrgg gggb bbbb
        // aaaa rrrr gggg bbbb
         "mov %%eax, %%edx       \n"
         "and $0x80008000, %%eax \n"
         "mov %%eax, %%ecx       \n"                // ecx = 0xa000000000000000
         "shr $1, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = 0xaa00000000000000
         "shr $1, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = 0xaaa0000000000000
         "shr $1, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = 0xaaaa000000000000

         "mov %%edx, %%eax       \n"
         "and $0x78007800, %%eax \n"        // eax = 0x0rrrr00000000000
         "shr $3, %%eax          \n"                // eax = 0x0000rrrr00000000
         "or %%eax, %%ecx        \n"                // ecx = 0xaaaarrrr00000000
         
         "mov %%edx, %%eax       \n"
         "and $0x03c003c0, %%eax \n"        // eax = 0x000000gggg000000
         "shr $2, %%eax          \n"                // eax = 0x00000000gggg0000
         "or %%eax, %%ecx        \n"                // ecx = 0xaaaarrrrgggg0000
         
         "and $0x001e001e, %%edx \n"        // edx = 0x00000000000bbbb0
         "shr $1, %%edx          \n"                // edx = 0x000000000000bbbb
         "or %%edx, %%ecx        \n"                // ecx = 0xaaaarrrrggggbbbb
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"
         
         "decl %[_size]            \n"
         "jnz 0b \n"
         : [_src]"+S"(_src), [_dst]"+D"(_dst), [_size]"+g"(_size)
         :
         : "memory", "cc", "eax", "edx", "ecx"
         );
#endif
}

void TexConv_AI88_ARGB4444 (unsigned char * _src, unsigned char * _dst, int width, int height)
{
    int _size = (width * height) << 1;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [_src]
        mov edi,dword ptr [_dst]
        mov ecx,dword ptr [_size]

tc1_loop:
        mov eax,dword ptr [esi]
        add esi,4

        // aaaa aaaa iiii iiii
        // aaaa rrrr gggg bbbb
        mov edx,eax
        and eax,0xF000F000      // eax = 0xaaaa000000000000
        mov ebx,eax             // ebx = 0xaaaa000000000000

        and edx,0x00F000F0      // edx = 0x00000000iiii0000
        shl edx,4               // edx = 0x0000iiii00000000
        or ebx,edx              // ebx = 0xaaaaiiii00000000
        shr edx,4               // edx = 0x00000000iiii0000
        or ebx,edx              // ebx = 0xaaaaiiiiiiii0000
        shr edx,4               // edx = 0x000000000000iiii
        or ebx,edx              // ebx = 0xaaaaiiiiiiiiiiii

        mov dword ptr [edi],ebx
        add edi,4

        dec ecx
        jnz tc1_loop
    }
#elif !defined(NO_ASM)
   //printf("TexConv_AI88_ARGB4444\n");
   asm volatile (
         //"tc1_loop3:              \n"
         "0: \n"
         "mov (%[_src]), %%eax     \n"
         "add $4, %[_src]          \n"
         
         // aaaa aaaa iiii iiii
         // aaaa rrrr gggg bbbb
         "mov %%eax, %%edx       \n"
         "and $0xF000F000, %%eax \n"        // eax = 0xaaaa000000000000
         "mov %%eax, %%ecx       \n"                // ecx = 0xaaaa000000000000
         
         "and $0x00F000F0, %%edx \n"        // edx = 0x00000000iiii0000
         "shl $4, %%edx          \n"                // edx = 0x0000iiii00000000
         "or %%edx, %%ecx        \n"                // ecx = 0xaaaaiiii00000000
         "shr $4, %%edx          \n"                // edx = 0x00000000iiii0000
         "or %%edx, %%ecx        \n"                // ecx = 0xaaaaiiiiiiii0000
         "shr $4, %%edx          \n"                // edx = 0x000000000000iiii
         "or %%edx, %%ecx        \n"                // ecx = 0xaaaaiiiiiiiiiiii
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"
         
         "decl %[_size]            \n"
         "jnz 0b \n"
         : [_src]"+S"(_src), [_dst]"+D"(_dst), [_size]"+g"(_size)
         :
         : "memory", "cc", "eax", "edx", "ecx"
         );
#endif
}

void TexConv_AI44_ARGB4444 (unsigned char * _src, unsigned char * _dst, int width, int height)
{
    int _size = width * height;
#if !defined(__GNUC__) &&  !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [_src]
        mov edi,dword ptr [_dst]
        mov ecx,dword ptr [_size]

tc1_loop:
        mov eax,dword ptr [esi]
        add esi,4

        // aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
        // aaaa1 rrrr1 gggg1 bbbb1 aaaa0 rrrr0 gggg0 bbbb0
        // aaaa3 rrrr3 gggg3 bbbb3 aaaa2 rrrr2 gggg2 bbbb2
        mov edx,eax             // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
        shl eax,16              // eax = aaaa1 iiii1 aaaa0 iiii0 0000  0000  0000  0000
        and eax,0xFF000000      // eax = aaaa1 iiii1 0000  0000  0000  0000  0000  0000
        mov ebx,eax             // ebx = aaaa1 iiii1 0000  0000  0000  0000  0000  0000
        and eax,0x0F000000      // eax = 0000  iiii1 0000  0000  0000  0000  0000  0000
        shr eax,4               // eax = 0000  0000  iiii1 0000  0000  0000  0000  0000
        or ebx,eax              // ebx = aaaa1 iiii1 iiii1 0000  0000  0000  0000  0000
        shr eax,4               // eax = 0000  0000  0000  iiii1 0000  0000  0000  0000
        or ebx,eax              // ebx = aaaa1 iiii1 iiii1 iiii1 0000  0000  0000  0000

        mov eax,edx             // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
        shl eax,8               // eax = aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0 0000  0000
        and eax,0x0000FF00      // eax = 0000  0000  0000  0000  aaaa0 iiii0 0000  0000
        or ebx,eax              // ebx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 0000  0000
        and eax,0x00000F00      // eax = 0000  0000  0000  0000  0000  iiii0 0000  0000
        shr eax,4               // eax = 0000  0000  0000  0000  0000  0000  iiii0 0000
        or ebx,eax              // ebx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 iiii0 0000
        shr eax,4               // eax = 0000  0000  0000  0000  0000  0000  0000  iiii0
        or ebx,eax              // ebx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 iiii0 iiii0

        mov dword ptr [edi],ebx
        add edi,4

        mov eax,edx             // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
        and eax,0xFF000000      // eax = aaaa3 iiii3 0000  0000  0000  0000  0000  0000
        mov ebx,eax             // ebx = aaaa3 iiii3 0000  0000  0000  0000  0000  0000
        and eax,0x0F000000      // eax = 0000  iiii3 0000  0000  0000  0000  0000  0000
        shr eax,4               // eax = 0000  0000  iiii3 0000  0000  0000  0000  0000
        or ebx,eax              // ebx = aaaa3 iiii3 iiii3 0000  0000  0000  0000  0000
        shr eax,4               // eax = 0000  0000  0000  iiii3 0000  0000  0000  0000
        or ebx,eax              // ebx = aaaa3 iiii3 iiii3 iiii3 0000  0000  0000  0000

                                // edx = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
        shr edx,8               // edx = 0000  0000  aaaa3 aaaa3 aaaa2 iiii2 aaaa1 iiii1
        and edx,0x0000FF00      // edx = 0000  0000  0000  0000  aaaa2 iiii2 0000  0000
        or ebx,edx              // ebx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 0000  0000
        and edx,0x00000F00      // edx = 0000  0000  0000  0000  0000  iiii2 0000  0000
        shr edx,4               // edx = 0000  0000  0000  0000  0000  0000  iiii2 0000
        or ebx,edx              // ebx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 iiii2 0000
        shr edx,4               // edx = 0000  0000  0000  0000  0000  0000  0000  iiii2
        or ebx,edx              // ebx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 iiii2 iiii2

        mov dword ptr [edi],ebx
        add edi,4

        dec ecx
        jnz tc1_loop
    }
#elif !defined(NO_ASM)
   //printf("TexConv_AI44_ARGB4444\n");
   asm volatile (
         //"tc1_loop4:             \n"
         "0: \n"
         "mov (%[_src]), %%eax     \n"
         "add $4, %[_src]          \n"
         
         // aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
         // aaaa1 rrrr1 gggg1 bbbb1 aaaa0 rrrr0 gggg0 bbbb0
         // aaaa3 rrrr3 gggg3 bbbb3 aaaa2 rrrr2 gggg2 bbbb2
         "mov %%eax, %%edx       \n"                // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
         "shl $16, %%eax         \n"                // eax = aaaa1 iiii1 aaaa0 iiii0 0000  0000  0000  0000
         "and $0xFF000000, %%eax \n"        // eax = aaaa1 iiii1 0000  0000  0000  0000  0000  0000
         "mov %%eax, %%ecx       \n"                // ecx = aaaa1 iiii1 0000  0000  0000  0000  0000  0000
         "and $0x0F000000, %%eax \n"        // eax = 0000  iiii1 0000  0000  0000  0000  0000  0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  iiii1 0000  0000  0000  0000  0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa1 iiii1 iiii1 0000  0000  0000  0000  0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  0000  iiii1 0000  0000  0000  0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa1 iiii1 iiii1 iiii1 0000  0000  0000  0000
         
         "mov %%edx, %%eax       \n"                // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
         "shl $8, %%eax          \n"                // eax = aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0 0000  0000
         "and $0x0000FF00, %%eax \n"        // eax = 0000  0000  0000  0000  aaaa0 iiii0 0000  0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 0000  0000
         "and $0x00000F00, %%eax \n"        // eax = 0000  0000  0000  0000  0000  iiii0 0000  0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  0000  0000  0000  0000  iiii0 0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 iiii0 0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  0000  0000  0000  0000  0000  iiii0
         "or %%eax, %%ecx        \n"                // ecx = aaaa1 iiii1 iiii1 iiii1 aaaa0 iiii0 iiii0 iiii0
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"
         
         "mov %%edx, %%eax       \n"                // eax = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
         "and $0xFF000000, %%eax \n"        // eax = aaaa3 iiii3 0000  0000  0000  0000  0000  0000
         "mov %%eax, %%ecx       \n"                // ecx = aaaa3 iiii3 0000  0000  0000  0000  0000  0000
         "and $0x0F000000, %%eax \n"        // eax = 0000  iiii3 0000  0000  0000  0000  0000  0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  iiii3 0000  0000  0000  0000  0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa3 iiii3 iiii3 0000  0000  0000  0000  0000
         "shr $4, %%eax          \n"                // eax = 0000  0000  0000  iiii3 0000  0000  0000  0000
         "or %%eax, %%ecx        \n"                // ecx = aaaa3 iiii3 iiii3 iiii3 0000  0000  0000  0000
         
                                                // edx = aaaa3 iiii3 aaaa2 iiii2 aaaa1 iiii1 aaaa0 iiii0
         "shr $8, %%edx          \n"                // edx = 0000  0000  aaaa3 aaaa3 aaaa2 iiii2 aaaa1 iiii1
         "and $0x0000FF00, %%edx \n"        // edx = 0000  0000  0000  0000  aaaa2 iiii2 0000  0000
         "or %%edx, %%ecx        \n"                // ecx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 0000  0000
         "and $0x00000F00, %%edx \n"        // edx = 0000  0000  0000  0000  0000  iiii2 0000  0000
         "shr $4, %%edx          \n"                // edx = 0000  0000  0000  0000  0000  0000  iiii2 0000
         "or %%edx, %%ecx        \n"                // ecx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 iiii2 0000
         "shr $4, %%edx          \n"                // edx = 0000  0000  0000  0000  0000  0000  0000  iiii2
         "or %%edx, %%ecx        \n"                // ecx = aaaa3 iiii3 iiii3 iiii3 aaaa2 iiii2 iiii2 iiii2
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"
         
         "decl %[_size]            \n"
         "jnz 0b \n"
         : [_src]"+S"(_src), [_dst]"+D"(_dst), [_size]"+g"(_size)
         :
         : "memory", "cc", "eax", "edx", "ecx"
         );
#endif
}

void TexConv_A8_ARGB4444 (unsigned char * _src, unsigned char * _dst, int width, int height)
{
    int _size = (width * height) << 1;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [_src]
        mov edi,dword ptr [_dst]
        mov ecx,dword ptr [_size]

tc1_loop:
        mov eax,dword ptr [esi]
        add esi,4

        // aaaa3 aaaa3 aaaa2 aaaa2 aaaa1 aaaa1 aaaa0 aaaa0
        // aaaa1 rrrr1 gggg1 bbbb1 aaaa0 rrrr0 gggg0 bbbb0
        // aaaa3 rrrr3 gggg3 bbbb3 aaaa2 rrrr2 gggg2 bbbb2
        mov edx,eax
        and eax,0x0000F000      // eax = 00 00 00 00 a1 00 00 00
        shl eax,16              // eax = a1 00 00 00 00 00 00 00
        mov ebx,eax             // ebx = a1 00 00 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a1 a1 00 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a1 a1 a1 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a1 a1 a1 a1 00 00 00 00

        mov eax,edx
        and eax,0x000000F0      // eax = 00 00 00 00 00 00 a0 00
        shl eax,8               // eax = 00 00 00 00 a0 00 00 00
        or ebx,eax
        shr eax,4
        or ebx,eax
        shr eax,4
        or ebx,eax
        shr eax,4
        or ebx,eax              // ebx = a1 a1 a1 a1 a0 a0 a0 a0

        mov dword ptr [edi],ebx
        add edi,4

        mov eax,edx             // eax = a3 a3 a2 a2 a1 a1 a0 a0
        and eax,0xF0000000      // eax = a3 00 00 00 00 00 00 00
        mov ebx,eax             // ebx = a3 00 00 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a3 a3 00 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a3 a3 a3 00 00 00 00 00
        shr eax,4
        or ebx,eax              // ebx = a3 a3 a3 a3 00 00 00 00

        and edx,0x00F00000      // eax = 00 00 a2 00 00 00 00 00
        shr edx,8               // eax = 00 00 00 00 a2 00 00 00
        or ebx,edx
        shr edx,4
        or ebx,edx
        shr edx,4
        or ebx,edx
        shr edx,4
        or ebx,edx              // ebx = a3 a3 a3 a3 a2 a2 a2 a2

        mov dword ptr [edi],ebx
        add edi,4

        dec ecx
        jnz tc1_loop
    }
#elif !defined(NO_ASM)
   //printf("TexConv_A8_ARGB4444\n");
   asm volatile (
         //"tc1_loop:              \n"
         "0: \n"
         "mov (%[src]), %%eax     \n"
         "add $4, %[src]          \n"
         
         // aaaa3 aaaa3 aaaa2 aaaa2 aaaa1 aaaa1 aaaa0 aaaa0
         // aaaa1 rrrr1 gggg1 bbbb1 aaaa0 rrrr0 gggg0 bbbb0
         // aaaa3 rrrr3 gggg3 bbbb3 aaaa2 rrrr2 gggg2 bbbb2
         "mov %%eax, %%edx       \n"
         "and $0x0000F000, %%eax \n"        // eax = 00 00 00 00 a1 00 00 00
         "shl $16, %%eax         \n"                // eax = a1 00 00 00 00 00 00 00
         "mov %%eax, %%ecx       \n"                // ecx = a1 00 00 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a1 a1 00 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a1 a1 a1 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a1 a1 a1 a1 00 00 00 00
         
         "mov %%edx, %%eax       \n"
         "and $0x000000F0, %%eax \n"        // eax = 00 00 00 00 00 00 a0 00
         "shl $8, %%eax          \n"                // eax = 00 00 00 00 a0 00 00 00
         "or %%eax, %%ecx        \n"
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a1 a1 a1 a1 a0 a0 a0 a0
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"

         "mov %%edx, %%eax       \n"                // eax = a3 a3 a2 a2 a1 a1 a0 a0
         "and $0xF0000000, %%eax \n"        // eax = a3 00 00 00 00 00 00 00
         "mov %%eax, %%ecx       \n"                // ecx = a3 00 00 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a3 a3 00 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a3 a3 a3 00 00 00 00 00
         "shr $4, %%eax          \n"
         "or %%eax, %%ecx        \n"                // ecx = a3 a3 a3 a3 00 00 00 00
         
         "and $0x00F00000, %%edx \n"        // eax = 00 00 a2 00 00 00 00 00
         "shr $8, %%edx          \n"                // eax = 00 00 00 00 a2 00 00 00
         "or %%edx, %%ecx        \n"
         "shr $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         "shr $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         "shr $4, %%edx          \n"
         "or %%edx, %%ecx        \n"                // ecx = a3 a3 a3 a3 a2 a2 a2 a2
         
         "mov %%ecx, (%[_dst])     \n"
         "add $4, %[_dst]          \n"
         
         "decl %[_size]            \n"
         "jnz 0b \n"
         : [src]"+S"(_src), [_dst]"+D"(_dst), [_size]"+g"(_size)
         :
         : "memory", "cc", "eax", "ecx", "edx"
         );
#endif
}

