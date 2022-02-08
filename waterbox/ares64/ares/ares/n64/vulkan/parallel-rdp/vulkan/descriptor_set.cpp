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

#include "descriptor_set.hpp"
#include "device.hpp"
#include <vector>

using namespace std;
using namespace Util;

namespace Vulkan
{
DescriptorSetAllocator::DescriptorSetAllocator(Hash hash, Device *device_, const DescriptorSetLayout &layout,
                                               const uint32_t *stages_for_binds,
                                               const ImmutableSampler * const *immutable_samplers)
	: IntrusiveHashMapEnabled<DescriptorSetAllocator>(hash)
	, device(device_)
	, table(device_->get_device_table())
{
	bindless = layout.array_size[0] == DescriptorSetLayout::UNSIZED_ARRAY;

	if (!bindless)
	{
		unsigned count = device_->num_thread_indices;
		for (unsigned i = 0; i < count; i++)
			per_thread.emplace_back(new PerThread);
	}

	if (bindless && !device->get_device_features().supports_descriptor_indexing)
	{
		LOGE("Cannot support descriptor indexing on this device.\n");
		return;
	}

	VkDescriptorSetLayoutCreateInfo info = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_CREATE_INFO };
	VkDescriptorSetLayoutBindingFlagsCreateInfoEXT flags = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_LAYOUT_BINDING_FLAGS_CREATE_INFO_EXT };
	VkSampler vk_immutable_samplers[VULKAN_NUM_BINDINGS] = {};
	vector<VkDescriptorSetLayoutBinding> bindings;
	VkDescriptorBindingFlagsEXT binding_flags = 0;

	if (bindless)
	{
		info.flags |= VK_DESCRIPTOR_SET_LAYOUT_CREATE_UPDATE_AFTER_BIND_POOL_BIT_EXT;
		info.pNext = &flags;

		flags.bindingCount = 1;
		flags.pBindingFlags = &binding_flags;
		binding_flags = VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT_EXT |
		                VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT_EXT;

		if (device->get_device_features().descriptor_indexing_features.descriptorBindingVariableDescriptorCount)
			binding_flags |= VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT_EXT;
	}

	for (unsigned i = 0; i < VULKAN_NUM_BINDINGS; i++)
	{
		auto stages = stages_for_binds[i];
		if (stages == 0)
			continue;

		unsigned array_size = layout.array_size[i];
		unsigned pool_array_size;
		if (array_size == DescriptorSetLayout::UNSIZED_ARRAY)
		{
			array_size = VULKAN_NUM_BINDINGS_BINDLESS_VARYING;
			pool_array_size = array_size;
			if (!device->get_device_features().descriptor_indexing_features.descriptorBindingVariableDescriptorCount)
				LOGW("Device does not support variable descriptor count.\n");
		}
		else
			pool_array_size = array_size * VULKAN_NUM_SETS_PER_POOL;

		unsigned types = 0;
		if (layout.sampled_image_mask & (1u << i))
		{
			if ((layout.immutable_sampler_mask & (1u << i)) && immutable_samplers && immutable_samplers[i])
				vk_immutable_samplers[i] = immutable_samplers[i]->get_sampler().get_sampler();

			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, array_size, stages,
			                     vk_immutable_samplers[i] != VK_NULL_HANDLE ? &vk_immutable_samplers[i] : nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, pool_array_size });
			types++;
		}

		if (layout.sampled_buffer_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER, pool_array_size });
			types++;
		}

		if (layout.storage_image_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_STORAGE_IMAGE, pool_array_size });
			types++;
		}

		if (layout.uniform_buffer_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC, pool_array_size });
			types++;
		}

		if (layout.storage_buffer_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_STORAGE_BUFFER, pool_array_size });
			types++;
		}

		if (layout.input_attachment_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT, pool_array_size });
			types++;
		}

		if (layout.separate_image_mask & (1u << i))
		{
			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, array_size, stages, nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE, pool_array_size });
			types++;
		}

		if (layout.sampler_mask & (1u << i))
		{
			if ((layout.immutable_sampler_mask & (1u << i)) && immutable_samplers && immutable_samplers[i])
				vk_immutable_samplers[i] = immutable_samplers[i]->get_sampler().get_sampler();

			bindings.push_back({ i, VK_DESCRIPTOR_TYPE_SAMPLER, array_size, stages,
			                     vk_immutable_samplers[i] != VK_NULL_HANDLE ? &vk_immutable_samplers[i] : nullptr });
			pool_size.push_back({ VK_DESCRIPTOR_TYPE_SAMPLER, pool_array_size });
			types++;
		}

		(void)types;
		VK_ASSERT(types <= 1 && "Descriptor set aliasing!");
	}

	if (!bindings.empty())
	{
		info.bindingCount = bindings.size();
		info.pBindings = bindings.data();

		if (bindless && bindings.size() != 1)
		{
			LOGE("Using bindless but have bindingCount != 1.\n");
			return;
		}
	}

