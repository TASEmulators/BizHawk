#ifndef __gl_ATI_h_
#define __gl_ATI_h_
/*
** Copyright (C) 1998-2002 ATI Technologies Inc.
*/

#ifdef __cplusplus
extern "C" {
#endif

#ifndef APIENTRY
#define WIN32_LEAN_AND_MEAN 1
#include <windows.h>
#endif

/*
** Notes:
**
**  Listed support is for current drivers and should really only be used
**  as a guideline.  ISV should still use glGetString() and
**  wglGetExtensionsString() to determine the exact set of supported
**  GL and WGL extensions.
**
*/

/*
**  ARB EXTENSIONS
*/

/*
** GL_ARB_matrix_palette
**
** Support:
**   Rage   128       based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Not Supported
*/
#ifndef GL_ARB_matrix_palette
#define GL_ARB_matrix_palette                   1

#define GL_MATRIX_PALETTE_ARB                   0x8840
#define GL_MAX_MATRIX_PALETTE_STACK_DEPTH_ARB   0x8841
#define GL_MAX_PALETTE_MATRICES_ARB             0x8842
#define GL_CURRENT_PALETTE_MATRIX_ARB           0x8843
#define GL_MATRIX_INDEX_ARRAY_ARB               0x8844
#define GL_CURRENT_MATRIX_INDEX_ARB             0x8845
#define GL_MATRIX_INDEX_ARRAY_SIZE_ARB          0x8846
#define GL_MATRIX_INDEX_ARRAY_TYPE_ARB          0x8847
#define GL_MATRIX_INDEX_ARRAY_STRIDE_ARB        0x8848
#define GL_MATRIX_INDEX_ARRAY_POINTER_ARB       0x8849

typedef void (APIENTRY * PFNGLCURRENTPALETTEMATRIXARBPROC) (GLint index);
typedef void (APIENTRY * PFNGLMATRIXINDEXUBVARBPROC) (GLint size, GLubyte *indices);
typedef void (APIENTRY * PFNGLMATRIXINDEXUSVARBPROC) (GLint size, GLushort *indices);
typedef void (APIENTRY * PFNGLMATRIXINDEXUIVARBPROC) (GLint size, GLuint *indices);
typedef void (APIENTRY * PFNGLMATRIXINDEXPOINTERARBPROC) (GLint size, GLenum type, GLsizei stride, GLvoid *pointer);

#endif /* GL_ARB_matrix_palette */

/*
** GL_ARB_multisample
**
** Support:
**   Rage   128       based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_multisample
#define GL_ARB_multisample

#define GL_MULTISAMPLE_ARB              0x809D
#define GL_SAMPLE_ALPHA_TO_COVERAGE_ARB 0x809E
#define GL_SAMPLE_ALPHA_TO_ONE_ARB      0x809F
#define GL_SAMPLE_COVERAGE_ARB          0x80A0
#define GL_SAMPLE_BUFFERS_ARB           0x80A8
#define GL_SAMPLES_ARB                  0x80A9
#define GL_SAMPLE_COVERAGE_VALUE_ARB    0x80AA
#define GL_SAMPLE_COVERAGE_INVERT_ARB   0x80AB
#define GL_MULTISAMPLE_BIT_ARB          0x20000000

typedef void (APIENTRY * PFNGLSAMPLECOVERAGEARBPROC)(GLclampf value, GLboolean invert);

#endif /* GL_ARB_multisample */

/*
** GL_ARB_multitexture
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_multitexture
#define GL_ARB_multitexture                 1

#define GL_ACTIVE_TEXTURE_ARB               0x84E0
#define GL_CLIENT_ACTIVE_TEXTURE_ARB        0x84E1
#define GL_MAX_TEXTURE_UNITS_ARB            0x84E2
#define GL_TEXTURE0_ARB                     0x84C0
#define GL_TEXTURE1_ARB                     0x84C1
#define GL_TEXTURE2_ARB                     0x84C2
#define GL_TEXTURE3_ARB                     0x84C3
#define GL_TEXTURE4_ARB                     0x84C4
#define GL_TEXTURE5_ARB                     0x84C5
#define GL_TEXTURE6_ARB                     0x84C6
#define GL_TEXTURE7_ARB                     0x84C7
#define GL_TEXTURE8_ARB                     0x84C8
#define GL_TEXTURE9_ARB                     0x84C9
#define GL_TEXTURE10_ARB                    0x84CA
#define GL_TEXTURE11_ARB                    0x84CB
#define GL_TEXTURE12_ARB                    0x84CC
#define GL_TEXTURE13_ARB                    0x84CD
#define GL_TEXTURE14_ARB                    0x84CE
#define GL_TEXTURE15_ARB                    0x84CF
#define GL_TEXTURE16_ARB                    0x84D0
#define GL_TEXTURE17_ARB                    0x84D1
#define GL_TEXTURE18_ARB                    0x84D2
#define GL_TEXTURE19_ARB                    0x84D3
#define GL_TEXTURE20_ARB                    0x84D4
#define GL_TEXTURE21_ARB                    0x84D5
#define GL_TEXTURE22_ARB                    0x84D6
#define GL_TEXTURE23_ARB                    0x84D7
#define GL_TEXTURE24_ARB                    0x84D8
#define GL_TEXTURE25_ARB                    0x84D9
#define GL_TEXTURE26_ARB                    0x84DA
#define GL_TEXTURE27_ARB                    0x84DB
#define GL_TEXTURE28_ARB                    0x84DC
#define GL_TEXTURE29_ARB                    0x84DD
#define GL_TEXTURE30_ARB                    0x84DE
#define GL_TEXTURE31_ARB                    0x84DF

typedef void (APIENTRY * PFNGLACTIVETEXTUREARBPROC) (GLenum texture);
typedef void (APIENTRY * PFNGLCLIENTACTIVETEXTUREARBPROC) (GLenum texture);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1DARBPROC) (GLenum texture, GLdouble s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1DVARBPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1FARBPROC) (GLenum texture, GLfloat s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1FVARBPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1IARBPROC) (GLenum texture, GLint s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1IVARBPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1SARBPROC) (GLenum texture, GLshort s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1SVARBPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2DARBPROC) (GLenum texture, GLdouble s, GLdouble t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2DVARBPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2FARBPROC) (GLenum texture, GLfloat s, GLfloat t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2FVARBPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2IARBPROC) (GLenum texture, GLint s, GLint t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2IVARBPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2SARBPROC) (GLenum texture, GLshort s, GLshort t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2SVARBPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3DARBPROC) (GLenum texture, GLdouble s, GLdouble t, GLdouble r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3DVARBPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3FARBPROC) (GLenum texture, GLfloat s, GLfloat t, GLfloat r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3FVARBPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3IARBPROC) (GLenum texture, GLint s, GLint t, GLint r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3IVARBPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3SARBPROC) (GLenum texture, GLshort s, GLshort t, GLshort r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3SVARBPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4DARBPROC) (GLenum texture, GLdouble s, GLdouble t, GLdouble r, GLdouble q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4DVARBPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4FARBPROC) (GLenum texture, GLfloat s, GLfloat t, GLfloat r, GLfloat q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4FVARBPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4IARBPROC) (GLenum texture, GLint s, GLint t, GLint r, GLint q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4IVARBPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4SARBPROC) (GLenum texture, GLshort s, GLshort t, GLshort r, GLshort q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4SVARBPROC) (GLenum texture, const GLshort *v);

#endif /* GL_ARB_multitexture */

/*
** GL_ARB_texture_border_clamp
**
** Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_border_clamp
#define GL_ARB_texture_border_clamp 1

#define GL_CLAMP_TO_BORDER_ARB  0x812D

#endif /* GL_ARB_texture_border_clamp */

/*
** GL_ARB_texture_compression
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_compression
#define GL_ARB_texture_compression 1

#define GL_COMPRESSED_ALPHA_ARB               0x84E9
#define GL_COMPRESSED_LUMINANCE_ARB           0x84EA
#define GL_COMPRESSED_LUMINANCE_ALPHA_ARB     0x84EB
#define GL_COMPRESSED_INTENSITY_ARB           0x84EC
#define GL_COMPRESSED_RGB_ARB                 0x84ED
#define GL_COMPRESSED_RGBA_ARB                0x84EE
#define GL_TEXTURE_COMPRESSION_HINT_ARB       0x84EF
#define GL_TEXTURE_COMPRESSED_IMAGE_SIZE_ARB  0x86A0
#define GL_TEXTURE_COMPRESSED_ARB             0x86A1
#define GL_NUM_COMPRESSED_TEXTURE_FORMATS_ARB 0x86A2
#define GL_COMPRESSED_TEXTURE_FORMATS_ARB     0x86A3

typedef void (APIENTRY * PFNGLCOMPRESSEDTEXIMAGE3DARBPROC)(GLenum target, GLint level,
                                                           GLenum internalFormat, GLsizei width,
                                                           GLsizei height, GLsizei depth,
                                                           GLint border, GLsizei imageSize,
                                                           const GLvoid *data);
typedef void (APIENTRY * PFNGLCOMPRESSEDTEXIMAGE2DARBPROC)(GLenum target, GLint level,
                                                           GLenum internalFormat, GLsizei width,
                                                           GLsizei height, GLint border,
                                                           GLsizei imageSize, const GLvoid *data);
typedef void (APIENTRY * PFNGLCOMPRESSEDTEXIMAGE1DARBPROC)(GLenum target, GLint level,
                                                           GLenum internalFormat, GLsizei width,
                                                           GLint border, GLsizei imageSize,
                                                           const GLvoid *data);
typedef void (APIENTRY * PFNGLCOMPRESSEDTEXSUBIMAGE3DARBPROC)(GLenum target, GLint level,
                                                              GLint xoffset, GLint yoffset,
                                                              GLint zoffset, GLsizei width,
                                                              GLsizei height, GLsizei depth,
                                                              GLenum format, GLsizei imageSize,
                                                              const GLvoid *data);
typedef void (APIENTRY * PFNGLCOMPRESSEDTEXSUBIMAGE2DARBPROC)(GLenum target, GLint level,
                                                              GLint xoffset, GLint yoffset,
                                                              GLsizei width, GLsizei height,
                                                              GLenum format, GLsizei imageSize,
                                                              const GLvoid *data);
typedef void (APIENTRY * PFNGLCOMPRESSEDTEXSUBIMAGE1DARBPROC)(GLenum target, GLint level,
                                                              GLint xoffset, GLsizei width,
                                                              GLenum format, GLsizei imageSize,
                                                              const GLvoid *data);
typedef void (APIENTRY * PFNGLGETCOMPRESSEDTEXIMAGEARBPROC)(GLenum target, GLint lod,
                                                            GLvoid *img);


#endif /* GL_ARB_texture_compression */

/*
** GL_ARB_texture_cube_map
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_cube_map
#define GL_ARB_texture_cube_map 1

#define GL_NORMAL_MAP_ARB                  0x8511
#define GL_REFLECTION_MAP_ARB              0x8512
#define GL_TEXTURE_CUBE_MAP_ARB            0x8513
#define GL_TEXTURE_BINDING_CUBE_MAP_ARB    0x8514
#define GL_TEXTURE_CUBE_MAP_POSITIVE_X_ARB 0x8515
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_X_ARB 0x8516
#define GL_TEXTURE_CUBE_MAP_POSITIVE_Y_ARB 0x8517
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_Y_ARB 0x8518
#define GL_TEXTURE_CUBE_MAP_POSITIVE_Z_ARB 0x8519
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_Z_ARB 0x851A
#define GL_PROXY_TEXTURE_CUBE_MAP_ARB      0x851B
#define GL_MAX_CUBE_MAP_TEXTURE_SIZE_ARB   0x851C

#endif /* GL_ARB_texture_cube_map */

/*
** GL_ARB_texture_env_add
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_env_add
#define GL_ARB_texture_env_add  1

#endif /* GL_ARB_texture_env_add */

/*
** GL_ARB_texture_env_combine
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/

#ifndef GL_ARB_texture_env_combine
#define GL_ARB_texture_env_combine  1

#define GL_COMBINE_ARB        0x8570
#define GL_COMBINE_RGB_ARB    0x8571
#define GL_COMBINE_ALPHA_ARB  0x8572
#define GL_SOURCE0_RGB_ARB    0x8580
#define GL_SOURCE1_RGB_ARB    0x8581
#define GL_SOURCE2_RGB_ARB    0x8582
#define GL_SOURCE0_ALPHA_ARB  0x8588
#define GL_SOURCE1_ALPHA_ARB  0x8589
#define GL_SOURCE2_ALPHA_ARB  0x858A
#define GL_OPERAND0_RGB_ARB   0x8590
#define GL_OPERAND1_RGB_ARB   0x8591
#define GL_OPERAND2_RGB_ARB   0x8592
#define GL_OPERAND0_ALPHA_ARB 0x8598
#define GL_OPERAND1_ALPHA_ARB 0x8599
#define GL_OPERAND2_ALPHA_ARB 0x859A
#define GL_RGB_SCALE_ARB      0x8573
#define GL_ADD_SIGNED_ARB     0x8574
#define GL_INTERPOLATE_ARB    0x8575
#define GL_CONSTANT_ARB       0x8576
#define GL_PRIMARY_COLOR_ARB  0x8577
#define GL_PREVIOUS_ARB       0x8578
#define GL_SUBTRACT_ARB       0x84E7

#endif /* GL_ARB_texture_env_combine */

/*
** GL_ARB_texture_env_crossbar
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_env_crossbar
#define GL_ARB_texture_env_crossbar 1

#endif /* GL_ARB_texture_env_crossbar */

/*
** GL_ARB_texture_env_dot3
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_env_dot3
#define GL_ARB_texture_env_dot3 1

#define GL_DOT3_RGB_ARB  0x86AE
#define GL_DOT3_RGBA_ARB 0x86AF

#endif /* GL_ARB_texture_env_dot3 */

/*
** GL_ARB_texture_mirrored_repeat
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_texture_mirrored_repeat
#define GL_ARB_texture_mirrored_repeat 1

#define GL_MIRRORED_REPEAT_ARB               0x8370

#endif /* GL_ARB_texture_mirrored_repeat */

/*
** GL_ARB_transpose_matrix
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_transpose_matrix
#define GL_ARB_transpose_matrix 1

#define GL_TRANSPOSE_MODELVIEW_MATRIX_ARB  0x84E3
#define GL_TRANSPOSE_PROJECTION_MATRIX_ARB 0x84E4
#define GL_TRANSPOSE_TEXTURE_MATRIX_ARB    0x84E5
#define GL_TRANSPOSE_COLOR_MATRIX_ARB      0x84E6

typedef void (APIENTRY * PFNGLLOADTRANSPOSEMATRIXFARBPROC)(const GLfloat m[16]);
typedef void (APIENTRY * PFNGLLOADTRANSPOSEMATRIXDARBPROC)(const GLdouble m[16]);
typedef void (APIENTRY * PFNGLMULTTRANSPOSEMATRIXFARBPROC)(const GLfloat m[16]);
typedef void (APIENTRY * PFNGLMULTTRANSPOSEMATRIXDARBPROC)(const GLdouble m[16]);

#endif /* GL_ARB_transpose_matrix */

/*
** GL_ARB_vertex_blend
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_vertex_blend
#define GL_ARB_vertex_blend         1

#define GL_MAX_VERTEX_UNITS_ARB     0x86A4
#define GL_ACTIVE_VERTEX_UNITS_ARB  0x86A5
#define GL_WEIGHT_SUM_UNITY_ARB     0x86A6
#define GL_VERTEX_BLEND_ARB         0x86A7
#define GL_CURRENT_WEIGHT_ARB       0x86A8
#define GL_WEIGHT_ARRAY_TYPE_ARB    0x86A9
#define GL_WEIGHT_ARRAY_STRIDE_ARB  0x86AA
#define GL_WEIGHT_ARRAY_SIZE_ARB    0x86AB
#define GL_WEIGHT_ARRAY_POINTER_ARB 0x86AC
#define GL_WEIGHT_ARRAY_ARB         0x86AD
#define GL_MODELVIEW0_ARB           0x1700
#define GL_MODELVIEW1_ARB           0x850a
#define GL_MODELVIEW2_ARB           0x8722
#define GL_MODELVIEW3_ARB           0x8723
#define GL_MODELVIEW4_ARB           0x8724
#define GL_MODELVIEW5_ARB           0x8725
#define GL_MODELVIEW6_ARB           0x8726
#define GL_MODELVIEW7_ARB           0x8727
#define GL_MODELVIEW8_ARB           0x8728
#define GL_MODELVIEW9_ARB           0x8729
#define GL_MODELVIEW10_ARB          0x872A
#define GL_MODELVIEW11_ARB          0x872B
#define GL_MODELVIEW12_ARB          0x872C
#define GL_MODELVIEW13_ARB          0x872D
#define GL_MODELVIEW14_ARB          0x872E
#define GL_MODELVIEW15_ARB          0x872F
#define GL_MODELVIEW16_ARB          0x8730
#define GL_MODELVIEW17_ARB          0x8731
#define GL_MODELVIEW18_ARB          0x8732
#define GL_MODELVIEW19_ARB          0x8733
#define GL_MODELVIEW20_ARB          0x8734
#define GL_MODELVIEW21_ARB          0x8735
#define GL_MODELVIEW22_ARB          0x8736
#define GL_MODELVIEW23_ARB          0x8737
#define GL_MODELVIEW24_ARB          0x8738
#define GL_MODELVIEW25_ARB          0x8739
#define GL_MODELVIEW26_ARB          0x873A
#define GL_MODELVIEW27_ARB          0x873B
#define GL_MODELVIEW28_ARB          0x873C
#define GL_MODELVIEW29_ARB          0x873D
#define GL_MODELVIEW30_ARB          0x873E
#define GL_MODELVIEW31_ARB          0x873F

typedef void (APIENTRY *PFNGLWEIGHTBVARBPROC)(GLint size, GLbyte *weights);
typedef void (APIENTRY *PFNGLWEIGHTSVARBPROC)(GLint size, GLshort *weights);
typedef void (APIENTRY *PFNGLWEIGHTIVARBPROC)(GLint size, GLint *weights);
typedef void (APIENTRY *PFNGLWEIGHTFVARBPROC)(GLint size, GLfloat *weights);
typedef void (APIENTRY *PFNGLWEIGHTDVARBPROC)(GLint size, GLdouble *weights);
typedef void (APIENTRY *PFNGLWEIGHTUBVARBPROC)(GLint size, GLubyte *weights);
typedef void (APIENTRY *PFNGLWEIGHTUSVARBPROC)(GLint size, GLushort *weights);
typedef void (APIENTRY *PFNGLWEIGHTUIVARBPROC)(GLint size, GLuint *weights);
typedef void (APIENTRY *PFNGLWEIGHTPOINTERARBPROC)(GLint size, GLenum type, GLsizei stride, GLvoid *pointer);
typedef void (APIENTRY *PFNGLVERTEXBLENDARBPROC)(GLint count);

#endif /* GL_ARB_vertex_blend */

/*
** GL_ARB_vertex_program
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_vertex_program
#define GL_ARB_vertex_program 1

#define GL_VERTEX_PROGRAM_ARB                               0x8620
#define GL_VERTEX_PROGRAM_POINT_SIZE_ARB                    0x8642
#define GL_VERTEX_PROGRAM_TWO_SIDE_ARB                      0x8643
#define GL_COLOR_SUM_ARB                                    0x8458
#define GL_PROGRAM_FORMAT_ASCII_ARB                         0x8875
#define GL_VERTEX_ATTRIB_ARRAY_ENABLED_ARB                  0x8622
#define GL_VERTEX_ATTRIB_ARRAY_SIZE_ARB                     0x8623
#define GL_VERTEX_ATTRIB_ARRAY_STRIDE_ARB                   0x8624
#define GL_VERTEX_ATTRIB_ARRAY_TYPE_ARB                     0x8625
#define GL_VERTEX_ATTRIB_ARRAY_NORMALIZED_ARB               0x886A
#define GL_CURRENT_VERTEX_ATTRIB_ARB                        0x8626
#define GL_VERTEX_ATTRIB_ARRAY_POINTER_ARB                  0x8645
#define GL_PROGRAM_LENGTH_ARB                               0x8627
#define GL_PROGRAM_FORMAT_ARB                               0x8876
#define GL_PROGRAM_BINDING_ARB                              0x8677
#define GL_PROGRAM_INSTRUCTIONS_ARB                         0x88A0
#define GL_MAX_PROGRAM_INSTRUCTIONS_ARB                     0x88A1
#define GL_PROGRAM_NATIVE_INSTRUCTIONS_ARB                  0x88A2
#define GL_MAX_PROGRAM_NATIVE_INSTRUCTIONS_ARB              0x88A3
#define GL_PROGRAM_TEMPORARIES_ARB                          0x88A4
#define GL_MAX_PROGRAM_TEMPORARIES_ARB                      0x88A5
#define GL_PROGRAM_NATIVE_TEMPORARIES_ARB                   0x88A6
#define GL_MAX_PROGRAM_NATIVE_TEMPORARIES_ARB               0x88A7
#define GL_PROGRAM_PARAMETERS_ARB                           0x88A8
#define GL_MAX_PROGRAM_PARAMETERS_ARB                       0x88A9
#define GL_PROGRAM_NATIVE_PARAMETERS_ARB                    0x88AA
#define GL_MAX_PROGRAM_NATIVE_PARAMETERS_ARB                0x88AB
#define GL_PROGRAM_ATTRIBS_ARB                              0x88AC
#define GL_MAX_PROGRAM_ATTRIBS_ARB                          0x88AD
#define GL_PROGRAM_NATIVE_ATTRIBS_ARB                       0x88AE
#define GL_MAX_PROGRAM_NATIVE_ATTRIBS_ARB                   0x88AF
#define GL_PROGRAM_ADDRESS_REGISTERS_ARB                    0x88B0
#define GL_MAX_PROGRAM_ADDRESS_REGISTERS_ARB                0x88B1
#define GL_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB             0x88B2
#define GL_MAX_PROGRAM_NATIVE_ADDRESS_REGISTERS_ARB         0x88B3
#define GL_MAX_PROGRAM_LOCAL_PARAMETERS_ARB                 0x88B4
#define GL_MAX_PROGRAM_ENV_PARAMETERS_ARB                   0x88B5
#define GL_PROGRAM_UNDER_NATIVE_LIMITS_ARB                  0x88B6
#define GL_PROGRAM_STRING_ARB                               0x8628
#define GL_PROGRAM_ERROR_POSITION_ARB                       0x864B
#define GL_CURRENT_MATRIX_ARB                               0x8641
#define GL_TRANSPOSE_CURRENT_MATRIX_ARB                     0x88B7
#define GL_CURRENT_MATRIX_STACK_DEPTH_ARB                   0x8640
#define GL_MAX_VERTEX_ATTRIBS_ARB                           0x8869
#define GL_MAX_PROGRAM_MATRICES_ARB                         0x862F
#define GL_MAX_PROGRAM_MATRIX_STACK_DEPTH_ARB               0x862E
#define GL_PROGRAM_ERROR_STRING_ARB                         0x8874
#define GL_MATRIX0_ARB                                      0x88C0
#define GL_MATRIX1_ARB                                      0x88C1
#define GL_MATRIX2_ARB                                      0x88C2
#define GL_MATRIX3_ARB                                      0x88C3
#define GL_MATRIX4_ARB                                      0x88C4
#define GL_MATRIX5_ARB                                      0x88C5
#define GL_MATRIX6_ARB                                      0x88C6
#define GL_MATRIX7_ARB                                      0x88C7
#define GL_MATRIX8_ARB                                      0x88C8
#define GL_MATRIX9_ARB                                      0x88C9
#define GL_MATRIX10_ARB                                     0x88CA
#define GL_MATRIX11_ARB                                     0x88CB
#define GL_MATRIX12_ARB                                     0x88CC
#define GL_MATRIX13_ARB                                     0x88CD
#define GL_MATRIX14_ARB                                     0x88CE
#define GL_MATRIX15_ARB                                     0x88CF
#define GL_MATRIX16_ARB                                     0x88D0
#define GL_MATRIX17_ARB                                     0x88D1
#define GL_MATRIX18_ARB                                     0x88D2
#define GL_MATRIX19_ARB                                     0x88D3
#define GL_MATRIX20_ARB                                     0x88D4
#define GL_MATRIX21_ARB                                     0x88D5
#define GL_MATRIX22_ARB                                     0x88D6
#define GL_MATRIX23_ARB                                     0x88D7
#define GL_MATRIX24_ARB                                     0x88D8
#define GL_MATRIX25_ARB                                     0x88D9
#define GL_MATRIX26_ARB                                     0x88DA
#define GL_MATRIX27_ARB                                     0x88DB
#define GL_MATRIX28_ARB                                     0x88DC
#define GL_MATRIX29_ARB                                     0x88DD
#define GL_MATRIX30_ARB                                     0x88DE
#define GL_MATRIX31_ARB                                     0x88DF

typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1SARBPROC)(GLuint index, GLshort x);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1FARBPROC)(GLuint index, GLfloat x);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1DARBPROC)(GLuint index, GLdouble x);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2SARBPROC)(GLuint index, GLshort x, GLshort y);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2FARBPROC)(GLuint index, GLfloat x, GLfloat y);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2DARBPROC)(GLuint index, GLdouble x, GLdouble y);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3SARBPROC)(GLuint index, GLshort x, GLshort y, GLshort z);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3FARBPROC)(GLuint index, GLfloat x, GLfloat y, GLfloat z);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3DARBPROC)(GLuint index, GLdouble x, GLdouble y, GLdouble z);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4SARBPROC)(GLuint index, GLshort x, GLshort y, GLshort z, GLshort w);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4FARBPROC)(GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4DARBPROC)(GLuint index, GLdouble x, GLdouble y, GLdouble z, GLdouble w);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NUBARBPROC)(GLuint index, GLubyte x, GLubyte y, GLubyte z, GLubyte w);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1SVARBPROC)(GLuint index, const GLshort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1FVARBPROC)(GLuint index, const GLfloat *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB1DVARBPROC)(GLuint index, const GLdouble *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2SVARBPROC)(GLuint index, const GLshort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2FVARBPROC)(GLuint index, const GLfloat *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB2DVARBPROC)(GLuint index, const GLdouble *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3SVARBPROC)(GLuint index, const GLshort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3FVARBPROC)(GLuint index, const GLfloat *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB3DVARBPROC)(GLuint index, const GLdouble *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4BVARBPROC)(GLuint index, const GLbyte *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4SVARBPROC)(GLuint index, const GLshort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4IVARBPROC)(GLuint index, const GLint *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4UBVARBPROC)(GLuint index, const GLubyte *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4USVARBPROC)(GLuint index, const GLushort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4UIVARBPROC)(GLuint index, const GLuint *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4FVARBPROC)(GLuint index, const GLfloat *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4DVARBPROC)(GLuint index, const GLdouble *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NBVARBPROC)(GLuint index, const GLbyte *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NSVARBPROC)(GLuint index, const GLshort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NIVARBPROC)(GLuint index, const GLint *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NUBVARBPROC)(GLuint index, const GLubyte *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NUSVARBPROC)(GLuint index, const GLushort *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIB4NUIVARBPROC)(GLuint index, const GLuint *v);
typedef GLvoid (APIENTRY *PFNGLVERTEXATTRIBPOINTERARBPROC)(GLuint index, GLint size, GLenum type,
                                                           GLboolean normalized, GLsizei stride,
                                                           const GLvoid *pointer);
typedef GLvoid (APIENTRY *PFNGLENABLEVERTEXATTRIBARRAYARBPROC)(GLuint index);
typedef GLvoid (APIENTRY *PFNGLDISABLEVERTEXATTRIBARRAYARBPROC)(GLuint index);
typedef GLvoid (APIENTRY *PFNGLPROGRAMSTRINGARBPROC)(GLenum target, GLenum format, GLsizei len, const GLvoid *string);
typedef GLvoid (APIENTRY *PFNGLBINDPROGRAMARBPROC)(GLenum target, GLuint program);
typedef GLvoid (APIENTRY *PFNGLDELETEPROGRAMSARBPROC)(GLsizei n, const GLuint *programs);
typedef GLvoid (APIENTRY *PFNGLGENPROGRAMSARBPROC)(GLsizei n, GLuint *programs);
typedef GLvoid (APIENTRY *PFNGLPROGRAMENVPARAMETER4FARBPROC)(GLenum target, GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
typedef GLvoid (APIENTRY *PFNGLPROGRAMENVPARAMETER4DARBPROC)(GLenum target, GLuint index, GLdouble x, GLdouble y, GLdouble z, GLdouble w);
typedef GLvoid (APIENTRY *PFNGLPROGRAMENVPARAMETER4FVARBPROC)(GLenum target, GLuint index, const GLfloat *params);
typedef GLvoid (APIENTRY *PFNGLPROGRAMENVPARAMETER4DVARBPROC)(GLenum target, GLuint index, const GLdouble *params);
typedef GLvoid (APIENTRY *PFNGLPROGRAMLOCALPARAMETER4FARBPROC)(GLenum target, GLuint index, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
typedef GLvoid (APIENTRY *PFNGLPROGRAMLOCALPARAMETER4DARBPROC)(GLenum target, GLuint index, GLdouble x, GLdouble y, GLdouble z, GLdouble w);
typedef GLvoid (APIENTRY *PFNGLPROGRAMLOCALPARAMETER4FVARBPROC)(GLenum target, GLuint index, const GLfloat *params);
typedef GLvoid (APIENTRY *PFNGLPROGRAMLOCALPARAMETER4DVARBPROC)(GLenum target, GLuint index, const GLdouble *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMENVPARAMETERFVARBPROC)(GLenum target, GLuint index, GLfloat *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMENVPARAMETERDVARBPROC)(GLenum target, GLuint index, GLdouble *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMLOCALPARAMETERFVARBPROC)(GLenum target, GLuint index, GLfloat *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMLOCALPARAMETERDVARBPROC)(GLenum target, GLuint index, GLdouble *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMIVARBPROC)(GLenum target, GLenum pname, GLint *params);
typedef GLvoid (APIENTRY *PFNGLGETPROGRAMSTRINGARBPROC)(GLenum target, GLenum pname, GLvoid *string);
typedef GLvoid (APIENTRY *PFNGLGETVERTEXATTRIBDVARBPROC)(GLuint index, GLenum pname, GLdouble *params);
typedef GLvoid (APIENTRY *PFNGLGETVERTEXATTRIBFVARBPROC)(GLuint index, GLenum pname, GLfloat *params);
typedef GLvoid (APIENTRY *PFNGLGETVERTEXATTRIBIVARBPROC)(GLuint index, GLenum pname, GLint *params);
typedef GLvoid (APIENTRY *PFNGLGETVERTEXATTRIBPOINTERVARBPROC)(GLuint index, GLenum pname, GLvoid **pointer);
typedef GLboolean (APIENTRY *PFNGLISPROGRAMARBPROC)(GLuint program);

#endif /* GL_ARB_vertex_program */

/*
** GL_ARB_window_pos
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_window_pos
#define GL_ARB_window_pos   1

typedef void (APIENTRY * PFNGLWINDOWPOS2DARBPROC) (double x, double y);
typedef void (APIENTRY * PFNGLWINDOWPOS2FARBPROC) (float x, float y);
typedef void (APIENTRY * PFNGLWINDOWPOS2IARBPROC) (int x, int y);
typedef void (APIENTRY * PFNGLWINDOWPOS2SARBPROC) (short x, short y);
typedef void (APIENTRY * PFNGLWINDOWPOS2IVARBPROC) (const int *p);
typedef void (APIENTRY * PFNGLWINDOWPOS2SVARBPROC) (const short *p);
typedef void (APIENTRY * PFNGLWINDOWPOS2FVARBPROC) (const float *p);
typedef void (APIENTRY * PFNGLWINDOWPOS2DVARBPROC) (const double *p);
typedef void (APIENTRY * PFNGLWINDOWPOS3IARBPROC) (int x, int y, int z);
typedef void (APIENTRY * PFNGLWINDOWPOS3SARBPROC) (short x, short y, short z);
typedef void (APIENTRY * PFNGLWINDOWPOS3FARBPROC) (float x, float y, float z);
typedef void (APIENTRY * PFNGLWINDOWPOS3DARBPROC) (double x, double y, double z);
typedef void (APIENTRY * PFNGLWINDOWPOS3IVARBPROC) (const int *p);
typedef void (APIENTRY * PFNGLWINDOWPOS3SVARBPROC) (const short *p);
typedef void (APIENTRY * PFNGLWINDOWPOS3FVARBPROC) (const float *p);
typedef void (APIENTRY * PFNGLWINDOWPOS3DVARBPROC) (const double *p);

#endif /* GL_ARB_window_pos */

/*
** GL_ARB_depth_texture
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/

#ifndef GL_ARB_depth_texture
#define GL_ARB_depth_texture            1

#define GL_DEPTH_COMPONENT16_ARB        0x81A5
#define GL_DEPTH_COMPONENT24_ARB        0x81A6
#define GL_DEPTH_COMPONENT32_ARB        0x81A7

#define GL_TEXTURE_DEPTH_SIZE_ARB       0x884A

#define GL_DEPTH_TEXTURE_MODE_ARB       0x884B

#endif /* GL_ARB_depth_texture */

#ifndef GL_ARB_shadow
#define GL_ARB_shadow                   1

/*
** GL_ARB_shadow
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/

#define GL_TEXTURE_COMPARE_MODE_ARB     0x884C
#define GL_TEXTURE_COMPARE_FUNC_ARB     0X884D
#define GL_COMPARE_R_TO_TEXTURE_ARB     0x884E
#endif  /* GL_ARB_shadow */

/*
** GL_ARB_shadow_ambient
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_shadow_ambient
#define GL_ARB_shadow_ambient             1

#define GL_TEXTURE_COMPARE_FAIL_VALUE_ARB 0x80BF

#endif /* GL_ARB_shadow_ambient */

/*
**  ATI EXTENSIONS
*/

/*
** GL_ATI_envmap_bumpmap
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_envmap_bumpmap
#define GL_ATI_envmap_bumpmap       1

#define GL_BUMP_ROT_MATRIX_ATI      0x8775
#define GL_BUMP_ROT_MATRIX_SIZE_ATI 0x8776
#define GL_BUMP_NUM_TEX_UNITS_ATI   0x8777
#define GL_BUMP_TEX_UNITS_ATI       0x8778
#define GL_DUDV_ATI                 0x8779
#define GL_DU8DV8_ATI               0x877A
#define GL_BUMP_ENVMAP_ATI          0x877B
#define GL_BUMP_TARGET_ATI          0x877C

typedef void (APIENTRY * PFNGLTEXBUMPPARAMETERIVATIPROC) (GLenum pname, GLint *param);
typedef void (APIENTRY * PFNGLTEXBUMPPARAMETERFVATIPROC) (GLenum pname, GLfloat *param);
typedef void (APIENTRY * PFNGLGETTEXBUMPPARAMETERIVATIPROC) (GLenum pname, GLint *param);
typedef void (APIENTRY * PFNGLGETTEXBUMPPARAMETERFVATIPROC) (GLenum pname, GLfloat *param);


#endif /* GL_ATI_envmap_bumpmap */


/*
** GL_ATI_fragment_shader
**
** Support:
**   Rage 128         based : Not Supported
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_fragment_shader
#define GL_ATI_fragment_shader                   1

#define GL_FRAGMENT_SHADER_ATI                   0x8920
#define GL_REG_0_ATI                             0x8921
#define GL_REG_1_ATI                             0x8922
#define GL_REG_2_ATI                             0x8923
#define GL_REG_3_ATI                             0x8924
#define GL_REG_4_ATI                             0x8925
#define GL_REG_5_ATI                             0x8926
#define GL_REG_6_ATI                             0x8927
#define GL_REG_7_ATI                             0x8928
#define GL_REG_8_ATI                             0x8929
#define GL_REG_9_ATI                             0x892A
#define GL_REG_10_ATI                            0x892B
#define GL_REG_11_ATI                            0x892C
#define GL_REG_12_ATI                            0x892D
#define GL_REG_13_ATI                            0x892E
#define GL_REG_14_ATI                            0x892F
#define GL_REG_15_ATI                            0x8930
#define GL_REG_16_ATI                            0x8931
#define GL_REG_17_ATI                            0x8932
#define GL_REG_18_ATI                            0x8933
#define GL_REG_19_ATI                            0x8934
#define GL_REG_20_ATI                            0x8935
#define GL_REG_21_ATI                            0x8936
#define GL_REG_22_ATI                            0x8937
#define GL_REG_23_ATI                            0x8938
#define GL_REG_24_ATI                            0x8939
#define GL_REG_25_ATI                            0x893A
#define GL_REG_26_ATI                            0x893B
#define GL_REG_27_ATI                            0x893C
#define GL_REG_28_ATI                            0x893D
#define GL_REG_29_ATI                            0x893E
#define GL_REG_30_ATI                            0x893F
#define GL_REG_31_ATI                            0x8940
#define GL_CON_0_ATI                             0x8941
#define GL_CON_1_ATI                             0x8942
#define GL_CON_2_ATI                             0x8943
#define GL_CON_3_ATI                             0x8944
#define GL_CON_4_ATI                             0x8945
#define GL_CON_5_ATI                             0x8946
#define GL_CON_6_ATI                             0x8947
#define GL_CON_7_ATI                             0x8948
#define GL_CON_8_ATI                             0x8949
#define GL_CON_9_ATI                             0x894A
#define GL_CON_10_ATI                            0x894B
#define GL_CON_11_ATI                            0x894C
#define GL_CON_12_ATI                            0x894D
#define GL_CON_13_ATI                            0x894E
#define GL_CON_14_ATI                            0x894F
#define GL_CON_15_ATI                            0x8950
#define GL_CON_16_ATI                            0x8951
#define GL_CON_17_ATI                            0x8952
#define GL_CON_18_ATI                            0x8953
#define GL_CON_19_ATI                            0x8954
#define GL_CON_20_ATI                            0x8955
#define GL_CON_21_ATI                            0x8956
#define GL_CON_22_ATI                            0x8957
#define GL_CON_23_ATI                            0x8958
#define GL_CON_24_ATI                            0x8959
#define GL_CON_25_ATI                            0x895A
#define GL_CON_26_ATI                            0x895B
#define GL_CON_27_ATI                            0x895C
#define GL_CON_28_ATI                            0x895D
#define GL_CON_29_ATI                            0x895E
#define GL_CON_30_ATI                            0x895F
#define GL_CON_31_ATI                            0x8960
#define GL_MOV_ATI                               0x8961
#define GL_ADD_ATI                               0x8963
#define GL_MUL_ATI                               0x8964
#define GL_SUB_ATI                               0x8965
#define GL_DOT3_ATI                              0x8966
#define GL_DOT4_ATI                              0x8967
#define GL_MAD_ATI                               0x8968
#define GL_LERP_ATI                              0x8969
#define GL_CND_ATI                               0x896A
#define GL_CND0_ATI                              0x896B
#define GL_DOT2_ADD_ATI                          0x896C
#define GL_SECONDARY_INTERPOLATOR_ATI            0x896D
#define GL_NUM_FRAGMENT_REGISTERS_ATI            0x896E
#define GL_NUM_FRAGMENT_CONSTANTS_ATI            0x896F
#define GL_NUM_PASSES_ATI                        0x8970
#define GL_NUM_INSTRUCTIONS_PER_PASS_ATI         0x8971
#define GL_NUM_INSTRUCTIONS_TOTAL_ATI            0x8972
#define GL_NUM_INPUT_INTERPOLATOR_COMPONENTS_ATI 0x8973
#define GL_NUM_LOOPBACK_COMPONENTS_ATI           0x8974
#define GL_COLOR_ALPHA_PAIRING_ATI               0x8975
#define GL_SWIZZLE_STR_ATI                       0x8976
#define GL_SWIZZLE_STQ_ATI                       0x8977
#define GL_SWIZZLE_STR_DR_ATI                    0x8978
#define GL_SWIZZLE_STQ_DQ_ATI                    0x8979
#define GL_SWIZZLE_STRQ_ATI                      0x897A
#define GL_SWIZZLE_STRQ_DQ_ATI                   0x897B
#define GL_RED_BIT_ATI                           0x00000001
#define GL_GREEN_BIT_ATI                         0x00000002
#define GL_BLUE_BIT_ATI                          0x00000004
#define GL_2X_BIT_ATI                            0x00000001
#define GL_4X_BIT_ATI                            0x00000002
#define GL_8X_BIT_ATI                            0x00000004
#define GL_HALF_BIT_ATI                          0x00000008
#define GL_QUARTER_BIT_ATI                       0x00000010
#define GL_EIGHTH_BIT_ATI                        0x00000020
#define GL_SATURATE_BIT_ATI                      0x00000040
#define GL_COMP_BIT_ATI                          0x00000002
#define GL_NEGATE_BIT_ATI                        0x00000004
#define GL_BIAS_BIT_ATI                          0x00000008


typedef GLuint (APIENTRY *PFNGLGENFRAGMENTSHADERSATIPROC)(GLuint range);
typedef GLvoid (APIENTRY *PFNGLBINDFRAGMENTSHADERATIPROC)(GLuint id);
typedef GLvoid (APIENTRY *PFNGLDELETEFRAGMENTSHADERATIPROC)(GLuint id);
typedef GLvoid (APIENTRY *PFNGLBEGINFRAGMENTSHADERATIPROC)(GLvoid);
typedef GLvoid (APIENTRY *PFNGLENDFRAGMENTSHADERATIPROC)(GLvoid);
typedef GLvoid (APIENTRY *PFNGLPASSTEXCOORDATIPROC)(GLuint dst, GLuint coord, GLenum swizzle);
typedef GLvoid (APIENTRY *PFNGLSAMPLEMAPATIPROC)(GLuint dst, GLuint interp, GLenum swizzle);
typedef GLvoid (APIENTRY *PFNGLCOLORFRAGMENTOP1ATIPROC)(GLenum op, GLuint dst, GLuint dstMask,
                                                        GLuint dstMod, GLuint arg1, GLuint arg1Rep,
                                                        GLuint arg1Mod);
typedef GLvoid (APIENTRY *PFNGLCOLORFRAGMENTOP2ATIPROC)(GLenum op, GLuint dst, GLuint dstMask,
                                                        GLuint dstMod, GLuint arg1, GLuint arg1Rep,
                                                        GLuint arg1Mod, GLuint arg2, GLuint arg2Rep,
                                                        GLuint arg2Mod);
typedef GLvoid (APIENTRY *PFNGLCOLORFRAGMENTOP3ATIPROC)(GLenum op, GLuint dst, GLuint dstMask,
                                                        GLuint dstMod, GLuint arg1, GLuint arg1Rep,
                                                        GLuint arg1Mod, GLuint arg2, GLuint arg2Rep,
                                                        GLuint arg2Mod, GLuint arg3, GLuint arg3Rep,
                                                        GLuint arg3Mod);
typedef GLvoid (APIENTRY *PFNGLALPHAFRAGMENTOP1ATIPROC)(GLenum op, GLuint dst, GLuint dstMod,
                                                        GLuint arg1, GLuint arg1Rep, GLuint arg1Mod);
typedef GLvoid (APIENTRY *PFNGLALPHAFRAGMENTOP2ATIPROC)(GLenum op, GLuint dst, GLuint dstMod,
                                                        GLuint arg1, GLuint arg1Rep, GLuint arg1Mod,
                                                        GLuint arg2, GLuint arg2Rep, GLuint arg2Mod);
typedef GLvoid (APIENTRY *PFNGLALPHAFRAGMENTOP3ATIPROC)(GLenum op, GLuint dst, GLuint dstMod,
                                                        GLuint arg1, GLuint arg1Rep, GLuint arg1Mod,
                                                        GLuint arg2, GLuint arg2Rep, GLuint arg2Mod,
                                                        GLuint arg3, GLuint arg3Rep, GLuint arg3Mod);
typedef GLvoid (APIENTRY *PFNGLSETFRAGMENTSHADERCONSTANTATIPROC)(GLuint dst, const GLfloat *value);

#endif /* GL_ATI_fragment_shader */

/*
** GL_ARB_fragment_program
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_fragment_program
#define GL_ARB_fragment_program                    1

#define GL_FRAGMENT_PROGRAM_ARB                    0x8804
#define GL_PROGRAM_ALU_INSTRUCTIONS_ARB            0x8805
#define GL_PROGRAM_TEX_INSTRUCTIONS_ARB            0x8806
#define GL_PROGRAM_TEX_INDIRECTIONS_ARB            0x8807
#define GL_PROGRAM_NATIVE_ALU_INSTRUCTIONS_ARB     0x8808
#define GL_PROGRAM_NATIVE_TEX_INSTRUCTIONS_ARB     0x8809
#define GL_PROGRAM_NATIVE_TEX_INDIRECTIONS_ARB     0x880A
#define GL_MAX_PROGRAM_ALU_INSTRUCTIONS_ARB        0x880B
#define GL_MAX_PROGRAM_TEX_INSTRUCTIONS_ARB        0x880C
#define GL_MAX_PROGRAM_TEX_INDIRECTIONS_ARB        0x880D
#define GL_MAX_PROGRAM_NATIVE_ALU_INSTRUCTIONS_ARB 0x880E
#define GL_MAX_PROGRAM_NATIVE_TEX_INSTRUCTIONS_ARB 0x880F
#define GL_MAX_PROGRAM_NATIVE_TEX_INDIRECTIONS_ARB 0x8810
#define GL_MAX_TEXTURE_COORDS_ARB                  0x8871
#define GL_MAX_TEXTURE_IMAGE_UNITS_ARB             0x8872

#endif /* GL_ARB_fragment_program */

/*
** GL_ATI_pn_triangles
**
** Support
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_pn_triangles
#define GL_ATI_pn_triangles                       1

#define GL_PN_TRIANGLES_ATI                       0x87F0
#define GL_MAX_PN_TRIANGLES_TESSELATION_LEVEL_ATI 0x87F1
#define GL_PN_TRIANGLES_POINT_MODE_ATI            0x87F2
#define GL_PN_TRIANGLES_NORMAL_MODE_ATI           0x87F3
#define GL_PN_TRIANGLES_TESSELATION_LEVEL_ATI     0x87F4
#define GL_PN_TRIANGLES_POINT_MODE_LINEAR_ATI     0x87F5
#define GL_PN_TRIANGLES_POINT_MODE_CUBIC_ATI      0x87F6
#define GL_PN_TRIANGLES_NORMAL_MODE_LINEAR_ATI    0x87F7
#define GL_PN_TRIANGLES_NORMAL_MODE_QUADRATIC_ATI 0x87F8

typedef void (APIENTRY *PFNGLPNTRIANGLESIATIPROC)(GLenum pname, GLint param);
typedef void (APIENTRY *PFNGLPNTRIANGLESFATIPROC)(GLenum pname, GLfloat param);

#endif /* GL_ATI_pn_triangles */

/*
** GL_ATI_separate_stencil
**
** Support
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Not Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_separate_stencil
#define GL_ATI_separate_stencil                     1

#define GL_STENCIL_BACK_FUNC_ATI                    0x8800
#define GL_STENCIL_BACK_FAIL_ATI                    0x8801
#define GL_STENCIL_BACK_PASS_DEPTH_FAIL_ATI         0x8802
#define GL_STENCIL_BACK_PASS_DEPTH_PASS_ATI         0x8803

typedef void (APIENTRY *PFNGLSTENCILOPSEPARATEATIPROC)(GLenum face, GLenum sfail, GLenum dpfail, GLenum dppass);
typedef void (APIENTRY *PFNGLSTENCILFUNCSEPARATEATIPROC)(GLenum frontfunc, GLenum backfunc, GLint ref, GLuint mask);

#endif /* GL_ATI_separate_stencil */


/*
** GL_ATI_texture_mirror_once
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_texture_mirror_once
#define GL_ATI_texture_mirror_once  1

#define GL_MIRROR_CLAMP_ATI         0x8742
#define GL_MIRROR_CLAMP_TO_EDGE_ATI 0x8743

#endif /* GL_ATI_texture_mirror_once */

/*
** GL_ATI_vertex_streams
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_vertex_streams
#define GL_ATI_vertex_streams                                   1

#define GL_MAX_VERTEX_STREAMS_ATI                               0x876B
#define GL_VERTEX_SOURCE_ATI                                    0x876C
#define GL_VERTEX_STREAM0_ATI                                   0x876D
#define GL_VERTEX_STREAM1_ATI                                   0x876E
#define GL_VERTEX_STREAM2_ATI                                   0x876F
#define GL_VERTEX_STREAM3_ATI                                   0x8770
#define GL_VERTEX_STREAM4_ATI                                   0x8771
#define GL_VERTEX_STREAM5_ATI                                   0x8772
#define GL_VERTEX_STREAM6_ATI                                   0x8773
#define GL_VERTEX_STREAM7_ATI                                   0x8774

typedef void (APIENTRY * PFNGLCLIENTACTIVEVERTEXSTREAMATIPROC) (GLenum stream);
typedef void (APIENTRY * PFNGLVERTEXBLENDENVIATIPROC) (GLenum pname, GLint param);
typedef void (APIENTRY * PFNGLVERTEXBLENDENVFATIPROC) (GLenum pname, GLfloat param);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2SATIPROC) (GLenum stream, GLshort x, GLshort y);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2SVATIPROC) (GLenum stream, const GLshort *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2IATIPROC) (GLenum stream, GLint x, GLint y);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2IVATIPROC) (GLenum stream, const GLint *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2FATIPROC) (GLenum stream, GLfloat x, GLfloat y);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2FVATIPROC) (GLenum stream, const GLfloat *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2DATIPROC) (GLenum stream, GLdouble x, GLdouble y);
typedef void (APIENTRY * PFNGLVERTEXSTREAM2DVATIPROC) (GLenum stream, const GLdouble *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3SATIPROC) (GLenum stream, GLshort x, GLshort y, GLshort z);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3SVATIPROC) (GLenum stream, const GLshort *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3IATIPROC) (GLenum stream, GLint x, GLint y, GLint z);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3IVATIPROC) (GLenum stream, const GLint *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3FATIPROC) (GLenum stream, GLfloat x, GLfloat y, GLfloat z);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3FVATIPROC) (GLenum stream, const GLfloat *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3DATIPROC) (GLenum stream, GLdouble x, GLdouble y, GLdouble z);
typedef void (APIENTRY * PFNGLVERTEXSTREAM3DVATIPROC) (GLenum stream, const GLdouble *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4SATIPROC) (GLenum stream, GLshort x, GLshort y, GLshort z, GLshort w);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4SVATIPROC) (GLenum stream, const GLshort *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4IATIPROC) (GLenum stream, GLint x, GLint y, GLint z, GLint w);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4IVATIPROC) (GLenum stream, const GLint *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4FATIPROC) (GLenum stream, GLfloat x, GLfloat y, GLfloat z, GLfloat w);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4FVATIPROC) (GLenum stream, const GLfloat *v);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4DATIPROC) (GLenum stream, GLdouble x, GLdouble y, GLdouble z, GLdouble w);
typedef void (APIENTRY * PFNGLVERTEXSTREAM4DVATIPROC) (GLenum stream, const GLdouble *v);
typedef void (APIENTRY * PFNGLNORMALSTREAM3BATIPROC) (GLenum stream, GLbyte x, GLbyte y, GLbyte z);
typedef void (APIENTRY * PFNGLNORMALSTREAM3BVATIPROC) (GLenum stream, const GLbyte *v);
typedef void (APIENTRY * PFNGLNORMALSTREAM3SATIPROC) (GLenum stream, GLshort x, GLshort y, GLshort z);
typedef void (APIENTRY * PFNGLNORMALSTREAM3SVATIPROC) (GLenum stream, const GLshort *v);
typedef void (APIENTRY * PFNGLNORMALSTREAM3IATIPROC) (GLenum stream, GLint x, GLint y, GLint z);
typedef void (APIENTRY * PFNGLNORMALSTREAM3IVATIPROC) (GLenum stream, const GLint *v);
typedef void (APIENTRY * PFNGLNORMALSTREAM3FATIPROC) (GLenum stream, GLfloat x, GLfloat y, GLfloat z);
typedef void (APIENTRY * PFNGLNORMALSTREAM3FVATIPROC) (GLenum stream, const GLfloat *v);
typedef void (APIENTRY * PFNGLNORMALSTREAM3DATIPROC) (GLenum stream, GLdouble x, GLdouble y, GLdouble z);
typedef void (APIENTRY * PFNGLNORMALSTREAM3DVATIPROC) (GLenum stream, const GLdouble *v);

#endif /* GL_ATI_vertex_streams */

/*
** GL_ATI_vertex_array_object
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_vertex_array_object
#define GL_ATI_vertex_array_object              1

#define GL_STATIC_ATI                           0x8760
#define GL_DYNAMIC_ATI                          0x8761
#define GL_PRESERVE_ATI                         0x8762
#define GL_DISCARD_ATI                          0x8763

#define GL_OBJECT_BUFFER_SIZE_ATI               0x8764
#define GL_OBJECT_BUFFER_USAGE_ATI              0x8765
#define GL_ARRAY_OBJECT_BUFFER_ATI              0x8766
#define GL_ARRAY_OBJECT_OFFSET_ATI              0x8767

typedef GLuint (APIENTRY * PFNGLNEWOBJECTBUFFERATIPROC)(GLsizei size, const GLvoid *pointer, GLenum usage);
typedef GLboolean (APIENTRY * PFNGLISOBJECTBUFFERATIPROC)(GLuint buffer);
typedef void (APIENTRY * PFNGLUPDATEOBJECTBUFFERATIPROC)(GLuint buffer, GLuint offset, GLsizei size,
                                                         const GLvoid *pointer, GLenum preserve);
typedef void (APIENTRY * PFNGLGETOBJECTBUFFERFVATIPROC)(GLuint buffer, GLenum pname, GLfloat *params);
typedef void (APIENTRY * PFNGLGETOBJECTBUFFERIVATIPROC)(GLuint buffer, GLenum pname, GLint *params);
typedef void (APIENTRY * PFNGLFREEOBJECTBUFFERATIPROC)(GLuint buffer);
typedef void (APIENTRY * PFNGLARRAYOBJECTATIPROC)(GLenum array, GLint size, GLenum type,
                                                  GLsizei stride, GLuint buffer, GLuint offset);
typedef void (APIENTRY * PFNGLGETARRAYOBJECTFVATIPROC)(GLenum array, GLenum pname, GLfloat *params);
typedef void (APIENTRY * PFNGLGETARRAYOBJECTIVATIPROC)(GLenum array, GLenum pname, GLint *params);
typedef void (APIENTRY * PFNGLVARIANTARRAYOBJECTATIPROC)(GLuint id, GLenum type,
                                                         GLsizei stride, GLuint buffer, GLuint offset);
typedef void (APIENTRY * PFNGLGETVARIANTARRAYOBJECTFVATIPROC)(GLuint id, GLenum pname, GLfloat *params);
typedef void (APIENTRY * PFNGLGETVARIANTARRAYOBJECTIVATIPROC)(GLuint id, GLenum pname, GLint *params);
#endif  /* GL_ATI_vertex_array_object */

/*
** GL_ATI_element_array
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_element_array
#define GL_ATI_element_array            1

#define GL_ELEMENT_ARRAY_ATI            0x8768

#define GL_ELEMENT_ARRAY_TYPE_ATI       0x8769
#define GL_ELEMENT_ARRAY_POINTER_ATI    0x876A

typedef void (APIENTRY * PFNGLELEMENTPOINTERATIPROC)(GLenum type, const GLvoid *pointer);
typedef void (APIENTRY * PFNGLDRAWELEMENTARRAYATIPROC)(GLenum mode, GLsizei count);
typedef void (APIENTRY * PFNGLDRAWRANGEELEMENTARRAYATIPROC)(GLenum mode, GLuint start, GLuint end, GLsizei count);

#endif /* GL_ATI_element_array */

/*
** GL_ATI_map_object_buffer
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_map_object_buffer
#define GL_ATI_map_object_buffer                1

typedef void *(APIENTRY * PFNGLMAPOBJECTBUFFERATIPROC)(GLuint buffer);
typedef void (APIENTRY * PFNGLUNMAPOBJECTBUFFERATIPROC)(GLuint buffer);

#endif /* GL_ATI_map_object_buffer */

/*
** GL_ATI_vertex_attrib_array_object
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_vertex_attrib_array_object
#define GL_ATI_vertex_attrib_array_object 1

typedef void (APIENTRY * PFNGLVERTEXATTRIBARRAYOBJECTATIPROC)(GLuint index, GLint size, GLenum type,
                                                              GLboolean normalized, GLsizei stride,
                                                              GLuint buffer, GLuint offset);
typedef void (APIENTRY * PFNGLGETVERTEXATTRIBARRAYOBJECTFVATIPROC)(GLuint index, GLenum pname,
                                                                   GLfloat *params);
typedef void (APIENTRY * PFNGLGETVERTEXATTRIBARRAYOBJECTIVATIPROC)(GLuint index, GLenum pname,
                                                                   GLint *params);

#endif /* GL_ATI_vertex_attrib_array_object */

/*
** GL_ATI_texture_float
**
** Support:
**  Rage 128         based : Not Supported
**  Radeon 7xxx      based : Not Supported
**  Radeon 8xxx/9000 based : Not Supported
**  Radeon 9500/9700 based : Supported
*/

#ifndef GL_ATI_texture_float
#define GL_ATI_texture_float                1

#define GL_RGBA_FLOAT32_ATI                 0x8814
#define GL_RGB_FLOAT32_ATI                  0x8815
#define GL_ALPHA_FLOAT32_ATI                0x8816
#define GL_INTENSITY_FLOAT32_ATI            0x8817
#define GL_LUMINANCE_FLOAT32_ATI            0x8818
#define GL_LUMINANCE_ALPHA_FLOAT32_ATI      0x8819
#define GL_RGBA_FLOAT16_ATI                 0x881A
#define GL_RGB_FLOAT16_ATI                  0x881B
#define GL_ALPHA_FLOAT16_ATI                0x881C
#define GL_INTENSITY_FLOAT16_ATI            0x881D
#define GL_LUMINANCE_FLOAT16_ATI            0x881E
#define GL_LUMINANCE_ALPHA_FLOAT16_ATI      0x881F

#endif  /* GL_ATI_texture_float */

/*
** GL_ATI_draw_buffers
**
** Support:
**  Rage 128         based : Not Supported
**  Radeon 7xxx      based : Not Supported
**  Radeon 8xxx/9000 based : Not Supported
**  Radeon 9500/9700 based : Supported
*/

#ifndef GL_ATI_draw_buffers
#define GL_ATI_draw_buffers     1

#define GL_MAX_DRAW_BUFFERS_ATI 0x8824
#define GL_DRAW_BUFFER0_ATI     0x8825
#define GL_DRAW_BUFFER1_ATI     0x8826
#define GL_DRAW_BUFFER2_ATI     0x8827
#define GL_DRAW_BUFFER3_ATI     0x8828
#define GL_DRAW_BUFFER4_ATI     0x8829
#define GL_DRAW_BUFFER5_ATI     0x882A
#define GL_DRAW_BUFFER6_ATI     0x882B
#define GL_DRAW_BUFFER7_ATI     0x882C
#define GL_DRAW_BUFFER8_ATI     0x882D
#define GL_DRAW_BUFFER9_ATI     0x882E
#define GL_DRAW_BUFFER10_ATI    0x882F
#define GL_DRAW_BUFFER11_ATI    0x8830
#define GL_DRAW_BUFFER12_ATI    0x8831
#define GL_DRAW_BUFFER13_ATI    0x8832
#define GL_DRAW_BUFFER14_ATI    0x8833
#define GL_DRAW_BUFFER15_ATI    0x8834

typedef void (APIENTRY * PFNGLDRAWBUFFERSATIPROC) (GLsizei n, const GLenum *bufs);

#endif /* GL_ATI_draw_buffers */

/*
** GL_ATI_texture_env_combine3
**
** Support:
**  Rage 128         based : Not Supported
**  Radeon 7xxx      based : Supported
**  Radeon 8xxx/9000 based : Supported
**  Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATI_texture_env_combine3
#define GL_ATI_texture_env_combine3

#define GL_ATI_texture_env_combine3
#define GL_MODULATE_ADD_ATI                             0x8744
#define GL_MODULATE_SIGNED_ADD_ATI                      0x8745
#define GL_MODULATE_SUBTRACT_ATI                        0x8746

#endif /* GL_ATI_texture_env_combine3 */

/*
**  ATIX EXTENSIONS
*/

/*
** GL_ATIX_texture_env_route
**
** Support
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATIX_texture_env_route
#define GL_ATIX_texture_env_route 1

#define GL_SECONDARY_COLOR_ATIX                 0x8747
#define GL_TEXTURE_OUTPUT_RGB_ATIX              0x8748
#define GL_TEXTURE_OUTPUT_ALPHA_ATIX            0x8749

#endif /* GL_ATIX_texture_env_route */

/*
** GL_ATIX_vertex_shader_output_point_size
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ATIX_vertex_shader_output_point_size
#define GL_ATIX_vertex_shader_output_point_size 1

#define GL_OUTPUT_POINT_SIZE_ATIX       0x610E

#endif /* GL_ATIX_vertex_shader_output_point_size */

/*
**  EXT EXTENSIONS
*/
/*
** GL_EXT_stencil_wrap
**
** Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_stencil_wrap
#define GL_EXT_stencil_wrap                                     1

#define GL_INCR_WRAP_EXT                                        0x8507
#define GL_DECR_WRAP_EXT                                        0x8508

#endif  /* GL_EXT_stencil_wrap */

/*
** GL_EXT_abgr
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_abgr
#define GL_EXT_abgr                         1

#define GL_ABGR_EXT                         0x8000

#endif /* GL_EXT_abgr */


/*
** GL_EXT_bgra
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_bgra
#define GL_EXT_bgra                         1

#define GL_BGR_EXT                          0x80E0
#define GL_BGRA_EXT                         0x80E1

#endif /* GL_EXT_bgra */

/*
** GL_EXT_blend_color
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_blend_color
#define GL_EXT_blend_color              1

#define GL_CONSTANT_COLOR_EXT           0x8001
#define GL_ONE_MINUS_CONSTANT_COLOR_EXT 0x8002
#define GL_CONSTANT_ALPHA_EXT           0x8003
#define GL_ONE_MINUS_CONSTANT_ALPHA_EXT 0x8004
#define GL_BLEND_COLOR_EXT              0x8005

typedef void (APIENTRY * PFNGLBLENDCOLOREXTPROC)  (GLclampf red, GLclampf green, GLclampf blue, GLclampf alpha);

#endif  /* GL_EXT_blend_color */

/*
** GL_EXT_blend_func_separate
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/

#ifndef GL_EXT_blend_func_separate
#define GL_EXT_blend_func_separate  1

#define GL_BLEND_DST_RGB_EXT    0x80C8
#define GL_BLEND_SRC_RGB_EXT    0x80C9
#define GL_BLEND_DST_ALPHA_EXT  0x80CA
#define GL_BLEND_SRC_ALPHA_EXT  0x80CB

typedef void (APIENTRY * PFNGLBLENDFUNCSEPARATEEXTPROC)  (GLenum sfactorRGB, GLenum dfactorRGB,
                                                                                                                  GLenum sfactorAlpha, GLenum dfactorAlpha);
#endif /* GL_EXT_blend_func_separate */

/*
** GL_EXT_blend_minmax
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_blend_minmax
#define GL_EXT_blend_minmax     1

#define GL_FUNC_ADD_EXT         0x8006
#define GL_MIN_EXT              0x8007
#define GL_MAX_EXT              0x8008
#define GL_BLEND_EQUATION_EXT   0x8009

typedef void (APIENTRY * PFNGLBLENDEQUATIONEXTPROC)  (GLenum mode);

#endif /* GL_EXT_blend_minmax */

/*
** GL_EXT_blend_subtract
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_blend_subtract
#define GL_EXT_blend_subtract                1

#define GL_FUNC_SUBTRACT_EXT                 0x800A
#define GL_FUNC_REVERSE_SUBTRACT_EXT         0x800B

#endif /* GL_EXT_blend_subtract */

/*
** GL_EXT_clip_volume_hint
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_clip_volume_hint
#define GL_EXT_clip_volume_hint             1

#define GL_CLIP_VOLUME_CLIPPING_HINT_EXT    0x80F0

#endif /* GL_EXT_clip_volume_hint */

/*
** GL_EXT_compiled_vertex_array
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_compiled_vertex_array
#define GL_EXT_compiled_vertex_array        1

#define GL_ARRAY_ELEMENT_LOCK_FIRST_EXT     0x81A8
#define GL_ARRAY_ELEMENT_LOCK_COUNT_EXT     0x81A9

typedef void (APIENTRY * PFNGLLOCKARRAYSEXTPROC) (GLint first, GLsizei count);
typedef void (APIENTRY * PFNGLUNLOCKARRAYSEXTPROC) (void);

#endif /* GL_EXT_compiled_vertex_array */

/*
** GL_EXT_draw_range_elements
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_draw_range_elements
#define GL_EXT_draw_range_elements           1

#define GL_MAX_ELEMENTS_VERTICES_EXT         0x80E8
#define GL_MAX_ELEMENTS_INDICES_EXT          0x80E9

typedef void (APIENTRY * PFNGLDRAWRANGEELEMENTSEXTPROC) (GLenum  mode, GLuint start,
                                                         GLuint end, GLsizei count,
                                                         GLenum type, const GLvoid *indices);

#endif /* GL_EXT_draw_range_elements */

/*
** GL_EXT_fog_coord
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_fog_coord
#define GL_EXT_fog_coord                     1

#define GL_FOG_COORDINATE_SOURCE_EXT         0x8450
#define GL_FOG_COORDINATE_EXT                0x8451
#define GL_FRAGMENT_DEPTH_EXT                0x8452
#define GL_CURRENT_FOG_COORDINATE_EXT        0x8453
#define GL_FOG_COORDINATE_ARRAY_TYPE_EXT     0x8454
#define GL_FOG_COORDINATE_ARRAY_STRIDE_EXT   0x8455
#define GL_FOG_COORDINATE_ARRAY_POINTER_EXT  0x8456
#define GL_FOG_COORDINATE_ARRAY_EXT          0x8457

typedef void (APIENTRY * PFNGLFOGCOORDFEXTPROC) (GLfloat f);
typedef void (APIENTRY * PFNGLFOGCOORDDEXTPROC) (GLdouble f);
typedef void (APIENTRY * PFNGLFOGCOORDFVEXTPROC) (const GLfloat *v);
typedef void (APIENTRY * PFNGLFOGCOORDDVEXTPROC) (const GLdouble *v);
typedef void (APIENTRY * PFNGLFOGCOORDPOINTEREXTPROC) (GLenum type, GLsizei stride, void *pointer);

#endif /* GL_EXT_fog_coord */

/*
** GL_EXT_packed_pixels
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_packed_pixels
#define GL_EXT_packed_pixels                 1

#define GL_UNSIGNED_BYTE_3_3_2_EXT           0x8032
#define GL_UNSIGNED_SHORT_4_4_4_4_EXT        0x8033
#define GL_UNSIGNED_SHORT_5_5_5_1_EXT        0x8034
#define GL_UNSIGNED_INT_8_8_8_8_EXT          0x8035
#define GL_UNSIGNED_INT_10_10_10_2_EXT       0x8036

#endif /* GL_EXT_packed_pixels */

/*
** GL_EXT_point_parameters
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_point_parameters
#define GL_EXT_point_parameters              1

#define GL_POINT_SIZE_MIN_EXT                0x8126
#define GL_POINT_SIZE_MAX_EXT                0x8127
#define GL_POINT_FADE_THRESHOLD_SIZE_EXT     0x8128
#define GL_DISTANCE_ATTENUATION_EXT          0x8129

typedef void (APIENTRY * PFNGLPOINTPARAMETERFEXTPROC)  (GLenum pname, GLfloat param);
typedef void (APIENTRY * PFNGLPOINTPARAMETERFVEXTPROC)  (GLenum pname, GLfloat *params);
typedef void (APIENTRY * PFNGLPOINTPARAMETERIEXTPROC)  (GLenum pname, GLint param);
typedef void (APIENTRY * PFNGLPOINTPARAMETERIVEXTPROC)  (GLenum pname, GLint *params);

#endif /* GL_EXT_point_parameters */

/*
** GL_ARB_point_parameters
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_ARB_point_parameters
#define GL_ARB_point_parameters              1

#define GL_POINT_SIZE_MIN_ARB                0x8126
#define GL_POINT_SIZE_MAX_ARB                0x8127
#define GL_POINT_FADE_THRESHOLD_SIZE_ARB     0x8128
#define GL_POINT_DISTANCE_ATTENUATION_ARB    0x8129

typedef void (APIENTRY * PFNGLPOINTPARAMETERFARBPROC)  (GLenum pname, GLfloat param);
typedef void (APIENTRY * PFNGLPOINTPARAMETERFVARBPROC)  (GLenum pname, GLfloat *params);
typedef void (APIENTRY * PFNGLPOINTPARAMETERIARBPROC)  (GLenum pname, GLint param);
typedef void (APIENTRY * PFNGLPOINTPARAMETERIVARBPROC)  (GLenum pname, GLint *params);

#endif /* GL_ARB_point_parameters */

/*
** GL_EXT_rescale_normal
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_rescale_normal
#define GL_EXT_rescale_normal                1

#define GL_RESCALE_NORMAL_EXT                0x803A

#endif /* GL_EXT_rescale_normal */

/*
** GL_EXT_separate_specular_color
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_separate_specular_color
#define GL_EXT_separate_specular_color       1

#define GL_LIGHT_MODEL_COLOR_CONTROL_EXT     0x81F8
#define GL_SINGLE_COLOR_EXT                  0x81F9
#define GL_SEPARATE_SPECULAR_COLOR_EXT       0x81FA

#endif /* GL_EXT_separate_specular_color */

/*
** GL_EXT_secondary_color
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_secondary_color
#define GL_EXT_secondary_color               1

#define GL_COLOR_SUM_EXT                     0x8458
#define GL_CURRENT_SECONDARY_COLOR_EXT       0x8459
#define GL_SECONDARY_COLOR_ARRAY_SIZE_EXT    0x845A
#define GL_SECONDARY_COLOR_ARRAY_TYPE_EXT    0x845B
#define GL_SECONDARY_COLOR_ARRAY_STRIDE_EXT  0x845C
#define GL_SECONDARY_COLOR_ARRAY_POINTER_EXT 0x845D
#define GL_SECONDARY_COLOR_ARRAY_EXT         0x845E

typedef void (APIENTRY * PFNGLSECONDARYCOLOR3FEXTPROC) (GLfloat r, GLfloat g, GLfloat b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3DEXTPROC) (GLdouble r, GLdouble g, GLdouble b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3BEXTPROC) (GLbyte r, GLbyte g, GLbyte b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3SEXTPROC) (GLshort r, GLshort g, GLshort b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3IEXTPROC) (GLint r, GLint g, GLint b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3UBEXTPROC)(GLubyte r, GLubyte g, GLubyte b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3USEXTPROC)(GLushort r, GLushort g, GLushort b);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3UIEXTPROC)(GLuint r, GLuint g, GLuint b);

typedef void (APIENTRY * PFNGLSECONDARYCOLOR3FVEXTPROC) (const GLfloat *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3DVEXTPROC) (const GLdouble *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3BVEXTPROC) (const GLbyte *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3SVEXTPROC) (const GLshort *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3IVEXTPROC) (const GLint *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3UBVEXTPROC)(const GLubyte *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3USVEXTPROC)(const GLushort *v);
typedef void (APIENTRY * PFNGLSECONDARYCOLOR3UIVEXTPROC)(const GLuint *v);

typedef void (APIENTRY * PFNGLSECONDARYCOLORPOINTEREXTPROC)(GLint size, GLenum type, GLsizei stride, void *pointer);

#endif /* GL_EXT_secondary_color */


/*
** GL_EXT_texgen_reflection
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texgen_reflection
#define GL_EXT_texgen_reflection              1

#define GL_NORMAL_MAP_EXT                     0x8511
#define GL_REFLECTION_MAP_EXT                 0x8512

#endif /* GL_EXT_texgen_reflection */

/*
** GL_EXT_texture_cube_map
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_cube_map
#define GL_EXT_texture_cube_map              1

#define GL_TEXTURE_CUBE_MAP_EXT              0x8513
#define GL_TEXTURE_BINDING_CUBE_MAP_EXT      0x8514
#define GL_TEXTURE_CUBE_MAP_POSITIVE_X_EXT   0x8515
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_X_EXT   0x8516
#define GL_TEXTURE_CUBE_MAP_POSITIVE_Y_EXT   0x8517
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_Y_EXT   0x8518
#define GL_TEXTURE_CUBE_MAP_POSITIVE_Z_EXT   0x8519
#define GL_TEXTURE_CUBE_MAP_NEGATIVE_Z_EXT   0x851A
#define GL_PROXY_TEXTURE_CUBE_MAP_EXT        0x851B
#define GL_MAX_CUBE_MAP_TEXTURE_SIZE_EXT     0x851C

#endif /* GL_EXT_texture_cube_map */

/*
** GL_EXT_texture3D
**
** Support:
**   Rage 128         based : Supported (software)
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture3D
#define GL_EXT_texture3D                     1

#define GL_TEXTURE_BINDING_3D_EXT            0x806A
#define GL_PACK_SKIP_IMAGES_EXT              0x806B
#define GL_PACK_IMAGE_HEIGHT_EXT             0x806C
#define GL_UNPACK_SKIP_IMAGES_EXT            0x806D
#define GL_UNPACK_IMAGE_HEIGHT_EXT           0x806E
#define GL_TEXTURE_3D_EXT                    0x806F
#define GL_PROXY_TEXTURE_3D_EXT              0x8070
#define GL_TEXTURE_DEPTH_EXT                 0x8071
#define GL_TEXTURE_WRAP_R_EXT                0x8072
#define GL_MAX_3D_TEXTURE_SIZE_EXT           0x8073


typedef void (APIENTRY * PFNGLTEXIMAGE3DEXTPROC)(GLenum target, GLint level, GLenum internalformat,
                                                 GLsizei width, GLsizei height, GLsizei depth,
                                                 GLint border, GLenum format, GLenum type,
                                                 const GLvoid* pixels);

typedef void (APIENTRY * PFNGLTEXSUBIMAGE3DPROC)(GLenum target, GLint lod,
                                                 GLint xoffset, GLint yoffset, GLint zoffset,
                                                 GLsizei w, GLsizei h, GLsizei d,
                                                 GLenum format, GLenum type, const GLvoid *buf);

typedef void (APIENTRY * PFNGLCOPYTEXSUBIMAGE3DPROC)(GLenum target, GLint level, GLint xoffset,
                                                     GLint yoffset, GLint zoffset, GLint x,
                                                     GLint y, GLsizei width, GLsizei height);

#endif /* GL_EXT_texture3D */

/*
** GL_EXT_texture_compression_s3tc
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_compression_s3tc
#define GL_EXT_texture_compression_s3tc      1

#define GL_COMPRESSED_RGB_S3TC_DXT1_EXT      0x83F0
#define GL_COMPRESSED_RGBA_S3TC_DXT1_EXT     0x83F1
#define GL_COMPRESSED_RGBA_S3TC_DXT3_EXT     0x83F2
#define GL_COMPRESSED_RGBA_S3TC_DXT5_EXT     0x83F3

#endif /* GL_EXT_texture_compression_s3tc */

/*
** GL_EXT_texture_edge_clamp
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_edge_clamp
#define GL_EXT_texture_edge_clamp            1

#define GL_CLAMP_TO_EDGE_EXT                 0x812F

#endif /* GL_EXT_texture_edge_clamp */

/*
** GL_EXT_texture_env_add
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_env_add
#define GL_EXT_texture_env_add               1

#endif /* GL_EXT_texture_env_add */

/*
** GL_EXT_texture_env_combine
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_env_combine
#define GL_EXT_texture_env_combine           1

#define GL_COMBINE_EXT                       0x8570
#define GL_COMBINE_RGB_EXT                   0x8571
#define GL_COMBINE_ALPHA_EXT                 0x8572
#define GL_SOURCE0_RGB_EXT                   0x8580
#define GL_SOURCE1_RGB_EXT                   0x8581
#define GL_SOURCE2_RGB_EXT                   0x8582
#define GL_SOURCE0_ALPHA_EXT                 0x8588
#define GL_SOURCE1_ALPHA_EXT                 0x8589
#define GL_SOURCE2_ALPHA_EXT                 0x858A
#define GL_OPERAND0_RGB_EXT                  0x8590
#define GL_OPERAND1_RGB_EXT                  0x8591
#define GL_OPERAND2_RGB_EXT                  0x8592
#define GL_OPERAND0_ALPHA_EXT                0x8598
#define GL_OPERAND1_ALPHA_EXT                0x8599
#define GL_OPERAND2_ALPHA_EXT                0x859A
#define GL_RGB_SCALE_EXT                     0x8573
#define GL_ADD_SIGNED_EXT                    0x8574
#define GL_INTERPOLATE_EXT                   0x8575
#define GL_CONSTANT_EXT                      0x8576
#define GL_PRIMARY_COLOR_EXT                 0x8577
#define GL_PREVIOUS_EXT                      0x8578

#endif /* GL_EXT_texture_env_combine */

/*
** GL_EXT_texture_env_dot3
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_env_dot3
#define GL_EXT_texture_env_dot3 1

#define GL_DOT3_RGB_EXT  0x8740
#define GL_DOT3_RGBA_EXT 0x8741

#endif /* GL_EXT_texture_env_dot3 */

/*
** GL_EXT_texture_lod_bias
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_lod_bias
#define GL_EXT_texture_lod_bias              1

#define GL_TEXTURE_FILTER_CONTROL_EXT 0x8500
#define GL_TEXTURE_LOD_BIAS_EXT       0x8501
#define GL_MAX_TEXTURE_LOD_BIAS_EXT   0x84FD

#endif /* GL_EXT_texture_lod_bias */

/*
** GL_EXT_texture_filter_anisotropic
**
** Support:
**
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_filter_anisotropic
#define GL_EXT_texture_filter_anisotropic    1

#define GL_TEXTURE_MAX_ANISOTROPY_EXT        0x84FE
#define GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT    0x84FF

#endif /* GL_EXT_texture_filter_anisotropic */

/*
** GL_EXT_texture_object
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_object
#define GL_EXT_texture_object              1

#define GL_TEXTURE_PRIORITY_EXT            0x8066
#define GL_TEXTURE_RESIDENT_EXT            0x8067
#define GL_TEXTURE_1D_BINDING_EXT          0x8068
#define GL_TEXTURE_2D_BINDING_EXT          0x8069
#define GL_TEXTURE_3D_BINDING_EXT          0x806A

typedef void (APIENTRY * PFNGLGENTEXTURESEXTPROC) ( GLsizei n, GLuint *textures);
typedef void (APIENTRY * PFNGLDELETETEXTURESEXTPROC) ( GLsizei n, const GLuint *textures);
typedef void (APIENTRY * PFNGLBINDTEXTUREEXTPROC) ( GLenum target, GLuint texture);
typedef void (APIENTRY * PFNGLPRIORITIZETEXTURESEXTPROC) ( GLsizei n, const GLuint *textures, const GLclampf *priorities);
typedef GLboolean (APIENTRY * PFNGLARETEXTURESRESIDENTEXTPROC) ( GLsizei n, const GLuint *textures, GLboolean *residences);
typedef GLboolean (APIENTRY * PFNGLISTEXTUREEXTPROC) ( GLuint texture);

#endif /* GL_EXT_texture_object */

/*
** GL_EXT_texture_rectangle
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_texture_rectangle
#define GL_EXT_texture_rectangle            1

#define GL_TEXTURE_RECTANGLE_EXT            0x84F5
#define GL_TEXTURE_BINDING_RECTANGLE_EXT    0x84F6
#define GL_PROXY_TEXTURE_RECTANGLE_EXT      0x84F7
#define GL_MAX_RECTANGLE_TEXTURE_SIZE_EXT   0x84F8

#endif /* GL_EXT_texture_rectangle */


/*
** GL_EXT_vertex_array
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_vertex_array
#define GL_EXT_vertex_array                 1

#define GL_VERTEX_ARRAY_EXT                 0x8074
#define GL_NORMAL_ARRAY_EXT                 0x8075
#define GL_COLOR_ARRAY_EXT                  0x8076
#define GL_INDEX_ARRAY_EXT                  0x8077
#define GL_TEXTURE_COORD_ARRAY_EXT          0x8078
#define GL_EDGE_FLAG_ARRAY_EXT              0x8079
#define GL_VERTEX_ARRAY_SIZE_EXT            0x807A
#define GL_VERTEX_ARRAY_TYPE_EXT            0x807B
#define GL_VERTEX_ARRAY_STRIDE_EXT          0x807C
#define GL_VERTEX_ARRAY_COUNT_EXT           0x807D
#define GL_NORMAL_ARRAY_TYPE_EXT            0x807E
#define GL_NORMAL_ARRAY_STRIDE_EXT          0x807F
#define GL_NORMAL_ARRAY_COUNT_EXT           0x8080
#define GL_COLOR_ARRAY_SIZE_EXT             0x8081
#define GL_COLOR_ARRAY_TYPE_EXT             0x8082
#define GL_COLOR_ARRAY_STRIDE_EXT           0x8083
#define GL_COLOR_ARRAY_COUNT_EXT            0x8084
#define GL_INDEX_ARRAY_TYPE_EXT             0x8085
#define GL_INDEX_ARRAY_STRIDE_EXT           0x8086
#define GL_INDEX_ARRAY_COUNT_EXT            0x8087
#define GL_TEXTURE_COORD_ARRAY_SIZE_EXT     0x8088
#define GL_TEXTURE_COORD_ARRAY_TYPE_EXT     0x8089
#define GL_TEXTURE_COORD_ARRAY_STRIDE_EXT   0x808A
#define GL_TEXTURE_COORD_ARRAY_COUNT_EXT    0x808B
#define GL_EDGE_FLAG_ARRAY_STRIDE_EXT       0x808C
#define GL_EDGE_FLAG_ARRAY_COUNT_EXT        0x808D
#define GL_VERTEX_ARRAY_POINTER_EXT         0x808E
#define GL_NORMAL_ARRAY_POINTER_EXT         0x808F
#define GL_COLOR_ARRAY_POINTER_EXT          0x8090
#define GL_INDEX_ARRAY_POINTER_EXT          0x8091
#define GL_TEXTURE_COORD_ARRAY_POINTER_EXT  0x8092
#define GL_EDGE_FLAG_ARRAY_POINTER_EXT      0x8093

typedef void (APIENTRY * PFNGLARRAYELEMENTEXTPROC) (GLint i);
typedef void (APIENTRY * PFNGLCOLORPOINTEREXTPROC) (GLint size, GLenum type, GLsizei stride, GLsizei count, const GLvoid *pointer);
typedef void (APIENTRY * PFNGLDRAWARRAYSEXTPROC) (GLenum mode, GLint first, GLsizei count);
typedef void (APIENTRY * PFNGLEDGEFLAGPOINTEREXTPROC) (GLsizei stride, GLsizei count, const GLboolean *pointer);
typedef void (APIENTRY * PFNGLGETPOINTERVEXTPROC) (GLenum pname, GLvoid* *params);
typedef void (APIENTRY * PFNGLINDEXPOINTEREXTPROC) (GLenum type, GLsizei stride, GLsizei count, const GLvoid *pointer);
typedef void (APIENTRY * PFNGLNORMALPOINTEREXTPROC) (GLenum type, GLsizei stride, GLsizei count, const GLvoid *pointer);
typedef void (APIENTRY * PFNGLTEXCOORDPOINTEREXTPROC) (GLint size, GLenum type, GLsizei stride, GLsizei count, const GLvoid *pointer);
typedef void (APIENTRY * PFNGLVERTEXPOINTEREXTPROC) (GLint size, GLenum type, GLsizei stride, GLsizei count, const GLvoid *pointer);

#endif /* GL_EXT_vertex_array */

/*
** GL_EXT_multi_draw_arrays
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_multi_draw_arrays
#define GL_EXT_multi_draw_arrays				1

typedef void (APIENTRY * PFNGLMULTIDRAWARRAYSEXTPROC) (GLenum mode, GLint *first, GLsizei *count, GLsizei primcount);
typedef void (APIENTRY * PFNGLMULTIDRAWELEMENTSEXTPROC) (GLenum mode, GLsizei *count, GLenum type, const GLvoid **indices, GLsizei primcount);

#endif /* GL_EXT_multi_draw_arrays */

/*
** GL_EXT_vertex_shader
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_EXT_vertex_shader
#define GL_EXT_vertex_shader                               1

#define GL_VERTEX_SHADER_EXT                               0x8780
#define GL_VERTEX_SHADER_BINDING_EXT                       0x8781
#define GL_OP_INDEX_EXT                                    0x8782
#define GL_OP_NEGATE_EXT                                   0x8783
#define GL_OP_DOT3_EXT                                     0x8784
#define GL_OP_DOT4_EXT                                     0x8785
#define GL_OP_MUL_EXT                                      0x8786
#define GL_OP_ADD_EXT                                      0x8787
#define GL_OP_MADD_EXT                                     0x8788
#define GL_OP_FRAC_EXT                                     0x8789
#define GL_OP_MAX_EXT                                      0x878A
#define GL_OP_MIN_EXT                                      0x878B
#define GL_OP_SET_GE_EXT                                   0x878C
#define GL_OP_SET_LT_EXT                                   0x878D
#define GL_OP_CLAMP_EXT                                    0x878E
#define GL_OP_FLOOR_EXT                                    0x878F
#define GL_OP_ROUND_EXT                                    0x8790
#define GL_OP_EXP_BASE_2_EXT                               0x8791
#define GL_OP_LOG_BASE_2_EXT                               0x8792
#define GL_OP_POWER_EXT                                    0x8793
#define GL_OP_RECIP_EXT                                    0x8794
#define GL_OP_RECIP_SQRT_EXT                               0x8795
#define GL_OP_SUB_EXT                                      0x8796
#define GL_OP_CROSS_PRODUCT_EXT                            0x8797
#define GL_OP_MULTIPLY_MATRIX_EXT                          0x8798
#define GL_OP_MOV_EXT                                      0x8799
#define GL_OUTPUT_VERTEX_EXT                               0x879A
#define GL_OUTPUT_COLOR0_EXT                               0x879B
#define GL_OUTPUT_COLOR1_EXT                               0x879C
#define GL_OUTPUT_TEXTURE_COORD0_EXT                       0x879D
#define GL_OUTPUT_TEXTURE_COORD1_EXT                       0x879E
#define GL_OUTPUT_TEXTURE_COORD2_EXT                       0x879F
#define GL_OUTPUT_TEXTURE_COORD3_EXT                       0x87A0
#define GL_OUTPUT_TEXTURE_COORD4_EXT                       0x87A1
#define GL_OUTPUT_TEXTURE_COORD5_EXT                       0x87A2
#define GL_OUTPUT_TEXTURE_COORD6_EXT                       0x87A3
#define GL_OUTPUT_TEXTURE_COORD7_EXT                       0x87A4
#define GL_OUTPUT_TEXTURE_COORD8_EXT                       0x87A5
#define GL_OUTPUT_TEXTURE_COORD9_EXT                       0x87A6
#define GL_OUTPUT_TEXTURE_COORD10_EXT                      0x87A7
#define GL_OUTPUT_TEXTURE_COORD11_EXT                      0x87A8
#define GL_OUTPUT_TEXTURE_COORD12_EXT                      0x87A9
#define GL_OUTPUT_TEXTURE_COORD13_EXT                      0x87AA
#define GL_OUTPUT_TEXTURE_COORD14_EXT                      0x87AB
#define GL_OUTPUT_TEXTURE_COORD15_EXT                      0x87AC
#define GL_OUTPUT_TEXTURE_COORD16_EXT                      0x87AD
#define GL_OUTPUT_TEXTURE_COORD17_EXT                      0x87AE
#define GL_OUTPUT_TEXTURE_COORD18_EXT                      0x87AF
#define GL_OUTPUT_TEXTURE_COORD19_EXT                      0x87B0
#define GL_OUTPUT_TEXTURE_COORD20_EXT                      0x87B1
#define GL_OUTPUT_TEXTURE_COORD21_EXT                      0x87B2
#define GL_OUTPUT_TEXTURE_COORD22_EXT                      0x87B3
#define GL_OUTPUT_TEXTURE_COORD23_EXT                      0x87B4
#define GL_OUTPUT_TEXTURE_COORD24_EXT                      0x87B5
#define GL_OUTPUT_TEXTURE_COORD25_EXT                      0x87B6
#define GL_OUTPUT_TEXTURE_COORD26_EXT                      0x87B7
#define GL_OUTPUT_TEXTURE_COORD27_EXT                      0x87B8
#define GL_OUTPUT_TEXTURE_COORD28_EXT                      0x87B9
#define GL_OUTPUT_TEXTURE_COORD29_EXT                      0x87BA
#define GL_OUTPUT_TEXTURE_COORD30_EXT                      0x87BB
#define GL_OUTPUT_TEXTURE_COORD31_EXT                      0x87BC
#define GL_OUTPUT_FOG_EXT                                  0x87BD
#define GL_SCALAR_EXT                                      0x87BE
#define GL_VECTOR_EXT                                      0x87BF
#define GL_MATRIX_EXT                                      0x87C0
#define GL_VARIANT_EXT                                     0x87C1
#define GL_INVARIANT_EXT                                   0x87C2
#define GL_LOCAL_CONSTANT_EXT                              0x87C3
#define GL_LOCAL_EXT                                       0x87C4
#define GL_MAX_VERTEX_SHADER_INSTRUCTIONS_EXT              0x87C5
#define GL_MAX_VERTEX_SHADER_VARIANTS_EXT                  0x87C6
#define GL_MAX_VERTEX_SHADER_INVARIANTS_EXT                0x87C7
#define GL_MAX_VERTEX_SHADER_LOCAL_CONSTANTS_EXT           0x87C8
#define GL_MAX_VERTEX_SHADER_LOCALS_EXT                    0x87C9
#define GL_MAX_OPTIMIZED_VERTEX_SHADER_INSTRUCTIONS_EXT    0x87CA
#define GL_MAX_OPTIMIZED_VERTEX_SHADER_VARIANTS_EXT        0x87CB
#define GL_MAX_OPTIMIZED_VERTEX_SHADER_INVARIANTS_EXT      0x87CC
#define GL_MAX_OPTIMIZED_VERTEX_SHADER_LOCAL_CONSTANTS_EXT 0x87CD
#define GL_MAX_OPTIMIZED_VERTEX_SHADER_LOCALS_EXT          0x87CE
#define GL_VERTEX_SHADER_INSTRUCTIONS_EXT                  0x87CF
#define GL_VERTEX_SHADER_VARIANTS_EXT                      0x87D0
#define GL_VERTEX_SHADER_INVARIANTS_EXT                    0x87D1
#define GL_VERTEX_SHADER_LOCAL_CONSTANTS_EXT               0x87D2
#define GL_VERTEX_SHADER_LOCALS_EXT                        0x87D3
#define GL_VERTEX_SHADER_OPTIMIZED_EXT                     0x87D4
#define GL_X_EXT                                           0x87D5
#define GL_Y_EXT                                           0x87D6
#define GL_Z_EXT                                           0x87D7
#define GL_W_EXT                                           0x87D8
#define GL_NEGATIVE_X_EXT                                  0x87D9
#define GL_NEGATIVE_Y_EXT                                  0x87DA
#define GL_NEGATIVE_Z_EXT                                  0x87DB
#define GL_NEGATIVE_W_EXT                                  0x87DC
#define GL_ZERO_EXT                                        0x87DD
#define GL_ONE_EXT                                         0x87DE
#define GL_NEGATIVE_ONE_EXT                                0x87DF
#define GL_NORMALIZED_RANGE_EXT                            0x87E0
#define GL_FULL_RANGE_EXT                                  0x87E1
#define GL_CURRENT_VERTEX_EXT                              0x87E2
#define GL_MVP_MATRIX_EXT                                  0x87E3
#define GL_VARIANT_VALUE_EXT                               0x87E4
#define GL_VARIANT_DATATYPE_EXT                            0x87E5
#define GL_VARIANT_ARRAY_STRIDE_EXT                        0x87E6
#define GL_VARIANT_ARRAY_TYPE_EXT                          0x87E7
#define GL_VARIANT_ARRAY_EXT                               0x87E8
#define GL_VARIANT_ARRAY_POINTER_EXT                       0x87E9
#define GL_INVARIANT_VALUE_EXT                             0x87EA
#define GL_INVARIANT_DATATYPE_EXT                          0x87EB
#define GL_LOCAL_CONSTANT_VALUE_EXT                        0x87EC
#define GL_LOCAL_CONSTANT_DATATYPE_EXT                     0x87ED

typedef GLvoid    (APIENTRY * PFNGLBEGINVERTEXSHADEREXTPROC) (void);
typedef GLvoid    (APIENTRY * PFNGLENDVERTEXSHADEREXTPROC) (void);
typedef GLvoid    (APIENTRY * PFNGLBINDVERTEXSHADEREXTPROC) (GLuint id);
typedef GLuint    (APIENTRY * PFNGLGENVERTEXSHADERSEXTPROC) (GLuint range);
typedef GLvoid    (APIENTRY * PFNGLDELETEVERTEXSHADEREXTPROC) (GLuint id);
typedef GLvoid    (APIENTRY * PFNGLSHADEROP1EXTPROC) (GLenum op, GLuint res, GLuint arg1);
typedef GLvoid    (APIENTRY * PFNGLSHADEROP2EXTPROC) (GLenum op, GLuint res, GLuint arg1,
                                                      GLuint arg2);
typedef GLvoid    (APIENTRY * PFNGLSHADEROP3EXTPROC) (GLenum op, GLuint res, GLuint arg1,
                                                      GLuint arg2, GLuint arg3);
typedef GLvoid    (APIENTRY * PFNGLSWIZZLEEXTPROC) (GLuint res, GLuint in, GLenum outX,
                                                    GLenum outY, GLenum outZ, GLenum outW);
typedef GLvoid    (APIENTRY * PFNGLWRITEMASKEXTPROC) (GLuint res, GLuint in, GLenum outX,
                                                      GLenum outY, GLenum outZ, GLenum outW);
typedef GLvoid    (APIENTRY * PFNGLINSERTCOMPONENTEXTPROC) (GLuint res, GLuint src, GLuint num);
typedef GLvoid    (APIENTRY * PFNGLEXTRACTCOMPONENTEXTPROC) (GLuint res, GLuint src, GLuint num);
typedef GLuint    (APIENTRY * PFNGLGENSYMBOLSEXTPROC) (GLenum dataType, GLenum storageType,
                                                       GLenum range, GLuint components);
typedef GLvoid    (APIENTRY * PFNGLSETINVARIANTEXTPROC) (GLuint id, GLenum type, GLvoid *addr);
typedef GLvoid    (APIENTRY * PFNGLSETLOCALCONSTANTEXTPROC) (GLuint id, GLenum type, GLvoid *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTBVEXTPROC) (GLuint id, GLbyte *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTSVEXTPROC) (GLuint id, GLshort *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTIVEXTPROC) (GLuint id, GLint *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTFVEXTPROC) (GLuint id, GLfloat *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTDVEXTPROC) (GLuint id, GLdouble *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTUBVEXTPROC) (GLuint id, GLubyte *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTUSVEXTPROC) (GLuint id, GLushort *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTUIVEXTPROC) (GLuint id, GLuint *addr);
typedef GLvoid    (APIENTRY * PFNGLVARIANTPOINTEREXTPROC) (GLuint id, GLenum type,
                                                           GLuint stride, GLvoid *addr);
typedef GLvoid    (APIENTRY * PFNGLENABLEVARIANTCLIENTSTATEEXTPROC) (GLuint id);
typedef GLvoid    (APIENTRY * PFNGLDISABLEVARIANTCLIENTSTATEEXTPROC) (GLuint id);
typedef GLuint    (APIENTRY * PFNGLBINDLIGHTPARAMETEREXTPROC) (GLenum light, GLenum value);
typedef GLuint    (APIENTRY * PFNGLBINDMATERIALPARAMETEREXTPROC) (GLenum face, GLenum value);
typedef GLuint    (APIENTRY * PFNGLBINDTEXGENPARAMETEREXTPROC) (GLenum unit, GLenum coord,
                                                                GLenum value);
typedef GLuint    (APIENTRY * PFNGLBINDTEXTUREUNITPARAMETEREXTPROC) (GLenum unit, GLenum value);
typedef GLuint    (APIENTRY * PFNGLBINDPARAMETEREXTPROC) (GLenum value);
typedef GLboolean (APIENTRY * PFNGLISVARIANTENABLEDEXTPROC) (GLuint id, GLenum cap);
typedef GLvoid    (APIENTRY * PFNGLGETVARIANTBOOLEANVEXTPROC) (GLuint id, GLenum value,
                                                               GLboolean *data);
typedef GLvoid    (APIENTRY * PFNGLGETVARIANTINTEGERVEXTPROC) (GLuint id, GLenum value,
                                                               GLint *data);
typedef GLvoid    (APIENTRY * PFNGLGETVARIANTFLOATVEXTPROC) (GLuint id, GLenum value,
                                                             GLfloat *data);
typedef GLvoid    (APIENTRY * PFNGLGETVARIANTPOINTERVEXTPROC) (GLuint id, GLenum value,
                                                               GLvoid **data);
typedef GLvoid    (APIENTRY * PFNGLGETINVARIANTBOOLEANVEXTPROC) (GLuint id, GLenum value,
                                                                 GLboolean *data);
typedef GLvoid    (APIENTRY * PFNGLGETINVARIANTINTEGERVEXTPROC) (GLuint id, GLenum value,
                                                                 GLint *data);
typedef GLvoid    (APIENTRY * PFNGLGETINVARIANTFLOATVEXTPROC) (GLuint id, GLenum value,
                                                               GLfloat *data);
typedef GLvoid    (APIENTRY * PFNGLGETLOCALCONSTANTBOOLEANVEXTPROC) (GLuint id, GLenum value,
                                                                     GLboolean *data);
typedef GLvoid    (APIENTRY * PFNGLGETLOCALCONSTANTINTEGERVEXTPROC) (GLuint id, GLenum value,
                                                                     GLint *data);
typedef GLvoid    (APIENTRY * PFNGLGETLOCALCONSTANTFLOATVEXTPROC) (GLuint id, GLenum value,
                                                                   GLfloat *data);

#endif /* GL_EXT_vertex_shader */


/*
**  IHV EXTENSIONS
*/

/*
** GL_NV_occlusion_query
**
** Support:
**
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_NV_occlusion_query
#define GL_NV_occlusion_query              1

#define GL_PIXEL_COUNTER_BITS_NV           0x8864
#define GL_CURRENT_OCCLUSION_QUERY_ID_NV   0x8865
#define GL_PIXEL_COUNT_NV                  0x8866
#define GL_PIXEL_COUNT_AVAILABLE_NV        0x8867

typedef GLvoid    (APIENTRY * PFNGLGENOCCLUSIONQUERIESNVPROC) (GLsizei n, GLuint *ids);
typedef GLvoid    (APIENTRY * PFNGLDELETEOCCLUSIONQUERIESNVPROC) (GLsizei n, const GLuint *ids);
typedef GLboolean (APIENTRY * PFNGLISOCCLUSIONQUERYNVPROC) (GLuint id);
typedef GLvoid    (APIENTRY * PFNGLBEGINOCCLUSIONQUERYNVPROC) (GLuint id);
typedef GLvoid    (APIENTRY * PFNGLENDOCCLUSIONQUERYNVPROC) (GLvoid);
typedef GLvoid    (APIENTRY * PFNGLGETOCCLUSIONQUERYIVNVPROC) (GLuint id, GLenum pname, GLint *params);
typedef GLvoid    (APIENTRY * PFNGLGETOCCLUSIONQUERYUIVNVPROC) (GLuint id, GLenum pname, GLuint *params);

#endif /* GL_NV_occlusion_query */

/*
** GL_NV_texgen_reflection
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_NV_texgen_reflection
#define GL_NV_texgen_reflection              1

#define GL_NORMAL_MAP_NV                     0x8511
#define GL_REFLECTION_MAP_NV                 0x8512

#endif /* GL_NV_texgen_reflection */

/*
** GL_NV_blend_square
**
**  Support:
**   Radeon    * based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_NV_blend_square
#define GL_NV_blend_square                   1

#endif /* GL_NV_blend_square */

/*
** GL_S3_s3tc
**
**  Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_S3_s3tc
#define GL_S3_s3tc                           1

#define GL_RGB_S3TC                          0x83A0
#define GL_RGB4_S3TC                         0x83A1
#define GL_RGBA_S3TC                         0x83A2
#define GL_RGBA4_S3TC                        0x83A3

#endif /* GL_S3_s3tc */

/*
** GL_SGI_texture_edge_clamp
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SGI_texture_edge_clamp
#define GL_SGI_texture_edge_clamp            1

#define GL_CLAMP_TO_EDGE_SGI                 0x812F

#endif /* GL_SGI_texture_edge_clamp */

/*
** GL_SGIS_generate_mipmap
**
**  Support:
**   Rage 128         based : Not supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SGIS_generate_mipmap
#define GL_SGIS_generate_mipmap              1

#define GL_GENERATE_MIPMAP_SGIS              0x8191
#define GL_GENERATE_MIPMAP_HINT_SGIS         0x8192

#endif /* GL_SGIS_generate_mipmap */

/*
** GL_SGIS_multitexture
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SGIS_multitexture
#define GL_SGIS_multitexture                1

#define TEXTURE0_SGIS                       0x835E
#define TEXTURE1_SGIS                       0x835F
#define GL_SELECTED_TEXTURE_SGIS            0x83C0
#define GL_SELECTED_TEXTURE_COORD_SET_SGIS  0x83C1
#define GL_SELECTED_TEXTURE_TRANSFORM_SGIS  0x83C2
#define GL_MAX_TEXTURES_SGIS                0x83C3
#define GL_MAX_TEXTURE_COORD_SETS_SGIS      0x83C4
#define GL_TEXTURE_ENV_COORD_SET_SGIS       0x83C5
#define GL_TEXTURE0_SGIS                    0x83C6
#define GL_TEXTURE1_SGIS                    0x83C7
#define GL_TEXTURE2_SGIS                    0x83C8
#define GL_TEXTURE3_SGIS                    0x83C9
#define GL_TEXTURE4_SGIS                    0x83CA
#define GL_TEXTURE5_SGIS                    0x83CB
#define GL_TEXTURE6_SGIS                    0x83CC
#define GL_TEXTURE7_SGIS                    0x83CD
#define GL_TEXTURE8_SGIS                    0x83CE
#define GL_TEXTURE9_SGIS                    0x83CF
#define GL_TEXTURE10_SGIS                   0x83D0
#define GL_TEXTURE11_SGIS                   0x83D1
#define GL_TEXTURE12_SGIS                   0x83D2
#define GL_TEXTURE13_SGIS                   0x83D3
#define GL_TEXTURE14_SGIS                   0x83D4
#define GL_TEXTURE15_SGIS                   0x83D5
#define GL_TEXTURE16_SGIS                   0x83D6
#define GL_TEXTURE17_SGIS                   0x83D7
#define GL_TEXTURE18_SGIS                   0x83D8
#define GL_TEXTURE19_SGIS                   0x83D9
#define GL_TEXTURE20_SGIS                   0x83DA
#define GL_TEXTURE21_SGIS                   0x83DB
#define GL_TEXTURE22_SGIS                   0x83DC
#define GL_TEXTURE23_SGIS                   0x83DD
#define GL_TEXTURE24_SGIS                   0x83DE
#define GL_TEXTURE25_SGIS                   0x83DF
#define GL_TEXTURE26_SGIS                   0x83E0
#define GL_TEXTURE27_SGIS                   0x83E1
#define GL_TEXTURE28_SGIS                   0x83E2
#define GL_TEXTURE29_SGIS                   0x83E3
#define GL_TEXTURE30_SGIS                   0x83E4
#define GL_TEXTURE31_SGIS                   0x83E5

typedef void (APIENTRY * PFNGLSELECTTEXTURESGISPROC) (GLenum texture);
typedef void (APIENTRY * PFNGLSELECTTEXTURETRANSFORMSGISPROC) (GLenum texture);
typedef void (APIENTRY * PFNGLSELECTTEXTURECOORDSETSGISPROC) (GLenum texture);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1DSGISPROC) (GLenum texture, GLdouble s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1DVSGISPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1FSGISPROC) (GLenum texture, GLfloat s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1FVSGISPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1ISGISPROC) (GLenum texture, GLint s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1IVSGISPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1SSGISPROC) (GLenum texture, GLshort s);
typedef void (APIENTRY * PFNGLMULTITEXCOORD1SVSGISPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2DSGISPROC) (GLenum texture, GLdouble s, GLdouble t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2DVSGISPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2FSGISPROC) (GLenum texture, GLfloat s, GLfloat t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2FVSGISPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2ISGISPROC) (GLenum texture, GLint s, GLint t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2IVSGISPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2SSGISPROC) (GLenum texture, GLshort s, GLshort t);
typedef void (APIENTRY * PFNGLMULTITEXCOORD2SVSGISPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3DSGISPROC) (GLenum texture, GLdouble s, GLdouble t, GLdouble r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3DVSGISPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3FSGISPROC) (GLenum texture, GLfloat s, GLfloat t, GLfloat r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3FVSGISPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3ISGISPROC) (GLenum texture, GLint s, GLint t, GLint r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3IVSGISPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3SSGISPROC) (GLenum texture, GLshort s, GLshort t, GLshort r);
typedef void (APIENTRY * PFNGLMULTITEXCOORD3SVSGISPROC) (GLenum texture, const GLshort *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4DSGISPROC) (GLenum texture, GLdouble s, GLdouble t, GLdouble r, GLdouble q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4DVSGISPROC) (GLenum texture, const GLdouble *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4FSGISPROC) (GLenum texture, GLfloat s, GLfloat t, GLfloat r, GLfloat q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4FVSGISPROC) (GLenum texture, const GLfloat *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4ISGISPROC) (GLenum texture, GLint s, GLint t, GLint r, GLint q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4IVSGISPROC) (GLenum texture, const GLint *v);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4SSGISPROC) (GLenum texture, GLshort s, GLshort t, GLshort r, GLshort q);
typedef void (APIENTRY * PFNGLMULTITEXCOORD4SVSGISPROC) (GLenum texture, const GLshort *v);

#endif /* GL_SGIS_multitexture */

/*
** GL_SGIS_texture_border_clamp
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SGIS_texture_border_clamp
#define GL_SGIS_texture_border_clamp         1

#define GL_CLAMP_TO_BORDER_SGIS              0x812D

#endif

/*
** GL_SGIS_texture_lod
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SGIS_texture_lod
#define GL_SGIS_texture_lod                  1

#define GL_TEXTURE_MIN_LOD_SGIS              0x813A
#define GL_TEXTURE_MAX_LOD_SGIS              0x813B
#define GL_TEXTURE_BASE_LEVEL_SGIS           0x813C
#define GL_TEXTURE_MAX_LEVEL_SGIS            0x813D

#endif /* GL_SGIS_texture_lod */


/*
**  ISV EXTENSIONS
*/

/*
** GL_KTX_buffer_region
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_KTX_buffer_region
#define GL_KTX_buffer_region                 1

#define GL_KTX_FRONT_REGION                  0x0
#define GL_KTX_BACK_REGION                   0x1
#define GL_KTX_Z_REGION                      0x2
#define GL_KTX_STENCIL_REGION                0x3

typedef GLuint (APIENTRY * PFNGLBUFFERREGIONENABLEDEXTPROC) ( void );
typedef GLuint (APIENTRY * PFNGLNEWBUFFERREGIONEXTPROC) ( GLenum region );
typedef void (APIENTRY * PFNGLDELETEBUFFERREGIONEXTPROC) ( GLenum region );
typedef void (APIENTRY * PFNGLREADBUFFERREGIONEXTPROC) ( GLuint region, GLint x, GLint y, GLsizei width, GLsizei height);
typedef void (APIENTRY * PFNGLDRAWBUFFERREGIONEXTPROC) ( GLuint region, GLint x, GLint y, GLsizei width, GLsizei height, GLint xDest, GLint yDest);

#endif /* GL_KTX_buffer_region */

/*
** GL_WIN_swap_hint
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_WIN_swap_hint
#define GL_WIN_swap_hint                     1

typedef void (APIENTRY * PFNGLADDSWAPHINTRECTWINPROC) (GLint x, GLint y, GLsizei width, GLsizei height);

#endif /* GL_WIN_swap_hint */

/*
** GL_SUN_multi_draw_arrays
**
**  Support:
**   Rage 128         based : Supported
**   Radeon 7xxx      based : Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_SUN_multi_draw_arrays
#define GL_SUN_multi_draw_arrays			  1

typedef void (APIENTRY * PFNGLMULTIDRAWARRAYSSUNPROC) (GLenum mode, GLint *first, GLsizei *count, GLsizei primcount);
typedef void (APIENTRY * PFNGLMULTIDRAWELEMENTSSUNPROC) (GLenum mode, GLsizei *count, GLenum type, const GLvoid **indices, GLsizei primcount);

#endif /* GL_SUN_multi_draw_arrays */

/*
** HP_occlusion_test
**
** Support:
**   Rage 128         based : Not Supported
**   Radeon 7xxx      based : Not Supported
**   Radeon 8xxx/9000 based : Supported
**   Radeon 9500/9700 based : Supported
*/
#ifndef GL_HP_occlusion_test
#define GL_HP_occlusion_test                 1

#define GL_OCCLUSION_TEST_HP                 0x8165
#define GL_OCCLUSION_TEST_RESULT_HP          0x8166

#endif  /* HP_occlusion_test */

#ifdef __cplusplus
} /* extern "C" */
#endif

#endif /* __gl_ATI_h_ */
