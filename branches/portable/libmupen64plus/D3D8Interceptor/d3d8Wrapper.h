#pragma once

#include <iostream>
#include <fstream>
#include "PointerSet.h"

#pragma comment(linker, "/EXPORT:Direct3DCreate8=_Direct3DCreate8@4")

//#define LOGGING 1

#ifdef LOGGING
#define LOG(x) { std::ofstream myfile; myfile.open ("d3d8_wrapper_log.txt", std::ios::app); myfile << x; myfile.close(); }
#else
#define LOG(x) 
#endif

namespace D3D8Base
{
	#include "d3d8base/d3d8.h"
}
extern "C"
{
	namespace D3D8Wrapper
	{
		class IDirect3DUnknown
		{

		public:
			IUnknown*   m_pUnk;
			ULONG       m_ulRef;

		public:
			IDirect3DUnknown(IUnknown* pUnk)
			{
				m_pUnk = pUnk;
				m_ulRef = 1;
			}

			/*** IUnknown methods ***/
			STDMETHOD(QueryInterface)(THIS_ REFIID riid, void** ppvObj)
			{
				return E_FAIL;
			}

			STDMETHOD_(ULONG,AddRef)(THIS)
			{
				m_pUnk->AddRef();
				return ++m_ulRef;
			}

			STDMETHOD_(ULONG,Release)(THIS)
			{
				m_pUnk->Release();

				ULONG ulRef = --m_ulRef;
				if( 0 == ulRef )
				{
					delete this;
					return 0;
				}
				return ulRef;
			}
		};	

		class IDirect3D8;
		class IDirect3DDevice8;

		class IDirect3DResource8;
		class IDirect3DBaseTexture8;
		class IDirect3DTexture8;
		class IDirect3DVolumeTexture8;
		class IDirect3DCubeTexture8;

		class IDirect3DVertexBuffer8;
		class IDirect3DIndexBuffer8;

		class IDirect3DSurface8;
		class IDirect3DVolume8;

		class IDirect3DSwapChain8;
		
		class IDirect3D8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3D8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:
			IDirect3D8(D3D8Base::IDirect3D8*);

			inline D3D8Base::IDirect3D8* GetDirect3D8() { return m_pD3D; }
			static IDirect3D8* GetDirect3D(D3D8Base::IDirect3D8* pD3D);

			/*** IDirect3DUnknown methods ***/
			STDMETHOD_(ULONG,Release)(THIS);

			/*** IDirect3D8 methods ***/
			STDMETHOD(RegisterSoftwareDevice)(THIS_ void* pInitializeFunction);
			STDMETHOD_(UINT, GetAdapterCount)(THIS);
			STDMETHOD(GetAdapterIdentifier)(THIS_ UINT Adapter,DWORD Flags,D3D8Base::D3DADAPTER_IDENTIFIER8* pIdentifier);
			STDMETHOD_(UINT, GetAdapterModeCount)(THIS_ UINT Adapter);
			STDMETHOD(EnumAdapterModes)(THIS_ UINT Adapter,UINT Mode,D3D8Base::D3DDISPLAYMODE* pMode);
			STDMETHOD(GetAdapterDisplayMode)(THIS_ UINT Adapter,D3D8Base::D3DDISPLAYMODE* pMode);
			STDMETHOD(CheckDeviceType)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE CheckType,D3D8Base::D3DFORMAT DisplayFormat,D3D8Base::D3DFORMAT BackBufferFormat,BOOL Windowed);
			STDMETHOD(CheckDeviceFormat)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,DWORD Usage,D3D8Base::D3DRESOURCETYPE RType,D3D8Base::D3DFORMAT CheckFormat);
			STDMETHOD(CheckDeviceMultiSampleType)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT SurfaceFormat,BOOL Windowed,D3D8Base::D3DMULTISAMPLE_TYPE MultiSampleType);
			STDMETHOD(CheckDepthStencilMatch)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DFORMAT AdapterFormat,D3D8Base::D3DFORMAT RenderTargetFormat,D3D8Base::D3DFORMAT DepthStencilFormat);
			STDMETHOD(GetDeviceCaps)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,D3D8Base::D3DCAPS8* pCaps);
			STDMETHOD_(HMONITOR, GetAdapterMonitor)(THIS_ UINT Adapter);
			STDMETHOD(CreateDevice)(THIS_ UINT Adapter,D3D8Base::D3DDEVTYPE DeviceType,HWND hFocusWindow,DWORD BehaviorFlags,D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DDevice8** ppReturnedDeviceInterface);
		};

		class IDirect3DDevice8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3DDevice8*  m_pDevice;
			static ThreadSafePointerSet	m_List;
		public:
			STDMETHOD(QueryInterface)(THIS_ REFIID riid, void** ppvObj)
			{

				return E_FAIL;
			}

			static IDirect3DDevice8* GetDirect3DDevice(D3D8Base::IDirect3DDevice8* pDevice); 
			__forceinline D3D8Base::IDirect3DDevice8* GetD3D8Device() { return m_pDevice; }

			/*** IDirect3DUnknown methods ***/
			STDMETHOD_(ULONG,Release)(THIS);

			IDirect3DDevice8(D3D8Base::IDirect3DDevice8*);

			/*** IDirect3DDevice8 methods ***/
			STDMETHOD(TestCooperativeLevel)(THIS);
			STDMETHOD_(UINT, GetAvailableTextureMem)(THIS);
			STDMETHOD(ResourceManagerDiscardBytes)(THIS_ DWORD Bytes);
			STDMETHOD(GetDirect3D)(THIS_ D3D8Wrapper::IDirect3D8** ppD3D8);
			STDMETHOD(GetDeviceCaps)(THIS_ D3D8Base::D3DCAPS8* pCaps);
			STDMETHOD(GetDisplayMode)(THIS_ D3D8Base::D3DDISPLAYMODE* pMode);
			STDMETHOD(GetCreationParameters)(THIS_ D3D8Base::D3DDEVICE_CREATION_PARAMETERS *pParameters);
			STDMETHOD(SetCursorProperties)(THIS_ UINT XHotSpot,UINT YHotSpot,D3D8Wrapper::IDirect3DSurface8* pCursorBitmap);
			STDMETHOD_(void, SetCursorPosition)(THIS_ int X,int Y,DWORD Flags);
			STDMETHOD_(BOOL, ShowCursor)(THIS_ BOOL bShow);
			STDMETHOD(CreateAdditionalSwapChain)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters,D3D8Wrapper::IDirect3DSwapChain8** pSwapChain);
			STDMETHOD(Reset)(THIS_ D3D8Base::D3DPRESENT_PARAMETERS* pPresentationParameters);
			STDMETHOD(Present)(THIS_ CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion);
			STDMETHOD(GetBackBuffer)(THIS_ UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer);
			STDMETHOD(GetRasterStatus)(THIS_ D3D8Base::D3DRASTER_STATUS* pRasterStatus);
			STDMETHOD_(void, SetGammaRamp)(THIS_ DWORD Flags,CONST D3D8Base::D3DGAMMARAMP* pRamp);
			STDMETHOD_(void, GetGammaRamp)(THIS_ D3D8Base::D3DGAMMARAMP* pRamp);
			STDMETHOD(CreateTexture)(THIS_ UINT Width,UINT Height,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DTexture8** ppTexture);
			STDMETHOD(CreateVolumeTexture)(THIS_ UINT Width,UINT Height,UINT Depth,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVolumeTexture8** ppVolumeTexture);
			STDMETHOD(CreateCubeTexture)(THIS_ UINT EdgeLength,UINT Levels,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DCubeTexture8** ppCubeTexture);
			STDMETHOD(CreateVertexBuffer)(THIS_ UINT Length,DWORD Usage,DWORD FVF,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DVertexBuffer8** ppVertexBuffer);
			STDMETHOD(CreateIndexBuffer)(THIS_ UINT Length,DWORD Usage,D3D8Base::D3DFORMAT Format,D3D8Base::D3DPOOL Pool,D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexBuffer);
			STDMETHOD(CreateRenderTarget)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,BOOL Lockable,D3D8Wrapper::IDirect3DSurface8** ppSurface);
			STDMETHOD(CreateDepthStencilSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Base::D3DMULTISAMPLE_TYPE MultiSample,D3D8Wrapper::IDirect3DSurface8** ppSurface);
			STDMETHOD(CreateImageSurface)(THIS_ UINT Width,UINT Height,D3D8Base::D3DFORMAT Format,D3D8Wrapper::IDirect3DSurface8** ppSurface);
			STDMETHOD(CopyRects)(THIS_ D3D8Wrapper::IDirect3DSurface8* pSourceSurface,CONST RECT* pSourceRectsArray,UINT cRects,D3D8Wrapper::IDirect3DSurface8* pDestinationSurface,CONST POINT* pDestPointsArray);
			STDMETHOD(UpdateTexture)(THIS_ D3D8Wrapper::IDirect3DBaseTexture8* pSourceTexture,D3D8Wrapper::IDirect3DBaseTexture8* pDestinationTexture);
			STDMETHOD(GetFrontBuffer)(THIS_ D3D8Wrapper::IDirect3DSurface8* pDestSurface);
			STDMETHOD(SetRenderTarget)(THIS_ D3D8Wrapper::IDirect3DSurface8* pRenderTarget,D3D8Wrapper::IDirect3DSurface8* pNewZStencil);
			STDMETHOD(GetRenderTarget)(THIS_ D3D8Wrapper::IDirect3DSurface8** ppRenderTarget);
			STDMETHOD(GetDepthStencilSurface)(THIS_ D3D8Wrapper::IDirect3DSurface8** ppZStencilSurface);
			STDMETHOD(BeginScene)(THIS);
			STDMETHOD(EndScene)(THIS);
			STDMETHOD(Clear)(THIS_ DWORD Count,CONST D3D8Base::D3DRECT* pRects,DWORD Flags,D3D8Base::D3DCOLOR Color,float Z,DWORD Stencil);
			STDMETHOD(SetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,CONST D3D8Base::D3DMATRIX* pMatrix);
			STDMETHOD(GetTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE State,D3D8Base::D3DMATRIX* pMatrix);
			STDMETHOD(MultiplyTransform)(THIS_ D3D8Base::D3DTRANSFORMSTATETYPE,CONST D3D8Base::D3DMATRIX*);
			STDMETHOD(SetViewport)(THIS_ CONST D3D8Base::D3DVIEWPORT8* pViewport);
			STDMETHOD(GetViewport)(THIS_ D3D8Base::D3DVIEWPORT8* pViewport);
			STDMETHOD(SetMaterial)(THIS_ CONST D3D8Base::D3DMATERIAL8* pMaterial);
			STDMETHOD(GetMaterial)(THIS_ D3D8Base::D3DMATERIAL8* pMaterial);
			STDMETHOD(SetLight)(THIS_ DWORD Index,CONST D3D8Base::D3DLIGHT8*);
			STDMETHOD(GetLight)(THIS_ DWORD Index,D3D8Base::D3DLIGHT8*);
			STDMETHOD(LightEnable)(THIS_ DWORD Index,BOOL Enable);
			STDMETHOD(GetLightEnable)(THIS_ DWORD Index,BOOL* pEnable);
			STDMETHOD(SetClipPlane)(THIS_ DWORD Index,CONST float* pPlane);
			STDMETHOD(GetClipPlane)(THIS_ DWORD Index,float* pPlane);
			STDMETHOD(SetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD Value);
			STDMETHOD(GetRenderState)(THIS_ D3D8Base::D3DRENDERSTATETYPE State,DWORD* pValue);
			STDMETHOD(BeginStateBlock)(THIS);
			STDMETHOD(EndStateBlock)(THIS_ DWORD* pToken);
			STDMETHOD(ApplyStateBlock)(THIS_ DWORD Token);
			STDMETHOD(CaptureStateBlock)(THIS_ DWORD Token);
			STDMETHOD(DeleteStateBlock)(THIS_ DWORD Token);
			STDMETHOD(CreateStateBlock)(THIS_ D3D8Base::D3DSTATEBLOCKTYPE Type,DWORD* pToken);
			STDMETHOD(SetClipStatus)(THIS_ CONST D3D8Base::D3DCLIPSTATUS8* pClipStatus);
			STDMETHOD(GetClipStatus)(THIS_ D3D8Base::D3DCLIPSTATUS8* pClipStatus);
			STDMETHOD(GetTexture)(THIS_ DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8** ppTexture);
			STDMETHOD(SetTexture)(THIS_ DWORD Stage,D3D8Wrapper::IDirect3DBaseTexture8* pTexture);
			STDMETHOD(GetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD* pValue);
			STDMETHOD(SetTextureStageState)(THIS_ DWORD Stage,D3D8Base::D3DTEXTURESTAGESTATETYPE Type,DWORD Value);
			STDMETHOD(ValidateDevice)(THIS_ DWORD* pNumPasses);
			STDMETHOD(GetInfo)(THIS_ DWORD DevInfoID,void* pDevInfoStruct,DWORD DevInfoStructSize);
			STDMETHOD(SetPaletteEntries)(THIS_ UINT PaletteNumber,CONST PALETTEENTRY* pEntries);
			STDMETHOD(GetPaletteEntries)(THIS_ UINT PaletteNumber,PALETTEENTRY* pEntries);
			STDMETHOD(SetCurrentTexturePalette)(THIS_ UINT PaletteNumber);
			STDMETHOD(GetCurrentTexturePalette)(THIS_ UINT *PaletteNumber);
			STDMETHOD(DrawPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT StartVertex,UINT PrimitiveCount);
			STDMETHOD(DrawIndexedPrimitive)(THIS_ D3D8Base::D3DPRIMITIVETYPE,UINT minIndex,UINT NumVertices,UINT startIndex,UINT primCount);
			STDMETHOD(DrawPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT PrimitiveCount,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride);
			STDMETHOD(DrawIndexedPrimitiveUP)(THIS_ D3D8Base::D3DPRIMITIVETYPE PrimitiveType,UINT MinVertexIndex,UINT NumVertexIndices,UINT PrimitiveCount,CONST void* pIndexData,D3D8Base::D3DFORMAT IndexDataFormat,CONST void* pVertexStreamZeroData,UINT VertexStreamZeroStride);
			STDMETHOD(ProcessVertices)(THIS_ UINT SrcStartIndex,UINT DestIndex,UINT VertexCount,D3D8Wrapper::IDirect3DVertexBuffer8* pDestBuffer,DWORD Flags);
			STDMETHOD(CreateVertexShader)(THIS_ CONST DWORD* pDeclaration,CONST DWORD* pFunction,DWORD* pHandle,DWORD Usage);
			STDMETHOD(SetVertexShader)(THIS_ DWORD Handle);
			STDMETHOD(GetVertexShader)(THIS_ DWORD* pHandle);
			STDMETHOD(DeleteVertexShader)(THIS_ DWORD Handle);
			STDMETHOD(SetVertexShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount);
			STDMETHOD(GetVertexShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount);
			STDMETHOD(GetVertexShaderDeclaration)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData);
			STDMETHOD(GetVertexShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData);
			STDMETHOD(SetStreamSource)(THIS_ UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8* pStreamData,UINT Stride);
			STDMETHOD(GetStreamSource)(THIS_ UINT StreamNumber,D3D8Wrapper::IDirect3DVertexBuffer8** ppStreamData,UINT* pStride);
			STDMETHOD(SetIndices)(THIS_ D3D8Wrapper::IDirect3DIndexBuffer8* pIndexData,UINT BaseVertexIndex);
			STDMETHOD(GetIndices)(THIS_ D3D8Wrapper::IDirect3DIndexBuffer8** ppIndexData,UINT* pBaseVertexIndex);
			STDMETHOD(CreatePixelShader)(THIS_ CONST DWORD* pFunction,DWORD* pHandle);
			STDMETHOD(SetPixelShader)(THIS_ DWORD Handle);
			STDMETHOD(GetPixelShader)(THIS_ DWORD* pHandle);
			STDMETHOD(DeletePixelShader)(THIS_ DWORD Handle);
			STDMETHOD(SetPixelShaderConstant)(THIS_ DWORD Register,CONST void* pConstantData,DWORD ConstantCount);
			STDMETHOD(GetPixelShaderConstant)(THIS_ DWORD Register,void* pConstantData,DWORD ConstantCount);
			STDMETHOD(GetPixelShaderFunction)(THIS_ DWORD Handle,void* pData,DWORD* pSizeOfData);
			STDMETHOD(DrawRectPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DRECTPATCH_INFO* pRectPatchInfo);
			STDMETHOD(DrawTriPatch)(THIS_ UINT Handle,CONST float* pNumSegs,CONST D3D8Base::D3DTRIPATCH_INFO* pTriPatchInfo);
			STDMETHOD(DeletePatch)(THIS_ UINT Handle);
		};

		class IDirect3DSwapChain8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3DSwapChain8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:
			/*** IUnknown methods ***/
			STDMETHOD_(ULONG,Release)(THIS);

			static IDirect3DSwapChain8* GetSwapChain(D3D8Base::IDirect3DSwapChain8* pSwapChain);
			inline D3D8Base::IDirect3DSwapChain8* GetSwapChain8() { return m_pD3D; }
			
			IDirect3DSwapChain8(D3D8Base::IDirect3DSwapChain8*);

			STDMETHOD(Present)(THIS_ CONST RECT* pSourceRect,CONST RECT* pDestRect,HWND hDestWindowOverride,CONST RGNDATA* pDirtyRegion);
			STDMETHOD(GetBackBuffer)(THIS_ UINT BackBuffer,D3D8Base::D3DBACKBUFFER_TYPE Type,D3D8Wrapper::IDirect3DSurface8** ppBackBuffer);
		};

		class IDirect3DResource8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3DResource8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:
			/*** IUnknown methods ***/
			STDMETHOD_(ULONG,Release)(THIS);

			static IDirect3DResource8* GetResource(D3D8Base::IDirect3DResource8* pSwapChain);
			inline D3D8Base::IDirect3DResource8* GetResource() { return m_pD3D; }
			
			IDirect3DResource8(D3D8Base::IDirect3DResource8*);

			STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice);
			STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags);
			STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData);
			STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid);
			STDMETHOD_(DWORD, SetPriority)(THIS_ DWORD PriorityNew);
			STDMETHOD_(DWORD, GetPriority)(THIS);
			STDMETHOD_(void, PreLoad)(THIS);
			STDMETHOD_(D3D8Base::D3DRESOURCETYPE, GetType)(THIS);
		};



		class IDirect3DBaseTexture8 : public IDirect3DResource8
		{
		protected:
			D3D8Base::IDirect3DBaseTexture8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DBaseTexture8(D3D8Base::IDirect3DBaseTexture8*);

			inline D3D8Base::IDirect3DBaseTexture8* GetBaseTexture() { return m_pD3D; }

			STDMETHOD_(DWORD, SetLOD)(THIS_ DWORD LODNew);
			STDMETHOD_(DWORD, GetLOD)(THIS);
			STDMETHOD_(DWORD, GetLevelCount)(THIS);
		};


		class IDirect3DTexture8 : public IDirect3DBaseTexture8
		{
		protected:
			D3D8Base::IDirect3DTexture8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DTexture8(D3D8Base::IDirect3DTexture8*);

			static D3D8Wrapper::IDirect3DTexture8* GetTexture(D3D8Base::IDirect3DTexture8*);

			STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc);
			STDMETHOD(GetSurfaceLevel)(THIS_ UINT Level,D3D8Wrapper::IDirect3DSurface8** ppSurfaceLevel);
			STDMETHOD(LockRect)(THIS_ UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags);
			STDMETHOD(UnlockRect)(THIS_ UINT Level);
			STDMETHOD(AddDirtyRect)(THIS_ CONST RECT* pDirtyRect);
		};



		class IDirect3DVolumeTexture8 : public IDirect3DBaseTexture8
		{
		protected:
			D3D8Base::IDirect3DVolumeTexture8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DVolumeTexture8(D3D8Base::IDirect3DVolumeTexture8*);

			STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DVOLUME_DESC *pDesc);
			STDMETHOD(GetVolumeLevel)(THIS_ UINT Level,D3D8Wrapper::IDirect3DVolume8** ppVolumeLevel);
			STDMETHOD(LockBox)(THIS_ UINT Level,D3D8Base::D3DLOCKED_BOX* pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags);
			STDMETHOD(UnlockBox)(THIS_ UINT Level);
			STDMETHOD(AddDirtyBox)(THIS_ CONST D3D8Base::D3DBOX* pDirtyBox);
		};



		class IDirect3DCubeTexture8 : public IDirect3DBaseTexture8
		{
		protected:
			D3D8Base::IDirect3DCubeTexture8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DCubeTexture8(D3D8Base::IDirect3DCubeTexture8*);

			STDMETHOD(GetLevelDesc)(THIS_ UINT Level,D3D8Base::D3DSURFACE_DESC *pDesc);
			STDMETHOD(GetCubeMapSurface)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Wrapper::IDirect3DSurface8** ppCubeMapSurface);
			STDMETHOD(LockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level,D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags);
			STDMETHOD(UnlockRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,UINT Level);
			STDMETHOD(AddDirtyRect)(THIS_ D3D8Base::D3DCUBEMAP_FACES FaceType,CONST RECT* pDirtyRect);
		}; 


		class IDirect3DVertexBuffer8 : public IDirect3DResource8
		{
		protected:
			D3D8Base::IDirect3DVertexBuffer8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DVertexBuffer8(D3D8Base::IDirect3DVertexBuffer8*);

			inline D3D8Base::IDirect3DVertexBuffer8* GetVertexBuffer() { return m_pD3D; }

			STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags);
			STDMETHOD(Unlock)(THIS);
			STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVERTEXBUFFER_DESC *pDesc);
		};



		class IDirect3DIndexBuffer8 : public IDirect3DResource8
		{
		protected:
			D3D8Base::IDirect3DIndexBuffer8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DIndexBuffer8(D3D8Base::IDirect3DIndexBuffer8*);

			inline D3D8Base::IDirect3DIndexBuffer8* GetIndexBuffer() { return m_pD3D; }

			STDMETHOD(Lock)(THIS_ UINT OffsetToLock,UINT SizeToLock,BYTE** ppbData,DWORD Flags);
			STDMETHOD(Unlock)(THIS);
			STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DINDEXBUFFER_DESC *pDesc);
		}; 




		class IDirect3DSurface8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3DSurface8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DSurface8(D3D8Base::IDirect3DSurface8*);
			static IDirect3DSurface8* GetSurface(D3D8Base::IDirect3DSurface8* pSurface);
			inline D3D8Base::IDirect3DSurface8* GetSurface() { return m_pD3D; }

			/*** IDirect3DUnknown methods ***/
			STDMETHOD_(ULONG, Release)(THIS);

			STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice);
			STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags);
			STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData);
			STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid);
			STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer);
			STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DSURFACE_DESC *pDesc);
			STDMETHOD(LockRect)(THIS_ D3D8Base::D3DLOCKED_RECT* pLockedRect,CONST RECT* pRect,DWORD Flags);
			STDMETHOD(UnlockRect)(THIS);
		}; 




		class IDirect3DVolume8 : public IDirect3DUnknown
		{
		protected:
			D3D8Base::IDirect3DVolume8*		m_pD3D;
			static ThreadSafePointerSet	m_List;
		public:

			IDirect3DVolume8(D3D8Base::IDirect3DVolume8*);


			STDMETHOD(GetDevice)(THIS_ D3D8Wrapper::IDirect3DDevice8** ppDevice);
			STDMETHOD(SetPrivateData)(THIS_ REFGUID refguid,CONST void* pData,DWORD SizeOfData,DWORD Flags);
			STDMETHOD(GetPrivateData)(THIS_ REFGUID refguid,void* pData,DWORD* pSizeOfData);
			STDMETHOD(FreePrivateData)(THIS_ REFGUID refguid);
			STDMETHOD(GetContainer)(THIS_ REFIID riid,void** ppContainer);
			STDMETHOD(GetDesc)(THIS_ D3D8Base::D3DVOLUME_DESC *pDesc);
			STDMETHOD(LockBox)(THIS_ D3D8Base::D3DLOCKED_BOX * pLockedVolume,CONST D3D8Base::D3DBOX* pBox,DWORD Flags);
			STDMETHOD(UnlockBox)(THIS);
		};
		

		typedef D3D8Base::IDirect3D8* (WINAPI *D3DCREATE)(UINT);
		IDirect3D8* WINAPI Direct3DCreate8(UINT Version);
		extern IDirect3DDevice8 *last_device;
		extern IDirect3DSurface8 *render_surface;
		extern void (*rendering_callback)( int );
	}
}