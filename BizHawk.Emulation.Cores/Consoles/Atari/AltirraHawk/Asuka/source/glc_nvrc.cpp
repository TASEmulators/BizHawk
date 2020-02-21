#include <stdafx.h>
#include "glc.h"
#include <vd2/system/vdalloc.h>
#include <vd2/VDDisplay/opengl.h>

using namespace GLCIL;

namespace {
	struct RegisterCombinerSrc {
		uint16	mReg;
		uint16	mMapping;
		uint16	mPortion;

		void SetZero() { 
			mReg = GL_ZERO;
			mMapping = GL_SIGNED_IDENTITY_NV;
			mPortion = GL_RGBA;
		}

		void SetOne() { 
			mReg = GL_ZERO;
			mMapping = GL_UNSIGNED_INVERT_NV;
			mPortion = GL_RGBA;
		}

		void SetMinusOne() {
			mReg = GL_ZERO;
			mMapping = GL_EXPAND_NORMAL_NV;
			mPortion = GL_RGBA;
		}

		void Print(uint16 defaultPortion) const {
			const char *s = "zero";

			switch(mReg) {
				case GL_SPARE0_NV:				s = "r0"; break;
				case GL_SPARE1_NV:				s = "r1"; break;
				case GL_PRIMARY_COLOR_NV:		s = "v0"; break;
				case GL_SECONDARY_COLOR_NV:		s = "v1"; break;
				case GL_TEXTURE0_ARB:			s = "t0"; break;
				case GL_TEXTURE1_ARB:			s = "t1"; break;
				case GL_TEXTURE2_ARB:			s = "t2"; break;
				case GL_TEXTURE3_ARB:			s = "t3"; break;
				case GL_CONSTANT_COLOR0_NV:		s = "c0"; break;
				case GL_CONSTANT_COLOR1_NV:		s = "c1"; break;
			}

			switch(mMapping) {
				case GL_UNSIGNED_IDENTITY_NV:	printf("%s_sat", s); break;
				case GL_UNSIGNED_INVERT_NV:		printf("1-%s", s); break;
				case GL_SIGNED_IDENTITY_NV:		printf("%s", s); break;
				case GL_SIGNED_NEGATE_NV:		printf("-%s", s); break;
				case GL_EXPAND_NORMAL_NV:		printf("%s_bx2", s); break;
				case GL_EXPAND_NEGATE_NV:		printf("-%s_bx2", s); break;
				case GL_HALF_BIAS_NORMAL_NV:	printf("%s_bias", s); break;
				case GL_HALF_BIAS_NEGATE_NV:	printf("-%s_bias", s); break;
			}

			if (mPortion != defaultPortion) {
				switch(mPortion) {
					case GL_RGB:
						printf(".rgb");
						break;
					case GL_ALPHA:
						printf(".a");
						break;
					case GL_BLUE:
						printf(".b");
						break;
				}
			}
		}

		static uint8 GetRegisterCode(uint16 reg) {
			uint8 code;

			switch(reg) {
				case GL_ZERO:					code = 0x00; break;
				case GL_DISCARD_NV:				code = 0x01; break;
				case GL_SPARE0_NV:				code = 0x02; break;
				case GL_SPARE1_NV:				code = 0x03; break;
				case GL_PRIMARY_COLOR_NV:		code = 0x04; break;
				case GL_SECONDARY_COLOR_NV:		code = 0x05; break;
				case GL_CONSTANT_COLOR0_NV:		code = 0x06; break;
				case GL_CONSTANT_COLOR1_NV:		code = 0x07; break;
				case GL_SPARE0_PLUS_SECONDARY_COLOR_NV:		code = 0x08; break;
				case GL_E_TIMES_F_NV:			code = 0x09; break;
				case GL_TEXTURE0_ARB:			code = 0x0C; break;
				case GL_TEXTURE1_ARB:			code = 0x0D; break;
				case GL_TEXTURE2_ARB:			code = 0x0E; break;
				case GL_TEXTURE3_ARB:			code = 0x0F; break;
			}

			return code;
		}

