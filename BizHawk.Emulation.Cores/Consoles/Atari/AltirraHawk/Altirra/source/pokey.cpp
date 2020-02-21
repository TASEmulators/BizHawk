//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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
#include <at/atcore/logging.h>
#include "pokey.h"
#include "pokeyrenderer.h"
#include "pokeytables.h"
#include <float.h>
#include <vd2/system/bitmath.h>
#include <vd2/system/math.h>
#include <vd2/system/binary.h>

#include <math.h>

#include "console.h"
#include "cpu.h"
#include "savestate.h"
#include "audiooutput.h"
#include "trace.h"

ATLogChannel g_ATLCSIOData(false, false, "SIODATA", "Serial I/O bus data");

namespace {
	enum {
		kATPokeyEventKeyboardIRQ = 2,
		kATPokeyEventKeyboardScan = 3,
		kATPokeyEventTimer1Borrow = 7,
		kATPokeyEventTimer2Borrow = 8,
		kATPokeyEventTimer3Borrow = 9,
		kATPokeyEventTimer4Borrow = 10,
		kATPokeyEventResetTimers = 11,
		kATPokeyEventSerialOutput = 12,
		kATPokeyEventSerialInput = 13,
		kATPokeyEventPot0ScanComplete = 16	// x8
	};

	const bool kForceActiveTimers = false;
}

ATPokeyEmulator::ATPokeyEmulator(bool isSlave)
	: mpRenderer(new ATPokeyRenderer)
	, mbCommandLineState(false)
	, mb5200Mode(false)
	, mbTraceSIO(false)
	, mbNonlinearMixingEnabled(true)
	, mKBCODE(0)
	, mKeyCodeTimer(0)
	, mKeyCooldownTimer(0)
	, mbKeyboardIRQPending(false)
	, mbUseKeyCooldownTimer(true)
	, mbCookedKeyMode(false)
	, mbKeyboardScanEnabled(false)
	, mbShiftKeyState(false)
	, mbShiftKeyLatchedState(false)
	, mbControlKeyState(false)
	, mbControlKeyLatchedState(false)
	, mbBreakKeyState(false)
	, mbBreakKeyLatchedState(false)
	, mAddressMask(0x0f)
	, mIRQEN(0)
	, mIRQST(0)
	, mAUDCTL(0)
	, mSERIN(0)
	, mSEROUT(0)
	, mSKSTAT(0)
	, mSKCTL(0)
	, mLastPolyTime(0)
	, mPoly17Counter(0)
	, mPoly9Counter(0)
	, mPolyShutOffTime(0)
	, mSerialOutputStartTime(0)
	, mSerialInputShiftRegister(0)
	, mSerialOutputShiftRegister(0)
	, mSerialInputCounter(0)
	, mSerialOutputCounter(0)
	, mbSerOutValid(false)
	, mbSerShiftValid(false)
	, mbSerialOutputState(false)
	, mbSpeakerActive(false)
	, mbSerialRateChanged(false)
	, mbSerialWaitingForStartBit(true)
	, mbSerInBurstPendingIRQ1(false)
	, mbSerInBurstPendingIRQ2(false)
	, mbSerInBurstPendingData(false)
	, mbSerInDeferredLoad(false)
	, mSerialSimulateInputBaseTime(0)
	, mSerialSimulateInputCyclesPerBit(0)
	, mSerialSimulateInputData(0)
	, mbSerialSimulateInputPort(false)
	, mSerialExtBaseTime(0)
	, mSerialExtPeriod(0)
	, mpAudioLog(NULL)
	, mbFastTimer1(false)
	, mbFastTimer3(false)
	, mbLinkedTimers12(false)
	, mbLinkedTimers34(false)
	, mbUse15KHzClock(false)
	, mLast15KHzTime(0)
	, mLast64KHzTime(0)
	, mpKeyboardScanEvent(NULL)
	, mpKeyboardIRQEvent(NULL)
	, mpStartBitEvent(NULL)
	, mpResetTimersEvent(NULL)
	, mpEventSerialInput(NULL)
	, mpEventSerialOutput(NULL)
	, mpScheduler(NULL)
	, mpConn(NULL)
	, mpTraceContext(nullptr)
	, mpSlave(NULL)
	, mbIsSlave(isSlave)
	, mbIrqAsserted(false)
{
	memset(mpTimerBorrowEvents, 0, sizeof(mpTimerBorrowEvents));
}

ATPokeyEmulator::~ATPokeyEmulator() {
	delete mpRenderer;
}

void ATPokeyEmulator::Init(IATPokeyEmulatorConnections *mem, ATScheduler *sched, IATAudioOutput *output, ATPokeyTables *tables) {
	mpConn = mem;
	mpScheduler = sched;
	mpAudioOut = output;
	mpTables = tables;
	UpdateMixTable();

	mpRenderer->Init(sched, tables);

	VDASSERT(!mpKeyboardScanEvent);
	VDASSERT(!mpKeyboardIRQEvent);

	for(int i=0; i<8; ++i) {
		mPotPositions[i] = 228;
		mPotHiPositions[i] = 228;
	}

	ColdReset();
}

void ATPokeyEmulator::ColdReset() {
	mKeyScanCode = 0;
	mKeyScanState = 0;
	mbKeyboardIRQPending = false;
	mbControlKeyLatchedState = false;
	mbShiftKeyLatchedState = false;
	mbBreakKeyLatchedState = false;

	memset(&mState, 0, sizeof mState);

	mKBCODE = 0;
	mSKSTAT = mbShiftKeyState ? 0xF7 : 0xFF;
	mSKCTL = 0;
	mKeyCodeTimer = 0;
	mKeyCooldownTimer = 0;
	mIRQEN = 0;
	mIRQST = 0xF7;

	mAUDCTL = 0;
	mbFastTimer1 = false;
	mbFastTimer3 = false;
	mbLinkedTimers12 = false;
	mbLinkedTimers34 = false;
	mbUse15KHzClock = false;

	for(int i=0; i<4; ++i) {
		mCounter[i] = 1;
		mCounterBorrow[i] = 0;
		mAUDFP1[i] = 1;
		mbDeferredTimerEvents[i] = false;

		mpScheduler->UnsetEvent(mpTimerBorrowEvents[i]);
	}

	RecomputeTimerPeriod<0>();
	RecomputeTimerPeriod<1>();
	RecomputeTimerPeriod<2>();
	RecomputeTimerPeriod<3>();
	RecomputeAllowedDeferredTimers();

	mpScheduler->UnsetEvent(mpKeyboardScanEvent);
	mpScheduler->UnsetEvent(mpKeyboardIRQEvent);
	mpScheduler->UnsetEvent(mpStartBitEvent);
	mpScheduler->UnsetEvent(mpResetTimersEvent);
	mpScheduler->UnsetEvent(mpEventSerialOutput);
	mpScheduler->UnsetEvent(mpEventSerialInput);

	mpRenderer->ColdReset();

	mLastPolyTime = ATSCHEDULER_GETTIME(mpScheduler);
	mPoly17Counter = 0;
	mPoly9Counter = 0;
	mSerialOutputStartTime = 0;
	mSerialInputShiftRegister = 0;
	mSerialOutputShiftRegister = 0;
	mSerialOutputCounter = 0;
	mSerialInputCounter = 0;
	mbSerOutValid = false;
	mbSerShiftValid = false;
	mbSerialOutputState = false;
	mbSerialWaitingForStartBit = true;
	mbSerInBurstPendingIRQ1 = false;
	mbSerInBurstPendingIRQ2 = false;
	mbSerInBurstPendingData = false;
	mSerOutBurstDeadline = 0;
	mbSerialSimulateInputPort = false;

	memset(mAUDF, 0, sizeof mAUDF);
	memset(mAUDC, 0, sizeof mAUDC);

	mLast15KHzTime = ATSCHEDULER_GETTIME(mpScheduler);
	mLast64KHzTime = ATSCHEDULER_GETTIME(mpScheduler);

	mALLPOT = 0;

	for(int i=0; i<8; ++i) {
		mPotLatches[i] = 228;
	}

	mPotLastTimeFast = ATSCHEDULER_GETTIME(mpScheduler);
	mPotLastTimeSlow = mPotLastTimeFast;
	mbPotScanActive = false;

	mbCommandLineState = false;

	if (mpSlave)
		mpSlave->ColdReset();

	NegateIrq(false);
}

void ATPokeyEmulator::SetSlave(ATPokeyEmulator *slave) {
	if (mpSlave)
		mpSlave->ColdReset();

	mpSlave = slave;

	UpdateAddressDecoding();

	if (mpSlave) {
		mpSlave->ColdReset();
		mpSlave->SyncRenderers(mpRenderer);

		// If we're hot-starting a slave, let's try to get it into
		// a somewhat reasonable state.

		static const uint8 kInitRegs[][2]={
			{ 0x00, 0xFF },		// AUDF1 = $00
			{ 0x01, 0xB0 },		// AUDC1 = $B0
			{ 0x02, 0xFF },		// AUDF2 = $00
			{ 0x03, 0xB0 },		// AUDC2 = $B0
			{ 0x04, 0xFF },		// AUDF3 = $00
			{ 0x05, 0xB0 },		// AUDC3 = $B0
			{ 0x06, 0xFF },		// AUDF4 = $00
			{ 0x07, 0xB0 },		// AUDC4 = $B0
			{ 0x08, 0x00 },		// AUDCTL = $00
			{ 0x0F, 0x03 },		// SKCTL = $03
		};

		for(const auto& data : kInitRegs)
			mpSlave->WriteByte(data[0], data[1]);
	}

	UpdateMixTable();
}

void ATPokeyEmulator::SetCassette(IATPokeyCassetteDevice *dev) {
	mpCassette = dev;
	mbSerialRateChanged = true;
}

void ATPokeyEmulator::SetAudioLog(ATPokeyAudioLog *log) {
	mpAudioLog = log;
}

void ATPokeyEmulator::Set5200Mode(bool enable) {
	if (mb5200Mode == enable)
		return;

	mb5200Mode = enable;
	UpdateKeyboardScanEvent();
}

void ATPokeyEmulator::AddSIODevice(IATPokeySIODevice *device) {
	mDevices.push_back(device);
	device->PokeyAttachDevice(this);
}

void ATPokeyEmulator::RemoveSIODevice(IATPokeySIODevice *device) {
	Devices::iterator it(std::find(mDevices.begin(), mDevices.end(), device));

	if (it != mDevices.end())
		mDevices.erase(it);
}

void ATPokeyEmulator::ReceiveSIOByte(uint8 c, uint32 cyclesPerBit, bool simulateInputPort, bool allowBurst, bool synchronous, bool forceFramingError) {
	if (cyclesPerBit && mbSerialNoiseEnabled) {
		const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

		uint32 pat = c + c + 0x200;
		pat ^= (pat + pat + 1);

		for (uint32 i = 0; i < 10; ++i) {
			if (pat & (1 << i))
				mpRenderer->AddSerialNoisePulse(t + cyclesPerBit * i);
		}
	}

	if (mbTraceSIO)
		ATConsoleTaggedPrintf("POKEY: Receiving byte (c=%02X; %02X %02X) at %u cycles/bit (%.1f baud)\n", c, mSERIN, mSerialInputShiftRegister, cyclesPerBit, 7159090.0f / 4.0f / (float)cyclesPerBit);

	VDStringA sioDataInfo;

	if (g_ATLCSIOData) {
		if (mTraceByteIndex >= 1000 || mTraceDirectionSend) {
			mTraceByteIndex = 0;
			mTraceDirectionSend = false;
		}

		sioDataInfo.sprintf("[%3u] Receive      < $%02X     (@ %u cycles/bit / %.1f baud)", mTraceByteIndex++, c, cyclesPerBit, 7159090.0f / 4.0f / (float)cyclesPerBit);
	}

	// check for attempted read in init mode (partial fix -- audio not emulated)
	if (!(mSKCTL & 3)) {
		if (mbTraceSIO)
			ATConsoleTaggedPrintf("POKEY: Dropping byte due to initialization mode.\n");

		if (g_ATLCSIOData) {
			sioDataInfo += " [dropped - init mode]";
			g_ATLCSIOData <<= sioDataInfo.c_str();
		}
		return;
	}

	mbSerialSimulateInputPort = simulateInputPort;

	if (simulateInputPort) {
		VDASSERT(cyclesPerBit);
		mSerialSimulateInputBaseTime = ATSCHEDULER_GETTIME(mpScheduler);
		mSerialSimulateInputCyclesPerBit = cyclesPerBit;
		mSerialSimulateInputData = ((uint32)c << 1) + 0x200;
	}

	if (!(mSKCTL & 0x30) && !mSerialExtPeriod) {
		if (mbTraceSIO)
			ATConsoleTaggedPrintf("POKEY: Dropping byte $%02X due to external receive mode being used with no external clock (SKCTL=$%02X).", c, mSKCTL);

		if (g_ATLCSIOData) {
			sioDataInfo += " [dropped - external mode with no clock]";
			sioDataInfo += "\n";
			g_ATLCSIOData <<= sioDataInfo.c_str();
		}

		return;
	}

	mSerialInputPendingStatus = 0xff;

	if (forceFramingError) {
		mSerialInputPendingStatus &= 0x7f;
		sioDataInfo += " [framing error]\n";
	}

	// check for attempted read in synchronous mode; note that external clock mode is OK as presumably that
	// is synchronized
	if ((mSKCTL & 0x30) == 0x20 && !synchronous) {
		// set the framing error bit
		mSerialInputPendingStatus &= 0x7F;

		if (mbTraceSIO)
			ATConsoleTaggedPrintf("POKEY: Trashing byte $%02x and signaling framing error due to asynchronous input mode not being enabled (SKCTL=$%02X).\n", c, mSKCTL);

		if (g_ATLCSIOData)
			sioDataInfo += " [garbled - receiving in synchronous mode]";

		// blown read -- trash the byte by faking a dropped bit
		c = (c & 0x0f) + ((c & 0xe0) >> 1) + 0x80;
	}

	// check for mismatched baud rate
	if (cyclesPerBit) {
		uint32 expectedCPB = GetSerialCyclesPerBitRecv();
		uint32 margin = (expectedCPB + 7) >> 3;

		if (cyclesPerBit < expectedCPB - margin || cyclesPerBit > expectedCPB + margin) {
			// blown read -- trash the byte and assert the framing error bit
			c = 0xFF;
			mSerialInputPendingStatus &= 0x7F;

			if (mbTraceSIO)
				ATConsoleTaggedPrintf("POKEY: Signaling framing error due to receive rate mismatch (expected %d cycles/bit, got %d)\n", expectedCPB, cyclesPerBit);

			if (g_ATLCSIOData)
				sioDataInfo.append_sprintf(" [garbled - expected %u cycles/bit]", expectedCPB);
		}
	}

	if (!(mSKCTL & 0x30)) {
		if (mpEventSerialInput) {
			if (mbTraceSIO)
				ATConsoleTaggedPrintf("POKEY: Interrupting send already in progress (%u cycles, %u bits left).\n", c, mpScheduler->GetTicksToEvent(mpEventSerialInput), mSerialInputCounter);
		}

		mpScheduler->SetEvent(mSerialExtPeriod, this, kATPokeyEventSerialInput, mpEventSerialInput);
	} else if (mSKCTL & 0x10) {
		// Restart timers 3 and 4 immediately.
		UpdateTimerCounter<2>();
		UpdateTimerCounter<3>();

		mCounter[2] = mAUDFP1[2];
		mCounter[3] = mAUDFP1[3];

		mbSerialWaitingForStartBit = false;
		SetupTimers(0x0c);

		if (mpStartBitEvent) {
			mpScheduler->RemoveEvent(mpStartBitEvent);
			mpStartBitEvent = NULL;
		}
	}

	if (g_ATLCSIOData) {
		sioDataInfo += '\n';
		g_ATLCSIOData <<= sioDataInfo.c_str();
	}

	mSerialInputShiftRegister = c;

	if (!(mSKCTL & 0x30))
		mSerialInputCounter = 9;
	else
		mSerialInputCounter = 19;

	// assert serial input busy
	mSKSTAT &= 0xfd;

	mbSerInDeferredLoad = simulateInputPort;
	
	if (allowBurst) {
		mbSerInBurstPendingData = true;
		mbSerInBurstPendingIRQ1 = true;
		mbSerInDeferredLoad = false;
	} else {
		mbSerInBurstPendingData = false;
		mbSerInBurstPendingIRQ1 = false;
	}

	mbSerInBurstPendingIRQ2 = false;
	mSerOutBurstDeadline = 0;

	if (!mbSerInDeferredLoad)
		ProcessReceivedSerialByte();
}

