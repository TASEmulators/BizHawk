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
*   License along with this program; if not, write to the Free
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

//****************************************************************
// Size: 0, Format: 2

DWORD Load4bCI (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 4)) << 1;
    unsigned short * pal = (rdp.pal_8 + (rdp.tiles[tile].palette << 4));
    if (rdp.tlut_mode == 2)
    {
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov ebx,dword ptr [pal]

        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        push ecx

        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx

        dec ecx
        jnz x_loop

        pop ecx
        dec ecx
        jz end_y_loop
        push ecx

        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        mov ecx,dword ptr [wid_64]
x_loop_2:
        push ecx

        mov eax,dword ptr [esi+4]       // read all 8 pixels
        bswap eax
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,8
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,1
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,1

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx

        dec ecx
        jnz x_loop_2
        
        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        pop ecx
        dec ecx
        jnz y_loop

end_y_loop:
    }
#elif !defined(NO_ASM)
       //printf("Load4bCI1\n");
        // This way, gcc generates either a 32 bit or a 64 bit register
        long lTempX, lTempY, lHeight = (long) height;
        intptr_t fake_eax, fake_edx;
       asm volatile (
        "1:                 \n" // y_loop
             "mov %[c], %[tempy]            \n"
             
             "mov %[wid_64], %%ecx    \n"
        "2:                 \n" // x_loop
             "mov %[c], %[tempx]            \n"
             
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7,%%eax            \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]),%%cx  \n"
             "ror $1,%%cx             \n"
             "shl $16,%%ecx           \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // * copy
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *
             
             "mov %[tempx], %[c]       \n"
             
             "dec %%ecx               \n"
             "jnz 2b                  \n" // x_loop
             
             "mov %[tempy], %[c]       \n"
             "dec %%ecx               \n"
             "jz 4f                    \n" // end_y_loop
             "mov %[c], %[tempy]       \n"

             "add %[line], %[src]      \n"
             "add %[ext], %[dst]       \n"

             "mov %[wid_64], %%ecx    \n"
             "3:                      \n" // x_loop_2
             "mov %[c], %[tempx]       \n"
             
             "mov 4(%[src]), %%eax     \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // * copy
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $8, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *

             "mov %[tempx], %[c]       \n"
             
             "dec %%ecx               \n"
             "jnz 3b                  \n" // x_loop_2
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[tempy], %[c]       \n"
             "dec %%ecx               \n"
             "jnz 1b                  \n" // y_loop

             "4:                      \n" // end_y_loop
             : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [a] "=&a"(fake_eax), [d] "=&d"(fake_edx), [src] "+S"(src), [dst] "+D"(dst), [c] "+c"(lHeight)
             // pal needs to be in a register because its used in mov (%[pal],...), ...
             : [pal] "r" (pal), [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
             : "memory", "cc"
             );
