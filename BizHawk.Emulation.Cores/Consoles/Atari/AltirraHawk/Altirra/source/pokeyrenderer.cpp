//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2011 Avery Lee
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

//=========================================================================
// POKEY renderer
//
// The POKEY renderer converts register change and timer events within
// the POKEY emulation to audio output. It exclusively handles portions of
// the audio circuits that have no feedback path to the 6502 and thus are
// not observable. Most of the logic simulated here is digital, except for
// downsample from 1.77/1.79MHz to 64KHz.
//
// The general approach is as follows:
//
// - Collected timing and register change events. Timing events are
//   accumulated, while register change events are handled immediately
//   and cause a flush.
//
// - The common cases of a timer running at a regular frequency are handled
//   by the deferred mechanism, where the POKEY emulator pushes the initial
//   timing parameters into the renderer and the renderer expands the
//   periodic ticks. This greatly reduces the timer overhead in the POKEY
//   emulator itself. The 16-bit linked timer case is a bit more
//   complicated to handle the uneven ticking of the low timer.
//
// - Timer events from each channel are converted to output change events.
//   This can involve sampling the polynomial counters for clocking and
//   output noise. The polynomial tables are offset by the initial offset
//   for the frame and then all channels independently sample noise off
//   of the tables.
//
// - Timer events from ch3/4 are also converted to high pass update events
//   for ch1/2. These have to be offset by half a cycle, so the output
//   section uses half-ticks (3.55/3.58MHz).
//
// - The output path XORs the high-pass flip/flops into ch1/2, converts the
//   four channel outputs to a mix level, then box filters the stairstep
//   waveform to 1/28 rate (64KHz). This output is then sent to the audio
//   sync mixer for mixing with non-POKEY sources, AC coupling filtering,
//   and downsample to 44/48KHz.
//

#include <stdafx.h>
#include <at/atcore/logging.h>
#include <at/atcore/scheduler.h>
#include <at/atcore/wraptime.h>
#include "pokey.h"
#include "pokeyrenderer.h"
#include "pokeytables.h"
#include "savestate.h"

ATLogChannel g_ATLCPokeyTEv(false, false, "POKEYTEV", "POKEY timer events (high traffic)");

namespace {
	const uint32 kAudioDelay = 2;
}

ATPokeyRenderer::ATPokeyRenderer()
	: mpScheduler(NULL)
	, mpTables(NULL)
	, mAccum(0)
	, mHighPassAccum(0)
	, mSpeakerAccum(0)
	, mOutputLevel(0)
	, mSpeakerLevel(0)
	, mLastOutputTime2(0)
	, mLastOutputSampleTime2(0)
	, mExternalInput(0)
	, mbSpeakerState(false)
	, mAUDCTL(0)
	, mOutputSampleCount(0)
{
	for(int i=0; i<4; ++i) {
		mbChannelEnabled[i] = true;
		mChannelVolume[i] = 0;
		mAUDC[i] = 0;
	}
}

ATPokeyRenderer::~ATPokeyRenderer() {
}

void ATPokeyRenderer::Init(ATScheduler *sch, ATPokeyTables *tables) {
	mpScheduler = sch;
	mpTables = tables;

	mLastOutputTime2 = ATSCHEDULER_GETTIME(mpScheduler) * 2;
	mLastOutputSampleTime2 = mLastOutputTime2;

	mSerialPulse = 200;

	ColdReset();
}

void ATPokeyRenderer::ColdReset() {
	mbInitMode = true;
	mPolyState.mInitMask = 0;

	// preset all noise and high-pass flip/flops
	mNoiseFlipFlops = 0x0F;
	mChannelOutputMask = 0x3F;

	for(int i=0; i<4; ++i) {
		mDeferredEvents[i].mbEnabled = false;
	}

	mOutputLevel = 0;
	mSpeakerLevel = 0;
	mOutputSampleCount = 0;

	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	mLastFlushTime = t;
	mLastOutputSampleTime2 = t*2;
	mLastOutputTime2 = t*2;
	mPolyState.mLastPoly17Time = t;
	mPolyState.mLastPoly9Time = t;
	mPolyState.mLastPoly5Time = t;
	mPolyState.mLastPoly4Time = t;
	mPolyState.mPoly17Counter = 0;
	mPolyState.mPoly9Counter = 0;
	mPolyState.mPoly5Counter = 0;
	mPolyState.mPoly4Counter = 0;

	for(ChannelEdges& edges : mChannelEdges) {
		edges.clear();
	}

	// This must be done after everything else is inited, as it will start recomputing
	// derived values.
	SetAUDCTL(0);

	for(int i=0; i<4; ++i)
		SetAUDCx(i, 0);
}

void ATPokeyRenderer::SyncTo(const ATPokeyRenderer& src) {
	mLastFlushTime = src.mLastFlushTime;
	mLastOutputSampleTime2 = src.mLastOutputSampleTime2;
	mLastOutputTime2 = src.mLastOutputTime2;
	mOutputSampleCount = src.mOutputSampleCount;

	memset(mRawOutputBuffer, 0, sizeof(mRawOutputBuffer[0]) * mOutputSampleCount);
}

