//	Altirra - Atari 800/800XL/5200 emulator
//	Device emulation library - modem sound synthesis engine
//	Copyright (C) 2009-2018 Avery Lee
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

#include "stdafx.h"
#include <vd2/system/refcount.h>
#include <vd2/system/math.h>
#include <at/atcore/audiomixer.h>
#include <at/atdevices/modemsound.h>

///////////////////////////////////////////////////////////////////////////

class ATSoundSourceSingleTone final : public IATAudioSampleSource, public vdrefcounted<IVDRefCount> {
public:
	void SetTone(float p1) {
		mPitch1 = p1;
	}

	void MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate);

private:
	float mPitch1 = 1;
};

void ATSoundSourceSingleTone::MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate) {
	// compute phase offsets
	const double invMixingRate2PI = nsVDMath::krTwoPi / (double)mixingRate;
	const double offsetSeconds2PI = (double)offset * invMixingRate2PI;
	const double phase1 = mPitch1 * offsetSeconds2PI;

	// compute initial vectors and phase shifts
	const float shift1R = cos(mPitch1 * invMixingRate2PI);
	const float shift1I = sin(mPitch1 * invMixingRate2PI);

	volume *= 0.5f;
	float vec1R = cos(phase1) * volume;
	float vec1I = sin(phase1) * volume;

	// generate samples
	while(len--) {
		*dst++ += vec1R;

		float vec1RNext = vec1R * shift1R - vec1I * shift1I;
		float vec1INext = vec1R * shift1I + vec1I * shift1R; 
		vec1R = vec1RNext;
		vec1I = vec1INext;
	}
}

///////////////////////////////////////////////////////////////////////////

class ATSoundSourceDualTone final : public IATAudioSampleSource, public vdrefcounted<IVDRefCount> {
public:
	void SetTones(float p1, float p2) {
		mPitch1 = p1;
		mPitch2 = p2;
	}

	void MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate);

private:
	float mPitch1 = 1;
	float mPitch2 = 1;
};

void ATSoundSourceDualTone::MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate) {
	// compute phase offsets
	const double invMixingRate2PI = nsVDMath::krTwoPi / (double)mixingRate;
	const double offsetSeconds2PI = (double)offset * invMixingRate2PI;
	const double phase1 = mPitch1 * offsetSeconds2PI;
	const double phase2 = mPitch2 * offsetSeconds2PI;

	// compute initial vectors and phase shifts
	const float shift1R = cos(mPitch1 * invMixingRate2PI);
	const float shift1I = sin(mPitch1 * invMixingRate2PI);
	const float shift2R = cos(mPitch2 * invMixingRate2PI);
	const float shift2I = sin(mPitch2 * invMixingRate2PI);

	volume *= 0.5f;
	float vec1R = cos(phase1) * volume;
	float vec1I = sin(phase1) * volume;
	float vec2R = cos(phase2) * volume;
	float vec2I = sin(phase2) * volume;

	// generate samples
	while(len--) {
		*dst++ += vec1R + vec2R;

		float vec1RNext = vec1R * shift1R - vec1I * shift1I;
		float vec1INext = vec1R * shift1I + vec1I * shift1R; 
		float vec2RNext = vec2R * shift2R - vec2I * shift2I;
		float vec2INext = vec2R * shift2I + vec2I * shift2R; 
		vec1R = vec1RNext;
		vec1I = vec1INext;
		vec2R = vec2RNext;
		vec2I = vec2INext;
	}
}

///////////////////////////////////////////////////////////////////////////

// Data sound source for V.22 1200 baud.
//
// V.22 sends dibits by PSK at 600 baud with the originating carrier at
// 1200Hz and the answering carrier at 2400Hz.
//
class ATSoundSourceModemDataV22 final : public IATAudioSampleSource, public vdrefcounted<IVDRefCount> {
public:
	void SetPitch(float pitch) {
		mPitch = pitch;
	}

