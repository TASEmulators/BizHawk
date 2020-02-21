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
#include <at/atcore/sioutils.h>
#include "pokey.h"
#include "pia.h"
#include "cpu.h"
#include "cpumemory.h"
#include "uirender.h"
#include "rs232.h"
#include "ksyms.h"
#include "modem.h"
#include "debuggerlog.h"
#include "firmwaremanager.h"

extern ATDebuggerLogChannel g_ATLCModemData;

namespace {
	static const uint8 kParityTable[16]={
		0x00, 0x80, 0x80, 0x00,
		0x80, 0x00, 0x00, 0x80,
		0x80, 0x00, 0x00, 0x80,
		0x00, 0x80, 0x80, 0x00,
	};
}

class ATRS232Channel1030 final : public IATSchedulerCallback {
public:
	ATRS232Channel1030();

	void Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr, IATDeviceRawSIO *siodev, IATAudioMixer *mixer);
	void Shutdown();

	void ColdReset();

	uint8 Open(uint8 aux1, uint8 aux2);
	void Close();

	uint32 GetOutputLevel() const { return mOutputLevel; }

	void GetSettings(ATPropertySet& pset);
	void SetSettings(const ATPropertySet& pset);

	void GetStatus(uint8 status[4]);
	bool GetByte(uint8& c);
	sint32 PutByte(uint8 c);

	bool IsSuspended() const { return mbSuspended; }
	void ReceiveByte(uint8 c);
	void ExecuteDeviceCommand(uint8 c);

public:
	void OnScheduledEvent(uint32 id) override;

protected:
	void OnControlStateChanged(const ATDeviceSerialStatus& status);
	void PollDevice();
	void EnqueueReceivedByte(uint8 c);

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
	bool	mbConcurrentMode;
	bool	mbSuspended;
	bool	mbHandlerMode;
	uint8	mCommandState;
	uint8	mWontTranslateChar;

	uint8	mOpenPerms;		// Mirror of ICAX1 at open time.
	uint8	mControlState;

	// These must match the error flags returned by STATUS.
	enum : uint8 {
		kErrorFlag1030_FramingError = 0x80,
		kErrorFlag1030_Overrun = 0x40,
		kErrorFlag1030_Parity = 0x20,
		kErrorFlag1030_Wraparound = 0x10,
		kErrorFlag1030_IllegalCommand = 0x01,

		kStatusFlag_CarrierDetect = 0x80,
		kStatusFlag_Loopback = 0x20,
		kStatusFlag_Answer = 0x10,
		kStatusFlag_AutoAnswer = 0x08,
		kStatusFlag_ToneDial = 0x04,
		kStatusFlag_OffHook = 0x01
	};

	enum : uint16 {
		// Memory locations defined by the T: handler.
		CMCMD = 7,
		INCNT = 0x400,
		OUTCNT = 0x401
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

	ParityMode	mInputParityMode;
	ParityMode	mOutputParityMode;

	ATDeviceSerialTerminalState	mTerminalState;

	int		mInputReadOffset;
	int		mInputWriteOffset;
	int		mInputLevel;
	int		mOutputReadOffset;
	int		mOutputWriteOffset;
	int		mOutputLevel;

	bool	mbDisableThrottling;
	bool	mbDialPending;
	bool	mbDialPendingPulse;
	VDStringA mDialAddress;
	VDStringA mDialService;

	// This buffer is 256 bytes internally for T:.
	uint8	mInputBuffer[256];
	uint8	mOutputBuffer[32];
};

ATRS232Channel1030::ATRS232Channel1030()
	: mpScheduler(NULL)
	, mpEvent(NULL)
	, mpDevice(nullptr)
	, mpCIOMgr(nullptr)
	, mpSIOMgr(NULL)
	, mpSIODev(NULL)
	, mbAddLFAfterEOL(false)
	, mbTranslationEnabled(true)
	, mbTranslationHeavy(false)
	, mbSuspended(true)
	, mbHandlerMode(false)
	, mCommandState(0)
	, mWontTranslateChar(0)
	, mOpenPerms(0)
	, mControlState(0)
	, mErrorFlags(0)
	, mStatusFlags(0)
	, mInputParityMode(kParityMode_None)
	, mOutputParityMode(kParityMode_None)
	, mInputReadOffset(0)
	, mInputWriteOffset(0)
	, mInputLevel(0)
	, mOutputReadOffset(0)
	, mOutputWriteOffset(0)
	, mOutputLevel(0)
	, mbDisableThrottling(false)
	, mbDialPending(false)
{
	mpDevice = new ATModemEmulator;
	mpDevice->AddRef();
	mpDevice->Set1030Mode();
}

