#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DCubeTexture8::IDirect3DCubeTexture8(D3D8Base::IDirect3DCubeTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DCubeTexture8( " << pTexture << " )\n");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DCubeTexture8::GetLevelDesc( " << pDesc << " )\n");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetCubeMapSurface)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetCubeMapSurface(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface)
		{
			LOG("IDirect3DCubeTexture8::GetCubeMapSurface( " << FaceType << " , " << Level << " , " << ppCubeMapSurface << " )\n");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetCubeMapSurface(FaceType,Level,&fd);//ppCubeMapSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppCubeMapSurface = f;

			return hr;
		}

		/*STDMETHOD(LockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::LockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DCubeTexture8::LockRect( " << FaceType << " , " << Level << " , " << pLockedRect << " , " << pRect << " , " << Flags << " )\n");
			HRESULT hr = m_pD3D->LockRect(FaceType,Level,pLockedRect,pRect,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::UnlockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level)
		{
			LOG("IDirect3DCubeTexture8::UnlockRect( " << FaceType << " , " << Level << " )\n");
			HRESULT hr = m_pD3D->UnlockRect(FaceType,Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::AddDirtyRect(D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DCubeTexture8::AddDirtyRect( " << FaceType << " , " << pDirtyRect << " )\n");
			HRESULT hr = m_pD3D->AddDirtyRect(FaceType,pDirtyRect);

			return hr;
		}
	}
}