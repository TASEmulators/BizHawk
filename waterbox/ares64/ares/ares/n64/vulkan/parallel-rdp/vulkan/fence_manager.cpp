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

#include "fence_manager.hpp"
#include "device.hpp"

namespace Vulkan
{
void FenceManager::init(Device *device_)
{
	device = device_;
	table = &device->get_device_table();
}

VkFence FenceManager::request_cleared_fence()
{
	if (!fences.empty())
	{
		auto ret = fences.back();
		fences.pop_back();
		return ret;
	}
	else
	{
		VkFence fence;
		VkFenceCreateInfo info = { VK_STRUCTURE_TYPE_FENCE_CREATE_INFO };
		table->vkCreateFence(device->get_device(), &info, nullptr, &fence);
		return fence;
	}
}

void FenceManager::recycle_fence(VkFence fence)
{
	fences.push_back(fence);
}

FenceManager::~FenceManager()
{
	for (auto &fence : fences)
		table->vkDestroyFence(device->get_device(), fence, nullptr);
}
}
