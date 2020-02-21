//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include <at/atcore/deviceimpl.h>
#include <at/atcore/devicecio.h>
#include <at/atcore/deviceparentimpl.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/devicesio.h>
#include <at/atcore/propertyset.h>
#include <at/atcore/scheduler.h>
#include "rs232.h"
#include "debuggerlog.h"
#include "firmwaremanager.h"

ATDebuggerLogChannel g_ATLC850SIO(false, false, "850", "850 interface SIO activity");
ATDebuggerLogChannel g_ATLCModemData(false, false, "MODEMDATA", "Modem interface data");

//////////////////////////////////////////////////////////////////////////

class IATRS232ChannelCallback {
public:
	virtual void OnChannelReceiveReady(int index) = 0;
};

class ATRS232Channel850 final : public IATSchedulerCallback {
public:
	ATRS232Channel850();

	IATDeviceSerial *GetSerialDevice() { return mpDeviceSerial; }
	void SetSerialDevice(IATDeviceSerial *dev);

	void Init(int index, IATRS232ChannelCallback *cb, ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr);
	void Shutdown();

	void ColdReset();

	uint8 Open(uint8 aux1, uint8 aux2, bool sioBased);
	void Close();

	uint8 GetCIOPermissions() const { return mOpenPerms; }
	uint32 GetOutputLevel() const { return mOutputLevel; }
	uint32 GetCyclesPerByte() const { return mCyclesPerByte; }
	uint32 GetCyclesPerByteDevice() const { return mCyclesPerByteDevice; }

	void SetCyclesPerByteDevice(uint32 cyc) { mCyclesPerByteDevice = cyc; }

	bool CheckMonitoredControlLines();

	bool CheckSerialFormatConcurrentOK(uint8 aux1);

	bool SetConcurrentMode();
	void ExitConcurrentMode();

	void ReconfigureDataRate(uint8 aux1, uint8 aux2, bool sioBased);
	void ReconfigureTranslation(uint8 aux1, uint8 aux2);

	void SetConfig(const ATRS232Config& config);

	void GetStatus(uint8 statusbuf[4]);
	void ReadControlStatus(uint8& errors, uint8& control);
	bool GetByte(uint8& c, bool siodirect);
	bool PutByte(uint8 c);

	bool IsSuspended() const { return mbSuspended; }
	void ExecuteDeviceCommand(uint8 c);

	void SetTerminalState(uint8 c);

protected:
	void FlushInputBuffer();
	void OnControlStateChanged(const ATDeviceSerialStatus& status);
	void OnScheduledEvent(uint32 id);
	void PollDevice(bool unthrottled);
	void EnqueueReceivedByte(uint8 c);

	int mIndex = 0;
	IATRS232ChannelCallback *mpCB = nullptr;
	ATScheduler	*mpScheduler = nullptr;
	ATEvent	*mpEvent = nullptr;
	vdrefptr<IATDeviceSerial> mpDeviceSerial;
	IATDeviceCIOManager *mpCIOMgr = nullptr;
	IATDeviceSIOManager *mpSIOMgr = nullptr;

	uint32	mCyclesPerByte;
	uint32	mCyclesPerByteDevice;

	bool	mbAddLFAfterEOL = false;
	bool	mbTranslationEnabled = false;
	bool	mbTranslationHeavy = false;
	bool	mbLFPending = false;
	bool	mbConcurrentMode = false;
	bool	mbSuspended = true;
	uint8	mWontTranslateChar = 0;

	uint8	mInputParityMask = 0;

	uint8	mOpenPerms = 0;		// Mirror of ICAX1 at open time.
	uint8	mControlState = 0;

	// These must match the error flags returned by STATUS.
	enum {
		kErrorFlag850_FramingError = 0x80
	};

	uint8	mErrorFlags = 0;
	uint8	mWordSize = 8;

	uint32	mBaudRate = 300;

	// These must match bits 0-1 of AUX1 as passed to XIO 38.
	enum OutputParityMode {
		kOutputParityNone	= 0x00,
		kOutputParityOdd	= 0x01,
		kOutputParityEven	= 0x02,
		kOutputParitySet	= 0x03
	};

	OutputParityMode	mOutputParityMode = kOutputParityNone;

	ATDeviceSerialTerminalState	mTerminalState;

	enum {
		kMonitorLine_CRX = 0x01,
		kMonitorLine_CTS = 0x02,
		kMonitorLine_DSR = 0x04
	};

	uint8	mControlMonitorMask = 0;

	int		mInputReadOffset = 0;
	int		mInputWriteOffset = 0;
	int		mInputLevel = 0;
	int		mInputBufferSize = 0;
	uint16	mInputBufAddr = 0;
	int		mOutputReadOffset = 0;
	int		mOutputWriteOffset = 0;
	int		mOutputLevel = 0;

	// This buffer is 32 bytes long internally for R:.
	uint8	mInputBuffer[32] {};
	uint8	mOutputBuffer[32] {};

	ATRS232Config mConfig;
};

ATRS232Channel850::ATRS232Channel850() {
}

void ATRS232Channel850::SetSerialDevice(IATDeviceSerial *dev) {
	if (mpDeviceSerial == dev)
		return;

	if (mpDeviceSerial)
		mpDeviceSerial->SetOnStatusChange(nullptr);

	if (dev)
		dev->SetOnStatusChange([this](const ATDeviceSerialStatus& status) { this->OnControlStateChanged(status); });

	mpDeviceSerial = dev;

	mControlState = 0;

	if (dev) {
		// compute initial control state
		ATDeviceSerialStatus cstate(dev->GetStatus());
	
		if (cstate.mbCarrierDetect)
			mControlState += 0x0c;

		if (cstate.mbClearToSend)
			mControlState += 0x30;

		if (cstate.mbDataSetReady)
			mControlState += 0xc0;
	}
}

