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
#include <vd2/system/bitmath.h>
#include <vd2/system/math.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/logging.h>
#include <at/atemulation/scsi.h>

ATLogChannel g_ATLCSCSICmd(true, false, "SCSICMD", "SCSI commands");
ATLogChannel g_ATLCSCSIBus(false, false, "SCSIBUS", "SCSI bus state");

ATSCSIBusEmulator::ATSCSIBusEmulator() {
}

ATSCSIBusEmulator::~ATSCSIBusEmulator() {
	Shutdown();
}

void ATSCSIBusEmulator::Init(ATScheduler *sch) {
	mpScheduler = sch;
}

void ATSCSIBusEmulator::Shutdown() {
	CommandAbort();

	for(uint32 i=0; i<vdcountof(mpDevices); ++i) {
		if (mpDevices[i]) {
			mpDevices[i]->Detach();
			mpDevices[i]->Release();
			mpDevices[i] = nullptr;
		}
	}
}

void ATSCSIBusEmulator::SetControl(uint32 idx, uint32 state, uint32 mask) {
	VDASSERT(idx < vdcountof(mEndpointState));

	state ^= 0xFF;
	state ^= (state ^ mEndpointState[idx]) & ~mask;

	if (mEndpointState[idx] == state)
		return;

	mEndpointState[idx] = state;

	UpdateBusState();
}

void ATSCSIBusEmulator::AttachDevice(uint32 id, IATSCSIDevice *dev) {
	VDASSERT(id < vdcountof(mpDevices));

	if (mpDevices[id]) {
		mpDevices[id]->Detach();
		mpDevices[id]->Release();
	}

	mpDevices[id] = dev;
	dev->AddRef();
	dev->Attach(this);
}

void ATSCSIBusEmulator::DetachDevice(IATSCSIDevice *dev) {
	for(size_t i=0; i<vdcountof(mpDevices); ++i) {
		if (mpDevices[i] == dev) {
			if (mpTargetDevice == dev)
				CommandAbort();

			mpDevices[i] = nullptr;
			dev->Detach();
			dev->Release();
			break;
		}
	}
}

void ATSCSIBusEmulator::SwapDevices(uint32 id1, uint32 id2) {
	VDASSERT(id1 < vdcountof(mpDevices));
	VDASSERT(id2 < vdcountof(mpDevices));

	std::swap(mpDevices[id1], mpDevices[id2]);
}

void ATSCSIBusEmulator::CommandAbort() {
	if (mpTargetDevice) {
		mpTargetDevice->AbortCommand();
		mpTargetDevice = nullptr;
	}

	CommandEnd();
}

void ATSCSIBusEmulator::CommandEnd() {
	SetBusPhase(kBusPhase_BusFree);
	mpTransferBuffer = nullptr;
	mTransferIndex = 0;
	mTransferLength = 0;
	mbTransferInActive = false;
	mbTransferOutActive = false;
	mbCommandActive = false;

	mpScheduler->UnsetEvent(mpEventCommandDelay);
}

void ATSCSIBusEmulator::CommandDelay(float us) {
	if (us <= 0)
		return;

	const int32_t delay = VDRoundToInt32(mpScheduler->GetRate().asDouble() * us / 1000000.0);
	if (delay > 0)
		mpScheduler->SetEvent(delay, this, 1, mpEventCommandDelay);
}

void ATSCSIBusEmulator::CommandSendData(SendMode mode, const void *data, uint32 length) {
	VDASSERT(!mbTransferInActive && !mbTransferOutActive);

	mpTransferBuffer = (const uint8 *)data;
	mTransferIndex = 0;
	mTransferLength = length;
	mbTransferInActive = true;

	switch(mode) {
		case kSendMode_DataIn:
			SetBusPhase(kBusPhase_DataIn);
			break;

		case kSendMode_Status:
			SetBusPhase(kBusPhase_Status);
			break;

		case kSendMode_MessageIn:
			SetBusPhase(kBusPhase_MessageIn);
			break;
	}
}

