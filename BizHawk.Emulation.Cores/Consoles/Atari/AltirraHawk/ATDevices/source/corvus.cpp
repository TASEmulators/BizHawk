//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - Corvus Disk Interface emulation
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

#include <stdafx.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <at/atcore/deviceport.h>
#include <at/atcore/deviceindicators.h>
#include <at/atcore/logging.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include <at/atdevices/corvus.h>

ATLogChannel g_ATLCCorvus(false, false, "CORVUS", "Corvus Disk Interface activity");

void ATCreateDeviceCorvus(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceCorvus> p(new ATDeviceCorvus);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefCorvus = { "corvus", "corvus", L"Corvus Disk Interface", ATCreateDeviceCorvus };

///////////////////////////////////////////////////////////////////////////

ATCorvusEmulator::ATCorvusEmulator() {
	// What used to be mass storage... is now an array on the heap. We are
	// using revision H geometry here (GTI p. 200):
	//
	//	- 512 bytes/sector
	//	- 20 sectors per track
	//	- 2 heads
	//	- 306 tracks
	//
	// Cylinder 0 is reserved for firmware use. Cylinder 1 is a mirror of
	// cylinder 0. 31 tracks are reserved for spares. This leads to a
	// physical block count of 12240 blocks and a user area of 11540 blocks.

	mPhysicalBlockCount = 20 * 2 * 306;
	mUserAreaBlockStart = 20 * 2 * 2;
	mUserAreaBlockCount = mPhysicalBlockCount - mUserAreaBlockStart - 31 * 20;
}

bool ATCorvusEmulator::IsReady() const {
	return true;
}

bool ATCorvusEmulator::IsWaitingForReceive() const {
	return mbReceiveMode;
}

void ATCorvusEmulator::ColdReset() {
	mbReceiveMode = false;
	mbPrepMode = false;
	mTransferIndex = 0;
	mTransferLength = 1;
}

uint8 ATCorvusEmulator::ReceiveByte(uint64 t) {
	if (!mbReceiveMode)
		return 0xFF;

	const uint8 c = mTransferBuffer[mTransferIndex++];

	if (mTransferIndex >= mTransferLength) {
		mbReceiveMode = false;
		mTransferIndex = 0;
		mTransferLength = 1;
	}

	return c;
}

void ATCorvusEmulator::SendByte(uint64 t, uint8 c) {
	if (mbReceiveMode)
		return;

	// The command timeout is four seconds (Corvus Mass Storage GTI, page 203). If it's
	// been longer than that, restart the transfer. The Corvus always does a rigid
	// receive-do-send pattern, so sends are always command related.
	if (mTransferIndex > 0 && t - mLastReceiveTime > 7000000)
		mTransferIndex = 0;

	mLastReceiveTime = t;

	mTransferBuffer[mTransferIndex++] = c;

	if (mTransferIndex == 1)
		DoCommandFirstByte();
	else if (mTransferIndex >= mTransferLength)
		DoCommand();
}

void ATCorvusEmulator::DoCommandFirstByte() {
	if (mbPrepMode) {
		switch(mTransferBuffer[0]) {
			case 0x00:	// reset drive
			case 0x07:	// verify drive
				DoCommand();
				break;

			case 0x32:	// read Corvus firmware
				mTransferLength = 2;
				break;

			case 0x33:	// write Corvus firmware
				mTransferLength = 514;
				break;

			default:
				InitReply(1, kReturnCode_IllegalCommandOpCode | kReturnCode_HardError);
				break;
		}

		return;
	}

	switch(mTransferBuffer[0]) {
		case 0x12:		// read sector, 128 bytes
		case 0x02:		// read sector, 256 bytes
		case 0x22:		// read sector, 256 bytes (alias)
		case 0x32:		// read sector, 512 bytes
			mTransferLength = 4;
			break;

		case 0x13:		// write sector, 128 bytes
			mTransferLength = 128 + 4;
			break;

		case 0x03:		// write sector, 256 bytes
		case 0x23:		// write sector, 256 bytes (alias)
			mTransferLength = 256 + 4;
			break;

		case 0x33:		// write sector, 512 bytes
			mTransferLength = 512 + 4;
			break;

		case 0x10:		// get drive parameters
			mTransferLength = 2;
			break;

		case 0x11:		// park heads / prep mode select
			mTransferLength = 514;
			break;

		default:
			InitReply(1, kReturnCode_IllegalCommandOpCode | kReturnCode_HardError);
			break;
	}
}

void ATCorvusEmulator::DoCommand() {
	if (!mpBlockDevice) {
		InitReply(1, kReturnCode_DriveNotOnline | kReturnCode_HardError);
		return;
	}

	const uint8 cmd = mTransferBuffer[0];

	if (mbPrepMode) {
		switch(cmd) {
			case 0x00:	// reset
				mbPrepMode = false;
				InitReply(1, kReturnCode_Success);
				break;

			case 0x07:	// verify drive
				InitReply(2, kReturnCode_Success);
				// byte 1 is already zeroed -- bad sector count
				break;

			case 0x32:	// read Corvus firmware
			case 0x33:	// write Corvus firmware
				{
					const uint32 head = (mTransferBuffer[1] >> 5) & 7;
					const uint32 sector = mTransferBuffer[1] & 31;

					if (head > 1 || sector > 19) {
						InitReply(1, kReturnCode_IllegalSectorAddress | kReturnCode_HardError);
						return;
					}

					const uint32 lba = head * 20 + sector;

					if (cmd == 0x32) {	// read
						try {
							mpBlockDevice->ReadSectors(mSectorBuffer, lba, 1);

							InitReply(513, kReturnCode_Success);
							memcpy(mTransferBuffer + 1, mSectorBuffer, 512);
						} catch(const MyError&) {
							InitReply(513, kReturnCode_Fault | kReturnCode_RecoverableError);
						}
					} else {			// write
						try {
							mpBlockDevice->WriteSectors(&mTransferBuffer[2], lba, 1);

							InitReply(1, kReturnCode_Success);
						} catch(const MyError&) {
							InitReply(1, kReturnCode_Fault | kReturnCode_RecoverableError);
						}
					}
				}
				break;
		}

		return;
	}

	static const char kSectorCmdToBlockShift[] = { 1, 2, 1, 0 };
	static const uint32 kSectorCmdToSize[] = { 256, 128, 256, 512 };

	switch(cmd) {
		case 0x02:		// read sector, 256 bytes
		case 0x12:		// read sector, 128 bytes
		case 0x22:		// read sector, 256 bytes (alias)
		case 0x32:		// read sector, 512 bytes
			if (mTransferBuffer[1] != 0x01) {
				InitReply(1, kReturnCode_DriveNotOnline | kReturnCode_HardError);
				return;
			}

			{
				uint32 sector = VDReadUnalignedLEU16(mTransferBuffer + 2) + ((mTransferBuffer[1] & 0xF0) << 12);

				const int sectorToBlockShift = kSectorCmdToBlockShift[cmd >> 4];

				if (sector >= (mUserAreaBlockCount << sectorToBlockShift)) {
					InitReply(1, kReturnCode_IllegalSectorAddress | kReturnCode_HardError);
					return;
				}

				const uint32 sectorSize = kSectorCmdToSize[cmd >> 4];
				const uint32 lba = mUserAreaBlockStart + (sector >> sectorToBlockShift);

				try {
					mpBlockDevice->ReadSectors(mSectorBuffer, lba, 1);

					InitReply(sectorSize + 1, kReturnCode_Success);
					memcpy(mTransferBuffer + 1, mSectorBuffer + (sector & ((1 << sectorToBlockShift) - 1)) * sectorSize, sectorSize);
				} catch(const MyError& e) {
					g_ATLCCorvus("Error while reading LBA %u: %s\n", lba, e.c_str());
					InitReply(sectorSize + 1, kReturnCode_Fault | kReturnCode_RecoverableError);
				}
		
				mpIndMgr->SetIDEActivity(false, mUserAreaBlockStart + (lba >> sectorToBlockShift));
			}
			break;

		case 0x13:		// write sector, 128 bytes
		case 0x03:		// write sector, 256 bytes
		case 0x23:		// write sector, 256 bytes (alias)
		case 0x33:		// write sector, 512 bytes
			if (mTransferBuffer[1] != 0x01) {
				InitReply(1, kReturnCode_DriveNotOnline | kReturnCode_HardError);
				return;
			}

			{
				uint32 sector = VDReadUnalignedLEU16(mTransferBuffer + 2) + ((mTransferBuffer[1] & 0xF0) << 12);
				
				const int sectorToBlockShift = kSectorCmdToBlockShift[cmd >> 4];

				if (sector >= (mUserAreaBlockCount << sectorToBlockShift)) {
					InitReply(1, kReturnCode_IllegalSectorAddress | kReturnCode_HardError);
					return;
				}

				const uint32 sectorSize = kSectorCmdToSize[cmd >> 4];
				const uint32 lba = mUserAreaBlockStart + (sector >> sectorToBlockShift);

				try {
					if (sectorSize < 512)
						mpBlockDevice->ReadSectors(mSectorBuffer, lba, 1);

					memcpy(mSectorBuffer + (sector & ((1 << sectorToBlockShift) - 1)) * sectorSize, mTransferBuffer + 4, sectorSize);

					mpBlockDevice->WriteSectors(mSectorBuffer, lba, 1);

					InitReply(1, kReturnCode_Success);
				} catch(const MyError& e) {
					g_ATLCCorvus("Error while writing LBA %u: %s\n", lba, e.c_str());
					InitReply(1, kReturnCode_Fault | kReturnCode_RecoverableError);
				}
			
				mpIndMgr->SetIDEActivity(true, lba);
			}
			break;

		case 0x10:		// get drive parameters
			// validate drive
			if (mTransferBuffer[1] != 0x01) {
				InitReply(1, kReturnCode_DriveNotOnline | kReturnCode_HardError);
				return;
			}

			// fill out return buffer
			InitReply(129, kReturnCode_Success);
			memcpy(&mTransferBuffer[1], "V17.3 CORVUS SYSTEMS 20-NOV-81  ", 32);
			mTransferBuffer[33] = 1;	// rom version
			mTransferBuffer[34] = 20;	// sectors per track
			mTransferBuffer[35] = 2;	// tracks per cylinder
			VDWriteUnalignedLEU16(&mTransferBuffer[36], 306);	// cylinders per drive
			mTransferBuffer[38] = (mUserAreaBlockCount) & 0xFF;
			mTransferBuffer[39] = (mUserAreaBlockCount >> 8) & 0xFF;
			mTransferBuffer[40] = (mUserAreaBlockCount >> 16) & 0xFF;
			mTransferBuffer[57] = 1;	// interleave
			mTransferBuffer[106] = 1;	// physical drive number
			mTransferBuffer[107] = (mPhysicalBlockCount) & 0xFF;		// physical drive capacity
			mTransferBuffer[108] = (mPhysicalBlockCount >> 8) & 0xFF;
			mTransferBuffer[109] = (mPhysicalBlockCount >> 16) & 0xFF;
			break;

		case 0x11:		// park heads / prep mode select
			// validate drive
			if (mTransferBuffer[1] != 0x01) {
				InitReply(1, kReturnCode_DriveNotOnline | kReturnCode_HardError);
				return;
			}

			mbPrepMode = true;

			InitReply(1, kReturnCode_Success);
			break;
	}
}

void ATCorvusEmulator::InitReply(uint32 len, uint8 status) {
	VDASSERT(len > 0);

	g_ATLCCorvus("Sending return code $%02X + %u bytes\n", status, len - 1);

	memset(mTransferBuffer, 0, len);
	mTransferBuffer[0] = status;
	mTransferIndex = 0;
	mTransferLength = len;
	mbReceiveMode = true;
}

///////////////////////////////////////////////////////////////////////////

ATDeviceCorvus::ATDeviceCorvus() {
}

ATDeviceCorvus::~ATDeviceCorvus() {
}

void *ATDeviceCorvus::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDevicePortInput::kTypeID: return static_cast<IATDevicePortInput *>(this);
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceParent::kTypeID: return static_cast<IATDeviceParent *>(&mDeviceParent);
	}

	return ATDevice::AsInterface(iid);
}

