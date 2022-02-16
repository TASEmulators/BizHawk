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

#ifndef COVERAGE_H_
#define COVERAGE_H_

#include "data_structures.h"

const int SUBPIXELS_LOG2 = 2;
const int SUBPIXELS = 1 << SUBPIXELS_LOG2;

u8 compute_coverage(u16x4 xleft, u16x4 xright, int x)
{
	u16x4 xshift = u16x4(0, 4, 2, 6) + (u16(x) << U16_C(3));
	bvec4 clip_lo_x01 = lessThan(xshift, xleft.xxyy);
	bvec4 clip_lo_x23 = lessThan(xshift, xleft.zzww);
	bvec4 clip_hi_x01 = greaterThanEqual(xshift, xright.xxyy);
	bvec4 clip_hi_x23 = greaterThanEqual(xshift, xright.zzww);

	u8x4 clip_x0 = u8x4(clip_lo_x01) | u8x4(clip_hi_x01);
	u8x4 clip_x1 = u8x4(clip_lo_x23) | u8x4(clip_hi_x23);
	u8x4 clip_x = clip_x0 * u8x4(1, 2, 4, 8) + clip_x1 * u8x4(16, 32, 64, 128);
	u8 clip_coverage = (clip_x.x | clip_x.y) | (clip_x.z | clip_x.w);
	return ~clip_coverage & U8_C(0xff);
}

const int COVERAGE_CLAMP = 0;
const int COVERAGE_WRAP = 1;
const int COVERAGE_ZAP = 2;
const int COVERAGE_SAVE = 3;

int blend_coverage(int coverage, int memory_coverage, bool blend_en, int mode)
{
	int res = 0;
	switch (mode)
	{
	case COVERAGE_CLAMP:
	{
		if (blend_en)
			res = min(7, memory_coverage + coverage); // image_read_en to read memory coverage, otherwise, it's 7.
		else
			res = (coverage - 1) & 7;
		break;
	}

	case COVERAGE_WRAP:
		res = (coverage + memory_coverage) & 7;
		break;

	case COVERAGE_ZAP:
		res = 7;
		break;

	case COVERAGE_SAVE:
		res = memory_coverage;
		break;
	}

	return res;
}

#endif