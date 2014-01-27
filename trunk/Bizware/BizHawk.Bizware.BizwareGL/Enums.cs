using System;

namespace BizHawk.Bizware.BizwareGL
{
	// Summary:
	//     Used in GL.BlendFunc, GL.BlendFuncSeparate
	public enum BlendingFactor
	{
		// Summary:
		//     Original was GL_ZERO = 0
		Zero = 0,
		//
		// Summary:
		//     Original was GL_ONE = 1
		One = 1,
		//
		// Summary:
		//     Original was GL_SRC_COLOR = 0x0300
		SrcColor = 768,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_SRC_COLOR = 0x0301
		OneMinusSrcColor = 769,
		//
		// Summary:
		//     Original was GL_SRC_ALPHA = 0x0302
		SrcAlpha = 770,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_SRC_ALPHA = 0x0303
		OneMinusSrcAlpha = 771,
		//
		// Summary:
		//     Original was GL_DST_ALPHA = 0x0304
		DstAlpha = 772,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_DST_ALPHA = 0x0305
		OneMinusDstAlpha = 773,
		//
		// Summary:
		//     Original was GL_DST_COLOR = 0x0306
		DstColor = 774,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_DST_COLOR = 0x0307
		OneMinusDstColor = 775,
		//
		// Summary:
		//     Original was GL_SRC_ALPHA_SATURATE = 0x0308
		SrcAlphaSaturate = 776,
		//
		// Summary:
		//     Original was GL_CONSTANT_COLOR_EXT = 0x8001
		ConstantColorExt = 32769,
		//
		// Summary:
		//     Original was GL_CONSTANT_COLOR = 0x8001
		ConstantColor = 32769,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_CONSTANT_COLOR = 0x8002
		OneMinusConstantColor = 32770,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_CONSTANT_COLOR_EXT = 0x8002
		OneMinusConstantColorExt = 32770,
		//
		// Summary:
		//     Original was GL_CONSTANT_ALPHA = 0x8003
		ConstantAlpha = 32771,
		//
		// Summary:
		//     Original was GL_CONSTANT_ALPHA_EXT = 0x8003
		ConstantAlphaExt = 32771,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_CONSTANT_ALPHA_EXT = 0x8004
		OneMinusConstantAlphaExt = 32772,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_CONSTANT_ALPHA = 0x8004
		OneMinusConstantAlpha = 32772,
		//
		// Summary:
		//     Original was GL_SRC1_ALPHA = 0x8589
		Src1Alpha = 34185,
		//
		// Summary:
		//     Original was GL_SRC1_COLOR = 0x88F9
		Src1Color = 35065,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_SRC1_COLOR = 0x88FA
		OneMinusSrc1Color = 35066,
		//
		// Summary:
		//     Original was GL_ONE_MINUS_SRC1_ALPHA = 0x88FB
		OneMinusSrc1Alpha = 35067,
	}

	// Summary:
	//     Used in GL.Arb.BlendEquation, GL.BlendEquation and 2 other functions
	public enum BlendEquationMode
	{
		// Summary:
		//     Original was GL_FUNC_ADD = 0x8006
		FuncAdd = 32774,
		//
		// Summary:
		//     Original was GL_MIN = 0x8007
		Min = 32775,
		//
		// Summary:
		//     Original was GL_MAX = 0x8008
		Max = 32776,
		//
		// Summary:
		//     Original was GL_FUNC_SUBTRACT = 0x800A
		FuncSubtract = 32778,
		//
		// Summary:
		//     Original was GL_FUNC_REVERSE_SUBTRACT = 0x800B
		FuncReverseSubtract = 32779,
	}

	// Summary:
	//     Used in GL.BlitFramebuffer, GL.Clear and 1 other function
	[Flags]
	public enum ClearBufferMask
	{
		// Summary:
		//     Original was GL_NONE = 0
		None = 0,
		//
		// Summary:
		//     Original was GL_DEPTH_BUFFER_BIT = 0x00000100
		DepthBufferBit = 256,
		//
		// Summary:
		//     Original was GL_ACCUM_BUFFER_BIT = 0x00000200
		AccumBufferBit = 512,
		//
		// Summary:
		//     Original was GL_STENCIL_BUFFER_BIT = 0x00000400
		StencilBufferBit = 1024,
		//
		// Summary:
		//     Original was GL_COLOR_BUFFER_BIT = 0x00004000
		ColorBufferBit = 16384,
		//
		// Summary:
		//     Original was GL_COVERAGE_BUFFER_BIT_NV = 0x00008000
		CoverageBufferBitNv = 32768,
	}

