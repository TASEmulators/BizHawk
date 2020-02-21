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

#ifndef f_AT_MODEM_H
#define f_AT_MODEM_H

#include <vd2/system/refcount.h>
#include <vd2/system/thread.h>
#include <vd2/system/VDString.h>
#include <at/atcore/deviceimpl.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/enumparse.h>
#include <at/atcore/scheduler.h>
#include "rs232.h"
#include "modemtcp.h"

class IATUIRenderer;
class ATModemSoundEngine;

enum class ATModemFlowControl : uint8 {
	None = 0,
	RTS_CTS = 3,
	XON_XOFF = 4,
	Transparent_XON_XOFF = 5
};

struct ATModemRegisters {
	ATModemRegisters();

	uint8	mAutoAnswerRings;				/// Number of rings on which the modem should auto-pickup an incoming call; zero to disable (S0).
	uint8	mEscapeChar;					/// Escape character for guard sequence; >127 disables, '+' is default (S2).
	uint8	mLineTermChar;					/// Line termination character for both command lines and output; CR is default (S3).
	uint8	mRespFormatChar;				/// Response formatting character for output; LF is default (S4).
	uint8	mCommandEditChar;				/// Backspace character for command-line editing; BS is default (S5).
	uint8	mDialToneWaitTime;				/// (S6).
	uint8	mDialCarrierWaitTime;			/// (S7).
	uint8	mDialPauseTime;					/// Pause time for , dial modifier in seconds. 2 is default (S8).
	uint8	mLostCarrierWaitTime;			/// Tenths of a second after lost carrier before auto hang up; 255 disables. 14 is default (S10).
	uint8	mDTMFToneDuration;				/// DTMF tone duration in milliseconds. 95 is default (S11). 
	uint8	mEscapePromptDelay;				/// (S12).
	bool	mbReportCarrier;				/// True if the CD line should report carrier state; default on (&C).
	uint8	mDTRMode;						/// 0 for ignore DTR; 1 for command mode on drop; 2 for hang up on drop; default 2 (&D).
	bool	mbEchoMode;						/// True if echoing is on (E).
	bool	mbQuietMode;					/// True if result codes should be suppressed; default off (Q).
	bool	mbToneDialMode;					/// True for tone dialing (T); false for pulse dialing (P).
	bool	mbShortResponses;				/// True for numeric responses (V0); false for long responses (V1).
	uint8	mExtendedResultCodes;			/// 0-4 (X).
	uint8	mSpeakerVolume;					/// 0=Off, 3=High (L).
	uint8	mSpeakerMode;					/// 0=off, 1=connect only, 2=on, 3=answer only (M).
	uint8	mGuardToneMode;					/// 0-2 (G).
	bool	mbLoopbackMode;					/// True if local analog loopback test mode is enabled (&T).
	bool	mbFullDuplex;					/// (S15 bit 3). Half/full duplex (F). SX212 only.
	bool	mbOriginateMode;				/// True if we are originating (ATD/ATO); false if we are in answer mode (ATA/ATDR).
	ATModemFlowControl	mFlowControlMode;	/// (S39). Default = 3 (RTS/CTS)
};


class ATModemEmulator final : public ATDevice
					, public IATRS232Device
					, public IATModemDriverCallback
					, public IATSchedulerCallback
					, public IATDeviceScheduling
					, public IATDeviceIndicators
					, public IATDeviceSerial
{
	ATModemEmulator(const ATModemEmulator&) = delete;
	ATModemEmulator& operator=(const ATModemEmulator&) = delete;
public:
	ATModemEmulator();

public:
	virtual int AddRef() override;
	virtual int Release() override;
	virtual void *AsInterface(uint32 iid) override;

public:
	virtual void GetDeviceInfo(ATDeviceInfo& info) override;
	virtual void GetSettings(ATPropertySet& settings) override;
	virtual bool SetSettings(const ATPropertySet& settings) override;
	virtual void Init() override;
	virtual void Shutdown() override;
	virtual void WarmReset() override;
	virtual void ColdReset() override;

public:
	virtual void InitScheduling(ATScheduler *sch, ATScheduler *slowsch) override;

public:
	virtual void InitIndicators(IATDeviceIndicatorManager *r) override;

public:
	virtual void SetOnStatusChange(const vdfunction<void(const ATDeviceSerialStatus&)>& fn) override;
	virtual void SetTerminalState(const ATDeviceSerialTerminalState&) override;
	virtual ATDeviceSerialStatus GetStatus() override;

public:
	void Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATAudioMixer *mixer);

	void SetConfig(const ATRS232Config& config) override;

	bool Read(uint32& baudRate, uint8& c) override;
	bool Read(uint32 baudRate, uint8& c, bool& framingError) override;
	void Write(uint32 baudRate, uint8 c) override;

	void Set1030Mode();
	void SetSX212Mode();
	void SetToneDialingMode(bool enable) override;
	bool IsToneDialingMode() const override;
	void HangUp() override;
	void Dial(const char *address, const char *service, const char *desc = nullptr) override;
	void Answer() override;

	void FlushBuffers() override;