void ATRS232Channel850::Init(int index, IATRS232ChannelCallback *cb, ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATDeviceCIOManager *ciomgr, IATDeviceSIOManager *siomgr) {
	mIndex = index;
	mpCB = cb;
	mpScheduler = sched;
	mpCIOMgr = ciomgr;
	mpSIOMgr = siomgr;

	// Default to 300 baud, 8 data bits, 1 stop bit, no input parity, space output parity,
	// no CR after LF, light translation. This must NOT be reapplied on each open or it
	// breaks BobTerm.
	ReconfigureDataRate(0, 0, false);
	ReconfigureTranslation(0, 0);

	mCyclesPerByteDevice = mCyclesPerByte;
}

void ATRS232Channel850::Shutdown() {
	mpDeviceSerial.clear();

	if (mpScheduler) {
		if (mpEvent) {
			mpScheduler->RemoveEvent(mpEvent);
			mpEvent = NULL;
		}

		mpScheduler = NULL;
	}

	mpSIOMgr = nullptr;
}

void ATRS232Channel850::ColdReset() {
	Close();

	mControlMonitorMask = 0;
}

uint8 ATRS232Channel850::Open(uint8 aux1, uint8 aux2, bool sioBased) {
	if (!mpEvent)
		mpEvent = mpScheduler->AddEvent(mCyclesPerByte, this, 1);

	mOpenPerms = aux1;
	mInputReadOffset = 0;
	mInputWriteOffset = 0;
	mInputLevel = 0;
	mInputBufferSize = 32;
	mInputBufAddr =0;

	mOutputReadOffset = 0;
	mOutputWriteOffset = 0;
	mOutputLevel = 0;

	mErrorFlags = 0;

	mbLFPending = false;

	// AUX1:
	//	bit 0 = concurrent mode
	//	bit 2 = input mode
	//	bit 3 = output mode

	// We ignore the concurrent I/O bit for now because concurrent I/O doesn't actually
	// start until XIO 40 is issued. This is required by ForemXEP, which opens for
	// concurrent mode but polls modem status before actually entering it. Not handling
	// this correctly results in ForemXEP immediately dropping calls because it thinks
	// the modem has lost carrier (it reads the input empty indicator as !CD).
	//mbConcurrentMode = (aux1 & 1) != 0;

	mbSuspended = false;

	if (!sioBased) {
		// ICD Multi I/O Manual Chapter 6: Open sets RTS and DTR to high.
		mTerminalState.mbDataTerminalReady = true;
		mTerminalState.mbRequestToSend = true;
	}

	if (mpDeviceSerial)
		mpDeviceSerial->SetTerminalState(mTerminalState);

	FlushInputBuffer();

	return kATCIOStat_Success;
}

void ATRS232Channel850::Close() {
	if (mpEvent) {
		mpScheduler->RemoveEvent(mpEvent);
		mpEvent = NULL;
	}

	if (mbConcurrentMode)
		mErrorFlags = 0;

	// LAMEAPP: This is required to make BBS Express work -- it expects to be able
	// to close a channel and then issue a status request to read CD without
	// re-opening the stream first.
	mbConcurrentMode = false;

	mbSuspended = true;
}

bool ATRS232Channel850::CheckMonitoredControlLines() {
	if (!mControlMonitorMask)
		return true;

	// We need to translate from the current+change pairs returned by
	// the status command to the test mask passed into XIO 36.
	uint8 currentState = 0;

	if (mControlState & 0x08)
		currentState += kMonitorLine_CRX;

	if (mControlState & 0x20)
		currentState += kMonitorLine_CTS;

	if (mControlState & 0x80)
		currentState += kMonitorLine_DSR;

	// Fail the transition if a monitored line is not asserted (on).
	if (~currentState & mControlMonitorMask) {
		// Set the external device not ready bit (bit 2).
		mErrorFlags |= 0x04;
		return false;
	}

	return true;
}

bool ATRS232Channel850::CheckSerialFormatConcurrentOK(uint8 iodir) {
	// The 850 can only handle input-only 300 baud max for odd word sizes.
	if (mWordSize < 8 && (mBaudRate > 300 || (iodir & 0x0c) != 0x04)) {
		// set interface error flag
		mErrorFlags |= 0x01;
		return false;
	}

	return true;
}

bool ATRS232Channel850::SetConcurrentMode() {
	if (!CheckMonitoredControlLines())
		return false;

	mbConcurrentMode = true;

	FlushInputBuffer();
	return true;
}

void ATRS232Channel850::ExitConcurrentMode() {
	mbConcurrentMode = false;
}