#ifdef VULKAN_DEBUG
	LOGI("Creating descriptor set layout.\n");
#endif
	if (table.vkCreateDescriptorSetLayout(device->get_device(), &info, nullptr, &set_layout) != VK_SUCCESS)
		LOGE("Failed to create descriptor set layout.");
#ifdef GRANITE_VULKAN_FOSSILIZE
	device->register_descriptor_set_layout(set_layout, get_hash(), info);
#endif
}

void DescriptorSetAllocator::reset_bindless_pool(VkDescriptorPool pool)
{
	table.vkResetDescriptorPool(device->get_device(), pool, 0);
}

VkDescriptorSet DescriptorSetAllocator::allocate_bindless_set(VkDescriptorPool pool, unsigned num_descriptors)
{
	if (!pool || !bindless)
		return VK_NULL_HANDLE;

	VkDescriptorSetAllocateInfo info = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO };
	info.descriptorPool = pool;
	info.descriptorSetCount = 1;
	info.pSetLayouts = &set_layout;

	VkDescriptorSetVariableDescriptorCountAllocateInfoEXT count_info =
			{ VK_STRUCTURE_TYPE_DESCRIPTOR_SET_VARIABLE_DESCRIPTOR_COUNT_ALLOCATE_INFO_EXT };

	uint32_t num_desc = num_descriptors;
	if (device->get_device_features().descriptor_indexing_features.descriptorBindingVariableDescriptorCount)
	{
		count_info.descriptorSetCount = 1;
		count_info.pDescriptorCounts = &num_desc;
		info.pNext = &count_info;
	}

	VkDescriptorSet desc_set = VK_NULL_HANDLE;
	if (table.vkAllocateDescriptorSets(device->get_device(), &info, &desc_set) != VK_SUCCESS)
		return VK_NULL_HANDLE;

	return desc_set;
}

VkDescriptorPool DescriptorSetAllocator::allocate_bindless_pool(unsigned num_sets, unsigned num_descriptors)
{
	if (!bindless)
		return VK_NULL_HANDLE;

	VkDescriptorPool pool = VK_NULL_HANDLE;
	VkDescriptorPoolCreateInfo info = { VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO };
	info.flags = VK_DESCRIPTOR_POOL_CREATE_UPDATE_AFTER_BIND_BIT_EXT;
	info.maxSets = num_sets;
	info.poolSizeCount = 1;

	VkDescriptorPoolSize size = pool_size[0];
	if (num_descriptors > size.descriptorCount)
	{
		LOGE("Trying to allocate more than max bindless descriptors for descriptor layout.\n");
		return VK_NULL_HANDLE;
	}

	// If implementation does not support variable descriptor count, allocate maximum.
	if (device->get_device_features().descriptor_indexing_features.descriptorBindingVariableDescriptorCount)
		size.descriptorCount = num_descriptors;
	else
		info.maxSets = 1;

	info.pPoolSizes = &size;

	if (table.vkCreateDescriptorPool(device->get_device(), &info, nullptr, &pool) != VK_SUCCESS)
	{
		LOGE("Failed to create descriptor pool.\n");
		return VK_NULL_HANDLE;
	}

	return pool;
}

