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

#include <assert.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>
#include "rdp_common.hpp"

namespace RDP
{
enum TriangleSetupFlagBits
{
	TRIANGLE_SETUP_FLIP_BIT = 1 << 0,
	TRIANGLE_SETUP_DO_OFFSET_BIT = 1 << 1,
	TRIANGLE_SETUP_SKIP_XFRAC_BIT = 1 << 2,
	TRIANGLE_SETUP_INTERLACE_FIELD_BIT = 1 << 3,
	TRIANGLE_SETUP_INTERLACE_KEEP_ODD_BIT = 1 << 4,
	TRIANGLE_SETUP_DISABLE_UPSCALING_BIT = 1 << 5,
	TRIANGLE_SETUP_NATIVE_LOD_BIT = 1 << 6
};
using TriangleSetupFlags = uint8_t;

enum StaticRasterizationFlagBits
{
	RASTERIZATION_INTERLACE_FIELD_BIT = 1 << 0,
	RASTERIZATION_INTERLACE_KEEP_ODD_BIT = 1 << 1,
	RASTERIZATION_AA_BIT = 1 << 2,
	RASTERIZATION_PERSPECTIVE_CORRECT_BIT = 1 << 3,
	RASTERIZATION_TLUT_BIT = 1 << 4,
	RASTERIZATION_TLUT_TYPE_BIT = 1 << 5,
	RASTERIZATION_CVG_TIMES_ALPHA_BIT = 1 << 6,
	RASTERIZATION_ALPHA_CVG_SELECT_BIT = 1 << 7,
	RASTERIZATION_MULTI_CYCLE_BIT = 1 << 8,
	RASTERIZATION_TEX_LOD_ENABLE_BIT = 1 << 9,
	RASTERIZATION_SHARPEN_LOD_ENABLE_BIT = 1 << 10,
	RASTERIZATION_DETAIL_LOD_ENABLE_BIT = 1 << 11,
	RASTERIZATION_FILL_BIT = 1 << 12,
	RASTERIZATION_COPY_BIT = 1 << 13,
	RASTERIZATION_SAMPLE_MODE_BIT = 1 << 14,
	RASTERIZATION_ALPHA_TEST_BIT = 1 << 15,
	RASTERIZATION_ALPHA_TEST_DITHER_BIT = 1 << 16,
	RASTERIZATION_SAMPLE_MID_TEXEL_BIT = 1 << 17,
	RASTERIZATION_USES_TEXEL0_BIT = 1 << 18,
	RASTERIZATION_USES_TEXEL1_BIT = 1 << 19,
	RASTERIZATION_USES_LOD_BIT = 1 << 20,
	RASTERIZATION_USES_PIPELINED_TEXEL1_BIT = 1 << 21,
	RASTERIZATION_CONVERT_ONE_BIT = 1 << 22,
	RASTERIZATION_BILERP_0_BIT = 1 << 23,
	RASTERIZATION_BILERP_1_BIT = 1 << 24,
	RASTERIZATION_UPSCALING_LOG2_BIT_OFFSET = 26,
	RASTERIZATION_NEED_NOISE_BIT = 1 << 28,
	RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT = 1 << 29,
	RASTERIZATION_USE_SPECIALIZATION_CONSTANT_BIT = 1 << 30
};
using StaticRasterizationFlags = uint32_t;

enum DepthBlendFlagBits
{
	DEPTH_BLEND_DEPTH_TEST_BIT = 1 << 0,
	DEPTH_BLEND_DEPTH_UPDATE_BIT = 1 << 1,
	DEPTH_BLEND_FORCE_BLEND_BIT = 1 << 3,
	DEPTH_BLEND_IMAGE_READ_ENABLE_BIT = 1 << 4,
	DEPTH_BLEND_COLOR_ON_COVERAGE_BIT = 1 << 5,
	DEPTH_BLEND_MULTI_CYCLE_BIT = 1 << 6,
	DEPTH_BLEND_AA_BIT = 1 << 7,
	DEPTH_BLEND_DITHER_ENABLE_BIT = 1 << 8
};
using DepthBlendFlags = uint32_t;

struct TriangleSetup
{
	int32_t xh, xm, xl;
	int16_t yh, ym;

