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
#include "logging.hpp"
#include "vulkan_common.hpp"
#include <memory>
#include <functional>

namespace Util
{
class TimelineTraceFile;
}

namespace Granite
{
class Filesystem;
class ThreadGroup;
}

namespace Vulkan
{
struct DeviceFeatures
{
	bool supports_physical_device_properties2 = false;
	bool supports_external = false;
	bool supports_dedicated = false;
	bool supports_image_format_list = false;
	bool supports_debug_utils = false;
	bool supports_mirror_clamp_to_edge = false;
	bool supports_google_display_timing = false;
	bool supports_nv_device_diagnostic_checkpoints = false;
	bool supports_vulkan_11_instance = false;
	bool supports_vulkan_11_device = false;
	bool supports_external_memory_host = false;
	bool supports_surface_capabilities2 = false;
	bool supports_full_screen_exclusive = false;
	bool supports_update_template = false;
	bool supports_maintenance_1 = false;
	bool supports_maintenance_2 = false;
	bool supports_maintenance_3 = false;
	bool supports_descriptor_indexing = false;
	bool supports_conservative_rasterization = false;
	bool supports_bind_memory2 = false;
	bool supports_get_memory_requirements2 = false;
	bool supports_draw_indirect_count = false;
	bool supports_draw_parameters = false;
	bool supports_driver_properties = false;
	bool supports_calibrated_timestamps = false;
	bool supports_memory_budget = false;
	bool supports_astc_decode_mode = false;
	bool supports_sync2 = false;
	bool supports_video_queue = false;
	bool supports_video_decode_queue = false;
	bool supports_video_decode_h264 = false;
	VkPhysicalDeviceSubgroupProperties subgroup_properties = {};
	VkPhysicalDevice8BitStorageFeaturesKHR storage_8bit_features = {};
	VkPhysicalDevice16BitStorageFeaturesKHR storage_16bit_features = {};
	VkPhysicalDeviceFloat16Int8FeaturesKHR float16_int8_features = {};
	VkPhysicalDeviceFeatures enabled_features = {};
	VkPhysicalDeviceExternalMemoryHostPropertiesEXT host_memory_properties = {};
	VkPhysicalDeviceMultiviewFeaturesKHR multiview_features = {};
	VkPhysicalDeviceSubgroupSizeControlFeaturesEXT subgroup_size_control_features = {};
	VkPhysicalDeviceSubgroupSizeControlPropertiesEXT subgroup_size_control_properties = {};
	VkPhysicalDeviceComputeShaderDerivativesFeaturesNV compute_shader_derivative_features = {};
	VkPhysicalDeviceHostQueryResetFeaturesEXT host_query_reset_features = {};
	VkPhysicalDeviceShaderDemoteToHelperInvocationFeaturesEXT demote_to_helper_invocation_features = {};
	VkPhysicalDeviceScalarBlockLayoutFeaturesEXT scalar_block_features = {};
	VkPhysicalDeviceUniformBufferStandardLayoutFeaturesKHR ubo_std430_features = {};
	VkPhysicalDeviceTimelineSemaphoreFeaturesKHR timeline_semaphore_features = {};
	VkPhysicalDeviceDescriptorIndexingFeaturesEXT descriptor_indexing_features = {};
	VkPhysicalDeviceDescriptorIndexingPropertiesEXT descriptor_indexing_properties = {};
	VkPhysicalDeviceConservativeRasterizationPropertiesEXT conservative_rasterization_properties = {};
	VkPhysicalDevicePerformanceQueryFeaturesKHR performance_query_features = {};
	VkPhysicalDeviceSamplerYcbcrConversionFeaturesKHR sampler_ycbcr_conversion_features = {};
	VkPhysicalDeviceDriverPropertiesKHR driver_properties = {};
	VkPhysicalDeviceMemoryPriorityFeaturesEXT memory_priority_features = {};
	VkPhysicalDeviceASTCDecodeFeaturesEXT astc_decode_features = {};
	VkPhysicalDeviceTextureCompressionASTCHDRFeaturesEXT astc_hdr_features = {};
	VkPhysicalDeviceSynchronization2FeaturesKHR sync2_features = {};
};

enum VendorID
{
	VENDOR_ID_AMD = 0x1002,
	VENDOR_ID_NVIDIA = 0x10de,
	VENDOR_ID_INTEL = 0x8086,
	VENDOR_ID_ARM = 0x13b5,
	VENDOR_ID_QCOM = 0x5143
};

enum ContextCreationFlagBits
{
	CONTEXT_CREATION_DISABLE_BINDLESS_BIT = 1 << 0
};
using ContextCreationFlags = uint32_t;

struct QueueInfo
{
	QueueInfo();
	VkQueue queues[QUEUE_INDEX_COUNT] = {};
	uint32_t family_indices[QUEUE_INDEX_COUNT];
	uint32_t timestamp_valid_bits = 0;
};

class Context
{
public:
	bool init_instance_and_device(const char **instance_ext, uint32_t instance_ext_count, const char **device_ext, uint32_t device_ext_count,
	                              ContextCreationFlags flags = 0);
	bool init_from_instance_and_device(VkInstance instance, VkPhysicalDevice gpu, VkDevice device, VkQueue queue, uint32_t queue_family);
	bool init_device_from_instance(VkInstance instance, VkPhysicalDevice gpu, VkSurfaceKHR surface, const char **required_device_extensions,
	                               unsigned num_required_device_extensions, const char **required_device_layers,
	                               unsigned num_required_device_layers, const VkPhysicalDeviceFeatures *required_features,
	                               ContextCreationFlags flags = 0);