		void Write(FILE *f) const {
			uint8 code = GetRegisterCode(mReg);

			switch(mMapping) {
				case GL_UNSIGNED_IDENTITY_NV:	break;
				case GL_UNSIGNED_INVERT_NV:		code |= 0x10; break;
				case GL_SIGNED_IDENTITY_NV:		code |= 0x20; break;
				case GL_SIGNED_NEGATE_NV:		code |= 0x30; break;
				case GL_EXPAND_NORMAL_NV:		code |= 0x40; break;
				case GL_EXPAND_NEGATE_NV:		code |= 0x50; break;
				case GL_HALF_BIAS_NORMAL_NV:	code |= 0x60; break;
				case GL_HALF_BIAS_NEGATE_NV:	code |= 0x70; break;
			}

			if (mPortion == GL_ALPHA)
				code |= 0x80;

			fprintf(f, "0x%02x", code);
		}
	};

	struct RegisterCombinerHalf {
		uint16	mDst[3];
		uint16	mScale;
		uint16	mBias;
		RegisterCombinerSrc	mSrc[4];
		bool	mbDotAB;
		bool	mbDotCD;
		bool	mbMux;

		RegisterCombinerHalf() {
			mDst[0] = mDst[1] = mDst[2] = GL_DISCARD_NV;
			mScale = GL_NONE;
			mBias = GL_NONE;
			mSrc[0].SetZero();
			mSrc[1].SetZero();
			mSrc[2].SetZero();
			mSrc[3].SetZero();
			mbDotAB = false;
			mbDotCD = false;
			mbMux = false;
		}

		void Write(FILE *f) {
			uint8 scaleBiasCode = 0;

			if (mBias == GL_BIAS_BY_NEGATIVE_ONE_HALF_NV) {
				if (mScale == GL_SCALE_BY_TWO_NV)
					scaleBiasCode = 5;
				else
					scaleBiasCode = 4;
			} else {
				switch(mScale) {
					case GL_SCALE_BY_TWO_NV:
						scaleBiasCode = 1;
						break;
					case GL_SCALE_BY_FOUR_NV:
						scaleBiasCode = 2;
						break;
					case GL_SCALE_BY_ONE_HALF_NV:
						scaleBiasCode = 3;
						break;
				}
			}

			uint8 dst0Code = RegisterCombinerSrc::GetRegisterCode(mDst[0]);
			uint8 dst1Code = RegisterCombinerSrc::GetRegisterCode(mDst[1]);
			uint8 dst2Code = RegisterCombinerSrc::GetRegisterCode(mDst[2]);

			fprintf(f, "0x%02x,0x%02x,", scaleBiasCode + (dst0Code << 4), dst1Code + (dst2Code << 4));
			fprintf(f, "0x%02x,", (mbDotAB ? 1 : 0) + (mbDotCD ? 2 : 0) + (mbMux ? 4 : 0));

			for(int i=0; i<4; ++i) {
				mSrc[i].Write(f);
				putc(',', f);
			}
		}
	};

	struct RegisterCombiner {
		RegisterCombinerHalf mColor;
		RegisterCombinerHalf mAlpha;
		int mConstantMapping[2];

		RegisterCombiner() {
			mConstantMapping[0] = -1;
			mConstantMapping[1] = -1;
		}

		void Write(FILE *f, bool rc2) {
			if (rc2)
				fprintf(f, "\t0x%02x,0x%02x,", (uint8)mConstantMapping[0], (uint8)mConstantMapping[1]);
			mColor.Write(f);
			mAlpha.Write(f);
			putc('\n', f);
		}
	};

	struct RegisterCombinerFinal {
		RegisterCombinerSrc mSrc[7];
		int mConstantMapping[2];

		RegisterCombinerFinal() {
			mConstantMapping[0] = -1;
			mConstantMapping[1] = -1;
			mSrc[0].SetZero();
			mSrc[0].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[1].mReg = GL_SPARE0_NV;
			mSrc[1].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[1].mPortion = GL_RGB;
			mSrc[2] = mSrc[1];
			mSrc[3].SetZero();
			mSrc[3].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[4].SetZero();
			mSrc[4].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[5].SetZero();
			mSrc[5].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[6].mReg = GL_SPARE0_NV;
			mSrc[6].mMapping = GL_UNSIGNED_IDENTITY_NV;
			mSrc[6].mPortion = GL_ALPHA;
		}

