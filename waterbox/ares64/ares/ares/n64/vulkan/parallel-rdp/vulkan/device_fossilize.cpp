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

#include "device.hpp"
#include "timer.hpp"

using namespace std;

namespace Vulkan
{
void Device::register_sampler(VkSampler sampler, Fossilize::Hash hash, const VkSamplerCreateInfo &info)
{
	state_recorder.record_sampler(sampler, info, hash);
}

void Device::register_descriptor_set_layout(VkDescriptorSetLayout layout, Fossilize::Hash hash, const VkDescriptorSetLayoutCreateInfo &info)
{
	state_recorder.record_descriptor_set_layout(layout, info, hash);
}

void Device::register_pipeline_layout(VkPipelineLayout layout, Fossilize::Hash hash, const VkPipelineLayoutCreateInfo &info)
{
	state_recorder.record_pipeline_layout(layout, info, hash);
}

void Device::register_shader_module(VkShaderModule module, Fossilize::Hash hash, const VkShaderModuleCreateInfo &info)
{
	state_recorder.record_shader_module(module, info, hash);
}

void Device::register_compute_pipeline(Fossilize::Hash hash, const VkComputePipelineCreateInfo &info)
{
	state_recorder.record_compute_pipeline(VK_NULL_HANDLE, info, nullptr, 0, hash);
}

void Device::register_graphics_pipeline(Fossilize::Hash hash, const VkGraphicsPipelineCreateInfo &info)
{
	state_recorder.record_graphics_pipeline(VK_NULL_HANDLE, info, nullptr, 0, hash);
}

void Device::register_render_pass(VkRenderPass render_pass, Fossilize::Hash hash, const VkRenderPassCreateInfo &info)
{
	state_recorder.record_render_pass(render_pass, info, hash);
}

bool Device::enqueue_create_shader_module(Fossilize::Hash hash, const VkShaderModuleCreateInfo *create_info, VkShaderModule *module)
{
	auto *ret = shaders.emplace_yield(hash, hash, this, create_info->pCode, create_info->codeSize);
	*module = ret->get_module();
	replayer_state.shader_map[*module] = ret;
	return true;
}

void Device::notify_replayed_resources_for_type()
{
#ifdef GRANITE_VULKAN_MT
	if (replayer_state.pipeline_group)
	{
		replayer_state.pipeline_group->wait();
		replayer_state.pipeline_group.reset();
	}
#endif
}

VkPipeline Device::fossilize_create_graphics_pipeline(Fossilize::Hash hash, VkGraphicsPipelineCreateInfo &info)
{
	if (info.stageCount != 2)
		return VK_NULL_HANDLE;
	if (info.pStages[0].stage != VK_SHADER_STAGE_VERTEX_BIT)
		return VK_NULL_HANDLE;
	if (info.pStages[1].stage != VK_SHADER_STAGE_FRAGMENT_BIT)
		return VK_NULL_HANDLE;

	// Find the Shader* associated with this VkShaderModule and just use that.
	auto vertex_itr = replayer_state.shader_map.find(info.pStages[0].module);
	if (vertex_itr == end(replayer_state.shader_map))
		return VK_NULL_HANDLE;

	// Find the Shader* associated with this VkShaderModule and just use that.
	auto fragment_itr = replayer_state.shader_map.find(info.pStages[1].module);
	if (fragment_itr == end(replayer_state.shader_map))
		return VK_NULL_HANDLE;

	auto *ret = request_program(vertex_itr->second, fragment_itr->second);

	// The layout is dummy, resolve it here.
	info.layout = ret->get_pipeline_layout()->get_layout();

	register_graphics_pipeline(hash, info);

	LOGI("Creating graphics pipeline.\n");
	VkPipeline pipeline = VK_NULL_HANDLE;
	VkResult res = table->vkCreateGraphicsPipelines(device, pipeline_cache, 1, &info, nullptr, &pipeline);
	if (res != VK_SUCCESS)
		LOGE("Failed to create graphics pipeline!\n");
	return ret->add_pipeline(hash, pipeline);
}

VkPipeline Device::fossilize_create_compute_pipeline(Fossilize::Hash hash, VkComputePipelineCreateInfo &info)
{
	// Find the Shader* associated with this VkShaderModule and just use that.
	auto itr = replayer_state.shader_map.find(info.stage.module);
	if (itr == end(replayer_state.shader_map))
		return VK_NULL_HANDLE;

	auto *ret = request_program(itr->second);

	// The layout is dummy, resolve it here.
	info.layout = ret->get_pipeline_layout()->get_layout();

	register_compute_pipeline(hash, info);

	LOGI("Creating compute pipeline.\n");
	VkPipeline pipeline = VK_NULL_HANDLE;
	VkResult res = table->vkCreateComputePipelines(device, pipeline_cache, 1, &info, nullptr, &pipeline);
	if (res != VK_SUCCESS)
		LOGE("Failed to create compute pipeline!\n");
	return ret->add_pipeline(hash, pipeline);
}

bool Device::enqueue_create_graphics_pipeline(Fossilize::Hash hash,
                                              const VkGraphicsPipelineCreateInfo *create_info,
                                              VkPipeline *pipeline)
{
#ifdef GRANITE_VULKAN_MT
	if (!replayer_state.pipeline_group)
		replayer_state.pipeline_group = Granite::Global::thread_group()->create_task();

	replayer_state.pipeline_group->enqueue_task([this, info = *create_info, hash, pipeline]() mutable {
		*pipeline = fossilize_create_graphics_pipeline(hash, info);
	});

	return true;
#else
	auto info = *create_info;
	*pipeline = fossilize_create_graphics_pipeline(hash, info);
	return *pipeline != VK_NULL_HANDLE;
#endif
}

bool Device::enqueue_create_compute_pipeline(Fossilize::Hash hash,
                                             const VkComputePipelineCreateInfo *create_info,
                                             VkPipeline *pipeline)
{
#ifdef GRANITE_VULKAN_MT
	if (!replayer_state.pipeline_group)
		replayer_state.pipeline_group = Granite::Global::thread_group()->create_task();

	replayer_state.pipeline_group->enqueue_task([this, info = *create_info, hash, pipeline]() mutable {
		*pipeline = fossilize_create_compute_pipeline(hash, info);
	});

	return true;
#else
	auto info = *create_info;
	*pipeline = fossilize_create_compute_pipeline(hash, info);
	return *pipeline != VK_NULL_HANDLE;
#endif
}

bool Device::enqueue_create_render_pass(Fossilize::Hash hash,
                                        const VkRenderPassCreateInfo *create_info,
                                        VkRenderPass *render_pass)
{
	auto *ret = render_passes.emplace_yield(hash, hash, this, *create_info);
	*render_pass = ret->get_render_pass();
	replayer_state.render_pass_map[*render_pass] = ret;
	return true;
}

bool Device::enqueue_create_sampler(Fossilize::Hash hash, const VkSamplerCreateInfo *, VkSampler *sampler)
{
	*sampler = get_stock_sampler(static_cast<StockSampler>(hash & 0xffffu)).get_sampler();
	return true;
}

bool Device::enqueue_create_descriptor_set_layout(Fossilize::Hash, const VkDescriptorSetLayoutCreateInfo *, VkDescriptorSetLayout *layout)
{
	// We will create this naturally when building pipelines, can just emit dummy handles.
	*layout = (VkDescriptorSetLayout) uint64_t(-1);
	return true;
}

bool Device::enqueue_create_pipeline_layout(Fossilize::Hash, const VkPipelineLayoutCreateInfo *, VkPipelineLayout *layout)
{
	// We will create this naturally when building pipelines, can just emit dummy handles.
	*layout = (VkPipelineLayout) uint64_t(-1);
	return true;
}

void Device::init_pipeline_state()
{
	state_recorder.init_recording_thread(nullptr);

	auto file = Granite::Global::filesystem()->open("assets://pipelines.json", Granite::FileMode::ReadOnly);
	if (!file)
		file = Granite::Global::filesystem()->open("cache://pipelines.json", Granite::FileMode::ReadOnly);

	if (!file)
		return;

	void *mapped = file->map();
	if (!mapped)
	{
		LOGE("Failed to map pipelines.json.\n");
		return;
	}

	LOGI("Replaying cached state.\n");
	Fossilize::StateReplayer replayer;
	auto start = Util::get_current_time_nsecs();
	replayer.parse(*this, nullptr, static_cast<const char *>(mapped), file->get_size());
	auto end = Util::get_current_time_nsecs();
	LOGI("Completed replaying cached state in %.3f ms.\n", (end - start) * 1e-6);
	replayer_state = {};
}

void Device::flush_pipeline_state()
{
	uint8_t *serialized = nullptr;
	size_t serialized_size = 0;
	if (!state_recorder.serialize(&serialized, &serialized_size))
	{
		LOGE("Failed to serialize Fossilize state.\n");
		return;
	}

	auto file = Granite::Global::filesystem()->open("cache://pipelines.json", Granite::FileMode::WriteOnly);
	if (file)
	{
		auto *data = static_cast<uint8_t *>(file->map_write(serialized_size));
		if (data)
		{
			memcpy(data, serialized, serialized_size);
			file->unmap();
		}
		else
			LOGE("Failed to serialize pipeline data.\n");

	}
	state_recorder.free_serialized(serialized);
}
}
