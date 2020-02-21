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

#ifndef f_VD2_RIZA_OPENGL_H
#define f_VD2_RIZA_OPENGL_H

#include <windows.h>
#include <gl/gl.h>

#if defined(VD_COMPILER_GCC_MINGW)
#include <gl/glext.h>
#endif

#ifndef f_VD2_SYSTEM_VDTYPES_H
	#include <vd2/system/vdtypes.h>
#endif

struct VDAPITableWGL {
	HGLRC 	(APIENTRY *wglCreateContext)(HDC hdc);
	BOOL	(APIENTRY *wglDeleteContext)(HGLRC hglrc);
	BOOL	(APIENTRY *wglMakeCurrent)(HDC  hdc, HGLRC hglrc);
	PROC	(APIENTRY *wglGetProcAddress)(const char *lpszProc);
	BOOL	(APIENTRY *wglSwapBuffers)(HDC  hdc);
	BOOL	(APIENTRY *wglUseFontBitmapsA)(HDC hdc, DWORD first, DWORD count, DWORD listBase);
};

struct VDAPITableOpenGL {
	// OpenGL 1.1
	void	(APIENTRY *glAlphaFunc)(GLenum func, GLclampf ref);
	void	(APIENTRY *glBegin)(GLenum mode);
	void	(APIENTRY *glBindTexture)(GLenum target, GLuint texture);
	void	(APIENTRY *glBlendFunc)(GLenum sfactor, GLenum dfactor);
	void	(APIENTRY *glCallList)(GLuint list);
	void	(APIENTRY *glClear)(GLbitfield mask);
	void	(APIENTRY *glClearColor)(GLclampf red, GLclampf green, GLclampf blue, GLclampf alpha);
	void	(APIENTRY *glColor4d)(GLdouble red, GLdouble green, GLdouble blue, GLdouble alpha);
	void	(APIENTRY *glColor4f)(GLfloat red, GLfloat green, GLfloat blue, GLfloat alpha);
	void	(APIENTRY *glColor4ub)(GLubyte red, GLubyte green, GLubyte blue, GLubyte alpha);
	void	(APIENTRY *glColorMask)(GLboolean red, GLboolean green, GLboolean blue, GLboolean alpha);
	void	(APIENTRY *glCopyTexSubImage2D)(GLenum target, GLint level, GLint xoffset, GLint yoffset, GLint x, GLint y, GLsizei width, GLsizei height);
	void	(APIENTRY *glDeleteLists)(GLuint list, GLsizei range);
	void	(APIENTRY *glDeleteTextures)(GLsizei n, const GLuint *textures);
	void	(APIENTRY *glDepthFunc)(GLenum func);
	void	(APIENTRY *glDepthMask)(GLboolean mask);
	void	(APIENTRY *glDisable)(GLenum cap);
	void	(APIENTRY *glDrawBuffer)(GLenum mode);
	void	(APIENTRY *glEnable)(GLenum cap);
	void	(APIENTRY *glEnd)();
	void	(APIENTRY *glEndList)();
	void	(APIENTRY *glFeedbackBuffer)(GLsizei n, GLenum type, GLfloat *buffer);
	void	(APIENTRY *glFinish)();
	void	(APIENTRY *glFlush)();
	void	(APIENTRY *glFrontFace)(GLenum mode);
	GLenum	(APIENTRY *glGetError)();
	void	(APIENTRY *glGetFloatv)(GLenum pname, GLfloat *params);
	void	(APIENTRY *glGetIntegerv)(GLenum pname, GLint *params);
	void	(APIENTRY *glGetTexLevelParameteriv)(GLenum target, GLint level, GLenum pname, GLint *params);
	GLuint	(APIENTRY *glGenLists)(GLsizei range);
	const GLubyte *(APIENTRY *glGetString)(GLenum);
	void	(APIENTRY *glGenTextures)(GLsizei n, GLuint *textures);
	void	(APIENTRY *glLoadIdentity)();
	void	(APIENTRY *glLoadMatrixd)(const GLdouble *m);
	void	(APIENTRY *glMatrixMode)(GLenum target);
	void	(APIENTRY *glNewList)(GLuint list, GLenum mode);
	void	(APIENTRY *glOrtho)(GLdouble left, GLdouble right, GLdouble bottom, GLdouble top, GLdouble zNear, GLdouble zFar);
	void	(APIENTRY *glPixelStorei)(GLenum pname, GLint param);
	void	(APIENTRY *glPopAttrib)();
	void	(APIENTRY *glPushAttrib)(GLbitfield mask);
	void	(APIENTRY *glReadBuffer)(GLenum mode);
	void	(APIENTRY *glReadPixels)(GLint x, GLint y, GLsizei width, GLsizei height, GLenum format, GLenum type, GLvoid *pixels);
	GLint	(APIENTRY *glRenderMode)(GLenum);
	void	(APIENTRY *glScissor)(GLint x, GLint y, GLsizei width, GLsizei height);
	void	(APIENTRY *glTexCoord2d)(GLdouble s, GLdouble t);
	void	(APIENTRY *glTexCoord2f)(GLfloat s, GLfloat t);
	void	(APIENTRY *glTexCoord2fv)(const GLfloat *v);
	void	(APIENTRY *glTexEnvf)(GLenum target, GLenum pname, GLfloat param);
	void	(APIENTRY *glTexEnvi)(GLenum target, GLenum pname, GLint param);
	void	(APIENTRY *glTexImage1D)(GLenum target, GLint level, GLint internalformat, GLsizei width, GLint border, GLenum format, GLenum type, const GLvoid *pixels);
	void	(APIENTRY *glTexImage2D)(GLenum target, GLint level, GLint internalformat, GLsizei width, GLsizei height, GLint border, GLenum format, GLenum type, const GLvoid *pixels);
	void	(APIENTRY *glTexParameterfv)(GLenum target, GLenum pname, const GLfloat *params);
	void	(APIENTRY *glTexParameteri)(GLenum target, GLenum pname, GLint param);
	void	(APIENTRY *glTexSubImage1D)(GLenum target, GLint level, GLint xoffset, GLsizei width, GLenum format, GLenum type, const GLvoid *pixels);
	void	(APIENTRY *glTexSubImage2D)(GLenum target, GLint level, GLint x, GLint y, GLsizei width, GLsizei height, GLenum format, GLenum type, const GLvoid *pixels);
	void	(APIENTRY *glTranslatef)(GLfloat x, GLfloat y, GLfloat z);
	void	(APIENTRY *glVertex2d)(GLdouble x, GLdouble y);
	void	(APIENTRY *glVertex2f)(GLfloat x, GLfloat y);
	void	(APIENTRY *glVertex2i)(GLint x, GLint y);
	void	(APIENTRY *glVertex3fv)(const GLfloat *v);
	void	(APIENTRY *glViewport)(GLint x, GLint y, GLsizei width, GLsizei height);
};

#define		GL_INVALID_FRAMEBUFFER_OPERATION_EXT	0x0506

