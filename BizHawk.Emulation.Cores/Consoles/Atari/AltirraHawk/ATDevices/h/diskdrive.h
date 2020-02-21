//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - disk drive module
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

#ifndef f_AT_ATDEVICES_DISKDRIVE_H
#define f_AT_ATDEVICES_DISKDRIVE_H

#include <vd2/system/function.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicesio.h>
#include <at/atio/diskimage.h>

class ATPropertySet;

class ATDeviceDiskDrive final : public ATDevice, public IATDeviceScheduling, public IATDeviceSIO {
	ATDeviceDiskDrive(const ATDeviceDiskDrive&);
	ATDeviceDiskDrive& operator=(const ATDeviceDiskDrive&);

public:
	ATDeviceDiskDrive();
	~ATDeviceDiskDrive();
	
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& pset) override;
	bool SetSettings(const ATPropertySet& pset) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;

public:
	void MountDisk(const wchar_t *path);

public:
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:
	void InitSIO(IATDeviceSIOManager *mgr) override;
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	void OnSerialAbortCommand() override;
	void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	void OnSerialFence(uint32 id) override;

	// Attempt to accelerate a command via SIOV intercept. This receives a superset
	// of the command structure received by OnSerialBeginCommand() and is intended
	// to allow a direct forward.
	//
	// This routine can also return the additional BypassAccel value, which means
	// to abort acceleration and force usage of native SIO. It is used for requests
	// that the device recognizes but which cannot be safely accelerated by any
	// device.
	CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

private:
	enum EmulationMode {
		kEmuMode_Generic,
		kEmuMode_Generic57600,
		kEmuMode_GenericFastest,
		kEmuMode_810,
		kEmuMode_1050,
		kEmuMode_XF551,
		kEmuMode_USDoubler,
		kEmuMode_Speedy1050,
		kEmuMode_IndusGT,
		kEmuMode_Happy,
		kEmuMode_1050Turbo,
		kEmuModeCount
	};

	struct EmulationProfile;

	CmdResponse OnCmdGetHighSpeedIndex();
	CmdResponse OnCmdReadSector();
	CmdResponse OnCmdGetStatus();
	CmdResponse OnCmdWriteSector();
	void OnCmdWriteSector2(const void *data, uint32 len);
	CmdResponse OnCmdPutSector();
	void OnCmdPutSector2(const void *data, uint32 len);
	CmdResponse OnCmdReadPERCOMBlock();
	CmdResponse OnCmdWritePERCOMBlock();
	CmdResponse OnCmdFormat();
	CmdResponse OnCmdFormatHighSpeedSkew();
	CmdResponse OnCmdFormatMedium();

	void UpdateRotationalPosition();

	ATScheduler *mpScheduler = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;

	vdrefptr<IATDiskImage> mpDiskImage;
	VDStringW mPath;

	bool mbLastOpError = false;
	uint8 mFDCStatus = 0xFF;

	const EmulationProfile *mpEmulationProfile = nullptr;

	uint32 mCyclesPerRotation = 0;
	uint32 mRotationOffset = 0;
	uint64 mRotationTime = 0;
	uint32 mWeakBitLFSR = 0;

	uint8 mDeviceId = 0x31;
	bool mbReadOnlyRequested = false;
	bool mbAutoFlush = false;
	bool mbAccurateTiming = false;
	bool mbReadOnly = false;
	bool mbHighSpeedXF551Enabled = false;

	uint32	mHighSpeedCmdDivisorLo = (uint32)0 - 1;
	uint32	mHighSpeedCmdDivisorHi = (uint32)0 - 1;

	ATDeviceSIOCommand mCommand = {};
	bool mbHighSpeedCommand = false;
	bool mbHighSpeedData = false;

	uint8 mPERCOM[12] = {};

	vdfunction<void()> mpFenceFn;

	// Each drive needs its own sector buffer, as there are some operations that
	// can return sector data from the last I/O operation on that drive.
	uint8 mSectorBuffer[8192];

	static const EmulationProfile kEmulationProfiles[];
};

#endif
