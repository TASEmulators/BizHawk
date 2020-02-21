#ifndef f_D3D9_CONTEXT_D3D9_H
#define f_D3D9_CONTEXT_D3D9_H

#include <vd2/system/profile.h>
#include <vd2/system/vdstl.h>
#include <vd2/Tessa/Context.h>

struct IDirect3DDevice9;
struct IDirect3DPixelShader9;
struct IDirect3DVertexShader9;
struct IDirect3DVertexDeclaration9;

class VDTContextD3D9;
class VDTResourceManagerD3D9;

///////////////////////////////////////////////////////////////////////////////
class VDTResourceD3D9 : public vdlist_node {
public:
	VDTResourceD3D9();
	virtual ~VDTResourceD3D9();

	virtual void Shutdown();
	virtual void ShutdownDefaultPool();

protected:
	friend class VDTResourceManagerD3D9;

	VDTResourceManagerD3D9 *mpParent;
};

class VDTResourceManagerD3D9 {
public:
	void AddResource(VDTResourceD3D9 *res);
	void RemoveResource(VDTResourceD3D9 *res);

	void ShutdownDefaultPoolResources();
	void ShutdownAllResources();

protected:
	typedef vdlist<VDTResourceD3D9> Resources;
	Resources mResources;
};

///////////////////////////////////////////////////////////////////////////////
class VDTReadbackBufferD3D9 : public vdrefcounted<IVDTReadbackBuffer>, VDTResourceD3D9 {
public:
	VDTReadbackBufferD3D9();
	~VDTReadbackBufferD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, uint32 width, uint32 height, VDTFormat format);
	void Shutdown();

	bool Restore();
	bool Lock(VDTLockData2D& lockData);
	void Unlock();

protected:
	friend class VDTContextD3D9;
	friend class VDTSurfaceD3D9;
	friend class VDTTexture2DD3D9;

	IDirect3DSurface9 *mpSurface;
};

///////////////////////////////////////////////////////////////////////////////
class VDTSurfaceD3D9 : public vdrefcounted<IVDTSurface>, VDTResourceD3D9 {
public:
	VDTSurfaceD3D9();
	~VDTSurfaceD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, uint32 width, uint32 height, VDTFormat format, VDTUsage usage);
	bool Init(VDTContextD3D9 *parent, IDirect3DSurface9 *surf, IDirect3DSurface9 *surfsys);
	void Shutdown();

	bool Restore();
	bool Readback(IVDTReadbackBuffer *target);
	void Load(uint32 dx, uint32 dy, const VDTInitData2D& srcData, uint32 bpr, uint32 h);
	void Copy(uint32 dx, uint32 dy, IVDTSurface *src, uint32 sx, uint32 sy, uint32 w, uint32 h);
	void GetDesc(VDTSurfaceDesc& desc);
	bool Lock(const vdrect32 *r, VDTLockData2D& lockData);
	void Unlock();

protected:
	void ShutdownDefaultPool();

	friend class VDTContextD3D9;

	IDirect3DSurface9 *mpSurface;
	IDirect3DSurface9 *mpSurfaceSys;
	bool mbDefaultPool;
	VDTSurfaceDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////

class VDTTextureD3D9 : protected VDTResourceD3D9 {
public:
	enum { kTypeD3DTexture = 'd3d ' };
};

///////////////////////////////////////////////////////////////////////////////
class VDTTexture2DD3D9 : public VDTTextureD3D9, public vdrefcounted<IVDTTexture2D> {
public:
	VDTTexture2DD3D9();
	~VDTTexture2DD3D9();

	void *AsInterface(uint32 id);

	bool Init(VDTContextD3D9 *parent, uint32 width, uint32 height, VDTFormat format, uint32 mipcount, VDTUsage usage, const VDTInitData2D *initData);
	void Shutdown();

	bool Restore();
	void GetDesc(VDTTextureDesc& desc);
	IVDTSurface *GetLevelSurface(uint32 level);
	void Load(uint32 mip, uint32 x, uint32 y, const VDTInitData2D& srcData, uint32 w, uint32 h);
	bool Lock(uint32 mip, const vdrect32 *r, VDTLockData2D& lockData);
	void Unlock(uint32 mip);

protected:
	void ShutdownDefaultPool();

	IDirect3DTexture9 *mpTexture;
	IDirect3DTexture9 *mpTextureSys;
	uint32	mWidth;
	uint32	mHeight;
	uint32	mMipCount;
	VDTUsage mUsage;
	VDTFormat mFormat;

