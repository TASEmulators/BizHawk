//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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

#ifndef f_AT_VIRTUALSCREEN_H
#define f_AT_VIRTUALSCREEN_H

#include <vd2/system/function.h>

class IATVirtualScreenHandler {
public:
	virtual ~IATVirtualScreenHandler() = default;

	virtual void GetScreen(uint32& width, uint32& height, const uint8 *&screen) const = 0;
	virtual bool GetCursorInfo(uint32& x, uint32& y) const = 0;

	virtual void SetReadyCallback(const vdfunction<void()>& fn) = 0;
	virtual bool IsReadyForInput() const = 0;

	virtual void ToggleSuspend() = 0;

	virtual void Resize(uint32 w, uint32 h) = 0;
	virtual void PushLine(const char *line) = 0;

	virtual bool IsRawInputActive() const = 0;
	virtual void SetShiftControlLockState(bool shift, bool ctrl) = 0;
	virtual bool GetShiftLockState() const = 0;
	virtual bool GetControlLockState() const = 0;
	virtual bool CheckForBell() = 0;
	virtual int ReadRawText(uint8 *dst, int x, int y, int n) const = 0;

	virtual void ColdReset() = 0;
	virtual void WarmReset() = 0;

	virtual void SetHookPage(uint8 hookPage) = 0;
	virtual void SetGetCharAddress(uint16 addr) = 0;
	virtual void OnCIOVector(ATCPUEmulator *cpu, ATCPUEmulatorMemory *mem, int offset) = 0;
};

IATVirtualScreenHandler *ATCreateVirtualScreenHandler();

#endif	// f_AT_VIRTUALSCREEN_H
