//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2010 Avery Lee
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
#include <vd2/system/math.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/time.h>
#include <vd2/Riza/audioout.h>
#include <at/atcore/audiosource.h>
#include <at/atio/wav.h>
#include "audiofilters.h"
#include "audiooutput.h"
#include "uirender.h"

class ATAudioOutput final : public IATAudioOutput, public IATAudioMixer, public VDAlignedObject<16> {
	ATAudioOutput(ATAudioOutput&) = delete;
	ATAudioOutput& operator=(const ATAudioOutput&) = delete;

public:
	ATAudioOutput();
	virtual ~ATAudioOutput() override;

	void Init(IATSyncAudioSamplePlayer *samplePlayer) override;

	ATAudioApi GetApi() override;
	void SetApi(ATAudioApi api) override;

	void SetAudioTap(IATAudioTap *tap) override;

	IATUIRenderer *GetStatusRenderer() override { return mpUIRenderer; }
	void SetStatusRenderer(IATUIRenderer *uir) override;

	IATAudioMixer& AsMixer() { return *this; }

	void AddSyncAudioSource(IATSyncAudioSource *src) override;
	void RemoveSyncAudioSource(IATSyncAudioSource *src) override;

	void SetCyclesPerSecond(double cps, double repeatfactor) override;

	bool GetMute() override;
	void SetMute(bool mute) override;

	float GetVolume() override;
	void SetVolume(float vol) override;

	float GetMixLevel(ATAudioMix mix) const override;
	void SetMixLevel(ATAudioMix mix, float level) override;

	int GetLatency() override;
	void SetLatency(int ms) override;

	int GetExtraBuffer() override;
	void SetExtraBuffer(int ms) override;

	void SetFiltersEnabled(bool enable) override {
		mFilters[0].SetActiveMode(enable);
		mFilters[1].SetActiveMode(enable);
	}

	void Pause() override;
	void Resume() override;

	void WriteAudio(
		const float *left,
		const float *right,
		uint32 count, bool pushAudio, uint64 timestamp) override;

public:
	IATSyncAudioSamplePlayer& GetSamplePlayer() override { return *mpSamplePlayer; }

protected:
	void InternalWriteAudio(const float *left, const float *right, uint32 count, bool pushAudio, uint64 timestamp);
	void RecomputeBuffering();
	void RecomputeResamplingRate();
	void ReinitAudio();
	bool ReinitAudio(ATAudioApi api);

	enum {
		// 1271 samples is the max (35568 cycles/frame / 28 cycles/sample + 1). We add a little bit here
		// to round it out. We need a 16 sample holdover in order to run the FIR filter.
		//
		// This should match the write size used by POKEY. However, if it doesn't, we just split the
		// write, so it'll still work.
		kBufferSize = 1536,

		// The filter needs to keep far enough ahead that there are enough samples to cover the
		// resampler plus the resampler step. The resampler itself needs 7 samples; we add another 9
		// samples to support about an 8:1 ratio.
		kFilterOffset = 16,

		// The prefilter needs to run ahead by the FIR kernel width (nominally 16 + 1 samples).
		kPreFilterOffset = kFilterOffset + ATAudioFilter::kFilterOverlap * 2,

		kSourceBufferSize = (kBufferSize + kPreFilterOffset + 15) & ~15,
	};

	uint32	mBufferLevel = 0;
	uint32	mFilteredSampleCount = 0;
	uint64	mResampleAccum = 0;
	sint64	mResampleRate = 0;
	float	mMixingRate = 0;
	uint32	mSamplingRate = 48000;
	ATAudioApi	mSelectedApi = kATAudioApi_WaveOut;
	ATAudioApi	mActiveApi = kATAudioApi_WaveOut;
	uint32	mPauseCount = 0;
	uint32	mLatencyTargetMin = 0;
	uint32	mLatencyTargetMax = 0;
	int		mLatency = 100;
	int		mExtraBuffer = 100;
	bool	mbMute = false;

