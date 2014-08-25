#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVolumeTexture8::IDirect3DVolumeTexture8(D3D8Base::IDirect3DVolumeTexture8* realVolumeTexture) : IDirect3DBaseTexture8((D3D8Base::IDirect3DBaseTexture8*) realVolumeTexture)
		{
			LOG("IDirect3DVolumeTexture8( " << realVolumeTexture << " )\n");
			m_pD3D = realVolumeTexture;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetLevelDesc(UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolumeTexture8::GetLevelDesc( " << Level << " , " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetLevelDesc(Level,pDesc);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::GetVolumeLevel(UINT Level,D3D8Wrapper::IDirect3DVolume8** ppVolumeLevel)
		{
			LOG("IDirect3DVolumeTexture8::GetVolumeLevel( " << Level << " , " << ppVolumeLevel << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DVolume8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetVolumeLevel(Level,&realD3D);

			D3D8Wrapper::IDirect3DVolume8* wrappedD3D = new D3D8Wrapper::IDirect3DVolume8(realD3D);

			*ppVolumeLevel = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::LockBox(UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolumeTexture8::LockBox( " << Level << " , " << pLockedVolume << " , " << pBox << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->LockBox(Level,pLockedVolume,pBox,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::UnlockBox(UINT Level)
		{
			LOG("IDirect3DVolumeTexture8::UnlockBox( " << Level << " ) [ " << this << " ]\n");
			return m_pD3D->UnlockBox(Level);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolumeTexture8::AddDirtyBox(CONST D3D8Base::D3DBOX* pDirtyBox)
		{
			LOG("IDirect3DVolumeTexture8::AddDirtyBox( " << pDirtyBox << " ) [ " << this << " ]\n");
			return m_pD3D->AddDirtyBox(pDirtyBox);
		}
	}
}