void ATRS232Channel1030::Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr, IATDeviceRawSIO *siodev, IATAudioMixer *mixer) {
	mpScheduler = sched;
	mpCIOMgr = ciomgr;
	mpSIOMgr = siomgr;
	mpSIODev = siodev;

	mpDevice->SetOnStatusChange([this](const ATDeviceSerialStatus& status) { this->OnControlStateChanged(status); });
	mpDevice->Init(sched, slowsched, uir, mixer);

	// compute initial control state
	ATDeviceSerialStatus cstate(mpDevice->GetStatus());

	mStatusFlags = 0;

	mControlState = 0;
	
	if (cstate.mbCarrierDetect)
		mControlState += 0x80;
}

void ATRS232Channel1030::Shutdown() {
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

void ATRS232Channel1030::ColdReset() {
	Close();

	if (mpDevice)
		mpDevice->ColdReset();

	mbDialPending = false;
}

uint8 ATRS232Channel1030::Open(uint8 aux1, uint8 aux2) {
	// clear input/output buffer counters 
	mpCIOMgr->WriteByte(INCNT, 0);
	mpCIOMgr->WriteByte(OUTCNT, 0);
	mpCIOMgr->WriteByte(CMCMD, 0);
	mbHandlerMode = true;
	mInputLevel = 0;
	mInputReadOffset = 0;
	mInputWriteOffset = 0;
	mOutputLevel = 0;
	mOutputReadOffset = 0;
	mOutputWriteOffset = 0;
	mErrorFlags = 0;
	mbTranslationEnabled = true;
	mbTranslationHeavy = false;
	mbAddLFAfterEOL = false;
	mInputParityMode = kParityMode_None;
	mOutputParityMode = kParityMode_None;
	mbLFPending = false;
	return kATCIOStat_Success;
}

void ATRS232Channel1030::Close() {
	mbHandlerMode = false;
}

void ATRS232Channel1030::GetSettings(ATPropertySet& props) {
	if (!mDialAddress.empty())
		props.SetString("dialaddr", VDTextAToW(mDialAddress).c_str());

	if (!mDialService.empty())
		props.SetString("dialsvc", VDTextAToW(mDialService).c_str());

	if (mbDisableThrottling)
		props.SetBool("unthrottled", true);

	mpDevice->GetSettings(props);
}

void ATRS232Channel1030::SetSettings(const ATPropertySet& props) {
	mDialAddress = VDTextWToA(props.GetString("dialaddr", L""));
	mDialService = VDTextWToA(props.GetString("dialsvc", L""));

	mbDisableThrottling = props.GetBool("unthrottled", false);

	mpDevice->SetSettings(props);
}

void ATRS232Channel1030::GetStatus(uint8 status[4]) {
	status[0] = mErrorFlags;
	mErrorFlags = 0;

	status[1] = mControlState;

	// undocumented - required by AMODEM 7.5
	status[2] = mInputLevel;
	status[3] = mOutputLevel;
}

bool ATRS232Channel1030::GetByte(uint8& c) {
	if (mbDisableThrottling)
		PollDevice();

	if (!mInputLevel)
		return false;

	c = mInputBuffer[mInputReadOffset];

	if (++mInputReadOffset >= 256)
		mInputReadOffset = 0;

	--mInputLevel;
	mpCIOMgr->WriteByte(INCNT, mInputLevel);

	if (mInputParityMode) {
		uint8 d = c;

		switch(mInputParityMode) {
			case kParityMode_Even:
				if (kParityTable[(d & 0x0F) ^ (d >> 4)] != 0x00)
					mErrorFlags |= kErrorFlag1030_Parity;
				break;

			case kParityMode_Odd:
				if (kParityTable[(d & 0x0F) ^ (d >> 4)] != 0x80)
					mErrorFlags |= kErrorFlag1030_Parity;
				break;
		}

		c &= 0x7F;
	}

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

	return true;
}

sint32 ATRS232Channel1030::PutByte(uint8 c) {
	switch(mCommandState) {
		case 0:		// waiting for ESC or byte
			// check CMCMD
			if (mpCIOMgr->ReadByte(7)) {
				if (c == 0x1B) {
					mCommandState = 1;
					return kATCIOStat_Success;
				}

				return kATCIOStat_InvalidCmd;
			}

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
				if (mOutputLevel >= sizeof mOutputBuffer)
					return -1;
		
				uint8 d = c;

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

				mOutputBuffer[mOutputWriteOffset] = d;
				if (++mOutputWriteOffset >= sizeof mOutputBuffer)
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
			break;

		case 1:		// waiting for command letter
			mCommandState = 0;

			switch(c) {
				case 0x1B:	// ESC
					mCommandState = 1;
					break;

				case 'A':	// Set Translation [p1 p2]
					mCommandState = 2;
					break;

				case 'C':	// Set Parity [p1]
					mCommandState = 4;
					break;

				case 'E':	// End of commands
					// clear CMCMD
					mpCIOMgr->WriteByte(7, 0);
					break;

				case 'F':	// Status
					{
						uint8 statusbuf[4];
						GetStatus(statusbuf);

						mpCIOMgr->WriteMemory(ATKernelSymbols::DVSTAT, statusbuf, 4);
					}
					break;

				case 'N':	// Set Pulse Dialing
					mStatusFlags &= ~kStatusFlag_ToneDial;
					break;

				case 'O':	// Set Tone Dialing
					mStatusFlags |= kStatusFlag_ToneDial;
					break;

				case 'H':	// Send Break Signal
					break;

				case 'I':	// Set Originate Mode
					mStatusFlags &= ~kStatusFlag_Answer;
					break;

				case 'J':	// Set Answer Mode
					mStatusFlags |= kStatusFlag_Answer;
					break;

				case 'K':	// Dial
					if (mStatusFlags & kStatusFlag_ToneDial) {
						ExecuteDeviceCommand('O');
						mCommandState = 5;
					} else {
						ExecuteDeviceCommand('K');
						mCommandState = 6;
					}
					break;

				case 'L':	// Pick up phone
				case 'M':	// Put phone on hook (hang up)
				case 'P':	// Start 30 second timeout
				case 'Q':	// Reset Modem
					break;

				case 'W':	// Set Analog Loopback Test
					mStatusFlags |= kStatusFlag_Loopback;
					ExecuteDeviceCommand(c);
					break;

				case 'X':	// Clear Analog Loopback Test
					mStatusFlags &= ~kStatusFlag_Loopback;
					ExecuteDeviceCommand(c);
					break;

				case 'Y':	// Resume Modem
					ExecuteDeviceCommand(c);
					break;

				case 'Z':	// Suspend Modem
					ExecuteDeviceCommand(c);
					break;

				default:
					mErrorFlags |= kErrorFlag1030_IllegalCommand;
					return kATCIOStat_InvalidCmd;
			}
			break;

		case 2:		// Set Translation, first byte
			mbAddLFAfterEOL = (c & 0x40) != 0;
			mbTranslationEnabled = !(c & 0x20);
			mbTranslationHeavy = (c & 0x10) != 0;

			mCommandState = 3;
			break;

		case 3:		// Set Translation, second byte
			mCommandState = 0;
			mWontTranslateChar = c;
			break;

		case 4:		// Set Parity, first byte
			mInputParityMode = (ParityMode)((c & 0x0c) >> 2);
			mOutputParityMode = (ParityMode)(c & 0x03);

			mCommandState = 0;
			break;

		case 5:		// Tone dial
			c &= 0x0f;

			if (c == 0x0B) {
				mCommandState = 0;
				ExecuteDeviceCommand('P');
			}

			break;

		case 6:		// Pulse dial
			c &= 0x0f;
			ExecuteDeviceCommand(c);

			if (c == 0x0B)
				mCommandState = 0;

			break;
	}

	return kATCIOStat_Success;
}

void ATRS232Channel1030::ReceiveByte(uint8 c) {
	if (!mbSuspended) {
		g_ATLCModemData("Sending byte to modem: $%02X\n", c);
		mpDevice->Write(300, c);
	}
}

void ATRS232Channel1030::ExecuteDeviceCommand(uint8 c) {
	if (mbDialPending && mbDialPendingPulse) {
		if (c < 0x10) {
			if (c == 0x0B) {
				mbDialPending = false;

				if (!mDialAddress.empty() && !mDialService.empty())
					mpDevice->Dial(mDialAddress.c_str(), mDialService.c_str());
			}

			return;
		} else {
			mbDialPending = false;
			mbDialPendingPulse = false;
		}
	}

	switch(c) {
		case 'H':	// Send Break Signal
			// currently ignored
			break;

		case 'I':	// Set Originate Mode
			// currently ignored
			break;

		case 'J':	// Set Answer Mode
			// currently ignored
			break;

		case 'K':	// Pulse dial
			mbDialPending = true;
			mbDialPendingPulse = true;
			break;

		case 'L':	// Pick up phone
			mpDevice->Answer();
			break;

		case 'M':	// Put phone on hook (hang up)
			mpDevice->HangUp();
			mbDialPending = false;
			break;

		case 'O':	// Tone dial
			mbDialPending = true;
			mbDialPendingPulse = false;
			break;

		case 'P':	// Start 30 second timeout
			if (mbDialPending) {
				mbDialPending = false;

				if (!mDialAddress.empty() && !mDialService.empty())
					mpDevice->Dial(mDialAddress.c_str(), mDialService.c_str());
			}
			break;

		case 'Q':	// Reset Modem
			mbDialPending = false;
			break;

		case 'W':	// Set Analog Loopback Test
			break;

		case 'X':	// Clear Analog Loopback Test
			break;

		case 'Y':	// Resume Modem
			mbSuspended = false;
			if (!mpEvent)
				mpEvent = mpScheduler->AddEvent(59659, this, 1);
			break;

		case 'Z':	// Suspend Modem
			mbSuspended = true;
			mpScheduler->UnsetEvent(mpEvent);
			break;
	}
}

void ATRS232Channel1030::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	uint32 oldState = mControlState;

	if (status.mbCarrierDetect)
		mControlState |= kStatusFlag_CarrierDetect;
	else
		mControlState &= ~kStatusFlag_CarrierDetect;

	if (mbHandlerMode) {
		mStatusFlags = (mStatusFlags & ~kStatusFlag_CarrierDetect) | (mControlState & kStatusFlag_CarrierDetect);
	} else {
		if (!mbSuspended && ((oldState ^ mControlState) & kStatusFlag_CarrierDetect)) {
			mpSIOMgr->SetSIOInterrupt(mpSIODev, true);
			mpSIOMgr->SetSIOInterrupt(mpSIODev, false);
		}
	}
}

