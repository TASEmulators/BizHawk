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
#include <vd2/system/strutil.h>
#include <at/atcore/cio.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicesioimpl.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include "pokey.h"
#include "pia.h"
#include "cpu.h"
#include "cpumemory.h"
#include "uirender.h"
#include "rs232.h"
#include "ksyms.h"
#include "modem.h"
#include "debuggerlog.h"

extern ATDebuggerLogChannel g_ATLCModemData;

namespace {
	static const uint8 kParityTable[16]={
		0x00, 0x80, 0x80, 0x00,
		0x80, 0x00, 0x00, 0x80,
		0x80, 0x00, 0x00, 0x80,
		0x00, 0x80, 0x80, 0x00,
	};
}

class ATRS232ChannelSX212 final : public IATSchedulerCallback {
public:
	ATRS232ChannelSX212();

	void Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr, IATDeviceRawSIO *siodev, IATAudioMixer *mixer);
	void Shutdown();

	void ColdReset();

	uint8 Open(uint8 aux1, uint8 aux2);
	void Close();

	uint32 GetOutputLevel() const { return mOutputLevel; }

	void GetSettings(ATPropertySet& pset);
	void SetSettings(const ATPropertySet& pset);

	sint32 HangUp(uint8 aux1);
	void SetTranslation(uint8 aux1, uint8 aux2);
	void SetSpeed(bool hispeed);
	void SetConcurrentMode();

	void GetStatus(uint8 status[4]);
	sint32 GetByte(uint8& c);
	sint32 PutByte(uint8 c);

	void ReceiveByte(uint32 baud, uint8 c);

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	void OnControlStateChanged(const ATDeviceSerialStatus& status);
	void PollDevice();
	void EnqueueReceivedByte(uint8 c, bool highSpeed);

	ATScheduler	*mpScheduler;
	ATEvent	*mpEvent;
	ATModemEmulator *mpDevice;
	IATDeviceCIOManager *mpCIOMgr;
	IATDeviceSIOManager *mpSIOMgr;
	IATDeviceRawSIO *mpSIODev;

	bool	mbAddLFAfterEOL;
	bool	mbTranslationEnabled;
	bool	mbTranslationHeavy;
	bool	mbLFPending;
	bool	mbHandlerMode;
	bool	mbConcurrentMode;
	bool	mbHandlerHighSpeed;
	uint8	mWontTranslateChar;

	uint8	mOpenPerms;		// Mirror of ICAX1 at open time.
	bool	mbHighSpeed;
	bool	mbCarrierDetect;

	// These must match the error flags returned by STATUS.
	enum : uint8 {
		kErrorFlagSX212_FramingError = 0x80,
		kErrorFlagSX212_Overrun = 0x40,
		kErrorFlagSX212_Wraparound = 0x10,

		kStatusFlag_CarrierDetect = 0x08,
		kStatusFlag_CarrierDetectSticky = 0x04,
		kStatusFlag_HighSpeed = 0x02,
		kStatusFlag_DataLine = 0x01
	};

	uint8	mErrorFlags;
	uint8	mStatusFlags;

	// These must match bits 0-1 of AUX1 as passed to XIO 38.
	enum ParityMode {
		kParityMode_None	= 0x00,
		kParityMode_Odd		= 0x01,
		kParityMode_Even	= 0x02,
		kParityMode_Mark	= 0x03
	};

	ParityMode	mOutputParityMode;

	int		mInputReadOffset;
	int		mInputWriteOffset;
	int		mInputLevel;
	int		mOutputReadOffset;
	int		mOutputWriteOffset;
	int		mOutputLevel;

	bool	mbDisableThrottling;

	// This buffer is 256 bytes internally for T:.
	uint16	mInputBuffer[256];
	uint16	mOutputBuffer[32];
};

