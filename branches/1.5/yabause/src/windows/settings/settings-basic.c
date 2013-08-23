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
#include <ctype.h>
#include "../cd.h"
//#undef FASTCALL
#include "../../cs0.h"
#include "../../cs2.h"
#include "../resource.h"
#include "../../yabause.h"
#include "settings.h"

char biosfilename[MAX_PATH] = "\0";
char cdrompath[MAX_PATH]="\0";
char backupramfilename[MAX_PATH] = "bkram.bin\0";
char mpegromfilename[MAX_PATH] = "\0";
char cartfilename[MAX_PATH] = "\0";

char bioslang=0;
char sh2coretype=0;
u8 regionid=0;
int disctype;
int carttype;

int num_cdroms=0;
char drive_list[24];

void StartGame();

//////////////////////////////////////////////////////////////////////////////

void GenerateCDROMList(HWND hWnd)
{
   int i=0;
   char tempstr[8];

   num_cdroms=0;

   // Go through drives C-Z and only add only cdrom drives to drive letter
   // list

   for (i = 0; i < 24; i++)
   {
      sprintf(tempstr, "%c:\\", 'c' + i);

      if (GetDriveTypeA(tempstr) == DRIVE_CDROM)
      {
         drive_list[num_cdroms] = 'c' + i;

         sprintf(tempstr, "%c", 'C' + i);
         SendDlgItemMessage(hWnd, IDC_DRIVELETTERCB, CB_ADDSTRING, 0, (LPARAM)_16(tempstr));
         num_cdroms++;
      } 
   }
}

//////////////////////////////////////////////////////////////////////////////

BOOL IsPathCdrom(const char *path)
{
   if (GetDriveTypeA(cdrompath) == DRIVE_CDROM)
      return TRUE;
   else
      return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK BasicSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam)
{
   static helpballoon_struct hb[9];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         BOOL imagebool=FALSE;
         char current_drive=0;
         int i;

         // Setup Combo Boxes

         // Disc Type Box
         SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("CD"));
         SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Image"));

         // Drive Letter Box
         SendDlgItemMessage(hDlg, IDC_DRIVELETTERCB, CB_RESETCONTENT, 0, 0);

         // figure out which drive letters are available
         GenerateCDROMList(hDlg);

         // Set Disc Type Selection
         if (IsPathCdrom(cdrompath))
         {
            current_drive = cdrompath[0];
            imagebool = FALSE;
         }
         else
         {
            // Assume it's a file
            current_drive = 0;
            imagebool = TRUE;
         }

         if (current_drive != 0)
         {
            for (i = 0; i < num_cdroms; i++)
            {
               if (toupper(current_drive) == toupper(drive_list[i]))
               {
                  SendDlgItemMessage(hDlg, IDC_DRIVELETTERCB, CB_SETCURSEL, i, 0);
               }
            }
         }
         else
         {
            // set it to the first drive
            SendDlgItemMessage(hDlg, IDC_DRIVELETTERCB, CB_SETCURSEL, 0, 0);
         }

         // disable/enable menu options depending on whether or not 
         // image is selected
         if (imagebool == FALSE)
         {
            SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_SETCURSEL, 0, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_IMAGEEDIT), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_IMAGEBROWSE), FALSE);

            EnableWindow(GetDlgItem(hDlg, IDC_DRIVELETTERCB), TRUE);
         }
         else
         {
            SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_SETCURSEL, 1, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_IMAGEEDIT), TRUE);
            EnableWindow(GetDlgItem(hDlg, IDC_IMAGEBROWSE), TRUE);
            SetDlgItemText(hDlg, IDC_IMAGEEDIT, _16(cdrompath));

            EnableWindow(GetDlgItem(hDlg, IDC_DRIVELETTERCB), FALSE);
         }