void ATPokeyEmulator::SetAudioLine2(int v) {
	mpRenderer->SetAudioLine2(v);
}

void ATPokeyEmulator::SetDataLine(bool newState) {
	if (newState)
		mSKSTAT |= 0x10;
	else
		mSKSTAT &= ~0x10;
}

void ATPokeyEmulator::SetCommandLine(bool newState) {
	if (newState == mbCommandLineState)
		return;

	if (mbTraceSIO)
		ATConsoleTaggedPrintf("POKEY: %s command line.\n", newState ? "asserting" : "negating");

	mbCommandLineState = newState;
	if (newState) {
		for(Devices::const_iterator it(mDevices.begin()), itEnd(mDevices.end()); it!=itEnd; ++it)
			(*it)->PokeyBeginCommand();
	} else {
		for(Devices::const_iterator it(mDevices.begin()), itEnd(mDevices.end()); it!=itEnd; ++it)
			(*it)->PokeyEndCommand();
	}
}

void ATPokeyEmulator::SetSpeaker(bool newState) {
	if (mpRenderer->SetSpeaker(newState))
		mbSpeakerActive = true;
}

void ATPokeyEmulator::SetStereoSoftEnable(bool enable) {
	if (mbStereoSoftEnable != enable) {
		mbStereoSoftEnable = enable;

		if (mpSlave)
			UpdateAddressDecoding();
	}
}

void ATPokeyEmulator::SetExternalSerialClock(uint32 basetime, uint32 period) {
	mSerialExtBaseTime = basetime;
	mSerialExtPeriod = period;

	if (!period) {
		mpScheduler->UnsetEvent(mpEventSerialInput);
		mpScheduler->UnsetEvent(mpEventSerialOutput);
	}
}

bool ATPokeyEmulator::IsChannelEnabled(uint32 channel) const {
	return mpRenderer->IsChannelEnabled(channel);
}

void ATPokeyEmulator::SetChannelEnabled(uint32 channel, bool enabled) {
	mpRenderer->SetChannelEnabled(channel, enabled);
}

void ATPokeyEmulator::SetNonlinearMixingEnabled(bool enable) {
	if (mbNonlinearMixingEnabled != enable) {
		mbNonlinearMixingEnabled = enable;

		UpdateMixTable();

		if (mpRenderer)
			mpRenderer->SetFiltersEnabled(enable);

		if (mpAudioOut)
			mpAudioOut->SetFiltersEnabled(enable);
	}
}

void ATPokeyEmulator::SetShiftKeyState(bool newState, bool immediate) {
	mbShiftKeyState = newState;

	// Shift key state can only change if keyboard scan is enabled. Debounce doesn't matter.
	if (immediate && (mSKCTL & 0x02)) {
		mbShiftKeyLatchedState = newState;

		if (newState)
			mSKSTAT &= ~0x08;
		else
			mSKSTAT |= 0x08;
	}

	// Shift is on the $10-17 row with the KR2 column
	UpdateKeyMatrix(2, 0x100, newState ? 0x100 : 0);
}

void ATPokeyEmulator::SetControlKeyState(bool newState) {
	mbControlKeyState = newState;

	// Control is on the $00-07 row with the KR2 column
	UpdateKeyMatrix(0, 0x100, newState ? 0x100 : 0);
}

void ATPokeyEmulator::ClearKeyQueue() {
	mKeyQueue.clear();
}

void ATPokeyEmulator::PushKey(uint8 c, bool repeat, bool allowQueue, bool flushQueue, bool useCooldown) {
	SetKeyboardModes(true, false);

	// Discard keys that are impossible due to key matrix conflicts.
	// Codes 0xC0-C7 and 0xD0-D7 cannot be produced due to a keyboard
	// matrix conflict when Ctrl+Shift is pressed and keys on 0x00-07/10-17.
	// They ARE possible with debounce disabled, but we don't support that
	// in the cooked key path.
	if ((c & 0xE8) == 0xC0)
		return;

	mbUseKeyCooldownTimer = useCooldown;

	if (allowQueue) {
		// Queue a key if we already have keys queued, or the OS can't accept one yet.
		if (!mKeyQueue.empty() || !CanPushKey(c)) {
			mKeyQueue.push_back(c);
			return;
		}
	} else if (flushQueue) {
		mKeyQueue.clear();
	}

	// If debounce or scan is disabled, drop the key.
	if ((mSKCTL & 3) != 3)
		return;

	mKBCODE = c;
	mSKSTAT &= ~0x04;

	if (!mKeyCodeTimer || !repeat)
		QueueKeyboardIRQ();

	mKeyCodeTimer = 1;
	mKeyCooldownTimer = 0;
}

uint64 ATPokeyEmulator::GetRawKeyMask() const {
	uint64 v = 0;

	for(int i=0; i<8; ++i)
		v += (uint64)(mKeyMatrix[i] & 0xFF) << (i*8);

	return v;
}

void ATPokeyEmulator::PushRawKey(uint8 c, bool immediate) {
	if (mb5200Mode)
		return;

	SetKeyboardModes(false, !immediate);
	mKeyQueue.clear();

	int row = (c >> 3) & 7;
	uint16 colbit = 1 << (c & 7);

	if (!immediate) {
		UpdateKeyMatrix(row, colbit, 0xFF);
	} else {
		// Stomp the entire main keyboard matrix with the newly pressed key (NOT the
		// extended matrix!). We need to update the effective matrix, too. Since we
		// only have one key active in the main matrix, it is not possible to introduce
		// phantoms in Ctrl/Shift/Break. It IS possible for Ctrl/Shift/Break to interfere
		// with the main matrix if two are held down; we handle Ctrl+Shift by a check on
		// the key code and ignore the Break conflict.
		for(int i=0; i<8; ++i)
			mKeyMatrix[i] &= 0xFF00;

		mKeyMatrix[row] |= colbit;

		memcpy(mEffectiveKeyMatrix, mKeyMatrix, sizeof mEffectiveKeyMatrix);

		// If we're in immediate mode, scan and debounce are enabled, and it's
		// not a key blocked by an inherent matrix conflict, push it now.
		if ((mSKCTL & 3) == 3 && (c & 0xE8) != 0xC0) {
			mSKSTAT &= ~0x04;
			mKBCODE = c;
		
			QueueKeyboardIRQ();
		}
	}
}

void ATPokeyEmulator::ReleaseRawKey(uint8 c, bool immediate) {
	if (mb5200Mode)
		return;

	SetKeyboardModes(false, !immediate);
	mKeyQueue.clear();

	const int row = (c >> 3) & 7;
	const uint16 colbit = 1 << (c & 7);
	if (!immediate) {
		UpdateKeyMatrix(row, colbit, 0);
	} else {
		if (mKeyMatrix[row] & colbit) {
			for(int i=0; i<8; ++i)
				mKeyMatrix[i] &= 0xFF00;

			memcpy(mEffectiveKeyMatrix, mKeyMatrix, sizeof mEffectiveKeyMatrix);

			mSKSTAT |= 0x04;
		}
	}
}

void ATPokeyEmulator::ReleaseAllRawKeys(bool immediate) {
	if (mb5200Mode)
		return;

	memset(mKeyMatrix, 0, sizeof mKeyMatrix);
	memset(mEffectiveKeyMatrix, 0, sizeof mEffectiveKeyMatrix);

	if (immediate) {
		mbKeyboardIRQPending = false;
		mSKSTAT |= 0x04;
	}

	SetKeyboardModes(mbCookedKeyMode, !immediate);
}

void ATPokeyEmulator::SetBreakKeyState(bool state, bool immediate) {
	if (mbBreakKeyState == state)
		return;

	mbBreakKeyState = state;

	if (immediate) {
		if (state)
			PushBreak();
	}

	// Break is on the $30-37 row with the KR2 column
	UpdateKeyMatrix(6, 0x100, state ? 0x100 : 0);
}

void ATPokeyEmulator::PushBreak() {
	mKeyQueue.clear();

	// The keyboard scan must be enabled for Break to be detected. However, debounce
	// doesn't matter.
	if (mSKCTL & 2)
		AssertBreakIRQ();
}

void ATPokeyEmulator::SetKeyMatrix(const bool matrix[64]) {
	if (matrix) {
		uint16 *dst = mKeyMatrix;
		const bool *src = matrix;

		for(int i=0; i<8; ++i) {
			uint16 v	= (src[0] ? 0x01 : 0x00)
						+ (src[1] ? 0x02 : 0x00)
						+ (src[2] ? 0x04 : 0x00)
						+ (src[3] ? 0x08 : 0x00)
						+ (src[4] ? 0x10 : 0x00)
						+ (src[5] ? 0x20 : 0x00)
						+ (src[6] ? 0x40 : 0x00)
						+ (src[7] ? 0x80 : 0x00);

			dst[i] = (dst[i] & 0xFF00) + v;
			src += 8;
		}
	} else
		memset(mKeyMatrix, 0, sizeof mKeyMatrix);

	UpdateEffectiveKeyMatrix();
}

template<uint8 activeChannel>
void ATPokeyEmulator::FireTimer() {
	mpRenderer->AddChannelEvent(activeChannel);

	if (activeChannel == 0) {
		if (mIRQEN & 0x01) {
			mIRQST &= ~0x01;
			AssertIrq(false);
		}

		// two tone
		if ((mSKCTL & 8) && mbSerialOutputState && !(mSKCTL & 0x80)) {
			// resync timer 2 to timer 1
			mCounter[1] = mAUDFP1[1];
			mCounterBorrow[1] = 0;
			SetupTimers(0x02);
		}
	}

	// count timer 2
	if (activeChannel == 1) {
		if (mIRQEN & 0x02) {
			mIRQST &= ~0x02;
			AssertIrq(false);
		}

		// two tone
		if (mSKCTL & 8) {
			// resync timer 1 to timer 2
			mCounter[0] = mAUDFP1[0];
			mCounterBorrow[0] = 0;
			SetupTimers(0x01);
		}

		if (mSerialOutputCounter) {
			if ((mSKCTL & 0x60) == 0x60)
				mpScheduler->SetEvent(2, this, kATPokeyEventSerialOutput, mpEventSerialOutput);
		}
	}

	// count timer 4
	if (activeChannel == 3) {
		if (mIRQEN & 0x04) {
			mIRQST &= ~0x04;
			AssertIrq(false);
		}

		if (mSKCTL & 0x30)
			OnSerialInputTick();

		if (mSerialOutputCounter) {
			switch(mSKCTL & 0x60) {
				case 0x20:
				case 0x40:
					mpScheduler->SetEvent(2, this, kATPokeyEventSerialOutput, mpEventSerialOutput);
					break;
			}
		}
	}
}

uint32 ATPokeyEmulator::UpdateLast15KHzTime() {
	return UpdateLast15KHzTime(ATSCHEDULER_GETTIME(mpScheduler));
}

uint32 ATPokeyEmulator::UpdateLast15KHzTime(uint32 t) {
	uint32 offset = t - mLast15KHzTime;

	if (offset >= 114)
		mLast15KHzTime += offset - offset % 114;

	return mLast15KHzTime;
}

uint32 ATPokeyEmulator::UpdateLast64KHzTime() {
	return UpdateLast64KHzTime(ATSCHEDULER_GETTIME(mpScheduler));
}

uint32 ATPokeyEmulator::UpdateLast64KHzTime(uint32 t) {
	uint32 offset = t - mLast64KHzTime;

	if (offset >= 28) {
		mLast64KHzTime += 28;
		offset -= 28;

		if (offset >= 28)
			mLast64KHzTime += offset - offset % 28;
	}

	return mLast64KHzTime;
}

