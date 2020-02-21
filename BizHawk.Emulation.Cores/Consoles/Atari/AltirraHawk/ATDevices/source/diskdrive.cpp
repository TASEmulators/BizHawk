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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/math.h>
#include <at/atcore/logging.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include "diskdrive.h"

ATLogChannel g_ATLCDisk(false, false, "DISK", "Disk activity");
ATLogChannel g_ATLCDiskCmd(false, false, "DISKCMD", "Disk commands");
ATLogChannel g_ATLCDiskData(false, false, "DISKXFR", "Disk data transfer");

namespace {
	const int kCyclesPerDiskRotation_288RPM = 372869;
	const int kCyclesPerDiskRotation_300RPM = 357955;
}

void ATCreateDeviceDiskDrive(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceDiskDrive> p(new ATDeviceDiskDrive);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefDiskDrive = { "disk", "disk", L"Disk Drive", ATCreateDeviceDiskDrive };

///////////////////////////////////////////////////////////////////////////

struct ATDeviceDiskDrive::EmulationProfile {
	uint8 mTimeout;
	sint8 mHighSpeedIndex;
	bool mbHighSpeed551;
	bool mbHighSpeedUSD;
	uint16 mCyclesPerByte;
	uint16 mCyclesPerBit;
	uint16 mCyclesPerByteHS;
	uint16 mCyclesPerBitHS;
	uint32 mCyclesPerRotation;
	bool mbPERCOMSupported;
	bool mbRPM300;
};

const ATDeviceDiskDrive::EmulationProfile ATDeviceDiskDrive::kEmulationProfiles[] = {
//						  TmOt, HSI HS551  HSUSD Byte  Bit HBy HBt  Cy/Rot PERCOM RPM300
/* Generic		  */	{ 0xE0, 16, false, false, 949, 94, 949, 94, 372869,  true, false },
/* Generic57600	  */	{ 0xE0,  8, false, false, 949, 94, 311, 31, 372869,  true, false },
/* GenericFastest */	{ 0xE0,  0, false, false, 949, 94, 140, 14, 372869,  true, false },
/* 810			  */	{ 0xE0, -1, false, false, 949, 94, 949, 94, 372869, false, false },
/* 1050			  */	{ 0xE0, -1, false, false, 982, 94, 982, 94, 372869, false, false },
/* XF551		  */	{ 0xFE, 16,  true, false, 949, 94, 450, 45, 357954,  true,  true },
/* USDoubler	  */	{ 0xE0, 10, false,  true, 949, 94, 394, 34, 372869,  true, false },
/* Speedy1050	  */	{ 0xE0,  9, false,  true, 949, 94, 383, 32, 372869,  true, false },
/* IndusGT		  */	{ 0xE0,  6, false,  true, 949, 94, 260, 26, 372869,  true, false },
/* Happy		  */	{ 0xE0, 10, false,  true, 949, 94, 394, 34, 372869,  true, false },
/* 1050Turbo      */	{ 0xE0,  6, false,  true, 949, 94, 260, 26, 372869,  true, false },
};

///////////////////////////////////////////////////////////////////////////

ATDeviceDiskDrive::ATDeviceDiskDrive() {
}

ATDeviceDiskDrive::~ATDeviceDiskDrive() {
}

void *ATDeviceDiskDrive::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceSIO::kTypeID: return static_cast<IATDeviceSIO *>(this);
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
			break;
	}

	return ATDevice::AsInterface(iid);
}

void ATDeviceDiskDrive::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefDiskDrive;
}

void ATDeviceDiskDrive::GetSettings(ATPropertySet& pset) {
	pset.SetString("path", mPath.c_str());
	pset.SetBool("writable", !mbReadOnlyRequested);
	pset.SetBool("autoflush", mbAutoFlush);
	pset.SetUint32("index", mDeviceId - 0x31);

	if (mbAccurateTiming)
		pset.SetBool("actiming", true);
}

bool ATDeviceDiskDrive::SetSettings(const ATPropertySet& pset) {
	const wchar_t *path = pset.GetString("path", L"");

	if (mPath != path)
		MountDisk(path);

	mbReadOnlyRequested = !pset.GetBool("writable", false); 
	mbAutoFlush = pset.GetBool("autoflush", false);
	mDeviceId = (uint8)(pset.GetUint32("index", 0) + 0x31);

	mbAccurateTiming = pset.GetBool("actiming", false);

	mbReadOnly = mbReadOnlyRequested || !mpDiskImage->IsUpdatable();

	return true;
}

void ATDeviceDiskDrive::Init() {
	mpEmulationProfile = &kEmulationProfiles[1];
	mHighSpeedCmdDivisorLo = 1;
	mHighSpeedCmdDivisorHi = 255;
	mCyclesPerRotation = mpEmulationProfile->mbRPM300 ? kCyclesPerDiskRotation_300RPM : kCyclesPerDiskRotation_288RPM;
}

