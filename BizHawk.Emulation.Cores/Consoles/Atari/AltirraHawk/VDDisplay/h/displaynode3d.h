#ifndef f_VD2_VDDISPLAY_DISPLAYNODE3D_H
#define f_VD2_VDDISPLAY_DISPLAYNODE3D_H

#include <vd2/system/vdstl.h>
#include <vd2/Tessa/Context.h>

struct VDPixmap;
class VDDisplayNode3D;

struct VDDisplayVertex3D {
	float x;
	float y;
	float z;
	float u;
	float v;
};

struct VDDisplayVertex2T3D {
	float x;
	float y;
	float z;
	float u0;
	float v0;
	float u1;
	float v1;
};

struct VDDisplayVertex3T3D {
	float x;
	float y;
	float z;
	float u0;
	float v0;
	float u1;
	float v1;
	float u2;
	float v2;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayNodeContext3D {
	VDDisplayNodeContext3D(const VDDisplayNodeContext3D&);
	VDDisplayNodeContext3D& operator=(const VDDisplayNodeContext3D&);
public:
	VDDisplayNodeContext3D();
	~VDDisplayNodeContext3D();

	bool Init(IVDTContext& ctx);
	void Shutdown();

public:
	IVDTVertexFormat *mpVFTexture;
	IVDTVertexFormat *mpVFTexture2T;
	IVDTVertexFormat *mpVFTexture3T;
	IVDTVertexProgram *mpVPTexture;
	IVDTVertexProgram *mpVPTexture2T;
	IVDTVertexProgram *mpVPTexture3T;
	IVDTFragmentProgram *mpFPBlit;
	IVDTSamplerState *mpSSPoint;
	IVDTSamplerState *mpSSBilinear;
	IVDTSamplerState *mpSSBilinearRepeatMip;

	VDTFormat mBGRAFormat;
};

struct VDDisplaySourceTexMapping {
	float mUSize;
	float mVSize;
	uint32 mWidth;
	uint32 mHeight;
	uint32 mTexWidth;
	uint32 mTexHeight;
	bool mbRBSwap;

	void Init(uint32 w, uint32 h, uint32 texw, uint32 texh, bool rbswap) {
		mUSize = (float)w / (float)texw;
		mVSize = (float)h / (float)texh;
		mWidth = w;
		mHeight = h;
		mTexWidth = texw;
		mTexHeight = texh;
		mbRBSwap = rbswap;
	}
};

///////////////////////////////////////////////////////////////////////////
// VDDisplaySourceNode3D
//
// Source nodes supply images through textures. They can either be
// regular 2D textures, or they can be render target textures produced by
// recursive renders.
//
class VDDisplaySourceNode3D : public vdrefcount {
public:
	virtual ~VDDisplaySourceNode3D();

	// Obtain the texture mapping for the source. This must be valid after
	// node init and before Draw(). It cannot change between Draw() calls
	// as its result may be used in a derived node's init.
	virtual VDDisplaySourceTexMapping GetTextureMapping() const = 0;

	// Update and return the current texture. The texture may change with
	// each call, and only the last returned texture is valid.
	virtual IVDTTexture2D *Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx) = 0;
};

///////////////////////////////////////////////////////////////////////////
// VDDisplayTextureSourceNode3D
//
class VDDisplayTextureSourceNode3D : public VDDisplaySourceNode3D {
public:
	VDDisplayTextureSourceNode3D();
	~VDDisplayTextureSourceNode3D();

	bool Init(IVDTTexture2D *tex, const VDDisplaySourceTexMapping& mapping);
	void Shutdown();

	VDDisplaySourceTexMapping GetTextureMapping() const;
	IVDTTexture2D *Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	IVDTTexture2D *mpImageTex;
	VDDisplaySourceTexMapping mMapping;
};

///////////////////////////////////////////////////////////////////////////
// VDDisplayImageSourceNode3D
//
// Image source nodes provide static or animated images through a texture.
// They can only support one image or video per texture and only those
// that can be uploaded in native format or through a red/blue flip.
//
class VDDisplayImageSourceNode3D : public VDDisplaySourceNode3D {
public:
	VDDisplayImageSourceNode3D();
	~VDDisplayImageSourceNode3D();

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, uint32 w, uint32 h, uint32 format);
	void Shutdown();

	void Load(const VDPixmap& px);

	VDDisplaySourceTexMapping GetTextureMapping() const;
	IVDTTexture2D *Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	IVDTTexture2D *mpImageTex;
	VDDisplaySourceTexMapping mMapping;
};