#define		WGL_FLOAT_COMPONENTS_NV				0x20B0
#define		WGL_BIND_TO_TEXTURE_RECTANGLE_FLOAT_R_NV		0x20B1
#define		WGL_BIND_TO_TEXTURE_RECTANGLE_FLOAT_RG_NV		0x20B2
#define		WGL_BIND_TO_TEXTURE_RECTANGLE_FLOAT_RGB_NV		0x20B3
#define		WGL_BIND_TO_TEXTURE_RECTANGLE_FLOAT_RGBA_NV		0x20B4
#define		WGL_TEXTURE_FLOAT_R_NV				0x20B5
#define		WGL_TEXTURE_FLOAT_RG_NV				0x20B6
#define		WGL_TEXTURE_FLOAT_RGB_NV			0x20B7
#define		WGL_TEXTURE_FLOAT_RGBA_NV			0x20B8

#define		GL_FUNC_ADD_EXT						0x8006
#define		GL_MIN_EXT							0x8007
#define		GL_MAX_EXT							0x8008
#define		GL_BLEND_EQUATION_EXT				0x8009
#define		GL_FUNC_SUBTRACT_EXT				0x800A
#define		GL_FUNC_REVERSE_SUBTRACT_EXT		0x800B

#define		GL_PACK_SKIP_IMAGES_EXT				0x806B
#define		GL_PACK_IMAGE_HEIGHT_EXT			0x806C
#define		GL_UNPACK_SKIP_IMAGES_EXT			0x806D
#define		GL_UNPACK_IMAGE_HEIGHT_EXT			0x806E
#define		GL_TEXTURE_3D_EXT					0x806F
#define		GL_PROXY_TEXTURE_3D_EXT				0x8070
#define		GL_TEXTURE_DEPTH_EXT				0x8071
#define		GL_TEXTURE_WRAP_R_EXT				0x8072
#define		GL_MAX_3D_TEXTURE_SIZE_EXT			0x8073

#define		GL_CLAMP_TO_EDGE_EXT				0x812F

#define		GL_TEXTURE_MIN_LOD					0x813A
#define		GL_TEXTURE_MIN_LOD_SGIS				0x813A
#define		GL_TEXTURE_MAX_LOD					0x813B
#define		GL_TEXTURE_MAX_LOD_SGIS				0x813B
#define		GL_TEXTURE_BASE_LEVEL				0x813C
#define		GL_TEXTURE_BASE_LEVEL_SGIS			0x813C
#define		GL_TEXTURE_MAX_LEVEL				0x813D
#define		GL_TEXTURE_MAX_LEVEL_SGIS			0x813D

#define		GL_OCCLUSION_TEST_HP				0x8165
#define		GL_OCCLUSION_TEST_RESULT_HP			0x8166

#define		GL_DEPTH_COMPONENT16_ARB			0x81A5
#define		GL_DEPTH_COMPONENT24_ARB			0x81A6
#define		GL_DEPTH_COMPONENT32_ARB			0x81A7

#ifndef		GL_UNSIGNED_SHORT_5_6_5
#define		GL_UNSIGNED_SHORT_5_6_5				0x8363
#endif
#ifndef		GL_UNSIGNED_SHORT_5_6_5_REV
#define		GL_UNSIGNED_SHORT_5_6_5_REV			0x8364
#endif
#ifndef		GL_UNSIGNED_SHORT_1_5_5_5_REV
#define		GL_UNSIGNED_SHORT_1_5_5_5_REV		0x8366
#endif

#define		GL_COMPRESSED_RGB_S3TC_DXT1_EXT		0x83F0
#define		GL_COMPRESSED_RGBA_S3TC_DXT1_EXT	0x83F1
#define		GL_COMPRESSED_RGBA_S3TC_DXT3_EXT	0x83F2
#define		GL_COMPRESSED_RGBA_S3TC_DXT5_EXT	0x83F3

#define		GL_COLOR_SUM_ARB					0x8458

#define		GL_TEXTURE0_ARB						0x84C0
#define		GL_TEXTURE1_ARB						0x84C1
#define		GL_TEXTURE2_ARB						0x84C2
#define		GL_TEXTURE3_ARB						0x84C3
#define		GL_TEXTURE4_ARB						0x84C4
#define		GL_TEXTURE5_ARB						0x84C5
#define		GL_TEXTURE6_ARB						0x84C6
#define		GL_TEXTURE7_ARB						0x84C7

#define		MAX_RENDERBUFFER_SIZE_EXT			0x84E8

#define		GL_TEXTURE_RECTANGLE_NV				0x84F5
#define		GL_TEXTURE_RECTANGLE_ARB			0x84F5
#define		GL_TEXTURE_BINDING_RECTANGLE_NV		0x84F6
#define		GL_TEXTURE_BINDING_RECTANGLE_ARB	0x84F6
#define		GL_PROXY_TEXTURE_RECTANGLE_NV		0x84F7
#define		GL_PROXY_TEXTURE_RECTANGLE_ARB		0x84F7
#define		GL_MAX_RECTANGLE_TEXTURE_SIZE_NV	0x84F8
#define		GL_MAX_RECTANGLE_TEXTURE_SIZE_ARB	0x84F8
#define		GL_MAX_TEXTURE_LOD_BIAS_EXT			0x84FD

#define		GL_TEXTURE_FILTER_CONTROL_EXT		0x8500
#define		GL_TEXTURE_LOD_BIAS_EXT				0x8501

#define		GL_NORMAL_MAP_ARB					0x8511
#define		GL_REFLECTION_MAP_ARB				0x8512
#define		GL_TEXTURE_CUBE_MAP_ARB				0x8513
#define		GL_TEXTURE_BINDING_CUBE_MAP_ARB		0x8514
#define		GL_TEXTURE_CUBE_MAP_POSITIVE_X_ARB	0x8515
#define		GL_TEXTURE_CUBE_MAP_NEGATIVE_X_ARB	0x8516
#define		GL_TEXTURE_CUBE_MAP_POSITIVE_Y_ARB	0x8517
#define		GL_TEXTURE_CUBE_MAP_NEGATIVE_Y_ARB	0x8518
#define		GL_TEXTURE_CUBE_MAP_POSITIVE_Z_ARB	0x8519
#define		GL_TEXTURE_CUBE_MAP_NEGATIVE_Z_ARB	0x851A
#define		GL_PROXY_TEXTURE_CUBE_MAP_ARB		0x851B
#define		GL_MAX_CUBE_MAP_TEXTURE_SIZE_ARB	0x851C

