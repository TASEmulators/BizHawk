#include <stdafx.h>
#include "glc.h"
#include <vd2/system/vdalloc.h>
#include <vd2/VDDisplay/opengl.h>

using namespace GLCIL;

namespace {
	enum {
		kChannelOpAdd,
		kChannelOpSub,
		kChannelOpMul,
		kChannelOpMad,
		kChannelOpLerp,
		kChannelOpMov,
		kChannelOpCnd,
		kChannelOpCnd0,
		kChannelOpDot2Add,
		kChannelOpDot3,
		kChannelOpDot4,
		kChannelOpTexcrd,
		kChannelOpTexld,
		kChannelOpMask			= 0x0F,
		kChannelOpModScaleMask	= 0x70,
		kChannelOpModScaleX2	= 0x10,
		kChannelOpModScaleX4	= 0x20,
		kChannelOpModScaleX8	= 0x30,
		kChannelOpModScaleD2	= 0x40,
		kChannelOpModScaleD4	= 0x50,
		kChannelOpModScaleD8	= 0x60,
		kChannelOpModSaturate	= 0x80
	};

	enum {
		kChanRegR0,
		kChanRegR1,
		kChanRegR2,
		kChanRegR3,
		kChanRegR4,
		kChanRegR5,
		kChanRegC0,
		kChanRegC1,
		kChanRegC2,
		kChanRegC3,
		kChanRegC4,
		kChanRegC5,
		kChanRegC6,
		kChanRegC7,
		kChanRegZero,
		kChanRegOne,
		kChanRegV0,
		kChanRegV1,
		kChanSrcMod2X		= 0x100,
		kChanSrcModComp		= 0x200,
		kChanSrcModNegate	= 0x400,
		kChanSrcModBias		= 0x800,
		kChanSrcSwizzleRed		= 0x1000,
		kChanSrcSwizzleGreen	= 0x2000,
		kChanSrcSwizzleBlue		= 0x3000,
		kChanSrcSwizzleAlpha	= 0x4000,
		kChanDstMaskRed		= 0x10,
		kChanDstMaskGreen	= 0x20,
		kChanDstMaskBlue	= 0x40,
		kChanDstMaskAlpha	= 0x80
	};

	struct ATIFSChannelOp {
		uint8 mOpcode;
		uint8 mDstArg;
		int mSrcArgCnt;
		uint16 mSrcArgs[3];

		void Write(FILE *f) const {
			fprintf(f, "0x%02x,0x%02x,", mOpcode, mDstArg);

			for(int i=0; i<mSrcArgCnt; ++i)
				fprintf(f, "0x%02x,0x%02x,", mSrcArgs[i] & 0xff, mSrcArgs[i] >> 8);
		}
	};

	enum {
		kTexAddrT0			= 0x00,
		kTexAddrT1			= 0x01,
		kTexAddrT2			= 0x02,
		kTexAddrT3			= 0x03,
		kTexAddrT4			= 0x04,
		kTexAddrT5			= 0x05,
		kTexAddrR0			= 0x08,
		kTexAddrR1			= 0x09,
		kTexAddrR2			= 0x0A,
		kTexAddrR3			= 0x0B,
		kTexAddrR4			= 0x0C,
		kTexAddrR5			= 0x0D,
		kTexSwizzleXYZ		= 0x00,
		kTexSwizzleXYW		= 0x10,
		kTexSwizzleXYZ_DZ	= 0x20,
		kTexSwizzleXYW_DW	= 0x30,
		kTexModeTexcrd		= 0x40,
		kTexModeTexld		= 0x80
	};

	struct ATIFragmentShaderPhase {
		uint8	mTexOps[6];
		vdfastvector<ATIFSChannelOp> mOps;

		ATIFragmentShaderPhase() {
			memset(mTexOps, 0, sizeof mTexOps);
		}

		void Write(FILE *f) const {
			fprintf(f, "\t%d,", (int)mOps.size());

			for(int i=0; i<6; ++i)
				fprintf(f, "0x%02x,", mTexOps[i]);

			fputs("\n", f);
			for(int i=0, cnt=mOps.size(); i<cnt; ++i) {
				fputs("\t", f);
				mOps[i].Write(f);
				fputs("\n", f);
			}
		}
	};

	class ATIFragmentShader : public vdrefcounted<IGLCFragmentShader> {
	public:
		ATIFragmentShader() : mPhaseCount(1) {}

