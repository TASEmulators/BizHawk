//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - Corvus Disk System emulation
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_ATDEVICES_CORVUS_H
#define f_AT_ATDEVICES_CORVUS_H

#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/blockdevice.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceparentimpl.h>

class ATPropertySet;

class ATCorvusEmulator {
public:
	ATCorvusEmulator();

	void Init(IATDeviceIndicatorManager *indMgr) { mpIndMgr = indMgr; }

	IATBlockDevice *GetAttachedDevice() const { return mpBlockDevice; }
	void SetAttachedDevice(IATBlockDevice *dev) { mpBlockDevice = dev; }

	bool IsReady() const;
	bool IsWaitingForReceive() const;

	void ColdReset();

	uint8 ReceiveByte(uint64 t);
	void SendByte(uint64 t, uint8 c);

private:
	enum ReturnCode : uint8 {
		kReturnCode_Success			= 0x00,

		kReturnCode_HeaderFault		= 0x00,
		kReturnCode_SeekTimeout		= 0x01,
		kReturnCode_SeekFault		= 0x02,
		kReturnCode_SeekError		= 0x03,
		kReturnCode_HeaderCRCError	= 0x04,
		kReturnCode_RezeroFault		= 0x05,
		kReturnCode_RezeroTimeout	= 0x06,
		kReturnCode_DriveNotOnline	= 0x07,
		kReturnCode_WriteFault		= 0x08,
		kReturnCode_ReadDataFault	= 0x0A,
		kReturnCode_DataCRCError	= 0x0B,
		kReturnCode_SectorLocateError	= 0x0C,
		kReturnCode_WriteProtected	= 0x0D,
		kReturnCode_IllegalSectorAddress	= 0x0E,
		kReturnCode_IllegalCommandOpCode	= 0x0F,
		kReturnCode_DriveNotAcknowledged	= 0x10,
		kReturnCode_AcknowledgeStuckActive	= 0x11,
		kReturnCode_Timeout			= 0x12,
		kReturnCode_Fault			= 0x13,
		kReturnCode_CRC				= 0x14,
		kReturnCode_Seek			= 0x15,
		kReturnCode_Verification	= 0x16,
		kReturnCode_DriveSpeedError	= 0x17,
		kReturnCode_DriveIllegalAddressError	= 0x18,
		kReturnCode_DriveRWFaultError	= 0x19,
		kReturnCode_DriveServoError	= 0x1A,
		kReturnCode_DriveGuardBand	= 0x1B,
		kReturnCode_DrivePLOError	= 0x1C,
		kReturnCode_DriveRWUnsafe	= 0x1D,

		kReturnCode_RecoverableError	= 0x20,
		kReturnCode_VerifyError			= 0x40,
		kReturnCode_HardError			= 0x80,
	};

	void DoCommand();
	void DoCommandFirstByte();

	void InitReply(uint32 len, uint8 status);

	IATDeviceIndicatorManager *mpIndMgr = nullptr;

	bool mbReceiveMode = false;
	bool mbPrepMode = false;

	uint64 mLastReceiveTime = 0;

	uint32	mUserAreaBlockStart = 0;
	uint32	mUserAreaBlockCount = 0;
	uint32	mPhysicalBlockCount = 0;

	vdrefptr<IATBlockDevice> mpBlockDevice;

	uint32 mTransferIndex = 0;
	uint32 mTransferLength = 0;
	uint8 mTransferBuffer[516] = { };
	uint8 mSectorBuffer[512];
};

class ATDeviceCorvus final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDevicePortInput
	, public IATDeviceIndicators
{
	ATDeviceCorvus(const ATDeviceCorvus&) = delete;
	ATDeviceCorvus& operator=(const ATDeviceCorvus&) = delete;

public:
	ATDeviceCorvus();
	~ATDeviceCorvus();
	
	void *AsInterface(uint32 iid) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& pset) override;
	bool SetSettings(const ATPropertySet& pset) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;

public:	// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDevicePortInput
	void InitPortInput(IATDevicePortManager *portmgr) override;

public:	// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *indmgr) override;

private:
	void OnPortOutputChanged(uint32 outputState);
	void ReinitPortOutput();

	ATScheduler *mpScheduler = nullptr;
	IATDeviceIndicatorManager *mpIndMgr = nullptr;
	IATDevicePortManager *mpPortManager = nullptr;
	int mPortInput = -1;
	int mPortOutput = -1;
	uint32 mLastPortState = 0;
	uint8 mPortShift = 0;

	uint8 mDataLatch = 0;

	ATCorvusEmulator mCorvusEmu;
	ATDeviceParentSingleChild mDeviceParent;
};

#endif
