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

#ifndef TEXTURE_H_
#define TEXTURE_H_

#include "data_structures.h"

const int TEXTURE_FORMAT_RGBA = 0;
const int TEXTURE_FORMAT_YUV = 1;
const int TEXTURE_FORMAT_CI = 2;
const int TEXTURE_FORMAT_IA = 3;
const int TEXTURE_FORMAT_I = 4;

int texel_mask_s(TileInfo tile, int s)
{
	if (tile.mask_s != 0)
	{
		int mask = 1 << tile.mask_s;
		if ((tile.flags & TILE_INFO_MIRROR_S_BIT) != 0)
			s ^= max((s & mask) - 1, 0);
		s &= mask - 1;
	}

	return s;
}

ivec2 texel_mask_s_copy(TileInfo tile, int s)
{
	ivec2 multi_s = s + ivec2(0, 1);

	if (tile.mask_s != 0)
	{
		int mask = 1 << tile.mask_s;
		if ((tile.flags & TILE_INFO_MIRROR_S_BIT) != 0)
			multi_s ^= max((multi_s & mask) - 1, 0);
		multi_s &= mask - 1;
	}

	return multi_s;
}

int texel_mask_t(TileInfo tile, int t)
{
	if (tile.mask_t != 0)
	{
		int mask = 1 << tile.mask_t;
		if ((tile.flags & TILE_INFO_MIRROR_T_BIT) != 0)
			t ^= max((t & mask) - 1, 0);
		t &= mask - 1;
	}

	return t;
}

i16x4 convert_rgba16(uint word)
{
	uvec3 rgb = (uvec3(word) >> uvec3(11, 6, 1)) & 31u;
	rgb = (rgb << 3u) | (rgb >> 2u);
	uint alpha = (word & 1u) * 0xffu;
	return i16x4(rgb, alpha);
}

i16x4 convert_ia16(uint word)
{
	uint intensity = word >> 8;
	uint alpha = word & 0xff;
	return i16x4(intensity, intensity, intensity, alpha);
}

i16x4 sample_texel_rgba4(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x >> 1;
	byte_offset &= 0xfff;

	uint shift = (~st.x & 1) * 4;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	word = (word >> shift) & 0xf;
	word |= word << 4;
	return i16x4(word);
}

i16x4 sample_texel_ia4(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x >> 1;
	byte_offset &= 0xfff;

	uint shift = (~st.x & 1) * 4;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	word = (word >> shift) & 0xf;

	uint intensity = word & 0xe;
	intensity = (intensity << 4) | (intensity << 1) | (intensity >> 2);
	return i16x4(intensity, intensity, intensity, (word & 1) * 0xff);
}

i16x4 sample_texel_ci4(TileInfo tile, uint tmem_instance, uvec2 st, uint pal)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x >> 1;
	byte_offset &= 0xfff;

	uint shift = (~st.x & 1) * 4;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	word = (word >> shift) & 0xf;
	word |= pal << 4;
	return i16x4(word);
}

i16x4 sample_texel_ci4_tlut(TileInfo tile, uint tmem_instance, uvec2 st, uint pal, uint lut_offset, uint addr_xor, bool tlut_type)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x >> 1;
	byte_offset &= 0x7ff;

	uint shift = (~st.x & 1) * 4;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	word = (word >> shift) & 0xf;
	word |= pal << 4;

	uint lut_entry = (word << 2) + lut_offset;
	lut_entry ^= addr_xor;

	word = uint(tmem16.instances[tmem_instance].elems[0x400 | lut_entry]);
	return tlut_type ? convert_ia16(word) : convert_rgba16(word);
}

i16x4 sample_texel_ci8_tlut(TileInfo tile, uint tmem_instance, uvec2 st, uint lut_offset, uint addr_xor, bool tlut_type)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x;
	byte_offset &= 0x7ff;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	uint lut_entry = (word << 2) + lut_offset;
	lut_entry ^= addr_xor;

	word = uint(tmem16.instances[tmem_instance].elems[0x400 | lut_entry]);
	return tlut_type ? convert_ia16(word) : convert_rgba16(word);
}

i16x4 sample_texel_ci32(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x * 2;
	byte_offset &= 0xfff;

	uint index = byte_offset >> 1;
	index ^= (st.y & 1) << 1;
	index ^= 1;

	uint word = uint(tmem16.instances[tmem_instance].elems[index]);
	return i16x2(word >> 8, word & 0xff).xyxy;
}