protected:
	~ATModemEmulator();

	void Poll();
	void ParseCommand();

	enum : uint32 {
		kEventId_ConnectionStateMachine = 5
	};

	enum {
		// These must match the values for the short responses.
		kResponseOK				= 0,
		kResponseConnect		= 1,
		kResponseRing			= 2,
		kResponseNoCarrier		= 3,
		kResponseError			= 4,
		kResponseConnect1200	= 5,
		kResponseNoDialtone		= 6,
		kResponseBusy			= 7,
		kResponseNoAnswer		= 8,
		kResponseConnect600		= 9,
		kResponseConnect2400	= 10,
		kResponseConnect4800	= 11,
		kResponseConnect9600	= 12,
		kResponseConnect7200	= 13,
		kResponseConnect12000	= 14,
		kResponseConnect14400	= 15,
		kResponseConnect19200	= 16,
		kResponseConnect38400	= 17,
		kResponseConnect57600	= 18,
		kResponseConnect115200	= 19,
		kResponseConnect230400	= 20
	};

	void SendConnectResponse();
	void SendResponse(int response);
	void SendResponse(const char *s);
	void SendResponseF(const char *format, ...);
	void ReportRegisters(const ATModemRegisters& reg, bool stored);

	void TerminateCall();
	void RestoreListeningState();
	void UpdateConfig();
	void UpdateControlState();
	void UpdateUIStatus();
	void EnterCommandMode(bool sendPrompt, bool force);

	int GetRegisterValue(uint32 reg) const;
	bool SetRegisterValue(uint32 reg, uint8 value);
	void UpdateDerivedRegisters();
	void ConnectPhoneLine();

	void AdvanceConnectionStateMachine();
	void SetConnectRate();
	void SetRemoteNameFromAddress();

	void OnScheduledEvent(uint32 id) override;
	void OnReadAvail(IATModemDriver *sender, uint32 len) override;
	void OnWriteAvail(IATModemDriver *sender) override;
	void OnEvent(IATModemDriver *sender, ATModemPhase phase, ATModemEvent event) override;

	ATScheduler *mpScheduler;
	ATScheduler *mpSlowScheduler;
	IATDeviceIndicatorManager *mpUIRenderer;
	IATModemDriver *mpDriver;
	vdfunction<void(const ATDeviceSerialStatus&)> mpCB;
	ATEvent *mpEventEnterCommandMode = nullptr;
	ATEvent *mpEventCommandModeTimeout = nullptr;
	ATEvent *mpEventCommandTermDelay = nullptr;
	ATEvent *mpEventPoll = nullptr;
	ATEvent *mpEventConnectionStateMachine = nullptr;
	uint8	mGuardCharCounter;
	uint8	mLastRegister;
	bool	mbCommandMode;

	enum ConnectionState {
		kConnectionState_NotConnected,
		kConnectionState_Connecting,
		kConnectionState_Connected,
		kConnectionState_LostCarrier,
		kConnectionState_Dialing,
		kConnectionState_Ringing,
		kConnectionState_Handshaking,
	} mConnectionState = kConnectionState_NotConnected;

	uint32	mConnectionSubState = 0;

	uint32	mConnectStartTime = 0;
	uint32	mLostCarrierTime;
	uint32	mLostCarrierDelayCycles;

	bool	mbIncomingConnection;
	bool	mbSuppressNoCarrier;
	bool	mbListenEnabled;				///< True if listening for incoming calls is enabled (not necessarily right now).
	bool	mbListening;					///< True if we are currently listening for a call (not dialing or connected).
	bool	mbLoggingState;
	bool	mbHighSpeed;
	VDAtomicBool	mbRinging = false;

	enum CommandState {
		kCommandState_Idle,
		kCommandState_A,
		kCommandState_AT,
		kCommandState_Dialing
	} mCommandState;

	ATModemRegisters	mRegisters;
	ATModemRegisters	mSavedRegisters;

	VDAtomicInt mbNewConnectedState;
	VDAtomicInt mbConnectionFailed;
	uint32	mLastWriteTime;
	uint32	mLastRingTime;
	uint32	mConnectRate;
	uint32	mCommandRate;

	VDStringA	mAddress;
	VDStringA	mService;
	VDStringA	mRemoteName;
	VDStringA	mDialString;

	ATRS232Config	mConfig;
	ATDeviceSerialStatus	mControlState {};
	ATDeviceSerialTerminalState mTerminalState {};

	ATModemSoundEngine *mpModemSound {};

	uint32	mTransmitIndex;
	uint32	mTransmitLength;
	uint8	mTransmitBuffer[512];

	VDStringA	mLastCommand;
	uint8	mCommandBuffer[128];
	uint32	mCommandLength;

	VDCriticalSection mMutex;
	uint32	mDeviceTransmitReadOffset;
	uint32	mDeviceTransmitWriteOffset;
	uint32	mDeviceTransmitLevel;
	bool	mbDeviceTransmitUnderflow;
	uint8	mDeviceTransmitBuffer[4096];
};

#endif
