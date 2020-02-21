//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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

#ifndef f_AT_UICAPTIONUPDATER_H
#define f_AT_UICAPTIONUPDATER_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>

class ATSimulator;

class IATUIWindowCaptionUpdater : public IVDRefCount {
public:
	virtual void Init(const vdfunction<void(const wchar_t *)>& fn) = 0;
	virtual void InitMonitoring(ATSimulator *sim) = 0;

	virtual bool SetTemplate(const char *s, uint32 *errorPos = nullptr) = 0;

	virtual void SetShowFps(bool showFps) = 0;
	virtual void SetFullScreen(bool fs) = 0;
	virtual void SetMouseCaptured(bool captured, bool mmbRelease) = 0;

	virtual void Update(bool running, int ticks, float fps, float cpu) = 0;
	virtual void CheckForStateChange(bool force) = 0;
};

void ATUICreateWindowCaptionUpdater(IATUIWindowCaptionUpdater **ptr);

sint32 ATUIParseWindowCaptionFormat(const char *str);
const char *ATUIGetDefaultWindowCaptionTemplate();

#endif	// f_AT_UICAPTIONUPDATER_H
