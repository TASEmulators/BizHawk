#include <windows.h>
#define INITGUID
#include <guiddef.h>
#include <ddraw.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>
#include <vd2/system/time.h>
#include <vd2/system/math.h>
#include <vd2/system/w32assist.h>
#include <vd2/Kasumi/blitter.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vd2/VDDisplay/compositor.h>
#include <vd2/VDDisplay/displaydrv.h>
#include <vd2/VDDisplay/renderer.h>
#include <vd2/VDDisplay/internal/directdraw.h>
#include <vd2/VDDisplay/internal/rendererddraw.h>

//#define VDDEBUG_DISP (void)sizeof printf
#define VDDEBUG_DISP VDDEBUG

#if 0
	#define DEBUG_LOG(x) VDLog(kVDLogInfo, VDStringW(L##x))
#else
	#define DEBUG_LOG(x)
#endif

void VDDitherImage(VDPixmap& dst, const VDPixmap& src, const uint8 *pLogPal);

///////////////////////////////////////////////////////////////////////////////////////////////////

class VDDDrawPresentHistory {
public:
	bool mbPresentPending;
	bool mbPresentBlitStarted;
	float mPresentDelay;
	float mVBlankSuccess;
	uint64	mPresentStartTime;

	double	mAveragePresentTime;
	double	mAverageStartScanline;
	double	mAverageEndScanline;
	uint32	mPollCount;
	uint32	mLastBracketY1;
	uint32	mLastBracketY2;

	float	mScanlineTarget;
	sint32	mLastScanline;
	bool	mbLastWasVBlank;

	sint32	mScanTop;
	sint32	mScanBottom;

	float mSuccessProb[17];
	float mAttemptProb[17];

	VDDDrawPresentHistory()
		: mbPresentPending(false)
		, mbPresentBlitStarted(false)
		, mPresentDelay(0.f)
		, mVBlankSuccess(1.0f)
		, mPresentStartTime(0)
		, mAveragePresentTime(0)
		, mAverageStartScanline(0)
		, mAverageEndScanline(0)
		, mPollCount(0)
		, mLastBracketY1(0)
		, mLastBracketY2(0)
		, mScanlineTarget(0)
		, mLastScanline(0)
		, mbLastWasVBlank(false)
		, mScanTop(0)
		, mScanBottom(0)
	{
		memset(&mSuccessProb, 0, sizeof mSuccessProb);
		memset(&mAttemptProb, 0, sizeof mAttemptProb);
	}
};

struct VDVideoDisplayDDManagerNode : public vdlist_node {};

class VDDirectDrawManager final : public IVDDirectDrawManager, public VDVideoDisplayDDManagerNode {
	VDDirectDrawManager(const VDDirectDrawManager&) = delete;
	VDDirectDrawManager& operator=(const VDDirectDrawManager&) = delete;
public:
	VDDirectDrawManager(VDThreadId tid, HMONITOR hMonitor);
	~VDDirectDrawManager();

	bool Init(IVDDirectDrawClient *pClient);
	bool Shutdown(IVDDirectDrawClient *pClient);

	VDThreadID GetThreadId() const { return mThreadId; }
	HMONITOR GetMonitor() const { return mhMonitor; }

	IDirectDraw2 *GetDDraw() { return mpdd; }
	const DDCAPS& GetCaps() { return mCaps; }

	IDirectDrawSurface2 *GetPrimary() { return mpddsPrimary; }
	const DDSURFACEDESC& GetPrimaryDesc() { return mPrimaryDesc; }

	HMONITOR GetMonitor() { return mhMonitor; }
	const vdrect32& GetMonitorRect() { return mMonitorRect; }

	bool Restore();

protected:
	bool InitPrimary();
	void ShutdownPrimary();


	uint32					mInitCount;

	HMODULE					mhmodDD;
	const HMONITOR			mhMonitor;
	const VDThreadId		mThreadId;

	IDirectDraw2			*mpdd;
	IDirectDrawSurface2		*mpddsPrimary;

	DDSURFACEDESC			mPrimaryDesc;
	DDCAPS					mCaps;

	vdrect32				mMonitorRect;

	typedef vdfastvector<IVDDirectDrawClient *> tClients;
	tClients mClients;
};

VDDirectDrawManager::VDDirectDrawManager(VDThreadId tid, HMONITOR hMonitor)
	: mInitCount(0)
	, mhmodDD(NULL)
	, mhMonitor(hMonitor)
	, mThreadId(tid)
	, mpdd(NULL)
	, mpddsPrimary(NULL)
	, mPrimaryDesc()
	, mCaps()
	, mMonitorRect(0, 0, 0, 0)
{
}

VDDirectDrawManager::~VDDirectDrawManager() {
}

namespace {
	struct VDDDGuidFinder {
		VDDDGuidFinder(HMONITOR hMonitor)
			: mhMonitor(hMonitor)
			, mbFound(false)
			, mbFoundDefault(false)
			, mbFoundDefaultGuid(false)
		{
		}

		static BOOL WINAPI EnumCallback(GUID FAR *lpGUID, LPSTR lpDriverDescription, LPSTR lpDriverName, LPVOID lpContext, HMONITOR hm) {
			VDDDGuidFinder *finder = (VDDDGuidFinder *)lpContext;

			if (hm == finder->mhMonitor) {
				finder->mGuid = *lpGUID;
				finder->mbFound = true;
				return FALSE;
			}

			if (!hm) {
				if (lpGUID) {
					finder->mDefaultGuid = *lpGUID;
					finder->mbFoundDefaultGuid = true;
				}

				finder->mbFoundDefault = true;
			}

			return TRUE;
		}

		HMONITOR mhMonitor;
		GUID mGuid;
		GUID mDefaultGuid;
		bool mbFound;
		bool mbFoundDefault;
		bool mbFoundDefaultGuid;
	};
}

bool VDDirectDrawManager::Init(IVDDirectDrawClient *pClient) {
	if (mInitCount) {
		++mInitCount;
		mClients.push_back(pClient);
		return true;
	}

	mMonitorRect.set(0, 0, ::GetSystemMetrics(SM_CXSCREEN), ::GetSystemMetrics(SM_CYSCREEN));

	// GetMonitorInfo() requires Windows 98/2000.
	bool isDefaultMonitor = true;

	if (mhMonitor) {
		typedef BOOL (APIENTRY *tpGetMonitorInfoA)(HMONITOR mon, LPMONITORINFO lpmi);
		tpGetMonitorInfoA pGetMonitorInfoA = (tpGetMonitorInfoA)GetProcAddress(GetModuleHandleW(L"user32"), "GetMonitorInfoA");

		if (pGetMonitorInfoA) {
			MONITORINFO monInfo = {sizeof(MONITORINFO)};
			if (pGetMonitorInfoA(mhMonitor, &monInfo)) {
				mMonitorRect.set(monInfo.rcMonitor.left, monInfo.rcMonitor.top, monInfo.rcMonitor.right, monInfo.rcMonitor.bottom);

				isDefaultMonitor = (monInfo.dwFlags & MONITORINFOF_PRIMARY) != 0;
			}
		}
	}

	mhmodDD = VDLoadSystemLibraryW32("ddraw");
	if (!mhmodDD)
		return false;

	do {
		typedef HRESULT (WINAPI *tpDirectDrawCreate)(GUID FAR *lpGUID, LPDIRECTDRAW FAR *lplpDD, IUnknown FAR *pUnkOuter);
		tpDirectDrawCreate pDirectDrawCreate = (tpDirectDrawCreate)GetProcAddress(mhmodDD, "DirectDrawCreate");

		if (!pDirectDrawCreate)
			break;

		GUID guid;
		GUID *pguid = NULL;

		if (mhMonitor) {
			// NOTE: This is a DX6 function.
			typedef HRESULT (WINAPI *tpDirectDrawEnumerateEx)(LPDDENUMCALLBACKEXA callback, LPVOID context, DWORD dwFlags);
			tpDirectDrawEnumerateEx pDirectDrawEnumerateEx = (tpDirectDrawEnumerateEx)GetProcAddress(mhmodDD, "DirectDrawEnumerateExA");

			if (pDirectDrawEnumerateEx) {
				VDDDGuidFinder finder(mhMonitor);
				pDirectDrawEnumerateEx(VDDDGuidFinder::EnumCallback, &finder, DDENUM_ATTACHEDSECONDARYDEVICES);

				if (finder.mbFound) {
					guid = finder.mGuid;
					pguid = &guid;
				} else if (isDefaultMonitor && finder.mbFoundDefault) {
					if (finder.mbFoundDefaultGuid) {
						guid = finder.mDefaultGuid;
						pguid = &guid;
					}
				} else {
					break;
				}
			}
		}

		IDirectDraw *pdd;
		HRESULT hr;

		// create DirectDraw object
		if (FAILED(pDirectDrawCreate(pguid, &pdd, NULL))) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't create DirectDraw2 object\n");
			break;
		}

		// query up to IDirectDraw2 (DirectX 3)
		hr = pdd->QueryInterface(IID_IDirectDraw2, (void **)&mpdd);
		pdd->Release();

		if (FAILED(hr))
			break;

		// get caps
		memset(&mCaps, 0, sizeof mCaps);
		mCaps.dwSize = sizeof(DDCAPS);
		hr = mpdd->GetCaps(&mCaps, NULL);
		if (FAILED(hr)) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't get caps\n");
			break;
		}

		// set cooperative level
		hr = mpdd->SetCooperativeLevel(NULL, DDSCL_NORMAL);
		if (FAILED(hr)) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't set cooperative level\n");
			break;
		}

		// attempt to create primary surface
		if (!InitPrimary())
			break;

		mInitCount = 1;
		mClients.push_back(pClient);
		return true;
	} while(false);

	Shutdown(NULL);
	return false;
}

