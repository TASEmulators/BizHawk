//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2012 Avery Lee
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
#include <vd2/system/bitmath.h>
#include <at/atcore/scheduler.h>
#include "pia.h"
#include "irqcontroller.h"
#include "console.h"
#include "savestate.h"
#include "trace.h"

ATPIAEmulator::ATPIAEmulator()
	: mpScheduler(nullptr)
	, mpIRQController(nullptr)
	, mpFloatingInputs(nullptr)
	, mInput(0xFFFFFFFF)
	, mOutput(0xFFFFFFFF)
	, mPortOutput(0)
	, mPortDirection(0)
	, mPORTACTL(0)
	, mPORTBCTL(0)
	, mbPIAEdgeA(false)
	, mbPIAEdgeB(false)
	, mbCA1(true)
	, mbCB1(true)
	, mPIACB2(kPIACS_Floating)
	, mOutputReportMask(0)
	, mOutputAllocBitmap(0)
	, mInputAllocBitmap(0)
{
	for(int i=0; i<4; ++i)
		mInputs[i] = 0xFFFF;

	memset(mOutputs, 0, sizeof mOutputs);
}

int ATPIAEmulator::AllocInput() {
	if (mInputAllocBitmap == 15) {
		VDASSERT(!"PIA inputs exhausted.");
		return -1;
	}

	int idx = VDFindLowestSetBitFast(~mInputAllocBitmap);

	mInputAllocBitmap |= (1 << idx);
	return idx;
}

void ATPIAEmulator::FreeInput(int index) {
	if (index >= 0) {
		const uint32 allocBit = UINT32_C(1) << index;

		VDASSERT(mInputAllocBitmap & allocBit);

		if (mInputAllocBitmap & allocBit) {
			mInputAllocBitmap &= ~allocBit;

			SetInput(index, ~UINT32_C(0));
		}
	}
}

void ATPIAEmulator::SetInput(int index, uint32 rval) {
	VDASSERT(index < 4);

	if (index >= 0 && rval != mInputs[index]) {
		mInputs[index] = rval;

		uint32 v = mInputs[0] & mInputs[1] & mInputs[2] & mInputs[3];

		if (mInput != v) {
			mInput = v;

			UpdateOutput();
		}
	}
}

int ATPIAEmulator::AllocOutput(ATPortOutputFn fn, void *ptr, uint32 changeMask) {
	if (mOutputAllocBitmap == (1U << vdcountof(mOutputs))) {
		VDASSERT(!"PIA outputs exhausted.");
		return -1;
	}

	int idx = VDFindLowestSetBitFast(~mOutputAllocBitmap);

	mOutputAllocBitmap |= (1 << idx);

	OutputEntry& output = mOutputs[idx];
	output.mChangeMask = changeMask;
	output.mpFn = fn;
	output.mpData = ptr;

	mOutputReportMask |= changeMask;

	return idx;
}

void ATPIAEmulator::ModifyOutputMask(int index, uint32 changeMask) {
	if (index < 0)
		return;

	VDASSERT(mOutputAllocBitmap & (1 << index));

	if (mOutputs[index].mChangeMask != changeMask) {
		mOutputs[index].mChangeMask = changeMask;

		mOutputReportMask = 0;
		for(const auto& output : mOutputs)
			mOutputReportMask |= output.mChangeMask;
	}
}

void ATPIAEmulator::FreeOutput(int index) {
	if (index >= 0) {
		mOutputAllocBitmap &= ~(1 << index);

		mOutputs[index].mChangeMask = 0;

		mOutputReportMask = 0;
		for(const auto& output : mOutputs)
			mOutputReportMask |= output.mChangeMask;
	}
}

void ATPIAEmulator::SetTraceContext(ATTraceContext *context) {
	if (mpTraceContext == context)
		return;

	mpTraceContext = context;

	if (context) {
		ATTraceGroup *group = context->mpCollection->AddGroup(L"PIA");

		mpTraceCRA = group->AddFormattedChannel(context->mBaseTime, context->mBaseTickScale, L"CRA");
		mpTraceCRB = group->AddFormattedChannel(context->mBaseTime, context->mBaseTickScale, L"CRB");
	} else {
		const uint64 t = mpScheduler->GetTick64();

		if (mpTraceCRA) {
			mpTraceCRA->TruncateLastEvent(t);
			mpTraceCRA = nullptr;
		}

		if (mpTraceCRB) {
			mpTraceCRB->TruncateLastEvent(t);
			mpTraceCRB = nullptr;
		}
	}
}

