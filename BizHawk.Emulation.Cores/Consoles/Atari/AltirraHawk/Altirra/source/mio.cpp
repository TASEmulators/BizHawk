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
#include <vd2/system/hash.h>
#include <vd2/system/int128.h>
#include <at/atcore/blockdevice.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/deviceserial.h>
#include "mio.h"
#include "debuggerlog.h"
#include "memorymanager.h"
#include "firmwaremanager.h"
#include "irqcontroller.h"
#include "scsidisk.h"

extern ATDebuggerLogChannel g_ATLCParPrint;

void ATCreateDeviceMIOEmulator(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATMIOEmulator> p(new ATMIOEmulator);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefMIO = { "mio", "mio", L"MIO", ATCreateDeviceMIOEmulator, kATDeviceDefFlag_RebootOnPlug };

ATMIOEmulator::ATMIOEmulator()
	: mPBIBANK(0)
	, mRAMPAGE(0)
	, mDataIn(0)
	, mDataOut(0)
	, mStatus1(0)
	, mStatus2(0)
	, mbPrinterIRQEnabled(false)
	, mbACIAIRQActive(false)
	, mpFwMan(nullptr)
	, mpUIRenderer(nullptr)
	, mpIRQController(nullptr)
	, mIRQBit(0)
	, mpScheduler(nullptr)
	, mpEventUpdateSCSIBus(nullptr)
	, mpMemMan(nullptr)
	, mpMemLayerPBI(nullptr)
	, mpMemLayerRAM(nullptr)
	, mpMemLayerFirmware(nullptr)
	, mpPrinterOutput(nullptr)
	, mpSerialDevice(nullptr)
	, mbRTS(false)
	, mbDTR(false)

{
	memset(mFirmware, 0xFF, sizeof mFirmware);

	mACIA.SetInterruptFn([this](bool state) { OnACIAIRQStateChanged(state); });
	mACIA.SetReceiveReadyFn([this]() { OnACIAReceiveReady(); });
	mACIA.SetTransmitFn([this](uint8 data, uint32 baudRate) { OnACIATransmit(data, baudRate); });
	mACIA.SetControlFn([this](bool rts, bool dtr) { OnACIAControlStateChanged(rts, dtr); });

	mSCSIBus.SetBusMonitor(this);
}

ATMIOEmulator::~ATMIOEmulator() {
}

void *ATMIOEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceMemMap::kTypeID: return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceFirmware::kTypeID: return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceIRQSource::kTypeID: return static_cast<IATDeviceIRQSource *>(this);
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceParent::kTypeID: return static_cast<IATDeviceParent *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
		case IATDevicePrinter::kTypeID: return static_cast<IATDevicePrinter *>(this);
	}

	return nullptr;
}

void ATMIOEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefMIO;
}

void ATMIOEmulator::GetSettings(ATPropertySet& settings) {
}

bool ATMIOEmulator::SetSettings(const ATPropertySet& settings) {
	return true;
}

void ATMIOEmulator::Init() {
	mSerialBus.Init(this, 0, IATDeviceSerial::kTypeID, "serial", L"Serial Port", "serport");

	mSerialBus.SetOnAttach(
		[this] {
			if (mpSerialDevice)
				return;

			IATDeviceSerial *serdev = mSerialBus.GetChild<IATDeviceSerial>();
			if (serdev) {
				vdsaferelease <<= mpSerialDevice;
				if (serdev)
					serdev->AddRef();
				mpSerialDevice = serdev;
				mpSerialDevice->SetOnStatusChange([this](const ATDeviceSerialStatus& status) { this->OnControlStateChanged(status); });

				UpdateSerialControlLines();
			}
		}
	);

	mSerialBus.SetOnDetach(
		[this] {
			if (mpSerialDevice) {
				mpSerialDevice->SetOnStatusChange(nullptr);
				vdpoly_cast<IATDevice *>(mpSerialDevice)->SetParent(nullptr, 0);
				vdsaferelease <<= mpSerialDevice;
			}

			mSerialCtlInputs = 0;
		}
	);
}

