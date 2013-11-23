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

DWORD Load32bRGBA (unsigned char * dst, unsigned char * src, int wid_64, int height, int line, int real_width, int tile)
{
    if (wid_64 < 1) wid_64 = 1;
    if (height < 1) height = 1;
    int ext = (real_width - (wid_64 << 1)) << 1;

    wid_64 >>= 1;       // re-shift it, load twice as many quadwords
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [src]
        mov edi,dword ptr [dst]

        mov ecx,dword ptr [height]
y_loop:
        push ecx

        mov ecx,dword ptr [wid_64]
x_loop:
        mov eax,dword ptr [esi]     // read first pixel
        add esi,4
        bswap eax
        mov edx,eax

        xor ebx,ebx
        shl eax,8   // 0x000000F0 -> 0x0000F000 (a)
        and eax,0x0000F000
        or ebx,eax
        shr edx,12  // 0x0000F000 -> 0x0000000F (b)
        mov eax,edx
        and eax,0x0000000F
        or ebx,eax
        shr edx,4   // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
        mov eax,edx
        and eax,0x000000F0
        or ebx,eax
        shr edx,4   // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
        and edx,0x00000F00
        or ebx,edx

        mov eax,dword ptr [esi]     // read second pixel
        add esi,4
        bswap eax
        mov edx,eax

        shl eax,24  // 0x000000F0 -> 0xF0000000 (a)
        and eax,0xF0000000
        or ebx,eax
                    // 0x00F00000 -> 0x00F00000 (g)
        mov eax,edx
        and eax,0x00F00000
        or ebx,eax
        rol edx,4   // 0x0000F000 (did not shift) -> 0x000F0000 (b)
        mov eax,edx
        and eax,0x000F0000
        or ebx,eax
        shl edx,24  // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
        and edx,0x0F000000
        or ebx,edx

        mov dword ptr [edi],ebx
        add edi,4

        // * copy
        mov eax,dword ptr [esi]     // read first pixel
        add esi,4
        bswap eax
        mov edx,eax

        xor ebx,ebx
        shl eax,8   // 0x000000F0 -> 0x0000F000 (a)
        and eax,0x0000F000
        or ebx,eax
        shr edx,12  // 0x0000F000 -> 0x0000000F (b)
        mov eax,edx
        and eax,0x0000000F
        or ebx,eax
        shr edx,4   // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
        mov eax,edx
        and eax,0x000000F0
        or ebx,eax
        shr edx,4   // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
        and edx,0x00000F00
        or ebx,edx

        mov eax,dword ptr [esi]     // read second pixel
        add esi,4
        bswap eax
        mov edx,eax

        shl eax,24  // 0x000000F0 -> 0xF0000000 (a)
        and eax,0xF0000000
        or ebx,eax
                    // 0x00F00000 -> 0x00F00000 (g)
        mov eax,edx
        and eax,0x00F00000
        or ebx,eax
        rol edx,4   // 0x0000F000 (did not shift) -> 0x000F0000 (b)
        mov eax,edx
        and eax,0x000F0000
        or ebx,eax
        shl edx,24  // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
        and edx,0x0F000000
        or ebx,edx

        mov dword ptr [edi],ebx
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
        mov eax,dword ptr [esi+8]       // read first pixel
        bswap eax
        mov edx,eax

        xor ebx,ebx
        shl eax,8   // 0x000000F0 -> 0x0000F000 (a)
        and eax,0x0000F000
        or ebx,eax
        shr edx,12  // 0x0000F000 -> 0x0000000F (b)
        mov eax,edx
        and eax,0x0000000F
        or ebx,eax
        shr edx,4   // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
        mov eax,edx
        and eax,0x000000F0
        or ebx,eax
        shr edx,4   // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
        and edx,0x00000F00
        or ebx,edx

        mov eax,dword ptr [esi+12]      // read second pixel
        bswap eax
        mov edx,eax

        shl eax,24  // 0x000000F0 -> 0xF0000000 (a)
        and eax,0xF0000000
        or ebx,eax
                    // 0x00F00000 -> 0x00F00000 (g)
        mov eax,edx
        and eax,0x00F00000
        or ebx,eax
        rol edx,4   // 0x0000F000 (did not shift) -> 0x000F0000 (b)
        mov eax,edx
        and eax,0x000F0000
        or ebx,eax
        shl edx,24  // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
        and edx,0x0F000000
        or ebx,edx

        mov dword ptr [edi],ebx
        add edi,4

        // * copy
        mov eax,dword ptr [esi+0]       // read first pixel
        bswap eax
        mov edx,eax

        xor ebx,ebx
        shl eax,8   // 0x000000F0 -> 0x0000F000 (a)
        and eax,0x0000F000
        or ebx,eax
        shr edx,12  // 0x0000F000 -> 0x0000000F (b)
        mov eax,edx
        and eax,0x0000000F
        or ebx,eax
        shr edx,4   // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
        mov eax,edx
        and eax,0x000000F0
        or ebx,eax
        shr edx,4   // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
        and edx,0x00000F00
        or ebx,edx

        mov eax,dword ptr [esi+4]       // read second pixel
        add esi,16
        bswap eax
        mov edx,eax

        shl eax,24  // 0x000000F0 -> 0xF0000000 (a)
        and eax,0xF0000000
        or ebx,eax
                    // 0x00F00000 -> 0x00F00000 (g)
        mov eax,edx
        and eax,0x00F00000
        or ebx,eax
        rol edx,4   // 0x0000F000 (did not shift) -> 0x000F0000 (b)
        mov eax,edx
        and eax,0x000F0000
        or ebx,eax
        shl edx,24  // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
        and edx,0x0F000000
        or ebx,edx

        mov dword ptr [edi],ebx
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
   //printf("Load32bRGBA\n");
   int lTemp, lHeight = (int) height;
   asm volatile (
         "y_loop9:               \n"

         "mov %[wid_64], %%eax   \n"
         "mov %%eax, %[temp]     \n"
         "x_loop9:               \n"
         "mov (%[src]), %%eax    \n"       // read first pixel
         "add $4, %[src]         \n"
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "xor %%ecx, %%ecx       \n"
         "shl $8, %%eax          \n"    // 0x000000F0 -> 0x0000F000 (a)
         "and $0x0000F000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $12, %%edx         \n"    // 0x0000F000 -> 0x0000000F (b)
         "mov %%edx, %%eax       \n"
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
         "mov %%edx, %%eax       \n"
         "and $0x000000F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
         "and $0x00000F00, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov (%[src]), %%eax     \n"       // read second pixel
         "add $4, %[src]          \n"
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "shl $24, %%eax         \n"    // 0x000000F0 -> 0xF0000000 (a)
         "and $0xF0000000, %%eax \n"
         "or %%eax, %%ecx        \n"    // 0x00F00000 -> 0x00F00000 (g)
         "mov %%edx, %%eax       \n"
         "and $0x00F00000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "rol $4, %%edx          \n"    // 0x0000F000 (did not shift) -> 0x000F0000 (b)
         "mov %%edx, %%eax       \n"
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $24, %%edx         \n"    // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         
         // * copy
         "mov (%[src]), %%eax     \n"       // read first pixel
         "add $4, %[src]          \n"
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "xor %%ecx, %%ecx       \n"
         "shl $8, %%eax          \n"    // 0x000000F0 -> 0x0000F000 (a)
         "and $0x0000F000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $12, %%edx         \n"    // 0x0000F000 -> 0x0000000F (b)
         "mov %%edx, %%eax       \n"
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
         "mov %%edx, %%eax       \n"
         "and $0x000000F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
         "and $0x00000F00, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov (%[src]), %%eax     \n"       // read second pixel
         "add $4, %[src]          \n"
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "shl $24, %%eax         \n"    // 0x000000F0 -> 0xF0000000 (a)
         "and $0xF0000000, %%eax \n"
         "or %%eax, %%ecx        \n"    // 0x00F00000 -> 0x00F00000 (g)
         "mov %%edx, %%eax       \n"
         "and $0x00F00000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "rol $4, %%edx          \n"    // 0x0000F000 (did not shift) -> 0x000F0000 (b)
         "mov %%edx, %%eax       \n"
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $24, %%edx         \n"    // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // *

         "decl %[temp]           \n"
         "jnz x_loop9            \n"
         
         "decl %[height]         \n"
         "jz end_y_loop9         \n"

         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "mov %[wid_64], %%eax   \n"
         "mov %%eax, %[temp]     \n"
         "x_loop_29:             \n"
         "mov 8(%[src]), %%eax   \n"       // read first pixel
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "xor %%ecx, %%ecx       \n"
         "shl $8, %%eax          \n"    // 0x000000F0 -> 0x0000F000 (a)
         "and $0x0000F000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $12, %%edx         \n"    // 0x0000F000 -> 0x0000000F (b)
         "mov %%edx, %%eax       \n"
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
         "mov %%edx, %%eax       \n"
         "and $0x000000F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
         "and $0x00000F00, %%edx \n"
         "or %%edx, %%ecx        \n"

         "mov 12(%[src]), %%eax   \n"       // read second pixel
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"

         "shl $24, %%eax         \n"    // 0x000000F0 -> 0xF0000000 (a)
         "and $0xF0000000, %%eax \n"
         "or %%eax, %%ecx        \n"    // 0x00F00000 -> 0x00F00000 (g)
         "mov %%edx, %%eax       \n"
         "and $0x00F00000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "rol $4, %%edx          \n"    // 0x0000F000 (did not shift) -> 0x000F0000 (b)
         "mov %%edx, %%eax       \n"
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $24, %%edx         \n"    // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         
         // * copy
         "mov (%[src]), %%eax     \n"       // read first pixel
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "xor %%ecx, %%ecx       \n"
         "shl $8, %%eax          \n"    // 0x000000F0 -> 0x0000F000 (a)
         "and $0x0000F000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $12, %%edx         \n"    // 0x0000F000 -> 0x0000000F (b)
         "mov %%edx, %%eax       \n"
         "and $0x0000000F, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0x00F00000 went to 0x00000F00 -> 0x000000F0 (g)
         "mov %%edx, %%eax       \n"
         "and $0x000000F0, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shr $4, %%edx          \n"    // 0xF0000000 went to 0x000F0000 went to 0x0000F000 -> 0x00000F00 (r)
         "and $0x00000F00, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov 4(%[src]), %%eax    \n"       // read second pixel
         "add $16, %[src]         \n"
         "bswap %%eax            \n"
         "mov %%eax, %%edx       \n"
         
         "shl $24, %%eax         \n"    // 0x000000F0 -> 0xF0000000 (a)
         "and $0xF0000000, %%eax \n"
         "or %%eax, %%ecx        \n"    // 0x00F00000 -> 0x00F00000 (g)
         "mov %%edx, %%eax       \n"
         "and $0x00F00000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "rol $4, %%edx          \n"    // 0x0000F000 (did not shift) -> 0x000F0000 (b)
         "mov %%edx, %%eax       \n"
         "and $0x000F0000, %%eax \n"
         "or %%eax, %%ecx        \n"
         "shl $24, %%edx         \n"    // 0xF0000000 went to 0x0000000F -> 0x0F000000 (r)
         "and $0x0F000000, %%edx \n"
         "or %%edx, %%ecx        \n"
         
         "mov %%ecx, (%[dst])     \n"
         "add $4, %[dst]          \n"
         // *

         "decl %[temp]           \n"
         "jnz x_loop_29          \n"
         
         "add %[line], %[src]    \n"
         "add %[ext], %[dst]     \n"
         
         "decl %[height]         \n"
         "jnz y_loop9            \n"
         
         "end_y_loop9:           \n"
         : [temp]"=m"(lTemp), [src]"+S"(src), [dst]"+D"(dst), [height]"+g"(lHeight)
         : [wid_64] "g" (wid_64), [line] "g" ((uintptr_t)line), [ext] "g" ((uintptr_t)ext)
         : "memory", "cc", "ecx", "eax", "edx"
         );
#endif
    return (1 << 16) | GR_TEXFMT_ARGB_4444;
}