void ATRS232Channel850::ReconfigureDataRate(uint8 aux1, uint8 aux2, bool sioBased) {
	static const uint32 kBaudTable[16]={
		300,		// 0000 = 300 baud
		45,			// 0001 = 45.5 baud
		50,			// 0010 = 50 baud
		57,			// 0011 = 56.875 baud
		75,			// 0100 = 75 baud
		110,		// 0101 = 110 baud
		135,		// 0110 = 134.5 baud
		150,		// 0111 = 150 baud
		300,		// 1000 = 300 baud
		600,		// 1001 = 600 baud
		1200,		// 1010 = 1200 baud
		1800,		// 1011 = 1800 baud
		2400,		// 1100 = 2400 baud
		4800,		// 1101 = 4800 baud
		9600,		// 1110 = 9600 baud
		19200,		// 1111 = 19200 baud
	};

	static const uint32 kPeriodTable[16]={
		 59659,		// 0000 = 300 baud
		393357,		// 0001 = 45.5 baud
		357955,		// 0010 = 50 baud
		314685,		// 0011 = 56.875 baud
		238636,		// 0100 = 75 baud
		162707,		// 0101 = 110 baud
		133069,		// 0110 = 134.5 baud
		119318,		// 0111 = 150 baud
		 59659,		// 1000 = 300 baud
		 29830,		// 1001 = 600 baud
		 14915,		// 1010 = 1200 baud
		  9943,		// 1011 = 1800 baud
		  7457,		// 1100 = 2400 baud
		  3729,		// 1101 = 4800 baud
		  1864,		// 1110 = 9600 baud
		   932,		// 1111 = 19200 baud
	};

	uint32 baudIdx = aux1 & 15;
	mBaudRate = kBaudTable[baudIdx];
	mCyclesPerByte = kPeriodTable[baudIdx];

	if (!sioBased)
		mCyclesPerByteDevice = mCyclesPerByte;

	mWordSize = 8 - ((aux1 >> 4) & 3);

	if (mConfig.mbExtendedBaudRates) {	// Atari800 extension
		if (aux1 & 0x40) {
			mBaudRate = 230400;
			mCyclesPerByte = 78;		// 77.68
		} else if (baudIdx == 1) {
			mBaudRate = 57600;
			mCyclesPerByte = 311;		// 310.7
		} else if (baudIdx == 3) {
			mBaudRate = 115200;
			mCyclesPerByte = 155;		// 155.36
		}
	}

	mControlMonitorMask = aux2 & 7;
	mErrorFlags = 0;
}

void ATRS232Channel850::ReconfigureTranslation(uint8 aux1, uint8 aux2) {
	mbTranslationEnabled = (aux1 & 0x20) == 0;
	mbTranslationHeavy = (aux1 & 0x10) != 0;

	// EOL -> CR+LF is only available if translation is enabled.
	mbAddLFAfterEOL = mbTranslationEnabled && (aux1 & 0x40);

	mWontTranslateChar = aux2;

	mInputParityMask = (aux1 & 0x0c) ? 0x7F : 0xFF;
	mOutputParityMode = (OutputParityMode)(aux1 & 0x03);

	mErrorFlags = 0;
}

void ATRS232Channel850::SetConfig(const ATRS232Config& config) {
	mConfig = config;
}

void ATRS232Channel850::GetStatus(uint8 statusbuf[4]) {
	if (mConfig.mbDisableThrottling)
		PollDevice(true);

	statusbuf[0] = mErrorFlags;

	if (mbConcurrentMode) {
		statusbuf[1] = mInputLevel & 0xFF;
	} else {
		statusbuf[1] = mControlState;
		mErrorFlags = 0;
	}

	statusbuf[2] = mInputLevel >> 8;
	statusbuf[3] = mOutputLevel;

	// reset sticky bits
	mControlState &= 0xa8;
	mControlState += (mControlState >> 1);
}

void ATRS232Channel850::ReadControlStatus(uint8& errors, uint8& control) {
	errors = 0;
	control = mControlState;

	mErrorFlags = 0;

	// reset sticky bits
	mControlState &= 0xa8;
	mControlState += (mControlState >> 1);
}

bool ATRS232Channel850::GetByte(uint8& c, bool siodirect) {
	if (mConfig.mbDisableThrottling && !siodirect)
		PollDevice(true);

	if (!mInputLevel)
		return false;

	if (mInputBufAddr)
		c = mpCIOMgr->ReadByte(mInputBufAddr + mInputReadOffset);
	else
		c = mInputBuffer[mInputReadOffset];

	if (++mInputReadOffset >= mInputBufferSize)
		mInputReadOffset = 0;

	--mInputLevel;

	c &= mInputParityMask;

	if (mbTranslationEnabled) {
		// strip high bit
		c &= 0x7f;

		// convert CR to EOL
		if (c == 0x0D)
			c = 0x9B;
		else if (mbTranslationHeavy && (uint8)(c - 0x20) > 0x5C)
			c = mWontTranslateChar;
	}

	return true;
}

bool ATRS232Channel850::PutByte(uint8 c) {
	if (mbLFPending)
		c = 0x0A;

	if (mbTranslationEnabled) {
		// convert EOL to CR
		if (c == 0x9B)
			c = 0x0D;

		if (mbTranslationHeavy) {
			// heavy translation -- drop bytes <$20 or >$7C
			if ((uint8)(c - 0x20) > 0x5C && c != 0x0D)
				return true;
		} else {
			// light translation - strip high bit
			c &= 0x7f;
		}
	}

	for(;;) {
		if (mOutputLevel >= sizeof mOutputBuffer)
			return false;
		
		uint8 d = c;

		static const uint8 kParityTable[16]={
			0x00, 0x80, 0x80, 0x00,
			0x80, 0x00, 0x00, 0x80,
			0x80, 0x00, 0x00, 0x80,
			0x00, 0x80, 0x80, 0x00,
		};

		switch(mOutputParityMode) {
			case kOutputParityNone:
			default:
				break;

			case kOutputParityEven:
				d ^= kParityTable[(d & 0x0F) ^ (d >> 4)];
				break;

			case kOutputParityOdd:
				d ^= kParityTable[(d & 0x0F) ^ (d >> 4)];
				d ^= 0x80;
				break;

			case kOutputParitySet:
				d |= 0x80;
				break;
		}

		mOutputBuffer[mOutputWriteOffset] = d;
		if (++mOutputWriteOffset >= sizeof mOutputBuffer)
			mOutputWriteOffset = 0;

		++mOutputLevel;

		if (mConfig.mbDisableThrottling)
			PollDevice(true);

		// If we just pushed out a CR byte and add-LF is enabled, we need to
		// loop around and push out another LF byte.
		if (c != 0x0D || !mbAddLFAfterEOL)
			break;

		mbLFPending = true;
		c = 0x0A;
	}

	mbLFPending = false;
	return true;
}

