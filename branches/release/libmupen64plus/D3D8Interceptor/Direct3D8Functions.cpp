#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet IDirect3D8::m_List;

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

	}
}