///////////////////////////////////////////////////////////////////////////
// VDDisplayBufferSourceNode3D
//
// This node collects the output of a subrender tree and makes it available
// as a source node.
//
class VDDisplayBufferSourceNode3D : public VDDisplaySourceNode3D {
public:
	VDDisplayBufferSourceNode3D();
	~VDDisplayBufferSourceNode3D();

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, uint32 w, uint32 h, VDDisplayNode3D *child);
	void Shutdown();

	VDDisplaySourceTexMapping GetTextureMapping() const;
	IVDTTexture2D *Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	struct Vertex;

	IVDTTexture2D *mpRTT;
	VDDisplayNode3D *mpChildNode;
	VDDisplaySourceTexMapping mMapping;
};

///////////////////////////////////////////////////////////////////////////
// VDDisplayNode3D
//
// Display nodes draw items to the current render target. They make up
// the primary composition tree.
//
class VDDisplayNode3D : public vdrefcount {
public:
	virtual ~VDDisplayNode3D();

	virtual void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx) = 0;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplaySequenceNode3D : public VDDisplayNode3D {
public:
	VDDisplaySequenceNode3D();
	~VDDisplaySequenceNode3D();

	void Shutdown();

	void AddNode(VDDisplayNode3D *node);

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

protected:
	typedef vdfastvector<VDDisplayNode3D *> Nodes;
	Nodes mNodes;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayClearNode3D : public VDDisplayNode3D {
public:
	VDDisplayClearNode3D();
	~VDDisplayClearNode3D();

	void SetClearColor(uint32 c);

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

protected:
	uint32 mColor;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayImageNode3D : public VDDisplayNode3D {
public:
	VDDisplayImageNode3D();
	~VDDisplayImageNode3D();

	bool CanStretch() const;
	void SetBilinear(bool enabled) { mbBilinear = enabled; }
	void SetDestArea(sint32 x, sint32 y, uint32 w, uint32 h) { mDstX = x; mDstY = y; mDstW = w; mDstH = h; }

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, uint32 w, uint32 h, uint32 format);
	void Shutdown();

	void Load(const VDPixmap& px);

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	enum RenderMode {
		kRenderMode_Blit,
		kRenderMode_BlitY,
		kRenderMode_BlitYCbCr,
		kRenderMode_BlitPal8,
		kRenderMode_BlitUYVY,
		kRenderMode_BlitRGB16,
		kRenderMode_BlitRGB16Direct,
		kRenderMode_BlitRGB24
	};

	IVDTTexture2D *mpImageTex[3];
	IVDTTexture2D *mpPaletteTex;
	IVDTVertexFormat *mpVF;
	IVDTVertexProgram *mpVP;
	IVDTFragmentProgram *mpFP;
	IVDTVertexBuffer *mpVB;

	RenderMode	mRenderMode;
	bool	mbRenderSwapRB;
	bool	mbRender2T;
	bool	mbBilinear;
	sint32	mDstX;
	sint32	mDstY;
	uint32	mDstW;
	uint32	mDstH;
	uint32	mTexWidth;
	uint32	mTexHeight;
	uint32	mTex2Width;
	uint32	mTex2Height;
};

///////////////////////////////////////////////////////////////////////////
// VDDisplayBlitNode3D
//
// This node does a simple render of a source node using point, bilinear,
// or sharp bilinear filtering.
//
class VDDisplayBlitNode3D : public VDDisplayNode3D {
public:
	VDDisplayBlitNode3D();
	~VDDisplayBlitNode3D();

	void SetDestArea(sint32 x, sint32 y, uint32 w, uint32 h) { mDstX = x; mDstY = y; mDstW = w; mDstH = h; }

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, uint32 w, uint32 h, bool linear, float sharpnessX, float sharpnessY, VDDisplaySourceNode3D *source);
	void Shutdown();

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	struct Vertex;

	IVDTVertexBuffer *mpVB;
	IVDTFragmentProgram *mpFP;
	VDDisplaySourceNode3D *mpSourceNode;
	bool mbLinear;
	float mSharpnessX;
	float mSharpnessY;
	sint32	mDstX;
	sint32	mDstY;
	uint32	mDstW;
	uint32	mDstH;
	VDDisplaySourceTexMapping mMapping;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayStretchBicubicNode3D : public VDDisplayNode3D {
public:
	VDDisplayStretchBicubicNode3D();
	~VDDisplayStretchBicubicNode3D();

	const vdrect32 GetDestArea() const;

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, uint32 srcw, uint32 srch, sint32 dstx, sint32 dsty, uint32 dstw, uint32 dsth, VDDisplaySourceNode3D *child);
	void Shutdown();

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	struct Vertex;

	IVDTVertexProgram *mpVP;
	IVDTFragmentProgram *mpFP;
	IVDTVertexFormat *mpVF;
	IVDTVertexBuffer *mpVB;

	IVDTTexture2D *mpRTTHoriz;
	IVDTTexture2D *mpFilterTex;
	VDDisplaySourceNode3D *mpSourceNode;

	uint32	mSrcW;
	uint32	mSrcH;
	sint32	mDstX;
	sint32	mDstY;
	uint32	mDstW;
	uint32	mDstH;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayScreenFXNode3D : public VDDisplayNode3D {
public:
	VDDisplayScreenFXNode3D();
	~VDDisplayScreenFXNode3D();

	const vdrect32 GetDestArea() const;

	struct Params {
		sint32 mDstX;
		sint32 mDstY;
		sint32 mDstW;
		sint32 mDstH;
		bool mbLinear;
		bool mbUseAdobeRGB;
		float mSharpnessX;
		float mSharpnessY;
		float mDistortionX;
		float mDistortionYRatio;
		float mScanlineIntensity;
		float mGamma;
		float mPALBlendingOffset;
		float mColorCorrectionMatrix[3][3];
	};

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, const Params& initParams, VDDisplaySourceNode3D *child);
	void Shutdown();

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx);

