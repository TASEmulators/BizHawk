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
#include <at/atcore/deviceprinter.h>
#include <at/atcore/deviceserial.h>
#include "blackbox.h"
#include "debuggerlog.h"
#include "memorymanager.h"
#include "firmwaremanager.h"
#include "irqcontroller.h"
#include "scsidisk.h"

ATDebuggerLogChannel g_ATLCParPrint(false, false, "PARPRINT", "Parallel printer I/O");

namespace {
	// Our PIA emulation is wired the same as Atari's PIA chip is labeled,
	// but the 8-bit actually has A0/A1 wired backwards to RS0/RS1 on the PIA.
	// The BlackBox doesn't and therefore we need to swap address lines here.
	const uint8 kPIAAddressSwap[4]={
		0,2,1,3
	};
};

ATBlackBoxEmulator::ATBlackBoxEmulator()
	: mPBIBANK(0)
	, mRAMPAGE(0)
	, mActiveIRQs(0)
	, mDipSwitches(0x0F)		// sw1-4 enabled with factory settings
	, mbHiRAMEnabled(false)
	, mRAMPAGEMask(0x1F)
	, mbFirmware32K(false)
	, mbSCSIBlockSize256(false)
	, mpFwMan(nullptr)
	, mpUIRenderer(nullptr)
	, mpIRQController(nullptr)
	, mIRQBit(0)
	, mpScheduler(nullptr)
	, mpMemMan(nullptr)
	, mpMemLayerPBI(nullptr)
	, mpMemLayerRAM(nullptr)
	, mpMemLayerFirmware(nullptr)
	, mpPrinterOutput(nullptr)
	, mpSerialDevice(nullptr)
	, mbRTS(false)
	, mbDTR(false)
	, mSerialCtlInputs(0)

{
	memset(mFirmware, 0xFF, sizeof mFirmware);

	mpEventsReleaseButton[0] = nullptr;
	mpEventsReleaseButton[1] = nullptr;

	mVIA.SetPortAInput(0xff);
	mVIA.SetPortBInput(0xef);
	mVIA.SetPortOutputFn(OnVIAOutputChanged, this);

	mVIA.SetInterruptFn([this](bool state) { OnVIAIRQStateChanged(state); });

	VDVERIFY(0 == mPIA.AllocInput());
	mPIA.AllocOutput(OnPIAOutputChanged, this, 0xFFFFFFFFUL);

	mACIA.SetInterruptFn([this](bool state) { OnACIAIRQStateChanged(state); });
	mACIA.SetReceiveReadyFn([this]() { OnACIAReceiveReady(); });
	mACIA.SetTransmitFn([this](uint8 data, uint32 baudRate) { OnACIATransmit(data, baudRate); });

	mSCSIBus.SetBusMonitor(this);
}

void ATCreateDeviceBlackBoxEmulator(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATBlackBoxEmulator> p(new ATBlackBoxEmulator);
	p->SetSettings(pset);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefBlackBox = { "blackbox", "blackbox", L"BlackBox", ATCreateDeviceBlackBoxEmulator, kATDeviceDefFlag_RebootOnPlug };

ATBlackBoxEmulator::~ATBlackBoxEmulator() {
}

void *ATBlackBoxEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDeviceMemMap::kTypeID: return static_cast<IATDeviceMemMap *>(this);
		case IATDeviceFirmware::kTypeID: return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceIRQSource::kTypeID: return static_cast<IATDeviceIRQSource *>(this);
		case IATDeviceScheduling::kTypeID: return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceButtons::kTypeID: return static_cast<IATDeviceButtons *>(this);
		case IATDeviceParent::kTypeID: return static_cast<IATDeviceParent *>(this);
		case IATDeviceIndicators::kTypeID: return static_cast<IATDeviceIndicators *>(this);
		case IATDevicePrinter::kTypeID: return static_cast<IATDevicePrinter *>(this);
	}

	return nullptr;
}

void ATBlackBoxEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefBlackBox;
}

void ATBlackBoxEmulator::GetSettings(ATPropertySet& settings) {
	settings.SetUint32("dipsw", mDipSwitches);
	settings.SetUint32("blksize", mbSCSIBlockSize256 ? 256 : 512);
	settings.SetUint32("ramsize", mbHiRAMEnabled ? 64 : mRAMPAGEMask >= 0x7F ? 32 : 8);
}