	// Summary:
	//     Used in GL.TexParameter, GL.TexParameterI and 5 other functions
	public enum TextureParameterName
	{
		// Summary:
		//     Original was GL_TEXTURE_BORDER_COLOR = 0x1004
		TextureBorderColor = 4100,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAG_FILTER = 0x2800
		TextureMagFilter = 10240,
		//
		// Summary:
		//     Original was GL_TEXTURE_MIN_FILTER = 0x2801
		TextureMinFilter = 10241,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_S = 0x2802
		TextureWrapS = 10242,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_T = 0x2803
		TextureWrapT = 10243,
		//
		// Summary:
		//     Original was GL_TEXTURE_PRIORITY = 0x8066
		TexturePriority = 32870,
		//
		// Summary:
		//     Original was GL_TEXTURE_PRIORITY_EXT = 0x8066
		TexturePriorityExt = 32870,
		//
		// Summary:
		//     Original was GL_TEXTURE_DEPTH = 0x8071
		TextureDepth = 32881,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_R_EXT = 0x8072
		TextureWrapRExt = 32882,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_R_OES = 0x8072
		TextureWrapROes = 32882,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_R = 0x8072
		TextureWrapR = 32882,
		//
		// Summary:
		//     Original was GL_DETAIL_TEXTURE_LEVEL_SGIS = 0x809A
		DetailTextureLevelSgis = 32922,
		//
		// Summary:
		//     Original was GL_DETAIL_TEXTURE_MODE_SGIS = 0x809B
		DetailTextureModeSgis = 32923,
		//
		// Summary:
		//     Original was GL_TEXTURE_COMPARE_FAIL_VALUE = 0x80BF
		TextureCompareFailValue = 32959,
		//
		// Summary:
		//     Original was GL_SHADOW_AMBIENT_SGIX = 0x80BF
		ShadowAmbientSgix = 32959,
		//
		// Summary:
		//     Original was GL_DUAL_TEXTURE_SELECT_SGIS = 0x8124
		DualTextureSelectSgis = 33060,
		//
		// Summary:
		//     Original was GL_QUAD_TEXTURE_SELECT_SGIS = 0x8125
		QuadTextureSelectSgis = 33061,
		//
		// Summary:
		//     Original was GL_CLAMP_TO_BORDER = 0x812D
		ClampToBorder = 33069,
		//
		// Summary:
		//     Original was GL_CLAMP_TO_EDGE = 0x812F
		ClampToEdge = 33071,
		//
		// Summary:
		//     Original was GL_TEXTURE_WRAP_Q_SGIS = 0x8137
		TextureWrapQSgis = 33079,
		//
		// Summary:
		//     Original was GL_TEXTURE_MIN_LOD = 0x813A
		TextureMinLod = 33082,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAX_LOD = 0x813B
		TextureMaxLod = 33083,
		//
		// Summary:
		//     Original was GL_TEXTURE_BASE_LEVEL = 0x813C
		TextureBaseLevel = 33084,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAX_LEVEL = 0x813D
		TextureMaxLevel = 33085,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_CENTER_SGIX = 0x8171
		TextureClipmapCenterSgix = 33137,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_FRAME_SGIX = 0x8172
		TextureClipmapFrameSgix = 33138,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_OFFSET_SGIX = 0x8173
		TextureClipmapOffsetSgix = 33139,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_VIRTUAL_DEPTH_SGIX = 0x8174
		TextureClipmapVirtualDepthSgix = 33140,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_LOD_OFFSET_SGIX = 0x8175
		TextureClipmapLodOffsetSgix = 33141,
		//
		// Summary:
		//     Original was GL_TEXTURE_CLIPMAP_DEPTH_SGIX = 0x8176
		TextureClipmapDepthSgix = 33142,
		//
		// Summary:
		//     Original was GL_POST_TEXTURE_FILTER_BIAS_SGIX = 0x8179
		PostTextureFilterBiasSgix = 33145,
		//
		// Summary:
		//     Original was GL_POST_TEXTURE_FILTER_SCALE_SGIX = 0x817A
		PostTextureFilterScaleSgix = 33146,
		//
		// Summary:
		//     Original was GL_TEXTURE_LOD_BIAS_S_SGIX = 0x818E
		TextureLodBiasSSgix = 33166,
		//
		// Summary:
		//     Original was GL_TEXTURE_LOD_BIAS_T_SGIX = 0x818F
		TextureLodBiasTSgix = 33167,
		//
		// Summary:
		//     Original was GL_TEXTURE_LOD_BIAS_R_SGIX = 0x8190
		TextureLodBiasRSgix = 33168,
		//
		// Summary:
		//     Original was GL_GENERATE_MIPMAP = 0x8191
		GenerateMipmap = 33169,
		//
		// Summary:
		//     Original was GL_GENERATE_MIPMAP_SGIS = 0x8191
		GenerateMipmapSgis = 33169,
		//
		// Summary:
		//     Original was GL_TEXTURE_COMPARE_SGIX = 0x819A
		TextureCompareSgix = 33178,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAX_CLAMP_S_SGIX = 0x8369
		TextureMaxClampSSgix = 33641,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAX_CLAMP_T_SGIX = 0x836A
		TextureMaxClampTSgix = 33642,
		//
		// Summary:
		//     Original was GL_TEXTURE_MAX_CLAMP_R_SGIX = 0x836B
		TextureMaxClampRSgix = 33643,
		//
		// Summary:
		//     Original was GL_TEXTURE_LOD_BIAS = 0x8501
		TextureLodBias = 34049,
		//
		// Summary:
		//     Original was GL_DEPTH_TEXTURE_MODE = 0x884B
		DepthTextureMode = 34891,
		//
		// Summary:
		//     Original was GL_TEXTURE_COMPARE_MODE = 0x884C
		TextureCompareMode = 34892,
		//
		// Summary:
		//     Original was GL_TEXTURE_COMPARE_FUNC = 0x884D
		TextureCompareFunc = 34893,
		//
		// Summary:
		//     Original was GL_TEXTURE_SWIZZLE_R = 0x8E42
		TextureSwizzleR = 36418,
		//
		// Summary:
		//     Original was GL_TEXTURE_SWIZZLE_G = 0x8E43
		TextureSwizzleG = 36419,
		//
		// Summary:
		//     Original was GL_TEXTURE_SWIZZLE_B = 0x8E44
		TextureSwizzleB = 36420,
		//
		// Summary:
		//     Original was GL_TEXTURE_SWIZZLE_A = 0x8E45
		TextureSwizzleA = 36421,
		//
		// Summary:
		//     Original was GL_TEXTURE_SWIZZLE_RGBA = 0x8E46
		TextureSwizzleRgba = 36422,
	}

