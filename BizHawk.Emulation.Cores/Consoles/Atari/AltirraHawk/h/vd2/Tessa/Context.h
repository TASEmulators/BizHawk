#ifndef f_VD2_TESSA_CONTEXT_H
#define f_VD2_TESSA_CONTEXT_H

#include <vd2/system/vectors.h>
#include <vd2/system/refcount.h>
#include <vd2/system/unknown.h>
#include <vd2/Tessa/Types.h>

class VDRTProfileChannel;

class IVDTResource : public IVDRefUnknown {
public:
	virtual bool Restore() = 0;
};

class IVDTReadbackBuffer : public IVDTResource {
public:
	virtual bool Lock(VDTLockData2D& lockData) = 0;
	virtual void Unlock() = 0;
};

class IVDTSurface : public IVDTResource {
public:
	virtual bool Restore() = 0;
	virtual bool Readback(IVDTReadbackBuffer *target) = 0;
	virtual void Copy(uint32 dx, uint32 dy, IVDTSurface *src, uint32 sx, uint32 sy, uint32 w, uint32 h) = 0;
	virtual void GetDesc(VDTSurfaceDesc& desc) = 0;
};

class IVDTTexture : public IVDTResource {
public:
	virtual bool Restore() = 0;
	virtual void GetDesc(VDTTextureDesc& desc) = 0;
};

class IVDTTexture2D : public IVDTTexture {
public:
	enum { kTypeID = '3t2d' };

	virtual IVDTSurface *GetLevelSurface(uint32 level) = 0;
	virtual void Load(uint32 mip, uint32 x, uint32 y, const VDTInitData2D& srcData, uint32 w, uint32 h) = 0;
	virtual bool Lock(uint32 mip, const vdrect32 *r, VDTLockData2D& lockData) = 0;
	virtual void Unlock(uint32 mip) = 0;
};

class IVDTVertexProgram : public IVDTResource {
};

class IVDTFragmentProgram : public IVDTResource {
};

class IVDTVertexFormat : public IVDTResource {
};

class IVDTVertexBuffer : public IVDTResource {
public:
	virtual bool Restore() = 0;
	virtual bool Load(uint32 offset, uint32 size, const void *data) = 0;
};

class IVDTIndexBuffer : public IVDTResource {
public:
	virtual bool Restore() = 0;
};

class IVDTBlendState: public IVDTResource {
};

class IVDTRasterizerState: public IVDTResource {
};

class IVDTSamplerState: public IVDTResource {
};

class IVDTAsyncPresent {
public:
	virtual void QueuePresent() = 0;
};

class IVDTSwapChain: public IVDTResource {
public:
	virtual void GetDesc(VDTSwapChainDesc& desc) = 0;
	virtual IVDTSurface *GetBackBuffer() = 0;

	virtual bool ResizeBuffers(uint32 width, uint32 height) = 0;

	virtual void Present() = 0;
	virtual void PresentVSync(void *monitor, IVDTAsyncPresent *callback) = 0;
	virtual void PresentVSyncComplete() = 0;
	virtual void PresentVSyncAbort() = 0;
};

class IVDTContext : public IVDRefUnknown {
public:
	virtual const VDTDeviceCaps& GetDeviceCaps() = 0;
	virtual bool IsFormatSupportedTexture2D(VDTFormat format) = 0;

	virtual bool CreateReadbackBuffer(uint32 width, uint32 height, VDTFormat format, IVDTReadbackBuffer **buffer) = 0;
	virtual bool CreateSurface(uint32 width, uint32 height, VDTFormat format, VDTUsage usage, IVDTSurface **surface) = 0;
	virtual bool CreateTexture2D(uint32 width, uint32 height, VDTFormat format, uint32 mipcount, VDTUsage usage, const VDTInitData2D *initData, IVDTTexture2D **tex) = 0;
	virtual bool CreateVertexProgram(VDTProgramFormat format, VDTData data, IVDTVertexProgram **tex) = 0;
	virtual bool CreateFragmentProgram(VDTProgramFormat format, VDTData data, IVDTFragmentProgram **tex) = 0;
	virtual bool CreateVertexFormat(const VDTVertexElement *elements, uint32 count, IVDTVertexProgram *vp, IVDTVertexFormat **format) = 0;
	virtual bool CreateVertexBuffer(uint32 size, bool dynamic, const void *initData, IVDTVertexBuffer **buffer) = 0;
	virtual bool CreateIndexBuffer(uint32 count, bool index32, bool dynamic, const void *initData, IVDTIndexBuffer **buffer) = 0;

