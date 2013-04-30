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

#ifndef YUIDEBUG_H
#define YUIDEBUG_H
#include "../../sh2core.h"

enum { OFN_DEFAULTSAVE=0, OFN_DEFAULTLOAD };

LRESULT CALLBACK MemTransferDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam);
LRESULT CALLBACK SH2DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam);
LRESULT CALLBACK VDP1DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);
LRESULT CALLBACK VDP2DebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);
LRESULT CALLBACK M68KDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);
LRESULT CALLBACK SCUDSPDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam);
LRESULT CALLBACK SCSPDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);
LRESULT CALLBACK SMPCDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                  LPARAM lParam);
LRESULT CALLBACK MemoryEditorDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                     LPARAM lParam);
LRESULT CALLBACK ErrorDebugDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                    LPARAM lParam);
LRESULT CALLBACK AboutDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                              LPARAM lParam);
LRESULT CALLBACK LogDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                            LPARAM lParam);
LRESULT CALLBACK MemDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                                 LPARAM lParam);
void DebugMouseWheel(HWND hctl, WPARAM wParam);
int SaveBitmap(const char *filename, int width, int height, u32 *data);

void UpdateLogCallback (char *string);
void SetupOFN(OPENFILENAME *ofn, int type, HWND hwnd, const LPCTSTR lpstrFilter, LPTSTR lpstrFile, DWORD nMaxFile);

extern SH2_struct *debugsh;
extern HWND LogWin;
extern char *logbuffer;
extern u32 logsize;

extern u32 memaddr;

#endif
