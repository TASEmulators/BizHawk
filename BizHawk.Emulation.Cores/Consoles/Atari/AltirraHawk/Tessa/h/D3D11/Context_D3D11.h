#ifndef f_D3D11_CONTEXT_D3D11_H
#define f_D3D11_CONTEXT_D3D11_H

#include <vd2/system/profile.h>
#include <vd2/system/thread.h>
#include <vd2/system/vdstl.h>
#include <vd2/Tessa/Context.h>

struct ID3D11Device;
struct ID3D11PixelShader;
struct ID3D11VertexShader;
struct ID3D11InputLayout;
struct IDXGISwapChain;
struct IDXGISwapChain1;
struct IDXGIAdapter1;

class VDTContextD3D11;
class VDTResourceManagerD3D11;
class VDD3D11Holder;

///////////////////////////////////////////////////////////////////////////////
class VDTResourceD3D11 : public vdlist_node {
public:
	VDTResourceD3D11();
	virtual ~VDTResourceD3D11();

	virtual void Shutdown();

protected:
	friend class VDTResourceManagerD3D11;

	VDTResourceManagerD3D11 *mpParent;
};

class VDTResourceManagerD3D11 {
public:
	void AddResource(VDTResourceD3D11 *res);
	void RemoveResource(VDTResourceD3D11 *res);

	void ShutdownAllResources();

protected:
	typedef vdlist<VDTResourceD3D11> Resources;
	Resources mResources;
};

///////////////////////////////////////////////////////////////////////////////
class VDTReadbackBufferD3D11 final : public vdrefcounted<IVDTReadbackBuffer>, VDTResourceD3D11 {
public:
	VDTReadbackBufferD3D11();
	~VDTReadbackBufferD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, uint32 width, uint32 height, VDTFormat format);
	void Shutdown();

	bool Restore();
	bool Lock(VDTLockData2D& lockData);
	void Unlock();

protected:
	friend class VDTContextD3D11;
	friend class VDTSurfaceD3D11;
	friend class VDTTexture2DD3D11;

	ID3D11Texture2D *mpSurface;
};

///////////////////////////////////////////////////////////////////////////////
class VDTSurfaceD3D11 final : public vdrefcounted<IVDTSurface>, VDTResourceD3D11 {
public:
	VDTSurfaceD3D11();
	~VDTSurfaceD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, uint32 width, uint32 height, VDTFormat format, VDTUsage usage);
	bool Init(VDTContextD3D11 *parent, ID3D11Texture2D *tex, ID3D11Texture2D *texsys, uint32 mipLevel, bool rt, bool onlyMip);
	void Shutdown();

	bool Restore();
	bool Readback(IVDTReadbackBuffer *target);
	void Load(uint32 dx, uint32 dy, const VDTInitData2D& srcData, uint32 bpr, uint32 h);
	void Copy(uint32 dx, uint32 dy, IVDTSurface *src, uint32 sx, uint32 sy, uint32 w, uint32 h);
	void GetDesc(VDTSurfaceDesc& desc);
	bool Lock(const vdrect32 *r, VDTLockData2D& lockData);
	void Unlock();

protected:
	friend class VDTContextD3D11;

	ID3D11Texture2D *mpTexture;
	ID3D11Texture2D *mpTextureSys;
	ID3D11RenderTargetView *mpRTView;
	uint32 mMipLevel;
	bool mbOnlyMip;
	VDTSurfaceDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////

class VDTTextureD3D11 : protected VDTResourceD3D11 {
public:
	enum { kTypeD3DShaderResView = 'dsrv' };

protected:
	friend class VDTContextD3D11;
};

///////////////////////////////////////////////////////////////////////////////
class VDTTexture2DD3D11 final : public VDTTextureD3D11, public vdrefcounted<IVDTTexture2D> {
public:
	VDTTexture2DD3D11();
	~VDTTexture2DD3D11();

	void *AsInterface(uint32 id);

	bool Init(VDTContextD3D11 *parent, uint32 width, uint32 height, VDTFormat format, uint32 mipcount, VDTUsage usage, const VDTInitData2D *initData);
	bool Init(VDTContextD3D11 *parent, ID3D11Texture2D *tex, ID3D11Texture2D *texsys);
	void Shutdown();