	typedef vdfastvector<VDTSurfaceD3D9 *> Mipmaps;
	Mipmaps mMipmaps;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexBufferD3D9 : public vdrefcounted<IVDTVertexBuffer>, VDTResourceD3D9 {
public:
	VDTVertexBufferD3D9();
	~VDTVertexBufferD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, uint32 size, bool dynamic, const void *initData);
	void Shutdown();

	bool Restore();
	bool Load(uint32 offset, uint32 size, const void *data);

protected:
	void ShutdownDefaultPool();

	friend class VDTContextD3D9;

	IDirect3DVertexBuffer9 *mpVB;
	uint32 mByteSize;
	bool mbDynamic;
};

///////////////////////////////////////////////////////////////////////////////
class VDTIndexBufferD3D9 : public vdrefcounted<IVDTIndexBuffer>, VDTResourceD3D9 {
public:
	VDTIndexBufferD3D9();
	~VDTIndexBufferD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, uint32 size, bool index32, bool dynamic, const void *initData);
	void Shutdown();

	bool Restore();
	bool Load(uint32 offset, uint32 size, const void *data);

protected:
	void ShutdownDefaultPool();

	friend class VDTContextD3D9;

	IDirect3DIndexBuffer9 *mpIB;
	uint32 mByteSize;
	bool mbDynamic;
	bool mbIndex32;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexFormatD3D9 : public vdrefcounted<IVDTVertexFormat>, VDTResourceD3D9 {
public:
	VDTVertexFormatD3D9();
	~VDTVertexFormatD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, const VDTVertexElement *elements, uint32 count);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	IDirect3DVertexDeclaration9 *mpVF;
};

///////////////////////////////////////////////////////////////////////////////
class VDTVertexProgramD3D9 : public vdrefcounted<IVDTVertexProgram>, VDTResourceD3D9 {
public:
	VDTVertexProgramD3D9();
	~VDTVertexProgramD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, VDTProgramFormat format, const void *data, uint32 size);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	IDirect3DVertexShader9 *mpVS;
};

///////////////////////////////////////////////////////////////////////////////
class VDTFragmentProgramD3D9 : public vdrefcounted<IVDTFragmentProgram>, VDTResourceD3D9 {
public:
	VDTFragmentProgramD3D9();
	~VDTFragmentProgramD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, VDTProgramFormat format, const void *data, uint32 size);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	IDirect3DPixelShader9 *mpPS;
};

///////////////////////////////////////////////////////////////////////////////
class VDTBlendStateD3D9 : public vdrefcounted<IVDTBlendState>, VDTResourceD3D9 {
public:
	VDTBlendStateD3D9();
	~VDTBlendStateD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, const VDTBlendStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	VDTBlendStateDesc mDesc;

	enum { kStateCount = 5 };
	static const uint32 kRenderStateIDs[kStateCount];

	uint32 mRenderStates[kStateCount];
};

///////////////////////////////////////////////////////////////////////////////
class VDTRasterizerStateD3D9 : public vdrefcounted<IVDTRasterizerState>, VDTResourceD3D9 {
public:
	VDTRasterizerStateD3D9();
	~VDTRasterizerStateD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, const VDTRasterizerStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	VDTRasterizerStateDesc mDesc;

	enum { kStateCount = 2 };
	static const uint32 kRenderStateIDs[kStateCount];

	uint32 mRenderStates[kStateCount];
};

///////////////////////////////////////////////////////////////////////////////
class VDTSamplerStateD3D9 : public vdrefcounted<IVDTSamplerState>, VDTResourceD3D9 {
public:
	VDTSamplerStateD3D9();
	~VDTSamplerStateD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, const VDTSamplerStateDesc& desc);
	void Shutdown();

	bool Restore();

protected:
	friend class VDTContextD3D9;

	VDTSamplerStateDesc mDesc;

	enum { kStateCount = 6 };
	static const uint32 kSamplerStateIDs[kStateCount];

	uint32 mSamplerStates[kStateCount];
};

///////////////////////////////////////////////////////////////////////////////
class VDTSwapChainD3D9 : public vdrefcounted<IVDTSwapChain>, VDTResourceD3D9 {
public:
	VDTSwapChainD3D9();
	~VDTSwapChainD3D9();

	void *AsInterface(uint32 iid) { return NULL; }

	bool Init(VDTContextD3D9 *parent, const VDTSwapChainDesc& desc);
	void Shutdown();

	virtual void GetDesc(VDTSwapChainDesc& desc);
	virtual IVDTSurface *GetBackBuffer();