void ATPokeyRenderer::GetAudioState(ATPokeyAudioState& state) {
	Flush(ATSCHEDULER_GETTIME(mpScheduler));

	uint8 outputMask = (mChannelOutputMask ^ (mChannelOutputMask >> 4)) | mVolumeOnlyMask;
	for(int ch=0; ch<4; ++ch) {
		int level = outputMask & (1 << ch) ? mChannelVolume[ch] : 0;

		state.mChannelOutputs[ch] = level;
	}
}

void ATPokeyRenderer::SetChannelEnabled(int channel, bool enabled) {
	VDASSERT(channel < 4);
	if (mbChannelEnabled[channel] != enabled) {
		const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
		Flush(t);

		mbChannelEnabled[channel] = enabled;

		UpdateVolume(channel);
		UpdateOutput(t);
	}
}

void ATPokeyRenderer::SetFiltersEnabled(bool enable) {
	if (!enable)
		mHighPassAccum = 0;
}

void ATPokeyRenderer::SetInitMode(bool init) {
	if (init == mbInitMode)
		return;

	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	Flush(t);

	mbInitMode = init;
	mPolyState.mInitMask = init ? 0 : UINT32_C(0xFFFFFFFF);

	// These offsets are specifically set so that the audio output patterns
	// are correctly timed.
	mPolyState.mPoly4Counter = 8 - kAudioDelay;
	mPolyState.mPoly5Counter = 22 - kAudioDelay;
	mPolyState.mPoly9Counter = 507 - kAudioDelay;
	mPolyState.mPoly17Counter = 131067 - kAudioDelay;
	mPolyState.mLastPoly17Time = t;
	mPolyState.mLastPoly9Time = t;
	mPolyState.mLastPoly5Time = t;
	mPolyState.mLastPoly4Time = t;
}

bool ATPokeyRenderer::SetSpeaker(bool newState) {
	if (mbSpeakerState == newState)
		return false;

	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	Flush(t);

	mbSpeakerState = newState;
	UpdateOutput(t);
	return true;
}

void ATPokeyRenderer::SetAudioLine2(int v) {
	if (mExternalInput != v) {
		const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
		Flush(t);

		mExternalInput = v;
		UpdateOutput(t);
	}
}

void ATPokeyRenderer::ResetTimers() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	Flush(t);

	// preset all noise flip/flops
	mNoiseFlipFlops = 0xF;
	mChannelOutputMask |= 0xF;

	UpdateOutput(t);
}

void ATPokeyRenderer::SetAUDCx(int index, uint8 value) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	Flush(t);

	mAUDC[index] = value;

	UpdateVolume(index);

	UpdateOutput(t);
}

void ATPokeyRenderer::SetAUDCTL(uint8 value) {
	const uint8 delta = mAUDCTL ^ value;
	if (!delta)
		return;

	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);
	Flush(t);

	mAUDCTL = value;

	bool outputsChanged = false;
	if ((delta & 0x04) && !(mAUDCTL & 0x04)) {
		if (!(mChannelOutputMask & 0x10)) {
			mChannelOutputMask |= 0x10;
			outputsChanged = true;
		}
	}

	if ((delta & 0x02) && !(mAUDCTL & 0x02)) {
		if (!(mChannelOutputMask & 0x20)) {
			mChannelOutputMask |= 0x20;
			outputsChanged = true;
		}
	}

	if (outputsChanged)
		UpdateOutput(t);
}

void ATPokeyRenderer::AddChannelEvent(int channel) {
	ChannelEdges& ce = mChannelEdges[channel];
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	VDASSERT(ce.empty() || t - ce.back() < 0x80000000);
	ce.push_back(t);
}

void ATPokeyRenderer::SetChannelDeferredEvents(int channel, uint32 start, uint32 period) {
	VDASSERT((uint32)(start*2 - mLastOutputTime2) < 0x80000000);
	VDASSERT(period < 7500000);

	DeferredEvent& ev = mDeferredEvents[channel];
	ev.mbEnabled = true;
	ev.mbLinked = false;
	ev.mNextTime = start;
	ev.mPeriod = period;
}

void ATPokeyRenderer::SetChannelDeferredEventsLinked(int channel, uint32 loStart, uint32 loPeriod, uint32 hiStart, uint32 hiPeriod, uint32 loOffset) {
	VDASSERT((uint32)(loStart*2 - mLastOutputTime2) < 0x80000000);
	VDASSERT(hiStart - loStart - 1 < 0x7FFFFFFFU);		// wrapped(hiStart > loStart)
	VDASSERT(loPeriod < 30000);
	VDASSERT(hiPeriod < 7500000);

	DeferredEvent& ev = mDeferredEvents[channel];
	ev.mbEnabled = true;
	ev.mbLinked = true;
	ev.mNextTime = loStart;
	ev.mPeriod = loPeriod;
	ev.mNextHiTime = hiStart;
	ev.mHiPeriod = hiPeriod;
	ev.mHiLoOffset = loOffset;
}