void ATPIAEmulator::Init(ATIRQController *irqcon, ATScheduler *scheduler) {
	mpIRQController = irqcon;
	mpScheduler = scheduler;
}

void ATPIAEmulator::ColdReset() {
	if (mpFloatingInputs)
		memset(mpFloatingInputs->mFloatTimers, 0, sizeof mpFloatingInputs->mFloatTimers);

	WarmReset();
}

void ATPIAEmulator::WarmReset() {
	// need to do this to float inputs
	SetPortBDirection(0);

	mPortOutput = kATPIAOutput_CA2 | kATPIAOutput_CB2;
	mPortDirection = kATPIAOutput_CA2 | kATPIAOutput_CB2;
	SetCRA(0);
	SetCRB(0);
	mbPIAEdgeA = false;
	mbPIAEdgeB = false;
	mPIACB2 = kPIACS_Floating;

	NegateIRQs(kATIRQSource_PIAA1 | kATIRQSource_PIAA2 | kATIRQSource_PIAB1 | kATIRQSource_PIAB2);

	UpdateOutput();
}

void ATPIAEmulator::SetCA1(bool level) {
	if (mbCA1 == level)
		return;

	mbCA1 = level;

	// check that the interrupt isn't already active
	if (mPORTACTL & 0x80)
		return;

	// check if we have the correct transition
	if (mPORTACTL & 0x02) {
		if (!level)
			return;
	} else {
		if (level)
			return;
	}

	// set interrupt flag
	SetCRA(mPORTACTL | 0x80);

	// assert IRQ if enabled
	if ((mPORTACTL & 0x01) && mpIRQController)
		AssertIRQs(kATIRQSource_PIAA1);
}

void ATPIAEmulator::SetCB1(bool level) {
	if (mbCB1 == level)
		return;

	mbCB1 = level;

	// check that the interrupt isn't already active
	if (mPORTBCTL & 0x80)
		return;

	// check if we have the correct transition
	if (mPORTBCTL & 0x02) {
		if (!level)
			return;
	} else {
		if (level)
			return;
	}

	// set interrupt flag
	SetCRB(mPORTBCTL | 0x80);

	// assert IRQ if enabled
	if ((mPORTBCTL & 0x01) && mpIRQController)
		AssertIRQs(kATIRQSource_PIAB1);
}

uint8 ATPIAEmulator::DebugReadByte(uint8 addr) const {
	switch(addr & 0x03) {
	case 0x00:
	default:
		// Port A reads the actual state of the output lines.
		return mPORTACTL & 0x04
			? (uint8)(mInput & (mOutput | ~mPortDirection))
			: (uint8)mPortDirection;

	case 0x01:
		// return DDRB if selected
		if (!(mPORTBCTL & 0x04))
			return (uint8)(mPortDirection >> 8);

		// Port B reads output bits instead of input bits for those selected as output. No ANDing with input.
		{
			uint8 pb = (uint8)((((mInput ^ mOutput) & mPortDirection) ^ mInput) >> 8);

			// If we have floating bits, roll them in.
			if (mpFloatingInputs) {
				const uint8 visibleFloatingBits = mpFloatingInputs->mFloatingInputMask & ~(mPortDirection >> 8);
				
				if (visibleFloatingBits) {
					const uint64 t64 = mpFloatingInputs->mpScheduler->GetTick64();
					uint8 bit = 0x01;

					for(int i=0; i<8; ++i, (bit += bit)) {
						if (visibleFloatingBits & bit) {
							// Turn off this bit if we've passed the droop deadline.
							if (t64 >= mpFloatingInputs->mFloatTimers[i])
								pb &= ~bit;
						}
					}
				}
			}

			return pb;
		}

	case 0x02:
		return mPORTACTL;

	case 0x03:
		return mPORTBCTL;
	}
}

uint8 ATPIAEmulator::ReadByte(uint8 addr) {
	switch(addr & 0x03) {
	case 0x00:
		if (mPORTACTL & 0x04) {
			// Reading the PIA port A data register negates port A interrupts.
			SetCRA(mPORTACTL & 0x3F);

			NegateIRQs(kATIRQSource_PIAA1 | kATIRQSource_PIAA2);
		}
		break;

	case 0x01:
		if (mPORTBCTL & 0x04) {
			// Reading the PIA port A data register negates port B interrupts.
			SetCRB(mPORTBCTL & 0x3F);

			NegateIRQs(kATIRQSource_PIAB1 | kATIRQSource_PIAB2);
		}

		break;

	case 0x02:
		return mPORTACTL;
	case 0x03:
		return mPORTBCTL;
	}

	return DebugReadByte(addr);
}