		void Write(FILE *f, bool rc2) {
			fputc('\t', f);
			if (rc2)
				fprintf(f, "0x%02x,0x%02x,", (uint8)mConstantMapping[0], (uint8)mConstantMapping[1]);

			for(int i=0; i<7; ++i) {
				mSrc[i].Write(f);
				fputc(',', f);
			}
			fputc('\n', f);
		}
	};

	class RegisterCombinerConfig : public vdrefcounted<IGLCFragmentShader> {
	public:
		RegisterCombinerConfig()
			: mGeneralCombinerCount(0)
			, mConstantsUsed(0)
		{
			memset(mConstants, 0, sizeof mConstants);
		}

		const char *GetTypeString() {
			return mConstantsUsed <= 2 && mGeneralCombinerCount <= 2 ? "kVDOpenGLFragmentShaderModeNVRC" : "kVDOpenGLFragmentShaderModeNVRC2";
		}

		void Write(FILE *f, const char *sym) {
			if (mConstantsUsed > 0) {
				fprintf(f, "static const float %s_constants[][4]={\n", sym);
				for(int i=0; i<mConstantsUsed; ++i) {
					fprintf(f, "\t{");

					for(int j=0; j<4; ++j) {
						char buf[512];
						sprintf(buf, "%g", mConstants[i][j]);
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

			bool rc2 = mConstantsUsed > 2 || mGeneralCombinerCount > 2;
			fprintf(f, "static const uint8 %s_bytecode[]={\n", sym);
			for(int i=0; i<mGeneralCombinerCount; ++i)
				mGeneralCombiners[i].Write(f, rc2);
			mFinalCombiner.Write(f, rc2);
			fprintf(f, "};\n");

			fprintf(f, "static const struct VDOpenGLNVRegisterCombinerConfig %s={\n", sym);
			fprintf(f, "\t%d, ", mConstantsUsed);
			fprintf(f, "%d, ", mGeneralCombinerCount);
			if (mConstantsUsed > 0)
				fprintf(f, "%s_constants, ", sym);
			else
				fprintf(f, "NULL, ");
			fprintf(f, "%s_bytecode\n", sym);
			fprintf(f, "};\n");
		}

	public:
		RegisterCombiner		mGeneralCombiners[8];
		RegisterCombinerFinal	mFinalCombiner;
		float	mConstants[16][4];
		int mConstantsUsed;
		int mGeneralCombinerCount;
	};
}

IGLCFragmentShader *CompileFragmentShaderNVRegisterCombiners(GLCErrorSink& errout, const GLCFragmentShader& shader, bool NV_register_combiners_2) {
	int combinerLimit = NV_register_combiners_2 ? 8 : 2;

	if (!NV_register_combiners_2 && shader.mUsedConstants > 3)
		errout.ThrowError(shader.mLocation, "NV_register_combiners only allows two constant registers");

	GLCFragmentShader::FragmentOps::const_iterator it(shader.mOps.begin()), itEnd(shader.mOps.end());
	bool seenFinalCombiner = false;
	int constantStageMask = 0;
	int constantStageCount = 0;
	bool combinerAlphaOp = false;
	bool combinerColorOp = false;

	vdrefptr<RegisterCombinerConfig> config(new RegisterCombinerConfig);
	RegisterCombiner *pCombiner = config->mGeneralCombiners;

	int constantMapping[16]={-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1};

	while(it!=itEnd) {
		const GLCFragmentOp& op = *it;
		bool isFinalCombiner = op.mInsn == kFSOpFinal;

		if (isFinalCombiner) {
			if (seenFinalCombiner)
				errout.ThrowError(op.mLocation, "Final combiner already configured");

			seenFinalCombiner = true;
		} else {
			if (config->mGeneralCombinerCount >= combinerLimit)
				errout.ThrowError(shader.mLocation, "Exceeded instruction count limit for profile (%d max)\n", combinerLimit);
		}

		// convert sources
		RegisterCombinerSrc rsrcs[7];
		bool allowAlpha = true;
		bool allowColor = true;

		for(int i=0; i<7; ++i) {
			const GLCSourceArg& opsrc = op.mSrcArgs[i];
			RegisterCombinerSrc& rsrc = rsrcs[i];
			int reg = opsrc.mReg;

			// map register
			switch(opsrc.mReg & kRegTypeMask) {
				case 0:
					rsrc.mReg = 0;
					break;
				case kRegC0:
					{
						int index = reg - kRegC0;

						if (constantMapping[index] == -1) {
							constantMapping[index] = config->mConstantsUsed;
							memcpy(config->mConstants[config->mConstantsUsed], shader.mConstants[index], sizeof(float)*4);
							index = config->mConstantsUsed++;
						}

						if (NV_register_combiners_2) {
							int *mappings = isFinalCombiner ? config->mFinalCombiner.mConstantMapping : pCombiner->mConstantMapping;

							if (index == mappings[0])
								rsrc.mReg = GL_CONSTANT_COLOR0_NV;
							else if (index == mappings[1])
								rsrc.mReg = GL_CONSTANT_COLOR1_NV;
							else if (mappings[0] == -1) {
								mappings[0] = index;
								rsrc.mReg = GL_CONSTANT_COLOR0_NV;
							} else if (mappings[1] == -1) {
								mappings[1] = index;
								rsrc.mReg = GL_CONSTANT_COLOR1_NV;
							} else
								errout.ThrowError(op.mLocation, "Too many constants used in combiner stage (2 max)");
						} else {
							rsrc.mReg = index ? GL_CONSTANT_COLOR1_NV : GL_CONSTANT_COLOR0_NV;
						}
					}
					break;
				case kRegV0:
					if (opsrc.mReg == kRegV0)
						rsrc.mReg = GL_PRIMARY_COLOR_NV;
					else
						rsrc.mReg = GL_SECONDARY_COLOR_NV;
					break;
				case kRegR0:
					if (opsrc.mReg - kRegR0 >= 2)
						errout.ThrowError(op.mLocation, "Invalid spare register (max 2)");

					if (opsrc.mReg == kRegR0)
						rsrc.mReg = GL_SPARE0_NV;
					else
						rsrc.mReg = GL_SPARE1_NV;
					break;

				case kRegT0:
					if (opsrc.mReg - kRegT0 >= 4)
						errout.ThrowError(op.mLocation, "Invalid spare register (max 2)");

					rsrc.mReg = GL_TEXTURE0_ARB + (opsrc.mReg - kRegT0);
					break;

				default:
					errout.ThrowError(op.mLocation, "Internal error");
			}

			// map swizzle
			if (opsrc.mSwizzle == kSwizzleNone || opsrc.mReg == 0)
				rsrc.mPortion = GL_RGBA;
			else if (opsrc.mSwizzle == kSwizzleRGB && opsrc.mSize == 3) {
				rsrc.mPortion = GL_RGB;
				allowAlpha = false;
			} else if (opsrc.mSwizzle == kSwizzleAlpha)
				rsrc.mPortion = GL_ALPHA;
			else if (opsrc.mSwizzle == kSwizzleBlue) {
				rsrc.mPortion = GL_BLUE;
				allowColor = false;
			} else
				errout.ThrowError(op.mLocation, "Swizzle not allowed in this profile: must be .a, .b, .rgb, or .rgba (none)");

			// map modifiers
			switch(opsrc.mMods) {
			case 0:
				rsrc.mMapping = GL_SIGNED_IDENTITY_NV;
				break;
			case kRegModNegate:
				rsrc.mMapping = GL_SIGNED_NEGATE_NV;
				break;
			case kRegModSaturate:
				rsrc.mMapping = GL_UNSIGNED_IDENTITY_NV;
				break;
			case kRegModComplement:
			case kRegModSaturate | kRegModComplement:
				rsrc.mMapping = GL_UNSIGNED_INVERT_NV;
				break;
			case kRegModBias | kRegModX2:
				rsrc.mMapping = GL_EXPAND_NORMAL_NV;
				break;
			case kRegModBias | kRegModX2 | kRegModNegate:
				rsrc.mMapping = GL_EXPAND_NEGATE_NV;
				break;
			case kRegModBias:
				rsrc.mMapping = GL_HALF_BIAS_NORMAL_NV;
				break;
			case kRegModBias | kRegModNegate:
				rsrc.mMapping = GL_HALF_BIAS_NEGATE_NV;
				break;
			default:
				errout.ThrowError(op.mLocation, "Unsupported source modifier");
				break;
			}
		}

		if (!isFinalCombiner) {
			// sanity check and convert destinations
			int dstMask = 0;
			uint16 rdsts[3];

			for(int dst=0; dst<3; ++dst) {
				int reg = op.mDstArgs[dst].mReg;

				rdsts[dst] = GL_DISCARD_NV;

				if (reg && reg != kRegDiscard) {
					int regMask = op.mDstArgs[dst].mWriteMask;
					if (!dstMask)
						dstMask = regMask;
					else if (dstMask != regMask)
						errout.ThrowError(op.mLocation, "Inconsistent destination write masks");

					switch(reg & kRegTypeMask) {
						case kRegC0:
							errout.ThrowError(op.mLocation, "Constant register cannot be used as destination");
							break;
						case kRegV0:
							errout.ThrowError(op.mLocation, "Interpolator register cannot be used as destination");
							break;
						case kRegT0:
							rdsts[dst] = GL_TEXTURE0_ARB + (reg - kRegT0);
							break;
						case kRegR0:
							if (reg - kRegR0 >= 2)
								errout.ThrowError(op.mLocation, "Invalid spare register (max 2)");

							rdsts[dst] = GL_SPARE0_NV + (reg - kRegR0);
							break;
						default:
							errout.ThrowError(op.mLocation, "Invalid destination register");
					}
				}
			}

			// assign to color and alpha combiner halves
			bool colorOp = false;
			bool alphaOp = false;

			switch(dstMask) {
				case 7:
					colorOp = true;
					break;
				case 8:
					alphaOp = true;
					break;
				case 15:
					alphaOp = true;
					colorOp = true;
					break;
				default:
					errout.ThrowError(op.mLocation, "Invalid destination write mask. Must be one of: .rgb, .a, none (.rgba)");
			}

			// convert instruction modifiers
			RegisterCombinerHalf chalf;

			switch(op.mModifiers) {
				case 0:
					chalf.mScale = GL_NONE;
					chalf.mBias = GL_NONE;
					break;
				case kInsnModD2:
					chalf.mScale = GL_SCALE_BY_ONE_HALF_NV;
					chalf.mBias = GL_NONE;
					break;
				case kInsnModX2:
					chalf.mScale = GL_SCALE_BY_TWO_NV;
					chalf.mBias = GL_NONE;
					break;
				case kInsnModX4:
					chalf.mScale = GL_SCALE_BY_FOUR_NV;
					chalf.mBias = GL_NONE;
					break;
				case kInsnModBias:
					chalf.mScale = GL_NONE;
					chalf.mBias = GL_BIAS_BY_NEGATIVE_ONE_HALF_NV;
					break;
				case kInsnModBX2:
					chalf.mScale = GL_SCALE_BY_TWO_NV;
					chalf.mBias = GL_BIAS_BY_NEGATIVE_ONE_HALF_NV;
					break;
				default:
					errout.ThrowError(op.mLocation, "Unsupported instruction modifier");
			}

			// create combiner configuration
			switch(op.mInsn) {
				case kFSOpMov:
					// A*1 + B*0
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = GL_DISCARD_NV;
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1].SetOne();
					chalf.mSrc[2].SetZero();
					chalf.mSrc[3].SetZero();
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpAdd:
					// A*1 + B*1
					chalf.mDst[0] = GL_DISCARD_NV;
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = rdsts[0];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1].SetOne();
					chalf.mSrc[2] = rsrcs[1];
					chalf.mSrc[3].SetOne();
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpSub:
					// A*1 + B*-1
					chalf.mDst[0] = GL_DISCARD_NV;
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = rdsts[0];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1].SetOne();
					chalf.mSrc[2] = rsrcs[1];
					chalf.mSrc[3].SetMinusOne();
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpMul:
					// A*B + 0*0
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = GL_DISCARD_NV;
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2].SetZero();
					chalf.mSrc[3].SetZero();
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpMad:
					// A*B + C*0
					chalf.mDst[0] = GL_DISCARD_NV;
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = rdsts[0];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3].SetOne();
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpLrp:
					// A*B + (1-A)*C
					chalf.mDst[0] = GL_DISCARD_NV;
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = rdsts[0];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[0];
					chalf.mSrc[3] = rsrcs[2];

