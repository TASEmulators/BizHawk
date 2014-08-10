#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Base::IDirect3DBaseTexture8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8::IDirect3DBaseTexture8( " << pTexture << " )\n");
			m_pD3D = pTexture;
		}

		/*STDMETHOD_(DWORD, SetLOD)(THIS_ DWORD LODNew) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::SetLOD(DWORD LODNew)
		{
			LOG("IDirect3DBaseTexture8::SetLOD( " << LODNew << " )\n");
			return m_pD3D->SetLOD(LODNew);
		}

		/*STDMETHOD_(DWORD, GetLOD)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLOD()
		{
			LOG("IDirect3DBaseTexture8::GetLOD()\n");
			return m_pD3D->GetLOD();
		}

		/*STDMETHOD_(DWORD, GetLevelCount)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLevelCount()
		{
			LOG("IDirect3DBaseTexture8::GetLevelCount()\n");
			return m_pD3D->GetLevelCount();
		}
	}
}