	int32_t dxhdy, dxmdy, dxldy;
	int16_t yl;
	TriangleSetupFlags flags;
	uint8_t tile;
};

struct AttributeSetup
{
	int32_t r, g, b, a;
	int32_t drdx, dgdx, dbdx, dadx;
	int32_t drde, dgde, dbde, dade;
	int32_t drdy, dgdy, dbdy, dady;

	int32_t s, t, z, w;
	int32_t dsdx, dtdx, dzdx, dwdx;
	int32_t dsde, dtde, dzde, dwde;
	int32_t dsdy, dtdy, dzdy, dwdy;
};

struct ConstantCombinerInputs
{
	uint8_t muladd[4];
	uint8_t mulsub[4];
	uint8_t mul[4];
	uint8_t add[4];
};

// Per-primitive state which is very dynamic in nature and does not change anything about the shader itself.
struct DerivedSetup
{
	ConstantCombinerInputs constants[2];
	uint8_t fog_color[4];
	uint8_t blend_color[4];
	uint32_t fill_color;
	uint16_t dz;
	uint8_t dz_compressed;
	uint8_t min_lod;
	int16_t convert_factors[4];
};

static_assert((sizeof(TriangleSetup) & 15) == 0, "TriangleSetup must be aligned to 16 bytes.");
static_assert((sizeof(AttributeSetup) & 15) == 0, "AttributeSetup must be aligned to 16 bytes.");
static_assert(sizeof(DerivedSetup) == 56, "DerivedSetup is not 56 bytes.");

struct ScissorState
{
	uint32_t xlo;
	uint32_t ylo;
	uint32_t xhi;
	uint32_t yhi;
};

struct StaticRasterizationState
{
	CombinerInputs combiner[2];
	StaticRasterizationFlags flags;
	uint32_t dither;
	uint32_t texture_size;
	uint32_t texture_fmt;
};
static_assert(sizeof(StaticRasterizationState) == 32, "StaticRasterizationState must be 32 bytes.");

struct DepthBlendState
{
	BlendModes blend_cycles[2];
	DepthBlendFlags flags;
	CoverageMode coverage_mode;
	ZMode z_mode;
	uint8_t padding[2];
};
static_assert(sizeof(DepthBlendState) == 16, "DepthBlendState must be 16 bytes.");

struct InstanceIndices
{
	uint8_t static_index;
	uint8_t depth_blend_index;
	uint8_t tile_instance_index;
	uint8_t padding[5];
	uint8_t tile_indices[8];
};
static_assert((sizeof(InstanceIndices) & 15) == 0, "InstanceIndices must be aligned to 16 bytes.");

struct UploadInfo
{
	int32_t width, height;
	float min_t_mod, max_t_mod;

	int32_t vram_addr;
	int32_t vram_width;
	int32_t vram_size;
	int32_t vram_effective_width;

	int32_t tmem_offset;
	int32_t tmem_stride_words;
	int32_t tmem_size;
	int32_t tmem_fmt;

	int32_t mode;
	float inv_tmem_stride_words;
	int32_t dxt;
	int32_t padding;
};
static_assert((sizeof(UploadInfo) & 15) == 0, "UploadInfo must be aligned to 16 bytes.");

struct SpanSetup
{
	int32_t r, g, b, a;
	int32_t s, t, w, z;

	int16_t xlo[4];
	int16_t xhi[4];