void ATDeviceDiskDrive::Shutdown() {
	MountDisk(nullptr);

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}

	mpScheduler = nullptr;
}

void ATDeviceDiskDrive::ColdReset() {
	mWeakBitLFSR = 1;
}

void ATDeviceDiskDrive::MountDisk(const wchar_t *path) {
	if (mpDiskImage) {
		try {
			mpDiskImage->Flush();
		} catch(const MyError&) {
		}

		mpDiskImage = nullptr;
	}

	mPath.clear();

	if (!path)
		return;

	ATLoadDiskImage(path, ~mpDiskImage);

	mbReadOnly = mbReadOnlyRequested || !mpDiskImage->IsUpdatable();

	mPath = path;
}

void ATDeviceDiskDrive::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;

	mRotationTime = ATSCHEDULER_GETTIME(mpScheduler);
}

void ATDeviceDiskDrive::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;

	mgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	bool hs = false;

	// Check if we have a high-speed command frame
	if (!cmd.mbStandardRate) {
		if (cmd.mCyclesPerBit < mHighSpeedCmdDivisorLo || cmd.mCyclesPerBit > mHighSpeedCmdDivisorHi)
			return kCmdResponse_NotHandled;

		hs = true;
	}

	if (cmd.mDevice != mDeviceId)
		return kCmdResponse_NotHandled;

	mCommand = cmd;
	mbHighSpeedCommand = hs;
	mbHighSpeedData = (cmd.mCommand & 0x80) != 0;

	// check if it is an XF551-style high speed command and if we actually support it
	if (mbHighSpeedData && !mbHighSpeedXF551Enabled)
		return kCmdResponse_Fail_NAK;

	switch(cmd.mCommand) {
		case 0x21:		// format
			return OnCmdFormat();

		case 0xA1:		// format with high speed skew
			return OnCmdFormatHighSpeedSkew();

		case 0x22:		// format medium
		case 0xA2:		// format medium w/ high speed skew
			return OnCmdFormatMedium();

		case 0x3F:
			return OnCmdGetHighSpeedIndex();

		case 0x4E:		// read PERCOM block
		case 0xCE:		// read PERCOM block (XF551 high speed)
			return OnCmdReadPERCOMBlock();

		case 0x4F:		// write PERCOM block
		case 0xCF:		// write PERCOM block (XF551 high speed)
			return OnCmdWritePERCOMBlock();

		case 0x50:		// put sector (write without verify)
		case 0xD0:		// put sector (write without verify) (XF551 high speed)
			return OnCmdPutSector();

		case 0x52:		// read sector
		case 0xD2:		// read sector (XF551 high speed)
			return OnCmdReadSector();

		case 0x53:		// status
		case 0xD3:		// status (XF551 high speed)
			return OnCmdGetStatus();

		case 0x57:		// write sector (write with verify)
		case 0xD7:		// write sector (write with verify) (XF551 high speed)
			return OnCmdWriteSector();
	}

	// unrecognized command
	return kCmdResponse_Fail_NAK;
}

void ATDeviceDiskDrive::OnSerialAbortCommand() {
}

void ATDeviceDiskDrive::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
	switch(id) {
		case 'P':	// put (no verify)
			OnCmdPutSector2(data, len);
			break;

		case 'W':	// write (verify)
			OnCmdWriteSector2(data, len);
			break;
	}
}

