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
// Size: 2, Format: 0

DWORD Load16bRGBA (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 2)) << 1;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        mov eax,dword ptr [esi]     // read both pixels
        add esi,4
        bswap eax
        mov edx,eax

        ror ax,1
        ror eax,16
        ror ax,1

        mov dword ptr [edi],eax
        add edi,4

        // * copy
        mov eax,dword ptr [esi]     // read both pixels
        add esi,4
        bswap eax
        mov edx,eax

        ror ax,1
        ror eax,16
        ror ax,1

        mov dword ptr [edi],eax
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
        mov eax,dword ptr [esi+4]       // read both pixels
        bswap eax
        mov edx,eax

        ror ax,1
        ror eax,16
        ror ax,1

        mov dword ptr [edi],eax
        add edi,4

        // * copy
        mov eax,dword ptr [esi]     // read both pixels
        add esi,8
        bswap eax
        mov edx,eax

        ror ax,1
        ror eax,16
        ror ax,1

        mov dword ptr [edi],eax
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
   //printf("Load16bRGBA\n");
   long lTemp, lHeight = (long) height;
   asm volatile (
         "y_loop7:              \n"
         "mov %[c], %[temp]     \n"
         
         "mov %[wid_64], %%ecx \n"
         "x_loop7:              \n"
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $4, %[src]         \n"
         "bswap %%eax           \n"
         "mov %%eax, %%edx      \n"
         
         "ror $1, %%ax          \n"
         "ror $16, %%eax        \n"
         "ror $1, %%ax          \n"
         
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         
         // * copy
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $4, %[src]         \n"
         "bswap %%eax           \n"
         "mov %%eax, %%edx      \n"
         
         "ror $1, %%ax          \n"
         "ror $16, %%eax        \n"
         "ror $1, %%ax          \n"
         
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         // *
         
         "dec %%ecx             \n"
         "jnz x_loop7           \n"
         
         "mov %[temp], %[c]     \n"
         "dec %%ecx             \n"
         "jz end_y_loop7        \n"
         "mov %[c], %[temp]     \n"

         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "mov %[wid_64], %%ecx \n"
         "x_loop_27:            \n"
         "mov 4(%[src]), %%eax   \n"        // read both pixels
         "bswap %%eax           \n"
         "mov %%eax, %%edx      \n"
         
         "ror $1, %%ax          \n"
         "ror $16, %%eax        \n"
         "ror $1, %%ax          \n"
         
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         
         // * copy
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $8, %[src]         \n"
         "bswap %%eax           \n"
         "mov %%eax, %%edx      \n"
         
         "ror $1, %%ax          \n"
         "ror $16, %%eax        \n"
         "ror $1, %%ax          \n"
         
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         // *

         "dec %%ecx             \n"
         "jnz x_loop_27         \n"
         
         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "mov %[temp], %[c]     \n"
         "dec %%ecx             \n"
         "jnz y_loop7           \n"
         
         "end_y_loop7:          \n"
         : [temp]"=m"(lTemp), [src]"+S"(src), [dst]"+D"(dst), [c]"+c"(lHeight)
         : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
         : "memory", "cc", "eax", "edx"
         );
#endif
    return (1 << 16) | GR_TEXFMT_ARGB_1555;
}

//****************************************************************
// Size: 2, Format: 3
//
// ** by Gugaman/Dave2001 **

DWORD Load16bIA (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 2)) << 1;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        mov eax,dword ptr [esi]     // read both pixels
        add esi,4
        mov dword ptr [edi],eax
        add edi,4

        // * copy
        mov eax,dword ptr [esi]     // read both pixels
        add esi,4
        mov dword ptr [edi],eax
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
        mov eax,dword ptr [esi+4]       // read both pixels
        mov dword ptr [edi],eax
        add edi,4

        // * copy
        mov eax,dword ptr [esi]     // read both pixels
        add esi,8
        mov dword ptr [edi],eax
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
   //printf("Load16bIA\n");
   long lTemp, lHeight = (long) height;
   asm volatile (
         "y_loop8:              \n"
         "mov %[c], %[temp]     \n"

         "mov %[wid_64], %%ecx \n"
         "x_loop8:              \n"
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $4, %[src]         \n"
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         
         // * copy
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $4, %[src]         \n"
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         // *

         "dec %%ecx             \n"
         "jnz x_loop8           \n"
         
         "mov %[temp], %[c]     \n"
         "dec %%ecx             \n"
         "jz end_y_loop8        \n"
         "mov %[c], %[temp]     \n"
         
         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "mov %[wid_64], %%ecx \n"
         "x_loop_28:            \n"
         "mov 4(%[src]), %%eax   \n"        // read both pixels
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         
         // * copy
         "mov (%[src]), %%eax    \n"        // read both pixels
         "add $8, %[src]         \n"
         "mov %%eax, (%[dst])    \n"
         "add $4, %[dst]         \n"
         // *

         "dec %%ecx             \n"
         "jnz x_loop_28         \n"
         
         "add %[line], %[src]   \n"
         "add %[ext], %[dst]    \n"
         
         "mov %[temp], %[c]     \n"
         "dec %%ecx             \n"
         "jnz y_loop8           \n"
         
         "end_y_loop8:          \n"
         : [temp]"=m"(lTemp), [src]"+S"(src), [dst]"+D"(dst), [c]"+c"(lHeight)
         : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
         : "memory", "cc", "eax"
         );
#endif
    return (1 << 16) | GR_TEXFMT_ALPHA_INTENSITY_88;
}