bool VDDirectDrawManager::InitPrimary() {
	do {
		// attempt to create primary surface
		DDSURFACEDESC ddsdPri = {sizeof(DDSURFACEDESC)};
		IDirectDrawSurface *pdds;

		ddsdPri.dwFlags				= DDSD_CAPS;
		ddsdPri.ddsCaps.dwCaps		= DDSCAPS_PRIMARYSURFACE;
		ddsdPri.ddpfPixelFormat.dwSize	= sizeof(DDPIXELFORMAT);

		if (FAILED(mpdd->CreateSurface(&ddsdPri, &pdds, NULL))) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't create primary surface\n");
			break;
		}

		// query up to IDirectDrawSurface2 (DX3)
		HRESULT hr = pdds->QueryInterface(IID_IDirectDrawSurface2, (void **)&mpddsPrimary);
		pdds->Release();

		if (FAILED(hr))
			break;

		// We cannot call GetSurfaceDesc() on the Primary as it causes the Vista beta 2
		// DWM to freak out.
		if (FAILED(mpdd->GetDisplayMode(&ddsdPri))) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't get primary desc\n");
			break;
		}

		mPrimaryDesc = ddsdPri;

		return true;
	} while(false);

	ShutdownPrimary();
	return false;
}

bool VDDirectDrawManager::Restore() {
	if (mpddsPrimary) {
		if (SUCCEEDED(mpddsPrimary->IsLost()))
			return true;

		VDDEBUG_DISP("VDDirectDraw: Primary surface restore requested.\n");

		HRESULT hr = mpddsPrimary->Restore();

		if (FAILED(hr)) {
			VDDEBUG_DISP("VDDirectDraw: Primary surface restore failed -- tearing down DirectDraw!\n");

			++mInitCount;

			for(tClients::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
				IVDDirectDrawClient *pClient = *it;

				pClient->DirectDrawShutdown();
			}

			--mInitCount;

			if (!mInitCount) {
				VDDEBUG_DISP("VDDirectDraw: All clients vacated.\n");
				Shutdown(NULL);
				return false;
			}

			Shutdown(NULL);
			if (!Init(NULL)) {
				VDDEBUG_DISP("VDDirectDraw: Couldn't resurrect DirectDraw!\n");
				return false;
			}
		}
	} else {
		if (!InitPrimary())
			return false;
	}

	VDDEBUG_DISP("VDDirectDraw: Primary surface restore complete.\n");
	for(tClients::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
		IVDDirectDrawClient *pClient = *it;

		pClient->DirectDrawPrimaryRestored();
	}

	return true;
}

void VDDirectDrawManager::ShutdownPrimary() {
	if (mpddsPrimary) {
		mpddsPrimary->Release();
		mpddsPrimary = 0;
	}
}

bool VDDirectDrawManager::Shutdown(IVDDirectDrawClient *pClient) {
	if (pClient) {
		tClients::iterator it(std::find(mClients.begin(), mClients.end(), pClient));

		if (it != mClients.end()) {
			*it = mClients.back();
			mClients.pop_back();
		}

		if (--mInitCount)
			return false;
	}

	ShutdownPrimary();

	if (mpdd) {
		mpdd->Release();
		mpdd = 0;
	}

	if (mhmodDD) {
		FreeLibrary(mhmodDD);
		mhmodDD = 0;
	}

	return true;
}

static VDCriticalSection g_csVDDisplayDDManagers;
static vdlist<VDVideoDisplayDDManagerNode> g_VDDisplayDDManagers;

IVDDirectDrawManager *VDInitDirectDraw(HMONITOR hmonitor, IVDDirectDrawClient *pClient) {
	VDDirectDrawManager *pMgr = NULL;
	bool firstClient = false;
	VDThreadID tid = VDGetCurrentThreadID();

	vdsynchronized(g_csVDDisplayDDManagers) {
		vdlist<VDVideoDisplayDDManagerNode>::iterator it(g_VDDisplayDDManagers.begin()), itEnd(g_VDDisplayDDManagers.end());

		for(; it != itEnd; ++it) {
			VDDirectDrawManager *mgr = static_cast<VDDirectDrawManager *>(*it);

			if (mgr->GetThreadId() == tid && mgr->GetMonitor() == hmonitor) {
				pMgr = mgr;
				break;
			}
		}

		if (!pMgr) {
			pMgr = new_nothrow VDDirectDrawManager(tid, hmonitor);
			if (!pMgr)
				return NULL;

			g_VDDisplayDDManagers.push_back(pMgr);
			firstClient = true;
		}
	}

	if (!pMgr->Init(pClient)) {
		if (firstClient) {
			vdsynchronized(g_csVDDisplayDDManagers) {
				g_VDDisplayDDManagers.erase(pMgr);
			}

			delete pMgr;
		}

		return NULL;
	}

	return pMgr;
}

void VDShutdownDirectDraw(IVDDirectDrawManager *pIMgr, IVDDirectDrawClient *pClient) {
	VDDirectDrawManager *pMgr = static_cast<VDDirectDrawManager *>(pIMgr);

	if (!pMgr->Shutdown(pClient))
		return;

	vdsynchronized(g_csVDDisplayDDManagers) {
		vdlist<VDVideoDisplayDDManagerNode>::iterator it(g_VDDisplayDDManagers.find(pMgr));

		if (it != g_VDDisplayDDManagers.end())
			g_VDDisplayDDManagers.erase(it);
	}

	delete pMgr;
}

///////////////////////////////////////////////////////////////////////////

class VDVideoDisplayMinidriverDirectDraw : public VDVideoDisplayMinidriver, public IVDDisplayCompositionEngine, protected IVDDirectDrawClient {
public:
	VDVideoDisplayMinidriverDirectDraw(bool enableOverlays, bool enableOldSecondaryMonitorBehavior);
	~VDVideoDisplayMinidriverDirectDraw();

	bool Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info);
	void Shutdown();

	bool ModifySource(const VDVideoDisplaySourceInfo& info);

	bool IsValid();
	bool IsFramePending() { return mbPresentPending; }

	bool Tick(int id);
	void Poll();
	bool Resize(int w, int h);
	bool Update(UpdateMode);
	void Refresh(UpdateMode);
	bool Paint(HDC hdc, const RECT& rClient, UpdateMode mode);
	bool SetSubrect(const vdrect32 *r);
	void SetLogicalPalette(const uint8 *pLogicalPalette) { mpLogicalPalette = pLogicalPalette; }

	IVDDisplayCompositionEngine *GetDisplayCompositionEngine() override { return this; }

