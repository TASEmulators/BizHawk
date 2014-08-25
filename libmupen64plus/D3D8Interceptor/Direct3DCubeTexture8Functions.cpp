#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DCubeTexture8::IDirect3DCubeTexture8(D3D8Base::IDirect3DCubeTexture8* realCubeTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) realCubeTexture)
		{
			LOG("IDirect3DCubeTexture8( " << realCubeTexture << " )\n");
			m_pD3D = realCubeTexture;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DCubeTexture8::GetLevelDesc( " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetLevelDesc(Level,pDesc);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::GetCubeMapSurface(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface)
		{
			LOG("IDirect3DCubeTexture8::GetCubeMapSurface( " << FaceType << " , " << Level << " , " << ppCubeMapSurface << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetCubeMapSurface(FaceType,Level,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppCubeMapSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::LockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
			LOG("IDirect3DCubeTexture8::LockRect( " << FaceType << " , " << Level << " , " << pLockedRect << " , " << pRect << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->LockRect(FaceType,Level,pLockedRect,pRect,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::UnlockRect(D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level)
		{
			LOG("IDirect3DCubeTexture8::UnlockRect( " << FaceType << " , " << Level << " ) [ " << this << " ]\n");
			return m_pD3D->UnlockRect(FaceType,Level);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DCubeTexture8::AddDirtyRect(D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect)
		{
			LOG("IDirect3DCubeTexture8::AddDirtyRect( " << FaceType << " , " << pDirtyRect << " ) [ " << this << " ]\n");
			return m_pD3D->AddDirtyRect(FaceType,pDirtyRect);
		}
	}
}