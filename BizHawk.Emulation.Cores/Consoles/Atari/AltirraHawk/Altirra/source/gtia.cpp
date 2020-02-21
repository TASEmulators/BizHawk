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
#include <vd2/system/binary.h>
#include <vd2/system/cpuaccel.h>
#include <vd2/system/math.h>
#include <vd2/VDDisplay/display.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/Kasumi/triblt.h>
#include <at/atcore/enumparseimpl.h>
#include "gtia.h"
#include "gtiatables.h"
#include "gtiarenderer.h"
#include "console.h"
#include "artifacting.h"
#include "savestate.h"
#include "uirender.h"
#include "vbxe.h"

using namespace ATGTIA;

AT_DEFINE_ENUM_TABLE_BEGIN(ATColorMatchingMode)
	{ ATColorMatchingMode::None, "none" },
	{ ATColorMatchingMode::SRGB, "srgb" },
	{ ATColorMatchingMode::AdobeRGB, "adobergb" },
AT_DEFINE_ENUM_TABLE_END(ATColorMatchingMode, ATColorMatchingMode::None)

#ifdef VD_CPU_X86
extern "C" void VDCDECL atasm_update_playfield_160_sse2(
	void *dst,
	const uint8 *src,
	uint32 n
);
#endif

#if defined(VD_CPU_X86) || defined(VD_CPU_X64)
#include "gtia_sse2_intrin.inl"
#elif defined(VD_CPU_ARM64)
#include "gtia_neon.inl"
#endif

///////////////////////////////////////////////////////////////////////////

bool ATColorParams::IsSimilar(const ATColorParams& other) const {
	const auto IsSimilar = [](float x, float y) { return fabsf(x - y) < 1e-5f; };

	return IsSimilar(mHueStart			, other.mHueStart)
		&& IsSimilar(mHueRange			, other.mHueRange)
		&& IsSimilar(mBrightness		, other.mBrightness)
		&& IsSimilar(mContrast			, other.mContrast)
		&& IsSimilar(mSaturation		, other.mSaturation)
		&& IsSimilar(mGammaCorrect		, other.mGammaCorrect)
		&& IsSimilar(mIntensityScale	, other.mIntensityScale)
		&& IsSimilar(mArtifactHue		, other.mArtifactHue)
		&& IsSimilar(mArtifactSat		, other.mArtifactSat)
		&& IsSimilar(mArtifactSharpness	, other.mArtifactSharpness)
		&& IsSimilar(mRedShift			, other.mRedShift)
		&& IsSimilar(mRedScale			, other.mRedScale)
		&& IsSimilar(mGrnShift			, other.mGrnShift)
		&& IsSimilar(mGrnScale			, other.mGrnScale)
		&& IsSimilar(mBluShift			, other.mBluShift)
		&& IsSimilar(mBluScale			, other.mBluScale)
		&& mbUsePALQuirks == other.mbUsePALQuirks
		&& mLumaRampMode == other.mLumaRampMode
		&& mColorMatchingMode == other.mColorMatchingMode;
}

ATArtifactingParams ATArtifactingParams::GetDefault() {
	ATArtifactingParams params = {};
	params.mScanlineIntensity = 0.75f;
	params.mbEnableBloom = false;
	params.mbBloomScanlineCompensation = true;
	params.mBloomThreshold = 0.01f;
	params.mBloomRadius = 9.8f;
	params.mBloomDirectIntensity = 1.00f;
	params.mBloomIndirectIntensity = 0.10f;

	return params;
}

///////////////////////////////////////////////////////////////////////////

namespace nsATColorPresets {
	constexpr ATColorParams GetPresetBase() {
		ATColorParams pa {};
		pa.mRedShift = 0;
		pa.mRedScale = 1;
		pa.mGrnShift = 0;
		pa.mGrnScale = 1;
		pa.mBluShift = 0;
		pa.mBluScale = 1;
		pa.mGammaCorrect = 1.0f;
		pa.mIntensityScale = 1.0f;
		pa.mArtifactSharpness = 0.50f;
		pa.mbUsePALQuirks = false;
		pa.mLumaRampMode = kATLumaRampMode_XL;
		pa.mColorMatchingMode = ATColorMatchingMode::None;

		return pa;
	}

	constexpr ATColorParams GetDefaultNTSCPreset() {
		ATColorParams pa = GetPresetBase();
		pa.mHueStart = -57.0f;
		pa.mHueRange = 27.1f * 15.0f;
		pa.mBrightness = -0.04f;
		pa.mContrast = 1.04f;
		pa.mSaturation = 0.20f;
		pa.mArtifactHue = 252.0f;
		pa.mArtifactSat = 1.15f;
		pa.mArtifactSharpness = 0.50f;
		pa.mColorMatchingMode = ATColorMatchingMode::SRGB;

		return pa;
	}

	constexpr ATColorParams GetDefaultPALPreset() {
		ATColorParams pa = GetPresetBase();
		pa.mHueStart = -24.0f;
		pa.mHueRange = 23.5f * 15.0f;
		pa.mBrightness = 0.0f;
		pa.mContrast = 1.0f;
		pa.mSaturation = 0.29f;
		pa.mArtifactHue = 80.0f;
		pa.mArtifactSat = 0.80f;
		pa.mArtifactSharpness = 0.50f;
		pa.mbUsePALQuirks = true;

		return pa;
	}
}

static constexpr struct ATColorPreset {
	const char *mpTag;
	const wchar_t *mpName;
	ATColorParams mParams;
} kColorPresets[] = {
	{ "default_ntsc", L"Default NTSC (XL)", nsATColorPresets::GetDefaultNTSCPreset()},
	{ "default_pal", L"Default PAL", nsATColorPresets::GetDefaultPALPreset()},

	{ "ntsc_xl_contemporary", L"NTSC Contemporary (XL)", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetDefaultNTSCPreset();
			pa.mHueStart = -33.0f;
			pa.mHueRange = 24.0f * 15.0f;
			return pa;
		}() },

	{ "ntsc_xe", L"NTSC (XE)", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetDefaultNTSCPreset();
			pa.mArtifactHue = 191.0f;
			pa.mArtifactSat = 1.32f;
			return pa;
		}() },

	{ "ntsc_800", L"NTSC (800)", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -57.0f;
			pa.mHueRange = 27.1f * 15.0f;
			pa.mBrightness = -0.04f;
			pa.mContrast = 1.04f;
			pa.mSaturation = 0.20f;
			pa.mGammaCorrect = 1.0f;
			pa.mIntensityScale = 0.77f;
			pa.mArtifactHue = 124.0f;
			pa.mArtifactSat = 2.08f;
			pa.mColorMatchingMode = ATColorMatchingMode::SRGB;
			return pa;
		}() },

	{ "ntsc_xl_1702", L"NTSC (XL + Commodore 1702 monitor)", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -33.0f;
			pa.mHueRange = 24.0f * 15.0f;
			pa.mBrightness = 0;
			pa.mContrast = 1.08f;
			pa.mSaturation = 0.30f;
			pa.mGammaCorrect = 1.0f;
			pa.mArtifactHue = 277.0f;
			pa.mArtifactSat = 2.13f;
			pa.mGrnScale = 0.60f;
			pa.mBluShift = -5.5f;
			pa.mBluScale = 1.56f;
			return pa;
		}() },

	{ "altirra310_ntsc", L"Altirra 3.10 NTSC", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -57.0f;
			pa.mHueRange = 27.1f * 15.0f;
			pa.mBrightness = -0.04f;
			pa.mContrast = 1.04f;
			pa.mSaturation = 0.20f;
			pa.mArtifactHue = 252.0f;
			pa.mArtifactSat = 1.15f;
			pa.mArtifactSharpness = 0.50f;
			pa.mBluScale = 1.50f;
			return pa;
		}() },

	{ "altirra280_ntsc", L"Altirra 2.80 NTSC", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -36.0f;
			pa.mHueRange = 25.5f * 15.0f;
			pa.mBrightness = -0.08f;
			pa.mContrast = 1.08f;
			pa.mSaturation = 0.33f;
			pa.mGammaCorrect = 1.0f;
			pa.mArtifactHue = 279.0f;
			pa.mArtifactSat = 0.68f;
			return pa;
		}() },

	{ "altirra250_ntsc", L"Altirra 2.50 NTSC", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -51.0f;
			pa.mHueRange = 27.9f * 15.0f;
			pa.mBrightness = 0.0f;
			pa.mContrast = 1.0f;
			pa.mSaturation = 75.0f / 255.0f;
			pa.mGammaCorrect = 1.0f;
			pa.mArtifactHue = -96.0f;
			pa.mArtifactSat = 2.76f;
			return pa;
		}() },

	{ "jakub", L"Jakub", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -9.36754f;
			pa.mHueRange = 361.019f;
			pa.mBrightness = +0.174505f;
			pa.mContrast = 0.82371f;
			pa.mSaturation = 0.21993f;
			pa.mGammaCorrect = 1.0f;
			pa.mArtifactHue = -96.0f;
			pa.mArtifactSat = 2.76f;
			return pa;
		}() },

	{ "olivierpal", L"OlivierPAL", []() -> ATColorParams {
			ATColorParams pa = nsATColorPresets::GetPresetBase();
			pa.mHueStart = -14.7889f;
			pa.mHueRange = 385.155f;
			pa.mBrightness = +0.057038f;
			pa.mContrast = 0.941149f;
			pa.mSaturation = 0.195861f;
			pa.mGammaCorrect = 1.0f;
			pa.mArtifactHue = 80.0f;
			pa.mArtifactSat = 0.80f;
			return pa;
		}() },
};

uint32 ATGetColorPresetCount() {
	return vdcountof(kColorPresets);
}

const char *ATGetColorPresetTagByIndex(uint32 i) {
	return kColorPresets[i].mpTag;
}

sint32 ATGetColorPresetIndexByTag(const char *tags) {
	VDStringRefA tagsRef(tags);

	while(!tagsRef.empty()) {
		VDStringRefA tag;
		if (!tagsRef.split(',', tag)) {
			tag = tagsRef;
			tagsRef.clear();
		}

		for(uint32 i=0; i<vdcountof(kColorPresets); ++i) {
			if (tag == kColorPresets[i].mpTag)
				return (sint32)i;
		}
	}

	return -1;
}

const wchar_t *ATGetColorPresetNameByIndex(uint32 i) {
	return kColorPresets[i].mpName;
}

ATColorParams ATGetColorPresetByIndex(uint32 i) {
	return kColorPresets[i].mParams;
}

///////////////////////////////////////////////////////////////////////////

class ATFrameTracker final : public vdrefcounted<IVDRefCount> {
public:
	ATFrameTracker() : mActiveFrames(0) { }
	VDAtomicInt mActiveFrames;
};

class ATFrameBuffer final : public VDVideoDisplayFrame, public IVDVideoDisplayScreenFXEngine {
public:
	ATFrameBuffer(ATFrameTracker *tracker, ATArtifactingEngine& artengine)
		: mpTracker(tracker)
		, mArtEngine(artengine)
	{
		++mpTracker->mActiveFrames;

		mpScreenFXEngine = this;
	}

	~ATFrameBuffer() {
		--mpTracker->mActiveFrames;
	}

	VDPixmap ApplyScreenFX(const VDPixmap& px) override;

	vdrefptr<ATFrameTracker> mpTracker;
	ATArtifactingEngine& mArtEngine;
	VDPixmapBuffer mBuffer;
	VDPixmapBuffer mEmulatedFXBuffer {};
	VDVideoDisplayScreenFXInfo mScreenFX {};

	uint32 mViewX1 = 0;
	uint32 mViewY1 = 0;
	const uint32 *mpPalette = nullptr;

	bool mbDualFieldFrame = false;
	bool mbIncludeHBlank = false;
	bool mbScanlineHasHires[312] {};
};

VDPixmap ATFrameBuffer::ApplyScreenFX(const VDPixmap& px) {
	// Software scanlines only support noninterlaced frames.
	const bool scanlines = mScreenFX.mScanlineIntensity != 0.0f && !mbDualFieldFrame;

	const bool src32 = (mBuffer.format == nsVDPixmap::kPixFormat_XRGB8888);
	const uint32 w = px.w;
	const uint32 h = px.h;
	mEmulatedFXBuffer.init(src32 ? w : mBuffer.w, scanlines ? h * 2 : h, nsVDPixmap::kPixFormat_XRGB8888);

	const char *src = src32 ? (const char *)px.data : (const char *)mBuffer.data + mBuffer.pitch * mViewY1;
	char *dst = (char *)mEmulatedFXBuffer.data;

	const bool palBlending = (mScreenFX.mPALBlendingOffset != 0);

	// We may be called in the middle of a frame for an immediate update, so we must suspend
	// and restore the existing frame settings.
	mArtEngine.SuspendFrame();
	mArtEngine.BeginFrame(palBlending, palBlending, false, false, false, false);

	const uint32 interpW = src32 ? w : mBuffer.w;
	const uint32 bpr = src32 ? interpW * 4 : interpW;

	const char *last = nullptr;
	for(uint32 y = 0; y < h; ++y) {
		char *dst1 = nullptr;
		if (last && scanlines) {
			dst1 = dst;
			dst += mEmulatedFXBuffer.pitch;
		}

		char *dst2 = dst;
		dst += mEmulatedFXBuffer.pitch;
		if (src32) {
			memcpy(dst2, src, bpr);
			mArtEngine.Artifact32(y, (uint32 *)dst2, w, false, mbIncludeHBlank);
		} else
			mArtEngine.Artifact8(y, (uint32 *)dst2, (const uint8 *)src, mbScanlineHasHires[y], false, mbIncludeHBlank);

		src += mBuffer.pitch;

		if (dst1) {
			mArtEngine.InterpolateScanlines((uint32 *)dst1, (const uint32 *)last, (const uint32 *)dst2, interpW);
		}

		last = dst2;
	}
	
	mArtEngine.ResumeFrame();

	if (scanlines)
		memcpy(dst, last, interpW*4);

	VDPixmap result = mEmulatedFXBuffer;

	if (!src32)
		result.data = (char *)result.data + mViewX1 * 4;

	result.w = px.w;
	result.palette = mpPalette;

	VDASSERT(VDAssertValidPixmap(result));

	return result;
}