void ATPokeyRenderer::ClearChannelDeferredEvents(int channel, uint32 t) {
	if (!mDeferredEvents[channel].mbEnabled)
		return;

	FlushDeferredEvents(channel, t);
	mDeferredEvents[channel].mbEnabled = false;
}

void ATPokeyRenderer::AddSerialNoisePulse(uint32 t) {
	if (mSerialPulseTimes.size() > 65536)
		return;

	if (!mSerialPulseTimes.empty() && mSerialPulseTimes.back() - t < 0x80000000)
		return;

	mSerialPulseTimes.push_back(t);
}

uint32 ATPokeyRenderer::EndBlock() {
	uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	Flush(t);

	// Merge noise samples
	const uint32 sampleCount = mOutputSampleCount;

	if (!mSerialPulseTimes.empty()) {
		const uint32 baseTime = t - sampleCount * 28;
		float pulse = mSerialPulse;

		auto it = mSerialPulseTimes.begin();
		auto itEnd = mSerialPulseTimes.end();

		while(it != itEnd) {
			const uint32 pulseTime = *it;
			const uint32 rawOffset = pulseTime - baseTime;

			if (rawOffset < 0x80000000) {
				const uint32 sampleOffset = rawOffset / 28;

				if (sampleOffset >= sampleCount)
					break;

				mRawOutputBuffer[sampleOffset] += pulse;
				pulse = -pulse;
			}
		
			++it;
		}
	
		mSerialPulse = pulse;
		mSerialPulseTimes.erase(mSerialPulseTimes.begin(), it);
	}

	mOutputSampleCount = 0;

	VDASSERT(t*2 - mLastOutputSampleTime2 <= 56);

	// prevent denormals
	if (fabsf(mHighPassAccum) < 1e-20)
		mHighPassAccum = 0;

	return sampleCount;
}

void ATPokeyRenderer::LoadState(ATSaveStateReader& reader) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	// Careful -- we save the polynomial counters in simulation time, but we
	// have to roll them back to where sound generation currently is.

	mPolyState.mPoly4Counter  = (reader.ReadUint8()  +     15 - (t - mPolyState.mLastPoly4Time) % 15) % 15;
	mPolyState.mPoly5Counter  = (reader.ReadUint8()  +     31 - (t - mPolyState.mLastPoly5Time) % 31) % 31;
	mPolyState.mPoly9Counter  = (reader.ReadUint16() +    511 - (t - mPolyState.mLastPoly9Time) % 511) % 511;
	mPolyState.mPoly17Counter = (reader.ReadUint32() + 131071 - (t - mPolyState.mLastPoly17Time) % 131071) % 131071;

	mNoiseFlipFlops = 0;
	for(int i=0; i<4; ++i)
		mNoiseFlipFlops += (reader.ReadUint8() & 1) << i;

	mChannelOutputMask = mNoiseFlipFlops;
	for(int i=0; i<2; ++i)
		mChannelOutputMask += (reader.ReadUint8() & 1) << (i + 4);

	// discard outputs (no longer needed -- high-pass XOR is done dynamically now)
	for(int i=0; i<4; ++i)
		reader.ReadUint8();
}

void ATPokeyRenderer::ResetState() {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	mPolyState.mPoly4Counter  = (    15 - (t - mPolyState.mLastPoly4Time ) % 15) % 15;
	mPolyState.mPoly5Counter  = (    31 - (t - mPolyState.mLastPoly5Time ) % 31) % 31;
	mPolyState.mPoly9Counter  = (   511 - (t - mPolyState.mLastPoly9Time ) % 511) % 511;
	mPolyState.mPoly17Counter = (131071 - (t - mPolyState.mLastPoly17Time) % 131071) % 131071;
	
	mNoiseFlipFlops = 0xF;
	mChannelOutputMask = 0x3F;
}

void ATPokeyRenderer::SaveState(ATSaveStateWriter& writer) {
	const uint32 t = ATSCHEDULER_GETTIME(mpScheduler);

	// Careful -- we can't update polynomial counters here like we do in the
	// main POKEY module. That's because the polynomial counters have to be
	// advanced by sound rendering and not by the simulation.

	writer.WriteUint8 ((mPolyState.mPoly4Counter  + (mPolyState.mInitMask & (mPolyState.mLastPoly4Time  - t))) % 15);
	writer.WriteUint8 ((mPolyState.mPoly5Counter  + (mPolyState.mInitMask & (mPolyState.mLastPoly5Time  - t))) % 31);
	writer.WriteUint16((mPolyState.mPoly9Counter  + (mPolyState.mInitMask & (mPolyState.mLastPoly9Time  - t))) % 511);
	writer.WriteUint32((mPolyState.mPoly17Counter + (mPolyState.mInitMask & (mPolyState.mLastPoly17Time - t))) % 131071);

	for(int i=0; i<4; ++i) {
		writer.WriteUint8((mNoiseFlipFlops >> i) & 1);
	}

	for(int i=0; i<2; ++i)
		writer.WriteUint8((mChannelOutputMask >> (4 + i)) & 1);

	const uint8 outputs = (mChannelOutputMask ^ (mChannelOutputMask >> 4)) | mVolumeOnlyMask;
	for(int i=0; i<4; ++i)
		writer.WriteUint8((outputs >> i) & 1);

	// mbInitMode is restored by the POKEY emulator.
	// AUDCTL is restored by the POKEY emulator.
	// AUDCx are restored by the POKEY emulator.
}