void ATSCSIBusEmulator::CommandReceiveData(ReceiveMode mode, void *buf, uint32 length) {
	VDASSERT(!mbTransferInActive && !mbTransferOutActive);

	mpTransferBuffer = (const uint8 *)buf;
	mTransferIndex = 0;
	mTransferLength = length;
	mbTransferOutActive = true;

	switch(mode) {
		case kReceiveMode_DataOut:
			SetBusPhase(kBusPhase_DataOut);
			break;

		case kReceiveMode_MessageOut:
			SetBusPhase(kBusPhase_MessageOut);
			break;
	}
}

void ATSCSIBusEmulator::OnScheduledEvent(uint32 id) {
	mpEventCommandDelay = nullptr;

	AdvanceCommand();
}

void ATSCSIBusEmulator::UpdateBusState() {
	uint32 busState = 0;
	for(uint32 i=0; i<vdcountof(mEndpointState); ++i)
		busState |= mEndpointState[i];

	busState ^= 0xFF;

	if (mBusState == busState)
		return;

	mBusState = busState;

	g_ATLCSCSIBus("New bus state: data=%02X, %cRST %cBSY %cSEL %cIO %cCD %cMSG %cACK %cREQ\n"
		, (uint8)busState
		, busState & kATSCSICtrlState_RST ? '+' : '-'
		, busState & kATSCSICtrlState_BSY ? '+' : '-'
		, busState & kATSCSICtrlState_SEL ? '+' : '-'
		, busState & kATSCSICtrlState_IO ? '+' : '-'
		, busState & kATSCSICtrlState_CD ? '+' : '-'
		, busState & kATSCSICtrlState_MSG ? '+' : '-'
		, busState & kATSCSICtrlState_ACK ? '+' : '-'
		, busState & kATSCSICtrlState_REQ ? '+' : '-'
		);

	if (mpBusMonitor)
		mpBusMonitor->OnSCSIControlStateChanged(mBusState);

	VDASSERT(busState == mBusState);

	// !RST & !SEL & !BSY -> bus free
	// BusFree & BSY -> arbitration
	// (BusFree | Arbitration) & SEL & !BSY & !IO -> selection
	// Selection & BSY -> Command
	// Command -> CD, !IO, !MSG
	// DataIn -> !CD, IO, !MSG
	// DataOut -> !CD, !IO, !MSG
	// Status -> CD, IO, !MSG
	// MessageIn -> CD, IO, MSG
	// MessageOut -> CD, !IO, MSG
	if (busState & kATSCSICtrlState_RST) {
		SetBusPhase(kBusPhase_BusFree);
		return;
	}

	const uint8 busData = (uint8)mBusState;

	if (!(busState & (kATSCSICtrlState_RST | kATSCSICtrlState_SEL | kATSCSICtrlState_BSY))) {
		CommandAbort();
		SetBusPhase(kBusPhase_BusFree);
	} else {
		switch(mBusPhase) {
			case kBusPhase_BusFree:
				if (busState & kATSCSICtrlState_SEL)
					SetBusPhase(kBusPhase_Selection);
				break;

			case kBusPhase_Selection:
				if (!(busState & kATSCSICtrlState_SEL)) {
					if (busData && !(busData & 0x7F & (busData - 1))) {
						mpTargetDevice = mpDevices[VDFindLowestSetBitFast(busData)];
					}

					if (mpTargetDevice)
						SetBusPhase(kBusPhase_Command);
					else
						SetBusPhase(kBusPhase_BusFree);
				}
				break;

			case kBusPhase_Command:
				if (mEndpointState[1] & kATSCSICtrlState_REQ) {
					if (busState & kATSCSICtrlState_ACK) {
						g_ATLCSCSIBus("Received command byte: %02X\n", (uint8)mBusState);
						SetControl(1, 0, kATSCSICtrlState_REQ);

						mCommandBuffer[mTransferIndex++] = (uint8)mBusState;
					}
				} else {
					if (!(busState & kATSCSICtrlState_ACK)) {
						if (mTransferIndex >= mTransferLength) {
							if (mTransferLength == 1) {
								switch((uint8)mBusState & 0xE0) {
									case 0x00:	// Group 0 (6 byte)
										mTransferLength = 6;
										break;
									case 0x20:	// Group 1 (10 byte)
										mTransferLength = 10;
										break;
									case 0xA0:	// Group 5 (12 byte)
										mTransferLength = 12;
										break;
								}
							} else {
								switch(mTransferLength) {
									case 6:
										g_ATLCSCSICmd("Group 0 command: %02X %02X %02X %02X %02X %02X\n"
											, mCommandBuffer[0]
											, mCommandBuffer[1]
											, mCommandBuffer[2]
											, mCommandBuffer[3]
											, mCommandBuffer[4]
											, mCommandBuffer[5]
											);
										break;
									case 10:
										g_ATLCSCSICmd("Group 1 command: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
											, mCommandBuffer[0]
											, mCommandBuffer[1]
											, mCommandBuffer[2]
											, mCommandBuffer[3]
											, mCommandBuffer[4]
											, mCommandBuffer[5]
											, mCommandBuffer[6]
											, mCommandBuffer[7]
											, mCommandBuffer[8]
											, mCommandBuffer[9]
											);
										break;
									case 12:
										g_ATLCSCSICmd("Group 5 command: %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X %02X\n"
											, mCommandBuffer[0]
											, mCommandBuffer[1]
											, mCommandBuffer[2]
											, mCommandBuffer[3]
											, mCommandBuffer[4]
											, mCommandBuffer[5]
											, mCommandBuffer[6]
											, mCommandBuffer[7]
											, mCommandBuffer[8]
											, mCommandBuffer[9]
											, mCommandBuffer[10]
											, mCommandBuffer[11]
											);
										break;
									default:
										g_ATLCSCSICmd("Unknown command: %02X\n");
										break;
								}

								mpTargetDevice->BeginCommand(mCommandBuffer, mTransferLength);
								mbCommandActive = true;
							}
						}

						// must retest this afterward as we may request a transfer extension
						if (mTransferIndex < mTransferLength)
							SetControl(1, kATSCSICtrlState_REQ, kATSCSICtrlState_REQ);
					}
				}
				break;
		}
	}

	if (mbTransferInActive) {
		// REQ ACK  Action
		//  -   -   +REQ with data if still bytes to read, otherwise advance command
		//  +   -   Wait for +ACK
		//  +   +   -REQ and remove data
		//  -   +   Wait for -ACK

		switch(busState & (kATSCSICtrlState_REQ | kATSCSICtrlState_ACK)) {
			case 0:
				if (mTransferIndex < mTransferLength) {
					g_ATLCSCSIBus("Receiving byte from target: [%u/%u] = %02X\n", mTransferIndex, mTransferLength, mpTransferBuffer[mTransferIndex]);
					SetControl(1, mpTransferBuffer[mTransferIndex++] | kATSCSICtrlState_REQ, kATSCSICtrlState_REQ | 0xFF);
				} else
					mbTransferInActive = false;
				break;

			case kATSCSICtrlState_REQ:
				break;

			case kATSCSICtrlState_REQ | kATSCSICtrlState_ACK:
				SetControl(1, 0xFF, kATSCSICtrlState_REQ | 0xFF);
				break;

			case kATSCSICtrlState_ACK:
				break;
		}
	}

	if (mbTransferOutActive) {
		// REQ ACK  Action
		//  -   -   +REQ still bytes to transfer, otherwise advance command
		//  +   -   Wait for +ACK
		//  +   +   -REQ and read data
		//  -   +   Wait for -ACK
		switch(busState & (kATSCSICtrlState_REQ | kATSCSICtrlState_ACK)) {
			case 0:
				if (mTransferIndex < mTransferLength)
					SetControl(1, kATSCSICtrlState_REQ, kATSCSICtrlState_REQ);
				else
					mbTransferOutActive = false;
				break;

			case kATSCSICtrlState_REQ:
				break;

			case kATSCSICtrlState_REQ | kATSCSICtrlState_ACK:
				// We don't normally need to check this, but we need to do so
				// in case the initiator decides to screw with REQ.
				if (mTransferIndex < mTransferLength) {
					g_ATLCSCSIBus("Sent byte from initiator: [%u/%u] = %02X\n", mTransferIndex, mTransferLength, busData);
					const_cast<uint8 *>(mpTransferBuffer)[mTransferIndex++] = busData;
				}
				SetControl(1, 0, kATSCSICtrlState_REQ);
				break;

			case kATSCSICtrlState_ACK:
				break;
		}
	}

	AdvanceCommand();
}

