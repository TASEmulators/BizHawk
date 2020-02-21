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
#include <vd2/system/cpuaccel.h>
#include <vd2/system/math.h>
#include <vd2/system/vectors.h>
#include "artifacting.h"
#include "artifacting_filters.h"
#include "gtia.h"
#include "gtiatables.h"

void ATArtifactPALLuma(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
void ATArtifactPALChroma(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
void ATArtifactPALFinal(uint32 *dst, const uint32 *ybuf, const uint32 *ubuf, const uint32 *vbuf, uint32 *ulbuf, uint32 *vlbuf, uint32 n);

#if VD_CPU_X86 || VD_CPU_X64
	void ATArtifactBlend_SSE2(uint32 *dst, const uint32 *src, uint32 n);
	void ATArtifactBlendExchange_SSE2(uint32 *dst, uint32 *blendDst, uint32 n);
	void ATArtifactBlendScanlines_SSE2(uint32 *dst0, const uint32 *src10, const uint32 *src20, uint32 n, float intensity);
	void ATArtifactNTSCFinal_SSE2(void *dst, const void *srcr, const void *srcg, const void *srcb, uint32 count);
#endif

#ifdef VD_CPU_X86
	void __cdecl ATArtifactNTSCAccum_SSE2(void *rout, const void *table, const void *src, uint32 count);
	void __cdecl ATArtifactNTSCAccumTwin_SSE2(void *rout, const void *table, const void *src, uint32 count);

	void __stdcall ATArtifactPALLuma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void __stdcall ATArtifactPALLumaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void __stdcall ATArtifactPALChroma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void __stdcall ATArtifactPALChromaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void __stdcall ATArtifactPALFinal_SSE2(uint32 *dst, const uint32 *ybuf, const uint32 *ubuf, const uint32 *vbuf, uint32 *ulbuf, uint32 *vlbuf, uint32 n);
#endif

#ifdef VD_CPU_AMD64
	void ATArtifactNTSCAccum_SSE2(void *rout, const void *table, const void *src, uint32 count);
	void ATArtifactNTSCAccumTwin_SSE2(void *rout, const void *table, const void *src, uint32 count);

	void ATArtifactPALLuma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void ATArtifactPALLumaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void ATArtifactPALChroma_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void ATArtifactPALChromaTwin_SSE2(uint32 *dst, const uint8 *src, uint32 n, const uint32 *kernels);
	void ATArtifactPALFinal_SSE2(uint32 *dst, const uint32 *ybuf, const uint32 *ubuf, const uint32 *vbuf, uint32 *ulbuf, uint32 *vlbuf, uint32 n);
#endif

#ifdef VD_CPU_ARM64
	void ATArtifactBlend_NEON(uint32 *dst, const uint32 *src, uint32 n);
	void ATArtifactBlendExchange_NEON(uint32 *dst, uint32 *blendDst, uint32 n);
	void ATArtifactBlendScanlines_NEON(uint32 *dst0, const uint32 *src10, const uint32 *src20, uint32 n, float intensity);

	void ATArtifactNTSCAccum_NEON(void *rout, const void *table, const void *src, uint32 count);
	void ATArtifactNTSCAccumTwin_NEON(void *rout, const void *table, const void *src, uint32 count);
	void ATArtifactNTSCFinal_NEON(void *dst0, const void *srcr0, const void *srcg0, const void *srcb0, uint32 count);
#endif

namespace {
	constexpr float kSaturation = 75.0f / 255.0f;

	// Unless we are showing horizontal blank, we only need to process pixels within
	// the visible range of $22-DD in color clocks, or twice that in hires pixels (7MHz
	// dot clock).
	constexpr int kLeftBorder7MHz = 34*2;
	constexpr int kRightBorder7MHz = 222*2;
	constexpr int kLeftBorder14MHz = kLeftBorder7MHz*2;
	constexpr int kRightBorder14MHz = kRightBorder7MHz*2;

	// These versions are for the display rect, expanded to the nearest multiples of 2/4/8/16. (They
	// do not currently differ.)
	constexpr int kLeftBorder7MHz_2 = kLeftBorder7MHz & ~1;
	constexpr int kRightBorder7MHz_2 = (kRightBorder7MHz + 1) & ~1;
	constexpr int kLeftBorder14MHz_2 = kLeftBorder14MHz & ~1;
	constexpr int kRightBorder14MHz_2 = (kRightBorder14MHz + 1) & ~1;

	constexpr int kLeftBorder7MHz_4 = kLeftBorder7MHz & ~3;
	constexpr int kRightBorder7MHz_4 = (kRightBorder7MHz + 3) & ~3;
	constexpr int kLeftBorder14MHz_4 = kLeftBorder14MHz & ~3;
	constexpr int kRightBorder14MHz_4 = (kRightBorder14MHz + 3) & ~3;

	constexpr int kLeftBorder7MHz_8 = kLeftBorder7MHz & ~7;
	constexpr int kRightBorder7MHz_8 = (kRightBorder7MHz + 7) & ~7;
	constexpr int kLeftBorder14MHz_8 = kLeftBorder14MHz & ~7;
	constexpr int kRightBorder14MHz_8 = (kRightBorder14MHz + 7) & ~7;

	constexpr int kLeftBorder7MHz_16 = kLeftBorder7MHz & ~15;
	constexpr int kRightBorder7MHz_16 = (kRightBorder7MHz + 15) & ~15;
	constexpr int kLeftBorder14MHz_16 = kLeftBorder14MHz & ~15;
	constexpr int kRightBorder14MHz_16 = (kRightBorder14MHz + 15) & ~15;

	void GammaCorrect(uint8 *VDRESTRICT dst8, uint32 N, const uint8 *VDRESTRICT gammaTab) {
		for(uint32 i=0; i<N; ++i) {
			dst8[0] = gammaTab[dst8[0]];
			dst8[1] = gammaTab[dst8[1]];
			dst8[2] = gammaTab[dst8[2]];

			dst8 += 4;
		}
	}
}

ATArtifactingEngine::ATArtifactingEngine()
	: mbBlendActive(false)
	, mbBlendCopy(false)
	, mbHighNTSCTablesInited(false)
	, mbHighPALTablesInited(false)
	, mbGammaIdentity(false)
{
	mArtifactingParams = ATArtifactingParams::GetDefault();
}

ATArtifactingEngine::~ATArtifactingEngine() {
}

void ATArtifactingEngine::SetColorParams(const ATColorParams& params, const vdfloat3x3 *matrix) {
	mColorParams = params;

	float lumaRamp[16];
	ATComputeLumaRamp(params.mLumaRampMode, lumaRamp);

	const float yscale = params.mContrast * params.mIntensityScale;
	const float ybias = params.mBrightness * params.mIntensityScale;

	const vdfloat2 co_r = vdfloat2x2::rotation(params.mRedShift * (nsVDMath::kfPi / 180.0f)) * vdfloat2 { +0.9563f, +0.6210f } * params.mRedScale * params.mIntensityScale;
	const vdfloat2 co_g = vdfloat2x2::rotation(params.mGrnShift * (nsVDMath::kfPi / 180.0f)) * vdfloat2 { -0.2721f, -0.6474f } * params.mGrnScale * params.mIntensityScale;
	const vdfloat2 co_b = vdfloat2x2::rotation(params.mBluShift * (nsVDMath::kfPi / 180.0f)) * vdfloat2 { -1.1070f, +1.7046f } * params.mBluScale * params.mIntensityScale;

	const float artphase = params.mArtifactHue * (nsVDMath::kfTwoPi / 360.0f);
	const vdfloat2 rot_art { cosf(artphase), sinf(artphase) };

	float artr = 64.0f * 255.0f * nsVDMath::dot(rot_art, co_r) * kSaturation / 15.0f * params.mArtifactSat;
	float artg = 64.0f * 255.0f * nsVDMath::dot(rot_art, co_g) * kSaturation / 15.0f * params.mArtifactSat;
	float artb = 64.0f * 255.0f * nsVDMath::dot(rot_art, co_b) * kSaturation / 15.0f * params.mArtifactSat;

	for(int i=-15; i<16; ++i) {
		int ar = VDRoundToInt32(artr * (float)i);
		int ag = VDRoundToInt32(artg * (float)i);
		int ab = VDRoundToInt32(artb * (float)i);

		if (ar != (sint16)ar) ar = (ar < 0) ? -0x8000 : 0x7FFF;
		if (ag != (sint16)ag) ag = (ag < 0) ? -0x8000 : 0x7FFF;
		if (ab != (sint16)ab) ab = (ab < 0) ? -0x8000 : 0x7FFF;

		mArtifactRamp[i+15][0] = ab;
		mArtifactRamp[i+15][1] = ag;
		mArtifactRamp[i+15][2] = ar;
		mArtifactRamp[i+15][3] = 0;
	}

	memset(mChromaVectors, 0, sizeof(mChromaVectors));

	vdfloat3 cvec[16];
	cvec[0] = vdfloat3 { 0, 0, 0 };

	for(int j=0; j<15; ++j) {
		float i = 0;
		float q = 0;

		if (params.mbUsePALQuirks) {
			static const float kPALPhaseLookup[][4]={
				{ -1.0f,  1, -5.0f,  1 },
				{  0.0f,  1, -6.0f,  1 },
				{ -7.0f, -1, -7.0f,  1 },
				{ -6.0f, -1,  0.0f, -1 },
				{ -5.0f, -1, -1.0f, -1 },
				{ -4.0f, -1, -2.0f, -1 },
				{ -2.0f, -1, -4.0f, -1 },
				{ -1.0f, -1, -5.0f, -1 },
				{  0.0f, -1, -6.0f, -1 },
				{ -7.0f,  1, -7.0f, -1 },
				{ -5.0f,  1, -1.0f,  1 },
				{ -4.0f,  1, -2.0f,  1 },
				{ -3.0f,  1, -3.0f,  1 },
				{ -2.0f,  1, -4.0f,  1 },
				{ -1.0f,  1, -5.0f,  1 },
			};

			const float step = params.mHueRange * (nsVDMath::kfTwoPi / (15.0f * 360.0f));
			const float theta = (params.mHueStart - 33.0f) * (nsVDMath::kfTwoPi / 360.0f);

			const float *co = kPALPhaseLookup[j];

			float angle2 = theta + step * (co[0] + 3.0f);
			float angle3 = theta + step * (-co[2] - 3.0f);
			float i2 = cosf(angle2) * co[1];
			float q2 = sinf(angle2) * co[1];
			float i3 = cosf(angle3) * co[3];
			float q3 = sinf(angle3) * co[3];

			i = (i2 + i3) * 0.5f;
			q = (q2 + q3) * 0.5f;
		} else {
			float theta = nsVDMath::kfTwoPi * (params.mHueStart / 360.0f + (float)j * (params.mHueRange / (15.0f * 360.0f)));
			i = cosf(theta);
			q = sinf(theta);
		}

		vdfloat2 iq { i, q };
		vdfloat3 chroma { nsVDMath::dot(co_r, iq), nsVDMath::dot(co_g, iq), nsVDMath::dot(co_b, iq) };

		chroma *= params.mSaturation;

		cvec[j+1] = chroma;

		int icr = VDRoundToInt(chroma.x * (64.0f * 255.0f));
		int icg = VDRoundToInt(chroma.y * (64.0f * 255.0f));
		int icb = VDRoundToInt(chroma.z * (64.0f * 255.0f));

		if (icr != (short)icr) icr = (icr < 0) ? -0x8000 : 0x7FFF;
		if (icg != (short)icg) icg = (icg < 0) ? -0x8000 : 0x7FFF;
		if (icb != (short)icb) icb = (icb < 0) ? -0x8000 : 0x7FFF;

		mChromaVectors[j+1][0] = icb;
		mChromaVectors[j+1][1] = icg;
		mChromaVectors[j+1][2] = icr;
	}

	for(int i=0; i<16; ++i) {
		float y = lumaRamp[i & 15] * yscale + ybias;

		mLumaRamp[i] = VDRoundToInt32(y * (64.0f * 255.0f)) + 32;
	}

	// compute gamma correction table
	const float gamma = 1.0f / params.mGammaCorrect;
	mbGammaIdentity = fabsf(params.mGammaCorrect - 1.0f) < 1e-5f;

	if (mbGammaIdentity) {
		for(int i=0; i<256; ++i)
			mGammaTable[i] = (uint8)i;
	} else {
		for(int i=0; i<256; ++i)
			mGammaTable[i] = (uint8)VDRoundToInt32(powf((float)i / 255.0f, gamma) * 255.0f);
	}

	for(int i=0; i<256; ++i) {
		int c = i >> 4;

		float y = lumaRamp[i & 15] * yscale + ybias;
		float r = cvec[c].x + y;
		float g = cvec[c].y + y;
		float b = cvec[c].z + y;

		mPalette[i]
			= (VDClampedRoundFixedToUint8Fast((float)r) << 16)
			+ (VDClampedRoundFixedToUint8Fast((float)g) <<  8)
			+ (VDClampedRoundFixedToUint8Fast((float)b)      );

		if (matrix) {
			mCorrectedPalette[i] = mPalette[i];
		} else {
			if (r > 0.0f)
				r = powf(r, gamma);

			if (g > 0.0f)
				g = powf(g, gamma);

			if (b > 0.0f)
				b = powf(b, gamma);

			mCorrectedPalette[i]
				= (VDClampedRoundFixedToUint8Fast((float)r) << 16)
				+ (VDClampedRoundFixedToUint8Fast((float)g) <<  8)
				+ (VDClampedRoundFixedToUint8Fast((float)b)      );
		}
	}

	mbEnableColorCorrection = matrix != nullptr;

	if (matrix) {
		mColorMatchingMatrix = *matrix;

		mColorMatchingMatrix16[0][0] = VDRoundToInt32(mColorMatchingMatrix.x.x * 16384.0f);
		mColorMatchingMatrix16[0][1] = VDRoundToInt32(mColorMatchingMatrix.x.y * 16384.0f);
		mColorMatchingMatrix16[0][2] = VDRoundToInt32(mColorMatchingMatrix.x.z * 16384.0f);
		mColorMatchingMatrix16[1][0] = VDRoundToInt32(mColorMatchingMatrix.y.x * 16384.0f);
		mColorMatchingMatrix16[1][1] = VDRoundToInt32(mColorMatchingMatrix.y.y * 16384.0f);
		mColorMatchingMatrix16[1][2] = VDRoundToInt32(mColorMatchingMatrix.y.z * 16384.0f);
		mColorMatchingMatrix16[2][0] = VDRoundToInt32(mColorMatchingMatrix.z.x * 16384.0f);
		mColorMatchingMatrix16[2][1] = VDRoundToInt32(mColorMatchingMatrix.z.y * 16384.0f);
		mColorMatchingMatrix16[2][2] = VDRoundToInt32(mColorMatchingMatrix.z.z * 16384.0f);

		// The PAL/SECAM standards specify a gamma of 2.8. However, there is apparently
		// a lot of deviation from this in the real world, where 2.2 and 2.5 are found instead.
		// 2.5 and 2.8 seem to be too extreme compared to actual screenshots of a color map
		// on an actual PAL system and display, so for now we just use 2.2.
		const float nativeGamma = 2.2f;

		for(int i=0; i<256; ++i) {
			float x = (float)i / 255.0f;
			float y = powf(x, nativeGamma);

			mCorrectLinearTable[i] = (sint16)floor(0.5f + y * 8191.0f);
		}

		if (params.mColorMatchingMode == ATColorMatchingMode::AdobeRGB) {
			for(int i=0; i<1024; ++i) {
				float x = (float)i / 1023.0f;
				float y = (x < 0) ? 0.0f : powf(x, 1.0f / 2.2f);

				y = powf(y, gamma);

				mCorrectGammaTable[i] = (uint8)(y * 255.0f + 0.5f);
			}
		} else {
			for(int i=0; i<1024; ++i) {
				float x = (float)i / 1023.0f;
				float y = (x < 0.0031308f) ? x * 12.92f : 1.055f * powf(x, 1.0f / 2.4f) - 0.055f;

				y = powf(y, gamma);

				mCorrectGammaTable[i] = (uint8)(y * 255.0f + 0.5f);
			}
		}

		ColorCorrect((uint8 *)mCorrectedPalette, 256);
	}

	mbHighNTSCTablesInited = false;
	mbHighPALTablesInited = false;
}

void ATArtifactingEngine::SetArtifactingParams(const ATArtifactingParams& params) {
	mArtifactingParams = params;

	mbHighNTSCTablesInited = false;
	mbHighPALTablesInited = false;
}

void ATArtifactingEngine::GetNTSCArtifactColors(uint32 c[2]) const {
	for(int i=0; i<2; ++i) {
		int art = i ? 0 : 30;
		int y = mLumaRamp[7];

		int cr = mArtifactRamp[art][2] + y;
		int cg = mArtifactRamp[art][1] + y;
		int cb = mArtifactRamp[art][0] + y;

		cr >>= 6;
		cg >>= 6;
		cb >>= 6;

		if (cr < 0)
			cr = 0;
		else if (cr > 255)
			cr = 255;

		if (cg < 0)
			cg = 0;
		else if (cg > 255)
			cg = 255;

		if (cb < 0)
			cb = 0;
		else if (cb > 255)
			cb = 255;

		if (!mbGammaIdentity && !mbEnableColorCorrection) {
			cr = mGammaTable[cr];
			cg = mGammaTable[cg];
			cb = mGammaTable[cb];
		}

		c[i] = cb + (cg << 8) + (cr << 16);
	}

	if (mbEnableColorCorrection)
		ColorCorrect((uint8 *)c, 2);

}

void ATArtifactingEngine::SuspendFrame() {
	mbSavedPAL = mbPAL;
	mbSavedChromaArtifacts = mbChromaArtifacts;
	mbSavedChromaArtifactsHi = mbChromaArtifactsHi;
	mbSavedBypassOutputCorrection = mbBypassOutputCorrection;
	mbSavedBlendActive = mbBlendActive;
	mbSavedBlendCopy = mbBlendCopy;
}

void ATArtifactingEngine::ResumeFrame() {
	mbPAL = mbSavedPAL;
	mbChromaArtifacts = mbSavedChromaArtifacts;
	mbChromaArtifactsHi = mbSavedChromaArtifactsHi;
	mbBypassOutputCorrection = mbSavedBypassOutputCorrection;
	mbBlendActive = mbSavedBlendActive;
	mbBlendCopy = mbSavedBlendCopy;
}

void ATArtifactingEngine::BeginFrame(bool pal, bool chromaArtifacts, bool chromaArtifactHi, bool blendIn, bool blendOut, bool bypassOutputCorrection) {
	mbPAL = pal;
	mbChromaArtifacts = chromaArtifacts;
	mbChromaArtifactsHi = chromaArtifactHi;
	mbBypassOutputCorrection = bypassOutputCorrection;

	if (chromaArtifactHi) {
		if (pal) {
			if (!mbHighPALTablesInited)
				RecomputePALTables(mColorParams);
		} else {
			if (!mbHighNTSCTablesInited)
				RecomputeNTSCTables(mColorParams);
		}
	}

	if (pal && chromaArtifacts) {
		if (chromaArtifactHi) {
#if defined(VD_CPU_AMD64)
			memset(mPALDelayLineUV, 0, sizeof mPALDelayLineUV);
#else
#if defined(VD_CPU_X86)
			if (SSE2_enabled) {
				memset(mPALDelayLineUV, 0, sizeof mPALDelayLineUV);
			} else
#endif
			{
				VDMemset32(mPALDelayLineUV, 0x20002000, sizeof(mPALDelayLineUV) / sizeof(mPALDelayLineUV[0][0]));
			}
#endif
		} else {
			memset(mPALDelayLine32, 0, sizeof mPALDelayLine32);
		}
	}

	mbBlendCopy = !blendIn;
	mbBlendActive = blendOut;
}

void ATArtifactingEngine::Artifact8(uint32 y, uint32 dst[N], const uint8 src[N], bool scanlineHasHiRes, bool temporaryUpdate, bool includeHBlank) {
	if (!mbChromaArtifacts)
		BlitNoArtifacts(dst, src, scanlineHasHiRes);
	else if (mbPAL) {
		if (mbChromaArtifactsHi)
			ArtifactPALHi(dst, src, scanlineHasHiRes, (y & 1) != 0);
		else
			ArtifactPAL8(dst, src);
	} else {
		if (mbChromaArtifactsHi)
			ArtifactNTSCHi(dst, src, scanlineHasHiRes, includeHBlank);
		else
			ArtifactNTSC(dst, src, scanlineHasHiRes, includeHBlank);
	}

	if (mbBlendActive && y < M) {
		uint32 *blendDst = mbChromaArtifactsHi ? mPrevFrame14MHz[y] : mPrevFrame7MHz[y];
		uint32 n = mbChromaArtifactsHi ? N*2 : N;

		if (!includeHBlank) {
			if (mbChromaArtifactsHi) {
				blendDst += kLeftBorder14MHz_4;
				dst += kLeftBorder14MHz_4;
				n = kRightBorder14MHz_4 - kLeftBorder14MHz_4;
			} else {
				blendDst += kLeftBorder7MHz_4;
				dst += kLeftBorder7MHz_4;
				n = kRightBorder7MHz_4 - kLeftBorder7MHz_4;
			}
		}

		if (mbBlendCopy) {
			if (!temporaryUpdate)
				memcpy(blendDst, dst, sizeof(uint32)*n);
		} else {
			if (temporaryUpdate)
				Blend(dst, blendDst, n);
			else
				BlendExchange(dst, blendDst, n);
		}
	}
}

void ATArtifactingEngine::Artifact32(uint32 y, uint32 *dst, uint32 width, bool temporaryUpdate, bool includeHBlank) {
	if (mbPAL)
		ArtifactPAL32(dst, width);

	if (mbBlendActive && y < M && width <= N*2) {
		uint32 *blendDst = width > N ? mPrevFrame14MHz[y] : mPrevFrame7MHz[y];

		if (mbBlendCopy) {
			if (!temporaryUpdate)
				memcpy(blendDst, dst, sizeof(uint32)*width);
		} else {
			if (temporaryUpdate)
				Blend(dst, blendDst, width);
			else
				BlendExchange(dst, blendDst, width);
		}
	}

	if (!mbBypassOutputCorrection) {
		if (mbEnableColorCorrection)
			ColorCorrect((uint8 *)dst, width);
		if (!mbGammaIdentity)
			GammaCorrect((uint8 *)dst, width, mGammaTable);
	}
}

void ATArtifactingEngine::InterpolateScanlines(uint32 *dst, const uint32 *src1, const uint32 *src2, uint32 n) {
#if VD_CPU_X86 || VD_CPU_X64
	if (SSE2_enabled) {
		ATArtifactBlendScanlines_SSE2(dst, src1, src2, n, mArtifactingParams.mScanlineIntensity);
		return;
	}
#elif VD_CPU_ARM64
	ATArtifactBlendScanlines_NEON(dst, src1, src2, n, mArtifactingParams.mScanlineIntensity);
	return;
#endif

	for(uint32 i=0; i<n; ++i) {
		uint32 prev = src1[i];
		uint32 next = src2[i];
		uint32 r = (prev | next) - (((prev ^ next) & 0xfefefe) >> 1);

		r -= (r & 0xfcfcfc) >> 2;
		dst[i] = r;
	}
}

void ATArtifactingEngine::ArtifactPAL8(uint32 dst[N], const uint8 src[N]) {
	const uint32 *VDRESTRICT palette = mbBypassOutputCorrection ? mPalette : mCorrectedPalette;

	for(int i=0; i<N; ++i) {
		uint8 prev = mPALDelayLine[i];
		uint8 next = src[i];
		uint32 prevColor = palette[(prev & 0xf0) + (next & 0x0f)];
		uint32 nextColor = palette[next];

		dst[i] = (prevColor | nextColor) - (((prevColor ^ nextColor) & 0xfefefe) >> 1);
	}

	memcpy(mPALDelayLine, src, sizeof mPALDelayLine);
}

void ATArtifactingEngine::ArtifactPAL32(uint32 *dst, uint32 width) {
	uint8 *dst8 = (uint8 *)dst;
	uint8 *delay8 = mPALDelayLine32;

	for(uint32 i=0; i<width; ++i) {
		// avg = (prev + next)/2
		// result = dot(next, lumaAxis) + avg - dot(avg, lumaAxis)
		//        = avg + dot(next - avg, lumaAxis)
		//        = avg + dot(next - prev/2 - next/2, lumaAxis)
		//        = avg + dot(next - prev, lumaAxis/2)
		int b1 = delay8[0];
		int g1 = delay8[1];
		int r1 = delay8[2];

		int b2 = dst8[0];
		int g2 = dst8[1];
		int r2 = dst8[2];
		delay8[0] = b2;
		delay8[1] = g2;
		delay8[2] = r2;
		delay8 += 3;

		int adj = ((b2 - b1) * 54 + (g2 - g1) * 183 + (b2 - b1) * 19 + 256) >> 9;
		int rf = ((r1 + r2 + 1) >> 1) + adj;
		int gf = ((g1 + g2 + 1) >> 1) + adj;
		int bf = ((b1 + b2 + 1) >> 1) + adj;

		if ((unsigned)rf >= 256)
			rf = (~rf >> 31);

		if ((unsigned)gf >= 256)
			gf = (~gf >> 31);

		if ((unsigned)bf >= 256)
			bf = (~bf >> 31);

		dst8[0] = (uint8)bf;
		dst8[1] = (uint8)gf;
		dst8[2] = (uint8)rf;
		dst8 += 4;
	}
}

void ATArtifactingEngine::ArtifactNTSC(uint32 dst[N], const uint8 src[N], bool scanlineHasHiRes, bool includeHBlank) {
	if (!scanlineHasHiRes) {
		BlitNoArtifacts(dst, src, false);
		return;
	}

#if defined(VD_COMPILER_MSVC) && (defined(VD_CPU_X86) || defined(VD_CPU_X64))
	if (SSE2_enabled) {
		if (!ArtifactNTSC_SSE2(dst, src, N))
			BlitNoArtifacts(dst, src, true);

		return;
	}
#endif

	uint8 luma[N + 4];
	uint8 luma2[N];
	sint8 inv[N];

	for(int i=0; i<N; ++i)
		luma[i+2] = src[i] & 15;

	luma[0] = luma[1] = luma[2];
	luma[N+2] = luma[N+3] = luma[N+1];

	int artsum = 0;
	for(int i=0; i<N; ++i) {
		int y0 = luma[i+1];
		int y1 = luma[i+2];
		int y2 = luma[i+3];

		int d = 0;

		if (y1 < y0 && y1 < y2) {
			if (y0 < y2)
				d = y1 - y0;
			else
				d = y1 - y2;
		} else if (y1 > y0 && y1 > y2) {
			if (y0 > y2)
				d = y1 - y0;
			else
				d = y1 - y2;
		}

		if (i & 1)
			d = -d;

		artsum |= d;

		inv[i] = (sint8)d;

		if (d)
			luma2[i] = (y0 + 2*y1 + y2 + 2) >> 2;
		else
			luma2[i] = y1;
	}

	if (!artsum) {
		BlitNoArtifacts(dst, src, true);
		return;
	}

	// This has no physical basis -- it just looks OK.
	uint32 *dst2 = dst;

	if (mbEnableColorCorrection || mbGammaIdentity || mbBypassOutputCorrection) {
		for(int x=0; x<N; ++x) {
			uint8 p = src[x];
			int art = inv[x]; 

			if (!art) {
				*dst2++ = mPalette[p];
			} else {
				int c = p >> 4;

				int cr = mChromaVectors[c][2];
				int cg = mChromaVectors[c][1];
				int cb = mChromaVectors[c][0];
				int y = mLumaRamp[luma2[x]];

				cr += mArtifactRamp[art+15][2] + y;
				cg += mArtifactRamp[art+15][1] + y;
				cb += mArtifactRamp[art+15][0] + y;

				cr >>= 6;
				cg >>= 6;
				cb >>= 6;

				if (cr < 0)
					cr = 0;
				else if (cr > 255)
					cr = 255;

				if (cg < 0)
					cg = 0;
				else if (cg > 255)
					cg = 255;

				if (cb < 0)
					cb = 0;
				else if (cb > 255)
					cb = 255;

				*dst2++ = cb + (cg << 8) + (cr << 16);
			}
		}

		if (mbEnableColorCorrection && !mbBypassOutputCorrection)
			ColorCorrect((uint8 *)dst, N);
	} else {
		for(int x=0; x<N; ++x) {
			uint8 p = src[x];
			int art = inv[x]; 

			if (!art) {
				*dst2++ = mCorrectedPalette[p];
			} else {
				int c = p >> 4;

				int cr = mChromaVectors[c][2];
				int cg = mChromaVectors[c][1];
				int cb = mChromaVectors[c][0];
				int y = mLumaRamp[luma2[x]];

				cr += mArtifactRamp[art+15][0] + y;
				cg += mArtifactRamp[art+15][1] + y;
				cb += mArtifactRamp[art+15][2] + y;

				cr >>= 6;
				cg >>= 6;
				cb >>= 6;

				if (cr < 0)
					cr = 0;
				else if (cr > 255)
					cr = 255;

				if (cg < 0)
					cg = 0;
				else if (cg > 255)
					cg = 255;

				if (cb < 0)
					cb = 0;
				else if (cb > 255)
					cb = 255;

				cr = mGammaTable[cr];
				cg = mGammaTable[cg];
				cb = mGammaTable[cb];

				*dst2++ = cb + (cg << 8) + (cr << 16);
			}
		}
	}
}

namespace {
	void rotate(float& xr, float& yr, float cs, float sn) {
		float x0 = xr;
		float y0 = yr;

		xr = x0*cs + y0*sn;
		yr = -x0*sn + y0*cs;
	}

	void rotate(float& xr, float& yr, float angle) {
		const float sn = sinf(angle);
		const float cs = cosf(angle);
		float x0 = xr;
		float y0 = yr;

		xr = x0*cs + y0*sn;
		yr = -x0*sn + y0*cs;
	}

	void accum(uint32 *VDRESTRICT dst, const uint32 (*VDRESTRICT table)[2][12], const uint8 *VDRESTRICT src, uint32 count) {
		count >>= 1;

		do {
			const uint8 p0 = *src++;
			const uint8 p1 = *src++;
			const uint32 *VDRESTRICT pr0 = table[p0][0];
			const uint32 *VDRESTRICT pr1 = table[p1][1];

			dst[ 0] += pr0[ 0];
			dst[ 1] += pr0[ 1] + pr1[ 1];
			dst[ 2] += pr0[ 2] + pr1[ 2];
			dst[ 3] += pr0[ 3] + pr1[ 3];
			dst[ 4] += pr0[ 4] + pr1[ 4];
			dst[ 5] += pr0[ 5] + pr1[ 5];	// center
			dst[ 6] += pr0[ 6] + pr1[ 6];
			dst[ 7] += pr0[ 7] + pr1[ 7];
			dst[ 8] += pr0[ 8] + pr1[ 8];
			dst[ 9] += pr0[ 9] + pr1[ 9];
			dst[10] += pr0[10] + pr1[10];
			dst[11] +=           pr1[11];

			dst += 2;
		} while(--count);
	}

	void accum_twin(uint32 *VDRESTRICT dst, const uint32 (*VDRESTRICT table)[12], const uint8 *VDRESTRICT src, uint32 count) {
		count >>= 1;

		do {
			uint8 p = *src;
			src += 2;

			const uint32 *VDRESTRICT pr = table[p];

			dst[ 0] += pr[ 0];
			dst[ 1] += pr[ 1];
			dst[ 2] += pr[ 2];
			dst[ 3] += pr[ 3];
			dst[ 4] += pr[ 4];
			dst[ 5] += pr[ 5];
			dst[ 6] += pr[ 6];
			dst[ 7] += pr[ 7];
			dst[ 8] += pr[ 8];
			dst[ 9] += pr[ 9];
			dst[10] += pr[10];
			dst[11] += pr[11];

			dst += 2;
		} while(--count);
	}

	void final(void *dst, const uint32 *VDRESTRICT srcr, const uint32 *VDRESTRICT srcg, const uint32 *VDRESTRICT srcb, uint32 count) {
		uint8 *VDRESTRICT dst8 = (uint8 *)dst;
		do {
			const uint32 rp = *srcr++;
			const uint32 gp = *srcg++;
			const uint32 bp = *srcb++;
			int r0 = ((int)(rp & 0xffff) - 0x7ff8) >> 4;
			int g0 = ((int)(gp & 0xffff) - 0x7ff8) >> 4;
			int b0 = ((int)(bp & 0xffff) - 0x7ff8) >> 4;
			int r1 = ((int)(rp >> 16) - 0x7ff8) >> 4;
			int g1 = ((int)(gp >> 16) - 0x7ff8) >> 4;
			int b1 = ((int)(bp >> 16) - 0x7ff8) >> 4;

			if (r0 < 0) r0 = 0; else if (r0 > 255) r0 = 255;
			if (g0 < 0) g0 = 0; else if (g0 > 255) g0 = 255;
			if (b0 < 0) b0 = 0; else if (b0 > 255) b0 = 255;
			if (r1 < 0) r1 = 0; else if (r1 > 255) r1 = 255;
			if (g1 < 0) g1 = 0; else if (g1 > 255) g1 = 255;
			if (b1 < 0) b1 = 0; else if (b1 > 255) b1 = 255;

			*dst8++ = (uint8)b0;
			*dst8++ = (uint8)g0;
			*dst8++ = (uint8)r0;
			*dst8++ = 0;
			*dst8++ = (uint8)b1;
			*dst8++ = (uint8)g1;
			*dst8++ = (uint8)r1;
			*dst8++ = 0;
		} while(--count);
	}
}

void ATArtifactColorCorrect_Scalar(uint8 *VDRESTRICT dst8, uint32 N, const sint16 linearTab[256], const uint8 gammaTab[1024], const sint16 matrix16[3][3]) {
	for(uint32 i=0; i<N; ++i) {
		// convert NTSC gamma to linear (2.2)
		sint32 r = linearTab[dst8[2]];
		sint32 g = linearTab[dst8[1]];
		sint32 b = linearTab[dst8[0]];

		// convert to new color space
		sint32 r2
			= r * matrix16[0][0]
			+ g * matrix16[1][0]
			+ b * matrix16[2][0];

		sint32 g2
			= r * matrix16[0][1]
			+ g * matrix16[1][1]
			+ b * matrix16[2][1];

		sint32 b2
			= r * matrix16[0][2]
			+ g * matrix16[1][2]
			+ b * matrix16[2][2];

		r2 >>= 17;
		g2 >>= 17;
		b2 >>= 17;

		if (r2 < 0) r2 = 0;
		if (g2 < 0) g2 = 0;
		if (b2 < 0) b2 = 0;
		if (r2 > 1023) r2 = 1023;
		if (g2 > 1023) g2 = 1023;
		if (b2 > 1023) b2 = 1023;

		dst8[2] = gammaTab[r2];
		dst8[1] = gammaTab[g2];
		dst8[0] = gammaTab[b2];
		dst8 += 4;
	}
}

#if VD_CPU_X86 || VD_CPU_X64
void ATArtifactColorCorrect_SSE2(uint8 *VDRESTRICT dst8, uint32 N, const sint16 linearTab[256], const uint8 gammaTab[1024], const sint16 matrix16[3][3]) {
	__m128i m0 = _mm_loadl_epi64((const __m128i *)matrix16[0]);
	__m128i m1 = _mm_loadl_epi64((const __m128i *)matrix16[1]);
	__m128i m2 = _mm_loadl_epi64((const __m128i *)matrix16[2]);
	__m128i zero = _mm_setzero_si128();
	__m128i limit = _mm_set_epi16(0, 0, 0, 0, 0, 1023, 1023, 1023);

	for(uint32 i=0; i<N; ++i) {
		// convert NTSC gamma to linear (2.2)
		__m128i r = _mm_cvtsi32_si128((uint16)linearTab[dst8[2]]);
		__m128i g = _mm_cvtsi32_si128((uint16)linearTab[dst8[1]]);
		__m128i b = _mm_cvtsi32_si128((uint16)linearTab[dst8[0]]);

		// convert to new color space
		__m128i rgb = _mm_mulhi_epi16(_mm_shufflelo_epi16(r, 0), m0);
		rgb = _mm_add_epi16(rgb, _mm_mulhi_epi16(_mm_shufflelo_epi16(g, 0), m1));
		rgb = _mm_add_epi16(rgb, _mm_mulhi_epi16(_mm_shufflelo_epi16(b, 0), m2));

		__m128i indices = _mm_max_epi16(zero, _mm_min_epi16(limit, _mm_srai_epi16(rgb, 1)));

		dst8[2] = gammaTab[(uint32)_mm_extract_epi16(indices, 0)];
		dst8[1] = gammaTab[(uint32)_mm_extract_epi16(indices, 1)];
		dst8[0] = gammaTab[(uint32)_mm_extract_epi16(indices, 2)];
		dst8 += 4;
	}
}
#endif

void ATArtifactingEngine::ColorCorrect(uint8 *VDRESTRICT dst8, uint32 n) const {
#if VD_CPU_X86 || VD_CPU_X64
	if (SSE2_enabled)
		ATArtifactColorCorrect_SSE2(dst8, n, mCorrectLinearTable, mCorrectGammaTable, mColorMatchingMatrix16);
	else
#endif
		ATArtifactColorCorrect_Scalar(dst8, n, mCorrectLinearTable, mCorrectGammaTable, mColorMatchingMatrix16);
}

void ATArtifactingEngine::ArtifactNTSCHi(uint32 dst[N*2], const uint8 src[N], bool scanlineHasHiRes, bool includeHBlank) {
	// We are using a 21 tap filter, so we're going to need arrays of N*2+20 (since we are
	// transforming 7MHz input to 14MHz output). However, we hold two elements in each int,
	// so we actually only need N+10 elements, which we round up to N+16. We need 8-byte
	// alignment for MMX.
	VDALIGN(16) uint32 rout[N+16];
	VDALIGN(16) uint32 gout[N+16];
	VDALIGN(16) uint32 bout[N+16];

#if defined(VD_CPU_ARM64)
	if (scanlineHasHiRes) {
		ATArtifactNTSCAccum_NEON(rout+2, m4x.mPalToR, src, N);
		ATArtifactNTSCAccum_NEON(gout+2, m4x.mPalToG, src, N);
		ATArtifactNTSCAccum_NEON(bout+2, m4x.mPalToB, src, N);
	} else {
		ATArtifactNTSCAccumTwin_NEON(rout+2, m4x.mPalToRTwin, src, N);
		ATArtifactNTSCAccumTwin_NEON(gout+2, m4x.mPalToGTwin, src, N);
		ATArtifactNTSCAccumTwin_NEON(bout+2, m4x.mPalToBTwin, src, N);
	}
#elif defined(VD_CPU_AMD64)
	if (scanlineHasHiRes) {
		ATArtifactNTSCAccum_SSE2(rout+2, m4x.mPalToR, src, N);
		ATArtifactNTSCAccum_SSE2(gout+2, m4x.mPalToG, src, N);
		ATArtifactNTSCAccum_SSE2(bout+2, m4x.mPalToB, src, N);
	} else {
		ATArtifactNTSCAccumTwin_SSE2(rout+2, m4x.mPalToRTwin, src, N);
		ATArtifactNTSCAccumTwin_SSE2(gout+2, m4x.mPalToGTwin, src, N);
		ATArtifactNTSCAccumTwin_SSE2(bout+2, m4x.mPalToBTwin, src, N);
	}
#else

#if defined(VD_COMPILER_MSVC) && defined(VD_CPU_X86)
	if (SSE2_enabled) {
		if (scanlineHasHiRes) {
			ATArtifactNTSCAccum_SSE2(rout+2, m4x.mPalToR, src, N);
			ATArtifactNTSCAccum_SSE2(gout+2, m4x.mPalToG, src, N);
			ATArtifactNTSCAccum_SSE2(bout+2, m4x.mPalToB, src, N);
		} else {
			ATArtifactNTSCAccumTwin_SSE2(rout+2, m4x.mPalToRTwin, src, N);
			ATArtifactNTSCAccumTwin_SSE2(gout+2, m4x.mPalToGTwin, src, N);
			ATArtifactNTSCAccumTwin_SSE2(bout+2, m4x.mPalToBTwin, src, N);
		}
	} else
#endif
	{
		for(int i=0; i<N+16; ++i)
			rout[i] = 0x80008000;

		if (scanlineHasHiRes)
			accum(rout, m2x.mPalToR, src, N);
		else
			accum_twin(rout, m2x.mPalToRTwin, src, N);

		for(int i=0; i<N+16; ++i)
			gout[i] = 0x80008000;

		if (scanlineHasHiRes)
			accum(gout, m2x.mPalToG, src, N);
		else
			accum_twin(gout, m2x.mPalToGTwin, src, N);

		for(int i=0; i<N+16; ++i)
			bout[i] = 0x80008000;

		if (scanlineHasHiRes)
			accum(bout, m2x.mPalToB, src, N);
		else
			accum_twin(bout, m2x.mPalToBTwin, src, N);
	}
#endif

	// downconvert+interleave RGB channels and do post-processing
	const int xdfinal = includeHBlank ? 0 : kLeftBorder14MHz_16;
	const int xfinal = includeHBlank ? 0 : kLeftBorder7MHz_8/2;
	const int nfinal = includeHBlank ? N : ((kRightBorder7MHz - kLeftBorder7MHz_8) + 7) & ~7;

#if VD_CPU_ARM64
	ATArtifactNTSCFinal_NEON(dst + xdfinal, rout+4+xfinal, gout+4+xfinal, bout+4+xfinal, nfinal);
#else
#if VD_CPU_X86 || VD_CPU_X64
	if (SSE2_enabled)
		ATArtifactNTSCFinal_SSE2(dst + xdfinal, rout+4+xfinal, gout+4+xfinal, bout+4+xfinal, nfinal);
	else
#endif
		final(dst + xdfinal, rout+4+xfinal, gout+4+xfinal, bout+4+xfinal, nfinal);
#endif

	if (!mbBypassOutputCorrection) {
		const int xpost = includeHBlank ? 0 : kLeftBorder14MHz;
		const int npost = includeHBlank ? N*2 : kRightBorder14MHz - kLeftBorder14MHz;

		if (mbEnableColorCorrection)
			ColorCorrect((uint8 *)(dst + xpost), npost);
		else if (!mbGammaIdentity)
			GammaCorrect((uint8 *)(dst + xpost), npost, mGammaTable);
	}
}

void ATArtifactingEngine::ArtifactPALHi(uint32 dst[N*2], const uint8 src[N], bool scanlineHasHiRes, bool oddLine) {
	// encode to YUV
	VDALIGN(16) uint32 ybuf[32 + N];
	VDALIGN(16) uint32 ubuf[32 + N];
	VDALIGN(16) uint32 vbuf[32 + N];

	uint32 *const ulbuf = mPALDelayLineUV[0];
	uint32 *const vlbuf = mPALDelayLineUV[1];

#if defined(VD_CPU_X86) || defined(VD_CPU_AMD64)
	if (SSE2_enabled) {
		if (scanlineHasHiRes) {
			ATArtifactPALLuma_SSE2(ybuf, src, N, &mPal8x.mPalToY[oddLine][0][0][0]);

			ATArtifactPALChroma_SSE2(ubuf, src, N, &mPal8x.mPalToU[oddLine][0][0][0]);
			ATArtifactPALChroma_SSE2(vbuf, src, N, &mPal8x.mPalToV[oddLine][0][0][0]);
		} else {
			ATArtifactPALLumaTwin_SSE2(ybuf, src, N, &mPal8x.mPalToYTwin[oddLine][0][0][0]);

			ATArtifactPALChromaTwin_SSE2(ubuf, src, N, &mPal8x.mPalToUTwin[oddLine][0][0][0]);
			ATArtifactPALChromaTwin_SSE2(vbuf, src, N, &mPal8x.mPalToVTwin[oddLine][0][0][0]);
		}

		ATArtifactPALFinal_SSE2(dst, ybuf, ubuf, vbuf, ulbuf, vlbuf, N);
	} else 
#endif
	{
		VDMemset32(ubuf, 0x20002000, sizeof(ubuf)/sizeof(ubuf[0]));
		VDMemset32(vbuf, 0x20002000, sizeof(vbuf)/sizeof(vbuf[0]));

		ATArtifactPALLuma(ybuf, src, N, &mPal2x.mPalToY[oddLine][0][0][0]);
		ATArtifactPALChroma(ubuf, src, N, &mPal2x.mPalToU[oddLine][0][0][0]);
		ATArtifactPALChroma(vbuf, src, N, &mPal2x.mPalToV[oddLine][0][0][0]);
		ATArtifactPALFinal(dst, ybuf, ubuf, vbuf, ulbuf, vlbuf, N);
	}

	if (!mbBypassOutputCorrection) {
		if (mbEnableColorCorrection)
			ColorCorrect((uint8 *)dst, N*2);
		else if (!mbGammaIdentity)
			GammaCorrect((uint8 *)dst, N*2, mGammaTable);
	}
}

void ATArtifactingEngine::BlitNoArtifacts(uint32 dst[N], const uint8 src[N], bool scanlineHasHiRes) {
	static_assert((N & 1) == 0);

	const uint32 *VDRESTRICT palette = mbBypassOutputCorrection ? mPalette : mCorrectedPalette;

	if (scanlineHasHiRes) {
		for(size_t x=0; x<N; ++x)
			dst[x] = palette[src[x]];
	} else {
		for(size_t x=0; x<N; x += 2) {
			const uint32 c = palette[src[x]];
			dst[x+0] = c;
			dst[x+1] = c;
		}
	}
}

void ATArtifactingEngine::Blend(uint32 *VDRESTRICT dst, const uint32 *VDRESTRICT src, uint32 n) {
#if VD_CPU_ARM64
	if (!(n & 3)) {
		ATArtifactBlend_NEON(dst, src, n);
		return;
	}
#endif
#if VD_CPU_X86 || VD_CPU_X64
	if (SSE2_enabled && !(((uintptr)dst | (uintptr)src) & 15) && !(n & 3)) {
		ATArtifactBlend_SSE2(dst, src, n);
		return;
	}
#endif

	for(uint32 x=0; x<n; ++x) {
		const uint32 a = dst[x];
		const uint32 b = src[x];

		dst[x] = (a|b) - (((a^b) >> 1) & 0x7f7f7f7f);
	}
}

void ATArtifactingEngine::BlendExchange(uint32 *VDRESTRICT dst, uint32 *VDRESTRICT blendDst, uint32 n) {
#if VD_CPU_ARM64
	if (!(n & 3)) {
		ATArtifactBlendExchange_NEON(dst, blendDst, n);
		return;
	}
#endif
#if VD_CPU_X86 || VD_CPU_X64
	if (SSE2_enabled && !(((uintptr)dst | (uintptr)blendDst) & 15) && !(n & 3)) {
		ATArtifactBlendExchange_SSE2(dst, blendDst, n);
		return;
	}
#endif

	for(uint32 x=0; x<n; ++x) {
		const uint32 a = dst[x];
		const uint32 b = blendDst[x];

		blendDst[x] = a;

		dst[x] = (a|b) - (((a^b) >> 1) & 0x7f7f7f7f);
	}
}

namespace {
	struct BiquadFilter {
		float b0;
		float b1;
		float b2;
		float a1;
		float a2;

		void Filter(float *p, size_t n) {
			float x1 = 0;
			float x2 = 0;
			float y1 = 0;
			float y2 = 0;

			while(n--) {
				const float x0 = *p;
				const float y0 = x0*b0 + x1*b1 + x2*b2 - y1*a1 - y2*a2;

				*p++ = y0;
				y2 = y1;
				y1 = y0;
				x2 = x1;
				x1 = x0;
			}
		}
	};

	struct BiquadLPF : public BiquadFilter {
		BiquadLPF(float fc, float Q) {
			const float w0 = nsVDMath::kfTwoPi * fc;
			const float cos_w0 = cosf(w0);
			const float alpha = sinf(w0) / (2*Q);

			const float inv_a0 = 1.0f / (1 + alpha);
			
			a2 = (1 - alpha) * inv_a0;
			a1 = (-2*cos_w0) * inv_a0;
			b0 = (0.5f - 0.5f*cos_w0) * inv_a0;
			b1 = (1 - cos_w0) * inv_a0;
			b2 = b0;
		}
	};

	struct BiquadBPF : public BiquadFilter {
		BiquadBPF(float fc, float Q) {
			const float w0 = nsVDMath::kfTwoPi * fc;
			const float cos_w0 = cosf(w0);
			const float alpha = sinf(w0) / (2*Q);

			const float inv_a0 = 1.0f / (1 + alpha);
			
			a1 = (-2*cos_w0) * inv_a0;
			a2 = (1 - alpha) * inv_a0;
			b0 = alpha * inv_a0;
			b1 = 0;
			b2 = -b0;
		}
	};

	struct BiquadPeak : public BiquadFilter {
		BiquadPeak(float fc, float Q, float dbGain) {
			const float A = sqrtf(powf(10.0f, dbGain / 40.0f));
			const float w0 = nsVDMath::kfTwoPi * fc;
			const float cos_w0 = cosf(w0);
			const float alpha = sinf(w0) / (2*Q);

			const float inv_a0 = 1.0f / (1 + alpha/A);
			
			a1 = (-2*cos_w0) * inv_a0;
			a2 = (1 - alpha/A) * inv_a0;
			b0 = (1 + alpha*A) * inv_a0;
			b1 = (-2*cos_w0) * inv_a0;
			b2 = (1 - alpha*A) * inv_a0;
		}
	};

	struct BiquadNotch : public BiquadFilter {
		BiquadNotch(float fc, float Q) {
			const float w0 = nsVDMath::kfTwoPi * fc;
			const float cos_w0 = cosf(w0);
			const float alpha = sinf(w0) / (2*Q);

			const float inv_a0 = 1.0f / (1 + alpha);
			
            a1 = (-2*cos_w0) * inv_a0;
            a2 = (1 - alpha) * inv_a0;
            b0 = inv_a0;
            b1 = a1;
            b2 = inv_a0;
		}
	};
}

void ATArtifactingEngine::RecomputeNTSCTables(const ATColorParams& params) {
	mbHighNTSCTablesInited = true;
	mbHighPALTablesInited = false;

	vdfloat3 y_to_rgb[16][2][22] {};
	vdfloat3 chroma_to_rgb[16][2][22] {};

	// NTSC signal parameters:
	//
	// Decoding matrix we use:
	//
	//	R-Y =  0.956*I + 0.620*Q
	//	G-Y = -0.272*I - 0.647*Q
	//	B-Y = -1.108*I + 1.705*Q
	//
	// Blank is at 0 IRE, black at 7.5 IRE, white at 100 IRE.
	// Color burst signal is +/-20 IRE.
	//
	// For 100% bars:
	//	white		100 IRE
	//	yellow		89.4 +/- 41.4 IRE (chroma 82.7 IRQ p-p)
	//	cyan		72.3 +/- 58.5 IRE
	//	green		61.8 +/- 54.7 IRE
	//	magenta		45.7 +/- 54.7 IRE
	//	red			35.1 +/- 58.5 IRE
	//	blue		18.0 +/- 41.4 IRE
	//
	// {Matlab/Scilab solver: YIQ = inv([1 0.956 0.620; 1 -0.272 -0.647; 1 -1.108 1.705])*[R G B]'; disp(YIQ); disp(YIQ(1)*92.5+7.5); disp(norm(YIQ(2:3))*100*0.925)}
	//
	// Note that the chrominance signal amplitude is also reduced when
	// adjusting for the 7.5 IRE pedestal.
	//
	// However, since the computer outputs the same amplitude for the colorburst as
	// for regular color, the saturation is reduced. Take 100% yellow, which
	// has a IQ amplitude of 0.447 raw and 41.4 IRE; the computer would produce
	// the equivalent of 20 IRE after the color AGC kicked in, giving an equivalent
	// raw IQ amplitude of 0.447 * 20 / 41.4 = 21.6%. In theory this is invariant
	// to the actual chrominance signal strength due to the color AGC.
	//
	// Eyeballed scope traces show the chroma amplitude to be about 30% of full luma
	// amplitude.
	//
	//
	float chromaSignalAmplitude = 0.5f / std::max<float>(0.10f, params.mArtifactSat);
	float chromaSignalInvAmplitude = params.mArtifactSat * 2.0f;

	float phadjust = -params.mArtifactHue * (nsVDMath::kfTwoPi / 360.0f) + nsVDMath::kfPi * 1.25f;

	float cp = cosf(phadjust);
	float sp = sinf(phadjust);

	float co_ir = 0.956f;
	float co_qr = 0.620f;
	float co_ig = -0.272f;
	float co_qg = -0.647f;
	float co_ib = -1.108f;
	float co_qb = 1.705f;
	rotate(co_ir, co_qr, -mColorParams.mRedShift * (nsVDMath::kfPi / 180.0f));
	rotate(co_ig, co_qg, -mColorParams.mGrnShift * (nsVDMath::kfPi / 180.0f));
	rotate(co_ib, co_qb, -mColorParams.mBluShift * (nsVDMath::kfPi / 180.0f));
	co_ir *= mColorParams.mRedScale;
	co_qr *= mColorParams.mRedScale;
	co_ig *= mColorParams.mGrnScale;
	co_qg *= mColorParams.mGrnScale;
	co_ib *= mColorParams.mBluScale;
	co_qb *= mColorParams.mBluScale;

	rotate(co_ir, co_qr, cp, -sp);
	rotate(co_ig, co_qg, cp, -sp);
	rotate(co_ib, co_qb, cp, -sp);

	const float saturationScale = params.mSaturation * 2;

	const vdfloat3 co_i = vdfloat3 { co_ir, co_ig, co_ib };
	const vdfloat3 co_q = vdfloat3 { co_qr, co_qg, co_qb };

	auto decodeChromaRGB = [=](float i, float q) {
		return i*co_i + q*co_q;
	};

	float lumaRamp[16];
	ATComputeLumaRamp(params.mLumaRampMode, lumaRamp);

	// chroma processing
	for(int i=0; i<15; ++i) {
		float chromatab[4];
		float phase = phadjust + nsVDMath::kfTwoPi * ((params.mHueStart / 360.0f) + (float)i / 15.0f * (params.mHueRange / 360.0f));

		// create chroma signal
		for(int j=0; j<4; ++j) {
			float v = sinf(phase + (0.25f * nsVDMath::kfTwoPi * j));

			chromatab[j] = v;
		}

		float c0 = chromatab[0];
		float c1 = chromatab[1];
		float c2 = chromatab[2];
		float c3 = chromatab[3];

		float ytab[22] = {0};
		float itab[22] = {0};
		float qtab[22] = {0};

		ytab[ 7-1] = 0;
		ytab[ 8-1] = (              1*c2 + 2*c3) * (1.0f / 16.0f);
		ytab[ 9-1] = (1*c0 + 2*c1 + 1*c2 + 0*c3) * (1.0f / 16.0f);
		ytab[10-1] = (1*c0 + 0*c1 + 2*c2 + 4*c3) * (1.0f / 16.0f);
		ytab[11-1] = (2*c0 + 4*c1 + 2*c2 + 0*c3) * (1.0f / 16.0f);
		ytab[12-1] = (2*c0 + 0*c1 + 1*c2 + 2*c3) * (1.0f / 16.0f);
		ytab[13-1] = (1*c0 + 2*c1 + 1*c2       ) * (1.0f / 16.0f);
		ytab[14-1] = (1*c0                     ) * (1.0f / 16.0f);

		// multiply chroma signal by pixel pulse
		const float chromaSharp = 0.50f;
		float t[28] = {0};
		t[11-5] = c3 * ((1.0f - chromaSharp) / 3.0f);
		t[12-5] = c0 * ((2.0f + chromaSharp) / 3.0f);
		t[13-5] = c1;
		t[14-5] = c2;
		t[15-5] = c3 * ((2.0f + chromaSharp) / 3.0f);
		t[16-5] = c0 * ((1.0f - chromaSharp) / 3.0f);

		// demodulate chroma axes by multiplying by sin/cos
		//	t[0] = +I
		//	t[1] = -Q
		//	t[2] = -I
		//	t[3] = +Q
		//
		for(int j=0; j<26; ++j) {
			if ((j+1) & 2)
				t[j] = -t[j];
		}

		// apply low-pass filter to chroma
		float u[28] = {0};

		for(int j=8; j<28; ++j) {
			u[j] = (  1 * t[j- 6])
				 + (  0.9732320952f * t[j- 4])
				 + (  0.9732320952f * t[j- 2])
				 + (  1 * t[j])
				 + (  0.1278410428f * u[j- 2]);
		}

		// compensate for gain from pixel shape filter (4x) and low-pass filter (~4.5x)
		for(float& y : u)
			y = y / 4 / ((2+0.9732320952f*2) / (1 - 0.1278410428f));

		// interpolate chroma
		for(int j=0; j<22; ++j) {
			if (!(j & 1)) {
				itab[j] = (u[j+2] + u[j+4])*0.625f - (u[j] + u[j+6])*0.125f;
				qtab[j] = u[j+3];
			} else {
				itab[j] = u[j+3];
				qtab[j] = (u[j+2] + u[j+4])*0.625f - (u[j] + u[j+6])*0.125f;
			}
		}

		vdfloat3 rgbtab[2][22];

		for(int j=0; j<22; ++j) {
			float fy = ytab[j] * chromaSignalAmplitude;
			float fi = itab[j];
			float fq = qtab[j];

			vdfloat3 fc = (fi*co_i + fq*co_q) * saturationScale;
			vdfloat3 f0 = fc - fy;
			vdfloat3 f1 = fc + fy;

			rgbtab[0][j] = f0;
			rgbtab[1][j] = f1;
		}

		for(int k=0; k<2; ++k) {
			for(int j=0; j<2; ++j) {
				rgbtab[k][j+14] += rgbtab[k][j+18];
				rgbtab[k][j+18] = { 0, 0, 0 };
			}

			for(int j=0; j<22; ++j)
				chroma_to_rgb[i+1][k][j] = rgbtab[k][j];
		}
	}

	////////////////////////// 28MHz SECTION //////////////////////////////

	const float lumaSharpness = params.mArtifactSharpness;
	float lumapulse[16] = {
		(1.0f - lumaSharpness) / 3.0f,
		(2.0f + lumaSharpness) / 3.0f,
		(2.0f + lumaSharpness) / 3.0f,
		(1.0f - lumaSharpness) / 3.0f,
	};

	for(int i=0; i<16; ++i) {
		float y = lumaRamp[i] * params.mContrast + params.mBrightness;

		float t[30] = {0};
		t[11] = y*((1.0f - 1.0f)/3.0f);
		t[12] = y*((2.0f + 1.0f)/3.0f);
		t[13] = y*((2.0f + 1.0f)/3.0f);
		t[14] = y*((1.0f - 1.0f)/3.0f);

		for(int j=0; j<30; ++j) {
			if (!(j & 2))
				t[j] = -t[j];
		}

		float u[28] = {0};

		for(int j=4; j<20; ++j) {
			u[j] = (t[j-4] * 0.25f + t[j-2]*0.625f + t[j]*0.75f + t[j+2]*0.625f + t[j+4]*0.25f) / 10.0f;
		}

		float ytab[22] = {0};
		float itab[22] = {0};
		float qtab[22] = {0};

		for(int j=0; j<22; ++j) {
			if ((j & 1)) {
				itab[j] = (u[j+2] + u[j+4]) * 0.575f - (u[j] + u[j+6]) * 0.065f;
				qtab[j] = u[j+3];
			} else {
				itab[j] = u[j+3];
				qtab[j] = (u[j+2] + u[j+4]) * 0.575f - (u[j] + u[j+6]) * 0.065f;
			}
		}

		// Form luma pulse (14MHz)
		for(int j=0; j<11; ++j)
			ytab[7+j] = y * lumapulse[j];

		// subtract chroma signal from luma
		const float antiChromaScale = 1.3333333f + lumaSharpness * 2.666666f;
		for(int j=0; j<22; ++j) {
			float cs = cosf((0.25f * nsVDMath::kfTwoPi) * (j+2));
			float sn = sinf((0.25f * nsVDMath::kfTwoPi) * (j+2));

			ytab[j] -= (cs*itab[j] + sn*qtab[j]) * antiChromaScale;
		}

		vdfloat3 rgbtab[2][22];

		for(int j=0; j<22; ++j) {
			float fy = ytab[j];
			float fi = itab[j];
			float fq = qtab[j];

			vdfloat3 fc = (fi*co_i + fq*co_q) * chromaSignalInvAmplitude;
			vdfloat3 f0 = fy + fc;
			vdfloat3 f1 = fy - fc;

			rgbtab[0][j] = f0 * params.mIntensityScale;
			rgbtab[1][j] = f1 * params.mIntensityScale;
		}

		for(int k=0; k<2; ++k) {
			for(int j=0; j<4; ++j) {
				rgbtab[k][j+14] += rgbtab[k][j+18];
				rgbtab[k][j+18] = { 0, 0, 0 };
			}

			for(int j=0; j<22; ++j)
				y_to_rgb[i][k][j] = rgbtab[k][j];
		}
	}


	// At this point we have all possible luma and chroma kernels computed. Add the luma and chroma
	// kernels together to produce all 256 color kernels in both phases. We then need to produce a
	// few variants:
	//
	//	- For SSE2, double the phases to 4 and add 2/4/6 output pixel delays.
	//	- Create a set of 'twin' kernels that correspond to paired pixels with the same color.
	//	  This is used to accelerate 160 resolution graphics.
	//	- For SSE2, create another set of 'quad' kernels that correspond to paired matching color
	//	  clocks. This is used to accelerate solid bands of color.

#if defined(VD_CPU_AMD64) || defined(VD_CPU_ARM64)
	if (true) {
#else
	if (SSE2_enabled) {
#endif
		memset(&m4x, 0, sizeof m4x);

		const vdfloat3 round { 8.0f, 8.0f, 8.0f };

		for(int idx=0; idx<256; ++idx) {
			int cidx = idx >> 4;
			int lidx = idx & 15;

			const auto& c_waves = chroma_to_rgb[cidx];
			const auto& y_waves = y_to_rgb[lidx];

			for(int k=0; k<4; ++k) {
				for(int i=0; i<11; ++i) {
					vdfloat3 pal_to_rgb0 = (c_waves[k&1][i*2+0] + y_waves[k&1][i*2+0]) * 16.0f * 255.0f;
					vdfloat3 pal_to_rgb1 = (c_waves[k&1][i*2+1] + y_waves[k&1][i*2+1]) * 16.0f * 255.0f;

					if (k == 0 && i < 4) {
						pal_to_rgb0 += round;
						pal_to_rgb1 += round;
					}

					m4x.mPalToR[idx][k][i+k] = (VDRoundToInt32(pal_to_rgb0.x) & 0xffff) + (VDRoundToInt32(pal_to_rgb1.x) << 16);
					m4x.mPalToG[idx][k][i+k] = (VDRoundToInt32(pal_to_rgb0.y) & 0xffff) + (VDRoundToInt32(pal_to_rgb1.y) << 16);
					m4x.mPalToB[idx][k][i+k] = (VDRoundToInt32(pal_to_rgb0.z) & 0xffff) + (VDRoundToInt32(pal_to_rgb1.z) << 16);
				}
			}

			for(int i=0; i<16; ++i) {
				m4x.mPalToRTwin[idx][0][i] = m4x.mPalToR[idx][0][i] + m4x.mPalToR[idx][1][i];
				m4x.mPalToGTwin[idx][0][i] = m4x.mPalToG[idx][0][i] + m4x.mPalToG[idx][1][i];
				m4x.mPalToBTwin[idx][0][i] = m4x.mPalToB[idx][0][i] + m4x.mPalToB[idx][1][i];
			}

			for(int i=0; i<16; ++i) {
				m4x.mPalToRTwin[idx][1][i] = m4x.mPalToR[idx][2][i] + m4x.mPalToR[idx][3][i];
				m4x.mPalToGTwin[idx][1][i] = m4x.mPalToG[idx][2][i] + m4x.mPalToG[idx][3][i];
				m4x.mPalToBTwin[idx][1][i] = m4x.mPalToB[idx][2][i] + m4x.mPalToB[idx][3][i];
			}

			for(int i=0; i<16; ++i) {
				m4x.mPalToRQuad[idx][i] = m4x.mPalToRTwin[idx][0][i] + m4x.mPalToRTwin[idx][1][i];
				m4x.mPalToGQuad[idx][i] = m4x.mPalToGTwin[idx][0][i] + m4x.mPalToGTwin[idx][1][i];
				m4x.mPalToBQuad[idx][i] = m4x.mPalToBTwin[idx][0][i] + m4x.mPalToBTwin[idx][1][i];
			}
		}
	} else {
		memset(&m2x, 0, sizeof m2x);

		for(int idx=0; idx<256; ++idx) {
			int cidx = idx >> 4;
			int lidx = idx & 15;

			for(int k=0; k<2; ++k) {
				for(int i=0; i<10; ++i) {
					vdfloat3 pal_to_rgb0 = (chroma_to_rgb[cidx][k][i*2+0] + y_to_rgb[lidx][k][i*2+0]) * 16.0f * 255.0f;
					vdfloat3 pal_to_rgb1 = (chroma_to_rgb[cidx][k][i*2+1] + y_to_rgb[lidx][k][i*2+1]) * 16.0f * 255.0f;

					m2x.mPalToR[idx][k][i+k] = VDRoundToInt32(pal_to_rgb0.x) + (VDRoundToInt32(pal_to_rgb1.x) << 16);
					m2x.mPalToG[idx][k][i+k] = VDRoundToInt32(pal_to_rgb0.y) + (VDRoundToInt32(pal_to_rgb1.y) << 16);
					m2x.mPalToB[idx][k][i+k] = VDRoundToInt32(pal_to_rgb0.z) + (VDRoundToInt32(pal_to_rgb1.z) << 16);
				}
			}

			for(int i=0; i<12; ++i) {
				m2x.mPalToRTwin[idx][i] = m2x.mPalToR[idx][0][i] + m2x.mPalToR[idx][1][i];
				m2x.mPalToGTwin[idx][i] = m2x.mPalToG[idx][0][i] + m2x.mPalToG[idx][1][i];
				m2x.mPalToBTwin[idx][i] = m2x.mPalToB[idx][0][i] + m2x.mPalToB[idx][1][i];
			}
		}
	}
}

void ATArtifactingEngine::RecomputePALTables(const ATColorParams& params) {
	mbHighNTSCTablesInited = false;
	mbHighPALTablesInited = true;

	float lumaRamp[16];
	ATComputeLumaRamp(params.mLumaRampMode, lumaRamp);

	// The PAL color subcarrier is about 25% faster than the NTSC subcarrier. This
	// means that a hi-res pixel covers 5/8ths of a color cycle instead of half, which
	// is a lot less convenient than the NTSC case.
	//
	// Our process looks something like this:
	//
	//	Low-pass C to 4.43MHz (1xFsc / 0.6xHR).
	//	QAM encode C at 35.54MHz (8xFsc / 5xHR).
	//	Add Y to C to produce composite signal S.
	//	Bandlimit S to around 4.43MHz (0.125f) to produce C'.
	//	Sample every 4 pixels to extract U/V (or I/Q).
	//	Low-pass S to ~3.5MHz to produce Y'.
	//
	// All of the above is baked into a series of filter kernels that run at 1.66xFsc/1xHR.
	// This avoids the high cost of actually synthesizing an 8xFsc signal.

	const float sat2 = mColorParams.mArtifactSat;
	const float sat1 = mColorParams.mSaturation / std::max<float>(0.001f, sat2);

	const float chromaPhaseStep = -mColorParams.mHueRange * (nsVDMath::kfTwoPi / (360.0f * 15.0f));

	// UV<->RGB chroma coefficients.
	const float co_vr = 1.1402509f;
	const float co_vg = -0.5808092f;
	const float co_ug = -0.3947314f;
	const float co_ub = 2.0325203f;

	float utab[2][16];
	float vtab[2][16];
	float ytab[16];

	static const float kPALPhaseLookup[][4]={
		{  2.0f,  1, -2.0f,  1 },
		{  3.0f,  1, -3.0f,  1 },
		{ -4.0f, -1, -4.0f,  1 },
		{ -3.0f, -1,  3.0f, -1 },
		{ -2.0f, -1,  2.0f, -1 },
		{ -1.0f, -1,  1.0f, -1 },
		{  1.0f, -1, -1.0f, -1 },
		{  2.0f, -1, -2.0f, -1 },
		{  3.0f, -1, -3.0f, -1 },
		{ -4.0f,  1, -4.0f, -1 },
		{ -2.0f,  1,  2.0f,  1 },
		{ -1.0f,  1,  1.0f,  1 },
		{  0.0f,  1,  0.0f,  1 },
		{  1.0f,  1, -1.0f,  1 },
		{  2.0f,  1, -2.0f,  1 },
	};

	utab[0][0] = 0;
	utab[1][0] = 0;
	vtab[0][0] = 0;
	vtab[1][0] = 0;

	for(int i=0; i<15; ++i) {
		const float *src = kPALPhaseLookup[i];
		float t1 = src[0] * chromaPhaseStep;
		float t2 = src[2] * chromaPhaseStep;

		utab[0][i+1] = cosf(t1)*src[1];
		vtab[0][i+1] = -sinf(t1)*src[1];
		utab[1][i+1] = cosf(t2)*src[3];
		vtab[1][i+1] = -sinf(t2)*src[3];
	}

	for(int i=0; i<16; ++i) {
		ytab[i] = mColorParams.mBrightness + mColorParams.mContrast * lumaRamp[i];
	}

	ATFilterKernel kernbase;
	ATFilterKernel kerncfilt;
	ATFilterKernel kernumod;
	ATFilterKernel kernvmod;

	// Box filter representing pixel time.
	kernbase.Init(0) = 1, 1, 1, 1, 1;

	kernbase *= params.mIntensityScale;

	// Chroma low-pass filter. We apply this before encoding to avoid excessive bleeding
	// into luma.
	kerncfilt.Init(-5) = 
		  1.0f / 1024.0f,
		 10.0f / 1024.0f,
		 45.0f / 1024.0f,
		120.0f / 1024.0f,
		210.0f / 1024.0f,
		252.0f / 1024.0f,
		210.0f / 1024.0f,
		120.0f / 1024.0f,
		 45.0f / 1024.0f,
		 10.0f / 1024.0f,
		  1.0f / 1024.0f;

	// Modulation filters -- sine and cosine of color subcarrier. We also apply chroma
	// amplitude adjustment here.
	const float ivrt2 = 0.70710678118654752440084436210485f;
	kernumod.Init(0) = 1, ivrt2, 0, -ivrt2, -1, -ivrt2, 0, ivrt2;
	kernumod *= sat1;
	kernvmod.Init(0) = 0, ivrt2, 1, ivrt2, 0, -ivrt2, -1, -ivrt2;
	kernvmod *= sat1;

	ATFilterKernel kernysep;
	ATFilterKernel kerncsep;
	ATFilterKernel kerncdemod;

	// Luma separation filter -- just a box filter.
	kernysep.Init(-4) =
		0.5f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		1.0f / 8.0f,
		0.5f / 8.0f;

	// Chroma separation filter -- dot with peaks of sine/cosine waves and apply box filter.
	kerncsep.Init(-16) = 1,0,0,0,-2,0,0,0,2,0,0,0,-2,0,0,0,2,0,0,0,-2,0,0,0,2,0,0,0,-2,0,0,0,1;
	kerncsep *= 1.0f / 16.0f;

	// Demodulation filter. Here we invert every other sample of extracted U and V.
	kerncdemod.Init(0) =
		 -1,
		 -1,
		 1,
		 1,
		 1,
		 1,
		 -1,
		 -1;

	kerncdemod *= sat2 * 0.5f;	// 0.5 is for chroma line averaging

	memset(&mPal8x, 0, sizeof mPal8x);

	const float ycphase = mColorParams.mArtifactHue * nsVDMath::kfPi / 180.0f;
	const float ycphasec = cosf(ycphase);
	const float ycphases = sinf(ycphase);

	for(int i=0; i<8; ++i) {
		const float yphase = 0;
		const float cphase = 0;

		ATFilterKernel kernbase2 = kernbase >> (5*i);
		ATFilterKernel kernsignaly = kernbase2;

		// downsample chroma and modulate
		ATFilterKernel kernsignalu = (kernbase2 * kerncfilt) ^ kernumod;
		ATFilterKernel kernsignalv = (kernbase2 * kerncfilt) ^ kernvmod;

		// extract Y via low pass filter
		ATFilterKernel kerny2y = ATFilterKernelSampleBicubic(kernsignaly * kernysep, yphase, 2.5f, -0.75f);
		ATFilterKernel kernu2y = ATFilterKernelSampleBicubic(kernsignalu * kernysep, yphase, 2.5f, -0.75f);
		ATFilterKernel kernv2y = ATFilterKernelSampleBicubic(kernsignalv * kernysep, yphase, 2.5f, -0.75f);

		// separate, low pass filter and demodulate chroma
		ATFilterKernel kerny2u = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignaly * kerncsep) ^ kerncdemod, 0, 4), cphase     , 0.625f, -0.75f);
		ATFilterKernel kernu2u = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignalu * kerncsep) ^ kerncdemod, 0, 4), cphase     , 0.625f, -0.75f);
		ATFilterKernel kernv2u = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignalv * kerncsep) ^ kerncdemod, 0, 4), cphase     , 0.625f, -0.75f);

		ATFilterKernel kerny2v = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignaly * kerncsep) ^ kerncdemod, 2, 4), cphase-0.5f, 0.625f, -0.75f);
		ATFilterKernel kernu2v = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignalu * kerncsep) ^ kerncdemod, 2, 4), cphase-0.5f, 0.625f, -0.75f);
		ATFilterKernel kernv2v = ATFilterKernelSampleBicubic(ATFilterKernelSamplePoint((kernsignalv * kerncsep) ^ kerncdemod, 2, 4), cphase-0.5f, 0.625f, -0.75f);

		ATFilterKernel u = std::move(kerny2u);
		ATFilterKernel v = std::move(kerny2v);
		kerny2u = u * ycphasec - v * ycphases;
		kerny2v = u * ycphases + v * ycphasec;

		for(int k=0; k<2; ++k) {
			float v_invert = k ? -1.0f : 1.0f;

			for(int j=0; j<256; ++j) {
				float u = utab[k][j >> 4];
				float v = vtab[k][j >> 4];
				float y = ytab[j & 15];

				float p2yw[8 + 8] = {0};
				float p2uw[24 + 8] = {0};
				float p2vw[24 + 8] = {0};

				if (SSE2_enabled) {
					int ypos = 3 - (i & 4)*2;
					int cpos = 12 - (i & 4)*2;

					ATFilterKernelAccumulateWindow(kerny2y, p2yw, ypos, 16, y);
					ATFilterKernelAccumulateWindow(kernu2y, p2yw, ypos, 16, u);
					ATFilterKernelAccumulateWindow(kernv2y, p2yw, ypos, 16, v);

					ATFilterKernelAccumulateWindow(kerny2u, p2uw, cpos, 32, y * co_ub);
					ATFilterKernelAccumulateWindow(kernu2u, p2uw, cpos, 32, u * co_ub);
					ATFilterKernelAccumulateWindow(kernv2u, p2uw, cpos, 32, v * co_ub);

					ATFilterKernelAccumulateWindow(kerny2v, p2vw, cpos, 32, y * co_vr * v_invert);
					ATFilterKernelAccumulateWindow(kernu2v, p2vw, cpos, 32, u * co_vr * v_invert);
					ATFilterKernelAccumulateWindow(kernv2v, p2vw, cpos, 32, v * co_vr * v_invert);

					uint32 *kerny16 = mPal8x.mPalToY[k][j][i];
					uint32 *kernu16 = mPal8x.mPalToU[k][j][i];
					uint32 *kernv16 = mPal8x.mPalToV[k][j][i];

					for(int offset=0; offset<8; ++offset) {
						sint32 w0 = VDRoundToInt32(p2yw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2yw[offset*2+1] * 64.0f * 255.0f);

						kerny16[offset] = (w1 << 16) + w0;
					}

					kerny16[i & 3] += 0x00200020;

					for(int offset=0; offset<16; ++offset) {
						sint32 w0 = VDRoundToInt32(p2uw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2uw[offset*2+1] * 64.0f * 255.0f);

						kernu16[offset] = (w1 << 16) + w0;
					}

					for(int offset=0; offset<16; ++offset) {
						sint32 w0 = VDRoundToInt32(p2vw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2vw[offset*2+1] * 64.0f * 255.0f);

						kernv16[offset] = (w1 << 16) + w0;
					}
				} else {
					int ypos = 3 - i*2;
					int cpos = 12 - i*2;

					ATFilterKernelAccumulateWindow(kerny2y, p2yw, ypos, 8, y);
					ATFilterKernelAccumulateWindow(kernu2y, p2yw, ypos, 8, u);
					ATFilterKernelAccumulateWindow(kernv2y, p2yw, ypos, 8, v);

					ATFilterKernelAccumulateWindow(kerny2u, p2uw, cpos, 24, y * co_ub);
					ATFilterKernelAccumulateWindow(kernu2u, p2uw, cpos, 24, u * co_ub);
					ATFilterKernelAccumulateWindow(kernv2u, p2uw, cpos, 24, v * co_ub);

					ATFilterKernelAccumulateWindow(kerny2v, p2vw, cpos, 24, y * co_vr * v_invert);
					ATFilterKernelAccumulateWindow(kernu2v, p2vw, cpos, 24, u * co_vr * v_invert);
					ATFilterKernelAccumulateWindow(kernv2v, p2vw, cpos, 24, v * co_vr * v_invert);

					uint32 *kerny16 = mPal2x.mPalToY[k][j][i];
					uint32 *kernu16 = mPal2x.mPalToU[k][j][i];
					uint32 *kernv16 = mPal2x.mPalToV[k][j][i];

					for(int offset=0; offset<4; ++offset) {
						sint32 w0 = VDRoundToInt32(p2yw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2yw[offset*2+1] * 64.0f * 255.0f);

						kerny16[offset] = (w1 << 16) + w0;
					}

					kerny16[3] += 0x40004000;

					for(int offset=0; offset<12; ++offset) {
						sint32 w0 = VDRoundToInt32(p2uw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2uw[offset*2+1] * 64.0f * 255.0f);

						kernu16[offset] = (w1 << 16) + w0;
					}

					for(int offset=0; offset<12; ++offset) {
						sint32 w0 = VDRoundToInt32(p2vw[offset*2+0] * 64.0f * 255.0f);
						sint32 w1 = VDRoundToInt32(p2vw[offset*2+1] * 64.0f * 255.0f);

						kernv16[offset] = (w1 << 16) + w0;
					}
				}
			}
		}
	}

	// Create twin kernels.
	//
	// Most scanlines on the Atari aren't hires, which means that pairs of pixels are identical.
	// What we do here is precompute pairs of adjacent phase filter kernels added together. On
	// any scanline that is lores only, we can use a faster twin-mode set of filter routines.

	if (SSE2_enabled) {
		for(int i=0; i<2; ++i) {
			for(int j=0; j<256; ++j) {
				for(int k=0; k<4; ++k) {
					for(int l=0; l<8; ++l)
						mPal8x.mPalToYTwin[i][j][k][l] = mPal8x.mPalToY[i][j][k*2][l] + mPal8x.mPalToY[i][j][k*2+1][l];

					for(int l=0; l<16; ++l)
						mPal8x.mPalToUTwin[i][j][k][l] = mPal8x.mPalToU[i][j][k*2][l] + mPal8x.mPalToU[i][j][k*2+1][l];

					for(int l=0; l<16; ++l)
						mPal8x.mPalToVTwin[i][j][k][l] = mPal8x.mPalToV[i][j][k*2][l] + mPal8x.mPalToV[i][j][k*2+1][l];
				}
			}
		}
	}
}