	bool	mbFilterStereo = false;
	uint32	mFilterMonoSamples = 0;

	uint32	mRepeatAccum = 0;
	uint32	mRepeatInc = 0;

	uint32	mCheckCounter = 0;
	uint32	mMinLevel = 0;
	uint32	mMaxLevel = 0;
	uint32	mUnderflowCount = 0;
	uint32	mOverflowCount = 0;
	uint32	mDropCounter = 0;

	uint32	mWritePosition = 0;

	uint32	mProfileCounter = 0;
	uint32	mProfileBlockStartPos = 0;
	uint64	mProfileBlockStartTime = 0;

	vdautoptr<IVDAudioOutput>	mpAudioOut;
	IATAudioTap *mpAudioTap = nullptr;
	IATUIRenderer *mpUIRenderer = nullptr;
	IATSyncAudioSamplePlayer *mpSamplePlayer = nullptr;

	ATUIAudioStatus	mAudioStatus = {};

	ATAudioFilter	mFilters[2];

	typedef vdfastvector<IATSyncAudioSource *> SyncAudioSources;
	SyncAudioSources mSyncAudioSources;
	
	float mMixLevels[kATAudioMixCount];

	alignas(16) float	mSourceBuffer[2][kBufferSize] {};
	alignas(16) float	mMonoMixBuffer[kBufferSize] {};

	vdblock<sint16> mOutputBuffer16;
};

ATAudioOutput::ATAudioOutput() {
	mMixLevels[kATAudioMix_Drive] = 0.8f;
	mMixLevels[kATAudioMix_Covox] = 1.0f;

	// Starting with Modem we supply the scaling factor in the mix level.
	mMixLevels[kATAudioMix_Modem] = 1680.0f * 0.7f;
}

ATAudioOutput::~ATAudioOutput() {
}

void ATAudioOutput::Init(IATSyncAudioSamplePlayer *samplePlayer) {
	memset(mSourceBuffer, 0, sizeof mSourceBuffer);

	mpSamplePlayer = samplePlayer;

	mbFilterStereo = false;
	mFilterMonoSamples = 0;

	mBufferLevel = 0;
	mResampleAccum = 0;

	mCheckCounter = 0;
	mMinLevel = 0xFFFFFFFFU;
	mMaxLevel = 0;
	mUnderflowCount = 0;
	mOverflowCount = 0;
	mDropCounter = 0;

	mWritePosition = 0;

	mProfileBlockStartPos = 0;
	mProfileBlockStartTime = VDGetPreciseTick();
	mProfileCounter = 0;

	mLatencyTargetMin = (mSamplingRate * 10 / 1000) * 4;
	mLatencyTargetMax = (mSamplingRate * 100 / 1000) * 4;

	RecomputeBuffering();
	ReinitAudio();

	SetCyclesPerSecond(1789772.5, 1.0);
}

ATAudioApi ATAudioOutput::GetApi() {
	return mSelectedApi;
}

void ATAudioOutput::SetApi(ATAudioApi api) {
	if (mSelectedApi == api)
		return;

	mSelectedApi = api;
	ReinitAudio();
}

void ATAudioOutput::SetAudioTap(IATAudioTap *tap) {
	mpAudioTap = tap;
}

void ATAudioOutput::SetStatusRenderer(IATUIRenderer *uir) {
	if (mpUIRenderer != uir) {
		if (mpUIRenderer)
			mpUIRenderer->SetAudioStatus(NULL);

		mpUIRenderer = uir;
	}
}

void ATAudioOutput::AddSyncAudioSource(IATSyncAudioSource *src) {
	mSyncAudioSources.push_back(src);
}

void ATAudioOutput::RemoveSyncAudioSource(IATSyncAudioSource *src) {
	SyncAudioSources::iterator it(std::find(mSyncAudioSources.begin(), mSyncAudioSources.end(), src));

	if (it != mSyncAudioSources.end())
		mSyncAudioSources.erase(it);
}

