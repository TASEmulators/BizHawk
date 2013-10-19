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
#include <stdio.h>
#include <tchar.h>
#undef FASTCALL
#include "../../scsp.h"
#include "../resource.h"
#include "../settings/settings.h"
#include "yuidebug.h"

LRESULT CALLBACK SCSPDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   char tempstr[2048];
   TCHAR tempstr2[MAX_PATH];
   int i;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_RESETCONTENT, 0, 0);

         for (i = 0; i < 32; i++)
         {
            sprintf(tempstr, "%d", i);
            SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_ADDSTRING, 0, (LPARAM) _16(tempstr));
         }

         SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_SETCURSEL, 0, 0);

         // Setup Slot Info
         ScspSlotDebugStats(0, tempstr);
         SetDlgItemText(hDlg, IDC_SCSPSLOTET, _16(tempstr));

         // Setup Common Control registers
         ScspCommonControlRegisterDebugStats(tempstr);
         SetDlgItemText(hDlg, IDC_SCSPCOMMONREGET, _16(tempstr));

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_SCSPSLOTCB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     u8 cursel=0;

                     // Update Sound Slot Info
                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_GETCURSEL, 0, 0);

                     ScspSlotDebugStats(cursel, tempstr);
                     SetDlgItemText(hDlg, IDC_SCSPSLOTET, _16(tempstr));

                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_SCSPSLOTSAVE:
            {
               OPENFILENAME ofn;
               u8 cursel=0;
               WCHAR filter[1024];

               cursel = (u8)SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_GETCURSEL, 0, 0);
               _stprintf(tempstr2, TEXT("channel%02d.wav"), cursel);

               CreateFilter(filter, 1024,
                  "WAV Files", "*.WAV",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, tempstr2, sizeof(tempstr2)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("WAV");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, tempstr2, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  ScspSlotDebugAudioSaveWav(cursel, tempstr);
               }

               return TRUE;
            }
            case IDC_SCSPSLOTREGSAVE:
            {
               OPENFILENAME ofn;
               u8 cursel=0;
               WCHAR filter[1024];

               cursel = (u8)SendDlgItemMessage(hDlg, IDC_SCSPSLOTCB, CB_GETCURSEL, 0, 0);
               _stprintf(tempstr2, TEXT("channel%02dregs.bin"), cursel);

               CreateFilter(filter, 1024,
                  "Binary Files", "*.BIN",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, tempstr2, sizeof(tempstr2)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("BIN");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, tempstr2, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  ScspSlotDebugSaveRegisters(cursel, tempstr);
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

