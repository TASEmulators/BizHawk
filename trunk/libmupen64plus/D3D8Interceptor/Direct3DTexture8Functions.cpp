#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
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
	}
}