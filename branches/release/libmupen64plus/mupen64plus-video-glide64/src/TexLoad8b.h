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

DWORD Load8bCI (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 3)) << 1;
    unsigned short * pal = rdp.pal_8;

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
                
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,4
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
                mov cx,word ptr [ebx+edx]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // * copy
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,4
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
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
                
                mov eax,dword ptr [esi+4]       // read all 4 pixels
                bswap eax
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
                mov cx,word ptr [ebx+edx]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // * copy
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,8
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,1
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
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
       //printf("Load8bCI1\n");
       long lTempX, lTempY, lHeight = (long) height;
       intptr_t fake_eax, fake_edx;
       asm volatile (
             "1:                     \n"  // y_loop4
             "mov %[c], %[tempy]     \n"
                
             "mov %[wid_64], %%ecx   \n"
             "2:                     \n"  // x_loop4
             "mov %[c], %[tempx]     \n"
             
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // * copy
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *
                
             "mov %[tempx], %[c]     \n"

             "dec %%ecx               \n"
             "jnz 2b                  \n"  // x_loop4
             
             "mov %[tempy], %[c]      \n"
             "dec %%ecx               \n"
             "jz 4f                   \n"  // end_y_loop4
             "mov %[c], %[tempy]      \n"
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[wid_64], %%ecx   \n"
             "3:                     \n"  // x_loop_24
             "mov %[c], %[tempx]     \n"
             
             "mov 4(%[src]), %%eax     \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // * copy
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $8, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $1, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $1, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *
             
             "mov %[tempx], %[c]      \n"
             "dec %%ecx               \n"
             "jnz 3b                  \n"  // x_loop_24
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[tempy], %[c]      \n"
             "dec %%ecx               \n"
             "jnz 1b                  \n"  // y_loop4
             
             "4:                      \n"  // end_y_loop4
             : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [a] "=&a" (fake_eax), [d] "=&d" (fake_edx), [src]"+S"(src), [dst]"+D"(dst), [c]"+c"(lHeight)
             : [pal] "r" (pal), [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
             : "memory", "cc"
             );
#endif
    return (1 << 16) | GR_TEXFMT_ARGB_1555;
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
                
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,4
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
                mov cx,word ptr [ebx+edx]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // * copy
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,4
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
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
                
                mov eax,dword ptr [esi+4]       // read all 4 pixels
                bswap eax
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
                mov cx,word ptr [ebx+edx]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // * copy
                mov eax,dword ptr [esi]     // read all 4 pixels
                bswap eax
                add esi,8
                mov edx,eax
                
                // 1st dword output {
                shr eax,15
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                mov eax,edx
                shr eax,23
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                
                mov dword ptr [edi],ecx
                add edi,4
                // }
                
                // 2nd dword output {
                mov eax,edx
                shl eax,1
                and eax,0x1FE
                mov cx,word ptr [ebx+eax]
                ror cx,8
                shl ecx,16
                
                shr edx,7
                and edx,0x1FE
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
       //printf("Load8bCI1\n");
       long lTempX, lTempY, lHeight = (long) height;
        intptr_t fake_eax, fake_edx;
       asm volatile (
             "1:                      \n"  // ia_y_loop2
             "mov %[c], %[tempy]      \n"
                
             "mov %[wid_64], %%ecx   \n"
             "2:                     \n"  // ia_x_loop2
             "mov %[c], %[tempx]     \n"
             
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // * copy
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $4, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *
                
             "mov %[tempx], %[c]      \n"
             "dec %%ecx               \n"
             "jnz 2b                  \n"  // ia_x_loop2
             
             "mov %[tempy], %[c]      \n"
             "dec %%ecx               \n"
             "jz 4f                   \n"  // ia_end_y_loop2
             "mov %[c], %[tempy]      \n"
                
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[wid_64], %%ecx    \n"
             "3:                      \n"  // ia_x_loop_22
             "mov %[c], %[tempx]      \n"
             
             "mov 4(%[src]), %%eax     \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
                
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // * copy
             "mov (%[src]), %%eax      \n"      // read all 4 pixels
             "bswap %%eax             \n"
             "add $8, %[src]           \n"
             "mov %%eax, %%edx        \n"
             
             // 1st dword output {
             "shr $15, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "mov %%edx, %%eax        \n"
             "shr $23, %%eax          \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             
             // 2nd dword output {
             "mov %%edx, %%eax        \n"
             "shl $1, %%eax           \n"
             "and $0x1FE, %%eax       \n"
             "mov (%[pal],%[a]), %%cx \n"
             "ror $8, %%cx            \n"
             "shl $16, %%ecx          \n"
             
             "shr $7, %%edx           \n"
             "and $0x1FE, %%edx       \n"
             "mov (%[pal],%[d]), %%cx \n"
             "ror $8, %%cx            \n"
             
             "mov %%ecx, (%[dst])      \n"
             "add $4, %[dst]           \n"
             // }
             // *

             "mov %[tempx], %[c]      \n"
             "dec %%ecx               \n"
             "jnz 3b                  \n"  // ia_x_loop_22
             
             "add %[line], %[src]     \n"
             "add %[ext], %[dst]      \n"
             
             "mov %[tempy], %[c]      \n"
             "dec %%ecx               \n"
             "jnz 1b                  \n"  // ia_y_loop2
             
             "4:                      \n"  // ia_end_y_loop2
             : [tempx]"=m"(lTempX), [tempy]"=m"(lTempY), [a] "=&a" (fake_eax), [d] "=&d" (fake_edx), [src]"+S"(src), [dst]"+D"(dst), [c]"+c"(lHeight)
             : [pal] "r" (pal), [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
             : "memory", "cc"
             );
#endif
    return (1 << 16) | GR_TEXFMT_ALPHA_INTENSITY_88;
    }
    
    return 0;
}