	void SetScrambled(bool scrambled) {
		if (scrambled) {
			mScramblerLFSR = 1;
		} else {
			// Yes, we are intentionally locking up the LFSR here to
			// produce zeroes, which will then select a phase shift of
			// 270d for the unscrambled '1'.
			mScramblerLFSR = 0;
		}
	}

	void MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate);

	float mPitch = 1;
	uint32 mPhaseOffset = 0;
	uint32 mBitAccum = 0;
	uint32 mScramblerLFSR = 1;
};

void ATSoundSourceModemDataV22::MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate) {
	static constexpr uint32 kBitInc = (uint32)(0.5 + 600.0 / 63920.0 * (double)(1ULL << 31));

	// compute phase offsets
	const double invMixingRate2PI = nsVDMath::krTwoPi / (double)mixingRate;
	const double offsetSeconds2PI = (double)offset * invMixingRate2PI;

	// compute initial vectors and phase shifts per sample
	const float shiftR = cosf(mPitch * (float)invMixingRate2PI);
	const float shiftI = sinf(mPitch * (float)invMixingRate2PI);

	volume *= 0.5f;
	
	// Note that these are shifted as we need phase[0] to be a shift of
	// 270 degrees for the unscrambled 1 bit case. That's the only case
	// we really care about accuracy wise as the scrambled data sounds
	// like noise, and we're not actually sending data here.
	static constexpr float kPhaseR[4]={0,1,0,-1};
	static constexpr float kPhaseI[4]={-1,0,1,0};

	float vecR = kPhaseR[mPhaseOffset & 3] * volume;
	float vecI = kPhaseI[mPhaseOffset & 3] * volume;


	// generate samples
	while(len--) {
		*dst++ += vecR;

		float vecRNext = vecR * shiftR - vecI * shiftI;
		float vecINext = vecR * shiftI + vecI * shiftR;
		vecR = vecRNext;
		vecI = vecINext;

		// check if we've passed a bit
		mBitAccum += kBitInc;
		if (mBitAccum >= (1U << 31)) {
			mBitAccum &= 0x7FFFFFFFU;

			// advance scrambler LFSR by two bits
			uint32 shiftBits = mScramblerLFSR & 3;
			mScramblerLFSR ^= (shiftBits << 14);
			mScramblerLFSR ^= (shiftBits << 17);
			mScramblerLFSR >>= 2;

			// update phase and amplitude
			mPhaseOffset += shiftBits;

			const float stepR = kPhaseR[shiftBits & 3];
			const float stepI = kPhaseI[shiftBits & 3];

			vecRNext = vecR * stepR - vecI * stepI;
			vecINext = vecR * stepI + vecI * stepR;
			vecR = vecRNext;
			vecI = vecINext;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

class ATSoundSourceModemData final : public IATAudioSampleSource, public vdrefcounted<IVDRefCount> {
public:
	void MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate);

	uint32 mPhaseOffset = 0;
	uint32 mBitAccum = 0;
	uint32 mScramblerLFSR = 1;
};

void ATSoundSourceModemData::MixAudio(float *dst, uint32 len, float volume, uint64 offset, float mixingRate) {
	static constexpr float pitch = 1800.0f;
	static constexpr uint32 kBitInc = (uint32)(0.5 + 2400.0 / 63920.0 * (double)(1ULL << 31));

	// compute phase offsets
	const double invMixingRate2PI = nsVDMath::krTwoPi / (double)mixingRate;
	const double offsetSeconds2PI = (double)offset * invMixingRate2PI;

	// compute initial vectors and phase shifts per sample
	const float shiftR = cosf(pitch * (float)invMixingRate2PI);
	const float shiftI = sinf(pitch * (float)invMixingRate2PI);

	volume *= 0.5f;
	
	static const float kPhaseR[4]={1,0,-1,0};
	static const float kPhaseI[4]={0,1,0,-1};

	float vecR = kPhaseR[mPhaseOffset & 3] * volume;
	float vecI = kPhaseI[mPhaseOffset & 3] * volume;


	// generate samples
	while(len--) {
		*dst++ += vecR;

		float vecRNext = vecR * shiftR - vecI * shiftI;
		float vecINext = vecR * shiftI + vecI * shiftR;
		vecR = vecRNext;
		vecI = vecINext;

		// check if we've passed a bit
		mBitAccum += kBitInc;
		if (mBitAccum >= (1U << 31)) {
			mBitAccum &= 0x7FFFFFFFU;

			// advance scrambler LFSR by 5 bits (1 + x^-18 + x^-23)
			uint32 shiftBits = mScramblerLFSR & 31;
			mScramblerLFSR ^= (shiftBits << 18);
			mScramblerLFSR ^= (shiftBits << 23);
			mScramblerLFSR >>= 5;

			// update phase and amplitude
			mPhaseOffset += (shiftBits & 3);

			const float stepR = kPhaseR[shiftBits & 3];
			const float stepI = kPhaseI[shiftBits & 3];

			vecRNext = vecR * stepR - vecI * stepI;
			vecINext = vecR * stepI + vecI * stepR;
			vecR = vecRNext;
			vecI = vecINext;
		}
	}
}

///////////////////////////////////////////////////////////////////////////

ATModemSoundEngine::ATModemSoundEngine() {
	mpSingleToneSource = new ATSoundSourceSingleTone;
	mpSingleToneSource->AddRef();
	mpDualToneSource = new ATSoundSourceDualTone;
	mpDualToneSource->AddRef();
}

ATModemSoundEngine::~ATModemSoundEngine() {
	vdsaferelease <<= mpSingleToneSource;
	vdsaferelease <<= mpDualToneSource;
}

void ATModemSoundEngine::Reset() {
	Stop();
}

void ATModemSoundEngine::Shutdown() {
	Reset();

	mpAudioMixer = nullptr;
}

void ATModemSoundEngine::SetAudioEnabledByPhase(bool enabled) {
	if (mbAudioEnabledByPhase != enabled) {
		mbAudioEnabledByPhase = enabled;

		UpdateAudioEnabled();
	}
}

void ATModemSoundEngine::SetSpeakerEnabled(bool enabled) {
	if (mbSpeakerEnabled != enabled) {
		mbSpeakerEnabled = enabled;

		UpdateAudioEnabled();
	}
}

void ATModemSoundEngine::PlayDialTone() {
	if (!mbAudioEnabled)
		return;

	Stop();

	mpDualToneSource->SetTones(350.0f, 440.0f);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, mpDualToneSource, mpDualToneSource, 1.0f);
}

void ATModemSoundEngine::PlayDTMFTone(uint32 index) {
	if (!mbAudioEnabled)
		return;

	Stop();

	static const float kTones[12][2] = {
		{ 941.0f, 1336.0f },
		{ 697.0f, 1209.0f },
		{ 697.0f, 1336.0f },
		{ 697.0f, 1477.0f },
		{ 770.0f, 1209.0f },
		{ 770.0f, 1336.0f },
		{ 770.0f, 1477.0f },
		{ 852.0f, 1209.0f },
		{ 852.0f, 1336.0f },
		{ 852.0f, 1477.0f },

		// star
		{ 941.0f, 1209.0f },

		// pound
		{ 941.0f, 1477.0f },
	};

	mpDualToneSource->SetTones(kTones[index][0], kTones[index][1]);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, mpDualToneSource, mpDualToneSource, 1.0f);
}