i16x4 sample_texel_ci32_tlut(TileInfo tile, uint tmem_instance, uvec2 st, uint lut_offset, uint addr_xor, bool tlut_type)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x * 2;
	byte_offset &= 0x7ff;

	uint index = byte_offset >> 1;
	index ^= (st.y & 1) << 1;
	index ^= 1;

	uint word = uint(tmem16.instances[tmem_instance].elems[index]);
	uint lut_entry = ((word >> 6) & ~3) + lut_offset;
	lut_entry ^= addr_xor;
	word = uint(tmem16.instances[tmem_instance].elems[0x400 | lut_entry]);
	return tlut_type ? convert_ia16(word) : convert_rgba16(word);
}

i16x4 sample_texel_rgba8(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x;
	byte_offset &= 0xfff;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	return i16x4(word);
}

i16x4 sample_texel_ia8(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x;
	byte_offset &= 0xfff;

	uint index = byte_offset;
	index ^= (st.y & 1) << 2;
	index ^= 3;

	uint word = uint(tmem8.instances[tmem_instance].elems[index]);
	uint intensity = word >> 4;
	uint alpha = word & 0xf;
	alpha |= alpha << 4;
	intensity |= intensity << 4;
	return i16x4(intensity, intensity, intensity, alpha);
}

i16x4 sample_texel_yuv16(TileInfo tile, uint tmem_instance, uvec2 st, uint chroma_x)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	uint byte_offset_luma = byte_offset + st.x;
	byte_offset_luma &= 0x7ff;

	uint byte_offset_chroma = byte_offset + chroma_x * 2;
	byte_offset_chroma &= 0x7ff;

	uint index_luma = byte_offset_luma;
	index_luma ^= (st.y & 1) << 2;
	index_luma ^= 3;

	uint index_chroma = byte_offset_chroma >> 1;
	index_chroma ^= (st.y & 1) << 1;
	index_chroma ^= 1;

	u8 luma = u8(tmem8.instances[tmem_instance].elems[index_luma | 0x800]);
	u16 chroma = u16(tmem16.instances[tmem_instance].elems[index_chroma]);
	u8 u = u8((chroma >> U16_C(8)) & U16_C(0xff));
	u8 v = u8((chroma >> U16_C(0)) & U16_C(0xff));
	return i16x4(i16(u) - I16_C(0x80), i16(v) - I16_C(0x80), luma, luma);
}

i16x4 sample_texel_rgba16(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x * 2;
	byte_offset &= 0xfff;

	uint index = byte_offset >> 1;
	index ^= (st.y & 1) << 1;
	index ^= 1;

	uint word = uint(tmem16.instances[tmem_instance].elems[index]);
	return convert_rgba16(word);
}

i16x4 sample_texel_ia16(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x * 2;
	byte_offset &= 0xfff;

	uint index = byte_offset >> 1;
	index ^= (st.y & 1) << 1;
	index ^= 1;

	uint word = uint(tmem16.instances[tmem_instance].elems[index]);
	return convert_ia16(word);
}

i16x4 sample_texel_rgba32(TileInfo tile, uint tmem_instance, uvec2 st)
{
	uint byte_offset = tile.offset + tile.stride * st.y;
	byte_offset += st.x * 2;
	byte_offset &= 0x7ff;

	uint index = byte_offset >> 1;
	index ^= (st.y & 1) << 1;
	index ^= 1;

	uint lower_word = uint(tmem16.instances[tmem_instance].elems[index]);
	uint upper_word = uint(tmem16.instances[tmem_instance].elems[index | 0x400]);
	return i16x4(lower_word >> 8, lower_word & 0xff, upper_word >> 8, upper_word & 0xff);
}

int clamp_and_shift_coord(bool clamp_bit, int coord, int lo, int hi, int shift)
{
	// Clamp 17-bit coordinate to 16-bit coordinate here.
	coord = clamp(coord, -0x8000, 0x7fff);

	if (shift < 11)
		coord >>= shift;
	else
	{
		coord <<= (32 - shift);
		coord >>= 16;
	}

	if (clamp_bit)
	{
		bool clamp_hi = (coord >> 3) >= hi;
		if (clamp_hi)
			coord = (((hi >> 2) - (lo >> 2)) & 0x3ff) << 5;
		else
			coord = max(coord - (lo << 3), 0);
	}
	else
		coord -= lo << 3;

	return coord;
}