void ATPokeyEmulator::UpdatePolyTime() {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	int polyDelta = (int)(t - mLastPolyTime);
	mPoly9Counter += polyDelta;
	mPoly17Counter += polyDelta;
	mLastPolyTime = t;

	if (mPoly9Counter >= 511)
		mPoly9Counter %= 511;

	if (mPoly17Counter >= 131071)
		mPoly17Counter %= 131071;
}

void ATPokeyEmulator::OnSerialInputTick() {
	if (!mSerialInputCounter)
		return;

	--mSerialInputCounter;

	if (!mSerialInputCounter) {
		// deassert serial input active
		mSKSTAT |= 0x02;

		mbSerialWaitingForStartBit = true;

		if ((mSKCTL & 0x10) && !mbLinkedTimers34) {
			mCounter[2] = mAUDFP1[2];
			SetupTimers(0x04);
		}

		if (mbSerInDeferredLoad)
			ProcessReceivedSerialByte();
	} else {
		if (!(mSKCTL & 0x30) && mSerialExtPeriod)
			mpScheduler->SetEvent(mSerialExtPeriod, this, kATPokeyEventSerialInput, mpEventSerialInput);
	}
}

void ATPokeyEmulator::OnSerialOutputTick() {
	--mSerialOutputCounter;

	// We've already transmitted the start bit (low), so we need to do data bits and then
	// stop bit (high).
	mbSerialOutputState = mSerialOutputCounter ? (mSerialOutputShiftRegister & (1 << (9 - (mSerialOutputCounter >> 1)))) != 0 : true;

	if (!mSerialOutputCounter) {
		FlushSerialOutput();

		if (mbSerOutValid) {
			mSerialOutputCounter = 20;
			mSerialOutputStartTime = mpScheduler->GetTick64();

			if (mSerOutBurstDeadline && ATSCHEDULER_GETTIME(mpScheduler) - mSerOutBurstDeadline >= uint32(0x80000000))
				mSerialOutputCounter = 1;

			mbSerialOutputState = true;
			mSerialOutputShiftRegister = mSEROUT;
			mbSerOutValid = false;
			mbSerShiftValid = true;

			if (mpCassette)
				mpCassette->PokeyBeginCassetteData(mSKCTL);

			// bit 3 is special and doesn't get cleared by IRQEN
			mIRQST |= 0x08;

			if (mIRQEN & 0x10)
				mIRQST &= ~0x10;
		} else
			mIRQST &= ~0x08;

		if (mIRQEN & ~mIRQST)
			AssertIrq(false);
		else
			NegateIrq(false);
	}

	// check if we must reset the tick for external clock
	if (mSerialOutputCounter && !(mSKCTL & 0x60)) {
		if (mSerialExtPeriod)
			mpScheduler->SetEvent(mSerialExtPeriod, this, kATPokeyEventSerialOutput, mpEventSerialOutput);
		else
			mpScheduler->UnsetEvent(mpEventSerialOutput);
	}
}

bool ATPokeyEmulator::IsSerialOutputClockRunning() const {
	switch(mSKCTL & 0x60) {
		default:
			VDNEVERHERE;
		case 0x00:		// external clock
			return mSerialExtPeriod != 0;

		case 0x20:		// timer 4 as transmit clock
		case 0x40:
			if (mSKCTL & 0x10) {
				// Asynchronous receive mode enabled, so clock only runs if receiving. For now, pretend
				// it's always halted for output purposes.
				return false;
			}

			// check if initialization mode is active
			if (mSKCTL & 3) {
				// nope, clocks are running
				return true;
			}

			// init mode is active -- check if 3+4 are linked
			if (mAUDCTL & 0x08) {
				// linked -- running if timer 3 is 1.79MHz
				return (mAUDCTL & 0x20) != 0;
			} else {
				// not linked -- timer 4 is halted
				return false;
			}
			break;

		case 0x60:		// timer 2 as transmit clock
			// check if initialization mode is active
			if (mSKCTL & 3) {
				// nope, clocks are running
				return true;
			}

			// init mode is active -- check if 1+2 are linked
			if (mAUDCTL & 0x10) {
				// linked -- running if timer 1 is 1.79MHz
				return (mAUDCTL & 0x40) != 0;
			} else {
				// not linked -- timer 2 is halted
				return false;
			}
			break;
	}
}

void ATPokeyEmulator::FlushSerialOutput() {
	if (!mbSerShiftValid)
		return;

	const uint8 originalCounter = mSerialOutputCounter;

	mbSerShiftValid = false;
	mSerialOutputCounter = 0;

	// check if we got out the start bit; if not, no byte would be noticed by
	// receivers
	if (mSerialOutputCounter >= 18)
		return;

	uint32 cyclesPerBit;
		
	switch(mSKCTL & 0x60) {
		default:
			VDNEVERHERE;
		case 0x00:		// external clock
			cyclesPerBit = mSerialExtPeriod;
			break;

		case 0x20:		// timer 4 as transmit clock
		case 0x40:
			cyclesPerBit = mTimerPeriod[3];
			break;

		case 0x60:		// timer 2 as transmit clock
			cyclesPerBit = mTimerPeriod[1];
			break;
	}

	cyclesPerBit += cyclesPerBit;

	if (mbTraceSIO) {
		ATConsoleTaggedPrintf("POKEY: Transmitted serial byte %02x to SIO bus at %u cycles/bit (%.1f baud)\n"
			, mSerialOutputShiftRegister
			, cyclesPerBit
			, (7159090.0f / 4.0f) / (float)cyclesPerBit
			);
	}

	if (g_ATLCSIOData) {
		if (mTraceByteIndex >= 1000 || !mTraceDirectionSend) {
			mTraceByteIndex = 0;
			mTraceDirectionSend = true;
		}

		g_ATLCSIOData("[%3u] Send     $%02X >         (@ %u cycles/bit / %.1f baud)\n", mTraceByteIndex++, mSerialOutputShiftRegister, cyclesPerBit, 7159090.0f / 4.0f / (float)cyclesPerBit);
	}

	bool burstOK = false;
	bool framingError = false;
	uint8 c = mSerialOutputShiftRegister;

	if (mSerialOutputCounter) {
		// Byte may have been truncated -- adjust it and the stop bit. Transmission
		// is LSB first, so stomp from MSBs down.
		uint8 truncationMask = 0xFF << ((18 - originalCounter) >> 1);

		c &= truncationMask;

		if (mbSerialNoiseEnabled)
			c |= truncationMask;

		// signal framing error if output is still low by stop bit time
		framingError = !mbSerialOutputState;
	}

	for(IATPokeySIODevice *dev : mDevices) {
		if (dev->PokeyWriteSIO(c, mbCommandLineState, cyclesPerBit, mSerialOutputStartTime, framingError))
			burstOK = true;
	}

	if (mpCassette)
		mpCassette->PokeyWriteCassetteData(mSerialOutputShiftRegister, cyclesPerBit);

	if (burstOK)
		mSerOutBurstDeadline = (ATSCHEDULER_GETTIME(mpScheduler) + cyclesPerBit*10) | 1;
	else
		mSerOutBurstDeadline = 0;
}

uint32 ATPokeyEmulator::GetSerialCyclesPerBitRecv() const {
	if (!(mSKCTL & 0x30))
		return mSerialExtPeriod;

	const uint32 divisor = mTimerPeriod[3];

	return divisor + divisor;
}

void ATPokeyEmulator::SetPotPos(unsigned idx, int pos) {
	if (pos > 228)
		pos = 228;

	if (pos < 1)
		pos = 1;

	mPotPositions[idx] = (uint8)pos;

	int hipos = pos * 114;
	if (hipos > 229)
		hipos = 229;

	if (hipos < 0)
		hipos = 0;

	mPotHiPositions[idx] = (uint8)hipos;
}

void ATPokeyEmulator::SetPotPosHires(unsigned idx, int pos, bool grounded) {
	int lopos = pos >> 16;
	if (lopos > 228)
		lopos = 228;

	if (lopos < 1)
		lopos = 1;

	mPotPositions[idx] = (uint8)lopos;

	int hipos = 255;
	if (!grounded) {
		hipos = (pos * 114) >> 16;

		if (hipos > 229)
			hipos = 229;

		if (hipos < 1)
			hipos = 1;
	}

	mPotHiPositions[idx] = (uint8)hipos;
}

void ATPokeyEmulator::AdvanceScanLine() {
	if (mbSerialRateChanged) {
		mbSerialRateChanged = false;

		if (mpCassette) {
			uint32 divisor = GetSerialCyclesPerBitRecv() >> 1;

			mpCassette->PokeyChangeSerialRate(divisor);
		}
	}

	if (mpAudioLog) {
		if (mpAudioLog->mRecordedCount < mpAudioLog->mMaxCount)
			GetAudioState(mpAudioLog->mpStates[mpAudioLog->mRecordedCount++]);
	}

	if (mpSlave)
		mpSlave->AdvanceScanLine();
}

void ATPokeyEmulator::AdvanceFrame(bool pushAudio, uint64 timestamp) {
	UpdatePots(0);

	if (mKeyCodeTimer) {
		if (!--mKeyCodeTimer) {
			mSKSTAT |= 0x04;

			mKeyCooldownTimer = 60;
		}
	} else if (mbSpeakerActive) {
		mKeyCooldownTimer = 60;
	} else {
		if (mKeyCooldownTimer)
			--mKeyCooldownTimer;

		if (!mKeyQueue.empty() && CanPushKey(mKeyQueue.front()))
			TryPushNextKey();
	}

	mbSpeakerActive = false;

	FlushAudio(pushAudio, timestamp);

	// Scan all of the deferred timers and push any that are too far behind. We need to do this to
	// prevent any of the timers from getting too far behind the clock (>30 bits). This calculation
	// is wrong for the low timer of a linked timer pair, but it turns out we don't use the start
	// value in that case; this module uses the high timer delay, and only the renderer does the
	// funky linked step.
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	for(int i=0; i<4; ++i) {
		if (!mbDeferredTimerEvents[i])
			continue;

		// Determine what kind of lag we want to check for on the deferred timer start. This must be
		// at least 8893, which is the number of times that a 1.79MHz frequency 0 timer can count in
		// a PAL frame. However, a slow 15KHz linked timer can take as long as 7.4M clocks to cycle.
		// We keep shifting up the value until it's a bit more than a second. We do need to keep this
		// value a multiple of the original, though.
		uint32 bigPeriod = mDeferredTimerPeriods[i];

		while(bigPeriod < 2097152) {
			bigPeriod <<= 4;
		}

		// The deferred timers may start in the future for one that hasn't hit the first tick,
		// so we must check for that case.
		if ((sint32)(t - mDeferredTimerStarts[i]) <= (sint32)bigPeriod)
			continue;

		// Bump the timer up by a number of periods so it catches up. 
		mDeferredTimerStarts[i] += bigPeriod;
	}

	// Catch up 15KHz and 64Khz clocks.
	UpdateLast15KHzTime();
	UpdateLast64KHzTime();

	// Catch up poly counters.
	UpdatePolyTime();

	// Catch up the external clock, to avoid glitches at 2^32
	if (mSerialExtPeriod) {
		uint32 extsince = t - mSerialExtBaseTime;

		if (extsince >= 0x40000000) {
			extsince += mSerialExtPeriod - 1;
			mSerialExtBaseTime += extsince - extsince % mSerialExtPeriod;
		}
	}
}

