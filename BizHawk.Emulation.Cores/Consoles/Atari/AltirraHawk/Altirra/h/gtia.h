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

#ifndef AT_GTIA_H
#define AT_GTIA_H

#include <vd2/system/linearalloc.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vectors.h>
#include <vd2/Kasumi/pixmap.h>
#include <at/atcore/enumparse.h>
#include <at/atcore/notifylist.h>

class IVDVideoDisplay;
class VDVideoDisplayFrame;
class IATUIRenderer;

class IATGTIAEmulatorConnections {
public:
	virtual uint32 GTIAGetXClock() = 0;
	virtual uint32 GTIAGetTimestamp() const = 0;
	virtual uint64 GTIAGetTimestamp64() const = 0;
	virtual void GTIASetSpeaker(bool state) = 0;
	virtual void GTIASelectController(uint8 index, bool potsEnabled) = 0;
	virtual void GTIARequestAnticSync(int offset) = 0;
	virtual uint32 GTIAGetLineEdgeTimingId(uint32 offset) const = 0;
};

class IATGTIAVideoTap {
public:
	virtual void WriteFrame(const VDPixmap& px, uint64 timestampStart, uint64 timestampEnd) = 0;
};

using ATGTIARawFrameFn = vdfunction<void(const VDPixmap& px)>;

class ATFrameTracker;
class ATArtifactingEngine;
class ATSaveStateReader;
class ATSaveStateWriter;
class ATGTIARenderer;
class ATVBXEEmulator;

enum ATLumaRampMode : uint8 {
	kATLumaRampMode_Linear,
	kATLumaRampMode_XL,
	kATLumaRampModeCount
};

enum class ATColorMatchingMode : uint8 {
	None,
	SRGB,
	AdobeRGB
};

AT_DECLARE_ENUM_TABLE(ATColorMatchingMode);

struct ATColorParams {
	float mHueStart;
	float mHueRange;
	float mBrightness;
	float mContrast;
	float mSaturation;
	float mGammaCorrect;
	float mIntensityScale;
	float mArtifactHue;	
	float mArtifactSat;
	float mArtifactSharpness;
	float mRedShift;
	float mRedScale;
	float mGrnShift;
	float mGrnScale;
	float mBluShift;
	float mBluScale;
	bool mbUsePALQuirks;
	ATLumaRampMode mLumaRampMode;
	ATColorMatchingMode mColorMatchingMode;

	bool IsSimilar(const ATColorParams& params) const;
};

struct ATNamedColorParams : public ATColorParams {
	VDStringA mPresetTag;
};

struct ATColorSettings {
	ATNamedColorParams	mNTSCParams;
	ATNamedColorParams	mPALParams;
	bool	mbUsePALParams;
};

struct ATArtifactingParams {
	// Intensity ratio of darkest point between scanlines to the brightest portion of
	// scanlines, in gamma space.
	float mScanlineIntensity;

	// Horizontal view angle in degrees (0-179).
	float mDistortionViewAngleX;

	// Ratio of vertical distortion to horizontal distortion.
	float mDistortionYRatio;

	bool mbEnableBloom;
	bool mbBloomScanlineCompensation;
	float mBloomThreshold;
	float mBloomRadius;
	float mBloomDirectIntensity;
	float mBloomIndirectIntensity;

	static ATArtifactingParams GetDefault();
};

uint32 ATGetColorPresetCount();
const char *ATGetColorPresetTagByIndex(uint32 i);
sint32 ATGetColorPresetIndexByTag(const char *tags);
const wchar_t *ATGetColorPresetNameByIndex(uint32 i);
ATColorParams ATGetColorPresetByIndex(uint32 i);

struct ATGTIARegisterState {
	uint8	mReg[0x20];
};

class ATGTIAEmulator {
public:
	ATGTIAEmulator();
	~ATGTIAEmulator();

	void Init(IATGTIAEmulatorConnections *);
	void ColdReset();

	void SetVBXE(ATVBXEEmulator *);
	void SetUIRenderer(IATUIRenderer *);
	
	enum AnalysisMode {
		kAnalyzeNone,
		kAnalyzeLayers,
		kAnalyzeColors,
		kAnalyzeDList,
		kAnalyzeCount
	};

	enum ArtifactMode {
		kArtifactNone,
		kArtifactNTSC,
		kArtifactPAL,
		kArtifactNTSCHi,
		kArtifactPALHi,
		kArtifactAuto,
		kArtifactAutoHi,
		kArtifactCount
	};

