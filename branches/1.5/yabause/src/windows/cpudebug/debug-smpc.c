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
#undef FASTCALL
#include "../resource.h"
#include "../../smpc.h"

void SMPCUpdateRegList(HWND hDlg)
{
   char tempstr[128];
   int i;

   SendMessage(GetDlgItem(hDlg, IDC_INPUTREGLB), LB_RESETCONTENT, 0, 0);
   for (i = 0; i < 7; i++)
   {
      sprintf(tempstr, "IREG%d = %02X", i, (int)SmpcRegs->IREG[i]);
      SendMessageA(GetDlgItem(hDlg, IDC_INPUTREGLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }

   SendMessage(GetDlgItem(hDlg, IDC_OUTPUTREGLB), LB_RESETCONTENT, 0, 0);

   for (i = 0; i < 10; i++)
   {
      sprintf(tempstr, "OREG%d =  %02X", i, (int)SmpcRegs->OREG[i]);
      SendMessageA(GetDlgItem(hDlg, IDC_OUTPUTREGLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }

   for (i = 10; i < 32; i++)
   {
      sprintf(tempstr, "OREG%02d = %02X", i, (int)SmpcRegs->OREG[i]);
      SendMessageA(GetDlgItem(hDlg, IDC_OUTPUTREGLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }
}

//////////////////////////////////////////////////////////////////////////////

extern SmpcInternal * SmpcInternalVars;

LRESULT CALLBACK SMPCDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         char tempstr[1024];
         char *buf;

         SMPCUpdateRegList(hDlg);
         sprintf(tempstr, "Status = %s\r\n", SmpcRegs->SF ? "Busy" : "Ready");
         buf = tempstr + strlen(tempstr);
         if (SmpcRegs->SF)
         {
            sprintf(buf, "Currently executing command: %02X\r\n", SmpcRegs->COMREG);
            sprintf(buf, "time remaining = %ld\r\n", SmpcInternalVars->timing);
         }
         else
            sprintf(buf, "Last executed command: %02X\r\n", SmpcRegs->COMREG);

         SetDlgItemText(hDlg, IDC_SMPCSTATUSET, _16(tempstr));
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
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

