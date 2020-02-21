#include <vd2/system/binary.h>
#include <vd2/VDDisplay/minid3dx.h>

static_assert(sizeof(VDD3DXConstantTableHeader) == 28, "Constant table header is wrong.");
static_assert(sizeof(VDD3DXParameterHeader) == 20, "Parameter header is wrong.");
static_assert(sizeof(VDD3DXTypeHeader) == 16, "Type header is wrong.");
static_assert(sizeof(VDD3DXStructMemberHeader) == 8, "Struct member header is wrong.");

bool VDD3DXCTMemberEnum::Next(VDD3DXCTParameter& p) {
	// This part sucks. The D3DX constant table format doesn't store full parameter information
	// for members, so it has to be inferred by order and type. Oddly enough, while structs can
	// contain bools/floats, the slots are allocated across both the c# and b# register files.
	// (For SM2/3, the compiler emulates ints with floats.) This means that a struct that
	// contains both will waste registers:
	//
	//	struct foo {
	//		float u;		// c0/b0
	//		bool b;			// c1/b1
	//		float v;		// c2
	//	};					// struct uses b0-b2, c0-c2
	//
	// When this occurs, the struct is *duplicated* in the parameter list, once for each
	// register file, sharing the same type list. The member list for the struct will contain
	// all fields even if some of the fields are truncated in one or both parameters. This
	// means that struct parameters are very bad for register allocation and parameter upload
	// speed if sparsely used.
	//
	// Also important to note is that the compiler may truncate the struct and overlap some
	// of its registers if later fields are not used. This means that the registers used by
	// a subfield must be limited by the register window of all ancestors.
	//
	// Shader model 2 / 3 do not allow samplers in structs, but nested structures ARE allowed.

	if (!mMembersLeft)
		return false;

	--mMembersLeft;

	uint32 numRegs = GetTypeRegisterCount(mpMembers->mTypeOffset);

	if (numRegs > mRegistersLeft)
		numRegs = mRegistersLeft;

	p = VDD3DXCTParameter(mpBase, *mpMembers++, mRegisterSet, mRegisterIndex, numRegs);

	mRegistersLeft -= numRegs;
	mRegisterIndex += numRegs;
	return true;
}

uint32 VDD3DXCTMemberEnum::GetTypeRegisterCount(uint32 typeOffset) {
	const VDD3DXTypeHeader& memberType = *(const VDD3DXTypeHeader *)(mpBase + typeOffset);

	uint32 elsize = 1;

	switch(memberType.mClass) {
		case kVDD3DXParameterClass_Vector:
			if (memberType.mType == kVDD3DXParameterType_Bool)
				elsize = memberType.mCols;
			break;

		case kVDD3DXParameterClass_Matrix_RowMajor:
			if (memberType.mType == kVDD3DXParameterType_Bool)
				elsize = memberType.mRows * memberType.mCols;
			else
				elsize = memberType.mRows;
			break;

		case kVDD3DXParameterClass_Matrix_ColumnMajor:
			if (memberType.mType == kVDD3DXParameterType_Bool)
				elsize = memberType.mRows * memberType.mCols;
			else
				elsize = memberType.mCols;
			break;

		case kVDD3DXParameterClass_Struct:
			{
				const VDD3DXStructMemberHeader *members = (const VDD3DXStructMemberHeader *)(mpBase + memberType.mStructMembersOffset);

				for(uint32 i=0; i<memberType.mStructMemberCount; ++i)
					elsize += GetTypeRegisterCount(members[i].mTypeOffset);
			}
			break;
	}

	return elsize * memberType.mElements;
}

bool VDD3DXConstantTable::Validate() const {
	if (!ValidateString(mpHeader->mCreatorOffset))
		return false;

	if (!ValidateString(mpHeader->mShaderProfileOffset))
		return false;

	// validate parameter table
	if (mpHeader->mParameterCount) {
		if (mpHeader->mParameterTableOffset & 3)
			return false;

		if (mpHeader->mParameterTableOffset >= mSize)
			return false;

		if (mpHeader->mParameterCount > (UINT32_C(0xFFFFFFFF) / sizeof(VDD3DXParameterHeader)))
			return false;

		if (mpHeader->mParameterCount * sizeof(VDD3DXParameterHeader) > mSize - mpHeader->mParameterTableOffset)
			return false;

		const VDD3DXParameterHeader *params = (const VDD3DXParameterHeader *)((char *)mpHeader + mpHeader->mParameterTableOffset);
		for(uint32 i=0; i<mpHeader->mParameterCount; ++i) {
			if (!ValidateParameter(params[i]))
				return false;
		}
	}

	return true;
}

