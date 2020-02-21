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
#include <at/atcore/propertyset.h>
#include <at/atcore/deviceserial.h>
#include <at/atcore/enumparseimpl.h>
#include <at/atcore/scheduler.h>
#include <at/atcore/wraptime.h>
#include <at/atdevices/modemsound.h>
#include "modem.h"
#include "uirender.h"
#include "console.h"
#include "debuggerlog.h"

ATDebuggerLogChannel g_ATLCModem(false, false, "MODEM", "Modem activity");
ATDebuggerLogChannel g_ATLCModemTCP(false, false, "MODEMTCP", "Modem TCP/IP activity");

AT_DEFINE_ENUM_TABLE_BEGIN(ATModemNetworkMode)
	{ ATModemNetworkMode::None, "none" },
	{ ATModemNetworkMode::Minimal, "minimal" },
	{ ATModemNetworkMode::Full, "full" }
AT_DEFINE_ENUM_TABLE_END(ATModemNetworkMode, ATModemNetworkMode::Full)

namespace {
	enum {
		// 1.0s guard time
		kGuardTime50 = 7159090 / 4,
		kCommandTimeout = 7159090 * 30 / 4,

		// 0.125s delay after a command completes
		kCommandTermDelay = 7159090 / 32,

		// two seconds of ringing
		kRingOnTime = 7159090 * 2 / 4,

		// four seconds of not ringing
		kRingOffTime = 7159090 * 4 / 4,

		// two seconds from dial/answer to CONNECT (skipped if dial flow is active)
		kConnectTime = 7159090/2
	};
}

void ATCreateDeviceModem(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATModemEmulator> p(new ATModemEmulator);

	p->SetSettings(pset);

	*dev = p;
	(*dev)->AddRef();
}

extern const ATDeviceDefinition g_ATDeviceDefModem = { "modem", "modem", L"Modem", ATCreateDeviceModem };

ATModemRegisters::ATModemRegisters()
	: mAutoAnswerRings(0)
	, mEscapeChar('+')
	, mLineTermChar(0x0D)
	, mRespFormatChar(0x0A)
	, mCommandEditChar(0x08)
	, mDialToneWaitTime(2)
	, mDialCarrierWaitTime(50)
	, mDialPauseTime(2)
	, mLostCarrierWaitTime(14)
	, mDTMFToneDuration(95)
	, mEscapePromptDelay(50)
	, mbReportCarrier(true)
	, mDTRMode(2)
	, mbEchoMode(true)
	, mbQuietMode(false)
	, mbToneDialMode(true)
	, mbShortResponses(false)
	, mExtendedResultCodes(4)
	, mSpeakerVolume(0)
	, mSpeakerMode(0)
	, mGuardToneMode(0)
	, mbLoopbackMode(false)
	, mbFullDuplex(true)
	, mbOriginateMode(false)
	, mFlowControlMode(ATModemFlowControl::None)
{
}

ATModemEmulator::ATModemEmulator()
	: mpScheduler(NULL)
	, mpSlowScheduler(NULL)
	, mpUIRenderer(nullptr)
	, mpDriver(NULL)
	, mbCommandMode(false)
	, mbListenEnabled(false)
	, mbListening(false)
	, mbLoggingState(false)
	, mbRinging(false)
	, mCommandState(kCommandState_Idle)
	, mLostCarrierDelayCycles(0)
	, mCommandRate(9600)
{
	mpModemSound = new ATModemSoundEngine;
}

ATModemEmulator::~ATModemEmulator() {
	Shutdown();
}

int ATModemEmulator::AddRef() {
	return ATDevice::AddRef();
}

int ATModemEmulator::Release() {
	return ATDevice::Release();
}

void *ATModemEmulator::AsInterface(uint32 iid) {
	switch(iid) {
		case IATDevice::kTypeID:
			return static_cast<IATDevice *>(this);

		case IATDeviceScheduling::kTypeID:
			return static_cast<IATDeviceScheduling *>(this);

		case IATDeviceIndicators::kTypeID:
			return static_cast<IATDeviceIndicators *>(this);

		case IATDeviceSerial::kTypeID:
			return static_cast<IATDeviceSerial *>(this);

		case IATDeviceAudioOutput::kTypeID:
			return static_cast<IATDeviceAudioOutput *>(mpModemSound);

		case IATRS232Device::kTypeID:
			return static_cast<IATRS232Device *>(this);

		default:
			return ATDevice::AsInterface(iid);
	}
}

void ATModemEmulator::GetDeviceInfo(ATDeviceInfo& info) {
	info.mpDef = &g_ATDeviceDefModem;
}

void ATModemEmulator::GetSettings(ATPropertySet& settings) {
	if (mConfig.mListenPort)
		settings.SetUint32("port", mConfig.mListenPort);

	settings.SetBool("outbound", mConfig.mbAllowOutbound);

	if (!mConfig.mTelnetTermType.empty())
		settings.SetString("termtype", VDTextAToW(mConfig.mTelnetTermType).c_str());

	settings.SetBool("telnet", mConfig.mbTelnetEmulation);
	settings.SetBool("telnetlf", mConfig.mbTelnetLFConversion);
	settings.SetBool("ipv6", mConfig.mbListenForIPv6);
	settings.SetBool("unthrottled", mConfig.mbDisableThrottling);

	if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
		if (mConfig.mConnectionSpeed > 300)
			settings.SetUint32("connect_rate", 1200);
		else
			settings.SetUint32("connect_rate", 300);
	} else if (mConfig.mDeviceMode != kATRS232DeviceMode_1030) {
		settings.SetBool("check_rate", mConfig.mbRequireMatchedDTERate);
		settings.SetUint32("connect_rate", mConfig.mConnectionSpeed);
	}

	if (!mConfig.mDialAddress.empty())
		settings.SetString("dialaddr", VDTextAToW(mConfig.mDialAddress).c_str());

	if (!mConfig.mDialService.empty())
		settings.SetString("dialsvc", VDTextAToW(mConfig.mDialService).c_str());

	settings.SetString("netmode", VDTextAToW(ATEnumToString(mConfig.mNetworkMode)).c_str());
}

bool ATModemEmulator::SetSettings(const ATPropertySet& settings) {
	mConfig.mListenPort = settings.GetUint32("port");
	mConfig.mbAllowOutbound = settings.GetBool("outbound", true);
	mConfig.mTelnetTermType = VDTextWToA(settings.GetString("termtype", L""));
	mConfig.mbTelnetEmulation = settings.GetBool("telnet", true);
	mConfig.mbTelnetLFConversion = settings.GetBool("telnetlf", true);
	mConfig.mbListenForIPv6 = settings.GetBool("ipv6", true);
	mConfig.mbDisableThrottling = settings.GetBool("unthrottled", false);
	mConfig.mbRequireMatchedDTERate = settings.GetBool("check_rate", false);
	mConfig.mConnectionSpeed = settings.GetUint32("connect_rate", 9600);

	// enforce civilized speeds (0 is particularly not good)
	if (mConfig.mConnectionSpeed < 300)
		mConfig.mConnectionSpeed = 300;
	else if (mConfig.mConnectionSpeed > 230400)
		mConfig.mConnectionSpeed = 230400;

	mConfig.mDialAddress = VDTextWToA(settings.GetString("dialaddr", L""));
	mConfig.mDialService = VDTextWToA(settings.GetString("dialsvc", L""));
	mConfig.mNetworkMode = ATParseEnum<ATModemNetworkMode>(VDTextWToA(settings.GetString("netmode", L"full"))).mValue;

	UpdateConfig();
	return true;
}

void ATModemEmulator::WarmReset() {
}

void ATModemEmulator::InitScheduling(ATScheduler *sched, ATScheduler *slowsched) {
	mpScheduler = sched;
	mpSlowScheduler = slowsched;
}

void ATModemEmulator::InitIndicators(IATDeviceIndicatorManager *r) {
	mpUIRenderer = r;
}

