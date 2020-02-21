//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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
#include <vd2/system/date.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicesio.h>
#include <at/atcore/scheduler.h>

// SIO high-speed test device
//
// This device simulates an external SIO device transmitting data at
// 447Kbps (mclk/4). This requires an external clock as POKEY cannot
// drive its clock internally that fast.
//
class ATDeviceTestSIOHighSpeed final : public ATDevice, public IATDeviceScheduling, public IATDeviceSIO, public IATDeviceRawSIO, public IATSchedulerCallback
{
public:
	virtual void *AsInterface(uint32 id) override;

	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void Shutdown() override;

public:	// IATDevice
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceSIO
	virtual void InitSIO(IATDeviceSIOManager *siomgr) override;
	virtual CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	virtual void OnSerialAbortCommand() override;
	virtual void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool transferOK) override;
	virtual void OnSerialFence(uint32 id) override;
	virtual CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

public:	// IATDeviceRawSIO
	virtual void OnCommandStateChanged(bool asserted) override;
	virtual void OnMotorStateChanged(bool asserted) override;
	virtual void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	virtual void OnSendReady() override;

public:	// IATSchedulerCallback
	virtual void OnScheduledEvent(uint32 id) override;

private:
	ATScheduler *mpScheduler = nullptr;
	ATEvent *mpTransferEvent = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;
	bool mbRawActive = false;
	uint32 mTransferIndex = 0;
	uint32 mTransferLength = 0;
	uint32 mTransferSector = 0;
	uint32 mCyclesPerBit = 6;
	uint32 mCyclesPerByte = 60;
};

void ATCreateDeviceTestSIOHighSpeed(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceTestSIOHighSpeed> p(new ATDeviceTestSIOHighSpeed);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefTestSIOHighSpeed = { "testsiohs", nullptr, L"SIO High Speed Test Device", ATCreateDeviceTestSIOHighSpeed };

void *ATDeviceTestSIOHighSpeed::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:
			return static_cast<IATDeviceScheduling *>(this);

		case IATDeviceSIO::kTypeID:
			return static_cast<IATDeviceSIO *>(this);

		case IATDeviceRawSIO::kTypeID:
			return static_cast<IATDeviceRawSIO *>(this);

		default:
			return ATDevice::AsInterface(id);
	}
}

void ATDeviceTestSIOHighSpeed::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefTestSIOHighSpeed;
}

void ATDeviceTestSIOHighSpeed::Shutdown() {
	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpTransferEvent);
		mpScheduler = nullptr;
	}

	if (mpSIOMgr) {
		if (mbRawActive) {
			mbRawActive = false;

			mpSIOMgr->RemoveRawDevice(this);
		}

		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}
}

void ATDeviceTestSIOHighSpeed::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATDeviceTestSIOHighSpeed::InitSIO(IATDeviceSIOManager *siomgr) {
	mpSIOMgr = siomgr;
	mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATDeviceTestSIOHighSpeed::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (cmd.mDevice != 0x31 || !cmd.mbStandardRate)
		return kCmdResponse_NotHandled;

	if (mbRawActive) {
		mbRawActive = false;

		mpSIOMgr->RemoveRawDevice(this);
	}

	if (cmd.mCommand == 0x52) {		// 'R'ead
		const uint32 sector = VDReadUnalignedLEU16(cmd.mAUX);

		if (sector == 0 || sector > 720)
			return kCmdResponse_Fail_NAK;

		mTransferIndex = 0;
		mTransferLength = 128;
		mTransferSector = sector;
			
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->Delay(2000);
		mpSIOMgr->InsertFence(0);

		return kCmdResponse_Start;

	} else if (cmd.mCommand == 0x53) {		// 'S'tatus
		mTransferIndex = 0;
		mTransferLength = 4;
			
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->Delay(2000);
		mpSIOMgr->InsertFence(0);

		return kCmdResponse_Start;

	} else if (cmd.mCommand == 0x47) {		// 'G'et
		// check if transfer length is valid
		const uint32 len = VDReadUnalignedLEU16(cmd.mAUX);

		if (!len)
			return kCmdResponse_Fail_NAK;

		mTransferIndex = 0;
		mTransferLength = len;
		mTransferSector = 1;
			
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->Delay(2000);
		mpSIOMgr->InsertFence(0);

		return kCmdResponse_Start;
	}

	// eh, we don't know this command
	return kCmdResponse_Fail_NAK;
}

void ATDeviceTestSIOHighSpeed::OnSerialAbortCommand() {
	mpScheduler->UnsetEvent(mpTransferEvent);

	if (mbRawActive) {
		mbRawActive = false;

		mpSIOMgr->RemoveRawDevice(this);
	}
}

void ATDeviceTestSIOHighSpeed::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool transferOK) {
}

void ATDeviceTestSIOHighSpeed::OnSerialFence(uint32 id) {
	if (!mbRawActive) {
		mbRawActive = true;

		mpSIOMgr->AddRawDevice(this);
		mpSIOMgr->SetExternalClock(this, 0, mCyclesPerBit);

		OnSendReady();
	}
}

IATDeviceSIO::CmdResponse ATDeviceTestSIOHighSpeed::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}

void ATDeviceTestSIOHighSpeed::OnCommandStateChanged(bool asserted) {
}

void ATDeviceTestSIOHighSpeed::OnMotorStateChanged(bool asserted) {
}

void ATDeviceTestSIOHighSpeed::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
}

void ATDeviceTestSIOHighSpeed::OnSendReady() {
	VDASSERT(mbRawActive);

	if (mTransferIndex == 0) {
		mpScheduler->SetEvent(mCyclesPerByte + 1000, this, 1, mpTransferEvent);

		mpSIOMgr->SendRawByte(0x43, mCyclesPerBit);
		++mTransferIndex;
	} else if (mTransferIndex <= mTransferLength+1) {
		mpScheduler->SetEvent(mCyclesPerByte, this, 1, mpTransferEvent);

		if (mTransferIndex <= mTransferLength) {
			mpSIOMgr->SendRawByte((uint8)((mTransferIndex - 1) + mTransferSector), mCyclesPerBit);
		} else {
			uint32 sum = ((mTransferLength + mTransferSector*2 - 1) * mTransferLength) >> 1;
			
			sum = (sum >> 16) + (sum & 0xffff);
			sum = (sum >> 16) + (sum & 0xffff);
			sum = (sum >> 8) + (sum & 0xff);
			sum = (sum >> 8) + (sum & 0xff);

			mpSIOMgr->SendRawByte((uint8)sum, mCyclesPerBit);
		}

		++mTransferIndex;
	} else {
		mpScheduler->UnsetEvent(mpTransferEvent);

		if (mbRawActive) {
			mbRawActive = false;

			mpSIOMgr->RemoveRawDevice(this);
		}

		mpSIOMgr->EndCommand();
	}
}

void ATDeviceTestSIOHighSpeed::OnScheduledEvent(uint32 id) {
	mpTransferEvent = nullptr;

	OnSendReady();
}
