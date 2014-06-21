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
#include <commctrl.h>
#include <tchar.h>
#include "../resource.h"
#undef FASTCALL
#include "../../memory.h"
#include "../../scu.h"
#include "../../sh2d.h"
#include "../../vdp2debug.h"
#include "../../yui.h"
#include "../disasm.h"
#include "../hexedit.h"
#include "../settings/settings.h"
#include "yuidebug.h"
#include "../yuiwin.h"

u32 mtrnssaddress=0x06004000;
u32 mtrnseaddress=0x06100000;
TCHAR mtrnsfilename[MAX_PATH] = TEXT("\0");
int mtrnsreadwrite=1;
int mtrnssetpc=TRUE;

u32 memaddr=0;

SH2_struct *debugsh;

addrlist_struct hexaddrlist[13] = {
   { 0x00000000, 0x0007FFFF, "Bios ROM" },
   { 0x00180000, 0x0018FFFF, "Backup RAM" },
   { 0x00200000, 0x002FFFFF, "Low Work RAM" },
   { 0x02000000, 0x03FFFFFF, "A-bus CS0" },
   { 0x04000000, 0x04FFFFFF, "A-bus CS1" },
   { 0x05000000, 0x057FFFFF, "A-bus Dummy" },
   { 0x05800000, 0x058FFFFF, "A-bus CS2" },
   { 0x05A00000, 0x05AFFFFF, "68k RAM" },
   { 0x05C00000, 0x05C7FFFF, "VDP1 RAM" },
   { 0x05C80000, 0x05CBFFFF, "VDP1 Framebuffer" },
   { 0x05E00000, 0x05E7FFFF, "VDP2 RAM" },
   { 0x05F00000, 0x05F00FFF, "VDP2 Color RAM" },
   { 0x06000000, 0x060FFFFF, "High Work RAM" }
};

HWND LogWin=NULL;
char *logbuffer;
u32 logcounter=0;
u32 logsize=512;

//////////////////////////////////////////////////////////////////////////////

void SetupOFN(OPENFILENAME *ofn, int type, HWND hwnd, const LPCTSTR lpstrFilter, LPTSTR lpstrFile, DWORD nMaxFile)
{
   ZeroMemory(ofn, sizeof(OPENFILENAME));
   ofn->lStructSize = sizeof(OPENFILENAME);
   ofn->hwndOwner = hwnd;
   ofn->lpstrFilter = lpstrFilter;
   ofn->nFilterIndex = 1;
   ofn->lpstrFile = lpstrFile;
   ofn->nMaxFile = nMaxFile;

   switch (type)
   {
      case OFN_DEFAULTSAVE:
      {
         ofn->Flags = OFN_OVERWRITEPROMPT;
         break;
      }
      case OFN_DEFAULTLOAD:
      {
         ofn->Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
         break;
      }
   }

}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK ErrorDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                   LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         SetDlgItemText(hDlg, IDC_EDTEXT, _16((char *)lParam));
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_EDCONTINUE:
            {
               EndDialog(hDlg, FALSE);

               return TRUE;
            }
            case IDC_EDDEBUG:
            {
               EndDialog(hDlg, TRUE);

               return TRUE;
            }
            default: break;
         }
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