ATRS232ChannelSX212::ATRS232ChannelSX212()
	: mpScheduler(NULL)
	, mpEvent(NULL)
	, mpDevice(nullptr)
	, mpCIOMgr(nullptr)
	, mpSIOMgr(NULL)
	, mpSIODev(NULL)
	, mbAddLFAfterEOL(false)
	, mbTranslationEnabled(true)
	, mbTranslationHeavy(false)
	, mbHandlerMode(false)
	, mbConcurrentMode(false)
	, mbHandlerHighSpeed(false)
	, mWontTranslateChar(0)
	, mOpenPerms(0)
	, mbHighSpeed(false)
	, mbCarrierDetect(false)
	, mErrorFlags(0)
	, mStatusFlags(0)
	, mOutputParityMode(kParityMode_None)
	, mInputReadOffset(0)
	, mInputWriteOffset(0)
	, mInputLevel(0)
	, mOutputReadOffset(0)
	, mOutputWriteOffset(0)
	, mOutputLevel(0)
	, mbDisableThrottling(false)
{
	mpDevice = new ATModemEmulator;
	mpDevice->AddRef();
	mpDevice->SetSX212Mode();
}

void ATRS232ChannelSX212::Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr, IATDeviceRawSIO *siodev, IATAudioMixer *mixer) {
	mpScheduler = sched;
	mpCIOMgr = ciomgr;
	mpSIOMgr = siomgr;
	mpSIODev = siodev;

	mpDevice->SetOnStatusChange([this](const ATDeviceSerialStatus& status) { this->OnControlStateChanged(status); });
	mpDevice->Init(sched, slowsched, uir, mixer);

	// compute initial control state
	ATDeviceSerialStatus cstate(mpDevice->GetStatus());

	mStatusFlags = 0;
	mErrorFlags = 0;

	mbCarrierDetect = cstate.mbCarrierDetect;
	mbConcurrentMode = false;

	if (!mpEvent)
		mpEvent = mpScheduler->AddEvent(59659, this, 1);
}

void ATRS232ChannelSX212::Shutdown() {
	if (mpDevice) {
		mpDevice->Release();
		mpDevice = NULL;
	}

	if (mpScheduler) {
		if (mpEvent) {
			mpScheduler->RemoveEvent(mpEvent);
			mpEvent = NULL;
		}

		mpScheduler = NULL;
	}

	mpCIOMgr = nullptr;
	mpSIOMgr = nullptr;
	mpSIODev = nullptr;
}

void ATRS232ChannelSX212::ColdReset() {
	Close();

	if (mpDevice)
		mpDevice->ColdReset();

	// The SX212 hardware powers up in high speed mode.
	mbHighSpeed = true;

	mbTranslationEnabled = true;
	mbTranslationHeavy = false;
	mbAddLFAfterEOL = false;
	mOutputParityMode = kParityMode_None;
	mbLFPending = false;
}

uint8 ATRS232ChannelSX212::Open(uint8 aux1, uint8 aux2) {
	// clear input/output buffer counters 
	mbHandlerMode = true;
	mInputLevel = 0;
	mInputReadOffset = 0;
	mInputWriteOffset = 0;
	mOutputLevel = 0;
	mOutputReadOffset = 0;
	mOutputWriteOffset = 0;
	mErrorFlags = 0;
	return kATCIOStat_Success;
}

void ATRS232ChannelSX212::Close() {
	mbHandlerMode = false;
	mbConcurrentMode = false;
}

void ATRS232ChannelSX212::GetSettings(ATPropertySet& props) {
	if (mbDisableThrottling)
		props.SetBool("unthrottled", true);

	mpDevice->GetSettings(props);
}

void ATRS232ChannelSX212::SetSettings(const ATPropertySet& props) {
	mbDisableThrottling = props.GetBool("unthrottled", false);

	mpDevice->SetSettings(props);
}

sint32 ATRS232ChannelSX212::HangUp(uint8 aux1) {
	// must not be in concurrent mode, per SX212 docs
	if (mbConcurrentMode)
		return 0x99;

	// AUX1 must be exactly 128 per SX212 docs
	if (aux1 == 0x80)
		mpDevice->HangUp();

	return kATCIOStat_Success;
}

