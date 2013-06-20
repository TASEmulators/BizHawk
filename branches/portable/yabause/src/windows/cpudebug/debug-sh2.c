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
#include "../../sh2d.h"
#include "yuidebug.h"
#include "../yuiwin.h"
#include "../../sh2int.h"

//////////////////////////////////////////////////////////////////////////////

void SH2UpdateRegList(HWND hDlg, sh2regs_struct *regs)
{
   char tempstr[128];
   int i;

   SendMessage(GetDlgItem(hDlg, IDC_REGLISTLB), LB_RESETCONTENT, 0, 0);

   for (i = 0; i < 16; i++)
   {                                       
      sprintf(tempstr, "R%02d =  %08x", i, (int)regs->R[i]);
      strupr(tempstr);
      SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
   }

   // SR
   sprintf(tempstr, "SR =   %08x", (int)regs->SR.all);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // GBR
   sprintf(tempstr, "GBR =  %08x", (int)regs->GBR);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // VBR
   sprintf(tempstr, "VBR =  %08x", (int)regs->VBR);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // MACH
   sprintf(tempstr, "MACH = %08x", (int)regs->MACH);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // MACL
   sprintf(tempstr, "MACL = %08x", (int)regs->MACL);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // PR
   sprintf(tempstr, "PR =   %08x", (int)regs->PR);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);

   // PC
   sprintf(tempstr, "PC =   %08x", (int)regs->PC);
   strupr(tempstr);
   SendMessageA(GetDlgItem(hDlg, IDC_REGLISTLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
}

//////////////////////////////////////////////////////////////////////////////

void SH2UpdateCodeList(HWND hDlg, u32 addr)
{
   SendDlgItemMessage(hDlg, IDC_DISASM, DIS_GOTOADDRESS,0, addr-(11 * sizeof(u16)));
   SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETPC,0, addr);
}

//////////////////////////////////////////////////////////////////////////////

void SH2BreakpointHandler (SH2_struct *context, u32 addr)
{
   ScspMuteAudio(SCSP_MUTE_SYSTEM);
   MessageBox (YabWin, _16("Breakpoint Reached"), _16("Notice"),  MB_OK | MB_ICONINFORMATION);

   debugsh = context;
   DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SH2DEBUG), YabWin, (DLGPROC)SH2DebugDlgProc);
   ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
}

//////////////////////////////////////////////////////////////////////////////

int SH2Dis(u32 addr, char *string)
{
   SH2Disasm(addr, MappedMemoryReadWord(addr), 0, string);
   return 2;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK SH2DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         sh2regs_struct sh2regs;
         const codebreakpoint_struct *cbp;
         const memorybreakpoint_struct *mbp;
         char tempstr[10];
         int i;

         SendDlgItemMessageA(hDlg, IDC_CODEBPET, EM_SETLIMITTEXT, 8, 0);
         SendDlgItemMessageA(hDlg, IDC_MEMBPET, EM_SETLIMITTEXT, 8, 0);

         cbp = SH2GetBreakpointList(debugsh);
         mbp = SH2GetMemoryBreakpointList(debugsh);

         for (i = 0; i < MAX_BREAKPOINTS; i++)
         {
            if (cbp[i].addr != 0xFFFFFFFF)
            {
               sprintf(tempstr, "%08X", (int)cbp[i].addr);
               SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
            }

            if (mbp[i].addr != 0xFFFFFFFF)
            {
               sprintf(tempstr, "%08X", (int)mbp[i].addr);
               SendMessageA(GetDlgItem(hDlg, IDC_MEMBPLB), LB_ADDSTRING, 0, (LPARAM)tempstr);
            }
         }

         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETDISFUNC, 0, (LPARAM)SH2Dis);
         SendDlgItemMessage(hDlg, IDC_DISASM, DIS_SETENDADDRESS, 0, 0x06100000);

//         if (proc->paused())
//         {
            SH2GetRegisters(debugsh, &sh2regs);
            SH2UpdateRegList(hDlg, &sh2regs);
            SH2UpdateCodeList(hDlg, sh2regs.PC);
