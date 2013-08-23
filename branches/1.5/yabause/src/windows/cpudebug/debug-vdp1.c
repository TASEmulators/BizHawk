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
#include "../resource.h"
#include "../settings/settings.h"
#include "../../vdp1.h"
#include "../../yabause.h"
#include "yuidebug.h"

u32 *vdp1texture=NULL;
int vdp1texturew, vdp1textureh;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK VDP1DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam)
{
   char tempstr[1024];
   TCHAR filename[MAX_PATH] = TEXT("\0");

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         char *string;
         u32 i=0;

         // execute yabause until vblank-out
         if (YabauseExec() != 0)
            return FALSE;

         // Build command list
         SendMessage(GetDlgItem(hDlg, IDC_VDP1CMDLB), LB_RESETCONTENT, 0, 0);

         for (;;)
         {
            if ((string = Vdp1DebugGetCommandNumberName(i)) == NULL)
               break;

            SendMessage(GetDlgItem(hDlg, IDC_VDP1CMDLB), LB_ADDSTRING, 0, (LPARAM) _16(string));

            i++;
         }

         vdp1texturew = vdp1textureh = 1;
         EnableWindow(GetDlgItem(hDlg, IDC_VDP1SAVEBMPBT), vdp1texture ? TRUE : FALSE);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_VDP1CMDLB:
            {
               switch(HIWORD(wParam))
               {
                  case LBN_SELCHANGE:
                  {
                     u32 cursel=0;

                     cursel = (u32)SendDlgItemMessage(hDlg, IDC_VDP1CMDLB, LB_GETCURSEL, 0, 0);

                     Vdp1DebugCommand(cursel, tempstr);
                     SetDlgItemText(hDlg, IDC_VDP1CMDET, _16(tempstr));

                     if (vdp1texture)
                        free(vdp1texture);

                     vdp1texture = Vdp1DebugTexture(cursel, &vdp1texturew, &vdp1textureh);
                     EnableWindow(GetDlgItem(hDlg, IDC_VDP1SAVEBMPBT), vdp1texture ? TRUE : FALSE);
                     InvalidateRect(hDlg, NULL, FALSE);
                     UpdateWindow(hDlg);

                     return TRUE;
                  }
                  default: break;
               }
               return TRUE;
            }
            case IDC_VDP1SAVEBMPBT:
            {
               OPENFILENAME ofn;
               WCHAR filter[1024];

               CreateFilter(filter, 1024,
                  "Bitmap Files", "*.BMP",
                  "All files (*.*)", "*.*", NULL);

               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, filename, sizeof(filename)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("BMP");

               if (vdp1texture && GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, filename, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  SaveBitmap(tempstr, vdp1texturew, vdp1textureh, vdp1texture);
               }

               return TRUE;
            }
            case IDOK:
            {
               EndDialog(hDlg, TRUE);

               return TRUE;
            }

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

         hdc = BeginPaint(GetDlgItem(hDlg, IDC_VDP1TEXTET), &ps);
         GetClientRect(GetDlgItem(hDlg, IDC_VDP1TEXTET), &rect);
         FillRect(hdc, &rect, (HBRUSH)GetStockObject(BLACK_BRUSH));

         if (vdp1texture == NULL || vdp1texturew == 0 || vdp1textureh == 0)
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
            bmi.bV4V4Compression = BI_RGB | BI_BITFIELDS; // double-check this
            bmi.bV4RedMask = 0x000000FF;
            bmi.bV4GreenMask = 0x0000FF00;
            bmi.bV4BlueMask = 0x00FF0000;
            bmi.bV4AlphaMask = 0xFF000000;
            bmi.bV4Width = vdp1texturew;
            bmi.bV4Height = -vdp1textureh;

            // Let's try to maintain a correct ratio
            if (vdp1texturew > vdp1textureh)
            {
               outw = rect.right;
               outh = rect.bottom * vdp1textureh / vdp1texturew;
            }
            else
            {
               outw = rect.right * vdp1texturew / vdp1textureh;
               outh = rect.bottom;
            }
   
            StretchDIBits(hdc, 0, 0, outw, outh, 0, 0, vdp1texturew, vdp1textureh, vdp1texture, (BITMAPINFO *)&bmi, DIB_RGB_COLORS, SRCCOPY);
         }
         EndPaint(GetDlgItem(hDlg, IDC_VDP1TEXTET), &ps);
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