void ATPokeyEmulator::OnScheduledEvent(uint32 id) {
	switch(id) {
		case kATPokeyEventKeyboardIRQ:
			mpKeyboardIRQEvent = nullptr;

			if (mbKeyboardIRQPending) {
				mbKeyboardIRQPending = false;

				if (mSKCTL & 2)
					AssertKeyboardIRQ();
			}
			break;

		case kATPokeyEventKeyboardScan:
			{
				mpKeyboardScanEvent = nullptr;

				if ((mb5200Mode || mbKeyboardScanEnabled) && (mSKCTL & 2)) {
					mpKeyboardScanEvent = mpScheduler->AddEvent(114, this, kATPokeyEventKeyboardScan);

					const uint8 kc = mKeyScanCode++ & 0x3F;

					// POKEY's keyboard circuitry is a two-bit state machine with a keyboard line and
					// a comparator as input, and result/comparator latch signals as output. The state
					// machine works as follows:
					//
					//	state 0 (00): waiting for key
					//		key pressed -> state 1, load compare latch
					//
					//	state 1 (01): waiting for key bounce
					//		key pressed, not same as compare latch -> state 0
					//		key not pressed, same as compare latch -> state 0
					//		key pressed, same as compare latch -> state 2
					//
					//	state 2 (11): waiting for key release
					//		key not pressed, same as compare latch -> state 3
					//
					//	state 3 (10): waiting for key debounce
					//		key pressed, same as compare latch -> state 2
					//		key not pressed, same as compare latch -> state 0
					//
					// If keyboard debounce (SKCTL bit 1) is disabled, the compare signal is always
					// true. SKSTAT bit 2 reads 1 for states 0 and 1, and 0 for states 2 and 3.
					//
					// The state machine and the binary counter that run the external keyboard logic
					// both run at HBLANK (15KHz) rate; this logic therefore cycles through the entire
					// keyboard 245.3 times a second (NTSC), or 4.1 times a frame. It takes two HBLANKs
					// for a key to be recognized, and since keys are mirrored four times on the 5200
					// keypad, this causes IRQs to be sent 8 times a frame.

					switch(kc) {
						case 0x00:
							mbControlKeyLatchedState = (mEffectiveKeyMatrix[0] & 0x100) != 0;
							break;

						case 0x10:
							{
								const bool shiftState = (mEffectiveKeyMatrix[2] & 0x100) != 0;

								if (mbShiftKeyLatchedState != shiftState) {
									mbShiftKeyLatchedState = shiftState;

									if (shiftState)
										mSKSTAT &= ~0x08;
									else
										mSKSTAT |= 0x08;
								}
							}
							break;

						case 0x30:
							{
								const bool breakKeyState = (mEffectiveKeyMatrix[6] & 0x100) != 0;

								if (mbBreakKeyLatchedState != breakKeyState) {
									mbBreakKeyLatchedState = breakKeyState;

									if (breakKeyState)
										AssertBreakIRQ();
								}
							}
							break;
					}

					const bool keyState = (mEffectiveKeyMatrix[kc >> 3] & (1 << (kc & 7))) != 0;

					switch(mKeyScanState) {
						case 0:		// waiting for key
							if (keyState) {
								mKeyScanLatch = kc;
								mKeyScanState = 1;
							}
							break;

						case 1:		// waiting for key bounce
							if (keyState) {
								if (kc == mKeyScanLatch || !(mSKCTL & 1)) {
									// same key down -- fire IRQ, save key, and continue
									mKeyScanState = 2;
									mKBCODE = kc | (mbShiftKeyLatchedState ? 0x40 : 0x00) | (mbControlKeyLatchedState ? 0x80 : 0x00);
									mSKSTAT &= ~0x04;
									if (mIRQEN & 0x40) {
										// If keyboard IRQ is already active, set the keyboard overrun bit.
										if (!(mIRQST & 0x40))
											mSKSTAT &= ~0x40;
										else {
											mIRQST &= ~0x40;
											AssertIrq(false);
										}
									}						
								} else {
									// different key down -- restart
									mKeyScanState = 0;
								}
							} else if (kc == mKeyScanLatch || !(mSKCTL & 1)) {
								// latched key no longer down -- restart
								mKeyScanState = 0;
							}
							break;

						case 2:		// waiting for key up
							if (kc == mKeyScanLatch || !(mSKCTL & 1)) {
								if (!keyState) {
									mKeyScanState = 3;
								}
							}
							break;

						case 3:		// waiting for debounce
							if (kc == mKeyScanLatch || !(mSKCTL & 1)) {
								if (keyState)
									mKeyScanState = 2;
								else {
									mSKSTAT |= 0x04;
									mKeyScanState = 0;
								}
							}
							break;
					}
				}
			}
			break;

		case kATPokeyEventTimer1Borrow:
			mpTimerBorrowEvents[0] = NULL;
			FireTimer<0>();

			if (!mbLinkedTimers12) {
				mCounter[0] = mAUDFP1[0];
			} else {
				// If we are operating at 1.79MHz, three cycles have already elapsed from the underflow to
				// when the borrow goes through.
				mCounter[0] = mbFastTimer1 ? 253 : 256;
			}

			mCounterBorrow[0] = 0;
			SetupTimers(0x01);
			break;

		case kATPokeyEventTimer2Borrow:
			mpTimerBorrowEvents[1] = NULL;
			FireTimer<1>();

			mCounterBorrow[1] = 0;
			mCounter[1] = mAUDFP1[1];
			if (mbLinkedTimers12) {
				mCounter[0] = mAUDFP1[0];
				mCounterBorrow[0] = 0;
				SetupTimers(0x03);
			} else
				SetupTimers(0x02);
			break;

		case kATPokeyEventTimer3Borrow:
			mpTimerBorrowEvents[2] = NULL;
			FireTimer<2>();

			if (!mbLinkedTimers34) {
				mCounter[2] = mAUDFP1[2];
			} else {
				// If we are operating at 1.79MHz, three cycles have already elapsed from the underflow to
				// when the borrow goes through.
				mCounter[2] = mbFastTimer3 ? 253 : 256;
			}

			mCounterBorrow[2] = 0;
			SetupTimers(0x04);
			break;

		case kATPokeyEventTimer4Borrow:
			mpTimerBorrowEvents[3] = NULL;
			FireTimer<3>();

			mCounter[3] = mAUDFP1[3];
			mCounterBorrow[3] = 0;
			if (mbLinkedTimers34) {
				mCounter[2] = mAUDFP1[2];
				mCounterBorrow[2] = 0;
				SetupTimers(0x0C);
			} else
				SetupTimers(0x08);
			break;

		case kATPokeyEventResetTimers:
			mpResetTimersEvent = NULL;
			mCounter[0] = mAUDFP1[0];
			mCounter[1] = mAUDFP1[1];
			mCounter[2] = mAUDFP1[2];
			mCounter[3] = mAUDFP1[3];

			mpRenderer->ResetTimers();

			for(int i=0; i<4; ++i) {
				mCounterBorrow[i] = 0;

				if (mpTimerBorrowEvents[i]) {
					mpScheduler->RemoveEvent(mpTimerBorrowEvents[i]);
					mpTimerBorrowEvents[i] = NULL;
				}
			}

			SetupTimers(0x0f);
			break;

		case kATPokeyEventSerialOutput:
			mpEventSerialOutput = NULL;

			if (mSerialOutputCounter)
				OnSerialOutputTick();
			break;

		case kATPokeyEventSerialInput:
			mpEventSerialInput = NULL;

			if (mSerialInputCounter)
				OnSerialInputTick();
			break;
	}
}

template<int channel>
void ATPokeyEmulator::RecomputeTimerPeriod() {

	const bool fastTimer = (channel == 0) ? mbFastTimer1 : (channel == 2) ? mbFastTimer3 : false;
	const bool hiLinkedTimer = channel == 1 ? mbLinkedTimers12 : channel == 3 ? mbLinkedTimers34 : false;
	const bool loLinkedTimer = channel == 0 ? mbLinkedTimers12 : channel == 2 ? mbLinkedTimers34 : false;

	uint32 period;
	if (hiLinkedTimer) {
		const bool fastLinkedTimer = (channel == 1) ? mbFastTimer1 : (channel == 3) ? mbFastTimer3 : false;

		period = ((uint32)mAUDF[channel] << 8) + mAUDFP1[channel - 1];

		if (fastLinkedTimer) {
			period += 6;
		} else if (mbUse15KHzClock)
			period *= 114;
		else
			period *= 28;
	} else {
		period = mAUDFP1[channel];

		if (fastTimer)
			period += 3;
		else if (mbUse15KHzClock)
			period *= 114;
		else
			period *= 28;

		if (loLinkedTimer) {
			uint32 fullPeriod;

			if (fastTimer)
				fullPeriod = 256;
			else if (mbUse15KHzClock)
				fullPeriod = 256 * 114;
			else
				fullPeriod = 256 * 28;

			mTimerFullPeriod[channel >> 1] = fullPeriod;
		}
	}

	mTimerPeriod[channel] = period;
}

void ATPokeyEmulator::RecomputeAllowedDeferredTimers() {
	// Timer 2 IRQ, two-tone mode, or timer 2 as serial output clock prohibits deferring timer 2.
	mbAllowDeferredTimer[1] = !kForceActiveTimers
		&& !(mIRQEN & 0x02)
		&& !(mSKCTL & 8)
		&& (mSKCTL & 0x60) != 0x60;

	// Timer 1 IRQ or two-tone mode prohibits deferring timer 1.
	mbAllowDeferredTimer[0] = !kForceActiveTimers
		&& !(mIRQEN & 0x01)
		&& !(mSKCTL & 8);

	// Timer 4 IRQ, asynchronous receive mode, or using timer 4 as transmit clock prohibits deferring timer 4.
	mbAllowDeferredTimer[3] = !kForceActiveTimers
		&& !(mIRQEN & 0x04)
		&& !(mSKCTL & 0x10)
		&& (mSKCTL & 0x60) != 0x20
		&& (mSKCTL & 0x60) != 0x40;

	// Timer 3 deferring is ordinarily always possible.
	mbAllowDeferredTimer[2] = !kForceActiveTimers;

	// Check if timers are linked; both timers must be able to defer together.
	if (mbLinkedTimers12) {
		if (!mbAllowDeferredTimer[0] || !mbAllowDeferredTimer[1]) {
			mbAllowDeferredTimer[0] = false;
			mbAllowDeferredTimer[1] = false;
		}
	}

	if (mbLinkedTimers34) {
		if (!mbAllowDeferredTimer[2] || !mbAllowDeferredTimer[3]) {
			mbAllowDeferredTimer[2] = false;
			mbAllowDeferredTimer[3] = false;
		}
	}
}

template<int channel>
void ATPokeyEmulator::UpdateTimerCounter() {
	VDASSERT(mCounter[0] > 0);
	VDASSERT(mCounterBorrow[0] >= 0);

	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mCounterBorrow[channel] = 0;

	const bool fastTimer = channel == 0 || (channel == 1 && mbLinkedTimers12) ? mbFastTimer1
		: channel == 2 || (channel == 3 && mbLinkedTimers34) ? mbFastTimer3 : false;
	const bool hiLinkedTimer = channel == 1 ? mbLinkedTimers12 : channel == 3 ? mbLinkedTimers34 : false;
	const bool loLinkedTimer = channel == 0 ? mbLinkedTimers12 : channel == 2 ? mbLinkedTimers34 : false;

	if (loLinkedTimer) {
		const int hiChannel = channel + 1;

		// Compute the number of ticks until the next borrow event. The borrow occurs three cycles after
		// the timer channel underflows; 16-bit channels take 6 cycles because the borrows from the two
		// channels are cascaded. If we have a borrow event, we can simply use that time directly.

		int ticksLeft;
		if (mbDeferredTimerEvents[hiChannel]) {
			if ((sint32)(t - mDeferredTimerStarts[hiChannel]) < 0)
				ticksLeft = mDeferredTimerStarts[hiChannel] - t;
			else
				ticksLeft = mDeferredTimerPeriods[hiChannel] - (t - mDeferredTimerStarts[hiChannel]) % mDeferredTimerPeriods[hiChannel];
		} else if (mpTimerBorrowEvents[hiChannel])
			ticksLeft = mpScheduler->GetTicksToEvent(mpTimerBorrowEvents[hiChannel]);
		else
			return;

		// Compute the low timer offset from the high timer offset.
		if (ticksLeft <= 3) {
			// Three ticks or less means that the high timer is borrowing, so we're free running. We've
			// already run through the three ticks of low timer borrow.
			if (fastTimer)
				mCounter[channel] = 253 - ticksLeft;
			else
				mCounter[channel] = 256;
		} else {
			ticksLeft = (uint32)(ticksLeft - 3) % mTimerFullPeriod[channel >> 1];

			if (ticksLeft <= 3) {
				// Low timer borrow is in progress.
				mCounterBorrow[channel] = ticksLeft;

				if (fastTimer)
					mCounter[channel] = 256 - ticksLeft;
				else
					mCounter[channel] = 256;
			} else {
				ticksLeft -= 3;

				if (!fastTimer) {
					if (mbUse15KHzClock)
						ticksLeft = (ticksLeft + 113) / 114;
					else
						ticksLeft = (ticksLeft + 27) / 28;
				}

				mCounter[channel] = ticksLeft;
			}
		}
	} else {
		// Compute the number of ticks until the next borrow event. The borrow occurs three cycles after
		// the timer channel underflows; 16-bit channels take 6 cycles because the borrows from the two
		// channels are cascaded. If we have a borrow event, we can simply use that time directly.

		int ticksLeft;
		if (mbDeferredTimerEvents[channel]) {
			if ((sint32)(t - mDeferredTimerStarts[channel]) < 0)
				ticksLeft = mDeferredTimerStarts[channel] - t;
			else
				ticksLeft = mDeferredTimerPeriods[channel] - (t - mDeferredTimerStarts[channel]) % mDeferredTimerPeriods[channel];
		} else if (mpTimerBorrowEvents[channel])
			ticksLeft = mpScheduler->GetTicksToEvent(mpTimerBorrowEvents[channel]);
		else
			return;

		// If we're three ticks or less away, we have a borrow pending. At fast clock the timer will begin
		// counting down immediately during the borrow; this is important to maintain 16-bit, 1.79MHz accuracy.
		// At slow clock this can only happen once if the slow clock just happens to fall at the right time
		// (can happen if 1.79MHz clocking is disabled).
		//
		// TODO: Handle slow tick happening in borrow land.

		if (ticksLeft <= 3) {
			mCounterBorrow[channel] = ticksLeft;

			if (fastTimer && !hiLinkedTimer)
				mCounter[channel] = 256 - ticksLeft;
			else
				mCounter[channel] = 256;
		} else {
			if (hiLinkedTimer) {
				if (fastTimer)
					mCounter[channel] = (ticksLeft + 255) >> 8;
				else if (mbUse15KHzClock)
					mCounter[channel] = (ticksLeft + 114*256 - 1) / (114*256);
				else
					mCounter[channel] = (ticksLeft + 28*256 - 1) / (28*256);
			} else {
				ticksLeft -= 3;

				if (fastTimer)
					mCounter[channel] = ticksLeft;
				else if (mbUse15KHzClock)
					mCounter[channel] = (ticksLeft + 113) / 114;
				else
					mCounter[channel] = (ticksLeft + 27) / 28;
			}
		}
	}

	VDASSERT(mCounter[channel] > 0 && mCounter[channel] <= 256);
	VDASSERT(mCounterBorrow[channel] >= 0);
}

