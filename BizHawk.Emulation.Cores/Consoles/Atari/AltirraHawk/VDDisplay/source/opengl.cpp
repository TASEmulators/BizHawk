//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2006 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <vd2/system/vdtypes.h>
#include <vd2/VDDisplay/opengl.h>
#include <windows.h>

namespace {
	static const char *const kWGLFunctions[]={
		"wglCreateContext",
		"wglDeleteContext",
		"wglMakeCurrent",
		"wglGetProcAddress",
		"wglSwapBuffers",
		"wglUseFontBitmapsA",
	};

	static const char *const kGLFunctions[]={
		"glAlphaFunc",
		"glBegin",
		"glBindTexture",
		"glBlendFunc",
		"glCallList",
		"glClear",
		"glClearColor",
		"glColor4d",
		"glColor4f",
		"glColor4ub",
		"glColorMask",
		"glCopyTexSubImage2D",
		"glDeleteLists",
		"glDeleteTextures",
		"glDepthFunc",
		"glDepthMask",
		"glDisable",
		"glDrawBuffer",
		"glEnable",
		"glEnd",
		"glEndList",
		"glFeedbackBuffer",
		"glFinish",
		"glFlush",
		"glFrontFace",
		"glGetError",
		"glGetFloatv",
		"glGetIntegerv",
		"glGetTexLevelParameteriv",
		"glGenLists",
		"glGetString",
		"glGenTextures",
		"glLoadIdentity",
		"glLoadMatrixd",
		"glMatrixMode",
		"glNewList",
		"glOrtho",
		"glPixelStorei",
		"glPopAttrib",
		"glPushAttrib",
		"glReadBuffer",
		"glReadPixels",
		"glRenderMode",
		"glScissor",
		"glTexCoord2d",
		"glTexCoord2f",
		"glTexCoord2fv",
		"glTexEnvf",
		"glTexEnvi",
		"glTexImage1D",
		"glTexImage2D",
		"glTexParameterfv",
		"glTexParameteri",
		"glTexSubImage1D",
		"glTexSubImage2D",
		"glTranslatef",
		"glVertex2d",
		"glVertex2f",
		"glVertex2i",
		"glVertex3fv",
		"glViewport",
	};