#define		GL_REGISTER_COMBINERS_NV			0x8522
#define		GL_VARIABLE_A_NV					0x8523
#define		GL_VARIABLE_B_NV					0x8524
#define		GL_VARIABLE_C_NV					0x8525
#define		GL_VARIABLE_D_NV					0x8526
#define		GL_VARIABLE_E_NV					0x8527
#define		GL_VARIABLE_F_NV					0x8528
#define		GL_VARIABLE_G_NV					0x8529
#define		GL_CONSTANT_COLOR0_NV				0x852A
#define		GL_CONSTANT_COLOR1_NV				0x852B
#define		GL_PRIMARY_COLOR_NV					0x852C
#define		GL_SECONDARY_COLOR_NV				0x852D
#define		GL_SPARE0_NV						0x852E
#define		GL_SPARE1_NV						0x852F
#define		GL_DISCARD_NV						0x8530
#define		GL_E_TIMES_F_NV						0x8531
#define		GL_SPARE0_PLUS_SECONDARY_COLOR_NV	0x8532
#define		GL_PER_STAGE_CONSTANTS_NV			0x8535
#define		GL_UNSIGNED_IDENTITY_NV				0x8536
#define		GL_UNSIGNED_INVERT_NV				0x8537
#define		GL_EXPAND_NORMAL_NV					0x8538
#define		GL_EXPAND_NEGATE_NV					0x8539
#define		GL_HALF_BIAS_NORMAL_NV				0x853A
#define		GL_HALF_BIAS_NEGATE_NV				0x853B
#define		GL_SIGNED_IDENTITY_NV				0x853C
#define		GL_SIGNED_NEGATE_NV					0x853D
#define		GL_SCALE_BY_TWO_NV					0x853E
#define		GL_SCALE_BY_FOUR_NV					0x853F
#define		GL_SCALE_BY_ONE_HALF_NV				0x8540
#define		GL_BIAS_BY_NEGATIVE_ONE_HALF_NV		0x8541
#define		GL_COMBINER_INPUT_NV				0x8542
#define		GL_COMBINER_MAPPING_NV				0x8543
#define		GL_COMBINER_COMPONENT_USAGE_NV		0x8544
#define		GL_COMBINER_AB_DOT_PRODUCT_NV		0x8545
#define		GL_COMBINER_CD_DOT_PRODUCT_NV		0x8546
#define		GL_COMBINER_MUX_SUM_NV				0x8547
#define		GL_COMBINER_SCALE_NV				0x8548
#define		GL_COMBINER_BIAS_NV					0x8549
#define		GL_COMBINER_AB_OUTPUT_NV			0x854A
#define		GL_COMBINER_CD_OUTPUT_NV			0x854B
#define		GL_COMBINER_SUM_OUTPUT_NV			0x854C
#define		GL_MAX_GENERAL_COMBINERS_NV			0x854D
#define		GL_NUM_GENERAL_COMBINERS_NV			0x854E
#define		GL_COLOR_SUM_CLAMP_NV				0x854F
#define		GL_COMBINER0_NV						0x8550
#define		GL_COMBINER1_NV						0x8551
#define		GL_COMBINER2_NV						0x8552
#define		GL_COMBINER3_NV						0x8553
#define		GL_COMBINER4_NV						0x8554
#define		GL_COMBINER5_NV						0x8555
#define		GL_COMBINER6_NV						0x8556
#define		GL_COMBINER7_NV						0x8557

#define		GL_COMBINE_EXT						0x8570
#define		GL_COMBINE_RGB_EXT					0x8571
#define		GL_COMBINE_ALPHA_EXT				0x8572
#define		GL_RGB_SCALE_EXT					0x8573
#define		GL_ADD_SIGNED_EXT					0x8574
#define		GL_INTERPOLATE_EXT					0x8575
#define		GL_CONSTANT_EXT						0x8576
#define		GL_PRIMARY_COLOR_EXT				0x8577
#define		GL_PRIMARY_COLOR_ARB				0x8577
#define		GL_PREVIOUS_EXT						0x8578
#define		GL_SOURCE0_RGB_EXT					0x8580
#define		GL_SOURCE1_RGB_EXT					0x8581
#define		GL_SOURCE2_RGB_EXT					0x8582
#define		GL_SOURCE0_ALPHA_EXT				0x8588
#define		GL_SOURCE1_ALPHA_EXT				0x8589
#define		GL_SOURCE2_ALPHA_EXT				0x858A
#define		GL_OPERAND0_RGB_EXT					0x8590
#define		GL_OPERAND1_RGB_EXT					0x8591
#define		GL_OPERAND2_RGB_EXT					0x8592
#define		GL_OPERAND0_ALPHA_EXT				0x8598
#define		GL_OPERAND1_ALPHA_EXT				0x8599
#define		GL_OPERAND2_ALPHA_EXT				0x859A

#define		GL_VERTEX_PROGRAM_ARB				0x8620

#define		GL_VERTEX_ATTRIB_ARRAY_ENABLED_ARB	0x8622
#define		GL_VERTEX_ATTRIB_ARRAY_SIZE_ARB		0x8623
#define		GL_VERTEX_ATTRIB_ARRAY_STRIDE_ARB	0x8624
#define		GL_VERTEX_ATTRIB_ARRAY_TYPE_ARB		0x8625
#define		GL_CURRENT_VERTEX_ATTRIB_ARB		0x8626

#define		GL_PROGRAM_LENGTH_ARB				0x8627
#define		GL_PROGRAM_STRING_ARB				0x8628
#define		GL_MAX_PROGRAM_MATRIX_STACK_DEPTH_ARB	0x862E
#define		GL_MAX_PROGRAM_MATRICES_ARB			0x862F

#define		GL_CURRENT_MATRIX_STACK_DEPTH_ARB	0x8640
#define		GL_CURRENT_MATRIX_ARB				0x8641
#define		GL_VERTEX_PROGRAM_POINT_SIZE_ARB	0x8642
#define		GL_VERTEX_PROGRAM_TWO_SIDE_ARB		0x8643
#define		GL_VERTEX_ATTRIB_ARRAY_POINTER_ARB	0x8645
#define		GL_PROGRAM_ERROR_POSITION_ARB		0x864B

#define		GL_PROGRAM_BINDING_ARB				0x8677

#define		GL_UNSIGNED_INT_S8_S8_8_8_NV		0x86DA
#define		GL_UNSIGNED_INT_8_8_S8_S8_REV_NV	0x86DB
#define		GL_DSDT_MAG_INTENSITY_NV			0x86DC

#define		GL_DOT_PRODUCT_TEXTURE_3D_NV		0x86EF

#define		GL_HILO_NV							0x86F4
#define		GL_DSDT_NV							0x86F5
#define		GL_DSDT_MAG_NV						0x86F6
#define		GL_DSDT_MAG_VIB_NV					0x86F7
#define		GL_HILO16_NV						0x86F8
#define		GL_SIGNED_HILO_NV					0x86F9
#define		GL_SIGNED_HILO16_NV					0x86FA
#define		GL_SIGNED_RGBA_NV					0x86FB
#define		GL_SIGNED_RGBA8_NV					0x86FC
#define		GL_SIGNED_RGB_NV					0x86FE
#define		GL_SIGNED_RGB8_NV					0x86FF
#define		GL_SIGNED_LUMINANCE_NV				0x8701
#define		GL_SIGNED_LUMINANCE8_NV				0x8702
#define		GL_SIGNED_LUMINANCE_ALPHA_NV		0x8703
#define		GL_SIGNED_LUMINANCE8_ALPHA8_NV		0x8704
#define		GL_SIGNED_ALPHA_NV					0x8705
#define		GL_SIGNED_ALPHA8_NV					0x8706
#define		GL_SIGNED_INTENSITY_NV				0x8707
#define		GL_SIGNED_INTENSITY8_NV				0x8708
#define		GL_DSDT8_NV							0x8709
#define		GL_DSDT8_MAG8_NV					0x870A
#define		GL_SIGNED_RGB_UNSIGNED_ALPHA_NV		0x870C
#define		GL_SIGNED_RGB8_UNSIGNED_ALPHA8_NV	0x870D
#define		GL_DSDT8_MAG8_INTENSITY8_NV			0x870B

