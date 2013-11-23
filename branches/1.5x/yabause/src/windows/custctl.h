/*  Copyright 2008 Theo Berkau

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

#ifndef CUSTCTL_H
#define CUSTCTL_H

#include "../core.h"
#include "../memory.h"

typedef struct
{
   HWND hwnd;
   HFONT font;
   int hasfocus;
   COLORREF text_color;
   COLORREF bg_color;
   SCROLLINFO scrollinfo;
   TEXTMETRIC fontmetric;
} CustomCtl_struct;

LRESULT InitCustomCtl(HWND hwnd, WPARAM wParam, LPARAM lParam, int customsize);
void DestroyCustomCtl(CustomCtl_struct *cc);
LRESULT CustomCtl_SetFont(CustomCtl_struct *cc, WPARAM wParam, LPARAM lParam);
void CustomCtl_SetFocus(CustomCtl_struct *cc);
void CustomCtl_KillFocus(CustomCtl_struct *cc);

#endif
