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

#include <string>

namespace Vulkan
{
static inline const char *layout_to_string(VkImageLayout layout)
{
	switch (layout)
	{
	case VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL:
		return "SHADER_READ_ONLY";
	case VK_IMAGE_LAYOUT_DEPTH_STENCIL_READ_ONLY_OPTIMAL:
		return "DS_READ_ONLY";
	case VK_IMAGE_LAYOUT_DEPTH_STENCIL_ATTACHMENT_OPTIMAL:
		return "DS";
	case VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL:
		return "COLOR";
	case VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL:
		return "TRANSFER_DST";
	case VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL:
		return "TRANSFER_SRC";
	case VK_IMAGE_LAYOUT_GENERAL:
		return "GENERAL";
	case VK_IMAGE_LAYOUT_PRESENT_SRC_KHR:
		return "PRESENT";
	default:
		return "UNDEFINED";
	}
}

static inline std::string access_flags_to_string(VkAccessFlags flags)
{
	std::string result;

	if (flags & VK_ACCESS_SHADER_READ_BIT)
		result += "SHADER_READ ";
	if (flags & VK_ACCESS_SHADER_WRITE_BIT)
		result += "SHADER_WRITE ";
	if (flags & VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_WRITE_BIT)
		result += "DS_WRITE ";
	if (flags & VK_ACCESS_DEPTH_STENCIL_ATTACHMENT_READ_BIT)
		result += "DS_READ ";
	if (flags & VK_ACCESS_COLOR_ATTACHMENT_READ_BIT)
		result += "COLOR_READ ";
	if (flags & VK_ACCESS_COLOR_ATTACHMENT_WRITE_BIT)
		result += "COLOR_WRITE ";
	if (flags & VK_ACCESS_INPUT_ATTACHMENT_READ_BIT)
		result += "INPUT_READ ";
	if (flags & VK_ACCESS_TRANSFER_WRITE_BIT)
		result += "TRANSFER_WRITE ";
	if (flags & VK_ACCESS_TRANSFER_READ_BIT)
		result += "TRANSFER_READ ";
	if (flags & VK_ACCESS_UNIFORM_READ_BIT)
		result += "UNIFORM_READ ";

	if (!result.empty())
		result.pop_back();
	else
		result = "NONE";

	return result;
}

static inline std::string stage_flags_to_string(VkPipelineStageFlags flags)
{
	std::string result;

	if (flags & VK_PIPELINE_STAGE_ALL_GRAPHICS_BIT)
		result += "GRAPHICS ";
	if (flags & (VK_PIPELINE_STAGE_EARLY_FRAGMENT_TESTS_BIT | VK_PIPELINE_STAGE_LATE_FRAGMENT_TESTS_BIT))
		result += "DEPTH ";
	if (flags & VK_PIPELINE_STAGE_COLOR_ATTACHMENT_OUTPUT_BIT)
		result += "COLOR ";
	if (flags & VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT)
		result += "FRAGMENT ";
	if (flags & VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT)
		result += "COMPUTE ";
	if (flags & VK_PIPELINE_STAGE_TRANSFER_BIT)
		result += "TRANSFER ";
	if (flags & (VK_PIPELINE_STAGE_VERTEX_INPUT_BIT | VK_PIPELINE_STAGE_VERTEX_SHADER_BIT | VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT))
		result += "VERTEX ";

	if (!result.empty())
		result.pop_back();
	else
		result = "NONE";

	return result;
}
}