bool VDD3DXConstantTable::ValidateString(uint32 offset) const {
	if (offset < sizeof(VDD3DXConstantTableHeader))
		return false;

	if (offset >= mSize)
		return false;

	const uint8 *base = (const uint8 *)mpHeader;

	while(offset < mSize) {
		if (!base[offset++])
			return true;
	}

	return false;
}

bool VDD3DXConstantTable::ValidateParameter(const VDD3DXParameterHeader& param) const {
	if (!ValidateString(param.mNameOffset))
		return false;

	if (param.mRegisterSet > kVDD3DXRegisterSet_Sampler)
		return false;

	if (param.mRegisterCount > UINT32_C(0x10000) - param.mRegisterIndex)
		return false;

	if (param.mTypeOffset < sizeof(VDD3DXConstantTableHeader) || (param.mTypeOffset & 3))
		return false;

	if (param.mTypeOffset >= mSize || mSize - param.mTypeOffset < sizeof(VDD3DXTypeHeader))
		return false;

	// validate type object
	if (!ValidateType(param.mTypeOffset, 0))
		return false;

	if (param.mDefaultValueOffset) {
		if (param.mDefaultValueOffset < sizeof(VDD3DXConstantTableHeader) || param.mDefaultValueOffset >= mSize)
			return false;

		if (param.mDefaultValueOffset & 3)
			return false;

		// Compute and validate size of default value.
		//
		// Argh:
		//	float			16 bytes
		//	float2			16 bytes
		//	float4			16 bytes
		//	int				16 bytes
		//	int2			16 bytes
		//	int4			16 bytes
		//	bool			4 bytes
		//	bool2			8 bytes
		//	bool3			12 bytes
		//	bool4			16 bytes
		//	bool3[2]		24 bytes
		//	bool2x2			16 bytes
		//	bool2x2[2]		32 bytes
		//
		// Important: The type of the struct field does not matter. It's the type of the REGISTER
		// that does. The struct does not have a layout until it is mapped onto a register via a
		// parameter, and it's that parameter that determines the format of the default value
		// assigned to that view.
		//
		const VDD3DXTypeHeader& type = *(const VDD3DXTypeHeader *)((const char *)mpHeader + param.mTypeOffset);
		uint32 elsize = 0;
		
		switch(type.mClass) {
			case kVDD3DXParameterClass_Scalar:
				if (param.mRegisterSet == kVDD3DXRegisterSet_Bool)
					elsize = 4;
				else
					elsize = 16;
				break;

			case kVDD3DXParameterClass_Vector:
				if (param.mRegisterSet == kVDD3DXRegisterSet_Bool)
					elsize = 4 * type.mCols;
				else
					elsize = 16;
				break;

			case kVDD3DXParameterClass_Matrix_RowMajor:
				if (param.mRegisterSet == kVDD3DXRegisterSet_Bool)
					elsize = 4 * type.mCols * type.mRows;
				else
					elsize = 16 * type.mRows;
				break;

			case kVDD3DXParameterClass_Matrix_ColumnMajor:
				if (param.mRegisterSet == kVDD3DXRegisterSet_Bool)
					elsize = 4 * type.mCols * type.mRows;
				else
					elsize = 16 * type.mCols;
				break;

			case kVDD3DXParameterClass_Object:
				// WTF - no defaults for object types
				return false;
		}

		if (mSize - param.mDefaultValueOffset < elsize * type.mElements)
			return false;
	}

	return true;
}