	static const char *const kGLExtFunctions[]={
		// ARB_multitexture
		"glActiveTextureARB",
		"glMultiTexCoord2fARB",

		// ARB_vertex_buffer_object (EXT_pixel_buffer_object/ARB_pixel_buffer_object)
		"glBindBufferARB",
		"glDeleteBuffersARB",
		"glGenBuffersARB",
		"glIsBufferARB",
		"glBufferDataARB",
		"glBufferSubDataARB",
		"glGetBufferSubDataARB",
		"glMapBufferARB",
		"glUnmapBufferARB",
		"glGetBufferParameterivARB",
		"glGetBufferPointervARB",

		// NV_register_combiners
		"glCombinerParameterfvNV",
		"glCombinerParameterivNV",
		"glCombinerParameterfNV",
		"glCombinerParameteriNV",
		"glCombinerInputNV",
		"glCombinerOutputNV",
		"glFinalCombinerInputNV",
		"glGetCombinerInputParameterfvNV",
		"glGetCombinerInputParameterivNV",
		"glGetCombinerOutputParameterfvNV",
		"glGetCombinerOutputParameterivNV",
		"glGetFinalCombinerInputParameterfvNV",
		"glGetFinalCombinerInputParameterivNV",

		// NV_register_combiners2
		"glCombinerStageParameterfvNV",

		// ATI_fragment_shader
		"glGenFragmentShadersATI",
		"glBindFragmentShaderATI",
		"glDeleteFragmentShaderATI",
		"glBeginFragmentShaderATI",
		"glEndFragmentShaderATI",
		"glPassTexCoordATI",
		"glSampleMapATI",
		"glColorFragmentOp1ATI",
		"glColorFragmentOp2ATI",
		"glColorFragmentOp3ATI",
		"glAlphaFragmentOp1ATI",
		"glAlphaFragmentOp2ATI",
		"glAlphaFragmentOp3ATI",
		"glSetFragmentShaderConstantATI",

		// NV_occlusion_query
		"glGenOcclusionQueriesNV",
		"glDeleteOcclusionQueriesNV",
		"glIsOcclusionQueryNV",
		"glBeginOcclusionQueryNV",
		"glEndOcclusionQueryNV",
		"glGetOcclusionQueryivNV",
		"glGetOcclusionQueryuivNV",

		// EXT_framebuffer_object
		"glIsRenderbufferEXT",
		"glBindRenderbufferEXT",
		"glDeleteRenderbuffersEXT",
		"glGenRenderbuffersEXT",
		"glRenderbufferStorageEXT",
		"glGetRenderbufferParameterivEXT",
		"glIsFramebufferEXT",
		"glBindFramebufferEXT",
		"glDeleteFramebuffersEXT",
		"glGenFramebuffersEXT",
		"glCheckFramebufferStatusEXT",
		"glFramebufferTexture1DEXT",
		"glFramebufferTexture2DEXT",
		"glFramebufferTexture3DEXT",
		"glFramebufferRenderbufferEXT",
		"glGetFramebufferAttachmentParameterivEXT",
		"glGenerateMipmapEXT",

		// ARB_vertex_program
		"glVertexAttrib1sARB",
		"glVertexAttrib1fARB",
		"glVertexAttrib1dARB",
		"glVertexAttrib2sARB",
		"glVertexAttrib2fARB",
		"glVertexAttrib2dARB",
		"glVertexAttrib3sARB",
		"glVertexAttrib3fARB",
		"glVertexAttrib3dARB",
		"glVertexAttrib4sARB",
		"glVertexAttrib4fARB",
		"glVertexAttrib4dARB",
		"glVertexAttrib4NubARB",
		"glVertexAttrib1svARB",
		"glVertexAttrib1fvARB",
		"glVertexAttrib1dvARB",
		"glVertexAttrib2svARB",
		"glVertexAttrib2fvARB",
		"glVertexAttrib2dvARB",
		"glVertexAttrib3svARB",
		"glVertexAttrib3fvARB",
		"glVertexAttrib3dvARB",
		"glVertexAttrib4bvARB",
		"glVertexAttrib4svARB",
		"glVertexAttrib4ivARB",
		"glVertexAttrib4ubvARB",
		"glVertexAttrib4usvARB",
		"glVertexAttrib4uivARB",
		"glVertexAttrib4fvARB",
		"glVertexAttrib4dvARB",
		"glVertexAttrib4NbvARB",
		"glVertexAttrib4NsvARB",
		"glVertexAttrib4NivARB",
		"glVertexAttrib4NubvARB",
		"glVertexAttrib4NusvARB",
		"glVertexAttrib4NuivARB",
		"glVertexAttribPointerARB",
		"glEnableVertexAttribArrayARB",
		"glDisableVertexAttribArrayARB",
		"glProgramStringARB",
		"glBindProgramARB",
		"glDeleteProgramsARB",
		"glGenProgramsARB",
		"glProgramEnvParameter4dARB",
		"glProgramEnvParameter4dvARB",
		"glProgramEnvParameter4fARB",
		"glProgramEnvParameter4fvARB",
		"glProgramLocalParameter4dARB",
		"glProgramLocalParameter4dvARB",
		"glProgramLocalParameter4fARB",
		"glProgramLocalParameter4fvARB",
		"glGetProgramEnvParameterdvARB",
		"glGetProgramEnvParameterfvARB",
		"glGetProgramLocalParameterdvARB",
		"glGetProgramLocalParameterfvARB",
		"glGetProgramivARB",
		"glGetProgramStringARB",
		"glGetVertexAttribdvARB",
		"glGetVertexAttribfvARB",
		"glGetVertexAttribivARB",
		"glGetVertexAttribPointervARB",
		"glIsProgramARB",

		// EXT_blend_minmax
		"glBlendEquationEXT",

		// EXT_secondary_color
		"glSecondaryColor3ubEXT",

		// WGL_ARB_extensions_string
		"wglGetExtensionsStringARB",

		// WGL_EXT_extensions_string
		"wglGetExtensionsStringEXT",

		// WGL_ARB_make_current_read
		"wglMakeContextCurrentARB",
		"wglGetCurrentReadDCARB",

		// WGL_EXT_swap_control
		"wglSwapIntervalEXT",
		"wglGetSwapIntervalEXT",
	};
}