void ATPokeyRenderer::FlushDeferredEvents(int channel, uint32 t) {
	DeferredEvent de = mDeferredEvents[channel];

	VDASSERT(de.mNextTime*2 - mLastOutputTime2 < 0x80000000);

	ChannelEdges& ce = mChannelEdges[channel];

	VDASSERT(ce.empty() || de.mNextTime - ce.back() < 0x80000000);		// wrap(nextTime >= back) -> nextTime - back >= 0

	if (de.mbLinked) {
		while((sint32)(de.mNextTime - t) < 0) {
			ce.push_back(de.mNextTime);
			de.mNextTime += de.mPeriod;

			if ((sint32)(de.mNextTime - de.mNextHiTime) >= 0) {
				de.mNextTime = de.mNextHiTime + de.mHiLoOffset;
				de.mNextHiTime += de.mHiPeriod;
			}
		}
	} else {
		while((sint32)(de.mNextTime - t) < 0) {
			ce.push_back(de.mNextTime);
			de.mNextTime += de.mPeriod;
		}
	}

	mDeferredEvents[channel] = de;
}

void ATPokeyRenderer::Flush(const uint32 t) {
	for(;;) {
		uint32 dt = t - mLastFlushTime;
		if (!dt)
			break;

		Flush2(mLastFlushTime + std::min<uint32>(dt, 0xC000));
	}

	GenerateSamples(t * 2);
}