	bool Restore();
	void GetDesc(VDTTextureDesc& desc);
	IVDTSurface *GetLevelSurface(uint32 level);
	void Load(uint32 mip, uint32 x, uint32 y, const VDTInitData2D& srcData, uint32 w, uint32 h);
	bool Lock(uint32 mip, const vdrect32 *r, VDTLockData2D& lockData);
	void Unlock(uint32 mip);

protected:
	ID3D11Texture2D *mpTexture;
	ID3D11Texture2D *mpTextureSys;
	ID3D11ShaderResourceView *mpShaderResView;
	uint32	mWidth;
	uint32	mHeight;
	uint32	mMipCount;
	VDTUsage mUsage;
	VDTFormat mFormat;

	typedef vdfastvector<VDTSurfaceD3D11 *> Mipmaps;
	Mipmaps mMipmaps;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexBufferD3D11 final : public vdrefcounted<IVDTVertexBuffer>, VDTResourceD3D11 {
public:
	VDTVertexBufferD3D11();
	~VDTVertexBufferD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, uint32 size, bool dynamic, const void *initData);
	void Shutdown();

	bool Restore();
	bool Load(uint32 offset, uint32 size, const void *data);

protected:
	friend class VDTContextD3D11;

	ID3D11Buffer *mpVB;
	uint32 mByteSize;
	bool mbDynamic;
};

///////////////////////////////////////////////////////////////////////////////
class VDTIndexBufferD3D11 final : public vdrefcounted<IVDTIndexBuffer>, VDTResourceD3D11 {
public:
	VDTIndexBufferD3D11();
	~VDTIndexBufferD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, uint32 size, bool index32, bool dynamic, const void *initData);
	void Shutdown();

	bool Restore();
	bool Load(uint32 offset, uint32 size, const void *data);

protected:
	friend class VDTContextD3D11;

	ID3D11Buffer *mpIB;
	uint32 mByteSize;
	bool mbDynamic;
	bool mbIndex32;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexProgramD3D11;

class VDTVertexFormatD3D11 final : public vdrefcounted<IVDTVertexFormat>, VDTResourceD3D11 {
public:
	VDTVertexFormatD3D11();
	~VDTVertexFormatD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, const VDTVertexElement *elements, uint32 count, VDTVertexProgramD3D11 *vp);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;

	ID3D11InputLayout *mpVF;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexProgramD3D11 final : public vdrefcounted<IVDTVertexProgram>, VDTResourceD3D11 {
public:
	VDTVertexProgramD3D11();
	~VDTVertexProgramD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, VDTProgramFormat format, const void *data, uint32 size);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;
	friend class VDTVertexFormatD3D11;

	ID3D11VertexShader *mpVS;
	vdfastvector<uint8> mByteCode;
};

///////////////////////////////////////////////////////////////////////////////
class VDTFragmentProgramD3D11 final : public vdrefcounted<IVDTFragmentProgram>, VDTResourceD3D11 {
public:
	VDTFragmentProgramD3D11();
	~VDTFragmentProgramD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, VDTProgramFormat format, const void *data, uint32 size);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;

	ID3D11PixelShader *mpPS;
};

///////////////////////////////////////////////////////////////////////////////
class VDTBlendStateD3D11 final : public vdrefcounted<IVDTBlendState>, VDTResourceD3D11 {
public:
	VDTBlendStateD3D11();
	~VDTBlendStateD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, const VDTBlendStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;

	ID3D11BlendState *mpBlendState;
	VDTBlendStateDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////
class VDTRasterizerStateD3D11 final : public vdrefcounted<IVDTRasterizerState>, VDTResourceD3D11 {
public:
	VDTRasterizerStateD3D11();
	~VDTRasterizerStateD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, const VDTRasterizerStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;

	ID3D11RasterizerState *mpRastState;
	VDTRasterizerStateDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////
class VDTSamplerStateD3D11 final : public vdrefcounted<IVDTSamplerState>, VDTResourceD3D11 {
public:
	VDTSamplerStateD3D11();
	~VDTSamplerStateD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, const VDTSamplerStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D11;

	ID3D11SamplerState *mpSamplerState;
	VDTSamplerStateDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////
class VDTSwapChainD3D11 final : public vdrefcounted<IVDTSwapChain>, VDTResourceD3D11, VDThread {
public:
	VDTSwapChainD3D11();
	~VDTSwapChainD3D11();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D11 *parent, const VDTSwapChainDesc& desc);
	void Shutdown();

	bool Restore() { return true; }

	void GetDesc(VDTSwapChainDesc& desc);
	IVDTSurface *GetBackBuffer();

	bool ResizeBuffers(uint32 width, uint32 height);

	void Present();