void ATRS232Channel850::ExecuteDeviceCommand(uint8 c) {
}

void ATRS232Channel850::SetTerminalState(uint8 c) {
	bool changed = false;

	if (c & 0x80) {
		bool dtr = (c & 0x40) != 0;

		if (mTerminalState.mbDataTerminalReady != dtr) {
			mTerminalState.mbDataTerminalReady = dtr;

			changed = true;
		}
	}

	if (c & 0x20) {
		bool rts = (c & 0x10) != 0;

		if (mTerminalState.mbRequestToSend != rts) {
			mTerminalState.mbRequestToSend = rts;

			changed = true;
		}
	}

	if (changed && mpDeviceSerial)
		mpDeviceSerial->SetTerminalState(mTerminalState);
}

void ATRS232Channel850::FlushInputBuffer() {
	mInputLevel = 0;
	mInputReadOffset = 0;
	mInputWriteOffset = 0;

	if (mpDeviceSerial)
		mpDeviceSerial->FlushBuffers();
}

void ATRS232Channel850::OnControlStateChanged(const ATDeviceSerialStatus& status) {
	// Line state transitions:
	//
	//	Prev				State	Next
	//	00 (always off)		off		00 (always off)
	//	01 (currently off)	off		01 (currently off)
	//	10 (currently on)	off		01 (currently off)
	//	11 (always on)		off		01 (currently off)
	//	00 (always off)		on		10 (currently on)
	//	01 (currently off)	on		10 (currently on)
	//	10 (currently on)	on		10 (currently on)
	//	11 (always on)		on		11 (always on)
	if (status.mbCarrierDetect) {
		if (!(mControlState & 0x08))
			mControlState = (mControlState & 0xf3) + 0x08;
	} else {
		if (mControlState & 0x08)
			mControlState = (mControlState & 0xf3) + 0x04;
	}

	if (status.mbClearToSend) {
		if (!(mControlState & 0x20))
			mControlState = (mControlState & 0xcf) + 0x20;
	} else {
		if (mControlState & 0x20)
			mControlState = (mControlState & 0xcf) + 0x10;
	}

	if (status.mbDataSetReady) {
		if (!(mControlState & 0x80))
			mControlState = (mControlState & 0x3f) + 0x80;
	} else {
		if (mControlState & 0x80)
			mControlState = (mControlState & 0x3f) + 0x40;
	}
}

void ATRS232Channel850::OnScheduledEvent(uint32 id) {
	mpEvent = mpScheduler->AddEvent(mCyclesPerByteDevice, this, 1);

	PollDevice(false);
}

void ATRS232Channel850::PollDevice(bool unthrottled) {
	if (mOutputLevel) {
		--mOutputLevel;

		if (mpDeviceSerial) {
			const uint8 c = mOutputBuffer[mOutputReadOffset];
			g_ATLCModemData("Sending byte to modem: $%02X [%c]\n", c, (uint8)(c - 0x20) < 0x7F ? c : '.');
			mpDeviceSerial->Write(mBaudRate, c);
		}

		if (++mOutputReadOffset >= sizeof mOutputBuffer)
			mOutputReadOffset = 0;
	}

	if (mpDeviceSerial) {
		uint8 c;
		bool framingError;

		while (mInputLevel < mInputBufferSize && mpDeviceSerial->Read(mBaudRate, c, framingError)) {
			g_ATLCModemData("Receiving byte from modem: $%02X [%c]\n", c, (uint8)(c - 0x20) < 0x7F ? c : '.');

			if (framingError)
				mErrorFlags |= kErrorFlag850_FramingError;

			EnqueueReceivedByte(c);

			if (!unthrottled)
				break;
		}
	}
}

void ATRS232Channel850::EnqueueReceivedByte(uint8 c) {
	if (mInputBufAddr)
		mpCIOMgr->WriteByte(mInputBufAddr + mInputWriteOffset, c);
	else
		mInputBuffer[mInputWriteOffset] = c;

	if (++mInputWriteOffset >= mInputBufferSize)
		mInputWriteOffset = 0;

	++mInputLevel;

	if (mpCB)
		mpCB->OnChannelReceiveReady(mIndex);
}

//////////////////////////////////////////////////////////////////////////

class ATRS232Emulator final
	: public ATDevice
	, public IATDeviceScheduling
	, public IATDeviceIndicators
	, public IATDeviceFirmware
	, public IATDeviceCIO
	, public IATDeviceSIO
	, public IATDeviceRawSIO
	, public IATRS232ChannelCallback
{
	ATRS232Emulator(const ATRS232Emulator&);
	ATRS232Emulator& operator=(const ATRS232Emulator&);
public:
	ATRS232Emulator();
	~ATRS232Emulator();

	void *AsInterface(uint32 id) override;

	void Init() override;
	void Shutdown() override;
	void GetDeviceInfo(ATDeviceInfo& info) override;

	void ColdReset() override;

	void GetSettings(ATPropertySet& props) override;
	bool SetSettings(const ATPropertySet& props) override;

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
	CmdResponse OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) override;
	void OnSerialAbortCommand() override;
	void OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) override;
	void OnSerialFence(uint32 id) override; 
	CmdResponse OnSerialAccelCommand(const ATDeviceSIORequest& request) override;