void ATPIAEmulator::WriteByte(uint8 addr, uint8 value) {
	switch(addr & 0x03) {
	case 0x00:
		{
			uint32 delta;
			if (mPORTACTL & 0x04) {
				delta = (mPortOutput ^ value) & 0xff;
				if (!delta)
					return;

				mPortOutput ^= delta;
			} else {
				delta = (mPortDirection ^ value) & 0xff;

				if (!delta)
					return;

				mPortDirection ^= delta;
			}

			UpdateOutput();
		}
		break;
	case 0x01:
		{
			uint32 delta;
			if (mPORTBCTL & 0x04) {
				delta = (mPortOutput ^ ((uint32)value << 8)) & 0xff00;
				if (!delta)
					return;

				mPortOutput ^= delta;
			} else {
				if (!SetPortBDirection(value))
					return;
			}

			UpdateOutput();
		}
		break;
	case 0x02:
		switch(value & 0x38) {
			case 0x00:
			case 0x08:
			case 0x28:
				mbPIAEdgeA = false;
				break;

			case 0x10:
			case 0x18:
				if (mbPIAEdgeA) {
					mbPIAEdgeA = false;
					SetCRA(mPORTACTL | 0x40);
				}
				break;

			case 0x20:
			case 0x38:
				break;

			case 0x30:
				mbPIAEdgeA = true;
				break;
		}

		{
			uint8 cra = mPORTACTL;

			if (value & 0x20)
				cra &= ~0x40;

			SetCRA((cra & 0xc0) + (value & 0x3f));
		}

		if (mpIRQController) {
			if ((mPORTACTL & 0x68) == 0x48)
				AssertIRQs(kATIRQSource_PIAA2);
			else
				NegateIRQs(kATIRQSource_PIAA2);

			if ((mPORTACTL & 0x81) == 0x81)
				AssertIRQs(kATIRQSource_PIAA1);
			else
				NegateIRQs(kATIRQSource_PIAA1);
		}

		UpdateCA2();
		break;
	case 0x03:
		switch(value & 0x38) {
			case 0x00:
			case 0x08:
			case 0x10:
			case 0x18:
				if (mbPIAEdgeB) {
					mbPIAEdgeB = false;
					SetCRB(mPORTBCTL | 0x40);
				}

				mPIACB2 = kPIACS_Floating;
				break;

			case 0x20:
				mbPIAEdgeB = false;
				break;

			case 0x28:
				mbPIAEdgeB = false;
				mPIACB2 = kPIACS_High;
				break;

			case 0x30:
				mbPIAEdgeB = false;
				mPIACB2 = kPIACS_Low;
				break;

			case 0x38:
				if (mPIACB2 == kPIACS_Low)
					mbPIAEdgeB = true;

				mPIACB2 = kPIACS_High;
				break;
		}

		{
			uint8 crb = mPORTBCTL;

			if (value & 0x20)
				crb &= ~0x40;

			SetCRB((crb & 0xc0) + (value & 0x3f));
		}

		if (mpIRQController) {
			if ((mPORTBCTL & 0x68) == 0x48)
				AssertIRQs(kATIRQSource_PIAB2);
			else
				NegateIRQs(kATIRQSource_PIAB2);

			if ((mPORTBCTL & 0x81) == 0x81)
				AssertIRQs(kATIRQSource_PIAB1);
			else
				NegateIRQs(kATIRQSource_PIAB1);
		}

		UpdateCB2();
		break;
	}
}

void ATPIAEmulator::SetPortBFloatingInputs(ATPIAFloatingInputs *inputs) {
	mpFloatingInputs = inputs;

	if (inputs)
		memset(inputs->mFloatTimers, 0, sizeof inputs->mFloatTimers);
}

void ATPIAEmulator::GetState(ATPIAState& state) const {
	state.mCRA = mPORTACTL;
	state.mCRB = mPORTBCTL;
	state.mDDRA = (uint8)mPortDirection;
	state.mDDRB = (uint8)(mPortDirection >> 8);
	state.mORA = (uint8)mPortOutput;
	state.mORB = (uint8)(mPortOutput >> 8);
}