void ATRS232Channel1030::OnScheduledEvent(uint32 id) {
	mpEvent = mpScheduler->AddEvent(59659, this, 1);

	PollDevice();
}

void ATRS232Channel1030::PollDevice() {
	if (mOutputLevel) {
		--mOutputLevel;

		if (mpDevice) {
			const uint8 c = mOutputBuffer[mOutputReadOffset];
			g_ATLCModemData("Sending byte to modem: $%02X\n", c);
			mpDevice->Write(300, c);
		}

		if (++mOutputReadOffset >= sizeof mOutputBuffer)
			mOutputReadOffset = 0;
	}

	if (mpDevice) {
		uint8 c;
		bool framingError;

		if (mInputLevel < 256 && mpDevice->Read(300, c, framingError)) {
			g_ATLCModemData("Receiving byte from modem: $%02X\n", c);

			if (framingError)
				mErrorFlags |= kErrorFlag1030_FramingError;

			EnqueueReceivedByte(c);
		}
	}

	if (mInputLevel && !mbHandlerMode) {
		const uint8 c = mInputBuffer[mInputReadOffset];

		if (++mInputReadOffset >= 256)
			mInputReadOffset = 0;

		--mInputLevel;
		mpSIOMgr->SendRawByte(c, 5966);
	}
}

