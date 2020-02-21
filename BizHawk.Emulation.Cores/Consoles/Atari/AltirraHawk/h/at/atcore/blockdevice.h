//	Altirra - Atari 800/800XL/5200 emulator
//	Core library -- block device interface
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

#ifndef f_AT_ATCORE_BLOCKDEVICE_H
#define f_AT_ATCORE_BLOCKDEVICE_H

#include <vd2/system/unknown.h>

struct ATBlockDeviceGeometry {
	uint32 mSectorsPerTrack;
	uint32 mHeads;
	uint32 mCylinders;
	bool mbSolidState;
};

class IATBlockDevice : public IVDRefUnknown {
public:
	enum { kTypeID = 'bldv' };

	virtual bool IsReadOnly() const = 0;
	virtual uint32 GetSectorCount() const = 0;
	virtual ATBlockDeviceGeometry GetGeometry() const = 0;
	virtual uint32 GetSerialNumber() const = 0;

	virtual void Flush() = 0;

	virtual void ReadSectors(void *data, uint32 lba, uint32 n) = 0;
	virtual void WriteSectors(const void *data, uint32 lba, uint32 n) = 0;
};

#endif
