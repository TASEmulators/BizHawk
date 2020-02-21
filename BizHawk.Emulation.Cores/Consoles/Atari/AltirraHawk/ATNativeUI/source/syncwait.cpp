//	Altirra - Atari 800/800XL/5200 emulator
//	Native UI library - synchronous wait support
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

#include <stdafx.h>
#include <windows.h>
#include <at/atnativeui/syncwait.h>

ATNativeUISyncWait::ATNativeUISyncWait()
	: mhEvent(CreateEvent(NULL, FALSE, FALSE, NULL))
	, mbWaitSatisfied(false)
{
}

ATNativeUISyncWait::~ATNativeUISyncWait() {
	if (mhEvent)
		CloseHandle(mhEvent);
}

void ATNativeUISyncWait::Wait() {
	MSG msg;
	DWORD messageWaitId = mhEvent ? WAIT_OBJECT_0 + 1 : WAIT_OBJECT_0;

	while(!mbWaitSatisfied) {
		DWORD r = MsgWaitForMultipleObjects(mhEvent ? 1 : 0, &mhEvent, FALSE, mhEvent ? INFINITE : 10, QS_SENDMESSAGE);
		if (r == messageWaitId) {
			while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE | PM_QS_SENDMESSAGE)) {
				TranslateMessage(&msg);
				DispatchMessage(&msg);
			}

			continue;
		} else if (r != WAIT_TIMEOUT)
			break;
	}
}

void ATNativeUISyncWait::Signal() {
	mbWaitSatisfied = true;
	SetEvent(mhEvent);
}