///////////////////////////////////////////////////////////////////////////

namespace {
	const int kPlayerWidths[4]={8,16,8,32};
	const int kMissileWidths[4]={2,4,2,8};

	const uint32 kSpriteShiftMasks[4]={
		0xFFFFFFFF,
		0x00,
		0x00,
		0x00,
	};

	const uint8 kSpriteStateTransitions[4][4]={
		{ 0, 0, 0, 0 },
		{ 1, 0, 1, 0 },
		{ 0, 2, 2, 0 },
		{ 1, 2, 3, 0 },
	};
}

///////////////////////////////////////////////////////////////////////////

void ATGTIAEmulator::SpriteState::Reset() {
	mShiftRegister = 0;
	mShiftState = 0;
	mSizeMode = 0;
	mDataLatch = 0;
}

// Detect collisions
uint8 ATGTIAEmulator::SpriteState::Detect(uint32 ticks, const uint8 *src) {
	uint8 shifter = mShiftRegister;
	int state = mShiftState;
	const uint8 *VDRESTRICT stateTransitions = kSpriteStateTransitions[mSizeMode];
	uint8 detect = 0;

	do {
		detect |= (*src++) & (uint8)((sint8)shifter >> 7);

		state = stateTransitions[state];
		shifter += shifter & kSpriteShiftMasks[state];
	} while(--ticks);

	return detect;
}

uint8 ATGTIAEmulator::SpriteState::Detect(uint32 ticks, const uint8 *src, const uint8 *hires) {
	uint8 shifter = mShiftRegister;
	int state = mShiftState;
	const uint8 *VDRESTRICT stateTransitions = kSpriteStateTransitions[mSizeMode];
	uint8 detect = 0;

	do {
		if ((sint8)shifter < 0) {
			detect |= (*src & (P01 | P23));

			if (*hires)
				detect |= PF2;
		}

		++src;
		++hires;

		state = stateTransitions[state];
		shifter += shifter & kSpriteShiftMasks[state];
	} while(--ticks);

	return detect;
}

uint8 ATGTIAEmulator::SpriteState::Generate(uint32 ticks, uint8 mask, uint8 *dst) {
	uint8 shifter = mShiftRegister;
	int state = mShiftState;
	const uint8 *VDRESTRICT stateTransitions = kSpriteStateTransitions[mSizeMode];
	uint8 detect = 0;

	do {
		if ((sint8)shifter < 0) {
			detect |= *dst;
			*dst |= mask;
		}

		++dst;

		state = stateTransitions[state];
		shifter += shifter & kSpriteShiftMasks[state];
	} while(--ticks);

	return detect;
}

uint8 ATGTIAEmulator::SpriteState::Generate(uint32 ticks, uint8 mask, uint8 *dst, const uint8 *hires) {
	uint8 shifter = mShiftRegister;
	int state = mShiftState;
	const uint8 *VDRESTRICT stateTransitions = kSpriteStateTransitions[mSizeMode];
	uint8 detect = 0;

	do {
		if ((sint8)shifter < 0) {
			detect |= (*dst & (P01 | P23));
			*dst |= mask;

			if (*hires)
				detect |= PF2;
		}

		++dst;
		++hires;

		state = stateTransitions[state];
		shifter += shifter & kSpriteShiftMasks[state];
	} while(--ticks);

	return detect;
}

// Advance the shift state for a player or missile by a number of ticks,
// without actually generating image data.
void ATGTIAEmulator::SpriteState::Advance(uint32 ticks) {
	int shifts = 0;

	switch(mSizeMode) {
		case 0:
			shifts = ticks;
			mShiftState = 0;
			break;

		case 1:
			shifts = ((mShiftState & 1) + ticks) >> 1;
			mShiftState = (ticks + mShiftState) & 1;
			break;

		case 2:
			// 00,11 -> 00
			// 01,10 -> 10
			switch(mShiftState) {
				case 0:
				case 3:
					shifts = ticks;
					mShiftState = 0;
					break;

				case 1:
					mShiftState = 2;
				case 2:
					break;
			}
			break;

		case 3:
			shifts = (mShiftState + ticks) >> 2;
			mShiftState = (mShiftState + ticks) & 3;
			break;
	}

	if (shifts >= 32)
		mShiftRegister = 0;
	else
		mShiftRegister <<= shifts;
}

///////////////////////////////////////////////////////////////////////////

void ATGTIAEmulator::Sprite::Sync(int pos) {
	if (mLastSync != pos) {
		mState.Advance(pos - mLastSync);
		mLastSync = pos;
	}
}

///////////////////////////////////////////////////////////////////////////

ATGTIAEmulator::ATGTIAEmulator()
	: mpConn(NULL)
	, mpFrameTracker(new ATFrameTracker)
	, mbCTIAMode(false)
	, mbPALMode(false)
	, mbSECAMMode(false)
	, mArtifactMode(kArtifactNone)
	, mOverscanMode(kOverscanExtended)
	, mVerticalOverscanMode(kVerticalOverscan_Default)
	, mVBlankMode(kVBlankModeOn)
	, mbVsyncEnabled(true)
	, mbBlendMode(false)
	, mbBlendModeLastFrame(false)
	, mbOverscanPALExtended(false)
	, mbOverscanPALExtendedThisFrame(false)
	, mbPALThisFrame(false)
	, mbInterlaceEnabled(false)
	, mbInterlaceEnabledThisFrame(false)
	, mbScanlinesEnabled(false)
	, mbSoftScanlinesEnabledThisFrame(false)
	, mbIncludeHBlankThisFrame(false)
	, mbFieldPolarity(false)
	, mbLastFieldPolarity(false)
	, mbPostProcessThisFrame(false)
	, mPreArtifactFrameBuffer(464*312+16)
	, mpArtifactingEngine(new ATArtifactingEngine)
	, mpRenderer(new ATGTIARenderer)
	, mpUIRenderer(NULL)
	, mpVBXE(NULL)
	, mRCIndex(0)
	, mRCCount(0)
	, mpFreeSpriteImages(NULL)
{
	ResetColors();

	mPreArtifactFrame.data = mPreArtifactFrameBuffer.data();
	mPreArtifactFrame.pitch = 464;
	mPreArtifactFrame.palette = mPalette;
	mPreArtifactFrame.data2 = NULL;
	mPreArtifactFrame.data3 = NULL;
	mPreArtifactFrame.pitch2 = 0;
	mPreArtifactFrame.pitch3 = 0;
	mPreArtifactFrame.w = 456;
	mPreArtifactFrame.h = 262;
	mPreArtifactFrame.format = nsVDPixmap::kPixFormat_Pal8;

	mpFrameTracker->AddRef();

	mSwitchOutput = 8;
	mSwitchInput = 15;
	mForcedSwitchInput = 15;
	mPRIOR = 0;
	mActivePRIOR = 0;

	for(int i=0; i<4; ++i) {
		mTRIG[i] = 0x01;
		mTRIGLatched[i] = 0x01;
		mTRIGSECAM[i] = 0x01;
		mTRIGSECAMLastUpdate[i] = 0;
	}

	SetAnalysisMode(kAnalyzeNone);
	memset(mPlayerCollFlags, 0, sizeof mPlayerCollFlags);
	memset(mMissileCollFlags, 0, sizeof mMissileCollFlags);
	mCollisionMask = 0xFF;

	mbTurbo = false;
	mbForcedBorder = false;

	SetPALMode(false);
}

ATGTIAEmulator::~ATGTIAEmulator() {
	mpLastFrame = NULL;

	if (mpFrameTracker) {
		mpFrameTracker->Release();
		mpFrameTracker = NULL;
	}

	delete mpArtifactingEngine;
	delete mpRenderer;
}

void ATGTIAEmulator::Init(IATGTIAEmulatorConnections *conn) {
	mpConn = conn;
	mY = 0;

	ColdReset();
}

void ATGTIAEmulator::ColdReset() {
	memset(mSpritePos, 0, sizeof mSpritePos);
	memset(mPMColor, 0, sizeof mPMColor);
	memset(mPFColor, 0, sizeof mPFColor);

	memset(&mState, 0, sizeof mState);

	memset(mPMColor, 0, sizeof mPMColor);
	memset(mPFColor, 0, sizeof mPFColor);
	mPFBAK = 0;
	mPRIOR = 0;
	mActivePRIOR = 0;
	mVDELAY = 0;
	mGRACTL = 0;
	mSwitchOutput = 0;

	memset(mPlayerCollFlags, 0, sizeof mPlayerCollFlags);
	memset(mMissileCollFlags, 0, sizeof mMissileCollFlags);

	ResetSprites();

	mpConn->GTIASelectController(0, false);
	mpRenderer->ColdReset();
}

void ATGTIAEmulator::SetVBXE(ATVBXEEmulator *vbxe) {
	mpVBXE = vbxe;

	if (mpVBXE)
		mpVBXE->SetDefaultPalette(mPalette);

	// kill current frame update
	mpDst = NULL;
	mpFrame = NULL;
}

void ATGTIAEmulator::SetUIRenderer(IATUIRenderer *r) {
	mpUIRenderer = r;
}

ATColorSettings ATGTIAEmulator::GetColorSettings() const {
	return mColorSettings;
}

void ATGTIAEmulator::SetColorSettings(const ATColorSettings& settings) {
	mColorSettings = settings;
	RecomputePalette();
}

ATArtifactingParams ATGTIAEmulator::GetArtifactingParams() const {
	return mpArtifactingEngine->GetArtifactingParams();
}

void ATGTIAEmulator::SetArtifactingParams(const ATArtifactingParams& params) {
	mpArtifactingEngine->SetArtifactingParams(params);
}

void ATGTIAEmulator::ResetColors() {
	mColorSettings.mNTSCParams.mPresetTag = "default_ntsc";
	static_cast<ATColorParams&>(mColorSettings.mNTSCParams) = ATGetColorPresetByIndex(ATGetColorPresetIndexByTag("default_ntsc"));
	mColorSettings.mPALParams.mPresetTag = "default_pal";
	static_cast<ATColorParams&>(mColorSettings.mPALParams) = ATGetColorPresetByIndex(ATGetColorPresetIndexByTag("default_pal"));
	mColorSettings.mbUsePALParams = true;

	RecomputePalette();
}

void ATGTIAEmulator::GetPalette(uint32 pal[256]) const {
	memcpy(pal, mPalette, sizeof(uint32)*256);
}

void ATGTIAEmulator::GetNTSCArtifactColors(uint32 c[2]) const {
	mpArtifactingEngine->GetNTSCArtifactColors(c);
}

bool ATGTIAEmulator::AreAcceleratedEffectsAvailable() const {
	return mpDisplay && mpDisplay->IsScreenFXPreferred();
}

void ATGTIAEmulator::SetAnalysisMode(AnalysisMode mode) {
	mAnalysisMode = mode;
	mpRenderer->SetAnalysisMode(mode != kAnalyzeNone);
}

void ATGTIAEmulator::SetOverscanMode(OverscanMode mode) {
	mOverscanMode = mode;
}

void ATGTIAEmulator::SetVerticalOverscanMode(VerticalOverscanMode mode) {
	mVerticalOverscanMode = mode;
}

void ATGTIAEmulator::SetOverscanPALExtended(bool extended) {
	mbOverscanPALExtended = extended;
}

vdrect32 ATGTIAEmulator::GetFrameScanArea() const {
	int xlo = 44;
	int xhi = 212;
	int ylo = 8;
	int yhi = 248;

	bool palext = mbPALMode && mbOverscanPALExtended;
	if (palext) {
		ylo -= 25;
		yhi += 25;
	}

	OverscanMode omode = mOverscanMode;
	VerticalOverscanMode vomode = DeriveVerticalOverscanMode();

	if (mAnalysisMode || mbForcedBorder) {
		omode = kOverscanFull;
		vomode = kVerticalOverscan_Full;
	}

	switch(omode) {
		case kOverscanFull:
			xlo = 0;
			xhi = 228;
			break;

		case kOverscanExtended:
			xlo = 34;
			xhi = 222;
			break;

		case kOverscanNormal:
			break;

		case kOverscanOSScreen:
			xlo = 48;
			xhi = 208;
			break;

		case kOverscanWidescreen:
			xlo = 128 - 176/2;
			xhi = 128 + 176/2;
			break;
	}

	switch(vomode) {
		case kVerticalOverscan_Full:
			ylo = 0;
			yhi = 262;

			if (palext) {
				ylo = -25;
				yhi = 287;
			}
			break;

		case kVerticalOverscan_Extended:
			break;

		case kVerticalOverscan_OSScreen:
			if (!palext) {
				ylo = 32;
				yhi = 224;
			}
			break;

		case kVerticalOverscan_Normal:
			if (!mbPALMode) {
				ylo = 16;
				yhi = 240;
			}
			break;
	}

	return vdrect32(xlo, ylo, xhi, yhi);
}

