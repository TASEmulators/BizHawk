#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVolume8::IDirect3DVolume8(D3D8Base::IDirect3DVolume8* realVolume) : IDirect3DUnknown((IUnknown*) realVolume)
		{
			LOG("IDirect3DVolume8::IDirect3DVolume8( " << realVolume << " )\n");
			m_pD3D = realVolume;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DVolume8::GetDevice( " << ppDevice << " )\n");

			D3D8Base::IDirect3DDevice8* realD3D = NULL;

			HRESULT hr = m_pD3D->GetDevice(&realD3D);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* wrappedD3D = new D3D8Wrapper::IDirect3DDevice8(realD3D);

			*ppDevice = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DVolume8::SetPrivateData( " << &refguid << " , " << pData << " , " << SizeOfData << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DVolume8::GetPrivateData( " << &refguid << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DVolume8::FreePrivateData( " << &refguid << " ) [ " << this << " ]\n");
			return m_pD3D->FreePrivateData(refguid);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DVolume8::GetContainer( " << &riid << " , " << ppContainer << " ) [ " << this << " ]\n");
			return m_pD3D->GetContainer(riid,ppContainer);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDesc(D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolume8::GetDesc( " << pDesc << " ) [ " << this << " ]\n");
			return m_pD3D->GetDesc(pDesc);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::LockBox(D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolume8::LockBox( " << pLockedVolume << " , " << pBox << " , " << Flags << " ) [ " << this << " ]\n");
			return m_pD3D->LockBox(pLockedVolume,pBox,Flags);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::UnlockBox()
		{
			LOG("IDirect3DVolume8::UnlockBox() [ " << this << " ]\n");
			return m_pD3D->UnlockBox();
		}
	}
}