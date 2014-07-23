#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DDevice8::IDirect3DDevice8(D3D8Base::IDirect3DDevice8* pDevice) : IDirect3DUnknown((IUnknown*) pDevice)
		{
			LOG("IDirect3DDevice8");
			m_pDevice = pDevice;
			rTarget = NULL;
			zStencil = NULL;
		}

		D3D8Wrapper::IDirect3DDevice8* D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(D3D8Base::IDirect3DDevice8* pDevice)
		{

			D3D8Wrapper::IDirect3DDevice8* p = (D3D8Wrapper::IDirect3DDevice8*) m_List.GetDataPtr(pDevice);
			if(p == NULL)
			{
				p = new D3D8Wrapper::IDirect3DDevice8(pDevice);
				m_List.AddMember(pDevice, p);
				return p;
			}

			p->m_ulRef++;
			return p;
		} 

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DDevice8::Release(THIS)
		{
			m_pUnk->Release();

			ULONG ulRef = --m_ulRef;

			if(ulRef == 0)
			{
				m_List.DeleteMember(GetD3D8Device());
				delete this;
				return NULL;
			}
			return ulRef;
		}

		
		/*STDMETHOD(TestCooperativeLevel)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::TestCooperativeLevel()
		{
			LOG("TestCooperativeLevel");
			HRESULT hr = m_pDevice->TestCooperativeLevel();

			return hr;
		}

		/*STDMETHOD_(UINT, GetAvailableTextureMem)(THIS) PURE;*/
		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3DDevice8::GetAvailableTextureMem()
		{
			LOG("GetAvailableTextureMem");
			HRESULT hr = m_pDevice->GetAvailableTextureMem();

			return hr;
		}

		/*STDMETHOD(ResourceManagerDiscardBytes)(THIS_ DWORD Bytes) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ResourceManagerDiscardBytes(DWORD Bytes)
		{
			LOG("ResourceManagerDiscardBytes");
			HRESULT hr = m_pDevice->ResourceManagerDiscardBytes(Bytes);

			return hr;
		}

		/*STDMETHOD(GetDirect3D)(THIS_ IDirect3D8** ppD3D8) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDirect3D(D3D8Wrapper::IDirect3D8** ppD3D8)
		{
			LOG("GetDirect3D");

			D3D8Base::IDirect3D8* fd = NULL;

			HRESULT hr = m_pDevice->GetDirect3D(&fd);//ppD3D8);

			D3D8Wrapper::IDirect3D8* f = D3D8Wrapper::IDirect3D8::GetDirect3D(fd);

			*ppD3D8 = f;

			return hr;
		}

		/*STDMETHOD(GetDeviceCaps)(THIS_ D3D8Base::D3DCAPS8* pCaps) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDeviceCaps(D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("GetDeviceCaps");
			HRESULT hr = m_pDevice->GetDeviceCaps(pCaps);

			return hr;
		}

		/*STDMETHOD(GetDisplayMode)(THIS_ D3D8Base::D3DDISPLAYMODE* pMode) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDisplayMode(D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("GetDisplayMode");
			HRESULT hr = m_pDevice->GetDisplayMode(pMode);

			return hr;
		}

		/*STDMETHOD(GetCreationParameters)(THIS_ D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCreationParameters(D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters)
		{
			LOG("GetCreationParameters");
			HRESULT hr = m_pDevice->GetCreationParameters(pParameters);

			return hr;
		}

		/*STDMETHOD(SetCursorProperties)(THIS_ UINT XHotSpot,UINT YHotSpot,D3D8Base::IDirect3DSurface8* pCursorBitmap) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCursorProperties(UINT XHotSpot,UINT YHotSpot,D3D8Wrapper::IDirect3DSurface8* pCursorBitmap)
		{
			LOG("SetCursorProperties");
			HRESULT hr = m_pDevice->SetCursorProperties(XHotSpot,YHotSpot,pCursorBitmap->GetSurface());

			return hr;
		}

		/*STDMETHOD_(void, SetCursorPosition)(THIS_ int X,int Y,DWORD Flags) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetCursorPosition(int X,int Y,DWORD Flags)
		{
			LOG("SetCursorPosition");
			m_pDevice->SetCursorPosition(X,Y,Flags);
		}

		/*STDMETHOD_(BOOL, ShowCursor)(THIS_ BOOL bShow) PURE;*/
		STDMETHODIMP_(BOOL) D3D8Wrapper::IDirect3DDevice8::ShowCursor(BOOL bShow)
		{
			LOG("ShowCursor");
			HRESULT hr = m_pDevice->ShowCursor(bShow);

			return hr;
		}

		/*STDMETHOD(CreateAdditionalSwapChain)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Base::IDirect3DSwapChain8** pSwapChain) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateAdditionalSwapChain(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DSwapChain8** pSwapChain)
		{
			LOG("CreateAdditionalSwapChain");
			D3D8Base::IDirect3DSwapChain8* fd = NULL;

			HRESULT hr = m_pDevice->CreateAdditionalSwapChain(pPresentationParameters,&fd);//pSwapChain);

			D3D8Wrapper::IDirect3DSwapChain8* f = new D3D8Wrapper::IDirect3DSwapChain8(fd);
			*pSwapChain = f;

			return hr;
		}

		/*STDMETHOD(Reset)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Reset(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters)
		{
			LOG("Reset");
			HRESULT hr = m_pDevice->Reset(pPresentationParameters);

			return hr;
		}

		int present_count = 0;
		/*STDMETHOD(Present)(THIS_ CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Present(CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion)
		{
			LOG("Present");
			LOG(present_count);
			LOG(pSourceRect);
			if (pSourceRect != NULL)
			{
				LOG(pSourceRect->left);
				LOG(pSourceRect->top);
				LOG(pSourceRect->right);
				LOG(pSourceRect->bottom);
			}
			LOG(pDestRect);
			if (pSourceRect != NULL)
			{
				LOG(pDestRect->left);
				LOG(pDestRect->top);
				LOG(pDestRect->right);
				LOG(pDestRect->bottom);
			}

			LOG(hDestWindowOverride);
			LOG(pDirtyRegion);
			HRESULT hr = D3D_OK;

			rendering_callback(0);

			//hr = m_pDevice->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);

			LOG(hr);
			return hr;
		}

		/*STDMETHOD(GetBackBuffer)(THIS_ UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Base::IDirect3DSurface8** ppBackBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("GetBackBuffer");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetBackBuffer(BackBuffer,Type,&fd);//ppBackBuffer);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppBackBuffer = f;

			return hr;
		}

		/*STDMETHOD(GetRasterStatus)(THIS_ D3D8Base::D3DRASTER_STATUS* pRasterStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRasterStatus(D3D8Base::D3DRASTER_STATUS* pRasterStatus)
		{
			LOG("GetRasterStatus");
			HRESULT hr = m_pDevice->GetRasterStatus(pRasterStatus);

			return hr;
		}

		/*STDMETHOD_(void, SetGammaRamp)(THIS_ DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetGammaRamp(DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("SetGammaRamp");
			m_pDevice->SetGammaRamp(Flags,pRamp);
		}

		/*STDMETHOD_(void, GetGammaRamp)(THIS_ D3D8Base::D3DGAMMARAMP* pRamp) PURE;*/
		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::GetGammaRamp(D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("GetGammaRamp");
			m_pDevice->GetGammaRamp(pRamp);
		}

		/*STDMETHOD(CreateTexture)(THIS_ UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DTexture8** ppTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateTexture(UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DTexture8** ppTexture)
		{
			LOG("CreateTexture");

			D3D8Base::IDirect3DTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateTexture(Width,Height,Levels,Usage,Format,Pool,&fd);//ppTexture);

			D3D8Wrapper::IDirect3DTexture8* f = D3D8Wrapper::IDirect3DTexture8::GetTexture(fd);

			*ppTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateVolumeTexture)(THIS_ UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DVolumeTexture8** ppVolumeTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVolumeTexture(UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVolumeTexture8** ppVolumeTexture)
		{
			LOG("CreateVolumeTexture");

			D3D8Base::IDirect3DVolumeTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateVolumeTexture(Width,Height,Depth,Levels,Usage,Format,Pool,&fd);//ppVolumeTexture);

			D3D8Wrapper::IDirect3DVolumeTexture8* f = new D3D8Wrapper::IDirect3DVolumeTexture8(fd);

			*ppVolumeTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateCubeTexture)(THIS_ UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DCubeTexture8** ppCubeTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateCubeTexture(UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DCubeTexture8** ppCubeTexture)
		{
			LOG("CreateCubeTexture");

			D3D8Base::IDirect3DCubeTexture8* fd = NULL;

			HRESULT hr = m_pDevice->CreateCubeTexture(EdgeLength,Levels, Usage,Format,Pool,&fd);//ppCubeTexture);

			D3D8Wrapper::IDirect3DCubeTexture8* f = new D3D8Wrapper::IDirect3DCubeTexture8(fd);

			*ppCubeTexture = f;

			return hr;
		}

		/*STDMETHOD(CreateVertexBuffer)(THIS_ UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DVertexBuffer8** ppVertexBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexBuffer(UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVertexBuffer8** ppVertexBuffer)
		{
			LOG("CreateVertexBuffer");

			D3D8Base::IDirect3DVertexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->CreateVertexBuffer(Length,Usage,FVF,Pool,&fd);//ppVertexBuffer);

			D3D8Wrapper::IDirect3DVertexBuffer8* f = new D3D8Wrapper::IDirect3DVertexBuffer8(fd);

			*ppVertexBuffer = f;

			return hr;
		}

		/*STDMETHOD(CreateIndexBuffer)(THIS_ UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Base::IDirect3DIndexBuffer8** ppIndexBuffer) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateIndexBuffer(UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexBuffer)
		{
			LOG("CreateIndexBuffer");

			D3D8Base::IDirect3DIndexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->CreateIndexBuffer(Length,Usage,Format,Pool,&fd);//ppIndexBuffer);

			D3D8Wrapper::IDirect3DIndexBuffer8* f = new D3D8Wrapper::IDirect3DIndexBuffer8(fd);

			*ppIndexBuffer = f;

			return hr;
		}

		/*STDMETHOD(CreateRenderTarget)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateRenderTarget(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateRenderTarget");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateRenderTarget(Width,Height,Format,MultiSample,Lockable,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CreateDepthStencilSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateDepthStencilSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateDepthStencilSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateDepthStencilSurface(Width,Height,Format,MultiSample,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CreateImageSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::IDirect3DSurface8** ppSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateImageSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("CreateImageSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->CreateImageSurface(Width,Height,Format,&fd);//ppSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppSurface = f;

			return hr;
		}

		/*STDMETHOD(CopyRects)(THIS_ D3D8Base::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Base::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CopyRects(D3D8Wrapper::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Wrapper::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray)
		{
			LOG("CopyRects");
			LOG(pSourceSurface);
			LOG(pSourceSurface->GetSurface());
			LOG(pDestinationSurface);
			LOG(pDestinationSurface->GetSurface());

			if (pSourceSurface->m_ulRef == 0 || (pSourceSurface->GetSurface()) == (pDestinationSurface->GetSurface()))
			{
				LOG("WTF");
				return D3DERR_INVALIDCALL;
			}

			HRESULT hr = m_pDevice->CopyRects(pSourceSurface->GetSurface(),pSourceRectsArray,cRects,pDestinationSurface->GetSurface(),pDestPointsArray);

			LOG("Back??");

			LOG(hr);

			LOG(pSourceSurface);
			LOG(pSourceSurface->GetSurface());
			LOG(pDestinationSurface);
			LOG(pDestinationSurface->GetSurface());
			return hr;
		}

		/*STDMETHOD(UpdateTexture)(THIS_ D3D8Base::IDirect3DBaseTexture8* pSourceTexture,D3D8Base::IDirect3DBaseTexture8* pDestinationTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::UpdateTexture(D3D8Wrapper::IDirect3DBaseTexture8* pSourceTexture,D3D8Wrapper::IDirect3DBaseTexture8* pDestinationTexture)
		{
			LOG("UpdateTexture");
			HRESULT hr = m_pDevice->UpdateTexture(pSourceTexture->GetBaseTexture(),pDestinationTexture->GetBaseTexture());

			return hr;
		}

		/*STDMETHOD(GetFrontBuffer)(THIS_ D3D8Base::IDirect3DSurface8* pDestSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetFrontBuffer(D3D8Wrapper::IDirect3DSurface8* pDestSurface)
		{
			LOG("GetFrontBuffer");
			HRESULT hr = m_pDevice->GetFrontBuffer(pDestSurface->GetSurface());

			return hr;
		}

		/*STDMETHOD(SetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8* pRenderTarget,D3D8Base::IDirect3DSurface8* pNewZStencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderTarget(D3D8Wrapper::IDirect3DSurface8* pRenderTarget,D3D8Wrapper::IDirect3DSurface8* pNewZStencil)
		{
			LOG("SetRenderTarget");
			LOG(pRenderTarget);
			LOG(pNewZStencil);
			//HRESULT hr = m_pDevice->SetRenderTarget(pRenderTarget->GetSurface(),pNewZStencil->GetSurface());
			HRESULT hr = m_pDevice->SetRenderTarget(render_surface->GetSurface(),pNewZStencil->GetSurface());

			/*if (this->rTarget != NULL)
			{
				this->rTarget->Release();
			}
			if (this->zStencil != NULL)
			{
				this->zStencil->Release();
			}*/

			this->rTarget = pRenderTarget;
			this->zStencil = pNewZStencil;

			this->rTarget->m_ulRef++;
			this->zStencil->m_ulRef++;



			return hr;
		}

		/*STDMETHOD(GetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8** ppRenderTarget) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderTarget(D3D8Wrapper::IDirect3DSurface8** ppRenderTarget)
		{
			LOG("GetRenderTarget");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetRenderTarget(&fd);//ppRenderTarget);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppRenderTarget = f;

			return hr;
		}

		/*STDMETHOD(GetDepthStencilSurface)(THIS_ D3D8Base::IDirect3DSurface8** ppZStencilSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDepthStencilSurface(D3D8Wrapper::IDirect3DSurface8** ppZStencilSurface)
		{
			LOG("GetDepthStencilSurface");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetDepthStencilSurface(&fd);//ppZStencilSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppZStencilSurface = f;

			return hr;
		}

		/*STDMETHOD(BeginScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginScene()
		{
			LOG("BeginScene");
			HRESULT hr = m_pDevice->BeginScene();

			return hr;
		}

		/*STDMETHOD(EndScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndScene()
		{
			LOG("EndScene");
			HRESULT hr = m_pDevice->EndScene();

			return hr;
		}

		/*STDMETHOD(Clear)(THIS_ DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Clear(DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil)
		{
			LOG("Clear");
			LOG(Count);
			LOG(pRects);
			if (pRects != NULL)
			{
				LOG(" " << pRects->x1);
				LOG(" " << pRects->y1);
				LOG(" " << pRects->x2);
				LOG(" " << pRects->y2);
			}
			LOG(Flags);
			LOG(Color);
			LOG(Z);
			LOG(Stencil);

			HRESULT hr = m_pDevice->Clear(Count,pRects,Flags,Color,Z,Stencil);

			return hr;
		}

		/*STDMETHOD(SetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("SetTransform");
			HRESULT hr = m_pDevice->SetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(GetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("GetTransform");
			HRESULT hr = m_pDevice->GetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(MultiplyTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE,CONST D3D8Base::D3DMATRIX*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::MultiplyTransform(D3D8Base::D3DTRANSFORMSTATETYPE foo,CONST D3D8Base::D3DMATRIX* bar)
		{
			LOG("MultiplyTransform");
			HRESULT hr = m_pDevice->MultiplyTransform(foo, bar);

			return hr;
		}

		/*STDMETHOD(SetViewport)(THIS_ CONST D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetViewport(CONST D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("SetViewport");

			HRESULT hr = m_pDevice->SetViewport(pViewport);
			LOG(hr);
			return hr;
		}

		/*STDMETHOD(GetViewport)(THIS_ D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetViewport(D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("GetViewport");
			HRESULT hr = m_pDevice->GetViewport(pViewport);
			LOG(pViewport);
			if (pViewport != NULL)
			{
				LOG(pViewport->X);
				LOG(pViewport->Y);
				LOG(pViewport->Width);
				LOG(pViewport->Height);
				LOG(pViewport->MinZ);
				LOG(pViewport->MaxZ);
			}
			LOG(hr);
			return hr;
		}

		/*STDMETHOD(SetMaterial)(THIS_ CONST D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetMaterial(CONST D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("SetMaterial");
			HRESULT hr = m_pDevice->SetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(GetMaterial)(THIS_ D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetMaterial(D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("GetMaterial");
			HRESULT hr = m_pDevice->GetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(SetLight)(THIS_ DWORD Index,CONST D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetLight(DWORD Index,CONST D3D8Base::D3DLIGHT8* foo)
		{
			LOG("SetLight");
			HRESULT hr = m_pDevice->SetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(GetLight)(THIS_ DWORD Index,D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLight(DWORD Index,D3D8Base::D3DLIGHT8* foo)
		{
			LOG("GetLight");
			HRESULT hr = m_pDevice->GetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(LightEnable)(THIS_ DWORD Index,BOOL Enable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::LightEnable(DWORD Index,BOOL Enable)
		{
			LOG("LightEnable");
			HRESULT hr = m_pDevice->LightEnable(Index,Enable);

			return hr;
		}

		/*STDMETHOD(GetLightEnable)(THIS_ DWORD Index,BOOL* pEnable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLightEnable(DWORD Index,BOOL* pEnable)
		{
			LOG("GetLightEnable");
			HRESULT hr = m_pDevice->GetLightEnable(Index,pEnable);

			return hr;
		}

		/*STDMETHOD(SetClipPlane)(THIS_ DWORD Index,CONST float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipPlane(DWORD Index,CONST float* pPlane)
		{
			LOG("SetClipPlane");
			HRESULT hr = m_pDevice->SetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(GetClipPlane)(THIS_ DWORD Index,float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipPlane(DWORD Index,float* pPlane)
		{
			LOG("GetClipPlane");
			HRESULT hr = m_pDevice->GetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(SetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD Value)
		{
			LOG("SetRenderState");
			LOG(State);
			LOG(Value);

			HRESULT hr = m_pDevice->SetRenderState(State,Value);

			return hr;
		}

		/*STDMETHOD(GetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue)
		{
			LOG("GetRenderState");
			HRESULT hr = m_pDevice->GetRenderState(State,pValue);

			return hr;
		}

		/*STDMETHOD(BeginStateBlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginStateBlock()
		{
			LOG("BeginStateBlock");
			HRESULT hr = m_pDevice->BeginStateBlock();

			return hr;
		}

		/*STDMETHOD(EndStateBlock)(THIS_ DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndStateBlock(DWORD* pToken)
		{
			LOG("EndStateBlock");
			HRESULT hr = m_pDevice->EndStateBlock(pToken);

			return hr;
		}

		/*STDMETHOD(ApplyStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ApplyStateBlock(DWORD Token)
		{
			LOG("ApplyStateBlock");
			HRESULT hr = m_pDevice->ApplyStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CaptureStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CaptureStateBlock(DWORD Token)
		{
			LOG("CaptureStateBlock");
			HRESULT hr = m_pDevice->CaptureStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(DeleteStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteStateBlock(DWORD Token)
		{
			LOG("DeleteStateBlock");
			HRESULT hr = m_pDevice->DeleteStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CreateStateBlock)(THIS_ D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateStateBlock(D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken)
		{
			LOG("CreateStateBlock");
			HRESULT hr = m_pDevice->CreateStateBlock(Type,pToken);

			return hr;
		}

		/*STDMETHOD(SetClipStatus)(THIS_ CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipStatus(CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("SetClipStatus");
			HRESULT hr = m_pDevice->SetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetClipStatus)(THIS_ D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipStatus(D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("GetClipStatus");
			HRESULT hr = m_pDevice->GetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8** ppTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8** ppTexture)
		{
			LOG("GetTexture");

			D3D8Base::IDirect3DBaseTexture8* fd = NULL;

			HRESULT hr = m_pDevice->GetTexture(Stage,&fd);//ppTexture);

			D3D8Wrapper::IDirect3DBaseTexture8* f = new D3D8Wrapper::IDirect3DBaseTexture8(fd);

			*ppTexture = f;

			return hr;
		}

		/*STDMETHOD(SetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8* pTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8* pTexture)
		{
			LOG("SetTexture");
			LOG("pTexture:  " << pTexture);

			if (pTexture == NULL)
			{
				return m_pDevice->SetTexture(Stage,NULL);
			}
			else
			{
				LOG(pTexture->GetResource());
				LOG(pTexture->GetBaseTexture());
				HRESULT hr = m_pDevice->SetTexture(Stage,pTexture->GetBaseTexture());

				return hr;
			}
		}

		/*STDMETHOD(GetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue)
		{
			LOG("GetTextureStageState");
			HRESULT hr = m_pDevice->GetTextureStageState(Stage,Type,pValue);

			return hr;
		}

		/*STDMETHOD(SetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value)
		{
			LOG("SetTextureStageState");
			HRESULT hr = m_pDevice->SetTextureStageState(Stage,Type,Value);

			return hr;
		}

		/*STDMETHOD(ValidateDevice)(THIS_ DWORD* pNumPasses) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ValidateDevice(DWORD* pNumPasses)
		{
			LOG("ValidateDevice");
			HRESULT hr = m_pDevice->ValidateDevice(pNumPasses);

			return hr;
		}

		/*STDMETHOD(GetInfo)(THIS_ DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetInfo(DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize)
		{
			LOG("GetInfo");
			HRESULT hr = m_pDevice->GetInfo(DevInfoID,pDevInfoStruct,DevInfoStructSize);

			return hr;
		}

		/*STDMETHOD(SetPaletteEntries)(THIS_ UINT PaletteNumber,CONST PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPaletteEntries(UINT PaletteNumber,CONST PALETTEENTRY* pEntries)
		{
			LOG("SetPaletteEntries");
			HRESULT hr = m_pDevice->SetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(GetPaletteEntries)(THIS_ UINT PaletteNumber,PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPaletteEntries(UINT PaletteNumber,PALETTEENTRY* pEntries)
		{
			LOG("GetPaletteEntries");
			HRESULT hr = m_pDevice->GetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(SetCurrentTexturePalette)(THIS_ UINT PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCurrentTexturePalette(UINT PaletteNumber)
		{
			LOG("SetCurrentTexturePalette");
			HRESULT hr = m_pDevice->SetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(GetCurrentTexturePalette)(THIS_ UINT *PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCurrentTexturePalette(UINT *PaletteNumber)
		{
			LOG("GetCurrentTexturePalette");
			HRESULT hr = m_pDevice->GetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(DrawPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount)
		{
			LOG("DrawPrimitive");
			HRESULT hr = m_pDevice->DrawPrimitive(PrimitiveType,StartVertex,PrimitiveCount);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount)
		{
			LOG("DrawIndexedPrimitive");
			HRESULT hr = m_pDevice->DrawIndexedPrimitive(PrimitiveType,minIndex,NumVertices,startIndex,primCount);

			return hr;
		}

		/*STDMETHOD(DrawPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("DrawPrimitiveUP");
			HRESULT hr = m_pDevice->DrawPrimitiveUP(PrimitiveType,PrimitiveCount,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("DrawIndexedPrimitiveUP");
			HRESULT hr = m_pDevice->DrawIndexedPrimitiveUP(PrimitiveType,MinVertexIndex,NumVertexIndices,PrimitiveCount,pIndexData,IndexDataFormat,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(ProcessVertices)(THIS_ UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Base::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ProcessVertices(UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Wrapper::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags)
		{
			LOG("ProcessVertices");
			HRESULT hr = m_pDevice->ProcessVertices(SrcStartIndex,DestIndex,VertexCount,pDestBuffer->GetVertexBuffer(),Flags);

			return hr;
		}

		/*STDMETHOD(CreateVertexShader)(THIS_ CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexShader(CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage)
		{
			LOG("CreateVertexShader");
			HRESULT hr = m_pDevice->CreateVertexShader(pDeclaration,pFunction,pHandle,Usage);

			return hr;
		}

		/*STDMETHOD(SetVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShader(DWORD Handle)
		{
			LOG("SetVertexShader");
			HRESULT hr = m_pDevice->SetVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetVertexShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShader(DWORD* pHandle)
		{
			LOG("GetVertexShader");
			HRESULT hr = m_pDevice->GetVertexShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeleteVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteVertexShader(DWORD Handle)
		{
			LOG("DeleteVertexShader");
			HRESULT hr = m_pDevice->DeleteVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetVertexShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("SetVertexShaderConstant");
			HRESULT hr = m_pDevice->SetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("GetVertexShaderConstant");
			HRESULT hr = m_pDevice->GetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderDeclaration)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderDeclaration(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetVertexShaderDeclaration");
			HRESULT hr = m_pDevice->GetVertexShaderDeclaration(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetVertexShaderFunction");
			HRESULT hr = m_pDevice->GetVertexShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(SetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8* pStreamData,UINT Stride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8* pStreamData,UINT Stride)
		{
			LOG("SetStreamSource");
			HRESULT hr = m_pDevice->SetStreamSource(StreamNumber,pStreamData->GetVertexBuffer(),Stride);

			return hr;
		}

		/*STDMETHOD(GetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride)
		{
			LOG("GetStreamSource");

			D3D8Base::IDirect3DVertexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetStreamSource(StreamNumber,&fd,pStride);//ppStreamData,pStride);

			D3D8Wrapper::IDirect3DVertexBuffer8* f = new D3D8Wrapper::IDirect3DVertexBuffer8(fd);

			*ppStreamData = f;

			return hr;
		}

		/*STDMETHOD(SetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetIndices(D3D8Wrapper::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex)
		{
			LOG("SetIndices");
			HRESULT hr = m_pDevice->SetIndices(pIndexData->GetIndexBuffer(),BaseVertexIndex);

			return hr;
		}

		/*STDMETHOD(GetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetIndices(D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex)
		{
			LOG("GetIndices");

			D3D8Base::IDirect3DIndexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetIndices(&fd,pBaseVertexIndex);// ppIndexData,pBaseVertexIndex);

			D3D8Wrapper::IDirect3DIndexBuffer8* f = new D3D8Wrapper::IDirect3DIndexBuffer8(fd);

			*ppIndexData = f;

			return hr;
		}

		/*STDMETHOD(CreatePixelShader)(THIS_ CONST DWORD* pFunction,DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreatePixelShader(CONST DWORD* pFunction,DWORD* pHandle)
		{
			LOG("CreatePixelShader");
			HRESULT hr = m_pDevice->CreatePixelShader(pFunction,pHandle);

			return hr;
		}

		/*STDMETHOD(SetPixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShader(DWORD Handle)
		{
			LOG("SetPixelShader");
			HRESULT hr = m_pDevice->SetPixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetPixelShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShader(DWORD* pHandle)
		{
			LOG("GetPixelShader");
			HRESULT hr = m_pDevice->GetPixelShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeletePixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePixelShader(DWORD Handle)
		{
			LOG("DeletePixelShader");
			HRESULT hr = m_pDevice->DeletePixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetPixelShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("SetPixelShaderConstant");
			HRESULT hr = m_pDevice->SetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("GetPixelShaderConstant");
			HRESULT hr = m_pDevice->GetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("GetPixelShaderFunction");
			HRESULT hr = m_pDevice->GetPixelShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(DrawRectPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawRectPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo)
		{
			LOG("DrawRectPatch");
			HRESULT hr = m_pDevice->DrawRectPatch(Handle,pNumSegs,pRectPatchInfo);

			return hr;
		}

		/*STDMETHOD(DrawTriPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawTriPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo)
		{
			LOG("DrawTriPatch");
			HRESULT hr = m_pDevice->DrawTriPatch(Handle,pNumSegs,pTriPatchInfo);

			return hr;
		}

		/*STDMETHOD(DeletePatch)(THIS_ UINT Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePatch(UINT Handle)
		{
			LOG("DeletePatch");
			HRESULT hr = m_pDevice->DeletePatch(Handle);

			return hr;
		}
	}
}