VDOpenGLBinding::VDOpenGLBinding()
	: mhmodOGL(NULL)
	, mhdc(NULL)
	, mhglrc(NULL)
{
}

VDOpenGLBinding::~VDOpenGLBinding() {
}

bool VDOpenGLBinding::Init() {
	mhmodOGL = LoadLibraryW(L"opengl32");
	if (!mhmodOGL)
		return false;

	// pull wgl functions
	for(int i=0; i<sizeof(kWGLFunctions)/sizeof(kWGLFunctions[0]); ++i) {
		void *p = (void *)GetProcAddress(mhmodOGL, kWGLFunctions[i]);

		if (!p) {
			Shutdown();
			return false;
		}

		((void **)static_cast<VDAPITableWGL *>(this))[i] = p;
	}

	return true;
}

void VDOpenGLBinding::Shutdown() {
	End();
	Detach();

	if (mhmodOGL) {
		FreeLibrary(mhmodOGL);
		mhmodOGL = NULL;
	}
}

bool VDOpenGLBinding::Attach(HDC hdc, int minColorBits, int minAlphaBits, int minDepthBits, int minStencilBits, bool doubleBuffer) {
	PIXELFORMATDESCRIPTOR pfd={};

	pfd.nSize			= sizeof(PIXELFORMATDESCRIPTOR);
	pfd.nVersion		= 1;
	pfd.dwFlags			= PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL;
	if (doubleBuffer)
		pfd.dwFlags |= PFD_DOUBLEBUFFER;
	pfd.iPixelType		= PFD_TYPE_RGBA;
	pfd.cColorBits		= minColorBits;
	pfd.cAlphaBits		= minAlphaBits;
	pfd.cDepthBits		= minDepthBits;
	pfd.cStencilBits	= minStencilBits;
	pfd.iLayerType		= PFD_MAIN_PLANE;

	int pf = ChoosePixelFormat(hdc, &pfd);
	if (!pf) {
		Detach();
		return false;
	}

	if (!SetPixelFormat(hdc, pf, &pfd)) {
		Detach();
		return false;
	}

	mhglrc = wglCreateContext(hdc);
	if (!mhglrc)
		return false;

	if (!Begin(hdc)) {
		Detach();
		return false;
	}

	for(int i=0; i<sizeof(kGLFunctions)/sizeof(kGLFunctions[0]); ++i) {
		void *p = (void *)GetProcAddress(mhmodOGL, kGLFunctions[i]);

		if (!p) {
			Detach();
			return false;
		}

		((void **)static_cast<VDAPITableOpenGL *>(this))[i] = p;
	}

	for(int i=0; i<sizeof(kGLExtFunctions)/sizeof(kGLExtFunctions[0]); ++i) {
		void *p = (void *)wglGetProcAddress(kGLExtFunctions[i]);

		((void **)static_cast<VDAPITableOpenGLEXT *>(this))[i] = p;
	}

	const char *ext = (const char *)glGetString(GL_EXTENSIONS);

	ARB_fragment_program = false;
	ARB_multitexture = false;
	ARB_pixel_buffer_object = false;
	ARB_vertex_program = false;
	ATI_fragment_shader = false;
	EXT_blend_minmax = false;
	EXT_blend_subtract = false;
	EXT_framebuffer_object = false;
	EXT_pixel_buffer_object = false;
	EXT_texture_env_combine = false;
	EXT_texture_edge_clamp = false;
	NV_occlusion_query = false;
	NV_register_combiners = false;
	NV_register_combiners2 = false;

	if (ext) {
		for(;;) {
			while(*ext == ' ')
				++ext;

			if (!*ext)
				break;

			const char *start = ext;
			while(*ext && *ext != ' ')
				++ext;

			int len = ext - start;

			switch(len) {
			case 19:
				if (!memcmp(start, "GL_ARB_multitexture", 19))
					ARB_multitexture = true;
				else if (!memcmp(start, "GL_EXT_blend_minmax", 19))
					EXT_blend_minmax = true;
				break;

			case 20:
				if (!memcmp(start, "GL_EXT_blend_subtract", 20))
					EXT_blend_subtract = true;
				break;

			case 21:
				if (!memcmp(start, "GL_NV_occlusion_query", 21))
					NV_occlusion_query = true;
				else if (!memcmp(start, "GL_ARB_vertex_program", 21))
					ARB_vertex_program = true;
				break;

			case 22:
				if (!memcmp(start, "GL_ATI_fragment_shader", 22))
					ATI_fragment_shader = true;
				else if (!memcmp(start, "GL_EXT_secondary_color", 22))
					EXT_secondary_color = true;
				break;

			case 23:
				if (!memcmp(start, "GL_ARB_fragment_program", 23))
					ARB_fragment_program = true;
				break;

			case 24:
				if (!memcmp(start, "GL_NV_register_combiners", 24))
					NV_register_combiners = true;
				break;
			case 25:
				if (!memcmp(start, "GL_NV_register_combiners2", 25))
					NV_register_combiners2 = true;
				else if (!memcmp(start, "GL_EXT_framebuffer_object", 25))
					EXT_framebuffer_object = true;
				else if (!memcmp(start, "GL_EXT_texture_edge_clamp", 25))
					EXT_texture_edge_clamp = true;
				break;
			case 26:
				if (!memcmp(start, "GL_EXT_pixel_buffer_object", 26))
					EXT_pixel_buffer_object = true;
				else if (!memcmp(start, "GL_ARB_pixel_buffer_object", 26))
					EXT_pixel_buffer_object = ARB_pixel_buffer_object = true;
				else if (!memcmp(start, "GL_EXT_texture_env_combine", 26))
					EXT_texture_env_combine = true;
				else if (!memcmp(start, "GL_ARB_texture_env_combine", 26))
					EXT_texture_env_combine = true;
				break;
			}
		}
	}

	ext = NULL;
	if (wglGetExtensionsStringARB)
		ext = wglGetExtensionsStringARB(hdc);
	else if (wglGetExtensionsStringEXT)
		ext = wglGetExtensionsStringEXT();

	EXT_swap_control = false;
	ARB_make_current_read = false;
	if (ext) {
		for(;;) {
			while(*ext == ' ')
				++ext;

			if (!*ext)
				break;

			const char *start = ext;
			while(*ext && *ext != ' ')
				++ext;

			int len = ext - start;

			switch(len) {
			case 20:
				if (!memcmp(start, "WGL_EXT_swap_control", 20))
					EXT_swap_control = true;
				break;
			case 25:
				if (!memcmp(start, "WGL_ARB_make_current_read", 25))
					ARB_make_current_read = true;
				break;
			}
		}
	}

	End();

	return true;
}