	enum OverscanMode {
		kOverscanNormal,		// 168cc
		kOverscanExtended,		// 192cc
		kOverscanFull,			// 228cc
		kOverscanOSScreen,		// 160cc
		kOverscanWidescreen,	// 176cc
		kOverscanCount
	};
	
	enum VerticalOverscanMode {
		kVerticalOverscan_Default,
		kVerticalOverscan_OSScreen,		// 192 lines
		kVerticalOverscan_Normal,		// 224 lines
		kVerticalOverscan_Extended,		// 240 lines
		kVerticalOverscan_Full,
		kVerticalOverscanCount
	};

	ATColorSettings GetColorSettings() const;
	void SetColorSettings(const ATColorSettings& settings);

	ATArtifactingParams GetArtifactingParams() const;
	void SetArtifactingParams(const ATArtifactingParams& params);

	void ResetColors();
	void GetPalette(uint32 pal[256]) const;
	void GetNTSCArtifactColors(uint32 c[2]) const;

	bool IsFrameInProgress() const { return mpFrame != NULL; }
	bool AreAcceleratedEffectsAvailable() const;

	bool IsVsyncEnabled() const { return mbVsyncEnabled; }
	void SetVsyncEnabled(bool enabled) { mbVsyncEnabled = enabled; }

	AnalysisMode GetAnalysisMode() const { return mAnalysisMode; }
	void SetAnalysisMode(AnalysisMode mode);

	OverscanMode GetOverscanMode() const { return mOverscanMode; }
	void SetOverscanMode(OverscanMode mode);

	VerticalOverscanMode GetVerticalOverscanMode() const { return mVerticalOverscanMode; }
	void SetVerticalOverscanMode(VerticalOverscanMode mode);

	bool IsOverscanPALExtended() const { return mbOverscanPALExtended; }
	void SetOverscanPALExtended(bool extended);

	vdrect32 GetFrameScanArea() const;
	void GetRawFrameFormat(int& w, int& h, bool& rgb32) const;
	void GetFrameSize(int& w, int& h) const;
	void GetPixelAspectMultiple(int& x, int& y) const;

	void SetForcedBorder(bool forcedBorder) { mbForcedBorder = forcedBorder; }
	void SetFrameSkip(bool turbo) { mbTurbo = turbo; }

	bool ArePMCollisionsEnabled() const;
	void SetPMCollisionsEnabled(bool enable);

	bool ArePFCollisionsEnabled() const;
	void SetPFCollisionsEnabled(bool enable);

	void SetVideoOutput(IVDVideoDisplay *pDisplay);

	bool IsCTIAMode() const { return mbCTIAMode; }
	void SetCTIAMode(bool enabled);

	bool IsPALMode() const { return mbPALMode; }
	void SetPALMode(bool enabled);

	bool IsSECAMMode() const { return mbSECAMMode; }
	void SetSECAMMode(bool enabled);

	ArtifactMode GetArtifactingMode() const { return mArtifactMode; }
	void SetArtifactingMode(ArtifactMode mode) { mArtifactMode = mode; }

	bool IsBlendModeEnabled() const { return mbBlendMode; }
	void SetBlendModeEnabled(bool enable) { mbBlendMode = enable; }

	bool IsInterlaceEnabled() const { return mbInterlaceEnabled; }
	void SetInterlaceEnabled(bool enable) { mbInterlaceEnabled = enable; }

	bool AreScanlinesEnabled() const { return mbScanlinesEnabled; }
	void SetScanlinesEnabled(bool enable) { mbScanlinesEnabled = enable; }

	bool GetAccelScreenFXEnabled() const { return mbAccelScreenFX; }
	void SetAccelScreenFXEnabled(bool enabled) { mbAccelScreenFX = enabled; }

	void SetConsoleSwitch(uint8 c, bool down);
	uint8 ReadConsoleSwitches() const;

	uint8 GetForcedConsoleSwitches() const { return mForcedSwitchInput; }
	void SetForcedConsoleSwitches(uint8 c);
	 
	void SetControllerTrigger(int index, bool state) {
		uint8 v = state ? 0x00 : 0x01;

		if (mbSECAMMode) {
			UpdateSECAMTriggerLatch(index);
			mTRIGSECAM[index] = v;
		} else {
			mTRIG[index] = v;
			mTRIGLatched[index] &= v;
		}
	}

	void AddVideoTap(IATGTIAVideoTap *vtap);
	void RemoveVideoTap(IATGTIAVideoTap *vtap);

