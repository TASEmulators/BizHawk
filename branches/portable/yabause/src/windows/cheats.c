/*  Copyright 2007-2008 Theo Berkau

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
#include "cheats.h"
#include "../cheat.h"
#include "resource.h"
#include "settings/settings.h"
#include "../memory.h"
#include "cpudebug/yuidebug.h"

extern HINSTANCE y_hInstance;

TCHAR cheatfilename[MAX_PATH] = TEXT("\0");

typedef struct
{
   u32 addr;
   u32 val;
} addcode_struct;

//////////////////////////////////////////////////////////////////////////////

void AddCode(HWND hParent, int i)
{
   LVITEM itemdata;
   char code[MAX_PATH];
   cheatlist_struct *cheat;
   int cheatnum;   

   cheat = CheatGetList(&cheatnum);

   switch(cheat[i].type)
   {
      case CHEATTYPE_ENABLE:
         sprintf(code, "Enable code: %08X %08X", (int)cheat[i].addr, (int)cheat[i].val);
         break;
      case CHEATTYPE_BYTEWRITE:
         sprintf(code, "Byte Write: %08X %02X", (int)cheat[i].addr, (int)cheat[i].val);
         break;
      case CHEATTYPE_WORDWRITE:
         sprintf(code, "Word write: %08X %04X", (int)cheat[i].addr, (int)cheat[i].val);
         break;
      case CHEATTYPE_LONGWRITE:
         sprintf(code, "Long write: %08X %08X", (int)cheat[i].addr, (int)cheat[i].val);
         break;
      default: break;
   }

   itemdata.mask = LVIF_TEXT;
   itemdata.iItem = (int)SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_GETITEMCOUNT, 0, 0);
   itemdata.iSubItem = 0;
   itemdata.pszText = _16(code);
   itemdata.cchTextMax = (int)wcslen(itemdata.pszText);
   SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_INSERTITEM, 0, (LPARAM)&itemdata);

   itemdata.iSubItem = 1;
   itemdata.pszText = _16(cheat[i].desc);
   itemdata.cchTextMax = (int)wcslen(itemdata.pszText);
   SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_SETITEM, 0, (LPARAM)&itemdata);

   itemdata.iSubItem = 2;
   if (cheat[i].enable)
      itemdata.pszText = _16("Enabled");
   else
      itemdata.pszText = _16("Disabled");
      
   itemdata.cchTextMax = (int)wcslen(itemdata.pszText);
   SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_SETITEM, 0, (LPARAM)&itemdata);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK AddARCodeDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
         SendDlgItemMessage(hDlg, IDC_CODE, EM_LIMITTEXT, 13, 0);
         Button_Enable(GetDlgItem(hDlg, IDOK), FALSE);
         return TRUE;
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
            {
               WCHAR wcode[MAX_PATH];
               WCHAR wdesc[MAX_PATH];
               char code[MAX_PATH];
               char desc[MAX_PATH];
               int cheatnum;

               GetDlgItemText(hDlg, IDC_CODE, wcode, 14);
               WideCharToMultiByte(CP_ACP, 0, wcode, -1, code, MAX_PATH, NULL, NULL);

               // Should verify text
               if (strlen(code) < 12)
               {
                   MessageBox (hDlg, _16("Invalid code. Should be in the format: XXXXXXXX YYYY"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                   return TRUE;
               }

               if (CheatAddARCode(code) != 0)
               {
                   MessageBox (hDlg, _16("Invalid code. Should be in the format: XXXXXXXX YYYY"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                   return TRUE;
               }

               GetDlgItemText(hDlg, IDC_CODEDESC, wdesc, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, wdesc, -1, desc, MAX_PATH, NULL, NULL);
               CheatGetList(&cheatnum);
               CheatChangeDescriptionByIndex(cheatnum-1, desc);
               AddCode(GetParent(hDlg), cheatnum-1);

               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_CLEARCODES), TRUE);
               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_SAVETOFILE), TRUE);

               EndDialog(hDlg, TRUE);
               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            case IDC_CODE:
            {
               if (HIWORD(wParam) == EN_CHANGE)
               {
                  WCHAR wtext[14];
                  LRESULT ret;

                  if ((ret = GetDlgItemText(hDlg, IDC_CODE, wtext, 14)) <= 0)
                     Button_Enable(GetDlgItem(hDlg, IDOK), FALSE);
                  else 
                     Button_Enable(GetDlgItem(hDlg, IDOK), TRUE);
               }
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

LRESULT CALLBACK AddCodeDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
         SendDlgItemMessage(hDlg, IDC_CODEADDR, EM_LIMITTEXT, 8, 0);
         SendDlgItemMessage(hDlg, IDC_CODEVAL, EM_LIMITTEXT, 3, 0);
         Button_Enable(GetDlgItem(hDlg, IDOK), FALSE);
         SendDlgItemMessage(hDlg, IDC_CTBYTEWRITE, BM_SETCHECK, BST_CHECKED, 0);
         return TRUE;
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
            {
               WCHAR tempwstr[MAX_PATH];
               char tempstr[MAX_PATH];
               char desc[MAX_PATH];
               int type;
               u32 addr;
               u32 val;
               int cheatnum;

               // Get address
               GetDlgItemText(hDlg, IDC_CODEADDR, tempwstr, 9);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);
               if (sscanf(tempstr, "%08lX", &addr) != 1)
               {
                  MessageBox (hDlg, _16("Invalid Address"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                  return TRUE;
               }

               // Get value
               GetDlgItemText(hDlg, IDC_CODEVAL, tempwstr, 11);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);
               if (sscanf(tempstr, "%ld", &val) != 1)
               {
                  MessageBox (hDlg, _16("Invalid Value"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                  return TRUE;
               }

               // Get type
               if (SendDlgItemMessage(hDlg, IDC_CTENABLE, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  type = CHEATTYPE_ENABLE;
               else if (SendDlgItemMessage(hDlg, IDC_CTBYTEWRITE, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  type = CHEATTYPE_BYTEWRITE;
               else if (SendDlgItemMessage(hDlg, IDC_CTWORDWRITE, BM_GETCHECK, 0, 0) == BST_CHECKED)
                  type = CHEATTYPE_WORDWRITE;
               else
                  type = CHEATTYPE_LONGWRITE;

               if (CheatAddCode(type, addr, val) != 0)
               {
                   MessageBox (hDlg, _16("Unable to add code"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                   return TRUE;
               }

               GetDlgItemText(hDlg, IDC_CODEDESC, tempwstr, MAX_PATH);
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, desc, MAX_PATH, NULL, NULL);
               CheatGetList(&cheatnum);
               CheatChangeDescriptionByIndex(cheatnum-1, desc);
               AddCode(GetParent(hDlg), cheatnum-1);

               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_CLEARCODES), TRUE);
               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_SAVETOFILE), TRUE);

               EndDialog(hDlg, TRUE);
               return TRUE;
            }
            case IDCANCEL:
            {
               EndDialog(hDlg, FALSE);
               return TRUE;
            }
            case IDC_CTENABLE:
            {
               if (HIWORD(wParam) == BN_CLICKED)
                  SendDlgItemMessage(hDlg, IDC_CODEVAL, EM_LIMITTEXT, 10, 0);
               break;
            }
            case IDC_CTBYTEWRITE:
            {
               if (HIWORD(wParam) == BN_CLICKED)
                  SendDlgItemMessage(hDlg, IDC_CODEVAL, EM_LIMITTEXT, 3, 0);
               break;
            }
            case IDC_CTWORDWRITE:
            {
               if (HIWORD(wParam) == BN_CLICKED)
                  SendDlgItemMessage(hDlg, IDC_CODEVAL, EM_LIMITTEXT, 5, 0);
               break;
            }
            case IDC_CTLONGWRITE:
            {
               if (HIWORD(wParam) == BN_CLICKED)
                  SendDlgItemMessage(hDlg, IDC_CODEVAL, EM_LIMITTEXT, 10, 0);
               break;
            }
            case IDC_CODEADDR:
            case IDC_CODEVAL:
            {
               if (HIWORD(wParam) == EN_CHANGE)
               {
                  WCHAR text[11];

                  if (GetDlgItemText(hDlg, IDC_CODEADDR, text, 9) <= 0 ||
                      GetDlgItemText(hDlg, IDC_CODEVAL, text, 11) <= 0)
                     Button_Enable(GetDlgItem(hDlg, IDOK), FALSE);
                  else 
                     Button_Enable(GetDlgItem(hDlg, IDOK), TRUE);
               }
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

LRESULT CALLBACK AddCodeDlgProc2(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         int cursel;
         char text[MAX_PATH];
         HWND hParent=GetParent(hDlg);
         AddCodeDlgProc(hDlg, uMsg, wParam, lParam);

         SendDlgItemMessage(hDlg, IDC_CTBYTEWRITE, BM_SETCHECK, 
                            SendDlgItemMessage(hParent, IDC_8BITRB, BM_GETCHECK, 0, 0), 0);
         SendDlgItemMessage(hDlg, IDC_CTWORDWRITE, BM_SETCHECK, 
                            SendDlgItemMessage(hParent, IDC_16BITRB, BM_GETCHECK, 0, 0), 0);
         SendDlgItemMessage(hDlg, IDC_CTLONGWRITE, BM_SETCHECK, 
                            SendDlgItemMessage(hParent, IDC_32BITRB, BM_GETCHECK, 0, 0), 0);

         // Get selected address and value, then set it to controls
         if ((cursel=(int)SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_GETNEXTITEM, -1, LVNI_SELECTED)) != -1)
         {
            LVITEM item;

            item.mask = LVIF_TEXT;
            item.iItem = cursel;
            item.iSubItem = 0;
            item.pszText = _16(text);
            item.cchTextMax = sizeof(_16(text));
            SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_GETITEM, 0, (LPARAM)&item);
            SetDlgItemText(hDlg, IDC_CODEADDR, _16(text));

            item.iSubItem = 1;
            SendDlgItemMessage(hParent, IDC_CHEATLIST, LVM_GETITEM, 0, (LPARAM)&item);
            SetDlgItemText(hDlg, IDC_CODEVAL, _16(text));
         }
         else
            return FALSE;
        
         return TRUE;
      }
      default: break;
   }

   return AddCodeDlgProc(hDlg, uMsg, wParam, lParam);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK CheatListDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   int cheatnum;
   int i;
   char text[MAX_PATH];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         LVCOLUMN coldata;

         ListView_SetExtendedListViewStyleEx(GetDlgItem(hDlg, IDC_CHEATLIST),
                                             LVS_EX_FULLROWSELECT, LVS_EX_FULLROWSELECT);

         coldata.mask = LVCF_TEXT | LVCF_WIDTH;
         coldata.pszText = _16("Code\0");
         coldata.cchTextMax = (int)wcslen(coldata.pszText);
         coldata.cx = 190;
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTCOLUMN, (WPARAM)0, (LPARAM)&coldata);

         coldata.pszText = _16("Description\0");
         coldata.cchTextMax = (int)wcslen(coldata.pszText);
         coldata.cx = 111;
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTCOLUMN, (WPARAM)1, (LPARAM)&coldata);

         coldata.pszText = _16("Status\0");
         coldata.cchTextMax = (int)wcslen(coldata.pszText);
         coldata.cx = 70;
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTCOLUMN, (WPARAM)2, (LPARAM)&coldata);

         // Generate cheat list
         CheatGetList(&cheatnum);

         for (i = 0; i < cheatnum; i++)
         {
            // Add code to code list
            AddCode(hDlg, i);
         }

         if (cheatnum == 0)
         {
            EnableWindow(GetDlgItem(hDlg, IDC_CLEARCODES), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_SAVETOFILE), FALSE);
         }
         else
            EnableWindow(GetDlgItem(hDlg, IDC_SAVETOFILE), TRUE);

         EnableWindow(GetDlgItem(hDlg, IDC_DELETECODE), FALSE);
         EnableWindow(GetDlgItem(hDlg, IDC_ADDFROMFILE), TRUE);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_ADDAR:
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_ADDARCODE), hDlg, (DLGPROC)AddARCodeDlgProc);
               break;
            case IDC_ADDRAWMEMADDR:
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_ADDCODE), hDlg, (DLGPROC)AddCodeDlgProc);
               break;
            case IDC_SAVETOFILE:
            {
               WCHAR filter[1024];
               OPENFILENAME ofn;

               CreateFilter(filter, 1024,
                  "Yabause Cheat Files", "*.YCT",
                  "All files (*.*)", "*.*", NULL);

               SetupOFN(&ofn, OFN_DEFAULTSAVE, hDlg, filter,
                        cheatfilename, sizeof(cheatfilename)/sizeof(TCHAR));
               ofn.lpstrDefExt = _16("YCT");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, cheatfilename, -1, text, sizeof(text), NULL, NULL);
                  if (CheatSave(text) != 0)
                     MessageBox (hDlg, _16("Unable to open file for saving"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
               }
               break;
            }
            case IDC_ADDFROMFILE:
            {
               WCHAR filter[1024];
               OPENFILENAME ofn;

               CreateFilter(filter, 1024,
                  "Yabause Cheat Files", "*.YCT",
                  "All files (*.*)", "*.*", NULL);

               // setup ofn structure
               SetupOFN(&ofn, OFN_DEFAULTLOAD, hDlg, filter,
                        cheatfilename, sizeof(cheatfilename)/sizeof(TCHAR));

               if (GetOpenFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, cheatfilename, -1, text, sizeof(text), NULL, NULL);
                  if (CheatLoad(text) == 0)
                  {
                     EnableWindow(GetDlgItem(GetParent(hDlg), IDC_SAVETOFILE), TRUE);
                     SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_DELETEALLITEMS, 0, 0);

                     // Generate cheat list
                     CheatGetList(&cheatnum);

                     for (i = 0; i < cheatnum; i++)
                     {
                        // Add code to code list
                        AddCode(hDlg, i);
                     }

                     if (cheatnum == 0)
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_CLEARCODES), FALSE);
                        EnableWindow(GetDlgItem(hDlg, IDC_SAVETOFILE), FALSE);
                     }
                     else
                     {
                        EnableWindow(GetDlgItem(hDlg, IDC_CLEARCODES), TRUE);
                        EnableWindow(GetDlgItem(hDlg, IDC_SAVETOFILE), TRUE);
                     }
                  }
                  else
                     MessageBox (hDlg, _16("Unable to open file for saving"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
               }

               break;
            }
            case IDC_DELETECODE:
            {
               int cursel=(int)SendDlgItemMessage(hDlg, IDC_CHEATLIST,
                                                  LVM_GETNEXTITEM, -1,
                                                  LVNI_SELECTED);
               if (cursel != -1)
               {
                  if (CheatRemoveCodeByIndex(cursel) != 0)
                  {
                     MessageBox (hDlg, _16("Unable to remove code"), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                     return TRUE;
                  }

                  SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_DELETEITEM, cursel, 0);
               }
               break;
            }
            case IDC_CLEARCODES:
               CheatClearCodes();
               SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_DELETEALLITEMS, 0, 0);
               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_CLEARCODES), FALSE);
               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_DELETECODE), FALSE);
               EnableWindow(GetDlgItem(GetParent(hDlg), IDC_SAVETOFILE), FALSE);
               break;
            case IDCANCEL:
            case IDOK:
            {
               EndDialog(hDlg, TRUE);
               return TRUE;
            }
            default: break;
         }
         break;
      }
      case WM_NOTIFY:
      {
         NMHDR *hdr;

         hdr = (NMHDR *)lParam;

         switch (hdr->idFrom)
         {
            case IDC_CHEATLIST:
            {
               switch(hdr->code)
               {
                  case NM_DBLCLK:
                  {
                     int cursel=(int)SendDlgItemMessage(hDlg, IDC_CHEATLIST,
                                                        LVM_GETNEXTITEM, -1,
                                                        LVNI_SELECTED);

                     if (cursel != -1)
                     {                        
                        // Enable or disable code
                        LVITEM itemdata;
                        WCHAR tempwstr[MAX_PATH];

                        itemdata.mask = LVIF_TEXT;
                        itemdata.iItem = cursel;
                        itemdata.iSubItem = 2;
                        itemdata.pszText = tempwstr;
                        itemdata.cchTextMax = MAX_PATH;
                        SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_GETITEM, 0, (LPARAM)&itemdata);
                        if (wcscmp(tempwstr, _16("Enabled")) == 0)
                        {
                           CheatDisableCode(cursel);
                           wcscpy(tempwstr, _16("Disabled"));
                        }
                        else
                        {
                           CheatEnableCode(cursel);
                           wcscpy(tempwstr, _16("Enabled"));
                        }

                        SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_SETITEM, 0, (LPARAM)&itemdata);
                     }

                     break;
                  }
                  case NM_CLICK:
                  {
                     int cursel=(int)SendDlgItemMessage(hDlg, IDC_CHEATLIST,
                                                        LVM_GETNEXTITEM, -1,
                                                        LVNI_SELECTED);
                     if (cursel != -1)
                        EnableWindow(GetDlgItem(hDlg, IDC_DELETECODE), TRUE);
                     else
                        EnableWindow(GetDlgItem(hDlg, IDC_DELETECODE), FALSE);

                     break;
                  }
                  default:
                     break;
               }
               break;
            }
            default: break;
         }

         return 0L;
      }

      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

static result_struct *cheatresults=NULL;
static u32 numresults;
static int searchtype=0;

void GetSearchTypes(HWND hDlg)
{
   switch(searchtype & 0xC)
   {
      case SEARCHEXACT:
         SendDlgItemMessage(hDlg, IDC_EXACTRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      case SEARCHLESSTHAN:
         SendDlgItemMessage(hDlg, IDC_LESSTHANRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      case SEARCHGREATERTHAN:
         SendDlgItemMessage(hDlg, IDC_GREATERTHANRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      default: break;
   }

   switch(searchtype & 0x70)
   {
      case SEARCHUNSIGNED:
         SendDlgItemMessage(hDlg, IDC_UNSIGNEDRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      case SEARCHSIGNED:
         SendDlgItemMessage(hDlg, IDC_SIGNEDRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      default: break;
   }

   switch(searchtype & 0x3)
   {
      case SEARCHBYTE:
         SendDlgItemMessage(hDlg, IDC_8BITRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      case SEARCHWORD:
         SendDlgItemMessage(hDlg, IDC_16BITRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      case SEARCHLONG:
         SendDlgItemMessage(hDlg, IDC_32BITRB, BM_SETCHECK, BST_CHECKED, 0);
         break;
      default: break;
   }
}

//////////////////////////////////////////////////////////////////////////////

void SetSearchTypes(HWND hDlg)
{
   searchtype = 0;
   if (SendDlgItemMessage(hDlg, IDC_EXACTRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
      searchtype |= SEARCHEXACT;
   else if (SendDlgItemMessage(hDlg, IDC_LESSTHANRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
      searchtype |= SEARCHLESSTHAN;
   else
      searchtype |= SEARCHGREATERTHAN;

   if (SendDlgItemMessage(hDlg, IDC_UNSIGNEDRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
      searchtype |= SEARCHUNSIGNED;
   else
      searchtype |= SEARCHSIGNED;

   if (SendDlgItemMessage(hDlg, IDC_8BITRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
      searchtype |= SEARCHBYTE;
   else if (SendDlgItemMessage(hDlg, IDC_16BITRB, BM_GETCHECK, 0, 0) == BST_CHECKED)
      searchtype |= SEARCHWORD;
   else
      searchtype |= SEARCHLONG;
}

//////////////////////////////////////////////////////////////////////////////

void ListResults(HWND hDlg)
{
   u32 i;
   char tempstr[11];

   SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_DELETEALLITEMS, 0, 0);
   EnableWindow(GetDlgItem(hDlg, IDC_CTADDCHEATBT), FALSE);

   if (cheatresults)
   {
      // Show results
      for (i = 0; i < numresults; i++)
      {
         LVITEM itemdata;

         itemdata.mask = LVIF_TEXT;
         itemdata.iItem = i;
         itemdata.iSubItem = 0;
         sprintf(tempstr, "%08X", cheatresults[i].addr);
         itemdata.pszText = _16(tempstr);
         itemdata.cchTextMax = (int)wcslen(itemdata.pszText);
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTITEM, 0, (LPARAM)&itemdata);

         itemdata.iSubItem = 1;
         switch(searchtype & 0x3)
         {
            case SEARCHBYTE:
               sprintf(tempstr, "%d", MappedMemoryReadByte(cheatresults[i].addr));
               break;
            case SEARCHWORD:
               sprintf(tempstr, "%d", MappedMemoryReadWord(cheatresults[i].addr));
               break;
               case SEARCHLONG:
               sprintf(tempstr, "%d", MappedMemoryReadLong(cheatresults[i].addr));
               break;
            default: break;
         }

         itemdata.pszText = _16(tempstr);
         itemdata.cchTextMax = (int)wcslen(itemdata.pszText);
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_SETITEM, 0, (LPARAM)&itemdata);
      }
   }
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK CheatSearchDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam)
{
   WCHAR tempwstr[11];
   char tempstr[11];

   switch (uMsg)
   {
      case WM_INITDIALOG:
      {
         LVCOLUMN coldata;

         // If cheat search hasn't been started yet, disable search and add
         // cheat
         if (cheatresults == NULL)
         {
            SetDlgItemText(hDlg, IDC_CTSEARCHRESTARTBT, _16("Start"));
            EnableWindow(GetDlgItem(hDlg, IDC_CTSEARCHBT), FALSE);
            EnableWindow(GetDlgItem(hDlg, IDC_CTADDCHEATBT), FALSE);
         }

         ListView_SetExtendedListViewStyleEx(GetDlgItem(hDlg, IDC_CHEATLIST),
                                             LVS_EX_FULLROWSELECT, LVS_EX_FULLROWSELECT);

         coldata.mask = LVCF_TEXT | LVCF_WIDTH;
         coldata.pszText = _16("Address\0");
         coldata.cchTextMax = (int)wcslen(coldata.pszText);
         coldata.cx = 190;
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTCOLUMN, (WPARAM)0, (LPARAM)&coldata);

         coldata.pszText = _16("Value\0");
         coldata.cchTextMax = (int)wcslen(coldata.pszText);
         coldata.cx = 111;
         SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_INSERTCOLUMN, (WPARAM)1, (LPARAM)&coldata);

         GetSearchTypes(hDlg);
         ListResults(hDlg);

         return TRUE;
      }
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDC_CTSEARCHRESTARTBT:
               if (cheatresults == NULL)
               {
                  SetDlgItemText(hDlg, IDC_CTSEARCHRESTARTBT, _16("Restart"));
                  EnableWindow(GetDlgItem(hDlg, IDC_CTSEARCHBT), TRUE);
               }
               else
                  free(cheatresults);

               // Setup initial values
               numresults = 0x100000;
               SendDlgItemMessage(hDlg, IDC_CHEATLIST, LVM_DELETEALLITEMS, 0, 0);
               break;
            case IDC_CTSEARCHBT:
            {
               // Search low wram and high wram areas
               SetSearchTypes(hDlg);
               GetDlgItemText(hDlg, IDC_CHEATSEARCHET, tempwstr, sizeof(tempwstr));
               WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);

               if (strcmp(tempstr, "") == 0)
               {
                  MessageBox (hDlg, _16("Please enter a value to search."), _16("Error"),  MB_OK | MB_ICONINFORMATION);
                  return TRUE;
               }

               cheatresults = MappedMemorySearch(0x06000000, 0x06100000, searchtype,
                                                 tempstr, cheatresults, &numresults);

               ListResults(hDlg);
               return TRUE;
            }
            case IDC_CTADDCHEATBT:
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_ADDCODE), hDlg, (DLGPROC)AddCodeDlgProc2);
               return TRUE;
            case IDCANCEL:
            case IDOK:
            {
               PostMessage(hDlg, WM_CLOSE, 0, 0);
               return TRUE;
            }
            default: break;
         }
         break;
      }
      case WM_CLOSE:
         SetSearchTypes(hDlg);
         EndDialog(hDlg, TRUE);
         return TRUE;
      case WM_NOTIFY:
      {
         LPNMHDR lpnm = (LPNMHDR)lParam;

         switch (((LPNMHDR)lParam)->idFrom)
         {
            case IDC_CHEATLIST:
               if (((LPNMHDR)lParam)->code == NM_CLICK)
               {

                  if (SendDlgItemMessage(hDlg, IDC_CHEATLIST,
                                         LVM_GETNEXTITEM, -1,
                                         LVNI_SELECTED) != -1)
                     EnableWindow(GetDlgItem(hDlg, IDC_CTADDCHEATBT), TRUE);
                  else
                     EnableWindow(GetDlgItem(hDlg, IDC_CTADDCHEATBT), FALSE);

                  break;
               }
               break;
            default: break;
         }
         break;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////