void ATPIAEmulator::DumpState() {
	ATPIAState state;
	GetState(state);

	static const char *const kCAB2Modes[] = {
		"-edge sense",
		"-edge sense w/IRQ",
		"+edge sense",
		"+edge sense w/IRQ",
		"read handshake w/CA1",
		"read pulse",
		"low / on",
		"high / off",
	};

	ATConsolePrintf("Port A control:   %02x (%s, motor line: %s, proceed line: %cedge%s)\n"
		, state.mCRA
		, state.mCRA & 0x04 ? "IOR" : "DDR"
		, kCAB2Modes[(state.mCRA >> 3) & 7]
		, state.mCRA & 0x02 ? '+' : '-'
		, state.mCRA & 0x01 ? " w/IRQ" : ""
		);
	ATConsolePrintf("Port A direction: %02x\n", state.mDDRA);
	ATConsolePrintf("Port A output:    %02x\n", state.mORA);
	ATConsolePrintf("Port A edge:      %s\n", mbPIAEdgeA ? "pending" : "none");

	ATConsolePrintf("Port B control:   %02x (%s, command line: %s, interrupt line: %cedge%s)\n"
		, state.mCRB
		, state.mCRB & 0x04 ? "IOR" : "DDR"
		, kCAB2Modes[(state.mCRB >> 3) & 7]
		, state.mCRB & 0x02 ? '+' : '-'
		, state.mCRB & 0x01 ? " w/IRQ" : ""
		);
	ATConsolePrintf("Port B direction: %02x\n", state.mDDRB);
	ATConsolePrintf("Port B output:    %02x\n", state.mORB);
	ATConsolePrintf("Port B edge:      %s\n", mbPIAEdgeB ? "pending" : "none");
}

void ATPIAEmulator::BeginLoadState(ATSaveStateReader& reader) {
	reader.RegisterHandlerMethod(kATSaveStateSection_Arch, VDMAKEFOURCC('P', 'I', 'A', ' '), this, &ATPIAEmulator::LoadStateArch);
	reader.RegisterHandlerMethod(kATSaveStateSection_End, 0, this, &ATPIAEmulator::EndLoadState);
}

void ATPIAEmulator::LoadStateArch(ATSaveStateReader& reader) {
	const uint8 ora = reader.ReadUint8();
	const uint8 orb = reader.ReadUint8();
	const uint8 ddra = reader.ReadUint8();
	const uint8 ddrb = reader.ReadUint8();

	mPortOutput = ((uint32)orb << 8) + ora;
	mPortDirection = ((uint32)ddrb << 8) + ddra + kATPIAOutput_CA2 + kATPIAOutput_CB2;
	mPORTACTL = reader.ReadUint8();
	mPORTBCTL = reader.ReadUint8();
}

void ATPIAEmulator::EndLoadState(ATSaveStateReader& reader) {
	UpdateCA2();
	UpdateCB2();
	UpdateOutput();

	if (mpFloatingInputs)
		memset(mpFloatingInputs->mFloatTimers, 0, sizeof mpFloatingInputs->mFloatTimers);
}

void ATPIAEmulator::BeginSaveState(ATSaveStateWriter& writer) {
	writer.RegisterHandlerMethod(kATSaveStateSection_Arch, this, &ATPIAEmulator::SaveStateArch);
}

void ATPIAEmulator::SaveStateArch(ATSaveStateWriter& writer) {
	ATPIAState state;
	GetState(state);

	writer.BeginChunk(VDMAKEFOURCC('P', 'I', 'A', ' '));
	writer.WriteUint8(state.mORA);
	writer.WriteUint8(state.mORB);
	writer.WriteUint8(state.mDDRA);
	writer.WriteUint8(state.mDDRB);
	writer.WriteUint8(state.mCRA);
	writer.WriteUint8(state.mCRB);
	writer.EndChunk();
}

void ATPIAEmulator::UpdateCA2() {
	// bits 3-5:
	//	0xx		input (passively pulled high)
	//	100		output - set high by interrupt and low by port A data read
	//	101		output - normally set high, pulse low for one cycle by port A data read
	//	110		output - low
	//	111		output - high
	//
	// Right now we don't emulate the pulse modes.

	if ((mPORTACTL & 0x38) == 0x30)
		mPortOutput &= ~kATPIAOutput_CA2;
	else
		mPortOutput |= kATPIAOutput_CA2;

	UpdateOutput();
}