void ATModemSoundEngine::PlayRingingTone() {
	if (!mbAudioEnabled)
		return;

	Stop();

	mpDualToneSource->SetTones(440.0f, 480.0f);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, mpDualToneSource, mpDualToneSource, 1.0f);
}

void ATModemSoundEngine::PlayModemData(float volume) {
	if (!mbAudioEnabled)
		return;

	Stop();

	vdrefptr<ATSoundSourceModemData> src { new ATSoundSourceModemData };
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src, src, volume);
}

void ATModemSoundEngine::PlayModemDataV22(bool answering, bool scrambled) {
	if (!mbAudioEnabled)
		return;

	mbDialToneActive = false;

	ATSoundId& sid = answering ? mSoundId2 : mSoundId;
	if (sid != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(sid);
		sid = ATSoundId::Invalid;
	}

	vdrefptr<ATSoundSourceModemDataV22> src { new ATSoundSourceModemDataV22 };
	src->SetPitch(answering ? 2400.0f : 1200.0f);
	src->SetScrambled(scrambled);
	sid = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src, src, 0.5f);
}

void ATModemSoundEngine::PlayOriginatingToneBell103() {
	if (!mbAudioEnabled)
		return;

	mbDialToneActive = false;

	if (mSoundId != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(mSoundId);
		mSoundId = ATSoundId::Invalid;
	}

	vdrefptr<ATSoundSourceSingleTone> src { new ATSoundSourceSingleTone };
	src->SetTone(1270.0f);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src, src, 0.5f);
}