void ATMIOEmulator::Shutdown() {
	mSerialBus.Shutdown();

	mSCSIBus.Shutdown();

	while(!mSCSIDisks.empty()) {
		const SCSIDiskEntry& ent = mSCSIDisks.back();

		ent.mpSCSIDevice->Release();
		ent.mpDisk->Release();
		if (ent.mpDevice) {
			ent.mpDevice->SetParent(nullptr, 0);
			ent.mpDevice->Release();
		}

		mSCSIDisks.pop_back();
	}

	if (mpIRQController) {
		if (mIRQBit) {
			mpIRQController->FreeIRQ(mIRQBit);
			mIRQBit = 0;
		}

		mpIRQController = nullptr;
	}

	mpPrinterOutput = nullptr;

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventUpdateSCSIBus);
		mpScheduler = nullptr;
	}

	if (mpMemLayerFirmware) {
		mpMemMan->DeleteLayer(mpMemLayerFirmware);
		mpMemLayerFirmware = nullptr;
	}

	if (mpMemLayerRAM) {
		mpMemMan->DeleteLayer(mpMemLayerRAM);
		mpMemLayerRAM = NULL;
	}

	if (mpMemLayerPBI) {
		mpMemMan->DeleteLayer(mpMemLayerPBI);
		mpMemLayerPBI = NULL;
	}

	mACIA.Shutdown();

	mpUIRenderer = nullptr;
}

void ATMIOEmulator::WarmReset() {
	mDataIn = 0xFF;
	mDataOut = 0xFF;
	mSCSIBus.SetControl(0, 0xFF | kATSCSICtrlState_RST, kATSCSICtrlState_All | 0xFF);
	mPrevSCSIState = mSCSIBus.GetBusState();

	// turn off printer IRQ
	mbPrinterIRQEnabled = false;

	mPBIBANK = -2;
	SetPBIBANK(-1);

	mRAMPAGE = 1;
	SetRAMPAGE(0);

	mpMemMan->EnableLayer(mpMemLayerRAM, false);

	mbLastPrinterStrobe = true;

	mACIA.Reset();

	mpIRQController->Negate(mIRQBit, false);

	OnSCSIControlStateChanged(mSCSIBus.GetBusState());
}

void ATMIOEmulator::ColdReset() {
	mStatus1 = 0xBF;		// D6=0 because printer is never busy
	mStatus2 = 0xFF;

	memset(mRAM, 0x00, sizeof mRAM);

	WarmReset();
}

void ATMIOEmulator::InitMemMap(ATMemoryManager *memmap) {
	mpMemMan = memmap;

	ATMemoryHandlerTable handlers={};
	handlers.mpDebugReadHandler = OnDebugRead;
	handlers.mpReadHandler = OnRead;
	handlers.mpWriteHandler = OnWrite;
	handlers.mpThis = this;
	handlers.mbPassAnticReads = true;
	handlers.mbPassReads = true;
	handlers.mbPassWrites = true;

	mpMemLayerPBI = mpMemMan->CreateLayer(kATMemoryPri_PBISelect+1, handlers, 0xD1, 0x01);
	mpMemMan->SetLayerName(mpMemLayerPBI, "MIO I/O");
	mpMemMan->EnableLayer(mpMemLayerPBI, true);

	mpMemLayerRAM = mpMemMan->CreateLayer(kATMemoryPri_PBI, mRAM, 0xD6, 0x01, false);
	mpMemMan->SetLayerName(mpMemLayerRAM, "MIO RAM");

	mpMemLayerFirmware = mpMemMan->CreateLayer(kATMemoryPri_PBI, mFirmware, 0xD8, 0x08, true);
	mpMemMan->SetLayerName(mpMemLayerFirmware, "MIO ROM");
	mpMemMan->EnableLayer(mpMemLayerFirmware, true);
}

bool ATMIOEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
	switch(index) {
	case 0:
		lo = 0xD100;
		hi = 0xD1FF;
		return true;

	case 1:
		lo = 0xD600;
		hi = 0xD6FF;
		return true;

	default:
		return false;
	}
}

void ATMIOEmulator::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMan = fwman;

	ReloadFirmware();
}

bool ATMIOEmulator::ReloadFirmware() {
	vduint128 checksum = VDHash128(mFirmware, sizeof mFirmware);

	memset(mFirmware, 0xFF, sizeof mFirmware);

	uint32 actual = 0;
	mpFwMan->LoadFirmware(mpFwMan->GetCompatibleFirmware(kATFirmwareType_MIO), mFirmware, 0, sizeof mFirmware, nullptr, &actual, nullptr, nullptr, &mbFirmwareUsable);

	return checksum != VDHash128(mFirmware, sizeof mFirmware);
}

