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

#ifndef f_AT_PCLINK_H
#define f_AT_PCLINK_H

#include "pokey.h"

class ATPokeyEmulator;
class ATScheduler;
class ATCPUEmulator;
class ATCPUEmulatorMemory;
struct ATKernelDatabase;
class IATUIRenderer;

class IATPCLinkDevice {
public:
	virtual ~IATPCLinkDevice() {}
	virtual void Shutdown() = 0;

	virtual bool IsReadOnly() = 0;
	virtual void SetReadOnly(bool readOnly) = 0;

	virtual const wchar_t *GetBasePath() = 0;
	virtual void SetBasePath(const wchar_t *basePath) = 0;
};


IATPCLinkDevice *ATCreatePCLinkDevice();

#endif