void ATModemSoundEngine::PlayOriginatingToneV32() {
	if (!mbAudioEnabled)
		return;

	if (mSoundId2 != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(mSoundId);
		mSoundId2 = ATSoundId::Invalid;
	}

	vdrefptr<ATSoundSourceSingleTone> src { new ATSoundSourceSingleTone };
	src->SetTone(1800.0f);
	mSoundId2 = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src, src, 0.5f);
}

void ATModemSoundEngine::PlayTrainingToneV32() {
	if (!mbAudioEnabled)
		return;

	Stop();

	vdrefptr<ATSoundSourceSingleTone> src1 { new ATSoundSourceSingleTone };
	src1->SetTone(1800.0f);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src1, src1, 0.5f);

	vdrefptr<ATSoundSourceDualTone> src2 { new ATSoundSourceDualTone };
	src2->SetTones(600.0f, 3000.0f);
	mSoundId2 = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src2, src2, 0.5f);
}

void ATModemSoundEngine::PlayAnswerTone(bool bell212a) {
	if (!mbAudioEnabled)
		return;

	Stop();

	vdrefptr<ATSoundSourceSingleTone> src { new ATSoundSourceSingleTone };

	static constexpr float kAnswerToneV22 = 2100.0f;
	static constexpr float kAnswerToneBell212A = 2225.0f;

	src->SetTone(bell212a ? kAnswerToneBell212A : kAnswerToneV22);
	mSoundId2 = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, src, src, 0.5f);
}

void ATModemSoundEngine::PlayEchoSuppressionTone() {
	if (!mbAudioEnabled)
		return;

	mbDialToneActive = false;

	if (mSoundId != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(mSoundId);
		mSoundId = ATSoundId::Invalid;
	}

	mpSingleToneSource->SetTone(2100.0f);
	mSoundId = mpAudioMixer->GetSamplePlayer().AddLoopingSound(kATAudioMix_Modem, 0, mpSingleToneSource, mpSingleToneSource, 1.0f);
}

void ATModemSoundEngine::Stop() {
	Stop1();

	if (mSoundId2 != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(mSoundId2);
		mSoundId2 = ATSoundId::Invalid;
	}
}

void ATModemSoundEngine::Stop1() {
	mbDialToneActive = false;

	if (mSoundId != ATSoundId::Invalid) {
		mpAudioMixer->GetSamplePlayer().StopSound(mSoundId);
		mSoundId = ATSoundId::Invalid;
	}
}

bool ATModemSoundEngine::RequiresStereoMixingNow() const {
	return false;
}

void ATModemSoundEngine::WriteAudio(const ATSyncAudioMixInfo& mixInfo) {
}

void ATModemSoundEngine::InitAudioOutput(IATAudioMixer *mixer) {
	mpAudioMixer = mixer;

	UpdateAudioEnabled();
}

void ATModemSoundEngine::UpdateAudioEnabled() {
	mbAudioEnabled = mpAudioMixer && mbSpeakerEnabled && mbAudioEnabledByPhase;

	if (!mbAudioEnabled)
		Stop();
}