bool VDD3DXConstantTable::ValidateType(uint32 typeOffset, uint32 nestingLevel) const {
	if (typeOffset < sizeof(VDD3DXConstantTableHeader) || (typeOffset & 3))
		return false;

	if (typeOffset >= mSize || mSize - typeOffset < sizeof(VDD3DXTypeHeader))
		return false;

	const VDD3DXTypeHeader& type = *(const VDD3DXTypeHeader *)((const char *)mpHeader + typeOffset);

	if (type.mClass > kVDD3DXParameterClass_Struct)
		return false;

	if (type.mType > kVDD3DXParameterType_Unsupported)
		return false;

	if (type.mRows > 4 || !type.mRows)
		return false;

	if (type.mClass != kVDD3DXParameterClass_Struct && (type.mCols > 4 || !type.mCols))
		return false;

	if (type.mClass == kVDD3DXParameterClass_Struct && type.mStructMemberCount) {
		if (nestingLevel > 16)
			return false;			// WTF, possible infinite loop

		if (type.mStructMembersOffset < sizeof(VDD3DXConstantTableHeader))
			return false;

		if (type.mStructMembersOffset & 3)
			return false;

		if (type.mStructMembersOffset >= mSize || mSize - type.mStructMembersOffset < sizeof(VDD3DXStructMemberHeader) * type.mStructMemberCount)
			return false;

		// validate struct members
		const VDD3DXStructMemberHeader *structMembers = (const VDD3DXStructMemberHeader *)((const char *)mpHeader + type.mStructMembersOffset);

		for(uint32 i=0; i<type.mStructMemberCount; ++i) {
			if (!ValidateString(structMembers[i].mNameOffset))
				return false;

			if (!ValidateType(structMembers[i].mTypeOffset, nestingLevel + 1))
				return false;
		}
	}

	return true;
}

///////////////////////////////////////////////////////////////////////////

uint32 VDD3DXGetShaderSize(const uint32 *data) {
	const uint32 *p = data;

	for(;;) {
		const uint32 v = *p++;

		// check for end token
		if (v == 0x0000FFFF)
			return (const char *)p - (const char *)data;

		// check for comment token
		if ((v & UINT32_C(0x8000FFFF)) == 0x0000FFFE)
			p += v >> 16;
	}
}

bool VDD3DXCheckShaderSize(const uint32 *data, uint32 sizeInBytes) {
	const uint32 *p = data;
	uint32 dwordsLeft = sizeInBytes >> 2;

	while(dwordsLeft--) {
		const uint32 v = *p++;

		// check for end token
		if (v == 0x0000FFFF)
			return true;

		// check for comment token
		if ((v & UINT32_C(0x8000FFFF)) == 0x0000FFFE) {
			uint32 commentLen = v >> 16;

			if (dwordsLeft < commentLen)
				return false;

			p += commentLen;
			dwordsLeft -= commentLen;
		}
	}

	return false;
}

const void *VDD3DXFindShaderComment(const uint32 *data, uint32 size, uint32 fourcc, uint32& commentSizeInBytes) {
	// bit 31=0, xxxxFFFE -> comment token
	// 0000FFFF -> end token
	// bit 31=0 -> instruction token
	// bit 31=1 -> label token, source parameter token, destination parameter token
	// FFFExxxx -> vertex shader version
	// FFFFxxxx -> pixel shader version

	uint32 size4 = size >> 2;

	while(size4--) {
		const uint32 v = *data++;

		// check for end token
		if (v == 0x0000FFFF)
			return nullptr;

		// check for comment token
		if ((v & UINT32_C(0x8000FFFF)) == 0x0000FFFE) {
			const uint32 len = v >> 16;

			if (size4 < len)
				return nullptr;

			size4 -= len;

			if (len && *data == fourcc) {
				commentSizeInBytes = (len - 1) * 4;
				return data + 1;
			}

			// advance past comment data -- note that len is in DWORDs and includes
			// the FOURCC
			data += len;
		}
	}

	return nullptr;
}

bool VDD3DXGetShaderConstantTable(const uint32 *data, uint32 size, VDD3DXConstantTable& ct) {
	// Search for the CTAB chunk. The D3DX interface is not defined for endianness, but it also
	// doesn't run on any big endian devices....
	uint32 commentSize;
	const void *comment = VDD3DXFindShaderComment(data, size, VDMAKEFOURCC('C', 'T', 'A', 'B'), commentSize);

	if (!comment)
		return false;

	ct = VDD3DXConstantTable(comment, commentSize);

	return ct.Validate();
}
