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

#ifndef INTERPOLATION_H_
#define INTERPOLATION_H_

#include "data_structures.h"
#include "clamping.h"
#include "perspective.h"

u8x4 interpolate_rgba(ivec4 rgba, ivec4 drgba_dx, ivec4 drgba_dy, int dx, int coverage)
{
	rgba += ((drgba_dx & ~0x1f) >> SCALING_LOG2) * dx;

	// RGBA is interpolated to 9-bit. The last bit is used to deal with clamping.
	// Slight underflow below 0 is clamped to 0 and slight overflow above 0xff is clamped to 0xff.

	// Keep 2 sign bits of precision before we complete the centroid interpolation.
	i16x4 snapped_rgba = i16x4(rgba >> 14);

	// Centroid clipping is based on the first coverage bit, and we interpolate at the first subpixel in scanline order.
	// With this layout we can just use findLSB to get correct result.
	// 0x01        0x02
	//       0x04        0x08
	// 0x10        0x20
	//       0x40        0x80
	int first_coverage = findLSB(coverage);
	i16 yoff = i16(first_coverage >> 1);
	i16 xoff = i16((first_coverage & 1) << 1) + (yoff & I16_C(1));
	snapped_rgba <<= I16_C(2 + SCALING_LOG2);
	snapped_rgba += xoff * i16x4(drgba_dx >> 14) + yoff * i16x4(drgba_dy >> 14);
	snapped_rgba >>= I16_C(4 + SCALING_LOG2);
	return clamp_9bit(snapped_rgba);
}

void interpolate_st_copy(SpanSetup span, ivec4 dstzw_dx, int x, bool perspective, bool flip,
                         out ivec2 st, out int s_offset)
{
	int dx = flip ? (x - span.start_x) : (span.end_x - x);

	// For copy pipe, we should duplicate pixels when scaling, there is no filtering we can (or should!) do.
	dx >>= SCALING_LOG2;

	// Snap DX to where we perform interpolation (once per N output pixels).
	int snapped_dx = dx & global_constants.fb_info.dx_mask;
	s_offset = dx - snapped_dx;
	int lerp_dx = (dx >> global_constants.fb_info.dx_shift) * (flip ? 1 : -1);
	ivec3 stw = span.stzw.xyw + (dstzw_dx.xyw & ~0x1f) * lerp_dx;

	if (perspective)
	{
		bool st_overflow;
		st = perspective_divide(stw >> 16, st_overflow);
	}
	else
		st = no_perspective_divide(stw >> 16);
}

ivec2 interpolate_st_single(ivec4 stzw, ivec4 dstzw_dx, int dx, bool perspective)
{
	ivec3 stw = stzw.xyw + ((dstzw_dx.xyw & ~0x1f) >> SCALING_LOG2) * dx;
	stw >>= 16;
	ivec2 st;

	if (perspective)
	{
		bool st_overflow;
		st = perspective_divide(stw, st_overflow);
	}
	else
		st = no_perspective_divide(stw);

	return st;
}

void interpolate_stz(ivec4 stzw, ivec4 dstzw_dx, ivec4 dstzw_dy, int dx, int coverage, bool perspective, bool uses_lod,
                     int flip_direction, out ivec2 st, out ivec2 st_dx, out ivec2 st_dy, out int z, inout bool st_overflow)
{
	ivec3 stw = stzw.xyw + ((dstzw_dx.xyw & ~0x1f) >> SCALING_LOG2) * dx;
	ivec3 stw_dx, stw_dy;

	if (uses_lod)
	{
		stw_dx = stw + flip_direction * ((dstzw_dx.xyw & ~0x1f) >> SCALING_LOG2);
		if (SCALING_FACTOR > 1)
			stw_dy = stw + abs(flip_direction) * ((dstzw_dy.xyw & ~0x7fff) >> SCALING_LOG2);
		else
			stw_dy = stw + ((dstzw_dy.xyw & ~0x7fff) >> SCALING_LOG2);
	}

	if (perspective)
	{
		st = perspective_divide(stw >> 16, st_overflow);
		if (uses_lod)
		{
			st_dx = perspective_divide(stw_dx >> 16, st_overflow);
			st_dy = perspective_divide(stw_dy >> 16, st_overflow);
		}
	}
	else
	{
		st = no_perspective_divide(stw >> 16);
		if (uses_lod)
		{
			st_dx = no_perspective_divide(stw_dx >> 16);
			st_dy = no_perspective_divide(stw_dy >> 16);
		}
	}

	// Ensure that interpolation snaps as we expect on every "main" pixel,
	// for subpixels, interpolate with quantized step factor.
	z = stzw.z + dstzw_dx.z * (dx >> SCALING_LOG2) + (dstzw_dx.z >> SCALING_LOG2) * (dx & (SCALING_FACTOR - 1));

	int snapped_z = z >> 10;
	int first_coverage = findLSB(coverage);
	int yoff = first_coverage >> 1;
	int xoff = ((first_coverage & 1) << 1) + (yoff & I16_C(1));
	snapped_z <<= 2 + SCALING_LOG2;
	snapped_z += xoff * (dstzw_dx.z >> 10) + yoff * (dstzw_dy.z >> 10);
	snapped_z >>= 5 + SCALING_LOG2;

	z = clamp_z(snapped_z);
}