void ATPokeyRenderer::Flush2(const uint32 t) {
	// flush deferred events
	bool haveAnyEvents = false;

	for(int i=0; i<4; ++i) {
		if (mDeferredEvents[i].mbEnabled)
			FlushDeferredEvents(i, t);

		if (!mChannelEdges[i].empty())
			haveAnyEvents = true;
	}

	// Check if the noise flip-flops are different from the output channel
	// mask for any channels that are not volume-only and have non-zero
	// volume; if there are any, update the mask and the current output
	// level. Any other channels that differ won't matter since their volume
	// is either being overridden or zero; we can update them later when
	// that changes.
	uint8 dirtyOutputs = (mChannelOutputMask ^ mNoiseFlipFlops) & ~mVolumeOnlyMask & mNonZeroVolumeMask;

	if (dirtyOutputs) {
		mChannelOutputMask ^= dirtyOutputs;
		UpdateOutput2(mLastOutputTime2);
	}

	const uint32 baseTime = mLastFlushTime;
	mLastFlushTime = t;

	// early out if we have no events to process
	if (!haveAnyEvents)
		return;

	// realign polynomial tables to start of frame
	if (mbInitMode) {
		mPolyState.mPoly4Offset =
			mPolyState.mPoly5Offset =
			mPolyState.mPoly9Offset =
			mPolyState.mPoly17Offset = (uintptr)mpTables->mInitModeBuffer;
	} else {
		mPolyState.UpdatePoly4Counter(baseTime);
		mPolyState.UpdatePoly5Counter(baseTime);
		mPolyState.UpdatePoly9Counter(baseTime);
		mPolyState.UpdatePoly17Counter(baseTime);

		mPolyState.mPoly4Offset  = (uintptr)mpTables->mPolyBuffer + mPolyState.mPoly4Counter;
		mPolyState.mPoly5Offset  = (uintptr)mpTables->mPolyBuffer + mPolyState.mPoly5Counter;
		mPolyState.mPoly9Offset  = (uintptr)mpTables->mPolyBuffer + mPolyState.mPoly9Counter;
		mPolyState.mPoly17Offset = (uintptr)mpTables->mPolyBuffer + mPolyState.mPoly17Counter;
	}

	for(int i=0; i<4; ++i) {
		auto& srcEdges = mChannelEdges[i];

		// We should not have any edges in the future. We may have some edges slightly in the
		// past since we keep a couple of cycles back to delay the audio output.
		if (!srcEdges.empty()) {
			VDASSERT(ATWrapTime{srcEdges.front()} >= baseTime - kAudioDelay);
			VDASSERT(ATWrapTime{srcEdges.back()} <= t);
		}

		auto& dstEdges = mSortedEdgesTemp[i];
		const uint32 numEdges = srcEdges.size();
		uint32 numAudioEdges = numEdges;
		
		while(numAudioEdges && ATWrapTime{srcEdges[numAudioEdges - 1]} >= t - kAudioDelay)
			--numAudioEdges;

		dstEdges.resize(numEdges + 1);

		uint32 *dst = dstEdges.data();
		uint32 *dst2 = (this->*GetFireTimerRoutine(i))(dst, srcEdges.data(), numAudioEdges, baseTime - kAudioDelay);

		*dst2++ = UINT32_C(0xFFFFFFFF);

		dstEdges.resize((size_t)(dst2 - dst));

		if (i >= 2) {
			// Merge in events to update the high-pass filter. If high-pass audio mode is enabled for
			// ch1/2, we need to insert all of the events from ch3/4; otherwise, we only need the last
			// event, if any.
			uint32 hpClockStart = 0;
			uint32 hpClockEnd = numEdges;

			while(hpClockStart != hpClockEnd && ATWrapTime{srcEdges[hpClockStart]} < baseTime)
				++hpClockStart;

			while(hpClockStart != hpClockEnd && ATWrapTime{srcEdges[hpClockEnd - 1]} >= t)
				--hpClockEnd;

			if (hpClockStart != hpClockEnd) {
				const bool hpEnabled = (mAUDCTL & (4 >> (i - 2))) != 0;
				auto& hpTargetEdges = mSortedEdgesTemp[i - 2];
				const uint32 hpUpdateCoding = 0x3F00 - (0x400 << i) + (4 << i);

				if (hpEnabled) {
					// high-pass is enabled -- offset events by audio delay, convert to HP update events, and merge
					// into ch1/2 list
					const uint32 numHpEvents = hpClockEnd - hpClockStart;

					mSortedEdgesHpTemp1.resize(numHpEvents + 1);

					for(uint32 i=0; i<numHpEvents; ++i) {
						const uint32 evTime = srcEdges[hpClockStart + i];

						// Add one half cycle to the high pass update so it's a half cycle earlier than
						// the output flip/flop. On real hardware, HP never updates at the same time;
						// it's either one half clock earlier or late, so there is no phase offset at
						// which high pass is fully effective or ineffective.
						mSortedEdgesHpTemp1[i] = ((evTime - baseTime) << 15) + (1 << 14) + hpUpdateCoding;
					}

					mSortedEdgesHpTemp1.back() = 0xFFFFFFFF;
					mSortedEdgesHpTemp2.resize(hpTargetEdges.size() + numHpEvents);

					MergeOutputEvents(hpTargetEdges.data(), mSortedEdgesHpTemp1.data(), mSortedEdgesHpTemp2.data());

					hpTargetEdges.swap(mSortedEdgesHpTemp2);
					hpTargetEdges.back() = 0xFFFFFFFF;
				}
			}
		}

		srcEdges.erase(srcEdges.begin(), srcEdges.begin() + numAudioEdges);
	}

	const uint32 n0 = (uint32)mSortedEdgesTemp[0].size() - 1;
	const uint32 n1 = (uint32)mSortedEdgesTemp[1].size() - 1;
	const uint32 n2 = (uint32)mSortedEdgesTemp[2].size() - 1;
	const uint32 n3 = (uint32)mSortedEdgesTemp[3].size() - 1;
	const uint32 n01 = n0 + n1;
	const uint32 n23 = n2 + n3;
	const uint32 n = n01 + n23;

	mSortedEdgesTemp2[0].resize(n01 + 1);
	mSortedEdgesTemp2[1].resize(n23 + 1);

	if (n01) {
		MergeOutputEvents(mSortedEdgesTemp[0].data(),
			mSortedEdgesTemp[1].data(),
			mSortedEdgesTemp2[0].data());
	}

	mSortedEdgesTemp2[0].back() = 0xFFFFFFFF;

	if (n23) {
		MergeOutputEvents(mSortedEdgesTemp[2].data(),
			mSortedEdgesTemp[3].data(),
			mSortedEdgesTemp2[1].data());
	}

	mSortedEdgesTemp2[1].back() = 0xFFFFFFFF;

	// The merge order here is critical -- we need channels 3 and 4 to update before
	// 1 and 2 in case high pass mode is enabled, because if 1+3 or 2+4 fire at the
	// same time, the high pass is updated with the output state from the last cycle.
	mSortedEdges.resize(n + 1);

	MergeOutputEvents(mSortedEdgesTemp2[1].data(),
		mSortedEdgesTemp2[0].data(),
		mSortedEdges.data());

	if (g_ATLCPokeyTEv.IsEnabled()) {
		static uint8 kBitLookup[16]={
			0,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3
		};

		for(const auto& edge : mSortedEdges) {
			g_ATLCPokeyTEv("%08X:%u\n", (edge >> 14) + baseTime, kBitLookup[(edge >> 8) & 15]);
		}
	}

	ProcessOutputEdges(baseTime, mSortedEdges.data(), n);
	mSortedEdges.clear();
}

void ATPokeyRenderer::MergeOutputEvents(const uint32 *VDRESTRICT src1, const uint32 *VDRESTRICT src2, uint32 *VDRESTRICT dst) {
	uint32 a = *src1++;
	uint32 b = *src2++;

	for(;;) {
		if (b < a) {
			*dst++ = b;
			b = *src2++;
			continue;
		}

		if (a < b) {
			*dst++ = a;
			a = *src1++;
			continue;
		}

		if (a == 0xFFFFFFFF)
			break;

		*dst++ = a;
		a = *src1++;
	}
}

