//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2009 Avery Lee
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
#include <at/atcore/scheduler.h>
#include "inputcontroller.h"
#include "inputmanager.h"
#include "gtia.h"
#include "pokey.h"
#include "antic.h"
#include "pia.h"

class ATAnticEmulator;

///////////////////////////////////////////////////////////////////////////

ATLightPenPort::ATLightPenPort()
	: mpAntic(NULL)
	, mTriggerStateMask(0x03)
	, mTriggerState(0)
	, mAdjustX(0)
	, mAdjustY(0)
	, mbOddPhase(false)
{
}

void ATLightPenPort::Init(ATAnticEmulator *antic) {
	mpAntic = antic;
}

void ATLightPenPort::SetIgnorePort34(bool ignore) {
	mTriggerStateMask = ignore ? 0x01 : 0x03;
}

void ATLightPenPort::SetColorClockPhase(bool odd) {
	mbOddPhase = odd;
}

void ATLightPenPort::SetPortTriggerState(int index, bool state) {
	uint8 newState = mTriggerState;

	if (state)
		newState |= 1 << index;
	else
		newState &= ~(1 << index);

	if (newState == mTriggerState)
		return;

	mTriggerState = newState;

	if ((newState & mTriggerStateMask) && mpAntic)
		mpAntic->SetLightPenPosition(mbOddPhase);
}

///////////////////////////////////////////////////////////////////////////

ATPortController::ATPortController()
	: mPIAInputIndex(-1)
	, mPIAOutputIndex(-1)
	, mPortValue(0xFF)
	, mbTrigger1(false)
	, mbTrigger2(false)
	, mMultiMask(0x0F000000)
	, mpGTIA(NULL)
	, mpPokey(NULL)
	, mpPIA(NULL)
	, mTriggerIndex(0)
	, mpLightPen(NULL)
{
}

ATPortController::~ATPortController() {
	Shutdown();
}

void ATPortController::Init(ATGTIAEmulator *gtia, ATPokeyEmulator *pokey, ATPIAEmulator *pia, ATLightPenPort *lightPen, int index) {
	VDASSERT(index == 0 || index == 2);

	mpGTIA = gtia;
	mpPokey = pokey;
	mpPIA = pia;
	mTriggerIndex = index;
	mpLightPen = lightPen;

	mPIAInputIndex = mpPIA->AllocInput();
}

void ATPortController::Shutdown() {
	if (mpPIA) {
		mpPIA->FreeInput(mPIAInputIndex);

		if (mPIAOutputIndex >= 0) {
			mpPIA->FreeOutput(mPIAOutputIndex);
			mPIAOutputIndex = -1;
		}

		mpPIA = NULL;
	}
}

void ATPortController::SetMultiMask(uint8 mask) {
	mMultiMask = (uint32)mask << 22;

	UpdatePortValue();
}

int ATPortController::AllocatePortInput(bool port2, int multiIndex) {
	PortInputs::iterator it(std::find(mPortInputs.begin(), mPortInputs.end(), 0));
	int index = (int)(it - mPortInputs.begin());

	uint32 v = port2 ? 0xC0000000 : 0x80000000;

	if (multiIndex >= 0)
		v |= 0x00400000 << multiIndex;
	else
		v |= 0x3FC00000;

	if (it != mPortInputs.end())
		*it = v;
	else
		mPortInputs.push_back(v);

	return index;
}

void ATPortController::FreePortInput(int index) {
	if ((uint32)index >= mPortInputs.size()) {
		VDASSERT(false);
		return;
	}

	uint8 oldVal = (uint8)mPortInputs[index];
	if (oldVal) {
		mPortInputs[index] = 0;
		UpdatePortValue();
	}

	while(!mPortInputs.empty() && mPortInputs.back() == 0)
		mPortInputs.pop_back();
}

void ATPortController::SetPortInput(int index, uint32 portBits) {
	uint32 oldVal = mPortInputs[index];
	if (oldVal != portBits) {
		mPortInputs[index] = (oldVal & 0xffc00000) + portBits;
		UpdatePortValue();
	}
}

void ATPortController::ResetPotPositions() {
	mpPokey->SetPotPos(mTriggerIndex * 2 + 0, 229);
	mpPokey->SetPotPos(mTriggerIndex * 2 + 1, 229);
}

void ATPortController::SetPotPosition(int offset, uint8 pos) {
	mpPokey->SetPotPos(mTriggerIndex * 2 + offset, pos);
}

void ATPortController::SetPotHiPosition(int offset, int hipos, bool grounded) {
	mpPokey->SetPotPosHires(mTriggerIndex * 2 + offset, hipos, grounded);
}

int ATPortController::AllocatePortOutput(ATPortInputController *target, uint8 mask) {
	auto it = std::find_if(mPortOutputs.begin(), mPortOutputs.end(),
		[](const PortOutput& portOutput) { return !portOutput.mpTarget; });

	int index = (int)(it - mPortOutputs.begin());

	const PortOutput newPortOutput = { target, mask };
	if (it != mPortOutputs.end())
		*it = newPortOutput;
	else
		mPortOutputs.push_back(newPortOutput);

	UpdatePortOutputRegistration();
	return index;
}

void ATPortController::SetPortOutputMask(int index, uint8 mask) {
	if (index < 0)
		return;

	if ((unsigned)index >= mPortOutputs.size()) {
		VDASSERT(false);
		return;
	}

	auto& portOutput = mPortOutputs[index];
	if (!portOutput.mpTarget) {
		VDASSERT(false);
		return;
	}

	if (portOutput.mMask != mask) {
		portOutput.mMask = mask;

		UpdatePortOutputRegistration();
	}
}

void ATPortController::FreePortOutput(int index) {
	if ((unsigned)index >= mPortOutputs.size()) {
		VDASSERT(false);
		return;
	}

	auto& portOutput = mPortOutputs[index];
	VDASSERT(portOutput.mpTarget);

	portOutput.mpTarget = nullptr;
	portOutput.mMask = 0;

	while(!mPortOutputs.empty() && !mPortOutputs.back().mpTarget)
		mPortOutputs.pop_back();

	UpdatePortOutputRegistration();
}

