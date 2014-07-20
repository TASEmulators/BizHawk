#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet D3D8Wrapper::IDirect3DTexture8::m_List;

		D3D8Wrapper::IDirect3DTexture8::IDirect3DTexture8(D3D8Base::IDirect3DTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DTexture8");
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DTexture8* D3D8Wrapper::IDirect3DTexture8::GetTexture(D3D8Base::IDirect3DTexture8* pTexture)
		{
			D3D8Wrapper::IDirect3DTexture8* p = (D3D8Wrapper::IDirect3DTexture8*) m_List.GetDataPtr(pTexture);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3DTexture8(pTexture);
				LOG("IDirect3DTexture8::GetTexture " << pTexture << " created new " << p)
				m_List.AddMember(pTexture, p);
				return p;
			}
    
			p->m_ulRef++;
			LOG("IDirect3DTexture8::GetTexture " << pTexture << " found existing " << p)
			return p;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DTexture8::Release(THIS)
		{
			LOG("IDirect3DTexture8::Release " << this);
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
	}
}