					switch(chalf.mSrc[0].mMapping) {
					case GL_UNSIGNED_IDENTITY_NV:		// _sat
					case GL_SIGNED_IDENTITY_NV:			// (none)
						chalf.mSrc[0].mMapping = GL_UNSIGNED_IDENTITY_NV;
						chalf.mSrc[2].mMapping = GL_UNSIGNED_INVERT_NV;
						break;

					case GL_UNSIGNED_INVERT_NV:			// 1-reg
						chalf.mSrc[0].mMapping = GL_UNSIGNED_INVERT_NV;
						chalf.mSrc[2].mMapping = GL_UNSIGNED_IDENTITY_NV;
						break;

					default:
						errout.ThrowError(op.mLocation, "The first argument to 'lrp' can only use _sat and 1-reg modifiers");
					}
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpDp3:
					// dot(A.rgb, B.rgb)
					if (alphaOp)
						errout.ThrowError(op.mLocation, "'dp3' cannot be issued as an alpha instruction");
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = GL_DISCARD_NV;
					chalf.mDst[2] = GL_DISCARD_NV;
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2].SetZero();
					chalf.mSrc[3].SetZero();
					chalf.mbDotAB = true;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpMma:
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = rdsts[1];
					chalf.mDst[2] = rdsts[2];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3] = rsrcs[3];
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpMms:
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = rdsts[1];
					chalf.mDst[2] = rdsts[2];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3] = rsrcs[3];
					chalf.mbDotAB = false;
					chalf.mbDotCD = false;
					chalf.mbMux = true;
					break;
				case kFSOpDm:
					if (alphaOp)
						errout.ThrowError("'dm' cannot be issued as an alpha instruction");
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = rdsts[1];
					chalf.mDst[2] = GL_DISCARD_NV;
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3] = rsrcs[3];
					chalf.mbDotAB = true;
					chalf.mbDotCD = false;
					chalf.mbMux = false;
					break;
				case kFSOpDd:
					if (alphaOp)
						errout.ThrowError("'dd' cannot be issued as an alpha instruction");
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = rdsts[1];
					chalf.mDst[2] = GL_DISCARD_NV;
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3] = rsrcs[3];
					chalf.mbDotAB = true;
					chalf.mbDotCD = true;
					chalf.mbMux = false;
					break;
				case kFSOpDda:
					if (alphaOp)
						errout.ThrowError("'dda' cannot be issued as an alpha instruction");
					chalf.mDst[0] = rdsts[0];
					chalf.mDst[1] = rdsts[1];
					chalf.mDst[2] = rdsts[2];
					chalf.mSrc[0] = rsrcs[0];
					chalf.mSrc[1] = rsrcs[1];
					chalf.mSrc[2] = rsrcs[2];
					chalf.mSrc[3] = rsrcs[3];
					chalf.mbDotAB = true;
					chalf.mbDotCD = true;
					chalf.mbMux = false;
					break;
				default:
					errout.ThrowError("Instruction not supported in this profile");
			}

			if (alphaOp) {
				if (!allowAlpha)
					errout.ThrowError(op.mLocation, "Cannot use RGB argument in alpha operation");

				if (combinerAlphaOp)
					errout.ThrowError(op.mLocation, "Cannot co-issue two alpha operations");

				combinerAlphaOp = true;

				RegisterCombinerHalf& calpha = pCombiner->mAlpha;
				calpha = chalf;

				for(int i=0; i<4; ++i) {
					if (calpha.mSrc[i].mPortion == GL_RGBA)
						calpha.mSrc[i].mPortion = GL_ALPHA;
				}
			}

			if (colorOp) {
				if (!allowColor)
					errout.ThrowError(op.mLocation, "Cannot use .b swizzle on color operation");

				if (combinerColorOp)
					errout.ThrowError(op.mLocation, "Cannot co-issue two color operations");

				combinerColorOp = true;

				RegisterCombinerHalf& ccolor = pCombiner->mColor;
				ccolor = chalf;

				for(int i=0; i<4; ++i) {
					if (ccolor.mSrc[i].mPortion == GL_RGBA)
						ccolor.mSrc[i].mPortion = GL_RGB;
				}
			}
		} else {
			for(int i=0; i<7; ++i) {
				RegisterCombinerSrc& rsrc = config->mFinalCombiner.mSrc[i];
				
				rsrc = rsrcs[i];

				switch(rsrc.mMapping) {
					case GL_SIGNED_IDENTITY_NV:		// we implicitly saturate, so we allow this
						rsrc.mMapping = GL_UNSIGNED_IDENTITY_NV;
						break;
					case GL_UNSIGNED_IDENTITY_NV:
					case GL_UNSIGNED_INVERT_NV:
						break;
					default:
						errout.ThrowError(op.mLocation, "Inputs to the final combiner must use unsigned saturation or complement");
						break;
				}

				if (i < 6) {
					if (rsrc.mPortion == GL_RGBA)
						rsrc.mPortion = GL_RGB;

					if (rsrc.mPortion == GL_BLUE)
						errout.ThrowError(op.mLocation, "Final combiner inputs A-F must use .a, .rgb, or .rgba (none) swizzle");
				} else {
					if (rsrc.mPortion == GL_RGB)
						errout.ThrowError(op.mLocation, "Final combiner input G must use .a, .b, or .rgba (none) swizzle");

					if (rsrc.mPortion == GL_RGBA)
						rsrc.mPortion = GL_ALPHA;
				}
			}
		}

		++it;
		if (isFinalCombiner || it == itEnd || !it->mbCoIssue) {
			// flush combiner
			if (!isFinalCombiner) {
				++config->mGeneralCombinerCount;
				++pCombiner;
			}

			constantStageMask = 0;
			constantStageCount = 0;
			combinerAlphaOp = false;
			combinerColorOp = false;
		}
	}

