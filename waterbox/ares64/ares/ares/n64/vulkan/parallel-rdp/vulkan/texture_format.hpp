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
#include "small_vector.hpp"
#include <vector>
#include <stddef.h>
#include <assert.h>

namespace Vulkan
{
class TextureFormatLayout
{
public:
	void set_1d(VkFormat format, uint32_t width, uint32_t array_layers = 1, uint32_t mip_levels = 1);
	void set_2d(VkFormat format, uint32_t width, uint32_t height, uint32_t array_layers = 1, uint32_t mip_levels = 1);
	void set_3d(VkFormat format, uint32_t width, uint32_t height, uint32_t depth, uint32_t mip_levels = 1);

	static uint32_t format_block_size(VkFormat format, VkImageAspectFlags aspect);
	static void format_block_dim(VkFormat format, uint32_t &width, uint32_t &height);
	static uint32_t num_miplevels(uint32_t width, uint32_t height = 1, uint32_t depth = 1);

	void set_buffer(void *buffer, size_t size);
	inline void *get_buffer()
	{
		return buffer;
	}

	uint32_t get_width(uint32_t mip = 0) const;
	uint32_t get_height(uint32_t mip = 0) const;
	uint32_t get_depth(uint32_t mip = 0) const;
	uint32_t get_levels() const;
	uint32_t get_layers() const;
	uint32_t get_block_stride() const;
	uint32_t get_block_dim_x() const;
	uint32_t get_block_dim_y() const;
	VkImageType get_image_type() const;
	VkFormat get_format() const;

	size_t get_required_size() const;

	size_t row_byte_stride(uint32_t row_length) const;
	size_t layer_byte_stride(uint32_t row_length, size_t row_byte_stride) const;

	inline size_t get_row_size(uint32_t mip) const
	{
		return mips[mip].block_row_length * block_stride;
	}

	inline size_t get_layer_size(uint32_t mip) const
	{
		return mips[mip].block_image_height * get_row_size(mip);
	}

	struct MipInfo
	{
		size_t offset = 0;
		uint32_t width = 1;
		uint32_t height = 1;
		uint32_t depth = 1;

		uint32_t block_image_height = 0;
		uint32_t block_row_length = 0;
		uint32_t image_height = 0;
		uint32_t row_length = 0;
	};

	const MipInfo &get_mip_info(uint32_t mip) const;

	inline void *data(uint32_t layer = 0, uint32_t mip = 0) const
	{
		assert(buffer);
		assert(buffer_size == required_size);
		auto &mip_info = mips[mip];
		uint8_t *slice = buffer + mip_info.offset;
		slice += block_stride * layer * mip_info.block_row_length * mip_info.block_image_height;
		return slice;
	}

	template <typename T>
	inline T *data_generic(uint32_t x, uint32_t y, uint32_t slice_index, uint32_t mip = 0) const
	{
		auto &mip_info = mips[mip];
		T *slice = reinterpret_cast<T *>(buffer + mip_info.offset);
		slice += slice_index * mip_info.block_row_length * mip_info.block_image_height;
		slice += y * mip_info.block_row_length;
		slice += x;
		return slice;
	}

	inline void *data_opaque(uint32_t x, uint32_t y, uint32_t slice_index, uint32_t mip = 0) const
	{
		auto &mip_info = mips[mip];
		uint8_t *slice = buffer + mip_info.offset;
		size_t off = slice_index * mip_info.block_row_length * mip_info.block_image_height;
		off += y * mip_info.block_row_length;
		off += x;
		return slice + off * block_stride;
	}

	template <typename T>
	inline T *data_generic() const
	{
		return data_generic<T>(0, 0, 0, 0);
	}

	template <typename T>
	inline T *data_1d(uint32_t x, uint32_t layer = 0, uint32_t mip = 0) const
	{
		assert(sizeof(T) == block_stride);
		assert(buffer);
		assert(image_type == VK_IMAGE_TYPE_1D);
		assert(buffer_size == required_size);
		return data_generic<T>(x, 0, layer, mip);
	}

	template <typename T>
	inline T *data_2d(uint32_t x, uint32_t y, uint32_t layer = 0, uint32_t mip = 0) const
	{
		assert(sizeof(T) == block_stride);
		assert(buffer);
		assert(image_type == VK_IMAGE_TYPE_2D);
		assert(buffer_size == required_size);
		return data_generic<T>(x, y, layer, mip);
	}

	template <typename T>
	inline T *data_3d(uint32_t x, uint32_t y, uint32_t z, uint32_t mip = 0) const
	{
		assert(sizeof(T) == block_stride);
		assert(buffer);
		assert(image_type == VK_IMAGE_TYPE_3D);
		assert(buffer_size == required_size);
		return data_generic<T>(x, y, z, mip);
	}

	void build_buffer_image_copies(Util::SmallVector<VkBufferImageCopy, 32> &copies) const;

private:
	uint8_t *buffer = nullptr;
	size_t buffer_size = 0;

	VkImageType image_type = VK_IMAGE_TYPE_MAX_ENUM;
	VkFormat format = VK_FORMAT_UNDEFINED;
	size_t required_size = 0;

	uint32_t block_stride = 1;
	uint32_t mip_levels = 1;
	uint32_t array_layers = 1;
	uint32_t block_dim_x = 1;
	uint32_t block_dim_y = 1;

	MipInfo mips[16];

	void fill_mipinfo(uint32_t width, uint32_t height, uint32_t depth);
};
}
