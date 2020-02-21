#ifndef f_GLC_H
#define f_GLC_H

#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>

namespace GLCIL {
	enum {
		kFSOpNone,
		kFSOpMov,
		kFSOpAdd,
		kFSOpSub,
		kFSOpMul,
		kFSOpMad,
		kFSOpLrp,
		kFSOpDp3,
		kFSOpDp4,
		kFSOpMma,		// mul/mul/add
		kFSOpMms,		// mul/mul/sel
		kFSOpDm,		// dp3/mul
		kFSOpDd,		// dp3/dp3
		kFSOpDda,		// dp3/dp3/add (check GL_NV_register_combiners -- this is actually allowed!)
		kFSOpFinal,
		kFSOpDef,
		kFSOpTexld2Arg,
		kFSOpTexcrd,
		kFSOpPhase
	};

	enum {
		kInsnModD2		= 0x0001,
		kInsnModD4		= 0x0002,
		kInsnModD8		= 0x0004,
		kInsnModX2		= 0x0008,
		kInsnModX4		= 0x0010,
		kInsnModX8		= 0x0020,
		kInsnModBias	= 0x0040,
		kInsnModBX2		= 0x0080,
		kInsnModSat		= 0x0100
	};

	enum {
		kRegModNegate		= 0x0001,
		kRegModSaturate		= 0x0002,
		kRegModComplement	= 0x0004,
		kRegModBias			= 0x0008,
		kRegModX2			= 0x0010,
	};

	enum {
		kRegDiscard	= 0x0001,
		kRegZero	= 0x0002,
		kRegR0		= 0x0100,
		kRegV0		= 0x0200,
		kRegT0		= 0x0300,
		kRegC0		= 0x0400,
		kRegTypeMask= 0xFF00
	};

	enum {
		kSwizzleRed		= 0x00,
		kSwizzleGreen	= 0x55,
		kSwizzleBlue	= 0xAA,
		kSwizzleAlpha	= 0xFF,
		kSwizzleRGB		= 0x24,
		kSwizzleNone	= 0xE4
	};
}

struct GLCDestArg {
	uint16	mReg;
	uint8	mWriteMask;
};

struct GLCSourceArg {
	uint16	mReg;
	uint16	mMods;
	uint8	mSwizzle;
	uint8	mSize;
};

struct GLCCodeLocation {
	const char *mpFileName;
	int		mLine;
	int		mColumn;
};

struct GLCFragmentOp {
	GLCCodeLocation	mLocation;
	uint32					mInsn;
	uint32					mModifiers;
	bool					mbCoIssue;
	GLCDestArg			mDstArgs[3];
	GLCSourceArg		mSrcArgs[7];
};

struct GLCFragmentShader {
	GLCCodeLocation		mLocation;

	typedef vdfastvector<GLCFragmentOp> FragmentOps;
	FragmentOps mOps;

	uint32		mUsedConstants;
	float		mConstants[16][4];

	GLCFragmentShader() : mUsedConstants(0) {
		memset(mConstants, 0, sizeof mConstants);
	}
};

class GLCErrorSink {
public:
	VDNORETURN void ThrowError(const char *format, ...);
	VDNORETURN void ThrowError(const GLCCodeLocation& loc, const char *format, ...);

	virtual GLCCodeLocation GetLocation() const = 0;
};

class IGLCFragmentShader : public IVDRefCount {
public:
	virtual ~IGLCFragmentShader() {}

	virtual const char *GetTypeString() = 0;
	virtual void Write(FILE *f, const char *sym) = 0;
};

#endif
