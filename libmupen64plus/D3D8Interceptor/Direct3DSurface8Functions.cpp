#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet IDirect3DSurface8::m_List;

		D3D8Wrapper::IDirect3DSurface8::IDirect3DSurface8(D3D8Base::IDirect3DSurface8* realSurface) : IDirect3DUnknown((IUnknown*) realSurface)
		{
			LOG("IDirect3DSurface8::IDirect3DSurface8( " << realSurface << " )\n");
			m_pD3D = realSurface;
		}

		D3D8Wrapper::IDirect3DSurface8* D3D8Wrapper::IDirect3DSurface8::GetSurface(D3D8Base::IDirect3DSurface8* realSurface)
		{
			LOG("IDirect3DSurface8::GetSurface( " << realSurface << " )\n");
			D3D8Wrapper::IDirect3DSurface8* wrappedSurface = (D3D8Wrapper::IDirect3DSurface8*) m_List.GetDataPtr(realSurface);
			if(wrappedSurface == NULL)
			{
				wrappedSurface = new IDirect3DSurface8(realSurface);
				m_List.AddMember(realSurface, wrappedSurface);
				return wrappedSurface;
			}
    
			wrappedSurface->m_ulRef++;
			return wrappedSurface;
		}

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DSurface8::Release(THIS)
		{
			LOG("IDirect3DSurface8::Release() [ " << this << " ]\n");
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;
			if(ulRef == 0)
			{
				m_List.DeleteMember(GetSurface());
				delete this;
				return 0;
			}
			return ulRef;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DSurface8::GetDevice( " << ppDevice << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DDevice8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetDevice(&realD3D);

			D3D8Wrapper::IDirect3DDevice8* wrappedD3D = new D3D8Wrapper::IDirect3DDevice8(realD3D);

			*ppDevice = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DSurface8::SetPrivateData( " << &refguid << " , " << pData << " , " << SizeOfData << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DSurface8::GetPrivateData( " << &refguid << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DSurface8::FreePrivateData( " << &refguid << " ) [ " << this << " ]\n");
			return m_pD3D->FreePrivateData(refguid);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DSurface8::GetContainer( " << &riid << " , " << ppContainer << " ) [ " << this << " ]\n");
			return m_pD3D->GetContainer(riid,ppContainer);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::GetDesc(D3D8Base::D3DSURFACE_DESC *pDesc)
		{
			LOG("IDirect3DSurface8::GetDesc( " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetDesc(pDesc);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::LockRect(D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags)
		{
#ifdef LOGGING
			LOG("IDirect3DSurface8::LockRect( " << pLockedRect << " , " << pRect);
			if (pRect != NULL)
			{
				LOG("{ " << pRect->left << " , " << pRect->top << " , " << pRect->right << " , " << pRect->bottom << " }");		
			}
			LOG(" , " << Flags << " ) [ " << this << " ]\n");
#endif
			return m_pD3D->LockRect(pLockedRect,pRect,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DSurface8::UnlockRect()
		{
			LOG("IDirect3DSurface8::UnlockRect() [ " << this << " ]\n");
			return m_pD3D->UnlockRect();
		}
	}
}