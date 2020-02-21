//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2005 Avery Lee
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
//
//
//	This file is the DirectX 9 driver for the video display subsystem.
//	It does traditional point sampled and bilinearly filtered upsampling
//	as well as a special multipass algorithm for emulated bicubic
//	filtering.
//

#include <vd2/system/vdtypes.h>

#define DIRECTDRAW_VERSION 0x0900
#define INITGUID
#include <d3d9.h>
#include <dxgi.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/refcount.h>
#include <vd2/system/math.h>
#include <vd2/system/time.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/w32assist.h>
#include <vd2/VDDisplay/direct3d.h>
#include <vd2/VDDisplay/logging.h>

///////////////////////////////////////////////////////////////////////////

#define VDDEBUG_D3D(...) VDDispLogF(__VA_ARGS__)
#define VDDEBUG_D3D_MODESWITCH(...) VDDispLogF(__VA_ARGS__)

using namespace nsVDD3D9;

///////////////////////////////////////////////////////////////////////////

class VDD3D9InitTexture : public vdrefcounted<IVDD3D9InitTexture> {
public:
	VDD3D9InitTexture();
	~VDD3D9InitTexture();

	bool Init(VDD3D9Manager *pManager, UINT width, UINT height, UINT levels, D3DFORMAT format);

	bool Lock(int level, VDD3D9LockInfo& lockInfo);
	void Unlock(int level);

protected:
	friend class VDD3D9Texture;

	vdrefptr<IDirect3DTexture9> mpTexture;
	UINT mWidth;
	UINT mHeight;
	UINT mLevels;
	D3DFORMAT mFormat;
};

VDD3D9InitTexture::VDD3D9InitTexture() {
}

VDD3D9InitTexture::~VDD3D9InitTexture() {
}

bool VDD3D9InitTexture::Init(VDD3D9Manager *pManager, UINT width, UINT height, UINT levels, D3DFORMAT format) {
	D3DPOOL pool = D3DPOOL_MANAGED;

	if (pManager->GetDeviceEx())
		pool = D3DPOOL_SYSTEMMEM;

	HRESULT hr = pManager->GetDevice()->CreateTexture(width, height, levels, 0, format, pool, ~mpTexture, NULL);
	if (FAILED(hr))
		return false;

	mWidth = width;
	mHeight = height;
	mLevels = levels;
	mFormat = format;
	return true;
}

bool VDD3D9InitTexture::Lock(int level, VDD3D9LockInfo& lockInfo) {
	D3DLOCKED_RECT lr;
	HRESULT hr = mpTexture->LockRect(level, &lr, NULL, 0);
	if (FAILED(hr))
		return false;

	lockInfo.mpData = lr.pBits;
	lockInfo.mPitch = lr.Pitch;
	return true;
}

void VDD3D9InitTexture::Unlock(int level) {
	mpTexture->UnlockRect(level);
}

///////////////////////////////////////////////////////////////////////////

class VDD3D9Texture final : public IVDD3D9Texture, public vdlist_node {
public:
	VDD3D9Texture();
	~VDD3D9Texture();

	int AddRef();
	int Release();

	bool Init(VDD3D9Manager *pManager, IVDD3D9TextureGenerator *pGenerator);
	void Shutdown();

	bool InitVRAMResources();
	void ShutdownVRAMResources();

	const char *GetName() const { return mName.data(); }
	void SetName(const char *name) { mName.assign(name, name+strlen(name)+1); }

	int GetWidth();
	int GetHeight();

	bool Init(IVDD3D9InitTexture *pInitTexture);

	void SetD3DTexture(IDirect3DTexture9 *pTexture);
	IDirect3DTexture9 *GetD3DTexture();

protected:
	vdfastvector<char> mName;
	IDirect3DTexture9 *mpD3DTexture;
	IVDD3D9TextureGenerator *mpGenerator;
	VDD3D9Manager *mpManager;
	int mWidth;
	int mHeight;
	int mRefCount;
	bool mbVRAMTexture;
};

VDD3D9Texture::VDD3D9Texture()
	: mpD3DTexture(NULL)
	, mpGenerator(NULL)
	, mpManager(NULL)
	, mWidth(0)
	, mHeight(0)
	, mRefCount(0)
	, mbVRAMTexture(false)
{
	mListNodeNext = mListNodePrev = this;
}

VDD3D9Texture::~VDD3D9Texture() {
}

int VDD3D9Texture::AddRef() {
	return ++mRefCount;
}

int VDD3D9Texture::Release() {
	int rc = --mRefCount;

	if (!rc) {
		vdlist_base::unlink(*this);
		Shutdown();
		delete this;
	}

	return rc;
}

bool VDD3D9Texture::Init(VDD3D9Manager *pManager, IVDD3D9TextureGenerator *pGenerator) {
	if (mpGenerator)
		mpGenerator->Release();
	if (pGenerator)
		pGenerator->AddRef();
	mpGenerator = pGenerator;
	mpManager = pManager;

	if (mpGenerator) {
		if (!mpGenerator->GenerateTexture(pManager, this))
			return false;
	}

	return true;
}

void VDD3D9Texture::Shutdown() {
	mpManager = NULL;

	if (mpGenerator) {
		mpGenerator->Release();
		mpGenerator = NULL;
	}

	if (mpD3DTexture) {
		mpD3DTexture->Release();
		mpD3DTexture = NULL;
	}
}

bool VDD3D9Texture::InitVRAMResources() {
	if (mbVRAMTexture && !mpD3DTexture && mpGenerator)
		return mpGenerator->GenerateTexture(mpManager, this);

	return true;
}

void VDD3D9Texture::ShutdownVRAMResources() {
	if (mbVRAMTexture) {
		if (mpD3DTexture) {
			mpD3DTexture->Release();
			mpD3DTexture = NULL;
		}
	}
}

int VDD3D9Texture::GetWidth() {
	return mWidth;
}

int VDD3D9Texture::GetHeight() {
	return mHeight;
}

bool VDD3D9Texture::Init(IVDD3D9InitTexture *pInitTexture) {
	VDD3D9InitTexture *p = static_cast<VDD3D9InitTexture *>(pInitTexture);

	if (!p->mpTexture)
		return false;

	if (!mpManager->GetDeviceEx()) {
		mpD3DTexture = p->mpTexture;
		mpD3DTexture->AddRef();
	} else {
		IDirect3DDevice9Ex *pDevEx = mpManager->GetDeviceEx();
		HRESULT hr = pDevEx->CreateTexture(p->mWidth, p->mHeight, p->mLevels, 0, p->mFormat, D3DPOOL_DEFAULT, &mpD3DTexture, NULL);
		if (FAILED(hr))
			return false;

		hr = pDevEx->UpdateTexture(p->mpTexture, mpD3DTexture);
		if (FAILED(hr))
			return false;
	}

	return true;
}

void VDD3D9Texture::SetD3DTexture(IDirect3DTexture9 *pTexture) {
	if (mpD3DTexture)
		mpD3DTexture->Release();
	if (pTexture) {
		pTexture->AddRef();

		D3DSURFACE_DESC desc;
		HRESULT hr = pTexture->GetLevelDesc(0, &desc);

		if (SUCCEEDED(hr)) {
			mWidth = desc.Width;
			mHeight = desc.Height;
			mbVRAMTexture = (desc.Pool == D3DPOOL_DEFAULT);
		} else {
			mWidth = 1;
			mHeight = 1;
			mbVRAMTexture = true;
		}

	}
	mpD3DTexture = pTexture;
}

IDirect3DTexture9 *VDD3D9Texture::GetD3DTexture() {
	return mpD3DTexture;
}

///////////////////////////////////////////////////////////////////////////

class VDD3D9SwapChain final : public vdrefcounted<IVDD3D9SwapChain>, public VDThread {
public:
	VDD3D9SwapChain(VDD3D9Manager *parent, IDirect3DSwapChain9 *pD3DSwapChain);
	~VDD3D9SwapChain();

	IDirect3DSwapChain9 *GetD3DSwapChain() const override { return mpD3DSwapChain; }

	bool SetVsyncCallback(HMONITOR hmon, const vdfunction<void()>& fn, uint32 postVsyncDelayMS) override;
	void RequestVsyncCallback() override;

private:
	void ThreadRun();

	bool GetOutput(IDXGIFactory1 *factory, HMONITOR hmon, IDXGIOutput **ppOutput);

	VDD3D9Manager *const mpParent;
	vdrefptr<IDirect3DSwapChain9> mpD3DSwapChain;

	enum State : int {
		kState_WaitForInit,
		kState_InitFailed,
		kState_Idle,
		kState_VsyncPolling,
		kState_SwitchMonitors,
		kState_ExitRequested
	};

	VDAtomicInt mState = kState_WaitForInit;
	VDAtomicInt mVsyncCounter = 0;

	VDSignal mCommand;
	VDSignal mReply;

	VDCriticalSection mcs;
	vdfunction<void()> mpVsyncCallback;
	HMONITOR mhMonitor = nullptr;
	uint32 mPostVsyncDelayMS = 0;
};

VDD3D9SwapChain::VDD3D9SwapChain(VDD3D9Manager *parent, IDirect3DSwapChain9 *pD3DSwapChain)
	: VDThread("D3D9 Windowed VSync thread")
	, mpParent(parent)
	, mpD3DSwapChain(pD3DSwapChain)
{
}

VDD3D9SwapChain::~VDD3D9SwapChain() {
	SetVsyncCallback(nullptr, {}, 0);
}

