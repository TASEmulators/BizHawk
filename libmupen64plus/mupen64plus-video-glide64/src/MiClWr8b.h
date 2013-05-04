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

//****************************************************************
// 8-bit Horizontal Mirror

void Mirror8bS (unsigned char * tex, DWORD mask, DWORD max_width, DWORD real_width, DWORD height)
{
    if (mask == 0) return;

    DWORD mask_width = (1 << mask);
    DWORD mask_mask = (mask_width-1);
    if (mask_width >= max_width) return;
    int count = max_width - mask_width;
    if (count <= 0) return;
    int line_full = real_width;
    int line = line_full - (count);
    if (line < 0) return;
    unsigned char * start = tex + (mask_width);
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov edi,dword ptr [start]

        mov ecx,dword ptr [height]
loop_y:

        xor edx,edx
loop_x:
        mov esi,dword ptr [tex]
        mov ebx,dword ptr [mask_width]
        add ebx,edx
        and ebx,dword ptr [mask_width]
        jnz is_mirrored

        mov eax,edx
        and eax,dword ptr [mask_mask]
        add esi,eax
        mov al,byte ptr [esi]
        mov byte ptr [edi],al
        inc edi
        jmp end_mirror_check
is_mirrored:
        add esi,dword ptr [mask_mask]
        mov eax,edx
        and eax,dword ptr [mask_mask]
        sub esi,eax
        mov al,byte ptr [esi]
        mov byte ptr [edi],al
        inc edi
end_mirror_check:

        inc edx
        cmp edx,dword ptr [count]
        jne loop_x

        add edi,dword ptr [line]
        mov eax,dword ptr [tex]
        add eax,dword ptr [line_full]
        mov dword ptr [tex],eax

        dec ecx
        jnz loop_y
    }
#elif !defined(NO_ASM)
   //printf("Mirror8bS\n");
    intptr_t fake_esi,fake_eax;
   asm volatile (
         "1:                        \n"  // loop_y3
         
         "xor %%edx, %%edx          \n"
         "2:                        \n"  // loop_x3
         "mov %[tex], %[S]        \n"
         "mov %[mask_width], %%eax \n"
         "add %%edx, %%eax          \n"
         "and %[mask_width], %%eax \n"
         "jnz 3f                   \n"  // is_mirrored2
         
         "mov %%edx, %%eax          \n"
         "and %[mask_mask], %[a]    \n"
         "add %[a], %[S]            \n"
         "mov (%[S]), %%al          \n"
         "mov %%al, (%[start])      \n"
         "inc %[start]              \n"
         "jmp 4f                    \n"  // end_mirror_check2
         "3:                        \n"  // is_mirrored2
         "add %[mask_mask], %[S]    \n"
         "mov %%edx, %%eax          \n"
         "and %[mask_mask], %[a]    \n"
         "sub %[a], %[S]            \n"
         "mov (%[S]), %%al          \n"
         "mov %%al, (%[start])      \n"
         "inc %[start]              \n"
         "4:                        \n"  // end_mirror_check2
         
         "inc %%edx                 \n"
         "cmp %[count], %%edx       \n"
         "jne 2b                    \n"  // loop_x3
         
         "add %[line], %[start]     \n"
         "add %[line_full], %[tex]  \n"
         
         "dec %%ecx                 \n"
         "jnz 1b                    \n"  // loop_y3
         : [S] "=&S" (fake_esi), [a]"=&a"(fake_eax), [start]"+D"(start), "+c"(height), [tex] "+r" (tex)
         : [mask_width] "g" (mask_width), [mask_mask] "g" ((intptr_t)mask_mask), [count] "g" (count), [line] "g" ((intptr_t)line), [line_full] "g" ((intptr_t)line_full)
         : "memory", "cc", "edx"
         );
#endif // _WIN32
}

//****************************************************************
// 8-bit Vertical Mirror

void Mirror8bT (unsigned char * tex, DWORD mask, DWORD max_height, DWORD real_width)
{
    if (mask == 0) return;

    DWORD mask_height = (1 << mask);
    DWORD mask_mask = mask_height-1;
    if (max_height <= mask_height) return;
    int line_full = real_width;

    unsigned char * dst = tex + mask_height * line_full;

    for (DWORD y=mask_height; y<max_height; y++)
    {
        if (y & mask_height)
        {
            // mirrored
            memcpy ((void*)dst, (void*)(tex + (mask_mask - (y & mask_mask)) * line_full), line_full);
        }
        else
        {
            // not mirrored
            memcpy ((void*)dst, (void*)(tex + (y & mask_mask) * line_full), line_full);
        }

        dst += line_full;
    }
}

//****************************************************************
// 8-bit Horizontal Wrap (like mirror) ** UNTESTED **

