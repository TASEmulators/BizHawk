//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - cassette analog decoder filters
//	Copyright (C) 2009-2017 Avery Lee
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
#include <complex>
#include <at/atio/cassettedecoder.h>

ATCassetteDecoderFSK::ATCassetteDecoderFSK() {
	Reset();
}

void ATCassetteDecoderFSK::Reset() {
	mAcc0R = 0;
	mAcc0I = 0;
	mAcc1R = 0;
	mAcc1I = 0;
	mIndex = 0;
	memset(mHistory, 0, sizeof mHistory);
}

template<bool T_DoAnalysis>
void ATCassetteDecoderFSK::Process(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest) {
	static constexpr float sin_0_24 = 0;
	static constexpr float sin_1_24 = 0.25881904510252076234889883762405f;
	static constexpr float sin_2_24 = 0.5f;
	static constexpr float sin_3_24 = 0.70710678118654752440084436210485f;
	static constexpr float sin_4_24 = 0.86602540378443864676372317075294;
	static constexpr float sin_5_24 = 0.9659258262890682867497431997289f;
	static constexpr float sin_6_24 = 1.0f;

	static constexpr float sintab_24[24]={
		sin_0_24,	sin_1_24,	sin_2_24,	sin_3_24,
		sin_4_24,	sin_5_24,	sin_6_24,	sin_5_24,
		sin_4_24,	sin_3_24,	sin_2_24,	sin_1_24,
		-sin_0_24,	-sin_1_24,	-sin_2_24,	-sin_3_24,
		-sin_4_24,	-sin_5_24,	-sin_6_24,	-sin_5_24,
		-sin_4_24,	-sin_3_24,	-sin_2_24,	-sin_1_24,
	};

	static constexpr struct RotTab {
		sint16 vec[32][4] = {};

		static constexpr sint16 intround16(float v) {
			return v < 0 ? (sint16)(v - 0.5f) : (sint16)(v + 0.5f);
		}

		constexpr RotTab() {
			for(int i=0; i<24; ++i) {
				vec[i][0] = intround16(sintab_24[(6 + i*3) % 24] * 0x1000);
				vec[i][1] = intround16(sintab_24[(0 + i*3) % 24] * 0x1000);
				vec[i][2] = intround16(sintab_24[(6 + i*4) % 24] * 0x1000);
				vec[i][3] = intround16(sintab_24[(0 + i*4) % 24] * 0x1000);
			}
		}
	} kRotTab;

	uint32 bitaccum = 0;
	uint32 bitcounter = 32 - bitoffset;

	do {
		// update history window
		const sint32 x1 = *samples;
		samples += 2;

		// We sample at 31960Hz and use a 24-point DFT.
		// 3995Hz (zero) filter extracts from bin 3.
		// 5327Hz (one) filter extracts from bin 4.
		//
		// We compute these via a sliding DFT. The per-sample phase shift angles
		// for the two filters are 2*pi/24*3 = pi/4 and 2*pi/24*4 = pi/3. A 24-point
		// DFT with rectangular windowing performed best in testing. A 32-point
		// DFT is not bad, but has non-ideal frequencies; a 48-point DFT was too
		// long in time domain. Different windows didn't work out either as the
		// ringing in the frequency domain increases the crosstalk between the
		// filters. With the 24-point DFT, the bins for the two FSK tones have
		// nulls in their responses at each other's frequencies, which is what we
		// want.
		//
		// The sliding DFT is computed in integer arithmetic to avoid error
		// accumulation that would occur with floating-point. A decay constant
		// is normally used to combat this, but if it is too short it makes the
		// response asymmetric, and if it is too long it causes false pulses on
		// long runs -- which is very important for the 30s leader.
		//
		// There is no silence detection on the filter. Cases have been seen where
		// the FSK detector has been able to recover data at extremely low volume
		// levels, as low as -60dB.
		//
		// This filter introduces a delay of 12 samples; currently we just ignore
		// that.

		uint32 hpos1 = mIndex++;

		if (mIndex == 24)
			mIndex = 0;

		const sint32 x0 = mHistory[hpos1];
		mHistory[hpos1] = x1;

		const sint32 y = x1 - x0;

		mAcc0R += kRotTab.vec[mIndex][0] * y;
		mAcc0I += kRotTab.vec[mIndex][1] * y;
		mAcc1R += kRotTab.vec[mIndex][2] * y;
		mAcc1I += kRotTab.vec[mIndex][3] * y;

		const float acc0r = (float)mAcc0R;
		const float acc0i = (float)mAcc0I;
		const float acc1r = (float)mAcc1R;
		const float acc1i = (float)mAcc1I;
		const float zero = acc0r * acc0r + acc0i * acc0i;
		const float one = acc1r * acc1r + acc1i * acc1i;

		if (T_DoAnalysis) {
			adest[0] = (float)mHistory[hpos1 >= 12 ? hpos1 - 12 : hpos1 + 12] * (1.0f / 32767.0f);
			adest[1] = sqrtf(zero) * (1.0f / 32767.0f / 4096.0f / 12.0f);
			adest[2] = sqrtf(one) * (1.0f / 32767.0f / 4096.0f / 12.0f);
			adest[3] = (one > zero ? 0.8f : -0.8f);
			// slots 4 and 5 reserved for direct decoder
			adest += 6;
		}

		bitaccum += bitaccum;
		if (one >= zero)
			++bitaccum;

		if (!--bitcounter) {
			bitcounter = 32;
			*bitfield++ |= bitaccum;
		}
	} while(--n);

	if (bitcounter < 32)
		*bitfield++ |= bitaccum << bitcounter;
}

template void ATCassetteDecoderFSK::Process<false>(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);
template void ATCassetteDecoderFSK::Process<true>(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);

///////////////////////////////////////////////////////////////////////////////

ATCassetteDecoderDirect::ATCassetteDecoderDirect() {
	Reset();
}

void ATCassetteDecoderDirect::Reset() {
	mPrevLevel = 0;
	mAGC = 0;
}

template<bool T_DoAnalysis>
void ATCassetteDecoderDirect::Process(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest) {
	uint32 bitaccum = 0;
	uint32 bitcounter = 32 - bitoffset;

	do {
		const float x = *samples;
		samples += 2;

		float y = x - mPrevLevel;
		mPrevLevel = x;

		float z = fabsf(y);
		const bool edge = (z > mAGC * 0.25f);

		if (edge)
			mbCurrentState = (y > 0);

		if (T_DoAnalysis) {
			// slots 0-3 reserved for FSK decoder
			adest[4] = mAGC * (1.0f / 32767.0f);
			adest[5] = mbCurrentState ? 0.8f : -0.8f;
			adest += 6;
		}

		if (z > mAGC)
			mAGC += (z - mAGC) * 0.40f;
		else
			mAGC += (z - mAGC) * 0.05f;

		bitaccum += bitaccum;
		if (mbCurrentState)
			++bitaccum;

		if (!--bitcounter) {
			bitcounter = 32;
			*bitfield++ |= bitaccum;
		}
	} while(--n);

	if (bitcounter < 32)
		*bitfield++ |= bitaccum << bitcounter;
}

template void ATCassetteDecoderDirect::Process<false>(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);
template void ATCassetteDecoderDirect::Process<true>(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);