uint8 ATPortController::GetPortOutputState() const {
	const uint32 state = mpPIA->GetOutputState();

	return mTriggerIndex ? (uint8)(state >> 8) : (uint8)state;
}

void ATPortController::UpdatePortValue() {
	PortInputs::const_iterator it(mPortInputs.begin()), itEnd(mPortInputs.end());
	
	uint32 portval = 0;
	while(it != itEnd) {
		uint32 pv = *it++;

		if (!(pv & mMultiMask))
			continue;

		if (pv & 0x40000000)
			pv <<= 4;

		portval |= pv;
	}

	if (portval & 0x1100) {
		mpLightPen->SetPortTriggerState(mTriggerIndex, true);
	} else {
		mpLightPen->SetPortTriggerState(mTriggerIndex, false);
	}

	mPortValue = ~(uint8)portval;

	if (mTriggerIndex)
		mpPIA->SetInput(mPIAInputIndex, ((uint32)mPortValue << 8) | 0xFF);
	else
		mpPIA->SetInput(mPIAInputIndex, (uint32)mPortValue | 0xFF00);

	bool trigger1 = (portval & 0x100) != 0;
	bool trigger2 = (portval & 0x1000) != 0;

	if (mbTrigger1 != trigger1) {
		mbTrigger1 = trigger1;

		mpGTIA->SetControllerTrigger(mTriggerIndex + 0, trigger1);
	}

	if (mbTrigger2 != trigger2) {
		mbTrigger2 = trigger2;

		mpGTIA->SetControllerTrigger(mTriggerIndex + 1, trigger2);
	}
}

void ATPortController::UpdatePortOutputRegistration() {
	uint8 totalMask = 0;

	for(const auto& output : mPortOutputs)
		totalMask |= output.mMask;

	if (totalMask) {
		uint32 piaChangeMask = totalMask;

		if (mTriggerIndex)
			piaChangeMask <<= 8;

		if (mPIAOutputIndex < 0)
			mPIAOutputIndex = mpPIA->AllocOutput(OnPortOutputUpdated, this, piaChangeMask);
		else
			mpPIA->ModifyOutputMask(mPIAOutputIndex, piaChangeMask);
	} else {
		if (mPIAOutputIndex >= 0) {
			mpPIA->FreeOutput(mPIAOutputIndex);
			mPIAOutputIndex = -1;
		}
	}
}

void ATPortController::OnPortOutputUpdated(void *data, uint32 outputState) {
	auto *const thisPtr = (ATPortController *)data;

	const uint8 portState = thisPtr->mTriggerIndex ? (uint8)(outputState >> 8) : (uint8)outputState;

	for(const auto& output : thisPtr->mPortOutputs) {
		if (output.mpTarget)
			output.mpTarget->UpdateOutput(portState);
	}
}

///////////////////////////////////////////////////////////////////////////

ATPortInputController::ATPortInputController()
	: mpPortController(NULL)
{
}

ATPortInputController::~ATPortInputController() {
	Detach();
}

void ATPortInputController::Attach(ATPortController *pc, bool port2, int multiIndex) {
	mpPortController = pc;
	mPortInputIndex = pc->AllocatePortInput(port2, multiIndex);
	mPortOutputIndex = -1;
	mPortOutputMask = 0;
	mbPort2 = port2;

	OnAttach();
}

void ATPortInputController::Detach() {
	if (mpPortController) {
		OnDetach();

		mpPortController->FreePortInput(mPortInputIndex);

		if (mPortOutputIndex >= 0) {
			mpPortController->FreePortOutput(mPortOutputIndex);
			mPortOutputIndex = -1;
		}

		mpPortController = NULL;
	}
}

void ATPortInputController::SetPortOutput(uint32 portBits) {
	if (mpPortController)
		mpPortController->SetPortInput(mPortInputIndex, portBits);
}

void ATPortInputController::SetPotPosition(bool second, uint8 pos) {
	if (mpPortController)
		mpPortController->SetPotPosition((mbPort2 ? 2 : 0) + (second ? 1 : 0), pos);
}

void ATPortInputController::SetPotHiPosition(bool second, int pos, bool grounded) {
	if (mpPortController)
		mpPortController->SetPotHiPosition((mbPort2 ? 2 : 0) + (second ? 1 : 0), pos, grounded);
}

void ATPortInputController::SetOutputMonitorMask(uint8 mask) {
	mask &= 0x0F;

	if (mPortOutputMask != mask) {
		mPortOutputMask = mask;

		if (mbPort2)
			mask <<= 4;

		if (mask) {
			if (mPortOutputIndex < 0)
				mPortOutputIndex = mpPortController->AllocatePortOutput(this, mask);
			else
				mpPortController->SetPortOutputMask(mPortOutputIndex, mask);

			mPortOutputState = mpPortController->GetPortOutputState();
		} else {
			if (mPortOutputIndex >= 0) {
				mpPortController->FreePortOutput(mPortOutputIndex);
				mPortOutputIndex = -1;
			}
		}
	}
}

void ATPortInputController::UpdateOutput(uint8 state) {
	if (mbPort2)
		state >>= 4;

	state &= 0x0F;

	if (mPortOutputState != state) {
		mPortOutputState = state;

		OnPortOutputChanged(state);
	}
}

///////////////////////////////////////////////////////////////////////////

ATMouseController::ATMouseController(bool amigaMode)
	: mPortBits(0)
	, mTargetX(0)
	, mTargetY(0)
	, mAccumX(0)
	, mAccumY(0)
	, mpUpdateXEvent(NULL)
	, mpUpdateYEvent(NULL)
	, mpScheduler(NULL)
	, mbAmigaMode(amigaMode)
{
	mbButtonState[0] = false;
	mbButtonState[1] = false;
}

ATMouseController::~ATMouseController() {
}

void ATMouseController::Init(ATScheduler *scheduler) {
	mpScheduler = scheduler;
}

void ATMouseController::SetPosition(int x, int y) {
	mTargetX = x;
	mTargetY = y;
}

void ATMouseController::AddDelta(int dx, int dy) {
	mTargetX += dx;
	mTargetY += dy;
}

void ATMouseController::SetButtonState(int button, bool state) {
	mbButtonState[button] = state;
}