//****************************************************************
// Size: 1, Format: 3
//
// ** by Gugaman **

DWORD Load8bIA (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)  
{ 
    if (rdp.tlut_mode != 0)
        return Load8bCI (dst, src, wid_64, height, line, real_width, tile);

    if (wid_64 < 1) wid_64 = 1;  
    if (height < 1) height = 1;  
    int ext = (real_width - (wid_64 << 3));  
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {  
        mov esi,dword ptr [src]  
            mov edi,dword ptr [dst]  
            
            mov ecx,dword ptr [height]  
y_loop:  
        push ecx  
            
            mov ecx,dword ptr [wid_64]  
x_loop:  
        mov eax,dword ptr [esi]          // read all 4 pixels  
            add esi,4  
            
            xor ebx,ebx 
            mov edx,eax 
            shr eax,4//all alpha 
            and eax,0x0F0F0F0F 
            or ebx,eax 
            mov eax,edx//intensity 
            shl eax,4 
            and eax,0xF0F0F0F0 
            or ebx,eax 
            
            mov dword ptr [edi],ebx // save dword 
            add edi,4  
            
            mov eax,dword ptr [esi]          // read all 4 pixels  
            add esi,4  
            
            xor ebx,ebx 
            mov edx,eax 
            shr eax,4//all alpha 
            and eax,0x0F0F0F0F 
            or ebx,eax 
            mov eax,edx//intensity 
            shl eax,4 
            and eax,0xF0F0F0F0 
            or ebx,eax 
            
            mov dword ptr [edi],ebx // save dword 
            add edi,4  
            // *  
            
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
        mov eax,dword ptr [esi+4]          // read both pixels  
            
            xor ebx,ebx 
            mov edx,eax 
            shr eax,4//all alpha 
            and eax,0x0F0F0F0F 
            or ebx,eax 
            mov eax,edx//intensity 
            shl eax,4 
            and eax,0xF0F0F0F0 
            or ebx,eax 
            
            mov dword ptr [edi],ebx //save dword 
            add edi,4  
            
            mov eax,dword ptr [esi]          // read both pixels  
            add esi,8  
            
            xor ebx,ebx 
            mov edx,eax 
            shr eax,4//all alpha 
            and eax,0x0F0F0F0F 
            or ebx,eax 
            mov eax,edx//intensity 
            shl eax,4 
            and eax,0xF0F0F0F0 
            or ebx,eax 
            
            mov dword ptr [edi],ebx //save dword 
            add edi,4  
            // *  
            
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
   //printf("Load8bIA\n");
   int lTemp, lHeight = (int) height;
   asm volatile (
         "1:                     \n"  // y_loop5
         "mov %[wid_64], %%eax    \n"
         "mov %%eax, %[temp]      \n"
         "2:                      \n"  // x_loop5
         "mov (%[src]), %%eax     \n"          // read all 4 pixels  
         "add $4, %[src]          \n"
         
         "xor %%ecx, %%ecx       \n"
         "mov %%eax, %%edx       \n"
         "shr $4, %%eax          \n"//all alpha 
         "and $0x0F0F0F0F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "mov %%edx, %%eax       \n"//intensity 
         "shl $4, %%eax          \n"
         "and $0xF0F0F0F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n" // save dword 
         "add $4, %[dst]          \n"
         
         "mov (%[src]), %%eax     \n"          // read all 4 pixels  
         "add $4, %[src]          \n"
         
         "xor %%ecx, %%ecx       \n"
         "mov %%eax, %%edx       \n"
         "shr $4, %%eax          \n"//all alpha 
         "and $0x0F0F0F0F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "mov %%edx, %%eax       \n"//intensity 
         "shl $4, %%eax          \n"
         "and $0xF0F0F0F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])    \n" // save dword 
         "add $4, %[dst]         \n"
            
         "decl %[temp]           \n"
         "jnz 2b                 \n"  // x_loop5
         
         "decl %[height]         \n"
         "jz 4f                  \n"  // end_y_loop5
         
         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "mov %[wid_64], %%eax    \n"
         "mov %%eax, %[temp]      \n"
         "3:                      \n"  // x_loop_25
         "mov 4(%[src]), %%eax    \n"          // read both pixels  
         
         "xor %%ecx, %%ecx       \n"
         "mov %%eax, %%edx       \n"
         "shr $4, %%eax          \n"//all alpha 
         "and $0x0F0F0F0F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "mov %%edx, %%eax       \n"//intensity 
         "shl $4, %%eax          \n"
         "and $0xF0F0F0F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n" //save dword 
         "add $4, %[dst]          \n"
         
         "mov (%[src]), %%eax     \n"          // read both pixels  
         "add $8, %[src]          \n"
         
         "xor %%ecx, %%ecx       \n"
         "mov %%eax, %%edx       \n"
         "shr $4, %%eax          \n"//all alpha 
         "and $0x0F0F0F0F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "mov %%edx, %%eax       \n"//intensity 
         "shl $4, %%eax          \n"
         "and $0xF0F0F0F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n" //save dword 
         "add $4, %[dst]          \n"
         // *  
         
         "decl %[temp]           \n"
         "jnz 3b                 \n"  // x_loop_25
         
         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "decl %[height]         \n"
         "jnz 1b                 \n"  // y_loop5
         
         "4:                     \n"  // end_y_loop5
           : [temp]"=m"(lTemp), [src] "+S"(src), [dst] "+D"(dst), [height] "+g"(lHeight)
           : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
           : "memory", "cc", "eax", "edx", "ecx"
           );
#endif
    return /*(0 << 16) | */GR_TEXFMT_ALPHA_INTENSITY_44;  
} 

//****************************************************************
// Size: 1, Format: 4
//
// ** by Gugaman **

DWORD Load8bI (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)  
{ 
    if (rdp.tlut_mode != 0)
        return Load8bCI (dst, src, wid_64, height, line, real_width, tile);
    
    if (wid_64 < 1) wid_64 = 1;  
    if (height < 1) height = 1;  
    int ext = (real_width - (wid_64 << 3));  
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {  
        mov esi,dword ptr [src]  
            mov edi,dword ptr [dst]  
            
            mov ecx,dword ptr [height]  
y_loop:  
        push ecx  
            
            mov ecx,dword ptr [wid_64]  
x_loop:  
        mov eax,dword ptr [esi]          // read all 4 pixels  
            add esi,4  
            
            mov dword ptr [edi],eax // save dword 
            add edi,4  
            
            mov eax,dword ptr [esi]          // read all 4 pixels  
            add esi,4  
            
            mov dword ptr [edi],eax // save dword 
            add edi,4  
            // *  
            
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
        mov eax,dword ptr [esi+4]          // read both pixels  
            
            mov dword ptr [edi],eax //save dword 
            add edi,4  
            
            mov eax,dword ptr [esi]          // read both pixels  
            add esi,8  
            
            mov dword ptr [edi],eax //save dword 
            add edi,4  
            // *  
            
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
   //printf("Load8bI\n");
   int lTemp, lHeight = (int) height;
   asm volatile (
         "1:                     \n"  // y_loop6
         "mov %[wid_64], %%eax   \n"
         "mov %%eax, %[temp]     \n"
         "2:                     \n"  // x_loop6
         "mov (%[src]), %%eax    \n"          // read all 4 pixels  
         "add $4, %[src]         \n"
         
         "mov %%eax, (%[dst])    \n" // save dword 
         "add $4, %[dst]         \n"
         
         "mov (%[src]), %%eax    \n"          // read all 4 pixels  
         "add $4, %[src]         \n"
         
         "mov %%eax, (%[dst])    \n" // save dword 
         "add $4, %[dst]         \n"
         // *  
         
         "decl %[temp]          \n"
         "jnz 2b                \n" // x_loop6
         
         "decl %[height]        \n"
         "jz 4f                 \n" // end_y_loop6
            
         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "mov %[wid_64], %%eax   \n"
         "mov %%eax, %[temp]     \n"
         "3:                     \n"  // x_loop_26
         "mov 4(%[src]), %%eax   \n"          // read both pixels  
         
         "mov %%eax, (%[dst])    \n" //save dword 
         "add $4, %[dst]         \n"
         
         "mov (%[src]), %%eax    \n"          // read both pixels  
         "add $8, %[src]         \n"
         
         "mov %%eax, (%[dst])    \n" //save dword 
         "add $4, %[dst]         \n"

         "decl %[temp]          \n"
         "jnz 3b                \n"  // x_loop_26
         
         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "decl %[height]        \n"
         "jnz 1b                \n"  // y_loop6
         
         "4:                    \n"  // end_y_loop6
         : [temp]"=m"(lTemp), [src]"+S"(src), [dst]"+D"(dst), [height]"+g"(lHeight)
         : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
         : "memory", "cc", "eax", "edx"
         );  
#endif
     return /*(0 << 16) | */GR_TEXFMT_ALPHA_8;  
}

