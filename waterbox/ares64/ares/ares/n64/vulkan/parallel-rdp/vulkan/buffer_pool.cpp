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

#include "buffer_pool.hpp"
#include "device.hpp"
#include <utility>

using namespace std;

namespace Vulkan
{
void BufferPool::init(Device *device_, VkDeviceSize block_size_,
                      VkDeviceSize alignment_, VkBufferUsageFlags usage_,
                      bool need_device_local_)
{
	device = device_;
	block_size = block_size_;
	alignment = alignment_;
	usage = usage_;
	need_device_local = need_device_local_;
}

void BufferPool::set_spill_region_size(VkDeviceSize spill_size_)
{
	spill_size = spill_size_;
}

void BufferPool::set_max_retained_blocks(size_t max_blocks)
{
	max_retained_blocks = max_blocks;
}

BufferBlock::~BufferBlock()
{
}

void BufferPool::reset()
{
	blocks.clear();
}

BufferBlock BufferPool::allocate_block(VkDeviceSize size)
{
	BufferDomain ideal_domain = need_device_local ?
	                            BufferDomain::Device :
	                            ((usage & VK_BUFFER_USAGE_TRANSFER_SRC_BIT) != 0) ? BufferDomain::Host : BufferDomain::LinkedDeviceHost;

	VkBufferUsageFlags extra_usage = ideal_domain == BufferDomain::Device ? VK_BUFFER_USAGE_TRANSFER_DST_BIT : 0;

	BufferBlock block;

	BufferCreateInfo info;
	info.domain = ideal_domain;
	info.size = size;
	info.usage = usage | extra_usage;

	block.gpu = device->create_buffer(info, nullptr);
	device->set_name(*block.gpu, "chain-allocated-block-gpu");
	block.gpu->set_internal_sync_object();

	// Try to map it, will fail unless the memory is host visible.
	block.mapped = static_cast<uint8_t *>(device->map_host_buffer(*block.gpu, MEMORY_ACCESS_WRITE_BIT));
	if (!block.mapped)
	{
		// Fall back to host memory, and remember to sync to gpu on submission time using DMA queue. :)
		BufferCreateInfo cpu_info;
		cpu_info.domain = BufferDomain::Host;
		cpu_info.size = size;
		cpu_info.usage = VK_BUFFER_USAGE_TRANSFER_SRC_BIT;

		block.cpu = device->create_buffer(cpu_info, nullptr);
		block.cpu->set_internal_sync_object();
		device->set_name(*block.cpu, "chain-allocated-block-cpu");
		block.mapped = static_cast<uint8_t *>(device->map_host_buffer(*block.cpu, MEMORY_ACCESS_WRITE_BIT));
	}
	else
		block.cpu = block.gpu;

	block.offset = 0;
	block.alignment = alignment;
	block.size = size;
	block.spill_size = spill_size;
	return block;
}

BufferBlock BufferPool::request_block(VkDeviceSize minimum_size)
{
	if ((minimum_size > block_size) || blocks.empty())
	{
		return allocate_block(max(block_size, minimum_size));
	}
	else
	{
		auto back = move(blocks.back());
		blocks.pop_back();

		back.mapped = static_cast<uint8_t *>(device->map_host_buffer(*back.cpu, MEMORY_ACCESS_WRITE_BIT));
		back.offset = 0;
		return back;
	}
}

void BufferPool::recycle_block(BufferBlock &block)
{
	VK_ASSERT(block.size == block_size);

	if (blocks.size() < max_retained_blocks)
		blocks.push_back(move(block));
	else
		block = {};
}

BufferPool::~BufferPool()
{
	VK_ASSERT(blocks.empty());
}

}
