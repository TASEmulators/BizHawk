//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - multithreaded logging module
//	Copyright (C) 2009-2015 Avery Lee
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

#ifndef f_ATS_LOGGER_H
#define f_ATS_LOGGER_H

#include <vd2/system/function.h>

class IATSLogger {
public:
	virtual void SetLogHandler(vdfunction<void()> fn) = 0;
	virtual void BeginAsync() = 0;
	virtual uint32 GetTextLength() const = 0;
	virtual const wchar_t *LockText() = 0;
	virtual void UnlockTextAndClear() = 0;
	virtual void EndAsync() = 0;
};

IATSLogger *ATSGetLogger();
void ATSInitLogger();
void ATSShutdownLogger();

#endif