#endif
    }
    else
    {
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov ebx,dword ptr [pal]

        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
ia_y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
ia_x_loop:
        push ecx

        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx

        dec ecx
        jnz ia_x_loop

        pop ecx
        dec ecx
        jz ia_end_y_loop
        push ecx

        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        mov ecx,dword ptr [wid_64]
ia_x_loop_2:
        push ecx

        mov eax,dword ptr [esi+4]       // read all 8 pixels
        bswap eax
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,8
        mov edx,eax

        // 1st dword output {
        shr eax,23
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,27
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword output {
        mov eax,edx
        shr eax,15
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,19
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 3rd dword output {
        mov eax,edx
        shr eax,7
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        mov eax,edx
        shr eax,11
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 4th dword output {
        mov eax,edx
        shl eax,1
        and eax,0x1E
        mov cx,word ptr [ebx+eax]
        ror cx,8
        shl ecx,16

        shr edx,3
        and edx,0x1E
        mov cx,word ptr [ebx+edx]
        ror cx,8

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx

        dec ecx
        jnz ia_x_loop_2
        
        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        pop ecx
        dec ecx
        jnz ia_y_loop

ia_end_y_loop:
    }
#elif !defined(NO_ASM)
       //printf("Load4bCI2\n");
       long lTempX, lTempY, lHeight = (long) height;
       intptr_t fake_eax, fake_edx;
       asm volatile (
             "1:                     \n"  // ia_y_loop
             "mov %[c], %[tempy]     \n"

             "mov %[wid_64], %%ecx   \n"
             "2:                     \n"  // ia_x_loop
             "mov %[c], %[tempx]     \n"
             
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // * copy
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8,%%cx             \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *

             "mov %[tempx], %[c]     \n"
             
             "dec %%ecx               \n"
             "jnz 2b                  \n"  // ia_x_loop
             
             "mov %[tempy], %[c]     \n"
             "dec %%ecx               \n"
             "jz 4f                  \n"  // ia_end_y_loop
             "mov %[c], %[tempy]     \n"
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"

             "mov %[wid_64], %%ecx   \n"
             "3:                     \n"  // ia_x_loop_2
             "mov %[c], %[tempx]     \n"
             
             "mov 4(%[src]), %%eax     \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // * copy
             "mov (%[src]), %%eax      \n"      // read all 8 pixels
             "bswap %%eax             \n"
             "add $8, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $23, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $27, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shr $15, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $19, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 3rd dword output {
             "mov %%edx, %%eax        \n"
             "shr $7, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $11, %%eax          \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }

             // 4th dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1E, %%eax        \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $3, %%edx           \n"
             "and $0x1E, %%edx        \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *

             "mov %[tempx], %[c]     \n"
             
             "dec %%ecx               \n"
             "jnz 3b                  \n"  // ia_x_loop_2
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[tempy], %[c]     \n"
             "dec %%ecx               \n"
             "jnz 1b                  \n"  // ia_y_loop
             
             "4:                      \n"  // ia_end_y_loop
             : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [a] "=&a"(fake_eax), [d] "=&d"(fake_edx), [src] "+S"(src), [dst] "+D"(dst), [c] "+c"(lHeight)
             // pal needs to be in a register because its used in mov (%[pal],...), ...
             : [pal] "r" (pal), [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
             : "memory", "cc"
             );
#endif
        return (1 << 16) | GR_TEXFMT_ALPHA_INTENSITY_88;
    }

    return (1 << 16) | GR_TEXFMT_ARGB_1555;
}

//****************************************************************
// Size: 0, Format: 3
//
// ** BY GUGAMAN **

DWORD Load4bIA (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (rdp.tlut_mode != 0)
        return Load4bCI (dst, src, wid_64, height, line, real_width, tile);

    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 4));
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        push ecx

        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword {  
        xor ecx,ecx

        // pixel #1
        //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,24 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,28 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #2
        //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        mov eax,edx
        shr eax,12 //Alpha 
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,16 // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #3
        //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,4 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #4
        //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,12 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,8 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax


        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword { 
        xor ecx,ecx 

        // pixel #5
        //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,8 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,12 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #6
        //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,4
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #7
        //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,16
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,12 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #8
        //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,28 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,24 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword {  
        xor ecx,ecx

        // pixel #1
        //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,24 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,28 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #2
        //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        mov eax,edx
        shr eax,12 //Alpha 
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,16 // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #3
        //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,4 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #4
        //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,12 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,8 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax


        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword { 
        xor ecx,ecx 

        // pixel #5
        //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,8 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,12 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #6
        //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,4
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #7
        //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,16
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,12 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #8
        //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,28 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,24 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // *

        pop ecx
        dec ecx
        jnz x_loop

        pop ecx
        dec ecx
        jz end_y_loop
        push ecx

        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        mov ecx,dword ptr [wid_64]
x_loop_2:
        push ecx

        mov eax,dword ptr [esi+4]       // read all 8 pixels
        bswap eax
        mov edx,eax

        // 1st dword {  
        xor ecx,ecx

        // pixel #1
        //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,24 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,28 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #2
        //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        mov eax,edx
        shr eax,12 //Alpha 
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,16 // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #3
        //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,4 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #4
        //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,12 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,8 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax


        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword { 
        xor ecx,ecx 

        // pixel #5
        //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,8 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,12 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #6
        //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,4
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #7
        //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,16
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,12 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #8
        //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,28 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,24 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,8
        mov edx,eax

// 1st dword {  
        xor ecx,ecx

        // pixel #1
        //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,24 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,28 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #2
        //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        mov eax,edx
        shr eax,12 //Alpha 
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,16 // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #3
        //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,4 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #4
        //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,12 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,8 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax


        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword { 
        xor ecx,ecx 

        // pixel #5
        //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
        //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
        mov eax,edx
        shr eax,8 //Alpha 
        and eax,0x00000010
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shr eax,12 // Intensity
        and eax,0x0000000E
        or ecx,eax
        shr eax,3
        or ecx,eax

        // pixel #6
        //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
        //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,4
        and eax,0x00001000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx // Intensity
        and eax,0x00000E00
        or ecx,eax
        shr eax,3
        and eax,0x00000100
        or ecx,eax
                
        // pixel #7
        //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
        //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
        //Alpha 
        mov eax,edx
        shl eax,16
        and eax,0x00100000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,12 // Intensity
        and eax,0x000E0000
        or ecx,eax
        shr eax,3
        and eax,0x00010000
        or ecx,eax

        // pixel #8
        //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
        //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
        mov eax,edx
        shl eax,28 //Alpha 
        and eax,0x10000000
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        shl eax,1
        or ecx,eax
        mov eax,edx
        shl eax,24 // Intensity
        and eax,0x0E000000
        or ecx,eax
        shr eax,3
        and eax,0x01000000
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx
        dec ecx
        jnz x_loop_2
        
        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        pop ecx
        dec ecx
        jnz y_loop

end_y_loop:
    }
#elif !defined(NO_ASM)
   //printf("Load4bIA\n");
   long lTempX, lTempY, lHeight = (long) height;
   asm volatile (
    "1:                     \n"  // y_loop2
    "mov %[c], %[tempy]     \n"

    "mov %[wid_64], %%ecx  \n"
    "2:                    \n"  // x_loop2
    "mov %[c], %[tempx]     \n"
    
    "mov (%[src]), %%eax     \n"        // read all 8 pixels
    "bswap %%eax            \n"
    "add $4, %[src]          \n"
    "mov %%eax, %%edx       \n"
    
    // 1st dword {  
    "xor %%ecx, %%ecx       \n"
    
    // pixel #1
    //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $24, %%eax         \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $28, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #2
    //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" //Alpha 
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $16, %%eax         \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #3
    //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $4, %%eax          \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #4
    //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $8, %%eax          \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

        // 2nd dword { 
    "xor %%ecx, %%ecx       \n"
    
    // pixel #5
    //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $8, %%eax          \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"

    // pixel #6
    //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $4, %%eax          \n"
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #7
    //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $16, %%eax         \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"

    // pixel #8
    //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $28, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $24, %%eax         \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

    // * copy
    "mov (%[src]), %%eax     \n"        // read all 8 pixels
    "bswap %%eax            \n"
    "add $4, %[src]          \n"
    "mov %%eax, %%edx       \n"
    
    // 1st dword {  
    "xor %%ecx, %%ecx       \n"
    
    // pixel #1
    //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $24, %%eax         \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $28, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #2
    //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" //Alpha 
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $16, %%eax         \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #3
    //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $4, %%eax          \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"

    // pixel #4
    //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $8, %%eax          \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

        // 2nd dword { 
    "xor %%ecx, %%ecx       \n"
    
    // pixel #5
    //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $8, %%eax          \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"

    // pixel #6
    //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $4, %%eax          \n"
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
                
    // pixel #7
    //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $16, %%eax         \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #8
    //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $28, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $24, %%eax         \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }
    
    // *

    "mov %[tempx], %[c]     \n"
    "dec %%ecx              \n"
    "jnz 2b                 \n"  // x_loop2
    
    "mov %[tempy], %[c]     \n"
    "dec %%ecx              \n"
    "jz 4f                  \n"  // end_y_loop2
    "mov %[c], %[tempy]     \n"

    "add %[line], %[src]    \n"
    "add %[ext], %[dst]     \n"

    "mov %[wid_64], %%ecx   \n"
    "3:                     \n"  // x_loop_22
    "mov %[c], %[tempx]     \n"
    
    "mov 4(%[src]), %%eax    \n"        // read all 8 pixels
    "bswap %%eax            \n"
    "mov %%eax, %%edx       \n"
    
    // 1st dword {  
    "xor %%ecx, %%ecx       \n"
    
    // pixel #1
    //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $24, %%eax         \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $28, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #2
    //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" //Alpha 
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $16, %%eax         \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #3
    //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $4, %%eax          \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #4
    //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $8, %%eax          \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

        // 2nd dword { 
    "xor %%ecx, %%ecx       \n"
    
    // pixel #5
    //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $8, %%eax          \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"

    // pixel #6
    //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $4, %%eax          \n"
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #7
    //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $16, %%eax         \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #8
    //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $28, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $24, %%eax         \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

    // * copy
    "mov (%[src]), %%eax     \n"        // read all 8 pixels
    "bswap %%eax            \n"
    "add $8, %[src]          \n"
    "mov %%eax, %%edx       \n"
    
    // 1st dword {  
    "xor %%ecx, %%ecx       \n"
    
    // pixel #1
    //  IIIAxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $24, %%eax         \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $28, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"

    // pixel #2
    //  xxxxIIIAxxxxxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" //Alpha 
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $16, %%eax         \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #3
    //  xxxxxxxxIIIAxxxxxxxxxxxxxxxxxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $4, %%eax          \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #4
    //  xxxxxxxxxxxxIIIAxxxxxxxxxxxxxxxx
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $8, %%eax          \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }

        // 2nd dword { 
    "xor %%ecx, %%ecx       \n"
    
    // pixel #5
    //  xxxxxxxxxxxxxxxxIIIAxxxxxxxxxxxx
    //  xxxxxxxxxxxxxxxxxxxxxxxxAAAAIIII
    "mov %%edx, %%eax       \n"
    "shr $8, %%eax          \n" //Alpha 
    "and $0x00000010, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shr $12, %%eax         \n" // Intensity
    "and $0x0000000E, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "or %%eax, %%ecx        \n"

    // pixel #6
    //  xxxxxxxxxxxxxxxxxxxxIIIAxxxxxxxx
    //  xxxxxxxxxxxxxxxxAAAAIIIIxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $4, %%eax          \n"
    "and $0x00001000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n" // Intensity
    "and $0x00000E00, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00000100, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    // pixel #7
    //  xxxxxxxxxxxxxxxxxxxxxxxxIIIAxxxx
    //  xxxxxxxxAAAAIIIIxxxxxxxxxxxxxxxx
    //Alpha 
    "mov %%edx, %%eax       \n"
    "shl $16, %%eax         \n"
    "and $0x00100000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $12, %%eax         \n" // Intensity
    "and $0x000E0000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x00010000, %%eax \n"
    "or %%eax, %%ecx        \n"

    // pixel #8
    //  xxxxxxxxxxxxxxxxxxxxxxxxxxxxIIIA
    //  AAAAIIIIxxxxxxxxxxxxxxxxxxxxxxxx
    "mov %%edx, %%eax       \n"
    "shl $28, %%eax         \n" //Alpha 
    "and $0x10000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "shl $1, %%eax          \n"
    "or %%eax, %%ecx        \n"
    "mov %%edx, %%eax       \n"
    "shl $24, %%eax         \n" // Intensity
    "and $0x0E000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    "shr $3, %%eax          \n"
    "and $0x01000000, %%eax \n"
    "or %%eax, %%ecx        \n"
    
    "mov %%ecx, (%[dst])     \n"
    "add $4, %[dst]          \n"
    // }
    // *

    "mov %[tempx], %[c]     \n"
    "dec %%ecx              \n"
    "jnz 3b                 \n"  // x_loop_22
    
    "add %[line], %[src]    \n"
    "add %[ext], %[dst]     \n"
    
    "mov %[tempy], %[c]     \n"
    "dec %%ecx              \n"
    "jnz 1b                 \n"  // y_loop2
    
    "4:                     \n"  // end_y_loop2
    : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [src]"+S"(src), [dst]"+D"(dst), [c]"+c"(lHeight)
    : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
    : "memory", "cc", "eax", "edx"
    );
#endif

    return /*(0 << 16) | */GR_TEXFMT_ALPHA_INTENSITY_44;
}

//****************************************************************
// Size: 0, Format: 4

DWORD Load4bI (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (rdp.tlut_mode != 0)
        return Load4bCI (dst, src, wid_64, height, line, real_width, tile);

    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 4));
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        push ecx

        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword {
        xor ecx,ecx
        shr eax,28      // 0xF0000000 -> 0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x0F000000 -> 0x00000F00
        shr eax,16
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shr eax,4       // 0x00F00000 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,8       // 0x000F0000 -> 0x0F000000
        and eax,0x0F000000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword {
        xor ecx,ecx
        mov eax,edx
        shr eax,12      // 0x0000F000 -> 0x0000000F
        and eax,0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x00000F00 -> 0x00000F00
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,12      // 0x000000F0 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        shl edx,24      // 0x0000000F -> 0x0F000000
        and edx,0x0F000000
        or ecx,edx
        shl edx,4
        or ecx,edx

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,4
        mov edx,eax

        // 1st dword {
        xor ecx,ecx
        shr eax,28      // 0xF0000000 -> 0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x0F000000 -> 0x00000F00
        shr eax,16
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shr eax,4       // 0x00F00000 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,8       // 0x000F0000 -> 0x0F000000
        and eax,0x0F000000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword {
        xor ecx,ecx
        mov eax,edx
        shr eax,12      // 0x0000F000 -> 0x0000000F
        and eax,0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x00000F00 -> 0x00000F00
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,12      // 0x000000F0 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        shl edx,24      // 0x0000000F -> 0x0F000000
        and edx,0x0F000000
        or ecx,edx
        shl edx,4
        or ecx,edx

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx
        dec ecx
        jnz x_loop

        pop ecx
        dec ecx
        jz end_y_loop
        push ecx

        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        mov ecx,dword ptr [wid_64]
x_loop_2:
        push ecx

        mov eax,dword ptr [esi+4]       // read all 8 pixels
        bswap eax
        mov edx,eax

        // 1st dword {
        xor ecx,ecx
        shr eax,28      // 0xF0000000 -> 0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x0F000000 -> 0x00000F00
        shr eax,16
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shr eax,4       // 0x00F00000 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,8       // 0x000F0000 -> 0x0F000000
        and eax,0x0F000000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword {
        xor ecx,ecx
        mov eax,edx
        shr eax,12      // 0x0000F000 -> 0x0000000F
        and eax,0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x00000F00 -> 0x00000F00
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,12      // 0x000000F0 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        shl edx,24      // 0x0000000F -> 0x0F000000
        and edx,0x0F000000
        or ecx,edx
        shl edx,4
        or ecx,edx

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // * copy
        mov eax,dword ptr [esi]     // read all 8 pixels
        bswap eax
        add esi,8
        mov edx,eax

        // 1st dword {
        xor ecx,ecx
        shr eax,28      // 0xF0000000 -> 0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x0F000000 -> 0x00000F00
        shr eax,16
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shr eax,4       // 0x00F00000 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,8       // 0x000F0000 -> 0x0F000000
        and eax,0x0F000000
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov dword ptr [edi],ecx
        add edi,4
        // }

        // 2nd dword {
        xor ecx,ecx
        mov eax,edx
        shr eax,12      // 0x0000F000 -> 0x0000000F
        and eax,0x0000000F
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx     // 0x00000F00 -> 0x00000F00
        and eax,0x00000F00
        or ecx,eax
        shl eax,4
        or ecx,eax

        mov eax,edx
        shl eax,12      // 0x000000F0 -> 0x000F0000
        and eax,0x000F0000
        or ecx,eax
        shl eax,4
        or ecx,eax

        shl edx,24      // 0x0000000F -> 0x0F000000
        and edx,0x0F000000
        or ecx,edx
        shl edx,4
        or ecx,edx

        mov dword ptr [edi],ecx
        add edi,4
        // }
        // *

        pop ecx
        dec ecx
        jnz x_loop_2
        
        add esi,dword ptr [line]
        add edi,dword ptr [ext]

        pop ecx
        dec ecx
        jnz y_loop

end_y_loop:
    }
#elif !defined(NO_ASM)
   //printf("Load4bI\n");
   int lTempX, lTempY, lHeight = (int) height;
   asm volatile (
         "1:                     \n"  // y_loop3
         "mov %[c], %[tempy]     \n"
         
         "mov %[wid_64], %%ecx   \n"
         "2:                     \n"  // x_loop3
         "mov %[c], %[tempx]     \n"
         
         "mov (%[src]), %%eax     \n"       // read all 8 pixels
         "bswap %%eax            \n"
         "add $4, %[src]          \n"
         "mov %%eax, %%edx       \n"

         // 1st dword {
         "xor %%ecx, %%ecx       \n"
         "shr $28, %%eax         \n"        // 0xF0000000 -> 0x0000000F
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x0F000000 -> 0x00000F00
         "shr $16, %%eax         \n"
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shr $4, %%eax          \n"        // 0x00F00000 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $8, %%eax          \n"        // 0x000F0000 -> 0x0F000000
         "and $0x0F000000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }

         // 2nd dword {
         "xor %%ecx, %%ecx       \n"
         "mov %%edx, %%eax       \n"
         "shr $12, %%eax         \n"        // 0x0000F000 -> 0x0000000F
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x00000F00 -> 0x00000F00
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $12, %%eax         \n"        // 0x000000F0 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "shl $24, %%edx         \n"        // 0x0000000F -> 0x0F000000
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         "shl $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }

         // * copy
         "mov (%[src]), %%eax     \n"       // read all 8 pixels
         "bswap %%eax            \n"
         "add $4, %[src]          \n"
         "mov %%eax, %%edx       \n"
         
         // 1st dword {
         "xor %%ecx, %%ecx       \n"
         "shr $28, %%eax         \n"        // 0xF0000000 -> 0x0000000F
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x0F000000 -> 0x00000F00
         "shr $16, %%eax         \n"
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shr $4, %%eax          \n"        // 0x00F00000 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $8, %%eax          \n"        // 0x000F0000 -> 0x0F000000
         "and $0x0F000000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }

         // 2nd dword {
         "xor %%ecx, %%ecx       \n"
         "mov %%edx, %%eax       \n"
         "shr $12, %%eax         \n"        // 0x0000F000 -> 0x0000000F
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x00000F00 -> 0x00000F00
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $12, %%eax         \n"        // 0x000000F0 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "shl $24, %%edx         \n"        // 0x0000000F -> 0x0F000000
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         "shl $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }
         // *

         "mov %[tempx], %[c]     \n"
         "dec %%ecx              \n"
         "jnz 2b                 \n"  // x_loop3
         
         "mov %[tempy], %[c]     \n"
         "dec %%ecx              \n"
         "jz 4f                  \n"  // end_y_loop3
         "mov %[c], %[tempy]     \n"
         
         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "mov %[wid_64], %%ecx   \n"
         "3:                     \n"  // x_loop_23
         "mov %[c], %[tempx]     \n"
         
         "mov 4(%[src]), %%eax    \n"       // read all 8 pixels
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         // 1st dword {
         "xor %%ecx, %%ecx       \n"
         "shr $28, %%eax         \n"        // 0xF0000000 -> 0x0000000F
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x0F000000 -> 0x00000F00
         "shr $16, %%eax         \n"
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shr $4, %%eax          \n"        // 0x00F00000 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $8, %%eax          \n"        // 0x000F0000 -> 0x0F000000
         "and $0x0F000000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }

         // 2nd dword {
         "xor %%ecx, %%ecx       \n"
         "mov %%edx, %%eax       \n"
         "shr $12, %%eax         \n"        // 0x0000F000 -> 0x0000000F
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x00000F00 -> 0x00000F00
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $12, %%eax         \n"        // 0x000000F0 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "shl $24, %%edx         \n"        // 0x0000000F -> 0x0F000000
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         "shl $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }

         // * copy
         "mov (%[src]), %%eax     \n"       // read all 8 pixels
         "bswap %%eax            \n"
         "add $8, %[src]          \n"
         "mov %%eax, %%edx       \n"
         
         // 1st dword {
         "xor %%ecx, %%ecx       \n"
         "shr $28, %%eax         \n"        // 0xF0000000 -> 0x0000000F
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x0F000000 -> 0x00000F00
         "shr $16, %%eax         \n"
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shr $4, %%eax          \n"        // 0x00F00000 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $8, %%eax          \n"        // 0x000F0000 -> 0x0F000000
         "and $0x0F000000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }
         
         // 2nd dword {
         "xor %%ecx, %%ecx       \n"
         "mov %%edx, %%eax       \n"
         "shr $12, %%eax         \n"        // 0x0000F000 -> 0x0000000F
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"        // 0x00000F00 -> 0x00000F00
         "and $0x00000F00, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%edx, %%eax       \n"
         "shl $12, %%eax         \n"        // 0x000000F0 -> 0x000F0000
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $4, %%eax          \n"
         "or %%eax, %%ecx        \n"

         "shl $24, %%edx         \n"        // 0x0000000F -> 0x0F000000
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         "shl $4, %%edx          \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // }
         // *

         "mov %[tempx], %[c]     \n"
         "dec %%ecx              \n"
         "jnz 3b                 \n"  // x_loop_23
         
         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "mov %[tempy], %[c]     \n"
         "dec %%ecx              \n"
         "jnz 1b                 \n"  // y_loop3
         
         "4:                     \n"  // end_y_loop3
         : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [src] "+S"(src), [dst] "+D"(dst), [c]"+c"(lHeight)
         : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
         : "memory", "cc", "eax", "edx"
         );
#endif

    return /*(0 << 16) | */GR_TEXFMT_ALPHA_INTENSITY_44;
}

//****************************************************************
// Size: 0, Format: 0

DWORD Load4bSelect (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (rdp.tlut_mode == 0)
        return Load4bI (dst, src, wid_64, height, line, real_width, tile);

    return Load4bCI (dst, src, wid_64, height, line, real_width, tile);
}

