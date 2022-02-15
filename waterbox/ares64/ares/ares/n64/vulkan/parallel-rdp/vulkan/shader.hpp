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
#include "descriptor_set.hpp"
#include "hash.hpp"
#include "intrusive.hpp"
#include "limits.hpp"
#include "vulkan_headers.hpp"
#include "enum_cast.hpp"

namespace spirv_cross
{
struct SPIRType;
}

namespace Vulkan
{
class Device;

enum class ShaderStage
{
	Vertex = 0,
	TessControl = 1,
	TessEvaluation = 2,
	Geometry = 3,
	Fragment = 4,
	Compute = 5,
	Count
};

struct ResourceLayout
{
	DescriptorSetLayout sets[VULKAN_NUM_DESCRIPTOR_SETS];
	uint32_t input_mask = 0;
	uint32_t output_mask = 0;
	uint32_t push_constant_size = 0;
	uint32_t spec_constant_mask = 0;
	uint32_t bindless_set_mask = 0;
	enum { Version = 2 };

	bool unserialize(const uint8_t *data, size_t size);
	bool serialize(uint8_t *data, size_t size) const;
	static size_t serialization_size();
};
static_assert(sizeof(DescriptorSetLayout) % 8 == 0, "Size of DescriptorSetLayout does not align to 64 bits.");

struct CombinedResourceLayout
{
	uint32_t attribute_mask = 0;
	uint32_t render_target_mask = 0;
	DescriptorSetLayout sets[VULKAN_NUM_DESCRIPTOR_SETS] = {};
	uint32_t stages_for_bindings[VULKAN_NUM_DESCRIPTOR_SETS][VULKAN_NUM_BINDINGS] = {};
	uint32_t stages_for_sets[VULKAN_NUM_DESCRIPTOR_SETS] = {};
	VkPushConstantRange push_constant_range = {};
	uint32_t descriptor_set_mask = 0;
	uint32_t bindless_descriptor_set_mask = 0;
	uint32_t spec_constant_mask[Util::ecast(ShaderStage::Count)] = {};
	uint32_t combined_spec_constant_mask = 0;
	Util::Hash push_constant_layout_hash = 0;
};

struct ResourceBinding
{
	union {
		VkDescriptorBufferInfo buffer;
		struct
		{
			VkDescriptorImageInfo fp;
			VkDescriptorImageInfo integer;
		} image;
		VkBufferView buffer_view;
	};
	VkDeviceSize dynamic_offset;
};

struct ResourceBindings
{
	ResourceBinding bindings[VULKAN_NUM_DESCRIPTOR_SETS][VULKAN_NUM_BINDINGS];
	uint64_t cookies[VULKAN_NUM_DESCRIPTOR_SETS][VULKAN_NUM_BINDINGS];
	uint64_t secondary_cookies[VULKAN_NUM_DESCRIPTOR_SETS][VULKAN_NUM_BINDINGS];
	uint8_t push_constant_data[VULKAN_PUSH_CONSTANT_SIZE];
};

struct ImmutableSamplerBank
{
	const ImmutableSampler *samplers[VULKAN_NUM_DESCRIPTOR_SETS][VULKAN_NUM_BINDINGS];
	static void hash(Util::Hasher &h, const ImmutableSamplerBank *bank);
};

class PipelineLayout : public HashedObject<PipelineLayout>
{
public:
	PipelineLayout(Util::Hash hash, Device *device, const CombinedResourceLayout &layout,
	               const ImmutableSamplerBank *sampler_bank);
	~PipelineLayout();

	const CombinedResourceLayout &get_resource_layout() const
	{
		return layout;
	}

	VkPipelineLayout get_layout() const
	{
		return pipe_layout;
	}

	DescriptorSetAllocator *get_allocator(unsigned set) const
	{
		return set_allocators[set];
	}

	VkDescriptorUpdateTemplateKHR get_update_template(unsigned set) const
	{
		return update_template[set];
	}

private:
	Device *device;
	VkPipelineLayout pipe_layout = VK_NULL_HANDLE;
	CombinedResourceLayout layout;
	DescriptorSetAllocator *set_allocators[VULKAN_NUM_DESCRIPTOR_SETS] = {};
	VkDescriptorUpdateTemplateKHR update_template[VULKAN_NUM_DESCRIPTOR_SETS] = {};
	void create_update_templates();
};

class Shader : public HashedObject<Shader>
{
public:
	Shader(Util::Hash binding, Device *device, const uint32_t *data, size_t size,
	       const ResourceLayout *layout = nullptr,
	       const ImmutableSamplerBank *sampler_bank = nullptr);
	~Shader();

	const ResourceLayout &get_layout() const
	{
		return layout;
	}

	const ImmutableSamplerBank &get_immutable_sampler_bank() const
	{
		return immutable_sampler_bank;
	}

	VkShaderModule get_module() const
	{
		return module;
	}

	static bool reflect_resource_layout(ResourceLayout &layout, const uint32_t *spirv_data, size_t spirv_size);

	static const char *stage_to_name(ShaderStage stage);

private:
	Device *device;
	VkShaderModule module = VK_NULL_HANDLE;
	ResourceLayout layout;
	ImmutableSamplerBank immutable_sampler_bank;
};

class Program : public HashedObject<Program>, public InternalSyncEnabled
{
public:
	Program(Device *device, Shader *vertex, Shader *fragment);
	Program(Device *device, Shader *compute);
	~Program();

	inline const Shader *get_shader(ShaderStage stage) const
	{
		return shaders[Util::ecast(stage)];
	}

	void set_pipeline_layout(PipelineLayout *new_layout)
	{
		layout = new_layout;
	}

	PipelineLayout *get_pipeline_layout() const
	{
		return layout;
	}

	VkPipeline get_pipeline(Util::Hash hash) const;
	VkPipeline add_pipeline(Util::Hash hash, VkPipeline pipeline);

	void promote_read_write_to_read_only();

private:
	void set_shader(ShaderStage stage, Shader *handle);
	Device *device;
	Shader *shaders[Util::ecast(ShaderStage::Count)] = {};
	PipelineLayout *layout = nullptr;
	VulkanCache<Util::IntrusivePODWrapper<VkPipeline>> pipelines;
	void destroy_pipeline(VkPipeline pipeline);
};
}