bool ATBlackBoxEmulator::SetSettings(const ATPropertySet& settings) {
	uint32 v;
	if (settings.TryGetUint32("dipsw", v)) {
		if (mDipSwitches != v) {
			mDipSwitches = v;

			UpdateDipSwitches();
		}
	}

	uint32 blockSize;
	if (settings.TryGetUint32("blksize", blockSize)) {
		if (blockSize == 256 || blockSize == 512) {
			const bool bs256 = (blockSize == 256);

			if (mbSCSIBlockSize256 != bs256) {
				mbSCSIBlockSize256 = bs256;

				for(const SCSIDiskEntry& diskEntry : mSCSIDisks) {
					diskEntry.mpSCSIDevice->SetBlockSize(blockSize);
				}
			}
		}
	}
	
	uint32 ramSize;
	if (settings.TryGetUint32("ramsize", ramSize)) {
		mbHiRAMEnabled = (ramSize >= 64);
		mRAMPAGEMask = (ramSize >= 32) ? 0x7F : 0x1F;
	}

	return true;
}

void ATBlackBoxEmulator::Init() {
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

	UpdateDipSwitches();
}

void ATBlackBoxEmulator::Shutdown() {
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
		mpScheduler->UnsetEvent(mpEventsReleaseButton[1]);
		mpScheduler->UnsetEvent(mpEventsReleaseButton[0]);

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

	mVIA.Shutdown();
	mACIA.Shutdown();

	mpUIRenderer = nullptr;
}

void ATBlackBoxEmulator::WarmReset() {
	mPBIBANK = 0xFF;
	mRAMPAGE = 0;

	SetPBIBANK(0x10);
	SetRAMPAGE(0xFF, true);

	mbLastPrinterStrobe = true;

	mPIA.WarmReset();
	mVIA.Reset();
	mVIA.SetCB1Input(false);

	mACIA.Reset();

	mActiveIRQs = 0x0C;
	mpIRQController->Negate(mIRQBit, false);

	mpScheduler->UnsetEvent(mpEventsReleaseButton[0]);
	mpScheduler->UnsetEvent(mpEventsReleaseButton[1]);
}

void ATBlackBoxEmulator::ColdReset() {
	memset(mRAM, 0x00, sizeof mRAM);

	mPIA.ColdReset();

	WarmReset();
}

void ATBlackBoxEmulator::InitMemMap(ATMemoryManager *memmap) {
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
	mpMemMan->SetLayerName(mpMemLayerPBI, "BlackBox PBI");
	mpMemMan->EnableLayer(mpMemLayerPBI, true);

	mpMemLayerRAM = mpMemMan->CreateLayer(kATMemoryPri_PBI, mRAM, 0xD6, 0x01, false);
	mpMemMan->SetLayerName(mpMemLayerRAM, "BlackBox RAM");
	mpMemMan->EnableLayer(mpMemLayerRAM, true);

	mpMemLayerFirmware = mpMemMan->CreateLayer(kATMemoryPri_PBI, mFirmware, 0xD8, 0x08, true);
	mpMemMan->SetLayerName(mpMemLayerFirmware, "BlackBox ROM");
	mpMemMan->EnableLayer(mpMemLayerFirmware, true);
}

bool ATBlackBoxEmulator::GetMappedRange(uint32 index, uint32& lo, uint32& hi) const {
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

void ATBlackBoxEmulator::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMan = fwman;

	ReloadFirmware();
}