void ATMouseController::SetDigitalTrigger(uint32 trigger, bool state) {
	switch(trigger) {
		case kATInputTrigger_Button0:
			if (mbButtonState[0] != state) {
				mbButtonState[0] = state;

				uint32 newBits = (mPortBits & ~0x100) + (state ? 0x100 : 0);

				if (mPortBits != newBits) {
					mPortBits = newBits;
					SetPortOutput(mPortBits);
				}
			}
			break;
		case kATInputTrigger_Button0 + 1:
			if (mbButtonState[1] != state) {
				mbButtonState[1] = state;

				SetPotPosition(false, state ? 0 : 229);
			}
			break;
	}
}

void ATMouseController::ApplyImpulse(uint32 trigger, int ds) {
	ds <<= 4;

	switch(trigger) {
		case kATInputTrigger_Axis0:
			mTargetX += ds;
			EnableUpdate();
			break;
		case kATInputTrigger_Axis0+1:
			mTargetY += ds;
			EnableUpdate();
			break;
	}
}

void ATMouseController::OnScheduledEvent(uint32 id) {
	if (id == 1) {
		mpUpdateXEvent = nullptr;
		Update(true, false);
		EnableUpdate();
	} else if (id == 2) {
		mpUpdateYEvent = nullptr;
		Update(false, true);
		EnableUpdate();
	}
}

void ATMouseController::EnableUpdate() {
	if (!mpUpdateXEvent) {
		const uint32 dx = (mAccumX << 16) - mTargetX;

		if (dx & UINT32_C(0xffff0000)) {
			uint32 adx = dx & UINT32_C(0x80000000) ? 0-dx : dx;

			mpUpdateXEvent = mpScheduler->AddEvent(std::max<sint32>(1, std::min<uint32>(256, 0x1000000 / adx)), this, 1);
		}
	}

	if (!mpUpdateYEvent) {
		const uint32 dy = (mAccumY << 16) - mTargetY;

		if (dy & UINT32_C(0xffff0000)) {
			uint32 ady = dy & UINT32_C(0x80000000) ? 0-dy : dy;

			mpUpdateYEvent = mpScheduler->AddEvent(std::max<sint32>(1, std::min<uint32>(256, 0x1000000 / ady)), this, 2);
		}
	}
}

void ATMouseController::Update(bool doX, bool doY) {
	bool changed = false;

	if (doX) {
		const uint32 posX = mTargetX >> 16;
		if (mAccumX != posX) {
			if ((sint16)(mAccumX - posX) < 0)
				++mAccumX;
			else
				--mAccumX;

			mAccumX &= 0xffff;

			changed = true;
		}
	}

	if (doY) {
		const int posY = mTargetY >> 16;
		if (mAccumY != posY) {
			if ((sint16)(mAccumY - posY) < 0)
				++mAccumY;
			else
				--mAccumY;

			mAccumY &= 0xffff;

			changed = true;
		}
	}

	if (changed)
		UpdatePort();
}

void ATMouseController::UpdatePort() {
	static const uint8 kSTTabX[4] = { 0x00, 0x02, 0x03, 0x01 };
	static const uint8 kSTTabY[4] = { 0x00, 0x08, 0x0c, 0x04 };
	static const uint8 kAMTabX[4] = { 0x00, 0x02, 0x0A, 0x08 };
	static const uint8 kAMTabY[4] = { 0x00, 0x01, 0x05, 0x04 };

	uint32 val;
		
	if (mbAmigaMode)
		val = kAMTabX[mAccumX & 3] + kAMTabY[mAccumY & 3];
	else
		val = kSTTabX[mAccumX & 3] + kSTTabY[mAccumY & 3];

	uint32 newPortBits = (mPortBits & ~15) + val;

	if (mPortBits != newPortBits) {
		mPortBits = newPortBits;
		SetPortOutput(mPortBits);
	}
}

void ATMouseController::OnAttach() {
}

void ATMouseController::OnDetach() {
	mpPortController->ResetPotPositions();

	if (mpScheduler) {
		mpScheduler->UnsetEvent(mpUpdateXEvent);
		mpScheduler->UnsetEvent(mpUpdateYEvent);
	}
}

///////////////////////////////////////////////////////////////////////////

ATTrackballController::ATTrackballController()
	: mPortBits(0)
	, mTargetX(0)
	, mTargetY(0)
	, mAccumX(0)
	, mAccumY(0)
	, mpUpdateEvent(NULL)
	, mpScheduler(NULL)
{
	mbButtonState = false;
}

ATTrackballController::~ATTrackballController() {
}

void ATTrackballController::Init(ATScheduler *scheduler) {
	mpScheduler = scheduler;
}

void ATTrackballController::SetPosition(int x, int y) {
	mTargetX = x;
	mTargetY = y;
}

void ATTrackballController::AddDelta(int dx, int dy) {
	mTargetX += dx;
	mTargetY += dy;
}

void ATTrackballController::SetButtonState(int button, bool state) {
	mbButtonState = state;
}

void ATTrackballController::SetDigitalTrigger(uint32 trigger, bool state) {
	switch(trigger) {
		case kATInputTrigger_Button0:
			if (mbButtonState != state) {
				mbButtonState = state;

				uint32 newBits = (mPortBits & ~0x100) + (state ? 0x100 : 0);

				if (mPortBits != newBits) {
					mPortBits = newBits;
					SetPortOutput(mPortBits);
				}
			}
			break;
	}
}

void ATTrackballController::ApplyImpulse(uint32 trigger, int ds) {
	ds <<= 5;

	switch(trigger) {
		case kATInputTrigger_Axis0:
			mTargetX += ds;
			break;
		case kATInputTrigger_Axis0+1:
			mTargetY += ds;
			break;
	}
}

void ATTrackballController::OnScheduledEvent(uint32 id) {
	mpUpdateEvent = mpScheduler->AddEvent(7, this, 1);
	Update();
}