public:
	void LoadCustomEffect(const wchar_t *path) override {}

protected:
	enum {
		kOverlayUpdateTimerId = 200
	};

	void DirectDrawShutdown() {
		Shutdown();
		mbReset = true;
	}

	void DirectDrawPrimaryRestored() {
		memset(&mLastDisplayRect, 0, sizeof mLastDisplayRect);
	}

	bool InitOverlay();
	bool InitOffscreen();
	void ShutdownDisplay();
	bool UpdateOverlay(bool force);
	void InternalRefresh(const RECT& rClient, UpdateMode mode, bool newFrame, bool doNotWait);
	bool InternalBlt(IDirectDrawSurface2 *&pDest, RECT *prDst, RECT *prSrc, bool doNotWait, bool& stillDrawing, bool usingCompositorSurface);
	bool InternalFill(IDirectDrawSurface2 *&pDest, const RECT& rDst, uint32 nativeColor);

	uint32 ConvertToNativeColor(uint32 rgb32) const;

	HWND		mhwnd;
	IVDDirectDrawManager	*mpddman;
	IDirectDrawClipper	*mpddc;
	IDirectDrawSurface2	*mpddsBitmap;
	IDirectDrawSurface2	*mpddsOverlay;
	int			mPrimaryFormat;
	int			mPrimaryW;
	int			mPrimaryH;
	const uint8 *mpLogicalPalette;

	RECT		mLastDisplayRect;
	UINT		mOverlayUpdateTimer;

	COLORREF	mChromaKey;
	unsigned	mRawChromaKey;

	bool		mbReset;
	bool		mbValid;
	bool		mbFirstFrame;
	bool		mbRepaintOnNextUpdate;
	bool		mbPresentPending;
	bool		mbSwapChromaPlanes;
	bool		mbUseSubrect;
	uint32		mPresentPendingFlags;
	vdrect32	mSubrect;

	bool		mbEnableOverlays;
	bool		mbEnableSecondaryDraw;

	DDCAPS		mCaps;
	VDVideoDisplaySourceInfo	mSource;

	VDDisplayRendererDirectDraw	mRenderer;

	VDPixmapCachedBlitter	mCachedBlitter;

	VDDDrawPresentHistory	mPresentHistory;
};

IVDVideoDisplayMinidriver *VDCreateVideoDisplayMinidriverDirectDraw(bool enableOverlays, bool enableOldSecondaryMonitorBehavior) {
	return new VDVideoDisplayMinidriverDirectDraw(enableOverlays, enableOldSecondaryMonitorBehavior);
}

VDVideoDisplayMinidriverDirectDraw::VDVideoDisplayMinidriverDirectDraw(bool enableOverlays, bool enableOldSecondaryMonitorBehavior)
	: mhwnd(0)
	, mpddman(0)
	, mpddc(0)
	, mpddsBitmap(0)
	, mpddsOverlay(0)
	, mpLogicalPalette(NULL)
	, mOverlayUpdateTimer(0)
	, mbReset(false)
	, mbValid(false)
	, mbFirstFrame(false)
	, mbPresentPending(false)
	, mbRepaintOnNextUpdate(false)
	, mbUseSubrect(false)
	, mPresentPendingFlags(0)
	, mbEnableOverlays(enableOverlays)
	, mbEnableSecondaryDraw(enableOldSecondaryMonitorBehavior)
{
	memset(&mSource, 0, sizeof mSource);
}

VDVideoDisplayMinidriverDirectDraw::~VDVideoDisplayMinidriverDirectDraw() {
}

bool VDVideoDisplayMinidriverDirectDraw::Init(HWND hwnd, HMONITOR hmonitor, const VDVideoDisplaySourceInfo& info) {
	mCachedBlitter.Invalidate();

	switch(info.pixmap.format) {
	case nsVDPixmap::kPixFormat_Pal8:
	case nsVDPixmap::kPixFormat_XRGB1555:
	case nsVDPixmap::kPixFormat_RGB565:
	case nsVDPixmap::kPixFormat_RGB888:
	case nsVDPixmap::kPixFormat_XRGB8888:
	case nsVDPixmap::kPixFormat_YUV422_YUYV:
	case nsVDPixmap::kPixFormat_YUV422_YUYV_FR:
	case nsVDPixmap::kPixFormat_YUV422_YUYV_709:
	case nsVDPixmap::kPixFormat_YUV422_YUYV_709_FR:
	case nsVDPixmap::kPixFormat_YUV422_UYVY:
	case nsVDPixmap::kPixFormat_YUV422_UYVY_FR:
	case nsVDPixmap::kPixFormat_YUV422_UYVY_709:
	case nsVDPixmap::kPixFormat_YUV422_UYVY_709_FR:
	case nsVDPixmap::kPixFormat_YUV444_Planar:
	case nsVDPixmap::kPixFormat_YUV444_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV444_Planar_709:
	case nsVDPixmap::kPixFormat_YUV444_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV422_Planar:
	case nsVDPixmap::kPixFormat_YUV422_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV422_Planar_709:
	case nsVDPixmap::kPixFormat_YUV422_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV420_Planar:
	case nsVDPixmap::kPixFormat_YUV420_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV420_Planar_709:
	case nsVDPixmap::kPixFormat_YUV420_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV411_Planar:
	case nsVDPixmap::kPixFormat_YUV411_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV411_Planar_709:
	case nsVDPixmap::kPixFormat_YUV411_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV410_Planar:
	case nsVDPixmap::kPixFormat_YUV410_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV410_Planar_709:
	case nsVDPixmap::kPixFormat_YUV410_Planar_709_FR:
	case nsVDPixmap::kPixFormat_Y8:
	case nsVDPixmap::kPixFormat_Y8_FR:
	case nsVDPixmap::kPixFormat_YUV422_V210:
	case nsVDPixmap::kPixFormat_YUV420_NV12:
	case nsVDPixmap::kPixFormat_YUV420i_Planar:
	case nsVDPixmap::kPixFormat_YUV420i_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV420i_Planar_709:
	case nsVDPixmap::kPixFormat_YUV420i_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV420it_Planar:
	case nsVDPixmap::kPixFormat_YUV420it_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV420it_Planar_709:
	case nsVDPixmap::kPixFormat_YUV420it_Planar_709_FR:
	case nsVDPixmap::kPixFormat_YUV420ib_Planar:
	case nsVDPixmap::kPixFormat_YUV420ib_Planar_FR:
	case nsVDPixmap::kPixFormat_YUV420ib_Planar_709:
	case nsVDPixmap::kPixFormat_YUV420ib_Planar_709_FR:
		break;
	default:
		return false;
	}

	mhwnd	= hwnd;
	mSource	= info;

	do {
		mpddman = VDInitDirectDraw(hmonitor, this);
		if (!mpddman)
			break;

		mRenderer.Init(mpddman);

		// The Windows Vista DWM has a bug where it allows you to create an overlay surface even
		// though you'd never be able to display it -- so we have to detect the DWM and force
		// overlays off.
		bool allowOverlay = mbEnableOverlays && !mbUseSubrect;

		if (mbEnableOverlays) {
			// Looks like some systems have screwed up configs where either someone has inserted
			// a fake DWMAPI.DLL into the path or have somehow gotten it installed on an XP system;
			// the result is a failed dependency error when we try loading it. We avoid this by
			// explicitly checking for Windows Vista or higher.

			if (VDIsAtLeastVistaW32()) {
				HMODULE hmodDwmApi = VDLoadSystemLibraryW32("dwmapi");
				if (hmodDwmApi) {
					typedef HRESULT (WINAPI *tpDwmIsCompositionEnabled)(BOOL *);

					tpDwmIsCompositionEnabled pDwmIsCompositionEnabled = (tpDwmIsCompositionEnabled)GetProcAddress(hmodDwmApi, "DwmIsCompositionEnabled");
					if (pDwmIsCompositionEnabled) {
						BOOL enabled;
						HRESULT hr = pDwmIsCompositionEnabled(&enabled);

						if (SUCCEEDED(hr) && enabled)
							allowOverlay = false;
					}

					FreeLibrary(hmodDwmApi);
				}
			}
		}

		mCaps = mpddman->GetCaps();

		const DDSURFACEDESC& ddsdPri = mpddman->GetPrimaryDesc();

		mPrimaryW = ddsdPri.dwWidth;
		mPrimaryH = ddsdPri.dwHeight;

		// Interestingly enough, if another app goes full-screen, it's possible for us to lose
		// the device and have a failed Restore() between InitOverlay() and InitOffscreen().
		if ((allowOverlay && InitOverlay()) || (mpddman && InitOffscreen()))
			return true;

	} while(false);

	Shutdown();
	return false;
}