template<int activeChannel, bool T_UsePoly9>
const ATPokeyRenderer::FireTimerRoutine ATPokeyRenderer::kFireRoutines[2][16]={
	// What we are trying to do here is minimize the amount of work done
	// in the FireTimer() routine, in two ways: precompile code paths with
	// specific functions enabled, and identify when the resultant signal
	// does not change.
	//
	// AUDCx bit 7 controls clock selection and isn't tied to anything else,
	//             so it always needs to go through.
	//
	// AUDCx bit 5 enables pure tone mode. When set, it overrides bit 6.
	//             Therefore, we map [6:5] = 11 to 01.
	//
	// AUDCx bit 6 chooses the 4-bit LFSR or the 9/17-bit LFSR. It is
	//             overridden in the table as noted above.
	//
	// AUDCx bit 4 controls volume only mode. When it is set, we must still
	//             update the internal flip-flop states, but the volume
	//             level is locked and can't affect the output.
	//
	// We also check the volume on the channel. If it is zero, then the
	// output also doesn't affect the volume and therefore we can skip the
	// flush in that case as well.
	//
	// High-pass mode throws a wrench into the works. In that case, a pair
	// of channels are tied together and we have to be careful about what
	// optimizations are applied. We need to check volumes on both channels,
	// and the high channel can cause signal changes if the low channel is
	// un-muted even if the high channel is muted.
	//
	// The control value $00 is especially important as it is the init state
	// used by the OS to silence the audio channels, and thus it should run
	// quickly. It is annoying to us since it is an LFSR-based mode rather
	// than volume level.

	{
		&ATPokeyRenderer::FireTimer<activeChannel, 0x00, false, T_UsePoly9>,	// poly5 + poly9/17
		&ATPokeyRenderer::FireTimer<activeChannel, 0x00, false, T_UsePoly9>,	// poly5 + poly9/17
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,			// poly5 + tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,			// poly5 + tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0x40, false, false>,			// poly5 + poly4
		&ATPokeyRenderer::FireTimer<activeChannel, 0x40, false, false>,			// poly5 + poly4
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,			// poly5 + tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,			// poly5 + tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0x80, false, T_UsePoly9>,	// poly9/17
		&ATPokeyRenderer::FireTimer<activeChannel, 0x80, false, T_UsePoly9>,	// poly9/17
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,			// tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,			// tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0xC0, false, false>,			// poly4
		&ATPokeyRenderer::FireTimer<activeChannel, 0xC0, false, false>,			// poly4
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,			// tone
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,			// tone
	},
	{
		&ATPokeyRenderer::FireTimer<activeChannel, 0x00, true , T_UsePoly9>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x00, false, T_UsePoly9>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x40, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x40, false, false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x20, false, false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x80, true , T_UsePoly9>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0x80, false, T_UsePoly9>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xC0, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xC0, false, false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, true , false>,
		&ATPokeyRenderer::FireTimer<activeChannel, 0xA0, false, false>,
	},
};

ATPokeyRenderer::FireTimerRoutine ATPokeyRenderer::GetFireTimerRoutine(int ch) const {
	switch(ch) {
		case 0: return GetFireTimerRoutine<0>();
		case 1: return GetFireTimerRoutine<1>();
		case 2: return GetFireTimerRoutine<2>();
		case 3: return GetFireTimerRoutine<3>();

		default:
			return nullptr;
	}
}

template<int activeChannel>
ATPokeyRenderer::FireTimerRoutine ATPokeyRenderer::GetFireTimerRoutine() const {
	// For ch1/2, if high pass is enabled we must generate output events even if volume
	// is zero so we can latch the noise flip/flop into the high-pass flip/flop.
	const bool highPassEnabled = (activeChannel == 0 && (mAUDCTL & 0x04)) || (activeChannel == 1 && (mAUDCTL & 0x02));
	const bool nonZeroVolume = mChannelVolume[activeChannel] || (highPassEnabled && mChannelVolume[activeChannel & 1]);

	using FireTimerTable = FireTimerRoutine[2][16];
	const FireTimerTable *tab[2] = {
		&kFireRoutines<activeChannel, false>,
		&kFireRoutines<activeChannel, true>,
	};
	const bool usePoly9 = (mAUDCTL & 0x80) != 0;

	return (*tab[usePoly9])[nonZeroVolume][mAUDC[activeChannel] >> 4];
}