	void PresentVSync(void *monitor, IVDTAsyncPresent *callback);
	void PresentVSyncComplete();
	void PresentVSyncAbort();

protected:
	friend class VDTContextD3D11;

	void ThreadRun();

	IDXGISwapChain *mpSwapChain;
	IDXGISwapChain1 *mpSwapChain1;

	VDTTexture2DD3D11 *mpTexture;

	VDTSwapChainDesc mDesc;

	VDSemaphore mVSyncPollPendingSema;
	bool	mbVSyncPending;
	VDCriticalSection mVSyncMutex;
	IVDTAsyncPresent *mpVSyncCallback;
	void	*mhVSyncMonitor;
	bool	mbVSyncPollPending;
	bool	mbVSyncExit;
	uint32	mAdapterLuidLo;
	uint32	mAdapterLuidHi;
	HANDLE	mWaitHandle;
};

///////////////////////////////////////////////////////////////////////////////
class VDTContextD3D11 final : public IVDTContext, public IVDTProfiler, public VDTResourceManagerD3D11, public VDAlignedObject<16> {
public:
	VDTContextD3D11();
	~VDTContextD3D11();

	int AddRef();
	int Release();
	void *AsInterface(uint32 id);

	bool Init(ID3D11Device *dev, ID3D11DeviceContext *devctx, IDXGIAdapter1 *adapter, IDXGIFactory *factory, VDD3D11Holder *dllHolder);
	void Shutdown();

	VDD3D11Holder *GetD3D11Holder() const { return mpD3DHolder; }
	IDXGIFactory *GetDXGIFactory() const { return mpDXGIFactory; }
	IDXGIAdapter1 *GetDXGIAdapter() const { return mpDXGIAdapter; }
	ID3D11Device *GetDeviceD3D11() const { return mpD3DDevice; }
	ID3D11DeviceContext *GetDeviceContextD3D11() const { return mpD3DDeviceContext; }

	void SetImplicitSwapChain(VDTSwapChainD3D11 *sc);

	const VDTDeviceCaps& GetDeviceCaps() { return mCaps; }
	bool IsFormatSupportedTexture2D(VDTFormat format);

	bool CreateReadbackBuffer(uint32 width, uint32 height, VDTFormat format, IVDTReadbackBuffer **buffer);
	bool CreateSurface(uint32 width, uint32 height, VDTFormat format, VDTUsage usage, IVDTSurface **surface);
	bool CreateTexture2D(uint32 width, uint32 height, VDTFormat format, uint32 mipcount, VDTUsage usage, const VDTInitData2D *initData, IVDTTexture2D **tex);
	bool CreateVertexProgram(VDTProgramFormat format, VDTData data, IVDTVertexProgram **tex);
	bool CreateFragmentProgram(VDTProgramFormat format, VDTData data, IVDTFragmentProgram **tex);
	bool CreateVertexFormat(const VDTVertexElement *elements, uint32 count, IVDTVertexProgram *vp, IVDTVertexFormat **format);
	bool CreateVertexBuffer(uint32 size, bool dynamic, const void *initData, IVDTVertexBuffer **buffer);
	bool CreateIndexBuffer(uint32 size, bool index32, bool dynamic, const void *initData, IVDTIndexBuffer **buffer);

	bool CreateBlendState(const VDTBlendStateDesc& desc, IVDTBlendState **state);
	bool CreateRasterizerState(const VDTRasterizerStateDesc& desc, IVDTRasterizerState **state);
	bool CreateSamplerState(const VDTSamplerStateDesc& desc, IVDTSamplerState **state);

	bool CreateSwapChain(const VDTSwapChainDesc& desc, IVDTSwapChain **swapChain);

	IVDTSurface *GetRenderTarget(uint32 index) const;

	void SetVertexFormat(IVDTVertexFormat *format);
	void SetVertexProgram(IVDTVertexProgram *program);
	void SetFragmentProgram(IVDTFragmentProgram *program);
	void SetVertexStream(uint32 index, IVDTVertexBuffer *buffer, uint32 offset, uint32 stride);
	void SetIndexStream(IVDTIndexBuffer *buffer);
	void SetRenderTarget(uint32 index, IVDTSurface *surface);

	void SetBlendState(IVDTBlendState *state);
	void SetSamplerStates(uint32 baseIndex, uint32 count, IVDTSamplerState *const *states);
	void SetTextures(uint32 baseIndex, uint32 count, IVDTTexture *const *textures);