	// Summary:
	//     Not used directly.
	public enum TextureMinFilter
	{
		// Summary:
		//     Original was GL_NEAREST = 0x2600
		Nearest = 9728,
		//
		// Summary:
		//     Original was GL_LINEAR = 0x2601
		Linear = 9729,
		//
		// Summary:
		//     Original was GL_NEAREST_MIPMAP_NEAREST = 0x2700
		NearestMipmapNearest = 9984,
		//
		// Summary:
		//     Original was GL_LINEAR_MIPMAP_NEAREST = 0x2701
		LinearMipmapNearest = 9985,
		//
		// Summary:
		//     Original was GL_NEAREST_MIPMAP_LINEAR = 0x2702
		NearestMipmapLinear = 9986,
		//
		// Summary:
		//     Original was GL_LINEAR_MIPMAP_LINEAR = 0x2703
		LinearMipmapLinear = 9987,
		//
		// Summary:
		//     Original was GL_FILTER4_SGIS = 0x8146
		Filter4Sgis = 33094,
		//
		// Summary:
		//     Original was GL_LINEAR_CLIPMAP_LINEAR_SGIX = 0x8170
		LinearClipmapLinearSgix = 33136,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_CEILING_SGIX = 0x8184
		PixelTexGenQCeilingSgix = 33156,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_ROUND_SGIX = 0x8185
		PixelTexGenQRoundSgix = 33157,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_FLOOR_SGIX = 0x8186
		PixelTexGenQFloorSgix = 33158,
		//
		// Summary:
		//     Original was GL_NEAREST_CLIPMAP_NEAREST_SGIX = 0x844D
		NearestClipmapNearestSgix = 33869,
		//
		// Summary:
		//     Original was GL_NEAREST_CLIPMAP_LINEAR_SGIX = 0x844E
		NearestClipmapLinearSgix = 33870,
		//
		// Summary:
		//     Original was GL_LINEAR_CLIPMAP_NEAREST_SGIX = 0x844F
		LinearClipmapNearestSgix = 33871,
	}

