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
#include "audiosampleplayer.h"
#include "oshelper.h"
#include "resource.h"

struct ATAudioSamplePlayer::SoundPred {
	SoundPred(uint32 t) : mTimeBase(t + 0x80000000U) {}

	bool operator()(const Sound& x, const Sound& y) const {
		return (x.mStartTime - mTimeBase) < (y.mStartTime - mTimeBase);
	}

	uint32 mTimeBase;
};

ATAudioSamplePlayer::ATAudioSamplePlayer()
	: mNextSoundId(1)
{
}

ATAudioSamplePlayer::~ATAudioSamplePlayer() {
}

void ATAudioSamplePlayer::Init(ATScheduler *sch) {
	mpScheduler = sch;

	static const uint32 kResIds[]={
		IDR_DISK_SPIN,
		IDR_TRACK_STEP,
		IDR_TRACK_STEP_2,
		IDR_TRACK_STEP_2,
		IDR_TRACK_STEP_3
	};

	static const float kBaseVolumes[]={
		0.05f,
		0.4f,
		0.8f,
		0.8f,
		0.4f
	};

	static_assert(vdcountof(kResIds) == vdcountof(mSamples), "Sample array mismatch");
	static_assert(vdcountof(kBaseVolumes) == vdcountof(mSamples), "Sample array mismatch");

	vdfastvector<uint8> data;
	for(size_t i=0; i<vdcountof(kResIds); ++i) {
		if (i + 1 == kATAudioSampleId_DiskStep2H) {
			// special case
			auto samp = mSamples[kATAudioSampleId_DiskStep2 - 1];
			samp.mLength >>= 1;
			mSamples[i] = samp;
		} else {
			ATLoadMiscResource(kResIds[i], data);

			const size_t len = data.size() * sizeof(data[0]);
			sint16 *p = (sint16 *)mAllocator.Allocate(len);
			memcpy(p, data.data(), len);

			mSamples[i] = { p, (uint32)(len / sizeof(sint16)), kBaseVolumes[i] };
		}
	}
}

void ATAudioSamplePlayer::Shutdown() {
	for(ATAudioSampleDesc& desc : mSamples)
		desc = {};

	for(Sound *s : mSounds) {
		FreeSound(s);
	}

	mSounds.clear();
	mFreeSounds.clear();
	mAllocator.Clear();

	mpScheduler = NULL;
}

ATSoundId ATAudioSamplePlayer::AddSound(ATAudioMix mix, uint32 delay, ATAudioSampleId sampleId, float volume) {
	const uint32 index = (uint32)((uint32)sampleId - 1);
	
	if (index >= vdcountof(mSamples))
		return ATSoundId::Invalid;

	const auto& sample = mSamples[index];
	return AddSound(mix, delay, sample.mpData, sample.mLength, sample.mBaseVolume * volume);
}

ATSoundId ATAudioSamplePlayer::AddLoopingSound(ATAudioMix mix, uint32 delay, ATAudioSampleId sampleId, float volume) {
	const uint32 index = (uint32)((uint32)sampleId - 1);
	
	if (index >= vdcountof(mSamples))
		return ATSoundId::Invalid;

	const auto& sample = mSamples[index];
	return AddLoopingSound(mix, delay, sample.mpData, sample.mLength, sample.mBaseVolume * volume);
}

ATSoundId ATAudioSamplePlayer::AddSound(ATAudioMix mix, uint32 delay, const sint16 *sample, uint32 len, float volume) {
	const uint64 t = mpScheduler->GetTick64() + delay;

	if (mFreeSounds.empty())
		mFreeSounds.push_back(mAllocator.Allocate<Sound>());

	Sound *s = mFreeSounds.back();
	mFreeSounds.pop_back();
	
	s->mId = (ATSoundId)mNextSoundId;
	mNextSoundId += 2;

	s->mStartTime = t;
	s->mLoopPeriod = 0;
	s->mEndTime = t + kATCyclesPerSyncSample * len;
	s->mLength = len;
	s->mVolume = volume * (60.0f * 28.0f / 32767.0f);
	s->mMix = mix;
	s->mpSample = sample;
	s->mbEndValid = true;

	mSounds.push_back(s);
	return s->mId;
}