/*
         // Setup Bios Language Combo box
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"English");
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"German");
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"French");
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"Spanish");
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"Italian");
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_ADDSTRING, 0, (long)"Japanese");

         // Set Selected Bios Language
         SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_SETCURSEL, bioslang, 0);

         // Since it's not fully working, let's disable it
         EnableWindow(GetDlgItem(hDlg, IDC_BIOSLANGCB), FALSE);
*/

         // Setup SH2 Core Combo box
         SendDlgItemMessage(hDlg, IDC_SH2CORECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_SH2CORECB, CB_ADDSTRING, 0, (LPARAM)_16("Fast Interpreter"));
         SendDlgItemMessage(hDlg, IDC_SH2CORECB, CB_ADDSTRING, 0, (LPARAM)_16("Debug Interpreter"));

         // Set Selected SH2 Core
         SendDlgItemMessage(hDlg, IDC_SH2CORECB, CB_SETCURSEL, sh2coretype, 0);

         // Setup Region Combo box
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Auto-detect"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Japan(NTSC)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Asia(NTSC)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("North America(NTSC)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Central/South America(NTSC)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Korea(NTSC)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Asia(PAL)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Europe + others(PAL)"));
         SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_ADDSTRING, 0, (LPARAM)_16("Central/South America(PAL)"));

         // Set Selected Region
         switch(regionid)
         {
            case 0:
            case 1:
            case 2:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, regionid, 0);
               break;
            case 4:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 3, 0);
               break;
            case 5:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 4, 0);
               break;
            case 6:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 5, 0);
               break;
            case 0xA:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 6, 0);
               break;
            case 0xC:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 7, 0);
               break;
            case 0xD:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 8, 0);
               break;
            default:
               SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_SETCURSEL, 0, 0);
               break;
         }

         // Set Default Bios ROM File
         SetDlgItemText(hDlg, IDC_BIOSEDIT, _16(biosfilename));

         // Set Default Backup RAM File
         SetDlgItemText(hDlg, IDC_BACKUPRAMEDIT, _16(backupramfilename));

         // Set Default MPEG ROM File
         SetDlgItemText(hDlg, IDC_MPEGROMEDIT, _16(mpegromfilename));

         // Setup Cart Type Combo box
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_RESETCONTENT, 0, 0);
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("None"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Pro Action Replay"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("4 Mbit Backup Ram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("8 Mbit Backup Ram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("16 Mbit Backup Ram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("32 Mbit Backup Ram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("8 Mbit Dram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("32 Mbit Dram"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Netlink"));
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("16 Mbit Rom"));
//         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_ADDSTRING, 0, (LPARAM)"Japanese Modem");

         // Set Selected Cart Type
         SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_SETCURSEL, carttype, 0);

         // Set Default Cart File
         SetDlgItemText(hDlg, IDC_CARTEDIT, _16(cartfilename));

         // Set Cart File window status
         switch (carttype)
         {
            case CART_NONE:
            case CART_DRAM8MBIT:
            case CART_DRAM32MBIT:
            case CART_NETLINK:
            case CART_JAPMODEM:
               EnableWindow(GetDlgItem(hDlg, IDC_CARTEDIT), FALSE);
               EnableWindow(GetDlgItem(hDlg, IDC_CARTBROWSE), FALSE);
               break;
            case CART_PAR:
            case CART_BACKUPRAM4MBIT:
            case CART_BACKUPRAM8MBIT:
            case CART_BACKUPRAM16MBIT:
            case CART_BACKUPRAM32MBIT:
            case CART_ROM16MBIT:
               EnableWindow(GetDlgItem(hDlg, IDC_CARTEDIT), TRUE);
               EnableWindow(GetDlgItem(hDlg, IDC_CARTBROWSE), TRUE);
               break;
            default: break;
         }

         // Setup Tooltips
         hb[0].string = "Select whether to use a cdrom or a disc image";
         hb[0].hParent = GetDlgItem(hDlg, IDC_DISCTYPECB);
         hb[1].string = "Use this to select the SH2 emulation method. If in doubt, leave it as 'Fast Interpreter'";
         hb[1].hParent = GetDlgItem(hDlg, IDC_SH2CORECB);
         hb[2].string = "Use this to select the region of the CD. Normally it's best to leave it as 'Auto-detect'";
         hb[2].hParent = GetDlgItem(hDlg, IDC_REGIONCB);
         hb[3].string = "This is where you put the path to a Saturn bios rom image. If you don't have one, just leave it blank";
         hb[3].hParent = GetDlgItem(hDlg, IDC_BIOSEDIT);
         hb[4].string = "This is where you put the path to internal backup ram file. This holds all your saves.";
         hb[4].hParent = GetDlgItem(hDlg, IDC_BACKUPRAMEDIT);
         hb[5].string = "If you don't know what this is, just leave it blank.";
         hb[5].hParent = GetDlgItem(hDlg, IDC_MPEGROMEDIT);  
         hb[6].string = "Use this to select what kind of cartridge to emulate.  If in doubt, leave it as 'None'";
         hb[6].hParent = GetDlgItem(hDlg, IDC_CARTTYPECB);
         hb[7].string = "This is where you put the path to the file used by the emulated cartridge. The kind of file that goes here depends on what Cartridge Type is set to";
         hb[7].hParent = GetDlgItem(hDlg, IDC_CARTEDIT);
         hb[8].string = NULL;

         CreateHelpBalloons(hb);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_DISCTYPECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     u8 cursel=0;

                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_GETCURSEL, 0, 0);

                     if (cursel == 0)
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_IMAGEEDIT), FALSE);
                        EnableWindow(GetDlgItem(hDlg, IDC_IMAGEBROWSE), FALSE);

                        EnableWindow(GetDlgItem(hDlg, IDC_DRIVELETTERCB), TRUE);
                     }
                     else
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_IMAGEEDIT), TRUE);
                        EnableWindow(GetDlgItem(hDlg, IDC_IMAGEBROWSE), TRUE);

                        EnableWindow(GetDlgItem(hDlg, IDC_DRIVELETTERCB), FALSE);
                     }

                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_CARTTYPECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     u8 cursel=0;

                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_GETCURSEL, 0, 0);

                     switch (cursel)
                     {
                        case CART_NONE:
                        case CART_DRAM8MBIT:
                        case CART_DRAM32MBIT:
                        case CART_NETLINK:
                           EnableWindow(GetDlgItem(hDlg, IDC_CARTEDIT), FALSE);
                           EnableWindow(GetDlgItem(hDlg, IDC_CARTBROWSE), FALSE);
                           break;
                        case CART_PAR:
                        case CART_BACKUPRAM4MBIT:
                        case CART_BACKUPRAM8MBIT:
                        case CART_BACKUPRAM16MBIT:
                        case CART_BACKUPRAM32MBIT:
                        case CART_ROM16MBIT:
                           EnableWindow(GetDlgItem(hDlg, IDC_CARTEDIT), TRUE);
                           EnableWindow(GetDlgItem(hDlg, IDC_CARTBROWSE), TRUE);
                           break;
                        default: break;
                     }

                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_IMAGEBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;

               // setup ofn structure
               ZeroMemory(&ofn, sizeof(OPENFILENAME));
               ofn.lStructSize = sizeof(OPENFILENAME);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Supported image files (*.cue, *.iso)", "*.cue;*.iso",
                  "Cue files (*.cue)", "*.cue",
                  "Iso files (*.iso)", "*.iso",
                  "All files (*.*)", "*.*", NULL);

               ofn.lpstrFilter = filter;
               GetDlgItemText(hDlg, IDC_IMAGEEDIT, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr);
               ofn.Flags = OFN_FILEMUSTEXIST;

               if (GetOpenFileName(&ofn))
               {
                  // adjust appropriate edit box
                  SetDlgItemText(hDlg, IDC_IMAGEEDIT, tempwstr);
               }

               return TRUE;
            }
            case IDC_BIOSBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;
               // setup ofn structure
               ZeroMemory(&ofn, sizeof(OPENFILENAME));
               ofn.lStructSize = sizeof(OPENFILENAME);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Binaries (*.bin)", "*.bin",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               GetDlgItemText(hDlg, IDC_BIOSEDIT, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr);
               ofn.Flags = OFN_FILEMUSTEXIST;

               if (GetOpenFileName(&ofn))
               {
                  // adjust appropriate edit box
                  SetDlgItemText(hDlg, IDC_BIOSEDIT, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, biosfilename, MAX_PATH, NULL, NULL);
               }

               return TRUE;
            }
            case IDC_BACKUPRAMBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;
               // setup ofn structure
               ZeroMemory(&ofn, sizeof(OPENFILENAME));
               ofn.lStructSize = sizeof(OPENFILENAME);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Binaries (*.bin)", "*.bin",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               GetDlgItemText(hDlg, IDC_BACKUPRAMEDIT, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr);

               if (GetOpenFileName(&ofn))
               {
                  // adjust appropriate edit box
                  SetDlgItemText(hDlg, IDC_BACKUPRAMEDIT, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, backupramfilename, MAX_PATH, NULL, NULL);
               }

               return TRUE;
            }
            case IDC_MPEGROMBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;
               // setup ofn structure
               ZeroMemory(&ofn, sizeof(OPENFILENAME));
               ofn.lStructSize = sizeof(OPENFILENAME);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Binaries (*.bin)", "*.bin",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               GetDlgItemText(hDlg, IDC_MPEGROMEDIT, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr);
               ofn.Flags = OFN_FILEMUSTEXIST;

               if (GetOpenFileName(&ofn))
               {
                  // adjust appropriate edit box
                  SetDlgItemText(hDlg, IDC_MPEGROMEDIT, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, mpegromfilename, MAX_PATH, NULL, NULL);
               }

               return TRUE;
            }
            case IDC_CARTBROWSE:
            {
               WCHAR tempwstr[MAX_PATH];
               WCHAR filter[1024];
               OPENFILENAME ofn;
               u8 cursel=0;

               // setup ofn structure
               ZeroMemory(&ofn, sizeof(OPENFILENAME));
               ofn.lStructSize = sizeof(OPENFILENAME);
               ofn.hwndOwner = hDlg;

               CreateFilter(filter, 1024,
                  "Binaries (*.bin)", "*.bin",
                  "All Files", "*.*", NULL);

               ofn.lpstrFilter = filter;
               GetDlgItemText(hDlg, IDC_CARTEDIT, tempwstr, MAX_PATH);
               ofn.lpstrFile = tempwstr;
               ofn.nMaxFile = sizeof(tempwstr);

               cursel = (u8)SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_GETCURSEL, 0, 0);

               switch (cursel)
               {
                  case CART_PAR:
                  case CART_ROM16MBIT:
                     ofn.Flags = OFN_FILEMUSTEXIST;
                     break;
                  default: break;
               }

               if (GetOpenFileName(&ofn))
               {
                  // adjust appropriate edit box
                  SetDlgItemText(hDlg, IDC_CARTEDIT, tempwstr);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, cartfilename, MAX_PATH, NULL, NULL);
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
               char current_drive=0;
               BOOL imagebool;
               BOOL cdromchanged=FALSE;

               // Convert Dialog items back to variables
               GetDlgItemText(hDlg, IDC_BIOSEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, biosfilename, MAX_PATH, NULL, NULL);
               GetDlgItemText(hDlg, IDC_BACKUPRAMEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, backupramfilename, MAX_PATH, NULL, NULL);
               GetDlgItemText(hDlg, IDC_MPEGROMEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1,  mpegromfilename, MAX_PATH, NULL, NULL);
               carttype = (int)SendDlgItemMessage(hDlg, IDC_CARTTYPECB, CB_GETCURSEL, 0, 0);
               GetDlgItemText(hDlg, IDC_CARTEDIT, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1,  cartfilename, MAX_PATH, NULL, NULL);

               // write path/filenames
               WritePrivateProfileStringA("General", "BiosPath", biosfilename, inifilename);
               WritePrivateProfileStringA("General", "BackupRamPath", backupramfilename, inifilename);
               WritePrivateProfileStringA("General", "MpegRomPath", mpegromfilename, inifilename);

               sprintf(tempstr, "%d", carttype);
               WritePrivateProfileStringA("General", "CartType", tempstr, inifilename);

               // figure out cart type, write cartfilename if necessary
               switch (carttype)
               {
                  case CART_PAR:
                  case CART_BACKUPRAM4MBIT:
                  case CART_BACKUPRAM8MBIT:
                  case CART_BACKUPRAM16MBIT:
                  case CART_BACKUPRAM32MBIT:
                  case CART_ROM16MBIT:
                     WritePrivateProfileStringA("General", "CartPath", cartfilename, inifilename);
                     break;
                  default: break;
               }

               imagebool = (BOOL)SendDlgItemMessage(hDlg, IDC_DISCTYPECB, CB_GETCURSEL, 0, 0);

               if (imagebool == FALSE)
               {
                  // convert drive letter to string
                  current_drive = (char)SendDlgItemMessage(hDlg, IDC_DRIVELETTERCB, CB_GETCURSEL, 0, 0);
                  sprintf(tempstr, "%c:", toupper(drive_list[(int)current_drive]));

                  if (strcmp(tempstr, cdrompath) != 0)
                  {
                     strcpy(cdrompath, tempstr);
                     cdromchanged = TRUE;
                  }
               }
               else
               {
                  // retrieve image filename string instead
                  GetDlgItemText(hDlg, IDC_IMAGEEDIT, tempwstr, MAX_PATH);
                  WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);

                  if (strcmp(tempstr, cdrompath) != 0)
                  {
                     strcpy(cdrompath, tempstr);
                     cdromchanged = TRUE;
                  }
               }

               WritePrivateProfileStringA("General", "CDROMDrive", cdrompath, inifilename);