	// Summary:
	//     Not used directly.
	public enum TextureMagFilter
	{
		// Summary:
		//     Original was GL_NEAREST = 0x2600
		Nearest = 9728,
		//
		// Summary:
		//     Original was GL_LINEAR = 0x2601
		Linear = 9729,
		//
		// Summary:
		//     Original was GL_LINEAR_DETAIL_SGIS = 0x8097
		LinearDetailSgis = 32919,
		//
		// Summary:
		//     Original was GL_LINEAR_DETAIL_ALPHA_SGIS = 0x8098
		LinearDetailAlphaSgis = 32920,
		//
		// Summary:
		//     Original was GL_LINEAR_DETAIL_COLOR_SGIS = 0x8099
		LinearDetailColorSgis = 32921,
		//
		// Summary:
		//     Original was GL_LINEAR_SHARPEN_SGIS = 0x80AD
		LinearSharpenSgis = 32941,
		//
		// Summary:
		//     Original was GL_LINEAR_SHARPEN_ALPHA_SGIS = 0x80AE
		LinearSharpenAlphaSgis = 32942,
		//
		// Summary:
		//     Original was GL_LINEAR_SHARPEN_COLOR_SGIS = 0x80AF
		LinearSharpenColorSgis = 32943,
		//
		// Summary:
		//     Original was GL_FILTER4_SGIS = 0x8146
		Filter4Sgis = 33094,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_CEILING_SGIX = 0x8184
		PixelTexGenQCeilingSgix = 33156,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_ROUND_SGIX = 0x8185
		PixelTexGenQRoundSgix = 33157,
		//
		// Summary:
		//     Original was GL_PIXEL_TEX_GEN_Q_FLOOR_SGIX = 0x8186
		PixelTexGenQFloorSgix = 33158,
	}

	public enum VertexAttributeType
	{
		// Summary:
		//     Original was GL_BYTE = 0x1400
		Byte = 5120,
		//
		// Summary:
		//     Original was GL_UNSIGNED_BYTE = 0x1401
		UnsignedByte = 5121,
		//
		// Summary:
		//     Original was GL_SHORT = 0x1402
		Short = 5122,
		//
		// Summary:
		//     Original was GL_UNSIGNED_SHORT = 0x1403
		UnsignedShort = 5123,
		//
		// Summary:
		//     Original was GL_INT = 0x1404
		Int = 5124,
		//
		// Summary:
		//     Original was GL_UNSIGNED_INT = 0x1405
		UnsignedInt = 5125,
		//
		// Summary:
		//     Original was GL_FLOAT = 0x1406
		Float = 5126,
		//
		// Summary:
		//     Original was GL_DOUBLE = 0x140A
		Double = 5130,
		//
		// Summary:
		//     Original was GL_HALF_FLOAT = 0x140B
		HalfFloat = 5131,
		//
		// Summary:
		//     Original was GL_FIXED = 0x140C
		Fixed = 5132,
		//
		// Summary:
		//     Original was GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368
		UnsignedInt2101010Rev = 33640,
		//
		// Summary:
		//     Original was GL_INT_2_10_10_10_REV = 0x8D9F
		Int2101010Rev = 36255,
	}

	// Summary:
	//     Used in GL.Apple.DrawElementArray, GL.Apple.DrawRangeElementArray and 38
	//     other functions
	public enum PrimitiveType
	{
		// Summary:
		//     Original was GL_POINTS = 0x0000
		Points = 0,
		//
		// Summary:
		//     Original was GL_LINES = 0x0001
		Lines = 1,
		//
		// Summary:
		//     Original was GL_LINE_LOOP = 0x0002
		LineLoop = 2,
		//
		// Summary:
		//     Original was GL_LINE_STRIP = 0x0003
		LineStrip = 3,
		//
		// Summary:
		//     Original was GL_TRIANGLES = 0x0004
		Triangles = 4,
		//
		// Summary:
		//     Original was GL_TRIANGLE_STRIP = 0x0005
		TriangleStrip = 5,
		//
		// Summary:
		//     Original was GL_TRIANGLE_FAN = 0x0006
		TriangleFan = 6,
		//
		// Summary:
		//     Original was GL_QUADS = 0x0007
		Quads = 7,
		//
		// Summary:
		//     Original was GL_QUAD_STRIP = 0x0008
		QuadStrip = 8,
		//
		// Summary:
		//     Original was GL_POLYGON = 0x0009
		Polygon = 9,
	}

}