ATDeviceFirmwareStatus ATMIOEmulator::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATMIOEmulator::InitIRQSource(ATIRQController *irqc) {
	mpIRQController = irqc;
	mIRQBit = irqc->AllocateIRQ();
}

void ATMIOEmulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;

	mSCSIBus.Init(sch);
	mACIA.Init(sch, slowsch);
}

void ATMIOEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATMIOEmulator::SetPrinterOutput(IATPrinterOutput *out) {
	mpPrinterOutput = out;
}

IATDeviceBus *ATMIOEmulator::GetDeviceBus(uint32 index) {
	switch(index) {
		case 0:
			return &mSerialBus;

		case 1:
			return this;

		default:
			return nullptr;
	}
}

const wchar_t *ATMIOEmulator::GetBusName() const {
	return L"SCSI Bus";
}

const char *ATMIOEmulator::GetBusTag() const {
	return "scsibus";
}

const char *ATMIOEmulator::GetSupportedType(uint32 index) {
	switch(index) {
		case 0: return "harddisk";
		default:
			return nullptr;
	}
}

void ATMIOEmulator::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	for(auto it = mSCSIDisks.begin(), itEnd = mSCSIDisks.end();
		it != itEnd;
		++it)
	{
		const SCSIDiskEntry& ent = *it;

		devs.push_back(vdpoly_cast<IATDevice *>(ent.mpDisk));
	}
}

void ATMIOEmulator::GetChildDevicePrefix(uint32 index, VDStringW& s) {
	if (index < mSCSIDisks.size())
		s.sprintf(L"SCSI ID %u: ", index);
}

void ATMIOEmulator::AddChildDevice(IATDevice *dev) {
	IATBlockDevice *disk = vdpoly_cast<IATBlockDevice *>(dev);
	if (disk) {
		VDASSERT(vdpoly_cast<IATDevice *>(disk));

		if (mSCSIDisks.size() >= 8)
			return;

		vdrefptr<IATSCSIDiskDevice> scsidev;
		ATCreateSCSIDiskDevice(disk, ~scsidev);

		const uint32 id = (uint32)mSCSIDisks.size();

		SCSIDiskEntry entry = { dev, scsidev, disk };
		mSCSIDisks.push_back(entry);
		dev->AddRef();
		dev->SetParent(this, 1);
		scsidev->AddRef();
		disk->AddRef();

		scsidev->SetBlockSize(256);
		scsidev->SetUIRenderer(mpUIRenderer);

		mSCSIBus.AttachDevice(id, scsidev);
	}
}

void ATMIOEmulator::RemoveChildDevice(IATDevice *dev) {
	IATBlockDevice *disk = vdpoly_cast<IATBlockDevice *>(dev);

	if (!disk)
		return;

	for(auto it = mSCSIDisks.begin(), itEnd = mSCSIDisks.end();
		it != itEnd;
		++it)
	{
		const SCSIDiskEntry& ent = *it;

		if (ent.mpDisk == disk) {
			dev->SetParent(nullptr, 0);
			mSCSIBus.DetachDevice(ent.mpSCSIDevice);
			ent.mpDisk->Release();
			ent.mpSCSIDevice->Release();
			ent.mpDevice->Release();

			const uint32 eraseIndex = (uint32)(it - mSCSIDisks.begin());
			mSCSIDisks.erase(it);

			for(uint32 i=eraseIndex; i<7; ++i)
				mSCSIBus.SwapDevices(i, i+1);
		}
	}
}

void ATMIOEmulator::OnSCSIControlStateChanged(uint32 state) {
	// Check for state changes:
	//	- IO change -> flip data bus driver
	//	- REQ deassert -> ACK deassert
	//
	// Note that we must NOT issue control changes from here. It will screw up the
	// SCSI bus!
	if (((mPrevSCSIState ^ state) & kATSCSICtrlState_IO) || (mPrevSCSIState & ~state & kATSCSICtrlState_REQ))
		mpScheduler->SetEvent(1, this, 1, mpEventUpdateSCSIBus);

	mPrevSCSIState = state;

	mDataIn = (uint8)~state;

	mStatus1 |= 0xA7;

	if (state & kATSCSICtrlState_CD)
		mStatus1 -= 0x01;

	if (state & kATSCSICtrlState_MSG)
		mStatus1 -= 0x02;

	if (state & kATSCSICtrlState_IO)
		mStatus1 -= 0x04;

	if (state & kATSCSICtrlState_BSY)
		mStatus1 -= 0x20;

	if (state & kATSCSICtrlState_REQ)
		mStatus1 -= 0x80;
}