	void AddRawFrameCallback(const ATGTIARawFrameFn *fn);
	void RemoveRawFrameCallback(const ATGTIARawFrameFn *fn);

	const VDPixmap *GetLastFrameBuffer() const;

	uint32 GetBackgroundColor24() const { return mPalette[mPFBAK]; }
	uint32 GetPlayfieldColor24(int index) const { return mPalette[mPFColor[index]]; }
	uint32 GetPlayfieldColorPF2H() const { return mPalette[(mPFColor[2] & 0xf0) + (mPFColor[1] & 0x0f)]; }

	bool IsPhantomDMAPossible() const {
		return (mGRACTL & 3) != 0;
	}

	void DumpStatus();

	void BeginLoadState(ATSaveStateReader& reader);
	void LoadStateArch(ATSaveStateReader& reader);
	void LoadStatePrivate(ATSaveStateReader& reader);
	void LoadStateResetPrivate(ATSaveStateReader& reader);
	void EndLoadState(ATSaveStateReader& reader);
	void BeginSaveState(ATSaveStateWriter& writer);
	void SaveStateArch(ATSaveStateWriter& writer);
	void SaveStatePrivate(ATSaveStateWriter& writer);

	void GetRegisterState(ATGTIARegisterState& state) const;

	enum VBlankMode {
		kVBlankModeOff,
		kVBlankModeOn,
		kVBlankModeBugged
	};

	void SetFieldPolarity(bool polarity);
	void SetVBLANK(VBlankMode vblMode);
	bool BeginFrame(bool force, bool drop);
	void BeginScanline(int y, bool hires);
	void EndScanline(uint8 dlControl, bool pfRendered);
	void UpdatePlayer(bool odd, int index, uint8 byte);
	void UpdateMissile(bool odd, uint8 byte);
	void UpdatePlayfield160(uint32 x, uint8 byte);
	void UpdatePlayfield160(uint32 x, const uint8 *src, uint32 n);
	void UpdatePlayfield320(uint32 x, uint8 byte);
	void UpdatePlayfield320(uint32 x, const uint8 *src, uint32 n);
	void Sync(int offset = 0);

	void RenderActivityMap(const uint8 *src);
	void UpdateScreen(bool immediate, bool forceAnyScreen);
	void RecomputePalette();

	uint8 DebugReadByte(uint8 reg) {
		return ReadByte(reg);
	}

	uint8 ReadByte(uint8 reg);
	void WriteByte(uint8 reg, uint8 value);

protected:
	struct RegisterChange {
		sint16 mPos;
		uint8 mReg;
		uint8 mValue;
	};

	struct SpriteState {
		uint8	mShiftRegister;
		uint8	mShiftState;
		uint8	mSizeMode;
		uint8	mDataLatch;

		void Reset();
		uint8 Detect(uint32 ticks, const uint8 *src);
		uint8 Detect(uint32 ticks, const uint8 *src, const uint8 *hires);
		uint8 Generate(uint32 ticks, uint8 mask, uint8 *dst);
		uint8 Generate(uint32 ticks, uint8 mask, uint8 *dst, const uint8 *hires);
		void Advance(uint32 ticks);
	};

	struct SpriteImage {
		SpriteImage *mpNext;
		sint16	mX1;
		sint16	mX2;
		SpriteState mState;
	};

	struct Sprite {
		SpriteState mState;
		SpriteImage *mpImageHead;
		SpriteImage *mpImageTail;
		int mLastSync;

		void Sync(int pos);
	};

	template<class T> void ExchangeStatePrivate(T& io);
	void SyncTo(int xend);
	void Render(int x1, int targetX);
	void ApplyArtifacting(bool immediate);
	void AddRegisterChange(uint8 pos, uint8 addr, uint8 value);
	void UpdateRegisters(const RegisterChange *rc, int count);
	void UpdateSECAMTriggerLatch(int index);
	void ResetSprites();
	void GenerateSpriteImages(int x1, int x2);
	void GenerateSpriteImage(Sprite& sprite, int pos);
	void FreeSpriteImage(SpriteImage *);
	SpriteImage *AllocSpriteImage();
	VerticalOverscanMode DeriveVerticalOverscanMode() const;

	// critical variables - sync
	IATGTIAEmulatorConnections *mpConn; 
	int		mLastSyncX;
	VBlankMode		mVBlankMode;
	bool	mbANTICHiresMode;
	bool	mbHiresMode;