/*
               // Convert ID to language string
               bioslang = (char)SendDlgItemMessage(hDlg, IDC_BIOSLANGCB, CB_GETCURSEL, 0, 0);

               switch (bioslang)
               {
                  case 0:
                     sprintf(tempstr, "English");
                     break;
                  case 1:
                     sprintf(tempstr, "German");
                     break;
                  case 2:
                     sprintf(tempstr, "French");
                     break;
                  case 3:
                     sprintf(tempstr, "Spanish");
                     break;
                  case 4:
                     sprintf(tempstr, "Italian");
                     break;
                  case 5:
                     sprintf(tempstr, "Japanese");
                     break;
                  default:break;
               }

//               WritePrivateProfileStringA("General", "BiosLanguage", tempstr, inifilename);
*/
               // Write SH2 core type
               sh2coretype = (char)SendDlgItemMessage(hDlg, IDC_SH2CORECB, CB_GETCURSEL, 0, 0);

               sprintf(tempstr, "%d", sh2coretype);
               WritePrivateProfileStringA("General", "SH2Core", tempstr, inifilename);

               // Convert Combo Box ID to Region ID
               regionid = (char)SendDlgItemMessage(hDlg, IDC_REGIONCB, CB_GETCURSEL, 0, 0);

               switch(regionid)
               {
                  case 0:
                     WritePrivateProfileStringA("General", "Region", "Auto", inifilename);
                     break;
                  case 1:
                     WritePrivateProfileStringA("General", "Region", "J", inifilename);
                     break;
                  case 2:
                     WritePrivateProfileStringA("General", "Region", "T", inifilename);
                     break;
                  case 3:
                     WritePrivateProfileStringA("General", "Region", "U", inifilename);
                     regionid = 4;
                     break;
                  case 4:
                     WritePrivateProfileStringA("General", "Region", "B", inifilename);
                     regionid = 5;
                     break;
                  case 5:
                     WritePrivateProfileStringA("General", "Region", "K", inifilename);
                     regionid = 6;
                     break;
                  case 6:
                     WritePrivateProfileStringA("General", "Region", "A", inifilename);
                     regionid = 0xA;
                     break;
                  case 7:
                     WritePrivateProfileStringA("General", "Region", "E", inifilename);
                     regionid = 0xC;
                     break;
                  case 8:
                     WritePrivateProfileStringA("General", "Region", "L", inifilename);
                     regionid = 0xD;
                     break;
                  default:
                     regionid = 0;
                     break;
               }

               if (cdromchanged && nocorechange == 0)
               {
					StartGame();

#ifndef USETHREADS
                  if (IsPathCdrom(cdrompath))
                     Cs2ChangeCDCore(CDCORE_SPTI, cdrompath);
                  else
                     Cs2ChangeCDCore(CDCORE_ISO, cdrompath);
#else
                  corechanged = 0;
                  changecore |= 1;
                  while (corechanged == 0) { Sleep(0); }
#endif
				  YabauseReset();
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
         DestroyHelpBalloons(hb);
         break;
      }
      default: break;
   }
   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
