#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DVolume8::IDirect3DVolume8(D3D8Base::IDirect3DVolume8* pTexture) : IDirect3DUnknown((IUnknown*) pTexture)
		{
			LOG("IDirect3DVolume8");
			m_pD3D = pTexture;
		}

		/*STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDevice(D3D8Wrapper::IDirect3DDevice8** ppDevice)
		{
			LOG("IDirect3DVolume8::GetDevice");

			D3D8Base::IDirect3DDevice8* fd = NULL;

			HRESULT hr = m_pD3D->GetDevice(&fd);//ppDevice);

			D3D8Wrapper::IDirect3DDevice8* f = new D3D8Wrapper::IDirect3DDevice8(fd);

			*ppDevice = f;

			return hr;
		}

		/*STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::SetPrivateData(REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags)
		{
			LOG("IDirect3DVolume8::SetPrivateData");
			HRESULT hr = m_pD3D->SetPrivateData(refguid,pData,SizeOfData,Flags);

			return hr;
		}

		/*STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetPrivateData(REFGUID refguid,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DVolume8::GetPrivateData");
			HRESULT hr = m_pD3D->GetPrivateData(refguid,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::FreePrivateData(REFGUID refguid)
		{
			LOG("IDirect3DVolume8::FreePrivateData");
			HRESULT hr = m_pD3D->FreePrivateData(refguid);

			return hr;
		}

		/*STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetContainer(REFIID riid,void** ppContainer)
		{
			LOG("IDirect3DVolume8::GetContainer");
			HRESULT hr = m_pD3D->GetContainer(riid,ppContainer);

			return hr;
		}

		/*STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVOLUME_DESC *pDesc) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::GetDesc(D3D8Base::D3DVOLUME_DESC *pDesc)
		{
			LOG("IDirect3DVolume8::GetDesc");
			HRESULT hr = m_pD3D->GetDesc(pDesc);

			return hr;
		}

		/*STDMETHOD(LockBox)(THIS_ D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::LockBox(D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags)
		{
			LOG("IDirect3DVolume8::LockBox");
			HRESULT hr = m_pD3D->LockBox(pLockedVolume,pBox,Flags);

			return hr;
		}

		/*STDMETHOD(UnlockBox)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DVolume8::UnlockBox()
		{
			LOG("IDirect3DVolume8::UnlockBox");
			HRESULT hr = m_pD3D->UnlockBox();

			return hr;
		}
	}
}