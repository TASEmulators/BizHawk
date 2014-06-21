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
#undef FASTCALL
#include "../disasm.h"
#include "../resource.h"
#include "../../scsp.h"
#include "../../scu.h"
#include "yuidebug.h"
#include "../yuiwin.h"

void SCUDSPUpdateRegList(HWND hDlg, scudspregs_struct *regs)
{
   char tempstr[128];

   SendMessage(GetDlgItem(hDlg, IDC_REGLISTLB), LB_RESETCONTENT, 0, 0);

   sprintf(tempstr, "PR = %d   EP = %d", regs->ProgControlPort.part.PR, regs->ProgControlPort.part.EP);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "T0 = %d   S =  %d", regs->ProgControlPort.part.T0, regs->ProgControlPort.part.S);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "Z =  %d   C =  %d", regs->ProgControlPort.part.Z, regs->ProgControlPort.part.C);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "V =  %d   E =  %d", regs->ProgControlPort.part.V, regs->ProgControlPort.part.E);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "ES = %d   EX = %d", regs->ProgControlPort.part.ES, regs->ProgControlPort.part.EX);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "LE =          %d", regs->ProgControlPort.part.LE);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "P =          %02X", regs->ProgControlPort.part.P);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "TOP =        %02X", regs->TOP);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "LOP =        %02X", regs->LOP);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "CT = %02X:%02X:%02X:%02X", regs->CT[0], regs->CT[1], regs->CT[2], regs->CT[3]);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "RA =   %08lX", regs->RA0);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "WA =   %08lX", regs->WA0);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "RX =   %08lX", regs->RX);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "RY =   %08lX", regs->RX);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "PH =       %04X", regs->P.part.H & 0xFFFF);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "PL =   %08X", (int)(regs->P.part.L & 0xFFFFFFFF));
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "ACH =      %04X", regs->AC.part.H & 0xFFFF);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   sprintf(tempstr, "ACL =  %08X", (int)(regs->AC.part.L & 0xFFFFFFFF));
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
}

//////////////////////////////////////////////////////////////////////////////

void SCUDSPUpdateCodeList(HWND hDlg, u8 addr)
{
   if (addr < 11)
      SendDlgItemMessage(hDlg, IDC_DISASM, DIS_GOTOADDRESS,0, 0);
   else
      SendDlgItemMessage(hDlg, IDC_DISASM, DIS_GOTOADDRESS,0, addr-11);
   SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETPC,0, addr);
}

//////////////////////////////////////////////////////////////////////////////

void SCUDSPBreakpointHandler (u32 addr)
{
   ScspMuteAudio(SCSP_MUTE_SYSTEM);
   MessageBox (YabWin, _16("Breakpoint Reached"), _16("Notice"),  MB_OK | MB_ICONINFORMATION);
   DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SCUDSPDEBUG), YabWin, (DLGPROC)SCUDSPDebugDlgProc);
   ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
}

//////////////////////////////////////////////////////////////////////////////
int SCUDSPDis(u32 addr, char *string)
{
   ScuDspDisasm((u8)addr, string);
   return 1;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK SCUDSPDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         scudspregs_struct dspregs;
         const scucodebreakpoint_struct *cbp;
         char tempstr[10];
         int i;

         SendDlgItemMessageA(hDlg, IDC_CODEBPET, EM_SETLIMITTEXT, 2, 0);
         cbp = ScuDspGetBreakpointList();

         for (i = 0; i < MAX_BREAKPOINTS; i++)
         {
            if (cbp[i].addr != 0xFFFFFFFF)
            {
               sprintf(tempstr, "%02X", (int)cbp[i].addr);
               SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
            }
         }

         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETDISFUNC, 0, (LPARAM)SCUDSPDis);
         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETENDADDRESS, 0, 0x100);

         EnableWindow(GetDlgItem(hDlg, IDC_STEP), TRUE);

         ScuDspGetRegisters(&dspregs);
         SCUDSPUpdateRegList(hDlg, &dspregs);
         SCUDSPUpdateCodeList(hDlg, (u8)dspregs.ProgControlPort.part.P);

         ScuDspSetBreakpointCallBack(&SCUDSPBreakpointHandler);

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
            case IDC_STEP:
            {
               scudspregs_struct dspregs;

               ScuDspStep();

               ScuDspGetRegisters(&dspregs);
               SCUDSPUpdateRegList(hDlg, &dspregs);
               SCUDSPUpdateCodeList(hDlg, (u8)dspregs.ProgControlPort.part.P);

               break;
            }
            case IDC_SAVEPROGRAM:
            {
               OPENFILENAME ofn;
               WCHAR filter[1024];
               TCHAR tempstr2[MAX_PATH]=TEXT("");
               char tempstr[MAX_PATH];

               CreateFilter(filter, 1024,
                  "Binary Files", "*.BIN",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, tempstr2, sizeof(tempstr2)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("BIN");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, tempstr2, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  ScuDspSaveProgram(tempstr);
               }
               break;
            }
            case IDC_SAVEMD0:
            case IDC_SAVEMD1:
            case IDC_SAVEMD2:
            case IDC_SAVEMD3:
            {
               OPENFILENAME ofn;
               WCHAR filter[1024];
               TCHAR tempstr2[MAX_PATH]=TEXT("");
               char tempstr[MAX_PATH];

               CreateFilter(filter, 1024,
                  "Binary Files", "*.BIN",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, tempstr2, sizeof(tempstr2)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("BIN");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, tempstr2, -1, tempstr, sizeof(tempstr), NULL, NULL);
                  ScuDspSaveMD(tempstr, LOWORD(wParam)-IDC_SAVEMD0);
               }
               break;
            }
            case IDC_ADDCODEBP:
            {
               // add a code breakpoint
               char bptext[10];
               u32 addr=0;
               memset(bptext, 0, 4);
               GetDlgItemTextA(hDlg, IDC_CODEBPET, bptext, 4);

               if (bptext[0] != 0)
               {
                  sscanf(bptext, "%lX", &addr);
                  sprintf(bptext, "%02X", (int)addr);

                  if (ScuDspAddCodeBreakpoint(addr) == 0)
                     SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)bptext);
               }
               break;
            }
            case IDC_DELCODEBP:
            {
               // delete a code breakpoint
               LRESULT ret;
               char bptext[10];
               u32 addr=0;

               if ((ret = SendDlgItemMessage(hDlg, IDC_CODEBPLB, LB_GETCURSEL, 0, 0)) != LB_ERR)
               {
                  SendDlgItemMessageA(hDlg, IDC_CODEBPLB, LB_GETTEXT, ret, (LPARAM)bptext);
                  sscanf(bptext, "%lX", &addr);
                  ScuDspDelCodeBreakpoint(addr);
                  SendDlgItemMessage(hDlg, IDC_CODEBPLB, LB_DELETESTRING, ret, 0);
               }

               break;
            }
            case IDC_DISASM:
            {
               switch (HIWORD(wParam))
               {
                  case LBN_DBLCLK:
                  {
                     // Add a code breakpoint when code is double-clicked
                     char bptext[10];
                     u32 addr=0;

                     addr = (u32)SendMessage(GetDlgItem(hDlg,LOWORD(wParam)), DIS_GETCURSEL,0,0);
                     sprintf(bptext, "%02X", (int)addr);

                     if (ScuDspAddCodeBreakpoint(addr) == 0)
                        SendMessage(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)bptext);

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
      case WM_MOUSEWHEEL:
         DebugMouseWheel(GetDlgItem(hDlg, IDC_DISASM), wParam);
         return TRUE;
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