void ATRS232Channel1030::EnqueueReceivedByte(uint8 c) {
	mInputBuffer[mInputWriteOffset] = c;

	if (++mInputWriteOffset >= 256)
		mInputWriteOffset = 0;

	++mInputLevel;

	if (mbHandlerMode)
		mpCIOMgr->WriteByte(INCNT, mInputLevel);
}

//////////////////////////////////////////////////////////////////////////

class ATDevice1030Modem final
	: public ATDevice
	, public IATDeviceFirmware
	, public IATDeviceScheduling
	, public IATDeviceIndicators		
	, public IATDeviceAudioOutput		
	, public IATDeviceCIO
	, public ATDeviceSIO
	, public IATDeviceRawSIO
{
	ATDevice1030Modem(const ATDevice1030Modem&) = delete;
	ATDevice1030Modem& operator=(const ATDevice1030Modem&) = delete;
public:
	ATDevice1030Modem();
	~ATDevice1030Modem();

	void *AsInterface(uint32 id) override;

public:
	void GetDeviceInfo(ATDeviceInfo& info) override;
	void GetSettings(ATPropertySet& settings) override;
	bool SetSettings(const ATPropertySet& settings) override;
	void Init() override;
	void Shutdown() override;
	void ColdReset() override;

public:	// IATDeviceFirmware
	void InitFirmware(ATFirmwareManager *fwman) override;
	bool ReloadFirmware() override;
	const wchar_t *GetWritableFirmwareDesc(uint32 idx) const override { return nullptr; }
	bool IsWritableFirmwareDirty(uint32 idx) const override { return false; }
	void SaveWritableFirmware(uint32 idx, IVDStream& stream) override {}
	ATDeviceFirmwareStatus GetFirmwareStatus() const override;

public:	// IATDeviceScheduling
	void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:	// IATDeviceIndicators
	void InitIndicators(IATDeviceIndicatorManager *r) override;

public:	// IATDeviceAudioOutput
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

public:	// ATDeviceSIO
	void InitSIO(IATDeviceSIOManager *mgr) override;
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;

public:	// IATDeviceRawSIO
	void OnCommandStateChanged(bool asserted) override;
	void OnMotorStateChanged(bool asserted) override;
	void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	void OnSendReady() override;

protected:
	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	IATDeviceIndicatorManager *mpUIRenderer = nullptr;
	IATAudioMixer *mpAudioMixer = nullptr;
	IATDeviceCIOManager *mpCIOMgr = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;
	ATFirmwareManager *mpFwMgr = nullptr;

	ATRS232Channel1030 *mpChannel = nullptr;

	AT850SIOEmulationLevel mEmulationLevel {};

	uint32 mDiskCounter = 0;
	bool mbFirmwareUsable = false;

	// AUTORUN.SYS and BOOT1030.COM style loaders hardcode a handler size of 0xB30, which
	// we must use. This comes from within 0x1100-1C2F in the ModemLink firmware. We also
	// keep the boot sector prepended to this.
	uint8 mFirmware[0x2880];
};

