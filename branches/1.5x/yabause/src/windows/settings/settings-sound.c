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
#include "../../scsp.h"
#include "settings.h"
#include "../../snddx.h"

extern SoundInterface_struct *SNDCoreList[];

char sndcoretype=SNDCORE_DIRECTX;
int sndvolume=100;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK SoundSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         int i;

         // Setup Sound Core Combo box
         SendDlgItemMessage(hDlg, IDC_SOUNDCORECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_SOUNDCORECB, CB_ADDSTRING, 0, (LPARAM)_16("None"));

         for (i = 1; SNDCoreList[i] != NULL; i++)
            SendDlgItemMessage(hDlg, IDC_SOUNDCORECB, CB_ADDSTRING, 0, (LPARAM)_16(SNDCoreList[i]->Name));

         // Set Selected Sound Core
         for (i = 0; SNDCoreList[i] != NULL; i++)
         {
            if (sndcoretype == SNDCoreList[i]->id)
               SendDlgItemMessage(hDlg, IDC_SOUNDCORECB, CB_SETCURSEL, i, 0);
         }

         // Setup Volume Slider
         SendDlgItemMessage(hDlg, IDC_SLVOLUME, TBM_SETRANGE, 0, MAKELONG(0, 100));

         // Set Selected Volume
         SendDlgItemMessage(hDlg, IDC_SLVOLUME, TBM_SETPOS, TRUE, sndvolume);

         return TRUE;
      }
      case WM_NOTIFY:
     		switch (((NMHDR *) lParam)->code) 
    		{
				case PSN_SETACTIVE:
					break;

				case PSN_APPLY:
            {
               char tempstr[MAX_PATH];
               int newsndcoretype;
               BOOL sndcorechanged=FALSE;

               // Write Sound core type
               newsndcoretype = SNDCoreList[SendDlgItemMessage(hDlg, IDC_SOUNDCORECB, CB_GETCURSEL, 0, 0)]->id;
               if (newsndcoretype != sndcoretype)
               {
                  sndcoretype = newsndcoretype;
                  sndcorechanged = TRUE;
                  sprintf(tempstr, "%d", sndcoretype);
                  WritePrivateProfileStringA("Sound", "SoundCore", tempstr, inifilename);
               }

               // Write Volume
               sndvolume = (int)SendDlgItemMessage(hDlg, IDC_SLVOLUME, TBM_GETPOS, 0, 0);
               sprintf(tempstr, "%d", sndvolume);
               WritePrivateProfileStringA("Sound", "Volume", tempstr, inifilename);
               if (sndcorechanged && nocorechange == 0)
               {
#ifndef USETHREADS
                  ScspChangeSoundCore(sndcoretype);
#else
                  corechanged = 0;
                  changecore |= 4;
                  while (corechanged == 0) { Sleep(0); }
#endif
               }

               ScspSetVolume(sndvolume);
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