void ATPokeyEmulator::SetupTimers(uint8 channels) {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	int cyclesToNextSlowTick;

	if (mbUse15KHzClock) {
		cyclesToNextSlowTick = 114 - (t - UpdateLast15KHzTime());

		if (cyclesToNextSlowTick)
			cyclesToNextSlowTick -= 114;
	} else {
		int slowTickOffset = t - UpdateLast64KHzTime();

		cyclesToNextSlowTick = (28 - slowTickOffset);

		if (cyclesToNextSlowTick)
			cyclesToNextSlowTick -= 28;
	}

	const bool slowTickValid = (mSKCTL & 3) != 0;

	VDASSERT(!slowTickValid || (cyclesToNextSlowTick >= -114 && cyclesToNextSlowTick <= 0));

	if (channels & 0x01) {
		mpScheduler->UnsetEvent(mpTimerBorrowEvents[0]);
		FlushDeferredTimerEvents(0);

		if (!mbFastTimer1 && !slowTickValid) {
			// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
			// fire the borrow event if we have one pending.
			if (mCounterBorrow[0])
				mpTimerBorrowEvents[0] = mpScheduler->AddEvent(mCounterBorrow[0], this, kATPokeyEventTimer1Borrow);
		} else {
			// Computer number of ticks until the next borrow event. If we have a borrow pending,
			// that's easy; otherwise, we have to look at the current counter value, slow tick offset,
			// and mode.
			uint32 ticks = mCounterBorrow[0];
			
			if (!ticks) {
				ticks = mCounter[0];

				if (!mbFastTimer1) {
					if (mbUse15KHzClock)
						ticks = ticks * 114;
					else
						ticks = ticks * 28;

					ticks += cyclesToNextSlowTick;
				}

				// Borrow takes place three cycles after underflow.
				ticks += 3;
			}

			VDASSERT((sint32)ticks > 0);

			if (!mbAllowDeferredTimer[0])
				mpTimerBorrowEvents[0] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer1Borrow);
			else if (mbLinkedTimers12) {
				const uint32 loTime = t + ticks;
				const uint32 hiTime = loTime + 3 + mTimerFullPeriod[0] * (mCounter[1] - 1);

				SetupDeferredTimerEventsLinked(0, loTime, mTimerFullPeriod[0], hiTime, mTimerPeriod[1], mTimerPeriod[0] - 3);
			} else
				SetupDeferredTimerEvents(0, t + ticks, mTimerPeriod[0]);
		}
	}

	if (channels & 0x02) {
		mpScheduler->UnsetEvent(mpTimerBorrowEvents[1]);
		FlushDeferredTimerEvents(1);

		if (mbLinkedTimers12) {
			if (!mbFastTimer1 && !slowTickValid) {
				// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
				// fire the borrow event if we have one pending.
				if (mCounterBorrow[1])
					mpTimerBorrowEvents[1] = mpScheduler->AddEvent(mCounterBorrow[1], this, kATPokeyEventTimer2Borrow);
			} else {
				// Computer number of ticks until the next borrow event. If we have a borrow pending,
				// that's easy; otherwise, we have to look at the current counter value, slow tick offset,
				// and mode.
				uint32 ticks = mCounterBorrow[1];
				
				if (!ticks) {
					ticks = mCounterBorrow[0];

					if (!ticks) {
						ticks = mCounter[0];

						if (!mbFastTimer1) {
							if (mbUse15KHzClock)
								ticks = ticks * 114;
							else
								ticks = ticks * 28;

							ticks += cyclesToNextSlowTick;
						}

						ticks += 3;
					}

					if (mbFastTimer1)
						ticks += (mCounter[1] - 1) << 8;
					else if (mbUse15KHzClock)
						ticks += (mCounter[1] - 1) * (256 * 114);
					else
						ticks += (mCounter[1] - 1) * (256 * 28);

					// Borrow takes place three cycles after underflow.
					ticks += 3;
				}

				VDASSERT((sint32)ticks > 0);

				if (!mbAllowDeferredTimer[1])
					mpTimerBorrowEvents[1] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer2Borrow);
				else
					SetupDeferredTimerEvents(1, t + ticks, mTimerPeriod[1]);

			}
		} else {
			if (!slowTickValid) {
				// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
				// fire the borrow event if we have one pending.
				if (mCounterBorrow[1])
					mpTimerBorrowEvents[1] = mpScheduler->AddEvent(mCounterBorrow[1], this, kATPokeyEventTimer2Borrow);
			} else {
				// Computer number of ticks until the next borrow event. If we have a borrow pending,
				// that easy; otherwise, we have to look at the current counter value, slow tick offset,
				// and mode.
				uint32 ticks = mCounterBorrow[1];
				
				if (!ticks) {
					ticks = mCounter[1];

					if (mbUse15KHzClock)
						ticks = ticks * 114;
					else
						ticks = ticks * 28;

					ticks += cyclesToNextSlowTick;

					// Borrow takes place three cycles after underflow.
					ticks += 3;
				}

				VDASSERT((sint32)ticks > 0);

				if (!mbAllowDeferredTimer[1]) {
					mpTimerBorrowEvents[1] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer2Borrow);
				} else {
					SetupDeferredTimerEvents(1, t + ticks, mTimerPeriod[1]);
				}
			}
		}
	}

	if ((mSKCTL & 0x10) && mbSerialWaitingForStartBit) {
		if (channels & 0x04) {
			mpScheduler->UnsetEvent(mpTimerBorrowEvents[2]);
			FlushDeferredTimerEvents(2);

			if (mCounterBorrow[2])
				mpTimerBorrowEvents[2] = mpScheduler->AddEvent(mCounterBorrow[2], this, kATPokeyEventTimer3Borrow);
		}

		if (channels & 0x08) {
			mpScheduler->UnsetEvent(mpTimerBorrowEvents[3]);
			FlushDeferredTimerEvents(3);

			if (mCounterBorrow[3])
				mpTimerBorrowEvents[3] = mpScheduler->AddEvent(mCounterBorrow[3], this, kATPokeyEventTimer4Borrow);
		}
	} else {
		if (channels & 0x04) {
			mpScheduler->UnsetEvent(mpTimerBorrowEvents[2]);
			FlushDeferredTimerEvents(2);

			if (!mbFastTimer3 && !slowTickValid) {
				// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
				// fire the borrow event if we have one pending.
				if (mCounterBorrow[2])
					mpTimerBorrowEvents[2] = mpScheduler->AddEvent(mCounterBorrow[2], this, kATPokeyEventTimer3Borrow);
			} else {
				// Computer number of ticks until the next borrow event. If we have a borrow pending,
				// that's easy; otherwise, we have to look at the current counter value, slow tick offset,
				// and mode.
				uint32 ticks = mCounterBorrow[2];
				
				if (!ticks) {
					ticks = mCounter[2];

					if (!mbFastTimer3) {
						if (mbUse15KHzClock)
							ticks = ticks * 114;
						else
							ticks = ticks * 28;

						ticks += cyclesToNextSlowTick;
					}

					// Borrow takes place three cycles after underflow.
					ticks += 3;
				}

				VDASSERT((sint32)ticks > 0);

				// Check if we need an active event or if we can go to deferred mode. We only need an
				// active event if timers are linked, timer 1 IRQ is enabled, or two-tone mode is active.
				if (!mbAllowDeferredTimer[2])
					mpTimerBorrowEvents[2] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer3Borrow);
				else if (mbLinkedTimers34) {
					const uint32 loTime = t + ticks;
					const uint32 hiTime = loTime + 3 + mTimerFullPeriod[1] * (mCounter[3] - 1);

					SetupDeferredTimerEventsLinked(2, loTime, mTimerFullPeriod[1], hiTime, mTimerPeriod[3], mTimerPeriod[2]);
				} else
					SetupDeferredTimerEvents(2, t + ticks, mTimerPeriod[2]);
			}
		}

		if (channels & 0x08) {
			mpScheduler->UnsetEvent(mpTimerBorrowEvents[3]);
			FlushDeferredTimerEvents(3);

			if (mbLinkedTimers34) {
				if (!mbFastTimer3 && !slowTickValid) {
					// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
					// fire the borrow event if we have one pending.
					if (mCounterBorrow[3])
						mpTimerBorrowEvents[3] = mpScheduler->AddEvent(mCounterBorrow[3], this, kATPokeyEventTimer4Borrow);
				} else {
					// Computer number of ticks until the next borrow event. If we have a borrow pending,
					// that's easy; otherwise, we have to look at the current counter value, slow tick offset,
					// and mode.
					uint32 ticks = mCounterBorrow[3];
					
					if (!ticks) {
						ticks = mCounterBorrow[2];

						if (!ticks) {
							ticks = mCounter[2];

							if (!mbFastTimer3) {
								if (mbUse15KHzClock)
									ticks = ticks * 114;
								else
									ticks = ticks * 28;

								ticks += cyclesToNextSlowTick;
							}

							ticks += 3;
						}

						if (mbFastTimer3)
							ticks += (mCounter[3] - 1) << 8;
						else if (mbUse15KHzClock)
							ticks += (mCounter[3] - 1) * (114 * 256);
						else
							ticks += (mCounter[3] - 1) * (28 * 256);

						// Borrow takes place three cycles after underflow.
						ticks += 3;
					}

					VDASSERT(ticks > 0);

					if (!mbAllowDeferredTimer[3])
						mpTimerBorrowEvents[3] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer4Borrow);
					else
						SetupDeferredTimerEvents(3, t + ticks, mTimerPeriod[3]);
				}
			} else {
				if (!slowTickValid) {
					// 15/64KHz clock is stopped and we're not running at 1.79MHz. In this case, we still
					// fire the borrow event if we have one pending.
					if (mCounterBorrow[3])
						mpTimerBorrowEvents[3] = mpScheduler->AddEvent(mCounterBorrow[3], this, kATPokeyEventTimer4Borrow);
				} else {
					// Computer number of ticks until the next borrow event. If we have a borrow pending,
					// that easy; otherwise, we have to look at the current counter value, slow tick offset,
					// and mode.
					uint32 ticks = mCounterBorrow[3];
					
					if (!ticks) {
						ticks = mCounter[3];

						if (mbUse15KHzClock)
							ticks = ticks * 114;
						else
							ticks = ticks * 28;

						ticks += cyclesToNextSlowTick;

						// Borrow takes place three cycles after underflow.
						ticks += 3;
					}

					VDASSERT(ticks > 0);

					// We only need the timer 4 event if the timers are linked, the timer 4 IRQ is enabled,
					// asynchronous receive mode is enabled, or timer 4 is being used as the output clock.
					if (!mbAllowDeferredTimer[3]) {
						mpTimerBorrowEvents[3] = mpScheduler->AddEvent(ticks, this, kATPokeyEventTimer4Borrow);
					} else {
						SetupDeferredTimerEvents(3, t + ticks, mTimerPeriod[3]);
					}
				}
			}
		}
	}
}

void ATPokeyEmulator::FlushDeferredTimerEvents(int channel) {
	if (!mbDeferredTimerEvents[channel])
		return;

	// get current time
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mbDeferredTimerEvents[channel] = false;

	mpRenderer->ClearChannelDeferredEvents(channel, t);
}

void ATPokeyEmulator::SetupDeferredTimerEvents(int channel, uint32 t0, uint32 period) {
	VDASSERT(!mbDeferredTimerEvents[channel]);
	mbDeferredTimerEvents[channel] = true;
	mDeferredTimerStarts[channel] = t0;
	mDeferredTimerPeriods[channel] = period;

	mpRenderer->SetChannelDeferredEvents(channel, t0, period);
}

void ATPokeyEmulator::SetupDeferredTimerEventsLinked(int channel, uint32 t0, uint32 period, uint32 hit0, uint32 hiperiod, uint32 hilooffset) {
	VDASSERT(!mbDeferredTimerEvents[channel]);
	mbDeferredTimerEvents[channel] = true;
	mDeferredTimerStarts[channel] = t0;
	mDeferredTimerPeriods[channel] = period;

	mpRenderer->SetChannelDeferredEventsLinked(channel, t0, period, hit0, hiperiod, hilooffset);
}

uint8 ATPokeyEmulator::DebugReadByte(uint8 reg) const {
	reg &= mAddressMask;

	switch(reg) {
		case 0x00:	// $D200 POT0
		case 0x01:	// $D201 POT1
		case 0x02:	// $D202 POT2
		case 0x03:	// $D203 POT3
		case 0x04:	// $D204 POT4
		case 0x05:	// $D205 POT5
		case 0x06:	// $D206 POT6
		case 0x07:	// $D207 POT7
			return const_cast<ATPokeyEmulator *>(this)->ReadByte(reg);
		case 0x08:	// $D208 ALLPOT
			const_cast<ATPokeyEmulator *>(this)->UpdatePots(0);
			return mALLPOT;
		case 0x09:	// $D209 KBCODE
			return mKBCODE;
		case 0x0A:	// $D20A RANDOM
			return const_cast<ATPokeyEmulator *>(this)->ReadByte(reg);
		case 0x0D:	// $D20D SERIN
			return mSERIN;
		case 0x0E:
			return mIRQST;
		case 0x0F:
			return const_cast<ATPokeyEmulator *>(this)->ReadByte(reg);
	}

	if (reg & 0x10) {
		if (mpSlave)
			return mpSlave->DebugReadByte(reg & 0x0f);
		else
			return DebugReadByte(reg & 0x0f);
	}

	return 0xFF;
}

