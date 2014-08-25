#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet D3D8Wrapper::IDirect3DResource8::m_List;

		D3D8Wrapper::IDirect3DResource8::IDirect3DResource8(D3D8Base::IDirect3DResource8* realResource) : IDirect3DUnknown((IUnknown*) realResource)
		{
			LOG("IDirect3DResource8::IDirect3DResource8( " << realResource << " )\n");
			m_pD3D = realResource;
		}

		D3D8Wrapper::IDirect3DResource8* D3D8Wrapper::IDirect3DResource8::GetResource(D3D8Base::IDirect3DResource8* realResource)
		{
			LOG("IDirect3DResource8::GetResource( " << realResource << " )\n");
			D3D8Wrapper::IDirect3DResource8* wrappedResource = (D3D8Wrapper::IDirect3DResource8*) m_List.GetDataPtr(realResource);
			if( wrappedResource == NULL )
			{
				wrappedResource = new D3D8Wrapper::IDirect3DResource8(realResource);
				m_List.AddMember(realResource, wrappedResource);
				return wrappedResource;
			}
    
			wrappedResource->m_ulRef++;
			return wrappedResource;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DResource8::Release(THIS)
		{
			LOG("IDirect3DResource8::Release() [ " << this << " ]\n");
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

		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DResource8::GetDevice( " << ppDevice << " ) [ " << this << " ]\n");
			D3D8Base::IDirect3DDevice8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetDevice(&realD3D);

			D3D8Wrapper::IDirect3DDevice8* wrappedD3D = new D3D8Wrapper::IDirect3DDevice8(realD3D);
			
			*ppDevice = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DResource8::SetPrivateData( " << &refguid << " , " << pData << " , " << SizeOfData << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DResource8::GetPrivateData( " << &refguid << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DResource8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DResource8::FreePrivateData( " << &refguid << " ) [ " << this << " ]\n");
			return m_pD3D->FreePrivateData(refguid);
		}

		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DResource8::SetPriority(DWORD PriorityNew)
		{
			LOG("IDirect3DResource8::SetPriority( " << PriorityNew << " ) [ " << this << " ]\n");
			return m_pD3D->SetPriority(PriorityNew);
		}

		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DResource8::GetPriority()
		{
			LOG("IDirect3DResource8::GetPriority() [ " << this << " ]\n");
			return m_pD3D->GetPriority();
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DResource8::PreLoad()
		{
			LOG("IDirect3DResource8::PreLoad() [ " << this << " ]\n");
			return m_pD3D->PreLoad();
		}

		STDMETHODIMP_(D3D8Base::D3DRESOURCETYPE) D3D8Wrapper::IDirect3DResource8::GetType()
		{
			LOG("IDirect3DResource8::GetType() [ " << this << " ]\n");
			return m_pD3D->GetType();
		}
	}
}