bool VDD3D9SwapChain::SetVsyncCallback(HMONITOR hmon, const vdfunction<void()>& fn, uint32 postVsyncDelayMS) {
	if (!mpParent->IsDXGIEnabled() || !mpParent->IsDWMCompositing())
		return false;

	if (hmon) {
		if (!isThreadActive()) {
			mState = kState_WaitForInit;
			if (!ThreadStart())
				return false;

			do {
				mReply.wait();
			} while(mState == kState_WaitForInit);

			if (mState == kState_InitFailed) {
				ThreadWait();
				return false;
			}
		}

		mcs.Lock();
		mhMonitor = hmon;
		mPostVsyncDelayMS = postVsyncDelayMS;
		mpVsyncCallback = fn;
		mcs.Unlock();
	
		mVsyncCounter = 30;
		mState = kState_SwitchMonitors;
		mCommand.signal();

		uint32 state;
		do {
			mReply.wait();
			state = mState;
		} while(state == kState_SwitchMonitors);

		return state == kState_VsyncPolling;
	} else {
		if (isThreadActive()) {
			mState = kState_ExitRequested;
			mCommand.signal();

			ThreadWait();
		}

		mpVsyncCallback = nullptr;
	}

	return false;
}

void VDD3D9SwapChain::RequestVsyncCallback() {
	VDASSERT(isThreadActive() && mState == kState_VsyncPolling);

	for(;;) {
		uint32 count = mVsyncCounter;

		if (count == mVsyncCounter.compareExchange(30, count)) {
			if (count == 0)
				mCommand.signal();

			break;
		}
	}
}

void VDD3D9SwapChain::ThreadRun() {
	vdrefptr<IDXGIFactory1> factory;

	HRESULT hr = mpParent->CreateDXGIFactory1(IID_IDXGIFactory1, (void **)~factory);
	if (FAILED(hr)) {
		mState = kState_InitFailed;
		return;
	}

	vdrefptr<IDXGIOutput> output;
	for(;;) {
		State state = (State)(int)mState;

		if (state == kState_ExitRequested)
			break;

		if (state == kState_SwitchMonitors) {
			mcs.Lock();
			HMONITOR hmon = mhMonitor;
			uint32 delay = mPostVsyncDelayMS;
			auto fn = std::move(mpVsyncCallback);
			mcs.Unlock();

			if (GetOutput(factory, hmon, ~output)) {
				mState = kState_VsyncPolling;
				mReply.signal();

				do {
					uint32 counter = mVsyncCounter;

					if (counter == 0) {
						mCommand.wait();
						continue;
					}

					if (counter != mVsyncCounter.compareExchange(counter - 1, counter))
						continue;

					output->WaitForVBlank();
					::Sleep(delay);

					mcs.Lock();
					fn();
					mcs.Unlock();
				} while(mState == kState_VsyncPolling);

				continue;
			}
		}

		if (state != kState_Idle) {
			mState = kState_Idle;
			mReply.signal();
		}

		mCommand.wait();
	}
}

bool VDD3D9SwapChain::GetOutput(IDXGIFactory1 *factory, HMONITOR hmon, IDXGIOutput **ppOutput) {
	const auto& adaptId = mpParent->GetAdapterIdentifier();
	HRESULT hr;
	
	vdrefptr<IDXGIOutput> output;
	for(uint32 adapterIndex = 0; ; ++adapterIndex) {
		vdrefptr<IDXGIAdapter1> adapter;
		hr = factory->EnumAdapters1(adapterIndex, ~adapter);
		if (FAILED(hr))
			break;

		DXGI_ADAPTER_DESC1 adapterDesc;
		hr = adapter->GetDesc1(&adapterDesc);
		if (FAILED(hr))
			continue;

		if (adapterDesc.DeviceId == adaptId.DeviceId
			&& adapterDesc.VendorId == adaptId.VendorId
			&& adapterDesc.SubSysId == adaptId.SubSysId
			&& adapterDesc.Revision == adaptId.Revision)
		{
			for(uint32 outputIndex = 0; ; ++outputIndex) {
				vdrefptr<IDXGIOutput> testOutput;
				hr = adapter->EnumOutputs(outputIndex, ~testOutput);

				if (FAILED(hr))
					break;

				DXGI_OUTPUT_DESC outputDesc;
				hr = testOutput->GetDesc(&outputDesc);
				if (SUCCEEDED(hr)) {
					if (outputDesc.Monitor == hmon) {
						output = std::move(testOutput);
						break;
					}
				}
			}

			if (output)
				break;
		}
	}

	if (!output)
		return false;

	*ppOutput = output.release();
	return true;
}

///////////////////////////////////////////////////////////////////////////

static VDCriticalSection g_csVDDirect3D9Managers;
static vdlist<VDD3D9Manager> g_VDDirect3D9Managers;

VDD3D9Manager *VDInitDirect3D9(VDD3D9Client *pClient, HMONITOR hmonitor, bool use9ex) {
	VDD3D9Manager *pMgr = NULL;
	bool firstClient = false;

	vdsynchronized(g_csVDDirect3D9Managers) {
		vdlist<VDD3D9Manager>::iterator it(g_VDDirect3D9Managers.begin()), itEnd(g_VDDirect3D9Managers.end());

		VDThreadID tid = VDGetCurrentThreadID();

		for(; it != itEnd; ++it) {
			VDD3D9Manager *mgr = *it;

			if (mgr->GetThreadID() == tid && mgr->GetMonitor() == hmonitor && mgr->IsD3D9ExEnabled() == use9ex) {
				pMgr = mgr;
				break;
			}
		}

		if (!pMgr) {
			pMgr = new_nothrow VDD3D9Manager(hmonitor, use9ex);
			if (!pMgr)
				return NULL;

			g_VDDirect3D9Managers.push_back(pMgr);
			firstClient = true;
		}
	}

	bool success = pMgr->Attach(pClient);

	if (success)
		return pMgr;

	if (firstClient) {
		// We need to synchronize here because the list is shared. However, we don't need to
		// synchronize on the manager itself because it's thread specific -- no one else can
		// be trying to attach to the same instance as we are occupying the only thread that
		// can access it.
		vdsynchronized(g_csVDDirect3D9Managers) {
			g_VDDirect3D9Managers.erase(pMgr);
		}

		delete pMgr;
	}

	return NULL;
}

void VDDeinitDirect3D9(VDD3D9Manager *p, VDD3D9Client *pClient) {
	if (p->Detach(pClient)) {
		vdsynchronized(g_csVDDirect3D9Managers) {
			g_VDDirect3D9Managers.erase(p);
		}
		delete p;
	}
}

VDD3D9Manager::VDD3D9Manager(HMONITOR hmonitor, bool use9ex)
	: mhmodDX9(NULL)
	, mpD3D(NULL)
	, mpD3DEx(NULL)
	, mpD3DDevice(NULL)
	, mpD3DDeviceEx(NULL)
	, mpD3DRTMain(NULL)
	, mhMonitor(hmonitor)
	, mDevWndClass(NULL)
	, mhwndDevice(NULL) 
	, mThreadID(0)
	, mbUseD3D9Ex(use9ex)
	, mbDeviceValid(false)
	, mbInScene(false)
	, mbSupportsEventQueries(false)
	, mbDwmCompositing(false)
	, mFullScreenCount(0)
	, mFSFence(0)
	, mpD3DVB(NULL)
	, mpD3DIB(NULL)
	, mpD3DQuery(NULL)
	, mpD3DVD(NULL)
	, mpImplicitSwapChain(NULL)
	, mRefCount(0)
	, mFenceQueueBase(0)
	, mFenceQueueHeadIndex(0)
{
}

VDD3D9Manager::~VDD3D9Manager() {
	VDASSERT(!mRefCount);
}

bool VDD3D9Manager::Attach(VDD3D9Client *pClient) {
	bool bSuccess = false;

	VDASSERT(mClients.find(pClient) == mClients.end());

	if (++mRefCount == 1)
		bSuccess = Init();
	else {
		VDASSERT(VDGetCurrentThreadID() == mThreadID);
		bSuccess = CheckDevice();
	}

	if (!bSuccess) {
		if (!--mRefCount)
			Shutdown();

		return false;
	}

	mClients.push_back(pClient);

	return bSuccess;
}

bool VDD3D9Manager::Detach(VDD3D9Client *pClient) {
	VDASSERT(mRefCount > 0);
	VDASSERT(VDGetCurrentThreadID() == mThreadID);

	VDASSERT(mClients.find(pClient) != mClients.end());
	mClients.erase(mClients.fast_find(pClient));

	if (!--mRefCount) {
		Shutdown();
		return true;
	}

	return false;
}