	int32_t interpolation_base_x;
	int32_t start_x;
	int32_t end_x;
	int16_t lodlength;
	uint16_t valid_line;
};
static_assert((sizeof(SpanSetup) & 15) == 0, "SpanSetup is not aligned to 16 bytes.");

struct SpanInfoOffsets
{
	int32_t offset, ylo, yhi, padding;
};
static_assert((sizeof(SpanInfoOffsets) == 16), "SpanInfoOffsets is not 16 bytes.");

struct SpanInterpolationJob
{
	uint16_t primitive_index, base_y, max_y, padding;
};
static_assert((sizeof(SpanInterpolationJob) == 8), "SpanInterpolationJob is not 8 bytes.");

struct GlobalState
{
	uint32_t addr_index;
	uint32_t depth_addr_index;
	uint32_t fb_width, fb_height;
	uint32_t group_mask;
};

struct TileRasterWork
{
	uint32_t tile_x, tile_y;
	uint32_t tile_instance;
	uint32_t primitive;
};
static_assert((sizeof(TileRasterWork) == 16), "TileRasterWork is not 16 bytes.");

struct GlobalFBInfo
{
	uint32_t dx_shift;
	uint32_t dx_mask;
	uint32_t fb_size;
	uint32_t base_primitive_index;
};

template <typename T, unsigned N>
class StateCache
{
public:
	unsigned add(const T &t)
	{
		if (cached_index >= 0)
			if (memcmp(&elements[cached_index], &t, sizeof(T)) == 0)
				return unsigned(cached_index);

		for (int i = int(count) - 1; i >= 0; i--)
		{
			if (memcmp(&elements[i], &t, sizeof(T)) == 0)
			{
				cached_index = i;
				return unsigned(i);
			}
		}

		assert(count < N);
		memcpy(elements + count, &t, sizeof(T));
		unsigned ret = count++;
		cached_index = int(ret);
		return ret;
	}

	bool full() const
	{
		return count == N;
	}

	unsigned size() const
	{
		return count;
	}

	unsigned byte_size() const
	{
		return size() * sizeof(T);
	}

	const T *data() const
	{
		return elements;
	}

	void reset()
	{
		count = 0;
		cached_index = -1;
	}

	bool empty() const
	{
		return count == 0;
	}

private:
	unsigned count = 0;
	int cached_index = -1;
	T elements[N];
};

template <typename T, unsigned N>
class StreamCache
{
public:
	void add(const T &t)
	{
		assert(count < N);
		memcpy(&elements[count++], &t, sizeof(T));
	}

	bool full() const
	{
		return count == N;
	}

	unsigned size() const
	{
		return count;
	}

	unsigned byte_size() const
	{
		return size() * sizeof(T);
	}

	const T *data() const
	{
		return elements;
	}

	void reset()
	{
		count = 0;
	}

	bool empty() const
	{
		return count == 0;
	}

private:
	unsigned count = 0;
	T elements[N];
};

namespace Limits
{
constexpr unsigned MaxPrimitives = 256;
constexpr unsigned MaxStaticRasterizationStates = 64;
constexpr unsigned MaxDepthBlendStates = 64;
constexpr unsigned MaxTileInfoStates = 256;
constexpr unsigned NumSyncStates = 32;
constexpr unsigned MaxNumTiles = 8;
constexpr unsigned MaxTMEMInstances = 256;
constexpr unsigned MaxSpanSetups = 32 * 1024;
constexpr unsigned MaxWidth = 1024;
constexpr unsigned MaxHeight = 1024;
constexpr unsigned MaxTileInstances = 0x8000;
}

namespace ImplementationConstants
{
constexpr unsigned DefaultWorkgroupSize = 64;

constexpr unsigned TileWidth = 8;
constexpr unsigned TileHeight = 8;
constexpr unsigned MaxTilesX = Limits::MaxWidth / TileWidth;
constexpr unsigned MaxTilesY = Limits::MaxHeight / TileHeight;
constexpr unsigned IncoherentPageSize = 1024;
constexpr unsigned MaxPendingRenderPassesBeforeFlush = 8;
constexpr unsigned MinimumPrimitivesForIdleFlush = 32;
constexpr unsigned MinimumRenderPassesForIdleFlush = 2;
}
}
