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

#include "query_pool.hpp"
#include "device.hpp"
#include <utility>

using namespace std;

namespace Vulkan
{
static const char *storage_to_str(VkPerformanceCounterStorageKHR storage)
{
	switch (storage)
	{
	case VK_PERFORMANCE_COUNTER_STORAGE_FLOAT32_KHR:
		return "float32";
	case VK_PERFORMANCE_COUNTER_STORAGE_FLOAT64_KHR:
		return "float32";
	case VK_PERFORMANCE_COUNTER_STORAGE_INT32_KHR:
		return "int32";
	case VK_PERFORMANCE_COUNTER_STORAGE_INT64_KHR:
		return "int64";
	case VK_PERFORMANCE_COUNTER_STORAGE_UINT32_KHR:
		return "uint32";
	case VK_PERFORMANCE_COUNTER_STORAGE_UINT64_KHR:
		return "uint64";
	default:
		return "???";
	}
}

static const char *scope_to_str(VkPerformanceCounterScopeKHR scope)
{
	switch (scope)
	{
	case VK_QUERY_SCOPE_COMMAND_BUFFER_KHR:
		return "command buffer";
	case VK_QUERY_SCOPE_RENDER_PASS_KHR:
		return "render pass";
	case VK_QUERY_SCOPE_COMMAND_KHR:
		return "command";
	default:
		return "???";
	}
}

static const char *unit_to_str(VkPerformanceCounterUnitKHR unit)
{
	switch (unit)
	{
	case VK_PERFORMANCE_COUNTER_UNIT_AMPS_KHR:
		return "A";
	case VK_PERFORMANCE_COUNTER_UNIT_BYTES_KHR:
		return "bytes";
	case VK_PERFORMANCE_COUNTER_UNIT_BYTES_PER_SECOND_KHR:
		return "bytes / second";
	case VK_PERFORMANCE_COUNTER_UNIT_CYCLES_KHR:
		return "cycles";
	case VK_PERFORMANCE_COUNTER_UNIT_GENERIC_KHR:
		return "units";
	case VK_PERFORMANCE_COUNTER_UNIT_HERTZ_KHR:
		return "Hz";
	case VK_PERFORMANCE_COUNTER_UNIT_KELVIN_KHR:
		return "K";
	case VK_PERFORMANCE_COUNTER_UNIT_NANOSECONDS_KHR:
		return "ns";
	case VK_PERFORMANCE_COUNTER_UNIT_PERCENTAGE_KHR:
		return "%";
	case VK_PERFORMANCE_COUNTER_UNIT_VOLTS_KHR:
		return "V";
	case VK_PERFORMANCE_COUNTER_UNIT_WATTS_KHR:
		return "W";
	default:
		return "???";
	}
}

void PerformanceQueryPool::init_device(Device *device_, uint32_t queue_family_index_)
{
	device = device_;
	queue_family_index = queue_family_index_;

	if (!device->get_device_features().performance_query_features.performanceCounterQueryPools)
		return;

	uint32_t num_counters = 0;
	if (vkEnumeratePhysicalDeviceQueueFamilyPerformanceQueryCountersKHR(
			device->get_physical_device(),
			queue_family_index,
			&num_counters,
			nullptr, nullptr) != VK_SUCCESS)
	{
		LOGE("Failed to enumerate performance counters.\n");
		return;
	}

	counters.resize(num_counters);
	counter_descriptions.resize(num_counters);

	if (vkEnumeratePhysicalDeviceQueueFamilyPerformanceQueryCountersKHR(
			device->get_physical_device(),
			queue_family_index,
			&num_counters,
			counters.data(), counter_descriptions.data()) != VK_SUCCESS)
	{
		LOGE("Failed to enumerate performance counters.\n");
		return;
	}

	LOGI("Available performance counters for queue family: %u\n", queue_family_index);
	for (uint32_t i = 0; i < num_counters; i++)
	{
		LOGI("  %s: %s\n", counter_descriptions[i].name, counter_descriptions[i].description);
		LOGI("    Storage: %s\n", storage_to_str(counters[i].storage));
		LOGI("    Scope: %s\n", scope_to_str(counters[i].scope));
		LOGI("    Unit: %s\n", unit_to_str(counters[i].unit));
	}
}

PerformanceQueryPool::~PerformanceQueryPool()
{
	if (pool)
		device->get_device_table().vkDestroyQueryPool(device->get_device(), pool, nullptr);
}

void PerformanceQueryPool::begin_command_buffer(VkCommandBuffer cmd)
{
	if (!pool)
		return;

	auto &table = device->get_device_table();
	table.vkResetQueryPoolEXT(device->get_device(), pool, 0, 0);
	table.vkCmdBeginQuery(cmd, pool, 0, 0);

	VkMemoryBarrier barrier = { VK_STRUCTURE_TYPE_MEMORY_BARRIER };
	barrier.srcAccessMask = VK_ACCESS_MEMORY_WRITE_BIT;
	barrier.dstAccessMask = VK_ACCESS_MEMORY_WRITE_BIT | VK_ACCESS_MEMORY_READ_BIT;
	table.vkCmdPipelineBarrier(cmd, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
	                           0, 1, &barrier, 0, nullptr, 0, nullptr);
}

void PerformanceQueryPool::end_command_buffer(VkCommandBuffer cmd)
{
	if (!pool)
		return;

	auto &table = device->get_device_table();

	VkMemoryBarrier barrier = { VK_STRUCTURE_TYPE_MEMORY_BARRIER };
	barrier.srcAccessMask = VK_ACCESS_MEMORY_WRITE_BIT;
	barrier.dstAccessMask = VK_ACCESS_MEMORY_WRITE_BIT | VK_ACCESS_MEMORY_READ_BIT;
	table.vkCmdPipelineBarrier(cmd, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT, VK_PIPELINE_STAGE_ALL_COMMANDS_BIT,
	                           0, 1, &barrier, 0, nullptr, 0, nullptr);
	table.vkCmdEndQuery(cmd, pool, 0);
}

void PerformanceQueryPool::report()
{
	auto &table = device->get_device_table();
	if (table.vkGetQueryPoolResults(device->get_device(), pool,
	                                0, 1,
	                                results.size() * sizeof(VkPerformanceCounterResultKHR),
	                                results.data(),
	                                sizeof(VkPerformanceCounterResultKHR),
	                                0) != VK_SUCCESS)
	{
		LOGE("Getting performance counters did not succeed.\n");
	}

	size_t num_counters = results.size();

	LOGI("\n=== Profiling result ===\n");
	for (size_t i = 0; i < num_counters; i++)
	{
		auto &counter = counters[active_indices[i]];
		auto &desc = counter_descriptions[active_indices[i]];

		switch (counter.storage)
		{
		case VK_PERFORMANCE_COUNTER_STORAGE_INT32_KHR:
			LOGI(" %s (%s): %d %s\n", desc.name, desc.description, results[i].int32, unit_to_str(counter.unit));
			break;
		case VK_PERFORMANCE_COUNTER_STORAGE_INT64_KHR:
			LOGI(" %s (%s): %lld %s\n", desc.name, desc.description, static_cast<long long>(results[i].int64), unit_to_str(counter.unit));
			break;
		case VK_PERFORMANCE_COUNTER_STORAGE_UINT32_KHR:
			LOGI(" %s (%s): %u %s\n", desc.name, desc.description, results[i].uint32, unit_to_str(counter.unit));
			break;
		case VK_PERFORMANCE_COUNTER_STORAGE_UINT64_KHR:
			LOGI(" %s (%s): %llu %s\n", desc.name, desc.description, static_cast<long long>(results[i].uint64), unit_to_str(counter.unit));
			break;
		case VK_PERFORMANCE_COUNTER_STORAGE_FLOAT32_KHR:
			LOGI(" %s (%s): %g %s\n", desc.name, desc.description, results[i].float32, unit_to_str(counter.unit));
			break;
		case VK_PERFORMANCE_COUNTER_STORAGE_FLOAT64_KHR:
			LOGI(" %s (%s): %g %s\n", desc.name, desc.description, results[i].float64, unit_to_str(counter.unit));
			break;
		default:
			break;
		}
	}
	LOGI("================================\n\n");
}

uint32_t PerformanceQueryPool::get_num_counters() const
{
	return uint32_t(counters.size());
}

const VkPerformanceCounterKHR *PerformanceQueryPool::get_available_counters() const
{
	return counters.data();
}

const VkPerformanceCounterDescriptionKHR *PerformanceQueryPool::get_available_counter_descs() const
{
	return counter_descriptions.data();
}

bool PerformanceQueryPool::init_counters(const std::vector<std::string> &counter_names)
{
	if (!device->get_device_features().performance_query_features.performanceCounterQueryPools)
	{
		LOGE("Device does not support VK_KHR_performance_query.\n");
		return false;
	}

	if (!device->get_device_features().host_query_reset_features.hostQueryReset)
	{
		LOGE("Device does not support host query reset.\n");
		return false;
	}

	auto &table = device->get_device_table();
	if (pool)
		table.vkDestroyQueryPool(device->get_device(), pool, nullptr);
	pool = VK_NULL_HANDLE;

	VkQueryPoolPerformanceCreateInfoKHR performance_info = { VK_STRUCTURE_TYPE_QUERY_POOL_PERFORMANCE_CREATE_INFO_KHR };
	VkQueryPoolCreateInfo info = { VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO };
	info.pNext = &performance_info;

	info.queryType = VK_QUERY_TYPE_PERFORMANCE_QUERY_KHR;
	info.queryCount = 1;

	active_indices.clear();

	for (auto &name : counter_names)
	{
		auto itr = find_if(begin(counter_descriptions), end(counter_descriptions), [&](const VkPerformanceCounterDescriptionKHR &desc) {
			return name == desc.name;
		});

		if (itr != end(counter_descriptions))
		{
			LOGI("Found counter %s: %s\n", itr->name, itr->description);
			active_indices.push_back(itr - begin(counter_descriptions));
		}
	}

	if (active_indices.empty())
	{
		LOGW("No performance counters were enabled.\n");
		return false;
	}

	performance_info.queueFamilyIndex = queue_family_index;
	performance_info.counterIndexCount = active_indices.size();
	performance_info.pCounterIndices = active_indices.data();
	results.resize(active_indices.size());

	uint32_t num_passes = 0;
	vkGetPhysicalDeviceQueueFamilyPerformanceQueryPassesKHR(device->get_physical_device(),
	                                                        &performance_info, &num_passes);

	if (num_passes != 1)
	{
		LOGE("Implementation requires %u passes to query performance counters. Cannot create query pool.\n",
		     num_passes);
		return false;
	}

	if (table.vkCreateQueryPool(device->get_device(), &info, nullptr, &pool) != VK_SUCCESS)
	{
		LOGE("Failed to create performance query pool.\n");
		return false;
	}

	return true;
}

QueryPool::QueryPool(Device *device_)
	: device(device_)
	, table(device_->get_device_table())
{
	supports_timestamp = device->get_gpu_properties().limits.timestampComputeAndGraphics;

	// Ignore timestampValidBits and friends for now.
	if (supports_timestamp)
		add_pool();
}

QueryPool::~QueryPool()
{
	for (auto &pool : pools)
		table.vkDestroyQueryPool(device->get_device(), pool.pool, nullptr);
}

void QueryPool::begin()
{
	for (unsigned i = 0; i <= pool_index; i++)
	{
		if (i >= pools.size())
			continue;

		auto &pool = pools[i];
		if (pool.index == 0)
			continue;

		table.vkGetQueryPoolResults(device->get_device(), pool.pool,
		                            0, pool.index,
		                            pool.index * sizeof(uint64_t),
		                            pool.query_results.data(),
		                            sizeof(uint64_t),
		                            VK_QUERY_RESULT_64_BIT | VK_QUERY_RESULT_WAIT_BIT);

		for (unsigned j = 0; j < pool.index; j++)
			pool.cookies[j]->signal_timestamp_ticks(pool.query_results[j]);

		if (device->get_device_features().host_query_reset_features.hostQueryReset)
			table.vkResetQueryPoolEXT(device->get_device(), pool.pool, 0, pool.index);
	}

	pool_index = 0;
	for (auto &pool : pools)
		pool.index = 0;
}

void QueryPool::add_pool()
{
	VkQueryPoolCreateInfo pool_info = { VK_STRUCTURE_TYPE_QUERY_POOL_CREATE_INFO };
	pool_info.queryType = VK_QUERY_TYPE_TIMESTAMP;
	pool_info.queryCount = 64;

	Pool pool;
	table.vkCreateQueryPool(device->get_device(), &pool_info, nullptr, &pool.pool);
	pool.size = pool_info.queryCount;
	pool.index = 0;
	pool.query_results.resize(pool.size);
	pool.cookies.resize(pool.size);

	if (device->get_device_features().host_query_reset_features.hostQueryReset)
		table.vkResetQueryPoolEXT(device->get_device(), pool.pool, 0, pool.size);

	pools.push_back(move(pool));
}

QueryPoolHandle QueryPool::write_timestamp(VkCommandBuffer cmd, VkPipelineStageFlagBits stage)
{
	if (!supports_timestamp)
	{
		LOGI("Timestamps are not supported on this implementation.\n");
		return {};
	}

	if (pools[pool_index].index >= pools[pool_index].size)
		pool_index++;

	if (pool_index >= pools.size())
		add_pool();

	auto &pool = pools[pool_index];

	auto cookie = QueryPoolHandle(device->handle_pool.query.allocate(device, true));
	pool.cookies[pool.index] = cookie;

	if (!device->get_device_features().host_query_reset_features.hostQueryReset)
		table.vkCmdResetQueryPool(cmd, pool.pool, pool.index, 1);
	table.vkCmdWriteTimestamp(cmd, stage, pool.pool, pool.index);

	pool.index++;
	return cookie;
}

void QueryPoolResultDeleter::operator()(QueryPoolResult *query)
{
	query->device->handle_pool.query.free(query);
}

void TimestampInterval::mark_end_of_frame_context()
{
	if (total_time > 0.0)
		total_frame_iterations++;
}

uint64_t TimestampInterval::get_total_accumulations() const
{
	return total_accumulations;
}

uint64_t TimestampInterval::get_total_frame_iterations() const
{
	return total_frame_iterations;
}

double TimestampInterval::get_total_time() const
{
	return total_time;
}

void TimestampInterval::accumulate_time(double t)
{
	total_time += t;
	total_accumulations++;
}

double TimestampInterval::get_time_per_iteration() const
{
	if (total_frame_iterations)
		return total_time / double(total_frame_iterations);
	else
		return 0.0;
}

double TimestampInterval::get_time_per_accumulation() const
{
	if (total_accumulations)
		return total_time / double(total_accumulations);
	else
		return 0.0;
}

const string &TimestampInterval::get_tag() const
{
	return tag;
}

TimestampInterval::TimestampInterval(string tag_)
	: tag(move(tag_))
{
}

TimestampInterval *TimestampIntervalManager::get_timestamp_tag(const char *tag)
{
	Util::Hasher h;
	h.string(tag);
	return timestamps.emplace_yield(h.get(), tag);
}

void TimestampIntervalManager::mark_end_of_frame_context()
{
	for (auto &timestamp : timestamps)
		timestamp.mark_end_of_frame_context();
}

void TimestampIntervalManager::log_simple()
{
	for (auto &timestamp : timestamps)
	{
		LOGI("Timestamp tag report: %s\n", timestamp.get_tag().c_str());
		if (timestamp.get_total_frame_iterations())
		{
			LOGI("  %.3f ms / iteration\n", 1000.0 * timestamp.get_time_per_accumulation());
			LOGI("  %.3f ms / frame context\n", 1000.0 * timestamp.get_time_per_iteration());
			LOGI("  %.3f iterations / frame context\n",
			     double(timestamp.get_total_accumulations()) / double(timestamp.get_total_frame_iterations()));
		}
	}
}
}