public:	// IATDeviceRawSIO
	void OnCommandStateChanged(bool asserted) override;
	void OnMotorStateChanged(bool asserted) override;
	void OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) override;
	void OnSendReady() override;

public:
	virtual void OnChannelReceiveReady(int index);

protected:
	void InitChannels();
	void ShutdownChannels();

	ATScheduler *mpScheduler;
	ATScheduler *mpSlowScheduler;
	IATDeviceIndicatorManager *mpUIRenderer;
	ATFirmwareManager *mpFwMgr;
	IATDeviceCIOManager *mpCIOMgr;
	IATDeviceSIOManager *mpSIOMgr;

	ATRS232Channel850 *mpChannels[4];
	ATRS232Config mConfig;

	bool	mbFirmwareUsable = false;

	sint8	mActiveConcurrentIndex;
	uint8	mActiveCommandData;

	sint8	mPollCounter;
	sint8	mDiskCounter;

	vdfastvector<uint8> mRelocator;
	vdfastvector<uint8> mHandler;
	
	ATDeviceParentSingleChild mDeviceParent;
};

void ATCreateDevice850Modem(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATRS232Emulator> p(new ATRS232Emulator);

	*dev = p.release();
}

extern const ATDeviceDefinition g_ATDeviceDef850Modem = { "850", "850", L"850 Interface Module", ATCreateDevice850Modem };

ATRS232Emulator::ATRS232Emulator()
	: mpScheduler(NULL)
	, mpSlowScheduler(NULL)
	, mpUIRenderer(NULL)
	, mpFwMgr(nullptr)
	, mpCIOMgr(nullptr)
	, mpSIOMgr(nullptr)
	, mActiveConcurrentIndex(-1)
{
	for(int i=0; i<4; ++i)
		mpChannels[i] = NULL;
}

ATRS232Emulator::~ATRS232Emulator() {
	Shutdown();
}

void *ATRS232Emulator::AsInterface(uint32 id) {
	switch(id) {
		case IATDeviceFirmware::kTypeID:	return static_cast<IATDeviceFirmware *>(this);
		case IATDeviceParent::kTypeID:		return static_cast<IATDeviceParent *>(&mDeviceParent);
		case IATDeviceScheduling::kTypeID:	return static_cast<IATDeviceScheduling *>(this);
		case IATDeviceIndicators::kTypeID:	return static_cast<IATDeviceIndicators *>(this);
		case IATDeviceCIO::kTypeID:			return static_cast<IATDeviceCIO *>(this);
		case IATDeviceSIO::kTypeID:			return static_cast<IATDeviceSIO *>(this);
		case IATDeviceRawSIO::kTypeID:		return static_cast<IATDeviceRawSIO *>(this);
	}

	return ATDevice::AsInterface(id);
}

void ATRS232Emulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDef850Modem;
}

void ATRS232Emulator::Init() {
	InitChannels();

	mDeviceParent.Init(IATDeviceSerial::kTypeID, "serial", L"Serial Port", "serial", this);
	mDeviceParent.SetOnAttach(
		[this] {
			for(auto *p : mpChannels) {
				if (p && !p->GetSerialDevice()) {
					p->SetSerialDevice(mDeviceParent.GetChild<IATDeviceSerial>());
					break;
				}
			}
		}
	);

	mDeviceParent.SetOnDetach(
		[this] {
			IATDeviceSerial *serdev = mDeviceParent.GetChild<IATDeviceSerial>();

			for(auto *p : mpChannels) {
				if (p && p->GetSerialDevice() == serdev) {
					p->SetSerialDevice(nullptr);
					break;
				}
			}
		}
	);
}

void ATRS232Emulator::Shutdown() {
	mDeviceParent.Shutdown();

	if (mpCIOMgr) {
		mpCIOMgr->RemoveCIODevice(this);
		mpCIOMgr = nullptr;
	}

	if (mpSIOMgr) {
		mpSIOMgr->RemoveDevice(this);
		mpSIOMgr->RemoveRawDevice(this);
		mpSIOMgr = nullptr;
	}

	ShutdownChannels();
	mpScheduler = NULL;
	mpSlowScheduler = NULL;

	mpUIRenderer = NULL;
}

void ATRS232Emulator::ColdReset() {
	for(int i=0; i<4; ++i) {
		if (mpChannels[i])
			mpChannels[i]->ColdReset();
	}

	mActiveConcurrentIndex = -1;

	if (mpSIOMgr)
		mpSIOMgr->RemoveRawDevice(this);

	mPollCounter = 26;
	mDiskCounter = 26;
}

void ATRS232Emulator::GetSettings(ATPropertySet& props) {
	if (mConfig.mbExtendedBaudRates)
		props.SetBool("baudex", true);

	if (mConfig.mbDisableThrottling)
		props.SetBool("unthrottled", true);

	if (mConfig.m850SIOLevel)
		props.SetUint32("emulevel", (uint32)mConfig.m850SIOLevel);
}

bool ATRS232Emulator::SetSettings(const ATPropertySet& props) {
	mConfig.mbExtendedBaudRates = props.GetBool("baudex", false);
	mConfig.mbDisableThrottling = props.GetBool("unthrottled", false);

	uint32 levelIdx = props.GetUint32("emulevel", 0);
	if (levelIdx >= kAT850SIOEmulationLevelCount)
		levelIdx = 0;

	AT850SIOEmulationLevel newLevel = (AT850SIOEmulationLevel)levelIdx;

	if (mConfig.m850SIOLevel != newLevel) {
		if (mpSIOMgr) {
			if (newLevel == kAT850SIOEmulationLevel_None)
				mpSIOMgr->RemoveDevice(this);
			else if (mConfig.m850SIOLevel == kAT850SIOEmulationLevel_None)
				mpSIOMgr->AddDevice(this);

			if (newLevel != kAT850SIOEmulationLevel_Full)
				mpSIOMgr->RemoveRawDevice(this);
		}

		if (mpCIOMgr) {
			if (newLevel == kAT850SIOEmulationLevel_Full)
				mpCIOMgr->RemoveCIODevice(this);
			else if (mConfig.m850SIOLevel == kAT850SIOEmulationLevel_Full)
				mpCIOMgr->AddCIODevice(this);
		}

		mConfig.m850SIOLevel = newLevel;
	}

	for(auto *p : mpChannels) {
		if (p)
			p->SetConfig(mConfig);
	}

	return true;
}

