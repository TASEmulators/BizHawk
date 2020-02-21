//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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
#include <at/atcore/blockdevice.h>
#include <at/atemulation/scsi.h>
#include "scsidisk.h"
#include "uirender.h"

class ATSCSIDiskDevice : public vdrefcounted<IATSCSIDiskDevice> {
public:
	ATSCSIDiskDevice();

	void *AsInterface(uint32 iid);

	IATBlockDevice *GetDisk() const { return mpDisk; }

	void Init(IATBlockDevice *disk);

	void SetUIRenderer(IATDeviceIndicatorManager *r);

	virtual void Attach(ATSCSIBusEmulator *bus);
	virtual void Detach();

	virtual void BeginCommand(const uint8 *command, uint32 length);
	virtual void AdvanceCommand();
	virtual void AbortCommand();

	virtual void SetBlockSize(uint32 blockSize);

protected:
	enum State {
		kState_None,
		kState_RequestSense_0,
		kState_TestUnitReady_0,
		kState_Read_0,
		kState_Read_1,
		kState_Write_0,
		kState_Write_1,
		kState_Write_2,
		kState_Inquiry_0,
		kState_ReadCapacity_0,
		kState_UnknownCommand,
		kState_Status,
		kState_Complete
	};

	void DecodeCmdGroup0(const uint8 *command);
	void DecodeCmdGroup1(const uint8 *command);

	ATSCSIBusEmulator *mpBus;
	IATDeviceIndicatorManager *mpUIRenderer;
	vdrefptr<IATBlockDevice> mpDisk;

	State mState;
	uint8 mLUN;
	uint32 mLBA;
	uint32 mBlockCount;
	bool mbClearErrorNextCommand;
	uint8 mError;
	uint32 mErrorLBA;
	bool mbBlockSize256;
	
	ATBlockDeviceGeometry mDiskGeometry;

	uint8 mCommandBuffer[16];
	uint8 mTransferBuffer[512];
	uint8 mSectorBuffer[512];

	static const float kIODelaySlow;
};

const float ATSCSIDiskDevice::kIODelaySlow = 5500.0f;		// 5.5ms

ATSCSIDiskDevice::ATSCSIDiskDevice()
	: mpBus(nullptr)
	, mpUIRenderer(nullptr)
	, mpDisk()
	, mState(kState_None)
	, mbClearErrorNextCommand(false)
	, mError(0)
	, mErrorLBA(0)
	, mbBlockSize256(false)
{
}

void *ATSCSIDiskDevice::AsInterface(uint32 iid) {
	switch(iid) {
		case IATSCSIDevice::kTypeID: return static_cast<IATSCSIDevice *>(this);
	}

	return nullptr;
}

void ATSCSIDiskDevice::Init(IATBlockDevice *disk) {
	mpDisk = disk;
	mDiskGeometry = disk->GetGeometry();
}