void ATModemEmulator::Init(ATScheduler *sched, ATScheduler *slowsched, IATDeviceIndicatorManager *uir, IATAudioMixer *mixer) {
	InitScheduling(sched, slowsched);
	InitIndicators(uir);
	mpModemSound->InitAudioOutput(mixer);
	Init();
}

void ATModemEmulator::Init() {
	mLastWriteTime = ATSCHEDULER_GETTIME(mpScheduler);
	mTransmitIndex = 0;
	mTransmitLength = 0;

	mDeviceTransmitReadOffset = 0;
	mDeviceTransmitWriteOffset = 0;
	mDeviceTransmitLevel = 0;
	mbDeviceTransmitUnderflow = true;

	mCommandLength = 0;
	mbCommandMode = true;
	mConnectionState = kConnectionState_NotConnected;
	mbListening = false;
	mbIncomingConnection = false;
	mbSuppressNoCarrier = false;
	mbConnectionFailed = false;
	mbNewConnectedState = 0;
	mConnectRate = 0;

	mRegisters = mSavedRegisters;
	UpdateDerivedRegisters();

	mpEventPoll = mpSlowScheduler->AddEvent(100, this, 3);

	UpdateControlState();
	RestoreListeningState();
}

void ATModemEmulator::Shutdown() {
	mpCB = nullptr;

	TerminateCall();

	if (mpModemSound) {
		mpModemSound->Shutdown();
		delete mpModemSound;
		mpModemSound = nullptr;
	}

	if (mpSlowScheduler) {
		if (mpEventPoll) {
			mpSlowScheduler->RemoveEvent(mpEventPoll);
			mpEventPoll = NULL;
		}

		mpSlowScheduler = NULL;
	}

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpEventCommandTermDelay);
		mpScheduler->UnsetEvent(mpEventEnterCommandMode);
		mpScheduler->UnsetEvent(mpEventCommandModeTimeout);
		mpScheduler->UnsetEvent(mpEventConnectionStateMachine);

		mpScheduler = NULL;
	}

	if (mpUIRenderer) {
		mpUIRenderer->SetModemConnection(NULL);
		mpUIRenderer = NULL;
	}
}

void ATModemEmulator::ColdReset() {
	mpScheduler->UnsetEvent(mpEventCommandTermDelay);
	mpScheduler->UnsetEvent(mpEventEnterCommandMode);
	mpScheduler->UnsetEvent(mpEventCommandModeTimeout);
	mpScheduler->UnsetEvent(mpEventConnectionStateMachine);

	TerminateCall();
	mpModemSound->Reset();

	mCommandRate = 9600;

	mControlState.mbHighSpeed = true;
	mControlState.mbRinging = false;

	mSavedRegisters = ATModemRegisters();
	mRegisters = mSavedRegisters;
	UpdateDerivedRegisters();
	UpdateControlState();

	RestoreListeningState();
	EnterCommandMode(false, true);
}

void ATModemEmulator::SetOnStatusChange(const vdfunction<void(const ATDeviceSerialStatus&)>& fn) {
	mpCB = fn;
}

void ATModemEmulator::SetTerminalState(const ATDeviceSerialTerminalState& state) {
	const bool dtrdrop = mTerminalState.mbDataTerminalReady && !state.mbDataTerminalReady;

	mTerminalState = state;

	if (dtrdrop) {
		switch(mRegisters.mDTRMode) {
			default:	// 0 - ignore DTR
				break;

			case 1:		// 1 - drop to command mode on DTR drop
				EnterCommandMode(true, false);
				break;

			case 2:		// 2 - hang up on DTR drop; don't auto-answer
				if (mConnectionState) {
					mbSuppressNoCarrier = true;

					TerminateCall();
					RestoreListeningState();
					EnterCommandMode(true, false);
				}
				break;
		}
	}
}

ATDeviceSerialStatus ATModemEmulator::GetStatus() {
	return mControlState;
}

void ATModemEmulator::SetConfig(const ATRS232Config& config) {
	mConfig = config;
	UpdateConfig();
}

void ATModemEmulator::UpdateConfig() {
	if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
		mConfig.mbRequireMatchedDTERate = true;
		mConfig.mConnectionSpeed = (mConfig.mConnectionSpeed > 300) ? 1200 : 300;
	} else if (mConfig.mDeviceMode == kATRS232DeviceMode_1030) {
		mConfig.mbRequireMatchedDTERate = true;
		mConfig.mConnectionSpeed = 300;
	}

	mbListenEnabled = (mConfig.mListenPort != 0);

	if (mbListening && !mbListenEnabled)
		TerminateCall();
	else if (!mbListening && mbListenEnabled && !mConnectionState)
		RestoreListeningState();

	if (mpDriver)
		mpDriver->SetConfig(mConfig);

	UpdateUIStatus();
}

bool ATModemEmulator::Read(uint32& baudRate, uint8& c) {
	bool loggingState = g_ATLCModemTCP.IsEnabled();
	if (mbLoggingState != loggingState) {
		mbLoggingState = loggingState;

		if (mpDriver)
			mpDriver->SetLoggingEnabled(loggingState);
	}

	// if we have RTS/CTS enabled and RTS is negated, don't send data
	if (mRegisters.mFlowControlMode == ATModemFlowControl::RTS_CTS) {
		if (!mTerminalState.mbRequestToSend)
			return false;
	}

	if (mTransmitIndex < mTransmitLength) {
		c = mTransmitBuffer[mTransmitIndex++];

		if (mTransmitIndex >= mTransmitLength) {
			mTransmitIndex = 0;
			mTransmitLength = 0;
		}

		baudRate = mCommandRate;
		return true;
	}

	if (mbCommandMode || mConnectionState != kConnectionState_Connected)
		return false;

	if (!mpDriver)
		return false;

	if (!mpDriver->Read(&c, 1))
		return false;

	VDStringA msgs;
	if (mpDriver->ReadLogMessages(msgs)) {
		VDStringA::size_type pos = 0;

		for(;;) {
			VDStringA::size_type term = msgs.find('\n', pos);

			if (term == VDStringA::npos)
				break;

			g_ATLCModemTCP("%.*s\n", term - pos, msgs.c_str() + pos);

			pos = term + 1;
		}
	}

	baudRate = mConnectRate;
	return true;
}

bool ATModemEmulator::Read(uint32 baudRate, uint8& c, bool& framingError) {
	framingError = false;

	uint32 transmitRate;
	if (!Read(transmitRate, c))
		return false;

	// check for more than a 5% discrepancy in baud rates between modem and serial port
	if (mConfig.mbRequireMatchedDTERate && abs((int)baudRate - (int)transmitRate) * 20 > (int)transmitRate) {
		// baud rate mismatch -- return some bogus character and flag a framing error
		c = 'U';
		framingError = true;
	}

	return true;
}