void ATDeviceCorvus::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefCorvus;
}

void ATDeviceCorvus::GetSettings(ATPropertySet& pset) {
	if (mPortShift)
		pset.SetBool("altports", true);
	else
		pset.Unset("altports");
}

bool ATDeviceCorvus::SetSettings(const ATPropertySet& pset) {
	const uint8 portShift = pset.GetBool("altports") ? 8 : 0;

	if (mPortShift != portShift) {
		mPortShift = portShift;

		if (mpPortManager) {
			mpPortManager->SetInput(mPortInput, UINT32_C(0xFFFFFFFF));

			ReinitPortOutput();
		}
	}

	return true;
}

void ATDeviceCorvus::Init() {
	mDeviceParent.Init(IATBlockDevice::kTypeID, "harddisk", L"Hard disk bus", "hdbus", this);
	mDeviceParent.SetOnAttach([this] { mCorvusEmu.SetAttachedDevice(mDeviceParent.GetChild<IATBlockDevice>()); });
	mDeviceParent.SetOnDetach([this] { mCorvusEmu.SetAttachedDevice(nullptr); });
}

void ATDeviceCorvus::Shutdown() {
	mDeviceParent.Shutdown();

	if (mpPortManager) {
		mpPortManager->FreeInput(mPortInput);
		mpPortManager->FreeOutput(mPortOutput);

		mpPortManager = nullptr;
	}

	mpIndMgr = nullptr;
	mpScheduler = nullptr;
}

