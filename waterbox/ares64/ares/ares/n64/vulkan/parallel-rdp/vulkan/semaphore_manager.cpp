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

#include "semaphore_manager.hpp"
#include "device.hpp"

namespace Vulkan
{
void SemaphoreManager::init(Device *device_)
{
	device = device_;
	table = &device->get_device_table();
}

SemaphoreManager::~SemaphoreManager()
{
	for (auto &sem : semaphores)
		table->vkDestroySemaphore(device->get_device(), sem, nullptr);
}

void SemaphoreManager::recycle(VkSemaphore sem)
{
	if (sem != VK_NULL_HANDLE)
		semaphores.push_back(sem);
}

VkSemaphore SemaphoreManager::request_cleared_semaphore()
{
	if (semaphores.empty())
	{
		VkSemaphore semaphore;
		VkSemaphoreCreateInfo info = { VK_STRUCTURE_TYPE_SEMAPHORE_CREATE_INFO };
		table->vkCreateSemaphore(device->get_device(), &info, nullptr, &semaphore);
		return semaphore;
	}
	else
	{
		auto sem = semaphores.back();
		semaphores.pop_back();
		return sem;
	}
}
}