void DescriptorSetAllocator::begin_frame()
{
	if (!bindless)
	{
		for (auto &thr : per_thread)
			thr->should_begin = true;
	}
}

pair<VkDescriptorSet, bool> DescriptorSetAllocator::find(unsigned thread_index, Hash hash)
{
	VK_ASSERT(!bindless);

	auto &state = *per_thread[thread_index];
	if (state.should_begin)
	{
		state.set_nodes.begin_frame();
		state.should_begin = false;
	}

	auto *node = state.set_nodes.request(hash);
	if (node)
		return { node->set, true };

	node = state.set_nodes.request_vacant(hash);
	if (node)
		return { node->set, false };

	VkDescriptorPool pool;
	VkDescriptorPoolCreateInfo info = { VK_STRUCTURE_TYPE_DESCRIPTOR_POOL_CREATE_INFO };
	info.maxSets = VULKAN_NUM_SETS_PER_POOL;
	if (!pool_size.empty())
	{
		info.poolSizeCount = pool_size.size();
		info.pPoolSizes = pool_size.data();
	}

	if (table.vkCreateDescriptorPool(device->get_device(), &info, nullptr, &pool) != VK_SUCCESS)
	{
		LOGE("Failed to create descriptor pool.\n");
		return { VK_NULL_HANDLE, false };
	}

	VkDescriptorSet sets[VULKAN_NUM_SETS_PER_POOL];
	VkDescriptorSetLayout layouts[VULKAN_NUM_SETS_PER_POOL];
	fill(begin(layouts), end(layouts), set_layout);

	VkDescriptorSetAllocateInfo alloc = { VK_STRUCTURE_TYPE_DESCRIPTOR_SET_ALLOCATE_INFO };
	alloc.descriptorPool = pool;
	alloc.descriptorSetCount = VULKAN_NUM_SETS_PER_POOL;
	alloc.pSetLayouts = layouts;

	if (table.vkAllocateDescriptorSets(device->get_device(), &alloc, sets) != VK_SUCCESS)
		LOGE("Failed to allocate descriptor sets.\n");
	state.pools.push_back(pool);

	for (auto set : sets)
		state.set_nodes.make_vacant(set);

	return { state.set_nodes.request_vacant(hash)->set, false };
}

void DescriptorSetAllocator::clear()
{
	for (auto &thr : per_thread)
	{
		thr->set_nodes.clear();
		for (auto &pool : thr->pools)
		{
			table.vkResetDescriptorPool(device->get_device(), pool, 0);
			table.vkDestroyDescriptorPool(device->get_device(), pool, nullptr);
		}
		thr->pools.clear();
	}
}

DescriptorSetAllocator::~DescriptorSetAllocator()
{
	if (set_layout != VK_NULL_HANDLE)
		table.vkDestroyDescriptorSetLayout(device->get_device(), set_layout, nullptr);
	clear();
}

BindlessDescriptorPool::BindlessDescriptorPool(Device *device_, DescriptorSetAllocator *allocator_,
                                               VkDescriptorPool pool, uint32_t num_sets, uint32_t num_desc)
	: device(device_), allocator(allocator_), desc_pool(pool), total_sets(num_sets), total_descriptors(num_desc)
{
}

BindlessDescriptorPool::~BindlessDescriptorPool()
{
	if (desc_pool)
	{
		if (internal_sync)
			device->destroy_descriptor_pool_nolock(desc_pool);
		else
			device->destroy_descriptor_pool(desc_pool);
	}
}

VkDescriptorSet BindlessDescriptorPool::get_descriptor_set() const
{
	return desc_set;
}

void BindlessDescriptorPool::reset()
{
	if (desc_pool != VK_NULL_HANDLE)
		allocator->reset_bindless_pool(desc_pool);
	desc_set = VK_NULL_HANDLE;
	allocated_descriptor_count = 0;
	allocated_sets = 0;
}

bool BindlessDescriptorPool::allocate_descriptors(unsigned count)
{
	// Not all drivers will exhaust the pool for us, so make sure we don't allocate more than expected.
	if (allocated_sets == total_sets)
		return false;
	if (allocated_descriptor_count + count > total_descriptors)
		return false;

	allocated_descriptor_count += count;
	allocated_sets++;

	desc_set = allocator->allocate_bindless_set(desc_pool, count);
	return desc_set != VK_NULL_HANDLE;
}

