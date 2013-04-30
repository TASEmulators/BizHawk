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
#include <windowsx.h>
#include <commctrl.h>
#undef FASTCALL
#include "../../perdx.h"
#include "../resource.h"
#include "settings.h"

extern HINSTANCE y_hInstance;

char percoretype=PERCORE_DIRECTX;
int configpadnum=0;

LRESULT CALLBACK PadConfigDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);

LRESULT CALLBACK MouseConfigDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam);

//////////////////////////////////////////////////////////////////////////////

int ConvertEmulateTypeSelStringToID(HWND hDlg, int id)
{
   WCHAR wtext[MAX_PATH];
   char text[MAX_PATH];

   if (!IsWindowEnabled(GetDlgItem(hDlg, id)))
      return EMUTYPE_NONE;

   ComboBox_GetLBText(GetDlgItem(hDlg, id),
                      ComboBox_GetCurSel(GetDlgItem(hDlg, id)),
                      wtext);
   WideCharToMultiByte(CP_ACP, 0, wtext, -1, text, MAX_PATH, NULL, NULL);

   if (strcmp(text, "Standard Pad") == 0)
      return EMUTYPE_STANDARDPAD;
   else if (strcmp(text, "Analog Pad") == 0)
      return EMUTYPE_ANALOGPAD;
   else if (strcmp(text, "Stunner") == 0)
      return EMUTYPE_STUNNER;
   else if (strcmp(text, "Mouse") == 0)
      return EMUTYPE_MOUSE;
   else if (strcmp(text, "Keyboard") == 0)
      return EMUTYPE_KEYBOARD;
   else
      return EMUTYPE_NONE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK InputSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam)
{
   static helpballoon_struct hb[26];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         int i;
         u32 j;

         for (i = 0; i < 2; i++)
         {
            SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB+i, CB_RESETCONTENT, 0, 0);
            SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("None"));
            SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Direct"));
            SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("6-port Multitap"));
         }

         for (i = 0; i < 6; i++)
         {
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_RESETCONTENT, 0, 0);
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("None"));
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Standard Pad"));
/*
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Analog Pad"));
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Stunner"));
*/
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Mouse"));
/*
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Keyboard"));
*/
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_RESETCONTENT, 0, 0);
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("None"));
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Standard Pad"));
/*
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Analog Pad"));
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Stunner"));
*/
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Mouse"));
/*
            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_ADDSTRING, 0, (LPARAM)_16("Keyboard"));
*/
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+i, CB_SETCURSEL, 0, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_PORT1ATYPECB+i), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_PORT1ACFGPB+i), FALSE);

            SendDlgItemMessage(hDlg, IDC_PORT2ATYPECB+i, CB_SETCURSEL, 0, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_PORT2ATYPECB+i), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_PORT2ACFGPB+i), FALSE);
         }

         SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB, CB_SETCURSEL, porttype[0], 0);
         PostMessage(hDlg, WM_COMMAND, MAKEWPARAM(IDC_PORT1CONNTYPECB, CBN_SELCHANGE), (LPARAM)GetDlgItem(hDlg, IDC_PORT1CONNTYPECB));

         SendDlgItemMessage(hDlg, IDC_PORT2CONNTYPECB, CB_SETCURSEL, porttype[1], 0);
         PostMessage(hDlg, WM_COMMAND, MAKEWPARAM(IDC_PORT2CONNTYPECB, CBN_SELCHANGE), (LPARAM)GetDlgItem(hDlg, IDC_PORT2CONNTYPECB));

         EnableWindow(GetDlgItem(hDlg, IDC_PORT1ATYPECB), TRUE);
         EnableWindow(GetDlgItem(hDlg, IDC_PORT2ATYPECB), TRUE);

         if (!PERCore)
         {
            SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB, CB_SETCURSEL, 1, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_PORT1ACFGPB), TRUE);            
         }

         // Go through previous settings and figure out which controls to 
         // enable, etc.
         for (j = 0; j < numpads; j++)
         {
            if (paddevice[j].emulatetype != 0)
            {
               int id=0;
               switch (pad[j]->perid)
               {
                  case PERPAD:
                     id = 1;
                     break;
                  case PERMOUSE:
                     id = 2;
                     break;
                  default: break;
               }

               SendDlgItemMessage(hDlg, IDC_PORT1ATYPECB+j, CB_SETCURSEL, id, 0);
               EnableWindow(GetDlgItem(hDlg, IDC_PORT1ACFGPB+j), TRUE);            
            }
         }

         // Setup Tooltips
         hb[0].string = "Use this to select whether to use a multi-tap or direct connection";
         hb[0].hParent = GetDlgItem(hDlg, IDC_PORT1CONNTYPECB);
         hb[1].string = hb[0].string;
         hb[1].hParent = GetDlgItem(hDlg, IDC_PORT2CONNTYPECB);

         for (i = 0; i < 6; i++)
         {
            hb[2+i].string = "Use this to select what kind of peripheral to emulate";
            hb[2+i].hParent = GetDlgItem(hDlg, IDC_PORT1ATYPECB+i);
            hb[8+i].string = hb[2+i].string;
            hb[8+i].hParent = GetDlgItem(hDlg, IDC_PORT2ATYPECB+i);            

            hb[14+i].string = "Press this to change the button configuration, etc.";
            hb[14+i].hParent = GetDlgItem(hDlg, IDC_PORT1ACFGPB+i);
            hb[20+i].string = hb[14+i].string;
            hb[20+i].hParent = GetDlgItem(hDlg, IDC_PORT2ACFGPB+i);            
         }
         
         hb[26].string = NULL;

         CreateHelpBalloons(hb);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_PORT1CONNTYPECB:
            case IDC_PORT2CONNTYPECB:
            {
               int i, id, id2;

               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                     switch(ComboBox_GetCurSel((HWND)lParam))
                     {
                        case 0: // None
                           if (LOWORD(wParam) == IDC_PORT1CONNTYPECB)
                           {
                              id = IDC_PORT1ATYPECB;
                              id2 = IDC_PORT1ACFGPB;
                           }
                           else
                           {
                              id = IDC_PORT2ATYPECB;
                              id2 = IDC_PORT2ACFGPB;
                           }

                           for (i = 0; i < 6; i++)
                           {
                              EnableWindow(GetDlgItem(hDlg, id+i), FALSE);
                              EnableWindow(GetDlgItem(hDlg, id2+i), FALSE);
                           }
                           break;
                        case 1: // Direct
                           if (LOWORD(wParam) == IDC_PORT1CONNTYPECB)
                           {
                              id = IDC_PORT1ATYPECB;
                              id2 = IDC_PORT1ACFGPB;
                           }
                           else
                           {
                              id = IDC_PORT2ATYPECB;
                              id2 = IDC_PORT2ACFGPB;
                           }

                           EnableWindow(GetDlgItem(hDlg, id), TRUE);
                           PostMessage(hDlg, WM_COMMAND, MAKEWPARAM(id, CBN_SELCHANGE), (LPARAM)GetDlgItem(hDlg, id));

                           for (i = 1; i < 6; i++)
                           {
                              EnableWindow(GetDlgItem(hDlg, id+i), FALSE);
                              EnableWindow(GetDlgItem(hDlg, id2+i), FALSE);
                           }
                           break;
                        case 2: // Multi-tap
                           id = (LOWORD(wParam) == IDC_PORT1CONNTYPECB ? IDC_PORT1ATYPECB : IDC_PORT2ATYPECB);

                           for (i = 0; i < 6; i++)
                           {
                              EnableWindow(GetDlgItem(hDlg, id+i), TRUE);
                              PostMessage(hDlg, WM_COMMAND, MAKEWPARAM(id+i, CBN_SELCHANGE), (LPARAM)GetDlgItem(hDlg, id+i));
                           }
                           break;
                        default: break;
                     }

                     break;
                  default: break;
               }
               break;
            }
            case IDC_PORT1ATYPECB:
            case IDC_PORT1BTYPECB:
            case IDC_PORT1CTYPECB:
            case IDC_PORT1DTYPECB:
            case IDC_PORT1ETYPECB:
            case IDC_PORT1FTYPECB:
            case IDC_PORT2ATYPECB:
            case IDC_PORT2BTYPECB:
            case IDC_PORT2CTYPECB:
            case IDC_PORT2DTYPECB:
            case IDC_PORT2ETYPECB:
            case IDC_PORT2FTYPECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                     // If emulated peripheral is set to none, we don't want 
                     // the config button enabled
                     if (ComboBox_GetCurSel((HWND)lParam) != 0)
                        Button_Enable(GetDlgItem(hDlg, IDC_PORT1ACFGPB+(int)LOWORD(wParam)-IDC_PORT1ATYPECB), TRUE);
                     else
                        Button_Enable(GetDlgItem(hDlg, IDC_PORT1ACFGPB+(int)LOWORD(wParam)-IDC_PORT1ATYPECB), FALSE);

                     break;
                  default: break;
               }
               break;
            }
            case IDC_PORT1ACFGPB:
            case IDC_PORT1BCFGPB:
            case IDC_PORT1CCFGPB:
            case IDC_PORT1DCFGPB:
            case IDC_PORT1ECFGPB:
            case IDC_PORT1FCFGPB:
            case IDC_PORT2ACFGPB:
            case IDC_PORT2BCFGPB:
            case IDC_PORT2CCFGPB:
            case IDC_PORT2DCFGPB:
            case IDC_PORT2ECFGPB:
            case IDC_PORT2FCFGPB:
               switch (ComboBox_GetCurSel(GetDlgItem(hDlg, IDC_PORT1ATYPECB+LOWORD(wParam)-IDC_PORT1ACFGPB)))
               {
                  case 1:
                     // Standard Pad
                     DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_PADCONFIG), hDlg, (DLGPROC)PadConfigDlgProc, (LPARAM)LOWORD(wParam)-IDC_PORT1ACFGPB);
                     break;
                  case 2:
                     // Mouse
                     DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_MOUSECONFIG), hDlg, (DLGPROC)MouseConfigDlgProc, (LPARAM)LOWORD(wParam)-IDC_PORT1ACFGPB);
                     break;
                  default: break;
               }
               return TRUE;
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
               u32 i, j;
               char string1[13], string2[32];

               for (i = 0; i < 2; i++)
               {
                  int cursel=(int)SendDlgItemMessage(hDlg, IDC_PORT1CONNTYPECB+i, CB_GETCURSEL, 0, 0);

                  sprintf(string1, "Port%dType", (int)i+1);
                  sprintf(string2, "%d", cursel);
                  WritePrivateProfileStringA("Input", string1, string2, inifilename);

                  for (j = 0; j < 6; j++)
                  {
                     sprintf(string1, "Peripheral%ld%C", i+1, 'A'+j);
                     sprintf(string2, "%d", ConvertEmulateTypeSelStringToID(hDlg, IDC_PORT1ATYPECB+(i*6)+j));
                     WritePrivateProfileStringA(string1, "EmulateType", string2, inifilename);
                  }
               }

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
         // Reload device(s)
         PERDXLoadDevices(inifilename);

         DestroyHelpBalloons(hb);
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