bool VDOpenGLBinding::AttachAux(HDC hdc, int minColorBits, int minAlphaBits, int minDepthBits, int minStencilBits, bool doubleBuffer) {
	PIXELFORMATDESCRIPTOR pfd={};

	pfd.nSize			= sizeof(PIXELFORMATDESCRIPTOR);
	pfd.nVersion		= 1;
	pfd.dwFlags			= PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL;
	if (doubleBuffer)
		pfd.dwFlags |= PFD_DOUBLEBUFFER;
	pfd.iPixelType		= PFD_TYPE_RGBA;
	pfd.cColorBits		= minColorBits;
	pfd.cAlphaBits		= minAlphaBits;
	pfd.cDepthBits		= minDepthBits;
	pfd.cStencilBits	= minStencilBits;
	pfd.iLayerType		= PFD_MAIN_PLANE;

	int pf = ChoosePixelFormat(hdc, &pfd);
	if (!pf)
		return false;

	if (!SetPixelFormat(hdc, pf, &pfd))
		return false;

	return true;
}

void VDOpenGLBinding::Detach() {
	if (mhglrc) {
		wglDeleteContext(mhglrc);
		mhglrc = NULL;
	}
}

bool VDOpenGLBinding::Begin(HDC hdc) {
	if (!wglMakeCurrent(hdc, mhglrc))
		return false;

	mhdc = hdc;
	return true;
}

