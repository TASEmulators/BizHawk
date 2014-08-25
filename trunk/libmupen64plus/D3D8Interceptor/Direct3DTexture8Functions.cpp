#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DTexture8::IDirect3DTexture8(D3D8Base::IDirect3DTexture8* realTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) realTexture)
		{
			LOG("IDirect3DTexture8::IDirect3DTexture8( " << realTexture << " )\n");
			m_pD3D = realTexture;
		}

		D3D8Wrapper::IDirect3DTexture8* D3D8Wrapper::IDirect3DTexture8::GetTexture(D3D8Base::IDirect3DTexture8* realTexture)
		{
			LOG("IDirect3DTexture8::GetTexture( " << realTexture << " )\n");
			D3D8Wrapper::IDirect3DTexture8* wrappedTexture = (D3D8Wrapper::IDirect3DTexture8*) D3D8Wrapper::IDirect3DResource8::m_List.GetDataPtr(realTexture);
			if( wrappedTexture == NULL )
			{
				wrappedTexture = new D3D8Wrapper::IDirect3DTexture8(realTexture);
				D3D8Wrapper::IDirect3DResource8::m_List.AddMember(realTexture, wrappedTexture);
				return wrappedTexture;
			}
    
			wrappedTexture->m_ulRef++;
			return wrappedTexture;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DTexture8::GetLevelDesc( " << Level << " , " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetLevelDesc(Level,pDesc);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::GetSurfaceLevel(UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel)
		{
			LOG("IDirect3DTexture8::GetSurfaceLevel( " << Level << " , " << ppSurfaceLevel << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetSurfaceLevel(Level,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurfaceLevel = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::LockRect(UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DTexture8::LockRect( " << Level << " , " << pLockedRect << " , " << pRect << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->LockRect(Level,pLockedRect,pRect,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::UnlockRect(UINT Level)
		{
			LOG("IDirect3DTexture8::UnlockRect() [ " << this << " ]\n");
			return m_pD3D->UnlockRect(Level);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DTexture8::AddDirtyRect(CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DTexture8::AddDirtyRect( " << pDirtyRect << " ) [ " << this << " ]\n");
			return m_pD3D->AddDirtyRect(pDirtyRect);
		}
	}
}