void YuiErrorMsg(const char *string)
{
   extern SH2Interface_struct *SH2Core;

   // This sucks, but until YuiErrorMsg is changed around, this will have to do
   if (strncmp(string, "Master SH2 invalid opcode", 25) == 0)
   {
      if (SH2Core->id == SH2CORE_DEBUGINTERPRETER)
      {
         if (DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_ERRORDEBUG), NULL, (DLGPROC)ErrorDebugDlgProc, (LPARAM)string) == TRUE)
         {
            debugsh = MSH2;
            DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SH2DEBUG), NULL, (DLGPROC)SH2DebugDlgProc);
         }
      }
   }
   else if (strncmp(string, "Slave SH2 invalid opcode", 24) == 0)
   {
      if (SH2Core->id == SH2CORE_DEBUGINTERPRETER)
      {
         if (DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_ERRORDEBUG), NULL, (DLGPROC)ErrorDebugDlgProc, (LPARAM)string) == TRUE)
         {
            debugsh = SSH2;
            DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SH2DEBUG), NULL, (DLGPROC)SH2DebugDlgProc);
         }
      }
   }
   else
      MessageBox (YabWin, _16(string), _16("Error"),  MB_OK | MB_ICONINFORMATION);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK MemTransferDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam)
{
   char tempstr[MAX_PATH];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         SetDlgItemText(hDlg, IDC_EDITTEXT1, mtrnsfilename);

         sprintf(tempstr, "%08X", (int)mtrnssaddress);
         SetDlgItemTextA(hDlg, IDC_EDITTEXT2, tempstr);

         sprintf(tempstr, "%08X", (int)mtrnseaddress);
         SetDlgItemTextA(hDlg, IDC_EDITTEXT3, tempstr);

         if (mtrnsreadwrite == 0)
         {
            SendMessage(GetDlgItem(hDlg, IDC_DOWNLOADMEM), BM_SETCHECK, BST_CHECKED, 0);
            SendMessage(GetDlgItem(hDlg, IDC_UPLOADMEM), BM_SETCHECK, BST_UNCHECKED, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_EDITTEXT3), TRUE);
            EnableWindow(GetDlgItem(hDlg, IDC_CHECKBOX1), FALSE);
         }
         else
         {
            SendMessage(GetDlgItem(hDlg, IDC_DOWNLOADMEM), BM_SETCHECK, BST_UNCHECKED, 0);
            SendMessage(GetDlgItem(hDlg, IDC_UPLOADMEM), BM_SETCHECK, BST_CHECKED, 0);
            if (mtrnssetpc)
               SendMessage(GetDlgItem(hDlg, IDC_CHECKBOX1), BM_SETCHECK, BST_CHECKED, 0);
            EnableWindow(GetDlgItem(hDlg, IDC_EDITTEXT3), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_CHECKBOX1), TRUE);
         }

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_BROWSE:
            {
               OPENFILENAME ofn;

               if (SendMessage(GetDlgItem(hDlg, IDC_DOWNLOADMEM), BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  WCHAR filter[1024];

                  CreateFilter(filter, 1024,
                     "All files (*.*)", "*.*",
                     "Binary Files", "*.BIN", NULL);

                  SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter,
                           mtrnsfilename, sizeof(mtrnsfilename)/sizeof(TCHAR));

                  if (GetSaveFileName(&ofn))
                     SetDlgItemText(hDlg, IDC_EDITTEXT1, mtrnsfilename);
               }
               else
               {
                  WCHAR filter[1024];

                  CreateFilter(filter, 1024,
                     "All files (*.*)", "*.*",
                     "Binary Files", "*.BIN",
                     "COFF Files", "*.COF;*.COFF", NULL);

                  // setup ofn structure
                  SetupOFN(&ofn, OFN_DEFAULTLOAD, hDlg, filter,
                           mtrnsfilename, sizeof(mtrnsfilename)/sizeof(TCHAR));

                  if (GetOpenFileName(&ofn))
                     SetDlgItemText(hDlg, IDC_EDITTEXT1, mtrnsfilename);
               }

               return TRUE;
            }
            case IDOK:
            {
               GetDlgItemText(hDlg, IDC_EDITTEXT1, mtrnsfilename, MAX_PATH);
               
               GetDlgItemTextA(hDlg, IDC_EDITTEXT2, tempstr, 9);
               sscanf(tempstr, "%08lX", &mtrnssaddress);

               GetDlgItemTextA(hDlg, IDC_EDITTEXT3, tempstr, 9);
               sscanf(tempstr, "%08lX", &mtrnseaddress);

               if ((mtrnseaddress - mtrnssaddress) < 0)
               {
                  MessageBox (hDlg, _16("Invalid Start/End Address Combination"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                  EndDialog(hDlg, TRUE);
                  return FALSE;
               }

               WideCharToMultiByte(CP_ACP, 0, mtrnsfilename, -1, tempstr, sizeof(tempstr), NULL, NULL);

               if (SendMessage(GetDlgItem(hDlg, IDC_DOWNLOADMEM), BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  // Let's do a ram dump
                  MappedMemorySave(tempstr, mtrnssaddress, mtrnseaddress - mtrnssaddress);
                  mtrnsreadwrite = 0;
               }
               else
               {
                  // upload to ram and possibly execute
                  mtrnsreadwrite = 1;

                  // Is this a program?
                  if (SendMessage(GetDlgItem(hDlg, IDC_CHECKBOX1), BM_GETCHECK, 0, 0) == BST_CHECKED)
                  {
                     MappedMemoryLoadExec(tempstr, mtrnssaddress);
                     mtrnssetpc = TRUE;
                  }
                  else
                  {
                     MappedMemoryLoad(tempstr, mtrnssaddress);
                     mtrnssetpc = FALSE;
                  }
               }

               EndDialog(hDlg, TRUE);

               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);

               return TRUE;
            }
            case IDC_UPLOADMEM:
            {
               if (HIWORD(wParam) == BN_CLICKED)
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_EDITTEXT3), FALSE);
                  EnableWindow(GetDlgItem(hDlg, IDC_CHECKBOX1), TRUE);
               }

               break;
            }
            case IDC_DOWNLOADMEM:
            {
               if (HIWORD(wParam) == BN_CLICKED)
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_EDITTEXT3), TRUE);
                  EnableWindow(GetDlgItem(hDlg, IDC_CHECKBOX1), FALSE);
               }
               break;
            }
            default: break;
         }
         break;
      }

      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

