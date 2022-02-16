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

#include "texture_format.hpp"
#include "format.hpp"
#include <algorithm>

using namespace std;

namespace Vulkan
{
uint32_t TextureFormatLayout::num_miplevels(uint32_t width, uint32_t height, uint32_t depth)
{
	uint32_t size = unsigned(max(max(width, height), depth));
	uint32_t levels = 0;
	while (size)
	{
		levels++;
		size >>= 1;
	}
	return levels;
}

void TextureFormatLayout::format_block_dim(VkFormat format, uint32_t &width, uint32_t &height)
{
#define fmt(x, w, h)     \
    case VK_FORMAT_##x: \
        width = w; \
        height = h; \
        break

	switch (format)
	{
	fmt(ETC2_R8G8B8A8_UNORM_BLOCK, 4, 4);
	fmt(ETC2_R8G8B8A8_SRGB_BLOCK, 4, 4);
	fmt(ETC2_R8G8B8A1_UNORM_BLOCK, 4, 4);
	fmt(ETC2_R8G8B8A1_SRGB_BLOCK, 4, 4);
	fmt(ETC2_R8G8B8_UNORM_BLOCK, 4, 4);
	fmt(ETC2_R8G8B8_SRGB_BLOCK, 4, 4);
	fmt(EAC_R11_UNORM_BLOCK, 4, 4);
	fmt(EAC_R11_SNORM_BLOCK, 4, 4);
	fmt(EAC_R11G11_UNORM_BLOCK, 4, 4);
	fmt(EAC_R11G11_SNORM_BLOCK, 4, 4);

	fmt(BC1_RGB_UNORM_BLOCK, 4, 4);
	fmt(BC1_RGB_SRGB_BLOCK, 4, 4);
	fmt(BC1_RGBA_UNORM_BLOCK, 4, 4);
	fmt(BC1_RGBA_SRGB_BLOCK, 4, 4);
	fmt(BC2_UNORM_BLOCK, 4, 4);
	fmt(BC2_SRGB_BLOCK, 4, 4);
	fmt(BC3_UNORM_BLOCK, 4, 4);
	fmt(BC3_SRGB_BLOCK, 4, 4);
	fmt(BC4_UNORM_BLOCK, 4, 4);
	fmt(BC4_SNORM_BLOCK, 4, 4);
	fmt(BC5_UNORM_BLOCK, 4, 4);
	fmt(BC5_SNORM_BLOCK, 4, 4);
	fmt(BC6H_UFLOAT_BLOCK, 4, 4);
	fmt(BC6H_SFLOAT_BLOCK, 4, 4);
	fmt(BC7_SRGB_BLOCK, 4, 4);
	fmt(BC7_UNORM_BLOCK, 4, 4);

#define astc_fmt(w, h) \
    fmt(ASTC_##w##x##h##_UNORM_BLOCK, w, h); \
    fmt(ASTC_##w##x##h##_SRGB_BLOCK, w, h); \
    fmt(ASTC_##w##x##h##_SFLOAT_BLOCK_EXT, w, h)

	astc_fmt(4, 4);
	astc_fmt(5, 4);
	astc_fmt(5, 5);
	astc_fmt(6, 5);
	astc_fmt(6, 6);
	astc_fmt(8, 5);
	astc_fmt(8, 6);
	astc_fmt(8, 8);
	astc_fmt(10, 5);
	astc_fmt(10, 6);
	astc_fmt(10, 8);
	astc_fmt(10, 10);
	astc_fmt(12, 10);
	astc_fmt(12, 12);

	default:
		width = 1;
		height = 1;
		break;
	}

#undef fmt
#undef astc_fmt
}

uint32_t TextureFormatLayout::format_block_size(VkFormat format, VkImageAspectFlags aspect)
{
#define fmt(x, bpp)     \
    case VK_FORMAT_##x: \
        return bpp

#define fmt2(x, bpp0, bpp1) \
	case VK_FORMAT_##x:     \
		return aspect == VK_IMAGE_ASPECT_PLANE_0_BIT ? bpp0 : bpp1

	switch (format)
	{
	fmt(R4G4_UNORM_PACK8, 1);
	fmt(R4G4B4A4_UNORM_PACK16, 2);
	fmt(B4G4R4A4_UNORM_PACK16, 2);
	fmt(R5G6B5_UNORM_PACK16, 2);
	fmt(B5G6R5_UNORM_PACK16, 2);
	fmt(R5G5B5A1_UNORM_PACK16, 2);
	fmt(B5G5R5A1_UNORM_PACK16, 2);
	fmt(A1R5G5B5_UNORM_PACK16, 2);
	fmt(R8_UNORM, 1);
	fmt(R8_SNORM, 1);
	fmt(R8_USCALED, 1);
	fmt(R8_SSCALED, 1);
	fmt(R8_UINT, 1);
	fmt(R8_SINT, 1);
	fmt(R8_SRGB, 1);
	fmt(R8G8_UNORM, 2);
	fmt(R8G8_SNORM, 2);
	fmt(R8G8_USCALED, 2);
	fmt(R8G8_SSCALED, 2);
	fmt(R8G8_UINT, 2);
	fmt(R8G8_SINT, 2);
	fmt(R8G8_SRGB, 2);
	fmt(R8G8B8_UNORM, 3);
	fmt(R8G8B8_SNORM, 3);
	fmt(R8G8B8_USCALED, 3);
	fmt(R8G8B8_SSCALED, 3);
	fmt(R8G8B8_UINT, 3);
	fmt(R8G8B8_SINT, 3);
	fmt(R8G8B8_SRGB, 3);
	fmt(R8G8B8A8_UNORM, 4);
	fmt(R8G8B8A8_SNORM, 4);
	fmt(R8G8B8A8_USCALED, 4);
	fmt(R8G8B8A8_SSCALED, 4);
	fmt(R8G8B8A8_UINT, 4);
	fmt(R8G8B8A8_SINT, 4);
	fmt(R8G8B8A8_SRGB, 4);
	fmt(B8G8R8A8_UNORM, 4);
	fmt(B8G8R8A8_SNORM, 4);
	fmt(B8G8R8A8_USCALED, 4);
	fmt(B8G8R8A8_SSCALED, 4);
	fmt(B8G8R8A8_UINT, 4);
	fmt(B8G8R8A8_SINT, 4);
	fmt(B8G8R8A8_SRGB, 4);
	fmt(A8B8G8R8_UNORM_PACK32, 4);
	fmt(A8B8G8R8_SNORM_PACK32, 4);
	fmt(A8B8G8R8_USCALED_PACK32, 4);
	fmt(A8B8G8R8_SSCALED_PACK32, 4);
	fmt(A8B8G8R8_UINT_PACK32, 4);
	fmt(A8B8G8R8_SINT_PACK32, 4);
	fmt(A8B8G8R8_SRGB_PACK32, 4);
	fmt(A2B10G10R10_UNORM_PACK32, 4);
	fmt(A2B10G10R10_SNORM_PACK32, 4);
	fmt(A2B10G10R10_USCALED_PACK32, 4);
	fmt(A2B10G10R10_SSCALED_PACK32, 4);
	fmt(A2B10G10R10_UINT_PACK32, 4);
	fmt(A2B10G10R10_SINT_PACK32, 4);
	fmt(A2R10G10B10_UNORM_PACK32, 4);
	fmt(A2R10G10B10_SNORM_PACK32, 4);
	fmt(A2R10G10B10_USCALED_PACK32, 4);
	fmt(A2R10G10B10_SSCALED_PACK32, 4);
	fmt(A2R10G10B10_UINT_PACK32, 4);
	fmt(A2R10G10B10_SINT_PACK32, 4);
	fmt(R16_UNORM, 2);
	fmt(R16_SNORM, 2);
	fmt(R16_USCALED, 2);
	fmt(R16_SSCALED, 2);
	fmt(R16_UINT, 2);
	fmt(R16_SINT, 2);
	fmt(R16_SFLOAT, 2);
	fmt(R16G16_UNORM, 4);
	fmt(R16G16_SNORM, 4);
	fmt(R16G16_USCALED, 4);
	fmt(R16G16_SSCALED, 4);
	fmt(R16G16_UINT, 4);
	fmt(R16G16_SINT, 4);
	fmt(R16G16_SFLOAT, 4);
	fmt(R16G16B16_UNORM, 6);
	fmt(R16G16B16_SNORM, 6);
	fmt(R16G16B16_USCALED, 6);
	fmt(R16G16B16_SSCALED, 6);
	fmt(R16G16B16_UINT, 6);
	fmt(R16G16B16_SINT, 6);
	fmt(R16G16B16_SFLOAT, 6);
	fmt(R16G16B16A16_UNORM, 8);
	fmt(R16G16B16A16_SNORM, 8);
	fmt(R16G16B16A16_USCALED, 8);
	fmt(R16G16B16A16_SSCALED, 8);
	fmt(R16G16B16A16_UINT, 8);
	fmt(R16G16B16A16_SINT, 8);
	fmt(R16G16B16A16_SFLOAT, 8);
	fmt(R32_UINT, 4);
	fmt(R32_SINT, 4);
	fmt(R32_SFLOAT, 4);
	fmt(R32G32_UINT, 8);
	fmt(R32G32_SINT, 8);
	fmt(R32G32_SFLOAT, 8);
	fmt(R32G32B32_UINT, 12);
	fmt(R32G32B32_SINT, 12);
	fmt(R32G32B32_SFLOAT, 12);
	fmt(R32G32B32A32_UINT, 16);
	fmt(R32G32B32A32_SINT, 16);
	fmt(R32G32B32A32_SFLOAT, 16);
	fmt(R64_UINT, 8);
	fmt(R64_SINT, 8);
	fmt(R64_SFLOAT, 8);
	fmt(R64G64_UINT, 16);
	fmt(R64G64_SINT, 16);
	fmt(R64G64_SFLOAT, 16);
	fmt(R64G64B64_UINT, 24);
	fmt(R64G64B64_SINT, 24);
	fmt(R64G64B64_SFLOAT, 24);
	fmt(R64G64B64A64_UINT, 32);
	fmt(R64G64B64A64_SINT, 32);
	fmt(R64G64B64A64_SFLOAT, 32);
	fmt(B10G11R11_UFLOAT_PACK32, 4);
	fmt(E5B9G9R9_UFLOAT_PACK32, 4);

	fmt(D16_UNORM, 2);
	fmt(X8_D24_UNORM_PACK32, 4);
	fmt(D32_SFLOAT, 4);
	fmt(S8_UINT, 1);

	case VK_FORMAT_D16_UNORM_S8_UINT:
		return aspect == VK_IMAGE_ASPECT_DEPTH_BIT ? 2 : 1;
	case VK_FORMAT_D24_UNORM_S8_UINT:
	case VK_FORMAT_D32_SFLOAT_S8_UINT:
		return aspect == VK_IMAGE_ASPECT_DEPTH_BIT ? 4 : 1;

		// ETC2
	fmt(ETC2_R8G8B8A8_UNORM_BLOCK, 16);
	fmt(ETC2_R8G8B8A8_SRGB_BLOCK, 16);
	fmt(ETC2_R8G8B8A1_UNORM_BLOCK, 8);
	fmt(ETC2_R8G8B8A1_SRGB_BLOCK, 8);
	fmt(ETC2_R8G8B8_UNORM_BLOCK, 8);
	fmt(ETC2_R8G8B8_SRGB_BLOCK, 8);
	fmt(EAC_R11_UNORM_BLOCK, 8);
	fmt(EAC_R11_SNORM_BLOCK, 8);
	fmt(EAC_R11G11_UNORM_BLOCK, 16);
	fmt(EAC_R11G11_SNORM_BLOCK, 16);

		// BC
	fmt(BC1_RGB_UNORM_BLOCK, 8);
	fmt(BC1_RGB_SRGB_BLOCK, 8);
	fmt(BC1_RGBA_UNORM_BLOCK, 8);
	fmt(BC1_RGBA_SRGB_BLOCK, 8);
	fmt(BC2_UNORM_BLOCK, 16);
	fmt(BC2_SRGB_BLOCK, 16);
	fmt(BC3_UNORM_BLOCK, 16);
	fmt(BC3_SRGB_BLOCK, 16);
	fmt(BC4_UNORM_BLOCK, 8);
	fmt(BC4_SNORM_BLOCK, 8);
	fmt(BC5_UNORM_BLOCK, 16);
	fmt(BC5_SNORM_BLOCK, 16);
	fmt(BC6H_UFLOAT_BLOCK, 16);
	fmt(BC6H_SFLOAT_BLOCK, 16);
	fmt(BC7_SRGB_BLOCK, 16);
	fmt(BC7_UNORM_BLOCK, 16);

		// ASTC
#define astc_fmt(w, h) \
    fmt(ASTC_##w##x##h##_UNORM_BLOCK, 16); \
    fmt(ASTC_##w##x##h##_SRGB_BLOCK, 16); \
    fmt(ASTC_##w##x##h##_SFLOAT_BLOCK_EXT, 16)

	astc_fmt(4, 4);
	astc_fmt(5, 4);
	astc_fmt(5, 5);
	astc_fmt(6, 5);
	astc_fmt(6, 6);
	astc_fmt(8, 5);
	astc_fmt(8, 6);
	astc_fmt(8, 8);
	astc_fmt(10, 5);
	astc_fmt(10, 6);
	astc_fmt(10, 8);
	astc_fmt(10, 10);
	astc_fmt(12, 10);
	astc_fmt(12, 12);

	fmt(G8B8G8R8_422_UNORM, 4);
	fmt(B8G8R8G8_422_UNORM, 4);

	fmt(G8_B8_R8_3PLANE_420_UNORM, 1);
	fmt2(G8_B8R8_2PLANE_420_UNORM, 1, 2);
	fmt(G8_B8_R8_3PLANE_422_UNORM, 1);
	fmt2(G8_B8R8_2PLANE_422_UNORM, 1, 2);
	fmt(G8_B8_R8_3PLANE_444_UNORM, 1);

	fmt(R10X6_UNORM_PACK16, 2);
	fmt(R10X6G10X6_UNORM_2PACK16, 4);
	fmt(R10X6G10X6B10X6A10X6_UNORM_4PACK16, 8);
	fmt(G10X6B10X6G10X6R10X6_422_UNORM_4PACK16, 8);
	fmt(B10X6G10X6R10X6G10X6_422_UNORM_4PACK16, 8);
	fmt(G10X6_B10X6_R10X6_3PLANE_420_UNORM_3PACK16, 2);
	fmt(G10X6_B10X6_R10X6_3PLANE_422_UNORM_3PACK16, 2);
	fmt(G10X6_B10X6_R10X6_3PLANE_444_UNORM_3PACK16, 2);
	fmt2(G10X6_B10X6R10X6_2PLANE_420_UNORM_3PACK16, 2, 4);
	fmt2(G10X6_B10X6R10X6_2PLANE_422_UNORM_3PACK16, 2, 4);

	fmt(R12X4_UNORM_PACK16, 2);
	fmt(R12X4G12X4_UNORM_2PACK16, 4);
	fmt(R12X4G12X4B12X4A12X4_UNORM_4PACK16, 8);
	fmt(G12X4B12X4G12X4R12X4_422_UNORM_4PACK16, 8);
	fmt(B12X4G12X4R12X4G12X4_422_UNORM_4PACK16, 8);
	fmt(G12X4_B12X4_R12X4_3PLANE_420_UNORM_3PACK16, 2);
	fmt(G12X4_B12X4_R12X4_3PLANE_422_UNORM_3PACK16, 2);
	fmt(G12X4_B12X4_R12X4_3PLANE_444_UNORM_3PACK16, 2);
	fmt2(G12X4_B12X4R12X4_2PLANE_420_UNORM_3PACK16, 2, 4);
	fmt2(G12X4_B12X4R12X4_2PLANE_422_UNORM_3PACK16, 2, 4);

	fmt(G16B16G16R16_422_UNORM, 8);
	fmt(B16G16R16G16_422_UNORM, 8);
	fmt(G16_B16_R16_3PLANE_420_UNORM, 2);
	fmt(G16_B16_R16_3PLANE_422_UNORM, 2);
	fmt(G16_B16_R16_3PLANE_444_UNORM, 2);
	fmt2(G16_B16R16_2PLANE_420_UNORM, 2, 4);
	fmt2(G16_B16R16_2PLANE_422_UNORM, 2, 4);

	default:
		assert(0 && "Unknown format.");
		return 0;
	}
#undef fmt
#undef fmt2
#undef astc_fmt
}

void TextureFormatLayout::fill_mipinfo(uint32_t width, uint32_t height, uint32_t depth)
{
	block_stride = format_block_size(format, 0);
	format_block_dim(format, block_dim_x, block_dim_y);

	if (mip_levels == 0)
		mip_levels = num_miplevels(width, height, depth);

	size_t offset = 0;

	for (uint32_t mip = 0; mip < mip_levels; mip++)
	{
		offset = (offset + 15) & ~15;

		uint32_t blocks_x = (width + block_dim_x - 1) / block_dim_x;
		uint32_t blocks_y = (height + block_dim_y - 1) / block_dim_y;
		size_t mip_size = blocks_x * blocks_y * array_layers * depth * block_stride;

		mips[mip].offset = offset;

		mips[mip].block_row_length = blocks_x;
		mips[mip].block_image_height = blocks_y;

		mips[mip].row_length = blocks_x * block_dim_x;
		mips[mip].image_height = blocks_y * block_dim_y;

		mips[mip].width = width;
		mips[mip].height = height;
		mips[mip].depth = depth;

		offset += mip_size;

		width = max((width >> 1u), 1u);
		height = max((height >> 1u), 1u);
		depth = max((depth >> 1u), 1u);
	}

	required_size = offset;
}

void TextureFormatLayout::set_1d(VkFormat format_, uint32_t width, uint32_t array_layers_, uint32_t mip_levels_)
{
	image_type = VK_IMAGE_TYPE_1D;
	format = format_;
	array_layers = array_layers_;
	mip_levels = mip_levels_;

	fill_mipinfo(width, 1, 1);
}

void TextureFormatLayout::set_2d(VkFormat format_, uint32_t width, uint32_t height,
                                 uint32_t array_layers_, uint32_t mip_levels_)
{
	image_type = VK_IMAGE_TYPE_2D;
	format = format_;
	array_layers = array_layers_;
	mip_levels = mip_levels_;

	fill_mipinfo(width, height, 1);
}

void TextureFormatLayout::set_3d(VkFormat format_, uint32_t width, uint32_t height, uint32_t depth, uint32_t mip_levels_)
{
	image_type = VK_IMAGE_TYPE_3D;
	format = format_;
	array_layers = 1;
	mip_levels = mip_levels_;

	fill_mipinfo(width, height, depth);
}

void TextureFormatLayout::set_buffer(void *buffer_, size_t size)
{
	buffer = static_cast<uint8_t *>(buffer_);
	buffer_size = size;
}

uint32_t TextureFormatLayout::get_width(uint32_t mip) const
{
	return mips[mip].width;
}

uint32_t TextureFormatLayout::get_height(uint32_t mip) const
{
	return mips[mip].height;
}

uint32_t TextureFormatLayout::get_depth(uint32_t mip) const
{
	return mips[mip].depth;
}

uint32_t TextureFormatLayout::get_layers() const
{
	return array_layers;
}

VkImageType TextureFormatLayout::get_image_type() const
{
	return image_type;
}

VkFormat TextureFormatLayout::get_format() const
{
	return format;
}

uint32_t TextureFormatLayout::get_block_stride() const
{
	return block_stride;
}

uint32_t TextureFormatLayout::get_levels() const
{
	return mip_levels;
}

size_t TextureFormatLayout::get_required_size() const
{
	return required_size;
}

const TextureFormatLayout::MipInfo &TextureFormatLayout::get_mip_info(uint32_t mip) const
{
	return mips[mip];
}

uint32_t TextureFormatLayout::get_block_dim_x() const
{
	return block_dim_x;
}

uint32_t TextureFormatLayout::get_block_dim_y() const
{
	return block_dim_y;
}

size_t TextureFormatLayout::row_byte_stride(uint32_t row_length) const
{
	return ((row_length + block_dim_x - 1) / block_dim_x) * block_stride;
}

size_t TextureFormatLayout::layer_byte_stride(uint32_t image_height, size_t row_byte_stride) const
{
	return ((image_height + block_dim_y - 1) / block_dim_y) * row_byte_stride;
}

void TextureFormatLayout::build_buffer_image_copies(Util::SmallVector<VkBufferImageCopy, 32> &copies) const
{
	copies.resize(mip_levels);
	for (unsigned level = 0; level < mip_levels; level++)
	{
		const auto &mip_info = mips[level];

		auto &blit = copies[level];
		blit = {};
		blit.bufferOffset = mip_info.offset;
		blit.bufferRowLength = mip_info.row_length;
		blit.bufferImageHeight = mip_info.image_height;
		blit.imageSubresource.aspectMask = format_to_aspect_mask(format);
		blit.imageSubresource.mipLevel = level;
		blit.imageSubresource.baseArrayLayer = 0;
		blit.imageSubresource.layerCount = array_layers;
		blit.imageExtent.width = mip_info.width;
		blit.imageExtent.height = mip_info.height;
		blit.imageExtent.depth = mip_info.depth;
	}
}

}