bool ATBlackBoxEmulator::ReloadFirmware() {
	vduint128 checksum = VDHash128(mFirmware, sizeof mFirmware);

	memset(mFirmware, 0xFF, sizeof mFirmware);

	uint32 actual = 0;
	mpFwMan->LoadFirmware(mpFwMan->GetCompatibleFirmware(kATFirmwareType_BlackBox), mFirmware, 0, sizeof mFirmware, nullptr, &actual, nullptr, nullptr, &mbFirmwareUsable);

	// check if we had a 16K ROM
	if (actual <= 0x4000) {
		// There are two bank orderings for 16K image dumps in the wild:
		//
		// 1) v1.34: PBI banks at 1:$0800, 2:$1000, 4:$2000, 8:$0000
		// 2) v1.41: PBI banks at 1:$0000, 2:$0800, 4:$1800, 8:$3800
		//
		// We want #1. To do this, we check for the presence of
		// the necessary ID bytes and JMP instructions at each base, and
		// if it doesn't conform to #1 but does to #2, rotate up the
		// image by 8K.
		uint32 validPBIBanks = 0;

		for(uint32 i = 0x4000; i; i -= 0x800) {
			const uint8 *p = &mFirmware[i - 0x800];

			validPBIBanks += validPBIBanks;

			if (p[3] == 0x80 && p[5] == 0x4C && p[8] == 0x4C && p[11] == 0x91)
				++validPBIBanks;
		}

		if ((validPBIBanks & 0x116) != 0x116 && (validPBIBanks & 0x8B) == 0x8B) {
			// rotate-copy to upper 16K and then copy back down
			memcpy(mFirmware + 0x4800, mFirmware, 0x3800);
			memcpy(mFirmware + 0x4000, mFirmware + 0x3800, 0x0800);
			memcpy(mFirmware, mFirmware + 0x4000, 0x4000);
		} else {
			// clone it up to 32K
			memcpy(mFirmware + 0x4000, mFirmware, 0x4000);
		}
	}

	// check if we only loaded a 32K ROM (<2.00), and if so, clone it
	// in both banks
	bool fw32K = true;

	if (actual <= 0x8000) {
		fw32K = false;
		memcpy(mFirmware + 0x8000, mFirmware, 0x8000);
	}

	if (mbFirmware32K != fw32K) {
		mbFirmware32K = fw32K;

		UpdateDipSwitches();
	}

	return checksum != VDHash128(mFirmware, sizeof mFirmware);
}

ATDeviceFirmwareStatus ATBlackBoxEmulator::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATBlackBoxEmulator::InitIRQSource(ATIRQController *irqc) {
	mpIRQController = irqc;
	mIRQBit = irqc->AllocateIRQ();
}

void ATBlackBoxEmulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;

	mSCSIBus.Init(sch);
	mVIA.Init(sch);
	mACIA.Init(sch, slowsch);
}

void ATBlackBoxEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATBlackBoxEmulator::SetPrinterOutput(IATPrinterOutput *out) {
	mpPrinterOutput = out;
}

uint32 ATBlackBoxEmulator::GetSupportedButtons() const {
	return (1 << kATDeviceButton_BlackBoxDumpScreen)
		| (1 << kATDeviceButton_BlackBoxMenu);
}

bool ATBlackBoxEmulator::IsButtonDepressed(ATDeviceButton idx) const {
	return false;
}

void ATBlackBoxEmulator::ActivateButton(ATDeviceButton idx, bool state) {
	if (!state)
		return;

	if (idx == kATDeviceButton_BlackBoxDumpScreen) {
		mActiveIRQs &= ~0x04;
		mVIA.SetCB1Input(false);
		mpScheduler->SetEvent(170000, this, 1, mpEventsReleaseButton[0]);
	} else if (idx == kATDeviceButton_BlackBoxMenu) {
		mActiveIRQs &= ~0x08;
		mVIA.SetCB1Input(false);
		mpScheduler->SetEvent(170000, this, 2, mpEventsReleaseButton[1]);
	}
}

IATDeviceBus *ATBlackBoxEmulator::GetDeviceBus(uint32 index) {
	switch(index) {
		case 0:
			return &mSerialBus;

		case 1:
			return this;

		default:
			return nullptr;
	}
}

const wchar_t *ATBlackBoxEmulator::GetBusName() const {
	return L"SCSI Bus";
}

const char *ATBlackBoxEmulator::GetBusTag() const {
	return "scsibus";
}

const char *ATBlackBoxEmulator::GetSupportedType(uint32 index) {
	return index ? nullptr : "harddisk";
}

void ATBlackBoxEmulator::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	for(auto it = mSCSIDisks.begin(), itEnd = mSCSIDisks.end();
		it != itEnd;
		++it)
	{
		const SCSIDiskEntry& ent = *it;

		devs.push_back(vdpoly_cast<IATDevice *>(ent.mpDisk));
	}
}

void ATBlackBoxEmulator::GetChildDevicePrefix(uint32 index, VDStringW& s) {
	if (index < mSCSIDisks.size())
		s.sprintf(L"SCSI ID %u: ", index);
}

void ATBlackBoxEmulator::AddChildDevice(IATDevice *dev) {
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

		scsidev->SetBlockSize(mbSCSIBlockSize256 ? 256 : 512);
		scsidev->SetUIRenderer(mpUIRenderer);

		mSCSIBus.AttachDevice(id, scsidev);
	}
}