void ATModemEmulator::Write(uint32 baudRate, uint8 c) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	uint32 delay = t - mLastWriteTime;
	mLastWriteTime = t;
	mCommandRate = baudRate;

	if (mpEventCommandTermDelay)
		return;

	if (mbCommandMode) {
		c &= 0x7F;

		if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
			const bool highSpeed = baudRate > 600;

			if (mControlState.mbHighSpeed != highSpeed) {
				mControlState.mbHighSpeed = highSpeed;

				g_ATLCModem("Autoswitching to %u baud\n", highSpeed ? 1200 : 300);

				if (mpCB)
					mpCB(mControlState);
			}
		}

		switch(mCommandState) {
			case kCommandState_Idle:
				if (mConfig.mDeviceMode == kATRS232DeviceMode_1030)
					break;

				if ((c & 0xdf) == 'A')
					mCommandState = kCommandState_A;
				break;

			case kCommandState_A:
				if ((c & 0xdf) == 'T')
					mCommandState = kCommandState_AT;
				else if (c == '/') {
					mCommandState = kCommandState_AT;
					mCommandLength = mLastCommand.size();
					mLastCommand.copy((char *)mCommandBuffer, mCommandLength);

					mpScheduler->SetEvent(kCommandTermDelay, this, 4, mpEventCommandTermDelay);
				} else
					mCommandState = kCommandState_Idle;
				break;

			case kCommandState_AT:
				if (c == mRegisters.mLineTermChar) {
					mpScheduler->SetEvent(kCommandTermDelay, this, 4, mpEventCommandTermDelay);
				} else if (c == mRegisters.mCommandEditChar) {
					// If the command buffer is empty, just dump the edit char. We don't even
					// echo it.
					if (!mCommandLength)
						return;

					--mCommandLength;

					// Conexant/3-79 [S5 - Command Line Editing Character] says this character
					// is echoed as edit, space, edit. Note that we'll be writing another edit char below.
					if (mRegisters.mbEchoMode) {
						if (mTransmitLength + 2 < sizeof mTransmitBuffer) {
							mTransmitBuffer[mTransmitLength++] = c;
							mTransmitBuffer[mTransmitLength++] = ' ';
						}
					}

					mpScheduler->SetEvent(kCommandTimeout, this, 2, mpEventCommandModeTimeout);
				} else {
					if (mCommandLength < sizeof mCommandBuffer)
						mCommandBuffer[mCommandLength++] = c;

					mpScheduler->SetEvent(kCommandTimeout, this, 2, mpEventCommandModeTimeout);
				}
				break;

			case kCommandState_Dialing:
				HangUp();
				SendResponse(kResponseOK);

				// suppress echo
				return;
		}

		if (mRegisters.mbEchoMode) {
			if (mTransmitLength < sizeof mTransmitBuffer)
				mTransmitBuffer[mTransmitLength++] = c;
		}
	} else {
		if ((c & 0x7f) == mRegisters.mEscapeChar && mConfig.mDeviceMode != kATRS232DeviceMode_1030) {
			const uint32 guardTime = (kGuardTime50 * mRegisters.mEscapePromptDelay) / 50;

			if (delay >= guardTime || mGuardCharCounter) {
				if (++mGuardCharCounter >= 3)
					mpScheduler->SetEvent(guardTime, this, 1, mpEventEnterCommandMode);
			}
		} else {
			mGuardCharCounter = 0;

			if (mpEventEnterCommandMode) {
				mpScheduler->RemoveEvent(mpEventEnterCommandMode);
				mpEventEnterCommandMode = NULL;
			}
		}

		if (mConfig.mbRequireMatchedDTERate && abs((int)baudRate - (int)mConnectRate) > 1) {
			// baud rate mismatch -- send some bogus character
			c = 'U';
		}

		mMutex.Lock();
		if (mDeviceTransmitLevel < sizeof mDeviceTransmitBuffer) {
			mDeviceTransmitBuffer[mDeviceTransmitWriteOffset] = c;

			if (++mDeviceTransmitWriteOffset >= sizeof mDeviceTransmitBuffer)
				mDeviceTransmitWriteOffset = 0;

			++mDeviceTransmitLevel;

			if (mbDeviceTransmitUnderflow) {
				if (mpDriver)
					OnWriteAvail(mpDriver);
			}
		}
		mMutex.Unlock();
	}
}

void ATModemEmulator::Set1030Mode() {
	mConfig.mDeviceMode = kATRS232DeviceMode_1030;
	mRegisters.mbReportCarrier = true;
}

void ATModemEmulator::SetSX212Mode() {
	mConfig.mDeviceMode = kATRS232DeviceMode_SX212;
	mRegisters.mbReportCarrier = true;
	mRegisters.mFlowControlMode = ATModemFlowControl::None;
	mTerminalState.mbDataTerminalReady = true;
	mTerminalState.mbRequestToSend = true;
}

void ATModemEmulator::SetToneDialingMode(bool enable) {
	mRegisters.mbToneDialMode = enable;
}

bool ATModemEmulator::IsToneDialingMode() const {
	return mRegisters.mbToneDialMode;
}

void ATModemEmulator::HangUp() {
	if (mCommandState == kCommandState_Dialing) {
		mCommandState = kCommandState_Idle;
	}
	
	mpModemSound->Reset();

	mbSuppressNoCarrier = true;

	TerminateCall();

	mbConnectionFailed = false;
	RestoreListeningState();
}

void ATModemEmulator::Dial(const char *address, const char *service, const char *desc) {
	TerminateCall();

	mbConnectionFailed = false;

	mAddress = address;
	mService = service;

	if (desc) {
		mRemoteName = desc;
	} else {
		SetRemoteNameFromAddress();
	}

	mCommandState = kCommandState_Dialing;
	mConnectionState = kConnectionState_Dialing;
	mConnectionSubState = 0;
	mpScheduler->SetEvent(1, this, kEventId_ConnectionStateMachine, mpEventConnectionStateMachine);

	UpdateUIStatus();
}

void ATModemEmulator::Answer() {
	// It's an error if ATA is issued while in online state (Table 9/V.250).
	if (mConnectionState) {
		SendResponse(kResponseError);
		return;
	}
	
	// If we have a connection incoming, then we're already virtually connected
	// and should "answer" the connection. The connection state switching code
	// will issue the CONNECT banner.
	if (mbIncomingConnection) {
		mbListening = false;
		mRegisters.mbOriginateMode = false;
		mConnectionState = kConnectionState_Handshaking;
		mConnectionSubState = 0;
		SetConnectRate();
		AdvanceConnectionStateMachine();
		return;
	}

	// No incoming connection, so report NO CARRIER.
	SendResponse(kResponseNoCarrier);
}

void ATModemEmulator::FlushBuffers() {
	mTransmitIndex = 0;
	mTransmitLength = 0;
}

void ATModemEmulator::Poll() {
	if (mpEventCommandTermDelay)
		return;

	if (mbConnectionFailed) {
		mbConnectionFailed = false;

		UpdateUIStatus();

		HangUp();
		SendResponse(kResponseNoAnswer);

		EnterCommandMode(false, false);

		RestoreListeningState();
	} else {
		bool nowConnected = (mbNewConnectedState != 0);

		if (mbListening) {
			if (mbIncomingConnection != nowConnected) {
				mbIncomingConnection = nowConnected;

				if (nowConnected) {
					mLastRingTime = ATSCHEDULER_GETTIME(mpScheduler) - kRingOnTime;
					mbRinging = true;

					uint32 port;
					if (mpDriver->GetLastIncomingAddress(mAddress, port)) {
						mService.sprintf("%u", port);
					} else {
						mAddress = "<Unknown>";
						mService.clear();
					}

					SetRemoteNameFromAddress();
					
					UpdateUIStatus();
					UpdateControlState();
				} else {
					mbRinging = false;
					UpdateUIStatus();

					if (!mbCommandMode) {
						HangUp();

						if (mbSuppressNoCarrier)
							mbSuppressNoCarrier = false;
						else
							SendResponse(kResponseNoCarrier);

						EnterCommandMode(false, false);
					}

					RestoreListeningState();
					UpdateControlState();
				}
			}

			if (mbIncomingConnection) {
				VDASSERT(mbCommandMode);

				// If DTR is low in AT&D2 mode, we should not auto-answer (level triggered).
				if (mRegisters.mAutoAnswerRings && (mRegisters.mDTRMode != 2 || mTerminalState.mbDataTerminalReady)) {
					Answer();
					// we will fall through to connection code next
				} else if (mCommandLength == 0) {
					uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

					if (t - mLastRingTime > (mbRinging ? (uint32)kRingOnTime : (uint32)kRingOffTime)) {
						mLastRingTime = t;

						if (mbRinging)
							SendResponse(kResponseRing);

						mbRinging = !mbRinging;
						UpdateControlState();
					}
				}
			}
		}
		
		// this is a separate clause so we can fall through on the same tick for auto-answer
		// above
		if (!mbListening) {
			if (mbRinging) {
				mbRinging = false;
				UpdateControlState();
			}

			switch(mConnectionState) {
				case kConnectionState_NotConnected:
					if (nowConnected) {
						SetConnectRate();
						mConnectionState = kConnectionState_Connecting;
						mConnectionSubState = 0;
						mConnectStartTime = ATSCHEDULER_GETTIME(mpScheduler);
						mbCommandMode = false;
						UpdateControlState();
						UpdateUIStatus();
						AdvanceConnectionStateMachine();
					}
					break;

				case kConnectionState_Ringing:
					if (nowConnected) {
						SetConnectRate();
						mConnectionState = kConnectionState_Handshaking;
						mConnectionSubState = 0;
						UpdateUIStatus();
						AdvanceConnectionStateMachine();
					}
					break;

				case kConnectionState_Connecting:
					if (!nowConnected) {
						goto no_carrier;
					} else if (ATWrapTime{ATSCHEDULER_GETTIME(mpScheduler)} > mConnectStartTime + kConnectTime) {
						mConnectionState = kConnectionState_Connected;

						SendConnectResponse();

						mbSuppressNoCarrier = false;

						UpdateControlState();
						UpdateUIStatus();
					}
					break;

				case kConnectionState_Connected:
					if (!nowConnected) {
						mConnectionState = kConnectionState_LostCarrier;
						mLostCarrierTime = ATSCHEDULER_GETTIME(mpScheduler);

						UpdateControlState();
						UpdateUIStatus();
					}
					break;

				case kConnectionState_LostCarrier:
					if (nowConnected) {
						mConnectionState = kConnectionState_Connected;
						UpdateControlState();
					} else if (mLostCarrierDelayCycles && ATSCHEDULER_GETTIME(mpScheduler) - mLostCarrierTime > mLostCarrierDelayCycles) {
						TerminateCall();

no_carrier:
						mConnectionState = kConnectionState_NotConnected;

						UpdateUIStatus();

						if (mbSuppressNoCarrier)
							mbSuppressNoCarrier = false;
						else
							SendResponse(kResponseNoCarrier);

						EnterCommandMode(false, false);

						if (mbListenEnabled)
							RestoreListeningState();

						UpdateControlState();
					}
					break;

			}
		}
	}
}