void ATAudioOutput::SetCyclesPerSecond(double cps, double repeatfactor) {
	mMixingRate = cps / 28.0;
	mAudioStatus.mExpectedRate = cps / 28.0;
	RecomputeResamplingRate();

	mRepeatInc = VDRoundToInt(repeatfactor * 65536.0);
}

bool ATAudioOutput::GetMute() {
	return mbMute;
}

void ATAudioOutput::SetMute(bool mute) {
	mbMute = mute;
}

float ATAudioOutput::GetVolume() {
	return mFilters[0].GetScale();
}

void ATAudioOutput::SetVolume(float vol) {
	mFilters[0].SetScale(vol);
	mFilters[1].SetScale(vol);
}

float ATAudioOutput::GetMixLevel(ATAudioMix mix) const {
	return mMixLevels[mix];
}

void ATAudioOutput::SetMixLevel(ATAudioMix mix, float level) {
	mMixLevels[mix] = level;
}

int ATAudioOutput::GetLatency() {
	return mLatency;
}

void ATAudioOutput::SetLatency(int ms) {
	if (ms < 10)
		ms = 10;
	else if (ms > 500)
		ms = 500;

	if (mLatency == ms)
		return;

	mLatency = ms;

	RecomputeBuffering();
}

int ATAudioOutput::GetExtraBuffer() {
	return mExtraBuffer;
}

void ATAudioOutput::SetExtraBuffer(int ms) {
	if (ms < 10)
		ms = 10;
	else if (ms > 500)
		ms = 500;

	if (mExtraBuffer == ms)
		return;

	mExtraBuffer = ms;

	RecomputeBuffering();
}

void ATAudioOutput::Pause() {
	if (!mPauseCount++)
		mpAudioOut->Stop();
}

void ATAudioOutput::Resume() {
	if (!--mPauseCount)
		mpAudioOut->Start();
}

void ATAudioOutput::WriteAudio(
	const float *left,
	const float *right,
	uint32 count,
	bool pushAudio,
	uint64 timestamp)
{
	if (!count)
		return;

	mWritePosition += count;

	for(;;) {
		uint32 tc = kBufferSize - mBufferLevel;
		if (tc > count)
			tc = count;

		InternalWriteAudio(left, right, tc, pushAudio, timestamp);

		// exit if we can't write anything -- we only do this after a call to
		// InternalWriteAudio() as we need to try to push existing buffered
		// audio to clear buffer space
		if (!tc)
			break;

		count -= tc;
		if (!count)
			break;

		timestamp += 28 * tc;
		left += tc;
		if (right)
			right += tc;
	}
}