void ATBlackBoxEmulator::RemoveChildDevice(IATDevice *dev) {
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

void ATBlackBoxEmulator::OnSCSIControlStateChanged(uint32 state) {
	uint8 portb = 0xef;

	if (state & kATSCSICtrlState_IO)
		portb &= ~0x01;

	if (state & kATSCSICtrlState_CD)
		portb &= ~0x02;

	if (state & kATSCSICtrlState_BSY)
		portb &= ~0x40;

	if (state & kATSCSICtrlState_REQ)
		portb &= ~0x80;

	mVIA.SetPortAInput(state & 0xff);
	mVIA.SetPortBInput(portb);
	mVIA.SetCA1Input(!(state & kATSCSICtrlState_REQ));
}

void ATBlackBoxEmulator::OnScheduledEvent(uint32 id) {
	if (id == 1) {
		mpEventsReleaseButton[0] = nullptr;
		mActiveIRQs |= 0x04;
		if ((mActiveIRQs & 0x0c) == 0x0c)
			mVIA.SetCB1Input(true);
	} else if (id == 2) {
		mpEventsReleaseButton[1] = nullptr;
		mActiveIRQs |= 0x08;
		if ((mActiveIRQs & 0x0c) == 0x0c)
			mVIA.SetCB1Input(true);
	}
}

void ATBlackBoxEmulator::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	mSerialCtlInputs = 0;

	if (status.mbDataSetReady)
		mSerialCtlInputs += 0x20;

	if (status.mbClearToSend)
		mSerialCtlInputs += 0x40;

	if (status.mbCarrierDetect)
		mSerialCtlInputs += 0x80;
}

sint32 ATBlackBoxEmulator::OnDebugRead(void *thisptr0, uint32 addr) {
	const auto thisptr = (ATBlackBoxEmulator *)thisptr0;

	if ((addr & 0xe0) == 0x20)
		return thisptr->mACIA.DebugReadByte(addr);

	if ((addr & 0xd0) == 0x50)
		return thisptr->mVIA.DebugReadByte(addr);

	if ((addr - 0xD180) < 0x40)
		return thisptr->mPIA.DebugReadByte(kPIAAddressSwap[addr & 3]);
	
	return OnRead(thisptr0, addr);
}

sint32 ATBlackBoxEmulator::OnRead(void *thisptr0, uint32 addr) {
	const auto thisptr = (ATBlackBoxEmulator *)thisptr0;

	if ((addr & 0xe0) == 0x20)
		return thisptr->mACIA.ReadByte(addr);

	if ((addr & 0xd0) == 0x50)
		return thisptr->mVIA.ReadByte(addr);

	if ((addr - 0xD180) < 0x40)
		return thisptr->mPIA.ReadByte(kPIAAddressSwap[addr & 3]);

	if ((addr - 0xD1C0) < 0x40)
		return thisptr->mActiveIRQs + thisptr->mSerialCtlInputs;

	return -1;
}

bool ATBlackBoxEmulator::OnWrite(void *thisptr0, uint32 addr, uint8 value) {
	const auto thisptr = (ATBlackBoxEmulator *)thisptr0;

	if ((addr & 0xe0) == 0x20) {
		thisptr->mACIA.WriteByte(addr, value);
		return false;
	}

	if ((addr & 0xd0) == 0x50) {
		thisptr->mVIA.WriteByte(addr, value);
		return false;
	}

	if ((addr - 0xD180) < 0x40) {
		thisptr->mPIA.WriteByte(kPIAAddressSwap[addr & 3], value);
		return false;
	}

	if ((addr - 0xD1C0) < 0x40) {	// PBIBANK/PDVS
		thisptr->SetPBIBANK((value & 15) + (thisptr->mPBIBANK & 16));
		return false;
	}

	return false;
}

void ATBlackBoxEmulator::OnPIAOutputChanged(void *thisptr0, uint32 val) {
	const auto thisptr = (ATBlackBoxEmulator *)thisptr0;

	// PORTA -> RAMPAGE
	thisptr->SetRAMPAGE((uint8)val, (val & 0x200) != 0);

	// PORTB bit 2 -> PBIBANK high
	thisptr->SetPBIBANK((thisptr->mPBIBANK & 15) + ((val & 0x400) >> 6));

	bool rts = (val & kATPIAOutput_CA2) != 0;
	bool dtr = (val & kATPIAOutput_CB2) != 0;

	if (thisptr->mbRTS != rts || thisptr->mbDTR != dtr) {
		thisptr->mbRTS = rts;
		thisptr->mbDTR = dtr;

		thisptr->UpdateSerialControlLines();
	}
}