void ATModemEmulator::ParseCommand() {
	if (mpEventCommandModeTimeout) {
		mpScheduler->RemoveEvent(mpEventCommandModeTimeout);
		mpEventCommandModeTimeout = NULL;
	}

	uint32 len = mCommandLength;
	mCommandLength = 0;
	mCommandState = kCommandState_Idle;

	if (!len) {
		SendResponse(kResponseOK);
		return;
	}

	// store last command for A/; note that this happens even if there is an error,
	// in which case A/ also replays the error
	mLastCommand.assign((const char *)mCommandBuffer, (const char *)mCommandBuffer + len);

	g_ATLCModem("Executing command: [%s]\n", mLastCommand.c_str());

	const char *s = (const char *)mCommandBuffer;
	const char *t = (const char *)mCommandBuffer + len;

	for(;;) {
		// eat whitespace
		uint8 c;
		do {
			if (s == t) {
				SendResponse(kResponseOK);
				return;
			}

			c = *s++;
		} while(c == ' ');

		// capture command
		uint8 cmd = c;
		uint8 extcmd = 0;

		if (cmd == '&') {
			if (s != t) {
				extcmd = *s++;
				if ((uint8)(extcmd - 'a') < 26)
					extcmd &= ~0x20;
			}
		} else if ((uint8)(cmd - 'a') < 26)
			cmd &= ~0x20;

		// eat more whitespace
		while(s != t && *s == ' ')
			++s;

		// capture a number
		bool hasNumber = false;
		uint32 number = 0;

		if (cmd != 'D') {
			if (s != t && (uint8)(*s - (uint8)'0') < 10) {
				hasNumber = true;

				do {
					number *= 10;
					number += (*s - '0');

					++s;
					if (s == t)
						break;

					c = *s;
				} while((uint8)(*s - (uint8)'0') < 10);
			}
		}

		// Commands supported by the SX212:
		//	ATA		Set answer mode
		//	ATB		Set Bell modulation mode
		//	ATC		Set transmit carrier
		//	ATD		Dial
		//	ATE		Set echo
		//	ATF		Set full duplex
		//	ATH		Set on/off hook
		//	ATI		Information
		//	ATL		Speaker loudness
		//	ATM		Speaker mode
		//	ATO		Set originate mode
		//	ATP		Set pulse dial mode
		//	ATQ		Set quiet mode
		//	ATR		Set reverse mode
		//	ATS		Set or query register
		//	ATT		Set touch dialing
		//	ATV		Set verbose reporting
		//	ATX		Set connect/busy/dialtone reporting
		//	ATY		Set long space disconnect enable
		//	ATZ		Reset modem
		switch(cmd) {
			case '&':	// extended command
				if (mConfig.mDeviceMode != kATRS232DeviceMode_850) {
					SendResponse(kResponseError);
					return;
				} else switch(extcmd) {
					case 'C':	// &C - RLSD behavior
						if (!hasNumber)
							number = 0;

						if (number >= 2) {
							SendResponse(kResponseError);
							return;
						}

						mRegisters.mbReportCarrier = (number > 0);
						UpdateControlState();
						break;

					case 'D':	// &D - DTR behavior
						if (!hasNumber)
							number = 2;

						if (number >= 3) {
							SendResponse(kResponseError);
							return;
						}

						mRegisters.mDTRMode = number;
						break;

					case 'F':	// &F - Set to Factory-Defined Configuration
						mRegisters = ATModemRegisters();
						UpdateDerivedRegisters();
						break;

					case 'G':	// &G - Select Guard Tone
						if (!hasNumber)
							number = 0;

						if (number > 2) {
							SendResponse(kResponseError);
							return;
						}

						mRegisters.mGuardToneMode = number;
						break;

					case 'K':	// &K - Flow control
						if (!hasNumber || (number != 0 && number != 3 && number != 4 && number != 5)) {
							SendResponse(kResponseError);
							return;
						}

						mRegisters.mFlowControlMode = (ATModemFlowControl)number;
						break;

					case 'P':	// &P - Select Pulse Dial Make/Break Ratio (validated but ignored)
						if (hasNumber && number > 3) {
							SendResponse(kResponseError);
							return;
						}
						break;

					case 'T':	// &T - Local Analog Loopback Test
						if (!hasNumber)
							number = 0;

						mRegisters.mbLoopbackMode = (number != 0);
						break;

					case 'V':	// &V - Display Current Configuration and Stored Profile
						SendResponse("");
						SendResponse("ACTIVE PROFILE:");
						ReportRegisters(mRegisters, false);
						SendResponse("");
						SendResponse("STORED PROFILE 0:");
						ReportRegisters(mSavedRegisters, true);
						break;

					case 'W':	// &W - Store Current Configuration
						mSavedRegisters = mRegisters;
						break;

					default:
						SendResponse(kResponseError);
						return;
				}
				break;

			case 'A':	// answer
				Answer();
				return;

			case 'B':	// select communication standard (ignored)
				break;

			case 'D':
				// eat more whitespace
				while(s != t && *s == ' ')
					++s;

				if (s == t) {
					SendResponse(kResponseError);
					return;
				}

				// check if we are in online command state -- err out if so
				if (mConnectionState) {
					SendResponse(kResponseError);
					return;
				}

				// check if we allow outbound connections -- if not, return no dialtone
				if (!mConfig.mbAllowOutbound) {
					SendResponse(kResponseNoDialtone);
					return;
				}

				mRegisters.mbOriginateMode = true;

				// check for 'I' to indicate IP-based dialing (for compatibility with Atari800)
				if (*s == 'I' || *s == 'i') {
					// parse out hostname and port
					++s;

					while(s != t && *s == ' ')
						++s;

					const char *hostname = s;

					while(s != t && *s != ' ')
						++s;

					const char *hostnameend = s;

					while(s != t && *s == ' ')
						++s;

					const char *servicename = "23";
					const char *servicenameend = servicename + 2;

					if (s != t) {
						servicename = s;

						while(s != t && *s != ' ')
							++s;

						servicenameend = s;
					}

					// Check if either the hostname or service have invalid characters or if
					// the address is empty and reject the connection if so.
					const auto invalidChar = [](char c) { return (((unsigned char)c - 0x20) & 0xFF) >= 0x5F; };
					if (hostname == hostnameend || std::any_of(hostname, hostnameend, invalidChar) || std::any_of(servicename, servicenameend, invalidChar)) {
						SendResponse(kResponseNoAnswer);
						return;
					}

					// 555-01xx numbers are reserved for fictional telephone numbers in North
					// America.
					mDialString = "3210555";

					Dial(VDStringA(hostname, hostnameend).c_str(), VDStringA(servicename, servicenameend).c_str());
				} else {
					mDialString.assign(s, t);
					std::reverse(mDialString.begin(), mDialString.end());

					if (!mConfig.mDialAddress.empty() && !mConfig.mDialService.empty())
						Dial(mConfig.mDialAddress.c_str(), mConfig.mDialService.c_str());
					else {
						VDStringA desc(s, t);

						desc.erase(std::remove_if(desc.begin(), desc.end(), [](char c) { return c < 0x20 || c >= 0x7f; }), desc.end());

						Dial("", "", desc.c_str());
					}
				}
				return;

			case 'E':	// echo (number)
				if (!hasNumber || number >= 2) {
					SendResponse(kResponseError);
					return;
				}

				mRegisters.mbEchoMode = (number != 0);
				break;

			case 'F':	// full duplex (SX212)
				if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
					mRegisters.mbFullDuplex = (number > 0);
				} else {
					SendResponse(kResponseError);
					return;
				}
				break;

			case 'H':	// hook control (optional number)
				if (hasNumber && number >= 2) {
					SendResponse(kResponseError);
					return;
				}

				HangUp();
				break;

			case 'I':	// version (SX212)
				if (mTransmitIndex <= sizeof mTransmitBuffer - 7) {
					if (!mRegisters.mbShortResponses) {
						mTransmitBuffer[mTransmitLength++] = mRegisters.mLineTermChar;
						mTransmitBuffer[mTransmitLength++] = mRegisters.mRespFormatChar;
					}

					if (number >= 2) {
						int v = number ? 103 : 134;

						mTransmitBuffer[mTransmitLength++] = (uint8)('0' + (v / 100)); v %= 100;
						mTransmitBuffer[mTransmitLength++] = (uint8)('0' + (v / 10)); v %= 10;
						mTransmitBuffer[mTransmitLength++] = (uint8)('0' + v);
					}

					mTransmitBuffer[mTransmitLength++] = mRegisters.mLineTermChar;
					mTransmitBuffer[mTransmitLength++] = mRegisters.mRespFormatChar;
				}
				break;

			case 'L':	// set speaker volume (ignored)
				if (!hasNumber || number >= 4) {
					SendResponse(kResponseError);
					return;
				}
				break;

			case 'M':	// speaker control
				if (!hasNumber || number >= 4) {
					SendResponse(kResponseError);
					return;
				}
				mpModemSound->SetSpeakerEnabled(number > 0);
				break;

			case 'O':	// on-hook
				if (!mConnectionState)
					SendResponse(kResponseError);
				else {
					SendConnectResponse();
					mbCommandMode = false;
					UpdateUIStatus();
				}
				return;

			case 'P':	// change dial mode default to pulse dial
				if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212 && number > 0) {
					SendResponse(kResponseError);
					return;
				}
				mRegisters.mbToneDialMode = false;
				break;

			case 'R':	// reverse connect (SX212)
				if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212 && hasNumber)
					break;

				SendResponse(kResponseError);
				return;

			case 'Q':	// quiet mode
				if (!hasNumber)
					number = 1;

				if (number >= 2) {
					SendResponse(kResponseError);
					return;
				}

				mRegisters.mbQuietMode = (number != 0);
				break;

			case '=':	// set/query register
				if (mConfig.mDeviceMode != kATRS232DeviceMode_850) {
					SendResponse(kResponseError);
					return;
				}
				// fall through
			case 'S':	// set/query register
				{
					bool set = false;
					bool query = false;

					if (cmd == '=')
						query = true;
					else if (s != t) {
						if (*s == '=') {
							++s;

							set = true;
						} else if (*s == '?') {
							++s;

							query = true;
						}
					}

					if (hasNumber) {
						if (GetRegisterValue(number) < 0) {
							SendResponse(kResponseError);
							return;
						}

						mLastRegister = number;
					}

					if (query) {
						int v = GetRegisterValue(mLastRegister);

						if (mTransmitIndex <= sizeof mTransmitBuffer - 7) {
							if (!mRegisters.mbShortResponses) {
								mTransmitBuffer[mTransmitLength++] = mRegisters.mLineTermChar;
								mTransmitBuffer[mTransmitLength++] = mRegisters.mRespFormatChar;
							}

							g_ATLCModem("Returning query: S%02d = %03d\n", mLastRegister, v);

							mTransmitBuffer[mTransmitLength++] = (uint8)('0' + (v / 100)); v %= 100;
							mTransmitBuffer[mTransmitLength++] = (uint8)('0' + (v / 10)); v %= 10;
							mTransmitBuffer[mTransmitLength++] = (uint8)('0' + v);
							mTransmitBuffer[mTransmitLength++] = mRegisters.mLineTermChar;
							mTransmitBuffer[mTransmitLength++] = mRegisters.mRespFormatChar;
						}
					} else if (set) {
						if (s == t && (uint8)(*s - '0') >= 10) {
							SendResponse(kResponseError);
							return;
						}

						uint32 value = (*s++ - '0');

						while(s != t) {
							c = (uint8)(*s - '0');

							if (c >= 10)
								break;

							++s;

							value = (value * 10) + c;
						}

						if (!SetRegisterValue(mLastRegister, value)) {
							SendResponse(kResponseError);
							return;
						}
					}
				}
				break;

			case 'T':	// change dial mode default to tone dial
				mRegisters.mbToneDialMode = true;
				break;

			case 'V':	// verbose mode
				if (hasNumber && number >= 2) {
					SendResponse(kResponseError);
					return;
				}

				mRegisters.mbShortResponses = (number == 0);
				break;

			case 'X':	// extended result code mode
				if (!hasNumber || number >= 5) {
					SendResponse(kResponseError);
					return;
				}

				mRegisters.mExtendedResultCodes = number;
				break;

			case 'Y':
				if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
					// ignore for now
				} else {
					SendResponse(kResponseError);
					return;
				}
				break;

			case 'Z':	// reset modem (number optional)
				TerminateCall();
				mRegisters = mSavedRegisters;
				UpdateDerivedRegisters();
				RestoreListeningState();
				break;

			default:
				SendResponse(kResponseError);
				return;
		}
	}
}