bool VDVideoDisplayMinidriverDirectDraw::InitOverlay() {
	DWORD dwFourCC;
	int minw = 1;
	int minh = 1;

	mbSwapChromaPlanes = false;
	switch(mSource.pixmap.format) {
	case nsVDPixmap::kPixFormat_YUV422_YUYV:
		dwFourCC = MAKEFOURCC('Y', 'U', 'Y', '2');
		minw = 2;
		break;

	case nsVDPixmap::kPixFormat_YUV422_UYVY:
		dwFourCC = MAKEFOURCC('U', 'Y', 'V', 'Y');
		minw = 2;
		break;

	case nsVDPixmap::kPixFormat_YUV420_Planar:
		dwFourCC = MAKEFOURCC('Y', 'V', '1', '2');
		mbSwapChromaPlanes = true;
		minw = 2;
		minh = 2;
		break;

	case nsVDPixmap::kPixFormat_YUV422_Planar:
		dwFourCC = MAKEFOURCC('Y', 'V', '1', '6');
		mbSwapChromaPlanes = true;
		minw = 2;
		break;

	case nsVDPixmap::kPixFormat_YUV410_Planar:
		dwFourCC = MAKEFOURCC('Y', 'V', 'U', '9');
		mbSwapChromaPlanes = true;
		minw = 4;
		minh = 4;
		break;

	case nsVDPixmap::kPixFormat_Y8:
		dwFourCC = MAKEFOURCC('Y', '8', ' ', ' ');
		mbSwapChromaPlanes = true;
		break;

	// Disabled because ForceWare 175.16 on XP+Quadro NVS 140M doesn't flip properly with
	// NV12 overlays.
	#if 0
		case nsVDPixmap::kPixFormat_YUV420_NV12:
			dwFourCC = MAKEFOURCC('N', 'V', '1', '2');
			minw = 2;
			minh = 2;
			break;
	#endif

	default:
		return false;
	}

	do {
		// attempt to create clipper (we need this for chromakey fills)
		if (FAILED(mpddman->GetDDraw()->CreateClipper(0, &mpddc, 0)))
			break;

		if (FAILED(mpddc->SetHWnd(0, mhwnd)))
			break;

		// create overlay surface
		DDSURFACEDESC ddsdOff = {sizeof(DDSURFACEDESC)};

		ddsdOff.dwFlags						= DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;
		ddsdOff.dwWidth						= (mSource.pixmap.w + minw - 1) & -minw;
		ddsdOff.dwHeight					= (mSource.pixmap.h + minh - 1) & -minh;
		ddsdOff.ddsCaps.dwCaps				= DDSCAPS_OVERLAY | DDSCAPS_VIDEOMEMORY;
		ddsdOff.ddpfPixelFormat.dwSize		= sizeof(DDPIXELFORMAT);
		ddsdOff.ddpfPixelFormat.dwFlags		= DDPF_FOURCC;
		ddsdOff.ddpfPixelFormat.dwFourCC	= dwFourCC;

		if (mCaps.dwCaps & DDCAPS_ALIGNSIZESRC) {
			ddsdOff.dwWidth += mCaps.dwAlignSizeSrc - 1;
			ddsdOff.dwWidth -= ddsdOff.dwWidth % mCaps.dwAlignSizeSrc;
		}

		IDirectDrawSurface *pdds;
		HRESULT hr = mpddman->GetDDraw()->CreateSurface(&ddsdOff, &pdds, NULL);

		if (FAILED(hr)) {
			DEBUG_LOG("VideoDisplay/DDraw: Overlay surface creation failed\n");
			break;
		}

		hr = pdds->QueryInterface(IID_IDirectDrawSurface2, (void **)&mpddsOverlay);
		pdds->Release();

		if (FAILED(hr))
			break;

		// Do not allow colorkey if the primary surface is paletted, as we may not be able
		// to reliably choose the correct color.
		mChromaKey = 0;

		if (!(mpddman->GetPrimaryDesc().ddpfPixelFormat.dwFlags & (DDPF_PALETTEINDEXED8|DDPF_PALETTEINDEXED4))) {
			if (mCaps.dwCKeyCaps & DDCKEYCAPS_DESTOVERLAY) {
				const DDSURFACEDESC& ddsdPri = mpddman->GetPrimaryDesc();

				mRawChromaKey = ddsdPri.ddpfPixelFormat.dwGBitMask & ~(ddsdPri.ddpfPixelFormat.dwGBitMask >> 1);
				mChromaKey = RGB(0,128,0);
			}
		}

		mOverlayUpdateTimer = SetTimer(mhwnd, kOverlayUpdateTimerId, 100, NULL);
		memset(&mLastDisplayRect, 0, sizeof mLastDisplayRect);

		VDDEBUG_DISP("VideoDisplay: Using DirectDraw overlay for %dx%d %s display.\n", mSource.pixmap.w, mSource.pixmap.h, VDPixmapGetInfo(mSource.pixmap.format).name);
		DEBUG_LOG("VideoDisplay/DDraw: Overlay surface creation successful\n");

		mbRepaintOnNextUpdate = true;
		mbValid = false;

		if (!UpdateOverlay(false))
			break;

		return true;
	} while(false);

	ShutdownDisplay();
	return false;
}