void ATBlackBoxEmulator::OnVIAOutputChanged(void *thisptr0, uint32 val) {
	const auto thisptr = (ATBlackBoxEmulator *)thisptr0;
	uint32 newState = val & 0xff;

	if (!(val & 0x400))
		newState |= kATSCSICtrlState_SEL;

	if (!(val & 0x800))
		newState |= kATSCSICtrlState_RST;

	if (!(val & kATVIAOutputBit_CA2))
		newState |= kATSCSICtrlState_ACK;

	if (!(val & kATVIAOutputBit_CB2)) {
		if (thisptr->mbLastPrinterStrobe) {
			uint8 c = ~val & 0xff;

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

	thisptr->mbLastPrinterStrobe = (val & kATVIAOutputBit_CB2) != 0;

	thisptr->mSCSIBus.SetControl(0, newState, kATSCSICtrlState_SEL | kATSCSICtrlState_RST | kATSCSICtrlState_ACK | 0xFF);
}

void ATBlackBoxEmulator::OnVIAIRQStateChanged(bool active) {
	if (active) {
		if (!(mActiveIRQs & 0x02)) {
			mActiveIRQs |= 0x02;

			if (!(mActiveIRQs & 0x01))
				mpIRQController->Assert(mIRQBit, false);
		}
	} else {
		if (mActiveIRQs & 0x02) {
			mActiveIRQs &= ~0x02;

			if (!(mActiveIRQs & 0x01))
				mpIRQController->Negate(mIRQBit, false);
		}
	}
}

void ATBlackBoxEmulator::OnACIAIRQStateChanged(bool active) {
	if (active) {
		if (!(mActiveIRQs & 0x01)) {
			mActiveIRQs |= 0x01;

			if (!(mActiveIRQs & 0x02))
				mpIRQController->Assert(mIRQBit, false);
		}
	} else {
		if (mActiveIRQs & 0x01) {
			mActiveIRQs &= ~0x01;

			if (!(mActiveIRQs & 0x02))
				mpIRQController->Negate(mIRQBit, false);
		}
	}
}

void ATBlackBoxEmulator::OnACIAReceiveReady() {
	if (mpSerialDevice) {
		uint32 baudRate;
		uint8 c;
		if (mpSerialDevice->Read(baudRate, c))
			mACIA.ReceiveByte(c, baudRate);
	}
}

void ATBlackBoxEmulator::OnACIATransmit(uint8 data, uint32 baudRate) {
	if (mpSerialDevice)
		mpSerialDevice->Write(baudRate, data);
}

void ATBlackBoxEmulator::UpdateSerialControlLines() {
	if (mpSerialDevice) {
		ATDeviceSerialTerminalState state;
		state.mbDataTerminalReady = mbDTR;
		state.mbRequestToSend = mbRTS;

		mpSerialDevice->SetTerminalState(state);
	}
}

void ATBlackBoxEmulator::SetPBIBANK(uint8 value) {
	value &= 0x1F;

	if (mPBIBANK == value)
		return;

	mPBIBANK = value;

	if (value & 0x0F) {
		uint32 offset = (uint32)(value & 15) << 11;

		if (value & 0x10)
			offset += 0x8000;

		mpMemMan->SetLayerMemory(mpMemLayerFirmware, mFirmware + offset);
		mpMemMan->EnableLayer(mpMemLayerFirmware, true);
	} else {
		mpMemMan->EnableLayer(mpMemLayerFirmware, false);
	}
}

void ATBlackBoxEmulator::SetRAMPAGE(uint8 value, bool hiselect) {
	uint8 page = (value & mRAMPAGEMask) + (hiselect && mbHiRAMEnabled ? 0x80 : 0x00);

	if (mRAMPAGE == page)
		return;

	mRAMPAGE = page;

	mpMemMan->SetLayerMemory(mpMemLayerRAM, mRAM + ((uint32)page << 8));
}

void ATBlackBoxEmulator::UpdateDipSwitches() {
	// Switches 2-7 (numbered 1-8) are mapped to PIA port B bits 2-7.
	// Note that the switches pull lines down to ground, so they are inverted.
	// Switch #2 is repurposed as a bank select bit for 32K firmware.
	uint8 v = mDipSwitches;

	if (mbFirmware32K)
		v &= ~0x02;

	mPIA.SetInput(0, 0x03FF + ((~v << 9) & 0xFC00));

	// redo PBI bank in case switch 2 is or was held down
	if (mpMemLayerFirmware) {
		uint8 pbibank = mPBIBANK;
		mPBIBANK = 0xFF;
		SetPBIBANK(pbibank);
	}
}
