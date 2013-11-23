/*  Copyright 2004-2009 Theo Berkau

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
#include "settings.h"

char logfilename[MAX_PATH];
char mini18nlogfilename[MAX_PATH];
int uselog=0;
int usemini18nlog=0;
int logtype=0;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK LogSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         // Setup use log setting
         SendDlgItemMessage(hDlg, IDC_USELOGCB, BM_SETCHECK, uselog ? BST_CHECKED : BST_UNCHECKED, 0);
         SendMessage(hDlg, WM_COMMAND, IDC_USELOGCB, 0);

         // Setup log type setting
         SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Write to File"));
         SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Write to Window"));
         SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_SETCURSEL, logtype, 0);

         // Setup log filename setting
         SetDlgItemText(hDlg, IDC_LOGFILENAMEET, _16(logfilename));

         // mini18n log settings
         SendDlgItemMessage(hDlg, IDC_USEMINI18NLOG, BM_SETCHECK, usemini18nlog ? BST_CHECKED : BST_UNCHECKED, 0);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_USELOGCB:
            {
               if (SendDlgItemMessage(hDlg, LOWORD(wParam), BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  SendMessage(hDlg, WM_COMMAND, (CBN_SELCHANGE << 16) | IDC_LOGTYPECB, 0);
                  EnableWindow(GetDlgItem(hDlg, IDC_LOGTYPECB), TRUE);
               }
               else
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_LOGTYPECB), FALSE);
                  EnableWindow(GetDlgItem(hDlg, IDC_LOGFILENAMEET), FALSE);
                  EnableWindow(GetDlgItem(hDlg, IDC_LOGBROWSEBT), FALSE);
               }

               return TRUE;
            }
            case IDC_LOGTYPECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     if (SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_GETCURSEL, 0, 0) == 0)
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_LOGFILENAMEET), TRUE);
                        EnableWindow(GetDlgItem(hDlg, IDC_LOGBROWSEBT), TRUE);
                     }
                     else
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_LOGFILENAMEET), FALSE);
                        EnableWindow(GetDlgItem(hDlg, IDC_LOGBROWSEBT), FALSE);
                     }

                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_LOGBROWSEBT:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;

               // setup ofn structure
               ZeroMemory(&ofn, sizeof(ofn));
               ofn.lStructSize = sizeof(ofn);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Text Files", "*.txt",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               ofn.nFilterIndex = 1;
               GetDlgItemText(hDlg, IDC_LOGFILENAMEET, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr)/sizeof(WCHAR);
               ofn.Flags = OFN_OVERWRITEPROMPT;
               ofn.lpstrDefExt = _16("TXT");

               if (GetSaveFileName(&ofn))
               {
                  SetDlgItemText(hDlg, IDC_LOGFILENAMEET, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, logfilename, MAX_PATH, NULL, NULL);
               }

               return TRUE;
            }
            case IDC_USEMINI18NLOG:
            {
               BOOL enabled;

               enabled = SendDlgItemMessage(hDlg, LOWORD(wParam), BM_GETCHECK, 0, 0) == BST_CHECKED ? TRUE : FALSE;

               EnableWindow(GetDlgItem(hDlg, IDC_MINI18NLOGFILENAME), enabled);
               EnableWindow(GetDlgItem(hDlg, IDC_MINI18NLOGBROWSE), enabled);

               return TRUE;
            }
            case IDC_MINI18NLOGBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;

               // setup ofn structure
               ZeroMemory(&ofn, sizeof(ofn));
               ofn.lStructSize = sizeof(ofn);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Yabause Translation Files", "*.YTS",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               ofn.nFilterIndex = 1;
               GetDlgItemText(hDlg, IDC_MINI18NLOGFILENAME, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr)/sizeof(WCHAR);
               ofn.Flags = OFN_OVERWRITEPROMPT;
               ofn.lpstrDefExt = _16("YTS");

               if (GetSaveFileName(&ofn))
               {
                  SetDlgItemText(hDlg, IDC_MINI18NLOGFILENAME, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, logfilename, MAX_PATH, NULL, NULL);
               }

               return TRUE;
            }
            default: break;
         }

         break;
      }
      case WM_NOTIFY:
     		switch (((NMHDR *) lParam)->code) 
    		{
				case PSN_SETACTIVE:
					break;

				case PSN_APPLY:
            {
               char tempstr[MAX_PATH];

               // Write use log setting
               if (SendDlgItemMessage(hDlg, IDC_USELOGCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  uselog = 1;
               else
                  uselog = 0;

               sprintf(tempstr, "%d", uselog);
               WritePrivateProfileStringA("Log", "Enable", tempstr, inifilename);

               // Write log type setting
               logtype = (int)SendDlgItemMessage(hDlg, IDC_LOGTYPECB, CB_GETCURSEL, 0, 0);
               sprintf(tempstr, "%d", logtype);
               WritePrivateProfileStringA("Log", "Type", tempstr, inifilename);

               // Write log filename
               WritePrivateProfileStringA("Log", "Filename", logfilename, inifilename);

               // Write use mini18n log setting
               if (SendDlgItemMessage(hDlg, IDC_USELOGCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  usemini18nlog = 1;
               else
                  usemini18nlog = 0;

               sprintf(tempstr, "%d", usemini18nlog);
               WritePrivateProfileStringA("Mini18nLog", "Enable", tempstr, inifilename);

               // Write mini18n log filename
               WritePrivateProfileStringA("Mini18nLog", "Filename", mini18nlogfilename, inifilename);

          		SetWindowLong(hDlg,	DWL_MSGRESULT, PSNRET_NOERROR);
					break;
            }
				case PSN_KILLACTIVE:
	        		SetWindowLong(hDlg,	DWL_MSGRESULT, FALSE);
   				return 1;
					break;
				case PSN_RESET:
					break;
        	}
         break;
      case WM_DESTROY:
      {
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