void ATGTIAEmulator::GetRawFrameFormat(int& w, int& h, bool& rgb32) const {
	rgb32 = (mpVBXE != NULL) || mArtifactMode || mbBlendMode || mbScanlinesEnabled;

	const vdrect32 scanArea = GetFrameScanArea();

	w = scanArea.width() * 2;
	h = scanArea.height();

	if (mbInterlaceEnabled || mbScanlinesEnabled)
		h *= 2;

	if (mpVBXE != NULL || mArtifactMode == kArtifactNTSCHi || mArtifactMode == kArtifactPALHi || mArtifactMode == kArtifactAutoHi)
		w *= 2;
}

void ATGTIAEmulator::GetFrameSize(int& w, int& h) const {
	const vdrect32 scanArea = GetFrameScanArea();

	w = scanArea.width() * 2;
	h = scanArea.height();

	if (mpVBXE != NULL || mArtifactMode == kArtifactNTSCHi || mArtifactMode == kArtifactPALHi || mArtifactMode == kArtifactAutoHi || mbInterlaceEnabled || mbScanlinesEnabled) {
		w *= 2;
		h *= 2;
	}
}

void ATGTIAEmulator::GetPixelAspectMultiple(int& x, int& y) const {
	int ix = 1;
	int iy = 1;

	if (mbInterlaceEnabled || mbScanlinesEnabled)
		iy = 2;

	if (mpVBXE != NULL || mArtifactMode == kArtifactNTSCHi || mArtifactMode == kArtifactPALHi || mArtifactMode == kArtifactAutoHi)
		ix = 2;

	x = ix;
	y = iy;
}

bool ATGTIAEmulator::ArePMCollisionsEnabled() const {
	return (mCollisionMask & 0xf0) != 0;
}

void ATGTIAEmulator::SetPMCollisionsEnabled(bool enable) {
	if (enable) {
		if (!(mCollisionMask & 0xf0)) {
			// we clear the collision flags directly when re-enabling collisions
			// as they were being masked in the register read
			for(int i=0; i<4; ++i) {
				mPlayerCollFlags[i] &= 0x0f;
				mMissileCollFlags[i] &= 0x0f;
			}
		}

		mCollisionMask |= 0xf0;
	} else {
		mCollisionMask &= 0x0f;
	}
}

bool ATGTIAEmulator::ArePFCollisionsEnabled() const {
	return (mCollisionMask & 0x0f) != 0;
}

void ATGTIAEmulator::SetPFCollisionsEnabled(bool enable) {
	if (enable) {
		if (!(mCollisionMask & 0x0f)) {
			// we clear the collision flags directly when re-enabling collisions
			// as they were being masked in the register read
			for(int i=0; i<4; ++i) {
				mPlayerCollFlags[i] &= 0xf0;
				mMissileCollFlags[i] &= 0xf0;
			}
		}

		mCollisionMask |= 0x0f;
	} else {
		mCollisionMask &= 0xf0;
	}
}

void ATGTIAEmulator::SetVideoOutput(IVDVideoDisplay *pDisplay) {
	mpDisplay = pDisplay;

	if (!pDisplay) {
		mpFrame = NULL;
		mpDst = NULL;
	}
}

void ATGTIAEmulator::SetCTIAMode(bool enabled) {
	mbCTIAMode = enabled;

	if (!enabled && (mPRIOR & 0xC0)) {
		mPRIOR &= 0x3F;

		mpRenderer->SetCTIAMode();

		// scrub any register changes
		for(int i=mRCIndex; i<mRCCount; ++i) {
			if (mRegisterChanges[i].mReg == 0x1B)
				mRegisterChanges[i].mValue &= 0x3F;
		}
	}
}

void ATGTIAEmulator::SetPALMode(bool enabled) {
	mbPALMode = enabled;

	RecomputePalette();
}

void ATGTIAEmulator::SetSECAMMode(bool enabled) {
	mbSECAMMode = enabled;

	mpRenderer->SetSECAMMode(enabled);
}

void ATGTIAEmulator::SetConsoleSwitch(uint8 c, bool set) {
	mSwitchInput &= ~c;

	if (!set)			// bit is active low
		mSwitchInput |= c;
}

uint8 ATGTIAEmulator::ReadConsoleSwitches() const {
	return (~mSwitchOutput & mSwitchInput & mForcedSwitchInput) & 15;
}

void ATGTIAEmulator::SetForcedConsoleSwitches(uint8 c) {
	mForcedSwitchInput = c;
}

void ATGTIAEmulator::AddVideoTap(IATGTIAVideoTap *vtap) {
	if (!mpVideoTaps)
		mpVideoTaps = new vdfastvector<IATGTIAVideoTap *>;

	mpVideoTaps->push_back(vtap);
}

void ATGTIAEmulator::RemoveVideoTap(IATGTIAVideoTap *vtap) {
	if (mpVideoTaps) {
		auto it = std::find(mpVideoTaps->begin(), mpVideoTaps->end(), vtap);

		if (it != mpVideoTaps->end()) {
			mpVideoTaps->erase(it);

			if (mpVideoTaps->empty())
				mpVideoTaps = nullptr;
		}
	}
}

void ATGTIAEmulator::AddRawFrameCallback(const ATGTIARawFrameFn *fn) {
	mRawFrameCallbacks.Add(fn);
}

void ATGTIAEmulator::RemoveRawFrameCallback(const ATGTIARawFrameFn *fn) {
	mRawFrameCallbacks.Remove(fn);
}

const VDPixmap *ATGTIAEmulator::GetLastFrameBuffer() const {
	return mpLastFrame ? &mpLastFrame->mPixmap : NULL;
}

void ATGTIAEmulator::DumpStatus() {
	for(int i=0; i<4; ++i) {
		ATConsolePrintf("Player  %d: color = %02x, pos = %02x, size=%d, data = %02x\n"
			, i
			, mPMColor[i]
			, mSpritePos[i]
			, mSprites[i].mState.mSizeMode
			, mSprites[i].mState.mDataLatch
			);
	}

	for(int i=0; i<4; ++i) {
		ATConsolePrintf("Missile %d: color = %02x, pos = %02x, size=%d, data = %02x\n"
			, i
			, mPRIOR & 0x10 ? mPFColor[3] : mPMColor[i]
			, mSpritePos[i+4]
			, mSprites[i+4].mState.mSizeMode
			, mSprites[i+4].mState.mDataLatch >> 6
			);
	}

	ATConsolePrintf("Playfield colors: %02x | %02x %02x %02x %02x\n"
		, mPFBAK
		, mPFColor[0]
		, mPFColor[1]
		, mPFColor[2]
		, mPFColor[3]);

	ATConsolePrintf("PRIOR:  %02x (pri=%2d%s%s %s)\n"
		, mPRIOR
		, mPRIOR & 15
		, mPRIOR & 0x10 ? ", pl5" : ""
		, mPRIOR & 0x20 ? ", multicolor" : ""
		, (mPRIOR & 0xc0) == 0x00 ? ", normal"
		: (mPRIOR & 0xc0) == 0x40 ? ", 1 color / 16 lumas"
		: (mPRIOR & 0xc0) == 0x80 ? ", 9 colors"
		: ", 16 colors / 1 luma");

	ATConsolePrintf("VDELAY: %02x\n", mVDELAY);

	ATConsolePrintf("GRACTL: %02x%s%s%s\n"
		, mGRACTL
		, mGRACTL & 0x04 ? ", latched" : ""
		, mGRACTL & 0x02 ? ", player DMA" : ""
		, mGRACTL & 0x01 ? ", missile DMA" : ""
		);

	uint8 consol = ~(mSwitchInput & mForcedSwitchInput & ~mSwitchOutput);
	ATConsolePrintf("CONSOL: %02x set <-> %02x input%s%s%s%s\n"
		, mSwitchOutput
		, mSwitchInput
		, mSwitchOutput & 0x08 ? ", speaker" : ""
		, consol & 0x04 ? ", option" : ""
		, consol & 0x02 ? ", select" : ""
		, consol & 0x01 ? ", start" : ""
		);

	uint8 v;
	for(int i=0; i<4; ++i) {
		v = ReadByte(0x00 + i);
		ATConsolePrintf("M%cPF:%s%s%s%s\n"
			, '0' + i
			, v & 0x01 ? " PF0" : ""
			, v & 0x02 ? " PF1" : ""
			, v & 0x04 ? " PF2" : ""
			, v & 0x08 ? " PF3" : "");
	}

	for(int i=0; i<4; ++i) {
		v = ReadByte(0x04 + i);
		ATConsolePrintf("P%cPF:%s%s%s%s\n"
			, '0' + i
			, v & 0x01 ? " PF0" : ""
			, v & 0x02 ? " PF1" : ""
			, v & 0x04 ? " PF2" : ""
			, v & 0x08 ? " PF3" : "");
	}

	for(int i=0; i<4; ++i) {
		v = ReadByte(0x08 + i);
		ATConsolePrintf("M%cPL:%s%s%s%s\n"
			, '0' + i
			, v & 0x01 ? " P0" : ""
			, v & 0x02 ? " P1" : ""
			, v & 0x04 ? " P2" : ""
			, v & 0x08 ? " P3" : "");
	}

	for(int i=0; i<4; ++i) {
		v = ReadByte(0x0c + i);
		ATConsolePrintf("P%cPL:%s%s%s%s\n"
			, '0' + i
			, v & 0x01 ? " PF0" : ""
			, v & 0x02 ? " PF1" : ""
			, v & 0x04 ? " PF2" : ""
			, v & 0x08 ? " PF3" : "");
	}
}

template<class T>
void ATGTIAEmulator::ExchangeStatePrivate(T& io) {
	for(int i=0; i<4; ++i)
		io != mPlayerCollFlags[i];

	for(int i=0; i<4; ++i)
		io != mMissileCollFlags[i];

	io != mbHiresMode;
	io != mActivePRIOR;
}

void ATGTIAEmulator::BeginLoadState(ATSaveStateReader& reader) {
	reader.RegisterHandlerMethod(kATSaveStateSection_Arch, VDMAKEFOURCC('G', 'T', 'I', 'A'), this, &ATGTIAEmulator::LoadStateArch);
	reader.RegisterHandlerMethod(kATSaveStateSection_Private, VDMAKEFOURCC('G', 'T', 'I', 'A'), this, &ATGTIAEmulator::LoadStatePrivate);
	reader.RegisterHandlerMethod(kATSaveStateSection_ResetPrivate, 0, this, &ATGTIAEmulator::LoadStateResetPrivate);
	reader.RegisterHandlerMethod(kATSaveStateSection_End, 0, this, &ATGTIAEmulator::EndLoadState);

	ResetSprites();
}

void ATGTIAEmulator::LoadStateArch(ATSaveStateReader& reader) {
	// P/M pos
	for(int i=0; i<8; ++i)
		reader != mSpritePos[i];

	// P/M size
	for(int i=0; i<4; ++i)
		mSprites[i].mState.mSizeMode = reader.ReadUint8() & 3;

	const uint8 missileSize = reader.ReadUint8();
	for(int i=0; i<4; ++i)
		mSprites[i+4].mState.mSizeMode = (missileSize >> (2*i)) & 3;

	// graphics latches
	for(int i=0; i<4; ++i)
		mSprites[i].mState.mDataLatch = reader.ReadUint8();

	const uint8 missileData = reader.ReadUint8();
	for(int i=0; i<4; ++i)
		mSprites[i+4].mState.mDataLatch = ((missileData >> (2*i)) & 3) << 6;

	// colors
	for(int i=0; i<4; ++i)
		reader != mPMColor[i];

	for(int i=0; i<4; ++i)
		reader != mPFColor[i];

	reader != mPFBAK;

	// misc registers
	reader != mPRIOR;
	reader != mVDELAY;
	reader != mGRACTL;
	reader != mSwitchOutput;
}

void ATGTIAEmulator::LoadStatePrivate(ATSaveStateReader& reader) {
	ExchangeStatePrivate(reader);

	// read register changes
	mRCCount = reader.ReadUint32();
	mRCIndex = 0;
	mRegisterChanges.resize(mRCCount);
	for(int i=0; i<mRCCount; ++i) {
		RegisterChange& rc = mRegisterChanges[i];

		rc.mPos = reader.ReadUint8();
		rc.mReg = reader.ReadUint8();
		rc.mValue = reader.ReadUint8();
	}

	mpRenderer->LoadState(reader);
}

void ATGTIAEmulator::LoadStateResetPrivate(ATSaveStateReader& reader) {
	for(int i=0; i<8; ++i) {
		mSprites[i].mState.mShiftState = 0;
		mSprites[i].mState.mShiftRegister = 0;
	}

	mbHiresMode = false;
	
	mRegisterChanges.clear();
	mRCCount = 0;
	mRCIndex = 0;

	mpRenderer->ResetState();
	mpRenderer->SetRegisterImmediate(0x1B, mPRIOR);
}

void ATGTIAEmulator::EndLoadState(ATSaveStateReader& writer) {
	// recompute derived state
	mpConn->GTIASetSpeaker(0 != (mSwitchOutput & 8));

	for(int i=0; i<4; ++i) {
		mpRenderer->SetRegisterImmediate(0x12 + i, mPMColor[i]);
		mpRenderer->SetRegisterImmediate(0x16 + i, mPFColor[i]);
	}

	mpRenderer->SetRegisterImmediate(0x1A, mPFBAK);

	// Terminate existing scan line
	mpDst = NULL;
	mpRenderer->EndScanline();
}

void ATGTIAEmulator::BeginSaveState(ATSaveStateWriter& writer) {
	writer.RegisterHandlerMethod(kATSaveStateSection_Arch, this, &ATGTIAEmulator::SaveStateArch);
	writer.RegisterHandlerMethod(kATSaveStateSection_Private, this, &ATGTIAEmulator::SaveStatePrivate);	
}

