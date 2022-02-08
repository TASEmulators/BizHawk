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

#include "cookie.hpp"
#include "vulkan_common.hpp"
#include "vulkan_headers.hpp"
#include "object_pool.hpp"

namespace Vulkan
{
enum class StockSampler
{
	NearestClamp,
	LinearClamp,
	TrilinearClamp,
	NearestWrap,
	LinearWrap,
	TrilinearWrap,
	NearestShadow,
	LinearShadow,
	DefaultGeometryFilterClamp,
	DefaultGeometryFilterWrap,
	Count
};

struct SamplerCreateInfo
{
	VkFilter mag_filter;
	VkFilter min_filter;
	VkSamplerMipmapMode mipmap_mode;
	VkSamplerAddressMode address_mode_u;
	VkSamplerAddressMode address_mode_v;
	VkSamplerAddressMode address_mode_w;
	float mip_lod_bias;
	VkBool32 anisotropy_enable;
	float max_anisotropy;
	VkBool32 compare_enable;
	VkCompareOp compare_op;
	float min_lod;
	float max_lod;
	VkBorderColor border_color;
	VkBool32 unnormalized_coordinates;
};

class Sampler;
struct SamplerDeleter
{
	void operator()(Sampler *sampler);
};

class Sampler : public Util::IntrusivePtrEnabled<Sampler, SamplerDeleter, HandleCounter>,
                public Cookie, public InternalSyncEnabled
{
public:
	friend struct SamplerDeleter;
	~Sampler();

	VkSampler get_sampler() const
	{
		return sampler;
	}

	const SamplerCreateInfo &get_create_info() const
	{
		return create_info;
	}

	static VkSamplerCreateInfo fill_vk_sampler_info(const SamplerCreateInfo &sampler_info);

private:
	friend class Util::ObjectPool<Sampler>;
	Sampler(Device *device, VkSampler sampler, const SamplerCreateInfo &info, bool immutable);

	Device *device;
	VkSampler sampler;
	SamplerCreateInfo create_info;
	bool immutable;
};
using SamplerHandle = Util::IntrusivePtr<Sampler>;

class ImmutableYcbcrConversion : public HashedObject<ImmutableYcbcrConversion>
{
public:
	ImmutableYcbcrConversion(Util::Hash hash, Device *device,
	                         const VkSamplerYcbcrConversionCreateInfo &info);
	~ImmutableYcbcrConversion();
	void operator=(const ImmutableYcbcrConversion &) = delete;
	ImmutableYcbcrConversion(const ImmutableYcbcrConversion &) = delete;

	VkSamplerYcbcrConversion get_conversion() const
	{
		return conversion;
	}

private:
	Device *device;
	VkSamplerYcbcrConversion conversion = VK_NULL_HANDLE;
};

class ImmutableSampler : public HashedObject<ImmutableSampler>
{
public:
	ImmutableSampler(Util::Hash hash, Device *device,
	                 const SamplerCreateInfo &info,
	                 const ImmutableYcbcrConversion *ycbcr);
	void operator=(const ImmutableSampler &) = delete;
	ImmutableSampler(const ImmutableSampler &) = delete;

	const Sampler &get_sampler() const
	{
		return *sampler;
	}

	VkSamplerYcbcrConversion get_ycbcr_conversion() const
	{
		return ycbcr ? ycbcr->get_conversion() : VK_NULL_HANDLE;
	}

private:
	Device *device;
	const ImmutableYcbcrConversion *ycbcr;
	SamplerHandle sampler;
};
}
