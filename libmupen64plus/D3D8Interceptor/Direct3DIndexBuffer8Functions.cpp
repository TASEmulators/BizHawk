#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DIndexBuffer8::IDirect3DIndexBuffer8(D3D8Base::IDirect3DIndexBuffer8* realIndexBuffer) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) realIndexBuffer)
		{
			LOG("IDirect3DIndexBuffer8( " << realIndexBuffer << " )\n");
			m_pD3D = realIndexBuffer;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DIndexBuffer8::Lock( " << OffsetToLock << " , " << SizeToLock << " , " << ppbData << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::Unlock()
		{
			LOG("IDirect3DIndexBuffer8::Unlock() [ " << this << " ]\n");
			return m_pD3D->Unlock();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DIndexBuffer8::GetDesc(D3D8Base::D3DINDEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DIndexBuffer8::GetDesc( " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetDesc(pDesc);
		}
	}
}