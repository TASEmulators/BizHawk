//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - system message loop support
//	Copyright (C) 2008-2015 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_AT_ATNATIVEUI_MESSAGELOOP_H
#define f_AT_ATNATIVEUI_MESSAGELOOP_H

#include <vd2/system/win32/miniwindows.h>

// Process messages in the Windows message queue. Returns true if
// successful, or false if WM_QUIT was encountered.
//
// Behaviors:
//	- Input has higher priority than usual.
//	- Key tunneling is implemented.
//	- Mouse wheel events are routed to the window under the
//	  pointer, not the focus.
//	- WM_QUIT is automatically reposted when encountered.
//
bool ATUIProcessMessages(bool waitForMessage, int& returnCode);

void ATUIRegisterTopLevelWindow(VDZHWND h);
void ATUIUnregisterTopLevelWindow(VDZHWND h);
void ATUIRegisterModelessDialog(VDZHWND h);
void ATUIUnregisterModelessDialog(VDZHWND h);
void ATUIShowModelessDialogs(bool visible, VDZHWND parent);
bool ATUIProcessModelessDialogs(MSG *msg);
void ATUISetGlobalEnableState(bool enable);
void ATUIDestroyModelessDialogs(VDZHWND parent);

#endif