uint8 ATPokeyEmulator::ReadByte(uint8 reg) {
	reg &= mAddressMask;

	switch(reg) {
		case 0x00:	// $D200 POT0
		case 0x01:	// $D201 POT1
		case 0x02:	// $D202 POT2
		case 0x03:	// $D203 POT3
		case 0x04:	// $D204 POT4
		case 0x05:	// $D205 POT5
		case 0x06:	// $D206 POT6
		case 0x07:	// $D207 POT7
			UpdatePots(0);

			if (mALLPOT & (1 << reg)) {
				uint8 count = mPotMasterCounter;

				// If we are in fast pot mode, we need to adjust the value that
				// comes back. This is because the read occurs at a point where
				// the count is unstable. The permutation below was determined
				// on real hardware by reading POT1 at varying cycle offsets.
				// Note that while all intermediate values read are even, the
				// final count can be odd, so we must not apply this to latched
				// values. Latched values can also vary but the mechanism is
				// TBD.
				if (mSKCTL & 4) {
					const uint8 kDeltaVec[16] = { 0, 1, 0, 3, 0, 1, 0, 7, 0, 1, 0, 3, 0, 1, 0, 15 };
					count ^= kDeltaVec[count & 15];
				}

				return count;
			}

			return mPotLatches[reg];

		case 0x08:	// $D208 ALLPOT
			UpdatePots(0);
			return mALLPOT;
		case 0x09:	// $D209 KBCODE
			return mKBCODE;
		case 0x0A:	// $D20A RANDOM
			{
				const bool initMode = !(mSKCTL & 3);
				uint8 forceMask = 0;

				if (initMode) {
					uint64 offset = mpScheduler->GetTick64() - mPolyShutOffTime;

					if (offset > 10)
						return 0xFF;

					if (offset)
						forceMask = (uint8)(0xFFE00 >> (int)(uint32)offset);
				}

				UpdatePolyTime();

				const uint8 *src = mAUDCTL & 0x80 ? &mpTables->mPolyBuffer[mPoly9Counter] : &mpTables->mPolyBuffer[mPoly17Counter];
				uint8 v = 0;

				if (mAUDCTL & 0x80) {
					for(int i=7; i>=0; --i)
						v = (v + v) + ((src[i] & 2) >> 1);
				} else {
					for(int i=7; i>=0; --i)
						v = (v + v) + (src[i] & 1);
				}

				return ~v | forceMask;
			}
		case 0x0D:	// $D20D SERIN
			{
				uint8 c = mSERIN;

				if (mbTraceSIO)
					ATConsoleTaggedPrintf("POKEY: Reading SERIN value %02x (shiftreg: %02x)\n", c, mSerialInputShiftRegister);

				if (mbSerInBurstPendingData) {
					mbSerInBurstPendingData = false;

					if (!mbSerInBurstPendingIRQ1 && !mbSerInBurstPendingIRQ2) {
						for(IATPokeySIODevice *dev : mDevices) {
							if (mbSerInBurstPendingData)
								break;

							dev->PokeySerInReady();
						}
					}
				}

				return c;
			}
			break;

		case 0x0E:	// $D20E IRQST
			return mIRQST;

		case 0x0F:
			{
				uint8 c = mSKSTAT;

				if (mbSerialSimulateInputPort) {
					uint32 dt = ATSCHEDULER_GETTIME(mpScheduler) - mSerialSimulateInputBaseTime;
					uint32 bitidx = dt / mSerialSimulateInputCyclesPerBit;

					if (bitidx >= 10)
						mbSerialSimulateInputPort = false;
					else {
						c &= 0xef;

						if (mSerialSimulateInputData & (1 << bitidx))
							c |= 0x10;
					}
				}

				return c;
			}
			break;

		default:
//			__debugbreak();
			break;
	}

	if (reg & 0x10)
		return mpSlave->ReadByte(reg & 0x0f);

	return 0xFF;
}

void ATPokeyEmulator::WriteByte(uint8 reg, uint8 value) {
	reg &= mAddressMask;

	mState.mReg[reg] = value;

	switch(reg) {
		case 0x00:	// $D200 AUDF1
			if (mAUDF[0] != value) {
				mAUDF[0] = value;
				mAUDFP1[0] = (int)value + 1;

				RecomputeTimerPeriod<0>();
				
				if (mbLinkedTimers12) {
					RecomputeTimerPeriod<1>();

					// Both counters must be updated before we run SetupTimers().
					UpdateTimerCounter<0>();
					UpdateTimerCounter<1>();
					SetupTimers(0x03);
				} else {
					UpdateTimerCounter<0>();
					SetupTimers(0x01);
				}
			}
			break;
		case 0x01:	// $D201 AUDC1
			if (mAUDC[0] != value) {
				mAUDC[0] = value;
				mpRenderer->SetAUDCx(0, value);
			}
			break;
		case 0x02:	// $D202 AUDF2
			if (mAUDF[1] != value) {
				mAUDF[1] = value;
				mAUDFP1[1] = (int)value + 1;

				RecomputeTimerPeriod<1>();

				if (mbLinkedTimers12) {
					// Both counters must be updated before we run SetupTimers().
					UpdateTimerCounter<0>();
					UpdateTimerCounter<1>();
					SetupTimers(0x03);
				} else {
					UpdateTimerCounter<1>();
					SetupTimers(0x02);
				}
			}
			break;
		case 0x03:	// $D203 AUDC2
			if (mAUDC[1] != value) {
				mAUDC[1] = value;
				mpRenderer->SetAUDCx(1, value);
			}
			break;
		case 0x04:	// $D204 AUDF3
			if (mAUDF[2] != value) {
				mAUDF[2] = value;
				mAUDFP1[2] = (int)value + 1;

				RecomputeTimerPeriod<2>();

				if (mbLinkedTimers34) {
					RecomputeTimerPeriod<3>();

					// Both counters must be updated before we run SetupTimers().
					UpdateTimerCounter<2>();
					UpdateTimerCounter<3>();
					SetupTimers(0x0C);
				} else {
					UpdateTimerCounter<2>();
					SetupTimers(0x04);
				}
			}
			mbSerialRateChanged = true;
			break;
		case 0x05:	// $D205 AUDC3
			if (mAUDC[2] != value) {
				mAUDC[2] = value;
				mpRenderer->SetAUDCx(2, value);
			}
			break;
		case 0x06:	// $D206 AUDF4
			if (mAUDF[3] != value) {
				mAUDF[3] = value;
				mAUDFP1[3] = (int)value + 1;

				RecomputeTimerPeriod<3>();

				if (mbLinkedTimers34) {
					// Both counters must be updated before we run SetupTimers().
					UpdateTimerCounter<2>();
					UpdateTimerCounter<3>();
					SetupTimers(0x0C);
				} else {
					UpdateTimerCounter<3>();
					SetupTimers(0x08);
				}
			}
			mbSerialRateChanged = true;
			break;
		case 0x07:	// $D207 AUDC4
			if (mAUDC[3] != value) {
				mAUDC[3] = value;
				mpRenderer->SetAUDCx(3, value);
			}
			break;
		case 0x08:	// $D208 AUDCTL
			if (mAUDCTL != value) {
				uint8 delta = mAUDCTL ^ value;
				if (delta & 0x29)
					mbSerialRateChanged = true;

				UpdateTimerCounter<0>();
				UpdateTimerCounter<1>();
				UpdateTimerCounter<2>();
				UpdateTimerCounter<3>();

				FlushDeferredTimerEvents(0);
				FlushDeferredTimerEvents(1);
				FlushDeferredTimerEvents(2);
				FlushDeferredTimerEvents(3);

				mAUDCTL = value;
				mbFastTimer1 = (mAUDCTL & 0x40) != 0;
				mbFastTimer3 = (mAUDCTL & 0x20) != 0;
				mbLinkedTimers12 = (mAUDCTL & 0x10) != 0;
				mbLinkedTimers34 = (mAUDCTL & 0x08) != 0;
				mbUse15KHzClock = (mAUDCTL & 0x01) != 0;

				mpRenderer->SetAUDCTL(value);

				if (delta & 0x18)
					RecomputeAllowedDeferredTimers();

				if (delta & 0x51) {
					RecomputeTimerPeriod<0>();
					RecomputeTimerPeriod<1>();
				}

				if (delta & 0x29) {
					RecomputeTimerPeriod<2>();
					RecomputeTimerPeriod<3>();
				}

				SetupTimers(0x0f);
			}
			break;
		case 0x09:	// $D209 STIMER
			mpScheduler->SetEvent(4, this, kATPokeyEventResetTimers, mpResetTimersEvent);
			break;
		case 0x0A:	// $D20A SKRES
			mSKSTAT |= 0xe0;
			break;
		case 0x0B:	// $D20B POTGO
			StartPotScan();
			break;
		case 0x0D:	// $D20D SEROUT
			if (mbTraceSIO)
				ATConsoleTaggedPrintf("POKEY: Sending serial byte %02x\n", value);

			// The only thing that writing to SEROUT does is load the register and set a latch
			// indicating that a byte is ready. The actual load into the output shift register
			// cannot occur until another serial clock pulse arrives.
			mSEROUT = value;
			if (mbSerOutValid && mbTraceSIO)
				ATConsoleTaggedPrintf("POKEY: Serial output overrun detected.\n");

			if (!mSerialOutputCounter) {
				mSerialOutputCounter = 1;

				// check if we are doing external output clock
				if (!(mSKCTL & 0x60) && mSerialExtPeriod) {
					// yup -- start external clock
					uint32 delay = (ATSCHEDULER_GETTIME(mpScheduler) + 2) - mSerialExtBaseTime;

					if (delay >= 0x80000000) {
						delay = (0 - delay) % mSerialExtPeriod;

						delay = mSerialExtPeriod - delay;
					} else {
						delay %= mSerialExtPeriod;

						if (!delay)
							delay = mSerialExtPeriod;
					}

					mpScheduler->SetEvent(delay, this, kATPokeyEventSerialOutput, mpEventSerialOutput);
				}
			}

			mbSerOutValid = true;
			break;
		case 0x0E:
			if (mIRQEN != value) {
				const uint8 delta = (mIRQEN ^ value);

				mIRQEN = value;

				mIRQST |= ~value & 0xF7;

				if (mbBreakKeyState && (mIRQEN & mIRQST & 0x80)) {
					mIRQST |= 0x80;
				}

				if (!(mIRQEN & ~mIRQST))
					NegateIrq(true);
				else
					AssertIrq(true);

				// Check if any of the IRQ bits are being turned on and we are currently running that timer
				// in deferred mode. If so, we need to yank it out of deferred mode. We don't do the
				// opposite here; we wait until the existing timer expires to reinit the timer into
				// deferred mode so as to not trigger on momentary clears.
				if (delta & 0x07) {
					RecomputeAllowedDeferredTimers();

					if (delta & mIRQEN & 0x07) {
						uint8 timersToChange = 0;

						// timer 1
						if ((delta & 0x01) && mbDeferredTimerEvents[0])
							timersToChange |= 0x01;

						// timer 2
						if ((delta & 0x02) && mbDeferredTimerEvents[1])
							timersToChange |= 0x02;

						// timer 4
						if ((delta & 0x04) && mbDeferredTimerEvents[3])
							timersToChange |= 0x08;

						if (timersToChange) {
							UpdateTimerCounter<0>();
							UpdateTimerCounter<1>();
							UpdateTimerCounter<2>();
							UpdateTimerCounter<3>();

							SetupTimers(timersToChange);
						}
					}
				}

				// check if the receive interrupt was toggled
				if (delta & 0x20) {
					if (value & 0x20) {
						if (mbSerInBurstPendingIRQ2) {
							mbSerInBurstPendingIRQ2 = false;
							
							if (!mbSerInBurstPendingData) {
								for(IATPokeySIODevice *dev : mDevices) {
									if (mbSerInBurstPendingData)
										break;

									dev->PokeySerInReady();
								}
							}
						}
					} else {
						if (mbSerInBurstPendingIRQ1) {
							mbSerInBurstPendingIRQ1 = false;
							mbSerInBurstPendingIRQ2 = true;
						}
					}
				}
			}
			break;
		case 0x0F:
			if (value != mSKCTL) {
				UpdateTimerCounter<0>();
				UpdateTimerCounter<1>();
				UpdateTimerCounter<2>();
				UpdateTimerCounter<3>();

				if (!(mSKCTL & 0x10) && (value & 0x10) && mbSerialWaitingForStartBit) {
					// Restart timers 3 and 4 immediately.
					mCounter[2] = mAUDFP1[2];
					mCounter[3] = mAUDFP1[3];
				}

				const uint8 delta = value ^ mSKCTL;

				// update pots if fast pot scan mode has changed
				if (delta & 0x04) {
					// A time skew of two cycles is necessary to handle this case properly:
					//
					// STA POTGO
					// STY SKCTL   ;enable fast pot scan mode
					//
					// The counter is delayed by two cycles, but so is the change to fast mode.
					UpdatePots(2);
				}

				// force serial rate re-evaluation if any clocking mode bits have changed
				if (delta & 0x70)
					mbSerialRateChanged = true;

				bool prvInit = (mSKCTL & 3) == 0;
				bool newInit = (value & 3) == 0;

				if (newInit != prvInit) {
					if (newInit) {
						mpScheduler->UnsetEvent(mpKeyboardScanEvent);
						mpScheduler->UnsetEvent(mpKeyboardIRQEvent);

						// Don't reset poly counters at this point -- we need to keep them going in
						// order to emulate the shift-out.
						mPolyShutOffTime = mpScheduler->GetTick64();
					} else {
						VDASSERT(!mpKeyboardScanEvent);

						// The 64KHz polynomial counter is a 5-bit LFSR that is reset to all 1s
						// on init and XORs bits 4 and 2, if seen as shifting left. In order to
						// achieve a 28 cycle period, a one is force fed when the register
						// equals 00010. A side effect of this mechanism is that resetting the
						// register actually starts it 19 cycles in. There is, however, an
						// additional two clock delay from the shift register comparator to
						// the timers.

						// The 15KHz polynomial counter is a 7-bit LFSR that is reset to all 0s
						// on init and XNORs bits 7 and 6, if seen as shifting left. In order to
						// achieve a 114 cycle period, a one is force fed when the register
						// equals 1001001.

						mLast15KHzTime = ATSCHEDULER_GETTIME(mpScheduler) + 81 - 114;
						mLast64KHzTime = ATSCHEDULER_GETTIME(mpScheduler) + 22 - 28;

						mPoly9Counter = 0;
						mPoly17Counter = 0;
						mLastPolyTime = ATSCHEDULER_GETTIME(mpScheduler) + 1;
					}

					mpRenderer->SetInitMode(newInit);

					mSerialInputShiftRegister = 0;
					mSerialOutputShiftRegister = 0;
					mSerialOutputCounter = 0;
					mSerialInputCounter = 0;
					mbSerOutValid = false;
					mbSerShiftValid = false;
					mbSerialOutputState = false;

					// reset burst state
					mbSerInBurstPendingData = false;
					mbSerInBurstPendingIRQ1 = false;
					mbSerInBurstPendingIRQ2 = false;
					mSerOutBurstDeadline = 0;

					// reset serial input active bit
					mSKSTAT |= 0x02;

					// assert serial output complete
					mIRQST &= ~0x08;

					if (!(mIRQEN & ~mIRQST))
						NegateIrq(true);
					else
						AssertIrq(true);

					if (mpCassette)
						mpCassette->PokeyResetSerialInput();

					++mSerialInputResetCounter;
				}

				mSKCTL = value;

				// check for change in keyboard scan
				if (delta & 0x02) {
					if (!(value & 0x02)) {
						// Keyboard scan is being disabled -- this resets the keyboard state machine
						// and so bit 2 deasserts. However, this doesn't affect the shift key state
						// (bit 3).
						mSKSTAT |= 0x04;

						mKeyScanState = 0;
						mKeyScanCode = 0;
					} else {
						// Keyboard scan is being enabled -- check if we are in cooked/non-immediate raw mode, and
						// if so, immediately update the shift key state
						if (!mbKeyboardScanEnabled && !mb5200Mode) {
							if (mbShiftKeyState)
								mSKSTAT &= ~0x08;
							else
								mSKSTAT |= 0x08;

							for(int i=0; i<8; ++i) {
								if (mKeyMatrix[i] & 0xff) {
									mSKSTAT &= ~0x04;
									mKBCODE = (uint8)(
										  i*8
										+ VDFindLowestSetBitFast(mKeyMatrix[i] & 0xFF)
										+ (mbShiftKeyState ? 0x40 : 0x00)
										+ (mbControlKeyState ? 0x80 : 0x00)
										);
									QueueKeyboardIRQ();
								}
							}
						}
					}

					UpdateKeyboardScanEvent();
				}

				RecomputeAllowedDeferredTimers();
				SetupTimers(0x0f);

				// check if serial timer is stopped and terminate output byte if needed
				if (!IsSerialOutputClockRunning())
					FlushSerialOutput();
			}
			break;

		default:
			if (reg & 0x10) {
				mpSlave->WriteByte(reg & 0x0f, value);
			}
			break;
	}
}

