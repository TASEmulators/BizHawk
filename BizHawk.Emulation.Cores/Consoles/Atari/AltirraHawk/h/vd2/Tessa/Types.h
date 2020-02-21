#ifndef f_VD2_TESSA_TYPES_H
#define f_VD2_TESSA_TYPES_H

#include <vd2/system/vdtypes.h>

enum VDTProgramFormat {
	kVDTPF_MultiTarget,
	kVDTPF_D3D9ByteCode,
	kVDTPF_D3D11ByteCode
};

enum VDTFormat {
	kVDTF_Unknown,
	kVDTF_R8G8B8A8,
	kVDTF_B8G8R8A8,
	kVDTF_U8V8,
	kVDTF_L8A8,
	kVDTF_R8G8,
	kVDTF_B5G6R5,
	kVDTF_B5G5R5A1,
	kVDTF_L8,
	kVDTF_R8
};

struct VDTInitData2D {
	const void *mpData;
	ptrdiff_t mPitch;
};

enum VDTPrimitiveType {
	kVDTPT_Triangles,
	kVDTPT_TriangleStrip,
	kVDTPT_Lines,
	kVDTPT_LineStrip
};

enum VDTClearFlags {
	kVDTClear_None			= 0,
	kVDTClear_Color			= 1,
	kVDTClear_Depth			= 2,
	kVDTClear_Stencil		= 4,
	kVDTClear_DepthStencil	= 6,
	kVDTClear_All			= 7
};

struct VDTLockData2D {
	void		*mpData;
	ptrdiff_t	mPitch;
};

struct VDTSurfaceDesc {
	uint32 mWidth;
	uint32 mHeight;
	VDTFormat mFormat;
};

struct VDTTextureDesc {
	uint32 mWidth;
	uint32 mHeight;
	uint32 mMipCount;
	VDTFormat mFormat;
};

enum VDTElementType {
	kVDTET_Float,
	kVDTET_Float2,
	kVDTET_Float3,
	kVDTET_Float4,
	kVDTET_UByte4,
	kVDTET_UByte4N
};

enum VDTElementUsage {
	kVDTEU_Position,
	kVDTEU_BlendWeight,
	kVDTEU_BlendIndices,
	kVDTEU_Normal,
	kVDTEU_TexCoord,
	kVDTEU_Tangent,
	kVDTEU_Binormal,
	kVDTEU_Color
};

struct VDTVertexElement {
	uint32			mOffset;
	VDTElementType	mType;
	VDTElementUsage	mUsage;
	uint32			mUsageIndex;
};

enum VDTCullMode {
	kVDTCull_None,
	kVDTCull_Front,
	kVDTCull_Back
};

enum VDTBlendFactor {
	kVDTBlend_Zero,
	kVDTBlend_One,
	kVDTBlend_SrcColor,
	kVDTBlend_InvSrcColor,
	kVDTBlend_SrcAlpha,
	kVDTBlend_InvSrcAlpha,
	kVDTBlend_DstAlpha,
	kVDTBlend_InvDstAlpha,
	kVDTBlend_DstColor,
	kVDTBlend_InvDstColor
};

enum VDTBlendOp {
	kVDTBlendOp_Add,
	kVDTBlendOp_Subtract,
	kVDTBlendOp_RevSubtract,
	kVDTBlendOp_Min,
	kVDTBlendOp_Max
};

struct VDTBlendStateDesc {
	bool			mbEnable;
	bool			mbEnableWriteMask;
	uint8			mWriteMask;
	VDTBlendFactor	mSrc;
	VDTBlendFactor	mDst;
	VDTBlendOp		mOp;
};

struct VDTRasterizerStateDesc {
	VDTCullMode		mCullMode;
	bool			mbFrontIsCCW;
	bool			mbEnableScissor;
};

enum VDTFilterMode {
	kVDTFilt_Point,
	kVDTFilt_Bilinear,
	kVDTFilt_BilinearMip,
	kVDTFilt_Trilinear,
	kVDTFilt_Anisotropic
};

enum VDTAddressMode {
	kVDTAddr_Clamp,
	kVDTAddr_Wrap
};

struct VDTSamplerStateDesc {
	VDTFilterMode	mFilterMode;
	VDTAddressMode	mAddressU;
	VDTAddressMode	mAddressV;
	VDTAddressMode	mAddressW;
};

struct VDTSwapChainDesc {
	uint32 mWidth;
	uint32 mHeight;
	void *mhWindow;
	bool mbWindowed;
	uint32 mRefreshRateNumerator;
	uint32 mRefreshRateDenominator;
};

enum VDTUsage {
	kVDTUsage_Default,
	kVDTUsage_Render
};

struct VDTViewport {
	sint32 mX;
	sint32 mY;
	uint32 mWidth;
	uint32 mHeight;
	float mMinZ;
	float mMaxZ;
};

struct VDTData {
	const void *mpData;
	uint32 mLength;
};

class VDTDataView : public VDTData {
public:
	template<class T, size_t N>
	constexpr VDTDataView(T (&array)[N]) {
		mpData = array;
		mLength = N * sizeof(T);
	}

	constexpr VDTDataView(const void *p, uint32 len) {
		mpData = p;
		mLength = len;
	}
};

struct VDTDeviceCaps {
	bool	mbNonPow2;
	bool	mbNonPow2Conditional;
	uint32	mMaxTextureWidth;
	uint32	mMaxTextureHeight;
};

#endif	// f_VD2_TESSA_TYPES_H
