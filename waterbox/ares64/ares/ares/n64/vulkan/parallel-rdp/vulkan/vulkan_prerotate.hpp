/* Copyright (c) 2017-2020 Hans-Kristian Arntzen
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

#include "vulkan_headers.hpp"

namespace Vulkan
{
// FIXME: Also consider that we might have to flip X or Y w.r.t. dimensions,
// but that only matters for partial rendering ...
static inline bool surface_transform_swaps_xy(VkSurfaceTransformFlagBitsKHR transform)
{
	return (transform & (
			VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_ROTATE_90_BIT_KHR |
			VK_SURFACE_TRANSFORM_HORIZONTAL_MIRROR_ROTATE_270_BIT_KHR |
			VK_SURFACE_TRANSFORM_ROTATE_90_BIT_KHR |
			VK_SURFACE_TRANSFORM_ROTATE_270_BIT_KHR)) != 0;
}

static inline void viewport_swap_xy(VkViewport &vp)
{
	std::swap(vp.x, vp.y);
	std::swap(vp.width, vp.height);
}

static inline void rect2d_swap_xy(VkRect2D &rect)
{
	std::swap(rect.offset.x, rect.offset.y);
	std::swap(rect.extent.width, rect.extent.height);
}

static inline void build_prerotate_matrix_2x2(VkSurfaceTransformFlagBitsKHR pre_rotate, float mat[4])
{
	// TODO: HORIZONTAL_MIRROR.
	switch (pre_rotate)
	{
	default:
		mat[0] = 1.0f;
		mat[1] = 0.0f;
		mat[2] = 0.0f;
		mat[3] = 1.0f;
		break;

	case VK_SURFACE_TRANSFORM_ROTATE_90_BIT_KHR:
		mat[0] = 0.0f;
		mat[1] = 1.0f;
		mat[2] = -1.0f;
		mat[3] = 0.0f;
		break;

	case VK_SURFACE_TRANSFORM_ROTATE_270_BIT_KHR:
		mat[0] = 0.0f;
		mat[1] = -1.0f;
		mat[2] = 1.0f;
		mat[3] = 0.0f;
		break;

	case VK_SURFACE_TRANSFORM_ROTATE_180_BIT_KHR:
		mat[0] = -1.0f;
		mat[1] = 0.0f;
		mat[2] = 0.0f;
		mat[3] = -1.0f;
		break;
	}
}
}