void ATModemEmulator::SendConnectResponse() {
	if (mRegisters.mbQuietMode)
		return;

	const uint32 rate = mConfig.mConnectionSpeed;
	if (!mRegisters.mExtendedResultCodes) {
		SendResponse(kResponseConnect);
		return;
	}

	static const uint32 kBaudRates[]={
		300,
		600,
		1200,
		2400,
		4800,
		7200,
		9600,
		12000,
		14400,
		19200,
		38400,
		57600,
		115200,
		230400,
	};

	static const uint32 kResponseIds[]={
		kResponseConnect,
#define X(rate) kResponseConnect##rate
		X(600),
		X(1200),
		X(2400),
		X(4800),
		X(7200),
		X(9600),
		X(12000),
		X(14400),
		X(19200),
		X(38400),
		X(57600),
		X(115200),
		X(230400),
#undef X
	};

	const uint32 *begin = kBaudRates;
	const uint32 *end = kBaudRates + sizeof(kBaudRates)/sizeof(kBaudRates[0]);
	const uint32 *it = std::lower_bound(begin, end, rate);

	if (it == end)
		--it;

	if (it != begin && (rate - it[-1]) < (it[0] - rate))
		--it;

	SendResponse(kResponseIds[it - begin]);
}

void ATModemEmulator::SendResponse(int response) {
	if (mRegisters.mbQuietMode || mConfig.mDeviceMode == kATRS232DeviceMode_1030)
		return;

	static const char *const kResponses[]={
		"OK",
		"CONNECT",
		"RING",
		"NO CARRIER",
		"ERROR",
		"CONNECT 1200",
		"NO DIALTONE",
		"BUSY",
		"NO ANSWER",
		"CONNECT 600",
		"CONNECT 2400",
		"CONNECT 4800",
		"CONNECT 9600",
		"CONNECT 7200",
		"CONNECT 12000",
		"CONNECT 14400",
		"CONNECT 19200",
		"CONNECT 38400",
		"CONNECT 57600",
		"CONNECT 115200",
		"CONNECT 230400",
	};

	VDASSERT((unsigned)response < sizeof(kResponses)/sizeof(kResponses[0]));

	if (mRegisters.mbShortResponses) {
		g_ATLCModem("Sending short response: %d (%s)\n", response, kResponses[response]);

		size_t len = response >= 10 ? 3 : 2;

		if (mTransmitLength > sizeof mTransmitBuffer - len)
			return;

		uint8 *dst = mTransmitBuffer + mTransmitLength;

		if (response >= 10) {
			*dst++ = (uint8)('0' + response / 10);
			response %= 10;
		}
		*dst++ = (uint8)('0' + response);
		*dst++ = mRegisters.mLineTermChar;

		mTransmitLength += (uint32)len;
	} else {
		g_ATLCModem("Sending response: %s\n", kResponses[response]);

		const char *resp = kResponses[response];
		size_t len = strlen(resp);

		if (mTransmitLength > sizeof mTransmitBuffer - (len + 4))
			return;

		uint8 *dst = mTransmitBuffer + mTransmitLength;

		*dst++ = mRegisters.mLineTermChar;
		*dst++ = mRegisters.mRespFormatChar;
		memcpy(dst, resp, len);
		dst += len;
		*dst++ = mRegisters.mLineTermChar;
		*dst++ = mRegisters.mRespFormatChar;

		mTransmitLength += (uint32)len + 4;
	}
}

