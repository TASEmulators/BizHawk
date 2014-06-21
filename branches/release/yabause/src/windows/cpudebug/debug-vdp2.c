/*  Copyright 2004 Guillaume Duhamel
    Copyright 2004-2008 Theo Berkau
    Copyright 2005 Joost Peters

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
#undef FASTCALL
#include "../resource.h"
#include "../../core.h"
#include "../../vdp2debug.h"
#include "../settings/settings.h"
#include "yuidebug.h"
#include "../yuiwin.h"

LRESULT CALLBACK VDP2ViewerDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam)
{
   static u32 *vdp2texture;
   static int width;
   static int height;
   TCHAR filename[MAX_PATH] = TEXT("\0");
   char tempstr[MAX_PATH];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         vdp2texture = NULL;

         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_ADDSTRING, 0, (LPARAM)_16("NBG0/RBG1"));
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_ADDSTRING, 0, (LPARAM)_16("NBG1"));
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_ADDSTRING, 0, (LPARAM)_16("NBG2"));
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_ADDSTRING, 0, (LPARAM)_16("NBG3"));
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_ADDSTRING, 0, (LPARAM)_16("RBG0"));
         SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_SETCURSEL, 0, 0);

         vdp2texture = Vdp2DebugTexture(0, &width, &height);
         EnableWindow(GetDlgItem(hDlg, IDC_VDP2SAVEBMPBT), vdp2texture ? TRUE : FALSE);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_VDP2SCREENCB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     u8 cursel = (u8)SendDlgItemMessage(hDlg, IDC_VDP2SCREENCB, CB_GETCURSEL, 0, 0);

                     if (vdp2texture)
                        free(vdp2texture);

                     vdp2texture = Vdp2DebugTexture(cursel, &width, &height);
                     EnableWindow(GetDlgItem(hDlg, IDC_VDP2SAVEBMPBT), vdp2texture ? TRUE : FALSE);

                     InvalidateRect(hDlg, NULL, FALSE);
                     UpdateWindow(hDlg);
                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_VDP2SAVEBMPBT:
            {
               OPENFILENAME ofn;
               WCHAR filter[1024];

               CreateFilter(filter, 1024,
                  "Bitmap Files", "*.BMP",
                  "All files (*.*)", "*.*", NULL);

               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, filename, sizeof(filename)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("BMP");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, filename, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  SaveBitmap(tempstr, width, height, vdp2texture);
               }

               return TRUE;
            }
            case IDOK:
               EndDialog(hDlg, TRUE);
               return TRUE;
            default: break;
         }
         break;
      }
      case WM_PAINT:
      {
         // Draw our texture box
         PAINTSTRUCT ps;
         HDC hdc;
         BITMAPV4HEADER bmi;
         int outw, outh;
         RECT rect;

         hdc = BeginPaint(GetDlgItem(hDlg, IDC_VDP2TEXTET), &ps);
         GetClientRect(GetDlgItem(hDlg, IDC_VDP2TEXTET), &rect);
         FillRect(hdc, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

         if (vdp2texture == NULL)
         {
            SetBkColor(hdc, RGB(0,0,0));
            SetTextColor(hdc, RGB(255,255,255));
            TextOut(hdc, 0, 0, _16("Not Available"), 13);
         }
         else
         {
            memset(&bmi, 0, sizeof(bmi));
            bmi.bV4Size = sizeof(bmi);
            bmi.bV4Planes = 1;
            bmi.bV4BitCount = 32;
            bmi.bV4V4Compression = BI_RGB | BI_BITFIELDS;
            bmi.bV4RedMask = 0x000000FF;
            bmi.bV4GreenMask = 0x0000FF00;
            bmi.bV4BlueMask = 0x00FF0000;
            bmi.bV4AlphaMask = 0xFF000000;
            bmi.bV4Width = width;
            bmi.bV4Height = -height;

            // Let's try to maintain a correct ratio
            if (width > height)
            {
               outw = rect.right;
               outh = rect.bottom * height / width;
            }
            else
            {
               outw = rect.right * width / height;
               outh = rect.bottom;
            }
   
            StretchDIBits(hdc, 0, 0, outw, outh, 0, 0, width, height, vdp2texture, (BITMAPINFO *)&bmi, DIB_RGB_COLORS, SRCCOPY);
         }
         EndPaint(GetDlgItem(hDlg, IDC_VDP2TEXTET), &ps);
         break;
      }
      case WM_CLOSE:
         EndDialog(hDlg, TRUE);

         return TRUE;
      case WM_DESTROY:
      {
         if (vdp2texture)
            free(vdp2texture);
         return TRUE;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK VDP2DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         char tempstr[VDP2_DEBUG_STRING_SIZE];
         int isscrenabled;

         // is NBG0/RBG1 enabled?
         Vdp2DebugStatsNBG0(tempstr, &isscrenabled);

         if (isscrenabled)
         {
            SendMessage(GetDlgItem(hDlg, IDC_NBG0ENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_NBG0ET, _16(tempstr));
         }
         else
            SendMessage(GetDlgItem(hDlg, IDC_NBG0ENABCB), BM_SETCHECK, BST_UNCHECKED, 0);

         Vdp2DebugStatsNBG1(tempstr, &isscrenabled);

         // is NBG1 enabled?
         if (isscrenabled)
         {
            // enabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG1ENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_NBG1ET, _16(tempstr));
         }
         else
            // disabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG1ENABCB), BM_SETCHECK, BST_UNCHECKED, 0);

         Vdp2DebugStatsNBG2(tempstr, &isscrenabled);

         // is NBG2 enabled?
         if (isscrenabled)
         {
            // enabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG2ENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_NBG2ET, _16(tempstr));
         }
         else
            // disabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG2ENABCB), BM_SETCHECK, BST_UNCHECKED, 0);

         Vdp2DebugStatsNBG3(tempstr, &isscrenabled);

         // is NBG3 enabled?
         if (isscrenabled)
         {
            // enabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG3ENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_NBG3ET, _16(tempstr));
         }
         else
            // disabled
            SendMessage(GetDlgItem(hDlg, IDC_NBG3ENABCB), BM_SETCHECK, BST_UNCHECKED, 0);

         Vdp2DebugStatsRBG0(tempstr, &isscrenabled);

         // is RBG0 enabled?
         if (isscrenabled)
         {
            // enabled
            SendMessage(GetDlgItem(hDlg, IDC_RBG0ENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_RBG0ET, _16(tempstr));
         }
         else
            // disabled
            SendMessage(GetDlgItem(hDlg, IDC_RBG0ENABCB), BM_SETCHECK, BST_UNCHECKED, 0);

         Vdp2DebugStatsGeneral(tempstr, &isscrenabled);

         if (isscrenabled)
         {
            // enabled
            SendMessage(GetDlgItem(hDlg, IDC_DISPENABCB), BM_SETCHECK, BST_CHECKED, 0);
            SetDlgItemText(hDlg, IDC_VDP2GENET, _16(tempstr));
         }
         else
            // disabled
            SendMessage(GetDlgItem(hDlg, IDC_DISPENABCB), BM_SETCHECK, BST_UNCHECKED, 0);


         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
               EndDialog(hDlg, TRUE);
               return TRUE;
            case IDC_VDP2VIEWER:
            {
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_VDP2VIEWER), NULL, (DLGPROC)VDP2ViewerDlgProc);
               return TRUE;
            }
            default: break;
         }
         break;
      }
      case WM_CLOSE:
      {
         EndDialog(hDlg, TRUE);

         return TRUE;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