void ATRS232Emulator::InitFirmware(ATFirmwareManager *fwman) {
	mpFwMgr = fwman;

	ReloadFirmware();
}

bool ATRS232Emulator::ReloadFirmware() {
	bool changed = false;

	bool relocatorUsable = false;
	bool handlerUsable = false;
	mpFwMgr->LoadFirmware(kATFirmwareId_850Relocator, nullptr, 0, 0, &changed, nullptr, &mRelocator, nullptr, &relocatorUsable);
	mpFwMgr->LoadFirmware(kATFirmwareId_850Handler, nullptr, 0, 0, &changed, nullptr, &mHandler, nullptr, &handlerUsable);

	mbFirmwareUsable = relocatorUsable && handlerUsable;

	if (mRelocator.size() > 2048)
		mRelocator.resize(2048);

	if (mHandler.size() > 2048)
		mHandler.resize(2048);

	return changed;
}

ATDeviceFirmwareStatus ATRS232Emulator::GetFirmwareStatus() const {
	return mbFirmwareUsable ? ATDeviceFirmwareStatus::OK : ATDeviceFirmwareStatus::Missing;
}

void ATRS232Emulator::InitScheduling(ATScheduler *sch, ATScheduler *slowsch) {
	mpScheduler = sch;
	mpSlowScheduler = slowsch;
}

void ATRS232Emulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATRS232Emulator::InitCIO(IATDeviceCIOManager *mgr) {
	mpCIOMgr = mgr;

	if (mConfig.m850SIOLevel != kAT850SIOEmulationLevel_Full)
		mpCIOMgr->AddCIODevice(this);
}

void ATRS232Emulator::GetCIODevices(char *buf, size_t len) const {
	vdstrlcpy(buf, "R", len);
}

sint32 ATRS232Emulator::OnCIOOpen(int channel, uint8 deviceNo, uint8 aux1, uint8 aux2, const uint8 *filename) {
	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];

	return ch.Open(aux1, aux2, false);
}

sint32 ATRS232Emulator::OnCIOClose(int channel, uint8 deviceNo) {
	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];

	// wait for output buffer to drain (requires assist)
	if (ch.GetOutputLevel())
		return -1;

	ch.Close();
	return kATCIOStat_Success;
}

sint32 ATRS232Emulator::OnCIOGetBytes(int channel, uint8 deviceNo, void *buf, uint32 len, uint32& actual) {
	if (mpCIOMgr->IsBreakActive())
		return kATCIOStat_Break;

	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];

	while(len--) {
		uint8 c;
		if (!ch.GetByte(c, false))
			return -1;

		*(uint8 *)buf = c;
		buf = (uint8 *)buf + 1;

		++actual;
	}

	return kATCIOStat_Success;
}

sint32 ATRS232Emulator::OnCIOPutBytes(int channel, uint8 deviceNo, const void *buf, uint32 len, uint32& actual) {
	if (mpCIOMgr->IsBreakActive())
		return kATCIOStat_Break;

	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];

	const uint8 *buf8 = (const uint8 *)buf;
	while(len--) {
		if (!ch.PutByte(*buf8++))
			return -1;

		++actual;
	}

	return kATCIOStat_Success;
}

sint32 ATRS232Emulator::OnCIOGetStatus(int channel, uint8 deviceNo, uint8 statusbuf[4]) {
	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];
	ch.GetStatus(statusbuf);
	return kATCIOStat_Success;
}

sint32 ATRS232Emulator::OnCIOSpecial(int channel, uint8 deviceNo, uint8 cmd, uint16 bufadr, uint16 buflen, uint8 aux[6]) {
	if (deviceNo < 1 || deviceNo > 4)
		return kATCIOStat_UnkDevice;

	ATRS232Channel850& ch = *mpChannels[deviceNo - 1];
	switch(cmd) {
		case 32:	// XIO 32 Force short block (silently ignored)
			break;

		case 34:	// XIO 34 control DTR, RTS, XMT (silently ignored)
			ch.SetTerminalState(aux[0]);
			break;

		case 36:	// XIO 36 configure baud rate (partial support)
			ch.ReconfigureDataRate(aux[0], aux[1], false);
			break;

		case 38:	// XIO 38 configure translation mode (silently ignored)
			ch.ReconfigureTranslation(aux[0], aux[1]);
			break;

		case 40:	// XIO 40 concurrent mode (silently ignored)
			ch.SetConcurrentMode();
			break;

		default:
			return kATCIOStat_NotSupported;
	}

	// Page 41 of the OS Manual says:
	// "You should not alter ICAX1 once the device/file is open."
	//
	// ...which of course means that Atari BASIC does it: XIO commands stomp the
	// AUX1 and AUX2 bytes. The former then causes GET BYTE operations to break as
	// they check the permission bits in AUX1. To work around this, the SPECIAL
	// routine of R: handlers have to restore the permissions byte.
	aux[0] = ch.GetCIOPermissions();

	return kATCIOStat_Success;
}

