#ifndef f_VD2_VDDISPLAY_MINID3DX_H
#define f_VD2_VDDISPLAY_MINID3DX_H

#include <vd2/system/vdtypes.h>

// Clone of D3DXREGISTER_SET.
enum VDD3DXRegisterSet : uint32 {
	kVDD3DXRegisterSet_Bool,
	kVDD3DXRegisterSet_Int4,
	kVDD3DXRegisterSet_Float4,
	kVDD3DXRegisterSet_Sampler
};

// Clone of D3DXPARAMETER_TYPE.
enum VDD3DXParameterType : uint32 {
	kVDD3DXParameterType_Void,
	kVDD3DXParameterType_Bool,
	kVDD3DXParameterType_Int,
	kVDD3DXParameterType_Float,
	kVDD3DXParameterType_String,
	kVDD3DXParameterType_Texture,
	kVDD3DXParameterType_Texture1D,
	kVDD3DXParameterType_Texture2D,
	kVDD3DXParameterType_Texture3D,
	kVDD3DXParameterType_TextureCube,
	kVDD3DXParameterType_Sampler,
	kVDD3DXParameterType_Sampler1D,
	kVDD3DXParameterType_Sampler2D,
	kVDD3DXParameterType_Sampler3D,
	kVDD3DXParameterType_SamplerCube,
	kVDD3DXParameterType_PixelShader,
	kVDD3DXParameterType_VertexShader,
	kVDD3DXParameterType_PixelFragment,
	kVDD3DXParameterType_VertexFragment,
	kVDD3DXParameterType_Unsupported
};

// Clone of D3DXPARAMETER_CLASS.
enum VDD3DXParameterClass : uint32 {
	// float, int, bool
	kVDD3DXParameterClass_Scalar,

	// float2-4, int2-4, bool2-4
	kVDD3DXParameterClass_Vector,

	// float/int/bool1x1-4x4. A float1x4 is not the same as a float4!
	kVDD3DXParameterClass_Matrix_RowMajor,
	kVDD3DXParameterClass_Matrix_ColumnMajor,

	// sampler, texture
	kVDD3DXParameterClass_Object,

	// struct
	kVDD3DXParameterClass_Struct
};

// First structure at the head of the CTAB structure.
struct VDD3DXConstantTableHeader {
	uint32	m_unk0;
	uint32	mCreatorOffset;
	uint32	mShaderVersion;
	uint32	mParameterCount;
	uint32	mParameterTableOffset;
	uint32	mFlags;				// D3DXSHADER flags -- ignorable
	uint32	mShaderProfileOffset;
};

// Describes a shader parameter; referred to by mParameterTableOffset. This is
// about half of D3DXCONSTANT_DESC; the other half is shared as type objects
// through mTypeOffset.
struct VDD3DXParameterHeader {
	uint32	mNameOffset;
	uint16	mRegisterSet;
	uint16	mRegisterIndex;
	uint16	mRegisterCount;
	uint16	m_pad0;
	uint32	mTypeOffset;
	uint32	mDefaultValueOffset;
};

// Describes a type reference; referred to by mTypeOffset. This is the other
// half of D3DXCONSTANT_DESC. It is sharable between parameters and struct
// members.
struct VDD3DXTypeHeader {
	uint16	mClass;
	uint16	mType;

	// The number of rows and columns in the base type. For instance float2x3
	// gives rows=2 and columns=3. Row/column major storage and array lengths
	// do not matter here. Scalars specify 1x1.
	//
	// For structs, rows=1 but cols=scalars, where:
	//
	//		struct {
	//			float a[4];
	//			float2 b;
	//		};
	//
	// gives cols = 6. Don't look at me, I didn't spec it. :(
	//
	uint16	mRows;
	uint16	mCols;

	// Number of matrix elements. Always 1 for non-matrix types. float4x4 gives
	// 1, float4x4[3] gives 3.
	uint16	mElements;

	uint16	mStructMemberCount;
	uint32	mStructMembersOffset;
};

// Describes a structure member.
struct VDD3DXStructMemberHeader {
	uint32	mNameOffset;
	uint32	mTypeOffset;
};

class VDD3DXCTParameter;

class VDD3DXCTMemberEnum {
public:
	bool Next(VDD3DXCTParameter& p);

private:
	friend class VDD3DXCTParameter;

	VDD3DXCTMemberEnum(
		const char *base,
		const VDD3DXStructMemberHeader *members,
		uint32 memberCount,
		VDD3DXRegisterSet registerSet,
		uint32 registerIndex,
		uint32 registersLeft
	)
		: mpBase(base)
		, mpMembers(members)
		, mMembersLeft(memberCount)
		, mRegisterSet(registerSet)
		, mRegisterIndex(registerIndex)
		, mRegistersLeft(registersLeft)
	{
	}