void ATModemEmulator::SendResponse(const char *s) {
	size_t len = strlen(s);

	if (sizeof(mTransmitBuffer) - mTransmitLength < len)
		return;

	memcpy(mTransmitBuffer + mTransmitLength, s, len);
	mTransmitLength += (uint32)len;

	if (mTransmitLength < sizeof mTransmitBuffer)
		mTransmitBuffer[mTransmitLength++] = mRegisters.mLineTermChar;

	if (mTransmitLength < sizeof mTransmitBuffer)
		mTransmitBuffer[mTransmitLength++] = mRegisters.mRespFormatChar;
}

void ATModemEmulator::SendResponseF(const char *format, ...) {
	char buf[512];
	va_list val;

	va_start(val, format);
	int n = _vsnprintf(buf, 512, format, val);
	va_end(val);

	if ((unsigned)n >= 511)
		return;

	SendResponse(buf);
}

void ATModemEmulator::ReportRegisters(const ATModemRegisters& reg, bool stored) {
	SendResponseF("E%u L%u M%u Q%u %c V%u X%u &C%u &D%u &G%u &K%u &T%u"
		, reg.mbEchoMode
		, reg.mSpeakerVolume
		, reg.mSpeakerMode
		, reg.mbQuietMode
		, reg.mbToneDialMode ? 'T' : 'P'
		, !reg.mbShortResponses
		, reg.mExtendedResultCodes
		, reg.mbReportCarrier
		, reg.mDTRMode
		, reg.mGuardToneMode
		, reg.mFlowControlMode
		, reg.mbLoopbackMode
		);

	if (stored) {
		SendResponseF("S00:%03u S02:%03u S06:%03u S07:%03u S08:%03u S10:%03u S11:%03u S12:%03u"
			, reg.mAutoAnswerRings
			, reg.mEscapeChar
			, reg.mDialToneWaitTime
			, reg.mDialCarrierWaitTime
			, reg.mDialPauseTime
			, reg.mLostCarrierWaitTime
			, reg.mDTMFToneDuration
			, reg.mEscapePromptDelay
			);
	} else {
		SendResponseF("S00:%03u S02:%03u S03:%03u S04:%03u S05:%03u S06:%03u S07:%03u S08:%03u"
			, reg.mAutoAnswerRings
			, reg.mEscapeChar
			, reg.mLineTermChar
			, reg.mRespFormatChar
			, reg.mCommandEditChar
			, reg.mDialToneWaitTime
			, reg.mDialCarrierWaitTime
			, reg.mDialPauseTime
			);

		SendResponseF("S10:%03u S11:%03u S12:%03u"
			, reg.mLostCarrierWaitTime
			, reg.mDTMFToneDuration
			, reg.mEscapePromptDelay
			);
	}
}

void ATModemEmulator::TerminateCall() {
	if (mpDriver) {
		mpDriver->Shutdown();
		delete mpDriver;
		mpDriver = NULL;
	}

	mbNewConnectedState = false;
	mConnectionState = kConnectionState_NotConnected;
	mbListening = false;
	mbDeviceTransmitUnderflow = true;
	mDeviceTransmitReadOffset = 0;
	mDeviceTransmitWriteOffset = 0;
	mDeviceTransmitLevel = 0;

	UpdateUIStatus();
	UpdateControlState();
}

void ATModemEmulator::RestoreListeningState() {
	TerminateCall();

	if (!mbListenEnabled)
		return;

	mpDriver = ATCreateModemDriverTCP();
	mpDriver->SetConfig(mConfig);
	if (!mpDriver->Init(NULL, NULL, mConfig.mListenPort, g_ATLCModemTCP.IsEnabled(), this)) {
		TerminateCall();
		return;
	}

	mbListening = true;

	UpdateUIStatus();
}

void ATModemEmulator::UpdateControlState() {
	const bool cd = mRegisters.mbReportCarrier ? mConnectionState == kConnectionState_Connected || mConnectionState == kConnectionState_Connecting : true;
	bool changed = false;

	if (mControlState.mbCarrierDetect != cd) {
		mControlState.mbCarrierDetect = cd;
		changed = true;
	}

	mControlState.mbClearToSend = true;
	mControlState.mbDataSetReady = true;

	if (mControlState.mbRinging != mbRinging) {
		mControlState.mbRinging = mbRinging;
		changed = true;
	}

	if (changed && mpCB)
		mpCB(mControlState);
}

void ATModemEmulator::UpdateUIStatus() {
	if (!mpUIRenderer)
		return;

	VDStringA str;

	switch(mConnectionState) {
		case kConnectionState_NotConnected:
			if (mpDriver) {
				if (mbListening) {
					if (mbRinging) {
						const char *preBracket = "";
						const char *postBracket = "";

						if (mAddress.find(':') != VDStringA::npos) {
							preBracket = "[";
							postBracket = "]";
						}

						str.sprintf("Incoming connection from %s%s%s:%s", preBracket, mAddress.c_str(), postBracket, mService.c_str());
					} else
						str.sprintf("Waiting for connection on port %u", mConfig.mListenPort);
				} else
					str.sprintf("Connecting to %s:%s...", mAddress.c_str(), mService.c_str());
			}
			break;

		case kConnectionState_Connecting:
		case kConnectionState_Connected:
			str.sprintf("%s to %s"
				, mConnectionState == kConnectionState_Connecting ? "Establishing connection" : "Connected"
				, mRemoteName.c_str()
				, mbCommandMode ? " (in command mode)" : "");
			break;

		case kConnectionState_LostCarrier:
			str = "Lost carrier (modem still in online state)";
			break;
	
		case kConnectionState_Dialing:
			str.sprintf("Dialing out to %s", mRemoteName.c_str());
			break;

		case kConnectionState_Ringing:
			if (mpDriver)
				str.sprintf("Connecting to %s", mRemoteName.c_str());
			else
				str.sprintf("Dialing out to %s", mRemoteName.c_str());
			break;

		case kConnectionState_Handshaking:
			str.sprintf("Connected to %s", mRemoteName.c_str());
			break;
	}

	if (mpUIRenderer)
		mpUIRenderer->SetModemConnection(str.c_str());
}