void ATMIOEmulator::OnScheduledEvent(uint32 id) {
	mpEventUpdateSCSIBus = nullptr;

	// IO controls data bus driver
	uint32 newState = 0;

	if (mPrevSCSIState & kATSCSICtrlState_IO)
		newState |= 0xff;
	else
		newState |= ~mDataOut & 0xff;

	// sense deassert REQ -> auto deassert ACK
	uint32 newMask = 0xff;
	if (!(mPrevSCSIState & kATSCSICtrlState_REQ))
		newMask |= kATSCSICtrlState_ACK;

	if (newMask)
		mSCSIBus.SetControl(0, newState, newMask);
}

void ATMIOEmulator::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	mStatus2 &= ~7;

	if (status.mbDataSetReady)
		mStatus2 += 0x02;

	if (status.mbClearToSend)
		mStatus2 += 0x04;

	if (status.mbCarrierDetect)
		mStatus2 += 0x01;
}

sint32 ATMIOEmulator::OnDebugRead(void *thisptr0, uint32 addr) {
	const auto thisptr = (ATMIOEmulator *)thisptr0;

	if ((addr - 0xD1C0) < 0x20)
		return thisptr->mACIA.DebugReadByte(addr);

	if ((addr - 0xD1E0) < 0x20) {
		switch(addr & 3) {
			case 0:		// reset SCSI bus
				break;

			case 1:
				return thisptr->mDataIn;

			case 2:
				return thisptr->mStatus1;

			case 3:
				return thisptr->mStatus2;
		}
	}

	return -1;
}

sint32 ATMIOEmulator::OnRead(void *thisptr0, uint32 addr) {
	const auto thisptr = (ATMIOEmulator *)thisptr0;

	if ((addr - 0xD1C0) < 0x20)
		return thisptr->mACIA.ReadByte(addr);

	if ((addr - 0xD1E0) < 0x20) {
		switch(addr & 3) {
			case 0:		// reset SCSI bus
				thisptr->mSCSIBus.SetControl(0, kATSCSICtrlState_RST, kATSCSICtrlState_RST);
				break;

			case 1: {
				uint8 value = thisptr->mDataIn;

				thisptr->mSCSIBus.SetControl(0, kATSCSICtrlState_ACK, kATSCSICtrlState_ACK);

				return value;
			}

			case 2:
				// clear RST
				thisptr->mSCSIBus.SetControl(0, 0, kATSCSICtrlState_RST);
				return thisptr->mStatus1;

			case 3:
				return thisptr->mStatus2;
		}
	}

	return -1;
}

