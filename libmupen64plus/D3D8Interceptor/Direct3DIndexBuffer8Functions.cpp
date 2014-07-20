#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DIndexBuffer8::IDirect3DIndexBuffer8(D3D8Base::IDirect3DIndexBuffer8* pTexture) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) pTexture)
		{
			LOG("IDirect3DIndexBuffer8");
			m_pD3D = pTexture;
		}

		D3D8Base::IDirect3DIndexBuffer8* D3D8Wrapper::IDirect3DIndexBuffer8::getReal2()
		{
			LOG("IDirect3DIndexBuffer8::getReal2");
			return m_pD3D;
		}

		/*STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DIndexBuffer8::Lock");
			HRESULT hr = m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);

			return hr;
		}

		/*STDMETHOD(Unlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Unlock()
		{
			LOG("IDirect3DIndexBuffer8::Unlock");
			HRESULT hr = m_pD3D->Unlock();

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DINDEXBUFFER_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::GetDesc(D3D8Base::D3DINDEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DIndexBuffer8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}
	}
}