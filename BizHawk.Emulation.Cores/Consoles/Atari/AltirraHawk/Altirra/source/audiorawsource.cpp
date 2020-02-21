//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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
#include <at/atcore/wraptime.h>
#include "audiooutput.h"
#include "audiorawsource.h"

ATAudioRawSource::ATAudioRawSource() {
}

ATAudioRawSource::~ATAudioRawSource() {
	Shutdown();
}

void ATAudioRawSource::Init(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;

	mixer->AddSyncAudioSource(this);
}

void ATAudioRawSource::Shutdown() {
	if (mpAudioMixer) {
		mpAudioMixer->RemoveSyncAudioSource(this);
		mpAudioMixer = nullptr;
	}
}

void ATAudioRawSource::SetOutput(uint32 t, float level) {
	if (level == mLevel)
		return;

	mLevel = level;

	if (!mLevelEdges.empty() && ATWrapTime{mLevelEdges.back().mTime} >= t)
		mLevelEdges.back().mLevel = level;
	else
		mLevelEdges.push_back({ t, level });
}

bool ATAudioRawSource::RequiresStereoMixingNow() const {
	return false;
}

void ATAudioRawSource::WriteAudio(const ATSyncAudioMixInfo& mixInfo) {
	const uint32 baseTime = mixInfo.mStartTime;
	const uint32 timeSpan = mixInfo.mCount * 28;
	const uint32 numSamples = mixInfo.mCount;

	// drop samples that are before the start time, updating the base
	// level
	while(!mLevelEdges.empty() && ATWrapTime{mLevelEdges.front().mTime} <= baseTime) {
		mStartLevel = mLevelEdges.front().mLevel;
		mLevelEdges.pop_front();
	}

	// check if we have any remaining edges within the time span
	if (mLevelEdges.empty() || (uint32)(mLevelEdges.front().mTime - baseTime) >= timeSpan) {
		// no -- do DC mixing
		*mixInfo.mpDCLeft += mStartLevel;
		*mixInfo.mpDCRight += mStartLevel;
	} else {
		// yes -- do full mixing
		float level = mStartLevel;
		float *dstL = mixInfo.mpLeft;
		float *dstR = mixInfo.mpRight;

		float *end = dstL + numSamples;
		uint32 relt = 0;
		uint32 offset = 0;
		float accum = 0;

		constexpr float kInvSubTicks = 1.0f / 28.0f;

		while(relt < timeSpan) {
			uint32 timeNext = timeSpan;
			float spanLevel = level;

			if (!mLevelEdges.empty()) {
				uint32 timeNext2 = mLevelEdges.front().mTime - baseTime;

				if (timeNext > timeNext2)
					timeNext = timeNext2;

				level = mLevelEdges.front().mLevel;
				mLevelEdges.pop_front();
			}

			if (relt >= timeNext)
				continue;

			uint32 subTicks = timeNext - relt;
			relt = timeNext;

			if (offset) {
				uint32 startSubTicks = 28 - offset;

				if (startSubTicks > subTicks)
					startSubTicks = subTicks;

				subTicks -= startSubTicks;
				accum += (float)startSubTicks * spanLevel;
				offset += startSubTicks;

				if (offset >= 28) {
					offset = 0;

					accum *= kInvSubTicks;
					(*dstL++) += accum;

					if (dstR)
						(*dstR++) += accum;

					accum = 0;
				}
			}

			if (subTicks) {
				if (dstR) {
					while(subTicks >= 28) {
						subTicks -= 28;

						(*dstL++) += spanLevel;
						(*dstR++) += spanLevel;
					}
				} else {
					while(subTicks >= 28) {
						subTicks -= 28;

						(*dstL++) += spanLevel;
					}
				}

				accum = (float)subTicks * spanLevel;
				offset = subTicks;
			}
		}

		mStartLevel = level;

		VDASSERT(dstL == end);
	}
}
