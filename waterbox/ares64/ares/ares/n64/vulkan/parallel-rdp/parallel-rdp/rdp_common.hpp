/* Copyright (c) 2020 Themaister
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#pragma once

namespace Vulkan
{
class Program;
class Shader;
}

namespace RDP
{
template <typename Program, typename Shader> struct Shaders;
using ShaderBank = Shaders<Vulkan::Program *, Vulkan::Shader *>;

// list of command IDs
enum class Op
{
	Nop = 0,

	MetaSignalTimeline = 1,
	MetaFlush = 2,
	MetaIdle = 3,
	MetaSetQuirks = 4,

	FillTriangle = 0x08,
	FillZBufferTriangle = 0x09,
	TextureTriangle = 0x0a,
	TextureZBufferTriangle = 0x0b,
	ShadeTriangle = 0x0c,
	ShadeZBufferTriangle = 0x0d,
	ShadeTextureTriangle = 0x0e,
	ShadeTextureZBufferTriangle = 0x0f,
	TextureRectangle = 0x24,
	TextureRectangleFlip = 0x25,
	SyncLoad = 0x26,
	SyncPipe = 0x27,
	SyncTile = 0x28,
	SyncFull = 0x29,
	SetKeyGB = 0x2a,
	SetKeyR = 0x2b,
	SetConvert = 0x2c,
	SetScissor = 0x2d,
	SetPrimDepth = 0x2e,
	SetOtherModes = 0x2f,
	LoadTLut = 0x30,
	SetTileSize = 0x32,
	LoadBlock = 0x33,
	LoadTile = 0x34,
	SetTile = 0x35,
	FillRectangle = 0x36,
	SetFillColor = 0x37,
	SetFogColor = 0x38,
	SetBlendColor = 0x39,
	SetPrimColor = 0x3a,
	SetEnvColor = 0x3b,
	SetCombine = 0x3c,
	SetTextureImage = 0x3d,
	SetMaskImage = 0x3e,
	SetColorImage = 0x3f
};

enum class RGBMul : uint8_t
{
	Combined = 0,
	Texel0 = 1,
	Texel1 = 2,
	Primitive = 3,
	Shade = 4,
	Env = 5,
	KeyScale = 6,
	CombinedAlpha = 7,
	Texel0Alpha = 8,
	Texel1Alpha = 9,
	PrimitiveAlpha = 10,
	ShadeAlpha = 11,
	EnvAlpha = 12,
	LODFrac = 13,
	PrimLODFrac = 14,
	ConvertK5 = 15,
	Zero = 16
};

enum class RGBMulAdd : uint8_t
{
	Combined = 0,
	Texel0 = 1,
	Texel1 = 2,
	Primitive = 3,
	Shade = 4,
	Env = 5,
	One = 6,
	Noise = 7,
	Zero = 8
};

enum class RGBMulSub : uint8_t
{
	Combined = 0,
	Texel0 = 1,
	Texel1 = 2,
	Primitive = 3,
	Shade = 4,
	Env = 5,
	KeyCenter = 6,
	ConvertK4 = 7,
	Zero = 8
};

enum class RGBAdd : uint8_t
{
	Combined = 0,
	Texel0 = 1,
	Texel1 = 2,
	Primitive = 3,
	Shade = 4,
	Env = 5,
	One = 6,
	Zero = 7
};

enum class AlphaAddSub : uint8_t
{
	CombinedAlpha = 0,
	Texel0Alpha = 1,
	Texel1Alpha = 2,
	PrimitiveAlpha = 3,
	ShadeAlpha = 4,
	EnvAlpha = 5,
	One = 6,
	Zero = 7
};

enum class AlphaMul : uint8_t
{
	LODFrac = 0,
	Texel0Alpha = 1,
	Texel1Alpha = 2,
	PrimitiveAlpha = 3,
	ShadeAlpha = 4,
	EnvAlpha = 5,
	PrimLODFrac = 6,
	Zero = 7
};

enum class TextureSize : uint8_t
{
	Bpp4 = 0,
	Bpp8 = 1,
	Bpp16 = 2,
	Bpp32 = 3
};

enum class TextureFormat : uint8_t
{
	RGBA = 0,
	YUV = 1,
	CI = 2,
	IA = 3,
	I = 4
};

enum class RGBDitherMode : uint8_t
{
	Magic = 0,
	Bayer = 1,
	Noise = 2,
	Off = 3
};

enum class AlphaDitherMode : uint8_t
{
	Pattern = 0,
	InvPattern = 1,
	Noise = 2,
	Off = 3
};

enum class CycleType : uint8_t
{
	Cycle1 = 0,
	Cycle2 = 1,
	Copy = 2,
	Fill = 3
};

enum class BlendMode1A : uint8_t
{
	PixelColor = 0,
	MemoryColor = 1,
	BlendColor = 2,
	FogColor = 3
};

enum class BlendMode1B : uint8_t
{
	PixelAlpha = 0,
	FogAlpha = 1,
	ShadeAlpha = 2,
	Zero = 3
};

enum class BlendMode2A : uint8_t
{
	PixelColor = 0,
	MemoryColor = 1,
	BlendColor = 2,
	FogColor = 3
};

enum class BlendMode2B : uint8_t
{
	InvPixelAlpha = 0,
	MemoryAlpha = 1,
	One = 2,
	Zero = 3
};

enum class CoverageMode : uint8_t
{
	Clamp = 0,
	Wrap = 1,
	Zap = 2,
	Save = 3
};

enum class ZMode : uint8_t
{
	Opaque = 0,
	Interpenetrating = 1,
	Transparent = 2,
	Decal = 3
};

enum TileInfoFlagBits
{
	TILE_INFO_CLAMP_S_BIT = 1 << 0,
	TILE_INFO_MIRROR_S_BIT = 1 << 1,
	TILE_INFO_CLAMP_T_BIT = 1 << 2,
	TILE_INFO_MIRROR_T_BIT = 1 << 3
};
using TileInfoFlags = uint8_t;

struct TileSize
{
	uint32_t slo = 0;
	uint32_t shi = 0;
	uint32_t tlo = 0;
	uint32_t thi = 0;
};

struct TileMeta
{
	uint32_t offset = 0;
	uint32_t stride = 0;
	TextureFormat fmt = TextureFormat::RGBA;
	TextureSize size = TextureSize::Bpp16;
	uint8_t palette = 0;
	uint8_t mask_s = 0;
	uint8_t shift_s = 0;
	uint8_t mask_t = 0;
	uint8_t shift_t = 0;
	TileInfoFlags flags = 0;
};

struct TileInfo
{
	TileSize size;
	TileMeta meta;
};

struct CombinerInputsRGB
{
	RGBMulAdd muladd;
	RGBMulSub mulsub;
	RGBMul mul;
	RGBAdd add;
};

struct CombinerInputsAlpha
{
	AlphaAddSub muladd;
	AlphaAddSub mulsub;
	AlphaMul mul;
	AlphaAddSub add;
};

struct CombinerInputs
{
	CombinerInputsRGB rgb;
	CombinerInputsAlpha alpha;
};

struct BlendModes
{
	BlendMode1A blend_1a;
	BlendMode1B blend_1b;
	BlendMode2A blend_2a;
	BlendMode2B blend_2b;
};

static_assert(sizeof(TileInfo) == 32, "TileInfo must be 32 bytes.");

enum class VIRegister
{
	Control = 0,
	Origin,
	Width,
	Intr,
	VCurrentLine,
	Timing,
	VSync,
	HSync,
	Leap,
	HStart,
	VStart,
	VBurst,
	XScale,
	YScale,
	Count
};

enum VIControlFlagBits
{
	VI_CONTROL_TYPE_BLANK_BIT = 0 << 0,
	VI_CONTROL_TYPE_RESERVED_BIT = 1 << 0,
	VI_CONTROL_TYPE_RGBA5551_BIT = 2 << 0,
	VI_CONTROL_TYPE_RGBA8888_BIT = 3 << 0,
	VI_CONTROL_TYPE_MASK = 3 << 0,
	VI_CONTROL_GAMMA_DITHER_ENABLE_BIT = 1 << 2,
	VI_CONTROL_GAMMA_ENABLE_BIT = 1 << 3,
	VI_CONTROL_DIVOT_ENABLE_BIT = 1 << 4,
	VI_CONTROL_SERRATE_BIT = 1 << 6,
	VI_CONTROL_AA_MODE_RESAMP_EXTRA_ALWAYS_BIT = 0 << 8,
	VI_CONTROL_AA_MODE_RESAMP_EXTRA_BIT = 1 << 8,
	VI_CONTROL_AA_MODE_RESAMP_ONLY_BIT = 2 << 8,
	VI_CONTROL_AA_MODE_RESAMP_REPLICATE_BIT = 3 << 8,
	VI_CONTROL_AA_MODE_MASK = 3 << 8,
	VI_CONTROL_DITHER_FILTER_ENABLE_BIT = 1 << 16,
	VI_CONTROL_META_AA_BIT = 1 << 17,
	VI_CONTROL_META_SCALE_BIT = 1 << 18
};
using VIControlFlags = uint32_t;

static inline uint32_t make_vi_start_register(uint32_t start_value, uint32_t end_value)
{
	return ((start_value & 0x3ff) << 16) | (end_value & 0x3ff);
}

static inline uint32_t make_vi_scale_register(uint32_t scale_factor, uint32_t bias)
{
	return ((bias & 0xfff) << 16) | (scale_factor & 0xfff);
}

constexpr uint32_t VI_V_SYNC_NTSC = 525;
constexpr uint32_t VI_V_SYNC_PAL = 625;
constexpr uint32_t VI_H_OFFSET_NTSC = 108;
constexpr uint32_t VI_H_OFFSET_PAL = 128;
constexpr uint32_t VI_V_OFFSET_NTSC = 34;
constexpr uint32_t VI_V_OFFSET_PAL = 44;
constexpr uint32_t VI_V_RES_NTSC = 480;
constexpr uint32_t VI_V_RES_PAL = 576;
constexpr int VI_SCANOUT_WIDTH = 640;

static inline uint32_t make_default_v_start()
{
	return make_vi_start_register(VI_V_OFFSET_NTSC, VI_V_OFFSET_NTSC + 224 * 2);
}

static inline uint32_t make_default_h_start()
{
	return make_vi_start_register(VI_H_OFFSET_NTSC, VI_H_OFFSET_NTSC + VI_SCANOUT_WIDTH);
}

template <int bits>
static int32_t sext(int32_t v)
{
	struct { int32_t dummy : bits; } d;
	d.dummy = v;
	return d.dummy;
}
}