void ATGTIAEmulator::SaveStateArch(ATSaveStateWriter& writer) {
	writer.BeginChunk(VDMAKEFOURCC('G', 'T', 'I', 'A'));

	// P/M pos
	for(int i=0; i<8; ++i)
		writer != mSpritePos[i];

	// P/M size
	for(int i=0; i<4; ++i)
		writer.WriteUint8(mSprites[i].mState.mSizeMode);

	writer.WriteUint8(
		(mSprites[4].mState.mSizeMode << 0) +
		(mSprites[5].mState.mSizeMode << 2) +
		(mSprites[6].mState.mSizeMode << 4) +
		(mSprites[7].mState.mSizeMode << 6));

	// graphics latches
	for(int i=0; i<4; ++i)
		writer.WriteUint8(mSprites[i].mState.mDataLatch);

	writer.WriteUint8(
		(mSprites[4].mState.mDataLatch >> 6) +
		(mSprites[5].mState.mDataLatch >> 4) +
		(mSprites[6].mState.mDataLatch >> 2) +
		(mSprites[7].mState.mDataLatch >> 0));

	// colors
	for(int i=0; i<4; ++i)
		writer != mPMColor[i];

	for(int i=0; i<4; ++i)
		writer != mPFColor[i];

	writer != mPFBAK;

	// misc registers
	writer != mPRIOR;
	writer != mVDELAY;
	writer != mGRACTL;
	writer != mSwitchOutput;

	writer.EndChunk();
}

void ATGTIAEmulator::SaveStatePrivate(ATSaveStateWriter& writer) {
	writer.BeginChunk(VDMAKEFOURCC('G', 'T', 'I', 'A'));
	ExchangeStatePrivate(writer);

	// write register changes
	writer.WriteUint32(mRCCount - mRCIndex);
	for(int i=mRCIndex; i<mRCCount; ++i) {
		const RegisterChange& rc = mRegisterChanges[i];

		writer.WriteSint16(rc.mPos);
		writer.WriteUint8(rc.mReg);
		writer.WriteUint8(rc.mValue);
	}

	mpRenderer->SaveState(writer);
	writer.EndChunk();
}

void ATGTIAEmulator::GetRegisterState(ATGTIARegisterState& state) const {
	state = mState;

	// $D000-D007 HPOSP0-3, HPOSM0-3
	for(int i=0; i<8; ++i)
		state.mReg[i] = mSpritePos[i];

	// $D008-D00B SIZEP0-3
	for(int i=0; i<4; ++i)
		state.mReg[i+8] = mSprites[i].mState.mSizeMode;

	// $D00C SIZEM
	state.mReg[0x0C] = 
		(mSprites[4].mState.mSizeMode << 0) +
		(mSprites[5].mState.mSizeMode << 2) +
		(mSprites[6].mState.mSizeMode << 4) +
		(mSprites[7].mState.mSizeMode << 6);

	// GRAFP0-GRAFP3
	state.mReg[0x0D] = mSprites[0].mState.mDataLatch;
	state.mReg[0x0E] = mSprites[1].mState.mDataLatch;
	state.mReg[0x0F] = mSprites[2].mState.mDataLatch;
	state.mReg[0x10] = mSprites[3].mState.mDataLatch;

	// GRAFM
	state.mReg[0x11] = 
		(mSprites[4].mState.mDataLatch >> 6) +
		(mSprites[5].mState.mDataLatch >> 4) +
		(mSprites[6].mState.mDataLatch >> 2) +
		(mSprites[7].mState.mDataLatch >> 0);

	state.mReg[0x12] = mPMColor[0];
	state.mReg[0x13] = mPMColor[1];
	state.mReg[0x14] = mPMColor[2];
	state.mReg[0x15] = mPMColor[3];
	state.mReg[0x16] = mPFColor[0];
	state.mReg[0x17] = mPFColor[1];
	state.mReg[0x18] = mPFColor[2];
	state.mReg[0x19] = mPFColor[3];
	state.mReg[0x1A] = mPFBAK;
	state.mReg[0x1B] = mPRIOR;
	state.mReg[0x1C] = mVDELAY;
	state.mReg[0x1D] = mGRACTL;
	state.mReg[0x1F] = mSwitchOutput;
}

void ATGTIAEmulator::SetFieldPolarity(bool polarity) {
	mbFieldPolarity = polarity;
}

void ATGTIAEmulator::SetVBLANK(VBlankMode vblMode) {
	mVBlankMode = vblMode;
}

bool ATGTIAEmulator::BeginFrame(bool force, bool drop) {
	if (mpFrame)
		return true;

	if (!mpDisplay)
		return true;

	if (mpVideoTaps)
		drop = false;

	if (!drop && !mpDisplay->RevokeBuffer(false, ~mpFrame)) {
		if (mpFrameTracker->mActiveFrames < 3) {
			ATFrameBuffer *fb = new ATFrameBuffer(mpFrameTracker, *mpArtifactingEngine);
			mpFrame = fb;

			fb->mPixmap.format = 0;
			fb->mbAllowConversion = true;
			fb->mFlags = 0;
		} else if ((mpVideoTaps || !mbTurbo) && !force) {
			if (!mpDisplay->RevokeBuffer(true, ~mpFrame))
				return false;
		}
	}

	mRawFrame.data = nullptr;

	if (mpFrame) {
		ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);

		if (mbVsyncEnabled)
			fb->mFlags |= IVDVideoDisplay::kVSync;
		else
			fb->mFlags &= ~IVDVideoDisplay::kVSync;

		mbFrameCopiedFromPrev = false;

		// Try to use hardware accelerated screen effects except when:
		//
		// - The current display driver doesn't support them.
		// - We have a video recording tap active.
		// - We have a raw frame callback active.
		//
		const bool canAccelFX = mpDisplay->IsScreenFXPreferred();
		const bool preferSoftFX = !mbAccelScreenFX || !canAccelFX || mpVideoTaps || !mRawFrameCallbacks.IsEmpty();

		// Horizontal resolution is doubled (640ish) if VBXE or high artifacting is enabled.
		const bool use14MHz = (mpVBXE != NULL) || mArtifactMode == kArtifactNTSCHi || mArtifactMode == kArtifactPALHi || mArtifactMode == kArtifactAutoHi;

		bool useArtifacting = mArtifactMode != kArtifactNone;
		bool usePalArtifacting = mArtifactMode == kArtifactPAL || mArtifactMode == kArtifactPALHi || ((mArtifactMode == kArtifactAuto || mArtifactMode == kArtifactAutoHi) && mbPALMode);
		bool useHighArtifacting = mArtifactMode == kArtifactNTSCHi || mArtifactMode == kArtifactPALHi || mArtifactMode == kArtifactAutoHi;
		bool useAccelPALBlending = false;

		if (!preferSoftFX && usePalArtifacting && !useHighArtifacting) {
			useAccelPALBlending = true;
			useArtifacting = false;
		}

		const bool rgb32 = useArtifacting || mbBlendMode || mbSoftScanlinesEnabledThisFrame || use14MHz;
		const ATColorParams& params = mActiveColorParams;
		const bool outputCorrection = (params.mGammaCorrect != 1.0f) || params.mColorMatchingMode != ATColorMatchingMode::None;

		mbPALThisFrame = mbPALMode;
		mbOverscanPALExtendedThisFrame = mbPALThisFrame && mbOverscanPALExtended;
		mb14MHzThisFrame = use14MHz;
		mbInterlaceEnabledThisFrame = mbInterlaceEnabled;

		// Soft scanlines only support noninterlaced operation. Accel scanlines support both.
		mbSoftScanlinesEnabledThisFrame = preferSoftFX && mbScanlinesEnabled && !mbInterlaceEnabled;
		const bool useAccelScanlines = !preferSoftFX && mbScanlinesEnabled;

		mbPostProcessThisFrame = (useArtifacting || mbBlendMode || mbSoftScanlinesEnabledThisFrame) && !mpVBXE;
		mbIncludeHBlankThisFrame = mOverscanMode == kOverscanFull || mAnalysisMode || mbForcedBorder;
		
		const auto& ap = mpArtifactingEngine->GetArtifactingParams();
		bool useDistortion = canAccelFX && ap.mDistortionViewAngleX > 0;
		bool useBloom = canAccelFX && ap.mbEnableBloom;
		mbScreenFXEnabledThisFrame = (!preferSoftFX && (useAccelScanlines || (rgb32 && outputCorrection) || useAccelPALBlending)) || useDistortion || useBloom;

		// needed for mRawFrame below even if no postprocessing
		mPreArtifactFrame.h = mbOverscanPALExtendedThisFrame ? 312 : 262;

		if (mbPostProcessThisFrame || (mpVBXE && (usePalArtifacting || mbBlendMode))) {
			mpArtifactingEngine->BeginFrame(usePalArtifacting, useArtifacting, useHighArtifacting, mbBlendModeLastFrame, mbBlendMode, mbScreenFXEnabledThisFrame);
		}

		int format = rgb32 ? nsVDPixmap::kPixFormat_XRGB8888 : nsVDPixmap::kPixFormat_Pal8;

		// compute size of full frame buffer, including overscan
		int frameWidth = 456;
		if (use14MHz)
			frameWidth *= 2;

		int frameHeight = mbOverscanPALExtendedThisFrame ? 312 : 262;
		
		const bool dualFieldFrame = mbInterlaceEnabledThisFrame || mbSoftScanlinesEnabledThisFrame;
		if (dualFieldFrame)
			frameHeight *= 2;

		// check if we need to reinitialize the frame bitmap
		if (fb->mBuffer.format != format || fb->mBuffer.w != frameWidth || fb->mBuffer.h != frameHeight) {
			VDPixmapLayout layout;
			VDPixmapCreateLinearLayout(layout, format, frameWidth, frameHeight, 16);

			// Add a little extra width on the end so we can go over slightly with MASKMOVDQU on SSE2
			// routines.
			fb->mBuffer.init(layout, 32);

			memset(fb->mBuffer.base(), 0, fb->mBuffer.size());
		}

		fb->mbDualFieldFrame = dualFieldFrame;
		fb->mPixmap = fb->mBuffer;
		fb->mPixmap.palette = useAccelPALBlending ? mSignedPalette : mPalette;
		fb->mpPalette = mPalette;

		mRawFrame = mPreArtifactFrame;

		if (!mbPostProcessThisFrame) {
			mRawFrame.data = fb->mBuffer.data;
			mRawFrame.pitch = fb->mBuffer.pitch;

			if (dualFieldFrame) {
				mRawFrame.pitch *= 2;

				if (mbInterlaceEnabledThisFrame && mbFieldPolarity)
					mRawFrame.data = (char *)mRawFrame.data + fb->mBuffer.pitch;
			}
		}

		// get visible area in color clocks
		vdrect32 scanArea = GetFrameScanArea();

		// In PAL extended mode, the top of the frame extends above scan 0 by our
		// numbering, so we must rebias the scan window here. The rendering code
		// similarly compensates by 16 scans.
		if (scanArea.top < 0) {
			scanArea.bottom -= scanArea.top;
			scanArea.top = 0;
		}

		// convert view area to hires pixels (320 res), our standard output image res.
		vdrect32 frameViewRect = scanArea;

		frameViewRect.left *= 2;
		frameViewRect.right *= 2;

		// double left/right if we're generating at double hires (14MHz instead of 7MHz)
		if (use14MHz) {
			frameViewRect.left *= 2;
			frameViewRect.right *= 2;
		}

		// convert the frame view rect to image view rect, which is the same except if the
		// image contains two fields (interlace or soft scanlines, but NOT accel scanlines)
		vdrect32 imageViewRect = frameViewRect;
		if (dualFieldFrame) {
			imageViewRect.top *= 2;
			imageViewRect.bottom *= 2;
		}

		// set pixmap on view area over framebuffer
		fb->mPixmap.w = imageViewRect.width();
		fb->mPixmap.h = imageViewRect.height();

		fb->mPixmap.data = (char *)fb->mPixmap.data + imageViewRect.left * (rgb32 ? 4 : 1) + fb->mPixmap.pitch * imageViewRect.top;

		// set up hardware screen FX
		if (mbScreenFXEnabledThisFrame) {
			fb->mpScreenFX = &fb->mScreenFX;

			fb->mScreenFX = {};
			fb->mScreenFX.mScanlineIntensity = useAccelScanlines ? mpArtifactingEngine->GetArtifactingParams().mScanlineIntensity : 0.0f;
			fb->mScreenFX.mPALBlendingOffset = useAccelPALBlending ? dualFieldFrame ? -2.0f : -1.0f : 0.0f;

			// Set color correction matrix and gamma. For 32-bit, we can and do want to hardware accelerate
			// this lookup if possible. If we're using a raw 8-bit frame, the color correction and gamma
			// correction is already baked into the palette for free and we should not apply it again.
			if (rgb32) {
				memcpy(fb->mScreenFX.mColorCorrectionMatrix, mColorMatchingMatrix, sizeof fb->mScreenFX.mColorCorrectionMatrix);
				fb->mScreenFX.mGamma = params.mGammaCorrect;
			} else {
				memset(fb->mScreenFX.mColorCorrectionMatrix, 0, sizeof fb->mScreenFX.mColorCorrectionMatrix);
				fb->mScreenFX.mGamma = 1.0f;
			}

			fb->mScreenFX.mDistortionX = ap.mDistortionViewAngleX;
			fb->mScreenFX.mDistortionYRatio = ap.mDistortionYRatio;

			if (ap.mbEnableBloom) {
				fb->mScreenFX.mBloomThreshold = ap.mBloomThreshold;
				fb->mScreenFX.mBloomRadius = use14MHz ? ap.mBloomRadius * 2.0f : ap.mBloomRadius;
				fb->mScreenFX.mBloomDirectIntensity = ap.mBloomDirectIntensity;
				fb->mScreenFX.mBloomIndirectIntensity = ap.mBloomIndirectIntensity;

				if (ap.mbBloomScanlineCompensation && fb->mScreenFX.mScanlineIntensity) {
					const float i1 = 1.0f;
					const float i2 = fb->mScreenFX.mScanlineIntensity;
					const float i3 = 0.5f * (i1 + i2);
					fb->mScreenFX.mBloomDirectIntensity /= i3*i3;
					fb->mScreenFX.mBloomIndirectIntensity /= i3*i3;
				}
			} else {
				fb->mScreenFX.mBloomThreshold = 0;
				fb->mScreenFX.mBloomRadius = 0;
				fb->mScreenFX.mBloomDirectIntensity = 0;
				fb->mScreenFX.mBloomIndirectIntensity = 0;
			}
		} else {
			fb->mpScreenFX = nullptr;
		}

		mPreArtifactFrameVisibleY1 = frameViewRect.top;
		mPreArtifactFrameVisibleY2 = frameViewRect.bottom;

		fb->mViewX1 = imageViewRect.left;
		fb->mViewY1 = imageViewRect.top;
		fb->mbIncludeHBlank = mbIncludeHBlankThisFrame;

		// copy over previous field
		if (mbInterlaceEnabledThisFrame) {
			VDPixmap dstField(VDPixmapExtractField(mpFrame->mPixmap, !mbFieldPolarity));

			if (mpLastFrame &&
				mpLastFrame->mPixmap.w == mpFrame->mPixmap.w &&
				mpLastFrame->mPixmap.h == mpFrame->mPixmap.h &&
				mpLastFrame->mPixmap.format == mpFrame->mPixmap.format &&
				mbFieldPolarity != mbLastFieldPolarity) {
				VDPixmap srcField(VDPixmapExtractField(mpLastFrame->mPixmap, !mbFieldPolarity));

				VDPixmapBlt(dstField, srcField);
			} else {
				VDPixmap srcField(VDPixmapExtractField(mpFrame->mPixmap, mbFieldPolarity));
				
				VDPixmapBlt(dstField, srcField);
			}

			mbLastFieldPolarity = mbFieldPolarity;
		}
	}

	mFrameTimestamp = mpConn->GTIAGetTimestamp64();

	// Reset Y to avoid weirdness in immediate updates from being between BeginFrame() and
	// the first BeginScanline().
	mY = 0;

	return true;
}

