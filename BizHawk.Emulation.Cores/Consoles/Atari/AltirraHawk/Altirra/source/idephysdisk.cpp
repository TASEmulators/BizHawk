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
#include <windows.h>
#include <winioctl.h>
#include <at/atcore/propertyset.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include "idephysdisk.h"

bool ATIDEIsPhysicalDiskPath(const wchar_t *path) {
	return wcsncmp(path, L"\\\\?\\", 4) == 0;
}

sint64 ATIDEGetPhysicalDiskSize(const wchar_t *path) {
	HANDLE h = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING, NULL);

	if (h == INVALID_HANDLE_VALUE)
		return -1;

	PARTITION_INFORMATION partInfo = {0};
	DWORD actual;
	bool success = 0 != DeviceIoControl(h, IOCTL_DISK_GET_PARTITION_INFO, NULL, 0, &partInfo, sizeof partInfo, &actual, NULL);

	CloseHandle(h);
	return success ? partInfo.PartitionLength.QuadPart : -1;
}

void ATCreateDeviceHardDiskPhysical(const ATPropertySet& pset, IATDevice **dev);

extern const ATDeviceDefinition g_ATDeviceDefIDEPhysDisk = { "hdphysdisk", "harddisk", L"Hard disk image (physical disk)", ATCreateDeviceHardDiskPhysical };

ATIDEPhysicalDisk::ATIDEPhysicalDisk()
	: mhDisk(INVALID_HANDLE_VALUE)
	, mpBuffer(NULL)
{
}

ATIDEPhysicalDisk::~ATIDEPhysicalDisk() {
	Shutdown();
}

int ATIDEPhysicalDisk::AddRef() {
	return ATDevice::AddRef();
}

int ATIDEPhysicalDisk::Release() {
	return ATDevice::Release();
}

void *ATIDEPhysicalDisk::AsInterface(uint32 iid) {
	switch(iid) {
		case IATBlockDevice::kTypeID: return static_cast<IATBlockDevice *>(this);
		default:
			return ATDevice::AsInterface(iid);
	}
}

void ATIDEPhysicalDisk::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefIDEPhysDisk;
}

void ATIDEPhysicalDisk::GetSettings(ATPropertySet& settings) {
	settings.SetString("path", mPath.c_str());
}

bool ATIDEPhysicalDisk::SetSettings(const ATPropertySet& settings) {
	return false;
}

ATBlockDeviceGeometry ATIDEPhysicalDisk::GetGeometry() const {
	return ATBlockDeviceGeometry();
}

uint32 ATIDEPhysicalDisk::GetSerialNumber() const {
	return VDHashString32I(mPath.c_str());
}

void ATIDEPhysicalDisk::Init(const wchar_t *path) {
	Shutdown();

	mPath = path;
	mhDisk = CreateFileW(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_NO_BUFFERING, NULL);

	if (mhDisk == INVALID_HANDLE_VALUE)
		throw MyWin32Error("Cannot open physical disk: %%s", GetLastError());

	mpBuffer = VDFile::AllocUnbuffer(512 * 32);
	if (!mpBuffer) {
		Shutdown();
		throw MyMemoryError();
	}

	DISK_GEOMETRY info = {0};
	DWORD actual;
	if (!DeviceIoControl(mhDisk, IOCTL_DISK_GET_DRIVE_GEOMETRY, NULL, 0, &info, sizeof info, &actual, NULL))
		throw MyWin32Error("Cannot get size of physical disk: %%s", GetLastError());

	mSectorCount = (uint32)info.Cylinders.QuadPart * info.TracksPerCylinder * info.SectorsPerTrack;
}

void ATIDEPhysicalDisk::Shutdown() {
	if (mpBuffer) {
		VDFile::FreeUnbuffer(mpBuffer);
		mpBuffer = NULL;
	}

	if (mhDisk != INVALID_HANDLE_VALUE) {
		CloseHandle(mhDisk);
		mhDisk = INVALID_HANDLE_VALUE;
	}
}

void ATIDEPhysicalDisk::Flush() {
}

void ATIDEPhysicalDisk::ReadSectors(void *data, uint32 lba, uint32 n) {
	const uint64 offset = (uint64)lba << 9;
	LONG offsetLo = (LONG)offset;
	LONG offsetHi = (LONG)(offset >> 32);

	if (INVALID_SET_FILE_POINTER == SetFilePointer(mhDisk, offsetLo, &offsetHi, FILE_BEGIN)) {
		DWORD err = GetLastError();

		if (err != NO_ERROR)
			throw MyWin32Error("Error reading from physical disk: %%s.", err);
	}

	uint32 bytes = n * 512;
	while(bytes) {
		uint32 toread = bytes > 512*32 ? 512*32 : bytes;

		DWORD actual;
		if (!ReadFile(mhDisk, mpBuffer, toread, &actual, NULL))
			throw MyWin32Error("Error reading from physical disk: %%s.", GetLastError());

		bytes -= toread;
		memcpy(data, mpBuffer, toread);
		data = (char *)data + toread;
	}
}

void ATIDEPhysicalDisk::WriteSectors(const void *data, uint32 lba, uint32 n) {
}