void ATCreateDevice1030Modem(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATDevice1030Modem> p(new ATDevice1030Modem);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDef1030Modem = { "1030", "1030", L"1030 Modem", ATCreateDevice1030Modem };

ATDevice1030Modem::ATDevice1030Modem()
	: mEmulationLevel(kAT850SIOEmulationLevel_None)
	, mDiskCounter(0)
{
	// We have to init this early so it can accept settings.
	mpChannel = new ATRS232Channel1030;
}

ATDevice1030Modem::~ATDevice1030Modem() {
	Shutdown();
}

void *ATDevice1030Modem::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceFirmware::kTypeID:	return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceAudioOutput::kTypeID:	return static_cast<IATDeviceAudioOutput *>(this);
		case IATDeviceCIO::kTypeID:			return static_cast<IATDeviceCIO *>(this);
		case IATDeviceSIO::kTypeID:			return static_cast<IATDeviceSIO *>(this);
		case IATDeviceRawSIO::kTypeID:		return static_cast<IATDeviceRawSIO *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATDevice1030Modem::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDef1030Modem;
}

void ATDevice1030Modem::GetSettings(ATPropertySet& settings) {
	if (mpChannel)
		mpChannel->GetSettings(settings);

	if (mEmulationLevel)
		settings.SetUint32("emulevel", (uint32)mEmulationLevel);
}

