// testdll.cpp : Defines the exported functions for the DLL application.
//

#include <iostream>
#include <fstream>
#include "d3d8Wrapper.h"
#include "PointerSet.h"

#pragma comment(linker, "/EXPORT:Direct3DCreate8=_Direct3DCreate8@4")

#define LOG(x) { std::ofstream myfile; myfile.open ("d3d8_wrapper_log.txt", std::ios::app); myfile << x << "\n"; myfile.close(); }

D3D8Base::LPDIRECT3D8 g_D3D=NULL;

HMODULE hD3D;

ThreadSafePointerSet D3D8Wrapper::IDirect3D8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DDevice8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DResource8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DBaseTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVolumeTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DCubeTexture8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVertexBuffer8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DIndexBuffer8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DSurface8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DVolume8::m_List;
ThreadSafePointerSet D3D8Wrapper::IDirect3DSwapChain8::m_List;


extern "C"
{

	D3D8Wrapper::IDirect3DDevice8 *last_device = NULL;
	void (*rendering_callback)( int );
	
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3D8* D3D8Wrapper::IDirect3D8::GetDirect3D(D3D8Base::IDirect3D8* pD3D)
		{
			D3D8Wrapper::IDirect3D8* p = (D3D8Wrapper::IDirect3D8*) m_List.GetDataPtr(pD3D);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3D8(pD3D);
				m_List.AddMember(pD3D,p);
				return p;
			}

			p->m_ulRef++;
			return p;
		} 


		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3D8::Release(THIS)
		{

			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;

			if(ulRef == 0)
			{
				m_List.DeleteMember(GetDirect3D8());
				delete this;
				return 0L;
			}
			return ulRef;
		} 


		D3D8Wrapper::IDirect3D8* WINAPI Direct3DCreate8(UINT Version)
		{
			LOG("I'M IN UR VIDJA GAME");

			hD3D = LoadLibrary("C:\\Windows\\SysWOW64\\d3d8.dll");

			D3D8Wrapper::D3DCREATE pCreate = (D3D8Wrapper::D3DCREATE)GetProcAddress(hD3D, "Direct3DCreate8");

			// Contains our real object
			D3D8Base::IDirect3D8* pD3D = pCreate(D3D_SDK_VERSION);

			D3D8Wrapper::IDirect3D8* fD3D = D3D8Wrapper::IDirect3D8::GetDirect3D(pD3D);

			MessageBox(NULL, "", "HAX", MB_OK);
			return fD3D; //D3D8Base::Direct3DCreate8(Version);
		}

		D3D8Wrapper::IDirect3D8::IDirect3D8(D3D8Base::IDirect3D8* real) : D3D8Wrapper::IDirect3DUnknown((IUnknown*) real)
		{
			m_pD3D = real;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetAdapterDisplayMode(THIS_ UINT Adapter,D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("displaymode");
			HRESULT hr = m_pD3D->GetAdapterDisplayMode(Adapter, pMode);
			return hr;
		}

		
		/*** IDirect3D8 methods ***/
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::RegisterSoftwareDevice(void* pInitializeFunction)
		{
			LOG("RegisterSoftwareDevice");
			HRESULT hr = m_pD3D->RegisterSoftwareDevice(pInitializeFunction);

			return hr;
		}
		
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3D8::GetAdapterCount(THIS)
		{
			LOG("GetAdapterCount");
			return m_pD3D->GetAdapterCount();
		}

		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetAdapterIdentifier(UINT Adapter,DWORD Flags,D3D8Base::D3DADAPTER_IDENTIFIER8* pIdentifier)
		{
			LOG("GetAdapterIdentifier");
			HRESULT hr = m_pD3D->GetAdapterIdentifier(Adapter,Flags,pIdentifier);

			return hr;
		}

		
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3D8::GetAdapterModeCount(UINT Adapter)
		{
			LOG("GetAdapterModeCount");
			return m_pD3D->GetAdapterModeCount(Adapter);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::EnumAdapterModes(UINT Adapter,UINT Mode,D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("EnumAdapterModes");
			HRESULT hr = m_pD3D->EnumAdapterModes(Adapter,Mode,pMode);

			return hr;
		}

		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceType(UINT Adapter,D3D8Base::D3DDEVTYPE CheckType,D3D8Base::D3DFORMAT DisplayFormat,D3D8Base::D3DFORMAT BackBufferFormat,BOOL Windowed)
		{
			LOG("CheckDeviceType");
			HRESULT hr = m_pD3D->CheckDeviceType(Adapter,CheckType,DisplayFormat,BackBufferFormat,Windowed);

			return hr;
		}

		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceFormat(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,DWORD Usage,D3D8Base::D3DRESOURCETYPE RType,D3D8Base::D3DFORMAT CheckFormat)
		{
			LOG("CheckDeviceFormat");
			HRESULT hr = m_pD3D->CheckDeviceFormat(Adapter,DeviceType,AdapterFormat,Usage,RType,CheckFormat);

			return hr;
		}

		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceMultiSampleType(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT SurfaceFormat,BOOL Windowed,D3D8Base::D3DMULTISAMPLE_TYPE MultiSampleType)
		{
			LOG("CheckDeviceMultiSampleType");
			HRESULT hr = m_pD3D->CheckDeviceMultiSampleType(Adapter,DeviceType,SurfaceFormat,Windowed,MultiSampleType);

			return hr;
		}

		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDepthStencilMatch(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,D3D8Base::D3DFORMAT RenderTargetFormat,D3D8Base::D3DFORMAT DepthStencilFormat)
		{
			LOG("CheckDepthStencilMatch");
			HRESULT hr = m_pD3D->CheckDepthStencilMatch(Adapter,DeviceType,AdapterFormat,RenderTargetFormat,DepthStencilFormat);

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetDeviceCaps(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("GetDeviceCaps");
			HRESULT hr = m_pD3D->GetDeviceCaps(Adapter,DeviceType,pCaps);

			return hr;
		}

		STDMETHODIMP_(HMONITOR) D3D8Wrapper::IDirect3D8::GetAdapterMonitor(UINT Adapter)
		{
			LOG("GetAdapterMonitor");
			return m_pD3D->GetAdapterMonitor(Adapter);
		}
		

		STDMETHODIMP D3D8Wrapper::IDirect3D8::CreateDevice(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,HWND hFocusWindow,DWORD BehaviorFlags,D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DDevice8** ppReturnedDeviceInterface)
		{
			LOG("createdevice");
			LOG(pPresentationParameters);
			if (pPresentationParameters != NULL)
			{
				LOG(pPresentationParameters->BackBufferWidth);
				LOG(pPresentationParameters->BackBufferHeight);
				LOG(pPresentationParameters->BackBufferFormat);
				LOG(pPresentationParameters->BackBufferCount);
				LOG(pPresentationParameters->MultiSampleType);
				LOG(pPresentationParameters->SwapEffect);
				LOG(pPresentationParameters->hDeviceWindow);
				LOG(pPresentationParameters->Windowed);
				LOG(pPresentationParameters->EnableAutoDepthStencil);
				LOG(pPresentationParameters->Flags);
				LOG(pPresentationParameters->FullScreen_RefreshRateInHz);
				LOG(pPresentationParameters->FullScreen_PresentationInterval);
			}
			
			D3D8Base::IDirect3DDevice8* fd = NULL;

			D3D8Base::IDirect3DDevice8** fdp = &fd;

			LOG(fd);

			HRESULT hr = m_pD3D->CreateDevice(Adapter,DeviceType,hFocusWindow,BehaviorFlags,pPresentationParameters,fdp);//(D3D8Base::IDirect3DDevice8**)ppReturnedDeviceInterface);
			LOG(fd);
			LOG(hr);


			D3D8Wrapper::IDirect3DDevice8* f = D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(fd);

			*ppReturnedDeviceInterface = f;//(D3D8Wrapper::IDirect3DDevice8*)fd;

			//hr = D3DERR_NOTAVAILABLE;

			return hr;
		} 






		D3D8Wrapper::IDirect3DDevice8::IDirect3DDevice8(D3D8Base::IDirect3DDevice8* pDevice) : IDirect3DUnknown((IUnknown*) pDevice)
		{
			LOG("IDirect3DDevice8");
			m_pDevice = pDevice;
			rTarget = NULL;
			zStencil = NULL;
		}

		D3D8Wrapper::IDirect3DDevice8* D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(D3D8Base::IDirect3DDevice8* pDevice)
		{

			D3D8Wrapper::IDirect3DDevice8* p = (D3D8Wrapper::IDirect3DDevice8*) m_List.GetDataPtr(pDevice);
			if(p == NULL)
			{
				p = new D3D8Wrapper::IDirect3DDevice8(pDevice);
				m_List.AddMember(pDevice, p);
				last_device = p;
				return p;
			}

			p->m_ulRef++;
			return p;
		} 

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DDevice8::Release(THIS)
		{
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;

			if(ulRef == 0)
			{
				m_List.DeleteMember(GetD3D8Device());
				delete this;
				return NULL;
			}
			return ulRef;
		}


		D3D8Base::IDirect3DSurface8* D3D8Wrapper::IDirect3DSurface8::getReal()
		{
			return m_pD3D;
		}

		
		/*STDMETHOD(TestCooperativeLevel)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::TestCooperativeLevel()
		{
			LOG("TestCooperativeLevel");
			HRESULT hr = m_pDevice->TestCooperativeLevel();

			return hr;
		}

		/*STDMETHOD_(UINT, GetAvailableTextureMem)(THIS) PURE;*/
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3DDevice8::GetAvailableTextureMem()
		{
			LOG("GetAvailableTextureMem");
			HRESULT hr = m_pDevice->GetAvailableTextureMem();

			return hr;
		}

		/*STDMETHOD(ResourceManagerDiscardBytes)(THIS_ DWORD Bytes) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ResourceManagerDiscardBytes(DWORD Bytes)
		{
			LOG("ResourceManagerDiscardBytes");
			HRESULT hr = m_pDevice->ResourceManagerDiscardBytes(Bytes);

			return hr;
		}

		/*STDMETHOD(GetDirect3D)(THIS_ IDirect3D8** ppD3D8) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDirect3D(D3D8Wrapper::IDirect3D8** ppD3D8)
		{
			LOG("GetDirect3D");

			D3D8Base::IDirect3D8* fd = NULL;

			HRESULT hr = m_pDevice->GetDirect3D(&fd);//ppD3D8);

			D3D8Wrapper::IDirect3D8* f = D3D8Wrapper::IDirect3D8::GetDirect3D(fd);

			*ppD3D8 = f;

			return hr;
		}

		/*STDMETHOD(GetDeviceCaps)(THIS_ D3D8Base::D3DCAPS8* pCaps) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDeviceCaps(D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("GetDeviceCaps");
			HRESULT hr = m_pDevice->GetDeviceCaps(pCaps);

			return hr;
		}

		/*STDMETHOD(GetDisplayMode)(THIS_ D3D8Base::D3DDISPLAYMODE* pMode) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDisplayMode(D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("GetDisplayMode");
			HRESULT hr = m_pDevice->GetDisplayMode(pMode);

			return hr;
		}

		/*STDMETHOD(GetCreationParameters)(THIS_ D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCreationParameters(D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters)
		{
			LOG("GetCreationParameters");
			HRESULT hr = m_pDevice->GetCreationParameters(pParameters);

			return hr;
		}

		/*STDMETHOD(SetCursorProperties)(THIS_ UINT XHotSpot,UINT YHotSpot,D3D8Base::IDirect3DSurface8* pCursorBitmap) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCursorProperties(UINT XHotSpot,UINT YHotSpot,D3D8Wrapper::IDirect3DSurface8* pCursorBitmap)
		{
			LOG("SetCursorProperties");
			HRESULT hr = m_pDevice->SetCursorProperties(XHotSpot,YHotSpot,pCursorBitmap->GetSurface8());

			return hr;
		}

		/*STDMETHOD_(void, SetCursorPosition)(THIS_ int X,int Y,DWORD Flags) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetCursorPosition(int X,int Y,DWORD Flags)
		{
			LOG("SetCursorPosition");
			m_pDevice->SetCursorPosition(X,Y,Flags);
		}

		/*STDMETHOD_(BOOL, ShowCursor)(THIS_ BOOL bShow) PURE;*/
		STDMETHODIMP_(BOOL) D3D8Wrapper::IDirect3DDevice8::ShowCursor(BOOL bShow)
		{
			LOG("ShowCursor");
			HRESULT hr = m_pDevice->ShowCursor(bShow);

			return hr;
		}

		/*STDMETHOD(CreateAdditionalSwapChain)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Base::IDirect3DSwapChain8** pSwapChain) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateAdditionalSwapChain(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DSwapChain8** pSwapChain)
		{
			LOG("CreateAdditionalSwapChain");
			D3D8Base::IDirect3DSwapChain8* fd = NULL;

			HRESULT hr = m_pDevice->CreateAdditionalSwapChain(pPresentationParameters,&fd);//pSwapChain);

			D3D8Wrapper::IDirect3DSwapChain8* f = new D3D8Wrapper::IDirect3DSwapChain8(fd);
			*pSwapChain = f;

			return hr;
		}

		/*STDMETHOD(Reset)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Reset(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters)
		{
			LOG("Reset");
			HRESULT hr = m_pDevice->Reset(pPresentationParameters);

			return hr;
		}

		int present_count = 0;
		/*STDMETHOD(Present)(THIS_ CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Present(CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion)
		{
			LOG("Present");
			LOG(present_count);
			LOG(pSourceRect);
			if (pSourceRect != NULL)
			{
				LOG(pSourceRect->left);
				LOG(pSourceRect->top);
				LOG(pSourceRect->right);
				LOG(pSourceRect->bottom);
			}
			LOG(pDestRect);
			if (pSourceRect != NULL)
			{
				LOG(pDestRect->left);
				LOG(pDestRect->top);
				LOG(pDestRect->right);
				LOG(pDestRect->bottom);
			}

			LOG(hDestWindowOverride);
			LOG(pDirtyRegion);
			HRESULT hr = D3D_OK;
			//if (present_count++ == 10)
			//{
			rendering_callback(0);
				hr = m_pDevice->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);
				//hr = m_pDevice->Present(NULL,NULL,hDestWindowOverride,pDirtyRegion);

				D3D8Base::D3DVIEWPORT8 blah;
				blah.X = 0;
				blah.Y = 0;
				blah.Width = 640;
				blah.Height = 480;
				blah.MinZ = 0;
				blah.MaxZ = 1;

				D3D8Base::D3DRECT blah2;
				blah2.x1 = 0;
				blah2.y1 = 0;
				blah2.x2 = 640;
				blah2.y2 = 480;

				//this->SetViewport(&blah);
				//this->SetRenderState(D3D8Base::D3DRS_ZWRITEENABLE,1);
				//this->Clear(1,&blah2,2,0,1,0);
				//this->SetRenderState(D3D8Base::D3DRS_ZWRITEENABLE,0);
			//	present_count = 0;
			//}
			LOG(hr);
			return hr;
		}

		/*STDMETHOD(GetBackBuffer)(THIS_ UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Base::IDirect3DSurface8** ppBackBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("GetBackBuffer");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetBackBuffer(BackBuffer,Type,&fd);//ppBackBuffer);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppBackBuffer = f;

			return hr;
		}

		/*STDMETHOD(GetRasterStatus)(THIS_ D3D8Base::D3DRASTER_STATUS* pRasterStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRasterStatus(D3D8Base::D3DRASTER_STATUS* pRasterStatus)
		{
			LOG("GetRasterStatus");
			HRESULT hr = m_pDevice->GetRasterStatus(pRasterStatus);

			return hr;
		}

		/*STDMETHOD_(void, SetGammaRamp)(THIS_ DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetGammaRamp(DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("SetGammaRamp");
			m_pDevice->SetGammaRamp(Flags,pRamp);
		}

		/*STDMETHOD_(void, GetGammaRamp)(THIS_ D3D8Base::D3DGAMMARAMP* pRamp) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::GetGammaRamp(D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("GetGammaRamp");
			m_pDevice->GetGammaRamp(pRamp);
		}

		/*STDMETHOD(CreateTexture)(THIS_ UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DTexture8** ppTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateTexture(UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DTexture8** ppTexture)
		{
			LOG("CreateTexture");

			D3D8Base::IDirect3DTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateTexture(Width,Height,Levels,Usage,Format,Pool,&fd);//ppTexture);

			D3D8Wrapper::IDirect3DTexture8* f = new D3D8Wrapper::IDirect3DTexture8(fd);

			*ppTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateVolumeTexture)(THIS_ UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DVolumeTexture8** ppVolumeTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVolumeTexture(UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVolumeTexture8** ppVolumeTexture)
		{
			LOG("CreateVolumeTexture");

			D3D8Base::IDirect3DVolumeTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateVolumeTexture(Width,Height,Depth,Levels,Usage,Format,Pool,&fd);//ppVolumeTexture);

			D3D8Wrapper::IDirect3DVolumeTexture8* f = new D3D8Wrapper::IDirect3DVolumeTexture8(fd);

			*ppVolumeTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateCubeTexture)(THIS_ UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DCubeTexture8** ppCubeTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateCubeTexture(UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DCubeTexture8** ppCubeTexture)
		{
			LOG("CreateCubeTexture");

			D3D8Base::IDirect3DCubeTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateCubeTexture(EdgeLength,Levels, Usage,Format,Pool,&fd);//ppCubeTexture);

			D3D8Wrapper::IDirect3DCubeTexture8* f = new D3D8Wrapper::IDirect3DCubeTexture8(fd);

			*ppCubeTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateVertexBuffer)(THIS_ UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DVertexBuffer8** ppVertexBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexBuffer(UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVertexBuffer8** ppVertexBuffer)
		{
			LOG("CreateVertexBuffer");

			D3D8Base::IDirect3DVertexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->CreateVertexBuffer(Length,Usage,FVF,Pool,&fd);//ppVertexBuffer);

			D3D8Wrapper::IDirect3DVertexBuffer8* f = new D3D8Wrapper::IDirect3DVertexBuffer8(fd);

			*ppVertexBuffer = f;

			return hr;
		}

		/*STDMETHOD(CreateIndexBuffer)(THIS_ UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DIndexBuffer8** ppIndexBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateIndexBuffer(UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexBuffer)
		{
			LOG("CreateIndexBuffer");

			D3D8Base::IDirect3DIndexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->CreateIndexBuffer(Length,Usage,Format,Pool,&fd);//ppIndexBuffer);

			D3D8Wrapper::IDirect3DIndexBuffer8* f = new D3D8Wrapper::IDirect3DIndexBuffer8(fd);

			*ppIndexBuffer = f;

			return hr;
		}

		/*STDMETHOD(CreateRenderTarget)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateRenderTarget(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateRenderTarget");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateRenderTarget(Width,Height,Format,MultiSample,Lockable,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CreateDepthStencilSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateDepthStencilSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateDepthStencilSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateDepthStencilSurface(Width,Height,Format,MultiSample,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CreateImageSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateImageSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateImageSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateImageSurface(Width,Height,Format,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CopyRects)(THIS_ D3D8Base::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Base::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CopyRects(D3D8Wrapper::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Wrapper::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray)
		{
			LOG("CopyRects");
			LOG(pSourceSurface);
			LOG(pSourceSurface->getReal());
			LOG(pDestinationSurface);
			LOG(pDestinationSurface->getReal());

			if (pSourceSurface->m_ulRef == 0 || (pSourceSurface->GetSurface8()) == (pDestinationSurface->GetSurface8()))
			{
				LOG("WTF");
				return D3DERR_INVALIDCALL;
			}

			HRESULT hr = m_pDevice->CopyRects(pSourceSurface->GetSurface8(),pSourceRectsArray,cRects,pDestinationSurface->GetSurface8(),pDestPointsArray);

			LOG("Back??");

			LOG(hr);

			LOG(pSourceSurface);
			LOG(pSourceSurface->getReal());
			LOG(pDestinationSurface);
			LOG(pDestinationSurface->getReal());
			return hr;
		}

		/*STDMETHOD(UpdateTexture)(THIS_ D3D8Base::IDirect3DBaseTexture8* pSourceTexture,D3D8Base::IDirect3DBaseTexture8* pDestinationTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::UpdateTexture(D3D8Wrapper::IDirect3DBaseTexture8* pSourceTexture,D3D8Wrapper::IDirect3DBaseTexture8* pDestinationTexture)
		{
			LOG("UpdateTexture");
			HRESULT hr = m_pDevice->UpdateTexture(pSourceTexture->getReal2(),pDestinationTexture->getReal2());

			return hr;
		}

		/*STDMETHOD(GetFrontBuffer)(THIS_ D3D8Base::IDirect3DSurface8* pDestSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetFrontBuffer(D3D8Wrapper::IDirect3DSurface8* pDestSurface)
		{
			LOG("GetFrontBuffer");
			HRESULT hr = m_pDevice->GetFrontBuffer(pDestSurface->GetSurface8());

			return hr;
		}

		/*STDMETHOD(SetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8* pRenderTarget,D3D8Base::IDirect3DSurface8* pNewZStencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderTarget(D3D8Wrapper::IDirect3DSurface8* pRenderTarget,D3D8Wrapper::IDirect3DSurface8* pNewZStencil)
		{
			LOG("SetRenderTarget");
			LOG(pRenderTarget);
			LOG(pNewZStencil);
			HRESULT hr = m_pDevice->SetRenderTarget(pRenderTarget->GetSurface8(),pNewZStencil->GetSurface8());

			/*if (this->rTarget != NULL)
			{
				this->rTarget->Release();
			}
			if (this->zStencil != NULL)
			{
				this->zStencil->Release();
			}*/

			this->rTarget = pRenderTarget;
			this->zStencil = pNewZStencil;

			this->rTarget->m_ulRef++;
			this->zStencil->m_ulRef++;



			return hr;
		}

		/*STDMETHOD(GetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8** ppRenderTarget) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderTarget(D3D8Wrapper::IDirect3DSurface8** ppRenderTarget)
		{
			LOG("GetRenderTarget");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetRenderTarget(&fd);//ppRenderTarget);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppRenderTarget = f;

			return hr;
		}

		/*STDMETHOD(GetDepthStencilSurface)(THIS_ D3D8Base::IDirect3DSurface8** ppZStencilSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDepthStencilSurface(D3D8Wrapper::IDirect3DSurface8** ppZStencilSurface)
		{
			LOG("GetDepthStencilSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetDepthStencilSurface(&fd);//ppZStencilSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppZStencilSurface = f;

			return hr;
		}

		/*STDMETHOD(BeginScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginScene()
		{
			LOG("BeginScene");
			HRESULT hr = m_pDevice->BeginScene();

			return hr;
		}

		/*STDMETHOD(EndScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndScene()
		{
			LOG("EndScene");
			HRESULT hr = m_pDevice->EndScene();

			return hr;
		}

		/*STDMETHOD(Clear)(THIS_ DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Clear(DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil)
		{
			LOG("Clear");
			LOG(Count);
			LOG(pRects);
			if (pRects != NULL)
			{
				LOG(" " << pRects->x1);
				LOG(" " << pRects->y1);
				LOG(" " << pRects->x2);
				LOG(" " << pRects->y2);
			}
			LOG(Flags);
			LOG(Color);
			LOG(Z);
			LOG(Stencil);

			HRESULT hr = m_pDevice->Clear(Count,pRects,Flags,Color,Z,Stencil);

			return hr;
		}

		/*STDMETHOD(SetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("SetTransform");
			HRESULT hr = m_pDevice->SetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(GetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("GetTransform");
			HRESULT hr = m_pDevice->GetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(MultiplyTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE,CONST D3D8Base::D3DMATRIX*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::MultiplyTransform(D3D8Base::D3DTRANSFORMSTATETYPE foo,CONST D3D8Base::D3DMATRIX* bar)
		{
			LOG("MultiplyTransform");
			HRESULT hr = m_pDevice->MultiplyTransform(foo, bar);

			return hr;
		}

		/*STDMETHOD(SetViewport)(THIS_ CONST D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetViewport(CONST D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("SetViewport");
			LOG(pViewport);
			if (pViewport != NULL)
			{
				LOG(pViewport->X);
				LOG(pViewport->Y);
				LOG(pViewport->Width);
				LOG(pViewport->Height);
				LOG(pViewport->MinZ);
				LOG(pViewport->MaxZ);
			}

			D3D8Base::D3DVIEWPORT8 blah;
			blah.X = 0;
			blah.Y = 0;
			blah.Width = 800;
			blah.Height = 600;
			blah.MinZ = 0;
			blah.MaxZ = 1;

			//HRESULT hr = m_pDevice->SetViewport(pViewport);
			HRESULT hr = m_pDevice->SetViewport(&blah);
			LOG(hr);
			return hr;
		}

		/*STDMETHOD(GetViewport)(THIS_ D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetViewport(D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("GetViewport");
			HRESULT hr = m_pDevice->GetViewport(pViewport);
			LOG(pViewport);
			if (pViewport != NULL)
			{
				LOG(pViewport->X);
				LOG(pViewport->Y);
				LOG(pViewport->Width);
				LOG(pViewport->Height);
				LOG(pViewport->MinZ);
				LOG(pViewport->MaxZ);
			}
			LOG(hr);
			return hr;
		}

		/*STDMETHOD(SetMaterial)(THIS_ CONST D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetMaterial(CONST D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("SetMaterial");
			HRESULT hr = m_pDevice->SetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(GetMaterial)(THIS_ D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetMaterial(D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("GetMaterial");
			HRESULT hr = m_pDevice->GetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(SetLight)(THIS_ DWORD Index,CONST D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetLight(DWORD Index,CONST D3D8Base::D3DLIGHT8* foo)
		{
			LOG("SetLight");
			HRESULT hr = m_pDevice->SetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(GetLight)(THIS_ DWORD Index,D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLight(DWORD Index,D3D8Base::D3DLIGHT8* foo)
		{
			LOG("GetLight");
			HRESULT hr = m_pDevice->GetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(LightEnable)(THIS_ DWORD Index,BOOL Enable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::LightEnable(DWORD Index,BOOL Enable)
		{
			LOG("LightEnable");
			HRESULT hr = m_pDevice->LightEnable(Index,Enable);

			return hr;
		}

		/*STDMETHOD(GetLightEnable)(THIS_ DWORD Index,BOOL* pEnable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLightEnable(DWORD Index,BOOL* pEnable)
		{
			LOG("GetLightEnable");
			HRESULT hr = m_pDevice->GetLightEnable(Index,pEnable);

			return hr;
		}

		/*STDMETHOD(SetClipPlane)(THIS_ DWORD Index,CONST float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipPlane(DWORD Index,CONST float* pPlane)
		{
			LOG("SetClipPlane");
			HRESULT hr = m_pDevice->SetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(GetClipPlane)(THIS_ DWORD Index,float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipPlane(DWORD Index,float* pPlane)
		{
			LOG("GetClipPlane");
			HRESULT hr = m_pDevice->GetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(SetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD Value)
		{
			LOG("SetRenderState");
			LOG(State);
			LOG(Value);

			HRESULT hr = m_pDevice->SetRenderState(State,Value);

			return hr;
		}

		/*STDMETHOD(GetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue)
		{
			LOG("GetRenderState");
			HRESULT hr = m_pDevice->GetRenderState(State,pValue);

			return hr;
		}

		/*STDMETHOD(BeginStateBlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginStateBlock()
		{
			LOG("BeginStateBlock");
			HRESULT hr = m_pDevice->BeginStateBlock();

			return hr;
		}

		/*STDMETHOD(EndStateBlock)(THIS_ DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndStateBlock(DWORD* pToken)
		{
			LOG("EndStateBlock");
			HRESULT hr = m_pDevice->EndStateBlock(pToken);

			return hr;
		}

		/*STDMETHOD(ApplyStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ApplyStateBlock(DWORD Token)
		{
			LOG("ApplyStateBlock");
			HRESULT hr = m_pDevice->ApplyStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CaptureStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CaptureStateBlock(DWORD Token)
		{
			LOG("CaptureStateBlock");
			HRESULT hr = m_pDevice->CaptureStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(DeleteStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteStateBlock(DWORD Token)
		{
			LOG("DeleteStateBlock");
			HRESULT hr = m_pDevice->DeleteStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CreateStateBlock)(THIS_ D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateStateBlock(D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken)
		{
			LOG("CreateStateBlock");
			HRESULT hr = m_pDevice->CreateStateBlock(Type,pToken);

			return hr;
		}

		/*STDMETHOD(SetClipStatus)(THIS_ CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipStatus(CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("SetClipStatus");
			HRESULT hr = m_pDevice->SetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetClipStatus)(THIS_ D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipStatus(D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("GetClipStatus");
			HRESULT hr = m_pDevice->GetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8** ppTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8** ppTexture)
		{
			LOG("GetTexture");

			D3D8Base::IDirect3DBaseTexture8* fd = NULL;

			HRESULT hr = m_pDevice->GetTexture(Stage,&fd);//ppTexture);

			D3D8Wrapper::IDirect3DBaseTexture8* f = new D3D8Wrapper::IDirect3DBaseTexture8(fd);

			*ppTexture = f;

			return hr;
		}

		/*STDMETHOD(SetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8* pTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8* pTexture)
		{
			LOG("SetTexture");
			LOG("pTexture:  " << pTexture);

			if (pTexture == NULL)
			{
				return m_pDevice->SetTexture(Stage,NULL);
			}
			else
			{
				LOG(pTexture->getReal());
				LOG(pTexture->getReal2());
				HRESULT hr = m_pDevice->SetTexture(Stage,pTexture->getReal2());

				return hr;
			}
		}

		/*STDMETHOD(GetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue)
		{
			LOG("GetTextureStageState");
			HRESULT hr = m_pDevice->GetTextureStageState(Stage,Type,pValue);

			return hr;
		}

		/*STDMETHOD(SetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value)
		{
			LOG("SetTextureStageState");
			HRESULT hr = m_pDevice->SetTextureStageState(Stage,Type,Value);

			return hr;
		}

		/*STDMETHOD(ValidateDevice)(THIS_ DWORD* pNumPasses) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ValidateDevice(DWORD* pNumPasses)
		{
			LOG("ValidateDevice");
			HRESULT hr = m_pDevice->ValidateDevice(pNumPasses);

			return hr;
		}

		/*STDMETHOD(GetInfo)(THIS_ DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetInfo(DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize)
		{
			LOG("GetInfo");
			HRESULT hr = m_pDevice->GetInfo(DevInfoID,pDevInfoStruct,DevInfoStructSize);

			return hr;
		}

		/*STDMETHOD(SetPaletteEntries)(THIS_ UINT PaletteNumber,CONST PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPaletteEntries(UINT PaletteNumber,CONST PALETTEENTRY* pEntries)
		{
			LOG("SetPaletteEntries");
			HRESULT hr = m_pDevice->SetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(GetPaletteEntries)(THIS_ UINT PaletteNumber,PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPaletteEntries(UINT PaletteNumber,PALETTEENTRY* pEntries)
		{
			LOG("GetPaletteEntries");
			HRESULT hr = m_pDevice->GetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(SetCurrentTexturePalette)(THIS_ UINT PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCurrentTexturePalette(UINT PaletteNumber)
		{
			LOG("SetCurrentTexturePalette");
			HRESULT hr = m_pDevice->SetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(GetCurrentTexturePalette)(THIS_ UINT *PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCurrentTexturePalette(UINT *PaletteNumber)
		{
			LOG("GetCurrentTexturePalette");
			HRESULT hr = m_pDevice->GetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(DrawPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount)
		{
			LOG("DrawPrimitive");
			HRESULT hr = m_pDevice->DrawPrimitive(PrimitiveType,StartVertex,PrimitiveCount);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount)
		{
			LOG("DrawIndexedPrimitive");
			HRESULT hr = m_pDevice->DrawIndexedPrimitive(PrimitiveType,minIndex,NumVertices,startIndex,primCount);

			return hr;
		}

		/*STDMETHOD(DrawPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("DrawPrimitiveUP");
			HRESULT hr = m_pDevice->DrawPrimitiveUP(PrimitiveType,PrimitiveCount,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("DrawIndexedPrimitiveUP");
			HRESULT hr = m_pDevice->DrawIndexedPrimitiveUP(PrimitiveType,MinVertexIndex,NumVertexIndices,PrimitiveCount,pIndexData,IndexDataFormat,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(ProcessVertices)(THIS_ UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Base::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ProcessVertices(UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Wrapper::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags)
		{
			LOG("ProcessVertices");
			HRESULT hr = m_pDevice->ProcessVertices(SrcStartIndex,DestIndex,VertexCount,pDestBuffer->getReal2(),Flags);

			return hr;
		}

		/*STDMETHOD(CreateVertexShader)(THIS_ CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexShader(CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage)
		{
			LOG("CreateVertexShader");
			HRESULT hr = m_pDevice->CreateVertexShader(pDeclaration,pFunction,pHandle,Usage);

			return hr;
		}

		/*STDMETHOD(SetVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShader(DWORD Handle)
		{
			LOG("SetVertexShader");
			HRESULT hr = m_pDevice->SetVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetVertexShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShader(DWORD* pHandle)
		{
			LOG("GetVertexShader");
			HRESULT hr = m_pDevice->GetVertexShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeleteVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteVertexShader(DWORD Handle)
		{
			LOG("DeleteVertexShader");
			HRESULT hr = m_pDevice->DeleteVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetVertexShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("SetVertexShaderConstant");
			HRESULT hr = m_pDevice->SetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("GetVertexShaderConstant");
			HRESULT hr = m_pDevice->GetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderDeclaration)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderDeclaration(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetVertexShaderDeclaration");
			HRESULT hr = m_pDevice->GetVertexShaderDeclaration(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetVertexShaderFunction");
			HRESULT hr = m_pDevice->GetVertexShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(SetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8* pStreamData,UINT Stride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8* pStreamData,UINT Stride)
		{
			LOG("SetStreamSource");
			HRESULT hr = m_pDevice->SetStreamSource(StreamNumber,pStreamData->getReal2(),Stride);

			return hr;
		}

		/*STDMETHOD(GetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride)
		{
			LOG("GetStreamSource");

			D3D8Base::IDirect3DVertexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetStreamSource(StreamNumber,&fd,pStride);//ppStreamData,pStride);

			D3D8Wrapper::IDirect3DVertexBuffer8* f = new D3D8Wrapper::IDirect3DVertexBuffer8(fd);

			*ppStreamData = f;

			return hr;
		}

		/*STDMETHOD(SetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetIndices(D3D8Wrapper::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex)
		{
			LOG("SetIndices");
			HRESULT hr = m_pDevice->SetIndices(pIndexData->getReal2(),BaseVertexIndex);

			return hr;
		}

		/*STDMETHOD(GetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetIndices(D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex)
		{
			LOG("GetIndices");

			D3D8Base::IDirect3DIndexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetIndices(&fd,pBaseVertexIndex);// ppIndexData,pBaseVertexIndex);

			D3D8Wrapper::IDirect3DIndexBuffer8* f = new D3D8Wrapper::IDirect3DIndexBuffer8(fd);

			*ppIndexData = f;

			return hr;
		}

		/*STDMETHOD(CreatePixelShader)(THIS_ CONST DWORD* pFunction,DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreatePixelShader(CONST DWORD* pFunction,DWORD* pHandle)
		{
			LOG("CreatePixelShader");
			HRESULT hr = m_pDevice->CreatePixelShader(pFunction,pHandle);

			return hr;
		}

		/*STDMETHOD(SetPixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShader(DWORD Handle)
		{
			LOG("SetPixelShader");
			HRESULT hr = m_pDevice->SetPixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetPixelShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShader(DWORD* pHandle)
		{
			LOG("GetPixelShader");
			HRESULT hr = m_pDevice->GetPixelShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeletePixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePixelShader(DWORD Handle)
		{
			LOG("DeletePixelShader");
			HRESULT hr = m_pDevice->DeletePixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetPixelShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("SetPixelShaderConstant");
			HRESULT hr = m_pDevice->SetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("GetPixelShaderConstant");
			HRESULT hr = m_pDevice->GetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetPixelShaderFunction");
			HRESULT hr = m_pDevice->GetPixelShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(DrawRectPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawRectPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo)
		{
			LOG("DrawRectPatch");
			HRESULT hr = m_pDevice->DrawRectPatch(Handle,pNumSegs,pRectPatchInfo);

			return hr;
		}

		/*STDMETHOD(DrawTriPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawTriPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo)
		{
			LOG("DrawTriPatch");
			HRESULT hr = m_pDevice->DrawTriPatch(Handle,pNumSegs,pTriPatchInfo);

			return hr;
		}

		/*STDMETHOD(DeletePatch)(THIS_ UINT Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePatch(UINT Handle)
		{
			LOG("DeletePatch");
			HRESULT hr = m_pDevice->DeletePatch(Handle);

			return hr;
		}





		D3D8Wrapper::IDirect3DSwapChain8::IDirect3DSwapChain8(D3D8Base::IDirect3DSwapChain8* pSwapChain) : IDirect3DUnknown((IUnknown*) pSwapChain)
		{
			LOG("IDirect3DSwapChain8");
			m_pD3D = pSwapChain;
		}

		D3D8Wrapper::IDirect3DSwapChain8* D3D8Wrapper::IDirect3DSwapChain8::GetSwapChain(D3D8Base::IDirect3DSwapChain8* pSwapChain)
		{
			D3D8Wrapper::IDirect3DSwapChain8* p = (D3D8Wrapper::IDirect3DSwapChain8*) m_List.GetDataPtr(pSwapChain);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3DSwapChain8(pSwapChain);
				m_List.AddMember(pSwapChain, p);
				return p;
			}
    
			p->m_ulRef++;
			return p;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DSwapChain8::Release(THIS)
		{
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetSwapChain8());
				delete this;
				return 0;
			}
			return ulRef;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSwapChain8::Present(CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion)
		{
			LOG("IDirect3DSwapChain8::Present");
			HRESULT hr = m_pD3D->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSwapChain8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("IDirect3DSwapChain8::GetBackBuffer");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetBackBuffer(BackBuffer,Type,&fd);//ppBackBuffer);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppBackBuffer = f;

			return hr;
		}





		D3D8Wrapper::IDirect3DResource8::IDirect3DResource8(D3D8Base::IDirect3DResource8* pResource) : IDirect3DUnknown((IUnknown*) pResource)
		{
			LOG("IDirect3DResource8");
			m_pD3D = pResource;
		}

		D3D8Wrapper::IDirect3DResource8::IDirect3DResource8(D3D8Wrapper::IDirect3DResource8* pResource) : IDirect3DUnknown((IUnknown*) pResource)
		{
			LOG("IDirect3DResource8 -- 2");
			m_pD3D = pResource->getReal();
		}

		D3D8Wrapper::IDirect3DResource8* D3D8Wrapper::IDirect3DResource8::GetResource(D3D8Base::IDirect3DResource8* pSwapChain)
		{
			D3D8Wrapper::IDirect3DResource8* p = (D3D8Wrapper::IDirect3DResource8*) m_List.GetDataPtr(pSwapChain);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3DResource8(pSwapChain);
				m_List.AddMember(pSwapChain, p);
				return p;
			}
    
			p->m_ulRef++;
			return p;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DResource8::Release(THIS)
		{
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetResource());
				delete this;
				return 0;
			}
			return ulRef;
		}

		D3D8Base::IDirect3DResource8* D3D8Wrapper::IDirect3DResource8::getReal()
		{
			LOG("IDirect3DResource8::getReal");
			return m_pD3D;
		}

		/*STDMETHOD(GetDevice)(THIS_ IDirect3DDevice8** ppDevice) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DResource8::GetDevice");
			D3D8Base::IDirect3DDevice8* fd = NULL;

			HRESULT hr = m_pD3D->GetDevice(&fd);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* f = new D3D8Wrapper::IDirect3DDevice8(fd);
			
			*ppDevice = f;

			return hr;
		}

		/*STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DResource8::SetPrivateData");
			HRESULT hr = m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);

			return hr;
		}

		/*STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DResource8::GetPrivateData");
			HRESULT hr = m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DResource8::FreePrivateData");
			HRESULT hr = m_pD3D->FreePrivateData(refguid);

			return hr;
		}

		/*STDMETHOD_(DWORD, SetPriority)(THIS_ DWORD PriorityNew) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DResource8::SetPriority(DWORD PriorityNew)
		{
			LOG("IDirect3DResource8::SetPriority");
			return m_pD3D->SetPriority(PriorityNew);
		}

		/*STDMETHOD_(DWORD, GetPriority)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DResource8::GetPriority()
		{
			LOG("IDirect3DResource8::GetPriority");
			return m_pD3D->GetPriority();
		}

		/*STDMETHOD_(void, PreLoad)(THIS) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DResource8::PreLoad()
		{
			LOG("IDirect3DResource8::PreLoad");
			return m_pD3D->PreLoad();
		}

		/*STDMETHOD_(D3DRESOURCETYPE, GetType)(THIS) PURE;*/
		STDMETHODIMP_(D3D8Base::D3DRESOURCETYPE) D3D8Wrapper::IDirect3DResource8::GetType()
		{
			LOG("IDirect3DResource8::GetType");
			return m_pD3D->GetType();
		}




		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Base::IDirect3DBaseTexture8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8 -- 1");
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Wrapper::IDirect3DBaseTexture8* pTexture) : IDirect3DResource8((D3D8Wrapper::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8 -- 2");
			m_pD3D = pTexture->getReal2();
		}

		D3D8Base::IDirect3DBaseTexture8* D3D8Wrapper::IDirect3DBaseTexture8::getReal2()
		{
			LOG("IDirect3DBaseTexture8::getReal2");
			return m_pD3D;
		}


		/*STDMETHOD_(DWORD, SetLOD)(THIS_ DWORD LODNew) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::SetLOD(DWORD LODNew)
		{
			LOG("IDirect3DBaseTexture8::SetLOD");
			return m_pD3D->SetLOD(LODNew);
		}

		/*STDMETHOD_(DWORD, GetLOD)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLOD()
		{
			LOG("IDirect3DBaseTexture8::GetLOD");
			return m_pD3D->GetLOD();
		}

		/*STDMETHOD_(DWORD, GetLevelCount)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLevelCount()
		{
			LOG("IDirect3DBaseTexture8::GetLevelCount");
			return m_pD3D->GetLevelCount();
		}




		D3D8Wrapper::IDirect3DTexture8::IDirect3DTexture8(D3D8Base::IDirect3DTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8");
			m_pD3D = pTexture;
		}


		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DTexture8::GetLevelDesc");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetSurfaceLevel)(THIS_ UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetSurfaceLevel(UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel)
		{
			LOG("IDirect3DTexture8::GetSurfaceLevel");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetSurfaceLevel(Level,&fd);//ppSurfaceLevel);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurfaceLevel = f;

			LOG(f);
			LOG(f->GetSurface8());
			LOG(hr);

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::LockRect(UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DTexture8::LockRect");
			HRESULT hr = m_pD3D->LockRect(Level,pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS_ UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::UnlockRect(UINT Level)
		{
			LOG("IDirect3DTexture8::UnlockRect");
			HRESULT hr = m_pD3D->UnlockRect(Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyRect)(THIS_ CONST RECT* pDirtyRect) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::AddDirtyRect(CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DTexture8::AddDirtyRect");
			HRESULT hr = m_pD3D->AddDirtyRect(pDirtyRect);

			return hr;
		}






		D3D8Wrapper::IDirect3DVolumeTexture8::IDirect3DVolumeTexture8(D3D8Base::IDirect3DVolumeTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolumeTexture8::GetLevelDesc");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetVolumeLevel)(THIS_ UINT Level,IDirect3DVolume8** ppVolumeLevel) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetVolumeLevel(UINT Level,D3D8Wrapper::IDirect3DVolume8** ppVolumeLevel)
		{
			LOG("IDirect3DVolumeTexture8::GetVolumeLevel");

			D3D8Base::IDirect3DVolume8* fd = NULL;

			HRESULT hr = m_pD3D->GetVolumeLevel(Level,&fd);//ppVolumeLevel);

			D3D8Wrapper::IDirect3DVolume8* f = new D3D8Wrapper::IDirect3DVolume8(fd);

			*ppVolumeLevel = f;

			return hr;
		}

		/*STDMETHOD(LockBox)(THIS_ UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::LockBox(UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolumeTexture8::LockBox");
			HRESULT hr = m_pD3D->LockBox(Level,pLockedVolume,pBox,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockBox)(THIS_ UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::UnlockBox(UINT Level)
		{
			LOG("IDirect3DVolumeTexture8::UnlockBox");
			HRESULT hr = m_pD3D->UnlockBox(Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyBox)(THIS_ CONST D3D8Base::D3DBOX* pDirtyBox) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::AddDirtyBox(CONST D3D8Base::D3DBOX* pDirtyBox)
		{
			LOG("IDirect3DVolumeTexture8::AddDirtyBox");
			HRESULT hr = m_pD3D->AddDirtyBox(pDirtyBox);

			return hr;
		}






		D3D8Wrapper::IDirect3DCubeTexture8::IDirect3DCubeTexture8(D3D8Base::IDirect3DCubeTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DCubeTexture8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DCubeTexture8::GetLevelDesc");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetCubeMapSurface)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetCubeMapSurface(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface)
		{
			LOG("IDirect3DCubeTexture8::GetCubeMapSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetCubeMapSurface(FaceType,Level,&fd);//ppCubeMapSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppCubeMapSurface = f;

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::LockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DCubeTexture8::LockRect");
			HRESULT hr = m_pD3D->LockRect(FaceType,Level,pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::UnlockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level)
		{
			LOG("IDirect3DCubeTexture8::UnlockRect");
			HRESULT hr = m_pD3D->UnlockRect(FaceType,Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::AddDirtyRect(D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DCubeTexture8::AddDirtyRect");
			HRESULT hr = m_pD3D->AddDirtyRect(FaceType,pDirtyRect);

			return hr;
		}






		D3D8Wrapper::IDirect3DVertexBuffer8::IDirect3DVertexBuffer8(D3D8Base::IDirect3DVertexBuffer8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DVertexBuffer8");
			m_pD3D = pTexture;
		}

		D3D8Base::IDirect3DVertexBuffer8* D3D8Wrapper::IDirect3DVertexBuffer8::getReal2()
		{
			LOG("IDirect3DVertexBuffer8::getReal2");
			return m_pD3D;
		}

		/*STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DVertexBuffer8::Lock");
			HRESULT hr = m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);

			return hr;
		}

		/*STDMETHOD(Unlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Unlock()
		{
			LOG("IDirect3DVertexBuffer8::Unlock");
			HRESULT hr = m_pD3D->Unlock();

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVERTEXBUFFER_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::GetDesc(D3D8Base::D3DVERTEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DVertexBuffer8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}





		D3D8Wrapper::IDirect3DIndexBuffer8::IDirect3DIndexBuffer8(D3D8Base::IDirect3DIndexBuffer8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DIndexBuffer8");
			m_pD3D = pTexture;
		}

		D3D8Base::IDirect3DIndexBuffer8* D3D8Wrapper::IDirect3DIndexBuffer8::getReal2()
		{
			LOG("IDirect3DIndexBuffer8::getReal2");
			return m_pD3D;
		}

		/*STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DIndexBuffer8::Lock");
			HRESULT hr = m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);

			return hr;
		}

		/*STDMETHOD(Unlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Unlock()
		{
			LOG("IDirect3DIndexBuffer8::Unlock");
			HRESULT hr = m_pD3D->Unlock();

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DINDEXBUFFER_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::GetDesc(D3D8Base::D3DINDEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DIndexBuffer8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}








		D3D8Wrapper::IDirect3DSurface8::IDirect3DSurface8(D3D8Base::IDirect3DSurface8* pTexture) : IDirect3DUnknown((IUnknown*) pTexture)
		{
			LOG("IDirect3DSurface8");
			LOG(this);
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DSurface8* D3D8Wrapper::IDirect3DSurface8::GetSurface(D3D8Base::IDirect3DSurface8* pSurface)
		{
			LOG("GetSurface");
			LOG(pSurface);
			D3D8Wrapper::IDirect3DSurface8* p = (D3D8Wrapper::IDirect3DSurface8*) m_List.GetDataPtr(pSurface);
			if(p == NULL)
			{
				p = new IDirect3DSurface8(pSurface);
				m_List.AddMember(pSurface, p);
				return p;
			}
    
			p->m_ulRef++;
			return p;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DSurface8::Release(THIS)
		{
			LOG("IDirect3DSurface8::Release");
			LOG(this);
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetSurface8());
				delete this;
				return 0;
			}
			return ulRef;
		}

		/*STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DSurface8::GetDevice");

			D3D8Base::IDirect3DDevice8* fd = NULL;

			HRESULT hr = m_pD3D->GetDevice(&fd);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* f = new D3D8Wrapper::IDirect3DDevice8(fd);

			*ppDevice = f;

			return hr;
		}

		/*STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DSurface8::SetPrivateData");
			HRESULT hr = m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);

			return hr;
		}

		/*STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DSurface8::GetPrivateData");
			HRESULT hr = m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DSurface8::FreePrivateData");
			HRESULT hr = m_pD3D->FreePrivateData(refguid);

			return hr;
		}

		/*STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DSurface8::GetContainer");
			HRESULT hr = m_pD3D->GetContainer(riid,ppContainer);

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDesc(D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DSurface8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::LockRect(D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DSurface8::LockRect");
			HRESULT hr = m_pD3D->LockRect(pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::UnlockRect()
		{
			LOG("IDirect3DSurface8::UnlockRect");
			HRESULT hr = m_pD3D->UnlockRect();

			return hr;
		}







		D3D8Wrapper::IDirect3DVolume8::IDirect3DVolume8(D3D8Base::IDirect3DVolume8* pTexture) : IDirect3DUnknown((IUnknown*) pTexture)
		{
			LOG("IDirect3DVolume8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DVolume8::GetDevice");

			D3D8Base::IDirect3DDevice8* fd = NULL;

			HRESULT hr = m_pD3D->GetDevice(&fd);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* f = new D3D8Wrapper::IDirect3DDevice8(fd);

			*ppDevice = f;

			return hr;
		}

		/*STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DVolume8::SetPrivateData");
			HRESULT hr = m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);

			return hr;
		}

		/*STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DVolume8::GetPrivateData");
			HRESULT hr = m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DVolume8::FreePrivateData");
			HRESULT hr = m_pD3D->FreePrivateData(refguid);

			return hr;
		}

		/*STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DVolume8::GetContainer");
			HRESULT hr = m_pD3D->GetContainer(riid,ppContainer);

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVOLUME_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDesc(D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolume8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}

		/*STDMETHOD(LockBox)(THIS_ D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::LockBox(D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolume8::LockBox");
			HRESULT hr = m_pD3D->LockBox(pLockedVolume,pBox,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockBox)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::UnlockBox()
		{
			LOG("IDirect3DVolume8::UnlockBox");
			HRESULT hr = m_pD3D->UnlockBox();

			return hr;
		}

	}





	
	__declspec(dllexport) void __cdecl SetRenderingCallback(void (*callback)(int))
	{
		rendering_callback = callback;
	}

	__declspec(dllexport) void __cdecl ReadScreen(void *dest, int *width, int *height)
	{
		if (last_device == NULL)
		{
			*width = 0;
			*height = 0;
			return;
		}

		// get back buffer (surface)
		D3D8Base::IDirect3DSurface8 *backbuffer;
		last_device->GetD3D8Device()->GetBackBuffer(0,D3D8Base::D3DBACKBUFFER_TYPE_MONO,&backbuffer);

		// surface...
		// make a D3DSURFACE_DESC, pass to GetDesc
		D3D8Base::D3DSURFACE_DESC desc;
		backbuffer->GetDesc(&desc);

		// get out height/width
		*width = desc.Width;
		*height = desc.Height;

		// if dest isn't null
		if (dest != NULL)
		{
			// make a RECT with size of buffer
			RECT entire_buffer;
			entire_buffer.left = 0;
			entire_buffer.top = 0;
			entire_buffer.right = desc.Width;
			entire_buffer.bottom = desc.Height;
		
			// make a D3DLOCKED_RECT, pass to LockRect
			D3D8Base::D3DLOCKED_RECT locked;
			backbuffer->LockRect(&locked,&entire_buffer,D3DLOCK_READONLY);

			// read out pBits from the LOCKED_RECT
			int from_row = desc.Height - 1;
			for (int dest_row = 0; dest_row < desc.Height; dest_row++)
			{
				for (int col = 0; col < desc.Width*4; col++)
				{
					((char *)dest)[dest_row * desc.Width * 4 + col] = ((char *)locked.pBits)[from_row * desc.Width * 4 + col];
				}
				from_row--;
			}

			// unlock rect
			backbuffer->UnlockRect();
		}

		// release the surface
		backbuffer->Release();
		
		// we're done, maybe?
	}
}