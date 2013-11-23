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
#include "resource.h"
#include "../bios.h"

u32 currentbupdevice=0;
deviceinfo_struct *devices=NULL;
int numbupdevices=0;
saveinfo_struct *saves=NULL;
int numsaves=0;

//////////////////////////////////////////////////////////////////////////////

void RefreshSaveList(HWND hDlg)
{
   int i;
   u32 freespace=0, maxspace=0;
   char tempstr[MAX_PATH];

   saves = BupGetSaveList(currentbupdevice, &numsaves);

   SendDlgItemMessage(hDlg, IDC_BUPSAVELB, LB_RESETCONTENT, 0, 0);

   for (i = 0; i < numsaves; i++)
      SendDlgItemMessageA(hDlg, IDC_BUPSAVELB, LB_ADDSTRING, 0, (LPARAM)saves[i].filename);

   BupGetStats(currentbupdevice, &freespace, &maxspace);
   sprintf(tempstr, "%d/%d blocks free", (int)freespace, (int)maxspace);
   SetDlgItemText(hDlg, IDC_BUPFREESPACELT, _16(tempstr));                     
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK BackupRamDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   char tempstr[MAX_PATH];
   char tempstr2[MAX_PATH];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         int i;

         // Get available devices
         if ((devices = BupGetDeviceList(&numbupdevices)) == NULL)
            return FALSE;

         SendDlgItemMessage(hDlg, IDC_BUPDEVICECB, CB_RESETCONTENT, 0, 0);
         for (i = 0; i < numbupdevices; i++)
            SendDlgItemMessage(hDlg, IDC_BUPDEVICECB, CB_ADDSTRING, 0, (LPARAM)_16(devices[i].name));

         SendDlgItemMessage(hDlg, IDC_BUPDEVICECB, CB_SETCURSEL, 0, 0);
         RefreshSaveList(hDlg);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_BUPDEVICECB:
            {
               switch(HIWORD(wParam))
               {
                  case CBN_SELCHANGE:
                  {
                     currentbupdevice = (u8)SendDlgItemMessage(hDlg, IDC_BUPDEVICECB, CB_GETCURSEL, 0, 0);
                     RefreshSaveList(hDlg);
                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_BUPSAVELB:
            {
               switch(HIWORD(wParam))
               {
                  case LBN_SELCHANGE:
                  {
                     u8 cursel=0;
                     int i;

                     cursel = (u8)SendDlgItemMessage(hDlg, IDC_BUPSAVELB, LB_GETCURSEL, 0, 0);

                     SendDlgItemMessage(hDlg, IDC_BUPSAVELB, LB_GETTEXT, cursel, (LPARAM)_16(tempstr));

                     for (i = 0; i < numsaves; i++)
                     {
                        if (strcmp(tempstr, saves[i].filename) == 0)
                        {
                           cursel = i;
                           break;
                        }
                     }

                     SetDlgItemText(hDlg, IDC_BUPFILENAMEET, _16(saves[cursel].filename));
                     SetDlgItemText(hDlg, IDC_BUPCOMMENTET, _16(saves[cursel].comment));
                     switch(saves[cursel].language)
                     {
                        case 0:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("Japanese"));
                           break;
                        case 1:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("English"));
                           break;
                        case 2:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("French"));
                           break;
                        case 3:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("German"));
                           break;
                        case 4:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("Spanish"));
                           break;
                        case 5:
                           SetDlgItemText(hDlg, IDC_BUPLANGUAGEET, _16("Italian"));
                           break;
                        default: break;
                     }
                     sprintf(tempstr, "%d", (int)saves[cursel].datasize);
                     SetDlgItemText(hDlg, IDC_BUPDATASIZEET, _16(tempstr));
                     sprintf(tempstr, "%d", saves[cursel].blocksize);
                     SetDlgItemText(hDlg, IDC_BUPBLOCKSIZEET, _16(tempstr));
                     return TRUE;
                  }
                  default: break;
               }

               return TRUE;
            }
            case IDC_BUPDELETEBT:
            {
               LRESULT cursel = SendDlgItemMessage(hDlg, IDC_BUPSAVELB, LB_GETCURSEL, 0, 0);

               if (cursel == LB_ERR)
                  return TRUE;

               SendDlgItemMessage(hDlg, IDC_BUPSAVELB, LB_GETTEXT, cursel, (LPARAM)tempstr);

               sprintf(tempstr2, "Are you sure you want to delete %s?", tempstr);
               if (MessageBox (hDlg, _16(tempstr2), _16("Confirm Delete"),  MB_YESNO | MB_ICONEXCLAMATION) == IDYES)
               {
                  BupDeleteSave(currentbupdevice, tempstr);
                  RefreshSaveList(hDlg);
               }
               return TRUE;
            }
            case IDC_BUPFORMATBT:
            {
               sprintf(tempstr, "Are you sure you want to format %s?", devices[currentbupdevice].name);
               if (MessageBox (hDlg, _16(tempstr), _16("Confirm Delete"),  MB_YESNO | MB_ICONEXCLAMATION | MB_DEFBUTTON2) == IDYES)
               {
                  BupFormat(currentbupdevice);
                  RefreshSaveList(hDlg);
               }
               return TRUE;
            }
            case IDC_BUPIMPORTBT:
            {
               RefreshSaveList(hDlg);
               return TRUE;
            }
            case IDC_BUPEXPORTBT:
            {
               RefreshSaveList(hDlg);
               return TRUE;
            }
            case IDOK:
            {
               EndDialog(hDlg, TRUE);
               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
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
      case WM_DESTROY:
      {
         if (saves)
            free(saves);
         break;
      }
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