bool VDVideoDisplayMinidriverDirectDraw::InitOffscreen() {
	HRESULT hr;

	do {
		const DDPIXELFORMAT& pf = mpddman->GetPrimaryDesc().ddpfPixelFormat;

		// determine primary surface pixel format
		if (pf.dwFlags & DDPF_PALETTEINDEXED8) {
			mPrimaryFormat = nsVDPixmap::kPixFormat_Pal8;
			VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is 8-bit paletted.\n");
		} else if (pf.dwFlags & DDPF_RGB) {
			if (   pf.dwRGBBitCount == 16 && pf.dwRBitMask == 0x7c00 && pf.dwGBitMask == 0x03e0 && pf.dwBBitMask == 0x001f) {
				mPrimaryFormat = nsVDPixmap::kPixFormat_XRGB1555;
				VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is 16-bit xRGB (1-5-5-5).\n");
			} else if (pf.dwRGBBitCount == 16 && pf.dwRBitMask == 0xf800 && pf.dwGBitMask == 0x07e0 && pf.dwBBitMask == 0x001f) {
				mPrimaryFormat = nsVDPixmap::kPixFormat_RGB565;
				VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is 16-bit RGB (5-6-5).\n");
			} else if (pf.dwRGBBitCount == 24 && pf.dwRBitMask == 0xff0000 && pf.dwGBitMask == 0x00ff00 && pf.dwBBitMask == 0x0000ff) {
				mPrimaryFormat = nsVDPixmap::kPixFormat_RGB888;
				VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is 24-bit RGB (8-8-8).\n");
			} else if (pf.dwRGBBitCount == 32 && pf.dwRBitMask == 0xff0000 && pf.dwGBitMask == 0x00ff00 && pf.dwBBitMask == 0x0000ff) {
				mPrimaryFormat = nsVDPixmap::kPixFormat_XRGB8888;
				VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is 32-bit xRGB (8-8-8-8).\n");
			} else
				break;
		} else
			break;

		if (mPrimaryFormat != mSource.pixmap.format) {
			if (!mSource.bAllowConversion) {
				VDDEBUG_DISP("VideoDisplay/DirectDraw: Display is not compatible with source and conversion is disallowed.\n");
				return false;
			}
		}

		// attempt to create clipper
		if (FAILED(mpddman->GetDDraw()->CreateClipper(0, &mpddc, 0)))
			break;

		if (FAILED(mpddc->SetHWnd(0, mhwnd)))
			break;

		// create bitmap surface
		DDSURFACEDESC ddsdOff = {sizeof(DDSURFACEDESC)};

		ddsdOff.dwFlags					= DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT;
		ddsdOff.dwWidth					= mSource.pixmap.w;
		ddsdOff.dwHeight				= mSource.pixmap.h;
		ddsdOff.ddsCaps.dwCaps			= DDSCAPS_OFFSCREENPLAIN;
		ddsdOff.ddpfPixelFormat			= pf;

		IDirectDrawSurface *pdds = NULL;

		// if the source is persistent, try to create the surface directly into system memory
		if (mSource.bPersistent) {
			mSource.bPersistent = false;

#if 0		// doesn't work in DX3 -- need DX7 interfaces to create client surfaces
			if (mPrimaryFormat == mSource.format) {
				DDSURFACEDESC ddsdOff2(ddsdOff);

				ddsdOff2.dwFlags			= DDSD_CAPS | DDSD_WIDTH | DDSD_HEIGHT | DDSD_PIXELFORMAT | DDSD_PITCH | DDSD_LPSURFACE;
				ddsdOff2.lpSurface			= (void *)mSource.data;
				ddsdOff2.lPitch				= mSource.pitch;
				ddsdOff2.ddsCaps.dwCaps		= DDSCAPS_OFFSCREENPLAIN | DDSCAPS_SYSTEMMEMORY;
				if (SUCCEEDED(mpddman->GetDDraw()->CreateSurface(&ddsdOff2, &pdds, NULL))) {
					DEBUG_LOG("VideoDriver/DDraw: Created surface directly in system memory (lucky!)\n");
					mSource.bPersistent = true;
				}
			}
#endif
		}

		if (!pdds && FAILED(mpddman->GetDDraw()->CreateSurface(&ddsdOff, &pdds, NULL))) {
			DEBUG_LOG("VideoDriver/DDraw: Couldn't create offscreen surface\n");
			break;
		}

		hr = pdds->QueryInterface(IID_IDirectDrawSurface2, (void **)&mpddsBitmap);
		pdds->Release();

		if (FAILED(hr))
			break;

		mChromaKey = 0;
		mbValid = false;
		mbRepaintOnNextUpdate = false;
		mbFirstFrame = true;

		DEBUG_LOG("VideoDriver/DDraw: Offscreen initialization successful\n");
		VDDEBUG_DISP("VideoDisplay: Using DirectDraw offscreen surface for %dx%d %s display.\n", mSource.pixmap.w, mSource.pixmap.h, VDPixmapGetInfo(mSource.pixmap.format).name);
		return true;
	} while(false); 

	ShutdownDisplay();
	return false;
}

void VDVideoDisplayMinidriverDirectDraw::ShutdownDisplay() {
	if (mpddc) {
		mpddc->Release();
		mpddc = 0;
	}

	if (mpddsBitmap) {
		mpddsBitmap->Release();
		mpddsBitmap = 0;
	}

	if (mpddsOverlay) {
		mpddsOverlay->Release();
		mpddsOverlay = 0;
	}

	mbValid = false;
}

void VDVideoDisplayMinidriverDirectDraw::Shutdown() {
	ShutdownDisplay();
	
	mRenderer.Shutdown();

	if (mpddman) {
		VDShutdownDirectDraw(mpddman, this);
		mpddman = NULL;
	}
}

bool VDVideoDisplayMinidriverDirectDraw::ModifySource(const VDVideoDisplaySourceInfo& info) {
	if (!mpddsBitmap && !mpddsOverlay)
		return false;

	if (mSource.pixmap.w == info.pixmap.w && mSource.pixmap.h == info.pixmap.h && mSource.pixmap.format == info.pixmap.format) {
		mSource = info;
		return true;
	}

	return false;
}

bool VDVideoDisplayMinidriverDirectDraw::IsValid() {
	return mbValid && ((mpddsOverlay && DD_OK == mpddsOverlay->IsLost()) || (mpddsBitmap && DD_OK == mpddsBitmap->IsLost()));
}

bool VDVideoDisplayMinidriverDirectDraw::Tick(int id) {
	if (id == kOverlayUpdateTimerId) {
		RECT r;
		GetClientRect(mhwnd, &r);
		MapWindowPoints(mhwnd, NULL, (LPPOINT)&r, 2);

		if (memcmp(&r, &mLastDisplayRect, sizeof(RECT)))
			Resize(r.right - r.left, r.bottom - r.top);
	}

	return !mbReset;
}

void VDVideoDisplayMinidriverDirectDraw::Poll() {
	if (mbPresentPending) {
		RECT rClient = { mClientRect.left, mClientRect.top, mClientRect.right, mClientRect.bottom };

		InternalRefresh(rClient, (UpdateMode)mPresentPendingFlags, false, true);
	}
}

bool VDVideoDisplayMinidriverDirectDraw::Resize(int w, int h) {
	if (!VDVideoDisplayMinidriver::Resize(w, h))
		return false;

	if (mpddsOverlay)
		UpdateOverlay(false);

	return !mbReset;
}

bool VDVideoDisplayMinidriverDirectDraw::UpdateOverlay(bool force) {
	do {
		RECT rDst0;

		GetClientRect(mhwnd, &rDst0);
		MapWindowPoints(mhwnd, NULL, (LPPOINT)&rDst0, 2);

		// destination clipping
		RECT rDst = rDst0;
		const int dstw = rDst.right - rDst.left;
		const int dsth = rDst.bottom - rDst.top;

		if (rDst.left < 0)
			rDst.left = 0;

		if (rDst.top < 0)
			rDst.top = 0;

		if (rDst.right > mPrimaryW)
			rDst.right = mPrimaryW;

		if (rDst.bottom > mPrimaryH)
			rDst.bottom = mPrimaryH;

		if (rDst.bottom <= rDst.top || rDst.right <= rDst.left)
			break;

		// source clipping
		RECT rSrc = {
			(rDst.left   - rDst0.left) * mSource.pixmap.w / dstw,
			(rDst.top    - rDst0.top ) * mSource.pixmap.h / dsth,
			(rDst.right  - rDst0.left) * mSource.pixmap.w / dstw,
			(rDst.bottom - rDst0.top ) * mSource.pixmap.h / dsth,
		};

		// source alignment
		if (mCaps.dwCaps & DDCAPS_ALIGNBOUNDARYSRC) {
			int align = mCaps.dwAlignBoundarySrc;
			rSrc.left -= rSrc.left % align;
		}

		if (mCaps.dwCaps & DDCAPS_ALIGNSIZESRC) {
			int w = rSrc.right - rSrc.left;

			w -= w % mCaps.dwAlignSizeSrc;

			rSrc.right = rSrc.left + w;
		}

		// destination alignment
		if (mCaps.dwCaps & DDCAPS_ALIGNBOUNDARYDEST) {
			int align = mCaps.dwAlignBoundaryDest;

			rDst.left += align-1;
			rDst.left -= rDst.left % align;
		}

		if (mCaps.dwCaps & DDCAPS_ALIGNSIZEDEST) {
			int w = rDst.right - rDst.left;

			w -= w % mCaps.dwAlignSizeDest;

			if (w <= 0)
				break;

			rDst.right = rDst.left + w;
		}

		DWORD dwFlags = DDOVER_SHOW | DDOVER_DDFX;
		DDOVERLAYFX ddfx = {sizeof(DDOVERLAYFX)};

		if (mChromaKey) {
			dwFlags |= DDOVER_KEYDESTOVERRIDE;
			ddfx.dckDestColorkey.dwColorSpaceLowValue = mRawChromaKey;
			ddfx.dckDestColorkey.dwColorSpaceHighValue = mRawChromaKey;
		}

		if (mCaps.dwFXCaps & DDFXCAPS_OVERLAYARITHSTRETCHY)
			ddfx.dwFlags |= DDOVERFX_ARITHSTRETCHY;

		IDirectDrawSurface2 *pDest = mpddman->GetPrimary();
		HRESULT hr = mpddsOverlay->UpdateOverlay(&rSrc, pDest, &rDst, dwFlags, &ddfx);

		if (FAILED(hr)) {
			mbValid = false;
			memset(&mLastDisplayRect, 0, sizeof mLastDisplayRect);

			// NVIDIA ForceWare 96.85 for Vista allows us to create multiple overlays,
			// but attempting to show more than one gives DDERR_NOTAVAILABLE.
			if (hr != DDERR_SURFACELOST)
				return false;

			if (FAILED(mpddsOverlay->Restore()))
				return false;

			if (FAILED(pDest->IsLost()) && mpddman->Restore())
				return false;
		} else
			mLastDisplayRect = rDst0;
		return !mbReset;
	} while(false);

	mpddsOverlay->UpdateOverlay(NULL, mpddman->GetPrimary(), NULL, DDOVER_HIDE, NULL);
	return !mbReset;
}