	uint32 GetTypeRegisterCount(uint32 typeOffset);

	const char *mpBase;
	const VDD3DXStructMemberHeader *mpMembers;
	uint32 mMembersLeft;
	VDD3DXRegisterSet mRegisterSet;
	uint32 mRegisterIndex;
	uint32 mRegistersLeft;
};

class VDD3DXCTParameter {
public:
	VDD3DXCTParameter() = default;

	const char *			GetName() const { return mpName; }
	VDD3DXRegisterSet		GetRegisterSet() const { return mRegisterSet; }
	uint32_t				GetRegisterIndex() const { return mRegisterIndex; }
	uint32_t				GetRegisterCount() const { return mRegisterCount; }
	VDD3DXParameterClass	GetParamClass() const { return (VDD3DXParameterClass)mpType->mClass; }
	VDD3DXParameterType		GetParamType() const { return (VDD3DXParameterType)mpType->mType; }
	uint32					GetParamRows() const { return mpType->mRows; }
	uint32					GetParamCols() const { return mpType->mCols; }
	uint32					GetParamElements() const { return mpType->mElements; }

	uint32					GetMemberCount() const { return mpType->mStructMemberCount; }

	VDD3DXCTMemberEnum		GetMembers() const {
		return VDD3DXCTMemberEnum(mpBase, (const VDD3DXStructMemberHeader *)(mpBase + mpType->mStructMembersOffset), mpType->mStructMemberCount, mRegisterSet, mRegisterIndex, mRegisterCount);
	}

	const void *			GetDefaultValue() const { return mpDefaultValue; }

private:
	friend class VDD3DXConstantTable;
	friend class VDD3DXCTMemberEnum;

	VDD3DXCTParameter(const void *base, uint32 ptoffset, uint32 ptindex)
		: mpBase((const char *)base)
	{
		const auto& header = ((const VDD3DXParameterHeader *)(mpBase + ptoffset))[ptindex];
		mpType = (const VDD3DXTypeHeader *)(mpBase + header.mTypeOffset);

		mpName = mpBase + header.mNameOffset;
		mRegisterSet = (VDD3DXRegisterSet)header.mRegisterSet;
		mRegisterIndex = header.mRegisterIndex;
		mRegisterCount = header.mRegisterCount;

		mpDefaultValue = header.mDefaultValueOffset ? mpBase + header.mDefaultValueOffset : nullptr;
	}

	VDD3DXCTParameter(const void *base, const VDD3DXStructMemberHeader& memberHdr, VDD3DXRegisterSet rset, uint32 rindex, uint32 rcount)
		: mpBase((const char *)base)
	{
		mpType = (const VDD3DXTypeHeader *)(mpBase + memberHdr.mTypeOffset);

		mpName = mpBase + memberHdr.mNameOffset;
		mRegisterSet = (VDD3DXRegisterSet)rset;
		mRegisterIndex = rindex;
		mRegisterCount = rcount;
	}

	const char *mpBase = nullptr;
	const char *mpName = nullptr;
	const void *mpDefaultValue = nullptr;
	VDD3DXRegisterSet mRegisterSet = {};
	uint32 mRegisterIndex = 0;
	uint32 mRegisterCount = 0;
	const VDD3DXTypeHeader *mpType = nullptr;
};

class VDD3DXConstantTable {
public:
	VDD3DXConstantTable() = default;
	VDD3DXConstantTable(const void *mem, uint32 size) : mpHeader((const VDD3DXConstantTableHeader*)mem), mSize(size) {}

	uint32 GetParameterCount() const { return mpHeader->mParameterCount; }
	VDD3DXCTParameter GetParameter(uint32 i) const {
		return VDD3DXCTParameter(mpHeader, mpHeader->mParameterTableOffset, i);
	}

	bool Validate() const;

private:
	bool ValidateString(uint32 offset) const;
	bool ValidateParameter(const VDD3DXParameterHeader& param) const;
	bool ValidateType(uint32 typeOffset, uint32 nestingLevel) const;

	const VDD3DXConstantTableHeader *mpHeader = nullptr;
	uint32 mSize = 0;
};

uint32 VDD3DXGetShaderSize(const uint32 *data);
bool VDD3DXCheckShaderSize(const uint32 *data, uint32 sizeInBytes);
const void *VDD3DXFindShaderComment(const uint32 *data, uint32 sizeInBytes, uint32 fourcc, uint32& commentSizeInBytes);
bool VDD3DXGetShaderConstantTable(const uint32 *data, uint32 size, VDD3DXConstantTable& ct);

#endif