bool VDD3D9Manager::Init() {
	HINSTANCE hInst = VDGetLocalModuleHandleW32();

	if (!mDevWndClass) {
		WNDCLASS wc;

		wc.cbClsExtra		= 0;
		wc.cbWndExtra		= sizeof(VDD3D9Manager *);
		wc.hbrBackground	= NULL;
		wc.hCursor			= NULL;
		wc.hIcon			= NULL;
		wc.hInstance		= hInst;
		wc.lpfnWndProc		= StaticDeviceWndProc;

		wchar_t buf[64];
		swprintf(buf, vdcountof(buf), L"RizaD3DDeviceWindow_%p", this);
		wc.lpszClassName	= buf;
		wc.lpszMenuName		= NULL;
		wc.style			= 0;

		mDevWndClass = RegisterClass(&wc);
		if (!mDevWndClass)
			return false;
	}

	mThreadID = VDGetCurrentThreadID();

	mhwndDevice = CreateWindow(MAKEINTATOM(mDevWndClass), L"", WS_POPUP, 0, 0, 0, 0, NULL, NULL, hInst, this);
	if (!mhwndDevice) {
		Shutdown();
		return false;
	}

	// attempt to load D3D9.DLL
	mhmodDX9 = VDLoadSystemLibraryW32("d3d9.dll");
	if (!mhmodDX9) {
		Shutdown();
		return false;
	}

	mpBeginEvent = NULL;
	mpEndEvent = NULL;

	DWORD (__stdcall *pD3DPERF_GetStatus)() = (DWORD (__stdcall *)())GetProcAddress(mhmodDX9, "D3DPERF_GetStatus");

	if (pD3DPERF_GetStatus && pD3DPERF_GetStatus()) {
		mpBeginEvent = (tpBeginEvent)GetProcAddress(mhmodDX9, "D3DPERF_BeginEvent");
		mpEndEvent = (tpEndEvent)GetProcAddress(mhmodDX9, "D3DPERF_EndEvent");

		if (!mpBeginEvent || !mpEndEvent) {
			mpBeginEvent = NULL;
			mpEndEvent = NULL;
		}
	}

	if (mbUseD3D9Ex) {
		// FLIPEX support requires at least Windows 7.
		if (VDIsAtLeast7W32()) {
			HRESULT (APIENTRY *pDirect3DCreate9Ex)(UINT, IDirect3D9Ex **) = (HRESULT (APIENTRY *)(UINT, IDirect3D9Ex **))GetProcAddress(mhmodDX9, "Direct3DCreate9Ex");
			if (pDirect3DCreate9Ex) {
				HRESULT hr = pDirect3DCreate9Ex(D3D_SDK_VERSION, &mpD3DEx);

				if (SUCCEEDED(hr))
					mpD3D = mpD3DEx;
			}
		}
	}

	if (!mpD3D) {
		IDirect3D9 *(APIENTRY *pDirect3DCreate9)(UINT) = (IDirect3D9 *(APIENTRY *)(UINT))GetProcAddress(mhmodDX9, "Direct3DCreate9");
		if (!pDirect3DCreate9) {
			Shutdown();
			return false;
		}

		// create Direct3D9 object
		mpD3D = pDirect3DCreate9(D3D_SDK_VERSION);
		if (!mpD3D) {
			Shutdown();
			return false;
		}
	}

	// create device
	memset(&mPresentParms, 0, sizeof mPresentParms);
	mPresentParms.Windowed			= TRUE;
	mPresentParms.SwapEffect		= D3DSWAPEFFECT_COPY;
	// BackBufferFormat is set below.
	mPresentParms.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;

	HRESULT hr;

	// Look for the NVPerfHUD 2.0 driver
	
	const UINT adapters = mpD3D->GetAdapterCount();
	DWORD dwFlags = D3DCREATE_FPU_PRESERVE | D3DCREATE_NOWINDOWCHANGES;
	UINT adapter = D3DADAPTER_DEFAULT;
	D3DDEVTYPE type = D3DDEVTYPE_HAL;
	bool perfHudActive = false;

	for(UINT n=0; n<adapters; ++n) {
		D3DADAPTER_IDENTIFIER9 ident;

		if (SUCCEEDED(mpD3D->GetAdapterIdentifier(n, 0, &ident))) {
			if (strstr(ident.Description, "PerfHUD")) {
				adapter = n;
				type = D3DDEVTYPE_REF;
				perfHudActive = true;
				break;
			}
		}
	}

	if (adapter == D3DADAPTER_DEFAULT && mhMonitor) {
		for(UINT n=0; n<adapters; ++n) {
			HMONITOR hmon = mpD3D->GetAdapterMonitor(n);

			if (hmon == mhMonitor) {
				adapter = n;
				break;
			}
		}
	}

	mAdapter = adapter;
	mDevType = type;
	mhMonitorActual = mhMonitor ? mhMonitor : mpD3D->GetAdapterMonitor(adapter);
	
	hr = mpD3D->GetAdapterIdentifier(adapter, 0, &mAdapterIdentifier);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Failed to retrieve adapter identifier (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		Shutdown();
		return false;
	}

	D3DDISPLAYMODE mode;
	hr = mpD3D->GetAdapterDisplayMode(adapter, &mode);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Failed to get current adapter mode (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		Shutdown();
		return false;
	}

	if (perfHudActive) {
		mPresentParms.BackBufferWidth	= mode.Width;
		mPresentParms.BackBufferHeight	= mode.Height;
	} else {
		mPresentParms.BackBufferWidth	= 32;
		mPresentParms.BackBufferHeight	= 32;
	}

	if (mpD3DDeviceEx)
		mPresentParms.Flags |= D3DPRESENTFLAG_UNPRUNEDMODE;

	// Make sure we have at least X8R8G8B8 for a texture format
	hr = mpD3D->CheckDeviceFormat(adapter, type, D3DFMT_X8R8G8B8, 0, D3DRTYPE_TEXTURE, D3DFMT_X8R8G8B8);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Device does not support X8R8G8B8 textures (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		Shutdown();
		return false;
	}

	// Make sure we have at least X8R8G8B8 or A8R8G8B8 for a backbuffer format
	mPresentParms.BackBufferFormat	= D3DFMT_A8R8G8B8;
	hr = mpD3D->CheckDeviceFormat(adapter, type, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, D3DFMT_A8R8G8B8);
	if (FAILED(hr)) {
		mPresentParms.BackBufferFormat	= D3DFMT_A8R8G8B8;
		hr = mpD3D->CheckDeviceFormat(adapter, type, D3DFMT_X8R8G8B8, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, D3DFMT_X8R8G8B8);

		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Device does not support X8R8G8B8 or A8R8G8B8 render targets.");
			Shutdown();
			return false;
		}
	}

	// Check if at least vertex shader 1.1 is supported; if not, force SWVP.
	hr = mpD3D->GetDeviceCaps(adapter, type, &mDevCaps);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Couldn't retrieve device caps.");
		Shutdown();
		return false;
	}

	if (mDevCaps.VertexShaderVersion >= D3DVS_VERSION(1, 1))
		dwFlags |= D3DCREATE_HARDWARE_VERTEXPROCESSING;
	else
		dwFlags |= D3DCREATE_SOFTWARE_VERTEXPROCESSING;

	// Create the device.
	if (mpD3DEx)
		hr = mpD3DEx->CreateDeviceEx(adapter, type, mhwndDevice, dwFlags, &mPresentParms, NULL, &mpD3DDeviceEx);
	else
		hr = mpD3D->CreateDevice(adapter, type, mhwndDevice, dwFlags, &mPresentParms, &mpD3DDevice);

	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Failed to create device (hr=%08X %s)", hr, VDDispDecodeD3D9Error(hr));
		Shutdown();
		return false;
	}

	if (mpD3DDeviceEx)
		mpD3DDevice = mpD3DDeviceEx;

	mbDeviceValid = true;

	// retrieve device caps
	memset(&mDevCaps, 0, sizeof mDevCaps);
	hr = mpD3DDevice->GetDeviceCaps(&mDevCaps);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Failed to retrieve device caps (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		Shutdown();
		return false;
	}

	VDDEBUG_D3D("VideoDisplay/DX9: Successfully created Direct3D 9%s device.", mpD3DEx ? "Ex" : "");
	VDDEBUG_D3D("Device: %s (%s)", mAdapterIdentifier.Driver, mAdapterIdentifier.Description);
	VDDEBUG_D3D("DeviceCaps: VS%u.%u, PS%u.%u, MaxTex %ux%u, ReadScanline %s, %s"
		, (mDevCaps.VertexShaderVersion >> 8) & 0xFF
		, mDevCaps.VertexShaderVersion & 0xFF
		, (mDevCaps.PixelShaderVersion >> 8) & 0xFF
		, mDevCaps.PixelShaderVersion & 0xFF
		, mDevCaps.MaxTextureWidth
		, mDevCaps.MaxTextureHeight
		, mDevCaps.Caps & D3DCAPS_READ_SCANLINE ? "Yes" : "No"
		, mDevCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT ? "HWVP" : "SWVP"
	);

	// Check for Virge/Rage Pro/Riva128
	if (Is3DCardLame()) {
		Shutdown();
		return false;
	}

	const D3DVERTEXELEMENT9 kVertexDecl[]={
		{ 0, 0, D3DDECLTYPE_FLOAT3, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0 },
		{ 0, 12, D3DDECLTYPE_D3DCOLOR, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_COLOR, 0 },
		{ 0, 16, D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 0 },
		{ 0, 24, D3DDECLTYPE_FLOAT2, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 1 },
		D3DDECL_END()
	};

	hr = mpD3DDevice->CreateVertexDeclaration(kVertexDecl, &mpD3DVD);
	if (FAILED(hr)) {
		Shutdown();
		return false;
	}

	if (!UpdateCachedDisplayMode()) {
		Shutdown();
		return false;
	}

	// If we're on Vista+, see if we can load DXGI 1.1.
	if (VDIsAtLeastVistaW32()) {
		mhmodDXGI = VDLoadSystemLibraryW32("dxgi.dll");

		if (mhmodDXGI) {
			mpCreateDXGIFactory1 = (decltype(mpCreateDXGIFactory1))GetProcAddress(mhmodDXGI, "CreateDXGIFactory1");

			if (!mpCreateDXGIFactory1) {
				FreeLibrary(mhmodDXGI);
				mhmodDXGI = nullptr;
			}
		}
	}

	if (!InitVRAMResources()) {
		Shutdown();
		return false;
	}

	VDDEBUG_D3D("VideoDisplay/DX9: Init successful on adapter %u (%s / %s), monitor %p.", adapter, mAdapterIdentifier.DeviceName, mAdapterIdentifier.Driver, mhMonitor);
	return true;
}