//         }


         SH2SetBreakpointCallBack(debugsh, (void (*)(void *, u32))&SH2BreakpointHandler);
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
               sh2regs_struct sh2regs;
               SH2Step(debugsh);
               SH2GetRegisters(debugsh, &sh2regs);
               SH2UpdateRegList(hDlg, &sh2regs);
               SH2UpdateCodeList(hDlg, sh2regs.PC);

               break;
            }
            case IDC_STEPOVER:
            {
               break;
            }
            case IDC_MEMTRANSFER:
            {
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_MEMTRANSFER), hDlg, (DLGPROC)MemTransferDlgProc);
               break;
            }
            case IDC_MEMEDITOR:
            {
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_MEMORYEDITOR), hDlg, (DLGPROC)MemoryEditorDlgProc);
               break;
            }
            case IDC_ADDCODEBP:
            {
               char bptext[10];
               u32 addr=0;
               extern SH2Interface_struct *SH2Core;

               if (SH2Core->id != SH2CORE_DEBUGINTERPRETER)
               {
                  MessageBox (hDlg, _16("Breakpoints only supported by SH2 Debug Interpreter"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                  break;
               }
                  
               memset(bptext, 0, 10);
               GetDlgItemTextA(hDlg, IDC_CODEBPET, bptext, 10);

               if (bptext[0] != 0)
               {
                  sscanf(bptext, "%lX", &addr);
                  sprintf(bptext, "%08lX", addr);

                  if (SH2AddCodeBreakpoint(debugsh, addr) == 0)
                     SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)bptext);
               }
               break;
            }
            case IDC_DELCODEBP:
            {
               LRESULT ret;
               char bptext[10];
               u32 addr=0;
               extern SH2Interface_struct *SH2Core;

               if (SH2Core->id != SH2CORE_DEBUGINTERPRETER)
                  break;

               if ((ret = SendDlgItemMessage(hDlg, IDC_CODEBPLB, LB_GETCURSEL, 0, 0)) != LB_ERR)
               {
                  SendDlgItemMessageA(hDlg, IDC_CODEBPLB, LB_GETTEXT, ret, (LPARAM)bptext);
                  sscanf(bptext, "%lX", &addr);
                  SH2DelCodeBreakpoint(debugsh, addr);
                  SendDlgItemMessage(hDlg, IDC_CODEBPLB, LB_DELETESTRING, ret, 0);
               }
               break;
            }
            case IDC_ADDMEMBP:
            {
               char bptext[10];
               u32 addr=0;
               u32 flags=0;

               memset(bptext, 0, 10);
               GetDlgItemTextA(hDlg, IDC_MEMBPET, bptext, 10);

               if (bptext[0] != 0)
               {
                  sscanf(bptext, "%lX", &addr);
                  sprintf(bptext, "%08lX", addr);

                  if (SendDlgItemMessage(hDlg, IDC_CHKREAD, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  {
                     if (SendDlgItemMessage(hDlg, IDC_CHKBYTE1, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_BYTEREAD;
                     if (SendDlgItemMessage(hDlg, IDC_CHKWORD1, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_WORDREAD;
                     if (SendDlgItemMessage(hDlg, IDC_CHKLONG1, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_LONGREAD;
                  }

                  if (SendDlgItemMessage(hDlg, IDC_CHKWRITE, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  {
                     if (SendDlgItemMessage(hDlg, IDC_CHKBYTE2, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_BYTEWRITE;
                     if (SendDlgItemMessage(hDlg, IDC_CHKWORD2, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_WORDWRITE;
                     if (SendDlgItemMessage(hDlg, IDC_CHKLONG2, BM_GETCHECK, 0, 0) == BST_CHECKED)
                        flags |= BREAK_LONGWRITE;
                  }

                  if (SH2AddMemoryBreakpoint(debugsh, addr, flags) == 0)
                     SendMessageA(GetDlgItem(hDlg, IDC_MEMBPLB), LB_ADDSTRING, 0, (LPARAM)bptext);
               }
               break;
            }
            case IDC_DELMEMBP:
            {
               LRESULT ret;
               char bptext[10];
               u32 addr=0;

               if ((ret = SendDlgItemMessage(hDlg, IDC_MEMBPLB, LB_GETCURSEL, 0, 0)) != LB_ERR)
               {
                  SendDlgItemMessageA(hDlg, IDC_MEMBPLB, LB_GETTEXT, ret, (LPARAM)bptext);
                  sscanf(bptext, "%lX", &addr);
                  SH2DelMemoryBreakpoint(debugsh, addr);
                  SendDlgItemMessage(hDlg, IDC_MEMBPLB, LB_DELETESTRING, ret, 0);
               }

               break;
            }
            case IDC_CHKREAD:
            {
               LRESULT ret;

               if (HIWORD(wParam) == BN_CLICKED)
               {
                  if ((ret = SendDlgItemMessage(hDlg, IDC_CHKREAD, BM_GETCHECK, 0, 0)) == BST_UNCHECKED)
                  {
                     SendDlgItemMessage(hDlg, IDC_CHKREAD, BM_SETCHECK, BST_CHECKED, 0);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKBYTE1), TRUE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKWORD1), TRUE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKLONG1), TRUE);
                  }
                  else if (ret == BST_CHECKED)
                  {
                     SendDlgItemMessage(hDlg, IDC_CHKREAD, BM_SETCHECK, BST_UNCHECKED, 0);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKBYTE1), FALSE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKWORD1), FALSE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKLONG1), FALSE);
                  }
               }

               break;
            }
            case IDC_CHKWRITE:
            {
               LRESULT ret;

               if (HIWORD(wParam) == BN_CLICKED)
               {
                  if ((ret = SendDlgItemMessage(hDlg, IDC_CHKWRITE, BM_GETCHECK, 0, 0)) == BST_UNCHECKED)
                  {
                     SendDlgItemMessage(hDlg, IDC_CHKWRITE, BM_SETCHECK, BST_CHECKED, 0);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKBYTE2), TRUE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKWORD2), TRUE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKLONG2), TRUE);
                  }
                  else if (ret == BST_CHECKED)
                  {
                     SendDlgItemMessage(hDlg, IDC_CHKWRITE, BM_SETCHECK, BST_UNCHECKED, 0);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKBYTE2), FALSE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKWORD2), FALSE);
                     EnableWindow(GetDlgItem(hDlg, IDC_CHKLONG2), FALSE);
                  }
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

                     sh2regs_struct sh2regs;
                     SH2GetRegisters(debugsh, &sh2regs);
                     cursel = (int)SendMessage(GetDlgItem(hDlg,LOWORD(wParam)), LB_GETCURSEL,0,0);

                     if (cursel < 16)
                     {
                        memaddr = sh2regs.R[cursel];
                     }
                     else if (cursel == 16)
                     {
                        memaddr = sh2regs.SR.all;
                     }
                     else if (cursel == 17)
                     {
                        memaddr = sh2regs.GBR;
                     }
                     else if (cursel == 18)
                     {
                        memaddr = sh2regs.VBR;
                     }
                     else if (cursel == 19)
                     {
                        memaddr = sh2regs.MACH;
                     }
                     else if (cursel == 20)
                     {
                        memaddr = sh2regs.MACL;
                     }
                     else if (cursel == 21)
                     {
                        memaddr = sh2regs.PR;
                     }
                     else if (cursel == 22)
                     {
                        memaddr = sh2regs.PC;
                     }

                     if (DialogBox(GetModuleHandle(0), MAKEINTRESOURCE(IDD_MEM), hDlg, (DLGPROC)MemDlgProc) != FALSE)
                     {
                        if (cursel < 16)
                        {
                           sh2regs.R[cursel] = memaddr;
                        }
                        else if (cursel == 16)
                        {
                           sh2regs.SR.all = memaddr;
                        }
                        else if (cursel == 17)
                        {
                           sh2regs.GBR = memaddr;
                        }
                        else if (cursel == 18)
                        {
                           sh2regs.VBR = memaddr;
                        }
                        else if (cursel == 19)
                        {
                           sh2regs.MACH = memaddr;
                        }
                        else if (cursel == 20)
                        {
                           sh2regs.MACL = memaddr;
                        }
                        else if (cursel == 21)
                        {
                           sh2regs.PR = memaddr;
                        }
                        else if (cursel == 22)
                        {
                           sh2regs.PC = memaddr;
                           SH2UpdateCodeList(hDlg, sh2regs.PC);
                        }
                     }

                     SH2SetRegisters(debugsh, &sh2regs);
                     SH2UpdateRegList(hDlg, &sh2regs);
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
                     extern SH2Interface_struct *SH2Core;

                     if (SH2Core->id != SH2CORE_DEBUGINTERPRETER)
                     {
                        MessageBox (hDlg, _16("Breakpoints only supported by SH2 Debug Interpreter"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                        break;
                     }

                     addr = (u32)SendMessage(GetDlgItem(hDlg,LOWORD(wParam)), DIS_GETCURSEL,0,0);
                     sprintf(bptext, "%08X", (int)addr);

                     if (SH2AddCodeBreakpoint(debugsh, addr) == 0)
                        SendMessageA(GetDlgItem(hDlg, IDC_CODEBPLB), LB_ADDSTRING, 0, (LPARAM)bptext);
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

