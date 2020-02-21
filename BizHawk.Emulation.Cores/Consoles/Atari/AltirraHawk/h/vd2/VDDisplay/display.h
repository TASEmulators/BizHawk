#ifndef f_VD2_RIZA_DISPLAY_H
#define f_VD2_RIZA_DISPLAY_H

#include <vd2/system/function.h>
#include <vd2/system/vectors.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>
#include <vd2/system/atomic.h>
#include <vd2/Kasumi/pixmap.h>

VDGUIHandle VDCreateDisplayWindowW32(uint32 dwExFlags, uint32 dwFlags, int x, int y, int width, int height, VDGUIHandle hwndParent);

class IVDVideoDisplay;
class IVDDisplayCompositor;
class VDPixmapBuffer;
class VDBufferedStream;

struct VDVideoDisplayScreenFXInfo {
	float mScanlineIntensity;
	float mGamma;

	float mPALBlendingOffset;

	float mDistortionX;
	float mDistortionYRatio;

	bool mbColorCorrectAdobeRGB;
	float mColorCorrectionMatrix[3][3];

	float mBloomThreshold;
	float mBloomRadius;
	float mBloomDirectIntensity;
	float mBloomIndirectIntensity;
};

class IVDVideoDisplayScreenFXEngine {
public:
	// Apply screen FX to an image in software; returns resulting image. The source image buffer must
	// not be modified, and the engine must keep the result image alive as long as the original submitted
	// frame.
	virtual VDPixmap ApplyScreenFX(const VDPixmap& px) = 0;
};

class VDVideoDisplayFrame : public vdlist_node, public IVDRefCount {
public:
	VDVideoDisplayFrame();
	virtual ~VDVideoDisplayFrame();

	virtual int AddRef();
	virtual int Release();

	VDPixmap	mPixmap {};
	uint32		mFlags = 0;
	bool		mbAllowConversion = false;

	IVDVideoDisplayScreenFXEngine *mpScreenFXEngine = nullptr;
	const VDVideoDisplayScreenFXInfo *mpScreenFX = nullptr;

protected:
	VDAtomicInt	mRefCount;
};

class VDINTERFACE IVDVideoDisplayCallback {
public:
	virtual void DisplayRequestUpdate(IVDVideoDisplay *pDisp) = 0;
};

class VDINTERFACE IVDVideoDisplay {
public:
	enum FieldMode {
		kVSync				= 0x0004,
		kDoNotCache			= 0x0020,
		kVisibleOnly		= 0x0040,
		kDoNotWait			= 0x0800,
		kFieldModeMax		= 0xffff,
	};

	enum FilterMode {
		kFilterAnySuitable,
		kFilterPoint,
		kFilterBilinear,
		kFilterBicubic
	};

	virtual void Destroy() = 0;
	virtual void Reset() = 0;
	virtual void SetSourceMessage(const wchar_t *msg) = 0;
	virtual bool SetSource(bool bAutoUpdate, const VDPixmap& src, bool bAllowConversion = true) = 0;
	virtual bool SetSourcePersistent(bool bAutoUpdate, const VDPixmap& src, bool bAllowConversion = true, const VDVideoDisplayScreenFXInfo *screenFX = nullptr, IVDVideoDisplayScreenFXEngine *screenFXEngine = nullptr) = 0;
	virtual void SetSourceSubrect(const vdrect32 *r) = 0;
	virtual void SetSourceSolidColor(uint32 color) = 0;

	virtual void SetReturnFocus(bool enable) = 0;
	virtual void SetTouchEnabled(bool enable) = 0;
	virtual void SetUse16Bit(bool enable) = 0;

	virtual void SetFullScreen(bool fs, uint32 width = 0, uint32 height = 0, uint32 refresh = 0) = 0;
	virtual void SetDestRect(const vdrect32 *r, uint32 backgroundColor) = 0;
	virtual void SetPixelSharpness(float xfactor, float yfactor) = 0;
	virtual void SetCompositor(IVDDisplayCompositor *compositor) = 0;

	virtual void PostBuffer(VDVideoDisplayFrame *) = 0;
	virtual bool RevokeBuffer(bool allowFrameSkip, VDVideoDisplayFrame **ppFrame) = 0;
	virtual void FlushBuffers() = 0;

	virtual void Invalidate() = 0;
	virtual void Update(int mode = 0) = 0;
	virtual void Cache() = 0;
	virtual void SetCallback(IVDVideoDisplayCallback *p) = 0;

	enum AccelerationMode {
		kAccelOnlyInForeground,
		kAccelResetInForeground,
		kAccelAlways
	};

	virtual void SetAccelerationMode(AccelerationMode mode) = 0;

	virtual FilterMode GetFilterMode() = 0;
	virtual void SetFilterMode(FilterMode) = 0;
	virtual float GetSyncDelta() const = 0;

	virtual vdrect32 GetMonitorRect() = 0;

	// Returns true if the current/last minidriver supported screen FX. This is a hint as to
	// whether hardware or software acceleration should be preferred. Calling code must still
	// be prepared to fall back to software emulation should the minidriver change to one that
	// doesn't support screen FX.
	virtual bool IsScreenFXPreferred() const = 0;

	// Map a normalized source point in [0,1] to the destination size. Returns true if the
	// point was within the source, false if it was clamped. This is a no-op if distortion is
	// off.
	virtual bool MapNormSourcePtToDest(vdfloat2& pt) const = 0;

	// Map a normalized destination point in [0,1] to the destination size. Returns true if the
	// point was within the source, false if it was clamped. This is a no-op if distortion is
	// off.
	virtual bool MapNormDestPtToSource(vdfloat2& pt) const = 0;

	enum ProfileEvent {
		kProfileEvent_BeginTick,
		kProfileEvent_EndTick,
	};

	virtual void SetProfileHook(const vdfunction<void(ProfileEvent)>& profileHook) = 0;
};

void VDVideoDisplaySetFeatures(bool enableDirectX, bool enableOverlays, bool enableTermServ, bool enableOpenGL, bool enableDirect3D, bool enableD3DFX, bool enableHighPrecision);
void VDVideoDisplaySetD3D9ExEnabled(bool enable);
void VDVideoDisplaySetDDrawEnabled(bool enable);
void VDVideoDisplaySet3DEnabled(bool enable);
void VDVideoDisplaySetDebugInfoEnabled(bool enable);
void VDVideoDisplaySetBackgroundFallbackEnabled(bool enable);
void VDVideoDisplaySetSecondaryDXEnabled(bool enable);
void VDVideoDisplaySetMonitorSwitchingDXEnabled(bool enable);
void VDVideoDisplaySetTermServ3DEnabled(bool enable);

IVDVideoDisplay *VDGetIVideoDisplay(VDGUIHandle hwnd);
bool VDRegisterVideoDisplayControl();

class IVDDisplayImageDecoder {
public:
	virtual bool DecodeImage(VDPixmapBuffer& buf, VDBufferedStream& stream) = 0;
};

void VDDisplaySetImageDecoder(IVDDisplayImageDecoder *pfn);

#endif
