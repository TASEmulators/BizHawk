/* Copyright (c) 2020 Themaister
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

#include "rdp_renderer.hpp"
#include "rdp_device.hpp"
#include "logging.hpp"
#include "bitops.hpp"
#include "luts.hpp"
#include "timer.hpp"
#include <limits>
#ifdef PARALLEL_RDP_SHADER_DIR
#include "global_managers.hpp"
#include "os_filesystem.hpp"
#else
#include "shaders/slangmosh.hpp"
#endif

namespace RDP
{
Renderer::Renderer(CommandProcessor &processor_)
	: processor(processor_)
{
	active_submissions = 0;
}

Renderer::~Renderer()
{
}

void Renderer::set_shader_bank(const ShaderBank *bank)
{
	shader_bank = bank;
}

bool Renderer::init_renderer(const RendererOptions &options)
{
	if (options.upscaling_factor == 0)
		return false;
	if (options.upscaling_factor == 1 && options.super_sampled_readback)
		return false;

	caps.max_width = options.upscaling_factor * Limits::MaxWidth;
	caps.max_height = options.upscaling_factor * Limits::MaxHeight;
	caps.max_tiles_x = options.upscaling_factor * ImplementationConstants::MaxTilesX;
	caps.max_tiles_y = options.upscaling_factor * ImplementationConstants::MaxTilesY;
	caps.max_num_tile_instances = options.upscaling_factor * options.upscaling_factor * Limits::MaxTileInstances;

#ifdef PARALLEL_RDP_SHADER_DIR
	pipeline_worker.reset(new WorkerThread<Vulkan::DeferredPipelineCompile, PipelineExecutor>(
			Granite::Global::create_thread_context(), { device }));
#else
	pipeline_worker.reset(new WorkerThread<Vulkan::DeferredPipelineCompile, PipelineExecutor>({ device }));
#endif

#ifdef PARALLEL_RDP_SHADER_DIR
	if (!GRANITE_FILESYSTEM()->get_backend("rdp"))
		GRANITE_FILESYSTEM()->register_protocol("rdp", std::make_unique<Granite::OSFilesystem>(PARALLEL_RDP_SHADER_DIR));
	device->get_shader_manager().add_include_directory("builtin://shaders/inc");
#endif

	for (auto &buffer : buffer_instances)
		buffer.init(*device);

	if (const char *env = getenv("RDP_DEBUG"))
		debug_channel = strtoul(env, nullptr, 0) != 0;
	if (const char *env = getenv("RDP_DEBUG_X"))
		filter_debug_channel_x = strtol(env, nullptr, 0);
	if (const char *env = getenv("RDP_DEBUG_Y"))
		filter_debug_channel_y = strtol(env, nullptr, 0);

	{
		Vulkan::BufferCreateInfo info = {};
		info.size = Limits::MaxTMEMInstances * 0x1000;
		info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
		info.domain = Vulkan::BufferDomain::Device;
		info.misc = Vulkan::BUFFER_MISC_ZERO_INITIALIZE_BIT;
		tmem_instances = device->create_buffer(info);
		device->set_name(*tmem_instances, "tmem-instances");
		stream.tmem_upload_infos.reserve(Limits::MaxTMEMInstances);
	}

	{
		Vulkan::BufferCreateInfo info = {};
		info.size = options.upscaling_factor * Limits::MaxSpanSetups * sizeof(SpanSetup);
		info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
		info.domain = Vulkan::BufferDomain::Device;
		info.misc = Vulkan::BUFFER_MISC_ZERO_INITIALIZE_BIT;
		span_setups = device->create_buffer(info);
		device->set_name(*span_setups, "span-setups");
	}

	init_blender_lut();
	init_buffers(options);
	if (options.upscaling_factor > 1 && !init_internal_upscaling_factor(options))
		return false;
	return init_caps();
}

void Renderer::set_device(Vulkan::Device *device_)
{
	device = device_;
}

bool Renderer::init_caps()
{
	auto &features = device->get_device_features();

	if (const char *timestamp = getenv("PARALLEL_RDP_BENCH"))
	{
		caps.timestamp = strtol(timestamp, nullptr, 0);
		LOGI("Enabling timestamps = %d\n", caps.timestamp);
	}

	if (const char *ubershader = getenv("PARALLEL_RDP_UBERSHADER"))
	{
		caps.ubershader = strtol(ubershader, nullptr, 0) > 0;
		LOGI("Overriding ubershader = %d\n", int(caps.ubershader));
	}

	if (const char *force_sync = getenv("PARALLEL_RDP_FORCE_SYNC_SHADER"))
	{
		caps.force_sync = strtol(force_sync, nullptr, 0) > 0;
		LOGI("Overriding force sync shader = %d\n", int(caps.force_sync));
	}

	bool allow_subgroup = true;
	if (const char *subgroup = getenv("PARALLEL_RDP_SUBGROUP"))
	{
		allow_subgroup = strtol(subgroup, nullptr, 0) > 0;
		LOGI("Allow subgroups = %d\n", int(allow_subgroup));
	}

	bool allow_small_types = true;
	bool forces_small_types = false;
	if (const char *small = getenv("PARALLEL_RDP_SMALL_TYPES"))
	{
		allow_small_types = strtol(small, nullptr, 0) > 0;
		forces_small_types = true;
		LOGI("Allow small types = %d.\n", int(allow_small_types));
	}

	if (!features.storage_16bit_features.storageBuffer16BitAccess)
	{
		LOGE("VK_KHR_16bit_storage for SSBOs is not supported! This is a minimum requirement for paraLLEl-RDP.\n");
		return false;
	}

	if (!features.storage_8bit_features.storageBuffer8BitAccess)
	{
		LOGE("VK_KHR_8bit_storage for SSBOs is not supported! This is a minimum requirement for paraLLEl-RDP.\n");
		return false;
	}

	// Driver workarounds here for 8/16-bit integer support.
	if (features.supports_driver_properties && !forces_small_types)
	{
		if (features.driver_properties.driverID == VK_DRIVER_ID_AMD_PROPRIETARY_KHR)
		{
			LOGW("Current proprietary AMD driver is known to be buggy with 8/16-bit integer arithmetic, disabling support for time being.\n");
			allow_small_types = false;
		}
		else if (features.driver_properties.driverID == VK_DRIVER_ID_AMD_OPEN_SOURCE_KHR ||
		         features.driver_properties.driverID == VK_DRIVER_ID_MESA_RADV_KHR)
		{
			LOGW("Current open-source AMD drivers are known to be slightly faster without 8/16-bit integer arithmetic.\n");
			allow_small_types = false;
		}
		else if (features.driver_properties.driverID == VK_DRIVER_ID_NVIDIA_PROPRIETARY_KHR)
		{
			LOGW("Current NVIDIA driver is known to be slightly faster without 8/16-bit integer arithmetic.\n");
			allow_small_types = false;
		}
		else if (features.driver_properties.driverID == VK_DRIVER_ID_INTEL_PROPRIETARY_WINDOWS_KHR)
		{
			LOGW("Current proprietary Intel Windows driver is tested to perform much better without 8/16-bit integer support.\n");
			allow_small_types = false;
		}

		// Intel ANV *must* use small integer arithmetic, or it doesn't pass test suite.
	}

	if (!allow_small_types)
	{
		caps.supports_small_integer_arithmetic = false;
	}
	else if (features.enabled_features.shaderInt16 && features.float16_int8_features.shaderInt8)
	{
		LOGI("Enabling 8 and 16-bit integer arithmetic support for more efficient shaders!\n");
		caps.supports_small_integer_arithmetic = true;
	}
	else
	{
		LOGW("Device does not support 8 and 16-bit integer arithmetic support. Falling back to 32-bit arithmetic everywhere.\n");
		caps.supports_small_integer_arithmetic = false;
	}

	uint32_t subgroup_size = features.subgroup_properties.subgroupSize;

	const VkSubgroupFeatureFlags required =
			VK_SUBGROUP_FEATURE_BALLOT_BIT |
			VK_SUBGROUP_FEATURE_BASIC_BIT |
			VK_SUBGROUP_FEATURE_VOTE_BIT |
			VK_SUBGROUP_FEATURE_ARITHMETIC_BIT;

	caps.subgroup_tile_binning =
			allow_subgroup &&
			(features.subgroup_properties.supportedOperations & required) == required &&
			(features.subgroup_properties.supportedStages & VK_SHADER_STAGE_COMPUTE_BIT) != 0 &&
			can_support_minimum_subgroup_size(32) && subgroup_size <= 64;

	caps.subgroup_depth_blend =
			caps.super_sample_readback &&
			allow_subgroup &&
			(features.subgroup_properties.supportedOperations & required) == required &&
			(features.subgroup_properties.supportedStages & VK_SHADER_STAGE_COMPUTE_BIT) != 0;

	return true;
}

int Renderer::resolve_shader_define(const char *name, const char *define) const
{
	if (strcmp(define, "DEBUG_ENABLE") == 0)
		return int(debug_channel);
	else if (strcmp(define, "UBERSHADER") == 0)
		return int(caps.ubershader);
	else if (strcmp(define, "SMALL_TYPES") == 0)
		return int(caps.supports_small_integer_arithmetic);
	else if (strcmp(define, "SUBGROUP") == 0)
	{
		if (strcmp(name, "tile_binning_combined") == 0)
			return int(caps.subgroup_tile_binning);
		else if (strcmp(name, "depth_blend") == 0 || strcmp(name, "ubershader") == 0)
			return int(caps.subgroup_depth_blend);
		else
			return 0;
	}
	else
		return 0;
}

void Renderer::init_buffers(const RendererOptions &options)
{
	Vulkan::BufferCreateInfo info = {};
	info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
	info.domain = Vulkan::BufferDomain::Device;
	info.misc = Vulkan::BUFFER_MISC_ZERO_INITIALIZE_BIT;

	static_assert((Limits::MaxPrimitives % 32) == 0, "MaxPrimitives must be divisble by 32.");
	static_assert(Limits::MaxPrimitives <= (32 * 32), "MaxPrimitives must be less-or-equal than 1024.");

	info.size = sizeof(uint32_t) *
	            (Limits::MaxPrimitives / 32) *
	            (caps.max_width / ImplementationConstants::TileWidth) *
	            (caps.max_height / ImplementationConstants::TileHeight);

	tile_binning_buffer = device->create_buffer(info);
	device->set_name(*tile_binning_buffer, "tile-binning-buffer");

	info.size = sizeof(uint32_t) *
	            (caps.max_width / ImplementationConstants::TileWidth) *
	            (caps.max_height / ImplementationConstants::TileHeight);

	tile_binning_buffer_coarse = device->create_buffer(info);
	device->set_name(*tile_binning_buffer_coarse, "tile-binning-buffer-coarse");

	if (!caps.ubershader)
	{
		info.size = sizeof(uint32_t) *
		            (Limits::MaxPrimitives / 32) *
		            (caps.max_width / ImplementationConstants::TileWidth) *
		            (caps.max_height / ImplementationConstants::TileHeight);

		per_tile_offsets = device->create_buffer(info);
		device->set_name(*per_tile_offsets, "per-tile-offsets");

		info.size = sizeof(TileRasterWork) * Limits::MaxStaticRasterizationStates * caps.max_num_tile_instances;
		tile_work_list = device->create_buffer(info);
		device->set_name(*tile_work_list, "tile-work-list");

		info.size = sizeof(uint32_t) *
		            caps.max_num_tile_instances *
		            ImplementationConstants::TileWidth *
		            ImplementationConstants::TileHeight;
		per_tile_shaded_color = device->create_buffer(info);
		device->set_name(*per_tile_shaded_color, "per-tile-shaded-color");
		per_tile_shaded_depth = device->create_buffer(info);
		device->set_name(*per_tile_shaded_depth, "per-tile-shaded-depth");

		info.size = sizeof(uint8_t) *
		            caps.max_num_tile_instances *
		            ImplementationConstants::TileWidth *
		            ImplementationConstants::TileHeight;
		per_tile_shaded_coverage = device->create_buffer(info);
		per_tile_shaded_shaded_alpha = device->create_buffer(info);
		device->set_name(*per_tile_shaded_coverage, "per-tile-shaded-coverage");
		device->set_name(*per_tile_shaded_shaded_alpha, "per-tile-shaded-shaded-alpha");
	}
}

void Renderer::init_blender_lut()
{
	Vulkan::BufferCreateInfo info = {};
	info.size = sizeof(blender_lut);
	info.domain = Vulkan::BufferDomain::Device;
	info.usage = VK_BUFFER_USAGE_UNIFORM_TEXEL_BUFFER_BIT;

	blender_divider_lut_buffer = device->create_buffer(info, blender_lut);
	device->set_name(*blender_divider_lut_buffer, "blender-divider-lut-buffer");

	Vulkan::BufferViewCreateInfo view = {};
	view.buffer = blender_divider_lut_buffer.get();
	view.format = VK_FORMAT_R8_UINT;
	view.range = info.size;
	blender_divider_buffer = device->create_buffer_view(view);
}

void Renderer::message(const std::string &tag, uint32_t code, uint32_t x, uint32_t y, uint32_t, uint32_t num_words,
                       const Vulkan::DebugChannelInterface::Word *words)
{
	if (filter_debug_channel_x >= 0 && x != uint32_t(filter_debug_channel_x))
		return;
	if (filter_debug_channel_y >= 0 && y != uint32_t(filter_debug_channel_y))
		return;

	enum Code
	{
		ASSERT_EQUAL = 0,
		ASSERT_NOT_EQUAL = 1,
		ASSERT_LESS_THAN = 2,
		ASSERT_LESS_THAN_EQUAL = 3,
		GENERIC = 4,
		HEX = 5
	};

	switch (Code(code))
	{
	case ASSERT_EQUAL:
		LOGE("ASSERT TRIPPED FOR (%u, %u), line %d, %d == %d failed.\n",
		     x, y, words[0].s32, words[1].s32, words[2].s32);
		break;

	case ASSERT_NOT_EQUAL:
		LOGE("ASSERT TRIPPED FOR (%u, %u), line %d, %d != %d failed.\n",
		     x, y, words[0].s32, words[1].s32, words[2].s32);
		break;

	case ASSERT_LESS_THAN:
		LOGE("ASSERT TRIPPED FOR (%u, %u), line %d, %d < %d failed.\n",
		     x, y, words[0].s32, words[1].s32, words[2].s32);
		break;

	case ASSERT_LESS_THAN_EQUAL:
		LOGE("ASSERT TRIPPED FOR (%u, %u), line %d, %d <= %d failed.\n",
		     x, y, words[0].s32, words[1].s32, words[2].s32);
		break;

	case GENERIC:
		switch (num_words)
		{
		case 1:
			LOGI("(%u, %u), line %d.\n", x, y, words[0].s32);
			break;

		case 2:
			LOGI("(%u, %u), line %d: (%d).\n", x, y, words[0].s32, words[1].s32);
			break;

		case 3:
			LOGI("(%u, %u), line %d: (%d, %d).\n", x, y, words[0].s32, words[1].s32, words[2].s32);
			break;

		case 4:
			LOGI("(%u, %u), line %d: (%d, %d, %d).\n", x, y,
					words[0].s32, words[1].s32, words[2].s32, words[3].s32);
			break;

		default:
			LOGE("Unknown number of generic parameters: %u\n", num_words);
			break;
		}
		break;

	case HEX:
		switch (num_words)
		{
		case 1:
			LOGI("(%u, %u), line %d.\n", x, y, words[0].s32);
			break;

		case 2:
			LOGI("(%u, %u), line %d: (0x%x).\n", x, y, words[0].s32, words[1].s32);
			break;

		case 3:
			LOGI("(%u, %u), line %d: (0x%x, 0x%x).\n", x, y, words[0].s32, words[1].s32, words[2].s32);
			break;

		case 4:
			LOGI("(%u, %u), line %d: (0x%x, 0x%x, 0x%x).\n", x, y,
			     words[0].s32, words[1].s32, words[2].s32, words[3].s32);
			break;

		default:
			LOGE("Unknown number of generic parameters: %u\n", num_words);
			break;
		}
		break;

	default:
		LOGE("Unexpected message code: %u\n", code);
		break;
	}
}

void Renderer::RenderBuffers::init(Vulkan::Device &device, Vulkan::BufferDomain domain,
                                   RenderBuffers *borrow)
{
	triangle_setup = create_buffer(device, domain,
	                               sizeof(TriangleSetup) * Limits::MaxPrimitives,
	                               borrow ? &borrow->triangle_setup : nullptr);
	device.set_name(*triangle_setup.buffer, "triangle-setup");

	attribute_setup = create_buffer(device, domain,
	                                sizeof(AttributeSetup) * Limits::MaxPrimitives,
	                                borrow ? &borrow->attribute_setup: nullptr);
	device.set_name(*attribute_setup.buffer, "attribute-setup");

	derived_setup = create_buffer(device, domain,
	                              sizeof(DerivedSetup) * Limits::MaxPrimitives,
	                              borrow ? &borrow->derived_setup : nullptr);
	device.set_name(*derived_setup.buffer, "derived-setup");

	scissor_setup = create_buffer(device, domain,
	                              sizeof(ScissorState) * Limits::MaxPrimitives,
	                              borrow ? &borrow->scissor_setup : nullptr);
	device.set_name(*scissor_setup.buffer, "scissor-state");

	static_raster_state = create_buffer(device, domain,
	                                    sizeof(StaticRasterizationState) * Limits::MaxStaticRasterizationStates,
	                                    borrow ? &borrow->static_raster_state : nullptr);
	device.set_name(*static_raster_state.buffer, "static-raster-state");

	depth_blend_state = create_buffer(device, domain,
	                                  sizeof(DepthBlendState) * Limits::MaxDepthBlendStates,
	                                  borrow ? &borrow->depth_blend_state : nullptr);
	device.set_name(*depth_blend_state.buffer, "depth-blend-state");

	tile_info_state = create_buffer(device, domain,
	                                sizeof(TileInfo) * Limits::MaxTileInfoStates,
	                                borrow ? &borrow->tile_info_state : nullptr);
	device.set_name(*tile_info_state.buffer, "tile-info-state");

	state_indices = create_buffer(device, domain,
	                              sizeof(InstanceIndices) * Limits::MaxPrimitives,
	                              borrow ? &borrow->state_indices : nullptr);
	device.set_name(*state_indices.buffer, "state-indices");

	span_info_offsets = create_buffer(device, domain,
	                                  sizeof(SpanInfoOffsets) * Limits::MaxPrimitives,
	                                  borrow ? &borrow->span_info_offsets : nullptr);
	device.set_name(*span_info_offsets.buffer, "span-info-offsets");

	span_info_jobs = create_buffer(device, domain,
	                               sizeof(SpanInterpolationJob) * Limits::MaxSpanSetups,
	                               borrow ? &borrow->span_info_jobs : nullptr);
	device.set_name(*span_info_jobs.buffer, "span-info-jobs");

	if (!borrow)
	{
		Vulkan::BufferViewCreateInfo info = {};
		info.buffer = span_info_jobs.buffer.get();
		info.format = VK_FORMAT_R16G16B16A16_UINT;
		info.range = span_info_jobs.buffer->get_create_info().size;
		span_info_jobs_view = device.create_buffer_view(info);
	}
}

Renderer::MappedBuffer Renderer::RenderBuffers::create_buffer(
		Vulkan::Device &device, Vulkan::BufferDomain domain, VkDeviceSize size,
		Renderer::MappedBuffer *borrow)
{
	Vulkan::BufferCreateInfo info = {};
	info.domain = domain;

	if (domain == Vulkan::BufferDomain::Device || domain == Vulkan::BufferDomain::LinkedDeviceHostPreferDevice)
	{
		info.usage = VK_BUFFER_USAGE_TRANSFER_SRC_BIT |
		             VK_BUFFER_USAGE_TRANSFER_DST_BIT |
		             VK_BUFFER_USAGE_STORAGE_BUFFER_BIT |
		             VK_BUFFER_USAGE_UNIFORM_TEXEL_BUFFER_BIT;
	}
	else if (borrow && borrow->is_host)
	{
		return *borrow;
	}
	else
	{
		info.usage = VK_BUFFER_USAGE_TRANSFER_SRC_BIT;
	}

	info.size = size;
	Renderer::MappedBuffer buffer;
	buffer.buffer = device.create_buffer(info);
	buffer.is_host = device.map_host_buffer(*buffer.buffer, 0) != nullptr;
	return buffer;
}

void Renderer::RenderBuffersUpdater::init(Vulkan::Device &device)
{
	gpu.init(device, Vulkan::BufferDomain::LinkedDeviceHostPreferDevice, nullptr);
	cpu.init(device, Vulkan::BufferDomain::Host, &gpu);
}

bool Renderer::init_internal_upscaling_factor(const RendererOptions &options)
{
	unsigned factor = options.upscaling_factor;
	if (!device || !rdram || !hidden_rdram)
	{
		LOGE("Renderer is not initialized.\n");
		return false;
	}

	caps.upscaling = factor;
	caps.super_sample_readback = options.super_sampled_readback;
	caps.super_sample_readback_dither = options.super_sampled_readback_dither;

	if (factor == 1)
	{
		upscaling_multisampled_hidden_rdram.reset();
		upscaling_reference_rdram.reset();
		upscaling_multisampled_rdram.reset();
		return true;
	}

	Vulkan::BufferCreateInfo info;
	info.domain = Vulkan::BufferDomain::Device;
	info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT;
	info.misc = Vulkan::BUFFER_MISC_ZERO_INITIALIZE_BIT;

	info.size = rdram_size;
	upscaling_reference_rdram = device->create_buffer(info);
	device->set_name(*upscaling_reference_rdram, "reference-rdram");

	info.size = rdram_size * factor * factor;
	// If we're super-sampling we'll need to carry forward a u8 writemask per unscaled pixel.
	// The resolve pass will conditionally write a resolved pixel to avoid potential race conditions.
	// We allocate 2 bits per pixel (color / depth write).
	// The SSAA resolve shader will convert this to a VRAM write mask if we also need to handle incoherent
	// RDRAM.
	if (caps.super_sample_readback)
		info.size += 2 * Limits::MaxWidth * Limits::MaxHeight / 8;

	upscaling_multisampled_rdram = device->create_buffer(info);
	device->set_name(*upscaling_multisampled_rdram, "multisampled-rdram");

	info.size = hidden_rdram->get_create_info().size * factor * factor;
	upscaling_multisampled_hidden_rdram = device->create_buffer(info);
	device->set_name(*upscaling_multisampled_hidden_rdram, "multisampled-hidden-rdram");

	{
		auto cmd = device->request_command_buffer();
		cmd->fill_buffer(*upscaling_multisampled_hidden_rdram, 0x03030303);
		cmd->barrier(VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_WRITE_BIT,
		             VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
		             VK_ACCESS_MEMORY_READ_BIT | VK_ACCESS_MEMORY_WRITE_BIT);
		device->submit(cmd);
	}

	return true;
}

void Renderer::set_rdram(Vulkan::Buffer *buffer, uint8_t *host_rdram, size_t offset, size_t size, bool coherent)
{
	rdram = buffer;
	rdram_offset = offset;
	rdram_size = size;
	is_host_coherent = coherent;
	device->set_name(*rdram, "rdram");

	if (!is_host_coherent)
	{
		assert(rdram_offset == 0);
		incoherent.host_rdram = host_rdram;

		// If we're not host coherent (missing VK_EXT_external_memory_host),
		// we need to create a staging RDRAM buffer which is used for the real RDRAM uploads.
		// RDRAM may be uploaded in a masked way (if GPU has pending writes), or direct copy (if no pending writes are outstanding).
		Vulkan::BufferCreateInfo info = {};
		info.size = size;
		info.domain = Vulkan::BufferDomain::Host;
		info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VK_BUFFER_USAGE_TRANSFER_SRC_BIT;
		incoherent.staging_rdram = device->create_buffer(info);
		device->set_name(*incoherent.staging_rdram, "staging-rdram");

		const auto div_round_up = [](size_t a, size_t b) -> size_t { return (a + b - 1) / b; };

		if (!rdram->get_allocation().is_host_allocation())
		{
			// If we cannot map RDRAM, we need a staging readback buffer.
			Vulkan::BufferCreateInfo readback_info = {};
			readback_info.domain = Vulkan::BufferDomain::CachedCoherentHostPreferCached;
			readback_info.size = rdram_size * Limits::NumSyncStates;
			readback_info.usage = VK_BUFFER_USAGE_TRANSFER_DST_BIT;
			incoherent.staging_readback = device->create_buffer(readback_info);
			device->set_name(*incoherent.staging_readback, "staging-readback");
			incoherent.staging_readback_pages = div_round_up(readback_info.size, ImplementationConstants::IncoherentPageSize);
		}

		incoherent.page_to_direct_copy.clear();
		incoherent.page_to_masked_copy.clear();
		incoherent.page_to_pending_readback.clear();

		auto packed_pages = div_round_up(size, ImplementationConstants::IncoherentPageSize * 32);
		incoherent.num_pages = div_round_up(size, ImplementationConstants::IncoherentPageSize);

		incoherent.page_to_direct_copy.resize(packed_pages);
		incoherent.page_to_masked_copy.resize(packed_pages);
		incoherent.page_to_pending_readback.resize(packed_pages);
		incoherent.pending_writes_for_page.reset(new std::atomic_uint32_t[incoherent.num_pages]);
		for (unsigned i = 0; i < incoherent.num_pages; i++)
			incoherent.pending_writes_for_page[i].store(0);
	}
	else
	{
		incoherent = {};
	}
}

void Renderer::set_hidden_rdram(Vulkan::Buffer *buffer)
{
	hidden_rdram = buffer;
	device->set_name(*hidden_rdram, "hidden-rdram");
}

void Renderer::set_tmem(Vulkan::Buffer *buffer)
{
	tmem = buffer;
	device->set_name(*tmem, "tmem");
}

void Renderer::flush_and_signal()
{
	flush_queues();
	submit_to_queue();
	assert(!stream.cmd);
}

void Renderer::set_color_framebuffer(uint32_t addr, uint32_t width, FBFormat fmt)
{
	if (fb.addr != addr || fb.width != width || fb.fmt != fmt)
		flush_queues();

	fb.addr = addr;
	fb.width = width;
	fb.fmt = fmt;
}

void Renderer::set_depth_framebuffer(uint32_t addr)
{
	if (fb.depth_addr != addr)
		flush_queues();

	fb.depth_addr = addr;
}

void Renderer::set_scissor_state(const ScissorState &state)
{
	stream.scissor_state = state;
}

void Renderer::set_static_rasterization_state(const StaticRasterizationState &state)
{
	stream.static_raster_state = state;
}

void Renderer::set_depth_blend_state(const DepthBlendState &state)
{
	stream.depth_blend_state = state;
}

void Renderer::draw_flat_primitive(TriangleSetup &setup)
{
	draw_shaded_primitive(setup, {});
}

static int normalize_dzpix(int dz)
{
	if (dz >= 0x8000)
		return 0x8000;
	else if (dz == 0)
		return 1;

	unsigned bit = 31 - leading_zeroes(dz);
	return 1 << (bit + 1);
}

static uint16_t dz_compress(int dz)
{
	int val = 0;
	if (dz & 0xff00)
		val |= 8;
	if (dz & 0xf0f0)
		val |= 4;
	if (dz & 0xcccc)
		val |= 2;
	if (dz & 0xaaaa)
		val |= 1;
	return uint16_t(val);
}

static void encode_rgb(uint8_t *rgba, uint32_t color)
{
	rgba[0] = uint8_t(color >> 24);
	rgba[1] = uint8_t(color >> 16);
	rgba[2] = uint8_t(color >> 8);
}

static void encode_alpha(uint8_t *rgba, uint32_t color)
{
	rgba[3] = uint8_t(color);
}

void Renderer::build_combiner_constants(DerivedSetup &setup, unsigned cycle) const
{
	auto &comb = stream.static_raster_state.combiner[cycle];
	auto &output = setup.constants[cycle];

	switch (comb.rgb.muladd)
	{
	case RGBMulAdd::Env:
		encode_rgb(output.muladd, constants.env_color);
		break;

	case RGBMulAdd::Primitive:
		encode_rgb(output.muladd, constants.primitive_color);
		break;

	default:
		break;
	}

	switch (comb.rgb.mulsub)
	{
	case RGBMulSub::Env:
		encode_rgb(output.mulsub, constants.env_color);
		break;

	case RGBMulSub::Primitive:
		encode_rgb(output.mulsub, constants.primitive_color);
		break;

	case RGBMulSub::ConvertK4:
		// Need to decode this specially since it's a 9-bit value.
		encode_rgb(output.mulsub, uint32_t(constants.convert[4]) << 8);
		break;

	case RGBMulSub::KeyCenter:
		output.mulsub[0] = constants.key_center[0];
		output.mulsub[1] = constants.key_center[1];
		output.mulsub[2] = constants.key_center[2];
		break;

	default:
		break;
	}

	switch (comb.rgb.mul)
	{
	case RGBMul::Primitive:
		encode_rgb(output.mul, constants.primitive_color);
		break;

	case RGBMul::Env:
		encode_rgb(output.mul, constants.env_color);
		break;

	case RGBMul::PrimitiveAlpha:
		encode_rgb(output.mul, 0x01010101 * ((constants.primitive_color) & 0xff));
		break;

	case RGBMul::EnvAlpha:
		encode_rgb(output.mul, 0x01010101 * ((constants.env_color) & 0xff));
		break;

	case RGBMul::PrimLODFrac:
		encode_rgb(output.mul, 0x01010101 * constants.prim_lod_frac);
		break;

	case RGBMul::ConvertK5:
		// Need to decode this specially since it's a 9-bit value.
		encode_rgb(output.mul, uint32_t(constants.convert[5]) << 8);
		break;

	case RGBMul::KeyScale:
		output.mul[0] = constants.key_scale[0];
		output.mul[1] = constants.key_scale[1];
		output.mul[2] = constants.key_scale[2];
		break;

	default:
		break;
	}

	switch (comb.rgb.add)
	{
	case RGBAdd::Primitive:
		encode_rgb(output.add, constants.primitive_color);
		break;

	case RGBAdd::Env:
		encode_rgb(output.add, constants.env_color);
		break;

	default:
		break;
	}

	switch (comb.alpha.muladd)
	{
	case AlphaAddSub::PrimitiveAlpha:
		encode_alpha(output.muladd, constants.primitive_color);
		break;

	case AlphaAddSub::EnvAlpha:
		encode_alpha(output.muladd, constants.env_color);
		break;

	default:
		break;
	}

	switch (comb.alpha.mulsub)
	{
	case AlphaAddSub::PrimitiveAlpha:
		encode_alpha(output.mulsub, constants.primitive_color);
		break;

	case AlphaAddSub::EnvAlpha:
		encode_alpha(output.mulsub, constants.env_color);
		break;

	default:
		break;
	}

	switch (comb.alpha.mul)
	{
	case AlphaMul::PrimitiveAlpha:
		encode_alpha(output.mul, constants.primitive_color);
		break;

	case AlphaMul::EnvAlpha:
		encode_alpha(output.mul, constants.env_color);
		break;

	case AlphaMul::PrimLODFrac:
		encode_alpha(output.mul, constants.prim_lod_frac);
		break;

	default:
		break;
	}

	switch (comb.alpha.add)
	{
	case AlphaAddSub::PrimitiveAlpha:
		encode_alpha(output.add, constants.primitive_color);
		break;

	case AlphaAddSub::EnvAlpha:
		encode_alpha(output.add, constants.env_color);
		break;

	default:
		break;
	}
}

DerivedSetup Renderer::build_derived_attributes(const AttributeSetup &attr) const
{
	DerivedSetup setup = {};
	if (constants.use_prim_depth)
	{
		setup.dz = constants.prim_dz;
		setup.dz_compressed = dz_compress(setup.dz);
	}
	else
	{
		int dzdx = attr.dzdx >> 16;
		int dzdy = attr.dzdy >> 16;
		int dzpix = (dzdx < 0 ? (~dzdx & 0x7fff) : dzdx) + (dzdy < 0 ? (~dzdy & 0x7fff) : dzdy);
		dzpix = normalize_dzpix(dzpix);
		setup.dz = dzpix;
		setup.dz_compressed = dz_compress(dzpix);
	}

	build_combiner_constants(setup, 0);
	build_combiner_constants(setup, 1);

	setup.fog_color[0] = uint8_t(constants.fog_color >> 24);
	setup.fog_color[1] = uint8_t(constants.fog_color >> 16);
	setup.fog_color[2] = uint8_t(constants.fog_color >> 8);
	setup.fog_color[3] = uint8_t(constants.fog_color >> 0);

	setup.blend_color[0] = uint8_t(constants.blend_color >> 24);
	setup.blend_color[1] = uint8_t(constants.blend_color >> 16);
	setup.blend_color[2] = uint8_t(constants.blend_color >> 8);
	setup.blend_color[3] = uint8_t(constants.blend_color >> 0);

	setup.fill_color = constants.fill_color;
	setup.min_lod = constants.min_level;

	for (unsigned i = 0; i < 4; i++)
		setup.convert_factors[i] = int16_t(constants.convert[i]);

	return setup;
}

static constexpr unsigned SUBPIXELS_Y = 4;

static int32_t clamp_int32(int64_t v)
{
	if (v < std::numeric_limits<int32_t>::min())
		return std::numeric_limits<int32_t>::min();
	else if (v > std::numeric_limits<int32_t>::max())
		return std::numeric_limits<int32_t>::max();
	else
		return int32_t(v);
}

static std::pair<int32_t, int32_t> interpolate_x(const TriangleSetup &setup, int y, bool flip, int scaling)
{
	// Interpolate in 64-bit so we are guaranteed to catch any overflow scenario.
	int64_t yh_interpolation_base = setup.yh & ~(SUBPIXELS_Y - 1);
	int64_t ym_interpolation_base = setup.ym;
	yh_interpolation_base *= scaling;
	ym_interpolation_base *= scaling;

	int64_t xh = scaling * setup.xh + int64_t(y - yh_interpolation_base) * setup.dxhdy;
	int64_t xm = scaling * setup.xm + int64_t(y - yh_interpolation_base) * setup.dxmdy;
	int64_t xl = scaling * setup.xl + int64_t(y - ym_interpolation_base) * setup.dxldy;
	if (y < scaling * setup.ym)
		xl = xm;

	int64_t xh_shifted = xh >> 15;
	int64_t xl_shifted = xl >> 15;

	int64_t xleft, xright;
	if (flip)
	{
		xleft = xh_shifted;
		xright = xl_shifted;
	}
	else
	{
		xleft = xl_shifted;
		xright = xh_shifted;
	}

	return { clamp_int32(xleft), clamp_int32(xright) };
}

unsigned Renderer::compute_conservative_max_num_tiles(const TriangleSetup &setup) const
{
	if (setup.yl <= setup.yh)
		return 0;

	int scaling = int(caps.upscaling);
	int start_y = setup.yh & ~(SUBPIXELS_Y - 1);
	int end_y = (setup.yl - 1) | (SUBPIXELS_Y - 1);

	start_y = std::max(int(stream.scissor_state.ylo), start_y);
	end_y = std::min(int(stream.scissor_state.yhi) - 1, end_y);
	start_y *= scaling;
	end_y *= scaling;

	// Y is clipped out, exit early.
	if (end_y < start_y)
		return 0;

	bool flip = (setup.flags & TRIANGLE_SETUP_FLIP_BIT) != 0;

	auto upper = interpolate_x(setup, start_y, flip, scaling);
	auto lower = interpolate_x(setup, end_y, flip, scaling);
	auto mid = upper;
	auto mid1 = upper;

	int ym = scaling * setup.ym;
	if (ym > start_y && ym < end_y)
	{
		mid = interpolate_x(setup, ym, flip, scaling);
		mid1 = interpolate_x(setup, ym - 1, flip, scaling);
	}

	// Robustness, check if we overflow the X rasterizer precision.
	// After shifting down, we should have 12 bits signed.
	// If we detect any overflow here we need to assume X range is scissor rect.
	// This really should never happen, but it's possible to write tests that intentionally trigger weird
	// overflow behavior that needs to be specially handled.
	// There might be freak scenarios where we cannot detect overflow since we're only sampling at 4 scanlines
	// and we have ~32 overflows happening at once,
	// So we need to interpolate in 64-bit to make this work.

	auto start_x = std::min(std::min(upper.first, lower.first), std::min(mid.first, mid1.first));
	auto end_x = std::max(std::max(upper.second, lower.second), std::max(mid.second, mid1.second));

	auto max_range_x = std::max(std::abs(start_x), std::abs(end_x));
	// Effective X range is [-2048, 2047], but just make it [-2047, 2047] to match binning shader.
	// If we interpolate X to something outside that range,
	// we must assume the entire X range will be covered.
	if (max_range_x > 2047 * scaling)
	{
		start_x = 0;
		end_x = 0x7fffffff;
	}

	start_x = std::max(start_x, scaling * (int(stream.scissor_state.xlo) >> 2));
	end_x = std::min(end_x, scaling * ((int(stream.scissor_state.xhi) + 3) >> 2) - 1);

	if (end_x < start_x)
		return 0;

	start_x /= ImplementationConstants::TileWidth;
	end_x /= ImplementationConstants::TileWidth;
	start_y /= (SUBPIXELS_Y * ImplementationConstants::TileHeight);
	end_y /= (SUBPIXELS_Y * ImplementationConstants::TileHeight);

	return (end_x - start_x + 1) * (end_y - start_y + 1);
}

static bool combiner_accesses_texel0(const CombinerInputs &inputs)
{
	return inputs.rgb.muladd == RGBMulAdd::Texel0 ||
	       inputs.rgb.mulsub == RGBMulSub::Texel0 ||
	       inputs.rgb.mul == RGBMul::Texel0 ||
	       inputs.rgb.add == RGBAdd::Texel0 ||
	       inputs.rgb.mul == RGBMul::Texel0Alpha ||
	       inputs.alpha.muladd == AlphaAddSub::Texel0Alpha ||
	       inputs.alpha.mulsub == AlphaAddSub::Texel0Alpha ||
	       inputs.alpha.mul == AlphaMul::Texel0Alpha ||
	       inputs.alpha.add == AlphaAddSub::Texel0Alpha;
}

static bool combiner_accesses_lod_frac(const CombinerInputs &inputs)
{
	return inputs.rgb.mul == RGBMul::LODFrac || inputs.alpha.mul == AlphaMul::LODFrac;
}

static bool combiner_accesses_texel1(const CombinerInputs &inputs)
{
	return inputs.rgb.muladd == RGBMulAdd::Texel1 ||
	       inputs.rgb.mulsub == RGBMulSub::Texel1 ||
	       inputs.rgb.mul == RGBMul::Texel1 ||
	       inputs.rgb.add == RGBAdd::Texel1 ||
	       inputs.rgb.mul == RGBMul::Texel1Alpha ||
	       inputs.alpha.muladd == AlphaAddSub::Texel1Alpha ||
	       inputs.alpha.mulsub == AlphaAddSub::Texel1Alpha ||
	       inputs.alpha.mul == AlphaMul::Texel1Alpha ||
	       inputs.alpha.add == AlphaAddSub::Texel1Alpha;
}

static bool combiner_uses_texel0(const StaticRasterizationState &state)
{
	// Texel0 can be safely used in cycle0 of CYCLE2 mode, or in cycle1 (only cycle) of CYCLE1 mode.
	if ((state.flags & RASTERIZATION_MULTI_CYCLE_BIT) != 0)
	{
		// In second cycle, Texel0 and Texel1 swap around ...
		return combiner_accesses_texel0(state.combiner[0]) ||
		       combiner_accesses_texel1(state.combiner[1]);
	}
	else
		return combiner_accesses_texel0(state.combiner[1]);
}

static bool combiner_uses_texel1(const StaticRasterizationState &state)
{
	// Texel1 can be safely used in cycle0 of CYCLE2 mode, and never in cycle1 mode.
	// Texel0 can be safely accessed in cycle1, which is an alias due to pipelining.
	if ((state.flags & RASTERIZATION_MULTI_CYCLE_BIT) != 0)
	{
		return combiner_accesses_texel1(state.combiner[0]) ||
		       combiner_accesses_texel0(state.combiner[1]);
	}
	else
		return false;
}

static bool combiner_uses_pipelined_texel1(const StaticRasterizationState &state)
{
	// If you access Texel1 in cycle1 mode, you end up reading the next pixel's color for whatever reason.
	if ((state.flags & RASTERIZATION_MULTI_CYCLE_BIT) == 0)
		return combiner_accesses_texel1(state.combiner[1]);
	else
		return false;
}

static bool combiner_uses_lod_frac(const StaticRasterizationState &state)
{
	if ((state.flags & RASTERIZATION_MULTI_CYCLE_BIT) != 0)
		return combiner_accesses_lod_frac(state.combiner[0]) || combiner_accesses_lod_frac(state.combiner[1]);
	else
		return false;
}

void Renderer::deduce_noise_state()
{
	auto &state = stream.static_raster_state;
	state.flags &= ~RASTERIZATION_NEED_NOISE_BIT;

	// Figure out if we need to seed noise variable for this primitive.
	if ((state.dither & 3) == 2 || ((state.dither >> 2) & 3) == 2)
	{
		state.flags |= RASTERIZATION_NEED_NOISE_BIT;
		return;
	}

	if ((state.flags & (RASTERIZATION_COPY_BIT | RASTERIZATION_FILL_BIT)) != 0)
		return;

	if ((state.flags & RASTERIZATION_MULTI_CYCLE_BIT) != 0)
	{
		if (state.combiner[0].rgb.muladd == RGBMulAdd::Noise)
			state.flags |= RASTERIZATION_NEED_NOISE_BIT;
	}
	else if (state.combiner[1].rgb.muladd == RGBMulAdd::Noise)
		state.flags |= RASTERIZATION_NEED_NOISE_BIT;

	if ((state.flags & (RASTERIZATION_ALPHA_TEST_BIT | RASTERIZATION_ALPHA_TEST_DITHER_BIT)) ==
	    (RASTERIZATION_ALPHA_TEST_BIT | RASTERIZATION_ALPHA_TEST_DITHER_BIT))
	{
		state.flags |= RASTERIZATION_NEED_NOISE_BIT;
	}
}

static RGBMulAdd normalize_combiner(RGBMulAdd muladd)
{
	switch (muladd)
	{
	case RGBMulAdd::Noise:
	case RGBMulAdd::Texel0:
	case RGBMulAdd::Texel1:
	case RGBMulAdd::Combined:
	case RGBMulAdd::One:
	case RGBMulAdd::Shade:
		return muladd;

	default:
		return RGBMulAdd::Zero;
	}
}

static RGBMulSub normalize_combiner(RGBMulSub mulsub)
{
	switch (mulsub)
	{
	case RGBMulSub::Combined:
	case RGBMulSub::Texel0:
	case RGBMulSub::Texel1:
	case RGBMulSub::Shade:
	case RGBMulSub::ConvertK4:
		return mulsub;

	default:
		return RGBMulSub::Zero;
	}
}

static RGBMul normalize_combiner(RGBMul mul)
{
	switch (mul)
	{
	case RGBMul::Combined:
	case RGBMul::CombinedAlpha:
	case RGBMul::Texel0:
	case RGBMul::Texel1:
	case RGBMul::Texel0Alpha:
	case RGBMul::Texel1Alpha:
	case RGBMul::Shade:
	case RGBMul::ShadeAlpha:
	case RGBMul::LODFrac:
	case RGBMul::ConvertK5:
		return mul;

	default:
		return RGBMul::Zero;
	}
}

static RGBAdd normalize_combiner(RGBAdd add)
{
	switch (add)
	{
	case RGBAdd::Texel0:
	case RGBAdd::Texel1:
	case RGBAdd::Combined:
	case RGBAdd::One:
	case RGBAdd::Shade:
		return add;

	default:
		return RGBAdd::Zero;
	}
}

static AlphaAddSub normalize_combiner(AlphaAddSub addsub)
{
	switch (addsub)
	{
	case AlphaAddSub::CombinedAlpha:
	case AlphaAddSub::Texel0Alpha:
	case AlphaAddSub::Texel1Alpha:
	case AlphaAddSub::ShadeAlpha:
	case AlphaAddSub::One:
		return addsub;

	default:
		return AlphaAddSub::Zero;
	}
}

static AlphaMul normalize_combiner(AlphaMul mul)
{
	switch (mul)
	{
	case AlphaMul::LODFrac:
	case AlphaMul::Texel0Alpha:
	case AlphaMul::Texel1Alpha:
	case AlphaMul::ShadeAlpha:
		return mul;

	default:
		return AlphaMul::Zero;
	}
}

static void normalize_combiner(CombinerInputsRGB &comb)
{
	comb.muladd = normalize_combiner(comb.muladd);
	comb.mulsub = normalize_combiner(comb.mulsub);
	comb.mul = normalize_combiner(comb.mul);
	comb.add = normalize_combiner(comb.add);
}

static void normalize_combiner(CombinerInputsAlpha &comb)
{
	comb.muladd = normalize_combiner(comb.muladd);
	comb.mulsub = normalize_combiner(comb.mulsub);
	comb.mul = normalize_combiner(comb.mul);
	comb.add = normalize_combiner(comb.add);
}

static void normalize_combiner(CombinerInputs &comb)
{
	normalize_combiner(comb.rgb);
	normalize_combiner(comb.alpha);
}

StaticRasterizationState Renderer::normalize_static_state(StaticRasterizationState state)
{
	if ((state.flags & RASTERIZATION_FILL_BIT) != 0)
	{
		state = {};
		state.flags = RASTERIZATION_FILL_BIT;
		return state;
	}

	if ((state.flags & RASTERIZATION_COPY_BIT) != 0)
	{
		auto flags = state.flags &
		             (RASTERIZATION_COPY_BIT |
		              RASTERIZATION_TLUT_BIT |
		              RASTERIZATION_TLUT_TYPE_BIT |
		              RASTERIZATION_USES_TEXEL0_BIT |
		              RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT |
		              RASTERIZATION_TEX_LOD_ENABLE_BIT |
		              RASTERIZATION_DETAIL_LOD_ENABLE_BIT |
		              RASTERIZATION_ALPHA_TEST_BIT);

		auto fmt = state.texture_fmt;
		auto siz = state.texture_size;
		state = {};
		state.flags = flags;
		state.texture_fmt = fmt;
		state.texture_size = siz;
		return state;
	}

	if ((state.flags & (RASTERIZATION_MULTI_CYCLE_BIT | RASTERIZATION_USES_PIPELINED_TEXEL1_BIT)) == 0)
		state.flags &= ~(RASTERIZATION_BILERP_1_BIT | RASTERIZATION_CONVERT_ONE_BIT);

	normalize_combiner(state.combiner[0]);
	normalize_combiner(state.combiner[1]);
	return state;
}

void Renderer::deduce_static_texture_state(unsigned tile, unsigned max_lod_level)
{
	auto &state = stream.static_raster_state;
	state.flags &= ~RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT;
	state.texture_size = 0;
	state.texture_fmt = 0;

	if ((state.flags & RASTERIZATION_FILL_BIT) != 0)
		return;

	auto fmt = tiles[tile].meta.fmt;
	auto siz = tiles[tile].meta.size;

	if ((state.flags & RASTERIZATION_COPY_BIT) == 0)
	{
		// If all tiles we sample have the same fmt and size (common case), we can use a static variant.
		bool uses_texel0 = combiner_uses_texel0(state);
		bool uses_texel1 = combiner_uses_texel1(state);
		bool uses_pipelined_texel1 = combiner_uses_pipelined_texel1(state);
		bool uses_lod_frac = combiner_uses_lod_frac(state);

		if (uses_texel1 && (state.flags & RASTERIZATION_CONVERT_ONE_BIT) != 0)
			uses_texel0 = true;

		state.flags &= ~(RASTERIZATION_USES_TEXEL0_BIT |
		                 RASTERIZATION_USES_TEXEL1_BIT |
		                 RASTERIZATION_USES_PIPELINED_TEXEL1_BIT |
		                 RASTERIZATION_USES_LOD_BIT);
		if (uses_texel0)
			state.flags |= RASTERIZATION_USES_TEXEL0_BIT;
		if (uses_texel1)
			state.flags |= RASTERIZATION_USES_TEXEL1_BIT;
		if (uses_pipelined_texel1)
			state.flags |= RASTERIZATION_USES_PIPELINED_TEXEL1_BIT;
		if (uses_lod_frac || (state.flags & RASTERIZATION_TEX_LOD_ENABLE_BIT) != 0)
			state.flags |= RASTERIZATION_USES_LOD_BIT;

		if (!uses_texel0 && !uses_texel1 && !uses_pipelined_texel1)
			return;

		bool use_lod = (state.flags & RASTERIZATION_TEX_LOD_ENABLE_BIT) != 0;
		bool use_detail = (state.flags & RASTERIZATION_DETAIL_LOD_ENABLE_BIT) != 0;

		bool uses_physical_texel1 = uses_texel1 &&
		                            ((state.flags & RASTERIZATION_CONVERT_ONE_BIT) == 0 ||
		                             (state.flags & RASTERIZATION_BILERP_1_BIT) != 0);

		if (!use_lod)
			max_lod_level = uses_physical_texel1 ? 1 : 0;
		if (use_detail)
			max_lod_level++;
		max_lod_level = std::min(max_lod_level, 7u);

		for (unsigned i = 1; i <= max_lod_level; i++)
		{
			auto &t = tiles[(tile + i) & 7].meta;
			if (t.fmt != fmt)
				return;
			if (t.size != siz)
				return;
		}
	}

	// We have a static format.
	state.flags |= RASTERIZATION_USE_STATIC_TEXTURE_SIZE_FORMAT_BIT;
	state.texture_fmt = uint32_t(fmt);
	state.texture_size = uint32_t(siz);
}

void Renderer::fixup_triangle_setup(TriangleSetup &setup) const
{
	// If YM is lower than the first sub-scanline we rasterize, we will never observe YM at all.
	// To account for this, fixup here so that YM is out of range.
	// No known content actually triggers this, but some public tests triggered it.
	int start_y = setup.yh & ~(SUBPIXELS_Y - 1);
	if (setup.ym < start_y)
		setup.ym = std::numeric_limits<int16_t>::max();

	if ((stream.static_raster_state.flags & RASTERIZATION_INTERLACE_FIELD_BIT) != 0)
	{
		setup.flags |= (stream.static_raster_state.flags & RASTERIZATION_INTERLACE_FIELD_BIT) ?
		               TRIANGLE_SETUP_INTERLACE_FIELD_BIT : 0;
		setup.flags |= (stream.static_raster_state.flags & RASTERIZATION_INTERLACE_KEEP_ODD_BIT) ?
		               TRIANGLE_SETUP_INTERLACE_KEEP_ODD_BIT : 0;
	}
}

void Renderer::draw_shaded_primitive(TriangleSetup &setup, const AttributeSetup &attr)
{
	fixup_triangle_setup(setup);

	unsigned num_tiles = compute_conservative_max_num_tiles(setup);

#if 0
	// Don't exit early, throws off seeding of noise channels.
	if (!num_tiles)
		return;
#endif

	if (!caps.ubershader)
		stream.max_shaded_tiles += num_tiles;

	update_deduced_height(setup);
	stream.span_info_offsets.add(allocate_span_jobs(setup));
	stream.triangle_setup.add(setup);

	if (constants.use_prim_depth)
	{
		auto tmp_attr = attr;
		tmp_attr.z = constants.prim_depth;
		tmp_attr.dzdx = 0;
		tmp_attr.dzde = 0;
		tmp_attr.dzdy = 0;
		stream.attribute_setup.add(tmp_attr);
	}
	else
	{
		stream.attribute_setup.add(attr);
	}

	stream.derived_setup.add(build_derived_attributes(attr));
	stream.scissor_setup.add(stream.scissor_state);

	deduce_static_texture_state(setup.tile & 7, setup.tile >> 3);
	deduce_noise_state();

	InstanceIndices indices = {};
	indices.static_index = stream.static_raster_state_cache.add(normalize_static_state(stream.static_raster_state));
	indices.depth_blend_index = stream.depth_blend_state_cache.add(stream.depth_blend_state);
	indices.tile_instance_index = uint8_t(stream.tmem_upload_infos.size());
	for (unsigned i = 0; i < 8; i++)
		indices.tile_indices[i] = stream.tile_info_state_cache.add(tiles[i]);
	stream.state_indices.add(indices);

	fb.color_write_pending = true;
	if (stream.depth_blend_state.flags & DEPTH_BLEND_DEPTH_UPDATE_BIT)
		fb.depth_write_pending = true;
	pending_primitives++;

	if (need_flush())
		flush_queues();
}

SpanInfoOffsets Renderer::allocate_span_jobs(const TriangleSetup &setup)
{
	int min_active_sub_scanline = std::max(int(setup.yh), int(stream.scissor_state.ylo));
	int min_active_line = min_active_sub_scanline >> 2;

	int max_active_sub_scanline = std::min(setup.yl - 1, int(stream.scissor_state.yhi) - 1);
	int max_active_line = max_active_sub_scanline >> 2;

	if (max_active_line < min_active_line)
		return { 0, 0, -1, 0 };

	// Need to poke into next scanline validation for certain workarounds.
	int height = std::max(max_active_line - min_active_line + 2, 0);
	height = std::min(height, 1024);

	int num_jobs = (height + ImplementationConstants::DefaultWorkgroupSize - 1) / ImplementationConstants::DefaultWorkgroupSize;

	SpanInfoOffsets offsets = {};
	offsets.offset = uint32_t(stream.span_info_jobs.size()) * ImplementationConstants::DefaultWorkgroupSize;
	offsets.ylo = min_active_line;
	offsets.yhi = max_active_line;

	for (int i = 0; i < num_jobs; i++)
	{
		SpanInterpolationJob interpolation_job = {};
		interpolation_job.primitive_index = uint32_t(stream.triangle_setup.size());
		interpolation_job.base_y = min_active_line + ImplementationConstants::DefaultWorkgroupSize * i;
		interpolation_job.max_y = max_active_line + 1;
		stream.span_info_jobs.add(interpolation_job);
	}
	return offsets;
}

void Renderer::update_deduced_height(const TriangleSetup &setup)
{
	int max_active_sub_scanline = std::min(setup.yl - 1, int(stream.scissor_state.yhi) - 1);
	int max_active_line = max_active_sub_scanline >> 2;
	int height = std::max(max_active_line + 1, 0);
	fb.deduced_height = std::max(fb.deduced_height, uint32_t(height));
}

bool Renderer::need_flush() const
{
	bool cache_full =
			stream.static_raster_state_cache.full() ||
			stream.depth_blend_state_cache.full() ||
			(stream.tile_info_state_cache.size() + 8 > Limits::MaxTileInfoStates);

	bool triangle_full =
			stream.triangle_setup.full();
	bool span_info_full =
			(stream.span_info_jobs.size() * ImplementationConstants::DefaultWorkgroupSize + Limits::MaxHeight > Limits::MaxSpanSetups);
	bool max_shaded_tiles =
			(stream.max_shaded_tiles + caps.max_tiles_x * caps.max_tiles_y > caps.max_num_tile_instances);

#ifdef VULKAN_DEBUG
	if (cache_full)
		LOGI("Cache is full.\n");
	if (triangle_full)
		LOGI("Triangle is full.\n");
	if (span_info_full)
		LOGI("Span info is full.\n");
	if (max_shaded_tiles)
		LOGI("Shaded tiles is full.\n");
#endif

	return cache_full || triangle_full || span_info_full || max_shaded_tiles;
}

template <typename Cache>
void Renderer::RenderBuffersUpdater::upload(Vulkan::CommandBuffer &cmd, Vulkan::Device &device,
                                            const MappedBuffer &gpu, const MappedBuffer &cpu, const Cache &cache,
                                            bool &did_upload)
{
	if (!cache.empty())
	{
		memcpy(device.map_host_buffer(*cpu.buffer, Vulkan::MEMORY_ACCESS_WRITE_BIT), cache.data(), cache.byte_size());
		device.unmap_host_buffer(*cpu.buffer, Vulkan::MEMORY_ACCESS_WRITE_BIT);
		if (gpu.buffer != cpu.buffer)
		{
			cmd.copy_buffer(*gpu.buffer, 0, *cpu.buffer, 0, cache.byte_size());
			did_upload = true;
		}
	}
}

void Renderer::RenderBuffersUpdater::upload(Vulkan::Device &device, const Renderer::StreamCaches &caches,
                                            Vulkan::CommandBuffer &cmd)
{
	bool did_upload = false;

	upload(cmd, device, gpu.triangle_setup, cpu.triangle_setup, caches.triangle_setup, did_upload);
	upload(cmd, device, gpu.attribute_setup, cpu.attribute_setup, caches.attribute_setup, did_upload);
	upload(cmd, device, gpu.derived_setup, cpu.derived_setup, caches.derived_setup, did_upload);
	upload(cmd, device, gpu.scissor_setup, cpu.scissor_setup, caches.scissor_setup, did_upload);

	upload(cmd, device, gpu.static_raster_state, cpu.static_raster_state, caches.static_raster_state_cache, did_upload);
	upload(cmd, device, gpu.depth_blend_state, cpu.depth_blend_state, caches.depth_blend_state_cache, did_upload);
	upload(cmd, device, gpu.tile_info_state, cpu.tile_info_state, caches.tile_info_state_cache, did_upload);

	upload(cmd, device, gpu.state_indices, cpu.state_indices, caches.state_indices, did_upload);
	upload(cmd, device, gpu.span_info_offsets, cpu.span_info_offsets, caches.span_info_offsets, did_upload);
	upload(cmd, device, gpu.span_info_jobs, cpu.span_info_jobs, caches.span_info_jobs, did_upload);

	if (did_upload)
	{
		cmd.barrier(VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_READ_BIT);
	}
}

void Renderer::update_tmem_instances(Vulkan::CommandBuffer &cmd)
{
	cmd.begin_region("tmem-update");
	cmd.set_storage_buffer(0, 0, *rdram, rdram_offset, rdram_size);
	cmd.set_storage_buffer(0, 1, *tmem);
	cmd.set_storage_buffer(0, 2, *tmem_instances);

	memcpy(cmd.allocate_typed_constant_data<UploadInfo>(1, 0, stream.tmem_upload_infos.size()),
	       stream.tmem_upload_infos.data(),
	       stream.tmem_upload_infos.size() * sizeof(UploadInfo));

	auto count = uint32_t(stream.tmem_upload_infos.size());

#ifdef PARALLEL_RDP_SHADER_DIR
	cmd.set_program("rdp://tmem_update.comp", {{ "DEBUG_ENABLE", debug_channel ? 1 : 0 }});
#else
	cmd.set_program(shader_bank->tmem_update);
#endif

	cmd.push_constants(&count, 0, sizeof(count));
	cmd.set_specialization_constant_mask(1);
	cmd.set_specialization_constant(0, ImplementationConstants::DefaultWorkgroupSize);

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 2)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
	cmd.dispatch(2048 / ImplementationConstants::DefaultWorkgroupSize, 1, 1);
	if (caps.timestamp >= 2)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts),
		                               "tmem-update", std::to_string(stream.tmem_upload_infos.size()));
	}
	cmd.end_region();
}

void Renderer::submit_span_setup_jobs(Vulkan::CommandBuffer &cmd, bool upscale)
{
	cmd.begin_region("span-setup");
	auto &instance = buffer_instances[buffer_instance];
	cmd.set_storage_buffer(0, 0, *instance.gpu.triangle_setup.buffer);
	cmd.set_storage_buffer(0, 1, *instance.gpu.attribute_setup.buffer);
	cmd.set_storage_buffer(0, 2, *instance.gpu.scissor_setup.buffer);
	cmd.set_storage_buffer(0, 3, *span_setups);

#ifdef PARALLEL_RDP_SHADER_DIR
	cmd.set_program("rdp://span_setup.comp", {{ "DEBUG_ENABLE", debug_channel ? 1 : 0 }});
#else
	cmd.set_program(shader_bank->span_setup);
#endif

	cmd.set_buffer_view(1, 0, *instance.gpu.span_info_jobs_view);
	cmd.set_specialization_constant_mask(3);
	cmd.set_specialization_constant(0, (upscale ? caps.upscaling : 1) * ImplementationConstants::DefaultWorkgroupSize);
	cmd.set_specialization_constant(1, upscale ? trailing_zeroes(caps.upscaling) : 0u);

	Vulkan::QueryPoolHandle begin_ts, end_ts;
	if (caps.timestamp >= 2)
		begin_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
	cmd.dispatch(stream.span_info_jobs.size(), 1, 1);
	if (caps.timestamp >= 2)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(begin_ts), std::move(end_ts), "span-info-jobs");
	}
	cmd.end_region();
}

void Renderer::clear_indirect_buffer(Vulkan::CommandBuffer &cmd)
{
	cmd.begin_region("clear-indirect-buffer");

#ifdef PARALLEL_RDP_SHADER_DIR
	cmd.set_program("rdp://clear_indirect_buffer.comp");
#else
	cmd.set_program(shader_bank->clear_indirect_buffer);
#endif

	cmd.set_storage_buffer(0, 0, *indirect_dispatch_buffer);

	static_assert((Limits::MaxStaticRasterizationStates % ImplementationConstants::DefaultWorkgroupSize) == 0, "MaxStaticRasterizationStates does not align.");
	cmd.set_specialization_constant_mask(1);
	cmd.set_specialization_constant(0, ImplementationConstants::DefaultWorkgroupSize);
	cmd.dispatch(Limits::MaxStaticRasterizationStates / ImplementationConstants::DefaultWorkgroupSize, 1, 1);
	cmd.end_region();
}

void Renderer::submit_rasterization(Vulkan::CommandBuffer &cmd, Vulkan::Buffer &tmem, bool upscaling)
{
	cmd.begin_region("rasterization");
	auto &instance = buffer_instances[buffer_instance];

	cmd.set_storage_buffer(0, 0, *instance.gpu.triangle_setup.buffer);
	cmd.set_storage_buffer(0, 1, *instance.gpu.attribute_setup.buffer);
	cmd.set_storage_buffer(0, 2, *instance.gpu.derived_setup.buffer);
	cmd.set_storage_buffer(0, 3, *instance.gpu.static_raster_state.buffer);
	cmd.set_storage_buffer(0, 4, *instance.gpu.state_indices.buffer);
	cmd.set_storage_buffer(0, 5, *instance.gpu.span_info_offsets.buffer);
	cmd.set_storage_buffer(0, 6, *span_setups);
	cmd.set_storage_buffer(0, 7, tmem);
	cmd.set_storage_buffer(0, 8, *instance.gpu.tile_info_state.buffer);

	cmd.set_storage_buffer(0, 9, *per_tile_shaded_color);
	cmd.set_storage_buffer(0, 10, *per_tile_shaded_depth);
	cmd.set_storage_buffer(0, 11, *per_tile_shaded_shaded_alpha);
	cmd.set_storage_buffer(0, 12, *per_tile_shaded_coverage);

	auto *global_fb_info = cmd.allocate_typed_constant_data<GlobalFBInfo>(2, 0, 1);
	switch (fb.fmt)
	{
	case FBFormat::I4:
		global_fb_info->fb_size = 0;
		global_fb_info->dx_mask = 0;
		global_fb_info->dx_shift = 0;
		break;

	case FBFormat::I8:
		global_fb_info->fb_size = 1;
		global_fb_info->dx_mask = ~7u;
		global_fb_info->dx_shift = 3;
		break;

	case FBFormat::RGBA5551:
	case FBFormat::IA88:
		global_fb_info->fb_size = 2;
		global_fb_info->dx_mask = ~3u;
		global_fb_info->dx_shift = 2;
		break;

	case FBFormat::RGBA8888:
		global_fb_info->fb_size = 4;
		global_fb_info->dx_shift = ~1u;
		global_fb_info->dx_shift = 1;
		break;
	}

	global_fb_info->base_primitive_index = base_primitive_index;

#ifdef PARALLEL_RDP_SHADER_DIR
	cmd.set_program("rdp://rasterizer.comp", {
		{ "DEBUG_ENABLE", debug_channel ? 1 : 0 },
		{ "SMALL_TYPES", caps.supports_small_integer_arithmetic ? 1 : 0 },
	});
#else
	cmd.set_program(shader_bank->rasterizer);
#endif

	cmd.set_specialization_constant(0, ImplementationConstants::TileWidth);
	cmd.set_specialization_constant(1, ImplementationConstants::TileHeight);

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 2)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);

	uint32_t scale_log2_bit = (upscaling ? trailing_zeroes(caps.upscaling) : 0u) << RASTERIZATION_UPSCALING_LOG2_BIT_OFFSET;

	for (size_t i = 0; i < stream.static_raster_state_cache.size(); i++)
	{
		cmd.set_storage_buffer(1, 0, *tile_work_list,
		                       i * sizeof(TileRasterWork) * caps.max_num_tile_instances,
		                       sizeof(TileRasterWork) * caps.max_num_tile_instances);

		auto &state = stream.static_raster_state_cache.data()[i];
		cmd.set_specialization_constant(2, state.flags | RASTERIZATION_USE_SPECIALIZATION_CONSTANT_BIT | scale_log2_bit);
		cmd.set_specialization_constant(3, state.combiner[0].rgb);
		cmd.set_specialization_constant(4, state.combiner[0].alpha);
		cmd.set_specialization_constant(5, state.combiner[1].rgb);
		cmd.set_specialization_constant(6, state.combiner[1].alpha);

		cmd.set_specialization_constant(7, state.dither |
		                                   (state.texture_size << 8u) |
		                                   (state.texture_fmt << 16u));
		cmd.set_specialization_constant_mask(0xff);

		if (!caps.force_sync && !cmd.flush_pipeline_state_without_blocking())
		{
			Vulkan::DeferredPipelineCompile compile;
			cmd.extract_pipeline_state(compile);
			if (pending_async_pipelines.count(compile.hash) == 0)
			{
				pending_async_pipelines.insert(compile.hash);
				pipeline_worker->push(std::move(compile));
			}
			cmd.set_specialization_constant_mask(7);
			cmd.set_specialization_constant(2, scale_log2_bit);
		}

		cmd.dispatch_indirect(*indirect_dispatch_buffer, 4 * sizeof(uint32_t) * i);
	}

	if (caps.timestamp >= 2)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts), "shading");
	}
	cmd.end_region();
}

void Renderer::submit_tile_binning_combined(Vulkan::CommandBuffer &cmd, bool upscale)
{
	cmd.begin_region("tile-binning-combined");
	auto &instance = buffer_instances[buffer_instance];
	cmd.set_storage_buffer(0, 0, *instance.gpu.triangle_setup.buffer);
	cmd.set_storage_buffer(0, 1, *instance.gpu.scissor_setup.buffer);
	cmd.set_storage_buffer(0, 2, *instance.gpu.state_indices.buffer);
	cmd.set_storage_buffer(0, 3, *tile_binning_buffer);
	cmd.set_storage_buffer(0, 4, *tile_binning_buffer_coarse);

	if (!caps.ubershader)
	{
		cmd.set_storage_buffer(0, 5, *per_tile_offsets);
		cmd.set_storage_buffer(0, 6, *indirect_dispatch_buffer);
		cmd.set_storage_buffer(0, 7, *tile_work_list);
	}

	cmd.set_specialization_constant_mask(0x7f);
	cmd.set_specialization_constant(1, ImplementationConstants::TileWidth);
	cmd.set_specialization_constant(2, ImplementationConstants::TileHeight);
	cmd.set_specialization_constant(3, Limits::MaxPrimitives);
	cmd.set_specialization_constant(4, upscale ? caps.max_width : Limits::MaxWidth);
	cmd.set_specialization_constant(5, caps.max_num_tile_instances);
	cmd.set_specialization_constant(6, upscale ? caps.upscaling : 1u);

	struct PushData
	{
		uint32_t width, height;
		uint32_t num_primitives;
	} push = {};
	push.width = fb.width;
	push.height = fb.deduced_height;

	if (upscale)
	{
		push.width *= caps.upscaling;
		push.height *= caps.upscaling;
	}

	push.num_primitives = uint32_t(stream.triangle_setup.size());
	unsigned num_primitives_32 = (push.num_primitives + 31) / 32;

	cmd.push_constants(&push, 0, sizeof(push));

	auto &features = device->get_device_features();
	uint32_t subgroup_size = features.subgroup_properties.subgroupSize;

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 2)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);

	if (caps.subgroup_tile_binning)
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://tile_binning_combined.comp", {
			{ "DEBUG_ENABLE", debug_channel ? 1 : 0 },
			{ "SUBGROUP", 1 },
			{ "UBERSHADER", int(caps.ubershader) },
			{ "SMALL_TYPES", caps.supports_small_integer_arithmetic ? 1 : 0 },
		});
#else
		cmd.set_program(shader_bank->tile_binning_combined);
#endif

		if (supports_subgroup_size_control(32, subgroup_size))
		{
			cmd.enable_subgroup_size_control(true);
			cmd.set_subgroup_size_log2(true, 5, trailing_zeroes(subgroup_size));
		}
	}
	else
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://tile_binning_combined.comp", {
			{ "DEBUG_ENABLE", debug_channel ? 1 : 0 },
			{ "SUBGROUP", 0 },
			{ "UBERSHADER", int(caps.ubershader) },
			{ "SMALL_TYPES", caps.supports_small_integer_arithmetic ? 1 : 0 },
		});
#else
		cmd.set_program(shader_bank->tile_binning_combined);
#endif

		subgroup_size = 32;
	}

	cmd.set_specialization_constant(0, subgroup_size);
	unsigned meta_tiles_x = 8;
	unsigned meta_tiles_y = subgroup_size / meta_tiles_x;
	unsigned num_tiles_x = (push.width + ImplementationConstants::TileWidth - 1) / ImplementationConstants::TileWidth;
	unsigned num_tiles_y = (push.height + ImplementationConstants::TileHeight - 1) / ImplementationConstants::TileHeight;
	unsigned num_meta_tiles_x = (num_tiles_x + meta_tiles_x - 1) / meta_tiles_x;
	unsigned num_meta_tiles_y = (num_tiles_y + meta_tiles_y - 1) / meta_tiles_y;
	cmd.dispatch(num_primitives_32, num_meta_tiles_x, num_meta_tiles_y);

	if (caps.timestamp >= 2)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts), "tile-binning");
	}

	cmd.enable_subgroup_size_control(false);
	cmd.end_region();
}

void Renderer::submit_update_upscaled_domain_external(Vulkan::CommandBuffer &cmd,
                                                      unsigned addr, unsigned length, unsigned pixel_size_log2)
{
	submit_update_upscaled_domain(cmd, ResolveStage::Pre, addr, addr, length, 1, pixel_size_log2);
}

void Renderer::submit_update_upscaled_domain(Vulkan::CommandBuffer &cmd, ResolveStage stage,
                                             unsigned addr, unsigned depth_addr,
                                             unsigned width, unsigned height,
                                             unsigned pixel_size_log2)
{
#ifdef PARALLEL_RDP_SHADER_DIR
	if (stage == ResolveStage::Pre)
		cmd.set_program("rdp://update_upscaled_domain_pre.comp");
	else if (stage == ResolveStage::Post)
		cmd.set_program("rdp://update_upscaled_domain_post.comp");
	else
		cmd.set_program("rdp://update_upscaled_domain_resolve.comp");
#else
	if (stage == ResolveStage::Pre)
		cmd.set_program(shader_bank->update_upscaled_domain_pre);
	else if (stage == ResolveStage::Post)
		cmd.set_program(shader_bank->update_upscaled_domain_post);
	else
		cmd.set_program(shader_bank->update_upscaled_domain_resolve);
#endif

	unsigned num_pixels = width * height;

	if (stage != ResolveStage::SSAAResolve)
	{
		// Ensure that we always process entire words, thus we avoid having to do weird swizzles,
		// and memory access patterns are linear with gl_GlobalInvocationID.x.
		addr &= ~3u;
		depth_addr &= ~3u;
		unsigned align_pixels = 4u >> pixel_size_log2;
		num_pixels = (num_pixels + align_pixels - 1u) & ~(align_pixels - 1u);
	}

	cmd.set_storage_buffer(0, 0, *rdram, rdram_offset,
	                       (stage == ResolveStage::SSAAResolve && !is_host_coherent ? 2 : 1) * rdram_size);
	cmd.set_storage_buffer(0, 1, *hidden_rdram);
	cmd.set_storage_buffer(0, 2, *upscaling_reference_rdram);
	cmd.set_storage_buffer(0, 3, *upscaling_multisampled_rdram);
	cmd.set_storage_buffer(0, 4, *upscaling_multisampled_hidden_rdram);

	cmd.set_specialization_constant_mask(0x7f);
	cmd.set_specialization_constant(0, uint32_t(rdram_size));
	cmd.set_specialization_constant(1, pixel_size_log2);
	cmd.set_specialization_constant(2, int(addr == depth_addr));
	cmd.set_specialization_constant(3, ImplementationConstants::DefaultWorkgroupSize);
	cmd.set_specialization_constant(4, caps.upscaling * caps.upscaling);
	if (stage == ResolveStage::SSAAResolve)
	{
		cmd.set_specialization_constant(5, uint32_t(caps.super_sample_readback_dither));
		cmd.set_specialization_constant(6, uint32_t(!is_host_coherent));
	}

	uint32_t num_workgroups_x, num_workgroups_y;

	if (stage == ResolveStage::SSAAResolve)
	{
		num_workgroups_x =
				(width + ImplementationConstants::DefaultWorkgroupSize - 1) /
				ImplementationConstants::DefaultWorkgroupSize;
		num_workgroups_y = height;
	}
	else
	{
		num_workgroups_x =
				(num_pixels + ImplementationConstants::DefaultWorkgroupSize - 1) /
				ImplementationConstants::DefaultWorkgroupSize;
		num_workgroups_y = 1;
	}

	struct Push
	{
		uint32_t pixels;
		uint32_t fb_addr, fb_depth_addr;
		uint32_t width, height;
	} push = {};
	push.pixels = num_pixels;
	push.fb_addr = addr >> pixel_size_log2;
	push.fb_depth_addr = depth_addr >> 1;
	push.width = width;
	push.height = height;

	cmd.push_constants(&push, 0, sizeof(push));

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 2 && stage == ResolveStage::SSAAResolve)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
	cmd.dispatch(num_workgroups_x, num_workgroups_y, 1);
	if (caps.timestamp >= 2 && stage == ResolveStage::SSAAResolve)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts), "ssaa-resolve");
	}
}

void Renderer::submit_clear_super_sample_write_mask(Vulkan::CommandBuffer &cmd, unsigned width, unsigned height)
{
	// Pack 4x4 block of pixel's writemasks in one u32. Allows for one depth_blend workgroup to target a single u32
	// in all cases, which is nice :)
	unsigned blocks_x = (width + 3) / 4;
	unsigned blocks_y = (height + 3) / 4;
	unsigned num_words = blocks_x * blocks_y;
	unsigned num_workgroups = (num_words + ImplementationConstants::DefaultWorkgroupSize - 1) /
	                          ImplementationConstants::DefaultWorkgroupSize;

#ifdef PARALLEL_RDP_SHADER_DIR
	cmd.set_program("rdp://clear_super_sampled_write_mask.comp");
#else
	cmd.set_program(shader_bank->clear_super_sampled_write_mask);
#endif

	cmd.set_storage_buffer(0, 0, *upscaling_multisampled_rdram, rdram_size * caps.upscaling * caps.upscaling,
	                       2 * Limits::MaxWidth * Limits::MaxHeight / 8);

	cmd.set_specialization_constant_mask(1);
	cmd.set_specialization_constant(0, ImplementationConstants::DefaultWorkgroupSize);
	cmd.dispatch(num_workgroups, 1, 1);
	cmd.set_specialization_constant_mask(0);
}

void Renderer::submit_update_upscaled_domain(Vulkan::CommandBuffer &cmd, ResolveStage stage)
{
	unsigned pixel_size_log2;

	switch (fb.fmt)
	{
	case FBFormat::RGBA8888:
		pixel_size_log2 = 2;
		break;

	case FBFormat::RGBA5551:
	case FBFormat::IA88:
		pixel_size_log2 = 1;
		break;

	default:
		pixel_size_log2 = 0;
		break;
	}

	submit_update_upscaled_domain(cmd, stage, fb.addr, fb.depth_addr,
	                              fb.width, fb.deduced_height, pixel_size_log2);
}

void Renderer::submit_depth_blend(Vulkan::CommandBuffer &cmd, Vulkan::Buffer &tmem, bool upscaled, bool force_write_mask)
{
	cmd.begin_region("render-pass");
	auto &instance = buffer_instances[buffer_instance];

	cmd.set_specialization_constant_mask(0xff);
	cmd.set_specialization_constant(0, uint32_t(rdram_size));
	cmd.set_specialization_constant(1, uint32_t(fb.fmt));
	cmd.set_specialization_constant(2, int(fb.addr == fb.depth_addr));
	cmd.set_specialization_constant(3, ImplementationConstants::TileWidth);
	cmd.set_specialization_constant(4, ImplementationConstants::TileHeight);
	cmd.set_specialization_constant(5, Limits::MaxPrimitives);
	cmd.set_specialization_constant(6, upscaled ? caps.max_width : Limits::MaxWidth);
	cmd.set_specialization_constant(7, uint32_t(force_write_mask || (!is_host_coherent && !upscaled)) |
	                                   ((upscaled ? trailing_zeroes(caps.upscaling) : 0u) << 1u));

	if (upscaled)
		cmd.set_storage_buffer(0, 0, *upscaling_multisampled_rdram);
	else
		cmd.set_storage_buffer(0, 0, *rdram, rdram_offset, rdram_size * (is_host_coherent ? 1 : 2));
	cmd.set_storage_buffer(0, 1, upscaled ? *upscaling_multisampled_hidden_rdram : *hidden_rdram);
	cmd.set_storage_buffer(0, 2, tmem);

	if (!caps.ubershader)
	{
		cmd.set_storage_buffer(0, 3, *per_tile_shaded_color);
		cmd.set_storage_buffer(0, 4, *per_tile_shaded_depth);
		cmd.set_storage_buffer(0, 5, *per_tile_shaded_shaded_alpha);
		cmd.set_storage_buffer(0, 6, *per_tile_shaded_coverage);
		cmd.set_storage_buffer(0, 7, *per_tile_offsets);
	}

	cmd.set_storage_buffer(1, 0, *instance.gpu.triangle_setup.buffer);
	cmd.set_storage_buffer(1, 1, *instance.gpu.attribute_setup.buffer);
	cmd.set_storage_buffer(1, 2, *instance.gpu.derived_setup.buffer);
	cmd.set_storage_buffer(1, 3, *instance.gpu.scissor_setup.buffer);
	cmd.set_storage_buffer(1, 4, *instance.gpu.static_raster_state.buffer);
	cmd.set_storage_buffer(1, 5, *instance.gpu.depth_blend_state.buffer);
	cmd.set_storage_buffer(1, 6, *instance.gpu.state_indices.buffer);
	cmd.set_storage_buffer(1, 7, *instance.gpu.tile_info_state.buffer);
	cmd.set_storage_buffer(1, 8, *span_setups);
	cmd.set_storage_buffer(1, 9, *instance.gpu.span_info_offsets.buffer);
	cmd.set_buffer_view(1, 10, *blender_divider_buffer);
	cmd.set_storage_buffer(1, 11, *tile_binning_buffer);
	cmd.set_storage_buffer(1, 12, *tile_binning_buffer_coarse);

	auto *global_fb_info = cmd.allocate_typed_constant_data<GlobalFBInfo>(2, 0, 1);

	GlobalState push = {};
	push.fb_width = fb.width;
	push.fb_height = fb.deduced_height;

	if (upscaled)
	{
		push.fb_width *= caps.upscaling;
		push.fb_height *= caps.upscaling;
	}

	switch (fb.fmt)
	{
	case FBFormat::I4:
		push.addr_index = fb.addr;
		global_fb_info->fb_size = 0;
		global_fb_info->dx_mask = 0;
		global_fb_info->dx_shift = 0;
		break;

	case FBFormat::I8:
		push.addr_index = fb.addr;
		global_fb_info->fb_size = 1;
		global_fb_info->dx_mask = ~7u;
		global_fb_info->dx_shift = 3;
		break;

	case FBFormat::RGBA5551:
	case FBFormat::IA88:
		push.addr_index = fb.addr >> 1u;
		global_fb_info->fb_size = 2;
		global_fb_info->dx_mask = ~3u;
		global_fb_info->dx_shift = 2;
		break;

	case FBFormat::RGBA8888:
		push.addr_index = fb.addr >> 2u;
		global_fb_info->fb_size = 4;
		global_fb_info->dx_mask = ~1u;
		global_fb_info->dx_shift = 1;
		break;
	}

	global_fb_info->base_primitive_index = base_primitive_index;

	push.depth_addr_index = fb.depth_addr >> 1;
	unsigned num_primitives_32 = (stream.triangle_setup.size() + 31) / 32;
	push.group_mask = (1u << num_primitives_32) - 1;
	cmd.push_constants(&push, 0, sizeof(push));

	if (caps.ubershader)
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://ubershader.comp", {
				{ "DEBUG_ENABLE", debug_channel ? 1 : 0 },
				{ "SMALL_TYPES", caps.supports_small_integer_arithmetic ? 1 : 0 },
				{ "SUBGROUP", caps.subgroup_depth_blend ? 1 : 0 },
		});
#else
		cmd.set_program(shader_bank->ubershader);
#endif
	}
	else
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://depth_blend.comp", {
				{ "DEBUG_ENABLE", debug_channel ? 1 : 0 },
				{ "SMALL_TYPES", caps.supports_small_integer_arithmetic ? 1 : 0 },
				{ "SUBGROUP", caps.subgroup_depth_blend ? 1 : 0 },
		});
#else
		cmd.set_program(shader_bank->depth_blend);
#endif
	}

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 2)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);

	cmd.dispatch((push.fb_width + 7) / 8, (push.fb_height + 7) / 8, 1);

	if (caps.timestamp >= 2)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts), "depth-blending");
	}

	cmd.end_region();
}

void Renderer::submit_render_pass(Vulkan::CommandBuffer &cmd)
{
	bool need_render_pass = fb.width != 0 && fb.deduced_height != 0 && !stream.span_info_jobs.empty();
	bool need_tmem_upload = !stream.tmem_upload_infos.empty();
	bool need_submit = need_render_pass || need_tmem_upload;
	if (!need_submit)
		return;

	Vulkan::QueryPoolHandle render_pass_start, render_pass_end;
	if (caps.timestamp >= 1)
		render_pass_start = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);

	if (debug_channel)
		cmd.begin_debug_channel(this, "Debug", 16 * 1024 * 1024);

	// Here we run 3 dispatches in parallel. Span setup and TMEM instances are low occupancy kind of jobs, but the binning
	// pass should dominate here unless the workload is trivial.
	if (need_render_pass)
	{
		submit_span_setup_jobs(cmd, false);
		submit_tile_binning_combined(cmd, false);
		if (caps.upscaling > 1)
			submit_update_upscaled_domain(cmd, ResolveStage::Pre);
	}

	if (need_tmem_upload)
		update_tmem_instances(cmd);

	cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
	            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT | (!caps.ubershader ? VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT : 0),
	            VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT |
	            (!caps.ubershader ? VK_ACCESS_INDIRECT_COMMAND_READ_BIT : 0));

	if (need_render_pass && !caps.ubershader)
	{
		submit_rasterization(cmd, need_tmem_upload ? *tmem_instances : *tmem, false);
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_READ_BIT);
	}

	if (need_render_pass)
		submit_depth_blend(cmd, need_tmem_upload ? *tmem_instances : *tmem, false, false);

	if (!caps.ubershader)
		clear_indirect_buffer(cmd);

	if (render_pass_is_upscaled())
	{
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
		            VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT);

		// TODO: Could probably do this reference update in the render pass itself,
		// just write output to two buffers ... This is more composable for now.
		submit_update_upscaled_domain(cmd, ResolveStage::Post);
	}

	if (caps.timestamp >= 1)
	{
		render_pass_end = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		std::string tag;
		tag = "(" + std::to_string(fb.width) + " x " + std::to_string(fb.deduced_height) + ")";
		tag += " (" + std::to_string(stream.triangle_setup.size()) + " triangles)";
		device->register_time_interval("RDP GPU", std::move(render_pass_start), std::move(render_pass_end), "render-pass", std::move(tag));
	}
}

void Renderer::submit_render_pass_upscaled(Vulkan::CommandBuffer &cmd)
{
	cmd.begin_region("render-pass-upscaled");
	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp >= 1)
		start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);

	bool need_tmem_upload = !stream.tmem_upload_infos.empty();

	submit_span_setup_jobs(cmd, true);
	submit_tile_binning_combined(cmd, true);
	if (caps.super_sample_readback)
	{
		submit_update_upscaled_domain(cmd, ResolveStage::Pre);
		submit_clear_super_sample_write_mask(cmd, fb.width, fb.deduced_height);
		if (need_tmem_upload)
			update_tmem_instances(cmd);
	}

	cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
	            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT |
	            (!caps.ubershader ? VK_PIPELINE_STAGE_DRAW_INDIRECT_BIT : 0),
	            VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT |
	            (!caps.ubershader ? VK_ACCESS_INDIRECT_COMMAND_READ_BIT : 0));

	if (!caps.ubershader)
	{
		submit_rasterization(cmd, need_tmem_upload ? *tmem_instances : *tmem, true);
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
		            VK_ACCESS_SHADER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
		            VK_ACCESS_SHADER_READ_BIT);
	}

	submit_depth_blend(cmd, need_tmem_upload ? *tmem_instances : *tmem, true, caps.super_sample_readback);
	if (!caps.ubershader)
		clear_indirect_buffer(cmd);

	if (caps.super_sample_readback)
	{
		cmd.begin_region("ssaa-resolve");
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
		            VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT);

		submit_update_upscaled_domain(cmd, ResolveStage::SSAAResolve);
		cmd.end_region();
	}

	if (caps.timestamp >= 1)
	{
		end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval("RDP GPU", std::move(start_ts), std::move(end_ts), "render-pass-upscaled");
	}
	cmd.end_region();
}

void Renderer::submit_render_pass_end(Vulkan::CommandBuffer &cmd)
{
	base_primitive_index += uint32_t(stream.triangle_setup.size());
	cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
	            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
	            VK_ACCESS_SHADER_READ_BIT | VK_ACCESS_SHADER_WRITE_BIT);
}

void Renderer::maintain_queues()
{
	// Some conditions dictate if we should flush a render pass.
	// These heuristics ensures we don't wait too long to flush render passes,
	// and also ensure that we don't spam submissions too often, causing massive bubbles on GPU.

	// If we get a lot of small render passes in a row, it makes sense to batch them up, e.g. 8 at a time.
	// If we get 2 full render passes of ~256 primitives, that's also a good indication we should flush since we're getting spammed.
	// If we have no pending submissions, the GPU is idle and there is no reason not to submit.
	// If we haven't submitted anything in a while (1.0 ms), it's probably fine to submit again.
	if (pending_render_passes >= ImplementationConstants::MaxPendingRenderPassesBeforeFlush ||
	    (caps.super_sample_readback && pending_render_passes_upscaled >= ImplementationConstants::MaxPendingRenderPassesBeforeFlush) ||
	    pending_primitives >= Limits::MaxPrimitives ||
	    pending_primitives_upscaled >= Limits::MaxPrimitives ||
	    active_submissions.load(std::memory_order_relaxed) == 0 ||
	    int64_t(Util::get_current_time_nsecs() - last_submit_ns) > 1000000)
	{
		submit_to_queue();
	}
}

void Renderer::lock_command_processing()
{
	idle_lock.lock();
}

void Renderer::unlock_command_processing()
{
	idle_lock.unlock();
}

void Renderer::maintain_queues_idle()
{
	std::lock_guard<std::mutex> holder{idle_lock};
	if (pending_primitives >= ImplementationConstants::MinimumPrimitivesForIdleFlush ||
	    pending_render_passes >= ImplementationConstants::MinimumRenderPassesForIdleFlush)
	{
		flush_queues();
		submit_to_queue();
	}
}

void Renderer::enqueue_fence_wait(Vulkan::Fence fence)
{
	CoherencyOperation op;
	op.fence = std::move(fence);
	op.unlock_cookie = &active_submissions;
	active_submissions.fetch_add(1, std::memory_order_relaxed);
	processor.enqueue_coherency_operation(std::move(op));
	last_submit_ns = Util::get_current_time_nsecs();
}

void Renderer::submit_to_queue()
{
	bool pending_host_visible_render_passes =
			(caps.super_sample_readback ? pending_render_passes_upscaled : pending_render_passes) != 0;
	bool pending_upscaled_passes = pending_render_passes_upscaled != 0;
	pending_render_passes = 0;
	pending_render_passes_upscaled = 0;
	pending_primitives = 0;
	pending_primitives_upscaled = 0;

	if (!stream.cmd)
	{
		if (pending_host_visible_render_passes)
		{
			Vulkan::Fence fence;
			device->submit_empty(Vulkan::CommandBuffer::Type::AsyncCompute, &fence);
			enqueue_fence_wait(fence);
		}
		return;
	}

	bool need_host_barrier = is_host_coherent || !incoherent.staging_readback;

	// If we maintain queues in-between doing 1x render pass and upscaled render pass,
	// we haven't flushed memory yet.
	bool need_memory_flush = pending_host_visible_render_passes && !pending_upscaled_passes;
	stream.cmd->barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
	                    need_memory_flush ? VK_ACCESS_MEMORY_WRITE_BIT : 0,
	                    (need_host_barrier ? VK_PIPELINE_STAGE_HOST_BIT : VK_PIPELINE_STAGE_TRANSFER_BIT),
	                    (need_host_barrier ? VK_ACCESS_HOST_READ_BIT : VK_ACCESS_TRANSFER_READ_BIT));

	Vulkan::Fence fence;

	if (is_host_coherent)
	{
		device->submit(stream.cmd, &fence);
		if (pending_host_visible_render_passes)
			enqueue_fence_wait(fence);
	}
	else
	{
		CoherencyOperation op;
		if (pending_host_visible_render_passes)
			resolve_coherency_gpu_to_host(op, *stream.cmd);

		device->submit(stream.cmd, &fence);

		if (pending_host_visible_render_passes)
		{
			enqueue_fence_wait(fence);
			op.fence = fence;
			if (!op.copies.empty())
				processor.enqueue_coherency_operation(std::move(op));
		}
	}

	Util::for_each_bit(sync_indices_needs_flush, [&](unsigned bit) {
		auto &sync = internal_sync[bit];
		sync.fence = fence;
	});
	sync_indices_needs_flush = 0;
	stream.cmd.reset();
}

void Renderer::reset_context()
{
	stream.scissor_setup.reset();
	stream.static_raster_state_cache.reset();
	stream.depth_blend_state_cache.reset();
	stream.tile_info_state_cache.reset();
	stream.triangle_setup.reset();
	stream.attribute_setup.reset();
	stream.derived_setup.reset();
	stream.state_indices.reset();
	stream.span_info_offsets.reset();
	stream.span_info_jobs.reset();
	stream.max_shaded_tiles = 0;

	fb.deduced_height = 0;
	fb.color_write_pending = false;
	fb.depth_write_pending = false;

	stream.tmem_upload_infos.clear();
}

void Renderer::begin_new_context()
{
	buffer_instance = (buffer_instance + 1) % Limits::NumSyncStates;
	reset_context();
}

uint32_t Renderer::get_byte_size_for_bound_color_framebuffer() const
{
	unsigned pixel_count = fb.width * fb.deduced_height;
	unsigned byte_count;
	switch (fb.fmt)
	{
	case FBFormat::RGBA8888:
		byte_count = pixel_count * 4;
		break;

	case FBFormat::RGBA5551:
	case FBFormat::IA88:
		byte_count = pixel_count * 2;
		break;

	default:
		byte_count = pixel_count;
		break;
	}

	return byte_count;
}

uint32_t Renderer::get_byte_size_for_bound_depth_framebuffer() const
{
	return fb.width * fb.deduced_height * 2;
}

void Renderer::mark_pages_for_gpu_read(uint32_t base_addr, uint32_t byte_count)
{
	if (byte_count == 0)
		return;

	uint32_t start_page = base_addr / ImplementationConstants::IncoherentPageSize;
	uint32_t end_page = (base_addr + byte_count - 1) / ImplementationConstants::IncoherentPageSize + 1;
	start_page &= incoherent.num_pages - 1;
	end_page &= incoherent.num_pages - 1;

	uint32_t page = start_page;
	while (page != end_page)
	{
		bool pending_writes = (incoherent.page_to_pending_readback[page / 32] & (1u << (page & 31))) != 0 ||
		                      incoherent.pending_writes_for_page[page].load(std::memory_order_relaxed) != 0;

		// We'll do an acquire memory barrier later before we start memcpy-ing from host memory.
		if (pending_writes)
			incoherent.page_to_masked_copy[page / 32] |= 1u << (page & 31);
		else
			incoherent.page_to_direct_copy[page / 32] |= 1u << (page & 31);

		page = (page + 1) & (incoherent.num_pages - 1);
	}
}

void Renderer::lock_pages_for_gpu_write(uint32_t base_addr, uint32_t byte_count)
{
	if (byte_count == 0)
		return;

	uint32_t start_page = base_addr / ImplementationConstants::IncoherentPageSize;
	uint32_t end_page = (base_addr + byte_count - 1) / ImplementationConstants::IncoherentPageSize + 1;

	for (uint32_t page = start_page; page < end_page; page++)
	{
		uint32_t wrapped_page = page & (incoherent.num_pages - 1);
		incoherent.page_to_pending_readback[wrapped_page / 32] |= 1u << (wrapped_page & 31);
	}
}

void Renderer::resolve_coherency_gpu_to_host(CoherencyOperation &op, Vulkan::CommandBuffer &cmd)
{
	cmd.begin_region("resolve-coherency-gpu-to-host");
	if (!incoherent.staging_readback)
	{
		// iGPU path.
		op.src = rdram;
		op.dst = incoherent.host_rdram;
		op.timeline_value = 0;

		for (auto &readback : incoherent.page_to_pending_readback)
		{
			uint32_t base_index = 32 * uint32_t(&readback - incoherent.page_to_pending_readback.data());

			Util::for_each_bit_range(readback, [&](unsigned index, unsigned count) {
				index += base_index;

				for (unsigned i = 0; i < count; i++)
					incoherent.pending_writes_for_page[index + i].fetch_add(1, std::memory_order_relaxed);

				CoherencyCopy coherent_copy = {};
				coherent_copy.counter_base = &incoherent.pending_writes_for_page[index];
				coherent_copy.counters = count;
				coherent_copy.src_offset = index * ImplementationConstants::IncoherentPageSize;
				coherent_copy.mask_offset = coherent_copy.src_offset + rdram_size;
				coherent_copy.dst_offset = index * ImplementationConstants::IncoherentPageSize;
				coherent_copy.size = ImplementationConstants::IncoherentPageSize * count;
				op.copies.push_back(coherent_copy);
			});

			readback = 0;
		}
	}
	else
	{
		// Discrete GPU path.
		Util::SmallVector<VkBufferCopy, 1024> copies;
		op.src = incoherent.staging_readback.get();
		op.dst = incoherent.host_rdram;
		op.timeline_value = 0;

		for (auto &readback : incoherent.page_to_pending_readback)
		{
			uint32_t base_index = 32 * uint32_t(&readback - incoherent.page_to_pending_readback.data());

			Util::for_each_bit_range(readback, [&](unsigned index, unsigned count) {
				index += base_index;

				for (unsigned i = 0; i < count; i++)
					incoherent.pending_writes_for_page[index + i].fetch_add(1, std::memory_order_relaxed);

				VkBufferCopy copy = {};
				copy.srcOffset = index * ImplementationConstants::IncoherentPageSize;

				unsigned dst_page_index = incoherent.staging_readback_index;
				copy.dstOffset = dst_page_index * ImplementationConstants::IncoherentPageSize;

				incoherent.staging_readback_index += count;
				incoherent.staging_readback_index &= (incoherent.staging_readback_pages - 1);
				// Unclean wraparound check.
				if (incoherent.staging_readback_index != 0 && incoherent.staging_readback_index < dst_page_index)
				{
					copy.dstOffset = 0;
					incoherent.staging_readback_index = count;
				}

				copy.size = ImplementationConstants::IncoherentPageSize * count;
				copies.push_back(copy);

				CoherencyCopy coherent_copy = {};
				coherent_copy.counter_base = &incoherent.pending_writes_for_page[index];
				coherent_copy.counters = count;
				coherent_copy.src_offset = copy.dstOffset;
				coherent_copy.dst_offset = index * ImplementationConstants::IncoherentPageSize;
				coherent_copy.size = ImplementationConstants::IncoherentPageSize * count;

				VkBufferCopy mask_copy = {};
				mask_copy.srcOffset = index * ImplementationConstants::IncoherentPageSize + rdram_size;

				dst_page_index = incoherent.staging_readback_index;
				mask_copy.dstOffset = dst_page_index * ImplementationConstants::IncoherentPageSize;

				incoherent.staging_readback_index += count;
				incoherent.staging_readback_index &= (incoherent.staging_readback_pages - 1);
				// Unclean wraparound check.
				if (incoherent.staging_readback_index != 0 && incoherent.staging_readback_index < dst_page_index)
				{
					mask_copy.dstOffset = 0;
					incoherent.staging_readback_index = count;
				}

				mask_copy.size = ImplementationConstants::IncoherentPageSize * count;
				copies.push_back(mask_copy);
				coherent_copy.mask_offset = mask_copy.dstOffset;

				op.copies.push_back(coherent_copy);
			});

			readback = 0;
		}

		if (!copies.empty())
		{
//#define COHERENCY_READBACK_TIMESTAMPS
#ifdef COHERENCY_READBACK_TIMESTAMPS
			Vulkan::QueryPoolHandle start_ts, end_ts;
			start_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_TRANSFER_BIT);
#endif
			cmd.copy_buffer(*incoherent.staging_readback, *rdram, copies.data(), copies.size());
#ifdef COHERENCY_READBACK_TIMESTAMPS
			end_ts = cmd.write_timestamp(VK_PIPELINE_STAGE_TRANSFER_BIT);
			device->register_time_interval(std::move(start_ts), std::move(end_ts), "coherency-readback");
#endif
			cmd.barrier(VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_WRITE_BIT,
			            VK_PIPELINE_STAGE_HOST_BIT | VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT,
			            VK_ACCESS_HOST_READ_BIT);
		}
	}
	cmd.end_region();
}

void Renderer::resolve_coherency_external(unsigned offset, unsigned length)
{
	mark_pages_for_gpu_read(offset, length);
	ensure_command_buffer();
	resolve_coherency_host_to_gpu(*stream.cmd);
	device->submit(stream.cmd);
	stream.cmd.reset();
}

unsigned Renderer::get_scaling_factor() const
{
	return caps.upscaling;
}

const Vulkan::Buffer *Renderer::get_upscaled_rdram_buffer() const
{
	return upscaling_multisampled_rdram.get();
}

const Vulkan::Buffer *Renderer::get_upscaled_hidden_rdram_buffer() const
{
	return upscaling_multisampled_hidden_rdram.get();
}

void Renderer::resolve_coherency_host_to_gpu(Vulkan::CommandBuffer &cmd)
{
	// Now, ensure that the GPU sees a coherent view of the CPU memory writes up until now.
	// Writes made by the GPU which are not known to be resolved on the timeline waiter thread will always
	// "win" over writes made by CPU, since CPU is not allowed to meaningfully overwrite data which the GPU
	// is going to touch.

	cmd.begin_region("resolve-coherency-host-to-gpu");

	Vulkan::QueryPoolHandle start_ts, end_ts;
	if (caps.timestamp)
		start_ts = device->write_calibrated_timestamp();

	std::atomic_thread_fence(std::memory_order_acquire);

	Util::SmallVector<VkBufferCopy, 1024> buffer_copies;
	Util::SmallVector<uint32_t, 1024> masked_page_copies;
	Util::SmallVector<uint32_t, 1024> to_clear_write_mask;

	// If we're able to map RDRAM directly, we can just memcpy straight into RDRAM if we have an unmasked copy.
	// Important for iGPU.
	if (rdram->get_allocation().is_host_allocation())
	{
		for (auto &direct : incoherent.page_to_direct_copy)
		{
			uint32_t base_index = 32 * (&direct - incoherent.page_to_direct_copy.data());
			Util::for_each_bit_range(direct, [&](unsigned index, unsigned count) {
				index += base_index;
				auto *mapped_rdram = device->map_host_buffer(*rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT,
				                                             ImplementationConstants::IncoherentPageSize * index,
				                                             ImplementationConstants::IncoherentPageSize * count);
				memcpy(mapped_rdram,
				       incoherent.host_rdram + ImplementationConstants::IncoherentPageSize * index,
				       ImplementationConstants::IncoherentPageSize * count);

				device->unmap_host_buffer(*rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT,
				                          ImplementationConstants::IncoherentPageSize * index,
				                          ImplementationConstants::IncoherentPageSize * count);

				mapped_rdram = device->map_host_buffer(*rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT,
				                                       ImplementationConstants::IncoherentPageSize * index + rdram_size,
				                                       ImplementationConstants::IncoherentPageSize * count);

				memset(mapped_rdram, 0, ImplementationConstants::IncoherentPageSize * count);

				device->unmap_host_buffer(*rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT,
				                          ImplementationConstants::IncoherentPageSize * index + rdram_size,
				                          ImplementationConstants::IncoherentPageSize * count);
			});
			direct = 0;
		}

		auto *mapped_staging = static_cast<uint8_t *>(device->map_host_buffer(*incoherent.staging_rdram,
		                                                                      Vulkan::MEMORY_ACCESS_WRITE_BIT));

		for (auto &indirect : incoherent.page_to_masked_copy)
		{
			uint32_t base_index = 32 * (&indirect - incoherent.page_to_masked_copy.data());
			Util::for_each_bit(indirect, [&](unsigned index) {
				index += base_index;
				masked_page_copies.push_back(index);
				memcpy(mapped_staging + ImplementationConstants::IncoherentPageSize * index,
				       incoherent.host_rdram + ImplementationConstants::IncoherentPageSize * index,
				       ImplementationConstants::IncoherentPageSize);
			});
			indirect = 0;
		}

		device->unmap_host_buffer(*incoherent.staging_rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT);
	}
	else
	{
		auto *mapped_rdram = static_cast<uint8_t *>(device->map_host_buffer(*incoherent.staging_rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT));

		size_t num_packed_pages = incoherent.page_to_masked_copy.size();
		for (size_t i = 0; i < num_packed_pages; i++)
		{
			uint32_t base_index = 32 * i;
			uint32_t tmp = incoherent.page_to_masked_copy[i] | incoherent.page_to_direct_copy[i];
			Util::for_each_bit(tmp, [&](unsigned index) {
				unsigned bit = index;
				index += base_index;

				if ((1u << bit) & incoherent.page_to_masked_copy[i])
					masked_page_copies.push_back(index);
				else
				{
					VkBufferCopy copy = {};
					copy.size = ImplementationConstants::IncoherentPageSize;
					copy.dstOffset = copy.srcOffset = index * ImplementationConstants::IncoherentPageSize;
					buffer_copies.push_back(copy);
					to_clear_write_mask.push_back(index);
				}

				memcpy(mapped_rdram + ImplementationConstants::IncoherentPageSize * index,
				       incoherent.host_rdram + ImplementationConstants::IncoherentPageSize * index,
				       ImplementationConstants::IncoherentPageSize);
			});

			incoherent.page_to_masked_copy[i] = 0;
			incoherent.page_to_direct_copy[i] = 0;
		}

		device->unmap_host_buffer(*incoherent.staging_rdram, Vulkan::MEMORY_ACCESS_WRITE_BIT);
	}

	if (!masked_page_copies.empty())
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://masked_rdram_resolve.comp");
#else
		cmd.set_program(shader_bank->masked_rdram_resolve);
#endif
		cmd.set_specialization_constant_mask(3);
		cmd.set_specialization_constant(0, ImplementationConstants::IncoherentPageSize / 4);
		cmd.set_specialization_constant(1, ImplementationConstants::IncoherentPageSize / 4);

		cmd.set_storage_buffer(0, 0, *rdram, rdram_offset, rdram_size);
		cmd.set_storage_buffer(0, 1, *incoherent.staging_rdram);
		cmd.set_storage_buffer(0, 2, *rdram, rdram_offset + rdram_size, rdram_size);

//#define COHERENCY_MASK_TIMESTAMPS
#ifdef COHERENCY_MASK_TIMESTAMPS
		Vulkan::QueryPoolHandle start_ts, end_ts;
		start_ts = cmd->write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
#endif

		for (size_t i = 0; i < masked_page_copies.size(); i += 4096)
		{
			size_t to_copy = std::min(masked_page_copies.size() - i, size_t(4096));
			memcpy(cmd.allocate_typed_constant_data<uint32_t>(1, 0, to_copy),
				   masked_page_copies.data() + i,
				   to_copy * sizeof(uint32_t));
			cmd.dispatch(to_copy, 1, 1);
		}

#ifdef COHERENCY_MASK_TIMESTAMPS
		end_ts = cmd->write_timestamp(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT);
		device->register_time_interval(std::move(start_ts), std::move(end_ts), "coherent-mask-copy");
#endif
	}

	// Could use FillBuffer here, but would need to use TRANSFER stage, and introduce more barriers than needed.
	if (!to_clear_write_mask.empty())
	{
#ifdef PARALLEL_RDP_SHADER_DIR
		cmd.set_program("rdp://clear_write_mask.comp");
#else
		cmd.set_program(shader_bank->clear_write_mask);
#endif
		cmd.set_specialization_constant_mask(3);
		cmd.set_specialization_constant(0, ImplementationConstants::IncoherentPageSize / 4);
		cmd.set_specialization_constant(1, ImplementationConstants::IncoherentPageSize / 4);
		cmd.set_storage_buffer(0, 0, *rdram, rdram_offset + rdram_size, rdram_size);
		for (size_t i = 0; i < to_clear_write_mask.size(); i += 4096)
		{
			size_t to_copy = std::min(to_clear_write_mask.size() - i, size_t(4096));
			memcpy(cmd.allocate_typed_constant_data<uint32_t>(1, 0, to_copy),
				   to_clear_write_mask.data() + i,
				   to_copy * sizeof(uint32_t));
			cmd.dispatch(to_copy, 1, 1);
		}
	}

	if (!to_clear_write_mask.empty() || !masked_page_copies.empty())
	{
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_READ_BIT);
	}

	// If we cannot map the device memory, copy. We're latency sensitive, so don't use DMA queue.
	if (!buffer_copies.empty())
	{
		cmd.barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, 0,
		            VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_WRITE_BIT);

//#define COHERENCY_COPY_TIMESTAMPS
#ifdef COHERENCY_COPY_TIMESTAMPS
		Vulkan::QueryPoolHandle start_ts, end_ts;
		start_ts = cmd->write_timestamp(VK_PIPELINE_STAGE_ALL_COMMANDS_BIT);
#endif
		cmd.copy_buffer(*rdram, *incoherent.staging_rdram, buffer_copies.data(), buffer_copies.size());
#ifdef COHERENCY_COPY_TIMESTAMPS
		end_ts = cmd->write_timestamp(VK_PIPELINE_STAGE_TRANSFER_BIT);
		device->register_time_interval(std::move(start_ts), std::move(end_ts), "coherent-copy");
#endif

		cmd.barrier(VK_PIPELINE_STAGE_TRANSFER_BIT, VK_ACCESS_TRANSFER_WRITE_BIT,
		            VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_READ_BIT);
	}

	if (caps.timestamp)
	{
		end_ts = device->write_calibrated_timestamp();
		device->register_time_interval("RDP CPU", std::move(start_ts), std::move(end_ts), "coherency-host-to-gpu");
	}

	cmd.end_region();
}

void Renderer::flush_queues()
{
	if (stream.tmem_upload_infos.empty() && stream.span_info_jobs.empty())
	{
		base_primitive_index += stream.triangle_setup.size();
		reset_context();
		return;
	}

	if (!is_host_coherent)
	{
		mark_pages_for_gpu_read(fb.addr, get_byte_size_for_bound_color_framebuffer());
		mark_pages_for_gpu_read(fb.depth_addr, get_byte_size_for_bound_depth_framebuffer());

		// We're going to write to these pages, so lock them down.
		lock_pages_for_gpu_write(fb.addr, get_byte_size_for_bound_color_framebuffer());
		lock_pages_for_gpu_write(fb.depth_addr, get_byte_size_for_bound_depth_framebuffer());
	}

	auto &instance = buffer_instances[buffer_instance];
	auto &sync = internal_sync[buffer_instance];
	if (sync_indices_needs_flush & (1u << buffer_instance))
		submit_to_queue();
	sync_indices_needs_flush |= 1u << buffer_instance;

	if (sync.fence)
	{
		Vulkan::QueryPoolHandle start_ts, end_ts;
		if (caps.timestamp)
			start_ts = device->write_calibrated_timestamp();
		sync.fence->wait();
		if (caps.timestamp)
		{
			end_ts = device->write_calibrated_timestamp();
			device->register_time_interval("RDP CPU", std::move(start_ts), std::move(end_ts), "render-pass-fence");
		}
		sync.fence.reset();
	}

	ensure_command_buffer();

	if (!is_host_coherent)
		resolve_coherency_host_to_gpu(*stream.cmd);
	instance.upload(*device, stream, *stream.cmd);

	// If we have super-sampled readback, then this is meaningless.
	bool has_single_sampled_render_pass = !caps.super_sample_readback;

	if (has_single_sampled_render_pass)
	{
		stream.cmd->begin_region("render-pass-1x");
		submit_render_pass(*stream.cmd);
		stream.cmd->end_region();
		pending_render_passes++;
	}

	if (render_pass_is_upscaled())
	{
		if (has_single_sampled_render_pass)
		{
			maintain_queues();
			ensure_command_buffer();

			// We're going to keep reading the same data structures, so make sure
			// we signal fence after upscaled render pass is submitted.
			sync_indices_needs_flush |= 1u << buffer_instance;
		}

		submit_render_pass_upscaled(*stream.cmd);
		pending_render_passes_upscaled++;
		pending_primitives_upscaled += uint32_t(stream.triangle_setup.size());
	}

	submit_render_pass_end(*stream.cmd);

	begin_new_context();
	maintain_queues();
}

bool Renderer::render_pass_is_upscaled() const
{
	if (caps.super_sample_readback)
		return true;

	bool need_render_pass = fb.width != 0 && fb.deduced_height != 0 && !stream.span_info_jobs.empty();
	return need_render_pass && should_render_upscaled();
}

bool Renderer::should_render_upscaled() const
{
	if (caps.upscaling > 1)
	{
		// A heuristic. There is no point to render upscaled for purely off-screen passes.
		// We should ideally only upscale the final pass which hits screen.
		// From a heuristic point-of-view we expect only 16-bit/32-bit frame buffers to be relevant,
		// and only frame buffers with at least 256 pixels.
		return (fb.fmt == FBFormat::RGBA5551 || fb.fmt == FBFormat::RGBA8888) && fb.width >= 256;
	}
	else
		return false;
}

void Renderer::ensure_command_buffer()
{
	if (!stream.cmd)
		stream.cmd = device->request_command_buffer(Vulkan::CommandBuffer::Type::AsyncCompute);

	if (!caps.ubershader && !indirect_dispatch_buffer)
	{
		Vulkan::BufferCreateInfo indirect_info = {};
		indirect_info.size = 4 * sizeof(uint32_t) * Limits::MaxStaticRasterizationStates;
		indirect_info.domain = Vulkan::BufferDomain::Device;
		indirect_info.usage = VK_BUFFER_USAGE_STORAGE_BUFFER_BIT | VK_BUFFER_USAGE_INDIRECT_BUFFER_BIT;

		indirect_dispatch_buffer = device->create_buffer(indirect_info);
		device->set_name(*indirect_dispatch_buffer, "indirect-dispatch-buffer");

		clear_indirect_buffer(*stream.cmd);
		stream.cmd->barrier(VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT,
		                    VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT, VK_ACCESS_SHADER_WRITE_BIT | VK_ACCESS_SHADER_READ_BIT);
	}
}

void Renderer::set_tile(uint32_t tile, const TileMeta &meta)
{
	tiles[tile].meta = meta;
}

void Renderer::set_tile_size(uint32_t tile, uint32_t slo, uint32_t shi, uint32_t tlo, uint32_t thi)
{
	tiles[tile].size.slo = slo;
	tiles[tile].size.shi = shi;
	tiles[tile].size.tlo = tlo;
	tiles[tile].size.thi = thi;
}

void Renderer::notify_idle_command_thread()
{
	maintain_queues_idle();
}

bool Renderer::tmem_upload_needs_flush(uint32_t addr) const
{
	// Not perfect, since TMEM upload could slice into framebuffer,
	// but I doubt this will be an issue (famous last words ...)

	if (fb.color_write_pending)
	{
		uint32_t offset = (addr - fb.addr) & (rdram_size - 1);
		uint32_t pending_pixels = fb.deduced_height * fb.width;

		switch (fb.fmt)
		{
		case FBFormat::RGBA5551:
		case FBFormat::I8:
			offset >>= 1;
			break;

		case FBFormat::RGBA8888:
			offset >>= 2;
			break;

		default:
			break;
		}

		if (offset < pending_pixels)
		{
			//LOGI("Flushing render pass due to coherent TMEM fetch from color buffer.\n");
			return true;
		}
	}

	if (fb.depth_write_pending)
	{
		uint32_t offset = (addr - fb.depth_addr) & (rdram_size - 1);
		uint32_t pending_pixels = fb.deduced_height * fb.width;
		offset >>= 1;

		if (offset < pending_pixels)
		{
			//LOGI("Flushing render pass due to coherent TMEM fetch from depth buffer.\n");
			return true;
		}
	}

	return false;
}

void Renderer::load_tile(uint32_t tile, const LoadTileInfo &info)
{
	if (tmem_upload_needs_flush(info.tex_addr))
		flush_queues();

	// Detect noop cases.
	if (info.mode != UploadMode::Block)
	{
		if ((info.thi >> 2) < (info.tlo >> 2))
			return;

		unsigned pixel_count = (((info.shi >> 2) - (info.slo >> 2)) + 1) & 0xfff;
		if (!pixel_count)
			return;
	}
	else
	{
		unsigned pixel_count = ((info.shi - info.slo) + 1) & 0xfff;
		if (!pixel_count)
			return;
	}

	if (!is_host_coherent)
	{
		unsigned pixel_count;
		unsigned offset_pixels;
		unsigned base_addr = info.tex_addr;

		if (info.mode == UploadMode::Block)
		{
			pixel_count = (info.shi - info.slo + 1) & 0xfff;
			offset_pixels = info.slo + info.tex_width * info.tlo;
		}
		else
		{
			unsigned max_x = ((info.shi >> 2) - (info.slo >> 2)) & 0xfff;
			unsigned max_y = (info.thi >> 2) - (info.tlo >> 2);
			pixel_count = max_y * info.tex_width + max_x + 1;
			offset_pixels = (info.slo >> 2) + info.tex_width * (info.tlo >> 2);
		}

		unsigned byte_size = pixel_count << (unsigned(info.size) - 1);
		byte_size = (byte_size + 7) & ~7;
		base_addr += offset_pixels << (unsigned(info.size) - 1);
		mark_pages_for_gpu_read(base_addr, byte_size);
	}

	if (info.mode == UploadMode::Tile)
	{
		auto &meta = tiles[tile].meta;
		unsigned pixels_coverered_per_line = (((info.shi >> 2) - (info.slo >> 2)) + 1) & 0xfff;

		// Technically, 32-bpp TMEM upload and YUV upload will work like 16bpp, just split into two halves, but that also means
		// we get 2kB wraparound instead of 4kB wraparound, so this works out just fine for our purposes.
		unsigned quad_words_covered_per_line = ((pixels_coverered_per_line << unsigned(meta.size)) + 15) >> 4;

		// Deal with mismatch in state, there is no reasonable scenarios where this should even matter, but you never know ...
		if (unsigned(meta.size) > unsigned(info.size))
			quad_words_covered_per_line <<= unsigned(meta.size) - unsigned(info.size);
		else if (unsigned(meta.size) < unsigned(info.size))
			quad_words_covered_per_line >>= unsigned(info.size) - unsigned(meta.size);

		// Compute a conservative estimate for how many bytes we're going to splat down into TMEM.
		unsigned bytes_covered_per_line = std::max<unsigned>(quad_words_covered_per_line * 8, meta.stride);

		unsigned max_bytes_per_line = 0x1000;
		// We need to write lower and upper halves at once,
		// so we need to wrap around at 2k boundary.
		if (meta.fmt == TextureFormat::YUV)
			max_bytes_per_line /= 2;

		unsigned num_lines = ((info.thi >> 2) - (info.tlo >> 2)) + 1;
		unsigned total_bytes_covered = bytes_covered_per_line * num_lines;

		if (total_bytes_covered > max_bytes_per_line)
		{
			// Welp, for whatever reason, the game wants to write more than 4k of texture data to TMEM in one go.
			// We can only handle 4kB in one go due to wrap-around effects,
			// so split up the upload in multiple chunks.

			unsigned max_lines_per_iteration = max_bytes_per_line / bytes_covered_per_line;
			// Align T-state.
			max_lines_per_iteration &= ~1u;

			if (max_lines_per_iteration == 0)
			{
				LOGE("Pure insanity where content is attempting to load more than 2kB of TMEM data in one single line ...\n");
				// Could be supported if we start splitting up horizonal direction as well, but seriously ...
				return;
			}

			for (unsigned line = 0; line < num_lines; line += max_lines_per_iteration)
			{
				unsigned to_copy_lines = std::min(num_lines - line, max_lines_per_iteration);

				LoadTileInfo tmp_info = info;
				tmp_info.tlo = info.tlo + (line << 2);
				tmp_info.thi = tmp_info.tlo + ((to_copy_lines - 1) << 2);
				load_tile_iteration(tile, tmp_info, line * meta.stride);
			}

			auto &size = tiles[tile].size;
			size.slo = info.slo;
			size.shi = info.shi;
			size.tlo = info.tlo;
			size.thi = info.thi;
		}
		else
			load_tile_iteration(tile, info, 0);
	}
	else
		load_tile_iteration(tile, info, 0);
}

void Renderer::load_tile_iteration(uint32_t tile, const LoadTileInfo &info, uint32_t tmem_offset)
{
	auto &size = tiles[tile].size;
	auto &meta = tiles[tile].meta;
	size.slo = info.slo;
	size.shi = info.shi;
	size.tlo = info.tlo;
	size.thi = info.thi;

	if (meta.fmt == TextureFormat::YUV && ((meta.size != TextureSize::Bpp16) || (info.size != TextureSize::Bpp16)))
	{
		LOGE("Only 16bpp is supported for YUV uploads.\n");
		return;
	}

	// This case does not appear to be supported.
	if (info.size == TextureSize::Bpp4)
	{
		LOGE("4-bit VRAM pointer crashes the RDP.\n");
		return;
	}

	if (meta.size == TextureSize::Bpp32 && meta.fmt != TextureFormat::RGBA)
	{
		LOGE("32bpp tile uploads must using RGBA texture format, unsupported otherwise.\n");
		return;
	}

	if (info.mode == UploadMode::TLUT && meta.size == TextureSize::Bpp32)
	{
		LOGE("TLUT uploads with 32bpp tiles are unsupported.\n");
		return;
	}

	if (info.mode != UploadMode::TLUT)
	{
		if (info.size == TextureSize::Bpp32 && meta.size == TextureSize::Bpp8)
		{
			LOGE("FIXME: Loading tile with Texture 32-bit and Tile 8-bit. This creates insane results, unsupported.\n");
			return;
		}
		else if (info.size == TextureSize::Bpp16 && meta.size == TextureSize::Bpp4)
		{
			LOGE("FIXME: Loading tile with Texture 16-bit and Tile 4-bit. This creates insane results, unsupported.\n");
			return;
		}
		else if (info.size == TextureSize::Bpp32 && meta.size == TextureSize::Bpp4)
		{
			LOGE("FIXME: Loading tile with Texture 32-bit and Tile 4-bit. This creates insane results, unsupported.\n");
			return;
		}
	}

	UploadInfo upload = {};
	upload.tmem_stride_words = meta.stride >> 1;

	uint32_t upload_x = 0;
	uint32_t upload_y = 0;

	auto upload_mode = info.mode;

	if (upload_mode == UploadMode::Block)
	{
		upload_x = info.slo;
		upload_y = info.tlo;

		// LoadBlock is kinda awkward. Rather than specifying width and height, we get width and dTdx.
		// dTdx will increment and generate a T coordinate based on S coordinate (T = (S_64bpp_word * dTdx) >> 11).
		// The stride is added on top of this, so effective stride is stride(T) + stride(tile).
		// Usually it makes sense for stride(tile) to be 0, but it doesn't have to be ...
		// The only reasonable solution is to try to decompose this mess into a normal width/height/stride.
		// In the general dTdx case, we don't have to deduce a stable value for stride.
		// If dTdx is very weird, we might get variable stride, which is near-impossible to deal with.
		// However, it makes zero sense for content to actually rely on this behavior.
		// Even if there are inaccuracies in the fraction, we always floor it to get T, and thus we'll have to run
		// for quite some time to observe the fractional error accumulate.

		unsigned pixel_count = (info.shi - info.slo + 1) & 0xfff;

		unsigned dt = info.thi;

		unsigned max_tmem_iteration = (pixel_count - 1) >> (4u - unsigned(info.size));
		unsigned max_t = (max_tmem_iteration * dt) >> 11;

		if (max_t != 0)
		{
			// dT is an inverse which is not necessarily accurate, we can end up with an uneven amount of
			// texels per "line". If we have stride == 0, this is fairly easy to deal with,
			// but for the case where stride != 0, it is very difficult to implement it correctly.
			// We will need to solve this kind of equation for X:

			// TMEM word = floor((x * dt) / 2048) * stride + x
			// This equation has no solutions for cases where we stride over TMEM words.
			// The only way I can think of is to test all candidates for the floor() expression, and see if that is a valid solution.
			// We can find an conservative estimate for floor() by:
			// t_min = TMEM word / (max_num_64bpp_elements + stride)
			// t_max = TMEM word / (min_num_64bpp_elements + stride)
			unsigned max_num_64bpp_elements_before_wrap = ((1u << 11u) + dt - 1u) / dt;
			unsigned min_num_64bpp_elements_before_wrap = (1u << 11u) / dt;

			bool uneven_dt = max_num_64bpp_elements_before_wrap != min_num_64bpp_elements_before_wrap;

			if (uneven_dt)
			{
				// If we never get rounding errors, we can handwave this issue away and pretend that min == max iterations.
				// This is by far the common case.

				// Each overflow into next T adds a certain amount of error.
				unsigned overflow_amt = dt * max_num_64bpp_elements_before_wrap - (1 << 11);

				// Multiply this by maximum value of T we can observe, and we have a conservative estimate for our T error.
				overflow_amt *= max_t;

				// If this error is less than 1 step of dt, we can be certain that we will get max_num iterations every time,
				// and we can ignore the worst edge cases.
				if (overflow_amt < dt)
				{
					min_num_64bpp_elements_before_wrap = max_num_64bpp_elements_before_wrap;
					uneven_dt = false;
				}
			}

			// Add more precision bits to DXT. We might have to shift it down if we have a meta.size fixup down below.
			// Also makes the right shift nicer (16 vs 11).
			upload.dxt = dt << 5;

			if (meta.size == TextureSize::Bpp32 || meta.fmt == TextureFormat::YUV)
			{
				// We iterate twice for Bpp32 and YUV to complete a 64bpp word.
				upload.tmem_stride_words <<= 1;

				// Pure, utter insanity, but no content should *ever* hit this ...
				if (uneven_dt && meta.size != info.size)
				{
					LOGE("Got uneven_dt, and texture size != tile size.\n");
					return;
				}
			}

			// If TMEM and VRAM bpp misalign, we need to fixup this since we step too fast or slow.
			if (unsigned(meta.size) > unsigned(info.size))
			{
				unsigned shamt = unsigned(meta.size) - unsigned(info.size);
				max_num_64bpp_elements_before_wrap <<= shamt;
				min_num_64bpp_elements_before_wrap <<= shamt;
				// Need to step slower so we can handle the added striding.
				upload.dxt >>= shamt;
			}
			else if (unsigned(info.size) > unsigned(meta.size))
			{
				// Here we step multiple times over the same pixel, but potentially with different T state,
				// since dTdx applies between the iterations.
				// Horrible, horrible mess ...
				LOGE("LoadBlock: VRAM bpp size is larger than tile bpp. This is unsupported.\n");
				return;
			}

			unsigned max_line_stride_64bpp = max_num_64bpp_elements_before_wrap + (upload.tmem_stride_words >> 2);
			unsigned min_line_stride_64bpp = min_num_64bpp_elements_before_wrap + (upload.tmem_stride_words >> 2);

			// Multiplying 64bpp TMEM word by these gives us lower and upper bounds for T.
			// These serve as candidate expressions for floor().
			float min_t_mod = 1.0f / float(max_line_stride_64bpp);
			float max_t_mod = 1.0f / float(min_line_stride_64bpp);
			upload.min_t_mod = min_t_mod;
			upload.max_t_mod = max_t_mod;

			upload.width = pixel_count;
			upload.height = 1;
			upload.tmem_stride_words >>= 2; // Stride in 64bpp instead of 16bpp.
		}
		else
		{
			// We never trigger a case where T is non-zero, so this is equivalent to a Tile upload.
			upload.width = pixel_count;
			upload.height = 1;
			upload.tmem_stride_words = 0;
			upload_mode = UploadMode::Tile;
		}
	}
	else
	{
		upload_x = info.slo >> 2;
		upload_y = info.tlo >> 2;
		upload.width = (((info.shi >> 2) - (info.slo >> 2)) + 1) & 0xfff;
		upload.height = ((info.thi >> 2) - (info.tlo >> 2)) + 1;
	}

	if (!upload.width)
		return;

	switch (info.size)
	{
	case TextureSize::Bpp8:
		upload.vram_effective_width = (upload.width + 7) & ~7;
		break;

	case TextureSize::Bpp16:
		// In 16-bit VRAM pointer with TLUT, we iterate one texel at a time, not 4.
		if (upload_mode == UploadMode::TLUT)
			upload.vram_effective_width = upload.width;
		else
			upload.vram_effective_width = (upload.width + 3) & ~3;
		break;

	case TextureSize::Bpp32:
		upload.vram_effective_width = (upload.width + 1) & ~1;
		break;

	default:
		break;
	}

	// Uploads happen in chunks of 8 bytes in groups of 4x16-bits.
	switch (meta.size)
	{
	case TextureSize::Bpp4:
		upload.width = (upload.width + 15) & ~15;
		upload.width >>= 2;
		break;

	case TextureSize::Bpp8:
		upload.width = (upload.width + 7) & ~7;
		upload.width >>= 1;
		break;

	case TextureSize::Bpp16:
		upload.width = (upload.width + 3) & ~3;
		// Consider YUV uploads to be 32bpp since that's kinda what they are.
		if (meta.fmt == TextureFormat::YUV)
			upload.width >>= 1;
		break;

	case TextureSize::Bpp32:
		upload.width = (upload.width + 1) & ~1;
		break;

	default:
		LOGE("Unimplemented!\n");
		break;
	}

	if (upload.height > 1 && upload_mode == UploadMode::TLUT)
	{
		LOGE("Load TLUT with height > 1 is not supported.\n");
		return;
	}

	upload.vram_addr = info.tex_addr + ((info.tex_width * upload_y + upload_x) << (unsigned(info.size) - 1));
	upload.vram_width = upload_mode == UploadMode::Block ? upload.vram_effective_width : info.tex_width;
	upload.vram_size = int32_t(info.size);

	upload.tmem_offset = (meta.offset + tmem_offset) & 0xfff;
	upload.tmem_size = int32_t(meta.size);
	upload.tmem_fmt = int32_t(meta.fmt);
	upload.mode = int32_t(upload_mode);

	upload.inv_tmem_stride_words = 1.0f / float(upload.tmem_stride_words);

	stream.tmem_upload_infos.push_back(upload);
	if (stream.tmem_upload_infos.size() + 1 >= Limits::MaxTMEMInstances)
		flush_queues();
}

void Renderer::set_blend_color(uint32_t color)
{
	constants.blend_color = color;
}

void Renderer::set_fog_color(uint32_t color)
{
	constants.fog_color = color;
}

void Renderer::set_env_color(uint32_t color)
{
	constants.env_color = color;
}

void Renderer::set_fill_color(uint32_t color)
{
	constants.fill_color = color;
}

void Renderer::set_primitive_depth(uint16_t prim_depth, uint16_t prim_dz)
{
	constants.prim_depth = int32_t(prim_depth & 0x7fff) << 16;
	constants.prim_dz = prim_dz;
}

void Renderer::set_enable_primitive_depth(bool enable)
{
	constants.use_prim_depth = enable;
}

void Renderer::set_convert(uint16_t k0, uint16_t k1, uint16_t k2, uint16_t k3, uint16_t k4, uint16_t k5)
{
	constants.convert[0] = 2 * sext<9>(k0) + 1;
	constants.convert[1] = 2 * sext<9>(k1) + 1;
	constants.convert[2] = 2 * sext<9>(k2) + 1;
	constants.convert[3] = 2 * sext<9>(k3) + 1;
	constants.convert[4] = k4;
	constants.convert[5] = k5;
}

void Renderer::set_color_key(unsigned component, uint32_t width, uint32_t center, uint32_t scale)
{
	constants.key_width[component] = width;
	constants.key_center[component] = center;
	constants.key_scale[component] = scale;
}

void Renderer::set_primitive_color(uint8_t min_level, uint8_t prim_lod_frac, uint32_t color)
{
	constants.primitive_color = color;
	constants.min_level = min_level;
	constants.prim_lod_frac = prim_lod_frac;
}

bool Renderer::can_support_minimum_subgroup_size(unsigned size) const
{
	return supports_subgroup_size_control(size, device->get_device_features().subgroup_properties.subgroupSize);
}

bool Renderer::supports_subgroup_size_control(uint32_t minimum_size, uint32_t maximum_size) const
{
	auto &features = device->get_device_features();

	if (!features.subgroup_size_control_features.computeFullSubgroups)
		return false;

	bool use_varying = minimum_size <= features.subgroup_size_control_properties.minSubgroupSize &&
	                   maximum_size >= features.subgroup_size_control_properties.maxSubgroupSize;

	if (!use_varying)
	{
		bool outside_range = minimum_size > features.subgroup_size_control_properties.maxSubgroupSize ||
		                     maximum_size < features.subgroup_size_control_properties.minSubgroupSize;
		if (outside_range)
			return false;

		if ((features.subgroup_size_control_properties.requiredSubgroupSizeStages & VK_SHADER_STAGE_COMPUTE_BIT) == 0)
			return false;
	}

	return true;
}

void Renderer::PipelineExecutor::perform_work(const Vulkan::DeferredPipelineCompile &compile) const
{
	auto start_ts = device->write_calibrated_timestamp();
	Vulkan::CommandBuffer::build_compute_pipeline(device, compile);
	auto end_ts = device->write_calibrated_timestamp();
	device->register_time_interval("RDP Pipeline", std::move(start_ts), std::move(end_ts),
	                               "pipeline-compilation", std::to_string(compile.hash));
}

bool Renderer::PipelineExecutor::is_sentinel(const Vulkan::DeferredPipelineCompile &compile) const
{
	return compile.hash == 0;
}

void Renderer::PipelineExecutor::notify_work_locked(const Vulkan::DeferredPipelineCompile &) const
{
}
}
