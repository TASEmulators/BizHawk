#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Base::IDirect3DBaseTexture8* realBaseTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) realBaseTexture)
		{
			LOG("IDirect3DBaseTexture8::IDirect3DBaseTexture8( " << realBaseTexture << " )\n");
			m_pD3D = realBaseTexture;
		}

		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::SetLOD(DWORD LODNew)
		{
			LOG("IDirect3DBaseTexture8::SetLOD( " << LODNew << " ) [ " << this << " ]\n");
			return m_pD3D->SetLOD(LODNew);
		}

		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLOD()
		{
			LOG("IDirect3DBaseTexture8::GetLOD() [ " << this << " ]\n");
			return m_pD3D->GetLOD();
		}

		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLevelCount()
		{
			LOG("IDirect3DBaseTexture8::GetLevelCount() [ " << this << " ]\n");
			return m_pD3D->GetLevelCount();
		}
	}
}