	typedef vdfastvector<RegisterChange> RegisterChanges;
	RegisterChanges mRegisterChanges;
	int mRCIndex;
	int mRCCount;

	// critical variables - sprite update
	uint8	mSpritePos[8];
	bool	mbSpritesActive;
	bool	mbPMRendered;
	SpriteImage *mpFreeSpriteImages;
	Sprite	mSprites[8];
	uint8	mPlayerCollFlags[4];
	uint8	mMissileCollFlags[4];

	// non-critical variables
	IVDVideoDisplay *mpDisplay;
	vdautoptr<vdfastvector<IATGTIAVideoTap *>> mpVideoTaps;
	uint32	mY;

	AnalysisMode	mAnalysisMode;
	ArtifactMode	mArtifactMode;
	OverscanMode	mOverscanMode;
	VerticalOverscanMode	mVerticalOverscanMode;
	bool	mbVsyncEnabled = false;
	bool	mbBlendMode = false;
	bool	mbBlendModeLastFrame = false;
	bool	mbFrameCopiedFromPrev = false;
	bool	mbOverscanPALExtended = false;
	bool	mbOverscanPALExtendedThisFrame = false;
	bool	mbPALThisFrame = false;
	bool	mbInterlaceEnabled = false;
	bool	mbInterlaceEnabledThisFrame = false;
	bool	mbScanlinesEnabled = false;
	bool	mbSoftScanlinesEnabledThisFrame = false;
	bool	mbIncludeHBlankThisFrame = false;
	bool	mbFieldPolarity = false;
	bool	mbLastFieldPolarity = false;
	bool	mbPostProcessThisFrame = false;
	bool	mb14MHzThisFrame = false;
	bool	mbAccelScreenFX = false;
	bool	mbScreenFXEnabledThisFrame = false;

	ATGTIARegisterState	mState;

	// used during register read
	uint8	mCollisionMask;

	// The following 9 registers must be contiguous.
	uint8	mPMColor[4];		// $D012-D015 player and missile colors
	uint8	mPFColor[4];		// $D016-D019 playfield colors
	uint8	mPFBAK;				// $D01A background color

	uint8	mActivePRIOR;		// $D01B priority - currently live value
	uint8	mPRIOR;				// $D01B priority - architectural value
	uint8	mVDELAY;			// $D01C vertical delay
	uint8	mGRACTL;			// $D01D
								// bit 2: latch trigger inputs
								// bit 1: enable players
								// bit 0: enable missiles
	uint8	mSwitchOutput;		// $D01F (CONSOL) output from GTIA
								// bit 3: speaker output
	uint8	mSwitchInput;		// $D01F (CONSOL) input to GTIA
	uint8	mForcedSwitchInput;

	uint8	mTRIG[4];
	uint8	mTRIGLatched[4];
	uint8	mTRIGSECAM[4];
	uint32	mTRIGSECAMLastUpdate[4];

	uint8	*mpDst;
	vdrefptr<VDVideoDisplayFrame>	mpFrame;
	VDPixmap	mRawFrame;
	uint64	mFrameTimestamp;
	ATFrameTracker *mpFrameTracker;
	bool	mbMixedRendering;	// GTIA mode with non-hires or pseudo mode E
	bool	mbGTIADisableTransition;
	bool	mbTurbo;
	bool	mbCTIAMode;
	bool	mbPALMode;
	bool	mbSECAMMode;
	bool	mbForcedBorder;

	VDALIGN(16)	uint8	mMergeBuffer[228+12];
	VDALIGN(16)	uint8	mAnticData[228+12];
	uint32	mPalette[256];
	uint32	mSignedPalette[256];
	bool	mbScanlinesWithHiRes[240];

	ATColorParams mActiveColorParams;
	ATColorSettings mColorSettings;
	float mColorMatchingMatrix[3][3];

	vdfastvector<uint8, vdaligned_alloc<uint8> > mPreArtifactFrameBuffer;
	VDPixmap	mPreArtifactFrame;
	uint32		mPreArtifactFrameVisibleY1;
	uint32		mPreArtifactFrameVisibleY2;

	ATArtifactingEngine	*mpArtifactingEngine;
	vdrefptr<VDVideoDisplayFrame> mpLastFrame;

	ATGTIARenderer *mpRenderer;
	IATUIRenderer *mpUIRenderer;
	ATVBXEEmulator *mpVBXE;

	ATNotifyList<const ATGTIARawFrameFn *> mRawFrameCallbacks;

	VDLinearAllocator mNodeAllocator;
};

#endif
