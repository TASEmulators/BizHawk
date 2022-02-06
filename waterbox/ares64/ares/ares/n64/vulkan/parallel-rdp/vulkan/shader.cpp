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

#include "shader.hpp"
#include "device.hpp"
#ifdef GRANITE_VULKAN_SPIRV_CROSS
#include "spirv_cross.hpp"
using namespace spirv_cross;
#endif

using namespace std;
using namespace Util;

namespace Vulkan
{
void ImmutableSamplerBank::hash(Util::Hasher &h, const ImmutableSamplerBank *sampler_bank)
{
	if (sampler_bank)
	{
		for (auto &set : sampler_bank->samplers)
		{
			for (auto *binding : set)
			{
				if (binding)
					h.u64(binding->get_hash());
				else
					h.u32(0);
			}
		}
	}
	else
		h.u32(0);
}

PipelineLayout::PipelineLayout(Hash hash, Device *device_, const CombinedResourceLayout &layout_,
                               const ImmutableSamplerBank *immutable_samplers)
	: IntrusiveHashMapEnabled<PipelineLayout>(hash)
	, device(device_)
	, layout(layout_)
{
	VkDescriptorSetLayout layouts[VULKAN_NUM_DESCRIPTOR_SETS] = {};
	unsigned num_sets = 0;
	for (unsigned i = 0; i < VULKAN_NUM_DESCRIPTOR_SETS; i++)
	{
		set_allocators[i] = device->request_descriptor_set_allocator(layout.sets[i], layout.stages_for_bindings[i],
		                                                             immutable_samplers ? immutable_samplers->samplers[i] : nullptr);
		layouts[i] = set_allocators[i]->get_layout();
		if (layout.descriptor_set_mask & (1u << i))
			num_sets = i + 1;
	}

	if (num_sets > device->get_gpu_properties().limits.maxBoundDescriptorSets)
	{
		LOGE("Number of sets %u exceeds device limit of %u.\n",
		     num_sets, device->get_gpu_properties().limits.maxBoundDescriptorSets);
	}

	VkPipelineLayoutCreateInfo info = { VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO };
	if (num_sets)
	{
		info.setLayoutCount = num_sets;
		info.pSetLayouts = layouts;
	}

	if (layout.push_constant_range.stageFlags != 0)
	{
		info.pushConstantRangeCount = 1;
		info.pPushConstantRanges = &layout.push_constant_range;
	}

#ifdef VULKAN_DEBUG
	LOGI("Creating pipeline layout.\n");
#endif
	auto &table = device->get_device_table();
	if (table.vkCreatePipelineLayout(device->get_device(), &info, nullptr, &pipe_layout) != VK_SUCCESS)
		LOGE("Failed to create pipeline layout.\n");
#ifdef GRANITE_VULKAN_FOSSILIZE
	device->register_pipeline_layout(pipe_layout, get_hash(), info);
#endif

	if (device->get_device_features().supports_update_template)
		create_update_templates();
}

void PipelineLayout::create_update_templates()
{
	auto &table = device->get_device_table();
	for (unsigned desc_set = 0; desc_set < VULKAN_NUM_DESCRIPTOR_SETS; desc_set++)
	{
		if ((layout.descriptor_set_mask & (1u << desc_set)) == 0)
			continue;
		if ((layout.bindless_descriptor_set_mask & (1u << desc_set)) == 0)
			continue;

		VkDescriptorUpdateTemplateEntryKHR update_entries[VULKAN_NUM_BINDINGS];
		uint32_t update_count = 0;

		auto &set_layout = layout.sets[desc_set];

		for_each_bit(set_layout.uniform_buffer_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER_DYNAMIC;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			entry.offset = offsetof(ResourceBinding, buffer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.storage_buffer_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			entry.offset = offsetof(ResourceBinding, buffer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.sampled_buffer_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_UNIFORM_TEXEL_BUFFER;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			entry.offset = offsetof(ResourceBinding, buffer_view) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.sampled_image_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			if (set_layout.fp_mask & (1u << binding))
				entry.offset = offsetof(ResourceBinding, image.fp) + sizeof(ResourceBinding) * binding;
			else
				entry.offset = offsetof(ResourceBinding, image.integer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.separate_image_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			if (set_layout.fp_mask & (1u << binding))
				entry.offset = offsetof(ResourceBinding, image.fp) + sizeof(ResourceBinding) * binding;
			else
				entry.offset = offsetof(ResourceBinding, image.integer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.sampler_mask & ~set_layout.immutable_sampler_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_SAMPLER;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			entry.offset = offsetof(ResourceBinding, image.fp) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.storage_image_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			if (set_layout.fp_mask & (1u << binding))
				entry.offset = offsetof(ResourceBinding, image.fp) + sizeof(ResourceBinding) * binding;
			else
				entry.offset = offsetof(ResourceBinding, image.integer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		for_each_bit(set_layout.input_attachment_mask, [&](uint32_t binding) {
			unsigned array_size = set_layout.array_size[binding];
			VK_ASSERT(update_count < VULKAN_NUM_BINDINGS);
			auto &entry = update_entries[update_count++];
			entry.descriptorType = VK_DESCRIPTOR_TYPE_INPUT_ATTACHMENT;
			entry.dstBinding = binding;
			entry.dstArrayElement = 0;
			entry.descriptorCount = array_size;
			if (set_layout.fp_mask & (1u << binding))
				entry.offset = offsetof(ResourceBinding, image.fp) + sizeof(ResourceBinding) * binding;
			else
				entry.offset = offsetof(ResourceBinding, image.integer) + sizeof(ResourceBinding) * binding;
			entry.stride = sizeof(ResourceBinding);
		});

		VkDescriptorUpdateTemplateCreateInfoKHR info = {VK_STRUCTURE_TYPE_DESCRIPTOR_UPDATE_TEMPLATE_CREATE_INFO_KHR };
		info.pipelineLayout = pipe_layout;
		info.descriptorSetLayout = set_allocators[desc_set]->get_layout();
		info.templateType = VK_DESCRIPTOR_UPDATE_TEMPLATE_TYPE_DESCRIPTOR_SET_KHR;
		info.set = desc_set;
		info.descriptorUpdateEntryCount = update_count;
		info.pDescriptorUpdateEntries = update_entries;
		info.pipelineBindPoint = (layout.stages_for_sets[desc_set] & VK_SHADER_STAGE_COMPUTE_BIT) ?
				VK_PIPELINE_BIND_POINT_COMPUTE : VK_PIPELINE_BIND_POINT_GRAPHICS;

		if (table.vkCreateDescriptorUpdateTemplateKHR(device->get_device(), &info, nullptr,
		                                              &update_template[desc_set]) != VK_SUCCESS)
		{
			LOGE("Failed to create descriptor update template.\n");
		}
	}
}

PipelineLayout::~PipelineLayout()
{
	auto &table = device->get_device_table();
	if (pipe_layout != VK_NULL_HANDLE)
		table.vkDestroyPipelineLayout(device->get_device(), pipe_layout, nullptr);

	for (auto &update : update_template)
		if (update != VK_NULL_HANDLE)
			table.vkDestroyDescriptorUpdateTemplateKHR(device->get_device(), update, nullptr);
}

const char *Shader::stage_to_name(ShaderStage stage)
{
	switch (stage)
	{
	case ShaderStage::Compute:
		return "compute";
	case ShaderStage::Vertex:
		return "vertex";
	case ShaderStage::Fragment:
		return "fragment";
	case ShaderStage::Geometry:
		return "geometry";
	case ShaderStage::TessControl:
		return "tess_control";
	case ShaderStage::TessEvaluation:
		return "tess_evaluation";
	default:
		return "unknown";
	}
}

// Implicitly also checks for endian issues.
static const uint16_t reflection_magic[] = { 'G', 'R', 'A', ResourceLayout::Version };

size_t ResourceLayout::serialization_size()
{
	return sizeof(ResourceLayout) + sizeof(reflection_magic);
}

bool ResourceLayout::serialize(uint8_t *data, size_t size) const
{
	if (size != serialization_size())
		return false;

	// Cannot serialize externally defined immutable samplers.
	for (auto &set : sets)
		if (set.immutable_sampler_mask != 0)
			return false;

	memcpy(data, reflection_magic, sizeof(reflection_magic));
	memcpy(data + sizeof(reflection_magic), this, sizeof(*this));
	return true;
}

bool ResourceLayout::unserialize(const uint8_t *data, size_t size)
{
	if (size != sizeof(*this) + sizeof(reflection_magic))
	{
		LOGE("Reflection size mismatch.\n");
		return false;
	}

	if (memcmp(data, reflection_magic, sizeof(reflection_magic)) != 0)
	{
		LOGE("Magic mismatch.\n");
		return false;
	}

	memcpy(this, data + sizeof(reflection_magic), sizeof(*this));
	return true;
}

#ifdef GRANITE_VULKAN_SPIRV_CROSS
static void update_array_info(ResourceLayout &layout, const SPIRType &type, unsigned set, unsigned binding)
{
	auto &size = layout.sets[set].array_size[binding];
	if (!type.array.empty())
	{
		if (type.array.size() != 1)
			LOGE("Array dimension must be 1.\n");
		else if (!type.array_size_literal.front())
			LOGE("Array dimension must be a literal.\n");
		else
		{
			if (type.array.front() == 0)
			{
				if (binding != 0)
					LOGE("Bindless textures can only be used with binding = 0 in a set.\n");

				if (type.basetype != SPIRType::Image || type.image.dim == spv::DimBuffer)
					LOGE("Can only use bindless for sampled images.\n");
				else
					layout.bindless_set_mask |= 1u << set;

				size = DescriptorSetLayout::UNSIZED_ARRAY;
			}
			else if (size && size != type.array.front())
				LOGE("Array dimension for (%u, %u) is inconsistent.\n", set, binding);
			else if (type.array.front() + binding > VULKAN_NUM_BINDINGS)
				LOGE("Binding array will go out of bounds.\n");
			else
				size = uint8_t(type.array.front());
		}
	}
	else
	{
		if (size && size != 1)
			LOGE("Array dimension for (%u, %u) is inconsistent.\n", set, binding);
		size = 1;
	}
}

bool Shader::reflect_resource_layout(ResourceLayout &layout, const uint32_t *data, size_t size)
{
	Compiler compiler(data, size / sizeof(uint32_t));

	auto resources = compiler.get_shader_resources();
	for (auto &image : resources.sampled_images)
	{
		auto set = compiler.get_decoration(image.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(image.id, spv::DecorationBinding);
		auto &type = compiler.get_type(image.type_id);
		if (type.image.dim == spv::DimBuffer)
			layout.sets[set].sampled_buffer_mask |= 1u << binding;
		else
			layout.sets[set].sampled_image_mask |= 1u << binding;

		if (compiler.get_type(type.image.type).basetype == SPIRType::BaseType::Float)
			layout.sets[set].fp_mask |= 1u << binding;

		update_array_info(layout, type, set, binding);
	}

	for (auto &image : resources.subpass_inputs)
	{
		auto set = compiler.get_decoration(image.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(image.id, spv::DecorationBinding);
		layout.sets[set].input_attachment_mask |= 1u << binding;

		auto &type = compiler.get_type(image.type_id);
		if (compiler.get_type(type.image.type).basetype == SPIRType::BaseType::Float)
			layout.sets[set].fp_mask |= 1u << binding;
		update_array_info(layout, type, set, binding);
	}

	for (auto &image : resources.separate_images)
	{
		auto set = compiler.get_decoration(image.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(image.id, spv::DecorationBinding);

		auto &type = compiler.get_type(image.type_id);
		if (compiler.get_type(type.image.type).basetype == SPIRType::BaseType::Float)
			layout.sets[set].fp_mask |= 1u << binding;

		if (type.image.dim == spv::DimBuffer)
			layout.sets[set].sampled_buffer_mask |= 1u << binding;
		else
			layout.sets[set].separate_image_mask |= 1u << binding;

		update_array_info(layout, type, set, binding);
	}

	for (auto &image : resources.separate_samplers)
	{
		auto set = compiler.get_decoration(image.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(image.id, spv::DecorationBinding);
		layout.sets[set].sampler_mask |= 1u << binding;
		update_array_info(layout, compiler.get_type(image.type_id), set, binding);
	}

	for (auto &image : resources.storage_images)
	{
		auto set = compiler.get_decoration(image.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(image.id, spv::DecorationBinding);
		layout.sets[set].storage_image_mask |= 1u << binding;

		auto &type = compiler.get_type(image.type_id);
		if (compiler.get_type(type.image.type).basetype == SPIRType::BaseType::Float)
			layout.sets[set].fp_mask |= 1u << binding;

		update_array_info(layout, type, set, binding);
	}

	for (auto &buffer : resources.uniform_buffers)
	{
		auto set = compiler.get_decoration(buffer.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(buffer.id, spv::DecorationBinding);
		layout.sets[set].uniform_buffer_mask |= 1u << binding;
		update_array_info(layout, compiler.get_type(buffer.type_id), set, binding);
	}

	for (auto &buffer : resources.storage_buffers)
	{
		auto set = compiler.get_decoration(buffer.id, spv::DecorationDescriptorSet);
		auto binding = compiler.get_decoration(buffer.id, spv::DecorationBinding);
		layout.sets[set].storage_buffer_mask |= 1u << binding;
		update_array_info(layout, compiler.get_type(buffer.type_id), set, binding);
	}

	for (auto &attrib : resources.stage_inputs)
	{
		auto location = compiler.get_decoration(attrib.id, spv::DecorationLocation);
		layout.input_mask |= 1u << location;
	}

	for (auto &attrib : resources.stage_outputs)
	{
		auto location = compiler.get_decoration(attrib.id, spv::DecorationLocation);
		layout.output_mask |= 1u << location;
	}

	if (!resources.push_constant_buffers.empty())
	{
		// Don't bother trying to extract which part of a push constant block we're using.
		// Just assume we're accessing everything. At least on older validation layers,
		// it did not do a static analysis to determine similar information, so we got a lot
		// of false positives.
		layout.push_constant_size =
				compiler.get_declared_struct_size(compiler.get_type(resources.push_constant_buffers.front().base_type_id));
	}

	auto spec_constants = compiler.get_specialization_constants();
	for (auto &c : spec_constants)
	{
		if (c.constant_id >= VULKAN_NUM_SPEC_CONSTANTS)
		{
			LOGE("Spec constant ID: %u is out of range, will be ignored.\n", c.constant_id);
			continue;
		}

		layout.spec_constant_mask |= 1u << c.constant_id;
	}

	return true;
}
#else
bool Shader::reflect_resource_layout(ResourceLayout &, const uint32_t *, size_t)
{
	return false;
}
#endif

Shader::Shader(Hash hash, Device *device_, const uint32_t *data, size_t size,
               const ResourceLayout *resource_layout, const ImmutableSamplerBank *sampler_bank)
	: IntrusiveHashMapEnabled<Shader>(hash)
	, device(device_)
{
	VkShaderModuleCreateInfo info = { VK_STRUCTURE_TYPE_SHADER_MODULE_CREATE_INFO };
	info.codeSize = size;
	info.pCode = data;

#ifdef VULKAN_DEBUG
	LOGI("Creating shader module.\n");
#endif
	auto &table = device->get_device_table();
	if (table.vkCreateShaderModule(device->get_device(), &info, nullptr, &module) != VK_SUCCESS)
		LOGE("Failed to create shader module.\n");

#ifdef GRANITE_VULKAN_FOSSILIZE
	device->register_shader_module(module, get_hash(), info);
#endif

	if (resource_layout)
		layout = *resource_layout;
#ifdef GRANITE_VULKAN_SPIRV_CROSS
	else if (!reflect_resource_layout(layout, data, size))
		LOGE("Failed to reflect resource layout.\n");
#endif

	if (sampler_bank)
		immutable_sampler_bank = *sampler_bank;

	for (unsigned set = 0; set < VULKAN_NUM_DESCRIPTOR_SETS; set++)
	{
		for_each_bit(layout.sets[set].sampler_mask | layout.sets[set].sampled_image_mask, [&](unsigned binding) {
			if (sampler_bank && sampler_bank->samplers[set][binding])
				layout.sets[set].immutable_sampler_mask |= 1u << binding;
		});
	}

	if (layout.bindless_set_mask != 0 && !device->get_device_features().supports_descriptor_indexing)
		LOGE("Sufficient features for descriptor indexing is not supported on this device.\n");
}

Shader::~Shader()
{
	auto &table = device->get_device_table();
	if (module)
		table.vkDestroyShaderModule(device->get_device(), module, nullptr);
}

void Program::set_shader(ShaderStage stage, Shader *handle)
{
	shaders[Util::ecast(stage)] = handle;
}

Program::Program(Device *device_, Shader *vertex, Shader *fragment)
    : device(device_)
{
	set_shader(ShaderStage::Vertex, vertex);
	set_shader(ShaderStage::Fragment, fragment);
	device->bake_program(*this);
}

Program::Program(Device *device_, Shader *compute_shader)
    : device(device_)
{
	set_shader(ShaderStage::Compute, compute_shader);
	device->bake_program(*this);
}

VkPipeline Program::get_pipeline(Hash hash) const
{
	auto *ret = pipelines.find(hash);
	return ret ? ret->get() : VK_NULL_HANDLE;
}

VkPipeline Program::add_pipeline(Hash hash, VkPipeline pipeline)
{
	return pipelines.emplace_yield(hash, pipeline)->get();
}

void Program::destroy_pipeline(VkPipeline pipeline)
{
	if (internal_sync)
		device->destroy_pipeline_nolock(pipeline);
	else
		device->destroy_pipeline(pipeline);
}

void Program::promote_read_write_to_read_only()
{
#ifdef GRANITE_VULKAN_MT
	pipelines.move_to_read_only();
#endif
}

Program::~Program()
{
#ifdef GRANITE_VULKAN_MT
	for (auto &pipe : pipelines.get_read_only())
		destroy_pipeline(pipe.get());
	for (auto &pipe : pipelines.get_read_write())
		destroy_pipeline(pipe.get());
#else
	for (auto &pipe : pipelines)
		destroy_pipeline(pipe.get());
#endif
}
}