void ATPokeyEmulator::DumpStatus() {
	if (mpSlave) {
		ATConsolePrintf("Primary POKEY:\n");
		DumpStatus(false);
		ATConsolePrintf("\nSecondary POKEY:\n");
		mpSlave->DumpStatus(true);
	} else {
		DumpStatus(false);
	}
}

void ATPokeyEmulator::BeginLoadState(ATSaveStateReader& reader) {
	reader.RegisterHandlerMethod(kATSaveStateSection_Arch, VDMAKEFOURCC('P', 'O', 'K', 'Y'), this, &ATPokeyEmulator::LoadStateArch);
	reader.RegisterHandlerMethod(kATSaveStateSection_Private, VDMAKEFOURCC('P', 'O', 'K', 'Y'), this, &ATPokeyEmulator::LoadStatePrivate);
	reader.RegisterHandlerMethod(kATSaveStateSection_ResetPrivate, 0, this, &ATPokeyEmulator::LoadStateResetPrivate);
	reader.RegisterHandlerMethod(kATSaveStateSection_End, 0, this, &ATPokeyEmulator::EndLoadState);

	if (mpSlave)
		mpSlave->BeginLoadState(reader);
}

void ATPokeyEmulator::LoadStateArch(ATSaveStateReader& reader) {
	for(int i=0; i<4; ++i) {
		mState.mReg[i*2 + 0] = mAUDF[i] = reader.ReadUint8();
		mState.mReg[i*2 + 1] = mAUDC[i] = reader.ReadUint8();
	}

	mState.mReg[8] = mAUDCTL = reader.ReadUint8();
	mState.mReg[0x0E] = mIRQEN = reader.ReadUint8();
	mIRQST = reader.ReadUint8();
	mState.mReg[0x0F] = mSKCTL = reader.ReadUint8();
}

void ATPokeyEmulator::LoadStatePrivate(ATSaveStateReader& reader) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mALLPOT = reader.ReadUint8();
	mKBCODE = reader.ReadUint8();
	
	for(int i=0; i<4; ++i)
		reader != mCounter[i];

	for(int i=0; i<4; ++i)
		reader != mCounterBorrow[i];

	mLast15KHzTime = t - reader.ReadUint8();
	mLast64KHzTime = t - reader.ReadUint8();

	mPoly9Counter = reader.ReadUint16() % 511;
	mPoly17Counter = reader.ReadUint32() % 131071;
	mPolyShutOffTime = reader.ReadUint64();

	mpRenderer->LoadState(reader);
}

void ATPokeyEmulator::LoadStateResetPrivate(ATSaveStateReader& reader) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mALLPOT = 0xFF;
	mKBCODE = 0xFF;

	for(int i=0; i<4; ++i) {
		mCounter[i] = 1;
		mCounterBorrow[i] = 0;
	}

	mLast15KHzTime = t;
	mLast64KHzTime = t;

	mPoly9Counter = 0;
	mPoly17Counter = 0;
	mPolyShutOffTime = mpScheduler->GetTick64();

	mpRenderer->ResetState();
}

void ATPokeyEmulator::EndLoadState(ATSaveStateReader& reader) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mbFastTimer1 = (mAUDCTL & 0x40) != 0;
	mbFastTimer3 = (mAUDCTL & 0x20) != 0;
	mbLinkedTimers12 = (mAUDCTL & 0x10) != 0;
	mbLinkedTimers34 = (mAUDCTL & 0x08) != 0;
	mbUse15KHzClock = (mAUDCTL & 0x01) != 0;

	mpRenderer->SetInitMode((mSKCTL & 3) == 0);
	mpRenderer->SetAUDCTL(mAUDCTL);
	for(int i=0; i<4; ++i) {
		mAUDFP1[i] = mAUDF[i] + 1;
		mpRenderer->SetAUDCx(i, mAUDC[i]);
	}

	mLastPolyTime = t;
	mbIrqAsserted = mIRQEN & ~mIRQST;

	if (mbIrqAsserted)
		AssertIrq(false);
	else
		NegateIrq(false);


	uint32 keyboardTickOffset = 114 - (t - UpdateLast15KHzTime());

	if (mbKeyboardIRQPending)
		mpScheduler->SetEvent(keyboardTickOffset, this, kATPokeyEventKeyboardIRQ, mpKeyboardIRQEvent);
	else
		mpScheduler->UnsetEvent(mpKeyboardIRQEvent);

	UpdateKeyboardScanEvent();

	RecomputeTimerPeriod<0>();
	RecomputeTimerPeriod<1>();
	RecomputeTimerPeriod<2>();
	RecomputeTimerPeriod<3>();
	RecomputeAllowedDeferredTimers();

	SetupTimers(0x0f);

	if (mpSlave)
		mpSlave->EndLoadState(reader);
}

void ATPokeyEmulator::BeginSaveState(ATSaveStateWriter& writer) {
	UpdateTimerCounter<0>();
	UpdateTimerCounter<1>();
	UpdateTimerCounter<2>();
	UpdateTimerCounter<3>();

	writer.RegisterHandlerMethod(kATSaveStateSection_Arch, this, &ATPokeyEmulator::SaveStateArch);
	writer.RegisterHandlerMethod(kATSaveStateSection_Private, this, &ATPokeyEmulator::SaveStatePrivate);
}

void ATPokeyEmulator::SaveStateArch(ATSaveStateWriter& writer) {
	writer.BeginChunk(VDMAKEFOURCC('P', 'O', 'K', 'Y'));

	for(int i=0; i<4; ++i) {
		writer.WriteUint8(mAUDF[i]);
		writer.WriteUint8(mAUDC[i]);
	}

	writer.WriteUint8(mAUDCTL);
	writer.WriteUint8(mIRQEN);
	writer.WriteUint8(mIRQST);
	writer.WriteUint8(mSKCTL);

	writer.EndChunk();

	if (mpSlave)
		mpSlave->SaveStateArch(writer);
}

void ATPokeyEmulator::SaveStatePrivate(ATSaveStateWriter& writer) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	writer.BeginChunk(VDMAKEFOURCC('P', 'O', 'K', 'Y'));

	writer.WriteUint8(mALLPOT);
	writer.WriteUint8(mKBCODE);

	for(int i=0; i<4; ++i)
		writer != mCounter[i];

	for(int i=0; i<4; ++i)
		writer != mCounterBorrow[i];

	writer.WriteUint8(t - UpdateLast15KHzTime(t));
	writer.WriteUint8(t - UpdateLast64KHzTime(t));

	int polyDelta = t - mLastPolyTime;
	writer.WriteUint16((mPoly9Counter + polyDelta) % 511);
	writer.WriteUint32((mPoly17Counter + polyDelta) % 131071);
	writer.WriteUint64(mpScheduler->GetTick64() - mPolyShutOffTime);

	mpRenderer->SaveState(writer);

	writer.EndChunk();

	if (mpSlave)
		mpSlave->SaveStatePrivate(writer);
}

void ATPokeyEmulator::GetRegisterState(ATPokeyRegisterState& state) const {
	state = mState;
}

void ATPokeyEmulator::GetAudioState(ATPokeyAudioState& state) const {
	mpRenderer->GetAudioState(state);
}

void ATPokeyEmulator::DumpStatus(bool isSlave) {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	VDStringA s;
	for(int i=0; i<4; ++i) {
		s.sprintf("AUDF%u: %02x  AUDC%u: %02x  Output: %d", i+1, mAUDF[i], i+1, mAUDC[i], mpRenderer->GetChannelOutput(i));

		if (mbDeferredTimerEvents[i]) {
			uint32 delay;

			if (mDeferredTimerStarts[i] - t - 1 < 0x7FFFFFFF)		// wrap(start > t) => wrap(start - t) > 0 => 0 < (start - t) < 80000000
				delay = mDeferredTimerStarts[i] - t;
			else
				delay = mDeferredTimerPeriods[i] - (t - mDeferredTimerStarts[i]) % mDeferredTimerPeriods[i];

			s.append_sprintf("  (%u cycles until fire) (passive: %d cycles)", delay, mDeferredTimerPeriods[i]);
		} else if (mpTimerBorrowEvents[i])
			s.append_sprintf("  (%u cycles until fire) (active)", (int)mpScheduler->GetTicksToEvent(mpTimerBorrowEvents[i]));

		s += '\n';
		ATConsoleWrite(s.c_str());
	}

	ATConsolePrintf("AUDCTL: %02x%s%s%s%s%s%s%s%s\n"
		, mAUDCTL
		, mAUDCTL & 0x80 ? ", 9-bit poly" : ", 17-bit poly"
		, mAUDCTL & 0x40 ? ", 1.79 ch1" : ""
		, mAUDCTL & 0x20 ? ", 1.79 ch3" : ""
		, mAUDCTL & 0x10 ? ", ch1+ch2" : ""
		, mAUDCTL & 0x08 ? ", ch3+ch4" : ""
		, mAUDCTL & 0x04 ? ", highpass 1+3" : ""
		, mAUDCTL & 0x02 ? ", highpass 2+4" : ""
		, mAUDCTL & 0x01 ? ", 15KHz" : ", 64KHz");

	static const char *kRecvModes[4]={
		"recv ext",
		"recv ch3+4 async",
		"recv ch4",
		"recv ch3+4 async",
	};

	static const char *kSendModes[8]={
		"send ext",
		"send ext",
		"send ch4",
		"send ch4 async (x)",
		"send ch4",
		"send ch4 async (x)",
		"send ch2",
		"send ch2",
	};

	static const char *kInitModes[4]={
		"init mode",
		"keyboard scan disabled",
		"keyboard scan enabled w/o debounce",
		"keyboard scan enabled",
	};

	ATConsolePrintf("SKCTL: %02x | %s | %s | %s | %s%s%s\n"
		, mSKCTL
		, kRecvModes[(mSKCTL >> 4) & 3]
		, kSendModes[(mSKCTL >> 4) & 7]
		, kInitModes[mSKCTL & 3]
		, mSKCTL & 0x80 ? " | force break" : ""
		, mSKCTL & 0x08 ? " | two-tone mode" : ""
		, mSKCTL & 0x04 ? " | fast pot scan" : ""
		);

	if (mSerialInputCounter)
		ATConsolePrintf("SERIN: %02X (shifting in %02X)\n", mSERIN, mSerialInputShiftRegister);
	else
		ATConsolePrintf("SERIN: %02X\n", mSERIN);

	ATConsolePrintf("SEROUT: %02x (%s)\n", mSEROUT, mbSerOutValid ? "pending" : "done");
	ATConsolePrintf("        shift register %02x (%d: %s)\n", mSerialOutputShiftRegister, mSerialOutputCounter, mSerialOutputCounter ? "pending" : "done");
	ATConsolePrintf("IRQEN:  %02x%s%s%s%s%s%s%s%s\n"
		, mIRQEN
		, mIRQEN & 0x80 ? ", break key" : ""
		, mIRQEN & 0x40 ? ", keyboard" : ""
		, mIRQEN & 0x20 ? ", serin" : ""
		, mIRQEN & 0x10 ? ", serout" : ""
		, mIRQEN & 0x08 ? ", sertrans" : ""
		, mIRQEN & 0x04 ? ", timer4" : ""
		, mIRQEN & 0x02 ? ", timer2" : ""
		, mIRQEN & 0x01 ? ", timer1" : ""
		);
	ATConsolePrintf("IRQST:  %02x%s%s%s%s%s%s%s%s\n"
		, mIRQST
		, mIRQST & 0x80 ? "" : ", break key"
		, mIRQST & 0x40 ? "" : ", keyboard"
		, mIRQST & 0x20 ? "" : ", serin"
		, mIRQST & 0x10 ? "" : ", serout"
		, mIRQST & 0x08 ? "" : ", sertrans"
		, mIRQST & 0x04 ? "" : ", timer4"
		, mIRQST & 0x02 ? "" : ", timer2"
		, mIRQST & 0x01 ? "" : ", timer1"
		);
	ATConsolePrintf("ALLPOT: %02x\n", mALLPOT);	

	ATConsolePrintf("\nCommand line: %s\n", mbCommandLineState ? "asserted" : "negated");
}

void ATPokeyEmulator::FlushAudio(bool pushAudio, uint64 timestamp) {
	const uint32 outputSampleCount = mpRenderer->EndBlock();

	if (mpSlave) {
		const uint32 slaveOutputSampleCount = mpSlave->mpRenderer->EndBlock();

		VDASSERT(outputSampleCount == slaveOutputSampleCount);
	}

	if (mpAudioOut) {
		uint64 startingTimestamp = timestamp - 28 * outputSampleCount;

		mpAudioOut->WriteAudio(
			mpRenderer->GetOutputBuffer(),
			mpSlave && mbStereoSoftEnable ? mpSlave->mpRenderer->GetOutputBuffer() : NULL,
			outputSampleCount,
			pushAudio,
			startingTimestamp);
	}
}