void VDOpenGLBinding::End() {
	if (mhdc) {
		wglMakeCurrent(mhdc, NULL);
		mhdc = NULL;
	}
}

namespace {
	void CreateNVRegisterCombinerSetup(VDOpenGLBinding& gl, const VDOpenGLNVRegisterCombinerConfig& config, bool nvrc2) {
		// load base constants
		if (!nvrc2) {
			for(int i=0; i<config.mConstantCount; ++i)
				gl.glCombinerParameterfvNV(GL_CONSTANT_COLOR0_NV + i, config.mpConstants[i]);
		}

		static const GLenum kRegisterTable[16]={
			GL_ZERO,
			GL_DISCARD_NV,
			GL_SPARE0_NV,
			GL_SPARE1_NV,
			GL_PRIMARY_COLOR_NV,
			GL_SECONDARY_COLOR_NV,
			GL_CONSTANT_COLOR0_NV,
			GL_CONSTANT_COLOR1_NV,
			GL_SPARE0_PLUS_SECONDARY_COLOR_NV,
			GL_E_TIMES_F_NV,
			GL_ZERO,
			GL_ZERO,
			GL_TEXTURE0_ARB,
			GL_TEXTURE1_ARB,
			GL_TEXTURE2_ARB,
			GL_TEXTURE3_ARB,
		};

		static const GLenum kScaleTable[4]={
			GL_NONE,
			GL_SCALE_BY_TWO_NV,
			GL_SCALE_BY_FOUR_NV,
			GL_SCALE_BY_ONE_HALF_NV,
		};

		static const GLenum kSourceMappingTable[8]={
			GL_UNSIGNED_IDENTITY_NV,
			GL_UNSIGNED_INVERT_NV,
			GL_SIGNED_IDENTITY_NV,
			GL_SIGNED_NEGATE_NV,
			GL_EXPAND_NORMAL_NV,
			GL_EXPAND_NEGATE_NV,
			GL_HALF_BIAS_NORMAL_NV,
			GL_HALF_BIAS_NEGATE_NV
		};

		// create combiner stages
		const uint8 *src = config.mpByteCode;
		for(int stage=0; stage<config.mGeneralCombinerCount; ++stage) {
			GLenum stageToken = GL_COMBINER0_NV + stage;

			// load per-stage colors
			if (nvrc2) {
				uint8 c0 = src[0];
				uint8 c1 = src[1];
				src += 2;

				if (c0 != 0xff)
					gl.glCombinerStageParameterfvNV(GL_COMBINER0_NV + stage, GL_CONSTANT_COLOR0_NV, config.mpConstants[c0]);
				if (c1 != 0xff)
					gl.glCombinerStageParameterfvNV(GL_COMBINER0_NV + stage, GL_CONSTANT_COLOR1_NV, config.mpConstants[c1]);
				VDASSERT(!gl.glGetError());
			}

			// load combiner halves
			for(int half=0; half<2; ++half) {
				GLenum portionToken = half ? GL_ALPHA : GL_RGB;

				// decode output
				GLenum dst0 = kRegisterTable[src[0] >> 4];
				GLenum dst1 = kRegisterTable[src[1] & 15];
				GLenum dst2 = kRegisterTable[src[1] >> 4];
				GLenum dstscale = kScaleTable[src[0] & 3];
				GLenum dstbias = src[0] & 4 ? GL_BIAS_BY_NEGATIVE_ONE_HALF_NV : GL_NONE;
				GLenum dstDotAB = src[2] & 1 ? GL_TRUE : GL_FALSE;
				GLenum dstDotCD = src[2] & 2 ? GL_TRUE : GL_FALSE;
				GLenum dstMux = src[2] & 4 ? GL_TRUE : GL_FALSE;
				src += 3;

				gl.glCombinerOutputNV(stageToken, portionToken, dst0, dst1, dst2, dstscale, dstbias, dstDotAB, dstDotCD, dstMux);
				VDASSERT(!gl.glGetError());

				// decode sources
				for(int i=0; i<4; ++i) {
					uint8 c = *src++;
					GLenum srcreg = kRegisterTable[c & 15];
					GLenum srcmap = kSourceMappingTable[(c >> 4) & 7];
					GLenum srcportion = c & 0x80 ? GL_ALPHA : half ? GL_BLUE : GL_RGB;

					gl.glCombinerInputNV(stageToken, portionToken, GL_VARIABLE_A_NV + i, srcreg, srcmap, srcportion);
					VDASSERT(!gl.glGetError());
				}
			}
		}

		gl.glCombinerParameteriNV(GL_NUM_GENERAL_COMBINERS_NV, config.mGeneralCombinerCount);

		// load final combiner
		if (nvrc2) {
			uint8 c0 = src[0];
			uint8 c1 = src[1];
			src += 2;

			if (c0 != 0xff)
				gl.glCombinerParameterfvNV(GL_CONSTANT_COLOR0_NV, config.mpConstants[c0]);
			if (c1 != 0xff)
				gl.glCombinerParameterfvNV(GL_CONSTANT_COLOR1_NV, config.mpConstants[c1]);
			VDASSERT(!gl.glGetError());
		}

		for(int i=0; i<7; ++i) {
			uint8 c = *src++;
			GLenum srcreg = kRegisterTable[c & 15];
			GLenum srcmap = kSourceMappingTable[(c >> 4) & 7];
			GLenum srcportion = c & 0x80 ? GL_ALPHA : i == 6 ? GL_BLUE : GL_RGB;

			gl.glFinalCombinerInputNV(GL_VARIABLE_A_NV + i, srcreg, srcmap, srcportion);
			VDASSERT(!gl.glGetError());
		}
	}

