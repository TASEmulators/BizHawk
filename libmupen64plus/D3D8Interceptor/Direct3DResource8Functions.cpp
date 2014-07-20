#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
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
	}
}