template<int activeChannel, uint8 audcn, bool outputAffectsSignal, bool T_UsePoly9>
uint32 *ATPokeyRenderer::FireTimer(uint32 *VDRESTRICT dst, const uint32 *VDRESTRICT src, uint32 n, uint32 timeBase) {
	static constexpr bool noiseEnabled = !(audcn & 0x20);
	static constexpr bool poly5Enabled = !(audcn & 0x80);
	static constexpr int polyOffset = 3 - activeChannel;

	PolyState polyState = mPolyState;

	const uint8 channelBit = (1 << activeChannel);
	uint32 noiseFF = (mNoiseFlipFlops & channelBit) ? 1 : 0;

	const uint32 baseMaskCode = 0x3F00 - (0x100 << activeChannel);
	uint32 currentMaskCode = baseMaskCode + (noiseFF ? channelBit : 0);

	// These aren't pointers because they aren't guaranteed to be within the array until offset
	// by the time offset.
	const uintptr poly4Base = polyState.mPoly4Offset + polyOffset;
	const uintptr poly5Base = polyState.mPoly5Offset + polyOffset;
	const uintptr poly9Base = polyState.mPoly9Offset + polyOffset;
	const uintptr poly17Base = polyState.mPoly17Offset + polyOffset;

	const uint32 masks[2] = {
		baseMaskCode,
		baseMaskCode + channelBit
	};

	while(n--) {
		const uint32 timeOffset = (*src++) - timeBase;

		if constexpr (poly5Enabled) {
			uint8 poly5 = *(const uint8 *)(poly5Base + timeOffset);

			if (!(poly5 & 4))
				continue;
		}
		
		if constexpr (noiseEnabled) {
			uint32 noiseFFInput;

			if constexpr ((audcn & 0x40) != 0) {
				const uint32 poly4 = *(const uint8 *)(poly4Base + timeOffset);
				noiseFFInput = (poly4 & 8) >> 3;
			} else if constexpr (T_UsePoly9) {
				const uint32 poly9 = *(const uint8 *)(poly9Base + timeOffset);
				noiseFFInput = (poly9 & 2) >> 1;
			} else {
				const uint32 poly17 = *(const uint8 *)(poly17Base + timeOffset);
				noiseFFInput = (poly17 & 1);
			}

			if constexpr (outputAffectsSignal) {
				// Because we are using noise in this path, using a branch would be fairly expensive
				// as it is highly likely to mispredict due to the random branch -- since we're
				// literally driving it from a psuedorandom noise generator. Use a branchless
				// algorithm instead.

				const uint32 outputChanged = noiseFF ^ noiseFFInput;
				noiseFF = noiseFFInput;

				*dst = (timeOffset << 15) + masks[noiseFF];
				dst += outputChanged;
			} else {
				noiseFF = noiseFFInput;
			}
		} else {
			// Noise isn't enabled -- hardcode some stuff since VC++ isn't able to
			// deduce that toggling a bit will always cause the changed check to pass.
			noiseFF ^= 1;

			if constexpr (outputAffectsSignal) {
				currentMaskCode ^= channelBit;

				// Update normal audio on full cycles; we reserve the half-cycle for high-pass update.
				*dst++ = (timeOffset << 15) + currentMaskCode;
			}
		}
	}

	if (noiseFF)
		mNoiseFlipFlops |= channelBit;
	else
		mNoiseFlipFlops &= ~channelBit;

	return dst;
}

void ATPokeyRenderer::ProcessOutputEdges(uint32 timeBase, const uint32 *edges, uint32 n) {
	const uint8 v0 = mChannelVolume[0];
	const uint8 v1 = mChannelVolume[1];
	const uint8 v2 = mChannelVolume[2];
	const uint8 v3 = mChannelVolume[3];
	uint8 v[16];

	v[0] = 0;
	v[1] = v0;
	v[2] = v1;
	v[3] = v0 + v1;

	for(int i=0; i<4; ++i)
		v[i+4] = v[i] + v2;

	for(int i=0; i<8; ++i)
		v[i+8] = v[i] + v3;

	uint32 timeBase2 = timeBase * 2;
	uint8 outputMask = mChannelOutputMask;

	while(n--) {
		const uint32 code = *edges++;
		const uint32 t2 = timeBase2 + (code >> 14);

		// apply AND mask to clear the bits we're about to update
		outputMask &= (uint8)((code >> 8) & 0x3F); 
		outputMask += (uint8)(code & ((outputMask << 4) + 15));

		uint8 idx = outputMask ^ (outputMask >> 4);
		UpdateOutput2(t2, v[(idx & 15) | mVolumeOnlyMask]);
	}

	mChannelOutputMask = outputMask;
}

void ATPokeyRenderer::UpdateVolume(int index) {
	mChannelVolume[index] = mbChannelEnabled[index] ? mAUDC[index] & 15 : 0;

	if (mAUDC[index] & 0x10)
		mVolumeOnlyMask |= (1 << index);
	else
		mVolumeOnlyMask &= ~(1 << index);

	if (mAUDC[index] & 0x0F)
		mNonZeroVolumeMask |= (1 << index);
	else
		mNonZeroVolumeMask &= ~(1 << index);
}

void ATPokeyRenderer::UpdateOutput(uint32 t) {
	UpdateOutput2(t * 2);
}

void ATPokeyRenderer::UpdateOutput2(uint32 t2) {
	uint8 outputMask = (mChannelOutputMask ^ (mChannelOutputMask >> 4)) | mVolumeOnlyMask;

	int v0 = mChannelVolume[0];
	int v1 = mChannelVolume[1];
	int v2 = mChannelVolume[2];
	int v3 = mChannelVolume[3];
	int vpok	= ((outputMask & 0x01) ? v0 : 0)
				+ ((outputMask & 0x02) ? v1 : 0)
				+ ((outputMask & 0x04) ? v2 : 0)
				+ ((outputMask & 0x08) ? v3 : 0);

	UpdateOutput2(t2, vpok);
}