		const char *GetTypeString() {
			return "kVDOpenGLFragmentShaderModeATIFS";
		}

		void Write(FILE *f, const char *sym) {
			if (!mConstants.empty()) {
				fprintf(f, "static const float %s_constants[][4]={\n", sym);
				for(int i=0; i<(int)mConstants.size(); i += 4) {
					fprintf(f, "\t{");

					for(int j=0; j<4; ++j) {
						char buf[512];
						sprintf(buf, "%g", mConstants[i+j]);
						if (strchr(buf, '.'))
							fprintf(f, " %sf", buf);
						else
							fprintf(f, " %s.f", buf);

						if (j != 3)
							putc(',', f);
					}

					fprintf(f, " },\n");
				};
				fprintf(f, "};\n");
			}

			fprintf(f, "static const uint8 %s_bytecode[]={\n", sym);
			for(int i=0; i<mPhaseCount; ++i) {
				mPhases[i].Write(f);
			}
			fprintf(f, "\t0\n");
			fprintf(f, "};\n");

			fprintf(f, "static const struct VDOpenGLATIFragmentShaderConfig %s[]={\n", sym);
			fprintf(f, "\t%d, \n", (int)mConstants.size() >> 2);
			if (mConstants.empty())
				fprintf(f, "NULL, ");
			else
				fprintf(f, "%s_constants, ", sym);
			fprintf(f, "%s_bytecode\n", sym);
			fprintf(f, "};\n");
		}

	public:
		int mPhaseCount;
		ATIFragmentShaderPhase mPhases[2];
		vdfastvector<float> mConstants;
	};
}