bool VDVideoDisplayMinidriverDirectDraw::Update(UpdateMode mode) {
	if (!mSource.pixmap.data)
		return false;

	HRESULT hr;
	DDSURFACEDESC ddsd = { sizeof(DDSURFACEDESC) };

	ddsd.ddpfPixelFormat.dwSize = sizeof(DDPIXELFORMAT);
	
	const DWORD dwLockFlags = DDLOCK_WRITEONLY | DDLOCK_WAIT;

	// When NView reverts between dual-display modes, we can get a DDERR_SURFACELOST on which
	// Restore() succeeds, but the next lock still fails. We insert a safety counter here to
	// prevent a hang.
	IDirectDrawSurface2 *pTarget;
	bool needRestore = false;

	for(int retries=0; retries<5; ++retries) {
		// We need to re-read this each time as a restore will create new ones.
		pTarget = mpddsBitmap ? mpddsBitmap : mpddsOverlay;

		if (!pTarget)
			return false;

		if (needRestore) {
			needRestore = false;

			hr = pTarget->Restore();
			if (FAILED(hr))
				break;
		}

		hr = pTarget->Lock(NULL, &ddsd, dwLockFlags, 0);

		if (SUCCEEDED(hr))
			break;

		if (hr != DDERR_SURFACELOST)
			break;

		mbValid = false;
		memset(&mLastDisplayRect, 0, sizeof mLastDisplayRect);

		if (!mpddman->Restore())
			break;

		needRestore = true;
	}

	if (FAILED(hr)) {
		mbValid = false;
		memset(&mLastDisplayRect, 0, sizeof mLastDisplayRect);
		return false;
	}

	VDPixmap source(mSource.pixmap);

	char *dst = (char *)ddsd.lpSurface;
	ptrdiff_t dstpitch = ddsd.lPitch;

	VDPixmap dstbm = { dst, NULL, (vdpixsize)ddsd.dwWidth, (vdpixsize)ddsd.dwHeight, dstpitch, mPrimaryFormat };

	if (mpddsOverlay)
		dstbm.format = source.format;

	const VDPixmapFormatInfo& dstinfo = VDPixmapGetInfo(dstbm.format);

	if (dstinfo.auxbufs >= 1) {
		const int qw = -(-dstbm.w >> dstinfo.qwbits);
		const int qh = -(-dstbm.h >> dstinfo.qhbits);

		VDASSERT((qw << dstinfo.qwbits) == dstbm.w);
		VDASSERT((qh << dstinfo.qhbits) == dstbm.h);

		dstbm.data2		= (char *)dstbm.data + dstpitch * qh;
		dstbm.pitch2	= dstpitch >> dstinfo.auxwbits;

		if (dstinfo.auxbufs >= 2) {
			dstbm.data3 = (char *)dstbm.data2 + dstbm.pitch2 * -(-dstbm.h >> dstinfo.auxhbits);
			dstbm.pitch3 = dstbm.pitch2;
		}

		if (mbSwapChromaPlanes) {
			std::swap(dstbm.data2, dstbm.data3);
			std::swap(dstbm.pitch2, dstbm.pitch3);
		}
	}

	bool dither = false;
	if (dstbm.format == nsVDPixmap::kPixFormat_Pal8) {
		switch(source.format) {
			case nsVDPixmap::kPixFormat_Pal8:
			case nsVDPixmap::kPixFormat_XRGB1555:
			case nsVDPixmap::kPixFormat_RGB565:
			case nsVDPixmap::kPixFormat_RGB888:
			case nsVDPixmap::kPixFormat_XRGB8888:
				dither = true;
				break;
		}
	}

	if (dither)
		VDDitherImage(dstbm, source, mpLogicalPalette);
	else
		mCachedBlitter.Blit(dstbm, source);
	
	hr = pTarget->Unlock(0);

	mbValid = SUCCEEDED(hr);

	if (mbValid) {
		mbPresentPending = true;
		mPresentPendingFlags = mode;
	}

	return !mbReset;
}

void VDVideoDisplayMinidriverDirectDraw::Refresh(UpdateMode mode) {
	if (mbValid) {
		if (mpddsOverlay) {
			Tick(kOverlayUpdateTimerId);
			if (mbRepaintOnNextUpdate) {
				InvalidateRect(mhwnd, NULL, TRUE);
				mbRepaintOnNextUpdate = false;
			}

			mbPresentPending = false;
			mSource.mpCB->RequestNextFrame();
		} else {
			RECT r;
			GetClientRect(mhwnd, &r);
			InternalRefresh(r, mode, true, (mode & kModeVSync) != 0);
		}
	}
}

