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
//#include <windowsx.h>
#include <commctrl.h>
#undef FASTCALL
#include "../resource.h"
#include "settings.h"

char inifilename[MAX_PATH];
int nocorechange = 0;
#ifdef USETHREADS
int changecore = 0;
int corechanged = 0;
#endif

extern HINSTANCE y_hInstance;

LRESULT CALLBACK BasicSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam);

LRESULT CALLBACK VideoSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam);

LRESULT CALLBACK SoundSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam);

LRESULT CALLBACK NetlinkSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                        LPARAM lParam);

LRESULT CALLBACK InputSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                      LPARAM lParam);

LRESULT CALLBACK LogSettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam);

//////////////////////////////////////////////////////////////////////////////

int CreateHelpBalloons(helpballoon_struct *hb)
{
   TOOLINFO ti;
   RECT rect;
   int i;

   for (i = 0; hb[i].string != NULL; i++)
   {
      hb[i].hWnd = CreateWindowEx(WS_EX_TOPMOST,
                                   TOOLTIPS_CLASS,
                                   NULL,
                                   WS_POPUP | TTS_NOPREFIX | TTS_ALWAYSTIP,
                                   CW_USEDEFAULT,
                                   CW_USEDEFAULT,
                                   CW_USEDEFAULT,
                                   CW_USEDEFAULT,
                                   hb[i].hParent,
                                   NULL,
                                   y_hInstance,
                                   NULL);

      if (!hb[i].hWnd)
         return -1;

      SetWindowPos(hb[i].hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                   SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

      // Create some help balloons
      ti.cbSize = sizeof(TOOLINFO);
      ti.uFlags = TTF_SUBCLASS;
      ti.hwnd = hb[i].hParent;
      ti.hinst = y_hInstance;
      ti.uId = 0;
      ti.lpszText = _16(hb[i].string);
      GetClientRect(hb[i].hParent, &rect);
      ti.rect.left = rect.left;
      ti.rect.top = rect.top;
      ti.rect.right = rect.right;
      ti.rect.bottom = rect.bottom;

      // Add it
      SendMessage(hb[i].hWnd, TTM_ADDTOOL, 0, (LPARAM) (LPTOOLINFO) &ti);
   }

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void DestroyHelpBalloons(helpballoon_struct *hb)
{
   int i;
   for (i = 0; hb[i].string != NULL; i++)
   {
      if (hb[i].hWnd)
      {
         DestroyWindow(hb[i].hWnd);
         hb[i].hWnd = NULL;
      }
   }
}

//////////////////////////////////////////////////////////////////////////////

void CreateFilter(WCHAR * filter, size_t maxlen, ...)
{
   va_list list;
   const char * str;
   WCHAR * filterpos = filter;
   int wrote;

   va_start(list, maxlen);

   str = va_arg(list, const char *);
   while(str != NULL) {
#ifdef __MINGW32_VERSION
      wrote = swprintf(filterpos, _16(str));
#else
      wrote = swprintf(filterpos, maxlen, _16(str));
#endif
      filterpos += 1 + wrote;
      maxlen -= 1 + wrote;
      str = va_arg(list, const char *);
   }
   *filterpos = '\0';

   va_end(list);
}

//////////////////////////////////////////////////////////////////////////////

BOOL CreatePropertySheet(psp_struct *psplist, LPCTSTR lpTemplate, LPCTSTR pszTitle, DLGPROC pfnDlgProc)
{
    PROPSHEETPAGE *newpsp;

    if (psplist->numprops+1 > MAXPROPPAGES)
       return FALSE;

    if ((newpsp = (PROPSHEETPAGE *)calloc(psplist->numprops+1, sizeof(PROPSHEETPAGE))) == NULL)
       return FALSE;

    if (psplist->psp)
    {
       memcpy(newpsp, psplist->psp, sizeof(PROPSHEETPAGE) * psplist->numprops);
       free(psplist->psp);
    }

    psplist->psp = newpsp;

    psplist->psp[psplist->numprops].dwSize = sizeof(PROPSHEETPAGE);
    psplist->psp[psplist->numprops].dwFlags = PSP_USETITLE;
    psplist->psp[psplist->numprops].hInstance = y_hInstance;
    psplist->psp[psplist->numprops].pszTemplate = lpTemplate;
    psplist->psp[psplist->numprops].pszIcon = NULL;
    psplist->psp[psplist->numprops].pfnDlgProc = pfnDlgProc;
    psplist->psp[psplist->numprops].pszTitle = pszTitle;
    psplist->psp[psplist->numprops].lParam = 0;
    psplist->psp[psplist->numprops].pfnCallback = NULL;
    psplist->numprops++;
    return TRUE;
}

INT_PTR SettingsCreatePropertySheets(HWND hParent, BOOL ismodal, psp_struct *psplist)
{
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_BASICSETTINGS), _16("Basic"), (DLGPROC)BasicSettingsDlgProc);
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_VIDEOSETTINGS), _16("Video"), (DLGPROC)VideoSettingsDlgProc);
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_SOUNDSETTINGS), _16("Sound"), (DLGPROC)SoundSettingsDlgProc);
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_INPUTSETTINGS), _16("Input"), (DLGPROC)InputSettingsDlgProc);

#ifdef USESOCKET
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_NETLINKSETTINGS), _16("Netlink"), (DLGPROC)NetlinkSettingsDlgProc);
#endif

#if DEBUG
   CreatePropertySheet(psplist, MAKEINTRESOURCE(IDD_LOGSETTINGS), _16("Log"), (DLGPROC)LogSettingsDlgProc);
#endif

   psplist->psh.dwSize = sizeof(PROPSHEETHEADER);
   psplist->psh.dwFlags = PSH_PROPSHEETPAGE | PSH_NOAPPLYNOW | PSH_NOCONTEXTHELP;
   if (!ismodal)
      psplist->psh.dwFlags |= PSH_MODELESS;
   psplist->psh.hwndParent = hParent;
   psplist->psh.hInstance = y_hInstance;
   psplist->psh.pszIcon = NULL;
   psplist->psh.pszCaption = (LPTSTR)_16("Settings");
   psplist->psh.nPages = psplist->numprops;
   psplist->psh.nStartPage = 0;
   psplist->psh.ppsp = (LPCPROPSHEETPAGE)psplist->psp;
   psplist->psh.pfnCallback = NULL;
   return PropertySheet(&psplist->psh);
}

//////////////////////////////////////////////////////////////////////////////

#ifndef HAVE_LIBMINI18N

LPWSTR _16(const char *string)
{
   static WCHAR wstring[1024];
   MultiByteToWideChar(CP_ACP, MB_COMPOSITE, string, -1, wstring, sizeof(wstring) / sizeof(WCHAR));	
   return wstring;
}

#endif