void ATGTIAEmulator::BeginScanline(int y, bool hires) {
	// Flush remaining register changes (required for PRIOR to interact properly with hires)
	//
	// Note that we must use a cycle offset of -1 here because we haven't done DMA fetches
	// for this cycle yet!
	Sync(-1);

	mbMixedRendering = false;
	mbANTICHiresMode = hires;
	mbHiresMode = hires && !(mActivePRIOR & 0xc0);
	mbGTIADisableTransition = false;

	if ((unsigned)(y - 8) < 240)
		mbScanlinesWithHiRes[y - 8] = mbHiresMode;

	if (mpVBXE) {
		if (y == 8)
			mpVBXE->BeginFrame();
		else if (y == 248)
			mpVBXE->EndFrame();
	}

	mpDst = NULL;
	
	mY = y;
	mbPMRendered = false;

	if (mpFrame) {
		ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);

		int yw = y;
		int h = mRawFrame.h;

		if (mbOverscanPALExtendedThisFrame) {
			// What we do here is wrap the last 16 lines back up to the top of
			// the display. This isn't correct, as it causes those lines to
			// lead by a frame, but it at least solves the vertical position
			// issue.
			if (yw >= 312 - 16)
				yw -= 312 - 16;
			else
				yw += 16;
		}

		if (yw < h)
			mpDst = (uint8 *)mRawFrame.data + mRawFrame.pitch * yw;

		if (y == 248 && !mRawFrameCallbacks.IsEmpty()) {
			VDPixmap px(mRawFrame);

			px.w = 376;
			px.h = 240;
			px.data = (char *)px.data + px.pitch * 8;

			mRawFrameCallbacks.NotifyAll([&](const ATGTIARawFrameFn *fn) { (*fn)(px); });
		}
	}

	memset(mMergeBuffer, 0, sizeof mMergeBuffer);
	memset(mAnticData, 0, sizeof mAnticData);

	if (mpVBXE)
		mpVBXE->BeginScanline((uint32*)mpDst, mMergeBuffer, mAnticData, mbHiresMode);
	else if (mpDst) {
		mpRenderer->SetVBlank((uint32)(y - 8) >= 240);
		mpRenderer->BeginScanline(mpDst, mMergeBuffer, mAnticData, mbHiresMode);
	}
}

void ATGTIAEmulator::EndScanline(uint8 dlControl, bool pfrendered) {
	// flush any remaining changes
	Sync();

	if (mpDst) {
		if (mpVBXE)
			mpVBXE->RenderScanline(222, pfrendered || mbPMRendered);
		else
			mpRenderer->RenderScanline(222, pfrendered, mbPMRendered, mbMixedRendering);
	}

	if (mpVBXE)
		mpVBXE->EndScanline();
	else
		mpRenderer->EndScanline();

	// move down buffers as necessary and offset all pending render changes by -scanline
	if (mRCIndex >= 64) {
		mRegisterChanges.erase(mRegisterChanges.begin(), mRegisterChanges.begin() + mRCIndex);
		mRCCount -= mRCIndex;
		mRCIndex = 0;
	}

	for(int i=mRCIndex; i<mRCCount; ++i) {
		mRegisterChanges[i].mPos -= 228;
	}

	for(int i=0; i<8; ++i) {
		Sprite& sprite = mSprites[i];

		sprite.mLastSync -= 228;

		// make sure the sprites don't get too far behind
		if (sprite.mLastSync < -10000)
			sprite.Sync(-2);

		for(SpriteImage *image = sprite.mpImageHead; image; image = image->mpNext) {
			image->mX1 -= 228;
			image->mX2 -= 228;
		}

		// delete any sprites that are too old (this can happen in vblank)
		while(sprite.mpImageHead && sprite.mpImageHead->mX2 < 34) {
			SpriteImage *next = sprite.mpImageHead->mpNext;
			FreeSpriteImage(sprite.mpImageHead);
			sprite.mpImageHead = next;

			if (!next)
				sprite.mpImageTail = NULL;
		}

		// check if the sprite is stuck -- if so, continue extending the current image
		if (sprite.mpImageTail && sprite.mState.mSizeMode == 2 && (sprite.mState.mShiftState == 1 || sprite.mState.mShiftState == 2)) {
			sprite.mpImageTail->mX1 = -2;
			sprite.mpImageTail->mX2 = 1024;
		}
	}

	// We have to restart at -2 instead of 0 because GTIA runs two color clocks head of ANTIC
	// for timing purposes.
	mLastSyncX = -2;

	if (!mpDst)
		return;

	switch(mAnalysisMode) {
		case kAnalyzeNone:
			break;
		case kAnalyzeColors:
			for(int i=0; i<9; ++i)
				mpDst[i*2+0] = mpDst[i*2+1] = ((const uint8 *)mPMColor)[i];
			break;
		case kAnalyzeDList:
			mpDst[0] = mpDst[1] = (dlControl & 0x80) ? 0x1f : 0x00;
			mpDst[2] = mpDst[3] = (dlControl & 0x40) ? 0x3f : 0x00;
			mpDst[4] = mpDst[5] = (dlControl & 0x20) ? 0x5f : 0x00;
			mpDst[6] = mpDst[7] = (dlControl & 0x10) ? 0x7f : 0x00;
			mpDst[8] = mpDst[9] = mpDst[10] = mpDst[11] = ((dlControl & 0x0f) << 4) + 15;
			break;
	}
}

void ATGTIAEmulator::UpdatePlayer(bool odd, int index, uint8 byte) {
	if (mGRACTL & 2) {
		if (odd || !(mVDELAY & (0x10 << index))) {
			const uint8 xpos = mpConn->GTIAGetXClock();
			AddRegisterChange(xpos + 3, 0x0D + index, byte);
		}
	}
}

void ATGTIAEmulator::UpdateMissile(bool odd, uint8 byte) {
	if (mGRACTL & 1) {
		const uint8 xpos = mpConn->GTIAGetXClock();
		AddRegisterChange(xpos + 3, 0x20, byte);
	}
}

void ATGTIAEmulator::UpdatePlayfield160(uint32 x, uint8 byte) {
	VDASSERT(x < 114);

	uint8 *dst = &mMergeBuffer[x*2];

	dst[0] = (byte >>  4) & 15;
	dst[1] = (byte      ) & 15;
}

void ATGTIAEmulator::UpdatePlayfield160(uint32 x, const uint8 *__restrict src, uint32 n) {
	if (!n)
		return;

	VDASSERT(x < 114);
	uint8 *__restrict dst = &mMergeBuffer[x*2];

#ifdef VD_CPU_X86
	if (SSE2_enabled) {
		atasm_update_playfield_160_sse2(dst, src, n);
		return;
	}
#endif

#ifdef VD_CPU_AMD64
	atasm_update_playfield_160_sse2(dst, src, n);
#elif defined(VD_CPU_ARM64)
	atasm_update_playfield_160_neon(dst, src, n);
#else
	do {
		const uint8 byte = *src++;
		dst[0] = (byte >>  4) & 15;
		dst[1] = (byte      ) & 15;
		dst += 2;
	} while(--n);
#endif
}

void ATGTIAEmulator::UpdatePlayfield320(uint32 x, uint8 byte) {
	uint8 *dstx = &mMergeBuffer[x];
	dstx[0] = PF2;
	dstx[1] = PF2;
	
	VDASSERT(x < 228);

	uint8 *dst = &mAnticData[x];
	dst[0] = (byte >> 2) & 3;
	dst[1] = (byte >> 0) & 3;
}

void ATGTIAEmulator::UpdatePlayfield320(uint32 x, const uint8 *src, uint32 n) {
	VDASSERT(x < 228);

#if VD_CPU_X86 || VD_CPU_X64
	atasm_update_playfield_320_sse2(mMergeBuffer, mAnticData, src, x, n);
#else
	memset(&mMergeBuffer[x], PF2, n*2);
	
	uint8 *VDRESTRICT dst = &mAnticData[x];
	do {
		const uint8 byte = *src++;
		dst[0] = (byte >> 2) & 3;
		dst[1] = (byte >> 0) & 3;
		dst += 2;
	} while(--n);
#endif
}

namespace {
	void Convert160To320(int x1, int x2, uint8 *dst, const uint8 *src) {
		static const uint8 kPriTable[16]={
			0,		// BAK
			0,		// PF0
			1,		// PF1
			1,		// PF01
			2,		// PF2
			2,		// PF02
			2,		// PF12
			2,		// PF012
			3,		// PF3
			3,		// PF03
			3,		// PF13
			3,		// PF013
			3,		// PF23
			3,		// PF023
			3,		// PF123
			3,		// PF0123
		};

		for(int x=x1; x<x2; ++x)
			dst[x] = kPriTable[src[x]];
	}

	void Convert320To160(int x1, int x2, uint8 *dst, const uint8 *src) {
		for(int x=x1; x<x2; ++x) {
			uint8 c = src[x];

			if (dst[x] & PF2)
				dst[x] = 1 << c;
		}
	}
}

void ATGTIAEmulator::Sync(int offset) {
	mpConn->GTIARequestAnticSync(offset);

	int xend = (int)mpConn->GTIAGetXClock() + 2;

	if (xend > 228)
		xend = 228;

	SyncTo(xend);
}

void ATGTIAEmulator::SyncTo(int xend) {
	int x1 = mLastSyncX;

	if (x1 >= xend)
		return;

	// render spans and process register changes
	do {
		int x2 = xend;

		if (mRCIndex < mRCCount) {
			const RegisterChange *rc0 = &mRegisterChanges[mRCIndex];
			const RegisterChange *rc = rc0;

			do {
				int xchg = rc->mPos;
				if (xchg > x1) {
					if (x2 > xchg)
						x2 = xchg;
					break;
				}

				++rc;
			} while(++mRCIndex < mRCCount);

			UpdateRegisters(rc0, (int)(rc - rc0));
		}

		if (x2 > x1) {
			if (mbSpritesActive)
				GenerateSpriteImages(x1, x2);

			Render(x1, x2);
			x1 = x2;
		}
	} while(x1 < xend);

	mLastSyncX = x1;
}