	virtual bool CreateBlendState(const VDTBlendStateDesc& desc, IVDTBlendState **state) = 0;
	virtual bool CreateRasterizerState(const VDTRasterizerStateDesc& desc, IVDTRasterizerState **state) = 0;
	virtual bool CreateSamplerState(const VDTSamplerStateDesc& desc, IVDTSamplerState **state) = 0;

	virtual bool CreateSwapChain(const VDTSwapChainDesc& desc, IVDTSwapChain **swapChain) = 0;

	virtual IVDTSurface *GetRenderTarget(uint32 index) const = 0;

	virtual void SetVertexFormat(IVDTVertexFormat *format) = 0;
	virtual void SetVertexProgram(IVDTVertexProgram *program) = 0;
	virtual void SetFragmentProgram(IVDTFragmentProgram *program) = 0;
	virtual void SetVertexStream(uint32 index, IVDTVertexBuffer *buffer, uint32 offset, uint32 stride) = 0;
	virtual void SetIndexStream(IVDTIndexBuffer *buffer) = 0;
	virtual void SetRenderTarget(uint32 index, IVDTSurface *surface) = 0;

	virtual void SetBlendState(IVDTBlendState *state) = 0;
	virtual void SetSamplerStates(uint32 baseIndex, uint32 count, IVDTSamplerState *const *states) = 0;
	virtual void SetTextures(uint32 baseIndex, uint32 count, IVDTTexture *const *textures) = 0;

	// rasterizer
	virtual void SetRasterizerState(IVDTRasterizerState *state) = 0;
	virtual VDTViewport GetViewport() = 0;
	virtual void SetViewport(const VDTViewport& vp) = 0;

	virtual vdrect32 GetScissorRect() = 0;
	virtual void SetScissorRect(const vdrect32& r) = 0;

	virtual void SetVertexProgramConstCount(uint32 count) = 0;
	virtual void SetVertexProgramConstF(uint32 baseIndex, uint32 count, const float *data) = 0;
	virtual void SetFragmentProgramConstCount(uint32 count) = 0;
	virtual void SetFragmentProgramConstF(uint32 baseIndex, uint32 count, const float *data) = 0;

	virtual void Clear(VDTClearFlags clearFlags, uint32 color, float depth, uint32 stencil) = 0;
	virtual void DrawPrimitive(VDTPrimitiveType type, uint32 startVertex, uint32 primitiveCount) = 0;
	virtual void DrawIndexedPrimitive(VDTPrimitiveType type, uint32 baseVertexIndex, uint32 minVertex, uint32 vertexCount, uint32 startIndex, uint32 primitiveCount) = 0;

	virtual uint32 InsertFence() = 0;
	virtual bool CheckFence(uint32 id) = 0;

	virtual bool RecoverDevice() = 0;
	virtual bool OpenScene() = 0;
	virtual bool CloseScene() = 0;
	virtual bool IsDeviceLost() const = 0;
	virtual uint32 GetDeviceLossCounter() const = 0;
	virtual void Present() = 0;

	virtual void SetGpuPriority(int priority) = 0;
};

class IVDTProfiler : public IVDRefUnknown {
public:
	enum { kTypeID = '3prf' };

	virtual void BeginScope(uint32 color, const char *message) = 0;
	virtual void EndScope() = 0;
	virtual VDRTProfileChannel *GetProfileChannel() = 0;
};

void VDTBeginScopeF(IVDTProfiler *profiler, uint32 color, const char *format, ...);

#endif	// f_VD2_TESSA_CONTEXT_H
