#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DSurface8::IDirect3DSurface8(D3D8Base::IDirect3DSurface8* pTexture) : IDirect3DUnknown((IUnknown*) pTexture)
		{
			LOG("IDirect3DSurface8::IDirect3DSurface8( " << pTexture << " )\n");
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DSurface8* D3D8Wrapper::IDirect3DSurface8::GetSurface(D3D8Base::IDirect3DSurface8* pSurface)
		{
			LOG("IDirect3DSurface8::GetSurface( " << pSurface << " )\n");
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
			LOG("IDirect3DSurface8::Release( " << this << " )\n");
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetSurface());
				delete this;
				return 0;
			}
			return ulRef;
		}

		/*STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DSurface8::GetDevice( " << ppDevice << " )\n");

			D3D8Base::IDirect3DDevice8* fd = NULL;

			HRESULT hr = m_pD3D->GetDevice(&fd);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* f = new D3D8Wrapper::IDirect3DDevice8(fd);

			*ppDevice = f;

			return hr;
		}

		/*STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DSurface8::SetPrivateData( " << &refguid << " , " << pData << " , " << SizeOfData << " , " << Flags << " )\n");
			HRESULT hr = m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);

			return hr;
		}

		/*STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DSurface8::GetPrivateData( " << &refguid << " , " << pData << " , " << pSizeOfData << " )\n");
			HRESULT hr = m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DSurface8::FreePrivateData( " << &refguid << " )\n");
			HRESULT hr = m_pD3D->FreePrivateData(refguid);

			return hr;
		}

		/*STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DSurface8::GetContainer( " << &riid << " , " << ppContainer << " )\n");
			HRESULT hr = m_pD3D->GetContainer(riid,ppContainer);

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDesc(D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DSurface8::GetDesc( " << pDesc << " )\n");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::LockRect(D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
#ifdef LOGGING
			LOG("IDirect3DSurface8::LockRect( " << pLockedRect << " , " << pRect);
			if (pRect != NULL)
			{
				LOG("{ " << pRect->left << " , " << pRect->top << " , " << pRect->right << " , " << pRect->bottom << " }");		
			}
			LOG(" , " << Flags << " )\n");
#endif
			HRESULT hr = m_pD3D->LockRect(pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::UnlockRect()
		{
			LOG("IDirect3DSurface8::UnlockRect()\n");
			HRESULT hr = m_pD3D->UnlockRect();

			return hr;
		}
	}
}