bool VDD3D9Manager::InitVRAMResources() {
	HRESULT hr;

	// retrieve back buffer
	if (!mpD3DRTMain) {
		hr = mpD3DDevice->GetBackBuffer(0, 0, D3DBACKBUFFER_TYPE_MONO, &mpD3DRTMain);
		if (FAILED(hr)) {
			ShutdownVRAMResources();
			return false;
		}
	}

	// create vertex buffer
	if (!mpD3DVB) {
		hr = mpD3DDevice->CreateVertexBuffer(kVertexBufferSize * sizeof(Vertex), D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY, D3DFVF_XYZ | D3DFVF_DIFFUSE | D3DFVF_TEX2, D3DPOOL_DEFAULT, &mpD3DVB, NULL);
		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Failed to create vertex buffer (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			ShutdownVRAMResources();
			return false;
		}
		mVertexBufferPt = 0;
	}

	// create index buffer
	if (!mpD3DIB) {
		hr = mpD3DDevice->CreateIndexBuffer(kIndexBufferSize * sizeof(uint16), D3DUSAGE_DYNAMIC | D3DUSAGE_WRITEONLY, D3DFMT_INDEX16, D3DPOOL_DEFAULT, &mpD3DIB, NULL);
		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Failed to create index buffer (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			ShutdownVRAMResources();
			return false;
		}
		mIndexBufferPt = 0;
	}

	// create flush event
	if (!mpD3DQuery) {
		mbSupportsEventQueries = false;
		hr = mpD3DDevice->CreateQuery(D3DQUERYTYPE_EVENT, NULL);
		if (SUCCEEDED(hr)) {
			mbSupportsEventQueries = true;
			hr = mpD3DDevice->CreateQuery(D3DQUERYTYPE_EVENT, &mpD3DQuery);
		}
	}

	// get implicit swap chain
	if (!mpImplicitSwapChain) {
		vdrefptr<IDirect3DSwapChain9> pD3DSwapChain;
		hr = mpD3DDevice->GetSwapChain(0, ~pD3DSwapChain);
		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Failed to obtain implicit swap chain (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			ShutdownVRAMResources();
			return false;
		}

		mpImplicitSwapChain = new_nothrow VDD3D9SwapChain(this, pD3DSwapChain);
		if (!mpImplicitSwapChain) {
			VDDEBUG_D3D("VideoDisplay/DX9: Failed to obtain implicit swap chain (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			ShutdownVRAMResources();
			return false;
		}
		mpImplicitSwapChain->AddRef();
	}

	SharedTextures::iterator it(mSharedTextures.begin()), itEnd(mSharedTextures.end());
	for(; it!=itEnd; ++it) {
		VDD3D9Texture& texture = **it;

		texture.InitVRAMResources();
	}

	return true;
}

void VDD3D9Manager::ShutdownVRAMResources() {
	SharedTextures::iterator it(mSharedTextures.begin()), itEnd(mSharedTextures.end());
	for(; it!=itEnd; ++it) {
		VDD3D9Texture& texture = **it;

		texture.ShutdownVRAMResources();
	}

	if (mpImplicitSwapChain) {
		mpImplicitSwapChain->Release();
		mpImplicitSwapChain = NULL;
	}

	mFenceQueueBase += mFenceQueue.size();
	mFenceQueueHeadIndex = 0;

	while(!mFenceQueue.empty()) {
		IDirect3DQuery9 *pQuery = mFenceQueue.back();
		mFenceQueue.pop_back();

		if (pQuery)
			pQuery->Release();
	}

	while(!mFenceFreeList.empty()) {
		IDirect3DQuery9 *pQuery = mFenceFreeList.back();
		mFenceFreeList.pop_back();

		pQuery->Release();
	}

	if (mpD3DQuery) {
		mpD3DQuery->Release();
		mpD3DQuery = NULL;
	}

	if (mpD3DRTMain) {
		mpD3DRTMain->Release();
		mpD3DRTMain = NULL;
	}

	if (mpD3DIB) {
		mpD3DIB->Release();
		mpD3DIB = NULL;
	}

	if (mpD3DVB) {
		mpD3DVB->Release();
		mpD3DVB = NULL;
	}
}

void VDD3D9Manager::Shutdown() {
	mbDeviceValid = false;

	ShutdownVRAMResources();

	// If we're on Vista+, see if we can load DXGI 1.1.
	if (mhmodDXGI) {
		mpCreateDXGIFactory1 = nullptr;

		FreeLibrary(mhmodDXGI);
		mhmodDXGI = nullptr;
	}

	for(vdlist<VDD3D9Client>::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
		VDD3D9Client& client = **it;

		client.OnPreDeviceReset();
	}

	while(!mSharedTextures.empty()) {
		VDD3D9Texture *pTexture = mSharedTextures.back();
		mSharedTextures.pop_back();

		pTexture->mListNodePrev = pTexture->mListNodeNext = pTexture;
		pTexture->Release();
	}

	if (mpD3DVD) {
		mpD3DVD->Release();
		mpD3DVD = NULL;
	}

	if (mpD3DDevice) {
		mpD3DDevice->Release();
		mpD3DDevice = NULL;
	}

	mpD3DDeviceEx = NULL;

	if (mpD3D) {
		mpD3D->Release();
		mpD3D = NULL;
	}

	mpD3DEx = NULL;

	if (mhmodDX9) {
		FreeLibrary(mhmodDX9);
		mhmodDX9 = NULL;
	}

	if (mhwndDevice) {
		DestroyWindow(mhwndDevice);
		mhwndDevice = NULL;
	}

	if (mDevWndClass) {
		UnregisterClass(MAKEINTATOM(mDevWndClass), VDGetLocalModuleHandleW32());
		mDevWndClass = NULL;
	}
}

bool VDD3D9Manager::UpdateCachedDisplayMode() {
	// We can get called from WM_DISPLAYCHANGE during Direct3DCreate9() under VirtualBox, presumably
	// due to some weirdness in the VBox display driver.
	if (!mpD3D)
		return false;

	// retrieve display mode
	HRESULT hr = mpD3D->GetAdapterDisplayMode(mAdapter, &mDisplayMode);
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Failed to get current adapter mode (hr=%08X %s)", hr, VDDispDecodeD3D9Error(hr));
		return false;
	}

	mbDwmCompositing = false;

	if (VDIsAtLeastVistaW32()) {
		HMODULE hmodDwmApi = VDLoadSystemLibraryW32("dwmapi");
		if (hmodDwmApi) {
			typedef HRESULT (WINAPI *tpDwmIsCompositionEnabled)(BOOL *);

			tpDwmIsCompositionEnabled pDwmIsCompositionEnabled = (tpDwmIsCompositionEnabled)GetProcAddress(hmodDwmApi, "DwmIsCompositionEnabled");
			if (pDwmIsCompositionEnabled) {
				BOOL enabled;
				HRESULT hr = pDwmIsCompositionEnabled(&enabled);

				if (SUCCEEDED(hr) && enabled)
					mbDwmCompositing = true;
			}

			FreeLibrary(hmodDwmApi);
		}
	}


	return true;
}

void VDD3D9Manager::AdjustFullScreen(bool fs, uint32 w, uint32 h, uint32 refresh, bool use16bit, HWND hwnd) {
	if (fs) {
		++mFullScreenCount;
	} else {
		--mFullScreenCount;
		VDASSERT(mFullScreenCount >= 0);
	}

	bool newState = mFullScreenCount != 0;

	if ((!mPresentParms.Windowed) == newState)
		return;

	mPresentParms.Windowed = !newState;
	mPresentParms.SwapEffect = newState ? D3DSWAPEFFECT_DISCARD : D3DSWAPEFFECT_COPY;
	mPresentParms.BackBufferCount = newState ? 3 : 1;
	mPresentParms.PresentationInterval = newState ? D3DPRESENT_INTERVAL_ONE : D3DPRESENT_INTERVAL_IMMEDIATE;

	// Windows 10 build 1709 introduces an undocumented breaking change via its "fullscreen optimizations"
	// where the device window is used to retrieve the display region in exclusive fullscreen mode. This
	// means that we cannot use the hidden focus window. Since the device window cannot be a child window,
	// we must retrieve the top-level window instead. Note that the top-level window must be unobscured for
	// this to work; we clear WS_CLIPCHILDREN so this is the case.
	mPresentParms.hDeviceWindow = newState ? GetAncestor(hwnd, GA_ROOT) : nullptr;

	D3DDISPLAYMODE dm = {0};
	HRESULT hrx = mpD3DDevice->GetDisplayMode(0, &dm);
	VDDEBUG_D3D_MODESWITCH("Current display mode: %ux%u (hr=%08x %s)", dm.Width, dm.Height, hrx, VDDispDecodeD3D9Error(hrx));
	if (newState) {
		const int targetwidth = w && h ? w : dm.Width;
		const int targetheight = w && h ? h : dm.Height;
		const int targetrefresh = w && h && refresh ? refresh : dm.RefreshRate;

		D3DFORMAT format = D3DFMT_X8R8G8B8;

		if (use16bit) {
			if (mpD3D->GetAdapterModeCount(mAdapter, D3DFMT_R5G6B5))
				format = D3DFMT_R5G6B5;
			else if (mpD3D->GetAdapterModeCount(mAdapter, D3DFMT_X1R5G5B5))
				format = D3DFMT_X1R5G5B5;
		}

		// First, we try resetting with the desired display mode. This is necessary to access
		// modes not reported by the monitor (i.e. 50Hz) as there is no way to enumerate
		// unpruned modes in Direct3D 9 or 9Ex. If that fails then we try monitor mode matching.
		mPresentParms.BackBufferWidth = targetwidth;
		mPresentParms.BackBufferHeight = targetheight;
		mPresentParms.FullScreen_RefreshRateInHz = targetrefresh;
		mPresentParms.BackBufferFormat = format;

		if (Reset())
			return;

		UINT count = mpD3D->GetAdapterModeCount(mAdapter, format);
		VDDEBUG_D3D_MODESWITCH("Unable to switch to requested mode. Trying to match against %u display modes.", count);

		D3DDISPLAYMODE dm2;
		D3DDISPLAYMODE dmbest={0};
		int bestindex = -1;
		int bestwidtherr = 0;
		int bestheighterr = 0;
		int bestrefresherr = 0;
		
		for(UINT mode=0; mode<count; ++mode) {
			HRESULT hr = mpD3D->EnumAdapterModes(mAdapter, format, mode, &dm2);
			if (FAILED(hr))
				break;

			const int widtherr = abs((int)dm2.Width - targetwidth);
			const int heighterr = abs((int)dm2.Height - targetheight);
			const int refresherr = abs((int)dm2.RefreshRate - targetrefresh);

			if (dmbest.Width != 0) {
				if (refresherr > bestrefresherr)
					continue;

				if (refresherr == bestrefresherr) {
					if (widtherr + heighterr >= bestwidtherr + bestheighterr)
						continue;
				}
			}

			dmbest = dm2;
			bestindex = (int)mode;
			bestwidtherr = widtherr;
			bestheighterr = heighterr;
			bestrefresherr = refresherr;
		}

		if (bestindex < 0) {
			mPresentParms.BackBufferWidth = dm.Width;
			mPresentParms.BackBufferHeight = dm.Height;
			mPresentParms.FullScreen_RefreshRateInHz = dm.RefreshRate;
			mPresentParms.BackBufferFormat = format;
		} else {
			mPresentParms.BackBufferWidth = dmbest.Width;
			mPresentParms.BackBufferHeight = dmbest.Height;
			mPresentParms.FullScreen_RefreshRateInHz = dmbest.RefreshRate;
			mPresentParms.BackBufferFormat = format;
		}
	} else {
		mPresentParms.BackBufferWidth = dm.Width;
		mPresentParms.BackBufferHeight = dm.Height;
		mPresentParms.BackBufferFormat = D3DFMT_UNKNOWN;
		mPresentParms.FullScreen_RefreshRateInHz = 0;
	}
	
	VDDEBUG_D3D_MODESWITCH("Attempting to switch to %ux%u @ %uHz."
		, mPresentParms.BackBufferWidth
		, mPresentParms.BackBufferHeight
		, mPresentParms.FullScreen_RefreshRateInHz
		);

	if (Reset()) {
		VDDEBUG_D3D_MODESWITCH("Switch to %ux%u @ %uHz was successful."
			, mPresentParms.BackBufferWidth
			, mPresentParms.BackBufferHeight
			, mPresentParms.FullScreen_RefreshRateInHz
			);
	} else {
		VDDEBUG_D3D_MODESWITCH("Switch to %ux%u @ %uHz FAILED."
			, mPresentParms.BackBufferWidth
			, mPresentParms.BackBufferHeight
			, mPresentParms.FullScreen_RefreshRateInHz
			);
	}
}