void ATTrackballController::Update() {
	bool changed = false;
	uint8 dirBits = mPortBits & 5;

	const uint32 posX = mTargetX >> 16;
	if (mAccumX != posX) {
		if ((sint16)(mAccumX - posX) < 0) {
			dirBits &= 0xFE;
			++mAccumX;
		} else {
			dirBits |= 0x01;
			--mAccumX;
		}

		mAccumX &= 0xffff;

		changed = true;
	}

	const int posY = mTargetY >> 16;
	if (mAccumY != posY) {
		if ((sint16)(mAccumY - posY) < 0) {
			++mAccumY;
			dirBits &= 0xFB;
		} else {
			--mAccumY;
			dirBits |= 0x04;
		}

		mAccumY &= 0xffff;

		changed = true;
	}

	if (changed) {
		uint32 val = dirBits + ((mAccumX << 1) & 0x02) + ((mAccumY << 3) & 0x08);

		uint32 newPortBits = (mPortBits & ~15) + val;

		if (mPortBits != newPortBits) {
			mPortBits = newPortBits;
			SetPortOutput(mPortBits);
		}
	}
}

void ATTrackballController::OnAttach() {
	if (!mpUpdateEvent)
		mpUpdateEvent = mpScheduler->AddEvent(32, this, 1);
}

void ATTrackballController::OnDetach() {
	if (mpUpdateEvent) {
		mpScheduler->RemoveEvent(mpUpdateEvent);
		mpUpdateEvent = NULL;
		mpScheduler = NULL;
	}
}

///////////////////////////////////////////////////////////////////////////

ATPaddleController::ATPaddleController()
	: mbSecond(false)
	, mPortBits(0)
	, mRawPos((228 << 16) + 0x8000)
	, mRotIndex(0)
	, mRotXLast(0)
	, mRotYLast(0)
{
	memset(mRotX, 0, sizeof mRotX);
	memset(mRotY, 0, sizeof mRotY);
}

ATPaddleController::~ATPaddleController() {
}

void ATPaddleController::SetHalf(bool second) {
	mbSecond = second;
}

void ATPaddleController::AddDelta(int delta) {
	int oldPos = mRawPos;

	mRawPos -= delta * 113;

	if (mRawPos < (1 << 16) + 0x8000)
		mRawPos = (1 << 16) + 0x8000;

	if (mRawPos > (228 << 16) + 0x8000)
		mRawPos = (228 << 16) + 0x8000;

	int newPos = mRawPos;

	if (newPos != oldPos)
		SetPotHiPosition(mbSecond, newPos);
}

void ATPaddleController::SetTrigger(bool enable) {
	const uint32 newbits = (enable ? mbSecond ? 0x08 : 0x04 : 0x00);

	if (mPortBits != newbits) {
		mPortBits = newbits;
		SetPortOutput(newbits);
	}
}

void ATPaddleController::SetDigitalTrigger(uint32 trigger, bool state) {
	if (trigger == kATInputTrigger_Button0)
		SetTrigger(state);
	else if (trigger == kATInputTrigger_Axis0) {
		const int pos = state ? (1 << 16) + 0x8000 : (228 << 16) + 0x8000;

		if (mRawPos != pos) {
			mRawPos = pos;
			SetPotHiPosition(mbSecond, pos, state);
		}
	}
}

void ATPaddleController::ApplyAnalogInput(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Axis0:
			{
				int oldPos = mRawPos;
				mRawPos = (114 << 16) + 0x8000 - ds * 114;

				if (mRawPos < (1 << 16) + 0x8000)
					mRawPos = (1 << 16) + 0x8000;

				if (mRawPos > (229 << 16) + 0x8000)
					mRawPos = (229 << 16) + 0x8000;

				int newPos = mRawPos;

				if (newPos != oldPos)
					SetPotHiPosition(mbSecond, newPos);
			}
			break;
		case kATInputTrigger_Axis0+1:
			mRotX[mRotIndex] = ds;
			break;
		case kATInputTrigger_Axis0+2:
			mRotY[mRotIndex] = ds;
			break;
	}
}

void ATPaddleController::ApplyImpulse(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Axis0:
		case kATInputTrigger_Right:
			AddDelta(ds);
			break;

		case kATInputTrigger_Left:
			AddDelta(-ds);
			break;
	}
}

void ATPaddleController::Tick() {
	mRotIndex = (mRotIndex + 1) & 3;

	float x = (float)((mRotX[0] + mRotX[1] + mRotX[2] + mRotX[3] + 2) >> 2);
	float y = (float)((mRotY[0] + mRotY[1] + mRotY[2] + mRotY[3] + 2) >> 2);

	AddDelta(VDRoundToInt32((y * mRotXLast - x * mRotYLast) * (1.0f / 200000.0f)));

	mRotXLast = x;
	mRotYLast = y;
}

void ATPaddleController::OnDetach() {
	SetPotPosition(mbSecond, 228);
}

///////////////////////////////////////////////////////////////////////////

ATTabletController::ATTabletController(int styUpPos, bool invertY)
	: mPortBits(0)
	, mbStylusUp(false)
	, mbInvertY(invertY)
	, mStylusUpPos(styUpPos)
{
	mRawPos[0] = 0;
	mRawPos[1] = 0;
}

ATTabletController::~ATTabletController() {
}

void ATTabletController::SetDigitalTrigger(uint32 trigger, bool state) {
	uint32 newBits = mPortBits;

	if (trigger == kATInputTrigger_Button0) {
		if (state)
			newBits |= 1;
		else
			newBits &= ~1;
	} else if (trigger == kATInputTrigger_Button0+1) {
		if (state)
			newBits |= 4;
		else
			newBits &= ~4;
	} else if (trigger == kATInputTrigger_Button0+2) {
		if (state)
			newBits |= 8;
		else
			newBits &= ~8;
	} else if (trigger == kATInputTrigger_Button0+3) {
		mbStylusUp = state;

		SetPos(0, mRawPos[0]);
		SetPos(1, mRawPos[1]);
	}

	if (mPortBits != newBits) {
		mPortBits = newBits;
		SetPortOutput(newBits);
	}
}

void ATTabletController::ApplyAnalogInput(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Axis0+1:
			if (mbInvertY)
				ds = -ds;
			// fall through
		case kATInputTrigger_Axis0:
			SetPos(trigger - kATInputTrigger_Axis0, ds);
			break;
	}
}