ATSoundId ATAudioSamplePlayer::AddLoopingSound(ATAudioMix mix, uint32 delay, const sint16 *sample, uint32 len, float volume) {
	const uint64 t = mpScheduler->GetTick64() + delay;

	if (mFreeSounds.empty())
		mFreeSounds.push_back(mAllocator.Allocate<Sound>());

	Sound *s = mFreeSounds.back();
	mFreeSounds.pop_back();

	s->mId = (ATSoundId)mNextSoundId;
	mNextSoundId += 2;

	s->mStartTime = t;
	s->mLoopPeriod = len;
	s->mEndTime = 0;
	s->mLength = len;
	s->mVolume = volume * (60.0f * 28.0f / 32767.0f);
	s->mMix = mix;
	s->mpSample = sample;
	s->mbEndValid = false;

	mSounds.push_back(s);
	return s->mId;
}

ATSoundId ATAudioSamplePlayer::AddSound(ATAudioMix mix, uint32 delay, IATAudioSampleSource *src, IVDRefCount *owner, uint32 len, float volume) {
	const uint64 t = mpScheduler->GetTick64() + delay;

	if (mFreeSounds.empty())
		mFreeSounds.push_back(mAllocator.Allocate<Sound>());

	Sound *s = mFreeSounds.back();
	mFreeSounds.pop_back();
	
	s->mId = (ATSoundId)mNextSoundId;
	mNextSoundId += 2;

	s->mStartTime = t;
	s->mLoopPeriod = 0;
	s->mEndTime = t + kATCyclesPerSyncSample * len;
	s->mLength = len;
	s->mVolume = volume;
	s->mMix = mix;
	s->mpSample = nullptr;
	s->mpSource = src;

	if (owner)
		owner->AddRef();

	s->mpOwner = owner;

	s->mbEndValid = true;

	mSounds.push_back(s);
	return s->mId;
}

ATSoundId ATAudioSamplePlayer::AddLoopingSound(ATAudioMix mix, uint32 delay, IATAudioSampleSource *src, IVDRefCount *owner, float volume) {
	const uint64 t = mpScheduler->GetTick64() + delay;

	if (mFreeSounds.empty())
		mFreeSounds.push_back(mAllocator.Allocate<Sound>());

	Sound *s = mFreeSounds.back();
	mFreeSounds.pop_back();

	s->mId = (ATSoundId)mNextSoundId;
	mNextSoundId += 2;

	s->mStartTime = t;
	s->mLoopPeriod = 0;
	s->mEndTime = t;
	s->mLength = 0;
	s->mVolume = volume;
	s->mMix = mix;
	s->mpSample = nullptr;
	s->mpSource = src;

	if (owner)
		owner->AddRef();

	s->mpOwner = owner;

	s->mbEndValid = false;

	mSounds.push_back(s);
	return s->mId;
}

void ATAudioSamplePlayer::ForceStopSound(ATSoundId id) {
	auto it = mSounds.begin(), itEnd = mSounds.end();

	for(; it != itEnd; ++it) {
		Sound *s = *it;
		if (s->mId != id)
			continue;

		*it = mSounds.back();
		mSounds.pop_back();

		FreeSound(s);
		return;
	}
}

void ATAudioSamplePlayer::StopSound(ATSoundId id) {
	StopSound(id, mpScheduler->GetTick64());
}

void ATAudioSamplePlayer::StopSound(ATSoundId id, uint64 time) {
	auto it = mSounds.begin(), itEnd = mSounds.end();

	for(; it != itEnd; ++it) {
		Sound *s = *it;

		if (s->mId != id)
			continue;

		// check if we're killing the sound before it starts
		if (time <= s->mStartTime) {
			*it = mSounds.back();
			mSounds.pop_back();

			FreeSound(s);
			return;
		}

		// check if we're trying to kill a one-shot after it would already end
		if (s->mbEndValid && time >= s->mEndTime)
			return;

		// mark new end time and exit
		s->mEndTime = time;
		s->mbEndValid = true;
		return;
	}
}