void ATGTIAEmulator::Render(int x1, int x2) {
	if (mVBlankMode == kVBlankModeOn)
		return;

	// determine displayed range
	int xc1 = x1;
	if (xc1 < 34)
		xc1 = 34;

	int xc2 = x2;
	if (xc2 > 222)
		xc2 = 222;

	if (xc2 <= xc1)
		return;

	// convert modes if necessary
	bool needHires = mbHiresMode || (mActivePRIOR & 0xC0);
	if (needHires != mbANTICHiresMode) {
		int xc1start = xc1;

		// We need to convert one clock back to support the case of a mode 8/L -> 10 transition;
		// we handle PRIOR changes one cycle later here than in the renderer, but the renderer
		// needs the converted result one half cycle (1cc) in.
		if (!mbMixedRendering) {
			mbMixedRendering = true;
			--xc1start;
		}

		if (mbANTICHiresMode)
			Convert320To160(xc1start, xc2, mMergeBuffer, mAnticData);
		else
			Convert160To320(xc1start, xc2, mAnticData, mMergeBuffer);
	}

	static const uint8 kPFTable[16]={
		0, 0, 0, 0, PF0, PF1, PF2, PF3,
		0, 0, 0, 0, PF0, PF1, PF2, PF3,
	};

	static const uint8 kPFMask[16]={
		0xF0, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
		0xFF, 0xFF, 0xFF, 0xFF,
	};

	if (xc1 < xc2) {
		switch(mActivePRIOR & 0xC0) {
			case 0x00:
				break;
			case 0x80:
				if (mbANTICHiresMode) {
					const uint8 *__restrict ad = &mAnticData[(xc1 - 1) & ~1];
					uint8 *__restrict dst = &mMergeBuffer[xc1];

					int w = xc2 - xc1;
					if (!(xc1 & 1)) {
						uint8 c = ad[0]*4 + ad[1];
						ad += 2;

						*dst++ = kPFTable[c];
						--w;
					}

					int w2 = w >> 1;
					while(w2--) {
						uint8 c = ad[0]*4 + ad[1];
						ad += 2;

						dst[0] = dst[1] = kPFTable[c];
						dst += 2;
					}

					if (w & 1) {
						uint8 c = ad[0]*4 + ad[1];
						*dst++ = kPFTable[c];
					}
				} else {
					const uint8 *__restrict ad = &mAnticData[(xc1 - 1) & ~1];
					uint8 *__restrict dst = &mMergeBuffer[xc1];

					int w = xc2 - xc1;
					if (!(xc1 & 1)) {
						uint8 c = ad[0]*4 + ad[1];
						ad += 2;

						*dst = kPFTable[c] & kPFMask[dst[-1] & 15];
						++dst;
						--w;
					}

					int w2 = w >> 1;
					while(w2--) {
						uint8 c = ad[0]*4 + ad[1];
						ad += 2;

						dst[0] = dst[1] = kPFTable[c] & kPFMask[dst[0] & 15];
						dst += 2;
					}

					if (w & 1) {
						uint8 c = ad[0]*4 + ad[1];
						*dst = kPFTable[c] & kPFMask[dst[0] & 15];
					}
				}
				break;
			case 0x40:
			case 0xC0:
				memset(mMergeBuffer + xc1, 0, (xc2 - xc1));
				break;
		}
	}

	if (mbGTIADisableTransition) {
		mbGTIADisableTransition = false;

		// The effects of the GTIA ANx latches are still in effect, which causes the low
		// two bits to be repeated.

		if (x1 >= xc1 && mMergeBuffer[x1])
			mMergeBuffer[x1] = kPFTable[4 + mAnticData[x1 - 1]];
	}

	// flush player images
	for(int i=0; i<4; ++i) {
		Sprite& sprite = mSprites[i];

		// check if we have any sprite images
		if (!sprite.mpImageHead)
			continue;

		// expire old sprite images
		for(;;) {
			if (sprite.mpImageHead->mX2 > xc1)
				break;

			SpriteImage *next = sprite.mpImageHead->mpNext;
			FreeSpriteImage(sprite.mpImageHead);
			sprite.mpImageHead = next;

			if (!next) {
				sprite.mpImageTail = NULL;
				break;
			}
		}

		// render out existing images
		for(SpriteImage *image = sprite.mpImageHead; image; image = image->mpNext) {
			if (image->mX1 >= xc2)
				break;

			if (image->mX1 < xc1) {
				image->mState.Advance((uint32)(xc1 - image->mX1));
				image->mX1 = xc1;
			}

			int minx2 = image->mX2;
			if (minx2 > xc2)
				minx2 = xc2;

			if (mbHiresMode)
				mPlayerCollFlags[i] |= image->mState.Generate(minx2 - image->mX1, P0 << i, mMergeBuffer + image->mX1, mAnticData + image->mX1);
			else
				mPlayerCollFlags[i] |= image->mState.Generate(minx2 - image->mX1, P0 << i, mMergeBuffer + image->mX1); 

			mbPMRendered = true;
		}
	}

	// Flush missile images.
	//
	// We _have_ to do this as two pass as the missiles will start overlapping with
	// the players once the scanout has started. Therefore, we do detection over all
	// missiles first before rendering any of them.

	for(int i=0; i<4; ++i) {
		Sprite& sprite = mSprites[i + 4];

		// check if we have any sprite images
		if (!sprite.mpImageHead)
			continue;

		// expire old sprite images
		for(;;) {
			if (sprite.mpImageHead->mX2 > xc1)
				break;

			SpriteImage *next = sprite.mpImageHead->mpNext;
			FreeSpriteImage(sprite.mpImageHead);
			sprite.mpImageHead = next;

			if (!next) {
				sprite.mpImageTail = NULL;
				break;
			}
		}

		// render out existing images
		for(SpriteImage *image = sprite.mpImageHead; image; image = image->mpNext) {
			if (image->mX1 >= xc2)
				break;

			if (image->mX1 < xc1) {
				image->mState.Advance((uint32)(xc1 - image->mX1));
				image->mX1 = xc1;
			}

			int minx2 = image->mX2;
			if (minx2 > xc2)
				minx2 = xc2;

			if (mbHiresMode)
				mMissileCollFlags[i] |= image->mState.Detect(minx2 - image->mX1, mMergeBuffer + image->mX1, mAnticData + image->mX1);
			else
				mMissileCollFlags[i] |= image->mState.Detect(minx2 - image->mX1, mMergeBuffer + image->mX1);
		}
	}

	for(int i=0; i<4; ++i) {
		Sprite& sprite = mSprites[i + 4];

		// render out existing images
		for(SpriteImage *image = sprite.mpImageHead; image; image = image->mpNext) {
			if (image->mX1 >= xc2)
				break;

			int minx2 = image->mX2;
			if (minx2 > xc2)
				minx2 = xc2;

			image->mState.Generate(minx2 - image->mX1, (mActivePRIOR & 0x10) ? PF3 : (P0 << i), mMergeBuffer + image->mX1); 
			mbPMRendered = true;
		}
	}
}

void ATGTIAEmulator::RenderActivityMap(const uint8 *src) {
	if (!mpFrame)
		return;

	ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);
	const VDPixmap& pxdst = mbPostProcessThisFrame ? mPreArtifactFrame : fb->mBuffer;
	uint8 *dst = (uint8 *)pxdst.data;

	// if PAL extended is enabled, there are 16 lines wrapped from the bottom to the top
	// of the framebuffer that we must skip and loop back to
	if (mbOverscanPALExtendedThisFrame)
		dst += 16 * pxdst.pitch;

	int h = this->mbOverscanPALExtendedThisFrame ? 312 : 262;

	for(int y=0; y<h; ++y) {
		uint8 *dst2 = dst;

		for(int x=0; x<114; ++x) {
			uint8 add = src[x] & 1 ? 0x08 : 0x00;
			dst2[0] = (dst2[0] & 0xf0) + ((dst2[0] & 0xf) >> 1) + add;
			dst2[1] = (dst2[1] & 0xf0) + ((dst2[1] & 0xf) >> 1) + add;
			dst2[2] = (dst2[2] & 0xf0) + ((dst2[2] & 0xf) >> 1) + add;
			dst2[3] = (dst2[3] & 0xf0) + ((dst2[3] & 0xf) >> 1) + add;
			dst2 += 4;
		}

		src += 114;
		dst += pxdst.pitch;

		if (y == 312 - 16 - 1)
			dst = (uint8 *)pxdst.data;
	}
}

void ATGTIAEmulator::UpdateScreen(bool immediate, bool forceAnyScreen) {
	if (!mpFrame) {
		if (forceAnyScreen && mpLastFrame)
			mpDisplay->SetSourcePersistent(true, mpLastFrame->mPixmap);

		mbLastFieldPolarity = mbFieldPolarity;
		return;
	}

	ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);

	if (immediate) {
		const VDPixmap& pxdst = mbPostProcessThisFrame ? mPreArtifactFrame : fb->mBuffer;
		uint32 x = mpConn->GTIAGetXClock();

		Sync();

		if (mpDst) {
			if (mpVBXE)
				mpVBXE->RenderScanline(x, true);
			else
				mpRenderer->RenderScanline(x, true, mbPMRendered, mbMixedRendering);
		}

		uint32 y = mY + 1;

		if (mbOverscanPALExtendedThisFrame) {
			// What we do here is wrap the last 16 lines back up to the top of
			// the display. This isn't correct, as it causes those lines to
			// lead by a frame, but it at least solves the vertical position
			// issue.
			if (y >= 312 - 16)
				y -= 312 - 16;
			else
				y += 16;
		}

		if (!mbPostProcessThisFrame && mbInterlaceEnabledThisFrame) {
			y += y;

			if (mbFieldPolarity)
				++y;
		}

		if (y < (uint32)pxdst.h) {
			uint8 *row = (uint8 *)pxdst.data + y*pxdst.pitch;

			if (mpVBXE) {
				VDMemset32(row, 0x00, 4*x);
				VDMemset32(row + x*4*4, 0xFFFF00, 912 - 4*x);
			} else {
				VDMemset8(row, 0x00, 2*x);
				VDMemset8(row + x*2, 0xFF, 464 - 2*x);
			}
		}

		if (!mbFrameCopiedFromPrev && !mbPostProcessThisFrame && mpLastFrame && y+1 < (uint32)pxdst.h) {
			mbFrameCopiedFromPrev = true;

			VDPixmapBlt(fb->mBuffer, 0, y+1, static_cast<ATFrameBuffer *>(&*mpLastFrame)->mBuffer, 0, y+1, pxdst.w, pxdst.h - (y + 1));
		}

		ApplyArtifacting(true);

		// frame is incomplete and has some past data, so just suppress all lores optimizations
		std::fill(std::begin(fb->mbScanlineHasHires), std::end(fb->mbScanlineHasHires), true);

		mpDisplay->SetSourcePersistent(true, mpFrame->mPixmap, true, mpFrame->mpScreenFX, mpFrame->mpScreenFXEngine);
	} else {
		ApplyArtifacting(false);

		if (mpVideoTaps) {
			for(auto *p : *mpVideoTaps)
				p->WriteFrame(mpFrame->mPixmap, mFrameTimestamp, mpConn->GTIAGetTimestamp64());
		}

		if (mbTurbo)
			fb->mFlags |= IVDVideoDisplay::kDoNotWait;
		else
			fb->mFlags &= ~IVDVideoDisplay::kDoNotWait;

		std::fill(std::begin(fb->mbScanlineHasHires), std::end(fb->mbScanlineHasHires), false);

		sint32 dsty1 = 0;
		sint32 dsty2 = mPreArtifactFrameVisibleY2 - mPreArtifactFrameVisibleY1;
		const sint32 vstart = mbOverscanPALExtendedThisFrame ? 24 : 8;
		sint32 srcy1 = (sint32)mPreArtifactFrameVisibleY1 - vstart;
		sint32 srcy2 = (sint32)mPreArtifactFrameVisibleY2 - vstart;

		if (dsty1 < 0) {
			srcy1 -= dsty1;
			dsty1 = 0;
		}

		if (srcy1 < 0) {
			dsty1 -= srcy1;
			srcy1 = 0;
		}

		if (srcy2 > 240) {
			dsty2 -= (240 - srcy2);
			srcy2 = 240;
		}

		if (dsty2 > (sint32)vdcountof(fb->mbScanlineHasHires)) {
			sint32 offset = dsty2 - (sint32)vdcountof(fb->mbScanlineHasHires);
			dsty2 = (sint32)vdcountof(fb->mbScanlineHasHires);
			srcy2 -= offset;
		}

		if (dsty2 > dsty1)
			std::copy(mbScanlinesWithHiRes + srcy1, mbScanlinesWithHiRes + srcy2, fb->mbScanlineHasHires + dsty1);

		mpDisplay->PostBuffer(fb);

		mpLastFrame = fb;
		mbBlendModeLastFrame = mbBlendMode;

		mpFrame = NULL;
	}
}