void ATTabletController::ApplyImpulse(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Left:
			ds = -ds;
		case kATInputTrigger_Axis0:
		case kATInputTrigger_Right:
			AddDelta(0, -ds);
			break;

		case kATInputTrigger_Up:
			ds = -ds;
		case kATInputTrigger_Axis0+1:
		case kATInputTrigger_Down:
			if (mbInvertY)
				ds = -ds;

			AddDelta(1, ds);
			break;
	}
}

void ATTabletController::AddDelta(int axis, int delta) {
	int pos = mRawPos[axis] + delta;

	SetPos(axis, pos);
}

void ATTabletController::SetPos(int axis, int pos) {
	if (pos < -0x10000)
		pos = -0x10000;

	if (pos > 0x10000)
		pos = 0x10000;

	int oldPos = (mRawPos[axis] * 114 + 0x728000) >> 16;
	int newPos = mbStylusUp ? mStylusUpPos : (pos * 114 + 0x728000) >> 16;

	mRawPos[axis] = pos;

	if (newPos != oldPos)
		SetPotPosition(axis != 0, newPos);
}

///////////////////////////////////////////////////////////////////////////

ATJoystickController::ATJoystickController()
	: mPortBits(0)
{
}

ATJoystickController::~ATJoystickController() {
}

void ATJoystickController::SetDigitalTrigger(uint32 trigger, bool state) {
	uint32 mask = 0;

	switch(trigger) {
		case kATInputTrigger_Button0:
			mask = 0x100;
			break;

		case kATInputTrigger_Up:
			mask = 0x01;
			break;

		case kATInputTrigger_Down:
			mask = 0x02;
			break;

		case kATInputTrigger_Left:
			mask = 0x04;
			break;

		case kATInputTrigger_Right:
			mask = 0x08;
			break;

		default:
			return;
	}

	uint32 bit = state ? mask : 0;

	if ((mPortBits ^ bit) & mask) {
		mPortBits ^= mask;

		UpdatePortOutput();
	}
}

void ATJoystickController::UpdatePortOutput() {
	uint32 v = mPortBits;

	if ((v & 0x0c) == 0x0c)
		v &= ~0x0c;

	if ((v & 0x03) == 0x03)
		v &= ~0x03;

	SetPortOutput(v);
}

///////////////////////////////////////////////////////////////////////////

ATDrivingController::ATDrivingController() {
}

ATDrivingController::~ATDrivingController() {
}

void ATDrivingController::SetDigitalTrigger(uint32 trigger, bool state) {
	if (trigger == kATInputTrigger_Button0) {
		const uint32 mask = 0x100;
		uint32 bit = state ? mask : 0;

		if ((mPortBits ^ bit) & mask) {
			mPortBits ^= mask;

			SetPortOutput(mPortBits);
		}
	}
}

void ATDrivingController::ApplyImpulse(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Axis0:
		case kATInputTrigger_Right:
			AddDelta(ds);
			break;

		case kATInputTrigger_Left:
			AddDelta(-ds);
			break;
	}
}

void ATDrivingController::AddDelta(int delta) {
	int oldPos = mRawPos;

	// The driving controller does 16 clicks per rotation. We set this to
	// achieve about the same rotational speed as the paddle.
	mRawPos -= delta * 0x20000;

	static const uint8 kRotaryEncode[4] = { 0, 1, 3, 2 };

	uint32 dirBits = kRotaryEncode[mRawPos >> 30];
	uint32 change = (dirBits ^ mPortBits) & 3;
	if (change) {
		mPortBits ^= change;
		SetPortOutput(mPortBits);
	}
}

///////////////////////////////////////////////////////////////////////////

ATConsoleController::ATConsoleController(ATInputManager *im)
	: mpParent(im)
{
}

ATConsoleController::~ATConsoleController() {
}

void ATConsoleController::SetDigitalTrigger(uint32 trigger, bool state) {
	IATInputConsoleCallback *cb = mpParent->GetConsoleCallback();

	if (cb)
		cb->SetConsoleTrigger(trigger, state);
}

///////////////////////////////////////////////////////////////////////////

ATInputStateController::ATInputStateController(ATInputManager *im, uint32 flagBase)
	: mpParent(im)
	, mFlagBase(flagBase)
{
}

ATInputStateController::~ATInputStateController() {
}

void ATInputStateController::SetDigitalTrigger(uint32 trigger, bool state) {
	switch(trigger) {
		case kATInputTrigger_Flag0:
			mpParent->ActivateFlag(mFlagBase + 0, state);
			break;

		case kATInputTrigger_Flag0+1:
			mpParent->ActivateFlag(mFlagBase + 1, state);
			break;
	}
}

///////////////////////////////////////////////////////////////////////////

AT5200ControllerController::AT5200ControllerController(int index, bool trackball)
	: mbActive(false)
	, mbPotsEnabled(false)
	, mbTrackball(trackball)
	, mIndex(index)
{
	for(int i=0; i<2; ++i) {
		mPot[i] = 114 << 16;
		mJitter[i] = 0;
	}

	memset(mbKeyMatrix, 0, sizeof mbKeyMatrix);
}

AT5200ControllerController::~AT5200ControllerController() {
}

bool AT5200ControllerController::Select5200Controller(int index, bool potsEnabled) {
	bool active = (index == mIndex);

	if (active != mbActive) {
		mbActive = active;

		ATPokeyEmulator& pokey = mpPortController->GetPokey();
		if (active) {
			pokey.SetKeyMatrix(mbKeyMatrix);
			UpdateTopButtonState();
		} else {
			pokey.SetShiftKeyState(false, false);
			pokey.SetControlKeyState(false);
			pokey.SetBreakKeyState(false, false);
			pokey.SetKeyMatrix(nullptr);
		}

	}

	if (mbPotsEnabled != potsEnabled) {
		mbPotsEnabled = potsEnabled;

		if (mpPortController) {
			UpdatePot(0);
			UpdatePot(1);
		}
	}

	return active;
}