	Context() = default;
	Context(const Context &) = delete;
	void operator=(const Context &) = delete;
	static bool init_loader(PFN_vkGetInstanceProcAddr addr);
	static PFN_vkGetInstanceProcAddr get_instance_proc_addr();

	~Context();

	VkInstance get_instance() const
	{
		return instance;
	}

	VkPhysicalDevice get_gpu() const
	{
		return gpu;
	}

	VkDevice get_device() const
	{
		return device;
	}

	const QueueInfo &get_queue_info() const
	{
		return queue_info;
	}

	const VkPhysicalDeviceProperties &get_gpu_props() const
	{
		return gpu_props;
	}

	const VkPhysicalDeviceMemoryProperties &get_mem_props() const
	{
		return mem_props;
	}

	void release_instance()
	{
		owned_instance = false;
	}

	void release_device()
	{
		owned_device = false;
	}

	const DeviceFeatures &get_enabled_device_features() const
	{
		return ext;
	}

	static const VkApplicationInfo &get_application_info(bool supports_vulkan_11);

	void notify_validation_error(const char *msg);
	void set_notification_callback(std::function<void (const char *)> func);

	void set_num_thread_indices(unsigned indices)
	{
		num_thread_indices = indices;
	}

	unsigned get_num_thread_indices() const
	{
		return num_thread_indices;
	}

	const VolkDeviceTable &get_device_table() const
	{
		return device_table;
	}

	struct SystemHandles
	{
		Util::TimelineTraceFile *timeline_trace_file = nullptr;
		Granite::Filesystem *filesystem = nullptr;
		Granite::ThreadGroup *thread_group = nullptr;
	};

	void set_system_handles(const SystemHandles &handles_)
	{
		handles = handles_;
	}

	const SystemHandles &get_system_handles() const
	{
		return handles;
	}

private:
	VkDevice device = VK_NULL_HANDLE;
	VkInstance instance = VK_NULL_HANDLE;
	VkPhysicalDevice gpu = VK_NULL_HANDLE;
	VolkDeviceTable device_table = {};
	SystemHandles handles;

	VkPhysicalDeviceProperties gpu_props = {};
	VkPhysicalDeviceMemoryProperties mem_props = {};

	QueueInfo queue_info;
	unsigned num_thread_indices = 1;

	bool create_instance(const char **instance_ext, uint32_t instance_ext_count);
	bool create_device(VkPhysicalDevice gpu, VkSurfaceKHR surface, const char **required_device_extensions,
	                   unsigned num_required_device_extensions, const char **required_device_layers,
	                   unsigned num_required_device_layers, const VkPhysicalDeviceFeatures *required_features,
	                   ContextCreationFlags flags);

	bool owned_instance = false;
	bool owned_device = false;
	DeviceFeatures ext;

#ifdef VULKAN_DEBUG
	VkDebugUtilsMessengerEXT debug_messenger = VK_NULL_HANDLE;
#endif
	std::function<void (const char *)> message_callback;

	void destroy();
	void check_descriptor_indexing_features();
	bool force_no_validation = false;
};
}