#define		GL_FRAGMENT_PROGRAM_ARB				0x8804
#define		GL_PROGRAM_ALU_INSTRUCTIONS_ARB		0x8805
#define		GL_PROGRAM_TEX_INSTRUCTIONS_ARB		0x8806
#define		GL_PROGRAM_TEX_INDIRECTIONS_ARB		0x8807
#define		GL_PROGRAM_NATIVE_ALU_INSTRUCTIONS_ARB	0x8808
#define		GL_PROGRAM_NATIVE_TEX_INSTRUCTIONS_ARB	0x8809
#define		GL_PROGRAM_NATIVE_TEX_INDIRECTIONS_ARB	0x880A
#define		GL_MAX_PROGRAM_ALU_INSTRUCTIONS_ARB	0x880B
#define		GL_MAX_PROGRAM_TEX_INSTRUCTIONS_ARB	0x880C
#define		GL_MAX_PROGRAM_TEX_INDIRECTIONS_ARB	0x880D
#define		GL_MAX_PROGRAM_NATIVE_ALU_INSTRUCTIONS_ARB	0x880E
#define		GL_MAX_PROGRAM_NATIVE_TEX_INSTRUCTIONS_ARB	0x880F
#define		GL_MAX_PROGRAM_NATIVE_TEX_INDIRECTIONS_ARB	0x8810

#define		GL_RGBA_FLOAT32_ATI					0x8814
#define		GL_RGB_FLOAT32_ATI					0x8815
#define		GL_ALPHA_FLOAT32_ATI				0x8816
#define		GL_INTENSITY_FLOAT32_ATI			0x8817
#define		GL_LUMINANCE_FLOAT32_ATI			0x8818
#define		GL_LUMINANCE_ALPHA_FLOAT32_ATI		0x8819
#define		GL_RGBA_FLOAT16_ATI					0x881A
#define		GL_RGB_FLOAT16_ATI					0x881B
#define		GL_ALPHA_FLOAT16_ATI				0x881C
#define		GL_INTENSITY_FLOAT16_ATI			0x881D
#define		GL_LUMINANCE_FLOAT16_ATI			0x881E
#define		GL_LUMINANCE_ALPHA_FLOAT16_ATI		0x881F

#define		GL_TEXTURE_DEPTH_SIZE_ARB			0x884A
#define		GL_DEPTH_TEXTURE_MODE_ARB			0x884B

#define		GL_PIXEL_COUNTER_BITS_NV			0x8864
#define		GL_CURRENT_OCCLUSION_QUERY_ID_NV	0x8865
#define		GL_PIXEL_COUNT_NV					0x8866
#define		GL_PIXEL_COUNT_AVAILABLE_NV			0x8867
#define		GL_MAX_VERTEX_ATTRIBS_ARB			0x8869
#define		GL_VERTEX_ATTRIB_ARRAY_NORMALIZED_ARB	0x886A

#define		GL_PROGRAM_ERROR_STRING_ARB			0x8874
#define		GL_PROGRAM_FORMAT_ASCII_ARB			0x8875
#define		GL_PROGRAM_FORMAT_ARB				0x8876

#define		GL_FLOAT_R_NV						0x8880
#define		GL_FLOAT_RG_NV						0x8881
#define		GL_FLOAT_RGB_NV						0x8882
#define		GL_FLOAT_RGBA_NV					0x8883
#define		GL_FLOAT_R16_NV						0x8884
#define		GL_FLOAT_R32_NV						0x8885
#define		GL_FLOAT_RG16_NV					0x8886
#define		GL_FLOAT_RG32_NV					0x8887
#define		GL_FLOAT_RGB16_NV					0x8888
#define		GL_FLOAT_RGB32_NV					0x8889
#define		GL_FLOAT_RGBA16_NV					0x888A
#define		GL_FLOAT_RGBA32_NV					0x888B
#define		GL_TEXTURE_FLOAT_COMPONENTS_NV		0x888C
#define		GL_FLOAT_CLEAR_COLOR_VALUE_NV		0x888D
#define		GL_FLOAT_RGBA_MODE_NV				0x888E

#define		GL_PROGRAM_INSTRUCTIONS_ARB			0x88A0
#define		GL_MAX_PROGRAM_INSTRUCTIONS_ARB		0x88A1
#define		GL_PROGRAM_NATIVE_INSTRUCTIONS_ARB	0x88A2
#define		GL_MAX_PROGRAM_NATIVE_INSTRUCTIONS_ARB	0x88A3
#define		GL_PROGRAM_TEMPORARIES_ARB			0x88A4
#define		GL_MAX_PROGRAM_TEMPORARIES_ARB		0x88A5
#define		GL_PROGRAM_NATIVE_TEMPORARIES_ARB	0x88A6
#define		GL_MAX_PROGRAM_NATIVE_TEMPORARIES_ARB	0x88A7
#define		GL_PROGRAM_PARAMETERS_ARB			0x88A8
#define		GL_MAX_PROGRAM_PARAMETERS_ARB		0x88A9
#define		GL_PROGRAM_NATIVE_PARAMETERS_ARB	0x88AA
#define		GL_MAX_PROGRAM_NATIVE_PARAMETERS_ARB	0x88AB
#define		GL_PROGRAM_ATTRIBS_ARB				0x88AC
#define		GL_MAX_PROGRAM_ATTRIBS_ARB			0x88AD
#define		GL_PROGRAM_NATIVE_ATTRIBS_ARB		0x88AE
#define		GL_MAX_PROGRAM_NATIVE_ATTRIBS_ARB	0x88AF
#define		GL_PROGRAM_ADDRESS_REGISTERS_ARB	0x88B0
#define		GL_MAX_PROGRAM_ADDRESS_REGISTERS_ARB	0x88B1
#define		GL_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB	0x88B2
#define		GL_MAX_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB	0x88B3
#define		GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB	0x88B4
#define		GL_MAX_PROGRAM_ENV_PARAMETERS_ARB	0x88B5
#define		GL_PROGRAM_UNDER_NATIVE_LIMITS_ARB	0x88B6

#define		GL_PROGRAM_ADDRESS_REGISTERS_ARB			0x88B0
#define		GL_MAX_PROGRAM_ADDRESS_REGISTERS_ARB		0x88B1
#define		GL_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB		0x88B2
#define		GL_MAX_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB	0x88B3
#define		GL_TRANSPOSE_CURRENT_MATRIX_ARB		0x88B7
#define		GL_READ_ONLY_ARB					0x88B8
#define		GL_WRITE_ONLY_ARB					0x88B9
#define		GL_READ_WRITE_ARB					0x88BA

