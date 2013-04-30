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
#include "../../m68kd.h"
#include "../../scsp.h"
#include "yuidebug.h"
#include "../yuiwin.h"

void M68KUpdateRegList(HWND hDlg, m68kregs_struct *regs)
{
   char tempstr[128];
   int i;

   SendMessage(GetDlgItem(hDlg, IDC_REGLISTLB), LB_RESETCONTENT, 0, 0);

   // Data registers
   for (i = 0; i < 8; i++)
   {
      sprintf(tempstr, "D%d =   %08x", i, (int)regs->D[i]);
      strupr(tempstr);
      SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }

   // Address registers
   for (i = 0; i < 8; i++)
   {
      sprintf(tempstr, "A%d =   %08x", i, (int)regs->A[i]);
      strupr(tempstr);
      SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }

   // SR
   sprintf(tempstr, "SR =   %08x", (int)regs->SR);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // PC
   sprintf(tempstr, "PC =   %08x", (int)regs->PC);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
}

//////////////////////////////////////////////////////////////////////////////

void M68KBreakpointHandler (u32 addr)
{
   ScspMuteAudio(SCSP_MUTE_SYSTEM);
   MessageBox (YabWin, _16("Breakpoint Reached"), _16("Notice"),  MB_OK | MB_ICONINFORMATION);
   DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_M68KDEBUG), YabWin, (DLGPROC)M68KDebugDlgProc);
   ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
}

//////////////////////////////////////////////////////////////////////////////

void M68KUpdateCodeList(HWND hDlg, u32 addr)
{
   SendDlgItemMessage(hDlg, IDC_DISASM, DIS_GOTOADDRESS, 0, addr);
   SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETPC, 0, addr);
}

//////////////////////////////////////////////////////////////////////////////

int M68KDis(u32 addr, char *string)
{
   return (int)(M68KDisasm(addr, string) - addr);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK M68KDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         m68kregs_struct m68kregs;
         const m68kcodebreakpoint_struct *cbp;
         char tempstr[10];
         int i;

         EnableWindow(GetDlgItem(hDlg, IDC_STEP), TRUE);

         SendDlgItemMessageA(hDlg, IDC_CODEBPET, EM_SETLIMITTEXT, 5, 0);
         cbp = M68KGetBreakpointList();

         for (i = 0; i < MAX_BREAKPOINTS; i++)
         {
            if (cbp[i].addr != 0xFFFFFFFF)
            {
               sprintf(tempstr, "%08X", (int)cbp[i].addr);
               SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
            }
         }

         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETDISFUNC, 0, (LPARAM)M68KDis);
         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETENDADDRESS, 0, 0x100000);

         M68KGetRegisters(&m68kregs);
         M68KUpdateRegList(hDlg, &m68kregs);
         M68KUpdateCodeList(hDlg, m68kregs.PC);

         M68KSetBreakpointCallBack(&M68KBreakpointHandler);
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
               m68kregs_struct m68kregs;

               // execute instruction
               M68KStep();

               M68KGetRegisters(&m68kregs);
               M68KUpdateRegList(hDlg, &m68kregs);
               M68KUpdateCodeList(hDlg, m68kregs.PC);
               break;
            }
            case IDC_ADDCODEBP:
            {
               // add a code breakpoint
               char bptext[10];
               u32 addr=0;
               memset(bptext, 0, 10);
               GetDlgItemTextA(hDlg, IDC_CODEBPET, bptext, 10);

               if (bptext[0] != 0)
               {
                  sscanf(bptext, "%lX", &addr);
                  sprintf(bptext, "%05X", (int)addr);

                  if (M68KAddCodeBreakpoint(addr) == 0)
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
                  M68KDelCodeBreakpoint(addr);
                  SendDlgItemMessage(hDlg, IDC_CODEBPLB, LB_DELETESTRING, ret, 0);
               }

               break;
            }
            case IDC_REGLISTLB:
            {
               switch (HIWORD(wParam))
               {
                  case LBN_DBLCLK:
                  {
                     // dialogue for changing register values
                     int cursel;
                     m68kregs_struct m68kregs;

                     cursel = (int)SendMessage(GetDlgItem(hDlg,LOWORD(wParam)), LB_GETCURSEL,0,0);

                     M68KGetRegisters(&m68kregs);

                     switch (cursel)
                     {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                           memaddr = m68kregs.D[cursel];                           
                           break;
                        case 8:
                        case 9:
                        case 10:
                        case 11:
                        case 12:
                        case 13:
                        case 14:
                        case 15:
                           memaddr = m68kregs.A[cursel - 8];
                           break;
                        case 16:
                           memaddr = m68kregs.SR;
                           break;
                        case 17:
                           memaddr = m68kregs.PC;
                           break;
                        default: break;
                     }

                     if (DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_MEM), hDlg, (DLGPROC)MemDlgProc) == TRUE)
                     {
                        switch (cursel)
                        {
                           case 0:
                           case 1:
                           case 2:
                           case 3:
                           case 4:
                           case 5:
                           case 6:
                           case 7:
                              m68kregs.D[cursel] = memaddr;
                              break;
                           case 8:
                           case 9:
                           case 10:
                           case 11:
                           case 12:
                           case 13:
                           case 14:
                           case 15:
                              m68kregs.A[cursel - 8] = memaddr;
                              break;
                           case 16:
                              m68kregs.SR = memaddr;
                              break;
                           case 17:
                              m68kregs.PC = memaddr;
                              M68KUpdateCodeList(hDlg, m68kregs.PC);
                              break;
                           default: break;
                        }

                        M68KSetRegisters(&m68kregs);
                     }

                     M68KUpdateRegList(hDlg, &m68kregs);
                     break;
                  }
                  default: break;
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
                     sprintf(bptext, "%05X", (int)addr);

                     if (M68KAddCodeBreakpoint(addr) == 0)
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

