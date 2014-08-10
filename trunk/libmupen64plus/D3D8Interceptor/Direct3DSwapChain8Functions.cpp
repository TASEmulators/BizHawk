#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DSwapChain8::IDirect3DSwapChain8(D3D8Base::IDirect3DSwapChain8* pSwapChain) : IDirect3DUnknown((IUnknown*) pSwapChain)
		{
			LOG("IDirect3DSwapChain8::IDirect3DSwapChain8( " << pSwapChain << " )\n");
			m_pD3D = pSwapChain;
		}

		D3D8Wrapper::IDirect3DSwapChain8* D3D8Wrapper::IDirect3DSwapChain8::GetSwapChain(D3D8Base::IDirect3DSwapChain8* pSwapChain)
		{
			LOG("IDirect3DSwapChain8::GetSwapChain( " << pSwapChain << " )\n");
			D3D8Wrapper::IDirect3DSwapChain8* p = (D3D8Wrapper::IDirect3DSwapChain8*) m_List.GetDataPtr(pSwapChain);
			if( p == NULL )
			{
				p = new D3D8Wrapper::IDirect3DSwapChain8(pSwapChain);
				m_List.AddMember(pSwapChain, p);
				return p;
			}
    
			p->m_ulRef++;
			return p;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DSwapChain8::Release(THIS)
		{
			LOG("IDirect3DSwapChain8::Release( " << this << " )\n");
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetSwapChain8());
				delete this;
				return 0;
			}
			return ulRef;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSwapChain8::Present(CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion)
		{
			LOG("IDirect3DSwapChain8::Present( " << pSourceRect << " , " << pDestRect << " , " << hDestWindowOverride << " , " << pDirtyRegion << " )\n");
			HRESULT hr = m_pD3D->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSwapChain8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("IDirect3DSwapChain8::GetBackBuffer( " << BackBuffer << " , " << Type << " , " << ppBackBuffer << " )\n");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pD3D->GetBackBuffer(BackBuffer,Type,&fd);//ppBackBuffer);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppBackBuffer = f;

			return hr;
		}
	}
}