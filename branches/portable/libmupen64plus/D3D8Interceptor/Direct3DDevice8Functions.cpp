#include "d3d8Wrapper.h"

extern "C"
{
	namespace D3D8Wrapper
	{
		ThreadSafePointerSet IDirect3DDevice8::m_List;

		D3D8Wrapper::IDirect3DDevice8::IDirect3DDevice8(D3D8Base::IDirect3DDevice8* realDevice) : IDirect3DUnknown((IUnknown*) realDevice)
		{
			LOG("IDirect3DDevice8::IDirect3DDevice8( " << realDevice << " )\n");
			m_pDevice = realDevice;
		}

		D3D8Wrapper::IDirect3DDevice8* D3D8Wrapper::IDirect3DDevice8::GetDirect3DDevice(D3D8Base::IDirect3DDevice8* realDevice)
		{
			LOG("IDirect3DDevice8::GetDirect3DDevice( " << realDevice << " )\n");
			D3D8Wrapper::IDirect3DDevice8* wrappedDevice = (D3D8Wrapper::IDirect3DDevice8*) m_List.GetDataPtr(realDevice);
			if(wrappedDevice == NULL)
			{
				wrappedDevice = new D3D8Wrapper::IDirect3DDevice8(realDevice);
				m_List.AddMember(realDevice, wrappedDevice);
				return wrappedDevice;
			}

			wrappedDevice->m_ulRef++;
			return wrappedDevice;
		} 

		STDMETHODIMP_(ULONG) D3D8Wrapper::IDirect3DDevice8::Release(THIS)
		{
			LOG("IDirect3DDevice8::Release() [ " << this << " ]\n");
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
			LOG("IDirect3DDevice8::TestCooperativeLevel() [ " << this << " ]\n");
			return m_pDevice->TestCooperativeLevel();
		}

		STDMETHODIMP_(UINT) D3D8Wrapper::IDirect3DDevice8::GetAvailableTextureMem()
		{
			LOG("IDirect3DDevice8::GetAvailableTextureMem() [ " << this << " ]\n");
			return m_pDevice->GetAvailableTextureMem();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ResourceManagerDiscardBytes(DWORD Bytes)
		{
			LOG("IDirect3DDevice8::ResourceManagerDiscardBytes( " << Bytes << " ) [ " << this << " ]\n");
			return m_pDevice->ResourceManagerDiscardBytes(Bytes);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDirect3D(D3D8Wrapper::IDirect3D8** ppD3D8)
		{
			LOG("IDirect3DDevice8::GetDirect3D( " << ppD3D8 << " ) [ " << this << " ]\n");

			// Run the function and wrap the result before returning it
			D3D8Base::IDirect3D8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetDirect3D(&realD3D);

			D3D8Wrapper::IDirect3D8* wrappedD3D = D3D8Wrapper::IDirect3D8::GetDirect3D(realD3D);

			*ppD3D8 = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDeviceCaps(D3D8Base::D3DCAPS8* pCaps)
		{
			LOG("IDirect3DDevice8::GetDeviceCaps( " << pCaps << " ) [ " << this << " ]\n");
			return m_pDevice->GetDeviceCaps(pCaps);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDisplayMode(D3D8Base::D3DDISPLAYMODE* pMode)
		{
			LOG("IDirect3DDevice8::GetDisplayMode( " << pMode << " ) [ " << this << " ]\n");
			return m_pDevice->GetDisplayMode(pMode);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCreationParameters(D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters)
		{
			LOG("IDirect3DDevice8::GetCreationParameters( " << pParameters << " ) [ " << this << " ]\n");
			return m_pDevice->GetCreationParameters(pParameters);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCursorProperties(UINT XHotSpot,UINT YHotSpot,D3D8Wrapper::IDirect3DSurface8* pCursorBitmap)
		{
			LOG("IDirect3DDevice8::SetCursorProperties( " << XHotSpot << " , " << YHotSpot << " , " << pCursorBitmap << " ) [ " << this << " ]\n");
			if (pCursorBitmap == NULL)
			{
				return m_pDevice->SetCursorProperties(XHotSpot,YHotSpot,NULL);
			}
			else
			{
				return m_pDevice->SetCursorProperties(XHotSpot,YHotSpot,pCursorBitmap->GetSurface());
			}
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetCursorPosition(int X,int Y,DWORD Flags)
		{
			LOG("IDirect3DDevice8::SetCursorPosition( " << X << " , " << Y << " , " << Flags << " ) [ " << this << " ]\n");
			m_pDevice->SetCursorPosition(X,Y,Flags);
		}

		STDMETHODIMP_(BOOL) D3D8Wrapper::IDirect3DDevice8::ShowCursor(BOOL bShow)
		{
			LOG("IDirect3DDevice8::ShowCursor( " << bShow << " ) [ " << this << " ]\n");
			return m_pDevice->ShowCursor(bShow);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateAdditionalSwapChain(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DSwapChain8** pSwapChain)
		{
			LOG("IDirect3DDevice8::CreateAdditionalSwapChain( " << pPresentationParameters << " , " << pSwapChain << " ) [ " << this << " ]\n");
			D3D8Base::IDirect3DSwapChain8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateAdditionalSwapChain(pPresentationParameters,&realD3D);

			D3D8Wrapper::IDirect3DSwapChain8* wrappedD3D = new D3D8Wrapper::IDirect3DSwapChain8(realD3D);

			*pSwapChain = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Reset(D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters)
		{
			LOG("IDirect3DDevice8::Reset( " << pPresentationParameters << " ) [ " << this << " ]\n");
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
			LOG(" , " << hDestWindowOverride << " , " << pDirtyRegion << " ) [ " << this << " ]\n");
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
			LOG("IDirect3DDevice8::GetBackBuffer( " << BackBuffer << " , " << Type << " , " << ppBackBuffer << " ) [ " << this << " ]\n");

			/*
			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetBackBuffer(BackBuffer,Type,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);
			*/

			// Return a pointer to our render surface, not the back buffer
			*ppBackBuffer = render_surface;// wrappedD3D;
			render_surface->m_ulRef++;

			return S_OK;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRasterStatus(D3D8Base::D3DRASTER_STATUS* pRasterStatus)
		{
			LOG("IDirect3DDevice8::GetRasterStatus( " << pRasterStatus << " ) [ " << this << " ]\n");
			return m_pDevice->GetRasterStatus(pRasterStatus);
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::SetGammaRamp(DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("IDirect3DDevice8::SetGammaRamp( " << Flags << " , " << pRamp << " ) [ " << this << " ]\n");
			m_pDevice->SetGammaRamp(Flags,pRamp);
		}

		STDMETHODIMP_(void) D3D8Wrapper::IDirect3DDevice8::GetGammaRamp(D3D8Base::D3DGAMMARAMP* pRamp)
		{
			LOG("IDirect3DDevice8::GetGammaRamp( " << pRamp << " ) [ " << this << " ]\n");
			m_pDevice->GetGammaRamp(pRamp);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateTexture(UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DTexture8** ppTexture)
		{
			LOG("IDirect3DDevice8::CreateTexture( " << Width << " , " << Height << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppTexture << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateTexture(Width,Height,Levels,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DTexture8* wrappedD3D = D3D8Wrapper::IDirect3DTexture8::GetTexture(realD3D);

			*ppTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVolumeTexture(UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVolumeTexture8** ppVolumeTexture)
		{
			LOG("IDirect3DDevice8::CreateVolumeTexture( " << Width << " , " << Height << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppVolumeTexture << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DVolumeTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateVolumeTexture(Width,Height,Depth,Levels,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DVolumeTexture8* wrappedD3D = new D3D8Wrapper::IDirect3DVolumeTexture8(realD3D);

			*ppVolumeTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateCubeTexture(UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DCubeTexture8** ppCubeTexture)
		{
			LOG("IDirect3DDevice8::CreateCubeTexture( " << EdgeLength << " , " << Levels << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppCubeTexture << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DCubeTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateCubeTexture(EdgeLength,Levels, Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DCubeTexture8* wrappedD3D = new D3D8Wrapper::IDirect3DCubeTexture8(realD3D);

			*ppCubeTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexBuffer(UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVertexBuffer8** ppVertexBuffer)
		{
			LOG("IDirect3DDevice8::CreateVertexBuffer( " << Length << " , " << Usage << " , " << FVF << " , " << Pool << " , " << ppVertexBuffer << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DVertexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateVertexBuffer(Length,Usage,FVF,Pool,&realD3D);

			D3D8Wrapper::IDirect3DVertexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DVertexBuffer8(realD3D);

			*ppVertexBuffer = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateIndexBuffer(UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexBuffer)
		{
			LOG("IDirect3DDevice8::CreateIndexBuffer( " << Length << " , " << Usage << " , " << Format << " , " << Pool << " , " << ppIndexBuffer << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DIndexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateIndexBuffer(Length,Usage,Format,Pool,&realD3D);

			D3D8Wrapper::IDirect3DIndexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DIndexBuffer8(realD3D);

			*ppIndexBuffer = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateRenderTarget(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateRenderTarget( " << Width << " , " << Height << " , " << Format << " , " << MultiSample << " , " << Lockable << " , " << ppSurface << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateRenderTarget(Width,Height,Format,MultiSample,Lockable,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateDepthStencilSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateDepthStencilSurface( " << Width << " , " << Height << " , " << Format << " , " << MultiSample << " , " << ppSurface << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateDepthStencilSurface(Width,Height,Format,MultiSample,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateImageSurface(UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Wrapper::IDirect3DSurface8** ppSurface)
		{
			LOG("IDirect3DDevice8::CreateImageSurface( " << Width << " , " << Height << " , " << Format << " , " << ppSurface << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->CreateImageSurface(Width,Height,Format,&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CopyRects(D3D8Wrapper::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Wrapper::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray)
		{
			LOG("IDirect3DDevice8::CopyRects( " << pSourceSurface << " , " << pSourceRectsArray << " , " << cRects << " , " << pDestinationSurface << " , " << pDestPointsArray << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* source;
			if (pSourceSurface == NULL)
			{
				source = NULL;
			}
			else
			{
				source = pSourceSurface->GetSurface();
			}

			D3D8Base::IDirect3DSurface8* destination;
			if (pDestinationSurface == NULL)
			{
				destination = NULL;
			}
			else
			{
				destination = pDestinationSurface->GetSurface();
			}

			if (pSourceSurface->m_ulRef == 0 || source == destination)
			{
				return D3DERR_INVALIDCALL;
			}

			return m_pDevice->CopyRects(source,pSourceRectsArray,cRects,destination,pDestPointsArray);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::UpdateTexture(D3D8Wrapper::IDirect3DBaseTexture8* pSourceTexture,D3D8Wrapper::IDirect3DBaseTexture8* pDestinationTexture)
		{
			LOG("IDirect3DDevice8::UpdateTexture( " << pSourceTexture << " , " << pDestinationTexture << " ) [ " << this << " ]\n");
			D3D8Base::IDirect3DBaseTexture8* source;
			if (pSourceTexture == NULL)
			{
				source = NULL;
			}
			else
			{
				source = pSourceTexture->GetBaseTexture();
			}

			D3D8Base::IDirect3DBaseTexture8* destination;
			if (pDestinationTexture == NULL)
			{
				destination = NULL;
			}
			else
			{
				destination = pDestinationTexture->GetBaseTexture();
			}

			return m_pDevice->UpdateTexture(source,destination);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetFrontBuffer(D3D8Wrapper::IDirect3DSurface8* pDestSurface)
		{
			LOG("IDirect3DDevice8::GetFrontBuffer( " << pDestSurface << " ) [ " << this << " ]\n");
			if (pDestSurface == NULL)
			{
				return m_pDevice->GetFrontBuffer(NULL);
			}
			else
			{
				return m_pDevice->GetFrontBuffer(pDestSurface->GetSurface());
			}
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderTarget(D3D8Wrapper::IDirect3DSurface8* pRenderTarget,D3D8Wrapper::IDirect3DSurface8* pNewZStencil)
		{
			LOG("IDirect3DDevice8::SetRenderTarget( " << pRenderTarget << " , " << pNewZStencil << " ) [ " << this << " ]\n");

			//HRESULT hr = m_pDevice->SetRenderTarget(pRenderTarget->GetSurface(),pNewZStencil->GetSurface());
			HRESULT hr = m_pDevice->SetRenderTarget(render_surface->GetSurface(),pNewZStencil->GetSurface());

			pRenderTarget->m_ulRef++;
			pNewZStencil->m_ulRef++;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderTarget(D3D8Wrapper::IDirect3DSurface8** ppRenderTarget)
		{
			LOG("IDirect3DDevice8::GetRenderTarget( " << ppRenderTarget << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetRenderTarget(&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppRenderTarget = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetDepthStencilSurface(D3D8Wrapper::IDirect3DSurface8** ppZStencilSurface)
		{
			LOG("IDirect3DDevice8::GetDepthStencilSurface( " << ppZStencilSurface << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DSurface8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetDepthStencilSurface(&realD3D);

			D3D8Wrapper::IDirect3DSurface8* wrappedD3D = D3D8Wrapper::IDirect3DSurface8::GetSurface(realD3D);

			*ppZStencilSurface = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginScene()
		{
			LOG("IDirect3DDevice8::BeginScene() [ " << this << " ]\n");
			return m_pDevice->BeginScene();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndScene()
		{
			LOG("IDirect3DDevice8::EndScene() [ " << this << " ]\n");
			return m_pDevice->EndScene();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::Clear(DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil)
		{
#ifdef LOGGING
			LOG("IDirect3DDevice8::Clear( " << Count << " , " << pRects);
			if (pRects != NULL)
			{
				LOG("{ " << pRects->x1 << " , " << pRects->y1 << " , " << pRects->x2 << " , " << pRects->y2 << " }")
			}
			LOG(" , " << Flags << " , " << Color << " , " << Z << " , " << Stencil << " ) [ " << this << " ]\n");
#endif			

			return m_pDevice->Clear(Count,pRects,Flags,Color,Z,Stencil);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("IDirect3DDevice8::SetTransform( " << State << " , " << pMatrix << " ) [ " << this << " ]\n");
			return m_pDevice->SetTransform(State,pMatrix);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTransform(D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix)
		{
			LOG("IDirect3DDevice8::GetTransform( " << State << " , " << pMatrix << " ) [ " << this << " ]\n");
			return m_pDevice->GetTransform(State,pMatrix);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::MultiplyTransform(D3D8Base::D3DTRANSFORMSTATETYPE foo,CONST D3D8Base::D3DMATRIX* bar)
		{
			LOG("IDirect3DDevice8::MultiplyTransform( " << foo << " , " << bar << " ) [ " << this << " ]\n");
			return m_pDevice->MultiplyTransform(foo, bar);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetViewport(CONST D3D8Base::D3DVIEWPORT8* pViewport)
		{
			LOG("IDirect3DDevice8::SetViewport( " << pViewport << " ) [ " << this << " ]\n");
			return m_pDevice->SetViewport(pViewport);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetViewport(D3D8Base::D3DVIEWPORT8* pViewport)
		{
#ifdef LOGGING
			LOG("IDirect3DDevice8::GetViewport( " << pViewport);
			if (pViewport != NULL)
			{
				LOG("{ " << pViewport->X << " , " << pViewport->Y << " , " << pViewport->Width << " , " << pViewport->Height << " , " << pViewport->MinZ << " , " << pViewport->MaxZ << " }");
			}
			LOG(" ) [ " << this << " ]\n");
#endif
			return m_pDevice->GetViewport(pViewport);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetMaterial(CONST D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("IDirect3DDevice8::SetMaterial( " << pMaterial << " ) [ " << this << " ]\n");
			return m_pDevice->SetMaterial(pMaterial);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetMaterial(D3D8Base::D3DMATERIAL8* pMaterial)
		{
			LOG("IDirect3DDevice8::GetMaterial( " << pMaterial << " ) [ " << this << " ]\n");
			return m_pDevice->GetMaterial(pMaterial);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetLight(DWORD Index,CONST D3D8Base::D3DLIGHT8* foo)
		{
			LOG("IDirect3DDevice8::SetLight( " << Index << " , " << foo << " ) [ " << this << " ]\n");
			return m_pDevice->SetLight(Index,foo);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLight(DWORD Index,D3D8Base::D3DLIGHT8* foo)
		{
			LOG("IDirect3DDevice8::GetLight( " << Index << " , " << foo << " ) [ " << this << " ]\n");
			return m_pDevice->GetLight(Index,foo);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::LightEnable(DWORD Index,BOOL Enable)
		{
			LOG("IDirect3DDevice8::LightEnable( " << Index << " , " << Enable << " ) [ " << this << " ]\n");
			return m_pDevice->LightEnable(Index,Enable);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetLightEnable(DWORD Index,BOOL* pEnable)
		{
			LOG("IDirect3DDevice8::GetLightEnable( " << Index << " , " << pEnable << " ) [ " << this << " ]\n");
			return m_pDevice->GetLightEnable(Index,pEnable);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipPlane(DWORD Index,CONST float* pPlane)
		{
			LOG("IDirect3DDevice8::SetClipPlane( " << Index << " , " << pPlane << " ) [ " << this << " ]\n");
			return m_pDevice->SetClipPlane(Index,pPlane);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipPlane(DWORD Index,float* pPlane)
		{
			LOG("IDirect3DDevice8::GetClipPlane( " << Index << " , " << pPlane << " ) [ " << this << " ]\n");
			return m_pDevice->GetClipPlane(Index,pPlane);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD Value)
		{
			LOG("IDirect3DDevice8::SetRenderState( " << State << " , " << Value << " ) [ " << this << " ]\n");
			return m_pDevice->SetRenderState(State,Value);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetRenderState(D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue)
		{
			LOG("IDirect3DDevice8::GetRenderState( " << State << " , " << pValue << " ) [ " << this << " ]\n");
			return m_pDevice->GetRenderState(State,pValue);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::BeginStateBlock()
		{
			LOG("IDirect3DDevice8::BeginStateBlock() [ " << this << " ]\n");
			return m_pDevice->BeginStateBlock();
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::EndStateBlock(DWORD* pToken)
		{
			LOG("IDirect3DDevice8::EndStateBlock( " << pToken << " ) [ " << this << " ]\n");
			return m_pDevice->EndStateBlock(pToken);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ApplyStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::ApplyStateBlock( " << Token << " ) [ " << this << " ]\n");
			return m_pDevice->ApplyStateBlock(Token);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CaptureStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::CaptureStateBlock( " << Token << " ) [ " << this << " ]\n");
			return m_pDevice->CaptureStateBlock(Token);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteStateBlock(DWORD Token)
		{
			LOG("IDirect3DDevice8::DeleteStateBlock( " << Token << " ) [ " << this << " ]\n");
			return m_pDevice->DeleteStateBlock(Token);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateStateBlock(D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken)
		{
			LOG("IDirect3DDevice8::CreateStateBlock( " << Type << " , " << pToken << " ) [ " << this << " ]\n");
			return m_pDevice->CreateStateBlock(Type,pToken);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetClipStatus(CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("IDirect3DDevice8::SetClipStatus( " << pClipStatus << " ) [ " << this << " ]\n");
			return m_pDevice->SetClipStatus(pClipStatus);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetClipStatus(D3D8Base::D3DCLIPSTATUS8* pClipStatus)
		{
			LOG("IDirect3DDevice8::GetClipStatus( " << pClipStatus << " ) [ " << this << " ]\n");
			return m_pDevice->GetClipStatus(pClipStatus);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8** ppTexture)
		{
			LOG("IDirect3DDevice8::GetTexture( " << Stage << " , " << ppTexture << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DBaseTexture8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetTexture(Stage,&realD3D);//ppTexture);

			D3D8Wrapper::IDirect3DBaseTexture8* wrappedD3D = new D3D8Wrapper::IDirect3DBaseTexture8(realD3D);

			*ppTexture = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTexture(DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8* pTexture)
		{
			LOG("IDirect3DDevice8::SetTexture( " << Stage << " , " << pTexture << " ) [ " << this << " ]\n");

			if (pTexture == NULL)
			{
				return m_pDevice->SetTexture(Stage,NULL);
			}
			else
			{
				return m_pDevice->SetTexture(Stage,pTexture->GetBaseTexture());
			}
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue)
		{
			LOG("IDirect3DDevice8::GetTextureStageState( " << Stage << " , " << Type << " , " << pValue << " ) [ " << this << " ]\n");
			return m_pDevice->GetTextureStageState(Stage,Type,pValue);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetTextureStageState(DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value)
		{
			LOG("IDirect3DDevice8::SetTextureStageState( " << Stage << " , " << Type << " , " << Value << " ) [ " << this << " ]\n");
			return m_pDevice->SetTextureStageState(Stage,Type,Value);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ValidateDevice(DWORD* pNumPasses)
		{
			LOG("IDirect3DDevice8::ValidateDevice( " << pNumPasses << " ) [ " << this << " ]\n");
			return m_pDevice->ValidateDevice(pNumPasses);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetInfo(DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize)
		{
			LOG("IDirect3DDevice8::GetInfo( " << DevInfoID << " , " << pDevInfoStruct << " , " << DevInfoStructSize << " ) [ " << this << " ]\n");
			return m_pDevice->GetInfo(DevInfoID,pDevInfoStruct,DevInfoStructSize);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPaletteEntries(UINT PaletteNumber,CONST PALETTEENTRY* pEntries)
		{
			LOG("IDirect3DDevice8::SetPaletteEntries( " << PaletteNumber << " , " << pEntries << " ) [ " << this << " ]\n");
			return m_pDevice->SetPaletteEntries(PaletteNumber,pEntries);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPaletteEntries(UINT PaletteNumber,PALETTEENTRY* pEntries)
		{
			LOG("IDirect3DDevice8::GetPaletteEntries( " << PaletteNumber << " , " << pEntries << " ) [ " << this << " ]\n");
			return m_pDevice->GetPaletteEntries(PaletteNumber,pEntries);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetCurrentTexturePalette(UINT PaletteNumber)
		{
			LOG("IDirect3DDevice8::SetCurrentTexturePalette( " << PaletteNumber << " ) [ " << this << " ]\n");
			return m_pDevice->SetCurrentTexturePalette(PaletteNumber);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetCurrentTexturePalette(UINT *PaletteNumber)
		{
			LOG("IDirect3DDevice8::GetCurrentTexturePalette( " << PaletteNumber << " ) [ " << this << " ]\n");
			return m_pDevice->GetCurrentTexturePalette(PaletteNumber);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount)
		{
			LOG("IDirect3DDevice8::DrawPrimitive( " << PrimitiveType << " , " << StartVertex << " , " << PrimitiveCount << " ) [ " << this << " ]\n");
			return m_pDevice->DrawPrimitive(PrimitiveType,StartVertex,PrimitiveCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitive(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount)
		{
			LOG("IDirect3DDevice8::DrawIndexedPrimitive( " << PrimitiveType << " , " << minIndex << " , " << NumVertices << " , " << startIndex << " , " << primCount << " ) [ " << this << " ]\n");
			return m_pDevice->DrawIndexedPrimitive(PrimitiveType,minIndex,NumVertices,startIndex,primCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("IDirect3DDevice8::DrawPrimitiveUP( " << PrimitiveType << " , " << PrimitiveCount << " , " << pVertexStreamZeroData << " , " << VertexStreamZeroStride << " ) [ " << this << " ]\n");
			return m_pDevice->DrawPrimitiveUP(PrimitiveType,PrimitiveCount,pVertexStreamZeroData,VertexStreamZeroStride);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawIndexedPrimitiveUP(D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride)
		{
			LOG("IDirect3DDevice8::DrawIndexedPrimitiveUP( " << PrimitiveType << " , " << MinVertexIndex << " , " << NumVertexIndices << " , " << PrimitiveCount << " , " << pIndexData << " , " << IndexDataFormat << " , " << pVertexStreamZeroData << " , " << VertexStreamZeroStride << " ) [ " << this << " ]\n");
			return m_pDevice->DrawIndexedPrimitiveUP(PrimitiveType,MinVertexIndex,NumVertexIndices,PrimitiveCount,pIndexData,IndexDataFormat,pVertexStreamZeroData,VertexStreamZeroStride);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::ProcessVertices(UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Wrapper::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags)
		{
			LOG("IDirect3DDevice8::ProcessVertices( " << SrcStartIndex << " , " << DestIndex << " , " << VertexCount << " , " << pDestBuffer << " , " << Flags << " ) [ " << this << " ]\n");
			if (pDestBuffer == NULL)
			{
				return m_pDevice->ProcessVertices(SrcStartIndex,DestIndex,VertexCount,NULL,Flags);
			}
			else
			{
				return m_pDevice->ProcessVertices(SrcStartIndex,DestIndex,VertexCount,pDestBuffer->GetVertexBuffer(),Flags);
			}
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreateVertexShader(CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage)
		{
			LOG("IDirect3DDevice8::CreateVertexShader( " << pDeclaration << " , " << pFunction << " , " << pHandle << " , " << Usage << " ) [ " << this << " ]\n");
			return m_pDevice->CreateVertexShader(pDeclaration,pFunction,pHandle,Usage);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::SetVertexShader( " << Handle << " ) [ " << this << " ]\n");
			return m_pDevice->SetVertexShader(Handle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShader(DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::GetVertexShader( " << pHandle << " ) [ " << this << " ]\n");
			return m_pDevice->GetVertexShader(pHandle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeleteVertexShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::DeleteVertexShader( " << Handle << " ) [ " << this << " ]\n");
			return m_pDevice->DeleteVertexShader(Handle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetVertexShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::SetVertexShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " ) [ " << this << " ]\n");
			return m_pDevice->SetVertexShaderConstant(Register,pConstantData,ConstantCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::GetVertexShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " ) [ " << this << " ]\n");
			return m_pDevice->GetVertexShaderConstant(Register,pConstantData,ConstantCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderDeclaration(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetVertexShaderDeclaration( " << Handle << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pDevice->GetVertexShaderDeclaration(Handle,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetVertexShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetVertexShaderFunction( " << Handle << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pDevice->GetVertexShaderFunction(Handle,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8* pStreamData,UINT Stride)
		{
			LOG("IDirect3DDevice8::SetStreamSource( " << StreamNumber << " , " << pStreamData << " , " << Stride << " ) [ " << this << " ]\n");
			if (pStreamData == NULL)
			{
				return m_pDevice->SetStreamSource(StreamNumber,NULL,Stride);
			}
			else
			{
				return m_pDevice->SetStreamSource(StreamNumber,pStreamData->GetVertexBuffer(),Stride);
			}
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetStreamSource(UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride)
		{
			LOG("IDirect3DDevice8::GetStreamSource( " << StreamNumber << " , " << ppStreamData << " , " << pStride << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DVertexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetStreamSource(StreamNumber,&realD3D,pStride);

			D3D8Wrapper::IDirect3DVertexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DVertexBuffer8(realD3D);

			*ppStreamData = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetIndices(D3D8Wrapper::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex)
		{
			LOG("IDirect3DDevice8::SetIndices( " << pIndexData << " , " << BaseVertexIndex << " ) [ " << this << " ]\n");
			if (pIndexData == NULL)
			{
				return m_pDevice->SetIndices(NULL,BaseVertexIndex);
			}
			else
			{
				return m_pDevice->SetIndices(pIndexData->GetIndexBuffer(),BaseVertexIndex);
			}
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetIndices(D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex)
		{
			LOG("IDirect3DDevice8::GetIndices( " << ppIndexData << " , " << pBaseVertexIndex << " ) [ " << this << " ]\n");

			D3D8Base::IDirect3DIndexBuffer8* realD3D = NULL;

			HRESULT hr = m_pDevice->GetIndices(&realD3D,pBaseVertexIndex);// ppIndexData,pBaseVertexIndex);

			D3D8Wrapper::IDirect3DIndexBuffer8* wrappedD3D = new D3D8Wrapper::IDirect3DIndexBuffer8(realD3D);

			*ppIndexData = wrappedD3D;

			return hr;
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::CreatePixelShader(CONST DWORD* pFunction,DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::CreatePixelShader( " << pFunction << " , " << pHandle << " ) [ " << this << " ]\n");
			return m_pDevice->CreatePixelShader(pFunction,pHandle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::SetPixelShader( " << Handle << " ) [ " << this << " ]\n");
			return m_pDevice->SetPixelShader(Handle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShader(DWORD* pHandle)
		{
			LOG("IDirect3DDevice8::GetPixelShader( " << pHandle << " ) [ " << this << " ]\n");
			return m_pDevice->GetPixelShader(pHandle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePixelShader(DWORD Handle)
		{
			LOG("IDirect3DDevice8::DeletePixelShader( " << Handle << " ) [ " << this << " ]\n");
			return m_pDevice->DeletePixelShader(Handle);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::SetPixelShaderConstant(DWORD Register,CONST void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::SetPixelShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " ) [ " << this << " ]\n");
			return m_pDevice->SetPixelShaderConstant(Register,pConstantData,ConstantCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderConstant(DWORD Register,void* pConstantData,DWORD ConstantCount)
		{
			LOG("IDirect3DDevice8::GetPixelShaderConstant( " << Register << " , " << pConstantData << " , " << ConstantCount << " ) [ " << this << " ]\n");
			return m_pDevice->GetPixelShaderConstant(Register,pConstantData,ConstantCount);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::GetPixelShaderFunction(DWORD Handle,void* pData,DWORD* pSizeOfData)
		{
			LOG("IDirect3DDevice8::GetPixelShaderFunction( " << Handle << " , " << pData << " , " << pSizeOfData << " ) [ " << this << " ]\n");
			return m_pDevice->GetPixelShaderFunction(Handle,pData,pSizeOfData);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawRectPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo)
		{
			LOG("IDirect3DDevice8::DrawRectPatch( " << Handle << " , " << pNumSegs << " , " << pRectPatchInfo << " ) [ " << this << " ]\n");
			return m_pDevice->DrawRectPatch(Handle,pNumSegs,pRectPatchInfo);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DrawTriPatch(UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo)
		{
			LOG("IDirect3DDevice8::DrawTriPatch( " << Handle << " , " << pNumSegs << " , " << pTriPatchInfo << " ) [ " << this << " ]\n");
			return m_pDevice->DrawTriPatch(Handle,pNumSegs,pTriPatchInfo);
		}

		STDMETHODIMP D3D8Wrapper::IDirect3DDevice8::DeletePatch(UINT Handle)
		{
			LOG("IDirect3DDevice8::DeletePatch( " << Handle << " ) [ " << this << " ]\n");
			return m_pDevice->DeletePatch(Handle);
		}
	}
}