void DebugMouseWheel(HWND hctl, WPARAM wParam)
{
   if (HIWORD(wParam) < 0x8000)
      PostMessage(hctl, WM_VSCROLL, MAKEWPARAM(SB_LINEUP, 0), (LPARAM)NULL);
   else if (HIWORD(wParam) >= 0x8000)
      PostMessage(hctl, WM_VSCROLL, MAKEWPARAM(SB_LINEDOWN, 0), (LPARAM)NULL);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK MemDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         char buf[9];

         sprintf(buf, "%08lX", memaddr);
         SetDlgItemTextA(hDlg, IDC_EDITTEXT1, buf);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (wParam)
         {
            case IDOK:
            {
               char buf[9];

               EndDialog(hDlg, TRUE);
               GetDlgItemTextA(hDlg, IDC_EDITTEXT1, buf, sizeof(buf));

               sscanf(buf, "%08lx", &memaddr);

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
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

int SaveBitmap(const char *filename, int width, int height, u32 *data)
{
   BITMAPFILEHEADER fileheader;
   BITMAPV4HEADER bmi;
   FILE *fp;
   int i;

   if (!filename)
      return -1;

   if ((fp = fopen(filename, "wb")) == NULL)
      return -1;

   memset(&fileheader, 0, sizeof(fileheader));
   fileheader.bfType = 'B' | ('M' << 8);
   fileheader.bfSize = sizeof(fileheader) + sizeof(bmi) + (width * height * 4);
   fileheader.bfOffBits = sizeof(fileheader)+sizeof(bmi);
   fwrite((void *)&fileheader, 1, sizeof(fileheader), fp);

   memset(&bmi, 0, sizeof(bmi));
   bmi.bV4Size = sizeof(bmi);
   bmi.bV4Planes = 1;
   bmi.bV4BitCount = 32;
   bmi.bV4V4Compression = BI_RGB | BI_BITFIELDS;
   bmi.bV4RedMask = 0x000000FF;
   bmi.bV4GreenMask = 0x0000FF00;
   bmi.bV4BlueMask = 0x00FF0000;
   bmi.bV4AlphaMask = 0xFF000000;
   bmi.bV4Width = width;
   bmi.bV4Height = -height;
   fwrite((void *)&bmi, 1, sizeof(bmi), fp);

   for (i = 0; i < height; i++)
   {
      fwrite(data, 1, width * sizeof(u32), fp);
      data += width;
   }
   fclose(fp);

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK GotoAddressDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam)
{
   static u32 *addr;
   char tempstr[9];
   int i;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         addr = (u32 *)lParam;
         sprintf(tempstr, "%08lX", addr[0]);
         SetDlgItemTextA(hDlg, IDC_OFFSETET, tempstr);

         SendDlgItemMessage(hDlg, IDC_SPECIFYADDRRB, BM_SETCHECK, BST_CHECKED, 0);
         SendDlgItemMessage(hDlg, IDC_PRESETADDRRB, BM_SETCHECK, BST_UNCHECKED, 0);

         EnableWindow(GetDlgItem(hDlg, IDC_OFFSETET), TRUE);
         EnableWindow(GetDlgItem(hDlg, IDC_PRESETLISTCB), FALSE);

         SendDlgItemMessage(hDlg, IDC_PRESETLISTCB, CB_RESETCONTENT, 0, 0);
         for (i = 0; i < 13; i++)
         {
            SendDlgItemMessageA(hDlg, IDC_PRESETLISTCB, CB_ADDSTRING, 0, (LPARAM)hexaddrlist[i].name);
            if (addr[0] >= hexaddrlist[i].start && addr[0] <= hexaddrlist[i].end)
               SendDlgItemMessage(hDlg, IDC_PRESETLISTCB, CB_SETCURSEL, i, 0);
         }
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
            {
               if (SendDlgItemMessage(hDlg, IDC_SPECIFYADDRRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
               {
                  GetDlgItemTextA(hDlg, IDC_OFFSETET, tempstr, 9);
                  sscanf(tempstr, "%08lX", addr);
               }
               else
                  addr[0] = hexaddrlist[SendDlgItemMessage(hDlg, IDC_PRESETLISTCB, CB_GETCURSEL, 0, 0)].start;
               EndDialog(hDlg, TRUE);
               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            case IDC_SPECIFYADDRRB:
            {
               if (HIWORD(wParam) == BN_CLICKED)
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_OFFSETET), TRUE);
                  EnableWindow(GetDlgItem(hDlg, IDC_PRESETLISTCB), FALSE);
               }

               break;
            }
            case IDC_PRESETADDRRB:
            {
               if (HIWORD(wParam) == BN_CLICKED)
               {
                  EnableWindow(GetDlgItem(hDlg, IDC_OFFSETET), FALSE);
                  EnableWindow(GetDlgItem(hDlg, IDC_PRESETLISTCB), TRUE);
               }
               break;
            }
            default: break;
         }
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

typedef struct
{
   char searchstr[1024];
   int searchtype;
   u32 startaddr;
   u32 endaddr;
   result_struct *results;
   HWND hDlg;
} searcharg_struct;

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK SearchMemoryDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                     LPARAM lParam)
{
   static searcharg_struct *searcharg;
   char tempstr[10];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         int cursel=0;

         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_RESETCONTENT, 0, 0);
         
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Hex value(s)"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Text"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("8-bit Relative value(s)"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("16-bit Relative value(s)"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Unsigned 8-bit value"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Signed 8-bit value"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Unsigned 16-bit value"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Signed 16-bit value"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Unsigned 32-bit value"));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_ADDSTRING, 0, (LPARAM)_16("Signed 32-bit value"));
         searcharg = (searcharg_struct *)lParam;

         switch (searcharg->searchtype & 0x70)
         {
            case SEARCHSIGNED:
               cursel += 1;
            case SEARCHUNSIGNED:
               cursel += 4 + ((searcharg->searchtype & 0x3) * 2);
               break;
            case SEARCHSTRING:
               cursel = 1;
               break;
            case SEARCHREL8BIT:
               cursel = 2;
               break;
            case SEARCHREL16BIT:
               cursel = 3;
               break;
            case SEARCHHEX:
            default: break;
         }

         SetDlgItemText(hDlg, IDC_SEARCHMEMET, _16(searcharg->searchstr));
         SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_SETCURSEL, cursel, 0);

         sprintf(tempstr, "%08X", (int)searcharg->startaddr);
         SetDlgItemText(hDlg, IDC_SEARCHSTARTADDRET, _16(tempstr));

         sprintf(tempstr, "%08X", (int)searcharg->endaddr);
         SetDlgItemText(hDlg, IDC_SEARCHENDADDRET, _16(tempstr));

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
            {
               WCHAR tempwstr[1024];
               int cursel=(int)SendDlgItemMessage(hDlg, IDC_SEARCHTYPECB, CB_GETCURSEL, 0, 0);

               switch(cursel)
               {
                  case 0:
                     searcharg->searchtype = SEARCHHEX;
                     break;
                  case 1:
                     searcharg->searchtype = SEARCHSTRING;
                     break;
                  case 2:
                     searcharg->searchtype = SEARCHREL8BIT;
                     break;
                  case 3:
                     searcharg->searchtype = SEARCHREL16BIT;
                     break;
                  case 4:
                     searcharg->searchtype = SEARCHBYTE | SEARCHUNSIGNED;
                     break;
                  case 5:
                     searcharg->searchtype = SEARCHBYTE | SEARCHSIGNED;
                     break;
                  case 6:
                     searcharg->searchtype = SEARCHWORD | SEARCHUNSIGNED;
                     break;
                  case 7:
                     searcharg->searchtype = SEARCHWORD | SEARCHSIGNED;
                     break;
                  case 8:
                     searcharg->searchtype = SEARCHLONG | SEARCHUNSIGNED;
                     break;
                  case 9:
                     searcharg->searchtype = SEARCHLONG | SEARCHSIGNED;
                     break;
               }

               GetDlgItemText(hDlg, IDC_SEARCHMEMET, tempwstr, 1024);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, searcharg->searchstr, 1024, NULL, NULL);
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
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

static int KillSearchThread=0;
static HANDLE hThread=INVALID_HANDLE_VALUE;
static DWORD thread_id;
#define SEARCHSIZE      0x10000

DWORD WINAPI __stdcall SearchThread(void *b)
{    
   result_struct *results;
   u32 startaddr;
   u32 endaddr;
   searcharg_struct *searcharg=(searcharg_struct *)b;

   startaddr=searcharg->startaddr;
   endaddr=searcharg->endaddr;

   PostMessage(GetDlgItem(searcharg->hDlg, IDC_SEARCHPB), PBM_SETRANGE, 0, MAKELPARAM (0, (endaddr-startaddr) / SEARCHSIZE));
   PostMessage(GetDlgItem(searcharg->hDlg, IDC_SEARCHPB), PBM_SETSTEP, 1, 0);

   while (KillSearchThread != 1)
   {
      u32 numresults=1;

      if ((searcharg->endaddr - startaddr) > SEARCHSIZE)
         endaddr = startaddr+SEARCHSIZE;
      else
         endaddr = searcharg->endaddr;

      results = MappedMemorySearch(startaddr, endaddr,
                                   searcharg->searchtype | SEARCHEXACT,
                                   searcharg->searchstr,
                                   NULL, &numresults);
      if (results && numresults)
      {
         // We're done
         searcharg->results = results;          
         EndDialog(searcharg->hDlg, TRUE);
         return 0;
      }

      if (results)
         free(results);

      startaddr += (endaddr - startaddr);
      if (startaddr >= searcharg->endaddr)
      {
         EndDialog(searcharg->hDlg, TRUE);
         searcharg->results = NULL;
         return 0;
      }

      PostMessage(GetDlgItem(searcharg->hDlg, IDC_SEARCHPB), PBM_STEPIT, 0, 0);
   }
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK SearchBusyDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                   LPARAM lParam)
{
   static searcharg_struct *searcharg;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         // Create thread
         KillSearchThread=0;
         searcharg = (searcharg_struct *)lParam;
         searcharg->hDlg = hDlg;
         hThread = CreateThread(NULL,0,(LPTHREAD_START_ROUTINE) SearchThread,(void *)lParam,0,&thread_id);
         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDCANCEL:
            {
               // Kill thread
               KillSearchThread = 1;
               if (WaitForSingleObject(hThread, INFINITE) == WAIT_TIMEOUT)
               {
                  // Couldn't close thread cleanly
                  TerminateThread(hThread,0);                                  
               }          
               CloseHandle(hThread);
               hThread = INVALID_HANDLE_VALUE;
               searcharg->results = NULL;
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            default: break;
         }
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK MemoryEditorDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                     LPARAM lParam)
{
   static searcharg_struct searcharg;

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         SendDlgItemMessage(hDlg, IDC_HEXEDIT, HEX_SETADDRESSLIST, 13, (LPARAM)hexaddrlist);
         searcharg.startaddr = 0;
         searcharg.endaddr = 0x06100000;
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
            case IDC_GOTOADDRESS:
            {
               u32 addr=0x06000000;

               if (DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_GOTOADDRESS), hDlg, (DLGPROC)GotoAddressDlgProc, (LPARAM)&addr) == TRUE)
               {
                  SendDlgItemMessage(hDlg, IDC_HEXEDIT, HEX_GOTOADDRESS, 0, addr);
                  SendMessage(hDlg, WM_NEXTDLGCTL, IDC_HEXEDIT, TRUE);
               }
               break;
            }
            case IDC_SEARCHMEM:
            {
               searcharg.startaddr = (u32)SendDlgItemMessage(hDlg, IDC_HEXEDIT, HEX_GETCURADDRESS, 0, 0);
               
               if (DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_SEARCHMEMORY), hDlg,
                                  (DLGPROC)SearchMemoryDlgProc,
                                  (LPARAM)&searcharg) == TRUE)
               {
                  // Open up searching dialog
                  if (DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_SEARCHBUSY), hDlg,
                                     (DLGPROC)SearchBusyDlgProc,
                                     (LPARAM)&searcharg) == TRUE)
                  {
                     if (searcharg.results)
                     {
                        // Ok, we found a match, go to that address
                        SendDlgItemMessage(hDlg, IDC_HEXEDIT, HEX_GOTOADDRESS, 0, searcharg.results[0].addr);
                        free(searcharg.results);
                     }
                     else
                     {
                        // No matches found, if the search wasn't from bios start,
                        // ask the user if they want to search from the begining.

                        if (SendDlgItemMessage(hDlg, IDC_HEXEDIT, HEX_GETCURADDRESS, 0, 0) != 0)
                           MessageBox (hDlg, _16("Finished searching up to end of memory, continue from the beginning?"), _16("Wrap search?"), MB_OKCANCEL);
                        else
                           MessageBox (hDlg, _16("No matches found"), _16("Finished search"), MB_OK);
                     }
                  }
               }
               break;
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

