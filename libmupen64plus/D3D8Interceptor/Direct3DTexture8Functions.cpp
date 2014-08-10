#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DTexture8::IDirect3DTexture8(D3D8Base::IDirect3DTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DTexture8::IDirect3DTexture8( " << pTexture << " )\n");
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DTexture8* D3D8Wrapper::IDirect3DTexture8::GetTexture(D3D8Base::IDirect3DTexture8* pTexture)
		{
			LOG("IDirect3DTexture8::GetTexture( " << pTexture << " )\n");
			D3D8Wrapper::IDirect3DTexture8* p = (D3D8Wrapper::IDirect3DTexture8*) D3D8Wrapper::IDirect3DResource8::m_List.GetDataPtr(pTexture);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3DTexture8(pTexture);
				//LOG("IDirect3DTexture8::GetTexture " << pTexture << " created new " << p << "\n")
				D3D8Wrapper::IDirect3DResource8::m_List.AddMember(pTexture, p);
				return p;
			}
    
			p->m_ulRef++;
			//LOG("IDirect3DTexture8::GetTexture " << pTexture << " found existing " << p << "\n")
			return p;
		}


		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DTexture8::GetLevelDesc( " << Level << " , " << pDesc << " )\n");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetSurfaceLevel)(THIS_ UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetSurfaceLevel(UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel)
		{
			LOG("IDirect3DTexture8::GetSurfaceLevel( " << Level << " , " << ppSurfaceLevel << " )\n");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetSurfaceLevel(Level,&fd);//ppSurfaceLevel);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurfaceLevel = f;

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::LockRect(UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DTexture8::LockRect( " << Level << " , " << pLockedRect << " , " << pRect << " , " << Flags << " )\n");
			HRESULT hr = m_pD3D->LockRect(Level,pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS_ UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::UnlockRect(UINT Level)
		{
			LOG("IDirect3DTexture8::UnlockRect()\n");
			HRESULT hr = m_pD3D->UnlockRect(Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyRect)(THIS_ CONST RECT* pDirtyRect) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::AddDirtyRect(CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DTexture8::AddDirtyRect( " << pDirtyRect << " )\n");
			HRESULT hr = m_pD3D->AddDirtyRect(pDirtyRect);

			return hr;
		}
	}
}