void BindlessDescriptorPool::set_texture(unsigned binding, const ImageView &view)
{
	// TODO: Deal with integer view for depth-stencil images?
	set_texture(binding, view.get_float_view(), view.get_image().get_layout(VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL));
}

void BindlessDescriptorPool::set_texture_unorm(unsigned binding, const ImageView &view)
{
	set_texture(binding, view.get_unorm_view(), view.get_image().get_layout(VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL));
}

void BindlessDescriptorPool::set_texture_srgb(unsigned binding, const ImageView &view)
{
	set_texture(binding, view.get_srgb_view(), view.get_image().get_layout(VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL));
}

void BindlessDescriptorPool::set_texture(unsigned binding, VkImageView view, VkImageLayout layout)
{
	VkWriteDescriptorSet write = { VK_STRUCTURE_TYPE_WRITE_DESCRIPTOR_SET };
	write.descriptorCount = 1;
	write.dstArrayElement = binding;
	write.descriptorType = VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
	write.dstSet = desc_set;

	const VkDescriptorImageInfo info = {
		VK_NULL_HANDLE,
		view,
		layout,
	};
	write.pImageInfo = &info;

	auto &table = device->get_device_table();
	table.vkUpdateDescriptorSets(device->get_device(), 1, &write, 0, nullptr);
}

void BindlessDescriptorPoolDeleter::operator()(BindlessDescriptorPool *pool)
{
	pool->device->handle_pool.bindless_descriptor_pool.free(pool);
}

unsigned BindlessAllocator::push(const ImageView &view)
{
	auto ret = unsigned(views.size());
	views.push_back(&view);
	if (views.size() > VULKAN_NUM_BINDINGS_BINDLESS_VARYING)
	{
		LOGE("Exceeding maximum number of bindless resources per set (%u >= %u).\n",
		     unsigned(views.size()), VULKAN_NUM_BINDINGS_BINDLESS_VARYING);
	}
	return ret;
}

void BindlessAllocator::begin()
{
	views.clear();
}

unsigned BindlessAllocator::get_next_offset() const
{
	return unsigned(views.size());
}

void BindlessAllocator::reserve_max_resources_per_pool(unsigned set_count, unsigned descriptor_count)
{
	max_sets_per_pool = std::max(max_sets_per_pool, set_count);
	max_descriptors_per_pool = std::max(max_descriptors_per_pool, descriptor_count);
	views.reserve(max_descriptors_per_pool);
}

void BindlessAllocator::set_bindless_resource_type(BindlessResourceType type)
{
	resource_type = type;
}

VkDescriptorSet BindlessAllocator::commit(Device &device)
{
	max_sets_per_pool = std::max(1u, max_sets_per_pool);
	max_descriptors_per_pool = std::max<unsigned>(views.size(), max_descriptors_per_pool);
	max_descriptors_per_pool = std::max<unsigned>(1u, max_descriptors_per_pool);
	max_descriptors_per_pool = std::min(max_descriptors_per_pool, VULKAN_NUM_BINDINGS_BINDLESS_VARYING);
	unsigned to_allocate = std::max<unsigned>(views.size(), 1u);

	if (!descriptor_pool)
	{
		descriptor_pool = device.create_bindless_descriptor_pool(
				resource_type, max_sets_per_pool, max_descriptors_per_pool);
	}

	if (!descriptor_pool->allocate_descriptors(to_allocate))
	{
		descriptor_pool = device.create_bindless_descriptor_pool(
				resource_type, max_sets_per_pool, max_descriptors_per_pool);

		if (!descriptor_pool->allocate_descriptors(to_allocate))
		{
			LOGE("Failed to allocate descriptors on a fresh descriptor pool!\n");
			return VK_NULL_HANDLE;
		}
	}

	for (size_t i = 0, n = views.size(); i < n; i++)
		descriptor_pool->set_texture(i, *views[i]);
	return descriptor_pool->get_descriptor_set();
}
}
