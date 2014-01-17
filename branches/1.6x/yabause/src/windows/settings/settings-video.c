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
#include "../../vidogl.h"
#include "../../vdp1.h"
#include "../../vdp2.h"

extern VideoInterface_struct *VIDCoreList[];

char vidcoretype=VIDCORE_OGL;
int enableautofskip=0;
int usefullscreenonstartup=0;
int fullscreenwidth=640;
int fullscreenheight=480;
int usecustomwindowsize=0;
int windowwidth=320;
int windowheight=224;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK VideoSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         char tempstr[MAX_PATH];
         DEVMODE dmSettings;
         int i;

         // Setup Video Core Combo box
         SendDlgItemMessage(hDlg, IDC_VIDEOCORECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_VIDEOCORECB, CB_ADDSTRING, 0, (LPARAM)_16("None"));

         for (i = 1; VIDCoreList[i] != NULL; i++)
            SendDlgItemMessage(hDlg, IDC_VIDEOCORECB, CB_ADDSTRING, 0, (LPARAM)_16(VIDCoreList[i]->Name));

         // Set Selected Video Core
         for (i = 0; VIDCoreList[i] != NULL; i++)
         {
            if (vidcoretype == VIDCoreList[i]->id)
               SendDlgItemMessage(hDlg, IDC_VIDEOCORECB, CB_SETCURSEL, i, 0);
         }

         // Setup Auto Frameskip checkbox
         if (enableautofskip)
            SendDlgItemMessage(hDlg, IDC_AUTOFRAMESKIPCB, BM_SETCHECK, BST_CHECKED, 0);
         else
            SendDlgItemMessage(hDlg, IDC_AUTOFRAMESKIPCB, BM_SETCHECK, BST_UNCHECKED, 0);

         // Setup Fullscreen on Startup checkbox
         if (usefullscreenonstartup)
            SendDlgItemMessage(hDlg, IDC_FULLSCREENSTARTUPCB, BM_SETCHECK, BST_CHECKED, 0);
         else
            SendDlgItemMessage(hDlg, IDC_FULLSCREENSTARTUPCB, BM_SETCHECK, BST_UNCHECKED, 0);

         // Setup FullScreen width/height settings
         SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_RESETCONTENT, 0, 0);

         for (i = 0;; i++)
         {
            if (EnumDisplaySettings(NULL, i, &dmSettings) == FALSE)
               break;
            if (dmSettings.dmBitsPerPel == 32)
            {
               int index;

               sprintf(tempstr, "%dx%d", (int)dmSettings.dmPelsWidth, (int)dmSettings.dmPelsHeight);
               if (SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_FINDSTRINGEXACT, 0, (LPARAM)_16(tempstr)) == CB_ERR)
               {
                  index = (int)SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_ADDSTRING, 0, (LPARAM)_16(tempstr));

                  if (dmSettings.dmPelsWidth == fullscreenwidth &&
                      dmSettings.dmPelsHeight == fullscreenheight)
                     SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_SETCURSEL, index, 0);
               }
            }
         }

         // Setup use custom window size
         if (usecustomwindowsize)
         {
            SendDlgItemMessage(hDlg, IDC_CUSTOMWINDOWCB, BM_SETCHECK, BST_CHECKED, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_WIDTHEDIT), TRUE);
            EnableWindow(GetDlgItem(hDlg, IDC_HEIGHTEDIT), TRUE);
         }
         else
            SendDlgItemMessage(hDlg, IDC_CUSTOMWINDOWCB, BM_SETCHECK, BST_UNCHECKED, 0);

         // Setup window width and height
         sprintf(tempstr, "%d", windowwidth);
         _16(tempstr);
         SetDlgItemText(hDlg, IDC_WIDTHEDIT, _16(tempstr));
         sprintf(tempstr, "%d", windowheight);
         SetDlgItemText(hDlg, IDC_HEIGHTEDIT, _16(tempstr));

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_CUSTOMWINDOWCB:
            {
               if (SendDlgItemMessage(hDlg, IDC_CUSTOMWINDOWCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_WIDTHEDIT), TRUE);
                  EnableWindow(GetDlgItem(hDlg, IDC_HEIGHTEDIT), TRUE);
               }
               else
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_WIDTHEDIT), FALSE);
                  EnableWindow(GetDlgItem(hDlg, IDC_HEIGHTEDIT), FALSE);
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
               WCHAR tempwstr[MAX_PATH];
               char tempstr[MAX_PATH];
               int cursel;
               int newvidcoretype;
               BOOL vidcorechanged=FALSE;

               // Write Video core type
               newvidcoretype = VIDCoreList[SendDlgItemMessage(hDlg, IDC_VIDEOCORECB, CB_GETCURSEL, 0, 0)]->id;
               if (newvidcoretype != vidcoretype)
               {
                  vidcoretype = newvidcoretype;
                  vidcorechanged = TRUE;
                  sprintf(tempstr, "%d", vidcoretype);
                  WritePrivateProfileStringA("Video", "VideoCore", tempstr, inifilename);
               }

               if (SendDlgItemMessage(hDlg, IDC_AUTOFRAMESKIPCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  EnableAutoFrameSkip();
                  enableautofskip = 1;
               }
               else
               {
                  DisableAutoFrameSkip();
                  enableautofskip = 0;
               }

               // Write Auto frameskip
               sprintf(tempstr, "%d", enableautofskip);
               WritePrivateProfileStringA("Video", "AutoFrameSkip", tempstr, inifilename);

               // Write full screen on startup setting
               if (SendDlgItemMessage(hDlg, IDC_FULLSCREENSTARTUPCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  usefullscreenonstartup = 1;
                  WritePrivateProfileStringA("Video", "UseFullScreenOnStartup", "1", inifilename);
               }
               else
               {
                  usefullscreenonstartup = 0;
                  WritePrivateProfileStringA("Video", "UseFullScreenOnStartup", "0", inifilename);
               }

               // Write full screen size settings
               cursel = (int)SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_GETCURSEL, 0, 0);
               if (SendDlgItemMessage(hDlg, IDC_FSSIZECB, CB_GETLBTEXTLEN, cursel, 0) <= MAX_PATH)
               {
                  SendDlgItemMessageA(hDlg, IDC_FSSIZECB, CB_GETLBTEXT, cursel, (LPARAM)tempstr);
                  sscanf(tempstr, "%dx%d", &fullscreenwidth, &fullscreenheight);
               }

               sprintf(tempstr, "%d", fullscreenwidth);
               WritePrivateProfileStringA("Video", "FullScreenWidth", tempstr, inifilename);
               sprintf(tempstr, "%d", fullscreenheight);
               WritePrivateProfileStringA("Video", "FullScreenHeight", tempstr, inifilename);

               // Write use custom window size setting
               if (SendDlgItemMessage(hDlg, IDC_CUSTOMWINDOWCB, BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  usecustomwindowsize = 1;
                  WritePrivateProfileStringA("Video", "UseCustomWindowSize", "1", inifilename);
               }
               else
               {
                  usecustomwindowsize = 0;
                  WritePrivateProfileStringA("Video", "UseCustomWindowSize", "0", inifilename);
               }
               
               // Write window width and height settings
               GetDlgItemText(hDlg, IDC_WIDTHEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);
               windowwidth = atoi(tempstr);
               WritePrivateProfileStringA("Video", "WindowWidth", tempstr, inifilename);
               GetDlgItemText(hDlg, IDC_HEIGHTEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);
               windowheight = atoi(tempstr);
               WritePrivateProfileStringA("Video", "WindowHeight", tempstr, inifilename);

               // Re-initialize Video
#ifdef USETHREADS
               if (nocorechange == 0)
               {
                  corechanged = 0;
                  changecore |= 2;
                  while (corechanged == 0) { Sleep(0); }
               }
#else
               if (vidcorechanged && nocorechange == 0)
                  VideoChangeCore(vidcoretype);

               if (VIDCore && !VIDCore->IsFullscreen() && usecustomwindowsize)
                  VIDCore->Resize(windowwidth, windowheight, 0);

#endif
               SetWindowLong(hDlg, DWL_MSGRESULT, PSNRET_NOERROR);
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
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
