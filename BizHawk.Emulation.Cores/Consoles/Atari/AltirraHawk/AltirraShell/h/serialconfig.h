//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - serial emulation configuration
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

#ifndef f_ATS_SERIALCONFIG_H
#define f_ATS_SERIALCONFIG_H

#include <vd2/system/VDString.h>

class VDRegistryKey;

struct ATSSerialConfig {
	VDStringW mSerialPath;
	
	enum HighSpeedMode {
		kHighSpeed_Disabled,
		kHighSpeed_Standard,
		kHighSpeed_PokeyDivisor
	} mHighSpeedMode;

	uint32 mHSBaudRate;
	uint32 mHSPokeyDivisor;
};

void ATSSaveSerialConfig(VDRegistryKey& key, const ATSSerialConfig&);
void ATSLoadSerialConfig(ATSSerialConfig&, const VDRegistryKey& key);

#endif