void ATDeviceCorvus::ColdReset() {
	mDataLatch = 0xFF;
}

void ATDeviceCorvus::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
}

void ATDeviceCorvus::InitPortInput(IATDevicePortManager *portMgr) {
	mpPortManager = portMgr;

	mPortInput = mpPortManager->AllocInput();

	ReinitPortOutput();

	mLastPortState = mpPortManager->GetOutputState();
}

void ATDeviceCorvus::InitIndicators(IATDeviceIndicatorManager *indmgr) {
	mpIndMgr = indmgr;

	mCorvusEmu.Init(mpIndMgr);
}

void ATDeviceCorvus::OnPortOutputChanged(uint32 outputState) {
	outputState <<= mPortShift;

	const uint32 lastState = mLastPortState;
	mLastPortState = outputState;

	// Check for port B bit 7 dropping.
	if (~outputState & lastState & 0x8000) {
		// It's a command. The following commands are documented (from
		// COMU.ASM on the Corvus disk), viewed at PORTB:
		//
		// x000xxxx - write lower nibble
		// x001xxxx - write upper nibble and send byte
		// x010xxxx - receive byte and read lower nibble
		// x011xxxx - read upper nibble
		// x100xxxx - read interface status (bit 1 = /receive, bit 0 = /ready)

		if ((outputState ^ mLastPortState) & 0x7000) {
			g_ATLCCorvus("Ambiguous command received: $%02X -> $%02X\n", (lastState >> 8) & 0xFF, (outputState >> 8) & 0xFF);
		}

		// We always use the *last* command in order to punish^H^H^Hcatch people who
		// try dropping the clock while data lines are transitioning.
		switch((lastState >> 12) & 7) {
			case 0:		// write lower nibble
				mDataLatch = (uint8)((mDataLatch & 0xF0) + ((lastState >> 8) & 0x0F));
				mpPortManager->SetInput(mPortInput, UINT32_C(0xFFFFFFFF));
				break;

			case 1:		// write upper nibble and send
				mDataLatch = (uint8)((mDataLatch & 0x0F) + ((lastState >> 4) & 0xF0));
				mpPortManager->SetInput(mPortInput, UINT32_C(0xFFFFFFFF));

				g_ATLCCorvus("Sending byte: $%02X\n", mDataLatch);
				mCorvusEmu.SendByte(mpScheduler->GetTick64(), mDataLatch);
				break;

			case 2:		// receive byte and read lower nibble
				mDataLatch = mCorvusEmu.ReceiveByte(mpScheduler->GetTick64());
				g_ATLCCorvus("Receiving byte: $%02X\n", mDataLatch);
				mpPortManager->SetInput(mPortInput, ~(UINT32_C(0xF00) >> mPortShift) + (((uint32)(mDataLatch & 0x0F) << 8) >> mPortShift));
				break;

			case 3:		// read upper nibble
				mpPortManager->SetInput(mPortInput, ~(UINT32_C(0xF00) >> mPortShift) + (((uint32)(mDataLatch & 0xF0) << 4) >> mPortShift));
				break;

			case 4:		// read status
			case 5:
			case 6:
			case 7:
				{
					const uint32 statusBits = (mCorvusEmu.IsReady() ? 0 : 0x100)
						+ (mCorvusEmu.IsWaitingForReceive() ? 0x200 : 0);

					mpPortManager->SetInput(mPortInput,
						~(UINT32_C(0x300) >> mPortShift) + (statusBits >> mPortShift));
				}
				break;
		}
	}
}

void ATDeviceCorvus::ReinitPortOutput() {
	if (mPortOutput >= 0)
		mpPortManager->FreeOutput(mPortOutput);

	mPortOutput = mpPortManager->AllocOutput(
		[](void *pThis, uint32 outputState) {
			((ATDeviceCorvus *)pThis)->OnPortOutputChanged(outputState);
		},
		this,
		0xFF00 >> mPortShift);
}