bool VDD3D9Manager::Reset() {
	if (!mPresentParms.Windowed) {
		HRESULT hr = mpD3DDevice->TestCooperativeLevel();
		if (FAILED(hr) && hr != D3DERR_DEVICENOTRESET) {
			VDDEBUG_D3D("Device reset blocked due to TestCooperativeLevel() failure: hr=%08X %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		// Do not reset the device if we do not have focus!
		HWND hwndFore = GetForegroundWindow();
		if (!hwndFore)
			return false;

		DWORD pid;
		DWORD tid = GetWindowThreadProcessId(hwndFore, &pid);

		if (GetCurrentProcessId() != pid)
			return false;
	}

	for(vdlist<VDD3D9Client>::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
		VDD3D9Client& client = **it;

		client.OnPreDeviceReset();
	}

	ShutdownVRAMResources();

	HRESULT hr;
	D3DPRESENT_PARAMETERS tmp(mPresentParms);
	
	if (mpD3DDeviceEx && !mPresentParms.Windowed) {
		D3DDISPLAYMODEEX mode;
		mode.Width = mPresentParms.BackBufferWidth;
		mode.Height = mPresentParms.BackBufferHeight;
		mode.Format = D3DFMT_X8R8G8B8;
		mode.RefreshRate = mPresentParms.FullScreen_RefreshRateInHz;
		mode.Size = sizeof(D3DDISPLAYMODEEX);
		mode.ScanLineOrdering = D3DSCANLINEORDERING_PROGRESSIVE;
		hr = mpD3DDeviceEx->ResetEx(&tmp, &mode);
	} else
		hr = mpD3DDevice->Reset(&tmp);

	if (FAILED(hr)) {
		mbDeviceValid = false;
		VDDEBUG_D3D_MODESWITCH("Device reset FAILED: hr=%08x %s. Requested mode: %ux%u @ %uHz", hr, VDDispDecodeD3D9Error(hr), mPresentParms.BackBufferWidth, mPresentParms.BackBufferHeight, mPresentParms.FullScreen_RefreshRateInHz);
		return false;
	}

	mbInScene = false;

	if (!UpdateCachedDisplayMode()) {
		ShutdownVRAMResources();
		return false;
	}

	if (!InitVRAMResources())
		return false;

	mbDeviceValid = true;

	for(vdlist<VDD3D9Client>::iterator it(mClients.begin()), itEnd(mClients.end()); it!=itEnd; ++it) {
		VDD3D9Client& client = **it;

		client.OnPostDeviceReset();
	}

	mFSFence = 0;

	return true;
}

bool VDD3D9Manager::CheckDevice() {
	if (!mpD3DDevice)
		return false;

	if (!mbDeviceValid) {
		HRESULT hr = mpD3DDevice->TestCooperativeLevel();

		if (FAILED(hr)) {
			if (hr != D3DERR_DEVICENOTRESET)
				return false;

			if (!Reset())
				return false;
		}
	}

	return InitVRAMResources();
}

bool VDD3D9Manager::CheckReturn(HRESULT hr) {
	if (hr == D3DERR_DEVICELOST)
		mbDeviceValid = false;

	return SUCCEEDED(hr);
}

bool VDD3D9Manager::AdjustTextureSize(uint32& texw, uint32& texh, bool nonPow2OK) {
	int texw2 = texw;
	int texh2 = texh;

	if (!AdjustTextureSize(texw2, texh2, nonPow2OK))
		return false;

	texw = texw2;
	texh = texh2;
	return true;
}

bool VDD3D9Manager::AdjustTextureSize(int& texw, int& texh, bool nonPow2OK) {
	int origw = texw;
	int origh = texh;

	// check if we need to force a power of two
	//
	// flag combos:
	//
	//	None					OK
	//	POW2					Constrain to pow2
	//	NONPOW2CONDITIONAL		Invalid (but happens with some virtualization software)
	//	POW2|NONPOW2CONDITIONAL	Constrain unless nonPow2OK is set

	if ((mDevCaps.TextureCaps & (D3DPTEXTURECAPS_POW2|D3DPTEXTURECAPS_NONPOW2CONDITIONAL)) && (!nonPow2OK || !(mDevCaps.TextureCaps & D3DPTEXTURECAPS_NONPOW2CONDITIONAL))) {
		// make power of two
		texw += texw - 1;
		texh += texh - 1;

		while(int tmp = texw & (texw-1))
			texw = tmp;
		while(int tmp = texh & (texh-1))
			texh = tmp;
	}

	// enforce aspect ratio
	if (mDevCaps.MaxTextureAspectRatio) {
		while(texw * (int)mDevCaps.MaxTextureAspectRatio < texh)
			texw += texw;
		while(texh * (int)mDevCaps.MaxTextureAspectRatio < texw)
			texh += texh;
	}

	// enforce size limits
	if ((unsigned)texw > mDevCaps.MaxTextureWidth)
		texw = mDevCaps.MaxTextureWidth;

	if ((unsigned)texh > mDevCaps.MaxTextureHeight)
		texh = mDevCaps.MaxTextureHeight;

	return texw >= origw && texh >= origh;
}

bool VDD3D9Manager::IsTextureFormatAvailable(D3DFORMAT format) {
	HRESULT hr = mpD3D->CheckDeviceFormat(mAdapter, mDevType, mDisplayMode.Format, 0, D3DRTYPE_TEXTURE, format);

	return SUCCEEDED(hr);
}

bool VDD3D9Manager::CheckResourceFormat(DWORD usage, D3DRESOURCETYPE rtype, D3DFORMAT checkFormat) const {
	HRESULT hr = mpD3D->CheckDeviceFormat(mAdapter, mDevType, mDisplayMode.Format, usage, rtype, checkFormat);

	return SUCCEEDED(hr);
}

void VDD3D9Manager::ClearRenderTarget(IDirect3DTexture9 *pTexture) {
	IDirect3DSurface9 *pRTSurface;
	if (FAILED(pTexture->GetSurfaceLevel(0, &pRTSurface)))
		return;
	HRESULT hr = mpD3DDevice->SetRenderTarget(0, pRTSurface);
	pRTSurface->Release();

	if (FAILED(hr))
		return;

	if (SUCCEEDED(mpD3DDevice->BeginScene())) {
		mpD3DDevice->Clear(0, NULL, D3DCLEAR_TARGET, 0, 0.f, 0);
		mpD3DDevice->EndScene();
	}
}

void VDD3D9Manager::ResetBuffers() {
	mVertexBufferPt = 0;
	mIndexBufferPt = 0;
}

Vertex *VDD3D9Manager::LockVertices(unsigned vertices) {
	VDASSERT(vertices <= kVertexBufferSize);
	if (mVertexBufferPt + vertices > kVertexBufferSize) {
		mVertexBufferPt = 0;
	}

	mVertexBufferLockSize = vertices;

	void *p = NULL;
	HRESULT hr;
	for(;;) {
		hr = mpD3DVB->Lock(mVertexBufferPt * sizeof(Vertex), mVertexBufferLockSize * sizeof(Vertex), &p, mVertexBufferPt ? D3DLOCK_NOOVERWRITE : D3DLOCK_DISCARD);
		if (hr != D3DERR_WASSTILLDRAWING)
			break;
		Sleep(1);
	}
	if (FAILED(hr)) {
		VDASSERT(false);
		return NULL;
	}

	return (Vertex *)p;
}

void VDD3D9Manager::UnlockVertices() {
	mVertexBufferPt += mVertexBufferLockSize;

	VDVERIFY(SUCCEEDED(mpD3DVB->Unlock()));
}

bool VDD3D9Manager::UploadVertices(unsigned vertices, const Vertex *data) {
	Vertex *vx = LockVertices(vertices);
	if (!vx)
		return false;

	// Default pool resources may return a broken lock on device loss on Windows XP.
	bool success = VDMemcpyGuarded(vx, data, sizeof(Vertex)*vertices);

	UnlockVertices();
	return success;
}

uint16 *VDD3D9Manager::LockIndices(unsigned indices) {
	VDASSERT(indices <= kIndexBufferSize);
	if (mIndexBufferPt + indices > kIndexBufferSize) {
		mIndexBufferPt = 0;
	}

	mIndexBufferLockSize = indices;

	void *p;
	HRESULT hr;
	for(;;) {
		hr = mpD3DIB->Lock(mIndexBufferPt * sizeof(uint16), mIndexBufferLockSize * sizeof(uint16), &p, mIndexBufferPt ? D3DLOCK_NOOVERWRITE : D3DLOCK_DISCARD);
		if (hr != D3DERR_WASSTILLDRAWING)
			break;
		Sleep(1);
	}
	if (FAILED(hr)) {
		VDASSERT(false);
		return NULL;
	}

	return (uint16 *)p;
}

void VDD3D9Manager::UnlockIndices() {
	mIndexBufferPt += mIndexBufferLockSize;

	VDVERIFY(SUCCEEDED(mpD3DIB->Unlock()));
}

bool VDD3D9Manager::BeginScene() {
	if (!mbInScene) {
		HRESULT hr = mpD3DDevice->BeginScene();

		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: BeginScene() failed! hr = %08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}

		mbInScene = true;
	}

	return true;
}

bool VDD3D9Manager::EndScene() {
	if (mbInScene) {
		mbInScene = false;
		HRESULT hr = mpD3DDevice->EndScene();

		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: EndScene() failed! hr = %08x %s", hr, VDDispDecodeD3D9Error(hr));
			return false;
		}
	}

	return true;
}