void ATAudioOutput::InternalWriteAudio(
	const float *left,
	const float *right,
	uint32 count,
	bool pushAudio,
	uint64 timestamp)
{
	VDASSERT(count > 0);
	VDASSERT(mBufferLevel + count <= kBufferSize);

	// check if any sync sources need stereo mixing
	bool needMono = false;
	bool needStereo = right != nullptr;

	for(IATSyncAudioSource *src : mSyncAudioSources) {
		if (src->RequiresStereoMixingNow()) {
			needStereo = true;
		} else {
			needMono = true;
		}
	}

	// if we need stereo and aren't currently doing stereo filtering, copy channel state over now
	if (needStereo && !mbFilterStereo) {
		mFilters[1].CopyState(mFilters[0]);
		memcpy(mSourceBuffer[1], mSourceBuffer[0], sizeof(float) * mBufferLevel);
		mbFilterStereo = true;
	}

	if (count) {
		// copy in samples
		float *const dstLeft = &mSourceBuffer[0][mBufferLevel];
		float *const dstRight = mbFilterStereo ? &mSourceBuffer[1][mBufferLevel] : nullptr;

		memcpy(dstLeft + kPreFilterOffset, left, sizeof(float) * count);

		if (mbFilterStereo) {
			if (right)
				memcpy(dstRight + kPreFilterOffset, right, sizeof(float) * count);
			else
				memcpy(dstRight + kPreFilterOffset, left, sizeof(float) * count);
		}


		// run audio sources
		float dcLevels[2] = { 0, 0 };

		ATSyncAudioMixInfo mixInfo {};
		mixInfo.mStartTime = timestamp;
		mixInfo.mCount = count;
		mixInfo.mMixingRate = mMixingRate;
		mixInfo.mpDCLeft = &dcLevels[0];
		mixInfo.mpDCRight = &dcLevels[1];
		mixInfo.mpMixLevels = mMixLevels;

		if (mbFilterStereo) {		// mixed mono/stereo mixing
			// mix mono first
			if (needMono) {
				// clear mono buffer
				memset(mMonoMixBuffer, 0, sizeof(float) * count);

				// mix mono sources into mono buffer
				mixInfo.mpLeft = mMonoMixBuffer;
				mixInfo.mpRight = nullptr;

				for(IATSyncAudioSource *src : mSyncAudioSources) {
					if (!src->RequiresStereoMixingNow())
						src->WriteAudio(mixInfo);
				}

				// mix mono buffer into stereo buffers
				for(uint32 i=0; i<count; ++i) {
					float v = mMonoMixBuffer[i];

					dstLeft[kPreFilterOffset + i] += v;
					dstRight[kPreFilterOffset + i] += v;
				}

				dcLevels[1] = dcLevels[0];
			}

			// mix stereo sources
			mixInfo.mpLeft = dstLeft + kPreFilterOffset;
			mixInfo.mpRight = dstRight + kPreFilterOffset;

			for(IATSyncAudioSource *src : mSyncAudioSources) {
				if (src->RequiresStereoMixingNow())
					src->WriteAudio(mixInfo);
			}
		} else {					// mono mixing
			mixInfo.mpLeft = dstLeft + kPreFilterOffset;
			mixInfo.mpRight = nullptr;

			for(IATSyncAudioSource *src : mSyncAudioSources)
				src->WriteAudio(mixInfo);
		}

		// filter channels
		for(int ch=0; ch<(mbFilterStereo ? 2 : 1); ++ch) {
			mFilters[ch].PreFilter(&mSourceBuffer[ch][mBufferLevel + kPreFilterOffset], count, dcLevels[ch]);
			mFilters[ch].Filter(&mSourceBuffer[ch][mBufferLevel + kFilterOffset], count);
		}
	}

	// if we're filtering stereo and getting mono, check if it's safe to switch over
	if (mbFilterStereo && !needStereo && mFilters[0].CloseTo(mFilters[1], 1e-10f)) {
		mFilterMonoSamples += count;

		if (mFilterMonoSamples >= kBufferSize)
			mbFilterStereo = false;
	} else {
		mFilterMonoSamples = 0;
	}

	// Send filtered samples to the audio tap.
	if (mpAudioTap) {
		if (mbFilterStereo)
			mpAudioTap->WriteRawAudio(mSourceBuffer[0] + mBufferLevel + kFilterOffset, mSourceBuffer[1] + mBufferLevel + kFilterOffset, count, timestamp);
		else
			mpAudioTap->WriteRawAudio(mSourceBuffer[0] + mBufferLevel + kFilterOffset, NULL, count, timestamp);
	}

	mBufferLevel += count;
	VDASSERT(mBufferLevel <= kBufferSize);

	// check for a change in output mixing rate that requires us to change our sampling rate
	const uint32 outputMixingRate = mpAudioOut->GetMixingRate();
	if (mSamplingRate != outputMixingRate) {
		mSamplingRate = outputMixingRate;

		RecomputeResamplingRate();
		RecomputeBuffering();
	}

	// Determine how many samples we can produce via resampling.
	uint32 resampleAvail = mBufferLevel + kFilterOffset;
	uint32 resampleCount = 0;

	uint64 limit = ((uint64)(resampleAvail - 8) << 32) + 0xFFFFFFFFU;

	if (limit >= mResampleAccum) {
		resampleCount = (uint32)((limit - mResampleAccum) / mResampleRate + 1);

		if (resampleCount) {
			if (mOutputBuffer16.size() < resampleCount * 2)
				mOutputBuffer16.resize((resampleCount * 2 + 2047) & ~2047);

			if (mbMute) {
				mResampleAccum += mResampleRate * resampleCount;
				memset(mOutputBuffer16.data(), 0, sizeof(mOutputBuffer16[0]) * resampleCount * 2);
			} else if (mbFilterStereo)
				mResampleAccum = ATFilterResampleStereo16(mOutputBuffer16.data(), mSourceBuffer[0], mSourceBuffer[1], resampleCount, mResampleAccum, mResampleRate);
			else
				mResampleAccum = ATFilterResampleMonoToStereo16(mOutputBuffer16.data(), mSourceBuffer[0], resampleCount, mResampleAccum, mResampleRate);

			// determine if we can now shift down the source buffer
			uint32 shift = (uint32)(mResampleAccum >> 32);

			if (shift > mBufferLevel)
				shift = mBufferLevel; 

			if (shift) {
				uint32 bytesToShift = sizeof(float) * (mBufferLevel - shift + kPreFilterOffset);

				memmove(mSourceBuffer[0], mSourceBuffer[0] + shift, bytesToShift);

				if (mbFilterStereo)
					memmove(mSourceBuffer[1], mSourceBuffer[1] + shift, bytesToShift);

				mBufferLevel -= shift;
				mResampleAccum -= (uint64)shift << 32;
			}
		}
	}
	
	// check that the resample source position isn't too far out of whack
	VDASSERT(mResampleAccum < (uint64)mOutputBuffer16.size() << (32+4));

	bool underflowDetected = false;
	uint32 bytes = mpAudioOut->EstimateHWBufferLevel(&underflowDetected);

	if (mMinLevel > bytes)
		mMinLevel = bytes;

	if (mMaxLevel < bytes)
		mMaxLevel = bytes;

	uint32 adjustedLatencyTargetMin = mLatencyTargetMin;
	uint32 adjustedLatencyTargetMax = mLatencyTargetMax;

	if (mActiveApi == kATAudioApi_XAudio2 || mActiveApi == kATAudioApi_WASAPI) {
		adjustedLatencyTargetMin += resampleCount * 4;
		adjustedLatencyTargetMax += resampleCount * 4;
	}

	bool dropBlock = false;
	if (++mCheckCounter >= 15) {
		mCheckCounter = 0;

		// None				See if we can remove data to lower latency
		// Underflow		Do nothing; we already add data for this
		// Overflow			Do nothing; we may be in turbo
		// Under+overflow	Increase spread

		bool tryDrop = false;
		if (!mUnderflowCount) {
			if (mMinLevel > adjustedLatencyTargetMin + resampleCount * 8) {
				tryDrop = true;
			}
		}

		if (tryDrop) {
			if (++mDropCounter >= 10) {
				mDropCounter = 0;
				dropBlock = true;
			}
		} else {
			mDropCounter = 0;
		}

		if (mpUIRenderer) {
			mAudioStatus.mMeasuredMin = mMinLevel;
			mAudioStatus.mMeasuredMax = mMaxLevel;
			mAudioStatus.mTargetMin = mLatencyTargetMin;
			mAudioStatus.mTargetMax = mLatencyTargetMax;
			mAudioStatus.mbStereoMixing = mbFilterStereo;
			mAudioStatus.mSamplingRate = mSamplingRate;

			mpUIRenderer->SetAudioStatus(&mAudioStatus);
		}

		mMinLevel = 0xFFFFFFFU;
		mMaxLevel = 0;
		mUnderflowCount = 0;
		mOverflowCount = 0;
	}

	if (++mProfileCounter >= 200) {
		mProfileCounter = 0;
		uint64 t = VDGetPreciseTick();

		mAudioStatus.mIncomingRate = (double)(mWritePosition - mProfileBlockStartPos) / (double)(t - mProfileBlockStartTime) * VDGetPreciseTicksPerSecond();

		mProfileBlockStartPos = mWritePosition;
		mProfileBlockStartTime = t;
	}

	if (bytes < adjustedLatencyTargetMin || underflowDetected) {
		++mAudioStatus.mUnderflowCount;
		++mUnderflowCount;

		mpAudioOut->Write(mOutputBuffer16.data(), resampleCount * 4);

		mDropCounter = 0;
		dropBlock = false;
	}

	if (dropBlock) {
		++mAudioStatus.mDropCount;
	} else {
		if (bytes < adjustedLatencyTargetMin + adjustedLatencyTargetMax) {
			if (pushAudio || true) {
				mRepeatAccum += mRepeatInc;

				uint32 count = mRepeatAccum >> 16;
				mRepeatAccum &= 0xffff;

				if (count > 10)
					count = 10;

				while(count--)
					mpAudioOut->Write(mOutputBuffer16.data(), resampleCount * 4);
			}
		} else {
			++mOverflowCount;
			++mAudioStatus.mOverflowCount;
		}
	}

	mpAudioOut->Flush();
}