void ATGTIAEmulator::RecomputePalette() {
	mActiveColorParams = mColorSettings.mbUsePALParams && mbPALMode ? mColorSettings.mPALParams : mColorSettings.mNTSCParams;

	const ATColorParams& params = mActiveColorParams;
	const bool palQuirks = params.mbUsePALQuirks;
	float angle = (params.mHueStart + (palQuirks ? -33.0f : 0.0f)) * (nsVDMath::kfTwoPi / 360.0f);
	float angleStep = params.mHueRange * (nsVDMath::kfTwoPi / (360.0f * 15.0f));
	float gamma = 1.0f / params.mGammaCorrect;

	float lumaRamp[16];

	ATComputeLumaRamp(params.mLumaRampMode, lumaRamp);

	// I/Q -> RGB coefficients
	//
	// There are a ton of matrices posted on the Internet that all vary in
	// small amounts due to roundoff. SMPTE 170M-2004 gives the best full derivation
	// of all the equations. Here's the gist:
	//
	// - Y has two definitions:
	//	 Y = 0.30R + 0.59G + 0.11B (NTSC)
	//   Y = 0.299R + 0.587G + 0.114B (SMPTE 170M-2004)
	//   SMPTE 170M-2004 Annex A indicates that the NTSC specification is
	//   rounded off from the original NTSC derivation.
	// - Also per SMPTE 170M-2004, R-Y and B-Y are scaled by 0.877283... and
	//   0.492111... to place yellow and cyan 75% bars at 100 IRE. This
	//   gives V = 0.877283(R-Y) and U=0.492111(B-Y).
	// - I-Q is rotated 33deg from V-U (*not* U-V).
	//
	// Final Scilab derivation:
	//
	// R=[1 0 0]; G=[0 1 0]; B=[0 0 1];
	// Y=0.299*R+0.587*G+0.114*B;
	// U=0.492111*(B-Y); V=0.877283*(R-Y);
	// cs=cosd(33); sn=sind(33);
	// I=V*cs-U*sn; Q=U*cs+V*sn;
	// inv([Y;I;Q])
	//
	// One benefit of using the precise values is that the angles make sense: B-Y
	// and R-Y are at 123 and 33 degrees in I-Q space, respectively, with exactly
	// 90 degrees between them.
	//
	// The angle and gain of each of these vectors are adjustable. This is equivalent
	// to arbitrarily setting all six elements of the matrix by polar/cartesian
	// equivalence.
	//
	vdfloat2 co_r { 0.956f, 0.621f };
	vdfloat2 co_g { -0.272f, -0.647f };
	vdfloat2 co_b { -1.107f, 1.704f };

	co_r = vdfloat2x2::rotation(params.mRedShift * (nsVDMath::kfPi / 180.0f)) * co_r * params.mRedScale;
	co_g = vdfloat2x2::rotation(params.mGrnShift * (nsVDMath::kfPi / 180.0f)) * co_g * params.mGrnScale;
	co_b = vdfloat2x2::rotation(params.mBluShift * (nsVDMath::kfPi / 180.0f)) * co_b * params.mBluScale;

	static constexpr vdfloat3x3 fromNTSC = vdfloat3x3 {
		{ 0.6068909f, 0.1735011f, 0.2003480f },
		{ 0.2989164f, 0.5865990f, 0.1144845f },
		{ 0.0000000f, 0.0660957f, 1.1162243f },
	}.transpose();

	static constexpr vdfloat3x3 fromPAL = vdfloat3x3 {
		{ 0.4306190f, 0.3415419f, 0.1783091f },
		{ 0.2220379f, 0.7066384f, 0.0713236f },
		{ 0.0201853f, 0.1295504f, 0.9390944f },
	}.transpose();

	static constexpr vdfloat3x3 tosRGB = vdfloat3x3 {
		{  3.2404542f, -1.5371385f, -0.4985314f },
		{ -0.9692660f,  1.8760108f,  0.0415560f },
		{  0.0556434f, -0.2040259f,  1.0572252f },
	}.transpose();

	static constexpr vdfloat3x3 toAdobeRGB = vdfloat3x3 {
		{  2.0413690f, -0.5649464f, -0.3446944f },
		{ -0.9692660f,  1.8760108f,  0.0415560f },
		{  0.0134474f, -0.1183897f,  1.0154096f },
	}.transpose();

	vdfloat3x3 mx;
	bool useMatrix = false;
	const vdfloat3x3 *toMat = nullptr;

	switch(params.mColorMatchingMode) {
		case ATColorMatchingMode::SRGB:
			toMat = &tosRGB;
			break;

		case ATColorMatchingMode::AdobeRGB:
			toMat = &toAdobeRGB;
			break;
	}

	if (toMat) {
		const vdfloat3x3 *fromMat = palQuirks ? &fromPAL : &fromNTSC;

		mx = (*fromMat) * (*toMat);

		useMatrix = true;
	}

	const float nativeGamma = 2.2f;

	uint32 *dst = mPalette;
	uint32 *dst2 = mSignedPalette;
	for(int hue=0; hue<16; ++hue) {
		float i = 0;
		float q = 0;

		if (hue) {
			if (palQuirks) {
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

				const float *co = kPALPhaseLookup[hue - 1];

				float angle2 = angle + angleStep * (co[0] + 3.0f);
				float angle3 = angle + angleStep * (-co[2] - 3.0f);
				float i2 = cosf(angle2) * co[1];
				float q2 = sinf(angle2) * co[1];
				float i3 = cosf(angle3) * co[3];
				float q3 = sinf(angle3) * co[3];

				i = (i2 + i3) * (0.5f * params.mSaturation);
				q = (q2 + q3) * (0.5f * params.mSaturation);
			} else {
				i = params.mSaturation * cos(angle);
				q = params.mSaturation * sin(angle);
				angle += angleStep;
			}
		}

		const vdfloat2 iq { i, q };
		float cr = nsVDMath::dot(iq, co_r);
		float cg = nsVDMath::dot(iq, co_g);
		float cb = nsVDMath::dot(iq, co_b);

		for(int luma=0; luma<16; ++luma) {
			float y = params.mContrast * lumaRamp[luma] + params.mBrightness;

			float r = y + cr;
			float g = y + cg;
			float b = y + cb;

			if (useMatrix) {
				if (r < 0.0f) r = 0.0f;
				if (g < 0.0f) g = 0.0f;
				if (b < 0.0f) b = 0.0f;

				r = powf(r, nativeGamma);
				g = powf(g, nativeGamma);
				b = powf(b, nativeGamma);

				vdfloat3 rgb = vdfloat3 { r, g, b } * mx;

				if (params.mColorMatchingMode == ATColorMatchingMode::AdobeRGB) {
					r = (rgb.x < 0) ? 0.0f : powf(rgb.x, 1.0f / 2.2f);
					g = (rgb.y < 0) ? 0.0f : powf(rgb.y, 1.0f / 2.2f);
					b = (rgb.z < 0) ? 0.0f : powf(rgb.z, 1.0f / 2.2f);
				} else {
					r = (rgb.x < 0.0031308f) ? rgb.x * 12.92f : 1.055f * powf(rgb.x, 1.0f / 2.4f) - 0.055f;
					g = (rgb.y < 0.0031308f) ? rgb.y * 12.92f : 1.055f * powf(rgb.y, 1.0f / 2.4f) - 0.055f;
					b = (rgb.z < 0.0031308f) ? rgb.z * 12.92f : 1.055f * powf(rgb.z, 1.0f / 2.4f) - 0.055f;
				}
			}

			if (r > 0.0f)
				r = powf(r, gamma);

			if (g > 0.0f)
				g = powf(g, gamma);

			if (b > 0.0f)
				b = powf(b, gamma);

			r *= params.mIntensityScale;
			g *= params.mIntensityScale;
			b *= params.mIntensityScale;

			*dst++	= (VDClampedRoundFixedToUint8Fast((float)r) << 16)
					+ (VDClampedRoundFixedToUint8Fast((float)g) <<  8)
					+ (VDClampedRoundFixedToUint8Fast((float)b)      );

			float r2 = r * 127.0f / 255.0f + 64.0f / 255.0f;
			float g2 = g * 127.0f / 255.0f + 64.0f / 255.0f;
			float b2 = b * 127.0f / 255.0f + 64.0f / 255.0f;

			*dst2++	= (VDClampedRoundFixedToUint8Fast((float)r2) << 16)
					+ (VDClampedRoundFixedToUint8Fast((float)g2) <<  8)
					+ (VDClampedRoundFixedToUint8Fast((float)b2)      );
		}
	}

	if (mpVBXE)
		mpVBXE->SetDefaultPalette(mPalette);

	if (useMatrix) {
		vdfloat3x3 mx2 = mx;//.transpose();

		static_assert(sizeof(mColorMatchingMatrix) == sizeof(mx2));
		memcpy(mColorMatchingMatrix, &mx2, sizeof mColorMatchingMatrix);
	} else {
		memset(mColorMatchingMatrix, 0, sizeof mColorMatchingMatrix);
	}

	mpArtifactingEngine->SetColorParams(params, useMatrix ? &mx : nullptr);
}

uint8 ATGTIAEmulator::ReadByte(uint8 reg) {
	reg &= 0x1F;

	// fast registers
	switch(reg) {
		case 0x10:
		case 0x11:
		case 0x12:
		case 0x13:
			if (mbSECAMMode)
				UpdateSECAMTriggerLatch(reg - 0x10);

			return (mGRACTL & 4) ? mTRIGLatched[reg - 0x10] : mTRIG[reg - 0x10];
		case 0x14:
			return mbPALMode ? 0x01 : 0x0F;
		case 0x15:
		case 0x16:
		case 0x17:	// must return LSB0 set or Recycle hangs
		case 0x18:
		case 0x19:
		case 0x1A:
		case 0x1B:
		case 0x1C:
		case 0x1D:
		case 0x1E:
			return 0x0F;
		case 0x1F:		// $D01F CONSOL
			return ReadConsoleSwitches();
	}

	Sync();	

	switch(reg) {
		// missile-to-playfield collisions
		case 0x00:	return mMissileCollFlags[0] & 15 & mCollisionMask;
		case 0x01:	return mMissileCollFlags[1] & 15 & mCollisionMask;
		case 0x02:	return mMissileCollFlags[2] & 15 & mCollisionMask;
		case 0x03:	return mMissileCollFlags[3] & 15 & mCollisionMask;

		// player-to-playfield collisions
		case 0x04:	return mPlayerCollFlags[0] & 15 & mCollisionMask;
		case 0x05:	return mPlayerCollFlags[1] & 15 & mCollisionMask;
		case 0x06:	return mPlayerCollFlags[2] & 15 & mCollisionMask;
		case 0x07:	return mPlayerCollFlags[3] & 15 & mCollisionMask;

		// missile-to-player collisions
		case 0x08:	return (mMissileCollFlags[0] & mCollisionMask) >> 4;
		case 0x09:	return (mMissileCollFlags[1] & mCollisionMask) >> 4;
		case 0x0A:	return (mMissileCollFlags[2] & mCollisionMask) >> 4;
		case 0x0B:	return (mMissileCollFlags[3] & mCollisionMask) >> 4;

		// player-to-player collisions
		case 0x0C:	return (  ((mPlayerCollFlags[1] >> 3) & 0x02)	// 1 -> 0
							+ ((mPlayerCollFlags[2] >> 2) & 0x04)	// 2 -> 0
							+ ((mPlayerCollFlags[3] >> 1) & 0x08)) & (mCollisionMask >> 4);	// 3 -> 0

		case 0x0D:	return (  ((mPlayerCollFlags[1] >> 4) & 0x01)	// 1 -> 0
							+ ((mPlayerCollFlags[2] >> 3) & 0x04)	// 2 -> 1
							+ ((mPlayerCollFlags[3] >> 2) & 0x08)) & (mCollisionMask >> 4);	// 3 -> 1

		case 0x0E:	return (  ((mPlayerCollFlags[2] >> 4) & 0x03)	// 2 -> 0, 1
							+ ((mPlayerCollFlags[3] >> 3) & 0x08)) & (mCollisionMask >> 4);	// 3 -> 2

		case 0x0F:	return (  ((mPlayerCollFlags[3] >> 4) & 0x07)) & (mCollisionMask >> 4);	// 3 -> 0, 1, 2

		default:
//			__debugbreak();
			break;
	}
	return 0;
}

void ATGTIAEmulator::WriteByte(uint8 reg, uint8 value) {
	reg &= 0x1F;

	mState.mReg[reg] = value;

	switch(reg) {
		case 0x12:
		case 0x13:
		case 0x14:
		case 0x15:
			mPMColor[reg - 0x12] = value & 0xfe;
			if (mpVBXE)
				mpVBXE->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			else
				mpRenderer->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			break;

		case 0x16:
		case 0x17:
		case 0x18:
		case 0x19:
			mPFColor[reg - 0x16] = value & 0xfe;
			if (mpVBXE)
				mpVBXE->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			else
				mpRenderer->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			break;

		case 0x1A:
			mPFBAK = value & 0xfe;
			if (mpVBXE)
				mpVBXE->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			else
				mpRenderer->AddRegisterChange(mpConn->GTIAGetXClock() + 1, reg, value);
			break;

		case 0x1B:
			if (mbCTIAMode)
				value &= 0x3F;

			mPRIOR = value;
			break;

		case 0x1C:
			mVDELAY = value;
			return;

		case 0x1D:
			// We actually need to sync the latches when latching is *enabled*, since they
			// are always updated but only read when latching is enabled.
			if (~mGRACTL & value & 4) {
				if (mbSECAMMode) {
					for(int i=0; i<4; ++i)
						UpdateSECAMTriggerLatch(i);
				}

				mTRIGLatched[0] = mTRIG[0];
				mTRIGLatched[1] = mTRIG[1];
				mTRIGLatched[2] = mTRIG[2];
				mTRIGLatched[3] = mTRIG[3];
			}

			mGRACTL = value;
			return;

		case 0x1F:		// $D01F CONSOL
			{
				uint8 newConsol = value & 0x0F;
				uint8 delta = newConsol ^ mSwitchOutput;
				if (delta) {
					if (delta & 8)
						mpConn->GTIASetSpeaker(0 != (newConsol & 8));

					if (delta & 7)
						mpConn->GTIASelectController(newConsol & 3, (newConsol & 4) != 0);
				}
				mSwitchOutput = newConsol;
			}
			return;
	}

	const uint8 xpos = mpConn->GTIAGetXClock();

	switch(reg) {
		case 0x00:	// $D000 HPOSP0
		case 0x01:	// $D001 HPOSP1
		case 0x02:	// $D002 HPOSP2
		case 0x03:	// $D003 HPOSP3
		case 0x04:	// $D004 HPOSM0
		case 0x05:	// $D005 HPOSM1
		case 0x06:	// $D006 HPOSM2
		case 0x07:	// $D007 HPOSM3
			AddRegisterChange(xpos + 5, reg, value);
			break;
		case 0x08:	// $D008 SIZEP0
		case 0x09:	// $D009 SIZEP1
		case 0x0A:	// $D00A SIZEP2
		case 0x0B:	// $D00B SIZEP3
		case 0x0C:	// $D00C SIZEM
			AddRegisterChange(xpos + 3, reg, value);
			break;
		case 0x0D:	// $D00D GRAFP0
		case 0x0E:	// $D00E GRAFP1
		case 0x0F:	// $D00F GRAFP2
		case 0x10:	// $D010 GRAFP3
		case 0x11:	// $D011 GRAFM
			AddRegisterChange(xpos + 3, reg, value);
			break;

		case 0x1B:	// $D01B PRIOR

			// PRIOR is quite an annoying register, since several of its components
			// take effect at different stages in the color pipeline:
			//
			//	|			|			|			|			|			|
			//	|			B->	PFx	----B-> priA ---B-> priB ---B->color----B-> output
			//	|			|	decode	|	 ^		|	 ^		|  lookup	|     ^
			//	|			|			|  pri0-3	|   5th		|			|	  |
			//	|			|			|			|  player	| mode 9/11	B-----/
			//	|			|			|			|			|	enable	|
			//	|			|			|			|			|			|
			//	2			1			2			1			2			1

			AddRegisterChange(xpos + 2, reg, value);
			if (mpVBXE)
				mpVBXE->AddRegisterChange(xpos + 1, reg, value);
			else
				mpRenderer->AddRegisterChange(xpos + 1, reg, value);
			break;

		case 0x1E:	// $D01E HITCLR
			AddRegisterChange(xpos + 3, reg, value);
			break;
	}
}