void VDD3D9Manager::Flush() {
	if (mpD3DQuery) {
		HRESULT hr = mpD3DQuery->Issue(D3DISSUE_END);
		if (SUCCEEDED(hr)) {
			mpD3DQuery->GetData(NULL, 0, D3DGETDATA_FLUSH);
		}
	}
}

void VDD3D9Manager::Finish() {
	if (mpD3DQuery) {
		HRESULT hr = mpD3DQuery->Issue(D3DISSUE_END);
		if (SUCCEEDED(hr)) {
			while(S_FALSE == mpD3DQuery->GetData(NULL, 0, D3DGETDATA_FLUSH))
				::Sleep(1);
		}
	}
}

uint32 VDD3D9Manager::InsertFence() {
	if (!mbSupportsEventQueries)
		return 0;

	IDirect3DQuery9 *pQuery = NULL;
	if (mFenceFreeList.empty()) {
		HRESULT hr = mpD3DDevice->CreateQuery(D3DQUERYTYPE_EVENT, &pQuery);
		if (FAILED(hr)) {
			VDASSERT(!"Unable to create D3D query.");
			pQuery = NULL;
		}
	} else {
		pQuery = mFenceFreeList.back();
		mFenceFreeList.pop_back();
	}

	uint32 id = (mFenceQueueBase + (uint32)mFenceQueue.size()) | 0x80000000;
	mFenceQueue.push_back(pQuery);

	if (pQuery) {
		HRESULT hr = pQuery->Issue(D3DISSUE_END);
		if (SUCCEEDED(hr))
			pQuery->GetData(NULL, 0, D3DGETDATA_FLUSH);
	}

	return id;
}

void VDD3D9Manager::WaitFence(uint32 id) {
	while(!IsFencePassed(id))
		::Sleep(1);
}

bool VDD3D9Manager::IsFencePassed(uint32 id) {
	if (!id)
		return true;

	uint32 offset = (id - mFenceQueueBase) & 0x7fffffff;

	uint32 fenceQueueSize = (uint32)mFenceQueue.size();
	if (offset >= fenceQueueSize)
		return true;

	IDirect3DQuery9 *pQuery = mFenceQueue[offset];
	if (pQuery) {
		if (S_FALSE == pQuery->GetData(NULL, 0, D3DGETDATA_FLUSH))
			return false;

		mFenceFreeList.push_back(pQuery);
		mFenceQueue[offset] = NULL;
	}

	while(mFenceQueueHeadIndex < fenceQueueSize) {
		IDirect3DQuery9 *pQuery = mFenceQueue[mFenceQueueHeadIndex];
		if (pQuery) {
			if (S_FALSE == pQuery->GetData(NULL, 0, 0))
				break;

			mFenceFreeList.push_back(pQuery);
			mFenceQueue[mFenceQueueHeadIndex] = NULL;
		}

		++mFenceQueueHeadIndex;
	}

	if (mFenceQueueHeadIndex >= 64 && mFenceQueueHeadIndex+mFenceQueueHeadIndex >= fenceQueueSize) {
		mFenceQueueBase += mFenceQueueHeadIndex;
		mFenceQueue.erase(mFenceQueue.begin(), mFenceQueue.begin() + mFenceQueueHeadIndex);
		mFenceQueueHeadIndex = 0;
	}

	return true;
}

void VDD3D9Manager::BeginScope(const wchar_t *name) {
	if (mpBeginEvent)
		mpBeginEvent(RGB(192, 192, 192), name);
}

void VDD3D9Manager::EndScope() {
	if (mpEndEvent)
		mpEndEvent();
}

HRESULT VDD3D9Manager::DrawArrays(D3DPRIMITIVETYPE type, UINT vertStart, UINT primCount) {
	VDASSERT(mbInScene);

	HRESULT hr = mpD3DDevice->DrawPrimitive(type, mVertexBufferPt - mVertexBufferLockSize + vertStart, primCount);

	VDASSERT(SUCCEEDED(hr));

	return hr;
}

HRESULT VDD3D9Manager::DrawElements(D3DPRIMITIVETYPE type, UINT vertStart, UINT vertCount, UINT idxStart, UINT primCount) {
	VDASSERT(mbInScene);

	// The documentation for IDirect3DDevice9::DrawIndexedPrimitive() was probably
	// written under a hallucinogenic state.

	HRESULT hr = mpD3DDevice->DrawIndexedPrimitive(type, mVertexBufferPt - mVertexBufferLockSize + vertStart, 0, vertCount, mIndexBufferPt - mIndexBufferLockSize + idxStart, primCount);

	VDASSERT(SUCCEEDED(hr));

	return hr;
}