	void CreateATIFragmentShader(VDOpenGLBinding& gl, const VDOpenGLATIFragmentShaderConfig& config) {
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
			kTexSwizzleMask		= 0x30,
			kTexModeTexcrd		= 0x40,
			kTexModeTexld		= 0x80,
			kTexModeMask		= 0xC0
		};

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
			kChanSrcMod2X		= 0x01,
			kChanSrcModComp		= 0x02,
			kChanSrcModNegate	= 0x04,
			kChanSrcModBias		= 0x08,
			kChanSrcSwizzleRed		= 0x10,
			kChanSrcSwizzleGreen	= 0x20,
			kChanSrcSwizzleBlue		= 0x30,
			kChanSrcSwizzleAlpha	= 0x40,
			kChanDstMaskRed		= 0x10,
			kChanDstMaskGreen	= 0x20,
			kChanDstMaskBlue	= 0x40,
			kChanDstMaskAlpha	= 0x80
		};

		VDASSERT(!gl.glGetError());
		gl.glBeginFragmentShaderATI();

		// load constants
		for(uint8 i=0; i<config.mConstantCount; ++i)
			gl.glSetFragmentShaderConstantATI(GL_CON_0_ATI + i, config.mpConstants[i]);
		VDASSERT(!gl.glGetError());

		// create passes
		const uint8 *src = config.mpByteCode;
		while(uint8 opCount = *src++) {
			// set up texture interpolators
			for(int i=0; i<6; ++i) {
				uint8 texOp = *src++;

				if (!texOp)
					continue;

				static const GLenum kTexSwizzleMode[4]={
					GL_SWIZZLE_STR_ATI,
					GL_SWIZZLE_STQ_ATI,
					GL_SWIZZLE_STR_DR_ATI,
					GL_SWIZZLE_STQ_DQ_ATI
				};

				GLenum dst = GL_REG_0_ATI + i;
				GLenum src = (texOp & 8 ? GL_REG_0_ATI : GL_TEXTURE0_ARB) + (texOp & 7);
				GLenum swizzle = kTexSwizzleMode[(texOp >> 4) & 3];

				switch(texOp & kTexModeMask) {
					case kTexModeTexcrd:
						gl.glPassTexCoordATI(dst, src, swizzle);
						break;
					case kTexModeTexld:
						gl.glSampleMapATI(dst, src, swizzle);
						break;
				}
			}
			VDASSERT(!gl.glGetError());

			// set up alu ops
			while(opCount--) {
				static const GLenum kOpTable[][2]={
					{ GL_ADD_ATI, 2 },
					{ GL_SUB_ATI, 2 },
					{ GL_MUL_ATI, 2 },
					{ GL_MAD_ATI, 3 },
					{ GL_LERP_ATI, 3 },
					{ GL_MOV_ATI, 1 },
					{ GL_CND_ATI, 3 },
					{ GL_CND0_ATI, 3 },
					{ GL_DOT2_ADD_ATI, 3 },
					{ GL_DOT3_ATI, 2 },
					{ GL_DOT4_ATI, 2 },
				};

				static const GLuint kDstModTable[16]={
					GL_NONE,
					GL_2X_BIT_ATI,
					GL_4X_BIT_ATI,
					GL_8X_BIT_ATI,
					GL_HALF_BIT_ATI,
					GL_QUARTER_BIT_ATI,
					GL_EIGHTH_BIT_ATI,
					GL_NONE,
					GL_SATURATE_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_2X_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_4X_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_8X_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_HALF_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_QUARTER_BIT_ATI,
					GL_SATURATE_BIT_ATI | GL_EIGHTH_BIT_ATI,
					GL_SATURATE_BIT_ATI,
				};

				static const GLenum kRegTable[]={
					GL_REG_0_ATI,
					GL_REG_1_ATI,
					GL_REG_2_ATI,
					GL_REG_3_ATI,
					GL_REG_4_ATI,
					GL_REG_5_ATI,
					GL_CON_0_ATI,
					GL_CON_1_ATI,
					GL_CON_2_ATI,
					GL_CON_3_ATI,
					GL_CON_4_ATI,
					GL_CON_5_ATI,
					GL_CON_6_ATI,
					GL_CON_7_ATI,
					GL_ZERO,
					GL_ONE,
					GL_PRIMARY_COLOR_ARB,
					GL_SECONDARY_INTERPOLATOR_ATI
				};

				static const GLenum kRepTable[]={
					GL_NONE,
					GL_RED,
					GL_GREEN,
					GL_BLUE,
					GL_ALPHA
				};

				const uint8 opcode = *src++;
				const uint8 dstarg = *src++;

				GLenum op = kOpTable[opcode & 15][0];
				GLenum dstMod = kDstModTable[opcode >> 4];
				GLenum dstReg = kRegTable[dstarg & 15];
				GLuint rgbMask = (dstarg >> 4) & 7;
				bool alphaMask = (dstarg & 0x80) != 0;

				int srcCount = kOpTable[opcode & 15][1];
				GLenum srcReg[3];
				GLenum srcRep[3];
				GLuint srcMod[3];
				for(int i=0; i<srcCount; ++i) {
					const uint8 mod = src[1];

					srcReg[i] = kRegTable[src[0]];
					srcRep[i] = kRepTable[mod >> 4];
					srcMod[i] = mod & 15;
					src += 2;
				}

				switch(srcCount) {
					case 1:
						if (rgbMask)
							gl.glColorFragmentOp1ATI(op, dstReg, rgbMask, dstMod, srcReg[0], srcRep[0], srcMod[0]);
						if (alphaMask)
							gl.glAlphaFragmentOp1ATI(op, dstReg, dstMod, srcReg[0], srcRep[0], srcMod[0]);
						break;
					case 2:
						if (rgbMask)
							gl.glColorFragmentOp2ATI(op, dstReg, rgbMask, dstMod, srcReg[0], srcRep[0], srcMod[0], srcReg[1], srcRep[1], srcMod[1]);
						if (alphaMask)
							gl.glAlphaFragmentOp2ATI(op, dstReg, dstMod, srcReg[0], srcRep[0], srcMod[0], srcReg[1], srcRep[1], srcMod[1]);
						break;
					case 3:
						if (rgbMask)
							gl.glColorFragmentOp3ATI(op, dstReg, rgbMask, dstMod, srcReg[0], srcRep[0], srcMod[0], srcReg[1], srcRep[1], srcMod[1], srcReg[2], srcRep[2], srcMod[2]);
						if (alphaMask)
							gl.glAlphaFragmentOp3ATI(op, dstReg, dstMod, srcReg[0], srcRep[0], srcMod[0], srcReg[1], srcRep[1], srcMod[1], srcReg[2], srcRep[2], srcMod[2]);
						break;
				}
				VDASSERT(!gl.glGetError());

			}
		}

		gl.glEndFragmentShaderATI();
		VDASSERT(!gl.glGetError());
	}
}