void AT5200ControllerController::SetDigitalTrigger(uint32 trigger, bool state) {
	switch(trigger) {
		case kATInputTrigger_Up:
			if (mbUp != state) {
				mbUp = state;
				mbImpulseMode = false;

				if (mbDown == state)
					SetPot(1, 114 << 16);
				else
					SetPot(1, state ? 1 << 16 : 227 << 16);
			}
			break;

		case kATInputTrigger_Down:
			if (mbDown != state) {
				mbDown = state;
				mbImpulseMode = false;

				if (mbUp == state)
					SetPot(1, 114 << 16);
				else
					SetPot(1, state ? 227 << 16 : 1 << 16);
			}
			break;

		case kATInputTrigger_Left:
			if (mbLeft != state) {
				mbLeft = state;
				mbImpulseMode = false;

				if (mbRight == state)
					SetPot(0, 114 << 16);
				else
					SetPot(0, state ? 1 << 16 : 227 << 16);
			}
			break;

		case kATInputTrigger_Right:
			if (mbRight != state) {
				mbRight = state;
				mbImpulseMode = false;

				if (mbLeft == state)
					SetPot(0, 114 << 16);
				else
					SetPot(0, state ? 227 << 16 : 1 << 16);
			}
			break;

		case kATInputTrigger_Button0:
			SetPortOutput(state ? 0x100 : 0);
			break;

		case kATInputTrigger_Button0+1:
			mbTopButton = state;
			UpdateTopButtonState();
			break;

		case kATInputTrigger_5200_0:
			SetKeyState(0x02, state);
			break;

		case kATInputTrigger_5200_1:
			SetKeyState(0x0F, state);
			break;

		case kATInputTrigger_5200_2:
			SetKeyState(0x0E, state);
			break;

		case kATInputTrigger_5200_3:
			SetKeyState(0x0D, state);
			break;

		case kATInputTrigger_5200_4:
			SetKeyState(0x0B, state);
			break;

		case kATInputTrigger_5200_5:
			SetKeyState(0x0A, state);
			break;

		case kATInputTrigger_5200_6:
			SetKeyState(0x09, state);
			break;

		case kATInputTrigger_5200_7:
			SetKeyState(0x07, state);
			break;

		case kATInputTrigger_5200_8:
			SetKeyState(0x06, state);
			break;

		case kATInputTrigger_5200_9:
			SetKeyState(0x05, state);
			break;

		case kATInputTrigger_5200_Pound:
			SetKeyState(0x01, state);
			break;

		case kATInputTrigger_5200_Star:
			SetKeyState(0x03, state);
			break;

		case kATInputTrigger_5200_Reset:
			SetKeyState(0x04, state);
			break;

		case kATInputTrigger_5200_Pause:
			SetKeyState(0x08, state);
			break;

		case kATInputTrigger_5200_Start:
			SetKeyState(0x0C, state);
			break;
	}
}

void AT5200ControllerController::ApplyImpulse(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Left:
			ApplyImpulse(kATInputTrigger_Axis0, -ds);
			break;

		case kATInputTrigger_Right:
			ApplyImpulse(kATInputTrigger_Axis0, ds);
			break;

		case kATInputTrigger_Up:
			ApplyImpulse(kATInputTrigger_Axis0+1, -ds);
			break;

		case kATInputTrigger_Down:
			ApplyImpulse(kATInputTrigger_Axis0+1, ds);
			break;

		case kATInputTrigger_Axis0:
		case kATInputTrigger_Axis0+1:
			{
				const int index = (int)(trigger - kATInputTrigger_Axis0);

				if (mbTrackball) {
					mbImpulseMode = true;

					mImpulseAccum[index] += (float)ds * 1e-5f;
				} else {
					SetPot(index, mPot[index] + ds * 113);
				}
			}
			break;
	}
}

void AT5200ControllerController::ApplyAnalogInput(uint32 trigger, int ds) {
	switch(trigger) {
		case kATInputTrigger_Axis0:
			mbImpulseMode = false;
			SetPot(0, ds * 113 + (114 << 16) + 0x8000);
			break;

		case kATInputTrigger_Axis0+1:
			mbImpulseMode = false;
			SetPot(1, ds * 113 + (114 << 16) + 0x8000);
			break;
	}
}

void AT5200ControllerController::Tick() {
	if (mbImpulseMode) {
		for(int i=0; i<2; ++i) {
			float v = mImpulseAccum[i];

			if (v < -1.0f)
				v = -1.0f;

			if (v > 1.0f)
				v = 1.0f;

			mImpulseAccum2[i] += VDRoundToInt((v * 0.70f) * (float)(114 << 16));
			mImpulseAccum[i] = 0;

			// We have a little bit of a cheat here. 5200 games commonly truncate LSBs, which causes
			// small deltas to get lost. To work around this issue, we accumulate and only send deltas
			// once they exceed +/-4 on the POT counter.
			int idx = (mImpulseAccum2[i] + (1 << 17)) >> 18;
			mImpulseAccum2[i] -= idx << 18;

			SetPot(i, (idx * 4 + 114 + (idx < 0 ? -2 : idx > 0 ? +2 : 0)) << 16);
		}
	}

	for(int i=0; i<2; ++i) {
		if (mJitter[i]) {
			mPot[i] += mJitter[i];
			mJitter[i] = 0;
			UpdatePot(i);
		}
	}
}

void AT5200ControllerController::OnAttach() {
	UpdatePot(0);
	UpdatePot(1);
}

void AT5200ControllerController::OnDetach() {
	mpPortController->ResetPotPositions();
}

void AT5200ControllerController::SetKeyState(uint8 index, bool state) {
	index += index;

	if (mbKeyMatrix[index] != state) {
		mbKeyMatrix[index] = state;
		mbKeyMatrix[index+1] = state;
		mbKeyMatrix[index+0x20] = state;
		mbKeyMatrix[index+0x21] = state;

		if (mbActive)
			mpPortController->GetPokey().SetKeyMatrix(mbKeyMatrix);
	}
}

void AT5200ControllerController::UpdateTopButtonState() {
	if (!mbActive)
		return;

	ATPokeyEmulator& pokey = mpPortController->GetPokey();
	pokey.SetShiftKeyState(mbTopButton, false);
	pokey.SetControlKeyState(mbTopButton);
	pokey.SetBreakKeyState(mbTopButton, false);
}