bool VDVideoDisplayMinidriverDirectDraw::Paint(HDC hdc, const RECT& rClient, UpdateMode mode) {
	// Paint the black borders around the dest rect. We only do this if
	// composition is disabled, as otherwise the borders are painted as
	// part of the composition surface.
	if (mBorderRectCount && !mpCompositor) {
		const uint32 backgroundNativeColor = ConvertToNativeColor(mBackgroundColor);

		IDirectDrawSurface2 *pDest = mpddman->GetPrimary();
		if (pDest) {
			pDest->SetClipper(mpddc);

			for(int i=0; i<mBorderRectCount; ++i) {
				const vdrect32& rFill = mBorderRects[i];
				RECT rFill2 = { rFill.left, rFill.top, rFill.right, rFill.bottom };

				MapWindowPoints(mhwnd, NULL, (LPPOINT)&rFill2, 2);
				InternalFill(pDest, rFill2, backgroundNativeColor);

				if (!pDest)
					break;
			}

			if (pDest)
				pDest->SetClipper(NULL);
		}
	}

	if (mpddsOverlay) {
		if (mChromaKey) {
			IDirectDrawSurface2 *pDest = mpddman->GetPrimary();

			if (pDest) {
				pDest->SetClipper(mpddc);

				RECT rFill = rClient;
				MapWindowPoints(mhwnd, NULL, (LPPOINT)&rFill, 2);
				InternalFill(pDest, rFill, mRawChromaKey);

				if (pDest)
					pDest->SetClipper(NULL);
			}
		}
	} else {
		InternalRefresh(rClient, mode, true, false);
	}

	// Workaround for Windows Vista DWM not adding window to composition tree immediately
	if (mbFirstFrame) {
		mbFirstFrame = false;
		SetWindowPos(mhwnd, NULL, 0, 0, 0, 0, SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
	}

	return !mbReset;
}

bool VDVideoDisplayMinidriverDirectDraw::SetSubrect(const vdrect32 *r) {
	if (mpddsOverlay)
		return false;

	if (r) {
		mbUseSubrect = true;
		mSubrect = *r;
	} else
		mbUseSubrect = false;

	return true;
}

void VDVideoDisplayMinidriverDirectDraw::InternalRefresh(const RECT& rClient, UpdateMode mode, bool newFrame, bool doNotWait) {
	RECT rDst = rClient;

	if (mbDestRectEnabled) {
		rDst.left = mDrawRect.left;
		rDst.top = mDrawRect.top;
		rDst.right = mDrawRect.right;
		rDst.bottom = mDrawRect.bottom;
	}

	// DirectX doesn't like null rects.
	if (rDst.right <= rDst.left || rDst.bottom <= rDst.top)
		return;

	IDirectDrawSurface2 *pDest = mpddman->GetPrimary();

	if (!pDest)
		return;

	// DDBLTFX_NOTEARING is ignored by DirectDraw in 2K/XP.
	if (!(mode & kModeVSync)) {
		mPresentHistory.mbPresentPending = false;
	} else {
		IDirectDraw2 *pDD = mpddman->GetDDraw();

		RECT rScanArea = rClient;
		
		if (!mpCompositor && mbDestRectEnabled) {
			rScanArea.left = mDrawRect.left;
			rScanArea.top = mDrawRect.top;
			rScanArea.right = mDrawRect.right;
			rScanArea.bottom = mDrawRect.bottom;
		}

		MapWindowPoints(mhwnd, NULL, (LPPOINT)&rScanArea, 2);

		if (newFrame && !mPresentHistory.mbPresentPending) {
			const vdrect32& monitorRect = mpddman->GetMonitorRect();
			int top = monitorRect.top;
			int bottom = monitorRect.bottom;

			if (rScanArea.top < top)
				rScanArea.top = top;
			if (rScanArea.bottom > bottom)
				rScanArea.bottom = bottom;

			rScanArea.top -= top;
			rScanArea.bottom -= top;

			mPresentHistory.mScanTop = rScanArea.top;
			mPresentHistory.mScanBottom = rScanArea.bottom;

			mPresentHistory.mbPresentPending = true;
			mPresentHistory.mbPresentBlitStarted = false;

			mPresentHistory.mLastScanline = -1;
			mPresentHistory.mPresentStartTime = VDGetPreciseTick();
		}

		if (!mPresentHistory.mbPresentPending)
			return;

		// Poll raster status, and wait until we can safely blit. We assume that the
		// blit can outrace the beam. 
		++mPresentHistory.mPollCount;
		for(;;) {
			// if we've already started the blit, skip beam-following
			if (mPresentHistory.mbPresentBlitStarted)
				break;

			DWORD scan;
			bool inVBlank = false;
			HRESULT hr = pDD->GetScanLine(&scan);
			if (FAILED(hr)) {
				scan = 0;
				inVBlank = true;
			}

			sint32 y1 = (sint32)mPresentHistory.mLastScanline;
			if (y1 < 0) {
				y1 = scan;
				mPresentHistory.mAverageStartScanline += ((float)y1 - mPresentHistory.mAverageStartScanline) * 0.01f;
			}

			sint32 y2 = (sint32)scan;

			mPresentHistory.mbLastWasVBlank	= inVBlank ? true : false;
			mPresentHistory.mLastScanline	= scan;

			sint32 yt = (sint32)mPresentHistory.mScanlineTarget;

			mPresentHistory.mLastBracketY1 = y1;
			mPresentHistory.mLastBracketY2 = y2;

			// check for yt in [y1, y2]... but we have to watch for a beam wrap (y1 > y2).
			if (y1 <= y2) {
				// non-wrap case
				if (y1 <= yt && yt <= y2)
					break;
			} else {
				// wrap case
				if (y1 <= yt || yt <= y2)
					break;
			}

			if (doNotWait)
				return;

			::Sleep(1);
		}

		mPresentHistory.mbPresentBlitStarted = true;
	}

	pDest->SetClipper(mpddc);

	bool usingCompositorSurface = false;

	VDDisplayCompositeInfo compInfo = {};

	if (mpCompositor) {
		compInfo.mWidth = rClient.right;
		compInfo.mHeight = rClient.bottom;

		mpCompositor->PreComposite(compInfo);

		if (mRenderer.Begin(rClient.right, rClient.bottom, (nsVDPixmap::VDPixmapFormat)mPrimaryFormat)) {
			pDest = mRenderer.GetCompositionSurface();
			usingCompositorSurface = true;
		}
	}

	if (!usingCompositorSurface)
		MapWindowPoints(mhwnd, NULL, (LPPOINT)&rDst, 2);

	bool success = true;
	bool stillDrawing = false;

	if (mColorOverride) {
		// convert color to primary surface format
		DDBLTFX fx = {sizeof(DDBLTFX)};
		fx.dwFillColor = ConvertToNativeColor(mColorOverride);

		for(int i=0; i<5; ++i) {
			HRESULT hr = pDest->Blt(&rDst, NULL, NULL, DDBLT_ASYNC | DDBLT_WAIT | DDBLT_COLORFILL, &fx);

			if (SUCCEEDED(hr))
				break;

			if (hr != DDERR_SURFACELOST)
				break;

			if (FAILED(pDest->IsLost())) {
				pDest->SetClipper(NULL);
				pDest = NULL;

				if (!mpddman->Restore())
					return;

				if (mbReset || usingCompositorSurface)
					return;

				pDest = mpddman->GetPrimary();
				if (!pDest)
					break;

				pDest->SetClipper(mpddc);
			}
		}
	} else {
		if (mbUseSubrect) {
			RECT rSrc = { mSubrect.left, mSubrect.top, mSubrect.right, mSubrect.bottom };
			success = InternalBlt(pDest, &rDst, &rSrc, doNotWait, stillDrawing, usingCompositorSurface);
		} else
			success = InternalBlt(pDest, &rDst, NULL, doNotWait, stillDrawing, usingCompositorSurface);
	}

	if (doNotWait && stillDrawing)
		return;

	if (usingCompositorSurface) {
		if (mbDestRectEnabled) {
			mRenderer.SetColorRGB(mBackgroundColor);

			for(int i=0; i<mBorderRectCount; ++i) {
				const vdrect32& rFill = mBorderRects[i];

				mRenderer.FillRect(rFill.left, rFill.top, rFill.width(), rFill.height());
			}

			mRenderer.SetColorRGB(0);
		}

		mpCompositor->Composite(mRenderer, compInfo);

		IDirectDrawSurface2 *primary = mpddman->GetPrimary();
		primary->SetClipper(mpddc);

		POINT pt = { 0, 0 };

		::ClientToScreen(mhwnd, &pt);
		mRenderer.End(pt.x, pt.y);

		primary->SetClipper(NULL);
	} else {
		if (pDest)
			pDest->SetClipper(NULL);
	}

	mbPresentPending = false;

	if (mode & kModeVSync) {
		mPresentHistory.mbPresentPending = false;

		if (!success)
			return;

		mPresentHistory.mAverageEndScanline += ((float)mPresentHistory.mLastScanline - mPresentHistory.mAverageEndScanline) * 0.01f;
		mPresentHistory.mAveragePresentTime += ((VDGetPreciseTick() - mPresentHistory.mPresentStartTime)*VDGetPreciseSecondsPerTick() - mPresentHistory.mAveragePresentTime) * 0.01f;

		IDirectDraw2 *pDD = mpddman->GetDDraw();
		DWORD scan2;
		bool inVBlank2 = false;
		HRESULT hr = pDD->GetScanLine(&scan2);
		if (hr == DDERR_VERTICALBLANKINPROGRESS) {
			scan2 = 0;
			inVBlank2 = true;
			hr = S_OK;
		}

		float syncDelta = 0.0f;
		if (SUCCEEDED(hr)) {
			float yf = ((float)scan2 - (float)mPresentHistory.mScanTop) / ((float)mPresentHistory.mScanBottom - (float)mPresentHistory.mScanTop);

			yf -= 0.2f;

			if (yf < 0.0f)
				yf = 0.0f;
			if (yf > 1.0f)
				yf = 1.0f;

			if (yf > 0.5f)
				yf -= 1.0f;

			syncDelta = yf;

			int displayHeight = mpddman->GetPrimaryDesc().dwHeight;

			mPresentHistory.mScanlineTarget -= yf * 15.0f;
			if (mPresentHistory.mScanlineTarget < 0.0f)
				mPresentHistory.mScanlineTarget += (float)displayHeight;
			else if (mPresentHistory.mScanlineTarget >= (float)displayHeight)
				mPresentHistory.mScanlineTarget -= (float)displayHeight;

			float success = inVBlank2 || (int)scan2 <= mPresentHistory.mScanTop || (int)scan2 >= mPresentHistory.mScanBottom ? 1.0f : 0.0f;

			int zone = 0;
			if (!mPresentHistory.mbLastWasVBlank)
				zone = ((int)mPresentHistory.mLastScanline * 16) / displayHeight;

			for(int i=0; i<17; ++i) {
				if (i != zone)
					mPresentHistory.mAttemptProb[i] *= 0.99f;
			}

			mPresentHistory.mAttemptProb[zone] += (1.0f - mPresentHistory.mAttemptProb[zone]) * 0.01f;
			mPresentHistory.mSuccessProb[zone] += (success - mPresentHistory.mSuccessProb[zone]) * 0.01f;

			if (mPresentHistory.mLastScanline < mPresentHistory.mScanTop) {
				mPresentHistory.mVBlankSuccess += (success - mPresentHistory.mVBlankSuccess) * 0.01f;
			}

			if (!mPresentHistory.mbLastWasVBlank && !inVBlank2 && (int)scan2 > mPresentHistory.mLastScanline) {
				float delta = (float)(int)(scan2 - mPresentHistory.mLastScanline);

				mPresentHistory.mPresentDelay += (delta - mPresentHistory.mPresentDelay) * 0.01f;
			}
		}
	}

	// Workaround for Windows Vista DWM not adding window to composition tree immediately
	if (mbFirstFrame) {
		mbFirstFrame = false;
		SetWindowPos(mhwnd, NULL, 0, 0, 0, 0, SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
	}

	mSource.mpCB->RequestNextFrame();

	return;
}

bool VDVideoDisplayMinidriverDirectDraw::InternalBlt(IDirectDrawSurface2 *&pDest, RECT *prDst, RECT *prSrc, bool doNotWait, bool& stillDrawing, bool usingCompositorSurface) {
	HRESULT hr;
	DWORD flags = doNotWait ? DDBLT_ASYNC : DDBLT_ASYNC | DDBLT_WAIT;
	RECT rdstClip;
	RECT rsrcClip;

	if (prDst && !mbEnableSecondaryDraw) {
		// NVIDIA drivers have an annoying habit of glitching horribly when the blit rectangle extends outside of the
		// primary monitor onto a secondary, so we clip manually.
		const vdrect32& rclip = mpddman->GetMonitorRect();

		if (prDst->left >= prDst->right && prDst->bottom >= prDst->top) {
			stillDrawing = false;
			return true;
		}

		RECT rsrc0;
		if (prSrc)
			rsrc0 = *prSrc;
		else {
			rsrc0.left = 0;
			rsrc0.top = 0;
			rsrc0.right = mSource.pixmap.w;
			rsrc0.bottom = mSource.pixmap.h;
		}

		rsrcClip = rsrc0;
		int offsetL = prDst->left - rclip.left;
		int offsetT = prDst->top - rclip.top;
		int offsetR = rclip.right - prDst->right;
		int offsetB = rclip.bottom - prDst->bottom;

		if ((offsetL | offsetT | offsetR | offsetB) < 0) {
			rdstClip = *prDst;

			float xRatio = (float)(rsrc0.right - rsrc0.left) / (float)(rdstClip.right - rdstClip.left);
			float yRatio = (float)(rsrc0.bottom - rsrc0.top) / (float)(rdstClip.bottom - rdstClip.top);

			if (offsetL < 0) {
				rdstClip.left = rclip.left;
				rsrcClip.left -= VDRoundToInt(offsetL * xRatio);
			}

			if (offsetT < 0) {
				rdstClip.top = rclip.top;
				rsrcClip.top -= VDRoundToInt(offsetT * yRatio);
			}

			if (offsetR < 0) {
				rdstClip.right = rclip.right;
				rsrcClip.right += VDRoundToInt(offsetR * xRatio);
			}

			if (offsetB < 0) {
				rdstClip.bottom = rclip.bottom;
				rsrcClip.bottom += VDRoundToInt(offsetB * yRatio);
			}

			if (rdstClip.left >= rdstClip.right || rdstClip.top >= rdstClip.bottom) {
				stillDrawing = false;
				return true;
			}

			if (rsrcClip.right <= rsrcClip.left) {
				rsrcClip.left = (rsrc0.left + rsrc0.right) >> 1;
				rsrcClip.right = rsrcClip.left + 1;
			}

			if (rsrcClip.bottom <= rsrcClip.top) {
				rsrcClip.top = (rsrc0.top + rsrc0.bottom) >> 1;
				rsrcClip.bottom = rsrcClip.top + 1;
			}

			prDst = &rdstClip;
			prSrc = &rsrcClip;
		}
	}

	stillDrawing = false;
	for(;;) {
		RECT rdstOffset;

		// offset dest rect from screen coordinates to primary surface coordinates
		if (prDst) {
			const vdrect32& rMonitor = mpddman->GetMonitorRect();
			rdstOffset.left = prDst->left - rMonitor.left;
			rdstOffset.top = prDst->top - rMonitor.top;
			rdstOffset.right = prDst->right - rMonitor.left;
			rdstOffset.bottom = prDst->bottom - rMonitor.top;

			prDst = &rdstOffset;
		}

		hr = pDest->Blt(prDst, mpddsBitmap, prSrc, 0, NULL);

		if (hr == DDERR_WASSTILLDRAWING) {
			stillDrawing = true;
			return true;
		}

		if (SUCCEEDED(hr))
			break;

		if (hr != DDERR_SURFACELOST || usingCompositorSurface)
			break;

		if (FAILED(mpddsBitmap->IsLost())) {
			mpddsBitmap->Restore();
			mbValid = false;
			break;
		}

		if (FAILED(pDest->IsLost())) {
			pDest->SetClipper(NULL);
			pDest = NULL;

			if (!mpddman->Restore())
				return false;

			if (mbReset)
				return false;

			pDest = mpddman->GetPrimary();
			pDest->SetClipper(mpddc);
		}
	}

	return SUCCEEDED(hr);
}

bool VDVideoDisplayMinidriverDirectDraw::InternalFill(IDirectDrawSurface2 *&pDest, const RECT& rDst, uint32 nativeColor) {
	if (rDst.right <= rDst.left || rDst.bottom <= rDst.top)
		return true;

	DDBLTFX fx = {sizeof(DDBLTFX)};
	fx.dwFillColor = nativeColor;

	RECT rDst2 = rDst;
	const vdrect32& rMonitor = mpddman->GetMonitorRect();

	OffsetRect(&rDst2, -rMonitor.left, -rMonitor.top);

	HRESULT hr = pDest->Blt(&rDst2, NULL, NULL, DDBLT_ASYNC | DDBLT_WAIT | DDBLT_COLORFILL, &fx);
	if (SUCCEEDED(hr))
		return true;

	if (hr != DDERR_SURFACELOST)
		return false;

	if (FAILED(pDest->IsLost())) {
		pDest->SetClipper(NULL);
		pDest = NULL;

		if (!mpddman->Restore())
			return false;

		if (mbReset)
			return false;

		pDest = mpddman->GetPrimary();
		pDest->SetClipper(mpddc);
	}

	return true;
}

uint32 VDVideoDisplayMinidriverDirectDraw::ConvertToNativeColor(uint32 rgb32) const {
	switch(mPrimaryFormat) {
		case nsVDPixmap::kPixFormat_Pal8:
			{
				const int red = (rgb32 >> 16) & 0xff;
				const int grn = (rgb32 >>  8) & 0xff;
				const int blu = (rgb32 >>  0) & 0xff;
				const int ridx = (red * 5 + 128) >> 8;
				const int gidx = (grn * 5 + 128) >> 8;
				const int bidx = (blu * 5 + 128) >> 8;
				uint8 idx = ridx * 36 + gidx * 6 + bidx;

				if (mpLogicalPalette)
					idx = mpLogicalPalette[idx];

				return idx;
			}
			break;

		case nsVDPixmap::kPixFormat_XRGB1555:
			return ((rgb32 & 0xf80000) >> 9)
				 + ((rgb32 & 0x00f800) >> 6)
				 + ((rgb32 & 0x0000f8) >> 3);
			break;

		case nsVDPixmap::kPixFormat_RGB565:
			return ((rgb32 & 0xf80000) >> 8)
				 + ((rgb32 & 0x00fc00) >> 5)
				 + ((rgb32 & 0x0000f8) >> 3);

		case nsVDPixmap::kPixFormat_RGB888:
		case nsVDPixmap::kPixFormat_XRGB8888:
			return rgb32;

		default:
			VDASSERT(!"Unsupported pixel type.");
			return 0;
	}
}