#define		GL_MATRIX0_ARB						0x88C0
#define		GL_MATRIX1_ARB						0x88C1
#define		GL_MATRIX2_ARB						0x88C2
#define		GL_MATRIX3_ARB						0x88C3
#define		GL_MATRIX4_ARB						0x88C4
#define		GL_MATRIX5_ARB						0x88C5
#define		GL_MATRIX6_ARB						0x88C6
#define		GL_MATRIX7_ARB						0x88C7
#define		GL_MATRIX8_ARB						0x88C8
#define		GL_MATRIX9_ARB						0x88C9
#define		GL_MATRIX10_ARB						0x88CA
#define		GL_MATRIX11_ARB						0x88CB
#define		GL_MATRIX12_ARB						0x88CC
#define		GL_MATRIX13_ARB						0x88CD
#define		GL_MATRIX14_ARB						0x88CE
#define		GL_MATRIX15_ARB						0x88CF
#define		GL_MATRIX16_ARB						0x88D0
#define		GL_MATRIX17_ARB						0x88D1
#define		GL_MATRIX18_ARB						0x88D2
#define		GL_MATRIX19_ARB						0x88D3
#define		GL_MATRIX20_ARB						0x88D4
#define		GL_MATRIX21_ARB						0x88D5
#define		GL_MATRIX22_ARB						0x88D6
#define		GL_MATRIX23_ARB						0x88D7
#define		GL_MATRIX24_ARB						0x88D8
#define		GL_MATRIX25_ARB						0x88D9
#define		GL_MATRIX26_ARB						0x88DA
#define		GL_MATRIX27_ARB						0x88DB
#define		GL_MATRIX28_ARB						0x88DC
#define		GL_MATRIX29_ARB						0x88DD
#define		GL_MATRIX30_ARB						0x88DE
#define		GL_MATRIX31_ARB						0x88DF

#define		GL_STREAM_DRAW_ARB					0x88E0
#define		GL_STREAM_READ_ARB					0x88E1
#define		GL_STREAM_COPY_ARB					0x88E2
#define		GL_STATIC_DRAW_ARB					0x88E4
#define		GL_STATIC_READ_ARB					0x88E5
#define		GL_STATIC_COPY_ARB					0x88E6
#define		GL_DYNAMIC_DRAW_ARB					0x88E8
#define		GL_DYNAMIC_READ_ARB					0x88E9
#define		GL_DYNAMIC_COPY_ARB					0x88EA

#define		GL_PIXEL_PACK_BUFFER_ARB			0x88EB
#define		GL_PIXEL_UNPACK_BUFFER_ARB			0x88EC
#define		GL_PIXEL_PACK_BUFFER_BINDING_ARB	0x88ED
#define		GL_PIXEL_UNPACK_BUFFER_BINDING_ARB	0x88EF

#define		GL_DEPTH24_STENCIL8_EXT				0x88F0
#define		GL_TEXTURE_STENCIL_SIZE_EXT			0x88F1

#define		GL_FRAGMENT_SHADER_ATI	0x8920
    
#define		GL_REG_0_ATI			0x8921
#define		GL_REG_1_ATI			0x8922
#define		GL_REG_2_ATI			0x8923
#define		GL_REG_3_ATI			0x8924
#define		GL_REG_4_ATI			0x8925
#define		GL_REG_5_ATI			0x8926

#define		GL_CON_0_ATI			0x8941
#define		GL_CON_1_ATI			0x8942
#define		GL_CON_2_ATI			0x8943
#define		GL_CON_3_ATI			0x8944
#define		GL_CON_4_ATI			0x8945
#define		GL_CON_5_ATI			0x8946
#define		GL_CON_6_ATI			0x8947
#define		GL_CON_7_ATI			0x8948

#define		GL_MOV_ATI				0x8961
#define		GL_ADD_ATI				0x8963
#define		GL_MUL_ATI				0x8964
#define		GL_SUB_ATI				0x8965
#define		GL_DOT3_ATI				0x8966
#define		GL_DOT4_ATI				0x8967
#define		GL_MAD_ATI				0x8968
#define		GL_LERP_ATI				0x8969
#define		GL_CND_ATI				0x896A
#define		GL_CND0_ATI				0x896B
#define		GL_DOT2_ADD_ATI			0x896C

#define		GL_SECONDARY_INTERPOLATOR_ATI	0x896D

#define		GL_SWIZZLE_STR_ATI					0x8976
#define		GL_SWIZZLE_STQ_ATI					0x8977
#define		GL_SWIZZLE_STR_DR_ATI				0x8978
#define		GL_SWIZZLE_STQ_DQ_ATI				0x8979

#define		GL_SAMPLER_2D_RECT_ARB				0x8B63
#define		GL_SAMPLER_2D_RECT_SHADOW_ARB		0x8B64

#define		GL_FRAMEBUFFER_BINDING_EXT			0x8CA6
#define		GL_RENDERBUFFER_BINDING_EXT			0x8CA7

#define		GL_FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE_EXT	0x8CD0
#define		GL_FRAMEBUFFER_ATTACHMENT_OBJECT_NAME_EXT	0x8CD1
#define		GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL_EXT	0x8CD2
#define		GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE_EXT	0x8CD3
#define		GL_FRAMEBUFFER_ATTACHMENT_TEXTURE_3D_ZOFFSET_EXT	0x8CD4
#define		GL_FRAMEBUFFER_COMPLETE_EXT			0x8CD5
#define		GL_FRAMEBUFFER_INCOMPLETE_ATTACHMENT_EXT	0x8CD6
#define		GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT_EXT	0x8CD7
#define		GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS_EXT	0x8CD9
#define		GL_FRAMEBUFFER_INCOMPLETE_FORMATS_EXT	0x8CDA
#define		GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER_EXT	0x8CDB
#define		GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER_EXT	0x8CDC
#define		GL_FRAMEBUFFER_UNSUPPORTED_EXT		0x8CDD
#define		MAX_COLOR_ATTACHMENTS_EXT			0x8CDF

#define		GL_COLOR_ATTACHMENT0_EXT			0x8CE0
#define		GL_COLOR_ATTACHMENT1_EXT			0x8CE1
#define		GL_COLOR_ATTACHMENT2_EXT			0x8CE2
#define		GL_COLOR_ATTACHMENT3_EXT			0x8CE3
#define		GL_COLOR_ATTACHMENT4_EXT			0x8CE4
#define		GL_COLOR_ATTACHMENT5_EXT			0x8CE5
#define		GL_COLOR_ATTACHMENT6_EXT			0x8CE6
#define		GL_COLOR_ATTACHMENT7_EXT			0x8CE7
#define		GL_COLOR_ATTACHMENT8_EXT			0x8CE8
#define		GL_COLOR_ATTACHMENT9_EXT			0x8CE9
#define		GL_COLOR_ATTACHMENT10_EXT			0x8CEA
#define		GL_COLOR_ATTACHMENT11_EXT			0x8CEB
#define		GL_COLOR_ATTACHMENT12_EXT			0x8CEC
#define		GL_COLOR_ATTACHMENT13_EXT			0x8CED
#define		GL_COLOR_ATTACHMENT14_EXT			0x8CEE
#define		GL_COLOR_ATTACHMENT15_EXT			0x8CEF

#define		GL_DEPTH_ATTACHMENT_EXT				0x8D00

#define		GL_STENCIL_ATTACHMENT_EXT			0x8D20

