/*
*   Glide64 - Glide video plugin for Nintendo 64 emulators.
*   Copyright (c) 2002  Dave2001
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

#ifndef Util_H
#define Util_H

#include "winlnxdefs.h"
#include "rdp.h"

#define NOT_TMU0    0x00
#define NOT_TMU1    0x01
#define NOT_TMU2    0x02

void util_init ();
void clip_z ();
void clip_tri (WORD linew = 0);

BOOL cull_tri (VERTEX **v);
void DrawTri (VERTEX **v, WORD linew = 0);
void do_triangle_stuff (WORD linew = 0);
void do_triangle_stuff_2 (WORD linew = 0);
void add_tri (VERTEX *v, int n, int type);
void apply_shade_mods (VERTEX *v);

void update ();
void update_scissor ();

void set_message_combiner ();

void fix_tex_coord (VERTEX **v);

// positional and texel coordinate clipping
#define CCLIP(ux,lx,ut,lt,uc,lc) \
        if (ux > lx || lx < uc || ux > lc) { rdp.tri_n += 2; return; } \
        if (ux < uc) { \
            float p = (uc-ux)/(lx-ux); \
            ut = p*(lt-ut)+ut; \
            ux = uc; \
        } \
        if (lx > lc) { \
            float p = (lc-ux)/(lx-ux); \
            lt = p*(lt-ut)+ut; \
            lx = lc; \
        }

#define CCLIP2(ux,lx,ut,lt,un,ln,uc,lc) \
        if (ux > lx || lx < uc || ux > lc) { rdp.tri_n += 2; return; } \
        if (ux < uc) { \
            float p = (uc-ux)/(lx-ux); \
            ut = p*(lt-ut)+ut; \
            un = p*(ln-un)+un; \
            ux = uc; \
        } \
        if (lx > lc) { \
            float p = (lc-ux)/(lx-ux); \
            lt = p*(lt-ut)+ut; \
            ln = p*(ln-un)+un; \
            lx = lc; \
        }

#endif  // ifndef Util_H