void DoControlConfig(HWND hDlg, u8 cursel, int controlid, int basecontrolid, int *controlmap)
{
   char buttonname[MAX_PATH];
   int ret;

   if ((ret = PERDXFetchNextPress(hDlg, cursel, buttonname)) >= 0)
   {
      SetDlgItemText(hDlg, controlid, _16(buttonname));
      controlmap[controlid - basecontrolid] = ret;
   }
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK PadConfigDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   static u8 cursel;
   static BOOL enablebuttons;
   static int controlmap[13];
   static int padnum;
   static HWND hParent;
   int i;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         padnum = (int)lParam;

         hParent = PropSheet_GetCurrentPageHwnd(GetParent(hDlg));
         PERDXListDevices(GetDlgItem(hDlg, IDC_DXDEVICECB), ConvertEmulateTypeSelStringToID(hParent, IDC_PORT1ATYPECB+padnum));

         // Load settings from ini here, if necessary
         PERDXInitControlConfig(hDlg, padnum, controlmap, inifilename);

         cursel = (u8)SendDlgItemMessage(hDlg, IDC_DXDEVICECB, CB_GETCURSEL, 0, 0);
         enablebuttons = cursel ? TRUE : FALSE;

         EnableWindow(GetDlgItem(hDlg, IDC_UPPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_DOWNPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_LEFTPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_RIGHTPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_LPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_RPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_STARTPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_APB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_BPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_CPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_XPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_YPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_ZPB), enablebuttons);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_UPPB:
            case IDC_DOWNPB:
            case IDC_LEFTPB:
            case IDC_RIGHTPB:
            case IDC_LPB:
            case IDC_RPB:
            case IDC_STARTPB:
            case IDC_APB:
            case IDC_BPB:
            case IDC_CPB:
            case IDC_XPB:
            case IDC_YPB:
            case IDC_ZPB:
            {
               DoControlConfig(hDlg, cursel-1, IDC_UPTEXT+(LOWORD(wParam)-IDC_UPPB), IDC_UPTEXT, controlmap);

               return TRUE;
            }
            case IDOK:
            {
               char string1[20];
               char string2[20];

               EndDialog(hDlg, TRUE);

               sprintf(string1, "Peripheral%d%C", ((padnum/6)+1), 'A'+(padnum%6));

               // Write GUID
               PERDXWriteGUID(cursel-1, padnum, inifilename);

               for (i = 0; i < 13; i++)
               {
                  sprintf(string2, "%d", controlmap[i]);
                  WritePrivateProfileStringA(string1, PerPadNames[i], string2, inifilename);
               }

               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            case IDC_DXDEVICECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_DXDEVICECB, CB_GETCURSEL, 0, 0);
                     enablebuttons = cursel ? TRUE : FALSE;

                     EnableWindow(GetDlgItem(hDlg, IDC_UPPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_DOWNPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_LEFTPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_RIGHTPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_LPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_RPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_STARTPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_APB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_BPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_CPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_XPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_YPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_ZPB), enablebuttons);
                     break;
                  }
                  default: break;
               }
               break;
            }
            default: break;
         }

         break;
      }
      case WM_DESTROY:
      {
         break;
      }
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK MouseConfigDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam)
{
   static u8 cursel;
   static BOOL enablebuttons;
   static int controlmap[13];
   static int padnum;
   static HWND hParent;
   static int emulatetype;
   int i;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         padnum = (int)lParam;

         hParent = PropSheet_GetCurrentPageHwnd(GetParent(hDlg));
         emulatetype = ConvertEmulateTypeSelStringToID(hParent, IDC_PORT1ATYPECB+padnum);
         PERDXListDevices(GetDlgItem(hDlg, IDC_DXDEVICECB), emulatetype);

         // Load settings from ini here, if necessary
         PERDXInitControlConfig(hDlg, padnum, controlmap, inifilename);

         cursel = (u8)SendDlgItemMessage(hDlg, IDC_DXDEVICECB, CB_GETCURSEL, 0, 0);
         enablebuttons = cursel ? TRUE : FALSE;

         EnableWindow(GetDlgItem(hDlg, IDC_STARTPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_APB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_BPB), enablebuttons);
         EnableWindow(GetDlgItem(hDlg, IDC_CPB), enablebuttons);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_STARTPB:
            case IDC_APB:
            case IDC_BPB:
            case IDC_CPB:
            {
               DoControlConfig(hDlg, cursel-1, IDC_UPTEXT+(LOWORD(wParam)-IDC_UPPB), IDC_UPTEXT, controlmap);

               return TRUE;
            }
            case IDOK:
            {
               char string1[20];
               char string2[20];

               EndDialog(hDlg, TRUE);

               sprintf(string1, "Peripheral%d%C", ((padnum/6)+1), 'A'+(padnum%6));

               // Write GUID
               PERDXWriteGUID(cursel-1, padnum, inifilename);

               for (i = 0; i < 13; i++)
               {
                  sprintf(string2, "%d", controlmap[i]);
                  WritePrivateProfileStringA(string1, PerPadNames[i], string2, inifilename);
               }
               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            case IDC_DXDEVICECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_DXDEVICECB, CB_GETCURSEL, 0, 0);
                     enablebuttons = cursel ? TRUE : FALSE;

                     EnableWindow(GetDlgItem(hDlg, IDC_STARTPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_APB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_BPB), enablebuttons);
                     EnableWindow(GetDlgItem(hDlg, IDC_CPB), enablebuttons);
                     break;
                  }
                  default: break;
               }
               break;
            }
            default: break;
         }

         break;
      }
      case WM_DESTROY:
      {
         break;
      }
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