HRESULT VDD3D9Manager::Present(const RECT *src, HWND hwndDest, bool vsync, float& syncdelta, VDD3DPresentHistory& history) {
	if (!mPresentParms.Windowed)
		return S_OK;

	HRESULT hr;

	if (vsync && (mDevCaps.Caps & D3DCAPS_READ_SCANLINE)) {
		if (mpD3DQuery) {
			hr = mpD3DQuery->Issue(D3DISSUE_END);
			if (SUCCEEDED(hr)) {
				while(S_FALSE == mpD3DQuery->GetData(NULL, 0, D3DGETDATA_FLUSH))
					::Sleep(1);
			}
		}

		RECT r;
		if (GetWindowRect(hwndDest, &r)) {
			int top = 0;
			int bottom = GetSystemMetrics(SM_CYSCREEN);

			HMONITOR hmon = mhMonitorActual;
			MONITORINFO monInfo = {sizeof(MONITORINFO)};
			if (GetMonitorInfoW(hmon, &monInfo)) {
				top = monInfo.rcMonitor.top;
				bottom = monInfo.rcMonitor.bottom;
			}

			if (r.top < top)
				r.top = top;
			if (r.bottom > bottom)
				r.bottom = bottom;

			top += VDRoundToInt(history.mPresentDelay);

			r.top -= top;
			r.bottom -= top;

			// Poll raster status, and wait until we can safely blit. We assume that the
			// blit can outrace the beam. 
			D3DRASTER_STATUS rastStatus;
			UINT maxScanline = 0;
			int firstScan = -1;
			while(SUCCEEDED(mpD3DDevice->GetRasterStatus(0, &rastStatus))) {
				if (firstScan < 0)
					firstScan = rastStatus.InVBlank ? 0 : (int)rastStatus.ScanLine;

				if (rastStatus.InVBlank) {
					if (history.mVBlankSuccess >= 0.5f)
						break;

					rastStatus.ScanLine = 0;
				}

				// Check if we have wrapped around without seeing the VBlank. If this
				// occurs, force an exit. This prevents us from potentially burning a lot
				// of CPU time if the CPU becomes busy and can't poll the beam in a timely
				// manner.
				if (rastStatus.ScanLine < maxScanline)
					break;

				// Check if we're outside of the danger zone.
				if ((int)rastStatus.ScanLine < r.top || (int)rastStatus.ScanLine >= r.bottom)
					break;

				// We're in the danger zone. If the delta is greater than one tenth of the
				// display, do a sleep.
				if ((r.bottom - (int)rastStatus.ScanLine) * 10 >= (int)mDisplayMode.Height)
					::Sleep(1);

				maxScanline = rastStatus.ScanLine;
			}

			syncdelta = (float)(firstScan - r.bottom) / (float)(int)mDisplayMode.Height;
			syncdelta -= floorf(syncdelta);
			if (syncdelta > 0.5f)
				syncdelta -= 1.0f;

			hr = mpD3DDevice->Present(src, NULL, hwndDest, NULL);
			if (FAILED(hr)) {
				VDDEBUG_D3D("VideoDisplay/DX9: Present() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
				return hr;
			}

			D3DRASTER_STATUS rastStatus2;
			hr = mpD3DDevice->GetRasterStatus(0, &rastStatus2);
			if (SUCCEEDED(hr)) {
				if (rastStatus.InVBlank) {
					float success = rastStatus2.InVBlank || (int)rastStatus2.ScanLine <= r.top || (int)rastStatus2.ScanLine >= r.bottom ? 1.0f : 0.0f;

					history.mVBlankSuccess += (success - history.mVBlankSuccess) * 0.01f;
				}

				if (!rastStatus.InVBlank && !rastStatus2.InVBlank && rastStatus2.ScanLine > rastStatus.ScanLine) {
					float delta = (float)(int)(rastStatus2.ScanLine - rastStatus.ScanLine);

					history.mPresentDelay += (delta - history.mPresentDelay) * 0.01f;
				}
			} else {
				VDDEBUG_D3D("VideoDisplay/DX9: GetRasterStatus() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			}
		}
	} else {
		syncdelta = 0.0f;

		hr = mpD3DDevice->Present(src, NULL, hwndDest, NULL);

		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Present() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		}
	}

	return hr;
}

HRESULT VDD3D9Manager::PresentFullScreen(bool wait) {
	if (mPresentParms.Windowed)
		return S_OK;

	if (!wait && !IsFencePassed(mFSFence))
		return S_FALSE;

	HRESULT hr;
	IDirect3DSwapChain9 *pSwapChain;
	hr = mpD3DDevice->GetSwapChain(0, &pSwapChain);
	if (FAILED(hr))
		return hr;

	for(;;) {
		hr = pSwapChain->Present(NULL, NULL, NULL, NULL, D3DPRESENT_DONOTWAIT);

		if (SUCCEEDED(hr) || hr != D3DERR_WASSTILLDRAWING) {
			if (FAILED(hr)) {
				VDDEBUG_D3D("VideoDisplay/DX9: Present() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			}

			break;
		}

		if (!wait) {
			pSwapChain->Release();
			return S_FALSE;
		}

		::Sleep(1);
	}

	mFSFence = InsertFence();

	pSwapChain->Release();

	return hr;
}

#define REQUIRE(x, reason) if (!(x)) { VDDEBUG_D3D("VideoDisplay/DX9: 3D device is unsupported -- reason: " reason); return true; } else ((void)0)
#define REQUIRECAPS(capsflag, bits, reason) REQUIRE(!(~mDevCaps.capsflag & (bits)), reason)

bool VDD3D9Manager::Is3DCardLame() {
	// The dither check has been removed since WARP11 doesn't have it set. Note
	// that the SW check is still in place -- WARP11 sets the HW rast and TnL flags!
	REQUIRE(mDevCaps.DeviceType != D3DDEVTYPE_SW, "software device detected");
	REQUIRECAPS(PrimitiveMiscCaps, D3DPMISCCAPS_CULLNONE, "primitive misc caps check failed");
	REQUIRECAPS(TextureCaps, D3DPTEXTURECAPS_ALPHA | D3DPTEXTURECAPS_MIPMAP, "texture caps failed");
	REQUIRE(!(mDevCaps.TextureCaps & D3DPTEXTURECAPS_SQUAREONLY), "device requires square textures");
	REQUIRECAPS(TextureFilterCaps, D3DPTFILTERCAPS_MAGFPOINT | D3DPTFILTERCAPS_MAGFLINEAR
								| D3DPTFILTERCAPS_MINFPOINT | D3DPTFILTERCAPS_MINFLINEAR
								| D3DPTFILTERCAPS_MIPFPOINT | D3DPTFILTERCAPS_MIPFLINEAR, "texture filtering modes insufficient");
	REQUIRECAPS(TextureAddressCaps, D3DPTADDRESSCAPS_CLAMP | D3DPTADDRESSCAPS_WRAP, "texture addressing modes insufficient");
	REQUIRE(mDevCaps.MaxTextureBlendStages>0 && mDevCaps.MaxSimultaneousTextures>0, "not enough texture stages");
	return false;
}

#undef REQUIRECAPS
#undef REQUIRE

bool VDD3D9Manager::CreateInitTexture(UINT width, UINT height, UINT levels, D3DFORMAT format, IVDD3D9InitTexture **ppInitTexture) {
	VDD3D9InitTexture *p = new_nothrow VDD3D9InitTexture;

	if (!p)
		return false;

	p->AddRef();

	if (!p->Init(this, width, height, levels, format)) {
		p->Release();
		return false;
	}

	*ppInitTexture = p;
	return true;
}

bool VDD3D9Manager::CreateTexture(IVDD3D9InitTexture *initTex, IVDD3D9Texture **ppTexture) {
	vdrefptr<VDD3D9Texture> pTexture(new_nothrow VDD3D9Texture);
	if (!pTexture)
		return false;

	if (!pTexture->Init(this, nullptr) || !pTexture->Init(initTex))
		return false;

	*ppTexture = pTexture.release();
	return true;
}

bool VDD3D9Manager::CreateSharedTexture(const char *name, SharedTextureFactory factory, IVDD3D9Texture **ppTexture) {
	SharedTextures::iterator it(mSharedTextures.begin()), itEnd(mSharedTextures.end());
	for(; it!=itEnd; ++it) {
		VDD3D9Texture& texture = **it;

		if (!strcmp(texture.GetName(), name)) {
			*ppTexture = &texture;
			texture.AddRef();
			return true;
		}
	}

	vdrefptr<IVDD3D9TextureGenerator> pGenerator;
	if (factory) {
		if (!factory(~pGenerator))
			return false;
	}

	vdrefptr<VDD3D9Texture> pTexture(new_nothrow VDD3D9Texture);
	if (!pTexture)
		return false;

	pTexture->SetName(name);

	if (!pTexture->Init(this, pGenerator))
		return false;

	mSharedTextures.push_back(pTexture);

	*ppTexture = pTexture.release();
	return true;
}

bool VDD3D9Manager::CreateSwapChain(HWND hwnd, int width, int height, bool clipToMonitor, bool use16bit, IVDD3D9SwapChain **ppSwapChain) {
	D3DPRESENT_PARAMETERS pparms={};

	pparms.Windowed			= TRUE;
	pparms.SwapEffect		= D3DSWAPEFFECT_COPY;
	pparms.PresentationInterval = D3DPRESENT_INTERVAL_IMMEDIATE;
	pparms.BackBufferCount	= 1;

	if (mpD3DDeviceEx) {
		pparms.SwapEffect = D3DSWAPEFFECT_FLIPEX;
		pparms.PresentationInterval = D3DPRESENT_INTERVAL_ONE;
		pparms.BackBufferCount	= 3;
	}

	pparms.BackBufferWidth	= width;
	pparms.BackBufferHeight	= height;

	pparms.BackBufferFormat	= mPresentParms.BackBufferFormat;

	if (use16bit) {
		if (D3D_OK == mpD3D->CheckDeviceFormat(mAdapter, mDevType, D3DFMT_R5G6B5, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, D3DFMT_R5G6B5))
			pparms.BackBufferFormat	= D3DFMT_R5G6B5;
		else if (D3D_OK == mpD3D->CheckDeviceFormat(mAdapter, mDevType, D3DFMT_X1R5G5B5, D3DUSAGE_RENDERTARGET, D3DRTYPE_SURFACE, D3DFMT_X1R5G5B5))
			pparms.BackBufferFormat	= D3DFMT_X1R5G5B5;
	}

	pparms.hDeviceWindow = hwnd;

	// Disabling DEVICECLIP with only one monitor due to clipping issues on the
	// Dell Venue 8 Pro with display rotation.
	pparms.Flags = clipToMonitor && !mpD3DDeviceEx && GetSystemMetrics(SM_CMONITORS) > 1 ? D3DPRESENTFLAG_DEVICECLIP : 0;

	if (mpD3DDeviceEx)
		pparms.Flags |= D3DPRESENTFLAG_UNPRUNEDMODE;

	vdrefptr<IDirect3DSwapChain9> pD3DSwapChain;
	HRESULT hr = mpD3DDevice->CreateAdditionalSwapChain(&pparms, ~pD3DSwapChain);
	if (FAILED(hr))
		return false;

	vdrefptr<VDD3D9SwapChain> pSwapChain(new_nothrow VDD3D9SwapChain(this, pD3DSwapChain));
	if (!pSwapChain)
		return false;

	*ppSwapChain = pSwapChain.release();
	return true;
}

void VDD3D9Manager::SetSwapChainActive(IVDD3D9SwapChain *pSwapChain) {
	if (!pSwapChain)
		pSwapChain = mpImplicitSwapChain;

	IDirect3DSwapChain9 *pD3DSwapChain = static_cast<VDD3D9SwapChain *>(pSwapChain)->GetD3DSwapChain();

	IDirect3DSurface9 *pD3DBackBuffer;
	HRESULT hr = pD3DSwapChain->GetBackBuffer(0, D3DBACKBUFFER_TYPE_MONO, &pD3DBackBuffer);
	if (FAILED(hr))
		return;

	hr = mpD3DDevice->SetRenderTarget(0, pD3DBackBuffer);
	pD3DBackBuffer->Release();
}

HRESULT VDD3D9Manager::PresentSwapChain(IVDD3D9SwapChain *pSwapChain, const RECT *srcRect, HWND hwndDest, bool vsync, bool newframe, bool donotwait, float& syncDelta, VDD3DPresentHistory& history) {
	if (!mPresentParms.Windowed)
		return S_OK;

	if (!pSwapChain) {
		pSwapChain = mpImplicitSwapChain;
		if (!pSwapChain)
			return E_FAIL;
	}

	IDirect3DSwapChain9 *pD3DSwapChain = static_cast<VDD3D9SwapChain *>(pSwapChain)->GetD3DSwapChain();
	HRESULT hr;

	if (mpD3DDeviceEx) {
		DWORD flags = 0;

		if (!history.mbPresentPending)
			history.mPresentStartTime = VDGetPreciseTick();

		if (donotwait)
			flags |= D3DPRESENT_DONOTWAIT;

//		if (!vsync)
//			flags |= D3DPRESENT_FORCEIMMEDIATE | D3DPRESENT_DONOTWAIT;

		for(;;) {
			hr = pD3DSwapChain->Present(NULL, NULL, NULL, NULL, flags);

			if (hr != D3DERR_WASSTILLDRAWING)
				break;
			
			if (donotwait) {
				history.mbPresentPending = true;
				return S_FALSE;
			}

			::Sleep(1);
		}

		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: Present() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		}

		history.mAveragePresentTime += ((VDGetPreciseTick() - history.mPresentStartTime)*VDGetPreciseSecondsPerTick() - history.mAveragePresentTime) * 0.01f;
		history.mbPresentPending = false;
		return hr;
	}

	if (!vsync || !(mDevCaps.Caps & D3DCAPS_READ_SCANLINE)) {
		if (!newframe)
			return S_OK;

		syncDelta = 0.0f;

		for(;;) {
			hr = pD3DSwapChain->Present(srcRect, NULL, hwndDest, NULL, D3DPRESENT_DONOTWAIT);

			if (hr != D3DERR_WASSTILLDRAWING)
				break;

			if (donotwait) {
				hr = S_FALSE;
				break;
			}

			::Sleep(1);
		}

		history.mbPresentPending = false;
		return hr;
	}

	// Okay, now we know we're doing vsync.
	if (newframe && !history.mbPresentPending) {
		RECT r;
		if (!GetWindowRect(hwndDest, &r))
			return E_FAIL;

		int top = 0;
		int bottom = GetSystemMetrics(SM_CYSCREEN);

		HMONITOR hmon = mhMonitorActual;
		MONITORINFO monInfo = {sizeof(MONITORINFO)};
		if (GetMonitorInfoW(hmon, &monInfo)) {
			top = monInfo.rcMonitor.top;
			bottom = monInfo.rcMonitor.bottom;
		}

		if (r.top < top)
			r.top = top;
		if (r.bottom > bottom)
			r.bottom = bottom;

		r.top -= top;
		r.bottom -= top;

		history.mScanTop = r.top;
		history.mScanBottom = r.bottom;

		newframe = false;
		history.mbPresentPending = true;
		history.mbPresentBlitStarted = false;

		history.mLastScanline = -1;
		history.mPresentStartTime = VDGetPreciseTick();
	}

	if (!history.mbPresentPending)
		return S_OK;

	// Poll raster status, and wait until we can safely blit. We assume that the
	// blit can outrace the beam. 
	++history.mPollCount;
	for(;;) {
		// if we've already started the blit, skip beam-following
		if (history.mbPresentBlitStarted)
			break;

		D3DRASTER_STATUS rastStatus;
		hr = pD3DSwapChain->GetRasterStatus(&rastStatus);
		if (FAILED(hr)) {
			VDDEBUG_D3D("VideoDisplay/DX9: GetRasterStatus() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
			return hr;
		}

		if (rastStatus.InVBlank)
			rastStatus.ScanLine = 0;

		sint32 y1 = (sint32)history.mLastScanline;
		if (y1 < 0) {
			y1 = rastStatus.ScanLine;
			history.mAverageStartScanline += ((float)y1 - history.mAverageStartScanline) * 0.01f;
		}

		sint32 y2 = (sint32)rastStatus.ScanLine;

		history.mbLastWasVBlank	= rastStatus.InVBlank ? true : false;
		history.mLastScanline	= rastStatus.ScanLine;

		sint32 yt = (sint32)history.mScanlineTarget;

		history.mLastBracketY1 = y1;
		history.mLastBracketY2 = y2;

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

		if (donotwait)
			return S_FALSE;

		::Sleep(1);
	}

	history.mbPresentBlitStarted = true;

	if (donotwait) {
		hr = pD3DSwapChain->Present(srcRect, NULL, hwndDest, NULL, D3DPRESENT_DONOTWAIT);

		if (hr == D3DERR_WASSTILLDRAWING)
			return S_FALSE;
	} else
		hr = pD3DSwapChain->Present(srcRect, NULL, hwndDest, NULL, 0);

	history.mbPresentPending = false;
	if (FAILED(hr)) {
		VDDEBUG_D3D("VideoDisplay/DX9: Present() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
		return hr;
	}

	history.mAverageEndScanline += ((float)history.mLastScanline - history.mAverageEndScanline) * 0.01f;
	history.mAveragePresentTime += ((VDGetPreciseTick() - history.mPresentStartTime)*VDGetPreciseSecondsPerTick() - history.mAveragePresentTime) * 0.01f;

	D3DRASTER_STATUS rastStatus2;
	hr = pD3DSwapChain->GetRasterStatus(&rastStatus2);
	syncDelta = 0.0f;
	if (SUCCEEDED(hr)) {
		if (rastStatus2.InVBlank)
			rastStatus2.ScanLine = 0;

		float yf = ((float)rastStatus2.ScanLine - (float)history.mScanTop) / ((float)history.mScanBottom - (float)history.mScanTop);

		yf -= 0.2f;

		if (yf < 0.0f)
			yf = 0.0f;
		if (yf > 1.0f)
			yf = 1.0f;

		if (yf > 0.5f)
			yf -= 1.0f;

		syncDelta = yf;

		if (mbDwmCompositing) {
			// The DWM makes vertical sync a bit easier as it already sync presents, but we have to avoid
			// updating the frame around the time that it reads it.
			history.mScanlineTarget = (float)mDisplayMode.Height * 0.5f;
		} else {
			history.mScanlineTarget -= yf * 15.0f;
			if (history.mScanlineTarget < 0.0f)
				history.mScanlineTarget += (float)mDisplayMode.Height;
			else if (history.mScanlineTarget >= (float)mDisplayMode.Height)
				history.mScanlineTarget -= (float)mDisplayMode.Height;
		}

		float success = rastStatus2.InVBlank || (int)rastStatus2.ScanLine <= history.mScanTop || (int)rastStatus2.ScanLine >= history.mScanBottom ? 1.0f : 0.0f;

		int zone = 0;
		if (!history.mbLastWasVBlank)
			zone = ((int)history.mLastScanline * 16) / (int)mDisplayMode.Height;

		for(int i=0; i<17; ++i) {
			if (i != zone)
				history.mAttemptProb[i] *= 0.99f;
		}

		history.mAttemptProb[zone] += (1.0f - history.mAttemptProb[zone]) * 0.01f;
		history.mSuccessProb[zone] += (success - history.mSuccessProb[zone]) * 0.01f;

		if (history.mLastScanline < history.mScanTop) {
			history.mVBlankSuccess += (success - history.mVBlankSuccess) * 0.01f;
		}

		if (!history.mbLastWasVBlank && !rastStatus2.InVBlank && (int)rastStatus2.ScanLine > history.mLastScanline) {
			float delta = (float)(int)(rastStatus2.ScanLine - history.mLastScanline);

			history.mPresentDelay += (delta - history.mPresentDelay) * 0.01f;
		}
	} else {
		VDDEBUG_D3D("VideoDisplay/DX9: GetRasterStatus() failed (hr=%08X %s).", hr, VDDispDecodeD3D9Error(hr));
	}

	return hr;
}

HRESULT VDD3D9Manager::CreateDXGIFactory1(REFIID riid, void **ppFactory) {
	if (!mpCreateDXGIFactory1)
		return E_NOTIMPL;

	return mpCreateDXGIFactory1(riid, ppFactory);
}

LRESULT CALLBACK VDD3D9Manager::StaticDeviceWndProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam) {
	switch(msg) {
		case WM_NCCREATE:
			SetWindowLongPtr(hwnd, 0, (LONG_PTR)((CREATESTRUCT *)lParam)->lpCreateParams);
			break;

		case WM_SETFOCUS:
			{
				HWND hwndOldFocus = (HWND)wParam;

				// Direct3D likes to send focus to the focus window when you go full screen. Well,
				// we don't want that since we're using a hidden window, so if the focus came from
				// another window of ours, we send the focus back.
				if (hwndOldFocus) {
					DWORD pid;
					DWORD tid = GetWindowThreadProcessId(hwndOldFocus, &pid);

					if (tid == GetCurrentThreadId()) {
						SetFocus(hwndOldFocus);
						return 0;
					}
				}
			}
			break;

		case WM_DISPLAYCHANGE:
			{
				VDD3D9Manager *pMgr = (VDD3D9Manager *)GetWindowLongPtr(hwnd, 0);

				if (pMgr)
					pMgr->UpdateCachedDisplayMode();
			}
			break;
	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

///////////////////////////////////////////////////////////////////////////

const char *VDDispDecodeD3D9Error(uint32 hr) {
	switch(hr) {
#define X(val) case val: return #val;
		X(D3D_OK)
		X(E_UNEXPECTED)
		X(E_NOTIMPL)
		X(E_INVALIDARG)
		X(E_POINTER)
		X(E_FAIL)
		X(D3DERR_WRONGTEXTUREFORMAT)
		X(D3DERR_UNSUPPORTEDCOLOROPERATION)
		X(D3DERR_UNSUPPORTEDCOLORARG)
		X(D3DERR_UNSUPPORTEDALPHAOPERATION)
		X(D3DERR_UNSUPPORTEDALPHAARG)
		X(D3DERR_TOOMANYOPERATIONS)
		X(D3DERR_CONFLICTINGTEXTUREFILTER)
		X(D3DERR_UNSUPPORTEDFACTORVALUE)
		X(D3DERR_CONFLICTINGRENDERSTATE)
		X(D3DERR_UNSUPPORTEDTEXTUREFILTER)
		X(D3DERR_CONFLICTINGTEXTUREPALETTE)
		X(D3DERR_DRIVERINTERNALERROR)
		X(D3DERR_NOTFOUND)
		X(D3DERR_MOREDATA)
		X(D3DERR_DEVICELOST)
		X(D3DERR_DEVICENOTRESET)
		X(D3DERR_NOTAVAILABLE)
		X(D3DERR_OUTOFVIDEOMEMORY)
		X(D3DERR_INVALIDDEVICE)
		X(D3DERR_INVALIDCALL)
		X(D3DERR_DRIVERINVALIDCALL)
		X(D3DERR_WASSTILLDRAWING)
		X(D3DOK_NOAUTOGEN)
		X(D3DERR_DEVICEREMOVED)
		X(S_NOT_RESIDENT)
		X(S_RESIDENT_IN_SHARED_MEMORY)
		X(S_PRESENT_MODE_CHANGED)
		X(S_PRESENT_OCCLUDED)
		X(D3DERR_DEVICEHUNG)
		X(D3DERR_UNSUPPORTEDOVERLAY)
		X(D3DERR_UNSUPPORTEDOVERLAYFORMAT)
		X(D3DERR_CANNOTPROTECTCONTENT)
		X(D3DERR_UNSUPPORTEDCRYPTO)
		X(D3DERR_PRESENT_STATISTICS_DISJOINT)
#undef X

	default:
		return "?";
	}
}


#undef VDDEBUG_D3D
#undef VDDEBUG_D3D_MODESWITCH