void ATGTIAEmulator::ApplyArtifacting(bool immediate) {
	if (mpVBXE) {
		const bool doBlending = mArtifactMode == kArtifactPAL || (mArtifactMode == kArtifactAuto && mbPALThisFrame) || mbBlendMode;

		if (doBlending || mbSoftScanlinesEnabledThisFrame) {
			ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);
			char *dstrow = (char *)fb->mBuffer.data;
			ptrdiff_t dstpitch = fb->mBuffer.pitch;
			uint32 h = fb->mBuffer.h;

			if (mbInterlaceEnabledThisFrame) {
				if (mbFieldPolarity)
					dstrow += dstpitch;

				dstpitch *= 2;
				h >>= 1;
			} else if (mbSoftScanlinesEnabledThisFrame)
				h >>= 1;

			for(uint32 row=0; row<h; ++row) {
				uint32 *dst = (uint32 *)dstrow;

				if (doBlending)
					mpArtifactingEngine->Artifact32(row, dst, 912, immediate, mbIncludeHBlankThisFrame);

				if (mbSoftScanlinesEnabledThisFrame) {
					if (row)
						mpArtifactingEngine->InterpolateScanlines((uint32 *)(dstrow - dstpitch), (const uint32 *)(dstrow - 2*dstpitch), dst, 912);

					dstrow += dstpitch;
				}

				dstrow += dstpitch;
			}

			if (mbSoftScanlinesEnabledThisFrame) {
				mpArtifactingEngine->InterpolateScanlines(
					(uint32 *)(dstrow - dstpitch),
					(const uint32 *)(dstrow - 2*dstpitch),
					(const uint32 *)(dstrow - 2*dstpitch),
					912);
			}
		}

		return;
	}

	if (!mbPostProcessThisFrame)
		return;

	ATFrameBuffer *fb = static_cast<ATFrameBuffer *>(&*mpFrame);
	char *dstrow = (char *)fb->mBuffer.data;
	ptrdiff_t dstpitch = fb->mBuffer.pitch;

	if (mbInterlaceEnabledThisFrame) {
		if (mbFieldPolarity)
			dstrow += dstpitch;

		dstpitch *= 2;
	}

	const uint8 *srcrow = (const uint8 *)mPreArtifactFrame.data;
	ptrdiff_t srcpitch = mPreArtifactFrame.pitch;

	uint32 y1 = mPreArtifactFrameVisibleY1;
	uint32 y2 = mPreArtifactFrameVisibleY2;

	if (y1)
		--y1;

	if (mbSoftScanlinesEnabledThisFrame)
		dstrow += dstpitch * 2 * y1;
	else
		dstrow += dstpitch * y1;

	srcrow += srcpitch * y1;

	// In PAL extended mode, we wrap the bottom 16 lines back up to the top, thus
	// the weird adjustment here.
	const uint32 vstart = mbOverscanPALExtendedThisFrame ? 24 : 8;
	const uint32 w = mb14MHzThisFrame ? 912 : 456;

	for(uint32 row=y1; row<y2; ++row) {
		uint32 *dst = (uint32 *)dstrow;
		const uint8 *src = srcrow;

		uint32 relativeRow = row - vstart;

		mpArtifactingEngine->Artifact8(row, dst, src, relativeRow < 240 && mbScanlinesWithHiRes[relativeRow], immediate, mbIncludeHBlankThisFrame);

		if (mbSoftScanlinesEnabledThisFrame) {
			if (row > y1)
				mpArtifactingEngine->InterpolateScanlines((uint32 *)(dstrow - dstpitch), (const uint32 *)(dstrow - 2*dstpitch), dst, w);

			dstrow += dstpitch;
		}

		srcrow += srcpitch;
		dstrow += dstpitch;
	}

	if (mbSoftScanlinesEnabledThisFrame) {
		mpArtifactingEngine->InterpolateScanlines(
			(uint32 *)(dstrow - dstpitch),
			(const uint32 *)(dstrow - 2*dstpitch),
			(const uint32 *)(dstrow - 2*dstpitch),
			w);
	}
}

void ATGTIAEmulator::AddRegisterChange(uint8 pos, uint8 addr, uint8 value) {
	RegisterChanges::iterator it(mRegisterChanges.end()), itBegin(mRegisterChanges.begin() + mRCIndex);

	while(it != itBegin && it[-1].mPos > pos)
		--it;

	RegisterChange change;
	change.mPos = pos;
	change.mReg = addr;
	change.mValue = value;
	mRegisterChanges.insert(it, change);

	++mRCCount;
}

void ATGTIAEmulator::UpdateRegisters(const RegisterChange *rc, int count) {
	while(count--) {
		uint8 value = rc->mValue;

		switch(rc->mReg) {
			case 0x00:
			case 0x01:
			case 0x02:
			case 0x03:
			case 0x04:
			case 0x05:
			case 0x06:
			case 0x07:
				mSpritePos[rc->mReg] = value;
				break;

			case 0x08:
			case 0x09:
			case 0x0A:
			case 0x0B:
				{
					Sprite& sprite = mSprites[rc->mReg & 3];
					const uint8 newSize = value & 3;

					if (sprite.mState.mSizeMode != newSize) {
						// catch sprite state up to this point
						sprite.Sync(rc->mPos);

						// change sprite mode
						sprite.mState.mSizeMode = newSize;

						// generate update image
						GenerateSpriteImage(sprite, rc->mPos);
					}
				}
				break;

			case 0x0C:
				for(int i=0; i<4; ++i) {
					Sprite& sprite = mSprites[i+4];

					const uint8 newSize = (value >> (2*i)) & 3;

					if (sprite.mState.mSizeMode != newSize) {
						// catch sprite state up to this point
						sprite.Sync(rc->mPos);

						// switch size mode
						sprite.mState.mSizeMode = newSize;

						// generate update image
						GenerateSpriteImage(sprite, rc->mPos);
					}
				}
				break;
			case 0x0D:
			case 0x0E:
			case 0x0F:
			case 0x10:
				mSprites[rc->mReg - 0x0D].mState.mDataLatch = value;
				if (value)
					mbSpritesActive = true;
				break;
			case 0x11:
				mSprites[4].mState.mDataLatch = (value << 6) & 0xc0;
				mSprites[5].mState.mDataLatch = (value << 4) & 0xc0;
				mSprites[6].mState.mDataLatch = (value << 2) & 0xc0;
				mSprites[7].mState.mDataLatch = (value     ) & 0xc0;
				if (value)
					mbSpritesActive = true;
				break;

			case 0x1B:
				if (!(value & 0xc0) && (mActivePRIOR & 0xc0))
					mbGTIADisableTransition = true;

				mActivePRIOR = value;

				if (value & 0xC0)
					mbHiresMode = false;

				break;
			case 0x1E:		// $D01E HITCLR
				memset(mPlayerCollFlags, 0, sizeof mPlayerCollFlags);
				memset(mMissileCollFlags, 0, sizeof mMissileCollFlags);
				break;

			case 0x20:		// missile DMA
				{
					uint8 mask = 0x0F;

					// We get called after ANTIC has bumped the scanline but before GTIA knows about it,
					// so mY is actually one off.
					if (mY & 1)
						mask = ~mVDELAY;

					if (mask & 0x01)
						mSprites[4].mState.mDataLatch = (value << 6) & 0xc0;

					if (mask & 0x02)
						mSprites[5].mState.mDataLatch = (value << 4) & 0xc0;

					if (mask & 0x04)
						mSprites[6].mState.mDataLatch = (value << 2) & 0xc0;

					if (mask & 0x08)
						mSprites[7].mState.mDataLatch = (value     ) & 0xc0;

					if (value)
						mbSpritesActive = true;
				}
				break;
		}

		++rc;
	}
}

void ATGTIAEmulator::UpdateSECAMTriggerLatch(int index) {
	// The 107 is a guess. The triggers start shifting into FGTIA on the
	// third cycle of horizontal blank according to the FGTIA doc.
	uint32 t = mpConn->GTIAGetLineEdgeTimingId(107 + index);

	if (mTRIGSECAMLastUpdate[index] != t) {
		mTRIGSECAMLastUpdate[index] = t;

		const uint8 v = mTRIGSECAM[index];
		mTRIG[index] = v;
		mTRIGLatched[index] &= v;
	}
}

void ATGTIAEmulator::ResetSprites() {
	for(int i=0; i<8; ++i) {
		Sprite& sprite = mSprites[i];

		sprite.mLastSync = 0;
		sprite.mState.Reset();

		SpriteImage *p = sprite.mpImageHead;
		while(p) {
			SpriteImage *next = p->mpNext;

			FreeSpriteImage(p);

			p = next;
		}

		sprite.mpImageHead = NULL;
		sprite.mpImageTail = NULL;
	}

	mbSpritesActive = false;
}

void ATGTIAEmulator::GenerateSpriteImages(int x1, int x2) {
	unsigned xr = (unsigned)(x2 - x1);

	// Trigger new sprite images
	bool foundActiveSprite = false;

	for(int i=0; i<8; ++i) {
		Sprite& sprite = mSprites[i];

		// Check if there is any latched or shifting data -- if not, we do not care because:
		//
		// - the only impact of the shifter state is its output for priority and collision
		// - shifting in any non-zero data would reset the state to 0
		//
		// Note that we still need to do this if data is still available to shift out as
		// we can re-capture that in a new image. Also, we are doing this on the last synced
		// state of the shift hardware instead of the state at the time of the trigger, but
		// we will check again below.

		if (!(sprite.mState.mDataLatch | sprite.mState.mShiftRegister))
			continue;

		foundActiveSprite = true;

		int pos = mSpritePos[i];

		if ((unsigned)(pos - x1) < xr) {
			// catch sprite state up to this point
			sprite.Sync(pos);

			// latch in new image
			if (sprite.mState.mShiftState) {
				sprite.mState.mShiftState = 0;
				sprite.mState.mShiftRegister += sprite.mState.mShiftRegister;
			}

			sprite.mState.mShiftRegister |= sprite.mState.mDataLatch;

			// generate new sprite image
			GenerateSpriteImage(sprite, pos);
		}
	}

	if (!foundActiveSprite)
		mbSpritesActive = false;
}

void ATGTIAEmulator::GenerateSpriteImage(Sprite& sprite, int pos) {
	// if we have a previous image, truncate it
	if (sprite.mpImageTail && sprite.mpImageTail->mX2 > pos)
		sprite.mpImageTail->mX2 = pos;

	// skip all zero images
	if (sprite.mState.mShiftRegister) {
		// compute sprite width
		static const int kWidthLookup[4] = {8,16,8,32};
		int width = kWidthLookup[sprite.mState.mSizeMode];

		// check for special case lockup
		if (sprite.mState.mSizeMode == 2 && (sprite.mState.mShiftState == 1 || sprite.mState.mShiftState == 2))
			width = 1024;

		// record image
		SpriteImage *image = AllocSpriteImage();
		image->mX1 = pos;
		image->mX2 = pos + width;
		image->mState = sprite.mState;
		image->mpNext = NULL;

		if (sprite.mpImageTail)
			sprite.mpImageTail->mpNext = image;
		else
			sprite.mpImageHead = image;

		sprite.mpImageTail = image;
	}
}

void ATGTIAEmulator::FreeSpriteImage(SpriteImage *p) {
	p->mpNext = mpFreeSpriteImages;
	mpFreeSpriteImages = p;
}

ATGTIAEmulator::SpriteImage *ATGTIAEmulator::AllocSpriteImage() {
	if (!mpFreeSpriteImages)
		return mNodeAllocator.Allocate<SpriteImage>();

	SpriteImage *p = mpFreeSpriteImages;
	mpFreeSpriteImages = p->mpNext;

	return p;
}

ATGTIAEmulator::VerticalOverscanMode ATGTIAEmulator::DeriveVerticalOverscanMode() const {
	if (mVerticalOverscanMode != kVerticalOverscan_Default)
		return mVerticalOverscanMode;

	switch(mOverscanMode) {
		case kOverscanFull:
			return kVerticalOverscan_Full;

		case kOverscanExtended:
			return kVerticalOverscan_Extended;

		default:
		case kOverscanNormal:
		case kOverscanWidescreen:
			return kVerticalOverscan_Normal;

		case kOverscanOSScreen:
			return kVerticalOverscan_OSScreen;
	}
}