void AT5200ControllerController::SetPot(int index, int pos) {
	// 5200 Galaxian has an awful bug in its calibration routine. It was meant to
	// use separate calibration variables for controllers 1 and 2, but due to
	// the following sequence:
	//
	//		LDX $D8
	//		AND #$01
	//		BNE $BD86
	//
	// ...it actually splits even and odd pot positions. This prevents the
	// calibration from working if the controller is only pushed to three positions
	// where center and right have different even/odd polarity. To fix this, we
	// apply a tiny jitter to the controller over a couple of frames.

	int& pot = mPot[index];
	int& jitter = mJitter[index];

	if (pos > pot) {
		pot = pos - (1 << 16);
		jitter = (1 << 16);
		UpdatePot(index);
	} else if (pos < pot) {
		pot = pos + (1 << 16);
		jitter = -(1 << 16);
		UpdatePot(index);
	}
}

void AT5200ControllerController::UpdatePot(int index) {
	// Note that we must not return $E4, which is used by many games to detect
	// either a trackball or a disconnected controller. In particular, Vanguard
	// breaks.

	int& pot = mPot[index];

	if (mbTrackball) {
		// Pengo barfs if the trackball returns values that are more than 80
		// units from the center position.
		if (pot < (38 << 16))
			pot = (38 << 16);
		else if (pot > (189 << 16))
			pot = (189 << 16);

		SetPotPosition(index != 0, mbPotsEnabled ? pot >> 16 : 114);
	} else {
		if (pot < (1 << 16))
			pot = (1 << 16);
		else if (pot > (227 << 16))
			pot = (227 << 16);

		SetPotPosition(index != 0, mbPotsEnabled ? pot >> 16 : 228);
	}
}

//////////////////////////////////////////////////////////////////////////

ATLightPenController::ATLightPenController()
	: mPortBits(0)
	, mpScheduler(NULL)
	, mpLightPen(NULL)
	, mpLPEvent(NULL)
	, mPosX(0)
	, mPosY(0)
	, mbPenDown(false)
{
}

ATLightPenController::~ATLightPenController() {
}

void ATLightPenController::Init(ATScheduler *fastScheduler, ATLightPenPort *lpp) {
	mpScheduler = fastScheduler;
	mpLightPen = lpp;
}

void ATLightPenController::SetDigitalTrigger(uint32 trigger, bool state) {
	uint32 mask = 0;

	switch(trigger) {
		case kATInputTrigger_Button0:
			mask = 0x01;
			state = !state;
			break;

		case kATInputTrigger_Button0+1:
			mask = 0x04;
			break;

		case kATInputTrigger_Button0+2:
			mbPenDown = state;

			if (mpLPEvent) {
				mpScheduler->RemoveEvent(mpLPEvent);
				mpLPEvent = NULL;
			}
			break;

		default:
			return;
	}

	uint32 bit = state ? mask : 0;

	if ((mPortBits ^ bit) & mask) {
		mPortBits ^= mask;
		SetPortOutput(mPortBits);
	}
}

void ATLightPenController::ApplyImpulse(uint32 trigger, int ds) {
	if (!ds)
		return;

	switch(trigger) {
		case kATInputTrigger_Left:
			ds = -ds;
			// fall through
		case kATInputTrigger_Axis0:
		case kATInputTrigger_Right:
			ApplyAnalogInput(kATInputTrigger_Axis0, mPosX + ds);
			break;

		case kATInputTrigger_Up:
			ds = -ds;
			// fall through
		case kATInputTrigger_Axis0+1:
		case kATInputTrigger_Down:
			ApplyAnalogInput(kATInputTrigger_Axis0+1, mPosY + ds);
			break;
	}
}

void ATLightPenController::ApplyAnalogInput(uint32 trigger, int ds) {
	// The center of the screen is at (128, 64), while the full visible area
	// is from (34, 4)-(222, 124). This gives a range of 188x120, with roughly
	// square pixels (PAL). We ignore the NTSC aspect and map a 188x188 area,
	// discarding about 36% of the vertical area to make a squarish mapping.

	switch(trigger) {
		case kATInputTrigger_Axis0:
			mPosX = ds;
			if (mPosX < -0x10000)
				mPosX = -0x10000;
			else if (mPosX > 0x10000)
				mPosX = 0x10000;
			break;

		case kATInputTrigger_Axis0+1:
			mPosY = ds;
			if (mPosY < -0xa367)
				mPosY = -0xa367;
			else if (mPosY > 0xa367)
				mPosY = 0xa367;
			break;
	}
}

void ATLightPenController::Tick() {
	// X range is [17, 111].
	// Y range is [4, 123].

	int x = (mPosX*94 + 0x808000) >> 16;
	int y = (mPosY*188 + 0x808000) >> 16;

	uint32 delay = 114 * (y + mpLightPen->GetAdjustY()) + ((x >> 1) + mpLightPen->GetAdjustX()) + 21 + 228;

	mbOddPhase = (x & 1) != 0;

	if (mbPenDown)
		mpScheduler->SetEvent(delay, this, 1, mpLPEvent);

	if (mPortBits & 0x100) {
		mPortBits &= ~0x100;
		SetPortOutput(mPortBits);
	}
}

void ATLightPenController::OnScheduledEvent(uint32 id) {
	if (!(mPortBits & 0x100)) {
		mPortBits |= 0x100;
		mpLightPen->SetColorClockPhase(mbOddPhase);
		SetPortOutput(mPortBits);
	}
	
	mpLPEvent = NULL;
}

void ATLightPenController::OnAttach() {
}

void ATLightPenController::OnDetach() {
	if (mpLPEvent) {
		mpScheduler->RemoveEvent(mpLPEvent);
		mpLPEvent = NULL;
	}
}

//////////////////////////////////////////////////////////////////////////

ATKeypadController::ATKeypadController()
	: mPortBits(0x1F)
{
}

ATKeypadController::~ATKeypadController() {
}