#if 0
	// dump combiners
	for(int i=0; i<config->mConstantsUsed; ++i) {
		printf("def c%d, %g, %g, %g, %g\n", i, config->mConstants[i][0], config->mConstants[i][1], config->mConstants[i][2], config->mConstants[i][3]);
	}

	for(int i=0; i<config->mGeneralCombinerCount; ++i) {
		const RegisterCombiner& comb = config->mGeneralCombiners[i];
		int maxdst = 2;
		bool coissue = false;

		for(int j=0; j<2; ++j) {
			const RegisterCombinerHalf& chalf = j ? comb.mAlpha : comb.mColor;

			if (chalf.mDst[0] == GL_DISCARD_NV && chalf.mDst[1] == GL_DISCARD_NV && chalf.mDst[2] == GL_DISCARD_NV)
				continue;

			if (coissue)
				printf("+ ");
			coissue = true;

			if (chalf.mbDotAB) {
				if (chalf.mbDotCD)
					printf("dd");
				else
					printf("dm");
			} else {
				if (chalf.mbDotCD)
					printf("md");
				else {
					maxdst = 3;
					if (chalf.mbMux)
						printf("mms");
					else
						printf("mma");
				}
			}

			if (chalf.mBias == GL_BIAS_BY_NEGATIVE_ONE_HALF_NV) {
				if (chalf.mScale == GL_SCALE_BY_TWO_NV)
					printf("_bx2");
				else
					printf("_bias");
			} else {
				switch(chalf.mScale) {
					case GL_SCALE_BY_ONE_HALF_NV:
						printf("_d2");
						break;
					case GL_SCALE_BY_TWO_NV:
						printf("_x2");
						break;
					case GL_SCALE_BY_FOUR_NV:
						printf("_x4");
						break;
				}
			}

			for(int k=0; k<maxdst; ++k) {
				if (k)
					putchar(',');

				putchar(' ');

				switch(chalf.mDst[k]) {
					case GL_DISCARD_NV:		printf("discard"); break;
					case GL_SPARE0_NV:		printf("r0"); break;
					case GL_SPARE1_NV:		printf("r1"); break;
					case GL_TEXTURE0_ARB:	printf("t0"); break;
					case GL_TEXTURE1_ARB:	printf("t1"); break;
					case GL_TEXTURE2_ARB:	printf("t2"); break;
					case GL_TEXTURE3_ARB:	printf("t3"); break;
				}

				if (chalf.mDst[k] != GL_DISCARD_NV)
					printf(j ? ".a" : ".rgb");
			}

			for(int k=0; k<4; ++k) {
				const RegisterCombinerSrc& rsrc = chalf.mSrc[k];

				printf(", ");

				rsrc.Print(j ? GL_ALPHA : GL_RGB);
			}

			putchar('\n');
		}
	}

	printf("final");
	for(int i=0; i<7; ++i) {
		if (i)
			putchar(',');
		putchar(' ');
		config->mFinalCombiner.mSrc[i].Print(i == 6 ? GL_ALPHA : GL_RGB);
	}
	putchar('\n');
#endif

	return config.release();
}
