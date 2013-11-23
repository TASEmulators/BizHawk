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
#include "../core.h"
#include "../memory.h"
#include "../debug.h"
#include "disasm.h"

typedef struct
{
   HWND hwnd;
   HFONT font;
   int hasfocus;
   COLORREF text_color;
   COLORREF bg_color;
   SCROLLINFO scrollinfo;
   TEXTMETRIC fontmetric;
   u32 addr;
   u32 pc;
   u32 e_addr;
   u32 scrollscale;
   int cursel;
   int (*disinst)(u32 addr, char *string);
} DisasmCtl_struct;

#define DISASMLINES  23

//////////////////////////////////////////////////////////////////////////////

LRESULT DisasmCtl_OnPaint(DisasmCtl_struct *cc, WPARAM wParam, LPARAM lParam)
{
   HDC hdc=(HDC)wParam;
   PAINTSTRUCT ps;
   RECT rect;
   HANDLE oldfont;
   int x=0, y=0;
   char text[MAX_PATH];
   u32 addr;
   SIZE size;
   RECT clip;
   int curaddr=-1;
   BOOL ispc;

   addr = cc->addr;

   // Setup everything for rendering
   if (hdc == NULL)
      hdc = BeginPaint(cc->hwnd, &ps);
   oldfont = SelectObject(hdc, cc->font);

   GetClientRect(cc->hwnd, &rect);

   for(;;)
   {
      ispc = (addr == cc->pc) ? TRUE : FALSE;
      x = 0;
      addr += cc->disinst(addr, text);
      GetTextExtentPoint32A(hdc, text, (int)strlen(text), &size);
      if (size.cy+y >= rect.bottom)
         break;

      // adjust clipping values
      if (y+(size.cy*2) >= rect.bottom)
         clip.bottom = rect.bottom;
      else
         clip.bottom = y+size.cy;

      // Draw the address text
      clip.left = x;
      clip.top = y;
      clip.right = rect.right;
      if (ispc)
      {
         SetTextColor(hdc, GetSysColor(COLOR_HIGHLIGHTTEXT));
         SetBkColor(hdc, GetSysColor(COLOR_HIGHLIGHT));
      }
      else
      {
         SetTextColor(hdc, cc->text_color);
         SetBkColor(hdc, cc->bg_color);
      }
      ExtTextOutA(hdc, x, y, ETO_OPAQUE | ETO_CLIPPED, &clip, text, strlen(text), 0);
      y += size.cy;
   }

   // Let's clean up, and we're done
   SelectObject(hdc, oldfont);

   if ((HDC)wParam == NULL)
      EndPaint(cc->hwnd, &ps);

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT DisasmCtl_SetScrollPos(DisasmCtl_struct *cc)
{
   INT min, max;
   u64 pos;

   GetScrollRange(cc->hwnd, SB_VERT, &min, &max);
   pos = ((u64)cc->addr) * (u64)max / (u64)(cc->e_addr-DISASMLINES);
   SetScrollPos(cc->hwnd, SB_VERT, (int)pos, TRUE);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT DisasmCtl_Vscroll(DisasmCtl_struct *cc, WPARAM wParam, LPARAM lParam)
{
   char text[MAX_PATH];

   switch (LOWORD(wParam))
   {
      case SB_LINEDOWN:
         cc->addr += cc->disinst(cc->addr, text);
         InvalidateRect(cc->hwnd, NULL, FALSE);
         DisasmCtl_SetScrollPos(cc);
         return 0;
      case SB_LINEUP:
         cc->addr -= cc->disinst(cc->addr, text); // rework this
         InvalidateRect(cc->hwnd, NULL, FALSE);
         DisasmCtl_SetScrollPos(cc);
         return 0;
      case SB_PAGEDOWN:
         cc->addr += (cc->disinst(cc->addr, text) * DISASMLINES); // this should allow for different window sizes and fonts and shouldn't rely on fixed instruction sizes
         InvalidateRect(cc->hwnd, NULL, FALSE);
         DisasmCtl_SetScrollPos(cc);
         return 0;
      case SB_PAGEUP:
         cc->addr -= (cc->disinst(cc->addr, text) * DISASMLINES); // this should allow for different window sizes and fonts and shouldn't rely on fixed instruction sizes
         InvalidateRect(cc->hwnd, NULL, FALSE);
         DisasmCtl_SetScrollPos(cc);
         return 0;
      case SB_THUMBTRACK:
         cc->addr = HIWORD(wParam) << cc->scrollscale;
         InvalidateRect(cc->hwnd, NULL, FALSE);
         return 0;
      case SB_THUMBPOSITION:
         SetScrollPos(cc->hwnd, SB_VERT, HIWORD(wParam), TRUE);
         return 0;
      default:
         break;
   }
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT DisasmCtl_KeyDown(DisasmCtl_struct *cc, WPARAM wParam, LPARAM lParam)
{
   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

int DisasmInstructionNull(u32 addr, char *string)
{
   strcpy(string, " ");
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK DisasmCtl(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
{
   DisasmCtl_struct *cc=(DisasmCtl_struct *)GetWindowLong(hwnd, 0);

   switch(message)
   {
      case WM_NCCREATE:
      {
         LRESULT ret = InitCustomCtl(hwnd, wParam, lParam, sizeof(DisasmCtl_struct));
         if (ret != FALSE)
         {
            cc = (DisasmCtl_struct *)GetWindowLong(hwnd, 0);
            cc->addr = 0;
            cc->cursel = 0;
            cc->disinst = DisasmInstructionNull;
            SetScrollRange(hwnd, SB_VERT, 0, 65535, TRUE);
         }

         return ret;
      }
      case WM_NCDESTROY:
         DestroyCustomCtl((CustomCtl_struct *)cc);
         break;
      case WM_PAINT:
         return DisasmCtl_OnPaint(cc, wParam, lParam);
      case WM_ERASEBKGND:
         return TRUE;
      case WM_SETFONT:
         return CustomCtl_SetFont((CustomCtl_struct *)cc, wParam, lParam);
      case WM_SETFOCUS:
         CustomCtl_SetFocus((CustomCtl_struct *)cc);
         break;
      case WM_KILLFOCUS:
         CustomCtl_KillFocus((CustomCtl_struct *)cc);
         break;
      case WM_VSCROLL:
         return DisasmCtl_Vscroll(cc, wParam, lParam);
      case WM_LBUTTONDOWN:
         return 0;
      case WM_LBUTTONDBLCLK:
      {
         cc->cursel = HIWORD(lParam) / cc->fontmetric.tmHeight;
         PostMessage(GetParent(hwnd), WM_COMMAND, MAKEWPARAM(GetDlgCtrlID(hwnd), LBN_DBLCLK), (LPARAM)hwnd);
         return 0;
      }
      case WM_KEYDOWN:
         return DisasmCtl_KeyDown(cc, wParam, lParam);
      case DIS_SETDISFUNC:
         cc->disinst = (int (*)(u32, char *))lParam;
         return 0;
      case DIS_SETENDADDRESS:
      {
         int highestbit=0;
         int i;
         cc->e_addr = (u32)lParam;

         for (i = 0; i < 31; i++)
         {
            if (lParam & 0x1)
               highestbit = i;
            lParam >>= 1;
         }

         if (highestbit > 15)
            cc->scrollscale = highestbit - 15;
         else
            cc->scrollscale = 2;
         SetScrollRange(hwnd, SB_VERT, 0, cc->e_addr >> cc->scrollscale, TRUE);
         return 0;
      }
      case DIS_GOTOADDRESS:
         cc->addr = (u32)lParam;
         SetScrollPos(cc->hwnd, SB_VERT, cc->addr >> cc->scrollscale, TRUE);
         InvalidateRect(cc->hwnd, NULL, FALSE);
         SetFocus(cc->hwnd);
         return 0;
      case DIS_SETPC:
         cc->pc = (u32)lParam;
         return 0;
      case DIS_GETCURSEL:      
      {
         char text[MAX_PATH];
         u32 addr=cc->addr;
         int i;

         for (i = 0; i < cc->cursel; i++)
            addr += cc->disinst(addr, text);

         return addr;
      }
      case DIS_GETCURADDRESS:
         return cc->addr;
      default:
         break;
   }

   return DefWindowProc(hwnd, message, wParam, lParam);
}

//////////////////////////////////////////////////////////////////////////////

void InitDisasm()
{
   WNDCLASSEX wc;

   wc.cbSize         = sizeof(wc);
   wc.lpszClassName  = DISASM;
   wc.hInstance      = GetModuleHandle(0);
   wc.lpfnWndProc    = DisasmCtl;
   wc.hCursor        = LoadCursor(NULL, IDC_ARROW);
   wc.hIcon          = 0;
   wc.lpszMenuName   = 0;
   wc.hbrBackground  = (HBRUSH)GetSysColorBrush(COLOR_WINDOW);
   wc.style          = CS_DBLCLKS;
   wc.cbClsExtra     = 0;
   wc.cbWndExtra     = sizeof(DisasmCtl_struct *);
   wc.hIconSm        = 0;

   RegisterClassEx(&wc);
}

//////////////////////////////////////////////////////////////////////////////