	virtual bool ResizeBuffers(uint32 width, uint32 height) { return false; }

	virtual void Present();
	virtual void PresentVSync(void *monitor, IVDTAsyncPresent *callback);
	virtual void PresentVSyncComplete();
	virtual void PresentVSyncAbort();

	virtual bool Restore();

protected:
	friend class VDTContextD3D9;

	IDirect3DSwapChain9 *mpSwapChain;
	VDTSurfaceD3D9 *mpBackBuffer;

	VDTSwapChainDesc mDesc;
};

///////////////////////////////////////////////////////////////////////////////
class VDTContextD3D9 final : public IVDTContext, public IVDTProfiler, public VDTResourceManagerD3D9 {
public:
	VDTContextD3D9();
	~VDTContextD3D9();

	int AddRef();
	int Release();
	void *AsInterface(uint32 id);

	bool Init(IDirect3DDevice9 *dev, IDirect3DDevice9Ex *dev9Ex, IVDRefUnknown *dllHolder);
	void Shutdown();

	IDirect3DDevice9 *GetDeviceD3D9() const { return mpD3DDevice; }
	IDirect3DDevice9Ex *GetDeviceD3D9Ex() const { return mpD3DDeviceEx; }

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
	bool CreateSwapChain(const VDTSwapChainDesc& desc, IVDTSwapChain **swapChain);

	bool CreateBlendState(const VDTBlendStateDesc& desc, IVDTBlendState **state);
	bool CreateRasterizerState(const VDTRasterizerStateDesc& desc, IVDTRasterizerState **state);
	bool CreateSamplerState(const VDTSamplerStateDesc& desc, IVDTSamplerState **state);

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
	bool IsDeviceLost() const { return mbDeviceLost; }
	uint32 GetDeviceLossCounter() const;
	void Present();

	void SetGpuPriority(int priority);

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

	void ResetDefaultSwapChain();

	void ProcessHRESULT(uint32 hr);

protected:
	bool ConnectSurfaces();
	bool CommitState();
	void UpdateRenderStates(const uint32 *ids, uint32 count, uint32 *shadow, const uint32 *values);

	struct PrivateData;

	VDAtomicInt	mRefCount;
	PrivateData *mpData;

	IVDRefUnknown *mpD3DHolder;
	IDirect3D9 *mpD3D;
	IDirect3DDevice9 *mpD3DDevice;
	IDirect3DDevice9Ex *mpD3DDeviceEx;

	uint32	mDeviceLossCounter;
	bool	mbDeviceLost;
	bool	mbInScene;
	bool	mbBSDirty;
	bool	mbRSDirty;
	bool	mbVPDirty;
	uint32	mDirtySamplerStates;

	VDTSurfaceD3D9 *mpCurrentRT;
	VDTVertexBufferD3D9 *mpCurrentVB;
	uint32 mCurrentVBOffset;
	uint32 mCurrentVBStride;
	VDTIndexBufferD3D9 *mpCurrentIB;
	VDTVertexProgramD3D9 *mpCurrentVP;
	VDTFragmentProgramD3D9 *mpCurrentFP;
	VDTVertexFormatD3D9 *mpCurrentVF;

	VDTBlendStateD3D9 *mpCurrentBS;
	VDTRasterizerStateD3D9 *mpCurrentRS;

	VDTSurfaceD3D9 *mpDefaultRT;
	VDTBlendStateD3D9 *mpDefaultBS;
	VDTRasterizerStateD3D9 *mpDefaultRS;
	VDTSamplerStateD3D9 *mpDefaultSS;
	
	VDTViewport mViewport;
	vdrect32 mScissorRect;
	VDTDeviceCaps mCaps;

	VDTSamplerStateD3D9 *mpCurrentSamplerStates[16];
	IVDTTexture *mpCurrentTextures[16];

	uint32 mD3DBlendStates[VDTBlendStateD3D9::kStateCount];
	uint32 mD3DRasterizerStates[VDTBlendStateD3D9::kStateCount];
	uint32 mD3DSamplerStates[16][VDTSamplerStateD3D9::kStateCount];

	void	*mpBeginEvent;
	void	*mpEndEvent;
	VDRTProfileChannel	mProfChan;
};

bool VDTCreateContextD3D9(int width, int height, int refresh, bool fullscreen, bool vsync, void *hwnd, IVDTContext **ppctx);
bool VDTCreateContextD3D9(IDirect3DDevice9 *dev, IDirect3DDevice9Ex *dev9Ex, IVDTContext **ppctx);

#endif	// f_D3D9_CONTEXT_D3D9_H