void ATRS232ChannelSX212::SetTranslation(uint8 aux1, uint8 aux2) {
	mbAddLFAfterEOL = (aux1 & 0x40) != 0;
	mbTranslationEnabled = !(aux1 & 0x20);
	mbTranslationHeavy = (aux1 & 0x10) != 0;
	mOutputParityMode = (ParityMode)(aux1 & 3);
	mWontTranslateChar = aux2;
}

void ATRS232ChannelSX212::SetSpeed(bool hispeed) {
	mbHandlerHighSpeed = hispeed;
}

void ATRS232ChannelSX212::SetConcurrentMode() {
	mbConcurrentMode = true;
}

void ATRS232ChannelSX212::GetStatus(uint8 status[4]) {
	status[0] = mErrorFlags;
	mErrorFlags = 0;

	status[2] = 0;

	if (mbConcurrentMode) {
		status[1] = mInputLevel;
		status[3] = mOutputLevel;
	} else {
		status[1] = mStatusFlags | 0xF0;
		status[3] = 0;
	}

	// reset sticky CD bit
	mStatusFlags &= ~kStatusFlag_CarrierDetectSticky;
	mStatusFlags |= (mStatusFlags & kStatusFlag_CarrierDetect) >> 1;
}

sint32 ATRS232ChannelSX212::GetByte(uint8& c) {
	if (!mbConcurrentMode)
		return 0x9A;

	if (mbDisableThrottling)
		PollDevice();

	if (!mInputLevel)
		return -1;

	c = (uint8)mInputBuffer[mInputReadOffset];

	if (++mInputReadOffset >= 256)
		mInputReadOffset = 0;

	--mInputLevel;

	if (mbTranslationEnabled) {
		// convert CR to EOL
		if (c == 0x0D)
			c = 0x9B;
		else if (mbTranslationHeavy && (uint8)(c - 0x20) > 0x5C)
			c = mWontTranslateChar;
		else {
			// strip high bit
			c &= 0x7f;
		}
	}

	return kATCIOStat_Success;
}

sint32 ATRS232ChannelSX212::PutByte(uint8 c) {
	if (!mbConcurrentMode)
		return 0x9A;

	if (mbLFPending)
		c = 0x0A;
	else if (mbTranslationEnabled) {
		// convert EOL to CR
		if (c == 0x9B)
			c = 0x0D;

		if (mbTranslationHeavy) {
			// heavy translation -- drop bytes <$20 or >$7C
			if ((uint8)(c - 0x20) > 0x5C && c != 0x0D)
				return kATCIOStat_Success;
		} else {
			// light translation - strip high bit
			c &= 0x7f;
		}
	}

	for(;;) {
		if (mOutputLevel >= vdcountof(mOutputBuffer))
			return -1;
		
		uint16 d = c;

		switch(mOutputParityMode) {
			case kParityMode_None:
			default:
				break;

			case kParityMode_Even:
				d ^= kParityTable[(d & 0x0F) ^ (d >> 4)];
				break;

			case kParityMode_Odd:
				d ^= kParityTable[(d & 0x0F) ^ (d >> 4)];
				d ^= 0x80;
				break;

			case kParityMode_Mark:
				d |= 0x80;
				break;
		}

		if (mbHandlerHighSpeed)
			d |= 0x100;

		mOutputBuffer[mOutputWriteOffset] = d;
		if (++mOutputWriteOffset >= vdcountof(mOutputBuffer))
			mOutputWriteOffset = 0;

		++mOutputLevel;

		if (mbDisableThrottling)
			PollDevice();

		// If we just pushed out a CR byte and add-LF is enabled, we need to
		// loop around and push out another LF byte.
		if (c != 0x0D || !mbTranslationEnabled || !mbAddLFAfterEOL)
			break;

		mbLFPending = true;
		c = 0x0A;
	}

	mbLFPending = false;
	return kATCIOStat_Success;
}

