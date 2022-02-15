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

#include "vulkan_common.hpp"
#include "vulkan_headers.hpp"
#include "object_pool.hpp"
#include "cookie.hpp"
#ifdef GRANITE_VULKAN_MT
#include <mutex>
#endif

namespace Vulkan
{
class Device;

class FenceHolder;
struct FenceHolderDeleter
{
	void operator()(FenceHolder *fence);
};

class FenceHolder : public Util::IntrusivePtrEnabled<FenceHolder, FenceHolderDeleter, HandleCounter>, public InternalSyncEnabled
{
public:
	friend struct FenceHolderDeleter;
	friend class WSI;

	~FenceHolder();
	void wait();
	bool wait_timeout(uint64_t nsec);

private:
	friend class Util::ObjectPool<FenceHolder>;
	FenceHolder(Device *device_, VkFence fence_)
		: device(device_),
		  fence(fence_),
		  timeline_semaphore(VK_NULL_HANDLE),
		  timeline_value(0)
	{
	}

	FenceHolder(Device *device_, uint64_t value, VkSemaphore timeline_semaphore_)
		: device(device_),
		  fence(VK_NULL_HANDLE),
		  timeline_semaphore(timeline_semaphore_),
		  timeline_value(value)
	{
		VK_ASSERT(value > 0);
	}

	VkFence get_fence() const;

	Device *device;
	VkFence fence;
	VkSemaphore timeline_semaphore;
	uint64_t timeline_value;
	bool observed_wait = false;
#ifdef GRANITE_VULKAN_MT
	std::mutex lock;
#endif
};

using Fence = Util::IntrusivePtr<FenceHolder>;
}
