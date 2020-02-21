//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2014 Avery Lee
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

#ifndef f_AT_FIRMWAREDETECT_H
#define f_AT_FIRMWAREDETECT_H

class VDStringW;
struct ATFirmwareInfo;
enum ATSpecificFirmwareType : uint32;

bool ATFirmwareAutodetectCheckSize(uint64 fileSize);
bool ATFirmwareAutodetect(const void *data, uint32 len, ATFirmwareInfo& info, ATSpecificFirmwareType& specificFirmware);

#endif