GLenum VDOpenGLBinding::InitTechniques(const VDOpenGLTechnique *techniques, int techniqueCount) {
	GLuint listBase = glGenLists(techniqueCount);

	for(int i=0; i<techniqueCount; ++i) {
		const VDOpenGLTechnique& tech = techniques[i];
		GLenum shader;

		switch(tech.mFragmentShaderMode) {
		case kVDOpenGLFragmentShaderModeNVRC2:
			if (!NV_register_combiners2)
				continue;
			// fall through
		case kVDOpenGLFragmentShaderModeNVRC:
			if (!NV_register_combiners)
				continue;
			break;
		case kVDOpenGLFragmentShaderModeATIFS:
			if (!ATI_fragment_shader)
				continue;
			break;
		}

		if (tech.mFragmentShaderMode == kVDOpenGLFragmentShaderModeATIFS) {
			shader = glGenFragmentShadersATI(1);
			glBindFragmentShaderATI(shader);
			CreateATIFragmentShader(*this, *(const VDOpenGLATIFragmentShaderConfig *)tech.mpFragmentShader);
			glBindFragmentShaderATI(0);
		}

		glNewList(listBase + i, GL_COMPILE);
		switch(tech.mFragmentShaderMode) {
		case kVDOpenGLFragmentShaderModeNVRC:
			glEnable(GL_REGISTER_COMBINERS_NV);
			glDisable(GL_PER_STAGE_CONSTANTS_NV);
			if (ATI_fragment_shader)
				glDisable(GL_FRAGMENT_SHADER_ATI);
			CreateNVRegisterCombinerSetup(*this, *(const VDOpenGLNVRegisterCombinerConfig *)tech.mpFragmentShader, false);
			break;
		case kVDOpenGLFragmentShaderModeNVRC2:
			glEnable(GL_REGISTER_COMBINERS_NV);
			glEnable(GL_PER_STAGE_CONSTANTS_NV);
			if (ATI_fragment_shader)
				glDisable(GL_FRAGMENT_SHADER_ATI);
			CreateNVRegisterCombinerSetup(*this, *(const VDOpenGLNVRegisterCombinerConfig *)tech.mpFragmentShader, true);
			break;
		case kVDOpenGLFragmentShaderModeATIFS:
			if (NV_register_combiners)
				glDisable(GL_REGISTER_COMBINERS_NV);
			glEnable(GL_FRAGMENT_SHADER_ATI);
			glBindFragmentShaderATI(shader);
			break;
		}
		glEndList();
	}

	return listBase;
}

void VDOpenGLBinding::DisableFragmentShaders() {
	if (NV_register_combiners)
		glDisable(GL_REGISTER_COMBINERS_NV);
	if (ATI_fragment_shader)
		glDisable(GL_FRAGMENT_SHADER_ATI);
}