int shift_coord(int coord, int lo, int shift)
{
	// Clamp 17-bit coordinate to 16-bit coordinate here.
	coord = clamp(coord, -0x8000, 0x7fff);

	if (shift < 11)
		coord >>= shift;
	else
	{
		coord <<= (32 - shift);
		coord >>= 16;
	}
	coord -= lo << 3;
	return coord;
}

// The copy pipe reads 4x16 words.
int sample_texture_copy_word(TileInfo tile, uint tmem_instance, ivec2 st, int s_offset, bool tlut, bool tlut_type)
{
	// For non-16bpp TMEM, the lower 32-bits are sampled based on direct 16-bit fetches. There are no shifts applied.
	bool high_word = s_offset < 2;
	bool replicate_8bpp = high_word && tile.size != 2 && !tlut;
	int samp;

	int s_shamt = min(int(tile.size), 2);
	bool large_texel = int(tile.size) == 3;
	int idx_mask = (large_texel || tlut) ? 0x3ff : 0x7ff;

	if (replicate_8bpp)
	{
		// The high word of 8-bpp replication is special in the sense that we sample 8-bpp correctly.
		// Sample the two possible words.
		st.x += 2 * s_offset;
		ivec2 s = texel_mask_s_copy(tile, st.x);
		int t = texel_mask_t(tile, st.y);

		uint tbase = tile.offset + tile.stride * t;
		uvec2 nibble_offset = (tbase * 2 + (s << s_shamt)) & 0x1fffu;
		nibble_offset ^= (t & 1u) * 8u;
		uvec2 index = nibble_offset >> 2u;

		index &= idx_mask;
		int samp0 = int(tmem16.instances[tmem_instance].elems[index.x ^ 1]);
		int samp1 = int(tmem16.instances[tmem_instance].elems[index.y ^ 1]);

		if (tile.size == 1)
		{
			samp0 >>= 8 - 4 * int(nibble_offset.x & 2);
			samp1 >>= 8 - 4 * int(nibble_offset.y & 2);
			samp0 &= 0xff;
			samp1 &= 0xff;
		}
		else if (tile.size == 0)
		{
			samp0 >>= 12 - 4 * int(nibble_offset.x & 3u);
			samp1 >>= 12 - 4 * int(nibble_offset.y & 3u);
			samp0 = (samp0 & 0xf) * 0x11;
			samp1 = (samp1 & 0xf) * 0x11;
		}
		else
		{
			samp0 >>= 8;
			samp1 >>= 8;
		}

		samp = (samp0 << 8) | samp1;
	}
	else
	{
		st.x += s_offset;
		int s = texel_mask_s(tile, st.x);
		int t = texel_mask_t(tile, st.y);

		uint tbase = tile.offset + tile.stride * t;
		uint nibble_offset = (tbase * 2 + (s << s_shamt)) & 0x1fffu;
		nibble_offset ^= (t & 1u) * 8u;

		uint index = nibble_offset >> 2u;
		index &= idx_mask;
		samp = int(tmem16.instances[tmem_instance].elems[index ^ 1]);

		if (tlut)
		{
			if (tile.size == 0)
			{
				samp >>= 12 - 4 * (nibble_offset & 3);
				samp &= 0xf;
				samp |= tile.palette << 4;
				samp <<= 2;
				samp += s_offset;
			}
			else
			{
				samp >>= 8 - 4 * (nibble_offset & 2);
				samp &= 0xff;
				samp <<= 2;
				samp += s_offset;
			}
			samp = int(tmem16.instances[tmem_instance].elems[(samp | 0x400) ^ 1]);
		}
	}

	return samp;
}

int sample_texture_copy(TileInfo tile, uint tmem_instance, ivec2 st, int s_offset, bool tlut, bool tlut_type)
{
	st.x = shift_coord(st.x, int(tile.slo), int(tile.shift_s));
	st.y = shift_coord(st.y, int(tile.tlo), int(tile.shift_t));
	st >>= 5;

	int samp;
	if (global_constants.fb_info.fb_size == 0)
	{
		samp = 0;
	}
	else if (global_constants.fb_info.fb_size == 1)
	{
		samp = sample_texture_copy_word(tile, tmem_instance, st, s_offset >> 1, tlut, tlut_type);
		samp >>= 8 - 8 * (s_offset & 1);
		samp &= 0xff;
	}
	else
	{
		samp = sample_texture_copy_word(tile, tmem_instance, st, s_offset, tlut, tlut_type);
	}

	return samp;
}

