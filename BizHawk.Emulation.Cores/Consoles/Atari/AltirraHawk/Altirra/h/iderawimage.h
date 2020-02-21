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

#ifndef f_AT_IDERAWIMAGE_H
#define f_AT_IDERAWIMAGE_H

#include <at/atcore/blockdevice.h>
#include <at/atcore/deviceimpl.h>
#include <vd2/system/file.h>

class ATIDERawImage final : public ATDevice, public IATBlockDevice {
	ATIDERawImage(const ATIDERawImage&) = delete;
	ATIDERawImage& operator=(const ATIDERawImage&) = delete;
public:
	ATIDERawImage();
	~ATIDERawImage();

public:
	int AddRef() override;
	int Release() override;
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettingsBlurb(VDStringW& buf) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;

public:
	bool IsReadOnly() const override;
	uint32 GetSectorCount() const override;
	ATBlockDeviceGeometry GetGeometry() const override;
	uint32 GetSerialNumber() const override;

	void Init(const wchar_t *path, bool write, bool solidState, uint32 sectorLimit, uint32 cyl, uint32 heads, uint32 spt);
	void Shutdown() override;

	void Flush() override;

	void ReadSectors(void *data, uint32 lba, uint32 n) override;
	void WriteSectors(const void *data, uint32 lba, uint32 n) override;

protected:
	VDFile mFile;
	VDStringW mPath;
	uint32 mSectorCount;
	uint32 mSectorCountLimit;
	bool mbReadOnly;

	ATBlockDeviceGeometry mGeometry = {};
};

#endif