void ATAudioOutput::RecomputeBuffering() {
	if (mActiveApi == kATAudioApi_XAudio2 || mActiveApi == kATAudioApi_WASAPI) {
		mLatencyTargetMin = 0;
		mLatencyTargetMax = mSamplingRate / 15 * 4;
	} else {
		mLatencyTargetMin = ((mLatency * mSamplingRate + 500) / 1000) * 4;
		mLatencyTargetMax = mLatencyTargetMin + ((mExtraBuffer * mSamplingRate + 500) / 1000) * 4;
	}
}

void ATAudioOutput::RecomputeResamplingRate() {
	mResampleRate = (sint64)(0.5 + 4294967296.0 * mAudioStatus.mExpectedRate / (double)mSamplingRate);
}

void ATAudioOutput::ReinitAudio() {
	if (mSelectedApi == kATAudioApi_Auto) {
		if (!ReinitAudio(kATAudioApi_WASAPI))
			ReinitAudio(kATAudioApi_WaveOut);
	} else {
		ReinitAudio(mSelectedApi);
	}
}

bool ATAudioOutput::ReinitAudio(ATAudioApi api) {
	if (api == kATAudioApi_WASAPI)
		mpAudioOut = VDCreateAudioOutputWASAPIW32();
	else if (api == kATAudioApi_XAudio2)
		mpAudioOut = VDCreateAudioOutputXAudio2W32();
	else if (api == kATAudioApi_DirectSound)
		mpAudioOut = VDCreateAudioOutputDirectSoundW32();
	else
		mpAudioOut = VDCreateAudioOutputWaveOutW32();

	mActiveApi = api;

	const uint32 preferredSamplingRate = mpAudioOut->GetPreferredSamplingRate(nullptr);

	if (preferredSamplingRate == 0)
		mSamplingRate = 48000;
	else if (preferredSamplingRate < 44100)
		mSamplingRate = 44100;
	else if (preferredSamplingRate > 48000)
		mSamplingRate = 48000;
	else
		mSamplingRate = preferredSamplingRate;

	nsVDWinFormats::WaveFormatExPCM wfex { mSamplingRate, 2, 16 };
	bool success = mpAudioOut->Init(kBufferSize * 4, 30, (const tWAVEFORMATEX *)&wfex, NULL);

	if (!success)
		mpAudioOut->GoSilent();

	if (!mpAudioOut->Start())
		success = false;

	RecomputeBuffering();

	return success;
}

///////////////////////////////////////////////////////////////////////////

IATAudioOutput *ATCreateAudioOutput() {
	return new ATAudioOutput;
}