#define		GL_FRAMEBUFFER_EXT					0x8D40
#define		GL_RENDERBUFFER_EXT					0x8D41
#define		GL_STENCIL_INDEX1_EXT				0x8D46
#define		GL_STENCIL_INDEX4_EXT				0x8D47
#define		GL_STENCIL_INDEX8_EXT				0x8D48
#define		GL_STENCIL_INDEX16_EXT				0x8D49
#define		GL_RENDERBUFFER_WIDTH_EXT			0x8D42
#define		GL_RENDERBUFFER_HEIGHT_EXT			0x8D43
#define		GL_RENDERBUFFER_INTERNAL_FORMAT_EXT	0x8D44

#define		GL_RENDERBUFFER_RED_SIZE_EXT		0x8D50
#define		GL_RENDERBUFFER_GREEN_SIZE_EXT		0x8D51
#define		GL_RENDERBUFFER_BLUE_SIZE_EXT		0x8D52
#define		GL_RENDERBUFFER_ALPHA_SIZE_EXT		0x8D53
#define		GL_RENDERBUFFER_DEPTH_SIZE_EXT		0x8D54
#define		GL_RENDERBUFFER_STENCIL_SIZE_EXT	0x8D55

#define		GL_DEPTH_COMPONENT32F_NV			0x8DAB
#define		GL_DEPTH32F_STENCIL8_NV				0x8DAC
#define		GL_FLOAT_32_UNSIGNED_INT_24_8_REV_NV	0x8DAD
#define		GL_DEPTH_BUFFER_FLOAT_MODE_NV		0x8DAF

#define		GL_DEPTH_STENCIL_EXT				0x8F49
#define		GL_UNSIGNED_INT_24_8_EXT			0x8F4A

#define		GL_RED_BIT_ATI			0x00000001
#define		GL_GREEN_BIT_ATI		0x00000002
#define		GL_BLUE_BIT_ATI			0x00000004

#define		GL_2X_BIT_ATI			0x00000001
#define		GL_4X_BIT_ATI			0x00000002
#define		GL_8X_BIT_ATI			0x00000004
#define		GL_HALF_BIT_ATI			0x00000008
#define		GL_QUARTER_BIT_ATI		0x00000010
#define		GL_EIGHTH_BIT_ATI		0x00000020
#define		GL_SATURATE_BIT_ATI		0x00000040
    
#define		GL_COMP_BIT_ATI			0x00000002
#define		GL_NEGATE_BIT_ATI		0x00000004
#define		GL_BIAS_BIT_ATI			0x00000008

typedef size_t GLsizeiptrARB;
typedef ptrdiff_t GLintptrARB;

struct VDAPITableOpenGLEXT {
	// ARB_multitexture
	void	(APIENTRY *glActiveTextureARB)(GLenum texture);
	void	(APIENTRY *glMultiTexCoord2fARB)(GLenum texture, GLfloat s, GLfloat t);

	// ARB_vertex_buffer_object (EXT_pixel_buffer_object)
	void		(APIENTRY *glBindBufferARB)(GLenum target, GLuint buffer);
	void		(APIENTRY *glDeleteBuffersARB)(GLsizei n, const GLuint *buffers);
	void		(APIENTRY *glGenBuffersARB)(GLsizei n, GLuint *buffers);
	GLboolean	(APIENTRY *glIsBufferARB)(GLuint buffer);
	void		(APIENTRY *glBufferDataARB)(GLenum target, GLsizeiptrARB size, const void *data, GLenum usage);
	void		(APIENTRY *glBufferSubDataARB)(GLenum target, GLintptrARB offset, GLsizeiptrARB size, const void *data);
	void		(APIENTRY *glGetBufferSubDataARB)(GLenum target, GLintptrARB offset, GLsizeiptrARB size, void *data);
	void *		(APIENTRY *glMapBufferARB)(GLenum target, GLenum access);
	GLboolean	(APIENTRY *glUnmapBufferARB)(GLenum target);
	void		(APIENTRY *glGetBufferParameterivARB)(GLenum target, GLenum pname, GLint *params);
	void		(APIENTRY *glGetBufferPointervARB)(GLenum target, GLenum pname, void **params);

	// NV_register_combiners
	void (APIENTRY *glCombinerParameterfvNV)(GLenum pname, const GLfloat *params);
	void (APIENTRY *glCombinerParameterivNV)(GLenum pname, const GLint *params);
	void (APIENTRY *glCombinerParameterfNV)(GLenum pname, GLfloat param);
	void (APIENTRY *glCombinerParameteriNV)(GLenum pname, GLint param);
	void (APIENTRY *glCombinerInputNV)(GLenum stage, GLenum portion, GLenum variable, GLenum input, GLenum mapping, GLenum componentUsage);
	void (APIENTRY *glCombinerOutputNV)(GLenum stage, GLenum portion, GLenum abOutput, GLenum cdOutput, GLenum sumOutput, GLenum scale, GLenum bias, GLboolean abDotProduct, GLboolean cdDotProduct, GLboolean muxSum);
	void (APIENTRY *glFinalCombinerInputNV)(GLenum variable, GLenum input, GLenum mapping, GLenum componentUsage);
	void (APIENTRY *glGetCombinerInputParameterfvNV)(GLenum stage, GLenum portion, GLenum variable, GLenum pname, GLfloat *params);
	void (APIENTRY *glGetCombinerInputParameterivNV)(GLenum stage, GLenum portion, GLenum variable, GLenum pname, GLint *params);
	void (APIENTRY *glGetCombinerOutputParameterfvNV)(GLenum stage, GLenum portion, GLenum pname, GLfloat *params); 
	void (APIENTRY *glGetCombinerOutputParameterivNV)(GLenum stage, GLenum portion, GLenum pname, GLint *params);
	void (APIENTRY *glGetFinalCombinerInputParameterfvNV)(GLenum variable, GLenum pname, GLfloat *params);
	void (APIENTRY *glGetFinalCombinerInputParameterivNV)(GLenum variable, GLenum pname, GLint *params);

	// NV_register_combiners2
	void (APIENTRY *glCombinerStageParameterfvNV)(GLenum stage, GLenum pname, const GLfloat *params);

	// ATI_fragment_shader
    GLuint (APIENTRY *glGenFragmentShadersATI)(GLuint range);
    void (APIENTRY *glBindFragmentShaderATI)(GLuint id);
    void (APIENTRY *glDeleteFragmentShaderATI)(GLuint id);
    void (APIENTRY *glBeginFragmentShaderATI)();
    void (APIENTRY *glEndFragmentShaderATI)();
    void (APIENTRY *glPassTexCoordATI)(GLuint dst, GLuint coord, GLenum swizzle);
    void (APIENTRY *glSampleMapATI)(GLuint dst, GLuint interp, GLenum swizzle);
    void (APIENTRY *glColorFragmentOp1ATI)(GLenum op, GLuint dst, GLuint dstMask, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod);
    void (APIENTRY *glColorFragmentOp2ATI)(GLenum op, GLuint dst, GLuint dstMask, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod, GLuint arg2, GLuint arg2Rep, GLuint arg2Mod);
    void (APIENTRY *glColorFragmentOp3ATI)(GLenum op, GLuint dst, GLuint dstMask, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod, GLuint arg2, GLuint arg2Rep, GLuint arg2Mod, GLuint arg3, GLuint arg3Rep, GLuint arg3Mod);
    void (APIENTRY *glAlphaFragmentOp1ATI)(GLenum op, GLuint dst, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod);
    void (APIENTRY *glAlphaFragmentOp2ATI)(GLenum op, GLuint dst, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod, GLuint arg2, GLuint arg2Rep, GLuint arg2Mod);
    void (APIENTRY *glAlphaFragmentOp3ATI)(GLenum op, GLuint dst, GLuint dstMod, GLuint arg1, GLuint arg1Rep, GLuint arg1Mod, GLuint arg2, GLuint arg2Rep, GLuint arg2Mod, GLuint arg3, GLuint arg3Rep, GLuint arg3Mod);
    void (APIENTRY *glSetFragmentShaderConstantATI)(GLuint dst, const float *value);