bool ATDevice1030Modem::SetSettings(const ATPropertySet& settings) {
	if (mpChannel)
		mpChannel->SetSettings(settings);

	uint32 emulevel = settings.GetUint32("emulevel", 0);
	if (emulevel < kAT850SIOEmulationLevelCount) {
		auto newLevel = (AT850SIOEmulationLevel)emulevel;
		
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

void ATDevice1030Modem::Init() {
	mpChannel->Init(mpScheduler, mpSlowScheduler, mpUIRenderer, mpCIOMgr, mpSIOMgr, this, mpAudioMixer);
}

void ATDevice1030Modem::Shutdown() {
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
	mpFwMgr = nullptr;

	mpUIRenderer = nullptr;
	mpAudioMixer = nullptr;
}

void ATDevice1030Modem::ColdReset() {
	if (mpChannel)
		mpChannel->ColdReset();

	mDiskCounter = 0;
}

void ATDevice1030Modem::InitFirmware(ATFirmwareManager *fwmgr) {
	mpFwMgr = fwmgr;

	ReloadFirmware();
}

bool ATDevice1030Modem::ReloadFirmware() {
	bool changed = false;

	mpFwMgr->LoadFirmware(mpFwMgr->GetCompatibleFirmware(kATFirmwareType_1030Firmware), mFirmware, 0, sizeof mFirmware, &changed, nullptr, nullptr, nullptr, &mbFirmwareUsable);

	return changed;
}

ATDeviceFirmwareStatus ATDevice1030Modem::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATDevice1030Modem::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;
}

void ATDevice1030Modem::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATDevice1030Modem::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;
}

void ATDevice1030Modem::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;

	if (mEmulationLevel != kAT850SIOEmulationLevel_Full)
		mpCIOMgr->AddCIODevice(this);
}

void ATDevice1030Modem::GetCIODevices(char *buf, size_t len) const {
	vdstrlcpy(buf, "T", len);
}

sint32 ATDevice1030Modem::OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) {
	if (deviceNo != 1)
		return kATCIOStat_UnkDevice;

	return mpChannel->Open(aux1, aux2);
}

sint32 ATDevice1030Modem::OnCIOClose(int channel, uint8 deviceNo) {
	// wait for output buffer to drain (requires assist)
	if (mpChannel->GetOutputLevel())
		return -1;

	mpChannel->Close();
	return kATCIOStat_Success;
}

sint32 ATDevice1030Modem::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	if (mpCIOMgr->IsBreakActive())
		return kATCIOStat_Break;

	while(len--) {
		uint8 c;
		if (!mpChannel->GetByte(c))
			return -1;

		++actual;
		*(uint8 *)buf = c;
		buf = (uint8 *)buf + 1;
	}

	return kATCIOStat_Success;
}

sint32 ATDevice1030Modem::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
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

sint32 ATDevice1030Modem::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	mpChannel->GetStatus(statusbuf);
	return kATCIOStat_Success;
}

sint32 ATDevice1030Modem::OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	return kATCIOStat_NotSupported;
}

void ATDevice1030Modem::OnCIOAbortAsync() {
}

void ATDevice1030Modem::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;

	if (mEmulationLevel != kAT850SIOEmulationLevel_None)
		mpSIOMgr->AddDevice(this);

	mpSIOMgr->AddRawDevice(this);
}

