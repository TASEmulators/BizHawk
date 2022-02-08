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

#include "fence.hpp"
#include "device.hpp"

namespace Vulkan
{
FenceHolder::~FenceHolder()
{
	if (fence != VK_NULL_HANDLE)
	{
		if (internal_sync)
			device->reset_fence_nolock(fence, observed_wait);
		else
			device->reset_fence(fence, observed_wait);
	}
}

VkFence FenceHolder::get_fence() const
{
	return fence;
}

void FenceHolder::wait()
{
	auto &table = device->get_device_table();

#ifdef GRANITE_VULKAN_MT
	// Waiting for the same VkFence in parallel is not allowed, and there seems to be some shenanigans on Intel
	// when waiting for a timeline semaphore in parallel with same value as well.
	std::lock_guard<std::mutex> holder{lock};
#endif
	if (observed_wait)
		return;

	if (timeline_value != 0)
	{
		VK_ASSERT(timeline_semaphore);
		VkSemaphoreWaitInfoKHR info = { VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO_KHR };
		info.semaphoreCount = 1;
		info.pSemaphores = &timeline_semaphore;
		info.pValues = &timeline_value;
		if (table.vkWaitSemaphoresKHR(device->get_device(), &info, UINT64_MAX) != VK_SUCCESS)
			LOGE("Failed to wait for timeline semaphore!\n");
		else
			observed_wait = true;
	}
	else
	{
		if (table.vkWaitForFences(device->get_device(), 1, &fence, VK_TRUE, UINT64_MAX) != VK_SUCCESS)
			LOGE("Failed to wait for fence!\n");
		else
			observed_wait = true;
	}
}

bool FenceHolder::wait_timeout(uint64_t timeout)
{
	bool ret = false;
	auto &table = device->get_device_table();
	if (timeline_value != 0)
	{
		VK_ASSERT(timeline_semaphore);
		VkSemaphoreWaitInfoKHR info = { VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO_KHR };
		info.semaphoreCount = 1;
		info.pSemaphores = &timeline_semaphore;
		info.pValues = &timeline_value;
		ret = table.vkWaitSemaphoresKHR(device->get_device(), &info, timeout) == VK_SUCCESS;
	}
	else
		ret = table.vkWaitForFences(device->get_device(), 1, &fence, VK_TRUE, timeout) == VK_SUCCESS;

	if (ret)
		observed_wait = true;
	return ret;
}

void FenceHolderDeleter::operator()(Vulkan::FenceHolder *fence)
{
	fence->device->handle_pool.fences.free(fence);
}
}