void ATRS232ChannelSX212::ReceiveByte(uint32 baud, uint8 c) {
	g_ATLCModemData("Sending byte to modem: $%02X\n", c);
	mpDevice->Write(baud, c);
}

void ATRS232ChannelSX212::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	if (mbHandlerMode) {
		if (mbCarrierDetect != status.mbCarrierDetect) {
			mbCarrierDetect = status.mbCarrierDetect;

			if (status.mbCarrierDetect)
				mStatusFlags |= kStatusFlag_CarrierDetect | kStatusFlag_CarrierDetectSticky;
			else
				mStatusFlags &= ~(kStatusFlag_CarrierDetect | kStatusFlag_CarrierDetectSticky);

			mStatusFlags ^= kStatusFlag_CarrierDetectSticky;
		}
	} else {
		if (mbCarrierDetect != status.mbCarrierDetect) {
			mbCarrierDetect = status.mbCarrierDetect;

			mpSIOMgr->SetSIOProceed(mpSIODev, status.mbCarrierDetect);
		}

		if (mbHighSpeed != status.mbHighSpeed) {
			mbHighSpeed = status.mbHighSpeed;

			mpSIOMgr->SetSIOInterrupt(mpSIODev, mbHighSpeed);
		}
	}
}

void ATRS232ChannelSX212::OnScheduledEvent(uint32 id) {
	mpEvent = mpScheduler->AddEvent(mbHighSpeed ? 14915 : 59659, this, 1);

	PollDevice();
}

void ATRS232ChannelSX212::PollDevice() {
	if (mOutputLevel) {
		--mOutputLevel;

		if (mpDevice) {
			const uint16 v = mOutputBuffer[mOutputReadOffset];
			const uint8 c = (uint8)v;
			g_ATLCModemData("Sending byte to modem @ %u: $%02X (%c)\n", v & 0x100 ? 1200 : 300, c, c >= 0x20 && c < 0x7F ? (char)c : '.');
			mpDevice->Write(v & 0x100 ? 1200 : 300, c);
		}

		if (++mOutputReadOffset >= vdcountof(mOutputBuffer))
			mOutputReadOffset = 0;
	}

	if (mpDevice) {
		uint8 c;
		uint32 baud;

		if (mInputLevel < 256 && mpDevice->Read(baud, c)) {
			g_ATLCModemData("Receiving byte from modem: $%02X (%c)\n", c, c >= 0x20 && c < 0x7F ? (char)c : '.');

			EnqueueReceivedByte(c, baud > 600);
		}
	}

	if (mInputLevel && !mbHandlerMode) {
		const uint16 c = mInputBuffer[mInputReadOffset];

		if (++mInputReadOffset >= 256)
			mInputReadOffset = 0;

		--mInputLevel;
		mpSIOMgr->SendRawByte((uint8)c, c & 0x100 ? 1491 : 5966);
	}
}

void ATRS232ChannelSX212::EnqueueReceivedByte(uint8 c, bool highSpeed) {
	mInputBuffer[mInputWriteOffset] = (uint16)c + (highSpeed ? 0x100 : 0);

	if (++mInputWriteOffset >= 256)
		mInputWriteOffset = 0;

	++mInputLevel;
}

//////////////////////////////////////////////////////////////////////////

class ATDeviceSX212 final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceIndicators
	, public IATDeviceAudioOutput
	, public IATDeviceCIO
	, public ATDeviceSIO
	, public IATDeviceRawSIO
{
	ATDeviceSX212(const ATDeviceSX212&) = delete;
	ATDeviceSX212& operator=(const ATDeviceSX212&) = delete;
public:
	ATDeviceSX212();
	~ATDeviceSX212();

	void *AsInterface(uint32 id);

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;

public:	// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:	// IATAudioOutput
	void InitAudioOutput(IATAudioMixer *mixer) override;

public:	// IATDeviceCIO
	void InitCIO(IATDeviceCIOManager *mgr) override;
	void GetCIODevices(char *buf, size_t len) const override;
	sint32 OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) override;
	sint32 OnCIOClose(int channel, uint8 deviceNo) override;
	sint32 OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) override;
	sint32 OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) override;
	sint32 OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) override;
	void OnCIOAbortAsync() override;

