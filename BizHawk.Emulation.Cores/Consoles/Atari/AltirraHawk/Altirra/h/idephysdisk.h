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

#ifndef f_AT_IDEPHYSDISK_H
#define f_AT_IDEPHYSDISK_H

#include <at/atcore/blockdevice.h>
#include <at/atcore/deviceimpl.h>

bool ATIDEIsPhysicalDiskPath(const wchar_t *path);
sint64 ATIDEGetPhysicalDiskSize(const wchar_t *path);

class ATIDEPhysicalDisk final : public IATBlockDevice, public ATDevice {
	ATIDEPhysicalDisk(const ATIDEPhysicalDisk&) = delete;
	ATIDEPhysicalDisk& operator=(const ATIDEPhysicalDisk&) = delete;
public:
	ATIDEPhysicalDisk();
	~ATIDEPhysicalDisk();

public:
	int AddRef() override;
	int Release() override;
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Shutdown() override;

public:
	bool IsReadOnly() const override { return true; }
	uint32 GetSectorCount() const override { return mSectorCount; }
	ATBlockDeviceGeometry GetGeometry() const override;
	uint32 GetSerialNumber() const override;

	void Init(const wchar_t *path);

	void Flush() override;

	void ReadSectors(void *data, uint32 lba, uint32 n) override;
	void WriteSectors(const void *data, uint32 lba, uint32 n) override;

protected:
	void *mhDisk;
	void *mpBuffer;
	uint32 mSectorCount;
	VDStringW mPath;
};

#endif
