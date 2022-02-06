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

#include "sampler.hpp"
#include "device.hpp"

namespace Vulkan
{
Sampler::Sampler(Device *device_, VkSampler sampler_, const SamplerCreateInfo &info, bool immutable_)
    : Cookie(device_)
    , device(device_)
    , sampler(sampler_)
    , create_info(info)
    , immutable(immutable_)
{
}

Sampler::~Sampler()
{
	if (sampler)
	{
		if (immutable)
			device->get_device_table().vkDestroySampler(device->get_device(), sampler, nullptr);
		else if (internal_sync)
			device->destroy_sampler_nolock(sampler);
		else
			device->destroy_sampler(sampler);
	}
}

void SamplerDeleter::operator()(Sampler *sampler)
{
	sampler->device->handle_pool.samplers.free(sampler);
}

VkSamplerCreateInfo Sampler::fill_vk_sampler_info(const SamplerCreateInfo &sampler_info)
{
	VkSamplerCreateInfo info = { VK_STRUCTURE_TYPE_SAMPLER_CREATE_INFO };

	info.magFilter = sampler_info.mag_filter;
	info.minFilter = sampler_info.min_filter;
	info.mipmapMode = sampler_info.mipmap_mode;
	info.addressModeU = sampler_info.address_mode_u;
	info.addressModeV = sampler_info.address_mode_v;
	info.addressModeW = sampler_info.address_mode_w;
	info.mipLodBias = sampler_info.mip_lod_bias;
	info.anisotropyEnable = sampler_info.anisotropy_enable;
	info.maxAnisotropy = sampler_info.max_anisotropy;
	info.compareEnable = sampler_info.compare_enable;
	info.compareOp = sampler_info.compare_op;
	info.minLod = sampler_info.min_lod;
	info.maxLod = sampler_info.max_lod;
	info.borderColor = sampler_info.border_color;
	info.unnormalizedCoordinates = sampler_info.unnormalized_coordinates;
	return info;
}

ImmutableSampler::ImmutableSampler(Util::Hash hash, Device *device_, const SamplerCreateInfo &sampler_info,
                                   const ImmutableYcbcrConversion *ycbcr_)
	: HashedObject<ImmutableSampler>(hash), device(device_), ycbcr(ycbcr_)
{
	VkSamplerYcbcrConversionInfoKHR conv_info = { VK_STRUCTURE_TYPE_SAMPLER_YCBCR_CONVERSION_INFO };
	auto info = Sampler::fill_vk_sampler_info(sampler_info);

	if (ycbcr)
	{
		conv_info.conversion = ycbcr->get_conversion();
		info.pNext = &conv_info;
	}

#ifdef VULKAN_DEBUG
	LOGI("Creating immutable sampler.\n");
#endif

	VkSampler vk_sampler = VK_NULL_HANDLE;
	if (device->get_device_table().vkCreateSampler(device->get_device(), &info, nullptr, &vk_sampler) != VK_SUCCESS)
		LOGE("Failed to create sampler.\n");

#ifdef GRANITE_VULKAN_FOSSILIZE
	register_sampler(vk_sampler, hash, info);
#endif

	sampler = SamplerHandle(device->handle_pool.samplers.allocate(device, vk_sampler, sampler_info, true));
}

ImmutableYcbcrConversion::ImmutableYcbcrConversion(Util::Hash hash, Device *device_,
                                                   const VkSamplerYcbcrConversionCreateInfo &info)
	: HashedObject<ImmutableYcbcrConversion>(hash), device(device_)
{
	if (device->get_device_features().sampler_ycbcr_conversion_features.samplerYcbcrConversion)
	{
		if (device->get_device_table().vkCreateSamplerYcbcrConversionKHR(device->get_device(), &info, nullptr,
		                                                                 &conversion) != VK_SUCCESS)
		{
			LOGE("Failed to create YCbCr conversion.\n");
		}
	}
	else
		LOGE("Ycbcr conversion is not supported on this device.\n");
}

ImmutableYcbcrConversion::~ImmutableYcbcrConversion()
{
	if (conversion)
		device->get_device_table().vkDestroySamplerYcbcrConversionKHR(device->get_device(), conversion, nullptr);
}
}