public:	// IATDeviceSIO
	void InitSIO(IATDeviceSIOManager *mgr) override;

public:	// IATDeviceRawSIO
	void OnCommandStateChanged(bool asserted) override;
	void OnMotorStateChanged(bool asserted) override;
	void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	void OnSendReady() override;

protected:
	ATScheduler *mpScheduler {};
	ATScheduler *mpSlowScheduler {};
	IATDeviceIndicatorManager *mpUIRenderer {};
	IATDeviceCIOManager *mpCIOMgr {};
	IATDeviceSIOManager *mpSIOMgr {};
	IATAudioMixer *mpAudioMixer {};

	ATRS232ChannelSX212 *mpChannel {};

	AT850SIOEmulationLevel mEmulationLevel = kAT850SIOEmulationLevel_None;
	bool mbSIOMotorState = false;
};

void ATCreateDeviceSX212(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDeviceSX212> p(new ATDeviceSX212);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDefSX212 = { "sx212", "sx212", L"SX212 Modem", ATCreateDeviceSX212 };

ATDeviceSX212::ATDeviceSX212() {
	// We have to init this early so it can accept settings.
	mpChannel = new ATRS232ChannelSX212;
}

ATDeviceSX212::~ATDeviceSX212() {
	Shutdown();
}

void *ATDeviceSX212::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceAudioOutput::kTypeID:	return static_cast<IATDeviceAudioOutput *>(this);
		case IATDeviceCIO::kTypeID:			return static_cast<IATDeviceCIO *>(this);
		case IATDeviceSIO::kTypeID:			return static_cast<IATDeviceSIO *>(this);
		case IATDeviceRawSIO::kTypeID:		return static_cast<IATDeviceRawSIO *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATDeviceSX212::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefSX212;
}

void ATDeviceSX212::GetSettings(ATPropertySet& settings) {
	if (mpChannel)
		mpChannel->GetSettings(settings);

	if (mEmulationLevel)
		settings.SetUint32("emulevel", (uint32)mEmulationLevel);
}

bool ATDeviceSX212::SetSettings(const ATPropertySet& settings) {
	if (mpChannel)
		mpChannel->SetSettings(settings);

	uint32 emulevel = settings.GetUint32("emulevel", 0);
	if (emulevel < kAT850SIOEmulationLevelCount) {
		auto newLevel = emulevel > kAT850SIOEmulationLevel_None ? kAT850SIOEmulationLevel_Full : kAT850SIOEmulationLevel_None;
		
		if (mEmulationLevel != newLevel) {
			if (mpCIOMgr) {
				if (newLevel == kAT850SIOEmulationLevel_Full)
					mpCIOMgr->RemoveCIODevice(this);
				else if (mEmulationLevel == kAT850SIOEmulationLevel_Full)
					mpCIOMgr->AddCIODevice(this);
			}

			if (mpSIOMgr) {
				if (newLevel == kAT850SIOEmulationLevel_None)
					mpSIOMgr->RemoveDevice(this);
				else if (mEmulationLevel == kAT850SIOEmulationLevel_None)
					mpSIOMgr->AddDevice(this);
			}

			mEmulationLevel = newLevel;
		}
	}

	return true;
}

void ATDeviceSX212::Init() {
	mpChannel->Init(mpScheduler, mpSlowScheduler, mpUIRenderer, mpCIOMgr, mpSIOMgr, this, mpAudioMixer);
}