IATDeviceSIO::CmdResponse ATDevice1030Modem::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	if (!cmd.mbStandardRate)
		return kCmdResponse_NotHandled;

	if (cmd.mDevice == 0x31) {
		if (cmd.mCommand == 0x53) {			// status
			// The 1030 answers on the 13th D1: status request.
			if (mDiskCounter >= 13) {
				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();

				const uint8 status[4]={
					0x00, 0x00, 0xE0, 0x00
				};

				mpSIOMgr->SendData(status, 4, true);
				mpSIOMgr->EndCommand();
				return kCmdResponse_Start;
			}

			++mDiskCounter;
		} else {
			if (cmd.mCommand == 0x52 && mDiskCounter >= 13) {	// read sector
				uint32 sector = VDReadUnalignedLEU16(cmd.mAUX);

				if (sector != 1)
					return kCmdResponse_Fail_NAK;

				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();
				mpSIOMgr->SendData(mFirmware, 0x80, true);
				mpSIOMgr->EndCommand();
			} else {
				mDiskCounter = 0;
			}
		}

		return kCmdResponse_NotHandled;
	}

	if (cmd.mDevice != 0x58)
		return kCmdResponse_NotHandled;

	if (cmd.mCommand == 0x3B) {
		if (mEmulationLevel == kAT850SIOEmulationLevel_Full) {
			// ModemLink software load -- return $2800 bytes to load at $C00
			//
			// The 1030 sends data at three different rates depending on which
			// portion of the firmware is being sent. The speeds below are calibrated
			// based on measurements from the actual hardware.

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();

			// ModemLink part 1
			mpSIOMgr->SetTransferRate(93, 1032);
			mpSIOMgr->SendData(mFirmware + 0x80, 0x1100, false);

			// T: handler
			mpSIOMgr->SetTransferRate(93, 1046);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1100, 0x100, false);
			mpSIOMgr->SetTransferRate(93, 1059);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1200, 0x780, false);
			mpSIOMgr->SetTransferRate(93, 1032);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1980, 0x200, false);
			mpSIOMgr->SetTransferRate(93, 1059);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1B80, 0xB0, false);

			// ModemLink part 2
			mpSIOMgr->SetTransferRate(93, 1032);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1C30, 0xB50, false);

			// blank sector
			mpSIOMgr->SetTransferRate(93, 1059);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x2780, 0x80, false);

			const uint8 checksum = ATComputeSIOChecksum(mFirmware+0x80, 0x2800);
			mpSIOMgr->SendData(&checksum, 1, false);

			mpSIOMgr->EndCommand();
			return kCmdResponse_Start;
		}
	} else if (cmd.mCommand == 0x3C) {
		// Handler load - return $B30 bytes to load at $1D00 and run at $1D0C
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();

		if (mEmulationLevel == kAT850SIOEmulationLevel_Full) {
			// Send the embedded handler within the ModemLink firmware. This is a
			// subset of the data sent by the $3B command, with the same rates.

			mpSIOMgr->SetTransferRate(93, 1046);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1100, 0x100, false);
			mpSIOMgr->SetTransferRate(93, 1059);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1200, 0x780, false);
			mpSIOMgr->SetTransferRate(93, 1032);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1980, 0x200, false);
			mpSIOMgr->SetTransferRate(93, 1059);
			mpSIOMgr->SendData(mFirmware + 0x80 + 0x1B80, 0xB0, false);

			const uint8 checksum = ATComputeSIOChecksum(mFirmware + 0x1180, 0xB30);
			mpSIOMgr->SendData(&checksum, 1, false);

		} else {
			vdfastvector<uint8> buf(0xB30, 0);
			buf[0x0C] = 0x60;		// RTS
			mpSIOMgr->SendData(buf.data(), (uint32)buf.size(), true);
		}

		mpSIOMgr->EndCommand();
		return kCmdResponse_Start;
	}

	return kCmdResponse_NotHandled;
}

void ATDevice1030Modem::OnCommandStateChanged(bool asserted) {
}

void ATDevice1030Modem::OnMotorStateChanged(bool asserted) {
}

void ATDevice1030Modem::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	// check for proper 300 baud operation (divisor = 2982, 5% tolerance)
	if (cyclesPerBit > 5666 && cyclesPerBit < 6266 && mpChannel) {
		if (command) {
			mpChannel->ExecuteDeviceCommand(c);

			mpSIOMgr->SetSIOProceed(this, true);
			mpSIOMgr->SetSIOProceed(this, false);
		} else
			mpChannel->ReceiveByte(c);
	}
}

void ATDevice1030Modem::OnSendReady() {
}
