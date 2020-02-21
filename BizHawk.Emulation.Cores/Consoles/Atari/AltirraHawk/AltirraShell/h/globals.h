//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - global nexus
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

#ifndef f_ATS_GLOBALS_H
#define f_ATS_GLOBALS_H

#include <vd2/system/function.h>

struct ATSSerialConfig;
class ATDeviceManager;

void ATSInitSerialEngine();
void ATSShutdownSerialEngine();
void ATSPostEngineRequest(vdfunction<void()> fn);
void ATSSendEngineRequest(vdfunction<void()> fn);

void ATSGetSerialConfig(ATSSerialConfig&);
void ATSSetSerialConfig(const ATSSerialConfig&);

void ATSInitDeviceManager();
void ATSShutdownDeviceManager();
ATDeviceManager *ATSGetDeviceManager();

#endif