void ATSCSIDiskDevice::SetUIRenderer(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATSCSIDiskDevice::Attach(ATSCSIBusEmulator *bus) {
	mpBus = bus;
}

void ATSCSIDiskDevice::Detach() {
	mpBus = nullptr;
}

void ATSCSIDiskDevice::BeginCommand(const uint8 *command, uint32 length) {
	// INQUIRY ($12) does not clear pending check condition status.
	if (mbClearErrorNextCommand && command[0] != 0x12) {
		mbClearErrorNextCommand = false;

		mError = 0;
	}

	memcpy(mCommandBuffer, command, std::min<uint32>(length, sizeof mCommandBuffer));

	switch(command[0] & 0xe0) {
		case 0x00:
			DecodeCmdGroup0(command);
			break;

		case 0x20:
			DecodeCmdGroup1(command);
			break;
	}

	switch(command[0]) {
		case 0x00:	// test unit ready
			mState = kState_TestUnitReady_0;
			break;

		case 0x03:	// request sense
			mState = kState_RequestSense_0;
			break;

		case 0x08:	// read (group 0)
		case 0x28:	// read (group 1)
			mState = kState_Read_0;
			break;

		case 0x0A:	// write (group 0)
		case 0x2A:	// write (group 1)
			mState = kState_Write_0;
			break;

		case 0x12:	// inquiry
			mState = kState_Inquiry_0;
			break;

		case 0x25:	// read capacity
			mState = kState_ReadCapacity_0;
			break;

		default:
			mState = kState_UnknownCommand;
			break;
	}
}

void ATSCSIDiskDevice::AdvanceCommand() {
	switch(mState) {
		case kState_RequestSense_0:
			mbClearErrorNextCommand = true;

			mTransferBuffer[0] = mError;
			mTransferBuffer[1] = (uint8)(mErrorLBA >> 16);
			mTransferBuffer[2] = (uint8)(mErrorLBA >> 8);
			mTransferBuffer[3] = (uint8)mErrorLBA;

			mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_DataIn, mTransferBuffer, 4);
			mState = kState_Status;
			break;

		case kState_TestUnitReady_0:
			mError = 0;
			mState = kState_Status;
			break;

		case kState_Read_0:
			if (mLBA >= (mpDisk->GetSectorCount() * (mbBlockSize256 ? 2 : 1))) {
				mError = 0x21;		// Class 2 Illegal block address
				mState = kState_Status;
			} else {
				if (!mDiskGeometry.mbSolidState)
					mpBus->CommandDelay(kIODelaySlow);

				mState = kState_Read_1;
			}
			break;

		case kState_Read_1:
			try {
				if (mpUIRenderer)
					mpUIRenderer->SetIDEActivity(false, mLBA);

				if (mbBlockSize256) {
					mpDisk->ReadSectors(mSectorBuffer, mLBA >> 1, 1);

					memcpy(mTransferBuffer, mSectorBuffer + (mLBA & 1 ? 256 : 0), 256);

					mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_DataIn, mTransferBuffer, 256);
				} else {
					mpDisk->ReadSectors(mTransferBuffer, mLBA, 1);
					mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_DataIn, mTransferBuffer, 512);
				}

				++mLBA;

				if (!--mBlockCount) {
					mError = 0;
					mState = kState_Status;
				}
			} catch(const MyError&) {
				mError = 0x21;		// Class 2 Illegal block address
				mState = kState_Status;
			}
			break;

		case kState_Write_0:
			if (mpDisk->IsReadOnly()) {
				mError = 0x17;		// Class 1 Write Protected
				mState = kState_Status;
			} else if (mLBA >= mpDisk->GetSectorCount()) {
				mError = 0x21;		// Class 2 Illegal block address
				mState = kState_Status;
			} else {
				mpBus->CommandReceiveData(ATSCSIBusEmulator::kReceiveMode_DataOut, mTransferBuffer, mbBlockSize256 ? 256 : 512);
				mState = kState_Write_1;
			}
			break;

		case kState_Write_1:
			if (!mDiskGeometry.mbSolidState)
				mpBus->CommandDelay(kIODelaySlow);

			mState = kState_Write_2;
			break;

		case kState_Write_2:
			try {
				if (mpUIRenderer)
					mpUIRenderer->SetIDEActivity(true, mLBA);

				if (mbBlockSize256) {
					mpDisk->ReadSectors(mSectorBuffer, mLBA >> 1, 1);

					memcpy(mSectorBuffer + (mLBA & 1 ? 256 : 0), mTransferBuffer, 256);

					mpDisk->WriteSectors(mSectorBuffer, mLBA >> 1, 1);
				} else {
					mpDisk->WriteSectors(mTransferBuffer, mLBA, 1);
				}

				++mLBA;

				if (--mBlockCount)
					mState = kState_Write_0;
				else {
					mError = 0;
					mState = kState_Status;
				}
			} catch(const MyError&) {
				mError = 0x11;		// Class 1 Uncorrectable data error
				mState = kState_Status;
			}
			break;

		case kState_Inquiry_0:
			// check if enable vital product data (EVPD) was set, and raise ILLEGAL REQUEST if so
			if (mCommandBuffer[1] & 0x01) {
				mError = 0x70;	// TODO: Implement extended sense data
				mState = kState_Status;
				break;
			}

			// For now, return very simple inquiry data
			mTransferBuffer[0] = 0x00;	// direct access device
			mTransferBuffer[1] = 0x00;	// no additional bytes
			mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_DataIn, mTransferBuffer, 2);
			mError = 0;
			mState = kState_Status;
			break;

		case kState_ReadCapacity_0:
			VDWriteUnalignedBEU32(mTransferBuffer + 0, (mpDisk->GetSectorCount() * (mbBlockSize256 ? 2 : 1)) - 1);

			// Note that SASI specifies this as 16-bit, but SCSI uses 32-bit.
			VDWriteUnalignedBEU32(mTransferBuffer + 4, mbBlockSize256 ? 256 : 512);

			mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_DataIn, mTransferBuffer, 8);
			mError = 0;
			mState = kState_Status;
			break;

		case kState_UnknownCommand:
			mState = kState_Status;
			break;

		case kState_Status:
			mTransferBuffer[0] = 0x80 + (mError ? 0x02 : 0x00);
			mTransferBuffer[1] = 0x00;
			mpBus->CommandSendData(ATSCSIBusEmulator::kSendMode_Status, mTransferBuffer, 2);
			mState = kState_Complete;
			break;

		case kState_Complete:
			mpBus->CommandEnd();
			mState = kState_None;
			break;
	}
}

void ATSCSIDiskDevice::AbortCommand() {
	mState = kState_None;
}

void ATSCSIDiskDevice::SetBlockSize(uint32 blockSize) {
	if (blockSize == 512)
		mbBlockSize256 = false;
	else if (blockSize == 256)
		mbBlockSize256 = true;
}

void ATSCSIDiskDevice::DecodeCmdGroup0(const uint8 *command) {
	mLUN = command[1] >> 5;
	mLBA = VDReadUnalignedBEU32(command) & 0x1FFFFF;
	mBlockCount = command[4];
	if (!mBlockCount)
		mBlockCount = 256;
}

void ATSCSIDiskDevice::DecodeCmdGroup1(const uint8 *command) {
	mLUN = command[1] >> 5;
	mLBA = VDReadUnalignedBEU32(command + 2);
	mBlockCount = VDReadUnalignedBEU16(command + 7);
	if (!mBlockCount)
		mBlockCount = 65536;
}

///////////////////////////////////////////////////////////////////////////

void ATCreateSCSIDiskDevice(IATBlockDevice *disk, IATSCSIDiskDevice **dev) {
	vdrefptr<ATSCSIDiskDevice> p(new ATSCSIDiskDevice);
	p->Init(disk);

	*dev = p.release();
}
