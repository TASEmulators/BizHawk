#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Base::IDirect3DBaseTexture8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8 -- 1");
			m_pD3D = pTexture;
		}

		D3D8Wrapper::IDirect3DBaseTexture8::IDirect3DBaseTexture8(D3D8Wrapper::IDirect3DBaseTexture8* pTexture) : IDirect3DResource8((D3D8Wrapper::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DBaseTexture8 -- 2");
			m_pD3D = pTexture->getReal2();
		}

		D3D8Base::IDirect3DBaseTexture8* D3D8Wrapper::IDirect3DBaseTexture8::getReal2()
		{
			LOG("IDirect3DBaseTexture8::getReal2");
			return m_pD3D;
		}


		/*STDMETHOD_(DWORD, SetLOD)(THIS_ DWORD LODNew) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::SetLOD(DWORD LODNew)
		{
			LOG("IDirect3DBaseTexture8::SetLOD");
			return m_pD3D->SetLOD(LODNew);
		}

		/*STDMETHOD_(DWORD, GetLOD)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLOD()
		{
			LOG("IDirect3DBaseTexture8::GetLOD");
			return m_pD3D->GetLOD();
		}

		/*STDMETHOD_(DWORD, GetLevelCount)(THIS) PURE;*/
		STDMETHODIMP_(DWORD) D3D8Wrapper::IDirect3DBaseTexture8::GetLevelCount()
		{
			LOG("IDirect3DBaseTexture8::GetLevelCount");
			return m_pD3D->GetLevelCount();
		}
	}
}