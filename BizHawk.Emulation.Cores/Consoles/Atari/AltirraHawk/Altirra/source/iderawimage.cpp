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

#include <stdafx.h>
#include <at/atcore/propertyset.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include "iderawimage.h"

void ATCreateDeviceHardDiskRawImage(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefIDERawImage = { "hdrawimage", "harddisk", L"Hard disk image (raw file)", ATCreateDeviceHardDiskRawImage };

ATIDERawImage::ATIDERawImage()
	: mSectorCount(0)
	, mSectorCountLimit(0)
	, mbReadOnly(false)
{
}

ATIDERawImage::~ATIDERawImage() {
	Shutdown();
}

int ATIDERawImage::AddRef() {
	return ATDevice::AddRef();
}

int ATIDERawImage::Release() {
	return ATDevice::Release();
}

void *ATIDERawImage::AsInterface(uint32 iid) {
	switch(iid) {
		case IATBlockDevice::kTypeID: return static_cast<IATBlockDevice *>(this);
		default:
			return ATDevice::AsInterface(iid);
	}
}

void ATIDERawImage::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefIDERawImage;
}

void ATIDERawImage::GetSettingsBlurb(VDStringW& buf) {
	buf = VDFileSplitPathRightSpan(mPath);
}

void ATIDERawImage::GetSettings(ATPropertySet& settings) {
	settings.SetString("path", mPath.c_str());
	settings.SetUint32("sectors", mSectorCountLimit);
	settings.SetUint32("cylinders", mGeometry.mCylinders);
	settings.SetUint32("heads", mGeometry.mHeads);
	settings.SetUint32("sectors_per_track", mGeometry.mSectorsPerTrack);
	settings.SetBool("write_enabled", !mbReadOnly);
	settings.SetBool("solid_state", mGeometry.mbSolidState);
}

bool ATIDERawImage::SetSettings(const ATPropertySet& settings) {
	// We need to force a recreate for the parent to pick up the
	// geometry change.
	return false;
}

bool ATIDERawImage::IsReadOnly() const {
	return mbReadOnly;
}

uint32 ATIDERawImage::GetSectorCount() const {
	return std::max(mSectorCount, mSectorCountLimit);
}

ATBlockDeviceGeometry ATIDERawImage::GetGeometry() const {
	return mGeometry;
}

uint32 ATIDERawImage::GetSerialNumber() const {
	return VDHashString32I(mPath.c_str());
}

void ATIDERawImage::Init(const wchar_t *path, bool write, bool solidState, uint32 sectorLimit, uint32 cyl, uint32 heads, uint32 spt) {
	Shutdown();

	mPath = path;
	mFile.open(path, write ? nsVDFile::kReadWrite | nsVDFile::kDenyAll | nsVDFile::kOpenAlways : nsVDFile::kRead | nsVDFile::kDenyWrite | nsVDFile::kOpenExisting);
	mbReadOnly = !write;

	uint64 sectors = (uint64)mFile.size() >> 9;
	mSectorCount = sectors > 0xFFFFFFFFU ? 0xFFFFFFFFU : (uint32)sectors;

	mSectorCountLimit = sectorLimit;

	mGeometry.mbSolidState = solidState;

	if (!cyl || !heads || !spt) {
		mGeometry.mCylinders = 0;
		mGeometry.mHeads = 0;
		mGeometry.mSectorsPerTrack = 0;
	} else {
		mGeometry.mCylinders = cyl;
		mGeometry.mHeads = heads;
		mGeometry.mSectorsPerTrack = spt;
	}
}

void ATIDERawImage::Shutdown() {
	mFile.closeNT();
}

void ATIDERawImage::Flush() {
}

void ATIDERawImage::ReadSectors(void *data, uint32 lba, uint32 n) {
	mFile.seek((sint64)lba << 9);

	uint32 requested = n << 9;
	uint32 actual = mFile.readData(data, requested);

	if (requested < actual)
		memset((char *)data + actual, 0, requested - actual);
}

void ATIDERawImage::WriteSectors(const void *data, uint32 lba, uint32 n) {
	mFile.seek((sint64)lba << 9);
	mFile.write(data, 512 * n);

	if (lba + n > mSectorCount)
		mSectorCount = lba + n;
}