i16x2 bilinear_3tap(i16x2 t00, i16x2 t10, i16x2 t01, i16x2 t11, ivec2 frac)
{
	int sum_frac = frac.x + frac.y;
	i16x2 t_base = sum_frac >= 32 ? t11 : t00;
	i16x2 flip_frac = i16x2(sum_frac >= 32 ? (32 - frac.yx) : frac);
	i16x2 accum = (t10 - t_base) * flip_frac.x;
	accum += (t01 - t_base) * flip_frac.y;
	accum += I16_C(0x10);
	accum >>= I16_C(5);
	accum += t_base;
	return accum;
}

i16x4 sample_texture(TileInfo tile, uint tmem_instance, ivec2 st, bool tlut, bool tlut_type, bool sample_quad, bool mid_texel, bool convert_one,
                     i16x4 prev_cycle)
{
	st.x = clamp_and_shift_coord((tile.flags & TILE_INFO_CLAMP_S_BIT) != 0, st.x, int(tile.slo), int(tile.shi), int(tile.shift_s));
	st.y = clamp_and_shift_coord((tile.flags & TILE_INFO_CLAMP_T_BIT) != 0, st.y, int(tile.tlo), int(tile.thi), int(tile.shift_t));

	ivec2 frac;
	if (sample_quad)
		frac = st & 31;
	else
		frac = ivec2(0);

	int sum_frac = frac.x + frac.y;
	st >>= 5;

	int s0 = texel_mask_s(tile, st.x);
	int t0 = texel_mask_t(tile, st.y);
	int s1 = texel_mask_s(tile, st.x + 1);
	int t1 = texel_mask_t(tile, st.y + 1);

	// Very specific weird logic going on with t0 and t1.
	int tdiff = max(t1 - t0, -255);
	t1 = (t0 & 0xff) + tdiff;
	t0 &= 0xff;

	i16x4 t_base, t10, t01, t11;

	mid_texel = all(bvec3(mid_texel, equal(frac, ivec2(0x10))));
	if (mid_texel)
		sum_frac = 0;

	bool yuv = tile.fmt == TEXTURE_FORMAT_YUV;
	ivec2 base_st = sum_frac >= 0x20 ? ivec2(s1, t1) : ivec2(s0, t0);

	if (tlut)
	{
		switch (int(tile.fmt))
		{
		case TEXTURE_FORMAT_RGBA:
		case TEXTURE_FORMAT_CI:
		case TEXTURE_FORMAT_IA:
		case TEXTURE_FORMAT_I:
		{
			// For TLUT, entries in the LUT are duplicated and we must make sure that we sample 3 different banks
			// when we look up the TLUT entry. In normal situations, this is irrelevant, but we're trying to be accurate here.
			bool upper = sum_frac >= 0x20;
			uint addr_xor = upper ? 2 : 1;

			switch (int(tile.size))
			{
			case 0:
				t_base = sample_texel_ci4_tlut(tile, tmem_instance, base_st, tile.palette, upper ? 3 : 0, addr_xor, tlut_type);
				if (sample_quad)
				{
					t10 = sample_texel_ci4_tlut(tile, tmem_instance, ivec2(s1, t0), tile.palette, 1, addr_xor,
					                            tlut_type);
					t01 = sample_texel_ci4_tlut(tile, tmem_instance, ivec2(s0, t1), tile.palette, 2, addr_xor,
					                            tlut_type);
				}
				if (mid_texel)
				{
					t11 = sample_texel_ci4_tlut(tile, tmem_instance, ivec2(s1, t1), tile.palette, 3, addr_xor,
					                            tlut_type);
				}
				break;

			case 1:
				t_base = sample_texel_ci8_tlut(tile, tmem_instance, base_st, upper ? 3 : 0, addr_xor, tlut_type);
				if (sample_quad)
				{
					t10 = sample_texel_ci8_tlut(tile, tmem_instance, ivec2(s1, t0), 1, addr_xor, tlut_type);
					t01 = sample_texel_ci8_tlut(tile, tmem_instance, ivec2(s0, t1), 2, addr_xor, tlut_type);
				}
				if (mid_texel)
					t11 = sample_texel_ci8_tlut(tile, tmem_instance, ivec2(s1, t1), 3, addr_xor, tlut_type);
				break;

			default:
				t_base = sample_texel_ci32_tlut(tile, tmem_instance, base_st, upper ? 3 : 0, addr_xor, tlut_type);
				if (sample_quad)
				{
					t10 = sample_texel_ci32_tlut(tile, tmem_instance, ivec2(s1, t0), 1, addr_xor, tlut_type);
					t01 = sample_texel_ci32_tlut(tile, tmem_instance, ivec2(s0, t1), 2, addr_xor, tlut_type);
				}
				if (mid_texel)
					t11 = sample_texel_ci32_tlut(tile, tmem_instance, ivec2(s1, t1), 3, addr_xor, tlut_type);
				break;
			}
			break;
		}
		}
	}
	else
	{
		switch (int(tile.fmt))
		{
		case TEXTURE_FORMAT_RGBA:
			switch (int(tile.size))
			{
			case 0:
				t_base = sample_texel_rgba4(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba4(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba4(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba4(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 1:
				t_base = sample_texel_rgba8(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba8(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 2:
				t_base = sample_texel_rgba16(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba16(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba16(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba16(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 3:
				t_base = sample_texel_rgba32(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba32(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba32(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba32(tile, tmem_instance, ivec2(s1, t1));
				break;
			}
			break;

		case TEXTURE_FORMAT_YUV:
		{
			uint chroma_x0 = s0 >> 1;
			uint chroma_x1 = (s1 + (s1 - s0)) >> 1;

			// Only implement 16bpp for now. It's the only one that gives meaningful results.
			t_base = sample_texel_yuv16(tile, tmem_instance, ivec2(s0, t0), chroma_x0);
			if (sample_quad)
			{
				t10 = sample_texel_yuv16(tile, tmem_instance, ivec2(s1, t0), chroma_x1);
				t01 = sample_texel_yuv16(tile, tmem_instance, ivec2(s0, t1), chroma_x0);
				t11 = sample_texel_yuv16(tile, tmem_instance, ivec2(s1, t1), chroma_x1);
			}
			break;
		}

		case TEXTURE_FORMAT_CI:
			switch (int(tile.size))
			{
			case 0:
				t_base = sample_texel_ci4(tile, tmem_instance, base_st, tile.palette);
				if (sample_quad)
				{
					t10 = sample_texel_ci4(tile, tmem_instance, ivec2(s1, t0), tile.palette);
					t01 = sample_texel_ci4(tile, tmem_instance, ivec2(s0, t1), tile.palette);
				}
				if (mid_texel)
					t11 = sample_texel_ci4(tile, tmem_instance, ivec2(s1, t1), tile.palette);
				break;

			case 1:
				t_base = sample_texel_rgba8(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba8(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t1));
				break;

			default:
				t_base = sample_texel_ci32(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ci32(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t1));
				break;
			}
			break;

		case TEXTURE_FORMAT_IA:
			switch (int(tile.size))
			{
			case 0:
				t_base = sample_texel_ia4(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ia4(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ia4(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ia4(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 1:
				t_base = sample_texel_ia8(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ia8(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ia8(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ia8(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 2:
				t_base = sample_texel_ia16(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ia16(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ia16(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ia16(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 3:
				t_base = sample_texel_ci32(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ci32(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t1));
				break;
			}
			break;

		case TEXTURE_FORMAT_I:
			switch (int(tile.size))
			{
			case 0:
				t_base = sample_texel_rgba4(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba4(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba4(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba4(tile, tmem_instance, ivec2(s1, t1));
				break;

			case 1:
				t_base = sample_texel_rgba8(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_rgba8(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_rgba8(tile, tmem_instance, ivec2(s1, t1));
				break;

			default:
				t_base = sample_texel_ci32(tile, tmem_instance, base_st);
				if (sample_quad)
				{
					t10 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t0));
					t01 = sample_texel_ci32(tile, tmem_instance, ivec2(s0, t1));
				}
				if (mid_texel)
					t11 = sample_texel_ci32(tile, tmem_instance, ivec2(s1, t1));
				break;
			}
			break;
		}
	}

	i16x4 accum;

	if (convert_one)
	{
		ivec4 prev_sext = bitfieldExtract(ivec4(prev_cycle), 0, 9);
		ivec2 factors = sum_frac >= 32 ? prev_sext.gr : prev_sext.rg;
		ivec4 converted = factors.r * (t10 - t_base) + factors.g * (t01 - t_base) + 0x80;
		converted >>= 8;
		converted += prev_sext.b;
		accum = i16x4(converted);
	}
	else if (yuv)
	{
		if (sample_quad)
		{
			int chroma_frac = ((s0 & 1) << 4) | (frac.x >> 1);
			i16x2 accum_chroma = bilinear_3tap(t_base.xy, t10.xy, t01.xy, t11.xy, ivec2(chroma_frac, frac.y));
			i16x2 accum_luma = bilinear_3tap(t_base.zw, t10.zw, t01.zw, t11.zw, frac);
			accum = i16x4(accum_chroma, accum_luma);
		}
		else
			accum = t_base;
	}
	else if (mid_texel)
	{
		accum = (t_base + t01 + t10 + t11 + I16_C(2)) >> I16_C(2);
	}
	else
	{
		i16x2 flip_frac = i16x2(sum_frac >= 32 ? (32 - frac.yx) : frac);
		accum = (t10 - t_base) * flip_frac.x;
		accum += (t01 - t_base) * flip_frac.y;
		accum += I16_C(0x10);
		accum >>= I16_C(5);
		accum += t_base;
	}
	return accum;
}

void compute_lod_2cycle(inout uint tile0, inout uint tile1, out i16 lod_frac, uint max_level, int min_lod,
                        ivec2 st, ivec2 st_dx, ivec2 st_dy,
                        bool perspective_overflow, bool tex_lod_en, bool sharpen_tex_en, bool detail_tex_en)
{
	bool magnify = false;
	bool distant = false;

	uint tile_offset = 0;

	if (perspective_overflow)
	{
		distant = true;
		lod_frac = i16(0xff);
	}
	else
	{
		ivec2 dx = st_dx - st;
		// Kinda abs, except it's 1 less than expected if negative.
		dx ^= dx >> 31;
		ivec2 dy = st_dy - st;
		// Kinda abs, except it's 1 less than expected if negative.
		dy ^= dy >> 31;

		ivec2 max_d2 = max(dx, dy);
		int max_d = max(max_d2.x, max_d2.y);

		if (max_d >= 0x4000)
		{
			distant = true;
			lod_frac = i16(0xff);
			tile_offset = max_level;
		}
		else if (max_d < 32) // LOD < 0
		{
			distant = max_level == 0u;
			magnify = true;

			if (!sharpen_tex_en && !detail_tex_en)
				lod_frac = i16(distant ? 0xff : 0);
			else
				lod_frac = i16((max(min_lod, max_d) << 3) + (sharpen_tex_en ? -0x100 : 0));
		}
		else
		{
			int mip_base = max(findMSB(max_d >> 5), 0);
			distant = mip_base >= max_level;

			if (distant && !sharpen_tex_en && !detail_tex_en)
			{
				lod_frac = i16(0xff);
			}
			else
			{
				lod_frac = i16(((max_d << 3) >> mip_base) & 0xff);
				tile_offset = mip_base;
			}
		}
	}

	if (tex_lod_en)
	{
		if (distant)
			tile_offset = max_level;

		if (!detail_tex_en)
		{
			tile0 = (tile0 + tile_offset) & 7u;
			if (distant || (!sharpen_tex_en && magnify))
				tile1 = tile0;
			else
				tile1 = (tile0 + 1) & 7;
		}
		else
		{
			tile1 = (tile0 + tile_offset + ((distant || magnify) ? 1 : 2)) & 7u;
			tile0 = (tile0 + tile_offset + (magnify ? 0 : 1)) & 7u;
		}
	}
}

i16x4 texture_convert_factors(i16x4 texel_in, i16x4 factors)
{
	ivec4 texel = bitfieldExtract(ivec4(texel_in), 0, 9);

	int r = texel.b + ((factors.x * texel.g + 0x80) >> 8);
	int g = texel.b + ((factors.y * texel.r + factors.z * texel.g + 0x80) >> 8);
	int b = texel.b + ((factors.w * texel.r + 0x80) >> 8);
	int a = texel.b;
	return i16x4(r, g, b, a);
}

#endif