void ATModemEmulator::EnterCommandMode(bool sendPrompt, bool force) {
	if (mbCommandMode && !force)
		return;

	if (mpEventEnterCommandMode) {
		mpScheduler->RemoveEvent(mpEventEnterCommandMode);
		mpEventEnterCommandMode = NULL;
	}

	// enter command mode
	if (sendPrompt)
		SendResponse(kResponseOK);

	mGuardCharCounter = 0;
	mbCommandMode = true;
	mCommandLength = 0;
	mCommandState = kCommandState_Idle;

	UpdateUIStatus();
}

int ATModemEmulator::GetRegisterValue(uint32 reg) const {
	if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
		switch(reg) {
			case 1:		// ring count
				return 0;

			case 8:		// pause time (seconds)
				return 2;

			case 9:		// carrier detect response time (tenths of seconds)
				return 6;

			case 11:	// duration and spacing of touch tones (msec)
				return 70;

			case 13:	// UART status register
				return 16;

			case 14:	// Option register
				return 106;

			case 15:	// Flag register
			{
				uint8 v = 0;

				// originate mode is reported on bit 2
				if (mRegisters.mbOriginateMode)
					v += 0x04;

				// full duplex is reported on bit 3
				if (mRegisters.mbFullDuplex)
					v += 0x08;

				// baud rate is reported on bits 0-1 and 4-5 (x1 = 110, 10 = 300, 11 = 1200)
				if (mCommandRate < 300)
					v += 0x11;
				else if (mCommandRate == 300)
					v += 0x22;
				else
					v += 0x33;

				// carrier detect is reported on bit 6
				if (mControlState.mbCarrierDetect)
					v += 0x40;

				// DTR enable is reported on bit 7 (currently always off)
				return v;
			}

			case 16:	// test modes
				return 0;
		}
	}

	switch(reg) {
		case 0:		return mRegisters.mAutoAnswerRings;
		case 2:		return mRegisters.mEscapeChar;
		case 3:		return mRegisters.mLineTermChar;
		case 4:		return mRegisters.mRespFormatChar;
		case 5:		return mRegisters.mCommandEditChar;
		case 6:		return mRegisters.mDialToneWaitTime;
		case 7:		return mRegisters.mDialCarrierWaitTime;
		case 8:		return mRegisters.mDialPauseTime;
		case 10:	return mRegisters.mLostCarrierWaitTime;
		case 11:	return mRegisters.mDTMFToneDuration;
		case 12:	return mRegisters.mEscapePromptDelay;

		case 15:
			return 0x3B;

		default:
			return -1;
	}
}

bool ATModemEmulator::SetRegisterValue(uint32 reg, uint8 value) {
	switch(reg) {
		case 0:
			mRegisters.mAutoAnswerRings = value;
			return true;

		case 2:
			mRegisters.mEscapeChar = value;
			return true;

		case 3:
			mRegisters.mLineTermChar = value;
			return true;

		case 4:
			mRegisters.mRespFormatChar = value;
			return true;

		case 5:
			mRegisters.mCommandEditChar = value;
			return true;

		case 6:
			mRegisters.mDialToneWaitTime = value;
			return true;

		case 7:
			mRegisters.mDialCarrierWaitTime = value;
			return true;
		
		case 8:
			mRegisters.mDialPauseTime = value;
			return true;

		case 10:
			mRegisters.mLostCarrierWaitTime = value;
			UpdateDerivedRegisters();
			return true;

		case 11:
			if (value < 50)
				return false;

			mRegisters.mDTMFToneDuration = value;
			return true;

		case 12:
			mRegisters.mEscapePromptDelay = value;
			return true;

		default:
			// The SX212 silently ignores an attempt to write to a non-existent register.
			if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212)
				return true;

			return false;
	}
}

void ATModemEmulator::UpdateDerivedRegisters() {
	mLostCarrierDelayCycles = mRegisters.mLostCarrierWaitTime == 255 ? 0 : 715909 * mRegisters.mLostCarrierWaitTime / 4 + 1;
}

void ATModemEmulator::ConnectPhoneLine() {
	if (mpDriver)
		return;

	mpDriver = ATCreateModemDriverTCP();
	mpDriver->SetConfig(mConfig);
	if (!mpDriver->Init(mAddress.c_str(), mService.c_str(), 0, g_ATLCModemTCP.IsEnabled(), this)) {
		HangUp();
		SendResponse(kResponseError);
		RestoreListeningState();
		return;
	}

	UpdateUIStatus();
}