void Wrap8bS (unsigned char * tex, DWORD mask, DWORD max_width, DWORD real_width, DWORD height)
{
    if (mask == 0) return;

    DWORD mask_width = (1 << mask);
    DWORD mask_mask = (mask_width-1) >> 2;
    if (mask_width >= max_width) return;
    int count = (max_width - mask_width) >> 2;
    if (count <= 0) return;
    int line_full = real_width;
    int line = line_full - (count << 2);
    if (line < 0) return;
    unsigned char * start = tex + (mask_width);
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov edi,dword ptr [start]

        mov ecx,dword ptr [height]
loop_y:

        xor edx,edx
loop_x:

        mov esi,dword ptr [tex]
        mov eax,edx
        and eax,dword ptr [mask_mask]
        shl eax,2
        add esi,eax
        mov eax,dword ptr [esi]
        mov dword ptr [edi],eax
        add edi,4

        inc edx
        cmp edx,dword ptr [count]
        jne loop_x

        add edi,dword ptr [line]
        mov eax,dword ptr [tex]
        add eax,dword ptr [line_full]
        mov dword ptr [tex],eax

        dec ecx
        jnz loop_y
    }
#elif !defined(NO_ASM)
   //printf("wrap8bS\n");
    intptr_t fake_esi,fake_eax;
   asm volatile (
         "1:                       \n"  // loop_y4

         "xor %%edx, %%edx         \n"
         "2:                       \n"  // loop_x4
         
         "mov %[tex], %[S]       \n"
         "mov %%edx, %%eax         \n"
         "and %[mask_mask], %%eax \n"
         "shl $2, %%eax            \n"
         "add %[a], %[S]         \n"
         "mov (%[S]), %%eax       \n"
         "mov %%eax, (%[start])       \n"
         "add $4, %[start]            \n"
         
         "inc %%edx                \n"
         "cmp %[count], %%edx     \n"
         "jne 2b                     \n"  // loop_x4

         "add %[line], %[start]      \n"
         "add %[line_full], %[tex] \n"
         
         "dec %%ecx                \n"
         "jnz 1b                   \n"  // loop_y4
         : [S] "=&S" (fake_esi), [a]"=&a"(fake_eax), [start]"+D"(start), [tex] "+r" (tex), "+c"(height)
         : [mask_mask] "g" (mask_mask), [count] "g" (count), [line] "g" ((intptr_t)line), [line_full] "g" ((intptr_t)line_full)
         : "memory", "cc", "edx"
         );
#endif
}

//****************************************************************
// 8-bit Vertical Wrap

void Wrap8bT (unsigned char * tex, DWORD mask, DWORD max_height, DWORD real_width)
{
    if (mask == 0) return;

    DWORD mask_height = (1 << mask);
    DWORD mask_mask = mask_height-1;
    if (max_height <= mask_height) return;
    int line_full = real_width;

    unsigned char * dst = tex + mask_height * line_full;

    for (DWORD y=mask_height; y<max_height; y++)
    {
        // not mirrored
        memcpy ((void*)dst, (void*)(tex + (y & mask_mask) * line_full), line_full);

        dst += line_full;
    }
}

//****************************************************************
// 8-bit Horizontal Clamp

void Clamp8bS (unsigned char * tex, DWORD width, DWORD clamp_to, DWORD real_width, DWORD real_height)
{
    if (real_width <= width) return;

    unsigned char * dest = tex + (width);
    unsigned char * constant = dest-1;
    int count = clamp_to - width;

    int line_full = real_width;
    int line = width;
#if !defined(__GNUC__) && !defined(NO_ASM)
    __asm {
        mov esi,dword ptr [constant]
        mov edi,dword ptr [dest]

        mov ecx,real_height
y_loop:

        mov al,byte ptr [esi]

        mov edx,dword ptr [count]
x_loop:

        mov byte ptr [edi],al       // don't unroll or make dword, it may go into next line (doesn't have to be multiple of two)
        inc edi

        dec edx
        jnz x_loop

        add esi,dword ptr [line_full]
        add edi,dword ptr [line]

        dec ecx
        jnz y_loop
    }
#elif !defined(NO_ASM)
   //printf("clamp8bs\n");
   asm volatile (
         "0: \n"
         
         "mov (%[constant]), %%al        \n"

         "mov %[count], %%edx     \n"
         "1: \n"
         
         "mov %%al, (%[dest])        \n"        // don't unroll or make dword, it may go into next line (doesn't have to be multiple of two)
         "inc %[dest]                \n"
         
         "dec %%edx                \n"
         "jnz 1b \n"
         
         "add %[line_full], %[constant] \n"
         "add %[line], %[dest]      \n"
         
         "dec %%ecx                \n"
         "jnz 0b \n"
         : [constant]"+S"(constant), [dest]"+D"(dest), "+c"(real_height)
         : [count] "g" (count), [line] "g" ((uintptr_t)line), [line_full] "g" ((intptr_t)line_full)
         : "memory", "cc", "eax", "edx"
         );
#endif
}

//****************************************************************
// 8-bit Vertical Clamp

void Clamp8bT (unsigned char * tex, DWORD height, DWORD real_width, DWORD clamp_to)
{
    int line_full = real_width;
    unsigned char * dst = tex + height * line_full;
    unsigned char * const_line = dst - line_full;

    for (DWORD y=height; y<clamp_to; y++)
    {
        memcpy ((void*)dst, (void*)const_line, line_full);
        dst += line_full;
    }
}