void ATSCSIBusEmulator::SetBusPhase(BusPhase phase) {
	if (mBusPhase == phase)
		return;

	mBusPhase = phase;

	switch(phase) {
		case kBusPhase_BusFree:
			g_ATLCSCSIBus <<= "Entering state: BUSFREE\n";
			mpTargetDevice = nullptr;
			SetControl(1, 0xFF, kATSCSICtrlState_All | 0xFF);
			break;

		case kBusPhase_Selection:
			g_ATLCSCSIBus <<= "Entering state: SELECTION\n";
			SetControl(1, kATSCSICtrlState_BSY);
			break;

		case kBusPhase_Command:
			g_ATLCSCSIBus <<= "Entering state: COMMAND\n";
			mTransferIndex = 0;
			mTransferLength = 1;
			SetControl(1, kATSCSICtrlState_BSY | kATSCSICtrlState_CD | kATSCSICtrlState_REQ);
			break;

		case kBusPhase_DataIn:
			mTransferIndex = 1;
			g_ATLCSCSIBus <<= "Entering state: DATAIN\n";
			SetControl(1, mpTransferBuffer[0] | kATSCSICtrlState_BSY | kATSCSICtrlState_IO | kATSCSICtrlState_REQ, kATSCSICtrlState_All | 0xFF);
			break;

		case kBusPhase_DataOut:
			mTransferIndex = 0;
			g_ATLCSCSIBus <<= "Entering state: DATAOUT\n";
			SetControl(1, kATSCSICtrlState_BSY);
			break;

		case kBusPhase_Status:
			g_ATLCSCSIBus <<= "Entering state: STATUS\n";
			mTransferIndex = 1;
			SetControl(1, mpTransferBuffer[0] | kATSCSICtrlState_BSY | kATSCSICtrlState_CD | kATSCSICtrlState_IO | kATSCSICtrlState_REQ, kATSCSICtrlState_All | 0xFF);
			break;

		case kBusPhase_MessageIn:
			g_ATLCSCSIBus <<= "Entering state: MESSAGEIN\n";
			SetControl(1, kATSCSICtrlState_BSY | kATSCSICtrlState_CD | kATSCSICtrlState_IO | kATSCSICtrlState_MSG | kATSCSICtrlState_REQ);
			break;

		case kBusPhase_MessageOut:
			g_ATLCSCSIBus <<= "Entering state: MESSAGEOUT\n";
			SetControl(1, kATSCSICtrlState_BSY | kATSCSICtrlState_CD | kATSCSICtrlState_MSG | kATSCSICtrlState_REQ);
			break;
	}
}

void ATSCSIBusEmulator::AdvanceCommand() {
	while(mbCommandActive && mpTargetDevice) {
		if (mbTransferInActive || mbTransferOutActive)
			return;

		if (mpEventCommandDelay)
			return;

		mpTargetDevice->AdvanceCommand();
	}
}