private:
	struct Vertex;

	IVDTVertexProgram *mpVP = nullptr;
	IVDTFragmentProgram *mpFP = nullptr;
	IVDTVertexFormat *mpVF = nullptr;
	IVDTVertexBuffer *mpVB = nullptr;

	IVDTTexture2D *mpGammaRampTex = nullptr;
	IVDTTexture2D *mpScanlineMaskTex = nullptr;
	VDDisplaySourceNode3D *mpSourceNode = nullptr;

	uint32 mVPMode = 0;
	uint32 mFPMode = 0;
	float mCachedGamma = 0;
	bool mbCachedGammaHasSrgb = false;
	bool mbCachedGammaHasAdobeRGB = false;

	VDDisplaySourceTexMapping mMapping {};
	Params mParams {};
	float mScanlineMaskNormH = 0;
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayArtifactingNode3D final : public VDDisplayNode3D {
public:
	VDDisplayArtifactingNode3D();
	~VDDisplayArtifactingNode3D();

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, float dy, VDDisplaySourceNode3D *child);
	void Shutdown();

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx) override;

private:
	struct Vertex;

	IVDTFragmentProgram *mpFP = nullptr;
	IVDTVertexBuffer *mpVB = nullptr;

	VDDisplaySourceNode3D *mpSourceNode = nullptr;

	VDDisplaySourceTexMapping mMapping {};
};

///////////////////////////////////////////////////////////////////////////

class VDDisplayBloomNode3D final : public VDDisplayNode3D {
public:
	VDDisplayBloomNode3D();
	~VDDisplayBloomNode3D();

	struct Params {
		float mDstX;
		float mDstY;
		float mDstW;
		float mDstH;
		float mThreshold;
		float mBlurRadius;
		float mDirectIntensity;
		float mIndirectIntensity;
	};

	bool Init(IVDTContext& ctx, VDDisplayNodeContext3D& dctx, const Params& params, VDDisplaySourceNode3D *child);
	void Shutdown();

	void Draw(IVDTContext& ctx, VDDisplayNodeContext3D& dctx) override;

private:
	struct Vertex;

	IVDTVertexProgram *mpVPs[3] {};
	IVDTFragmentProgram *mpFPs[4] {};
	IVDTVertexBuffer *mpVB = nullptr;
	IVDTTexture2D *mpRTTPrescale = nullptr;
	IVDTTexture2D *mpRTT1 = nullptr;
	IVDTTexture2D *mpRTT2 = nullptr;

	Params mParams {};
	uint32 mBlurW = 0;
	uint32 mBlurH = 0;
	uint32 mBlurW2 = 0;
	uint32 mBlurH2 = 0;
	bool mbPrescale2x = false;

	float mVPConstants1[12] {};
	float mVPConstants2[12] {};
	float mVPConstants3[12] {};
	float mFPConstants[8] {};

	VDDisplaySourceNode3D *mpSourceNode = nullptr;

	VDDisplaySourceTexMapping mMapping {};
};

#endif