void ATPokeyEmulator::SetTraceContext(ATTraceContext *context) {
	mpTraceContext = context;

	if (context) {
		ATTraceCollection *coll = context->mpCollection;

		mpTraceChannelIrq = coll->AddGroup(L"POKEY")->AddSimpleChannel(context->mBaseTime, context->mBaseTickScale, L"IRQ");
		mbTraceIrqPending = false;
	} else {
		mpTraceChannelIrq = nullptr;
	}
}

void ATPokeyEmulator::UpdateMixTable() {
	if (mbNonlinearMixingEnabled) {
		// This table is an average of all volumes measured on a real 800XL
		// at a frequency of 314Hz. It's not entirely accurate because the
		// output of each channel has uneven volume steps and the effect
		// varies by frequency, but it's reasonable enough.

		static const float kMixTable[61]={
			0.0f,					2.2531377552f,			4.2472153279f,			6.500391935f,
			8.9508990534f,			11.4574264806f,			13.9021746199f,			16.4639482412f,
			19.0064170324f,			21.4960784166f,			23.9395872707f,			26.3295307579f,
			28.6392299848f,			30.9133574054f,			33.1163210469f,			35.2267639589f,
			37.2167441749f,			39.0864696084f,			40.8392802498f,			42.4828246319f,
			44.0230000906f,			45.4666953783f,			46.8220599132f,			48.097146906f,
			49.2958623121f,			50.4211078033f,			51.4695404841f,			52.438465135f,
			53.325178072f,			54.1314833312f,			54.8585846028f,			55.5051873239f,
			56.0743392951f,			56.5677964414f,			56.9906057796f,			57.3499814438f,
			57.6534805767f,			57.9095660775f,			58.1255653166f,			58.3064356456f,
			58.4579355164f,			58.5826773523f,			58.6861268801f,			58.7720420762f,
			58.8434313481f,			58.9031757296f,			58.9563884057f,			59.0092500513f,
			59.0631705296f,			59.1201462142f,			59.1800746579f,			59.2422604549f,
			59.3062129126f,			59.3766274524f,			59.4493321676f,			59.5218792178f,
			59.5968984698f,			59.6788381494f,			59.77366993f,			59.8727379928f,
			60.0f
		};

		VDASSERTCT(sizeof(mpTables->mMixTable) == sizeof(kMixTable));

		memcpy(mpTables->mMixTable, kMixTable, sizeof mpTables->mMixTable);

		const float alpha = 0.01f;
		float timeConstant = (1.0f - alpha) / (alpha * 63920.0f);
		float rc = timeConstant * 1789772.5f;
		float neg_inv_rc = -1.0f / rc;

		// integral(e^-t/RC) = -RC*e^-t/RC + C
		mpTables->mHPTable[0] = 0.0f;
		mpTables->mHPIntegralTable[0] = 0.0f;

		for(int i=1; i<=56; ++i) {
			mpTables->mHPTable[i] = 1.0f - expf((float)i * neg_inv_rc * 0.5f);
			mpTables->mHPIntegralTable[i] = rc - rc*expf((float)i * neg_inv_rc * 0.5f);
		}
	} else {
		for(int i=0; i<61; ++i)
			mpTables->mMixTable[i] = (float)i;

		for(int i=0; i<=56; ++i) {
			mpTables->mHPTable[i] = 0.0f;
			mpTables->mHPIntegralTable[i] = (float)i * 0.5f;
		}
	}
}

void ATPokeyEmulator::UpdateKeyMatrix(int index, uint16 mask, uint16 state) {
	uint16 delta = (mKeyMatrix[index] ^ state) & mask;

	if (delta) {
		mKeyMatrix[index] ^= delta;

		UpdateEffectiveKeyMatrix();
	}
}

// Three or more keys pressed at the same time can produce phantom keys
// by connecting column lines to the active row line by other row lines:
//
// 1   .    A<---B
//     .    |    ^
//     .    |    |
// 2 --.----.--->C   active row
//     .    |    |
//     .    |    |
// 3   .    .    .
//     .    |    |
//     .    v    v
//     1    2    3
//        sensed columns
//
// Here the connections formed by depressed keys A, B, and C cause a
// false detection of a fourth key at row 2, column 2. Trickier, however,
// is that such phantom keys can effectively serve as one of the keys
// necessary to produce other phantom keys:
//
// 1   .    A<---B
//     .    |    ^
//     .    |    |
// 2 --E----.--->C
//     |    |    |
//     |    |    |
// 3 --D----.----.   active row
//     .    |    |
//     .    v    v
//     1    2    3
//        sensed columns
//
// In this case, phantom keys at row 2, column 2 and row 3, column 3
// can be directly determined, but an additional phantom key at row
// 3, column 2 is also indirectly produced. This means that we need
// to produce the transitive closure of all interactions to get the
// full set of phantom keys.
//
// Why do we bother with this? Normally, pressing multiple keys does
// nothing because of debounce. However, there are two exceptions.
// First, Ctrl/Shift/Break participate in the matrix but are not
// affected by debounce. This causes Ctrl+Shift+X keys corresponding
// to scan codes $C0-C7 and $D0-D7 to not work. Second, the phantom
// keys ARE visible if debounce is disabled.
//
// To compute the transitive closure, we use an O(N^2) algorithm to
// compute the connected rows, and then an O(N) pass to propagate
// the unified column sets to all rows. Since only a few keys are
// pressed most of the time, we can save a lot of time by doing an
// early out for rows that have no active switches.
// 
void ATPokeyEmulator::UpdateEffectiveKeyMatrix() {
	int srcRows[8];
	bool activeKeys = false;

	memcpy(mEffectiveKeyMatrix, mKeyMatrix, sizeof mEffectiveKeyMatrix);

	// The computer line connects the control (KR2) signal via the same
	// row select lines as the main keyboard on KR1, so both the main
	// keyboard and the Ctrl/Shift/Break keys can interact to create
	// phantom keys on both sides. The 5200 just connects the top button
	// to KR2, so nothing in the main matrix can interfere.
	const uint16 crossConnectMask = mb5200Mode ? 0xFF : 0xFFFF;

	for(int i=0; i<8; ++i) {
		int srcRow = i;
		uint16 connectedColumns = mEffectiveKeyMatrix[i] & crossConnectMask;

		if (connectedColumns) {
			for(int j = i + 1; j < 8; ++j) {
				if (mEffectiveKeyMatrix[j] & connectedColumns) {
					connectedColumns |= mEffectiveKeyMatrix[j];
					srcRow = j;
				}
			}

			mEffectiveKeyMatrix[srcRow] |= connectedColumns & crossConnectMask;

			activeKeys = true;
		}

		srcRows[i] = srcRow;
	}

	if (!activeKeys)
		return;

	for(int i=0; i<8; ++i)
		mEffectiveKeyMatrix[i] |= mEffectiveKeyMatrix[srcRows[i]];
}

bool ATPokeyEmulator::CanPushKey(uint8 scanCode) const {
	// wait if keyboard IRQ is still pending (not necessarily active in IRQST yet!)
	if (mbKeyboardIRQPending)
		return false;

	// wait if keyboard IRQ is still active or disabled
	if (!(mIRQST & mIRQEN & 0x40))
		return false;

	// wait if keyboard scan is disabled
	if ((mSKCTL & 3) != 3)
		return false;

	// wait if cooldown timer is still active to dodge the speaker / keyclick, unless we have
	// credible evidence that the OS can accept another key sooner
	const bool cooldownExpired = (mbUseKeyCooldownTimer && !mKeyCooldownTimer);
	if (!mpConn->PokeyIsKeyPushOK(scanCode, cooldownExpired))
		return false;

	// looks fine to push a new key...
	return true;
}

void ATPokeyEmulator::TryPushNextKey() {
	uint8 c = mKeyQueue.front();
	mKeyQueue.pop_front();

	PushKey(c, false, false, false, mbUseKeyCooldownTimer);
}

void ATPokeyEmulator::SetKeyboardModes(bool cooked, bool scanEnabled) {
	mbCookedKeyMode = cooked;
	mbKeyboardScanEnabled = scanEnabled;

	UpdateKeyboardScanEvent();
}

void ATPokeyEmulator::UpdateKeyboardScanEvent() {
	if ((mb5200Mode || mbKeyboardScanEnabled) && (mSKCTL & 2)) {
		const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

		mpScheduler->SetEvent(114 - (t - UpdateLast15KHzTime()), this, kATPokeyEventKeyboardScan, mpKeyboardScanEvent);
	} else
		mpScheduler->UnsetEvent(mpKeyboardScanEvent);
}

void ATPokeyEmulator::QueueKeyboardIRQ() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	if (!mpKeyboardIRQEvent)
		mpKeyboardIRQEvent = mpScheduler->AddEvent(114 - (t - UpdateLast15KHzTime()), this, kATPokeyEventKeyboardIRQ);
	mbKeyboardIRQPending = true;
}

void ATPokeyEmulator::AssertKeyboardIRQ() {
	if (mIRQEN & 0x40) {
		// If keyboard IRQ is already active, set the keyboard overrun bit.
		if (!(mIRQST & 0x40))
			mSKSTAT &= ~0x40;

		mIRQST &= ~0x40;
		AssertIrq(false);
	}
}

void ATPokeyEmulator::AssertBreakIRQ() {
	if (mIRQEN & 0x80) {
		mIRQST &= ~0x80;
		AssertIrq(false);
	}
}

void ATPokeyEmulator::AssertIrq(bool cpuBased) {
	if (!mbIrqAsserted) {
		mbIrqAsserted = true;

		mpConn->PokeyAssertIRQ(cpuBased);

		if (mpTraceContext) {
			const uint64 t = mpScheduler->GetTick64() + (cpuBased ? 0 : -1);
			mTraceIrqStart = t;
			mbTraceIrqPending = true;
		}
	}
}

void ATPokeyEmulator::NegateIrq(bool cpuBased) {
	if (mbIrqAsserted) {
		mbIrqAsserted = false;

		mpConn->PokeyNegateIRQ(cpuBased);

		if (mpTraceContext && mbTraceIrqPending) {
			mbTraceIrqPending = false;

			const uint64 t = mpScheduler->GetTick64() + (cpuBased ? 0 : -1);

			mpTraceChannelIrq->AddTickEvent(mTraceIrqStart, t, L"IRQ", kATTraceColor_Default);
		}
	}
}

void ATPokeyEmulator::ProcessReceivedSerialByte() {
	if (mbTraceSIO)
		ATConsoleTaggedPrintf("POKEY: Reasserting serial input IRQ. IRQEN=%02x, IRQST=%02x\n", mIRQEN, mIRQST);

	if (mIRQEN & 0x20) {
		// check for overrun
		if (!(mIRQST & 0x20)) {
			if (mSKSTAT & 0x20)
				g_ATLCSIOData <<= "Signaling first serial input overrun.\n";

			mSKSTAT &= 0xdf;

			if (mbTraceSIO)
				ATConsoleTaggedPrintf("POKEY: Serial input overrun detected (c=%02x; %02x %02x)\n", mSerialInputShiftRegister, mSERIN, mSerialInputShiftRegister);
		}

		mIRQST &= ~0x20;
		AssertIrq(false);
	}

	mSERIN = mSerialInputShiftRegister;
	mSKSTAT &= mSerialInputPendingStatus;
}

void ATPokeyEmulator::SyncRenderers(ATPokeyRenderer *r) {
	mpRenderer->SyncTo(*r);
}

void ATPokeyEmulator::StartPotScan() {
	// If we're in fast pot mode, update the pots now in case we're interrupting
	// a scan.
	if (mSKCTL & 4)
		UpdatePots(0);

	mbPotScanActive = true;
	mPotMasterCounter = 0;

	const uint32 fastTime = ATSCHEDULER_GETTIME(mpScheduler);
	const uint32 slowTime = UpdateLast15KHzTime();

	mPotLastTimeFast = fastTime + 2;
	mPotLastTimeSlow = slowTime;

	// If we're in slow pot mode, turn on the dumping caps and begin
	// charging from level 0.
	//
	// If we're in fast pot mode, any pot lines that have already reached
	// threshold will stay there unless they are being drained. For these
	// lines, ALLPOT will indicate that the pot is done, but the POTn
	// register will not update. The behavior is different if fast pot
	// mode is activated *after* the pot scan has started, but we aren't
	// handling that here.
	// 
	if (!(mSKCTL & 4)) {
		mALLPOT = 0xFF;
	} else {
		for(int i=0; i<8; ++i) {
			if (mPotHiPositions[i] == 0xFF)
				mALLPOT |= (1 << i);
		}
	}
}

void ATPokeyEmulator::UpdatePots(uint32 timeSkew) {
	const uint32 fastTime = ATSCHEDULER_GETTIME(mpScheduler) + timeSkew;
	if ((fastTime - mPotLastTimeFast - 1) >= (uint32)0x7FFFFFFF)		// wrap(fastTime <= mPotLastTimeFast)
		return;

	const uint32 slowTime = UpdateLast15KHzTime();

	uint32 count = mPotMasterCounter;
	if (mSKCTL & 4) {		// fast pot scan
		count += (fastTime - mPotLastTimeFast);

		if (count >= 229) {
			count = 229;

			mbPotScanActive = false;
		}

	} else {				// slow pot scan
		count += (slowTime - mPotLastTimeSlow) / 114; 

		if (count >= 228) {
			count = 228;
			mbPotScanActive = false;
		}
	}

	mPotMasterCounter = (uint8)count;

	mPotLastTimeFast = fastTime;
	mPotLastTimeSlow = slowTime;

	if (mALLPOT) {
		const uint8 (&positions)[8] = *((mSKCTL & 4) ? &mPotHiPositions : &mPotPositions);
		for(int i=0; i<8; ++i) {
			if (!(mALLPOT & (1 << i)))
				continue;

			if (mPotMasterCounter >= positions[i]) {
				mALLPOT &= ~(1 << i);

				mPotLatches[i] = std::min<uint8>(229, positions[i]);
			}
		}
	}
}

void ATPokeyEmulator::UpdateAddressDecoding() {
	mAddressMask = mpSlave && mbStereoSoftEnable ? 0x1F : 0x0F;
}
