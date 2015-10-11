#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet IDirect3D8::m_List;

		D3D8Wrapper::IDirect3D8::IDirect3D8(D3D8Base::IDirect3D8* real) : D3D8Wrapper::IDirect3DUnknown((IUnknown*) real)
		{
			LOG("IDirect3D8::IDirect3D8( " << real << " )\n");
			m_pD3D = real;
		}

		// Tries to find the real object in the pointer set, or creates a new wrapped object
		D3D8Wrapper::IDirect3D8* D3D8Wrapper::IDirect3D8::GetDirect3D(D3D8Base::IDirect3D8* pD3D)
		{
			LOG("IDirect3D8::GetDirect3D( " << pD3D << " )\n");
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
			LOG("IDirect3D8::Release() [ " << this << " ]\n");
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


		/*** IDirect3D8 methods ***/

		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetAdapterDisplayMode(THIS_ UINT Adapter,D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("IDirect3D8::GetAdapterDisplayMode( " << Adapter << " , " << pMode << " ) [ " << this << " ]\n");
			return m_pD3D->GetAdapterDisplayMode(Adapter, pMode);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3D8::RegisterSoftwareDevice(void* pInitializeFunction)
		{
			LOG("IDirect3D8::RegisterSoftwareDevice( " << pInitializeFunction << " ) [ " << this << " ]\n");
			return m_pD3D->RegisterSoftwareDevice(pInitializeFunction);
		}
		
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3D8::GetAdapterCount(THIS)
		{
			LOG("IDirect3D8::GetAdapterCount() [ " << this << " ]\n");
			return m_pD3D->GetAdapterCount();
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetAdapterIdentifier(UINT Adapter,DWORD Flags,D3D8Base::D3DADAPTER_IDENTIFIER8* pIdentifier)
		{
			LOG("IDirect3D8::GetAdapterIdentifier( " << Adapter << " , " << Flags << " , " << pIdentifier << " ) [ " << this << " ]\n");
			return m_pD3D->GetAdapterIdentifier(Adapter,Flags,pIdentifier);
		}
		
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3D8::GetAdapterModeCount(UINT Adapter)
		{
			LOG("IDirect3D8::GetAdapterModeCount( " << Adapter << " ) [ " << this << " ]\n");
			return m_pD3D->GetAdapterModeCount(Adapter);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::EnumAdapterModes(UINT Adapter,UINT Mode,D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("IDirect3D8::EnumAdapterModes( " << Adapter << " , " << Mode << " , " << pMode << " ) [ " << this << " ]\n");
			return m_pD3D->EnumAdapterModes(Adapter,Mode,pMode);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceType(UINT Adapter,D3D8Base::D3DDEVTYPE CheckType,D3D8Base::D3DFORMAT DisplayFormat,D3D8Base::D3DFORMAT BackBufferFormat,BOOL Windowed)
		{
			LOG("IDirect3D8::CheckDeviceType( " << Adapter << " , " << CheckType << " , " << DisplayFormat << " , " << BackBufferFormat << " , " << Windowed << " ) [ " << this << " ]\n");
			return m_pD3D->CheckDeviceType(Adapter,CheckType,DisplayFormat,BackBufferFormat,Windowed);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceFormat(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,DWORD Usage,D3D8Base::D3DRESOURCETYPE RType,D3D8Base::D3DFORMAT CheckFormat)
		{
			LOG("IDirect3D8::CheckDeviceFormat( " << Adapter << " , " << DeviceType << " , " << AdapterFormat << " , " << Usage << " , " << RType << " , " << CheckFormat << " ) [ " << this << " ]\n");
			return m_pD3D->CheckDeviceFormat(Adapter,DeviceType,AdapterFormat,Usage,RType,CheckFormat);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDeviceMultiSampleType(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT SurfaceFormat,BOOL Windowed,D3D8Base::D3DMULTISAMPLE_TYPE MultiSampleType)
		{
			LOG("IDirect3D8::CheckDeviceMultiSampleType( " << Adapter << " , " << DeviceType << " , " << SurfaceFormat << " , " << Windowed << " , " << MultiSampleType << " ) [ " << this << " ]\n");
			return m_pD3D->CheckDeviceMultiSampleType(Adapter,DeviceType,SurfaceFormat,Windowed,MultiSampleType);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CheckDepthStencilMatch(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,D3D8Base::D3DFORMAT RenderTargetFormat,D3D8Base::D3DFORMAT DepthStencilFormat)
		{
			LOG("IDirect3D8::CheckDepthStencilMatch( " << Adapter << " , " << DeviceType << " , " << AdapterFormat << " , " << RenderTargetFormat << " , " << DepthStencilFormat << " ) [ " << this << " ]\n");
			return m_pD3D->CheckDepthStencilMatch(Adapter,DeviceType,AdapterFormat,RenderTargetFormat,DepthStencilFormat);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3D8::GetDeviceCaps(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("IDirect3D8::GetDeviceCaps( " << Adapter << " , " << DeviceType << " , " << pCaps << " ) [ " << this << " ]\n");
			return m_pD3D->GetDeviceCaps(Adapter,DeviceType,pCaps);
		}

		STDMETHODIMP_(HMONITOR) D3D8Wrapper::IDirect3D8::GetAdapterMonitor(UINT Adapter)
		{
			LOG("IDirect3D8::GetAdapterMonitor( " << Adapter << " ) [ " << this << " ]\n");
			return m_pD3D->GetAdapterMonitor(Adapter);
		}
		
		STDMETHODIMP D3D8Wrapper::IDirect3D8::CreateDevice(UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,HWND hFocusWindow,DWORD BehaviorFlags,D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DDevice8** ppReturnedDeviceInterface)
		{
			//sometimes, Intel drivers will clear the dll path. So let's save and restore it (do their job for them)
			//it doesn't seem like this happens any time besides creating the D3D8 object and a device.
			//If it does, then this solution isn't scalable at all.
			//This is a good place to note that it appears possible that on the affected drivers, the D3D9 interface will only SetDllDirectory the first time a D3D9 object is created
			char oldDllDirectory[MAX_PATH];
			GetDllDirectory(MAX_PATH, oldDllDirectory);

			LOG("IDirect3D8::CreateDevice( " << Adapter << " , " << DeviceType << " , " << hFocusWindow << " , " << BehaviorFlags << " , " << pPresentationParameters << " , " << ppReturnedDeviceInterface << " ) [ " << this << " ]\n");
			D3D8Base::IDirect3DDevice8* realDevice = NULL;

			HRESULT hr = m_pD3D->CreateDevice(Adapter,DeviceType,hFocusWindow,BehaviorFlags,pPresentationParameters,&realDevice);

			//restore old DLL directory
			SetDllDirectory(oldDllDirectory);

			if(FAILED(hr))
			{
				return hr;
			}

			// Wrap the real object
			D3D8Wrapper::IDirect3DDevice8* wrappedDevice = D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(realDevice);

			// Store this wrapped pointer for grabbing the screen later
			last_device = wrappedDevice;

			// Create a new render target
			D3D8Base::IDirect3DSurface8 *realSurface = NULL;
			HRESULT hr2 = realDevice->CreateRenderTarget(pPresentationParameters->BackBufferWidth,pPresentationParameters->BackBufferHeight,D3D8Base::D3DFMT_X8R8G8B8,pPresentationParameters->MultiSampleType,FALSE,&realSurface);

			// Store a wrapped pointer to it for grabbing the screen
			render_surface = D3D8Wrapper::IDirect3DSurface8::GetSurface(realSurface);

			// Return our wrapped object
			*ppReturnedDeviceInterface = wrappedDevice;

			return hr;
		} 

	}
}