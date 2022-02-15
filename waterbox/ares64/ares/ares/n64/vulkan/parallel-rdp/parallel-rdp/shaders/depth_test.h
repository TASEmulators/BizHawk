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

#ifndef DEPTH_TEST_H_
#define DEPTH_TEST_H_

#include "z_encode.h"

const int Z_MODE_OPAQUE = 0;
const int Z_MODE_INTERPENETRATING = 1;
const int Z_MODE_TRANSPARENT = 2;
const int Z_MODE_DECAL = 3;

int combine_dz(int dz)
{
	// Find largest POT which is <= dz.
	if (dz != 0)
		dz = 1 << findMSB(dz);
	return dz;
}

bool depth_test(int z, int dz, int dz_compressed,
                u16 current_depth, u8 current_dz,
                inout int coverage_count, int current_coverage_count,
                bool z_compare, int z_mode,
                bool force_blend, bool aa_enable,
                out bool blend_en, out bool coverage_wrap, out u8x2 blend_shift)
{
	bool depth_pass;

	if (z_compare)
	{
		int memory_z = z_decompress(current_depth);
		int memory_dz = dz_decompress(current_dz);
		int precision_factor = (int(current_depth) >> 11) & 0xf;
		bool coplanar = false;

		blend_shift.x = u8(clamp(dz_compressed - current_dz, 0, 4));
		blend_shift.y = u8(clamp(current_dz - dz_compressed, 0, 4));

		if (precision_factor < 3)
		{
			if (memory_dz != 0x8000)
				memory_dz = max(memory_dz << 1, 16 >> precision_factor);
			else
			{
				coplanar = true;
				memory_dz = 0xffff;
			}
		}

		int combined_dz = combine_dz(dz | memory_dz);
		int combined_dz_interpenetrate = combined_dz;
		combined_dz <<= 3;

		bool farther = coplanar || ((z + combined_dz) >= memory_z);
		bool overflow = (coverage_count + current_coverage_count) >= 8;

		blend_en = force_blend || (!overflow && aa_enable && farther);
		coverage_wrap = overflow;

		depth_pass = false;
		bool max_z = memory_z == 0x3ffff;
		bool front = z < memory_z;
		int z_closest_possible = z - combined_dz;
		bool nearer = coplanar || (z_closest_possible <= memory_z);

		switch (z_mode)
		{
		case Z_MODE_OPAQUE:
		{
			// The OPAQUE mode is normal less-than.
			// However, if z is sufficiently close enough to memory Z, we assume that we have the same surface
			// and we should simply increment coverage (blend_en).
			// If we overflow coverage, it is clear that we have a different surface, and here we should only
			// consider pure in-front test and overwrite coverage.
			depth_pass = max_z || (overflow ? front : nearer);
			break;
		}

		case Z_MODE_INTERPENETRATING:
		{
			// This one is ... interesting as it affects coverage.
			if (!front || !farther || !overflow)
			{
				// If there is no decal-like intersect, treat this as normal opaque mode.
				depth_pass = max_z || (overflow ? front : nearer);
			}
			else
			{
				// Modify coverage based on how far away current surface we are somehow?
				combined_dz_interpenetrate = dz_compress(combined_dz_interpenetrate & 0xffff);
				int cvg_coeff = ((memory_z >> combined_dz_interpenetrate) - (z >> combined_dz_interpenetrate)) & 0xf;
				coverage_count = min((cvg_coeff * coverage_count) >> 3, 8);
				depth_pass = true;
			}
			break;
		}

		case Z_MODE_TRANSPARENT:
		{
			depth_pass = front || max_z;
			break;
		}

		case Z_MODE_DECAL:
		{
			// Decals pass if |z - memory_z| <= max(dz, memory_dz).
			depth_pass = farther && nearer && !max_z;
			break;
		}
		}
	}
	else
	{
		blend_shift.x = u8(0);
		blend_shift.y = u8(min(0xf - dz_compressed, 4));

		bool overflow = (coverage_count + current_coverage_count) >= 8;
		blend_en = force_blend || (!overflow && aa_enable);
		coverage_wrap = overflow;
		depth_pass = true;
	}
	return depth_pass;
}

#endif