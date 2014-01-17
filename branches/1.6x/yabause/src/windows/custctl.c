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

#include <windows.h>
#include "../debug.h"
#include "custctl.h"

//////////////////////////////////////////////////////////////////////////////

LRESULT InitCustomCtl(HWND hwnd, WPARAM wParam, LPARAM lParam, int customsize)
{
   CustomCtl_struct *cc;

   if ((cc = (CustomCtl_struct *)malloc(customsize)) == NULL)
      return FALSE;

   cc->hwnd = hwnd;
   cc->font = GetStockObject(DEFAULT_GUI_FONT);
   cc->text_color = GetSysColor(COLOR_WINDOWTEXT);
   cc->bg_color = GetSysColor(COLOR_WINDOW);
   cc->hasfocus = FALSE;

   // Set the text
   SetWindowText(hwnd, ((CREATESTRUCT *)lParam)->lpszName);

   // Retrieve scroll info
   cc->scrollinfo.cbSize = sizeof(SCROLLINFO);
   cc->scrollinfo.fMask = SIF_RANGE;
   GetScrollInfo(hwnd, SB_VERT, &cc->scrollinfo);

   // Set our newly created structure to the extra area                                                                                                SetCustCtrl(hwnd, ccp);
   SetWindowLong(hwnd, 0, (LONG)cc);
   return TRUE;
}

//////////////////////////////////////////////////////////////////////////////

void DestroyCustomCtl(CustomCtl_struct *cc)
{
   if (cc)
      free(cc);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CustomCtl_SetFont(CustomCtl_struct *cc, WPARAM wParam, LPARAM lParam)
{
   HDC hdc;
   HFONT oldfont;

   cc->font = (HFONT)wParam;
   hdc = GetDC(cc->hwnd);
   oldfont = SelectObject(hdc, cc->font);
   GetTextMetrics(hdc, &cc->fontmetric);
   SelectObject(hdc, oldfont);
   ReleaseDC(cc->hwnd, hdc);
   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

void CustomCtl_SetFocus(CustomCtl_struct *cc)
{
   cc->hasfocus = TRUE;
}

//////////////////////////////////////////////////////////////////////////////

void CustomCtl_KillFocus(CustomCtl_struct *cc)
{
   cc->hasfocus = FALSE;
}

//////////////////////////////////////////////////////////////////////////////