	// NV_occlusion_query
	void (APIENTRY *glGenOcclusionQueriesNV)(GLsizei n, GLuint *ids);
	void (APIENTRY *glDeleteOcclusionQueriesNV)(GLsizei n, const GLuint *ids);
	GLboolean (APIENTRY *glIsOcclusionQueryNV)(GLuint id);
	void (APIENTRY *glBeginOcclusionQueryNV)(GLuint id);
	void (APIENTRY *glEndOcclusionQueryNV)();
	void (APIENTRY *glGetOcclusionQueryivNV)(GLuint id, GLenum pname, GLint *params);
	void (APIENTRY *glGetOcclusionQueryuivNV)(GLuint id, GLenum pname, GLuint *params);

	// EXT_framebuffer_object
	GLboolean (APIENTRY *glIsRenderbufferEXT)(GLuint renderbuffer);
	void (APIENTRY *glBindRenderbufferEXT)(GLenum target, GLuint renderbuffer);
	void (APIENTRY *glDeleteRenderbuffersEXT)(GLsizei n, const GLuint *renderbuffers);
	void (APIENTRY *glGenRenderbuffersEXT)(GLsizei n, GLuint *renderbuffers);
	void (APIENTRY *glRenderbufferStorageEXT)(GLenum target, GLenum internalformat, GLsizei width, GLsizei height);
	void (APIENTRY *glGetRenderbufferParameterivEXT)(GLenum target, GLenum pname, GLint *params);
	GLboolean (APIENTRY *glIsFramebufferEXT)(GLuint framebuffer);
	void (APIENTRY *glBindFramebufferEXT)(GLenum target, GLuint framebuffer);
	void (APIENTRY *glDeleteFramebuffersEXT)(GLsizei n, const GLuint *framebuffers);
	void (APIENTRY *glGenFramebuffersEXT)(GLsizei n, GLuint *framebuffers);
	GLenum (APIENTRY *glCheckFramebufferStatusEXT)(GLenum target);
	void (APIENTRY *glFramebufferTexture1DEXT)(GLenum target, GLenum attachment, GLenum textarget, GLuint texture, GLint level);
	void (APIENTRY *glFramebufferTexture2DEXT)(GLenum target, GLenum attachment, GLenum textarget, GLuint texture, GLint level);
	void (APIENTRY *glFramebufferTexture3DEXT)(GLenum target, GLenum attachment, GLenum textarget, GLuint texture, GLint level, GLint zoffset);
	void (APIENTRY *glFramebufferRenderbufferEXT)(GLenum target, GLenum attachment, GLenum renderbuffertarget, GLuint renderbuffer);
	void (APIENTRY *glGetFramebufferAttachmentParameterivEXT)(GLenum target, GLenum attachment, GLenum pname, GLint *params);
	void (APIENTRY *glGenerateMipmapEXT)(GLenum target);

