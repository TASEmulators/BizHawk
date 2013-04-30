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
#include <commctrl.h>
#undef FASTCALL
#include "../resource.h"
#include "settings.h"

DWORD netlinklocalremoteip=MAKEIPADDRESS(127, 0, 0, 1);
int netlinkport=7845;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK NetlinkSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                        LPARAM lParam)
{
   WCHAR tempwstr[MAX_PATH];
   char tempstr[MAX_PATH];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         SendDlgItemMessage(hDlg, IDC_LOCALREMOTEIP, IPM_SETADDRESS, 0, netlinklocalremoteip);

         sprintf(tempstr, "%d", netlinkport);
         SetDlgItemText(hDlg, IDC_PORTET, _16(tempstr));

         return TRUE;
      }
      case WM_NOTIFY:
     		switch (((NMHDR *) lParam)->code) 
    		{
				case PSN_SETACTIVE:
					break;

				case PSN_APPLY:
               // Local/Remote IP
               SendDlgItemMessage(hDlg, IDC_LOCALREMOTEIP, IPM_GETADDRESS, 0, (LPARAM)&netlinklocalremoteip);
               sprintf(tempstr, "%d.%d.%d.%d", (int)FIRST_IPADDRESS(netlinklocalremoteip), (int)SECOND_IPADDRESS(netlinklocalremoteip), (int)THIRD_IPADDRESS(netlinklocalremoteip), (int)FOURTH_IPADDRESS(netlinklocalremoteip));
               WritePrivateProfileStringA("Netlink", "LocalRemoteIP", tempstr, inifilename);

               // Port Number
               GetDlgItemText(hDlg, IDC_PORTET, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);
               netlinkport = atoi(tempstr);
               WritePrivateProfileStringA("Netlink", "Port", tempstr, inifilename);

          		SetWindowLong(hDlg,	DWL_MSGRESULT, PSNRET_NOERROR);
					break;

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
