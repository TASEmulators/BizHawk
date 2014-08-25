#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVertexBuffer8::IDirect3DVertexBuffer8(D3D8Base::IDirect3DVertexBuffer8* realVertexBuffer) : IDirect3DResource8((D3D8Base::IDirect3DResource8*) realVertexBuffer)
		{
			LOG("IDirect3DVertexBuffer8::IDirect3DVertexBuffer8( " << realVertexBuffer << " )\n");
			m_pD3D = realVertexBuffer;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Lock(UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags)
		{
			LOG("IDirect3DVertexBuffer8::Lock( " << OffsetToLock << " , " << SizeToLock << " , " << ppbData << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->Lock(OffsetToLock,SizeToLock,ppbData,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::Unlock()
		{
			LOG("IDirect3DVertexBuffer8::Unlock() [ " << this << " ]\n");
			return m_pD3D->Unlock();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVertexBuffer8::GetDesc(D3D8Base::D3DVERTEXBUFFER_DESC *pDesc)
		{
			LOG("IDirect3DVertexBuffer8::GetDesc( " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetDesc(pDesc);
		}
	}
}