LRESULT CALLBACK LogDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                            LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
         // Use the maximum available characters
         SendDlgItemMessage(hDlg, IDC_LOGET, EM_LIMITTEXT, 0, 0); 
         return TRUE;
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_CLEARBT:
               SetDlgItemText(hDlg, IDC_LOGET, _16(""));
               return TRUE;
            case IDC_SAVELOGBT:
            {
               OPENFILENAME ofn;
               WCHAR filter[1024];
               TCHAR filename[MAX_PATH]=TEXT("\0");

               CreateFilter(filter, 1024,
                  "Text Files", "*.txt",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter, filename, sizeof(filename)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("TXT");

               if (GetSaveFileName(&ofn))
               {
                  FILE *fp=_tfopen(filename, TEXT("wb"));
                  HLOCAL *localbuf=(HLOCAL *)SendDlgItemMessage(hDlg, IDC_LOGET, EM_GETHANDLE, 0, 0);
                  TCHAR *buf;
                  char *buf2;                  

                  if (fp == NULL)
                  {
                     MessageBox (hDlg, _16("Unable to open file for writing"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                     return FALSE;
                  }

                  buf = LocalLock(localbuf);
                  if (buf2 = malloc(lstrlen(buf)+1))
                  {
                     WideCharToMultiByte(CP_ACP, 0, buf, -1, buf2, lstrlen(buf)+1, NULL, NULL);
                     fwrite((void *)buf2, 1, strlen(buf2), fp);
                     free(buf2);
                  }
                  LocalUnlock(localbuf);
                  fclose(fp);
               }

               return TRUE;
            }
            default: break;
         }
         break;
      }
      case WM_DESTROY:
      {
         KillTimer(hDlg, 1);
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

void UpdateLogCallback (char *string)
{
   int len = GetWindowTextLength(GetDlgItem(LogWin, IDC_LOGET));
   sprintf(logbuffer, "%s\r\n", string);
   SendDlgItemMessage(LogWin, IDC_LOGET, EM_SETSEL, len, len);
   SendDlgItemMessage(LogWin, IDC_LOGET, EM_REPLACESEL, FALSE, (LPARAM)_16(logbuffer));  
}

//////////////////////////////////////////////////////////////////////////////