void ATModemEmulator::AdvanceConnectionStateMachine() {
	mpScheduler->UnsetEvent(mpEventConnectionStateMachine);

	for(;;) {
		float nextDelay = 0;

		switch(mConnectionState) {
			case kConnectionState_Dialing:
			case kConnectionState_Ringing:
				mpModemSound->SetAudioEnabledByPhase(mConfig.mNetworkMode != ATModemNetworkMode::None);
				break;

			case kConnectionState_Handshaking:
				mpModemSound->SetAudioEnabledByPhase(mConfig.mNetworkMode == ATModemNetworkMode::Full);
				break;

			default:
				mpModemSound->SetAudioEnabledByPhase(false);
				break;
		}

		switch(mConnectionState) {
			default:
				return;

			case kConnectionState_Dialing:
				switch(mConnectionSubState) {
					case 0:
						if (!mDialString.empty()) {
							mpModemSound->PlayDialTone();

							nextDelay = 1.5f;
						}

						++mConnectionSubState;
						break;

					case 1:
						if (mDialString.empty()) {
							mConnectionSubState = 10;
							break;
						} else {
							const char c = mDialString.back();
							mDialString.pop_back();

							if (c >= '0' && c <= '9') {
								mpModemSound->PlayDTMFTone((uint32)(c - '0'));
								nextDelay = (float)mRegisters.mDTMFToneDuration * 0.001f;
								mConnectionSubState = 2;
							} else if (c == '*') {
								mpModemSound->PlayDTMFTone(10);
								nextDelay = (float)mRegisters.mDTMFToneDuration * 0.001f;
								mConnectionSubState = 2;
							} else if (c == '#') {
								mpModemSound->PlayDTMFTone(11);
								nextDelay = (float)mRegisters.mDTMFToneDuration * 0.001f;
								mConnectionSubState = 2;
							} else if (c == 'T') {
								// tone dialing -- ignore
							} else if (c == 'P') {
								// tone dialing -- ignore
							} else if (c == '@') {
								// wait 5s silence
								nextDelay = 5.0f;
							} else if (c == ',') {
								// pause (S8)
								nextDelay = (float)mRegisters.mDialPauseTime;
							} else {
								// ()- and space ignored, invalid characters ignored
							}
						}
						break;

					case 2:
						mpModemSound->Stop();
						nextDelay = 0.05f;

						mConnectionSubState = 1;
						break;

					case 10:
					default:
						mpModemSound->Stop();
						mConnectionState = kConnectionState_Ringing;
						mConnectionSubState = 0;
						nextDelay = 0.5f;
						UpdateUIStatus();
						break;
				}
				break;

			case kConnectionState_Ringing:
				if ((mConnectionSubState & 1) == 0) {
					if (mConnectionSubState == 2) {
						if (!mAddress.empty())
							ConnectPhoneLine();
					} else if (mConnectionSubState == 8) {
						if (mAddress.empty()) {
							HangUp();
							SendResponse(kResponseNoAnswer);
							break;
						}
					}

					mpModemSound->PlayRingingTone();
					++mConnectionSubState;
					nextDelay = 2.0f;
				} else {
					mpModemSound->Stop();
					++mConnectionSubState;
					nextDelay = 4.0f;
				}
				break;

			case kConnectionState_Handshaking:
				switch(mConnectionSubState) {
					case 0:
						// We set this here so that kConnectTime is essentially defeated when
						// we are skipping this connection flow.
						mConnectStartTime = ATSCHEDULER_GETTIME(mpScheduler);

						if (mConnectRate < 1200) {
							// 300 baud - Bell 103
							mConnectionSubState = 100;
						} else if (mConnectRate < 4800) {
							// 1200 baud - Bell 212A
							mConnectionSubState = 200;
						} else {
							// 4800/9600 baud - V.32
							mConnectionSubState = 400;
						}
						break;

					//--- Bell 103 modulation (300 baud)
					case 100:
						mpModemSound->PlayAnswerTone(true);
						nextDelay = 0.155f + 0.456f;
						++mConnectionSubState;
						break;

					case 101:
						mpModemSound->PlayOriginatingToneBell103();
						nextDelay = 0.270f + 0.765f;
						++mConnectionSubState;
						break;

					//--- suppress echo cancellation tone
					case 200:
					case 202:
					case 204:
						mpModemSound->PlayEchoSuppressionTone();
						nextDelay = 0.450f;
						++mConnectionSubState;
						break;

					case 201:
					case 203:
					case 205:
						mpModemSound->Stop();
						nextDelay = 0.02f;
						++mConnectionSubState;
						break;

					case 206:
						// 1200/2400 baud - Bell 212A
						mConnectionSubState = 300;
						break;

					//--- Bell 212A modulation (1200/2400 baud)
					case 300:
						// play answer tone at 2225Hz (Bell 212A)
						mpModemSound->PlayAnswerTone(true);
						
						// detect answer tone in 155+/-50 ms and wait
						// 456+/-10ms
						nextDelay = 0.155f + 0.456f;
						++mConnectionSubState;
						break;

					case 301:
						// play binary 1 at 1200Hz
						mpModemSound->PlayModemDataV22(false, true);

						// detect in 270+/-40ms
						nextDelay = 0.270f;
						++mConnectionSubState;
						break;

					case 302:
						// switch to scrambled 1 at 2400Hz
						mpModemSound->PlayModemDataV22(true, true);

						// detect in 270+/-40ms and wait 765+/-10ms
						nextDelay = 0.270f + 0.765f;
						++mConnectionSubState;
						break;

					// V.32 modulation (4800/9600 baud)
					case 406:
						mpModemSound->PlayOriginatingToneV32();
						// fall through
					case 400:
					case 402:
					case 404:
					case 408:
						mpModemSound->PlayEchoSuppressionTone();
						nextDelay = 0.450f;
						++mConnectionSubState;
						break;

					case 401:
					case 403:
					case 405:
					case 407:
					case 409:
						mpModemSound->Stop1();
						nextDelay = 0.02f;
						++mConnectionSubState;
						break;

					case 410:
					case 412:
					case 414:
						// begin training answering receiver
						mpModemSound->Stop();
						mpModemSound->PlayTrainingToneV32();
						nextDelay = 256.0f / 2400.0f;
						++mConnectionSubState;
						break;
					
					case 411:
					case 413:
					case 415:
						mpModemSound->Stop();
						nextDelay = 0.01f;
						++mConnectionSubState;
						break;

					case 416:
						mConnectionSubState = 421;
						break;

					case 421:
						// train answering receiver
						mpModemSound->Stop();
						mpModemSound->PlayModemData(0.8f);
						nextDelay = 2.5f;
						++mConnectionSubState;
						break;

					case 422:
						// begin training calling receiver
						mpModemSound->Stop();
						mpModemSound->PlayTrainingToneV32();
						nextDelay = 256.0f / 2400.0f;
						++mConnectionSubState;
						break;

					case 423:
						// train calling receiver
						mpModemSound->Stop();
						mpModemSound->PlayModemData(0.5f);
						nextDelay = 2.5f;
						++mConnectionSubState;
						break;

					case 424:
						// begin training answering receiver while originating still transmitting
						mpModemSound->Stop();
						mpModemSound->PlayTrainingToneV32();
						nextDelay = 256.0f / 2400.0f;
						++mConnectionSubState;
						break;

					case 425:
						// train calling receiver
						mpModemSound->Stop();
						mpModemSound->PlayModemData(0.8f);
						nextDelay = 2.5;
						++mConnectionSubState;
						break;

					default:
						mpModemSound->Stop();

						mbCommandMode = false;
						UpdateControlState();
						mConnectionState = kConnectionState_Connecting;
						mConnectionSubState = 0;
						UpdateUIStatus();
						break;
				}
				break;
		}

		if (nextDelay > 0) {
			uint32 nextDelayCycles = (uint32)(0.5f + mpScheduler->GetRate().asDouble() * nextDelay);

			switch(mConnectionState) {
				case kConnectionState_Dialing:
				case kConnectionState_Ringing:
					if (mConfig.mNetworkMode == ATModemNetworkMode::None)
						nextDelayCycles = 1;
					break;

				case kConnectionState_Handshaking:
					if (mConfig.mNetworkMode != ATModemNetworkMode::Full)
						nextDelayCycles = 1;
					break;
			}

			if (nextDelayCycles) {
				mpScheduler->SetEvent(nextDelayCycles, this, kEventId_ConnectionStateMachine, mpEventConnectionStateMachine);
				break;
			}
		}
	}
}

void ATModemEmulator::SetConnectRate() {
	if (mConfig.mDeviceMode == kATRS232DeviceMode_SX212) {
		const bool highSpeed = mConfig.mConnectionSpeed > 600;

		mConnectRate = highSpeed ? 1200 : 300;

		if (mControlState.mbHighSpeed != highSpeed) {
			mControlState.mbHighSpeed = highSpeed;

			if (mpCB)
				mpCB(mControlState);
		}
	} else {
		mConnectRate = mConfig.mConnectionSpeed;

		if (!mConnectRate)
			mConnectRate = 9600;
	}
}

void ATModemEmulator::SetRemoteNameFromAddress() {
	const char *preBracket = "";
	const char *postBracket = "";

	if (mAddress.find(':') != VDStringA::npos) {
		preBracket = "[";
		postBracket = "]";
	}

	mRemoteName.sprintf("%s%s%s:%s", preBracket, mAddress.c_str(), postBracket, mService.c_str());
}

void ATModemEmulator::OnScheduledEvent(uint32 id) {
	if (id == 1) {
		mpEventEnterCommandMode = NULL;

		if (mGuardCharCounter >= 3) {
			// enter command mode
			EnterCommandMode(true, true);
		}
	} else if (id == 2) {
		mpEventCommandModeTimeout = NULL;

		if (mbCommandMode && mCommandLength > 0) {
			mCommandLength = 0;

			SendResponse(kResponseError);
		}
	} else if (id == 3) {
		mpEventPoll = mpSlowScheduler->AddEvent(100, this, 3);

		Poll();
	} else if (id == 4) {
		mpEventCommandTermDelay = NULL;
		ParseCommand();
	} else if (id == kEventId_ConnectionStateMachine) {
		mpEventConnectionStateMachine = nullptr;

		AdvanceConnectionStateMachine();
	}
}

void ATModemEmulator::OnReadAvail(IATModemDriver *sender, uint32 len) {
	// do nothing -- we poll this
}

void ATModemEmulator::OnWriteAvail(IATModemDriver *sender) {
	mMutex.Lock();
	for(;;) {
		uint32 tc = mDeviceTransmitLevel;

		if (!tc) {
			mbDeviceTransmitUnderflow = true;
			break;
		}

		if (mDeviceTransmitReadOffset + tc > sizeof mDeviceTransmitBuffer)
			tc = sizeof mDeviceTransmitBuffer - mDeviceTransmitReadOffset;

		tc = sender->Write(mDeviceTransmitBuffer + mDeviceTransmitReadOffset, tc);

		if (!tc)
			break;

		mbDeviceTransmitUnderflow = false;

		mDeviceTransmitReadOffset += tc;
		if (mDeviceTransmitReadOffset >= sizeof mDeviceTransmitBuffer)
			mDeviceTransmitReadOffset = 0;

		mDeviceTransmitLevel -= tc;
	}
	mMutex.Unlock();
}

void ATModemEmulator::OnEvent(IATModemDriver *sender, ATModemPhase phase, ATModemEvent event) {
	if (event == kATModemEvent_Connected)
		mbNewConnectedState = true;
	else {
		if (event == kATModemEvent_ConnectionClosing && !mbRinging)
			return;

		mbNewConnectedState = false;

		if (phase <= kATModemPhase_Connecting)
			mbConnectionFailed = true;
	}
}