	// rasterizer
	void SetRasterizerState(IVDTRasterizerState *state);
	VDTViewport GetViewport();
	void SetViewport(const VDTViewport& vp);
	vdrect32 GetScissorRect();
	void SetScissorRect(const vdrect32& r);

	void SetVertexProgramConstCount(uint32 count);
	void SetVertexProgramConstF(uint32 baseIndex, uint32 count, const float *data);
	void SetFragmentProgramConstCount(uint32 count);
	void SetFragmentProgramConstF(uint32 baseIndex, uint32 count, const float *data);

	void Clear(VDTClearFlags clearFlags, uint32 color, float depth, uint32 stencil);
	void DrawPrimitive(VDTPrimitiveType type, uint32 startVertex, uint32 primitiveCount);
	void DrawIndexedPrimitive(VDTPrimitiveType type, uint32 baseVertexIndex, uint32 minVertex, uint32 vertexCount, uint32 startIndex, uint32 primitiveCount);

	uint32 InsertFence();
	bool CheckFence(uint32 id);

	bool RecoverDevice();
	bool OpenScene();
	bool CloseScene();
	bool IsDeviceLost() const { return false; }
	uint32 GetDeviceLossCounter() const;
	void Present();

	void SetGpuPriority(int priority) {}

public:
	void BeginScope(uint32 color, const char *message);
	void EndScope();
	VDRTProfileChannel *GetProfileChannel();

public:
	void UnsetVertexFormat(IVDTVertexFormat *format);
	void UnsetVertexProgram(IVDTVertexProgram *program);
	void UnsetFragmentProgram(IVDTFragmentProgram *program);
	void UnsetVertexBuffer(IVDTVertexBuffer *buffer);
	void UnsetIndexBuffer(IVDTIndexBuffer *buffer);
	void UnsetRenderTarget(IVDTSurface *surface);

	void UnsetBlendState(IVDTBlendState *state);
	void UnsetRasterizerState(IVDTRasterizerState *state);
	void UnsetSamplerState(IVDTSamplerState *state);
	void UnsetTexture(IVDTTexture *tex);

	void ProcessHRESULT(uint32 hr);

protected:
	void UpdateConstants();

	struct PrivateData;

	VDAtomicInt	mRefCount;
	PrivateData *mpData;

	VDD3D11Holder *mpD3DHolder;
	IDXGIFactory *mpDXGIFactory;
	IDXGIAdapter1 *mpDXGIAdapter;
	ID3D11Device *mpD3DDevice;
	ID3D11DeviceContext *mpD3DDeviceContext;

	static const uint32 kConstBaseShift = 4;
	static const uint32 kConstMaxShift = 5;
	static const uint8 kConstLookup[17];

	ID3D11Buffer *mpVSConstBuffers[kConstMaxShift] = {};
	ID3D11Buffer *mpPSConstBuffers[kConstMaxShift] = {};
	uint32 mVSConstShift = 0;
	uint32 mPSConstShift = 0;
	bool mbVSConstDirty = true;
	bool mbPSConstDirty = true;

	VDTSwapChainD3D11 *mpSwapChain;
	VDTSurfaceD3D11 *mpCurrentRT;
	VDTVertexBufferD3D11 *mpCurrentVB;
	uint32 mCurrentVBOffset;
	uint32 mCurrentVBStride;
	VDTIndexBufferD3D11 *mpCurrentIB;
	VDTVertexProgramD3D11 *mpCurrentVP;
	VDTFragmentProgramD3D11 *mpCurrentFP;
	VDTVertexFormatD3D11 *mpCurrentVF;

	VDTBlendStateD3D11 *mpCurrentBS;
	VDTRasterizerStateD3D11 *mpCurrentRS;

	VDTBlendStateD3D11 *mpDefaultBS;
	VDTRasterizerStateD3D11 *mpDefaultRS;
	VDTSamplerStateD3D11 *mpDefaultSS;

	VDTViewport mViewport;
	vdrect32 mScissorRect;
	VDTDeviceCaps mCaps;

	VDTSamplerStateD3D11 *mpCurrentSamplerStates[16];
	IVDTTexture *mpCurrentTextures[16];

	void	*mpBeginEvent;
	void	*mpEndEvent;
	VDRTProfileChannel	mProfChan;

	alignas(16) float mVSConsts[16][4] = {};
	alignas(16) float mPSConsts[16][4] = {};
};

bool VDTCreateContextD3D11(IVDTContext **ppctx);
bool VDTCreateContextD3D11(ID3D11Device *dev, IVDTContext **ppctx);

#endif	// f_D3D11_CONTEXT_D3D11_H