void ATPokeyRenderer::UpdateOutput2(uint32 t2, int vpok) {
	VDASSERT(t2 - mLastOutputTime2 < 0x80000000);

	GenerateSamples(t2);

	int oc = t2 - mLastOutputTime2;

	float delta = mOutputLevel - mHighPassAccum;
	mAccum += delta * mpTables->mHPIntegralTable[oc];
	mHighPassAccum += delta * mpTables->mHPTable[oc];

	mSpeakerAccum += mSpeakerLevel * ((float)oc * 0.5f);
	mLastOutputTime2 = t2;

	VDASSERT(t2 - mLastOutputSampleTime2 <= 56);

	mOutputLevel	= mpTables->mMixTable[vpok];

	// The XL/XE speaker is about as loud peak-to-peak as a channel at volume 6.
	// However, it is added in later in the output circuitry and has different
	// audio characteristics, so we must treat it separately.
	mSpeakerLevel	= (mbSpeakerState ? -mpTables->mMixTable[6] : 0.0f) + mExternalInput;
}

void ATPokeyRenderer::GenerateSamples(uint32 t2) {
	sint32 delta = t2 - mLastOutputSampleTime2;

	if (!delta)
		return;

	if (delta >= 56) {
		mLastOutputSampleTime2 += 56;

		int oc = mLastOutputSampleTime2 - mLastOutputTime2;
		VDASSERT((unsigned)oc <= 56);

		float delta = mOutputLevel - mHighPassAccum;
		mAccum += delta * mpTables->mHPIntegralTable[oc];
		mHighPassAccum += delta * mpTables->mHPTable[oc];

		mSpeakerAccum += mSpeakerLevel * ((float)oc * 0.5f);
		mLastOutputTime2 = mLastOutputSampleTime2;

		float v = mAccum + mSpeakerAccum;

		mAccum = 0;
		mSpeakerAccum = 0;
		mRawOutputBuffer[mOutputSampleCount] = v;

		if (++mOutputSampleCount >= kBufferSize) {
			mOutputSampleCount = kBufferSize - 1;

			while((t2 - mLastOutputSampleTime2) >= 56)
				mLastOutputSampleTime2 += 56;

			mLastOutputTime2 = mLastOutputSampleTime2;
			return;
		}
	}

	if ((t2 - mLastOutputSampleTime2) < 56)
		return;

//	const float v1 = mOutputLevel * 56;
	const float coeff1 = mpTables->mHPIntegralTable[56];
	const float coeff2 = mpTables->mHPTable[56];
	const float v2 = mSpeakerLevel * (56 * 0.5f);
	mAccum = 0;
	mSpeakerAccum = 0;

	while((t2 - mLastOutputSampleTime2) >= 56) {
		mLastOutputSampleTime2 += 56;

		float delta = mOutputLevel - mHighPassAccum;
		mRawOutputBuffer[mOutputSampleCount] = delta * coeff1 + v2;
		mHighPassAccum += delta * coeff2;

		if (++mOutputSampleCount >= kBufferSize) {
			mOutputSampleCount = kBufferSize - 1;

			VDASSERT(t2 - mLastOutputSampleTime2 < 56000000);

			while((t2 - mLastOutputSampleTime2) >= 56)
				mLastOutputSampleTime2 += 56;
			break;
		}
	}

	mLastOutputTime2 = mLastOutputSampleTime2;

	VDASSERT(t2 - mLastOutputSampleTime2 <= 56);
}

void ATPokeyRenderer::PolyState::UpdatePoly17Counter(uint32 t) {
	int polyDelta = t - mLastPoly17Time;
	mPoly17Counter += polyDelta & mInitMask;
	mLastPoly17Time = t;

	if (mPoly17Counter >= 131071)
		mPoly17Counter %= 131071;
}

void ATPokeyRenderer::PolyState::UpdatePoly9Counter(uint32 t) {
	int polyDelta = t - mLastPoly9Time;
	mPoly9Counter += polyDelta & mInitMask;
	mLastPoly9Time = t;

	if (mPoly9Counter >= 511)
		mPoly9Counter %= 511;
}

void ATPokeyRenderer::PolyState::UpdatePoly5Counter(uint32 t) {
	int polyDelta = t - mLastPoly5Time;
	mPoly5Counter += polyDelta & mInitMask;
	mLastPoly5Time = t;

	if (mPoly5Counter >= 31)
		mPoly5Counter %= 31;
}

void ATPokeyRenderer::PolyState::UpdatePoly4Counter(uint32 t) {
	int polyDelta = t - mLastPoly4Time;
	VDASSERT(polyDelta >= 0);
	mPoly4Counter += polyDelta & mInitMask;
	mLastPoly4Time = t;

	if (mPoly4Counter >= 15)
		mPoly4Counter %= 15;
}