#if 0
u8x4 interpolate_rgba(TriangleSetup setup, AttributeSetup attr, int x, int y, int coverage)
{
	bool do_offset = (setup.flags & TRIANGLE_SETUP_DO_OFFSET_BIT) != 0;
	int y_interpolation_base = int(setup.yh) >> 2;
	int xh = setup.xh + (y - y_interpolation_base) * (setup.dxhdy << 2);

	ivec4 drgba_diff = ivec4(0);

	// In do_offset mode, varyings are latched at last subpixel line instead of first (for some reason).
	if (do_offset)
	{
		xh += 3 * setup.dxhdy;
		ivec4 drgba_deh = attr.drgba_de & ~0x1ff;
		ivec4 drgba_dyh = attr.drgba_dy & ~0x1ff;
		drgba_diff = drgba_deh - (drgba_deh >> 2) - drgba_dyh + (drgba_dyh >> 2);
	}

	int base_x = xh >> 16;
	int xfrac = (xh >> 8) & 0xff;

	ivec4 rgba = attr.rgba;
	rgba += attr.drgba_de * (y - y_interpolation_base);
	rgba = ((rgba & ~0x1ff) + drgba_diff - xfrac * ((attr.drgba_dx >> 8) & ~1)) & ~0x3ff;
	rgba += (attr.drgba_dx & ~0x1f) * (x - base_x);

	// RGBA is interpolated to 9-bit. The last bit is used to deal with clamping.
	// Slight underflow below 0 is clamped to 0 and slight overflow above 0xff is clamped to 0xff.

	// Keep 2 sign bits of precision before we complete the centroid interpolation.
	i16x4 snapped_rgba = i16x4(rgba >> 14);

	// Centroid clipping is based on the first coverage bit, and we interpolate at the first subpixel in scanline order.
	// FWIW, Angrylion has a very different coverage bit assignment, but we need this layout to avoid an awkward LUT.
	// With this layout we can just use findLSB instead.
	// 0x01        0x02
	//       0x04        0x08
	// 0x10        0x20
	//       0x40        0x80
	int first_coverage = findLSB(coverage);
	i16 yoff = i16(first_coverage >> 1);
	i16 xoff = i16((first_coverage & 1) << 1) + (yoff & I16_C(1));
	snapped_rgba <<= I16_C(2);
	snapped_rgba += xoff * i16x4(attr.drgba_dx >> 14) + yoff * i16x4(attr.drgba_dy >> 14);
	snapped_rgba >>= I16_C(4);
	return clamp_9bit(snapped_rgba);
}

ivec3 interpolate_stw(TriangleSetup setup, AttributeSetup attr, int x, int y)
{
	bool do_offset = (setup.flags & TRIANGLE_SETUP_DO_OFFSET_BIT) != 0;
	int y_interpolation_base = int(setup.yh) >> 2;
	int xh = setup.xh + (y - y_interpolation_base) * (setup.dxhdy << 2);

	ivec3 dstw_diff = ivec3(0);

	// In do_offset mode, varyings are latched at last subpixel line instead of first (for some reason).
	if (do_offset)
	{
		xh += 3 * setup.dxhdy;
		ivec3 dstw_deh = attr.dstzw_de.xyw & ~0x1ff;
		ivec3 dstw_dyh = attr.dstzw_dy.xyw & ~0x1ff;
		dstw_diff = dstw_deh - (dstw_deh >> 2) - dstw_dyh + (dstw_dyh >> 2);
	}

	int base_x = xh >> 16;
	int xfrac = (xh >> 8) & 0xff;

	ivec3 stw = attr.stzw.xyw;
	stw += attr.dstzw_de.xyw * (y - y_interpolation_base);
	stw = ((stw & ~0x1ff) + dstw_diff - xfrac * ((attr.dstzw_dx.xyw >> 8) & ~1)) & ~0x3ff;
	stw += (attr.dstzw_dx.xyw & ~0x1f) * (x - base_x);

	ivec3 snapped_stw = stw >> 16;
	return snapped_stw;
}

int interpolate_z(TriangleSetup setup, AttributeSetup attr, int x, int y, int coverage)
{
	bool do_offset = (setup.flags & TRIANGLE_SETUP_DO_OFFSET_BIT) != 0;
	int y_interpolation_base = int(setup.yh) >> 2;
	int xh = setup.xh + (y - y_interpolation_base) * (setup.dxhdy << 2);

	int dzdiff = 0;
	// In do_offset mode, varyings are latched at last subpixel line instead of first (for some reason).
	if (do_offset)
	{
		xh += 3 * setup.dxhdy;
		int dzdeh = attr.dstzw_de.z & ~0x1ff;
		int dzdyh = attr.dstzw_dy.z & ~0x1ff;
		dzdiff = dzdeh - (dzdeh >> 2) - dzdyh + (dzdyh >> 2);
	}

	int base_x = xh >> 16;
	int xfrac = (xh >> 8) & 0xff;
	int z = attr.stzw.z;
	z += attr.dstzw_de.z * (y - y_interpolation_base);
	z = ((z & ~0x1ff) + dzdiff - xfrac * ((attr.dstzw_dx.z >> 8) & ~1)) & ~0x3ff;
	z += attr.dstzw_dx.z * (x - base_x);

	int snapped_z = z >> 10;
	int first_coverage = findLSB(coverage);
	int yoff = first_coverage >> 1;
	int xoff = ((first_coverage & 1) << 1) + (yoff & 1s);
	snapped_z <<= 2;
	snapped_z += xoff * (attr.dstzw_dx.z >> 10) + yoff * (attr.dstzw_dy.z >> 10);
	snapped_z >>= 5;
	return clamp_z(snapped_z);
}
#endif

#endif