void ATAudioSamplePlayer::WriteAudio(const ATSyncAudioMixInfo& mixInfo) {
	// we are a mono source and should not be asked to mix stereo
	VDASSERT(!mixInfo.mpRight);

	uint64 startTime = mixInfo.mStartTime;
	float *dst = mixInfo.mpLeft;
	uint32 n = mixInfo.mCount;

	const uint64 endTime = startTime + (uint64)n * (uint32)kATCyclesPerSyncSample;

	// process one-shot sounds
	auto it = mSounds.begin(), itEnd = mSounds.end();
	while(it != itEnd) {
		Sound *s = *it;

		// drop sounds that we've already passed
		if (s->mEndTime <= startTime && s->mbEndValid) {
			// end time has already passed -- free the sound
			*it = mSounds.back();
			mSounds.pop_back();
			itEnd = mSounds.end();
			
			FreeSound(s);
			continue;
		}
		
		++it;

		// skip if sound hasn't happened yet
		if (s->mStartTime >= endTime)
			continue;

		// check for the sample starting behind the current window
		uint32 len = s->mLoopPeriod ? n + s->mLoopPeriod : s->mLength;
		uint32 dstOffset = 0;
		uint32 srcOffset = 0;
		
		if (s->mStartTime < startTime) {
			// count the number of samples we need to skip between the sound start and the
			// window start
			uint64 skippedSamples = (startTime - s->mStartTime) / (uint32)kATCyclesPerSyncSample;

			if (skippedSamples) {
				// if looping is enabled, wrap offset within loop
				if (s->mLoopPeriod)
					skippedSamples %= s->mLoopPeriod;

				// if we skipped everything, skip the sound -- this can happen if it only has
				// a fractional sample left
				if (len && skippedSamples >= len)
					continue;

				const uint32 shift = (uint32)skippedSamples;
				srcOffset = shift;
				len -= shift;

				VDASSERT(s->mLoopPeriod || !s->mLength || srcOffset <= s->mLength);
			}
		} else {
			// sound starts within window -- set destination offset
			dstOffset = (s->mStartTime - startTime) / (uint32)kATCyclesPerSyncSample;
		}

		// convert infinite sample length at this point
		if (!len)
			len = n;

		// check if the sound will be truncated due to ending before the window -- note that
		// this may not match the end of a one-shot if the sound has been stopped
		uint32 mixEnd = n;

		if (s->mbEndValid && s->mEndTime < endTime) {
			mixEnd = (s->mEndTime - startTime) / (sint32)kATCyclesPerSyncSample;

			if (mixEnd <= dstOffset) {
				VDASSERT(mixEnd >= dstOffset);
				continue;
			}
		}

		// clip sound to end of current mixing window
		if (len > mixEnd - dstOffset)
			len = mixEnd - dstOffset;

		if (!len)
			continue;

		// mix samples
		const float vol = s->mVolume * mixInfo.mpMixLevels[s->mMix];
		float *VDRESTRICT dst2 = dst + dstOffset;

		if (s->mpSource) {
			// mix source
			s->mpSource->MixAudio(dst2, len, vol, srcOffset, mixInfo.mMixingRate);
		} else {
			// direct sample -- mix it ourselves
			const sint16 *VDRESTRICT src = s->mpSample + srcOffset;
			const uint32 loopPeriod = s->mLoopPeriod;

			while(len) {
				// compute the length of the block to mix
				uint32 blockLen = len;

				// if looping is enabled, make sure this block does not
				// cross a loop boundary
				if (loopPeriod && blockLen > loopPeriod - srcOffset)
					blockLen = loopPeriod - srcOffset;

				len -= blockLen;

				// mix this block 
				while(blockLen--) {
					const float sample = (float)*src++ * vol;
					*dst2++ += sample;
				}

				src = s->mpSample;
				srcOffset = 0;
			}
		}
	}
}

void ATAudioSamplePlayer::FreeSound(Sound *s) {
	s->mpSample = nullptr;
	s->mpSource = nullptr;

	if (s->mpOwner) {
		s->mpOwner->Release();
		s->mpOwner = nullptr;
	}

	mFreeSounds.push_back(s);
}