void ATPIAEmulator::UpdateCB2() {
	// bits 3-5:
	//	0xx		input (passively pulled high)
	//	100		output - set high by interrupt and low by port B data read
	//	101		output - normally set high, pulse low for one cycle by port B data read
	//	110		output - low
	//	111		output - high
	//
	// Right now we don't emulate the pulse modes.

	if ((mPORTBCTL & 0x38) == 0x30)
		mPortOutput &= ~kATPIAOutput_CB2;
	else
		mPortOutput |= kATPIAOutput_CB2;

	UpdateOutput();
}

void ATPIAEmulator::UpdateOutput() {
	const uint32 newOutput = mPortOutput | ~mPortDirection;
	const uint32 delta = mOutput ^ newOutput;

	if (!delta)
		return;

	mOutput = newOutput;

	if (delta & mOutputReportMask) {
		for(const OutputEntry& output : mOutputs) {
			if (output.mChangeMask & delta)
				output.mpFn(output.mpData, mOutput);
		}
	}
}

bool ATPIAEmulator::SetPortBDirection(uint8 value) {
	const uint32 delta = (mPortDirection ^ ((uint32)value << 8)) & 0xff00;

	if (!delta)
		return false;

	mPortDirection ^= delta;

	// Check if any bits that have transitioned from output (1) to input (0) correspond
	// to floating inputs. If so, we need to update the floating timers.
	if (mpFloatingInputs) {
		const uint8 newlyFloatingInputs = mpFloatingInputs->mFloatingInputMask & (uint8)((delta & ~mPortDirection) >> 8);

		if (newlyFloatingInputs) {
			const uint64 t64 = mpFloatingInputs->mpScheduler->GetTick64();
			const uint8 outputs = (uint8)(mPortOutput >> 8);
			uint8 bit = 0x01;

			// if we have any bits that are transitioning 1 -> floating, update the PRNG
			if (newlyFloatingInputs & outputs) {
				mpFloatingInputs->mRandomSeed ^= (uint32)t64;
				mpFloatingInputs->mRandomSeed &= 0x7FFFFFFFU;

				if (!mpFloatingInputs->mRandomSeed)
					mpFloatingInputs->mRandomSeed = 1;
			}

			for(int i=0; i<8; ++i, (bit += bit)) {
				if (newlyFloatingInputs & bit) {
					// Floating bits slowly drift toward 0. Therefore, we need to check whether
					// the output was a 0. If it is, reset the timer to 0 so it stays 0; otherwise,
					// compute a pseudorandom timeout value.
					if (outputs & bit) {
						// pull 16 bits out of 31-bit LFSR
						uint32 rval = mpFloatingInputs->mRandomSeed & 0xffff;
						mpFloatingInputs->mRandomSeed = (rval << 12) ^ (rval << 15) ^ (mpFloatingInputs->mRandomSeed >> 16);

						mpFloatingInputs->mFloatTimers[i] = t64 + mpFloatingInputs->mDecayTimeMin + (uint32)(((uint64)mpFloatingInputs->mDecayTimeRange * rval) >> 16);
					} else {
						mpFloatingInputs->mFloatTimers[i] = 0;
					}
				}
			}
		}
	}

	return true;
}

void ATPIAEmulator::NegateIRQs(uint32 mask) {
	if (!mpIRQController)
		return;

	mpIRQController->Negate(mask, true);
}

void ATPIAEmulator::AssertIRQs(uint32 mask) {
	if (!mpIRQController)
		return;

	mpIRQController->Assert(mask, true);
}

void ATPIAEmulator::SetCRA(uint8 v) {
	if (mPORTACTL == v)
		return;

	mPORTACTL = v;

	UpdateTraceCRA();
}

void ATPIAEmulator::SetCRB(uint8 v) {
	if (mPORTBCTL == v)
		return;

	mPORTBCTL = v;

	UpdateTraceCRB();
}

void ATPIAEmulator::UpdateTraceCRA() {
	if (!mpTraceCRA)
		return;

	const uint64 t = mpScheduler->GetTick64();
	mpTraceCRA->TruncateLastEvent(t);
	mpTraceCRA->AddOpenTickEventF(t, kATTraceColor_Default, L"%02X", mPORTACTL);
}

void ATPIAEmulator::UpdateTraceCRB() {
	if (!mpTraceCRB)
		return;

	const uint64 t = mpScheduler->GetTick64();
	mpTraceCRB->TruncateLastEvent(t);
	mpTraceCRB->AddOpenTickEventF(t, kATTraceColor_Default, L"%02X", mPORTBCTL);
}