IGLCFragmentShader *CompileFragmentShaderATIFragmentShader(GLCErrorSink& errout, const GLCFragmentShader& shader) {
	vdrefptr<ATIFragmentShader> out(new ATIFragmentShader);

	bool secondPhase = false;

	int constant_assignment[16] = {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1};
	int constantCount = 0;

	bool pairColorNext = true;
	int phasePairCount = 0;

	GLCFragmentShader::FragmentOps::const_iterator it(shader.mOps.begin()), itEnd(shader.mOps.end());
	for(; it!=itEnd; ++it) {
		const GLCFragmentOp& srcop = *it;
		int atiop = 0;
		int argcnt = 0;

		if (srcop.mInsn == kFSOpPhase) {
			if (secondPhase)
				errout.ThrowError(srcop.mLocation, "Second pass already started");

			out->mPhaseCount = 2;
			secondPhase = true;
			pairColorNext = true;
			phasePairCount = 0;
			continue;
		}

		if (srcop.mInsn == kFSOpTexld2Arg) {
			const GLCDestArg& opdst = srcop.mDstArgs[0];
			const GLCSourceArg& tcsrc = srcop.mSrcArgs[0];

			if (opdst.mWriteMask != 15)
				errout.ThrowError(srcop.mLocation, "texld cannot be used with a write mask");

			if ((opdst.mReg & kRegTypeMask) != kRegR0)
				errout.ThrowError(srcop.mLocation, "texld destination must be a temporary register");

			int index = opdst.mReg - kRegR0;

			if (out->mPhases[secondPhase].mTexOps[index])
				errout.ThrowError(srcop.mLocation, "Destination already used in a texld or texcrd instruction in this phase");

			uint8 encoding = 0;

			switch(tcsrc.mReg & kRegTypeMask) {
				case kRegR0:
					if (!secondPhase)
						errout.ThrowError(srcop.mLocation, "Dependent texture reads can only occur in second phase");

					encoding = kTexAddrR0 + (tcsrc.mReg - kRegR0);
					break;
				case kRegT0:
					encoding = kTexAddrT0 + (tcsrc.mReg - kRegT0);
					break;
				default:
					errout.ThrowError(srcop.mLocation, "texld source must be a temporary register (rn) or a texture coordinate register (tn)");
			}

			if ((tcsrc.mSwizzle & 0x3f) == kSwizzleRGB && tcsrc.mSize >= 3)
				encoding |= kTexSwizzleXYZ;
			else if ((tcsrc.mSwizzle & 0x3f) == 0x34 && tcsrc.mSize >= 3)
				encoding |= kTexSwizzleXYW;
			else
				errout.ThrowError(srcop.mLocation, "Swizzle on texture coordinate source must be .xyz or .xyw");

			out->mPhases[secondPhase].mTexOps[index] = encoding | kTexModeTexld;
			continue;
		}

		if (srcop.mInsn == kFSOpTexcrd) {
			const GLCDestArg& opdst = srcop.mDstArgs[0];
			const GLCSourceArg& tcsrc = srcop.mSrcArgs[0];

			if (opdst.mWriteMask != 15)
				errout.ThrowError(srcop.mLocation, "'texcrd' cannot be used with a write mask");

			if ((opdst.mReg & kRegTypeMask) != kRegR0)
				errout.ThrowError(srcop.mLocation, "'texcrd' destination must be a temporary register");

			int index = opdst.mReg - kRegR0;

			if (out->mPhases[secondPhase].mTexOps[index])
				errout.ThrowError(srcop.mLocation, "Destination already used in a texld or texcrd instruction in this phase");

			uint8 encoding = 0;

			switch(tcsrc.mReg & kRegTypeMask) {
				case kRegR0:
					if (!secondPhase)
						errout.ThrowError(srcop.mLocation, "'texcrd' cannot reference temporary register in phase 1");

					encoding = kTexAddrR0 + (tcsrc.mReg - kRegR0);
					break;
				case kRegT0:
					encoding = kTexAddrT0 + (tcsrc.mReg - kRegT0);
					break;
				default:
					errout.ThrowError(srcop.mLocation, "'texld': Source must be a temporary register (rn) or a texture coordinate register (tn)");
			}

			if ((tcsrc.mSwizzle & 0x3f) == kSwizzleRGB && tcsrc.mSize >= 3)
				encoding |= kTexSwizzleXYZ;
			else if ((tcsrc.mSwizzle & 0x3f) == 0x34 && tcsrc.mSize >= 3)
				encoding |= kTexSwizzleXYW;
			else
				errout.ThrowError(srcop.mLocation, "Swizzle on texture coordinate source must be .xyz or .xyw");

			out->mPhases[secondPhase].mTexOps[index] = encoding | kTexModeTexcrd;
			continue;
		}

		switch(srcop.mInsn) {
			case kFSOpMov:
				atiop = kChannelOpMov;
				argcnt = 1;
				break;
			case kFSOpAdd:
				atiop = kChannelOpAdd;
				argcnt = 2;
				break;
			case kFSOpSub:
				atiop = kChannelOpSub;
				argcnt = 2;
				break;
			case kFSOpMul:
				atiop = kChannelOpMul;
				argcnt = 2;
				break;
			case kFSOpMad:
				atiop = kChannelOpMad;
				argcnt = 3;
				break;
			case kFSOpLrp:
				atiop = kChannelOpLerp;
				argcnt = 3;
				break;
			case kFSOpDp3:
				atiop = kChannelOpDot3;
				argcnt = 2;
				break;
			case kFSOpDp4:
				atiop = kChannelOpDot4;
				argcnt = 2;
				break;

			default:
				errout.ThrowError(srcop.mLocation, "Instruction not allowed in ATI_fragment_shader profile");
		}

		ATIFSChannelOp op;

		// convert destination argument
		const GLCDestArg& dstarg = srcop.mDstArgs[0];
		op.mDstArg = 0;

		switch(dstarg.mReg & kRegTypeMask) {
			case kRegT0:
				errout.ThrowError(srcop.mLocation, "Texture coordinate register cannot be used as destination");
				break;
			case kRegC0:
				errout.ThrowError(srcop.mLocation, "Constant register cannot be used as destination");
				break;
			case kRegR0:
				op.mDstArg = kChanRegR0 + (dstarg.mReg - kRegR0);
				break;
			case kRegV0:
				errout.ThrowError(srcop.mLocation, "Interpolator register cannot be used as destination");
				break;
		}

		// convert destination write mask
		if (dstarg.mWriteMask & 1)
			op.mDstArg |= kChanDstMaskRed;
		if (dstarg.mWriteMask & 2)
			op.mDstArg |= kChanDstMaskGreen;
		if (dstarg.mWriteMask & 4)
			op.mDstArg |= kChanDstMaskBlue;
		if (dstarg.mWriteMask & 8)
			op.mDstArg |= kChanDstMaskAlpha;

		bool isDot = (atiop == kChannelOpDot3 || atiop == kChannelOpDot4);
		bool isColor = (dstarg.mWriteMask & 7) != 0 || isDot;
		bool isAlpha = (dstarg.mWriteMask & 8) != 0 || atiop == kChannelOpDot4;

		if (srcop.mbCoIssue) {
			if (isColor && isAlpha)
				errout.ThrowError(srcop.mLocation, "A color+alpha instruction cannot be co-issued");

			if (isColor)
				errout.ThrowError(srcop.mLocation, "A color instruction must be the first in a co-issued pair");

			if (isAlpha && pairColorNext)
				errout.ThrowError(srcop.mLocation, "Cannot co-issue two alpha instructions");
		}

		if (isColor || (isAlpha && pairColorNext)) {
			if (phasePairCount >= 8)
				errout.ThrowError(srcop.mLocation, "Instruction pair count exceeded for this phase (8 max)");
			++phasePairCount;
		}

		pairColorNext = isAlpha;

		// convert instruction modifiers
		op.mOpcode = atiop;

		if (srcop.mModifiers & ~(kInsnModD2 | kInsnModD4 | kInsnModD8 | kInsnModX2 | kInsnModX4 | kInsnModX8 | kInsnModSat))
			errout.ThrowError(srcop.mLocation, "Unsupported instruction modifier");

		if (srcop.mModifiers & kInsnModD2)
			op.mOpcode |= kChannelOpModScaleD2;
		else if (srcop.mModifiers & kInsnModD4)
			op.mOpcode |= kChannelOpModScaleD4;
		else if (srcop.mModifiers & kInsnModD8)
			op.mOpcode |= kChannelOpModScaleD8;
		else if (srcop.mModifiers & kInsnModX2)
			op.mOpcode |= kChannelOpModScaleX2;
		else if (srcop.mModifiers & kInsnModX4)
			op.mOpcode |= kChannelOpModScaleX4;
		else if (srcop.mModifiers & kInsnModX8)
			op.mOpcode |= kChannelOpModScaleX8;

		if (srcop.mModifiers & kInsnModSat)
			op.mOpcode |= kChannelOpModSaturate;

		// convert source arguments
		op.mSrcArgCnt = argcnt;
		for(int i=0; i<argcnt; ++i) {
			const GLCSourceArg& srcarg = srcop.mSrcArgs[i];
			int atireg = 0;
			int index = srcarg.mReg & 0xff;

			switch(srcarg.mReg & kRegTypeMask) {
				case kRegT0:
					errout.ThrowError(srcop.mLocation, "Texture coordinate register can only be used as an argument to texld or texcrd");
					break;
				case kRegC0:
					if (constant_assignment[index] < 0) {
						if (constantCount >= 8)
							errout.ThrowError(srcop.mLocation, "Constant count limit exceeded (8 max)");

						constant_assignment[index] = constantCount++;
						out->mConstants.insert(out->mConstants.end(), shader.mConstants[index], shader.mConstants[index] + 4);
					}
					atireg = kChanRegC0 + constant_assignment[index];
					break;
				case kRegR0:
					atireg = kChanRegR0 + index;
					break;
				case kRegV0:
					atireg = kChanRegV0 + index;
					break;
			}

			if (srcarg.mMods & ~(kRegModNegate | kRegModComplement | kRegModBias | kRegModX2))
				errout.ThrowError(srcop.mLocation, "Unsupported source modifier");

			if (srcarg.mMods & kRegModNegate)
				atireg |= kChanSrcModNegate;

			if (srcarg.mMods & kRegModComplement)
				atireg |= kChanSrcModComp;

			if (srcarg.mMods & kRegModBias)
				atireg |= kChanSrcModBias;

			if (srcarg.mMods & kRegModX2)
				atireg |= kChanSrcMod2X;

			switch(srcarg.mSwizzle) {
				case kSwizzleNone:
					break;

				case kSwizzleRGB:
					if (isAlpha)
						errout.ThrowError(srcop.mLocation, "Cannot use .rgb swizzle with alpha instruction");
					break;

				case kSwizzleRed:
					atireg |= kChanSrcSwizzleRed;
					break;
				case kSwizzleGreen:
					atireg |= kChanSrcSwizzleGreen;
					break;
				case kSwizzleBlue:
					atireg |= kChanSrcSwizzleBlue;
					break;
				case kSwizzleAlpha:
					atireg |= kChanSrcSwizzleAlpha;
					break;

				default:
					errout.ThrowError(srcop.mLocation, "Swizzle not supported in ATI_fragment_shader profile");
					break;
			}

			op.mSrcArgs[i] = atireg;
		}

		for(int i=argcnt; i<3; ++i)
			op.mSrcArgs[i] = 0;

		out->mPhases[secondPhase].mOps.push_back(op);
	}

	return out.release();
}
