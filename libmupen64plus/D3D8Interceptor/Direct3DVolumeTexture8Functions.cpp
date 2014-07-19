#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVolumeTexture8::IDirect3DVolumeTexture8(D3D8Base::IDirect3DVolumeTexture8* pTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolumeTexture8::GetLevelDesc");
			HRESULT hr = m_pD3D->GetLevelDesc(Level,pDesc);

			return hr;
		}

		/*STDMETHOD(GetVolumeLevel)(THIS_ UINT Level,IDirect3DVolume8** ppVolumeLevel) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetVolumeLevel(UINT Level,D3D8Wrapper::IDirect3DVolume8** ppVolumeLevel)
		{
			LOG("IDirect3DVolumeTexture8::GetVolumeLevel");

			D3D8Base::IDirect3DVolume8* fd = NULL;

			HRESULT hr = m_pD3D->GetVolumeLevel(Level,&fd);//ppVolumeLevel);

			D3D8Wrapper::IDirect3DVolume8* f = new D3D8Wrapper::IDirect3DVolume8(fd);

			*ppVolumeLevel = f;

			return hr;
		}

		/*STDMETHOD(LockBox)(THIS_ UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::LockBox(UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolumeTexture8::LockBox");
			HRESULT hr = m_pD3D->LockBox(Level,pLockedVolume,pBox,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockBox)(THIS_ UINT Level) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::UnlockBox(UINT Level)
		{
			LOG("IDirect3DVolumeTexture8::UnlockBox");
			HRESULT hr = m_pD3D->UnlockBox(Level);

			return hr;
		}

		/*STDMETHOD(AddDirtyBox)(THIS_ CONST D3D8Base::D3DBOX* pDirtyBox) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::AddDirtyBox(CONST D3D8Base::D3DBOX* pDirtyBox)
		{
			LOG("IDirect3DVolumeTexture8::AddDirtyBox");
			HRESULT hr = m_pD3D->AddDirtyBox(pDirtyBox);

			return hr;
		}
	}
}