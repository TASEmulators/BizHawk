/*  Copyright 2004-2008 Theo Berkau

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

#ifndef SETTINGS_H
#define SETTINGS_H

#include <windows.h>
#include <prsht.h>
#include "../../core.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
   char *string;
   HWND hWnd;
   HWND hParent;
} helpballoon_struct;

LRESULT CALLBACK SettingsDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam);

LRESULT CALLBACK BackupRamDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);

extern BOOL IsPathCdrom(const char *path);

int CreateHelpBalloons(helpballoon_struct *hb);
void DestroyHelpBalloons(helpballoon_struct *hb);

extern char biosfilename[MAX_PATH];
extern char cdrompath[MAX_PATH];
extern char backupramfilename[MAX_PATH];
extern char mpegromfilename[MAX_PATH];
extern char cartfilename[MAX_PATH];
extern char inifilename[MAX_PATH];
extern char logfilename[MAX_PATH];
extern char mini18nlogfilename[MAX_PATH];

extern char bioslang;
extern char sh2coretype;
extern char vidcoretype;
extern char sndcoretype;
extern int sndvolume;
extern int enableautofskip;
extern int usefullscreenonstartup;
extern int fullscreenwidth;
extern int fullscreenheight;
extern int usecustomwindowsize;
extern int windowwidth;
extern int windowheight;
extern char percoretype;
extern u8 regionid;
extern int disctype;
extern int carttype;
extern DWORD netlinklocalremoteip;
extern int netlinkport;
extern int uselog;
extern int usemini18nlog;
extern int logtype;
extern int nocorechange;
#ifdef USETHREADS
extern int changecore;
extern int corechanged;
#endif

enum {
   EMUTYPE_NONE=0,
   EMUTYPE_STANDARDPAD,
   EMUTYPE_ANALOGPAD,
   EMUTYPE_STUNNER,
   EMUTYPE_MOUSE,
   EMUTYPE_KEYBOARD
};

void CreateFilter(WCHAR * filter, size_t maxlen, ...);

typedef struct
{
   PROPSHEETHEADER psh;
   PROPSHEETPAGE *psp;
   int numprops;
} psp_struct;

BOOL CreatePropertySheet(psp_struct *psplist, LPCTSTR lpTemplate, LPCTSTR pszTitle, DLGPROC pfnDlgProc);
INT_PTR SettingsCreatePropertySheets(HWND hParent, BOOL ismodal, psp_struct *psplist);

#ifndef HAVE_LIBMINI18N
LPWSTR _16(const char *string);
#endif

#ifdef __cplusplus
}
#endif

#endif

