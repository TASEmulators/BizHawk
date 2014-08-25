#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet IDirect3DSwapChain8::m_List;

		D3D8Wrapper::IDirect3DSwapChain8::IDirect3DSwapChain8(D3D8Base::IDirect3DSwapChain8* realSwapChain) : IDirect3DUnknown((IUnknown*) realSwapChain)
		{
			LOG("IDirect3DSwapChain8::IDirect3DSwapChain8( " << realSwapChain << " )\n");
			m_pD3D = realSwapChain;
		}

		D3D8Wrapper::IDirect3DSwapChain8* D3D8Wrapper::IDirect3DSwapChain8::GetSwapChain(D3D8Base::IDirect3DSwapChain8* realSwapChain)
		{
			LOG("IDirect3DSwapChain8::GetSwapChain( " << realSwapChain << " )\n");
			D3D8Wrapper::IDirect3DSwapChain8* wrappedSwapChain = (D3D8Wrapper::IDirect3DSwapChain8*) m_List.GetDataPtr(realSwapChain);
			if( wrappedSwapChain == NULL )
			{
				wrappedSwapChain = new D3D8Wrapper::IDirect3DSwapChain8(realSwapChain);
				m_List.AddMember(realSwapChain, wrappedSwapChain);
				return wrappedSwapChain;
			}
    
			wrappedSwapChain->m_ulRef++;
			return wrappedSwapChain;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DSwapChain8::Release(THIS)
		{
			LOG("IDirect3DSwapChain8::Release()n[ " << this << " ]\n");
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
			LOG("IDirect3DSwapChain8::Present( " << pSourceRect << " , " << pDestRect << " , " << hDestWindowOverride << " , " << pDirtyRegion << " ) [ " << this << " ]\n");
			return m_pD3D->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSwapChain8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("IDirect3DSwapChain8::GetBackBuffer( " << BackBuffer << " , " << Type << " , " << ppBackBuffer << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetBackBuffer(BackBuffer,Type,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppBackBuffer = wrappedD3D;

			return hr;
		}
	}
}