void ATRS232Emulator::OnCIOAbortAsync() {
}

void ATRS232Emulator::InitSIO(IATDeviceSIOManager *mgr) {
	mpSIOMgr = mgr;

	if (mConfig.m850SIOLevel != kAT850SIOEmulationLevel_None)
		mpSIOMgr->AddDevice(this);
}

IATDeviceSIO::CmdResponse ATRS232Emulator::OnSerialBeginCommand(const ATDeviceSIOCommand& cmd) {
	VDASSERT(mConfig.m850SIOLevel);

	if (!cmd.mbStandardRate)
		return kCmdResponse_NotHandled;

	// check device ID
	const uint8 cmdid = cmd.mCommand;

	// non-poll commands reset poll counter, if poll hasn't already been answered
	if (cmdid != 0x3F && mPollCounter >= 0)
		mPollCounter = 26;

	if (mConfig.m850SIOLevel == kAT850SIOEmulationLevel_Full && cmd.mDevice == 0x31) {
		if (cmdid == 0x53) {				// status
			// The 850 only answers the 26th status request. Once this arrives, it unlocks
			// that status request and any subsequent disk requests. Another status request
			// turns off disk emulation.
			if (mDiskCounter > 0 && !--mDiskCounter) {
				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();

				const uint8 kData[4]={ 0x00, 0xFF, 0xFE, 0x00 };
				mpSIOMgr->SendData(kData, 4, true);

				mpSIOMgr->EndCommand();
				return kCmdResponse_Start;
			}

			return kCmdResponse_NotHandled;
		} else if (cmdid == 0x52) {		// read
			if (mDiskCounter == 0) {
				uint32 sector = VDReadUnalignedLEU16(cmd.mAUX);

				if (sector < 1 || sector > 3) {
					// out of range -- send NAK
					return kCmdResponse_Fail_NAK;
				}

				uint32 rellen = (uint32)mRelocator.size();
				uint32 offset = (sector-1) * 128;

				vdblock<uint8> buf(128);
				memset(buf.data(), 0, 128);

				if (offset < rellen)
					memcpy(buf.data(), mRelocator.data() + offset, std::min<uint32>(128, rellen - offset));

				mpSIOMgr->BeginCommand();
				mpSIOMgr->SendACK();
				mpSIOMgr->SendComplete();
				mpSIOMgr->SendData(buf.data(), 128, true);
				mpSIOMgr->EndCommand();
				return kCmdResponse_Start;
			}

			return kCmdResponse_NotHandled;
		}
	}

	// Non-poll and non-D1: commands kill disk emulation.
	if (cmdid != 0x3F && cmd.mDevice != 0x31)
		mDiskCounter = -1;

	const uint32 index = cmd.mDevice - 0x50;
	if (cmdid != 0x3F && index >= 4)
		return kCmdResponse_NotHandled;

	if (cmdid == 0x3F || index < 4)
		g_ATLC850SIO("Unit %d | Command %02x %02x %02x\n", index + 1, cmd.mCommand, cmd.mAUX[0], cmd.mAUX[1]);

	if (cmdid == 0x3F) {
		// Poll command -- send back SIO command for booting. This
		// is an 12 byte + chk block that is meant to be written to
		// the SIO parameter block starting at DDEVIC ($0300).
		//
		// The boot block MUST start at $0500. There are both BASIC-
		// based and cart-based loaders that use JSR $0506 to run the
		// loader.

		if (mPollCounter > 0 && !--mPollCounter) {
			uint8 bootBlock[12]={
				0x50,		// DDEVIC
				0x01,		// DUNIT
				0x21,		// DCOMND = '!' (boot)
				0x40,		// DSTATS
				0x00, 0x05,	// DBUFLO, DBUFHI == $0500
				0x08,		// DTIMLO = 8 vblanks
				0x00,		// not used
				0x00, 0x00,	// DBYTLO, DBYTHI
				0x00,		// DAUX1
				0x00,		// DAUX2
			};

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();

			uint32 relsize = (uint32)mRelocator.size();
			bootBlock[8] = (uint8)relsize;
			bootBlock[9] = (uint8)(relsize >> 8);
			mpSIOMgr->SendData(bootBlock, 12, true);
			mpSIOMgr->EndCommand();

			// Once the poll is answered, we don't answer it again until power cycle.
			mPollCounter = -1;
			return kCmdResponse_Start;
		}

		return kCmdResponse_NotHandled;
	}

	if (index >= 4)
		return kCmdResponse_NotHandled;
	
	if (cmdid == 0x21) {
		// Boot command -- send back boot loader.
		static const uint8 kLoaderBlock[7]={
			0x00,		// flags
			0x01,		// sector count
			0x00, 0x05,	// load address
			0x06, 0x05,	// init address
			0x60
		};

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();

		if (mConfig.m850SIOLevel == kAT850SIOEmulationLevel_StubLoader) {
			mpSIOMgr->SendData(kLoaderBlock, (uint32)sizeof kLoaderBlock, true);
		} else {
			mpSIOMgr->SendData(mRelocator.data(), (uint32)mRelocator.size(), true);
		}

		mpSIOMgr->EndCommand();
		return kCmdResponse_Start;
	}
	
	if (mConfig.m850SIOLevel != kAT850SIOEmulationLevel_Full)
		return kCmdResponse_NotHandled;

	ATRS232Channel850& ch = *static_cast<ATRS232Channel850 *>(mpChannels[index]);

	if (cmdid == 0x53) {	// 'S' / status
		uint8 buf[2];
		ch.ReadControlStatus(buf[0], buf[1]);

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();
		mpSIOMgr->SendData(buf, 2, true);
		mpSIOMgr->EndCommand();
		return kCmdResponse_Start;
	} else if (cmdid == 0x57) {	// 'W' / write

		// check data length
		uint8 dataLen = cmd.mAUX[0];

		if (dataLen > 64)
			return kCmdResponse_Fail_NAK;

		if (!dataLen) {
			// 0 is a special case with no data frame
			return kCmdResponse_Send_ACK_Complete;
		} else {
			mActiveCommandData = dataLen;

			mpSIOMgr->BeginCommand();
			mpSIOMgr->SendACK();
			mpSIOMgr->ReceiveData(index, 64, true);
			mpSIOMgr->SendACK();
			mpSIOMgr->SendComplete();
		}
	} else if (cmdid == 0x41) {	// 'A' / control
		ch.SetTerminalState(cmd.mAUX[0]);

		return kCmdResponse_Send_ACK_Complete;
	} else if (cmdid == 0x58) {	// 'X' / stream
		if (!ch.CheckSerialFormatConcurrentOK(cmd.mAUX[0])) {
			// Invalid option configuration -- NAK it
			return kCmdResponse_Fail_NAK;
		}

		ch.Open(0x0C, 0, true);
		ch.ReconfigureTranslation(0x20, 0);

		if (!ch.SetConcurrentMode()) {
			// Hmm, seems a monitored control line was not active. NAK it.
			return kCmdResponse_Fail_NAK;
		}

		mActiveConcurrentIndex = index;

		mpSIOMgr->AddRawDevice(this);

		// The data payload from the stream command is 9 bytes to set
		// to POKEY.
		const uint32 cyclesPerHalfBit = (ch.GetCyclesPerByte() + 10) / 20;
		const uint32 divisor = cyclesPerHalfBit - 7;
		uint8 divlo = (uint8)divisor;
		uint8 divhi = (uint8)(divisor >> 8);

		uint8 buf[9];
		buf[0] = divlo;
		buf[1] = 0xA0;		// AUDC1
		buf[2] = divhi;
		buf[3] = 0xA0;		// AUDC2
		buf[4] = divlo;
		buf[5] = 0xA0;		// AUDC3
		buf[6] = divhi;
		buf[7] = 0xA0;		// AUDC4
		buf[8] = 0x78;		// AUDCTL

		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();
		mpSIOMgr->SendData(buf, 9, true);
		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	} else if (cmdid == 0x42) {	// 'B' / configure
		ch.ReconfigureDataRate(cmd.mAUX[0], cmd.mAUX[1], true);

		return kCmdResponse_Send_ACK_Complete;
	} else if (cmdid == 0x26) {	// '&' / load handler
		mpSIOMgr->BeginCommand();
		mpSIOMgr->SendACK();
		mpSIOMgr->SendComplete();
		mpSIOMgr->SendData(mHandler.data(), (uint32)mHandler.size(), true);
		mpSIOMgr->EndCommand();

		return kCmdResponse_Start;
	}

	// don't know this one - NAK it
	return kCmdResponse_Fail_NAK;
}