void ATKeypadController::SetDigitalTrigger(uint32 trigger, bool state) {
	// DPRTA
	// 00000	-
	// 00001	+/ENTER
	// 00010	.
	// 00011	0
	// 00100	3
	// 00101	2
	// 00110	1
	// 00111	Y
	// 01000	9
	// 01001	8
	// 01010	7
	// 01011	N
	// 01100	6
	// 01101	5
	// 01110	4
	// 01111	DEL
	// 10000
	// 10001
	// 10010
	// 10011	ESC
	// 10100
	// 10101
	// 10110
	// 10111
	// 11000
	// 11001
	// 11010
	// 11011
	// 11100
	// 11101
	// 11110
	// 11111

	static const uint8 kButtonLookup[17]={
		0x06, 0x05, 0x04,	// 1 2 3
		0x0E, 0x0D, 0x0C,	// 4 5 6
		0x0A, 0x09, 0x08,	// 7 8 9
		0x03,	// 0
		0x02,	// .
		0x01,	// +/ENTER
		0x00,	// -
		0x07,	// Y
		0x0B,	// N
		0x0F,	// DEL
		0x13	// ESC
	};

	trigger -= kATInputTrigger_Button0;

	if (trigger >= 17)
		return;

	if (state) {
		uint8 code = kButtonLookup[trigger];
		mPortBits = 0x100 + (code & 0x0F);

		SetPotPosition(true, code & 0x10 ? 228 : 0);
	} else
		mPortBits &= ~0x100;

	SetPortOutput(mPortBits);
}

void ATKeypadController::OnAttach() {
}

void ATKeypadController::OnDetach() {
	mpPortController->ResetPotPositions();
}

//////////////////////////////////////////////////////////////////////////

ATKeyboardController::ATKeyboardController()
	: mKeyState(0)
{
}

ATKeyboardController::~ATKeyboardController() {
}

void ATKeyboardController::SetDigitalTrigger(uint32 trigger, bool state) {
	trigger -= kATInputTrigger_Button0;

	if (trigger >= 12)
		return;

	const uint32 bit = (1 << trigger);
	
	if (state)
		mKeyState |= bit;
	else
		mKeyState &= ~bit;

	UpdatePortOutput();
}

void ATKeyboardController::OnAttach() {
	// The four inputs are used as row select lines, so enable output monitoring.
	SetOutputMonitorMask(15);
}

void ATKeyboardController::OnDetach() {
	mpPortController->ResetPotPositions();
}

void ATKeyboardController::OnPortOutputChanged(uint8 outputState) {
	UpdatePortOutput();
}

void ATKeyboardController::UpdatePortOutput() {
	// The keyboard controller uses a keyboard matrix as follows:
	//
	// [1]-[2]-[3]------- pin 1 (up)
	//  |   |   |
	// [4]-[5]-[6]------- pin 2 (down)
	//  |   |   |
	// [7]-[8]-[9]------- pin 3 (left)
	//  |   |   |
	// [*]-[0]-[#]------- pin 4 (right)
	//  |   |   |
	//  |   |   +-------- pin 6 (trigger)
	//  |   |
	//  |   +------------ pin 9 (paddle A)
	//  |
	//  +---------------- pin 5 (paddle B)
	//
	// Thus, it is a simple row/column matrix. The switches are
	// designed to pull down the paddle or trigger lines to ground
	// (there are pull-ups on the paddle lines). There are no diodes,
	// so holding down more than one button or pulling down more than
	// one of the row lines can give phantoms.

	// Check for no rows or no columns.
	uint8 colState = 0;

	uint8 activeRows = ~mPortOutputState & 15;
	if (mKeyState && activeRows) {
		// Check for exactly one row and column.
		if (!(mKeyState & (mKeyState - 1)) && !(activeRows & (activeRows - 1))) {
			switch(activeRows) {
				case 1:
					colState = mKeyState & 7;
					break;

				case 2:
					colState = (mKeyState >> 3) & 7;
					break;

				case 4:
					colState = (mKeyState >> 6) & 7;
					break;

				case 8:
					colState = (mKeyState >> 9) & 7;
					break;
			}
		} else {
			// Ugh. Form a unified bitmask with rows and columns, then run over the
			// matrix until we have the transitive closure.
			uint8 mergedMask = activeRows << 3;

			static const uint8 kButtonConnections[12]={
				0x09, 0x0A, 0x0C,
				0x11, 0x12, 0x14,
				0x21, 0x22, 0x24,
				0x41, 0x42, 0x44
			};

			bool changed;
			do {
				changed = false;

				for(int i=0; i<12; ++i) {
					if (!(mKeyState & (1 << i)))
						continue;

					// check if some but not all lines connected by this button
					// are asserted
					const uint8 conn = kButtonConnections[i];
					if ((mergedMask & conn) && (~mergedMask & conn)) {
						// assert all lines and mark a change
						mergedMask |= conn;
						changed = true;
					}
				}
			} while(changed);

			colState = mergedMask & 7;
		}
	}

	// When buttons are depressed, the pot lines are grounded and are
	// guaranteed to never charge up to threshold. When they aren't,
	// pull-up resistors charge the caps. The value in POT0/1 that
	// results is low but non-zero. Also, the more buttons that are
	// held down, the faster the charge rate and the lower the POT
	// values, due to additional current being supplied by the PIA
	// outputs. Approximate readings in slow and fast pot modes (130XE):
	//
	//	Buttons		Slow	Fast
	//	   1		  2		 AA
	//	   2		  1		 85
	//	   3		  1		 63
	//
	// If columns are independent, they vary independently. If they
	// are bridged, then both columns get the average effect. The
	// trigger column is not involved. However, read timing does in
	// fast mode due to the caps being charged and discharged. For now, we
	// approximate it.

	uint32 column1Keys = mKeyState & 0x249;
	uint32 column2Keys = mKeyState & 0x492;
	int floorPos1 = 0x20000 - 0x5316 * std::min(2, VDCountBits(column1Keys));
	int floorPos2 = 0x20000 - 0x5316 * std::min(2, VDCountBits(column2Keys));

	// check if columns are connected
	if (column1Keys & (column2Keys >> 1)) {
		const int avg = (floorPos1 + floorPos2) >> 1;

		floorPos1 = avg;
		floorPos2 = avg;
	}

	SetPotHiPosition(false, (colState & 2) ? 255 << 16 : floorPos2, (colState & 2) != 0);
	SetPotHiPosition(true, (colState & 1) ? 255 << 16 : floorPos1, (colState & 1) != 0);
	SetPortOutput(colState & 4 ? 0x100 : 0);
}