void ATDeviceDiskDrive::OnSerialFence(uint32 id) {
	vdfunction<void()> fn = std::move(mpFenceFn);

	if (fn) {
		mpFenceFn = nullptr;

		fn();
	}
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdGetHighSpeedIndex() {
	if (mpEmulationProfile->mHighSpeedIndex < 0)
		return kCmdResponse_Fail_NAK;

	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->SendComplete();

//	const uint8 hsindex = (uint8)mpEmulationProfile->mHighSpeedIndex;
	const uint8 hsindex = 8;
	mpSIOMgr->SendData(&hsindex, 1, true);
	mpSIOMgr->EndCommand();

	return kCmdResponse_Start;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdReadSector() {
	uint16 sector = VDReadUnalignedLEU16(mCommand.mAUX);

	if (!mpDiskImage || !sector || sector > mpDiskImage->GetVirtualSectorCount())
		return kCmdResponse_Fail_NAK;

	uint32 sectorSize = mpDiskImage->GetSectorSize(sector - 1);

	ATDiskVirtualSectorInfo vsi;
	mpDiskImage->GetVirtualSectorInfo(sector - 1, vsi);

	if (vsi.mNumPhysSectors == 0) {
		// indicate missing track/sector (record not found), then send
		// back an error
		mFDCStatus = 0xEF;

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendError();
		mpSIOMgr->SendData(mSectorBuffer, sectorSize, true);
		mpSIOMgr->EndCommand();
	} else {
		// apply initial command processing, ACK offset, and FDC command offset
		const uint32 initialDelay = 7231;

		// find the nearest sector rotationally
		UpdateRotationalPosition();

		uint32 rotOffsetAdjusted = mRotationOffset;
		rotOffsetAdjusted = (rotOffsetAdjusted + initialDelay) % mCyclesPerRotation;

		const float frot = rotOffsetAdjusted / (float)mCyclesPerRotation;

		ATDiskPhysicalSectorInfo psi;
		uint32 bestPhyIndex = 0;
		float bestDelay = 10.0f;

		for(uint32 i=0; i<vsi.mNumPhysSectors; ++i) {
			mpDiskImage->GetPhysicalSectorInfo(vsi.mStartPhysSector + i, psi);

			float rotOff = psi.mRotPos - frot;
			rotOff -= floorf(rotOff);

			if (bestDelay > rotOff) {
				bestDelay = rotOff;

				bestPhyIndex = vsi.mStartPhysSector + i;
			}
		}

		const uint32 rotationalDelayCycles = VDRoundToInt(bestDelay * (float)mCyclesPerRotation);

		mpDiskImage->GetPhysicalSectorInfo(bestPhyIndex, psi);

		// Set FDC status.
		//
		// Note that in order to get a lost data condition (bit 2), there must already have
		// been data pending (bit 1) when more data arrived. The 810 ROM does not clear the
		// DRQ flag before storing status. The Music Studio requires this.
		mFDCStatus = psi.mFDCStatus;
		if (!(mFDCStatus & 0x04))
			mFDCStatus &= ~0x02;

		try {
			mpDiskImage->ReadPhysicalSector(bestPhyIndex, mSectorBuffer, sectorSize);
		} catch(const MyError&) {
			// fake a CRC error
			mFDCStatus = 0xF7;
		}

		// Check if we should emulate weak bits.
		if (psi.mWeakDataOffset >= 0) {
			for(int i = psi.mWeakDataOffset; i < (int)psi.mPhysicalSize; ++i) {
				mSectorBuffer[i] ^= (uint8)mWeakBitLFSR;

				mWeakBitLFSR = (mWeakBitLFSR << 8) + (0xff & ((mWeakBitLFSR >> (28 - 8)) ^ (mWeakBitLFSR >> (31 - 8))));
			}
		}

		// Compute time delay to start of complete/error byte
		//
		// Read sector: 156 raw bytes @ 125000/8 bytes/second = 17869.1 cycles
		// Status and checksumming: 2581 drive cycles = 9240.0 cycles
		// Transmit complete/error and sector: 34335 drive cycles = 122919.3 cycles

		const uint32 kFDCStartReadToStartCompleteCycles = 27109;
		const uint32 kReturnDataCycles = 122919;

		// Warp disk rotation to end of sector
		uint32 finalRotationalOffset = VDRoundToInt(psi.mRotPos * mCyclesPerRotation);

		//if (mpSIOMgr->IsAccelRequest())
			finalRotationalOffset += kFDCStartReadToStartCompleteCycles + kReturnDataCycles;
		
		finalRotationalOffset %= mCyclesPerRotation;

		g_ATLCDisk("Reading vsec=%3d (%d/%d) (trk=%d), psec=%3d, rot=%.2f >[%+.2f]> %.2f >> %.2f%s.\n"
				, sector
				, bestPhyIndex - vsi.mStartPhysSector + 1
				, vsi.mNumPhysSectors
				, (sector - 1) / 18
				, bestPhyIndex
				, (float)mRotationOffset / (float)mCyclesPerRotation
				, bestDelay
				, psi.mRotPos
				, (float)finalRotationalOffset / (float)mCyclesPerRotation
				,  psi.mWeakDataOffset >= 0 ? " (w/weak bits)"
					: !(mFDCStatus & 0x04) ? " (w/long sector)"
					: !(mFDCStatus & 0x08) ? " (w/CRC error)"
					: !(mFDCStatus & 0x10) ? " (w/missing sector)"
					: !(mFDCStatus & 0x20) ? " (w/deleted sector)"
					: ""
				);

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();

		if (mbAccurateTiming)
			mpSIOMgr->Delay(rotationalDelayCycles + kFDCStartReadToStartCompleteCycles);

		if (mFDCStatus == 0xFF)
			mpSIOMgr->SendComplete();
		else
			mpSIOMgr->SendError();

		mpSIOMgr->SendData(mSectorBuffer, sectorSize, true);

		mpFenceFn = [finalRotationalOffset, this]() {
			mRotationOffset = finalRotationalOffset;
			mRotationTime = ATSCHEDULER_GETTIME(mpScheduler);
		};
		mpSIOMgr->InsertFence(0);

		mpSIOMgr->EndCommand();
	}

	return kCmdResponse_Start;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdGetStatus() {
	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->SendComplete();

	uint8 status[4] = {
		0,
		mFDCStatus,
		mpEmulationProfile->mTimeout,
		0
	};

	if (mPERCOM[4] > 0)
		status[0] += 0x40;

	// We need to check the sector size in the PERCOM block and not the physical
	// disk for this value. This is required as SmartDOS 8.2D does a Write PERCOM
	// Block command and then depends on FM/MFM selection being reflected in the
	// result.
	if (mPERCOM[6])		// sector size high byte
		status[0] += 0x20;

	if (mbReadOnly)
		status[0] += 0x08;

	if (mbLastOpError)
		status[0] += 0x04;

	mpSIOMgr->SendData(status, 4, true);
	mpSIOMgr->EndCommand();
	return kCmdResponse_Start;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdPutSector() {
	uint16 sector = VDReadUnalignedLEU16(mCommand.mAUX);

	if (!sector || sector > mpDiskImage->GetVirtualSectorCount())
		return kCmdResponse_Fail_NAK;
	
	uint32 sectorSize = mpDiskImage->GetSectorSize(sector - 1);

	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->ReceiveData('P', sectorSize, true);
	return kCmdResponse_Start;
}

void ATDeviceDiskDrive::OnCmdPutSector2(const void *buf, uint32 len) {
	if (mbReadOnly) {
		mpSIOMgr->SendError();
		mpSIOMgr->EndCommand();
		return;
	}

	uint16 sector = VDReadUnalignedLEU16(mCommand.mAUX);

	// need to copy data to sector buffer in case we read it back later
	memcpy(mSectorBuffer, buf, len);

	try {
		mpDiskImage->WriteVirtualSector(sector - 1, buf, len);
	} catch(const MyError&) {
		mpSIOMgr->SendError();
		mpSIOMgr->EndCommand();
		return;
	}

	mpSIOMgr->SendComplete();
	mpSIOMgr->EndCommand();	
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdWriteSector() {
	uint16 sector = VDReadUnalignedLEU16(mCommand.mAUX);

	if (!sector || sector > mpDiskImage->GetVirtualSectorCount())
		return kCmdResponse_Fail_NAK;
	
	uint32 sectorSize = mpDiskImage->GetSectorSize(sector - 1);

	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->ReceiveData('W', sectorSize, true);
	return kCmdResponse_Start;
}

void ATDeviceDiskDrive::OnCmdWriteSector2(const void *buf, uint32 len) {
	if (mbReadOnly) {
		mpSIOMgr->SendError();
		mpSIOMgr->EndCommand();
		return;
	}

	uint16 sector = VDReadUnalignedLEU16(mCommand.mAUX);

	// need to copy data to sector buffer in case we read it back later
	memcpy(mSectorBuffer, buf, len);

	try {
		mpDiskImage->WriteVirtualSector(sector - 1, buf, len);
	} catch(const MyError&) {
		mpSIOMgr->SendError();
		mpSIOMgr->EndCommand();
		return;
	}

	mpSIOMgr->SendComplete();
	mpSIOMgr->EndCommand();
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdReadPERCOMBlock() {
	if (!mpEmulationProfile->mbPERCOMSupported)
		return kCmdResponse_Fail_NAK;

	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->SendComplete();
	mpSIOMgr->SendData(mPERCOM, sizeof mPERCOM, true);
	mpSIOMgr->EndCommand();

	return kCmdResponse_Start;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdWritePERCOMBlock() {
	if (!mpEmulationProfile->mbPERCOMSupported)
		return kCmdResponse_Fail_NAK;

	mpSIOMgr->BeginCommand();
	mpSIOMgr->SendACK();
	mpSIOMgr->SendComplete();
	mpSIOMgr->ReceiveData(0x4F, sizeof mPERCOM, true);
	mpSIOMgr->EndCommand();

	return kCmdResponse_Start;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdFormat() {
	return kCmdResponse_NotHandled;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdFormatHighSpeedSkew() {
	return kCmdResponse_NotHandled;
}

IATDeviceSIO::CmdResponse ATDeviceDiskDrive::OnCmdFormatMedium() {
	return kCmdResponse_NotHandled;
}

void ATDeviceDiskDrive::UpdateRotationalPosition() {
	uint64 t = ATSCHEDULER_GETTIME(mpScheduler);

	mRotationOffset = (t - mRotationTime + mRotationOffset) % mCyclesPerRotation;

	mRotationTime = t;
}