bool ATMIOEmulator::OnWrite(void *thisptr0, uint32 addr, uint8 value) {
	const auto thisptr = (ATMIOEmulator *)thisptr0;

	if ((addr - 0xD1C0) < 0x20) {
		thisptr->mACIA.WriteByte(addr, value);
		return false;
	}

	if ((addr - 0xD1E0) < 0x20) {
		switch(addr & 3) {
			case 0:		// RAM page A8-A15
				thisptr->SetRAMPAGE((thisptr->mRAMPAGE & 0xF00) + value);
				break;

			case 1:
				// Writing this register sets ACK.
				// The MIO does not invert data for the SCSI bus, so we need to invert it here.
				thisptr->mDataOut = value;

				// check if REQ is set
				if (thisptr->mStatus1 & 0x80) {
					// nope -- don't assert ACK, only possibly output state
					if (thisptr->mStatus1 & 0x04)
						thisptr->mSCSIBus.SetControl(0, (~value & 0xff), 0xff);
				} else {
					// yes -- assert AC, and check output latch state to see if we are driving data bus
					if (thisptr->mStatus1 & 0x04)
						thisptr->mSCSIBus.SetControl(0, (~value & 0xff) + kATSCSICtrlState_ACK, 0xff + kATSCSICtrlState_ACK);
					else
						thisptr->mSCSIBus.SetControl(0, kATSCSICtrlState_ACK, kATSCSICtrlState_ACK);
				}
				break;

			case 2:
				// D0-D3 controls RAM A16-A19
				thisptr->SetRAMPAGE((thisptr->mRAMPAGE & 0x0FF) + ((uint32)value << 8));

				// D4 controls SCSI SEL
				thisptr->mSCSIBus.SetControl(0, (value & 0x10) ? kATSCSICtrlState_SEL : 0, kATSCSICtrlState_SEL);

				// D5=1 enables RAM access
				thisptr->mpMemMan->EnableLayer(thisptr->mpMemLayerRAM, (value & 0x20) != 0);

				// D7 controls printer BUSY IRQ
				thisptr->mbPrinterIRQEnabled = (value & 0x80) != 0;
				if (thisptr->mbPrinterIRQEnabled) {
					// Currently the printer never goes BUSY, so if the IRQ is enabled, it is
					// active.
					if (!(thisptr->mStatus2 & 0x08)) {
						thisptr->mStatus2 |= 0x18;

						if (!thisptr->mbACIAIRQActive)
							thisptr->mpIRQController->Assert(thisptr->mIRQBit, false);
					}
				} else {
					if (thisptr->mStatus2 & 0x08) {
						thisptr->mStatus2 &= ~0x08;

						if (!thisptr->mbACIAIRQActive) {
							thisptr->mStatus2 &= ~0x10;
							thisptr->mpIRQController->Negate(thisptr->mIRQBit, false);
						}
					}
				}

				// D6 controls printer STROBE
				if (value & 0x40) {
					if (!thisptr->mbLastPrinterStrobe) {
						uint8 c = thisptr->mDataOut;

						g_ATLCParPrint("Sending byte to printer: $%02X\n", c);

						// HACK
						if (c != 0x0A) {
							if (thisptr->mpPrinterOutput) {
								if (c == 0x0D)
									c = 0x0A;

								thisptr->mpPrinterOutput->WriteASCII(&c, 1);
							}
						}
					}
				}

				thisptr->mbLastPrinterStrobe = (value & 0x40) != 0;
				break;

			case 3:
				{
					// This is driven by the following logic:
					// A11 = /D0 * /D2
					// A12 = /D0 * /D1

					static const sint8 kBankMap[] = { -1, 0, 1, 0, 2, 0, 0, 0, 3, 0, 1, 0, 2, 0, 0, 0 };

					thisptr->SetPBIBANK(kBankMap[(value >> 2) & 15]);
				}
				break;
		}

		return true;
	}

	return false;
}

void ATMIOEmulator::OnACIAControlStateChanged(bool rts, bool dtr) {
	if (mbRTS != rts || mbDTR != dtr) {
		mbRTS = rts;
		mbDTR = dtr;

		UpdateSerialControlLines();
	}
}

void ATMIOEmulator::OnACIAIRQStateChanged(bool active) {
	if (mbACIAIRQActive == active)
		return;

	mbACIAIRQActive = active;

	if (active) {
		if (!(mStatus2 & 0x08)) {
			mStatus2 |= 0x10;
			mpIRQController->Assert(mIRQBit, false);
		}

	} else {
		if (!(mStatus2 & 0x08)) {
			mStatus2 &= ~0x10;
			mpIRQController->Negate(mIRQBit, false);
		}
	}
}

void ATMIOEmulator::OnACIAReceiveReady() {
	if (mpSerialDevice) {
		uint32 baudRate;
		uint8 c;
		if (mpSerialDevice->Read(baudRate, c))
			mACIA.ReceiveByte(c, baudRate);
	}
}

void ATMIOEmulator::OnACIATransmit(uint8 data, uint32 baudRate) {
	if (mpSerialDevice)
		mpSerialDevice->Write(baudRate, data);
}

void ATMIOEmulator::UpdateSerialControlLines() {
	if (mpSerialDevice) {
		ATDeviceSerialTerminalState state;
		state.mbDataTerminalReady = mbDTR;
		state.mbRequestToSend = mbRTS;

		mpSerialDevice->SetTerminalState(state);
	}
}

void ATMIOEmulator::SetPBIBANK(sint8 value) {
	if (mPBIBANK == value)
		return;

	mPBIBANK = value;

	if (value >= 0) {
		uint32 offset = value << 11;

		mpMemMan->SetLayerMemory(mpMemLayerFirmware, mFirmware + offset);
		mpMemMan->EnableLayer(mpMemLayerFirmware, true);
	} else {
		mpMemMan->EnableLayer(mpMemLayerFirmware, false);
	}
}

void ATMIOEmulator::SetRAMPAGE(uint32 page) {
	page &= 0xFFF;	// 4K pages (1MB)

	if (mRAMPAGE == page)
		return;

	mRAMPAGE = page;

	mpMemMan->SetLayerMemory(mpMemLayerRAM, mRAM + ((uint32)page << 8));
}
