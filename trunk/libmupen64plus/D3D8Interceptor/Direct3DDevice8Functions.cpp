#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		D3D8Wrapper::IDirect3DDevice8::IDirect3DDevice8(D3D8Base::IDirect3DDevice8* pDevice) : IDirect3DUnknown((IUnknown*) pDevice)
		{
			LOG("IDirect3DDevice8::IDirect3DDevice8( " << pDevice << " )\n");
			m_pDevice = pDevice;
		}

		D3D8Wrapper::IDirect3DDevice8* D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(D3D8Base::IDirect3DDevice8* pDevice)
		{
			LOG("IDirect3DDevice8::GetDirect3DDevice( " << pDevice << " )\n");
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
			LOG("IDirect3DDevice8::Release " << this << "\n");
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

		
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::TestCooperativeLevel()
		{
			LOG("IDirect3DDevice8::TestCooperativeLevel()\n");
			return m_pDevice->TestCooperativeLevel();
		}

		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3DDevice8::GetAvailableTextureMem()
		{
			LOG("IDirect3DDevice8::GetAvailableTextureMem()\n");
			return m_pDevice->GetAvailableTextureMem();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ResourceManagerDiscardBytes(DWORD Bytes)
		{
			LOG("IDirect3DDevice8::ResourceManagerDiscardBytes( " << Bytes << " )\n");
			return m_pDevice->ResourceManagerDiscardBytes(Bytes);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDirect3D(D3D8Wrapper::IDirect3D8** ppD3D8)
		{
			LOG("IDirect3DDevice8::GetDirect3D( " << ppD3D8 << " )\n");

			// Run the function and wrap the result before returning it
			D3D8Base::IDirect3D8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetDirect3D(&realD3D);

			D3D8Wrapper::IDirect3D8* wrappedD3D = D3D8Wrapper::IDirect3D8::GetDirect3D(realD3D);

			*ppD3D8 = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDeviceCaps(D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("IDirect3DDevice8::GetDeviceCaps( " << pCaps << " )\n");
			return m_pDevice->GetDeviceCaps(pCaps);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDisplayMode(D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("IDirect3DDevice8::GetDisplayMode( " << pMode << " )\n");
			return m_pDevice->GetDisplayMode(pMode);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCreationParameters(D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters)
		{
			LOG("IDirect3DDevice8::GetCreationParameters( " << pParameters << " )\n");
			return m_pDevice->GetCreationParameters(pParameters);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCursorProperties(UINT XHotSpot,UINT YHotSpot,D3D8Wrapper::IDirect3DSurface8* pCursorBitmap)
		{
			LOG("IDirect3DDevice8::SetCursorProperties( " << XHotSpot << " , " << YHotSpot << " , " << pCursorBitmap << " )\n");
			return m_pDevice->SetCursorProperties(XHotSpot,YHotSpot,pCursorBitmap->GetSurface());
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetCursorPosition(int X,int Y,DWORD Flags)
		{
			LOG("IDirect3DDevice8::SetCursorPosition( " << X << " , " << Y << " , " << Flags << " )\n");
			m_pDevice->SetCursorPosition(X,Y,Flags);
		}

		STDMETHODIMP_(BOOL) D3D8Wrapper::IDirect3DDevice8::ShowCursor(BOOL bShow)
		{
			LOG("IDirect3DDevice8::ShowCursor( " << bShow << " )\n");
			return m_pDevice->ShowCursor(bShow);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateAdditionalSwapChain(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DSwapChain8** pSwapChain)
		{
			LOG("IDirect3DDevice8::CreateAdditionalSwapChain( " << pPresentationParameters << " , " << pSwapChain << " )\n");
			D3D8Base::IDirect3DSwapChain8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateAdditionalSwapChain(pPresentationParameters,&realD3D);

			D3D8Wrapper::IDirect3DSwapChain8* wrappedD3D = new D3D8Wrapper::IDirect3DSwapChain8(realD3D);

			*pSwapChain = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Reset(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters)
		{
			LOG("IDirect3DDevice8::Reset( " << pPresentationParameters << " )\n");
			return m_pDevice->Reset(pPresentationParameters);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Present(CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion)
		{
#ifdef LOGGING
			LOG("IDirect3DDevice8::Present( " << pSourceRect);
			if (pSourceRect != NULL)
			{
				LOG("{ " << pSourceRect->left << " , " << pSourceRect->top << " , " << pSourceRect->right << " , " << pSourceRect->bottom << " }");
			}
			LOG(" , " << pDestRect);
			if (pSourceRect != NULL)
			{
				LOG("{ " << pDestRect->left << " , " << pDestRect->top << " , " << pDestRect->right << " , " << pDestRect->bottom << " }");
			}
			LOG(" , " << hDestWindowOverride << " , " << pDirtyRegion << " )\n");
#endif
			// Force the result to OK
			HRESULT hr = D3D_OK;

			// Don't call the real present
			//hr = m_pDevice->Present(pSourceRect,pDestRect,hDestWindowOverride,pDirtyRegion);

			// Let bizhawk know the frame is ready
			rendering_callback(0);

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetBackBuffer(UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer)
		{
			LOG("IDirect3DDevice8::GetBackBuffer( " << BackBuffer << " , " << Type << " , " << ppBackBuffer << " )\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetBackBuffer(BackBuffer,Type,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppBackBuffer = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRasterStatus(D3D8Base::D3DRASTER_STATUS* pRasterStatus)
		{
			LOG("IDirect3DDevice8::GetRasterStatus( " << pRasterStatus << " )\n");
			return m_pDevice->GetRasterStatus(pRasterStatus);
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetGammaRamp(DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("IDirect3DDevice8::SetGammaRamp( " << Flags << " , " << pRamp << " )\n");
			m_pDevice->SetGammaRamp(Flags,pRamp);
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::GetGammaRamp(D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("IDirect3DDevice8::GetGammaRamp( " << pRamp << " )\n");
			m_pDevice->GetGammaRamp(pRamp);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateTexture(UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DTexture8** ppTexture)
		{
			LOG("IDirect3DDevice8::CreateTexture( " << Width << " , " << Height << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppTexture << " )\n");

			D3D8Base::IDirect3DTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateTexture(Width,Height,Levels,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DTexture8* wrappedD3D = D3D8Wrapper::IDirect3DTexture8::GetTexture(realD3D);

			*ppTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVolumeTexture(UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVolumeTexture8** ppVolumeTexture)
		{
			LOG("IDirect3DDevice8::CreateVolumeTexture( " << Width << " , " << Height << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppVolumeTexture << " )\n");

			D3D8Base::IDirect3DVolumeTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateVolumeTexture(Width,Height,Depth,Levels,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DVolumeTexture8* wrappedD3D = new D3D8Wrapper::IDirect3DVolumeTexture8(realD3D);

			*ppVolumeTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateCubeTexture(UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DCubeTexture8** ppCubeTexture)
		{
			LOG("IDirect3DDevice8::CreateCubeTexture( " << EdgeLength << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppCubeTexture << " )\n");

			D3D8Base::IDirect3DCubeTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateCubeTexture(EdgeLength,Levels, Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DCubeTexture8* wrappedD3D = new D3D8Wrapper::IDirect3DCubeTexture8(realD3D);

			*ppCubeTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexBuffer(UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVertexBuffer8** ppVertexBuffer)
		{
			LOG("IDirect3DDevice8::CreateVertexBuffer( " << Length << " , " << Usage << " , " << FVF << " , " << Pool << " , " << ppVertexBuffer << " )\n");

			D3D8Base::IDirect3DVertexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateVertexBuffer(Length,Usage,FVF,Pool,&realD3D);

			D3D8Wrapper::IDirect3DVertexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DVertexBuffer8(realD3D);

			*ppVertexBuffer = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateIndexBuffer(UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexBuffer)
		{
			LOG("IDirect3DDevice8::CreateIndexBuffer( " << Length << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppIndexBuffer << " )\n");

			D3D8Base::IDirect3DIndexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateIndexBuffer(Length,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DIndexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DIndexBuffer8(realD3D);

			*ppIndexBuffer = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateRenderTarget(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateRenderTarget( " << Width << " , " << Height << " , " << Format << " , " << MultiSample << " , " << Lockable << " , " << ppSurface << " )\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateRenderTarget(Width,Height,Format,MultiSample,Lockable,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateDepthStencilSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateDepthStencilSurface( " << Width << " , " << Height << " , " << Format << " , " << MultiSample << " , " << ppSurface << " )\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateDepthStencilSurface(Width,Height,Format,MultiSample,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateImageSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateImageSurface( " << Width << " , " << Height << " , " << Format << " , " << ppSurface << " )\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateImageSurface(Width,Height,Format,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CopyRects(D3D8Wrapper::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Wrapper::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray)
		{
			LOG("IDirect3DDevice8::CopyRects( " << pSourceSurface << " , " << pSourceRectsArray << " , " << cRects << " , " << pDestinationSurface << " , " << pDestPointsArray << " )\n");
			
			if (pSourceSurface->m_ulRef == 0 || (pSourceSurface->GetSurface()) == (pDestinationSurface->GetSurface()))
			{
				LOG("WTF\n");
				return D3DERR_INVALIDCALL;
			}

			HRESULT hr = m_pDevice->CopyRects(pSourceSurface->GetSurface(),pSourceRectsArray,cRects,pDestinationSurface->GetSurface(),pDestPointsArray);

			return hr;
		}

		/*STDMETHOD(UpdateTexture)(THIS_ D3D8Base::IDirect3DBaseTexture8* pSourceTexture,D3D8Base::IDirect3DBaseTexture8* pDestinationTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::UpdateTexture(D3D8Wrapper::IDirect3DBaseTexture8* pSourceTexture,D3D8Wrapper::IDirect3DBaseTexture8* pDestinationTexture)
		{
			LOG("IDirect3DDevice8::UpdateTexture( " << pSourceTexture << " , " << pDestinationTexture << " )\n");
			HRESULT hr = m_pDevice->UpdateTexture(pSourceTexture->GetBaseTexture(),pDestinationTexture->GetBaseTexture());

			return hr;
		}

		/*STDMETHOD(GetFrontBuffer)(THIS_ D3D8Base::IDirect3DSurface8* pDestSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetFrontBuffer(D3D8Wrapper::IDirect3DSurface8* pDestSurface)
		{
			LOG("IDirect3DDevice8::GetFrontBuffer( " << pDestSurface << " )\n");
			HRESULT hr = m_pDevice->GetFrontBuffer(pDestSurface->GetSurface());

			return hr;
		}

		/*STDMETHOD(SetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8* pRenderTarget,D3D8Base::IDirect3DSurface8* pNewZStencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderTarget(D3D8Wrapper::IDirect3DSurface8* pRenderTarget,D3D8Wrapper::IDirect3DSurface8* pNewZStencil)
		{
			LOG("IDirect3DDevice8::SetRenderTarget( " << pRenderTarget << " , " << pNewZStencil << " )\n");

			//HRESULT hr = m_pDevice->SetRenderTarget(pRenderTarget->GetSurface(),pNewZStencil->GetSurface());
			HRESULT hr = m_pDevice->SetRenderTarget(render_surface->GetSurface(),pNewZStencil->GetSurface());

			pRenderTarget->m_ulRef++;
			pNewZStencil->m_ulRef++;

			return hr;
		}

		/*STDMETHOD(GetRenderTarget)(THIS_ D3D8Base::IDirect3DSurface8** ppRenderTarget) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderTarget(D3D8Wrapper::IDirect3DSurface8** ppRenderTarget)
		{
			LOG("IDirect3DDevice8::GetRenderTarget( " << ppRenderTarget << " )\n");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetRenderTarget(&fd);//ppRenderTarget);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppRenderTarget = f;

			return hr;
		}

		/*STDMETHOD(GetDepthStencilSurface)(THIS_ D3D8Base::IDirect3DSurface8** ppZStencilSurface) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDepthStencilSurface(D3D8Wrapper::IDirect3DSurface8** ppZStencilSurface)
		{
			LOG("IDirect3DDevice8::GetDepthStencilSurface( " << ppZStencilSurface << " )\n");

			D3D8Base::IDirect3DSurface8* fd = NULL;

			HRESULT hr = m_pDevice->GetDepthStencilSurface(&fd);//ppZStencilSurface);

			D3D8Wrapper::IDirect3DSurface8* f = D3D8Wrapper::IDirect3DSurface8::GetSurface(fd);

			*ppZStencilSurface = f;

			return hr;
		}

		/*STDMETHOD(BeginScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginScene()
		{
			LOG("IDirect3DDevice8::BeginScene()\n");
			HRESULT hr = m_pDevice->BeginScene();

			return hr;
		}

		/*STDMETHOD(EndScene)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndScene()
		{
			LOG("IDirect3DDevice8::EndScene()\n");
			HRESULT hr = m_pDevice->EndScene();

			return hr;
		}

		/*STDMETHOD(Clear)(THIS_ DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Clear(DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil)
		{
#ifdef LOGGING
			LOG("IDirect3DDevice8::Clear( " << Count << " , " << pRects);
			if (pRects != NULL)
			{
				LOG("{ " << pRects->x1 << " , " << pRects->y1 << " , " << pRects->x2 << " , " << pRects->y2 << " }")
			}
			LOG(" , " << Flags << " , " << Color << " , " << Z << " , " << Stencil << " )\n");
#endif			

			HRESULT hr = m_pDevice->Clear(Count,pRects,Flags,Color,Z,Stencil);

			return hr;
		}

		/*STDMETHOD(SetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("IDirect3DDevice8::SetTransform( " << State << " , " << pMatrix << " )\n");
			HRESULT hr = m_pDevice->SetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(GetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("IDirect3DDevice8::GetTransform( " << State << " , " << pMatrix << " )\n");
			HRESULT hr = m_pDevice->GetTransform(State,pMatrix);

			return hr;
		}

		/*STDMETHOD(MultiplyTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE,CONST D3D8Base::D3DMATRIX*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::MultiplyTransform(D3D8Base::D3DTRANSFORMSTATETYPE foo,CONST D3D8Base::D3DMATRIX* bar)
		{
			LOG("IDirect3DDevice8::MultiplyTransform( " << foo << " , " << bar << " )\n");
			HRESULT hr = m_pDevice->MultiplyTransform(foo, bar);

			return hr;
		}

		/*STDMETHOD(SetViewport)(THIS_ CONST D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetViewport(CONST D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("IDirect3DDevice8::SetViewport( " << pViewport << " )\n");

			HRESULT hr = m_pDevice->SetViewport(pViewport);
			return hr;
		}

		/*STDMETHOD(GetViewport)(THIS_ D3D8Base::D3DVIEWPORT8* pViewport) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetViewport(D3D8Base::D3DVIEWPORT8* pViewport)
		{
#ifdef LOGGING
			LOG("IDirect3DDevice8::GetViewport( " << pViewport);
			if (pViewport != NULL)
			{
				LOG("{ " << pViewport->X << " , " << pViewport->Y << " , " << pViewport->Width << " , " << pViewport->Height << " , " << pViewport->MinZ << " , " << pViewport->MaxZ << " }");
			}
			LOG(" )\n");
#endif
			HRESULT hr = m_pDevice->GetViewport(pViewport);

			return hr;
		}

		/*STDMETHOD(SetMaterial)(THIS_ CONST D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetMaterial(CONST D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("IDirect3DDevice8::SetMaterial( " << pMaterial << " )\n");
			HRESULT hr = m_pDevice->SetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(GetMaterial)(THIS_ D3D8Base::D3DMATERIAL8* pMaterial) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetMaterial(D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("IDirect3DDevice8::GetMaterial( " << pMaterial << " )\n");
			HRESULT hr = m_pDevice->GetMaterial(pMaterial);

			return hr;
		}

		/*STDMETHOD(SetLight)(THIS_ DWORD Index,CONST D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetLight(DWORD Index,CONST D3D8Base::D3DLIGHT8* foo)
		{
			LOG("IDirect3DDevice8::SetLight( " << Index << " , " << foo << " )\n");
			HRESULT hr = m_pDevice->SetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(GetLight)(THIS_ DWORD Index,D3D8Base::D3DLIGHT8*) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLight(DWORD Index,D3D8Base::D3DLIGHT8* foo)
		{
			LOG("IDirect3DDevice8::GetLight( " << Index << " , " << foo << " )\n");
			HRESULT hr = m_pDevice->GetLight(Index,foo);

			return hr;
		}

		/*STDMETHOD(LightEnable)(THIS_ DWORD Index,BOOL Enable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::LightEnable(DWORD Index,BOOL Enable)
		{
			LOG("IDirect3DDevice8::LightEnable( " << Index << " , " << Enable << " )\n");
			HRESULT hr = m_pDevice->LightEnable(Index,Enable);

			return hr;
		}

		/*STDMETHOD(GetLightEnable)(THIS_ DWORD Index,BOOL* pEnable) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLightEnable(DWORD Index,BOOL* pEnable)
		{
			LOG("IDirect3DDevice8::GetLightEnable( " << Index << " , " << pEnable << " )\n");
			HRESULT hr = m_pDevice->GetLightEnable(Index,pEnable);

			return hr;
		}

		/*STDMETHOD(SetClipPlane)(THIS_ DWORD Index,CONST float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipPlane(DWORD Index,CONST float* pPlane)
		{
			LOG("IDirect3DDevice8::SetClipPlane( " << Index << " , " << pPlane << " )\n");
			HRESULT hr = m_pDevice->SetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(GetClipPlane)(THIS_ DWORD Index,float* pPlane) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipPlane(DWORD Index,float* pPlane)
		{
			LOG("IDirect3DDevice8::GetClipPlane( " << Index << " , " << pPlane << " )\n");
			HRESULT hr = m_pDevice->GetClipPlane(Index,pPlane);

			return hr;
		}

		/*STDMETHOD(SetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD Value)
		{
			LOG("IDirect3DDevice8::SetRenderState( " << State << " , " << Value << " )\n");

			HRESULT hr = m_pDevice->SetRenderState(State,Value);

			return hr;
		}

		/*STDMETHOD(GetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue)
		{
			LOG("IDirect3DDevice8::GetRenderState( " << State << " , " << pValue << " )\n");
			HRESULT hr = m_pDevice->GetRenderState(State,pValue);

			return hr;
		}

		/*STDMETHOD(BeginStateBlock)(THIS) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginStateBlock()
		{
			LOG("IDirect3DDevice8::BeginStateBlock()\n");
			HRESULT hr = m_pDevice->BeginStateBlock();

			return hr;
		}

		/*STDMETHOD(EndStateBlock)(THIS_ DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndStateBlock(DWORD* pToken)
		{
			LOG("IDirect3DDevice8::EndStateBlock( " << pToken << " )\n");
			HRESULT hr = m_pDevice->EndStateBlock(pToken);

			return hr;
		}

		/*STDMETHOD(ApplyStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ApplyStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::ApplyStateBlock( " << Token << " )\n");
			HRESULT hr = m_pDevice->ApplyStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CaptureStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CaptureStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::CaptureStateBlock( " << Token << " )\n");
			HRESULT hr = m_pDevice->CaptureStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(DeleteStateBlock)(THIS_ DWORD Token) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::DeleteStateBlock( " << Token << " )\n");
			HRESULT hr = m_pDevice->DeleteStateBlock(Token);

			return hr;
		}

		/*STDMETHOD(CreateStateBlock)(THIS_ D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateStateBlock(D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken)
		{
			LOG("IDirect3DDevice8::CreateStateBlock( " << Type << " , " << pToken << " )\n");
			HRESULT hr = m_pDevice->CreateStateBlock(Type,pToken);

			return hr;
		}

		/*STDMETHOD(SetClipStatus)(THIS_ CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipStatus(CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("IDirect3DDevice8::SetClipStatus( " << pClipStatus << " )\n");
			HRESULT hr = m_pDevice->SetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetClipStatus)(THIS_ D3D8Base::D3DCLIPSTATUS8* pClipStatus) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipStatus(D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("IDirect3DDevice8::GetClipStatus( " << pClipStatus << " )\n");
			HRESULT hr = m_pDevice->GetClipStatus(pClipStatus);

			return hr;
		}

		/*STDMETHOD(GetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8** ppTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8** ppTexture)
		{
			LOG("IDirect3DDevice8::GetTexture( " << Stage << " , " << ppTexture << " )\n");

			D3D8Base::IDirect3DBaseTexture8* fd = NULL;

			HRESULT hr = m_pDevice->GetTexture(Stage,&fd);//ppTexture);

			D3D8Wrapper::IDirect3DBaseTexture8* f = new D3D8Wrapper::IDirect3DBaseTexture8(fd);

			*ppTexture = f;

			return hr;
		}

		/*STDMETHOD(SetTexture)(THIS_ DWORD Stage,D3D8Base::IDirect3DBaseTexture8* pTexture) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8* pTexture)
		{
			LOG("IDirect3DDevice8::SetTexture( " << Stage << " , " << pTexture << " )\n");

			if (pTexture == NULL)
			{
				return m_pDevice->SetTexture(Stage,NULL);
			}
			else
			{
				//LOG(pTexture->GetResource() << "\n");
				//LOG(pTexture->GetBaseTexture() << "\n");
				HRESULT hr = m_pDevice->SetTexture(Stage,pTexture->GetBaseTexture());

				return hr;
			}
		}

		/*STDMETHOD(GetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue)
		{
			LOG("IDirect3DDevice8::GetTextureStageState( " << Stage << " , " << Type << " , " << pValue << " )\n");
			HRESULT hr = m_pDevice->GetTextureStageState(Stage,Type,pValue);

			return hr;
		}

		/*STDMETHOD(SetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value)
		{
			LOG("IDirect3DDevice8::SetTextureStageState( " << Stage << " , " << Type << " , " << Value << " )\n");
			HRESULT hr = m_pDevice->SetTextureStageState(Stage,Type,Value);

			return hr;
		}

		/*STDMETHOD(ValidateDevice)(THIS_ DWORD* pNumPasses) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ValidateDevice(DWORD* pNumPasses)
		{
			LOG("IDirect3DDevice8::ValidateDevice( " << pNumPasses << " )\n");
			HRESULT hr = m_pDevice->ValidateDevice(pNumPasses);

			return hr;
		}

		/*STDMETHOD(GetInfo)(THIS_ DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetInfo(DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize)
		{
			LOG("IDirect3DDevice8::GetInfo( " << DevInfoID << " , " << pDevInfoStruct << " , " << DevInfoStructSize << " )\n");
			HRESULT hr = m_pDevice->GetInfo(DevInfoID,pDevInfoStruct,DevInfoStructSize);

			return hr;
		}

		/*STDMETHOD(SetPaletteEntries)(THIS_ UINT PaletteNumber,CONST PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPaletteEntries(UINT PaletteNumber,CONST PALETTEENTRY* pEntries)
		{
			LOG("IDirect3DDevice8::SetPaletteEntries( " << PaletteNumber << " , " << pEntries << " )\n");
			HRESULT hr = m_pDevice->SetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(GetPaletteEntries)(THIS_ UINT PaletteNumber,PALETTEENTRY* pEntries) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPaletteEntries(UINT PaletteNumber,PALETTEENTRY* pEntries)
		{
			LOG("IDirect3DDevice8::GetPaletteEntries( " << PaletteNumber << " , " << pEntries << " )\n");
			HRESULT hr = m_pDevice->GetPaletteEntries(PaletteNumber,pEntries);

			return hr;
		}

		/*STDMETHOD(SetCurrentTexturePalette)(THIS_ UINT PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCurrentTexturePalette(UINT PaletteNumber)
		{
			LOG("IDirect3DDevice8::SetCurrentTexturePalette( " << PaletteNumber << " )\n");
			HRESULT hr = m_pDevice->SetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(GetCurrentTexturePalette)(THIS_ UINT *PaletteNumber) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCurrentTexturePalette(UINT *PaletteNumber)
		{
			LOG("IDirect3DDevice8::GetCurrentTexturePalette( " << PaletteNumber << " )\n");
			HRESULT hr = m_pDevice->GetCurrentTexturePalette(PaletteNumber);

			return hr;
		}

		/*STDMETHOD(DrawPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount)
		{
			LOG("IDirect3DDevice8::DrawPrimitive( " << PrimitiveType << " , " << StartVertex << " , " << PrimitiveCount << " )\n");
			HRESULT hr = m_pDevice->DrawPrimitive(PrimitiveType,StartVertex,PrimitiveCount);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount)
		{
			LOG("IDirect3DDevice8::DrawIndexedPrimitive( " << PrimitiveType << " , " << minIndex << " , " << NumVertices << " , " << startIndex << " , " << primCount << " )\n");
			HRESULT hr = m_pDevice->DrawIndexedPrimitive(PrimitiveType,minIndex,NumVertices,startIndex,primCount);

			return hr;
		}

		/*STDMETHOD(DrawPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("IDirect3DDevice8::DrawPrimitiveUP( " << PrimitiveType << " , " << PrimitiveCount << " , " << pVertexStreamZeroData << " , " << VertexStreamZeroStride << " )\n");
			HRESULT hr = m_pDevice->DrawPrimitiveUP(PrimitiveType,PrimitiveCount,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(DrawIndexedPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("IDirect3DDevice8::DrawIndexedPrimitiveUP( " << PrimitiveType << " , " << MinVertexIndex << " , " << NumVertexIndices << " , " << PrimitiveCount << " , " << pIndexData << " , " << IndexDataFormat << " , " << pVertexStreamZeroData << " , " << VertexStreamZeroStride << " )\n");
			HRESULT hr = m_pDevice->DrawIndexedPrimitiveUP(PrimitiveType,MinVertexIndex,NumVertexIndices,PrimitiveCount,pIndexData,IndexDataFormat,pVertexStreamZeroData,VertexStreamZeroStride);

			return hr;
		}

		/*STDMETHOD(ProcessVertices)(THIS_ UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Base::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ProcessVertices(UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Wrapper::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags)
		{
			LOG("IDirect3DDevice8::ProcessVertices( " << SrcStartIndex << " , " << DestIndex << " , " << VertexCount << " , " << pDestBuffer << " , " << Flags << " )\n");
			HRESULT hr = m_pDevice->ProcessVertices(SrcStartIndex,DestIndex,VertexCount,pDestBuffer->GetVertexBuffer(),Flags);

			return hr;
		}

		/*STDMETHOD(CreateVertexShader)(THIS_ CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexShader(CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage)
		{
			LOG("IDirect3DDevice8::CreateVertexShader( " << pDeclaration << " , " << pFunction << " , " << pHandle << " , " << Usage << " )\n");
			HRESULT hr = m_pDevice->CreateVertexShader(pDeclaration,pFunction,pHandle,Usage);

			return hr;
		}

		/*STDMETHOD(SetVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::SetVertexShader( " << Handle << " )\n");
			HRESULT hr = m_pDevice->SetVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetVertexShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShader(DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::GetVertexShader( " << pHandle << " )\n");
			HRESULT hr = m_pDevice->GetVertexShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeleteVertexShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteVertexShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::DeleteVertexShader( " << Handle << " )\n");
			HRESULT hr = m_pDevice->DeleteVertexShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetVertexShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::SetVertexShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " )\n");
			HRESULT hr = m_pDevice->SetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::GetVertexShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " )\n");
			HRESULT hr = m_pDevice->GetVertexShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderDeclaration)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderDeclaration(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetVertexShaderDeclaration( " << Handle << " , " << pData << " , " << pSizeOfData << " )\n");
			HRESULT hr = m_pDevice->GetVertexShaderDeclaration(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(GetVertexShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetVertexShaderFunction( " << Handle << " , " << pData << " , " << pSizeOfData << " )\n");
			HRESULT hr = m_pDevice->GetVertexShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(SetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8* pStreamData,UINT Stride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8* pStreamData,UINT Stride)
		{
			LOG("IDirect3DDevice8::SetStreamSource( " << StreamNumber << " , " << pStreamData << " , " << Stride << " )\n");
			HRESULT hr = m_pDevice->SetStreamSource(StreamNumber,pStreamData->GetVertexBuffer(),Stride);

			return hr;
		}

		/*STDMETHOD(GetStreamSource)(THIS_ UINT StreamNumber,D3D8Base::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride)
		{
			LOG("IDirect3DDevice8::GetStreamSource( " << StreamNumber << " , " << ppStreamData << " , " << pStride << " )\n");

			D3D8Base::IDirect3DVertexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetStreamSource(StreamNumber,&fd,pStride);//ppStreamData,pStride);

			D3D8Wrapper::IDirect3DVertexBuffer8* f = new D3D8Wrapper::IDirect3DVertexBuffer8(fd);

			*ppStreamData = f;

			return hr;
		}

		/*STDMETHOD(SetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetIndices(D3D8Wrapper::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex)
		{
			LOG("IDirect3DDevice8::SetIndices( " << pIndexData << " , " << BaseVertexIndex << " )\n");
			HRESULT hr = m_pDevice->SetIndices(pIndexData->GetIndexBuffer(),BaseVertexIndex);

			return hr;
		}

		/*STDMETHOD(GetIndices)(THIS_ D3D8Base::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetIndices(D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex)
		{
			LOG("IDirect3DDevice8::GetIndices( " << ppIndexData << " , " << pBaseVertexIndex << " )\n");

			D3D8Base::IDirect3DIndexBuffer8* fd = NULL;

			HRESULT hr = m_pDevice->GetIndices(&fd,pBaseVertexIndex);// ppIndexData,pBaseVertexIndex);

			D3D8Wrapper::IDirect3DIndexBuffer8* f = new D3D8Wrapper::IDirect3DIndexBuffer8(fd);

			*ppIndexData = f;

			return hr;
		}

		/*STDMETHOD(CreatePixelShader)(THIS_ CONST DWORD* pFunction,DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreatePixelShader(CONST DWORD* pFunction,DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::CreatePixelShader( " << pFunction << " , " << pHandle << " )\n");
			HRESULT hr = m_pDevice->CreatePixelShader(pFunction,pHandle);

			return hr;
		}

		/*STDMETHOD(SetPixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::SetPixelShader( " << Handle << " )\n");
			HRESULT hr = m_pDevice->SetPixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(GetPixelShader)(THIS_ DWORD* pHandle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShader(DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::GetPixelShader( " << pHandle << " )\n");
			HRESULT hr = m_pDevice->GetPixelShader(pHandle);

			return hr;
		}

		/*STDMETHOD(DeletePixelShader)(THIS_ DWORD Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePixelShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::DeletePixelShader( " << Handle << " )\n");
			HRESULT hr = m_pDevice->DeletePixelShader(Handle);

			return hr;
		}

		/*STDMETHOD(SetPixelShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::SetPixelShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " )\n");
			HRESULT hr = m_pDevice->SetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::GetPixelShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " )\n");
			HRESULT hr = m_pDevice->GetPixelShaderConstant(Register,pConstantData,ConstantCount);

			return hr;
		}

		/*STDMETHOD(GetPixelShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetPixelShaderFunction( " << Handle << " , " << pData << " , " << pSizeOfData << " )\n");
			HRESULT hr = m_pDevice->GetPixelShaderFunction(Handle,pData,pSizeOfData);

			return hr;
		}

		/*STDMETHOD(DrawRectPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawRectPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo)
		{
			LOG("IDirect3DDevice8::DrawRectPatch( " << Handle << " , " << pNumSegs << " , " << pRectPatchInfo << " )\n");
			HRESULT hr = m_pDevice->DrawRectPatch(Handle,pNumSegs,pRectPatchInfo);

			return hr;
		}

		/*STDMETHOD(DrawTriPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawTriPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo)
		{
			LOG("IDirect3DDevice8::DrawTriPatch( " << Handle << " , " << pNumSegs << " , " << pTriPatchInfo << " )\n");
			HRESULT hr = m_pDevice->DrawTriPatch(Handle,pNumSegs,pTriPatchInfo);

			return hr;
		}

		/*STDMETHOD(DeletePatch)(THIS_ UINT Handle) PURE;*/
		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePatch(UINT Handle)
		{
			LOG("IDirect3DDevice8::DeletePatch( " << Handle << " )\n");
			HRESULT hr = m_pDevice->DeletePatch(Handle);

			return hr;
		}
	}
}