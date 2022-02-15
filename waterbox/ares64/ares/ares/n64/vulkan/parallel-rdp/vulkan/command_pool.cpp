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

#include "command_pool.hpp"
#include "device.hpp"

namespace Vulkan
{
CommandPool::CommandPool(Device *device_, uint32_t queue_family_index)
    : device(device_), table(&device_->get_device_table())
{
	VkCommandPoolCreateInfo info = { VK_STRUCTURE_TYPE_COMMAND_POOL_CREATE_INFO };
	info.flags = VK_COMMAND_POOL_CREATE_TRANSIENT_BIT;
	info.queueFamilyIndex = queue_family_index;
	if (queue_family_index != VK_QUEUE_FAMILY_IGNORED)
		table->vkCreateCommandPool(device->get_device(), &info, nullptr, &pool);
}

CommandPool::CommandPool(CommandPool &&other) noexcept
{
	*this = std::move(other);
}

CommandPool &CommandPool::operator=(CommandPool &&other) noexcept
{
	if (this != &other)
	{
		device = other.device;
		table = other.table;
		if (!buffers.empty())
			table->vkFreeCommandBuffers(device->get_device(), pool, buffers.size(), buffers.data());
		if (pool != VK_NULL_HANDLE)
			table->vkDestroyCommandPool(device->get_device(), pool, nullptr);

		pool = VK_NULL_HANDLE;
		buffers.clear();
		std::swap(pool, other.pool);
		std::swap(buffers, other.buffers);
		index = other.index;
		other.index = 0;
#ifdef VULKAN_DEBUG
		in_flight.clear();
		std::swap(in_flight, other.in_flight);
#endif
	}
	return *this;
}

CommandPool::~CommandPool()
{
	if (!buffers.empty())
		table->vkFreeCommandBuffers(device->get_device(), pool, buffers.size(), buffers.data());
	if (!secondary_buffers.empty())
		table->vkFreeCommandBuffers(device->get_device(), pool, secondary_buffers.size(), secondary_buffers.data());
	if (pool != VK_NULL_HANDLE)
		table->vkDestroyCommandPool(device->get_device(), pool, nullptr);
}

void CommandPool::signal_submitted(VkCommandBuffer cmd)
{
#ifdef VULKAN_DEBUG
	VK_ASSERT(in_flight.find(cmd) != end(in_flight));
	in_flight.erase(cmd);
#else
	(void)cmd;
#endif
}

VkCommandBuffer CommandPool::request_secondary_command_buffer()
{
	VK_ASSERT(pool != VK_NULL_HANDLE);

	if (secondary_index < secondary_buffers.size())
	{
		auto ret = secondary_buffers[secondary_index++];
#ifdef VULKAN_DEBUG
		VK_ASSERT(in_flight.find(ret) == end(in_flight));
		in_flight.insert(ret);
#endif
		return ret;
	}
	else
	{
		VkCommandBuffer cmd;
		VkCommandBufferAllocateInfo info = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
		info.commandPool = pool;
		info.level = VK_COMMAND_BUFFER_LEVEL_SECONDARY;
		info.commandBufferCount = 1;

		table->vkAllocateCommandBuffers(device->get_device(), &info, &cmd);
#ifdef VULKAN_DEBUG
		VK_ASSERT(in_flight.find(cmd) == end(in_flight));
		in_flight.insert(cmd);
#endif
		secondary_buffers.push_back(cmd);
		secondary_index++;
		return cmd;
	}
}

VkCommandBuffer CommandPool::request_command_buffer()
{
	VK_ASSERT(pool != VK_NULL_HANDLE);

	if (index < buffers.size())
	{
		auto ret = buffers[index++];
#ifdef VULKAN_DEBUG
		VK_ASSERT(in_flight.find(ret) == end(in_flight));
		in_flight.insert(ret);
#endif
		return ret;
	}
	else
	{
		VkCommandBuffer cmd;
		VkCommandBufferAllocateInfo info = { VK_STRUCTURE_TYPE_COMMAND_BUFFER_ALLOCATE_INFO };
		info.commandPool = pool;
		info.level = VK_COMMAND_BUFFER_LEVEL_PRIMARY;
		info.commandBufferCount = 1;

		table->vkAllocateCommandBuffers(device->get_device(), &info, &cmd);
#ifdef VULKAN_DEBUG
		VK_ASSERT(in_flight.find(cmd) == end(in_flight));
		in_flight.insert(cmd);
#endif
		buffers.push_back(cmd);
		index++;
		return cmd;
	}
}

void CommandPool::begin()
{
	if (pool == VK_NULL_HANDLE)
		return;

#ifdef VULKAN_DEBUG
	VK_ASSERT(in_flight.empty());
#endif
	if (index > 0 || secondary_index > 0)
		table->vkResetCommandPool(device->get_device(), pool, 0);
	index = 0;
	secondary_index = 0;
}

void CommandPool::trim()
{
	if (pool == VK_NULL_HANDLE)
		return;

	table->vkResetCommandPool(device->get_device(), pool, VK_COMMAND_POOL_RESET_RELEASE_RESOURCES_BIT);
	if (!buffers.empty())
		table->vkFreeCommandBuffers(device->get_device(), pool, buffers.size(), buffers.data());
	if (!secondary_buffers.empty())
		table->vkFreeCommandBuffers(device->get_device(), pool, secondary_buffers.size(), secondary_buffers.data());
	buffers.clear();
	secondary_buffers.clear();
	if (device->get_device_features().supports_maintenance_1)
		table->vkTrimCommandPoolKHR(device->get_device(), pool, 0);
}
}
