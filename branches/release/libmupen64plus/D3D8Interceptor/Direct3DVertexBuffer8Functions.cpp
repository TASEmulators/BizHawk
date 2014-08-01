#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVertexBuffer8::IDirect3DVertexBuffer8(D3D8Base::IDirect3DVertexBuffer8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DVertexBuffer8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DVertexBuffer8::Lock");
			HRESULT hr = m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);

			return hr;
		}

		/*STDMETHOD(Unlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Unlock()
		{
			LOG("IDirect3DVertexBuffer8::Unlock");
			HRESULT hr = m_pD3D->Unlock();

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVERTEXBUFFER_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::GetDesc(D3D8Base::D3DVERTEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DVertexBuffer8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}
	}
}