void ATDeviceSX212::Shutdown() {
	if (mpCIOMgr) {
		mpCIOMgr->RemoveCIODevice(this);
		mpCIOMgr = nullptr;
	}

	if (mpSIOMgr) {
		mpSIOMgr->RemoveRawDevice(this);
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr = nullptr;
	}

	if (mpChannel) {
		mpChannel->Shutdown();
		delete mpChannel;
		mpChannel = NULL;
	}

	mpScheduler = nullptr;
	mpSlowScheduler = nullptr;

	mpUIRenderer = nullptr;
	mpAudioMixer = nullptr;
}

void ATDeviceSX212::ColdReset() {
	if (mpChannel)
		mpChannel->ColdReset();

	mbSIOMotorState = false;
}

void ATDeviceSX212::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;
}

void ATDeviceSX212::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATDeviceSX212::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

void ATDeviceSX212::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;

	if (mEmulationLevel != kAT850SIOEmulationLevel_Full)
		mpCIOMgr->AddCIODevice(this);
}

void ATDeviceSX212::GetCIODevices(char *buf, size_t len) const {
	vdstrlcpy(buf, "R", len);
}

sint32 ATDeviceSX212::OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) {
	if (deviceNo != 1)
		return kATCIOStat_UnkDevice;

	return mpChannel->Open(aux1, aux2);
}

sint32 ATDeviceSX212::OnCIOClose(int channel, uint8 deviceNo) {
	// wait for output buffer to drain (requires assist)
	if (mpChannel->GetOutputLevel())
		return -1;

	mpChannel->Close();
	return kATCIOStat_Success;
}

sint32 ATDeviceSX212::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	if (mpCIOMgr->IsBreakActive())
		return kATCIOStat_Break;

	while(len--) {
		uint8 c;
		sint32 status = mpChannel->GetByte(c);

		if (status < 0 || status >= 0x80)
			return status;

		++actual;
		*(uint8 *)buf = c;
		buf = (uint8 *)buf + 1;
	}

	return kATCIOStat_Success;
}

sint32 ATDeviceSX212::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
	if (mpCIOMgr->IsBreakActive())
		return kATCIOStat_Break;

	while(len--) {
		sint32 rval = mpChannel->PutByte(*(const uint8 *)buf);
		if (rval != kATCIOStat_Success) {
			if (rval >= 0)
				++actual;

			return rval;
		}

		++actual;
		buf = (const uint8 *)buf + 1;
	}

	return kATCIOStat_Success;
}

sint32 ATDeviceSX212::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	mpChannel->GetStatus(statusbuf);
	return kATCIOStat_Success;
}

sint32 ATDeviceSX212::OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	switch(cmd) {
		case 34:	// XIO 34 (hang up)
			return mpChannel->HangUp(aux[0]);

		case 36:	// XIO 36 (set baud)
			mpChannel->SetSpeed((aux[0] & 0x0f) >= 10);
			return kATCIOStat_Success;

		case 38:	// XIO 38 (set translation)
			mpChannel->SetTranslation(aux[0], aux[1]);
			return kATCIOStat_Success;

		case 40:	// XIO 40 (enter concurrent mode)
			mpChannel->SetConcurrentMode();
			return kATCIOStat_Success;
	}

	return kATCIOStat_NotSupported;
}

void ATDeviceSX212::OnCIOAbortAsync() {
}

void ATDeviceSX212::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;

	if (mEmulationLevel != kAT850SIOEmulationLevel_None)
		mpSIOMgr->AddRawDevice(this);
}

void ATDeviceSX212::OnCommandStateChanged(bool asserted) {
}

void ATDeviceSX212::OnMotorStateChanged(bool asserted) {
	mbSIOMotorState = asserted;
}

void ATDeviceSX212::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	if (mbSIOMotorState) {
		// check for proper 300 baud operation (divisor = 2982, 5% tolerance)
		if (cyclesPerBit > 5666 && cyclesPerBit < 6266)
			mpChannel->ReceiveByte(300, c);
		else if (cyclesPerBit > 1416 && cyclesPerBit < 1567)
			mpChannel->ReceiveByte(1200, c);
	}
}

void ATDeviceSX212::OnSendReady() {
}
