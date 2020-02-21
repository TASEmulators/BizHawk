//	Altirra - Atari 800/800XL/5200 emulator
//	Core library -- Device indicator interfaces
//	Copyright (C) 2009-2016 Avery Lee
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

//=========================================================================
// Device indicator interface
//

#ifndef f_AT_ATCORE_DEVICEINDICATORS_H
#define f_AT_ATCORE_DEVICEINDICATORS_H

#include <vd2/system/unknown.h>

class IATDeviceIndicatorManager {
public:
	virtual void SetStatusFlags(uint32 flags) = 0;
	virtual void ResetStatusFlags(uint32 flags) = 0;
	virtual void PulseStatusFlags(uint32 flags) = 0;

	virtual void SetStatusCounter(uint32 index, uint32 value) = 0;
	virtual void SetDiskLEDState(uint32 index, sint32 ledDisplay) = 0;

	virtual void SetDiskMotorActivity(uint32 index, bool on) = 0;
	virtual void SetDiskErrorState(uint32 index, bool error) = 0;

	virtual void SetHActivity(bool write) = 0;
	virtual void SetIDEActivity(bool write, uint32 lba) = 0;
	virtual void SetPCLinkActivity(bool write) = 0;

	virtual void SetFlashWriteActivity() = 0;

	virtual void SetCassetteIndicatorVisible(bool vis) = 0;
	virtual void SetCassettePosition(float pos, float len, bool recordMode, bool fskMode) = 0;

	virtual void SetRecordingPosition() = 0;
	virtual void SetRecordingPosition(float time, sint64 size) = 0;

	virtual void SetModemConnection(const char *str) = 0;

	virtual void SetStatusMessage(const wchar_t *s) = 0;
};

#endif
