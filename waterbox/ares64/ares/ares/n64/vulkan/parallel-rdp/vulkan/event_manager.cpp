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

#include "event_manager.hpp"
#include "device.hpp"

namespace Vulkan
{
EventManager::~EventManager()
{
	if (!workaround)
		for (auto &event : events)
			table->vkDestroyEvent(device->get_device(), event, nullptr);
}

void EventManager::recycle(VkEvent event)
{
	if (!workaround && event != VK_NULL_HANDLE)
	{
		table->vkResetEvent(device->get_device(), event);
		events.push_back(event);
	}
}

VkEvent EventManager::request_cleared_event()
{
	if (workaround)
	{
		// Can't use reinterpret_cast because of MSVC.
		return (VkEvent) ++workaround_counter;
	}
	else if (events.empty())
	{
		VkEvent event;
		VkEventCreateInfo info = { VK_STRUCTURE_TYPE_EVENT_CREATE_INFO };
		table->vkCreateEvent(device->get_device(), &info, nullptr, &event);
		return event;
	}
	else
	{
		auto event = events.back();
		events.pop_back();
		return event;
	}
}

void EventManager::init(Device *device_)
{
	device = device_;
	table = &device->get_device_table();
	workaround = device_->get_workarounds().emulate_event_as_pipeline_barrier;
}
}