	// ARB_vertex_program
    void (APIENTRY *glVertexAttrib1sARB)(GLuint index, GLshort x);
    void (APIENTRY *glVertexAttrib1fARB)(GLuint index, GLfloat x);
    void (APIENTRY *glVertexAttrib1dARB)(GLuint index, GLdouble x);
    void (APIENTRY *glVertexAttrib2sARB)(GLuint index, GLshort x, GLshort y);
    void (APIENTRY *glVertexAttrib2fARB)(GLuint index, GLfloat x, GLfloat y);
    void (APIENTRY *glVertexAttrib2dARB)(GLuint index, GLdouble x, GLdouble y);
    void (APIENTRY *glVertexAttrib3sARB)(GLuint index, GLshort x, GLshort y, GLshort z);
    void (APIENTRY *glVertexAttrib3fARB)(GLuint index, GLfloat x, GLfloat y, GLfloat z);
    void (APIENTRY *glVertexAttrib3dARB)(GLuint index, GLdouble x, GLdouble y, GLdouble z);
    void (APIENTRY *glVertexAttrib4sARB)(GLuint index, GLshort x, GLshort y, GLshort z, GLshort w);
    void (APIENTRY *glVertexAttrib4fARB)(GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
    void (APIENTRY *glVertexAttrib4dARB)(GLuint index, GLdouble x, GLdouble y, GLdouble z, GLdouble w);
    void (APIENTRY *glVertexAttrib4NubARB)(GLuint index, GLubyte x, GLubyte y, GLubyte z, GLubyte w);

    void (APIENTRY *glVertexAttrib1svARB)(GLuint index, const GLshort *v);
    void (APIENTRY *glVertexAttrib1fvARB)(GLuint index, const GLfloat *v);
    void (APIENTRY *glVertexAttrib1dvARB)(GLuint index, const GLdouble *v);
    void (APIENTRY *glVertexAttrib2svARB)(GLuint index, const GLshort *v);
    void (APIENTRY *glVertexAttrib2fvARB)(GLuint index, const GLfloat *v);
    void (APIENTRY *glVertexAttrib2dvARB)(GLuint index, const GLdouble *v);
    void (APIENTRY *glVertexAttrib3svARB)(GLuint index, const GLshort *v);
    void (APIENTRY *glVertexAttrib3fvARB)(GLuint index, const GLfloat *v);
    void (APIENTRY *glVertexAttrib3dvARB)(GLuint index, const GLdouble *v);
    void (APIENTRY *glVertexAttrib4bvARB)(GLuint index, const GLbyte *v);
    void (APIENTRY *glVertexAttrib4svARB)(GLuint index, const GLshort *v);
    void (APIENTRY *glVertexAttrib4ivARB)(GLuint index, const GLint *v);
    void (APIENTRY *glVertexAttrib4ubvARB)(GLuint index, const GLubyte *v);
    void (APIENTRY *glVertexAttrib4usvARB)(GLuint index, const GLushort *v);
    void (APIENTRY *glVertexAttrib4uivARB)(GLuint index, const GLuint *v);
    void (APIENTRY *glVertexAttrib4fvARB)(GLuint index, const GLfloat *v);
    void (APIENTRY *glVertexAttrib4dvARB)(GLuint index, const GLdouble *v);
    void (APIENTRY *glVertexAttrib4NbvARB)(GLuint index, const GLbyte *v);
    void (APIENTRY *glVertexAttrib4NsvARB)(GLuint index, const GLshort *v);
    void (APIENTRY *glVertexAttrib4NivARB)(GLuint index, const GLint *v);
    void (APIENTRY *glVertexAttrib4NubvARB)(GLuint index, const GLubyte *v);
    void (APIENTRY *glVertexAttrib4NusvARB)(GLuint index, const GLushort *v);
    void (APIENTRY *glVertexAttrib4NuivARB)(GLuint index, const GLuint *v);

    void (APIENTRY *glVertexAttribPointerARB)(GLuint index, GLint size, GLenum type, 
                                GLboolean normalized, GLsizei stride,
                                const void *pointer);

    void (APIENTRY *glEnableVertexAttribArrayARB)(GLuint index);
    void (APIENTRY *glDisableVertexAttribArrayARB)(GLuint index);

    void (APIENTRY *glProgramStringARB)(GLenum target, GLenum format, GLsizei len, 
                          const void *string); 

    void (APIENTRY *glBindProgramARB)(GLenum target, GLuint program);

    void (APIENTRY *glDeleteProgramsARB)(GLsizei n, const GLuint *programs);

    void (APIENTRY *glGenProgramsARB)(GLsizei n, GLuint *programs);

    void (APIENTRY *glProgramEnvParameter4dARB)(GLenum target, GLuint index,
                                  GLdouble x, GLdouble y, GLdouble z, GLdouble w);
    void (APIENTRY *glProgramEnvParameter4dvARB)(GLenum target, GLuint index,
                                   const GLdouble *params);
    void (APIENTRY *glProgramEnvParameter4fARB)(GLenum target, GLuint index,
                                  GLfloat x, GLfloat y, GLfloat z, GLfloat w);
    void (APIENTRY *glProgramEnvParameter4fvARB)(GLenum target, GLuint index,
                                   const GLfloat *params);

    void (APIENTRY *glProgramLocalParameter4dARB)(GLenum target, GLuint index,
                                    GLdouble x, GLdouble y, GLdouble z, GLdouble w);
    void (APIENTRY *glProgramLocalParameter4dvARB)(GLenum target, GLuint index,
                                     const GLdouble *params);
    void (APIENTRY *glProgramLocalParameter4fARB)(GLenum target, GLuint index,
                                    GLfloat x, GLfloat y, GLfloat z, GLfloat w);
    void (APIENTRY *glProgramLocalParameter4fvARB)(GLenum target, GLuint index,
                                     const GLfloat *params);

    void (APIENTRY *glGetProgramEnvParameterdvARB)(GLenum target, GLuint index,
                                     GLdouble *params);
    void (APIENTRY *glGetProgramEnvParameterfvARB)(GLenum target, GLuint index, 
                                     GLfloat *params);

    void (APIENTRY *glGetProgramLocalParameterdvARB)(GLenum target, GLuint index,
                                       GLdouble *params);
    void (APIENTRY *glGetProgramLocalParameterfvARB)(GLenum target, GLuint index, 
                                       GLfloat *params);

    void (APIENTRY *glGetProgramivARB)(GLenum target, GLenum pname, GLint *params);

    void (APIENTRY *glGetProgramStringARB)(GLenum target, GLenum pname, void *string);

    void (APIENTRY *glGetVertexAttribdvARB)(GLuint index, GLenum pname, GLdouble *params);
    void (APIENTRY *glGetVertexAttribfvARB)(GLuint index, GLenum pname, GLfloat *params);
    void (APIENTRY *glGetVertexAttribivARB)(GLuint index, GLenum pname, GLint *params);

    void (APIENTRY *glGetVertexAttribPointervARB)(GLuint index, GLenum pname, void **pointer);

    GLboolean (APIENTRY *glIsProgramARB)(GLuint program);

	// EXT_blend_minmax
	void (APIENTRY *glBlendEquationEXT)(GLenum mode);

	// EXT_secondary_color
	void (APIENTRY *glSecondaryColor3ub)(GLubyte red, GLubyte green, GLubyte blue);

	// WGL_ARB_extensions_string
	const char *(APIENTRY *wglGetExtensionsStringARB)(HDC hdc);

	// WGL_EXT_extensions_string
	const char *(APIENTRY *wglGetExtensionsStringEXT)();

	// WGL_ARB_make_current_read
	void (APIENTRY *wglMakeContextCurrentARB)(HDC hDrawDC, HDC hReadDC, HGLRC hglrc);
	HDC (APIENTRY *wglGetCurrentReadDCARB)();

	// WGL_EXT_swap_control
	BOOL (APIENTRY *wglSwapIntervalEXT)(int interval);
	int (APIENTRY *wglGetSwapIntervalEXT)();
};

struct VDOpenGLTechnique {
	const void *mpFragmentShader;
	uint8 mFragmentShaderMode;
};

struct VDOpenGLNVRegisterCombinerConfig {
	uint8 mConstantCount;
	uint8 mGeneralCombinerCount;
	const float (*mpConstants)[4];
	const uint8 *mpByteCode;
};

struct VDOpenGLATIFragmentShaderConfig {
	uint8 mConstantCount;
	const float (*mpConstants)[4];
	const uint8 *mpByteCode;
};

class VDOpenGLBinding : public VDAPITableWGL, public VDAPITableOpenGL, public VDAPITableOpenGLEXT {
	VDOpenGLBinding(const VDOpenGLBinding&);
	VDOpenGLBinding& operator=(const VDOpenGLBinding&);
public:
	VDOpenGLBinding();
	~VDOpenGLBinding();

	bool IsInited() const { return mhglrc != NULL; }

	bool Init();
	void Shutdown();

	bool Attach(HDC hdc, int minColorBits, int minAlphaBits, int minDepthBits, int minStencilBits, bool doubleBuffer);
	void Detach();

	bool AttachAux(HDC hdc, int minColorBits, int minAlphaBits, int minDepthBits, int minStencilBits, bool doubleBuffer);

	bool Begin(HDC hdc);
	void End();

	GLenum InitTechniques(const VDOpenGLTechnique *techniques, int techniqueCount);
	void DisableFragmentShaders();

public:
	// extension flags
	bool ARB_fragment_program;
	bool ARB_multitexture;
	bool ARB_pixel_buffer_object;
	bool ARB_vertex_program;
	bool ATI_fragment_shader;
	bool EXT_blend_minmax;
	bool EXT_blend_subtract;
	bool EXT_framebuffer_object;
	bool EXT_pixel_buffer_object;
	bool EXT_texture_edge_clamp;
	bool EXT_texture_env_combine;
	bool EXT_secondary_color;
	bool NV_occlusion_query;
	bool NV_register_combiners;
	bool NV_register_combiners2;

	// WGL extension flags
	bool ARB_make_current_read;
	bool EXT_swap_control;

protected:
	HMODULE mhmodOGL;
	HDC		mhdc;
	HGLRC	mhglrc;
	int		mPixelFormat;
};

enum VDOpenGLFragmentShaderMode {
	kVDOpenGLFragmentShaderModeNVRC,	// NV_register_combiners
	kVDOpenGLFragmentShaderModeNVRC2,	// NV_register_combiners2
	kVDOpenGLFragmentShaderModeATIFS,	// ATI_fragment_shader
	kVDOpenGLFragmentShaderModeCount
};

#endif