void ATRS232Emulator::OnSerialAbortCommand() {
}

void ATRS232Emulator::OnSerialReceiveComplete(uint32 id, const void *data, uint32 len, bool checksumOK) {
	ATRS232Channel850& ch = *static_cast<ATRS232Channel850 *>(mpChannels[id]);

	// push bytes into the device
	for(uint8 i=0; i<mActiveCommandData; ++i)
		ch.PutByte(((const uint8 *)data)[i]);
}

void ATRS232Emulator::OnSerialFence(uint32 id) {
}

IATDeviceSIO::CmdResponse ATRS232Emulator::OnSerialAccelCommand(const ATDeviceSIORequest& request) {
	return OnSerialBeginCommand(request);
}

void ATRS232Emulator::OnCommandStateChanged(bool asserted) {
	if (asserted) {
		for(int i=0; i<4; ++i)
			mpChannels[i]->ExitConcurrentMode();

		mActiveConcurrentIndex = -1;

		if (mpSIOMgr)
			mpSIOMgr->RemoveRawDevice(this);
	}
}

void ATRS232Emulator::OnMotorStateChanged(bool asserted) {
}

void ATRS232Emulator::OnReceiveByte(uint8 c, bool command, uint32 cyclesPerBit) {
	if (mActiveConcurrentIndex >= 0) {
		ATRS232Channel850& ch = *static_cast<ATRS232Channel850 *>(mpChannels[mActiveConcurrentIndex]);

		ch.PutByte(c);
		ch.SetCyclesPerByteDevice(cyclesPerBit * 10);
	}
}

void ATRS232Emulator::OnSendReady() {
}

void ATRS232Emulator::OnChannelReceiveReady(int index) {
	if (index == mActiveConcurrentIndex) {
		ATRS232Channel850& ch = *static_cast<ATRS232Channel850 *>(mpChannels[mActiveConcurrentIndex]);
		
		uint8 c;
		if (ch.GetByte(c, true)) {
			mpSIOMgr->SendRawByte(c, (ch.GetCyclesPerByteDevice() + 5) / 10);
		}
	}
}

void ATRS232Emulator::InitChannels() {
	for(int i=0; i<4; ++i) {
		auto *p = new ATRS232Channel850;
		mpChannels[i] = p;
		p->Init(i, this, mpScheduler, mpSlowScheduler, i ? NULL : mpUIRenderer, mpCIOMgr, mpSIOMgr);
	}
}

void ATRS232Emulator::ShutdownChannels() {
	for(auto*& p : mpChannels) {
		if (p) {
			p->Shutdown();
			delete p;
			p = NULL;
		}
	}
}
